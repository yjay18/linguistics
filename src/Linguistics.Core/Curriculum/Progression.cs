namespace Linguistics.Core.Curriculum;

public enum ConceptProgressState
{
    Locked,
    Available,
    Introduced,
    Practicing,
    ProvisionallyMastered,
    ReviewDue,
    Mastered,
}

public sealed record LearningEvidence(
    bool? CommunicativeSuccess,
    double? LinguisticAccuracy,
    double? Fluency,
    double? Pronunciation,
    double? TargetConceptPerformance,
    double? Comprehension,
    double? DelayedRecall);

public sealed record SelectedBridgeReference(
    TransferMappingId MappingId,
    VersionId MappingVersion,
    VersionId RoutingConfigurationVersion,
    double Score);

public sealed record ConceptAttempt(
    Guid Id,
    ConceptId ConceptId,
    DateTimeOffset OccurredAt,
    LearningEvidence Evidence,
    VersionId ContentVersion,
    VersionId ProgressionConfigurationVersion,
    VersionId SelectionConfigurationVersion,
    SelectedBridgeReference? SelectedBridge);

public sealed record ConceptProgress(
    ConceptId ConceptId,
    ConceptProgressState State,
    int AttemptCount,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? ReviewDueAt,
    int RecurringErrorCount,
    int CognitiveLoad)
{
    public static ConceptProgress Locked(ConceptId id) =>
        new(id, ConceptProgressState.Locked, 0, null, null, 0, 0);
}

public sealed record CurriculumHistory(
    IReadOnlyList<ConceptProgress> Progress,
    IReadOnlyList<ConceptAttempt> Attempts,
    VersionId ProgressionConfigurationVersion,
    VersionId SelectionConfigurationVersion)
{
    public static CurriculumHistory Empty =>
        new([], [], ProgressionConfiguration.Default.Version, ConceptSelectionConfiguration.Default.Version);
}

public static class CurriculumHistoryValidator
{
    public static void Validate(CurriculumHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);

        var errors = new List<string>();
        if (history.Progress is null)
        {
            errors.Add("The concept-progress collection is missing.");
        }

        if (history.Attempts is null)
        {
            errors.Add("The concept-attempt collection is missing.");
        }

        if (string.IsNullOrWhiteSpace(history.ProgressionConfigurationVersion.Value))
        {
            errors.Add("The progression configuration version is missing.");
        }

        if (string.IsNullOrWhiteSpace(history.SelectionConfigurationVersion.Value))
        {
            errors.Add("The selection configuration version is missing.");
        }

        var progressItems = (history.Progress ?? []).OfType<ConceptProgress>().ToArray();
        if (history.Progress is not null && progressItems.Length != history.Progress.Count)
        {
            errors.Add("A concept-progress entry is missing.");
        }

        foreach (var duplicate in progressItems
                     .GroupBy(progress => progress.ConceptId)
                     .Where(group => group.Count() > 1))
        {
            errors.Add($"Concept progress for '{duplicate.Key}' appears more than once.");
        }

        foreach (var progress in progressItems)
        {
            ValidateProgress(progress, errors);
        }

        var attemptItems = (history.Attempts ?? []).OfType<ConceptAttempt>().ToArray();
        if (history.Attempts is not null && attemptItems.Length != history.Attempts.Count)
        {
            errors.Add("A concept-attempt entry is missing.");
        }

        foreach (var duplicate in attemptItems
                     .GroupBy(attempt => attempt.Id)
                     .Where(group => group.Count() > 1))
        {
            errors.Add($"Concept attempt '{duplicate.Key}' appears more than once.");
        }

        var progressIds = progressItems.Select(progress => progress.ConceptId).ToHashSet();
        foreach (var attempt in attemptItems)
        {
            CollectAttemptErrors(attempt, errors);
            if (!progressIds.Contains(attempt.ConceptId))
            {
                errors.Add($"Concept attempt '{attempt.Id}' has no matching progress record.");
            }
        }

        if (errors.Count > 0)
        {
            throw new CurriculumValidationException(errors);
        }
    }

    public static void ValidateAttempt(ConceptAttempt attempt)
    {
        ArgumentNullException.ThrowIfNull(attempt);

        var errors = new List<string>();
        CollectAttemptErrors(attempt, errors);
        if (errors.Count > 0)
        {
            throw new CurriculumValidationException(errors);
        }
    }

    private static void ValidateProgress(
        ConceptProgress progress,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(progress.ConceptId.Value))
        {
            errors.Add("A concept-progress ID is missing.");
        }

        if (!Enum.IsDefined(progress.State))
        {
            errors.Add($"Concept '{progress.ConceptId}' has an invalid progression state.");
        }

        if (progress.AttemptCount < 0 || progress.RecurringErrorCount < 0)
        {
            errors.Add($"Concept '{progress.ConceptId}' has a negative counter.");
        }

        if (progress.CognitiveLoad is < 0 or > 5)
        {
            errors.Add($"Concept '{progress.ConceptId}' has cognitive load outside 0 to 5.");
        }

        var requiresReviewDate = progress.State is
            ConceptProgressState.ProvisionallyMastered or
            ConceptProgressState.ReviewDue or
            ConceptProgressState.Mastered;
        if (requiresReviewDate != (progress.ReviewDueAt is not null))
        {
            errors.Add($"Concept '{progress.ConceptId}' has an invalid review date for {progress.State}.");
        }
    }

    private static void CollectAttemptErrors(
        ConceptAttempt attempt,
        ICollection<string> errors)
    {
        if (attempt.Id == Guid.Empty)
        {
            errors.Add("A concept-attempt ID is missing.");
        }

        if (string.IsNullOrWhiteSpace(attempt.ConceptId.Value))
        {
            errors.Add($"Concept attempt '{attempt.Id}' has no concept ID.");
        }

        if (attempt.OccurredAt == default)
        {
            errors.Add($"Concept attempt '{attempt.Id}' has no occurrence time.");
        }

        if (attempt.Evidence is null)
        {
            errors.Add($"Concept attempt '{attempt.Id}' has no evidence.");
        }
        else
        {
            ValidateEvidence(attempt.Id, attempt.Evidence, errors);
        }

        if (string.IsNullOrWhiteSpace(attempt.ContentVersion.Value) ||
            string.IsNullOrWhiteSpace(attempt.ProgressionConfigurationVersion.Value) ||
            string.IsNullOrWhiteSpace(attempt.SelectionConfigurationVersion.Value))
        {
            errors.Add($"Concept attempt '{attempt.Id}' is missing a content or configuration version.");
        }

        if (attempt.SelectedBridge is { } bridge &&
            (string.IsNullOrWhiteSpace(bridge.MappingId.Value) ||
             string.IsNullOrWhiteSpace(bridge.MappingVersion.Value) ||
             string.IsNullOrWhiteSpace(bridge.RoutingConfigurationVersion.Value) ||
             !IsUnitValue(bridge.Score)))
        {
            errors.Add($"Concept attempt '{attempt.Id}' has an invalid selected bridge.");
        }
    }

    private static void ValidateEvidence(
        Guid attemptId,
        LearningEvidence evidence,
        ICollection<string> errors)
    {
        var values = new[]
        {
            evidence.LinguisticAccuracy,
            evidence.Fluency,
            evidence.Pronunciation,
            evidence.TargetConceptPerformance,
            evidence.Comprehension,
            evidence.DelayedRecall,
        };

        if (evidence.CommunicativeSuccess is null && values.All(value => value is null))
        {
            errors.Add($"Concept attempt '{attemptId}' contains no measured evidence.");
        }

        if (values.Any(value => value is not null && !IsUnitValue(value.Value)))
        {
            errors.Add($"Concept attempt '{attemptId}' has evidence outside 0 to 1.");
        }
    }

    private static bool IsUnitValue(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value) && value is >= 0 and <= 1;
}

public sealed record ProgressionConfiguration(
    VersionId Version,
    double ProvisionalMasteryThreshold,
    double DelayedRecallMasteryThreshold,
    double RegressionThreshold,
    TimeSpan InitialReviewDelay,
    TimeSpan MasteryReviewDelay)
{
    public static ProgressionConfiguration Default { get; } = new(
        new VersionId("progression-v1"),
        ProvisionalMasteryThreshold: 0.8,
        DelayedRecallMasteryThreshold: 0.8,
        RegressionThreshold: 0.5,
        InitialReviewDelay: TimeSpan.FromDays(2),
        MasteryReviewDelay: TimeSpan.FromDays(14));

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Version.Value) ||
            !IsUnitValue(ProvisionalMasteryThreshold) ||
            !IsUnitValue(DelayedRecallMasteryThreshold) ||
            !IsUnitValue(RegressionThreshold) ||
            InitialReviewDelay <= TimeSpan.Zero ||
            MasteryReviewDelay <= TimeSpan.Zero)
        {
            throw new ArgumentException("The progression configuration is invalid.", nameof(ProgressionConfiguration));
        }
    }

    private static bool IsUnitValue(double value) => value is >= 0 and <= 1;
}

public enum ProgressionReason
{
    NoChange,
    PrerequisitesSatisfied,
    FirstAttempt,
    PracticeStarted,
    ProvisionalThresholdMet,
    ReviewBecameDue,
    DelayedRecallThresholdMet,
    EvidenceRegressed,
}

public sealed record ProgressionDecision(
    ConceptProgress Previous,
    ConceptProgress Current,
    ProgressionReason Reason);

public static class ConceptProgression
{
    public static ProgressionDecision Advance(
        ConceptProgress progress,
        bool prerequisitesSatisfied,
        ConceptAttempt? attempt,
        DateTimeOffset now,
        ProgressionConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();
        CurriculumHistoryValidator.Validate(new CurriculumHistory(
            [progress],
            [],
            configuration.Version,
            ConceptSelectionConfiguration.Default.Version));

        if (attempt is not null)
        {
            CurriculumHistoryValidator.ValidateAttempt(attempt);
            if (attempt.ConceptId != progress.ConceptId)
            {
                throw new CurriculumValidationException(
                    ["The attempt concept does not match the progress record."]);
            }

            if (attempt.OccurredAt > now)
            {
                throw new CurriculumValidationException(
                    ["A future attempt cannot change current progress."]);
            }

            if (progress.State == ConceptProgressState.Locked ||
                progress.State is ConceptProgressState.ProvisionallyMastered or ConceptProgressState.Mastered &&
                IsDue(progress, now))
            {
                throw new CurriculumValidationException(
                    ["Refresh availability or due-review state before applying an attempt."]);
            }
        }

        var nextState = progress.State;
        var reason = ProgressionReason.NoChange;
        switch (progress.State)
        {
            case ConceptProgressState.Locked when prerequisitesSatisfied:
                nextState = ConceptProgressState.Available;
                reason = ProgressionReason.PrerequisitesSatisfied;
                break;
            case ConceptProgressState.Available when attempt is not null:
                nextState = ConceptProgressState.Introduced;
                reason = ProgressionReason.FirstAttempt;
                break;
            case ConceptProgressState.Introduced when attempt is not null:
                nextState = ConceptProgressState.Practicing;
                reason = ProgressionReason.PracticeStarted;
                break;
            case ConceptProgressState.Practicing when IsProvisionalMastery(attempt, configuration):
                nextState = ConceptProgressState.ProvisionallyMastered;
                reason = ProgressionReason.ProvisionalThresholdMet;
                break;
            case ConceptProgressState.ProvisionallyMastered when IsRegression(attempt, configuration):
                nextState = ConceptProgressState.Practicing;
                reason = ProgressionReason.EvidenceRegressed;
                break;
            case ConceptProgressState.ProvisionallyMastered when IsDue(progress, now):
                nextState = ConceptProgressState.ReviewDue;
                reason = ProgressionReason.ReviewBecameDue;
                break;
            case ConceptProgressState.ReviewDue when IsMastery(attempt, configuration):
                nextState = ConceptProgressState.Mastered;
                reason = ProgressionReason.DelayedRecallThresholdMet;
                break;
            case ConceptProgressState.ReviewDue when attempt is not null:
                nextState = ConceptProgressState.Practicing;
                reason = ProgressionReason.EvidenceRegressed;
                break;
            case ConceptProgressState.Mastered when IsRegression(attempt, configuration):
                nextState = ConceptProgressState.Practicing;
                reason = ProgressionReason.EvidenceRegressed;
                break;
            case ConceptProgressState.Mastered when IsDue(progress, now):
                nextState = ConceptProgressState.ReviewDue;
                reason = ProgressionReason.ReviewBecameDue;
                break;
        }

        if (!IsAllowedTransition(progress.State, nextState))
        {
            throw new CurriculumValidationException(
                [$"Progress cannot transition from {progress.State} to {nextState}."]);
        }

        var current = progress with
        {
            State = nextState,
            AttemptCount = progress.AttemptCount + (attempt is null ? 0 : 1),
            LastAttemptAt = attempt?.OccurredAt ?? progress.LastAttemptAt,
            ReviewDueAt = ReviewDueAt(progress, nextState, attempt, now, configuration),
        };

        return new ProgressionDecision(progress, current, reason);
    }

    public static bool IsAllowedTransition(
        ConceptProgressState from,
        ConceptProgressState to) =>
        from == to || (from, to) switch
        {
            (ConceptProgressState.Locked, ConceptProgressState.Available) => true,
            (ConceptProgressState.Available, ConceptProgressState.Introduced) => true,
            (ConceptProgressState.Introduced, ConceptProgressState.Practicing) => true,
            (ConceptProgressState.Practicing, ConceptProgressState.ProvisionallyMastered) => true,
            (ConceptProgressState.ProvisionallyMastered, ConceptProgressState.ReviewDue) => true,
            (ConceptProgressState.ProvisionallyMastered, ConceptProgressState.Practicing) => true,
            (ConceptProgressState.ReviewDue, ConceptProgressState.Mastered) => true,
            (ConceptProgressState.ReviewDue, ConceptProgressState.Practicing) => true,
            (ConceptProgressState.Mastered, ConceptProgressState.ReviewDue) => true,
            (ConceptProgressState.Mastered, ConceptProgressState.Practicing) => true,
            _ => false,
        };

    private static bool IsProvisionalMastery(
        ConceptAttempt? attempt,
        ProgressionConfiguration configuration) =>
        attempt?.Evidence.CommunicativeSuccess == true &&
        attempt.Evidence.TargetConceptPerformance >= configuration.ProvisionalMasteryThreshold;

    private static bool IsMastery(
        ConceptAttempt? attempt,
        ProgressionConfiguration configuration) =>
        IsProvisionalMastery(attempt, configuration) &&
        attempt!.Evidence.DelayedRecall >= configuration.DelayedRecallMasteryThreshold;

    private static bool IsRegression(
        ConceptAttempt? attempt,
        ProgressionConfiguration configuration) =>
        attempt?.Evidence.TargetConceptPerformance is { } performance &&
        performance < configuration.RegressionThreshold;

    private static bool IsDue(ConceptProgress progress, DateTimeOffset now) =>
        progress.ReviewDueAt is { } due && due <= now;

    private static DateTimeOffset? ReviewDueAt(
        ConceptProgress previous,
        ConceptProgressState nextState,
        ConceptAttempt? attempt,
        DateTimeOffset now,
        ProgressionConfiguration configuration) => nextState switch
        {
            ConceptProgressState.ProvisionallyMastered
                when previous.State != ConceptProgressState.ProvisionallyMastered =>
                now + configuration.InitialReviewDelay,
            ConceptProgressState.Mastered
                when previous.State != ConceptProgressState.Mastered || attempt is not null =>
                now + configuration.MasteryReviewDelay,
            ConceptProgressState.Practicing => null,
            _ => previous.ReviewDueAt,
        };
}
