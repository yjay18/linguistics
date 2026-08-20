using System.Text.Json;
using System.Text.Json.Serialization;

namespace Linguistics.App.Diagnostics;

public enum DiagnosticCategory
{
    Application,
    Curriculum,
    Routing,
    Task,
    Ollama,
    Speech,
    Assessment,
    Persistence,
    Review,
}

public enum DiagnosticEventCode
{
    AppOpened,
    ProfileLoaded,
    ProfileLoadFailed,
    RecoveryPreserved,
    ReviewSynchronized,
    ReviewRecorded,
    LearningDataDeleted,
}

public enum DiagnosticOutcome
{
    Started,
    Succeeded,
    Failed,
    Unavailable,
    Cancelled,
}

public sealed record LocalDiagnosticEntry(
    DateTimeOffset Timestamp,
    DiagnosticCategory Category,
    DiagnosticEventCode EventCode,
    DiagnosticOutcome Outcome,
    Guid? RequestId,
    long? DurationMilliseconds,
    string? ConfigurationVersion);

public sealed record DiagnosticLogSnapshot(
    long SizeBytes,
    int EntryCount);

public sealed class LocalDiagnosticLog
{
    private const long DefaultMaximumBytes = 262_144;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string _filePath;
    private readonly string _directory;
    private readonly Func<DateTimeOffset> _clock;
    private readonly long _maximumBytes;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public LocalDiagnosticLog(
        string filePath,
        Func<DateTimeOffset>? clock = null,
        long maximumBytes = DefaultMaximumBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (maximumBytes < 256)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumBytes),
                "The diagnostic log limit must be at least 256 bytes.");
        }

        _filePath = Path.GetFullPath(filePath);
        _directory = Path.GetDirectoryName(_filePath)
            ?? throw new ArgumentException("The diagnostic log needs a parent directory.", nameof(filePath));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _maximumBytes = maximumBytes;
    }

    public async Task WriteAsync(
        DiagnosticCategory category,
        DiagnosticEventCode eventCode,
        DiagnosticOutcome outcome,
        Guid? requestId = null,
        TimeSpan? duration = null,
        string? configurationVersion = null,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(category) || !Enum.IsDefined(eventCode) || !Enum.IsDefined(outcome) ||
            requestId == Guid.Empty ||
            duration is { } measured && (measured < TimeSpan.Zero || measured > TimeSpan.FromDays(1)) ||
            !IsSafeVersion(configurationVersion))
        {
            throw new ArgumentException("The diagnostic event is invalid or contains an unsafe value.");
        }

        var entry = new LocalDiagnosticEntry(
            _clock(),
            category,
            eventCode,
            outcome,
            requestId,
            duration is null ? null : checked((long)duration.Value.TotalMilliseconds),
            configurationVersion);
        var line = JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(_directory);
            EnsureRegularFileOrMissing();
            if (File.Exists(_filePath) && new FileInfo(_filePath).Length + line.Length > _maximumBytes)
            {
                await File.WriteAllTextAsync(_filePath, string.Empty, cancellationToken).ConfigureAwait(false);
            }

            await File.AppendAllTextAsync(_filePath, line, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DiagnosticLogException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new DiagnosticLogException("The local diagnostic event could not be written.", exception);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DiagnosticLogSnapshot> InspectAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureRegularFileOrMissing();
            if (!File.Exists(_filePath))
            {
                return new DiagnosticLogSnapshot(0, 0);
            }

            var entries = 0;
            using var reader = File.OpenText(_filePath);
            while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is not null)
            {
                entries++;
            }

            return new DiagnosticLogSnapshot(new FileInfo(_filePath).Length, entries);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DiagnosticLogException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new DiagnosticLogException("The local diagnostic log could not be inspected.", exception);
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
            EnsureRegularFileOrMissing();
            File.Delete(_filePath);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DiagnosticLogException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new DiagnosticLogException("The local diagnostic log could not be deleted.", exception);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static bool IsSafeVersion(string? value) =>
        value is null ||
        value.Length is >= 2 and <= 128 &&
        value.All(character =>
            character is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '-' or '_');

    private void EnsureRegularFileOrMissing()
    {
        var info = new FileInfo(_filePath);
        if (info.LinkTarget is not null ||
            info.Exists && (info.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new DiagnosticLogException(
                "The diagnostic-log path is a filesystem link and was not accessed.");
        }
    }
}

public sealed class DiagnosticLogException : Exception
{
    public DiagnosticLogException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
