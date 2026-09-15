// Helper that builds tests/upstream/harness/harness.c, a thin wrapper that simply forwards to
// siara-cc/Unishox2 unishox2.c, starts it as a single process and exchanges requests and responses
// over standard input and output.
// On Linux it is built directly with gcc. On Windows it is built and run natively with MSVC (cl.exe)
// through the VsDevCmd.bat of a Visual Studio installation located by vswhere.exe; WSL is not used.
// It is not started by the normal test run. When the siara-cc/Unishox2 comparison is requested,
// a build failure is treated as a test failure.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace Sakilabo.Unishox2.Tests;

public sealed class NativeHarness : IDisposable
{
    private readonly Process? _process;

    /// <summary>
    /// Why the harness is unavailable, such as the cl.exe or gcc output from a failed build.
    /// Null when IsAvailable is true. It is included in the exception or skip reason produced by
    /// RequireAvailableOrSkip so that build errors can be diagnosed instead of being swallowed.
    /// </summary>
    public string? UnavailableReason { get; }

    private NativeHarness(Process? process, string? unavailableReason = null)
    {
        _process = process;
        UnavailableReason = unavailableReason;
    }

    public bool IsAvailable => _process != null;

    /// <summary>
    /// Whether the comparison against the siara-cc/Unishox2 C implementation was requested explicitly
    /// with -p:RequireNativeHarness=true. The normal test run never detects, builds or runs a C compiler,
    /// regardless of the operating system.
    /// </summary>
    public static bool IsMandatory
    {
        get
        {
#if REQUIRE_NATIVE_HARNESS
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// Shared handling for an unavailable harness. Where IsMandatory is false the test is skipped
    /// explicitly, which assumes SkippableFact or SkippableTheory.
    /// Where the comparison against the C implementation was requested explicitly, being unavailable is
    /// itself a test failure, so that real problems such as a broken build or a missing MSVC are not hidden.
    /// </summary>
    public static void RequireAvailableOrSkip(NativeHarness harness)
    {
        if (harness.IsAvailable)
            return;

        string detail = harness.UnavailableReason ?? "(reason unknown)";

        Skip.If(!IsMandatory,
            $"Skipped because the native harness (gcc or MSVC cl.exe) is unavailable in this environment. Detail: {detail}");

        throw new InvalidOperationException(
            "The comparison against the siara-cc/Unishox2 C implementation was requested, but the native harness is unavailable. " +
            $"Check that gcc is installed, or that Visual Studio 2022 has the \"Desktop development with C++\" workload (VC.Tools.x86.x64). Detail: {detail}");
    }

    public static NativeHarness TryCreate()
    {
        if (!IsMandatory)
            return new NativeHarness(null, "The comparison against the siara-cc/Unishox2 C implementation was not requested. Pass -p:RequireNativeHarness=true to run it.");

        try
        {
            string repoRoot = FindRepoRoot();
            string upstreamDir = Path.Combine(repoRoot, "tests", "upstream");
            string harnessSrc = Path.Combine(upstreamDir, "harness", "harness.c");
            string unishoxDir = Path.Combine(upstreamDir, "Unishox2");
            string unishoxSrc = Path.Combine(unishoxDir, "unishox2.c");

            if (!File.Exists(harnessSrc) || !File.Exists(unishoxSrc))
                return new NativeHarness(null, $"harness.c or unishox2.c was not found ({harnessSrc} / {unishoxSrc}). Check that the submodule at tests/upstream/Unishox2 has been fetched with 'git submodule update --init --recursive'.");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string binPath = Path.Combine(Path.GetTempPath(), "unishoxsharp_harness_" + Guid.NewGuid().ToString("N"));
                var buildArgs = new[] { "-DUNISHOX_API_WITH_OUTPUT_LEN=1", "-O2", "-I", unishoxDir, harnessSrc, unishoxSrc, "-o", binPath };
                if (!RunProcess("gcc", buildArgs, out string gccOutput))
                    return new NativeHarness(null, "The gcc build failed. Check that gcc is installed.\n" + gccOutput);
                return new NativeHarness(StartProcess(binPath, Array.Empty<string>()));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string? vsDevCmd = FindVsDevCmd(out string vsSearchDetail);
                if (vsDevCmd == null)
                    return new NativeHarness(null, "vswhere.exe found no Visual Studio installation that includes the C++ build tools (VC.Tools.x86.x64)." + vsSearchDetail);

                // Use a fresh temporary directory per call so that parallel test builds do not collide.
                string binName = "harness_" + Guid.NewGuid().ToString("N");
                string workDir = Path.Combine(Path.GetTempPath(), binName);
                Directory.CreateDirectory(workDir);
                string binPath = Path.Combine(workDir, "harness.exe");

                // Put VsDevCmd.bat and cl.exe in a single .cmd so that paths containing spaces are quoted once.
                // /TC compiles as C, since unishox2.c and harness.c are C sources.
                // /std:c11 /utf-8 matches the siara-cc/Unishox2 sources, which are C11 and encoded as UTF-8.
                // MSVC rejects the comma operator inside #if, so the API without an output length is used.
                string buildScript = Path.Combine(workDir, "build.cmd");
                string scriptContent =
                    "@echo off\r\n" +
                    $"call \"{vsDevCmd}\" -arch=x64 -host_arch=x64 -no_logo\r\n" +
                    "if errorlevel 1 exit /b 1\r\n" +
                    $"cl /nologo /TC /std:c11 /utf-8 /W3 /I \"{unishoxDir}\" \"{harnessSrc}\" \"{unishoxSrc}\" /Fe:\"{binPath}\"\r\n";
                File.WriteAllText(buildScript, scriptContent, new UTF8Encoding(false));

                if (!RunProcess("cmd.exe", new[] { "/d", "/c", buildScript }, workDir, out string clOutput))
                    return new NativeHarness(null, "The MSVC (cl.exe) build failed.\nBuild script: " + buildScript + "\nOutput:\n" + clOutput);

                var process = StartProcess(binPath, Array.Empty<string>());
                if (process == null)
                    return new NativeHarness(null, $"The harness.exe that was built could not be started ({binPath}).");
                return new NativeHarness(process);
            }

            return new NativeHarness(null, $"Unsupported operating system ({RuntimeInformation.OSDescription}).");
        }
        catch (Exception ex)
        {
            return new NativeHarness(null, ex.ToString());
        }
    }

    /// <summary>
    /// Uses vswhere.exe at its fixed install location to find the newest Visual Studio that includes the
    /// C++ build tools (VC.Tools.x86.x64) and returns the path to its VsDevCmd.bat, or null if none is found.
    /// </summary>
    private static string? FindVsDevCmd(out string detail)
    {
        string vswhere = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Microsoft Visual Studio", "Installer", "vswhere.exe");

        if (!File.Exists(vswhere))
        {
            detail = $"vswhere.exe is not at its default location ({vswhere}).";
            return null;
        }

        var psi = new ProcessStartInfo(vswhere)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var a in new[] { "-latest", "-products", "*", "-requires", "Microsoft.VisualStudio.Component.VC.Tools.x86.x64", "-property", "installationPath" })
            psi.ArgumentList.Add(a);

        using var p = Process.Start(psi);
        if (p == null)
        {
            detail = "vswhere.exe could not be started.";
            return null;
        }
        string stdout = p.StandardOutput.ReadToEnd();
        string stderr = p.StandardError.ReadToEnd();
        p.WaitForExit(30000);

        string installPath = stdout.Trim();
        if (p.ExitCode != 0 || installPath.Length == 0)
        {
            detail = $"vswhere.exe found no Visual Studio with the C++ build tools (exit={p.ExitCode}).\n{stderr}";
            return null;
        }

        string vsDevCmd = Path.Combine(installPath, "Common7", "Tools", "VsDevCmd.bat");
        if (!File.Exists(vsDevCmd))
        {
            detail = $"VsDevCmd.bat was not found ({vsDevCmd}).";
            return null;
        }

        detail = "";
        return vsDevCmd;
    }

    /// <summary>Sends a one-line request and returns the one-line response.</summary>
    public string Send(string request)
    {
        if (_process == null)
            throw new InvalidOperationException("The native harness is unavailable.");
        _process.StandardInput.Write(request);
        _process.StandardInput.Write('\n');
        _process.StandardInput.Flush();
        string? line = _process.StandardOutput.ReadLine();
        return line ?? throw new InvalidOperationException("No response from the native harness; the process may have exited.");
    }

    private static bool RunProcess(string fileName, string[] arguments, out string output)
        => RunProcess(fileName, arguments, workingDirectory: null, out output);

    private static bool RunProcess(string fileName, string[] arguments, string? workingDirectory, out string output)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        if (workingDirectory != null)
            psi.WorkingDirectory = workingDirectory;
        foreach (var a in arguments)
            psi.ArgumentList.Add(a);
        return RunProcessCore(psi, out output);
    }

    /// <summary>The maximum number of output lines retained.</summary>
    private const int MaxCapturedOutputLines = 500;

    /// <summary>
    /// Drains stdout and stderr asynchronously before waiting for the process to exit. Using the plain
    /// synchronous ReadToEnd on the two streams in turn can deadlock, because a large amount of output
    /// fills the other pipe.
    /// On timeout the whole process tree is terminated, including children such as the cl.exe that cmd.exe starts.
    /// The captured output is truncated to at most 500 lines.
    /// </summary>
    private static bool RunProcessCore(ProcessStartInfo psi, out string output)
    {
        using var p = Process.Start(psi);
        if (p == null)
        {
            output = $"The process could not be started: {psi.FileName}";
            return false;
        }

        var sync = new object();
        var lines = new System.Collections.Generic.List<string>();
        int omitted = 0;
        void Capture(string? data)
        {
            if (data == null) return;
            lock (sync)
            {
                // Reserve up to two lines for the timeout notice and the omitted-line count.
                if (lines.Count < MaxCapturedOutputLines - 2) lines.Add(data);
                else omitted++;
            }
        }
        p.OutputDataReceived += (_, e) => Capture(e.Data);
        p.ErrorDataReceived += (_, e) => Capture(e.Data);
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        bool exited = p.WaitForExit(120000);
        if (!exited)
        {
            try { p.Kill(entireProcessTree: true); } catch { /* ignore exceptions raised while tearing down */ }
            p.WaitForExit(5000);
            lock (sync) lines.Add("(the process tree was terminated because of a timeout)");
        }
        else
        {
            // WaitForExit(int) does not guarantee that every asynchronous read event for the redirected
            // output has been processed; this is a documented caveat in the .NET documentation.
            // Call the parameterless WaitForExit() afterwards to wait for OutputDataReceived and
            // ErrorDataReceived to finish before treating the output as complete.
            p.WaitForExit();
        }

        lock (sync)
        {
            if (omitted > 0)
                lines.Add($"...({omitted} output lines omitted; the limit is {MaxCapturedOutputLines} lines)");
            output = string.Join(Environment.NewLine, lines);
        }
        return exited && p.ExitCode == 0;
    }

    private static Process? StartProcess(string fileName, string[] arguments)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardInputEncoding = Encoding.ASCII,
            StandardOutputEncoding = Encoding.ASCII,
        };
        foreach (var a in arguments)
            psi.ArgumentList.Add(a);
        var p = Process.Start(psi);
        return p;
    }

    private static string FindRepoRoot()
    {
        string dir = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(dir, "tests", "upstream")))
                return dir;
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        throw new DirectoryNotFoundException("tests/upstream was not found, so the repository root could not be determined.");
    }

    public void Dispose()
    {
        if (_process == null) return;
        try
        {
            if (!_process.HasExited)
            {
                _process.StandardInput.Close();
                _process.WaitForExit(5000);
                if (!_process.HasExited) _process.Kill();
            }
        }
        catch
        {
            // Ignore exceptions raised while tearing down the test.
        }
        _process.Dispose();
    }
}
