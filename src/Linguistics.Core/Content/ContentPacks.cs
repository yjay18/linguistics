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
    string Meaning,
    string Note);

public sealed record ConceptSuccessCriteria(
    int MinimumAttempts,
    double MinimumAccuracy,
    IReadOnlyList<string> RequiredEvaluatorIds);

public sealed record TargetConceptContent(
    string Id,
    string Language,
    ConceptType Type,
    string CefrApproximation,
    string Title,
    string Description,
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
    string Meaning,
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
    string Description);

public sealed record TaskTemplateContent(
    string Id,
    string Language,
    string Domain,
    string CefrApproximation,
    string Goal,
    string Context,
    string LearnerRole,
    string NpcRole,
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
    string Message,
    string RetryPrompt,
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

public sealed record TransferMappingContent(
    string Id,
    string SourceLanguage,
    string TargetLanguage,
    string TargetConceptId,
    TransferRelation Relation,
    double Strength,
    IReadOnlyList<string> BridgeConcepts,
    string LearnerExplanation,
    string TeacherNotes,
    IReadOnlyList<ContentExample> PositiveExamples,
    IReadOnlyList<string> NegativeTransferRisks,
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
    IReadOnlyList<TransferMappingContent> TransferMappings);

public sealed class ValidatedContentCatalog
{
    internal ValidatedContentCatalog(
        IReadOnlyList<ContentPackDocument> packs,
        ContentLoadPolicy policy)
    {
        Packs = packs;
        Policy = policy;
    }

    public IReadOnlyList<ContentPackDocument> Packs { get; }

    public ContentLoadPolicy Policy { get; }

    public ConceptGraph CreateRuntimeConceptGraph(LanguageCode targetLanguage)
    {
        EnsureRuntimePolicy();

        var concepts = Packs
            .Where(pack => pack.Manifest.Kind == ContentPackKind.TargetLanguage)
            .SelectMany(pack => pack.Concepts.Select(concept => ToConceptNode(pack.Manifest, concept)))
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

    private static ConceptNode ToConceptNode(
        ContentPackManifest manifest,
        TargetConceptContent concept) =>
        new(
            new ConceptId(concept.Id),
            new LanguageCode(concept.Language),
            concept.Type,
            concept.Title,
            concept.Description,
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
}
