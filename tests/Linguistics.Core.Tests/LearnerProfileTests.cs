using Linguistics.Core.Profiles;

namespace Linguistics.Core.Tests;

[TestClass]
public sealed class LearnerProfileTests
{
    [TestMethod]
    public void LanguageCodeNormalizesSupportedIdentifiers()
    {
        Assert.AreEqual("en", new LanguageCode(" EN ").Value);
        Assert.AreEqual("de-de", new LanguageCode("de-DE").Value);
    }

    [TestMethod]
    public void LanguageCodeRejectsPathsAndMalformedValues()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new LanguageCode("../en"));
        Assert.ThrowsExactly<ArgumentException>(() => new LanguageCode("e"));
        Assert.ThrowsExactly<ArgumentException>(() => new LanguageCode("en_uk"));
    }

    [TestMethod]
    public void ValidationRejectsDuplicateKnownLanguages()
    {
        var english = Known("en", allowExplanations: true);
        var profile = Profile(
            [english, english],
            new LearnerSettings(MultilingualShortcutMode.AskFirst, null, MicrophonePreference.Later, false));

        var exception = Assert.ThrowsExactly<LearnerProfileValidationException>(
            () => LearnerProfileValidator.Validate(profile));

        StringAssert.Contains(exception.Message, "appears more than once");
    }

    [TestMethod]
    public void PreferredLanguageMustBeEligibleForExplanations()
    {
        var profile = Profile(
            [Known("en", allowExplanations: false)],
            new LearnerSettings(
                MultilingualShortcutMode.PreferredLanguage,
                new LanguageCode("en"),
                MicrophonePreference.Later,
                false));

        var exception = Assert.ThrowsExactly<LearnerProfileValidationException>(
            () => LearnerProfileValidator.Validate(profile));

        StringAssert.Contains(exception.Message, "must be an allowed known language");
    }

    [TestMethod]
    public void ValidationRejectsDefaultLanguageIdentifiers()
    {
        var profile = new LearnerProfile(
            Guid.NewGuid(),
            default,
            [new KnownLanguage(default, LanguageProficiency.Advanced, true, true, true)],
            new LearnerSettings(
                MultilingualShortcutMode.AskFirst,
                null,
                MicrophonePreference.Later,
                false));

        var exception = Assert.ThrowsExactly<LearnerProfileValidationException>(
            () => LearnerProfileValidator.Validate(profile));

        StringAssert.Contains(exception.Message, "target language is missing");
        StringAssert.Contains(exception.Message, "known-language identifier is missing");
    }

    [TestMethod]
    public async Task OwnerCreatesRestoresAndDeletesAValidatedProfile()
    {
        var repository = new InMemoryLearnerRepository();
        var owner = new LearnerProfileOwner(repository);
        var input = new NewLearnerProfile(
            new LanguageCode("de"),
            [Known("en", true), Known("hi", true)],
            new LearnerSettings(
                MultilingualShortcutMode.AskFirst,
                null,
                MicrophonePreference.Later,
                false));

        var created = await owner.CompleteOnboardingAsync(input);

        Assert.AreNotEqual(Guid.Empty, created.Id);
        Assert.AreEqual(created, await owner.RestoreAsync());

        var updated = created with
        {
            Settings = created.Settings with { Microphone = MicrophonePreference.Never },
        };
        Assert.AreEqual(updated, await owner.UpdateAsync(updated));
        Assert.AreEqual(updated, await owner.RestoreAsync());

        await owner.DeleteAllAsync();
        Assert.IsNull(await owner.RestoreAsync());
    }

    [TestMethod]
    public async Task OwnerRejectsAStaleUpdateAfterDeletion()
    {
        var repository = new InMemoryLearnerRepository();
        var owner = new LearnerProfileOwner(repository);
        var created = await owner.CompleteOnboardingAsync(new NewLearnerProfile(
            new LanguageCode("de"),
            [Known("en", true)],
            new LearnerSettings(
                MultilingualShortcutMode.AskFirst,
                null,
                MicrophonePreference.Later,
                false)));

        await owner.DeleteAllAsync();

        var exception = await Assert.ThrowsExactlyAsync<LearnerProfileValidationException>(
            () => owner.UpdateAsync(created));

        StringAssert.Contains(exception.Message, "no longer active");
        Assert.IsNull(await owner.RestoreAsync());
    }

    private static LearnerProfile Profile(
        IReadOnlyList<KnownLanguage> languages,
        LearnerSettings settings) =>
        new(Guid.NewGuid(), new LanguageCode("de"), languages, settings);

    private static KnownLanguage Known(string code, bool allowExplanations) =>
        new(
            new LanguageCode(code),
            LanguageProficiency.Advanced,
            ComfortableReading: true,
            ComfortableListening: true,
            AllowExplanations: allowExplanations);

    private sealed class InMemoryLearnerRepository : ILearnerRepository
    {
        private LearnerProfile? _profile;

        public Task<LearnerProfile?> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_profile);

        public Task SaveAsync(
            LearnerProfile profile,
            CancellationToken cancellationToken = default)
        {
            _profile = profile;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CancellationToken cancellationToken = default)
        {
            _profile = null;
            return Task.CompletedTask;
        }
    }
}
