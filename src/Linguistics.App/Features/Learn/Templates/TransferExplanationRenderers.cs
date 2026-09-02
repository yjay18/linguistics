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

internal static class BridgeNoteRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var sourceLanguage = TemplateRendering.Text(parameters, "source-language");
        var noteType = TemplateRendering.Text(parameters, "note-type");
        var explanation = TemplateRendering.Text(parameters, "explanation");
        var risks = TemplateRendering.OptionalOptions(parameters, "risks");
        var mode = TemplateRendering.Text(parameters, "preference-mode");
        var actions = TemplateRendering.Options(parameters, "actions");
        var acknowledgementId = TemplateRendering.Text(parameters, "acknowledgement");
        var dismissalId = TemplateRendering.Text(parameters, "dismissal");
        var acknowledgement = actions.Single(action => action.Id == acknowledgementId);
        var dismissal = actions.Single(action => action.Id == dismissalId);
        var requiresConfirmation = mode switch
        {
            "ask-first" => true,
            "automatic" => false,
            _ => throw new InvalidOperationException("Bridge note preference mode must be ask-first or automatic."),
        };

        var replayButton = new Button { Content = "Replay note", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "BridgeNoteReplay");
        AutomationProperties.SetName(replayButton, "Replay the transfer note entrance");
        var skipButton = new Button { Content = "Skip entrance", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "BridgeNoteSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed transfer note");

        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Transfer note instruction. {instruction}");
        var headerActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        headerActions.Children.Add(replayButton);
        headerActions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(headerActions, 1);
        header.Children.Add(headerActions);

        var note = new TransferNoteCardView(
            new TransferNoteCardContent(
                sourceLanguage,
                noteType,
                explanation,
                risks.Select(risk => risk.Label).ToArray(),
                requiresConfirmation,
                dismissal.Label),
            "BridgeNote")
        {
            Width = 620,
        };
        var stage = TemplateRendering.CreateStage(
            300,
            $"Routed {sourceLanguage} {noteType} note");
        TemplateRendering.AddBackdrop(stage, imageCache: null, assetReferenceId: null);
        PaperStage.SetLayer(note, PaperStageLayer.Subject);
        PaperStage.SetAnchor(note, PaperAnchorLine.Waist);
        PaperStage.SetAnchorX(note, 0.5);
        stage.Children.Add(note);

        var keepButton = new Button
        {
            Content = acknowledgement.Label,
            Classes = { "primary", "lift" },
        };
        AutomationProperties.SetAutomationId(keepButton, "BridgeNoteAcknowledge");
        AutomationProperties.SetName(keepButton, acknowledgement.Label);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 12 };
        footer.Children.Add(keepButton);
        Grid.SetColumn(outcomePanel, 1);
        footer.Children.Add(outcomePanel);

        void Apply(TemplateOutcome outcome, string? overrideCopy = null)
        {
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            if (overrideCopy is not null)
            {
                outcomeText.Text = overrideCopy;
            }

            reportOutcome(outcome);
        }

        keepButton.Click += (_, _) =>
        {
            if (requiresConfirmation && !note.IsConfirmed)
            {
                Apply(
                    TemplateInteractionEvaluator.EvaluateAdvisoryChoice(
                        actions,
                        acknowledgementId,
                        selectedActionId: null));
                return;
            }

            Apply(TemplateInteractionEvaluator.EvaluateAdvisoryChoice(
                actions,
                acknowledgementId,
                acknowledgementId));
        };
        note.Dismissed += (_, _) =>
        {
            note.IsVisible = false;
            Apply(
                TemplateInteractionEvaluator.EvaluateAdvisoryChoice(
                    actions,
                    acknowledgementId,
                    dismissalId),
                "Note dismissed. This activity remains available without the bridge.");
        };

        var root = new StackPanel { Spacing = 14 };
        root.Children.Add(header);
        root.Children.Add(stage);
        root.Children.Add(footer);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            note.IsVisible = true;
            TemplateRendering.Prepare(shouldReduceMotion, note, footer);
            if (!shouldReduceMotion)
            {
                note.RenderTransform = TemplateRendering.Transform(-8, 8, -1.2, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Move(TimeSpan.FromMilliseconds(420), note, 0, 0, 0, 1),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), footer),
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
        skipButton.Click += (_, _) =>
        {
            scene?.Skip();
            note.SkipEntrance();
        };
        return root;
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Bridge acknowledged. Its routed explanation stays available.",
        TemplateOutcomeState.Uncertain => "Confirm the bridge before using it for this activity.",
        TemplateOutcomeState.Failure => "Replay the note and read its source and caution again.",
        _ => "Ready: read, use, or dismiss this routed language note.",
    };
}

internal static class FalseFriendAlarmRenderer
{
    public static Control Render(
        ContentImageCache? imageCache,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var instruction = TemplateRendering.Localized(parameters, "instruction", instructionLanguage);
        var sourceLanguage = TemplateRendering.Text(parameters, "source-language");
        var temptingForm = TemplateRendering.Text(parameters, "tempting-form");
        var targetForm = TemplateRendering.Text(parameters, "target-form");
        var explanation = TemplateRendering.Text(parameters, "explanation");
        var risk = TemplateRendering.Text(parameters, "risk");
        var actions = TemplateRendering.Options(parameters, "actions");
        var acknowledgementId = TemplateRendering.Text(parameters, "acknowledgement");
        var dismissalId = TemplateRendering.Text(parameters, "dismissal");
        var acknowledgement = actions.Single(action => action.Id == acknowledgementId);
        var dismissal = actions.Single(action => action.Id == dismissalId);

        var replayButton = new Button { Content = "Replay alarm", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "FalseFriendAlarmReplay");
        AutomationProperties.SetName(replayButton, "Replay the interference warning");
        var skipButton = new Button { Content = "Skip entrance", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "FalseFriendAlarmSkip");
        AutomationProperties.SetName(skipButton, "Skip to the completed interference warning");
        var instructionText = new TextBlock
        {
            Text = instruction,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(instructionText, $"Interference warning instruction. {instruction}");
        var headerActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        headerActions.Children.Add(replayButton);
        headerActions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(instructionText);
        Grid.SetColumn(headerActions, 1);
        header.Children.Add(headerActions);

        var temptingCard = FormCard(
            $"{sourceLanguage} habit",
            temptingForm,
            "soft-card",
            "TemptingFormCard");
        var arrow = new TextBlock
        {
            Text = "→",
            FontSize = 34,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(arrow, "Compare with the authored target form");
        var targetCard = FormCard(
            "German target form",
            targetForm,
            "accent-card",
            "TargetFormCard");
        var stamp = new PaperStamp { Content = "WATCH", Angle = -5 };
        AutomationProperties.SetAutomationId(stamp, "FalseFriendAlarmStamp");
        AutomationProperties.SetName(
            stamp,
            $"Interference warning stamped over {temptingForm}");

        var stage = TemplateRendering.CreateStage(
            278,
            $"{sourceLanguage} interference comparison: {temptingForm}, German {targetForm}");
        TemplateRendering.AddBackdrop(stage, imageCache: null, assetReferenceId: null);
        Place(stage, temptingCard, PaperStageLayer.Subject, PaperAnchorLine.Waist, 0.27);
        Place(stage, arrow, PaperStageLayer.AmbientPieces, PaperAnchorLine.Waist, 0.5);
        Place(stage, targetCard, PaperStageLayer.Subject, PaperAnchorLine.Waist, 0.73);
        Place(
            stage,
            stamp,
            PaperStageLayer.ReactionBurst,
            PaperAnchorLine.Waist,
            0.27,
            offsetY: -44);

        var explanationText = new TextBlock
        {
            Text = explanation,
            FontSize = 16,
            LineHeight = 22,
            TextWrapping = TextWrapping.Wrap,
        };
        AutomationProperties.SetAutomationId(explanationText, "FalseFriendAlarmExplanation");
        AutomationProperties.SetName(explanationText, $"Interference explanation. {explanation}");
        var riskText = new TextBlock
        {
            Text = risk,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Classes = { "muted" },
        };
        AutomationProperties.SetName(riskText, $"Interference boundary. {risk}");
        var explanationCard = new Border
        {
            Padding = new Thickness(14, 11),
            Classes = { "soft-card" },
            Child = new StackPanel
            {
                Spacing = 7,
                Children = { explanationText, riskText },
            },
        };

        var acknowledgeButton = new Button
        {
            Content = acknowledgement.Label,
            Classes = { "primary", "lift" },
        };
        AutomationProperties.SetAutomationId(acknowledgeButton, "FalseFriendAlarmAcknowledge");
        AutomationProperties.SetName(acknowledgeButton, acknowledgement.Label);
        var dismissButton = new Button { Content = dismissal.Label, Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(dismissButton, "FalseFriendAlarmDismiss");
        AutomationProperties.SetName(dismissButton, $"{dismissal.Label}. Continue without this warning.");
        var actionPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        actionPanel.Children.Add(acknowledgeButton);
        actionPanel.Children.Add(dismissButton);
        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 12 };
        footer.Children.Add(actionPanel);
        Grid.SetColumn(outcomePanel, 1);
        footer.Children.Add(outcomePanel);

        void Apply(string actionId, string? overrideCopy = null)
        {
            var outcome = TemplateInteractionEvaluator.EvaluateAdvisoryChoice(
                actions,
                acknowledgementId,
                actionId);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            if (overrideCopy is not null)
            {
                outcomeText.Text = overrideCopy;
            }

            reportOutcome(outcome);
        }

        acknowledgeButton.Click += (_, _) => Apply(acknowledgementId);
        dismissButton.Click += (_, _) =>
        {
            stage.IsVisible = false;
            Apply(dismissalId, "Alert dismissed. The authored target form remains in the explanation.");
        };

        var root = new StackPanel { Spacing = 14 };
        root.Children.Add(header);
        root.Children.Add(stage);
        root.Children.Add(explanationCard);
        root.Children.Add(footer);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            stage.IsVisible = true;
            TemplateRendering.Prepare(
                shouldReduceMotion,
                temptingCard,
                arrow,
                targetCard,
                stamp,
                explanationCard,
                footer);
            if (!shouldReduceMotion)
            {
                temptingCard.RenderTransform = TemplateRendering.Transform(-8, 0, -1.2, 0.98);
                targetCard.RenderTransform = TemplateRendering.Transform(8, 0, 1.2, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), temptingCard, arrow, targetCard),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(180), stamp),
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(220), explanationCard, footer),
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
        skipButton.Click += (_, _) =>
        {
            scene?.Skip();
            stamp.SkipEntrance();
        };
        return root;
    }

    private static Border FormCard(
        string label,
        string form,
        string className,
        string automationId)
    {
        var panel = new StackPanel
        {
            Spacing = 7,
            Children =
            {
                new TextBlock
                {
                    Text = label.ToUpperInvariant(),
                    FontSize = 11,
                    FontWeight = FontWeight.Bold,
                },
                new TextBlock
                {
                    Text = form,
                    FontSize = 29,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                },
            },
        };
        var card = new Border
        {
            Width = 228,
            Height = 126,
            Padding = new Thickness(16, 13),
            Child = panel,
            Classes = { className },
        };
        AutomationProperties.SetAutomationId(card, automationId);
        AutomationProperties.SetName(card, $"{label}: {form}");
        return card;
    }

    private static void Place(
        PaperStage stage,
        Control control,
        PaperStageLayer layer,
        PaperAnchorLine anchor,
        double anchorX,
        double offsetY = 0)
    {
        PaperStage.SetLayer(control, layer);
        PaperStage.SetAnchor(control, anchor);
        PaperStage.SetAnchorX(control, anchorX);
        PaperStage.SetAnchorOffsetY(control, offsetY);
        stage.Children.Add(control);
    }

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "Interference noticed. The authored German form stays visible.",
        TemplateOutcomeState.Uncertain => "Compare both written forms before continuing.",
        TemplateOutcomeState.Failure => "Replay the warning and read the transfer boundary again.",
        _ => "Ready: compare the tempting form with the authored target form.",
    };
}
