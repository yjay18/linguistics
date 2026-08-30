using System.Globalization;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media.Transformation;
using Linguistics.App.Motion;

namespace Linguistics.App.Controls;

public sealed class PaperTape : ContentControl
{
    public static readonly StyledProperty<double> AngleProperty =
        AvaloniaProperty.Register<PaperTape, double>(nameof(Angle), -1.5);

    private PaperChoreography? _entrance;

    public PaperTape()
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
            Opacity = 0.45;
            RenderTransform = ParseTransform(Angle - 3, 1.06, -8);
        }

        _entrance = new PaperChoreography(
        [
            new PaperChoreographyStep(
                TimeSpan.FromMilliseconds(34),
                cancellationToken => Task.Delay(34, cancellationToken),
                static () => { }),
            new PaperChoreographyStep(
                TimeSpan.FromMilliseconds(220),
                async cancellationToken =>
                {
                    Transitions = CreateTransitions(
                        TimeSpan.FromMilliseconds(220),
                        new SteppedEasing(frames: 2));
                    Opacity = 1;
                    RenderTransform = ParseTransform(Angle, 1, 0);
                    await Task.Delay(220, cancellationToken);
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
        RenderTransform = ParseTransform(Angle, 1, 0);
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

    private static TransformOperations ParseTransform(double angle, double scale, double translateY) =>
        TransformOperations.Parse(string.Create(
            CultureInfo.InvariantCulture,
            $"translateY({translateY}px) rotate({angle}deg) scale({scale})"));
}
