using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;
using Linguistics.App.Diagnostics;

namespace Linguistics.App.Features.Review;

public sealed record LearningSnapshot(
    LearnerLearningState State,
    ReviewQueue Queue,
    LearningProgressOverview Progress,
    TodayPlan Today);

public sealed record ReviewSubmission(
    ReviewDecision Decision,
    LearningSnapshot Snapshot);

public sealed class ReviewController
{
    public static CapabilityDefinition CafeCapability { get; } = new(
        "cafe-order",
        "de.task.cafe.order-one-item",
        "Order at a café",
        "Request one item politely and complete the exchange.");

    private readonly LearnerProfileOwner _profileOwner;
    private readonly ConceptGraph? _graph;
    private readonly Func<DateTimeOffset> _clock;
    private readonly ReviewConfiguration _configuration;
    private readonly LocalDiagnosticLog? _diagnosticLog;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private LearnerLearningState? _state;

    public ReviewController(
        LearnerProfileOwner profileOwner,
        ConceptGraph? graph,
        Func<DateTimeOffset>? clock = null,
        ReviewConfiguration? configuration = null,
        LocalDiagnosticLog? diagnosticLog = null)
    {
        _profileOwner = profileOwner ?? throw new ArgumentNullException(nameof(profileOwner));
        _graph = graph;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _configuration = configuration ?? ReviewConfiguration.Default;
        _diagnosticLog = diagnosticLog;
        _configuration.Validate();
    }

    public async Task<LearningSnapshot> InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await _profileOwner.LoadLearningStateAsync(cancellationToken).ConfigureAwait(false);
            var review = ReviewHistorySynchronizer.Synchronize(
                state.Review,
                state.Curriculum,
                state.Tasks,
                state.Pronunciation,
                _configuration);
            if (!state.Review.Schedules.SequenceEqual(review.Schedules) ||
                !state.Review.Attempts.SequenceEqual(review.Attempts))
            {
                var synchronized = state with { Review = review };
                await _profileOwner
                    .SaveLearningStateAsync(synchronized, cancellationToken)
                    .ConfigureAwait(false);
                await TryLogAsync(
                    DiagnosticEventCode.ReviewSynchronized,
                    _configuration.Version.Value,
                    cancellationToken).ConfigureAwait(false);
                state = synchronized;
            }

            _state = state;
            return BuildSnapshot(state, _clock());
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ReviewSubmission> RecordAsync(
        ReviewItemId itemId,
        ReviewRating rating,
        TimeSpan responseLatency,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = _state ?? throw new InvalidOperationException(
                "Review must be initialized before an outcome is recorded.");
            var now = _clock();
            var schedule = state.Review.Schedules.SingleOrDefault(item => item.Id == itemId)
                ?? throw new CurriculumValidationException([$"Review item '{itemId}' does not exist."]);
            var decision = ReviewScheduler.Record(
                schedule,
                rating,
                responseLatency,
                now,
                Guid.NewGuid(),
                _configuration);
            var curriculum = decision.Current.Kind == ReviewItemKind.Concept
                ? ReviewProgression.Apply(
                    state.Curriculum,
                    _graph ?? throw new InvalidOperationException(
                        "Reviewed concept content is unavailable; no progress was changed."),
                    decision)
                : state.Curriculum;
            var review = state.Review with
            {
                Schedules = state.Review.Schedules
                    .Where(item => item.Id != itemId)
                    .Append(decision.Current)
                    .OrderBy(item => item.Id.Value, StringComparer.Ordinal)
                    .ToArray(),
                Attempts = state.Review.Attempts
                    .Append(decision.Attempt)
                    .OrderBy(item => item.OccurredAt)
                    .ThenBy(item => item.Id)
                    .ToArray(),
            };
            var updated = state with { Curriculum = curriculum, Review = review };
            ReviewHistoryValidator.Validate(updated.Review);
            await _profileOwner
                .SaveLearningStateAsync(updated, cancellationToken)
                .ConfigureAwait(false);
            await TryLogAsync(
                DiagnosticEventCode.ReviewRecorded,
                _configuration.Version.Value,
                cancellationToken).ConfigureAwait(false);
            _state = updated;
            return new ReviewSubmission(decision, BuildSnapshot(updated, now));
        }
        finally
        {
            _gate.Release();
        }
    }

    private static LearningSnapshot BuildSnapshot(
        LearnerLearningState state,
        DateTimeOffset now)
    {
        var queue = ReviewQueue.Build(state.Review, now);
        var progress = LearningProgressBuilder.Build(
            [CafeCapability],
            state.Curriculum,
            state.Tasks,
            state.Pronunciation,
            queue,
            now);
        return new LearningSnapshot(state, queue, progress, TodayPlanner.Build(progress));
    }

    private async Task TryLogAsync(
        DiagnosticEventCode eventCode,
        string configurationVersion,
        CancellationToken cancellationToken)
    {
        if (_diagnosticLog is null)
        {
            return;
        }

        try
        {
            await _diagnosticLog.WriteAsync(
                DiagnosticCategory.Review,
                eventCode,
                DiagnosticOutcome.Succeeded,
                configurationVersion: configurationVersion,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (DiagnosticLogException)
        {
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The learning-state save has already succeeded; optional logging cannot turn it into a retry.
        }
    }
}
