using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Linguistics.App.Persistence;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class JsonLearnerRepositoryTests
{
    private static readonly DateTimeOffset AttemptTime =
        new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task CompleteLearningStateRoundTripsThroughSchemaFiveStore()
    {
        await WithStoreAsync(async (repository, filePath) =>
        {
            var profile = CreateProfile();
            var curriculum = CreateCurriculum();
            var tasks = CreateTaskHistory();
            var pronunciation = CreatePronunciationHistory();
            var review = CreateReviewHistory();

            await repository.SaveAsync(profile);
            await repository.SaveLearningStateAsync(
                profile.Id,
                new LearnerLearningState(
                    curriculum,
                    tasks,
                    pronunciation,
                    review));
            var restoredProfile = await repository.LoadAsync();
            var restoredState = await repository.LoadLearningStateAsync(profile.Id);

            Assert.IsNotNull(restoredProfile);
            Assert.AreEqual(profile.Id, restoredProfile.Id);
            Assert.AreEqual(profile.TargetLanguage, restoredProfile.TargetLanguage);
            Assert.AreEqual(profile.Settings, restoredProfile.Settings);
            CollectionAssert.AreEqual(
                profile.KnownLanguages.ToArray(),
                restoredProfile.KnownLanguages.ToArray());
            AssertCurriculumEqual(curriculum, restoredState.Curriculum);
            AssertTaskHistoryEqual(tasks, restoredState.Tasks);
            CollectionAssert.AreEqual(
                pronunciation.Attempts.ToArray(),
                restoredState.Pronunciation.Attempts.ToArray());
            CollectionAssert.AreEqual(review.Schedules.ToArray(), restoredState.Review.Schedules.ToArray());
            CollectionAssert.AreEqual(review.Attempts.ToArray(), restoredState.Review.Attempts.ToArray());
            StringAssert.Contains(await File.ReadAllTextAsync(filePath), "\"schemaVersion\": 5");
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
            StringAssert.Contains(upgraded, "\"schemaVersion\": 5");
            Assert.AreEqual(profile.Id, (await repository.LoadAsync())?.Id);
            AssertCurriculumEqual(curriculum, await repository.LoadCurriculumAsync(profile.Id));
            AssertTaskHistoryEqual(
                TaskHistory.Empty,
                (await repository.LoadLearningStateAsync(profile.Id)).Tasks);
        });
    }

    [TestMethod]
    public async Task SchemaTwoLoadsWithoutRewriteAndUpgradesOnSuccessfulLearningStateSave()
    {
        await WithStoreAsync(async (repository, filePath) =>
        {
            var profile = CreateProfile();
            var curriculum = CreateCurriculum();
            var schemaTwo = SerializeSchemaTwo(profile, curriculum);
            await File.WriteAllTextAsync(filePath, schemaTwo);

            var restored = await repository.LoadLearningStateAsync(profile.Id);

            AssertCurriculumEqual(curriculum, restored.Curriculum);
            AssertTaskHistoryEqual(TaskHistory.Empty, restored.Tasks);
            Assert.AreEqual(schemaTwo, await File.ReadAllTextAsync(filePath));

            var tasks = CreateTaskHistory();
            await repository.SaveLearningStateAsync(
                profile.Id,
                new LearnerLearningState(
                    curriculum,
                    tasks,
                    Linguistics.Core.Speech.PronunciationHistory.Empty));

            StringAssert.Contains(await File.ReadAllTextAsync(filePath), "\"schemaVersion\": 5");
            AssertTaskHistoryEqual(
                tasks,
                (await repository.LoadLearningStateAsync(profile.Id)).Tasks);
        });
    }

    [TestMethod]
    public async Task SchemaThreeLoadsWithoutRewriteAndAddsEmptyPronunciationOnNextSave()
    {
        await WithStoreAsync(async (repository, filePath) =>
        {
            var profile = CreateProfile();
            var curriculum = CreateCurriculum();
            var tasks = CreateTaskHistory();
            var schemaThree = SerializeSchemaThree(profile, curriculum, tasks);
            await File.WriteAllTextAsync(filePath, schemaThree);

            var restored = await repository.LoadLearningStateAsync(profile.Id);

            AssertCurriculumEqual(curriculum, restored.Curriculum);
            AssertTaskHistoryEqual(tasks, restored.Tasks);
            Assert.IsEmpty(restored.Pronunciation.Attempts);
            Assert.AreEqual(schemaThree, await File.ReadAllTextAsync(filePath));

            await repository.SaveLearningStateAsync(profile.Id, restored);

            var upgraded = await File.ReadAllTextAsync(filePath);
            StringAssert.Contains(upgraded, "\"schemaVersion\": 5");
            StringAssert.Contains(upgraded, "\"pronunciation\"");
            StringAssert.Contains(upgraded, "\"review\"");
        });
    }

    [TestMethod]
    public async Task SchemaFourLoadsWithoutRewriteAndAddsEmptyReviewOnNextSave()
    {
        await WithStoreAsync(async (repository, filePath) =>
        {
            var profile = CreateProfile();
            var curriculum = CreateCurriculum();
            var tasks = CreateTaskHistory();
            var pronunciation = CreatePronunciationHistory();
            var schemaFour = SerializeSchemaFour(profile, curriculum, tasks, pronunciation);
            await File.WriteAllTextAsync(filePath, schemaFour);

            var restored = await repository.LoadLearningStateAsync(profile.Id);

            AssertCurriculumEqual(curriculum, restored.Curriculum);
            AssertTaskHistoryEqual(tasks, restored.Tasks);
            CollectionAssert.AreEqual(pronunciation.Attempts.ToArray(), restored.Pronunciation.Attempts.ToArray());
            Assert.IsEmpty(restored.Review.Schedules);
            Assert.IsEmpty(restored.Review.Attempts);
            Assert.AreEqual(schemaFour, await File.ReadAllTextAsync(filePath));

            await repository.SaveLearningStateAsync(profile.Id, restored);

            var upgraded = await File.ReadAllTextAsync(filePath);
            StringAssert.Contains(upgraded, "\"schemaVersion\": 5");
            StringAssert.Contains(upgraded, "\"review\"");
        });
    }

    [TestMethod]
    public async Task ProfileUpdatePreservesLearningHistory()
    {
        await WithStoreAsync(async (repository, _) =>
        {
            var profile = CreateProfile();
            var curriculum = CreateCurriculum();
            var tasks = CreateTaskHistory();
            var pronunciation = CreatePronunciationHistory();
            var review = CreateReviewHistory();
            await repository.SaveAsync(profile);
            await repository.SaveLearningStateAsync(
                profile.Id,
                new LearnerLearningState(
                    curriculum,
                    tasks,
                    pronunciation,
                    review));

            await repository.SaveAsync(profile with
            {
                Settings = profile.Settings with { Microphone = MicrophonePreference.Never },
            });

            var restored = await repository.LoadLearningStateAsync(profile.Id);
            AssertCurriculumEqual(curriculum, restored.Curriculum);
            AssertTaskHistoryEqual(tasks, restored.Tasks);
            CollectionAssert.AreEqual(
                pronunciation.Attempts.ToArray(),
                restored.Pronunciation.Attempts.ToArray());
            CollectionAssert.AreEqual(review.Schedules.ToArray(), restored.Review.Schedules.ToArray());
            CollectionAssert.AreEqual(review.Attempts.ToArray(), restored.Review.Attempts.ToArray());
        });
    }

    [TestMethod]
    public async Task SelectedLocalModelRoundTripsAsAnOptionalProfileSetting()
    {
        await WithStoreAsync(async (repository, _) =>
        {
            var profile = CreateProfile();
            var configured = profile with
            {
                Settings = profile.Settings with { SelectedLocalModel = "fixture:local" },
            };

            await repository.SaveAsync(configured);
            var restored = await repository.LoadAsync();

            Assert.AreEqual("fixture:local", restored?.Settings.SelectedLocalModel);
        });
    }

    [TestMethod]
    public async Task ReducedMotionRoundTripsAndMissingSchemaFiveSettingDefaultsOffWithoutRewrite()
    {
        await WithStoreAsync(async (repository, filePath) =>
        {
            var profile = CreateProfile();
            await repository.SaveAsync(profile);
            var previousSchemaFive = (await File.ReadAllTextAsync(filePath))
                .Replace(",\n    \"reduceMotion\": false", string.Empty, StringComparison.Ordinal);
            await File.WriteAllTextAsync(filePath, previousSchemaFive);

            var restored = await repository.LoadAsync();

            Assert.IsFalse(restored?.Settings.ReduceMotion);
            Assert.AreEqual(previousSchemaFive, await File.ReadAllTextAsync(filePath));

            await repository.SaveAsync(restored! with
            {
                Settings = restored.Settings with { ReduceMotion = true },
            });

            Assert.IsTrue((await repository.LoadAsync())?.Settings.ReduceMotion);
        });
    }

    [TestMethod]
    public async Task UnsupportedSchemaFailsWithoutChangingTheFile()
    {
        await WithStoreAsync(async (repository, filePath) =>
        {
            const string unsupported = "{\"schemaVersion\":6,\"profile\":null}";
            await File.WriteAllTextAsync(filePath, unsupported);

            var exception = await Assert.ThrowsExactlyAsync<LearnerStoreException>(
                () => repository.LoadAsync());

            StringAssert.Contains(exception.Message, "schema 6 is unsupported");
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
    public async Task MalformedStoreCanBePreservedBeforeStartingAgain()
    {
        await WithStoreAsync(async (repository, filePath) =>
        {
            const string malformed = "{not-json-with-history";
            await File.WriteAllTextAsync(filePath, malformed);
            var siblingPath = Path.Combine(Path.GetDirectoryName(filePath)!, "keep.txt");
            await File.WriteAllTextAsync(siblingPath, "unrelated");

            var result = await repository.PreserveForRecoveryAsync();

            var recoveryPath = Path.Combine(
                Path.GetDirectoryName(filePath)!,
                "Recovery",
                result.RecoveryFileName);
            Assert.AreEqual(1, result.PreservedFileCount);
            Assert.IsFalse(File.Exists(filePath));
            Assert.IsTrue(File.Exists(recoveryPath));
            Assert.AreEqual(malformed, await File.ReadAllTextAsync(recoveryPath));
            Assert.AreEqual("unrelated", await File.ReadAllTextAsync(siblingPath));
            Assert.IsNull(await repository.LoadAsync());
        });
    }

    [TestMethod]
    public async Task UnfinishedWriteRequiresExplicitRecoveryAndIsPreserved()
    {
        await WithStoreAsync(async (repository, filePath) =>
        {
            const string unfinished = "{partial";
            await File.WriteAllTextAsync(filePath + ".tmp", unfinished);

            var exception = await Assert.ThrowsExactlyAsync<LearnerStoreException>(
                () => repository.LoadAsync());
            StringAssert.Contains(exception.Message, "unfinished learner data write");

            var result = await repository.PreserveForRecoveryAsync();
            var recoveryPath = Path.Combine(
                Path.GetDirectoryName(filePath)!,
                "Recovery",
                result.RecoveryFileName);
            Assert.AreEqual(1, result.PreservedFileCount);
            Assert.AreEqual(unfinished, await File.ReadAllTextAsync(recoveryPath));
            Assert.IsFalse(File.Exists(filePath + ".tmp"));
        });
    }

    [TestMethod]
    public async Task LearnerStoreLinksAreRejectedForReadAndDelete()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var targetPath = Path.Combine(
            Path.GetTempPath(),
            $"linguistics-link-target-{Guid.NewGuid():N}.json");
        try
        {
            await WithStoreAsync(async (repository, filePath) =>
            {
                await File.WriteAllTextAsync(targetPath, "outside");
                File.CreateSymbolicLink(filePath, targetPath);

                var read = await Assert.ThrowsExactlyAsync<LearnerStoreException>(
                    () => repository.LoadAsync());
                StringAssert.Contains(read.Message, "filesystem link");
                var delete = await Assert.ThrowsExactlyAsync<LearnerStoreException>(
                    () => repository.DeleteAsync());
                StringAssert.Contains(delete.Message, "filesystem link");
                Assert.AreEqual("outside", await File.ReadAllTextAsync(targetPath));
            });
        }
        finally
        {
            File.Delete(targetPath);
        }
    }

    [TestMethod]
    public async Task InvalidCurrentSchemaCurriculumFailsWithoutChangingTheFile()
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

    private static TaskHistory CreateTaskHistory()
    {
        var attemptId = Guid.NewGuid();
        var attempt = new TaskAttempt(
            attemptId,
            Guid.NewGuid(),
            "de.task.cafe.order-one-item",
            AttemptTime.AddMinutes(-2),
            AttemptTime,
            TurnCount: 2,
            RetryCount: 1,
            new LearningEvidence(
                CommunicativeSuccess: true,
                LinguisticAccuracy: 1,
                Fluency: 0.8,
                Pronunciation: null,
                TargetConceptPerformance: 1,
                Comprehension: null,
                DelayedRecall: null),
            ["de.error.accusative-masculine"],
            new VersionId("language.de.core.v1"),
            new VersionId("cafe-order-evaluator-v1"),
            DialogueRealizationMode.Scripted,
            LocalModel: null,
            DialogueSchemaVersion: "cafe-order-dialogue-v1",
            SelectedBridge: null);
        return new TaskHistory(
            [attempt],
            [new ReviewHandoff(
                Guid.NewGuid(),
                attemptId,
                new ConceptId("de.function.order-polite"),
                AttemptTime,
                attempt.EncounteredErrorRuleIds)]);
    }

    private static PronunciationHistory CreatePronunciationHistory() =>
        new(
        [
            new PronunciationAttempt(
                Guid.NewGuid(),
                "de.utterance.order",
                AttemptTime,
                new PronunciationEvidence(
                    PronunciationAssessmentOutcome.Intelligible,
                    1,
                    5,
                    5,
                    5,
                    TimeSpan.FromSeconds(4),
                    "fixture-recognizer-v1",
                    TranscriptPronunciationAssessmentProvider.Version),
                "language.de.core.v1"),
        ]);

    private static ReviewHistory CreateReviewHistory()
    {
        var itemId = ReviewItemId.Create(ReviewItemKind.Concept, "fixture.concept");
        return new ReviewHistory(
            [new ReviewSchedule(
                itemId,
                ReviewItemKind.Concept,
                "fixture.concept",
                new VersionId("fixture-content-v1"),
                AttemptTime.AddDays(-2),
                AttemptTime,
                AttemptTime.AddDays(3),
                SuccessStreak: 1,
                FailureCount: 0,
                Difficulty: 0.45,
                TimeSpan.FromSeconds(3),
                ReviewConfiguration.Default.Version)],
            [new ReviewAttempt(
                Guid.NewGuid(),
                itemId,
                AttemptTime,
                ReviewRating.Good,
                TimeSpan.FromSeconds(3),
                ReviewConfiguration.Default.Version)]);
    }

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

    private static string SerializeSchemaTwo(
        LearnerProfile profile,
        CurriculumHistory curriculum)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };
        return JsonSerializer.Serialize(
            new { SchemaVersion = 2, Profile = profile, Curriculum = curriculum },
            options);
    }

    private static string SerializeSchemaThree(
        LearnerProfile profile,
        CurriculumHistory curriculum,
        TaskHistory tasks)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };
        var root = JsonSerializer.SerializeToNode(
            new { SchemaVersion = 3, Profile = profile, Curriculum = curriculum, Tasks = tasks },
            options)!.AsObject();
        foreach (var attempt in root["tasks"]!["attempts"]!.AsArray().OfType<JsonObject>())
        {
            attempt.Remove("inputMode");
            attempt.Remove("speechEvidence");
        }

        return root.ToJsonString(options);
    }

    private static string SerializeSchemaFour(
        LearnerProfile profile,
        CurriculumHistory curriculum,
        TaskHistory tasks,
        PronunciationHistory pronunciation)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };
        return JsonSerializer.Serialize(
            new
            {
                SchemaVersion = 4,
                Profile = profile,
                Curriculum = curriculum,
                Tasks = tasks,
                Pronunciation = pronunciation,
            },
            options);
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

    private static void AssertTaskHistoryEqual(TaskHistory expected, TaskHistory actual)
    {
        Assert.HasCount(expected.Attempts.Count, actual.Attempts);
        for (var index = 0; index < expected.Attempts.Count; index++)
        {
            var expectedAttempt = expected.Attempts[index];
            var actualAttempt = actual.Attempts[index];
            Assert.AreEqual(expectedAttempt.Id, actualAttempt.Id);
            Assert.AreEqual(expectedAttempt.SessionId, actualAttempt.SessionId);
            Assert.AreEqual(expectedAttempt.TaskId, actualAttempt.TaskId);
            Assert.AreEqual(expectedAttempt.StartedAt, actualAttempt.StartedAt);
            Assert.AreEqual(expectedAttempt.CompletedAt, actualAttempt.CompletedAt);
            Assert.AreEqual(expectedAttempt.TurnCount, actualAttempt.TurnCount);
            Assert.AreEqual(expectedAttempt.RetryCount, actualAttempt.RetryCount);
            Assert.AreEqual(expectedAttempt.Evidence, actualAttempt.Evidence);
            Assert.AreEqual(expectedAttempt.ContentVersion, actualAttempt.ContentVersion);
            Assert.AreEqual(expectedAttempt.EvaluationVersion, actualAttempt.EvaluationVersion);
            Assert.AreEqual(expectedAttempt.DialogueMode, actualAttempt.DialogueMode);
            Assert.AreEqual(expectedAttempt.LocalModel, actualAttempt.LocalModel);
            Assert.AreEqual(expectedAttempt.DialogueSchemaVersion, actualAttempt.DialogueSchemaVersion);
            Assert.AreEqual(expectedAttempt.SelectedBridge, actualAttempt.SelectedBridge);
            CollectionAssert.AreEqual(
                expectedAttempt.EncounteredErrorRuleIds.ToArray(),
                actualAttempt.EncounteredErrorRuleIds.ToArray());
        }

        Assert.HasCount(expected.ReviewHandoffs.Count, actual.ReviewHandoffs);
        for (var index = 0; index < expected.ReviewHandoffs.Count; index++)
        {
            var expectedHandoff = expected.ReviewHandoffs[index];
            var actualHandoff = actual.ReviewHandoffs[index];
            Assert.AreEqual(expectedHandoff.Id, actualHandoff.Id);
            Assert.AreEqual(expectedHandoff.TaskAttemptId, actualHandoff.TaskAttemptId);
            Assert.AreEqual(expectedHandoff.ConceptId, actualHandoff.ConceptId);
            Assert.AreEqual(expectedHandoff.CreatedAt, actualHandoff.CreatedAt);
            CollectionAssert.AreEqual(
                expectedHandoff.ErrorRuleIds.ToArray(),
                actualHandoff.ErrorRuleIds.ToArray());
        }
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
