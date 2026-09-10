using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Linguistics.App.Diagnostics;
using Linguistics.App.Content;
using Linguistics.App.Features.Developer;
using Linguistics.App.Features.Onboarding;
using Linguistics.App.Features.Shell;
using Linguistics.App.Localization;
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
    private StartupPerformanceSnapshot? _startupPerformance;
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
        ContentImageCache? imageCache = null,
        StartupPerformanceSnapshot? startupPerformance = null)
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
        _startupPerformance = startupPerformance;
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
        if (_startupPerformance is { } performance)
        {
            await TryLogAsync(
                DiagnosticCategory.Curriculum,
                DiagnosticEventCode.ContentCatalogLoaded,
                performance.ContentCatalogLoadOutcome,
                performance.ContentCatalogLoadDuration);
        }

        await LoadProfileAsync();
        var processEntryToReadyDuration = TimeSpan.Zero;
        if (_startupPerformance is { } completedPerformance)
        {
            processEntryToReadyDuration = Stopwatch.GetElapsedTime(
                completedPerformance.ProcessStartedAtTimestamp);
            await TryLogAsync(
                DiagnosticCategory.Application,
                DiagnosticEventCode.AppOpened,
                DiagnosticOutcome.Succeeded,
                processEntryToReadyDuration);
        }

        QueueDeveloperCaptures(processEntryToReadyDuration);
    }

    private void QueueDeveloperCaptures(TimeSpan processEntryToReadyDuration)
    {
        var galleryOutputPath = TemplateGalleryCapture.RequestedOutputPath();
        var performanceOutputPath = PerformanceEvidenceCapture.RequestedOutputPath();
        if (galleryOutputPath is null && performanceOutputPath is null)
        {
            return;
        }

        Dispatcher.UIThread.Post(
            () =>
            {
                var lifetime = Application.Current?.ApplicationLifetime as
                    IClassicDesktopStyleApplicationLifetime;
                _ = CaptureAndShutdownAsync(lifetime);
            },
            DispatcherPriority.Background);

        async Task CaptureAndShutdownAsync(IClassicDesktopStyleApplicationLifetime? lifetime)
        {
            var errorPath = (galleryOutputPath ?? performanceOutputPath)! + ".error.txt";
            try
            {
                if (galleryOutputPath is not null)
                {
                    TemplateGalleryCapture.Save(this, galleryOutputPath);
                }

                if (performanceOutputPath is not null)
                {
                    if (_startupPerformance is null || _imageCache is null)
                    {
                        throw new InvalidOperationException(
                            "Performance capture requires startup and image-cache aggregates.");
                    }

                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await PerformanceEvidenceCapture.SaveAsync(
                        this,
                        performanceOutputPath,
                        processEntryToReadyDuration,
                        _startupPerformance,
                        _imageCache,
                        timeout.Token);
                }

                lifetime?.Shutdown(0);
            }
            catch (Exception exception)
            {
                File.WriteAllText(errorPath, exception.ToString());
                lifetime?.Shutdown(1);
            }
        }
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
            StartupTitle.Text = AppStrings.Get("Startup_ConfirmRecovery_Title");
            StartupMessage.Text = AppStrings.Get("Startup_ConfirmRecovery_Body");
            RecoveryButton.Content = AppStrings.Get("Startup_ConfirmRecovery_Action");
            RetryButton.Content = AppStrings.Get("Common_Cancel");
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
            StartupTitle.Text = AppStrings.Get("Startup_RecoveryPreserved_Title");
            StartupMessage.Text = AppStrings.Format(
                "Startup_RecoveryPreserved_Body",
                result.PreservedFileCount,
                result.RecoveryFileName);
            RecoveryButton.IsVisible = false;
            RetryButton.Content = AppStrings.Get("Startup_ContinueSetup");
            RetryButton.IsEnabled = true;
            RetryButton.IsVisible = true;
        }
        catch (OperationCanceledException)
        {
        }
        catch (LearnerStoreException exception)
        {
            StartupProgress.IsVisible = false;
            StartupTitle.Text = AppStrings.Get("Startup_RecoveryFailed_Title");
            StartupMessage.Text = exception.Message;
            RecoveryButton.Content = AppStrings.Get("Startup_TryPreservationAgain");
            RecoveryButton.IsEnabled = true;
            RetryButton.Content = AppStrings.Get("Common_Cancel");
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
            StartupTitle.Text = AppStrings.Get("Startup_OpenFailed_Title");
            StartupMessage.Text = exception.Message;
            RetryButton.IsVisible = true;
            RecoveryButton.IsVisible = _recoverLearnerStore is not null;
        }
    }

    private void ShowLoadingState()
    {
        StartupStatus.IsVisible = true;
        StartupProgress.IsVisible = true;
        StartupTitle.Text = AppStrings.Get("Startup_OpeningTitle");
        StartupMessage.Text = AppStrings.Get("Startup_LoadingProfile");
        RetryButton.Content = AppStrings.Get("Common_TryAgain");
        RetryButton.IsEnabled = true;
        RetryButton.IsVisible = false;
        RecoveryButton.Content = AppStrings.Get("Startup_PreserveAndRestart");
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

        ApplyAppLanguage(profile);
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

        RootContent.Content = new OnboardingView(
            _profileOwner,
            ShowShell,
            _runtimeContentCatalog ?? _authoringContentCatalog);
        StartupStatus.IsVisible = false;
    }

    private void OnClosed(object? sender, EventArgs args)
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
    }

    private void ApplyMotionPreference(bool savedPreference) =>
        Classes.Set("motion-enabled", !MotionPreferences.ShouldReduce(savedPreference));

    private void ApplyAppLanguage(LearnerProfile profile)
    {
        var catalog = _runtimeContentCatalog ?? _authoringContentCatalog;
        var instructionLanguage = catalog?
            .SelectInstructionLanguage(profile)
            .SelectedLanguage;
        AppStrings.UseLanguage(AppLanguageSelector.Select(profile, instructionLanguage));
    }

    private async Task TryLogAsync(
        DiagnosticCategory category,
        DiagnosticEventCode eventCode,
        DiagnosticOutcome outcome,
        TimeSpan? duration = null)
    {
        if (_diagnosticLog is null)
        {
            return;
        }

        try
        {
            await _diagnosticLog.WriteAsync(
                category,
                eventCode,
                outcome,
                duration: duration);
        }
        catch (DiagnosticLogException)
        {
        }
    }
}
