using Avalonia.Controls;
using Avalonia.Interactivity;
using Linguistics.Core.Content;
using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.App.Features.Pronunciation;

public partial class PronunciationView : UserControl
{
    private readonly ISpeechSynthesisProvider? _synthesisProvider;
    private readonly ISpeechRecognitionProvider? _recognitionProvider;
    private readonly string? _runtimeContentError;
    private PronunciationPracticeController? _controller;
    private CancellationTokenSource? _recognitionCancellation;
    private CancellationTokenSource? _playbackCancellation;
    private SpeechRecognitionRequest? _activeRecognitionRequest;
    private bool _initialized;

    public PronunciationView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public PronunciationView(
        LearnerProfile profile,
        LearnerProfileOwner profileOwner,
        ValidatedContentCatalog? runtimeCatalog,
        string? runtimeContentError,
        ISpeechSynthesisProvider speechSynthesisProvider,
        ISpeechRecognitionProvider speechRecognitionProvider,
        IPronunciationAssessmentProvider assessmentProvider)
        : this()
    {
        _synthesisProvider = speechSynthesisProvider;
        _recognitionProvider = speechRecognitionProvider;
        _runtimeContentError = runtimeContentError;
        if (runtimeCatalog is not null)
        {
            _controller = PronunciationPracticeController.Create(
                profile,
                profileOwner,
                runtimeCatalog,
                assessmentProvider);
        }
    }

    private async void OnLoaded(object? sender, RoutedEventArgs args)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        if (_controller is null || _synthesisProvider is null || _recognitionProvider is null)
        {
            ShowContentGate();
            return;
        }

        try
        {
            var initializationTask = _controller.InitializeAsync();
            var synthesisTask = _synthesisProvider.InspectAsync();
            var recognitionTask = _recognitionProvider.InspectAsync();
            await Task.WhenAll(initializationTask, synthesisTask, recognitionTask);
            var initialization = await initializationTask;
            var synthesis = await synthesisTask;
            var recognition = await recognitionTask;
            ShowPractice(initialization, synthesis, recognition);
        }
        catch (Exception exception) when (
            exception is LearnerStoreException or
            LearnerProfileValidationException or
            InvalidOperationException or
            ArgumentException)
        {
            LoadingPanel.IsVisible = false;
            ContentGatePanel.IsVisible = true;
            ContentGateTechnicalText.IsVisible = true;
            ContentGateTechnicalText.Text =
                $"Pronunciation practice could not initialize safely: {exception.Message}";
        }
    }

    private void ShowContentGate()
    {
        LoadingPanel.IsVisible = false;
        ContentGatePanel.IsVisible = true;
        ContentGateTechnicalText.IsVisible = DeveloperModeEnabled();
        ContentGateTechnicalText.Text = string.IsNullOrWhiteSpace(_runtimeContentError)
            ? "No approved runtime content catalog was loaded."
            : _runtimeContentError;
    }

    private void ShowPractice(
        PronunciationPracticeInitialization initialization,
        SpeechSynthesisSnapshot synthesis,
        SpeechRecognitionSnapshot recognition)
    {
        LoadingPanel.IsVisible = false;
        ContentGatePanel.IsVisible = false;
        PracticePanel.IsVisible = true;
        ExpectedPhraseText.Text = initialization.Utterance.Text;
        PreviousAttemptsText.Text = initialization.PreviousAttempts == 0
            ? "No prior pronunciation attempt metadata is stored."
            : $"{initialization.PreviousAttempts} prior attempt(s) are stored as counts and outcomes only.";

        var hasVoice = synthesis.Status == SpeechCapabilityStatus.Available &&
                       synthesis.Voices.Any(voice => voice.Language == initialization.Utterance.Language);
        ListenButton.IsEnabled = hasVoice;
        SlowerButton.IsEnabled = hasVoice;
        StopPlaybackButton.IsEnabled = hasVoice;
        PlaybackStatusText.Text = hasVoice
            ? synthesis.Message
            : "No matching system voice is installed. The caption remains the authoritative phrase.";

        var recognitionAvailable = recognition.Status == SpeechCapabilityStatus.Available;
        RecordButton.IsEnabled = recognitionAvailable && initialization.MicrophoneAllowed;
        RecognitionStatusText.Text = initialization.MicrophoneAllowed
            ? recognition.Message
            : initialization.Message;
        SpeechModelDetailsText.Text = recognition.Model is { } model
            ? $"Model: {model.Name} • {FormatBytes(model.SizeBytes)} • provider {model.ProviderVersion}\nSource: {model.Source}\nLicense: {model.License}"
            : "No model was downloaded or selected by Linguistics. Configure one explicitly; text and playback do not depend on it.";
    }

    private async void OnListenClicked(object? sender, RoutedEventArgs args) =>
        await SpeakAsync(rate: 1);

    private async void OnSlowerClicked(object? sender, RoutedEventArgs args) =>
        await SpeakAsync(rate: 0.72);

    private async Task SpeakAsync(double rate)
    {
        if (_controller is null || _synthesisProvider is null)
        {
            return;
        }

        _playbackCancellation?.Cancel();
        _playbackCancellation?.Dispose();
        _playbackCancellation = new CancellationTokenSource();
        var utterance = _controller.Utterance;
        PlaybackStatusText.Text = rate < 1 ? "Playing the caption more slowly…" : "Playing the caption…";
        var result = await _synthesisProvider.SpeakAsync(
            new SpeechSynthesisRequest(
                Guid.NewGuid(),
                utterance.Text,
                utterance.Language,
                utterance.Id,
                Rate: rate),
            _playbackCancellation.Token);
        PlaybackStatusText.Text = result.Message;
    }

    private async void OnStopPlaybackClicked(object? sender, RoutedEventArgs args)
    {
        _playbackCancellation?.Cancel();
        if (_synthesisProvider is not null)
        {
            await _synthesisProvider.StopAsync();
        }

        PlaybackStatusText.Text = "Speech playback stopped. The caption remains visible.";
    }

    private void OnRecordClicked(object? sender, RoutedEventArgs args)
    {
        DisclosurePanel.IsVisible = true;
        RecordButton.IsVisible = false;
        StartRecordingButton.Focus();
    }

    private void OnDismissDisclosureClicked(object? sender, RoutedEventArgs args)
    {
        DisclosurePanel.IsVisible = false;
        RecordButton.IsVisible = true;
        RecordButton.Focus();
    }

    private async void OnStartRecordingClicked(object? sender, RoutedEventArgs args)
    {
        if (_controller is null || _recognitionProvider is null)
        {
            return;
        }

        DisclosurePanel.IsVisible = false;
        ResultPanel.IsVisible = false;
        RecordingPanel.IsVisible = true;
        RecognitionStatusText.Text = "Microphone active; processing remains local.";
        _recognitionCancellation?.Cancel();
        _recognitionCancellation?.Dispose();
        _recognitionCancellation = new CancellationTokenSource();
        try
        {
            _activeRecognitionRequest = _controller.BeginRecognition();
            var result = await _recognitionProvider.RecognizeAsync(
                _activeRecognitionRequest,
                _recognitionCancellation.Token);
            RecordingPanel.IsVisible = false;
            if (result.Status is SpeechRecognitionResultStatus.Accepted or SpeechRecognitionResultStatus.NoSpeech)
            {
                var outcome = await _controller.CompleteRecognitionAsync(result);
                _activeRecognitionRequest = null;
                ShowOutcome(result, outcome);
            }
            else
            {
                _controller.CancelRecognition(result.RequestId);
                _activeRecognitionRequest = null;
                RecognitionStatusText.Text = result.Message;
                RecordButton.IsVisible = true;
            }
        }
        catch (OperationCanceledException)
        {
            RecordingPanel.IsVisible = false;
            RecognitionStatusText.Text = "Recording cancelled. No pronunciation result was created.";
            RecordButton.IsVisible = true;
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or ArgumentException or LearnerStoreException)
        {
            RecordingPanel.IsVisible = false;
            RecognitionStatusText.Text = exception.Message;
            RecordButton.IsVisible = true;
        }
    }

    private void ShowOutcome(
        SpeechRecognitionResult recognition,
        PronunciationPracticeOutcome outcome)
    {
        ResultPanel.IsVisible = true;
        RecordButton.IsVisible = false;
        OutcomeTitleText.Text = outcome.Assessment.Evidence.Outcome switch
        {
            PronunciationAssessmentOutcome.Intelligible => "Phrase understood",
            PronunciationAssessmentOutcome.PartlyIntelligible => "Part of the phrase was understood",
            PronunciationAssessmentOutcome.NotIntelligible => "Try the phrase once more",
            _ => "No speech evidence yet",
        };
        OutcomeMessageText.Text = outcome.Assessment.Message;
        RecognizedPhraseText.Text = recognition.Transcript ?? "No words recognized";
        WordComparisonText.Text = Comparison(outcome.Assessment);
        PersistenceStatusText.Text = outcome.PersistenceMessage;
        RetrySaveButton.IsVisible = !outcome.Persisted;
        RecognitionStatusText.Text = recognition.Message;
    }

    private void OnTryAgainClicked(object? sender, RoutedEventArgs args)
    {
        ResultPanel.IsVisible = false;
        RecordButton.IsVisible = true;
        RecordButton.Focus();
    }

    private async void OnRetrySaveClicked(object? sender, RoutedEventArgs args)
    {
        if (_controller is null)
        {
            return;
        }

        var result = await _controller.RetryPersistenceAsync();
        PersistenceStatusText.Text = result.Message;
        RetrySaveButton.IsVisible = !result.Persisted;
    }

    private void OnCancelRecordingClicked(object? sender, RoutedEventArgs args)
    {
        _recognitionCancellation?.Cancel();
        if (_activeRecognitionRequest is { } request && _controller is not null)
        {
            _controller.CancelRecognition(request.RequestId);
            _activeRecognitionRequest = null;
        }

        RecordingPanel.IsVisible = false;
        RecognitionStatusText.Text = "Recording cancelled. No task or pronunciation state changed.";
        RecordButton.IsVisible = true;
        RecordButton.Focus();
    }

    private async void OnUnloaded(object? sender, RoutedEventArgs args)
    {
        _recognitionCancellation?.Cancel();
        _recognitionCancellation?.Dispose();
        _playbackCancellation?.Cancel();
        _playbackCancellation?.Dispose();
        if (_activeRecognitionRequest is { } request && _controller is not null)
        {
            _controller.CancelRecognition(request.RequestId);
        }

        if (_synthesisProvider is not null)
        {
            await _synthesisProvider.StopAsync();
        }
    }

    private static string Comparison(PronunciationAssessmentResult assessment)
    {
        var missing = assessment.MissingExpectedWords.Count == 0
            ? "none"
            : string.Join(", ", assessment.MissingExpectedWords);
        var unexpected = assessment.UnexpectedRecognizedWords.Count == 0
            ? "none"
            : string.Join(", ", assessment.UnexpectedRecognizedWords);
        return $"Expected words matched in order: {assessment.Evidence.MatchedWordCount} of {assessment.Evidence.ExpectedWordCount}\n" +
               $"Expected words not heard: {missing}\nOther recognized words: {unexpected}";
    }

    private static string FormatBytes(long bytes) =>
        bytes >= 1_073_741_824
            ? $"{bytes / 1_073_741_824d:0.0} GiB"
            : $"{bytes / 1_048_576d:0} MiB";

    private static bool DeveloperModeEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable("LINGUISTICS_DEVELOPER_MODE"),
            "1",
            StringComparison.Ordinal);
}
