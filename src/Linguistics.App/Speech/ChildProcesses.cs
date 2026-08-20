using System.Diagnostics;

namespace Linguistics.App.Speech;

internal interface IChildProcess : IDisposable
{
    TextWriter StandardInput { get; }

    TextReader StandardOutput { get; }

    TextReader StandardError { get; }

    bool HasExited { get; }

    int ExitCode { get; }

    int Id { get; }

    Task WaitForExitAsync(CancellationToken cancellationToken = default);

    void Kill();
}

internal interface IChildProcessLauncher
{
    IChildProcess Start(ProcessStartInfo startInfo);
}

internal sealed class ChildProcessLauncher : IChildProcessLauncher
{
    public IChildProcess Start(ProcessStartInfo startInfo)
    {
        var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException("The local speech process did not start.");
        }

        return new ChildProcess(process);
    }
}

internal sealed class ChildProcess(Process process) : IChildProcess
{
    public TextWriter StandardInput => process.StandardInput;

    public TextReader StandardOutput => process.StandardOutput;

    public TextReader StandardError => process.StandardError;

    public bool HasExited => process.HasExited;

    public int ExitCode => process.ExitCode;

    public int Id => process.Id;

    public Task WaitForExitAsync(CancellationToken cancellationToken = default) =>
        process.WaitForExitAsync(cancellationToken);

    public void Kill()
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }
    }

    public void Dispose() => process.Dispose();
}

internal static class ChildProcessStartInfo
{
    public static ProcessStartInfo Create(string executable) => new()
    {
        FileName = executable,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
}
