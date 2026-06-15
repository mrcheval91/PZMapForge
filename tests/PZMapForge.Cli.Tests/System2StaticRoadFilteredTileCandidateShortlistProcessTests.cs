using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadFilteredTileCandidateShortlistProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-filtered-shortlist-cli", Path.GetRandomFileName());

    public System2StaticRoadFilteredTileCandidateShortlistProcessTests() =>
        Directory.CreateDirectory(_tempDir);

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

    private string MakeFilteredSurveyJson()
    {
        var json = """
{
  "format": "pzmapforge.deadmtl.system2.static-road-local-tile-survey-filtered.v1",
  "families": [
    {
      "candidate_family": "asphalt_road_surface_candidate",
      "role": "road_surface",
      "resolution_status": "FILTERED_SURVEY_CANDIDATES_FOUND",
      "confidence": "LOCAL_FILTERED_TEXT_MATCH_ONLY",
      "search_terms": ["asphalt", "road"],
      "candidate_tiles": [
        { "tile_name": "asphalt_road_01", "source_file": "media/tiles/exterior.tiles",
          "match_reason": "matched term: asphalt", "confidence": "LOCAL_FILTERED_TEXT_MATCH_ONLY" }
      ]
    }
  ]
}
""";
        var path = Path.Combine(_tempDir, "filtered_survey.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private (string Json, string Md, string Csv, string Summary) MakeOutputPaths() =>
    (
        Path.Combine(_tempDir, ".local", "shortlist.json"),
        Path.Combine(_tempDir, ".local", "shortlist.md"),
        Path.Combine(_tempDir, ".local", "shortlist.csv"),
        Path.Combine(_tempDir, ".local", "shortlist.summary.txt")
    );

    private void RunCommand(string input, string outJson, string outMd, string outCsv, string outSumm) =>
        RunCli("system2-build-static-road-filtered-tile-candidate-shortlist",
            "--input",       input,
            "--output-json", outJson,
            "--output-md",   outMd,
            "--output-csv",  outCsv,
            "--summary",     outSumm);

    // -----------------------------------------------------------------------
    // Exit zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_OnFakeFilteredSurvey()
    {
        var input                              = MakeFilteredSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        var (code, stdout, stderr) = RunCli(
            "system2-build-static-road-filtered-tile-candidate-shortlist",
            "--input",       input,
            "--output-json", outJson,
            "--output-md",   outMd,
            "--output-csv",  outCsv,
            "--summary",     outSumm);

        Assert.True(code == 0, $"Exit {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // Output files created
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesOutputJson()
    {
        var input                              = MakeFilteredSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outJson), "shortlist.json not created");
    }

    [Fact]
    public void Cli_CreatesOutputMd()
    {
        var input                              = MakeFilteredSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outMd), "shortlist.md not created");
    }

    [Fact]
    public void Cli_CreatesOutputCsv()
    {
        var input                              = MakeFilteredSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outCsv), "shortlist.csv not created");
    }

    [Fact]
    public void Cli_CreatesSummaryTxt()
    {
        var input                              = MakeFilteredSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outSumm), "shortlist.summary.txt not created");
    }

    // -----------------------------------------------------------------------
    // JSON status fields
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("FILTERED_SHORTLIST_ONLY")]
    [InlineData("NOT_RUNTIME_PROVEN")]
    [InlineData("NOT_IMPLEMENTED")]
    [InlineData("LOCAL_FILTERED_TEXT_MATCH_RANKED_ONLY")]
    public void Cli_OutputJson_ContainsStatusField(string expected)
    {
        var input                              = MakeFilteredSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.Contains(expected, File.ReadAllText(outJson), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Markdown content
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputMd_ContainsMap22PTitle()
    {
        var input                              = MakeFilteredSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.Contains("MAP-22P", File.ReadAllText(outMd), StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputMd_ContainsVerdict()
    {
        var input                              = MakeFilteredSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.Contains("MAP22P_SYSTEM2_STATIC_ROAD_FILTERED_TILE_CANDIDATE_SHORTLIST_COMPLETE",
            File.ReadAllText(outMd), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV header
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputCsv_ContainsRequiredHeader()
    {
        var input                              = MakeFilteredSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.Contains(
            "candidate_family,role,rank,tile_name,source_file,score,score_reasons," +
            "source_confidence,confidence,resolution_status",
            File.ReadAllText(outCsv), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Missing args → exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenRequiredArgsMissing()
    {
        var (code, _, stderr) = RunCli("system2-build-static-road-filtered-tile-candidate-shortlist");
        Assert.Equal(1, code);
        Assert.Contains("--input", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Output not under .local → exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var input   = MakeFilteredSurveyJson();
        var badJson = Path.Combine(_tempDir, "shortlist.json");
        var (_, outMd, outCsv, outSumm) = MakeOutputPaths();

        var (code, _, stderr) = RunCli(
            "system2-build-static-road-filtered-tile-candidate-shortlist",
            "--input",       input,
            "--output-json", badJson,
            "--output-md",   outMd,
            "--output-csv",  outCsv,
            "--summary",     outSumm);

        Assert.Equal(1, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Unknown command mentions new command
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommandError_MentionsFilteredShortlistCommand()
    {
        var (code, _, stderr) = RunCli("not-a-real-command");
        Assert.Equal(1, code);
        Assert.Contains("system2-build-static-road-filtered-tile-candidate-shortlist",
            stderr, StringComparison.Ordinal);
    }
}

// -----------------------------------------------------------------------
// Helper script: static + process tests
// -----------------------------------------------------------------------

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadFilteredTileCandidateShortlistHelperScriptTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-system2-static-road-filtered-tile-candidate-shortlist.ps1");

    private static string OutDir =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring",
            "system2-static-road-filtered-tile-candidate-shortlist");

    private static string OutJson =>
        Path.Combine(OutDir, "system2_static_road_filtered_tile_candidate_shortlist.json");

    private static string OutMd =>
        Path.Combine(OutDir, "system2_static_road_filtered_tile_candidate_shortlist.md");

    private static string OutCsv =>
        Path.Combine(OutDir, "system2_static_road_filtered_tile_candidate_shortlist.csv");

    private static string OutSummary =>
        Path.Combine(OutDir, "system2_static_road_filtered_tile_candidate_shortlist.summary.txt");

    private static string DocPath =>
        Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_SYSTEM2_STATIC_ROAD_FILTERED_TILE_CANDIDATE_SHORTLIST.md");

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
    // Static: doc
    // -----------------------------------------------------------------------

    [Fact]
    public void Doc_MentionsNoRuntimeProof()
    {
        var text = File.ReadAllText(DocPath);
        Assert.True(
            text.Contains("NOT_RUNTIME_PROVEN", StringComparison.Ordinal) ||
            text.Contains("Runtime proof is NOT claimed", StringComparison.OrdinalIgnoreCase),
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
    public void Doc_StatesFilteredShortlistIsNotWriterReady()
    {
        var text = File.ReadAllText(DocPath);
        Assert.True(
            text.Contains("writer-ready", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("writer ready", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("not writer readiness", StringComparison.OrdinalIgnoreCase),
            "doc should state filtered shortlist is not writer-ready");
    }

    [Fact]
    public void Readme_MentionsFilteredShortlistScript() =>
        Assert.Contains("run-system2-static-road-filtered-tile-candidate-shortlist.ps1",
            File.ReadAllText(ReadmePath), StringComparison.Ordinal);

    // -----------------------------------------------------------------------
    // Process
    // -----------------------------------------------------------------------

    [Fact]
    public void HelperScript_ExitsZero()
    {
        var (code, stdout, stderr) = RunScript();
        Assert.True(code == 0,
            $"Script exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void HelperScript_CreatesOutJson()
    {
        RunScript();
        Assert.True(File.Exists(OutJson), $"Expected: {OutJson}");
    }

    [Fact]
    public void HelperScript_CreatesOutMd()
    {
        RunScript();
        Assert.True(File.Exists(OutMd), $"Expected: {OutMd}");
    }

    [Fact]
    public void HelperScript_CreatesOutCsv()
    {
        RunScript();
        Assert.True(File.Exists(OutCsv), $"Expected: {OutCsv}");
    }

    [Fact]
    public void HelperScript_CreatesOutSummary()
    {
        RunScript();
        Assert.True(File.Exists(OutSummary), $"Expected: {OutSummary}");
    }

    [Fact]
    public void HelperScript_DoesNotCreateWorldGenOverrideLua()
    {
        RunScript();
        Assert.False(File.Exists(Path.Combine(OutDir, "WorldGenOverride.lua")),
            "WorldGenOverride.lua must not be written");
    }

    [Fact]
    public void HelperScript_DoesNotCreateLotpackFile()
    {
        RunScript();
        var lotpacks = Directory.Exists(OutDir)
            ? Directory.GetFiles(OutDir, "*.lotpack")
            : Array.Empty<string>();
        Assert.Empty(lotpacks);
    }
}
