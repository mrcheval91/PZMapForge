using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadSampleExtractTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string ScriptsDir =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts");

    private static string SampleScript =>
        Path.Combine(ScriptsDir, "generate-system2-static-road-sample.ps1");

    private static string RunHelperScript =>
        Path.Combine(ScriptsDir, "run-system2-static-road-sample-extract.ps1");

    private static string SampleDir =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "system2-static-road-sample");

    private static string ExtractDir =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "system2-static-road-sample-extract");

    private static string ExtractJson =>
        Path.Combine(ExtractDir, "system2_static_road_sample_extract.json");

    private static string SummaryTxt =>
        Path.Combine(ExtractDir, "system2_static_road_sample_extract.summary.txt");

    private static (int ExitCode, string Stdout, string Stderr) RunPowerShell(string script)
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

        using var proc = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Sample generator process tests
    // -----------------------------------------------------------------------

    [Fact]
    public void SampleGenerator_ExitsZero()
    {
        var (code, stdout, stderr) = RunPowerShell(SampleScript);
        Assert.True(code == 0,
            $"Script exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Theory]
    [InlineData("layers/static_roads_local.png")]
    [InlineData("layers/static_roads_alleys.png")]
    [InlineData("layers/static_roads_service.png")]
    [InlineData("layers/static_roads_parking_access.png")]
    [InlineData("layers/static_pedestrian_cuts.png")]
    [InlineData("layers/static_road_nodes.png")]
    public void SampleGenerator_CreatesLayerPng(string relPath)
    {
        RunPowerShell(SampleScript);
        var full = Path.Combine(SampleDir, relPath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(full), $"{relPath} not created");
        Assert.True(new FileInfo(full).Length > 0, $"{relPath} is empty");
    }

    [Fact]
    public void SampleGenerator_CreatesContractJson()
    {
        RunPowerShell(SampleScript);
        Assert.True(
            File.Exists(Path.Combine(SampleDir, "system2-static-road-overlay-contract.json")),
            "contract JSON not created");
    }

    [Fact]
    public void SampleGenerator_CreatesPaletteJson()
    {
        RunPowerShell(SampleScript);
        Assert.True(
            File.Exists(Path.Combine(SampleDir, "palettes", "system2-static-road-intent-palette.json")),
            "palette JSON not created");
    }

    // -----------------------------------------------------------------------
    // Run helper process tests
    // -----------------------------------------------------------------------

    [Fact]
    public void RunHelper_ExitsZero()
    {
        var (code, stdout, stderr) = RunPowerShell(RunHelperScript);
        Assert.True(code == 0,
            $"Script exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void RunHelper_CreatesExtractJson()
    {
        RunPowerShell(RunHelperScript);
        Assert.True(File.Exists(ExtractJson), $"Expected: {ExtractJson}");
    }

    [Fact]
    public void RunHelper_CreatesSummaryTxt()
    {
        RunPowerShell(RunHelperScript);
        Assert.True(File.Exists(SummaryTxt), $"Expected: {SummaryTxt}");
    }

    // -----------------------------------------------------------------------
    // Extract JSON: status fields
    // -----------------------------------------------------------------------

    [Fact]
    public void ExtractJson_ContainsExtractOnly()
    {
        RunPowerShell(RunHelperScript);
        Assert.Contains("EXTRACT_ONLY", File.ReadAllText(ExtractJson), StringComparison.Ordinal);
    }

    [Fact]
    public void ExtractJson_ContainsNotRuntimeProven()
    {
        RunPowerShell(RunHelperScript);
        Assert.Contains("NOT_RUNTIME_PROVEN", File.ReadAllText(ExtractJson), StringComparison.Ordinal);
    }

    [Fact]
    public void ExtractJson_ContainsNotImplemented()
    {
        RunPowerShell(RunHelperScript);
        Assert.Contains("NOT_IMPLEMENTED", File.ReadAllText(ExtractJson), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Extract JSON: totals
    // -----------------------------------------------------------------------

    [Fact]
    public void ExtractJson_TotalsLayerCountIsSix()
    {
        RunPowerShell(RunHelperScript);
        using var doc = JsonDocument.Parse(File.ReadAllText(ExtractJson));
        Assert.Equal(6, doc.RootElement.GetProperty("totals").GetProperty("layer_count").GetInt32());
    }

    [Fact]
    public void ExtractJson_TotalsNonEmptyPixelsIsGreaterThanZero()
    {
        RunPowerShell(RunHelperScript);
        using var doc = JsonDocument.Parse(File.ReadAllText(ExtractJson));
        var nonEmpty = doc.RootElement.GetProperty("totals").GetProperty("non_empty_pixels").GetInt32();
        Assert.True(nonEmpty > 0, $"non_empty_pixels expected > 0, got {nonEmpty}");
    }

    // -----------------------------------------------------------------------
    // Extract JSON: per-layer non-empty checks
    // -----------------------------------------------------------------------

    private static JsonElement GetLayer(JsonDocument doc, string id)
    {
        foreach (var layer in doc.RootElement.GetProperty("layers").EnumerateArray())
            if (layer.GetProperty("id").GetString() == id)
                return layer;
        throw new Exception($"Layer '{id}' not found in extract JSON");
    }

    [Fact]
    public void ExtractJson_LocalLayer_HasNonEmptyPixels()
    {
        RunPowerShell(RunHelperScript);
        using var doc = JsonDocument.Parse(File.ReadAllText(ExtractJson));
        var px = GetLayer(doc, "static_roads_local").GetProperty("non_empty_pixels").GetInt32();
        Assert.True(px > 0, $"static_roads_local non_empty_pixels expected > 0, got {px}");
    }

    [Fact]
    public void ExtractJson_AlleysLayer_HasNonEmptyPixels()
    {
        RunPowerShell(RunHelperScript);
        using var doc = JsonDocument.Parse(File.ReadAllText(ExtractJson));
        var px = GetLayer(doc, "static_roads_alleys").GetProperty("non_empty_pixels").GetInt32();
        Assert.True(px > 0, $"static_roads_alleys non_empty_pixels expected > 0, got {px}");
    }

    // -----------------------------------------------------------------------
    // Extract JSON: intent types present
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("local_street_asphalt")]
    [InlineData("alley_ruelle_asphalt")]
    [InlineData("service_lane")]
    [InlineData("parking_access")]
    [InlineData("sidewalk_or_pedestrian_cut")]
    [InlineData("intersection_node")]
    [InlineData("road_turn_node")]
    [InlineData("dead_end_node")]
    public void ExtractJson_ContainsIntentType(string intent)
    {
        RunPowerShell(RunHelperScript);
        var text = File.ReadAllText(ExtractJson);
        Assert.Contains(intent, text, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Extract JSON: at least one run and one node
    // -----------------------------------------------------------------------

    [Fact]
    public void ExtractJson_ContainsAtLeastOneRun()
    {
        RunPowerShell(RunHelperScript);
        using var doc = JsonDocument.Parse(File.ReadAllText(ExtractJson));
        var totalRuns = doc.RootElement.GetProperty("layers").EnumerateArray()
            .Sum(l => l.GetProperty("runs").GetArrayLength());
        Assert.True(totalRuns > 0, $"Expected at least one run, got {totalRuns}");
    }

    [Fact]
    public void ExtractJson_ContainsAtLeastOneNode()
    {
        RunPowerShell(RunHelperScript);
        using var doc = JsonDocument.Parse(File.ReadAllText(ExtractJson));
        var totalNodes = doc.RootElement.GetProperty("layers").EnumerateArray()
            .Sum(l => l.GetProperty("nodes").GetArrayLength());
        Assert.True(totalNodes > 0, $"Expected at least one node, got {totalNodes}");
    }

    // -----------------------------------------------------------------------
    // Negative: no forbidden output files
    // -----------------------------------------------------------------------

    [Fact]
    public void RunHelper_DoesNotCreateWorldGenOverrideLua()
    {
        RunPowerShell(RunHelperScript);
        var luaPath = Path.Combine(ExtractDir, "WorldGenOverride.lua");
        Assert.False(File.Exists(luaPath), "WorldGenOverride.lua must not be written");
    }

    [Fact]
    public void RunHelper_DoesNotCreateLotpack()
    {
        RunPowerShell(RunHelperScript);
        var lotpacks = Directory.Exists(ExtractDir)
            ? Directory.GetFiles(ExtractDir, "*.lotpack")
            : Array.Empty<string>();
        Assert.Empty(lotpacks);
    }
}
