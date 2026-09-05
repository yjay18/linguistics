using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Linguistics.App.Content;
using Linguistics.App.Controls;
using Linguistics.App.Motion;
using Linguistics.Core.Content;
using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.App.Features.Learn.Templates;

internal static class SceneEstablishRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var location = TemplateRendering.Text(parameters, "location");
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var cast = TemplateRendering.Options(parameters, "cast");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");

        var replayButton = new Button { Content = "Replay entrance", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "SceneEstablishReplay");
        AutomationProperties.SetName(replayButton, "Replay the scene entrance");
        var skipButton = new Button { Content = "Skip entrance", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "SceneEstablishSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed scene");

        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Scene instruction. {instruction}");
        var sceneActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
        };
        sceneActions.Children.Add(replayButton);
        sceneActions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(sceneActions, 1);
        header.Children.Add(sceneActions);

        var stage = TemplateRendering.CreateStage(300, $"Opening scene at {location}");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);

        var locationTape = new PaperTape { Content = location.ToUpperInvariant(), Angle = -1.2 };
        PaperStage.SetLayer(locationTape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(locationTape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(locationTape, 0.22);
        PaperStage.SetAnchorOffsetY(locationTape, -10);
        stage.Children.Add(locationTape);

        var castPanel = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        AutomationProperties.SetName(castPanel, $"Scene cast: {string.Join(", ", cast.Select(member => member.Label))}");
        var isCrowdedCast = cast.Count >= 4;
        foreach (var (member, index) in cast.Select((member, index) => (member, index)))
        {
            var name = new TextBlock
            {
                Text = member.Label,
                FontSize = isCrowdedCast ? 16 : 18,
                FontWeight = FontWeight.Bold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var cutout = new CutoutFrame
            {
                Width = isCrowdedCast ? 122 : 138,
                Height = 122,
                Margin = new Thickness(isCrowdedCast ? 8 : 12, 4),
                Content = name,
                RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
            };
            cutout.Classes.Add(index % 2 == 0 ? "tilt-left" : "tilt-right");
            AutomationProperties.SetName(cutout, $"Cast member {member.Label}");
            castPanel.Children.Add(cutout);
        }

        PaperStage.SetLayer(castPanel, PaperStageLayer.SupportingCast);
        PaperStage.SetAnchor(castPanel, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(castPanel, isCrowdedCast ? 0.5 : 0.58);
        PaperStage.SetAnchorOffsetY(castPanel, -4);
        stage.Children.Add(castPanel);

        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var continueButton = new Button
        {
            Content = "Begin the story",
            Classes = { "primary", "lift" },
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        AutomationProperties.SetAutomationId(continueButton, "SceneEstablishContinue");
        AutomationProperties.SetName(continueButton, "Confirm the opening scene is understood");
        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        footer.Children.Add(outcomePanel);
        Grid.SetColumn(continueButton, 1);
        footer.Children.Add(continueButton);

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = $"Text-only scene: {location}. Cast: {string.Join(", ", cast.Select(member => member.Label))}.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(stage);
        if (!parameters.UseTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                [backdropRendered ? backdropReference : null],
                "SceneEstablishImageCredits") is { } credits)
        {
            root.Children.Add(credits);
        }

        root.Children.Add(footer);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, locationTape, castPanel);
            if (!shouldReduceMotion)
            {
                castPanel.RenderTransform = TemplateRendering.Transform(-56, 6, -1.8, 0.96);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(250), locationTape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(900), castPanel, 0, 0, 0, 1),
            ]);
            await scene.PlayAsync(shouldReduceMotion);
        }

        root.AttachedToVisualTree += async (_, _) => await PlayAsync();
        root.DetachedFromVisualTree += (_, _) =>
        {
            scene?.Skip();
            scene?.Dispose();
            scene = null;
        };
        skipButton.Click += (_, _) =>
        {
            scene?.Skip();
            locationTape.SkipEntrance();
        };
        replayButton.Click += async (_, _) => await PlayAsync();
        continueButton.Click += (_, _) =>
        {
            var outcome = TemplateInteractionEvaluator.EvaluateAcknowledgement(acknowledged: true);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            reportOutcome(outcome);
        };
        return root;
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Scene set. The place and cast are ready.",
        TemplateOutcomeState.Uncertain => "Pause on the location and cast before the story begins.",
        TemplateOutcomeState.Failure => "Replay the entrance and read each cast label.",
        _ => "Ready: meet the place and cast before continuing.",
    };
}

internal static class ObjectAnatomyRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var title = TemplateRendering.Text(parameters, "title");
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var parts = TemplateRendering.Options(parameters, "parts");
        var assetReference = TemplateRendering.AssetReference(parameters, "asset");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var subjectImage = parameters.UseTextOnlyFallback
            ? null
            : TemplateRendering.CreateContentImage(imageCache, assetReference, 142);

        var stage = TemplateRendering.CreateStage(308, $"Object anatomy for {title}");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var subject = new CutoutFrame
        {
            Width = 228,
            Height = 186,
            Content = subjectImage as Control ?? new TextBlock
            {
                Text = title,
                FontSize = 29,
                FontWeight = FontWeight.Bold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
            RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        };
        subject.Classes.Add("tilt-left");
        PaperStage.SetLayer(subject, PaperStageLayer.Subject);
        PaperStage.SetAnchor(subject, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(subject, 0.38);
        PaperStage.SetAnchorOffsetY(subject, -2);
        stage.Children.Add(subject);

        var labelControls = new List<Control>();
        var anchors = new[]
        {
            PaperAnchorLine.Shoulder,
            PaperAnchorLine.Waist,
            PaperAnchorLine.Foot,
            PaperAnchorLine.Head,
        };
        foreach (var (part, index) in parts.Take(anchors.Length).Select((part, index) => (part, index)))
        {
            var label = new PaperTape
            {
                Content = part.Label,
                Angle = index % 2 == 0 ? 1.1 : -1.3,
            };
            PaperStage.SetLayer(label, PaperStageLayer.VerdictCard);
            PaperStage.SetAnchor(label, anchors[index]);
            PaperStage.SetAnchorX(label, 0.72);
            PaperStage.SetAnchorOffsetY(label, index == 2 ? -44 : 0);
            AutomationProperties.SetName(label, $"Part label {part.Label}");
            stage.Children.Add(label);
            labelControls.Add(label);
        }

        var animatedControls = new[] { subject }.Concat(labelControls).ToArray();
        var textOnlyCopy = $"Text-only object: {title}. Parts: {string.Join(", ", parts.Select(part => part.Label))}.";
        return SceneStoryPresentation.Compose(
            "ObjectAnatomy",
            instruction,
            stage,
            imageCache,
            [subjectImage is not null ? assetReference : null, backdropRendered ? backdropReference : null],
            parameters.UseTextOnlyFallback,
            textOnlyCopy,
            parameters.PreviewOutcome,
            shouldReduceMotion,
            OutcomeCopy,
            animatedControls,
            () => subject.RenderTransform = TemplateRendering.Transform(-26, 5, -3, 0.96),
            () =>
            {
                var steps = new List<PaperChoreographyStep>
                {
                    TemplateRendering.Move(TimeSpan.FromMilliseconds(550), subject, 0, 0, -1.2, 1),
                };
                steps.AddRange(labelControls.Select(label =>
                    TemplateRendering.Reveal(TimeSpan.FromMilliseconds(260), label)));
                return steps;
            },
            () =>
            {
                foreach (var label in labelControls.OfType<PaperTape>())
                {
                    label.SkipEntrance();
                }
            },
            reportOutcome,
            "Parts noticed");
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Parts noticed. The labels stay with the complete object.",
        TemplateOutcomeState.Uncertain => "Pause on each label before continuing.",
        TemplateOutcomeState.Failure => "Replay the labels and read them from top to bottom.",
        _ => "Ready: let each part label settle beside the object.",
    };
}

internal static class PaperDialogueRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ISpeechSynthesisProvider? speechSynthesisProvider,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var speakerOne = TemplateRendering.Text(parameters, "speaker-one");
        var lineOne = TemplateRendering.Text(parameters, "line-one");
        var speakerTwo = TemplateRendering.Text(parameters, "speaker-two");
        var lineTwo = TemplateRendering.Text(parameters, "line-two");
        var speechLanguageText = TemplateRendering.Text(parameters, "speech-language");
        LanguageCode? speechLanguage = null;
        try
        {
            speechLanguage = new LanguageCode(speechLanguageText);
        }
        catch (ArgumentException)
        {
            // Captions remain the complete fallback when authored speech metadata is unusable.
        }

        var speakerOneAsset = TemplateRendering.AssetReference(parameters, "speaker-one-asset");
        var speakerTwoAsset = TemplateRendering.AssetReference(parameters, "speaker-two-asset");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var firstImage = parameters.UseTextOnlyFallback
            ? null
            : TemplateRendering.CreateContentImage(imageCache, speakerOneAsset, 88);
        var secondImage = parameters.UseTextOnlyFallback
            ? null
            : TemplateRendering.CreateContentImage(imageCache, speakerTwoAsset, 88);

        var stage = TemplateRendering.CreateStage(320, $"Dialogue between {speakerOne} and {speakerTwo}");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);
        var firstPuppet = CreatePuppet(speakerOne, firstImage, tiltLeft: true);
        PaperStage.SetLayer(firstPuppet, PaperStageLayer.Subject);
        PaperStage.SetAnchor(firstPuppet, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(firstPuppet, 0.24);
        stage.Children.Add(firstPuppet);

        var secondPuppet = CreatePuppet(speakerTwo, secondImage, tiltLeft: false);
        PaperStage.SetLayer(secondPuppet, PaperStageLayer.Subject);
        PaperStage.SetAnchor(secondPuppet, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(secondPuppet, 0.76);
        stage.Children.Add(secondPuppet);

        var firstBubble = CreateBubble(speakerOne, lineOne, angle: -1.1);
        PaperStage.SetLayer(firstBubble, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(firstBubble, PaperAnchorLine.Shoulder);
        PaperStage.SetAnchorX(firstBubble, 0.35);
        PaperStage.SetAnchorOffsetY(firstBubble, -34);
        stage.Children.Add(firstBubble);

        var secondBubble = CreateBubble(speakerTwo, lineTwo, angle: 1.2);
        PaperStage.SetLayer(secondBubble, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(secondBubble, PaperAnchorLine.Shoulder);
        PaperStage.SetAnchorX(secondBubble, 0.65);
        PaperStage.SetAnchorOffsetY(secondBubble, 36);
        stage.Children.Add(secondBubble);

        var captionStatus = new PaperTape
        {
            Content = "CAPTIONS ON",
            Angle = -2,
        };
        AutomationProperties.SetName(
            captionStatus,
            "Complete captions are visible. Optional local text to speech may be available below.");
        PaperStage.SetLayer(captionStatus, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(captionStatus, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(captionStatus, 0.5);
        PaperStage.SetAnchorOffsetY(captionStatus, -48);
        stage.Children.Add(captionStatus);

        var animatedControls = new Control[]
        {
            firstPuppet,
            firstBubble,
            secondPuppet,
            secondBubble,
            captionStatus,
        };
        var playFirstButton = new Button
        {
            Content = $"Play {speakerOne}",
            Classes = { "quiet" },
            IsEnabled = speechSynthesisProvider is not null && speechLanguage is not null,
        };
        AutomationProperties.SetAutomationId(playFirstButton, "PaperDialoguePlaySpeakerOne");
        AutomationProperties.SetName(playFirstButton, $"Play {speakerOne}'s caption with local system speech");
        var playSecondButton = new Button
        {
            Content = $"Play {speakerTwo}",
            Classes = { "quiet" },
            IsEnabled = speechSynthesisProvider is not null && speechLanguage is not null,
        };
        AutomationProperties.SetAutomationId(playSecondButton, "PaperDialoguePlaySpeakerTwo");
        AutomationProperties.SetName(playSecondButton, $"Play {speakerTwo}'s caption with local system speech");
        var playbackStatus = new TextBlock
        {
            Text = playFirstButton.IsEnabled
                ? "Optional local playback uses no microphone. Captions remain complete."
                : "Local playback is unavailable. Captions remain complete.",
            Classes = { "muted" },
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetAutomationId(playbackStatus, "PaperDialoguePlaybackStatus");
        AutomationProperties.SetLiveSetting(playbackStatus, AutomationLiveSetting.Polite);
        var playbackActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
        };
        playbackActions.Children.Add(playFirstButton);
        playbackActions.Children.Add(playSecondButton);
        var playbackGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        playbackGrid.Children.Add(playbackStatus);
        Grid.SetColumn(playbackActions, 1);
        playbackGrid.Children.Add(playbackActions);
        var playbackPanel = new PaperCard
        {
            Padding = new Thickness(12, 8),
            Content = playbackGrid,
        };
        playbackPanel.Classes.Add("soft");
        AutomationProperties.SetName(
            playbackPanel,
            "Optional local speech controls. Captions remain complete. No microphone is used.");

        var availabilityCancellation = new CancellationTokenSource();
        playbackPanel.AttachedToVisualTree += async (_, _) =>
        {
            if (speechSynthesisProvider is null || speechLanguage is not { } language)
            {
                return;
            }

            try
            {
                var snapshot = await speechSynthesisProvider.InspectAsync(availabilityCancellation.Token);
                var hasMatchingVoice = snapshot.Status == SpeechCapabilityStatus.Available &&
                                       snapshot.Voices.Any(voice => voice.Language == language);
                playFirstButton.IsEnabled = hasMatchingVoice;
                playSecondButton.IsEnabled = hasMatchingVoice;
                playbackStatus.Text = hasMatchingVoice
                    ? "Optional local playback uses no microphone. Captions remain complete."
                    : "No matching local system voice is installed. Captions remain complete.";
            }
            catch (OperationCanceledException)
            {
            }
        };

        CancellationTokenSource? playbackCancellation = null;
        async Task PlayLineAsync(string speaker, string line, string seed)
        {
            if (speechSynthesisProvider is null || speechLanguage is not { } language)
            {
                playbackStatus.Text = "Local playback is unavailable. Captions remain complete.";
                return;
            }

            playbackCancellation?.Cancel();
            playbackCancellation?.Dispose();
            playbackCancellation = new CancellationTokenSource();
            playFirstButton.IsEnabled = false;
            playSecondButton.IsEnabled = false;
            playbackStatus.Text = $"Playing {speaker}'s caption locally.";
            try
            {
                var result = await speechSynthesisProvider.SpeakAsync(
                    new SpeechSynthesisRequest(
                        Guid.NewGuid(),
                        line,
                        language,
                        seed),
                    playbackCancellation.Token);
                playbackStatus.Text = result.Message;
            }
            catch (OperationCanceledException)
            {
                playbackStatus.Text = "Speech playback stopped. Captions remain complete.";
            }
            finally
            {
                playFirstButton.IsEnabled = true;
                playSecondButton.IsEnabled = true;
            }
        }

        playFirstButton.Click += async (_, _) =>
            await PlayLineAsync(speakerOne, lineOne, $"paper-dialogue:{speakerOne}:1");
        playSecondButton.Click += async (_, _) =>
            await PlayLineAsync(speakerTwo, lineTwo, $"paper-dialogue:{speakerTwo}:2");
        playbackPanel.DetachedFromVisualTree += (_, _) =>
        {
            availabilityCancellation.Cancel();
            availabilityCancellation.Dispose();
            playbackCancellation?.Cancel();
            playbackCancellation?.Dispose();
            playbackCancellation = null;
        };

        return SceneStoryPresentation.Compose(
            "PaperDialogue",
            instruction,
            stage,
            imageCache,
            [
                firstImage is not null ? speakerOneAsset : null,
                secondImage is not null ? speakerTwoAsset : null,
                backdropRendered ? backdropReference : null,
            ],
            parameters.UseTextOnlyFallback,
            $"Text-only dialogue. {speakerOne}: {lineOne} {speakerTwo}: {lineTwo}",
            parameters.PreviewOutcome,
            shouldReduceMotion,
            OutcomeCopy,
            animatedControls,
            () =>
            {
                firstPuppet.RenderTransform = TemplateRendering.Transform(-34, 4, -3, 0.96);
                secondPuppet.RenderTransform = TemplateRendering.Transform(34, 4, 3, 0.96);
            },
            () =>
            [
                TemplateRendering.Move(TimeSpan.FromMilliseconds(420), firstPuppet, 0, 0, -1.2, 1),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(340), firstBubble),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(420), secondPuppet, 0, 0, 1.2, 1),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(340), secondBubble, captionStatus),
            ],
            () => captionStatus.SkipEntrance(),
            reportOutcome,
            "Dialogue noticed",
            playbackPanel);
    }

    private static CutoutFrame CreatePuppet(string speaker, Image? image, bool tiltLeft)
    {
        var content = new StackPanel
        {
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (image is not null)
        {
            content.Children.Add(image);
        }

        content.Children.Add(new TextBlock
        {
            Text = speaker,
            FontWeight = FontWeight.Bold,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        var puppet = new CutoutFrame
        {
            Width = 150,
            Height = 132,
            Content = content,
            RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        };
        puppet.Classes.Add(tiltLeft ? "tilt-left" : "tilt-right");
        AutomationProperties.SetName(puppet, $"Speaker {speaker}");
        return puppet;
    }

    private static PaperCard CreateBubble(string speaker, string line, double angle)
    {
        var copy = new StackPanel { Spacing = 4 };
        copy.Children.Add(new TextBlock
        {
            Text = speaker,
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Classes = { "muted" },
        });
        copy.Children.Add(new TextBlock
        {
            Text = line,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
        var bubble = new PaperCard
        {
            Width = 218,
            Padding = new Thickness(14, 10),
            Content = copy,
        };
        PaperStage.SetLayerTransform(bubble, TemplateRendering.Transform(0, 0, angle, 1));
        AutomationProperties.SetName(bubble, $"{speaker} says: {line}");
        return bubble;
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Dialogue noticed. Both complete captions remain visible.",
        TemplateOutcomeState.Uncertain => "Read both captions before continuing.",
        TemplateOutcomeState.Failure => "Replay the exchange and follow each speaker label.",
        _ => "Ready: follow the two captioned turns.",
    };
}

internal static class StreetWalkRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var subjectName = TemplateRendering.Text(parameters, "subject");
        var route = TemplateRendering.Options(parameters, "route");
        var subjectAsset = TemplateRendering.AssetReference(parameters, "subject-asset");
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var subjectImage = parameters.UseTextOnlyFallback
            ? null
            : TemplateRendering.CreateContentImage(imageCache, subjectAsset, 102);

        var stage = TemplateRendering.CreateStage(314, $"Street walk for {subjectName}");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);

        var routePanel = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(routePanel, $"Route: {string.Join(", ", route.Select(stop => stop.Label))}");
        foreach (var (stop, index) in route.Take(4).Select((stop, index) => (stop, index)))
        {
            var sign = new PaperTape
            {
                Content = stop.Label,
                Angle = index % 2 == 0 ? -1.1 : 1.2,
                Margin = new Thickness(8),
            };
            AutomationProperties.SetName(sign, $"Route stop {index + 1}: {stop.Label}");
            routePanel.Children.Add(sign);
        }

        PaperStage.SetLayer(routePanel, PaperStageLayer.SupportingCast);
        PaperStage.SetAnchor(routePanel, PaperAnchorLine.Shoulder);
        PaperStage.SetAnchorX(routePanel, 0.5);
        PaperStage.SetAnchorOffsetY(routePanel, -14);
        stage.Children.Add(routePanel);

        var subjectContent = new StackPanel
        {
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        if (subjectImage is not null)
        {
            subjectContent.Children.Add(subjectImage);
        }

        subjectContent.Children.Add(new TextBlock
        {
            Text = subjectName,
            FontWeight = FontWeight.Bold,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        var walker = new CutoutFrame
        {
            Width = 138,
            Height = 132,
            Content = subjectContent,
            RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        };
        walker.Classes.Add("tilt-left");
        AutomationProperties.SetName(walker, $"Walker {subjectName}");
        PaperStage.SetLayer(walker, PaperStageLayer.Subject);
        PaperStage.SetAnchor(walker, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(walker, 0.44);
        stage.Children.Add(walker);

        var foregroundLeft = new TornEdge { Width = 260 };
        PaperStage.SetLayer(foregroundLeft, PaperStageLayer.ForegroundSilhouettes);
        PaperStage.SetAnchor(foregroundLeft, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(foregroundLeft, 0.16);
        PaperStage.SetAnchorOffsetY(foregroundLeft, 6);
        stage.Children.Add(foregroundLeft);
        var foregroundRight = new TornEdge { Width = 280 };
        foregroundRight.Classes.Add("bottom");
        PaperStage.SetLayer(foregroundRight, PaperStageLayer.ForegroundSilhouettes);
        PaperStage.SetAnchor(foregroundRight, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(foregroundRight, 0.84);
        PaperStage.SetAnchorOffsetY(foregroundRight, 8);
        stage.Children.Add(foregroundRight);

        var animatedControls = new Control[] { routePanel, walker };
        return SceneStoryPresentation.Compose(
            "StreetWalk",
            instruction,
            stage,
            imageCache,
            [subjectImage is not null ? subjectAsset : null, backdropRendered ? backdropReference : null],
            parameters.UseTextOnlyFallback,
            $"Text-only route for {subjectName}: {string.Join(", ", route.Select(stop => stop.Label))}.",
            parameters.PreviewOutcome,
            shouldReduceMotion,
            OutcomeCopy,
            animatedControls,
            () =>
            {
                routePanel.RenderTransform = TemplateRendering.Transform(0, -8, 0, 0.98);
                walker.RenderTransform = TemplateRendering.Transform(-180, 5, -3, 0.96);
            },
            () =>
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(360), routePanel),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(1_650), walker, 138, 0, 1.2, 1),
            ],
            () =>
            {
                foreach (var sign in routePanel.Children.OfType<PaperTape>())
                {
                    sign.SkipEntrance();
                }
            },
            reportOutcome,
            "Route noticed");
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Route noticed. The labeled places stay in walking order.",
        TemplateOutcomeState.Uncertain => "Pause on the three route labels before continuing.",
        TemplateOutcomeState.Failure => "Replay the walk and read each place from left to right.",
        _ => "Ready: follow the walker past each labeled place.",
    };
}

internal static class PostcardStoryRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var frontTitle = TemplateRendering.Text(parameters, "front-title");
        var frontCaption = TemplateRendering.Localized(parameters, "front-caption", instructionLanguage);
        var backTitle = TemplateRendering.Text(parameters, "back-title");
        var backBody = TemplateRendering.Localized(parameters, "back-body", instructionLanguage);
        var frontAsset = TemplateRendering.AssetReference(parameters, "front-asset");
        var backAsset = TemplateRendering.AssetReference(parameters, "back-asset");
        var frontImage = parameters.UseTextOnlyFallback
            ? null
            : TemplateRendering.CreateContentImage(imageCache, frontAsset, 116, Stretch.UniformToFill);
        var backImage = parameters.UseTextOnlyFallback
            ? null
            : TemplateRendering.CreateContentImage(imageCache, backAsset, 116, Stretch.UniformToFill);

        var stage = TemplateRendering.CreateStage(318, $"Two-sided postcard: {frontTitle}");
        TemplateRendering.AddBackdrop(stage, imageCache, assetReferenceId: null);
        var frontCard = CreateSide(frontTitle, frontCaption, frontImage, tilt: -1.2);
        PaperStage.SetLayer(frontCard, PaperStageLayer.Subject);
        PaperStage.SetAnchor(frontCard, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(frontCard, 0.5);
        PaperStage.SetAnchorOffsetY(frontCard, -50);
        stage.Children.Add(frontCard);

        var backCard = CreateSide(backTitle, backBody, backImage, tilt: 1.1);
        backCard.IsVisible = false;
        PaperStage.SetLayer(backCard, PaperStageLayer.Subject);
        PaperStage.SetAnchor(backCard, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(backCard, 0.5);
        PaperStage.SetAnchorOffsetY(backCard, -50);
        stage.Children.Add(backCard);

        var sideStatus = new TextBlock
        {
            Text = "Postcard front visible",
            Classes = { "muted" },
            FontSize = 12,
            TextAlignment = TextAlignment.Center,
        };
        AutomationProperties.SetLiveSetting(sideStatus, AutomationLiveSetting.Polite);
        AutomationProperties.SetAutomationId(sideStatus, "PostcardStorySideStatus");
        PaperStage.SetLayer(sideStatus, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(sideStatus, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(sideStatus, 0.38);
        PaperStage.SetAnchorOffsetY(sideStatus, -10);
        stage.Children.Add(sideStatus);

        var turnButton = new Button
        {
            Content = "Turn postcard over",
            Classes = { "quiet", "lift" },
        };
        AutomationProperties.SetAutomationId(turnButton, "PostcardStoryTurn");
        AutomationProperties.SetName(turnButton, "Show the back of the postcard");
        PaperStage.SetLayer(turnButton, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(turnButton, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(turnButton, 0.65);
        PaperStage.SetAnchorOffsetY(turnButton, -8);
        stage.Children.Add(turnButton);

        var showingBack = false;
        turnButton.Click += (_, _) =>
        {
            showingBack = !showingBack;
            frontCard.IsVisible = !showingBack;
            backCard.IsVisible = showingBack;
            turnButton.Content = showingBack ? "Show postcard front" : "Turn postcard over";
            AutomationProperties.SetName(
                turnButton,
                showingBack ? "Show the front of the postcard" : "Show the back of the postcard");
            sideStatus.Text = showingBack ? "Postcard back visible" : "Postcard front visible";
        };

        return SceneStoryPresentation.Compose(
            "PostcardStory",
            instruction,
            stage,
            imageCache,
            [frontImage is not null ? frontAsset : null, backImage is not null ? backAsset : null],
            parameters.UseTextOnlyFallback,
            $"Text-only postcard.\nFront: {frontTitle}.\n{frontCaption}\nBack: {backTitle}.\n{backBody}",
            parameters.PreviewOutcome,
            shouldReduceMotion,
            OutcomeCopy,
            [frontCard, sideStatus, turnButton],
            () => frontCard.RenderTransform = TemplateRendering.Transform(0, 12, -4, 0.92),
            () =>
            [
                TemplateRendering.Move(TimeSpan.FromMilliseconds(650), frontCard, 0, 0, -1.2, 1),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(260), sideStatus, turnButton),
            ],
            static () => { },
            reportOutcome,
            "Postcard read");
    }

    private static PaperCard CreateSide(string title, string body, Image? image, double tilt)
    {
        var copy = new StackPanel { Spacing = 8 };
        if (image is not null)
        {
            copy.Children.Add(image);
        }

        copy.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 22,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 2,
        });
        copy.Children.Add(new TextBlock
        {
            Text = body,
            FontSize = 15,
            TextWrapping = TextWrapping.Wrap,
            Classes = { "muted" },
        });
        var card = new PaperCard
        {
            Width = 520,
            Height = 226,
            Padding = new Thickness(18, 14),
            Content = copy,
        };
        PaperStage.SetLayerTransform(card, TemplateRendering.Transform(0, 0, tilt, 1));
        AutomationProperties.SetName(card, $"Postcard side. {title}. {body}");
        return card;
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Postcard read. Both authored sides remain available.",
        TemplateOutcomeState.Uncertain => "Turn the postcard and read both sides before continuing.",
        TemplateOutcomeState.Failure => "Return to the front, then turn the card and read again.",
        _ => "Ready: read the front, then turn the postcard over.",
    };
}

internal static class PhotoAlbumRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var title = TemplateRendering.Text(parameters, "title");
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var pages = TemplateRendering.Options(parameters, "pages");
        var stage = TemplateRendering.CreateStage(324, $"Photo album: {title}");
        TemplateRendering.AddBackdrop(stage, imageCache, assetReferenceId: null);

        var albumTitle = new PaperTape { Content = title.ToUpperInvariant(), Angle = -1.2 };
        PaperStage.SetLayer(albumTitle, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(albumTitle, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(albumTitle, 0.23);
        PaperStage.SetAnchorOffsetY(albumTitle, -10);
        stage.Children.Add(albumTitle);

        var pageCards = new List<CutoutFrame>();
        var renderedAssetIds = new List<string?>();
        foreach (var (page, index) in pages.Select((page, index) => (page, index)))
        {
            var image = parameters.UseTextOnlyFallback
                ? null
                : TemplateRendering.CreateContentImage(
                    imageCache,
                    page.AssetReferenceId,
                    132,
                    Stretch.UniformToFill);
            if (image is not null)
            {
                renderedAssetIds.Add(page.AssetReferenceId);
            }

            var pageCopy = new StackPanel
            {
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
            };
            if (image is not null)
            {
                pageCopy.Children.Add(image);
            }

            pageCopy.Children.Add(new TextBlock
            {
                Text = page.Label,
                FontSize = image is null ? 28 : 19,
                FontWeight = FontWeight.Bold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            var card = new CutoutFrame
            {
                Width = 490,
                Height = 210,
                Content = pageCopy,
                IsVisible = index == 0,
            };
            card.Classes.Add(index % 2 == 0 ? "tilt-left" : "tilt-right");
            AutomationProperties.SetName(card, $"Album page {index + 1} of {pages.Count}: {page.Label}");
            AutomationProperties.SetAutomationId(card, $"PhotoAlbumPage_{page.Id}");
            PaperStage.SetLayer(card, PaperStageLayer.Subject);
            PaperStage.SetAnchor(card, PaperAnchorLine.Waist);
            PaperStage.SetAnchorX(card, 0.5);
            PaperStage.SetAnchorOffsetY(card, -44);
            stage.Children.Add(card);
            pageCards.Add(card);
        }

        var pageStatus = new TextBlock
        {
            Text = $"Page 1 of {pages.Count}",
            Classes = { "muted" },
            FontSize = 12,
            TextAlignment = TextAlignment.Center,
        };
        AutomationProperties.SetAutomationId(pageStatus, "PhotoAlbumPageStatus");
        AutomationProperties.SetLiveSetting(pageStatus, AutomationLiveSetting.Polite);
        var previousButton = new Button { Content = "Previous page", Classes = { "quiet" }, IsEnabled = false };
        AutomationProperties.SetAutomationId(previousButton, "PhotoAlbumPrevious");
        var nextButton = new Button { Content = "Next page", Classes = { "quiet", "lift" } };
        AutomationProperties.SetAutomationId(nextButton, "PhotoAlbumNext");
        var navigation = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 8,
        };
        navigation.Children.Add(previousButton);
        navigation.Children.Add(pageStatus);
        navigation.Children.Add(nextButton);
        var navigationCard = new PaperCard
        {
            Padding = new Thickness(8, 4),
            HorizontalAlignment = HorizontalAlignment.Center,
            Content = navigation,
        };
        navigationCard.Classes.Add("soft");
        AutomationProperties.SetName(navigationCard, "Album page navigation");
        PaperStage.SetLayer(navigationCard, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(navigationCard, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(navigationCard, 0.5);
        PaperStage.SetAnchorOffsetY(navigationCard, -10);
        stage.Children.Add(navigationCard);

        var pageIndex = 0;
        void ShowPage(int requestedIndex)
        {
            pageIndex = Math.Clamp(requestedIndex, 0, pageCards.Count - 1);
            for (var index = 0; index < pageCards.Count; index++)
            {
                pageCards[index].IsVisible = index == pageIndex;
            }

            previousButton.IsEnabled = pageIndex > 0;
            nextButton.IsEnabled = pageIndex < pageCards.Count - 1;
            pageStatus.Text = $"Page {pageIndex + 1} of {pageCards.Count}: {pages[pageIndex].Label}";
        }

        previousButton.Click += (_, _) => ShowPage(pageIndex - 1);
        nextButton.Click += (_, _) => ShowPage(pageIndex + 1);

        return SceneStoryPresentation.Compose(
            "PhotoAlbum",
            instruction,
            stage,
            imageCache,
            renderedAssetIds,
            parameters.UseTextOnlyFallback,
            $"Text-only album, {title}: {string.Join(", ", pages.Select(page => page.Label))}.",
            parameters.PreviewOutcome,
            shouldReduceMotion,
            OutcomeCopy,
            [albumTitle, pageCards[0], navigationCard],
            () => pageCards[0].RenderTransform = TemplateRendering.Transform(18, 8, -4, 0.94),
            () =>
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(230), albumTitle),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(620), pageCards[0], 0, 0, -1.2, 1),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(240), navigationCard),
            ],
            () => albumTitle.SkipEntrance(),
            reportOutcome,
            "Album viewed");
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Album viewed. Every caption remains available by page.",
        TemplateOutcomeState.Uncertain => "Turn through each page before continuing.",
        TemplateOutcomeState.Failure => "Return to the first page and read each caption again.",
        _ => "Ready: use the page controls to explore the album.",
    };
}

internal static class CulturePlateRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var title = TemplateRendering.Text(parameters, "title");
        var caption = TemplateRendering.Localized(parameters, "caption", instructionLanguage);
        var sourceNote = TemplateRendering.Text(parameters, "source-note");
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var assetReference = TemplateRendering.AssetReference(parameters, "asset");
        var artifactImage = parameters.UseTextOnlyFallback
            ? null
            : TemplateRendering.CreateContentImage(imageCache, assetReference, 164, Stretch.Uniform);

        var stage = TemplateRendering.CreateStage(316, $"Culture plate: {title}");
        TemplateRendering.AddBackdrop(stage, imageCache, assetReferenceId: null);
        var artifactContent = artifactImage as Control ?? new StackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    FontSize = 28,
                    FontWeight = FontWeight.Bold,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                },
                new TextBlock
                {
                    Text = "Authored text-only plate",
                    FontSize = 13,
                    FontWeight = FontWeight.SemiBold,
                    TextAlignment = TextAlignment.Center,
                },
            },
        };
        var artifact = new CutoutFrame
        {
            Width = 250,
            Height = 206,
            Content = artifactContent,
        };
        artifact.Classes.Add("tilt-left");
        AutomationProperties.SetName(
            artifact,
            artifactImage is null ? $"Text-only plate for {title}" : $"Artifact image for {title}");
        PaperStage.SetLayer(artifact, PaperStageLayer.Subject);
        PaperStage.SetAnchor(artifact, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(artifact, 0.31);
        PaperStage.SetAnchorOffsetY(artifact, -22);
        stage.Children.Add(artifact);

        var captionCopy = new StackPanel { Spacing = 7 };
        captionCopy.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 22,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap,
        });
        captionCopy.Children.Add(new TextBlock
        {
            Text = caption,
            FontSize = 15,
            TextWrapping = TextWrapping.Wrap,
        });
        var sourceCard = new PaperTape { Content = sourceNote, Angle = -1.4 };
        AutomationProperties.SetName(sourceCard, $"Source state. {sourceNote}");
        captionCopy.Children.Add(sourceCard);
        var captionCard = new PaperCard
        {
            Width = 340,
            Padding = new Thickness(16, 13),
            Content = captionCopy,
        };
        PaperStage.SetLayer(captionCard, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(captionCard, PaperAnchorLine.Shoulder);
        PaperStage.SetAnchorX(captionCard, 0.7);
        PaperStage.SetAnchorOffsetY(captionCard, 16);
        PaperStage.SetLayerTransform(captionCard, TemplateRendering.Transform(0, 0, 1.1, 1));
        AutomationProperties.SetName(captionCard, $"Culture plate caption. {caption}");
        stage.Children.Add(captionCard);

        return SceneStoryPresentation.Compose(
            "CulturePlate",
            instruction,
            stage,
            imageCache,
            [artifactImage is not null ? assetReference : null],
            parameters.UseTextOnlyFallback || artifactImage is null,
            $"Text-only culture plate: {title}.\n{caption}\nSource state: {sourceNote}",
            parameters.PreviewOutcome,
            shouldReduceMotion,
            OutcomeCopy,
            [artifact, captionCard, sourceCard],
            () => artifact.RenderTransform = TemplateRendering.Transform(-24, 4, -4, 0.94),
            () =>
            [
                TemplateRendering.Move(TimeSpan.FromMilliseconds(560), artifact, 0, 0, -1.2, 1),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(360), captionCard),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(280), sourceCard),
            ],
            () => sourceCard.SkipEntrance(),
            reportOutcome,
            "Plate read");
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Plate read. Caption and source state remain together.",
        TemplateOutcomeState.Uncertain => "Read the caption and source state before continuing.",
        TemplateOutcomeState.Failure => "Replay the plate and check the source state again.",
        _ => "Ready: inspect the plate, caption, and source state.",
    };
}

internal static class WeatherWindowRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var weather = TemplateRendering.Text(parameters, "weather");
        var season = TemplateRendering.Text(parameters, "season");
        var effect = TemplateRendering.Text(parameters, "effect");
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var backdropReference = TemplateRendering.AssetReference(parameters, "backdrop");
        var stage = TemplateRendering.CreateStage(316, $"Weather window: {weather}, {season}");
        var backdropRendered = TemplateRendering.AddBackdrop(
            stage,
            imageCache,
            parameters.UseTextOnlyFallback ? null : backdropReference);

        var windowFrame = new Border
        {
            Width = 560,
            Height = 222,
            ClipToBounds = true,
            IsHitTestVisible = false,
            Child = CreateWindowGrid(),
        };
        windowFrame.Classes.Add("weather-window-frame");
        PaperStage.SetLayer(windowFrame, PaperStageLayer.SupportingCast);
        PaperStage.SetAnchor(windowFrame, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(windowFrame, 0.5);
        PaperStage.SetAnchorOffsetY(windowFrame, -30);
        stage.Children.Add(windowFrame);

        var particles = new Canvas
        {
            Width = 520,
            Height = 184,
            IsHitTestVisible = false,
        };
        var symbol = effect.ToLowerInvariant() switch
        {
            "snow" => "•",
            "sun" => "✦",
            _ => "╱",
        };
        var positions = new (double X, double Y)[]
        {
            (24, 18),
            (88, 56),
            (154, 26),
            (220, 82),
            (286, 36),
            (348, 94),
            (414, 24),
            (478, 68),
            (54, 126),
            (182, 142),
            (318, 132),
            (446, 146),
        };
        foreach (var (x, y) in positions)
        {
            var particle = new TextBlock
            {
                Text = symbol,
                FontSize = symbol == "✦" ? 22 : 18,
                FontWeight = FontWeight.Bold,
                Opacity = 0.72,
                Classes = { "muted" },
            };
            Canvas.SetLeft(particle, x);
            Canvas.SetTop(particle, y);
            particles.Children.Add(particle);
        }

        AutomationProperties.SetName(particles, $"Static paper weather effect: {effect}");
        PaperStage.SetLayer(particles, PaperStageLayer.AmbientPieces);
        PaperStage.SetAnchor(particles, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(particles, 0.5);
        PaperStage.SetAnchorOffsetY(particles, -28);
        stage.Children.Add(particles);

        var weatherLabel = new PaperTape
        {
            Content = $"{weather} · {season}",
            Angle = -1.3,
        };
        PaperStage.SetLayer(weatherLabel, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(weatherLabel, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(weatherLabel, 0.25);
        PaperStage.SetAnchorOffsetY(weatherLabel, -10);
        AutomationProperties.SetName(weatherLabel, $"Weather {weather}. Season {season}.");
        stage.Children.Add(weatherLabel);

        return SceneStoryPresentation.Compose(
            "WeatherWindow",
            instruction,
            stage,
            imageCache,
            [backdropRendered ? backdropReference : null],
            parameters.UseTextOnlyFallback,
            $"Text-only weather window. Weather: {weather}. Season: {season}.",
            parameters.PreviewOutcome,
            shouldReduceMotion,
            OutcomeCopy,
            [windowFrame, particles, weatherLabel],
            () =>
            {
                windowFrame.RenderTransform = TemplateRendering.Transform(0, 8, 0, 0.98);
                particles.RenderTransform = TemplateRendering.Transform(-18, -10, 0, 1);
            },
            () =>
            [
                TemplateRendering.Move(TimeSpan.FromMilliseconds(420), windowFrame, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(760), particles, 0, 0, 0, 1),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(260), weatherLabel),
            ],
            () => weatherLabel.SkipEntrance(),
            reportOutcome,
            "Weather noticed");
    }

    private static Grid CreateWindowGrid()
    {
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("*,*"),
            ColumnDefinitions = new ColumnDefinitions("*,*"),
        };
        var vertical = new Border { Width = 8, HorizontalAlignment = HorizontalAlignment.Center };
        vertical.Classes.Add("weather-window-mullion");
        Grid.SetRowSpan(vertical, 2);
        grid.Children.Add(vertical);
        var horizontal = new Border { Height = 8, VerticalAlignment = VerticalAlignment.Center };
        horizontal.Classes.Add("weather-window-mullion");
        Grid.SetColumnSpan(horizontal, 2);
        grid.Children.Add(horizontal);
        return grid;
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Weather noticed. Condition and season remain visible together.",
        TemplateOutcomeState.Uncertain => "Pause on the weather and season labels before continuing.",
        TemplateOutcomeState.Failure => "Replay the window and read both labels again.",
        _ => "Ready: watch the paper weather settle across the window.",
    };
}

internal static class ClockTheatreRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var time = TemplateRendering.Text(parameters, "time");
        var hourText = TemplateRendering.Text(parameters, "hour");
        var minuteText = TemplateRendering.Text(parameters, "minute");
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var hourParsed = int.TryParse(hourText, out var hour);
        var minuteParsed = int.TryParse(minuteText, out var minute);
        var hasTime = hourParsed &&
                      minuteParsed &&
                      hour is >= 0 and <= 23 &&
                      minute is >= 0 and <= 59;
        var hourAngle = hasTime ? ((hour % 12) * 30) + (minute * 0.5) : 0;
        var minuteAngle = hasTime ? minute * 6 : 0;

        var stage = TemplateRendering.CreateStage(318, $"Paper clock showing {time}");
        TemplateRendering.AddBackdrop(stage, imageCache, assetReferenceId: null);
        var (clockFace, hourHand, minuteHand) = CreateClockFace(time);
        PaperStage.SetLayer(clockFace, PaperStageLayer.Subject);
        PaperStage.SetAnchor(clockFace, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(clockFace, 0.36);
        PaperStage.SetAnchorOffsetY(clockFace, -28);
        stage.Children.Add(clockFace);

        var timeLabel = new PaperTape { Content = time, Angle = -1.2 };
        PaperStage.SetLayer(timeLabel, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(timeLabel, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(timeLabel, 0.7);
        PaperStage.SetAnchorOffsetY(timeLabel, -16);
        AutomationProperties.SetName(timeLabel, $"Authored time: {time}");
        stage.Children.Add(timeLabel);

        Control? unavailableCard = null;
        if (!hasTime)
        {
            unavailableCard = new PaperCard
            {
                Width = 280,
                Padding = new Thickness(14, 10),
                Content = new TextBlock
                {
                    Text = $"Clock hands unavailable. Read the authored time: {time}.",
                    TextWrapping = TextWrapping.Wrap,
                    Classes = { "muted" },
                },
            };
            PaperStage.SetLayer(unavailableCard, PaperStageLayer.VerdictCard);
            PaperStage.SetAnchor(unavailableCard, PaperAnchorLine.Foot);
            PaperStage.SetAnchorX(unavailableCard, 0.7);
            PaperStage.SetAnchorOffsetY(unavailableCard, -60);
            AutomationProperties.SetName(unavailableCard, $"Clock geometry unavailable. Authored time: {time}.");
            stage.Children.Add(unavailableCard);
        }

        var animatedControls = unavailableCard is null
            ? new Control[] { clockFace, hourHand, minuteHand, timeLabel }
            : [clockFace, timeLabel, unavailableCard];
        return SceneStoryPresentation.Compose(
            "ClockTheatre",
            instruction,
            stage,
            imageCache,
            [],
            parameters.UseTextOnlyFallback || !hasTime,
            $"Text-only clock. Authored time: {time}. Hour: {hourText}. Minute: {minuteText}.",
            parameters.PreviewOutcome,
            shouldReduceMotion,
            OutcomeCopy,
            animatedControls,
            () => clockFace.RenderTransform = TemplateRendering.Transform(0, 8, -3, 0.94),
            () => CreateSteps(clockFace, hourHand, minuteHand, timeLabel, unavailableCard, hourAngle, minuteAngle),
            () => timeLabel.SkipEntrance(),
            reportOutcome,
            "Time noticed");
    }

    private static IReadOnlyList<PaperChoreographyStep> CreateSteps(
        Control clockFace,
        Control hourHand,
        Control minuteHand,
        Control timeLabel,
        Control? unavailableCard,
        double hourAngle,
        double minuteAngle)
    {
        var steps = new List<PaperChoreographyStep>
        {
            TemplateRendering.Move(TimeSpan.FromMilliseconds(420), clockFace, 0, 0, 0, 1),
        };
        if (unavailableCard is null)
        {
            steps.Add(TemplateRendering.Move(TimeSpan.FromMilliseconds(720), hourHand, 0, 0, hourAngle, 1));
            steps.Add(TemplateRendering.Move(TimeSpan.FromMilliseconds(720), minuteHand, 0, 0, minuteAngle, 1));
        }
        else
        {
            steps.Add(TemplateRendering.Reveal(TimeSpan.FromMilliseconds(320), unavailableCard));
        }

        steps.Add(TemplateRendering.Reveal(TimeSpan.FromMilliseconds(260), timeLabel));
        return steps;
    }

    private static (PaperCard Face, Border HourHand, Border MinuteHand) CreateClockFace(string time)
    {
        const double size = 220;
        const double center = size / 2;
        var canvas = new Canvas { Width = size, Height = size };
        foreach (var (label, x, y) in new[]
                 {
                     ("12", center - 11, 8d),
                     ("3", size - 25, center - 13),
                     ("6", center - 7, size - 32),
                     ("9", 12d, center - 13),
                 })
        {
            var number = new TextBlock
            {
                Text = label,
                FontSize = 18,
                FontWeight = FontWeight.Bold,
            };
            Canvas.SetLeft(number, x);
            Canvas.SetTop(number, y);
            canvas.Children.Add(number);
        }

        var hourHand = CreateHand(width: 8, height: 54, center, center - 54);
        var minuteHand = CreateHand(width: 5, height: 78, center, center - 78);
        canvas.Children.Add(hourHand);
        canvas.Children.Add(minuteHand);
        var pin = new Border
        {
            Width = 16,
            Height = 16,
            CornerRadius = new CornerRadius(8),
        };
        pin.Classes.Add("lesson-unit-number");
        Canvas.SetLeft(pin, center - 8);
        Canvas.SetTop(pin, center - 8);
        canvas.Children.Add(pin);

        var face = new PaperCard
        {
            Width = 250,
            Height = 250,
            CornerRadius = new CornerRadius(125),
            Padding = new Thickness(15),
            Content = canvas,
        };
        AutomationProperties.SetName(face, $"Paper clock face. Authored time: {time}.");
        return (face, hourHand, minuteHand);
    }

    private static Border CreateHand(double width, double height, double center, double top)
    {
        var hand = new Border
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(width / 2),
            RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        };
        hand.Classes.Add("lesson-unit-number");
        Canvas.SetLeft(hand, center - (width / 2));
        Canvas.SetTop(hand, top);
        return hand;
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Time noticed. The paper hands and authored phrase stay together.",
        TemplateOutcomeState.Uncertain => "Pause on the hands and the written time before continuing.",
        TemplateOutcomeState.Failure => "Replay the clock and read the authored time again.",
        _ => "Ready: watch the two paper hands settle on the time.",
    };
}

internal static class SceneStoryPresentation
{
    public static Control Compose(
        string automationPrefix,
        string instruction,
        PaperStage stage,
        ContentImageCache? imageCache,
        IEnumerable<string?> creditAssetIds,
        bool useTextOnlyFallback,
        string textOnlyCopy,
        TemplateOutcomeState previewOutcome,
        bool shouldReduceMotion,
        Func<TemplateOutcomeState, string> outcomeCopy,
        IReadOnlyList<Control> animatedControls,
        Action prepareAnimatedStart,
        Func<IReadOnlyList<PaperChoreographyStep>> createSteps,
        Action skipEntrances,
        Action<TemplateOutcome> reportOutcome,
        string completeButtonText,
        Control? supplementalControl = null)
    {
        var replayButton = new Button { Content = "Replay scene", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, $"{automationPrefix}Replay");
        AutomationProperties.SetName(replayButton, "Replay the presentation scene");
        var skipButton = new Button { Content = "Skip scene", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, $"{automationPrefix}Skip");
        AutomationProperties.SetName(skipButton, "Skip to the completed presentation");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Template instruction. {instruction}");
        var sceneActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
        };
        sceneActions.Children.Add(replayButton);
        sceneActions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(sceneActions, 1);
        header.Children.Add(sceneActions);

        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            previewOutcome,
            outcomeCopy,
            out var outcomeText);
        var completeButton = new Button
        {
            Content = completeButtonText,
            Classes = { "primary", "lift" },
        };
        AutomationProperties.SetAutomationId(completeButton, $"{automationPrefix}Continue");
        AutomationProperties.SetName(completeButton, $"Confirm: {completeButtonText}");
        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        footer.Children.Add(outcomePanel);
        Grid.SetColumn(completeButton, 1);
        footer.Children.Add(completeButton);

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        if (useTextOnlyFallback)
        {
            var textEquivalent = new StackPanel { Spacing = 4 };
            foreach (var block in textOnlyCopy.Split(
                         '\n',
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                textEquivalent.Children.Add(new TextBlock
                {
                    Text = block,
                    Classes = { "muted" },
                    TextWrapping = TextWrapping.Wrap,
                });
            }

            root.Children.Add(textEquivalent);
        }

        root.Children.Add(stage);
        if (supplementalControl is not null)
        {
            root.Children.Add(supplementalControl);
        }

        if (!useTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                creditAssetIds,
                $"{automationPrefix}ImageCredits") is { } credits)
        {
            root.Children.Add(credits);
        }

        root.Children.Add(footer);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, animatedControls.ToArray());
            if (!shouldReduceMotion)
            {
                prepareAnimatedStart();
            }

            scene = new PaperChoreography(createSteps());
            await scene.PlayAsync(shouldReduceMotion);
        }

        root.AttachedToVisualTree += async (_, _) => await PlayAsync();
        root.DetachedFromVisualTree += (_, _) =>
        {
            scene?.Skip();
            scene?.Dispose();
            scene = null;
        };
        skipButton.Click += (_, _) =>
        {
            scene?.Skip();
            skipEntrances();
        };
        replayButton.Click += async (_, _) => await PlayAsync();
        completeButton.Click += (_, _) =>
        {
            var outcome = TemplateInteractionEvaluator.EvaluateAcknowledgement(acknowledged: true);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, outcomeCopy);
            reportOutcome(outcome);
        };
        return root;
    }
}
