using Linguistics.Core.Content;
using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.App.Features.Pronunciation;

public sealed record PronunciationPracticeInitialization(
    RuntimePronunciationUtterance Utterance,
    int PreviousAttempts,
    bool MicrophoneAllowed,
    string Message);

public sealed record PronunciationPracticeOutcome(
    PronunciationAssessmentResult Assessment,
    bool Persisted,
    string PersistenceMessage);

public sealed record PronunciationPersistenceResult(bool Persisted, string Message);

public sealed class PronunciationPracticeController
{
    private readonly LearnerProfile _profile;
    private readonly LearnerProfileOwner _profileOwner;
    private readonly RuntimePronunciationUtterance _utterance;
    private readonly IPronunciationAssessmentProvider _assessmentProvider;
    private readonly Func<DateTimeOffset> _clock;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private LearnerLearningState? _learningState;
    private LearnerLearningState? _pendingState;
    private Guid? _activeRequestId;

    private PronunciationPracticeController(
        LearnerProfile profile,
        LearnerProfileOwner profileOwner,
        RuntimePronunciationUtterance utterance,
        IPronunciationAssessmentProvider assessmentProvider,
        Func<DateTimeOffset>? clock)
    {
        _profile = profile;
        _profileOwner = profileOwner;
        _utterance = utterance;
        _assessmentProvider = assessmentProvider;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public RuntimePronunciationUtterance Utterance => _utterance;

    public static PronunciationPracticeController Create(
        LearnerProfile profile,
        LearnerProfileOwner profileOwner,
        ValidatedContentCatalog catalog,
        IPronunciationAssessmentProvider assessmentProvider,
        Func<DateTimeOffset>? clock = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(profileOwner);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(assessmentProvider);

        var utterance = catalog
            .CreateRuntimePronunciationUtterances(profile.TargetLanguage)
            .Single(item => item.Id == "de.utterance.order");
        return new PronunciationPracticeController(
            profile,
            profileOwner,
            utterance,
            assessmentProvider,
            clock);
    }

    internal static PronunciationPracticeController CreateFromUtterance(
        LearnerProfile profile,
        LearnerProfileOwner profileOwner,
        RuntimePronunciationUtterance utterance,
        IPronunciationAssessmentProvider assessmentProvider,
        Func<DateTimeOffset>? clock = null) =>
        new(profile, profileOwner, utterance, assessmentProvider, clock);

    public async Task<PronunciationPracticeInitialization> InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        _learningState = await _profileOwner
            .LoadLearningStateAsync(cancellationToken)
            .ConfigureAwait(false);
        var previousAttempts = _learningState.Pronunciation.Attempts.Count(attempt =>
            attempt.UtteranceId == _utterance.Id);
        var microphoneAllowed = _profile.Settings.Microphone != MicrophonePreference.Never;
        return new PronunciationPracticeInitialization(
            _utterance,
            previousAttempts,
            microphoneAllowed,
            microphoneAllowed
                ? "Listen first, then record locally. Text and captions remain available."
                : "Your saved preference disables microphone use. Playback and captions remain available.");
    }

    public SpeechRecognitionRequest BeginRecognition()
    {
        if (_learningState is null)
        {
            throw new InvalidOperationException("Initialize pronunciation practice before recording.");
        }

        if (_profile.Settings.Microphone == MicrophonePreference.Never)
        {
            throw new InvalidOperationException("Microphone use is disabled in Settings.");
        }

        if (_pendingState is not null)
        {
            throw new InvalidOperationException("Retry the pending local save before recording again.");
        }

        if (_activeRequestId is not null)
        {
            throw new InvalidOperationException("A pronunciation recording is already active.");
        }

        _activeRequestId = Guid.NewGuid();
        return new SpeechRecognitionRequest(
            _activeRequestId.Value,
            _utterance.Language,
            TimeSpan.FromSeconds(15),
            RetainAudio: false);
    }

    public void CancelRecognition(Guid requestId)
    {
        if (_activeRequestId == requestId)
        {
            _activeRequestId = null;
        }
    }

    public async Task<PronunciationPracticeOutcome> CompleteRecognitionAsync(
        SpeechRecognitionResult recognition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recognition);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_activeRequestId != recognition.RequestId)
            {
                throw new InvalidOperationException(
                    "This pronunciation result belongs to an expired recording and was ignored.");
            }

            _activeRequestId = null;
            if (recognition.Status is not (
                    SpeechRecognitionResultStatus.Accepted or
                    SpeechRecognitionResultStatus.NoSpeech))
            {
                throw new ArgumentException(
                    "The recognition result does not contain assessable local evidence.",
                    nameof(recognition));
            }

            var assessment = _assessmentProvider.Assess(
                new PronunciationAssessmentRequest(
                    _utterance.Text,
                    recognition.Transcript ?? string.Empty,
                    recognition.Duration),
                recognition.ProviderVersion);
            BuildPendingAttempt(assessment.Evidence);
            var persistence = await SavePendingAsync().ConfigureAwait(false);
            return new PronunciationPracticeOutcome(
                assessment,
                persistence.Persisted,
                persistence.Message);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<PronunciationPersistenceResult> RetryPersistenceAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            return await SavePendingAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private void BuildPendingAttempt(PronunciationEvidence evidence)
    {
        if (_learningState is null)
        {
            throw new InvalidOperationException("Pronunciation learning state is unavailable.");
        }

        var pronunciation = _learningState.Pronunciation with
        {
            Attempts = _learningState.Pronunciation.Attempts.Append(new PronunciationAttempt(
                Guid.NewGuid(),
                _utterance.Id,
                _clock(),
                evidence,
                _utterance.ContentVersion.Value)).ToArray(),
        };
        PronunciationHistoryValidator.Validate(pronunciation);
        _pendingState = _learningState with { Pronunciation = pronunciation };
    }

    private async Task<PronunciationPersistenceResult> SavePendingAsync()
    {
        if (_pendingState is null)
        {
            return new PronunciationPersistenceResult(true, "There is no pending pronunciation update.");
        }

        try
        {
            await _profileOwner
                .SaveLearningStateAsync(_pendingState, CancellationToken.None)
                .ConfigureAwait(false);
            _learningState = _pendingState;
            _pendingState = null;
            return new PronunciationPersistenceResult(
                true,
                "Saved only local pronunciation metadata; no transcript or audio was stored.");
        }
        catch (Exception exception) when (
            exception is LearnerStoreException or
            LearnerProfileValidationException or
            ArgumentException)
        {
            return new PronunciationPersistenceResult(
                false,
                $"The result is visible, but its local metadata was not saved: {exception.Message}");
        }
    }
}
