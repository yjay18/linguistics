using Avalonia.Controls;
using Linguistics.App.Content;
using Linguistics.Core.Content;
using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.App.Features.Learn.Templates;

internal delegate Control TemplateRendererFactory(
    ContentImageCache? imageCache,
    ResolvedTemplateParameters parameters,
    LanguageCode instructionLanguage,
    bool shouldReduceMotion,
    Action<TemplateOutcome> reportOutcome);

internal sealed class TemplateRegistry
{
    private readonly IReadOnlyDictionary<TemplateId, TemplateRendererFactory> _renderers;
    private readonly ContentImageCache? _imageCache;

    public TemplateRegistry(IEnumerable<KeyValuePair<TemplateId, TemplateRendererFactory>> renderers)
        : this(null, renderers)
    {
    }

    public TemplateRegistry(
        ContentImageCache? imageCache,
        IEnumerable<KeyValuePair<TemplateId, TemplateRendererFactory>> renderers)
    {
        ArgumentNullException.ThrowIfNull(renderers);
        _imageCache = imageCache;

        var registrations = renderers.ToArray();
        var duplicate = registrations
            .GroupBy(registration => registration.Key)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Template renderer '{duplicate.Key}' is registered more than once.",
                nameof(renderers));
        }

        _renderers = registrations.ToDictionary(registration => registration.Key, registration => registration.Value);
        RegisteredTemplateIds = _renderers.Keys
            .OrderBy(id => id.Value, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<TemplateId> RegisteredTemplateIds { get; }

    public static TemplateRegistry CreateDefault(
        ContentImageCache? imageCache = null,
        ISpeechSynthesisProvider? speechSynthesisProvider = null,
        ISpeechRecognitionProvider? speechRecognitionProvider = null,
        IPronunciationAssessmentProvider? pronunciationAssessmentProvider = null,
        bool microphoneAllowed = false) => new(
        imageCache,
    [
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("scene-establish"),
            SceneEstablishRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("object-spotlight"),
            ObjectSpotlightRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("object-anatomy"),
            ObjectAnatomyRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("paper-dialogue"),
            (cache, parameters, instructionLanguage, shouldReduceMotion, reportOutcome) =>
                PaperDialogueRenderer.Render(
                    cache,
                    speechSynthesisProvider,
                    parameters,
                    instructionLanguage,
                    shouldReduceMotion,
                    reportOutcome)),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("street-walk"),
            StreetWalkRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("postcard-story"),
            PostcardStoryRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("photo-album"),
            PhotoAlbumRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("culture-plate"),
            CulturePlateRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("weather-window"),
            WeatherWindowRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("clock-theatre"),
            ClockTheatreRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("picture-match"),
            (cache, parameters, instructionLanguage, shouldReduceMotion, reportOutcome) =>
                PictureMatchRenderer.Render(
                    cache,
                    speechSynthesisProvider,
                    parameters,
                    instructionLanguage,
                    shouldReduceMotion,
                    reportOutcome)),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("word-match"),
            WordMatchRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("pair-cards"),
            PairCardsRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("odd-one-out"),
            OddOneOutRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("sort-into-baskets"),
            SortIntoBasketsRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("article-stamp"),
            ArticleStampRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("plural-fold"),
            PluralFoldRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("color-swatch"),
            ColorSwatchRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("number-tiles"),
            NumberTilesRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("label-the-scene"),
            LabelTheSceneRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("word-order-train"),
            WordOrderTrainRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("gap-card"),
            GapCardRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("sentence-fold"),
            SentenceFoldRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("conjugation-wheel"),
            ConjugationWheelRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("case-switchboard"),
            CaseSwitchboardRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("separable-verb-split"),
            SeparableVerbSplitRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("question-flip"),
            QuestionFlipRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("negation-strike"),
            NegationStrikeRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("preposition-stage"),
            PrepositionStageRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("sentence-expand"),
            SentenceExpandRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("listen-pick-image"),
            (cache, parameters, instructionLanguage, shouldReduceMotion, reportOutcome) =>
                ListenPickImageRenderer.Render(
                    cache,
                    speechSynthesisProvider,
                    parameters,
                    instructionLanguage,
                    shouldReduceMotion,
                    reportOutcome)),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("listen-order"),
            (cache, parameters, instructionLanguage, shouldReduceMotion, reportOutcome) =>
                ListenOrderRenderer.Render(
                    cache,
                    speechSynthesisProvider,
                    parameters,
                    instructionLanguage,
                    shouldReduceMotion,
                    reportOutcome)),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("listen-type"),
            (cache, parameters, instructionLanguage, shouldReduceMotion, reportOutcome) =>
                ListenTypeRenderer.Render(
                    cache,
                    speechSynthesisProvider,
                    parameters,
                    instructionLanguage,
                    shouldReduceMotion,
                    reportOutcome)),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("minimal-pair-doors"),
            (cache, parameters, instructionLanguage, shouldReduceMotion, reportOutcome) =>
                MinimalPairDoorsRenderer.Render(
                    cache,
                    speechSynthesisProvider,
                    parameters,
                    instructionLanguage,
                    shouldReduceMotion,
                    reportOutcome)),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("listen-route"),
            (cache, parameters, instructionLanguage, shouldReduceMotion, reportOutcome) =>
                ListenRouteRenderer.Render(
                    cache,
                    speechSynthesisProvider,
                    parameters,
                    instructionLanguage,
                    shouldReduceMotion,
                    reportOutcome)),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("listen-price-tag"),
            (cache, parameters, instructionLanguage, shouldReduceMotion, reportOutcome) =>
                ListenPriceTagRenderer.Render(
                    cache,
                    speechSynthesisProvider,
                    parameters,
                    instructionLanguage,
                    shouldReduceMotion,
                    reportOutcome)),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("dialogue-eavesdrop"),
            (cache, parameters, instructionLanguage, shouldReduceMotion, reportOutcome) =>
                DialogueEavesdropRenderer.Render(
                    cache,
                    speechSynthesisProvider,
                    parameters,
                    instructionLanguage,
                    shouldReduceMotion,
                    reportOutcome)),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("echo-stage"),
            (cache, parameters, instructionLanguage, shouldReduceMotion, reportOutcome) =>
                EchoStageRenderer.Render(
                    cache,
                    speechSynthesisProvider,
                    speechRecognitionProvider,
                    pronunciationAssessmentProvider,
                    microphoneAllowed,
                    parameters,
                    instructionLanguage,
                    shouldReduceMotion,
                    reportOutcome)),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("read-aloud-card"),
            (cache, parameters, instructionLanguage, shouldReduceMotion, reportOutcome) =>
                ReadAloudCardRenderer.Render(
                    cache,
                    speechRecognitionProvider,
                    pronunciationAssessmentProvider,
                    microphoneAllowed,
                    parameters,
                    instructionLanguage,
                    shouldReduceMotion,
                    reportOutcome)),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("prompt-respond"),
            (cache, parameters, instructionLanguage, shouldReduceMotion, reportOutcome) =>
                PromptRespondRenderer.Render(
                    cache,
                    speechSynthesisProvider,
                    speechRecognitionProvider,
                    pronunciationAssessmentProvider,
                    microphoneAllowed,
                    parameters,
                    instructionLanguage,
                    shouldReduceMotion,
                    reportOutcome)),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("syllable-clap"),
            (cache, parameters, instructionLanguage, shouldReduceMotion, reportOutcome) =>
                SyllableClapRenderer.Render(
                    cache,
                    speechSynthesisProvider,
                    parameters,
                    instructionLanguage,
                    shouldReduceMotion,
                    reportOutcome)),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("long-short-vowel"),
            (cache, parameters, instructionLanguage, shouldReduceMotion, reportOutcome) =>
                LongShortVowelRenderer.Render(
                    cache,
                    speechSynthesisProvider,
                    parameters,
                    instructionLanguage,
                    shouldReduceMotion,
                    reportOutcome)),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("sign-reading"),
            SignReadingRenderer.Render),
    ]);

    public Control Render(
        TemplateId templateId,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(reportOutcome);

        if (!_renderers.TryGetValue(templateId, out var renderer))
        {
            throw new KeyNotFoundException($"Template renderer '{templateId}' is not registered.");
        }

        return renderer(_imageCache, parameters, instructionLanguage, shouldReduceMotion, reportOutcome);
    }
}
