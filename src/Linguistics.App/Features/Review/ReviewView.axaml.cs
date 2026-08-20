using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
        string? contentError)
        : this()
    {
        ArgumentNullException.ThrowIfNull(profile);
        _contentError = contentError;
        if (contentCatalog is not null)
        {
            _graph = contentCatalog.CreateRuntimeConceptGraph(profile.TargetLanguage);
            _cafeDefinition = contentCatalog.CreateRuntimeCafeOrderDefinition();
            _utterances = contentCatalog
                .CreateRuntimePronunciationUtterances(profile.TargetLanguage)
                .ToDictionary(item => item.Id, StringComparer.Ordinal);
        }

        _controller = new ReviewController(profileOwner, _graph);
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
            ShowError("Review is unavailable because the learning service was not initialized.");
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
            StatusText.Text = $"Saved locally. This item is next due {FormatDue(nextDue)}.";
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

        QueueCountText.Text = $"{_snapshot.Queue.Due.Count} due";
        if (_graph is null || _cafeDefinition is null)
        {
            ReviewCard.IsVisible = false;
            EmptyState.IsVisible = false;
            ContentGateState.IsVisible = true;
            ContentGateMessage.Text = string.IsNullOrWhiteSpace(_contentError)
                ? "The installed content is not approved for learner-facing review."
                : "The installed content did not pass the runtime review gate.";
            return;
        }

        ContentGateState.IsVisible = false;
        _current = _snapshot.Queue.Due.FirstOrDefault();
        if (_current is null)
        {
            ReviewCard.IsVisible = false;
            EmptyState.IsVisible = true;
            EmptyMessage.Text = _snapshot.Queue.Upcoming.FirstOrDefault() is { } upcoming
                ? $"Your next local review is due {FormatDue(upcoming.DueAt.ToLocalTime())}."
                : "Complete a learning task or pronunciation attempt and the deterministic queue will prepare the follow-up.";
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
            throw new InvalidOperationException("Reviewed content is unavailable.");
        }

        return schedule.Kind switch
        {
            ReviewItemKind.Phrase => new ReviewDescriptor(
                "Phrase",
                "Rebuild the complete café request from memory.",
                "Think first, then reveal. Exact punctuation is not the learning target.",
                _cafeDefinition.PronunciationTargetText),
            ReviewItemKind.Concept => DescribeConcept(schedule),
            ReviewItemKind.RecurringError => DescribeError(schedule),
            ReviewItemKind.PronunciationTarget when _utterances.TryGetValue(schedule.TargetId, out var utterance) =>
                new ReviewDescriptor(
                    "Pronunciation target",
                    "Recall the phrase, then say it aloud or trace it silently.",
                    "Speech is optional; the complete target remains visible after reveal.",
                    utterance.Text),
            _ => new ReviewDescriptor(
                schedule.Kind.ToString(),
                "Recall this reviewed learning item.",
                "The installed pack provides no richer prompt for this item type.",
                schedule.TargetId),
        };
    }

    private ReviewDescriptor DescribeConcept(ReviewSchedule schedule)
    {
        var concept = _graph!.Get(new ConceptId(schedule.TargetId));
        return new ReviewDescriptor(
            "Capability concept",
            $"What does “{concept.Title}” let you accomplish?",
            "Recall the communicative goal before revealing its reviewed description.",
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
                "Recurring form",
                "Recall the smallest correction that kept the café exchange moving.",
                "This historical rule is not present in the installed content version.",
                schedule.TargetId)
            : new ReviewDescriptor(
                "Recurring form",
                intervention.RetryPrompt,
                "Recall one focused contrast; do not fix every detail at once.",
                intervention.Message);
    }

    private static string FormatDue(DateTimeOffset localDue)
    {
        var remaining = localDue - DateTimeOffset.Now;
        if (remaining <= TimeSpan.FromMinutes(1))
        {
            return "now";
        }

        if (remaining < TimeSpan.FromHours(2))
        {
            return $"in {Math.Ceiling(remaining.TotalMinutes):0} minutes";
        }

        if (remaining < TimeSpan.FromDays(1))
        {
            return $"in {Math.Ceiling(remaining.TotalHours):0} hours";
        }

        return localDue.ToString("ddd, d MMM");
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
