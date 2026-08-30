using Linguistics.Core.Content;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Learn.Templates;

internal sealed record TemplateGalleryFixture(
    TemplateId TemplateId,
    string Title,
    string Family,
    LanguageCode InstructionLanguage,
    ResolvedTemplateParameters Parameters);

internal static class TemplateGalleryFixtures
{
    public static IReadOnlyList<TemplateGalleryFixture> All { get; } =
    [
        new(
            new TemplateId("object-spotlight"),
            "Object spotlight",
            "Scene · synthetic Hindi instruction route",
            new LanguageCode("hi"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["word"] = new(TemplateParameterKind.Text, Text: "Marktstand"),
                ["article"] = new(TemplateParameterKind.Text, Text: "der"),
                ["meaning"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "market stall",
                        ["hi"] = "बाज़ार का स्टॉल",
                    }),
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Notice the word, article, and meaning together.",
                        ["hi"] = "शब्द, लेख और अर्थ को एक साथ देखें।",
                    }),
                ["asset"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "preview.market-stall"),
            })),
        new(
            new TemplateId("picture-match"),
            "Picture match",
            "Recognition · synthetic local cutouts",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["prompt"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Which cutout shows the learner?",
                        ["hi"] = "कौन-सा कटआउट सीखने वाले व्यक्ति को दिखाता है?",
                    }),
                ["options"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("stall", "Market stall", "preview.market-stall"),
                        new("learner", "Learner", "preview.learner"),
                        new("square", "Market square", "preview.market-square"),
                    ]),
                ["answer"] = new(TemplateParameterKind.Text, Text: "learner"),
            })),
        new(
            new TemplateId("word-order-train"),
            "Word order train",
            "Construction · synthetic café request",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["prompt"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Build the café request from left to right.",
                        ["hi"] = "कैफ़े का अनुरोध बाएँ से दाएँ बनाएँ।",
                    }),
                ["options"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("ich", "Ich"),
                        new("moechte", "möchte"),
                        new("einen", "einen"),
                        new("kaffee", "Kaffee,"),
                        new("bitte", "bitte."),
                    ]),
            })),
    ];
}
