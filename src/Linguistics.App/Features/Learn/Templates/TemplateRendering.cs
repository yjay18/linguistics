using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Linguistics.App.Content;
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

    public static IReadOnlyList<TemplateOption> OptionalOptions(
        ResolvedTemplateParameters parameters,
        string name) =>
        parameters.Values.TryGetValue(name, out var value) && value.Options is { Count: > 0 } options
            ? options
            : [];

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

    public static bool AddBackdrop(
        PaperStage stage,
        ContentImageCache? imageCache,
        string? assetReferenceId)
    {
        Control backdrop;
        if (CreateContentImage(imageCache, assetReferenceId, double.NaN, Stretch.UniformToFill) is { } image)
        {
            image.Opacity = 0.88;
            backdrop = image;
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
        return backdrop is Image;
    }

    public static Image? CreateContentImage(
        ContentImageCache? imageCache,
        string? assetReferenceId,
        double height,
        Stretch stretch = Stretch.Uniform)
    {
        if (imageCache is null ||
            !imageCache.TryGetBitmap(assetReferenceId, out var bitmap) ||
            bitmap is null)
        {
            return null;
        }

        return new Image
        {
            Source = bitmap,
            Height = height,
            Stretch = stretch,
        };
    }

    public static Expander? CreateCreditsDisclosure(
        ContentImageCache? imageCache,
        IEnumerable<string?> assetReferenceIds,
        string automationId)
    {
        ArgumentNullException.ThrowIfNull(assetReferenceIds);
        if (imageCache is null)
        {
            return null;
        }

        var assets = assetReferenceIds
            .Where(assetId => !string.IsNullOrWhiteSpace(assetId))
            .Distinct(StringComparer.Ordinal)
            .Select(assetId => imageCache.TryGetAsset(assetId, out var asset) ? asset : null)
            .OfType<ValidatedContentAsset>()
            .ToArray();
        if (assets.Length == 0)
        {
            return null;
        }

        var list = new StackPanel { Spacing = 10 };
        foreach (var asset in assets)
        {
            list.Children.Add(CreateAssetCreditCard(asset));
        }

        var disclosure = new Expander
        {
            Header = assets.Length == 1 ? "Image credit" : $"Image credits · {assets.Length}",
            Content = list,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        AutomationProperties.SetAutomationId(disclosure, automationId);
        AutomationProperties.SetName(
            disclosure,
            $"{assets.Length} image {(assets.Length == 1 ? "credit" : "credits")}. Expand for attribution details.");
        return disclosure;
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

    public static Border CreateAssetCreditCard(ValidatedContentAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        var record = asset.Record;
        var title = record.Source?.Title ?? record.Generation?.Title ?? record.Id;
        var provenance = record.Source is { } source
            ? $"Photograph by {source.Author} · {record.License.Identifier}"
            : $"Generated illustration · {record.Generation!.GeneratorName}";
        var details = new StackPanel { Spacing = 4 };
        details.Children.Add(new TextBlock
        {
            Text = title,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
        details.Children.Add(new TextBlock
        {
            Text = provenance,
            Classes = { "muted" },
            TextWrapping = TextWrapping.Wrap,
        });
        details.Children.Add(new TextBlock
        {
            Text = record.License.RequiredAttribution,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
        });
        if (record.Source is { } photographed)
        {
            details.Children.Add(new TextBlock
            {
                Text = $"Source: {photographed.SourceUrl}",
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
            });
            details.Children.Add(new TextBlock
            {
                Text = $"License: {record.License.LicenseTextLocation} · retrieved {photographed.RetrievedOn:yyyy-MM-dd}",
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
            });
        }
        else
        {
            details.Children.Add(new TextBlock
            {
                Text = $"Prompt summary: {record.Generation!.PromptSummary}",
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
            });
        }

        details.Children.Add(new TextBlock
        {
            Text = record.Transformation.IsDerivative
                ? $"Processed derivative: {record.Transformation.Description}"
                : record.Transformation.Description,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
        });
        details.Children.Add(new TextBlock
        {
            Text = "Preview asset · license and redistribution review remain pending.",
            Classes = { "muted" },
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
        });
        var card = new Border
        {
            Padding = new Thickness(12, 10),
            Child = details,
        };
        card.Classes.Add("soft-card");
        AutomationProperties.SetName(card, $"Image credit for {title}. {provenance}.");
        return card;
    }
}
