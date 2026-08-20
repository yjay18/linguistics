using Avalonia.Animation;
using Avalonia.Controls;
using Linguistics.App.Diagnostics;
using Linguistics.App.Persistence;
using Linguistics.App.Features.Languages;
using Linguistics.App.Features.Learn;
using Linguistics.App.Features.Pronunciation;
using Linguistics.App.Features.Progress;
using Linguistics.App.Features.Review;
using Linguistics.App.Features.Scenarios;
using Linguistics.App.Features.Settings;
using Linguistics.App.Features.Today;
using Linguistics.App.Speech;
using Linguistics.Core.Content;
using Linguistics.Core.Providers;
using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.App.Features.Shell;

public partial class ShellView : UserControl
{
    private LearnerProfile? _profile;
    private LearnerProfileOwner? _profileOwner;
    private Action? _profileDeleted;
    private ValidatedContentCatalog? _runtimeContentCatalog;
    private string? _runtimeContentError;
    private ValidatedContentCatalog? _authoringContentCatalog;
    private string? _authoringContentError;
    private ILanguageModelProvider? _languageModelProvider;
    private ISpeechSynthesisProvider? _speechSynthesisProvider;
    private ISpeechRecognitionProvider? _speechRecognitionProvider;
    private IPronunciationAssessmentProvider? _pronunciationAssessmentProvider;
    private SpeechRecordingStore? _speechRecordingStore;
    private LocalDiagnosticLog? _diagnosticLog;

    public ShellView()
    {
        InitializeComponent();
        NavigationList.SelectionChanged += OnNavigationChanged;
        AttachedToVisualTree += (_, _) => ApplyMotionPreference();
        NavigationList.SelectedIndex = 0;
    }

    public ShellView(
        LearnerProfile profile,
        LearnerProfileOwner profileOwner,
        Action profileDeleted,
        ValidatedContentCatalog? runtimeContentCatalog = null,
        string? runtimeContentError = null,
        ValidatedContentCatalog? authoringContentCatalog = null,
        string? authoringContentError = null,
        ILanguageModelProvider? languageModelProvider = null,
        ISpeechSynthesisProvider? speechSynthesisProvider = null,
        ISpeechRecognitionProvider? speechRecognitionProvider = null,
        IPronunciationAssessmentProvider? pronunciationAssessmentProvider = null,
        SpeechRecordingStore? speechRecordingStore = null,
        LocalDiagnosticLog? diagnosticLog = null)
        : this()
    {
        _profile = profile;
        _profileOwner = profileOwner;
        _profileDeleted = profileDeleted;
        _runtimeContentCatalog = runtimeContentCatalog;
        _runtimeContentError = runtimeContentError;
        _authoringContentCatalog = authoringContentCatalog;
        _authoringContentError = authoringContentError;
        _languageModelProvider = languageModelProvider;
        _speechSynthesisProvider = speechSynthesisProvider;
        _speechRecognitionProvider = speechRecognitionProvider;
        _pronunciationAssessmentProvider = pronunciationAssessmentProvider;
        _speechRecordingStore = speechRecordingStore;
        _diagnosticLog = diagnosticLog;
        ApplyMotionPreference();
        ShowSelectedPage();
    }

    private void OnNavigationChanged(object? sender, SelectionChangedEventArgs args)
        => ShowSelectedPage();

    private void ShowSelectedPage()
    {
        if (NavigationList.SelectedItem is not ListBoxItem item)
        {
            return;
        }

        PageTitle.Text = item.Content?.ToString() ?? "Linguistics";
        PageDescription.Text = item.Tag?.ToString() ?? "This area is not available yet.";

        if (_profile is null || _profileOwner is null)
        {
            ShowUnavailable();
            return;
        }

        switch (item.Content?.ToString())
        {
            case "Today":
                ShowPage(new TodayView(
                    _profile,
                    _profileOwner,
                    _runtimeContentCatalog,
                    NavigateTo,
                    _diagnosticLog));
                break;
            case "Languages":
                ShowPage(new LanguagesView(_profile, SaveProfileAsync));
                break;
            case "Learn" when DeveloperModeEnabled():
                ShowPage(new CurriculumDiagnosticsView(
                    _profile,
                    _authoringContentCatalog,
                    _authoringContentError,
                    _profileOwner));
                break;
            case "Scenarios":
                ShowPage(new CafeOrderView(
                    _profile,
                    _profileOwner,
                    _runtimeContentCatalog,
                    _runtimeContentError,
                    _languageModelProvider,
                    _speechSynthesisProvider,
                    _speechRecognitionProvider));
                break;
            case "Pronunciation" when
                _speechSynthesisProvider is not null &&
                _speechRecognitionProvider is not null &&
                _pronunciationAssessmentProvider is not null:
                ShowPage(new PronunciationView(
                    _profile,
                    _profileOwner,
                    _runtimeContentCatalog,
                    _runtimeContentError,
                    _speechSynthesisProvider,
                    _speechRecognitionProvider,
                    _pronunciationAssessmentProvider));
                break;
            case "Review":
                ShowPage(new ReviewView(
                    _profile,
                    _profileOwner,
                    _runtimeContentCatalog,
                    _runtimeContentError,
                    _diagnosticLog));
                break;
            case "Progress":
                ShowPage(new ProgressView(
                    _profile,
                    _profileOwner,
                    _runtimeContentCatalog,
                    _diagnosticLog));
                break;
            case "Settings":
                ShowPage(new SettingsView(
                    _profile,
                    SaveProfileAsync,
                    DeleteProfileAsync,
                    _languageModelProvider,
                    _speechSynthesisProvider,
                    _speechRecognitionProvider,
                    _speechRecordingStore));
                break;
            default:
                ShowUnavailable();
                break;
        }
    }

    private async Task<LearnerProfile> SaveProfileAsync(LearnerProfile profile)
    {
        _profile = await _profileOwner!.UpdateAsync(profile);
        ApplyMotionPreference();
        return _profile;
    }

    private async Task DeleteProfileAsync()
    {
        await LocalLearningDataDeletion.DeleteAllAsync(
            _profileOwner!,
            _speechRecordingStore,
            _diagnosticLog);
        _profile = null;
        _profileDeleted?.Invoke();
    }

    private void ShowPage(Control page)
    {
        PageContent.Content = page;
        PageContent.IsVisible = true;
        UnavailableState.IsVisible = false;
    }

    private void ShowUnavailable()
    {
        PageContent.Content = null;
        PageContent.IsVisible = false;
        UnavailableState.IsVisible = true;
    }

    private void NavigateTo(string destination)
    {
        var item = NavigationList.Items
            .OfType<ListBoxItem>()
            .SingleOrDefault(candidate =>
                string.Equals(candidate.Content?.ToString(), destination, StringComparison.Ordinal));
        if (item is not null)
        {
            NavigationList.SelectedItem = item;
        }
    }

    private void ApplyMotionPreference()
    {
        var reduceMotion = MotionPreferences.ShouldReduce(_profile?.Settings.ReduceMotion == true);
        PageContent.PageTransition = reduceMotion
            ? null
            : new CrossFade(TimeSpan.FromMilliseconds(180));
        TopLevel.GetTopLevel(this)?.Classes.Set("motion-enabled", !reduceMotion);
    }

    private static bool DeveloperModeEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable("LINGUISTICS_DEVELOPER_MODE"),
            "1",
            StringComparison.Ordinal);
}
