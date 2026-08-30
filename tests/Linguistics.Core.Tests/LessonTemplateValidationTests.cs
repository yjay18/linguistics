using Linguistics.Core.Content;

namespace Linguistics.Core.Tests;

[TestClass]
public sealed class LessonTemplateValidationTests
{
    private const string AssetId = "preview.asset.fixture";
    private const string ExampleId = "de.example.fixture";

    private static readonly LessonTemplateSchema Schema = new(
        new TemplateId("fixture-template"),
        1,
        [
            new("title", TemplateParameterKind.Text, IsRequired: true),
            new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
            new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
            new("example", TemplateParameterKind.ExampleReference, IsRequired: true),
            new("asset", TemplateParameterKind.AssetReference, IsRequired: false),
            new("task", TemplateParameterKind.TaskReference, IsRequired: true),
            new("options", TemplateParameterKind.OptionList, IsRequired: true),
        ]);

    [TestMethod]
    public void ValidTypedInstanceResolvesEveryReferenceAndInstructionLanguage()
    {
        var (packs, _) = ValidFixture();

        var errors = Validate(packs);

        Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    [DataRow("unknown-template", "template.unknown", "templateId")]
    [DataRow("unsupported-template-version", "template.version", "templateVersion")]
    [DataRow("parameter-type", "template.parameter.type", "title")]
    [DataRow("missing-required-parameter", "template.parameter.missing", "title")]
    [DataRow("unknown-parameter", "template.parameter.unknown", "extra")]
    [DataRow("dangling-concept", "template.reference.concept", "concept")]
    [DataRow("dangling-example", "template.reference.example", "example")]
    [DataRow("dangling-asset", "template.reference.asset", "asset")]
    [DataRow("dangling-task", "template.reference.task", "task")]
    [DataRow("invalid-options", "template.option", "options")]
    [DataRow("instruction-language-coverage", "template.parameter.language", "instruction")]
    public void InvalidTypedInstanceFailsWithPackLessonAndParameter(
        string corruption,
        string expectedCode,
        string expectedParameter)
    {
        var (packs, lessonId) = ValidFixture();
        var targetIndex = Array.FindIndex(packs, pack => pack.Manifest.Id == "language.de.core");
        var target = packs[targetIndex];
        var lesson = target.Lessons.Single();
        var instance = lesson.TemplateInstances.Single();
        var parameters = instance.Parameters.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        switch (corruption)
        {
            case "unknown-template":
                instance = instance with { TemplateId = new TemplateId("missing-template") };
                break;
            case "unsupported-template-version":
                instance = instance with { TemplateVersion = 2 };
                break;
            case "parameter-type":
                parameters["title"] = new TemplateParameterValue(
                    TemplateParameterKind.TaskReference,
                    Value: target.Tasks[0].Id);
                break;
            case "missing-required-parameter":
                parameters.Remove("title");
                break;
            case "unknown-parameter":
                parameters["extra"] = new TemplateParameterValue(
                    TemplateParameterKind.Text,
                    Value: "extra");
                break;
            case "dangling-concept":
                parameters["concept"] = new TemplateParameterValue(
                    TemplateParameterKind.ConceptReference,
                    Value: "de.concept.missing");
                break;
            case "dangling-example":
                parameters["example"] = new TemplateParameterValue(
                    TemplateParameterKind.ExampleReference,
                    Value: "de.example.missing");
                break;
            case "dangling-asset":
                parameters["asset"] = new TemplateParameterValue(
                    TemplateParameterKind.AssetReference,
                    Value: "preview.asset.missing");
                break;
            case "dangling-task":
                parameters["task"] = new TemplateParameterValue(
                    TemplateParameterKind.TaskReference,
                    Value: "de.task.missing");
                break;
            case "invalid-options":
                parameters["options"] = new TemplateParameterValue(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new TemplateOption("same", "one"),
                        new TemplateOption("same", "two"),
                    ]);
                break;
            case "instruction-language-coverage":
                parameters["instruction"] = new TemplateParameterValue(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string> { ["en"] = "Choose one." });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(corruption));
        }

        instance = instance with { Parameters = parameters };
        target = target with
        {
            Lessons = [lesson with { TemplateInstances = [instance] }],
        };
        packs[targetIndex] = target;

        var errors = Validate(packs);
        var error = errors.FirstOrDefault(candidate => candidate.Code == expectedCode);

        Assert.IsNotNull(error, string.Join(Environment.NewLine, errors));
        Assert.AreEqual("language.de.core", error.PackId);
        Assert.AreEqual(lessonId, error.LessonId);
        Assert.AreEqual(expectedParameter, error.Parameter);
    }

    private static IReadOnlyList<ContentValidationError> Validate(
        IReadOnlyList<ContentPackDocument> packs) =>
        ContentPackValidator.Validate(
            packs,
            ContentLoadPolicy.ValidationOnly,
            [Schema],
            [AssetId]);

    private static (ContentPackDocument[] Packs, string LessonId) ValidFixture()
    {
        var packs = ContentPackLoader.LoadDirectory(
                Path.Combine(AppContext.BaseDirectory, "Content"),
                ContentLoadPolicy.AuthoringPreview)
            .Packs
            .ToArray();
        var targetIndex = Array.FindIndex(packs, pack => pack.Manifest.Id == "language.de.core");
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
                            Schema.Id,
                            Schema.Version,
                            new Dictionary<string, TemplateParameterValue>
                            {
                                ["title"] = new(TemplateParameterKind.Text, Value: concept.Title),
                                ["instruction"] = new(
                                    TemplateParameterKind.TextByLanguage,
                                    TextByLanguage: new Dictionary<string, string>
                                    {
                                        ["en"] = "Choose one.",
                                        ["hi"] = "एक चुनें।",
                                    }),
                                ["concept"] = new(TemplateParameterKind.ConceptReference, Value: concept.Id),
                                ["example"] = new(TemplateParameterKind.ExampleReference, Value: ExampleId),
                                ["asset"] = new(TemplateParameterKind.AssetReference, Value: AssetId),
                                ["task"] = new(TemplateParameterKind.TaskReference, Value: target.Tasks[0].Id),
                                ["options"] = new(
                                    TemplateParameterKind.OptionList,
                                    Options: [new TemplateOption("one", "Hallo", AssetId)]),
                            }),
                    ]),
            ],
        };
        packs[targetIndex] = target;
        return (packs, lessonId);
    }

    private static IReadOnlyList<T> Replace<T>(IReadOnlyList<T> items, int index, T replacement)
    {
        var result = items.ToArray();
        result[index] = replacement;
        return result;
    }
}
