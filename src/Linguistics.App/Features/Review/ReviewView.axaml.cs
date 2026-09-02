using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Linguistics.App.Diagnostics;
using Linguistics.App.Localization;
using Linguistics.Core.Content;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Review;

public partial class ReviewView : UserControl
{
    private ReviewController? _controller;
    private ConceptGraph? _graph;
    private CafeOrderDefinition? _cafeDefinition;
    private IReadOnlyDictionary<string, RuntimePronunciationUtterance> _utterances =
        new Dictionary<string, RuntimePronunciationUtterance>();
    private string? _contentError;
    private LearningSnapshot? _snapshot;
    private ReviewSchedule? _current;
    private long _shownAt;
    private bool _busy;
    private bool _initialized;

    public ReviewView()
    {
        InitializeComponent();
        AttachedToVisualTree += async (_, _) => await InitializeAsync();
    }

    public ReviewView(
        LearnerProfile profile,
        LearnerProfileOwner profileOwner,
        ValidatedContentCatalog? contentCatalog,
        string? contentError,
        LocalDiagnosticLog? diagnosticLog = null)
        : this()
    {
        ArgumentNullException.ThrowIfNull(profile);
        _contentError = contentError;
        if (contentCatalog is not null)
        {
            var selection = contentCatalog.SelectInstructionLanguage(profile);
            if (selection.SelectedLanguage is { } instructionLanguage)
            {
                _graph = contentCatalog.CreateRuntimeConceptGraph(
                    profile.TargetLanguage,
                    instructionLanguage);
                _cafeDefinition = contentCatalog.CreateRuntimeCafeOrderDefinition(
                    instructionLanguage);
                _utterances = contentCatalog
                    .CreateRuntimePronunciationUtterances(profile.TargetLanguage)
                    .ToDictionary(item => item.Id, StringComparer.Ordinal);
            }
            else if (string.IsNullOrWhiteSpace(_contentError))
            {
                _contentError = selection.Explanation.Summary;
            }
        }

        _controller = new ReviewController(profileOwner, _graph, diagnosticLog: diagnosticLog);
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
            ShowError(AppStrings.Get("Review_Unavailable"));
            return;
        }

        try
        {
            _snapshot = await _controller.InitializeAsync();
            Render();
        }
        catch (Exception exception) when (
            exception is LearnerStoreException or CurriculumValidationException or ArgumentException)
        {
            ShowError(exception.Message);
        }
    }

    private void OnRevealClicked(object? sender, RoutedEventArgs args)
    {
        if (_current is null || _busy)
        {
            return;
        }

        RevealButton.IsVisible = false;
        AnswerPanel.IsVisible = true;
        RatingPanel.IsVisible = true;
    }

    private async void OnRatingClicked(object? sender, RoutedEventArgs args)
    {
        if (_controller is null ||
            _current is null ||
            _busy ||
            sender is not Button { Tag: string value } ||
            !Enum.TryParse<ReviewRating>(value, out var rating))
        {
            return;
        }

        _busy = true;
        RatingPanel.IsEnabled = false;
        ClearMessages();
        try
        {
            var latency = Stopwatch.GetElapsedTime(_shownAt);
            var submission = await _controller.RecordAsync(_current.Id, rating, latency);
            _snapshot = submission.Snapshot;
            var nextDue = submission.Decision.Current.DueAt.ToLocalTime();
            Render();
            StatusText.Text = AppStrings.Format("Review_Saved", FormatDue(nextDue));
            StatusText.IsVisible = true;
        }
        catch (Exception exception) when (
            exception is LearnerStoreException or CurriculumValidationException or InvalidOperationException)
        {
            ShowError(exception.Message);
            RatingPanel.IsEnabled = true;
        }
        finally
        {
            _busy = false;
        }
    }

    private void Render()
    {
        LoadingState.IsVisible = false;
        ClearMessages();
        if (_snapshot is null)
        {
            return;
        }

        QueueCountText.Text = AppStrings.Format("Review_DueCount", _snapshot.Queue.Due.Count);
        if (_graph is null || _cafeDefinition is null)
        {
            ReviewCard.IsVisible = false;
            EmptyState.IsVisible = false;
            ContentGateState.IsVisible = true;
            ContentGateMessage.Text = string.IsNullOrWhiteSpace(_contentError)
                ? AppStrings.Get("Review_Gate_NotApproved")
                : AppStrings.Get("Review_Gate_Failed");
            return;
        }

        ContentGateState.IsVisible = false;
        _current = _snapshot.Queue.Due.FirstOrDefault();
        if (_current is null)
        {
            ReviewCard.IsVisible = false;
            EmptyState.IsVisible = true;
            EmptyMessage.Text = _snapshot.Queue.Upcoming.FirstOrDefault() is { } upcoming
                ? AppStrings.Format(
                    "Review_NextDue",
                    FormatDue(upcoming.DueAt.ToLocalTime()))
                : AppStrings.Get("Review_Empty_Body");
            return;
        }

        EmptyState.IsVisible = false;
        ReviewCard.IsVisible = true;
        var descriptor = Describe(_current);
        KindText.Text = descriptor.Kind.ToUpperInvariant();
        PromptText.Text = descriptor.Prompt;
        SupportText.Text = descriptor.Support;
        AnswerText.Text = descriptor.Answer;
        RevealButton.IsVisible = true;
        RevealButton.IsEnabled = true;
        AnswerPanel.IsVisible = false;
        RatingPanel.IsVisible = false;
        RatingPanel.IsEnabled = true;
        _shownAt = Stopwatch.GetTimestamp();
    }

    private ReviewDescriptor Describe(ReviewSchedule schedule)
    {
        if (_cafeDefinition is null || _graph is null)
        {
            throw new InvalidOperationException(AppStrings.Get("Review_ContentUnavailable"));
        }

        return schedule.Kind switch
        {
            ReviewItemKind.Phrase => new ReviewDescriptor(
                AppStrings.Get("Review_Kind_Phrase"),
                AppStrings.Get("Review_Phrase_Prompt"),
                AppStrings.Get("Review_Phrase_Support"),
                _cafeDefinition.PronunciationTargetText),
            ReviewItemKind.Concept => DescribeConcept(schedule),
            ReviewItemKind.RecurringError => DescribeError(schedule),
            ReviewItemKind.PronunciationTarget when _utterances.TryGetValue(schedule.TargetId, out var utterance) =>
                new ReviewDescriptor(
                    AppStrings.Get("Review_Kind_Pronunciation"),
                    AppStrings.Get("Review_Pronunciation_Prompt"),
                    AppStrings.Get("Review_Pronunciation_Support"),
                    utterance.Text),
            _ => new ReviewDescriptor(
                schedule.Kind.ToString(),
                AppStrings.Get("Review_Generic_Prompt"),
                AppStrings.Get("Review_Generic_Support"),
                schedule.TargetId),
        };
    }

    private ReviewDescriptor DescribeConcept(ReviewSchedule schedule)
    {
        var concept = _graph!.Get(new ConceptId(schedule.TargetId));
        return new ReviewDescriptor(
            AppStrings.Get("Review_Kind_Concept"),
            AppStrings.Format("Review_Concept_Prompt", concept.Title),
            AppStrings.Get("Review_Concept_Support"),
            concept.Description);
    }

    private ReviewDescriptor DescribeError(ReviewSchedule schedule)
    {
        var interventions = new[]
        {
            _cafeDefinition!.ArticleIntervention,
            _cafeDefinition.CapitalizationIntervention,
            _cafeDefinition.PolitenessIntervention,
        };
        var intervention = interventions.SingleOrDefault(item =>
            string.Equals(item.ErrorRuleId, schedule.TargetId, StringComparison.Ordinal));
        return intervention is null
            ? new ReviewDescriptor(
                AppStrings.Get("Review_Kind_RecurringForm"),
                AppStrings.Get("Review_RecurringMissing_Prompt"),
                AppStrings.Get("Review_RecurringMissing_Support"),
                schedule.TargetId)
            : new ReviewDescriptor(
                AppStrings.Get("Review_Kind_RecurringForm"),
                intervention.RetryPrompt,
                AppStrings.Get("Review_Recurring_Support"),
                intervention.Message);
    }

    private static string FormatDue(DateTimeOffset localDue)
    {
        var remaining = localDue - DateTimeOffset.Now;
        if (remaining <= TimeSpan.FromMinutes(1))
        {
            return AppStrings.Get("Review_Due_Now");
        }

        if (remaining < TimeSpan.FromHours(2))
        {
            return AppStrings.Format(
                "Review_Due_Minutes",
                Math.Ceiling(remaining.TotalMinutes));
        }

        if (remaining < TimeSpan.FromDays(1))
        {
            return AppStrings.Format(
                "Review_Due_Hours",
                Math.Ceiling(remaining.TotalHours));
        }

        return localDue.ToString("ddd, d MMM", AppStrings.CurrentCulture);
    }

    private void ShowError(string message)
    {
        LoadingState.IsVisible = false;
        ErrorText.Text = message;
        ErrorText.IsVisible = true;
    }

    private void ClearMessages()
    {
        StatusText.Text = string.Empty;
        StatusText.IsVisible = false;
        ErrorText.Text = string.Empty;
        ErrorText.IsVisible = false;
    }

    private sealed record ReviewDescriptor(
        string Kind,
        string Prompt,
        string Support,
        string Answer);
}
