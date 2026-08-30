using Linguistics.Core.Content;

namespace Linguistics.Core.Tests;

[TestClass]
public sealed class TemplateInteractionEvaluatorTests
{
    private static readonly IReadOnlyList<TemplateOption> Options =
    [
        new("ich", "Ich"),
        new("moechte", "möchte"),
        new("kaffee", "Kaffee"),
    ];

    [TestMethod]
    public void AcknowledgementMapsOnlyToReadyOrSuccess()
    {
        Assert.AreEqual(
            TemplateOutcomeState.Ready,
            TemplateInteractionEvaluator.EvaluateAcknowledgement(acknowledged: false).State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateAcknowledgement(acknowledged: true).State);
    }

    [TestMethod]
    public void PictureMatchMapsValidSelectionsDeterministically()
    {
        var success = TemplateInteractionEvaluator.EvaluatePictureMatch(Options, "kaffee", "kaffee");
        var failure = TemplateInteractionEvaluator.EvaluatePictureMatch(Options, "kaffee", "ich");
        var uncertain = TemplateInteractionEvaluator.EvaluatePictureMatch(Options, "kaffee", null);

        Assert.AreEqual(TemplateOutcomeState.Success, success.State);
        Assert.AreEqual("kaffee", success.ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Failure, failure.State);
        Assert.AreEqual(TemplateOutcomeState.Uncertain, uncertain.State);
    }

    [TestMethod]
    public void PictureMatchRejectsAnAnswerOutsideTheOptions()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            TemplateInteractionEvaluator.EvaluatePictureMatch(Options, "wasser", "ich"));
    }

    [TestMethod]
    public void WordOrderDistinguishesCompleteCorrectWrongAndIncompleteSequences()
    {
        var correct = TemplateInteractionEvaluator.EvaluateWordOrder(
            Options,
            ["ich", "moechte", "kaffee"]);
        var wrong = TemplateInteractionEvaluator.EvaluateWordOrder(
            Options,
            ["kaffee", "ich", "moechte"]);
        var incomplete = TemplateInteractionEvaluator.EvaluateWordOrder(
            Options,
            ["ich", "kaffee"]);

        Assert.AreEqual(TemplateOutcomeState.Success, correct.State);
        Assert.AreEqual(TemplateOutcomeState.Failure, wrong.State);
        Assert.AreEqual(TemplateOutcomeState.Uncertain, incomplete.State);
        CollectionAssert.AreEqual(
            new[] { "ich", "moechte", "kaffee" },
            correct.OrderedOptionIds!.ToArray());
    }

    [TestMethod]
    public void WordOrderRejectsAmbiguousExpectedIds()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            TemplateInteractionEvaluator.EvaluateWordOrder(
                [new("same", "One"), new("same", "Two")],
                ["same", "same"]));
    }
}
