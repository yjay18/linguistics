using Avalonia.Controls;
using Linguistics.App.Features.Learn.Templates;
using Linguistics.Core.Content;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class TemplateGalleryViewTests
{
    [TestMethod]
    public void GalleryCyclesEveryOutcomeWithSyntheticParameters()
    {
        var id = new TemplateId("fixture-template");
        var renderedOutcomes = new List<TemplateOutcomeState>();
        TemplateRendererFactory renderer = (parameters, _, _, _) =>
        {
            renderedOutcomes.Add(parameters.PreviewOutcome);
            return new Border();
        };
        var registry = new TemplateRegistry(
            [new KeyValuePair<TemplateId, TemplateRendererFactory>(id, renderer)]);
        var fixture = new TemplateGalleryFixture(
            id,
            "Fixture",
            "Synthetic family",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>()));
        var gallery = new TemplateGalleryView(registry, [fixture], shouldReduceMotion: true);

        gallery.CycleOutcome();
        gallery.CycleOutcome();
        gallery.CycleOutcome();
        gallery.CycleOutcome();

        CollectionAssert.AreEqual(
            new[]
            {
                TemplateOutcomeState.Ready,
                TemplateOutcomeState.Success,
                TemplateOutcomeState.Uncertain,
                TemplateOutcomeState.Failure,
                TemplateOutcomeState.Ready,
            },
            renderedOutcomes.ToArray());
    }

    [TestMethod]
    public void GalleryCanForceTextOnlyWithoutLearnerDataOrPersistence()
    {
        var id = new TemplateId("fixture-template");
        var textOnlyStates = new List<bool>();
        TemplateRendererFactory renderer = (parameters, _, _, _) =>
        {
            textOnlyStates.Add(parameters.UseTextOnlyFallback);
            return new Border();
        };
        var registry = new TemplateRegistry(
            [new KeyValuePair<TemplateId, TemplateRendererFactory>(id, renderer)]);
        var fixture = new TemplateGalleryFixture(
            id,
            "Fixture",
            "Synthetic family",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>()));
        var gallery = new TemplateGalleryView(registry, [fixture], shouldReduceMotion: false);

        gallery.SetTextOnlyFallback(true);

        Assert.IsTrue(gallery.UseTextOnlyFallback);
        Assert.IsTrue(textOnlyStates.Last());
        var constructorTypes = typeof(TemplateGalleryView)
            .GetConstructors(System.Reflection.BindingFlags.Instance |
                             System.Reflection.BindingFlags.Public |
                             System.Reflection.BindingFlags.NonPublic)
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType.FullName ?? parameter.ParameterType.Name)
            .ToArray();
        Assert.IsFalse(constructorTypes.Any(type =>
            type.Contains("LearnerProfile", StringComparison.Ordinal) ||
            type.Contains("Persistence", StringComparison.Ordinal)));
    }
}
