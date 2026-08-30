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
            ]),
        new LessonTemplateSchema(
            new TemplateId("picture-match"),
            1,
            [
                new("prompt", TemplateParameterKind.TextByLanguage, IsRequired: true),
                new("concept", TemplateParameterKind.ConceptReference, IsRequired: true),
                new("options", TemplateParameterKind.OptionList, IsRequired: true),
                new("answer", TemplateParameterKind.Text, IsRequired: true),
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
