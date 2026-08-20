using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
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
        ISpeechRecognitionProvider? speechRecognitionProvider = null)
        : this()
    {
        _runtimeContentError = runtimeContentError;
        _speechSynthesisProvider = speechSynthesisProvider;
        _speechRecognitionProvider = speechRecognitionProvider;
        _microphoneAllowed = profile.Settings.Microphone != MicrophonePreference.Never;
        if (runtimeCatalog is not null)
        {
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
            ContentGateTechnicalText.Text =
                $"The local scenario could not initialize safely: {exception.Message}";
        }
    }

    private void ShowContentGate()
    {
        LoadingPanel.IsVisible = false;
        ContentGatePanel.IsVisible = true;
        ContentGateTechnicalText.IsVisible = DeveloperModeEnabled();
        ContentGateTechnicalText.Text = string.IsNullOrWhiteSpace(_runtimeContentError)
            ? "No runtime-approved content catalog was loaded."
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
        ReadyStatusText.Text = state.Message;
        PreviousAttemptsText.Text = state.PreviousCompletions == 0
            ? "No previous café completion is stored."
            : $"Completed locally {state.PreviousCompletions} time(s).";
        MissingPrerequisitesText.IsVisible = state.MissingPrerequisiteTitles.Count > 0;
        MissingPrerequisitesText.Text = state.MissingPrerequisiteTitles.Count == 0
            ? string.Empty
            : $"Still needed: {string.Join(", ", state.MissingPrerequisiteTitles)}.";
        StartButton.IsEnabled = state.CanStart;

        if (state.Bridge is { } bridge)
        {
            BridgeCard.IsVisible = true;
            BridgeLabelText.Text =
                $"{LanguageName(bridge.SourceLanguage)} BRIDGE • {RelationName(bridge.Relation)}";
            BridgeExplanationText.Text = bridge.Explanation;
            BridgeRiskText.IsVisible = bridge.Risks.Count > 0;
            BridgeRiskText.Text = bridge.Risks.Count == 0
                ? string.Empty
                : $"Keep in mind: {string.Join(" ", bridge.Risks)}";
            UseBridgeCheckBox.IsVisible = bridge.RequiresConfirmation;
            UseBridgeCheckBox.IsChecked = false;
            BridgeModeText.IsVisible = !bridge.RequiresConfirmation;
            BridgeModeText.Text = "Shown according to your saved multilingual shortcut preference.";
        }
        else
        {
            BridgeCard.IsVisible = false;
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
            var opening = _controller.Start(UseBridgeCheckBox.IsChecked == true);
            ReadyPanel.IsVisible = false;
            ActiveScenarioPanel.IsVisible = true;
            AddConversationMessage("Server", opening, isLearner: false);
            TurnStatusText.Text = "Scripted dialogue is ready. Type or use configured local speech; a selected local model may vary only an allowed server line.";
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
            TurnStatusText.Text = "Type a short reply before sending.";
            LearnerInput.Focus();
            return;
        }

        var speechResult = _pendingSpeechResult is { } pending &&
                           string.Equals(pending.Transcript?.Trim(), learnerText, StringComparison.Ordinal)
            ? pending
            : null;
        AddConversationMessage("You", learnerText, isLearner: true);
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
            TurnStatusText.Text = "The response was cancelled. The deterministic scenario remains consistent.";
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
            AddConversationMessage("Server", npcResponse, isLearner: false);
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
        TurnStatusText.Text = outcome.ModelMessage;
        if (outcome.PronunciationAssessment is { } pronunciation)
        {
            TurnStatusText.Text +=
                $" Local word evidence: {pronunciation.Evidence.MatchedWordCount} of {pronunciation.Evidence.ExpectedWordCount} expected words matched in order; this is not a phoneme or accent score.";
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
        CompletionPanel.IsVisible = true;
        var evidence = outcome.Evaluation.Evidence!;
        var pronunciationText = outcome.PronunciationAssessment is { } pronunciation
            ? $"Recognizer intelligibility proxy: {pronunciation.Evidence.MatchedWordCount} of {pronunciation.Evidence.ExpectedWordCount} expected words matched"
            : "Pronunciation: not measured from text";
        EvidenceText.Text =
            $"Communicative goal: achieved\n" +
            $"Linguistic accuracy: {Percent(evidence.LinguisticAccuracy)}\n" +
            $"Fluency: {Percent(evidence.Fluency)}\n" +
            $"Target concept: {Percent(evidence.TargetConceptPerformance)}\n" +
            pronunciationText;
        PersistenceStatusText.Text = outcome.Persisted
            ? $"Saved locally. Concept state: {outcome.UpdatedProgressState}. A deterministic review handoff was created."
            : outcome.PersistenceError;
        RetrySaveButton.IsVisible = !outcome.Persisted;
        PracticeAgainButton.IsVisible = outcome.Persisted;
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
            PersistenceStatusText.Text = result.Message;
            RetrySaveButton.IsVisible = !result.Persisted;
            PracticeAgainButton.IsVisible = result.Persisted;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OnPracticeAgainClicked(object? sender, RoutedEventArgs args) => StartScenario();

    private void OnCancelResponseClicked(object? sender, RoutedEventArgs args)
    {
        _turnCancellation?.Cancel();
        TurnStatusText.Text = "Cancelling the optional local response; scripted recovery will remain available.";
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
        ReadyPanel.IsVisible = true;
        ReadyStatusText.Text = "Scenario exited. No unfinished conversation text was stored.";
        StartButton.Focus();
    }

    private void OnTranslationClicked(object? sender, RoutedEventArgs args)
    {
        if (_lastNpcResponse is null)
        {
            return;
        }

        SupportText.Text = $"On request: {Translation(_lastNpcResponse)}";
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

        SpeechStatusText.Text = "Speech playback stopped. The caption remains in the conversation.";
    }

    private async Task SpeakLastNpcAsync(double rate)
    {
        if (_lastNpcResponse is null ||
            _speechSynthesisProvider is null ||
            _controller?.Session is not { } session)
        {
            SpeechStatusText.Text = "No server caption is ready to play.";
            return;
        }

        _playbackCancellation?.Cancel();
        _playbackCancellation?.Dispose();
        _playbackCancellation = new CancellationTokenSource();
        SpeechStatusText.Text = rate < 1 ? "Playing the server caption more slowly…" : "Playing the server caption…";
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
        SpeechStatusText.Text = "Microphone active. Speak one short café reply; processing stays local.";
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
                SpeechStatusText.Text = result.Message +
                    " Sending it unchanged keeps transcript-based intelligibility evidence; editing treats it as text.";
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
        SpeechStatusText.Text = "Recording cancelled. The café task did not change; text remains available.";
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
            SpeechStatusText.Text =
                "Transcript edited. This reply will be evaluated as text, so no pronunciation evidence will be attached.";
        }
    }

    private async Task RefreshSpeechCapabilitiesAsync()
    {
        if (_speechSynthesisProvider is null || _speechRecognitionProvider is null)
        {
            SpeechStatusText.Text = "Local speech providers are unavailable. Text and captions remain complete.";
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
        SpeechStatusText.Text =
            $"Playback: {(hasGermanVoice ? "German system voice ready" : "no German system voice")}. " +
            $"Microphone transcription: {recognition.Message}";
    }

    private void ResetTaskSurface()
    {
        ConversationPanel.Children.Clear();
        FeedbackPanel.IsVisible = false;
        OtherObservationsExpander.IsVisible = false;
        CompletionPanel.IsVisible = false;
        InputPanel.IsVisible = true;
        RetrySaveButton.IsVisible = false;
        PracticeAgainButton.IsVisible = false;
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
        PracticeAgainButton.IsEnabled = !busy;
        RecordReplyButton.IsEnabled = !busy && _microphoneAllowed && _recognitionAvailable;
        TurnStatusText.Text = busy
            ? "Checking the deterministic task, then asking the optional local renderer…"
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
        value is null ? "not measured" : $"{value:P0}";

    private static string LanguageName(LanguageCode language) => language.Value switch
    {
        "en" => "English",
        "hi" => "Hindi",
        _ => language.Value,
    };

    private static string RelationName(TransferRelation relation) => relation switch
    {
        TransferRelation.Facilitative => "helpful similarity",
        TransferRelation.PartiallyFacilitative => "partial bridge",
        TransferRelation.Interfering => "interference warning",
        _ => "language note",
    };

    private static string Translation(string german) => german switch
    {
        "Guten Tag! Was möchten Sie?" => "Hello! What would you like?",
        "Sie können beginnen: Ich möchte ..." => "You can begin: I would like ...",
        "Kaffee, Tee oder Wasser?" => "Coffee, tea, or water?",
        "Wählen Sie ein Getränk." => "Choose a drink.",
        "Gern. Einen Moment, bitte." => "Certainly. One moment, please.",
        _ => "A reviewed translation is not available for this line.",
    };

    private static bool DeveloperModeEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable("LINGUISTICS_DEVELOPER_MODE"),
            "1",
            StringComparison.Ordinal);
}
