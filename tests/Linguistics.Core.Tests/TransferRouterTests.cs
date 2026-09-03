using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;

namespace Linguistics.Core.Tests;

[TestClass]
public sealed class TransferRouterTests
{
    private static readonly ConceptNode Concept = new(
        new ConceptId("fixture.concept"),
        new LanguageCode("de"),
        ConceptType.Grammatical,
        "Synthetic concept",
        "Synthetic developer fixture without a linguistic claim.",
        Cefr: null,
        [],
        ["Complete the synthetic fixture."],
        [],
        ["fixture"],
        new VersionId("fixture-content-v1"));

    [TestMethod]
    public void HigherHindiScoreBeatsLowerEnglishScore()
    {
        var result = Route(
            Profile(Known("en"), Known("hi")),
            Mapping("en", 0.6),
            Mapping("hi", 0.9));

        Assert.AreEqual("fixture.hi", result.Selection?.Mapping.Id.Value);
    }

    [TestMethod]
    public void HinglishProfileUsesHindiMappingAndPreferenceDeterministically()
    {
        var profile = Profile(
            [Known("en"), Known("hi-latn")],
            new LearnerSettings(
                MultilingualShortcutMode.PreferredLanguage,
                new LanguageCode("hi-latn"),
                MicrophonePreference.Later,
                false));

        var result = Route(profile, Mapping("en", 0.9), Mapping("hi", 0.9));
        var repeated = Route(profile, Mapping("en", 0.9), Mapping("hi", 0.9));

        Assert.AreEqual("fixture.hi", result.Selection?.Mapping.Id.Value);
        Assert.AreEqual(result.Selection, repeated.Selection);
        Assert.IsTrue(result.Explanation.Candidates.All(candidate => candidate.Eligible));
    }

    [TestMethod]
    public void HigherEnglishScoreBeatsLowerHindiScore()
    {
        var result = Route(
            Profile(Known("en"), Known("hi")),
            Mapping("en", 0.95),
            Mapping("hi", 0.5));

        Assert.AreEqual("fixture.en", result.Selection?.Mapping.Id.Value);
    }

    [TestMethod]
    public void ExplanationPermissionAndRelevantSkillAreRequired()
    {
        var profile = Profile(
            Known("en", allowExplanations: false),
            Known("hi", comfortableReading: false));

        var result = Route(profile, Mapping("en", 1), Mapping("hi", 1));

        Assert.IsNull(result.Selection);
        Assert.IsTrue(result.Explanation.Candidates.Any(candidate =>
            candidate.MappingId.Value == "fixture.en" &&
            candidate.RejectionReason == TransferRejectionReason.ExplanationNotAllowed));
        Assert.IsTrue(result.Explanation.Candidates.Any(candidate =>
            candidate.MappingId.Value == "fixture.hi" &&
            candidate.RejectionReason == TransferRejectionReason.SkillNotComfortable));
    }

    [TestMethod]
    public void LowProficiencyCanFallBelowTheConfiguredThreshold()
    {
        var result = Route(
            Profile(Known("en", proficiency: LanguageProficiency.Beginner)),
            Mapping("en", 1));

        Assert.IsNull(result.Selection);
        Assert.AreEqual(
            TransferRejectionReason.BelowThreshold,
            result.Explanation.Candidates.Single().RejectionReason);
    }

    [TestMethod]
    public void NeverModeProducesNoBridge()
    {
        var result = Route(
            Profile(
                [Known("en")],
                new LearnerSettings(
                    MultilingualShortcutMode.Never,
                    null,
                    MicrophonePreference.Later,
                    false)),
            Mapping("en", 1));

        Assert.IsNull(result.Selection);
        Assert.AreEqual(
            TransferRejectionReason.ShortcutDisabled,
            result.Explanation.Candidates.Single().RejectionReason);
    }

    [TestMethod]
    public void CloseScoresUseThePreferredLanguageAndStableMappingIdTieBreak()
    {
        var profile = Profile(
            [Known("en"), Known("hi")],
            new LearnerSettings(
                MultilingualShortcutMode.PreferredLanguage,
                new LanguageCode("hi"),
                MicrophonePreference.Later,
                false));

        var result = Route(
            profile,
            Mapping("en", 0.92),
            Mapping("hi", 0.9, idSuffix: "z"),
            Mapping("hi", 0.9, idSuffix: "a"));

        Assert.AreEqual("fixture.hi.a", result.Selection?.Mapping.Id.Value);
        Assert.IsTrue(result.Explanation.Candidates.All(candidate => candidate.Eligible));
    }

    [TestMethod]
    public void SubstantiallyStrongerMappingOutranksThePreferredLanguage()
    {
        var profile = Profile(
            [Known("en"), Known("hi")],
            new LearnerSettings(
                MultilingualShortcutMode.PreferredLanguage,
                new LanguageCode("hi"),
                MicrophonePreference.Later,
                false));

        var result = Route(profile, Mapping("en", 1), Mapping("hi", 0.5));

        Assert.AreEqual("fixture.en", result.Selection?.Mapping.Id.Value);
    }

    [TestMethod]
    [DataRow(TransferRelation.Neutral, TransferReviewStatus.Approved, TransferRejectionReason.RelationNotDisplayable)]
    [DataRow(TransferRelation.Unknown, TransferReviewStatus.Approved, TransferRejectionReason.RelationNotDisplayable)]
    [DataRow(TransferRelation.Facilitative, TransferReviewStatus.Draft, TransferRejectionReason.Unapproved)]
    [DataRow(TransferRelation.Facilitative, TransferReviewStatus.Deprecated, TransferRejectionReason.Unapproved)]
    public void NonRuntimeMappingsAreRejected(
        TransferRelation relation,
        TransferReviewStatus reviewStatus,
        TransferRejectionReason expected)
    {
        var result = Route(
            Profile(Known("en")),
            Mapping("en", 1, relation: relation, reviewStatus: reviewStatus));

        Assert.IsNull(result.Selection);
        Assert.AreEqual(expected, result.Explanation.Candidates.Single().RejectionReason);
    }

    [TestMethod]
    public void InvalidAndBrokenMappingsAreRejectedWithoutHidingValidOnes()
    {
        var invalid = Mapping("en", double.NaN, idSuffix: "invalid");
        var broken = Mapping("en", 1, idSuffix: "broken") with
        {
            TargetConceptId = new ConceptId("fixture.other"),
        };
        var valid = Mapping("en", 0.8, idSuffix: "valid");

        var result = Route(Profile(Known("en")), invalid, broken, valid);

        Assert.AreEqual(valid.Id, result.Selection?.Mapping.Id);
        Assert.AreEqual(
            TransferRejectionReason.InvalidMapping,
            result.Explanation.Candidates.Single(candidate => candidate.MappingId == invalid.Id).RejectionReason);
        Assert.AreEqual(
            TransferRejectionReason.DifferentConcept,
            result.Explanation.Candidates.Single(candidate => candidate.MappingId == broken.Id).RejectionReason);
    }

    [TestMethod]
    public void DuplicateMappingIdsAreRejected()
    {
        var duplicate = Mapping("en", 0.8, idSuffix: "duplicate");
        var result = Route(Profile(Known("en")), duplicate, duplicate with { Strength = 0.9 });

        Assert.IsNull(result.Selection);
        Assert.HasCount(2, result.Explanation.Candidates);
        Assert.IsTrue(result.Explanation.Candidates.All(candidate =>
            candidate.RejectionReason == TransferRejectionReason.InvalidMapping));
    }

    [TestMethod]
    public void UnknownLanguageAndNoMappingsProduceNoInventedBridge()
    {
        var profile = Profile(Known("en"));

        var unknownLanguage = Route(profile, Mapping("hi", 1));
        var empty = Route(profile);

        Assert.IsNull(unknownLanguage.Selection);
        Assert.IsNull(empty.Selection);
        Assert.AreEqual(TransferRejectionReason.LanguageNotKnown,
            unknownLanguage.Explanation.Candidates.Single().RejectionReason);
        Assert.IsEmpty(empty.Explanation.Candidates);
    }

    [TestMethod]
    public void IdenticalInputsProduceIdenticalSelectionAndExplanationMetadata()
    {
        var profile = Profile(Known("en"), Known("hi"));
        var mappings = new[] { Mapping("hi", 0.8), Mapping("en", 0.8) };

        var first = Route(profile, mappings);
        var second = Route(profile, mappings);

        Assert.AreEqual(first.Selection, second.Selection);
        CollectionAssert.AreEqual(
            first.Explanation.Candidates.ToArray(),
            second.Explanation.Candidates.ToArray());
        Assert.AreEqual(first.Explanation.Summary, second.Explanation.Summary);
        Assert.IsTrue(mappings.Any(mapping => mapping.Id == first.Selection?.Mapping.Id));
    }

    [TestMethod]
    public void ChangingOneLanguagePreferenceChangesOnlyThatCandidatesEligibility()
    {
        var allowed = Route(Profile(Known("en"), Known("hi")), Mapping("en", 0.7), Mapping("hi", 0.8));
        var disallowed = Route(
            Profile(Known("en"), Known("hi", allowExplanations: false)),
            Mapping("en", 0.7),
            Mapping("hi", 0.8));

        Assert.AreEqual("fixture.hi", allowed.Selection?.Mapping.Id.Value);
        Assert.AreEqual("fixture.en", disallowed.Selection?.Mapping.Id.Value);
        Assert.AreEqual(
            allowed.Explanation.Candidates.Single(candidate => candidate.MappingId.Value == "fixture.en"),
            disallowed.Explanation.Candidates.Single(candidate => candidate.MappingId.Value == "fixture.en"));
    }

    [TestMethod]
    public void AskFirstRequiresConfirmationAndSpokenRoutingUsesListeningComfort()
    {
        var profile = Profile(
            Known("en", comfortableReading: true, comfortableListening: false));

        var written = Route(profile, TransferPresentationMode.Written, Mapping("en", 1));
        var spoken = Route(profile, TransferPresentationMode.Spoken, Mapping("en", 1));

        Assert.IsTrue(written.Selection?.RequiresConfirmation);
        Assert.IsNull(spoken.Selection);
        Assert.AreEqual(
            TransferRejectionReason.SkillNotComfortable,
            spoken.Explanation.Candidates.Single().RejectionReason);
    }

    private static TransferRoutingResult Route(
        LearnerProfile profile,
        params TransferMapping[] mappings) =>
        Route(profile, TransferPresentationMode.Written, mappings);

    private static TransferRoutingResult Route(
        LearnerProfile profile,
        TransferPresentationMode mode,
        params TransferMapping[] mappings) =>
        TransferRouter.Route(Concept, mappings, profile, mode, TransferRoutingConfiguration.Default);

    private static TransferMapping Mapping(
        string sourceLanguage,
        double strength,
        string? idSuffix = null,
        TransferRelation relation = TransferRelation.Facilitative,
        TransferReviewStatus reviewStatus = TransferReviewStatus.Approved) =>
        new(
            new TransferMappingId($"fixture.{sourceLanguage}{(idSuffix is null ? string.Empty : $".{idSuffix}")}"),
            new VersionId("mapping-v1"),
            new LanguageCode(sourceLanguage),
            new LanguageCode("de"),
            Concept.Id,
            relation,
            strength,
            reviewStatus);

    private static LearnerProfile Profile(params KnownLanguage[] languages) =>
        Profile(
            languages,
            new LearnerSettings(
                MultilingualShortcutMode.AskFirst,
                null,
                MicrophonePreference.Later,
                false));

    private static LearnerProfile Profile(
        IReadOnlyList<KnownLanguage> languages,
        LearnerSettings settings) =>
        new(Guid.NewGuid(), new LanguageCode("de"), languages, settings);

    private static KnownLanguage Known(
        string code,
        bool allowExplanations = true,
        bool comfortableReading = true,
        bool comfortableListening = true,
        LanguageProficiency proficiency = LanguageProficiency.Advanced) =>
        new(
            new LanguageCode(code),
            proficiency,
            comfortableReading,
            comfortableListening,
            allowExplanations);
}
