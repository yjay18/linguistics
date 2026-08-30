using System.Text.Json;
using Linguistics.Core.Content;

namespace Linguistics.Core.Tests;

[TestClass]
public sealed class LessonTemplateContractsTests
{
    [TestMethod]
    public void TemplateIdsNormalizeAndSerializeAsStrings()
    {
        var id = new TemplateId(" Object-Spotlight ");

        Assert.AreEqual("object-spotlight", id.Value);
        Assert.AreEqual("\"object-spotlight\"", JsonSerializer.Serialize(id));
        Assert.AreEqual(id, JsonSerializer.Deserialize<TemplateId>("\"object-spotlight\""));
        Assert.ThrowsExactly<ArgumentException>(() => new TemplateId("../object-spotlight"));
    }

    [TestMethod]
    public void SchemaAndInstanceKeepEveryRequiredParameterKindTyped()
    {
        var definitions = Enum.GetValues<TemplateParameterKind>()
            .Select(kind => new TemplateParameterDefinition(kind.ToString(), kind, IsRequired: true))
            .ToArray();
        var schema = new LessonTemplateSchema(new TemplateId("fixture-template"), 2, definitions);
        var instance = new TemplateInstance(
            "lesson.fixture.instance.01",
            schema.Id,
            schema.Version,
            new Dictionary<string, TemplateParameterValue>
            {
                ["title"] = new(TemplateParameterKind.Text, Value: "Kaffee"),
                ["meaning"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string> { ["en"] = "coffee" }),
                ["concept"] = new(TemplateParameterKind.ConceptReference, Value: "de.lexicon.cafe-items"),
                ["example"] = new(TemplateParameterKind.ExampleReference, Value: "de.example.kaffee"),
                ["asset"] = new(TemplateParameterKind.AssetReference, Value: "de.asset.kaffee"),
                ["task"] = new(TemplateParameterKind.TaskReference, Value: "de.task.cafe.order-one-item"),
                ["options"] = new(
                    TemplateParameterKind.OptionList,
                    Options: [new TemplateOption("coffee", "Kaffee", "de.asset.kaffee")]),
            });

        Assert.AreEqual(2, schema.Version);
        Assert.HasCount(7, schema.Parameters);
        Assert.AreEqual(schema.Id, instance.TemplateId);
        Assert.AreEqual(TemplateParameterKind.OptionList, instance.Parameters["options"].Kind);
    }

    [TestMethod]
    public void TemplateErrorsNamePackLessonAndParameter()
    {
        var error = new ContentValidationError(
            "template.parameter.missing",
            "language.de.core",
            "lessons[0].instances[0].parameters.title",
            "A required parameter is missing.",
            "lesson.de.lexicon.cafe-items",
            "title");

        Assert.AreEqual("language.de.core", error.PackId);
        Assert.AreEqual("lesson.de.lexicon.cafe-items", error.LessonId);
        Assert.AreEqual("title", error.Parameter);
        StringAssert.Contains(error.ToString(), "language.de.core");
        StringAssert.Contains(error.ToString(), "lesson.de.lexicon.cafe-items");
        StringAssert.Contains(error.ToString(), "title");
    }
}
