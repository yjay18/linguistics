using Avalonia.Controls;
using Linguistics.App.Diagnostics;
using Linguistics.App.Features.Review;
using Linguistics.App.Localization;
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
        ValidatedContentCatalog? contentCatalog,
        LocalDiagnosticLog? diagnosticLog = null)
        : this()
    {
        var instructionLanguage = contentCatalog?
            .SelectInstructionLanguage(profile)
            .SelectedLanguage;
        var graph = instructionLanguage is null
            ? null
            : contentCatalog!.CreateRuntimeConceptGraph(
                profile.TargetLanguage,
                instructionLanguage.Value);
        _controller = new ReviewController(profileOwner, graph, diagnosticLog: diagnosticLog);
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
            ShowError(AppStrings.Get("Progress_Unavailable"));
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
        CapabilityTitle.Text = AppStrings.Get("Progress_CafeCapability_Title");
        CapabilityDescription.Text = AppStrings.Get("Progress_CafeCapability_Description");
        (CapabilityGlyph.Content, CapabilityEvidence.Text) = capability.Status switch
        {
            CapabilityStatus.Demonstrated => (
                "✓",
                capability.LastEvidenceAt is { } evidenceAt
                    ? AppStrings.Format(
                        "Progress_DemonstratedOn",
                        evidenceAt.ToLocalTime().ToString(
                            "d MMM yyyy",
                            AppStrings.CurrentCulture))
                    : AppStrings.Get("Progress_Demonstrated")),
            CapabilityStatus.Practicing => (
                "↗",
                AppStrings.Format("Progress_PracticingAttempts", capability.AttemptCount)),
            _ => ("○", AppStrings.Get("Progress_NotStarted")),
        };
        PracticingCount.Text = snapshot.Progress.PracticingConceptCount.ToString();
        StrongCount.Text = snapshot.Progress.StrongConceptCount.ToString();
        DueCount.Text = snapshot.Progress.DueConceptCount.ToString();
    }

    private void ShowError(string message)
    {
        LoadingState.IsVisible = false;
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
