using Avalonia.Controls;
using Linguistics.App.Features.Onboarding;
using Linguistics.App.Features.Shell;
using Linguistics.Core.Profiles;

namespace Linguistics.App;

public partial class MainWindow : Window
{
    private LearnerProfileOwner? _profileOwner;
    private CancellationTokenSource? _loadCancellation;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(LearnerProfileOwner profileOwner)
        : this()
    {
        _profileOwner = profileOwner;
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
                RootContent.Content = new OnboardingView(_profileOwner, ShowShell);
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
            exception is LearnerStoreException or LearnerProfileValidationException)
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
        RootContent.Content = new ShellView();
        StartupStatus.IsVisible = false;
    }

    private void OnClosed(object? sender, EventArgs args)
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
    }
}
