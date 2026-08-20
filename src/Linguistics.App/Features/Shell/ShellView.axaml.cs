using Avalonia.Controls;
using Linguistics.App.Features.Languages;
using Linguistics.App.Features.Learn;
using Linguistics.App.Features.Settings;
using Linguistics.Core.Content;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Shell;

public partial class ShellView : UserControl
{
    private LearnerProfile? _profile;
    private LearnerProfileOwner? _profileOwner;
    private Action? _profileDeleted;
    private ValidatedContentCatalog? _contentCatalog;
    private string? _contentError;

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
        ValidatedContentCatalog? contentCatalog = null,
        string? contentError = null)
        : this()
    {
        _profile = profile;
        _profileOwner = profileOwner;
        _profileDeleted = profileDeleted;
        _contentCatalog = contentCatalog;
        _contentError = contentError;
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
                ShowPage(new CurriculumDiagnosticsView(_profile, _contentCatalog, _contentError));
                break;
            case "Settings":
                ShowPage(new SettingsView(_profile, SaveProfileAsync, DeleteProfileAsync));
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
