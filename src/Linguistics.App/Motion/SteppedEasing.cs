using Avalonia.Animation.Easings;

namespace Linguistics.App.Motion;

public sealed class SteppedEasing : Easing
{
    public SteppedEasing(int frames)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(frames, 1);
        Frames = frames;
    }

    public int Frames { get; }

    public override double Ease(double progress)
    {
        var clamped = Math.Clamp(progress, 0, 1);
        return clamped >= 1
            ? 1
            : Math.Floor(clamped * Frames) / Frames;
    }
}
