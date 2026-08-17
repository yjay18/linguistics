using System.Text.Json;
using System.Text.Json.Serialization;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Persistence;

public sealed class JsonLearnerRepository : ILearnerRepository
{
    public const int CurrentSchemaVersion = 1;
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
            if (!File.Exists(_filePath))
            {
                return null;
            }

            if (new FileInfo(_filePath).Length > MaximumStoreBytes)
            {
                throw new LearnerStoreException("The learner store is larger than the supported limit.");
            }

            await using var stream = new FileStream(
                _filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            var envelope = await JsonSerializer
                .DeserializeAsync<LearnerStoreEnvelope>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (envelope is null)
            {
                throw new LearnerStoreException("The learner store is empty or invalid.");
            }

            if (envelope.SchemaVersion != CurrentSchemaVersion)
            {
                throw new LearnerStoreException(
                    $"Learner store schema {envelope.SchemaVersion} is unsupported; expected {CurrentSchemaVersion}.");
            }

            return envelope.Profile
                ?? throw new LearnerStoreException("The learner store does not contain a profile.");
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
            throw new LearnerStoreException("The learner profile could not be read.", exception);
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

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(_directory);
            var envelope = new LearnerStoreEnvelope(CurrentSchemaVersion, profile);

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
            throw new LearnerStoreException("The learner profile could not be saved.", exception);
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
            throw new LearnerStoreException("The learner profile could not be deleted.", exception);
        }
        finally
        {
            _gate.Release();
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

    private sealed record LearnerStoreEnvelope(int SchemaVersion, LearnerProfile? Profile);
}
