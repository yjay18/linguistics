using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Linguistics.Core.Content;

namespace Linguistics.Core.Tests;

[TestClass]
public sealed class ContentAssetValidationTests
{
    private const string AssetId = "asset.de.fixture";
    private const string SecondAssetId = "asset.de.fixture-second";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false),
        },
    };

    [TestMethod]
    public void ValidAssetManifestLoadsReadOnlyMetadataAndResolvedPath()
    {
        var directory = WriteFixture("valid");
        try
        {
            var catalog = ContentPackLoader.LoadDirectory(
                directory,
                ContentLoadPolicy.AuthoringPreview);

            var asset = catalog.Assets.Single(candidate => candidate.Record.Id == AssetId);
            Assert.AreEqual("language.de.core.v2:asset.de.fixture", asset.CacheKey);
            Assert.IsTrue(File.Exists(asset.AbsoluteFilePath));
            Assert.AreEqual(ContentAssetProvenance.WikimediaCommons, asset.Record.Provenance);
            Assert.AreEqual(ContentReviewStatus.MachineValidated, asset.Record.Review.Status);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow("missing-license", "asset.license.missing")]
    [DataRow("dangling-reference", "template.reference.asset")]
    [DataRow("oversize", "asset.size")]
    [DataRow("hash-mismatch", "asset.hash")]
    public void BadAssetFixturesFailWithPackAssetAndAttributableError(
        string corruption,
        string expectedCode)
    {
        var directory = WriteFixture(corruption);
        try
        {
            var exception = Assert.ThrowsExactly<ContentValidationException>(() =>
                ContentPackLoader.LoadDirectory(directory, ContentLoadPolicy.AuthoringPreview));
            var error = exception.Errors.FirstOrDefault(candidate => candidate.Code == expectedCode);

            Assert.IsNotNull(error, string.Join(Environment.NewLine, exception.Errors));
            Assert.AreEqual(
                corruption == "dangling-reference"
                    ? "language.de.a1.unit01"
                    : "language.de.core",
                error.PackId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(error.Path));
            StringAssert.Contains(error.Message, corruption == "dangling-reference" ? "asset.de.missing" : AssetId);
            if (corruption == "dangling-reference")
            {
                Assert.AreEqual("lesson.de.a1.u01.greetings-by-time", error.LessonId);
                Assert.AreEqual("backdrop", error.Parameter);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void MalformedAssetManifestNamesItsPackAndFile()
    {
        var directory = WriteFixture("valid");
        try
        {
            File.WriteAllText(
                Path.Combine(directory, "language.de.core", "assets.json"),
                "{\"schemaVersion\":1,\"assets\":[");

            var exception = Assert.ThrowsExactly<ContentValidationException>(() =>
                ContentPackLoader.LoadDirectory(directory, ContentLoadPolicy.AuthoringPreview));
            var error = exception.Errors.Single(candidate => candidate.Code == "asset.manifest.json");

            Assert.AreEqual("language.de.core", error.PackId);
            Assert.StartsWith("assets.json", error.Path, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void TemplateAssetBudgetCountsDistinctReferencedFilesTogether()
    {
        var directory = WriteFixture("template-budget");
        try
        {
            var exception = Assert.ThrowsExactly<ContentValidationException>(() =>
                ContentPackLoader.LoadDirectory(directory, ContentLoadPolicy.AuthoringPreview));
            var error = exception.Errors.Single(candidate => candidate.Code == "template.asset.budget");

            Assert.AreEqual("language.de.a1.unit01", error.PackId);
            Assert.AreEqual("lesson.de.a1.u01.greetings-by-time", error.LessonId);
            Assert.AreEqual("assets", error.Parameter);
            StringAssert.Contains(error.Message, AssetId);
            StringAssert.Contains(error.Message, SecondAssetId);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string WriteFixture(string corruption)
    {
        var packs = ContentPackLoader.LoadDirectory(
                Path.Combine(AppContext.BaseDirectory, "Content"),
                ContentLoadPolicy.AuthoringPreview)
            .Packs
            .Where(pack =>
                pack.Manifest.Kind == ContentPackKind.Transfer ||
                pack.Manifest.Id is "language.de.core" or "language.de.a1.unit01")
            .Select(WithoutAssetReferences)
            .ToArray();
        var targetIndex = Array.FindIndex(packs, pack => pack.Manifest.Id == "language.de.a1.unit01");
        var target = packs[targetIndex];
        var lesson = target.Lessons.Single(item =>
            item.Id == "lesson.de.a1.u01.greetings-by-time");
        var scene = lesson.TemplateInstances[0];
        var parameters = scene.Parameters.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);
        parameters["backdrop"] = new TemplateParameterValue(
            TemplateParameterKind.AssetReference,
            Value: corruption == "dangling-reference" ? "asset.de.missing" : AssetId);
        if (corruption == "template-budget")
        {
            parameters["backdrop"] = new TemplateParameterValue(
                TemplateParameterKind.AssetReference,
                Value: SecondAssetId);
            parameters["cast"] = parameters["cast"] with
            {
                Options =
                [
                    parameters["cast"].Options![0] with { AssetReferenceId = AssetId },
                    .. parameters["cast"].Options!.Skip(1),
                ],
            };
        }

        scene = scene with { Parameters = parameters };
        target = target with
        {
            Lessons =
            [
                lesson with
                {
                    TemplateInstances =
                    [
                        scene,
                        .. lesson.TemplateInstances.Skip(1),
                    ],
                },
            ],
        };
        packs[targetIndex] = target;

        var directory = Path.Combine(Path.GetTempPath(), $"linguistics-assets-{Guid.NewGuid():N}");
        foreach (var pack in packs)
        {
            var packDirectory = Path.Combine(directory, pack.Manifest.Id);
            Directory.CreateDirectory(packDirectory);
            File.WriteAllText(
                Path.Combine(packDirectory, "pack.json"),
                JsonSerializer.Serialize(pack, SerializerOptions));
        }

        var targetDirectory = Path.Combine(directory, "language.de.core");
        var assetsDirectory = Path.Combine(targetDirectory, "assets");
        Directory.CreateDirectory(assetsDirectory);
        var bytes = corruption == "oversize"
            ? new byte[(300 * 1024) + 1]
            : corruption == "template-budget"
                ? new byte[160 * 1024]
            : Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        var assetPath = Path.Combine(assetsDirectory, "fixture.png");
        File.WriteAllBytes(assetPath, bytes);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var record = new ContentAssetRecord(
            AssetId,
            "assets/fixture.png",
            "image/png",
            bytes.LongLength,
            corruption == "hash-mismatch" ? new string('0', 64) : hash,
            ContentAssetProvenance.WikimediaCommons,
            ContentAssetRepresentation.Photograph,
            new ContentAssetSource(
                "Synthetic fixture",
                "Synthetic author",
                "https://commons.wikimedia.org/wiki/File:Synthetic_fixture.png",
                new DateOnly(2026, 8, 30),
                hash),
            null,
            new ContentAssetTransformation(
                IsDerivative: false,
                Cropped: false,
                BackgroundRemoved: false,
                ShareAlikeObligationsRetained: false,
                "Test fixture kept at source dimensions.",
                ContentAssetQaStatus.MachineInspected,
                "Decoded fixture checked by the automated test."),
            corruption == "missing-license"
                ? null!
                : new ContentLicense(
                    "CC0-1.0",
                    "Synthetic author",
                    "https://creativecommons.org/publicdomain/zero/1.0/",
                    "Automated validation fixture only.",
                    ModificationReviewed: false,
                    RedistributionReviewed: false,
                    "No attribution required; fixture author retained for provenance.",
                    LicenseReviewStatus.Pending),
            new ContentReview(
                ContentReviewStatus.MachineValidated,
                null,
                null,
                "Synthetic asset validation fixture; not distribution approval."));
        var records = new List<ContentAssetRecord> { record };
        if (corruption == "template-budget")
        {
            var secondPath = Path.Combine(assetsDirectory, "fixture-second.png");
            File.WriteAllBytes(secondPath, bytes);
            records.Add(record with
            {
                Id = SecondAssetId,
                File = "assets/fixture-second.png",
            });
        }

        var manifest = new ContentAssetManifest(
            1,
            "language.de.core",
            2,
            records);
        File.WriteAllText(
            Path.Combine(targetDirectory, "assets.json"),
            JsonSerializer.Serialize(manifest, SerializerOptions));
        return directory;
    }

    private static ContentPackDocument WithoutAssetReferences(ContentPackDocument pack) =>
        pack with
        {
            Lessons = pack.Lessons.Select(lesson => lesson with
            {
                TemplateInstances = lesson.TemplateInstances.Select(instance => instance with
                {
                    Parameters = instance.Parameters
                        .Where(pair => pair.Value.Kind != TemplateParameterKind.AssetReference)
                        .ToDictionary(
                            pair => pair.Key,
                            pair => pair.Value.Kind == TemplateParameterKind.OptionList
                                ? pair.Value with
                                {
                                    Options = pair.Value.Options?
                                        .Select(option => option with { AssetReferenceId = null })
                                        .ToArray(),
                                }
                                : pair.Value,
                            StringComparer.Ordinal),
                }).ToArray(),
            }).ToArray(),
        };
}
