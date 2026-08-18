using Avalonia.Controls;
using Avalonia.Interactivity;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Languages;

public partial class LanguagesView : UserControl
{
    private LearnerProfile? _profile;
    private Func<LearnerProfile, Task<LearnerProfile>>? _saveProfile;
    private bool _saving;

    public LanguagesView()
    {
        InitializeComponent();
    }

    public LanguagesView(
        LearnerProfile profile,
        Func<LearnerProfile, Task<LearnerProfile>> saveProfile)
        : this()
    {
        _profile = profile;
        _saveProfile = saveProfile;
        LoadProfile(profile);
    }

    private void LoadProfile(LearnerProfile profile)
    {
        LoadLanguage(
            profile,
            "en",
            EnglishSelected,
            EnglishDetails,
            EnglishProficiency,
            EnglishReading,
            EnglishListening,
            EnglishExplanations);
        LoadLanguage(
            profile,
            "hi",
            HindiSelected,
            HindiDetails,
            HindiProficiency,
            HindiReading,
            HindiListening,
            HindiExplanations);
    }

    private static void LoadLanguage(
        LearnerProfile profile,
        string code,
        CheckBox selected,
        Control details,
        ComboBox proficiency,
        CheckBox reading,
        CheckBox listening,
        CheckBox explanations)
    {
        var language = profile.KnownLanguages.SingleOrDefault(
            candidate => candidate.Language.Value == code);
        selected.IsChecked = language is not null;
        details.IsVisible = language is not null;
        proficiency.SelectedIndex = language is null ? 2 : (int)language.Proficiency;
        reading.IsChecked = language?.ComfortableReading ?? true;
        listening.IsChecked = language?.ComfortableListening ?? true;
        explanations.IsChecked = language?.AllowExplanations ?? true;
    }

    private void OnLanguageSelectionChanged(object? sender, RoutedEventArgs args)
    {
        if (EnglishDetails is null || HindiDetails is null)
        {
            return;
        }

        EnglishDetails.IsVisible = EnglishSelected.IsChecked == true;
        HindiDetails.IsVisible = HindiSelected.IsChecked == true;
        ClearMessages();
    }

    private async void OnSaveClicked(object? sender, RoutedEventArgs args)
    {
        if (_saving || _profile is null || _saveProfile is null)
        {
            return;
        }

        ClearMessages();
        var languages = BuildKnownLanguages();
        if (!PreferredLanguageRemainsEligible(languages))
        {
            ShowError(
                "Your preferred explanation language must remain selected and allowed for explanations. Change the shortcut mode in Settings first.");
            return;
        }

        _saving = true;
        SaveButton.IsEnabled = false;
        try
        {
            _profile = await _saveProfile(_profile with { KnownLanguages = languages });
            StatusText.Text = "Language preferences saved locally.";
            StatusText.IsVisible = true;
        }
        catch (Exception exception) when (
            exception is LearnerStoreException or LearnerProfileValidationException)
        {
            ShowError(exception.Message);
        }
        finally
        {
            _saving = false;
            SaveButton.IsEnabled = true;
        }
    }

    private IReadOnlyList<KnownLanguage> BuildKnownLanguages()
    {
        var languages = _profile?.KnownLanguages
            .Where(language => language.Language.Value is not ("en" or "hi"))
            .ToList() ?? [];
        AddLanguage(
            languages,
            "en",
            EnglishSelected,
            EnglishProficiency,
            EnglishReading,
            EnglishListening,
            EnglishExplanations);
        AddLanguage(
            languages,
            "hi",
            HindiSelected,
            HindiProficiency,
            HindiReading,
            HindiListening,
            HindiExplanations);
        return languages;
    }

    private static void AddLanguage(
        ICollection<KnownLanguage> languages,
        string code,
        CheckBox selected,
        ComboBox proficiency,
        CheckBox reading,
        CheckBox listening,
        CheckBox explanations)
    {
        if (selected.IsChecked != true)
        {
            return;
        }

        languages.Add(new KnownLanguage(
            new LanguageCode(code),
            SelectedProficiency(proficiency),
            reading.IsChecked == true,
            listening.IsChecked == true,
            explanations.IsChecked == true));
    }

    private bool PreferredLanguageRemainsEligible(IReadOnlyList<KnownLanguage> languages)
    {
        if (_profile!.Settings.ShortcutMode != MultilingualShortcutMode.PreferredLanguage)
        {
            return true;
        }

        var preferred = _profile.Settings.PreferredExplanationLanguage;
        return preferred is not null && languages.Any(language =>
            language.Language == preferred.Value && language.AllowExplanations);
    }

    private static LanguageProficiency SelectedProficiency(ComboBox comboBox)
    {
        var value = (comboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        return Enum.TryParse<LanguageProficiency>(value, out var proficiency)
            ? proficiency
            : LanguageProficiency.Advanced;
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
