using Linguistics.Core.Profiles;

namespace Linguistics.Core.Content;

public enum InstructionLanguageRejectionReason
{
    None,
    LanguageNotKnown,
    ExplanationNotAllowed,
    ReadingNotComfortable,
    KnownLanguageExplanationsDisabled,
}

public enum InstructionLanguageSelectionReason
{
    PreferredLanguage,
    EligibleKnownLanguage,
    TargetLanguageFallback,
    Unavailable,
}

public sealed record InstructionLanguageCandidateDecision(
    LanguageCode Language,
    bool IsTargetLanguage,
    bool IsKnownLanguage,
    bool? AllowsExplanations,
    bool? ComfortableReading,
    bool Eligible,
    InstructionLanguageRejectionReason RejectionReason);

public sealed record InstructionLanguageSelectionExplanation(
    LanguageCode? PreferredLanguage,
    IReadOnlyList<InstructionLanguageCandidateDecision> Candidates,
    InstructionLanguageSelectionReason SelectionReason,
    string Summary);

public sealed record InstructionLanguageSelectionResult(
    LanguageCode? SelectedLanguage,
    InstructionLanguageSelectionExplanation Explanation);

public static class InstructionLanguageSelector
{
    public static InstructionLanguageSelectionResult Select(
        LearnerProfile profile,
        IEnumerable<LanguageCode> packInstructionLanguages)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(packInstructionLanguages);
        LearnerProfileValidator.Validate(profile);

        var declaredLanguages = packInstructionLanguages
            .Distinct()
            .OrderBy(language => language.Value, StringComparer.Ordinal)
            .ToArray();
        if (declaredLanguages.Any(language => string.IsNullOrWhiteSpace(language.Value)))
        {
            throw new ArgumentException(
                "Pack instruction languages must contain valid language codes.",
                nameof(packInstructionLanguages));
        }

        var candidates = declaredLanguages
            .Select(language => Evaluate(language, profile))
            .ToArray();
        var preferredLanguage = profile.Settings.PreferredExplanationLanguage;

        var preferred = preferredLanguage is null
            ? null
            : candidates.SingleOrDefault(candidate =>
                candidate.Language == preferredLanguage.Value && candidate.Eligible);
        if (preferred is not null)
        {
            return Result(
                preferred.Language,
                preferredLanguage,
                candidates,
                InstructionLanguageSelectionReason.PreferredLanguage,
                $"Selected preferred instruction language '{preferred.Language}'.");
        }

        if (profile.Settings.ShortcutMode != MultilingualShortcutMode.Never)
        {
            var knownLanguage = candidates.FirstOrDefault(candidate =>
                candidate.Eligible && !candidate.IsTargetLanguage);
            if (knownLanguage is not null)
            {
                return Result(
                    knownLanguage.Language,
                    preferredLanguage,
                    candidates,
                    InstructionLanguageSelectionReason.EligibleKnownLanguage,
                    preferredLanguage is null
                        ? $"Selected eligible known language '{knownLanguage.Language}' using stable language-code order."
                        : $"Preferred instruction language '{preferredLanguage}' was unavailable; selected eligible known language '{knownLanguage.Language}' using stable language-code order.");
            }
        }

        var targetLanguage = candidates.SingleOrDefault(candidate =>
            candidate.IsTargetLanguage && candidate.Eligible);
        if (targetLanguage is not null)
        {
            return Result(
                targetLanguage.Language,
                preferredLanguage,
                candidates,
                InstructionLanguageSelectionReason.TargetLanguageFallback,
                $"Selected target language '{targetLanguage.Language}' because no permitted known-language instruction was available.");
        }

        return Result(
            null,
            preferredLanguage,
            candidates,
            InstructionLanguageSelectionReason.Unavailable,
            profile.Settings.ShortcutMode == MultilingualShortcutMode.Never
                ? $"Known-language explanations are disabled and target language '{profile.TargetLanguage}' is not declared by the pack."
                : "No pack-declared instruction language met the learner's explanation consent and reading-comfort settings.");
    }

    private static InstructionLanguageCandidateDecision Evaluate(
        LanguageCode language,
        LearnerProfile profile)
    {
        if (language == profile.TargetLanguage)
        {
            return new InstructionLanguageCandidateDecision(
                language,
                IsTargetLanguage: true,
                IsKnownLanguage: false,
                AllowsExplanations: null,
                ComfortableReading: null,
                Eligible: true,
                InstructionLanguageRejectionReason.None);
        }

        var knownLanguage = profile.KnownLanguages.SingleOrDefault(candidate =>
            candidate.Language == language);
        if (knownLanguage is null)
        {
            return Rejected(
                language,
                InstructionLanguageRejectionReason.LanguageNotKnown);
        }

        if (profile.Settings.ShortcutMode == MultilingualShortcutMode.Never)
        {
            return Rejected(
                language,
                InstructionLanguageRejectionReason.KnownLanguageExplanationsDisabled,
                knownLanguage);
        }

        if (!knownLanguage.AllowExplanations)
        {
            return Rejected(
                language,
                InstructionLanguageRejectionReason.ExplanationNotAllowed,
                knownLanguage);
        }

        if (!knownLanguage.ComfortableReading)
        {
            return Rejected(
                language,
                InstructionLanguageRejectionReason.ReadingNotComfortable,
                knownLanguage);
        }

        return new InstructionLanguageCandidateDecision(
            language,
            IsTargetLanguage: false,
            IsKnownLanguage: true,
            knownLanguage.AllowExplanations,
            knownLanguage.ComfortableReading,
            Eligible: true,
            InstructionLanguageRejectionReason.None);
    }

    private static InstructionLanguageCandidateDecision Rejected(
        LanguageCode language,
        InstructionLanguageRejectionReason reason,
        KnownLanguage? knownLanguage = null) =>
        new(
            language,
            IsTargetLanguage: false,
            IsKnownLanguage: knownLanguage is not null,
            knownLanguage?.AllowExplanations,
            knownLanguage?.ComfortableReading,
            Eligible: false,
            reason);

    private static InstructionLanguageSelectionResult Result(
        LanguageCode? selectedLanguage,
        LanguageCode? preferredLanguage,
        IReadOnlyList<InstructionLanguageCandidateDecision> candidates,
        InstructionLanguageSelectionReason selectionReason,
        string summary) =>
        new(
            selectedLanguage,
            new InstructionLanguageSelectionExplanation(
                preferredLanguage,
                candidates,
                selectionReason,
                summary));
}
