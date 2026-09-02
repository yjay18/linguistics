using System.Text.Json;
using System.Text.Json.Serialization;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.App.Persistence;

public sealed class JsonLearnerRepository : ILearnerRepository
{
    public const int CurrentSchemaVersion = 7;
    private const int LessonHistorySchemaVersion = 6;
    private const int ReviewSchemaVersion = 5;
    private const int PronunciationSchemaVersion = 4;
    private const int TaskHistorySchemaVersion = 3;
    private const int CurriculumSchemaVersion = 2;
    private const int LegacyProfileSchemaVersion = 1;
    private const long MaximumStoreBytes = 1_048_576;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string _directory;
    private readonly string _filePath;
    private readonly string _temporaryFilePath;
    private readonly string _recoveryDirectory;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonLearnerRepository(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _filePath = Path.GetFullPath(filePath);
        _directory = Path.GetDirectoryName(_filePath)
            ?? throw new ArgumentException("The learner store must have a parent directory.", nameof(filePath));
        _temporaryFilePath = _filePath + ".tmp";
        _recoveryDirectory = Path.Combine(_directory, "Recovery");
    }

    public async Task<LearnerProfile?> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return (await ReadEnvelopeAsync(cancellationToken).ConfigureAwait(false))?.Profile;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(
        LearnerProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        LearnerProfileValidator.Validate(profile);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var existing = await ReadEnvelopeAsync(cancellationToken).ConfigureAwait(false);
            if (existing?.Profile is { } storedProfile && storedProfile.Id != profile.Id)
            {
                throw new LearnerStoreException(
                    "The learner store belongs to a different profile and was not overwritten.");
            }

            await WriteEnvelopeAsync(
                new LearnerStoreEnvelope(
                    CurrentSchemaVersion,
                    profile,
                    existing?.Curriculum ?? CurriculumHistory.Empty,
                    existing?.Tasks ?? TaskHistory.Empty,
                    existing?.Pronunciation ?? PronunciationHistory.Empty,
                    existing?.Review ?? ReviewHistory.Empty,
                    existing?.Lessons ?? LessonHistory.Empty),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CurriculumHistory> LoadCurriculumAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("The profile ID is required.", nameof(profileId));
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var envelope = await RequireProfileAsync(profileId, cancellationToken).ConfigureAwait(false);
            CurriculumHistoryValidator.Validate(envelope.Curriculum);
            return envelope.Curriculum;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveCurriculumAsync(
        Guid profileId,
        CurriculumHistory history,
        CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("The profile ID is required.", nameof(profileId));
        }

        CurriculumHistoryValidator.Validate(history);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var envelope = await RequireProfileAsync(profileId, cancellationToken).ConfigureAwait(false);
            await WriteEnvelopeAsync(
                envelope with { SchemaVersion = CurrentSchemaVersion, Curriculum = history },
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LearnerLearningState> LoadLearningStateAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("The profile ID is required.", nameof(profileId));
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var envelope = await RequireProfileAsync(profileId, cancellationToken).ConfigureAwait(false);
            CurriculumHistoryValidator.Validate(envelope.Curriculum);
            TaskHistoryValidator.Validate(envelope.Tasks);
            PronunciationHistoryValidator.Validate(envelope.Pronunciation);
            ReviewHistoryValidator.Validate(envelope.Review);
            LessonHistoryValidator.Validate(envelope.Lessons);
            return new LearnerLearningState(
                envelope.Curriculum,
                envelope.Tasks,
                envelope.Pronunciation,
                envelope.Review,
                envelope.Lessons);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveLearningStateAsync(
        Guid profileId,
        LearnerLearningState state,
        CancellationToken cancellationToken = default)
    {
        if (profileId == Guid.Empty)
        {
            throw new ArgumentException("The profile ID is required.", nameof(profileId));
        }

        ArgumentNullException.ThrowIfNull(state);
        CurriculumHistoryValidator.Validate(state.Curriculum);
        TaskHistoryValidator.Validate(state.Tasks);
        PronunciationHistoryValidator.Validate(state.Pronunciation);
        ReviewHistoryValidator.Validate(state.Review);
        LessonHistoryValidator.Validate(state.Lessons);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var envelope = await RequireProfileAsync(profileId, cancellationToken).ConfigureAwait(false);
            await WriteEnvelopeAsync(
                envelope with
                {
                    SchemaVersion = CurrentSchemaVersion,
                    Curriculum = state.Curriculum,
                    Tasks = state.Tasks,
                    Pronunciation = state.Pronunciation,
                    Review = state.Review,
                    Lessons = state.Lessons,
                },
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureRegularFileOrMissing(_filePath);
            EnsureRegularFileOrMissing(_temporaryFilePath);
            var recoveryFiles = FindRecoveryFiles();
            foreach (var recoveryFile in recoveryFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                File.Delete(recoveryFile);
            }

            File.Delete(_filePath);
            File.Delete(_temporaryFilePath);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new LearnerStoreException("The learner data could not be deleted.", exception);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LearnerStoreRecoveryResult> PreserveForRecoveryAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureRegularFileOrMissing(_filePath);
            EnsureRegularFileOrMissing(_temporaryFilePath);
            if (!File.Exists(_filePath) && !File.Exists(_temporaryFilePath))
            {
                throw new LearnerStoreException("There is no learner data file to preserve for recovery.");
            }

            Directory.CreateDirectory(_recoveryDirectory);
            EnsureDirectoryIsNotLink(_recoveryDirectory);
            var recoveryName =
                $"learner-data-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json";
            var recoveryPath = Path.Combine(_recoveryDirectory, recoveryName);
            var preservedFiles = 0;
            var preservedMainFile = false;
            if (File.Exists(_filePath))
            {
                File.Move(_filePath, recoveryPath);
                preservedFiles++;
                preservedMainFile = true;
            }

            if (File.Exists(_temporaryFilePath))
            {
                var temporaryRecoveryPath = preservedMainFile
                    ? recoveryPath + ".unfinished"
                    : recoveryPath;
                File.Move(_temporaryFilePath, temporaryRecoveryPath);
                preservedFiles++;
            }

            return new LearnerStoreRecoveryResult(recoveryName, preservedFiles);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (LearnerStoreException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new LearnerStoreException(
                "The unreadable learner data could not be preserved for recovery.",
                exception);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<LearnerStoreEnvelope> RequireProfileAsync(
        Guid profileId,
        CancellationToken cancellationToken)
    {
        var envelope = await ReadEnvelopeAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new LearnerStoreException("No learner data exists for curriculum persistence.");

        if (envelope.Profile.Id != profileId)
        {
            throw new LearnerStoreException(
                "The curriculum update does not belong to the active learner profile.");
        }

        return envelope;
    }

    private async Task<LearnerStoreEnvelope?> ReadEnvelopeAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            EnsureRegularFileOrMissing(_temporaryFilePath);
            if (File.Exists(_temporaryFilePath))
            {
                throw new LearnerStoreException(
                    "An unfinished learner data write was found. Preserve it for recovery before starting again.");
            }

            return null;
        }

        EnsureRegularFileOrMissing(_filePath);

        if (new FileInfo(_filePath).Length > MaximumStoreBytes)
        {
            throw new LearnerStoreException("The learner store is larger than the supported limit.");
        }

        try
        {
            await using var stream = new FileStream(
                _filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);
            using var document = await JsonDocument
                .ParseAsync(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("schemaVersion", out var schemaElement) ||
                !schemaElement.TryGetInt32(out var schemaVersion))
            {
                throw new LearnerStoreException("The learner store has no valid schema version.");
            }

            return schemaVersion switch
            {
                LegacyProfileSchemaVersion => MigrateSchemaOne(document.RootElement),
                CurriculumSchemaVersion => MigrateSchemaTwo(document.RootElement),
                TaskHistorySchemaVersion => MigrateSchemaThree(document.RootElement),
                PronunciationSchemaVersion => MigrateSchemaFour(document.RootElement),
                ReviewSchemaVersion => MigrateSchemaFive(document.RootElement),
                LessonHistorySchemaVersion => MigrateSchemaSix(document.RootElement),
                CurrentSchemaVersion => ReadCurrentSchema(document.RootElement),
                _ => throw new LearnerStoreException(
                    $"Learner store schema {schemaVersion} is unsupported; expected {CurrentSchemaVersion}."),
            };
        }
        catch (LearnerStoreException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new LearnerStoreException("The learner data could not be read.", exception);
        }
    }

    private static LearnerStoreEnvelope MigrateSchemaOne(JsonElement root)
    {
        var legacy = root.Deserialize<SchemaOneEnvelope>(JsonOptions)
            ?? throw new LearnerStoreException("The learner store is empty or invalid.");
        var profile = legacy.Profile
            ?? throw new LearnerStoreException("The learner store does not contain a profile.");
        LearnerProfileValidator.Validate(profile);
        return new LearnerStoreEnvelope(
            CurrentSchemaVersion,
            profile,
            CurriculumHistory.Empty,
            TaskHistory.Empty,
            PronunciationHistory.Empty,
            ReviewHistory.Empty,
            LessonHistory.Empty);
    }

    private static LearnerStoreEnvelope MigrateSchemaTwo(JsonElement root)
    {
        var envelope = root.Deserialize<SchemaTwoEnvelope>(JsonOptions)
            ?? throw new LearnerStoreException("The learner store is empty or invalid.");
        if (envelope.Profile is null)
        {
            throw new LearnerStoreException("The learner store does not contain a profile.");
        }

        if (envelope.Curriculum is null)
        {
            throw new LearnerStoreException("The learner store does not contain curriculum history.");
        }

        LearnerProfileValidator.Validate(envelope.Profile);
        CurriculumHistoryValidator.Validate(envelope.Curriculum);
        return new LearnerStoreEnvelope(
            CurrentSchemaVersion,
            envelope.Profile,
            envelope.Curriculum,
            TaskHistory.Empty,
            PronunciationHistory.Empty,
            ReviewHistory.Empty,
            LessonHistory.Empty);
    }

    private static LearnerStoreEnvelope MigrateSchemaThree(JsonElement root)
    {
        var envelope = root.Deserialize<SchemaThreeEnvelope>(JsonOptions)
            ?? throw new LearnerStoreException("The learner store is empty or invalid.");
        if (envelope.Profile is null || envelope.Curriculum is null || envelope.Tasks is null)
        {
            throw new LearnerStoreException("The learner store is missing profile, curriculum, or task history.");
        }

        LearnerProfileValidator.Validate(envelope.Profile);
        CurriculumHistoryValidator.Validate(envelope.Curriculum);
        TaskHistoryValidator.Validate(envelope.Tasks);
        return new LearnerStoreEnvelope(
            CurrentSchemaVersion,
            envelope.Profile,
            envelope.Curriculum,
            envelope.Tasks,
            PronunciationHistory.Empty,
            ReviewHistory.Empty,
            LessonHistory.Empty);
    }

    private static LearnerStoreEnvelope MigrateSchemaFour(JsonElement root)
    {
        var envelope = root.Deserialize<SchemaFourEnvelope>(JsonOptions)
            ?? throw new LearnerStoreException("The learner store is empty or invalid.");
        if (envelope.Profile is null ||
            envelope.Curriculum is null ||
            envelope.Tasks is null ||
            envelope.Pronunciation is null)
        {
            throw new LearnerStoreException(
                "The learner store is missing profile, curriculum, task, or pronunciation history.");
        }

        LearnerProfileValidator.Validate(envelope.Profile);
        CurriculumHistoryValidator.Validate(envelope.Curriculum);
        TaskHistoryValidator.Validate(envelope.Tasks);
        PronunciationHistoryValidator.Validate(envelope.Pronunciation);
        return new LearnerStoreEnvelope(
            CurrentSchemaVersion,
            envelope.Profile,
            envelope.Curriculum,
            envelope.Tasks,
            envelope.Pronunciation,
            ReviewHistory.Empty,
            LessonHistory.Empty);
    }

    private static LearnerStoreEnvelope MigrateSchemaFive(JsonElement root)
    {
        var envelope = root.Deserialize<SchemaFiveEnvelope>(JsonOptions)
            ?? throw new LearnerStoreException("The learner store is empty or invalid.");
        if (envelope.Profile is null ||
            envelope.Curriculum is null ||
            envelope.Tasks is null ||
            envelope.Pronunciation is null ||
            envelope.Review is null)
        {
            throw new LearnerStoreException(
                "The learner store is missing profile, curriculum, task, pronunciation, or review history.");
        }

        LearnerProfileValidator.Validate(envelope.Profile);
        CurriculumHistoryValidator.Validate(envelope.Curriculum);
        TaskHistoryValidator.Validate(envelope.Tasks);
        PronunciationHistoryValidator.Validate(envelope.Pronunciation);
        ReviewHistoryValidator.Validate(envelope.Review);
        return new LearnerStoreEnvelope(
            CurrentSchemaVersion,
            envelope.Profile,
            envelope.Curriculum,
            envelope.Tasks,
            envelope.Pronunciation,
            envelope.Review,
            LessonHistory.Empty);
    }

    private static LearnerStoreEnvelope MigrateSchemaSix(JsonElement root)
    {
        var envelope = ReadAndValidateCurrentEnvelope(root);
        return envelope with { SchemaVersion = CurrentSchemaVersion };
    }

    private static LearnerStoreEnvelope ReadCurrentSchema(JsonElement root) =>
        ReadAndValidateCurrentEnvelope(root);

    private static LearnerStoreEnvelope ReadAndValidateCurrentEnvelope(JsonElement root)
    {
        var envelope = root.Deserialize<LearnerStoreEnvelope>(JsonOptions)
            ?? throw new LearnerStoreException("The learner store is empty or invalid.");
        if (envelope.Profile is null ||
            envelope.Curriculum is null ||
            envelope.Tasks is null ||
            envelope.Pronunciation is null ||
            envelope.Review is null ||
            envelope.Lessons is null)
        {
            throw new LearnerStoreException(
                "The learner store is missing profile, curriculum, task, pronunciation, review, or lesson history.");
        }

        LearnerProfileValidator.Validate(envelope.Profile);
        CurriculumHistoryValidator.Validate(envelope.Curriculum);
        TaskHistoryValidator.Validate(envelope.Tasks);
        PronunciationHistoryValidator.Validate(envelope.Pronunciation);
        ReviewHistoryValidator.Validate(envelope.Review);
        LessonHistoryValidator.Validate(envelope.Lessons);
        return envelope;
    }

    private async Task WriteEnvelopeAsync(
        LearnerStoreEnvelope envelope,
        CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(_directory);
            EnsureRegularFileOrMissing(_filePath);
            EnsureRegularFileOrMissing(_temporaryFilePath);
            await using (var stream = new FileStream(
                _temporaryFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                options: FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer
                    .SerializeAsync(stream, envelope, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(_temporaryFilePath, _filePath, overwrite: true);
        }
        catch (OperationCanceledException)
        {
            TryDeleteTemporaryFile();
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            TryDeleteTemporaryFile();
            throw new LearnerStoreException("The learner data could not be saved.", exception);
        }
    }

    private void TryDeleteTemporaryFile()
    {
        try
        {
            File.Delete(_temporaryFilePath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void EnsureRegularFileOrMissing(string path)
    {
        var info = new FileInfo(path);
        if (info.LinkTarget is not null ||
            info.Exists && (info.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new LearnerStoreException(
                "The learner data path is a filesystem link and was not accessed.");
        }

        if (Directory.Exists(path))
        {
            throw new LearnerStoreException(
                "The learner data path is a directory and was not accessed.");
        }
    }

    private IReadOnlyList<string> FindRecoveryFiles()
    {
        var directory = new DirectoryInfo(_recoveryDirectory);
        if (directory.LinkTarget is not null ||
            directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new LearnerStoreException(
                "The learner data recovery directory is a filesystem link and was not accessed.");
        }

        if (!directory.Exists)
        {
            return [];
        }

        var files = new List<string>();
        foreach (var path in Directory.EnumerateFileSystemEntries(
                     _recoveryDirectory,
                     "*",
                     SearchOption.TopDirectoryOnly))
        {
            if (!IsRecoveryFileName(Path.GetFileName(path)))
            {
                continue;
            }

            EnsureRegularFileOrMissing(path);
            files.Add(path);
        }

        return files;
    }

    private static bool IsRecoveryFileName(string fileName)
    {
        const string prefix = "learner-data-";
        const string suffix = ".json";
        const string unfinishedSuffix = ".unfinished";

        var completeName = fileName.EndsWith(unfinishedSuffix, StringComparison.Ordinal)
            ? fileName[..^unfinishedSuffix.Length]
            : fileName;
        if (!completeName.StartsWith(prefix, StringComparison.Ordinal) ||
            !completeName.EndsWith(suffix, StringComparison.Ordinal))
        {
            return false;
        }

        var identifier = completeName[prefix.Length..^suffix.Length];
        var parts = identifier.Split('-');
        return parts.Length == 3 &&
               parts[0].Length == 8 &&
               parts[0].All(char.IsAsciiDigit) &&
               parts[1].Length == 6 &&
               parts[1].All(char.IsAsciiDigit) &&
               Guid.TryParseExact(parts[2], "N", out _);
    }

    private static void EnsureDirectoryIsNotLink(string path)
    {
        var info = new DirectoryInfo(path);
        if (info.LinkTarget is not null ||
            (info.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new LearnerStoreException(
                "The learner data recovery directory is a filesystem link and was not used.");
        }
    }

    private sealed record SchemaOneEnvelope(int SchemaVersion, LearnerProfile? Profile);

    private sealed record SchemaTwoEnvelope(
        int SchemaVersion,
        LearnerProfile Profile,
        CurriculumHistory Curriculum);

    private sealed record SchemaThreeEnvelope(
        int SchemaVersion,
        LearnerProfile Profile,
        CurriculumHistory Curriculum,
        TaskHistory Tasks);

    private sealed record SchemaFourEnvelope(
        int SchemaVersion,
        LearnerProfile Profile,
        CurriculumHistory Curriculum,
        TaskHistory Tasks,
        PronunciationHistory Pronunciation);

    private sealed record SchemaFiveEnvelope(
        int SchemaVersion,
        LearnerProfile Profile,
        CurriculumHistory Curriculum,
        TaskHistory Tasks,
        PronunciationHistory Pronunciation,
        ReviewHistory Review);

    private sealed record LearnerStoreEnvelope(
        int SchemaVersion,
        LearnerProfile Profile,
        CurriculumHistory Curriculum,
        TaskHistory Tasks,
        PronunciationHistory Pronunciation,
        ReviewHistory Review,
        LessonHistory Lessons);
}

public sealed record LearnerStoreRecoveryResult(
    string RecoveryFileName,
    int PreservedFileCount);
