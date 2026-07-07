using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadTileFamilySurveyProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-survey-cli", Path.GetRandomFileName());

    public System2StaticRoadTileFamilySurveyProcessTests() => Directory.CreateDirectory(_tempDir);

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
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Minimal tile-family plan fixture (all 6 candidate families)
    // -----------------------------------------------------------------------

    private string MakeMinimalTileFamilyPlanJson()
    {
        var json = """
{
  "format": "pzmapforge.deadmtl.system2.static-road-tile-family-plan.v1",
  "status": "PLAN_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_placement_plan": "test",
  "records": [
    { "world_x": 10600, "world_y": 8250, "pixel_x": 20, "pixel_y": 50,
      "layer_id": "static_roads_local", "class": "local_street",
      "intent": "local_street_asphalt", "role": "road_surface",
      "candidate_family": "asphalt_road_surface_candidate",
      "confidence": "LOW_METADATA_ONLY", "color": "#404040" },
    { "world_x": 10625, "world_y": 8270, "pixel_x": 45, "pixel_y": 70,
      "layer_id": "static_roads_alleys", "class": "alley_ruelle",
      "intent": "alley_ruelle_asphalt", "role": "road_surface",
      "candidate_family": "asphalt_alley_surface_candidate",
      "confidence": "LOW_METADATA_ONLY", "color": "#303030" },
    { "world_x": 10710, "world_y": 8240, "pixel_x": 130, "pixel_y": 40,
      "layer_id": "static_roads_service", "class": "service_lane",
      "intent": "service_lane", "role": "road_surface",
      "candidate_family": "asphalt_service_lane_candidate",
      "confidence": "LOW_METADATA_ONLY", "color": "#505050" },
    { "world_x": 10720, "world_y": 8280, "pixel_x": 140, "pixel_y": 80,
      "layer_id": "static_roads_parking_access", "class": "parking_access",
      "intent": "parking_access", "role": "road_surface",
      "candidate_family": "asphalt_parking_access_candidate",
      "confidence": "LOW_METADATA_ONLY", "color": "#606060" },
    { "world_x": 10640, "world_y": 8290, "pixel_x": 60, "pixel_y": 90,
      "layer_id": "static_pedestrian_cuts", "class": "pedestrian_cut",
      "intent": "sidewalk_or_pedestrian_cut", "role": "pedestrian_cut",
      "candidate_family": "concrete_or_sidewalk_candidate",
      "confidence": "LOW_METADATA_ONLY", "color": "#B0B0B0" },
    { "world_x": 10650, "world_y": 8252, "pixel_x": 70, "pixel_y": 52,
      "layer_id": "static_road_nodes", "class": "intersection_turn_deadend_nodes",
      "intent": "intersection_node", "role": "road_node",
      "candidate_family": "road_node_metadata_candidate",
      "confidence": "LOW_METADATA_ONLY", "color": "#FF00FF" }
  ],
  "totals": { "record_count": 6, "by_candidate_family": {}, "by_role": {}, "by_intent": {}, "unmapped_count": 0 },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false,
                      "runtime_proven": false, "public_playable_claim": false }
}
""";
        var path = Path.Combine(_tempDir, "tile_family_plan.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    // -----------------------------------------------------------------------
    // CLI command: exit zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_OnValidTileFamilyPlan()
    {
        var input      = MakeMinimalTileFamilyPlanJson();
        var outputJson = Path.Combine(_tempDir, ".local", "survey.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "survey.summary.txt");

        var (code, stdout, stderr) = RunCli(
            "system2-build-static-road-tile-family-survey",
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
        var input      = MakeMinimalTileFamilyPlanJson();
        var outputJson = Path.Combine(_tempDir, ".local", "survey.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "survey.summary.txt");

        RunCli("system2-build-static-road-tile-family-survey",
            "--input", input, "--output", outputJson, "--summary", summaryTxt);

        Assert.True(File.Exists(outputJson), "survey.json not created");
    }

    [Fact]
    public void Cli_CreatesSummaryTxt()
    {
        var input      = MakeMinimalTileFamilyPlanJson();
        var outputJson = Path.Combine(_tempDir, ".local", "survey.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "survey.summary.txt");

        RunCli("system2-build-static-road-tile-family-survey",
            "--input", input, "--output", outputJson, "--summary", summaryTxt);

        Assert.True(File.Exists(summaryTxt), "survey.summary.txt not created");
    }

    // -----------------------------------------------------------------------
    // CLI command: JSON status fields
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("SURVEY_CONTRACT_ONLY")]
    [InlineData("NOT_RUNTIME_PROVEN")]
    [InlineData("NOT_IMPLEMENTED")]
    public void Cli_OutputJson_ContainsStatusField(string expected)
    {
        var input      = MakeMinimalTileFamilyPlanJson();
        var outputJson = Path.Combine(_tempDir, ".local", "survey.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "survey.summary.txt");

        RunCli("system2-build-static-road-tile-family-survey",
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
        var input      = MakeMinimalTileFamilyPlanJson();
        var outputJson = Path.Combine(_tempDir, ".local", "survey.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "survey.summary.txt");

        RunCli("system2-build-static-road-tile-family-survey",
            "--input", input, "--output", outputJson, "--summary", summaryTxt);

        Assert.Contains(expected, File.ReadAllText(outputJson), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CLI command: UNRESOLVED status present
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputJson_ContainsUnresolvedStatus()
    {
        var input      = MakeMinimalTileFamilyPlanJson();
        var outputJson = Path.Combine(_tempDir, ".local", "survey.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "survey.summary.txt");

        RunCli("system2-build-static-road-tile-family-survey",
            "--input", input, "--output", outputJson, "--summary", summaryTxt);

        Assert.Contains("UNRESOLVED_NEEDS_TILE_SURVEY",
            File.ReadAllText(outputJson), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CLI command: missing args -> exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenRequiredArgsMissing()
    {
        var (code, _, stderr) = RunCli("system2-build-static-road-tile-family-survey");
        Assert.Equal(1, code);
        Assert.Contains("--input", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // CLI command: output not under .local -> exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var input      = MakeMinimalTileFamilyPlanJson();
        var badOutput  = Path.Combine(_tempDir, "survey.json");
        var summaryTxt = Path.Combine(_tempDir, ".local", "survey.summary.txt");

        var (code, _, stderr) = RunCli(
            "system2-build-static-road-tile-family-survey",
            "--input", input, "--output", badOutput, "--summary", summaryTxt);

        Assert.Equal(1, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Unknown command list
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommandError_MentionsSurveyCommand()
    {
        var (code, _, stderr) = RunCli("not-a-real-command");
        Assert.Equal(1, code);
        Assert.Contains("system2-build-static-road-tile-family-survey", stderr, StringComparison.Ordinal);
    }
}

// -----------------------------------------------------------------------
// Helper script: static content + process tests
// -----------------------------------------------------------------------

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadTileFamilySurveyHelperScriptTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-system2-static-road-tile-family-survey.ps1");

    private static string SurveyDir =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "system2-static-road-tile-family-survey");

    private static string SurveyJson =>
        Path.Combine(SurveyDir, "system2_static_road_tile_family_survey.json");

    private static string SurveySummary =>
        Path.Combine(SurveyDir, "system2_static_road_tile_family_survey.summary.txt");

    private static string DocPath =>
        Path.Combine(RepoRoot, "docs", "authoring", "DEADMTL_SYSTEM2_STATIC_ROAD_TILE_FAMILY_SURVEY.md");

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
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
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
    public void Readme_MentionsRunSurveyScript() =>
        Assert.Contains("run-system2-static-road-tile-family-survey.ps1",
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
    public void HelperScript_CreatesSurveyJson()
    {
        RunScript();
        Assert.True(File.Exists(SurveyJson), $"Expected: {SurveyJson}");
    }

    [Fact]
    public void HelperScript_CreatesSurveySummaryTxt()
    {
        RunScript();
        Assert.True(File.Exists(SurveySummary), $"Expected: {SurveySummary}");
    }

    [Fact]
    public void HelperScript_OutputJson_ContainsSurveyContractOnly()
    {
        RunScript();
        Assert.Contains("SURVEY_CONTRACT_ONLY",
            File.ReadAllText(SurveyJson), StringComparison.Ordinal);
    }

    [Fact]
    public void HelperScript_OutputJson_ContainsUnresolvedStatus()
    {
        RunScript();
        Assert.Contains("UNRESOLVED_NEEDS_TILE_SURVEY",
            File.ReadAllText(SurveyJson), StringComparison.Ordinal);
    }

    [Fact]
    public void HelperScript_FamilyCount_IsSix()
    {
        RunScript();
        using var doc = JsonDocument.Parse(File.ReadAllText(SurveyJson));
        var familyCount = doc.RootElement.GetProperty("totals")
            .GetProperty("family_count").GetInt32();
        Assert.Equal(6, familyCount);
    }

    [Fact]
    public void HelperScript_DoesNotCreateWorldGenOverrideLua()
    {
        RunScript();
        Assert.False(File.Exists(Path.Combine(SurveyDir, "WorldGenOverride.lua")),
            "WorldGenOverride.lua must not be written");
    }

    [Fact]
    public void HelperScript_DoesNotCreateLotpackFile()
    {
        RunScript();
        var lotpacks = Directory.Exists(SurveyDir)
            ? Directory.GetFiles(SurveyDir, "*.lotpack")
            : Array.Empty<string>();
        Assert.Empty(lotpacks);
    }
}
