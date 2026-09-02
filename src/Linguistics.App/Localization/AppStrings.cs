using System.Globalization;
using System.ComponentModel;
using System.Resources;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Localization;

public static class AppStrings
{
    private static readonly ResourceManager Resources = new(
        "Linguistics.App.Resources.AppStrings",
        typeof(AppStrings).Assembly);
    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en");
    private static CultureInfo _culture = EnglishCulture;

    public static IReadOnlyList<LanguageCode> SupportedLanguages { get; } =
        [new("en"), new("hi")];

    public static LanguageCode CurrentLanguage => new(_culture.Name);

    public static bool Supports(LanguageCode language) =>
        SupportedLanguages.Contains(language);

    public static void UseLanguage(LanguageCode language)
    {
        if (!Supports(language))
        {
            throw new ArgumentException(
                $"App language '{language}' is unsupported.",
                nameof(language));
        }

        _culture = CultureInfo.GetCultureInfo(language.Value);
        AppStringProvider.Instance.Refresh();
    }

    public static string Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return Resources.GetString(key, _culture) ??
               Resources.GetString(key, EnglishCulture) ??
               throw new MissingManifestResourceException(
                   $"App string resource '{key}' is missing.");
    }

    public static string Format(string key, params object?[] arguments) =>
        string.Format(_culture, Get(key), arguments);
}

public sealed class AppStringProvider : INotifyPropertyChanged
{
    public static AppStringProvider Instance { get; } = new();

    private AppStringProvider()
    {
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => AppStrings.Get(key);

    internal void Refresh() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
}

public static class AppLanguageSelector
{
    private static readonly LanguageCode English = new("en");

    public static LanguageCode Select(
        LearnerProfile profile,
        LanguageCode? instructionLanguage)
    {
        ArgumentNullException.ThrowIfNull(profile);
        LearnerProfileValidator.Validate(profile);

        if (profile.Settings.AppLanguageOverride is { } appLanguage &&
            AppStrings.Supports(appLanguage))
        {
            return appLanguage;
        }

        return instructionLanguage is { } selected && AppStrings.Supports(selected)
            ? selected
            : English;
    }
}

public sealed class LocalizeExtension : MarkupExtension
{
    public LocalizeExtension(string key)
    {
        Key = key;
    }

    public string Key { get; }

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new Binding($"[{Key}]")
        {
            Source = AppStringProvider.Instance,
            Mode = BindingMode.OneWay,
        };
}
