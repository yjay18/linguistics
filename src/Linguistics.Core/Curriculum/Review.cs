using System.Text.Json.Serialization;

namespace Linguistics.Core.Curriculum;

public readonly record struct ReviewItemId
{
    [JsonConstructor]
    public ReviewItemId(string value) => Value = CurriculumIdentifier.Normalize(value, nameof(value));

    public string Value { get; }

    public static ReviewItemId Create(ReviewItemKind kind, string targetId) =>
        new($"review.{kind.ToString().ToLowerInvariant()}.{targetId}");

    public override string ToString() => Value;
}

public enum ReviewItemKind
{
    Word,
    Phrase,
    Concept,
    ListeningContrast,
    PronunciationTarget,
    RecurringError,
}

public enum ReviewRating
{
    Again,
    Hard,
    Good,
    Easy,
}

public sealed record ReviewSchedule(
    ReviewItemId Id,
    ReviewItemKind Kind,
    string TargetId,
    VersionId ContentVersion,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt,
    DateTimeOffset DueAt,
    int SuccessStreak,
    int FailureCount,
    double Difficulty,
    TimeSpan? RecentLatency,
    VersionId ConfigurationVersion);

public sealed record ReviewAttempt(
    Guid Id,
    ReviewItemId ItemId,
    DateTimeOffset OccurredAt,
    ReviewRating Rating,
    TimeSpan ResponseLatency,
    VersionId ConfigurationVersion);

public sealed record ReviewHistory(
    IReadOnlyList<ReviewSchedule> Schedules,
    IReadOnlyList<ReviewAttempt> Attempts)
{
    public static ReviewHistory Empty => new([], []);
}

public sealed record ReviewConfiguration(
    VersionId Version,
    TimeSpan NewItemDelay,
    TimeSpan AgainDelay,
    TimeSpan HardDelay,
    TimeSpan GoodDelay,
    TimeSpan EasyDelay,
    TimeSpan MaximumDelay,
    TimeSpan SlowResponseThreshold)
{
    public static ReviewConfiguration Default { get; } = new(
        new VersionId("review-v1"),
        NewItemDelay: TimeSpan.FromDays(1),
        AgainDelay: TimeSpan.FromMinutes(10),
        HardDelay: TimeSpan.FromDays(1),
        GoodDelay: TimeSpan.FromDays(3),
        EasyDelay: TimeSpan.FromDays(7),
        MaximumDelay: TimeSpan.FromDays(60),
        SlowResponseThreshold: TimeSpan.FromSeconds(15));

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Version.Value) ||
            NewItemDelay <= TimeSpan.Zero ||
            AgainDelay <= TimeSpan.Zero ||
            HardDelay <= AgainDelay ||
            GoodDelay <= HardDelay ||
            EasyDelay <= GoodDelay ||
            MaximumDelay < EasyDelay ||
            SlowResponseThreshold <= TimeSpan.Zero)
        {
            throw new ArgumentException("The review configuration is invalid.", nameof(ReviewConfiguration));
        }
    }
}

public sealed record ReviewDecision(
    ReviewSchedule Previous,
    ReviewSchedule Current,
    ReviewAttempt Attempt,
    TimeSpan ScheduledDelay);

public static class ReviewScheduler
{
    public static ReviewDecision Record(
        ReviewSchedule schedule,
        ReviewRating rating,
        TimeSpan responseLatency,
        DateTimeOffset now,
        Guid attemptId,
        ReviewConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();
        ReviewHistoryValidator.Validate(new ReviewHistory([schedule], []));

        if (!Enum.IsDefined(rating))
        {
            throw new CurriculumValidationException(["The review rating is invalid."]);
        }

        if (attemptId == Guid.Empty || responseLatency < TimeSpan.Zero || responseLatency > TimeSpan.FromHours(1))
        {
            throw new CurriculumValidationException(["The review attempt is invalid."]);
        }

        if (now < schedule.CreatedAt || now < schedule.DueAt)
        {
            throw new CurriculumValidationException(["A review cannot be recorded before it is due."]);
        }

        var slowAdjustment = responseLatency > configuration.SlowResponseThreshold ? 0.05 : 0;
        var ratingAdjustment = rating switch
        {
            ReviewRating.Again => 0.15,
            ReviewRating.Hard => 0.05,
            ReviewRating.Good => -0.05,
            ReviewRating.Easy => -0.1,
            _ => 0,
        };
        var difficulty = Math.Clamp(schedule.Difficulty + ratingAdjustment + slowAdjustment, 0.1, 1);
        var successStreak = rating == ReviewRating.Again ? 0 : schedule.SuccessStreak + 1;
        var baseDelay = rating switch
        {
            ReviewRating.Again => configuration.AgainDelay,
            ReviewRating.Hard => configuration.HardDelay,
            ReviewRating.Good => configuration.GoodDelay,
            ReviewRating.Easy => configuration.EasyDelay,
            _ => throw new InvalidOperationException("The validated review rating is unavailable."),
        };
        var multiplier = rating == ReviewRating.Again
            ? 1
            : Math.Max(1, successStreak) * (1.5 - difficulty);
        var delay = TimeSpan.FromTicks(Math.Min(
            configuration.MaximumDelay.Ticks,
            checked((long)(baseDelay.Ticks * multiplier))));

        var attempt = new ReviewAttempt(
            attemptId,
            schedule.Id,
            now,
            rating,
            responseLatency,
            configuration.Version);
        var current = schedule with
        {
            LastSeenAt = now,
            DueAt = now + delay,
            SuccessStreak = successStreak,
            FailureCount = schedule.FailureCount + (rating == ReviewRating.Again ? 1 : 0),
            Difficulty = difficulty,
            RecentLatency = responseLatency,
            ConfigurationVersion = configuration.Version,
        };

        ReviewHistoryValidator.Validate(new ReviewHistory([current], [attempt]));
        return new ReviewDecision(schedule, current, attempt, delay);
    }
}

public static class ReviewHistorySynchronizer
{
    public static ReviewHistory Synchronize(
        ReviewHistory history,
        CurriculumHistory curriculum,
        TaskHistory tasks,
        Linguistics.Core.Speech.PronunciationHistory pronunciation,
        ReviewConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(curriculum);
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(pronunciation);
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();
        ReviewHistoryValidator.Validate(history);
        CurriculumHistoryValidator.Validate(curriculum);
        TaskHistoryValidator.Validate(tasks);
        Linguistics.Core.Speech.PronunciationHistoryValidator.Validate(pronunciation);

        var schedules = history.Schedules.ToDictionary(item => item.Id);
        var taskAttempts = tasks.Attempts.ToDictionary(item => item.Id);
        foreach (var handoff in tasks.ReviewHandoffs.OrderBy(item => item.CreatedAt).ThenBy(item => item.Id))
        {
            var taskAttempt = taskAttempts[handoff.TaskAttemptId];
            AddIfMissing(
                schedules,
                ReviewItemKind.Phrase,
                taskAttempt.TaskId,
                taskAttempt.ContentVersion,
                handoff.CreatedAt,
                handoff.CreatedAt + configuration.NewItemDelay,
                configuration.Version);
            AddIfMissing(
                schedules,
                ReviewItemKind.Concept,
                handoff.ConceptId.Value,
                taskAttempt.ContentVersion,
                handoff.CreatedAt,
                handoff.CreatedAt + configuration.NewItemDelay,
                configuration.Version);

            foreach (var errorRuleId in handoff.ErrorRuleIds.Order(StringComparer.Ordinal))
            {
                AddIfMissing(
                    schedules,
                    ReviewItemKind.RecurringError,
                    errorRuleId,
                    taskAttempt.ContentVersion,
                    handoff.CreatedAt,
                    handoff.CreatedAt + configuration.NewItemDelay,
                    configuration.Version);
            }
        }

        foreach (var progress in curriculum.Progress
                     .Where(item => item.ReviewDueAt is not null)
                     .OrderBy(item => item.ConceptId.Value, StringComparer.Ordinal))
        {
            var sourceAttempt = curriculum.Attempts
                .Where(item => item.ConceptId == progress.ConceptId)
                .OrderByDescending(item => item.OccurredAt)
                .ThenByDescending(item => item.Id)
                .FirstOrDefault();
            if (sourceAttempt is not null)
            {
                AddIfMissing(
                    schedules,
                    ReviewItemKind.Concept,
                    progress.ConceptId.Value,
                    sourceAttempt.ContentVersion,
                    sourceAttempt.OccurredAt,
                    progress.ReviewDueAt!.Value,
                    configuration.Version);
            }
        }

        foreach (var attempt in pronunciation.Attempts.OrderBy(item => item.OccurredAt).ThenBy(item => item.Id))
        {
            AddIfMissing(
                schedules,
                ReviewItemKind.PronunciationTarget,
                attempt.UtteranceId,
                new VersionId(attempt.ContentVersion),
                attempt.OccurredAt,
                attempt.OccurredAt + configuration.NewItemDelay,
                configuration.Version);
        }

        var synchronized = new ReviewHistory(
            schedules.Values.OrderBy(item => item.Id.Value, StringComparer.Ordinal).ToArray(),
            history.Attempts.OrderBy(item => item.OccurredAt).ThenBy(item => item.Id).ToArray());
        ReviewHistoryValidator.Validate(synchronized);
        return synchronized;
    }

    private static void AddIfMissing(
        IDictionary<ReviewItemId, ReviewSchedule> schedules,
        ReviewItemKind kind,
        string targetId,
        VersionId contentVersion,
        DateTimeOffset createdAt,
        DateTimeOffset dueAt,
        VersionId configurationVersion)
    {
        var id = ReviewItemId.Create(kind, targetId);
        if (schedules.TryGetValue(id, out var existing))
        {
            var lastEvidenceAt = existing.LastSeenAt ?? existing.CreatedAt;
            if (createdAt > lastEvidenceAt && dueAt < existing.DueAt)
            {
                schedules[id] = existing with
                {
                    ContentVersion = contentVersion,
                    DueAt = dueAt,
                    ConfigurationVersion = configurationVersion,
                };
            }

            return;
        }

        schedules[id] = new ReviewSchedule(
            id,
            kind,
            targetId,
            contentVersion,
            createdAt,
            LastSeenAt: null,
            dueAt,
            SuccessStreak: 0,
            FailureCount: 0,
            Difficulty: 0.5,
            RecentLatency: null,
            configurationVersion);
    }
}

public sealed record ReviewQueue(
    IReadOnlyList<ReviewSchedule> Due,
    IReadOnlyList<ReviewSchedule> Upcoming)
{
    public static ReviewQueue Build(ReviewHistory history, DateTimeOffset now)
    {
        ReviewHistoryValidator.Validate(history);
        var ordered = history.Schedules
            .OrderBy(item => item.DueAt)
            .ThenBy(item => item.Kind)
            .ThenBy(item => item.Id.Value, StringComparer.Ordinal)
            .ToArray();
        return new ReviewQueue(
            ordered.Where(item => item.DueAt <= now).ToArray(),
            ordered.Where(item => item.DueAt > now).ToArray());
    }
}

public static class ReviewHistoryValidator
{
    public static void Validate(ReviewHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);
        var errors = new List<string>();
        var schedules = (history.Schedules ?? []).OfType<ReviewSchedule>().ToArray();
        var attempts = (history.Attempts ?? []).OfType<ReviewAttempt>().ToArray();

        if (history.Schedules is null || schedules.Length != history.Schedules.Count)
        {
            errors.Add("A review schedule is missing.");
        }

        if (history.Attempts is null || attempts.Length != history.Attempts.Count)
        {
            errors.Add("A review attempt is missing.");
        }

        foreach (var duplicate in schedules.GroupBy(item => item.Id).Where(group => group.Count() > 1))
        {
            errors.Add($"Review schedule '{duplicate.Key}' appears more than once.");
        }

        foreach (var schedule in schedules)
        {
            if (!Enum.IsDefined(schedule.Kind) ||
                string.IsNullOrWhiteSpace(schedule.Id.Value) ||
                string.IsNullOrWhiteSpace(schedule.TargetId) ||
                schedule.TargetId.Length > 128 ||
                string.IsNullOrWhiteSpace(schedule.ContentVersion.Value) ||
                string.IsNullOrWhiteSpace(schedule.ConfigurationVersion.Value) ||
                schedule.CreatedAt == default ||
                schedule.DueAt < schedule.CreatedAt ||
                schedule.LastSeenAt is { } lastSeen && (lastSeen < schedule.CreatedAt || schedule.DueAt <= lastSeen) ||
                schedule.SuccessStreak < 0 ||
                schedule.FailureCount < 0 ||
                double.IsNaN(schedule.Difficulty) ||
                schedule.Difficulty is < 0.1 or > 1 ||
                schedule.RecentLatency is { } latency && (latency < TimeSpan.Zero || latency > TimeSpan.FromHours(1)))
            {
                errors.Add($"Review schedule '{schedule?.Id}' is invalid.");
            }
        }

        var scheduleIds = schedules.Select(item => item.Id).ToHashSet();
        foreach (var duplicate in attempts.GroupBy(item => item.Id).Where(group => group.Count() > 1))
        {
            errors.Add($"Review attempt '{duplicate.Key}' appears more than once.");
        }

        foreach (var attempt in attempts)
        {
            if (attempt.Id == Guid.Empty ||
                !scheduleIds.Contains(attempt.ItemId) ||
                attempt.OccurredAt == default ||
                !Enum.IsDefined(attempt.Rating) ||
                attempt.ResponseLatency < TimeSpan.Zero ||
                attempt.ResponseLatency > TimeSpan.FromHours(1) ||
                string.IsNullOrWhiteSpace(attempt.ConfigurationVersion.Value))
            {
                errors.Add($"Review attempt '{attempt?.Id}' is invalid.");
            }
        }

        if (errors.Count > 0)
        {
            throw new CurriculumValidationException(errors);
        }
    }
}
