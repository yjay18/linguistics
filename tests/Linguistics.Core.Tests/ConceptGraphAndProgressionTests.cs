using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;

namespace Linguistics.Core.Tests;

[TestClass]
public sealed class ConceptGraphAndProgressionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void CurriculumIdentifiersNormalizeAndRejectPaths()
    {
        Assert.AreEqual("de.fixture.start", new ConceptId(" DE.Fixture.Start ").Value);
        Assert.ThrowsExactly<ArgumentException>(() => new ConceptId("../fixture"));
        Assert.ThrowsExactly<ArgumentException>(() => new VersionId("version/1"));
    }

    [TestMethod]
    public void GraphRejectsDuplicateConcepts()
    {
        var node = Node("fixture.start");

        var exception = Assert.ThrowsExactly<CurriculumValidationException>(
            () => new ConceptGraph([node, node]));

        StringAssert.Contains(exception.Message, "appears more than once");
    }

    [TestMethod]
    public void GraphRejectsMissingPrerequisites()
    {
        var exception = Assert.ThrowsExactly<CurriculumValidationException>(
            () => new ConceptGraph([
                Node("fixture.next", [new ConceptId("fixture.missing")]),
            ]));

        StringAssert.Contains(exception.Message, "missing prerequisite");
    }

    [TestMethod]
    public void GraphRejectsCycles()
    {
        var exception = Assert.ThrowsExactly<CurriculumValidationException>(
            () => new ConceptGraph([
                Node("fixture.one", [new ConceptId("fixture.two")]),
                Node("fixture.two", [new ConceptId("fixture.one")]),
            ]));

        StringAssert.Contains(exception.Message, "contains a cycle");
    }

    [TestMethod]
    public void GraphRejectsCrossTargetPrerequisites()
    {
        var german = Node("fixture.de");
        var otherTarget = Node(
            "fixture.other",
            [german.Id]) with
        {
            TargetLanguage = new LanguageCode("fr"),
        };

        var exception = Assert.ThrowsExactly<CurriculumValidationException>(
            () => new ConceptGraph([german, otherTarget]));

        StringAssert.Contains(exception.Message, "another target language");
    }

    [TestMethod]
    public void ReadinessRequiresEveryPrerequisiteToBeProvisionallyMasteredOrMastered()
    {
        var graph = new ConceptGraph([
            Node("fixture.one"),
            Node("fixture.two"),
            Node("fixture.next", [new ConceptId("fixture.one"), new ConceptId("fixture.two")]),
        ]);
        var states = new Dictionary<ConceptId, ConceptProgressState>
        {
            [new ConceptId("fixture.one")] = ConceptProgressState.ProvisionallyMastered,
            [new ConceptId("fixture.two")] = ConceptProgressState.Practicing,
        };

        Assert.IsFalse(graph.IsReady(new ConceptId("fixture.next"), states));
        states[new ConceptId("fixture.two")] = ConceptProgressState.Mastered;
        Assert.IsTrue(graph.IsReady(new ConceptId("fixture.next"), states));
    }

    [TestMethod]
    public void ProgressionFollowsTheConfiguredForwardSequence()
    {
        var id = new ConceptId("fixture.start");
        var progress = ConceptProgress.Locked(id);

        progress = Advance(progress, prerequisitesSatisfied: true).Current;
        Assert.AreEqual(ConceptProgressState.Available, progress.State);

        progress = Advance(progress, attempt: Attempt(id, targetPerformance: 0.4)).Current;
        Assert.AreEqual(ConceptProgressState.Introduced, progress.State);

        progress = Advance(progress, attempt: Attempt(id, targetPerformance: 0.5)).Current;
        Assert.AreEqual(ConceptProgressState.Practicing, progress.State);

        progress = Advance(progress, attempt: Attempt(id, targetPerformance: 0.9)).Current;
        Assert.AreEqual(ConceptProgressState.ProvisionallyMastered, progress.State);
        Assert.AreEqual(Now + ProgressionConfiguration.Default.InitialReviewDelay, progress.ReviewDueAt);

        progress = Advance(
            progress,
            now: progress.ReviewDueAt!.Value).Current;
        Assert.AreEqual(ConceptProgressState.ReviewDue, progress.State);

        progress = Advance(
            progress,
            attempt: Attempt(id, targetPerformance: 0.9, delayedRecall: 0.9),
            now: progress.ReviewDueAt!.Value).Current;
        Assert.AreEqual(ConceptProgressState.Mastered, progress.State);
        Assert.AreEqual(
            Now + ProgressionConfiguration.Default.InitialReviewDelay +
            ProgressionConfiguration.Default.MasteryReviewDelay,
            progress.ReviewDueAt);
        Assert.AreEqual(4, progress.AttemptCount);
    }

    [TestMethod]
    [DataRow(ConceptProgressState.ProvisionallyMastered)]
    [DataRow(ConceptProgressState.ReviewDue)]
    [DataRow(ConceptProgressState.Mastered)]
    public void WeakTargetEvidenceReturnsAdvancedStatesToPractice(ConceptProgressState state)
    {
        var id = new ConceptId("fixture.start");
        var due = state == ConceptProgressState.ReviewDue ? Now : Now.AddDays(1);
        var progress = new ConceptProgress(id, state, 3, Now.AddDays(-1), due, 1, 1);

        var decision = Advance(progress, attempt: Attempt(id, targetPerformance: 0.2));

        Assert.AreEqual(ConceptProgressState.Practicing, decision.Current.State);
        Assert.AreEqual(ProgressionReason.EvidenceRegressed, decision.Reason);
        Assert.IsNull(decision.Current.ReviewDueAt);
    }

    [TestMethod]
    public void MasteredConceptBecomesReviewDueWhenItsClockSaysSo()
    {
        var progress = new ConceptProgress(
            new ConceptId("fixture.start"),
            ConceptProgressState.Mastered,
            5,
            Now.AddDays(-14),
            Now,
            0,
            0);

        var decision = Advance(progress);

        Assert.AreEqual(ConceptProgressState.ReviewDue, decision.Current.State);
        Assert.AreEqual(ProgressionReason.ReviewBecameDue, decision.Reason);
    }

    [TestMethod]
    public void ProgressionRejectsMismatchedAndFutureAttempts()
    {
        var progress = new ConceptProgress(
            new ConceptId("fixture.start"),
            ConceptProgressState.Available,
            0,
            null,
            null,
            0,
            0);

        var mismatch = Assert.ThrowsExactly<CurriculumValidationException>(() =>
            Advance(progress, attempt: Attempt(new ConceptId("fixture.other"), 0.5)));
        StringAssert.Contains(mismatch.Message, "does not match");

        var future = Assert.ThrowsExactly<CurriculumValidationException>(() =>
            Advance(progress, attempt: Attempt(progress.ConceptId, 0.5, occurredAt: Now.AddMinutes(1))));
        StringAssert.Contains(future.Message, "future attempt");
    }

    [TestMethod]
    public void AttemptCannotSkipAvailabilityOrDueReviewTransitions()
    {
        var id = new ConceptId("fixture.start");
        var locked = ConceptProgress.Locked(id);
        var lockedException = Assert.ThrowsExactly<CurriculumValidationException>(() =>
            Advance(locked, prerequisitesSatisfied: true, attempt: Attempt(id, 0.8)));
        StringAssert.Contains(lockedException.Message, "Refresh availability");

        var due = new ConceptProgress(
            id,
            ConceptProgressState.ProvisionallyMastered,
            3,
            Now.AddDays(-2),
            Now,
            0,
            0);
        var dueException = Assert.ThrowsExactly<CurriculumValidationException>(() =>
            Advance(due, attempt: Attempt(id, 0.9, delayedRecall: 0.9)));
        StringAssert.Contains(dueException.Message, "due-review state");
    }

    [TestMethod]
    public void AllowedTransitionTableRejectsEveryUndocumentedStateChange()
    {
        var allowed = new HashSet<(ConceptProgressState From, ConceptProgressState To)>
        {
            (ConceptProgressState.Locked, ConceptProgressState.Available),
            (ConceptProgressState.Available, ConceptProgressState.Introduced),
            (ConceptProgressState.Introduced, ConceptProgressState.Practicing),
            (ConceptProgressState.Practicing, ConceptProgressState.ProvisionallyMastered),
            (ConceptProgressState.ProvisionallyMastered, ConceptProgressState.ReviewDue),
            (ConceptProgressState.ProvisionallyMastered, ConceptProgressState.Practicing),
            (ConceptProgressState.ReviewDue, ConceptProgressState.Mastered),
            (ConceptProgressState.ReviewDue, ConceptProgressState.Practicing),
            (ConceptProgressState.Mastered, ConceptProgressState.ReviewDue),
            (ConceptProgressState.Mastered, ConceptProgressState.Practicing),
        };

        foreach (var from in Enum.GetValues<ConceptProgressState>())
        {
            foreach (var to in Enum.GetValues<ConceptProgressState>())
            {
                Assert.AreEqual(
                    from == to || allowed.Contains((from, to)),
                    ConceptProgression.IsAllowedTransition(from, to),
                    $"Unexpected transition result for {from} -> {to}.");
            }
        }
    }

    [TestMethod]
    public void HistoryKeepsSeparateEvidenceAndRejectsInvalidValues()
    {
        var id = new ConceptId("fixture.start");
        var progress = new ConceptProgress(id, ConceptProgressState.Practicing, 1, Now, null, 0, 0);
        var attempt = Attempt(id, 0.8) with
        {
            Evidence = new LearningEvidence(
                CommunicativeSuccess: true,
                LinguisticAccuracy: 0.7,
                Fluency: 0.6,
                Pronunciation: null,
                TargetConceptPerformance: 0.8,
                Comprehension: 0.9,
                DelayedRecall: null),
            SelectedBridge = new SelectedBridgeReference(
                new TransferMappingId("fixture.bridge"),
                new VersionId("mapping-v1"),
                TransferRoutingConfiguration.Default.Version,
                0.75),
        };
        var history = new CurriculumHistory(
            [progress],
            [attempt],
            ProgressionConfiguration.Default.Version,
            ConceptSelectionConfiguration.Default.Version);

        CurriculumHistoryValidator.Validate(history);
        Assert.AreEqual(0.7, history.Attempts[0].Evidence.LinguisticAccuracy);
        Assert.IsNull(history.Attempts[0].Evidence.Pronunciation);

        var invalid = history with
        {
            Attempts = [attempt with
            {
                Evidence = attempt.Evidence with { Fluency = 1.1 },
            }],
        };
        var exception = Assert.ThrowsExactly<CurriculumValidationException>(
            () => CurriculumHistoryValidator.Validate(invalid));
        StringAssert.Contains(exception.Message, "outside 0 to 1");
    }

    [TestMethod]
    public void HistoryRejectsMissingEntriesAndInvalidReviewDates()
    {
        var invalidProgress = new ConceptProgress(
            new ConceptId("fixture.start"),
            ConceptProgressState.Mastered,
            1,
            Now,
            ReviewDueAt: null,
            0,
            0);
        var history = new CurriculumHistory(
            [invalidProgress, null!],
            [],
            ProgressionConfiguration.Default.Version,
            ConceptSelectionConfiguration.Default.Version);

        var exception = Assert.ThrowsExactly<CurriculumValidationException>(
            () => CurriculumHistoryValidator.Validate(history));

        StringAssert.Contains(exception.Message, "progress entry is missing");
        StringAssert.Contains(exception.Message, "invalid review date");
    }

    private static ProgressionDecision Advance(
        ConceptProgress progress,
        bool prerequisitesSatisfied = false,
        ConceptAttempt? attempt = null,
        DateTimeOffset? now = null) =>
        ConceptProgression.Advance(
            progress,
            prerequisitesSatisfied,
            attempt,
            now ?? Now,
            ProgressionConfiguration.Default);

    private static ConceptAttempt Attempt(
        ConceptId id,
        double targetPerformance,
        double? delayedRecall = null,
        DateTimeOffset? occurredAt = null) =>
        new(
            Guid.NewGuid(),
            id,
            occurredAt ?? Now,
            new LearningEvidence(
                CommunicativeSuccess: true,
                LinguisticAccuracy: null,
                Fluency: null,
                Pronunciation: null,
                TargetConceptPerformance: targetPerformance,
                Comprehension: null,
                DelayedRecall: delayedRecall),
            new VersionId("fixture-content-v1"),
            ProgressionConfiguration.Default.Version,
            ConceptSelectionConfiguration.Default.Version,
            SelectedBridge: null);

    private static ConceptNode Node(
        string id,
        IReadOnlyList<ConceptId>? prerequisites = null) =>
        new(
            new ConceptId(id),
            new LanguageCode("de"),
            ConceptType.Grammatical,
            $"Synthetic {id}",
            "Synthetic developer fixture without a linguistic claim.",
            Cefr: null,
            prerequisites ?? [],
            ["Complete the synthetic fixture."],
            [],
            ["fixture"],
            new VersionId("fixture-content-v1"));
}
