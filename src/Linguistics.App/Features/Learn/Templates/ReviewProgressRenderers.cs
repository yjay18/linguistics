using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Linguistics.App.Content;
using Linguistics.App.Controls;
using Linguistics.App.Motion;
using Linguistics.Core.Content;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Learn.Templates;

internal static class ReviewFlashRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var prompt = TemplateRendering.Text(parameters, "prompt");
        var answer = TemplateRendering.Text(parameters, "answer");
        var details = TemplateRendering.Options(parameters, "details");
        var ratings = TemplateRendering.Options(parameters, "ratings");
        var configurationVersion = TemplateRendering.Text(parameters, "configuration-version");

        var replayButton = new Button { Content = "Replay card", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "ReviewFlashReplay");
        AutomationProperties.SetName(replayButton, "Reset and replay the review card entrance");
        var skipButton = new Button { Content = "Skip entrance", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "ReviewFlashSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed review card entrance");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Review instruction. {instruction}");
        var headerActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        headerActions.Children.Add(replayButton);
        headerActions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(headerActions, 1);
        header.Children.Add(headerActions);

        var stage = TemplateRendering.CreateStage(
            304,
            $"Review recall card. Prompt: {prompt}. Reveal the answer before rating recall.");
        TemplateRendering.AddBackdrop(stage, imageCache, assetReferenceId: null);
        var deckShadow = new PaperCard
        {
            Width = 472,
            Height = 196,
            Opacity = 0.56,
            IsHitTestVisible = false,
        };
        deckShadow.Classes.Add("soft");
        PaperStage.SetLayer(deckShadow, PaperStageLayer.AmbientPieces);
        PaperStage.SetAnchor(deckShadow, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(deckShadow, 0.51);
        PaperStage.SetAnchorOffsetY(deckShadow, -39);
        stage.Children.Add(deckShadow);

        var frontCard = new PaperCard
        {
            Width = 480,
            Height = 202,
            Padding = new Thickness(24, 20),
            Content = new StackPanel
            {
                Spacing = 18,
                Children =
                {
                    new PaperTape
                    {
                        Content = "RECALL",
                        Angle = -1.6,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Classes = { "compact" },
                    },
                    new TextBlock
                    {
                        Text = prompt,
                        FontSize = 25,
                        FontWeight = FontWeight.Bold,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                    },
                },
            },
        };
        frontCard.Classes.Add("accent");
        AutomationProperties.SetAutomationId(frontCard, "ReviewFlashFront");
        AutomationProperties.SetName(frontCard, $"Review card front. Recall: {prompt}");
        PaperStage.SetLayer(frontCard, PaperStageLayer.Subject);
        PaperStage.SetAnchor(frontCard, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(frontCard, 0.5);
        PaperStage.SetAnchorOffsetY(frontCard, -44);
        stage.Children.Add(frontCard);

        var detailList = new StackPanel { Spacing = 5 };
        foreach (var detail in details)
        {
            detailList.Children.Add(new TextBlock
            {
                Text = detail.Label,
                FontSize = 13,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            });
        }

        var backCard = new PaperCard
        {
            Width = 480,
            Height = 220,
            Padding = new Thickness(24, 17),
            IsVisible = false,
            Content = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new PaperTape
                    {
                        Content = "REVIEWED ANSWER",
                        Angle = 1.2,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Classes = { "compact" },
                    },
                    new TextBlock
                    {
                        Text = answer,
                        FontSize = 24,
                        FontWeight = FontWeight.Bold,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    detailList,
                },
            },
        };
        backCard.Classes.Add("soft");
        AutomationProperties.SetAutomationId(backCard, "ReviewFlashBack");
        AutomationProperties.SetName(
            backCard,
            $"Reviewed answer. {answer}. {string.Join(" ", details.Select(detail => detail.Label))}");
        PaperStage.SetLayer(backCard, PaperStageLayer.Subject);
        PaperStage.SetAnchor(backCard, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(backCard, 0.5);
        PaperStage.SetAnchorOffsetY(backCard, -34);
        stage.Children.Add(backCard);

        var recallStatus = new TextBlock
        {
            Text = "Answer hidden. Recall before revealing.",
            FontSize = 13,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Classes = { "muted" },
        };
        AutomationProperties.SetAutomationId(recallStatus, "ReviewFlashStatus");
        AutomationProperties.SetName(recallStatus, "Review card status");
        AutomationProperties.SetLiveSetting(recallStatus, AutomationLiveSetting.Polite);

        var revealButton = new Button
        {
            Content = "Reveal reviewed answer",
            HorizontalAlignment = HorizontalAlignment.Left,
            Classes = { "primary" },
        };
        AutomationProperties.SetAutomationId(revealButton, "ReviewFlashReveal");
        AutomationProperties.SetName(revealButton, "Reveal the reviewed answer and rating choices");

        var ratingButtons = new List<Button>();
        var ratingPanel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            IsVisible = false,
        };
        AutomationProperties.SetAutomationId(ratingPanel, "ReviewFlashRatings");
        AutomationProperties.SetName(ratingPanel, "Rate recall as Again, Hard, Good, or Easy");
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        foreach (var rating in ratings)
        {
            var button = new Button
            {
                Content = rating.Label,
                Margin = new Thickness(0, 0, 8, 8),
                IsEnabled = false,
                Classes = { rating.Id is "good" or "easy" ? "primary" : "quiet" },
            };
            AutomationProperties.SetAutomationId(button, $"ReviewFlashRating_{rating.Id}");
            AutomationProperties.SetName(button, $"Rate recall {rating.Label}");
            button.Click += (_, _) =>
            {
                var outcome = TemplateInteractionEvaluator.EvaluateReviewRating(ratings, rating.Id);
                recallStatus.Text = $"Recall rated {rating.Label}.";
                TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
                reportOutcome(outcome);
            };
            ratingButtons.Add(button);
            ratingPanel.Children.Add(button);
        }

        var configurationNote = new PaperCard
        {
            Padding = new Thickness(14, 10),
            Content = new TextBlock
            {
                Text = $"Review rule set: {configurationVersion}. This card reports one rating; the deterministic review flow schedules it.",
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
            },
        };
        configurationNote.Classes.Add("soft");
        AutomationProperties.SetAutomationId(configurationNote, "ReviewFlashConfiguration");
        AutomationProperties.SetName(
            configurationNote,
            $"Review configuration {configurationVersion}. Rating only; no scheduling occurs in this card.");

        var controls = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 12 };
        controls.Children.Add(revealButton);
        Grid.SetColumn(recallStatus, 1);
        controls.Children.Add(recallStatus);
        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 12 };
        footer.Children.Add(ratingPanel);
        Grid.SetColumn(outcomePanel, 1);
        footer.Children.Add(outcomePanel);

        void ResetRecall()
        {
            frontCard.IsVisible = true;
            backCard.IsVisible = false;
            revealButton.IsVisible = true;
            ratingPanel.IsVisible = false;
            foreach (var button in ratingButtons)
            {
                button.IsEnabled = false;
            }

            recallStatus.Text = "Answer hidden. Recall before revealing.";
            TemplateRendering.ApplyOutcome(
                outcomePanel,
                outcomeText,
                parameters.PreviewOutcome,
                OutcomeCopy);
        }

        revealButton.Click += (_, _) =>
        {
            frontCard.IsVisible = false;
            backCard.IsVisible = true;
            revealButton.IsVisible = false;
            ratingPanel.IsVisible = true;
            foreach (var button in ratingButtons)
            {
                button.IsEnabled = true;
            }

            recallStatus.Text = $"Reviewed answer revealed. {answer}";
        };

        var root = new StackPanel { Spacing = 14 };
        root.Children.Add(header);
        root.Children.Add(stage);
        root.Children.Add(controls);
        root.Children.Add(configurationNote);
        root.Children.Add(footer);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            ResetRecall();
            TemplateRendering.Prepare(
                shouldReduceMotion,
                deckShadow,
                frontCard,
                controls,
                configurationNote,
                footer);
            if (!shouldReduceMotion)
            {
                frontCard.RenderTransform = TemplateRendering.Transform(0, 16, -4, 0.95);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(140), deckShadow),
                TemplateRendering.Move(
                    TimeSpan.FromMilliseconds(300),
                    frontCard,
                    translateX: 0,
                    translateY: 0,
                    angle: -1.2,
                    scale: 1),
                TemplateRendering.Reveal(
                    TimeSpan.FromMilliseconds(180),
                    controls,
                    configurationNote,
                    footer),
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
        replayButton.Click += async (_, _) => await PlayAsync();
        skipButton.Click += (_, _) => scene?.Skip();
        return root;
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Good or Easy selected. Scheduling follows the existing review rules.",
        TemplateOutcomeState.Uncertain => "Recall is pending or Hard selected. Scheduling follows the existing review rules.",
        TemplateOutcomeState.Failure => "Again selected. Scheduling follows the existing review rules.",
        _ => "Ready: reveal only after you have recalled the answer.",
    };
}
