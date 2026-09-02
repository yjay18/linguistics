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
using Linguistics.Core.Speech;

namespace Linguistics.App.Features.Learn.Templates;

internal static class PictureMatchRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ISpeechSynthesisProvider? speechSynthesisProvider,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var prompt = TemplateRendering.Localized(parameters, "prompt", instructionLanguage);
        var options = TemplateRendering.Options(parameters, "options");
        var answerId = TemplateRendering.Text(parameters, "answer");
        var spokenText = TemplateRendering.Text(parameters, "spoken-text");
        var speechLanguageText = TemplateRendering.Text(parameters, "speech-language");
        LanguageCode? speechLanguage = null;
        try
        {
            speechLanguage = new LanguageCode(speechLanguageText);
        }
        catch (ArgumentException)
        {
            // The written target remains complete when authored speech metadata is unusable.
        }
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var useVisuals = !parameters.UseTextOnlyFallback &&
            options.Any(option => imageCache?.TryGetBitmap(option.AssetReferenceId, out _) == true);

        var replayButton = new Button
        {
            Content = AppStrings.Get("Template_ReplayReveal"),
            Classes = { "quiet" },
        };
        AutomationProperties.SetAutomationId(replayButton, "PictureMatchReplay");
        var skipButton = new Button
        {
            Content = AppStrings.Get("Template_SkipReveal"),
            Classes = { "quiet" },
        };
        AutomationProperties.SetAutomationId(skipButton, "PictureMatchSkip");
        var promptText = new TextBlock
        {
            Text = prompt,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(
            promptText,
            AppStrings.Format("Template_PictureMatch_Prompt", prompt));
        var sceneActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
        };
        sceneActions.Children.Add(replayButton);
        sceneActions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(promptText);
        Grid.SetColumn(sceneActions, 1);
        header.Children.Add(sceneActions);

        var stage = TemplateRendering.CreateStage(
            304,
            AppStrings.Format("Template_PictureMatch_Stage", prompt));
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var tape = new PaperTape
        {
            Content = AppStrings.Get("Template_PictureMatch_Tape"),
            Angle = 1.2,
        };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -11);
        stage.Children.Add(tape);

        var optionPanel = new WrapPanel
        {
            Margin = new Thickness(28, 68, 28, 20),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ItemWidth = 174,
            ItemHeight = useVisuals ? 188 : 118,
        };
        PaperStage.SetLayer(optionPanel, PaperStageLayer.Subject);
        stage.Children.Add(optionPanel);

        var buttons = new Dictionary<string, Button>(StringComparer.Ordinal);
        string? selectedId = InitialSelection(parameters.PreviewOutcome, options, answerId);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);

        var renderedAssetReferences = new List<string?>();
        foreach (var option in options)
        {
            var image = parameters.UseTextOnlyFallback
                ? null
                : TemplateRendering.CreateContentImage(imageCache, option.AssetReferenceId, 102);
            var optionCopy = new StackPanel
            {
                Spacing = 7,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = image is null
                    ? VerticalAlignment.Center
                    : VerticalAlignment.Stretch,
            };
            if (image is not null)
            {
                renderedAssetReferences.Add(option.AssetReferenceId);
                optionCopy.Children.Add(image);
            }

            optionCopy.Children.Add(new TextBlock
            {
                Text = option.Label,
                FontSize = image is null ? 22 : 16,
                FontWeight = FontWeight.Bold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = image is null ? VerticalAlignment.Center : VerticalAlignment.Bottom,
            });
            var frame = new CutoutFrame
            {
                Width = 154,
                Height = image is null ? 92 : 166,
                Content = optionCopy,
            };
            frame.Classes.Add(buttons.Count % 2 == 0 ? "tilt-left" : "tilt-right");
            var button = new Button
            {
                Width = 166,
                Height = image is null ? 106 : 180,
                Padding = new Thickness(5),
                Content = frame,
                Classes = { "quiet", "lift" },
            };
            AutomationProperties.SetName(
                button,
                AppStrings.Format("Template_ChooseOption", option.Label));
            AutomationProperties.SetAutomationId(button, $"PictureMatchOption_{option.Id}");
            button.Click += (_, _) =>
            {
                selectedId = option.Id;
                UpdateSelection(buttons, selectedId);
                var outcome = TemplateInteractionEvaluator.EvaluatePictureMatch(
                    options,
                    answerId,
                    selectedId);
                TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
                reportOutcome(outcome);
            };
            buttons.Add(option.Id, button);
            optionPanel.Children.Add(button);
        }

        UpdateSelection(buttons, selectedId);
        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        var playButton = new Button
        {
            Content = AppStrings.Format("Template_PlayTarget", spokenText),
            Classes = { "quiet" },
            IsEnabled = speechSynthesisProvider is not null && speechLanguage is not null,
        };
        AutomationProperties.SetAutomationId(playButton, "PictureMatchPlayTarget");
        AutomationProperties.SetName(
            playButton,
            AppStrings.Format("Template_PlayTarget_Automation", spokenText));
        var playbackStatus = new TextBlock
        {
            Text = playButton.IsEnabled
                ? AppStrings.Format("Template_WrittenTarget_Playback", spokenText)
                : AppStrings.Format("Template_WrittenTarget_Unavailable", spokenText),
            Classes = { "muted" },
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetAutomationId(playbackStatus, "PictureMatchPlaybackStatus");
        AutomationProperties.SetLiveSetting(playbackStatus, AutomationLiveSetting.Polite);
        var playbackGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        playbackGrid.Children.Add(playbackStatus);
        Grid.SetColumn(playButton, 1);
        playbackGrid.Children.Add(playButton);
        var playbackPanel = new PaperCard
        {
            Padding = new Thickness(12, 8),
            Content = playbackGrid,
        };
        playbackPanel.Classes.Add("soft");
        AutomationProperties.SetName(
            playbackPanel,
            AppStrings.Format("Template_WrittenTarget_Automation", spokenText));
        root.Children.Add(playbackPanel);

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
                var hasMatchingVoice = snapshot.Status == SpeechCapabilityStatus.Available &&
                                       snapshot.Voices.Any(voice => voice.Language == language);
                playButton.IsEnabled = hasMatchingVoice;
                playbackStatus.Text = hasMatchingVoice
                    ? AppStrings.Format("Template_WrittenTarget_Playback", spokenText)
                    : AppStrings.Format("Template_WrittenTarget_NoVoice", spokenText);
            }
            catch (OperationCanceledException)
            {
            }
        };

        CancellationTokenSource? playbackCancellation = null;
        playButton.Click += async (_, _) =>
        {
            if (speechSynthesisProvider is null || speechLanguage is not { } language)
            {
                playbackStatus.Text = AppStrings.Format(
                    "Template_WrittenTarget_Unavailable",
                    spokenText);
                return;
            }

            playbackCancellation?.Cancel();
            playbackCancellation?.Dispose();
            playbackCancellation = new CancellationTokenSource();
            playButton.IsEnabled = false;
            playbackStatus.Text = AppStrings.Format("Template_PlayingTarget", spokenText);
            try
            {
                var result = await speechSynthesisProvider.SpeakAsync(
                    new SpeechSynthesisRequest(
                        Guid.NewGuid(),
                        spokenText,
                        language,
                        $"picture-match:{answerId}"),
                    playbackCancellation.Token);
                playbackStatus.Text = result.Message;
            }
            catch (OperationCanceledException)
            {
                playbackStatus.Text = AppStrings.Format(
                    "Template_PlaybackStopped",
                    spokenText);
            }
            finally
            {
                playButton.IsEnabled = true;
            }
        };
        playbackPanel.DetachedFromVisualTree += (_, _) =>
        {
            availabilityCancellation.Cancel();
            availabilityCancellation.Dispose();
            playbackCancellation?.Cancel();
            playbackCancellation?.Dispose();
            playbackCancellation = null;
        };

        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = AppStrings.Get("Template_PictureMatch_TextOnly"),
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
                "PictureMatchImageCredits") is { } credits)
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
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(250), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(1_350), optionPanel, 0, 0, 0, 1),
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
    }

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

    private static string? InitialSelection(
        TemplateOutcomeState state,
        IReadOnlyList<TemplateOption> options,
        string answerId) => state switch
        {
            TemplateOutcomeState.Success => answerId,
            TemplateOutcomeState.Failure => options.First(option => option.Id != answerId).Id,
            _ => null,
        };

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => AppStrings.Get("Template_PictureMatch_Success"),
        TemplateOutcomeState.Uncertain => AppStrings.Get("Template_PictureMatch_Uncertain"),
        TemplateOutcomeState.Failure => AppStrings.Get("Template_PictureMatch_Failure"),
        _ => AppStrings.Get("Template_PictureMatch_Ready"),
    };
}
