using Avalonia.Controls;
using Avalonia.Interactivity;
using Linguistics.Core.Providers;
using Linguistics.Core.Profiles;
using Linguistics.App.Speech;
using Linguistics.App.Features.Learn.Templates;
using Linguistics.Core.Content;
using Linguistics.Core.Speech;

namespace Linguistics.App.Features.Settings;

public partial class SettingsView : UserControl
{
    private LearnerProfile? _profile;
    private Func<LearnerProfile, Task<LearnerProfile>>? _saveProfile;
    private Func<Task>? _deleteProfile;
    private ILanguageModelProvider? _languageModelProvider;
    private ISpeechSynthesisProvider? _speechSynthesisProvider;
    private ISpeechRecognitionProvider? _speechRecognitionProvider;
    private SpeechRecordingStore? _speechRecordingStore;
    private CancellationTokenSource? _modelInspectionCancellation;
    private bool _busy;
    private bool _modelBusy;
    private bool _speechBusy;

    public SettingsView()
    {
        InitializeComponent();
    }

    public SettingsView(
        LearnerProfile profile,
        Func<LearnerProfile, Task<LearnerProfile>> saveProfile,
        Func<Task> deleteProfile,
        ILanguageModelProvider? languageModelProvider = null,
        ISpeechSynthesisProvider? speechSynthesisProvider = null,
        ISpeechRecognitionProvider? speechRecognitionProvider = null,
        SpeechRecordingStore? speechRecordingStore = null,
        IReadOnlyList<ValidatedContentAsset>? contentAssets = null)
        : this()
    {
        _profile = profile;
        _saveProfile = saveProfile;
        _deleteProfile = deleteProfile;
        _languageModelProvider = languageModelProvider;
        _speechSynthesisProvider = speechSynthesisProvider;
        _speechRecognitionProvider = speechRecognitionProvider;
        _speechRecordingStore = speechRecordingStore;
        LoadAssetCredits(contentAssets ?? []);
        LoadProfile(profile);
    }

    private void LoadAssetCredits(IReadOnlyList<ValidatedContentAsset> assets)
    {
        AssetCreditsPanel.Children.Clear();
        AssetCreditsSummary.Text = assets.Count == 0
            ? "No validated pack images are available in this build."
            : $"{assets.Count} bundled Preview {(assets.Count == 1 ? "image has" : "images have")} local attribution and provenance records.";
        foreach (var asset in assets.OrderBy(asset => asset.Record.Id, StringComparer.Ordinal))
        {
            AssetCreditsPanel.Children.Add(TemplateRendering.CreateAssetCreditCard(asset));
        }
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
        ReduceMotion.IsChecked = profile.Settings.ReduceMotion;

        RefreshPreferredLanguageOptions();
        PreferredLanguage.SelectedItem = profile.Settings.PreferredExplanationLanguage is { } preferred
            ? PreferredItem(preferred.Value)
            : FirstVisiblePreferredItem();
        PreferredLanguagePanel.IsVisible = ShortcutPreferred.IsChecked == true;
        SetModelChoices([], profile.Settings.SelectedLocalModel);
        ModelServiceStatus.Text = profile.Settings.SelectedLocalModel is null
            ? "Scripted practice is active. Checking Ollama is optional."
            : $"Saved local model: {profile.Settings.SelectedLocalModel}. Check Ollama to verify current availability.";
        SpeechServiceStatus.Text =
            "System playback and local microphone transcription are checked only when you ask. No speech model is downloaded automatically.";
    }

    private async void OnCheckOllamaClicked(object? sender, RoutedEventArgs args)
    {
        if (_modelBusy || _languageModelProvider is null)
        {
            ModelServiceStatus.Text = "The local model provider is not available in this build.";
            return;
        }

        SetModelBusy(true);
        ModelDetailsText.Text = string.Empty;
        try
        {
            var snapshot = await _languageModelProvider.InspectServiceAsync();
            ModelServiceStatus.Text = snapshot.Message;
            SetModelChoices(
                snapshot.Status == LocalModelServiceStatus.Available
                    ? snapshot.Models.Where(model => !model.IsCloudAlias).ToArray()
                    : [],
                _profile?.Settings.SelectedLocalModel);
        }
        catch (OperationCanceledException)
        {
            ModelServiceStatus.Text = "The local Ollama check was cancelled.";
        }
        finally
        {
            SetModelBusy(false);
        }
    }

    private async void OnModelSelectionChanged(object? sender, SelectionChangedEventArgs args)
    {
        _modelInspectionCancellation?.Cancel();
        _modelInspectionCancellation?.Dispose();
        _modelInspectionCancellation = null;
        ModelDetailsText.Text = string.Empty;

        var model = SelectedModelName();
        if (model is null || _languageModelProvider is null)
        {
            return;
        }

        var cancellation = new CancellationTokenSource();
        _modelInspectionCancellation = cancellation;
        try
        {
            var details = await _languageModelProvider.InspectModelAsync(model, cancellation.Token);
            if (cancellation.IsCancellationRequested)
            {
                return;
            }

            var capabilities = details.Capabilities.Count == 0
                ? "not reported"
                : string.Join(", ", details.Capabilities);
            var license = string.IsNullOrWhiteSpace(details.LicenseText)
                ? "License text not reported."
                : $"Reported license excerpt: {Excerpt(details.LicenseText)}";
            ModelDetailsText.Text =
                $"{details.Message}\nCapabilities: {capabilities}.\n{license}\n" +
                "Source and storage: your local Ollama installation. Linguistics never downloads the model.";
        }
        catch (OperationCanceledException)
        {
        }
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

    private async void OnCheckSpeechClicked(object? sender, RoutedEventArgs args)
    {
        if (_speechBusy ||
            _speechSynthesisProvider is null ||
            _speechRecognitionProvider is null)
        {
            SpeechServiceStatus.Text = "Local speech providers are unavailable in this build.";
            return;
        }

        SetSpeechBusy(true);
        try
        {
            var synthesisTask = _speechSynthesisProvider.InspectAsync();
            var recognitionTask = _speechRecognitionProvider.InspectAsync();
            var recordingsTask = _speechRecordingStore?.InspectAsync() ??
                                 Task.FromResult(new SpeechRecordingSnapshot(0, 0));
            await Task.WhenAll(synthesisTask, recognitionTask, recordingsTask);
            var synthesis = await synthesisTask;
            var recognition = await recognitionTask;
            var recordings = await recordingsTask;
            var germanVoices = synthesis.Voices.Count(voice => voice.Language == new LanguageCode("de"));
            SpeechServiceStatus.Text =
                $"Playback: {germanVoices} installed German voice(s).\n" +
                $"Recognition: {recognition.Message}\n" +
                $"Legacy audio files: {recordings.FileCount} file(s), {FormatBytes(recordings.TotalBytes)}.";
            SpeechModelDetailsText.Text = recognition.Model is { } model
                ? $"Configured model: {model.Name} • {FormatBytes(model.SizeBytes)} • {model.ProviderVersion}\n" +
                  $"Source: {model.Source}\nLicense: {model.License}\n" +
                  "The current stream adapter does not retain microphone audio."
                : "To enable transcription, explicitly install whisper.cpp and set LINGUISTICS_WHISPER_MODEL to a model whose size, source, and terms you reviewed. Linguistics does not download or redistribute it.";
        }
        finally
        {
            SetSpeechBusy(false);
        }
    }

    private async void OnDeleteSpeechRecordingsClicked(object? sender, RoutedEventArgs args)
    {
        if (_speechBusy || _speechRecordingStore is null)
        {
            return;
        }

        SetSpeechBusy(true);
        try
        {
            var result = await _speechRecordingStore.DeleteAllAsync();
            SpeechDeletionStatusText.Text = result.Message;
            SpeechDeletionStatusText.IsVisible = true;
        }
        finally
        {
            SetSpeechBusy(false);
        }
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
                RetainSpeechRecordings: false,
                SelectedModelName(),
                ReduceMotion.IsChecked == true);
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

    private void SetModelBusy(bool busy)
    {
        _modelBusy = busy;
        CheckOllamaButton.IsEnabled = !busy;
        ModelSelection.IsEnabled = !busy;
    }

    private void SetSpeechBusy(bool busy)
    {
        _speechBusy = busy;
        CheckSpeechButton.IsEnabled = !busy;
        DeleteSpeechRecordingsButton.IsEnabled = !busy;
    }

    private void SetModelChoices(
        IReadOnlyList<LocalModelSummary> models,
        string? selectedModel)
    {
        var choices = new List<ModelChoice>
        {
            new(null, "Scripted only. No model selected"),
        };
        choices.AddRange(models.Select(model => new ModelChoice(
            model.Name,
            $"{model.Name}: {FormatBytes(model.SizeBytes)}; {TextOrUnknown(model.ParameterSize)}; {TextOrUnknown(model.Quantization)}")));

        if (selectedModel is not null && choices.All(choice => choice.Name != selectedModel))
        {
            choices.Add(new ModelChoice(selectedModel, $"{selectedModel}: saved, currently unavailable"));
        }

        ModelSelection.ItemsSource = choices;
        ModelSelection.SelectedItem = choices.First(choice => choice.Name == selectedModel);
    }

    private string? SelectedModelName() =>
        ModelSelection.SelectedItem is ModelChoice choice ? choice.Name : null;

    private static string FormatBytes(long bytes) =>
        bytes >= 1_073_741_824
            ? $"{bytes / 1_073_741_824d:0.0} GiB"
            : $"{bytes / 1_048_576d:0} MiB";

    private static string TextOrUnknown(string value) =>
        string.IsNullOrWhiteSpace(value) ? "not reported" : value;

    private static string Excerpt(string value)
    {
        var compact = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return compact.Length <= 480 ? compact : compact[..480] + "…";
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

    private sealed record ModelChoice(string? Name, string Label)
    {
        public override string ToString() => Label;
    }
}
