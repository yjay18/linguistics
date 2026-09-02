using Linguistics.Core.Content;

namespace Linguistics.Core.Tests;

[TestClass]
public sealed class TemplateCatalogValidationTests
{
    private const string AssetId = "preview.asset.template-catalog-fixture";
    private const string ExampleId = "de.example.template-catalog-fixture";

    [TestMethod]
    public void EveryRegisteredTemplateAcceptsAValidFixture()
    {
        foreach (var schema in LessonTemplateSchemas.All)
        {
            var (packs, _) = Fixture(schema);
            var errors = Validate(packs, schema);

            Assert.IsEmpty(
                errors,
                $"{schema.Id}:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    [TestMethod]
    public void EveryRegisteredTemplateRejectsOneBadValuePerParameterCategory()
    {
        foreach (var schema in LessonTemplateSchemas.All)
        {
            foreach (var definition in schema.Parameters
                         .GroupBy(parameter => parameter.Kind)
                         .Select(group => group.First()))
            {
                var (packs, lessonId) = Fixture(
                    schema,
                    parameters => parameters[definition.Name] = InvalidValue(definition.Kind));
                var errors = Validate(packs, schema);
                var expectedCode = ExpectedCode(definition.Kind);
                var error = errors.FirstOrDefault(candidate =>
                    candidate.Code == expectedCode &&
                    candidate.LessonId == lessonId &&
                    candidate.Parameter == definition.Name);

                Assert.IsNotNull(
                    error,
                    $"{schema.Id}.{definition.Name} expected {expectedCode}:{Environment.NewLine}" +
                    string.Join(Environment.NewLine, errors));
            }
        }
    }

    [TestMethod]
    public void EveryRegisteredTemplateRejectsAMissingRequiredParameter()
    {
        foreach (var schema in LessonTemplateSchemas.All)
        {
            var required = schema.Parameters.First(parameter => parameter.IsRequired);
            var (packs, lessonId) = Fixture(
                schema,
                parameters => parameters.Remove(required.Name));
            var errors = Validate(packs, schema);
            var error = errors.FirstOrDefault(candidate =>
                candidate.Code == "template.parameter.missing" &&
                candidate.LessonId == lessonId &&
                candidate.Parameter == required.Name);

            Assert.IsNotNull(
                error,
                $"{schema.Id}.{required.Name} expected template.parameter.missing:{Environment.NewLine}" +
                string.Join(Environment.NewLine, errors));
        }
    }

    private static IReadOnlyList<ContentValidationError> Validate(
        IReadOnlyList<ContentPackDocument> packs,
        LessonTemplateSchema schema) =>
        ContentPackValidator.Validate(
            packs,
            ContentLoadPolicy.ValidationOnly,
            [schema],
            [AssetId]);

    private static (ContentPackDocument[] Packs, string LessonId) Fixture(
        LessonTemplateSchema schema,
        Action<Dictionary<string, TemplateParameterValue>>? mutate = null)
    {
        var packs = ContentPackLoader.LoadDirectory(
                Path.Combine(AppContext.BaseDirectory, "Content"),
                ContentLoadPolicy.AuthoringPreview)
            .Packs
            .ToArray();
        var targetIndex = Array.FindIndex(
            packs,
            pack => pack.Manifest.Id == "language.de.core");
        var target = packs[targetIndex];
        var concept = target.Concepts[0] with
        {
            Examples = Replace(
                target.Concepts[0].Examples,
                0,
                target.Concepts[0].Examples[0] with { Id = ExampleId }),
        };
        target = target with
        {
            Concepts = Replace(target.Concepts, 0, concept),
        };
        var parameters = schema.Parameters.ToDictionary(
            definition => definition.Name,
            definition => ValidValue(definition.Kind, concept.Id, target.Tasks[0].Id),
            StringComparer.Ordinal);
        mutate?.Invoke(parameters);
        var lessonId = $"lesson.{concept.Id}";
        target = target with
        {
            Lessons =
            [
                new LessonTemplateContent(
                    lessonId,
                    [
                        new TemplateInstance(
                            $"{lessonId}.template.01",
                            schema.Id,
                            schema.Version,
                            parameters),
                    ]),
            ],
        };
        packs[targetIndex] = target;
        return (packs, lessonId);
    }

    private static TemplateParameterValue ValidValue(
        TemplateParameterKind kind,
        string conceptId,
        string taskId) => kind switch
        {
            TemplateParameterKind.Text => new(kind, Value: "one"),
            TemplateParameterKind.TextByLanguage => new(
                kind,
                TextByLanguage: new Dictionary<string, string>
                {
                    ["en"] = "Read the authored prompt.",
                    ["hi"] = "लिखे हुए निर्देश को पढ़ें।",
                }),
            TemplateParameterKind.ConceptReference => new(kind, Value: conceptId),
            TemplateParameterKind.ExampleReference => new(kind, Value: ExampleId),
            TemplateParameterKind.AssetReference => new(kind, Value: AssetId),
            TemplateParameterKind.TaskReference => new(kind, Value: taskId),
            TemplateParameterKind.OptionList => new(
                kind,
                Options:
                [
                    new TemplateOption("one", "Eins", AssetId),
                    new TemplateOption("two", "Zwei"),
                ]),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    private static TemplateParameterValue InvalidValue(TemplateParameterKind kind) => kind switch
    {
        TemplateParameterKind.Text => new(kind, Value: " "),
        TemplateParameterKind.TextByLanguage => new(
            kind,
            TextByLanguage: new Dictionary<string, string> { ["hi"] = "केवल हिन्दी।" }),
        TemplateParameterKind.ConceptReference => new(kind, Value: "de.concept.missing"),
        TemplateParameterKind.ExampleReference => new(kind, Value: "de.example.missing"),
        TemplateParameterKind.AssetReference => new(kind, Value: "preview.asset.missing"),
        TemplateParameterKind.TaskReference => new(kind, Value: "de.task.missing"),
        TemplateParameterKind.OptionList => new(
            kind,
            Options:
            [
                new TemplateOption("same", "Eins"),
                new TemplateOption("same", "Zwei"),
            ]),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static string ExpectedCode(TemplateParameterKind kind) => kind switch
    {
        TemplateParameterKind.Text => "template.parameter.type",
        TemplateParameterKind.TextByLanguage => "template.parameter.language",
        TemplateParameterKind.ConceptReference => "template.reference.concept",
        TemplateParameterKind.ExampleReference => "template.reference.example",
        TemplateParameterKind.AssetReference => "template.reference.asset",
        TemplateParameterKind.TaskReference => "template.reference.task",
        TemplateParameterKind.OptionList => "template.option",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static IReadOnlyList<T> Replace<T>(
        IReadOnlyList<T> items,
        int index,
        T replacement)
    {
        var result = items.ToArray();
        result[index] = replacement;
        return result;
    }
}
