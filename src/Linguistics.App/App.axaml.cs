using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Linguistics.App.Diagnostics;
using Linguistics.App.LocalAI;
using Linguistics.App.Persistence;
using Linguistics.App.Speech;
using Linguistics.Core.Content;
using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ApplyDeveloperThemeOverride();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var paths = AppDataPaths.CreateDefault();
            var repository = new JsonLearnerRepository(paths.LearnerProfileFile);
            var diagnosticLog = new LocalDiagnosticLog(paths.DiagnosticLogFile);
            var languageModelProvider = OllamaProvider.CreateDefault();
            var speechSynthesisProvider = SystemSpeechSynthesisProvider.CreateDefault();
            var speechRecognitionProvider = WhisperStreamRecognitionProvider.CreateDefault();
            var pronunciationAssessmentProvider = new TranscriptPronunciationAssessmentProvider();
            var speechRecordingStore = new SpeechRecordingStore(paths.SpeechRecordingsDirectory);
            var contentDirectory = Path.Combine(AppContext.BaseDirectory, "Content");
            ValidatedContentCatalog? runtimeContentCatalog = null;
            string? runtimeContentError = null;
            try
            {
                runtimeContentCatalog = ContentPackLoader.LoadDirectory(
                    contentDirectory,
                    ContentLoadPolicy.Runtime);
            }
            catch (ContentValidationException exception)
            {
                runtimeContentError = exception.Message;
            }

            ValidatedContentCatalog? authoringContentCatalog = null;
            string? authoringContentError = null;
            if (DeveloperModeEnabled())
            {
                try
                {
                    authoringContentCatalog = ContentPackLoader.LoadDirectory(
                        contentDirectory,
                        ContentLoadPolicy.AuthoringPreview);
                }
                catch (ContentValidationException exception)
                {
                    authoringContentError = exception.Message;
                }
            }

            desktop.MainWindow = new MainWindow(
                new LearnerProfileOwner(repository),
                runtimeContentCatalog,
                runtimeContentError,
                authoringContentCatalog,
                authoringContentError,
                languageModelProvider,
                speechSynthesisProvider,
                speechRecognitionProvider,
                pronunciationAssessmentProvider,
                speechRecordingStore,
                repository.PreserveForRecoveryAsync,
                diagnosticLog);
            desktop.Exit += (_, _) =>
            {
                languageModelProvider.Dispose();
                speechSynthesisProvider.Dispose();
                speechRecognitionProvider.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static bool DeveloperModeEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable("LINGUISTICS_DEVELOPER_MODE"),
            "1",
            StringComparison.Ordinal);

    private void ApplyDeveloperThemeOverride()
    {
        if (!DeveloperModeEnabled())
        {
            return;
        }

        RequestedThemeVariant = Environment
            .GetEnvironmentVariable("LINGUISTICS_DEVELOPER_THEME")?
            .ToUpperInvariant() switch
        {
            "LIGHT" => ThemeVariant.Light,
            "DARK" => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}
