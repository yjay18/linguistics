using Linguistics.App.Persistence;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class JsonLearnerRepositoryTests
{
    [TestMethod]
    public async Task ProfileRoundTripsThroughSchemaOneStore()
    {
        await WithStoreAsync(async (repository, filePath) =>
        {
            var profile = CreateProfile();

            await repository.SaveAsync(profile);
            var restored = await repository.LoadAsync();

            Assert.IsNotNull(restored);
            Assert.AreEqual(profile.Id, restored.Id);
            Assert.AreEqual(profile.TargetLanguage, restored.TargetLanguage);
            Assert.AreEqual(profile.Settings, restored.Settings);
            CollectionAssert.AreEqual(
                profile.KnownLanguages.ToArray(),
                restored.KnownLanguages.ToArray());
            StringAssert.Contains(await File.ReadAllTextAsync(filePath), "\"schemaVersion\": 1");
        });
    }

    [TestMethod]
    public async Task UnsupportedSchemaFailsWithoutChangingTheFile()
    {
        await WithStoreAsync(async (repository, filePath) =>
        {
            const string unsupported = "{\"schemaVersion\":2,\"profile\":null}";
            await File.WriteAllTextAsync(filePath, unsupported);

            var exception = await Assert.ThrowsExactlyAsync<LearnerStoreException>(
                () => repository.LoadAsync());

            StringAssert.Contains(exception.Message, "schema 2 is unsupported");
            Assert.AreEqual(unsupported, await File.ReadAllTextAsync(filePath));
        });
    }

    [TestMethod]
    public async Task DeleteRemovesOnlyTheLearnerStoreFiles()
    {
        await WithStoreAsync(async (repository, filePath) =>
        {
            await repository.SaveAsync(CreateProfile());
            var siblingPath = Path.Combine(Path.GetDirectoryName(filePath)!, "keep.txt");
            await File.WriteAllTextAsync(siblingPath, "unrelated");

            await repository.DeleteAsync();

            Assert.IsFalse(File.Exists(filePath));
            Assert.IsFalse(File.Exists(filePath + ".tmp"));
            Assert.IsTrue(File.Exists(siblingPath));
        });
    }

    [TestMethod]
    public async Task MissingStoreRestoresAsNoProfile()
    {
        await WithStoreAsync(async (repository, _) =>
            Assert.IsNull(await repository.LoadAsync()));
    }

    private static LearnerProfile CreateProfile() =>
        new(
            Guid.NewGuid(),
            new LanguageCode("de"),
            [
                new KnownLanguage(
                    new LanguageCode("en"),
                    LanguageProficiency.Advanced,
                    ComfortableReading: true,
                    ComfortableListening: true,
                    AllowExplanations: true),
                new KnownLanguage(
                    new LanguageCode("hi"),
                    LanguageProficiency.Advanced,
                    ComfortableReading: true,
                    ComfortableListening: true,
                    AllowExplanations: true),
            ],
            new LearnerSettings(
                MultilingualShortcutMode.AskFirst,
                null,
                MicrophonePreference.Later,
                RetainSpeechRecordings: false));

    private static async Task WithStoreAsync(
        Func<JsonLearnerRepository, string, Task> assertion)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "linguistics-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, "learner-profile.json");

        try
        {
            await assertion(new JsonLearnerRepository(filePath), filePath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
