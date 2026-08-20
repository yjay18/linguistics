using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Linguistics.App.LocalAI;
using Linguistics.App.Persistence;
using Linguistics.Core.Content;
using Linguistics.Core.Profiles;

namespace Linguistics.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var paths = AppDataPaths.CreateDefault();
            var repository = new JsonLearnerRepository(paths.LearnerProfileFile);
            var languageModelProvider = OllamaProvider.CreateDefault();
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
                languageModelProvider);
            desktop.Exit += (_, _) => languageModelProvider.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static bool DeveloperModeEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable("LINGUISTICS_DEVELOPER_MODE"),
            "1",
            StringComparison.Ordinal);
}
