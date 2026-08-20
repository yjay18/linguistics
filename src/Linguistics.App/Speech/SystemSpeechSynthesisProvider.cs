using System.Diagnostics;
using System.Text.RegularExpressions;
using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.App.Speech;

internal enum SystemSpeechPlatform
{
    Unsupported,
    MacOS,
    Windows,
}

public sealed partial class SystemSpeechSynthesisProvider : ISpeechSynthesisProvider
{
    private const string WindowsVoiceListScript =
        "Add-Type -AssemblyName System.Speech; " +
        "$s=[System.Speech.Synthesis.SpeechSynthesizer]::new(); " +
        "foreach($v in $s.GetInstalledVoices()){if($v.Enabled){" +
        "[Console]::Out.WriteLine($v.VoiceInfo.Name+[char]31+$v.VoiceInfo.Culture.Name)}}; $s.Dispose()";

    private const string WindowsSpeakScript =
        "Add-Type -AssemblyName System.Speech; " +
        "$s=[System.Speech.Synthesis.SpeechSynthesizer]::new(); " +
        "$s.Rate=[int]$args[0]; if($args[1]){$s.SelectVoice($args[1])}; " +
        "$text=[Console]::In.ReadToEnd(); $s.Speak($text); $s.Dispose()";

    private readonly SystemSpeechPlatform _platform;
    private readonly string? _executable;
    private readonly IChildProcessLauncher _launcher;
    private readonly object _processGate = new();
    private IChildProcess? _activeProcess;
    private CancellationTokenSource? _activeStopCancellation;
    private SpeechSynthesisSnapshot? _cachedSnapshot;
    private bool _disposed;

    private SystemSpeechSynthesisProvider(
        SystemSpeechPlatform platform,
        string? executable,
        IChildProcessLauncher launcher)
    {
        _platform = platform;
        _executable = executable;
        _launcher = launcher;
    }

    public static SystemSpeechSynthesisProvider CreateDefault()
    {
        if (OperatingSystem.IsMacOS())
        {
            return new SystemSpeechSynthesisProvider(
                SystemSpeechPlatform.MacOS,
                "/usr/bin/say",
                new ChildProcessLauncher());
        }

        if (OperatingSystem.IsWindows())
        {
            var executable = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "System32",
                "WindowsPowerShell",
                "v1.0",
                "powershell.exe");
            return new SystemSpeechSynthesisProvider(
                SystemSpeechPlatform.Windows,
                executable,
                new ChildProcessLauncher());
        }

        return new SystemSpeechSynthesisProvider(
            SystemSpeechPlatform.Unsupported,
            null,
            new ChildProcessLauncher());
    }

    internal static SystemSpeechSynthesisProvider CreateForTests(
        SystemSpeechPlatform platform,
        string? executable,
        IChildProcessLauncher launcher) =>
        new(platform, executable, launcher);

    public async Task<SpeechSynthesisSnapshot> InspectAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_cachedSnapshot is not null)
        {
            return _cachedSnapshot;
        }

        if (_platform == SystemSpeechPlatform.Unsupported ||
            string.IsNullOrWhiteSpace(_executable) ||
            !File.Exists(_executable))
        {
            return _cachedSnapshot = new SpeechSynthesisSnapshot(
                SpeechCapabilityStatus.Unavailable,
                [],
                "System speech playback is not available on this platform.");
        }

        try
        {
            var startInfo = ChildProcessStartInfo.Create(_executable);
            if (_platform == SystemSpeechPlatform.MacOS)
            {
                startInfo.ArgumentList.Add("-v");
                startInfo.ArgumentList.Add("?");
            }
            else
            {
                AddPowerShellCommand(startInfo, WindowsVoiceListScript);
            }

            using var process = _launcher.Start(startInfo);
            using var registration = cancellationToken.Register(Kill, process);
            process.StandardInput.Close();
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var output = await outputTask.ConfigureAwait(false);
            var error = await errorTask.ConfigureAwait(false);
            var voices = _platform == SystemSpeechPlatform.MacOS
                ? ParseMacVoices(output)
                : ParseWindowsVoices(output);
            if (process.ExitCode != 0)
            {
                return _cachedSnapshot = new SpeechSynthesisSnapshot(
                    SpeechCapabilityStatus.Unavailable,
                    [],
                    SafeFailure("System voice discovery failed", error));
            }

            return _cachedSnapshot = new SpeechSynthesisSnapshot(
                voices.Count > 0 ? SpeechCapabilityStatus.Available : SpeechCapabilityStatus.Unavailable,
                voices,
                voices.Count > 0
                    ? $"Found {voices.Count} installed system voice(s); playback stays on this device."
                    : "No enabled system speech voices were found.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            return _cachedSnapshot = new SpeechSynthesisSnapshot(
                SpeechCapabilityStatus.Unavailable,
                [],
                $"System voice discovery failed: {exception.Message}");
        }
    }

    public async Task<SpeechSynthesisResult> SpeakAsync(
        SpeechSynthesisRequest request,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var validation = Validate(request);
        if (validation is not null)
        {
            return new SpeechSynthesisResult(
                request.RequestId,
                SpeechSynthesisResultStatus.InvalidRequest,
                null,
                TimeSpan.Zero,
                validation);
        }

        var snapshot = await InspectAsync(cancellationToken).ConfigureAwait(false);
        if (snapshot.Status != SpeechCapabilityStatus.Available || _executable is null)
        {
            return new SpeechSynthesisResult(
                request.RequestId,
                SpeechSynthesisResultStatus.Unavailable,
                null,
                TimeSpan.Zero,
                snapshot.Message);
        }

        var voice = request.VoiceId is { } requestedId
            ? snapshot.Voices.SingleOrDefault(candidate =>
                candidate.Id == requestedId && candidate.Language == request.Language)
            : SpeechVoiceSelector.Select(snapshot.Voices, request.Language, request.Seed);
        if (voice is null)
        {
            return new SpeechSynthesisResult(
                request.RequestId,
                SpeechSynthesisResultStatus.Unavailable,
                null,
                TimeSpan.Zero,
                $"No installed {request.Language} system voice is available. Captions remain available.");
        }

        await StopAsync().ConfigureAwait(false);
        var stopwatch = Stopwatch.StartNew();
        IChildProcess? process = null;
        using var stopCancellation = new CancellationTokenSource();
        try
        {
            var startInfo = CreateSpeakStartInfo(_executable, request, voice);
            process = _launcher.Start(startInfo);
            lock (_processGate)
            {
                _activeProcess = process;
                _activeStopCancellation = stopCancellation;
            }

            await process.StandardInput.WriteAsync(request.Text.AsMemory(), cancellationToken)
                .ConfigureAwait(false);
            process.StandardInput.Close();
            var errorTask = process.StandardError.ReadToEndAsync(CancellationToken.None);
            using var callerRegistration = cancellationToken.Register(Kill, process);
            using var stopRegistration = stopCancellation.Token.Register(Kill, process);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            var error = await errorTask.ConfigureAwait(false);
            stopwatch.Stop();
            if (cancellationToken.IsCancellationRequested || stopCancellation.IsCancellationRequested)
            {
                return new SpeechSynthesisResult(
                    request.RequestId,
                    SpeechSynthesisResultStatus.Cancelled,
                    voice.Id,
                    stopwatch.Elapsed,
                    "Speech playback was stopped.");
            }

            return process.ExitCode == 0
                ? new SpeechSynthesisResult(
                    request.RequestId,
                    SpeechSynthesisResultStatus.Completed,
                    voice.Id,
                    stopwatch.Elapsed,
                    "Speech playback completed locally.")
                : new SpeechSynthesisResult(
                    request.RequestId,
                    SpeechSynthesisResultStatus.Failed,
                    voice.Id,
                    stopwatch.Elapsed,
                    SafeFailure("System speech playback failed", error));
        }
        catch (OperationCanceledException)
        {
            Kill(process);
            return new SpeechSynthesisResult(
                request.RequestId,
                SpeechSynthesisResultStatus.Cancelled,
                voice.Id,
                stopwatch.Elapsed,
                "Speech playback was stopped.");
        }
        catch (Exception exception) when (
            exception is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            Kill(process);
            return new SpeechSynthesisResult(
                request.RequestId,
                SpeechSynthesisResultStatus.Failed,
                voice.Id,
                stopwatch.Elapsed,
                $"System speech playback failed: {exception.Message}");
        }
        finally
        {
            lock (_processGate)
            {
                if (ReferenceEquals(_activeProcess, process))
                {
                    _activeProcess = null;
                    _activeStopCancellation = null;
                }
            }

            process?.Dispose();
        }
    }

    public Task StopAsync()
    {
        IChildProcess? process;
        CancellationTokenSource? stopCancellation;
        lock (_processGate)
        {
            process = _activeProcess;
            stopCancellation = _activeStopCancellation;
            _activeProcess = null;
            _activeStopCancellation = null;
        }

        stopCancellation?.Cancel();
        Kill(process);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopAsync().GetAwaiter().GetResult();
    }

    internal static IReadOnlyList<SpeechVoice> ParseMacVoices(string output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var voices = new List<SpeechVoice>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = MacVoiceLine().Match(line.TrimEnd('\r'));
            if (!match.Success)
            {
                continue;
            }

            var locale = match.Groups["locale"].Value.Replace('_', '-').ToLowerInvariant();
            var name = match.Groups["name"].Value.Trim();
            voices.Add(new SpeechVoice(
                name,
                name,
                locale,
                new LanguageCode(locale.Split('-')[0])));
        }

        return voices.OrderBy(voice => voice.Id, StringComparer.Ordinal).ToArray();
    }

    internal static IReadOnlyList<SpeechVoice> ParseWindowsVoices(string output)
    {
        ArgumentNullException.ThrowIfNull(output);
        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd('\r').Split((char)31))
            .Where(parts => parts.Length == 2 &&
                            !string.IsNullOrWhiteSpace(parts[0]) &&
                            !string.IsNullOrWhiteSpace(parts[1]))
            .Select(parts =>
            {
                var locale = parts[1].Trim().ToLowerInvariant();
                return new SpeechVoice(
                    parts[0].Trim(),
                    parts[0].Trim(),
                    locale,
                    new LanguageCode(locale.Split('-')[0]));
            })
            .OrderBy(voice => voice.Id, StringComparer.Ordinal)
            .ToArray();
    }

    internal static ProcessStartInfo CreateSpeakStartInfo(
        string executable,
        SpeechSynthesisRequest request,
        SpeechVoice voice)
    {
        var startInfo = ChildProcessStartInfo.Create(executable);
        if (OperatingSystem.IsMacOS() || executable.EndsWith("say", StringComparison.Ordinal))
        {
            startInfo.ArgumentList.Add("-v");
            startInfo.ArgumentList.Add(voice.Id);
            startInfo.ArgumentList.Add("-r");
            startInfo.ArgumentList.Add(Math.Clamp((int)Math.Round(175 * request.Rate), 90, 300).ToString());
        }
        else
        {
            AddPowerShellCommand(startInfo, WindowsSpeakScript);
            startInfo.ArgumentList.Add(Math.Clamp((int)Math.Round((request.Rate - 1) * 5), -10, 10).ToString());
            startInfo.ArgumentList.Add(voice.Id);
        }

        return startInfo;
    }

    private static string? Validate(SpeechSynthesisRequest request)
    {
        if (request.RequestId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.Text) ||
            request.Text.Length > 500 ||
            string.IsNullOrWhiteSpace(request.Seed) ||
            double.IsNaN(request.Rate) ||
            double.IsInfinity(request.Rate) ||
            request.Rate is < 0.5 or > 1.5)
        {
            return "The speech playback request is invalid.";
        }

        return null;
    }

    private static void AddPowerShellCommand(ProcessStartInfo startInfo, string script)
    {
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(script);
    }

    private static string SafeFailure(string prefix, string error)
    {
        var compact = string.Join(' ', error.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(compact)
            ? $"{prefix}."
            : $"{prefix}: {(compact.Length <= 240 ? compact : compact[..240] + "…")}";
    }

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

    [GeneratedRegex("^(?<name>.+?)\\s+(?<locale>[a-z]{2}_[A-Z]{2})\\s+#", RegexOptions.CultureInvariant)]
    private static partial Regex MacVoiceLine();
}
