using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class DesignSystemTests
{
    [TestMethod]
    public void ReducedMotionAcceptsSavedPreferenceAndExplicitEnvironmentValues()
    {
        Assert.IsFalse(MotionPreferences.ShouldReduce(savedPreference: false, environmentValue: null));
        Assert.IsTrue(MotionPreferences.ShouldReduce(savedPreference: true, environmentValue: null));
        Assert.IsTrue(MotionPreferences.ShouldReduce(savedPreference: false, environmentValue: "1"));
        Assert.IsTrue(MotionPreferences.ShouldReduce(savedPreference: false, environmentValue: "TRUE"));
        Assert.IsTrue(MotionPreferences.ShouldReduce(savedPreference: false, environmentValue: "yes"));
        Assert.IsTrue(MotionPreferences.ShouldReduce(savedPreference: false, environmentValue: "ON"));
        Assert.IsFalse(MotionPreferences.ShouldReduce(savedPreference: false, environmentValue: "0"));
        Assert.IsFalse(MotionPreferences.ShouldReduce(savedPreference: false, environmentValue: "sometimes"));
    }

    [TestMethod]
    public void EveryAppBrushReferenceExistsInBothThemes()
    {
        var expression = new Regex(
            @"\{DynamicResource (?<key>App[A-Za-z0-9]+)\}",
            RegexOptions.CultureInvariant);
        var references = Directory
            .EnumerateFiles(
                Path.Combine(RepositoryRoot, "src", "Linguistics.App"),
                "*.axaml",
                SearchOption.AllDirectories)
            .SelectMany(File.ReadLines)
            .SelectMany(line => expression.Matches(line).Select(match => match.Groups["key"].Value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        foreach (var themeName in new[] { "Light", "Dark" })
        {
            var resources = LoadThemeResourceKeys(themeName);
            foreach (var reference in references)
            {
                Assert.Contains(reference, resources, $"{themeName} is missing {reference}.");
            }
        }
    }

    [TestMethod]
    public void SemanticTextAndFocusPairsMeetMinimumContrastInBothThemes()
    {
        foreach (var themeName in new[] { "Light", "Dark" })
        {
            var brushes = LoadThemeBrushes(themeName);
            AssertContrast(themeName, brushes, "AppTextBrush", "AppCanvasBrush", 4.5);
            AssertContrast(themeName, brushes, "AppTextBrush", "AppSurfaceBrush", 4.5);
            AssertContrast(themeName, brushes, "AppTextBrush", "AppAccentSoftBrush", 4.5);
            AssertContrast(themeName, brushes, "AppTextBrush", "AppAmberSoftBrush", 4.5);
            AssertContrast(themeName, brushes, "AppTextBrush", "AppBlueSoftBrush", 4.5);
            AssertContrast(themeName, brushes, "AppTextBrush", "AppDangerSoftBrush", 4.5);
            AssertContrast(themeName, brushes, "AppTextMutedBrush", "AppCanvasBrush", 4.5);
            AssertContrast(themeName, brushes, "AppTextMutedBrush", "AppSurfaceBrush", 4.5);
            AssertContrast(themeName, brushes, "AppOnAccentBrush", "AppAccentBrush", 4.5);
            AssertContrast(themeName, brushes, "AppAccentTextBrush", "AppAccentSoftBrush", 4.5);
            AssertContrast(themeName, brushes, "AppNavTextBrush", "AppNavBrush", 4.5);
            AssertContrast(themeName, brushes, "AppNavMutedBrush", "AppNavBrush", 4.5);
            AssertContrast(themeName, brushes, "AppOnAccentBrush", "AppDangerBrush", 4.5);
            AssertContrast(themeName, brushes, "AppDangerBrush", "AppCanvasBrush", 4.5);
            AssertContrast(themeName, brushes, "AppDangerBrush", "AppSurfaceBrush", 4.5);
            AssertContrast(themeName, brushes, "AppNavBrush", "AppAmberBrush", 4.5);
            AssertContrast(themeName, brushes, "AppFocusBrush", "AppCanvasBrush", 3);
            AssertContrast(themeName, brushes, "AppFocusBrush", "AppSurfaceBrush", 3);
        }
    }

    private static IReadOnlyDictionary<string, string> LoadThemeBrushes(string themeName)
    {
        XNamespace presentation = "https://github.com/avaloniaui";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        return LoadTheme(themeName)
            .Descendants(presentation + "SolidColorBrush")
            .ToDictionary(
                element => (string)element.Attribute(xaml + "Key")!,
                element => (string)element.Attribute("Color")!,
                StringComparer.Ordinal);
    }

    private static IReadOnlySet<string> LoadThemeResourceKeys(string themeName)
    {
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        return LoadTheme(themeName)
            .Descendants()
            .Select(element => (string?)element.Attribute(xaml + "Key"))
            .Where(key => key is not null)
            .Select(key => key!)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static XElement LoadTheme(string themeName)
    {
        XNamespace presentation = "https://github.com/avaloniaui";
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        var appXaml = Path.Combine(RepositoryRoot, "src", "Linguistics.App", "App.axaml");
        return XDocument
            .Load(appXaml)
            .Descendants(presentation + "ResourceDictionary")
            .Single(element => (string?)element.Attribute(xaml + "Key") == themeName);
    }

    private static void AssertContrast(
        string theme,
        IReadOnlyDictionary<string, string> brushes,
        string foreground,
        string background,
        double minimum)
    {
        var ratio = Contrast(brushes[foreground], brushes[background]);
        Assert.IsGreaterThanOrEqualTo(
            minimum,
            ratio,
            $"{theme} {foreground} on {background} is {ratio:0.00}:1; expected at least {minimum:0.0}:1.");
    }

    private static double Contrast(string first, string second)
    {
        var firstLuminance = Luminance(first);
        var secondLuminance = Luminance(second);
        return (Math.Max(firstLuminance, secondLuminance) + 0.05) /
               (Math.Min(firstLuminance, secondLuminance) + 0.05);
    }

    private static double Luminance(string color)
    {
        var value = color.TrimStart('#');
        if (value.Length != 6)
        {
            throw new InvalidOperationException($"Only opaque RGB colors are supported; found '{color}'.");
        }

        var channels = Enumerable.Range(0, 3)
            .Select(index => byte.Parse(
                value.AsSpan(index * 2, 2),
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture) / 255d)
            .Select(channel => channel <= 0.04045
                ? channel / 12.92
                : Math.Pow((channel + 0.055) / 1.055, 2.4))
            .ToArray();
        return 0.2126 * channels[0] + 0.7152 * channels[1] + 0.0722 * channels[2];
    }

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../"));
}
