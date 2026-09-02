using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Linguistics.App.Content;
using Linguistics.App.Controls;
using Linguistics.App.Localization;
using Linguistics.App.Motion;
using Linguistics.Core.Content;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Learn.Templates;

internal static class ObjectSpotlightRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
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
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var subjectImage = parameters.UseTextOnlyFallback
            ? null
            : TemplateRendering.CreateContentImage(imageCache, assetReference, 142);
        var useVisual = subjectImage is not null;

        var replayButton = new Button
        {
            Content = AppStrings.Get("Template_ReplayScene"),
            Classes = { "quiet" },
        };
        AutomationProperties.SetAutomationId(replayButton, "ObjectSpotlightReplay");
        var skipButton = new Button
        {
            Content = AppStrings.Get("Template_SkipScene"),
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
        AutomationProperties.SetName(
            instructionText,
            AppStrings.Format("Template_ObjectSpotlight_Instruction", instruction));
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

        var stage = TemplateRendering.CreateStage(
            292,
            AppStrings.Format(
                "Template_ObjectSpotlight_Stage",
                $"{article} {word}".Trim()));
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);

        var tape = new PaperTape
        {
            Content = AppStrings.Get("Template_ObjectSpotlight_Tape"),
            Angle = -1.4,
        };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.23);
        PaperStage.SetAnchorOffsetY(tape, -12);
        stage.Children.Add(tape);

        var subjectContent = subjectImage as Control ??
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

        var wordCard = new PaperCard
        {
            Width = 218,
            Padding = new Thickness(14, 10),
            Content = new TextBlock
            {
                Text = word,
                FontSize = 28,
                FontWeight = FontWeight.Bold,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
            },
        };
        PaperStage.SetLayer(wordCard, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(wordCard, PaperAnchorLine.Shoulder);
        PaperStage.SetAnchorX(wordCard, 0.72);
        PaperStage.SetAnchorOffsetY(wordCard, 8);
        PaperStage.SetLayerTransform(wordCard, TemplateRendering.Transform(0, 0, 1.1, 1));
        stage.Children.Add(wordCard);

        PaperStamp? articleStamp = null;
        if (!string.IsNullOrWhiteSpace(article))
        {
            articleStamp = new PaperStamp
            {
                Content = article.ToUpperInvariant(),
                Angle = -2.4,
            };
            PaperStage.SetLayer(articleStamp, PaperStageLayer.VerdictCard);
            PaperStage.SetAnchor(articleStamp, PaperAnchorLine.Waist);
            PaperStage.SetAnchorX(articleStamp, 0.64);
            PaperStage.SetAnchorOffsetY(articleStamp, -16);
            stage.Children.Add(articleStamp);
        }

        var meaningCard = new PaperCard
        {
            Width = 218,
            Padding = new Thickness(14, 10),
            Content = new TextBlock
            {
                Text = meaning,
                FontSize = 17,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Classes = { "muted" },
            },
        };
        PaperStage.SetLayer(meaningCard, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(meaningCard, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(meaningCard, 0.72);
        PaperStage.SetAnchorOffsetY(meaningCard, -58);
        PaperStage.SetLayerTransform(meaningCard, TemplateRendering.Transform(0, 0, -0.8, 1));
        stage.Children.Add(meaningCard);

        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var acknowledge = new Button
        {
            Content = AppStrings.Get("Template_ObjectSpotlight_Acknowledge"),
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
        if (!parameters.UseTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                [useVisual ? assetReference : null, backdropRendered ? backdropReference : null],
                "ObjectSpotlightImageCredits") is { } credits)
        {
            root.Children.Add(credits);
        }

        root.Children.Add(footer);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Insert(1, new TextBlock
            {
                Text = AppStrings.Get("Template_ObjectSpotlight_TextOnly"),
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            var revealControls = articleStamp is null
                ? new Control[] { tape, subject, wordCard, meaningCard }
                : [tape, subject, wordCard, articleStamp, meaningCard];
            TemplateRendering.Prepare(shouldReduceMotion, revealControls);
            if (!shouldReduceMotion)
            {
                subject.RenderTransform = TemplateRendering.Transform(-44, 8, -4, 0.92);
            }

            var steps = new List<PaperChoreographyStep>
            {
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(250), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(550), subject, 0, 0, -1.4, 1),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(250), wordCard),
            };
            if (articleStamp is not null)
            {
                steps.Add(TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), articleStamp));
            }

            steps.Add(TemplateRendering.Reveal(TimeSpan.FromMilliseconds(350), meaningCard));
            scene = new PaperChoreography(steps);
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
            articleStamp?.SkipEntrance();
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
        TemplateOutcomeState.Success => AppStrings.Get("Template_ObjectSpotlight_Success"),
        TemplateOutcomeState.Uncertain => AppStrings.Get("Template_ObjectSpotlight_Uncertain"),
        TemplateOutcomeState.Failure => AppStrings.Get("Template_ObjectSpotlight_Failure"),
        _ => AppStrings.Get("Template_ObjectSpotlight_Ready"),
    };
}
