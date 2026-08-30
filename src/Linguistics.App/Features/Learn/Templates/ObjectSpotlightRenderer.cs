using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Linguistics.App.Controls;
using Linguistics.App.Motion;
using Linguistics.Core.Content;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Learn.Templates;

internal static class ObjectSpotlightRenderer
{
    public static Control Render(
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var word = TemplateRendering.Text(parameters, "word");
        var article = TemplateRendering.OptionalText(parameters, "article");
        var meaning = TemplateRendering.Localized(parameters, "meaning", instructionLanguage);
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var assetReference = TemplateRendering.AssetReference(parameters, "asset");
        var useVisual = !parameters.UseTextOnlyFallback && assetReference is not null;

        var replayButton = new Button
        {
            Content = "Replay scene",
            Classes = { "quiet" },
        };
        AutomationProperties.SetAutomationId(replayButton, "ObjectSpotlightReplay");
        var skipButton = new Button
        {
            Content = "Skip scene",
            Classes = { "quiet" },
        };
        AutomationProperties.SetAutomationId(skipButton, "ObjectSpotlightSkip");

        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 17,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Object spotlight instruction. {instruction}");
        var sceneActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
        };
        sceneActions.Children.Add(replayButton);
        sceneActions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(sceneActions, 1);
        header.Children.Add(sceneActions);

        var stage = TemplateRendering.CreateStage(292, $"Object spotlight for {article} {word}".Trim());
        TemplateRendering.AddBackdrop(stage, useVisual);

        var tape = new PaperTape { Content = "OBJECT SPOTLIGHT", Angle = -1.4 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.23);
        PaperStage.SetAnchorOffsetY(tape, -12);
        stage.Children.Add(tape);

        var subjectContent = (useVisual
                ? TemplateRendering.CreatePreviewImage(assetReference, 142)
                : null) as Control ??
            new TextBlock
            {
                Text = word,
                FontSize = 32,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
        var subject = new CutoutFrame
        {
            Width = useVisual ? 208 : 230,
            Height = 176,
            Content = subjectContent,
            RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        };
        subject.Classes.Add("tilt-left");
        PaperStage.SetLayer(subject, PaperStageLayer.Subject);
        PaperStage.SetAnchor(subject, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(subject, 0.39);
        stage.Children.Add(subject);

        var wordLine = string.IsNullOrWhiteSpace(article) ? word : $"{article} {word}";
        var revealCopy = new StackPanel { Spacing = 5 };
        revealCopy.Children.Add(new TextBlock
        {
            Text = wordLine,
            FontSize = 28,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap,
        });
        revealCopy.Children.Add(new TextBlock
        {
            Text = meaning,
            FontSize = 17,
            TextWrapping = TextWrapping.Wrap,
            Classes = { "muted" },
        });
        var revealCard = new PaperCard
        {
            Width = 238,
            Padding = new Thickness(18),
            Content = revealCopy,
        };
        PaperStage.SetLayer(revealCard, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(revealCard, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(revealCard, 0.72);
        PaperStage.SetAnchorOffsetY(revealCard, -8);
        PaperStage.SetLayerTransform(revealCard, TemplateRendering.Transform(0, 0, 1.2, 1));
        stage.Children.Add(revealCard);

        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var acknowledge = new Button
        {
            Content = "I noticed it",
            Classes = { "primary", "lift" },
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        AutomationProperties.SetAutomationId(acknowledge, "ObjectSpotlightAcknowledge");

        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        footer.Children.Add(outcomePanel);
        Grid.SetColumn(acknowledge, 1);
        footer.Children.Add(acknowledge);

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        root.Children.Add(stage);
        root.Children.Add(footer);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Insert(1, new TextBlock
            {
                Text = "Text-only presentation: the authored word, article, and meaning remain complete.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, tape, subject, revealCard);
            if (!shouldReduceMotion)
            {
                subject.RenderTransform = TemplateRendering.Transform(-44, 8, -4, 0.92);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(250), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(700), subject, 0, 0, -1.4, 1),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(700), revealCard),
            ]);
            await scene.PlayAsync(shouldReduceMotion);
        }

        root.AttachedToVisualTree += async (_, _) => await PlayAsync();
        root.DetachedFromVisualTree += (_, _) =>
        {
            scene?.Skip();
            scene?.Dispose();
            scene = null;
        };
        skipButton.Click += (_, _) =>
        {
            scene?.Skip();
            tape.SkipEntrance();
        };
        replayButton.Click += async (_, _) => await PlayAsync();
        acknowledge.Click += (_, _) =>
        {
            var outcome = TemplateInteractionEvaluator.EvaluateAcknowledgement(acknowledged: true);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            reportOutcome(outcome);
        };
        return root;
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Seen: the word, article, and meaning are together.",
        TemplateOutcomeState.Uncertain => "Pause on the article and meaning before moving on.",
        TemplateOutcomeState.Failure => "Replay the reveal and look at the complete noun entry.",
        _ => "Watch the cutout settle, then notice the complete noun entry.",
    };
}
