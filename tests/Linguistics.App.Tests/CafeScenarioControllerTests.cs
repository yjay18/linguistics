using Linguistics.App.Features.Scenarios;
using Linguistics.App.Persistence;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Providers;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class CafeScenarioControllerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task FullCorrectionRetryUsesBoundedModelAndPersistsSeparateEvidence()
    {
        var setup = await CreateSetupAsync(prerequisiteReady: true, new AcceptedProvider());
        var initialization = await setup.Controller.InitializeAsync();

        Assert.IsTrue(initialization.CanStart);
        Assert.AreEqual(ConceptProgressState.Available, initialization.TargetProgressState);
        Assert.AreEqual(new LanguageCode("hi"), initialization.Bridge?.SourceLanguage);
        Assert.IsTrue(initialization.Bridge?.RequiresConfirmation);

        setup.Controller.Start(useConfirmedBridge: true);
        var correction = await setup.Controller.SubmitAsync("Ich möchte ein Kaffee, bitte");

        Assert.IsFalse(correction.Evaluation.Completed);
        Assert.AreEqual(
            "de.error.accusative-masculine",
            correction.Evaluation.PrimaryIntervention?.ErrorRuleId);
        Assert.AreEqual(0, setup.Provider!.Calls);

        var completion = await setup.Controller.SubmitAsync("einen Kaffee, bitte");

        Assert.IsTrue(completion.Evaluation.Completed);
        Assert.IsTrue(completion.Persisted);
        Assert.AreEqual(DialogueRealizationMode.LocalModel, completion.DialogueMode);
        Assert.AreEqual("Gern. Einen Moment, bitte.", completion.NpcResponse);
        Assert.AreEqual(1, setup.Provider.Calls);
        Assert.AreEqual(ConceptProgressState.Introduced, completion.UpdatedProgressState);

        var saved = setup.Repository.State;
        Assert.HasCount(1, saved.Tasks.Attempts);
        Assert.HasCount(1, saved.Tasks.ReviewHandoffs);
        Assert.HasCount(1, saved.Curriculum.Attempts);
        var taskAttempt = saved.Tasks.Attempts.Single();
        Assert.IsTrue(taskAttempt.Evidence.CommunicativeSuccess);
        Assert.AreEqual(1, taskAttempt.Evidence.LinguisticAccuracy);
        Assert.IsNull(taskAttempt.Evidence.Pronunciation);
        Assert.AreEqual(DialogueRealizationMode.LocalModel, taskAttempt.DialogueMode);
        Assert.AreEqual("fixture:local", taskAttempt.LocalModel);
        Assert.AreEqual(
            "hi-de.de.noun.gender-basic.category-bridge",
            taskAttempt.SelectedBridge?.MappingId.Value);
    }

    [TestMethod]
    public async Task ScriptedCompletionWorksWithoutProviderOrConfirmedBridge()
    {
        var setup = await CreateSetupAsync(prerequisiteReady: true, provider: null);
        var initialization = await setup.Controller.InitializeAsync();
        Assert.IsTrue(initialization.CanStart);

        setup.Controller.Start(useConfirmedBridge: false);
        var completion = await setup.Controller.SubmitAsync(
            "Ich möchte einen Kaffee, bitte");

        Assert.IsTrue(completion.Persisted);
        Assert.AreEqual(DialogueRealizationMode.Scripted, completion.DialogueMode);
        Assert.AreEqual("Gern. Einen Moment, bitte.", completion.NpcResponse);
        Assert.IsNull(setup.Repository.State.Tasks.Attempts.Single().SelectedBridge);
        Assert.IsNull(setup.Repository.State.Tasks.Attempts.Single().LocalModel);
    }

    [TestMethod]
    public async Task LockedPrerequisitesBlockTheScenarioWithoutWriting()
    {
        var setup = await CreateSetupAsync(prerequisiteReady: false, provider: null);

        var initialization = await setup.Controller.InitializeAsync();

        Assert.IsFalse(initialization.CanStart);
        Assert.AreEqual(ConceptProgressState.Locked, initialization.TargetProgressState);
        Assert.HasCount(1, initialization.MissingPrerequisiteTitles);
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            setup.Controller.Start(useConfirmedBridge: false));
        Assert.IsEmpty(setup.Repository.State.Tasks.Attempts);
    }

    [TestMethod]
    public async Task FailedAtomicSaveCanBeRetriedWithoutDuplicatingTheAttempt()
    {
        var setup = await CreateSetupAsync(prerequisiteReady: true, provider: null);
        await setup.Controller.InitializeAsync();
        setup.Controller.Start(useConfirmedBridge: false);
        setup.Repository.FailLearningStateSave = true;

        var completion = await setup.Controller.SubmitAsync(
            "Ich möchte einen Kaffee, bitte");

        Assert.IsFalse(completion.Persisted);
        Assert.IsEmpty(setup.Repository.State.Tasks.Attempts);

        setup.Repository.FailLearningStateSave = false;
        var retry = await setup.Controller.RetryPersistenceAsync();

        Assert.IsTrue(retry.Persisted);
        Assert.HasCount(1, setup.Repository.State.Tasks.Attempts);
        Assert.HasCount(1, setup.Repository.State.Curriculum.Attempts);
    }

    [TestMethod]
    public async Task RealStoreRelaunchRestoresCompletionWithoutRawDialogue()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "linguistics-scenario-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "learner-profile.json");
        try
        {
            var profile = CreateProfile();
            var bridgeId = new ConceptId("de.noun.gender-basic");
            var targetId = new ConceptId("de.function.order-polite");
            var graph = new ConceptGraph(
                [Node(bridgeId, "Learn a noun with its article", []), Node(targetId, "Order one café item politely", [bridgeId])]);
            var initial = new LearnerLearningState(
                CurriculumHistory.Empty with
                {
                    Progress =
                    [
                        new ConceptProgress(
                            bridgeId,
                            ConceptProgressState.Mastered,
                            2,
                            Now.AddDays(-1),
                            Now.AddDays(13),
                            0,
                            1),
                    ],
                },
                TaskHistory.Empty);
            var repository = new JsonLearnerRepository(path);
            await repository.SaveAsync(profile);
            await repository.SaveLearningStateAsync(profile.Id, initial);
            var owner = new LearnerProfileOwner(repository);
            await owner.RestoreAsync();
            var controller = CreateController(profile, owner, graph, targetId, bridgeId, provider: null);
            await controller.InitializeAsync();
            controller.Start(useConfirmedBridge: false);

            var completion = await controller.SubmitAsync(
                "Ich möchte einen Kaffee, bitte. raw-secret-marker");

            Assert.IsTrue(completion.Persisted);
            var storedJson = await File.ReadAllTextAsync(path);
            StringAssert.Contains(storedJson, "\"schemaVersion\": 3");
            Assert.IsFalse(storedJson.Contains("raw-secret-marker", StringComparison.Ordinal));

            var relaunchedOwner = new LearnerProfileOwner(new JsonLearnerRepository(path));
            var restoredProfile = await relaunchedOwner.RestoreAsync();
            Assert.IsNotNull(restoredProfile);
            var relaunched = CreateController(
                restoredProfile,
                relaunchedOwner,
                graph,
                targetId,
                bridgeId,
                provider: null);
            var restored = await relaunched.InitializeAsync();

            Assert.AreEqual(1, restored.PreviousCompletions);
            Assert.AreEqual(ConceptProgressState.Introduced, restored.TargetProgressState);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task<Setup> CreateSetupAsync(
        bool prerequisiteReady,
        AcceptedProvider? provider)
    {
        var profile = CreateProfile();
        var bridgeId = new ConceptId("de.noun.gender-basic");
        var targetId = new ConceptId("de.function.order-polite");
        var bridge = Node(bridgeId, "Learn a noun with its article", []);
        var target = Node(targetId, "Order one café item politely", [bridgeId]);
        var graph = new ConceptGraph([bridge, target]);
        var progress = prerequisiteReady
            ? new[]
            {
                new ConceptProgress(
                    bridgeId,
                    ConceptProgressState.Mastered,
                    AttemptCount: 2,
                    LastAttemptAt: Now.AddDays(-1),
                    ReviewDueAt: Now.AddDays(13),
                    RecurringErrorCount: 0,
                    CognitiveLoad: 1),
            }
            : [];
        var repository = new ScenarioRepository(
            profile,
            new LearnerLearningState(
                CurriculumHistory.Empty with { Progress = progress },
                TaskHistory.Empty));
        var owner = new LearnerProfileOwner(repository);
        await owner.RestoreAsync();
        var controller = CreateController(profile, owner, graph, targetId, bridgeId, provider);
        return new Setup(controller, repository, provider);
    }

    private static LearnerProfile CreateProfile() =>
        new(
            Guid.NewGuid(),
            new LanguageCode("de"),
            [new KnownLanguage(
                new LanguageCode("hi"),
                LanguageProficiency.Advanced,
                ComfortableReading: true,
                ComfortableListening: true,
                AllowExplanations: true)],
            new LearnerSettings(
                MultilingualShortcutMode.AskFirst,
                null,
                MicrophonePreference.Later,
                RetainSpeechRecordings: false,
                SelectedLocalModel: "fixture:local"));

    private static CafeScenarioController CreateController(
        LearnerProfile profile,
        LearnerProfileOwner owner,
        ConceptGraph graph,
        ConceptId targetId,
        ConceptId bridgeId,
        AcceptedProvider? provider)
    {
        var mapping = new TransferMapping(
            new TransferMappingId("hi-de.de.noun.gender-basic.category-bridge"),
            new VersionId("transfer.hi-de.core.v1"),
            new LanguageCode("hi"),
            new LanguageCode("de"),
            bridgeId,
            TransferRelation.Facilitative,
            Strength: 0.65,
            TransferReviewStatus.Approved);
        return CafeScenarioController.CreateFromResources(
            profile,
            owner,
            graph,
            Definition(targetId, bridgeId),
            [new TransferNote(
                mapping,
                "Hindi grammatical gender can make the idea familiar.",
                ["Do not transfer a Hindi noun's gender."])],
            provider,
            () => Now);
    }

    private static ConceptNode Node(
        ConceptId id,
        string title,
        IReadOnlyList<ConceptId> prerequisites) =>
        new(
            id,
            new LanguageCode("de"),
            ConceptType.Pragmatic,
            title,
            title,
            "A1",
            prerequisites,
            ["Complete the fixture."],
            [],
            ["cafe"],
            new VersionId("language.de.core.v1"));

    private static CafeOrderDefinition Definition(
        ConceptId targetId,
        ConceptId bridgeId) =>
        new(
            "de.task.cafe.order-one-item",
            new VersionId("language.de.core.v1"),
            new VersionId("cafe-order-evaluator-v1"),
            targetId,
            bridgeId,
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
                ["frame"] = ["Kaffee, Tee oder Wasser?"],
                ["complete"] = ["Gern. Einen Moment, bitte."],
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

    private sealed record Setup(
        CafeScenarioController Controller,
        ScenarioRepository Repository,
        AcceptedProvider? Provider);

    private sealed class ScenarioRepository(
        LearnerProfile profile,
        LearnerLearningState state) : ILearnerRepository
    {
        public LearnerLearningState State { get; private set; } = state;

        public bool FailLearningStateSave { get; set; }

        public Task<LearnerProfile?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<LearnerProfile?>(profile);

        public Task SaveAsync(
            LearnerProfile updated,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<CurriculumHistory> LoadCurriculumAsync(
            Guid profileId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(State.Curriculum);

        public Task SaveCurriculumAsync(
            Guid profileId,
            CurriculumHistory history,
            CancellationToken cancellationToken = default)
        {
            State = State with { Curriculum = history };
            return Task.CompletedTask;
        }

        public Task<LearnerLearningState> LoadLearningStateAsync(
            Guid profileId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(State);

        public Task SaveLearningStateAsync(
            Guid profileId,
            LearnerLearningState newState,
            CancellationToken cancellationToken = default)
        {
            if (FailLearningStateSave)
            {
                throw new LearnerStoreException("Synthetic disk failure.");
            }

            State = newState;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class AcceptedProvider : ILanguageModelProvider
    {
        public int Calls { get; private set; }

        public Task<LocalModelServiceSnapshot> InspectServiceAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<LocalModelDetails> InspectModelAsync(
            string model,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<DialogueGenerationResult> GenerateDialogueAsync(
            DialogueGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            var proposal = new DialogueProposal(
                request.AllowedNpcResponses[0],
                request.AllowedIntents[0],
                request.AllowedNextStates[0],
                []);
            return Task.FromResult(new DialogueGenerationResult(
                LanguageModelResultStatus.Accepted,
                proposal,
                request.ScriptedFallback,
                "Synthetic accepted proposal.",
                new LanguageModelDiagnostic(
                    request.SessionId,
                    request.RequestId,
                    request.SelectedModel,
                    TimeSpan.FromMilliseconds(4),
                    DialogueProposalValidator.PromptVersion,
                    DialogueProposalValidator.SchemaVersion,
                    "accepted")));
        }
    }
}
