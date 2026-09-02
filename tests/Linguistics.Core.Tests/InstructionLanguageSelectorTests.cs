using Linguistics.Core.Content;
using Linguistics.Core.Profiles;

namespace Linguistics.Core.Tests;

[TestClass]
public sealed class InstructionLanguageSelectorTests
{
    [TestMethod]
    public void PreferredEligibleLanguageIsSelected()
    {
        var result = InstructionLanguageSelector.Select(
            Profile(
                [Known("en"), Known("hi")],
                MultilingualShortcutMode.PreferredLanguage,
                preferred: "hi"),
            Languages("en", "hi"));

        Assert.AreEqual(new LanguageCode("hi"), result.SelectedLanguage);
        Assert.AreEqual(
            InstructionLanguageSelectionReason.PreferredLanguage,
            result.Explanation.SelectionReason);
        StringAssert.Contains(result.Explanation.Summary, "preferred instruction language 'hi'");
    }

    [TestMethod]
    public void UnsupportedPreferredLanguageUsesStableEligibleFallback()
    {
        var result = InstructionLanguageSelector.Select(
            Profile(
                [Known("en"), Known("hi")],
                MultilingualShortcutMode.PreferredLanguage,
                preferred: "hi"),
            Languages("en"));

        Assert.AreEqual(new LanguageCode("en"), result.SelectedLanguage);
        Assert.AreEqual(
            InstructionLanguageSelectionReason.EligibleKnownLanguage,
            result.Explanation.SelectionReason);
        StringAssert.Contains(result.Explanation.Summary, "Preferred instruction language 'hi' was unavailable");
    }

    [TestMethod]
    public void UncomfortablePreferredLanguageUsesAnotherEligibleLanguage()
    {
        var result = InstructionLanguageSelector.Select(
            Profile(
                [Known("en"), Known("hi", comfortableReading: false)],
                MultilingualShortcutMode.PreferredLanguage,
                preferred: "hi"),
            Languages("hi", "en"));

        Assert.AreEqual(new LanguageCode("en"), result.SelectedLanguage);
        Assert.AreEqual(
            InstructionLanguageRejectionReason.ReadingNotComfortable,
            Decision(result, "hi").RejectionReason);
    }

    [TestMethod]
    public void ConsentReadingAndKnownLanguageChecksExplainEveryRejection()
    {
        var result = InstructionLanguageSelector.Select(
            Profile(
                [
                    Known("en", allowExplanations: false),
                    Known("hi", comfortableReading: false),
                ]),
            Languages("fr", "hi", "en"));

        Assert.IsNull(result.SelectedLanguage);
        Assert.AreEqual(
            InstructionLanguageRejectionReason.ExplanationNotAllowed,
            Decision(result, "en").RejectionReason);
        Assert.AreEqual(
            InstructionLanguageRejectionReason.LanguageNotKnown,
            Decision(result, "fr").RejectionReason);
        Assert.AreEqual(
            InstructionLanguageRejectionReason.ReadingNotComfortable,
            Decision(result, "hi").RejectionReason);
        Assert.AreEqual(
            InstructionLanguageSelectionReason.Unavailable,
            result.Explanation.SelectionReason);
    }

    [TestMethod]
    public void EligibleFallbackIsIndependentOfInputOrdering()
    {
        var first = InstructionLanguageSelector.Select(
            Profile([Known("hi"), Known("en")]),
            Languages("hi", "en"));
        var second = InstructionLanguageSelector.Select(
            Profile([Known("en"), Known("hi")]),
            Languages("en", "hi"));

        Assert.AreEqual(new LanguageCode("en"), first.SelectedLanguage);
        Assert.AreEqual(first.SelectedLanguage, second.SelectedLanguage);
        CollectionAssert.AreEqual(
            first.Explanation.Candidates.ToArray(),
            second.Explanation.Candidates.ToArray());
        Assert.AreEqual(first.Explanation.Summary, second.Explanation.Summary);
    }

    [TestMethod]
    public void TargetLanguageIsTheFinalFallback()
    {
        var result = InstructionLanguageSelector.Select(
            Profile([Known("en", allowExplanations: false)]),
            Languages("en", "de"));

        Assert.AreEqual(new LanguageCode("de"), result.SelectedLanguage);
        Assert.AreEqual(
            InstructionLanguageSelectionReason.TargetLanguageFallback,
            result.Explanation.SelectionReason);
        Assert.IsTrue(Decision(result, "de").Eligible);
    }

    [TestMethod]
    public void NeverModeUsesOnlyADeclaredTargetLanguage()
    {
        var result = InstructionLanguageSelector.Select(
            Profile([Known("en")], MultilingualShortcutMode.Never),
            Languages("en", "de"));

        Assert.AreEqual(new LanguageCode("de"), result.SelectedLanguage);
        Assert.AreEqual(
            InstructionLanguageRejectionReason.KnownLanguageExplanationsDisabled,
            Decision(result, "en").RejectionReason);
        Assert.AreEqual(
            InstructionLanguageSelectionReason.TargetLanguageFallback,
            result.Explanation.SelectionReason);
    }

    [TestMethod]
    public void NeverModeIsUnavailableWithoutTargetLanguageInstruction()
    {
        var result = InstructionLanguageSelector.Select(
            Profile([Known("en")], MultilingualShortcutMode.Never),
            Languages("en"));

        Assert.IsNull(result.SelectedLanguage);
        Assert.AreEqual(
            InstructionLanguageSelectionReason.Unavailable,
            result.Explanation.SelectionReason);
        StringAssert.Contains(result.Explanation.Summary, "target language 'de' is not declared");
    }

    [TestMethod]
    public void EmptyPackLanguageSetProducesExplainedUnavailableResult()
    {
        var result = InstructionLanguageSelector.Select(Profile([Known("en")]), []);

        Assert.IsNull(result.SelectedLanguage);
        Assert.IsEmpty(result.Explanation.Candidates);
        Assert.AreEqual(
            InstructionLanguageSelectionReason.Unavailable,
            result.Explanation.SelectionReason);
    }

    [TestMethod]
    public void InvalidProfileAndDefaultPackLanguageFailClosed()
    {
        var invalidProfile = Profile(
            [Known("en")],
            MultilingualShortcutMode.PreferredLanguage,
            preferred: null);

        Assert.ThrowsExactly<LearnerProfileValidationException>(() =>
            InstructionLanguageSelector.Select(invalidProfile, Languages("en")));
        Assert.ThrowsExactly<ArgumentException>(() =>
            InstructionLanguageSelector.Select(Profile([Known("en")]), [default]));
    }

    private static InstructionLanguageCandidateDecision Decision(
        InstructionLanguageSelectionResult result,
        string language) =>
        result.Explanation.Candidates.Single(candidate => candidate.Language.Value == language);

    private static LearnerProfile Profile(
        IReadOnlyList<KnownLanguage> knownLanguages,
        MultilingualShortcutMode mode = MultilingualShortcutMode.Automatic,
        string? preferred = null) =>
        new(
            Guid.Parse("7b8fc7d1-137d-4f52-9c8e-16fd875298a8"),
            new LanguageCode("de"),
            knownLanguages,
            new LearnerSettings(
                mode,
                preferred is null ? null : new LanguageCode(preferred),
                MicrophonePreference.Later,
                RetainSpeechRecordings: false));

    private static KnownLanguage Known(
        string language,
        bool allowExplanations = true,
        bool comfortableReading = true) =>
        new(
            new LanguageCode(language),
            LanguageProficiency.Advanced,
            comfortableReading,
            ComfortableListening: true,
            allowExplanations);

    private static LanguageCode[] Languages(params string[] languages) =>
        languages.Select(language => new LanguageCode(language)).ToArray();
}
