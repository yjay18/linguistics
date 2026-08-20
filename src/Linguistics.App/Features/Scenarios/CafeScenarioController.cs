using Linguistics.Core.Content;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Providers;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Scenarios;

public sealed record CafeBridgePresentation(
    LanguageCode SourceLanguage,
    TransferRelation Relation,
    string Explanation,
    IReadOnlyList<string> Risks,
    bool RequiresConfirmation,
    SelectedBridgeReference Reference);

public sealed record CafeScenarioInitialization(
    bool CanStart,
    string Message,
    ConceptProgressState TargetProgressState,
    IReadOnlyList<string> MissingPrerequisiteTitles,
    CafeBridgePresentation? Bridge,
    int PreviousCompletions);

public sealed record CafeScenarioTurnOutcome(
    CafeOrderTurnResult Evaluation,
    string? NpcResponse,
    DialogueRealizationMode DialogueMode,
    string ModelMessage,
    LanguageModelDiagnostic? ModelDiagnostic,
    bool Persisted,
    string? PersistenceError,
    ConceptProgressState? UpdatedProgressState);

public sealed record CafePersistenceResult(
    bool Persisted,
    string Message,
    ConceptProgressState? UpdatedProgressState);

public sealed class CafeScenarioController
{
    private readonly LearnerProfile _profile;
    private readonly LearnerProfileOwner _profileOwner;
    private readonly ConceptGraph _graph;
    private readonly CafeOrderDefinition _definition;
    private readonly IReadOnlyList<TransferNote> _transferNotes;
    private readonly ILanguageModelProvider? _languageModelProvider;
    private readonly Func<DateTimeOffset> _clock;
    private readonly SemaphoreSlim _turnGate = new(1, 1);

    private LearnerLearningState? _learningState;
    private ConceptProgress? _targetProgress;
    private CafeBridgePresentation? _bridge;
    private SelectedBridgeReference? _selectedBridge;
    private CafeOrderSession? _session;
    private LearnerLearningState? _pendingLearningState;
    private ConceptProgressState? _pendingProgressState;

    private CafeScenarioController(
        LearnerProfile profile,
        LearnerProfileOwner profileOwner,
        ConceptGraph graph,
        CafeOrderDefinition definition,
        IReadOnlyList<TransferNote> transferNotes,
        ILanguageModelProvider? languageModelProvider,
        Func<DateTimeOffset>? clock)
    {
        _profile = profile;
        _profileOwner = profileOwner;
        _graph = graph;
        _definition = definition;
        _transferNotes = transferNotes;
        _languageModelProvider = languageModelProvider;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public CafeOrderDefinition Definition => _definition;

    public CafeOrderSession? Session => _session;

    public static CafeScenarioController Create(
        LearnerProfile profile,
        LearnerProfileOwner profileOwner,
        ValidatedContentCatalog runtimeCatalog,
        ILanguageModelProvider? languageModelProvider = null,
        Func<DateTimeOffset>? clock = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(profileOwner);
        ArgumentNullException.ThrowIfNull(runtimeCatalog);

        var german = new LanguageCode("de");
        var transferNotes = profile.KnownLanguages
            .SelectMany(language => runtimeCatalog.CreateRuntimeTransferNotes(language.Language, german))
            .GroupBy(note => note.Mapping.Id)
            .Select(group => group.Single())
            .OrderBy(note => note.Mapping.Id.Value, StringComparer.Ordinal)
            .ToArray();
        return new CafeScenarioController(
            profile,
            profileOwner,
            runtimeCatalog.CreateRuntimeConceptGraph(german),
            runtimeCatalog.CreateRuntimeCafeOrderDefinition(),
            transferNotes,
            languageModelProvider,
            clock);
    }

    internal static CafeScenarioController CreateFromResources(
        LearnerProfile profile,
        LearnerProfileOwner profileOwner,
        ConceptGraph graph,
        CafeOrderDefinition definition,
        IReadOnlyList<TransferNote> transferNotes,
        ILanguageModelProvider? languageModelProvider = null,
        Func<DateTimeOffset>? clock = null) =>
        new(
            profile,
            profileOwner,
            graph,
            definition,
            transferNotes,
            languageModelProvider,
            clock);

    public async Task<CafeScenarioInitialization> InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        _learningState = await _profileOwner
            .LoadLearningStateAsync(cancellationToken)
            .ConfigureAwait(false);

        var target = _graph.Get(_definition.TargetConceptId);
        if (_profile.TargetLanguage != target.TargetLanguage)
        {
            _targetProgress = ConceptProgress.Locked(target.Id);
            return new CafeScenarioInitialization(
                CanStart: false,
                "This first scenario currently supports German learner profiles only.",
                _targetProgress.State,
                [],
                Bridge: null,
                PreviousCompletions: 0);
        }

        var progressById = _learningState.Curriculum.Progress
            .ToDictionary(progress => progress.ConceptId);
        _targetProgress = progressById.TryGetValue(target.Id, out var stored)
            ? stored
            : ConceptProgress.Locked(target.Id);
        var stateById = progressById.ToDictionary(pair => pair.Key, pair => pair.Value.State);
        var prerequisitesReady = _graph.IsReady(target.Id, stateById);
        if (_targetProgress.State == ConceptProgressState.Locked && prerequisitesReady)
        {
            _targetProgress = ConceptProgression.Advance(
                _targetProgress,
                prerequisitesSatisfied: true,
                attempt: null,
                _clock(),
                ProgressionConfiguration.Default).Current;
        }

        _bridge = SelectBridge();
        var missing = target.Prerequisites
            .Where(id => !stateById.TryGetValue(id, out var state) ||
                         state is not (ConceptProgressState.ProvisionallyMastered or ConceptProgressState.Mastered))
            .Select(id => _graph.Get(id).Title)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var previousCompletions = _learningState.Tasks.Attempts.Count(attempt =>
            attempt.TaskId == _definition.TaskId);

        return new CafeScenarioInitialization(
            _targetProgress.State != ConceptProgressState.Locked,
            _targetProgress.State == ConceptProgressState.Locked
                ? "Build the prerequisite capabilities first; the scenario will unlock deterministically."
                : "Ready for a short, text-first café exchange. Scripted dialogue is always available.",
            _targetProgress.State,
            missing,
            _bridge,
            previousCompletions);
    }

    public string Start(bool useConfirmedBridge)
    {
        if (_learningState is null || _targetProgress is null)
        {
            throw new InvalidOperationException("Initialize the café scenario before starting it.");
        }

        if (_targetProgress.State == ConceptProgressState.Locked)
        {
            throw new InvalidOperationException("The café scenario prerequisites are not ready.");
        }

        if (_pendingLearningState is not null)
        {
            throw new InvalidOperationException("Retry the pending local save before starting again.");
        }

        _selectedBridge = _bridge is null ||
                          (_bridge.RequiresConfirmation && !useConfirmedBridge)
            ? null
            : _bridge.Reference;
        _session = CafeOrderSession.Start(_definition, _clock());
        return _definition.ScriptedResponses[_definition.WaitingStateId][0];
    }

    public async Task<CafeScenarioTurnOutcome> SubmitAsync(
        string learnerText,
        CancellationToken cancellationToken = default)
    {
        await _turnGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_session is null)
            {
                throw new InvalidOperationException("Start the café scenario before submitting a turn.");
            }

            if (_pendingLearningState is not null)
            {
                throw new InvalidOperationException("Retry the pending local save before continuing.");
            }

            var evaluation = CafeOrderEngine.Evaluate(_definition, _session, learnerText);
            _session = evaluation.Session;
            var realization = await RealizeNpcResponseAsync(
                evaluation,
                learnerText,
                cancellationToken).ConfigureAwait(false);

            var persisted = false;
            string? persistenceError = null;
            ConceptProgressState? progressState = null;
            if (evaluation.Completed)
            {
                BuildPendingCompletion(evaluation, realization.Mode, realization.Model);
                var persistence = await SavePendingAsync().ConfigureAwait(false);
                persisted = persistence.Persisted;
                persistenceError = persistence.Persisted ? null : persistence.Message;
                progressState = persistence.UpdatedProgressState;
            }

            return new CafeScenarioTurnOutcome(
                evaluation,
                realization.Response,
                realization.Mode,
                realization.Message,
                realization.Diagnostic,
                persisted,
                persistenceError,
                progressState);
        }
        finally
        {
            _turnGate.Release();
        }
    }

    public async Task<CafePersistenceResult> RetryPersistenceAsync()
    {
        await _turnGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_pendingLearningState is null)
            {
                return new CafePersistenceResult(
                    Persisted: true,
                    "There is no pending learning update.",
                    _targetProgress?.State);
            }

            return await SavePendingAsync().ConfigureAwait(false);
        }
        finally
        {
            _turnGate.Release();
        }
    }

    public void Exit()
    {
        if (_pendingLearningState is null)
        {
            _session = null;
            _selectedBridge = null;
        }
    }

    private CafeBridgePresentation? SelectBridge()
    {
        var concept = _graph.Get(_definition.BridgeConceptId);
        var routing = TransferRouter.Route(
            concept,
            _transferNotes.Select(note => note.Mapping),
            _profile,
            TransferPresentationMode.Written,
            TransferRoutingConfiguration.Default);
        if (routing.Selection is not { } selection)
        {
            return null;
        }

        var note = _transferNotes.Single(note => note.Mapping.Id == selection.Mapping.Id);
        return new CafeBridgePresentation(
            selection.Mapping.SourceLanguage,
            selection.Mapping.Relation,
            note.LearnerExplanation,
            note.NegativeTransferRisks,
            selection.RequiresConfirmation,
            new SelectedBridgeReference(
                selection.Mapping.Id,
                selection.Mapping.Version,
                TransferRoutingConfiguration.Default.Version,
                selection.Score));
    }

    private async Task<DialogueRealization> RealizeNpcResponseAsync(
        CafeOrderTurnResult evaluation,
        string learnerText,
        CancellationToken cancellationToken)
    {
        if (evaluation.AllowedNpcResponses.Count == 0)
        {
            return new DialogueRealization(
                Response: null,
                DialogueRealizationMode.Scripted,
                Model: null,
                "No NPC response is needed while the focused retry is active.",
                Diagnostic: null);
        }

        var fallback = evaluation.AllowedNpcResponses[0];
        if (_languageModelProvider is null)
        {
            return new DialogueRealization(
                fallback,
                DialogueRealizationMode.Scripted,
                Model: null,
                "The optional local model is unavailable; the validated scripted response was used.",
                Diagnostic: null);
        }

        var request = new DialogueGenerationRequest(
            evaluation.Session.Id,
            Guid.NewGuid(),
            _profile.Settings.SelectedLocalModel,
            _definition.NpcRole,
            _definition.Goal,
            evaluation.PreviousStateId,
            [evaluation.Intent],
            [evaluation.Session.StateId],
            _definition.Vocabulary,
            evaluation.AllowedNpcResponses,
            [
                _definition.Context,
                $"The deterministic next state is {evaluation.Session.StateId}.",
            ],
            learnerText,
            fallback);

        try
        {
            var result = await _languageModelProvider
                .GenerateDialogueAsync(request, cancellationToken)
                .ConfigureAwait(false);
            return result.Status == LanguageModelResultStatus.Accepted && result.Proposal is { } proposal
                ? new DialogueRealization(
                    proposal.NpcResponse,
                    DialogueRealizationMode.LocalModel,
                    request.SelectedModel,
                    result.Message,
                    result.Diagnostic)
                : new DialogueRealization(
                    result.ScriptedFallback,
                    DialogueRealizationMode.Scripted,
                    Model: null,
                    result.Message,
                    result.Diagnostic);
        }
        catch (OperationCanceledException)
        {
            return new DialogueRealization(
                fallback,
                DialogueRealizationMode.Scripted,
                Model: null,
                "The local-model request was cancelled; the validated scripted response was used.",
                Diagnostic: null);
        }
    }

    private void BuildPendingCompletion(
        CafeOrderTurnResult evaluation,
        DialogueRealizationMode dialogueMode,
        string? model)
    {
        if (_learningState is null || _targetProgress is null || evaluation.Evidence is null)
        {
            throw new InvalidOperationException("The completed café result has no initialized learning state.");
        }

        var completedAt = _clock();
        if (completedAt < evaluation.Session.StartedAt)
        {
            completedAt = evaluation.Session.StartedAt;
        }

        var taskAttemptId = Guid.NewGuid();
        var taskAttempt = new TaskAttempt(
            taskAttemptId,
            evaluation.Session.Id,
            _definition.TaskId,
            evaluation.Session.StartedAt,
            completedAt,
            evaluation.Session.TurnCount,
            evaluation.Session.RetryCount,
            evaluation.Evidence,
            evaluation.Session.EncounteredErrorRuleIds,
            _definition.ContentVersion,
            _definition.EvaluationVersion,
            dialogueMode,
            model,
            DialogueProposalValidator.SchemaVersion,
            _selectedBridge);
        var conceptAttempt = new ConceptAttempt(
            Guid.NewGuid(),
            _definition.TargetConceptId,
            completedAt,
            evaluation.Evidence,
            _definition.ContentVersion,
            ProgressionConfiguration.Default.Version,
            ConceptSelectionConfiguration.Default.Version,
            _selectedBridge);
        var progression = ConceptProgression.Advance(
            _targetProgress,
            prerequisitesSatisfied: true,
            conceptAttempt,
            completedAt,
            ProgressionConfiguration.Default);

        var curriculum = _learningState.Curriculum with
        {
            Progress = _learningState.Curriculum.Progress
                .Where(progress => progress.ConceptId != _definition.TargetConceptId)
                .Append(progression.Current)
                .OrderBy(progress => progress.ConceptId.Value, StringComparer.Ordinal)
                .ToArray(),
            Attempts = _learningState.Curriculum.Attempts.Append(conceptAttempt).ToArray(),
        };
        var tasks = _learningState.Tasks with
        {
            Attempts = _learningState.Tasks.Attempts.Append(taskAttempt).ToArray(),
            ReviewHandoffs = _learningState.Tasks.ReviewHandoffs.Append(new ReviewHandoff(
                Guid.NewGuid(),
                taskAttemptId,
                _definition.TargetConceptId,
                completedAt,
                evaluation.Session.EncounteredErrorRuleIds)).ToArray(),
        };

        _pendingLearningState = new LearnerLearningState(curriculum, tasks);
        _pendingProgressState = progression.Current.State;
    }

    private async Task<CafePersistenceResult> SavePendingAsync()
    {
        if (_pendingLearningState is null)
        {
            return new CafePersistenceResult(
                Persisted: true,
                "There is no pending learning update.",
                _targetProgress?.State);
        }

        try
        {
            await _profileOwner
                .SaveLearningStateAsync(_pendingLearningState, CancellationToken.None)
                .ConfigureAwait(false);
            _learningState = _pendingLearningState;
            _targetProgress = _learningState.Curriculum.Progress.Single(progress =>
                progress.ConceptId == _definition.TargetConceptId);
            _pendingLearningState = null;
            var state = _pendingProgressState;
            _pendingProgressState = null;
            return new CafePersistenceResult(
                Persisted: true,
                "Progress and the review handoff were saved locally.",
                state);
        }
        catch (Exception exception) when (
            exception is LearnerStoreException or
            LearnerProfileValidationException or
            CurriculumValidationException)
        {
            return new CafePersistenceResult(
                Persisted: false,
                $"The task was completed, but the local learning update was not saved: {exception.Message}",
                _pendingProgressState);
        }
    }

    private sealed record DialogueRealization(
        string? Response,
        DialogueRealizationMode Mode,
        string? Model,
        string Message,
        LanguageModelDiagnostic? Diagnostic);
}
