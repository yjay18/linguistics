using System.Reflection;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Linguistics.App.Features.Learn.Templates;
using Linguistics.Core.Content;
using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class TemplateRegistryTests
{
    [TestMethod]
    public void RegistryPassesOnlyTheFiveRendererInputs()
    {
        var id = new TemplateId("fixture-template");
        var parameters = new ResolvedTemplateParameters(
            new Dictionary<string, ResolvedTemplateParameter>
            {
                ["title"] = new(TemplateParameterKind.Text, Text: "Hallo"),
            });
        var language = new LanguageCode("en");
        var reported = new List<TemplateOutcome>();
        var expected = new Border();
        TemplateRendererFactory renderer = (cache, actualParameters, actualLanguage, actualMotion, callback) =>
        {
            Assert.IsNull(cache);
            Assert.AreSame(parameters, actualParameters);
            Assert.AreEqual(language, actualLanguage);
            Assert.IsTrue(actualMotion);
            callback(new TemplateOutcome(TemplateOutcomeState.Success, "fixture-response"));
            return expected;
        };
        var registry = new TemplateRegistry(
            [new KeyValuePair<TemplateId, TemplateRendererFactory>(id, renderer)]);

        var rendered = registry.Render(id, parameters, language, shouldReduceMotion: true, reported.Add);

        Assert.AreSame(expected, rendered);
        Assert.HasCount(1, reported);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[0].State);
        CollectionAssert.AreEqual(new[] { id }, registry.RegisteredTemplateIds.ToArray());
    }

    [TestMethod]
    public void RegistryRejectsMissingAndDuplicateRenderers()
    {
        var id = new TemplateId("fixture-template");
        TemplateRendererFactory renderer = (_, _, _, _, _) => new Border();
        var registry = new TemplateRegistry([]);

        Assert.ThrowsExactly<KeyNotFoundException>(() => registry.Render(
            id,
            new ResolvedTemplateParameters(new Dictionary<string, ResolvedTemplateParameter>()),
            new LanguageCode("en"),
            shouldReduceMotion: false,
            _ => { }));
        Assert.ThrowsExactly<ArgumentException>(() => new TemplateRegistry(
            [
                new KeyValuePair<TemplateId, TemplateRendererFactory>(id, renderer),
                new KeyValuePair<TemplateId, TemplateRendererFactory>(id, renderer),
            ]));
    }

    [TestMethod]
    public void LocalizedTemplateTextUsesOnlyTheSelectedInstructionLanguage()
    {
        var parameters = new ResolvedTemplateParameters(
            new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Choose one.",
                        ["hi"] = "एक चुनें।",
                    }),
            });

        Assert.AreEqual(
            "एक चुनें।",
            TemplateRendering.Localized(
                parameters,
                "instruction",
                new LanguageCode("hi")));
        Assert.ThrowsExactly<KeyNotFoundException>(() =>
            TemplateRendering.Localized(
                parameters,
                "instruction",
                new LanguageCode("fr")));
    }

    [TestMethod]
    public void DefaultRegistryMatchesTheRegisteredTemplateSchemas()
    {
        CollectionAssert.AreEqual(
            LessonTemplateSchemas.All
                .Select(schema => schema.Id.Value)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray(),
            TemplateRegistry.CreateDefault()
                .RegisteredTemplateIds
                .Select(id => id.Value)
                .ToArray());
    }

    [TestMethod]
    public void GalleryHasOneFixtureForEveryRegisteredTemplate()
    {
        CollectionAssert.AreEqual(
            TemplateRegistry.CreateDefault()
                .RegisteredTemplateIds
                .Select(id => id.Value)
                .ToArray(),
            TemplateGalleryFixtures.All
                .Select(fixture => fixture.TemplateId.Value)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray());
    }

    [TestMethod]
    public void RendererContractCannotReceivePersistenceOrMasteryServices()
    {
        var invoke = typeof(TemplateRendererFactory).GetMethod("Invoke")!;

        CollectionAssert.AreEqual(
            new[]
            {
                typeof(Linguistics.App.Content.ContentImageCache),
                typeof(ResolvedTemplateParameters),
                typeof(LanguageCode),
                typeof(bool),
                typeof(Action<TemplateOutcome>),
            },
            invoke.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
        Assert.AreEqual(typeof(Control), invoke.ReturnType);

        var rendererTypes = typeof(TemplateRegistry).Assembly
            .GetTypes()
            .Where(type =>
                type.Namespace == typeof(TemplateRegistry).Namespace &&
                type.Name.EndsWith("Renderer", StringComparison.Ordinal))
            .ToArray();
        Assert.IsTrue(rendererTypes.All(type => type.IsAbstract && type.IsSealed),
            "Template renderers must be static and dependency-free.");

        var templatesDirectory = Path.Combine(
            RepositoryRoot,
            "src",
            "Linguistics.App",
            "Features",
            "Learn",
            "Templates");
        var forbidden = new[]
        {
            "Linguistics.App.Persistence",
            "LearnerProfileOwner",
            "LearnerLearningState",
            "ConceptProgress",
            "Mastery",
            "ReviewScheduler",
            "LearnerRepository",
        };
        foreach (var path in Directory.EnumerateFiles(templatesDirectory, "*Renderer*.cs"))
        {
            var source = File.ReadAllText(path);
            foreach (var term in forbidden)
            {
                Assert.IsFalse(source.Contains(term, StringComparison.Ordinal),
                    $"{Path.GetFileName(path)} may not reference {term}.");
            }
        }
    }

    [TestMethod]
    public void EveryCatalogFixtureRendersEveryOutcomeAndTextOnlyStateWithoutProviders()
    {
        var registry = TemplateRegistry.CreateDefault();
        var reported = new List<TemplateOutcome>();

        foreach (var fixture in TemplateGalleryFixtures.All)
        {
            foreach (var outcome in Enum.GetValues<TemplateOutcomeState>())
            {
                foreach (var textOnly in new[] { false, true })
                {
                    var parameters = fixture.Parameters with
                    {
                        PreviewOutcome = outcome,
                        UseTextOnlyFallback = textOnly,
                    };

                    var control = registry.Render(
                        fixture.TemplateId,
                        parameters,
                        fixture.InstructionLanguage,
                        shouldReduceMotion: true,
                        reported.Add);

                    Assert.IsNotNull(
                        control,
                        $"{fixture.TemplateId} failed for {outcome}, text only {textOnly}.");
                    if (textOnly)
                    {
                        var doubledPunctuation = control
                            .GetLogicalDescendants()
                            .OfType<TextBlock>()
                            .Select(text => text.Text)
                            .FirstOrDefault(text => text?.Contains("..", StringComparison.Ordinal) == true);
                        Assert.IsNull(
                            doubledPunctuation,
                            $"{fixture.TemplateId} text-only copy contains doubled punctuation: {doubledPunctuation}");
                    }
                }
            }
        }

        Assert.IsEmpty(reported, "Rendering must not report an outcome without a learner action.");
    }

    [TestMethod]
    public void WordOrderTrainReservesVerbSecondAndRightBracketCars()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("word-order-train"));
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            _ => { });
        var names = rendered
            .GetLogicalDescendants()
            .OfType<Control>()
            .Select(AutomationProperties.GetName)
            .Where(name => name is not null)
            .ToArray();

        CollectionAssert.Contains(names, "VERB 2 reserved train car, empty");
        CollectionAssert.Contains(names, "RIGHT BRACKET reserved train car, empty");
    }

    [TestMethod]
    public void PaperDialogueSendsOnlyTheSelectedCaptionToLocalSpeech()
    {
        using var provider = new RecordingSpeechProvider();
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("paper-dialogue"));
        var rendered = TemplateRegistry.CreateDefault(speechSynthesisProvider: provider).Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            _ => { });
        var playButton = rendered
            .GetLogicalDescendants()
            .OfType<Button>()
            .Single(button =>
                AutomationProperties.GetAutomationId(button) == "PaperDialoguePlaySpeakerOne");

        playButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.IsNotNull(provider.LastRequest);
        Assert.AreEqual("Guten Tag!", provider.LastRequest.Text);
        Assert.AreEqual(new LanguageCode("de"), provider.LastRequest.Language);
        Assert.AreEqual("paper-dialogue:Mina:1", provider.LastRequest.Seed);
    }

    [TestMethod]
    public void PaperDialogueKeepsCompleteCaptionsWhenLocalSpeechIsUnavailable()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("paper-dialogue"));
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            _ => { });
        var playbackButtons = rendered
            .GetLogicalDescendants()
            .OfType<Button>()
            .Where(button =>
                AutomationProperties.GetAutomationId(button)?.StartsWith(
                    "PaperDialoguePlay",
                    StringComparison.Ordinal) == true)
            .ToArray();
        var status = rendered
            .GetLogicalDescendants()
            .OfType<TextBlock>()
            .Single(text =>
                AutomationProperties.GetAutomationId(text) == "PaperDialoguePlaybackStatus");

        Assert.HasCount(2, playbackButtons);
        Assert.IsTrue(playbackButtons.All(button => !button.IsEnabled));
        Assert.AreEqual("Local playback is unavailable. Captions remain complete.", status.Text);
    }

    [TestMethod]
    public void PictureMatchSendsOnlyTheAuthoredTargetToLocalSpeech()
    {
        using var provider = new RecordingSpeechProvider();
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("picture-match"));
        var rendered = TemplateRegistry.CreateDefault(speechSynthesisProvider: provider).Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            _ => { });
        var playButton = rendered
            .GetLogicalDescendants()
            .OfType<Button>()
            .Single(button =>
                AutomationProperties.GetAutomationId(button) == "PictureMatchPlayTarget");

        playButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.IsNotNull(provider.LastRequest);
        Assert.AreEqual("Kaffee", provider.LastRequest.Text);
        Assert.AreEqual(new LanguageCode("de"), provider.LastRequest.Language);
        Assert.AreEqual("picture-match:kaffee", provider.LastRequest.Seed);
    }

    [TestMethod]
    public void PictureMatchKeepsTheWrittenTargetWhenLocalSpeechIsUnavailable()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("picture-match"));
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            _ => { });
        var playButton = rendered
            .GetLogicalDescendants()
            .OfType<Button>()
            .Single(button =>
                AutomationProperties.GetAutomationId(button) == "PictureMatchPlayTarget");
        var status = rendered
            .GetLogicalDescendants()
            .OfType<TextBlock>()
            .Single(text =>
                AutomationProperties.GetAutomationId(text) == "PictureMatchPlaybackStatus");

        Assert.IsFalse(playButton.IsEnabled);
        Assert.AreEqual("Written target: Kaffee. Local playback is unavailable.", status.Text);
    }

    [TestMethod]
    public void ListenPickImageSendsOnlyTheAuthoredPromptToLocalSpeech()
    {
        using var provider = new RecordingSpeechProvider();
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("listen-pick-image"));
        var rendered = TemplateRegistry.CreateDefault(speechSynthesisProvider: provider).Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            _ => { });
        var playButton = rendered
            .GetLogicalDescendants()
            .OfType<Button>()
            .Single(button =>
                AutomationProperties.GetAutomationId(button) == "ListenPickImagePlayPrompt");

        playButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.IsNotNull(provider.LastRequest);
        Assert.AreEqual("Ich möchte einen Tee, bitte.", provider.LastRequest.Text);
        Assert.AreEqual(new LanguageCode("de"), provider.LastRequest.Language);
        Assert.AreEqual("listen-pick-image:tea", provider.LastRequest.Seed);
    }

    [TestMethod]
    public void ListenPickImageShowsItsWrittenPromptWhenLocalSpeechIsUnavailable()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("listen-pick-image"));
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            _ => { });
        var playButton = rendered
            .GetLogicalDescendants()
            .OfType<Button>()
            .Single(button =>
                AutomationProperties.GetAutomationId(button) == "ListenPickImagePlayPrompt");
        var transcript = rendered
            .GetLogicalDescendants()
            .OfType<TextBlock>()
            .Single(text =>
                AutomationProperties.GetAutomationId(text) == "ListenPickImageTranscript");

        Assert.IsFalse(playButton.IsEnabled);
        Assert.IsTrue(transcript.IsVisible);
        Assert.AreEqual("Written prompt: Ich möchte einen Tee, bitte.", transcript.Text);
    }

    [TestMethod]
    public void EchoStageOffersNormalAndSlowerLocalPlayback()
    {
        using var provider = new RecordingSpeechProvider();
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("echo-stage"));
        var rendered = TemplateRegistry.CreateDefault(speechSynthesisProvider: provider).Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            _ => { });
        var buttons = rendered
            .GetLogicalDescendants()
            .OfType<Button>()
            .Where(button => AutomationProperties.GetAutomationId(button) is not null)
            .ToDictionary(
                button => AutomationProperties.GetAutomationId(button)!,
                StringComparer.Ordinal);

        buttons["EchoStagePlayNormal"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        buttons["EchoStagePlaySlower"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.HasCount(2, provider.Requests);
        Assert.AreEqual("Ich möchte einen Tee, bitte.", provider.Requests[0].Text);
        Assert.AreEqual(1d, provider.Requests[0].Rate);
        Assert.AreEqual("Ich möchte einen Tee, bitte.", provider.Requests[1].Text);
        Assert.AreEqual(0.72d, provider.Requests[1].Rate);
    }

    [TestMethod]
    public void EchoStageRequiresConfirmationBeforeLocalRecognition()
    {
        using var synthesis = new RecordingSpeechProvider();
        using var recognition = new RecordingRecognitionProvider();
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("echo-stage"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault(
                speechSynthesisProvider: synthesis,
                speechRecognitionProvider: recognition,
                pronunciationAssessmentProvider: new TranscriptPronunciationAssessmentProvider(),
                microphoneAllowed: true)
            .Render(
                fixture.TemplateId,
                fixture.Parameters,
                fixture.InstructionLanguage,
                shouldReduceMotion: true,
                reported.Add);
        var controls = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var request = controls.OfType<Button>().Single(button =>
            AutomationProperties.GetAutomationId(button) == "EchoStageRequestMicrophone");
        var confirm = controls.OfType<Button>().Single(button =>
            AutomationProperties.GetAutomationId(button) == "EchoStageConfirmMicrophone");
        var disclosure = controls.OfType<Border>().Single(border =>
            AutomationProperties.GetAutomationId(border) == "EchoStageMicrophoneDisclosure");

        request.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.AreEqual(0, recognition.RequestCount);
        Assert.IsTrue(disclosure.IsVisible);

        confirm.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.AreEqual(1, recognition.RequestCount);
        Assert.IsNotNull(recognition.LastRequest);
        Assert.AreEqual(TimeSpan.FromSeconds(15), recognition.LastRequest.MaximumDuration);
        Assert.IsFalse(recognition.LastRequest.RetainAudio);
        Assert.HasCount(1, reported);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[0].State);
        Assert.IsNull(reported[0].ResponseId);
    }

    [TestMethod]
    public void EchoStageTextOnlyRouteNeverUsesTheMicrophone()
    {
        using var recognition = new RecordingRecognitionProvider();
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("echo-stage"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault(
                speechRecognitionProvider: recognition,
                pronunciationAssessmentProvider: new TranscriptPronunciationAssessmentProvider(),
                microphoneAllowed: true)
            .Render(
                fixture.TemplateId,
                fixture.Parameters with { UseTextOnlyFallback = true },
                fixture.InstructionLanguage,
                shouldReduceMotion: true,
                reported.Add);
        var controls = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var voiceButton = controls.OfType<Button>().Single(button =>
            AutomationProperties.GetAutomationId(button) == "EchoStageRequestMicrophone");
        var response = controls.OfType<TextBox>().Single(textBox =>
            AutomationProperties.GetAutomationId(textBox) == "EchoStageTextResponse");
        var compare = controls.OfType<Button>().Single(button =>
            AutomationProperties.GetAutomationId(button) == "EchoStageCompareText");

        response.Text = "  ICH MÖCHTE EINEN TEE, BITTE! ";
        compare.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.IsFalse(voiceButton.IsVisible);
        Assert.AreEqual(0, recognition.RequestCount);
        Assert.HasCount(1, reported);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[0].State);
    }

    [TestMethod]
    public void ReadAloudCardTextOnlyRouteChecksWordingWithoutPronunciationClaims()
    {
        using var recognition = new RecordingRecognitionProvider();
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("read-aloud-card"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault(
                speechRecognitionProvider: recognition,
                pronunciationAssessmentProvider: new TranscriptPronunciationAssessmentProvider(),
                microphoneAllowed: true)
            .Render(
                fixture.TemplateId,
                fixture.Parameters with { UseTextOnlyFallback = true },
                fixture.InstructionLanguage,
                shouldReduceMotion: true,
                reported.Add);
        var controls = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var card = controls.Single(control =>
            AutomationProperties.GetAutomationId(control) == "ReadAloudCardText");
        var voiceButton = controls.OfType<Button>().Single(button =>
            AutomationProperties.GetAutomationId(button) == "ReadAloudCardRequestMicrophone");
        var response = controls.OfType<TextBox>().Single(textBox =>
            AutomationProperties.GetAutomationId(textBox) == "ReadAloudCardTextResponse");
        var compare = controls.OfType<Button>().Single(button =>
            AutomationProperties.GetAutomationId(button) == "ReadAloudCardCompareText");

        response.Text = "GUTEN MORGEN. EINEN KAFFEE, BITTE!";
        compare.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.AreEqual(
            "Read aloud card. Guten Morgen. Einen Kaffee, bitte.",
            AutomationProperties.GetName(card));
        Assert.IsFalse(voiceButton.IsVisible);
        Assert.AreEqual(0, recognition.RequestCount);
        Assert.HasCount(1, reported);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[0].State);
        Assert.IsTrue(rendered
            .GetLogicalDescendants()
            .OfType<TextBlock>()
            .Any(text => text.Text?.Contains(
                "cannot score phonemes, accent, or native-likeness",
                StringComparison.Ordinal) == true));
    }

    [TestMethod]
    public void PromptRespondPlaysOnlyThePromptAndAcceptsAnAuthoredTypedAnswer()
    {
        using var synthesis = new RecordingSpeechProvider();
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("prompt-respond"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault(speechSynthesisProvider: synthesis).Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            reported.Add);
        var controls = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var playPrompt = controls.OfType<Button>().Single(button =>
            AutomationProperties.GetAutomationId(button) == "PromptRespondPlayPrompt");
        var response = controls.OfType<TextBox>().Single(textBox =>
            AutomationProperties.GetAutomationId(textBox) == "PromptRespondTextResponse");
        var compare = controls.OfType<Button>().Single(button =>
            AutomationProperties.GetAutomationId(button) == "PromptRespondCompareText");

        playPrompt.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        response.Text = "EINEN TEE, BITTE!";
        compare.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.IsNotNull(synthesis.LastRequest);
        Assert.AreEqual("Was möchtest du trinken?", synthesis.LastRequest.Text);
        Assert.AreEqual(new LanguageCode("de"), synthesis.LastRequest.Language);
        Assert.HasCount(1, reported);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[0].State);
        Assert.AreEqual("short", reported[0].ResponseId);
    }

    [TestMethod]
    public void SyllableClapUsesKeyboardButtonsAndNeverRequestsTheMicrophone()
    {
        using var synthesis = new RecordingSpeechProvider();
        using var recognition = new RecordingRecognitionProvider();
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("syllable-clap"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault(
                speechSynthesisProvider: synthesis,
                speechRecognitionProvider: recognition,
                pronunciationAssessmentProvider: new TranscriptPronunciationAssessmentProvider(),
                microphoneAllowed: true)
            .Render(
                fixture.TemplateId,
                fixture.Parameters with { UseTextOnlyFallback = true },
                fixture.InstructionLanguage,
                shouldReduceMotion: true,
                reported.Add);
        var buttons = rendered.GetLogicalDescendants().OfType<Button>().ToArray();
        var play = buttons.Single(button =>
            AutomationProperties.GetAutomationId(button) == "SyllableClapPlayPhrase");
        var tap = buttons.Single(button =>
            AutomationProperties.GetAutomationId(button) == "SyllableClapTap");
        var check = buttons.Single(button =>
            AutomationProperties.GetAutomationId(button) == "SyllableClapCheck");

        play.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        tap.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        check.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.IsFalse(buttons.Any(button =>
            AutomationProperties.GetAutomationId(button)?.Contains(
                "Microphone",
                StringComparison.Ordinal) == true));
        Assert.AreEqual(0, recognition.RequestCount);
        Assert.IsNotNull(synthesis.LastRequest);
        Assert.AreEqual("Kaffee", synthesis.LastRequest.Text);
        Assert.HasCount(1, reported);
        Assert.AreEqual(TemplateOutcomeState.Uncertain, reported[0].State);
        CollectionAssert.AreEqual(new[] { "tap-1" }, reported[0].OrderedOptionIds!.ToArray());
    }

    [TestMethod]
    public void LongShortVowelKeepsProductionUnscoredAndChoiceDeterministic()
    {
        using var synthesis = new RecordingSpeechProvider();
        using var recognition = new RecordingRecognitionProvider();
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("long-short-vowel"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault(
                speechSynthesisProvider: synthesis,
                speechRecognitionProvider: recognition,
                pronunciationAssessmentProvider: new TranscriptPronunciationAssessmentProvider(),
                microphoneAllowed: true)
            .Render(
                fixture.TemplateId,
                fixture.Parameters with { UseTextOnlyFallback = true },
                fixture.InstructionLanguage,
                shouldReduceMotion: true,
                reported.Add);
        var buttons = rendered.GetLogicalDescendants().OfType<Button>().ToArray();
        var play = buttons.Single(button =>
            AutomationProperties.GetAutomationId(button) == "LongShortVowelPlayTarget");
        var practice = buttons.Single(button =>
            AutomationProperties.GetAutomationId(button) == "LongShortVowelPractice");
        var chooseLong = buttons.Single(button =>
            AutomationProperties.GetAutomationId(button) == "LongShortVowelOption_long");
        var status = rendered
            .GetLogicalDescendants()
            .OfType<TextBlock>()
            .Single(text =>
                AutomationProperties.GetAutomationId(text) == "LongShortVowelPracticeStatus");

        play.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        practice.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.IsEmpty(reported);
        Assert.AreEqual(0, recognition.RequestCount);
        Assert.Contains("not scored", status.Text ?? string.Empty, StringComparison.Ordinal);

        chooseLong.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.IsNotNull(synthesis.LastRequest);
        Assert.AreEqual("Staat", synthesis.LastRequest.Text);
        Assert.HasCount(1, reported);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[0].State);
        Assert.AreEqual("long", reported[0].ResponseId);
    }

    [TestMethod]
    public void SignReadingUsesAuthoredTextWhenThePhotographIsUnavailable()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("sign-reading"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            reported.Add);
        var descendants = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var buttons = descendants.OfType<Button>().ToArray();
        var chooseWrong = buttons.Single(button =>
            AutomationProperties.GetAutomationId(button) == "SignReadingOption_everyone");
        var chooseCorrect = buttons.Single(button =>
            AutomationProperties.GetAutomationId(button) == "SignReadingOption_customers");
        var fallback = descendants.Single(control =>
            AutomationProperties.GetAutomationId(control) == "SignReadingAssetStatus");

        Assert.IsFalse(descendants.OfType<Image>().Any());
        Assert.Contains(
            "photograph unavailable",
            AutomationProperties.GetName(fallback) ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
        Assert.IsTrue(descendants
            .OfType<TextBlock>()
            .Any(text => string.Equals(
                text.Text,
                "Eingang nur für Kunden",
                StringComparison.Ordinal)));
        Assert.IsTrue(buttons.Any(button =>
            AutomationProperties.GetAutomationId(button) == "SignReadingReplay"));
        Assert.IsTrue(buttons.Any(button =>
            AutomationProperties.GetAutomationId(button) == "SignReadingSkip"));

        chooseWrong.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        chooseCorrect.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.HasCount(2, reported);
        Assert.AreEqual(TemplateOutcomeState.Failure, reported[0].State);
        Assert.AreEqual("everyone", reported[0].ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[1].State);
        Assert.AreEqual("customers", reported[1].ResponseId);
    }

    [TestMethod]
    public void FormFillKeepsSyntheticResponsesLocalAndReportsFieldIds()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("form-fill"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            reported.Add);
        var descendants = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var inputs = descendants
            .OfType<TextBox>()
            .ToDictionary(
                input => AutomationProperties.GetAutomationId(input)!,
                StringComparer.Ordinal);
        var buttons = descendants.OfType<Button>().ToArray();
        var check = buttons.Single(button =>
            AutomationProperties.GetAutomationId(button) == "FormFillCheck");
        var clear = buttons.Single(button =>
            AutomationProperties.GetAutomationId(button) == "FormFillClear");

        check.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        inputs["FormFillField_name"].Text = "Mina Weber";
        inputs["FormFillField_origin"].Text = "Hamburg";
        inputs["FormFillField_address"].Text = "Marktstraße 5";
        check.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        inputs["FormFillField_origin"].Text = "Berlin";
        check.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.HasCount(3, reported);
        Assert.AreEqual(TemplateOutcomeState.Uncertain, reported[0].State);
        Assert.AreEqual(TemplateOutcomeState.Failure, reported[1].State);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[2].State);
        CollectionAssert.AreEqual(
            new[] { "name", "origin", "address" },
            reported[2].OrderedOptionIds!.ToArray());
        Assert.IsFalse(reported[2].OrderedOptionIds!.Contains("Mina Weber", StringComparer.Ordinal));
        Assert.IsTrue(buttons.Any(button =>
            AutomationProperties.GetAutomationId(button) == "FormFillReplay"));
        Assert.IsTrue(buttons.Any(button =>
            AutomationProperties.GetAutomationId(button) == "FormFillSkip"));

        clear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.IsTrue(inputs.Values.All(input => string.IsNullOrEmpty(input.Text)));
    }

    [TestMethod]
    public void NoteWriteReportsOnlyMatchedAuthoredCriteria()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("note-write"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            reported.Add);
        var descendants = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var input = descendants.OfType<TextBox>().Single(textBox =>
            AutomationProperties.GetAutomationId(textBox) == "NoteWriteInput");
        var buttons = descendants.OfType<Button>().ToArray();
        var check = buttons.Single(button =>
            AutomationProperties.GetAutomationId(button) == "NoteWriteCheck");
        var clear = buttons.Single(button =>
            AutomationProperties.GetAutomationId(button) == "NoteWriteClear");

        check.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        input.Text = "Ich bin auf dem Markt.";
        check.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        input.Text = "Ich bin auf dem Markt und komme um sechs Uhr zurück.";
        check.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.HasCount(3, reported);
        Assert.AreEqual(TemplateOutcomeState.Uncertain, reported[0].State);
        Assert.AreEqual(TemplateOutcomeState.Failure, reported[1].State);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[2].State);
        CollectionAssert.AreEqual(
            new[] { "location", "return-time" },
            reported[2].OrderedOptionIds!.ToArray());
        Assert.IsFalse(reported[2].OrderedOptionIds!.Contains("Markt", StringComparer.Ordinal));
        Assert.IsTrue(buttons.Any(button =>
            AutomationProperties.GetAutomationId(button) == "NoteWriteReplay"));
        Assert.IsTrue(buttons.Any(button =>
            AutomationProperties.GetAutomationId(button) == "NoteWriteSkip"));

        clear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.IsTrue(string.IsNullOrEmpty(input.Text));
    }

    [TestMethod]
    public void MenuReadReportsDeterministicAuthoredPriceIds()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("menu-read"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            reported.Add);
        var descendants = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var buttons = descendants.OfType<Button>().ToArray();
        var wrong = buttons.Single(button =>
            AutomationProperties.GetAutomationId(button) == "MenuReadOption_price-280");
        var correct = buttons.Single(button =>
            AutomationProperties.GetAutomationId(button) == "MenuReadOption_price-340");

        wrong.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        correct.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.HasCount(2, reported);
        Assert.AreEqual(TemplateOutcomeState.Failure, reported[0].State);
        Assert.AreEqual("price-280", reported[0].ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[1].State);
        Assert.AreEqual("price-340", reported[1].ResponseId);
        Assert.AreNotEqual("3,40 €", reported[1].ResponseId);
        Assert.IsTrue(buttons.Any(button =>
            AutomationProperties.GetAutomationId(button) == "MenuReadReplay"));
        Assert.IsTrue(buttons.Any(button =>
            AutomationProperties.GetAutomationId(button) == "MenuReadSkip"));
        Assert.IsTrue(descendants.OfType<TextBlock>().Any(text =>
            text.Text == "Kännchen Tee · 3,40 €"));
    }

    [TestMethod]
    public void ScheduleReadReportsDeterministicAuthoredTimeIds()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("schedule-read"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            reported.Add);
        var descendants = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var buttons = descendants.OfType<Button>().ToArray();
        var wrong = buttons.Single(button =>
            AutomationProperties.GetAutomationId(button) == "ScheduleReadOption_time-0900");
        var correct = buttons.Single(button =>
            AutomationProperties.GetAutomationId(button) == "ScheduleReadOption_time-1000");

        wrong.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        correct.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.HasCount(2, reported);
        Assert.AreEqual(TemplateOutcomeState.Failure, reported[0].State);
        Assert.AreEqual("time-0900", reported[0].ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[1].State);
        Assert.AreEqual("time-1000", reported[1].ResponseId);
        Assert.AreNotEqual("10:00 Uhr", reported[1].ResponseId);
        Assert.IsTrue(buttons.Any(button =>
            AutomationProperties.GetAutomationId(button) == "ScheduleReadReplay"));
        Assert.IsTrue(buttons.Any(button =>
            AutomationProperties.GetAutomationId(button) == "ScheduleReadSkip"));
        Assert.IsTrue(descendants.OfType<TextBlock>().Any(text =>
            text.Text == "Dienstag · 10:00 bis 19:00"));
    }

    [TestMethod]
    public void SpellingTilesReportsOnlyAuthoredTileOrder()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("spelling-tiles"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            reported.Add);
        var descendants = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var buttons = descendants.OfType<Button>().ToArray();
        var byId = buttons.ToDictionary(
            button => AutomationProperties.GetAutomationId(button) ?? string.Empty,
            StringComparer.Ordinal);
        var check = byId["SpellingTilesCheck"];
        var reset = byId["SpellingTilesReset"];

        check.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        foreach (var id in new[] { "letter-p", "letter-a", "letter-f", "letter-e", "letter-l" })
        {
            byId[$"SpellingTilesBank_{id}"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        check.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        reset.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        foreach (var id in new[] { "letter-a", "letter-p", "letter-f", "letter-e", "letter-l" })
        {
            byId[$"SpellingTilesBank_{id}"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        check.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.HasCount(3, reported);
        Assert.AreEqual(TemplateOutcomeState.Uncertain, reported[0].State);
        Assert.AreEqual(TemplateOutcomeState.Failure, reported[1].State);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[2].State);
        CollectionAssert.AreEqual(
            new[] { "letter-a", "letter-p", "letter-f", "letter-e", "letter-l" },
            reported[2].OrderedOptionIds!.ToArray());
        Assert.IsFalse(reported[2].OrderedOptionIds!.Contains("A", StringComparer.Ordinal));
        Assert.IsTrue(byId.ContainsKey("SpellingTilesReplay"));
        Assert.IsTrue(byId.ContainsKey("SpellingTilesSkip"));
        Assert.IsTrue(descendants.OfType<TextBlock>().Any(text => text.Text == "pe"));
    }

    [TestMethod]
    public void BridgeNoteUsesRoutedCopyAndAdvisoryActionsOnly()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("bridge-note"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            reported.Add);
        var controls = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var byId = controls
            .Where(control => AutomationProperties.GetAutomationId(control) is not null)
            .ToDictionary(
                control => AutomationProperties.GetAutomationId(control)!,
                StringComparer.Ordinal);

        byId["BridgeNoteAcknowledge"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.HasCount(1, reported);
        Assert.AreEqual(TemplateOutcomeState.Uncertain, reported[0].State);

        ((CheckBox)byId["BridgeNoteConfirmBridge"]).IsChecked = true;
        byId["BridgeNoteAcknowledge"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        byId["BridgeNoteDismissBridge"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.HasCount(3, reported);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[1].State);
        Assert.AreEqual("use-bridge", reported[1].ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Ready, reported[2].State);
        Assert.AreEqual("dismiss-bridge", reported[2].ResponseId);
        Assert.IsTrue(byId.ContainsKey("BridgeNoteReplay"));
        Assert.IsTrue(byId.ContainsKey("BridgeNoteSkip"));
        Assert.IsTrue(controls.OfType<TextBlock>().Any(text =>
            text.Text?.Contains("grammatical gender", StringComparison.Ordinal) == true));
    }

    [TestMethod]
    public void FalseFriendAlarmStampsTheTemptingFormAndReportsAdvisoryActions()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("false-friend-alarm"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            reported.Add);
        var controls = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var byId = controls
            .Where(control => AutomationProperties.GetAutomationId(control) is not null)
            .ToDictionary(
                control => AutomationProperties.GetAutomationId(control)!,
                StringComparer.Ordinal);

        byId["FalseFriendAlarmAcknowledge"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        byId["FalseFriendAlarmDismiss"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.HasCount(2, reported);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[0].State);
        Assert.AreEqual("notice-capital", reported[0].ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Ready, reported[1].State);
        Assert.AreEqual("dismiss-alarm", reported[1].ResponseId);
        var warningStamp = (Linguistics.App.Controls.PaperStamp)byId["FalseFriendAlarmStamp"];
        Assert.AreEqual("WATCH", warningStamp.Content);
        Assert.AreEqual(Linguistics.App.Controls.PaperAnchorLine.Head, Linguistics.App.Controls.PaperStage.GetAnchor(warningStamp));
        Assert.AreEqual(0.4, Linguistics.App.Controls.PaperStage.GetAnchorX(warningStamp));
        Assert.AreEqual(
            "English habit: kaffee",
            AutomationProperties.GetName(byId["TemptingFormCard"]));
        Assert.AreEqual(
            "German target form: Kaffee",
            AutomationProperties.GetName(byId["TargetFormCard"]));
        Assert.IsTrue(byId.ContainsKey("FalseFriendAlarmReplay"));
        Assert.IsTrue(byId.ContainsKey("FalseFriendAlarmSkip"));
    }

    [TestMethod]
    public void CognateThreadConnectsRoutedWordsAndReportsAdvisoryActions()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("cognate-thread"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            reported.Add);
        var controls = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var byId = controls
            .Where(control => AutomationProperties.GetAutomationId(control) is not null)
            .ToDictionary(
                control => AutomationProperties.GetAutomationId(control)!,
                StringComparer.Ordinal);

        byId["CognateThreadAcknowledge"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        byId["CognateThreadDismiss"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.HasCount(2, reported);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[0].State);
        Assert.AreEqual("trace-thread", reported[0].ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Ready, reported[1].State);
        Assert.AreEqual("dismiss-thread", reported[1].ResponseId);
        Assert.AreEqual(
            "English word name",
            AutomationProperties.GetName(byId["CognateThreadSourceWord"]));
        Assert.AreEqual(
            "German word Name",
            AutomationProperties.GetName(byId["CognateThreadTargetWord"]));
        Assert.IsTrue(byId.ContainsKey("CognateThreadLine"));
        Assert.IsTrue(byId.ContainsKey("CognateThreadReplay"));
        Assert.IsTrue(byId.ContainsKey("CognateThreadSkip"));
    }

    [TestMethod]
    public void ContrastPanesKeepTransferAndChangeSeparateAndAdvisory()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("contrast-panes"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            reported.Add);
        var controls = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var byId = controls
            .Where(control => AutomationProperties.GetAutomationId(control) is not null)
            .ToDictionary(
                control => AutomationProperties.GetAutomationId(control)!,
                StringComparer.Ordinal);

        byId["ContrastPanesAcknowledge"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        byId["ContrastPanesDismiss"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.HasCount(2, reported);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[0].State);
        Assert.AreEqual("compare-panes", reported[0].ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Ready, reported[1].State);
        Assert.AreEqual("dismiss-comparison", reported[1].ResponseId);
        StringAssert.Contains(
            AutomationProperties.GetName(byId["ContrastPanesTransfers"]),
            "Hindi");
        StringAssert.Contains(
            AutomationProperties.GetName(byId["ContrastPanesChanges"]),
            "German");
        Assert.IsTrue(byId.ContainsKey("ContrastPanesBoundary"));
        Assert.IsTrue(byId.ContainsKey("ContrastPanesHinge"));
        Assert.AreEqual(52, byId["ContrastPanesHinge"].Width);
        Assert.IsTrue(byId["ContrastPanesHinge"].GetLogicalDescendants().OfType<TextBlock>().Any(text =>
            text.Text == "COMPARE"));
        Assert.IsTrue(byId.ContainsKey("ContrastPanesReplay"));
        Assert.IsTrue(byId.ContainsKey("ContrastPanesSkip"));
    }

    [TestMethod]
    public void ScenarioTheatreProjectsTaskDataAndReportsOnlyResponseIds()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("scenario-theatre"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            reported.Add);
        var controls = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var byId = controls
            .Where(control => AutomationProperties.GetAutomationId(control) is not null)
            .ToDictionary(
                control => AutomationProperties.GetAutomationId(control)!,
                StringComparer.Ordinal);

        byId["ScenarioTheatreResponse_frame-only"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.IsTrue(byId["ScenarioTheatreRetry"].IsVisible);
        byId["ScenarioTheatreRetry"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.IsFalse(byId["ScenarioTheatreRetry"].IsVisible);
        byId["ScenarioTheatreResponse_full-request"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.HasCount(2, reported);
        Assert.AreEqual(TemplateOutcomeState.Failure, reported[0].State);
        Assert.AreEqual("frame-only", reported[0].ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[1].State);
        Assert.AreEqual("full-request", reported[1].ResponseId);
        StringAssert.Contains(
            AutomationProperties.GetName(byId["ScenarioTheatreGoal"]),
            "Request one available drink politely");
        Assert.AreEqual(
            "NPC puppet. Café worker",
            AutomationProperties.GetName(byId["ScenarioTheatreNpc"]));
        Assert.IsTrue(byId.ContainsKey("ScenarioTheatreReplay"));
        Assert.IsTrue(byId.ContainsKey("ScenarioTheatreSkip"));
        Assert.IsTrue(byId.ContainsKey("ScenarioTheatreTextEquivalent"));
    }

    [TestMethod]
    public void ConsequenceVerdictPreservesProjectionAndReportsOnlyActionIds()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("consequence-verdict"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters with { PreviewOutcome = TemplateOutcomeState.Failure },
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            reported.Add);
        var controls = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var byId = controls
            .Where(control => AutomationProperties.GetAutomationId(control) is not null)
            .ToDictionary(
                control => AutomationProperties.GetAutomationId(control)!,
                StringComparer.Ordinal);

        byId["ConsequenceVerdictAction_continue"].RaiseEvent(
            new RoutedEventArgs(Button.ClickEvent));
        byId["ConsequenceVerdictAction_retry"].RaiseEvent(
            new RoutedEventArgs(Button.ClickEvent));

        Assert.HasCount(2, reported);
        Assert.AreEqual(TemplateOutcomeState.Failure, reported[0].State);
        Assert.AreEqual("continue", reported[0].ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Ready, reported[1].State);
        Assert.AreEqual("retry", reported[1].ResponseId);
        StringAssert.Contains(
            AutomationProperties.GetName(byId["ConsequenceVerdictCard"]),
            "Request needs another turn");
        StringAssert.Contains(
            AutomationProperties.GetName(byId["ConsequenceVerdictReport"]),
            "deterministic task outcome");
        Assert.IsTrue(byId.ContainsKey("ConsequenceVerdictPuppet"));
        Assert.IsTrue(byId.ContainsKey("ConsequenceVerdictClearingLabel"));
        Assert.IsTrue(byId.ContainsKey("ConsequenceVerdictReplay"));
        Assert.IsTrue(byId.ContainsKey("ConsequenceVerdictSkip"));
        Assert.IsTrue(byId.ContainsKey("ConsequenceVerdictTextEquivalent"));
    }

    [TestMethod]
    public void ReviewFlashRevealsBeforeReportingStableRatingIds()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("review-flash"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            reported.Add);
        var controls = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var byId = controls
            .Where(control => AutomationProperties.GetAutomationId(control) is not null)
            .ToDictionary(
                control => AutomationProperties.GetAutomationId(control)!,
                StringComparer.Ordinal);

        Assert.IsTrue(byId["ReviewFlashFront"].IsVisible);
        Assert.IsFalse(byId["ReviewFlashBack"].IsVisible);
        Assert.IsFalse(byId["ReviewFlashRatings"].IsVisible);
        Assert.IsFalse(byId["ReviewFlashRating_again"].IsEnabled);
        byId["ReviewFlashReveal"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.IsFalse(byId["ReviewFlashFront"].IsVisible);
        Assert.IsTrue(byId["ReviewFlashBack"].IsVisible);
        Assert.IsTrue(byId["ReviewFlashRatings"].IsVisible);
        Assert.IsTrue(byId["ReviewFlashRating_again"].IsEnabled);

        byId["ReviewFlashRating_again"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        byId["ReviewFlashRating_good"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.HasCount(2, reported);
        Assert.AreEqual(TemplateOutcomeState.Failure, reported[0].State);
        Assert.AreEqual("again", reported[0].ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[1].State);
        Assert.AreEqual("good", reported[1].ResponseId);
        StringAssert.Contains(
            AutomationProperties.GetName(byId["ReviewFlashBack"]),
            "Ich möchte einen Kaffee, bitte");
        StringAssert.Contains(
            AutomationProperties.GetName(byId["ReviewFlashConfiguration"]),
            "review-v1");
        Assert.IsTrue(byId.ContainsKey("ReviewFlashReplay"));
        Assert.IsTrue(byId.ContainsKey("ReviewFlashSkip"));
        Assert.IsTrue(byId.ContainsKey("ReviewFlashStatus"));
    }

    [TestMethod]
    public void RecapScrapbookKeepsAuthoredPieceOrderAndReportsOnlyActionIds()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("recap-scrapbook"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            reported.Add);
        var controls = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var byId = controls
            .Where(control => AutomationProperties.GetAutomationId(control) is not null)
            .ToDictionary(
                control => AutomationProperties.GetAutomationId(control)!,
                StringComparer.Ordinal);

        var pieceIds = controls
            .Select(AutomationProperties.GetAutomationId)
            .Where(id => id?.StartsWith("RecapScrapbookPiece_", StringComparison.Ordinal) == true)
            .ToArray();
        CollectionAssert.AreEqual(
            new[]
            {
                "RecapScrapbookPiece_goal",
                "RecapScrapbookPiece_frame",
                "RecapScrapbookPiece_choice",
                "RecapScrapbookPiece_repair",
            },
            pieceIds);
        byId["RecapScrapbookAction_keep-open"].RaiseEvent(
            new RoutedEventArgs(Button.ClickEvent));
        byId["RecapScrapbookAction_finish-recap"].RaiseEvent(
            new RoutedEventArgs(Button.ClickEvent));

        Assert.HasCount(2, reported);
        Assert.AreEqual(TemplateOutcomeState.Ready, reported[0].State);
        Assert.AreEqual("keep-open", reported[0].ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[1].State);
        Assert.AreEqual("finish-recap", reported[1].ResponseId);
        StringAssert.Contains(
            AutomationProperties.GetName(byId["RecapScrapbookClosing"]),
            "full request frame");
        Assert.IsTrue(byId.ContainsKey("RecapScrapbookSpread"));
        Assert.IsTrue(byId.ContainsKey("RecapScrapbookReplay"));
        Assert.IsTrue(byId.ContainsKey("RecapScrapbookSkip"));
        Assert.IsTrue(byId.ContainsKey("RecapScrapbookTextEquivalent"));
        Assert.IsTrue(byId.ContainsKey("RecapScrapbookStatus"));
    }

    [TestMethod]
    public void UnitCapstoneUnlocksTheAuthoredTemplateChainInOrder()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("unit-capstone"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            reported.Add);
        var controls = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var byId = controls
            .Where(control => AutomationProperties.GetAutomationId(control) is not null)
            .ToDictionary(
                control => AutomationProperties.GetAutomationId(control)!,
                StringComparer.Ordinal);

        Assert.IsTrue(byId["UnitCapstoneStep_notice-item"].IsEnabled);
        Assert.IsFalse(byId["UnitCapstoneStep_build-request"].IsEnabled);
        Assert.IsFalse(byId["UnitCapstoneStep_answer-worker"].IsEnabled);
        byId["UnitCapstoneStep_notice-item"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.IsFalse(byId["UnitCapstoneStep_notice-item"].IsEnabled);
        Assert.IsTrue(byId["UnitCapstoneStep_build-request"].IsEnabled);
        byId["UnitCapstoneStep_build-request"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.IsTrue(byId["UnitCapstoneStep_answer-worker"].IsEnabled);
        byId["UnitCapstoneStep_answer-worker"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.HasCount(3, reported);
        Assert.AreEqual(TemplateOutcomeState.Uncertain, reported[0].State);
        Assert.AreEqual("notice-item", reported[0].ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Uncertain, reported[1].State);
        Assert.AreEqual("build-request", reported[1].ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[2].State);
        Assert.AreEqual("answer-worker", reported[2].ResponseId);
        CollectionAssert.AreEqual(
            new[] { "notice-item", "build-request", "answer-worker" },
            reported[2].OrderedOptionIds!.ToArray());
        StringAssert.Contains(
            AutomationProperties.GetName(byId["UnitCapstoneGoal"]),
            "Order one café drink politely");
        StringAssert.Contains(
            AutomationProperties.GetName(byId["UnitCapstoneCard_notice-item"]),
            "object-spotlight");
        Assert.IsTrue(byId.ContainsKey("UnitCapstoneRoute"));
        Assert.IsTrue(byId.ContainsKey("UnitCapstoneReplay"));
        Assert.IsTrue(byId.ContainsKey("UnitCapstoneSkip"));
        Assert.IsTrue(byId.ContainsKey("UnitCapstoneTextEquivalent"));
        Assert.IsTrue(byId.ContainsKey("UnitCapstoneStatus"));
    }

    [TestMethod]
    public void ProgressShelfPreservesProjectedStatusesAndReportsStableCapabilityIds()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("progress-shelf"));
        var reported = new List<TemplateOutcome>();
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            fixture.Parameters,
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            reported.Add);
        var controls = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var byId = controls
            .Where(control => AutomationProperties.GetAutomationId(control) is not null)
            .ToDictionary(
                control => AutomationProperties.GetAutomationId(control)!,
                StringComparer.Ordinal);

        byId["ProgressShelfCapability_order-drink"].RaiseEvent(
            new RoutedEventArgs(Button.ClickEvent));
        byId["ProgressShelfCapability_repair-request"].RaiseEvent(
            new RoutedEventArgs(Button.ClickEvent));
        byId["ProgressShelfCapability_ask-directions"].RaiseEvent(
            new RoutedEventArgs(Button.ClickEvent));

        Assert.HasCount(3, reported);
        Assert.AreEqual(TemplateOutcomeState.Success, reported[0].State);
        Assert.AreEqual("order-drink", reported[0].ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Uncertain, reported[1].State);
        Assert.AreEqual("repair-request", reported[1].ResponseId);
        Assert.AreEqual(TemplateOutcomeState.Ready, reported[2].State);
        Assert.AreEqual("ask-directions", reported[2].ResponseId);
        StringAssert.Contains(
            ((TextBlock)byId["ProgressShelfSelectionStatus"]).Text,
            "Ask where a destination is");
        StringAssert.Contains(
            AutomationProperties.GetName(byId["ProgressShelfMethod"]),
            "deterministic task evidence");
        Assert.IsTrue(byId.ContainsKey("ProgressShelfObject_order-drink"));
        Assert.IsTrue(byId.ContainsKey("ProgressShelfObjects"));
        Assert.IsTrue(byId.ContainsKey("ProgressShelfBoard"));
        Assert.IsTrue(byId.ContainsKey("ProgressShelfReplay"));
        Assert.IsTrue(byId.ContainsKey("ProgressShelfSkip"));
        Assert.IsTrue(byId.ContainsKey("ProgressShelfTextEquivalent"));

        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Linguistics.App",
            "Features",
            "Learn",
            "Templates",
            "ReviewProgressRenderers.cs"));
        var shelfSource = source[source.IndexOf(
            "internal static class ProgressShelfRenderer",
            StringComparison.Ordinal)..];
        Assert.IsFalse(shelfSource.Contains("XP", StringComparison.Ordinal));
        Assert.IsFalse(shelfSource.Contains("streak", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ProgressShelfRendersAnAuthoredEmptyState()
    {
        var fixture = TemplateGalleryFixtures.All.Single(candidate =>
            candidate.TemplateId == new TemplateId("progress-shelf"));
        var emptyValues = fixture.Parameters.Values
            .Where(pair => pair.Key is not "demonstrated" and not "practicing" and not "not-started")
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var rendered = TemplateRegistry.CreateDefault().Render(
            fixture.TemplateId,
            new ResolvedTemplateParameters(emptyValues, UseTextOnlyFallback: true),
            fixture.InstructionLanguage,
            shouldReduceMotion: true,
            _ => Assert.Fail("Rendering the empty shelf must not report an outcome."));
        var controls = rendered.GetLogicalDescendants().OfType<Control>().ToArray();
        var empty = controls.Single(control =>
            AutomationProperties.GetAutomationId(control) == "ProgressShelfEmpty");

        StringAssert.Contains(AutomationProperties.GetName(empty), "No capability evidence");
        Assert.IsFalse(controls.Any(control =>
            AutomationProperties.GetAutomationId(control)?.StartsWith(
                "ProgressShelfCapability_",
                StringComparison.Ordinal) == true));
    }

    [TestMethod]
    public void TemplateSourcesContainNoEmDash()
    {
        var templatesDirectory = Path.Combine(
            RepositoryRoot,
            "src",
            "Linguistics.App",
            "Features",
            "Learn",
            "Templates");
        foreach (var path in Directory.EnumerateFiles(templatesDirectory))
        {
            var source = File.ReadAllText(path);
            Assert.DoesNotContain("—", source, StringComparison.Ordinal, Path.GetFileName(path));
        }
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../"));

    private sealed class RecordingSpeechProvider : ISpeechSynthesisProvider
    {
        public SpeechSynthesisRequest? LastRequest { get; private set; }

        public List<SpeechSynthesisRequest> Requests { get; } = [];

        public Task<SpeechSynthesisSnapshot> InspectAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SpeechSynthesisSnapshot(
                SpeechCapabilityStatus.Available,
                [new SpeechVoice("de-test", "German test voice", "de-DE", new LanguageCode("de"))],
                "Local test voice available."));

        public Task<SpeechSynthesisResult> SpeakAsync(
            SpeechSynthesisRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            Requests.Add(request);
            return Task.FromResult(new SpeechSynthesisResult(
                request.RequestId,
                SpeechSynthesisResultStatus.Completed,
                "de-test",
                TimeSpan.Zero,
                "Speech playback completed locally."));
        }

        public Task StopAsync() => Task.CompletedTask;

        public void Dispose()
        {
        }
    }

    private sealed class RecordingRecognitionProvider : ISpeechRecognitionProvider
    {
        public int RequestCount { get; private set; }

        public SpeechRecognitionRequest? LastRequest { get; private set; }

        public Task<SpeechRecognitionSnapshot> InspectAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new SpeechRecognitionSnapshot(
                SpeechCapabilityStatus.Available,
                new SpeechModelDescriptor(
                    "test-model",
                    1,
                    "local fixture",
                    "MIT",
                    "test-recognition-v1"),
                "Local recognition is available."));

        public Task<SpeechRecognitionResult> RecognizeAsync(
            SpeechRecognitionRequest request,
            CancellationToken cancellationToken = default)
        {
            RequestCount++;
            LastRequest = request;
            return Task.FromResult(new SpeechRecognitionResult(
                request.RequestId,
                SpeechRecognitionResultStatus.Accepted,
                "Ich möchte einen Tee, bitte.",
                request.Language,
                TimeSpan.FromSeconds(2),
                "test-recognition-v1",
                "test-model",
                "Local transcript ready."));
        }

        public void Dispose()
        {
        }
    }
}
