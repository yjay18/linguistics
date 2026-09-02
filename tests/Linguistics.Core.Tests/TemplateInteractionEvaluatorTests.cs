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
    [DataRow("long-short-vowel")]
    [DataRow("sign-reading")]
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
    public void ReadAloudCardUsesDeterministicWordingAndIntelligibilityPaths()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("read-aloud-card")));
        var accepted = new[]
        {
            new TemplateOption("full", "Guten Morgen. Einen Kaffee, bitte."),
        };

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateDictation(
                accepted,
                "GUTEN MORGEN. EINEN KAFFEE, BITTE!").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateDictation(
                accepted,
                "Guten Abend.").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluatePronunciationAssessment(
                PronunciationAssessmentOutcome.NotIntelligible).State);
    }

    [TestMethod]
    public void PromptRespondSelectsTheBestAuthoredVoiceOrTextResponse()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("prompt-respond")));
        var accepted = new[]
        {
            new TemplateOption("full", "Ich möchte einen Tee, bitte."),
            new TemplateOption("short", "Einen Tee, bitte."),
        };

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateDictation(
                accepted,
                "Einen Tee, bitte!").State);
        var voice = TemplateInteractionEvaluator.EvaluateBestPronunciationAssessment(
        [
            new("full", PronunciationAssessmentOutcome.PartlyIntelligible),
            new("short", PronunciationAssessmentOutcome.Intelligible),
        ]);
        Assert.AreEqual(TemplateOutcomeState.Success, voice.State);
        Assert.AreEqual("short", voice.ResponseId);
        Assert.ThrowsExactly<ArgumentException>(() =>
            TemplateInteractionEvaluator.EvaluateBestPronunciationAssessment(
            [
                new("same", PronunciationAssessmentOutcome.Intelligible),
                new("same", PronunciationAssessmentOutcome.NotIntelligible),
            ]));
    }

    [TestMethod]
    public void SyllableClapUsesOnlyAuthoredBeatCountAndTimingBounds()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("syllable-clap")));
        var minimum = TimeSpan.FromMilliseconds(180);
        var maximum = TimeSpan.FromMilliseconds(900);

        var success = TemplateInteractionEvaluator.EvaluateTapRhythm(
            2,
            minimum,
            maximum,
            [TimeSpan.Zero, TimeSpan.FromMilliseconds(480)]);
        var tooFast = TemplateInteractionEvaluator.EvaluateTapRhythm(
            2,
            minimum,
            maximum,
            [TimeSpan.Zero, TimeSpan.FromMilliseconds(80)]);
        var incomplete = TemplateInteractionEvaluator.EvaluateTapRhythm(
            2,
            minimum,
            maximum,
            [TimeSpan.Zero]);
        var extra = TemplateInteractionEvaluator.EvaluateTapRhythm(
            2,
            minimum,
            maximum,
            [TimeSpan.Zero, TimeSpan.FromMilliseconds(480), TimeSpan.FromMilliseconds(960)]);

        Assert.AreEqual(TemplateOutcomeState.Success, success.State);
        Assert.AreEqual(TemplateOutcomeState.Failure, tooFast.State);
        Assert.AreEqual(TemplateOutcomeState.Uncertain, incomplete.State);
        Assert.AreEqual(TemplateOutcomeState.Failure, extra.State);
        CollectionAssert.AreEqual(
            new[] { "tap-1", "tap-2" },
            success.OrderedOptionIds!.ToArray());
        Assert.ThrowsExactly<ArgumentException>(() =>
            TemplateInteractionEvaluator.EvaluateTapRhythm(
                2,
                minimum,
                maximum,
                [TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(20)]));
    }

    [TestMethod]
    public void LongShortVowelUsesOnlyTheAuthoredChoiceForItsOutcome()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("long-short-vowel")));
        var options = new[]
        {
            new TemplateOption("short", "kurz · Stadt"),
            new TemplateOption("long", "lang · Staat"),
        };

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                options,
                "long",
                "long").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                options,
                "long",
                "short").State);
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                options,
                "long",
                null).State);
    }

    [TestMethod]
    public void SignReadingUsesOnlyTheAuthoredChoiceForItsOutcome()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("sign-reading")));
        var options = new[]
        {
            new TemplateOption("customers", "Nur Kunden"),
            new TemplateOption("everyone", "Alle Personen"),
            new TemplateOption("staff", "Nur Mitarbeitende"),
        };

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                options,
                "customers",
                "customers").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                options,
                "customers",
                "everyone").State);
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                options,
                "customers",
                null).State);
    }

    [TestMethod]
    public void BridgeNoteKeepsDismissalAdvisoryAndReportsOnlyActionIds()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("bridge-note")));
        var actions = new[]
        {
            new TemplateOption("use-bridge", "Use this bridge"),
            new TemplateOption("dismiss-bridge", "Dismiss note"),
        };

        var pending = TemplateInteractionEvaluator.EvaluateAdvisoryChoice(
            actions,
            "use-bridge",
            null);
        var dismissed = TemplateInteractionEvaluator.EvaluateAdvisoryChoice(
            actions,
            "use-bridge",
            "dismiss-bridge");
        var acknowledged = TemplateInteractionEvaluator.EvaluateAdvisoryChoice(
            actions,
            "use-bridge",
            "use-bridge");

        Assert.AreEqual(TemplateOutcomeState.Uncertain, pending.State);
        Assert.AreEqual(TemplateOutcomeState.Ready, dismissed.State);
        Assert.AreEqual("dismiss-bridge", dismissed.ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Success, acknowledged.State);
        Assert.AreEqual("use-bridge", acknowledged.ResponseId);
        Assert.AreNotEqual("Use this bridge", acknowledged.ResponseId);
        Assert.ThrowsExactly<ArgumentException>(() =>
            TemplateInteractionEvaluator.EvaluateAdvisoryChoice(
                actions,
                "use-bridge",
                "invented-action"));
    }

    [TestMethod]
    public void FalseFriendAlarmReportsOnlyAuthoredAdvisoryActionIds()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("false-friend-alarm")));
        var actions = new[]
        {
            new TemplateOption("notice-capital", "I noticed the capital"),
            new TemplateOption("dismiss-alarm", "Dismiss alert"),
        };

        var acknowledged = TemplateInteractionEvaluator.EvaluateAdvisoryChoice(
            actions,
            "notice-capital",
            "notice-capital");
        var dismissed = TemplateInteractionEvaluator.EvaluateAdvisoryChoice(
            actions,
            "notice-capital",
            "dismiss-alarm");

        Assert.AreEqual(TemplateOutcomeState.Success, acknowledged.State);
        Assert.AreEqual("notice-capital", acknowledged.ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Ready, dismissed.State);
        Assert.AreEqual("dismiss-alarm", dismissed.ResponseId);
        Assert.AreNotEqual("kaffee", dismissed.ResponseId);
    }

    [TestMethod]
    public void CognateThreadReportsOnlyAuthoredAdvisoryActionIds()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("cognate-thread")));
        var actions = new[]
        {
            new TemplateOption("trace-thread", "Trace this connection"),
            new TemplateOption("dismiss-thread", "Dismiss thread"),
        };

        var traced = TemplateInteractionEvaluator.EvaluateAdvisoryChoice(
            actions,
            "trace-thread",
            "trace-thread");
        var dismissed = TemplateInteractionEvaluator.EvaluateAdvisoryChoice(
            actions,
            "trace-thread",
            "dismiss-thread");

        Assert.AreEqual(TemplateOutcomeState.Success, traced.State);
        Assert.AreEqual("trace-thread", traced.ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Ready, dismissed.State);
        Assert.AreEqual("dismiss-thread", dismissed.ResponseId);
        Assert.AreNotEqual("name", traced.ResponseId);
    }

    [TestMethod]
    public void FormFillReturnsOnlyAuthoredFieldIds()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("form-fill")));
        var expected = new[]
        {
            new TemplateOption("name", "Mina Weber"),
            new TemplateOption("origin", "Berlin"),
            new TemplateOption("address", "Marktstraße 5"),
        };

        var success = TemplateInteractionEvaluator.EvaluateTextFields(
            expected,
            new Dictionary<string, string>
            {
                ["name"] = "mina weber",
                ["origin"] = "Berlin.",
                ["address"] = "Marktstraße 5",
            });
        var failure = TemplateInteractionEvaluator.EvaluateTextFields(
            expected,
            new Dictionary<string, string>
            {
                ["name"] = "Mina Weber",
                ["origin"] = "Hamburg",
                ["address"] = "Marktstraße 5",
            });
        var incomplete = TemplateInteractionEvaluator.EvaluateTextFields(
            expected,
            new Dictionary<string, string>
            {
                ["name"] = "Mina Weber",
            });

        Assert.AreEqual(TemplateOutcomeState.Success, success.State);
        Assert.AreEqual(TemplateOutcomeState.Failure, failure.State);
        Assert.AreEqual(TemplateOutcomeState.Uncertain, incomplete.State);
        CollectionAssert.AreEqual(
            new[] { "name", "origin", "address" },
            success.OrderedOptionIds!.ToArray());
        CollectionAssert.AreEqual(new[] { "name" }, incomplete.OrderedOptionIds!.ToArray());
        Assert.IsFalse(success.OrderedOptionIds!.Contains("Mina Weber", StringComparer.Ordinal));
        Assert.ThrowsExactly<ArgumentException>(() =>
            TemplateInteractionEvaluator.EvaluateTextFields(
                expected,
                new Dictionary<string, string> { ["unknown"] = "value" }));
    }

    [TestMethod]
    public void NoteWriteMatchesAuthoredContentWithoutReportingLearnerText()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("note-write")));
        var requiredContent = new[]
        {
            new TemplateOption("location", "auf dem Markt"),
            new TemplateOption("return-time", "um sechs Uhr"),
        };

        var success = TemplateInteractionEvaluator.EvaluateRequiredContent(
            requiredContent,
            "Hallo Sam! Ich bin auf dem MARKT; ich komme um sechs Uhr zurück.");
        var failure = TemplateInteractionEvaluator.EvaluateRequiredContent(
            requiredContent,
            "Ich bin auf dem Markt.");
        var incomplete = TemplateInteractionEvaluator.EvaluateRequiredContent(
            requiredContent,
            "!!!");

        Assert.AreEqual(TemplateOutcomeState.Success, success.State);
        Assert.AreEqual(TemplateOutcomeState.Failure, failure.State);
        Assert.AreEqual(TemplateOutcomeState.Uncertain, incomplete.State);
        CollectionAssert.AreEqual(
            new[] { "location", "return-time" },
            success.OrderedOptionIds!.ToArray());
        CollectionAssert.AreEqual(new[] { "location" }, failure.OrderedOptionIds!.ToArray());
        Assert.IsFalse(success.OrderedOptionIds!.Contains("auf dem Markt", StringComparer.Ordinal));
        Assert.ThrowsExactly<ArgumentException>(() =>
            TemplateInteractionEvaluator.EvaluateRequiredContent(
                Array.Empty<TemplateOption>(),
                "Text"));
    }

    [TestMethod]
    public void MenuReadMapsOnlyAuthoredPriceOptionIds()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("menu-read")));
        var options = new[]
        {
            new TemplateOption("price-280", "2,80 €"),
            new TemplateOption("price-340", "3,40 €"),
            new TemplateOption("price-420", "4,20 €"),
        };

        var incomplete = TemplateInteractionEvaluator.EvaluateSingleSelection(
            options,
            "price-340",
            null);
        var failure = TemplateInteractionEvaluator.EvaluateSingleSelection(
            options,
            "price-340",
            "price-280");
        var success = TemplateInteractionEvaluator.EvaluateSingleSelection(
            options,
            "price-340",
            "price-340");

        Assert.AreEqual(TemplateOutcomeState.Uncertain, incomplete.State);
        Assert.AreEqual(TemplateOutcomeState.Failure, failure.State);
        Assert.AreEqual(TemplateOutcomeState.Success, success.State);
        Assert.AreEqual("price-280", failure.ResponseId);
        Assert.AreEqual("price-340", success.ResponseId);
        Assert.AreNotEqual("3,40 €", success.ResponseId);
    }

    [TestMethod]
    public void ScheduleReadMapsOnlyAuthoredTimeOptionIds()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("schedule-read")));
        var options = new[]
        {
            new TemplateOption("time-0900", "09:00 Uhr"),
            new TemplateOption("time-1000", "10:00 Uhr"),
            new TemplateOption("time-1100", "11:00 Uhr"),
        };

        var incomplete = TemplateInteractionEvaluator.EvaluateSingleSelection(
            options,
            "time-1000",
            null);
        var failure = TemplateInteractionEvaluator.EvaluateSingleSelection(
            options,
            "time-1000",
            "time-0900");
        var success = TemplateInteractionEvaluator.EvaluateSingleSelection(
            options,
            "time-1000",
            "time-1000");

        Assert.AreEqual(TemplateOutcomeState.Uncertain, incomplete.State);
        Assert.AreEqual(TemplateOutcomeState.Failure, failure.State);
        Assert.AreEqual(TemplateOutcomeState.Success, success.State);
        Assert.AreEqual("time-0900", failure.ResponseId);
        Assert.AreEqual("time-1000", success.ResponseId);
        Assert.AreNotEqual("10:00 Uhr", success.ResponseId);
    }

    [TestMethod]
    public void SpellingTilesMapOnlyAuthoredLetterTileIds()
    {
        Assert.IsTrue(LessonTemplateSchemas.All.Any(schema =>
            schema.Id == new TemplateId("spelling-tiles")));
        var letters = new[]
        {
            new TemplateOption("letter-a", "A"),
            new TemplateOption("letter-p", "P"),
            new TemplateOption("letter-f", "F"),
            new TemplateOption("letter-e", "E"),
            new TemplateOption("letter-l", "L"),
        };

        var incomplete = TemplateInteractionEvaluator.EvaluateWordOrder(
            letters,
            ["letter-a", "letter-p"]);
        var failure = TemplateInteractionEvaluator.EvaluateWordOrder(
            letters,
            ["letter-p", "letter-a", "letter-f", "letter-e", "letter-l"]);
        var success = TemplateInteractionEvaluator.EvaluateWordOrder(
            letters,
            ["letter-a", "letter-p", "letter-f", "letter-e", "letter-l"]);

        Assert.AreEqual(TemplateOutcomeState.Uncertain, incomplete.State);
        Assert.AreEqual(TemplateOutcomeState.Failure, failure.State);
        Assert.AreEqual(TemplateOutcomeState.Success, success.State);
        CollectionAssert.AreEqual(
            new[] { "letter-a", "letter-p", "letter-f", "letter-e", "letter-l" },
            success.OrderedOptionIds!.ToArray());
        Assert.IsFalse(success.OrderedOptionIds!.Contains("A", StringComparer.Ordinal));
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
