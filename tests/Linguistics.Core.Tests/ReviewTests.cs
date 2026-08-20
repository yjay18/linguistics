using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.Core.Tests;

[TestClass]
public sealed class ReviewTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly ConceptId ConceptId = new("de.function.order-polite");
    private static readonly VersionId ContentVersion = new("language.de.core.v1");

    [TestMethod]
    public void SchedulerIsDeterministicForFixedInputsAndUsesLatency()
    {
        var schedule = Schedule(ReviewItemKind.Concept, ConceptId.Value, Now);
        var attemptId = Guid.Parse("9c53474a-9d26-4a54-8768-216e6f4fe31b");

        var first = ReviewScheduler.Record(
            schedule,
            ReviewRating.Good,
            TimeSpan.FromSeconds(4),
            Now,
            attemptId,
            ReviewConfiguration.Default);
        var repeated = ReviewScheduler.Record(
            schedule,
            ReviewRating.Good,
            TimeSpan.FromSeconds(4),
            Now,
            attemptId,
            ReviewConfiguration.Default);
        var slow = ReviewScheduler.Record(
            schedule,
            ReviewRating.Good,
            TimeSpan.FromSeconds(20),
            Now,
            attemptId,
            ReviewConfiguration.Default);

        Assert.AreEqual(first, repeated);
        Assert.AreEqual(1, first.Current.SuccessStreak);
        Assert.AreEqual(0, first.Current.FailureCount);
        Assert.AreEqual(0.45, first.Current.Difficulty, 0.0001);
        Assert.IsGreaterThan(first.Current.Difficulty, slow.Current.Difficulty);
        Assert.IsLessThan(first.ScheduledDelay, slow.ScheduledDelay);
    }

    [TestMethod]
    public void AgainResetsSuccessAndSchedulesAShortRetry()
    {
        var schedule = Schedule(ReviewItemKind.Phrase, "de.task.cafe.order-one-item", Now) with
        {
            SuccessStreak = 4,
            FailureCount = 2,
            Difficulty = 0.4,
        };

        var decision = ReviewScheduler.Record(
            schedule,
            ReviewRating.Again,
            TimeSpan.FromSeconds(5),
            Now,
            Guid.NewGuid(),
            ReviewConfiguration.Default);

        Assert.AreEqual(0, decision.Current.SuccessStreak);
        Assert.AreEqual(3, decision.Current.FailureCount);
        Assert.AreEqual(ReviewConfiguration.Default.AgainDelay, decision.ScheduledDelay);
        Assert.AreEqual(Now + ReviewConfiguration.Default.AgainDelay, decision.Current.DueAt);
    }

    [TestMethod]
    public void SchedulerRejectsEarlyOrInvalidEvidence()
    {
        var schedule = Schedule(ReviewItemKind.Concept, ConceptId.Value, Now.AddMinutes(1));

        var early = Assert.ThrowsExactly<CurriculumValidationException>(() => ReviewScheduler.Record(
            schedule,
            ReviewRating.Good,
            TimeSpan.FromSeconds(2),
            Now,
            Guid.NewGuid(),
            ReviewConfiguration.Default));
        StringAssert.Contains(early.Message, "before it is due");

        Assert.ThrowsExactly<CurriculumValidationException>(() => ReviewScheduler.Record(
            schedule with { DueAt = Now },
            ReviewRating.Good,
            TimeSpan.FromSeconds(-1),
            Now,
            Guid.NewGuid(),
            ReviewConfiguration.Default));
    }

    [TestMethod]
    public void LearningEvidenceCreatesOnlyStableMvpReviewItems()
    {
        var taskAttempt = TaskAttempt();
        var tasks = new TaskHistory(
            [taskAttempt],
            [new ReviewHandoff(
                Guid.NewGuid(),
                taskAttempt.Id,
                ConceptId,
                Now,
                ["de.error.accusative-masculine"])]);
        var curriculum = CurriculumHistory.Empty with
        {
            Progress = [new ConceptProgress(
                ConceptId,
                ConceptProgressState.ProvisionallyMastered,
                3,
                Now,
                Now.AddDays(2),
                1,
                1)],
            Attempts = [ConceptAttempt(communicativeSuccess: true)],
        };
        var pronunciation = new PronunciationHistory([PronunciationAttempt()]);

        var synchronized = ReviewHistorySynchronizer.Synchronize(
            ReviewHistory.Empty,
            curriculum,
            tasks,
            pronunciation,
            ReviewConfiguration.Default);
        var repeated = ReviewHistorySynchronizer.Synchronize(
            synchronized,
            curriculum,
            tasks,
            pronunciation,
            ReviewConfiguration.Default);

        Assert.HasCount(4, synchronized.Schedules);
        CollectionAssert.AreEquivalent(
            new[]
            {
                ReviewItemKind.Phrase,
                ReviewItemKind.Concept,
                ReviewItemKind.PronunciationTarget,
                ReviewItemKind.RecurringError,
            },
            synchronized.Schedules.Select(item => item.Kind).ToArray());
        CollectionAssert.AreEqual(
            synchronized.Schedules.ToArray(),
            repeated.Schedules.ToArray());
        Assert.IsEmpty(synchronized.Attempts);
    }

    [TestMethod]
    public void QueueSortsDueBeforeUpcomingWithoutMutatingHistory()
    {
        var history = new ReviewHistory(
            [
                Schedule(ReviewItemKind.Phrase, "de.task.cafe.order-one-item", Now.AddDays(1)),
                Schedule(ReviewItemKind.Concept, ConceptId.Value, Now.AddHours(-2)),
                Schedule(ReviewItemKind.PronunciationTarget, "de.utterance.order", Now.AddHours(-1)),
            ],
            []);

        var queue = ReviewQueue.Build(history, Now);

        Assert.HasCount(2, queue.Due);
        Assert.AreEqual(ReviewItemKind.Concept, queue.Due[0].Kind);
        Assert.AreEqual(ReviewItemKind.PronunciationTarget, queue.Due[1].Kind);
        Assert.HasCount(1, queue.Upcoming);
        Assert.HasCount(3, history.Schedules);
    }

    [TestMethod]
    public void SuccessfulDelayedRecallAdvancesACommunicativelyProvenConcept()
    {
        var priorAttempt = ConceptAttempt(communicativeSuccess: true);
        var curriculum = CurriculumHistory.Empty with
        {
            Progress = [new ConceptProgress(
                ConceptId,
                ConceptProgressState.ReviewDue,
                3,
                Now.AddDays(-2),
                Now,
                0,
                1)],
            Attempts = [priorAttempt],
        };
        var schedule = Schedule(ReviewItemKind.Concept, ConceptId.Value, Now);
        var review = ReviewScheduler.Record(
            schedule,
            ReviewRating.Good,
            TimeSpan.FromSeconds(3),
            Now,
            Guid.NewGuid(),
            ReviewConfiguration.Default);

        var updated = ReviewProgression.Apply(curriculum, Graph(), review);

        Assert.AreEqual(ConceptProgressState.Mastered, updated.Progress.Single().State);
        Assert.AreEqual(4, updated.Progress.Single().AttemptCount);
        Assert.AreEqual(0.85, updated.Attempts.Last().Evidence.DelayedRecall);
        Assert.IsTrue(updated.Attempts.Last().Evidence.CommunicativeSuccess);
    }

    [TestMethod]
    public void WeakRecallReturnsADueConceptToPractice()
    {
        var curriculum = CurriculumHistory.Empty with
        {
            Progress = [new ConceptProgress(
                ConceptId,
                ConceptProgressState.ReviewDue,
                3,
                Now.AddDays(-2),
                Now,
                0,
                1)],
            Attempts = [ConceptAttempt(communicativeSuccess: true)],
        };
        var review = ReviewScheduler.Record(
            Schedule(ReviewItemKind.Concept, ConceptId.Value, Now),
            ReviewRating.Again,
            TimeSpan.FromSeconds(3),
            Now,
            Guid.NewGuid(),
            ReviewConfiguration.Default);

        var updated = ReviewProgression.Apply(curriculum, Graph(), review);

        Assert.AreEqual(ConceptProgressState.Practicing, updated.Progress.Single().State);
        Assert.IsNull(updated.Progress.Single().ReviewDueAt);
    }

    [TestMethod]
    public void ProgressLeadsWithCapabilityInsteadOfActivityCurrency()
    {
        var queue = new ReviewQueue(
            [Schedule(ReviewItemKind.Concept, ConceptId.Value, Now)],
            []);
        var overview = LearningProgressBuilder.Build(
            [new CapabilityDefinition(
                "cafe-order",
                "de.task.cafe.order-one-item",
                "Order at a café",
                "Request one item and respond to the server.")],
            CurriculumHistory.Empty,
            new TaskHistory([TaskAttempt()], []),
            PronunciationHistory.Empty,
            queue,
            Now);
        var plan = TodayPlanner.Build(overview);

        Assert.AreEqual(CapabilityStatus.Demonstrated, overview.Capabilities.Single().Status);
        Assert.AreEqual(TodayAction.Review, plan.PrimaryAction);
        Assert.AreEqual(1, overview.DueReviewCount);
    }

    [TestMethod]
    public void ValidatorRejectsOrphanAttemptsAndImpossibleSchedules()
    {
        var schedule = Schedule(ReviewItemKind.Concept, ConceptId.Value, Now);
        var orphan = new ReviewAttempt(
            Guid.NewGuid(),
            ReviewItemId.Create(ReviewItemKind.Concept, "de.other"),
            Now,
            ReviewRating.Good,
            TimeSpan.FromSeconds(2),
            ReviewConfiguration.Default.Version);

        var exception = Assert.ThrowsExactly<CurriculumValidationException>(() =>
            ReviewHistoryValidator.Validate(new ReviewHistory([schedule], [orphan])));
        StringAssert.Contains(exception.Message, "is invalid");

        Assert.ThrowsExactly<CurriculumValidationException>(() =>
            ReviewHistoryValidator.Validate(new ReviewHistory(
                [schedule with { Difficulty = double.NaN }],
                [])));
    }

    private static ReviewSchedule Schedule(
        ReviewItemKind kind,
        string targetId,
        DateTimeOffset dueAt) =>
        new(
            ReviewItemId.Create(kind, targetId),
            kind,
            targetId,
            ContentVersion,
            Now.AddDays(-2),
            LastSeenAt: null,
            dueAt,
            SuccessStreak: 0,
            FailureCount: 0,
            Difficulty: 0.5,
            RecentLatency: null,
            ReviewConfiguration.Default.Version);

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

    private static ConceptAttempt ConceptAttempt(bool communicativeSuccess) =>
        new(
            Guid.NewGuid(),
            ConceptId,
            Now.AddDays(-2),
            new LearningEvidence(
                communicativeSuccess,
                LinguisticAccuracy: 0.9,
                Fluency: null,
                Pronunciation: null,
                TargetConceptPerformance: 0.9,
                Comprehension: null,
                DelayedRecall: null),
            ContentVersion,
            ProgressionConfiguration.Default.Version,
            ConceptSelectionConfiguration.Default.Version,
            SelectedBridge: null);

    private static TaskAttempt TaskAttempt() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "de.task.cafe.order-one-item",
            Now.AddMinutes(-2),
            Now,
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

    private static PronunciationAttempt PronunciationAttempt() =>
        new(
            Guid.NewGuid(),
            "de.utterance.order",
            Now,
            new PronunciationEvidence(
                PronunciationAssessmentOutcome.Intelligible,
                0.9,
                ExpectedWordCount: 5,
                RecognizedWordCount: 5,
                MatchedWordCount: 5,
                TimeSpan.FromSeconds(3),
                "fixture-recognizer-v1",
                TranscriptPronunciationAssessmentProvider.Version),
            ContentVersion.Value);
}
