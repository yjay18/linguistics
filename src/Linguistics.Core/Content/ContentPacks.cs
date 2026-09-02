using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;

namespace Linguistics.Core.Content;

public enum ContentPackKind
{
    TargetLanguage,
    Transfer,
}

public enum ContentReviewStatus
{
    Draft,
    MachineValidated,
    LinguisticallyReviewed,
    Approved,
    Deprecated,
    Rejected,
}

public enum LicenseReviewStatus
{
    Pending,
    Reviewed,
    Rejected,
}

public enum ContentLoadPolicy
{
    ValidationOnly,
    AuthoringPreview,
    Runtime,
}

public enum TaskEvaluatorKind
{
    RequiredToken,
    RequiredTokenSequence,
    AnyAllowedToken,
    StateReached,
}

public enum ErrorPatternKind
{
    RequiredToken,
    RequiredTokenSequence,
    ForbiddenToken,
}

public enum ErrorSeverity
{
    CommunicationBlocking,
    TargetConcept,
    Repeated,
    Intelligibility,
    Minor,
}

public enum TaskOutcomeDimension
{
    CommunicativeSuccess,
    LinguisticAccuracy,
    TargetConceptPerformance,
}

public enum PronunciationPurpose
{
    Perception,
    Production,
}

public enum PronunciationAssessmentMode
{
    None,
}

public sealed record ContentReview(
    ContentReviewStatus Status,
    string? Reviewer,
    DateOnly? ReviewedOn,
    string Notes);

public sealed record ContentLicense(
    string Identifier,
    string CopyrightHolder,
    string LicenseTextLocation,
    string IntendedUse,
    bool ModificationReviewed,
    bool RedistributionReviewed,
    string RequiredAttribution,
    LicenseReviewStatus ReviewStatus);

public sealed record PackDependency(
    string PackId,
    int MinimumVersion,
    int MaximumVersion);

public sealed record ContentPackManifest(
    string Id,
    int Version,
    int SchemaVersion,
    ContentPackKind Kind,
    IReadOnlyList<string> Languages,
    IReadOnlyList<string> InstructionLanguages,
    IReadOnlyList<PackDependency> Dependencies,
    ContentLicense License,
    ContentReview Review);

public sealed record SourceRecord(
    string Id,
    string Title,
    string Citation,
    string Url,
    string Claim,
    ContentLicense License,
    string Notes);

public sealed record ContentExample(
    string Text,
    IReadOnlyDictionary<string, string> Meaning,
    IReadOnlyDictionary<string, string> Note,
    string? Id = null);

public sealed record ConceptSuccessCriteria(
    int MinimumAttempts,
    double MinimumAccuracy,
    IReadOnlyList<string> RequiredEvaluatorIds);

public sealed record TargetConceptContent(
    string Id,
    string Language,
    ConceptType Type,
    string CefrApproximation,
    IReadOnlyDictionary<string, string> Title,
    IReadOnlyDictionary<string, string> Description,
    IReadOnlyList<string> PrerequisiteIds,
    ConceptSuccessCriteria SuccessCriteria,
    IReadOnlyList<string> ErrorRuleIds,
    IReadOnlyList<string> TaskTags,
    IReadOnlyList<ContentExample> Examples,
    IReadOnlyList<ContentExample> Counterexamples,
    IReadOnlyList<string> SourceIds,
    ContentReview Review);

public sealed record LexicalEntryContent(
    string Id,
    string Language,
    string Lemma,
    string? Article,
    IReadOnlyDictionary<string, string> Meaning,
    IReadOnlyList<string> ConceptIds,
    IReadOnlyList<ContentExample> Examples,
    IReadOnlyList<string> SourceIds,
    ContentReview Review);

public sealed record TaskStateContent(
    string Id,
    IReadOnlyList<string> AllowedIntents,
    IReadOnlyList<string> ScriptedFallback);

public sealed record TaskTransitionContent(
    string FromStateId,
    string ToStateId,
    string EvaluatorId);

public sealed record TaskEvaluatorContent(
    string Id,
    TaskEvaluatorKind Kind,
    IReadOnlyList<string> ExpectedValues);

public sealed record TaskSuccessCondition(
    string EvaluatorId,
    IReadOnlyDictionary<string, string> Description);

public sealed record TaskTemplateContent(
    string Id,
    string Language,
    string Domain,
    string CefrApproximation,
    IReadOnlyDictionary<string, string> Goal,
    IReadOnlyDictionary<string, string> Context,
    IReadOnlyDictionary<string, string> LearnerRole,
    IReadOnlyDictionary<string, string> NpcRole,
    IReadOnlyList<string> RequiredFunctionIds,
    IReadOnlyList<string> EligibleConceptIds,
    string InitialStateId,
    IReadOnlyList<string> SuccessStateIds,
    IReadOnlyList<TaskStateContent> States,
    IReadOnlyList<TaskTransitionContent> Transitions,
    IReadOnlyList<TaskEvaluatorContent> Evaluators,
    IReadOnlyList<TaskSuccessCondition> SuccessConditions,
    IReadOnlyList<string> SourceIds,
    ContentReview Review);

public sealed record ErrorPatternContent(
    ErrorPatternKind Kind,
    IReadOnlyList<string> Values);

public sealed record ErrorRuleContent(
    string Id,
    string TargetConceptId,
    ErrorPatternContent Pattern,
    string ExpectedProperty,
    ErrorSeverity Severity,
    string FeedbackTemplateId,
    IReadOnlyList<string> Examples,
    IReadOnlyList<string> Counterexamples,
    IReadOnlyList<string> SourceIds,
    ContentReview Review);

public sealed record FeedbackTemplateContent(
    string Id,
    string Language,
    IReadOnlyDictionary<string, string> Message,
    IReadOnlyDictionary<string, string> RetryPrompt,
    IReadOnlyList<string> SourceIds,
    ContentReview Review);

public sealed record RubricDimensionContent(
    TaskOutcomeDimension Dimension,
    string EvaluatorId);

public sealed record RubricContent(
    string Id,
    string TaskId,
    IReadOnlyList<RubricDimensionContent> Dimensions,
    IReadOnlyList<string> SourceIds,
    ContentReview Review);

public sealed record PronunciationUtteranceContent(
    string Id,
    string Language,
    string Locale,
    string Text,
    PronunciationPurpose Purpose,
    PronunciationAssessmentMode AssessmentMode,
    IReadOnlyList<string> ConceptIds,
    IReadOnlyList<string> SourceIds,
    ContentReview Review);

public sealed record RuntimePronunciationUtterance(
    string Id,
    LanguageCode Language,
    string Locale,
    string Text,
    PronunciationPurpose Purpose,
    IReadOnlyList<ConceptId> ConceptIds,
    VersionId ContentVersion);

public sealed record TransferMappingContent(
    string Id,
    string SourceLanguage,
    string TargetLanguage,
    string TargetConceptId,
    TransferRelation Relation,
    double Strength,
    IReadOnlyList<string> BridgeConcepts,
    IReadOnlyDictionary<string, string> LearnerExplanation,
    string TeacherNotes,
    IReadOnlyList<ContentExample> PositiveExamples,
    IReadOnlyDictionary<string, IReadOnlyList<string>> NegativeTransferRisks,
    IReadOnlyList<string> SourceIds,
    ContentReview Review);

public sealed record ContentPackDocument(
    ContentPackManifest Manifest,
    IReadOnlyList<SourceRecord> Sources,
    IReadOnlyList<TargetConceptContent> Concepts,
    IReadOnlyList<LexicalEntryContent> Lexicon,
    IReadOnlyList<TaskTemplateContent> Tasks,
    IReadOnlyList<ErrorRuleContent> ErrorRules,
    IReadOnlyList<FeedbackTemplateContent> FeedbackTemplates,
    IReadOnlyList<RubricContent> Rubrics,
    IReadOnlyList<PronunciationUtteranceContent> PronunciationUtterances,
    IReadOnlyList<LessonTemplateContent> Lessons,
    IReadOnlyList<TransferMappingContent> TransferMappings);

public static class InstructionText
{
    public static string Resolve(
        IReadOnlyDictionary<string, string> values,
        LanguageCode language)
    {
        ArgumentNullException.ThrowIfNull(values);

        return values.TryGetValue(language.Value, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new KeyNotFoundException(
                $"Instruction text for language '{language}' is unavailable.");
    }

    public static IReadOnlyList<string> Resolve(
        IReadOnlyDictionary<string, IReadOnlyList<string>> values,
        LanguageCode language)
    {
        ArgumentNullException.ThrowIfNull(values);

        return values.TryGetValue(language.Value, out var value)
            ? value
            : throw new KeyNotFoundException(
                $"Instruction text for language '{language}' is unavailable.");
    }
}

public sealed class ValidatedContentCatalog
{
    internal ValidatedContentCatalog(
        IReadOnlyList<ContentPackDocument> packs,
        IReadOnlyList<ValidatedContentAsset> assets,
        ContentLoadPolicy policy)
    {
        Packs = packs;
        Assets = assets;
        Policy = policy;
    }

    public IReadOnlyList<ContentPackDocument> Packs { get; }

    public IReadOnlyList<ValidatedContentAsset> Assets { get; }

    public ContentLoadPolicy Policy { get; }

    public IReadOnlyList<LanguageCode> GetInstructionLanguages(LanguageCode targetLanguage)
    {
        var manifests = Packs
            .Where(pack =>
                pack.Manifest.Kind == ContentPackKind.TargetLanguage &&
                pack.Manifest.Languages.Contains(
                    targetLanguage.Value,
                    StringComparer.Ordinal))
            .Select(pack => pack.Manifest)
            .OrderBy(manifest => manifest.Id, StringComparer.Ordinal)
            .ToArray();
        if (manifests.Length == 0)
        {
            return [];
        }

        return manifests[0].InstructionLanguages
            .Where(language => manifests.All(manifest =>
                manifest.InstructionLanguages.Contains(language, StringComparer.Ordinal)))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(language => new LanguageCode(language))
            .ToArray();
    }

    public InstructionLanguageSelectionResult SelectInstructionLanguage(LearnerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return InstructionLanguageSelector.Select(
            profile,
            GetInstructionLanguages(profile.TargetLanguage));
    }

    public CourseCatalog CreateCourseCatalog(
        LanguageCode targetLanguage,
        LanguageCode instructionLanguage,
        CourseCatalogConfiguration? configuration = null) =>
        CourseCatalogBuilder.Build(
            Packs,
            Policy,
            targetLanguage,
            instructionLanguage,
            configuration ?? CourseCatalogConfiguration.Default);

    public ConceptGraph CreateRuntimeConceptGraph(
        LanguageCode targetLanguage,
        LanguageCode instructionLanguage)
    {
        EnsureRuntimePolicy();
        EnsureInstructionLanguage(targetLanguage, instructionLanguage);

        var concepts = Packs
            .Where(pack =>
                pack.Manifest.Kind == ContentPackKind.TargetLanguage &&
                pack.Manifest.Languages.Contains(
                    targetLanguage.Value,
                    StringComparer.Ordinal))
            .SelectMany(pack => pack.Concepts.Select(concept => ToConceptNode(
                pack.Manifest,
                concept,
                instructionLanguage)))
            .Where(concept => concept.TargetLanguage == targetLanguage)
            .ToArray();

        return new ConceptGraph(concepts);
    }

    public IReadOnlyList<TransferMapping> CreateRuntimeTransferMappings(
        LanguageCode sourceLanguage,
        LanguageCode targetLanguage)
    {
        EnsureRuntimePolicy();

        return Packs
            .Where(pack => pack.Manifest.Kind == ContentPackKind.Transfer)
            .SelectMany(pack => pack.TransferMappings.Select(mapping => ToTransferMapping(pack.Manifest, mapping)))
            .Where(mapping =>
                mapping.SourceLanguage == sourceLanguage &&
                mapping.TargetLanguage == targetLanguage)
            .OrderBy(mapping => mapping.Id.Value, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<TransferNote> CreateRuntimeTransferNotes(
        LanguageCode sourceLanguage,
        LanguageCode targetLanguage,
        LanguageCode instructionLanguage)
    {
        EnsureRuntimePolicy();

        return Packs
            .Where(pack =>
                pack.Manifest.Kind == ContentPackKind.Transfer &&
                pack.Manifest.InstructionLanguages.Contains(
                    instructionLanguage.Value,
                    StringComparer.Ordinal))
            .SelectMany(pack => pack.TransferMappings.Select(mapping => new TransferNote(
                ToTransferMapping(pack.Manifest, mapping),
                InstructionText.Resolve(mapping.LearnerExplanation, instructionLanguage),
                InstructionText.Resolve(mapping.NegativeTransferRisks, instructionLanguage))))
            .Where(note =>
                note.Mapping.SourceLanguage == sourceLanguage &&
                note.Mapping.TargetLanguage == targetLanguage)
            .OrderBy(note => note.Mapping.Id.Value, StringComparer.Ordinal)
            .ToArray();
    }

    public CafeOrderDefinition CreateRuntimeCafeOrderDefinition(
        LanguageCode instructionLanguage)
    {
        EnsureRuntimePolicy();

        var pack = Packs.Single(pack => pack.Manifest.Id == "language.de.core");
        EnsureInstructionLanguage(pack.Manifest, instructionLanguage);
        var task = pack.Tasks.Single(task => task.Id == "de.task.cafe.order-one-item");
        var states = task.States.ToDictionary(state => state.Id, StringComparer.Ordinal);
        var feedback = pack.FeedbackTemplates.ToDictionary(template => template.Id, StringComparer.Ordinal);
        var vocabulary = pack.Lexicon
            .Where(entry => entry.Id is "de.lexeme.kaffee" or "de.lexeme.bitte")
            .ToDictionary(entry => entry.Id, entry => entry.Lemma, StringComparer.Ordinal);
        var pronunciationTarget = pack.PronunciationUtterances.Single(utterance =>
            utterance.Id == "de.utterance.order");

        FocusIntervention Intervention(
            string errorRuleId,
            string feedbackId,
            FeedbackPriority priority)
        {
            var template = feedback[feedbackId];
            return new FocusIntervention(
                errorRuleId,
                priority,
                InstructionText.Resolve(template.Message, instructionLanguage),
                InstructionText.Resolve(template.RetryPrompt, instructionLanguage));
        }

        return new CafeOrderDefinition(
            task.Id,
            PackVersion(pack.Manifest),
            new VersionId("cafe-order-evaluator-v1"),
            new ConceptId("de.function.order-polite"),
            new ConceptId("de.noun.gender-basic"),
            InstructionText.Resolve(task.Goal, instructionLanguage),
            InstructionText.Resolve(task.Context, instructionLanguage),
            InstructionText.Resolve(task.NpcRole, instructionLanguage),
            task.SuccessConditions
                .Select(condition => InstructionText.Resolve(condition.Description, instructionLanguage))
                .ToArray(),
            task.InitialStateId,
            "de.state.order.frame",
            task.SuccessStateIds.Single(),
            states.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ScriptedFallback,
                StringComparer.Ordinal),
            vocabulary,
            Intervention(
                "de.error.accusative-masculine",
                "de.feedback.accusative-masculine",
                FeedbackPriority.TargetConcept),
            Intervention(
                "de.error.noun-capitalization",
                "de.feedback.noun-capitalization",
                FeedbackPriority.TargetConcept),
            Intervention(
                "de.error.order-bitte",
                "de.feedback.order-bitte",
                FeedbackPriority.Minor),
            states[task.InitialStateId].ScriptedFallback.Last(),
            states["de.state.order.frame"].ScriptedFallback.Last(),
            pronunciationTarget.Text);
    }

    public IReadOnlyList<RuntimePronunciationUtterance> CreateRuntimePronunciationUtterances(
        LanguageCode targetLanguage)
    {
        EnsureRuntimePolicy();

        return Packs
            .Where(pack => pack.Manifest.Kind == ContentPackKind.TargetLanguage)
            .SelectMany(pack => pack.PronunciationUtterances.Select(utterance =>
                new RuntimePronunciationUtterance(
                    utterance.Id,
                    new LanguageCode(utterance.Language),
                    utterance.Locale,
                    utterance.Text,
                    utterance.Purpose,
                    utterance.ConceptIds.Select(id => new ConceptId(id)).ToArray(),
                    PackVersion(pack.Manifest))))
            .Where(utterance => utterance.Language == targetLanguage)
            .OrderBy(utterance => utterance.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static ConceptNode ToConceptNode(
        ContentPackManifest manifest,
        TargetConceptContent concept,
        LanguageCode instructionLanguage) =>
        new(
            new ConceptId(concept.Id),
            new LanguageCode(concept.Language),
            concept.Type,
            InstructionText.Resolve(concept.Title, instructionLanguage),
            InstructionText.Resolve(concept.Description, instructionLanguage),
            concept.CefrApproximation,
            concept.PrerequisiteIds.Select(id => new ConceptId(id)).ToArray(),
            [
                $"At least {concept.SuccessCriteria.MinimumAttempts} attempts.",
                $"Accuracy at or above {concept.SuccessCriteria.MinimumAccuracy:0.##}.",
            ],
            concept.ErrorRuleIds,
            concept.TaskTags,
            PackVersion(manifest));

    private static TransferMapping ToTransferMapping(
        ContentPackManifest manifest,
        TransferMappingContent mapping) =>
        new(
            new TransferMappingId(mapping.Id),
            PackVersion(manifest),
            new LanguageCode(mapping.SourceLanguage),
            new LanguageCode(mapping.TargetLanguage),
            new ConceptId(mapping.TargetConceptId),
            mapping.Relation,
            mapping.Strength,
            TransferReviewStatus.Approved);

    private static VersionId PackVersion(ContentPackManifest manifest) =>
        new($"{manifest.Id}.v{manifest.Version}");

    private void EnsureRuntimePolicy()
    {
        if (Policy != ContentLoadPolicy.Runtime)
        {
            throw new InvalidOperationException(
                "Only a runtime-approved content catalog can create teaching-domain objects.");
        }
    }

    private void EnsureInstructionLanguage(
        LanguageCode targetLanguage,
        LanguageCode instructionLanguage)
    {
        if (!GetInstructionLanguages(targetLanguage).Contains(instructionLanguage))
        {
            throw new InvalidOperationException(
                $"Instruction language '{instructionLanguage}' is unavailable for target language '{targetLanguage}'.");
        }
    }

    private static void EnsureInstructionLanguage(
        ContentPackManifest manifest,
        LanguageCode instructionLanguage)
    {
        if (!manifest.InstructionLanguages.Contains(
                instructionLanguage.Value,
                StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Instruction language '{instructionLanguage}' is unavailable in pack '{manifest.Id}'.");
        }
    }
}
