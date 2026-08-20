using Linguistics.Core.Curriculum;

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

    Task DeleteAsync(CancellationToken cancellationToken = default);
}

public sealed class LearnerStoreException : Exception
{
    public LearnerStoreException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
