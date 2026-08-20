using System.Security.Cryptography;
using System.Text;
using Linguistics.Core.Profiles;

namespace Linguistics.Core.Speech;

public enum SpeechCapabilityStatus
{
    Available,
    Unavailable,
    Misconfigured,
}

public sealed record SpeechVoice(
    string Id,
    string Name,
    string Locale,
    LanguageCode Language);

public sealed record SpeechSynthesisSnapshot(
    SpeechCapabilityStatus Status,
    IReadOnlyList<SpeechVoice> Voices,
    string Message);

public sealed record SpeechSynthesisRequest(
    Guid RequestId,
    string Text,
    LanguageCode Language,
    string Seed,
    string? VoiceId = null,
    double Rate = 1);

public enum SpeechSynthesisResultStatus
{
    Completed,
    Unavailable,
    InvalidRequest,
    Failed,
    Cancelled,
}

public sealed record SpeechSynthesisResult(
    Guid RequestId,
    SpeechSynthesisResultStatus Status,
    string? VoiceId,
    TimeSpan Duration,
    string Message);

public interface ISpeechSynthesisProvider : IDisposable
{
    Task<SpeechSynthesisSnapshot> InspectAsync(CancellationToken cancellationToken = default);

    Task<SpeechSynthesisResult> SpeakAsync(
        SpeechSynthesisRequest request,
        CancellationToken cancellationToken = default);

    Task StopAsync();
}

public static class SpeechVoiceSelector
{
    public static SpeechVoice? Select(
        IReadOnlyList<SpeechVoice> voices,
        LanguageCode language,
        string seed)
    {
        ArgumentNullException.ThrowIfNull(voices);
        ArgumentException.ThrowIfNullOrWhiteSpace(seed);

        var matching = voices
            .Where(voice => voice is not null && voice.Language == language)
            .OrderBy(voice => voice.Id, StringComparer.Ordinal)
            .ToArray();
        if (matching.Length == 0)
        {
            return null;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        var index = (int)(BitConverter.ToUInt32(hash, 0) % (uint)matching.Length);
        return matching[index];
    }
}

public sealed record SpeechModelDescriptor(
    string Name,
    long SizeBytes,
    string Source,
    string License,
    string ProviderVersion);

public sealed record SpeechRecognitionSnapshot(
    SpeechCapabilityStatus Status,
    SpeechModelDescriptor? Model,
    string Message);

public sealed record SpeechRecognitionRequest(
    Guid RequestId,
    LanguageCode Language,
    TimeSpan MaximumDuration,
    bool RetainAudio);

public enum SpeechRecognitionResultStatus
{
    Accepted,
    Unavailable,
    InvalidRequest,
    PermissionDenied,
    MicrophoneUnavailable,
    NoSpeech,
    Failed,
    Cancelled,
}

public enum LearnerInputMode
{
    Text,
    Speech,
}

public sealed record SpeechRecognitionResult(
    Guid RequestId,
    SpeechRecognitionResultStatus Status,
    string? Transcript,
    LanguageCode Language,
    TimeSpan Duration,
    string ProviderVersion,
    string? ModelName,
    string Message);

public interface ISpeechRecognitionProvider : IDisposable
{
    Task<SpeechRecognitionSnapshot> InspectAsync(CancellationToken cancellationToken = default);

    Task<SpeechRecognitionResult> RecognizeAsync(
        SpeechRecognitionRequest request,
        CancellationToken cancellationToken = default);
}

public enum PronunciationAssessmentOutcome
{
    Intelligible,
    PartlyIntelligible,
    NotIntelligible,
    NoSpeech,
}

public sealed record PronunciationAssessmentRequest(
    string ExpectedText,
    string RecognizedText,
    TimeSpan Duration);

public sealed record PronunciationEvidence(
    PronunciationAssessmentOutcome Outcome,
    double? Intelligibility,
    int ExpectedWordCount,
    int RecognizedWordCount,
    int MatchedWordCount,
    TimeSpan Duration,
    string RecognitionProviderVersion,
    string AssessmentVersion);

public sealed record PronunciationAssessmentResult(
    PronunciationEvidence Evidence,
    IReadOnlyList<string> ExpectedWords,
    IReadOnlyList<string> RecognizedWords,
    IReadOnlyList<string> MissingExpectedWords,
    IReadOnlyList<string> UnexpectedRecognizedWords,
    string Message);

public interface IPronunciationAssessmentProvider
{
    PronunciationAssessmentResult Assess(
        PronunciationAssessmentRequest request,
        string recognitionProviderVersion);
}

public sealed class TranscriptPronunciationAssessmentProvider : IPronunciationAssessmentProvider
{
    public const string Version = "transcript-intelligibility-v1";

    public PronunciationAssessmentResult Assess(
        PronunciationAssessmentRequest request,
        string recognitionProviderVersion)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.ExpectedText);
        ArgumentNullException.ThrowIfNull(request.RecognizedText);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ExpectedText);
        ArgumentException.ThrowIfNullOrWhiteSpace(recognitionProviderVersion);
        if (request.Duration < TimeSpan.Zero || request.ExpectedText.Length > 500 || request.RecognizedText.Length > 500)
        {
            throw new ArgumentException("The pronunciation assessment request is invalid.", nameof(request));
        }

        var expected = Tokenize(request.ExpectedText);
        if (expected.Length == 0)
        {
            throw new ArgumentException("The expected phrase has no comparable words.", nameof(request));
        }

        var recognized = Tokenize(request.RecognizedText);
        var matched = LongestCommonSubsequenceLength(expected, recognized);
        double? intelligibility = recognized.Length == 0
            ? null
            : matched / (double)expected.Length;
        var outcome = intelligibility switch
        {
            null => PronunciationAssessmentOutcome.NoSpeech,
            >= 0.8 => PronunciationAssessmentOutcome.Intelligible,
            >= 0.4 => PronunciationAssessmentOutcome.PartlyIntelligible,
            _ => PronunciationAssessmentOutcome.NotIntelligible,
        };
        var missing = MultisetDifference(expected, recognized);
        var unexpected = MultisetDifference(recognized, expected);
        var evidence = new PronunciationEvidence(
            outcome,
            intelligibility,
            expected.Length,
            recognized.Length,
            matched,
            request.Duration,
            recognitionProviderVersion,
            Version);
        return new PronunciationAssessmentResult(
            evidence,
            expected,
            recognized,
            missing,
            unexpected,
            Message(outcome));
    }

    private static string[] Tokenize(string value)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        foreach (var character in value.Normalize().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                current.Append(character);
            }
            else if (current.Length > 0)
            {
                tokens.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens.ToArray();
    }

    private static int LongestCommonSubsequenceLength(
        IReadOnlyList<string> expected,
        IReadOnlyList<string> recognized)
    {
        var previous = new int[recognized.Count + 1];
        var current = new int[recognized.Count + 1];
        for (var expectedIndex = 1; expectedIndex <= expected.Count; expectedIndex++)
        {
            for (var recognizedIndex = 1; recognizedIndex <= recognized.Count; recognizedIndex++)
            {
                current[recognizedIndex] = expected[expectedIndex - 1] == recognized[recognizedIndex - 1]
                    ? previous[recognizedIndex - 1] + 1
                    : Math.Max(previous[recognizedIndex], current[recognizedIndex - 1]);
            }

            (previous, current) = (current, previous);
            Array.Clear(current);
        }

        return previous[recognized.Count];
    }

    private static string[] MultisetDifference(
        IReadOnlyList<string> values,
        IReadOnlyList<string> subtract)
    {
        var remaining = subtract
            .GroupBy(value => value, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var difference = new List<string>();
        foreach (var value in values)
        {
            if (remaining.TryGetValue(value, out var count) && count > 0)
            {
                remaining[value] = count - 1;
            }
            else
            {
                difference.Add(value);
            }
        }

        return difference.ToArray();
    }

    private static string Message(PronunciationAssessmentOutcome outcome) => outcome switch
    {
        PronunciationAssessmentOutcome.Intelligible =>
            "The local recognizer captured most of the expected words under this recording condition.",
        PronunciationAssessmentOutcome.PartlyIntelligible =>
            "The local recognizer captured part of the expected phrase; compare the words and try once more.",
        PronunciationAssessmentOutcome.NotIntelligible =>
            "The local recognizer captured too few expected words to support an intelligibility result.",
        _ => "No comparable speech was recognized, so no pronunciation result was created.",
    };
}

public sealed record PronunciationAttempt(
    Guid Id,
    string UtteranceId,
    DateTimeOffset OccurredAt,
    PronunciationEvidence Evidence,
    string ContentVersion);

public sealed record PronunciationHistory(IReadOnlyList<PronunciationAttempt> Attempts)
{
    public static PronunciationHistory Empty => new([]);
}

public static class PronunciationHistoryValidator
{
    public static void Validate(PronunciationHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);
        var errors = new List<string>();
        if (history.Attempts is null)
        {
            errors.Add("The pronunciation-attempt collection is missing.");
        }

        var attempts = (history.Attempts ?? []).OfType<PronunciationAttempt>().ToArray();
        if (history.Attempts is not null && attempts.Length != history.Attempts.Count)
        {
            errors.Add("A pronunciation attempt is missing.");
        }

        foreach (var duplicate in attempts.GroupBy(attempt => attempt.Id).Where(group => group.Count() > 1))
        {
            errors.Add($"Pronunciation attempt '{duplicate.Key}' appears more than once.");
        }

        foreach (var attempt in attempts)
        {
            var evidence = attempt.Evidence;
            if (attempt.Id == Guid.Empty ||
                string.IsNullOrWhiteSpace(attempt.UtteranceId) ||
                attempt.OccurredAt == default ||
                string.IsNullOrWhiteSpace(attempt.ContentVersion) ||
                evidence is null ||
                evidence.Duration < TimeSpan.Zero ||
                evidence.ExpectedWordCount <= 0 ||
                evidence.RecognizedWordCount < 0 ||
                evidence.MatchedWordCount < 0 ||
                evidence.MatchedWordCount > evidence.ExpectedWordCount ||
                evidence.MatchedWordCount > evidence.RecognizedWordCount ||
                string.IsNullOrWhiteSpace(evidence.RecognitionProviderVersion) ||
                string.IsNullOrWhiteSpace(evidence.AssessmentVersion) ||
                !Enum.IsDefined(evidence.Outcome) ||
                (evidence.Intelligibility is { } value &&
                 (double.IsNaN(value) || double.IsInfinity(value) || value is < 0 or > 1)) ||
                (evidence.Outcome == PronunciationAssessmentOutcome.NoSpeech && evidence.Intelligibility is not null) ||
                (evidence.Outcome != PronunciationAssessmentOutcome.NoSpeech && evidence.Intelligibility is null))
            {
                errors.Add($"Pronunciation attempt '{attempt?.Id}' is invalid.");
            }
        }

        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(Environment.NewLine, errors), nameof(history));
        }
    }
}
