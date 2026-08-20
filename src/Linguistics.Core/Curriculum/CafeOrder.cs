using System.Text;
using Linguistics.Core.Speech;

namespace Linguistics.Core.Curriculum;

public enum FeedbackPriority
{
    CommunicationBlocking,
    TargetConcept,
    Repeated,
    Intelligibility,
    Minor,
}

public enum DialogueRealizationMode
{
    Scripted,
    LocalModel,
}

public sealed record FocusIntervention(
    string ErrorRuleId,
    FeedbackPriority Priority,
    string Message,
    string RetryPrompt);

public sealed record CafeOrderDefinition(
    string TaskId,
    VersionId ContentVersion,
    VersionId EvaluationVersion,
    ConceptId TargetConceptId,
    ConceptId BridgeConceptId,
    string Goal,
    string Context,
    string NpcRole,
    IReadOnlyList<string> SuccessCriteria,
    string WaitingStateId,
    string FrameStateId,
    string CompleteStateId,
    IReadOnlyDictionary<string, IReadOnlyList<string>> ScriptedResponses,
    IReadOnlyDictionary<string, string> Vocabulary,
    FocusIntervention ArticleIntervention,
    FocusIntervention CapitalizationIntervention,
    FocusIntervention PolitenessIntervention,
    string FrameHint,
    string ItemHint,
    string PronunciationTargetText);

public sealed record CafeOrderSession(
    Guid Id,
    string StateId,
    int TurnCount,
    int RetryCount,
    DateTimeOffset StartedAt,
    IReadOnlyList<string> EncounteredErrorRuleIds)
{
    public static CafeOrderSession Start(CafeOrderDefinition definition, DateTimeOffset startedAt) =>
        new(Guid.NewGuid(), definition.WaitingStateId, 0, 0, startedAt, []);
}

public sealed record CafeOrderTurnResult(
    CafeOrderSession Session,
    string PreviousStateId,
    string Intent,
    bool StateChanged,
    bool Completed,
    FocusIntervention? PrimaryIntervention,
    IReadOnlyList<FocusIntervention> OtherObservations,
    IReadOnlyList<string> AllowedNpcResponses,
    IReadOnlyList<string> UsedVocabularyIds,
    LearningEvidence? Evidence,
    string Explanation);

public static class CafeOrderEngine
{
    public static CafeOrderTurnResult Evaluate(
        CafeOrderDefinition definition,
        CafeOrderSession session,
        string learnerText)
    {
        ValidateDefinition(definition);
        ArgumentNullException.ThrowIfNull(session);
        if (session.Id == Guid.Empty ||
            session.TurnCount < 0 ||
            session.RetryCount < 0 ||
            session.StartedAt == default ||
            session.EncounteredErrorRuleIds is null)
        {
            throw new ArgumentException("The café session is invalid.", nameof(session));
        }

        if (session.StateId == definition.CompleteStateId)
        {
            throw new InvalidOperationException("A completed café session cannot accept another turn.");
        }

        if (session.StateId != definition.WaitingStateId && session.StateId != definition.FrameStateId)
        {
            throw new ArgumentException("The café session has an unknown state.", nameof(session));
        }

        if (string.IsNullOrWhiteSpace(learnerText) || learnerText.Length > 500)
        {
            return Prompt(
                definition,
                session,
                session.StateId == definition.WaitingStateId ? definition.FrameHint : definition.ItemHint,
                "No bounded learner input was available.");
        }

        var tokens = Tokenize(learnerText);
        var lower = tokens.Select(token => token.ToLowerInvariant()).ToArray();
        var previousState = session.StateId;
        var turnCount = session.TurnCount + 1;
        var hasFrame = HasSequence(lower, "ich", "möchte");
        var hasItem = lower.Contains("kaffee", StringComparer.Ordinal);

        if (session.StateId == definition.WaitingStateId && !hasFrame)
        {
            return Prompt(
                definition,
                session with { TurnCount = turnCount },
                definition.FrameHint,
                "The required request frame was not detected.");
        }

        if (!hasItem)
        {
            var next = session with
            {
                StateId = definition.FrameStateId,
                TurnCount = turnCount,
            };
            return new CafeOrderTurnResult(
                next,
                previousState,
                "requestItem",
                next.StateId != previousState,
                Completed: false,
                PrimaryIntervention: null,
                OtherObservations: [],
                Responses(definition, definition.FrameStateId),
                [],
                Evidence: null,
                "The request frame was detected; the deterministic task now requires the item.");
        }

        var targetErrors = new List<FocusIntervention>();
        if (!HasSequence(lower, "einen", "kaffee"))
        {
            targetErrors.Add(definition.ArticleIntervention);
        }

        if (!tokens.Contains("Kaffee", StringComparer.Ordinal))
        {
            targetErrors.Add(definition.CapitalizationIntervention);
        }

        if (targetErrors.Count > 0)
        {
            var primary = targetErrors
                .OrderBy(intervention => InterventionRank(definition, intervention))
                .First();
            var encountered = session.EncounteredErrorRuleIds
                .Concat(targetErrors.Select(error => error.ErrorRuleId))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();
            var next = session with
            {
                StateId = definition.FrameStateId,
                TurnCount = turnCount,
                RetryCount = session.RetryCount + 1,
                EncounteredErrorRuleIds = encountered,
            };
            return new CafeOrderTurnResult(
                next,
                previousState,
                "nameItem",
                next.StateId != previousState,
                Completed: false,
                primary,
                targetErrors.Where(error => error != primary).ToArray(),
                [],
                ["de.lexeme.kaffee"],
                Evidence: null,
                $"The highest-ranked target-concept intervention is '{primary.ErrorRuleId}'; context is retained for retry.");
        }

        var hasBitte = lower.Contains("bitte", StringComparer.Ordinal);
        var observations = hasBitte ? [] : new[] { definition.PolitenessIntervention };
        var completedSession = session with
        {
            StateId = definition.CompleteStateId,
            TurnCount = turnCount,
        };
        var fluency = Math.Max(0.5, 1 - completedSession.RetryCount * 0.15 - (turnCount - 1) * 0.05);
        var accuracy = hasBitte ? 1 : 0.85;
        var evidence = new LearningEvidence(
            CommunicativeSuccess: true,
            LinguisticAccuracy: accuracy,
            Fluency: fluency,
            Pronunciation: null,
            TargetConceptPerformance: accuracy,
            Comprehension: null,
            DelayedRecall: null);
        return new CafeOrderTurnResult(
            completedSession,
            previousState,
            "nameItem",
            StateChanged: true,
            Completed: true,
            PrimaryIntervention: null,
            OtherObservations: observations,
            Responses(definition, definition.CompleteStateId),
            ["de.lexeme.kaffee"],
            evidence,
            "The deterministic request frame, item, article, and capitalization checks passed; communicative success is independent of the optional politeness observation.");
    }

    private static CafeOrderTurnResult Prompt(
        CafeOrderDefinition definition,
        CafeOrderSession session,
        string hint,
        string explanation) =>
        new(
            session,
            session.StateId,
            "retry",
            StateChanged: false,
            Completed: false,
            new FocusIntervention("task.prompt", FeedbackPriority.CommunicationBlocking, hint, "Try again."),
            [],
            [],
            [],
            Evidence: null,
            explanation);

    private static IReadOnlyList<string> Responses(CafeOrderDefinition definition, string stateId) =>
        definition.ScriptedResponses.TryGetValue(stateId, out var responses) ? responses : [];

    private static int InterventionRank(
        CafeOrderDefinition definition,
        FocusIntervention intervention)
    {
        if (intervention.ErrorRuleId == definition.ArticleIntervention.ErrorRuleId)
        {
            return 0;
        }

        if (intervention.ErrorRuleId == definition.CapitalizationIntervention.ErrorRuleId)
        {
            return 1;
        }

        return 2 + (int)intervention.Priority;
    }

    private static string[] Tokenize(string value)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        foreach (var character in value.Normalize())
        {
            if (char.IsLetter(character))
            {
                current.Append(character);
            }
            else if (current.Length > 0)
            {
                tokens.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens.ToArray();
    }

    private static bool HasSequence(IReadOnlyList<string> tokens, params string[] expected)
    {
        for (var start = 0; start <= tokens.Count - expected.Length; start++)
        {
            if (expected.Select((value, offset) => tokens[start + offset] == value).All(match => match))
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateDefinition(CafeOrderDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var required = new[]
        {
            definition.TaskId,
            definition.TargetConceptId.Value,
            definition.BridgeConceptId.Value,
            definition.Goal,
            definition.Context,
            definition.NpcRole,
            definition.WaitingStateId,
            definition.FrameStateId,
            definition.CompleteStateId,
            definition.FrameHint,
            definition.ItemHint,
            definition.PronunciationTargetText,
        };
        if (required.Any(string.IsNullOrWhiteSpace) ||
            definition.ScriptedResponses is null ||
            definition.SuccessCriteria is null ||
            definition.SuccessCriteria.Count == 0 ||
            definition.SuccessCriteria.Any(string.IsNullOrWhiteSpace) ||
            !definition.ScriptedResponses.ContainsKey(definition.WaitingStateId) ||
            !definition.ScriptedResponses.ContainsKey(definition.FrameStateId) ||
            !definition.ScriptedResponses.ContainsKey(definition.CompleteStateId) ||
            definition.Vocabulary is null ||
            definition.ArticleIntervention is null ||
            definition.CapitalizationIntervention is null ||
            definition.PolitenessIntervention is null)
        {
            throw new ArgumentException("The café task definition is incomplete.", nameof(definition));
        }
    }
}

public sealed record TaskAttempt(
    Guid Id,
    Guid SessionId,
    string TaskId,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    int TurnCount,
    int RetryCount,
    LearningEvidence Evidence,
    IReadOnlyList<string> EncounteredErrorRuleIds,
    VersionId ContentVersion,
    VersionId EvaluationVersion,
    DialogueRealizationMode DialogueMode,
    string? LocalModel,
    string DialogueSchemaVersion,
    SelectedBridgeReference? SelectedBridge,
    LearnerInputMode InputMode = LearnerInputMode.Text,
    PronunciationEvidence? SpeechEvidence = null);

public sealed record ReviewHandoff(
    Guid Id,
    Guid TaskAttemptId,
    ConceptId ConceptId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> ErrorRuleIds);

public sealed record TaskHistory(
    IReadOnlyList<TaskAttempt> Attempts,
    IReadOnlyList<ReviewHandoff> ReviewHandoffs)
{
    public static TaskHistory Empty => new([], []);
}

public static class TaskHistoryValidator
{
    public static void Validate(TaskHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);
        var errors = new List<string>();
        if (history.Attempts is null || history.ReviewHandoffs is null)
        {
            errors.Add("Task-history collections are required.");
        }

        var attempts = (history.Attempts ?? []).OfType<TaskAttempt>().ToArray();
        if (history.Attempts is not null && attempts.Length != history.Attempts.Count)
        {
            errors.Add("A task attempt is missing.");
        }

        foreach (var duplicate in attempts.GroupBy(attempt => attempt.Id).Where(group => group.Count() > 1))
        {
            errors.Add($"Task attempt '{duplicate.Key}' appears more than once.");
        }

        foreach (var attempt in attempts)
        {
            if (attempt.Id == Guid.Empty ||
                attempt.SessionId == Guid.Empty ||
                string.IsNullOrWhiteSpace(attempt.TaskId) ||
                attempt.StartedAt == default ||
                attempt.CompletedAt == default ||
                attempt.CompletedAt < attempt.StartedAt ||
                attempt.TurnCount <= 0 ||
                attempt.RetryCount < 0 ||
                attempt.RetryCount >= attempt.TurnCount ||
                attempt.EncounteredErrorRuleIds is null ||
                attempt.EncounteredErrorRuleIds.Any(string.IsNullOrWhiteSpace) ||
                attempt.EncounteredErrorRuleIds.Count != attempt.EncounteredErrorRuleIds.Distinct(StringComparer.Ordinal).Count() ||
                string.IsNullOrWhiteSpace(attempt.ContentVersion.Value) ||
                string.IsNullOrWhiteSpace(attempt.EvaluationVersion.Value) ||
                string.IsNullOrWhiteSpace(attempt.DialogueSchemaVersion) ||
                !Enum.IsDefined(attempt.DialogueMode) ||
                (attempt.DialogueMode == DialogueRealizationMode.Scripted && attempt.LocalModel is not null) ||
                (attempt.DialogueMode == DialogueRealizationMode.LocalModel && string.IsNullOrWhiteSpace(attempt.LocalModel)) ||
                !Enum.IsDefined(attempt.InputMode) ||
                (attempt.InputMode == LearnerInputMode.Text && attempt.SpeechEvidence is not null) ||
                (attempt.InputMode == LearnerInputMode.Speech && attempt.SpeechEvidence is null) ||
                (attempt.SpeechEvidence is { } speech &&
                 (speech.Intelligibility != attempt.Evidence.Pronunciation ||
                  speech.Intelligibility is null or < 0 or > 1 ||
                  speech.Outcome == PronunciationAssessmentOutcome.NoSpeech ||
                  !Enum.IsDefined(speech.Outcome) ||
                  speech.ExpectedWordCount <= 0 ||
                  speech.RecognizedWordCount <= 0 ||
                  speech.MatchedWordCount < 0 ||
                  speech.MatchedWordCount > speech.ExpectedWordCount ||
                  speech.MatchedWordCount > speech.RecognizedWordCount ||
                  speech.Duration < TimeSpan.Zero ||
                  string.IsNullOrWhiteSpace(speech.RecognitionProviderVersion) ||
                  string.IsNullOrWhiteSpace(speech.AssessmentVersion))))
            {
                errors.Add($"Task attempt '{attempt?.Id}' is invalid.");
                continue;
            }

            try
            {
                CurriculumHistoryValidator.ValidateAttempt(new ConceptAttempt(
                    attempt.Id,
                    new ConceptId("task.outcome"),
                    attempt.CompletedAt,
                    attempt.Evidence,
                    attempt.ContentVersion,
                    ProgressionConfiguration.Default.Version,
                    ConceptSelectionConfiguration.Default.Version,
                    attempt.SelectedBridge));
            }
            catch (CurriculumValidationException exception)
            {
                errors.Add($"Task attempt '{attempt.Id}' evidence is invalid: {exception.Message}");
            }
        }

        var attemptIds = attempts.Select(attempt => attempt.Id).ToHashSet();
        var handoffs = (history.ReviewHandoffs ?? []).OfType<ReviewHandoff>().ToArray();
        if (history.ReviewHandoffs is not null && handoffs.Length != history.ReviewHandoffs.Count)
        {
            errors.Add("A review handoff is missing.");
        }

        foreach (var duplicate in handoffs.GroupBy(handoff => handoff.Id).Where(group => group.Count() > 1))
        {
            errors.Add($"Review handoff '{duplicate.Key}' appears more than once.");
        }

        foreach (var handoff in handoffs)
        {
            if (handoff.Id == Guid.Empty ||
                handoff.TaskAttemptId == Guid.Empty ||
                !attemptIds.Contains(handoff.TaskAttemptId) ||
                string.IsNullOrWhiteSpace(handoff.ConceptId.Value) ||
                handoff.CreatedAt == default ||
                handoff.ErrorRuleIds is null ||
                handoff.ErrorRuleIds.Any(string.IsNullOrWhiteSpace) ||
                handoff.ErrorRuleIds.Count != handoff.ErrorRuleIds.Distinct(StringComparer.Ordinal).Count())
            {
                errors.Add($"Review handoff '{handoff?.Id}' is invalid.");
            }
        }

        if (errors.Count > 0)
        {
            throw new CurriculumValidationException(errors);
        }
    }
}
