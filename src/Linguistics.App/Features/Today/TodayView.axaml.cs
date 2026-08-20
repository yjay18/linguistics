using Avalonia.Controls;
using Avalonia.Interactivity;
using Linguistics.App.Features.Review;
using Linguistics.Core.Content;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Today;

public partial class TodayView : UserControl
{
    private ReviewController? _controller;
    private Action<string>? _navigate;
    private TodayAction _action;
    private bool _initialized;

    public TodayView()
    {
        InitializeComponent();
        AttachedToVisualTree += async (_, _) => await InitializeAsync();
    }

    public TodayView(
        LearnerProfile profile,
        LearnerProfileOwner profileOwner,
        ValidatedContentCatalog? contentCatalog,
        Action<string> navigate)
        : this()
    {
        var graph = contentCatalog?.CreateRuntimeConceptGraph(profile.TargetLanguage);
        _controller = new ReviewController(profileOwner, graph);
        _navigate = navigate;
    }

    private async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        if (_controller is null)
        {
            ShowError("Today is unavailable because the learning service was not initialized.");
            return;
        }

        try
        {
            Render(await _controller.InitializeAsync());
        }
        catch (Exception exception) when (
            exception is LearnerStoreException or CurriculumValidationException or ArgumentException)
        {
            ShowError(exception.Message);
        }
    }

    private void OnPrimaryActionClicked(object? sender, RoutedEventArgs args)
    {
        var destination = _action switch
        {
            TodayAction.Review => "Review",
            TodayAction.Pronunciation => "Pronunciation",
            _ => "Scenarios",
        };
        _navigate?.Invoke(destination);
    }

    private void Render(LearningSnapshot snapshot)
    {
        LoadingState.IsVisible = false;
        PlanCard.IsVisible = true;
        EvidenceGrid.IsVisible = true;
        _action = snapshot.Today.PrimaryAction;
        HeadlineText.Text = snapshot.Today.Headline;
        ExplanationText.Text = snapshot.Today.Explanation;
        DueCountText.Text = snapshot.Progress.DueReviewCount.ToString();
        StrongCountText.Text = snapshot.Progress.StrongConceptCount.ToString();
        SpeechCountText.Text = snapshot.Progress.PronunciationPracticeCount.ToString();
        (PrimaryActionButton.Content, ActionGlyph.Text) = _action switch
        {
            TodayAction.Review => ("Open review", "↻"),
            TodayAction.Pronunciation => ("Open pronunciation", "◌"),
            _ => ("Enter the café", "→"),
        };
    }

    private void ShowError(string message)
    {
        LoadingState.IsVisible = false;
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
