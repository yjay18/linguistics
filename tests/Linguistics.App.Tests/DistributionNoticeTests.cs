using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using Linguistics.Core.Content;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class DistributionNoticeTests
{
    private const string NativeNoticeSha256 =
        "21504c46c4c58aa64c1055bd2dcbc5f9a136b4b8c412ed3cc6740e22c5b127f5";
    private const string AngleLicenseSha256 =
        "54aff7276217df9f6b5181613999d208c9e40d2b1d51bf55217837e6871a4a63";

    private static readonly string[] ExpectedRuntimePackages =
    [
        "Avalonia.Angle.Windows.Natives/2.1.27548.20260419",
        "Avalonia.Desktop/12.1.0",
        "Avalonia.FreeDesktop.AtSpi/12.1.0",
        "Avalonia.FreeDesktop/12.1.0",
        "Avalonia.HarfBuzz/12.1.0",
        "Avalonia.Native/12.1.0",
        "Avalonia.Remote.Protocol/12.1.0",
        "Avalonia.Skia/12.1.0",
        "Avalonia.Themes.Fluent/12.1.0",
        "Avalonia.Win32/12.1.0",
        "Avalonia.X11/12.1.0",
        "Avalonia/12.1.0",
        "HarfBuzzSharp.NativeAssets.Linux/8.3.1.3",
        "HarfBuzzSharp.NativeAssets.WebAssembly/8.3.1.3",
        "HarfBuzzSharp.NativeAssets.Win32/8.3.1.3",
        "HarfBuzzSharp.NativeAssets.macOS/8.3.1.3",
        "HarfBuzzSharp/8.3.1.3",
        "MicroCom.Runtime/0.11.6",
        "SkiaSharp.NativeAssets.Linux/3.119.4",
        "SkiaSharp.NativeAssets.WebAssembly/3.119.4",
        "SkiaSharp.NativeAssets.Win32/3.119.4",
        "SkiaSharp.NativeAssets.macOS/3.119.4",
        "SkiaSharp/3.119.4",
        "Tmds.DBus.Protocol/0.94.1",
    ];

    [TestMethod]
    public void RuntimePackageGraphHasOnlyAuditedLicenseMetadata()
    {
        using var assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Linguistics.App",
            "obj",
            "project.assets.json")));
        var root = assets.RootElement;
        var libraries = root.GetProperty("libraries");
        var runtimePackages = root
            .GetProperty("targets")
            .GetProperty("net10.0")
            .EnumerateObject()
            .Where(item =>
                libraries.GetProperty(item.Name).GetProperty("type").GetString() == "package" &&
                HasRuntimeAssets(item.Value))
            .Select(item => item.Name)
            .OrderBy(item => item, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(
            ExpectedRuntimePackages.OrderBy(item => item, StringComparer.Ordinal).ToArray(),
            runtimePackages);

        var packageRoots = root
            .GetProperty("packageFolders")
            .EnumerateObject()
            .Select(item => item.Name)
            .ToArray();
        foreach (var package in runtimePackages)
        {
            var separator = package.LastIndexOf('/');
            var id = package[..separator];
            var version = package[(separator + 1)..];
            var directory = PackageDirectory(packageRoots, id, version);
            var nuspec = Directory.GetFiles(directory, "*.nuspec", SearchOption.TopDirectoryOnly).Single();
            var license = XDocument
                .Load(nuspec)
                .Descendants()
                .Single(element => element.Name.LocalName == "license");

            if (id == "Avalonia.Angle.Windows.Natives")
            {
                Assert.AreEqual("file", license.Attribute("type")?.Value);
                Assert.AreEqual("LICENSE", license.Value);
                Assert.AreEqual(AngleLicenseSha256, Sha256(Path.Combine(directory, license.Value)));
            }
            else
            {
                Assert.AreEqual("expression", license.Attribute("type")?.Value, package);
                Assert.AreEqual("MIT", license.Value, package);
            }
        }

        var notice = File.ReadAllText(Path.Combine(RepositoryRoot, "docs", "third-party-notices.md"));
        foreach (var version in new[] { "12.1.0", "2.1.27548.20260419", "3.119.4", "8.3.1.3", "0.11.6", "0.94.1" })
        {
            Assert.Contains(version, notice, StringComparison.Ordinal);
        }

        Assert.Contains(NativeNoticeSha256, notice, StringComparison.Ordinal);
        Assert.Contains(AngleLicenseSha256, notice, StringComparison.Ordinal);
        Assert.AreEqual(
            NativeNoticeSha256,
            Sha256(Path.Combine(
                PackageDirectory(packageRoots, "SkiaSharp.NativeAssets.macOS", "3.119.4"),
                "THIRD-PARTY-NOTICES.txt")));
        Assert.AreEqual(
            NativeNoticeSha256,
            Sha256(Path.Combine(
                PackageDirectory(packageRoots, "HarfBuzzSharp.NativeAssets.macOS", "8.3.1.3"),
                "THIRD-PARTY-NOTICES.txt")));
    }

    [TestMethod]
    public void BundledAssetNoticeMatchesTheValidatedPendingInventory()
    {
        var catalog = ContentPackLoader.LoadDirectory(
            Path.Combine(RepositoryRoot, "content"),
            ContentLoadPolicy.AuthoringPreview);
        var assets = catalog.Assets.Select(asset => asset.Record).ToArray();
        var commons = assets.Where(asset => asset.Provenance == ContentAssetProvenance.WikimediaCommons).ToArray();
        var generated = assets.Where(asset => asset.Provenance == ContentAssetProvenance.Generated).ToArray();
        var notice = File.ReadAllText(Path.Combine(RepositoryRoot, "docs", "third-party-notices.md"));

        Assert.HasCount(12, assets);
        Assert.HasCount(7, commons);
        Assert.HasCount(5, generated);
        foreach (var asset in assets)
        {
            Assert.AreEqual(LicenseReviewStatus.Pending, asset.License.ReviewStatus, asset.Id);
            Assert.IsFalse(asset.License.ModificationReviewed, asset.Id);
            Assert.IsFalse(asset.License.RedistributionReviewed, asset.Id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(asset.License.RequiredAttribution), asset.Id);
            if (asset.License.Identifier.StartsWith("CC-BY-SA-", StringComparison.Ordinal) &&
                asset.Transformation.IsDerivative)
            {
                Assert.IsTrue(asset.Transformation.ShareAlikeObligationsRetained, asset.Id);
            }
        }

        foreach (var asset in commons)
        {
            Assert.IsNotNull(asset.Source);
            Assert.Contains(asset.Source.SourceUrl, notice, StringComparison.Ordinal);
            Assert.Contains(asset.Source.Author, notice, StringComparison.Ordinal);
            Assert.Contains(DisplayLicense(asset.License.Identifier), notice, StringComparison.Ordinal);
        }

        Assert.IsTrue(generated.All(asset =>
            asset.License.Identifier == "LicenseRef-Generated-Internal-Draft"));
        Assert.Contains("Five additional paper-stage files", notice, StringComparison.Ordinal);
        Assert.Contains("block public distribution", notice, StringComparison.Ordinal);
    }

    [TestMethod]
    public void AppProjectCopiesTheAuditedNoticesIntoPublishOutput()
    {
        var projectPath = Path.Combine(
            RepositoryRoot,
            "src",
            "Linguistics.App",
            "Linguistics.App.csproj");
        var project = XDocument.Load(projectPath);
        var publishedLinks = project
            .Descendants("Content")
            .Where(element =>
                element.Element("CopyToPublishDirectory")?.Value == "PreserveNewest")
            .Select(element => element.Element("Link")?.Value)
            .OfType<string>()
            .ToArray();

        foreach (var required in new[]
                 {
                     "content-license.md",
                     "third-party-notices.md",
                     "SkiaSharp-HarfBuzzSharp-THIRD-PARTY-NOTICES.txt",
                     "ANGLE-LICENSE.txt",
                 })
        {
            Assert.IsTrue(
                publishedLinks.Any(link => link.EndsWith(required, StringComparison.Ordinal)),
                required);
        }
    }

    private static bool HasRuntimeAssets(JsonElement item) =>
        HasEntries(item, "runtime") ||
        HasEntries(item, "native") ||
        HasEntries(item, "runtimeTargets");

    private static bool HasEntries(JsonElement item, string propertyName) =>
        item.TryGetProperty(propertyName, out var property) && property.EnumerateObject().Any();

    private static string DisplayLicense(string identifier) => identifier
        .Replace("CC-BY-SA-", "CC BY-SA ", StringComparison.Ordinal)
        .Replace("CC-BY-", "CC BY ", StringComparison.Ordinal);

    private static string Sha256(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

    private static string PackageDirectory(
        IEnumerable<string> packageRoots,
        string id,
        string version) => packageRoots
        .Select(root => Path.Combine(root, id.ToLowerInvariant(), version.ToLowerInvariant()))
        .First(Directory.Exists);

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../"));
}
