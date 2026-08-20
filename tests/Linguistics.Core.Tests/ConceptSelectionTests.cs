using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;

namespace Linguistics.Core.Tests;

[TestClass]
public sealed class ConceptSelectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void DueReviewOutranksAReadyNewConcept()
    {
        var graph = Graph();
        var progress = new[]
        {
            Progress("fixture.review", ConceptProgressState.ReviewDue, due: Now.AddDays(-1)),
            Progress("fixture.ready", ConceptProgressState.Available),
        };

        var result = Select(graph, progress);

        Assert.AreEqual("fixture.review", result.SelectedConcept?.Id.Value);
        Assert.AreEqual(ConceptSelectionReason.DueReview, result.Explanation.Reason);
        Assert.IsGreaterThan(
            result.Explanation.Candidates[1].Total,
            result.Explanation.Candidates[0].Total);
    }

    [TestMethod]
    public void SelectorUsesEveryConfiguredFactorAndPenalty()
    {
        var graph = new ConceptGraph([
            Node("fixture.focus", taskTags: ["travel"]),
        ]);
        var progress = new[]
        {
            new ConceptProgress(
                new ConceptId("fixture.focus"),
                ConceptProgressState.Practicing,
                2,
                Now.AddDays(-1),
                null,
                RecurringErrorCount: 5,
                CognitiveLoad: 2),
        };
        var context = new ConceptSelectionContext(
            Now,
            Seed: 42,
            new HashSet<string>(["travel"], StringComparer.Ordinal),
            new Dictionary<ConceptId, double>
            {
                [new ConceptId("fixture.focus")] = 0.75,
            });

        var result = ConceptSelector.Select(
            graph,
            progress,
            context,
            ConceptSelectionConfiguration.Default);
        var factors = result.Explanation.Candidates.Single().Factors;

        Assert.AreEqual(0, factors.ReviewUrgency);
        Assert.AreEqual(1, factors.PrerequisiteReadiness);
        Assert.AreEqual(1, factors.RecurringError);
        Assert.AreEqual(1, factors.TaskRelevance);
        Assert.AreEqual(0.75, factors.TransferOpportunity);
        Assert.AreEqual(0.4, factors.CognitiveLoadPenalty);
    }

    [TestMethod]
    public void PrerequisitesHideLockedConceptUntilReady()
    {
        var graph = new ConceptGraph([
            Node("fixture.first"),
            Node("fixture.next", prerequisites: [new ConceptId("fixture.first")]),
        ]);

        var locked = Select(
            graph,
            [Progress("fixture.first", ConceptProgressState.Practicing)]);
        Assert.AreEqual("fixture.first", locked.SelectedConcept?.Id.Value);
        Assert.IsFalse(locked.Explanation.Candidates.Any(candidate =>
            candidate.ConceptId.Value == "fixture.next"));

        var ready = Select(
            graph,
            [Progress("fixture.first", ConceptProgressState.Mastered, due: Now.AddDays(10))]);
        Assert.AreEqual("fixture.next", ready.SelectedConcept?.Id.Value);
    }

    [TestMethod]
    public void IdenticalInputsReturnTheSameConceptTaskTypeAndExplanation()
    {
        var graph = Graph();
        var progress = new[]
        {
            Progress("fixture.ready", ConceptProgressState.Available),
            Progress("fixture.review", ConceptProgressState.Available),
        };

        var first = Select(graph, progress, seed: 97);
        var second = Select(graph, progress, seed: 97);
        var firstLesson = LessonComposer.Compose(first);
        var secondLesson = LessonComposer.Compose(second);

        Assert.AreEqual(first.SelectedConcept?.Id, second.SelectedConcept?.Id);
        Assert.AreEqual(firstLesson.TaskType, secondLesson.TaskType);
        CollectionAssert.AreEqual(firstLesson.Components.ToArray(), secondLesson.Components.ToArray());
        CollectionAssert.AreEqual(
            first.Explanation.Candidates.ToArray(),
            second.Explanation.Candidates.ToArray());
    }

    [TestMethod]
    public void SeededTieBreakIsStableAndDoesNotDependOnInputOrder()
    {
        var forward = new ConceptGraph([Node("fixture.one"), Node("fixture.two")]);
        var reverse = new ConceptGraph([Node("fixture.two"), Node("fixture.one")]);

        var first = Select(forward, [], seed: 123);
        var second = Select(reverse, [], seed: 123);

        Assert.AreEqual(first.SelectedConcept?.Id, second.SelectedConcept?.Id);
        Assert.AreEqual(
            first.Explanation.Candidates[0].StableTieBreaker,
            second.Explanation.Candidates[0].StableTieBreaker);
    }

    [TestMethod]
    public void FullyMasteredNotDueGraphReturnsNoCandidate()
    {
        var graph = new ConceptGraph([Node("fixture.done")]);
        var progress = new[]
        {
            Progress("fixture.done", ConceptProgressState.Mastered, due: Now.AddDays(10)),
        };

        var result = Select(graph, progress);

        Assert.IsNull(result.SelectedConcept);
        Assert.AreEqual(ConceptSelectionReason.NoCandidate, result.Explanation.Reason);
        Assert.ThrowsExactly<InvalidOperationException>(() => LessonComposer.Compose(result));
    }

    [TestMethod]
    public void SelectorRejectsDuplicateProgressAndInvalidTransferScores()
    {
        var graph = new ConceptGraph([Node("fixture.one")]);
        var progress = Progress("fixture.one", ConceptProgressState.Available);

        var duplicate = Assert.ThrowsExactly<CurriculumValidationException>(
            () => Select(graph, [progress, progress]));
        StringAssert.Contains(duplicate.Message, "appears more than once");

        var context = new ConceptSelectionContext(
            Now,
            1,
            new HashSet<string>(),
            new Dictionary<ConceptId, double>
            {
                [progress.ConceptId] = 1.1,
            });
        var invalidScore = Assert.ThrowsExactly<CurriculumValidationException>(() =>
            ConceptSelector.Select(
                graph,
                [progress],
                context,
                ConceptSelectionConfiguration.Default));
        StringAssert.Contains(invalidScore.Message, "outside 0 to 1");
    }

    [TestMethod]
    public void LessonComposerUsesOnlyApprovedMinimalComponents()
    {
        var newLesson = LessonComposer.Compose(Select(
            new ConceptGraph([Node("fixture.new")]),
            []));
        var reviewLesson = LessonComposer.Compose(Select(
            new ConceptGraph([Node("fixture.review")]),
            [Progress("fixture.review", ConceptProgressState.ReviewDue, Now)]));

        Assert.AreEqual(LessonTaskType.IntroduceAndPractice, newLesson.TaskType);
        CollectionAssert.AreEqual(
            new[]
            {
                LessonComponentKind.ComprehensibleInput,
                LessonComponentKind.CommunicativeTask,
                LessonComponentKind.Recap,
            },
            newLesson.Components.ToArray());
        Assert.AreEqual(LessonTaskType.ReviewAndRetrieve, reviewLesson.TaskType);
        CollectionAssert.AreEqual(
            new[]
            {
                LessonComponentKind.RetrievalWarmUp,
                LessonComponentKind.CommunicativeTask,
                LessonComponentKind.Recap,
            },
            reviewLesson.Components.ToArray());
    }

    private static ConceptSelectionResult Select(
        ConceptGraph graph,
        IReadOnlyList<ConceptProgress> progress,
        int seed = 42) =>
        ConceptSelector.Select(
            graph,
            progress,
            new ConceptSelectionContext(
                Now,
                seed,
                new HashSet<string>(StringComparer.Ordinal),
                new Dictionary<ConceptId, double>()),
            ConceptSelectionConfiguration.Default);

    private static ConceptGraph Graph() => new([
        Node("fixture.ready"),
        Node("fixture.review"),
    ]);

    private static ConceptProgress Progress(
        string id,
        ConceptProgressState state,
        DateTimeOffset? due = null) =>
        new(new ConceptId(id), state, 0, null, due, 0, 0);

    private static ConceptNode Node(
        string id,
        IReadOnlyList<ConceptId>? prerequisites = null,
        IReadOnlyList<string>? taskTags = null) =>
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
            taskTags ?? ["fixture"],
            new VersionId("fixture-content-v1"));
}
