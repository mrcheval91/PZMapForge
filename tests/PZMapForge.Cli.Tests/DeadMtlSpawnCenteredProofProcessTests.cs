using System.Diagnostics;
using System.Runtime.Versioning;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlSpawnCenteredProofProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-spawn-proof-process", Path.GetRandomFileName());

    public DeadMtlSpawnCenteredProofProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private static string GenerateScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "generate-spawn-centered-proof-pack.ps1");

    private static (int ExitCode, string Stdout, string Stderr) RunPowerShell(string script, params string[] extraArgs)
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
        foreach (var a in extraArgs) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stdout, stderr);
    }

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
    // Test 1: generator exits 0
    // -----------------------------------------------------------------------

    [Fact]
    public void GenerateScript_ExitsZero()
    {
        var packDir = Path.Combine(_tempDir, "spawn-centered-proof-pack");
        var (code, stdout, stderr) = RunPowerShell(GenerateScript, "-OutputDir", packDir);

        Assert.True(code == 0,
            $"Generator exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // Test 2: generator creates project manifest
    // -----------------------------------------------------------------------

    [Fact]
    public void GenerateScript_CreatesProjectManifest()
    {
        var packDir = Path.Combine(_tempDir, "spawn-centered-proof-pack");
        RunPowerShell(GenerateScript, "-OutputDir", packDir);

        Assert.True(
            File.Exists(Path.Combine(packDir, "deadmtl_worldgen_project.json")),
            "Project manifest not found after generator run");
    }

    // -----------------------------------------------------------------------
    // Test 3: generator creates all 12 layer PNGs
    // -----------------------------------------------------------------------

    [Fact]
    public void GenerateScript_CreatesAllLayerPngs()
    {
        var packDir  = Path.Combine(_tempDir, "spawn-centered-proof-pack");
        RunPowerShell(GenerateScript, "-OutputDir", packDir);

        var layerIds = new[]
        {
            "water", "shore", "parks_forest", "roads_major",
            "roads_local", "zones_residential", "zones_commercial", "zones_industrial",
            "placed_buildings", "props", "npc_zones", "ownership",
        };
        foreach (var id in layerIds)
        {
            var path = Path.Combine(packDir, "layers", $"{id}.png");
            Assert.True(File.Exists(path), $"Missing layer PNG: {id}.png");
        }
    }

    // -----------------------------------------------------------------------
    // Test 4: generated pack passes DeadMtlLayerPackValidator
    // -----------------------------------------------------------------------

    [Fact]
    public void GenerateScript_PackPassesValidator()
    {
        var packDir = Path.Combine(_tempDir, "spawn-centered-proof-pack");
        var (genCode, genStdout, genStderr) = RunPowerShell(GenerateScript, "-OutputDir", packDir);
        Assert.True(genCode == 0,
            $"Generator failed (exit {genCode}).\nStdout: {genStdout}\nStderr: {genStderr}");

        var result = PZMapForge.Core.WorldGen.DeadMtlLayerPackValidator.Validate(packDir);
        Assert.True(result.IsValid,
            $"Generated pack failed validation:\n{string.Join("\n", result.Errors)}");
    }

    // -----------------------------------------------------------------------
    // Test 5: generated pack has expected map_id
    // -----------------------------------------------------------------------

    [Fact]
    public void GenerateScript_ManifestHasExpectedMapId()
    {
        var packDir = Path.Combine(_tempDir, "spawn-centered-proof-pack");
        RunPowerShell(GenerateScript, "-OutputDir", packDir);

        var manifest = File.ReadAllText(Path.Combine(packDir, "deadmtl_worldgen_project.json"));
        Assert.Contains("deadmtl_spawn_centered_proof_v1", manifest, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 6: authoring build from generated pack exits 0
    // -----------------------------------------------------------------------

    [Fact]
    public void AuthoringBuild_FromGeneratedPack_ExitsZero()
    {
        var packDir    = Path.Combine(_tempDir, "spawn-centered-proof-pack");
        var outputDir  = Path.Combine(_tempDir, ".local", "authoring");

        var (genCode, genStdout, genStderr) = RunPowerShell(GenerateScript, "-OutputDir", packDir);
        Assert.True(genCode == 0,
            $"Generator failed.\nStdout: {genStdout}\nStderr: {genStderr}");

        var (code, stdout, stderr) = RunCli(
            "deadmtl-authoring-build", "--input", packDir, "--output", outputDir);

        Assert.True(code == 0,
            $"Authoring build exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // Test 7: authoring build writes all three output files
    // -----------------------------------------------------------------------

    [Fact]
    public void AuthoringBuild_FromGeneratedPack_WritesThreeFiles()
    {
        var packDir   = Path.Combine(_tempDir, "spawn-centered-proof-pack");
        var outputDir = Path.Combine(_tempDir, ".local", "authoring");

        RunPowerShell(GenerateScript, "-OutputDir", packDir);
        RunCli("deadmtl-authoring-build", "--input", packDir, "--output", outputDir);

        Assert.True(File.Exists(Path.Combine(outputDir, "worldgen_layers.json")));
        Assert.True(File.Exists(Path.Combine(outputDir, "WorldGenOverride.lua")));
        Assert.True(File.Exists(Path.Combine(outputDir, "proof-summary.txt")));
    }

    // -----------------------------------------------------------------------
    // Test 8: generated Lua contains proof map_id
    // -----------------------------------------------------------------------

    [Fact]
    public void AuthoringBuild_FromGeneratedPack_LuaContainsMapId()
    {
        var packDir   = Path.Combine(_tempDir, "spawn-centered-proof-pack");
        var outputDir = Path.Combine(_tempDir, ".local", "authoring");

        RunPowerShell(GenerateScript, "-OutputDir", packDir);
        RunCli("deadmtl-authoring-build", "--input", packDir, "--output", outputDir);

        var lua = File.ReadAllText(Path.Combine(outputDir, "WorldGenOverride.lua"));
        Assert.Contains("deadmtl_spawn_centered_proof_v1", lua, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 9: generated Lua has proven markers
    // -----------------------------------------------------------------------

    [Fact]
    public void AuthoringBuild_FromGeneratedPack_LuaHasProvenMarkers()
    {
        var packDir   = Path.Combine(_tempDir, "spawn-centered-proof-pack");
        var outputDir = Path.Combine(_tempDir, ".local", "authoring");

        RunPowerShell(GenerateScript, "-OutputDir", packDir);
        RunCli("deadmtl-authoring-build", "--input", packDir, "--output", outputDir);

        var lua = File.ReadAllText(Path.Combine(outputDir, "WorldGenOverride.lua"));
        Assert.Contains("PZMAPFORGE_WORLDGENOVERRIDE_LOADED", lua, StringComparison.Ordinal);
        Assert.Contains("worldgen[\"static_modules\"] = {",  lua, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 10: generated Lua is ASCII / no BOM
    // -----------------------------------------------------------------------

    [Fact]
    public void AuthoringBuild_FromGeneratedPack_LuaIsAsciiNoBom()
    {
        var packDir   = Path.Combine(_tempDir, "spawn-centered-proof-pack");
        var outputDir = Path.Combine(_tempDir, ".local", "authoring");

        RunPowerShell(GenerateScript, "-OutputDir", packDir);
        RunCli("deadmtl-authoring-build", "--input", packDir, "--output", outputDir);

        var bytes       = File.ReadAllBytes(Path.Combine(outputDir, "WorldGenOverride.lua"));
        var hasBom      = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        var hasNonAscii = bytes.Any(b => b > 127);

        Assert.False(hasBom,        "Lua must not have BOM");
        Assert.False(hasNonAscii,   "Lua must be ASCII-only");
    }

    // -----------------------------------------------------------------------
    // Test 11: run script does not install by default (no -Install flag)
    // -----------------------------------------------------------------------

    [Fact]
    public void RunScript_WithoutInstall_DoesNotWriteToGameFolder()
    {
        var runScript = Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-spawn-centered-proof-build.ps1");

        // Verify the script text has the guard that skips install by default
        var scriptText = File.ReadAllText(runScript);
        Assert.Contains("if (-not $Install)", scriptText, StringComparison.Ordinal);

        // The install destination must only appear inside the Install block
        const string installDest = "pzmapforge_build42_candidate_v4_001";
        var installIdx  = scriptText.IndexOf(installDest, StringComparison.Ordinal);
        var installGuardIdx = scriptText.IndexOf("if (-not $Install)", StringComparison.Ordinal);

        // Install destination reference must come after the guard (inside the Install block)
        Assert.True(installIdx > installGuardIdx,
            "Install destination path appears before the -Install guard — default run may install");
    }

    // -----------------------------------------------------------------------
    // Test 12: proof summary contains spawn-centered map_id
    // -----------------------------------------------------------------------

    [Fact]
    public void AuthoringBuild_FromGeneratedPack_ProofSummaryContainsMapId()
    {
        var packDir   = Path.Combine(_tempDir, "spawn-centered-proof-pack");
        var outputDir = Path.Combine(_tempDir, ".local", "authoring");

        RunPowerShell(GenerateScript, "-OutputDir", packDir);
        RunCli("deadmtl-authoring-build", "--input", packDir, "--output", outputDir);

        var proof = File.ReadAllText(Path.Combine(outputDir, "proof-summary.txt"));
        Assert.Contains("deadmtl_spawn_centered_proof_v1", proof, StringComparison.Ordinal);
    }
}
