using Avalonia.Controls;
using Linguistics.App.Features.Review;
using Linguistics.Core.Content;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Progress;

public partial class ProgressView : UserControl
{
    private ReviewController? _controller;
    private bool _initialized;

    public ProgressView()
    {
        InitializeComponent();
        AttachedToVisualTree += async (_, _) => await InitializeAsync();
    }

    public ProgressView(
        LearnerProfile profile,
        LearnerProfileOwner profileOwner,
        ValidatedContentCatalog? contentCatalog)
        : this()
    {
        var graph = contentCatalog?.CreateRuntimeConceptGraph(profile.TargetLanguage);
        _controller = new ReviewController(profileOwner, graph);
    }

    private async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        if (_controller is null)
        {
            ShowError("Progress is unavailable because the learning service was not initialized.");
            return;
        }

        try
        {
            Render(await _controller.InitializeAsync());
        }
        catch (Exception exception) when (
            exception is LearnerStoreException or CurriculumValidationException or ArgumentException)
        {
            ShowError(exception.Message);
        }
    }

    private void Render(LearningSnapshot snapshot)
    {
        LoadingState.IsVisible = false;
        CapabilityCard.IsVisible = true;
        ConceptHeading.IsVisible = true;
        ConceptGrid.IsVisible = true;
        MethodCard.IsVisible = true;

        var capability = snapshot.Progress.Capabilities.Single();
        CapabilityTitle.Text = capability.Definition.Title;
        CapabilityDescription.Text = capability.Definition.Description;
        (CapabilityGlyph.Text, CapabilityEvidence.Text) = capability.Status switch
        {
            CapabilityStatus.Demonstrated => (
                "✓",
                $"Demonstrated locally{FormatEvidenceDate(capability.LastEvidenceAt)}."),
            CapabilityStatus.Practicing => (
                "↗",
                $"Practicing across {capability.AttemptCount} stored attempt{(capability.AttemptCount == 1 ? string.Empty : "s")}."),
            _ => ("○", "Not started yet. No ability is inferred from setup alone."),
        };
        PracticingCount.Text = snapshot.Progress.PracticingConceptCount.ToString();
        StrongCount.Text = snapshot.Progress.StrongConceptCount.ToString();
        DueCount.Text = snapshot.Progress.DueConceptCount.ToString();
    }

    private static string FormatEvidenceDate(DateTimeOffset? evidenceAt) =>
        evidenceAt is null ? string.Empty : $" on {evidenceAt.Value.ToLocalTime():d MMM yyyy}";

    private void ShowError(string message)
    {
        LoadingState.IsVisible = false;
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
