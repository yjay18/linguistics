using Linguistics.App.Features.Review;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class ReviewControllerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly ConceptId ConceptId = new("de.function.order-polite");
    private static readonly VersionId ContentVersion = new("language.de.core.v1");

    [TestMethod]
    public async Task InitializationSynchronizesLearningEvidenceOnce()
    {
        var taskAttempt = CreateTaskAttempt();
        var repository = await CreateRepositoryAsync(new LearnerLearningState(
            CreateCurriculum(ConceptProgressState.Practicing, reviewDueAt: null),
            new TaskHistory(
                [taskAttempt],
                [new ReviewHandoff(
                    Guid.NewGuid(),
                    taskAttempt.Id,
                    ConceptId,
                    Now.AddDays(-2),
                    ["de.error.accusative-masculine"])]),
            PronunciationHistory.Empty,
            ReviewHistory.Empty));
        var controller = new ReviewController(repository.Owner, Graph(), () => Now);

        var first = await controller.InitializeAsync();
        var repeated = await controller.InitializeAsync();

        Assert.HasCount(3, first.Queue.Due);
        Assert.HasCount(3, repeated.Queue.Due);
        Assert.AreEqual(1, repository.SaveLearningStateCount);
        CollectionAssert.AreEquivalent(
            new[] { ReviewItemKind.Phrase, ReviewItemKind.Concept, ReviewItemKind.RecurringError },
            repository.State.Review.Schedules.Select(item => item.Kind).ToArray());
    }

    [TestMethod]
    public async Task ReviewAndConceptProgressSaveAtomically()
    {
        var taskAttempt = CreateTaskAttempt();
        var repository = await CreateRepositoryAsync(new LearnerLearningState(
            CreateCurriculum(ConceptProgressState.ReviewDue, Now.AddDays(-1)),
            new TaskHistory(
                [taskAttempt],
                [new ReviewHandoff(
                    Guid.NewGuid(),
                    taskAttempt.Id,
                    ConceptId,
                    Now.AddDays(-2),
                    [])]),
            PronunciationHistory.Empty,
            ReviewHistory.Empty));
        var controller = new ReviewController(repository.Owner, Graph(), () => Now);
        var initialized = await controller.InitializeAsync();
        var concept = initialized.Queue.Due.Single(item => item.Kind == ReviewItemKind.Concept);

        var submission = await controller.RecordAsync(
            concept.Id,
            ReviewRating.Good,
            TimeSpan.FromSeconds(4));

        Assert.AreEqual(ConceptProgressState.Mastered, repository.State.Curriculum.Progress.Single().State);
        Assert.HasCount(2, repository.State.Curriculum.Attempts);
        Assert.HasCount(1, repository.State.Review.Attempts);
        Assert.IsFalse(submission.Snapshot.Queue.Due.Any(item => item.Id == concept.Id));
        Assert.AreEqual(2, repository.SaveLearningStateCount);
    }

    [TestMethod]
    public async Task FailedSaveCanRetryWithoutDuplicatingReviewEvidence()
    {
        var schedule = new ReviewSchedule(
            ReviewItemId.Create(ReviewItemKind.Concept, ConceptId.Value),
            ReviewItemKind.Concept,
            ConceptId.Value,
            ContentVersion,
            Now.AddDays(-2),
            LastSeenAt: null,
            Now.AddDays(-1),
            SuccessStreak: 0,
            FailureCount: 0,
            Difficulty: 0.5,
            RecentLatency: null,
            ReviewConfiguration.Default.Version);
        var repository = await CreateRepositoryAsync(new LearnerLearningState(
            CreateCurriculum(ConceptProgressState.ReviewDue, Now.AddDays(-1)),
            TaskHistory.Empty,
            PronunciationHistory.Empty,
            new ReviewHistory([schedule], [])));
        var controller = new ReviewController(repository.Owner, Graph(), () => Now);
        await controller.InitializeAsync();
        repository.FailNextLearningStateSave = true;

        await Assert.ThrowsExactlyAsync<LearnerStoreException>(() => controller.RecordAsync(
            schedule.Id,
            ReviewRating.Good,
            TimeSpan.FromSeconds(3)));
        Assert.IsEmpty(repository.State.Review.Attempts);
        Assert.HasCount(1, repository.State.Curriculum.Attempts);

        await controller.RecordAsync(schedule.Id, ReviewRating.Good, TimeSpan.FromSeconds(3));

        Assert.HasCount(1, repository.State.Review.Attempts);
        Assert.HasCount(2, repository.State.Curriculum.Attempts);
    }

    private static async Task<ReviewRepository> CreateRepositoryAsync(LearnerLearningState state)
    {
        var profile = new LearnerProfile(
            Guid.NewGuid(),
            new LanguageCode("de"),
            [],
            new LearnerSettings(
                MultilingualShortcutMode.Never,
                null,
                MicrophonePreference.Later,
                RetainSpeechRecordings: false));
        var repository = new ReviewRepository(profile, state);
        await repository.Owner.RestoreAsync();
        return repository;
    }

    private static CurriculumHistory CreateCurriculum(
        ConceptProgressState state,
        DateTimeOffset? reviewDueAt)
    {
        var priorAttempt = new ConceptAttempt(
            Guid.NewGuid(),
            ConceptId,
            Now.AddDays(-2),
            new LearningEvidence(
                CommunicativeSuccess: true,
                LinguisticAccuracy: 0.9,
                Fluency: 0.8,
                Pronunciation: null,
                TargetConceptPerformance: 0.9,
                Comprehension: null,
                DelayedRecall: null),
            ContentVersion,
            ProgressionConfiguration.Default.Version,
            ConceptSelectionConfiguration.Default.Version,
            SelectedBridge: null);
        return CurriculumHistory.Empty with
        {
            Progress = [new ConceptProgress(
                ConceptId,
                state,
                AttemptCount: 1,
                LastAttemptAt: priorAttempt.OccurredAt,
                reviewDueAt,
                RecurringErrorCount: 0,
                CognitiveLoad: 1)],
            Attempts = [priorAttempt],
        };
    }

    private static TaskAttempt CreateTaskAttempt() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "de.task.cafe.order-one-item",
            Now.AddDays(-2).AddMinutes(-2),
            Now.AddDays(-2),
            TurnCount: 2,
            RetryCount: 1,
            new LearningEvidence(
                CommunicativeSuccess: true,
                LinguisticAccuracy: 0.9,
                Fluency: 0.8,
                Pronunciation: null,
                TargetConceptPerformance: 0.9,
                Comprehension: null,
                DelayedRecall: null),
            ["de.error.accusative-masculine"],
            ContentVersion,
            new VersionId("cafe-order-evaluator-v1"),
            DialogueRealizationMode.Scripted,
            LocalModel: null,
            "cafe-order-dialogue-v1",
            SelectedBridge: null);

    private static ConceptGraph Graph() =>
        new([
            new ConceptNode(
                ConceptId,
                new LanguageCode("de"),
                ConceptType.Pragmatic,
                "Order politely",
                "Request one item in a café.",
                "A1",
                [],
                ["Complete one order."],
                [],
                ["cafe"],
                ContentVersion),
        ]);

    private sealed class ReviewRepository : ILearnerRepository
    {
        private LearnerProfile? _profile;

        public ReviewRepository(LearnerProfile profile, LearnerLearningState state)
        {
            _profile = profile;
            State = state;
            Owner = new LearnerProfileOwner(this);
        }

        public LearnerProfileOwner Owner { get; }

        public LearnerLearningState State { get; private set; }

        public int SaveLearningStateCount { get; private set; }

        public bool FailNextLearningStateSave { get; set; }

        public Task<LearnerProfile?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_profile);

        public Task SaveAsync(LearnerProfile profile, CancellationToken cancellationToken = default)
        {
            _profile = profile;
            return Task.CompletedTask;
        }

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
            LearnerLearningState state,
            CancellationToken cancellationToken = default)
        {
            if (FailNextLearningStateSave)
            {
                FailNextLearningStateSave = false;
                throw new LearnerStoreException("Fixture save failed.");
            }

            State = state;
            SaveLearningStateCount++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CancellationToken cancellationToken = default)
        {
            _profile = null;
            return Task.CompletedTask;
        }
    }
}
