using Avalonia.Controls;
using Linguistics.App.Features.Languages;
using Linguistics.App.Features.Learn;
using Linguistics.App.Features.Scenarios;
using Linguistics.App.Features.Settings;
using Linguistics.Core.Content;
using Linguistics.Core.Providers;
using Linguistics.Core.Profiles;

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

    public ShellView()
    {
        InitializeComponent();
        NavigationList.SelectionChanged += OnNavigationChanged;
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
        ILanguageModelProvider? languageModelProvider = null)
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
            case "Languages":
                ShowPage(new LanguagesView(_profile, SaveProfileAsync));
                break;
            case "Learn" when DeveloperModeEnabled():
                ShowPage(new CurriculumDiagnosticsView(
                    _profile,
                    _authoringContentCatalog,
                    _authoringContentError));
                break;
            case "Scenarios":
                ShowPage(new CafeOrderView(
                    _profile,
                    _profileOwner,
                    _runtimeContentCatalog,
                    _runtimeContentError,
                    _languageModelProvider));
                break;
            case "Settings":
                ShowPage(new SettingsView(
                    _profile,
                    SaveProfileAsync,
                    DeleteProfileAsync,
                    _languageModelProvider));
                break;
            default:
                ShowUnavailable();
                break;
        }
    }

    private async Task<LearnerProfile> SaveProfileAsync(LearnerProfile profile)
    {
        _profile = await _profileOwner!.UpdateAsync(profile);
        return _profile;
    }

    private async Task DeleteProfileAsync()
    {
        await _profileOwner!.DeleteAllAsync();
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

    private static bool DeveloperModeEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable("LINGUISTICS_DEVELOPER_MODE"),
            "1",
            StringComparison.Ordinal);
}
