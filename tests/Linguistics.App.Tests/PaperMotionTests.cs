using Linguistics.App.Motion;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class PaperMotionTests
{
    [TestMethod]
    public void SteppedEasingProducesTheRequestedNumberOfJumps()
    {
        const int frames = 4;
        var easing = new SteppedEasing(frames);
        var values = Enumerable
            .Range(0, 401)
            .Select(index => easing.Ease(index / 400d))
            .Distinct()
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { 0d, 0.25d, 0.5d, 0.75d, 1d },
            values);
        Assert.HasCount(frames + 1, values);
    }

    [TestMethod]
    public void SteppedEasingClampsBoundsAndRejectsInvalidFrameCounts()
    {
        var easing = new SteppedEasing(6);

        Assert.AreEqual(0, easing.Ease(-0.1));
        Assert.AreEqual(1, easing.Ease(1));
        Assert.AreEqual(1, easing.Ease(1.1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SteppedEasing(0));
    }

    [TestMethod]
    public async Task SkipCancelsTheActiveStepAndJumpsToEveryFinalValue()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstValue = 0d;
        var secondValue = 0d;
        using var choreography = new PaperChoreography(
        [
            new PaperChoreographyStep(
                TimeSpan.FromMilliseconds(200),
                async cancellationToken =>
                {
                    started.SetResult();
                    await hold.Task.WaitAsync(cancellationToken);
                },
                () => firstValue = 1),
            new PaperChoreographyStep(
                TimeSpan.FromMilliseconds(200),
                _ => Task.CompletedTask,
                () => secondValue = 2),
        ]);

        var play = choreography.PlayAsync(reduceMotion: false);
        await started.Task;
        choreography.Skip();

        Assert.AreEqual(1, firstValue);
        Assert.AreEqual(2, secondValue);
        await play;
    }

    [TestMethod]
    public async Task ReducedMotionAppliesFinalValuesWithoutRunningAnyStep()
    {
        var runCount = 0;
        var finalValue = 0d;
        using var choreography = new PaperChoreography(
        [
            new PaperChoreographyStep(
                TimeSpan.FromMilliseconds(250),
                _ =>
                {
                    runCount++;
                    return Task.CompletedTask;
                },
                () => finalValue = 1),
        ]);

        await choreography.PlayAsync(reduceMotion: true);

        Assert.AreEqual(0, runCount);
        Assert.AreEqual(1, finalValue);
    }

    [TestMethod]
    public void ChoreographyRejectsScenesThatReachFourSeconds()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new PaperChoreography(
        [
            new PaperChoreographyStep(
                PaperChoreography.MaximumDuration,
                _ => Task.CompletedTask,
                () => { }),
        ]));
    }
}
