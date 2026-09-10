using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Controls;
using Linguistics.App.Content;
using Linguistics.App.Diagnostics;

namespace Linguistics.App.Features.Developer;

internal sealed record AnimationFrameTimingSummary(
    int SampleCount,
    double MedianIntervalMilliseconds,
    double P95IntervalMilliseconds,
    double MaximumIntervalMilliseconds,
    int IntervalsOver34Milliseconds);

internal sealed record PerformanceEvidenceSnapshot(
    int SchemaVersion,
    long ProcessEntryToReadyMilliseconds,
    long ContentCatalogLoadMilliseconds,
    DiagnosticOutcome ContentCatalogLoadOutcome,
    long WorkingSetBytes,
    long ManagedHeapBytes,
    int DecodedImageCount,
    long EstimatedDecodedBytes,
    int MaximumDecodedImages,
    long MaximumDecodedBytes,
    AnimationFrameTimingSummary AnimationFrames);

internal static class PerformanceEvidenceCapture
{
    private const string DeveloperModeVariable = "LINGUISTICS_DEVELOPER_MODE";
    private const string OutputPathVariable = "LINGUISTICS_PERFORMANCE_CAPTURE_PATH";
    private const int FrameIntervalSampleCount = 30;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

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
            !string.Equals(Path.GetExtension(outputPath), ".json", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{OutputPathVariable} must be an absolute JSON path.");
        }

        return Path.GetFullPath(outputPath);
    }

    public static async Task<PerformanceEvidenceSnapshot> SaveAsync(
        Window window,
        string outputPath,
        TimeSpan processEntryToReadyDuration,
        StartupPerformanceSnapshot startup,
        ContentImageCache imageCache,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(startup);
        ArgumentNullException.ThrowIfNull(imageCache);
        if (processEntryToReadyDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(processEntryToReadyDuration));
        }

        var fullPath = Path.GetFullPath(outputPath);
        if (File.Exists(fullPath))
        {
            throw new InvalidOperationException("The performance evidence target already exists.");
        }

        foreach (var asset in imageCache.Assets)
        {
            imageCache.TryGetBitmap(asset.Record.Id, out _);
        }

        var frameTimestamps = await SampleFrameTimestampsAsync(window, cancellationToken);
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        var snapshot = new PerformanceEvidenceSnapshot(
            SchemaVersion: 1,
            ProcessEntryToReadyMilliseconds: checked((long)processEntryToReadyDuration.TotalMilliseconds),
            ContentCatalogLoadMilliseconds: checked((long)startup.ContentCatalogLoadDuration.TotalMilliseconds),
            startup.ContentCatalogLoadOutcome,
            WorkingSetBytes: process.WorkingSet64,
            ManagedHeapBytes: GC.GetTotalMemory(forceFullCollection: false),
            imageCache.DecodedImageCount,
            imageCache.EstimatedDecodedBytes,
            imageCache.MaximumDecodedImages,
            imageCache.MaximumDecodedBytes,
            SummarizeFrameTimestamps(frameTimestamps));

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(
            fullPath,
            JsonSerializer.Serialize(snapshot, JsonOptions),
            cancellationToken);
        return snapshot;
    }

    internal static AnimationFrameTimingSummary SummarizeFrameTimestamps(
        IReadOnlyList<TimeSpan> timestamps)
    {
        ArgumentNullException.ThrowIfNull(timestamps);
        if (timestamps.Count < 2)
        {
            throw new ArgumentException("At least two animation-frame timestamps are required.", nameof(timestamps));
        }

        var intervals = timestamps
            .Zip(timestamps.Skip(1), (previous, current) => current - previous)
            .ToArray();
        if (intervals.Any(interval => interval <= TimeSpan.Zero))
        {
            throw new ArgumentException(
                "Animation-frame timestamps must increase monotonically.",
                nameof(timestamps));
        }

        var orderedMilliseconds = intervals
            .Select(interval => interval.TotalMilliseconds)
            .Order()
            .ToArray();
        var middle = orderedMilliseconds.Length / 2;
        var median = orderedMilliseconds.Length % 2 == 0
            ? (orderedMilliseconds[middle - 1] + orderedMilliseconds[middle]) / 2
            : orderedMilliseconds[middle];
        var p95Index = Math.Clamp(
            (int)Math.Ceiling(orderedMilliseconds.Length * 0.95) - 1,
            0,
            orderedMilliseconds.Length - 1);

        return new AnimationFrameTimingSummary(
            intervals.Length,
            Milliseconds(median),
            Milliseconds(orderedMilliseconds[p95Index]),
            Milliseconds(orderedMilliseconds[^1]),
            orderedMilliseconds.Count(interval => interval > 34));
    }

    private static double Milliseconds(double value) =>
        Math.Round(value, 3, MidpointRounding.AwayFromZero);

    private static async Task<IReadOnlyList<TimeSpan>> SampleFrameTimestampsAsync(
        TopLevel topLevel,
        CancellationToken cancellationToken)
    {
        var timestamps = new List<TimeSpan>(FrameIntervalSampleCount + 1);
        while (timestamps.Count <= FrameIntervalSampleCount)
        {
            var nextFrame = new TaskCompletionSource<TimeSpan>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            topLevel.InvalidateVisual();
            topLevel.RequestAnimationFrame(timestamp => nextFrame.TrySetResult(timestamp));
            timestamps.Add(await nextFrame.Task.WaitAsync(cancellationToken));
        }

        return timestamps;
    }
}
