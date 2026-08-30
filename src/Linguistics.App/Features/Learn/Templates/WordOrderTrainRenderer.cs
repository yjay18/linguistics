using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Linguistics.App.Controls;
using Linguistics.App.Motion;
using Linguistics.Core.Content;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Learn.Templates;

internal static class WordOrderTrainRenderer
{
    public static Control Render(
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        var prompt = TemplateRendering.Localized(parameters, "prompt", instructionLanguage);
        var options = TemplateRendering.Options(parameters, "options");
        var selectedIds = InitialOrder(parameters.PreviewOutcome, options).ToList();
        var bankButtons = new Dictionary<string, Button>(StringComparer.Ordinal);

        var replayButton = new Button { Content = "Replay build", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(replayButton, "WordOrderTrainReplay");
        var skipButton = new Button { Content = "Skip build", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(skipButton, "WordOrderTrainSkip");
        var promptText = new TextBlock
        {
            Text = prompt,
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetName(promptText, $"Word order prompt. {prompt}");
        var sceneActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
        };
        sceneActions.Children.Add(replayButton);
        sceneActions.Children.Add(skipButton);
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        header.Children.Add(promptText);
        Grid.SetColumn(sceneActions, 1);
        header.Children.Add(sceneActions);

        var stage = TemplateRendering.CreateStage(320, "Word order train construction stage");
        TemplateRendering.AddBackdrop(stage, useScenicPreview: false);
        var tape = new PaperTape { Content = "BUILD THE REQUEST", Angle = -1.1 };
        PaperStage.SetLayer(tape, PaperStageLayer.TapedLabel);
        PaperStage.SetAnchor(tape, PaperAnchorLine.Head);
        PaperStage.SetAnchorX(tape, 0.2);
        PaperStage.SetAnchorOffsetY(tape, -12);
        stage.Children.Add(tape);

        var bankPanel = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            ItemHeight = 54,
        };
        var trainPanel = new WrapPanel
        {
            MinHeight = 70,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ItemHeight = 60,
        };
        AutomationProperties.SetName(bankPanel, "Available word cards");
        AutomationProperties.SetName(trainPanel, "Selected train cars in sentence order");

        var construction = new Grid
        {
            Margin = new Avalonia.Thickness(26, 72, 26, 22),
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"),
            RowSpacing = 8,
        };
        construction.Children.Add(new TextBlock
        {
            Text = "WORD BANK",
            Classes = { "eyebrow" },
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        Grid.SetRow(bankPanel, 1);
        construction.Children.Add(bankPanel);
        var rail = new Grid
        {
            Height = 24,
            Margin = new Avalonia.Thickness(8, 30, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        var upperRail = new Border { Height = 2, VerticalAlignment = VerticalAlignment.Top };
        upperRail.Classes.Add("divider");
        var sleepers = new TextBlock
        {
            Text = "▮   ▮   ▮   ▮   ▮   ▮   ▮   ▮   ▮   ▮   ▮   ▮   ▮",
            FontSize = 10,
            LetterSpacing = 1.5,
            Opacity = 0.28,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var lowerRail = new Border { Height = 2, VerticalAlignment = VerticalAlignment.Bottom };
        lowerRail.Classes.Add("divider");
        rail.Children.Add(upperRail);
        rail.Children.Add(sleepers);
        rail.Children.Add(lowerRail);
        Grid.SetRow(rail, 2);
        construction.Children.Add(rail);
        Grid.SetRow(trainPanel, 2);
        construction.Children.Add(trainPanel);
        var trainHint = new TextBlock
        {
            Text = "Select a word to add it. Select a train car to return it.",
            Classes = { "muted" },
            FontSize = 12,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };
        Grid.SetRow(trainHint, 3);
        construction.Children.Add(trainHint);
        PaperStage.SetLayer(construction, PaperStageLayer.Subject);
        stage.Children.Add(construction);

        foreach (var option in DeterministicBankOrder(options))
        {
            var button = new Button
            {
                Content = option.Label,
                Margin = new Avalonia.Thickness(4),
                Classes = { "lift" },
            };
            AutomationProperties.SetName(button, $"Add {option.Label} to the sentence");
            AutomationProperties.SetAutomationId(button, $"WordOrderBank_{option.Id}");
            button.Click += (_, _) =>
            {
                if (!selectedIds.Contains(option.Id, StringComparer.Ordinal))
                {
                    selectedIds.Add(option.Id);
                    RefreshTrain();
                }
            };
            bankButtons.Add(option.Id, button);
            bankPanel.Children.Add(button);
        }

        var outcomePanel = TemplateRendering.CreateOutcomePanel(
            parameters.PreviewOutcome,
            OutcomeCopy,
            out var outcomeText);
        var resetButton = new Button { Content = "Reset", Classes = { "quiet" } };
        AutomationProperties.SetAutomationId(resetButton, "WordOrderTrainReset");
        var checkButton = new Button { Content = "Check order", Classes = { "primary", "lift" } };
        AutomationProperties.SetAutomationId(checkButton, "WordOrderTrainCheck");
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
        };
        actions.Children.Add(resetButton);
        actions.Children.Add(checkButton);
        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        footer.Children.Add(outcomePanel);
        Grid.SetColumn(actions, 1);
        footer.Children.Add(actions);

        void RefreshTrain()
        {
            trainPanel.Children.Clear();
            foreach (var pair in bankButtons)
            {
                pair.Value.IsEnabled = !selectedIds.Contains(pair.Key, StringComparer.Ordinal);
            }

            foreach (var id in selectedIds)
            {
                var option = options.Single(candidate => candidate.Id == id);
                if (trainPanel.Children.Count > 0)
                {
                    trainPanel.Children.Add(new TextBlock
                    {
                        Text = "—",
                        FontSize = 17,
                        FontWeight = FontWeight.Bold,
                        VerticalAlignment = VerticalAlignment.Center,
                        Opacity = 0.55,
                    });
                }

                var carCopy = new StackPanel
                {
                    Spacing = 0,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                carCopy.Children.Add(new TextBlock
                {
                    Text = option.Label,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Classes = { "on-accent" },
                });
                carCopy.Children.Add(new TextBlock
                {
                    Text = "●     ●",
                    FontSize = 8,
                    LetterSpacing = 0.5,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Opacity = 0.82,
                    Classes = { "on-accent" },
                });
                var car = new Button
                {
                    MinWidth = 64,
                    Height = 58,
                    Padding = new Avalonia.Thickness(9, 4),
                    CornerRadius = new Avalonia.CornerRadius(6),
                    Content = carCopy,
                    Margin = new Avalonia.Thickness(3),
                    Classes = { "primary", "lift" },
                };
                AutomationProperties.SetName(car, $"Remove {option.Label} from the sentence");
                AutomationProperties.SetAutomationId(car, $"WordOrderCar_{option.Id}");
                car.Click += (_, _) =>
                {
                    selectedIds.Remove(option.Id);
                    RefreshTrain();
                };
                trainPanel.Children.Add(car);
            }

            if (selectedIds.Count == 0)
            {
                trainPanel.Children.Add(new TextBlock
                {
                    Text = "The paper train is waiting for its first word.",
                    Classes = { "muted" },
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                });
            }
        }

        RefreshTrain();
        resetButton.Click += (_, _) =>
        {
            selectedIds.Clear();
            RefreshTrain();
            TemplateRendering.ApplyOutcome(
                outcomePanel,
                outcomeText,
                TemplateOutcomeState.Ready,
                OutcomeCopy);
        };
        checkButton.Click += (_, _) =>
        {
            var outcome = TemplateInteractionEvaluator.EvaluateWordOrder(options, selectedIds);
            TemplateRendering.ApplyOutcome(outcomePanel, outcomeText, outcome.State, OutcomeCopy);
            reportOutcome(outcome);
        };

        var root = new StackPanel { Spacing = 12 };
        root.Children.Add(header);
        if (parameters.UseTextOnlyFallback)
        {
            root.Children.Add(new TextBlock
            {
                Text = "Text-only construction: every word and action remains keyboard operable.",
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(stage);
        root.Children.Add(footer);

        PaperChoreography? scene = null;
        async Task PlayAsync()
        {
            scene?.Skip();
            scene?.Dispose();
            TemplateRendering.Prepare(shouldReduceMotion, tape, bankPanel, trainPanel);
            if (!shouldReduceMotion)
            {
                bankPanel.RenderTransform = TemplateRendering.Transform(-18, 0, -1.2, 0.98);
                trainPanel.RenderTransform = TemplateRendering.Transform(24, 0, 1.1, 0.98);
            }

            scene = new PaperChoreography(
            [
                TemplateRendering.Reveal(TimeSpan.FromMilliseconds(250), tape),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(650), bankPanel, 0, 0, 0, 1),
                TemplateRendering.Move(TimeSpan.FromMilliseconds(650), trainPanel, 0, 0, 0, 1),
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
            tape.SkipEntrance();
        };
        replayButton.Click += async (_, _) => await PlayAsync();
        return root;
    }

    private static IReadOnlyList<TemplateOption> DeterministicBankOrder(
        IReadOnlyList<TemplateOption> options) =>
        options.Where((_, index) => index % 2 == 1)
            .Concat(options.Where((_, index) => index % 2 == 0))
            .ToArray();

    private static IReadOnlyList<string> InitialOrder(
        TemplateOutcomeState state,
        IReadOnlyList<TemplateOption> options) => state switch
        {
            TemplateOutcomeState.Success => options.Select(option => option.Id).ToArray(),
            TemplateOutcomeState.Failure => options.Reverse().Select(option => option.Id).ToArray(),
            TemplateOutcomeState.Uncertain => options.Take(Math.Max(1, options.Count / 2))
                .Select(option => option.Id)
                .ToArray(),
            _ => [],
        };

    private static string OutcomeCopy(TemplateOutcomeState state) => state switch
    {
        TemplateOutcomeState.Success => "The train reads in the authored order.",
        TemplateOutcomeState.Uncertain => "The train needs every word before it can be checked.",
        TemplateOutcomeState.Failure => "The words are all here, but the train order needs another pass.",
        _ => "Ready: build the sentence from left to right.",
    };
}
