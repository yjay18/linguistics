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

internal static class RecapScrapbookRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var title = TemplateRendering.Text(parameters, "title");
        var pieces = TemplateRendering.Options(parameters, "pieces");
        var closing = TemplateRendering.Text(parameters, "closing");
        var actions = TemplateRendering.Options(parameters, "actions");
        var acknowledgementId = TemplateRendering.Text(parameters, "acknowledgement");

        var replayButton = new Button { Content = "Replay assembly", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "RecapScrapbookReplay");
        AutomationProperties.SetName(replayButton, "Replay the scrapbook assembly");
        var skipButton = new Button { Content = "Skip assembly", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "RecapScrapbookSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed scrapbook spread");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Recap instruction. {instruction}");
        var headerActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        headerActions.Children.Add(replayButton);
        headerActions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(headerActions, 1);
        header.Children.Add(headerActions);

        var stage = TemplateRendering.CreateStage(
            372,
            $"Scrapbook recap titled {title}. Ordered pieces: {string.Join(" ", pieces.Select(piece => piece.Label))}");
        TemplateRendering.AddBackdrop(stage, imageCache, assetReferenceId: null);
        var leftPieces = new StackPanel { Spacing = 9 };
        var rightPieces = new StackPanel { Spacing = 9 };
        var pieceCards = new List<Control>();
        var midpoint = (pieces.Count + 1) / 2;
        for (var index = 0; index < pieces.Count; index++)
        {
            var piece = pieces[index];
            var card = CreatePiece(piece, index);
            pieceCards.Add(card);
            (index < midpoint ? leftPieces : rightPieces).Children.Add(card);
        }

        var leftPage = CreatePage("LESSON PIECES", leftPieces, "RecapScrapbookLeftPage");
        var rightPage = CreatePage("KEEP TOGETHER", rightPieces, "RecapScrapbookRightPage");
        var binding = new Border
        {
            Width = 14,
            Margin = new Thickness(8, 4),
            IsHitTestVisible = false,
            Classes = { "soft-card" },
        };
        AutomationProperties.SetAutomationId(binding, "RecapScrapbookBinding");
        AutomationProperties.SetName(binding, "Paper scrapbook binding");
        var spread = new Grid
        {
            Width = 700,
            Height = 286,
            ColumnDefinitions = new ColumnDefinitions("*,32,*"),
        };
        spread.Children.Add(leftPage);
        Grid.SetColumn(binding, 1);
        spread.Children.Add(binding);
        Grid.SetColumn(rightPage, 2);
        spread.Children.Add(rightPage);
        AutomationProperties.SetAutomationId(spread, "RecapScrapbookSpread");
        AutomationProperties.SetName(spread, $"Open scrapbook spread. {pieces.Count} ordered lesson pieces.");
        PaperStage.SetLayer(spread, PaperStageLayer.Subject);
        PaperStage.SetAnchor(spread, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(spread, 0.5);
        PaperStage.SetAnchorOffsetY(spread, -39);
        stage.Children.Add(spread);

        var closingTape = new PaperTape
        {
            Content = closing,
            Angle = -1.1,
            MaxWidth = 620,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Classes = { "compact" },
        };
        AutomationProperties.SetAutomationId(closingTape, "RecapScrapbookClosing");
        AutomationProperties.SetName(closingTape, $"Recap closing note. {closing}");
        PaperStage.SetLayer(closingTape, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(closingTape, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(closingTape, 0.5);
        PaperStage.SetAnchorOffsetY(closingTape, -4);
        stage.Children.Add(closingTape);

        var modeText = new TextBlock
        {
            Text = parameters.UseTextOnlyFallback
                ? "Text-only recap is active. Every ordered lesson piece remains complete."
                : "This text-led scrapbook keeps every lesson piece available without decorative imagery.",
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Classes = { "muted" },
        };
        AutomationProperties.SetAutomationId(modeText, "RecapScrapbookTextEquivalent");
        AutomationProperties.SetName(modeText, "Complete text equivalent for the recap spread");

        var status = new TextBlock
        {
            Text = "Scrapbook spread assembled in authored order.",
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Classes = { "muted" },
        };
        AutomationProperties.SetAutomationId(status, "RecapScrapbookStatus");
        AutomationProperties.SetName(status, "Scrapbook recap status");
        AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var actionPanel = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var action in actions)
        {
            var button = new Button
            {
                Content = action.Label,
                Margin = new Thickness(0, 0, 8, 8),
                Classes =
                {
                    string.Equals(action.Id, acknowledgementId, StringComparison.Ordinal)
                        ? "primary"
                        : "quiet",
                },
            };
            AutomationProperties.SetAutomationId(button, $"RecapScrapbookAction_{action.Id}");
            AutomationProperties.SetName(button, action.Label);
            button.Click += (_, _) =>
            {
                var outcome = TemplateInteractionEvaluator.EvaluateAdvisoryChoice(
                    actions,
                    acknowledgementId,
                    action.Id);
                status.Text = outcome.State == TemplateOutcomeState.Success
                    ? "Recap acknowledged."
                    : "Scrapbook remains open.";
                TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
                reportOutcome(outcome);
            };
            actionPanel.Children.Add(button);
        }

        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 12 };
        footer.Children.Add(actionPanel);
        Grid.SetColumn(outcomePanel, 1);
        footer.Children.Add(outcomePanel);
        var root = new StackPanel { Spacing = 14 };
        root.Children.Add(header);
        root.Children.Add(stage);
        root.Children.Add(modeText);
        root.Children.Add(status);
        root.Children.Add(footer);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            status.Text = "Scrapbook spread assembled in authored order.";
            TemplateRendering.ApplyOutcome(
                outcomePanel,
                outcomeText,
                parameters.PreviewOutcome,
                OutcomeCopy);
            TemplateRendering.Prepare(
                shouldReduceMotion,
                [
                    leftPage,
                    binding,
                    rightPage,
                    closingTape,
                    modeText,
                    status,
                    footer,
                    .. pieceCards,
                ]);
            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(180), leftPage),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(100), binding),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(180), rightPage),
                TemplateRendering.Reveal(
                    TimeSpan.FromMilliseconds(220),
                    [.. pieceCards.Take(midpoint)]),
                TemplateRendering.Reveal(
                    TimeSpan.FromMilliseconds(220),
                    [.. pieceCards.Skip(midpoint)]),
                TemplateRendering.Reveal(
                    TimeSpan.FromMilliseconds(220),
                    closingTape,
                    modeText,
                    status,
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

    private static PaperCard CreatePage(string label, Control pieces, string automationId)
    {
        var page = new PaperCard
        {
            Padding = new Thickness(16, 14),
            Content = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new PaperTape
                    {
                        Content = label,
                        Angle = label == "LESSON PIECES" ? -1 : 1,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Classes = { "compact" },
                    },
                    pieces,
                },
            },
        };
        page.Classes.Add("settings-sheet");
        AutomationProperties.SetAutomationId(page, automationId);
        AutomationProperties.SetName(page, label);
        return page;
    }

    private static PaperCard CreatePiece(TemplateOption piece, int index)
    {
        var card = new PaperCard
        {
            Padding = new Thickness(10, 8),
            Content = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                ColumnSpacing = 9,
                Children =
                {
                    new PaperTape
                    {
                        Content = (index + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture),
                        Angle = index % 2 == 0 ? -1.2 : 1.1,
                        Classes = { "compact" },
                    },
                    new TextBlock
                    {
                        Text = piece.Label,
                        FontSize = 13,
                        FontWeight = FontWeight.SemiBold,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                },
            },
        };
        card.Classes.Add(index % 2 == 0 ? "tilt-left" : "tilt-right");
        var text = ((Grid)card.Content).Children[1];
        Grid.SetColumn(text, 1);
        AutomationProperties.SetAutomationId(card, $"RecapScrapbookPiece_{piece.Id}");
        AutomationProperties.SetName(card, $"Lesson piece {index + 1}. {piece.Label}");
        return card;
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Recap complete. The authored lesson pieces remain together.",
        TemplateOutcomeState.Uncertain => "Pause on any lesson piece that still needs attention.",
        TemplateOutcomeState.Failure => "Reopen the spread and review each lesson piece in order.",
        _ => "Ready: review the assembled lesson pieces.",
    };
}

internal static class UnitCapstoneRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var unitLabel = TemplateRendering.Text(parameters, "unit-label");
        var goal = TemplateRendering.Text(parameters, "goal");
        var steps = TemplateRendering.Options(parameters, "steps");
        var templateChain = TemplateRendering.Options(parameters, "template-chain");
        var backdropAssetId = TemplateRendering.AssetReference(parameters, "backdrop");
        _ = TemplateInteractionEvaluator.EvaluateCapstoneStep(steps, templateChain, [], null);

        var replayButton = new Button { Content = "Replay mission", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "UnitCapstoneReplay");
        AutomationProperties.SetName(replayButton, "Replay the mission board entrance");
        var skipButton = new Button { Content = "Skip entrance", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "UnitCapstoneSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed mission board entrance");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Capstone instruction. {instruction}");
        var headerActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        headerActions.Children.Add(replayButton);
        headerActions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(headerActions, 1);
        header.Children.Add(headerActions);

        var stage = TemplateRendering.CreateStage(
            422,
            $"{unitLabel}. Mission goal: {goal}. Complete {steps.Count} authored activities in order.");
        var hasBackdrop = !parameters.UseTextOnlyFallback &&
                          TemplateRendering.AddBackdrop(stage, imageCache, backdropAssetId);
        if (parameters.UseTextOnlyFallback)
        {
            TemplateRendering.AddBackdrop(stage, imageCache: null, assetReferenceId: null);
        }

        var missionStamp = new PaperTape
        {
            Content = unitLabel.ToUpperInvariant(),
            Angle = -1.8,
            Classes = { "compact" },
        };
        AutomationProperties.SetAutomationId(missionStamp, "UnitCapstoneLabel");
        AutomationProperties.SetName(missionStamp, unitLabel);
        PaperStage.SetLayer(missionStamp, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(missionStamp, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(missionStamp, 0.22);
        PaperStage.SetAnchorOffsetY(missionStamp, 9);
        stage.Children.Add(missionStamp);

        var goalCard = new PaperCard
        {
            Width = 620,
            Padding = new Thickness(18, 14),
            Content = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock
                    {
                        Text = "ONE MISSION GOAL",
                        FontSize = 11,
                        FontWeight = FontWeight.Bold,
                    },
                    new TextBlock
                    {
                        Text = goal,
                        FontSize = 18,
                        FontWeight = FontWeight.SemiBold,
                        TextWrapping = TextWrapping.Wrap,
                    },
                },
            },
        };
        goalCard.Classes.Add("settings-sheet");
        AutomationProperties.SetAutomationId(goalCard, "UnitCapstoneGoal");
        AutomationProperties.SetName(goalCard, $"Mission goal. {goal}");
        PaperStage.SetLayer(goalCard, PaperStageLayer.AmbientPieces);
        PaperStage.SetAnchor(goalCard, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(goalCard, 0.54);
        PaperStage.SetAnchorOffsetY(goalCard, 34);
        stage.Children.Add(goalCard);

        var stepCards = new List<PaperCard>();
        var stepButtons = new List<Button>();
        var stepStatuses = new List<TextBlock>();
        var route = new WrapPanel
        {
            Width = 700,
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            var templateSurface = templateChain[index].Label.Replace('-', ' ').ToUpperInvariant();
            var statusLabel = new TextBlock
            {
                Text = index == 0 ? "CURRENT" : "WAITING",
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Classes = { "muted" },
            };
            var completeButton = new Button
            {
                Content = index == 0 ? "Complete this step" : "Waiting",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                IsEnabled = index == 0,
                Classes = { index == 0 ? "primary" : "quiet" },
            };
            AutomationProperties.SetAutomationId(completeButton, $"UnitCapstoneStep_{step.Id}");
            var card = new PaperCard
            {
                Width = 218,
                Height = 208,
                Margin = new Thickness(7),
                Padding = new Thickness(14, 12),
                Content = new StackPanel
                {
                    Spacing = 9,
                    Children =
                    {
                        new PaperTape
                        {
                            Content = $"STEP {index + 1}",
                            Angle = index % 2 == 0 ? -1.2 : 1.1,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Classes = { "compact" },
                        },
                        new TextBlock
                        {
                            Text = step.Label,
                            FontSize = 14,
                            FontWeight = FontWeight.SemiBold,
                            TextWrapping = TextWrapping.Wrap,
                        },
                        new TextBlock
                        {
                            Text = templateSurface,
                            FontSize = 10,
                            TextWrapping = TextWrapping.Wrap,
                            Classes = { "muted" },
                        },
                        statusLabel,
                        completeButton,
                    },
                },
            };
            card.Classes.Add(index == 0 ? "settings-sheet" : "soft");
            AutomationProperties.SetAutomationId(card, $"UnitCapstoneCard_{step.Id}");
            AutomationProperties.SetName(
                card,
                $"Mission step {index + 1}. {step.Label}. Template surface {templateChain[index].Label}.");
            stepCards.Add(card);
            stepButtons.Add(completeButton);
            stepStatuses.Add(statusLabel);
            route.Children.Add(card);
        }

        AutomationProperties.SetAutomationId(route, "UnitCapstoneRoute");
        AutomationProperties.SetName(route, "Ordered capstone activity chain");
        PaperStage.SetLayer(route, PaperStageLayer.Subject);
        PaperStage.SetAnchor(route, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(route, 0.5);
        PaperStage.SetAnchorOffsetY(route, -14);
        stage.Children.Add(route);

        var modeText = new TextBlock
        {
            Text = parameters.UseTextOnlyFallback
                ? "Text-only mission mode is active. Goal, activity order, and controls remain complete."
                : hasBackdrop
                    ? "Validated local backdrop active. Every mission step remains fully text labelled."
                    : "Authored paper mission board active. Every activity remains fully text labelled.",
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Classes = { "muted" },
        };
        AutomationProperties.SetAutomationId(modeText, "UnitCapstoneTextEquivalent");

        var status = new TextBlock
        {
            Text = $"Mission ready. Start with {steps[0].Label}",
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Classes = { "muted" },
        };
        AutomationProperties.SetAutomationId(status, "UnitCapstoneStatus");
        AutomationProperties.SetName(status, "Unit mission progress");
        AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        IReadOnlyList<string> completedStepIds = [];

        void RefreshStepState()
        {
            for (var index = 0; index < steps.Count; index++)
            {
                var isComplete = index < completedStepIds.Count;
                var isCurrent = index == completedStepIds.Count && completedStepIds.Count < steps.Count;
                var card = stepCards[index];
                card.Classes.Remove("settings-sheet");
                card.Classes.Remove("soft");
                card.Classes.Add(isComplete || isCurrent ? "settings-sheet" : "soft");
                stepStatuses[index].Text = isComplete ? "COMPLETE" : isCurrent ? "CURRENT" : "WAITING";
                stepButtons[index].Content = isComplete
                    ? "Step complete"
                    : isCurrent
                        ? "Complete this step"
                        : "Waiting";
                stepButtons[index].IsEnabled = isCurrent;
                AutomationProperties.SetName(
                    stepButtons[index],
                    isComplete
                        ? $"Step {index + 1} complete. {steps[index].Label}"
                        : isCurrent
                            ? $"Complete step {index + 1}. {steps[index].Label}"
                            : $"Step {index + 1} waits for earlier activities. {steps[index].Label}");
            }
        }

        for (var index = 0; index < stepButtons.Count; index++)
        {
            var selectedIndex = index;
            stepButtons[index].Click += (_, _) =>
            {
                var selected = steps[selectedIndex];
                var outcome = TemplateInteractionEvaluator.EvaluateCapstoneStep(
                    steps,
                    templateChain,
                    completedStepIds,
                    selected.Id);
                if (outcome.State != TemplateOutcomeState.Failure)
                {
                    completedStepIds = outcome.OrderedOptionIds ?? completedStepIds;
                    RefreshStepState();
                }

                status.Text = outcome.State switch
                {
                    TemplateOutcomeState.Success => "Mission chain complete.",
                    TemplateOutcomeState.Failure => "That activity is out of order. Continue from the current card.",
                    _ => $"Step {completedStepIds.Count} complete. Continue with {steps[completedStepIds.Count].Label}",
                };
                TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
                reportOutcome(outcome);
            };
        }

        RefreshStepState();
        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 12 };
        footer.Children.Add(status);
        Grid.SetColumn(outcomePanel, 1);
        footer.Children.Add(outcomePanel);
        var root = new StackPanel { Spacing = 14 };
        root.Children.Add(header);
        root.Children.Add(stage);
        root.Children.Add(modeText);
        root.Children.Add(footer);
        if (!parameters.UseTextOnlyFallback && hasBackdrop &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                [backdropAssetId],
                "UnitCapstoneImageCredits") is { } credits)
        {
            root.Children.Add(credits);
        }

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            var midpoint = (stepCards.Count + 1) / 2;
            TemplateRendering.Prepare(
                shouldReduceMotion,
                [missionStamp, goalCard, modeText, footer, .. stepCards]);
            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(160), missionStamp, goalCard),
                TemplateRendering.Reveal(
                    TimeSpan.FromMilliseconds(260),
                    [.. stepCards.Take(midpoint)]),
                TemplateRendering.Reveal(
                    TimeSpan.FromMilliseconds(260),
                    [.. stepCards.Skip(midpoint)]),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(180), modeText, footer),
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
        TemplateOutcomeState.Success => "Mission complete. Every authored activity is finished in order.",
        TemplateOutcomeState.Uncertain => "Mission in progress. Continue with the next authored activity.",
        TemplateOutcomeState.Failure => "That activity is out of order. Return to the current mission card.",
        _ => "Ready: begin with the first mission activity.",
    };
}

internal static class ProgressShelfRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var title = TemplateRendering.Text(parameters, "title");
        var demonstrated = TemplateRendering.OptionalOptions(parameters, "demonstrated");
        var practicing = TemplateRendering.OptionalOptions(parameters, "practicing");
        var notStarted = TemplateRendering.OptionalOptions(parameters, "not-started");
        var emptyCopy = TemplateRendering.Text(parameters, "empty-copy");
        var methodNote = TemplateRendering.Text(parameters, "method-note");
        _ = TemplateInteractionEvaluator.EvaluateCapabilitySelection(
            demonstrated,
            practicing,
            notStarted,
            null);
        var projected = demonstrated
            .Select(capability => new ProjectedCapability(capability, ShelfCapabilityStatus.Demonstrated))
            .Concat(practicing.Select(capability =>
                new ProjectedCapability(capability, ShelfCapabilityStatus.Practicing)))
            .Concat(notStarted.Select(capability =>
                new ProjectedCapability(capability, ShelfCapabilityStatus.NotStarted)))
            .ToArray();

        var replayButton = new Button { Content = "Replay shelf", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "ProgressShelfReplay");
        AutomationProperties.SetName(replayButton, "Replay the capability shelf entrance");
        var skipButton = new Button { Content = "Skip entrance", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "ProgressShelfSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed capability shelf");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Progress instruction. {instruction}");
        var headerActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        headerActions.Children.Add(replayButton);
        headerActions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(headerActions, 1);
        header.Children.Add(headerActions);

        var selectedText = new TextBlock
        {
            Text = projected.Length == 0
                ? emptyCopy
                : "Select a paper situation to inspect its projected status.",
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(selectedText, "ProgressShelfSelectionStatus");
        AutomationProperties.SetName(selectedText, "Selected capability status");
        AutomationProperties.SetLiveSetting(selectedText, AutomationLiveSetting.Polite);
        var detailCard = new PaperCard
        {
            Padding = new Thickness(16, 12),
            Content = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock
                    {
                        Text = "SELECTED SITUATION",
                        FontSize = 11,
                        FontWeight = FontWeight.Bold,
                    },
                    selectedText,
                },
            },
        };
        detailCard.Classes.Add("settings-sheet");
        AutomationProperties.SetAutomationId(detailCard, "ProgressShelfDetail");
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);

        var stage = TemplateRendering.CreateStage(
            402,
            $"{title}. {demonstrated.Count} demonstrated, {practicing.Count} practicing, " +
            $"and {notStarted.Count} not started situations are projected.");
        TemplateRendering.AddBackdrop(stage, imageCache, assetReferenceId: null);
        var shelfHeader = new PaperTape
        {
            Content = title.ToUpperInvariant(),
            Angle = -1.4,
            MaxWidth = 600,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Classes = { "compact" },
        };
        AutomationProperties.SetAutomationId(shelfHeader, "ProgressShelfTitle");
        AutomationProperties.SetName(shelfHeader, title);
        PaperStage.SetLayer(shelfHeader, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(shelfHeader, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(shelfHeader, 0.5);
        PaperStage.SetAnchorOffsetY(shelfHeader, 7);
        stage.Children.Add(shelfHeader);

        var shelfLine = new Border
        {
            Width = 720,
            Height = 18,
            IsHitTestVisible = false,
            Classes = { "soft-card" },
        };
        AutomationProperties.SetAutomationId(shelfLine, "ProgressShelfBoard");
        AutomationProperties.SetName(shelfLine, "Paper capability shelf");
        PaperStage.SetLayer(shelfLine, PaperStageLayer.ForegroundSilhouettes);
        PaperStage.SetAnchor(shelfLine, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(shelfLine, 0.5);
        PaperStage.SetAnchorOffsetY(shelfLine, -3);
        stage.Children.Add(shelfLine);

        var objectsPanel = new WrapPanel
        {
            Width = 720,
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        AutomationProperties.SetAutomationId(objectsPanel, "ProgressShelfObjects");
        AutomationProperties.SetName(objectsPanel, "Projected capability objects in status order");
        var shelfObjects = new List<Control>();
        if (projected.Length == 0)
        {
            var emptyCard = new PaperCard
            {
                Width = 620,
                Margin = new Thickness(10),
                Padding = new Thickness(22, 18),
                Content = new StackPanel
                {
                    Spacing = 9,
                    Children =
                    {
                        new PaperTape
                        {
                            Content = "SHELF READY",
                            Angle = 1.1,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Classes = { "compact" },
                        },
                        new TextBlock
                        {
                            Text = emptyCopy,
                            FontSize = 16,
                            FontWeight = FontWeight.SemiBold,
                            TextWrapping = TextWrapping.Wrap,
                        },
                    },
                },
            };
            emptyCard.Classes.Add("soft");
            AutomationProperties.SetAutomationId(emptyCard, "ProgressShelfEmpty");
            AutomationProperties.SetName(emptyCard, $"Empty capability shelf. {emptyCopy}");
            shelfObjects.Add(emptyCard);
            objectsPanel.Children.Add(emptyCard);
        }
        else
        {
            for (var index = 0; index < projected.Length; index++)
            {
                var projection = projected[index];
                var statusLabel = StatusLabel(projection.Status);
                var selectButton = new Button
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Classes = { "quiet" },
                    Content = new StackPanel
                    {
                        Spacing = 12,
                        Children =
                        {
                            new PaperStamp
                            {
                                Content = statusLabel,
                                Angle = index % 2 == 0 ? -2 : 1.6,
                                HorizontalAlignment = HorizontalAlignment.Left,
                                Classes = { "compact" },
                            },
                            new TextBlock
                            {
                                Text = projection.Capability.Label,
                                FontSize = 16,
                                FontWeight = FontWeight.Bold,
                                TextWrapping = TextWrapping.Wrap,
                            },
                            new TextBlock
                            {
                                Text = "Inspect situation",
                                FontSize = 12,
                                Classes = { "muted" },
                            },
                        },
                    },
                };
                AutomationProperties.SetAutomationId(
                    selectButton,
                    $"ProgressShelfCapability_{projection.Capability.Id}");
                AutomationProperties.SetName(
                    selectButton,
                    $"{projection.Capability.Label}. {StatusDescription(projection.Status)} Select for details.");
                selectButton.Click += (_, _) =>
                {
                    var outcome = TemplateInteractionEvaluator.EvaluateCapabilitySelection(
                        demonstrated,
                        practicing,
                        notStarted,
                        projection.Capability.Id);
                    selectedText.Text =
                        $"{projection.Capability.Label}. {StatusDescription(projection.Status)}";
                    TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
                    reportOutcome(outcome);
                };
                var frame = new CutoutFrame
                {
                    Width = 224,
                    Height = 238,
                    Margin = new Thickness(8),
                    Padding = new Thickness(7),
                    Content = selectButton,
                };
                frame.Classes.Add(index % 2 == 0 ? "tilt-left" : "tilt-right");
                AutomationProperties.SetAutomationId(
                    frame,
                    $"ProgressShelfObject_{projection.Capability.Id}");
                AutomationProperties.SetName(
                    frame,
                    $"Paper situation object. {projection.Capability.Label}. {statusLabel}.");
                shelfObjects.Add(frame);
                objectsPanel.Children.Add(frame);
            }
        }

        PaperStage.SetLayer(objectsPanel, PaperStageLayer.Subject);
        PaperStage.SetAnchor(objectsPanel, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(objectsPanel, 0.5);
        PaperStage.SetAnchorOffsetY(objectsPanel, -22);
        stage.Children.Add(objectsPanel);

        var modeText = new TextBlock
        {
            Text = parameters.UseTextOnlyFallback
                ? "Text-only progress mode is active. Every situation and projected status remains complete."
                : "Paper objects lead with situations and projected evidence status.",
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Classes = { "muted" },
        };
        AutomationProperties.SetAutomationId(modeText, "ProgressShelfTextEquivalent");
        var methodCard = new PaperCard
        {
            Padding = new Thickness(14, 10),
            Content = new TextBlock
            {
                Text = methodNote,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
            },
        };
        methodCard.Classes.Add("soft");
        AutomationProperties.SetAutomationId(methodCard, "ProgressShelfMethod");
        AutomationProperties.SetName(methodCard, $"How status is projected. {methodNote}");
        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 12 };
        footer.Children.Add(detailCard);
        Grid.SetColumn(outcomePanel, 1);
        footer.Children.Add(outcomePanel);
        var root = new StackPanel { Spacing = 14 };
        root.Children.Add(header);
        root.Children.Add(stage);
        root.Children.Add(modeText);
        root.Children.Add(methodCard);
        root.Children.Add(footer);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            var midpoint = (shelfObjects.Count + 1) / 2;
            TemplateRendering.Prepare(
                shouldReduceMotion,
                [shelfHeader, shelfLine, modeText, methodCard, footer, .. shelfObjects]);
            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(140), shelfHeader),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(140), shelfLine),
                TemplateRendering.Reveal(
                    TimeSpan.FromMilliseconds(280),
                    [.. shelfObjects.Take(midpoint)]),
                TemplateRendering.Reveal(
                    TimeSpan.FromMilliseconds(280),
                    [.. shelfObjects.Skip(midpoint)]),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(200), modeText, methodCard, footer),
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

    private static string StatusLabel(ShelfCapabilityStatus status) => status switch
    {
        ShelfCapabilityStatus.Demonstrated => "CAN HANDLE",
        ShelfCapabilityStatus.Practicing => "PRACTICING",
        _ => "NOT STARTED",
    };

    private static string StatusDescription(ShelfCapabilityStatus status) => status switch
    {
        ShelfCapabilityStatus.Demonstrated => "Demonstrated from projected task evidence.",
        ShelfCapabilityStatus.Practicing => "Practicing from projected task evidence.",
        _ => "Not started. No ability is inferred from setup alone.",
    };

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Demonstrated situation selected from projected task evidence.",
        TemplateOutcomeState.Uncertain => "Practicing situation selected, or choose a paper situation to inspect.",
        TemplateOutcomeState.Failure => "Capability status is unavailable. Return to the projected shelf.",
        _ => "Not-started situation selected, or the capability shelf is ready.",
    };

    private sealed record ProjectedCapability(
        TemplateOption Capability,
        ShelfCapabilityStatus Status);

    private enum ShelfCapabilityStatus
    {
        Demonstrated,
        Practicing,
        NotStarted,
    }
}
