using System.Diagnostics;
using System.Runtime.Versioning;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlPaletteProofProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-palette-proof-process", Path.GetRandomFileName());

    public DeadMtlPaletteProofProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private static string ChartScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "generate-palette-color-charts.ps1");

    private static string ProofPackScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "generate-worldgen-palette-proof-pack.ps1");

    // Temp paths under .local/ so CLI .local/ guard is satisfied
    private string LocalPackDir    => Path.Combine(_tempDir, ".local", "worldgen-palette-proof-pack");
    private string LocalOutputDir  => Path.Combine(_tempDir, ".local", "worldgen-palette-proof");
    private string LocalPalettesDir => Path.Combine(_tempDir, ".local", "palettes");

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
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
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
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Chart generation
    // -----------------------------------------------------------------------

    [Fact]
    public void ChartScript_ExitsZero_WithRealPalettesDir()
    {
        var realPalettesDir = Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "palettes");
        var (code, stdout, stderr) = RunPowerShell(ChartScript, "-PalettesDir", realPalettesDir);

        Assert.True(code == 0,
            $"Chart script exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void ChartScript_WritesChartPng()
    {
        var realPalettesDir = Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "palettes");
        RunPowerShell(ChartScript, "-PalettesDir", realPalettesDir);

        var chartPath = Path.Combine(realPalettesDir, "worldgen-png-palette.chart.png");
        Assert.True(File.Exists(chartPath), "chart.png not found");

        var size = new FileInfo(chartPath).Length;
        Assert.True(size > 0, "chart.png is empty");
    }

    [Fact]
    public void ChartScript_WritesSwatchesTxt()
    {
        var realPalettesDir = Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "palettes");
        RunPowerShell(ChartScript, "-PalettesDir", realPalettesDir);

        var path = Path.Combine(realPalettesDir, "worldgen-png-palette.swatches.txt");
        Assert.True(File.Exists(path), "swatches.txt not found");

        var text = File.ReadAllText(path);
        Assert.Contains("VISUAL_CONFIRMED", text, StringComparison.Ordinal);
        // KNOWN_IN_CODE still appears in the header legend (expected after promotion)
        Assert.Contains("KNOWN_IN_CODE", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ChartScript_Swatches_GrassPlainIsVisualConfirmed()
    {
        var realPalettesDir = Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "palettes");
        RunPowerShell(ChartScript, "-PalettesDir", realPalettesDir);

        var text = File.ReadAllText(Path.Combine(realPalettesDir, "worldgen-png-palette.swatches.txt"));
        var grassLine = text.Split('\n')
            .FirstOrDefault(l => l.Contains("grass_plain", StringComparison.Ordinal)
                              && l.Contains("VISUAL_CONFIRMED", StringComparison.Ordinal));
        Assert.NotNull(grassLine);
    }

    [Fact]
    public void ChartScript_Swatches_PineForestIsVisualConfirmed()
    {
        var realPalettesDir = Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "palettes");
        RunPowerShell(ChartScript, "-PalettesDir", realPalettesDir);

        var text = File.ReadAllText(Path.Combine(realPalettesDir, "worldgen-png-palette.swatches.txt"));
        // "pine_forest" but not "light_pine_forest" — filter via VISUAL_CONFIRMED data line
        var pineLine = text.Split('\n')
            .FirstOrDefault(l => l.Contains("pine_forest", StringComparison.Ordinal)
                              && !l.Contains("light_pine_forest", StringComparison.Ordinal)
                              && l.Contains("VISUAL_CONFIRMED", StringComparison.Ordinal));
        Assert.NotNull(pineLine);
    }

    [Fact]
    public void ChartScript_WritesLayerGuideTxt()
    {
        var realPalettesDir = Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "palettes");
        RunPowerShell(ChartScript, "-PalettesDir", realPalettesDir);

        var path = Path.Combine(realPalettesDir, "worldgen-png-palette.layer-guide.txt");
        Assert.True(File.Exists(path), "layer-guide.txt not found");

        var text = File.ReadAllText(path);
        Assert.Contains("Proof chain", text, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Proof pack generation
    // -----------------------------------------------------------------------

    [Fact]
    public void ProofPackScript_ExitsZero()
    {
        var (code, stdout, stderr) = RunPowerShell(ProofPackScript, "-OutputDir", LocalPackDir);

        Assert.True(code == 0,
            $"Proof pack script exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void ProofPackScript_CreatesProjectManifest()
    {
        RunPowerShell(ProofPackScript, "-OutputDir", LocalPackDir);

        Assert.True(
            File.Exists(Path.Combine(LocalPackDir, "worldgen_palette_probe_project.json")),
            "Project manifest not found");
    }

    [Fact]
    public void ProofPackScript_CreatesBiomesLayerNonzero()
    {
        RunPowerShell(ProofPackScript, "-OutputDir", LocalPackDir);

        var path = Path.Combine(LocalPackDir, "layers", "worldgen_probe_biomes.png");
        Assert.True(File.Exists(path), "worldgen_probe_biomes.png not found");
        Assert.True(new FileInfo(path).Length > 0, "worldgen_probe_biomes.png is empty");
    }

    [Fact]
    public void ProofPackScript_CreatesPrefabsLayerNonzero()
    {
        RunPowerShell(ProofPackScript, "-OutputDir", LocalPackDir);

        var path = Path.Combine(LocalPackDir, "layers", "worldgen_probe_prefabs.png");
        Assert.True(File.Exists(path), "worldgen_probe_prefabs.png not found");
        Assert.True(new FileInfo(path).Length > 0, "worldgen_probe_prefabs.png is empty");
    }

    [Fact]
    public void ProofPackScript_CreatesChartPngNonzero()
    {
        RunPowerShell(ProofPackScript, "-OutputDir", LocalPackDir);

        var path = Path.Combine(LocalPackDir, "palettes", "worldgen-png-palette.chart.png");
        Assert.True(File.Exists(path), "chart.png not found in proof pack palettes");
        Assert.True(new FileInfo(path).Length > 0, "chart.png is empty");
    }

    // -----------------------------------------------------------------------
    // compile-worldgen-project from proof pack
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldgenProject_FromProofPack_ExitsZero()
    {
        RunPowerShell(ProofPackScript, "-OutputDir", LocalPackDir);
        Directory.CreateDirectory(LocalOutputDir);

        var projectFile  = Path.Combine(LocalPackDir, "worldgen_palette_probe_project.json");
        var manifestPath = Path.Combine(LocalOutputDir, "worldgen_layers.json");

        var (code, stdout, stderr) = RunCli(
            "compile-worldgen-project",
            "--input",  projectFile,
            "--output", manifestPath);

        Assert.True(code == 0,
            $"compile-worldgen-project exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void CompileWorldgenProject_FromProofPack_WritesManifest()
    {
        RunPowerShell(ProofPackScript, "-OutputDir", LocalPackDir);
        Directory.CreateDirectory(LocalOutputDir);

        var projectFile  = Path.Combine(LocalPackDir, "worldgen_palette_probe_project.json");
        var manifestPath = Path.Combine(LocalOutputDir, "worldgen_layers.json");

        RunCli("compile-worldgen-project", "--input", projectFile, "--output", manifestPath);

        Assert.True(File.Exists(manifestPath), "worldgen_layers.json not found");
    }

    // -----------------------------------------------------------------------
    // compile-worldgen from generated manifest
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldgen_FromProofManifest_ExitsZero()
    {
        RunPowerShell(ProofPackScript, "-OutputDir", LocalPackDir);
        Directory.CreateDirectory(LocalOutputDir);

        var projectFile  = Path.Combine(LocalPackDir, "worldgen_palette_probe_project.json");
        var manifestPath = Path.Combine(LocalOutputDir, "worldgen_layers.json");
        var luaPath      = Path.Combine(LocalOutputDir, "WorldGenOverride.lua");

        RunCli("compile-worldgen-project", "--input", projectFile, "--output", manifestPath);
        var (code, stdout, stderr) = RunCli("compile-worldgen", "--input", manifestPath, "--output", luaPath);

        Assert.True(code == 0,
            $"compile-worldgen exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void CompileWorldgen_FromProofManifest_LuaIsAsciiNoBom()
    {
        RunPowerShell(ProofPackScript, "-OutputDir", LocalPackDir);
        Directory.CreateDirectory(LocalOutputDir);

        var projectFile  = Path.Combine(LocalPackDir, "worldgen_palette_probe_project.json");
        var manifestPath = Path.Combine(LocalOutputDir, "worldgen_layers.json");
        var luaPath      = Path.Combine(LocalOutputDir, "WorldGenOverride.lua");

        RunCli("compile-worldgen-project", "--input", projectFile, "--output", manifestPath);
        RunCli("compile-worldgen", "--input", manifestPath, "--output", luaPath);

        var bytes       = File.ReadAllBytes(luaPath);
        var hasBom      = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        var hasNonAscii = bytes.Any(b => b > 127);

        Assert.False(hasBom,        "Lua must not have BOM");
        Assert.False(hasNonAscii,   "Lua must be ASCII-only");
    }

    [Fact]
    public void CompileWorldgen_FromProofManifest_LuaHasProvenSyntax()
    {
        RunPowerShell(ProofPackScript, "-OutputDir", LocalPackDir);
        Directory.CreateDirectory(LocalOutputDir);

        var projectFile  = Path.Combine(LocalPackDir, "worldgen_palette_probe_project.json");
        var manifestPath = Path.Combine(LocalOutputDir, "worldgen_layers.json");
        var luaPath      = Path.Combine(LocalOutputDir, "WorldGenOverride.lua");

        RunCli("compile-worldgen-project", "--input", projectFile, "--output", manifestPath);
        RunCli("compile-worldgen", "--input", manifestPath, "--output", luaPath);

        var lua = File.ReadAllText(luaPath);
        Assert.Contains("PZMAPFORGE_WORLDGENOVERRIDE_LOADED",  lua, StringComparison.Ordinal);
        Assert.Contains("worldgen[\"static_modules\"] = {",   lua, StringComparison.Ordinal);
    }

    [Fact]
    public void CompileWorldgen_FromProofManifest_LuaContainsMapId()
    {
        RunPowerShell(ProofPackScript, "-OutputDir", LocalPackDir);
        Directory.CreateDirectory(LocalOutputDir);

        var projectFile  = Path.Combine(LocalPackDir, "worldgen_palette_probe_project.json");
        var manifestPath = Path.Combine(LocalOutputDir, "worldgen_layers.json");
        var luaPath      = Path.Combine(LocalOutputDir, "WorldGenOverride.lua");

        RunCli("compile-worldgen-project", "--input", projectFile, "--output", manifestPath);
        RunCli("compile-worldgen", "--input", manifestPath, "--output", luaPath);

        var lua = File.ReadAllText(luaPath);
        Assert.Contains("deadmtl_worldgen_palette_probe_v1", lua, StringComparison.Ordinal);
    }
}
