using System.Security.Cryptography;
using Linguistics.Core.Curriculum;

namespace Linguistics.Core.Content;

public enum ContentAssetProvenance
{
    WikimediaCommons,
    Generated,
}

public enum ContentAssetRepresentation
{
    Photograph,
    GeneratedIllustration,
}

public enum ContentAssetQaStatus
{
    Pending,
    MachineInspected,
    HumanReviewed,
}

public sealed record ContentAssetSource(
    string Title,
    string Author,
    string SourceUrl,
    DateOnly RetrievedOn,
    string OriginalSha256);

public sealed record ContentAssetGeneration(
    string Title,
    string GeneratorName,
    string PromptSummary,
    string OriginalSha256);

public sealed record ContentAssetTransformation(
    bool IsDerivative,
    bool Cropped,
    bool BackgroundRemoved,
    bool ShareAlikeObligationsRetained,
    string Description,
    ContentAssetQaStatus QaStatus,
    string QaNotes);

public sealed record ContentAssetRecord(
    string Id,
    string File,
    string MediaType,
    long ByteSize,
    string Sha256,
    ContentAssetProvenance Provenance,
    ContentAssetRepresentation Representation,
    ContentAssetSource? Source,
    ContentAssetGeneration? Generation,
    ContentAssetTransformation Transformation,
    ContentLicense License,
    ContentReview Review);

public sealed record ContentAssetManifest(
    int SchemaVersion,
    string PackId,
    int PackVersion,
    IReadOnlyList<ContentAssetRecord> Assets);

public sealed record ValidatedContentAsset(
    string PackId,
    int PackVersion,
    ContentAssetRecord Record,
    string AbsoluteFilePath)
{
    public string CacheKey => $"{PackId}.v{PackVersion}:{Record.Id}";
}

internal sealed record ContentAssetManifestLocation(
    ContentAssetManifest Manifest,
    string PackDirectory,
    string ManifestPath);

internal static class ContentAssetValidator
{
    public const int SupportedSchemaVersion = 1;
    public const long MaximumAssetBytes = 300 * 1024;
    public const long MaximumTemplateAssetBytes = 300 * 1024;
    public const long MaximumPackAssetBytes = 40 * 1024 * 1024;

    private static readonly HashSet<string> AllowedWikimediaLicenses = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "Public-Domain",
        "CC0-1.0",
        "CC-BY-1.0",
        "CC-BY-2.0",
        "CC-BY-2.5",
        "CC-BY-3.0",
        "CC-BY-4.0",
        "CC-BY-SA-1.0",
        "CC-BY-SA-2.0",
        "CC-BY-SA-2.5",
        "CC-BY-SA-3.0",
        "CC-BY-SA-4.0",
    };

    public static IReadOnlyList<ContentValidationError> Validate(
        IReadOnlyList<ContentPackDocument> packs,
        IReadOnlyList<ContentAssetManifestLocation> manifests,
        ContentLoadPolicy policy,
        out IReadOnlyList<ValidatedContentAsset> validatedAssets)
    {
        var errors = new List<ContentValidationError>();
        var assets = new List<ValidatedContentAsset>();
        var packsById = packs
            .Where(pack => pack?.Manifest is not null)
            .GroupBy(pack => pack.Manifest.Id, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        var assetIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var location in manifests.OrderBy(item => item.ManifestPath, StringComparer.Ordinal))
        {
            ValidateManifest(location, packsById, policy, assetIds, assets, errors);
        }

        ValidateTemplateAssetBudgets(packs, assets, errors);

        validatedAssets = assets.OrderBy(asset => asset.Record.Id, StringComparer.Ordinal).ToArray();
        return Order(errors);
    }

    private static void ValidateTemplateAssetBudgets(
        IReadOnlyList<ContentPackDocument> packs,
        IReadOnlyCollection<ValidatedContentAsset> assets,
        ICollection<ContentValidationError> errors)
    {
        var bytesByAssetId = assets
            .GroupBy(asset => asset.Record.Id, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(
                group => group.Key,
                group => new FileInfo(group.Single().AbsoluteFilePath).Length,
                StringComparer.Ordinal);

        foreach (var pack in packs.Where(pack => pack?.Manifest is not null))
        {
            foreach (var (lesson, lessonIndex) in (pack.Lessons ?? []).Select((lesson, index) => (lesson, index)))
            {
                if (lesson?.TemplateInstances is null)
                {
                    continue;
                }

                foreach (var (instance, instanceIndex) in lesson.TemplateInstances.Select((instance, index) => (instance, index)))
                {
                    if (instance?.Parameters is null)
                    {
                        continue;
                    }

                    var referencedAssetIds = instance.Parameters.Values
                        .Where(parameter => parameter is not null)
                        .SelectMany(AssetReferences)
                        .Where(bytesByAssetId.ContainsKey)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(id => id, StringComparer.Ordinal)
                        .ToArray();
                    var totalBytes = referencedAssetIds.Sum(id => bytesByAssetId[id]);
                    if (totalBytes <= MaximumTemplateAssetBytes)
                    {
                        continue;
                    }

                    var instancePath = $"lessons[{lessonIndex}].templateInstances[{instanceIndex}]";
                    errors.Add(new ContentValidationError(
                        "template.asset.budget",
                        pack.Manifest.Id,
                        $"{instancePath}.parameters",
                        $"Template instance '{instance.Id}' references {totalBytes} bytes across assets " +
                        $"{string.Join(", ", referencedAssetIds.Select(id => $"'{id}'"))}; " +
                        $"the per-template limit is {MaximumTemplateAssetBytes} bytes.",
                        lesson.Id,
                        "assets"));
                }
            }
        }
    }

    private static IEnumerable<string> AssetReferences(TemplateParameterValue parameter)
    {
        if (parameter.Kind == TemplateParameterKind.AssetReference &&
            !string.IsNullOrWhiteSpace(parameter.Value))
        {
            yield return parameter.Value;
        }

        if (parameter.Kind != TemplateParameterKind.OptionList || parameter.Options is null)
        {
            yield break;
        }

        foreach (var option in parameter.Options)
        {
            if (!string.IsNullOrWhiteSpace(option?.AssetReferenceId))
            {
                yield return option.AssetReferenceId;
            }
        }
    }

    private static void ValidateManifest(
        ContentAssetManifestLocation location,
        IReadOnlyDictionary<string, ContentPackDocument> packsById,
        ContentLoadPolicy policy,
        ISet<string> assetIds,
        ICollection<ValidatedContentAsset> assets,
        ICollection<ContentValidationError> errors)
    {
        var manifest = location.Manifest;
        var packId = string.IsNullOrWhiteSpace(manifest.PackId)
            ? Path.GetFileName(location.PackDirectory)
            : manifest.PackId;
        if (manifest.SchemaVersion != SupportedSchemaVersion)
        {
            Add(
                errors,
                "asset.schema.unsupported",
                packId,
                "assets.schemaVersion",
                $"Asset schema version {manifest.SchemaVersion} is unsupported; expected {SupportedSchemaVersion}.");
        }

        if (!packsById.TryGetValue(manifest.PackId, out var pack))
        {
            Add(
                errors,
                "asset.pack.reference",
                packId,
                "assets.packId",
                $"Asset manifest pack '{manifest.PackId}' does not resolve.");
        }
        else if (manifest.PackVersion != pack.Manifest.Version)
        {
            Add(
                errors,
                "asset.pack.version",
                packId,
                "assets.packVersion",
                $"Asset manifest version {manifest.PackVersion} does not match pack version {pack.Manifest.Version}.");
        }

        if (manifest.Assets is null)
        {
            Add(errors, "asset.collection", packId, "assets", "The asset collection is missing.");
            return;
        }

        long totalBytes = 0;
        var declaredFiles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (asset, index) in manifest.Assets.Select((asset, index) => (asset, index)))
        {
            var path = $"assets[{index}]";
            if (asset is null)
            {
                Add(errors, "asset.missing", packId, path, "An asset record is missing.");
                continue;
            }

            ValidateAssetShape(asset, packId, path, policy, errors);
            if (!assetIds.Add(asset.Id))
            {
                Add(errors, "asset.id.duplicate", packId, $"{path}.id", $"Asset ID '{asset.Id}' appears more than once.");
            }

            var fullPath = ResolveAssetPath(location.PackDirectory, asset.File);
            if (fullPath is null)
            {
                Add(
                    errors,
                    "asset.path",
                    packId,
                    $"{path}.file",
                    $"Asset '{asset.Id}' must use a relative path inside the pack's assets directory.");
                continue;
            }

            declaredFiles.Add(fullPath);
            if (!File.Exists(fullPath))
            {
                Add(errors, "asset.file.missing", packId, $"{path}.file", $"Asset '{asset.Id}' file '{asset.File}' is missing.");
                continue;
            }

            var actualBytes = new FileInfo(fullPath).Length;
            totalBytes += actualBytes;
            if (actualBytes != asset.ByteSize)
            {
                Add(
                    errors,
                    "asset.size.mismatch",
                    packId,
                    $"{path}.byteSize",
                    $"Asset '{asset.Id}' records {asset.ByteSize} bytes but the file contains {actualBytes} bytes.");
            }

            if (actualBytes > MaximumAssetBytes)
            {
                Add(
                    errors,
                    "asset.size",
                    packId,
                    $"{path}.file",
                    $"Asset '{asset.Id}' is {actualBytes} bytes; the per-asset limit is {MaximumAssetBytes} bytes.");
            }

            var actualHash = Sha256(fullPath);
            if (!string.Equals(actualHash, asset.Sha256, StringComparison.Ordinal))
            {
                Add(
                    errors,
                    "asset.hash",
                    packId,
                    $"{path}.sha256",
                    $"Asset '{asset.Id}' SHA-256 does not match its manifest record.");
            }

            assets.Add(new ValidatedContentAsset(packId, manifest.PackVersion, asset, fullPath));
        }

        if (totalBytes > MaximumPackAssetBytes)
        {
            Add(
                errors,
                "asset.pack.size",
                packId,
                "assets",
                $"Pack assets contain {totalBytes} bytes; the limit is {MaximumPackAssetBytes} bytes.");
        }

        var assetsDirectory = Path.Combine(location.PackDirectory, "assets");
        if (Directory.Exists(assetsDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(assetsDirectory, "*", SearchOption.AllDirectories)
                         .Where(IsSupportedImage)
                         .OrderBy(file => file, StringComparer.Ordinal))
            {
                var fullPath = Path.GetFullPath(file);
                if (!declaredFiles.Contains(fullPath))
                {
                    Add(
                        errors,
                        "asset.file.undeclared",
                        packId,
                        Path.GetRelativePath(location.PackDirectory, fullPath),
                        "Every bundled image must have exactly one asset manifest record.");
                }
            }
        }
    }

    private static void ValidateAssetShape(
        ContentAssetRecord asset,
        string packId,
        string path,
        ContentLoadPolicy policy,
        ICollection<ContentValidationError> errors)
    {
        if (!IsCanonicalIdentifier(asset.Id))
        {
            Add(errors, "asset.id", packId, $"{path}.id", $"Asset ID '{asset.Id}' is missing or not canonical.");
        }

        RequireText(asset.File, "asset.file", packId, $"{path}.file", errors);
        RequireText(asset.MediaType, "asset.mediaType", packId, $"{path}.mediaType", errors);
        var expectedMediaType = Path.GetExtension(asset.File).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => null,
        };
        if (expectedMediaType is null || !string.Equals(asset.MediaType, expectedMediaType, StringComparison.Ordinal))
        {
            Add(
                errors,
                "asset.mediaType",
                packId,
                $"{path}.mediaType",
                $"Asset '{asset.Id}' media type must match a PNG or JPEG file extension.");
        }

        RequireHash(asset.Sha256, packId, $"{path}.sha256", errors);
        if (asset.ByteSize < 1)
        {
            Add(errors, "asset.size", packId, $"{path}.byteSize", $"Asset '{asset.Id}' needs a positive byte size.");
        }

        if (!Enum.IsDefined(asset.Provenance))
        {
            Add(errors, "asset.provenance", packId, $"{path}.provenance", $"Asset '{asset.Id}' has invalid provenance.");
        }

        if (!Enum.IsDefined(asset.Representation))
        {
            Add(errors, "asset.representation", packId, $"{path}.representation", $"Asset '{asset.Id}' has an invalid representation.");
        }

        ValidateAssetLicense(asset, packId, path, policy, errors);
        ValidateAssetReview(asset.Review, packId, $"{path}.review", policy, errors);
        ValidateTransformation(asset, packId, path, errors);

        if (asset.Provenance == ContentAssetProvenance.WikimediaCommons)
        {
            if (asset.Source is null)
            {
                Add(errors, "asset.source.missing", packId, $"{path}.source", $"Wikimedia asset '{asset.Id}' needs original source metadata.");
            }
            else
            {
                RequireText(asset.Source.Title, "asset.source.field", packId, $"{path}.source.title", errors);
                RequireText(asset.Source.Author, "asset.source.field", packId, $"{path}.source.author", errors);
                RequireHash(asset.Source.OriginalSha256, packId, $"{path}.source.originalSha256", errors);
                if (!Uri.TryCreate(asset.Source.SourceUrl, UriKind.Absolute, out var sourceUri) ||
                    sourceUri.Scheme != Uri.UriSchemeHttps)
                {
                    Add(errors, "asset.source.url", packId, $"{path}.source.sourceUrl", $"Asset '{asset.Id}' needs an HTTPS source URL.");
                }

                if (asset.Source.RetrievedOn == default)
                {
                    Add(errors, "asset.source.field", packId, $"{path}.source.retrievedOn", $"Asset '{asset.Id}' needs a retrieval date.");
                }
            }

            if (asset.Generation is not null)
            {
                Add(errors, "asset.provenance", packId, $"{path}.generation", $"Wikimedia asset '{asset.Id}' cannot carry generated provenance.");
            }

            if (asset.License is not null &&
                !AllowedWikimediaLicenses.Contains(asset.License.Identifier))
            {
                Add(
                    errors,
                    "asset.license.unsupported",
                    packId,
                    $"{path}.license.identifier",
                    $"Wikimedia asset '{asset.Id}' uses unsupported license '{asset.License.Identifier}'.");
            }
        }
        else if (asset.Provenance == ContentAssetProvenance.Generated)
        {
            if (asset.Source is not null)
            {
                Add(errors, "asset.provenance", packId, $"{path}.source", $"Generated asset '{asset.Id}' cannot claim a photographed source.");
            }

            if (asset.Representation != ContentAssetRepresentation.GeneratedIllustration)
            {
                Add(
                    errors,
                    "asset.representation",
                    packId,
                    $"{path}.representation",
                    $"Generated asset '{asset.Id}' must be labeled as a generated illustration, never a photograph.");
            }

            if (asset.Generation is null)
            {
                Add(errors, "asset.generation.missing", packId, $"{path}.generation", $"Generated asset '{asset.Id}' needs generator provenance.");
            }
            else
            {
                RequireText(asset.Generation.Title, "asset.generation.field", packId, $"{path}.generation.title", errors);
                RequireText(asset.Generation.GeneratorName, "asset.generation.field", packId, $"{path}.generation.generatorName", errors);
                RequireText(asset.Generation.PromptSummary, "asset.generation.field", packId, $"{path}.generation.promptSummary", errors);
                RequireHash(asset.Generation.OriginalSha256, packId, $"{path}.generation.originalSha256", errors);
            }
        }
    }

    private static void ValidateAssetLicense(
        ContentAssetRecord asset,
        string packId,
        string path,
        ContentLoadPolicy policy,
        ICollection<ContentValidationError> errors)
    {
        var license = asset.License;
        if (license is null)
        {
            Add(errors, "asset.license.missing", packId, $"{path}.license", $"Asset '{asset.Id}' needs a complete license record.");
            return;
        }

        RequireText(license.Identifier, "asset.license.field", packId, $"{path}.license.identifier", errors);
        RequireText(license.CopyrightHolder, "asset.license.field", packId, $"{path}.license.copyrightHolder", errors);
        RequireText(license.LicenseTextLocation, "asset.license.field", packId, $"{path}.license.licenseTextLocation", errors);
        RequireText(license.IntendedUse, "asset.license.field", packId, $"{path}.license.intendedUse", errors);
        RequireText(license.RequiredAttribution, "asset.license.field", packId, $"{path}.license.requiredAttribution", errors);
        if (!Enum.IsDefined(license.ReviewStatus))
        {
            Add(errors, "asset.license.status", packId, $"{path}.license.reviewStatus", $"Asset '{asset.Id}' has an invalid license review status.");
        }
        else if (license.ReviewStatus == LicenseReviewStatus.Rejected ||
                 policy == ContentLoadPolicy.Runtime && license.ReviewStatus != LicenseReviewStatus.Reviewed)
        {
            Add(
                errors,
                "asset.license.unreviewed",
                packId,
                $"{path}.license.reviewStatus",
                $"Asset '{asset.Id}' license status '{license.ReviewStatus}' is not eligible for {policy}.");
        }
    }

    private static void ValidateAssetReview(
        ContentReview review,
        string packId,
        string path,
        ContentLoadPolicy policy,
        ICollection<ContentValidationError> errors)
    {
        if (review is null)
        {
            Add(errors, "asset.review.missing", packId, path, "Asset review metadata is required.");
            return;
        }

        RequireText(review.Notes, "asset.review.field", packId, $"{path}.notes", errors);
        var eligible = policy switch
        {
            ContentLoadPolicy.ValidationOnly => review.Status != ContentReviewStatus.Rejected,
            ContentLoadPolicy.AuthoringPreview => review.Status is
                ContentReviewStatus.MachineValidated or
                ContentReviewStatus.LinguisticallyReviewed or
                ContentReviewStatus.Approved,
            ContentLoadPolicy.Runtime => review.Status == ContentReviewStatus.Approved,
            _ => false,
        };
        if (!eligible)
        {
            Add(
                errors,
                "asset.review.ineligible",
                packId,
                $"{path}.status",
                $"Asset review status '{review.Status}' is not eligible for {policy}.");
        }
    }

    private static void ValidateTransformation(
        ContentAssetRecord asset,
        string packId,
        string path,
        ICollection<ContentValidationError> errors)
    {
        if (asset.Transformation is null)
        {
            Add(errors, "asset.transformation.missing", packId, $"{path}.transformation", $"Asset '{asset.Id}' needs a transformation record.");
            return;
        }

        RequireText(asset.Transformation.Description, "asset.transformation.field", packId, $"{path}.transformation.description", errors);
        RequireText(asset.Transformation.QaNotes, "asset.transformation.field", packId, $"{path}.transformation.qaNotes", errors);
        if (!Enum.IsDefined(asset.Transformation.QaStatus))
        {
            Add(errors, "asset.transformation.qa", packId, $"{path}.transformation.qaStatus", $"Asset '{asset.Id}' has an invalid QA status.");
        }

        var shareAlike = asset.License?.Identifier?.StartsWith("CC-BY-SA-", StringComparison.OrdinalIgnoreCase) == true;
        if (shareAlike && asset.Transformation.IsDerivative && !asset.Transformation.ShareAlikeObligationsRetained)
        {
            Add(
                errors,
                "asset.license.shareAlike",
                packId,
                $"{path}.transformation.shareAlikeObligationsRetained",
                $"Derivative asset '{asset.Id}' must retain its CC BY-SA obligations.");
        }
    }

    private static string? ResolveAssetPath(string packDirectory, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            Path.IsPathRooted(relativePath) ||
            relativePath.Contains('\\'))
        {
            return null;
        }

        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(packDirectory, normalized));
        var assetsRoot = Path.GetFullPath(Path.Combine(packDirectory, "assets")) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(assetsRoot, StringComparison.Ordinal)
            ? fullPath
            : null;
    }

    private static bool IsSupportedImage(string file) =>
        Path.GetExtension(file).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg";

    private static bool IsCanonicalIdentifier(string id)
    {
        try
        {
            return string.Equals(
                id,
                CurriculumIdentifier.Normalize(id, nameof(id)),
                StringComparison.Ordinal);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static void RequireHash(
        string value,
        string packId,
        string path,
        ICollection<ContentValidationError> errors)
    {
        if (value is null || value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)) ||
            !string.Equals(value, value.ToLowerInvariant(), StringComparison.Ordinal))
        {
            Add(errors, "asset.hash.format", packId, path, "SHA-256 values must be 64 lower-case hexadecimal characters.");
        }
    }

    private static void RequireText(
        string value,
        string code,
        string packId,
        string path,
        ICollection<ContentValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Add(errors, code, packId, path, "A required asset field is missing.");
        }
    }

    private static void Add(
        ICollection<ContentValidationError> errors,
        string code,
        string packId,
        string path,
        string message) =>
        errors.Add(new ContentValidationError(code, packId, path, message));

    private static IReadOnlyList<ContentValidationError> Order(
        IEnumerable<ContentValidationError> errors) =>
        errors
            .OrderBy(error => error.PackId, StringComparer.Ordinal)
            .ThenBy(error => error.Path, StringComparer.Ordinal)
            .ThenBy(error => error.Code, StringComparer.Ordinal)
            .ThenBy(error => error.Message, StringComparer.Ordinal)
            .ToArray();
}
