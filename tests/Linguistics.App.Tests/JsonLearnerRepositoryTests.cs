using System.Text.Json;
using System.Text.Json.Serialization;
using Linguistics.App.Persistence;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class JsonLearnerRepositoryTests
{
    private static readonly DateTimeOffset AttemptTime =
        new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task ProfileAndCurriculumRoundTripThroughSchemaTwoStore()
    {
        await WithStoreAsync(async (repository, filePath) =>
        {
            var profile = CreateProfile();
            var curriculum = CreateCurriculum();

            await repository.SaveAsync(profile);
            await repository.SaveCurriculumAsync(profile.Id, curriculum);
            var restoredProfile = await repository.LoadAsync();
            var restoredCurriculum = await repository.LoadCurriculumAsync(profile.Id);

            Assert.IsNotNull(restoredProfile);
            Assert.AreEqual(profile.Id, restoredProfile.Id);
            Assert.AreEqual(profile.TargetLanguage, restoredProfile.TargetLanguage);
            Assert.AreEqual(profile.Settings, restoredProfile.Settings);
            CollectionAssert.AreEqual(
                profile.KnownLanguages.ToArray(),
                restoredProfile.KnownLanguages.ToArray());
            AssertCurriculumEqual(curriculum, restoredCurriculum);
            StringAssert.Contains(await File.ReadAllTextAsync(filePath), "\"schemaVersion\": 2");
        });
    }

    [TestMethod]
    public async Task SchemaOneLoadsWithoutRewriteAndUpgradesOnSuccessfulCurriculumSave()
    {
        await WithStoreAsync(async (repository, filePath) =>
        {
            var profile = CreateProfile();
            var schemaOne = SerializeSchemaOne(profile);
            await File.WriteAllTextAsync(filePath, schemaOne);

            var restored = await repository.LoadAsync();
            var emptyCurriculum = await repository.LoadCurriculumAsync(profile.Id);

            Assert.AreEqual(profile.Id, restored?.Id);
            Assert.IsEmpty(emptyCurriculum.Progress);
            Assert.IsEmpty(emptyCurriculum.Attempts);
            Assert.AreEqual(schemaOne, await File.ReadAllTextAsync(filePath));

            var curriculum = CreateCurriculum();
            await repository.SaveCurriculumAsync(profile.Id, curriculum);

            var upgraded = await File.ReadAllTextAsync(filePath);
            StringAssert.Contains(upgraded, "\"schemaVersion\": 2");
            Assert.AreEqual(profile.Id, (await repository.LoadAsync())?.Id);
            AssertCurriculumEqual(curriculum, await repository.LoadCurriculumAsync(profile.Id));
        });
    }

    [TestMethod]
    public async Task ProfileUpdatePreservesCurriculumHistory()
    {
        await WithStoreAsync(async (repository, _) =>
        {
            var profile = CreateProfile();
            var curriculum = CreateCurriculum();
            await repository.SaveAsync(profile);
            await repository.SaveCurriculumAsync(profile.Id, curriculum);

            await repository.SaveAsync(profile with
            {
                Settings = profile.Settings with { Microphone = MicrophonePreference.Never },
            });

            AssertCurriculumEqual(curriculum, await repository.LoadCurriculumAsync(profile.Id));
        });
    }

    [TestMethod]
    public async Task UnsupportedSchemaFailsWithoutChangingTheFile()
    {
        await WithStoreAsync(async (repository, filePath) =>
        {
            const string unsupported = "{\"schemaVersion\":3,\"profile\":null}";
            await File.WriteAllTextAsync(filePath, unsupported);

            var exception = await Assert.ThrowsExactlyAsync<LearnerStoreException>(
                () => repository.LoadAsync());

            StringAssert.Contains(exception.Message, "schema 3 is unsupported");
            Assert.AreEqual(unsupported, await File.ReadAllTextAsync(filePath));
        });
    }

    [TestMethod]
    public async Task MalformedStoreFailsWithoutChangingTheFile()
    {
        await WithStoreAsync(async (repository, filePath) =>
        {
            const string malformed = "{not-json";
            await File.WriteAllTextAsync(filePath, malformed);

            var exception = await Assert.ThrowsExactlyAsync<LearnerStoreException>(
                () => repository.LoadAsync());

            StringAssert.Contains(exception.Message, "could not be read");
            Assert.AreEqual(malformed, await File.ReadAllTextAsync(filePath));
        });
    }

    [TestMethod]
    public async Task InvalidSchemaTwoCurriculumFailsWithoutChangingTheFile()
    {
        await WithStoreAsync(async (repository, filePath) =>
        {
            var profile = CreateProfile();
            await repository.SaveAsync(profile);
            await repository.SaveCurriculumAsync(profile.Id, CreateCurriculum());
            var invalid = (await File.ReadAllTextAsync(filePath))
                .Replace("\"cognitiveLoad\": 1", "\"cognitiveLoad\": 6", StringComparison.Ordinal);
            await File.WriteAllTextAsync(filePath, invalid);

            var exception = await Assert.ThrowsExactlyAsync<CurriculumValidationException>(
                () => repository.LoadAsync());

            StringAssert.Contains(exception.Message, "cognitive load outside 0 to 5");
            Assert.AreEqual(invalid, await File.ReadAllTextAsync(filePath));
        });
    }

    [TestMethod]
    public async Task CurriculumSaveRequiresTheActiveProfileAndCannotRecreateDeletedData()
    {
        await WithStoreAsync(async (repository, filePath) =>
        {
            var profile = CreateProfile();
            await repository.SaveAsync(profile);

            var mismatch = await Assert.ThrowsExactlyAsync<LearnerStoreException>(() =>
                repository.SaveCurriculumAsync(Guid.NewGuid(), CreateCurriculum()));
            StringAssert.Contains(mismatch.Message, "does not belong");

            await repository.DeleteAsync();
            var deleted = await Assert.ThrowsExactlyAsync<LearnerStoreException>(() =>
                repository.SaveCurriculumAsync(profile.Id, CreateCurriculum()));
            StringAssert.Contains(deleted.Message, "No learner data exists");
            Assert.IsFalse(File.Exists(filePath));
        });
    }

    [TestMethod]
    public async Task DeleteRemovesOnlyTheLearnerStoreFiles()
    {
        await WithStoreAsync(async (repository, filePath) =>
        {
            var profile = CreateProfile();
            await repository.SaveAsync(profile);
            await repository.SaveCurriculumAsync(profile.Id, CreateCurriculum());
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

    private static CurriculumHistory CreateCurriculum()
    {
        var conceptId = new ConceptId("fixture.concept");
        var attempt = new ConceptAttempt(
            Guid.NewGuid(),
            conceptId,
            AttemptTime,
            new LearningEvidence(
                CommunicativeSuccess: true,
                LinguisticAccuracy: 0.75,
                Fluency: 0.6,
                Pronunciation: null,
                TargetConceptPerformance: 0.8,
                Comprehension: 0.9,
                DelayedRecall: null),
            new VersionId("fixture-content-v1"),
            ProgressionConfiguration.Default.Version,
            ConceptSelectionConfiguration.Default.Version,
            new SelectedBridgeReference(
                new TransferMappingId("fixture.bridge"),
                new VersionId("mapping-v1"),
                TransferRoutingConfiguration.Default.Version,
                0.7));

        return new CurriculumHistory(
            [new ConceptProgress(
                conceptId,
                ConceptProgressState.Practicing,
                AttemptCount: 1,
                LastAttemptAt: AttemptTime,
                ReviewDueAt: null,
                RecurringErrorCount: 0,
                CognitiveLoad: 1)],
            [attempt],
            ProgressionConfiguration.Default.Version,
            ConceptSelectionConfiguration.Default.Version);
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

    private static string SerializeSchemaOne(LearnerProfile profile)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };
        return JsonSerializer.Serialize(new { SchemaVersion = 1, Profile = profile }, options);
    }

    private static void AssertCurriculumEqual(
        CurriculumHistory expected,
        CurriculumHistory actual)
    {
        Assert.AreEqual(expected.ProgressionConfigurationVersion, actual.ProgressionConfigurationVersion);
        Assert.AreEqual(expected.SelectionConfigurationVersion, actual.SelectionConfigurationVersion);
        CollectionAssert.AreEqual(expected.Progress.ToArray(), actual.Progress.ToArray());
        CollectionAssert.AreEqual(expected.Attempts.ToArray(), actual.Attempts.ToArray());
    }

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
