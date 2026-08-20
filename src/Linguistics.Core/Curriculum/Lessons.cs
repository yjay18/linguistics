using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Linguistics.Core.Curriculum;

public sealed record ConceptSelectionConfiguration(
    VersionId Version,
    double ReviewUrgencyWeight,
    double PrerequisiteReadinessWeight,
    double RecurringErrorWeight,
    double TaskRelevanceWeight,
    double TransferOpportunityWeight,
    double CognitiveLoadPenaltyWeight)
{
    public static ConceptSelectionConfiguration Default { get; } = new(
        new VersionId("selection-v1"),
        ReviewUrgencyWeight: 10,
        PrerequisiteReadinessWeight: 1,
        RecurringErrorWeight: 1,
        TaskRelevanceWeight: 1,
        TransferOpportunityWeight: 1,
        CognitiveLoadPenaltyWeight: 1);

    public void Validate()
    {
        var weights = new[]
        {
            ReviewUrgencyWeight,
            PrerequisiteReadinessWeight,
            RecurringErrorWeight,
            TaskRelevanceWeight,
            TransferOpportunityWeight,
            CognitiveLoadPenaltyWeight,
        };

        if (string.IsNullOrWhiteSpace(Version.Value) ||
            weights.Any(weight => double.IsNaN(weight) || double.IsInfinity(weight) || weight < 0) ||
            ReviewUrgencyWeight <=
            PrerequisiteReadinessWeight + RecurringErrorWeight + TaskRelevanceWeight +
            TransferOpportunityWeight + CognitiveLoadPenaltyWeight)
        {
            throw new ArgumentException(
                "The concept-selection configuration is invalid or does not prioritize due review.",
                nameof(ConceptSelectionConfiguration));
        }
    }
}

public sealed record ConceptSelectionContext(
    DateTimeOffset Now,
    int Seed,
    IReadOnlySet<string> DesiredTaskTags,
    IReadOnlyDictionary<ConceptId, double> TransferOpportunityScores);

public sealed record ConceptScoreFactors(
    double ReviewUrgency,
    double PrerequisiteReadiness,
    double RecurringError,
    double TaskRelevance,
    double TransferOpportunity,
    double CognitiveLoadPenalty);

public sealed record ConceptCandidateScore(
    ConceptId ConceptId,
    ConceptProgressState State,
    ConceptScoreFactors Factors,
    double Total,
    ulong StableTieBreaker);

public enum ConceptSelectionReason
{
    NoCandidate,
    DueReview,
    ReadyConcept,
}

public sealed record ConceptSelectionExplanation(
    VersionId ConfigurationVersion,
    DateTimeOffset EvaluatedAt,
    int Seed,
    ConceptSelectionReason Reason,
    IReadOnlyList<ConceptCandidateScore> Candidates);

public sealed record ConceptSelectionResult(
    ConceptNode? SelectedConcept,
    ConceptSelectionExplanation Explanation);

public static class ConceptSelector
{
    public static ConceptSelectionResult Select(
        ConceptGraph graph,
        IReadOnlyList<ConceptProgress> progress,
        ConceptSelectionContext context,
        ConceptSelectionConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();
        CurriculumHistoryValidator.Validate(new CurriculumHistory(
            progress,
            [],
            ProgressionConfiguration.Default.Version,
            configuration.Version));

        if (context.DesiredTaskTags is null || context.TransferOpportunityScores is null)
        {
            throw new ArgumentException("Selection context collections are required.", nameof(context));
        }

        var duplicate = progress.GroupBy(item => item.ConceptId).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new CurriculumValidationException(
                [$"Concept progress for '{duplicate.Key}' appears more than once."]);
        }

        var byId = progress.ToDictionary(item => item.ConceptId);
        var states = progress.ToDictionary(item => item.ConceptId, item => item.State);
        var candidates = graph.Nodes
            .Where(node => IsCandidate(node, graph, byId, states, context.Now))
            .Select(node => Score(node, graph, byId, states, context, configuration))
            .OrderByDescending(candidate => candidate.Total)
            .ThenBy(candidate => candidate.StableTieBreaker)
            .ThenBy(candidate => candidate.ConceptId.Value, StringComparer.Ordinal)
            .ToArray();

        var selected = candidates.Length == 0 ? null : graph.Get(candidates[0].ConceptId);
        var reason = selected is null
            ? ConceptSelectionReason.NoCandidate
            : candidates[0].Factors.ReviewUrgency > 0
                ? ConceptSelectionReason.DueReview
                : ConceptSelectionReason.ReadyConcept;

        return new ConceptSelectionResult(
            selected,
            new ConceptSelectionExplanation(
                configuration.Version,
                context.Now,
                context.Seed,
                reason,
                candidates));
    }

    private static bool IsCandidate(
        ConceptNode node,
        ConceptGraph graph,
        IReadOnlyDictionary<ConceptId, ConceptProgress> progress,
        IReadOnlyDictionary<ConceptId, ConceptProgressState> states,
        DateTimeOffset now)
    {
        if (!progress.TryGetValue(node.Id, out var current))
        {
            return graph.IsReady(node.Id, states);
        }

        return current.State switch
        {
            ConceptProgressState.Locked => graph.IsReady(node.Id, states),
            ConceptProgressState.Available or
                ConceptProgressState.Introduced or
                ConceptProgressState.Practicing or
                ConceptProgressState.ReviewDue => true,
            ConceptProgressState.ProvisionallyMastered or ConceptProgressState.Mastered =>
                current.ReviewDueAt is { } due && due <= now,
            _ => false,
        };
    }

    private static ConceptCandidateScore Score(
        ConceptNode node,
        ConceptGraph graph,
        IReadOnlyDictionary<ConceptId, ConceptProgress> progress,
        IReadOnlyDictionary<ConceptId, ConceptProgressState> states,
        ConceptSelectionContext context,
        ConceptSelectionConfiguration configuration)
    {
        progress.TryGetValue(node.Id, out var current);
        var state = current?.State ?? ConceptProgressState.Locked;
        var reviewUrgency = ReviewUrgency(current, context.Now);
        var prerequisiteReadiness = graph.IsReady(node.Id, states) ? 1 : 0;
        var recurringError = Math.Min(current?.RecurringErrorCount ?? 0, 5) / 5d;
        var taskRelevance = context.DesiredTaskTags.Count > 0 &&
                            node.TaskTags.Any(context.DesiredTaskTags.Contains)
            ? 1
            : 0;
        var transferOpportunity = context.TransferOpportunityScores.TryGetValue(node.Id, out var score)
            ? ValidUnitScore(node.Id, score)
            : 0;
        var cognitiveLoadPenalty = Math.Min(current?.CognitiveLoad ?? 0, 5) / 5d;
        var factors = new ConceptScoreFactors(
            reviewUrgency,
            prerequisiteReadiness,
            recurringError,
            taskRelevance,
            transferOpportunity,
            cognitiveLoadPenalty);
        var total =
            factors.ReviewUrgency * configuration.ReviewUrgencyWeight +
            factors.PrerequisiteReadiness * configuration.PrerequisiteReadinessWeight +
            factors.RecurringError * configuration.RecurringErrorWeight +
            factors.TaskRelevance * configuration.TaskRelevanceWeight +
            factors.TransferOpportunity * configuration.TransferOpportunityWeight -
            factors.CognitiveLoadPenalty * configuration.CognitiveLoadPenaltyWeight;

        return new ConceptCandidateScore(
            node.Id,
            state,
            factors,
            total,
            StableTieBreaker(context.Seed, node.Id));
    }

    private static double ReviewUrgency(ConceptProgress? progress, DateTimeOffset now)
    {
        if (progress is null ||
            (progress.State != ConceptProgressState.ReviewDue &&
             !(progress.ReviewDueAt is { } due && due <= now)))
        {
            return 0;
        }

        var overdueDays = progress.ReviewDueAt is { } reviewDue
            ? Math.Max(0, (now - reviewDue).TotalDays)
            : 0;
        return 1 + Math.Min(overdueDays / 30, 1);
    }

    private static double ValidUnitScore(ConceptId id, double score)
    {
        if (double.IsNaN(score) || double.IsInfinity(score) || score is < 0 or > 1)
        {
            throw new CurriculumValidationException(
                [$"Transfer opportunity for '{id}' is outside 0 to 1."]);
        }

        return score;
    }

    private static ulong StableTieBreaker(int seed, ConceptId id)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{seed}:{id.Value}"));
        return BinaryPrimitives.ReadUInt64BigEndian(bytes);
    }
}

public enum LessonTaskType
{
    IntroduceAndPractice,
    ReviewAndRetrieve,
}

public enum LessonComponentKind
{
    RetrievalWarmUp,
    ComprehensibleInput,
    CommunicativeTask,
    Recap,
}

public sealed record LessonPlan(
    ConceptId ConceptId,
    LessonTaskType TaskType,
    IReadOnlyList<LessonComponentKind> Components,
    VersionId SelectionConfigurationVersion);

public static class LessonComposer
{
    public static LessonPlan Compose(ConceptSelectionResult selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        var concept = selection.SelectedConcept
            ?? throw new InvalidOperationException("A lesson cannot be composed without a selected concept.");
        var review = selection.Explanation.Reason == ConceptSelectionReason.DueReview;

        return new LessonPlan(
            concept.Id,
            review ? LessonTaskType.ReviewAndRetrieve : LessonTaskType.IntroduceAndPractice,
            review
                ? [
                    LessonComponentKind.RetrievalWarmUp,
                    LessonComponentKind.CommunicativeTask,
                    LessonComponentKind.Recap,
                ]
                : [
                    LessonComponentKind.ComprehensibleInput,
                    LessonComponentKind.CommunicativeTask,
                    LessonComponentKind.Recap,
                ],
            selection.Explanation.ConfigurationVersion);
    }
}
