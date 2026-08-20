using System.Text.Json;
using System.Text.Json.Serialization;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Persistence;

public sealed class JsonLearnerRepository : ILearnerRepository
{
    public const int CurrentSchemaVersion = 2;
    private const int PreviousSchemaVersion = 1;
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
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonLearnerRepository(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _filePath = Path.GetFullPath(filePath);
        _directory = Path.GetDirectoryName(_filePath)
            ?? throw new ArgumentException("The learner store must have a parent directory.", nameof(filePath));
        _temporaryFilePath = _filePath + ".tmp";
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
                    existing?.Curriculum ?? CurriculumHistory.Empty),
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

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
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
            return null;
        }

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
                PreviousSchemaVersion => MigrateSchemaOne(document.RootElement),
                CurrentSchemaVersion => ReadSchemaTwo(document.RootElement),
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
        return new LearnerStoreEnvelope(CurrentSchemaVersion, profile, CurriculumHistory.Empty);
    }

    private static LearnerStoreEnvelope ReadSchemaTwo(JsonElement root)
    {
        var envelope = root.Deserialize<LearnerStoreEnvelope>(JsonOptions)
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
        return envelope;
    }

    private async Task WriteEnvelopeAsync(
        LearnerStoreEnvelope envelope,
        CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(_directory);
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

    private sealed record SchemaOneEnvelope(int SchemaVersion, LearnerProfile? Profile);

    private sealed record LearnerStoreEnvelope(
        int SchemaVersion,
        LearnerProfile Profile,
        CurriculumHistory Curriculum);
}
