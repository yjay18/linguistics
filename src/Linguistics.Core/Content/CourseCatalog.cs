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
    Template,
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
    string? TaskId,
    CourseTemplateInstance? TemplateInstance = null);

public sealed record CourseTemplateInstance(
    string Id,
    TemplateId TemplateId,
    int TemplateVersion,
    ResolvedTemplateParameters Parameters);

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
    LanguageCode InstructionLanguage,
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
        LanguageCode instructionLanguage,
        CourseCatalogConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(packs);
        ArgumentNullException.ThrowIfNull(configuration);
        configuration.Validate();

        if (policy == ContentLoadPolicy.ValidationOnly)
        {
            throw new InvalidOperationException("Validation only content cannot create a learner course.");
        }

        var targetPacks = packs
            .Where(pack =>
                pack.Manifest.Kind == ContentPackKind.TargetLanguage &&
                pack.Manifest.Languages.Contains(
                    targetLanguage.Value,
                    StringComparer.Ordinal))
            .ToArray();
        var entries = targetPacks
            .SelectMany(pack => pack.Concepts.Select(concept => new CourseConcept(
                pack.Manifest,
                concept,
                pack.Lessons.FirstOrDefault(lesson => lesson.Id == $"lesson.{concept.Id}"))))
            .Where(entry => entry.Concept.Language == targetLanguage.Value)
            .ToArray();
        if (entries.Length == 0)
        {
            throw new InvalidOperationException($"No course content exists for language '{targetLanguage}'.");
        }

        var unsupportedPacks = targetPacks
            .Where(pack => !pack.Manifest.InstructionLanguages.Contains(
                instructionLanguage.Value,
                StringComparer.Ordinal))
            .Select(pack => pack.Manifest.Id)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (unsupportedPacks.Length > 0)
        {
            throw new InvalidOperationException(
                $"Instruction language '{instructionLanguage}' is unavailable for target language " +
                $"'{targetLanguage}' in pack(s): {string.Join(", ", unsupportedPacks)}.");
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
        var tasks = targetPacks
            .SelectMany(pack => pack.Tasks)
            .Where(task => task.Language == targetLanguage.Value)
            .OrderBy(task => task.Id, StringComparer.Ordinal)
            .ToArray();
        var conceptsById = packs
            .SelectMany(pack => pack.Concepts)
            .ToDictionary(concept => concept.Id, StringComparer.Ordinal);
        var examplesById = packs
            .SelectMany(NamedExamples)
            .ToDictionary(example => example.Id!, StringComparer.Ordinal);
        var tasksById = packs
            .SelectMany(pack => pack.Tasks)
            .ToDictionary(task => task.Id, StringComparer.Ordinal);
        var units = ordered
            .Chunk(configuration.LessonsPerUnit)
            .Select((chunk, index) => CreateUnit(
                targetLanguage,
                instructionLanguage,
                index + 1,
                chunk,
                tasks,
                conceptsById,
                examplesById,
                tasksById))
            .ToArray();

        return new CourseCatalog(
            targetLanguage,
            instructionLanguage,
            configuration.Version,
            policy == ContentLoadPolicy.Runtime
                ? CoursePublicationState.Ready
                : CoursePublicationState.Preview,
            configuration.TargetLessonCount,
            units);
    }

    private static CourseUnit CreateUnit(
        LanguageCode targetLanguage,
        LanguageCode instructionLanguage,
        int number,
        IReadOnlyList<CourseConcept> entries,
        IReadOnlyList<TaskTemplateContent> tasks,
        IReadOnlyDictionary<string, TargetConceptContent> conceptsById,
        IReadOnlyDictionary<string, ContentExample> examplesById,
        IReadOnlyDictionary<string, TaskTemplateContent> tasksById)
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
            entries.Select(entry => CreateLesson(
                entry,
                instructionLanguage,
                tasks,
                conceptsById,
                examplesById,
                tasksById)).ToArray());
    }

    private static CourseLesson CreateLesson(
        CourseConcept entry,
        LanguageCode instructionLanguage,
        IReadOnlyList<TaskTemplateContent> tasks,
        IReadOnlyDictionary<string, TargetConceptContent> conceptsById,
        IReadOnlyDictionary<string, ContentExample> examplesById,
        IReadOnlyDictionary<string, TaskTemplateContent> tasksById)
    {
        var concept = entry.Concept;
        var lessonId = $"lesson.{concept.Id}";
        var task = tasks.FirstOrDefault(candidate =>
            candidate.EligibleConceptIds.Contains(concept.Id, StringComparer.Ordinal));
        var slides = entry.Lesson is { } authored
            ? authored.TemplateInstances
                .Select((instance, index) => TemplateSlide(
                    lessonId,
                    index + 1,
                    concept,
                    instance,
                    instructionLanguage,
                    conceptsById,
                    examplesById,
                    tasksById))
                .ToList()
            : CreateFallbackSlides(lessonId, concept, task, instructionLanguage);

        return new CourseLesson(
            lessonId,
            new ConceptId(concept.Id),
            InstructionText.Resolve(concept.Title, instructionLanguage),
            InstructionText.Resolve(concept.Description, instructionLanguage),
            concept.CefrApproximation,
            concept.Review.Status,
            new VersionId($"{entry.Manifest.Id}.v{entry.Manifest.Version}"),
            slides);
    }

    private static List<CourseSlide> CreateFallbackSlides(
        string lessonId,
        TargetConceptContent concept,
        TaskTemplateContent? task,
        LanguageCode instructionLanguage)
    {
        var slides = new List<CourseSlide>
        {
            Slide(
                lessonId,
                1,
                CourseSlideKind.Welcome,
                "Lesson goal",
                InstructionText.Resolve(concept.Title, instructionLanguage),
                InstructionText.Resolve(concept.Description, instructionLanguage),
                $"Level {concept.CefrApproximation}"),
            Slide(
                lessonId,
                2,
                CourseSlideKind.Explanation,
                "Notice",
                "Meet the idea",
                InstructionText.Resolve(concept.Description, instructionLanguage),
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
                InstructionText.Resolve(example.Meaning, instructionLanguage),
                InstructionText.Resolve(example.Note, instructionLanguage)));
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
                InstructionText.Resolve(task.Goal, instructionLanguage),
                InstructionText.Resolve(task.Context, instructionLanguage),
                "The app checks the task with deterministic rules.",
                task.Id));
        slides.Add(Slide(
            lessonId,
            slideNumber,
            CourseSlideKind.Recap,
            "Recap",
            InstructionText.Resolve(concept.Title, instructionLanguage),
            InstructionText.Resolve(concept.Description, instructionLanguage),
            "Completing this lesson records a visit. Mastery still requires assessed evidence."));
        return slides;
    }

    private static CourseSlide TemplateSlide(
        string lessonId,
        int number,
        TargetConceptContent concept,
        TemplateInstance instance,
        LanguageCode instructionLanguage,
        IReadOnlyDictionary<string, TargetConceptContent> conceptsById,
        IReadOnlyDictionary<string, ContentExample> examplesById,
        IReadOnlyDictionary<string, TaskTemplateContent> tasksById) =>
        new(
            $"{lessonId}.slide.{number:00}",
            CourseSlideKind.Template,
            "Lesson",
            InstructionText.Resolve(concept.Title, instructionLanguage),
            InstructionText.Resolve(concept.Description, instructionLanguage),
            "Authored presentation",
            instance.Parameters.Values
                .FirstOrDefault(value => value.Kind == TemplateParameterKind.TaskReference)
                ?.Value,
            new CourseTemplateInstance(
                instance.Id,
                instance.TemplateId,
                instance.TemplateVersion,
                new ResolvedTemplateParameters(instance.Parameters
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .ToDictionary(
                        pair => pair.Key,
                        pair => ResolveParameter(pair.Value, conceptsById, examplesById, tasksById),
                        StringComparer.Ordinal))));

    private static ResolvedTemplateParameter ResolveParameter(
        TemplateParameterValue value,
        IReadOnlyDictionary<string, TargetConceptContent> conceptsById,
        IReadOnlyDictionary<string, ContentExample> examplesById,
        IReadOnlyDictionary<string, TaskTemplateContent> tasksById) =>
        value.Kind switch
        {
            TemplateParameterKind.Text => new(value.Kind, Text: value.Value),
            TemplateParameterKind.TextByLanguage => new(
                value.Kind,
                TextByLanguage: value.TextByLanguage!
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)),
            TemplateParameterKind.ConceptReference => new(
                value.Kind,
                Concept: conceptsById[value.Value!]),
            TemplateParameterKind.ExampleReference => new(
                value.Kind,
                Example: examplesById[value.Value!]),
            TemplateParameterKind.AssetReference => new(
                value.Kind,
                AssetReferenceId: value.Value),
            TemplateParameterKind.TaskReference => new(
                value.Kind,
                Task: tasksById[value.Value!]),
            TemplateParameterKind.OptionList => new(
                value.Kind,
                Options: value.Options!.ToArray()),
            _ => throw new InvalidOperationException($"Template parameter kind '{value.Kind}' is unsupported."),
        };

    private static IEnumerable<ContentExample> NamedExamples(ContentPackDocument pack) =>
        pack.Concepts
            .SelectMany(concept => concept.Examples.Concat(concept.Counterexamples))
            .Concat(pack.Lexicon.SelectMany(entry => entry.Examples))
            .Concat(pack.TransferMappings.SelectMany(mapping => mapping.PositiveExamples))
            .Where(example => example.Id is not null);

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
        TargetConceptContent Concept,
        LessonTemplateContent? Lesson);
}
