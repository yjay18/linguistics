using System.Text.Json;
using Linguistics.App.Diagnostics;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class LocalDiagnosticLogTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task LogAcceptsOnlyBoundedRedactedFields()
    {
        await WithLogAsync(async (log, path) =>
        {
            var requestId = Guid.NewGuid();
            await log.WriteAsync(
                DiagnosticCategory.Review,
                DiagnosticEventCode.ReviewRecorded,
                DiagnosticOutcome.Succeeded,
                requestId,
                TimeSpan.FromMilliseconds(420),
                "review-v1");

            var text = await File.ReadAllTextAsync(path);
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            Assert.AreEqual("review", root.GetProperty("category").GetString());
            Assert.AreEqual("reviewRecorded", root.GetProperty("eventCode").GetString());
            Assert.AreEqual(requestId, root.GetProperty("requestId").GetGuid());
            Assert.AreEqual(420, root.GetProperty("durationMilliseconds").GetInt64());
            Assert.AreEqual("review-v1", root.GetProperty("configurationVersion").GetString());
            Assert.IsFalse(root.TryGetProperty("message", out _));
            Assert.IsFalse(root.TryGetProperty("path", out _));
            Assert.IsFalse(root.TryGetProperty("payload", out _));

            await Assert.ThrowsExactlyAsync<ArgumentException>(() => log.WriteAsync(
                DiagnosticCategory.Persistence,
                DiagnosticEventCode.ProfileLoadFailed,
                DiagnosticOutcome.Failed,
                configurationVersion: "/Users/person/learner reply"));
        });
    }

    [TestMethod]
    public async Task LogIsBoundedAndInspectionCountsCurrentEntries()
    {
        await WithLogAsync(async (_, path) =>
        {
            var log = new LocalDiagnosticLog(path, () => Now, maximumBytes: 400);
            for (var index = 0; index < 12; index++)
            {
                await log.WriteAsync(
                    DiagnosticCategory.Application,
                    DiagnosticEventCode.AppOpened,
                    DiagnosticOutcome.Succeeded,
                    requestId: Guid.NewGuid());
            }

            var snapshot = await log.InspectAsync();
            Assert.IsGreaterThan(0, snapshot.EntryCount);
            Assert.IsLessThanOrEqualTo(800, snapshot.SizeBytes);
        });
    }

    [TestMethod]
    public async Task DeleteTargetsOnlyTheDiagnosticFile()
    {
        await WithLogAsync(async (log, path) =>
        {
            await log.WriteAsync(
                DiagnosticCategory.Persistence,
                DiagnosticEventCode.ProfileLoaded,
                DiagnosticOutcome.Succeeded);
            var sibling = Path.Combine(Path.GetDirectoryName(path)!, "learner-profile.json");
            await File.WriteAllTextAsync(sibling, "keep");

            await log.DeleteAsync();

            Assert.IsFalse(File.Exists(path));
            Assert.AreEqual("keep", await File.ReadAllTextAsync(sibling));
        });
    }

    [TestMethod]
    public async Task DiagnosticFileLinksAreRejected()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var target = Path.Combine(
            Path.GetTempPath(),
            $"linguistics-log-target-{Guid.NewGuid():N}.jsonl");
        try
        {
            await WithLogAsync(async (log, path) =>
            {
                await File.WriteAllTextAsync(target, "outside");
                File.CreateSymbolicLink(path, target);

                await Assert.ThrowsExactlyAsync<DiagnosticLogException>(() => log.WriteAsync(
                    DiagnosticCategory.Application,
                    DiagnosticEventCode.AppOpened,
                    DiagnosticOutcome.Started));
                Assert.AreEqual("outside", await File.ReadAllTextAsync(target));
            });
        }
        finally
        {
            File.Delete(target);
        }
    }

    private static async Task WithLogAsync(
        Func<LocalDiagnosticLog, string, Task> assertion)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "linguistics-diagnostic-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "diagnostics.jsonl");
        try
        {
            await assertion(new LocalDiagnosticLog(path, () => Now), path);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
