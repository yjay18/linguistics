using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Diagnostics;
using System.Globalization;
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

internal static class SpeakingComparisonCard
{
    public static PaperCard Create(
        string prefix,
        IReadOnlyList<TemplateOption> acceptedTranscripts,
        string speechLanguageText,
        ISpeechRecognitionProvider? speechRecognitionProvider,
        IPronunciationAssessmentProvider? pronunciationAssessmentProvider,
        bool microphoneAllowed,
        bool useTextOnlyFallback,
        TemplateOutcomeState previewOutcome,
        Border outcomePanel,
        TextBlock outcomeText,
        Func<TemplateOutcomeState, string> outcomeCopy,
        Action<TemplateOutcome> reportOutcome)
    {
        var speechLanguage = SpeakingTemplatePresentation.TryLanguage(speechLanguageText);
        var responseBox = new TextBox
        {
            Text = previewOutcome switch
            {
                TemplateOutcomeState.Success => acceptedTranscripts[0].Label,
                TemplateOutcomeState.Failure => "Anderer Text.",
                _ => string.Empty,
            },
            PlaceholderText = "Type the wording you practiced",
            MaxLength = 500,
        };
        AutomationProperties.SetAutomationId(responseBox, $"{prefix}TextResponse");
        AutomationProperties.SetName(
            responseBox,
            "Typed wording for the complete microphone-free comparison");
        var compareButton = new Button
        {
            Content = "Compare typed wording",
            Classes = { "primary" },
        };
        AutomationProperties.SetAutomationId(compareButton, $"{prefix}CompareText");
        AutomationProperties.SetName(
            compareButton,
            "Compare typed wording. This does not assess pronunciation");

        var voiceButton = new Button { Content = "Use local microphone", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(voiceButton, $"{prefix}RequestMicrophone");
        AutomationProperties.SetName(
            voiceButton,
            "Review the local microphone disclosure before optional recognition");
        var confirmButton = new Button
        {
            Content = "Start local recognition",
            Classes = { "primary" },
        };
        AutomationProperties.SetAutomationId(confirmButton, $"{prefix}ConfirmMicrophone");
        AutomationProperties.SetName(
            confirmButton,
            "Start local microphone recognition for up to fifteen seconds");
        var dismissButton = new Button { Content = "Keep typing", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(dismissButton, $"{prefix}DismissMicrophone");
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
        AutomationProperties.SetAutomationId(consentPanel, $"{prefix}MicrophoneDisclosure");
        AutomationProperties.SetName(
            consentPanel,
            "Local microphone disclosure. Audio is not retained and text remains available");

        var recognitionConfigured =
            microphoneAllowed &&
            speechRecognitionProvider is not null &&
            pronunciationAssessmentProvider is not null &&
            speechLanguage is not null &&
            !useTextOnlyFallback;
        voiceButton.IsVisible = !useTextOnlyFallback;
        voiceButton.IsEnabled = recognitionConfigured;
        var recognitionStatus = new TextBlock
        {
            Text = useTextOnlyFallback
                ? "Text-only practice is active. Pronunciation is not assessed."
                : recognitionConfigured
                    ? "Optional local recognition is available after confirmation."
                    : microphoneAllowed
                        ? "Local recognition is unavailable. Typed practice remains complete."
                        : "Your microphone preference is off. Typed practice remains complete.",
            TextWrapping = TextWrapping.Wrap,
            Classes = { "muted" },
        };
        AutomationProperties.SetAutomationId(recognitionStatus, $"{prefix}RecognitionStatus");
        AutomationProperties.SetLiveSetting(recognitionStatus, AutomationLiveSetting.Polite);
        var evidenceLimit = new TextBlock
        {
            Text = "Recognition can show intelligibility and word differences. It cannot score phonemes, accent, or native-likeness.",
            TextWrapping = TextWrapping.Wrap,
            Classes = { "muted" },
        };
        AutomationProperties.SetName(evidenceLimit, "Limits of the pronunciation assessment");
        var comparisonText = new TextBlock
        {
            IsVisible = false,
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(comparisonText, $"{prefix}Comparison");
        AutomationProperties.SetLiveSetting(comparisonText, AutomationLiveSetting.Polite);

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
            "Speaking comparison with a complete typed route and optional local recognition");

        compareButton.Click += (_, _) =>
        {
            var outcome = TemplateInteractionEvaluator.EvaluateDictation(
                acceptedTranscripts,
                responseBox.Text);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, outcomeCopy);
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
                    var assessments = acceptedTranscripts
                        .Select(option => new
                        {
                            Option = option,
                            Assessment = pronunciationAssessmentProvider.Assess(
                                new PronunciationAssessmentRequest(
                                    option.Label,
                                    result.Transcript,
                                    result.Duration),
                                result.ProviderVersion),
                        })
                        .ToArray();
                    var outcome = TemplateInteractionEvaluator.EvaluateBestPronunciationAssessment(
                        assessments
                            .Select(candidate => new KeyValuePair<string, PronunciationAssessmentOutcome>(
                                candidate.Option.Id,
                                candidate.Assessment.Evidence.Outcome))
                            .ToArray());
                    var best = assessments.Single(candidate =>
                        string.Equals(candidate.Option.Id, outcome.ResponseId, StringComparison.Ordinal));
                    TemplateRendering.ApplyOutcome(
                        outcomePanel,
                        outcomeText,
                        outcome.State,
                        outcomeCopy);
                    comparisonText.Text =
                        $"Expected: {best.Option.Label}{Environment.NewLine}Recognized: {result.Transcript}";
                    comparisonText.IsVisible = true;
                    recognitionStatus.Text = best.Assessment.Message;
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
                        outcomeCopy);
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

        practiceCard.AttachedToVisualTree += async (_, _) =>
        {
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
        practiceCard.DetachedFromVisualTree += (_, _) =>
        {
            availabilityCancellation.Cancel();
            availabilityCancellation.Dispose();
            recognitionCancellation?.Cancel();
            recognitionCancellation?.Dispose();
            recognitionCancellation = null;
            activeRequestId = null;
        };
        return practiceCard;
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

internal static class ReadAloudCardRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ISpeechRecognitionProvider? speechRecognitionProvider,
        IPronunciationAssessmentProvider? pronunciationAssessmentProvider,
        bool microphoneAllowed,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var cardText = TemplateRendering.Text(parameters, "card-text");
        var speechLanguage = TemplateRendering.Text(parameters, "speech-language");
        var acceptedTranscripts = TemplateRendering.Options(parameters, "accepted-transcripts");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var header = SpeakingTemplatePresentation.CreateHeader(
            "ReadAloudCard",
            instruction,
            "Replay card",
            "Skip card",
            out var replayButton,
            out var skipButton);
        var stage = TemplateRendering.CreateStage(300, "Read-aloud paper card stage");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var tape = new PaperTape { Content = "READ ALOUD", Angle = -1.4 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);

        var cardCopy = new StackPanel
        {
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        cardCopy.Children.Add(new TextBlock
        {
            Text = "VOICE CARD",
            Classes = { "eyebrow" },
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        cardCopy.Children.Add(new TextBlock
        {
            Text = cardText,
            FontSize = 27,
            FontWeight = FontWeight.Bold,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        var readingCard = new CutoutFrame
        {
            Width = 520,
            MinHeight = 148,
            Padding = new Thickness(28, 22),
            Content = cardCopy,
        };
        readingCard.Classes.Add("tilt-right");
        AutomationProperties.SetAutomationId(readingCard, "ReadAloudCardText");
        AutomationProperties.SetName(readingCard, $"Read aloud card. {cardText}");
        PaperStage.SetLayer(readingCard, PaperStageLayer.Subject);
        PaperStage.SetAnchor(readingCard, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(readingCard, 0.5);
        PaperStage.SetAnchorOffsetY(readingCard, -12);
        stage.Children.Add(readingCard);

        var limitStamp = new PaperStamp { Content = "INTELLIGIBILITY ONLY", Angle = -2 };
        limitStamp.Classes.Add("rectangle");
        AutomationProperties.SetName(limitStamp, "Intelligibility only assessment stamp");
        PaperStage.SetLayer(limitStamp, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(limitStamp, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(limitStamp, 0.62);
        PaperStage.SetAnchorOffsetY(limitStamp, -18);
        stage.Children.Add(limitStamp);

        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var practiceCard = SpeakingComparisonCard.Create(
            "ReadAloudCard",
            acceptedTranscripts,
            speechLanguage,
            speechRecognitionProvider,
            pronunciationAssessmentProvider,
            microphoneAllowed,
            parameters.UseTextOnlyFallback,
            parameters.PreviewOutcome,
            outcomePanel,
            outcomeText,
            OutcomeCopy,
            reportOutcome);

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        root.Children.Add(new TextBlock
        {
            Text = "Read the complete card. Typed comparison checks wording only.",
            TextWrapping = TextWrapping.Wrap,
            Classes = { "muted" },
        });
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = "Text-only read-aloud practice: read silently or aloud, then compare typed wording.",
                TextWrapping = TextWrapping.Wrap,
                Classes = { "muted" },
            });
        }

        root.Children.Add(stage);
        if (!parameters.UseTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                backdropRendered ? [backdropReference] : [],
                "ReadAloudCardImageCredits") is { } credits)
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
            TemplateRendering.Prepare(shouldReduceMotion, tape, readingCard, limitStamp, practiceCard);
            if (!shouldReduceMotion)
            {
                readingCard.RenderTransform = TemplateRendering.Transform(0, 12, 1.2, 0.98);
                limitStamp.RenderTransform = TemplateRendering.Transform(-8, 5, -2, 0.96);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(620), readingCard, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(340), limitStamp, 0, 0, 0, 1),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(260), practiceCard),
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
        TemplateOutcomeState.Success => "The wording matched, or local recognition found the card intelligible.",
        TemplateOutcomeState.Uncertain => "No complete comparison yet, or local recognition found partial intelligibility.",
        TemplateOutcomeState.Failure => "The wording differed, or local recognition found substantial intelligibility loss.",
        _ => "Ready: read the card aloud or silently, then choose a local comparison path.",
    };
}

internal static class PromptRespondRenderer
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
        var speaker = TemplateRendering.Text(parameters, "speaker");
        var prompt = TemplateRendering.Text(parameters, "prompt");
        var speechLanguage = TemplateRendering.Text(parameters, "speech-language");
        var acceptedResponses = TemplateRendering.Options(parameters, "accepted-responses");
        var speakerAssetReference = TemplateRendering.AssetReference(parameters, "speaker-asset");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var header = SpeakingTemplatePresentation.CreateHeader(
            "PromptRespond",
            instruction,
            "Replay prompt",
            "Skip prompt",
            out var replayButton,
            out var skipButton);
        var playbackPanel = ListeningTemplatePresentation.CreatePlaybackPanel(
            "PromptRespond",
            speechSynthesisProvider,
            speechLanguage,
            [
                new ListeningPrompt(
                    "Prompt",
                    $"Play {speaker}",
                    prompt,
                    $"prompt-respond:{speaker}:{acceptedResponses[0].Id}"),
            ],
            parameters.UseTextOnlyFallback);
        var stage = TemplateRendering.CreateStage(326, "Prompt and response puppet stage");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var tape = new PaperTape { Content = "ANSWER THE PUPPET", Angle = 1.1 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);

        var speakerImage = parameters.UseTextOnlyFallback
            ? null
            : TemplateRendering.CreateContentImage(
                imageCache,
                speakerAssetReference,
                height: 158,
                Stretch.Uniform);
        Control speakerCutout;
        if (speakerImage is not null)
        {
            var speakerCopy = new StackPanel
            {
                Spacing = 5,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            speakerCopy.Children.Add(speakerImage);
            speakerCopy.Children.Add(new TextBlock
            {
                Text = speaker,
                FontWeight = FontWeight.Bold,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            speakerCutout = new CutoutFrame
            {
                Width = 184,
                Height = 216,
                Content = speakerCopy,
            };
        }
        else
        {
            speakerCutout = new CutoutFrame
            {
                Width = 184,
                Height = 132,
                Content = new TextBlock
                {
                    Text = speaker,
                    FontSize = 24,
                    FontWeight = FontWeight.Bold,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                },
            };
        }

        speakerCutout.Classes.Add("tilt-left");
        AutomationProperties.SetAutomationId(speakerCutout, "PromptRespondSpeaker");
        AutomationProperties.SetName(speakerCutout, $"Prompt speaker {speaker}");
        PaperStage.SetLayer(speakerCutout, PaperStageLayer.SupportingCast);
        PaperStage.SetAnchor(speakerCutout, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(speakerCutout, 0.25);
        PaperStage.SetAnchorOffsetY(speakerCutout, -12);
        stage.Children.Add(speakerCutout);

        var promptCopy = new StackPanel { Spacing = 7 };
        promptCopy.Children.Add(new TextBlock
        {
            Text = speaker.ToUpperInvariant(),
            Classes = { "eyebrow" },
        });
        promptCopy.Children.Add(new TextBlock
        {
            Text = prompt,
            FontSize = 23,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
        var promptBubble = new PaperCard
        {
            Width = 460,
            Padding = new Thickness(24, 18),
            Content = promptCopy,
        };
        promptBubble.Classes.Add("soft");
        AutomationProperties.SetAutomationId(promptBubble, "PromptRespondPrompt");
        AutomationProperties.SetName(promptBubble, $"{speaker} asks. {prompt}");
        PaperStage.SetLayer(promptBubble, PaperStageLayer.Subject);
        PaperStage.SetAnchor(promptBubble, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(promptBubble, 0.65);
        PaperStage.SetAnchorOffsetY(promptBubble, -34);
        stage.Children.Add(promptBubble);

        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var practiceCard = SpeakingComparisonCard.Create(
            "PromptRespond",
            acceptedResponses,
            speechLanguage,
            speechRecognitionProvider,
            pronunciationAssessmentProvider,
            microphoneAllowed,
            parameters.UseTextOnlyFallback,
            parameters.PreviewOutcome,
            outcomePanel,
            outcomeText,
            OutcomeCopy,
            reportOutcome);

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        root.Children.Add(playbackPanel);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = $"Text-only prompt: {speaker} asks, {prompt}",
                TextWrapping = TextWrapping.Wrap,
                Classes = { "muted" },
            });
        }

        root.Children.Add(stage);
        if (!parameters.UseTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                new[]
                {
                    speakerImage is not null ? speakerAssetReference : null,
                    backdropRendered ? backdropReference : null,
                },
                "PromptRespondImageCredits") is { } credits)
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
            TemplateRendering.Prepare(
                shouldReduceMotion,
                tape,
                speakerCutout,
                promptBubble,
                practiceCard);
            if (!shouldReduceMotion)
            {
                speakerCutout.RenderTransform = TemplateRendering.Transform(-18, 4, -1.2, 0.98);
                promptBubble.RenderTransform = TemplateRendering.Transform(18, 8, 0.8, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(560), speakerCutout, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(520), promptBubble, 0, 0, 0, 1),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(260), practiceCard),
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
        TemplateOutcomeState.Success => "The response matches an authored answer, or local recognition found one intelligible.",
        TemplateOutcomeState.Uncertain => "No complete response yet, or local recognition found partial evidence.",
        TemplateOutcomeState.Failure => "The response differs from the authored answers, or intelligibility was substantially reduced.",
        _ => "Ready: hear or read the puppet prompt, then answer by voice or text.",
    };
}

internal static class SyllableClapRenderer
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
        var phrase = TemplateRendering.Text(parameters, "phrase");
        var speechLanguage = TemplateRendering.Text(parameters, "speech-language");
        var beats = TemplateRendering.Options(parameters, "beats");
        var stressBeat = TemplateRendering.Text(parameters, "stress-beat");
        if (!beats.Any(beat => string.Equals(beat.Id, stressBeat, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("The stressed beat must name an authored syllable.");
        }

        var minimumInterval = ParseInterval(parameters, "minimum-interval-ms");
        var maximumInterval = ParseInterval(parameters, "maximum-interval-ms");
        if (maximumInterval < minimumInterval || maximumInterval > TimeSpan.FromSeconds(3))
        {
            throw new InvalidOperationException("The authored clap interval range is invalid.");
        }

        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var header = SpeakingTemplatePresentation.CreateHeader(
            "SyllableClap",
            instruction,
            "Replay rhythm",
            "Skip rhythm",
            out var replayButton,
            out var skipButton);
        var playbackPanel = ListeningTemplatePresentation.CreatePlaybackPanel(
            "SyllableClap",
            speechSynthesisProvider,
            speechLanguage,
            [new ListeningPrompt("Phrase", "Play phrase", phrase, $"syllable-clap:{stressBeat}")],
            parameters.UseTextOnlyFallback);
        var stage = TemplateRendering.CreateStage(308, "Syllable stress rhythm stage");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var tape = new PaperTape { Content = "CLAP THE RHYTHM", Angle = -1 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);

        var beatPanel = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ItemWidth = 170,
            ItemHeight = 126,
            Margin = new Thickness(30, 76, 30, 24),
        };
        AutomationProperties.SetName(beatPanel, $"Written stress pattern for {phrase}");
        foreach (var (beat, index) in beats.Select((beat, index) => (beat, index)))
        {
            var isStressed = string.Equals(beat.Id, stressBeat, StringComparison.Ordinal);
            var beatCopy = new StackPanel
            {
                Spacing = 6,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            beatCopy.Children.Add(new TextBlock
            {
                Text = (index + 1).ToString(CultureInfo.InvariantCulture),
                Classes = { "eyebrow" },
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            beatCopy.Children.Add(new TextBlock
            {
                Text = beat.Label,
                FontSize = isStressed ? 30 : 23,
                FontWeight = isStressed ? FontWeight.Bold : FontWeight.SemiBold,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            beatCopy.Children.Add(new TextBlock
            {
                Text = isStressed ? "STRONG" : "LIGHT",
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            var beatCard = new PaperCard
            {
                Width = 156,
                Height = 112,
                Padding = new Thickness(14),
                Content = beatCopy,
            };
            beatCard.Classes.Add(isStressed ? "accent-card" : "soft");
            AutomationProperties.SetName(
                beatCard,
                $"Beat {index + 1}, {beat.Label}, {(isStressed ? "strong" : "light")}");
            beatPanel.Children.Add(beatCard);
        }

        PaperStage.SetLayer(beatPanel, PaperStageLayer.Subject);
        stage.Children.Add(beatPanel);
        var noMicrophoneStamp = new PaperStamp { Content = "NO MICROPHONE", Angle = 1.8 };
        noMicrophoneStamp.Classes.Add("rectangle");
        AutomationProperties.SetName(noMicrophoneStamp, "This rhythm activity uses no microphone");
        PaperStage.SetLayer(noMicrophoneStamp, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(noMicrophoneStamp, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(noMicrophoneStamp, 0.7);
        PaperStage.SetAnchorOffsetY(noMicrophoneStamp, -16);
        stage.Children.Add(noMicrophoneStamp);

        var tapOffsets = new List<TimeSpan>();
        long? firstTapTimestamp = null;
        var tapStatus = new TextBlock
        {
            Text = PreviewTapStatus(parameters.PreviewOutcome, beats.Count),
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(tapStatus, "SyllableClapTapStatus");
        AutomationProperties.SetLiveSetting(tapStatus, AutomationLiveSetting.Polite);
        var tapButton = new Button { Content = "Tap beat 1", Classes = { "primary" } };
        AutomationProperties.SetAutomationId(tapButton, "SyllableClapTap");
        AutomationProperties.SetName(tapButton, "Tap the next syllable beat");
        var checkButton = new Button { Content = "Check rhythm", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(checkButton, "SyllableClapCheck");
        AutomationProperties.SetName(checkButton, "Check the complete tap rhythm");
        var resetButton = new Button { Content = "Reset taps", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(resetButton, "SyllableClapReset");
        AutomationProperties.SetName(resetButton, "Clear every recorded tap");
        var tapActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        tapActions.Children.Add(tapButton);
        tapActions.Children.Add(checkButton);
        tapActions.Children.Add(resetButton);
        var tapCopy = new StackPanel { Spacing = 9 };
        tapCopy.Children.Add(new TextBlock
        {
            Text = "Tap once per written syllable. Keyboard activation and pointer clicks use the same timing path.",
            TextWrapping = TextWrapping.Wrap,
        });
        tapCopy.Children.Add(tapActions);
        tapCopy.Children.Add(tapStatus);
        var tapCard = new PaperCard
        {
            Padding = new Thickness(16, 14),
            Content = tapCopy,
        };
        tapCard.Classes.Add("soft");
        AutomationProperties.SetName(tapCard, "Keyboard and pointer syllable rhythm controls");
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);

        tapButton.Click += (_, _) =>
        {
            if (tapOffsets.Count >= beats.Count)
            {
                return;
            }

            if (firstTapTimestamp is null)
            {
                firstTapTimestamp = Stopwatch.GetTimestamp();
                tapOffsets.Add(TimeSpan.Zero);
            }
            else
            {
                tapOffsets.Add(Stopwatch.GetElapsedTime(firstTapTimestamp.Value));
            }

            tapStatus.Text = $"Recorded {tapOffsets.Count} of {beats.Count} beats.";
            tapButton.Content = tapOffsets.Count < beats.Count
                ? $"Tap beat {tapOffsets.Count + 1}"
                : "All beats tapped";
            tapButton.IsEnabled = tapOffsets.Count < beats.Count;
        };
        checkButton.Click += (_, _) =>
        {
            var outcome = TemplateInteractionEvaluator.EvaluateTapRhythm(
                beats.Count,
                minimumInterval,
                maximumInterval,
                tapOffsets);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            reportOutcome(outcome);
        };
        resetButton.Click += (_, _) =>
        {
            tapOffsets.Clear();
            firstTapTimestamp = null;
            tapButton.Content = "Tap beat 1";
            tapButton.IsEnabled = true;
            tapStatus.Text = $"Ready for {beats.Count} taps.";
        };

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        root.Children.Add(playbackPanel);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = $"Text-only rhythm: {string.Join(" | ", beats.Select(beat => beat.Label))}.",
                TextWrapping = TextWrapping.Wrap,
                Classes = { "muted" },
            });
            root.Children.Add(new TextBlock
            {
                Text = $"Tap gaps may be {minimumInterval.TotalMilliseconds:0} to {maximumInterval.TotalMilliseconds:0} milliseconds.",
                TextWrapping = TextWrapping.Wrap,
                Classes = { "muted" },
            });
        }

        root.Children.Add(stage);
        if (!parameters.UseTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                backdropRendered ? [backdropReference] : [],
                "SyllableClapImageCredits") is { } credits)
        {
            root.Children.Add(credits);
        }

        root.Children.Add(tapCard);
        root.Children.Add(outcomePanel);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(
                shouldReduceMotion,
                tape,
                beatPanel,
                noMicrophoneStamp,
                tapCard);
            if (!shouldReduceMotion)
            {
                beatPanel.RenderTransform = TemplateRendering.Transform(0, 10, -0.6, 0.98);
                noMicrophoneStamp.RenderTransform = TemplateRendering.Transform(6, 5, 1.8, 0.97);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(620), beatPanel, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(320), noMicrophoneStamp, 0, 0, 0, 1),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(260), tapCard),
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

    private static TimeSpan ParseInterval(
        ResolvedTemplateParameters parameters,
        string parameterName)
    {
        var value = TemplateRendering.Text(parameters, parameterName);
        if (!int.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var milliseconds) ||
            milliseconds < 1)
        {
            throw new InvalidOperationException(
                $"Template parameter '{parameterName}' must be positive milliseconds.");
        }

        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private static string PreviewTapStatus(TemplateOutcomeState state, int beatCount) => state switch
    {
        TemplateOutcomeState.Success => $"Synthetic preview: {beatCount} well-spaced taps.",
        TemplateOutcomeState.Uncertain => $"Synthetic preview: fewer than {beatCount} taps.",
        TemplateOutcomeState.Failure => $"Synthetic preview: {beatCount} taps outside the authored window.",
        _ => $"Ready for {beatCount} taps.",
    };

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The tap count and intervals fit the authored rhythm window.",
        TemplateOutcomeState.Uncertain => "The rhythm is incomplete. Tap once for every written syllable.",
        TemplateOutcomeState.Failure => "The tap count or intervals fall outside the authored rhythm window.",
        _ => "Ready: play or read the phrase, then tap each syllable in rhythm.",
    };
}

internal static class LongShortVowelRenderer
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
        var contrastLabel = TemplateRendering.Text(parameters, "contrast-label");
        var options = TemplateRendering.Options(parameters, "options");
        var longOptionId = TemplateRendering.Text(parameters, "long-option");
        var answerId = TemplateRendering.Text(parameters, "answer");
        var optionIds = options.Select(option => option.Id).ToArray();
        if (options.Count != 2 ||
            !optionIds.Contains(longOptionId, StringComparer.Ordinal) ||
            !optionIds.Contains(answerId, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                "The vowel contrast requires two choices, a long choice, and an authored answer.");
        }

        var selectedId = parameters.PreviewOutcome switch
        {
            TemplateOutcomeState.Success => answerId,
            TemplateOutcomeState.Failure => options.First(option => option.Id != answerId).Id,
            _ => null,
        };
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var header = SpeakingTemplatePresentation.CreateHeader(
            "LongShortVowel",
            instruction,
            "Replay contrast",
            "Skip contrast",
            out var replayButton,
            out var skipButton);
        var playbackPanel = ListeningTemplatePresentation.CreatePlaybackPanel(
            "LongShortVowel",
            speechSynthesisProvider,
            speechLanguage,
            [new ListeningPrompt("Target", "Play target", utterance, $"long-short-vowel:{answerId}")],
            parameters.UseTextOnlyFallback);
        var stage = TemplateRendering.CreateStage(328, "Long and short vowel paper stretch stage");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var tape = new PaperTape { Content = "HEAR THE LENGTH", Angle = 1.2 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.5);
        PaperStage.SetAnchorOffsetY(tape, -10);
        stage.Children.Add(tape);

        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var bandPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 18,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(24, 78, 24, 30),
        };
        AutomationProperties.SetName(bandPanel, $"Vowel contrast choices. {contrastLabel}");
        var buttons = new Dictionary<string, Button>(StringComparer.Ordinal);
        foreach (var (option, index) in options.Select((option, index) => (option, index)))
        {
            var isLong = string.Equals(option.Id, longOptionId, StringComparison.Ordinal);
            var width = isLong ? 292 : 190;
            var bandCopy = new StackPanel
            {
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            bandCopy.Children.Add(new TextBlock
            {
                Text = isLong ? "LONG VOWEL" : "SHORT VOWEL",
                Classes = { "eyebrow" },
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            bandCopy.Children.Add(new TextBlock
            {
                Text = option.Label,
                FontSize = 25,
                FontWeight = FontWeight.Bold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            var band = new PaperCard
            {
                Width = width,
                Height = 122,
                Padding = new Thickness(16),
                Content = bandCopy,
            };
            band.Classes.Add("soft");
            var button = new Button
            {
                Width = width + 12,
                Height = 136,
                Padding = new Thickness(5),
                Content = band,
                Classes = { "quiet", "lift" },
            };
            AutomationProperties.SetAutomationId(button, $"LongShortVowelOption_{option.Id}");
            AutomationProperties.SetName(
                button,
                $"Choose {(isLong ? "long" : "short")} vowel, {option.Label}");
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
            bandPanel.Children.Add(button);
            band.Classes.Add(index == 0 ? "tilt-left" : "tilt-right");
        }

        PaperStage.SetLayer(bandPanel, PaperStageLayer.Subject);
        stage.Children.Add(bandPanel);
        var honestyStamp = new PaperStamp { Content = "NO PHONEME SCORE", Angle = -1.7 };
        honestyStamp.Classes.Add("rectangle");
        AutomationProperties.SetName(
            honestyStamp,
            "Production practice has no phoneme or accent score");
        PaperStage.SetLayer(honestyStamp, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(honestyStamp, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(honestyStamp, 0.68);
        PaperStage.SetAnchorOffsetY(honestyStamp, -15);
        stage.Children.Add(honestyStamp);

        var practiceStatus = new TextBlock
        {
            Text = "Production practice is optional and unscored. The choice path reports the outcome.",
            TextWrapping = TextWrapping.Wrap,
            Classes = { "muted" },
        };
        AutomationProperties.SetAutomationId(practiceStatus, "LongShortVowelPracticeStatus");
        AutomationProperties.SetLiveSetting(practiceStatus, AutomationLiveSetting.Polite);
        var practiceButton = new Button
        {
            Content = "Practice the target aloud",
            Classes = { "quiet" },
        };
        AutomationProperties.SetAutomationId(practiceButton, "LongShortVowelPractice");
        AutomationProperties.SetName(
            practiceButton,
            "Mark unscored production practice. No microphone is used");
        practiceButton.Click += (_, _) =>
        {
            practiceStatus.Text =
                "Practice noted for this screen. Vowel length, phonemes, and accent were not scored.";
        };
        var practiceActions = new StackPanel { Spacing = 8 };
        practiceActions.Children.Add(practiceButton);
        practiceActions.Children.Add(practiceStatus);
        var practiceCard = new PaperCard
        {
            Padding = new Thickness(16, 14),
            Content = practiceActions,
        };
        practiceCard.Classes.Add("soft");
        AutomationProperties.SetName(
            practiceCard,
            "Optional unscored vowel production practice without microphone access");
        RefreshSelection();

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        root.Children.Add(playbackPanel);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = $"Text-only vowel contrast: {contrastLabel}.",
                TextWrapping = TextWrapping.Wrap,
                Classes = { "muted" },
            });
            root.Children.Add(new TextBlock
            {
                Text = $"Written target: {utterance}. Choices: {string.Join(", ", options.Select(option => option.Label))}.",
                TextWrapping = TextWrapping.Wrap,
                Classes = { "muted" },
            });
        }

        root.Children.Add(stage);
        if (!parameters.UseTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                backdropRendered ? [backdropReference] : [],
                "LongShortVowelImageCredits") is { } credits)
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
            TemplateRendering.Prepare(
                shouldReduceMotion,
                tape,
                bandPanel,
                honestyStamp,
                practiceCard);
            if (!shouldReduceMotion)
            {
                bandPanel.RenderTransform = TemplateRendering.Transform(-12, 8, -0.7, 0.96);
                honestyStamp.RenderTransform = TemplateRendering.Transform(7, 5, -1.7, 0.97);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(720), bandPanel, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(320), honestyStamp, 0, 0, 0, 1),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(260), practiceCard),
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
        TemplateOutcomeState.Success => "The selected length matches the authored listening target.",
        TemplateOutcomeState.Uncertain => "Choose the long or short written option for a scored outcome.",
        TemplateOutcomeState.Failure => "The selected length does not match the authored listening target.",
        _ => "Ready: play or read the target, then choose its vowel length or practice unscored production.",
    };
}
