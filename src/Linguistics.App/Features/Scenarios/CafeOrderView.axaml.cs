using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Linguistics.App.Content;
using Linguistics.App.Controls;
using Linguistics.App.Features.Learn.Templates;
using Linguistics.App.Localization;
using Linguistics.Core.Content;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Providers;
using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.App.Features.Scenarios;

public partial class CafeOrderView : UserControl
{
    private CafeScenarioController? _controller;
    private readonly ISpeechSynthesisProvider? _speechSynthesisProvider;
    private readonly ISpeechRecognitionProvider? _speechRecognitionProvider;
    private readonly bool _microphoneAllowed;
    private readonly string? _runtimeContentError;
    private readonly ContentImageCache? _imageCache;
    private readonly bool _shouldReduceMotion;
    private readonly LanguageCode _instructionLanguage = new("en");
    private CancellationTokenSource? _turnCancellation;
    private CancellationTokenSource? _recognitionCancellation;
    private CancellationTokenSource? _playbackCancellation;
    private SpeechRecognitionResult? _pendingSpeechResult;
    private Guid? _activeSpeechRequestId;
    private bool _settingTranscript;
    private bool _recognitionAvailable;
    private bool _initialized;
    private bool _busy;
    private string? _lastNpcResponse;
    private TransferNoteCardView? _bridgeNote;

    public CafeOrderView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public CafeOrderView(
        LearnerProfile profile,
        LearnerProfileOwner profileOwner,
        ValidatedContentCatalog? runtimeCatalog,
        string? runtimeContentError,
        ILanguageModelProvider? languageModelProvider = null,
        ISpeechSynthesisProvider? speechSynthesisProvider = null,
        ISpeechRecognitionProvider? speechRecognitionProvider = null,
        ContentImageCache? imageCache = null)
        : this()
    {
        _runtimeContentError = runtimeContentError;
        _speechSynthesisProvider = speechSynthesisProvider;
        _speechRecognitionProvider = speechRecognitionProvider;
        _imageCache = imageCache;
        _microphoneAllowed = profile.Settings.Microphone != MicrophonePreference.Never;
        _shouldReduceMotion = MotionPreferences.ShouldReduce(profile.Settings.ReduceMotion);
        if (runtimeCatalog is not null)
        {
            _instructionLanguage = runtimeCatalog
                .SelectInstructionLanguage(profile)
                .SelectedLanguage ?? AppStrings.CurrentLanguage;
            _controller = CafeScenarioController.Create(
                profile,
                profileOwner,
                runtimeCatalog,
                languageModelProvider);
        }
    }

    private async void OnLoaded(object? sender, RoutedEventArgs args)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        if (_controller is null)
        {
            ShowContentGate();
            return;
        }

        try
        {
            var state = await _controller.InitializeAsync();
            ShowReadyState(state);
            await RefreshSpeechCapabilitiesAsync();
        }
        catch (Exception exception) when (
            exception is LearnerStoreException or
            LearnerProfileValidationException or
            CurriculumValidationException or
            ContentValidationException or
            InvalidOperationException)
        {
            LoadingPanel.IsVisible = false;
            ContentGatePanel.IsVisible = true;
            ContentGateTechnicalText.IsVisible = true;
            ContentGateTechnicalText.Text = AppStrings.Format(
                "Scenario_InitializeFailed",
                exception.Message);
        }
    }

    private void ShowContentGate()
    {
        LoadingPanel.IsVisible = false;
        ContentGatePanel.IsVisible = true;
        ContentGateTechnicalText.IsVisible = DeveloperModeEnabled();
        ContentGateTechnicalText.Text = string.IsNullOrWhiteSpace(_runtimeContentError)
            ? AppStrings.Get("Scenario_NoRuntimeCatalog")
            : _runtimeContentError;
    }

    private void ShowReadyState(CafeScenarioInitialization state)
    {
        LoadingPanel.IsVisible = false;
        ContentGatePanel.IsVisible = false;
        ScenarioPanel.IsVisible = true;

        var definition = _controller!.Definition;
        GoalText.Text = definition.Goal;
        ContextText.Text = definition.Context;
        SuccessCriteriaList.ItemsSource = definition.SuccessCriteria
            .Select(criterion => $"• {criterion}")
            .ToArray();
        ReadyStatusText.Text = state.CanStart
            ? AppStrings.Get("Scenario_Ready")
            : AppStrings.Get("Scenario_PrerequisitesLocked");
        PreviousAttemptsText.Text = state.PreviousCompletions == 0
            ? AppStrings.Get("Scenario_NoPreviousCompletion")
            : AppStrings.Format(
                "Scenario_PreviousCompletions",
                state.PreviousCompletions);
        MissingPrerequisitesText.IsVisible = state.MissingPrerequisiteTitles.Count > 0;
        MissingPrerequisitesText.Text = state.MissingPrerequisiteTitles.Count == 0
            ? string.Empty
            : AppStrings.Format(
                "Scenario_MissingPrerequisites",
                string.Join(", ", state.MissingPrerequisiteTitles));
        StartButton.IsEnabled = state.CanStart;

        if (state.Bridge is { } bridge)
        {
            _bridgeNote = new TransferNoteCardView(
                new TransferNoteCardContent(
                    LanguageName(bridge.SourceLanguage),
                    RelationName(bridge.Relation),
                    bridge.Explanation,
                    bridge.Risks,
                    bridge.RequiresConfirmation,
                    AppStrings.Get("Scenario_DismissBridge")),
                "Cafe");
            _bridgeNote.Dismissed += (_, _) =>
            {
                BridgeHost.IsVisible = false;
                _controller?.DismissBridge();
            };
            BridgeHost.Content = _bridgeNote;
            BridgeHost.IsVisible = true;
        }
        else
        {
            _bridgeNote = null;
            BridgeHost.Content = null;
            BridgeHost.IsVisible = false;
        }

        DeveloperPanel.IsVisible = DeveloperModeEnabled();
        DeveloperTraceText.Text =
            $"task={definition.TaskId}\ncontent={definition.ContentVersion}\n" +
            $"evaluator={definition.EvaluationVersion}\nstate={state.TargetProgressState}\n" +
            "authority=deterministic C# engine";
    }

    private void OnStartClicked(object? sender, RoutedEventArgs args) => StartScenario();

    private void StartScenario()
    {
        if (_controller is null || _busy)
        {
            return;
        }

        try
        {
            ResetTaskSurface();
            var opening = _controller.Start(_bridgeNote?.IsConfirmed == true);
            ScenarioOverviewPanel.IsVisible = false;
            ReadyPanel.IsVisible = false;
            ActiveScenarioPanel.IsVisible = true;
            AddConversationMessage(AppStrings.Get("Scenario_Server"), opening, isLearner: false);
            RenderScenarioTheatre(opening);
            TurnStatusText.Text = AppStrings.Get("Scenario_DialogueReady");
            LearnerInput.Focus();
        }
        catch (InvalidOperationException exception)
        {
            ReadyStatusText.Text = exception.Message;
        }
    }

    private async void OnSendClicked(object? sender, RoutedEventArgs args) =>
        await SubmitTurnAsync();

    private async void OnLearnerInputKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key == Key.Enter && args.KeyModifiers == KeyModifiers.None)
        {
            args.Handled = true;
            await SubmitTurnAsync();
        }
    }

    private async Task SubmitTurnAsync()
    {
        if (_controller is null || _busy)
        {
            return;
        }

        var learnerText = LearnerInput.Text?.Trim();
        if (string.IsNullOrWhiteSpace(learnerText))
        {
            TurnStatusText.Text = AppStrings.Get("Scenario_ReplyRequired");
            LearnerInput.Focus();
            return;
        }

        var speechResult = _pendingSpeechResult is { } pending &&
                           string.Equals(pending.Transcript?.Trim(), learnerText, StringComparison.Ordinal)
            ? pending
            : null;
        AddConversationMessage(AppStrings.Get("Scenario_You"), learnerText, isLearner: true);
        _settingTranscript = true;
        LearnerInput.Text = string.Empty;
        _settingTranscript = false;
        FeedbackPanel.IsVisible = false;
        SupportText.IsVisible = false;
        SetBusy(true);
        _turnCancellation?.Cancel();
        _turnCancellation?.Dispose();
        _turnCancellation = new CancellationTokenSource();
        try
        {
            var outcome = speechResult is null
                ? await _controller.SubmitAsync(learnerText, _turnCancellation.Token)
                : await _controller.SubmitSpeechAsync(speechResult, _turnCancellation.Token);
            _pendingSpeechResult = null;
            _activeSpeechRequestId = null;
            ShowTurnOutcome(outcome);
        }
        catch (OperationCanceledException)
        {
            TurnStatusText.Text = AppStrings.Get("Scenario_ResponseCancelled");
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
            ArgumentException or
            CurriculumValidationException)
        {
            TurnStatusText.Text = exception.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ShowTurnOutcome(CafeScenarioTurnOutcome outcome)
    {
        if (outcome.NpcResponse is { } npcResponse)
        {
            AddConversationMessage(AppStrings.Get("Scenario_Server"), npcResponse, isLearner: false);
            RenderScenarioTheatre(npcResponse);
        }

        if (outcome.Evaluation.PrimaryIntervention is { } primary)
        {
            FeedbackPanel.IsVisible = true;
            FeedbackMessageText.Text = primary.Message;
            RetryPromptText.Text = primary.RetryPrompt;
        }

        OtherObservationsExpander.IsVisible = outcome.Evaluation.OtherObservations.Count > 0;
        OtherObservationsText.Text = string.Join(
            Environment.NewLine,
            outcome.Evaluation.OtherObservations.Select(observation => $"• {observation.Message}"));
        TurnStatusText.Text = outcome.DialogueMode == DialogueRealizationMode.LocalModel
            ? AppStrings.Get("Scenario_LocalModelResponse")
            : AppStrings.Get("Scenario_ScriptedResponse");
        if (outcome.PronunciationAssessment is { } pronunciation)
        {
            TurnStatusText.Text += " " + AppStrings.Format(
                "Scenario_WordEvidence",
                pronunciation.Evidence.MatchedWordCount,
                pronunciation.Evidence.ExpectedWordCount);
        }

        var diagnostic = outcome.ModelDiagnostic;
        DeveloperTraceText.Text =
            $"task={_controller!.Definition.TaskId}\n" +
            $"state={outcome.Evaluation.PreviousStateId} -> {outcome.Evaluation.Session.StateId}\n" +
            $"intent={outcome.Evaluation.Intent}\naccepted={outcome.Evaluation.StateChanged}\n" +
            $"evaluation={outcome.Evaluation.Explanation}\n" +
            $"dialogue={outcome.DialogueMode}\n" +
            (diagnostic is null
                ? "provider=not called"
                : $"request={diagnostic.RequestId}\nschema={diagnostic.SchemaVersion}\nvalidation={diagnostic.ValidationResult}\nduration={diagnostic.Duration.TotalMilliseconds:0}ms");

        if (outcome.Evaluation.Completed)
        {
            ShowCompletion(outcome);
        }
        else
        {
            LearnerInput.Focus();
        }
    }

    private void ShowCompletion(CafeScenarioTurnOutcome outcome)
    {
        InputPanel.IsVisible = false;
        ScenarioTheatreHost.IsVisible = false;
        CompletionPanel.IsVisible = true;
        var evidence = outcome.Evaluation.Evidence!;
        var pronunciationText = outcome.PronunciationAssessment is { } pronunciation
            ? AppStrings.Format(
                "Scenario_Evidence_Pronunciation",
                pronunciation.Evidence.MatchedWordCount,
                pronunciation.Evidence.ExpectedWordCount)
            : AppStrings.Get("Scenario_Evidence_PronunciationNotMeasured");
        ConsequenceVerdictHost.Content = ConsequenceVerdictRenderer.Render(
            _imageCache,
            CreateConsequenceParameters(evidence, pronunciationText),
            _instructionLanguage,
            _shouldReduceMotion,
            OnConsequenceAction);
        PersistenceStatusText.Text = outcome.Persisted
            ? AppStrings.Get("Scenario_PersistenceSaved")
            : outcome.PersistenceError;
        RetrySaveButton.IsVisible = !outcome.Persisted;
    }

    private async void OnRetrySaveClicked(object? sender, RoutedEventArgs args)
    {
        if (_controller is null || _busy)
        {
            return;
        }

        SetBusy(true);
        try
        {
            var result = await _controller.RetryPersistenceAsync();
            PersistenceStatusText.Text = result.Persisted
                ? AppStrings.Get("Scenario_PersistenceSaved")
                : result.Message;
            RetrySaveButton.IsVisible = !result.Persisted;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OnCancelResponseClicked(object? sender, RoutedEventArgs args)
    {
        _turnCancellation?.Cancel();
        TurnStatusText.Text = AppStrings.Get("Scenario_CancellingResponse");
    }

    private void OnExitClicked(object? sender, RoutedEventArgs args)
    {
        if (_busy || _controller is null)
        {
            return;
        }

        CancelActiveSpeechRequest();
        _controller.Exit();
        ActiveScenarioPanel.IsVisible = false;
        ScenarioOverviewPanel.IsVisible = true;
        ReadyPanel.IsVisible = true;
        ReadyStatusText.Text = AppStrings.Get("Scenario_Exited");
        StartButton.Focus();
    }

    private void OnTranslationClicked(object? sender, RoutedEventArgs args)
    {
        if (_lastNpcResponse is null)
        {
            return;
        }

        SupportText.Text = AppStrings.Format(
            "Scenario_TranslationResult",
            Translation(_lastNpcResponse));
        SupportText.IsVisible = true;
    }

    private void OnHintClicked(object? sender, RoutedEventArgs args)
    {
        if (_controller is null)
        {
            return;
        }

        SupportText.Text = _controller.Session?.StateId == _controller.Definition.WaitingStateId
            ? _controller.Definition.FrameHint
            : _controller.Definition.ItemHint;
        SupportText.IsVisible = true;
    }

    private async void OnRepeatClicked(object? sender, RoutedEventArgs args)
    {
        await SpeakLastNpcAsync(rate: 1);
    }

    private async void OnSlowerClicked(object? sender, RoutedEventArgs args) =>
        await SpeakLastNpcAsync(rate: 0.72);

    private async void OnStopPlaybackClicked(object? sender, RoutedEventArgs args)
    {
        _playbackCancellation?.Cancel();
        if (_speechSynthesisProvider is not null)
        {
            await _speechSynthesisProvider.StopAsync();
        }

        SpeechStatusText.Text = AppStrings.Get("Scenario_PlaybackStopped");
    }

    private async Task SpeakLastNpcAsync(double rate)
    {
        if (_lastNpcResponse is null ||
            _speechSynthesisProvider is null ||
            _controller?.Session is not { } session)
        {
            SpeechStatusText.Text = AppStrings.Get("Scenario_NoCaption");
            return;
        }

        _playbackCancellation?.Cancel();
        _playbackCancellation?.Dispose();
        _playbackCancellation = new CancellationTokenSource();
        SpeechStatusText.Text = rate < 1
            ? AppStrings.Get("Scenario_PlayingSlowly")
            : AppStrings.Get("Scenario_Playing");
        var result = await _speechSynthesisProvider.SpeakAsync(
            new SpeechSynthesisRequest(
                Guid.NewGuid(),
                _lastNpcResponse,
                new LanguageCode("de"),
                session.Id.ToString("N"),
                Rate: rate),
            _playbackCancellation.Token);
        SpeechStatusText.Text = result.Message;
    }

    private void OnRecordClicked(object? sender, RoutedEventArgs args)
    {
        SpeechDisclosurePanel.IsVisible = true;
        RecordReplyButton.IsVisible = false;
        ConfirmRecordButton.Focus();
    }

    private void OnDismissRecordClicked(object? sender, RoutedEventArgs args)
    {
        SpeechDisclosurePanel.IsVisible = false;
        RecordReplyButton.IsVisible = true;
        RecordReplyButton.Focus();
    }

    private async void OnConfirmRecordClicked(object? sender, RoutedEventArgs args)
    {
        if (_controller is null || _speechRecognitionProvider is null || _busy)
        {
            return;
        }

        CancelActiveSpeechRequest();
        SpeechDisclosurePanel.IsVisible = false;
        RecordingPanel.IsVisible = true;
        RecordReplyButton.IsVisible = false;
        SpeechStatusText.Text = AppStrings.Get("Scenario_MicrophoneActive");
        _recognitionCancellation?.Dispose();
        _recognitionCancellation = new CancellationTokenSource();
        try
        {
            _activeSpeechRequestId = _controller.BeginSpeechInput();
            var request = new SpeechRecognitionRequest(
                _activeSpeechRequestId.Value,
                new LanguageCode("de"),
                TimeSpan.FromSeconds(15),
                RetainAudio: false);
            var result = await _speechRecognitionProvider.RecognizeAsync(
                request,
                _recognitionCancellation.Token);
            RecordingPanel.IsVisible = false;
            if (result.Status == SpeechRecognitionResultStatus.Accepted &&
                !string.IsNullOrWhiteSpace(result.Transcript))
            {
                _pendingSpeechResult = result;
                _recognitionCancellation?.Dispose();
                _recognitionCancellation = null;
                _settingTranscript = true;
                LearnerInput.Text = result.Transcript;
                _settingTranscript = false;
                SpeechStatusText.Text = result.Message + " " +
                    AppStrings.Get("Scenario_TranscriptAccepted");
                RecordReplyButton.IsVisible = true;
                LearnerInput.Focus();
            }
            else
            {
                _controller.CancelSpeechInput(result.RequestId);
                _activeSpeechRequestId = null;
                _recognitionCancellation?.Dispose();
                _recognitionCancellation = null;
                SpeechStatusText.Text = result.Message;
                RecordReplyButton.IsVisible = true;
            }
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or ArgumentException)
        {
            RecordingPanel.IsVisible = false;
            _recognitionCancellation?.Dispose();
            _recognitionCancellation = null;
            SpeechStatusText.Text = exception.Message;
            RecordReplyButton.IsVisible = true;
        }
    }

    private void OnCancelRecordingClicked(object? sender, RoutedEventArgs args)
    {
        _recognitionCancellation?.Cancel();
        CancelActiveSpeechRequest();
        RecordingPanel.IsVisible = false;
        RecordReplyButton.IsVisible = true;
        SpeechStatusText.Text = AppStrings.Get("Scenario_RecordingCancelled");
        LearnerInput.Focus();
    }

    private void OnLearnerInputChanged(object? sender, TextChangedEventArgs args)
    {
        if (_settingTranscript || _pendingSpeechResult is null)
        {
            return;
        }

        if (!string.Equals(
                LearnerInput.Text?.Trim(),
                _pendingSpeechResult.Transcript?.Trim(),
                StringComparison.Ordinal))
        {
            CancelActiveSpeechRequest();
            SpeechStatusText.Text = AppStrings.Get("Scenario_TranscriptEdited");
        }
    }

    private async Task RefreshSpeechCapabilitiesAsync()
    {
        if (_speechSynthesisProvider is null || _speechRecognitionProvider is null)
        {
            SpeechStatusText.Text = AppStrings.Get("Scenario_SpeechUnavailable");
            RecordReplyButton.IsEnabled = false;
            _recognitionAvailable = false;
            RepeatButton.IsEnabled = false;
            SlowerButton.IsEnabled = false;
            StopPlaybackButton.IsEnabled = false;
            return;
        }

        var synthesisTask = _speechSynthesisProvider.InspectAsync();
        var recognitionTask = _speechRecognitionProvider.InspectAsync();
        await Task.WhenAll(synthesisTask, recognitionTask);
        var synthesis = await synthesisTask;
        var recognition = await recognitionTask;
        var hasGermanVoice = synthesis.Status == SpeechCapabilityStatus.Available &&
                             synthesis.Voices.Any(voice => voice.Language == new LanguageCode("de"));
        RepeatButton.IsEnabled = hasGermanVoice;
        SlowerButton.IsEnabled = hasGermanVoice;
        StopPlaybackButton.IsEnabled = hasGermanVoice;
        _recognitionAvailable = recognition.Status == SpeechCapabilityStatus.Available;
        RecordReplyButton.IsEnabled = _microphoneAllowed && _recognitionAvailable;
        SpeechStatusText.Text = AppStrings.Format(
            "Scenario_SpeechStatus",
            hasGermanVoice
                ? AppStrings.Get("Scenario_GermanVoiceReady")
                : AppStrings.Get("Scenario_GermanVoiceMissing"),
            recognition.Message);
    }

    private void RenderScenarioTheatre(string npcLine)
    {
        if (_controller is null)
        {
            return;
        }

        var definition = _controller.Definition;
        ScenarioTheatreHost.Content = ScenarioTheatreRenderer.RenderLive(
            _imageCache,
            new ScenarioTheatreLivePresentation(
                AppStrings.Get("ScenarioTheatre_LiveInstruction"),
                definition.Goal,
                definition.Context,
                definition.NpcRole,
                definition.SuccessCriteria,
                ScenarioStateLabel(definition),
                npcLine),
            _shouldReduceMotion);
    }

    private string ScenarioStateLabel(CafeOrderDefinition definition) =>
        _controller?.Session?.StateId switch
        {
            var state when state == definition.FrameStateId =>
                AppStrings.Get("ScenarioTheatre_StateChooseItem"),
            var state when state == definition.CompleteStateId =>
                AppStrings.Get("Scenario_Completed_Title"),
            _ => AppStrings.Get("Scenario_AtCounter"),
        };

    private ResolvedTemplateParameters CreateConsequenceParameters(
        LearningEvidence evidence,
        string pronunciationText) => new(
        new Dictionary<string, ResolvedTemplateParameter>
        {
            ["instruction"] = new(
                TemplateParameterKind.TextByLanguage,
                TextByLanguage: new Dictionary<string, string>
                {
                    [_instructionLanguage.Value] = AppStrings.Get("ConsequenceVerdict_Instruction"),
                }),
            ["subject"] = new(
                TemplateParameterKind.Text,
                Text: AppStrings.Get("ConsequenceVerdict_Learner")),
            ["state-label"] = new(
                TemplateParameterKind.Text,
                Text: AppStrings.Get("ConsequenceVerdict_StateComplete")),
            ["verdicts"] = new(
                TemplateParameterKind.OptionList,
                Options:
                [
                    new("ready", AppStrings.Get("ConsequenceVerdict_VerdictReady")),
                    new("success", AppStrings.Get("Scenario_Completed_Title")),
                    new("uncertain", AppStrings.Get("ConsequenceVerdict_VerdictUncertain")),
                    new("failure", AppStrings.Get("ConsequenceVerdict_VerdictFailure")),
                ]),
            ["consequences"] = new(
                TemplateParameterKind.OptionList,
                Options:
                [
                    new("ready", AppStrings.Get("ConsequenceVerdict_ConsequenceReady")),
                    new("success", AppStrings.Get("ConsequenceVerdict_ConsequenceSuccess")),
                    new("uncertain", AppStrings.Get("ConsequenceVerdict_ConsequenceUncertain")),
                    new("failure", AppStrings.Get("ConsequenceVerdict_ConsequenceFailure")),
                ]),
            ["report-lines"] = new(
                TemplateParameterKind.OptionList,
                Options:
                [
                    new("goal", AppStrings.Get("ConsequenceVerdict_ReportGoal")),
                    new(
                        "accuracy",
                        AppStrings.Format(
                            "ConsequenceVerdict_ReportAccuracy",
                            Percent(evidence.LinguisticAccuracy))),
                    new(
                        "fluency",
                        AppStrings.Format(
                            "ConsequenceVerdict_ReportFluency",
                            Percent(evidence.Fluency))),
                    new(
                        "concept",
                        AppStrings.Format(
                            "ConsequenceVerdict_ReportConcept",
                            Percent(evidence.TargetConceptPerformance))),
                    new("pronunciation", pronunciationText),
                ]),
            ["actions"] = new(
                TemplateParameterKind.OptionList,
                Options:
                [
                    new("continue", AppStrings.Get("ConsequenceVerdict_Continue")),
                    new("retry", AppStrings.Get("Scenario_PracticeAgain")),
                ]),
            ["retry-action"] = new(
                TemplateParameterKind.Text,
                Text: "retry"),
        },
        PreviewOutcome: TemplateOutcomeState.Success,
        UseTextOnlyFallback: true);

    private void OnConsequenceAction(TemplateOutcome outcome)
    {
        if (_controller is null || _busy)
        {
            return;
        }

        if (RetrySaveButton.IsVisible)
        {
            PersistenceStatusText.Text = AppStrings.Get("ConsequenceVerdict_SaveBeforeAction");
            RetrySaveButton.Focus();
            return;
        }

        if (string.Equals(outcome.ResponseId, "retry", StringComparison.Ordinal))
        {
            StartScenario();
            return;
        }

        if (string.Equals(outcome.ResponseId, "continue", StringComparison.Ordinal))
        {
            _controller.Exit();
            ActiveScenarioPanel.IsVisible = false;
            ScenarioOverviewPanel.IsVisible = true;
            ReadyPanel.IsVisible = true;
            ReadyStatusText.Text = AppStrings.Get("ConsequenceVerdict_ReadyAgain");
            StartButton.Focus();
        }
    }

    private void ResetTaskSurface()
    {
        ConversationPanel.Children.Clear();
        ScenarioTheatreHost.Content = null;
        ScenarioTheatreHost.IsVisible = true;
        ConsequenceVerdictHost.Content = null;
        FeedbackPanel.IsVisible = false;
        OtherObservationsExpander.IsVisible = false;
        CompletionPanel.IsVisible = false;
        InputPanel.IsVisible = true;
        RetrySaveButton.IsVisible = false;
        SupportText.IsVisible = false;
        TurnStatusText.Text = string.Empty;
        SpeechDisclosurePanel.IsVisible = false;
        RecordingPanel.IsVisible = false;
        RecordReplyButton.IsVisible = true;
        CancelActiveSpeechRequest();
        _lastNpcResponse = null;
    }

    private void AddConversationMessage(string speaker, string text, bool isLearner)
    {
        var label = new TextBlock
        {
            Text = speaker.ToUpperInvariant(),
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            Opacity = 0.68,
        };
        var message = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 16,
            Foreground = Brushes.White,
        };
        var stack = new StackPanel { Spacing = 5 };
        stack.Children.Add(label);
        stack.Children.Add(message);
        var bubble = new Border
        {
            MaxWidth = 520,
            Padding = new Thickness(14, 11),
            CornerRadius = new CornerRadius(12),
            HorizontalAlignment = isLearner
                ? Avalonia.Layout.HorizontalAlignment.Right
                : Avalonia.Layout.HorizontalAlignment.Left,
            Child = stack,
        };
        bubble.Classes.Add("conversation-bubble");
        bubble.Classes.Add(isLearner ? "learner-bubble" : "guide-bubble");
        ConversationPanel.Children.Add(bubble);

        if (!isLearner)
        {
            _lastNpcResponse = text;
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        SendButton.IsEnabled = !busy;
        LearnerInput.IsEnabled = !busy;
        ExitButton.IsEnabled = !busy;
        CancelResponseButton.IsVisible = busy;
        RetrySaveButton.IsEnabled = !busy;
        RecordReplyButton.IsEnabled = !busy && _microphoneAllowed && _recognitionAvailable;
        TurnStatusText.Text = busy
            ? AppStrings.Get("Scenario_CheckingTask")
            : TurnStatusText.Text;
    }

    private async void OnUnloaded(object? sender, RoutedEventArgs args)
    {
        _turnCancellation?.Cancel();
        _turnCancellation?.Dispose();
        _turnCancellation = null;
        _recognitionCancellation?.Cancel();
        _recognitionCancellation?.Dispose();
        _recognitionCancellation = null;
        _playbackCancellation?.Cancel();
        _playbackCancellation?.Dispose();
        _playbackCancellation = null;
        CancelActiveSpeechRequest();
        if (_speechSynthesisProvider is not null)
        {
            await _speechSynthesisProvider.StopAsync();
        }
    }

    private void CancelActiveSpeechRequest()
    {
        _recognitionCancellation?.Cancel();
        if (_activeSpeechRequestId is { } requestId && _controller is not null)
        {
            _controller.CancelSpeechInput(requestId);
        }

        _activeSpeechRequestId = null;
        _pendingSpeechResult = null;
    }

    private static string Percent(double? value) =>
        value is null
            ? AppStrings.Get("Scenario_NotMeasured")
            : value.Value.ToString("P0", AppStrings.CurrentCulture);

    private static string LanguageName(LanguageCode language) => language.Value switch
    {
        "en" => AppStrings.Get("Language_English"),
        "hi" => AppStrings.Get("Language_Hindi"),
        _ => language.Value,
    };

    private static string RelationName(TransferRelation relation) => relation switch
    {
        TransferRelation.Facilitative => AppStrings.Get("Scenario_Relation_Helpful"),
        TransferRelation.PartiallyFacilitative => AppStrings.Get("Scenario_Relation_Partial"),
        TransferRelation.Interfering => AppStrings.Get("Scenario_Relation_Interfering"),
        _ => AppStrings.Get("Scenario_Relation_Note"),
    };

    private static string Translation(string german) => german switch
    {
        "Guten Tag! Was möchten Sie?" => AppStrings.Get("Scenario_Translation_Greeting"),
        "Sie können beginnen: Ich möchte ..." => AppStrings.Get("Scenario_Translation_Frame"),
        "Kaffee, Tee oder Wasser?" => AppStrings.Get("Scenario_Translation_Options"),
        "Wählen Sie ein Getränk." => AppStrings.Get("Scenario_Translation_Choose"),
        "Gern. Einen Moment, bitte." => AppStrings.Get("Scenario_Translation_Complete"),
        _ => AppStrings.Get("Scenario_Translation_Unavailable"),
    };

    private static bool DeveloperModeEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable("LINGUISTICS_DEVELOPER_MODE"),
            "1",
            StringComparison.Ordinal);
}
