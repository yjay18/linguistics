namespace Linguistics.Core.Curriculum;

public sealed record LessonProgress(
    string LessonId,
    ConceptId ConceptId,
    int VisitCount,
    int CompletedCount,
    DateTimeOffset FirstVisitedAt,
    DateTimeOffset LastVisitedAt,
    DateTimeOffset? CurrentStartedAt,
    DateTimeOffset? LastCompletedAt,
    int LastSlideIndex,
    int SlideCount,
    VersionId ContentVersion)
{
    public bool IsInProgress => CurrentStartedAt is not null;
}

public sealed record LessonHistory(IReadOnlyList<LessonProgress> Lessons)
{
    public static LessonHistory Empty { get; } = new([]);
}

public static class LessonHistoryValidator
{
    public const int MaximumLessons = 500;
    public const int MaximumVisitsPerLesson = 100_000;

    public static void Validate(LessonHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);

        var errors = new List<string>();
        if (history.Lessons is null)
        {
            errors.Add("The lesson progress collection is missing.");
        }

        var lessons = (history.Lessons ?? []).OfType<LessonProgress>().ToArray();
        if (history.Lessons is not null && lessons.Length != history.Lessons.Count)
        {
            errors.Add("A lesson progress entry is missing.");
        }

        if (lessons.Length > MaximumLessons)
        {
            errors.Add($"Lesson progress exceeds the {MaximumLessons} lesson limit.");
        }

        foreach (var duplicate in lessons
                     .GroupBy(lesson => lesson.LessonId, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            errors.Add($"Lesson progress for '{duplicate.Key}' appears more than once.");
        }

        foreach (var lesson in lessons)
        {
            CollectErrors(lesson, errors);
        }

        if (errors.Count > 0)
        {
            throw new CurriculumValidationException(errors);
        }
    }

    private static void CollectErrors(LessonProgress lesson, ICollection<string> errors)
    {
        try
        {
            _ = NormalizeId(lesson.LessonId);
        }
        catch (ArgumentException)
        {
            errors.Add("A lesson progress entry has an invalid lesson ID.");
        }

        if (string.IsNullOrWhiteSpace(lesson.ConceptId.Value))
        {
            errors.Add($"Lesson '{lesson.LessonId}' has no concept ID.");
        }

        if (lesson.VisitCount is < 1 or > MaximumVisitsPerLesson ||
            lesson.CompletedCount < 0 ||
            lesson.CompletedCount > lesson.VisitCount)
        {
            errors.Add($"Lesson '{lesson.LessonId}' has invalid visit counters.");
        }

        if (lesson.FirstVisitedAt == default || lesson.LastVisitedAt < lesson.FirstVisitedAt)
        {
            errors.Add($"Lesson '{lesson.LessonId}' has invalid visit times.");
        }

        if (lesson.CurrentStartedAt is { } started &&
            (started < lesson.FirstVisitedAt || started > lesson.LastVisitedAt))
        {
            errors.Add($"Lesson '{lesson.LessonId}' has an invalid current session time.");
        }

        if (lesson.LastCompletedAt is { } completed &&
            (completed < lesson.FirstVisitedAt || completed > lesson.LastVisitedAt))
        {
            errors.Add($"Lesson '{lesson.LessonId}' has an invalid completion time.");
        }

        if ((lesson.CompletedCount == 0) != (lesson.LastCompletedAt is null))
        {
            errors.Add($"Lesson '{lesson.LessonId}' has inconsistent completion history.");
        }

        if (lesson.SlideCount is < 1 or > 100 ||
            lesson.LastSlideIndex < 0 || lesson.LastSlideIndex >= lesson.SlideCount)
        {
            errors.Add($"Lesson '{lesson.LessonId}' has an invalid slide position.");
        }

        if (string.IsNullOrWhiteSpace(lesson.ContentVersion.Value))
        {
            errors.Add($"Lesson '{lesson.LessonId}' has no content version.");
        }
    }

    internal static string NormalizeId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length is < 2 or > 160 ||
            !normalized.All(character =>
                character is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '-' or '_'))
        {
            throw new ArgumentException(
                "Lesson identifiers may contain lowercase letters, digits, dots, hyphens, and underscores.",
                nameof(value));
        }

        return normalized;
    }
}

public static class LessonProgressTracker
{
    public static LessonHistory Begin(
        LessonHistory history,
        string lessonId,
        ConceptId conceptId,
        int slideCount,
        VersionId contentVersion,
        DateTimeOffset now)
    {
        LessonHistoryValidator.Validate(history);
        var normalizedId = LessonHistoryValidator.NormalizeId(lessonId);
        ValidateInput(conceptId, slideCount, contentVersion, now);

        var existing = history.Lessons.FirstOrDefault(lesson =>
            string.Equals(lesson.LessonId, normalizedId, StringComparison.Ordinal));
        if (existing is not null && existing.ConceptId != conceptId)
        {
            throw new CurriculumValidationException(
                [$"Lesson '{normalizedId}' cannot change its concept ID."]);
        }

        if (existing is not null && now < existing.LastVisitedAt)
        {
            throw new CurriculumValidationException(
                [$"Lesson '{normalizedId}' cannot be visited before its stored history."]);
        }

        var updated = existing is null
            ? new LessonProgress(
                normalizedId,
                conceptId,
                VisitCount: 1,
                CompletedCount: 0,
                FirstVisitedAt: now,
                LastVisitedAt: now,
                CurrentStartedAt: now,
                LastCompletedAt: null,
                LastSlideIndex: 0,
                SlideCount: slideCount,
                contentVersion)
            : existing with
            {
                VisitCount = existing.VisitCount + 1,
                LastVisitedAt = now,
                CurrentStartedAt = now,
                LastSlideIndex = 0,
                SlideCount = slideCount,
                ContentVersion = contentVersion,
            };
        return Replace(history, existing, updated);
    }

    public static LessonHistory Move(
        LessonHistory history,
        string lessonId,
        int slideIndex,
        DateTimeOffset now)
    {
        LessonHistoryValidator.Validate(history);
        var existing = RequireInProgress(history, lessonId, now);
        if (slideIndex < 0 || slideIndex >= existing.SlideCount)
        {
            throw new ArgumentOutOfRangeException(nameof(slideIndex));
        }

        return Replace(history, existing, existing with
        {
            LastVisitedAt = now,
            LastSlideIndex = slideIndex,
        });
    }

    public static LessonHistory Complete(
        LessonHistory history,
        string lessonId,
        DateTimeOffset now)
    {
        LessonHistoryValidator.Validate(history);
        var existing = RequireInProgress(history, lessonId, now);
        return Replace(history, existing, existing with
        {
            CompletedCount = existing.CompletedCount + 1,
            LastVisitedAt = now,
            CurrentStartedAt = null,
            LastCompletedAt = now,
            LastSlideIndex = existing.SlideCount - 1,
        });
    }

    private static LessonProgress RequireInProgress(
        LessonHistory history,
        string lessonId,
        DateTimeOffset now)
    {
        var normalizedId = LessonHistoryValidator.NormalizeId(lessonId);
        var existing = history.Lessons.SingleOrDefault(lesson =>
            string.Equals(lesson.LessonId, normalizedId, StringComparison.Ordinal))
            ?? throw new CurriculumValidationException(
                [$"Lesson '{normalizedId}' has not been started."]);
        if (!existing.IsInProgress)
        {
            throw new CurriculumValidationException(
                [$"Lesson '{normalizedId}' has no active visit."]);
        }

        if (now < existing.LastVisitedAt)
        {
            throw new CurriculumValidationException(
                [$"Lesson '{normalizedId}' cannot move before its stored history."]);
        }

        return existing;
    }

    private static LessonHistory Replace(
        LessonHistory history,
        LessonProgress? existing,
        LessonProgress updated)
    {
        var lessons = existing is null
            ? history.Lessons.Append(updated).ToArray()
            : history.Lessons.Select(lesson =>
                ReferenceEquals(lesson, existing) ? updated : lesson).ToArray();
        var result = new LessonHistory(lessons);
        LessonHistoryValidator.Validate(result);
        return result;
    }

    private static void ValidateInput(
        ConceptId conceptId,
        int slideCount,
        VersionId contentVersion,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(conceptId.Value))
        {
            throw new ArgumentException("The concept ID is required.", nameof(conceptId));
        }

        if (slideCount is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(slideCount));
        }

        if (string.IsNullOrWhiteSpace(contentVersion.Value))
        {
            throw new ArgumentException("The content version is required.", nameof(contentVersion));
        }

        if (now == default)
        {
            throw new ArgumentException("The visit time is required.", nameof(now));
        }
    }
}
