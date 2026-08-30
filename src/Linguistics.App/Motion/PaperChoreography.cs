using Avalonia.Animation;

namespace Linguistics.App.Motion;

internal sealed record PaperChoreographyStep(
    TimeSpan Duration,
    Func<CancellationToken, Task> RunAsync,
    Action ApplyFinal)
{
    public static PaperChoreographyStep Keyframes(
        TimeSpan duration,
        Animatable target,
        Animation animation,
        Action applyFinal)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(animation);
        ArgumentNullException.ThrowIfNull(applyFinal);

        return new PaperChoreographyStep(
            duration,
            cancellationToken => animation.RunAsync(target, cancellationToken),
            applyFinal);
    }
}

internal sealed class PaperChoreography : IDisposable
{
    internal static readonly TimeSpan MaximumDuration = TimeSpan.FromSeconds(4);

    private readonly object _gate = new();
    private readonly IReadOnlyList<PaperChoreographyStep> _steps;
    private readonly bool[] _finalApplied;
    private CancellationTokenSource? _activeRun;
    private bool _isRunning;
    private bool _skipRequested;

    public PaperChoreography(IEnumerable<PaperChoreographyStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        _steps = steps.ToArray();
        if (_steps.Count == 0)
        {
            throw new ArgumentException("A choreography requires at least one step.", nameof(steps));
        }

        if (_steps.Any(step =>
                step.Duration < TimeSpan.Zero ||
                step.RunAsync is null ||
                step.ApplyFinal is null))
        {
            throw new ArgumentException(
                "Every choreography step requires a nonnegative duration, runner, and final state.",
                nameof(steps));
        }

        var totalDuration = _steps.Aggregate(TimeSpan.Zero, (total, step) => total + step.Duration);
        if (totalDuration >= MaximumDuration)
        {
            throw new ArgumentOutOfRangeException(
                nameof(steps),
                $"Paper choreographies must finish in less than {MaximumDuration.TotalSeconds:0} seconds.");
        }

        _finalApplied = new bool[_steps.Count];
    }

    public async Task PlayAsync(bool reduceMotion, CancellationToken cancellationToken = default)
    {
        CancellationTokenSource activeRun;
        lock (_gate)
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("This choreography is already running.");
            }

            _isRunning = true;
            _skipRequested = false;
            Array.Fill(_finalApplied, false);
            _activeRun = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            activeRun = _activeRun;
        }

        try
        {
            if (reduceMotion)
            {
                ApplyAllFinalStates();
                return;
            }

            for (var index = 0; index < _steps.Count; index++)
            {
                await _steps[index].RunAsync(activeRun.Token);
                ApplyFinalState(index);
            }
        }
        catch (OperationCanceledException) when (SkipWasRequested())
        {
        }
        finally
        {
            lock (_gate)
            {
                _activeRun?.Dispose();
                _activeRun = null;
                _isRunning = false;
            }
        }
    }

    public void Skip()
    {
        CancellationTokenSource? activeRun;
        lock (_gate)
        {
            _skipRequested = true;
            activeRun = _activeRun;
        }

        activeRun?.Cancel();
        ApplyAllFinalStates();
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _activeRun?.Cancel();
            _activeRun?.Dispose();
            _activeRun = null;
            _isRunning = false;
        }
    }

    private bool SkipWasRequested()
    {
        lock (_gate)
        {
            return _skipRequested;
        }
    }

    private void ApplyAllFinalStates()
    {
        for (var index = 0; index < _steps.Count; index++)
        {
            ApplyFinalState(index);
        }
    }

    private void ApplyFinalState(int index)
    {
        Action? applyFinal = null;
        lock (_gate)
        {
            if (!_finalApplied[index])
            {
                _finalApplied[index] = true;
                applyFinal = _steps[index].ApplyFinal;
            }
        }

        applyFinal?.Invoke();
    }
}
