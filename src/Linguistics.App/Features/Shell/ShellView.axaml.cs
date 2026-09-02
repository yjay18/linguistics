using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Threading;
using Linguistics.App.Diagnostics;
using Linguistics.App.Content;
using Linguistics.App.Features.Developer;
using Linguistics.App.Persistence;
using Linguistics.App.Features.Languages;
using Linguistics.App.Features.Learn;
using Linguistics.App.Features.Learn.Templates;
using Linguistics.App.Features.Pronunciation;
using Linguistics.App.Features.Progress;
using Linguistics.App.Features.Review;
using Linguistics.App.Features.Scenarios;
using Linguistics.App.Features.Settings;
using Linguistics.App.Features.Today;
using Linguistics.App.Localization;
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
    private ContentImageCache? _imageCache;

    public ShellView()
    {
        InitializeComponent();
        NavigationList.SelectionChanged += OnNavigationChanged;
        AttachedToVisualTree += (_, _) => ApplyMotionPreference();
        TemplateGalleryNavItem.IsVisible = DeveloperModeEnabled();
        PaperStageNavItem.IsVisible = DeveloperModeEnabled();
        NavigationList.SelectedItem = RequestedDeveloperPage() switch
        {
            "TEMPLATES" or "TEMPLATEGALLERY" => TemplateGalleryNavItem,
            "PAPERSTAGE" => PaperStageNavItem,
            "TODAY" => TodayNavItem,
            "PROGRESS" => ProgressNavItem,
            "SETTINGS" or "SETTINGSBOTTOM" => SettingsNavItem,
            _ when DeveloperModeEnabled() => LearnNavItem,
            _ => TodayNavItem,
        };
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
        LocalDiagnosticLog? diagnosticLog = null,
        ContentImageCache? imageCache = null)
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
        _imageCache = imageCache;
        ApplyAppLanguage();
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

        var destination = GetDestination(item);
        RefreshPageHeader(destination);

        if (_profile is null || _profileOwner is null)
        {
            ShowUnavailable();
            return;
        }

        switch (destination)
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
                ShowPage(new LanguagesView(
                    _profile,
                    SaveProfileAsync,
                    _runtimeContentCatalog ?? _authoringContentCatalog));
                break;
            case "Learn" when _runtimeContentCatalog is not null:
                ShowPage(new LearnView(
                    _profile,
                    _runtimeContentCatalog,
                    _runtimeContentError,
                    _profileOwner,
                    imageCache: _imageCache,
                    speechSynthesisProvider: _speechSynthesisProvider,
                    speechRecognitionProvider: _speechRecognitionProvider,
                    pronunciationAssessmentProvider: _pronunciationAssessmentProvider));
                break;
            case "Learn" when DeveloperModeEnabled():
                ShowPage(new LearnView(
                    _profile,
                    _authoringContentCatalog,
                    _authoringContentError,
                    _profileOwner,
                    showDeveloperDetails: true,
                    imageCache: _imageCache,
                    speechSynthesisProvider: _speechSynthesisProvider,
                    speechRecognitionProvider: _speechRecognitionProvider,
                    pronunciationAssessmentProvider: _pronunciationAssessmentProvider));
                break;
            case "Scenarios":
                ShowPage(new CafeOrderView(
                    _profile,
                    _profileOwner,
                    _runtimeContentCatalog,
                    _runtimeContentError,
                    _languageModelProvider,
                    _speechSynthesisProvider,
                    _speechRecognitionProvider,
                    _imageCache));
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
                    _diagnosticLog,
                    _imageCache));
                break;
            case "Progress":
                ShowPage(new ProgressView(
                    _profile,
                    _profileOwner,
                    _runtimeContentCatalog,
                    _diagnosticLog,
                    _imageCache));
                break;
            case "Settings":
                ShowPage(new SettingsView(
                    _profile,
                    SaveProfileAsync,
                    DeleteProfileAsync,
                    _languageModelProvider,
                    _speechSynthesisProvider,
                    _speechRecognitionProvider,
                    _speechRecordingStore,
                    _imageCache?.Assets));
                break;
            case "TemplateGallery" when DeveloperModeEnabled():
                ShowPage(new TemplateGalleryView(
                    TemplateRegistry.CreateDefault(
                        _imageCache,
                        _speechSynthesisProvider,
                        _speechRecognitionProvider,
                        _pronunciationAssessmentProvider,
                        _profile.Settings.Microphone != MicrophonePreference.Never),
                    TemplateGalleryFixtures.All,
                    MotionPreferences.ShouldReduce(_profile.Settings.ReduceMotion),
                    _imageCache));
                break;
            case "PaperStage" when DeveloperModeEnabled():
                ShowPage(new PaperStageSandboxView(_imageCache));
                break;
            default:
                ShowUnavailable();
                break;
        }
    }

    private async Task<LearnerProfile> SaveProfileAsync(LearnerProfile profile)
    {
        _profile = await _profileOwner!.UpdateAsync(profile);
        ApplyAppLanguage();
        if (NavigationList.SelectedItem is ListBoxItem item)
        {
            RefreshPageHeader(GetDestination(item));
        }
        ApplyMotionPreference();
        return _profile;
    }

    private void ApplyAppLanguage()
    {
        if (_profile is null)
        {
            AppStrings.UseLanguage(new LanguageCode("en"));
            return;
        }

        var catalog = _runtimeContentCatalog ?? _authoringContentCatalog;
        var instructionLanguage = catalog?
            .SelectInstructionLanguage(_profile)
            .SelectedLanguage;
        AppStrings.UseLanguage(AppLanguageSelector.Select(_profile, instructionLanguage));
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
        QueueScrollPosition();
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
                string.Equals(GetDestination(candidate), destination, StringComparison.Ordinal));
        if (item is not null)
        {
            NavigationList.SelectedItem = item;
        }
    }

    private void ApplyMotionPreference()
    {
        var reduceMotion = MotionPreferences.ShouldReduce(_profile?.Settings.ReduceMotion == true);
        PageContent.PageTransition = new CrossFade(
            MotionPreferences.PageTransitionDuration(reduceMotion));
        TopLevel.GetTopLevel(this)?.Classes.Set("motion-enabled", !reduceMotion);
    }

    private static bool DeveloperModeEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable("LINGUISTICS_DEVELOPER_MODE"),
            "1",
            StringComparison.Ordinal);

    private void QueueScrollPosition() =>
        Dispatcher.UIThread.Post(
            () =>
            {
                var bottom = DeveloperModeEnabled() && RequestedDeveloperPage() == "SETTINGSBOTTOM";
                MainScrollViewer.Offset = new Vector(
                    0,
                    bottom
                        ? Math.Max(0, MainScrollViewer.Extent.Height - MainScrollViewer.Viewport.Height)
                        : 0);
            },
            DispatcherPriority.Loaded);

    private static string? RequestedDeveloperPage() =>
        DeveloperModeEnabled()
            ? Environment
                .GetEnvironmentVariable("LINGUISTICS_DEVELOPER_PAGE")
                ?.Trim()
                .ToUpperInvariant()
            : null;

    private void RefreshPageHeader(string destination)
    {
        if (destination is "TemplateGallery" or "PaperStage")
        {
            PageTitle.Text = destination == "TemplateGallery"
                ? "Template gallery"
                : "Paper stage";
            PageDescription.Text = destination == "TemplateGallery"
                ? "Inspect template outcomes, motion settings, and text only presentation."
                : "Inspect stage layers, anchor lines, themes, and motion paths.";
            return;
        }

        PageTitle.Text = AppStrings.Get($"Nav_{destination}_Title");
        PageDescription.Text = AppStrings.Get($"Nav_{destination}_Description");
    }

    private static string GetDestination(ListBoxItem item) =>
        item.Tag?.ToString() ?? "Today";
}
