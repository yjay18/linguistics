using Avalonia.Controls;
using Linguistics.App.Diagnostics;
using Linguistics.App.Content;
using Linguistics.App.Features.Onboarding;
using Linguistics.App.Features.Shell;
using Linguistics.App.Persistence;
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
    private Func<CancellationToken, Task<LearnerStoreRecoveryResult>>? _recoverLearnerStore;
    private LocalDiagnosticLog? _diagnosticLog;
    private ContentImageCache? _imageCache;
    private CancellationTokenSource? _loadCancellation;
    private bool _recoveryConfirmationPending;

    public MainWindow()
    {
        InitializeComponent();
        ApplyMotionPreference(savedPreference: false);
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
        SpeechRecordingStore? speechRecordingStore = null,
        Func<CancellationToken, Task<LearnerStoreRecoveryResult>>? recoverLearnerStore = null,
        LocalDiagnosticLog? diagnosticLog = null,
        ContentImageCache? imageCache = null)
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
        _recoverLearnerStore = recoverLearnerStore;
        _diagnosticLog = diagnosticLog;
        _imageCache = imageCache;
        Opened += OnOpened;
        Closed += OnClosed;
    }

    private async void OnOpened(object? sender, EventArgs args)
    {
        Opened -= OnOpened;
        await TryLogAsync(
            DiagnosticCategory.Application,
            DiagnosticEventCode.AppOpened,
            DiagnosticOutcome.Started);
        await LoadProfileAsync();
    }

    private async void OnRetryClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs args)
    {
        await LoadProfileAsync();
    }

    private async void OnRecoveryClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs args)
    {
        if (_recoverLearnerStore is null || _loadCancellation is null)
        {
            return;
        }

        if (!_recoveryConfirmationPending)
        {
            _recoveryConfirmationPending = true;
            StartupTitle.Text = "Preserve the unreadable file?";
            StartupMessage.Text =
                "Linguistics will move the original bytes into its Recovery folder, then allow a new local profile. The recovery copy is not deleted or reinterpreted.";
            RecoveryButton.Content = "Confirm preserve and start fresh";
            RetryButton.Content = "Cancel";
            RetryButton.IsVisible = true;
            return;
        }

        RecoveryButton.IsEnabled = false;
        RetryButton.IsEnabled = false;
        StartupProgress.IsVisible = true;
        try
        {
            var result = await _recoverLearnerStore(_loadCancellation.Token);
            await TryLogAsync(
                DiagnosticCategory.Persistence,
                DiagnosticEventCode.RecoveryPreserved,
                DiagnosticOutcome.Succeeded);
            _recoveryConfirmationPending = false;
            StartupProgress.IsVisible = false;
            StartupTitle.Text = "Recovery copy preserved";
            StartupMessage.Text =
                $"Preserved {result.PreservedFileCount} file(s) as {result.RecoveryFileName}. Continue to create a new local profile; the copy remains available for manual recovery.";
            RecoveryButton.IsVisible = false;
            RetryButton.Content = "Continue to setup";
            RetryButton.IsEnabled = true;
            RetryButton.IsVisible = true;
        }
        catch (OperationCanceledException)
        {
        }
        catch (LearnerStoreException exception)
        {
            StartupProgress.IsVisible = false;
            StartupTitle.Text = "Recovery could not be completed";
            StartupMessage.Text = exception.Message;
            RecoveryButton.Content = "Try preservation again";
            RecoveryButton.IsEnabled = true;
            RetryButton.Content = "Cancel";
            RetryButton.IsEnabled = true;
            RetryButton.IsVisible = true;
        }
    }

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
            await TryLogAsync(
                DiagnosticCategory.Persistence,
                DiagnosticEventCode.ProfileLoaded,
                DiagnosticOutcome.Succeeded);
            if (profile is null)
            {
                ApplyMotionPreference(savedPreference: false);
                ShowOnboarding();
            }
            else
            {
                ApplyMotionPreference(profile.Settings.ReduceMotion);
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
            await TryLogAsync(
                DiagnosticCategory.Persistence,
                DiagnosticEventCode.ProfileLoadFailed,
                DiagnosticOutcome.Failed);
            StartupProgress.IsVisible = false;
            StartupTitle.Text = "Your learning data could not be opened";
            StartupMessage.Text = exception.Message;
            RetryButton.IsVisible = true;
            RecoveryButton.IsVisible = _recoverLearnerStore is not null;
        }
    }

    private void ShowLoadingState()
    {
        StartupStatus.IsVisible = true;
        StartupProgress.IsVisible = true;
        StartupTitle.Text = "Opening Linguistics";
        StartupMessage.Text = "Loading your local learning profile.";
        RetryButton.Content = "Try again";
        RetryButton.IsEnabled = true;
        RetryButton.IsVisible = false;
        RecoveryButton.Content = "Preserve unreadable data and start fresh";
        RecoveryButton.IsEnabled = true;
        RecoveryButton.IsVisible = false;
        _recoveryConfirmationPending = false;
    }

    private void ShowShell(LearnerProfile profile)
    {
        if (_profileOwner is null)
        {
            return;
        }

        ApplyMotionPreference(profile.Settings.ReduceMotion);
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
            _speechRecordingStore,
            _diagnosticLog,
            _imageCache);
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

    private void ApplyMotionPreference(bool savedPreference) =>
        Classes.Set("motion-enabled", !MotionPreferences.ShouldReduce(savedPreference));

    private async Task TryLogAsync(
        DiagnosticCategory category,
        DiagnosticEventCode eventCode,
        DiagnosticOutcome outcome)
    {
        if (_diagnosticLog is null)
        {
            return;
        }

        try
        {
            await _diagnosticLog.WriteAsync(category, eventCode, outcome);
        }
        catch (DiagnosticLogException)
        {
        }
    }
}
