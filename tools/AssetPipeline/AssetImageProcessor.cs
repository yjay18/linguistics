using System.Security.Cryptography;
using SkiaSharp;

namespace Linguistics.AssetPipeline;

public sealed record ImageCrop(int X, int Y, int Width, int Height);

public sealed record ImageProcessingOptions(
    int MaximumDimension,
    long MaximumBytes,
    ImageCrop? Crop = null,
    string? BackgroundColor = null,
    int BackgroundThreshold = 28,
    int BackgroundFeather = 18);

public sealed record ProcessedImageResult(
    int Width,
    int Height,
    long ByteSize,
    string Sha256,
    bool Cropped,
    bool BackgroundRemoved,
    string Description);

public static class AssetImageProcessor
{
    public static ProcessedImageResult Process(
        string sourcePath,
        string outputPath,
        ImageProcessingOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(options);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The source image does not exist.", sourcePath);
        }

        if (options.MaximumDimension is < 96 or > 4096)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "The maximum dimension must be between 96 and 4096 pixels.");
        }

        if (options.MaximumBytes is < 4_096 or > 300 * 1024)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "The output budget must be between 4 KiB and 300 KiB.");
        }

        using var decoded = DecodeOriented(sourcePath);
        using var cropped = Crop(decoded, options.Crop);
        using var sized = ResizeToMaximum(cropped, options.MaximumDimension);
        if (options.BackgroundColor is { } background)
        {
            RemoveEdgeConnectedBackground(
                sized,
                ParseColor(background),
                options.BackgroundThreshold,
                options.BackgroundFeather);
        }

        var extension = Path.GetExtension(outputPath).ToLowerInvariant();
        var format = extension switch
        {
            ".png" => SKEncodedImageFormat.Png,
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            _ => throw new ArgumentException("The output path must end in .png, .jpg, or .jpeg.", nameof(outputPath)),
        };
        var encoded = EncodeWithinBudget(sized, format, options.MaximumBytes, out var finalWidth, out var finalHeight);
        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath))!;
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(outputPath, encoded);
        var hash = Convert.ToHexString(SHA256.HashData(encoded)).ToLowerInvariant();
        var operations = new List<string>
        {
            $"Decoded and downscaled to {finalWidth}x{finalHeight}",
            $"encoded as {(format == SKEncodedImageFormat.Png ? "PNG" : "JPEG")}",
        };
        if (options.Crop is not null)
        {
            operations.Add("cropped to the authored subject bounds");
        }

        if (options.BackgroundColor is not null)
        {
            operations.Add("removed only edge-connected pixels near the authored background colour");
        }

        return new ProcessedImageResult(
            finalWidth,
            finalHeight,
            encoded.LongLength,
            hash,
            options.Crop is not null,
            options.BackgroundColor is not null,
            string.Join("; ", operations) + ".");
    }

    private static SKBitmap DecodeOriented(string sourcePath)
    {
        using var stream = File.OpenRead(sourcePath);
        using var codec = SKCodec.Create(stream) ??
                          throw new InvalidOperationException("SkiaSharp could not inspect the source image.");
        using var decoded = SKBitmap.Decode(sourcePath) ??
                            throw new InvalidOperationException("SkiaSharp could not decode the source image.");
        var origin = codec.EncodedOrigin;
        if (origin == SKEncodedOrigin.TopLeft)
        {
            return decoded.Copy(SKColorType.Rgba8888) ??
                   throw new InvalidOperationException("The decoded bitmap could not be copied.");
        }

        var swapsAxes = origin is SKEncodedOrigin.LeftTop or
            SKEncodedOrigin.RightTop or
            SKEncodedOrigin.RightBottom or
            SKEncodedOrigin.LeftBottom;
        var result = new SKBitmap(
            new SKImageInfo(
                swapsAxes ? decoded.Height : decoded.Width,
                swapsAxes ? decoded.Width : decoded.Height,
                SKColorType.Rgba8888,
                SKAlphaType.Premul));
        for (var y = 0; y < decoded.Height; y++)
        {
            for (var x = 0; x < decoded.Width; x++)
            {
                var (destinationX, destinationY) = origin switch
                {
                    SKEncodedOrigin.TopRight => (decoded.Width - 1 - x, y),
                    SKEncodedOrigin.BottomRight => (decoded.Width - 1 - x, decoded.Height - 1 - y),
                    SKEncodedOrigin.BottomLeft => (x, decoded.Height - 1 - y),
                    SKEncodedOrigin.LeftTop => (y, x),
                    SKEncodedOrigin.RightTop => (decoded.Height - 1 - y, x),
                    SKEncodedOrigin.RightBottom => (decoded.Height - 1 - y, decoded.Width - 1 - x),
                    SKEncodedOrigin.LeftBottom => (y, decoded.Width - 1 - x),
                    _ => (x, y),
                };
                result.SetPixel(destinationX, destinationY, decoded.GetPixel(x, y));
            }
        }

        return result;
    }

    private static SKBitmap Crop(SKBitmap source, ImageCrop? crop)
    {
        if (crop is null)
        {
            return source.Copy(SKColorType.Rgba8888) ??
                   throw new InvalidOperationException("The source bitmap could not be copied.");
        }

        var rect = new SKRectI(crop.X, crop.Y, crop.X + crop.Width, crop.Y + crop.Height);
        if (crop.Width < 1 || crop.Height < 1 ||
            rect.Left < 0 || rect.Top < 0 || rect.Right > source.Width || rect.Bottom > source.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(crop), "The crop must stay inside the source image.");
        }

        var result = new SKBitmap(
            new SKImageInfo(crop.Width, crop.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (!source.ExtractSubset(result, rect))
        {
            result.Dispose();
            throw new InvalidOperationException("The requested crop could not be extracted.");
        }

        return result;
    }

    private static SKBitmap ResizeToMaximum(SKBitmap source, int maximumDimension)
    {
        var scale = Math.Min(1d, maximumDimension / (double)Math.Max(source.Width, source.Height));
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        if (width == source.Width && height == source.Height)
        {
            return source.Copy(SKColorType.Rgba8888) ??
                   throw new InvalidOperationException("The source bitmap could not be copied.");
        }

        return source.Resize(
                   new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul),
                   new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear)) ??
               throw new InvalidOperationException("The source bitmap could not be resized.");
    }

    private static byte[] EncodeWithinBudget(
        SKBitmap source,
        SKEncodedImageFormat format,
        long maximumBytes,
        out int finalWidth,
        out int finalHeight)
    {
        using var current = source.Copy(SKColorType.Rgba8888) ??
                            throw new InvalidOperationException("The processed bitmap could not be copied.");
        SKBitmap active = current;
        SKBitmap? resized = null;
        try
        {
            while (true)
            {
                var encoded = Encode(active, format, maximumBytes);
                if (encoded.LongLength <= maximumBytes)
                {
                    finalWidth = active.Width;
                    finalHeight = active.Height;
                    return encoded;
                }

                if (active.Width <= 96 && active.Height <= 96)
                {
                    throw new InvalidOperationException(
                        $"The processed image cannot meet the {maximumBytes}-byte budget without becoming smaller than 96 pixels.");
                }

                var nextWidth = Math.Max(96, (int)Math.Floor(active.Width * 0.84));
                var nextHeight = Math.Max(96, (int)Math.Floor(active.Height * 0.84));
                var next = active.Resize(
                               new SKImageInfo(nextWidth, nextHeight, SKColorType.Rgba8888, SKAlphaType.Premul),
                               new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear)) ??
                           throw new InvalidOperationException("The processed image could not be resized to meet its byte budget.");
                resized?.Dispose();
                resized = next;
                active = resized;
            }
        }
        finally
        {
            resized?.Dispose();
        }
    }

    private static byte[] Encode(
        SKBitmap bitmap,
        SKEncodedImageFormat format,
        long maximumBytes)
    {
        if (format == SKEncodedImageFormat.Png)
        {
            return Encode(bitmap, format, quality: 100);
        }

        for (var quality = 88; quality >= 52; quality -= 6)
        {
            var encoded = Encode(bitmap, format, quality);
            if (encoded.LongLength <= maximumBytes || quality == 52)
            {
                return encoded;
            }
        }

        throw new InvalidOperationException("JPEG quality selection did not produce an encoded image.");
    }

    private static byte[] Encode(SKBitmap bitmap, SKEncodedImageFormat format, int quality)
    {
        using var prepared = format == SKEncodedImageFormat.Jpeg
            ? FlattenForJpeg(bitmap)
            : null;
        using var image = SKImage.FromBitmap(prepared ?? bitmap);
        using var data = image.Encode(format, quality) ??
                         throw new InvalidOperationException("SkiaSharp could not encode the processed image.");
        return data.ToArray();
    }

    private static SKBitmap FlattenForJpeg(SKBitmap bitmap)
    {
        var result = new SKBitmap(
            new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Opaque));
        using var canvas = new SKCanvas(result);
        canvas.Clear(new SKColor(247, 243, 232));
        canvas.DrawBitmap(bitmap, 0, 0);
        canvas.Flush();
        return result;
    }

    private static void RemoveEdgeConnectedBackground(
        SKBitmap bitmap,
        SKColor background,
        int threshold,
        int feather)
    {
        if (threshold is < 0 or > 255 || feather is < 0 or > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), "Background thresholds must be between 0 and 255.");
        }

        var width = bitmap.Width;
        var height = bitmap.Height;
        var queued = new bool[width * height];
        var queue = new Queue<int>();
        void Enqueue(int x, int y)
        {
            var index = (y * width) + x;
            if (!queued[index] && Distance(bitmap.GetPixel(x, y), background) <= threshold + feather)
            {
                queued[index] = true;
                queue.Enqueue(index);
            }
        }

        for (var x = 0; x < width; x++)
        {
            Enqueue(x, 0);
            Enqueue(x, height - 1);
        }

        for (var y = 1; y < height - 1; y++)
        {
            Enqueue(0, y);
            Enqueue(width - 1, y);
        }

        while (queue.TryDequeue(out var index))
        {
            var x = index % width;
            var y = index / width;
            var color = bitmap.GetPixel(x, y);
            var distance = Distance(color, background);
            byte alpha = distance <= threshold || feather == 0
                ? (byte)0
                : (byte)Math.Clamp(
                    (int)Math.Round(255d * (distance - threshold) / feather),
                    0,
                    color.Alpha);
            bitmap.SetPixel(x, y, color.WithAlpha(alpha));
            if (x > 0)
            {
                Enqueue(x - 1, y);
            }

            if (x + 1 < width)
            {
                Enqueue(x + 1, y);
            }

            if (y > 0)
            {
                Enqueue(x, y - 1);
            }

            if (y + 1 < height)
            {
                Enqueue(x, y + 1);
            }
        }
    }

    private static double Distance(SKColor left, SKColor right)
    {
        var red = left.Red - right.Red;
        var green = left.Green - right.Green;
        var blue = left.Blue - right.Blue;
        return Math.Sqrt((red * red) + (green * green) + (blue * blue));
    }

    private static SKColor ParseColor(string value)
    {
        if (!SKColor.TryParse(value, out var color))
        {
            throw new ArgumentException(
                "Background colours use #RRGGBB or #AARRGGBB notation.",
                nameof(value));
        }

        return color;
    }
}
