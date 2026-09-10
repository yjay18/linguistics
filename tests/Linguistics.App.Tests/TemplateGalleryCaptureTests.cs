using Linguistics.App.Features.Developer;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class TemplateGalleryCaptureTests
{
    [TestMethod]
    public void CaptureRequestRequiresDeveloperModeAndAnAbsolutePngPath()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), "template-gallery.png");

        Assert.IsNull(TemplateGalleryCapture.ResolveRequestedOutputPath(null, outputPath));
        Assert.IsNull(TemplateGalleryCapture.ResolveRequestedOutputPath("0", outputPath));
        Assert.AreEqual(
            Path.GetFullPath(outputPath),
            TemplateGalleryCapture.ResolveRequestedOutputPath("1", outputPath));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            TemplateGalleryCapture.ResolveRequestedOutputPath("1", "template-gallery.png"));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            TemplateGalleryCapture.ResolveRequestedOutputPath(
                "1",
                Path.Combine(Path.GetTempPath(), "template-gallery.jpg")));
    }
}
