using System.Text.Json;
using System.Text.Json.Serialization;
using Linguistics.Core.Curriculum;

namespace Linguistics.Core.Content;

[JsonConverter(typeof(TemplateIdJsonConverter))]
public readonly record struct TemplateId
{
    [JsonConstructor]
    public TemplateId(string value) => Value = CurriculumIdentifier.Normalize(value, nameof(value));

    public string Value { get; }

    public override string ToString() => Value;
}

public enum TemplateParameterKind
{
    Text,
    TextByLanguage,
    ConceptReference,
    ExampleReference,
    AssetReference,
    TaskReference,
    OptionList,
}

public sealed record TemplateParameterDefinition(
    string Name,
    TemplateParameterKind Kind,
    bool IsRequired);

public sealed record LessonTemplateSchema(
    TemplateId Id,
    int Version,
    IReadOnlyList<TemplateParameterDefinition> Parameters);

public sealed record TemplateOption(
    string Id,
    string Label,
    string? AssetReferenceId = null);

public sealed record TemplateParameterValue(
    TemplateParameterKind Kind,
    string? Value = null,
    IReadOnlyDictionary<string, string>? TextByLanguage = null,
    IReadOnlyList<TemplateOption>? Options = null);

public sealed record TemplateInstance(
    string Id,
    TemplateId TemplateId,
    int TemplateVersion,
    IReadOnlyDictionary<string, TemplateParameterValue> Parameters);

public sealed record LessonTemplateContent(
    string Id,
    IReadOnlyList<TemplateInstance> TemplateInstances);

public sealed record ResolvedTemplateParameters(
    IReadOnlyDictionary<string, ResolvedTemplateParameter> Values,
    TemplateOutcomeState PreviewOutcome = TemplateOutcomeState.Ready,
    bool UseTextOnlyFallback = false);

public sealed record ResolvedTemplateParameter(
    TemplateParameterKind Kind,
    string? Text = null,
    IReadOnlyDictionary<string, string>? TextByLanguage = null,
    TargetConceptContent? Concept = null,
    ContentExample? Example = null,
    string? AssetReferenceId = null,
    TaskTemplateContent? Task = null,
    IReadOnlyList<TemplateOption>? Options = null);

public enum TemplateOutcomeState
{
    Ready,
    Success,
    Uncertain,
    Failure,
}

public sealed record TemplateOutcome(
    TemplateOutcomeState State,
    string? ResponseId = null,
    IReadOnlyList<string>? OrderedOptionIds = null);

public static class LessonTemplateSchemas
{
    public static IReadOnlyList<LessonTemplateSchema> All { get; } =
    [
        new LessonTemplateSchema(
            new TemplateId("scene-establish"),
            1,
            [
                new("location", TemplateParameterKind.Text, IsRequired: true),
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("cast", TemplateParameterKind.OptionList, IsRequired: true),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("object-spotlight"),
            1,
            [
                new("word", TemplateParameterKind.Text, IsRequired: true),
                new("article", TemplateParameterKind.Text, IsRequired: false),
                new("meaning", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("example", TemplateParameterKind.ExampleReference, IsRequired: true),
                new("asset", TemplateParameterKind.AssetReference, IsRequired: false),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("object-anatomy"),
            1,
            [
                new("title", TemplateParameterKind.Text, IsRequired: true),
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("parts", TemplateParameterKind.OptionList, IsRequired: true),
                new("asset", TemplateParameterKind.AssetReference, IsRequired: false),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("paper-dialogue"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("speaker-one", TemplateParameterKind.Text, IsRequired: true),
                new("line-one", TemplateParameterKind.Text, IsRequired: true),
                new("speaker-two", TemplateParameterKind.Text, IsRequired: true),
                new("line-two", TemplateParameterKind.Text, IsRequired: true),
                new("speech-language", TemplateParameterKind.Text, IsRequired: true),
                new("speaker-one-asset", TemplateParameterKind.AssetReference, IsRequired: false),
                new("speaker-two-asset", TemplateParameterKind.AssetReference, IsRequired: false),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("street-walk"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("subject", TemplateParameterKind.Text, IsRequired: true),
                new("route", TemplateParameterKind.OptionList, IsRequired: true),
                new("subject-asset", TemplateParameterKind.AssetReference, IsRequired: false),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("postcard-story"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("front-title", TemplateParameterKind.Text, IsRequired: true),
                new("front-caption", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("back-title", TemplateParameterKind.Text, IsRequired: true),
                new("back-body", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("front-asset", TemplateParameterKind.AssetReference, IsRequired: false),
                new("back-asset", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("photo-album"),
            1,
            [
                new("title", TemplateParameterKind.Text, IsRequired: true),
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("pages", TemplateParameterKind.OptionList, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("culture-plate"),
            1,
            [
                new("title", TemplateParameterKind.Text, IsRequired: true),
                new("caption", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("source-note", TemplateParameterKind.Text, IsRequired: true),
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("asset", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("weather-window"),
            1,
            [
                new("weather", TemplateParameterKind.Text, IsRequired: true),
                new("season", TemplateParameterKind.Text, IsRequired: true),
                new("effect", TemplateParameterKind.Text, IsRequired: true),
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("clock-theatre"),
            1,
            [
                new("time", TemplateParameterKind.Text, IsRequired: true),
                new("hour", TemplateParameterKind.Text, IsRequired: true),
                new("minute", TemplateParameterKind.Text, IsRequired: true),
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("picture-match"),
            1,
            [
                new("prompt", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("options", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
                new("spoken-text", TemplateParameterKind.Text, IsRequired: true),
                new("speech-language", TemplateParameterKind.Text, IsRequired: true),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("word-match"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("subject-description", TemplateParameterKind.Text, IsRequired: true),
                new("options", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
                new("asset", TemplateParameterKind.AssetReference, IsRequired: false),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("pair-cards"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("pairs", TemplateParameterKind.OptionList, IsRequired: true),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("odd-one-out"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("category-label", TemplateParameterKind.Text, IsRequired: true),
                new("options", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("sort-into-baskets"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("items", TemplateParameterKind.OptionList, IsRequired: true),
                new("baskets", TemplateParameterKind.OptionList, IsRequired: true),
                new("answers", TemplateParameterKind.OptionList, IsRequired: true),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("article-stamp"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("noun", TemplateParameterKind.Text, IsRequired: true),
                new("options", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
                new("asset", TemplateParameterKind.AssetReference, IsRequired: false),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("plural-fold"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("example", TemplateParameterKind.ExampleReference, IsRequired: true),
                new("singular", TemplateParameterKind.Text, IsRequired: true),
                new("plural", TemplateParameterKind.Text, IsRequired: true),
                new("asset", TemplateParameterKind.AssetReference, IsRequired: false),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("color-swatch"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("object-name", TemplateParameterKind.Text, IsRequired: true),
                new("options", TemplateParameterKind.OptionList, IsRequired: true),
                new("swatch-colors", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
                new("asset", TemplateParameterKind.AssetReference, IsRequired: false),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("number-tiles"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("quantity-description", TemplateParameterKind.Text, IsRequired: true),
                new("pieces", TemplateParameterKind.OptionList, IsRequired: true),
                new("options", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
                new("asset", TemplateParameterKind.AssetReference, IsRequired: false),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("label-the-scene"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("target-label", TemplateParameterKind.Text, IsRequired: true),
                new("hotspots", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("word-order-train"),
            1,
            [
                new("prompt", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("example", TemplateParameterKind.ExampleReference, IsRequired: true),
                new("options", TemplateParameterKind.OptionList, IsRequired: true),
                new("task", TemplateParameterKind.TaskReference, IsRequired: false),
                new("asset", TemplateParameterKind.AssetReference, IsRequired: false),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("gap-card"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("example", TemplateParameterKind.ExampleReference, IsRequired: true),
                new("sentence-before", TemplateParameterKind.Text, IsRequired: true),
                new("sentence-after", TemplateParameterKind.Text, IsRequired: true),
                new("options", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("sentence-fold"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("example", TemplateParameterKind.ExampleReference, IsRequired: true),
                new("segments", TemplateParameterKind.OptionList, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("conjugation-wheel"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("example", TemplateParameterKind.ExampleReference, IsRequired: true),
                new("lemma", TemplateParameterKind.Text, IsRequired: true),
                new("persons", TemplateParameterKind.OptionList, IsRequired: true),
                new("forms", TemplateParameterKind.OptionList, IsRequired: true),
                new("answers", TemplateParameterKind.OptionList, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("case-switchboard"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("example", TemplateParameterKind.ExampleReference, IsRequired: true),
                new("noun", TemplateParameterKind.Text, IsRequired: true),
                new("roles", TemplateParameterKind.OptionList, IsRequired: true),
                new("articles", TemplateParameterKind.OptionList, IsRequired: true),
                new("answers", TemplateParameterKind.OptionList, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("separable-verb-split"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("example", TemplateParameterKind.ExampleReference, IsRequired: true),
                new("joined-form", TemplateParameterKind.Text, IsRequired: true),
                new("sentence-start", TemplateParameterKind.Text, IsRequired: true),
                new("prefix", TemplateParameterKind.Text, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("question-flip"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("example", TemplateParameterKind.ExampleReference, IsRequired: true),
                new("statement", TemplateParameterKind.Text, IsRequired: true),
                new("question", TemplateParameterKind.Text, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("negation-strike"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("example", TemplateParameterKind.ExampleReference, IsRequired: true),
                new("sentence-start", TemplateParameterKind.Text, IsRequired: true),
                new("object", TemplateParameterKind.Text, IsRequired: true),
                new("sentence-end", TemplateParameterKind.Text, IsRequired: true),
                new("negators", TemplateParameterKind.OptionList, IsRequired: true),
                new("slots", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer-negator", TemplateParameterKind.Text, IsRequired: true),
                new("answer-slot", TemplateParameterKind.Text, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("preposition-stage"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("example", TemplateParameterKind.ExampleReference, IsRequired: true),
                new("object-label", TemplateParameterKind.Text, IsRequired: true),
                new("reference-label", TemplateParameterKind.Text, IsRequired: true),
                new("positions", TemplateParameterKind.OptionList, IsRequired: true),
                new("phrases", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
                new("asset", TemplateParameterKind.AssetReference, IsRequired: false),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("sentence-expand"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("example", TemplateParameterKind.ExampleReference, IsRequired: true),
                new("base", TemplateParameterKind.Text, IsRequired: true),
                new("complements", TemplateParameterKind.OptionList, IsRequired: true),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("listen-pick-image"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("utterance", TemplateParameterKind.Text, IsRequired: true),
                new("speech-language", TemplateParameterKind.Text, IsRequired: true),
                new("options", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("listen-order"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("utterance", TemplateParameterKind.Text, IsRequired: true),
                new("speech-language", TemplateParameterKind.Text, IsRequired: true),
                new("events", TemplateParameterKind.OptionList, IsRequired: true),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("listen-type"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("utterance", TemplateParameterKind.Text, IsRequired: true),
                new("speech-language", TemplateParameterKind.Text, IsRequired: true),
                new("accepted-answers", TemplateParameterKind.OptionList, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("minimal-pair-doors"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("utterance", TemplateParameterKind.Text, IsRequired: true),
                new("speech-language", TemplateParameterKind.Text, IsRequired: true),
                new("options", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("listen-route"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("utterance", TemplateParameterKind.Text, IsRequired: true),
                new("speech-language", TemplateParameterKind.Text, IsRequired: true),
                new("route", TemplateParameterKind.OptionList, IsRequired: true),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("listen-price-tag"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("utterance", TemplateParameterKind.Text, IsRequired: true),
                new("speech-language", TemplateParameterKind.Text, IsRequired: true),
                new("options", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("dialogue-eavesdrop"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("speaker-one", TemplateParameterKind.Text, IsRequired: true),
                new("line-one", TemplateParameterKind.Text, IsRequired: true),
                new("speaker-two", TemplateParameterKind.Text, IsRequired: true),
                new("line-two", TemplateParameterKind.Text, IsRequired: true),
                new("speech-language", TemplateParameterKind.Text, IsRequired: true),
                new("question", TemplateParameterKind.Text, IsRequired: true),
                new("options", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("echo-stage"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("phrase", TemplateParameterKind.Text, IsRequired: true),
                new("speech-language", TemplateParameterKind.Text, IsRequired: true),
                new("accepted-transcripts", TemplateParameterKind.OptionList, IsRequired: true),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("read-aloud-card"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("card-text", TemplateParameterKind.Text, IsRequired: true),
                new("speech-language", TemplateParameterKind.Text, IsRequired: true),
                new("accepted-transcripts", TemplateParameterKind.OptionList, IsRequired: true),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("prompt-respond"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("speaker", TemplateParameterKind.Text, IsRequired: true),
                new("prompt", TemplateParameterKind.Text, IsRequired: true),
                new("speech-language", TemplateParameterKind.Text, IsRequired: true),
                new("accepted-responses", TemplateParameterKind.OptionList, IsRequired: true),
                new("speaker-asset", TemplateParameterKind.AssetReference, IsRequired: false),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("syllable-clap"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("phrase", TemplateParameterKind.Text, IsRequired: true),
                new("speech-language", TemplateParameterKind.Text, IsRequired: true),
                new("beats", TemplateParameterKind.OptionList, IsRequired: true),
                new("stress-beat", TemplateParameterKind.Text, IsRequired: true),
                new("minimum-interval-ms", TemplateParameterKind.Text, IsRequired: true),
                new("maximum-interval-ms", TemplateParameterKind.Text, IsRequired: true),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("long-short-vowel"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("utterance", TemplateParameterKind.Text, IsRequired: true),
                new("speech-language", TemplateParameterKind.Text, IsRequired: true),
                new("contrast-label", TemplateParameterKind.Text, IsRequired: true),
                new("options", TemplateParameterKind.OptionList, IsRequired: true),
                new("long-option", TemplateParameterKind.Text, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("sign-reading"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("sign-text", TemplateParameterKind.Text, IsRequired: true),
                new("context", TemplateParameterKind.Text, IsRequired: true),
                new("question", TemplateParameterKind.Text, IsRequired: true),
                new("options", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
                new("sign-asset", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("form-fill"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("form-title", TemplateParameterKind.Text, IsRequired: true),
                new("prompt", TemplateParameterKind.Text, IsRequired: true),
                new("fields", TemplateParameterKind.OptionList, IsRequired: true),
                new("answers", TemplateParameterKind.OptionList, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("note-write"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("stationery-title", TemplateParameterKind.Text, IsRequired: true),
                new("prompt", TemplateParameterKind.Text, IsRequired: true),
                new("required-content", TemplateParameterKind.OptionList, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("menu-read"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("menu-title", TemplateParameterKind.Text, IsRequired: true),
                new("menu-items", TemplateParameterKind.OptionList, IsRequired: true),
                new("question", TemplateParameterKind.Text, IsRequired: true),
                new("options", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("schedule-read"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("schedule-title", TemplateParameterKind.Text, IsRequired: true),
                new("entries", TemplateParameterKind.OptionList, IsRequired: true),
                new("question", TemplateParameterKind.Text, IsRequired: true),
                new("options", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("spelling-tiles"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("word", TemplateParameterKind.Text, IsRequired: true),
                new("meaning", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("letters", TemplateParameterKind.OptionList, IsRequired: true),
                new("letter-names", TemplateParameterKind.OptionList, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("bridge-note"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("source-language", TemplateParameterKind.Text, IsRequired: true),
                new("note-type", TemplateParameterKind.Text, IsRequired: true),
                new("explanation", TemplateParameterKind.Text, IsRequired: true),
                new("risks", TemplateParameterKind.OptionList, IsRequired: false),
                new("preference-mode", TemplateParameterKind.Text, IsRequired: true),
                new("actions", TemplateParameterKind.OptionList, IsRequired: true),
                new("acknowledgement", TemplateParameterKind.Text, IsRequired: true),
                new("dismissal", TemplateParameterKind.Text, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("false-friend-alarm"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("source-language", TemplateParameterKind.Text, IsRequired: true),
                new("tempting-form", TemplateParameterKind.Text, IsRequired: true),
                new("target-form", TemplateParameterKind.Text, IsRequired: true),
                new("explanation", TemplateParameterKind.Text, IsRequired: true),
                new("risk", TemplateParameterKind.Text, IsRequired: true),
                new("actions", TemplateParameterKind.OptionList, IsRequired: true),
                new("acknowledgement", TemplateParameterKind.Text, IsRequired: true),
                new("dismissal", TemplateParameterKind.Text, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("cognate-thread"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("source-language", TemplateParameterKind.Text, IsRequired: true),
                new("target-language", TemplateParameterKind.Text, IsRequired: true),
                new("source-word", TemplateParameterKind.Text, IsRequired: true),
                new("target-word", TemplateParameterKind.Text, IsRequired: true),
                new("explanation", TemplateParameterKind.Text, IsRequired: true),
                new("actions", TemplateParameterKind.OptionList, IsRequired: true),
                new("acknowledgement", TemplateParameterKind.Text, IsRequired: true),
                new("dismissal", TemplateParameterKind.Text, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("contrast-panes"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("source-language", TemplateParameterKind.Text, IsRequired: true),
                new("target-language", TemplateParameterKind.Text, IsRequired: true),
                new("transfers", TemplateParameterKind.OptionList, IsRequired: true),
                new("changes", TemplateParameterKind.OptionList, IsRequired: true),
                new("risk", TemplateParameterKind.Text, IsRequired: true),
                new("actions", TemplateParameterKind.OptionList, IsRequired: true),
                new("acknowledgement", TemplateParameterKind.Text, IsRequired: true),
                new("dismissal", TemplateParameterKind.Text, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("scenario-theatre"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("task", TemplateParameterKind.TaskReference, IsRequired: true),
                new("state-label", TemplateParameterKind.Text, IsRequired: true),
                new("npc-line", TemplateParameterKind.Text, IsRequired: true),
                new("responses", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
                new("retry-hint", TemplateParameterKind.Text, IsRequired: true),
                new("npc-asset", TemplateParameterKind.AssetReference, IsRequired: false),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("consequence-verdict"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("subject", TemplateParameterKind.Text, IsRequired: true),
                new("state-label", TemplateParameterKind.Text, IsRequired: true),
                new("verdicts", TemplateParameterKind.OptionList, IsRequired: true),
                new("consequences", TemplateParameterKind.OptionList, IsRequired: true),
                new("report-lines", TemplateParameterKind.OptionList, IsRequired: true),
                new("actions", TemplateParameterKind.OptionList, IsRequired: true),
                new("retry-action", TemplateParameterKind.Text, IsRequired: true),
                new("subject-asset", TemplateParameterKind.AssetReference, IsRequired: false),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
        new LessonTemplateSchema(
            new TemplateId("review-flash"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("prompt", TemplateParameterKind.Text, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
                new("details", TemplateParameterKind.OptionList, IsRequired: true),
                new("ratings", TemplateParameterKind.OptionList, IsRequired: true),
                new("configuration-version", TemplateParameterKind.Text, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("recap-scrapbook"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("title", TemplateParameterKind.Text, IsRequired: true),
                new("pieces", TemplateParameterKind.OptionList, IsRequired: true),
                new("closing", TemplateParameterKind.Text, IsRequired: true),
                new("actions", TemplateParameterKind.OptionList, IsRequired: true),
                new("acknowledgement", TemplateParameterKind.Text, IsRequired: true),
            ]),
        new LessonTemplateSchema(
            new TemplateId("unit-capstone"),
            1,
            [
                new("instruction", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("unit-label", TemplateParameterKind.Text, IsRequired: true),
                new("goal", TemplateParameterKind.Text, IsRequired: true),
                new("steps", TemplateParameterKind.OptionList, IsRequired: true),
                new("template-chain", TemplateParameterKind.OptionList, IsRequired: true),
                new("backdrop", TemplateParameterKind.AssetReference, IsRequired: false),
            ]),
    ];
}

public sealed class TemplateIdJsonConverter : JsonConverter<TemplateId>
{
    public override TemplateId Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String || reader.GetString() is not { } value)
        {
            throw new JsonException("A template ID must be a string.");
        }

        try
        {
            return new TemplateId(value);
        }
        catch (ArgumentException exception)
        {
            throw new JsonException("The template ID is invalid.", exception);
        }
    }

    public override void Write(
        Utf8JsonWriter writer,
        TemplateId value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
