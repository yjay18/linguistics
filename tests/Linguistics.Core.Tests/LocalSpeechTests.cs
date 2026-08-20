using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.Core.Tests;

[TestClass]
public sealed class LocalSpeechTests
{
    [TestMethod]
    public void VoiceSelectionIsStableAcrossInventoryOrder()
    {
        var german = new LanguageCode("de");
        var voices = new[]
        {
            new SpeechVoice("z", "Z", "de-de", german),
            new SpeechVoice("a", "A", "de-at", german),
            new SpeechVoice("en", "English", "en-us", new LanguageCode("en")),
        };

        var first = SpeechVoiceSelector.Select(voices, german, "lesson-42");
        var second = SpeechVoiceSelector.Select(voices.Reverse().ToArray(), german, "lesson-42");

        Assert.IsNotNull(first);
        Assert.AreEqual(first, second);
        Assert.AreEqual(german, first.Language);
    }

    [TestMethod]
    public void MissingTargetLanguageVoiceReturnsNoSelection()
    {
        var selected = SpeechVoiceSelector.Select(
            [new SpeechVoice("en", "English", "en-us", new LanguageCode("en"))],
            new LanguageCode("de"),
            "fixed-seed");

        Assert.IsNull(selected);
    }

    [TestMethod]
    public void ExactRecognizedPhraseProducesOnlyTranscriptBasedEvidence()
    {
        var provider = new TranscriptPronunciationAssessmentProvider();

        var result = provider.Assess(
            new PronunciationAssessmentRequest(
                "Ich möchte einen Kaffee, bitte.",
                "ich möchte einen kaffee bitte",
                TimeSpan.FromSeconds(4)),
            "fixture-recognizer-v1");

        Assert.AreEqual(PronunciationAssessmentOutcome.Intelligible, result.Evidence.Outcome);
        Assert.AreEqual(1, result.Evidence.Intelligibility);
        Assert.AreEqual(5, result.Evidence.ExpectedWordCount);
        Assert.AreEqual(5, result.Evidence.MatchedWordCount);
        Assert.IsEmpty(result.MissingExpectedWords);
        Assert.IsEmpty(result.UnexpectedRecognizedWords);
        Assert.AreEqual(
            TranscriptPronunciationAssessmentProvider.Version,
            result.Evidence.AssessmentVersion);
    }

    [TestMethod]
    public void PartialRecognitionReportsWordDifferencesWithoutPhonemeClaims()
    {
        var provider = new TranscriptPronunciationAssessmentProvider();

        var result = provider.Assess(
            new PronunciationAssessmentRequest(
                "Ich möchte einen Kaffee, bitte.",
                "Ich Kaffee heute",
                TimeSpan.FromSeconds(3)),
            "fixture-recognizer-v1");

        Assert.AreEqual(PronunciationAssessmentOutcome.PartlyIntelligible, result.Evidence.Outcome);
        Assert.AreEqual(2, result.Evidence.MatchedWordCount);
        CollectionAssert.Contains(result.MissingExpectedWords.ToArray(), "möchte");
        CollectionAssert.Contains(result.UnexpectedRecognizedWords.ToArray(), "heute");
        Assert.IsFalse(result.Message.Contains("phoneme", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.Message.Contains("accent", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void EmptyRecognitionCreatesNoNumericEvidence()
    {
        var provider = new TranscriptPronunciationAssessmentProvider();

        var result = provider.Assess(
            new PronunciationAssessmentRequest(
                "Guten Tag!",
                string.Empty,
                TimeSpan.FromSeconds(10)),
            "fixture-recognizer-v1");

        Assert.AreEqual(PronunciationAssessmentOutcome.NoSpeech, result.Evidence.Outcome);
        Assert.IsNull(result.Evidence.Intelligibility);
        Assert.AreEqual(0, result.Evidence.RecognizedWordCount);
    }

    [TestMethod]
    public void PronunciationHistoryRejectsUnsupportedNumericEvidence()
    {
        var history = new PronunciationHistory(
        [
            new PronunciationAttempt(
                Guid.NewGuid(),
                "de.utterance.order",
                DateTimeOffset.UtcNow,
                new PronunciationEvidence(
                    PronunciationAssessmentOutcome.Intelligible,
                    1.2,
                    5,
                    5,
                    5,
                    TimeSpan.FromSeconds(4),
                    "fixture-recognizer-v1",
                    TranscriptPronunciationAssessmentProvider.Version),
                "language.de.core.v1"),
        ]);

        Assert.ThrowsExactly<ArgumentException>(() =>
            PronunciationHistoryValidator.Validate(history));
    }
}
