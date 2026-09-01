using Linguistics.Core.Content;
using Linguistics.Core.Speech;

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
    public void SceneEstablishUsesAcknowledgementOnlyForItsDeterministicOutcome()
    {
        var ready = TemplateInteractionEvaluator.EvaluateAcknowledgement(acknowledged: false);
        var complete = TemplateInteractionEvaluator.EvaluateAcknowledgement(acknowledged: true);

        Assert.AreEqual(TemplateOutcomeState.Ready, ready.State);
        Assert.AreEqual(TemplateOutcomeState.Success, complete.State);
        Assert.IsNull(complete.ResponseId);
        Assert.IsNull(complete.OrderedOptionIds);
    }

    [TestMethod]
    [DataRow("scene-establish")]
    [DataRow("object-spotlight")]
    [DataRow("object-anatomy")]
    [DataRow("paper-dialogue")]
    [DataRow("street-walk")]
    [DataRow("postcard-story")]
    [DataRow("photo-album")]
    [DataRow("culture-plate")]
    [DataRow("weather-window")]
    [DataRow("clock-theatre")]
    [DataRow("sentence-fold")]
    [DataRow("separable-verb-split")]
    [DataRow("question-flip")]
    public void AcknowledgementTemplatesMapCompletionDeterministically(string templateId)
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema => schema.Id.Value == templateId));
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
    [DataRow("picture-match")]
    [DataRow("word-match")]
    [DataRow("odd-one-out")]
    [DataRow("article-stamp")]
    [DataRow("color-swatch")]
    [DataRow("number-tiles")]
    [DataRow("label-the-scene")]
    [DataRow("gap-card")]
    [DataRow("preposition-stage")]
    [DataRow("listen-pick-image")]
    [DataRow("minimal-pair-doors")]
    [DataRow("listen-price-tag")]
    [DataRow("dialogue-eavesdrop")]
    public void SingleSelectionTemplatesUseDeterministicMapping(string templateId)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(templateId));
        var success = TemplateInteractionEvaluator.EvaluateSingleSelection(Options, "kaffee", "kaffee");
        var failure = TemplateInteractionEvaluator.EvaluateSingleSelection(Options, "kaffee", "ich");
        var uncertain = TemplateInteractionEvaluator.EvaluateSingleSelection(Options, "kaffee", null);

        Assert.AreEqual(TemplateOutcomeState.Success, success.State);
        Assert.AreEqual(TemplateOutcomeState.Failure, failure.State);
        Assert.AreEqual(TemplateOutcomeState.Uncertain, uncertain.State);
    }

    [TestMethod]
    public void PairCardsMatchesOnlyOppositeSidesOfTheSamePair()
    {
        var success = TemplateInteractionEvaluator.EvaluatePairCards(
            Options,
            ["word:kaffee", "image:kaffee"]);
        var failure = TemplateInteractionEvaluator.EvaluatePairCards(
            Options,
            ["word:ich", "image:kaffee"]);
        var uncertain = TemplateInteractionEvaluator.EvaluatePairCards(
            Options,
            ["word:ich"]);

        Assert.AreEqual(TemplateOutcomeState.Success, success.State);
        Assert.AreEqual(TemplateOutcomeState.Failure, failure.State);
        Assert.AreEqual(TemplateOutcomeState.Uncertain, uncertain.State);
        CollectionAssert.AreEqual(
            new[] { "word:kaffee", "image:kaffee" },
            success.OrderedOptionIds!.ToArray());
    }

    [TestMethod]
    public void SortAssignmentsRequireEveryItemAndUseTheAuthoredAnswerMap()
    {
        var baskets = new[]
        {
            new TemplateOption("drink", "Drinks"),
            new TemplateOption("person", "People"),
        };
        var expected = new Dictionary<string, string>
        {
            ["ich"] = "person",
            ["moechte"] = "person",
            ["kaffee"] = "drink",
        };
        var correct = TemplateInteractionEvaluator.EvaluateSortAssignments(
            Options,
            baskets,
            expected,
            expected);
        var wrong = TemplateInteractionEvaluator.EvaluateSortAssignments(
            Options,
            baskets,
            expected,
            new Dictionary<string, string>
            {
                ["ich"] = "drink",
                ["moechte"] = "person",
                ["kaffee"] = "drink",
            });
        var incomplete = TemplateInteractionEvaluator.EvaluateSortAssignments(
            Options,
            baskets,
            expected,
            new Dictionary<string, string> { ["kaffee"] = "drink" });

        Assert.AreEqual(TemplateOutcomeState.Success, correct.State);
        Assert.AreEqual(TemplateOutcomeState.Failure, wrong.State);
        Assert.AreEqual(TemplateOutcomeState.Uncertain, incomplete.State);
    }

    [TestMethod]
    public void ConjugationWheelUsesTheAuthoredPersonToFormMap()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("conjugation-wheel")));
        var persons = new[]
        {
            new TemplateOption("ich", "ich"),
            new TemplateOption("du", "du"),
        };
        var forms = new[]
        {
            new TemplateOption("gehe", "gehe"),
            new TemplateOption("gehst", "gehst"),
        };
        var answers = new Dictionary<string, string>
        {
            ["ich"] = "gehe",
            ["du"] = "gehst",
        };

        var success = TemplateInteractionEvaluator.EvaluateMappedPair(
            persons,
            forms,
            answers,
            "du",
            "gehst");
        var failure = TemplateInteractionEvaluator.EvaluateMappedPair(
            persons,
            forms,
            answers,
            "du",
            "gehe");
        var uncertain = TemplateInteractionEvaluator.EvaluateMappedPair(
            persons,
            forms,
            answers,
            "du",
            null);

        Assert.AreEqual(TemplateOutcomeState.Success, success.State);
        Assert.AreEqual(TemplateOutcomeState.Failure, failure.State);
        Assert.AreEqual(TemplateOutcomeState.Uncertain, uncertain.State);
        CollectionAssert.AreEqual(new[] { "du", "gehst" }, success.OrderedOptionIds!.ToArray());
    }

    [TestMethod]
    public void CaseSwitchboardUsesTheAuthoredRoleToArticleMap()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("case-switchboard")));
        var roles = new[]
        {
            new TemplateOption("subject", "Subject"),
            new TemplateOption("object", "Object"),
        };
        var articles = new[]
        {
            new TemplateOption("der", "der"),
            new TemplateOption("den", "den"),
        };
        var answers = new Dictionary<string, string>
        {
            ["subject"] = "der",
            ["object"] = "den",
        };

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateMappedPair(
                roles,
                articles,
                answers,
                "object",
                "den").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateMappedPair(
                roles,
                articles,
                answers,
                "object",
                "der").State);
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateMappedPair(
                roles,
                articles,
                answers,
                null,
                "der").State);
    }

    [TestMethod]
    public void NegationStrikeUsesTheAuthoredTokenAndSlot()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("negation-strike")));
        var negators = new[]
        {
            new TemplateOption("nicht", "nicht"),
            new TemplateOption("kein", "kein"),
        };
        var slots = new[]
        {
            new TemplateOption("before", "Before"),
            new TemplateOption("after", "After"),
        };

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSelectionPair(
                negators,
                slots,
                "kein",
                "before",
                "kein",
                "before").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSelectionPair(
                negators,
                slots,
                "kein",
                "before",
                "nicht",
                "before").State);
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateSelectionPair(
                negators,
                slots,
                "kein",
                "before",
                "kein",
                null).State);
    }

    [TestMethod]
    public void PluralFoldUsesAcknowledgementAfterTheAuthoredFoldOpens()
    {
        Assert.AreEqual(
            TemplateOutcomeState.Ready,
            TemplateInteractionEvaluator.EvaluateAcknowledgement(acknowledged: false).State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateAcknowledgement(acknowledged: true).State);
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
    public void SentenceExpandUsesTheAuthoredComplementOrder()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("sentence-expand")));
        var complements = new[]
        {
            new TemplateOption("object", "einen Kaffee"),
            new TemplateOption("place", "im Café"),
            new TemplateOption("time", "am Morgen"),
        };

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                complements,
                ["object", "place", "time"]).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                complements,
                ["time", "place", "object"]).State);
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                complements,
                ["object"]).State);
    }

    [TestMethod]
    public void ListenOrderUsesTheAuthoredEventSequence()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("listen-order")));
        var events = new[]
        {
            new TemplateOption("tea", "Tee"),
            new TemplateOption("water", "Wasser"),
            new TemplateOption("coffee", "Kaffee"),
        };

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                events,
                ["tea", "water", "coffee"]).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                events,
                ["coffee", "water", "tea"]).State);
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                events,
                ["tea"]).State);
    }

    [TestMethod]
    public void ListenTypeUsesBoundedAuthoredDictationTolerance()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("listen-type")));
        var acceptedAnswers = new[]
        {
            new TemplateOption("full", "Ich möchte einen Tee, bitte."),
        };

        var accepted = TemplateInteractionEvaluator.EvaluateDictation(
            acceptedAnswers,
            "  ICH   MÖCHTE EINEN TEE, BITTE!  ");
        var blank = TemplateInteractionEvaluator.EvaluateDictation(
            acceptedAnswers,
            "  ");
        var different = TemplateInteractionEvaluator.EvaluateDictation(
            acceptedAnswers,
            "Ich trinke Tee.");

        Assert.AreEqual(TemplateOutcomeState.Success, accepted.State);
        Assert.AreEqual("full", accepted.ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Uncertain, blank.State);
        Assert.AreEqual(TemplateOutcomeState.Failure, different.State);
        Assert.IsNull(different.ResponseId);
    }

    [TestMethod]
    public void ListenTypeRejectsDuplicateNormalizedAcceptedAnswers()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            TemplateInteractionEvaluator.EvaluateDictation(
                [
                    new("one", "Guten Morgen."),
                    new("two", "  GUTEN   MORGEN! "),
                ],
                "Guten Morgen"));
    }

    [TestMethod]
    public void EchoStageMapsOnlySupportedIntelligibilityEvidence()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("echo-stage")));

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluatePronunciationAssessment(
                PronunciationAssessmentOutcome.Intelligible).State);
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluatePronunciationAssessment(
                PronunciationAssessmentOutcome.PartlyIntelligible).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluatePronunciationAssessment(
                PronunciationAssessmentOutcome.NotIntelligible).State);
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluatePronunciationAssessment(
                PronunciationAssessmentOutcome.NoSpeech).State);
    }

    [TestMethod]
    public void ListenRouteUsesOnlyTheAuthoredStopOrder()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("listen-route")));
        var route = new[]
        {
            new TemplateOption("cafe", "Café"),
            new TemplateOption("market", "Markt"),
            new TemplateOption("station", "Bahnhof"),
        };

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                route,
                ["cafe", "market", "station"]).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                route,
                ["station", "market", "cafe"]).State);
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                route,
                ["cafe", "market"]).State);
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
