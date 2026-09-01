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

internal static class ConstructionTemplatePresentation
{
    public static Grid CreateHeader(
        string prefix,
        string instruction,
        string replayLabel,
        string skipLabel,
        out Button replayButton,
        out Button skipButton)
    {
        replayButton = new Button { Content = replayLabel, Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, $"{prefix}Replay");
        AutomationProperties.SetName(replayButton, $"Replay {replayLabel.ToLowerInvariant()}");
        skipButton = new Button { Content = skipLabel, Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, $"{prefix}Skip");
        AutomationProperties.SetName(skipButton, "Skip to the completed construction");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Construction instruction. {instruction}");
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actions.Children.Add(replayButton);
        actions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(actions, 1);
        header.Children.Add(actions);
        return header;
    }

    public static StackPanel CreateRoot(
        Control header,
        PaperStage stage,
        Control? supplemental,
        Border outcomePanel,
        ContentImageCache? imageCache,
        IEnumerable<string?> creditReferences,
        string creditAutomationId,
        bool useTextOnlyFallback,
        string textOnlyCopy)
    {
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
        if (supplemental is not null)
        {
            root.Children.Add(supplemental);
        }

        if (!useTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                creditReferences,
                creditAutomationId) is { } credits)
        {
            root.Children.Add(credits);
        }

        root.Children.Add(outcomePanel);
        return root;
    }

    public static void AttachChoreography(
        Control root,
        Button replayButton,
        Button skipButton,
        bool shouldReduceMotion,
        IReadOnlyList<Control> animatedControls,
        Action prepareMotion,
        Func<IReadOnlyList<PaperChoreographyStep>> createSteps,
        Action skipExtra)
    {
        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, animatedControls.ToArray());
            if (!shouldReduceMotion)
            {
                prepareMotion();
            }

            scene = new PaperChoreography(createSteps());
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
    }
}

internal static class GapCardRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var sentenceBefore = TemplateRendering.Text(parameters, "sentence-before");
        var sentenceAfter = TemplateRendering.Text(parameters, "sentence-after");
        var options = TemplateRendering.Options(parameters, "options");
        var answerId = TemplateRendering.Text(parameters, "answer");
        var header = ConstructionTemplatePresentation.CreateHeader(
            "GapCard",
            instruction,
            "Replay tiles",
            "Skip tiles",
            out var replayButton,
            out var skipButton);

        var stage = TemplateRendering.CreateStage(280, "Gap card cloze construction stage");
        TemplateRendering.AddBackdrop(stage, imageCache: null, assetReferenceId: null);
        var tape = new PaperTape { Content = "COMPLETE THE GAP", Angle = -1.2 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);

        var gapButton = new Button
        {
            Content = "______",
            MinWidth = 130,
            Classes = { "quiet", "lift" },
        };
        AutomationProperties.SetAutomationId(gapButton, "GapCardTarget");
        AutomationProperties.SetName(gapButton, "Sentence gap. Select a tile, then fill this gap");
        KeyboardNavigation.SetTabIndex(gapButton, 20);
        var sentence = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        sentence.Children.Add(new TextBlock
        {
            Text = sentenceBefore,
            FontSize = 24,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        });
        sentence.Children.Add(gapButton);
        sentence.Children.Add(new TextBlock
        {
            Text = sentenceAfter,
            FontSize = 24,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        });
        var sentenceCard = new PaperCard
        {
            Margin = new Thickness(44, 76, 44, 118),
            Padding = new Thickness(18, 14),
            Content = sentence,
        };
        sentenceCard.Classes.Add("soft");
        AutomationProperties.SetName(
            sentenceCard,
            $"Cloze sentence. {sentenceBefore}, gap, {sentenceAfter}");
        PaperStage.SetLayer(sentenceCard, PaperStageLayer.SupportingCast);
        stage.Children.Add(sentenceCard);

        var tilePanel = new WrapPanel
        {
            Margin = new Thickness(34, 176, 34, 18),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            ItemWidth = 144,
            ItemHeight = 66,
        };
        PaperStage.SetLayer(tilePanel, PaperStageLayer.Subject);
        stage.Children.Add(tilePanel);
        var buttons = new Dictionary<string, Button>(StringComparer.Ordinal);
        string? selectedId = parameters.PreviewOutcome switch
        {
            TemplateOutcomeState.Success => answerId,
            TemplateOutcomeState.Failure => options.First(option => option.Id != answerId).Id,
            _ => null,
        };
        var status = new TextBlock
        {
            Text = selectedId is null
                ? "Select a tile, then choose the sentence gap."
                : $"Selected {options.Single(option => option.Id == selectedId).Label}.",
            Classes = { "muted" },
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(status, "GapCardStatus");
        AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);

        foreach (var (option, index) in options.Select((option, index) => (option, index)))
        {
            var button = new Button
            {
                Width = 132,
                Height = 56,
                Margin = new Thickness(5),
                Content = new PaperTape
                {
                    Content = option.Label,
                    Angle = index % 2 == 0 ? -1.1 : 1.1,
                },
                Classes = { "quiet", "lift" },
            };
            AutomationProperties.SetAutomationId(button, $"GapCardTile_{option.Id}");
            AutomationProperties.SetName(button, $"Select tile {option.Label}");
            KeyboardNavigation.SetTabIndex(button, 10 + index);
            button.Click += (_, _) => Select(option.Id);
            PointerPressedEventArgs? dragStartArgs = null;
            Point? dragStartPoint = null;
            button.PointerPressed += (_, args) =>
            {
                if (args.GetCurrentPoint(button).Properties.IsLeftButtonPressed)
                {
                    dragStartArgs = args;
                    dragStartPoint = args.GetPosition(button);
                }
            };
            button.PointerMoved += async (_, args) =>
            {
                if (dragStartArgs is null || dragStartPoint is null)
                {
                    return;
                }

                var current = args.GetPosition(button);
                if (Math.Abs(current.X - dragStartPoint.Value.X) < 7 &&
                    Math.Abs(current.Y - dragStartPoint.Value.Y) < 7)
                {
                    return;
                }

                var pointerPressedArgs = dragStartArgs;
                dragStartArgs = null;
                dragStartPoint = null;
                var transfer = new DataTransfer();
                transfer.Add(DataTransferItem.CreateText(option.Id));
                await DragDrop.DoDragDropAsync(pointerPressedArgs, transfer, DragDropEffects.Move);
            };
            button.PointerReleased += (_, _) =>
            {
                dragStartArgs = null;
                dragStartPoint = null;
            };
            buttons.Add(option.Id, button);
            tilePanel.Children.Add(button);
        }

        UpdateSelection();
        gapButton.Click += (_, _) => Submit(selectedId);
        DragDrop.SetAllowDrop(gapButton, true);
        DragDrop.AddDragOverHandler(gapButton, (_, args) =>
        {
            args.DragEffects = options.Any(option =>
                string.Equals(option.Id, args.DataTransfer.TryGetText(), StringComparison.Ordinal))
                ? DragDropEffects.Move
                : DragDropEffects.None;
        });
        DragDrop.AddDropHandler(gapButton, (_, args) =>
        {
            var optionId = args.DataTransfer.TryGetText();
            if (optionId is not null && options.Any(option => option.Id == optionId))
            {
                Select(optionId);
                Submit(optionId);
                args.DragEffects = DragDropEffects.Move;
            }
        });

        var root = ConstructionTemplatePresentation.CreateRoot(
            header,
            stage,
            status,
            outcomePanel,
            imageCache,
            [],
            "GapCardImageCredits",
            parameters.UseTextOnlyFallback,
            $"Text-only cloze. Sentence: {sentenceBefore} [gap] {sentenceAfter.TrimEnd('.', '!', '?')}. Choices: {string.Join(", ", options.Select(option => option.Label))}.");
        ConstructionTemplatePresentation.AttachChoreography(
            root,
            replayButton,
            skipButton,
            shouldReduceMotion,
            [tape, sentenceCard, tilePanel],
            () =>
            {
                sentenceCard.RenderTransform = TemplateRendering.Transform(-18, 4, -1, 0.98);
                tilePanel.RenderTransform = TemplateRendering.Transform(20, 0, 1.2, 0.98);
            },
            () =>
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(620), sentenceCard, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(520), tilePanel, 0, 0, 0, 1),
            ],
            tape.SkipEntrance);
        return root;

        void Select(string optionId)
        {
            selectedId = optionId;
            UpdateSelection();
            var option = options.Single(candidate => candidate.Id == optionId);
            gapButton.Content = option.Label;
            status.Text = $"Selected {option.Label}. Choose the gap to check.";
        }

        void Submit(string? optionId)
        {
            var outcome = TemplateInteractionEvaluator.EvaluateSingleSelection(
                options,
                answerId,
                optionId);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            status.Text = outcome.State switch
            {
                TemplateOutcomeState.Success => "The authored tile completes the gap.",
                TemplateOutcomeState.Failure => "That tile fits the space, but not the authored answer.",
                _ => "Select a tile before checking the gap.",
            };
            reportOutcome(outcome);
        }

        void UpdateSelection()
        {
            foreach (var pair in buttons)
            {
                pair.Value.Classes.Remove("primary");
                if (string.Equals(pair.Key, selectedId, StringComparison.Ordinal))
                {
                    pair.Value.Classes.Add("primary");
                }
            }

            gapButton.Content = selectedId is null
                ? "______"
                : options.Single(option => option.Id == selectedId).Label;
        }
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The authored tile completes the sentence.",
        TemplateOutcomeState.Uncertain => "Choose one tile before checking the gap.",
        TemplateOutcomeState.Failure => "The sentence needs a different tile in this gap.",
        _ => "Ready: choose a tile for the sentence gap.",
    };
}

internal static class SentenceFoldRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var segments = TemplateRendering.Options(parameters, "segments");
        var header = ConstructionTemplatePresentation.CreateHeader(
            "SentenceFold",
            instruction,
            "Replay fold",
            "Skip fold",
            out var replayButton,
            out var skipButton);
        var stage = TemplateRendering.CreateStage(276, "Accordion sentence fold stage");
        TemplateRendering.AddBackdrop(stage, imageCache: null, assetReferenceId: null);
        var tape = new PaperTape { Content = "UNFOLD THE SENTENCE", Angle = 1.1 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);

        var accordion = new WrapPanel
        {
            Margin = new Thickness(28, 94, 28, 26),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            ItemWidth = 162,
            ItemHeight = 112,
        };
        AutomationProperties.SetName(accordion, "Authored sentence sections in order");
        PaperStage.SetLayer(accordion, PaperStageLayer.Subject);
        stage.Children.Add(accordion);
        var segmentCards = new List<PaperCard>();
        foreach (var (segment, index) in segments.Select((segment, index) => (segment, index)))
        {
            var card = new PaperCard
            {
                Width = 150,
                Height = 100,
                Margin = new Thickness(index == 0 ? 0 : -8, 5, 0, 5),
                Padding = new Thickness(12, 9),
            };
            card.Classes.Add(index % 2 == 0 ? "tilt-left" : "tilt-right");
            AutomationProperties.SetAutomationId(card, $"SentenceFoldSegment_{segment.Id}");
            segmentCards.Add(card);
            accordion.Children.Add(card);
        }

        var visibleCount = parameters.PreviewOutcome switch
        {
            TemplateOutcomeState.Success => segments.Count,
            TemplateOutcomeState.Uncertain => Math.Max(1, segments.Count / 2),
            _ => 1,
        };
        var nextButton = new Button
        {
            Classes = { "primary", "lift" },
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        AutomationProperties.SetAutomationId(nextButton, "SentenceFoldNext");
        var status = new TextBlock
        {
            Classes = { "muted" },
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(status, "SentenceFoldStatus");
        AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
        var controls = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        controls.Children.Add(status);
        Grid.SetColumn(nextButton, 1);
        controls.Children.Add(nextButton);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        Refresh();

        PaperChoreography? foldScene = null;
        nextButton.Click += async (_, _) =>
        {
            if (visibleCount >= segments.Count)
            {
                return;
            }

            var revealedCard = segmentCards[visibleCount];
            visibleCount++;
            Refresh();
            foldScene?.Skip();
            foldScene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, revealedCard);
            if (!shouldReduceMotion)
            {
                revealedCard.RenderTransform = TemplateRendering.Transform(-14, 0, -3, 0.96);
            }

            foldScene = new PaperChoreography(
            [
                TemplateRendering.Move(
                    TimeSpan.FromMilliseconds(420),
                    revealedCard,
                    0,
                    0,
                    visibleCount % 2 == 0 ? -1 : 1,
                    1),
            ]);
            await foldScene.PlayAsync(shouldReduceMotion);
            if (visibleCount == segments.Count)
            {
                var outcome = TemplateInteractionEvaluator.EvaluateAcknowledgement(acknowledged: true);
                TemplateRendering.ApplyOutcome(
                    outcomePanel,
                    outcomeText,
                    outcome.State,
                    OutcomeCopy);
                status.Text = "The complete authored sentence is unfolded.";
                reportOutcome(outcome);
            }
        };

        var root = ConstructionTemplatePresentation.CreateRoot(
            header,
            stage,
            controls,
            outcomePanel,
            imageCache,
            [],
            "SentenceFoldImageCredits",
            parameters.UseTextOnlyFallback,
            $"Text-only sentence: {string.Join(" ", segments.Select(segment => segment.Label))} Every section remains available in order.");
        root.DetachedFromVisualTree += (_, _) =>
        {
            foldScene?.Skip();
            foldScene?.Dispose();
            foldScene = null;
        };
        ConstructionTemplatePresentation.AttachChoreography(
            root,
            replayButton,
            skipButton,
            shouldReduceMotion,
            [tape, accordion, controls],
            () =>
            {
                accordion.RenderTransform = TemplateRendering.Transform(-24, 0, -1.4, 0.97);
                controls.RenderTransform = TemplateRendering.Transform(0, 10, 0, 0.98);
            },
            () =>
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(760), accordion, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(420), controls, 0, 0, 0, 1),
            ],
            tape.SkipEntrance);
        return root;

        void Refresh()
        {
            for (var index = 0; index < segments.Count; index++)
            {
                var segment = segments[index];
                var revealed = index < visibleCount;
                var copy = new StackPanel { Spacing = 5 };
                copy.Children.Add(new TextBlock
                {
                    Text = $"SECTION {index + 1}",
                    Classes = { "eyebrow" },
                    HorizontalAlignment = HorizontalAlignment.Center,
                });
                copy.Children.Add(new TextBlock
                {
                    Text = revealed ? segment.Label : "FOLDED",
                    FontSize = revealed ? 18 : 12,
                    FontWeight = FontWeight.Bold,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                });
                segmentCards[index].Content = copy;
                AutomationProperties.SetName(
                    segmentCards[index],
                    revealed
                        ? $"Sentence section {index + 1}: {segment.Label}"
                        : $"Sentence section {index + 1}, folded");
            }

            var complete = visibleCount >= segments.Count;
            nextButton.IsEnabled = !complete;
            nextButton.Content = complete ? "Sentence unfolded" : "Unfold next section";
            AutomationProperties.SetName(
                nextButton,
                complete
                    ? "Every authored sentence section is unfolded"
                    : $"Unfold sentence section {visibleCount + 1}");
            status.Text = complete
                ? "Every authored section is visible."
                : $"{visibleCount} of {segments.Count} sections visible.";
        }
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The complete authored sentence is visible in order.",
        TemplateOutcomeState.Uncertain => "More sentence sections remain folded.",
        TemplateOutcomeState.Failure => "The authored sequence is not complete yet.",
        _ => "Ready: unfold the sentence one section at a time.",
    };
}

internal static class ConjugationWheelRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var lemma = TemplateRendering.Text(parameters, "lemma");
        var persons = TemplateRendering.Options(parameters, "persons");
        var forms = TemplateRendering.Options(parameters, "forms");
        var answers = TemplateRendering.Options(parameters, "answers")
            .ToDictionary(answer => answer.Id, answer => answer.Label, StringComparer.Ordinal);
        var personIndex = 0;
        var expectedFormId = answers[persons[personIndex].Id];
        var expectedFormIndex = forms
            .Select((form, index) => (form, index))
            .Single(pair => pair.form.Id == expectedFormId)
            .index;
        var formIndex = parameters.PreviewOutcome switch
        {
            TemplateOutcomeState.Success => expectedFormIndex,
            TemplateOutcomeState.Failure => Enumerable.Range(0, forms.Count)
                .First(index => index != expectedFormIndex),
            _ => 0,
        };
        var header = ConstructionTemplatePresentation.CreateHeader(
            "ConjugationWheel",
            instruction,
            "Replay wheels",
            "Skip wheels",
            out var replayButton,
            out var skipButton);
        var stage = TemplateRendering.CreateStage(324, $"Conjugation wheel for {lemma}");
        TemplateRendering.AddBackdrop(stage, imageCache: null, assetReferenceId: null);
        var tape = new PaperTape { Content = lemma.ToUpperInvariant(), Angle = -1.3 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);

        var personText = new TextBlock
        {
            FontSize = 30,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var personCopy = new StackPanel { Spacing = 8 };
        personCopy.Children.Add(new TextBlock
        {
            Text = "PERSON",
            Classes = { "eyebrow" },
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        personCopy.Children.Add(personText);
        var personWheel = new Border
        {
            Width = 184,
            Height = 184,
            CornerRadius = new CornerRadius(92),
            Padding = new Thickness(20),
            Child = personCopy,
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
        };
        personWheel.Classes.Add("soft-card");

        var formText = new TextBlock
        {
            FontSize = 30,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var formCopy = new StackPanel { Spacing = 8 };
        formCopy.Children.Add(new TextBlock
        {
            Text = "VERB FORM",
            Classes = { "eyebrow" },
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        formCopy.Children.Add(formText);
        var formWheel = new Border
        {
            Width = 184,
            Height = 184,
            CornerRadius = new CornerRadius(92),
            Padding = new Thickness(20),
            Child = formCopy,
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
        };
        formWheel.Classes.Add("soft-card");
        var alignment = new TextBlock
        {
            Text = "+",
            FontSize = 28,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        AutomationProperties.SetName(alignment, "aligned with");
        var wheels = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 18,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(24, 76, 24, 24),
        };
        wheels.Children.Add(personWheel);
        wheels.Children.Add(alignment);
        wheels.Children.Add(formWheel);
        PaperStage.SetLayer(wheels, PaperStageLayer.Subject);
        stage.Children.Add(wheels);

        var nextPerson = new Button { Content = "Next person", Classes = { "quiet", "lift" } };
        AutomationProperties.SetAutomationId(nextPerson, "ConjugationWheelNextPerson");
        AutomationProperties.SetName(nextPerson, "Rotate to the next person");
        var nextForm = new Button { Content = "Next form", Classes = { "quiet", "lift" } };
        AutomationProperties.SetAutomationId(nextForm, "ConjugationWheelNextForm");
        AutomationProperties.SetName(nextForm, "Rotate to the next verb form");
        var checkButton = new Button { Content = "Check alignment", Classes = { "primary", "lift" } };
        AutomationProperties.SetAutomationId(checkButton, "ConjugationWheelCheck");
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        buttons.Children.Add(nextPerson);
        buttons.Children.Add(nextForm);
        buttons.Children.Add(checkButton);
        var status = new TextBlock
        {
            Classes = { "muted" },
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(status, "ConjugationWheelStatus");
        AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
        var controls = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        controls.Children.Add(status);
        Grid.SetColumn(buttons, 1);
        controls.Children.Add(buttons);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        Refresh();

        PaperChoreography? wheelScene = null;
        nextPerson.Click += async (_, _) =>
        {
            personIndex = (personIndex + 1) % persons.Count;
            await RotateAsync(personWheel, -11);
            Refresh();
        };
        nextForm.Click += async (_, _) =>
        {
            formIndex = (formIndex + 1) % forms.Count;
            await RotateAsync(formWheel, 11);
            Refresh();
        };
        checkButton.Click += (_, _) =>
        {
            var outcome = TemplateInteractionEvaluator.EvaluateMappedPair(
                persons,
                forms,
                answers,
                persons[personIndex].Id,
                forms[formIndex].Id);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            status.Text = outcome.State == TemplateOutcomeState.Success
                ? "The authored person and verb form are aligned."
                : "Rotate one wheel and compare the alignment again.";
            reportOutcome(outcome);
        };

        var root = ConstructionTemplatePresentation.CreateRoot(
            header,
            stage,
            controls,
            outcomePanel,
            imageCache,
            [],
            "ConjugationWheelImageCredits",
            parameters.UseTextOnlyFallback,
            $"Text-only wheel. Persons: {string.Join(", ", persons.Select(person => person.Label))}. Forms: {string.Join(", ", forms.Select(form => form.Label))}.");
        root.DetachedFromVisualTree += (_, _) =>
        {
            wheelScene?.Skip();
            wheelScene?.Dispose();
            wheelScene = null;
        };
        ConstructionTemplatePresentation.AttachChoreography(
            root,
            replayButton,
            skipButton,
            shouldReduceMotion,
            [tape, personWheel, formWheel, controls],
            () =>
            {
                personWheel.RenderTransform = TemplateRendering.Transform(-20, 0, -8, 0.96);
                formWheel.RenderTransform = TemplateRendering.Transform(20, 0, 8, 0.96);
                controls.RenderTransform = TemplateRendering.Transform(0, 10, 0, 0.98);
            },
            () =>
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(640), personWheel, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(640), formWheel, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(380), controls, 0, 0, 0, 1),
            ],
            tape.SkipEntrance);
        return root;

        async Task RotateAsync(Control wheel, double angle)
        {
            wheelScene?.Skip();
            wheelScene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, wheel);
            if (!shouldReduceMotion)
            {
                wheel.RenderTransform = TemplateRendering.Transform(0, 0, angle, 0.97);
            }

            wheelScene = new PaperChoreography(
            [
                TemplateRendering.Move(TimeSpan.FromMilliseconds(300), wheel, 0, 0, 0, 1),
            ]);
            await wheelScene.PlayAsync(shouldReduceMotion);
        }

        void Refresh()
        {
            var person = persons[personIndex];
            var form = forms[formIndex];
            personText.Text = person.Label;
            formText.Text = form.Label;
            AutomationProperties.SetName(personWheel, $"Person wheel: {person.Label}");
            AutomationProperties.SetName(formWheel, $"Verb form wheel: {form.Label}");
            status.Text = $"Current alignment: {person.Label} {form.Label}.";
        }
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The wheels show the authored person and verb form pair.",
        TemplateOutcomeState.Uncertain => "Rotate both wheels before checking the pair.",
        TemplateOutcomeState.Failure => "The two wheels need a different alignment.",
        _ => "Ready: align one person with one authored verb form.",
    };
}

internal static class CaseSwitchboardRenderer
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
        var roles = TemplateRendering.Options(parameters, "roles");
        var articles = TemplateRendering.Options(parameters, "articles");
        var answers = TemplateRendering.Options(parameters, "answers")
            .ToDictionary(answer => answer.Id, answer => answer.Label, StringComparer.Ordinal);
        string? selectedRoleId = parameters.PreviewOutcome is TemplateOutcomeState.Success or
            TemplateOutcomeState.Failure
            ? roles[0].Id
            : null;
        var expectedArticleId = answers[roles[0].Id];
        var expectedArticleIndex = articles
            .Select((article, index) => (article, index))
            .Single(pair => pair.article.Id == expectedArticleId)
            .index;
        var articleIndex = parameters.PreviewOutcome switch
        {
            TemplateOutcomeState.Success => expectedArticleIndex,
            TemplateOutcomeState.Failure => Enumerable.Range(0, articles.Count)
                .First(index => index != expectedArticleIndex),
            _ => 0,
        };
        var header = ConstructionTemplatePresentation.CreateHeader(
            "CaseSwitchboard",
            instruction,
            "Replay switchboard",
            "Skip switchboard",
            out var replayButton,
            out var skipButton);
        var stage = TemplateRendering.CreateStage(310, $"Case switchboard for {noun}");
        TemplateRendering.AddBackdrop(stage, imageCache: null, assetReferenceId: null);
        var tape = new PaperTape { Content = "SWITCH THE ROLE", Angle = 1.2 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);

        var roleButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
        var rolePanel = new WrapPanel
        {
            Margin = new Thickness(34, 78, 34, 158),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            ItemWidth = 182,
            ItemHeight = 64,
        };
        AutomationProperties.SetName(rolePanel, "Sentence role switches");
        foreach (var (role, index) in roles.Select((role, index) => (role, index)))
        {
            var button = new Button
            {
                Width = 170,
                Height = 54,
                Margin = new Thickness(5),
                Content = new PaperTape
                {
                    Content = role.Label,
                    Angle = index % 2 == 0 ? -1 : 1,
                },
                Classes = { "quiet", "lift" },
            };
            AutomationProperties.SetAutomationId(button, $"CaseSwitchboardRole_{role.Id}");
            AutomationProperties.SetName(button, $"Choose sentence role {role.Label}");
            roleButtons.Add(role.Id, button);
            rolePanel.Children.Add(button);
        }

        PaperStage.SetLayer(rolePanel, PaperStageLayer.SupportingCast);
        stage.Children.Add(rolePanel);
        var articleButton = new Button
        {
            Width = 128,
            Height = 84,
            Padding = new Thickness(6),
            Classes = { "quiet", "lift" },
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
        };
        AutomationProperties.SetAutomationId(articleButton, "CaseSwitchboardArticle");
        var nounCard = new CutoutFrame
        {
            Width = 220,
            Height = 104,
            Padding = new Thickness(14),
            Content = new TextBlock
            {
                Text = noun,
                FontSize = 30,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        AutomationProperties.SetName(nounCard, $"Noun card: {noun}");
        var phrase = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Margin = new Thickness(90, 174, 90, 22),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
        };
        phrase.Children.Add(articleButton);
        phrase.Children.Add(nounCard);
        PaperStage.SetLayer(phrase, PaperStageLayer.Subject);
        stage.Children.Add(phrase);

        var checkButton = new Button
        {
            Content = "Check role and article",
            Classes = { "primary", "lift" },
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        AutomationProperties.SetAutomationId(checkButton, "CaseSwitchboardCheck");
        var status = new TextBlock
        {
            Classes = { "muted" },
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(status, "CaseSwitchboardStatus");
        AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
        var controls = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        controls.Children.Add(status);
        Grid.SetColumn(checkButton, 1);
        controls.Children.Add(checkButton);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);

        foreach (var role in roles)
        {
            roleButtons[role.Id].Click += (_, _) =>
            {
                selectedRoleId = role.Id;
                Refresh();
            };
        }

        PaperChoreography? flipScene = null;
        articleButton.Click += async (_, _) =>
        {
            articleIndex = (articleIndex + 1) % articles.Count;
            flipScene?.Skip();
            flipScene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, articleButton);
            if (!shouldReduceMotion)
            {
                articleButton.RenderTransform = TemplateRendering.Transform(0, 0, 8, 0.94);
            }

            flipScene = new PaperChoreography(
            [
                TemplateRendering.Move(
                    TimeSpan.FromMilliseconds(320),
                    articleButton,
                    0,
                    0,
                    0,
                    1),
            ]);
            await flipScene.PlayAsync(shouldReduceMotion);
            Refresh();
        };
        checkButton.Click += (_, _) =>
        {
            var outcome = TemplateInteractionEvaluator.EvaluateMappedPair(
                roles,
                articles,
                answers,
                selectedRoleId,
                articles[articleIndex].Id);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            status.Text = outcome.State switch
            {
                TemplateOutcomeState.Success => "The authored role and article form match.",
                TemplateOutcomeState.Failure => "Keep the role, then flip the article card again.",
                _ => "Choose a sentence role before checking the article.",
            };
            reportOutcome(outcome);
        };
        Refresh();

        var root = ConstructionTemplatePresentation.CreateRoot(
            header,
            stage,
            controls,
            outcomePanel,
            imageCache,
            [],
            "CaseSwitchboardImageCredits",
            parameters.UseTextOnlyFallback,
            $"Text-only switchboard. Noun: {noun}. Roles: {string.Join(", ", roles.Select(role => role.Label))}. Articles: {string.Join(", ", articles.Select(article => article.Label))}.");
        root.DetachedFromVisualTree += (_, _) =>
        {
            flipScene?.Skip();
            flipScene?.Dispose();
            flipScene = null;
        };
        ConstructionTemplatePresentation.AttachChoreography(
            root,
            replayButton,
            skipButton,
            shouldReduceMotion,
            [tape, rolePanel, phrase, controls],
            () =>
            {
                rolePanel.RenderTransform = TemplateRendering.Transform(-18, 0, -1, 0.98);
                phrase.RenderTransform = TemplateRendering.Transform(22, 0, 1.4, 0.97);
                controls.RenderTransform = TemplateRendering.Transform(0, 10, 0, 0.98);
            },
            () =>
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(560), rolePanel, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(640), phrase, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(360), controls, 0, 0, 0, 1),
            ],
            tape.SkipEntrance);
        return root;

        void Refresh()
        {
            foreach (var pair in roleButtons)
            {
                pair.Value.Classes.Remove("primary");
                if (string.Equals(pair.Key, selectedRoleId, StringComparison.Ordinal))
                {
                    pair.Value.Classes.Add("primary");
                }
            }

            var article = articles[articleIndex];
            articleButton.Content = new PaperTape
            {
                Content = article.Label,
                Angle = articleIndex % 2 == 0 ? -1.2 : 1.2,
            };
            AutomationProperties.SetName(articleButton, $"Article card: {article.Label}. Flip to next form");
            status.Text = selectedRoleId is null
                ? $"Choose a sentence role. Article card shows {article.Label}."
                : $"{roles.Single(role => role.Id == selectedRoleId).Label}. Article card shows {article.Label}.";
        }
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The article card matches the authored sentence role.",
        TemplateOutcomeState.Uncertain => "Choose a role before checking its article form.",
        TemplateOutcomeState.Failure => "The noun needs a different article card for this role.",
        _ => "Ready: choose a role and flip the article card.",
    };
}

internal static class SeparableVerbSplitRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var joinedForm = TemplateRendering.Text(parameters, "joined-form");
        var sentenceStart = TemplateRendering.Text(parameters, "sentence-start");
        var prefix = TemplateRendering.Text(parameters, "prefix");
        var isSplit = parameters.PreviewOutcome == TemplateOutcomeState.Success;
        var header = ConstructionTemplatePresentation.CreateHeader(
            "SeparableVerbSplit",
            instruction,
            "Replay split",
            "Skip split",
            out var replayButton,
            out var skipButton);
        var stage = TemplateRendering.CreateStage(286, $"Separable verb stage for {joinedForm}");
        TemplateRendering.AddBackdrop(stage, imageCache: null, assetReferenceId: null);
        var tape = new PaperTape { Content = "SEPARATE THE PREFIX", Angle = -1.2 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);

        var sentenceText = new TextBlock
        {
            FontSize = 28,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };
        var sentenceCard = new PaperCard
        {
            Width = 300,
            Height = 112,
            Padding = new Thickness(18),
            Content = sentenceText,
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
        };
        sentenceCard.Classes.Add("soft");
        PaperStage.SetLayer(sentenceCard, PaperStageLayer.Subject);
        PaperStage.SetAnchor(sentenceCard, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(sentenceCard, 0.42);
        stage.Children.Add(sentenceCard);
        var prefixCard = new PaperCard
        {
            Content = prefix,
            Width = 116,
            Height = 64,
            Padding = new Thickness(16, 10),
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
        };
        prefixCard.Classes.Add("soft");
        PaperStage.SetLayer(prefixCard, PaperStageLayer.ReactionBurst);
        PaperStage.SetAnchor(prefixCard, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(prefixCard, 0.38);
        stage.Children.Add(prefixCard);

        var splitButton = new Button
        {
            Classes = { "primary", "lift" },
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        AutomationProperties.SetAutomationId(splitButton, "SeparableVerbSplitToggle");
        var status = new TextBlock
        {
            Classes = { "muted" },
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(status, "SeparableVerbSplitStatus");
        AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
        var controls = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        controls.Children.Add(status);
        Grid.SetColumn(splitButton, 1);
        controls.Children.Add(splitButton);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        Refresh();

        PaperChoreography? splitScene = null;
        splitButton.Click += async (_, _) =>
        {
            splitScene?.Skip();
            splitScene?.Dispose();
            if (isSplit)
            {
                isSplit = false;
                Refresh();
                TemplateRendering.ApplyOutcome(
                    outcomePanel,
                    outcomeText,
                    TemplateOutcomeState.Ready,
                    OutcomeCopy);
                return;
            }

            isSplit = true;
            sentenceText.Text = sentenceStart;
            TemplateRendering.Prepare(shouldReduceMotion, prefixCard);
            if (!shouldReduceMotion)
            {
                prefixCard.RenderTransform = TemplateRendering.Transform(-24, 0, -3, 0.95);
            }

            splitScene = new PaperChoreography(
            [
                TemplateRendering.Move(
                    TimeSpan.FromMilliseconds(560),
                    prefixCard,
                    214,
                    0,
                    1.6,
                    1),
            ]);
            await splitScene.PlayAsync(shouldReduceMotion);
            Refresh();
            var outcome = TemplateInteractionEvaluator.EvaluateAcknowledgement(acknowledged: true);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            reportOutcome(outcome);
        };

        var root = ConstructionTemplatePresentation.CreateRoot(
            header,
            stage,
            controls,
            outcomePanel,
            imageCache,
            [],
            "SeparableVerbSplitImageCredits",
            parameters.UseTextOnlyFallback,
            $"Text-only split. Joined: {joinedForm}. Clause: {sentenceStart} {prefix}");
        root.DetachedFromVisualTree += (_, _) =>
        {
            splitScene?.Skip();
            splitScene?.Dispose();
            splitScene = null;
        };
        ConstructionTemplatePresentation.AttachChoreography(
            root,
            replayButton,
            skipButton,
            shouldReduceMotion,
            [tape, sentenceCard, controls],
            () =>
            {
                sentenceCard.RenderTransform = TemplateRendering.Transform(-18, 0, -1.4, 0.97);
                controls.RenderTransform = TemplateRendering.Transform(0, 10, 0, 0.98);
            },
            () =>
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(680), sentenceCard, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(380), controls, 0, 0, 0, 1),
            ],
            tape.SkipEntrance);
        return root;

        void Refresh()
        {
            sentenceText.Text = isSplit ? sentenceStart : joinedForm;
            prefixCard.Opacity = isSplit ? 1 : 0;
            prefixCard.RenderTransform = isSplit
                ? TemplateRendering.Transform(214, 0, 1.6, 1)
                : TemplateRendering.Transform(0, 0, 0, 1);
            splitButton.Content = isSplit ? "Join the verb" : "Split the verb";
            AutomationProperties.SetName(
                splitButton,
                isSplit ? $"Join {joinedForm} again" : $"Split prefix from {joinedForm}");
            AutomationProperties.SetName(
                sentenceCard,
                isSplit ? $"Clause start: {sentenceStart}" : $"Joined verb: {joinedForm}");
            AutomationProperties.SetName(prefixCard, $"Separated prefix at clause end: {prefix}");
            status.Text = isSplit
                ? $"Split clause: {sentenceStart} {prefix}"
                : $"Joined verb: {joinedForm}.";
        }
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The authored prefix now sits at the clause end.",
        TemplateOutcomeState.Uncertain => "Split the verb to expose its clause-end prefix.",
        TemplateOutcomeState.Failure => "The prefix has not reached the authored clause end.",
        _ => "Ready: split the authored verb into its clause positions.",
    };
}

internal static class QuestionFlipRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var statement = TemplateRendering.Text(parameters, "statement");
        var question = TemplateRendering.Text(parameters, "question");
        var showsQuestion = parameters.PreviewOutcome == TemplateOutcomeState.Success;
        var header = ConstructionTemplatePresentation.CreateHeader(
            "QuestionFlip",
            instruction,
            "Replay card",
            "Skip card",
            out var replayButton,
            out var skipButton);
        var stage = TemplateRendering.CreateStage(286, "Statement and question flip stage");
        TemplateRendering.AddBackdrop(stage, imageCache: null, assetReferenceId: null);
        var tape = new PaperTape { Content = "FLIP THE SENTENCE", Angle = 1.2 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);

        var sideLabel = new TextBlock
        {
            Classes = { "eyebrow" },
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        var sentenceText = new TextBlock
        {
            FontSize = 26,
            FontWeight = FontWeight.Bold,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        var cardCopy = new StackPanel { Spacing = 12 };
        cardCopy.Children.Add(sideLabel);
        cardCopy.Children.Add(sentenceText);
        var flipButton = new Button
        {
            Width = 510,
            Height = 150,
            Padding = new Thickness(26, 18),
            Content = cardCopy,
            Classes = { "quiet", "lift" },
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
        };
        AutomationProperties.SetAutomationId(flipButton, "QuestionFlipToggle");
        PaperStage.SetLayer(flipButton, PaperStageLayer.Subject);
        PaperStage.SetAnchor(flipButton, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(flipButton, 0.5);
        stage.Children.Add(flipButton);
        var status = new TextBlock
        {
            Classes = { "muted" },
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(status, "QuestionFlipStatus");
        AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        Refresh();

        PaperChoreography? flipScene = null;
        flipButton.Click += async (_, _) =>
        {
            showsQuestion = !showsQuestion;
            Refresh();
            flipScene?.Skip();
            flipScene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, flipButton);
            if (!shouldReduceMotion)
            {
                flipButton.RenderTransform = TemplateRendering.Transform(0, 0, 3.5, 0.94);
            }

            flipScene = new PaperChoreography(
            [
                TemplateRendering.Move(
                    TimeSpan.FromMilliseconds(360),
                    flipButton,
                    0,
                    0,
                    showsQuestion ? -1 : 1,
                    1),
            ]);
            await flipScene.PlayAsync(shouldReduceMotion);
            if (showsQuestion)
            {
                var outcome = TemplateInteractionEvaluator.EvaluateAcknowledgement(acknowledged: true);
                TemplateRendering.ApplyOutcome(
                    outcomePanel,
                    outcomeText,
                    outcome.State,
                    OutcomeCopy);
                reportOutcome(outcome);
            }
            else
            {
                TemplateRendering.ApplyOutcome(
                    outcomePanel,
                    outcomeText,
                    TemplateOutcomeState.Ready,
                    OutcomeCopy);
            }
        };

        var root = ConstructionTemplatePresentation.CreateRoot(
            header,
            stage,
            status,
            outcomePanel,
            imageCache,
            [],
            "QuestionFlipImageCredits",
            parameters.UseTextOnlyFallback,
            $"Text-only card. Statement: {statement} Question: {question}");
        root.DetachedFromVisualTree += (_, _) =>
        {
            flipScene?.Skip();
            flipScene?.Dispose();
            flipScene = null;
        };
        ConstructionTemplatePresentation.AttachChoreography(
            root,
            replayButton,
            skipButton,
            shouldReduceMotion,
            [tape, flipButton, status],
            () =>
            {
                flipButton.RenderTransform = TemplateRendering.Transform(-18, 0, -2, 0.97);
                status.RenderTransform = TemplateRendering.Transform(0, 8, 0, 0.98);
            },
            () =>
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(720), flipButton, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(340), status, 0, 0, 0, 1),
            ],
            tape.SkipEntrance);
        return root;

        void Refresh()
        {
            sideLabel.Text = showsQuestion ? "QUESTION" : "STATEMENT";
            sentenceText.Text = showsQuestion ? question : statement;
            status.Text = showsQuestion
                ? "Question side visible. Select the card to return to the statement."
                : "Statement side visible. Select the card to reveal the question.";
            AutomationProperties.SetName(
                flipButton,
                showsQuestion
                    ? $"Question side. {question}. Flip to statement"
                    : $"Statement side. {statement}. Flip to question");
        }
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The authored question form is visible.",
        TemplateOutcomeState.Uncertain => "Flip the card to compare both sentence forms.",
        TemplateOutcomeState.Failure => "The question side has not been revealed yet.",
        _ => "Ready: flip the statement into its authored question form.",
    };
}

internal static class NegationStrikeRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var sentenceStart = TemplateRendering.Text(parameters, "sentence-start");
        var sentenceObject = TemplateRendering.Text(parameters, "object");
        var sentenceEnd = TemplateRendering.Text(parameters, "sentence-end");
        var negators = TemplateRendering.Options(parameters, "negators");
        var slots = TemplateRendering.Options(parameters, "slots");
        var answerNegator = TemplateRendering.Text(parameters, "answer-negator");
        var answerSlot = TemplateRendering.Text(parameters, "answer-slot");
        string? selectedNegatorId = parameters.PreviewOutcome switch
        {
            TemplateOutcomeState.Success or TemplateOutcomeState.Failure or
                TemplateOutcomeState.Uncertain => answerNegator,
            _ => null,
        };
        string? selectedSlotId = parameters.PreviewOutcome switch
        {
            TemplateOutcomeState.Success => answerSlot,
            TemplateOutcomeState.Failure => slots.First(slot => slot.Id != answerSlot).Id,
            _ => null,
        };
        var header = ConstructionTemplatePresentation.CreateHeader(
            "NegationStrike",
            instruction,
            "Replay placement",
            "Skip placement",
            out var replayButton,
            out var skipButton);
        var stage = TemplateRendering.CreateStage(310, "Negation token placement stage");
        TemplateRendering.AddBackdrop(stage, imageCache: null, assetReferenceId: null);
        var tape = new PaperTape { Content = "PLACE THE NEGATOR", Angle = -1.2 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);

        var negatorButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
        var negatorPanel = new WrapPanel
        {
            Margin = new Thickness(34, 78, 34, 170),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            ItemWidth = 150,
            ItemHeight = 62,
        };
        AutomationProperties.SetName(negatorPanel, "Available negation tokens");
        foreach (var (negator, index) in negators.Select((negator, index) => (negator, index)))
        {
            var button = new Button
            {
                Width = 138,
                Height = 52,
                Margin = new Thickness(5),
                Content = new PaperTape
                {
                    Content = negator.Label,
                    Angle = index % 2 == 0 ? -1.1 : 1.1,
                },
                Classes = { "quiet", "lift" },
            };
            AutomationProperties.SetAutomationId(button, $"NegationStrikeToken_{negator.Id}");
            AutomationProperties.SetName(button, $"Select negation token {negator.Label}");
            negatorButtons.Add(negator.Id, button);
            negatorPanel.Children.Add(button);
        }

        PaperStage.SetLayer(negatorPanel, PaperStageLayer.SupportingCast);
        stage.Children.Add(negatorPanel);
        var beforeSlot = new Button
        {
            Width = 110,
            Height = 62,
            Classes = { "quiet", "lift" },
        };
        var afterSlot = new Button
        {
            Width = 110,
            Height = 62,
            Classes = { "quiet", "lift" },
        };
        AutomationProperties.SetAutomationId(beforeSlot, $"NegationStrikeSlot_{slots[0].Id}");
        AutomationProperties.SetAutomationId(afterSlot, $"NegationStrikeSlot_{slots[1].Id}");
        var sentence = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        sentence.Children.Add(SentencePart(sentenceStart));
        sentence.Children.Add(beforeSlot);
        sentence.Children.Add(SentencePart(sentenceObject));
        sentence.Children.Add(afterSlot);
        sentence.Children.Add(SentencePart(sentenceEnd));
        var sentenceCard = new PaperCard
        {
            Margin = new Thickness(24, 176, 24, 24),
            Padding = new Thickness(14, 12),
            Content = sentence,
        };
        sentenceCard.Classes.Add("soft");
        AutomationProperties.SetName(
            sentenceCard,
            $"Sentence frame: {sentenceStart}, two negation slots, {sentenceObject}{sentenceEnd}");
        PaperStage.SetLayer(sentenceCard, PaperStageLayer.Subject);
        stage.Children.Add(sentenceCard);

        var checkButton = new Button
        {
            Content = "Check placement",
            Classes = { "primary", "lift" },
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        AutomationProperties.SetAutomationId(checkButton, "NegationStrikeCheck");
        var status = new TextBlock
        {
            Classes = { "muted" },
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(status, "NegationStrikeStatus");
        AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
        var controls = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        controls.Children.Add(status);
        Grid.SetColumn(checkButton, 1);
        controls.Children.Add(checkButton);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);

        foreach (var negator in negators)
        {
            negatorButtons[negator.Id].Click += (_, _) =>
            {
                selectedNegatorId = negator.Id;
                Refresh();
            };
        }

        beforeSlot.Click += (_, _) =>
        {
            selectedSlotId = slots[0].Id;
            Refresh();
        };
        afterSlot.Click += (_, _) =>
        {
            selectedSlotId = slots[1].Id;
            Refresh();
        };
        PaperChoreography? wobbleScene = null;
        checkButton.Click += async (_, _) =>
        {
            var outcome = TemplateInteractionEvaluator.EvaluateSelectionPair(
                negators,
                slots,
                answerNegator,
                answerSlot,
                selectedNegatorId,
                selectedSlotId);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            reportOutcome(outcome);
            if (outcome.State == TemplateOutcomeState.Failure && selectedSlotId is not null)
            {
                var selectedSlot = selectedSlotId == slots[0].Id ? beforeSlot : afterSlot;
                if (!shouldReduceMotion)
                {
                    wobbleScene?.Skip();
                    wobbleScene?.Dispose();
                    selectedSlot.RenderTransform = TemplateRendering.Transform(0, 0, -4, 1);
                    wobbleScene = new PaperChoreography(
                    [
                        TemplateRendering.Move(TimeSpan.FromMilliseconds(120), selectedSlot, 0, 0, 4, 1),
                        TemplateRendering.Move(TimeSpan.FromMilliseconds(120), selectedSlot, 0, 0, -2, 1),
                        TemplateRendering.Move(TimeSpan.FromMilliseconds(120), selectedSlot, 0, 0, 0, 1),
                    ]);
                    await wobbleScene.PlayAsync(reduceMotion: false);
                }

                selectedSlotId = null;
                Refresh();
                status.Text = "That placement returned to the token bank. Choose another slot.";
            }
            else
            {
                status.Text = outcome.State switch
                {
                    TemplateOutcomeState.Success => "The authored negator is in its authored slot.",
                    _ => "Choose both a negator and a sentence slot.",
                };
            }
        };
        Refresh();

        var root = ConstructionTemplatePresentation.CreateRoot(
            header,
            stage,
            controls,
            outcomePanel,
            imageCache,
            [],
            "NegationStrikeImageCredits",
            parameters.UseTextOnlyFallback,
            $"Text-only placement. Frame: {sentenceStart} [slot] {sentenceObject} [slot]{sentenceEnd} Tokens: {string.Join(", ", negators.Select(negator => negator.Label))}.");
        root.DetachedFromVisualTree += (_, _) =>
        {
            wobbleScene?.Skip();
            wobbleScene?.Dispose();
            wobbleScene = null;
        };
        ConstructionTemplatePresentation.AttachChoreography(
            root,
            replayButton,
            skipButton,
            shouldReduceMotion,
            [tape, negatorPanel, sentenceCard, controls],
            () =>
            {
                negatorPanel.RenderTransform = TemplateRendering.Transform(-18, 0, -1, 0.98);
                sentenceCard.RenderTransform = TemplateRendering.Transform(20, 0, 1.2, 0.97);
                controls.RenderTransform = TemplateRendering.Transform(0, 10, 0, 0.98);
            },
            () =>
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(520), negatorPanel, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(620), sentenceCard, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(360), controls, 0, 0, 0, 1),
            ],
            tape.SkipEntrance);
        return root;

        TextBlock SentencePart(string text) => new()
        {
            Text = text,
            FontSize = 20,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        };

        void Refresh()
        {
            foreach (var pair in negatorButtons)
            {
                pair.Value.Classes.Remove("primary");
                if (string.Equals(pair.Key, selectedNegatorId, StringComparison.Ordinal))
                {
                    pair.Value.Classes.Add("primary");
                }
            }

            var token = selectedNegatorId is null
                ? null
                : negators.Single(negator => negator.Id == selectedNegatorId).Label;
            beforeSlot.Content = selectedSlotId == slots[0].Id ? token : "[ slot ]";
            afterSlot.Content = selectedSlotId == slots[1].Id ? token : "[ slot ]";
            AutomationProperties.SetName(
                beforeSlot,
                $"{slots[0].Label}. {(selectedSlotId == slots[0].Id ? token : "empty")}");
            AutomationProperties.SetName(
                afterSlot,
                $"{slots[1].Label}. {(selectedSlotId == slots[1].Id ? token : "empty")}");
            status.Text = token is null
                ? "Choose a negation token, then choose one labeled slot."
                : selectedSlotId is null
                    ? $"Selected {token}. Choose a sentence slot."
                    : $"Placed {token} in {slots.Single(slot => slot.Id == selectedSlotId).Label.ToLowerInvariant()}.";
        }
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The authored negator is in the authored sentence slot.",
        TemplateOutcomeState.Uncertain => "Choose both a negator and a sentence slot.",
        TemplateOutcomeState.Failure => "That negator or position needs another placement.",
        _ => "Ready: place one negation token in the sentence.",
    };
}

internal static class PrepositionStageRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var objectLabel = TemplateRendering.Text(parameters, "object-label");
        var referenceLabel = TemplateRendering.Text(parameters, "reference-label");
        var positions = TemplateRendering.Options(parameters, "positions");
        var phrases = TemplateRendering.Options(parameters, "phrases")
            .ToDictionary(phrase => phrase.Id, phrase => phrase.Label, StringComparer.Ordinal);
        var answerId = TemplateRendering.Text(parameters, "answer");
        var assetReference = TemplateRendering.AssetReference(parameters, "asset");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        string? selectedPositionId = parameters.PreviewOutcome switch
        {
            TemplateOutcomeState.Success => answerId,
            TemplateOutcomeState.Failure => positions.First(position => position.Id != answerId).Id,
            _ => null,
        };
        var header = ConstructionTemplatePresentation.CreateHeader(
            "PrepositionStage",
            instruction,
            "Replay scene",
            "Skip scene",
            out var replayButton,
            out var skipButton);
        var stage = TemplateRendering.CreateStage(356, $"Preposition stage for {objectLabel}");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var tape = new PaperTape { Content = "MOVE THE CUTOUT", Angle = -1.2 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);

        var table = new PaperCard
        {
            Width = 330,
            Height = 72,
            Padding = new Thickness(14, 10),
            Content = new TextBlock
            {
                Text = referenceLabel,
                FontSize = 20,
                FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        table.Classes.Add("soft");
        AutomationProperties.SetName(table, $"Reference surface: {referenceLabel}");
        PaperStage.SetLayer(table, PaperStageLayer.SupportingCast);
        PaperStage.SetAnchor(table, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(table, 0.5);
        stage.Children.Add(table);

        var objectImage = parameters.UseTextOnlyFallback
            ? null
            : TemplateRendering.CreateContentImage(imageCache, assetReference, 78);
        var objectFrame = new CutoutFrame
        {
            Width = 118,
            Height = 98,
            Padding = new Thickness(8),
            Content = objectImage as Control ?? new TextBlock
            {
                Text = objectLabel,
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        var objectButton = new Button
        {
            Width = 132,
            Height = 110,
            Padding = new Thickness(5),
            Content = objectFrame,
            Classes = { "quiet", "lift" },
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
        };
        AutomationProperties.SetAutomationId(objectButton, "PrepositionStageObject");
        AutomationProperties.SetName(objectButton, $"Movable object: {objectLabel}. Drag to a position");
        PaperStage.SetLayer(objectButton, PaperStageLayer.Subject);
        PaperStage.SetAnchor(objectButton, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(objectButton, 0.16);
        stage.Children.Add(objectButton);

        var positionButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
        foreach (var (position, index) in positions.Select((position, index) => (position, index)))
        {
            var button = new Button
            {
                Width = 112,
                Height = 50,
                Padding = new Thickness(5),
                Content = new PaperTape
                {
                    Content = position.Label,
                    Angle = index % 2 == 0 ? -1 : 1,
                },
                Classes = { "quiet", "lift" },
            };
            AutomationProperties.SetAutomationId(button, $"PrepositionStagePosition_{position.Id}");
            AutomationProperties.SetName(button, $"Move {objectLabel} to {position.Label}");
            var (anchor, anchorX, offsetY) = HotspotPosition(index);
            PaperStage.SetLayer(button, PaperStageLayer.ReactionBurst);
            PaperStage.SetAnchor(button, anchor);
            PaperStage.SetAnchorX(button, anchorX);
            PaperStage.SetAnchorOffsetY(button, offsetY);
            DragDrop.SetAllowDrop(button, true);
            positionButtons.Add(position.Id, button);
            stage.Children.Add(button);
        }

        PointerPressedEventArgs? dragStartArgs = null;
        Point? dragStartPoint = null;
        objectButton.PointerPressed += (_, args) =>
        {
            if (args.GetCurrentPoint(objectButton).Properties.IsLeftButtonPressed)
            {
                dragStartArgs = args;
                dragStartPoint = args.GetPosition(objectButton);
            }
        };
        objectButton.PointerMoved += async (_, args) =>
        {
            if (dragStartArgs is null || dragStartPoint is null)
            {
                return;
            }

            var current = args.GetPosition(objectButton);
            if (Math.Abs(current.X - dragStartPoint.Value.X) < 7 &&
                Math.Abs(current.Y - dragStartPoint.Value.Y) < 7)
            {
                return;
            }

            var pointerPressedArgs = dragStartArgs;
            dragStartArgs = null;
            dragStartPoint = null;
            var transfer = new DataTransfer();
            transfer.Add(DataTransferItem.CreateText("preposition-object"));
            await DragDrop.DoDragDropAsync(pointerPressedArgs, transfer, DragDropEffects.Move);
        };
        objectButton.PointerReleased += (_, _) =>
        {
            dragStartArgs = null;
            dragStartPoint = null;
        };

        var phraseText = new TextBlock
        {
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(phraseText, "PrepositionStagePhrase");
        AutomationProperties.SetLiveSetting(phraseText, AutomationLiveSetting.Polite);
        var phrasePanel = new Border
        {
            Padding = new Thickness(14, 10),
            Child = phraseText,
        };
        phrasePanel.Classes.Add("soft-card");
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        Refresh();

        PaperChoreography? moveScene = null;
        foreach (var position in positions)
        {
            var button = positionButtons[position.Id];
            button.Click += async (_, _) => await MoveToAsync(position.Id);
            DragDrop.AddDragOverHandler(button, (_, args) =>
            {
                args.DragEffects = string.Equals(
                    args.DataTransfer.TryGetText(),
                    "preposition-object",
                    StringComparison.Ordinal)
                    ? DragDropEffects.Move
                    : DragDropEffects.None;
            });
            DragDrop.AddDropHandler(button, async (_, args) =>
            {
                if (string.Equals(
                    args.DataTransfer.TryGetText(),
                    "preposition-object",
                    StringComparison.Ordinal))
                {
                    await MoveToAsync(position.Id);
                    args.DragEffects = DragDropEffects.Move;
                }
            });
        }

        var root = ConstructionTemplatePresentation.CreateRoot(
            header,
            stage,
            phrasePanel,
            outcomePanel,
            imageCache,
            [objectImage is not null ? assetReference : null, backdropRendered ? backdropReference : null],
            "PrepositionStageImageCredits",
            parameters.UseTextOnlyFallback,
            $"Text-only positions for {objectLabel} and {referenceLabel}: {string.Join(", ", phrases.Values)}.");
        root.DetachedFromVisualTree += (_, _) =>
        {
            moveScene?.Skip();
            moveScene?.Dispose();
            moveScene = null;
        };
        ConstructionTemplatePresentation.AttachChoreography(
            root,
            replayButton,
            skipButton,
            shouldReduceMotion,
            [tape, table, phrasePanel, .. positionButtons.Values],
            () =>
            {
                table.RenderTransform = TemplateRendering.Transform(-16, 0, -1, 0.98);
                phrasePanel.RenderTransform = TemplateRendering.Transform(0, 8, 0, 0.98);
            },
            () =>
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(620), table, 0, 0, 0, 1),
                TemplateRendering.Reveal(
                    TimeSpan.FromMilliseconds(480),
                    positionButtons.Values.Cast<Control>().ToArray()),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(360), phrasePanel, 0, 0, 0, 1),
            ],
            tape.SkipEntrance);
        return root;

        async Task MoveToAsync(string positionId)
        {
            selectedPositionId = positionId;
            Refresh(updateObjectPosition: false);
            var index = positions
                .Select((position, index) => (position, index))
                .Single(pair => pair.position.Id == positionId)
                .index;
            var (x, y, angle) = ObjectPosition(index);
            moveScene?.Skip();
            moveScene?.Dispose();
            if (shouldReduceMotion)
            {
                objectButton.RenderTransform = TemplateRendering.Transform(x, y, angle, 1);
            }
            else
            {
                moveScene = new PaperChoreography(
                [
                    TemplateRendering.Move(
                        TimeSpan.FromMilliseconds(520),
                        objectButton,
                        x,
                        y,
                        angle,
                        1),
                ]);
                await moveScene.PlayAsync(reduceMotion: false);
            }

            var outcome = TemplateInteractionEvaluator.EvaluateSingleSelection(
                positions,
                answerId,
                positionId);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            reportOutcome(outcome);
        }

        void Refresh(bool updateObjectPosition = true)
        {
            foreach (var pair in positionButtons)
            {
                pair.Value.Classes.Remove("primary");
                if (string.Equals(pair.Key, selectedPositionId, StringComparison.Ordinal))
                {
                    pair.Value.Classes.Add("primary");
                }
            }

            if (selectedPositionId is null)
            {
                if (updateObjectPosition)
                {
                    objectButton.RenderTransform = TemplateRendering.Transform(0, 0, -1.4, 1);
                }

                phraseText.Text = "Choose a labeled position to form the authored phrase.";
                AutomationProperties.SetName(phrasePanel, "No preposition phrase selected yet");
                return;
            }

            var index = positions
                .Select((position, index) => (position, index))
                .Single(pair => pair.position.Id == selectedPositionId)
                .index;
            var (x, y, angle) = ObjectPosition(index);
            if (updateObjectPosition)
            {
                objectButton.RenderTransform = TemplateRendering.Transform(x, y, angle, 1);
            }

            phraseText.Text = phrases[selectedPositionId];
            AutomationProperties.SetName(phrasePanel, $"Resulting phrase: {phrases[selectedPositionId]}");
        }

        static (PaperAnchorLine Anchor, double X, double OffsetY) HotspotPosition(int index) =>
            (index % 4) switch
            {
                0 => (PaperAnchorLine.Shoulder, 0.38, -8),
                1 => (PaperAnchorLine.Foot, 0.5, -20),
                2 => (PaperAnchorLine.Waist, 0.84, 0),
                _ => (PaperAnchorLine.Waist, 0.18, 0),
            };

        static (double X, double Y, double Angle) ObjectPosition(int index) => (index % 4) switch
        {
            0 => (150, -36, -1),
            1 => (245, 96, 1),
            2 => (480, 4, 1.4),
            _ => (18, 4, -1.4),
        };
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The cutout is at the authored preposition position.",
        TemplateOutcomeState.Uncertain => "Move the cutout to one labeled position.",
        TemplateOutcomeState.Failure => "Read the phrase, then try another position.",
        _ => "Ready: move the cutout and read the resulting phrase.",
    };
}

internal static class SentenceExpandRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var baseText = TemplateRendering.Text(parameters, "base");
        var complements = TemplateRendering.Options(parameters, "complements");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var selectedIds = parameters.PreviewOutcome switch
        {
            TemplateOutcomeState.Success => complements.Select(complement => complement.Id).ToList(),
            TemplateOutcomeState.Failure => complements.Reverse().Select(complement => complement.Id).ToList(),
            TemplateOutcomeState.Uncertain => complements.Take(Math.Max(1, complements.Count / 2))
                .Select(complement => complement.Id)
                .ToList(),
            _ => [],
        };
        var header = ConstructionTemplatePresentation.CreateHeader(
            "SentenceExpand",
            instruction,
            "Replay sentence",
            "Skip sentence",
            out var replayButton,
            out var skipButton);
        var stage = TemplateRendering.CreateStage(390, "Sentence expansion paper stage");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var tape = new PaperTape { Content = "GROW THE SENTENCE", Angle = 1.2 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);

        var baseCard = new PaperCard
        {
            Width = 280,
            Height = 72,
            Margin = new Thickness(28, 100, 28, 218),
            Padding = new Thickness(14, 10),
            Content = new TextBlock
            {
                Text = baseText,
                FontSize = 22,
                FontWeight = FontWeight.Bold,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        baseCard.Classes.Add("soft");
        AutomationProperties.SetName(baseCard, $"Base sentence: {baseText}");
        PaperStage.SetLayer(baseCard, PaperStageLayer.SupportingCast);
        stage.Children.Add(baseCard);

        var selectedPanel = new WrapPanel
        {
            Margin = new Thickness(30, 158, 30, 130),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            ItemWidth = 170,
            ItemHeight = 82,
        };
        AutomationProperties.SetName(selectedPanel, "Selected sentence complements in order");
        PaperStage.SetLayer(selectedPanel, PaperStageLayer.Subject);
        stage.Children.Add(selectedPanel);
        var bankPanel = new WrapPanel
        {
            Margin = new Thickness(30, 270, 30, 20),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            ItemWidth = 170,
            ItemHeight = 88,
        };
        AutomationProperties.SetName(bankPanel, "Available complement cutouts");
        PaperStage.SetLayer(bankPanel, PaperStageLayer.ReactionBurst);
        stage.Children.Add(bankPanel);
        var bankButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
        foreach (var complement in complements)
        {
            var button = new Button
            {
                Width = 158,
                Height = 78,
                Margin = new Thickness(5),
                Padding = new Thickness(6),
                Content = ComplementContent(complement, compact: false),
                Classes = { "quiet", "lift" },
            };
            AutomationProperties.SetAutomationId(button, $"SentenceExpandBank_{complement.Id}");
            AutomationProperties.SetName(button, $"Add complement {complement.Label}");
            bankButtons.Add(complement.Id, button);
            bankPanel.Children.Add(button);
        }

        var resetButton = new Button { Content = "Reset", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(resetButton, "SentenceExpandReset");
        var checkButton = new Button { Content = "Check sentence", Classes = { "primary", "lift" } };
        AutomationProperties.SetAutomationId(checkButton, "SentenceExpandCheck");
        var actionButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        actionButtons.Children.Add(resetButton);
        actionButtons.Children.Add(checkButton);
        var phraseText = new TextBlock
        {
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(phraseText, "SentenceExpandPhrase");
        AutomationProperties.SetLiveSetting(phraseText, AutomationLiveSetting.Polite);
        var controls = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        controls.Children.Add(phraseText);
        Grid.SetColumn(actionButtons, 1);
        controls.Children.Add(actionButtons);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        foreach (var complement in complements)
        {
            bankButtons[complement.Id].Click += (_, _) =>
            {
                if (!selectedIds.Contains(complement.Id, StringComparer.Ordinal))
                {
                    selectedIds.Add(complement.Id);
                    Refresh();
                }
            };
        }

        resetButton.Click += (_, _) =>
        {
            selectedIds.Clear();
            Refresh();
            TemplateRendering.ApplyOutcome(
                outcomePanel,
                outcomeText,
                TemplateOutcomeState.Ready,
                OutcomeCopy);
        };
        checkButton.Click += (_, _) =>
        {
            var outcome = TemplateInteractionEvaluator.EvaluateWordOrder(complements, selectedIds);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            reportOutcome(outcome);
        };
        Refresh();

        var root = ConstructionTemplatePresentation.CreateRoot(
            header,
            stage,
            controls,
            outcomePanel,
            imageCache,
            complements.Select(complement =>
                    parameters.UseTextOnlyFallback ? null : complement.AssetReferenceId)
                .Append(backdropRendered ? backdropReference : null),
            "SentenceExpandImageCredits",
            parameters.UseTextOnlyFallback,
            $"Text-only sentence. Base: {baseText}. Complements: {string.Join(", ", complements.Select(complement => complement.Label.TrimEnd('.', '!', '?')))}.");
        ConstructionTemplatePresentation.AttachChoreography(
            root,
            replayButton,
            skipButton,
            shouldReduceMotion,
            [tape, baseCard, selectedPanel, bankPanel, controls],
            () =>
            {
                baseCard.RenderTransform = TemplateRendering.Transform(-18, 0, -1, 0.98);
                selectedPanel.RenderTransform = TemplateRendering.Transform(18, 0, 1, 0.98);
                bankPanel.RenderTransform = TemplateRendering.Transform(0, 12, -0.6, 0.98);
                controls.RenderTransform = TemplateRendering.Transform(0, 8, 0, 0.98);
            },
            () =>
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(520), baseCard, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(520), selectedPanel, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(460), bankPanel, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(340), controls, 0, 0, 0, 1),
            ],
            tape.SkipEntrance);
        return root;

        Control ComplementContent(TemplateOption complement, bool compact)
        {
            var copy = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 7,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            if (!parameters.UseTextOnlyFallback &&
                TemplateRendering.CreateContentImage(
                    imageCache,
                    complement.AssetReferenceId,
                    compact ? 34 : 44) is { } image)
            {
                copy.Children.Add(image);
            }

            copy.Children.Add(new TextBlock
            {
                Text = complement.Label,
                FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
            });
            return copy;
        }

        void Refresh()
        {
            foreach (var pair in bankButtons)
            {
                pair.Value.IsEnabled = !selectedIds.Contains(pair.Key, StringComparer.Ordinal);
            }

            selectedPanel.Children.Clear();
            foreach (var id in selectedIds)
            {
                var complement = complements.Single(candidate => candidate.Id == id);
                var button = new Button
                {
                    Width = 158,
                    Height = 70,
                    Margin = new Thickness(5),
                    Padding = new Thickness(6),
                    Content = ComplementContent(complement, compact: true),
                    Classes = { "primary", "lift" },
                };
                AutomationProperties.SetAutomationId(button, $"SentenceExpandSelected_{complement.Id}");
                AutomationProperties.SetName(button, $"Remove complement {complement.Label}");
                button.Click += (_, _) =>
                {
                    selectedIds.Remove(complement.Id);
                    Refresh();
                };
                selectedPanel.Children.Add(button);
            }

            if (selectedIds.Count == 0)
            {
                var emptyCard = new PaperCard
                {
                    Width = 158,
                    Height = 70,
                    Padding = new Thickness(10, 8),
                    Content = new TextBlock
                    {
                        Text = "Choose the first complement from the cutout bank.",
                        Classes = { "muted" },
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                };
                emptyCard.Classes.Add("soft");
                AutomationProperties.SetName(
                    emptyCard,
                    "No complements selected. Choose the first complement from the cutout bank.");
                selectedPanel.Children.Add(emptyCard);
            }

            var selectedCopy = selectedIds
                .Select(id => complements.Single(complement => complement.Id == id).Label);
            phraseText.Text = string.Join(" ", new[] { baseText }.Concat(selectedCopy));
            AutomationProperties.SetName(controls, $"Current sentence: {phraseText.Text}");
        }
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The sentence contains every authored complement in order.",
        TemplateOutcomeState.Uncertain => "Add every authored complement before checking the sentence.",
        TemplateOutcomeState.Failure => "Every complement is present, but the order needs another pass.",
        _ => "Ready: grow the base sentence with complement cutouts.",
    };
}
