using System.Reflection;
using Avalonia.Controls;
using Linguistics.App.Features.Learn.Templates;
using Linguistics.Core.Content;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class TemplateRegistryTests
{
    [TestMethod]
    public void RegistryPassesOnlyTheFourRendererInputs()
    {
        var id = new TemplateId("fixture-template");
        var parameters = new ResolvedTemplateParameters(
            new Dictionary<string, ResolvedTemplateParameter>
            {
                ["title"] = new(TemplateParameterKind.Text, Text: "Hallo"),
            });
        var language = new LanguageCode("en");
        var reported = new List<TemplateOutcome>();
        var expected = new Border();
        TemplateRendererFactory renderer = (actualParameters, actualLanguage, actualMotion, callback) =>
        {
            Assert.AreSame(parameters, actualParameters);
            Assert.AreEqual(language, actualLanguage);
            Assert.IsTrue(actualMotion);
            callback(new TemplateOutcome(TemplateOutcomeState.Success, "fixture-response"));
            return expected;
        };
        var registry = new TemplateRegistry(
            [new KeyValuePair<TemplateId, TemplateRendererFactory>(id, renderer)]);

        var rendered = registry.Render(id, parameters, language, shouldReduceMotion: true, reported.Add);

        Assert.AreSame(expected, rendered);
        Assert.HasCount(1, reported);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[0].State);
        CollectionAssert.AreEqual(new[] { id }, registry.RegisteredTemplateIds.ToArray());
    }

    [TestMethod]
    public void RegistryRejectsMissingAndDuplicateRenderers()
    {
        var id = new TemplateId("fixture-template");
        TemplateRendererFactory renderer = (_, _, _, _) => new Border();
        var registry = new TemplateRegistry([]);

        Assert.ThrowsExactly<KeyNotFoundException>(() => registry.Render(
            id,
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>()),
            new LanguageCode("en"),
            shouldReduceMotion: false,
            _ => { }));
        Assert.ThrowsExactly<ArgumentException>(() => new TemplateRegistry(
            [
                new KeyValuePair<TemplateId, TemplateRendererFactory>(id, renderer),
                new KeyValuePair<TemplateId, TemplateRendererFactory>(id, renderer),
            ]));
    }

    [TestMethod]
    public void DefaultRegistryContainsExactlyTheThreeProvingTemplates()
    {
        CollectionAssert.AreEqual(
            new[] { "object-spotlight", "picture-match", "word-order-train" },
            TemplateRegistry.CreateDefault()
                .RegisteredTemplateIds
                .Select(id => id.Value)
                .ToArray());
    }

    [TestMethod]
    public void RendererContractCannotReceivePersistenceOrMasteryServices()
    {
        var invoke = typeof(TemplateRendererFactory).GetMethod("Invoke")!;

        CollectionAssert.AreEqual(
            new[]
            {
                typeof(ResolvedTemplateParameters),
                typeof(LanguageCode),
                typeof(bool),
                typeof(Action<TemplateOutcome>),
            },
            invoke.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
        Assert.AreEqual(typeof(Control), invoke.ReturnType);

        var rendererTypes = typeof(TemplateRegistry).Assembly
            .GetTypes()
            .Where(type =>
                type.Namespace == typeof(TemplateRegistry).Namespace &&
                type.Name.EndsWith("Renderer", StringComparison.Ordinal))
            .ToArray();
        Assert.IsTrue(rendererTypes.All(type => type.IsAbstract && type.IsSealed),
            "Template renderers must be static and dependency-free.");

        var templatesDirectory = Path.Combine(
            RepositoryRoot,
            "src",
            "Linguistics.App",
            "Features",
            "Learn",
            "Templates");
        var forbidden = new[]
        {
            "Linguistics.App.Persistence",
            "LearnerProfileOwner",
            "LearnerLearningState",
            "ConceptProgress",
            "Mastery",
            "ReviewScheduler",
            "LearnerRepository",
        };
        foreach (var path in Directory.EnumerateFiles(templatesDirectory, "*Renderer.cs"))
        {
            var source = File.ReadAllText(path);
            foreach (var term in forbidden)
            {
                Assert.IsFalse(source.Contains(term, StringComparison.Ordinal),
                    $"{Path.GetFileName(path)} may not reference {term}.");
            }
        }
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../"));
}
