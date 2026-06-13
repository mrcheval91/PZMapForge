using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlLayerPackProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-deadmtl-cli-tests", Path.GetRandomFileName());

    public DeadMtlLayerPackProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private static string PackProjectJson =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "deadmtl_worldgen_project.json");

    private string LocalManifest => Path.Combine(_tempDir, ".local", "worldgen", "worldgen_layers.json");
    private string LocalLua      => Path.Combine(_tempDir, ".local", "worldgen", "WorldGenOverride.lua");

    private static (int ExitCode, string Stdout, string Stderr) RunCli(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = "dotnet",
            WorkingDirectory       = RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add(CliProjectPath);
        psi.ArgumentList.Add("--configuration");
        psi.ArgumentList.Add("Release");
        psi.ArgumentList.Add("--no-build");
        psi.ArgumentList.Add("--");
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Test 1: compile-worldgen-project exits 0 and writes manifest
    // -----------------------------------------------------------------------

    [Fact]
    public void DeadMtl_CompileProject_ExitsZeroAndWritesManifest()
    {
        var (code, stdout, stderr) = RunCli(
            "compile-worldgen-project",
            "--input",  PackProjectJson,
            "--output", LocalManifest);

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
        Assert.True(File.Exists(LocalManifest), $"Manifest not found: {LocalManifest}");
    }

    // -----------------------------------------------------------------------
    // Test 2: stdout contains Status OK
    // -----------------------------------------------------------------------

    [Fact]
    public void DeadMtl_CompileProject_StdoutContainsStatusOk()
    {
        var (_, stdout, _) = RunCli(
            "compile-worldgen-project",
            "--input",  PackProjectJson,
            "--output", LocalManifest);

        Assert.Contains("Status:        OK", stdout, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 3: stdout reports map ID
    // -----------------------------------------------------------------------

    [Fact]
    public void DeadMtl_CompileProject_StdoutReportsDeadMtlMapId()
    {
        var (_, stdout, _) = RunCli(
            "compile-worldgen-project",
            "--input",  PackProjectJson,
            "--output", LocalManifest);

        Assert.Contains("deadmtl_build42_worldgen_v1", stdout, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 4: generated manifest has correct format
    // -----------------------------------------------------------------------

    [Fact]
    public void DeadMtl_CompileProject_ManifestHasCorrectFormat()
    {
        RunCli("compile-worldgen-project", "--input", PackProjectJson, "--output", LocalManifest);

        var json = File.ReadAllText(LocalManifest);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("pzmapforge.worldgen.layers.v1",
            doc.RootElement.GetProperty("format").GetString());
    }

    // -----------------------------------------------------------------------
    // Test 5: generated manifest has map_id
    // -----------------------------------------------------------------------

    [Fact]
    public void DeadMtl_CompileProject_ManifestHasMapId()
    {
        RunCli("compile-worldgen-project", "--input", PackProjectJson, "--output", LocalManifest);

        using var doc = JsonDocument.Parse(File.ReadAllText(LocalManifest));
        var mapId     = doc.RootElement.GetProperty("map_id").GetString();
        Assert.Equal("deadmtl_build42_worldgen_v1", mapId);
    }

    // -----------------------------------------------------------------------
    // Test 6: generated manifest has nonzero modules
    // -----------------------------------------------------------------------

    [Fact]
    public void DeadMtl_CompileProject_ManifestHasModules()
    {
        RunCli("compile-worldgen-project", "--input", PackProjectJson, "--output", LocalManifest);

        using var doc = JsonDocument.Parse(File.ReadAllText(LocalManifest));
        var count     = doc.RootElement.GetProperty("static_modules").GetArrayLength();
        Assert.True(count > 0, $"Expected modules, got {count}");
    }

    // -----------------------------------------------------------------------
    // Test 7: manifest contains water, sand_bank, birch_forest, normal_road_WE_00
    // -----------------------------------------------------------------------

    [Fact]
    public void DeadMtl_CompileProject_ManifestContainsSupportedBiomesAndPrefabs()
    {
        RunCli("compile-worldgen-project", "--input", PackProjectJson, "--output", LocalManifest);

        var json = File.ReadAllText(LocalManifest);
        Assert.Contains("water",              json, StringComparison.Ordinal);
        Assert.Contains("sand_bank",          json, StringComparison.Ordinal);
        Assert.Contains("birch_forest",       json, StringComparison.Ordinal);
        Assert.Contains("normal_road_WE_00",  json, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 8: manifest feeds into compile-worldgen (end-to-end pipeline)
    // -----------------------------------------------------------------------

    [Fact]
    public void DeadMtl_ManifestFeedsIntoCompileWorldGen()
    {
        RunCli("compile-worldgen-project", "--input", PackProjectJson, "--output", LocalManifest);
        Assert.True(File.Exists(LocalManifest));

        var (code, stdout, stderr) = RunCli(
            "compile-worldgen",
            "--input",  LocalManifest,
            "--output", LocalLua);

        Assert.True(code == 0, $"compile-worldgen failed. Stdout: {stdout}. Stderr: {stderr}");
        Assert.True(File.Exists(LocalLua));
    }

    // -----------------------------------------------------------------------
    // Test 9: generated Lua has proven markers
    // -----------------------------------------------------------------------

    [Fact]
    public void DeadMtl_GeneratedLua_HasProvenMarkers()
    {
        RunCli("compile-worldgen-project", "--input", PackProjectJson, "--output", LocalManifest);
        RunCli("compile-worldgen", "--input", LocalManifest, "--output", LocalLua);

        var lua = File.ReadAllText(LocalLua);
        Assert.Contains("PZMAPFORGE_WORLDGENOVERRIDE_LOADED",  lua, StringComparison.Ordinal);
        Assert.Contains("worldgen[\"static_modules\"] = {",   lua, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 10: generated Lua is ASCII, no BOM
    // -----------------------------------------------------------------------

    [Fact]
    public void DeadMtl_GeneratedLua_IsAsciiNoBom()
    {
        RunCli("compile-worldgen-project", "--input", PackProjectJson, "--output", LocalManifest);
        RunCli("compile-worldgen", "--input", LocalManifest, "--output", LocalLua);

        var bytes = File.ReadAllBytes(LocalLua);
        var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        var hasNonAscii = bytes.Any(b => b > 127);

        Assert.False(hasBom,        "Lua must not have UTF-8 BOM");
        Assert.False(hasNonAscii,   "Lua must contain only ASCII bytes");
    }

    // -----------------------------------------------------------------------
    // Test 11: Lua does not use local-var form
    // -----------------------------------------------------------------------

    [Fact]
    public void DeadMtl_GeneratedLua_DoesNotUseLocalVarForm()
    {
        RunCli("compile-worldgen-project", "--input", PackProjectJson, "--output", LocalManifest);
        RunCli("compile-worldgen", "--input", LocalManifest, "--output", LocalLua);

        var lua = File.ReadAllText(LocalLua);
        Assert.DoesNotContain("local worldgenOverride", lua, StringComparison.Ordinal);
        Assert.DoesNotContain("return worldgenOverride", lua, StringComparison.Ordinal);
    }
}
