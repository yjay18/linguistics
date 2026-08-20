using Linguistics.Core.Profiles;

namespace Linguistics.Core.Curriculum;

public enum TransferRelation
{
    Facilitative,
    PartiallyFacilitative,
    Neutral,
    Interfering,
    Unknown,
}

public enum TransferReviewStatus
{
    Draft,
    Approved,
    Deprecated,
}

public enum TransferPresentationMode
{
    Written,
    Spoken,
}

public sealed record TransferMapping(
    TransferMappingId Id,
    VersionId Version,
    LanguageCode SourceLanguage,
    LanguageCode TargetLanguage,
    ConceptId TargetConceptId,
    TransferRelation Relation,
    double Strength,
    TransferReviewStatus ReviewStatus);

public sealed record TransferNote(
    TransferMapping Mapping,
    string LearnerExplanation,
    IReadOnlyList<string> NegativeTransferRisks);

public sealed record TransferRoutingConfiguration(
    VersionId Version,
    double MinimumScore,
    double CloseScoreTolerance,
    double BeginnerWeight,
    double IntermediateWeight,
    double AdvancedWeight,
    double NativeWeight,
    double AskFirstWeight,
    double PartialRelationWeight,
    double InterferingRelationWeight)
{
    public static TransferRoutingConfiguration Default { get; } = new(
        new VersionId("routing-v1"),
        MinimumScore: 0.35,
        CloseScoreTolerance: 0.05,
        BeginnerWeight: 0.25,
        IntermediateWeight: 0.55,
        AdvancedWeight: 0.85,
        NativeWeight: 1,
        AskFirstWeight: 0.95,
        PartialRelationWeight: 0.9,
        InterferingRelationWeight: 0.85);

    public void Validate()
    {
        var values = new[]
        {
            MinimumScore,
            CloseScoreTolerance,
            BeginnerWeight,
            IntermediateWeight,
            AdvancedWeight,
            NativeWeight,
            AskFirstWeight,
            PartialRelationWeight,
            InterferingRelationWeight,
        };

        if (string.IsNullOrWhiteSpace(Version.Value) ||
            values.Any(value => double.IsNaN(value) || double.IsInfinity(value) || value is < 0 or > 1))
        {
            throw new ArgumentException("The transfer-routing configuration is invalid.", nameof(TransferRoutingConfiguration));
        }
    }

    public double ProficiencyWeight(LanguageProficiency proficiency) => proficiency switch
    {
        LanguageProficiency.Beginner => BeginnerWeight,
        LanguageProficiency.Intermediate => IntermediateWeight,
        LanguageProficiency.Advanced => AdvancedWeight,
        LanguageProficiency.NativeOrNearNative => NativeWeight,
        _ => 0,
    };

    public double RelationWeight(TransferRelation relation) => relation switch
    {
        TransferRelation.Facilitative => 1,
        TransferRelation.PartiallyFacilitative => PartialRelationWeight,
        TransferRelation.Interfering => InterferingRelationWeight,
        _ => 0,
    };
}

public enum TransferRejectionReason
{
    None,
    InvalidMapping,
    DifferentConcept,
    IncompatibleTargetLanguage,
    Unapproved,
    RelationNotDisplayable,
    LanguageNotKnown,
    ExplanationNotAllowed,
    SkillNotComfortable,
    ShortcutDisabled,
    BelowThreshold,
}

public sealed record TransferScoreInputs(
    double MappingStrength,
    double ProficiencyWeight,
    double PreferenceWeight,
    double RelationWeight);

public sealed record TransferCandidateDecision(
    TransferMappingId MappingId,
    bool Eligible,
    double Score,
    TransferRejectionReason RejectionReason,
    TransferScoreInputs? Inputs);

public sealed record TransferSelection(
    TransferMapping Mapping,
    double Score,
    bool RequiresConfirmation);

public sealed record TransferRoutingExplanation(
    ConceptId ConceptId,
    VersionId ConfigurationVersion,
    IReadOnlyList<TransferCandidateDecision> Candidates,
    string Summary);

public sealed record TransferRoutingResult(
    TransferSelection? Selection,
    TransferRoutingExplanation Explanation);

public static class TransferRouter
{
    public static TransferRoutingResult Route(
        ConceptNode concept,
        IEnumerable<TransferMapping> mappings,
        LearnerProfile profile,
        TransferPresentationMode presentationMode,
        TransferRoutingConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(concept);
        ArgumentNullException.ThrowIfNull(mappings);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(configuration);
        LearnerProfileValidator.Validate(profile);
        configuration.Validate();

        if (!Enum.IsDefined(presentationMode))
        {
            throw new ArgumentOutOfRangeException(nameof(presentationMode));
        }

        var materialized = mappings.ToArray();
        var duplicateIds = materialized
            .OfType<TransferMapping>()
            .Where(mapping => !string.IsNullOrWhiteSpace(mapping.Id.Value))
            .GroupBy(mapping => mapping.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();
        var evaluated = materialized
            .Select(mapping => (
                Mapping: mapping,
                Decision: Evaluate(
                    concept,
                    mapping,
                    duplicateIds.Contains(mapping?.Id ?? default),
                    profile,
                    presentationMode,
                    configuration)))
            .ToArray();
        var decisions = evaluated
            .Select(candidate => candidate.Decision)
            .OrderBy(decision => decision.MappingId.Value, StringComparer.Ordinal)
            .ToArray();

        var eligible = evaluated
            .Where(candidate => candidate.Decision.Eligible)
            .OrderByDescending(candidate => candidate.Decision.Score)
            .ThenBy(candidate => candidate.Mapping.Id.Value, StringComparer.Ordinal)
            .ToArray();

        TransferSelection? selection = null;
        if (eligible.Length > 0)
        {
            var topScore = eligible[0].Decision.Score;
            var close = eligible
                .Where(candidate => topScore - candidate.Decision.Score <= configuration.CloseScoreTolerance)
                .ToArray();
            var preferred = profile.Settings.PreferredExplanationLanguage;
            var selected = preferred is null
                ? eligible[0]
                : close
                    .Where(candidate => candidate.Mapping.SourceLanguage == preferred.Value)
                    .OrderByDescending(candidate => candidate.Decision.Score)
                    .ThenBy(candidate => candidate.Mapping.Id.Value, StringComparer.Ordinal)
                    .FirstOrDefault();

            if (selected == default)
            {
                selected = eligible[0];
            }

            selection = new TransferSelection(
                selected.Mapping,
                selected.Decision.Score,
                profile.Settings.ShortcutMode == MultilingualShortcutMode.AskFirst);
        }

        return new TransferRoutingResult(
            selection,
            new TransferRoutingExplanation(
                concept.Id,
                configuration.Version,
                decisions,
                selection is null
                    ? "No approved mapping met the learner and configuration rules."
                    : $"Selected '{selection.Mapping.Id}' with score {selection.Score:0.###}."));
    }

    private static TransferCandidateDecision Evaluate(
        ConceptNode concept,
        TransferMapping mapping,
        bool duplicateId,
        LearnerProfile profile,
        TransferPresentationMode presentationMode,
        TransferRoutingConfiguration configuration)
    {
        if (mapping is null ||
            duplicateId ||
            string.IsNullOrWhiteSpace(mapping.Id.Value) ||
            string.IsNullOrWhiteSpace(mapping.Version.Value) ||
            string.IsNullOrWhiteSpace(mapping.SourceLanguage.Value) ||
            string.IsNullOrWhiteSpace(mapping.TargetLanguage.Value) ||
            string.IsNullOrWhiteSpace(mapping.TargetConceptId.Value) ||
            !Enum.IsDefined(mapping.Relation) ||
            !Enum.IsDefined(mapping.ReviewStatus) ||
            double.IsNaN(mapping.Strength) ||
            double.IsInfinity(mapping.Strength) ||
            mapping.Strength is < 0 or > 1)
        {
            return Rejected(mapping?.Id ?? default, TransferRejectionReason.InvalidMapping);
        }

        if (mapping.TargetConceptId != concept.Id)
        {
            return Rejected(mapping.Id, TransferRejectionReason.DifferentConcept);
        }

        if (mapping.TargetLanguage != concept.TargetLanguage ||
            mapping.TargetLanguage != profile.TargetLanguage)
        {
            return Rejected(mapping.Id, TransferRejectionReason.IncompatibleTargetLanguage);
        }

        if (mapping.ReviewStatus != TransferReviewStatus.Approved)
        {
            return Rejected(mapping.Id, TransferRejectionReason.Unapproved);
        }

        if (mapping.Relation is TransferRelation.Neutral or TransferRelation.Unknown)
        {
            return Rejected(mapping.Id, TransferRejectionReason.RelationNotDisplayable);
        }

        var knownLanguage = profile.KnownLanguages.SingleOrDefault(language =>
            language.Language == mapping.SourceLanguage);
        if (knownLanguage is null)
        {
            return Rejected(mapping.Id, TransferRejectionReason.LanguageNotKnown);
        }

        if (!knownLanguage.AllowExplanations)
        {
            return Rejected(mapping.Id, TransferRejectionReason.ExplanationNotAllowed);
        }

        var skillComfortable = presentationMode == TransferPresentationMode.Written
            ? knownLanguage.ComfortableReading
            : knownLanguage.ComfortableListening;
        if (!skillComfortable)
        {
            return Rejected(mapping.Id, TransferRejectionReason.SkillNotComfortable);
        }

        if (profile.Settings.ShortcutMode == MultilingualShortcutMode.Never)
        {
            return Rejected(mapping.Id, TransferRejectionReason.ShortcutDisabled);
        }

        var inputs = new TransferScoreInputs(
            mapping.Strength,
            configuration.ProficiencyWeight(knownLanguage.Proficiency),
            profile.Settings.ShortcutMode == MultilingualShortcutMode.AskFirst
                ? configuration.AskFirstWeight
                : 1,
            configuration.RelationWeight(mapping.Relation));
        var score = inputs.MappingStrength *
                    inputs.ProficiencyWeight *
                    inputs.PreferenceWeight *
                    inputs.RelationWeight;

        return score < configuration.MinimumScore
            ? new TransferCandidateDecision(
                mapping.Id,
                Eligible: false,
                score,
                TransferRejectionReason.BelowThreshold,
                inputs)
            : new TransferCandidateDecision(
                mapping.Id,
                Eligible: true,
                score,
                TransferRejectionReason.None,
                inputs);
    }

    private static TransferCandidateDecision Rejected(
        TransferMappingId id,
        TransferRejectionReason reason) =>
        new(id, Eligible: false, Score: 0, reason, Inputs: null);
}
