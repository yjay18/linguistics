using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Linguistics.App.Content;
using Linguistics.App.Controls;
using Linguistics.Core.Content;

namespace Linguistics.App.Features.Learn.Templates;

public partial class TemplateGalleryView : UserControl
{
    private readonly TemplateRegistry? _registry;
    private readonly IReadOnlyList<TemplateGalleryFixture> _fixtures = [];
    private readonly bool _shouldReduceMotion;
    private readonly ContentImageCache? _imageCache;

    public TemplateGalleryView()
    {
        InitializeComponent();
    }

    internal TemplateGalleryView(
        TemplateRegistry registry,
        IReadOnlyList<TemplateGalleryFixture> fixtures,
        bool shouldReduceMotion,
        ContentImageCache? imageCache = null)
        : this()
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(fixtures);
        _registry = registry;
        _fixtures = fixtures;
        _shouldReduceMotion = shouldReduceMotion;
        _imageCache = imageCache;
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
        RenderSeedBatch();
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

    private void RenderSeedBatch()
    {
        SeedAssetPanel.Children.Clear();
        SeedCreditsHost.Content = null;
        if (_imageCache is null)
        {
            SeedBatchCard.IsVisible = false;
            return;
        }

        SeedBatchCard.IsVisible = true;
        var seedAssets = _imageCache.Assets
            .Where(asset => asset.Record.Provenance == ContentAssetProvenance.WikimediaCommons)
            .OrderBy(asset => asset.Record.Id, StringComparer.Ordinal)
            .ToArray();
        if (UseTextOnlyFallback)
        {
            SeedAssetPanel.ItemWidth = double.NaN;
            SeedAssetPanel.ItemHeight = double.NaN;
            SeedAssetPanel.Children.Add(new TextBlock
            {
                Text = $"Text-only fallback active · {seedAssets.Length} local seed images intentionally hidden.",
                TextWrapping = TextWrapping.Wrap,
                Classes = { "muted" },
            });
            return;
        }

        SeedAssetPanel.ItemWidth = 174;
        SeedAssetPanel.ItemHeight = 174;
        var renderedIds = new List<string>(seedAssets.Length);
        foreach (var asset in seedAssets)
        {
            var image = TemplateRendering.CreateContentImage(
                _imageCache,
                asset.Record.Id,
                height: 112,
                Stretch.Uniform);
            if (image is null)
            {
                continue;
            }

            var subject = asset.Record.Id.Split('.').Last().Replace('-', ' ');
            var title = char.ToUpperInvariant(subject[0]) + subject[1..];
            var content = new StackPanel { Spacing = 6 };
            content.Children.Add(image);
            content.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                MaxLines = 2,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.Wrap,
            });
            var frame = new CutoutFrame
            {
                Width = 162,
                Height = 162,
                Margin = new Avalonia.Thickness(4),
                Content = content,
            };
            frame.Classes.Add(renderedIds.Count % 2 == 0 ? "tilt-left" : "tilt-right");
            AutomationProperties.SetName(frame, $"Seed asset {title}");
            SeedAssetPanel.Children.Add(frame);
            renderedIds.Add(asset.Record.Id);
        }

        SeedCreditsHost.Content = TemplateRendering.CreateCreditsDisclosure(
            _imageCache,
            renderedIds,
            "TemplateGallerySeedCredits");
    }
}
