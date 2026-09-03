using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Linguistics.App.Localization;
using Linguistics.Core.Content;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Onboarding;

public partial class OnboardingView : UserControl
{
    private static readonly string[] TitleKeys =
    [
        "Onboarding_Title_Target",
        "Onboarding_Title_Known",
        "Onboarding_Title_Details",
        "Onboarding_Title_Instruction",
        "Onboarding_Title_Microphone",
        "Onboarding_Title_Motion",
        "Onboarding_Title_Review",
    ];

    private static readonly string[] BodyKeys =
    [
        "Onboarding_Body_Target",
        "Onboarding_Body_Known",
        "Onboarding_Body_Details",
        "Onboarding_Body_Instruction",
        "Onboarding_Body_Microphone",
        "Onboarding_Body_Motion",
        "Onboarding_Body_Review",
    ];

    private LearnerProfileOwner? _profileOwner;
    private Action<LearnerProfile>? _completed;
    private ValidatedContentCatalog? _contentCatalog;
    private readonly Control[] _steps;
    private int _step;
    private bool _saving;

    public OnboardingView()
    {
        InitializeComponent();

        _steps =
        [
            TargetStep,
            KnownLanguagesStep,
            LanguageDetailsStep,
            ShortcutStep,
            MicrophoneStep,
            MotionStep,
            ReviewStep,
        ];

        ShowStep(0);
    }

    public OnboardingView(
        LearnerProfileOwner profileOwner,
        Action<LearnerProfile> completed,
        ValidatedContentCatalog? contentCatalog)
        : this()
    {
        _profileOwner = profileOwner;
        _completed = completed;
        _contentCatalog = contentCatalog;
    }

    private void OnBackClicked(object? sender, RoutedEventArgs args)
    {
        if (_saving || _step == 0)
        {
            return;
        }

        ShowStep(_step - 1);
    }

    private async void OnContinueClicked(object? sender, RoutedEventArgs args)
    {
        if (_saving)
        {
            return;
        }

        ClearError();
        if (_step == _steps.Length - 1)
        {
            await CompleteAsync();
            return;
        }

        if (_step == 2)
        {
            RefreshPreferredLanguageOptions();
        }

        if (_step == 3 && !ValidateShortcutSelection())
        {
            return;
        }

        ShowStep(_step + 1);
    }

    private void OnShortcutChoiceChanged(object? sender, RoutedEventArgs args)
    {
        if (PreferredLanguagePanel is null)
        {
            return;
        }

        PreferredLanguagePanel.IsVisible = ShortcutPreferred.IsChecked == true;
        RefreshInstructionStatus();
    }

    private void ShowStep(int step)
    {
        _step = step;
        for (var index = 0; index < _steps.Length; index++)
        {
            _steps[index].IsVisible = index == step;
        }

        if (step == 2)
        {
            EnglishDetails.IsVisible = EnglishSelected.IsChecked == true;
            HindiDetails.IsVisible = HindiSelected.IsChecked == true;
            HinglishDetails.IsVisible = HinglishSelected.IsChecked == true;
            NoKnownLanguagesMessage.IsVisible =
                EnglishSelected.IsChecked != true &&
                HindiSelected.IsChecked != true &&
                HinglishSelected.IsChecked != true;
        }

        if (step == 3)
        {
            RefreshPreferredLanguageOptions();
            PreferredLanguagePanel.IsVisible = ShortcutPreferred.IsChecked == true;
            RefreshInstructionStatus();
        }

        if (step == 6)
        {
            ReviewSummary.Text = BuildSummary();
        }

        StepText.Text = AppStrings.Format("Onboarding_Step", step + 1, _steps.Length);
        StepProgress.Value = step + 1;
        QuestionTitle.Text = AppStrings.Get(TitleKeys[step]);
        QuestionBody.Text = AppStrings.Get(BodyKeys[step]);
        BackButton.IsEnabled = step > 0;
        ContinueButton.Content = step == _steps.Length - 1
            ? AppStrings.Get("Onboarding_SaveProfile")
            : AppStrings.Get("Common_Continue");
        ClearError();
    }

    private bool ValidateShortcutSelection()
    {
        if (ShortcutPreferred.IsChecked != true)
        {
            return true;
        }

        if (PreferredLanguage.SelectedItem is ComboBoxItem { IsVisible: true })
        {
            return true;
        }

        ShowError(AppStrings.Get("Onboarding_Preferred_Error"));
        return false;
    }

    private void RefreshPreferredLanguageOptions()
    {
        PreferredEnglish.IsVisible =
            EnglishSelected.IsChecked == true && EnglishExplanations.IsChecked == true;
        PreferredHindi.IsVisible =
            HindiSelected.IsChecked == true && HindiExplanations.IsChecked == true;
        PreferredHinglish.IsVisible =
            HinglishSelected.IsChecked == true && HinglishExplanations.IsChecked == true;

        if (PreferredLanguage.SelectedItem is ComboBoxItem selected && !selected.IsVisible)
        {
            PreferredLanguage.SelectedItem = null;
        }

        PreferredLanguage.SelectedItem ??=
            PreferredEnglish.IsVisible ? PreferredEnglish :
            PreferredHindi.IsVisible ? PreferredHindi :
            PreferredHinglish.IsVisible ? PreferredHinglish : null;
        RefreshInstructionStatus();
    }

    private void OnPreferredLanguageChanged(object? sender, SelectionChangedEventArgs args) =>
        RefreshInstructionStatus();

    private void RefreshInstructionStatus()
    {
        if (InstructionSelectionStatus is null)
        {
            return;
        }

        if (ShortcutPreferred.IsChecked == true &&
            PreferredLanguage.SelectedItem is not ComboBoxItem { IsVisible: true })
        {
            InstructionSelectionStatus.Text = AppStrings.Get("Onboarding_Instruction_Ineligible");
            return;
        }

        if (_contentCatalog is null)
        {
            InstructionSelectionStatus.Text = AppStrings.Get("Onboarding_Instruction_NoCatalog");
            return;
        }

        var profile = new LearnerProfile(
            Guid.Parse("2c455f20-18a8-4ee8-ae46-20234a65d317"),
            new LanguageCode("de"),
            BuildKnownLanguages(),
            new LearnerSettings(
                SelectedShortcutMode(),
                SelectedPreferredLanguage(),
                SelectedMicrophonePreference(),
                RetainSpeechRecordings: false,
                ReduceMotion: ReduceMotion.IsChecked == true));
        var selection = _contentCatalog.SelectInstructionLanguage(profile);
        InstructionSelectionStatus.Text = selection.SelectedLanguage is { } language
            ? AppStrings.Format(
                "Onboarding_Instruction_Selected",
                LanguageName(language))
            : AppStrings.Get("Onboarding_Instruction_Unavailable");
    }

    private async Task CompleteAsync()
    {
        if (_profileOwner is null || _completed is null)
        {
            ShowError(AppStrings.Get("Onboarding_Unavailable"));
            return;
        }

        _saving = true;
        BackButton.IsEnabled = false;
        ContinueButton.IsEnabled = false;
        ErrorText.Text = AppStrings.Get("Onboarding_Saving");
        ErrorText.IsVisible = true;

        try
        {
            var profile = await _profileOwner.CompleteOnboardingAsync(BuildProfileInput());
            _completed(profile);
        }
        catch (Exception exception) when (
            exception is LearnerStoreException or LearnerProfileValidationException)
        {
            _saving = false;
            BackButton.IsEnabled = true;
            ContinueButton.IsEnabled = true;
            ShowError(exception.Message);
        }
    }

    private NewLearnerProfile BuildProfileInput() =>
        new(
            new LanguageCode("de"),
            BuildKnownLanguages(),
            new LearnerSettings(
                SelectedShortcutMode(),
                SelectedPreferredLanguage(),
                SelectedMicrophonePreference(),
                RetainSpeechRecordings: false,
                ReduceMotion: ReduceMotion.IsChecked == true));

    private IReadOnlyList<KnownLanguage> BuildKnownLanguages()
    {
        var languages = new List<KnownLanguage>();
        if (EnglishSelected.IsChecked == true)
        {
            languages.Add(new KnownLanguage(
                new LanguageCode("en"),
                SelectedProficiency(EnglishProficiency),
                EnglishReading.IsChecked == true,
                EnglishListening.IsChecked == true,
                EnglishExplanations.IsChecked == true));
        }

        if (HindiSelected.IsChecked == true)
        {
            languages.Add(new KnownLanguage(
                new LanguageCode("hi"),
                SelectedProficiency(HindiProficiency),
                HindiReading.IsChecked == true,
                HindiListening.IsChecked == true,
                HindiExplanations.IsChecked == true));
        }

        if (HinglishSelected.IsChecked == true)
        {
            languages.Add(new KnownLanguage(
                new LanguageCode("hi-latn"),
                SelectedProficiency(HinglishProficiency),
                HinglishReading.IsChecked == true,
                HinglishListening.IsChecked == true,
                HinglishExplanations.IsChecked == true));
        }

        return languages;
    }

    private static LanguageProficiency SelectedProficiency(ComboBox comboBox)
    {
        var value = (comboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        return Enum.TryParse<LanguageProficiency>(value, out var proficiency)
            ? proficiency
            : LanguageProficiency.Advanced;
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

    private LanguageCode? SelectedPreferredLanguage()
    {
        if (ShortcutPreferred.IsChecked != true ||
            PreferredLanguage.SelectedItem is not ComboBoxItem item ||
            item.Tag is not string value)
        {
            return null;
        }

        return new LanguageCode(value);
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

    private string BuildSummary()
    {
        var languages = BuildKnownLanguages();
        var knownNames = languages.Count == 0
            ? AppStrings.Get("Onboarding_NoneSelected")
            : string.Join(", ", languages.Select(language =>
                LanguageName(language.Language)));

        var instructionLanguage = _contentCatalog?
            .SelectInstructionLanguage(new LearnerProfile(
                Guid.Parse("2c455f20-18a8-4ee8-ae46-20234a65d317"),
                new LanguageCode("de"),
                languages,
                new LearnerSettings(
                    SelectedShortcutMode(),
                    SelectedPreferredLanguage(),
                    SelectedMicrophonePreference(),
                    RetainSpeechRecordings: false,
                    ReduceMotion: ReduceMotion.IsChecked == true)))
            .SelectedLanguage;

        var summary = new StringBuilder()
            .AppendLine(AppStrings.Format("Onboarding_Summary_Target", AppStrings.Get("Language_German")))
            .AppendLine(AppStrings.Format("Onboarding_Summary_Known", knownNames))
            .AppendLine(AppStrings.Format(
                "Onboarding_Summary_Instruction",
                instructionLanguage is { } selected
                    ? LanguageName(selected)
                    : AppStrings.Get("Onboarding_Instruction_None")))
            .AppendLine(AppStrings.Format("Onboarding_Summary_Shortcuts", ShortcutLabel(SelectedShortcutMode())))
            .AppendLine(AppStrings.Format("Onboarding_Summary_Microphone", MicrophoneLabel(SelectedMicrophonePreference())))
            .AppendLine(AppStrings.Get("Onboarding_Summary_SpeechRetention"))
            .Append(AppStrings.Format(
                "Onboarding_Summary_Motion",
                AppStrings.Get(ReduceMotion.IsChecked == true ? "Common_Yes" : "Common_No")));

        return summary.ToString();
    }

    private static string ShortcutLabel(MultilingualShortcutMode mode) => mode switch
    {
        MultilingualShortcutMode.Automatic => AppStrings.Get("Onboarding_Shortcut_Automatic_Label"),
        MultilingualShortcutMode.AskFirst => AppStrings.Get("Onboarding_Shortcut_Ask_Label"),
        MultilingualShortcutMode.PreferredLanguage => AppStrings.Get("Onboarding_Shortcut_Preferred_Label"),
        MultilingualShortcutMode.Never => AppStrings.Get("Onboarding_Shortcut_Never_Label"),
        _ => AppStrings.Get("Common_Unknown"),
    };

    private static string MicrophoneLabel(MicrophonePreference preference) => preference switch
    {
        MicrophonePreference.Now => AppStrings.Get("Onboarding_Microphone_Now_Label"),
        MicrophonePreference.Later => AppStrings.Get("Onboarding_Microphone_Later_Label"),
        MicrophonePreference.Never => AppStrings.Get("Onboarding_Microphone_Never_Label"),
        _ => AppStrings.Get("Common_Unknown"),
    };

    private static string LanguageName(LanguageCode language) => language.Value switch
    {
        "en" => AppStrings.Get("Language_English"),
        "hi" => AppStrings.Get("Language_Hindi"),
        "hi-latn" => AppStrings.Get("Language_Hinglish"),
        "de" => AppStrings.Get("Language_German"),
        _ => language.Value,
    };

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }

    private void ClearError()
    {
        ErrorText.Text = string.Empty;
        ErrorText.IsVisible = false;
    }
}
