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

internal static class SpeakingTemplatePresentation
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
        AutomationProperties.SetName(replayButton, replayLabel);
        skipButton = new Button { Content = skipLabel, Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, $"{prefix}Skip");
        AutomationProperties.SetName(skipButton, "Skip to the completed speaking stage");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Speaking instruction. {instruction}");
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actions.Children.Add(replayButton);
        actions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(actions, 1);
        header.Children.Add(actions);
        return header;
    }

    public static LanguageCode? TryLanguage(string value)
    {
        try
        {
            return new LanguageCode(value);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}

internal static class EchoStageRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ISpeechSynthesisProvider? speechSynthesisProvider,
        ISpeechRecognitionProvider? speechRecognitionProvider,
        IPronunciationAssessmentProvider? pronunciationAssessmentProvider,
        bool microphoneAllowed,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var phrase = TemplateRendering.Text(parameters, "phrase");
        var speechLanguageText = TemplateRendering.Text(parameters, "speech-language");
        var acceptedTranscripts = TemplateRendering.Options(parameters, "accepted-transcripts");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var speechLanguage = SpeakingTemplatePresentation.TryLanguage(speechLanguageText);
        var header = SpeakingTemplatePresentation.CreateHeader(
            "EchoStage",
            instruction,
            "Replay stage",
            "Skip stage",
            out var replayButton,
            out var skipButton);
        var playbackPanel = ListeningTemplatePresentation.CreatePlaybackPanel(
            "EchoStage",
            speechSynthesisProvider,
            speechLanguageText,
            [
                new ListeningPrompt(
                    "Normal",
                    "Play phrase",
                    phrase,
                    $"echo-stage:{acceptedTranscripts[0].Id}:normal"),
                new ListeningPrompt(
                    "Slower",
                    "Play slower",
                    phrase,
                    $"echo-stage:{acceptedTranscripts[0].Id}:slower",
                    Rate: 0.72),
            ],
            parameters.UseTextOnlyFallback);

        var stage = TemplateRendering.CreateStage(292, "Echo speaking stage");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var tape = new PaperTape { Content = "HEAR IT, THEN ECHO", Angle = -1.1 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);

        var phraseCopy = new StackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        phraseCopy.Children.Add(new TextBlock
        {
            Text = "EXPECTED PHRASE",
            Classes = { "eyebrow" },
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        phraseCopy.Children.Add(new TextBlock
        {
            Text = phrase,
            FontSize = 27,
            FontWeight = FontWeight.Bold,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        var phraseStrip = new CutoutFrame
        {
            Width = 500,
            MinHeight = 126,
            Padding = new Thickness(26, 20),
            Content = phraseCopy,
        };
        phraseStrip.Classes.Add("tilt-left");
        AutomationProperties.SetAutomationId(phraseStrip, "EchoStageExpectedPhrase");
        AutomationProperties.SetName(phraseStrip, $"Expected phrase. {phrase}");
        PaperStage.SetLayer(phraseStrip, PaperStageLayer.Subject);
        PaperStage.SetAnchor(phraseStrip, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(phraseStrip, 0.5);
        PaperStage.SetAnchorOffsetY(phraseStrip, -18);
        stage.Children.Add(phraseStrip);

        var echoStamp = new PaperStamp { Content = "ECHO", Angle = 2.2 };
        echoStamp.Classes.Add("rectangle");
        AutomationProperties.SetName(echoStamp, "Echo practice stamp");
        PaperStage.SetLayer(echoStamp, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(echoStamp, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(echoStamp, 0.68);
        PaperStage.SetAnchorOffsetY(echoStamp, -22);
        stage.Children.Add(echoStamp);

        var responseBox = new TextBox
        {
            Text = parameters.PreviewOutcome switch
            {
                TemplateOutcomeState.Success => acceptedTranscripts[0].Label,
                TemplateOutcomeState.Failure => "Ich nehme einen Kaffee.",
                _ => string.Empty,
            },
            PlaceholderText = "Type what you said",
            MaxLength = 500,
        };
        AutomationProperties.SetAutomationId(responseBox, "EchoStageTextResponse");
        AutomationProperties.SetName(
            responseBox,
            "Typed wording for the microphone-free echo comparison");
        var compareButton = new Button { Content = "Compare typed wording", Classes = { "primary" } };
        AutomationProperties.SetAutomationId(compareButton, "EchoStageCompareText");
        AutomationProperties.SetName(
            compareButton,
            "Compare typed wording. This does not assess pronunciation");

        var voiceButton = new Button { Content = "Use local microphone", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(voiceButton, "EchoStageRequestMicrophone");
        AutomationProperties.SetName(
            voiceButton,
            "Review the local microphone disclosure before optional echo recognition");
        var confirmButton = new Button { Content = "Start local recognition", Classes = { "primary" } };
        AutomationProperties.SetAutomationId(confirmButton, "EchoStageConfirmMicrophone");
        AutomationProperties.SetName(
            confirmButton,
            "Start local microphone recognition for up to fifteen seconds");
        var dismissButton = new Button { Content = "Keep typing", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(dismissButton, "EchoStageDismissMicrophone");
        AutomationProperties.SetName(dismissButton, "Dismiss microphone disclosure and keep typing");
        var consentActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        consentActions.Children.Add(confirmButton);
        consentActions.Children.Add(dismissButton);
        var consentCopy = new StackPanel { Spacing = 8 };
        consentCopy.Children.Add(new TextBlock
        {
            Text = "A local speech process will use the microphone for up to 15 seconds.",
            TextWrapping = TextWrapping.Wrap,
        });
        consentCopy.Children.Add(new TextBlock
        {
            Text = "Audio is not retained. You can cancel or keep typing.",
            TextWrapping = TextWrapping.Wrap,
            Classes = { "muted" },
        });
        consentCopy.Children.Add(consentActions);
        var consentPanel = new Border
        {
            Padding = new Thickness(12, 10),
            Child = consentCopy,
            IsVisible = false,
        };
        consentPanel.Classes.Add("warning-card");
        AutomationProperties.SetAutomationId(consentPanel, "EchoStageMicrophoneDisclosure");
        AutomationProperties.SetName(
            consentPanel,
            "Local microphone disclosure. Audio is not retained and text remains available");

        var recognitionConfigured =
            microphoneAllowed &&
            speechRecognitionProvider is not null &&
            pronunciationAssessmentProvider is not null &&
            speechLanguage is not null &&
            !parameters.UseTextOnlyFallback;
        voiceButton.IsVisible = !parameters.UseTextOnlyFallback;
        voiceButton.IsEnabled = recognitionConfigured;
        var recognitionStatus = new TextBlock
        {
            Text = parameters.UseTextOnlyFallback
                ? "Text-only practice is active. Pronunciation is not assessed."
                : recognitionConfigured
                    ? "Optional local recognition is available after confirmation."
                    : microphoneAllowed
                        ? "Local recognition is unavailable. Typed practice remains complete."
                        : "Your microphone preference is off. Typed practice remains complete.",
            TextWrapping = TextWrapping.Wrap,
            Classes = { "muted" },
        };
        AutomationProperties.SetAutomationId(recognitionStatus, "EchoStageRecognitionStatus");
        AutomationProperties.SetLiveSetting(recognitionStatus, AutomationLiveSetting.Polite);
        var evidenceLimit = new TextBlock
        {
            Text = "Recognition can show intelligibility and word differences. It cannot score phonemes, accent, or native-likeness.",
            TextWrapping = TextWrapping.Wrap,
            Classes = { "muted" },
        };
        AutomationProperties.SetName(evidenceLimit, "Limits of the echo assessment");
        var comparisonText = new TextBlock
        {
            IsVisible = false,
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(comparisonText, "EchoStageComparison");
        AutomationProperties.SetLiveSetting(comparisonText, AutomationLiveSetting.Polite);

        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var responseActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        responseActions.Children.Add(compareButton);
        responseActions.Children.Add(voiceButton);
        var practiceCopy = new StackPanel { Spacing = 9 };
        practiceCopy.Children.Add(new TextBlock
        {
            Text = "Microphone-free route",
            FontWeight = FontWeight.SemiBold,
        });
        practiceCopy.Children.Add(responseBox);
        practiceCopy.Children.Add(responseActions);
        practiceCopy.Children.Add(consentPanel);
        practiceCopy.Children.Add(recognitionStatus);
        practiceCopy.Children.Add(evidenceLimit);
        practiceCopy.Children.Add(comparisonText);
        var practiceCard = new PaperCard
        {
            Padding = new Thickness(16, 14),
            Content = practiceCopy,
        };
        practiceCard.Classes.Add("soft");
        AutomationProperties.SetName(
            practiceCard,
            "Echo comparison with a complete typed route and optional local recognition");

        compareButton.Click += (_, _) =>
        {
            var outcome = TemplateInteractionEvaluator.EvaluateDictation(
                acceptedTranscripts,
                responseBox.Text);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            comparisonText.Text = string.IsNullOrWhiteSpace(responseBox.Text)
                ? "No typed wording was provided. Pronunciation was not assessed."
                : $"Typed wording: {responseBox.Text.Trim()}";
            comparisonText.IsVisible = true;
            recognitionStatus.Text = "Typed wording was compared. Pronunciation was not assessed.";
            reportOutcome(outcome);
        };
        voiceButton.Click += (_, _) => consentPanel.IsVisible = true;
        dismissButton.Click += (_, _) => consentPanel.IsVisible = false;

        var availabilityCancellation = new CancellationTokenSource();
        CancellationTokenSource? recognitionCancellation = null;
        Guid? activeRequestId = null;
        confirmButton.Click += async (_, _) =>
        {
            if (!recognitionConfigured ||
                speechRecognitionProvider is null ||
                pronunciationAssessmentProvider is null ||
                speechLanguage is not { } language)
            {
                consentPanel.IsVisible = false;
                recognitionStatus.Text = "Local recognition is unavailable. Typed practice remains complete.";
                return;
            }

            recognitionCancellation?.Cancel();
            recognitionCancellation?.Dispose();
            recognitionCancellation = new CancellationTokenSource();
            var request = new SpeechRecognitionRequest(
                Guid.NewGuid(),
                language,
                TimeSpan.FromSeconds(15),
                RetainAudio: false);
            activeRequestId = request.RequestId;
            consentPanel.IsVisible = false;
            voiceButton.IsEnabled = false;
            recognitionStatus.Text = "Microphone active. Recognition remains local and audio is not retained.";
            try
            {
                var result = await speechRecognitionProvider.RecognizeAsync(
                    request,
                    recognitionCancellation.Token);
                if (activeRequestId != result.RequestId)
                {
                    return;
                }

                if (result.Status == SpeechRecognitionResultStatus.Accepted &&
                    !string.IsNullOrWhiteSpace(result.Transcript))
                {
                    var assessment = pronunciationAssessmentProvider.Assess(
                        new PronunciationAssessmentRequest(
                            phrase,
                            result.Transcript,
                            result.Duration),
                        result.ProviderVersion);
                    var outcome = TemplateInteractionEvaluator.EvaluatePronunciationAssessment(
                        assessment.Evidence.Outcome);
                    TemplateRendering.ApplyOutcome(
                        outcomePanel,
                        outcomeText,
                        outcome.State,
                        OutcomeCopy);
                    comparisonText.Text = $"Expected: {phrase}{Environment.NewLine}Recognized: {result.Transcript}";
                    comparisonText.IsVisible = true;
                    recognitionStatus.Text = assessment.Message;
                    reportOutcome(outcome);
                }
                else if (result.Status == SpeechRecognitionResultStatus.NoSpeech)
                {
                    var outcome = TemplateInteractionEvaluator.EvaluatePronunciationAssessment(
                        PronunciationAssessmentOutcome.NoSpeech);
                    TemplateRendering.ApplyOutcome(
                        outcomePanel,
                        outcomeText,
                        outcome.State,
                        OutcomeCopy);
                    recognitionStatus.Text = result.Message;
                    reportOutcome(outcome);
                }
                else
                {
                    recognitionStatus.Text = result.Message;
                }
            }
            catch (OperationCanceledException)
            {
                recognitionStatus.Text = "Recognition cancelled. Typed practice remains complete.";
            }
            catch (Exception)
            {
                recognitionStatus.Text = "Local recognition failed. Typed practice remains complete.";
            }
            finally
            {
                if (activeRequestId == request.RequestId)
                {
                    activeRequestId = null;
                    voiceButton.IsEnabled = recognitionConfigured;
                }
            }
        };

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        root.Children.Add(playbackPanel);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = "Text-only echo practice: read the phrase and compare typed wording without a microphone.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(stage);
        if (!parameters.UseTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                backdropRendered ? [backdropReference] : [],
                "EchoStageImageCredits") is { } credits)
        {
            root.Children.Add(credits);
        }

        root.Children.Add(practiceCard);
        root.Children.Add(outcomePanel);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, tape, phraseStrip, echoStamp, practiceCard);
            if (!shouldReduceMotion)
            {
                phraseStrip.RenderTransform = TemplateRendering.Transform(-10, 8, -1.4, 0.98);
                echoStamp.RenderTransform = TemplateRendering.Transform(8, 6, 2.2, 0.96);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(620), phraseStrip, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(340), echoStamp, 0, 0, 0, 1),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(260), practiceCard),
            ]);
            await scene.PlayAsync(shouldReduceMotion);
        }

        root.AttachedToVisualTree += async (_, _) =>
        {
            await PlayAsync();
            if (!recognitionConfigured || speechRecognitionProvider is null)
            {
                return;
            }

            try
            {
                var snapshot = await speechRecognitionProvider.InspectAsync(
                    availabilityCancellation.Token);
                recognitionConfigured = snapshot.Status == SpeechCapabilityStatus.Available;
                voiceButton.IsEnabled = recognitionConfigured;
                recognitionStatus.Text = recognitionConfigured
                    ? "Optional local recognition is available after confirmation."
                    : $"{snapshot.Message} Typed practice remains complete.";
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                recognitionConfigured = false;
                voiceButton.IsEnabled = false;
                recognitionStatus.Text = "Local recognition check failed. Typed practice remains complete.";
            }
        };
        root.DetachedFromVisualTree += (_, _) =>
        {
            scene?.Skip();
            scene?.Dispose();
            scene = null;
            availabilityCancellation.Cancel();
            availabilityCancellation.Dispose();
            recognitionCancellation?.Cancel();
            recognitionCancellation?.Dispose();
            recognitionCancellation = null;
            activeRequestId = null;
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
        TemplateOutcomeState.Success => "The authored wording matched, or local recognition found it intelligible.",
        TemplateOutcomeState.Uncertain => "No complete comparison yet, or local recognition found only partial evidence.",
        TemplateOutcomeState.Failure => "The wording differed, or local recognition found substantial intelligibility loss.",
        _ => "Ready: listen or read, echo aloud if you wish, then choose a local comparison path.",
    };
}
