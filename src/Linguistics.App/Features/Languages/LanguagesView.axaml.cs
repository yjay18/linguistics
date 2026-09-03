using Avalonia.Controls;
using Avalonia.Interactivity;
using Linguistics.App.Localization;
using Linguistics.Core.Content;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Languages;

public partial class LanguagesView : UserControl
{
    private LearnerProfile? _profile;
    private Func<LearnerProfile, Task<LearnerProfile>>? _saveProfile;
    private ValidatedContentCatalog? _contentCatalog;
    private bool _saving;

    public LanguagesView()
    {
        InitializeComponent();
    }

    public LanguagesView(
        LearnerProfile profile,
        Func<LearnerProfile, Task<LearnerProfile>> saveProfile,
        ValidatedContentCatalog? contentCatalog)
        : this()
    {
        _profile = profile;
        _saveProfile = saveProfile;
        _contentCatalog = contentCatalog;
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
        LoadLanguage(
            profile,
            "hi-latn",
            HinglishSelected,
            HinglishDetails,
            HinglishProficiency,
            HinglishReading,
            HinglishListening,
            HinglishExplanations);
        InstructionLanguage.SelectedItem =
            profile.Settings.ShortcutMode == MultilingualShortcutMode.PreferredLanguage
                ? InstructionItem(profile.Settings.PreferredExplanationLanguage)
                : InstructionAutomatic;
        RefreshInstructionStatus();
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
        if (EnglishDetails is null || HindiDetails is null || HinglishDetails is null)
        {
            return;
        }

        EnglishDetails.IsVisible = EnglishSelected.IsChecked == true;
        HindiDetails.IsVisible = HindiSelected.IsChecked == true;
        HinglishDetails.IsVisible = HinglishSelected.IsChecked == true;
        ClearMessages();
        RefreshInstructionStatus();
    }

    private void OnLanguageDetailsChanged(object? sender, RoutedEventArgs args) =>
        RefreshInstructionStatus();

    private void OnInstructionLanguageChanged(object? sender, SelectionChangedEventArgs args)
    {
        ClearMessages();
        RefreshInstructionStatus();
    }

    private async void OnSaveClicked(object? sender, RoutedEventArgs args)
    {
        if (_saving || _profile is null || _saveProfile is null)
        {
            return;
        }

        ClearMessages();
        var languages = BuildKnownLanguages();
        var preferred = SelectedInstructionLanguage();
        if (preferred is { } selected && !IsEligible(languages, selected))
        {
            ShowError(AppStrings.Get("Languages_Instruction_Ineligible"));
            return;
        }

        var settings = _profile.Settings with
        {
            ShortcutMode = preferred is null
                ? MultilingualShortcutMode.Automatic
                : MultilingualShortcutMode.PreferredLanguage,
            PreferredExplanationLanguage = preferred,
        };

        _saving = true;
        SaveButton.IsEnabled = false;
        try
        {
            _profile = await _saveProfile(_profile with
            {
                KnownLanguages = languages,
                Settings = settings,
            });
            RefreshInstructionStatus();
            StatusText.Text = AppStrings.Format(
                "Languages_Saved",
                SelectedInstructionName(_profile));
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
            .Where(language => language.Language.Value is not ("en" or "hi" or "hi-latn"))
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
        AddLanguage(
            languages,
            "hi-latn",
            HinglishSelected,
            HinglishProficiency,
            HinglishReading,
            HinglishListening,
            HinglishExplanations);
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

    private void RefreshInstructionStatus()
    {
        if (_profile is null || InstructionLanguage is null)
        {
            return;
        }

        var languages = BuildKnownLanguages();
        var preferred = SelectedInstructionLanguage();
        if (preferred is { } selected && !IsEligible(languages, selected))
        {
            InstructionStatus.Text = AppStrings.Get("Languages_Instruction_Ineligible");
            return;
        }

        var candidate = _profile with
        {
            KnownLanguages = languages,
            Settings = _profile.Settings with
            {
                ShortcutMode = preferred is null
                    ? MultilingualShortcutMode.Automatic
                    : MultilingualShortcutMode.PreferredLanguage,
                PreferredExplanationLanguage = preferred,
            },
        };
        InstructionStatus.Text = _contentCatalog is null
            ? AppStrings.Get("Languages_Instruction_NoCatalog")
            : SelectionCopy(_contentCatalog.SelectInstructionLanguage(candidate));
    }

    private LanguageCode? SelectedInstructionLanguage() =>
        InstructionLanguage.SelectedItem is ComboBoxItem { Tag: string code } && code != "auto"
            ? new LanguageCode(code)
            : null;

    private ComboBoxItem InstructionItem(LanguageCode? language) => language?.Value switch
    {
        "en" => InstructionEnglish,
        "hi" => InstructionHindi,
        "hi-latn" => InstructionHinglish,
        _ => InstructionAutomatic,
    };

    private static bool IsEligible(
        IReadOnlyList<KnownLanguage> languages,
        LanguageCode language) =>
        languages.Any(candidate =>
            candidate.Language == language &&
            candidate.AllowExplanations &&
            candidate.ComfortableReading);

    private static string SelectionCopy(InstructionLanguageSelectionResult selection)
    {
        if (selection.SelectedLanguage is not { } selected)
        {
            return AppStrings.Get("Languages_Instruction_Unavailable");
        }

        var name = LanguageName(selected);
        return selection.Explanation.SelectionReason ==
            InstructionLanguageSelectionReason.PreferredLanguage
            ? AppStrings.Format("Languages_Instruction_UsingPreferred", name)
            : AppStrings.Format("Languages_Instruction_UsingAutomatic", name);
    }

    private string SelectedInstructionName(LearnerProfile profile) =>
        _contentCatalog?.SelectInstructionLanguage(profile).SelectedLanguage is { } selected
            ? LanguageName(selected)
            : AppStrings.Get("Languages_Instruction_None");

    private static string LanguageName(LanguageCode language) => language.Value switch
    {
        "en" => AppStrings.Get("Language_English"),
        "hi" => AppStrings.Get("Language_Hindi"),
        "hi-latn" => AppStrings.Get("Language_Hinglish"),
        "de" => AppStrings.Get("Language_German"),
        _ => language.Value,
    };

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
