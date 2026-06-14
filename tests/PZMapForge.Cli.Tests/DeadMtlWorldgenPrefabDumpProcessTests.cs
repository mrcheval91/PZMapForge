using System.Diagnostics;
using System.Runtime.Versioning;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlWorldgenPrefabDumpProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-prefab-dump-process", Path.GetRandomFileName());

    public DeadMtlWorldgenPrefabDumpProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string GeneratorScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "generate-worldgen-prefab-dump-lua.ps1");

    private static string RunScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-worldgen-prefab-dump.ps1");

    private string LocalOutputDir => Path.Combine(_tempDir, ".local", "worldgen-prefab-dump");

    private static (int ExitCode, string Stdout, string Stderr) RunPowerShell(string script, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = "powershell",
            WorkingDirectory       = RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        psi.ArgumentList.Add("-ExecutionPolicy");
        psi.ArgumentList.Add("Bypass");
        psi.ArgumentList.Add("-File");
        psi.ArgumentList.Add(script);
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Generator script process tests
    // -----------------------------------------------------------------------

    [Fact]
    public void GeneratorScript_ExitsZero()
    {
        var (code, stdout, stderr) = RunPowerShell(GeneratorScript, "-OutputDir", LocalOutputDir);

        Assert.True(code == 0,
            $"Generator script exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void GeneratorScript_CreatesLuaFile()
    {
        RunPowerShell(GeneratorScript, "-OutputDir", LocalOutputDir);

        var luaPath = Path.Combine(LocalOutputDir, "WorldGenOverride.lua");
        Assert.True(File.Exists(luaPath), "WorldGenOverride.lua not found");
        Assert.True(new FileInfo(luaPath).Length > 0, "WorldGenOverride.lua is empty");
    }

    [Fact]
    public void GeneratorScript_LuaIsAsciiNoBom()
    {
        RunPowerShell(GeneratorScript, "-OutputDir", LocalOutputDir);

        var bytes       = File.ReadAllBytes(Path.Combine(LocalOutputDir, "WorldGenOverride.lua"));
        var hasBom      = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        var hasNonAscii = bytes.Any(b => b > 127);

        Assert.False(hasBom,      "Lua must not have BOM");
        Assert.False(hasNonAscii, "Lua must be ASCII-only");
    }

    [Fact]
    public void GeneratorScript_LuaContainsPrefabDumpLoaded()
    {
        RunPowerShell(GeneratorScript, "-OutputDir", LocalOutputDir);

        var lua = File.ReadAllText(Path.Combine(LocalOutputDir, "WorldGenOverride.lua"));
        Assert.Contains("PZMAPFORGE_PREFAB_DUMP_LOADED", lua, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratorScript_LuaContainsPrefabKeyMarker()
    {
        RunPowerShell(GeneratorScript, "-OutputDir", LocalOutputDir);

        var lua = File.ReadAllText(Path.Combine(LocalOutputDir, "WorldGenOverride.lua"));
        Assert.Contains("PZMAPFORGE_PREFAB_KEY=", lua, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Run script process tests (no -Install)
    // -----------------------------------------------------------------------

    [Fact]
    public void RunScript_ExitsZeroWithoutInstall()
    {
        // Run without -Install; uses default .local/ output dir in repo root
        var (code, stdout, stderr) = RunPowerShell(RunScript);

        Assert.True(code == 0,
            $"Run script exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void RunScript_NoInstallDoesNotShowInstallVerdict()
    {
        var (_, stdout, _) = RunPowerShell(RunScript);

        Assert.DoesNotContain("MAP22B_WORLDGEN_PREFAB_DUMP_INSTALLED_RESTART_REQUIRED",
            stdout, StringComparison.Ordinal);
        Assert.Contains("SKIPPED", stdout, StringComparison.OrdinalIgnoreCase);
    }
}
