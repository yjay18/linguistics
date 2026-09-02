using Linguistics.App.Features.Learn;
using Linguistics.Core.Content;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class LearnExperienceTests
{
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
        CollectionAssert.AreEqual(
            new[] { first.Id, second.Id },
            journey[0].Lessons.Select(item => item.Lesson.Id).ToArray());
        Assert.AreEqual(second.Id, journey[0].Lessons.Single(item => item.IsNext).Lesson.Id);
        Assert.AreNotEqual(
            journey[0].Lessons[0].PresentationKind,
            journey[0].Lessons[1].PresentationKind);
        Assert.IsTrue(journey[0].Lessons.All(item => !string.IsNullOrWhiteSpace(item.ReviewState)));
    }

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
}
