using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadLocalTileSurveyProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-local-survey-cli", Path.GetRandomFileName());

    public System2StaticRoadLocalTileSurveyProcessTests() => Directory.CreateDirectory(_tempDir);

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

    private string MakeSurveyJson()
    {
        var json = """
{
  "format": "pzmapforge.deadmtl.system2.static-road-tile-family-survey.v1",
  "status": "SURVEY_CONTRACT_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_tile_family_plan": "test.json",
  "families": [
    { "candidate_family": "asphalt_road_surface_candidate",
      "source_intents": ["local_street_asphalt"], "role": "road_surface",
      "resolution_status": "UNRESOLVED_NEEDS_TILE_SURVEY", "confidence": "NONE_YET",
      "candidate_tiles": [], "notes": "" },
    { "candidate_family": "asphalt_alley_surface_candidate",
      "source_intents": ["alley_ruelle_asphalt"], "role": "road_surface",
      "resolution_status": "UNRESOLVED_NEEDS_TILE_SURVEY", "confidence": "NONE_YET",
      "candidate_tiles": [], "notes": "" },
    { "candidate_family": "asphalt_service_lane_candidate",
      "source_intents": ["service_lane"], "role": "road_surface",
      "resolution_status": "UNRESOLVED_NEEDS_TILE_SURVEY", "confidence": "NONE_YET",
      "candidate_tiles": [], "notes": "" },
    { "candidate_family": "asphalt_parking_access_candidate",
      "source_intents": ["parking_access"], "role": "road_surface",
      "resolution_status": "UNRESOLVED_NEEDS_TILE_SURVEY", "confidence": "NONE_YET",
      "candidate_tiles": [], "notes": "" },
    { "candidate_family": "concrete_or_sidewalk_candidate",
      "source_intents": ["sidewalk_or_pedestrian_cut"], "role": "pedestrian_cut",
      "resolution_status": "UNRESOLVED_NEEDS_TILE_SURVEY", "confidence": "NONE_YET",
      "candidate_tiles": [], "notes": "" },
    { "candidate_family": "road_node_metadata_candidate",
      "source_intents": ["intersection_node", "road_turn_node", "dead_end_node"], "role": "road_node",
      "resolution_status": "UNRESOLVED_NEEDS_TILE_SURVEY", "confidence": "NONE_YET",
      "candidate_tiles": [], "notes": "" }
  ],
  "totals": { "family_count": 6, "resolved_family_count": 0, "unresolved_family_count": 6, "candidate_tile_count": 0 },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false,
                      "runtime_proven": false, "public_playable_claim": false }
}
""";
        var path = Path.Combine(_tempDir, "survey.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private string MakeFakePzRoot()
    {
        var pzRoot = Path.Combine(_tempDir, "fake_pz");
        Directory.CreateDirectory(Path.Combine(pzRoot, "media", "tiles"));
        File.WriteAllText(
            Path.Combine(pzRoot, "media", "tiles", "exterior.tiles"),
            "floors_exterior_street_asphalt_01_0\nfloors_concrete_sidewalk_curb_01_0\n",
            Encoding.UTF8);
        return pzRoot;
    }

    // -----------------------------------------------------------------------
    // CLI command: exit zero when PZ root missing
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_WhenPzRootMissing()
    {
        var input   = MakeSurveyJson();
        var outJson = Path.Combine(_tempDir, ".local", "local.json");
        var outSumm = Path.Combine(_tempDir, ".local", "local.summary.txt");

        var (code, stdout, stderr) = RunCli(
            "system2-build-static-road-local-tile-survey",
            "--input",   input,
            "--pz-root", Path.Combine(_tempDir, "no-such-pz"),
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
        var input   = MakeSurveyJson();
        var outJson = Path.Combine(_tempDir, ".local", "local.json");
        var outSumm = Path.Combine(_tempDir, ".local", "local.summary.txt");

        RunCli("system2-build-static-road-local-tile-survey",
            "--input", input, "--pz-root", Path.Combine(_tempDir, "no-pz"),
            "--output", outJson, "--summary", outSumm);

        Assert.True(File.Exists(outJson), "local.json not created");
    }

    [Fact]
    public void Cli_CreatesSummaryTxt()
    {
        var input   = MakeSurveyJson();
        var outJson = Path.Combine(_tempDir, ".local", "local.json");
        var outSumm = Path.Combine(_tempDir, ".local", "local.summary.txt");

        RunCli("system2-build-static-road-local-tile-survey",
            "--input", input, "--pz-root", Path.Combine(_tempDir, "no-pz"),
            "--output", outJson, "--summary", outSumm);

        Assert.True(File.Exists(outSumm), "local.summary.txt not created");
    }

    // -----------------------------------------------------------------------
    // CLI command: JSON status fields
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("LOCAL_TILE_SURVEY_ONLY")]
    [InlineData("NOT_RUNTIME_PROVEN")]
    [InlineData("NOT_IMPLEMENTED")]
    public void Cli_OutputJson_ContainsStatusField(string expected)
    {
        var input   = MakeSurveyJson();
        var outJson = Path.Combine(_tempDir, ".local", "local.json");
        var outSumm = Path.Combine(_tempDir, ".local", "local.summary.txt");

        RunCli("system2-build-static-road-local-tile-survey",
            "--input", input, "--pz-root", Path.Combine(_tempDir, "no-pz"),
            "--output", outJson, "--summary", outSumm);

        Assert.Contains(expected, File.ReadAllText(outJson), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CLI command: LOCAL_TEXT_MATCH_ONLY present when candidates found
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputJson_ContainsLocalTextMatchOnly_WhenCandidatesFound()
    {
        var input   = MakeSurveyJson();
        var pzRoot  = MakeFakePzRoot();
        var outJson = Path.Combine(_tempDir, ".local", "local.json");
        var outSumm = Path.Combine(_tempDir, ".local", "local.summary.txt");

        RunCli("system2-build-static-road-local-tile-survey",
            "--input", input, "--pz-root", pzRoot,
            "--output", outJson, "--summary", outSumm);

        Assert.Contains("LOCAL_TEXT_MATCH_ONLY", File.ReadAllText(outJson), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CLI command: missing args -> exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenRequiredArgsMissing()
    {
        var (code, _, stderr) = RunCli("system2-build-static-road-local-tile-survey");
        Assert.Equal(1, code);
        Assert.Contains("--input", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // CLI command: output not under .local -> exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var input   = MakeSurveyJson();
        var badOut  = Path.Combine(_tempDir, "local.json");
        var outSumm = Path.Combine(_tempDir, ".local", "local.summary.txt");

        var (code, _, stderr) = RunCli(
            "system2-build-static-road-local-tile-survey",
            "--input", input, "--pz-root", Path.Combine(_tempDir, "no-pz"),
            "--output", badOut, "--summary", outSumm);

        Assert.Equal(1, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Unknown command list
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommandError_MentionsLocalTileSurveyCommand()
    {
        var (code, _, stderr) = RunCli("not-a-real-command");
        Assert.Equal(1, code);
        Assert.Contains("system2-build-static-road-local-tile-survey", stderr, StringComparison.Ordinal);
    }
}

// -----------------------------------------------------------------------
// Helper script: static content + process tests
// -----------------------------------------------------------------------

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadLocalTileSurveyHelperScriptTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-system2-static-road-local-tile-survey.ps1");

    private static string LocalSurveyDir =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "system2-static-road-local-tile-survey");

    private static string LocalSurveyJson =>
        Path.Combine(LocalSurveyDir, "system2_static_road_local_tile_survey.json");

    private static string LocalSurveySummary =>
        Path.Combine(LocalSurveyDir, "system2_static_road_local_tile_survey.summary.txt");

    private static string DocPath =>
        Path.Combine(RepoRoot, "docs", "authoring", "DEADMTL_SYSTEM2_STATIC_ROAD_LOCAL_TILE_SURVEY.md");

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
    public void Readme_MentionsRunLocalTileSurveyScript() =>
        Assert.Contains("run-system2-static-road-local-tile-survey.ps1",
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
    public void HelperScript_CreatesLocalSurveyJson()
    {
        RunScript();
        Assert.True(File.Exists(LocalSurveyJson), $"Expected: {LocalSurveyJson}");
    }

    [Fact]
    public void HelperScript_CreatesLocalSurveySummaryTxt()
    {
        RunScript();
        Assert.True(File.Exists(LocalSurveySummary), $"Expected: {LocalSurveySummary}");
    }

    [Fact]
    public void HelperScript_FamilyCount_IsSix()
    {
        RunScript();
        using var doc = JsonDocument.Parse(File.ReadAllText(LocalSurveyJson));
        var familyCount = doc.RootElement.GetProperty("totals")
            .GetProperty("family_count").GetInt32();
        Assert.Equal(6, familyCount);
    }

    [Fact]
    public void HelperScript_DoesNotCreateWorldGenOverrideLua()
    {
        RunScript();
        Assert.False(File.Exists(Path.Combine(LocalSurveyDir, "WorldGenOverride.lua")),
            "WorldGenOverride.lua must not be written");
    }

    [Fact]
    public void HelperScript_DoesNotCreateLotpackFile()
    {
        RunScript();
        var lotpacks = Directory.Exists(LocalSurveyDir)
            ? Directory.GetFiles(LocalSurveyDir, "*.lotpack")
            : Array.Empty<string>();
        Assert.Empty(lotpacks);
    }
}
