namespace Linguistics.App.Speech;

public sealed record SpeechRecordingSnapshot(int FileCount, long TotalBytes);

public sealed record SpeechRecordingDeletionResult(
    int DeletedFileCount,
    int FailedFileCount,
    string Message);

public sealed class SpeechRecordingStore
{
    private static readonly EnumerationOptions OwnedFileEnumeration = new()
    {
        RecurseSubdirectories = true,
        AttributesToSkip = FileAttributes.ReparsePoint,
        IgnoreInaccessible = false,
        ReturnSpecialDirectories = false,
    };

    private readonly string _rootDirectory;

    public SpeechRecordingStore(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        _rootDirectory = Path.GetFullPath(rootDirectory);
    }

    public Task<SpeechRecordingSnapshot> InspectAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Directory.Exists(_rootDirectory))
        {
            return Task.FromResult(new SpeechRecordingSnapshot(0, 0));
        }

        if (IsReparsePoint(_rootDirectory))
        {
            throw new IOException("The speech recording directory is an unsupported link.");
        }

        var files = Directory
            .EnumerateFiles(_rootDirectory, "*.wav", OwnedFileEnumeration)
            .Select(path => new FileInfo(path))
            .ToArray();
        return Task.FromResult(new SpeechRecordingSnapshot(
            files.Length,
            files.Sum(file => file.Length)));
    }

    public Task<SpeechRecordingDeletionResult> DeleteAllAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Directory.Exists(_rootDirectory))
        {
            return Task.FromResult(new SpeechRecordingDeletionResult(
                0,
                0,
                "No app-owned legacy audio files were found."));
        }

        if (IsReparsePoint(_rootDirectory))
        {
            return Task.FromResult(new SpeechRecordingDeletionResult(
                0,
                1,
                "The speech recording directory is an unsupported link and was not changed."));
        }

        var deleted = 0;
        var failed = 0;
        foreach (var file in Directory.EnumerateFiles(
                     _rootDirectory,
                     "*.wav",
                     OwnedFileEnumeration))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                File.Delete(file);
                deleted++;
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException)
            {
                failed++;
            }
        }

        foreach (var directory in Directory
                     .EnumerateDirectories(_rootDirectory, "*", OwnedFileEnumeration)
                     .OrderByDescending(path => path.Length))
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException)
            {
            }
        }

        return Task.FromResult(new SpeechRecordingDeletionResult(
            deleted,
            failed,
            failed == 0
                ? deleted == 0
                    ? "No app-owned legacy audio files were found."
                    : $"Deleted {deleted} app-owned legacy audio file(s)."
                : $"Deleted {deleted} app-owned legacy audio file(s); {failed} could not be removed."));
    }

    private static bool IsReparsePoint(string path) =>
        File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
}
