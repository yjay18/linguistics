using System.Globalization;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media.Transformation;
using Linguistics.App.Motion;

namespace Linguistics.App.Controls;

public sealed class PaperStamp : ContentControl
{
    public static readonly StyledProperty<double> AngleProperty =
        AvaloniaProperty.Register<PaperStamp, double>(nameof(Angle), -3);

    private PaperChoreography? _entrance;

    public PaperStamp()
    {
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += (_, _) => CancelEntrance();
    }

    public double Angle
    {
        get => GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    public async Task PlayEntranceAsync()
    {
        CancelEntrance();
        var reduceMotion = ShouldReduceMotion();
        if (!reduceMotion)
        {
            Transitions = null;
            Opacity = 0.18;
            RenderTransform = ParseTransform(Angle - 5, 1.34);
        }

        _entrance = new PaperChoreography(
        [
            new PaperChoreographyStep(
                TimeSpan.FromMilliseconds(34),
                cancellationToken => Task.Delay(34, cancellationToken),
                static () => { }),
            new PaperChoreographyStep(
                TimeSpan.FromMilliseconds(95),
                async cancellationToken =>
                {
                    SetPressState();
                    await Task.Delay(95, cancellationToken);
                },
                SetPressState),
            new PaperChoreographyStep(
                TimeSpan.FromMilliseconds(82),
                async cancellationToken =>
                {
                    SetReboundState();
                    await Task.Delay(82, cancellationToken);
                },
                SetReboundState),
            new PaperChoreographyStep(
                TimeSpan.FromMilliseconds(88),
                async cancellationToken =>
                {
                    SetFinalState();
                    await Task.Delay(88, cancellationToken);
                },
                SetFinalState),
        ]);
        await _entrance.PlayAsync(reduceMotion);
    }

    public void SkipEntrance() => _entrance?.Skip();

    private async void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs args) =>
        await PlayEntranceAsync();

    private bool ShouldReduceMotion()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        return MotionPreferences.ShouldReduce(savedPreference: false) ||
               topLevel?.Classes.Contains("motion-enabled") != true;
    }

    private void SetFinalState()
    {
        Transitions = null;
        Opacity = 1;
        RenderTransform = ParseTransform(Angle, 1);
    }

    private void SetPressState()
    {
        Transitions = CreateTransitions(
            TimeSpan.FromMilliseconds(90),
            new SteppedEasing(frames: 1));
        Opacity = 1;
        RenderTransform = ParseTransform(Angle, 0.92);
    }

    private void SetReboundState()
    {
        Transitions = CreateTransitions(
            TimeSpan.FromMilliseconds(78),
            new SteppedEasing(frames: 1));
        RenderTransform = ParseTransform(Angle + 0.7, 1.04);
    }

    private void CancelEntrance()
    {
        _entrance?.Skip();
        _entrance?.Dispose();
        _entrance = null;
    }

    private static Transitions CreateTransitions(TimeSpan duration, Easing easing) =>
    [
        new DoubleTransition
        {
            Property = OpacityProperty,
            Duration = duration,
            Easing = easing,
        },
        new TransformOperationsTransition
        {
            Property = RenderTransformProperty,
            Duration = duration,
            Easing = easing,
        },
    ];

    private static TransformOperations ParseTransform(double angle, double scale) =>
        TransformOperations.Parse(string.Create(
            CultureInfo.InvariantCulture,
            $"rotate({angle}deg) scale({scale})"));
}
