using Avalonia.Controls;
using Linguistics.App.Content;
using Linguistics.App.Diagnostics;
using Linguistics.App.Features.Learn.Templates;
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
    private readonly ContentImageCache? _imageCache;
    private readonly bool _shouldReduceMotion;
    private readonly LanguageCode _instructionLanguage = new("en");

    public ProgressView()
    {
        InitializeComponent();
        AttachedToVisualTree += async (_, _) => await InitializeAsync();
    }

    public ProgressView(
        LearnerProfile profile,
        LearnerProfileOwner profileOwner,
        ValidatedContentCatalog? contentCatalog,
        LocalDiagnosticLog? diagnosticLog = null,
        ContentImageCache? imageCache = null)
        : this()
    {
        _imageCache = imageCache;
        _shouldReduceMotion = MotionPreferences.ShouldReduce(profile.Settings.ReduceMotion);
        var instructionLanguage = contentCatalog?
            .SelectInstructionLanguage(profile)
            .SelectedLanguage;
        _instructionLanguage = instructionLanguage ?? AppStrings.CurrentLanguage;
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
        ProgressShelfHost.Content = ProgressShelfRenderer.Render(
            _imageCache,
            CreateTemplateParameters(snapshot.Progress),
            _instructionLanguage,
            _shouldReduceMotion,
            _ => { });
        ProgressShelfHost.IsVisible = true;
    }

    private ResolvedTemplateParameters CreateTemplateParameters(
        LearningProgressOverview progress)
    {
        var capabilities = progress.Capabilities
            .OrderBy(item => item.Definition.Id, StringComparer.Ordinal)
            .Select(item => new
            {
                Progress = item,
                Option = new TemplateOption(item.Definition.Id, CapabilityLabel(item)),
            })
            .ToArray();

        return new ResolvedTemplateParameters(
            new Dictionary<string, ResolvedTemplateParameter>
            {
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        [_instructionLanguage.Value] = AppStrings.Get("ProgressShelf_Instruction"),
                    }),
                ["title"] = new(
                    TemplateParameterKind.Text,
                    Text: AppStrings.Get("ProgressShelf_Title")),
                ["demonstrated"] = new(
                    TemplateParameterKind.OptionList,
                    Options: capabilities
                        .Where(item => item.Progress.Status == CapabilityStatus.Demonstrated)
                        .Select(item => item.Option)
                        .ToArray()),
                ["practicing"] = new(
                    TemplateParameterKind.OptionList,
                    Options: capabilities
                        .Where(item => item.Progress.Status == CapabilityStatus.Practicing)
                        .Select(item => item.Option)
                        .ToArray()),
                ["not-started"] = new(
                    TemplateParameterKind.OptionList,
                    Options: capabilities
                        .Where(item => item.Progress.Status == CapabilityStatus.NotStarted)
                        .Select(item => item.Option)
                        .ToArray()),
                ["empty-copy"] = new(
                    TemplateParameterKind.Text,
                    Text: AppStrings.Get("ProgressShelf_Empty")),
                ["method-note"] = new(
                    TemplateParameterKind.Text,
                    Text: AppStrings.Get("ProgressShelf_Method")),
            });
    }

    private static string CapabilityLabel(CapabilityProgress capability) =>
        string.Equals(capability.Definition.Id, ReviewController.CafeCapability.Id, StringComparison.Ordinal)
            ? AppStrings.Get("Progress_CafeCapability_Title")
            : capability.Definition.Title;

    private void ShowError(string message)
    {
        LoadingState.IsVisible = false;
        ProgressShelfHost.IsVisible = false;
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }
}
