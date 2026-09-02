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

namespace Linguistics.App.Features.Learn.Templates;

internal static class ScenarioTheatreRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var task = TemplateRendering.TaskReference(parameters, "task");
        var taskGoal = InstructionText.Resolve(task.Goal, instructionLanguage);
        var taskContext = InstructionText.Resolve(task.Context, instructionLanguage);
        var npcRole = InstructionText.Resolve(task.NpcRole, instructionLanguage);
        var stateLabel = TemplateRendering.Text(parameters, "state-label");
        var npcLine = TemplateRendering.Text(parameters, "npc-line");
        var responses = TemplateRendering.Options(parameters, "responses");
        var answer = TemplateRendering.Text(parameters, "answer");
        var retryHint = TemplateRendering.Text(parameters, "retry-hint");
        var npcAssetId = TemplateRendering.AssetReference(parameters, "npc-asset");
        var backdropAssetId = TemplateRendering.AssetReference(parameters, "backdrop");

        var replayButton = new Button { Content = "Replay scene", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "ScenarioTheatreReplay");
        AutomationProperties.SetName(replayButton, "Replay the scenario entrance");
        var skipButton = new Button { Content = "Skip entrance", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "ScenarioTheatreSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed scenario scene");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Scenario instruction. {instruction}");
        var headerActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        headerActions.Children.Add(replayButton);
        headerActions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(headerActions, 1);
        header.Children.Add(headerActions);

        var successCriteria = new StackPanel { Spacing = 5 };
        foreach (var condition in task.SuccessConditions)
        {
            successCriteria.Children.Add(new TextBlock
            {
                Text = $"• {InstructionText.Resolve(condition.Description, instructionLanguage)}",
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
            });
        }

        var goalCard = new PaperCard
        {
            Padding = new Thickness(18, 14),
            Content = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("2*,3*"),
                ColumnSpacing = 18,
                Children =
                {
                    new StackPanel
                    {
                        Spacing = 7,
                        Children =
                        {
                            new PaperTape { Content = "MISSION GOAL", Angle = -1, Classes = { "compact" } },
                            new TextBlock
                            {
                                Text = taskGoal,
                                FontSize = 20,
                                FontWeight = FontWeight.Bold,
                                TextWrapping = TextWrapping.Wrap,
                            },
                            new TextBlock
                            {
                                Text = taskContext,
                                FontSize = 13,
                                TextWrapping = TextWrapping.Wrap,
                                Classes = { "muted" },
                            },
                        },
                    },
                    new StackPanel
                    {
                        Spacing = 6,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "SUCCESS LOOKS LIKE",
                                FontSize = 11,
                                FontWeight = FontWeight.Bold,
                            },
                            successCriteria,
                        },
                    },
                },
            },
        };
        Grid.SetColumn(((Grid)goalCard.Content).Children[1], 1);
        AutomationProperties.SetAutomationId(goalCard, "ScenarioTheatreGoal");
        AutomationProperties.SetName(
            goalCard,
            $"Scenario goal. {taskGoal} Context. {taskContext}");

        var stage = TemplateRendering.CreateStage(
            310,
            $"Paper scenario for task {task.Id}. {npcRole} says {npcLine}");
        var hasBackdrop = !parameters.UseTextOnlyFallback &&
                          TemplateRendering.AddBackdrop(stage, imageCache, backdropAssetId);
        var stateTape = new PaperTape
        {
            Content = stateLabel.ToUpperInvariant(),
            Angle = -1.3,
            Classes = { "compact" },
        };
        PaperStage.SetLayer(stateTape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(stateTape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(stateTape, 0.22);
        PaperStage.SetAnchorOffsetY(stateTape, -22);
        stage.Children.Add(stateTape);

        var counter = new PaperCard
        {
            Width = 590,
            Height = 76,
            Padding = new Thickness(18, 12),
            Content = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock
                    {
                        Text = "CAFÉ COUNTER",
                        FontSize = 13,
                        FontWeight = FontWeight.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    },
                    new TextBlock
                    {
                        Text = "A complete authored paper set is active.",
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Classes = { "muted" },
                    },
                },
            },
        };
        counter.Classes.Add("soft");
        AutomationProperties.SetAutomationId(counter, "ScenarioTheatreSet");
        AutomationProperties.SetName(counter, "Paper café counter set");
        PaperStage.SetLayer(counter, PaperStageLayer.ForegroundSilhouettes);
        PaperStage.SetAnchor(counter, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(counter, 0.5);
        PaperStage.SetAnchorOffsetY(counter, 14);
        stage.Children.Add(counter);

        Control npcContent;
        var npcImage = parameters.UseTextOnlyFallback
            ? null
            : TemplateRendering.CreateContentImage(imageCache, npcAssetId, 146);
        if (npcImage is not null)
        {
            AutomationProperties.SetName(npcImage, $"{npcRole} scene cutout");
            npcContent = npcImage;
        }
        else
        {
            npcContent = new StackPanel
            {
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new PaperTape { Content = "NPC PUPPET", Angle = 1.1, Classes = { "compact" } },
                    new TextBlock
                    {
                        Text = npcRole,
                        FontSize = 20,
                        FontWeight = FontWeight.Bold,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = "Authored text-only character",
                        FontSize = 12,
                        FontWeight = FontWeight.SemiBold,
                        Opacity = 0.72,
                        TextAlignment = TextAlignment.Center,
                    },
                },
            };
        }

        var npc = new CutoutFrame
        {
            Width = 202,
            Height = 188,
            Padding = new Thickness(12),
            Content = npcContent,
            RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        };
        npc.Classes.Add("tilt-right");
        AutomationProperties.SetAutomationId(npc, "ScenarioTheatreNpc");
        AutomationProperties.SetName(npc, $"NPC puppet. {npcRole}");
        PaperStage.SetLayer(npc, PaperStageLayer.Subject);
        PaperStage.SetAnchor(npc, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(npc, 0.77);
        PaperStage.SetAnchorOffsetY(npc, -28);
        stage.Children.Add(npc);

        var npcSpeech = new PaperCard
        {
            Width = 350,
            Padding = new Thickness(16, 13),
            Content = new StackPanel
            {
                Spacing = 7,
                Children =
                {
                    new TextBlock
                    {
                        Text = npcRole.ToUpperInvariant(),
                        FontSize = 11,
                        FontWeight = FontWeight.Bold,
                    },
                    new TextBlock
                    {
                        Text = npcLine,
                        FontSize = 18,
                        FontWeight = FontWeight.SemiBold,
                        TextWrapping = TextWrapping.Wrap,
                    },
                },
            },
        };
        npcSpeech.Classes.Add("soft");
        AutomationProperties.SetAutomationId(npcSpeech, "ScenarioTheatreNpcLine");
        AutomationProperties.SetName(npcSpeech, $"{npcRole} says {npcLine}");
        PaperStage.SetLayer(npcSpeech, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(npcSpeech, PaperAnchorLine.Shoulder);
        PaperStage.SetAnchorX(npcSpeech, 0.34);
        PaperStage.SetAnchorOffsetY(npcSpeech, 4);
        stage.Children.Add(npcSpeech);

        var modeText = new TextBlock
        {
            Text = parameters.UseTextOnlyFallback
                ? "Text-only scene mode is active. Goal, dialogue, responses, and result remain complete."
                : hasBackdrop && npcImage is not null
                    ? "Validated local scene art is active. All dialogue remains real text."
                    : "Absent scene art is replaced by the complete authored paper set and character card.",
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Classes = { "muted" },
        };
        AutomationProperties.SetAutomationId(modeText, "ScenarioTheatreTextEquivalent");

        var responseButtons = new List<Button>();
        var responsesPanel = new WrapPanel { Orientation = Orientation.Horizontal };
        AutomationProperties.SetAutomationId(responsesPanel, "ScenarioTheatreResponses");
        AutomationProperties.SetName(responsesPanel, "Choose one authored scenario response");
        var conversationStatus = new TextBlock
        {
            Text = "Conversation ready. Choose one authored response.",
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(conversationStatus, "ScenarioTheatreConversation");
        AutomationProperties.SetLiveSetting(conversationStatus, AutomationLiveSetting.Polite);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var retryButton = new Button
        {
            Content = "Retry response",
            Classes = { "quiet" },
            IsVisible = parameters.PreviewOutcome == TemplateOutcomeState.Failure,
        };
        AutomationProperties.SetAutomationId(retryButton, "ScenarioTheatreRetry");
        AutomationProperties.SetName(retryButton, "Reset the conversation response and try again");

        void ResetResponse()
        {
            foreach (var button in responseButtons)
            {
                button.IsEnabled = true;
            }

            retryButton.IsVisible = false;
            conversationStatus.Text = "Conversation reset. Choose one authored response.";
            TemplateRendering.ApplyOutcome(
                outcomePanel,
                outcomeText,
                TemplateOutcomeState.Ready,
                OutcomeCopy);
        }

        foreach (var response in responses)
        {
            var button = new Button
            {
                Content = response.Label,
                Margin = new Thickness(0, 0, 8, 8),
                Classes = { "quiet" },
            };
            AutomationProperties.SetAutomationId(button, $"ScenarioTheatreResponse_{response.Id}");
            AutomationProperties.SetName(button, $"Respond: {response.Label}");
            button.Click += (_, _) =>
            {
                var outcome = TemplateInteractionEvaluator.EvaluateScenarioChoice(
                    responses,
                    answer,
                    response.Id);
                foreach (var candidate in responseButtons)
                {
                    candidate.IsEnabled = false;
                }

                conversationStatus.Text = outcome.State == TemplateOutcomeState.Success
                    ? $"You: {response.Label} The task response is accepted."
                    : $"You: {response.Label} {retryHint}";
                retryButton.IsVisible = true;
                retryButton.Content = outcome.State == TemplateOutcomeState.Success
                    ? "Practice response again"
                    : "Retry response";
                TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
                reportOutcome(outcome);
            };
            responseButtons.Add(button);
            responsesPanel.Children.Add(button);
        }

        retryButton.Click += (_, _) => ResetResponse();
        var conversationCard = new Border
        {
            Padding = new Thickness(14, 10),
            Child = conversationStatus,
            Classes = { "soft-card" },
        };
        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 12 };
        footer.Children.Add(retryButton);
        Grid.SetColumn(outcomePanel, 1);
        footer.Children.Add(outcomePanel);

        var root = new StackPanel { Spacing = 14 };
        root.Children.Add(header);
        root.Children.Add(goalCard);
        root.Children.Add(stage);
        root.Children.Add(modeText);
        root.Children.Add(responsesPanel);
        root.Children.Add(conversationCard);
        root.Children.Add(footer);
        if (!parameters.UseTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                [backdropAssetId, npcAssetId],
                "ScenarioTheatreImageCredits") is { } credits)
        {
            root.Children.Add(credits);
        }

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(
                shouldReduceMotion,
                goalCard,
                stateTape,
                counter,
                npc,
                npcSpeech,
                modeText,
                responsesPanel,
                conversationCard,
                footer);
            if (!shouldReduceMotion)
            {
                npc.RenderTransform = TemplateRendering.Transform(10, 0, 1.2, 0.98);
                npcSpeech.RenderTransform = TemplateRendering.Transform(-8, 0, -0.8, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(180), goalCard, stateTape),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(180), counter),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(240), npc),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), npcSpeech),
                TemplateRendering.Reveal(
                    TimeSpan.FromMilliseconds(220),
                    modeText,
                    responsesPanel,
                    conversationCard,
                    footer),
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
        replayButton.Click += async (_, _) => await PlayAsync();
        skipButton.Click += (_, _) => scene?.Skip();
        return root;
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Task response accepted by the deterministic scenario check.",
        TemplateOutcomeState.Uncertain => "Choose one authored response to continue the scenario.",
        TemplateOutcomeState.Failure => "That response did not complete this task state. Retry remains available.",
        _ => "Ready: read the goal, then answer the NPC.",
    };
}

internal static class ConsequenceVerdictRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var subject = TemplateRendering.Text(parameters, "subject");
        var stateLabel = TemplateRendering.Text(parameters, "state-label");
        var verdicts = TemplateRendering.Options(parameters, "verdicts");
        var consequences = TemplateRendering.Options(parameters, "consequences");
        var reportLines = TemplateRendering.Options(parameters, "report-lines");
        var actions = TemplateRendering.Options(parameters, "actions");
        var retryActionId = TemplateRendering.Text(parameters, "retry-action");
        var subjectAssetId = TemplateRendering.AssetReference(parameters, "subject-asset");
        var backdropAssetId = TemplateRendering.AssetReference(parameters, "backdrop");
        var projectedOutcome = parameters.PreviewOutcome;
        var outcomeKey = OutcomeKey(projectedOutcome);
        var verdict = verdicts.Single(option => option.Id == outcomeKey).Label;
        var consequence = consequences.Single(option => option.Id == outcomeKey).Label;

        var replayButton = new Button { Content = "Replay verdict", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "ConsequenceVerdictReplay");
        AutomationProperties.SetName(replayButton, "Replay the consequence and verdict entrance");
        var skipButton = new Button { Content = "Skip entrance", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "ConsequenceVerdictSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed consequence report");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Consequence instruction. {instruction}");
        var headerActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        headerActions.Children.Add(replayButton);
        headerActions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(headerActions, 1);
        header.Children.Add(headerActions);

        var stage = TemplateRendering.CreateStage(
            320,
            $"Outcome consequence for {subject}. {verdict}. {consequence}");
        var hasBackdrop = !parameters.UseTextOnlyFallback &&
                          TemplateRendering.AddBackdrop(stage, imageCache, backdropAssetId);
        var pendingLabel = new PaperTape
        {
            Content = stateLabel.ToUpperInvariant(),
            Angle = -1.5,
            Classes = { "compact" },
        };
        AutomationProperties.SetAutomationId(pendingLabel, "ConsequenceVerdictClearingLabel");
        AutomationProperties.SetName(pendingLabel, $"Clearing state label. {stateLabel}");
        PaperStage.SetLayer(pendingLabel, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(pendingLabel, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(pendingLabel, 0.5);
        PaperStage.SetAnchorOffsetY(pendingLabel, 10);
        stage.Children.Add(pendingLabel);

        Control subjectContent;
        var subjectImage = parameters.UseTextOnlyFallback
            ? null
            : TemplateRendering.CreateContentImage(imageCache, subjectAssetId, 178);
        if (subjectImage is not null)
        {
            AutomationProperties.SetName(subjectImage, $"{subject} cutout");
            subjectContent = subjectImage;
        }
        else
        {
            subjectContent = new StackPanel
            {
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new PaperTape { Content = "PUPPET", Angle = 1, Classes = { "compact" } },
                    new TextBlock
                    {
                        Text = subject,
                        FontSize = 20,
                        FontWeight = FontWeight.Bold,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = "Authored text-only subject",
                        FontSize = 12,
                        TextAlignment = TextAlignment.Center,
                        Classes = { "muted" },
                    },
                },
            };
        }

        var puppet = new CutoutFrame
        {
            Width = 220,
            Height = 210,
            Padding = new Thickness(10),
            Content = subjectContent,
            RenderTransformOrigin = new RelativePoint(0.5, 1, RelativeUnit.Relative),
        };
        puppet.Classes.Add("tilt-left");
        AutomationProperties.SetAutomationId(puppet, "ConsequenceVerdictPuppet");
        AutomationProperties.SetName(puppet, $"Outcome puppet. {subject}. {consequence}");
        PaperStage.SetLayer(puppet, PaperStageLayer.Subject);
        PaperStage.SetAnchor(puppet, PaperAnchorLine.Foot);
        PaperStage.SetAnchorX(puppet, 0.3);
        PaperStage.SetAnchorOffsetY(puppet, -18);
        stage.Children.Add(puppet);

        var verdictCard = new PaperCard
        {
            Width = 356,
            Padding = new Thickness(18, 15),
            Content = new StackPanel
            {
                Spacing = 9,
                Children =
                {
                    new PaperTape
                    {
                        Content = verdict.ToUpperInvariant(),
                        Angle = 1.4,
                        Classes = { "compact" },
                    },
                    new TextBlock
                    {
                        Text = consequence,
                        FontSize = 17,
                        FontWeight = FontWeight.SemiBold,
                        TextWrapping = TextWrapping.Wrap,
                    },
                },
            },
        };
        verdictCard.Classes.Add("soft");
        AutomationProperties.SetAutomationId(verdictCard, "ConsequenceVerdictCard");
        AutomationProperties.SetName(verdictCard, $"Verdict. {verdict}. {consequence}");
        PaperStage.SetLayer(verdictCard, PaperStageLayer.VerdictCard);
        PaperStage.SetAnchor(verdictCard, PaperAnchorLine.Shoulder);
        PaperStage.SetAnchorX(verdictCard, 0.7);
        PaperStage.SetAnchorOffsetY(verdictCard, 14);
        stage.Children.Add(verdictCard);

        var reportList = new StackPanel { Spacing = 6 };
        foreach (var line in reportLines)
        {
            reportList.Children.Add(new TextBlock
            {
                Text = $"• {line.Label}",
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
            });
        }

        var reportCard = new PaperCard
        {
            Padding = new Thickness(16, 13),
            Content = new StackPanel
            {
                Spacing = 7,
                Children =
                {
                    new TextBlock
                    {
                        Text = "DETAILED STATIC REPORT",
                        FontSize = 11,
                        FontWeight = FontWeight.Bold,
                    },
                    reportList,
                },
            },
        };
        reportCard.Classes.Add("soft");
        AutomationProperties.SetAutomationId(reportCard, "ConsequenceVerdictReport");
        AutomationProperties.SetName(
            reportCard,
            $"Detailed static outcome report. {string.Join(" ", reportLines.Select(line => line.Label))}");

        var modeText = new TextBlock
        {
            Text = parameters.UseTextOnlyFallback
                ? "Text-only outcome mode is active. The consequence, verdict, and report remain complete."
                : hasBackdrop && subjectImage is not null
                    ? "Validated local scene art is active. The full static report remains available."
                    : "Absent scene art is replaced by the complete authored paper outcome scene.",
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Classes = { "muted" },
        };
        AutomationProperties.SetAutomationId(modeText, "ConsequenceVerdictTextEquivalent");

        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            projectedOutcome,
            OutcomeCopy,
            out var outcomeText);
        PaperChoreography? scene = null;
        Grid footer = null!;
        var actionPanel = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var action in actions)
        {
            var button = new Button
            {
                Content = action.Label,
                Margin = new Thickness(0, 0, 8, 8),
                Classes = { string.Equals(action.Id, retryActionId, StringComparison.Ordinal) ? "quiet" : "primary" },
            };
            AutomationProperties.SetAutomationId(button, $"ConsequenceVerdictAction_{action.Id}");
            AutomationProperties.SetName(button, action.Label);
            button.Click += (_, _) =>
            {
                var outcome = TemplateInteractionEvaluator.EvaluateConsequenceAction(
                    actions,
                    retryActionId,
                    projectedOutcome,
                    action.Id);
                TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
                reportOutcome(outcome);
                if (string.Equals(action.Id, retryActionId, StringComparison.Ordinal))
                {
                    _ = PlayAsync();
                }
            };
            actionPanel.Children.Add(button);
        }

        footer = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 12 };
        footer.Children.Add(actionPanel);
        Grid.SetColumn(outcomePanel, 1);
        footer.Children.Add(outcomePanel);

        var root = new StackPanel { Spacing = 14 };
        root.Children.Add(header);
        root.Children.Add(stage);
        root.Children.Add(modeText);
        root.Children.Add(reportCard);
        root.Children.Add(footer);
        if (!parameters.UseTextOnlyFallback &&
            TemplateRendering.CreateCreditsDisclosure(
                imageCache,
                [backdropAssetId, subjectAssetId],
                "ConsequenceVerdictImageCredits") is { } credits)
        {
            root.Children.Add(credits);
        }

        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            pendingLabel.IsVisible = true;
            pendingLabel.Opacity = 1;
            TemplateRendering.Prepare(
                shouldReduceMotion,
                pendingLabel,
                puppet,
                verdictCard,
                modeText,
                reportCard,
                footer);
            if (!shouldReduceMotion)
            {
                verdictCard.RenderTransform = TemplateRendering.Transform(0, -18, 2.2, 0.98);
            }

            var (translateX, translateY, angle, scale) = ConsequenceMotion(projectedOutcome);
            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(160), pendingLabel, puppet),
                TemplateRendering.Move(
                    TimeSpan.FromMilliseconds(240),
                    puppet,
                    translateX,
                    translateY,
                    angle,
                    scale),
                ClearLabel(TimeSpan.FromMilliseconds(140), pendingLabel),
                TemplateRendering.Move(
                    TimeSpan.FromMilliseconds(240),
                    verdictCard,
                    translateX: 0,
                    translateY: 0,
                    angle: 1.2,
                    scale: 1),
                TemplateRendering.Reveal(
                    TimeSpan.FromMilliseconds(220),
                    modeText,
                    reportCard,
                    footer),
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
        replayButton.Click += async (_, _) => await PlayAsync();
        skipButton.Click += (_, _) => scene?.Skip();
        return root;
    }

    private static PaperChoreographyStep ClearLabel(TimeSpan duration, Control label) => new(
        duration,
        async cancellationToken =>
        {
            await Task.Delay(duration, cancellationToken);
            label.IsVisible = false;
        },
        () => label.IsVisible = false);

    private static (double X, double Y, double Angle, double Scale) ConsequenceMotion(
        TemplateOutcomeState state) => state switch
        {
            TemplateOutcomeState.Success => (0, -12, -1.2, 1.03),
            TemplateOutcomeState.Uncertain => (-5, 0, -2.2, 0.99),
            TemplateOutcomeState.Failure => (12, 7, 5.2, 0.96),
            _ => (0, 0, 0, 1),
        };

    private static string OutcomeKey(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "success",
        TemplateOutcomeState.Uncertain => "uncertain",
        TemplateOutcomeState.Failure => "failure",
        _ => "ready",
    };

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Success outcome preserved. Continue when the static report is clear.",
        TemplateOutcomeState.Uncertain => "Uncertain outcome preserved. Review the static report before continuing.",
        TemplateOutcomeState.Failure => "Failure outcome preserved. Retry remains available without hiding the report.",
        _ => "Ready: the projected outcome and its report are available.",
    };
}
