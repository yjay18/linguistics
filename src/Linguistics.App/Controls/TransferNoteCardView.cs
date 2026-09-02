using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Linguistics.App.Controls;

internal sealed record TransferNoteCardContent(
    string SourceLanguage,
    string NoteType,
    string Explanation,
    IReadOnlyList<string> Risks,
    bool RequiresConfirmation,
    string DismissLabel);

internal sealed class TransferNoteCardView : ContentControl
{
    private readonly PaperTape _badge;
    private readonly CheckBox _confirmation;

    public TransferNoteCardView(
        TransferNoteCardContent content,
        string automationPrefix)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(automationPrefix);

        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        _badge = new PaperTape
        {
            Content = $"{content.SourceLanguage.ToUpperInvariant()} · {content.NoteType.ToUpperInvariant()}",
            Angle = -1.1,
            Classes = { "compact" },
        };
        AutomationProperties.SetName(
            _badge,
            $"Source language {content.SourceLanguage}. Note type {content.NoteType}.");

        var dismiss = new Button
        {
            Content = content.DismissLabel,
            Classes = { "quiet" },
            VerticalAlignment = VerticalAlignment.Center,
        };
        AutomationProperties.SetAutomationId(dismiss, $"{automationPrefix}DismissBridge");
        AutomationProperties.SetName(dismiss, $"{content.DismissLabel}. Continue without this language note.");
        dismiss.Click += (_, _) => Dismissed?.Invoke(this, EventArgs.Empty);

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 12,
        };
        header.Children.Add(_badge);
        Grid.SetColumn(dismiss, 1);
        header.Children.Add(dismiss);

        var body = new StackPanel { Spacing = 8 };
        AutomationProperties.SetAutomationId(body, $"{automationPrefix}TransferExplanation");
        AutomationProperties.SetName(body, $"Transfer explanation. {content.Explanation}");
        foreach (var block in BodyBlocks(content.Explanation))
        {
            var explanation = new TextBlock
            {
                Text = block,
                FontSize = 17,
                LineHeight = 23,
                TextWrapping = TextWrapping.Wrap,
            };
            body.Children.Add(explanation);
        }

        if (content.Risks.Count > 0)
        {
            var riskStack = new StackPanel { Spacing = 5 };
            riskStack.Children.Add(new TextBlock
            {
                Text = "Keep in mind",
                FontSize = 12,
                FontWeight = FontWeight.Bold,
            });
            foreach (var risk in content.Risks.SelectMany(BodyBlocks))
            {
                riskStack.Children.Add(new TextBlock
                {
                    Text = risk,
                    FontSize = 13,
                    LineHeight = 18,
                    TextWrapping = TextWrapping.Wrap,
                });
            }

            var riskCard = new Border
            {
                Padding = new Avalonia.Thickness(12, 9),
                Child = riskStack,
                Classes = { "soft-card" },
            };
            AutomationProperties.SetName(
                riskCard,
                $"Transfer caution. {string.Join(" ", content.Risks)}");
            body.Children.Add(riskCard);
        }

        _confirmation = new CheckBox
        {
            Content = "Use this language bridge for this activity",
            IsVisible = content.RequiresConfirmation,
        };
        AutomationProperties.SetAutomationId(_confirmation, $"{automationPrefix}ConfirmBridge");
        AutomationProperties.SetName(_confirmation, "Confirm use of this routed language bridge");
        body.Children.Add(_confirmation);
        if (!content.RequiresConfirmation)
        {
            body.Children.Add(new TextBlock
            {
                Text = "Shown from your saved language bridge preference.",
                FontSize = 13,
                Classes = { "muted" },
                TextWrapping = TextWrapping.Wrap,
            });
        }

        var layout = new StackPanel { Spacing = 12 };
        layout.Children.Add(header);
        layout.Children.Add(body);
        var card = new PaperCard
        {
            Content = layout,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
        };
        card.Classes.Add("accent-card");
        Content = card;
        AutomationProperties.SetAutomationId(this, $"{automationPrefix}TransferNote");
        AutomationProperties.SetName(
            this,
            $"{content.SourceLanguage} {content.NoteType} transfer note. {content.Explanation}");
    }

    public event EventHandler? Dismissed;

    public bool IsConfirmed => _confirmation.IsChecked == true;

    public void SkipEntrance() => _badge.SkipEntrance();

    private static IEnumerable<string> BodyBlocks(string text)
    {
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        for (var start = 0; start < words.Length; start += 25)
        {
            yield return string.Join(' ', words.Skip(start).Take(25));
        }
    }
}
