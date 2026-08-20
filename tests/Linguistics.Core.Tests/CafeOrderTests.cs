using Linguistics.Core.Curriculum;

namespace Linguistics.Core.Tests;

[TestClass]
public sealed class CafeOrderTests
{
    private static readonly DateTimeOffset StartedAt =
        new(2026, 8, 20, 10, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void MissingRequestFrameKeepsTheWaitingState()
    {
        var definition = CreateDefinition();
        var session = CafeOrderSession.Start(definition, StartedAt);

        var result = CafeOrderEngine.Evaluate(definition, session, "Kaffee, bitte");

        Assert.AreEqual(definition.WaitingStateId, result.Session.StateId);
        Assert.AreEqual("task.prompt", result.PrimaryIntervention?.ErrorRuleId);
        Assert.IsFalse(result.StateChanged);
        Assert.IsFalse(result.Completed);
        Assert.AreEqual(1, result.Session.TurnCount);
    }

    [TestMethod]
    public void RequestFrameMovesToItemStateWithoutInventingSuccess()
    {
        var definition = CreateDefinition();

        var result = CafeOrderEngine.Evaluate(
            definition,
            CafeOrderSession.Start(definition, StartedAt),
            "Ich möchte");

        Assert.AreEqual(definition.FrameStateId, result.Session.StateId);
        Assert.IsTrue(result.StateChanged);
        Assert.IsFalse(result.Completed);
        Assert.IsNull(result.Evidence);
        CollectionAssert.AreEqual(
            definition.ScriptedResponses[definition.FrameStateId].ToArray(),
            result.AllowedNpcResponses.ToArray());
    }

    [TestMethod]
    public void ArticleCorrectionWinsAndTheScenarioContextIsRetained()
    {
        var definition = CreateDefinition();
        var session = CafeOrderSession.Start(definition, StartedAt);

        var result = CafeOrderEngine.Evaluate(
            definition,
            session,
            "Ich möchte ein kaffee");

        Assert.AreEqual(definition.FrameStateId, result.Session.StateId);
        Assert.AreEqual(definition.ArticleIntervention, result.PrimaryIntervention);
        Assert.IsTrue(result.OtherObservations.Contains(definition.CapitalizationIntervention));
        Assert.AreEqual(1, result.Session.RetryCount);
        CollectionAssert.AreEquivalent(
            new[] { "de.error.accusative-masculine", "de.error.noun-capitalization" },
            result.Session.EncounteredErrorRuleIds.ToArray());
        Assert.IsEmpty(result.AllowedNpcResponses);
    }

    [TestMethod]
    public void CorrectedRetryCompletesWithSeparateDeterministicEvidence()
    {
        var definition = CreateDefinition();
        var first = CafeOrderEngine.Evaluate(
            definition,
            CafeOrderSession.Start(definition, StartedAt),
            "Ich möchte ein Kaffee, bitte");

        var corrected = CafeOrderEngine.Evaluate(
            definition,
            first.Session,
            "einen Kaffee, bitte");

        Assert.IsTrue(corrected.Completed);
        Assert.AreEqual(definition.CompleteStateId, corrected.Session.StateId);
        Assert.AreEqual(2, corrected.Session.TurnCount);
        Assert.AreEqual(1, corrected.Session.RetryCount);
        Assert.IsTrue(corrected.Evidence?.CommunicativeSuccess);
        Assert.AreEqual(1, corrected.Evidence?.LinguisticAccuracy);
        Assert.AreEqual(0.8, corrected.Evidence!.Fluency!.Value, 0.0001);
        Assert.IsNull(corrected.Evidence?.Pronunciation);
        Assert.IsNotEmpty(corrected.AllowedNpcResponses);
    }

    [TestMethod]
    public void MissingOptionalPolitenessStillCommunicatesButProducesMinorFeedback()
    {
        var definition = CreateDefinition();

        var result = CafeOrderEngine.Evaluate(
            definition,
            CafeOrderSession.Start(definition, StartedAt),
            "Ich möchte einen Kaffee");

        Assert.IsTrue(result.Completed);
        Assert.IsTrue(result.Evidence?.CommunicativeSuccess);
        Assert.AreEqual(0.85, result.Evidence?.LinguisticAccuracy);
        Assert.AreEqual(definition.PolitenessIntervention, result.OtherObservations.Single());
    }

    [TestMethod]
    public void IdenticalStateAndInputProduceIdenticalDecisions()
    {
        var definition = CreateDefinition();
        var session = CafeOrderSession.Start(definition, StartedAt);

        var first = CafeOrderEngine.Evaluate(definition, session, "Ich möchte einen Kaffee, bitte");
        var second = CafeOrderEngine.Evaluate(definition, session, "Ich möchte einen Kaffee, bitte");

        Assert.AreEqual(first.Session.Id, second.Session.Id);
        Assert.AreEqual(first.Session.StateId, second.Session.StateId);
        Assert.AreEqual(first.Session.TurnCount, second.Session.TurnCount);
        Assert.AreEqual(first.Intent, second.Intent);
        Assert.AreEqual(first.Completed, second.Completed);
        Assert.AreEqual(first.Evidence, second.Evidence);
        Assert.AreEqual(first.Explanation, second.Explanation);
        CollectionAssert.AreEqual(
            first.AllowedNpcResponses.ToArray(),
            second.AllowedNpcResponses.ToArray());
        CollectionAssert.AreEqual(
            first.UsedVocabularyIds.ToArray(),
            second.UsedVocabularyIds.ToArray());
    }

    [TestMethod]
    public void CompletedSessionRejectsAnotherTurn()
    {
        var definition = CreateDefinition();
        var completed = CafeOrderEngine.Evaluate(
            definition,
            CafeOrderSession.Start(definition, StartedAt),
            "Ich möchte einen Kaffee, bitte");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            CafeOrderEngine.Evaluate(definition, completed.Session, "Noch einen"));
    }

    [TestMethod]
    public void TaskHistoryAcceptsVersionedMinimalDataAndRejectsInvalidReferences()
    {
        var attempt = CreateAttempt();
        var valid = new TaskHistory(
            [attempt],
            [new ReviewHandoff(
                Guid.NewGuid(),
                attempt.Id,
                new ConceptId("de.function.order-polite"),
                StartedAt.AddMinutes(2),
                attempt.EncounteredErrorRuleIds)]);

        TaskHistoryValidator.Validate(valid);

        var invalidAttempt = attempt with { EvaluationVersion = default };
        var invalid = valid with
        {
            Attempts = [invalidAttempt],
            ReviewHandoffs =
            [
                valid.ReviewHandoffs[0] with
                {
                    TaskAttemptId = Guid.NewGuid(),
                    ErrorRuleIds = ["same", "same"],
                },
            ],
        };
        var exception = Assert.ThrowsExactly<CurriculumValidationException>(() =>
            TaskHistoryValidator.Validate(invalid));

        Assert.IsTrue(exception.Errors.Any(error => error.Contains("Task attempt", StringComparison.Ordinal)));
        Assert.IsTrue(exception.Errors.Any(error => error.Contains("Review handoff", StringComparison.Ordinal)));
    }

    private static CafeOrderDefinition CreateDefinition() =>
        new(
            "de.task.cafe.order-one-item",
            new VersionId("language.de.core.v1"),
            new VersionId("cafe-order-evaluator-v1"),
            new ConceptId("de.function.order-polite"),
            new ConceptId("de.noun.gender-basic"),
            "Order one café drink politely.",
            "A quiet café counter.",
            "Café server",
            ["Use the request frame.", "Name one item."],
            "waiting",
            "frame",
            "complete",
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                ["waiting"] = ["Guten Tag! Was möchten Sie?"],
                ["frame"] = ["Was möchten Sie?"],
                ["complete"] = ["Sehr gern. Ein Kaffee."],
            },
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["de.lexeme.kaffee"] = "Kaffee",
                ["de.lexeme.bitte"] = "bitte",
            },
            new FocusIntervention(
                "de.error.accusative-masculine",
                FeedbackPriority.TargetConcept,
                "Use einen before Kaffee.",
                "Try the item again."),
            new FocusIntervention(
                "de.error.noun-capitalization",
                FeedbackPriority.TargetConcept,
                "Capitalize Kaffee.",
                "Try the item again."),
            new FocusIntervention(
                "de.error.order-bitte",
                FeedbackPriority.Minor,
                "Bitte makes the request warmer.",
                "Optional polish."),
            "Begin with Ich möchte.",
            "Now name the item.");

    private static TaskAttempt CreateAttempt() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "de.task.cafe.order-one-item",
            StartedAt,
            StartedAt.AddMinutes(2),
            TurnCount: 2,
            RetryCount: 1,
            new LearningEvidence(true, 1, 0.8, null, 1, null, null),
            ["de.error.accusative-masculine"],
            new VersionId("language.de.core.v1"),
            new VersionId("cafe-order-evaluator-v1"),
            DialogueRealizationMode.Scripted,
            LocalModel: null,
            DialogueSchemaVersion: "cafe-order-dialogue-v1",
            SelectedBridge: null);
}
