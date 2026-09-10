using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Linguistics.App.Features.Learn.Templates;

namespace Linguistics.App.Features.Developer;

internal static class TemplateGalleryCapture
{
    private const string DeveloperModeVariable = "LINGUISTICS_DEVELOPER_MODE";
    private const string OutputPathVariable = "LINGUISTICS_GALLERY_CAPTURE_PATH";

    public static string? RequestedOutputPath() => ResolveRequestedOutputPath(
        Environment.GetEnvironmentVariable(DeveloperModeVariable),
        Environment.GetEnvironmentVariable(OutputPathVariable));

    internal static string? ResolveRequestedOutputPath(string? developerMode, string? outputPath)
    {
        if (!string.Equals(developerMode, "1", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(outputPath))
        {
            return null;
        }

        if (!Path.IsPathFullyQualified(outputPath) ||
            !string.Equals(Path.GetExtension(outputPath), ".png", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{OutputPathVariable} must be an absolute PNG path.");
        }

        return Path.GetFullPath(outputPath);
    }

    public static void Save(Window window, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (!window.GetLogicalDescendants().OfType<TemplateGalleryView>().Any())
        {
            throw new InvalidOperationException("The template gallery is not active for capture.");
        }

        if (window.ClientSize.Width < 1 || window.ClientSize.Height < 1)
        {
            throw new InvalidOperationException("The template gallery window has no renderable size.");
        }

        var fullPath = Path.GetFullPath(outputPath);
        if (File.Exists(fullPath))
        {
            throw new InvalidOperationException("The gallery capture target already exists.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var pixelSize = PixelSize.FromSize(window.ClientSize, 1d);
        using var bitmap = new RenderTargetBitmap(pixelSize);
        bitmap.Render(window);
        bitmap.Save(fullPath, PngBitmapEncoderOptions.Default);
    }
}
