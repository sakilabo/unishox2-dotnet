// tests/upstream/harness/harness.c(siara-cc/Unishox2 unishox2.c を素通しで呼ぶだけの薄いラッパー)を
// ビルドして 1 プロセスとして起動し、標準入出力でリクエスト/応答をやり取りするヘルパー。
// Linux では gcc で直接ビルドする。Windows では vswhere.exe で見つけた Visual Studio の
// VsDevCmd.bat 経由で MSVC(cl.exe)をネイティブにビルド・実行する(WSL は使わない)。
// 通常テストでは起動しない。siara-cc/Unishox2比較を指定した場合のビルド失敗はテスト失敗にする。

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
    /// harness が利用できない場合の理由(ビルド失敗時の cl.exe/gcc の出力など)。
    /// IsAvailable が true の場合は null。ビルドエラーを握り潰さず診断できるようにするため、
    /// RequireAvailableOrSkip の例外・スキップ理由に含める。
    /// </summary>
    public string? UnavailableReason { get; }

    private NativeHarness(Process? process, string? unavailableReason = null)
    {
        _process = process;
        UnavailableReason = unavailableReason;
    }

    public bool IsAvailable => _process != null;

    /// <summary>
    /// -p:RequireNativeHarness=true でsiara-cc/Unishox2C版比較を明示的に指定したかどうか。
    /// 通常のテストではOSにかかわらずCコンパイラの検出・ビルド・実行を行わない。
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
    /// harness が利用できない場合の共通処理。IsMandatory が false の環境では
    /// (SkippableFact/SkippableTheory 前提で)明示的にスキップする。
    /// siara-cc/Unishox2C版比較を明示的に指定した場合では、利用できないこと自体を
    /// テスト失敗として扱う(ビルド不能・MSVC 未導入などの実際の問題を隠さないため)。
    /// </summary>
    public static void RequireAvailableOrSkip(NativeHarness harness)
    {
        if (harness.IsAvailable)
            return;

        string detail = harness.UnavailableReason ?? "(理由不明)";

        Skip.If(!IsMandatory,
            $"ネイティブハーネス(gcc または MSVC cl.exe)がこの環境では利用できないためスキップします。詳細: {detail}");

        throw new InvalidOperationException(
            "siara-cc/Unishox2C版比較が指定されましたが、ネイティブハーネスを利用できません。" +
            $"gcc のインストール、または Visual Studio 2022 の「C++ によるデスクトップ開発」(VC.Tools.x86.x64)の状態を確認してください。詳細: {detail}");
    }

    public static NativeHarness TryCreate()
    {
        if (!IsMandatory)
            return new NativeHarness(null, "siara-cc/Unishox2C版比較は未指定です。実行時は -p:RequireNativeHarness=true を指定してください。");

        try
        {
            string repoRoot = FindRepoRoot();
            string upstreamDir = Path.Combine(repoRoot, "tests", "upstream");
            string harnessSrc = Path.Combine(upstreamDir, "harness", "harness.c");
            string unishoxDir = Path.Combine(upstreamDir, "Unishox2");
            string unishoxSrc = Path.Combine(unishoxDir, "unishox2.c");

            if (!File.Exists(harnessSrc) || !File.Exists(unishoxSrc))
                return new NativeHarness(null, $"harness.c または unishox2.c が見つかりません({harnessSrc} / {unishoxSrc})。submodule(tests/upstream/Unishox2)を 'git submodule update --init --recursive' で取得済みか確認してください。");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string binPath = Path.Combine(Path.GetTempPath(), "unishoxsharp_harness_" + Guid.NewGuid().ToString("N"));
                var buildArgs = new[] { "-DUNISHOX_API_WITH_OUTPUT_LEN=1", "-O2", "-I", unishoxDir, harnessSrc, unishoxSrc, "-o", binPath };
                if (!RunProcess("gcc", buildArgs, out string gccOutput))
                    return new NativeHarness(null, "gcc によるビルドに失敗しました。gcc がインストールされているか確認してください。\n" + gccOutput);
                return new NativeHarness(StartProcess(binPath, Array.Empty<string>()));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string? vsDevCmd = FindVsDevCmd(out string vsSearchDetail);
                if (vsDevCmd == null)
                    return new NativeHarness(null, "vswhere.exe で C++ ビルドツール(VC.Tools.x86.x64)を含む Visual Studio が見つかりません。" + vsSearchDetail);

                // 並列テストのビルド出力が競合しないよう、呼び出しごとに一時ディレクトリを使う。
                string binName = "harness_" + Guid.NewGuid().ToString("N");
                string workDir = Path.Combine(Path.GetTempPath(), binName);
                Directory.CreateDirectory(workDir);
                string binPath = Path.Combine(workDir, "harness.exe");

                // VsDevCmd.bat と cl.exe を一つの .cmd に置き、空白を含むパスを一度だけ引用する。
                // /TC: C としてコンパイル(unishox2.c/harness.c は C ソース)。
                // /std:c11 /utf-8: siara-cc/Unishox2 のソースは C11 相当、ソースは UTF-8。
                // MSVC が #if 内のコンマ演算子を受理しないため、出力長なしの API を使う。
                string buildScript = Path.Combine(workDir, "build.cmd");
                string scriptContent =
                    "@echo off\r\n" +
                    $"call \"{vsDevCmd}\" -arch=x64 -host_arch=x64 -no_logo\r\n" +
                    "if errorlevel 1 exit /b 1\r\n" +
                    $"cl /nologo /TC /std:c11 /utf-8 /W3 /I \"{unishoxDir}\" \"{harnessSrc}\" \"{unishoxSrc}\" /Fe:\"{binPath}\"\r\n";
                File.WriteAllText(buildScript, scriptContent, new UTF8Encoding(false));

                if (!RunProcess("cmd.exe", new[] { "/d", "/c", buildScript }, workDir, out string clOutput))
                    return new NativeHarness(null, "MSVC(cl.exe)によるビルドに失敗しました。\nビルドスクリプト: " + buildScript + "\n出力:\n" + clOutput);

                var process = StartProcess(binPath, Array.Empty<string>());
                if (process == null)
                    return new NativeHarness(null, $"ビルドした harness.exe の起動に失敗しました({binPath})。");
                return new NativeHarness(process);
            }

            return new NativeHarness(null, $"未対応の OS です({RuntimeInformation.OSDescription})。");
        }
        catch (Exception ex)
        {
            return new NativeHarness(null, ex.ToString());
        }
    }

    /// <summary>
    /// vswhere.exe(固定インストール先)で、C++ ビルドツール(VC.Tools.x86.x64)を含む
    /// 最新の Visual Studio を探し、その VsDevCmd.bat のパスを返す。見つからない場合は null。
    /// </summary>
    private static string? FindVsDevCmd(out string detail)
    {
        string vswhere = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Microsoft Visual Studio", "Installer", "vswhere.exe");

        if (!File.Exists(vswhere))
        {
            detail = $"vswhere.exe が既定の場所({vswhere})にありません。";
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
            detail = "vswhere.exe の起動に失敗しました。";
            return null;
        }
        string stdout = p.StandardOutput.ReadToEnd();
        string stderr = p.StandardError.ReadToEnd();
        p.WaitForExit(30000);

        string installPath = stdout.Trim();
        if (p.ExitCode != 0 || installPath.Length == 0)
        {
            detail = $"vswhere.exe が C++ ビルドツールを含む Visual Studio を見つけられませんでした(exit={p.ExitCode})。\n{stderr}";
            return null;
        }

        string vsDevCmd = Path.Combine(installPath, "Common7", "Tools", "VsDevCmd.bat");
        if (!File.Exists(vsDevCmd))
        {
            detail = $"VsDevCmd.bat が見つかりません({vsDevCmd})。";
            return null;
        }

        detail = "";
        return vsDevCmd;
    }

    /// <summary>1 行のリクエストを送り、1 行の応答を返す。</summary>
    public string Send(string request)
    {
        if (_process == null)
            throw new InvalidOperationException("ネイティブハーネスは利用できません。");
        _process.StandardInput.Write(request);
        _process.StandardInput.Write('\n');
        _process.StandardInput.Flush();
        string? line = _process.StandardOutput.ReadLine();
        return line ?? throw new InvalidOperationException("ネイティブハーネスからの応答がありません(プロセス終了の可能性)。");
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

    /// <summary>保持する出力の最大行数。</summary>
    private const int MaxCapturedOutputLines = 500;

    /// <summary>
    /// stdout/stderr を非同期に読み切ってからプロセス終了を待つ(標準の同期 ReadToEnd を
    /// 両ストリームに順番に使うと、出力量によっては相手側パイプが詰まってデッドロックしうるため)。
    /// タイムアウト時は子プロセス(cmd.exe が起動する cl.exe 等)ごとツリーで終了する。
    /// 出力は最大 500 行に切り詰める。
    /// </summary>
    private static bool RunProcessCore(ProcessStartInfo psi, out string output)
    {
        using var p = Process.Start(psi);
        if (p == null)
        {
            output = $"プロセスを起動できません: {psi.FileName}";
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
                // タイムアウトと省略件数の通知に最大2行を確保する。
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
            try { p.Kill(entireProcessTree: true); } catch { /* 終了処理中の例外は無視する */ }
            p.WaitForExit(5000);
            lock (sync) lines.Add("(タイムアウトのため、プロセスツリーを終了しました)");
        }
        else
        {
            // WaitForExit(int) は、リダイレクトされた出力の非同期読み取りイベントが
            // すべて処理し終わったことまでは保証しない(.NET のドキュメント上の既知の注意点)。
            // 引数無しの WaitForExit() を続けて呼び、OutputDataReceived/ErrorDataReceived の
            // 完了を待ってから出力を確定させる。
            p.WaitForExit();
        }

        lock (sync)
        {
            if (omitted > 0)
                lines.Add($"...(出力 {omitted} 行を省略、上限 {MaxCapturedOutputLines} 行)");
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
        throw new DirectoryNotFoundException("tests/upstream が見つかりません(リポジトリルートを特定できません)。");
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
            // テスト終了処理での例外は無視する。
        }
        _process.Dispose();
    }
}
