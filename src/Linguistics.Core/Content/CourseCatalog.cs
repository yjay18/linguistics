using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;

namespace Linguistics.Core.Content;

public enum CoursePublicationState
{
    Preview,
    Ready,
}

public enum CourseSlideKind
{
    Welcome,
    Explanation,
    Example,
    Activity,
    Recap,
}

public sealed record CourseSlide(
    string Id,
    CourseSlideKind Kind,
    string Eyebrow,
    string Title,
    string Body,
    string SupportingText,
    string? TaskId);

public sealed record CourseLesson(
    string Id,
    ConceptId ConceptId,
    string Title,
    string Goal,
    string CefrApproximation,
    ContentReviewStatus ReviewStatus,
    VersionId ContentVersion,
    IReadOnlyList<CourseSlide> Slides);

public sealed record CourseUnit(
    string Id,
    int Number,
    string Title,
    string Description,
    IReadOnlyList<CourseLesson> Lessons);

public sealed record CourseCatalog(
    LanguageCode TargetLanguage,
    VersionId Version,
    CoursePublicationState PublicationState,
    int TargetLessonCount,
    IReadOnlyList<CourseUnit> Units)
{
    public int AuthoredLessonCount => Units.Sum(unit => unit.Lessons.Count);

    public int RemainingLessonCount => Math.Max(0, TargetLessonCount - AuthoredLessonCount);
}

public sealed record CourseCatalogConfiguration(
    VersionId Version,
    int TargetLessonCount,
    int LessonsPerUnit)
{
    public const int MinimumLessonCount = 400;
    public const int MaximumLessonCount = 500;

    public static CourseCatalogConfiguration Default { get; } = new(
        new VersionId("course-catalog-v1"),
        TargetLessonCount: 450,
        LessonsPerUnit: 20);

    public void Validate()
    {
        if (TargetLessonCount is < MinimumLessonCount or > MaximumLessonCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(TargetLessonCount),
                $"A course target must contain between {MinimumLessonCount} and {MaximumLessonCount} lessons.");
        }

        if (LessonsPerUnit is < 5 or > 30)
        {
            throw new ArgumentOutOfRangeException(
                nameof(LessonsPerUnit),
                "A course unit must contain between 5 and 30 lessons.");
        }
    }
}

internal static class CourseCatalogBuilder
{
    private static readonly IReadOnlyDictionary<ConceptType, (string Title, string Description)> UnitCopy =
        new Dictionary<ConceptType, (string Title, string Description)>
        {
            [ConceptType.Pragmatic] = ("Speak with purpose", "Use language to accomplish something useful."),
            [ConceptType.Lexical] = ("Build useful words", "Grow the words you can recognize and use."),
            [ConceptType.Grammatical] = ("Shape clear sentences", "Notice patterns that make meaning precise."),
            [ConceptType.Listening] = ("Train your ear", "Recognize useful language in short exchanges."),
            [ConceptType.Phonological] = ("Make speech clear", "Practice sounds and rhythm in meaningful phrases."),
            [ConceptType.Discourse] = ("Connect ideas", "Link meaning across a complete exchange."),
            [ConceptType.Sociolinguistic] = ("Choose language for the moment", "Match your language to the situation."),
        };

    public static CourseCatalog Build(
        IReadOnlyList<ContentPackDocument> packs,
        ContentLoadPolicy policy,
        LanguageCode targetLanguage,
        CourseCatalogConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(packs);
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();

        if (policy == ContentLoadPolicy.ValidationOnly)
        {
            throw new InvalidOperationException("Validation only content cannot create a learner course.");
        }

        var entries = packs
            .Where(pack => pack.Manifest.Kind == ContentPackKind.TargetLanguage)
            .SelectMany(pack => pack.Concepts.Select(concept => new CourseConcept(pack.Manifest, concept)))
            .Where(entry => entry.Concept.Language == targetLanguage.Value)
            .ToArray();
        if (entries.Length == 0)
        {
            throw new InvalidOperationException($"No course content exists for language '{targetLanguage}'.");
        }

        var depthById = new Dictionary<string, int>(StringComparer.Ordinal);
        var entryById = entries.ToDictionary(entry => entry.Concept.Id, StringComparer.Ordinal);
        int Depth(CourseConcept entry)
        {
            if (depthById.TryGetValue(entry.Concept.Id, out var depth))
            {
                return depth;
            }

            depth = entry.Concept.PrerequisiteIds.Count == 0
                ? 0
                : entry.Concept.PrerequisiteIds.Max(id => Depth(entryById[id])) + 1;
            depthById.Add(entry.Concept.Id, depth);
            return depth;
        }

        var ordered = entries
            .OrderBy(Depth)
            .ToArray();
        var tasks = packs
            .Where(pack => pack.Manifest.Kind == ContentPackKind.TargetLanguage)
            .SelectMany(pack => pack.Tasks)
            .Where(task => task.Language == targetLanguage.Value)
            .OrderBy(task => task.Id, StringComparer.Ordinal)
            .ToArray();
        var units = ordered
            .Chunk(configuration.LessonsPerUnit)
            .Select((chunk, index) => CreateUnit(targetLanguage, index + 1, chunk, tasks))
            .ToArray();

        return new CourseCatalog(
            targetLanguage,
            configuration.Version,
            policy == ContentLoadPolicy.Runtime
                ? CoursePublicationState.Ready
                : CoursePublicationState.Preview,
            configuration.TargetLessonCount,
            units);
    }

    private static CourseUnit CreateUnit(
        LanguageCode targetLanguage,
        int number,
        IReadOnlyList<CourseConcept> entries,
        IReadOnlyList<TaskTemplateContent> tasks)
    {
        var dominantType = entries
            .GroupBy(entry => entry.Concept.Type)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .First()
            .Key;
        var copy = UnitCopy[dominantType];

        return new CourseUnit(
            $"unit.{targetLanguage.Value}.{number:000}",
            number,
            copy.Title,
            copy.Description,
            entries.Select(entry => CreateLesson(entry, tasks)).ToArray());
    }

    private static CourseLesson CreateLesson(
        CourseConcept entry,
        IReadOnlyList<TaskTemplateContent> tasks)
    {
        var concept = entry.Concept;
        var lessonId = $"lesson.{concept.Id}";
        var task = tasks.FirstOrDefault(candidate =>
            candidate.EligibleConceptIds.Contains(concept.Id, StringComparer.Ordinal));
        var slides = new List<CourseSlide>
        {
            Slide(
                lessonId,
                1,
                CourseSlideKind.Welcome,
                "Lesson goal",
                concept.Title,
                concept.Description,
                $"Level {concept.CefrApproximation}"),
            Slide(
                lessonId,
                2,
                CourseSlideKind.Explanation,
                "Notice",
                "Meet the idea",
                concept.Description,
                "Move at your own pace. You can revisit every card."),
        };
        var slideNumber = 3;
        foreach (var example in concept.Examples.Take(2))
        {
            slides.Add(Slide(
                lessonId,
                slideNumber++,
                CourseSlideKind.Example,
                "Example",
                example.Text,
                example.Meaning,
                example.Note));
        }

        slides.Add(task is null
            ? Slide(
                lessonId,
                slideNumber++,
                CourseSlideKind.Activity,
                "Your turn",
                "Try it from memory",
                "Recall the meaning, then say or write the example without looking.",
                "This practice does not change mastery on its own.")
            : Slide(
                lessonId,
                slideNumber++,
                CourseSlideKind.Activity,
                "Your turn",
                task.Goal,
                task.Context,
                "The app checks the task with deterministic rules.",
                task.Id));
        slides.Add(Slide(
            lessonId,
            slideNumber,
            CourseSlideKind.Recap,
            "Recap",
            concept.Title,
            concept.Description,
            "Completing this lesson records a visit. Mastery still requires assessed evidence."));

        return new CourseLesson(
            lessonId,
            new ConceptId(concept.Id),
            concept.Title,
            concept.Description,
            concept.CefrApproximation,
            concept.Review.Status,
            new VersionId($"{entry.Manifest.Id}.v{entry.Manifest.Version}"),
            slides);
    }

    private static CourseSlide Slide(
        string lessonId,
        int number,
        CourseSlideKind kind,
        string eyebrow,
        string title,
        string body,
        string supportingText,
        string? taskId = null) =>
        new(
            $"{lessonId}.slide.{number:00}",
            kind,
            eyebrow,
            title,
            body,
            supportingText,
            taskId);

    private sealed record CourseConcept(
        ContentPackManifest Manifest,
        TargetConceptContent Concept);
}
