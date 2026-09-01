using Linguistics.AssetPipeline;
using SkiaSharp;

namespace Linguistics.Core.Tests;

[TestClass]
public sealed class AssetPipelineTests
{
    [TestMethod]
    [DataRow("Public domain", "Public-Domain")]
    [DataRow("CC0 1.0", "CC0-1.0")]
    [DataRow("CC BY 4.0", "CC-BY-4.0")]
    [DataRow("CC-BY-SA-3.0", "CC-BY-SA-3.0")]
    public void CommonsLicenseFilterNormalizesOnlyAllowedFamilies(string input, string expected)
    {
        Assert.AreEqual(expected, WikimediaCommonsClient.NormalizeLicense(input));
        Assert.IsNull(WikimediaCommonsClient.NormalizeLicense("GFDL 1.2"));
    }

    [TestMethod]
    public void CommonsResponseKeepsCompleteAllowedAttributionAndSkipsAmbiguousLicense()
    {
        const string json =
            """
            {
              "query": {
                "pages": [
                  {
                    "pageid": 20,
                    "title": "File:Allowed.png",
                    "imageinfo": [{
                      "url": "https://upload.wikimedia.org/allowed.png",
                      "descriptionurl": "https://commons.wikimedia.org/wiki/File:Allowed.png",
                      "mime": "image/png",
                      "width": 800,
                      "height": 600,
                      "size": 12345,
                      "extmetadata": {
                        "Artist": { "value": "<b>Ada Example</b>" },
                        "LicenseShortName": { "value": "CC BY-SA 4.0" },
                        "LicenseUrl": { "value": "https://creativecommons.org/licenses/by-sa/4.0/" }
                      }
                    }]
                  },
                  {
                    "pageid": 21,
                    "title": "File:Ambiguous.png",
                    "imageinfo": [{
                      "url": "https://upload.wikimedia.org/ambiguous.png",
                      "descriptionurl": "https://commons.wikimedia.org/wiki/File:Ambiguous.png",
                      "mime": "image/png",
                      "width": 800,
                      "height": 600,
                      "size": 12345,
                      "extmetadata": {
                        "Artist": { "value": "Unknown" },
                        "LicenseShortName": { "value": "GFDL" },
                        "LicenseUrl": { "value": "https://example.invalid/gfdl" }
                      }
                    }]
                  }
                ]
              }
            }
            """;

        var candidate = WikimediaCommonsClient.ParseCandidates(json).Single();

        Assert.AreEqual(20, candidate.PageId);
        Assert.AreEqual("Ada Example", candidate.Author);
        Assert.AreEqual("CC-BY-SA-4.0", candidate.LicenseIdentifier);
        StringAssert.StartsWith(WikimediaCommonsClient.UserAgent, "LinguisticsAssetPipelineBot/0.1");
        StringAssert.Contains(WikimediaCommonsClient.UserAgent, "https://github.com/yjay18/linguistics");
    }

    [TestMethod]
    public void ProcessingDownscalesRemovesOnlyEdgeBackgroundAndMeetsBudget()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"asset-pipeline-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var sourcePath = Path.Combine(directory, "source.png");
        var outputPath = Path.Combine(directory, "output.png");
        try
        {
            using (var bitmap = new SKBitmap(600, 400, SKColorType.Rgba8888, SKAlphaType.Premul))
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.White);
                using var paint = new SKPaint { Color = SKColors.Crimson };
                canvas.DrawRect(new SKRect(170, 80, 430, 340), paint);
                canvas.Flush();
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                File.WriteAllBytes(sourcePath, data.ToArray());
            }

            var result = AssetImageProcessor.Process(
                sourcePath,
                outputPath,
                new ImageProcessingOptions(
                    MaximumDimension: 300,
                    MaximumBytes: 50 * 1024,
                    BackgroundColor: "#ffffff"));

            Assert.IsLessThanOrEqualTo(300, Math.Max(result.Width, result.Height));
            Assert.IsLessThanOrEqualTo(50 * 1024, result.ByteSize);
            Assert.IsTrue(result.BackgroundRemoved);
            using var output = SKBitmap.Decode(outputPath);
            Assert.AreEqual(0, output.GetPixel(0, 0).Alpha);
            Assert.IsGreaterThan(0, output.GetPixel(output.Width / 2, output.Height / 2).Alpha);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
