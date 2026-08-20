using Linguistics.App.Diagnostics;
using Linguistics.App.Persistence;
using Linguistics.App.Speech;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class LocalLearningDataDeletionTests
{
    [TestMethod]
    public async Task DeleteAllRemovesPersonalAppDataAndPreservesSeparateArtifacts()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "linguistics-delete-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var learnerPath = Path.Combine(directory, "learner-profile.json");
            var recordingsPath = Path.Combine(directory, "Speech Recordings");
            var diagnosticsPath = Path.Combine(directory, "diagnostics.jsonl");
            var recoveryDirectory = Path.Combine(directory, "Recovery");
            var recoveryPath = Path.Combine(
                recoveryDirectory,
                "learner-data-20260820-120000-0123456789abcdef0123456789abcdef.json");
            var unrelatedRecoveryPath = Path.Combine(recoveryDirectory, "keep.txt");
            var modelPath = Path.Combine(directory, "Models", "model.bin");
            var unrelatedPath = Path.Combine(directory, "keep.txt");
            Directory.CreateDirectory(recordingsPath);
            Directory.CreateDirectory(Path.GetDirectoryName(recoveryPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
            await File.WriteAllBytesAsync(Path.Combine(recordingsPath, "attempt.wav"), [1, 2, 3]);
            await File.WriteAllTextAsync(Path.Combine(recordingsPath, "keep.txt"), "unrelated recording sibling");
            await File.WriteAllTextAsync(recoveryPath, "preserved history");
            await File.WriteAllTextAsync(unrelatedRecoveryPath, "unrelated recovery sibling");
            await File.WriteAllTextAsync(modelPath, "external model placeholder");
            await File.WriteAllTextAsync(unrelatedPath, "unrelated root sibling");

            var repository = new JsonLearnerRepository(learnerPath);
            var profile = new LearnerProfile(
                Guid.NewGuid(),
                new LanguageCode("de"),
                [],
                new LearnerSettings(
                    MultilingualShortcutMode.Never,
                    null,
                    MicrophonePreference.Never,
                    RetainSpeechRecordings: false));
            await repository.SaveAsync(profile);
            var owner = new LearnerProfileOwner(repository);
            await owner.RestoreAsync();
            var log = new LocalDiagnosticLog(diagnosticsPath);
            await log.WriteAsync(
                DiagnosticCategory.Application,
                DiagnosticEventCode.AppOpened,
                DiagnosticOutcome.Succeeded);

            await LocalLearningDataDeletion.DeleteAllAsync(
                owner,
                new SpeechRecordingStore(recordingsPath),
                log);

            Assert.IsFalse(File.Exists(learnerPath));
            Assert.IsFalse(File.Exists(learnerPath + ".tmp"));
            Assert.IsFalse(File.Exists(Path.Combine(recordingsPath, "attempt.wav")));
            Assert.IsFalse(File.Exists(diagnosticsPath));
            Assert.AreEqual(
                "unrelated recording sibling",
                await File.ReadAllTextAsync(Path.Combine(recordingsPath, "keep.txt")));
            Assert.IsFalse(File.Exists(recoveryPath));
            Assert.AreEqual(
                "unrelated recovery sibling",
                await File.ReadAllTextAsync(unrelatedRecoveryPath));
            Assert.AreEqual("external model placeholder", await File.ReadAllTextAsync(modelPath));
            Assert.AreEqual("unrelated root sibling", await File.ReadAllTextAsync(unrelatedPath));
            Assert.IsNull(await repository.LoadAsync());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
