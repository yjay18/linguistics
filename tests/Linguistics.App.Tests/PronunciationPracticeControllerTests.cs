using Linguistics.App.Features.Pronunciation;
using Linguistics.Core.Content;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class PronunciationPracticeControllerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 20, 14, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task AcceptedTranscriptPersistsMetadataButNotTranscript()
    {
        var setup = await CreateSetupAsync();
        var initialization = await setup.Controller.InitializeAsync();
        Assert.AreEqual(0, initialization.PreviousAttempts);
        var request = setup.Controller.BeginRecognition();

        var outcome = await setup.Controller.CompleteRecognitionAsync(new SpeechRecognitionResult(
            request.RequestId,
            SpeechRecognitionResultStatus.Accepted,
            "Ich möchte einen Kaffee, bitte.",
            new LanguageCode("de"),
            TimeSpan.FromSeconds(4),
            "fixture-recognizer-v1",
            "fixture-model",
            "accepted"));

        Assert.IsTrue(outcome.Persisted);
        var attempt = setup.Repository.State.Pronunciation.Attempts.Single();
        Assert.AreEqual("de.utterance.order", attempt.UtteranceId);
        Assert.AreEqual(PronunciationAssessmentOutcome.Intelligible, attempt.Evidence.Outcome);
        Assert.AreEqual(5, attempt.Evidence.MatchedWordCount);
        Assert.IsFalse(attempt.ToString().Contains("Ich möchte", StringComparison.Ordinal));
        Assert.IsEmpty(setup.Repository.State.Tasks.Attempts);
    }

    [TestMethod]
    public async Task ExpiredRecognitionResultIsRejectedWithoutPersistence()
    {
        var setup = await CreateSetupAsync();
        await setup.Controller.InitializeAsync();
        var request = setup.Controller.BeginRecognition();
        setup.Controller.CancelRecognition(request.RequestId);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            setup.Controller.CompleteRecognitionAsync(new SpeechRecognitionResult(
                request.RequestId,
                SpeechRecognitionResultStatus.NoSpeech,
                null,
                new LanguageCode("de"),
                TimeSpan.FromSeconds(15),
                "fixture-recognizer-v1",
                "fixture-model",
                "no speech")));

        StringAssert.Contains(exception.Message, "expired");
        Assert.IsEmpty(setup.Repository.State.Pronunciation.Attempts);
    }

    [TestMethod]
    public async Task FailedMetadataSaveCanBeRetriedWithoutDuplicateAttempt()
    {
        var setup = await CreateSetupAsync();
        await setup.Controller.InitializeAsync();
        var request = setup.Controller.BeginRecognition();
        setup.Repository.FailSave = true;

        var outcome = await setup.Controller.CompleteRecognitionAsync(new SpeechRecognitionResult(
            request.RequestId,
            SpeechRecognitionResultStatus.NoSpeech,
            null,
            new LanguageCode("de"),
            TimeSpan.FromSeconds(15),
            "fixture-recognizer-v1",
            "fixture-model",
            "no speech"));

        Assert.IsFalse(outcome.Persisted);
        Assert.IsEmpty(setup.Repository.State.Pronunciation.Attempts);
        setup.Repository.FailSave = false;
        var retry = await setup.Controller.RetryPersistenceAsync();
        Assert.IsTrue(retry.Persisted);
        Assert.HasCount(1, setup.Repository.State.Pronunciation.Attempts);
    }

    private static async Task<Setup> CreateSetupAsync()
    {
        var profile = new LearnerProfile(
            Guid.NewGuid(),
            new LanguageCode("de"),
            [new KnownLanguage(
                new LanguageCode("en"),
                LanguageProficiency.Advanced,
                true,
                true,
                true)],
            new LearnerSettings(
                MultilingualShortcutMode.Automatic,
                null,
                MicrophonePreference.Later,
                RetainSpeechRecordings: false));
        var repository = new PronunciationRepository(
            profile,
            new LearnerLearningState(
                CurriculumHistory.Empty,
                TaskHistory.Empty,
                PronunciationHistory.Empty));
        var owner = new LearnerProfileOwner(repository);
        await owner.RestoreAsync();
        var utterance = new RuntimePronunciationUtterance(
            "de.utterance.order",
            new LanguageCode("de"),
            "de-de",
            "Ich möchte einen Kaffee, bitte.",
            PronunciationPurpose.Production,
            [new ConceptId("de.function.order-polite")],
            new VersionId("language.de.core.v1"));
        var controller = PronunciationPracticeController.CreateFromUtterance(
            profile,
            owner,
            utterance,
            new TranscriptPronunciationAssessmentProvider(),
            () => Now);
        return new Setup(controller, repository);
    }

    private sealed record Setup(
        PronunciationPracticeController Controller,
        PronunciationRepository Repository);

    private sealed class PronunciationRepository(
        LearnerProfile profile,
        LearnerLearningState state) : ILearnerRepository
    {
        public LearnerLearningState State { get; private set; } = state;

        public bool FailSave { get; set; }

        public Task<LearnerProfile?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<LearnerProfile?>(profile);

        public Task SaveAsync(
            LearnerProfile updated,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<CurriculumHistory> LoadCurriculumAsync(
            Guid profileId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(State.Curriculum);

        public Task SaveCurriculumAsync(
            Guid profileId,
            CurriculumHistory history,
            CancellationToken cancellationToken = default)
        {
            State = State with { Curriculum = history };
            return Task.CompletedTask;
        }

        public Task<LearnerLearningState> LoadLearningStateAsync(
            Guid profileId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(State);

        public Task SaveLearningStateAsync(
            Guid profileId,
            LearnerLearningState updated,
            CancellationToken cancellationToken = default)
        {
            if (FailSave)
            {
                throw new LearnerStoreException("fixture save failure");
            }

            State = updated;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
