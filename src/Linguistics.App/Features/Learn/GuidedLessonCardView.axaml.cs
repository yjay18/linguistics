using Avalonia.Automation;
using Avalonia.Controls;
using Linguistics.App.Localization;
using Linguistics.Core.Content;

namespace Linguistics.App.Features.Learn;

public partial class GuidedLessonCardView : UserControl
{
    public GuidedLessonCardView()
    {
        InitializeComponent();
    }

    public GuidedLessonCardView(CourseSlide slide)
        : this()
    {
        ArgumentNullException.ThrowIfNull(slide);
        var eyebrow = AppStrings.Get($"Learn_Slide_{slide.Kind}_Eyebrow");
        var title = PresentedTitle(slide);
        var body = PresentedBody(slide);
        EyebrowText.Text = eyebrow.ToUpperInvariant();
        SymbolStamp.Content = Symbol(slide.Kind);
        TitleText.Text = title;
        TitleText.FontSize = slide.Kind == CourseSlideKind.Example ? 40 : 34;
        BodyText.Text = body;
        SupportingText.Text = PresentedSupportingText(slide);
        Card.Classes.Add(slide.Kind == CourseSlideKind.Activity ? "accent" : "soft");
        AutomationProperties.SetName(this, $"{eyebrow}. {title}. {body}");
    }

    private static string Symbol(CourseSlideKind kind) => kind switch
    {
        CourseSlideKind.Welcome => "01",
        CourseSlideKind.Explanation => "✦",
        CourseSlideKind.Example => "Aa",
        CourseSlideKind.Activity => "→",
        CourseSlideKind.Recap => "✓",
        _ => "•",
    };

    private static string PresentedTitle(CourseSlide slide) => slide.Kind switch
    {
        CourseSlideKind.Explanation => AppStrings.Get("Learn_Slide_Explanation_Title"),
        CourseSlideKind.Activity when slide.TaskId is null =>
            AppStrings.Get("Learn_Slide_Recall_Title"),
        _ => LearnView.Clean(slide.Title),
    };

    private static string PresentedBody(CourseSlide slide) =>
        slide.Kind == CourseSlideKind.Activity && slide.TaskId is null
            ? AppStrings.Get("Learn_Slide_Recall_Body")
            : LearnView.Clean(slide.Body);

    private static string PresentedSupportingText(CourseSlide slide) => slide.Kind switch
    {
        CourseSlideKind.Welcome => AppStrings.Format(
            "Learn_Slide_Welcome_Supporting",
            LearnView.Clean(slide.SupportingText).StartsWith("Level ", StringComparison.Ordinal)
                ? LearnView.Clean(slide.SupportingText)["Level ".Length..]
                : LearnView.Clean(slide.SupportingText)),
        CourseSlideKind.Explanation => AppStrings.Get("Learn_Slide_Explanation_Supporting"),
        CourseSlideKind.Activity when slide.TaskId is null =>
            AppStrings.Get("Learn_Slide_Recall_Supporting"),
        CourseSlideKind.Activity => AppStrings.Get("Learn_Slide_Activity_Supporting"),
        CourseSlideKind.Recap => AppStrings.Get("Learn_Slide_Recap_Supporting"),
        _ => LearnView.Clean(slide.SupportingText),
    };
}
