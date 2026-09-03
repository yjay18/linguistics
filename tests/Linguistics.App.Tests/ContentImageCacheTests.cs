using Linguistics.App.Content;
using Linguistics.App.Features.Learn.Templates;
using Linguistics.Core.Content;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class ContentImageCacheTests
{
    [TestMethod]
    public void CacheIndexesEachValidatedPackVersionAndNeverResolvesUnknownIds()
    {
        var catalog = LoadBundled();
        using var cache = new ContentImageCache(catalog.Assets);

        Assert.HasCount(12, cache.Assets);
        Assert.IsTrue(cache.TryGetAsset("asset.de.cafe.coffee", out var asset));
        Assert.AreEqual("language.de.core.v2:asset.de.cafe.coffee", asset!.CacheKey);
        Assert.IsTrue(cache.TryGetAsset("asset.de.cafe.coffee", out var repeated));
        Assert.AreSame(asset, repeated);
        Assert.IsFalse(cache.TryGetAsset("asset.de.missing", out var missing));
        Assert.IsNull(missing);
    }

    [TestMethod]
    public void SettingsDeclaresACompleteLocalCreditsSurface()
    {
        var catalog = LoadBundled();
        var repositoryRoot = RepositoryRoot();
        var settingsSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "Linguistics.App",
            "Features",
            "Settings",
            "SettingsView.axaml.cs"));
        var settingsMarkup = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "Linguistics.App",
            "Features",
            "Settings",
            "SettingsView.axaml"));

        Assert.HasCount(12, catalog.Assets);
        Assert.Contains("foreach (var asset in assets", settingsSource, StringComparison.Ordinal);
        Assert.Contains("CreateAssetCreditCard(asset)", settingsSource, StringComparison.Ordinal);
        Assert.Contains("AssetCreditsPanel", settingsMarkup, StringComparison.Ordinal);
        Assert.Contains("Settings_Images_Disclaimer", settingsMarkup, StringComparison.Ordinal);
    }

    [TestMethod]
    public void GalleryReferencesOnlyValidatedImagesAndProvingTemplatesOwnTextOnlyFallbacks()
    {
        var catalog = LoadBundled();
        var validatedIds = catalog.Assets.Select(asset => asset.Record.Id).ToHashSet(StringComparer.Ordinal);
        var provingTemplateIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "object-spotlight",
            "picture-match",
            "word-order-train",
        };

        foreach (var fixture in TemplateGalleryFixtures.All)
        {
            var assetIds = fixture.Parameters.Values.Values
                .SelectMany(value => new[] { value.AssetReferenceId }
                    .Concat(value.Options?.Select(option => option.AssetReferenceId) ?? []))
                .Where(assetId => !string.IsNullOrWhiteSpace(assetId))
                .Cast<string>()
                .ToArray();
            if (provingTemplateIds.Contains(fixture.TemplateId.Value))
            {
                Assert.IsNotEmpty(assetIds, fixture.TemplateId.Value);
            }

            Assert.IsTrue(assetIds.All(validatedIds.Contains), fixture.TemplateId.Value);
        }

        foreach (var renderer in new[]
                 {
                     "ObjectSpotlightRenderer.cs",
                     "PictureMatchRenderer.cs",
                     "WordOrderTrainRenderer.cs",
                 })
        {
            var source = File.ReadAllText(Path.Combine(
                RepositoryRoot(),
                "src",
                "Linguistics.App",
                "Features",
                "Learn",
                "Templates",
                renderer));
            Assert.Contains("CreateContentImage", source, StringComparison.Ordinal, renderer);
            Assert.Contains("CreateCreditsDisclosure", source, StringComparison.Ordinal, renderer);
            Assert.Contains("UseTextOnlyFallback", source, StringComparison.Ordinal, renderer);
        }
    }

    [TestMethod]
    public void RuntimeImagePathContainsNoNetworkClientAndToolStaysOutsideTheAppProject()
    {
        var repositoryRoot = RepositoryRoot();
        var cacheSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "Linguistics.App",
            "Content",
            "ContentImageCache.cs"));
        var appProject = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "Linguistics.App",
            "Linguistics.App.csproj"));

        Assert.DoesNotContain("HttpClient", cacheSource, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net", cacheSource, StringComparison.Ordinal);
        Assert.DoesNotContain("AssetPipeline", appProject, StringComparison.Ordinal);
    }

    private static ValidatedContentCatalog LoadBundled() =>
        ContentPackLoader.LoadDirectory(
            Path.Combine(AppContext.BaseDirectory, "Content"),
            ContentLoadPolicy.AuthoringPreview);

    private static string RepositoryRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
}
