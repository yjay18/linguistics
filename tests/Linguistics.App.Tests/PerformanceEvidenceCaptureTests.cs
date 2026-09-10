using Linguistics.App.Features.Developer;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class PerformanceEvidenceCaptureTests
{
    [TestMethod]
    public void CaptureRequestRequiresDeveloperModeAndAnAbsoluteJsonPath()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), "performance-evidence.json");

        Assert.IsNull(PerformanceEvidenceCapture.ResolveRequestedOutputPath(null, outputPath));
        Assert.IsNull(PerformanceEvidenceCapture.ResolveRequestedOutputPath("0", outputPath));
        Assert.AreEqual(
            Path.GetFullPath(outputPath),
            PerformanceEvidenceCapture.ResolveRequestedOutputPath("1", outputPath));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            PerformanceEvidenceCapture.ResolveRequestedOutputPath("1", "performance-evidence.json"));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            PerformanceEvidenceCapture.ResolveRequestedOutputPath(
                "1",
                Path.Combine(Path.GetTempPath(), "performance-evidence.txt")));
    }

    [TestMethod]
    public void FrameSummaryReportsMedianTailMaximumAndBudgetMisses()
    {
        var summary = PerformanceEvidenceCapture.SummarizeFrameTimestamps(
        [
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(16),
            TimeSpan.FromMilliseconds(32),
            TimeSpan.FromMilliseconds(82),
        ]);

        Assert.AreEqual(3, summary.SampleCount);
        Assert.AreEqual(16, summary.MedianIntervalMilliseconds);
        Assert.AreEqual(50, summary.P95IntervalMilliseconds);
        Assert.AreEqual(50, summary.MaximumIntervalMilliseconds);
        Assert.AreEqual(1, summary.IntervalsOver34Milliseconds);
    }

    [TestMethod]
    public void FrameSummaryRejectsMissingOrNonIncreasingTimestamps()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            PerformanceEvidenceCapture.SummarizeFrameTimestamps([TimeSpan.Zero]));
        Assert.ThrowsExactly<ArgumentException>(() =>
            PerformanceEvidenceCapture.SummarizeFrameTimestamps(
            [
                TimeSpan.FromMilliseconds(16),
                TimeSpan.FromMilliseconds(16),
            ]));
    }
}
