using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Transformation;
using Avalonia.Platform;
using Linguistics.App.Controls;
using Linguistics.App.Motion;
using Linguistics.Core.Content;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Learn.Templates;

internal static class TemplateRendering
{
    private static readonly string[] OutcomeClasses =
    [
        "soft-card",
        "accent-card",
        "warning-card",
        "danger-card",
    ];

    public static string Text(ResolvedTemplateParameters parameters, string name) =>
        parameters.Values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value.Text)
            ? value.Text
            : throw new InvalidOperationException($"Template parameter '{name}' has no resolved text.");

    public static string OptionalText(ResolvedTemplateParameters parameters, string name) =>
        parameters.Values.TryGetValue(name, out var value) ? value.Text ?? string.Empty : string.Empty;

    public static string Localized(
        ResolvedTemplateParameters parameters,
        string name,
        LanguageCode instructionLanguage)
    {
        if (!parameters.Values.TryGetValue(name, out var value) || value.TextByLanguage is null)
        {
            throw new InvalidOperationException($"Template parameter '{name}' has no resolved language map.");
        }

        if (value.TextByLanguage.TryGetValue(instructionLanguage.Value, out var localized))
        {
            return localized;
        }

        if (value.TextByLanguage.TryGetValue("en", out var english))
        {
            return english;
        }

        return value.TextByLanguage
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => pair.Value)
            .First();
    }

    public static IReadOnlyList<TemplateOption> Options(
        ResolvedTemplateParameters parameters,
        string name) =>
        parameters.Values.TryGetValue(name, out var value) && value.Options is { Count: > 0 } options
            ? options
            : throw new InvalidOperationException($"Template parameter '{name}' has no resolved options.");

    public static string? AssetReference(ResolvedTemplateParameters parameters, string name) =>
        parameters.Values.TryGetValue(name, out var value) ? value.AssetReferenceId : null;

    public static PaperStage CreateStage(double height, string accessibleName)
    {
        var stage = new PaperStage
        {
            Height = height,
            ClipToBounds = true,
        };
        AutomationProperties.SetName(stage, accessibleName);
        return stage;
    }

    public static void AddBackdrop(PaperStage stage, bool useScenicPreview)
    {
        Control backdrop;
        if (useScenicPreview)
        {
            backdrop = new Image
            {
                Source = LoadPreviewBitmap("market-backdrop.png"),
                Stretch = Stretch.UniformToFill,
                Opacity = 0.88,
            };
        }
        else
        {
            var paper = new PaperCard { IsHitTestVisible = false };
            paper.Classes.Add("settings-sheet");
            backdrop = paper;
        }

        PaperStage.SetLayer(backdrop, PaperStageLayer.Backdrop);
        stage.Children.Add(backdrop);

        var wash = new Border { IsHitTestVisible = false, Opacity = 0.28 };
        wash.Classes.Add("soft-card");
        PaperStage.SetLayer(wash, PaperStageLayer.PaperWash);
        stage.Children.Add(wash);
    }

    public static Image? CreatePreviewImage(string? assetReferenceId, double height)
    {
        var fileName = assetReferenceId switch
        {
            "preview.market-stall" => "market-stall-cutout.png",
            "preview.market-square" => "market-backdrop.png",
            "preview.learner" => "learner-cutout.png",
            "preview.market-foreground" => "market-foreground-cutout.png",
            "preview.success-burst" => "success-burst-cutout.png",
            _ => null,
        };
        return fileName is null
            ? null
            : new Image
            {
                Source = LoadPreviewBitmap(fileName),
                Height = height,
                Stretch = Stretch.Uniform,
            };
    }

    public static Border CreateOutcomePanel(
        TemplateOutcomeState state,
        Func<TemplateOutcomeState, string> copy,
        out TextBlock text)
    {
        text = new TextBlock
        {
            Text = copy(state),
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetLiveSetting(text, AutomationLiveSetting.Polite);
        var panel = new Border
        {
            Padding = new Thickness(14, 10),
            Child = text,
        };
        ApplyOutcome(panel, text, state, copy);
        return panel;
    }

    public static void ApplyOutcome(
        Border panel,
        TextBlock text,
        TemplateOutcomeState state,
        Func<TemplateOutcomeState, string> copy)
    {
        foreach (var className in OutcomeClasses)
        {
            panel.Classes.Remove(className);
        }

        panel.Classes.Add(state switch
        {
            TemplateOutcomeState.Success => "accent-card",
            TemplateOutcomeState.Uncertain => "warning-card",
            TemplateOutcomeState.Failure => "danger-card",
            _ => "soft-card",
        });
        text.Text = copy(state);
    }

    public static PaperChoreographyStep Reveal(TimeSpan duration, params Control[] controls) =>
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

    public static PaperChoreographyStep Move(
        TimeSpan duration,
        Control control,
        double translateX,
        double translateY,
        double angle,
        double scale)
    {
        var transform = Transform(translateX, translateY, angle, scale);
        return new PaperChoreographyStep(
            duration,
            async cancellationToken =>
            {
                var easing = new SteppedEasing(
                    frames: Math.Max(1, (int)Math.Round(duration.TotalSeconds * 8)));
                control.Transitions =
                [
                    new DoubleTransition
                    {
                        Property = Visual.OpacityProperty,
                        Duration = duration,
                        Easing = easing,
                    },
                    new TransformOperationsTransition
                    {
                        Property = Visual.RenderTransformProperty,
                        Duration = duration,
                        Easing = easing,
                    },
                ];
                control.Opacity = 1;
                control.RenderTransform = transform;
                await Task.Delay(duration, cancellationToken);
            },
            () =>
            {
                ApplyFinal(control);
                control.RenderTransform = transform;
            });
    }

    public static void Prepare(bool shouldReduceMotion, params Control[] controls)
    {
        if (shouldReduceMotion)
        {
            foreach (var control in controls)
            {
                ApplyFinal(control);
            }

            return;
        }

        foreach (var control in controls)
        {
            control.Transitions = null;
            control.Opacity = 0;
        }
    }

    public static TransformOperations Transform(
        double translateX,
        double translateY,
        double angle,
        double scale) =>
        TransformOperations.Parse(FormattableString.Invariant(
            $"translate({translateX}px, {translateY}px) rotate({angle}deg) scale({scale})"));

    private static void BeginReveal(TimeSpan duration, Control control)
    {
        control.Transitions =
        [
            new DoubleTransition
            {
                Property = Visual.OpacityProperty,
                Duration = duration,
                Easing = new SteppedEasing(
                    frames: Math.Max(1, (int)Math.Round(duration.TotalSeconds * 8))),
            },
        ];
        control.Opacity = 1;
    }

    private static void ApplyFinal(Control control)
    {
        control.Transitions = null;
        control.Opacity = 1;
    }

    private static Bitmap LoadPreviewBitmap(string fileName)
    {
        using var stream = AssetLoader.Open(
            new Uri($"avares://Linguistics/Assets/PaperStage/{fileName}"));
        return new Bitmap(stream);
    }
}
