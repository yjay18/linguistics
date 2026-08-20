using Linguistics.Core.Curriculum;

namespace Linguistics.Core.Tests;

[TestClass]
public sealed class LessonProgressTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void VisitMovesAndCompletesWithoutCreatingMasteryEvidence()
    {
        var started = LessonProgressTracker.Begin(
            LessonHistory.Empty,
            "lesson.de.greeting",
            new ConceptId("de.greeting"),
            slideCount: 5,
            new VersionId("language.de.v1"),
            Now);
        var moved = LessonProgressTracker.Move(
            started,
            "lesson.de.greeting",
            slideIndex: 3,
            Now.AddMinutes(1));
        var completed = LessonProgressTracker.Complete(
            moved,
            "lesson.de.greeting",
            Now.AddMinutes(2));

        var progress = completed.Lessons.Single();
        Assert.AreEqual(1, progress.VisitCount);
        Assert.AreEqual(1, progress.CompletedCount);
        Assert.AreEqual(4, progress.LastSlideIndex);
        Assert.IsFalse(progress.IsInProgress);
        Assert.AreEqual(Now.AddMinutes(2), progress.LastCompletedAt);
    }

    [TestMethod]
    public void ReopeningACompletedLessonStartsAtTheFirstCardAndKeepsCounts()
    {
        var first = LessonProgressTracker.Begin(
            LessonHistory.Empty,
            "lesson.de.greeting",
            new ConceptId("de.greeting"),
            slideCount: 5,
            new VersionId("language.de.v1"),
            Now);
        var completed = LessonProgressTracker.Complete(
            first,
            "lesson.de.greeting",
            Now.AddMinutes(1));

        var reopened = LessonProgressTracker.Begin(
            completed,
            "lesson.de.greeting",
            new ConceptId("de.greeting"),
            slideCount: 6,
            new VersionId("language.de.v2"),
            Now.AddDays(1));

        var progress = reopened.Lessons.Single();
        Assert.AreEqual(2, progress.VisitCount);
        Assert.AreEqual(1, progress.CompletedCount);
        Assert.AreEqual(0, progress.LastSlideIndex);
        Assert.AreEqual(6, progress.SlideCount);
        Assert.AreEqual("language.de.v2", progress.ContentVersion.Value);
        Assert.IsTrue(progress.IsInProgress);
    }

    [TestMethod]
    public void InvalidOrUnstartedMovementFailsClosed()
    {
        Assert.ThrowsExactly<CurriculumValidationException>(() =>
            LessonProgressTracker.Move(
                LessonHistory.Empty,
                "lesson.de.greeting",
                slideIndex: 1,
                Now));

        var invalid = new LessonHistory(
        [
            new LessonProgress(
                "lesson.de.greeting",
                new ConceptId("de.greeting"),
                VisitCount: 1,
                CompletedCount: 2,
                Now,
                Now,
                CurrentStartedAt: null,
                LastCompletedAt: Now,
                LastSlideIndex: 4,
                SlideCount: 5,
                new VersionId("language.de.v1")),
        ]);

        Assert.ThrowsExactly<CurriculumValidationException>(() =>
            LessonHistoryValidator.Validate(invalid));
    }

    [TestMethod]
    public void HistoryCannotExceedTheCourseCapacityBoundary()
    {
        var lessons = Enumerable.Range(1, LessonHistoryValidator.MaximumLessons + 1)
            .Select(number => new LessonProgress(
                $"lesson.de.item.{number}",
                new ConceptId($"de.item.{number}"),
                VisitCount: 1,
                CompletedCount: 0,
                Now,
                Now,
                CurrentStartedAt: Now,
                LastCompletedAt: null,
                LastSlideIndex: 0,
                SlideCount: 5,
                new VersionId("language.de.v1")))
            .ToArray();

        Assert.ThrowsExactly<CurriculumValidationException>(() =>
            LessonHistoryValidator.Validate(new LessonHistory(lessons)));
    }
}
