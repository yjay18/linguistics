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

internal static class SignReadingRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var signText = TemplateRendering.Text(parameters, "sign-text");
        var context = TemplateRendering.Text(parameters, "context");
        var question = TemplateRendering.Text(parameters, "question");
        var options = TemplateRendering.Options(parameters, "options");
        var answerId = TemplateRendering.Text(parameters, "answer");
        var assetReference = TemplateRendering.AssetReference(parameters, "sign-asset");
        if (options.Count < 2)
        {
            throw new InvalidOperationException("Sign reading requires at least two answer options.");
        }

        if (!options.Any(option => string.Equals(option.Id, answerId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Sign reading answer must name an available option.");
        }

        var replayButton = new Button { Content = "Replay sign", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "SignReadingReplay");
        AutomationProperties.SetName(replayButton, "Replay the sign reading stage");
        var skipButton = new Button { Content = "Skip sign", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "SignReadingSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed sign reading stage");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Reading instruction. {instruction}");
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actions.Children.Add(replayButton);
        actions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(actions, 1);
        header.Children.Add(actions);

        var signImage = parameters.UseTextOnlyFallback
            ? null
            : TemplateRendering.CreateContentImage(
                imageCache,
                assetReference,
                154,
                Stretch.UniformToFill);
        var stage = TemplateRendering.CreateStage(304, $"Reading sign. {signText}");
        TemplateRendering.AddBackdrop(stage, imageCache, assetReferenceId: null);
        var stageTape = new PaperTape { Content = "READ THE SIGN", Angle = -1.2 };
        PaperStage.SetLayer(stageTape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(stageTape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(stageTape, 0.24);
        PaperStage.SetAnchorOffsetY(stageTape, -10);
        stage.Children.Add(stageTape);

        var signContent = signImage is null
            ? CreateTextOnlySign(signText)
            : CreatePhotographedSign(signImage, signText);
        var signFrame = new CutoutFrame
        {
            Width = 520,
            Height = 208,
            Content = signContent,
            RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        };
        signFrame.Classes.Add("tilt-left");
        AutomationProperties.SetName(
            signFrame,
            signImage is null
                ? $"Authored text-only sign. {signText}"
                : $"Photographed sign with complete written text. {signText}");
        PaperStage.SetLayer(signFrame, PaperStageLayer.Subject);
        PaperStage.SetAnchor(signFrame, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(signFrame, 0.5);
        PaperStage.SetAnchorOffsetY(signFrame, -30);
        stage.Children.Add(signFrame);

        var questionText = new TextBlock
        {
            Text = question,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetName(questionText, $"Sign comprehension question. {question}");
        var optionPanel = new WrapPanel { Orientation = Orientation.Horizontal };
        AutomationProperties.SetName(optionPanel, "Sign comprehension choices");
        var buttons = new Dictionary<string, Button>(StringComparer.Ordinal);
        string? selectedId = null;
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        foreach (var option in options)
        {
            var button = new Button
            {
                Content = option.Label,
                Margin = new Thickness(0, 6, 8, 0),
                Classes = { "quiet" },
            };
            AutomationProperties.SetAutomationId(button, $"SignReadingOption_{option.Id}");
            AutomationProperties.SetName(button, $"Answer {option.Label}");
            button.Click += (_, _) =>
            {
                selectedId = option.Id;
                RefreshSelection();
                var outcome = TemplateInteractionEvaluator.EvaluateSingleSelection(
                    options,
                    answerId,
                    selectedId);
                TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
                reportOutcome(outcome);
            };
            buttons.Add(option.Id, button);
            optionPanel.Children.Add(button);
        }

        var questionContent = new StackPanel { Spacing = 6 };
        questionContent.Children.Add(questionText);
        questionContent.Children.Add(optionPanel);
        var questionCard = new PaperCard
        {
            Padding = new Thickness(14, 12),
            Content = questionContent,
        };
        AutomationProperties.SetName(questionCard, "Written sign comprehension check");

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        root.Children.Add(new TextBlock
        {
            Text = context,
            Classes = { "muted" },
            TextWrapping = TextWrapping.Wrap,
        });
        if (parameters.UseTextOnlyFallback || signImage is null)
        {
            var fallback = new PaperCard
            {
                Padding = new Thickness(12, 8),
                Content = new TextBlock
                {
                    Text = parameters.UseTextOnlyFallback
                        ? $"Text-only sign: {signText}."
                        : "No validated sign photograph is bundled. The complete sign text is shown.",
                    TextWrapping = TextWrapping.Wrap,
                },
            };
            fallback.Classes.Add("soft");
            AutomationProperties.SetAutomationId(fallback, "SignReadingAssetStatus");
            AutomationProperties.SetName(
                fallback,
                signImage is null
                    ? "Sign photograph unavailable. Complete authored text is shown."
                    : "Text-only sign equivalent");
            root.Children.Add(fallback);
        }

        root.Children.Add(stage);
        if (signImage is not null &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                [assetReference],
                "SignReadingImageCredits") is { } credits)
        {
            root.Children.Add(credits);
        }

        root.Children.Add(questionCard);
        root.Children.Add(outcomePanel);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, stageTape, signFrame, questionCard);
            if (!shouldReduceMotion)
            {
                signFrame.RenderTransform = TemplateRendering.Transform(-28, 5, -3, 0.96);
                questionCard.RenderTransform = TemplateRendering.Transform(0, 8, 0, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), stageTape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(520), signFrame, 0, 0, -1, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(340), questionCard, 0, 0, 0, 1),
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
        skipButton.Click += (_, _) =>
        {
            scene?.Skip();
            stageTape.SkipEntrance();
        };
        return root;

        void RefreshSelection()
        {
            foreach (var pair in buttons)
            {
                pair.Value.Classes.Remove("primary");
                pair.Value.Classes.Remove("quiet");
                pair.Value.Classes.Add(string.Equals(pair.Key, selectedId, StringComparison.Ordinal)
                    ? "primary"
                    : "quiet");
            }
        }
    }

    private static Control CreateTextOnlySign(string signText)
    {
        var content = new StackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        content.Children.Add(new TextBlock
        {
            Text = "AUTHORED SIGN TEXT",
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            TextAlignment = TextAlignment.Center,
            Classes = { "muted" },
        });
        content.Children.Add(new TextBlock
        {
            Text = signText,
            FontSize = 30,
            FontWeight = FontWeight.Bold,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        });
        return content;
    }

    private static Control CreatePhotographedSign(Image image, string signText)
    {
        var text = new PaperCard
        {
            Padding = new Thickness(10, 6),
            Content = new TextBlock
            {
                Text = signText,
                FontWeight = FontWeight.SemiBold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            },
        };
        text.Classes.Add("soft");
        var content = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        content.Children.Add(image);
        Grid.SetRow(text, 1);
        content.Children.Add(text);
        return content;
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The answer matches the authored sign text.",
        TemplateOutcomeState.Uncertain => "Choose one meaning after reading the complete sign.",
        TemplateOutcomeState.Failure => "That answer does not match the authored sign text.",
        _ => "Ready: read the complete sign, then choose one meaning.",
    };
}

internal static class FormFillRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        _ = imageCache;
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var formTitle = TemplateRendering.Text(parameters, "form-title");
        var prompt = TemplateRendering.Text(parameters, "prompt");
        var fields = TemplateRendering.Options(parameters, "fields");
        var answers = TemplateRendering.Options(parameters, "answers");
        var fieldIds = fields.Select(field => field.Id).ToArray();
        var answerIds = answers.Select(answer => answer.Id).ToArray();
        if (fields.Count == 0 ||
            !fieldIds.OrderBy(id => id, StringComparer.Ordinal)
                .SequenceEqual(answerIds.OrderBy(id => id, StringComparer.Ordinal), StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Form fields and answers must declare the same IDs.");
        }

        var replayButton = new Button { Content = "Replay form", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "FormFillReplay");
        AutomationProperties.SetName(replayButton, "Replay the form entrance");
        var skipButton = new Button { Content = "Skip form", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "FormFillSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed form");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Writing instruction. {instruction}");
        var headerActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        headerActions.Children.Add(replayButton);
        headerActions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(headerActions, 1);
        header.Children.Add(headerActions);

        var promptCard = new PaperCard
        {
            Padding = new Thickness(14, 10),
            Content = new StackPanel
            {
                Spacing = 5,
                Children =
                {
                    new TextBlock
                    {
                        Text = "SYNTHETIC PREVIEW DETAILS",
                        FontSize = 12,
                        FontWeight = FontWeight.Bold,
                        Classes = { "muted" },
                    },
                    new TextBlock
                    {
                        Text = prompt,
                        FontSize = 17,
                        FontWeight = FontWeight.SemiBold,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = "Use only these synthetic details. Nothing is saved.",
                        Classes = { "muted" },
                        TextWrapping = TextWrapping.Wrap,
                    },
                },
            },
        };
        promptCard.Classes.Add("soft");
        AutomationProperties.SetName(promptCard, $"Synthetic details to copy. {prompt}");

        var formTape = new PaperTape { Content = formTitle.ToUpperInvariant(), Angle = -1.1 };
        AutomationProperties.SetName(formTape, $"Form title. {formTitle}");
        var fieldPanel = new StackPanel { Spacing = 10 };
        var inputs = new Dictionary<string, TextBox>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            var label = new TextBlock
            {
                Text = field.Label,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var input = new TextBox
            {
                PlaceholderText = $"Enter {field.Label.ToLowerInvariant()}",
                MinWidth = 360,
            };
            AutomationProperties.SetAutomationId(input, $"FormFillField_{field.Id}");
            AutomationProperties.SetName(input, $"{field.Label} form field");
            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("132,*"),
                ColumnSpacing = 12,
            };
            row.Children.Add(label);
            Grid.SetColumn(input, 1);
            row.Children.Add(input);
            fieldPanel.Children.Add(row);
            inputs.Add(field.Id, input);
        }

        var formContent = new StackPanel { Spacing = 12 };
        formContent.Children.Add(formTape);
        formContent.Children.Add(fieldPanel);
        var formCard = new PaperCard
        {
            Padding = new Thickness(20, 18),
            Content = formContent,
            RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        };
        formCard.Classes.Add("settings-sheet");
        AutomationProperties.SetName(formCard, $"Paper form. {formTitle}. {fields.Count} fields.");

        var checkButton = new Button { Content = "Check form", Classes = { "primary", "lift" } };
        AutomationProperties.SetAutomationId(checkButton, "FormFillCheck");
        AutomationProperties.SetName(checkButton, "Check every form field");
        var clearButton = new Button { Content = "Clear form", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(clearButton, "FormFillClear");
        AutomationProperties.SetName(clearButton, "Clear every form field");
        var controls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { checkButton, clearButton },
        };
        var controlsCard = new PaperCard
        {
            Padding = new Thickness(12, 9),
            Content = controls,
        };
        controlsCard.Classes.Add("soft");
        AutomationProperties.SetName(controlsCard, "Form actions");

        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        checkButton.Click += (_, _) =>
        {
            var responses = inputs.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Text ?? string.Empty,
                StringComparer.Ordinal);
            var outcome = TemplateInteractionEvaluator.EvaluateTextFields(answers, responses);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            reportOutcome(outcome);
        };
        clearButton.Click += (_, _) =>
        {
            foreach (var input in inputs.Values)
            {
                input.Text = string.Empty;
            }

            TemplateRendering.ApplyOutcome(
                outcomePanel,
                outcomeText,
                TemplateOutcomeState.Ready,
                OutcomeCopy);
        };

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = "Text-only form mode is active. Every label and field remains available.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(promptCard);
        root.Children.Add(formCard);
        root.Children.Add(controlsCard);
        root.Children.Add(outcomePanel);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, promptCard, formCard, controlsCard);
            if (!shouldReduceMotion)
            {
                formCard.RenderTransform = TemplateRendering.Transform(0, 12, -1.2, 0.98);
                controlsCard.RenderTransform = TemplateRendering.Transform(0, 8, 0, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(240), promptCard),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(460), formCard, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(300), controlsCard, 0, 0, 0, 1),
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
        skipButton.Click += (_, _) =>
        {
            scene?.Skip();
            formTape.SkipEntrance();
        };
        return root;
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Every field matches the synthetic details.",
        TemplateOutcomeState.Uncertain => "Complete every field before checking the form.",
        TemplateOutcomeState.Failure => "One or more fields do not match the synthetic details.",
        _ => "Ready: copy each synthetic detail into its matching field.",
    };
}

internal static class NoteWriteRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        _ = imageCache;
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var stationeryTitle = TemplateRendering.Text(parameters, "stationery-title");
        var prompt = TemplateRendering.Text(parameters, "prompt");
        var requiredContent = TemplateRendering.Options(parameters, "required-content");
        if (requiredContent.Count == 0)
        {
            throw new InvalidOperationException("Note writing requires at least one content check.");
        }

        var replayButton = new Button { Content = "Replay note", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "NoteWriteReplay");
        AutomationProperties.SetName(replayButton, "Replay the stationery entrance");
        var skipButton = new Button { Content = "Skip note", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "NoteWriteSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed stationery");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Writing instruction. {instruction}");
        var headerActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        headerActions.Children.Add(replayButton);
        headerActions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(headerActions, 1);
        header.Children.Add(headerActions);

        var promptCard = new PaperCard
        {
            Padding = new Thickness(14, 10),
            Content = new StackPanel
            {
                Spacing = 5,
                Children =
                {
                    new TextBlock
                    {
                        Text = "NOTE BRIEF",
                        FontSize = 12,
                        FontWeight = FontWeight.Bold,
                        Classes = { "muted" },
                    },
                    new TextBlock
                    {
                        Text = prompt,
                        FontSize = 17,
                        FontWeight = FontWeight.SemiBold,
                        TextWrapping = TextWrapping.Wrap,
                    },
                },
            },
        };
        promptCard.Classes.Add("soft");
        AutomationProperties.SetName(promptCard, $"Note brief. {prompt}");

        var stationeryTape = new PaperTape { Content = stationeryTitle.ToUpperInvariant(), Angle = -1.1 };
        AutomationProperties.SetName(stationeryTape, $"Stationery title. {stationeryTitle}");
        var checks = new TextBlock
        {
            Text = $"Include: {string.Join(" · ", requiredContent.Select(item => item.Label))}",
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetName(
            checks,
            $"Required content. {string.Join(". ", requiredContent.Select(item => item.Label))}");
        var noteInput = new TextBox
        {
            AcceptsReturn = true,
            MinHeight = 132,
            TextWrapping = TextWrapping.Wrap,
            PlaceholderText = "Write your note in German",
        };
        AutomationProperties.SetAutomationId(noteInput, "NoteWriteInput");
        AutomationProperties.SetName(noteInput, "German note text");
        var stationeryContent = new StackPanel { Spacing = 12 };
        stationeryContent.Children.Add(stationeryTape);
        stationeryContent.Children.Add(checks);
        stationeryContent.Children.Add(noteInput);
        var stationeryCard = new PaperCard
        {
            Padding = new Thickness(20, 18),
            Content = stationeryContent,
            RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        };
        stationeryCard.Classes.Add("settings-sheet");
        AutomationProperties.SetName(
            stationeryCard,
            $"Paper stationery. {stationeryTitle}. Write one short German note.");

        var checkButton = new Button { Content = "Check note", Classes = { "primary", "lift" } };
        AutomationProperties.SetAutomationId(checkButton, "NoteWriteCheck");
        AutomationProperties.SetName(checkButton, "Check the note for required content");
        var clearButton = new Button { Content = "Clear note", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(clearButton, "NoteWriteClear");
        AutomationProperties.SetName(clearButton, "Clear the note text");
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { checkButton, clearButton },
        };
        var actionsCard = new PaperCard { Padding = new Thickness(12, 9), Content = actions };
        actionsCard.Classes.Add("soft");
        AutomationProperties.SetName(actionsCard, "Note actions");

        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        checkButton.Click += (_, _) =>
        {
            var outcome = TemplateInteractionEvaluator.EvaluateRequiredContent(
                requiredContent,
                noteInput.Text ?? string.Empty);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            reportOutcome(outcome);
        };
        clearButton.Click += (_, _) =>
        {
            noteInput.Text = string.Empty;
            TemplateRendering.ApplyOutcome(
                outcomePanel,
                outcomeText,
                TemplateOutcomeState.Ready,
                OutcomeCopy);
        };

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = "Text-only stationery mode is active. The brief, checks, and writing field remain available.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(promptCard);
        root.Children.Add(stationeryCard);
        root.Children.Add(actionsCard);
        root.Children.Add(outcomePanel);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, promptCard, stationeryCard, actionsCard);
            if (!shouldReduceMotion)
            {
                stationeryCard.RenderTransform = TemplateRendering.Transform(0, 12, -1.2, 0.98);
                actionsCard.RenderTransform = TemplateRendering.Transform(0, 8, 0, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), promptCard),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(460), stationeryCard, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(280), actionsCard, 0, 0, 0, 1),
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
        skipButton.Click += (_, _) =>
        {
            scene?.Skip();
            stationeryTape.SkipEntrance();
        };
        return root;
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The note includes all required details.",
        TemplateOutcomeState.Uncertain => "Write a short note before checking it.",
        TemplateOutcomeState.Failure => "Add every required detail, then check the note again.",
        _ => "Ready: write a short note using every listed detail.",
    };
}

internal static class MenuReadRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        _ = imageCache;
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var menuTitle = TemplateRendering.Text(parameters, "menu-title");
        var menuItems = TemplateRendering.Options(parameters, "menu-items");
        var question = TemplateRendering.Text(parameters, "question");
        var options = TemplateRendering.Options(parameters, "options");
        var answerId = TemplateRendering.Text(parameters, "answer");
        if (menuItems.Count < 2 || options.Count < 2)
        {
            throw new InvalidOperationException("Menu reading requires menu items and at least two answer options.");
        }

        if (!options.Any(option => string.Equals(option.Id, answerId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Menu reading answer must name an available option.");
        }

        var replayButton = new Button { Content = "Replay menu", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "MenuReadReplay");
        AutomationProperties.SetName(replayButton, "Replay the menu entrance");
        var skipButton = new Button { Content = "Skip menu", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "MenuReadSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed menu");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Reading instruction. {instruction}");
        var headerActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        headerActions.Children.Add(replayButton);
        headerActions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(headerActions, 1);
        header.Children.Add(headerActions);

        var menuTape = new PaperTape { Content = menuTitle.ToUpperInvariant(), Angle = -1.1 };
        AutomationProperties.SetName(menuTape, $"Menu title. {menuTitle}");
        var itemPanel = new StackPanel { Spacing = 8 };
        foreach (var item in menuItems)
        {
            var itemText = new TextBlock
            {
                Text = item.Label,
                FontSize = 17,
                FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap,
            };
            AutomationProperties.SetName(itemText, $"Menu item. {item.Label}");
            itemPanel.Children.Add(itemText);
        }

        var menuContent = new StackPanel { Spacing = 12 };
        menuContent.Children.Add(menuTape);
        menuContent.Children.Add(new TextBlock
        {
            Text = "SYNTHETIC PREVIEW MENU",
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Classes = { "muted" },
        });
        menuContent.Children.Add(itemPanel);
        var menuCard = new PaperCard
        {
            Padding = new Thickness(22, 18),
            Content = menuContent,
            RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        };
        menuCard.Classes.Add("settings-sheet");
        AutomationProperties.SetName(
            menuCard,
            $"Synthetic menu. {menuTitle}. {string.Join(". ", menuItems.Select(item => item.Label))}");

        var questionText = new TextBlock
        {
            Text = question,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetName(questionText, $"Menu question. {question}");
        var optionPanel = new WrapPanel { Orientation = Orientation.Horizontal };
        AutomationProperties.SetName(optionPanel, "Menu answer choices");
        var buttons = new Dictionary<string, Button>(StringComparer.Ordinal);
        string? selectedId = null;
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        foreach (var option in options)
        {
            var button = new Button
            {
                Content = option.Label,
                Margin = new Thickness(0, 6, 8, 0),
                Classes = { "quiet" },
            };
            AutomationProperties.SetAutomationId(button, $"MenuReadOption_{option.Id}");
            AutomationProperties.SetName(button, $"Answer {option.Label}");
            button.Click += (_, _) =>
            {
                selectedId = option.Id;
                RefreshSelection();
                var outcome = TemplateInteractionEvaluator.EvaluateSingleSelection(
                    options,
                    answerId,
                    selectedId);
                TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
                reportOutcome(outcome);
            };
            buttons.Add(option.Id, button);
            optionPanel.Children.Add(button);
        }

        var questionContent = new StackPanel { Spacing = 6 };
        questionContent.Children.Add(questionText);
        questionContent.Children.Add(optionPanel);
        var questionCard = new PaperCard
        {
            Padding = new Thickness(14, 12),
            Content = questionContent,
        };
        questionCard.Classes.Add("soft");
        AutomationProperties.SetName(questionCard, "Synthetic menu extraction question");

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = "Text-only menu mode is active. Every item, price, question, and choice remains available.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(menuCard);
        root.Children.Add(questionCard);
        root.Children.Add(outcomePanel);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, menuCard, questionCard);
            if (!shouldReduceMotion)
            {
                menuCard.RenderTransform = TemplateRendering.Transform(-10, 10, -1.2, 0.98);
                questionCard.RenderTransform = TemplateRendering.Transform(10, 6, 0.8, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Move(TimeSpan.FromMilliseconds(520), menuCard, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(320), questionCard, 0, 0, 0, 1),
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
        skipButton.Click += (_, _) =>
        {
            scene?.Skip();
            menuTape.SkipEntrance();
        };
        return root;

        void RefreshSelection()
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

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "That price matches the requested menu item.",
        TemplateOutcomeState.Uncertain => "Choose one price from the synthetic menu.",
        TemplateOutcomeState.Failure => "Check the requested item and its printed price again.",
        _ => "Ready: find the requested item, then choose its price.",
    };
}

internal static class ScheduleReadRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        _ = imageCache;
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var scheduleTitle = TemplateRendering.Text(parameters, "schedule-title");
        var entries = TemplateRendering.Options(parameters, "entries");
        var question = TemplateRendering.Text(parameters, "question");
        var options = TemplateRendering.Options(parameters, "options");
        var answerId = TemplateRendering.Text(parameters, "answer");
        if (entries.Count < 2 || options.Count < 2)
        {
            throw new InvalidOperationException("Schedule reading requires entries and at least two answer options.");
        }

        if (!options.Any(option => string.Equals(option.Id, answerId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Schedule reading answer must name an available option.");
        }

        var replayButton = new Button { Content = "Replay hours", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "ScheduleReadReplay");
        AutomationProperties.SetName(replayButton, "Replay the opening-hours entrance");
        var skipButton = new Button { Content = "Skip hours", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "ScheduleReadSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed opening hours");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Reading instruction. {instruction}");
        var headerActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        headerActions.Children.Add(replayButton);
        headerActions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(headerActions, 1);
        header.Children.Add(headerActions);

        var scheduleTape = new PaperTape { Content = scheduleTitle.ToUpperInvariant(), Angle = -1.1 };
        AutomationProperties.SetName(scheduleTape, $"Schedule title. {scheduleTitle}");
        var entryPanel = new StackPanel { Spacing = 8 };
        foreach (var entry in entries)
        {
            var entryText = new TextBlock
            {
                Text = entry.Label,
                FontSize = 17,
                FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap,
            };
            AutomationProperties.SetName(entryText, $"Opening-hours entry. {entry.Label}");
            entryPanel.Children.Add(entryText);
        }

        var scheduleContent = new StackPanel { Spacing = 12 };
        scheduleContent.Children.Add(scheduleTape);
        scheduleContent.Children.Add(new TextBlock
        {
            Text = "SYNTHETIC OPENING HOURS",
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Classes = { "muted" },
        });
        scheduleContent.Children.Add(entryPanel);
        var scheduleCard = new PaperCard
        {
            Padding = new Thickness(22, 18),
            Content = scheduleContent,
            RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        };
        scheduleCard.Classes.Add("settings-sheet");
        AutomationProperties.SetName(
            scheduleCard,
            $"Synthetic opening hours. {scheduleTitle}. {string.Join(". ", entries.Select(entry => entry.Label))}");

        var questionText = new TextBlock
        {
            Text = question,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetName(questionText, $"Opening-hours question. {question}");
        var optionPanel = new WrapPanel { Orientation = Orientation.Horizontal };
        AutomationProperties.SetName(optionPanel, "Opening-hours answer choices");
        var buttons = new Dictionary<string, Button>(StringComparer.Ordinal);
        string? selectedId = null;
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        foreach (var option in options)
        {
            var button = new Button
            {
                Content = option.Label,
                Margin = new Thickness(0, 6, 8, 0),
                Classes = { "quiet" },
            };
            AutomationProperties.SetAutomationId(button, $"ScheduleReadOption_{option.Id}");
            AutomationProperties.SetName(button, $"Answer {option.Label}");
            button.Click += (_, _) =>
            {
                selectedId = option.Id;
                RefreshSelection();
                var outcome = TemplateInteractionEvaluator.EvaluateSingleSelection(
                    options,
                    answerId,
                    selectedId);
                TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
                reportOutcome(outcome);
            };
            buttons.Add(option.Id, button);
            optionPanel.Children.Add(button);
        }

        var questionContent = new StackPanel { Spacing = 6 };
        questionContent.Children.Add(questionText);
        questionContent.Children.Add(optionPanel);
        var questionCard = new PaperCard
        {
            Padding = new Thickness(14, 12),
            Content = questionContent,
        };
        questionCard.Classes.Add("soft");
        AutomationProperties.SetName(questionCard, "Synthetic opening-hours extraction question");

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = "Text-only schedule mode is active. Every day, time, question, and choice remains available.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(scheduleCard);
        root.Children.Add(questionCard);
        root.Children.Add(outcomePanel);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, scheduleCard, questionCard);
            if (!shouldReduceMotion)
            {
                scheduleCard.RenderTransform = TemplateRendering.Transform(-10, 10, -1.2, 0.98);
                questionCard.RenderTransform = TemplateRendering.Transform(10, 6, 0.8, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Move(TimeSpan.FromMilliseconds(520), scheduleCard, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(320), questionCard, 0, 0, 0, 1),
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
        skipButton.Click += (_, _) =>
        {
            scene?.Skip();
            scheduleTape.SkipEntrance();
        };
        return root;

        void RefreshSelection()
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

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "That time matches the requested opening-hours entry.",
        TemplateOutcomeState.Uncertain => "Choose one time from the synthetic opening hours.",
        TemplateOutcomeState.Failure => "Check the requested day and its printed opening time again.",
        _ => "Ready: find the requested day, then choose its opening time.",
    };
}

internal static class SpellingTilesRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        _ = imageCache;
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var word = TemplateRendering.Text(parameters, "word");
        var meaning = TemplateRendering.Localized(parameters, "meaning", instructionLanguage);
        var letters = TemplateRendering.Options(parameters, "letters");
        var letterNames = TemplateRendering.Options(parameters, "letter-names");
        var letterIds = letters.Select(letter => letter.Id).ToArray();
        var nameIds = letterNames.Select(letterName => letterName.Id).ToArray();
        if (letters.Count < 2 ||
            !letterIds.OrderBy(id => id, StringComparer.Ordinal)
                .SequenceEqual(nameIds.OrderBy(id => id, StringComparer.Ordinal), StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Spelling letters and letter names must declare the same IDs.");
        }

        if (!string.Equals(
            string.Concat(letters.Select(letter => letter.Label)),
            word,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Authored spelling letters must form the target word.");
        }

        var namesById = letterNames.ToDictionary(
            letterName => letterName.Id,
            letterName => letterName.Label,
            StringComparer.Ordinal);
        var selectedIds = InitialOrder(parameters.PreviewOutcome, letters).ToList();
        var bankButtons = new Dictionary<string, Button>(StringComparer.Ordinal);

        var replayButton = new Button { Content = "Replay tiles", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "SpellingTilesReplay");
        AutomationProperties.SetName(replayButton, "Replay the spelling-tile entrance");
        var skipButton = new Button { Content = "Skip tiles", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "SpellingTilesSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed spelling board");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Spelling instruction. {instruction}");
        var headerActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        headerActions.Children.Add(replayButton);
        headerActions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(headerActions, 1);
        header.Children.Add(headerActions);

        var meaningCard = new PaperCard
        {
            Padding = new Thickness(14, 10),
            Content = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock
                    {
                        Text = "TARGET MEANING",
                        FontSize = 12,
                        FontWeight = FontWeight.Bold,
                        Classes = { "muted" },
                    },
                    new TextBlock
                    {
                        Text = meaning,
                        FontSize = 20,
                        FontWeight = FontWeight.Bold,
                        TextWrapping = TextWrapping.Wrap,
                    },
                },
            },
        };
        meaningCard.Classes.Add("soft");
        AutomationProperties.SetName(meaningCard, $"Target meaning. {meaning}");

        var boardTape = new PaperTape { Content = "BUILD THE SPELLING", Angle = -1.1 };
        AutomationProperties.SetName(boardTape, "Build the spelling");
        var bankPanel = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            ItemHeight = 68,
        };
        AutomationProperties.SetName(bankPanel, "Available letter tiles with German letter names");
        var spellingPanel = new WrapPanel
        {
            MinHeight = 72,
            HorizontalAlignment = HorizontalAlignment.Center,
            ItemHeight = 64,
        };
        AutomationProperties.SetName(spellingPanel, "Selected letters in spelling order");
        var spellingStatus = new TextBlock
        {
            Text = "Current spelling is empty.",
            Classes = { "muted" },
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetLiveSetting(spellingStatus, AutomationLiveSetting.Polite);

        foreach (var letter in DeterministicBankOrder(letters))
        {
            var tileCopy = CreateTileCopy(letter.Label, namesById[letter.Id], onAccent: false);
            var button = new Button
            {
                Width = 76,
                Height = 62,
                Content = tileCopy,
                Margin = new Thickness(4),
                Classes = { "lift" },
            };
            AutomationProperties.SetAutomationId(button, $"SpellingTilesBank_{letter.Id}");
            AutomationProperties.SetName(
                button,
                $"Add letter {letter.Label}, German letter name {namesById[letter.Id]}");
            button.Click += (_, _) =>
            {
                if (!selectedIds.Contains(letter.Id, StringComparer.Ordinal))
                {
                    selectedIds.Add(letter.Id);
                    RefreshSpelling();
                }
            };
            bankButtons.Add(letter.Id, button);
            bankPanel.Children.Add(button);
        }

        var boardContent = new StackPanel { Spacing = 10 };
        boardContent.Children.Add(boardTape);
        boardContent.Children.Add(new TextBlock
        {
            Text = "LETTER BANK",
            Classes = { "eyebrow" },
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        boardContent.Children.Add(bankPanel);
        boardContent.Children.Add(new TextBlock
        {
            Text = "YOUR SPELLING",
            Classes = { "eyebrow" },
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        boardContent.Children.Add(spellingPanel);
        boardContent.Children.Add(spellingStatus);
        var boardCard = new PaperCard
        {
            Padding = new Thickness(18, 14),
            Content = boardContent,
            RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        };
        boardCard.Classes.Add("settings-sheet");
        AutomationProperties.SetName(boardCard, "Paper spelling board with removable letter tiles");

        var resetButton = new Button { Content = "Reset tiles", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(resetButton, "SpellingTilesReset");
        AutomationProperties.SetName(resetButton, "Return every letter to the bank");
        var checkButton = new Button { Content = "Check spelling", Classes = { "primary", "lift" } };
        AutomationProperties.SetAutomationId(checkButton, "SpellingTilesCheck");
        AutomationProperties.SetName(checkButton, "Check the selected letter order");
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { resetButton, checkButton },
        };
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        footer.Children.Add(outcomePanel);
        Grid.SetColumn(actions, 1);
        footer.Children.Add(actions);

        void RefreshSpelling()
        {
            spellingPanel.Children.Clear();
            foreach (var pair in bankButtons)
            {
                pair.Value.IsEnabled = !selectedIds.Contains(pair.Key, StringComparer.Ordinal);
            }

            foreach (var selectedId in selectedIds)
            {
                var letter = letters.Single(candidate => candidate.Id == selectedId);
                var tile = new Button
                {
                    Width = 76,
                    Height = 58,
                    Content = CreateTileCopy(letter.Label, namesById[letter.Id], onAccent: true),
                    Margin = new Thickness(4),
                    Classes = { "primary", "lift" },
                };
                AutomationProperties.SetAutomationId(tile, $"SpellingTilesSelected_{letter.Id}");
                AutomationProperties.SetName(
                    tile,
                    $"Selected letter {letter.Label}. Remove it from the spelling");
                tile.Click += (_, _) =>
                {
                    selectedIds.Remove(letter.Id);
                    RefreshSpelling();
                };
                spellingPanel.Children.Add(tile);
            }

            spellingStatus.Text = selectedIds.Count == 0
                ? "Current spelling is empty."
                : $"Current spelling: {string.Join(" ", selectedIds.Select(id => letters.Single(letter => letter.Id == id).Label))}.";
        }

        RefreshSpelling();
        resetButton.Click += (_, _) =>
        {
            selectedIds.Clear();
            RefreshSpelling();
            TemplateRendering.ApplyOutcome(
                outcomePanel,
                outcomeText,
                TemplateOutcomeState.Ready,
                OutcomeCopy);
        };
        checkButton.Click += (_, _) =>
        {
            var outcome = TemplateInteractionEvaluator.EvaluateWordOrder(letters, selectedIds);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            reportOutcome(outcome);
        };

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = "Text-only spelling mode is active. Every letter, letter name, and action remains available.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(meaningCard);
        root.Children.Add(boardCard);
        root.Children.Add(footer);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, meaningCard, boardCard, footer);
            if (!shouldReduceMotion)
            {
                boardCard.RenderTransform = TemplateRendering.Transform(0, 12, -1.1, 0.98);
                footer.RenderTransform = TemplateRendering.Transform(0, 8, 0, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), meaningCard),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(520), boardCard, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(300), footer, 0, 0, 0, 1),
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
        skipButton.Click += (_, _) =>
        {
            scene?.Skip();
            boardTape.SkipEntrance();
        };
        return root;
    }

    private static StackPanel CreateTileCopy(string letter, string letterName, bool onAccent)
    {
        var copy = new StackPanel
        {
            Spacing = 0,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        var letterText = new TextBlock
        {
            Text = letter,
            FontSize = 22,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        var nameText = new TextBlock
        {
            Text = letterName,
            FontSize = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        if (onAccent)
        {
            letterText.Classes.Add("on-accent");
            nameText.Classes.Add("on-accent");
        }

        copy.Children.Add(letterText);
        copy.Children.Add(nameText);
        return copy;
    }

    private static IReadOnlyList<TemplateOption> DeterministicBankOrder(
        IReadOnlyList<TemplateOption> letters) =>
        letters.Where((_, index) => index % 2 == 1)
            .Concat(letters.Where((_, index) => index % 2 == 0))
            .ToArray();

    private static IReadOnlyList<string> InitialOrder(
        TemplateOutcomeState state,
        IReadOnlyList<TemplateOption> letters) => state switch
        {
            TemplateOutcomeState.Success => letters.Select(letter => letter.Id).ToArray(),
            TemplateOutcomeState.Failure => letters.Reverse().Select(letter => letter.Id).ToArray(),
            TemplateOutcomeState.Uncertain => letters.Take(Math.Max(1, letters.Count / 2))
                .Select(letter => letter.Id)
                .ToArray(),
            _ => [],
        };

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The tiles match the authored spelling.",
        TemplateOutcomeState.Uncertain => "Add every letter before checking the spelling.",
        TemplateOutcomeState.Failure => "Every letter is present, but the order needs another pass.",
        _ => "Ready: build the spelling from left to right.",
    };
}
