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
using Linguistics.Core.Speech;

namespace Linguistics.App.Features.Learn.Templates;

internal sealed record ListeningPrompt(
    string Id,
    string ButtonLabel,
    string Transcript,
    string Seed,
    double Rate = 1);

internal static class ListeningTemplatePresentation
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
        AutomationProperties.SetName(skipButton, "Skip to the completed listening stage");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Listening instruction. {instruction}");
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actions.Children.Add(replayButton);
        actions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(actions, 1);
        header.Children.Add(actions);
        return header;
    }

    public static PaperCard CreatePlaybackPanel(
        string prefix,
        ISpeechSynthesisProvider? speechSynthesisProvider,
        string speechLanguageText,
        IReadOnlyList<ListeningPrompt> prompts,
        bool useTextOnlyFallback)
    {
        ArgumentNullException.ThrowIfNull(prompts);
        if (prompts.Count == 0)
        {
            throw new ArgumentException("At least one listening prompt is required.", nameof(prompts));
        }

        LanguageCode? speechLanguage = null;
        try
        {
            speechLanguage = new LanguageCode(speechLanguageText);
        }
        catch (ArgumentException)
        {
            // The written prompt remains complete when speech metadata is unusable.
        }

        var canPlay = speechSynthesisProvider is not null && speechLanguage is not null;
        var writtenPrompts = prompts
            .Select(prompt => prompt.Transcript)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var transcript = new TextBlock
        {
            Text = writtenPrompts.Length == 1
                ? $"Written prompt: {writtenPrompts[0]}"
                : string.Join(
                    Environment.NewLine,
                    writtenPrompts.Select((prompt, index) =>
                        $"Written prompt {index + 1}: {prompt}")),
            TextWrapping = TextWrapping.Wrap,
            IsVisible = useTextOnlyFallback || !canPlay,
        };
        AutomationProperties.SetAutomationId(transcript, $"{prefix}Transcript");
        AutomationProperties.SetName(transcript, "Complete written alternative for the listening prompt");
        AutomationProperties.SetLiveSetting(transcript, AutomationLiveSetting.Polite);

        var playbackStatus = new TextBlock
        {
            Text = canPlay
                ? "Optional local playback uses no microphone. A written prompt is available."
                : "Local playback is unavailable. The written prompt is shown.",
            Classes = { "muted" },
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetAutomationId(playbackStatus, $"{prefix}PlaybackStatus");
        AutomationProperties.SetLiveSetting(playbackStatus, AutomationLiveSetting.Polite);

        var playbackActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        var playButtons = new List<Button>();
        foreach (var prompt in prompts)
        {
            var playButton = new Button
            {
                Content = prompt.ButtonLabel,
                Classes = { "quiet" },
                IsEnabled = canPlay,
            };
            AutomationProperties.SetAutomationId(playButton, $"{prefix}Play{prompt.Id}");
            AutomationProperties.SetName(
                playButton,
                $"{prompt.ButtonLabel} with local system speech. No microphone is used");
            playbackActions.Children.Add(playButton);
            playButtons.Add(playButton);
        }

        var transcriptButton = new Button
        {
            Content = transcript.IsVisible ? "Hide written prompt" : "Show written prompt",
            Classes = { "quiet" },
            IsVisible = !useTextOnlyFallback,
        };
        AutomationProperties.SetAutomationId(transcriptButton, $"{prefix}ToggleTranscript");
        AutomationProperties.SetName(transcriptButton, "Show or hide the complete written prompt");
        transcriptButton.Click += (_, _) =>
        {
            transcript.IsVisible = !transcript.IsVisible;
            transcriptButton.Content = transcript.IsVisible
                ? "Hide written prompt"
                : "Show written prompt";
        };
        playbackActions.Children.Add(transcriptButton);

        var playbackGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 12,
        };
        playbackGrid.Children.Add(playbackStatus);
        Grid.SetColumn(playbackActions, 1);
        playbackGrid.Children.Add(playbackActions);
        var playbackCopy = new StackPanel { Spacing = 8 };
        playbackCopy.Children.Add(playbackGrid);
        playbackCopy.Children.Add(transcript);
        var playbackPanel = new PaperCard
        {
            Padding = new Thickness(12, 8),
            Content = playbackCopy,
        };
        playbackPanel.Classes.Add("soft");
        AutomationProperties.SetName(
            playbackPanel,
            "Optional local listening controls with a complete written alternative. No microphone is used.");

        var availabilityCancellation = new CancellationTokenSource();
        playbackPanel.AttachedToVisualTree += async (_, _) =>
        {
            if (speechSynthesisProvider is null || speechLanguage is not { } language)
            {
                return;
            }

            try
            {
                var snapshot = await speechSynthesisProvider.InspectAsync(availabilityCancellation.Token);
                canPlay = snapshot.Status == SpeechCapabilityStatus.Available &&
                          snapshot.Voices.Any(voice => voice.Language == language);
                foreach (var playButton in playButtons)
                {
                    playButton.IsEnabled = canPlay;
                }

                playbackStatus.Text = canPlay
                    ? "Optional local playback uses no microphone. A written prompt is available."
                    : "No matching local system voice is installed. The written prompt is shown.";
                if (!canPlay)
                {
                    transcript.IsVisible = true;
                    transcriptButton.Content = "Hide written prompt";
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                canPlay = false;
                foreach (var playButton in playButtons)
                {
                    playButton.IsEnabled = false;
                }

                transcript.IsVisible = true;
                transcriptButton.Content = "Hide written prompt";
                playbackStatus.Text = "Local playback check failed. The written prompt is shown.";
            }
        };

        CancellationTokenSource? playbackCancellation = null;
        foreach (var (prompt, playButton) in prompts.Zip(playButtons))
        {
            playButton.Click += async (_, _) =>
            {
                if (!canPlay || speechSynthesisProvider is null || speechLanguage is not { } language)
                {
                    transcript.IsVisible = true;
                    transcriptButton.Content = "Hide written prompt";
                    playbackStatus.Text = "Local playback is unavailable. The written prompt is shown.";
                    return;
                }

                playbackCancellation?.Cancel();
                playbackCancellation?.Dispose();
                playbackCancellation = new CancellationTokenSource();
                foreach (var button in playButtons)
                {
                    button.IsEnabled = false;
                }

                playbackStatus.Text = "Playing the authored prompt locally.";
                try
                {
                    var result = await speechSynthesisProvider.SpeakAsync(
                        new SpeechSynthesisRequest(
                            Guid.NewGuid(),
                            prompt.Transcript,
                            language,
                            prompt.Seed,
                            Rate: prompt.Rate),
                        playbackCancellation.Token);
                    playbackStatus.Text = result.Message;
                }
                catch (OperationCanceledException)
                {
                    playbackStatus.Text = "Playback stopped. The written prompt remains available.";
                }
                finally
                {
                    foreach (var button in playButtons)
                    {
                        button.IsEnabled = canPlay;
                    }
                }
            };
        }

        playbackPanel.DetachedFromVisualTree += (_, _) =>
        {
            availabilityCancellation.Cancel();
            availabilityCancellation.Dispose();
            playbackCancellation?.Cancel();
            playbackCancellation?.Dispose();
            playbackCancellation = null;
        };
        return playbackPanel;
    }
}

internal static class ListenPickImageRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ISpeechSynthesisProvider? speechSynthesisProvider,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var utterance = TemplateRendering.Text(parameters, "utterance");
        var speechLanguage = TemplateRendering.Text(parameters, "speech-language");
        var options = TemplateRendering.Options(parameters, "options");
        var answerId = TemplateRendering.Text(parameters, "answer");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var selectedId = parameters.PreviewOutcome switch
        {
            TemplateOutcomeState.Success => answerId,
            TemplateOutcomeState.Failure => options.First(option => option.Id != answerId).Id,
            _ => null,
        };
        var header = ListeningTemplatePresentation.CreateHeader(
            "ListenPickImage",
            instruction,
            "Replay choices",
            "Skip choices",
            out var replayButton,
            out var skipButton);
        var playbackPanel = ListeningTemplatePresentation.CreatePlaybackPanel(
            "ListenPickImage",
            speechSynthesisProvider,
            speechLanguage,
            [new ListeningPrompt("Prompt", "Play prompt", utterance, $"listen-pick-image:{answerId}")],
            parameters.UseTextOnlyFallback);

        var stage = TemplateRendering.CreateStage(318, "Listening image-choice stage");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var tape = new PaperTape { Content = "LISTEN, THEN CHOOSE", Angle = -1.2 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);

        var optionPanel = new WrapPanel
        {
            Margin = new Thickness(24, 72, 24, 18),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ItemWidth = 184,
            ItemHeight = parameters.UseTextOnlyFallback ? 108 : 190,
        };
        AutomationProperties.SetName(optionPanel, "Listening prompt image choices");
        PaperStage.SetLayer(optionPanel, PaperStageLayer.Subject);
        stage.Children.Add(optionPanel);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var buttons = new Dictionary<string, Button>(StringComparer.Ordinal);
        var renderedAssetReferences = new List<string?>();
        foreach (var option in options)
        {
            var image = parameters.UseTextOnlyFallback
                ? null
                : TemplateRendering.CreateContentImage(imageCache, option.AssetReferenceId, 104);
            if (image is not null)
            {
                renderedAssetReferences.Add(option.AssetReferenceId);
            }

            var copy = new StackPanel
            {
                Spacing = 7,
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
                FontSize = image is null ? 22 : 16,
                FontWeight = FontWeight.Bold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            var frame = new CutoutFrame
            {
                Width = 164,
                Height = image is null ? 88 : 170,
                Content = copy,
            };
            var button = new Button
            {
                Width = 176,
                Height = image is null ? 100 : 182,
                Padding = new Thickness(5),
                Content = frame,
                Classes = { "quiet", "lift" },
            };
            AutomationProperties.SetAutomationId(button, $"ListenPickImageOption_{option.Id}");
            AutomationProperties.SetName(button, $"Choose listening image {option.Label}");
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

        RefreshSelection();
        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        root.Children.Add(playbackPanel);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = "Text-only listening choice: the written prompt and every authored option remain available.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(stage);
        if (!parameters.UseTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                backdropRendered
                    ? renderedAssetReferences.Append(backdropReference)
                    : renderedAssetReferences,
                "ListenPickImageImageCredits") is { } credits)
        {
            root.Children.Add(credits);
        }

        root.Children.Add(outcomePanel);
        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, tape, optionPanel);
            if (!shouldReduceMotion)
            {
                optionPanel.RenderTransform = TemplateRendering.Transform(0, 12, -0.8, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(720), optionPanel, 0, 0, 0, 1),
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
            tape.SkipEntrance();
        };
        return root;

        void RefreshSelection()
        {
            foreach (var pair in buttons)
            {
                pair.Value.Classes.Remove("primary");
                pair.Value.Classes.Remove("quiet");
                if (string.Equals(pair.Key, selectedId, StringComparison.Ordinal))
                {
                    pair.Value.Classes.Add("primary");
                }
                else
                {
                    pair.Value.Classes.Add("quiet");
                }
            }
        }
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The cutout matches the authored listening prompt.",
        TemplateOutcomeState.Uncertain => "Choose one cutout after playing or reading the prompt.",
        TemplateOutcomeState.Failure => "That cutout does not match the authored listening prompt.",
        _ => "Ready: play the prompt or reveal its written alternative, then choose.",
    };
}

internal static class ListenOrderRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ISpeechSynthesisProvider? speechSynthesisProvider,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var utterance = TemplateRendering.Text(parameters, "utterance");
        var speechLanguage = TemplateRendering.Text(parameters, "speech-language");
        var events = TemplateRendering.Options(parameters, "events");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var selectedIds = parameters.PreviewOutcome switch
        {
            TemplateOutcomeState.Success => events.Select(item => item.Id).ToList(),
            TemplateOutcomeState.Failure => events.Reverse().Select(item => item.Id).ToList(),
            TemplateOutcomeState.Uncertain => events.Take(Math.Max(1, events.Count / 2))
                .Select(item => item.Id)
                .ToList(),
            _ => [],
        };
        var header = ListeningTemplatePresentation.CreateHeader(
            "ListenOrder",
            instruction,
            "Replay sequence",
            "Skip sequence",
            out var replayButton,
            out var skipButton);
        var playbackPanel = ListeningTemplatePresentation.CreatePlaybackPanel(
            "ListenOrder",
            speechSynthesisProvider,
            speechLanguage,
            [new ListeningPrompt("Prompt", "Play sequence", utterance, "listen-order:events")],
            parameters.UseTextOnlyFallback);

        var stage = TemplateRendering.CreateStage(360, "Listening event-order stage");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var tape = new PaperTape { Content = "PUT THE EVENTS IN ORDER", Angle = 1.1 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -34);
        stage.Children.Add(tape);

        var sequencePanel = new WrapPanel
        {
            Margin = new Thickness(26, 78, 26, 176),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            ItemWidth = 178,
            ItemHeight = 102,
        };
        AutomationProperties.SetName(sequencePanel, "Selected event cards in listening order");
        PaperStage.SetLayer(sequencePanel, PaperStageLayer.Subject);
        stage.Children.Add(sequencePanel);
        var bankPanel = new WrapPanel
        {
            Margin = new Thickness(26, 220, 26, 18),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            ItemWidth = 178,
            ItemHeight = 102,
        };
        AutomationProperties.SetName(bankPanel, "Available event cards");
        PaperStage.SetLayer(bankPanel, PaperStageLayer.ReactionBurst);
        stage.Children.Add(bankPanel);
        var status = new TextBlock
        {
            Classes = { "muted" },
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(status, "ListenOrderStatus");
        AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
        var bankButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
        foreach (var item in events)
        {
            var button = new Button
            {
                Width = 166,
                Height = 92,
                MinHeight = 82,
                Margin = new Thickness(5),
                Padding = new Thickness(6),
                Content = EventContent(item, compact: false),
                Classes = { "lesson-tile", "lift" },
            };
            AutomationProperties.SetAutomationId(button, $"ListenOrderBank_{item.Id}");
            AutomationProperties.SetName(button, $"Add event {item.Label} to the sequence");
            button.Click += (_, _) =>
            {
                if (!selectedIds.Contains(item.Id, StringComparer.Ordinal))
                {
                    selectedIds.Add(item.Id);
                    Refresh();
                }
            };
            bankButtons.Add(item.Id, button);
            bankPanel.Children.Add(button);
        }

        var resetButton = new Button { Content = "Reset", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(resetButton, "ListenOrderReset");
        var checkButton = new Button { Content = "Check order", Classes = { "primary", "lift" } };
        AutomationProperties.SetAutomationId(checkButton, "ListenOrderCheck");
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        actions.Children.Add(resetButton);
        actions.Children.Add(checkButton);
        var controls = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 12,
        };
        controls.Children.Add(status);
        Grid.SetColumn(actions, 1);
        controls.Children.Add(actions);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
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
            var outcome = TemplateInteractionEvaluator.EvaluateWordOrder(events, selectedIds);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            reportOutcome(outcome);
        };
        Refresh();

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        root.Children.Add(playbackPanel);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = $"Text-only listening order: {string.Join(", ", events.Select(item => item.Label))}.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(stage);
        root.Children.Add(controls);
        if (!parameters.UseTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                events.Select(item => item.AssetReferenceId)
                    .Append(backdropRendered ? backdropReference : null),
                "ListenOrderImageCredits") is { } credits)
        {
            root.Children.Add(credits);
        }

        root.Children.Add(outcomePanel);
        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, tape, sequencePanel, bankPanel, controls);
            if (!shouldReduceMotion)
            {
                sequencePanel.RenderTransform = TemplateRendering.Transform(-16, 0, -1, 0.98);
                bankPanel.RenderTransform = TemplateRendering.Transform(16, 0, 1, 0.98);
                controls.RenderTransform = TemplateRendering.Transform(0, 8, 0, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(560), sequencePanel, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(560), bankPanel, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(340), controls, 0, 0, 0, 1),
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
            tape.SkipEntrance();
        };
        return root;

        Control EventContent(TemplateOption item, bool compact)
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
                    item.AssetReferenceId,
                    compact ? 38 : 48) is { } image)
            {
                copy.Children.Add(image);
            }

            copy.Children.Add(new TextBlock
            {
                Text = item.Label,
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

            sequencePanel.Children.Clear();
            foreach (var (id, index) in selectedIds.Select((id, index) => (id, index)))
            {
                var item = events.Single(candidate => candidate.Id == id);
                var button = new Button
                {
                    Width = 166,
                    Height = 92,
                    Margin = new Thickness(5),
                    Padding = new Thickness(6),
                    Content = EventContent(item, compact: true),
                    Classes = { "primary", "lift" },
                };
                AutomationProperties.SetAutomationId(button, $"ListenOrderSelected_{item.Id}");
                AutomationProperties.SetName(
                    button,
                    $"Event {index + 1}: {item.Label}. Remove from the sequence");
                button.Click += (_, _) =>
                {
                    selectedIds.Remove(item.Id);
                    Refresh();
                };
                sequencePanel.Children.Add(button);
            }

            if (selectedIds.Count == 0)
            {
                var emptyCard = new PaperCard
                {
                    Width = 166,
                    Height = 92,
                    Padding = new Thickness(10),
                    Content = new TextBlock
                    {
                        Text = "Choose the first event card.",
                        Classes = { "muted" },
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                };
                emptyCard.Classes.Add("soft");
                AutomationProperties.SetName(emptyCard, "No event cards selected yet");
                sequencePanel.Children.Add(emptyCard);
            }

            status.Text = $"{selectedIds.Count} of {events.Count} event cards selected.";
        }
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The event cards match the authored listening order.",
        TemplateOutcomeState.Uncertain => "Add every event card before checking the order.",
        TemplateOutcomeState.Failure => "Every event is present, but the order needs another pass.",
        _ => "Ready: play or read the sequence, then order the event cards.",
    };
}

internal static class ListenTypeRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ISpeechSynthesisProvider? speechSynthesisProvider,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var utterance = TemplateRendering.Text(parameters, "utterance");
        var speechLanguage = TemplateRendering.Text(parameters, "speech-language");
        var acceptedAnswers = TemplateRendering.Options(parameters, "accepted-answers");
        var initialResponse = parameters.PreviewOutcome switch
        {
            TemplateOutcomeState.Success => acceptedAnswers[0].Label,
            TemplateOutcomeState.Failure => "Ich trinke Tee.",
            _ => string.Empty,
        };
        var header = ListeningTemplatePresentation.CreateHeader(
            "ListenType",
            instruction,
            "Replay typewriter",
            "Skip typewriter",
            out var replayButton,
            out var skipButton);
        var playbackPanel = ListeningTemplatePresentation.CreatePlaybackPanel(
            "ListenType",
            speechSynthesisProvider,
            speechLanguage,
            [new ListeningPrompt("Prompt", "Play dictation", utterance, "listen-type:dictation")],
            parameters.UseTextOnlyFallback);

        var stage = TemplateRendering.CreateStage(300, "Local dictation typewriter stage");
        TemplateRendering.AddBackdrop(stage, imageCache: null, assetReferenceId: null);
        var tape = new PaperTape { Content = "TYPE WHAT YOU HEAR", Angle = -1.1 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -34);
        stage.Children.Add(tape);

        var responseBox = new TextBox
        {
            Text = initialResponse,
            PlaceholderText = "Type the complete prompt",
            MinWidth = 500,
            FontSize = 20,
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(responseBox, "ListenTypeResponse");
        AutomationProperties.SetName(responseBox, "Type the complete listening prompt");
        var typewriterCopy = new StackPanel { Spacing = 12 };
        typewriterCopy.Children.Add(new TextBlock
        {
            Text = "LOCAL DICTATION",
            Classes = { "eyebrow" },
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        typewriterCopy.Children.Add(responseBox);
        typewriterCopy.Children.Add(new TextBlock
        {
            Text = "A  S  D  F     J  K  L",
            Classes = { "muted" },
            FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        var typewriter = new PaperCard
        {
            Width = 590,
            Height = 168,
            Padding = new Thickness(24, 18),
            Content = typewriterCopy,
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
        };
        typewriter.Classes.Add("soft");
        AutomationProperties.SetName(typewriter, "Paper typewriter with a local text response");
        PaperStage.SetLayer(typewriter, PaperStageLayer.Subject);
        PaperStage.SetAnchor(typewriter, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(typewriter, 0.5);
        stage.Children.Add(typewriter);

        var status = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(initialResponse)
                ? "No response typed yet."
                : "A local response is ready to check.",
            Classes = { "muted" },
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(status, "ListenTypeStatus");
        AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
        responseBox.TextChanged += (_, _) =>
        {
            status.Text = string.IsNullOrWhiteSpace(responseBox.Text)
                ? "No response typed yet."
                : "A local response is ready to check.";
        };
        var clearButton = new Button { Content = "Clear", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(clearButton, "ListenTypeClear");
        var checkButton = new Button { Content = "Check text", Classes = { "primary", "lift" } };
        AutomationProperties.SetAutomationId(checkButton, "ListenTypeCheck");
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        actions.Children.Add(clearButton);
        actions.Children.Add(checkButton);
        var controls = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 12,
        };
        controls.Children.Add(status);
        Grid.SetColumn(actions, 1);
        controls.Children.Add(actions);
        var toleranceCopy = new TextBlock
        {
            Text = "Case, repeated spaces, and final punctuation do not affect this deterministic check.",
            Classes = { "muted" },
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetName(
            toleranceCopy,
            "Dictation tolerance: case, repeated spaces, and final punctuation are ignored");
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        clearButton.Click += (_, _) =>
        {
            responseBox.Text = string.Empty;
            TemplateRendering.ApplyOutcome(
                outcomePanel,
                outcomeText,
                TemplateOutcomeState.Ready,
                OutcomeCopy);
        };
        checkButton.Click += (_, _) =>
        {
            var outcome = TemplateInteractionEvaluator.EvaluateDictation(
                acceptedAnswers,
                responseBox.Text);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            status.Text = outcome.State switch
            {
                TemplateOutcomeState.Success => "The bounded core check accepted this local response.",
                TemplateOutcomeState.Uncertain => "Type a response before checking.",
                _ => "The local response differs from the authored accepted text.",
            };
            reportOutcome(outcome);
        };

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        root.Children.Add(playbackPanel);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = "Text-only dictation: the complete written prompt and local text field remain available.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(stage);
        root.Children.Add(controls);
        root.Children.Add(toleranceCopy);
        root.Children.Add(outcomePanel);
        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, tape, typewriter, controls);
            if (!shouldReduceMotion)
            {
                typewriter.RenderTransform = TemplateRendering.Transform(0, 14, -1.1, 0.98);
                controls.RenderTransform = TemplateRendering.Transform(0, 8, 0, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(680), typewriter, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(340), controls, 0, 0, 0, 1),
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
            tape.SkipEntrance();
        };
        return root;
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The typed text matches an authored accepted answer.",
        TemplateOutcomeState.Uncertain => "Type a response before checking the dictation.",
        TemplateOutcomeState.Failure => "The typed text differs after the bounded core normalization.",
        _ => "Ready: play or read the prompt, then type it locally.",
    };
}

internal static class MinimalPairDoorsRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ISpeechSynthesisProvider? speechSynthesisProvider,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var utterance = TemplateRendering.Text(parameters, "utterance");
        var speechLanguage = TemplateRendering.Text(parameters, "speech-language");
        var options = TemplateRendering.Options(parameters, "options");
        var answerId = TemplateRendering.Text(parameters, "answer");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var selectedId = parameters.PreviewOutcome switch
        {
            TemplateOutcomeState.Success => answerId,
            TemplateOutcomeState.Failure => options.First(option => option.Id != answerId).Id,
            _ => null,
        };
        var header = ListeningTemplatePresentation.CreateHeader(
            "MinimalPairDoors",
            instruction,
            "Replay doors",
            "Skip doors",
            out var replayButton,
            out var skipButton);
        var playbackPanel = ListeningTemplatePresentation.CreatePlaybackPanel(
            "MinimalPairDoors",
            speechSynthesisProvider,
            speechLanguage,
            [new ListeningPrompt("Prompt", "Play sound", utterance, $"minimal-pair-doors:{answerId}", 0.88)],
            parameters.UseTextOnlyFallback);

        var stage = TemplateRendering.CreateStage(330, "Two listening doors for an authored minimal pair");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var tape = new PaperTape { Content = "WHICH DOOR MATCHES?", Angle = 1.1 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -34);
        stage.Children.Add(tape);

        var doorPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 32,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(24, 70, 24, 16),
        };
        AutomationProperties.SetName(doorPanel, "Two keyboard-operable minimal pair doors");
        PaperStage.SetLayer(doorPanel, PaperStageLayer.Subject);
        stage.Children.Add(doorPanel);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var buttons = new Dictionary<string, Button>(StringComparer.Ordinal);
        foreach (var option in options)
        {
            var doorCopy = new Grid
            {
                RowDefinitions = new RowDefinitions("*,Auto"),
                Margin = new Thickness(10),
            };
            doorCopy.Children.Add(new TextBlock
            {
                Text = option.Label,
                FontSize = 34,
                FontWeight = FontWeight.Bold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            });
            var handle = new TextBlock
            {
                Text = "●",
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            Grid.SetRow(handle, 1);
            doorCopy.Children.Add(handle);
            var door = new PaperCard
            {
                Width = 214,
                Height = 190,
                Padding = new Thickness(12),
                Content = doorCopy,
            };
            door.Classes.Add("soft");
            AutomationProperties.SetName(door, $"Paper door labelled {option.Label}");
            var button = new Button
            {
                Width = 228,
                Height = 204,
                Padding = new Thickness(6),
                Content = door,
                Classes = { "quiet", "lift" },
            };
            AutomationProperties.SetAutomationId(button, $"MinimalPairDoorsOption_{option.Id}");
            AutomationProperties.SetName(button, $"Open door {option.Label}");
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
            doorPanel.Children.Add(button);
        }

        RefreshSelection();
        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        root.Children.Add(playbackPanel);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = "Text-only sound choice: the written word and both door labels remain available.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(stage);
        if (!parameters.UseTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                backdropRendered ? [backdropReference] : [],
                "MinimalPairDoorsImageCredits") is { } credits)
        {
            root.Children.Add(credits);
        }

        root.Children.Add(outcomePanel);
        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, tape, doorPanel);
            if (!shouldReduceMotion)
            {
                doorPanel.RenderTransform = TemplateRendering.Transform(0, 15, 0.8, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(760), doorPanel, 0, 0, 0, 1),
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
            tape.SkipEntrance();
        };
        return root;

        void RefreshSelection()
        {
            foreach (var pair in buttons)
            {
                pair.Value.Classes.Remove("primary");
                pair.Value.Classes.Remove("quiet");
                if (string.Equals(pair.Key, selectedId, StringComparison.Ordinal))
                {
                    pair.Value.Classes.Add("primary");
                }
                else
                {
                    pair.Value.Classes.Add("quiet");
                }
            }
        }
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "That door matches the authored sound.",
        TemplateOutcomeState.Uncertain => "Choose one door after playing or reading the sound.",
        TemplateOutcomeState.Failure => "That door does not match the authored sound.",
        _ => "Ready: play the sound or reveal its written alternative, then choose.",
    };
}

internal static class ListenRouteRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ISpeechSynthesisProvider? speechSynthesisProvider,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var utterance = TemplateRendering.Text(parameters, "utterance");
        var speechLanguage = TemplateRendering.Text(parameters, "speech-language");
        var route = TemplateRendering.Options(parameters, "route");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var selectedIds = parameters.PreviewOutcome switch
        {
            TemplateOutcomeState.Success => route.Select(stop => stop.Id).ToList(),
            TemplateOutcomeState.Failure => route.Reverse().Select(stop => stop.Id).ToList(),
            TemplateOutcomeState.Uncertain => route.Take(Math.Max(1, route.Count / 2))
                .Select(stop => stop.Id)
                .ToList(),
            _ => [],
        };
        var header = ListeningTemplatePresentation.CreateHeader(
            "ListenRoute",
            instruction,
            "Replay route",
            "Skip route",
            out var replayButton,
            out var skipButton);
        var playbackPanel = ListeningTemplatePresentation.CreatePlaybackPanel(
            "ListenRoute",
            speechSynthesisProvider,
            speechLanguage,
            [new ListeningPrompt("Prompt", "Play directions", utterance, "listen-route:directions", 0.92)],
            parameters.UseTextOnlyFallback);

        var stage = TemplateRendering.CreateStage(372, "Paper map with an authored listening route");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var tape = new PaperTape { Content = "BUILD THE ROUTE", Angle = -1 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -34);
        stage.Children.Add(tape);

        var routePanel = new WrapPanel
        {
            Margin = new Thickness(26, 78, 26, 182),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            ItemWidth = 178,
            ItemHeight = 96,
        };
        AutomationProperties.SetName(routePanel, "Selected route stops in travel order");
        PaperStage.SetLayer(routePanel, PaperStageLayer.Subject);
        stage.Children.Add(routePanel);
        var bankPanel = new WrapPanel
        {
            Margin = new Thickness(26, 232, 26, 18),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            ItemWidth = 178,
            ItemHeight = 92,
        };
        AutomationProperties.SetName(bankPanel, "Available route stops");
        PaperStage.SetLayer(bankPanel, PaperStageLayer.ReactionBurst);
        stage.Children.Add(bankPanel);
        var status = new TextBlock
        {
            Classes = { "muted" },
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(status, "ListenRouteStatus");
        AutomationProperties.SetLiveSetting(status, AutomationLiveSetting.Polite);
        var bankButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
        foreach (var stop in route)
        {
            var button = new Button
            {
                Width = 166,
                Height = 82,
                MinHeight = 76,
                Margin = new Thickness(5),
                Padding = new Thickness(8),
                Content = StopCard(stop.Label, null),
                Classes = { "lesson-tile", "lift" },
            };
            AutomationProperties.SetAutomationId(button, $"ListenRouteBank_{stop.Id}");
            AutomationProperties.SetName(button, $"Add route stop {stop.Label}");
            button.Click += (_, _) =>
            {
                if (!selectedIds.Contains(stop.Id, StringComparer.Ordinal))
                {
                    selectedIds.Add(stop.Id);
                    Refresh();
                }
            };
            bankButtons.Add(stop.Id, button);
            bankPanel.Children.Add(button);
        }

        var resetButton = new Button { Content = "Reset", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(resetButton, "ListenRouteReset");
        var checkButton = new Button { Content = "Check route", Classes = { "primary", "lift" } };
        AutomationProperties.SetAutomationId(checkButton, "ListenRouteCheck");
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        actions.Children.Add(resetButton);
        actions.Children.Add(checkButton);
        var controls = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 12,
        };
        controls.Children.Add(status);
        Grid.SetColumn(actions, 1);
        controls.Children.Add(actions);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
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
            var outcome = TemplateInteractionEvaluator.EvaluateWordOrder(route, selectedIds);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            reportOutcome(outcome);
        };
        Refresh();

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        root.Children.Add(playbackPanel);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = $"Text-only route stops: {string.Join(", ", route.Select(stop => stop.Label))}.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(stage);
        root.Children.Add(controls);
        if (!parameters.UseTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                backdropRendered ? [backdropReference] : [],
                "ListenRouteImageCredits") is { } credits)
        {
            root.Children.Add(credits);
        }

        root.Children.Add(outcomePanel);
        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, tape, routePanel, bankPanel, controls);
            if (!shouldReduceMotion)
            {
                routePanel.RenderTransform = TemplateRendering.Transform(-15, 0, -1, 0.98);
                bankPanel.RenderTransform = TemplateRendering.Transform(15, 0, 1, 0.98);
                controls.RenderTransform = TemplateRendering.Transform(0, 8, 0, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(520), routePanel, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(520), bankPanel, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(320), controls, 0, 0, 0, 1),
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
            tape.SkipEntrance();
        };
        return root;

        Control StopCard(string label, int? sequence)
        {
            var copy = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            if (sequence is { } number)
            {
                var sequenceText = new TextBlock
                {
                    Text = number.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
                };
                sequenceText.Classes.Add("on-accent");
                copy.Children.Add(sequenceText);
            }

            var labelText = new TextBlock
            {
                Text = label,
                FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
            };
            if (sequence is not null)
            {
                labelText.Classes.Add("on-accent");
            }

            copy.Children.Add(labelText);
            return copy;
        }

        void Refresh()
        {
            foreach (var pair in bankButtons)
            {
                pair.Value.IsEnabled = !selectedIds.Contains(pair.Key, StringComparer.Ordinal);
            }

            routePanel.Children.Clear();
            foreach (var (id, index) in selectedIds.Select((id, index) => (id, index)))
            {
                var stop = route.Single(candidate => candidate.Id == id);
                var button = new Button
                {
                    Width = 166,
                    Height = 86,
                    Margin = new Thickness(5),
                    Padding = new Thickness(8),
                    Content = StopCard(stop.Label, index + 1),
                    Classes = { "primary", "lift" },
                };
                AutomationProperties.SetAutomationId(button, $"ListenRouteSelected_{stop.Id}");
                AutomationProperties.SetName(
                    button,
                    $"Route stop {index + 1}: {stop.Label}. Remove from the route");
                button.Click += (_, _) =>
                {
                    selectedIds.Remove(stop.Id);
                    Refresh();
                };
                routePanel.Children.Add(button);
            }

            if (selectedIds.Count == 0)
            {
                var emptyCard = new PaperCard
                {
                    Width = 166,
                    Height = 86,
                    Padding = new Thickness(10),
                    Content = new TextBlock
                    {
                        Text = "Choose the first map stop.",
                        Classes = { "muted" },
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                };
                emptyCard.Classes.Add("soft");
                AutomationProperties.SetName(emptyCard, "No route stops selected yet");
                routePanel.Children.Add(emptyCard);
            }

            status.Text = $"{selectedIds.Count} of {route.Count} route stops selected.";
        }
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The stops match the authored listening route.",
        TemplateOutcomeState.Uncertain => "Add every map stop before checking the route.",
        TemplateOutcomeState.Failure => "Every stop is present, but the route order needs another pass.",
        _ => "Ready: play or read the directions, then build the route.",
    };
}

internal static class ListenPriceTagRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ISpeechSynthesisProvider? speechSynthesisProvider,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var utterance = TemplateRendering.Text(parameters, "utterance");
        var speechLanguage = TemplateRendering.Text(parameters, "speech-language");
        var options = TemplateRendering.Options(parameters, "options");
        var answerId = TemplateRendering.Text(parameters, "answer");
        var selectedId = parameters.PreviewOutcome switch
        {
            TemplateOutcomeState.Success => answerId,
            TemplateOutcomeState.Failure => options.First(option => option.Id != answerId).Id,
            _ => null,
        };
        var header = ListeningTemplatePresentation.CreateHeader(
            "ListenPriceTag",
            instruction,
            "Replay tags",
            "Skip tags",
            out var replayButton,
            out var skipButton);
        var playbackPanel = ListeningTemplatePresentation.CreatePlaybackPanel(
            "ListenPriceTag",
            speechSynthesisProvider,
            speechLanguage,
            [new ListeningPrompt("Prompt", "Play price", utterance, $"listen-price-tag:{answerId}", 0.9)],
            parameters.UseTextOnlyFallback);

        var stage = TemplateRendering.CreateStage(292, "Listening price-tag counter");
        TemplateRendering.AddBackdrop(stage, imageCache: null, assetReferenceId: null);
        var tape = new PaperTape { Content = "SET THE PRICE TAG", Angle = 1 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);

        var tagPanel = new WrapPanel
        {
            Margin = new Thickness(24, 74, 24, 18),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ItemWidth = 190,
            ItemHeight = 150,
        };
        AutomationProperties.SetName(tagPanel, "Keyboard-operable paper price tags");
        PaperStage.SetLayer(tagPanel, PaperStageLayer.Subject);
        stage.Children.Add(tagPanel);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var buttons = new Dictionary<string, Button>(StringComparer.Ordinal);
        foreach (var option in options)
        {
            var tagCopy = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*"),
                Margin = new Thickness(8),
            };
            tagCopy.Children.Add(new TextBlock
            {
                Text = "○",
                Classes = { "muted" },
                HorizontalAlignment = HorizontalAlignment.Left,
            });
            var amount = new TextBlock
            {
                Text = option.Label,
                FontSize = 32,
                FontWeight = FontWeight.Bold,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetRow(amount, 1);
            tagCopy.Children.Add(amount);
            var tag = new PaperCard
            {
                Width = 168,
                Height = 128,
                Padding = new Thickness(10),
                Content = tagCopy,
            };
            tag.Classes.Add("soft");
            AutomationProperties.SetName(tag, $"Paper price tag {option.Label}");
            var button = new Button
            {
                Width = 182,
                Height = 142,
                Margin = new Thickness(4),
                Padding = new Thickness(6),
                Content = tag,
                Classes = { "quiet", "lift" },
            };
            AutomationProperties.SetAutomationId(button, $"ListenPriceTagOption_{option.Id}");
            AutomationProperties.SetName(button, $"Set price tag {option.Label}");
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
            tagPanel.Children.Add(button);
        }

        RefreshSelection();
        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        root.Children.Add(playbackPanel);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = $"Text-only price choices: {string.Join(", ", options.Select(option => option.Label))}.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(stage);
        root.Children.Add(outcomePanel);
        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, tape, tagPanel);
            if (!shouldReduceMotion)
            {
                tagPanel.RenderTransform = TemplateRendering.Transform(0, 14, -0.8, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(680), tagPanel, 0, 0, 0, 1),
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
            tape.SkipEntrance();
        };
        return root;

        void RefreshSelection()
        {
            foreach (var pair in buttons)
            {
                pair.Value.Classes.Remove("primary");
                pair.Value.Classes.Remove("quiet");
                if (string.Equals(pair.Key, selectedId, StringComparison.Ordinal))
                {
                    pair.Value.Classes.Add("primary");
                }
                else
                {
                    pair.Value.Classes.Add("quiet");
                }
            }
        }
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The tag matches the authored spoken price.",
        TemplateOutcomeState.Uncertain => "Choose one tag after playing or reading the price.",
        TemplateOutcomeState.Failure => "That tag does not match the authored spoken price.",
        _ => "Ready: play the price or reveal its written alternative, then choose.",
    };
}

internal static class DialogueEavesdropRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ISpeechSynthesisProvider? speechSynthesisProvider,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var speakerOne = TemplateRendering.Text(parameters, "speaker-one");
        var lineOne = TemplateRendering.Text(parameters, "line-one");
        var speakerTwo = TemplateRendering.Text(parameters, "speaker-two");
        var lineTwo = TemplateRendering.Text(parameters, "line-two");
        var speechLanguage = TemplateRendering.Text(parameters, "speech-language");
        var question = TemplateRendering.Text(parameters, "question");
        var options = TemplateRendering.Options(parameters, "options");
        var answerId = TemplateRendering.Text(parameters, "answer");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var selectedId = parameters.PreviewOutcome switch
        {
            TemplateOutcomeState.Success => answerId,
            TemplateOutcomeState.Failure => options.First(option => option.Id != answerId).Id,
            _ => null,
        };
        var header = ListeningTemplatePresentation.CreateHeader(
            "DialogueEavesdrop",
            instruction,
            "Replay exchange",
            "Skip exchange",
            out var replayButton,
            out var skipButton);
        var playbackPanel = ListeningTemplatePresentation.CreatePlaybackPanel(
            "DialogueEavesdrop",
            speechSynthesisProvider,
            speechLanguage,
            [
                new ListeningPrompt("One", $"Play {speakerOne}", lineOne, $"dialogue-eavesdrop:{speakerOne}:1"),
                new ListeningPrompt("Two", $"Play {speakerTwo}", lineTwo, $"dialogue-eavesdrop:{speakerTwo}:2"),
            ],
            parameters.UseTextOnlyFallback);

        var stage = TemplateRendering.CreateStage(336, $"Captioned exchange between {speakerOne} and {speakerTwo}");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var tape = new PaperTape { Content = "CAPTIONS STAY ON", Angle = -1.1 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -34);
        stage.Children.Add(tape);

        var firstSpeaker = CreateSpeaker(speakerOne, tiltLeft: true);
        PaperStage.SetLayer(firstSpeaker, PaperStageLayer.Subject);
        PaperStage.SetAnchor(firstSpeaker, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(firstSpeaker, 0.22);
        stage.Children.Add(firstSpeaker);
        var secondSpeaker = CreateSpeaker(speakerTwo, tiltLeft: false);
        PaperStage.SetLayer(secondSpeaker, PaperStageLayer.Subject);
        PaperStage.SetAnchor(secondSpeaker, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(secondSpeaker, 0.78);
        stage.Children.Add(secondSpeaker);
        var firstBubble = CreateBubble(speakerOne, lineOne, -1);
        PaperStage.SetLayer(firstBubble, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(firstBubble, PaperAnchorLine.Shoulder);
        PaperStage.SetAnchorX(firstBubble, 0.35);
        PaperStage.SetAnchorOffsetY(firstBubble, 0);
        stage.Children.Add(firstBubble);
        var secondBubble = CreateBubble(speakerTwo, lineTwo, 1);
        PaperStage.SetLayer(secondBubble, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(secondBubble, PaperAnchorLine.Shoulder);
        PaperStage.SetAnchorX(secondBubble, 0.65);
        PaperStage.SetAnchorOffsetY(secondBubble, 38);
        stage.Children.Add(secondBubble);

        var questionCopy = new StackPanel { Spacing = 10 };
        questionCopy.Children.Add(new TextBlock
        {
            Text = question,
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap,
        });
        var optionPanel = new WrapPanel
        {
            ItemWidth = 148,
            ItemHeight = 52,
        };
        AutomationProperties.SetName(optionPanel, "Dialogue comprehension choices");
        questionCopy.Children.Add(optionPanel);
        var questionCard = new PaperCard
        {
            Padding = new Thickness(18, 14),
            Content = questionCopy,
        };
        questionCard.Classes.Add("soft");
        AutomationProperties.SetName(questionCard, $"Comprehension question. {question}");
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var buttons = new Dictionary<string, Button>(StringComparer.Ordinal);
        foreach (var option in options)
        {
            var button = new Button
            {
                Width = 138,
                MinHeight = 44,
                Margin = new Thickness(4),
                Content = option.Label,
                Classes = { "quiet", "lift" },
            };
            AutomationProperties.SetAutomationId(button, $"DialogueEavesdropOption_{option.Id}");
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

        RefreshSelection();
        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        root.Children.Add(playbackPanel);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = $"Text-only dialogue. {speakerOne}: {lineOne} {speakerTwo}: {lineTwo}",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(stage);
        root.Children.Add(questionCard);
        if (!parameters.UseTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                backdropRendered ? [backdropReference] : [],
                "DialogueEavesdropImageCredits") is { } credits)
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
                tape,
                firstSpeaker,
                firstBubble,
                secondSpeaker,
                secondBubble,
                questionCard);
            if (!shouldReduceMotion)
            {
                firstSpeaker.RenderTransform = TemplateRendering.Transform(-28, 4, -2, 0.97);
                secondSpeaker.RenderTransform = TemplateRendering.Transform(28, 4, 2, 0.97);
                questionCard.RenderTransform = TemplateRendering.Transform(0, 8, 0, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(200), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(380), firstSpeaker, 0, 0, -1, 1),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(300), firstBubble),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(380), secondSpeaker, 0, 0, 1, 1),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(300), secondBubble),
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
            tape.SkipEntrance();
        };
        return root;

        void RefreshSelection()
        {
            foreach (var pair in buttons)
            {
                pair.Value.Classes.Remove("primary");
                pair.Value.Classes.Remove("quiet");
                if (string.Equals(pair.Key, selectedId, StringComparison.Ordinal))
                {
                    pair.Value.Classes.Add("primary");
                }
                else
                {
                    pair.Value.Classes.Add("quiet");
                }
            }
        }
    }

    private static CutoutFrame CreateSpeaker(string speaker, bool tiltLeft)
    {
        var name = new TextBlock
        {
            Text = speaker,
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var puppet = new CutoutFrame
        {
            Width = 146,
            Height = 126,
            Content = name,
            RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        };
        puppet.Classes.Add(tiltLeft ? "tilt-left" : "tilt-right");
        AutomationProperties.SetName(puppet, $"Speaker {speaker} represented by an authored text cutout");
        return puppet;
    }

    private static PaperCard CreateBubble(string speaker, string line, double angle)
    {
        var copy = new StackPanel { Spacing = 4 };
        copy.Children.Add(new TextBlock
        {
            Text = speaker,
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Classes = { "muted" },
        });
        copy.Children.Add(new TextBlock
        {
            Text = line,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
        var bubble = new PaperCard
        {
            Width = 218,
            Padding = new Thickness(14, 10),
            Content = copy,
        };
        PaperStage.SetLayerTransform(bubble, TemplateRendering.Transform(0, 0, angle, 1));
        AutomationProperties.SetName(bubble, $"{speaker} says: {line}");
        return bubble;
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The answer matches the authored exchange.",
        TemplateOutcomeState.Uncertain => "Choose one answer after following both captioned turns.",
        TemplateOutcomeState.Failure => "That answer does not match the authored exchange.",
        _ => "Ready: follow both turns, then answer the written question.",
    };
}
