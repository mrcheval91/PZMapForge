using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadTileCandidateShortlistProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-shortlist-cli", Path.GetRandomFileName());

    public System2StaticRoadTileCandidateShortlistProcessTests() => Directory.CreateDirectory(_tempDir);

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

    private string MakeLocalSurveyJson()
    {
        var json = """
{
  "format": "pzmapforge.deadmtl.system2.static-road-local-tile-survey.v1",
  "status": "LOCAL_TILE_SURVEY_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_survey": "test.json",
  "pz_root": "D:\\fake\\pz",
  "families": [
    { "candidate_family": "asphalt_road_surface_candidate",
      "source_intents": ["local_street_asphalt"], "role": "road_surface",
      "resolution_status": "SURVEYED_CANDIDATES_FOUND", "confidence": "LOCAL_TEXT_MATCH_ONLY",
      "search_terms": ["asphalt", "street", "road", "pavement"],
      "candidate_tiles": [
        { "tile_name": "floors_exterior_street_asphalt_01_0",
          "source_file": "media/tiles/exterior.tiles",
          "match_reason": "matched term: asphalt",
          "confidence": "LOCAL_TEXT_MATCH_ONLY" },
        { "tile_name": "floors_exterior_street_asphalt_02_0",
          "source_file": "media/tiles/exterior.tiles",
          "match_reason": "matched term: asphalt",
          "confidence": "LOCAL_TEXT_MATCH_ONLY" },
        { "tile_name": "floors_exterior_street_asphalt_03_0",
          "source_file": "media/tiles/exterior.tiles",
          "match_reason": "matched term: asphalt",
          "confidence": "LOCAL_TEXT_MATCH_ONLY" }
      ], "notes": "" },
    { "candidate_family": "asphalt_alley_surface_candidate",
      "source_intents": ["alley_ruelle_asphalt"], "role": "road_surface",
      "resolution_status": "SURVEYED_NO_CANDIDATES_FOUND", "confidence": "NONE_YET",
      "search_terms": ["asphalt", "alley", "road", "pavement"],
      "candidate_tiles": [], "notes": "" },
    { "candidate_family": "asphalt_service_lane_candidate",
      "source_intents": ["service_lane"], "role": "road_surface",
      "resolution_status": "SURVEYED_NO_CANDIDATES_FOUND", "confidence": "NONE_YET",
      "search_terms": ["asphalt", "service", "road", "lane", "pavement"],
      "candidate_tiles": [], "notes": "" },
    { "candidate_family": "asphalt_parking_access_candidate",
      "source_intents": ["parking_access"], "role": "road_surface",
      "resolution_status": "SURVEYED_NO_CANDIDATES_FOUND", "confidence": "NONE_YET",
      "search_terms": ["asphalt", "parking", "driveway", "pavement"],
      "candidate_tiles": [], "notes": "" },
    { "candidate_family": "concrete_or_sidewalk_candidate",
      "source_intents": ["sidewalk_or_pedestrian_cut"], "role": "pedestrian_cut",
      "resolution_status": "SURVEYED_NO_CANDIDATES_FOUND", "confidence": "NONE_YET",
      "search_terms": ["concrete", "sidewalk", "pavement", "curb"],
      "candidate_tiles": [], "notes": "" },
    { "candidate_family": "road_node_metadata_candidate",
      "source_intents": ["intersection_node", "road_turn_node", "dead_end_node"], "role": "road_node",
      "resolution_status": "UNRESOLVED_NEEDS_TILE_SURVEY", "confidence": "NONE_YET",
      "search_terms": [],
      "candidate_tiles": [], "notes": "" }
  ],
  "totals": { "family_count": 6, "families_with_candidates": 1, "families_without_candidates": 5, "candidate_tile_count": 3 },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false, "runtime_proven": false, "public_playable_claim": false }
}
""";
        var path = Path.Combine(_tempDir, "local_survey.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    // -----------------------------------------------------------------------
    // CLI command: exit zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_OnFakeLocalSurvey()
    {
        var input   = MakeLocalSurveyJson();
        var outJson = Path.Combine(_tempDir, ".local", "shortlist.json");
        var outSumm = Path.Combine(_tempDir, ".local", "shortlist.summary.txt");

        var (code, stdout, stderr) = RunCli(
            "system2-build-static-road-tile-candidate-shortlist",
            "--input",   input,
            "--output",  outJson,
            "--summary", outSumm);

        Assert.True(code == 0,
            $"Exit {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // CLI command: output files created
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesOutputJson()
    {
        var input   = MakeLocalSurveyJson();
        var outJson = Path.Combine(_tempDir, ".local", "shortlist.json");
        var outSumm = Path.Combine(_tempDir, ".local", "shortlist.summary.txt");

        RunCli("system2-build-static-road-tile-candidate-shortlist",
            "--input", input, "--output", outJson, "--summary", outSumm);

        Assert.True(File.Exists(outJson), "shortlist.json not created");
    }

    [Fact]
    public void Cli_CreatesSummaryTxt()
    {
        var input   = MakeLocalSurveyJson();
        var outJson = Path.Combine(_tempDir, ".local", "shortlist.json");
        var outSumm = Path.Combine(_tempDir, ".local", "shortlist.summary.txt");

        RunCli("system2-build-static-road-tile-candidate-shortlist",
            "--input", input, "--output", outJson, "--summary", outSumm);

        Assert.True(File.Exists(outSumm), "shortlist.summary.txt not created");
    }

    // -----------------------------------------------------------------------
    // CLI command: JSON status fields
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("SHORTLIST_ONLY")]
    [InlineData("NOT_RUNTIME_PROVEN")]
    [InlineData("NOT_IMPLEMENTED")]
    [InlineData("LOCAL_TEXT_MATCH_RANKED_ONLY")]
    public void Cli_OutputJson_ContainsStatusField(string expected)
    {
        var input   = MakeLocalSurveyJson();
        var outJson = Path.Combine(_tempDir, ".local", "shortlist.json");
        var outSumm = Path.Combine(_tempDir, ".local", "shortlist.summary.txt");

        RunCli("system2-build-static-road-tile-candidate-shortlist",
            "--input", input, "--output", outJson, "--summary", outSumm);

        Assert.Contains(expected, File.ReadAllText(outJson), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CLI command: --top limits shortlist
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_TopFlag_LimitsShortlistPerFamily()
    {
        var input   = MakeLocalSurveyJson();
        var outJson = Path.Combine(_tempDir, ".local", "shortlist.json");
        var outSumm = Path.Combine(_tempDir, ".local", "shortlist.summary.txt");

        RunCli("system2-build-static-road-tile-candidate-shortlist",
            "--input", input, "--output", outJson, "--summary", outSumm, "--top", "2");

        using var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        var asphaltFamily = doc.RootElement.GetProperty("families").EnumerateArray()
            .Single(f => f.GetProperty("candidate_family").GetString() == "asphalt_road_surface_candidate");
        var candidateCount = asphaltFamily.GetProperty("candidates").GetArrayLength();
        Assert.True(candidateCount <= 2, $"Expected <= 2 candidates, got {candidateCount}");
    }

    // -----------------------------------------------------------------------
    // CLI command: missing args -> exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenRequiredArgsMissing()
    {
        var (code, _, stderr) = RunCli("system2-build-static-road-tile-candidate-shortlist");
        Assert.Equal(1, code);
        Assert.Contains("--input", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // CLI command: output not under .local -> exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var input   = MakeLocalSurveyJson();
        var badOut  = Path.Combine(_tempDir, "shortlist.json");
        var outSumm = Path.Combine(_tempDir, ".local", "shortlist.summary.txt");

        var (code, _, stderr) = RunCli(
            "system2-build-static-road-tile-candidate-shortlist",
            "--input", input, "--output", badOut, "--summary", outSumm);

        Assert.Equal(1, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Unknown command list
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommandError_MentionsShortlistCommand()
    {
        var (code, _, stderr) = RunCli("not-a-real-command");
        Assert.Equal(1, code);
        Assert.Contains("system2-build-static-road-tile-candidate-shortlist", stderr, StringComparison.Ordinal);
    }
}

// -----------------------------------------------------------------------
// Helper script: static content + process tests
// -----------------------------------------------------------------------

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadTileCandidateShortlistHelperScriptTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-system2-static-road-tile-candidate-shortlist.ps1");

    private static string ShortlistDir =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "system2-static-road-tile-candidate-shortlist");

    private static string ShortlistJson =>
        Path.Combine(ShortlistDir, "system2_static_road_tile_candidate_shortlist.json");

    private static string ShortlistSummary =>
        Path.Combine(ShortlistDir, "system2_static_road_tile_candidate_shortlist.summary.txt");

    private static string DocPath =>
        Path.Combine(RepoRoot, "docs", "authoring", "DEADMTL_SYSTEM2_STATIC_ROAD_TILE_CANDIDATE_SHORTLIST.md");

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
    public void Readme_MentionsRunShortlistScript() =>
        Assert.Contains("run-system2-static-road-tile-candidate-shortlist.ps1",
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
    public void HelperScript_CreatesShortlistJson()
    {
        RunScript();
        Assert.True(File.Exists(ShortlistJson), $"Expected: {ShortlistJson}");
    }

    [Fact]
    public void HelperScript_CreatesShortlistSummaryTxt()
    {
        RunScript();
        Assert.True(File.Exists(ShortlistSummary), $"Expected: {ShortlistSummary}");
    }

    [Fact]
    public void HelperScript_DoesNotCreateWorldGenOverrideLua()
    {
        RunScript();
        Assert.False(File.Exists(Path.Combine(ShortlistDir, "WorldGenOverride.lua")),
            "WorldGenOverride.lua must not be written");
    }

    [Fact]
    public void HelperScript_DoesNotCreateLotpackFile()
    {
        RunScript();
        var lotpacks = Directory.Exists(ShortlistDir)
            ? Directory.GetFiles(ShortlistDir, "*.lotpack")
            : Array.Empty<string>();
        Assert.Empty(lotpacks);
    }
}
