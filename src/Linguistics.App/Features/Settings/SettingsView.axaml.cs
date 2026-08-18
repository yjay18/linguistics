using Avalonia.Controls;
using Avalonia.Interactivity;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Settings;

public partial class SettingsView : UserControl
{
    private LearnerProfile? _profile;
    private Func<LearnerProfile, Task<LearnerProfile>>? _saveProfile;
    private Func<Task>? _deleteProfile;
    private bool _busy;

    public SettingsView()
    {
        InitializeComponent();
    }

    public SettingsView(
        LearnerProfile profile,
        Func<LearnerProfile, Task<LearnerProfile>> saveProfile,
        Func<Task> deleteProfile)
        : this()
    {
        _profile = profile;
        _saveProfile = saveProfile;
        _deleteProfile = deleteProfile;
        LoadProfile(profile);
    }

    private void LoadProfile(LearnerProfile profile)
    {
        ShortcutAutomatic.IsChecked =
            profile.Settings.ShortcutMode == MultilingualShortcutMode.Automatic;
        ShortcutAskFirst.IsChecked =
            profile.Settings.ShortcutMode == MultilingualShortcutMode.AskFirst;
        ShortcutPreferred.IsChecked =
            profile.Settings.ShortcutMode == MultilingualShortcutMode.PreferredLanguage;
        ShortcutNever.IsChecked =
            profile.Settings.ShortcutMode == MultilingualShortcutMode.Never;

        MicrophoneNow.IsChecked = profile.Settings.Microphone == MicrophonePreference.Now;
        MicrophoneLater.IsChecked = profile.Settings.Microphone == MicrophonePreference.Later;
        MicrophoneNever.IsChecked = profile.Settings.Microphone == MicrophonePreference.Never;
        RetainRecordings.IsChecked = profile.Settings.RetainSpeechRecordings;

        RefreshPreferredLanguageOptions();
        PreferredLanguage.SelectedItem = profile.Settings.PreferredExplanationLanguage is { } preferred
            ? PreferredItem(preferred.Value)
            : FirstVisiblePreferredItem();
        PreferredLanguagePanel.IsVisible = ShortcutPreferred.IsChecked == true;
    }

    private void OnShortcutChoiceChanged(object? sender, RoutedEventArgs args)
    {
        if (PreferredLanguagePanel is null)
        {
            return;
        }

        PreferredLanguagePanel.IsVisible = ShortcutPreferred.IsChecked == true;
        ClearMessages();
    }

    private async void OnSaveClicked(object? sender, RoutedEventArgs args)
    {
        if (_busy || _profile is null || _saveProfile is null)
        {
            return;
        }

        ClearMessages();
        var preferred = SelectedPreferredLanguage();
        if (ShortcutPreferred.IsChecked == true && preferred is null)
        {
            ShowError(
                "Choose a known language that is allowed for explanations, or select another shortcut mode.");
            return;
        }

        SetBusy(true);
        try
        {
            var settings = new LearnerSettings(
                SelectedShortcutMode(),
                preferred,
                SelectedMicrophonePreference(),
                RetainRecordings.IsChecked == true);
            _profile = await _saveProfile(_profile with { Settings = settings });
            StatusText.Text = "Settings saved locally.";
            StatusText.IsVisible = true;
        }
        catch (Exception exception) when (
            exception is LearnerStoreException or LearnerProfileValidationException)
        {
            ShowError(exception.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OnShowDeleteConfirmationClicked(object? sender, RoutedEventArgs args)
    {
        ClearMessages();
        DeleteConfirmation.IsVisible = true;
        ShowDeleteConfirmationButton.IsVisible = false;
        ConfirmDeleteButton.Focus();
    }

    private void OnCancelDeleteClicked(object? sender, RoutedEventArgs args)
    {
        DeleteConfirmation.IsVisible = false;
        ShowDeleteConfirmationButton.IsVisible = true;
        ShowDeleteConfirmationButton.Focus();
    }

    private async void OnConfirmDeleteClicked(object? sender, RoutedEventArgs args)
    {
        if (_busy || _deleteProfile is null)
        {
            return;
        }

        ClearMessages();
        SetBusy(true);
        try
        {
            await _deleteProfile();
        }
        catch (LearnerStoreException exception)
        {
            ShowError(exception.Message);
            SetBusy(false);
        }
    }

    private void RefreshPreferredLanguageOptions()
    {
        PreferredEnglish.IsVisible = IsEligible("en");
        PreferredHindi.IsVisible = IsEligible("hi");
    }

    private bool IsEligible(string code) =>
        _profile?.KnownLanguages.Any(language =>
            language.Language.Value == code && language.AllowExplanations) == true;

    private ComboBoxItem? PreferredItem(string code) => code switch
    {
        "en" when PreferredEnglish.IsVisible => PreferredEnglish,
        "hi" when PreferredHindi.IsVisible => PreferredHindi,
        _ => null,
    };

    private ComboBoxItem? FirstVisiblePreferredItem() =>
        PreferredEnglish.IsVisible ? PreferredEnglish :
        PreferredHindi.IsVisible ? PreferredHindi : null;

    private LanguageCode? SelectedPreferredLanguage()
    {
        if (ShortcutPreferred.IsChecked != true ||
            PreferredLanguage.SelectedItem is not ComboBoxItem { IsVisible: true } item ||
            item.Tag is not string code)
        {
            return null;
        }

        return new LanguageCode(code);
    }

    private MultilingualShortcutMode SelectedShortcutMode()
    {
        if (ShortcutAutomatic.IsChecked == true)
        {
            return MultilingualShortcutMode.Automatic;
        }

        if (ShortcutPreferred.IsChecked == true)
        {
            return MultilingualShortcutMode.PreferredLanguage;
        }

        return ShortcutNever.IsChecked == true
            ? MultilingualShortcutMode.Never
            : MultilingualShortcutMode.AskFirst;
    }

    private MicrophonePreference SelectedMicrophonePreference()
    {
        if (MicrophoneNow.IsChecked == true)
        {
            return MicrophonePreference.Now;
        }

        return MicrophoneNever.IsChecked == true
            ? MicrophonePreference.Never
            : MicrophonePreference.Later;
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        SaveButton.IsEnabled = !busy;
        ShowDeleteConfirmationButton.IsEnabled = !busy;
        ConfirmDeleteButton.IsEnabled = !busy;
        CancelDeleteButton.IsEnabled = !busy;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }

    private void ClearMessages()
    {
        ErrorText.IsVisible = false;
        StatusText.IsVisible = false;
    }
}
