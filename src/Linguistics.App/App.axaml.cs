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
            ValidatedContentCatalog? contentCatalog = null;
            string? contentError = null;
            if (DeveloperModeEnabled())
            {
                try
                {
                    contentCatalog = ContentPackLoader.LoadDirectory(
                        Path.Combine(AppContext.BaseDirectory, "Content"),
                        ContentLoadPolicy.AuthoringPreview);
                }
                catch (ContentValidationException exception)
                {
                    contentError = exception.Message;
                }
            }

            desktop.MainWindow = new MainWindow(
                new LearnerProfileOwner(repository),
                contentCatalog,
                contentError,
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
