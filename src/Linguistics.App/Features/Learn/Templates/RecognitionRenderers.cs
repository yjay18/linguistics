using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Linguistics.App.Content;
using Linguistics.App.Controls;
using Linguistics.App.Motion;
using Linguistics.Core.Content;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Learn.Templates;

internal static class ChoiceTemplatePresentation
{
    public static Control Compose(
        string prefix,
        string instruction,
        PaperStage stage,
        IReadOnlyList<TemplateOption> options,
        string answerId,
        Func<TemplateOption, int, Control> createChoiceContent,
        ContentImageCache? imageCache,
        IEnumerable<string?> creditReferences,
        bool useTextOnlyFallback,
        string textOnlyCopy,
        TemplateOutcomeState previewOutcome,
        bool shouldReduceMotion,
        Func<TemplateOutcomeState, string> outcomeCopy,
        IReadOnlyList<Control> animatedStageControls,
        Action<Control> prepareMotion,
        Func<Control, IReadOnlyList<PaperChoreographyStep>> createSteps,
        Action skipExtra,
        Action<TemplateOutcome> reportOutcome,
        Action<Button, TemplateOutcome>? afterChoice = null)
    {
        var replayButton = new Button { Content = "Replay reveal", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, $"{prefix}Replay");
        AutomationProperties.SetName(replayButton, "Replay the paper reveal");
        var skipButton = new Button { Content = "Skip reveal", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, $"{prefix}Skip");
        AutomationProperties.SetName(skipButton, "Skip to the completed paper scene");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Activity instruction. {instruction}");
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actions.Children.Add(replayButton);
        actions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(actions, 1);
        header.Children.Add(actions);

        var choicePanel = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            ItemWidth = 160,
            ItemHeight = 92,
        };
        var buttons = new Dictionary<string, Button>(StringComparer.Ordinal);
        var selectedId = InitialSelection(previewOutcome, options, answerId);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            previewOutcome,
            outcomeCopy,
            out var outcomeText);
        foreach (var (option, index) in options.Select((option, index) => (option, index)))
        {
            var button = new Button
            {
                Width = 148,
                Height = 80,
                Margin = new Thickness(5),
                Padding = new Thickness(6),
                Content = createChoiceContent(option, index),
                Classes = { "quiet", "lift" },
            };
            AutomationProperties.SetAutomationId(button, $"{prefix}Option_{option.Id}");
            AutomationProperties.SetName(button, $"Choose {option.Label}");
            button.Click += (_, _) =>
            {
                selectedId = option.Id;
                UpdateSelection(buttons, selectedId);
                var outcome = TemplateInteractionEvaluator.EvaluateSingleSelection(
                    options,
                    answerId,
                    selectedId);
                TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, outcomeCopy);
                afterChoice?.Invoke(button, outcome);
                reportOutcome(outcome);
            };
            buttons.Add(option.Id, button);
            choicePanel.Children.Add(button);
        }

        UpdateSelection(buttons, selectedId);
        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        if (useTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = textOnlyCopy,
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(stage);
        root.Children.Add(choicePanel);
        if (!useTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                creditReferences,
                $"{prefix}ImageCredits") is { } credits)
        {
            root.Children.Add(credits);
        }

        root.Children.Add(outcomePanel);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(
                shouldReduceMotion,
                animatedStageControls.Append(choicePanel).ToArray());
            if (!shouldReduceMotion)
            {
                prepareMotion(choicePanel);
            }

            scene = new PaperChoreography(createSteps(choicePanel));
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
            skipExtra();
        };
        replayButton.Click += async (_, _) => await PlayAsync();
        return root;
    }

    private static string? InitialSelection(
        TemplateOutcomeState state,
        IReadOnlyList<TemplateOption> options,
        string answerId) => state switch
        {
            TemplateOutcomeState.Success => answerId,
            TemplateOutcomeState.Failure => options.FirstOrDefault(option => option.Id != answerId)?.Id,
            _ => null,
        };

    private static void UpdateSelection(
        IReadOnlyDictionary<string, Button> buttons,
        string? selectedId)
    {
        foreach (var pair in buttons)
        {
            pair.Value.Classes.Remove("primary");
            if (string.Equals(pair.Key, selectedId, StringComparison.Ordinal))
            {
                pair.Value.Classes.Add("primary");
            }
        }
    }
}

internal static class WordMatchRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var description = TemplateRendering.Text(parameters, "subject-description");
        var options = TemplateRendering.Options(parameters, "options");
        var answerId = TemplateRendering.Text(parameters, "answer");
        var assetReference = TemplateRendering.AssetReference(parameters, "asset");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var image = parameters.UseTextOnlyFallback
            ? null
            : TemplateRendering.CreateContentImage(imageCache, assetReference, 142);

        var stage = TemplateRendering.CreateStage(272, $"Word match subject. {description}");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var subject = new CutoutFrame
        {
            Width = 236,
            Height = 184,
            Content = image as Control ?? new TextBlock
            {
                Text = description,
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
            RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        };
        subject.Classes.Add("tilt-left");
        AutomationProperties.SetName(subject, $"Subject: {description}");
        PaperStage.SetLayer(subject, PaperStageLayer.Subject);
        PaperStage.SetAnchor(subject, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(subject, 0.5);
        stage.Children.Add(subject);
        var tape = new PaperTape { Content = "NAME THIS", Angle = 1.4 };
        PaperStage.SetLayer(tape, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -9);
        stage.Children.Add(tape);

        return ChoiceTemplatePresentation.Compose(
            "WordMatch",
            instruction,
            stage,
            options,
            answerId,
            (option, index) => new PaperTape
            {
                Content = option.Label,
                Angle = index % 2 == 0 ? -1.1 : 1.2,
            },
            imageCache,
            [image is not null ? assetReference : null, backdropRendered ? backdropReference : null],
            parameters.UseTextOnlyFallback,
            $"Text-only subject: {description}. Every word choice remains available.",
            parameters.PreviewOutcome,
            shouldReduceMotion,
            OutcomeCopy,
            [tape, subject],
            choicePanel =>
            {
                subject.RenderTransform = TemplateRendering.Transform(-24, 8, -2, 0.96);
                choicePanel.RenderTransform = TemplateRendering.Transform(0, 10, 0, 0.98);
            },
            choicePanel =>
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(720), subject, 0, 0, -1.1, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(620), choicePanel, 0, 0, 0, 1),
            ],
            tape.SkipEntrance,
            reportOutcome);
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Matched. The authored word names the cutout.",
        TemplateOutcomeState.Uncertain => "Choose one complete word before checking the match.",
        TemplateOutcomeState.Failure => "Not this word yet. Compare the choices and try again.",
        _ => "Ready: choose the word that names the cutout.",
    };
}

internal static class PairCardsRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var pairs = TemplateRendering.Options(parameters, "pairs");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var replayButton = new Button { Content = "Replay deal", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "PairCardsReplay");
        AutomationProperties.SetName(replayButton, "Replay the paper card deal");
        var skipButton = new Button { Content = "Skip deal", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "PairCardsSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed card deal");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Pair cards instruction. {instruction}");
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actions.Children.Add(replayButton);
        actions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(actions, 1);
        header.Children.Add(actions);

        var stage = TemplateRendering.CreateStage(344, "Pair cards word and picture matching table");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var tape = new PaperTape { Content = "FIND A PAIR", Angle = -1.4 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -12);
        stage.Children.Add(tape);
        var cardsPanel = new WrapPanel
        {
            Width = 450,
            Margin = new Thickness(34, 100, 34, 12),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            ItemWidth = 140,
            ItemHeight = 104,
        };
        PaperStage.SetLayer(cardsPanel, PaperStageLayer.Subject);
        stage.Children.Add(cardsPanel);

        var cardButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
        var selectedCardIds = InitialCards(parameters.PreviewOutcome, pairs).ToList();
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var renderedAssets = parameters.UseTextOnlyFallback
            ? new List<string?>()
            : pairs.Select(pair => pair.AssetReferenceId).Cast<string?>().ToList();
        foreach (var (pair, pairIndex) in pairs.Select((pair, index) => (pair, index)))
        {
            AddCard("word", pair, pairIndex * 2);
            AddCard("image", pair, (pairIndex * 2) + 1);
        }

        foreach (var selectedCardId in selectedCardIds)
        {
            SetCardFace(cardButtons[selectedCardId], selectedCardId, revealed: true);
        }

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = $"Text-only pairs: {string.Join(", ", pairs.Select(pair => pair.Label))}. Word and picture-equivalent cards remain distinct.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(stage);
        if (!parameters.UseTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                renderedAssets.Append(backdropRendered ? backdropReference : null),
                "PairCardsImageCredits") is { } credits)
        {
            root.Children.Add(credits);
        }

        root.Children.Add(outcomePanel);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, tape, cardsPanel);
            if (!shouldReduceMotion)
            {
                cardsPanel.RenderTransform = TemplateRendering.Transform(0, 14, 0.8, 0.97);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(960), cardsPanel, 0, 0, 0, 1),
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
        return root;

        void AddCard(string side, TemplateOption pair, int index)
        {
            var cardId = $"{side}:{pair.Id}";
            var button = new Button
            {
                Width = 130,
                Height = 94,
                Margin = new Thickness(5),
                Padding = new Thickness(5),
                Classes = { "quiet", "lift" },
                RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            };
            button.Classes.Add(index % 2 == 0 ? "tilt-left" : "tilt-right");
            AutomationProperties.SetAutomationId(button, $"PairCardsCard_{side}_{pair.Id}");
            SetCardFace(button, cardId, selectedCardIds.Contains(cardId, StringComparer.Ordinal));
            button.Click += async (_, _) => await RevealCardAsync(button, cardId);
            cardButtons.Add(cardId, button);
            cardsPanel.Children.Add(button);
        }

        async Task RevealCardAsync(Button button, string cardId)
        {
            if (selectedCardIds.Count == 2)
            {
                foreach (var oldCardId in selectedCardIds)
                {
                    cardButtons[oldCardId].Classes.Remove("primary");
                    SetCardFace(cardButtons[oldCardId], oldCardId, revealed: false);
                }

                selectedCardIds.Clear();
            }

            if (selectedCardIds.Contains(cardId, StringComparer.Ordinal))
            {
                var repeated = TemplateInteractionEvaluator.EvaluatePairCards(pairs, selectedCardIds);
                TemplateRendering.ApplyOutcome(
                    outcomePanel,
                    outcomeText,
                    repeated.State,
                    OutcomeCopy);
                reportOutcome(repeated);
                return;
            }

            button.IsEnabled = false;
            if (!shouldReduceMotion)
            {
                button.RenderTransform = TemplateRendering.Transform(0, 0, 0, 0.12);
                await Task.Delay(90);
            }

            selectedCardIds.Add(cardId);
            SetCardFace(button, cardId, revealed: true);
            button.Classes.Add("primary");
            button.RenderTransform = TemplateRendering.Transform(0, 0, 0, 1);
            button.IsEnabled = true;
            var outcome = TemplateInteractionEvaluator.EvaluatePairCards(pairs, selectedCardIds);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            reportOutcome(outcome);
        }

        void SetCardFace(Button button, string cardId, bool revealed)
        {
            var parts = cardId.Split(':', 2);
            var side = parts[0];
            var pair = pairs.Single(candidate => candidate.Id == parts[1]);
            Control content;
            if (!revealed)
            {
                content = new TextBlock
                {
                    Text = "?",
                    FontSize = 32,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                AutomationProperties.SetName(button, $"Hidden {side} card. Reveal card");
            }
            else if (side == "word")
            {
                content = new TextBlock
                {
                    Text = pair.Label,
                    FontSize = 20,
                    FontWeight = FontWeight.Bold,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                AutomationProperties.SetName(button, $"Revealed word card {pair.Label}");
            }
            else
            {
                var image = parameters.UseTextOnlyFallback
                    ? null
                    : TemplateRendering.CreateContentImage(imageCache, pair.AssetReferenceId, 62);
                if (image is not null)
                {
                    renderedAssets.Add(pair.AssetReferenceId);
                }

                content = image as Control ?? new TextBlock
                {
                    Text = $"Picture: {pair.Label}",
                    FontWeight = FontWeight.Bold,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                AutomationProperties.SetName(button, $"Revealed picture card for {pair.Label}");
            }

            var frame = new CutoutFrame
            {
                Width = 112,
                Height = 76,
                Padding = new Thickness(4),
                Content = content,
            };
            frame.Classes.Add(side == "word" ? "tilt-left" : "tilt-right");
            button.Content = frame;
        }
    }

    private static IReadOnlyList<string> InitialCards(
        TemplateOutcomeState state,
        IReadOnlyList<TemplateOption> pairs)
    {
        var first = pairs[0].Id;
        return state switch
        {
            TemplateOutcomeState.Success => [$"word:{first}", $"image:{first}"],
            TemplateOutcomeState.Failure when pairs.Count > 1 =>
                [$"word:{first}", $"image:{pairs[1].Id}"],
            TemplateOutcomeState.Uncertain => [$"word:{first}"],
            _ => [],
        };
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Pair found. The word and picture belong together.",
        TemplateOutcomeState.Uncertain => "Reveal one more card to complete this pair.",
        TemplateOutcomeState.Failure => "Those cards do not match. Start a new pair.",
        _ => "Ready: reveal one word card and one picture card.",
    };
}

internal static class OddOneOutRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var categoryLabel = TemplateRendering.Text(parameters, "category-label");
        var options = TemplateRendering.Options(parameters, "options");
        var answerId = TemplateRendering.Text(parameters, "answer");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var stage = TemplateRendering.CreateStage(174, $"Odd one out category: {categoryLabel}");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var categoryCard = new PaperCard
        {
            Width = 248,
            Padding = new Thickness(18, 14),
            Content = new TextBlock
            {
                Text = categoryLabel,
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                TextAlignment = TextAlignment.Center,
            },
        };
        categoryCard.Classes.Add("soft");
        AutomationProperties.SetName(categoryCard, $"Shared category: {categoryLabel}");
        PaperStage.SetLayer(categoryCard, PaperStageLayer.Subject);
        PaperStage.SetAnchor(categoryCard, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(categoryCard, 0.5);
        stage.Children.Add(categoryCard);
        var tape = new PaperTape { Content = "THREE BELONG", Angle = 1.5 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -8);
        stage.Children.Add(tape);

        var renderedAssets = new List<string?>();
        return ChoiceTemplatePresentation.Compose(
            "OddOneOut",
            instruction,
            stage,
            options,
            answerId,
            (option, index) =>
            {
                var image = parameters.UseTextOnlyFallback
                    ? null
                    : TemplateRendering.CreateContentImage(imageCache, option.AssetReferenceId, 24);
                if (image is not null)
                {
                    renderedAssets.Add(option.AssetReferenceId);
                }

                var copy = new StackPanel
                {
                    Spacing = 1,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                if (image is not null)
                {
                    copy.Children.Add(image);
                }

                copy.Children.Add(new TextBlock
                {
                    Text = option.Label,
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                });
                var frame = new CutoutFrame
                {
                    Width = 132,
                    Height = 62,
                    Padding = new Thickness(4),
                    Content = copy,
                };
                frame.Classes.Add(index % 2 == 0 ? "tilt-left" : "tilt-right");
                return frame;
            },
            imageCache,
            renderedAssets.Append(backdropRendered ? backdropReference : null),
            parameters.UseTextOnlyFallback,
            $"Text-only category: {categoryLabel}. Choices: {string.Join(", ", options.Select(option => option.Label))}.",
            parameters.PreviewOutcome,
            shouldReduceMotion,
            OutcomeCopy,
            [tape, categoryCard],
            choicePanel =>
            {
                categoryCard.RenderTransform = TemplateRendering.Transform(0, 8, -1, 0.97);
                choicePanel.RenderTransform = TemplateRendering.Transform(0, 10, 0, 0.98);
            },
            choicePanel =>
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(520), categoryCard, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(720), choicePanel, 0, 0, 0, 1),
            ],
            tape.SkipEntrance,
            reportOutcome);
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Found it. That cutout sits outside the authored category.",
        TemplateOutcomeState.Uncertain => "Choose one cutout before checking the category.",
        TemplateOutcomeState.Failure => "That one belongs. Compare the remaining cutouts.",
        _ => "Ready: find the one cutout that does not belong.",
    };
}

internal static class SortIntoBasketsRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var items = TemplateRendering.Options(parameters, "items");
        var baskets = TemplateRendering.Options(parameters, "baskets");
        var answers = TemplateRendering.Options(parameters, "answers");
        var expectedAssignments = answers.ToDictionary(
            answer => answer.Id,
            answer => answer.Label,
            StringComparer.Ordinal);
        var selectedAssignments = InitialAssignments(
            parameters.PreviewOutcome,
            items,
            baskets,
            expectedAssignments);
        _ = TemplateInteractionEvaluator.EvaluateSortAssignments(
            items,
            baskets,
            expectedAssignments,
            selectedAssignments);
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");

        var replayButton = new Button { Content = "Replay sorting table", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "SortIntoBasketsReplay");
        AutomationProperties.SetName(replayButton, "Replay the paper sorting table entrance");
        var skipButton = new Button { Content = "Skip entrance", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "SortIntoBasketsSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed sorting table");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Sorting instruction. {instruction}");
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actions.Children.Add(replayButton);
        actions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(actions, 1);
        header.Children.Add(actions);

        var stage = TemplateRendering.CreateStage(394, "Sorting table with labeled paper baskets");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var tape = new PaperTape { Content = "SORT THE CUTOUTS", Angle = -1.2 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -11);
        stage.Children.Add(tape);

        var itemPanel = new WrapPanel
        {
            Margin = new Thickness(28, 68, 28, 170),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ItemWidth = 150,
            ItemHeight = 104,
        };
        PaperStage.SetLayer(itemPanel, PaperStageLayer.Subject);
        stage.Children.Add(itemPanel);
        var basketPanel = new WrapPanel
        {
            Margin = new Thickness(50, 236, 50, 22),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ItemWidth = 240,
            ItemHeight = 118,
        };
        PaperStage.SetLayer(basketPanel, PaperStageLayer.VerdictCard);
        stage.Children.Add(basketPanel);

        var status = new TextBlock
        {
            Text = "Select an item, then choose a basket. Dragging is also available.",
            Classes = { "muted" },
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(status, "SortIntoBasketsStatus");
        AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var itemButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
        string? activeItemId = null;
        foreach (var (item, index) in items.Select((item, index) => (item, index)))
        {
            var button = new Button
            {
                Width = 140,
                Height = 94,
                Margin = new Thickness(5),
                Padding = new Thickness(5),
                Classes = { "quiet", "lift" },
            };
            AutomationProperties.SetAutomationId(button, $"SortIntoBasketsItem_{item.Id}");
            button.Click += (_, _) => SelectItem(item.Id);
            PointerPressedEventArgs? dragStartArgs = null;
            Point? dragStartPoint = null;
            button.PointerPressed += (_, args) =>
            {
                if (!args.GetCurrentPoint(button).Properties.IsLeftButtonPressed)
                {
                    return;
                }

                dragStartArgs = args;
                dragStartPoint = args.GetPosition(button);
            };
            button.PointerMoved += async (_, args) =>
            {
                if (dragStartArgs is null || dragStartPoint is not { } start)
                {
                    return;
                }

                var current = args.GetPosition(button);
                if (Math.Abs(current.X - start.X) < 7 && Math.Abs(current.Y - start.Y) < 7)
                {
                    return;
                }

                var pointerPressedArgs = dragStartArgs;
                dragStartArgs = null;
                dragStartPoint = null;
                var transfer = new DataTransfer();
                transfer.Add(DataTransferItem.CreateText(item.Id));
                await DragDrop.DoDragDropAsync(pointerPressedArgs, transfer, DragDropEffects.Move);
            };
            button.PointerReleased += (_, _) =>
            {
                dragStartArgs = null;
                dragStartPoint = null;
            };
            itemButtons.Add(item.Id, button);
            itemPanel.Children.Add(button);
            UpdateItemCard(item.Id, index);
        }

        foreach (var basket in baskets)
        {
            var label = new StackPanel
            {
                Spacing = 5,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            label.Children.Add(new TextBlock
            {
                Text = "BASKET",
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Classes = { "muted" },
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            label.Children.Add(new TextBlock
            {
                Text = basket.Label,
                FontSize = 19,
                FontWeight = FontWeight.Bold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            var card = new PaperCard
            {
                Width = 220,
                Height = 98,
                Padding = new Thickness(14, 10),
                Content = label,
            };
            card.Classes.Add("soft");
            var button = new Button
            {
                Width = 232,
                Height = 108,
                Margin = new Thickness(4),
                Padding = new Thickness(5),
                Content = card,
                Classes = { "quiet", "lift" },
            };
            AutomationProperties.SetAutomationId(button, $"SortIntoBasketsBasket_{basket.Id}");
            AutomationProperties.SetName(
                button,
                $"Assign selected item to {basket.Label}. Drop items here");
            button.Click += (_, _) =>
            {
                if (activeItemId is null)
                {
                    status.Text = $"Select an item before choosing {basket.Label}.";
                    return;
                }

                Assign(activeItemId, basket.Id);
            };
            DragDrop.SetAllowDrop(button, true);
            DragDrop.AddDragOverHandler(button, (_, args) =>
            {
                args.DragEffects = items.Any(item =>
                    string.Equals(item.Id, args.DataTransfer.TryGetText(), StringComparison.Ordinal))
                    ? DragDropEffects.Move
                    : DragDropEffects.None;
            });
            DragDrop.AddDropHandler(button, (_, args) =>
            {
                var itemId = args.DataTransfer.TryGetText();
                if (itemId is not null && items.Any(item => item.Id == itemId))
                {
                    Assign(itemId, basket.Id);
                    args.DragEffects = DragDropEffects.Move;
                }
            });
            basketPanel.Children.Add(button);
        }

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = $"Text-only items: {string.Join(", ", items.Select(item => item.Label))}. Baskets: {string.Join(", ", baskets.Select(basket => basket.Label))}.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(stage);
        root.Children.Add(status);
        if (!parameters.UseTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                items.Select(item => item.AssetReferenceId)
                    .Append(backdropRendered ? backdropReference : null),
                "SortIntoBasketsImageCredits") is { } credits)
        {
            root.Children.Add(credits);
        }

        root.Children.Add(outcomePanel);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, tape, itemPanel, basketPanel);
            if (!shouldReduceMotion)
            {
                itemPanel.RenderTransform = TemplateRendering.Transform(-18, 8, -1, 0.98);
                basketPanel.RenderTransform = TemplateRendering.Transform(18, 10, 1, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(620), itemPanel, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(620), basketPanel, 0, 0, 0, 1),
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
        return root;

        void SelectItem(string itemId)
        {
            activeItemId = itemId;
            foreach (var pair in itemButtons)
            {
                pair.Value.Classes.Remove("primary");
                if (pair.Key == itemId)
                {
                    pair.Value.Classes.Add("primary");
                }
            }

            var item = items.Single(candidate => candidate.Id == itemId);
            status.Text = $"Selected {item.Label}. Choose a labeled basket.";
        }

        void Assign(string itemId, string basketId)
        {
            selectedAssignments[itemId] = basketId;
            var index = items.Select(item => item.Id).ToList().IndexOf(itemId);
            UpdateItemCard(itemId, index);
            var item = items.Single(candidate => candidate.Id == itemId);
            var basket = baskets.Single(candidate => candidate.Id == basketId);
            status.Text = $"Assigned {item.Label} to {basket.Label}.";
            var outcome = TemplateInteractionEvaluator.EvaluateSortAssignments(
                items,
                baskets,
                expectedAssignments,
                selectedAssignments);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            reportOutcome(outcome);
            activeItemId = null;
            foreach (var button in itemButtons.Values)
            {
                button.Classes.Remove("primary");
            }
        }

        void UpdateItemCard(string itemId, int index)
        {
            var item = items.Single(candidate => candidate.Id == itemId);
            var image = parameters.UseTextOnlyFallback
                ? null
                : TemplateRendering.CreateContentImage(imageCache, item.AssetReferenceId, 44);
            var content = new StackPanel
            {
                Spacing = 3,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            if (image is not null)
            {
                content.Children.Add(image);
            }

            content.Children.Add(new TextBlock
            {
                Text = selectedAssignments.TryGetValue(itemId, out var basketId)
                    ? $"{item.Label} · {baskets.Single(basket => basket.Id == basketId).Label}"
                    : item.Label,
                FontWeight = FontWeight.Bold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            var frame = new CutoutFrame { Width = 122, Height = 76, Content = content };
            frame.Classes.Add(index % 2 == 0 ? "tilt-left" : "tilt-right");
            itemButtons[itemId].Content = frame;
            AutomationProperties.SetName(
                itemButtons[itemId],
                selectedAssignments.TryGetValue(itemId, out var assignedBasketId)
                    ? $"{item.Label}, assigned to {baskets.Single(basket => basket.Id == assignedBasketId).Label}. Select or drag to reassign"
                    : $"{item.Label}, unassigned. Select or drag to a basket");
        }
    }

    private static Dictionary<string, string> InitialAssignments(
        TemplateOutcomeState state,
        IReadOnlyList<TemplateOption> items,
        IReadOnlyList<TemplateOption> baskets,
        IReadOnlyDictionary<string, string> expected)
    {
        if (state == TemplateOutcomeState.Ready)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        if (state == TemplateOutcomeState.Uncertain)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [items[0].Id] = expected[items[0].Id],
            };
        }

        var assignments = expected.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        if (state == TemplateOutcomeState.Failure)
        {
            assignments[items[0].Id] = baskets.First(basket => basket.Id != expected[items[0].Id]).Id;
        }

        return assignments;
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Sorted. Every item is in its authored basket.",
        TemplateOutcomeState.Uncertain => "Keep sorting until every item has a basket.",
        TemplateOutcomeState.Failure => "One or more items need a different basket.",
        _ => "Ready: assign every item by drag, mouse, or keyboard.",
    };
}

internal static class ArticleStampRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var noun = TemplateRendering.Text(parameters, "noun");
        var options = TemplateRendering.Options(parameters, "options");
        var answerId = TemplateRendering.Text(parameters, "answer");
        var assetReference = TemplateRendering.AssetReference(parameters, "asset");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var image = parameters.UseTextOnlyFallback
            ? null
            : TemplateRendering.CreateContentImage(imageCache, assetReference, 118);
        var stage = TemplateRendering.CreateStage(276, $"Article stamp noun: {noun}");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var subjectContent = new StackPanel
        {
            Spacing = 7,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (image is not null)
        {
            subjectContent.Children.Add(image);
        }

        subjectContent.Children.Add(new TextBlock
        {
            Text = noun,
            FontSize = 26,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        var subject = new CutoutFrame
        {
            Width = 244,
            Height = 194,
            Content = subjectContent,
            RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        };
        subject.Classes.Add("tilt-right");
        AutomationProperties.SetName(subject, $"German noun {noun}");
        PaperStage.SetLayer(subject, PaperStageLayer.Subject);
        PaperStage.SetAnchor(subject, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(subject, 0.5);
        stage.Children.Add(subject);
        var tape = new PaperTape { Content = "CHOOSE AN ARTICLE", Angle = -1.4 };
        PaperStage.SetLayer(tape, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);
        var stamps = new List<PaperStamp>();

        return ChoiceTemplatePresentation.Compose(
            "ArticleStamp",
            instruction,
            stage,
            options,
            answerId,
            (option, index) =>
            {
                var stamp = new PaperStamp
                {
                    Content = option.Label,
                    Angle = index switch { 0 => -3, 1 => 1.5, _ => 3 },
                };
                stamp.Classes.Add("rectangle");
                AutomationProperties.SetName(stamp, $"Article stamp {option.Label}");
                stamps.Add(stamp);
                return stamp;
            },
            imageCache,
            [image is not null ? assetReference : null, backdropRendered ? backdropReference : null],
            parameters.UseTextOnlyFallback,
            $"Text-only noun: {noun}. Article choices: {string.Join(", ", options.Select(option => option.Label))}.",
            parameters.PreviewOutcome,
            shouldReduceMotion,
            OutcomeCopy,
            [tape, subject],
            choicePanel =>
            {
                subject.RenderTransform = TemplateRendering.Transform(20, 8, 2, 0.96);
                choicePanel.RenderTransform = TemplateRendering.Transform(0, 10, 0, 0.98);
            },
            choicePanel =>
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(660), subject, 0, 0, 1.1, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(560), choicePanel, 0, 0, 0, 1),
            ],
            () =>
            {
                tape.SkipEntrance();
                foreach (var stamp in stamps)
                {
                    stamp.SkipEntrance();
                }
            },
            reportOutcome,
            (button, outcome) =>
            {
                if (button.Content is PaperStamp stamp && outcome.State == TemplateOutcomeState.Success)
                {
                    _ = stamp.PlayEntranceAsync();
                }
                else if (outcome.State == TemplateOutcomeState.Failure)
                {
                    _ = WobbleAsync(button, shouldReduceMotion);
                }
            });
    }

    private static async Task WobbleAsync(Control control, bool shouldReduceMotion)
    {
        if (shouldReduceMotion)
        {
            control.RenderTransform = TemplateRendering.Transform(0, 0, 0, 1);
            return;
        }

        control.RenderTransform = TemplateRendering.Transform(0, -4, -3, 1.02);
        await Task.Delay(85);
        control.RenderTransform = TemplateRendering.Transform(0, -2, 3, 1.01);
        await Task.Delay(85);
        control.RenderTransform = TemplateRendering.Transform(0, 0, 0, 1);
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Stamped. The authored article belongs with this noun.",
        TemplateOutcomeState.Uncertain => "Choose one article stamp before checking the noun.",
        TemplateOutcomeState.Failure => "That stamp lifts away. Try another authored article.",
        _ => "Ready: choose an article stamp for the noun.",
    };
}

internal static class PluralFoldRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var singular = TemplateRendering.Text(parameters, "singular");
        var plural = TemplateRendering.Text(parameters, "plural");
        var assetReference = TemplateRendering.AssetReference(parameters, "asset");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var image = parameters.UseTextOnlyFallback
            ? null
            : TemplateRendering.CreateContentImage(imageCache, assetReference, 104);
        var replayButton = new Button { Content = "Replay fold entrance", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "PluralFoldReplay");
        AutomationProperties.SetName(replayButton, "Replay the folded card entrance");
        var skipButton = new Button { Content = "Skip entrance", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "PluralFoldSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed folded card scene");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Plural fold instruction. {instruction}");
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actions.Children.Add(replayButton);
        actions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(actions, 1);
        header.Children.Add(actions);

        var stage = TemplateRendering.CreateStage(302, $"Plural fold from {singular} to {plural}");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var tape = new PaperTape { Content = "ONE / MORE THAN ONE", Angle = 1.3 };
        PaperStage.SetLayer(tape, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);

        var foldedCopy = new StackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (image is not null)
        {
            foldedCopy.Children.Add(image);
        }

        foldedCopy.Children.Add(new TextBlock
        {
            Text = singular,
            FontSize = 27,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        var foldedFace = new PaperCard
        {
            Width = 286,
            Height = 202,
            Padding = new Thickness(20, 16),
            Content = foldedCopy,
        };
        AutomationProperties.SetName(foldedFace, $"Folded singular card. {singular}");
        var unfoldedCopy = new StackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        unfoldedCopy.Children.Add(new TextBlock
        {
            Text = singular,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        unfoldedCopy.Children.Add(new TornEdge { Width = 216, Height = 24 });
        unfoldedCopy.Children.Add(new TextBlock
        {
            Text = plural,
            FontSize = 29,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        var unfoldedFace = new PaperCard
        {
            Width = 322,
            Height = 202,
            Padding = new Thickness(20, 16),
            Content = unfoldedCopy,
        };
        unfoldedFace.Classes.Add("soft");
        AutomationProperties.SetName(
            unfoldedFace,
            $"Unfolded word forms. Singular {singular}. Plural {plural}");
        var foldCard = new Grid
        {
            Width = 330,
            Height = 210,
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
        };
        foldCard.Children.Add(foldedFace);
        foldCard.Children.Add(unfoldedFace);
        var isUnfolded = parameters.PreviewOutcome == TemplateOutcomeState.Success;
        foldedFace.IsVisible = !isUnfolded;
        unfoldedFace.IsVisible = isUnfolded;
        PaperStage.SetLayer(foldCard, PaperStageLayer.Subject);
        PaperStage.SetAnchor(foldCard, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(foldCard, 0.5);
        stage.Children.Add(foldCard);

        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var foldButton = new Button
        {
            Content = isUnfolded ? "Fold back" : "Unfold plural",
            Classes = { "primary", "lift" },
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        AutomationProperties.SetAutomationId(foldButton, "PluralFoldToggle");
        UpdateFoldButtonName();
        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        footer.Children.Add(outcomePanel);
        Grid.SetColumn(foldButton, 1);
        footer.Children.Add(foldButton);

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = $"Text-only word forms. Singular: {singular}. Plural: {plural}.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(stage);
        if (!parameters.UseTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                [image is not null ? assetReference : null, backdropRendered ? backdropReference : null],
                "PluralFoldImageCredits") is { } credits)
        {
            root.Children.Add(credits);
        }

        root.Children.Add(footer);

        foldButton.Click += async (_, _) =>
        {
            foldButton.IsEnabled = false;
            if (!shouldReduceMotion)
            {
                foldCard.RenderTransform = TemplateRendering.Transform(0, 0, 0, 0.12);
                await Task.Delay(110);
            }

            isUnfolded = !isUnfolded;
            foldedFace.IsVisible = !isUnfolded;
            unfoldedFace.IsVisible = isUnfolded;
            foldCard.RenderTransform = TemplateRendering.Transform(0, 0, 0, 1);
            foldButton.Content = isUnfolded ? "Fold back" : "Unfold plural";
            UpdateFoldButtonName();
            foldButton.IsEnabled = true;
            var outcome = TemplateInteractionEvaluator.EvaluateAcknowledgement(isUnfolded);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            reportOutcome(outcome);
        };

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, tape, foldCard);
            if (!shouldReduceMotion)
            {
                foldCard.RenderTransform = TemplateRendering.Transform(24, 10, 2, 0.97);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(760), foldCard, 0, 0, -1, 1),
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
        return root;

        void UpdateFoldButtonName() => AutomationProperties.SetName(
            foldButton,
            isUnfolded
                ? $"Fold the card back to singular {singular}"
                : $"Unfold the card to reveal plural {plural}");
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Unfolded. Both authored word forms are visible together.",
        TemplateOutcomeState.Uncertain => "Unfold the card when you are ready to compare.",
        TemplateOutcomeState.Failure => "Folded. Open the card to compare both forms again.",
        _ => "Ready: unfold the singular card to reveal its plural.",
    };
}

internal static class ColorSwatchRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var objectName = TemplateRendering.Text(parameters, "object-name");
        var options = TemplateRendering.Options(parameters, "options");
        var swatchColors = TemplateRendering.Options(parameters, "swatch-colors")
            .ToDictionary(swatch => swatch.Id, swatch => swatch.Label, StringComparer.Ordinal);
        if (options.Any(option => !swatchColors.ContainsKey(option.Id)))
        {
            throw new InvalidOperationException("Every color option needs an authored swatch color.");
        }

        var answerId = TemplateRendering.Text(parameters, "answer");
        var assetReference = TemplateRendering.AssetReference(parameters, "asset");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var image = parameters.UseTextOnlyFallback
            ? null
            : TemplateRendering.CreateContentImage(imageCache, assetReference, 94);
        var stage = TemplateRendering.CreateStage(270, $"Color swatch object: {objectName}");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var subjectCopy = new StackPanel
        {
            Spacing = 7,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (image is not null)
        {
            subjectCopy.Children.Add(image);
        }

        subjectCopy.Children.Add(new TextBlock
        {
            Text = objectName,
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        var colorPatch = new Border
        {
            Width = 252,
            Height = 178,
            Padding = new Thickness(10),
            CornerRadius = new CornerRadius(6),
            Child = new CutoutFrame
            {
                Width = 224,
                Height = 152,
                Content = subjectCopy,
            },
            RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        };
        AutomationProperties.SetName(colorPatch, $"Object card for {objectName}. No pigment applied yet");
        PaperStage.SetLayer(colorPatch, PaperStageLayer.Subject);
        PaperStage.SetAnchor(colorPatch, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(colorPatch, 0.5);
        stage.Children.Add(colorPatch);
        var tape = new PaperTape { Content = "APPLY A COLOR", Angle = -1.5 };
        PaperStage.SetLayer(tape, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -9);
        stage.Children.Add(tape);

        var initialColorId = parameters.PreviewOutcome switch
        {
            TemplateOutcomeState.Success => answerId,
            TemplateOutcomeState.Failure => options.First(option => option.Id != answerId).Id,
            _ => null,
        };
        ApplyColor(initialColorId);

        return ChoiceTemplatePresentation.Compose(
            "ColorSwatch",
            instruction,
            stage,
            options,
            answerId,
            (option, index) =>
            {
                var chip = new StackPanel
                {
                    Spacing = 4,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                chip.Children.Add(new Border
                {
                    Width = 74,
                    Height = 26,
                    Background = Pigment(swatchColors[option.Id]),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Opacity = 0.9,
                });
                chip.Children.Add(new TextBlock
                {
                    Text = option.Label,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                });
                var card = new PaperCard
                {
                    Width = 128,
                    Height = 64,
                    Padding = new Thickness(4),
                    Content = chip,
                };
                card.Classes.Add("soft");
                PaperStage.SetLayerTransform(
                    card,
                    TemplateRendering.Transform(0, 0, index % 2 == 0 ? -1 : 1, 1));
                return card;
            },
            imageCache,
            [image is not null ? assetReference : null, backdropRendered ? backdropReference : null],
            parameters.UseTextOnlyFallback,
            $"Text-only object: {objectName}. Color choices: {string.Join(", ", options.Select(option => option.Label))}.",
            parameters.PreviewOutcome,
            shouldReduceMotion,
            OutcomeCopy,
            [tape, colorPatch],
            choicePanel =>
            {
                colorPatch.RenderTransform = TemplateRendering.Transform(-18, 8, -2, 0.97);
                choicePanel.RenderTransform = TemplateRendering.Transform(0, 10, 0, 0.98);
            },
            choicePanel =>
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(650), colorPatch, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(560), choicePanel, 0, 0, 0, 1),
            ],
            tape.SkipEntrance,
            reportOutcome,
            (_, outcome) => ApplyColor(outcome.ResponseId));

        void ApplyColor(string? colorId)
        {
            if (colorId is null || !swatchColors.TryGetValue(colorId, out var colorValue))
            {
                colorPatch.Background = Brushes.Transparent;
                AutomationProperties.SetName(
                    colorPatch,
                    $"Object card for {objectName}. No pigment applied yet");
                return;
            }

            colorPatch.Background = Pigment(colorValue);
            var label = options.Single(option => option.Id == colorId).Label;
            AutomationProperties.SetName(
                colorPatch,
                $"Object card for {objectName}. Applied color {label}");
        }
    }

    private static IBrush Pigment(string value) =>
        Color.TryParse(value, out var color)
            ? new SolidColorBrush(color)
            : Brushes.Transparent;

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Applied. The authored color term matches the target pigment.",
        TemplateOutcomeState.Uncertain => "Choose one paint chip before applying its pigment.",
        TemplateOutcomeState.Failure => "That pigment does not match the authored target yet.",
        _ => "Ready: choose a paint chip for the object card.",
    };
}

internal static class NumberTilesRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var quantityDescription = TemplateRendering.Text(parameters, "quantity-description");
        var pieces = TemplateRendering.Options(parameters, "pieces");
        var options = TemplateRendering.Options(parameters, "options");
        var answerId = TemplateRendering.Text(parameters, "answer");
        var assetReference = TemplateRendering.AssetReference(parameters, "asset");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var image = parameters.UseTextOnlyFallback
            ? null
            : TemplateRendering.CreateContentImage(imageCache, assetReference, 100);
        var stage = TemplateRendering.CreateStage(282, $"Quantity scene. {quantityDescription}");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var sceneCopy = new StackPanel
        {
            Spacing = 9,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (image is not null)
        {
            sceneCopy.Children.Add(image);
        }

        var piecePanel = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            ItemWidth = 46,
            ItemHeight = 46,
        };
        foreach (var (piece, index) in pieces.Select((piece, index) => (piece, index)))
        {
            var block = new CutoutFrame
            {
                Width = 38,
                Height = 38,
                Margin = new Thickness(4),
                Content = new TextBlock
                {
                    Text = "●",
                    FontSize = 18,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };
            block.Classes.Add(index % 2 == 0 ? "tilt-left" : "tilt-right");
            AutomationProperties.SetName(block, piece.Label);
            piecePanel.Children.Add(block);
        }

        sceneCopy.Children.Add(piecePanel);
        var quantityCard = new PaperCard
        {
            Width = 356,
            Height = 204,
            Padding = new Thickness(18, 14),
            Content = sceneCopy,
            RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        };
        quantityCard.Classes.Add("soft");
        AutomationProperties.SetName(
            quantityCard,
            $"Authored quantity scene. {quantityDescription}");
        PaperStage.SetLayer(quantityCard, PaperStageLayer.Subject);
        PaperStage.SetAnchor(quantityCard, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(quantityCard, 0.5);
        stage.Children.Add(quantityCard);
        var tape = new PaperTape { Content = "COUNT, THEN CHOOSE", Angle = 1.3 };
        PaperStage.SetLayer(tape, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);

        return ChoiceTemplatePresentation.Compose(
            "NumberTiles",
            instruction,
            stage,
            options,
            answerId,
            (option, index) =>
            {
                var card = new PaperCard
                {
                    Width = 94,
                    Height = 58,
                    Content = new TextBlock
                    {
                        Text = option.Label,
                        FontSize = 28,
                        FontWeight = FontWeight.Bold,
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                };
                card.Classes.Add("soft");
                PaperStage.SetLayerTransform(
                    card,
                    TemplateRendering.Transform(0, 0, index % 2 == 0 ? -1.4 : 1.4, 1));
                return card;
            },
            imageCache,
            [image is not null ? assetReference : null, backdropRendered ? backdropReference : null],
            parameters.UseTextOnlyFallback,
            $"Text-only quantity: {quantityDescription}. Digit choices: {string.Join(", ", options.Select(option => option.Label))}.",
            parameters.PreviewOutcome,
            shouldReduceMotion,
            OutcomeCopy,
            [tape, quantityCard],
            choicePanel =>
            {
                quantityCard.RenderTransform = TemplateRendering.Transform(-16, 8, -1.5, 0.97);
                choicePanel.RenderTransform = TemplateRendering.Transform(0, 10, 0, 0.98);
            },
            choicePanel =>
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(720), quantityCard, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(540), choicePanel, 0, 0, 0, 1),
            ],
            tape.SkipEntrance,
            reportOutcome);
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Counted. The digit tile matches the authored quantity.",
        TemplateOutcomeState.Uncertain => "Choose one digit tile after counting every block.",
        TemplateOutcomeState.Failure => "That digit does not match this quantity yet.",
        _ => "Ready: count the blocks and choose a digit tile.",
    };
}

internal static class LabelTheSceneRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var targetLabel = TemplateRendering.Text(parameters, "target-label");
        var hotspots = TemplateRendering.Options(parameters, "hotspots");
        var answerId = TemplateRendering.Text(parameters, "answer");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var replayButton = new Button { Content = "Replay scene", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "LabelTheSceneReplay");
        AutomationProperties.SetName(replayButton, "Replay the busy scene entrance");
        var skipButton = new Button { Content = "Skip entrance", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "LabelTheSceneSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed busy scene");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Scene label instruction. {instruction}");
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actions.Children.Add(replayButton);
        actions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(actions, 1);
        header.Children.Add(actions);

        var stage = TemplateRendering.CreateStage(350, $"Busy scene. Find {targetLabel}");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var targetTape = new PaperTape { Content = $"FIND {targetLabel.ToUpperInvariant()}", Angle = -1.3 };
        PaperStage.SetLayer(targetTape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(targetTape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(targetTape, 0.5);
        PaperStage.SetAnchorOffsetY(targetTape, -12);
        stage.Children.Add(targetTape);
        var revealedLabel = new PaperTape { Content = "CHOOSE A HOTSPOT", Angle = 1.2 };
        AutomationProperties.SetAutomationId(revealedLabel, "LabelTheSceneRevealedLabel");
        AutomationProperties.SetLiveSetting(revealedLabel, AutomationLiveSetting.Polite);
        AutomationProperties.SetName(revealedLabel, "No hotspot label revealed yet");
        PaperStage.SetLayer(revealedLabel, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(revealedLabel, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(revealedLabel, 0.5);
        PaperStage.SetAnchorOffsetY(revealedLabel, -4);
        stage.Children.Add(revealedLabel);

        var anchors = new (PaperAnchorLine Line, double X, double OffsetY)[]
        {
            (PaperAnchorLine.Shoulder, 0.22, -6),
            (PaperAnchorLine.Shoulder, 0.74, 16),
            (PaperAnchorLine.Waist, 0.38, 18),
            (PaperAnchorLine.Waist, 0.9, -8),
        };
        var hotspotButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
        var renderedAssets = new List<string?>();
        var selectedId = InitialSelection(parameters.PreviewOutcome, hotspots, answerId);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        foreach (var (hotspot, index) in hotspots.Take(anchors.Length).Select((hotspot, index) => (hotspot, index)))
        {
            var image = parameters.UseTextOnlyFallback
                ? null
                : TemplateRendering.CreateContentImage(imageCache, hotspot.AssetReferenceId, 68);
            if (image is not null)
            {
                renderedAssets.Add(hotspot.AssetReferenceId);
            }

            var frame = new CutoutFrame
            {
                Width = 116,
                Height = 88,
                Content = image as Control ?? new TextBlock
                {
                    Text = hotspot.Label,
                    FontWeight = FontWeight.Bold,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };
            frame.Classes.Add(index % 2 == 0 ? "tilt-left" : "tilt-right");
            var button = new Button
            {
                Width = 128,
                Height = 100,
                Padding = new Thickness(5),
                Content = frame,
                Classes = { "quiet", "lift" },
            };
            AutomationProperties.SetAutomationId(button, $"LabelTheSceneHotspot_{hotspot.Id}");
            AutomationProperties.SetName(button, $"Hotspot {index + 1}, {hotspot.Label}");
            button.Click += (_, _) =>
            {
                selectedId = hotspot.Id;
                UpdateSelection();
                revealedLabel.Content = hotspot.Label.ToUpperInvariant();
                AutomationProperties.SetName(revealedLabel, $"Revealed label {hotspot.Label}");
                var outcome = TemplateInteractionEvaluator.EvaluateSingleSelection(
                    hotspots,
                    answerId,
                    selectedId);
                TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
                reportOutcome(outcome);
            };
            var anchor = anchors[index];
            PaperStage.SetLayer(button, PaperStageLayer.Subject);
            PaperStage.SetAnchor(button, anchor.Line);
            PaperStage.SetAnchorX(button, anchor.X);
            PaperStage.SetAnchorOffsetY(button, anchor.OffsetY);
            stage.Children.Add(button);
            hotspotButtons.Add(hotspot.Id, button);
        }

        UpdateSelection();
        if (selectedId is not null)
        {
            var selected = hotspots.Single(hotspot => hotspot.Id == selectedId);
            revealedLabel.Content = selected.Label.ToUpperInvariant();
            AutomationProperties.SetName(revealedLabel, $"Revealed label {selected.Label}");
        }

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = $"Text-only scene. Target: {targetLabel}. Hotspots: {string.Join(", ", hotspots.Select(hotspot => hotspot.Label))}.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(stage);
        if (!parameters.UseTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                renderedAssets.Append(backdropRendered ? backdropReference : null),
                "LabelTheSceneImageCredits") is { } credits)
        {
            root.Children.Add(credits);
        }

        root.Children.Add(outcomePanel);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            var controls = hotspotButtons.Values.Cast<Control>()
                .Prepend(targetTape)
                .Append(revealedLabel)
                .ToArray();
            TemplateRendering.Prepare(shouldReduceMotion, controls);
            if (!shouldReduceMotion)
            {
                foreach (var (button, index) in hotspotButtons.Values.Select((button, index) => (button, index)))
                {
                    button.RenderTransform = TemplateRendering.Transform(
                        index % 2 == 0 ? -18 : 18,
                        8,
                        index % 2 == 0 ? -2 : 2,
                        0.96);
                }
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), targetTape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(760), hotspotButtons.Values.ElementAt(0), 0, 0, -1, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(240), hotspotButtons.Values.ElementAt(1), 0, 0, 1, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(240), hotspotButtons.Values.ElementAt(2), 0, 0, -1, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(240), hotspotButtons.Values.ElementAt(3), 0, 0, 1, 1),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), revealedLabel),
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
            targetTape.SkipEntrance();
            revealedLabel.SkipEntrance();
        };
        replayButton.Click += async (_, _) => await PlayAsync();
        return root;

        void UpdateSelection()
        {
            foreach (var pair in hotspotButtons)
            {
                pair.Value.Classes.Remove("primary");
                if (pair.Key == selectedId)
                {
                    pair.Value.Classes.Add("primary");
                }
            }
        }
    }

    private static string? InitialSelection(
        TemplateOutcomeState state,
        IReadOnlyList<TemplateOption> hotspots,
        string answerId) => state switch
        {
            TemplateOutcomeState.Success => answerId,
            TemplateOutcomeState.Failure => hotspots.First(hotspot => hotspot.Id != answerId).Id,
            _ => null,
        };

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Found it. The revealed label matches the target.",
        TemplateOutcomeState.Uncertain => "Choose one hotspot to reveal its complete label.",
        TemplateOutcomeState.Failure => "That label names a different part of the scene.",
        _ => "Ready: explore the tabbable hotspots and find the target.",
    };
}
