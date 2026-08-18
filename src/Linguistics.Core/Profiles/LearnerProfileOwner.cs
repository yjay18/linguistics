namespace Linguistics.Core.Profiles;

public sealed class LearnerProfileOwner(ILearnerRepository repository)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Guid? _activeProfileId;

    public async Task<LearnerProfile?> RestoreAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var profile = await repository.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (profile is not null)
            {
                LearnerProfileValidator.Validate(profile);
            }

            _activeProfileId = profile?.Id;
            return profile;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LearnerProfile> CompleteOnboardingAsync(
        NewLearnerProfile input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_activeProfileId is not null)
            {
                throw StateError("A learner profile is already active.");
            }

            var profile = new LearnerProfile(
                Guid.NewGuid(),
                input.TargetLanguage,
                input.KnownLanguages,
                input.Settings);

            await SaveValidatedAsync(profile, cancellationToken).ConfigureAwait(false);
            _activeProfileId = profile.Id;
            return profile;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LearnerProfile> UpdateAsync(
        LearnerProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_activeProfileId is null || profile.Id != _activeProfileId)
            {
                throw StateError("The learner profile is no longer active. Reload or complete setup before saving.");
            }

            await SaveValidatedAsync(profile, cancellationToken).ConfigureAwait(false);
            return profile;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await repository.DeleteAsync(cancellationToken).ConfigureAwait(false);
            _activeProfileId = null;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SaveValidatedAsync(
        LearnerProfile profile,
        CancellationToken cancellationToken)
    {
        LearnerProfileValidator.Validate(profile);
        await repository.SaveAsync(profile, cancellationToken).ConfigureAwait(false);
    }

    private static LearnerProfileValidationException StateError(string message) =>
        new([message]);
}
