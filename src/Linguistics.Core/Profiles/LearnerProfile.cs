using System.Text.Json.Serialization;

namespace Linguistics.Core.Profiles;

public readonly record struct LanguageCode
{
    [JsonConstructor]
    public LanguageCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim().ToLowerInvariant();
        if (!IsValid(normalized))
        {
            throw new ArgumentException("Language codes must be valid BCP 47-style identifiers.", nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString() => Value;

    private static bool IsValid(string value)
    {
        if (value.Length is < 2 or > 35)
        {
            return false;
        }

        var parts = value.Split('-');
        if (parts[0].Length is < 2 or > 3 || !parts[0].All(IsAsciiLetter))
        {
            return false;
        }

        return parts.Skip(1).All(part =>
            part.Length is >= 1 and <= 8 && part.All(IsAsciiLetterOrDigit));
    }

    private static bool IsAsciiLetter(char value) =>
        value is >= 'a' and <= 'z';

    private static bool IsAsciiLetterOrDigit(char value) =>
        IsAsciiLetter(value) || value is >= '0' and <= '9';
}

public enum LanguageProficiency
{
    Beginner,
    Intermediate,
    Advanced,
    NativeOrNearNative,
}

public enum MultilingualShortcutMode
{
    Automatic,
    AskFirst,
    PreferredLanguage,
    Never,
}

public enum MicrophonePreference
{
    Now,
    Later,
    Never,
}

public sealed record KnownLanguage(
    LanguageCode Language,
    LanguageProficiency Proficiency,
    bool ComfortableReading,
    bool ComfortableListening,
    bool AllowExplanations);

public sealed record LearnerSettings(
    MultilingualShortcutMode ShortcutMode,
    LanguageCode? PreferredExplanationLanguage,
    MicrophonePreference Microphone,
    bool RetainSpeechRecordings,
    string? SelectedLocalModel = null,
    bool ReduceMotion = false);

public sealed record LearnerProfile(
    Guid Id,
    LanguageCode TargetLanguage,
    IReadOnlyList<KnownLanguage> KnownLanguages,
    LearnerSettings Settings);

public sealed record NewLearnerProfile(
    LanguageCode TargetLanguage,
    IReadOnlyList<KnownLanguage> KnownLanguages,
    LearnerSettings Settings);
