using System.Diagnostics;
using Linguistics.App.Speech;
using Linguistics.Core.Profiles;
using Linguistics.Core.Speech;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class LocalSpeechProviderTests
{
    [TestMethod]
    public void MacVoiceParserFindsGermanVoicesWithoutAssumingOneName()
    {
        const string output =
            "Anna                de_DE    # Hallo!\n" +
            "Flo (Deutsch)       de_DE    # Hallo!\n" +
            "Samantha            en_US    # Hello!\n";

        var voices = SystemSpeechSynthesisProvider.ParseMacVoices(output);

        Assert.HasCount(3, voices);
        Assert.HasCount(2, voices.Where(voice => voice.Language == new LanguageCode("de")));
        Assert.IsTrue(voices.Any(voice => voice.Id == "Flo (Deutsch)"));
    }

    [TestMethod]
    public void SpeechStartInfoKeepsLearnerTextOutOfArguments()
    {
        var request = new SpeechSynthesisRequest(
            Guid.NewGuid(),
            "Hallo; $(touch should-not-run)",
            new LanguageCode("de"),
            "fixed",
            Rate: 0.72);
        var voice = new SpeechVoice("Anna", "Anna", "de-de", new LanguageCode("de"));

        var startInfo = SystemSpeechSynthesisProvider.CreateSpeakStartInfo(
            "/usr/bin/say",
            request,
            voice);

        CollectionAssert.DoesNotContain(startInfo.ArgumentList.ToArray(), request.Text);
        CollectionAssert.Contains(startInfo.ArgumentList.ToArray(), "Anna");
        Assert.IsFalse(startInfo.UseShellExecute);
        Assert.IsTrue(startInfo.RedirectStandardInput);
    }

    [TestMethod]
    public void WhisperStartInfoUsesSeparateArgumentsAndNeverSavesAudio()
    {
        var request = new SpeechRecognitionRequest(
            Guid.NewGuid(),
            new LanguageCode("de"),
            TimeSpan.FromSeconds(15),
            RetainAudio: false);
        const string model = "/tmp/model with spaces;safe.bin";

        var startInfo = WhisperStreamRecognitionProvider.CreateStartInfo(
            "/tmp/whisper-stream",
            model,
            request);

        CollectionAssert.Contains(startInfo.ArgumentList.ToArray(), model);
        CollectionAssert.Contains(startInfo.ArgumentList.ToArray(), "de");
        CollectionAssert.DoesNotContain(startInfo.ArgumentList.ToArray(), "--save-audio");
        CollectionAssert.DoesNotContain(startInfo.ArgumentList.ToArray(), "-sa");
        Assert.IsFalse(startInfo.UseShellExecute);
    }

    [TestMethod]
    public void WhisperOutputParserReturnsOnlyTheFirstTranscriptBlock()
    {
        const string output = """
            [Start speaking]

            ### Transcription 0 START | t0 = 0 ms | t1 = 4000 ms

            [00:00:00.000 --> 00:00:02.000]  Ich möchte einen Kaffee,
            [00:00:02.000 --> 00:00:04.000]  bitte.

            ### Transcription 0 END
            ### Transcription 1 START | t0 = 4000 ms | t1 = 8000 ms
            [00:00:04.000 --> 00:00:08.000]  Ignore me
            ### Transcription 1 END
            """;

        var transcript = WhisperStreamRecognitionProvider.ParseFirstTranscription(output);

        Assert.AreEqual("Ich möchte einen Kaffee, bitte.", transcript);
    }

    [TestMethod]
    public async Task WhisperProviderAcceptsOneBoundedLocalTranscriptAndKillsOwnedProcess()
    {
        var directory = CreateTempDirectory();
        var executable = Path.Combine(directory, "whisper-stream");
        var model = Path.Combine(directory, "ggml-base.bin");
        await File.WriteAllTextAsync(executable, "fixture");
        await File.WriteAllBytesAsync(model, [1, 2, 3]);
        var process = new FakeChildProcess(
            """
            [Start speaking]
            ### Transcription 0 START | t0 = 0 ms | t1 = 3000 ms
            [00:00:00.000 --> 00:00:03.000]  Ich möchte einen Kaffee, bitte.
            ### Transcription 0 END
            """,
            string.Empty);
        var launcher = new QueueProcessLauncher(process);
        using var provider = WhisperStreamRecognitionProvider.CreateForTests(
            executable,
            model,
            launcher);
        var request = new SpeechRecognitionRequest(
            Guid.NewGuid(),
            new LanguageCode("de"),
            TimeSpan.FromSeconds(5),
            RetainAudio: false);
        try
        {
            var result = await provider.RecognizeAsync(request);

            Assert.AreEqual(SpeechRecognitionResultStatus.Accepted, result.Status);
            Assert.AreEqual("Ich möchte einen Kaffee, bitte.", result.Transcript);
            Assert.IsTrue(process.Killed);
            Assert.AreEqual(request.RequestId, result.RequestId);
            CollectionAssert.DoesNotContain(
                launcher.StartInfos.Single().ArgumentList.ToArray(),
                "-sa");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task WhisperProviderCancellationKillsOnlyItsOwnedProcess()
    {
        var directory = CreateTempDirectory();
        var executable = Path.Combine(directory, "whisper-stream");
        var model = Path.Combine(directory, "ggml-base.bin");
        await File.WriteAllTextAsync(executable, "fixture");
        await File.WriteAllBytesAsync(model, [1, 2, 3]);
        var process = new FakeChildProcess(new BlockingTextReader(), new StringReader(string.Empty));
        using var provider = WhisperStreamRecognitionProvider.CreateForTests(
            executable,
            model,
            new QueueProcessLauncher(process));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));
        try
        {
            var result = await provider.RecognizeAsync(
                new SpeechRecognitionRequest(
                    Guid.NewGuid(),
                    new LanguageCode("de"),
                    TimeSpan.FromSeconds(5),
                    RetainAudio: false),
                cancellation.Token);

            Assert.AreEqual(SpeechRecognitionResultStatus.Cancelled, result.Status);
            Assert.IsTrue(process.Killed);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task MissingConfiguredWhisperModelIsExplicitlyUnavailable()
    {
        var directory = CreateTempDirectory();
        var executable = Path.Combine(directory, "whisper-stream");
        await File.WriteAllTextAsync(executable, "fixture");
        using var provider = WhisperStreamRecognitionProvider.CreateForTests(
            executable,
            Path.Combine(directory, "missing.bin"),
            new QueueProcessLauncher());
        try
        {
            var snapshot = await provider.InspectAsync();

            Assert.AreEqual(SpeechCapabilityStatus.Misconfigured, snapshot.Status);
            StringAssert.Contains(snapshot.Message, "never downloads");
            Assert.IsNull(snapshot.Model);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task StopPlaybackCancelsTheOwnedSystemSpeechProcess()
    {
        var directory = CreateTempDirectory();
        var executable = Path.Combine(directory, "say");
        await File.WriteAllTextAsync(executable, "fixture");
        var discovery = new FakeChildProcess(
            "Anna                de_DE    # Hallo!\n",
            string.Empty,
            completeOnCreation: true);
        var playback = new FakeChildProcess(string.Empty, string.Empty);
        var launcher = new QueueProcessLauncher(discovery, playback);
        using var provider = SystemSpeechSynthesisProvider.CreateForTests(
            SystemSpeechPlatform.MacOS,
            executable,
            launcher);
        try
        {
            var speak = provider.SpeakAsync(new SpeechSynthesisRequest(
                Guid.NewGuid(),
                "Guten Tag!",
                new LanguageCode("de"),
                "fixed-seed"));
            while (launcher.StartInfos.Count < 2)
            {
                await Task.Yield();
            }

            await provider.StopAsync();
            var result = await speak;

            Assert.AreEqual(SpeechSynthesisResultStatus.Cancelled, result.Status);
            Assert.IsTrue(playback.Killed);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task RecordingDeletionPreservesUnrelatedFiles()
    {
        var directory = CreateTempDirectory();
        var nested = Path.Combine(directory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(nested);
        var recording = Path.Combine(nested, "owned.wav");
        var unrelated = Path.Combine(directory, "keep.txt");
        await File.WriteAllBytesAsync(recording, [1, 2, 3]);
        await File.WriteAllTextAsync(unrelated, "keep");
        var store = new SpeechRecordingStore(directory);
        try
        {
            var result = await store.DeleteAllAsync();

            Assert.AreEqual(1, result.DeletedFileCount);
            Assert.IsFalse(File.Exists(recording));
            Assert.IsTrue(File.Exists(unrelated));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "linguistics-speech-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed class QueueProcessLauncher(params IChildProcess[] processes) : IChildProcessLauncher
    {
        private readonly Queue<IChildProcess> _processes = new(processes);

        public List<ProcessStartInfo> StartInfos { get; } = [];

        public IChildProcess Start(ProcessStartInfo startInfo)
        {
            StartInfos.Add(startInfo);
            return _processes.Dequeue();
        }
    }

    private sealed class FakeChildProcess : IChildProcess
    {
        private readonly TaskCompletionSource _exit = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public FakeChildProcess(string output, string error, bool completeOnCreation = false)
            : this(new StringReader(output), new StringReader(error), completeOnCreation)
        {
        }

        public FakeChildProcess(
            TextReader output,
            TextReader error,
            bool completeOnCreation = false)
        {
            StandardOutput = output;
            StandardError = error;
            StandardInput = new StringWriter();
            if (completeOnCreation)
            {
                _exit.TrySetResult();
            }
        }

        public TextWriter StandardInput { get; }

        public TextReader StandardOutput { get; }

        public TextReader StandardError { get; }

        public bool HasExited => _exit.Task.IsCompleted;

        public int ExitCode => Killed ? 137 : 0;

        public int Id => 4242;

        public bool Killed { get; private set; }

        public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
            _exit.Task.WaitAsync(cancellationToken);

        public void Kill()
        {
            Killed = true;
            _exit.TrySetResult();
        }

        public void Dispose()
        {
            StandardInput.Dispose();
            StandardOutput.Dispose();
            StandardError.Dispose();
        }
    }

    private sealed class BlockingTextReader : TextReader
    {
        public override async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return null;
        }
    }
}
