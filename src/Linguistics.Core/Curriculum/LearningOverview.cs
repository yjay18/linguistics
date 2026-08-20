namespace Linguistics.Core.Curriculum;

public static class ReviewProgression
{
    public static CurriculumHistory Apply(
        CurriculumHistory history,
        ConceptGraph graph,
        ReviewDecision review)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(review);
        CurriculumHistoryValidator.Validate(history);

        if (review.Current.Kind != ReviewItemKind.Concept)
        {
            return history;
        }

        var conceptId = new ConceptId(review.Current.TargetId);
        _ = graph.Get(conceptId);
        var progress = history.Progress.SingleOrDefault(item => item.ConceptId == conceptId)
            ?? throw new CurriculumValidationException(
                [$"Review concept '{conceptId}' has no progress record."]);
        var now = review.Attempt.OccurredAt;
        var states = history.Progress.ToDictionary(item => item.ConceptId, item => item.State);
        var prerequisitesSatisfied = graph.IsReady(conceptId, states);

        if (progress.State == ConceptProgressState.Locked ||
            progress.State is ConceptProgressState.ProvisionallyMastered or ConceptProgressState.Mastered &&
            progress.ReviewDueAt <= now)
        {
            progress = ConceptProgression.Advance(
                progress,
                prerequisitesSatisfied,
                attempt: null,
                now,
                ProgressionConfiguration.Default).Current;
        }

        if (progress.State == ConceptProgressState.Locked)
        {
            throw new CurriculumValidationException(
                [$"Review concept '{conceptId}' is still locked by its prerequisites."]);
        }

        var performance = review.Attempt.Rating switch
        {
            ReviewRating.Again => 0.2,
            ReviewRating.Hard => 0.6,
            ReviewRating.Good => 0.85,
            ReviewRating.Easy => 1,
            _ => throw new InvalidOperationException("The validated review rating is unavailable."),
        };
        var priorCommunicativeSuccess = history.Attempts.Any(item =>
            item.ConceptId == conceptId && item.Evidence.CommunicativeSuccess == true);
        var attempt = new ConceptAttempt(
            review.Attempt.Id,
            conceptId,
            now,
            new LearningEvidence(
                CommunicativeSuccess: priorCommunicativeSuccess ? true : null,
                LinguisticAccuracy: null,
                Fluency: null,
                Pronunciation: null,
                TargetConceptPerformance: performance,
                Comprehension: null,
                DelayedRecall: performance),
            review.Current.ContentVersion,
            history.ProgressionConfigurationVersion,
            history.SelectionConfigurationVersion,
            SelectedBridge: null);
        var updatedProgress = ConceptProgression.Advance(
            progress,
            prerequisitesSatisfied,
            attempt,
            now,
            ProgressionConfiguration.Default).Current;
        var updated = history with
        {
            Progress = history.Progress
                .Where(item => item.ConceptId != conceptId)
                .Append(updatedProgress)
                .OrderBy(item => item.ConceptId.Value, StringComparer.Ordinal)
                .ToArray(),
            Attempts = history.Attempts
                .Append(attempt)
                .OrderBy(item => item.OccurredAt)
                .ThenBy(item => item.Id)
                .ToArray(),
        };
        CurriculumHistoryValidator.Validate(updated);
        return updated;
    }
}

public enum CapabilityStatus
{
    NotStarted,
    Practicing,
    Demonstrated,
}

public sealed record CapabilityDefinition(
    string Id,
    string TaskId,
    string Title,
    string Description);

public sealed record CapabilityProgress(
    CapabilityDefinition Definition,
    CapabilityStatus Status,
    int AttemptCount,
    DateTimeOffset? LastEvidenceAt,
    double? LatestTargetConceptPerformance);

public sealed record LearningProgressOverview(
    IReadOnlyList<CapabilityProgress> Capabilities,
    int PracticingConceptCount,
    int StrongConceptCount,
    int DueConceptCount,
    int DueReviewCount,
    int UpcomingReviewCount,
    int PronunciationPracticeCount);

public static class LearningProgressBuilder
{
    public static LearningProgressOverview Build(
        IReadOnlyList<CapabilityDefinition> capabilities,
        CurriculumHistory curriculum,
        TaskHistory tasks,
        Linguistics.Core.Speech.PronunciationHistory pronunciation,
        ReviewQueue reviewQueue,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(curriculum);
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(pronunciation);
        ArgumentNullException.ThrowIfNull(reviewQueue);
        CurriculumHistoryValidator.Validate(curriculum);
        TaskHistoryValidator.Validate(tasks);
        Linguistics.Core.Speech.PronunciationHistoryValidator.Validate(pronunciation);

        var capabilityProgress = capabilities
            .OrderBy(item => item.Id, StringComparer.Ordinal)
            .Select(definition =>
            {
                if (string.IsNullOrWhiteSpace(definition.Id) ||
                    string.IsNullOrWhiteSpace(definition.TaskId) ||
                    string.IsNullOrWhiteSpace(definition.Title) ||
                    string.IsNullOrWhiteSpace(definition.Description))
                {
                    throw new CurriculumValidationException(["A capability definition is invalid."]);
                }

                var attempts = tasks.Attempts
                    .Where(item => string.Equals(item.TaskId, definition.TaskId, StringComparison.Ordinal))
                    .OrderBy(item => item.CompletedAt)
                    .ThenBy(item => item.Id)
                    .ToArray();
                var latest = attempts.LastOrDefault();
                var latestSuccess = attempts.LastOrDefault(item => item.Evidence.CommunicativeSuccess == true);
                var status = latestSuccess is not null
                    ? CapabilityStatus.Demonstrated
                    : attempts.Length > 0
                        ? CapabilityStatus.Practicing
                        : CapabilityStatus.NotStarted;
                return new CapabilityProgress(
                    definition,
                    status,
                    attempts.Length,
                    (latestSuccess ?? latest)?.CompletedAt,
                    latest?.Evidence.TargetConceptPerformance);
            })
            .ToArray();

        return new LearningProgressOverview(
            capabilityProgress,
            PracticingConceptCount: curriculum.Progress.Count(item => item.State is
                ConceptProgressState.Introduced or ConceptProgressState.Practicing),
            StrongConceptCount: curriculum.Progress.Count(item => item.State is
                ConceptProgressState.ProvisionallyMastered or ConceptProgressState.Mastered),
            DueConceptCount: curriculum.Progress.Count(item =>
                item.State == ConceptProgressState.ReviewDue || item.ReviewDueAt <= now),
            DueReviewCount: reviewQueue.Due.Count,
            UpcomingReviewCount: reviewQueue.Upcoming.Count,
            PronunciationPracticeCount: pronunciation.Attempts.Count);
    }
}

public enum TodayAction
{
    Review,
    Scenario,
    Pronunciation,
}

public sealed record TodayPlan(
    TodayAction PrimaryAction,
    string Headline,
    string Explanation);

public static class TodayPlanner
{
    public static TodayPlan Build(LearningProgressOverview progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        if (progress.DueReviewCount > 0)
        {
            return new TodayPlan(
                TodayAction.Review,
                $"{progress.DueReviewCount} review item{(progress.DueReviewCount == 1 ? string.Empty : "s")} ready",
                "Retrieve what you learned before starting something new.");
        }

        if (progress.Capabilities.All(item => item.Status == CapabilityStatus.NotStarted))
        {
            return new TodayPlan(
                TodayAction.Scenario,
                "Handle your first café exchange",
                "Practice a complete local task with text; speech remains optional.");
        }

        if (progress.PronunciationPracticeCount == 0)
        {
            return new TodayPlan(
                TodayAction.Pronunciation,
                "Hear and rehearse the café phrase",
                "Use system playback, then speak locally or follow the complete text-only path.");
        }

        return new TodayPlan(
            TodayAction.Scenario,
            "Revisit the café when it feels useful",
            "There is nothing due. Repeat the real-world task without streak pressure.");
    }
}
