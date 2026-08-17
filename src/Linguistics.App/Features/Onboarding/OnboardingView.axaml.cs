using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Onboarding;

public partial class OnboardingView : UserControl
{
    private static readonly string[] Titles =
    [
        "What would you like to learn?",
        "Which languages do you already know?",
        "How do you use those languages?",
        "How should multilingual shortcuts work?",
        "Would you like to use a microphone?",
        "Should speech recordings be retained?",
        "Review your local profile",
    ];

    private static readonly string[] Bodies =
    [
        "The first content configuration is intentionally small.",
        "Your repertoire can include more than one language, with different preferences for each.",
        "These preferences decide whether a language may support explanations; they do not infer identity or background.",
        "A language switch will always be visible and explained. Reviewed data—not a model—decides when a comparison is valid.",
        "Speech will be optional when it is implemented.",
        "Recording retention is separate from microphone access and remains off unless you opt in.",
        "Nothing is written until you finish this step.",
    ];

    private LearnerProfileOwner? _profileOwner;
    private Action<LearnerProfile>? _completed;
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
            RetentionStep,
            ReviewStep,
        ];

        ShowStep(0);
    }

    public OnboardingView(
        LearnerProfileOwner profileOwner,
        Action<LearnerProfile> completed)
        : this()
    {
        _profileOwner = profileOwner;
        _completed = completed;
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
            NoKnownLanguagesMessage.IsVisible =
                EnglishSelected.IsChecked != true && HindiSelected.IsChecked != true;
        }

        if (step == 3)
        {
            RefreshPreferredLanguageOptions();
            PreferredLanguagePanel.IsVisible = ShortcutPreferred.IsChecked == true;
        }

        if (step == 6)
        {
            ReviewSummary.Text = BuildSummary();
        }

        StepText.Text = $"Step {step + 1} of {_steps.Length}";
        StepProgress.Value = step + 1;
        QuestionTitle.Text = Titles[step];
        QuestionBody.Text = Bodies[step];
        BackButton.IsEnabled = step > 0;
        ContinueButton.Content = step == _steps.Length - 1 ? "Save profile" : "Continue";
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

        ShowError("Choose a known language that you allowed for explanations, or select another shortcut mode.");
        return false;
    }

    private void RefreshPreferredLanguageOptions()
    {
        PreferredEnglish.IsVisible =
            EnglishSelected.IsChecked == true && EnglishExplanations.IsChecked == true;
        PreferredHindi.IsVisible =
            HindiSelected.IsChecked == true && HindiExplanations.IsChecked == true;

        if (PreferredLanguage.SelectedItem is ComboBoxItem selected && !selected.IsVisible)
        {
            PreferredLanguage.SelectedItem = null;
        }

        PreferredLanguage.SelectedItem ??=
            PreferredEnglish.IsVisible ? PreferredEnglish :
            PreferredHindi.IsVisible ? PreferredHindi : null;
    }

    private async Task CompleteAsync()
    {
        if (_profileOwner is null || _completed is null)
        {
            ShowError("Onboarding is unavailable because the profile service was not initialized.");
            return;
        }

        _saving = true;
        BackButton.IsEnabled = false;
        ContinueButton.IsEnabled = false;
        ErrorText.Text = "Saving your profile locally…";
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
                RetainRecordings.IsChecked == true));

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
            ? "None selected"
            : string.Join(", ", languages.Select(language =>
                language.Language.Value == "en" ? "English" : "Hindi"));

        var summary = new StringBuilder()
            .AppendLine("Target language: German")
            .AppendLine($"Known languages: {knownNames}")
            .AppendLine($"Multilingual shortcuts: {ShortcutLabel(SelectedShortcutMode())}")
            .AppendLine($"Microphone: {MicrophoneLabel(SelectedMicrophonePreference())}")
            .Append($"Retain speech recordings: {(RetainRecordings.IsChecked == true ? "Yes" : "No")}");

        return summary.ToString();
    }

    private static string ShortcutLabel(MultilingualShortcutMode mode) => mode switch
    {
        MultilingualShortcutMode.Automatic => "Automatic",
        MultilingualShortcutMode.AskFirst => "Ask first",
        MultilingualShortcutMode.PreferredLanguage => "One preferred language",
        MultilingualShortcutMode.Never => "Never",
        _ => "Unknown",
    };

    private static string MicrophoneLabel(MicrophonePreference preference) => preference switch
    {
        MicrophonePreference.Now => "Use when available",
        MicrophonePreference.Later => "Decide later",
        MicrophonePreference.Never => "Never",
        _ => "Unknown",
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
