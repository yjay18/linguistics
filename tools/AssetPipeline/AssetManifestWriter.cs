using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Linguistics.Core.Content;

namespace Linguistics.AssetPipeline;

public static class AssetManifestWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false),
        },
    };

    public static ContentAssetRecord WikimediaRecord(
        string assetId,
        string manifestPath,
        string outputPath,
        FetchedWikimediaSource source,
        ProcessedImageResult processed,
        string qaNotes)
    {
        var candidate = source.Candidate;
        var isShareAlike = candidate.LicenseIdentifier.StartsWith(
            "CC-BY-SA-",
            StringComparison.OrdinalIgnoreCase);
        return new ContentAssetRecord(
            assetId,
            RelativeAssetPath(manifestPath, outputPath),
            MediaType(outputPath),
            processed.ByteSize,
            processed.Sha256,
            ContentAssetProvenance.WikimediaCommons,
            ContentAssetRepresentation.Photograph,
            new ContentAssetSource(
                candidate.Title,
                candidate.Author,
                candidate.SourceUrl,
                source.RetrievedOn,
                source.OriginalSha256),
            null,
            new ContentAssetTransformation(
                IsDerivative: processed.Cropped || processed.BackgroundRemoved,
                processed.Cropped,
                processed.BackgroundRemoved,
                ShareAlikeObligationsRetained:
                    isShareAlike && (processed.Cropped || processed.BackgroundRemoved),
                processed.Description,
                ContentAssetQaStatus.MachineInspected,
                qaNotes),
            new ContentLicense(
                candidate.LicenseIdentifier,
                candidate.Author,
                candidate.LicenseUrl,
                "Bundled local lesson image with source attribution; redistribution remains review-gated.",
                ModificationReviewed: false,
                RedistributionReviewed: false,
                $"{candidate.Title} by {candidate.Author}, {candidate.LicenseIdentifier}. {candidate.SourceUrl}",
                LicenseReviewStatus.Pending),
            new ContentReview(
                ContentReviewStatus.MachineValidated,
                null,
                null,
                "Machine-validated asset draft; competent license and content review is still required."));
    }

    public static ContentAssetRecord GeneratedRecord(
        string assetId,
        string manifestPath,
        string sourcePath,
        string outputPath,
        string title,
        string generatorName,
        string promptSummary,
        ProcessedImageResult processed,
        string qaNotes)
    {
        var originalHash = Sha256(sourcePath);
        return new ContentAssetRecord(
            assetId,
            RelativeAssetPath(manifestPath, outputPath),
            MediaType(outputPath),
            processed.ByteSize,
            processed.Sha256,
            ContentAssetProvenance.Generated,
            ContentAssetRepresentation.GeneratedIllustration,
            null,
            new ContentAssetGeneration(title, generatorName, promptSummary, originalHash),
            new ContentAssetTransformation(
                IsDerivative: processed.Cropped || processed.BackgroundRemoved,
                processed.Cropped,
                processed.BackgroundRemoved,
                ShareAlikeObligationsRetained: false,
                processed.Description,
                ContentAssetQaStatus.MachineInspected,
                qaNotes),
            new ContentLicense(
                "LicenseRef-Generated-Internal-Draft",
                "Linguistics project contributors",
                "docs/content-license.md",
                "Local machine-validated preview only; public redistribution remains blocked.",
                ModificationReviewed: false,
                RedistributionReviewed: false,
                $"{title}; generated with {generatorName}; see the prompt summary in assets.json.",
                LicenseReviewStatus.Pending),
            new ContentReview(
                ContentReviewStatus.MachineValidated,
                null,
                null,
                "Generated illustration draft; it is not a photograph of a real subject and still needs human review."));
    }

    public static void Upsert(
        string manifestPath,
        string packId,
        int packVersion,
        ContentAssetRecord record,
        bool replace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);
        ArgumentNullException.ThrowIfNull(record);
        var manifest = File.Exists(manifestPath)
            ? JsonSerializer.Deserialize<ContentAssetManifest>(
                File.ReadAllText(manifestPath),
                SerializerOptions) ?? throw new InvalidOperationException("The existing asset manifest decoded to null.")
            : new ContentAssetManifest(1, packId, packVersion, []);
        if (manifest.SchemaVersion != 1 ||
            !string.Equals(manifest.PackId, packId, StringComparison.Ordinal) ||
            manifest.PackVersion != packVersion)
        {
            throw new InvalidOperationException(
                "The existing asset manifest schema, pack ID, or pack version does not match this import.");
        }

        var records = manifest.Assets.ToDictionary(asset => asset.Id, StringComparer.Ordinal);
        if (!replace && records.ContainsKey(record.Id))
        {
            throw new InvalidOperationException(
                $"Asset '{record.Id}' already exists. Pass --replace only after reviewing the replacement.");
        }

        records[record.Id] = record;
        manifest = manifest with
        {
            Assets = records.Values.OrderBy(asset => asset.Id, StringComparer.Ordinal).ToArray(),
        };
        var directory = Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".{Path.GetFileName(manifestPath)}.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(temporary, JsonSerializer.Serialize(manifest, SerializerOptions) + Environment.NewLine);
        File.Move(temporary, manifestPath, overwrite: true);
    }

    public static T Read<T>(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), SerializerOptions) ??
        throw new InvalidOperationException($"'{path}' decoded to null.");

    public static void Write<T>(string path, T value)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(directory);
        File.WriteAllText(path, JsonSerializer.Serialize(value, SerializerOptions) + Environment.NewLine);
    }

    private static string RelativeAssetPath(string manifestPath, string outputPath)
    {
        var packDirectory = Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;
        var relative = Path.GetRelativePath(packDirectory, Path.GetFullPath(outputPath))
            .Replace(Path.DirectorySeparatorChar, '/');
        if (!relative.StartsWith("assets/", StringComparison.Ordinal) || relative.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Processed assets must be written inside the pack's assets directory.");
        }

        return relative;
    }

    private static string MediaType(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => throw new InvalidOperationException("Processed assets must be PNG or JPEG files."),
        };

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
