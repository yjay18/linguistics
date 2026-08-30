using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Linguistics.App.Motion;

namespace Linguistics.App.Features.Developer;

public partial class PaperStageSandboxView : UserControl
{
    private PaperChoreography? _scene;

    public PaperStageSandboxView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += (_, _) => CancelScene();
    }

    private async void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs args)
    {
        AttachedToVisualTree -= OnAttachedToVisualTree;
        await PlaySceneAsync();
    }

    private async void OnReplayClicked(object? sender, RoutedEventArgs args) =>
        await PlaySceneAsync();

    private void OnSkipClicked(object? sender, RoutedEventArgs args)
    {
        _scene?.Skip();
        LocationTape.SkipEntrance();
        VerdictStamp.SkipEntrance();
        MotionStatusText.Text = "Scene skipped to the complete final composition.";
    }

    private void OnShowAnchorsChanged(object? sender, RoutedEventArgs args)
    {
        if (AnchorGuideOverlay is not null)
        {
            AnchorGuideOverlay.IsVisible = ShowAnchorsCheckBox.IsChecked == true;
        }
    }

    private void OnLightThemeClicked(object? sender, RoutedEventArgs args) =>
        SetTheme(ThemeVariant.Light);

    private void OnDarkThemeClicked(object? sender, RoutedEventArgs args) =>
        SetTheme(ThemeVariant.Dark);

    private void OnSystemThemeClicked(object? sender, RoutedEventArgs args) =>
        SetTheme(ThemeVariant.Default);

    private async Task PlaySceneAsync()
    {
        CancelScene();
        var reduceMotion = ShouldReduceMotion();
        if (!reduceMotion)
        {
            SetStartState();
        }

        MotionStatusText.Text = reduceMotion
            ? "Reduced motion is active. The complete scene appears instantly."
            : "A cutout puppet crosses the stage in stepped poses and can be skipped.";

        _scene = new PaperChoreography(
        [
            RevealStep(TimeSpan.FromMilliseconds(170), BackdropLayer, PaperWashLayer),
            new PaperChoreographyStep(
                TimeSpan.FromMilliseconds(210),
                async cancellationToken =>
                {
                    BeginReveal(TimeSpan.FromMilliseconds(210), SupportingCastLayer, AmbientLayer);
                    await Task.WhenAll(
                        Task.Delay(210, cancellationToken),
                        LocationTape.PlayEntranceAsync());
                },
                () =>
                {
                    ApplyFinal(SupportingCastLayer);
                    ApplyFinal(AmbientLayer);
                    LocationTape.SkipEntrance();
                }),
            MoveCutoutStep(
                TimeSpan.FromMilliseconds(310),
                translateX: -128,
                translateY: -5,
                angle: -1.4,
                scale: 0.99),
            MoveCutoutStep(
                TimeSpan.FromMilliseconds(310),
                translateX: 0,
                translateY: 0,
                angle: 1.2,
                scale: 1),
            new PaperChoreographyStep(
                TimeSpan.FromMilliseconds(120),
                async cancellationToken =>
                {
                    BeginCutoutTransition(
                        TimeSpan.FromMilliseconds(120),
                        SubjectLayer,
                        ParseTransform(0, -17, -1.8, 1.025));
                    BeginCutoutTransition(
                        TimeSpan.FromMilliseconds(120),
                        ReactionLayer,
                        ParseTransform(0, 0, 2.5, 1.08));
                    await Task.Delay(120, cancellationToken);
                },
                () =>
                {
                    ApplyCutoutFinal(SubjectLayer, ParseTransform(0, -17, -1.8, 1.025));
                    ApplyCutoutFinal(ReactionLayer, ParseTransform(0, 0, 2.5, 1.08));
                }),
            new PaperChoreographyStep(
                TimeSpan.FromMilliseconds(120),
                async cancellationToken =>
                {
                    BeginCutoutTransition(
                        TimeSpan.FromMilliseconds(120),
                        SubjectLayer,
                        ParseTransform(0, 0, 1.2, 1));
                    BeginCutoutTransition(
                        TimeSpan.FromMilliseconds(120),
                        ReactionLayer,
                        ParseTransform(0, 0, 0, 1));
                    await Task.Delay(120, cancellationToken);
                },
                () =>
                {
                    ApplyCutoutFinal(SubjectLayer, ParseTransform(0, 0, 1.2, 1));
                    ApplyCutoutFinal(ReactionLayer, ParseTransform(0, 0, 0, 1));
                }),
            new PaperChoreographyStep(
                TimeSpan.FromMilliseconds(230),
                async cancellationToken =>
                {
                    BeginReveal(TimeSpan.FromMilliseconds(230), VerdictLayer);
                    await Task.WhenAll(
                        Task.Delay(230, cancellationToken),
                        VerdictStamp.PlayEntranceAsync());
                },
                () =>
                {
                    ApplyFinal(VerdictLayer);
                    VerdictStamp.SkipEntrance();
                }),
        ]);
        await _scene.PlayAsync(reduceMotion);
    }

    private PaperChoreographyStep MoveCutoutStep(
        TimeSpan duration,
        double translateX,
        double translateY,
        double angle,
        double scale)
    {
        var transform = ParseTransform(translateX, translateY, angle, scale);
        return new PaperChoreographyStep(
            duration,
            async cancellationToken =>
            {
                BeginCutoutTransition(duration, SubjectLayer, transform);
                await Task.Delay(duration, cancellationToken);
            },
            () => ApplyCutoutFinal(SubjectLayer, transform));
    }

    private PaperChoreographyStep RevealStep(TimeSpan duration, params Control[] controls) =>
        new(
            duration,
            async cancellationToken =>
            {
                foreach (var control in controls)
                {
                    BeginReveal(duration, control);
                }

                await Task.Delay(duration, cancellationToken);
            },
            () =>
            {
                foreach (var control in controls)
                {
                    ApplyFinal(control);
                }
            });

    private static void BeginReveal(TimeSpan duration, params Control[] controls)
    {
        foreach (var control in controls)
        {
            control.Transitions =
            [
                new DoubleTransition
                {
                    Property = OpacityProperty,
                    Duration = duration,
                    Easing = new SteppedEasing(frames: Math.Max(1, (int)Math.Round(duration.TotalSeconds * 8))),
                },
            ];
            control.Opacity = 1;
        }
    }

    private static void BeginCutoutTransition(
        TimeSpan duration,
        Control control,
        TransformOperations transform)
    {
        var easing = new SteppedEasing(frames: Math.Max(1, (int)Math.Round(duration.TotalSeconds * 8)));
        control.Transitions =
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
        control.Opacity = 1;
        control.RenderTransform = transform;
    }

    private static void ApplyFinal(Control control)
    {
        control.Transitions = null;
        control.Opacity = 1;
    }

    private static void ApplyCutoutFinal(Control control, TransformOperations transform)
    {
        control.Transitions = null;
        control.Opacity = 1;
        control.RenderTransform = transform;
    }

    private void SetStartState()
    {
        foreach (var control in new Control[]
                 {
                     BackdropLayer,
                     PaperWashLayer,
                     SupportingCastLayer,
                     AmbientLayer,
                     SubjectLayer,
                     ReactionLayer,
                     VerdictLayer,
                 })
        {
            control.Transitions = null;
            control.Opacity = 0;
        }

        SubjectLayer.RenderTransform = ParseTransform(-470, 8, -2.4, 0.98);
        ReactionLayer.RenderTransform = ParseTransform(0, 4, -5, 0.72);
    }

    private bool ShouldReduceMotion()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        return MotionPreferences.ShouldReduce(savedPreference: false) ||
               topLevel?.Classes.Contains("motion-enabled") != true;
    }

    private void CancelScene()
    {
        _scene?.Skip();
        _scene?.Dispose();
        _scene = null;
    }

    private static void SetTheme(ThemeVariant theme) =>
        Application.Current!.RequestedThemeVariant = theme;

    private static TransformOperations ParseTransform(
        double translateX,
        double translateY,
        double angle,
        double scale) =>
        TransformOperations.Parse(FormattableString.Invariant(
            $"translate({translateX}px, {translateY}px) rotate({angle}deg) scale({scale})"));
}
