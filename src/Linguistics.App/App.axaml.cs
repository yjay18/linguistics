using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Linguistics.App.Persistence;
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
            desktop.MainWindow = new MainWindow(new LearnerProfileOwner(repository));
        }

        base.OnFrameworkInitializationCompleted();
    }
}
