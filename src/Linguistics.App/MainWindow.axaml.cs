using Avalonia.Controls;
using Linguistics.App.Features.Onboarding;
using Linguistics.App.Features.Shell;
using Linguistics.App.Speech;
using Linguistics.Core.Content;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Providers;
using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.App;

public partial class MainWindow : Window
{
    private LearnerProfileOwner? _profileOwner;
    private ValidatedContentCatalog? _runtimeContentCatalog;
    private string? _runtimeContentError;
    private ValidatedContentCatalog? _authoringContentCatalog;
    private string? _authoringContentError;
    private ILanguageModelProvider? _languageModelProvider;
    private ISpeechSynthesisProvider? _speechSynthesisProvider;
    private ISpeechRecognitionProvider? _speechRecognitionProvider;
    private IPronunciationAssessmentProvider? _pronunciationAssessmentProvider;
    private SpeechRecordingStore? _speechRecordingStore;
    private CancellationTokenSource? _loadCancellation;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(
        LearnerProfileOwner profileOwner,
        ValidatedContentCatalog? runtimeContentCatalog = null,
        string? runtimeContentError = null,
        ValidatedContentCatalog? authoringContentCatalog = null,
        string? authoringContentError = null,
        ILanguageModelProvider? languageModelProvider = null,
        ISpeechSynthesisProvider? speechSynthesisProvider = null,
        ISpeechRecognitionProvider? speechRecognitionProvider = null,
        IPronunciationAssessmentProvider? pronunciationAssessmentProvider = null,
        SpeechRecordingStore? speechRecordingStore = null)
        : this()
    {
        _profileOwner = profileOwner;
        _runtimeContentCatalog = runtimeContentCatalog;
        _runtimeContentError = runtimeContentError;
        _authoringContentCatalog = authoringContentCatalog;
        _authoringContentError = authoringContentError;
        _languageModelProvider = languageModelProvider;
        _speechSynthesisProvider = speechSynthesisProvider;
        _speechRecognitionProvider = speechRecognitionProvider;
        _pronunciationAssessmentProvider = pronunciationAssessmentProvider;
        _speechRecordingStore = speechRecordingStore;
        Opened += OnOpened;
        Closed += OnClosed;
    }

    private async void OnOpened(object? sender, EventArgs args)
    {
        Opened -= OnOpened;
        await LoadProfileAsync();
    }

    private async void OnRetryClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs args) =>
        await LoadProfileAsync();

    private async Task LoadProfileAsync()
    {
        if (_profileOwner is null)
        {
            return;
        }

        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();

        ShowLoadingState();
        try
        {
            var profile = await _profileOwner.RestoreAsync(_loadCancellation.Token);
            if (profile is null)
            {
                ShowOnboarding();
            }
            else
            {
                ShowShell(profile);
            }

            StartupStatus.IsVisible = false;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception) when (
            exception is LearnerStoreException or
            LearnerProfileValidationException or
            CurriculumValidationException)
        {
            StartupProgress.IsVisible = false;
            StartupTitle.Text = "Your learning data could not be opened";
            StartupMessage.Text = exception.Message;
            RetryButton.IsVisible = true;
        }
    }

    private void ShowLoadingState()
    {
        StartupStatus.IsVisible = true;
        StartupProgress.IsVisible = true;
        StartupTitle.Text = "Opening Linguistics";
        StartupMessage.Text = "Loading your local learning profile.";
        RetryButton.IsVisible = false;
    }

    private void ShowShell(LearnerProfile profile)
    {
        if (_profileOwner is null)
        {
            return;
        }

        RootContent.Content = new ShellView(
            profile,
            _profileOwner,
            ShowOnboarding,
            _runtimeContentCatalog,
            _runtimeContentError,
            _authoringContentCatalog,
            _authoringContentError,
            _languageModelProvider,
            _speechSynthesisProvider,
            _speechRecognitionProvider,
            _pronunciationAssessmentProvider,
            _speechRecordingStore);
        StartupStatus.IsVisible = false;
    }

    private void ShowOnboarding()
    {
        if (_profileOwner is null)
        {
            return;
        }

        RootContent.Content = new OnboardingView(_profileOwner, ShowShell);
        StartupStatus.IsVisible = false;
    }

    private void OnClosed(object? sender, EventArgs args)
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
    }
}
