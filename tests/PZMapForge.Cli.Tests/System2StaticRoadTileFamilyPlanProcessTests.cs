using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadTileFamilyPlanProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-tile-family-cli", Path.GetRandomFileName());

    public System2StaticRoadTileFamilyPlanProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProject =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj");

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
        psi.ArgumentList.Add(CliProject);
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
    // Minimal placement plan fixture (all 6 candidate families present)
    // -----------------------------------------------------------------------

    private string MakeMinimalPlacementPlanJson()
    {
        var json = """
{
  "format": "pzmapforge.deadmtl.system2.static-road-placement-plan.v1",
  "status": "PLAN_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_extract": "test",
  "origin_x": 10580, "origin_y": 8200, "width": 220, "height": 170,
  "placements": [
    { "world_x": 10600, "world_y": 8250, "pixel_x": 20, "pixel_y": 50,
      "layer_id": "static_roads_local", "class": "local_street",
      "intent": "local_street_asphalt", "role": "road_surface", "color": "#404040" },
    { "world_x": 10625, "world_y": 8270, "pixel_x": 45, "pixel_y": 70,
      "layer_id": "static_roads_alleys", "class": "alley_ruelle",
      "intent": "alley_ruelle_asphalt", "role": "road_surface", "color": "#303030" },
    { "world_x": 10710, "world_y": 8240, "pixel_x": 130, "pixel_y": 40,
      "layer_id": "static_roads_service", "class": "service_lane",
      "intent": "service_lane", "role": "road_surface", "color": "#505050" },
    { "world_x": 10720, "world_y": 8280, "pixel_x": 140, "pixel_y": 80,
      "layer_id": "static_roads_parking_access", "class": "parking_access",
      "intent": "parking_access", "role": "road_surface", "color": "#606060" },
    { "world_x": 10640, "world_y": 8290, "pixel_x": 60, "pixel_y": 90,
      "layer_id": "static_pedestrian_cuts", "class": "pedestrian_cut",
      "intent": "sidewalk_or_pedestrian_cut", "role": "pedestrian_cut", "color": "#B0B0B0" },
    { "world_x": 10650, "world_y": 8252, "pixel_x": 70, "pixel_y": 52,
      "layer_id": "static_road_nodes", "class": "intersection_turn_deadend_nodes",
      "intent": "intersection_node", "role": "road_node", "color": "#FF00FF" },
    { "world_x": 10700, "world_y": 8255, "pixel_x": 120, "pixel_y": 55,
      "layer_id": "static_road_nodes", "class": "intersection_turn_deadend_nodes",
      "intent": "road_turn_node", "role": "road_node", "color": "#00FFFF" },
    { "world_x": 10695, "world_y": 8272, "pixel_x": 115, "pixel_y": 72,
      "layer_id": "static_road_nodes", "class": "intersection_turn_deadend_nodes",
      "intent": "dead_end_node", "role": "road_node", "color": "#FF9900" }
  ],
  "totals": { "placement_count": 8, "run_source_count": 5, "node_source_count": 3,
              "duplicate_position_count": 0, "by_role": {}, "by_intent": {} },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false,
                      "runtime_proven": false, "public_playable_claim": false }
}
""";
        var path = Path.Combine(_tempDir, "placement_plan.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    // -----------------------------------------------------------------------
    // CLI command: exit zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_OnValidPlacementPlan()
    {
        var input      = MakeMinimalPlacementPlanJson();
        var outputJson = Path.Combine(_tempDir, ".local", "tile_family_plan.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "tile_family_plan.summary.txt");

        var (code, stdout, stderr) = RunCli(
            "system2-build-static-road-tile-family-plan",
            "--input",   input,
            "--output",  outputJson,
            "--summary", summaryTxt);

        Assert.True(code == 0,
            $"Exit {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // CLI command: output files
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesOutputJson()
    {
        var input      = MakeMinimalPlacementPlanJson();
        var outputJson = Path.Combine(_tempDir, ".local", "tile_family_plan.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "tile_family_plan.summary.txt");

        RunCli("system2-build-static-road-tile-family-plan",
            "--input", input, "--output", outputJson, "--summary", summaryTxt);

        Assert.True(File.Exists(outputJson), "tile_family_plan.json not created");
    }

    [Fact]
    public void Cli_CreatesSummaryTxt()
    {
        var input      = MakeMinimalPlacementPlanJson();
        var outputJson = Path.Combine(_tempDir, ".local", "tile_family_plan.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "tile_family_plan.summary.txt");

        RunCli("system2-build-static-road-tile-family-plan",
            "--input", input, "--output", outputJson, "--summary", summaryTxt);

        Assert.True(File.Exists(summaryTxt), "tile_family_plan.summary.txt not created");
    }

    // -----------------------------------------------------------------------
    // CLI command: JSON status fields
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("PLAN_ONLY")]
    [InlineData("NOT_RUNTIME_PROVEN")]
    [InlineData("NOT_IMPLEMENTED")]
    public void Cli_OutputJson_ContainsStatusField(string expected)
    {
        var input      = MakeMinimalPlacementPlanJson();
        var outputJson = Path.Combine(_tempDir, ".local", "tile_family_plan.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "tile_family_plan.summary.txt");

        RunCli("system2-build-static-road-tile-family-plan",
            "--input", input, "--output", outputJson, "--summary", summaryTxt);

        Assert.Contains(expected, File.ReadAllText(outputJson), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CLI command: candidate families present in JSON
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("asphalt_road_surface_candidate")]
    [InlineData("asphalt_alley_surface_candidate")]
    [InlineData("asphalt_service_lane_candidate")]
    [InlineData("asphalt_parking_access_candidate")]
    [InlineData("concrete_or_sidewalk_candidate")]
    [InlineData("road_node_metadata_candidate")]
    public void Cli_OutputJson_ContainsCandidateFamily(string expected)
    {
        var input      = MakeMinimalPlacementPlanJson();
        var outputJson = Path.Combine(_tempDir, ".local", "tile_family_plan.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "tile_family_plan.summary.txt");

        RunCli("system2-build-static-road-tile-family-plan",
            "--input", input, "--output", outputJson, "--summary", summaryTxt);

        Assert.Contains(expected, File.ReadAllText(outputJson), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CLI command: missing args -> exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenRequiredArgsMissing()
    {
        var (code, _, stderr) = RunCli("system2-build-static-road-tile-family-plan");
        Assert.Equal(1, code);
        Assert.Contains("--input", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // CLI command: output not under .local -> exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var input      = MakeMinimalPlacementPlanJson();
        var badOutput  = Path.Combine(_tempDir, "tile_family_plan.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "tile_family_plan.summary.txt");

        var (code, _, stderr) = RunCli(
            "system2-build-static-road-tile-family-plan",
            "--input", input, "--output", badOutput, "--summary", summaryTxt);

        Assert.Equal(1, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Unknown command list
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommandError_MentionsTileFamilyCommand()
    {
        var (code, _, stderr) = RunCli("not-a-real-command");
        Assert.Equal(1, code);
        Assert.Contains("system2-build-static-road-tile-family-plan", stderr, StringComparison.Ordinal);
    }
}

// -----------------------------------------------------------------------
// Helper script: static content + process tests
// -----------------------------------------------------------------------

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadTileFamilyPlanHelperScriptTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-system2-static-road-tile-family-plan.ps1");

    private static string TileDir =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "system2-static-road-tile-family-plan");

    private static string TileJson =>
        Path.Combine(TileDir, "system2_static_road_tile_family_plan.json");

    private static string TileSummary =>
        Path.Combine(TileDir, "system2_static_road_tile_family_plan.summary.txt");

    private static string DocPath =>
        Path.Combine(RepoRoot, "docs", "authoring", "DEADMTL_SYSTEM2_STATIC_ROAD_TILE_FAMILY_PLAN.md");

    private static string ReadmePath =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "README.md");

    private static (int ExitCode, string Stdout, string Stderr) RunScript()
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
        psi.ArgumentList.Add(HelperScript);

        using var proc = Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Static: forbidden strings
    // -----------------------------------------------------------------------

    [Fact]
    public void HelperScript_DoesNotContainCompileWorldgen() =>
        Assert.DoesNotContain("compile-worldgen", File.ReadAllText(HelperScript), StringComparison.Ordinal);

    [Fact]
    public void HelperScript_DoesNotContainWorldGenOverrideLua() =>
        Assert.DoesNotContain("WorldGenOverride.lua", File.ReadAllText(HelperScript), StringComparison.Ordinal);

    [Fact]
    public void HelperScript_DoesNotContainLotpack() =>
        Assert.DoesNotContain(".lotpack", File.ReadAllText(HelperScript), StringComparison.Ordinal);

    // -----------------------------------------------------------------------
    // Static: doc and README
    // -----------------------------------------------------------------------

    [Fact]
    public void Doc_MentionsNoRuntimeProof()
    {
        var text = File.ReadAllText(DocPath);
        Assert.True(
            text.Contains("NOT_RUNTIME_PROVEN", StringComparison.Ordinal) ||
            text.Contains("not runtime proof", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Runtime proof is NOT", StringComparison.OrdinalIgnoreCase),
            "doc should state no runtime proof");
    }

    [Fact]
    public void Doc_MentionsNoLotpack()
    {
        var text = File.ReadAllText(DocPath);
        Assert.True(
            text.Contains("lotpack", StringComparison.OrdinalIgnoreCase),
            "doc should mention lotpack claim boundary");
    }

    [Fact]
    public void Readme_MentionsRunTileFamilyPlanScript() =>
        Assert.Contains("run-system2-static-road-tile-family-plan.ps1",
            File.ReadAllText(ReadmePath), StringComparison.Ordinal);

    // -----------------------------------------------------------------------
    // Process: run helper script
    // -----------------------------------------------------------------------

    [Fact]
    public void HelperScript_ExitsZero()
    {
        var (code, stdout, stderr) = RunScript();
        Assert.True(code == 0,
            $"Script exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void HelperScript_CreatesTileFamilyPlanJson()
    {
        RunScript();
        Assert.True(File.Exists(TileJson), $"Expected: {TileJson}");
    }

    [Fact]
    public void HelperScript_CreatesTileFamilyPlanSummaryTxt()
    {
        RunScript();
        Assert.True(File.Exists(TileSummary), $"Expected: {TileSummary}");
    }

    [Theory]
    [InlineData("PLAN_ONLY")]
    [InlineData("NOT_RUNTIME_PROVEN")]
    [InlineData("NOT_IMPLEMENTED")]
    public void HelperScript_OutputJson_ContainsStatusField(string expected)
    {
        RunScript();
        Assert.Contains(expected, File.ReadAllText(TileJson), StringComparison.Ordinal);
    }

    [Fact]
    public void HelperScript_RecordCount_Equals1141()
    {
        RunScript();
        using var doc     = JsonDocument.Parse(File.ReadAllText(TileJson));
        var recordCount   = doc.RootElement.GetProperty("totals")
            .GetProperty("record_count").GetInt32();
        Assert.Equal(1141, recordCount);
    }

    [Fact]
    public void HelperScript_DoesNotCreateWorldGenOverrideLua()
    {
        RunScript();
        Assert.False(File.Exists(Path.Combine(TileDir, "WorldGenOverride.lua")),
            "WorldGenOverride.lua must not be written");
    }

    [Fact]
    public void HelperScript_DoesNotCreateLotpackFile()
    {
        RunScript();
        var lotpacks = Directory.Exists(TileDir)
            ? Directory.GetFiles(TileDir, "*.lotpack")
            : Array.Empty<string>();
        Assert.Empty(lotpacks);
    }
}
