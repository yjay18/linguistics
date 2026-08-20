using Linguistics.Core.Curriculum;
using Linguistics.Core.Speech;

namespace Linguistics.Core.Profiles;

public interface ILearnerRepository
{
    Task<LearnerProfile?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(LearnerProfile profile, CancellationToken cancellationToken = default);

    Task<CurriculumHistory> LoadCurriculumAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task SaveCurriculumAsync(
        Guid profileId,
        CurriculumHistory history,
        CancellationToken cancellationToken = default);

    Task<LearnerLearningState> LoadLearningStateAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task SaveLearningStateAsync(
        Guid profileId,
        LearnerLearningState state,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(CancellationToken cancellationToken = default);
}

public sealed record LearnerLearningState(
    CurriculumHistory Curriculum,
    TaskHistory Tasks,
    PronunciationHistory Pronunciation,
    ReviewHistory Review)
{
    public LearnerLearningState(
        CurriculumHistory curriculum,
        TaskHistory tasks,
        PronunciationHistory pronunciation)
        : this(curriculum, tasks, pronunciation, ReviewHistory.Empty)
    {
    }
}

public sealed class LearnerStoreException : Exception
{
    public LearnerStoreException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
