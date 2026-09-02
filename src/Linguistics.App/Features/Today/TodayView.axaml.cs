using Avalonia.Controls;
using Avalonia.Interactivity;
using Linguistics.App.Diagnostics;
using Linguistics.App.Features.Review;
using Linguistics.App.Localization;
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
        Action<string> navigate,
        LocalDiagnosticLog? diagnosticLog = null)
        : this()
    {
        var instructionLanguage = contentCatalog?
            .SelectInstructionLanguage(profile)
            .SelectedLanguage;
        var graph = instructionLanguage is null
            ? null
            : contentCatalog!.CreateRuntimeConceptGraph(
                profile.TargetLanguage,
                instructionLanguage.Value);
        _controller = new ReviewController(profileOwner, graph, diagnosticLog: diagnosticLog);
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
            ShowError(AppStrings.Get("Today_Unavailable"));
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
        (HeadlineText.Text, ExplanationText.Text) = _action switch
        {
            TodayAction.Review => (
                AppStrings.Format("Today_Review_Headline", snapshot.Progress.DueReviewCount),
                AppStrings.Get("Today_Review_Explanation")),
            TodayAction.Pronunciation => (
                AppStrings.Get("Today_Pronunciation_Headline"),
                AppStrings.Get("Today_Pronunciation_Explanation")),
            _ when snapshot.Progress.Capabilities.All(item =>
                item.Status == CapabilityStatus.NotStarted) => (
                    AppStrings.Get("Today_FirstScenario_Headline"),
                    AppStrings.Get("Today_FirstScenario_Explanation")),
            _ => (
                AppStrings.Get("Today_Scenario_Headline"),
                AppStrings.Get("Today_Scenario_Explanation")),
        };
        DueCountText.Text = snapshot.Progress.DueReviewCount.ToString();
        StrongCountText.Text = snapshot.Progress.StrongConceptCount.ToString();
        SpeechCountText.Text = snapshot.Progress.PronunciationPracticeCount.ToString();
        (PrimaryActionButton.Content, ActionGlyph.Content) = _action switch
        {
            TodayAction.Review => (AppStrings.Get("Today_OpenReview"), "↻"),
            TodayAction.Pronunciation => (AppStrings.Get("Today_OpenPronunciation"), "◌"),
            _ => (AppStrings.Get("Today_EnterCafe"), "→"),
        };
    }

    private void ShowError(string message)
    {
        LoadingState.IsVisible = false;
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
