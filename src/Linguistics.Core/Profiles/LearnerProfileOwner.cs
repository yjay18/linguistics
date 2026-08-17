namespace Linguistics.Core.Profiles;

public sealed class LearnerProfileOwner(ILearnerRepository repository)
{
    public async Task<LearnerProfile?> RestoreAsync(CancellationToken cancellationToken = default)
    {
        var profile = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (profile is not null)
        {
            LearnerProfileValidator.Validate(profile);
        }

        return profile;
    }

    public async Task<LearnerProfile> CompleteOnboardingAsync(
        NewLearnerProfile input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var profile = new LearnerProfile(
            Guid.NewGuid(),
            input.TargetLanguage,
            input.KnownLanguages,
            input.Settings);

        await SaveValidatedAsync(profile, cancellationToken).ConfigureAwait(false);
        return profile;
    }

    public async Task<LearnerProfile> UpdateAsync(
        LearnerProfile profile,
        CancellationToken cancellationToken = default)
    {
        await SaveValidatedAsync(profile, cancellationToken).ConfigureAwait(false);
        return profile;
    }

    public Task DeleteAllAsync(CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(cancellationToken);

    private async Task SaveValidatedAsync(
        LearnerProfile profile,
        CancellationToken cancellationToken)
    {
        LearnerProfileValidator.Validate(profile);
        await repository.SaveAsync(profile, cancellationToken).ConfigureAwait(false);
    }
}
