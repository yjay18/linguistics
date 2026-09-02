using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Linguistics.App.Content;
using Linguistics.App.Features.Learn.Templates;
using Linguistics.App.Localization;
using Linguistics.Core.Content;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.App.Features.Learn;

public partial class LearnView : UserControl
{
    private readonly Dictionary<Button, CourseLesson> _lessonsByButton = [];
    private readonly TemplateRegistry _templateRegistry = TemplateRegistry.CreateDefault();
    private readonly LearnerProfileOwner? _profileOwner;
    private LanguageCode _instructionLanguage = new("en");
    private bool _shouldReduceMotion;
    private CourseCatalog? _course;
    private CourseLesson? _activeLesson;
    private LearnerLearningState? _learningState;
    private CourseLesson? _resumeLesson;
    private int _slideIndex;
    private bool _canPersistLessonProgress;
    private bool _historyLoadStarted;

    public LearnView()
    {
        InitializeComponent();
    }

    public LearnView(
        LearnerProfile profile,
        ValidatedContentCatalog? contentCatalog,
        string? contentError,
        LearnerProfileOwner? profileOwner = null,
        bool showDeveloperDetails = false,
        ContentImageCache? imageCache = null,
        ISpeechSynthesisProvider? speechSynthesisProvider = null,
        ISpeechRecognitionProvider? speechRecognitionProvider = null,
        IPronunciationAssessmentProvider? pronunciationAssessmentProvider = null)
        : this()
    {
        ArgumentNullException.ThrowIfNull(profile);
        _profileOwner = profileOwner;
        _templateRegistry = TemplateRegistry.CreateDefault(
            imageCache,
            speechSynthesisProvider,
            speechRecognitionProvider,
            pronunciationAssessmentProvider,
            profile.Settings.Microphone != MicrophonePreference.Never);
        _shouldReduceMotion = MotionPreferences.ShouldReduce(profile.Settings.ReduceMotion);
        SlideHost.PageTransition = _shouldReduceMotion
            ? null
            : new CrossFade(TimeSpan.FromMilliseconds(220));

        var instructionSelection = contentCatalog?.SelectInstructionLanguage(profile);

        if (showDeveloperDetails)
        {
            DeveloperDetails.IsVisible = true;
            DeveloperDetailsContent.Content = new CurriculumDiagnosticsView(
                profile,
                contentCatalog,
                contentError,
                profileOwner,
                instructionSelection);
        }

        if (contentCatalog is null)
        {
            ShowError(string.IsNullOrWhiteSpace(contentError)
                ? AppStrings.Get("Learn_NoValidatedContent")
                : contentError);
            return;
        }

        if (instructionSelection?.SelectedLanguage is not { } instructionLanguage)
        {
            ShowError(
                instructionSelection?.Explanation.Summary ??
                AppStrings.Get("Learn_NoInstructionLanguage"));
            return;
        }

        _instructionLanguage = instructionLanguage;

        try
        {
            var course = contentCatalog.CreateCourseCatalog(
                profile.TargetLanguage,
                instructionLanguage);
            RenderCourse(course);
            _canPersistLessonProgress =
                course.PublicationState == CoursePublicationState.Ready && profileOwner is not null;
            if (_canPersistLessonProgress)
            {
                StartCourseButton.IsEnabled = false;
                UnitsPanel.IsEnabled = false;
                AttachedToVisualTree += async (_, _) => await LoadLessonProgressAsync();
            }
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or ArgumentException)
        {
            ShowError(exception.Message);
        }
    }

    private void RenderCourse(CourseCatalog course)
    {
        _course = course;
        PreviewNotice.IsVisible = course.PublicationState == CoursePublicationState.Preview;
        CourseTitleText.Text = course.TargetLanguage.Value == "de"
            ? AppStrings.Get("Learn_GermanFoundations")
            : AppStrings.Format(
                "Learn_TargetFoundations",
                course.TargetLanguage.Value.ToUpperInvariant());
        CourseAvailabilityText.Text = course.PublicationState == CoursePublicationState.Preview
            ? AppStrings.Format("Learn_Availability_Preview", course.AuthoredLessonCount)
            : AppStrings.Format("Learn_Availability_Ready", course.AuthoredLessonCount);
        CatalogProgress.Minimum = 0;
        CatalogProgress.Maximum = course.TargetLessonCount;
        CatalogProgress.Value = course.AuthoredLessonCount;
        AuthoredCountText.Text = course.AuthoredLessonCount.ToString();
        PlannedContentText.Text = course.RemainingLessonCount == 0
            ? AppStrings.Get("Learn_CapacityComplete")
            : AppStrings.Format(
                "Learn_CapacityRemaining",
                course.RemainingLessonCount,
                course.TargetLessonCount);
        UnitsPanel.Children.Clear();
        _lessonsByButton.Clear();

        var lessonNumber = 1;
        foreach (var unit in course.Units)
        {
            UnitsPanel.Children.Add(CreateUnitCard(unit, ref lessonNumber));
        }

        StartCourseButton.IsEnabled = course.AuthoredLessonCount > 0;
    }

    private Control CreateUnitCard(CourseUnit unit, ref int lessonNumber)
    {
        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            ColumnSpacing = 14,
        };
        var number = new Border
        {
            Width = 48,
            Height = 48,
            CornerRadius = new CornerRadius(16),
            Child = new TextBlock
            {
                Text = unit.Number.ToString("00"),
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        number.Classes.Add("lesson-unit-number");
        ((TextBlock)number.Child).Classes.Add("on-accent");
        var title = new StackPanel { Spacing = 3 };
        title.Children.Add(new TextBlock
        {
            Text = AppStrings.Get($"Learn_Unit_{unit.DominantConceptType}_Title"),
            FontSize = 21,
            FontWeight = FontWeight.SemiBold,
        });
        title.Children.Add(new TextBlock
        {
            Text = AppStrings.Get($"Learn_Unit_{unit.DominantConceptType}_Description"),
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.72,
        });
        Grid.SetColumn(title, 1);
        header.Children.Add(number);
        header.Children.Add(title);

        var lessonTiles = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            ItemWidth = 250,
            ItemHeight = 112,
        };
        foreach (var lesson in unit.Lessons)
        {
            var tile = CreateLessonButton(lesson, lessonNumber++);
            lessonTiles.Children.Add(tile);
        }

        var content = new StackPanel { Spacing = 16 };
        content.Children.Add(header);
        content.Children.Add(lessonTiles);
        var card = new Border { Child = content };
        card.Classes.Add("card");
        return card;
    }

    private Button CreateLessonButton(CourseLesson lesson, int number)
    {
        var copy = new StackPanel
        {
            Spacing = 5,
            Margin = new Thickness(2),
        };
        var label = new TextBlock
        {
            Text = AppStrings.Format("Learn_LessonNumber", number),
            FontSize = 10,
            FontWeight = FontWeight.Bold,
            LetterSpacing = 1.1,
        };
        label.Classes.Add("lesson-label");
        copy.Children.Add(label);
        copy.Children.Add(new TextBlock
        {
            Text = Clean(lesson.Title),
            FontSize = 16,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 2,
        });
        copy.Children.Add(new TextBlock
        {
            Text = AppStrings.Format("Learn_ShortCards", lesson.Slides.Count),
            FontSize = 12,
            Opacity = 0.68,
        });

        var button = new Button
        {
            Content = copy,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 0, 10, 10),
        };
        button.Classes.Add("lesson-tile");
        button.Classes.Add("lift");
        AutomationProperties.SetName(
            button,
            AppStrings.Format("Learn_OpenLesson", number, Clean(lesson.Title)));
        button.Click += OnLessonClicked;
        _lessonsByButton.Add(button, lesson);
        return button;
    }

    private async void OnStartCourseClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs args)
    {
        var lesson = _resumeLesson ??
            _course?.Units.SelectMany(unit => unit.Lessons).FirstOrDefault();
        if (lesson is not null)
        {
            await OpenLessonAsync(lesson);
        }
    }

    private async void OnLessonClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs args)
    {
        if (sender is Button button && _lessonsByButton.TryGetValue(button, out var lesson))
        {
            await OpenLessonAsync(lesson);
        }
    }

    private async Task OpenLessonAsync(CourseLesson lesson)
    {
        _activeLesson = lesson;
        var stored = FindStoredProgress(lesson);
        _slideIndex = stored?.IsInProgress == true
            ? Math.Clamp(stored.LastSlideIndex, 0, lesson.Slides.Count - 1)
            : 0;
        CoursePanel.IsVisible = false;
        ErrorPanel.IsVisible = false;
        LessonPanel.IsVisible = true;
        RenderSlide();
        SlideHost.Focus();

        if (_canPersistLessonProgress && stored?.IsInProgress != true)
        {
            await SaveLessonHistoryAsync(history => LessonProgressTracker.Begin(
                history,
                lesson.Id,
                lesson.ConceptId,
                lesson.Slides.Count,
                lesson.ContentVersion,
                DateTimeOffset.UtcNow));
        }
    }

    private async void OnBackClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs args)
    {
        if (_slideIndex == 0)
        {
            CloseLesson();
            return;
        }

        _slideIndex--;
        RenderSlide();
        await SaveCurrentPositionAsync();
    }

    private async void OnContinueClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs args)
    {
        if (_activeLesson is null)
        {
            return;
        }

        if (_slideIndex < _activeLesson.Slides.Count - 1)
        {
            _slideIndex++;
            RenderSlide();
            await SaveCurrentPositionAsync();
            return;
        }

        var completedLesson = _activeLesson;
        var saved = await SaveLessonHistoryAsync(history => LessonProgressTracker.Complete(
            history,
            completedLesson.Id,
            DateTimeOffset.UtcNow));
        SessionStatusText.Text = _canPersistLessonProgress
            ? saved
                ? AppStrings.Format("Learn_VisitSaved", Clean(completedLesson.Title))
                : AppStrings.Format("Learn_VisitNotSaved", Clean(completedLesson.Title))
            : AppStrings.Format("Learn_PreviewVisit", Clean(completedLesson.Title));
        SessionStatusText.IsVisible = true;
        _resumeLesson = null;
        StartCourseButton.Content = AppStrings.Get("Learn_StartFirstLesson");
        CloseLesson();
    }

    private void OnCloseLessonClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs args)
        => CloseLesson();

    private void CloseLesson()
    {
        _activeLesson = null;
        LessonPanel.IsVisible = false;
        CoursePanel.IsVisible = true;
        StartCourseButton.Focus();
    }

    private void RenderSlide()
    {
        if (_activeLesson is null)
        {
            return;
        }

        var slide = _activeLesson.Slides[_slideIndex];
        LessonPositionText.Text = AppStrings.Format(
            "Learn_CardPosition",
            _slideIndex + 1,
            _activeLesson.Slides.Count);
        LessonLevelText.Text = Clean(_activeLesson.CefrApproximation);
        LessonProgress.Minimum = 0;
        LessonProgress.Maximum = _activeLesson.Slides.Count;
        LessonProgress.Value = _slideIndex + 1;
        BackButton.Content = _slideIndex == 0
            ? AppStrings.Get("Learn_CourseMap")
            : AppStrings.Get("Common_Back");
        ContinueButton.Content = _slideIndex == _activeLesson.Slides.Count - 1
            ? AppStrings.Get("Learn_FinishLesson")
            : AppStrings.Get("Common_Continue");
        LessonTemplateOutcomeText.IsVisible = false;
        SlideHost.Content = CreateSlideCard(slide);
    }

    private Control CreateSlideCard(CourseSlide slide)
    {
        if (slide.TemplateInstance is { } template)
        {
            return _templateRegistry.Render(
                template.TemplateId,
                template.Parameters,
                _instructionLanguage,
                _shouldReduceMotion,
                OnTemplateOutcome);
        }

        var content = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            MinHeight = 340,
        };
        var eyebrow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
        };
        var presentedEyebrow = AppStrings.Get($"Learn_Slide_{slide.Kind}_Eyebrow");
        var presentedTitle = PresentedTitle(slide);
        var presentedBody = PresentedBody(slide);
        var presentedSupportingText = PresentedSupportingText(slide);
        var eyebrowText = new TextBlock
        {
            Text = presentedEyebrow.ToUpperInvariant(),
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            LetterSpacing = 1.4,
        };
        eyebrowText.Classes.Add("lesson-label");
        eyebrow.Children.Add(eyebrowText);
        var symbol = new Border
        {
            Width = 46,
            Height = 46,
            CornerRadius = new CornerRadius(23),
            Child = new TextBlock
            {
                Text = Symbol(slide.Kind),
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        symbol.Classes.Add("lesson-symbol");
        ((TextBlock)symbol.Child).Classes.Add("on-accent");
        AutomationProperties.SetAccessibilityView(symbol, AccessibilityView.Raw);
        Grid.SetColumn(symbol, 1);
        eyebrow.Children.Add(symbol);

        var main = new StackPanel
        {
            Spacing = 18,
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 680,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        main.Children.Add(new TextBlock
        {
            Text = presentedTitle,
            FontSize = slide.Kind == CourseSlideKind.Example ? 40 : 34,
            FontWeight = FontWeight.SemiBold,
            LineHeight = 44,
            TextWrapping = TextWrapping.Wrap,
        });
        main.Children.Add(new TextBlock
        {
            Text = presentedBody,
            FontSize = 19,
            LineHeight = 28,
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.84,
        });
        Grid.SetRow(main, 1);

        var supporting = new Border
        {
            Padding = new Thickness(16, 13),
            CornerRadius = new CornerRadius(13),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = presentedSupportingText,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 22,
            },
        };
        supporting.Classes.Add("lesson-supporting");
        Grid.SetRow(supporting, 2);
        content.Children.Add(eyebrow);
        content.Children.Add(main);
        content.Children.Add(supporting);

        var card = new Border { Child = content };
        card.Classes.Add(slide.Kind == CourseSlideKind.Activity ? "accent-card" : "hero-card");
        AutomationProperties.SetName(
            card,
            $"{presentedEyebrow}. {presentedTitle}. {presentedBody}");
        return card;
    }

    private void OnTemplateOutcome(TemplateOutcome outcome)
    {
        LessonTemplateOutcomeText.Text = outcome.State switch
        {
            TemplateOutcomeState.Success =>
                AppStrings.Get("Learn_Outcome_Success"),
            TemplateOutcomeState.Uncertain =>
                AppStrings.Get("Learn_Outcome_Uncertain"),
            TemplateOutcomeState.Failure =>
                AppStrings.Get("Learn_Outcome_Failure"),
            _ => AppStrings.Get("Learn_Outcome_Ready"),
        };
        LessonTemplateOutcomeText.IsVisible = true;
    }

    internal static string Clean(string value) =>
        value
            .Replace('-', ' ')
            .Replace('–', ' ')
            .Replace('—', ' ');

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
        _ => Clean(slide.Title),
    };

    private static string PresentedBody(CourseSlide slide) =>
        slide.Kind == CourseSlideKind.Activity && slide.TaskId is null
            ? AppStrings.Get("Learn_Slide_Recall_Body")
            : Clean(slide.Body);

    private static string PresentedSupportingText(CourseSlide slide) => slide.Kind switch
    {
        CourseSlideKind.Welcome =>
            AppStrings.Format(
                "Learn_Slide_Welcome_Supporting",
                Clean(slide.SupportingText).StartsWith("Level ", StringComparison.Ordinal)
                    ? Clean(slide.SupportingText)["Level ".Length..]
                    : Clean(slide.SupportingText)),
        CourseSlideKind.Explanation =>
            AppStrings.Get("Learn_Slide_Explanation_Supporting"),
        CourseSlideKind.Activity when slide.TaskId is null =>
            AppStrings.Get("Learn_Slide_Recall_Supporting"),
        CourseSlideKind.Activity => AppStrings.Get("Learn_Slide_Activity_Supporting"),
        CourseSlideKind.Recap => AppStrings.Get("Learn_Slide_Recap_Supporting"),
        _ => Clean(slide.SupportingText),
    };

    private void ShowError(string message)
    {
        CoursePanel.IsVisible = false;
        LessonPanel.IsVisible = false;
        ErrorText.Text = Clean(message);
        ErrorPanel.IsVisible = true;
    }

    private async Task LoadLessonProgressAsync()
    {
        if (_historyLoadStarted || _profileOwner is null)
        {
            return;
        }

        _historyLoadStarted = true;
        try
        {
            _learningState = await _profileOwner.LoadLearningStateAsync();
            var lessons = _course?.Units.SelectMany(unit => unit.Lessons).ToArray() ?? [];
            var resume = _learningState.Lessons.Lessons
                .Where(progress => progress.IsInProgress)
                .OrderByDescending(progress => progress.LastVisitedAt)
                .Select(progress => new
                {
                    Progress = progress,
                    Lesson = lessons.FirstOrDefault(lesson =>
                        string.Equals(lesson.Id, progress.LessonId, StringComparison.Ordinal) &&
                        lesson.ContentVersion == progress.ContentVersion &&
                        lesson.Slides.Count == progress.SlideCount),
                })
                .FirstOrDefault(item => item.Lesson is not null);
            if (resume?.Lesson is { } lesson)
            {
                _resumeLesson = lesson;
                var resumeSlideIndex = resume.Progress.LastSlideIndex;
                StartCourseButton.Content = AppStrings.Get("Learn_ResumeLesson");
                SessionStatusText.Text = AppStrings.Format(
                    "Learn_ResumeReady",
                    Clean(lesson.Title),
                    resumeSlideIndex + 1);
                SessionStatusText.IsVisible = true;
            }
        }
        catch (Exception exception) when (
            exception is LearnerStoreException or CurriculumValidationException or
                InvalidOperationException or ArgumentException)
        {
            _canPersistLessonProgress = false;
            SessionStatusText.Text = AppStrings.Get("Learn_ProgressUnavailable");
            SessionStatusText.IsVisible = true;
        }
        finally
        {
            StartCourseButton.IsEnabled = _course?.AuthoredLessonCount > 0;
            UnitsPanel.IsEnabled = true;
        }
    }

    private LessonProgress? FindStoredProgress(CourseLesson lesson) =>
        _learningState?.Lessons.Lessons.FirstOrDefault(progress =>
            progress.IsInProgress &&
            string.Equals(progress.LessonId, lesson.Id, StringComparison.Ordinal) &&
            progress.ContentVersion == lesson.ContentVersion &&
            progress.SlideCount == lesson.Slides.Count);

    private Task<bool> SaveCurrentPositionAsync()
    {
        if (_activeLesson is null)
        {
            return Task.FromResult(false);
        }

        var lessonId = _activeLesson.Id;
        var slideIndex = _slideIndex;
        return SaveLessonHistoryAsync(history => LessonProgressTracker.Move(
            history,
            lessonId,
            slideIndex,
            DateTimeOffset.UtcNow));
    }

    private async Task<bool> SaveLessonHistoryAsync(
        Func<LessonHistory, LessonHistory> update)
    {
        if (!_canPersistLessonProgress || _profileOwner is null || _learningState is null)
        {
            return false;
        }

        ContinueButton.IsEnabled = false;
        BackButton.IsEnabled = false;
        try
        {
            _learningState = _learningState with
            {
                Lessons = update(_learningState.Lessons),
            };
            await _profileOwner.SaveLearningStateAsync(_learningState);
            return true;
        }
        catch (Exception exception) when (
            exception is LearnerStoreException or CurriculumValidationException or
                InvalidOperationException or ArgumentException)
        {
            SessionStatusText.Text = AppStrings.Get("Learn_ProgressSaveFailed");
            SessionStatusText.IsVisible = true;
            return false;
        }
        finally
        {
            ContinueButton.IsEnabled = true;
            BackButton.IsEnabled = true;
        }
    }
}
