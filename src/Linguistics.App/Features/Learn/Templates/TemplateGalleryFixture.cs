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
            new TemplateId("scene-establish"),
            "Scene establish",
            "Scene and story · validated backdrop · authored text cast",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["location"] = new(TemplateParameterKind.Text, Text: "Marktplatz"),
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Meet the place and the people before the story begins.",
                        ["hi"] = "कहानी शुरू होने से पहले जगह और लोगों से मिलें।",
                    }),
                ["cast"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("vendor", "Marktfrau"),
                        new("visitor", "Besucher"),
                    ]),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("object-spotlight"),
            "Object spotlight",
            "Scene · validated pack image · Hindi instruction route",
            new LanguageCode("hi"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["word"] = new(TemplateParameterKind.Text, Text: "Kaffee"),
                ["article"] = new(TemplateParameterKind.Text, Text: "der"),
                ["meaning"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "coffee",
                        ["hi"] = "कॉफ़ी",
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
                    AssetReferenceId: "asset.de.cafe.coffee"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("object-anatomy"),
            "Object anatomy",
            "Scene and story · synthetic Preview labels · local photograph",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["title"] = new(TemplateParameterKind.Text, Text: "die Schere"),
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Notice how two named parts make the classroom object.",
                        ["hi"] = "ध्यान दें कि दो नामित हिस्से कक्षा की वस्तु बनाते हैं।",
                    }),
                ["parts"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("handle", "der Griff"),
                        new("blade", "die Klinge"),
                    ]),
                ["asset"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.classroom.scissors"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("paper-dialogue"),
            "Paper dialogue",
            "Scene and story · caption-complete · optional local TTS",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Watch the greeting pass from one speaker to the other.",
                        ["hi"] = "अभिवादन को एक वक्ता से दूसरे तक जाते हुए देखें।",
                    }),
                ["speaker-one"] = new(TemplateParameterKind.Text, Text: "Mina"),
                ["line-one"] = new(TemplateParameterKind.Text, Text: "Guten Tag!"),
                ["speaker-two"] = new(TemplateParameterKind.Text, Text: "Jonas"),
                ["line-two"] = new(TemplateParameterKind.Text, Text: "Hallo, Mina!"),
                ["speech-language"] = new(TemplateParameterKind.Text, Text: "de"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("street-walk"),
            "Street walk",
            "Scene and story · stepped route · native torn foreground",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Follow Mina past three labeled places on the short route.",
                        ["hi"] = "मीना के साथ छोटे रास्ते पर तीन नामित जगहों के पास जाएँ।",
                    }),
                ["subject"] = new(TemplateParameterKind.Text, Text: "Mina"),
                ["route"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("cafe", "Café"),
                        new("market", "Markt"),
                        new("station", "Bahnhof"),
                    ]),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("postcard-story"),
            "Postcard story",
            "Scene and story · two-sided card · local photographs",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Turn the postcard to read the short note on its back.",
                        ["hi"] = "पीछे का छोटा नोट पढ़ने के लिए पोस्टकार्ड पलटें।",
                    }),
                ["front-title"] = new(TemplateParameterKind.Text, Text: "Grüße vom Markt"),
                ["front-caption"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "A friendly greeting at the market.",
                        ["hi"] = "बाज़ार में एक दोस्ताना अभिवादन।",
                    }),
                ["back-title"] = new(TemplateParameterKind.Text, Text: "Hallo aus Berlin"),
                ["back-body"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Mina sends a short greeting after visiting the market.",
                        ["hi"] = "मीना बाज़ार जाने के बाद एक छोटा अभिवादन भेजती है।",
                    }),
                ["front-asset"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.greetings.handshake"),
                ["back-asset"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("photo-album"),
            "Photo album",
            "Scene and story · captioned local photo set · keyboard paging",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["title"] = new(TemplateParameterKind.Text, Text: "Im Café"),
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Turn through three café items and read each complete caption.",
                        ["hi"] = "कैफ़े की तीन वस्तुओं के पन्ने पलटें और हर कैप्शन पढ़ें।",
                    }),
                ["pages"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("coffee", "der Kaffee", "asset.de.cafe.coffee"),
                        new("tea", "der Tee", "asset.de.cafe.tea"),
                        new("water", "das Wasser", "asset.de.cafe.water"),
                    ]),
            })),
        new(
            new TemplateId("culture-plate"),
            "Culture plate",
            "Scene and story · authored text-only equivalent · asset follow-up",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["title"] = new(TemplateParameterKind.Text, Text: "Begrüßung"),
                ["caption"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "A synthetic Preview plate tests the cultural-note layout without claiming reviewed cultural content.",
                        ["hi"] = "यह सिंथेटिक प्रीव्यू बिना समीक्षित सांस्कृतिक दावा किए सांस्कृतिक नोट का लेआउट जाँचता है।",
                    }),
                ["source-note"] = new(
                    TemplateParameterKind.Text,
                    Text: "No suitable Commons artifact is bundled yet."),
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Read the caption and the explicit source state together.",
                        ["hi"] = "कैप्शन और स्पष्ट स्रोत स्थिति को साथ पढ़ें।",
                    }),
            })),
        new(
            new TemplateId("weather-window"),
            "Weather window",
            "Scene and story · native paper rain · validated backdrop",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["weather"] = new(TemplateParameterKind.Text, Text: "Regen"),
                ["season"] = new(TemplateParameterKind.Text, Text: "Frühling"),
                ["effect"] = new(TemplateParameterKind.Text, Text: "rain"),
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Look through the window and connect weather with the season.",
                        ["hi"] = "खिड़की से देखें और मौसम को ऋतु से जोड़ें।",
                    }),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("clock-theatre"),
            "Clock theatre",
            "Scene and story · native paper clock · authored time",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["time"] = new(TemplateParameterKind.Text, Text: "zehn Uhr dreißig"),
                ["hour"] = new(TemplateParameterKind.Text, Text: "10"),
                ["minute"] = new(TemplateParameterKind.Text, Text: "30"),
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Watch the paper hands settle on the authored time.",
                        ["hi"] = "काग़ज़ की सुइयों को लिखे हुए समय पर टिकते देखें।",
                    }),
            })),
        new(
            new TemplateId("picture-match"),
            "Picture match",
            "Recognition · validated café photographs",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["prompt"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Which picture shows Kaffee?",
                        ["hi"] = "कौन-सी तस्वीर Kaffee दिखाती है?",
                    }),
                ["options"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("kaffee", "Kaffee", "asset.de.cafe.coffee"),
                        new("tee", "Tee", "asset.de.cafe.tea"),
                        new("wasser", "Wasser", "asset.de.cafe.water"),
                    ]),
                ["answer"] = new(TemplateParameterKind.Text, Text: "kaffee"),
                ["spoken-text"] = new(TemplateParameterKind.Text, Text: "Kaffee"),
                ["speech-language"] = new(TemplateParameterKind.Text, Text: "de"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("word-match"),
            "Word match",
            "Recognition · validated cutout · complete text equivalent",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Choose the German word that names the cutout.",
                        ["hi"] = "कटआउट का जर्मन नाम चुनें।",
                    }),
                ["subject-description"] = new(
                    TemplateParameterKind.Text,
                    Text: "A white cup filled with coffee"),
                ["options"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("kaffee", "Kaffee"),
                        new("tee", "Tee"),
                        new("wasser", "Wasser"),
                    ]),
                ["answer"] = new(TemplateParameterKind.Text, Text: "kaffee"),
                ["asset"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.cafe.coffee"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("pair-cards"),
            "Pair cards",
            "Recognition · keyboard card flips · validated café set",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Reveal two cards and match a German word with its picture.",
                        ["hi"] = "दो कार्ड खोलें और जर्मन शब्द को उसकी तस्वीर से मिलाएँ।",
                    }),
                ["pairs"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("kaffee", "Kaffee", "asset.de.cafe.coffee"),
                        new("tee", "Tee", "asset.de.cafe.tea"),
                        new("wasser", "Wasser", "asset.de.cafe.water"),
                    ]),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("odd-one-out"),
            "Odd one out",
            "Recognition · four validated cutouts · authored category",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Choose the cutout that does not belong with the café drinks.",
                        ["hi"] = "वह कटआउट चुनें जो कैफ़े पेयों के साथ नहीं आता।",
                    }),
                ["category-label"] = new(TemplateParameterKind.Text, Text: "Café drinks"),
                ["options"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("kaffee", "Kaffee", "asset.de.cafe.coffee"),
                        new("tee", "Tee", "asset.de.cafe.tea"),
                        new("wasser", "Wasser", "asset.de.cafe.water"),
                        new("schere", "Schere", "asset.de.classroom.scissors"),
                    ]),
                ["answer"] = new(TemplateParameterKind.Text, Text: "schere"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("sort-into-baskets"),
            "Sort into baskets",
            "Recognition · drag or keyboard assignment · two authored groups",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Drag each item, or select it and choose its labeled basket.",
                        ["hi"] = "हर वस्तु खींचें, या उसे चुनकर सही नाम वाली टोकरी चुनें।",
                    }),
                ["items"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("kaffee", "Kaffee", "asset.de.cafe.coffee"),
                        new("tee", "Tee", "asset.de.cafe.tea"),
                        new("schere", "Schere", "asset.de.classroom.scissors"),
                        new("stifte", "Stifte", "asset.de.classroom.colouring-pencils"),
                    ]),
                ["baskets"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("cafe", "Im Café"),
                        new("classroom", "Im Klassenzimmer"),
                    ]),
                ["answers"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("kaffee", "cafe"),
                        new("tee", "cafe"),
                        new("schere", "classroom"),
                        new("stifte", "classroom"),
                    ]),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("article-stamp"),
            "Article stamp",
            "Recognition · native paper stamps · authored noun gender",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Stamp the authored article beside the German noun.",
                        ["hi"] = "जर्मन संज्ञा के पास सही लिखा हुआ लेख लगाएँ।",
                    }),
                ["noun"] = new(TemplateParameterKind.Text, Text: "Kaffee"),
                ["options"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("der", "der"),
                        new("die", "die"),
                        new("das", "das"),
                    ]),
                ["answer"] = new(TemplateParameterKind.Text, Text: "der"),
                ["asset"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.cafe.coffee"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("plural-fold"),
            "Plural fold",
            "Recognition · two-sided paper fold · authored word forms",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Unfold the card to compare the authored singular and plural.",
                        ["hi"] = "लिखे हुए एकवचन और बहुवचन की तुलना के लिए कार्ड खोलें।",
                    }),
                ["singular"] = new(TemplateParameterKind.Text, Text: "der Stift"),
                ["plural"] = new(TemplateParameterKind.Text, Text: "die Stifte"),
                ["asset"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.classroom.colouring-pencils"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("color-swatch"),
            "Color swatch",
            "Recognition · authored pigment chips · validated classroom object",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Choose blau and apply its paper pigment to the object card.",
                        ["hi"] = "blau चुनें और उसके काग़ज़ी रंग को वस्तु कार्ड पर लगाएँ।",
                    }),
                ["object-name"] = new(TemplateParameterKind.Text, Text: "der Stift"),
                ["options"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("blau", "blau"),
                        new("rot", "rot"),
                        new("gruen", "grün"),
                    ]),
                ["swatch-colors"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("blau", "#6F8FAF"),
                        new("rot", "#A95F52"),
                        new("gruen", "#6F8A70"),
                    ]),
                ["answer"] = new(TemplateParameterKind.Text, Text: "blau"),
                ["asset"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.classroom.colouring-pencils"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("number-tiles"),
            "Number tiles",
            "Recognition · authored quantity scene · validated counting image",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Count the paper blocks and choose the matching digit tile.",
                        ["hi"] = "काग़ज़ी ब्लॉक गिनें और मिलती हुई अंक टाइल चुनें।",
                    }),
                ["quantity-description"] = new(
                    TemplateParameterKind.Text,
                    Text: "Four separate counting blocks"),
                ["pieces"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("block-1", "block one"),
                        new("block-2", "block two"),
                        new("block-3", "block three"),
                        new("block-4", "block four"),
                    ]),
                ["options"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("3", "3"),
                        new("4", "4"),
                        new("5", "5"),
                    ]),
                ["answer"] = new(TemplateParameterKind.Text, Text: "4"),
                ["asset"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.numbers.counting-blocks"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("label-the-scene"),
            "Label the scene",
            "Recognition · busy market stage · four tabbable hotspots",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Find der Kaffee in the busy scene and reveal its label.",
                        ["hi"] = "व्यस्त दृश्य में der Kaffee खोजें और उसका लेबल खोलें।",
                    }),
                ["target-label"] = new(TemplateParameterKind.Text, Text: "der Kaffee"),
                ["hotspots"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("kaffee", "der Kaffee", "asset.de.cafe.coffee"),
                        new("tee", "der Tee", "asset.de.cafe.tea"),
                        new("wasser", "das Wasser", "asset.de.cafe.water"),
                        new("schere", "die Schere", "asset.de.classroom.scissors"),
                    ]),
                ["answer"] = new(TemplateParameterKind.Text, Text: "kaffee"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
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
                ["asset"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.cafe.coffee"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("gap-card"),
            "Gap card",
            "Construction · synthetic Preview cloze · drag or keyboard",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Complete the authored café request with one word tile.",
                        ["hi"] = "लिखे हुए कैफ़े अनुरोध को एक शब्द टाइल से पूरा करें।",
                    }),
                ["sentence-before"] = new(TemplateParameterKind.Text, Text: "Ich"),
                ["sentence-after"] = new(TemplateParameterKind.Text, Text: "einen Kaffee, bitte."),
                ["options"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("moechte", "möchte"),
                        new("trinke", "trinke"),
                        new("sehe", "sehe"),
                    ]),
                ["answer"] = new(TemplateParameterKind.Text, Text: "moechte"),
            })),
        new(
            new TemplateId("sentence-fold"),
            "Sentence fold",
            "Construction · synthetic Preview accordion · authored sequence",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Unfold the authored café sentence one section at a time.",
                        ["hi"] = "लिखे हुए कैफ़े वाक्य को एक बार में एक भाग खोलें।",
                    }),
                ["segments"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("subject", "Ich"),
                        new("verb", "möchte"),
                        new("object", "einen Kaffee"),
                        new("courtesy", "bitte."),
                    ]),
            })),
        new(
            new TemplateId("conjugation-wheel"),
            "Conjugation wheel",
            "Construction · synthetic Preview forms · two keyboard wheels",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Rotate both paper wheels to align a person with its authored form.",
                        ["hi"] = "व्यक्ति को उसके लिखे हुए रूप से मिलाने के लिए दोनों पहिए घुमाएँ।",
                    }),
                ["lemma"] = new(TemplateParameterKind.Text, Text: "gehen"),
                ["persons"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("ich", "ich"),
                        new("du", "du"),
                        new("er", "er"),
                    ]),
                ["forms"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("gehe", "gehe"),
                        new("gehst", "gehst"),
                        new("geht", "geht"),
                    ]),
                ["answers"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("ich", "gehe"),
                        new("du", "gehst"),
                        new("er", "geht"),
                    ]),
            })),
        new(
            new TemplateId("case-switchboard"),
            "Case switchboard",
            "Construction · synthetic Preview roles · flipping article card",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Choose a sentence role, then flip the article card to its authored form.",
                        ["hi"] = "वाक्य की भूमिका चुनें, फिर लेख कार्ड को लिखे हुए रूप में पलटें।",
                    }),
                ["noun"] = new(TemplateParameterKind.Text, Text: "Kaffee"),
                ["roles"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("subject", "Subject"),
                        new("direct-object", "Direct object"),
                    ]),
                ["articles"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("der", "der"),
                        new("den", "den"),
                    ]),
                ["answers"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("subject", "der"),
                        new("direct-object", "den"),
                    ]),
            })),
        new(
            new TemplateId("separable-verb-split"),
            "Separable verb split",
            "Construction · synthetic Preview clause · paper prefix motion",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Split the authored verb and move its prefix to the clause end.",
                        ["hi"] = "लिखी हुई क्रिया को अलग करें और उपसर्ग को वाक्य के अंत में ले जाएँ।",
                    }),
                ["joined-form"] = new(TemplateParameterKind.Text, Text: "aufstehen"),
                ["sentence-start"] = new(TemplateParameterKind.Text, Text: "Ich stehe"),
                ["prefix"] = new(TemplateParameterKind.Text, Text: "auf."),
            })),
        new(
            new TemplateId("question-flip"),
            "Question flip",
            "Construction · synthetic Preview sentence pair · two-sided card",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Flip the authored statement card to inspect its question form.",
                        ["hi"] = "लिखे हुए कथन कार्ड को पलटकर उसका प्रश्न रूप देखें।",
                    }),
                ["statement"] = new(TemplateParameterKind.Text, Text: "Du trinkst Kaffee."),
                ["question"] = new(TemplateParameterKind.Text, Text: "Trinkst du Kaffee?"),
            })),
        new(
            new TemplateId("negation-strike"),
            "Negation strike",
            "Construction · synthetic Preview placement · native paper wobble",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Choose a negator and place it in the authored sentence slot.",
                        ["hi"] = "निषेध शब्द चुनें और उसे लिखे हुए वाक्य स्थान में रखें।",
                    }),
                ["sentence-start"] = new(TemplateParameterKind.Text, Text: "Ich trinke"),
                ["object"] = new(TemplateParameterKind.Text, Text: "Wasser"),
                ["sentence-end"] = new(TemplateParameterKind.Text, Text: "."),
                ["negators"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("nicht", "nicht"),
                        new("kein", "kein"),
                    ]),
                ["slots"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("before-object", "Before the object"),
                        new("after-object", "After the object"),
                    ]),
                ["answer-negator"] = new(TemplateParameterKind.Text, Text: "kein"),
                ["answer-slot"] = new(TemplateParameterKind.Text, Text: "before-object"),
            })),
        new(
            new TemplateId("preposition-stage"),
            "Preposition stage",
            "Construction · validated cutout · drag or keyboard positions",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Move der Kaffee to auf and read the resulting authored phrase.",
                        ["hi"] = "der Kaffee को auf पर ले जाएँ और बना हुआ लिखा वाक्यांश पढ़ें।",
                    }),
                ["object-label"] = new(TemplateParameterKind.Text, Text: "der Kaffee"),
                ["reference-label"] = new(TemplateParameterKind.Text, Text: "dem Tisch"),
                ["positions"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("auf", "auf"),
                        new("unter", "unter"),
                        new("neben", "neben"),
                    ]),
                ["phrases"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("auf", "der Kaffee auf dem Tisch"),
                        new("unter", "der Kaffee unter dem Tisch"),
                        new("neben", "der Kaffee neben dem Tisch"),
                    ]),
                ["answer"] = new(TemplateParameterKind.Text, Text: "auf"),
                ["asset"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.cafe.coffee"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("sentence-expand"),
            "Sentence expand",
            "Construction · synthetic Preview sentence · optional local cutouts",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Add the authored complements to grow the café sentence in order.",
                        ["hi"] = "कैफ़े वाक्य को क्रम में बढ़ाने के लिए लिखे हुए पूरक जोड़ें।",
                    }),
                ["base"] = new(TemplateParameterKind.Text, Text: "Ich bestelle"),
                ["complements"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("object", "einen Kaffee", "asset.de.cafe.coffee"),
                        new("place", "im Café"),
                        new("time", "am Morgen."),
                    ]),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("listen-pick-image"),
            "Listen and pick an image",
            "Listening · optional local TTS · written prompt fallback",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Play the café request, then choose the matching cutout.",
                        ["hi"] = "कैफ़े का अनुरोध चलाएँ, फिर उससे मिलता कटआउट चुनें।",
                    }),
                ["utterance"] = new(
                    TemplateParameterKind.Text,
                    Text: "Ich möchte einen Tee, bitte."),
                ["speech-language"] = new(TemplateParameterKind.Text, Text: "de"),
                ["options"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("coffee", "der Kaffee", "asset.de.cafe.coffee"),
                        new("tea", "der Tee", "asset.de.cafe.tea"),
                        new("water", "das Wasser", "asset.de.cafe.water"),
                    ]),
                ["answer"] = new(TemplateParameterKind.Text, Text: "tea"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("listen-order"),
            "Listen and order",
            "Listening · authored event sequence · optional local TTS",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Play the three-item sequence, then order its event cards.",
                        ["hi"] = "तीन वस्तुओं का क्रम चलाएँ, फिर घटना कार्ड सही क्रम में रखें।",
                    }),
                ["utterance"] = new(
                    TemplateParameterKind.Text,
                    Text: "Zuerst Tee, dann Wasser, zuletzt Kaffee."),
                ["speech-language"] = new(TemplateParameterKind.Text, Text: "de"),
                ["events"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("tea", "Tee", "asset.de.cafe.tea"),
                        new("water", "Wasser", "asset.de.cafe.water"),
                        new("coffee", "Kaffee", "asset.de.cafe.coffee"),
                    ]),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("listen-type"),
            "Listen and type",
            "Listening · local dictation · bounded core tolerance",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Play the café sentence, then type its complete wording.",
                        ["hi"] = "कैफ़े का वाक्य चलाएँ, फिर उसका पूरा पाठ लिखें।",
                    }),
                ["utterance"] = new(
                    TemplateParameterKind.Text,
                    Text: "Ich möchte einen Tee, bitte."),
                ["speech-language"] = new(TemplateParameterKind.Text, Text: "de"),
                ["accepted-answers"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("full", "Ich möchte einen Tee, bitte."),
                    ]),
            })),
        new(
            new TemplateId("minimal-pair-doors"),
            "Minimal pair doors",
            "Listening · two authored sound doors · no microphone",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Play the sound, then open the door with its matching word.",
                        ["hi"] = "ध्वनि चलाएँ, फिर उससे मिलता शब्द वाला दरवाज़ा खोलें।",
                    }),
                ["utterance"] = new(TemplateParameterKind.Text, Text: "ich"),
                ["speech-language"] = new(TemplateParameterKind.Text, Text: "de"),
                ["options"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("ich", "ich"),
                        new("ach", "ach"),
                    ]),
                ["answer"] = new(TemplateParameterKind.Text, Text: "ich"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("listen-route"),
            "Listen and route",
            "Listening · authored map route · deterministic sequence",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Play the directions, then build the route across the paper map.",
                        ["hi"] = "दिशाएँ चलाएँ, फिर कागज़ी नक्शे पर रास्ता बनाएँ।",
                    }),
                ["utterance"] = new(
                    TemplateParameterKind.Text,
                    Text: "Gehe zuerst zum Café, dann zum Markt, zuletzt zum Bahnhof."),
                ["speech-language"] = new(TemplateParameterKind.Text, Text: "de"),
                ["route"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("cafe", "Café"),
                        new("market", "Markt"),
                        new("station", "Bahnhof"),
                    ]),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("listen-price-tag"),
            "Listen and set a price tag",
            "Listening · number discrimination · optional local TTS",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Play the market price, then set the matching paper tag.",
                        ["hi"] = "बाज़ार का दाम चलाएँ, फिर उससे मिलता कागज़ी टैग चुनें।",
                    }),
                ["utterance"] = new(
                    TemplateParameterKind.Text,
                    Text: "Das kostet drei Euro fünfzig."),
                ["speech-language"] = new(TemplateParameterKind.Text, Text: "de"),
                ["options"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("three-fifteen", "€3,15"),
                        new("three-fifty", "€3,50"),
                        new("five-thirty", "€5,30"),
                    ]),
                ["answer"] = new(TemplateParameterKind.Text, Text: "three-fifty"),
            })),
        new(
            new TemplateId("dialogue-eavesdrop"),
            "Dialogue eavesdrop",
            "Listening · captioned exchange · deterministic comprehension",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Watch the café exchange, then answer the written question.",
                        ["hi"] = "कैफ़े की बातचीत देखें, फिर लिखे हुए प्रश्न का उत्तर दें।",
                    }),
                ["speaker-one"] = new(TemplateParameterKind.Text, Text: "Mina"),
                ["line-one"] = new(
                    TemplateParameterKind.Text,
                    Text: "Ich nehme den Tee."),
                ["speaker-two"] = new(TemplateParameterKind.Text, Text: "Max"),
                ["line-two"] = new(
                    TemplateParameterKind.Text,
                    Text: "Ich nehme den Kaffee."),
                ["speech-language"] = new(TemplateParameterKind.Text, Text: "de"),
                ["question"] = new(TemplateParameterKind.Text, Text: "Was nimmt Mina?"),
                ["options"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("tea", "Tee"),
                        new("coffee", "Kaffee"),
                        new("water", "Wasser"),
                    ]),
                ["answer"] = new(TemplateParameterKind.Text, Text: "tea"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("echo-stage"),
            "Echo stage",
            "Speaking · optional local recognition · complete typed route",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Listen to the café phrase, echo it, then compare through voice or text.",
                        ["hi"] = "कैफ़े का वाक्य सुनें, दोहराएँ, फिर आवाज़ या पाठ से तुलना करें।",
                    }),
                ["phrase"] = new(
                    TemplateParameterKind.Text,
                    Text: "Ich möchte einen Tee, bitte."),
                ["speech-language"] = new(TemplateParameterKind.Text, Text: "de"),
                ["accepted-transcripts"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("full", "Ich möchte einen Tee, bitte."),
                    ]),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("read-aloud-card"),
            "Read-aloud card",
            "Speaking · intelligibility only · complete typed route",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Read the café card aloud, or rehearse silently and compare its wording.",
                        ["hi"] = "कैफ़े कार्ड ज़ोर से पढ़ें, या चुपचाप अभ्यास करके शब्दों की तुलना करें।",
                    }),
                ["card-text"] = new(
                    TemplateParameterKind.Text,
                    Text: "Guten Morgen. Einen Kaffee, bitte."),
                ["speech-language"] = new(TemplateParameterKind.Text, Text: "de"),
                ["accepted-transcripts"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("full", "Guten Morgen. Einen Kaffee, bitte."),
                    ]),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("prompt-respond"),
            "Prompt and respond",
            "Speaking · authored puppet prompt · voice or typed response",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Hear or read Mina's question, then answer through local voice or text.",
                        ["hi"] = "मीना का प्रश्न सुनें या पढ़ें, फिर स्थानीय आवाज़ या पाठ से उत्तर दें।",
                    }),
                ["speaker"] = new(TemplateParameterKind.Text, Text: "Mina"),
                ["prompt"] = new(
                    TemplateParameterKind.Text,
                    Text: "Was möchtest du trinken?"),
                ["speech-language"] = new(TemplateParameterKind.Text, Text: "de"),
                ["accepted-responses"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("full", "Ich möchte einen Tee, bitte."),
                        new("short", "Einen Tee, bitte."),
                    ]),
                ["speaker-asset"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.learner"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("syllable-clap"),
            "Syllable clap",
            "Speaking · deterministic tap timing · no microphone",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Play or read Kaffee, then tap its two written syllables with the first beat strong.",
                        ["hi"] = "Kaffee को चलाएँ या पढ़ें, फिर पहले ताल को मजबूत रखते हुए दो अक्षरखंड थपथपाएँ।",
                    }),
                ["phrase"] = new(TemplateParameterKind.Text, Text: "Kaffee"),
                ["speech-language"] = new(TemplateParameterKind.Text, Text: "de"),
                ["beats"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("ka", "KA"),
                        new("ffee", "ffee"),
                    ]),
                ["stress-beat"] = new(TemplateParameterKind.Text, Text: "ka"),
                ["minimum-interval-ms"] = new(TemplateParameterKind.Text, Text: "180"),
                ["maximum-interval-ms"] = new(TemplateParameterKind.Text, Text: "900"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("long-short-vowel"),
            "Long and short vowel",
            "Speaking · length contrast choice · unscored production",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Play or read Staat, then choose its written length or practice it without a score.",
                        ["hi"] = "Staat को चलाएँ या पढ़ें, फिर लिखी लंबाई चुनें या बिना अंक के अभ्यास करें।",
                    }),
                ["utterance"] = new(TemplateParameterKind.Text, Text: "Staat"),
                ["speech-language"] = new(TemplateParameterKind.Text, Text: "de"),
                ["contrast-label"] = new(
                    TemplateParameterKind.Text,
                    Text: "Machine-validated Preview contrast: Stadt and Staat"),
                ["options"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("short", "kurz · Stadt"),
                        new("long", "lang · Staat"),
                    ]),
                ["long-option"] = new(TemplateParameterKind.Text, Text: "long"),
                ["answer"] = new(TemplateParameterKind.Text, Text: "long"),
                ["backdrop"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.market-backdrop"),
            })),
        new(
            new TemplateId("sign-reading"),
            "Sign reading",
            "Reading · authored sign text · photograph follow-up",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Read the complete entrance sign, then choose who may use the door.",
                        ["hi"] = "पूरा प्रवेश संकेत पढ़ें, फिर चुनें कि दरवाज़ा कौन इस्तेमाल कर सकता है।",
                    }),
                ["sign-text"] = new(
                    TemplateParameterKind.Text,
                    Text: "Eingang nur für Kunden"),
                ["context"] = new(
                    TemplateParameterKind.Text,
                    Text: "A shop entrance sign in a synthetic Preview reading task."),
                ["question"] = new(
                    TemplateParameterKind.Text,
                    Text: "Wer darf diesen Eingang benutzen?"),
                ["options"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("customers", "Nur Kunden"),
                        new("everyone", "Alle Personen"),
                        new("staff", "Nur Mitarbeitende"),
                    ]),
                ["answer"] = new(TemplateParameterKind.Text, Text: "customers"),
            })),
        new(
            new TemplateId("form-fill"),
            "Form fill",
            "Writing · synthetic paper form · deterministic field checks",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Copy the three synthetic details into their matching German form fields.",
                        ["hi"] = "तीन कृत्रिम विवरणों को उनके मिलते जर्मन फ़ॉर्म क्षेत्रों में लिखें।",
                    }),
                ["form-title"] = new(
                    TemplateParameterKind.Text,
                    Text: "Anmeldeformular"),
                ["prompt"] = new(
                    TemplateParameterKind.Text,
                    Text: "Name: Mina Weber. Herkunft: Berlin. Adresse: Marktstraße 5."),
                ["fields"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("name", "Name"),
                        new("origin", "Herkunft"),
                        new("address", "Adresse"),
                    ]),
                ["answers"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("name", "Mina Weber"),
                        new("origin", "Berlin"),
                        new("address", "Marktstraße 5"),
                    ]),
            })),
        new(
            new TemplateId("note-write"),
            "Note write",
            "Writing · paper stationery · deterministic content checks",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Write the short German note using both required details.",
                        ["hi"] = "दोनों आवश्यक विवरणों का उपयोग करके छोटा जर्मन नोट लिखें।",
                    }),
                ["stationery-title"] = new(
                    TemplateParameterKind.Text,
                    Text: "Für Sam"),
                ["prompt"] = new(
                    TemplateParameterKind.Text,
                    Text: "You are at the market and will return at six. Leave your host a note."),
                ["required-content"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("location", "auf dem Markt"),
                        new("return-time", "um sechs Uhr"),
                    ]),
            })),
        new(
            new TemplateId("menu-read"),
            "Menu read",
            "Reading · synthetic café menu · deterministic price extraction",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Read the synthetic menu, then choose the requested price.",
                        ["hi"] = "कृत्रिम मेनू पढ़ें, फिर पूछा गया मूल्य चुनें।",
                    }),
                ["menu-title"] = new(
                    TemplateParameterKind.Text,
                    Text: "Café Morgenrot"),
                ["menu-items"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("coffee", "Kaffee · 2,80 €"),
                        new("tea", "Kännchen Tee · 3,40 €"),
                        new("cake", "Apfelkuchen · 4,20 €"),
                    ]),
                ["question"] = new(
                    TemplateParameterKind.Text,
                    Text: "Was kostet ein Kännchen Tee?"),
                ["options"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("price-280", "2,80 €"),
                        new("price-340", "3,40 €"),
                        new("price-420", "4,20 €"),
                    ]),
                ["answer"] = new(
                    TemplateParameterKind.Text,
                    Text: "price-340"),
            })),
        new(
            new TemplateId("schedule-read"),
            "Schedule read",
            "Reading · synthetic opening hours · deterministic time extraction",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Read the synthetic opening hours, then choose the requested time.",
                        ["hi"] = "कृत्रिम खुलने का समय पढ़ें, फिर पूछा गया समय चुनें।",
                    }),
                ["schedule-title"] = new(
                    TemplateParameterKind.Text,
                    Text: "Stadtbibliothek Nord"),
                ["entries"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("monday", "Montag · 09:00 bis 18:00"),
                        new("tuesday", "Dienstag · 10:00 bis 19:00"),
                        new("wednesday", "Mittwoch · 11:00 bis 18:00"),
                    ]),
                ["question"] = new(
                    TemplateParameterKind.Text,
                    Text: "Wann öffnet die Bibliothek am Dienstag?"),
                ["options"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("time-0900", "09:00 Uhr"),
                        new("time-1000", "10:00 Uhr"),
                        new("time-1100", "11:00 Uhr"),
                    ]),
                ["answer"] = new(
                    TemplateParameterKind.Text,
                    Text: "time-1000"),
            })),
        new(
            new TemplateId("spelling-tiles"),
            "Spelling tiles",
            "Writing · paper letter tiles · deterministic authored order",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Spell the German word for apple from left to right.",
                        ["hi"] = "सेब के जर्मन शब्द की वर्तनी बाएँ से दाएँ बनाएँ।",
                    }),
                ["word"] = new(
                    TemplateParameterKind.Text,
                    Text: "APFEL"),
                ["meaning"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "apple",
                        ["hi"] = "सेब",
                    }),
                ["letters"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("letter-a", "A"),
                        new("letter-p", "P"),
                        new("letter-f", "F"),
                        new("letter-e", "E"),
                        new("letter-l", "L"),
                    ]),
                ["letter-names"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("letter-a", "a"),
                        new("letter-p", "pe"),
                        new("letter-f", "ef"),
                        new("letter-e", "e"),
                        new("letter-l", "el"),
                    ]),
            })),
        new(
            new TemplateId("bridge-note"),
            "Bridge note",
            "Transfer · routed Hindi note · advisory dismissal",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Read why the selected Hindi bridge appears, then use or dismiss it.",
                        ["hi"] = "चुना हुआ हिंदी सेतु क्यों दिखता है, इसे पढ़ें, फिर उपयोग करें या हटाएँ।",
                    }),
                ["source-language"] = new(
                    TemplateParameterKind.Text,
                    Text: "Hindi"),
                ["note-type"] = new(
                    TemplateParameterKind.Text,
                    Text: "partial bridge"),
                ["explanation"] = new(
                    TemplateParameterKind.Text,
                    Text: "Hindi experience with grammatical gender can make the idea familiar. German has three noun genders in this Preview, so learn each noun with its article."),
                ["risks"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("gender-caution", "Do not infer a German article from the Hindi translation."),
                    ]),
                ["preference-mode"] = new(
                    TemplateParameterKind.Text,
                    Text: "ask-first"),
                ["actions"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("use-bridge", "Use this bridge"),
                        new("dismiss-bridge", "Dismiss note"),
                    ]),
                ["acknowledgement"] = new(
                    TemplateParameterKind.Text,
                    Text: "use-bridge"),
                ["dismissal"] = new(
                    TemplateParameterKind.Text,
                    Text: "dismiss-bridge"),
            })),
        new(
            new TemplateId("false-friend-alarm"),
            "False friend alarm",
            "Transfer · routed interference warning · text complete",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Compare the tempting English-shaped form with the authored German noun.",
                        ["hi"] = "अंग्रेज़ी जैसे दिखने वाले रूप की लिखे हुए जर्मन संज्ञा से तुलना करें।",
                    }),
                ["source-language"] = new(
                    TemplateParameterKind.Text,
                    Text: "English"),
                ["tempting-form"] = new(
                    TemplateParameterKind.Text,
                    Text: "kaffee"),
                ["target-form"] = new(
                    TemplateParameterKind.Text,
                    Text: "Kaffee"),
                ["explanation"] = new(
                    TemplateParameterKind.Text,
                    Text: "English normally writes common nouns such as coffee in lowercase. German writes Kaffee with a capital K."),
                ["risk"] = new(
                    TemplateParameterKind.Text,
                    Text: "Do not generalize this noun-capitalization cue to every kind of German word."),
                ["actions"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("notice-capital", "I noticed the capital"),
                        new("dismiss-alarm", "Dismiss alert"),
                    ]),
                ["acknowledgement"] = new(
                    TemplateParameterKind.Text,
                    Text: "notice-capital"),
                ["dismissal"] = new(
                    TemplateParameterKind.Text,
                    Text: "dismiss-alarm"),
            })),
        new(
            new TemplateId("cognate-thread"),
            "Cognate thread",
            "Transfer · routed written-form cue · explicit boundary",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Trace the selected written-form cue from the known word to German.",
                        ["hi"] = "चुने हुए लिखित-रूप संकेत को ज्ञात शब्द से जर्मन तक देखें।",
                    }),
                ["source-language"] = new(
                    TemplateParameterKind.Text,
                    Text: "English"),
                ["target-language"] = new(
                    TemplateParameterKind.Text,
                    Text: "German"),
                ["source-word"] = new(
                    TemplateParameterKind.Text,
                    Text: "name"),
                ["target-word"] = new(
                    TemplateParameterKind.Text,
                    Text: "Name"),
                ["explanation"] = new(
                    TemplateParameterKind.Text,
                    Text: "English name can help you remember German Name. Keep the German capital letter and learn the full frame Ich heiße …"),
                ["actions"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("trace-thread", "Trace this connection"),
                        new("dismiss-thread", "Dismiss thread"),
                    ]),
                ["acknowledgement"] = new(
                    TemplateParameterKind.Text,
                    Text: "trace-thread"),
                ["dismissal"] = new(
                    TemplateParameterKind.Text,
                    Text: "dismiss-thread"),
            })),
        new(
            new TemplateId("contrast-panes"),
            "Contrast panes",
            "Transfer · routed Hindi comparison · explicit non-transfer boundary",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Compare what the selected Hindi bridge supports with what changes in German.",
                        ["hi"] = "चुना हुआ हिंदी सेतु किसमें सहायक है और जर्मन में क्या बदलता है, इसकी तुलना करें।",
                    }),
                ["source-language"] = new(
                    TemplateParameterKind.Text,
                    Text: "Hindi"),
                ["target-language"] = new(
                    TemplateParameterKind.Text,
                    Text: "German"),
                ["transfers"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("gender-category", "Prior experience with grammatical gender may make the category familiar."),
                    ]),
                ["changes"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("three-genders", "German uses three noun genders in this Preview."),
                        new("learn-article", "Learn each German noun with its article."),
                    ]),
                ["risk"] = new(
                    TemplateParameterKind.Text,
                    Text: "Do not infer a German article from the Hindi translation."),
                ["actions"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("compare-panes", "I compared both panes"),
                        new("dismiss-comparison", "Dismiss comparison"),
                    ]),
                ["acknowledgement"] = new(
                    TemplateParameterKind.Text,
                    Text: "compare-panes"),
                ["dismissal"] = new(
                    TemplateParameterKind.Text,
                    Text: "dismiss-comparison"),
            })),
        new(
            new TemplateId("scenario-theatre"),
            "Scenario theatre",
            "Scenario · projected café task · deterministic response check",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Read the mission, then answer the café worker from the authored choices.",
                        ["hi"] = "मिशन पढ़ें, फिर लिखे हुए विकल्पों से कैफ़े कर्मचारी को उत्तर दें।",
                    }),
                ["task"] = new(
                    TemplateParameterKind.TaskReference,
                    Task: ScenarioTask()),
                ["state-label"] = new(
                    TemplateParameterKind.Text,
                    Text: "At the counter"),
                ["npc-line"] = new(
                    TemplateParameterKind.Text,
                    Text: "Guten Tag! Was möchten Sie?"),
                ["responses"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("frame-only", "Ich möchte."),
                        new("full-request", "Ich möchte einen Kaffee, bitte."),
                    ]),
                ["answer"] = new(
                    TemplateParameterKind.Text,
                    Text: "full-request"),
                ["retry-hint"] = new(
                    TemplateParameterKind.Text,
                    Text: "Name one available drink and keep the complete request frame."),
            })),
        new(
            new TemplateId("consequence-verdict"),
            "Consequence verdict",
            "Scenario · projected outcome · detailed static report",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Watch the projected task consequence, then use the detailed static report.",
                        ["hi"] = "दिखाए गए कार्य परिणाम को देखें, फिर विस्तृत स्थिर रिपोर्ट का उपयोग करें।",
                    }),
                ["subject"] = new(
                    TemplateParameterKind.Text,
                    Text: "Learner puppet"),
                ["state-label"] = new(
                    TemplateParameterKind.Text,
                    Text: "Order pending"),
                ["verdicts"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("ready", "Ready for the result"),
                        new("success", "Order understood"),
                        new("uncertain", "One detail needs checking"),
                        new("failure", "Request needs another turn"),
                    ]),
                ["consequences"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("ready", "The puppet waits at the counter."),
                        new("success", "The puppet lifts the cup token."),
                        new("uncertain", "The puppet pauses beside the order card."),
                        new("failure", "The order card returns to the counter."),
                    ]),
                ["report-lines"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("goal", "Goal: request one drink politely."),
                        new("evidence", "Evidence: deterministic task outcome only."),
                        new("boundary", "Typed practice creates no pronunciation score."),
                    ]),
                ["actions"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("continue", "Continue"),
                        new("retry", "Retry task"),
                    ]),
                ["retry-action"] = new(
                    TemplateParameterKind.Text,
                    Text: "retry"),
                ["subject-asset"] = new(
                    TemplateParameterKind.AssetReference,
                    AssetReferenceId: "asset.de.stage.learner"),
            })),
        new(
            new TemplateId("review-flash"),
            "Review flash",
            "Review · recall reveal · stable review-v1 rating",
            new LanguageCode("en"),
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Recall the complete request before revealing the reviewed answer.",
                        ["hi"] = "समीक्षा उत्तर दिखाने से पहले पूरा अनुरोध याद करें।",
                    }),
                ["prompt"] = new(
                    TemplateParameterKind.Text,
                    Text: "Ask politely for one coffee."),
                ["answer"] = new(
                    TemplateParameterKind.Text,
                    Text: "Ich möchte einen Kaffee, bitte."),
                ["details"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("frame", "Frame begins with Ich möchte."),
                        new("item", "Item: einen Kaffee"),
                        new("politeness", "Politeness: bitte"),
                    ]),
                ["ratings"] = new(
                    TemplateParameterKind.OptionList,
                    Options:
                    [
                        new("again", "Again"),
                        new("hard", "Hard"),
                        new("good", "Good"),
                        new("easy", "Easy"),
                    ]),
                ["configuration-version"] = new(
                    TemplateParameterKind.Text,
                    Text: "review-v1"),
            })),
    ];

    private static TaskTemplateContent ScenarioTask() => new(
        "de.task.cafe.order-one-item",
        "de",
        "cafe",
        "A1",
        "Request one available drink politely.",
        "At a café counter, choose Kaffee, Tee, or Wasser.",
        "Customer",
        "Café worker",
        ["de.function.order-polite"],
        ["de.lexicon.cafe-items"],
        "de.state.order.waiting",
        ["de.state.order.complete"],
        [
            new TaskStateContent(
                "de.state.order.waiting",
                ["requestItem"],
                ["Guten Tag! Was möchten Sie?"]),
            new TaskStateContent(
                "de.state.order.complete",
                ["continue", "exit"],
                ["Gern. Einen Moment, bitte."]),
        ],
        [
            new TaskTransitionContent(
                "de.state.order.waiting",
                "de.state.order.complete",
                "de.eval.order-full-request"),
        ],
        [
            new TaskEvaluatorContent(
                "de.eval.order-full-request",
                TaskEvaluatorKind.RequiredTokenSequence,
                ["ich", "möchte", "einen", "kaffee"]),
            new TaskEvaluatorContent(
                "de.eval.order-complete",
                TaskEvaluatorKind.StateReached,
                ["de.state.order.complete"]),
        ],
        [
            new TaskSuccessCondition(
                "de.eval.order-complete",
                "Reach the complete state with the request frame and one available drink."),
        ],
        ["source.de.dib-2017"],
        new ContentReview(
            ContentReviewStatus.MachineValidated,
            Reviewer: null,
            ReviewedOn: null,
            "Synthetic gallery projection. Competent review remains pending."));
}
