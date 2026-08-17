namespace Linguistics.Core.Profiles;

public static class LearnerProfileValidator
{
    public static void Validate(LearnerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var errors = new List<string>();
        if (profile.Id == Guid.Empty)
        {
            errors.Add("The learner profile identifier is missing.");
        }

        if (string.IsNullOrWhiteSpace(profile.TargetLanguage.Value))
        {
            errors.Add("The target language is missing.");
        }

        if (profile.KnownLanguages is null)
        {
            errors.Add("The known-language collection is missing.");
        }
        else
        {
            var duplicates = profile.KnownLanguages
                .Where(language => language is not null)
                .GroupBy(language => language.Language)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key.Value);

            foreach (var duplicate in duplicates)
            {
                errors.Add($"Known language '{duplicate}' appears more than once.");
            }

            foreach (var language in profile.KnownLanguages)
            {
                if (language is null)
                {
                    errors.Add("A known-language entry is missing.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(language.Language.Value))
                {
                    errors.Add("A known-language identifier is missing.");
                }

                if (!Enum.IsDefined(language.Proficiency))
                {
                    errors.Add($"Known language '{language.Language}' has an invalid proficiency.");
                }
            }
        }

        if (profile.Settings is null)
        {
            errors.Add("Learner settings are missing.");
        }
        else
        {
            if (!Enum.IsDefined(profile.Settings.ShortcutMode))
            {
                errors.Add("The multilingual shortcut mode is invalid.");
            }

            if (!Enum.IsDefined(profile.Settings.Microphone))
            {
                errors.Add("The microphone preference is invalid.");
            }

            ValidatePreferredLanguage(profile, errors);
        }

        if (errors.Count > 0)
        {
            throw new LearnerProfileValidationException(errors);
        }
    }

    private static void ValidatePreferredLanguage(
        LearnerProfile profile,
        ICollection<string> errors)
    {
        var preferred = profile.Settings.PreferredExplanationLanguage;
        if (profile.Settings.ShortcutMode != MultilingualShortcutMode.PreferredLanguage)
        {
            if (preferred is not null)
            {
                errors.Add("A preferred explanation language is set when preferred-language routing is off.");
            }

            return;
        }

        if (preferred is null)
        {
            errors.Add("Preferred-language routing requires an explanation language.");
            return;
        }

        var eligible = profile.KnownLanguages?.Any(language =>
            language.Language == preferred.Value && language.AllowExplanations) == true;

        if (!eligible)
        {
            errors.Add("The preferred explanation language must be an allowed known language.");
        }
    }
}

public sealed class LearnerProfileValidationException : Exception
{
    public LearnerProfileValidationException(IReadOnlyList<string> errors)
        : base(string.Join(" ", errors))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}
