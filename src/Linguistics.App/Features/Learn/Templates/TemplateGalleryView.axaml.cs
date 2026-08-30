using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Linguistics.App.Controls;
using Linguistics.Core.Content;

namespace Linguistics.App.Features.Learn.Templates;

public partial class TemplateGalleryView : UserControl
{
    private readonly TemplateRegistry? _registry;
    private readonly IReadOnlyList<TemplateGalleryFixture> _fixtures = [];
    private readonly bool _shouldReduceMotion;

    public TemplateGalleryView()
    {
        InitializeComponent();
    }

    internal TemplateGalleryView(
        TemplateRegistry registry,
        IReadOnlyList<TemplateGalleryFixture> fixtures,
        bool shouldReduceMotion)
        : this()
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(fixtures);
        _registry = registry;
        _fixtures = fixtures;
        _shouldReduceMotion = shouldReduceMotion;
        RenderFixtures();
    }

    internal TemplateOutcomeState CurrentOutcome { get; private set; }

    internal bool UseTextOnlyFallback { get; private set; }

    private void OnNextOutcomeClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs args) =>
        CycleOutcome();

    private void OnTextOnlyChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs args)
    {
        UseTextOnlyFallback = TextOnlyCheckBox.IsChecked == true;
        RenderFixtures();
    }

    internal void CycleOutcome()
    {
        CurrentOutcome = CurrentOutcome switch
        {
            TemplateOutcomeState.Ready => TemplateOutcomeState.Success,
            TemplateOutcomeState.Success => TemplateOutcomeState.Uncertain,
            TemplateOutcomeState.Uncertain => TemplateOutcomeState.Failure,
            _ => TemplateOutcomeState.Ready,
        };
        RenderFixtures();
    }

    internal void SetTextOnlyFallback(bool value)
    {
        UseTextOnlyFallback = value;
        TextOnlyCheckBox.IsChecked = value;
        RenderFixtures();
    }

    private void RenderFixtures()
    {
        OutcomeText.Text = $"Outcome preview: {CurrentOutcome}";
        FixturePanel.Children.Clear();
        EmptyState.IsVisible = _fixtures.Count == 0;
        if (_registry is null)
        {
            return;
        }

        foreach (var fixture in _fixtures)
        {
            var header = new StackPanel { Spacing = 4 };
            header.Children.Add(new TextBlock
            {
                Text = fixture.Title,
                FontSize = 21,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
            });
            header.Children.Add(new TextBlock
            {
                Text = fixture.Family,
                Classes = { "muted" },
            });

            var parameters = fixture.Parameters with
            {
                PreviewOutcome = CurrentOutcome,
                UseTextOnlyFallback = UseTextOnlyFallback,
            };
            var host = new ContentControl
            {
                MinHeight = 420,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Content = _registry.Render(
                    fixture.TemplateId,
                    parameters,
                    fixture.InstructionLanguage,
                    _shouldReduceMotion,
                    outcome => OutcomeText.Text = $"Reported outcome: {outcome.State}"),
            };
            AutomationProperties.SetName(host, $"{fixture.Title} synthetic fixture");

            var content = new StackPanel { Spacing = 14 };
            content.Children.Add(header);
            content.Children.Add(host);
            var card = new PaperCard { Content = content };
            FixturePanel.Children.Add(card);
        }
    }
}
