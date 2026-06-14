using System.Diagnostics;
using System.Runtime.Versioning;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlReadableTerrainProofProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-readable-terrain-process", Path.GetRandomFileName());

    public DeadMtlReadableTerrainProofProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private static string GeneratorScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "generate-readable-terrain-proof-pack.ps1");

    // Temp paths under .local/ so CLI .local/ guard is satisfied
    private string LocalPackDir   => Path.Combine(_tempDir, ".local", "readable-terrain-proof-pack");
    private string LocalOutputDir => Path.Combine(_tempDir, ".local", "readable-terrain-proof");

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
    // Generator script process tests
    // -----------------------------------------------------------------------

    [Fact]
    public void GeneratorScript_ExitsZero()
    {
        var (code, stdout, stderr) = RunPowerShell(GeneratorScript, "-OutputDir", LocalPackDir);

        Assert.True(code == 0,
            $"Generator script exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void GeneratorScript_CreatesProjectManifest()
    {
        RunPowerShell(GeneratorScript, "-OutputDir", LocalPackDir);

        Assert.True(
            File.Exists(Path.Combine(LocalPackDir, "readable_terrain_proof_project.json")),
            "readable_terrain_proof_project.json not found");
    }

    [Fact]
    public void GeneratorScript_CreatesBiomesLayerNonzero()
    {
        RunPowerShell(GeneratorScript, "-OutputDir", LocalPackDir);

        var path = Path.Combine(LocalPackDir, "layers", "terrain_biomes.png");
        Assert.True(File.Exists(path), "terrain_biomes.png not found");
        Assert.True(new FileInfo(path).Length > 0, "terrain_biomes.png is empty");
    }

    [Fact]
    public void GeneratorScript_CreatesRoadsLayerNonzero()
    {
        RunPowerShell(GeneratorScript, "-OutputDir", LocalPackDir);

        var path = Path.Combine(LocalPackDir, "layers", "terrain_roads.png");
        Assert.True(File.Exists(path), "terrain_roads.png not found");
        Assert.True(new FileInfo(path).Length > 0, "terrain_roads.png is empty");
    }

    [Fact]
    public void GeneratorScript_CreatesLayerColorChartNonzero()
    {
        RunPowerShell(GeneratorScript, "-OutputDir", LocalPackDir);

        var path = Path.Combine(LocalPackDir, "layers", "layer-color-chart.png");
        Assert.True(File.Exists(path), "layer-color-chart.png not found");
        Assert.True(new FileInfo(path).Length > 0, "layer-color-chart.png is empty");
    }

    [Fact]
    public void GeneratorScript_CreatesPaletteChartSidecar()
    {
        RunPowerShell(GeneratorScript, "-OutputDir", LocalPackDir);

        var path = Path.Combine(LocalPackDir, "palettes", "worldgen-png-palette.chart.png");
        Assert.True(File.Exists(path), "palette chart.png not found in proof pack palettes");
        Assert.True(new FileInfo(path).Length > 0, "palette chart.png is empty");
    }

    // -----------------------------------------------------------------------
    // compile-worldgen-project from generated proof pack
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldgenProject_FromGeneratedPack_ExitsZero()
    {
        RunPowerShell(GeneratorScript, "-OutputDir", LocalPackDir);
        Directory.CreateDirectory(LocalOutputDir);

        var projectFile  = Path.Combine(LocalPackDir, "readable_terrain_proof_project.json");
        var manifestPath = Path.Combine(LocalOutputDir, "worldgen_layers.json");

        var (code, stdout, stderr) = RunCli(
            "compile-worldgen-project",
            "--input",  projectFile,
            "--output", manifestPath);

        Assert.True(code == 0,
            $"compile-worldgen-project exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void CompileWorldgenProject_FromGeneratedPack_WritesManifest()
    {
        RunPowerShell(GeneratorScript, "-OutputDir", LocalPackDir);
        Directory.CreateDirectory(LocalOutputDir);

        var projectFile  = Path.Combine(LocalPackDir, "readable_terrain_proof_project.json");
        var manifestPath = Path.Combine(LocalOutputDir, "worldgen_layers.json");

        RunCli("compile-worldgen-project", "--input", projectFile, "--output", manifestPath);

        Assert.True(File.Exists(manifestPath), "worldgen_layers.json not found");
    }

    // -----------------------------------------------------------------------
    // compile-worldgen from generated manifest
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldgen_ExitsZero()
    {
        RunPowerShell(GeneratorScript, "-OutputDir", LocalPackDir);
        Directory.CreateDirectory(LocalOutputDir);

        var projectFile  = Path.Combine(LocalPackDir, "readable_terrain_proof_project.json");
        var manifestPath = Path.Combine(LocalOutputDir, "worldgen_layers.json");
        var luaPath      = Path.Combine(LocalOutputDir, "WorldGenOverride.lua");

        RunCli("compile-worldgen-project", "--input", projectFile, "--output", manifestPath);
        var (code, stdout, stderr) = RunCli("compile-worldgen", "--input", manifestPath, "--output", luaPath);

        Assert.True(code == 0,
            $"compile-worldgen exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void CompileWorldgen_LuaIsAsciiNoBom()
    {
        RunPowerShell(GeneratorScript, "-OutputDir", LocalPackDir);
        Directory.CreateDirectory(LocalOutputDir);

        var projectFile  = Path.Combine(LocalPackDir, "readable_terrain_proof_project.json");
        var manifestPath = Path.Combine(LocalOutputDir, "worldgen_layers.json");
        var luaPath      = Path.Combine(LocalOutputDir, "WorldGenOverride.lua");

        RunCli("compile-worldgen-project", "--input", projectFile, "--output", manifestPath);
        RunCli("compile-worldgen", "--input", manifestPath, "--output", luaPath);

        var bytes       = File.ReadAllBytes(luaPath);
        var hasBom      = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        var hasNonAscii = bytes.Any(b => b > 127);

        Assert.False(hasBom,      "Lua must not have BOM");
        Assert.False(hasNonAscii, "Lua must be ASCII-only");
    }

    [Fact]
    public void CompileWorldgen_LuaHasProvenSyntax()
    {
        RunPowerShell(GeneratorScript, "-OutputDir", LocalPackDir);
        Directory.CreateDirectory(LocalOutputDir);

        var projectFile  = Path.Combine(LocalPackDir, "readable_terrain_proof_project.json");
        var manifestPath = Path.Combine(LocalOutputDir, "worldgen_layers.json");
        var luaPath      = Path.Combine(LocalOutputDir, "WorldGenOverride.lua");

        RunCli("compile-worldgen-project", "--input", projectFile, "--output", manifestPath);
        RunCli("compile-worldgen", "--input", manifestPath, "--output", luaPath);

        var lua = File.ReadAllText(luaPath);
        Assert.Contains("PZMAPFORGE_WORLDGENOVERRIDE_LOADED", lua, StringComparison.Ordinal);
        Assert.Contains("worldgen[\"static_modules\"] = {",  lua, StringComparison.Ordinal);
    }

    [Fact]
    public void CompileWorldgen_LuaContainsMapId()
    {
        RunPowerShell(GeneratorScript, "-OutputDir", LocalPackDir);
        Directory.CreateDirectory(LocalOutputDir);

        var projectFile  = Path.Combine(LocalPackDir, "readable_terrain_proof_project.json");
        var manifestPath = Path.Combine(LocalOutputDir, "worldgen_layers.json");
        var luaPath      = Path.Combine(LocalOutputDir, "WorldGenOverride.lua");

        RunCli("compile-worldgen-project", "--input", projectFile, "--output", manifestPath);
        RunCli("compile-worldgen", "--input", manifestPath, "--output", luaPath);

        var lua = File.ReadAllText(luaPath);
        Assert.Contains("deadmtl_readable_terrain_proof_v1", lua, StringComparison.Ordinal);
    }
}
