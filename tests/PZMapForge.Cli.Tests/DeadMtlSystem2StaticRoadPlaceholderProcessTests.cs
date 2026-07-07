using System.Diagnostics;
using System.Runtime.Versioning;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlSystem2StaticRoadPlaceholderProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-system2-placeholders", Path.GetRandomFileName());

    public DeadMtlSystem2StaticRoadPlaceholderProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string PlaceholderScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "generate-system2-static-road-placeholders.ps1");

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

    private void RunScript() => RunPowerShell(PlaceholderScript, "-OutputDir", _tempDir);

    // -----------------------------------------------------------------------
    // Exit code and top-level outputs
    // -----------------------------------------------------------------------

    [Fact]
    public void Script_ExitsZero()
    {
        var (code, stdout, stderr) = RunPowerShell(PlaceholderScript, "-OutputDir", _tempDir);

        Assert.True(code == 0,
            $"Script exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void Script_CreatesReadme()
    {
        RunScript();
        Assert.True(File.Exists(Path.Combine(_tempDir, "README.md")), "README.md not created");
    }

    [Fact]
    public void Script_CreatesContractJson()
    {
        RunScript();
        Assert.True(
            File.Exists(Path.Combine(_tempDir, "system2-static-road-overlay-contract.json")),
            "system2-static-road-overlay-contract.json not created");
    }

    [Fact]
    public void Script_CreatesPaletteJson()
    {
        RunScript();
        Assert.True(
            File.Exists(Path.Combine(_tempDir, "palettes", "system2-static-road-intent-palette.json")),
            "palettes/system2-static-road-intent-palette.json not created");
    }

    // -----------------------------------------------------------------------
    // Placeholder PNGs (six layers, each nonzero)
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("layers/static_roads_local.png")]
    [InlineData("layers/static_roads_alleys.png")]
    [InlineData("layers/static_roads_service.png")]
    [InlineData("layers/static_roads_parking_access.png")]
    [InlineData("layers/static_pedestrian_cuts.png")]
    [InlineData("layers/static_road_nodes.png")]
    public void Script_CreatesPlaceholderPng_NonzeroSize(string relPath)
    {
        RunScript();
        var fullPath = Path.Combine(_tempDir, relPath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(fullPath), $"{relPath} not created");
        Assert.True(new FileInfo(fullPath).Length > 0, $"{relPath} is empty");
    }

    // -----------------------------------------------------------------------
    // Color chart
    // -----------------------------------------------------------------------

    [Fact]
    public void Script_CreatesLayerColorChart()
    {
        RunScript();
        var chartPath = Path.Combine(_tempDir, "layers", "layer-color-chart.png");
        Assert.True(File.Exists(chartPath), "layer-color-chart.png not created");
        Assert.True(new FileInfo(chartPath).Length > 0, "layer-color-chart.png is empty");
    }

    // -----------------------------------------------------------------------
    // Negative: no Lua override written
    // -----------------------------------------------------------------------

    [Fact]
    public void Script_DoesNotCreateWorldGenOverrideLua()
    {
        RunScript();
        var luaPath = Path.Combine(_tempDir, "WorldGenOverride.lua");
        Assert.False(File.Exists(luaPath), "WorldGenOverride.lua must not be written by this script");
    }
}
