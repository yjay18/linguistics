using Avalonia.Controls;
using Linguistics.Core.Content;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Learn;

public partial class CurriculumDiagnosticsView : UserControl
{
    private TargetConceptContent[] _concepts = [];
    private TransferMappingContent[] _mappings = [];

    public CurriculumDiagnosticsView()
    {
        InitializeComponent();
        ConceptList.SelectionChanged += OnConceptSelectionChanged;
        TransferList.SelectionChanged += OnTransferSelectionChanged;
    }

    public CurriculumDiagnosticsView(
        LearnerProfile profile,
        ValidatedContentCatalog? contentCatalog,
        string? contentError)
        : this()
    {
        ArgumentNullException.ThrowIfNull(profile);
        ShowContentPreview(contentCatalog, contentError);
        ShowDiagnostics(BuildDiagnostics(profile, DateTimeOffset.UtcNow));
    }

    private void ShowContentPreview(
        ValidatedContentCatalog? catalog,
        string? contentError)
    {
        if (catalog is null)
        {
            ContentStatusText.Text = string.IsNullOrWhiteSpace(contentError)
                ? "The developer content catalog was not loaded."
                : $"Content validation failed closed: {contentError}";
            ContentBrowserPanel.IsVisible = false;
            return;
        }

        _concepts = catalog.Packs
            .SelectMany(pack => pack.Concepts)
            .OrderBy(concept => concept.Id, StringComparer.Ordinal)
            .ToArray();
        _mappings = catalog.Packs
            .SelectMany(pack => pack.TransferMappings)
            .OrderBy(mapping => mapping.Id, StringComparer.Ordinal)
            .ToArray();
        var tasks = catalog.Packs
            .SelectMany(pack => pack.Tasks)
            .OrderBy(task => task.Id, StringComparer.Ordinal)
            .ToArray();
        var pendingLicenses = catalog.Packs.Count(pack =>
            pack.Manifest.License.ReviewStatus != LicenseReviewStatus.Reviewed);

        ContentStatusText.Text =
            $"Validated {catalog.Packs.Count} packs, {_concepts.Length} concepts, " +
            $"{tasks.Length} tasks, and {_mappings.Length} transfer records. " +
            $"{pendingLicenses} pack licenses and all linguistic claims remain approval-gated.";
        ConceptList.ItemsSource = _concepts.Select(concept => $"{concept.Id} — {concept.Title}").ToArray();
        TransferList.ItemsSource = _mappings
            .Select(mapping => $"{mapping.SourceLanguage} → {mapping.TargetLanguage}: {mapping.Relation} — {mapping.TargetConceptId}")
            .ToArray();
        TaskSummaryText.Text = string.Join(
            Environment.NewLine,
            tasks.Select(task =>
                $"• {task.Goal} ({task.Domain}; {task.States.Count} states, " +
                $"{task.Transitions.Count} transitions, {task.SuccessConditions.Count} success contract)"));

        if (_concepts.Length > 0)
        {
            ConceptList.SelectedIndex = 0;
        }

        if (_mappings.Length > 0)
        {
            TransferList.SelectedIndex = 0;
        }
    }

    private void OnConceptSelectionChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (ConceptList.SelectedIndex < 0 || ConceptList.SelectedIndex >= _concepts.Length)
        {
            ConceptDetailText.Text = string.Empty;
            return;
        }

        var concept = _concepts[ConceptList.SelectedIndex];
        ConceptDetailText.Text =
            $"{concept.Title} ({concept.CefrApproximation}, {concept.Type})\n" +
            $"{concept.Description}\n" +
            $"Prerequisites: {(concept.PrerequisiteIds.Count == 0 ? "none" : string.Join(", ", concept.PrerequisiteIds))}\n" +
            $"Examples: {string.Join(" | ", concept.Examples.Select(example => $"{example.Text} — {example.Meaning}"))}\n" +
            $"Review: {concept.Review.Status}; human reviewer not recorded.";
    }

    private void OnTransferSelectionChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (TransferList.SelectedIndex < 0 || TransferList.SelectedIndex >= _mappings.Length)
        {
            TransferDetailText.Text = string.Empty;
            return;
        }

        var mapping = _mappings[TransferList.SelectedIndex];
        TransferDetailText.Text =
            $"Draft {mapping.Relation} note for {mapping.TargetConceptId}:\n" +
            $"{mapping.LearnerExplanation}\n" +
            $"Risks: {(mapping.NegativeTransferRisks.Count == 0 ? "none recorded" : string.Join(" | ", mapping.NegativeTransferRisks))}\n" +
            $"Sources: {string.Join(", ", mapping.SourceIds)}; review: {mapping.Review.Status}.";
    }

    private void ShowDiagnostics(CurriculumDiagnostic diagnostic)
    {
        SelectedConceptText.Text = diagnostic.SelectedConcept;
        SelectionReasonText.Text = diagnostic.SelectionReason;
        BridgeText.Text = diagnostic.Bridge;
        LessonText.Text = diagnostic.Lesson;
        ConfigurationText.Text = diagnostic.Configuration;
    }

    private static CurriculumDiagnostic BuildDiagnostics(
        LearnerProfile profile,
        DateTimeOffset now)
    {
        var reviewId = new ConceptId("fixture.review");
        var readyId = new ConceptId("fixture.ready");
        var contentVersion = new VersionId("synthetic-fixture-v1");
        var graph = new ConceptGraph([
            Node(reviewId, profile.TargetLanguage, contentVersion),
            Node(readyId, profile.TargetLanguage, contentVersion),
        ]);
        var progress = new[]
        {
            new ConceptProgress(
                reviewId,
                ConceptProgressState.ReviewDue,
                AttemptCount: 2,
                LastAttemptAt: now.AddDays(-3),
                ReviewDueAt: now.AddDays(-1),
                RecurringErrorCount: 1,
                CognitiveLoad: 1),
            new ConceptProgress(
                readyId,
                ConceptProgressState.Available,
                AttemptCount: 0,
                LastAttemptAt: null,
                ReviewDueAt: null,
                RecurringErrorCount: 0,
                CognitiveLoad: 0),
        };
        var selection = ConceptSelector.Select(
            graph,
            progress,
            new ConceptSelectionContext(
                now,
                Seed: 20260819,
                new HashSet<string>(["fixture"], StringComparer.Ordinal),
                new Dictionary<ConceptId, double>
                {
                    [reviewId] = 0.85,
                    [readyId] = 0.8,
                }),
            ConceptSelectionConfiguration.Default);
        var selectedConcept = selection.SelectedConcept!;
        var routing = TransferRouter.Route(
            selectedConcept,
            [
                Mapping("en", 0.8, selectedConcept, contentVersion),
                Mapping("hi", 0.85, selectedConcept, contentVersion),
            ],
            profile,
            TransferPresentationMode.Written,
            TransferRoutingConfiguration.Default);
        var lesson = LessonComposer.Compose(selection);
        var selectedScore = selection.Explanation.Candidates[0];

        return new CurriculumDiagnostic(
            $"{selectedConcept.Id} ({selection.Explanation.Reason})",
            $"Score {selectedScore.Total:0.###}: review urgency " +
            $"{selectedScore.Factors.ReviewUrgency:0.###}, readiness " +
            $"{selectedScore.Factors.PrerequisiteReadiness:0.###}, recurring error " +
            $"{selectedScore.Factors.RecurringError:0.###}, task relevance " +
            $"{selectedScore.Factors.TaskRelevance:0.###}, transfer opportunity " +
            $"{selectedScore.Factors.TransferOpportunity:0.###}, cognitive-load penalty " +
            $"{selectedScore.Factors.CognitiveLoadPenalty:0.###}.",
            routing.Selection is { } bridge
                ? $"{bridge.Mapping.Id} selected at {bridge.Score:0.###}. " +
                  (bridge.RequiresConfirmation ? "Learner confirmation is required." : "No confirmation is required.")
                : routing.Explanation.Summary,
            $"{lesson.TaskType}: {string.Join(", ", lesson.Components)}.",
            $"Configuration versions: {selection.Explanation.ConfigurationVersion}, " +
            $"{routing.Explanation.ConfigurationVersion}, {ProgressionConfiguration.Default.Version}.");
    }

    private static ConceptNode Node(
        ConceptId id,
        LanguageCode targetLanguage,
        VersionId contentVersion) =>
        new(
            id,
            targetLanguage,
            ConceptType.Pragmatic,
            $"Synthetic {id}",
            "Synthetic developer fixture without a linguistic claim.",
            Cefr: null,
            [],
            ["Complete the synthetic fixture."],
            [],
            ["fixture"],
            contentVersion);

    private static TransferMapping Mapping(
        string sourceLanguage,
        double strength,
        ConceptNode concept,
        VersionId version) =>
        new(
            new TransferMappingId($"fixture.{sourceLanguage}.{concept.Id.Value}"),
            version,
            new LanguageCode(sourceLanguage),
            concept.TargetLanguage,
            concept.Id,
            TransferRelation.Facilitative,
            strength,
            TransferReviewStatus.Approved);

    private sealed record CurriculumDiagnostic(
        string SelectedConcept,
        string SelectionReason,
        string Bridge,
        string Lesson,
        string Configuration);
}
