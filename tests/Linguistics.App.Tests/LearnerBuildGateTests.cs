using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Linguistics.App.Tests;

[TestClass]
[DoNotParallelize]
public sealed class LearnerBuildGateTests
{
    private static readonly string[] ForbiddenBuilderTerms =
    [
        "authored",
        "authoring",
        "deterministic check",
        "fixture",
        "template gallery",
        "paper stage",
        "sandbox",
        "developer mode",
    ];

    [TestMethod]
    public void GateProcessRunsWithDeveloperModeOff()
    {
        Assert.AreNotEqual(
            "1",
            Environment.GetEnvironmentVariable("LINGUISTICS_DEVELOPER_MODE"),
            "Run the learner-build gate with LINGUISTICS_DEVELOPER_MODE unset.");
        Assert.IsNull(
            Environment.GetEnvironmentVariable("LINGUISTICS_DEVELOPER_PAGE"),
            "Run the learner-build gate with LINGUISTICS_DEVELOPER_PAGE unset.");
    }

    [TestMethod]
    public void LearnerNavigationRemovesDeveloperDestinationsFromTheCollection()
    {
        var shellRoot = Path.Combine(
            RepositoryRoot,
            "src",
            "Linguistics.App",
            "Features",
            "Shell");
        var markup = XDocument.Load(Path.Combine(shellRoot, "ShellView.axaml"));
        var declaredDestinations = markup
            .Descendants()
            .Where(element => element.Name.LocalName == "ListBoxItem")
            .Select(element => element.Attribute("Tag")?.Value)
            .ToArray();
        CollectionAssert.AreEqual(
            new[]
            {
                "Today",
                "Learn",
                "Scenarios",
                "Pronunciation",
                "Review",
                "Progress",
                "Languages",
                "Settings",
                "TemplateGallery",
                "PaperStage",
            },
            declaredDestinations,
            "Only the reviewed learner destinations and two developer-only destinations may be declared.");

        var source = File.ReadAllText(Path.Combine(shellRoot, "ShellView.axaml.cs"));
        var learnerBranch = Regex.Match(
            source,
            @"if\s*\(!developerMode\)\s*\{(?<body>.*?)\}",
            RegexOptions.CultureInvariant | RegexOptions.Singleline);
        Assert.IsTrue(learnerBranch.Success, "Shell must have an explicit developer-mode-off branch.");
        StringAssert.Contains(
            learnerBranch.Groups["body"].Value,
            "NavigationList.Items.Remove(TemplateGalleryNavItem);");
        StringAssert.Contains(
            learnerBranch.Groups["body"].Value,
            "NavigationList.Items.Remove(PaperStageNavItem);");
        StringAssert.Contains(
            source,
            "private static string? RequestedDeveloperPage() =>\n        DeveloperModeEnabled()");
    }

    [TestMethod]
    public void LearnerLabelsContainNoKnownBuilderVocabulary()
    {
        var findings = new List<string>();
        FindResourceViolations(findings);
        FindMarkupViolations(findings);
        FindCodeAssignedLabelViolations(findings);

        Assert.IsEmpty(
            findings,
            "Builder-facing vocabulary reached a learner-visible label:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, findings));
    }

    [TestMethod]
    public void LearnerProgressIndicatorsMatchTheReviewedInventory()
    {
        var appRoot = Path.Combine(RepositoryRoot, "src", "Linguistics.App");
        var actual = Directory
            .EnumerateFiles(appRoot, "*.axaml", SearchOption.AllDirectories)
            .Where(path => !IsDeveloperOnlyPath(path))
            .SelectMany(path => XDocument
                .Load(path)
                .Descendants()
                .Where(element => element.Name.LocalName == "ProgressBar")
                .Select(element =>
                {
                    var relativePath = Path.GetRelativePath(appRoot, path).Replace('\\', '/');
                    var name = Attribute(element, "Name") ??
                               Attribute(element, "AutomationProperties.Name") ??
                               "<unnamed>";
                    return $"{relativePath}|{name}";
                }))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var expected = new[]
        {
            "Features/Learn/LearnView.axaml|LessonProgress",
            "Features/Onboarding/OnboardingView.axaml|StepProgress",
            "Features/Progress/ProgressView.axaml|{loc:Localize Progress_LoadingName}",
            "Features/Pronunciation/PronunciationView.axaml|{loc:Localize Pronunciation_LoadingName}",
            "Features/Pronunciation/PronunciationView.axaml|{loc:Localize Pronunciation_RecordingName}",
            "Features/Review/ReviewView.axaml|{loc:Localize Review_LoadingName}",
            "Features/Scenarios/CafeOrderView.axaml|{loc:Localize Scenario_LoadingName}",
            "Features/Scenarios/CafeOrderView.axaml|{loc:Localize Scenario_RecordingName}",
            "Features/Today/TodayView.axaml|{loc:Localize Today_Preparing}",
            "MainWindow.axaml|StartupProgress",
        }.Order(StringComparer.Ordinal).ToArray();

        CollectionAssert.AreEqual(
            expected,
            actual,
            "Every learner-visible progress indicator requires an explicit learning/operation source review.");
    }

    [TestMethod]
    public void DeterminateProgressUsesLearnerActivitySources()
    {
        var onboarding = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Linguistics.App",
            "Features",
            "Onboarding",
            "OnboardingView.axaml.cs"));
        var learn = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "src",
            "Linguistics.App",
            "Features",
            "Learn",
            "LearnView.axaml.cs"));

        StringAssert.Contains(onboarding, "StepProgress.Value = step + 1;");
        StringAssert.Contains(learn, "LessonProgress.Maximum = _activeLesson.Slides.Count;");
        StringAssert.Contains(learn, "LessonProgress.Value = _slideIndex + 1;");
        Assert.IsFalse(
            onboarding.Contains("Author", StringComparison.OrdinalIgnoreCase),
            "Onboarding progress must not depend on authoring state.");
        Assert.IsFalse(
            LinesAssigningProgress(learn).Any(line =>
                line.Contains("Author", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Plan", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Template", StringComparison.OrdinalIgnoreCase)),
            "Lesson progress must be based on the active learner lesson, not authoring totals.");
    }

    private static IEnumerable<string> LinesAssigningProgress(string source) =>
        source
            .Split('\n')
            .Where(line => line.Contains("LessonProgress.", StringComparison.Ordinal));

    private static void FindResourceViolations(List<string> findings)
    {
        var resources = Path.Combine(RepositoryRoot, "src", "Linguistics.App", "Resources");
        var referencedKeys = ReferencedLearnerResourceKeys();
        foreach (var path in Directory.EnumerateFiles(resources, "AppStrings*.resx"))
        {
            var document = XDocument.Load(path, LoadOptions.SetLineInfo);
            foreach (var data in document.Descendants().Where(element => element.Name.LocalName == "data"))
            {
                if (data.Attribute("name")?.Value is not { } key || !referencedKeys.Contains(key))
                {
                    continue;
                }

                var value = data.Elements().SingleOrDefault(element => element.Name.LocalName == "value")?.Value;
                AddFindingIfForbidden(
                    findings,
                    Path.GetRelativePath(RepositoryRoot, path),
                    LineNumber(data),
                    value);
            }
        }
    }

    private static void FindMarkupViolations(List<string> findings)
    {
        var appRoot = Path.Combine(RepositoryRoot, "src", "Linguistics.App");
        foreach (var path in Directory.EnumerateFiles(appRoot, "*.axaml", SearchOption.AllDirectories))
        {
            if (IsDeveloperOnlyPath(path))
            {
                continue;
            }

            var document = XDocument.Load(path, LoadOptions.SetLineInfo);
            foreach (var element in document.Descendants().Where(IsLearnerReachableElement))
            {
                foreach (var attribute in element.Attributes().Where(attribute =>
                             attribute.Name.LocalName is "Text" or "Content" or "Header" or
                                 "Watermark" or "AutomationProperties.Name" or
                                 "AutomationProperties.HelpText"))
                {
                    AddFindingIfForbidden(
                        findings,
                        Path.GetRelativePath(RepositoryRoot, path),
                        LineNumber(element),
                        attribute.Value);
                }
            }
        }
    }

    private static void FindCodeAssignedLabelViolations(List<string> findings)
    {
        var appRoot = Path.Combine(RepositoryRoot, "src", "Linguistics.App");
        foreach (var path in Directory.EnumerateFiles(appRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (IsDeveloperOnlyPath(path) ||
                Path.GetFileName(path).StartsWith("TemplateGallery", StringComparison.Ordinal) ||
                Path.GetFileName(path).StartsWith("CurriculumDiagnostics", StringComparison.Ordinal))
            {
                continue;
            }

            var lineNumber = 0;
            foreach (var line in File.ReadLines(path))
            {
                lineNumber++;
                if (!IsVisibleLabelAssignment(line))
                {
                    continue;
                }

                foreach (Match match in Regex.Matches(
                             line,
                             "(?:\\$)?\\\"(?<value>(?:\\\\.|[^\\\"])*)\\\"",
                             RegexOptions.CultureInvariant))
                {
                    AddFindingIfForbidden(
                        findings,
                        Path.GetRelativePath(RepositoryRoot, path),
                        lineNumber,
                        match.Groups["value"].Value);
                }
            }
        }
    }

    private static HashSet<string> ReferencedLearnerResourceKeys()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var appRoot = Path.Combine(RepositoryRoot, "src", "Linguistics.App");
        var markupKey = new Regex(
            @"Localize\s+(?<key>[A-Za-z0-9_]+)",
            RegexOptions.CultureInvariant);
        foreach (var path in Directory.EnumerateFiles(appRoot, "*.axaml", SearchOption.AllDirectories))
        {
            if (IsDeveloperOnlyPath(path))
            {
                continue;
            }

            var document = XDocument.Load(path);
            foreach (var value in document
                         .Descendants()
                         .Where(IsLearnerReachableElement)
                         .SelectMany(element => element.Attributes())
                         .Select(attribute => attribute.Value))
            {
                foreach (Match match in markupKey.Matches(value))
                {
                    keys.Add(match.Groups["key"].Value);
                }
            }
        }

        var codeKey = new Regex(
            "AppStrings\\.(?:Get|Format)\\(\\s*\\\"(?<key>[A-Za-z0-9_]+)\\\"",
            RegexOptions.CultureInvariant);
        foreach (var path in Directory.EnumerateFiles(appRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (IsDeveloperOnlyPath(path) ||
                Path.GetFileName(path).StartsWith("TemplateGallery", StringComparison.Ordinal) ||
                Path.GetFileName(path).StartsWith("CurriculumDiagnostics", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (Match match in codeKey.Matches(File.ReadAllText(path)))
            {
                keys.Add(match.Groups["key"].Value);
            }
        }

        return keys;
    }

    private static bool IsVisibleLabelAssignment(string line) =>
        line.Contains("Text =", StringComparison.Ordinal) ||
        line.Contains(".Text =", StringComparison.Ordinal) ||
        line.Contains("Content =", StringComparison.Ordinal) ||
        line.Contains(".Content =", StringComparison.Ordinal) ||
        line.Contains("AutomationProperties.SetName", StringComparison.Ordinal) ||
        line.Contains("AutomationProperties.SetHelpText", StringComparison.Ordinal);

    private static bool IsLearnerReachableElement(XElement element) =>
        !element
            .AncestorsAndSelf()
            .Any(ancestor =>
                ancestor.Name.LocalName == "ListBoxItem" &&
                ancestor.Attribute("Tag")?.Value is "TemplateGallery" or "PaperStage");

    private static bool IsDeveloperOnlyPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/Features/Developer/", StringComparison.Ordinal) ||
               normalized.EndsWith("/Features/Learn/Templates/TemplateGalleryView.axaml", StringComparison.Ordinal) ||
               normalized.EndsWith("/Features/Learn/CurriculumDiagnosticsView.axaml", StringComparison.Ordinal);
    }

    private static void AddFindingIfForbidden(
        List<string> findings,
        string path,
        int lineNumber,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var term = ForbiddenBuilderTerms.FirstOrDefault(candidate =>
            value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
        if (term is not null)
        {
            findings.Add($"{path.Replace('\\', '/')}:{lineNumber}: [{term}] {value.Trim()}");
        }
    }

    private static int LineNumber(XObject node) =>
        node is IXmlLineInfo lineInfo && lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0;

    private static string? Attribute(XElement element, string localName) =>
        element.Attributes().SingleOrDefault(attribute => attribute.Name.LocalName == localName)?.Value;

    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../"));
}
