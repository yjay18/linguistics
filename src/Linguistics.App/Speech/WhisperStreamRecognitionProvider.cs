using System.Diagnostics;
using System.Text.RegularExpressions;
using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.App.Speech;

public sealed partial class WhisperStreamRecognitionProvider : ISpeechRecognitionProvider
{
    public const string ProviderVersion = "whisper.cpp-stream-v1";
    public const string ModelSource = "https://huggingface.co/ggerganov/whisper.cpp";
    public const string License = "MIT model-conversion repository; verify the selected model's source terms";

    private readonly string? _executable;
    private readonly string? _modelPath;
    private readonly IChildProcessLauncher _launcher;
    private readonly object _processGate = new();
    private IChildProcess? _activeProcess;
    private bool _disposed;

    private WhisperStreamRecognitionProvider(
        string? executable,
        string? modelPath,
        IChildProcessLauncher launcher)
    {
        _executable = executable;
        _modelPath = modelPath;
        _launcher = launcher;
    }

    public static WhisperStreamRecognitionProvider CreateDefault() => new(
        ResolveExecutable(Environment.GetEnvironmentVariable("LINGUISTICS_WHISPER_STREAM")),
        ResolveModel(Environment.GetEnvironmentVariable("LINGUISTICS_WHISPER_MODEL")),
        new ChildProcessLauncher());

    internal static WhisperStreamRecognitionProvider CreateForTests(
        string? executable,
        string? modelPath,
        IChildProcessLauncher launcher) =>
        new(executable, modelPath, launcher);

    public Task<SpeechRecognitionSnapshot> InspectAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(_executable) || !File.Exists(_executable))
        {
            return Task.FromResult(new SpeechRecognitionSnapshot(
                SpeechCapabilityStatus.Unavailable,
                null,
                "Local microphone transcription needs an installed whisper-stream executable. Text practice remains available."));
        }

        if (string.IsNullOrWhiteSpace(_modelPath) || !File.Exists(_modelPath))
        {
            return Task.FromResult(new SpeechRecognitionSnapshot(
                SpeechCapabilityStatus.Misconfigured,
                null,
                "whisper-stream is installed, but no speech model is configured. Linguistics never downloads one silently; set LINGUISTICS_WHISPER_MODEL to a model you reviewed."));
        }

        var info = new FileInfo(_modelPath);
        if (info.Length <= 0)
        {
            return Task.FromResult(new SpeechRecognitionSnapshot(
                SpeechCapabilityStatus.Misconfigured,
                null,
                "The configured local speech model is empty. Text practice remains available."));
        }

        var model = new SpeechModelDescriptor(
            info.Name,
            info.Length,
            ModelSource,
            License,
            ProviderVersion);
        return Task.FromResult(new SpeechRecognitionSnapshot(
            SpeechCapabilityStatus.Available,
            model,
            $"Local microphone transcription is configured with {info.Name} ({FormatBytes(info.Length)}). Audio is not sent over the network."));
    }

    public async Task<SpeechRecognitionResult> RecognizeAsync(
        SpeechRecognitionRequest request,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var invalid = Validate(request);
        if (invalid is not null)
        {
            return Result(request, SpeechRecognitionResultStatus.InvalidRequest, null, TimeSpan.Zero, null, invalid);
        }

        var snapshot = await InspectAsync(cancellationToken).ConfigureAwait(false);
        if (snapshot.Status != SpeechCapabilityStatus.Available ||
            snapshot.Model is null ||
            _executable is null ||
            _modelPath is null)
        {
            return Result(
                request,
                SpeechRecognitionResultStatus.Unavailable,
                null,
                TimeSpan.Zero,
                snapshot.Model?.Name,
                snapshot.Message);
        }

        var stopwatch = Stopwatch.StartNew();
        IChildProcess? process = null;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(request.MaximumDuration);
        try
        {
            var startInfo = CreateStartInfo(_executable, _modelPath, request);
            var modelName = snapshot.Model.Name;
            process = _launcher.Start(startInfo);
            lock (_processGate)
            {
                _activeProcess = process;
            }
            process.StandardInput.Close();
            var errorTask = process.StandardError.ReadToEndAsync(CancellationToken.None);
            using var cancellationRegistration = timeout.Token.Register(Kill, process);
            var transcript = await ReadFirstTranscriptionAsync(
                process.StandardOutput,
                timeout.Token).ConfigureAwait(false);
            Kill(process);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            var error = await errorTask.ConfigureAwait(false);
            stopwatch.Stop();

            if (cancellationToken.IsCancellationRequested)
            {
                return Result(
                    request,
                    SpeechRecognitionResultStatus.Cancelled,
                    null,
                    stopwatch.Elapsed,
                    modelName,
                    "Microphone capture was cancelled. No task state changed.");
            }

            if (timeout.IsCancellationRequested)
            {
                return Result(
                    request,
                    SpeechRecognitionResultStatus.NoSpeech,
                    null,
                    stopwatch.Elapsed,
                    modelName,
                    "No complete speech segment was recognized before the local recording limit.");
            }

            if (!string.IsNullOrWhiteSpace(transcript))
            {
                return Result(
                    request,
                    SpeechRecognitionResultStatus.Accepted,
                    transcript,
                    stopwatch.Elapsed,
                    modelName,
                    "A local transcript is ready. Review it before using it; recognition can be wrong.");
            }

            return FailureFrom(error, request, stopwatch.Elapsed, modelName);
        }
        catch (OperationCanceledException)
        {
            Kill(process);
            stopwatch.Stop();
            return Result(
                request,
                cancellationToken.IsCancellationRequested
                    ? SpeechRecognitionResultStatus.Cancelled
                    : SpeechRecognitionResultStatus.NoSpeech,
                null,
                stopwatch.Elapsed,
                snapshot.Model?.Name,
                cancellationToken.IsCancellationRequested
                    ? "Microphone capture was cancelled. No task state changed."
                    : "No complete speech segment was recognized before the local recording limit.");
        }
        catch (Exception exception) when (
            exception is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            Kill(process);
            stopwatch.Stop();
            return Result(
                request,
                SpeechRecognitionResultStatus.Failed,
                null,
                stopwatch.Elapsed,
                snapshot.Model?.Name,
                $"Local speech recognition failed to run: {exception.Message}");
        }
        finally
        {
            lock (_processGate)
            {
                if (ReferenceEquals(_activeProcess, process))
                {
                    _activeProcess = null;
                }
            }

            process?.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        IChildProcess? process;
        lock (_processGate)
        {
            process = _activeProcess;
            _activeProcess = null;
        }

        Kill(process);
    }

    internal static ProcessStartInfo CreateStartInfo(
        string executable,
        string modelPath,
        SpeechRecognitionRequest request)
    {
        var startInfo = ChildProcessStartInfo.Create(executable);
        startInfo.WorkingDirectory = AppContext.BaseDirectory;
        startInfo.ArgumentList.Add("--step");
        startInfo.ArgumentList.Add("0");
        startInfo.ArgumentList.Add("--length");
        startInfo.ArgumentList.Add(Math.Clamp((int)request.MaximumDuration.TotalMilliseconds, 3_000, 30_000).ToString());
        startInfo.ArgumentList.Add("--language");
        startInfo.ArgumentList.Add(request.Language.Value);
        startInfo.ArgumentList.Add("--model");
        startInfo.ArgumentList.Add(modelPath);
        startInfo.ArgumentList.Add("--no-fallback");
        return startInfo;
    }

    internal static string? ParseFirstTranscription(string output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var inTranscription = false;
        var segments = new List<string>();
        foreach (var rawLine in output.Split('\n'))
        {
            var line = StripAnsi(rawLine.TrimEnd('\r'));
            if (line.StartsWith("### Transcription ", StringComparison.Ordinal) &&
                line.Contains(" START", StringComparison.Ordinal))
            {
                inTranscription = true;
                continue;
            }

            if (inTranscription &&
                line.StartsWith("### Transcription ", StringComparison.Ordinal) &&
                line.EndsWith(" END", StringComparison.Ordinal))
            {
                break;
            }

            if (inTranscription)
            {
                var segment = TimestampPrefix().Replace(line, string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(segment))
                {
                    segments.Add(segment);
                }
            }
        }

        return NormalizeTranscript(segments);
    }

    private static async Task<string?> ReadFirstTranscriptionAsync(
        TextReader reader,
        CancellationToken cancellationToken)
    {
        var inTranscription = false;
        var segments = new List<string>();
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } rawLine)
        {
            var line = StripAnsi(rawLine);
            if (line.StartsWith("### Transcription ", StringComparison.Ordinal) &&
                line.Contains(" START", StringComparison.Ordinal))
            {
                inTranscription = true;
                continue;
            }

            if (inTranscription &&
                line.StartsWith("### Transcription ", StringComparison.Ordinal) &&
                line.EndsWith(" END", StringComparison.Ordinal))
            {
                return NormalizeTranscript(segments);
            }

            if (inTranscription)
            {
                var segment = TimestampPrefix().Replace(line, string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(segment))
                {
                    segments.Add(segment);
                }
            }
        }

        return NormalizeTranscript(segments);
    }

    private static string? NormalizeTranscript(IReadOnlyList<string> segments)
    {
        var transcript = string.Join(' ', segments)
            .Replace("[SPEAKER_TURN]", string.Empty, StringComparison.Ordinal)
            .Trim();
        transcript = string.Join(' ', transcript.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (transcript.Length == 0 ||
            transcript.Equals("[BLANK_AUDIO]", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return transcript.Length <= 500 ? transcript : transcript[..500];
    }

    private static SpeechRecognitionResult FailureFrom(
        string error,
        SpeechRecognitionRequest request,
        TimeSpan duration,
        string modelName)
    {
        if (error.Contains("permission", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("not authorized", StringComparison.OrdinalIgnoreCase))
        {
            return Result(
                request,
                SpeechRecognitionResultStatus.PermissionDenied,
                null,
                duration,
                modelName,
                "Microphone access was denied. Enable it in system privacy settings or continue with text.");
        }

        if (error.Contains("audio.init() failed", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("capture device", StringComparison.OrdinalIgnoreCase))
        {
            return Result(
                request,
                SpeechRecognitionResultStatus.MicrophoneUnavailable,
                null,
                duration,
                modelName,
                "No usable microphone capture device was available. Text practice remains complete.");
        }

        return Result(
            request,
            SpeechRecognitionResultStatus.Failed,
            null,
            duration,
            modelName,
            "The local recognizer stopped before producing a complete transcript.");
    }

    private static SpeechRecognitionResult Result(
        SpeechRecognitionRequest request,
        SpeechRecognitionResultStatus status,
        string? transcript,
        TimeSpan duration,
        string? modelName,
        string message) =>
        new(
            request.RequestId,
            status,
            transcript,
            request.Language,
            duration,
            ProviderVersion,
            modelName,
            message);

    private static string? Validate(SpeechRecognitionRequest request)
    {
        if (request.RequestId == Guid.Empty ||
            request.MaximumDuration < TimeSpan.FromSeconds(3) ||
            request.MaximumDuration > TimeSpan.FromSeconds(30))
        {
            return "The local speech-recognition request is invalid.";
        }

        if (request.RetainAudio)
        {
            return "This whisper-stream adapter does not retain recordings. Turn retention off or continue with text.";
        }

        return null;
    }

    private static string? ResolveExecutable(string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        var name = OperatingSystem.IsWindows() ? "whisper-stream.exe" : "whisper-stream";
        var path = Environment.GetEnvironmentVariable("PATH")
            ?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(directory => Path.Combine(directory, name))
            .FirstOrDefault(File.Exists);
        if (path is not null)
        {
            return path;
        }

        var common = OperatingSystem.IsMacOS()
            ? new[] { "/opt/homebrew/bin/whisper-stream", "/usr/local/bin/whisper-stream" }
            : [];
        return common.FirstOrDefault(File.Exists);
    }

    private static string? ResolveModel(string? configured) =>
        string.IsNullOrWhiteSpace(configured) ? null : Path.GetFullPath(configured);

    private static string FormatBytes(long bytes) =>
        bytes >= 1_073_741_824
            ? $"{bytes / 1_073_741_824d:0.0} GiB"
            : $"{bytes / 1_048_576d:0} MiB";

    private static string StripAnsi(string value) => AnsiEscape().Replace(value, string.Empty);

    private static void Kill(object? state) => Kill(state as IChildProcess);

    private static void Kill(IChildProcess? process)
    {
        try
        {
            process?.Kill();
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
        }
    }

    [GeneratedRegex("^\\[[0-9:.]+ --> [0-9:.]+\\]\\s*", RegexOptions.CultureInvariant)]
    private static partial Regex TimestampPrefix();

    [GeneratedRegex("\\x1B\\[[0-?]*[ -/]*[@-~]", RegexOptions.CultureInvariant)]
    private static partial Regex AnsiEscape();
}
