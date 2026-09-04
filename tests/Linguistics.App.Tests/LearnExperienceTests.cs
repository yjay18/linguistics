using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Linguistics.App.Controls;
using Linguistics.App.Features.Learn;
using Linguistics.App.Features.Learn.Templates;
using Linguistics.App.Features.Review;
using Linguistics.Core.Content;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class LearnExperienceTests
{
    [TestMethod]
    public void EveryRegisteredTemplateExposesPlayerReplayAndSkipControls()
    {
        var registry = TemplateRegistry.CreateDefault();

        foreach (var fixture in TemplateGalleryFixtures.All)
        {
            var rendered = registry.RenderForPlayer(
                fixture.TemplateId,
                fixture.Parameters,
                fixture.InstructionLanguage,
                shouldReduceMotion: true,
                _ => { });
            var choreographyButtons = rendered.Content
                .GetLogicalDescendants()
                .OfType<Button>()
                .Where(button =>
                    AutomationProperties.GetAutomationId(button)?.EndsWith(
                        "Replay",
                        StringComparison.Ordinal) == true ||
                    AutomationProperties.GetAutomationId(button)?.EndsWith(
                        "Skip",
                        StringComparison.Ordinal) == true)
                .ToArray();

            Assert.HasCount(2, choreographyButtons, fixture.TemplateId.Value);
            Assert.IsTrue(choreographyButtons.All(button => !button.IsVisible));
            rendered.Skip();
            rendered.Replay();
        }
    }

    [TestMethod]
    public void LearnViewKeepsLayoutConstructionInAxamlControls()
    {
        var code = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Linguistics.App",
            "Features",
            "Learn",
            "LearnView.axaml.cs"));

        foreach (var control in new[] { "Grid", "StackPanel", "Border", "TextBlock", "Button", "WrapPanel" })
        {
            Assert.DoesNotContain($"new {control}", code, $"{control} layout belongs in AXAML.");
        }
    }

    [TestMethod]
    public void CourseJourneyPreservesUnitAndLessonOrderWithOneDeterministicNextStep()
    {
        var first = Lesson("lesson.first", "First", templateAuthored: false);
        var second = Lesson("lesson.second", "Second", templateAuthored: true);
        var course = new CourseCatalog(
            new LanguageCode("de"),
            new LanguageCode("en"),
            new VersionId("course-catalog-v1"),
            CoursePublicationState.Preview,
            TargetLessonCount: 450,
            [new CourseUnit(
                "unit.de.001",
                1,
                ConceptType.Lexical,
                "Words",
                "Useful words",
                [first, second])]);

        var journey = LearnView.CreateJourney(course, second);

        Assert.HasCount(1, journey);
        Assert.AreEqual("Words", journey[0].Title);
        Assert.AreEqual("Useful words", journey[0].Description);
        StringAssert.Contains(journey[0].UnitAutomationName, "Words");
        CollectionAssert.AreEqual(
            new[] { first.Id, second.Id },
            journey[0].Lessons.Select(item => item.Lesson.Id).ToArray());
        Assert.AreEqual(second.Id, journey[0].Lessons.Single(item => item.IsNext).Lesson.Id);
        Assert.AreNotEqual(
            journey[0].Lessons[0].PresentationKind,
            journey[0].Lessons[1].PresentationKind);
        Assert.IsTrue(journey[0].Lessons.All(item => !string.IsNullOrWhiteSpace(item.ReviewState)));
    }

    [TestMethod]
    public void LiveScenarioTheatreKeepsTheFullTextSceneAndMotionControls()
    {
        var rendered = ScenarioTheatreRenderer.RenderLive(
            imageCache: null,
            new ScenarioTheatreLivePresentation(
                "Listen, then reply.",
                "Order one drink.",
                "You are at a café counter.",
                "Café worker",
                ["Use the complete request frame."],
                "At the counter",
                "Guten Tag! Was möchten Sie?"),
            shouldReduceMotion: true);
        var controls = rendered.GetLogicalDescendants().OfType<Control>().ToArray();

        Assert.HasCount(1, controls.OfType<PaperStage>());
        Assert.IsTrue(controls.Any(control =>
            AutomationProperties.GetAutomationId(control) == "CafeScenarioTheatreGoal"));
        Assert.IsTrue(controls.Any(control =>
            AutomationProperties.GetAutomationId(control) == "CafeScenarioTheatreTextEquivalent"));
        Assert.IsTrue(controls.Any(control =>
            AutomationProperties.GetAutomationId(control) == "CafeScenarioTheatreReplay"));
        Assert.IsTrue(controls.Any(control =>
            AutomationProperties.GetAutomationId(control) == "CafeScenarioTheatreSkip"));
    }

    [TestMethod]
    public void CafeViewComposesTheProvenScenarioAndConsequenceRenderers()
    {
        var code = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Linguistics.App",
            "Features",
            "Scenarios",
            "CafeOrderView.axaml.cs"));
        var axaml = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Linguistics.App",
            "Features",
            "Scenarios",
            "CafeOrderView.axaml"));

        Assert.Contains("ScenarioTheatreRenderer.RenderLive", code);
        Assert.Contains("ConsequenceVerdictRenderer.Render", code);
        Assert.Contains("CafeScenarioTheatreHost", axaml);
        Assert.Contains("CafeConsequenceVerdictHost", axaml);
        Assert.DoesNotContain("Scenario_Completed_Title", axaml);
    }

    [TestMethod]
    public void ReviewProgressAndTodayUseThePhaseSixPaperSurfaces()
    {
        var reviewCode = FeatureText("Review", "ReviewView.axaml.cs");
        var reviewAxaml = FeatureText("Review", "ReviewView.axaml");
        var progressCode = FeatureText("Progress", "ProgressView.axaml.cs");
        var progressAxaml = FeatureText("Progress", "ProgressView.axaml");
        var todayAxaml = FeatureText("Today", "TodayView.axaml");

        Assert.Contains("ReviewFlashRenderer.Render", reviewCode);
        Assert.Contains("_controller.RecordAsync", reviewCode);
        Assert.Contains("ReviewFlashHost", reviewAxaml);
        Assert.DoesNotContain("OnRatingClicked", reviewAxaml);

        Assert.Contains("ProgressShelfRenderer.Render", progressCode);
        Assert.Contains("ProgressShelfHost", progressAxaml);
        Assert.DoesNotContain("CapabilityCard", progressAxaml);

        Assert.Contains("controls:PaperCard", todayAxaml);
        Assert.Contains("controls:PaperTape", todayAxaml);
        Assert.Contains("controls:PaperStamp", todayAxaml);
        Assert.Contains("controls:CutoutFrame", todayAxaml);
        Assert.Contains("<WrapPanel x:Name=\"EvidenceGrid\"", todayAxaml);
    }

    [TestMethod]
    public void ReviewFlashResponseIdsMapToTheExistingSchedulerRatings()
    {
        Assert.AreEqual(ReviewRating.Again, ReviewView.RatingFromResponseId("again"));
        Assert.AreEqual(ReviewRating.Hard, ReviewView.RatingFromResponseId("hard"));
        Assert.AreEqual(ReviewRating.Good, ReviewView.RatingFromResponseId("good"));
        Assert.AreEqual(ReviewRating.Easy, ReviewView.RatingFromResponseId("easy"));
        Assert.IsNull(ReviewView.RatingFromResponseId("unknown"));
        Assert.IsNull(ReviewView.RatingFromResponseId(null));
    }

    private static string FeatureText(string feature, string fileName) => File.ReadAllText(
        Path.Combine(
            RepositoryRoot,
            "src",
            "Linguistics.App",
            "Features",
            feature,
            fileName));

    private static CourseLesson Lesson(string id, string title, bool templateAuthored)
    {
        var template = templateAuthored
            ? new CourseTemplateInstance(
                $"{id}.template",
                new TemplateId("object-spotlight"),
                1,
                new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>()))
            : null;
        return new CourseLesson(
            id,
            new ConceptId($"de.lexical.{title.ToLowerInvariant()}"),
            title,
            title,
            "A1",
            ContentReviewStatus.MachineValidated,
            new VersionId("language.de.core.v1"),
            [new CourseSlide(
                $"{id}.slide.01",
                templateAuthored ? CourseSlideKind.Template : CourseSlideKind.Welcome,
                "Lesson",
                title,
                title,
                title,
                TaskId: null,
                template)]);
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "..",
        ".."));
}
