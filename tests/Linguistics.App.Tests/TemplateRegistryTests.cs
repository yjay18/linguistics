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
