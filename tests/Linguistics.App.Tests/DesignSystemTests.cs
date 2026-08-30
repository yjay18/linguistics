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
            AssertContrast(themeName, brushes, "AppTextBrush", "AppPaperBrush", 4.5);
            AssertContrast(themeName, brushes, "AppTextMutedBrush", "AppPaperBrush", 4.5);
            AssertContrast(themeName, brushes, "AppTextBrush", "AppPaperAltBrush", 4.5);
            AssertContrast(themeName, brushes, "AppTextMutedBrush", "AppPaperAltBrush", 4.5);
            AssertContrast(themeName, brushes, "AppPaperEdgeBrush", "AppPaperBrush", 1.35);
            AssertContrast(themeName, brushes, "AppPaperEdgeBrush", "AppPaperAltBrush", 1.35);
            AssertContrast(themeName, brushes, "AppCutoutEdgeBrush", "AppCutoutMarginBrush", 1.35);
            AssertContrast(themeName, brushes, "AppTapeEdgeBrush", "AppTapeBrush", 3);
            AssertContrast(themeName, brushes, "AppStampInkBrush", "AppStampBrush", 4.5);
            AssertContrast(themeName, brushes, "AppTornEdgeBrush", "AppCanvasBrush", 1.1);
            AssertContrast(themeName, brushes, "AppOnAccentBrush", "AppAccentBrush", 4.5);
            AssertContrast(themeName, brushes, "AppAccentTextBrush", "AppAccentSoftBrush", 4.5);
            AssertContrast(themeName, brushes, "AppNavTextBrush", "AppNavBrush", 4.5);
            AssertContrast(themeName, brushes, "AppNavMutedBrush", "AppNavBrush", 4.5);
            AssertContrast(themeName, brushes, "AppNavAccentBrush", "AppNavSurfaceBrush", 4.5);
            AssertContrast(themeName, brushes, "AppOnAccentBrush", "AppDangerBrush", 4.5);
            AssertContrast(themeName, brushes, "AppDangerBrush", "AppCanvasBrush", 4.5);
            AssertContrast(themeName, brushes, "AppDangerBrush", "AppSurfaceBrush", 4.5);
            AssertContrast(themeName, brushes, "AppNavBrush", "AppAmberBrush", 4.5);
            AssertContrast(themeName, brushes, "AppFocusBrush", "AppCanvasBrush", 3);
            AssertContrast(themeName, brushes, "AppFocusBrush", "AppSurfaceBrush", 3);
        }
    }

    [TestMethod]
    public void PaperMaterialsExistInBothThemesAndUseTintedShadows()
    {
        var expectedResources = new[]
        {
            "AppPaperBrush",
            "AppPaperAltBrush",
            "AppPaperEdgeBrush",
            "AppCutoutMarginBrush",
            "AppCutoutEdgeBrush",
            "AppPaperShadow",
            "AppCutoutShadow",
            "AppPaperGrainBrush",
            "AppTapeBrush",
            "AppTapeEdgeBrush",
            "AppStampBrush",
            "AppStampInkBrush",
            "AppStampShadow",
            "AppTornEdgeBrush",
        };
        var colorExpression = new Regex("#[0-9A-Fa-f]{8}", RegexOptions.CultureInvariant);

        foreach (var themeName in new[] { "Light", "Dark" })
        {
            var theme = LoadTheme(themeName);
            var resources = LoadThemeResourceKeys(themeName);
            foreach (var resource in expectedResources)
            {
                Assert.Contains(resource, resources, $"{themeName} is missing {resource}.");
            }

            var shadowColors = theme
                .Descendants()
                .Where(element => element.Name.LocalName == "BoxShadows")
                .SelectMany(element => colorExpression.Matches(element.Value).Select(match => match.Value));
            foreach (var color in shadowColors)
            {
                Assert.AreNotEqual(
                    "000000",
                    color[^6..],
                    true,
                    CultureInfo.InvariantCulture,
                    $"{themeName} paper shadows must be tinted rather than pure black.");
            }
        }
    }

    [TestMethod]
    public void RecordingRetentionIsNotOfferedInLearnerFacingViews()
    {
        foreach (var relativePath in new[]
                 {
                     Path.Combine("Features", "Onboarding", "OnboardingView.axaml"),
                     Path.Combine("Features", "Settings", "SettingsView.axaml"),
                 })
        {
            var markup = File.ReadAllText(Path.Combine(
                RepositoryRoot,
                "src",
                "Linguistics.App",
                relativePath));
            Assert.IsFalse(markup.Contains("RetainRecordings", StringComparison.Ordinal));
            Assert.IsFalse(markup.Contains("Keep speech recordings", StringComparison.Ordinal));
            Assert.IsFalse(markup.Contains("Speech recording retention", StringComparison.Ordinal));
        }

        var settingsCode = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Linguistics.App",
            "Features",
            "Settings",
            "SettingsView.axaml.cs"));
        Assert.IsFalse(settingsCode.Contains("future retention preference", StringComparison.Ordinal));
    }

    [TestMethod]
    public void PrimaryShellSurfacesUsePaperMaterialControls()
    {
        var appRoot = Path.Combine(RepositoryRoot, "src", "Linguistics.App");
        AssertMaterialCounts(
            Path.Combine(appRoot, "Features", "Shell", "ShellView.axaml"),
            ("PaperTape", 2),
            ("CutoutFrame", 3));
        AssertMaterialCounts(
            Path.Combine(appRoot, "Features", "Today", "TodayView.axaml"),
            ("PaperCard", 3),
            ("PaperTape", 1),
            ("PaperStamp", 1),
            ("CutoutFrame", 3));
        AssertMaterialCounts(
            Path.Combine(appRoot, "Features", "Progress", "ProgressView.axaml"),
            ("PaperCard", 3),
            ("PaperTape", 2),
            ("PaperStamp", 1),
            ("CutoutFrame", 3));
        AssertMaterialCounts(
            Path.Combine(appRoot, "Features", "Settings", "SettingsView.axaml"),
            ("PaperCard", 7));
    }

    [TestMethod]
    public void PaperStageSandboxUsesRasterCutoutsInsteadOfVectorSceneArt()
    {
        var appRoot = Path.Combine(RepositoryRoot, "src", "Linguistics.App");
        var sandbox = XDocument.Load(Path.Combine(
            appRoot,
            "Features",
            "Developer",
            "PaperStageSandboxView.axaml"));
        var vectorPrimitives = new HashSet<string>(
            ["Path", "Ellipse", "Rectangle", "Polygon"],
            StringComparer.Ordinal);

        Assert.IsEmpty(sandbox
            .Descendants()
            .Where(element => vectorPrimitives.Contains(element.Name.LocalName))
            .ToArray());

        var sources = sandbox
            .Descendants()
            .Where(element => element.Name.LocalName == "Image")
            .Select(element => (string?)element.Attribute("Source"))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
        foreach (var fileName in new[]
                 {
                     "market-backdrop.png",
                     "market-stall-cutout.png",
                     "market-foreground-cutout.png",
                     "learner-cutout.png",
                     "success-burst-cutout.png",
                 })
        {
            Assert.Contains($"/Assets/PaperStage/{fileName}", sources);
            Assert.IsTrue(File.Exists(Path.Combine(appRoot, "Assets", "PaperStage", fileName)));
        }
    }

    [TestMethod]
    public void KeyFrameAnimationsDoNotTargetUnsupportedRenderTransform()
    {
        XNamespace presentation = "https://github.com/avaloniaui";
        var appXaml = Path.Combine(RepositoryRoot, "src", "Linguistics.App", "App.axaml");
        var unsupportedSetters = XDocument
            .Load(appXaml)
            .Descendants(presentation + "Animation")
            .Descendants(presentation + "Setter")
            .Where(element => (string?)element.Attribute("Property") == "RenderTransform")
            .ToArray();

        Assert.IsEmpty(unsupportedSetters);
    }

    [TestMethod]
    public void StaticUserFacingCopyDoesNotUseDashes()
    {
        var visibleAttributes = new HashSet<string>(
            ["Text", "Content", "Tag", "AutomationProperties.Name"],
            StringComparer.Ordinal);
        var values = Directory
            .EnumerateFiles(
                Path.Combine(RepositoryRoot, "src", "Linguistics.App"),
                "*.axaml",
                SearchOption.AllDirectories)
            .SelectMany(path => XDocument.Load(path).Descendants())
            .SelectMany(element => element.Attributes())
            .Where(attribute => visibleAttributes.Contains(attribute.Name.LocalName))
            .Select(attribute => attribute.Value);

        foreach (var value in values)
        {
            Assert.IsFalse(value.Contains('-') || value.Contains('–') || value.Contains('—'), value);
        }
    }

    [TestMethod]
    public void DynamicCourseCopyRemovesDashCharacters()
    {
        Assert.AreEqual(
            "first person singular",
            global::Linguistics.App.Features.Learn.LearnView.Clean("first-person singular"));
        Assert.AreEqual(
            "one two three",
            global::Linguistics.App.Features.Learn.LearnView.Clean("one–two—three"));
    }

    [TestMethod]
    public void LearnCardsDoNotResolveThemeResourcesBeforeVisualAttachment()
    {
        var code = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Linguistics.App",
            "Features",
            "Learn",
            "LearnView.axaml.cs"));

        Assert.IsFalse(code.Contains(".FindResource(", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains(".TryFindResource(", StringComparison.Ordinal));
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

    private static void AssertMaterialCounts(
        string path,
        params (string ControlName, int MinimumCount)[] expectations)
    {
        var document = XDocument.Load(path);
        foreach (var (controlName, minimumCount) in expectations)
        {
            var count = document
                .Descendants()
                .Count(element => element.Name.LocalName == controlName);
            Assert.IsGreaterThanOrEqualTo(
                minimumCount,
                count,
                $"{Path.GetFileName(path)} should use at least {minimumCount} {controlName} controls.");
        }
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
