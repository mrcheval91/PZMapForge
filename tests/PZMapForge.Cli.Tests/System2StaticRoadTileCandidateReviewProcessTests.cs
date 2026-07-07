using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadTileCandidateReviewProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-review-cli", Path.GetRandomFileName());

    public System2StaticRoadTileCandidateReviewProcessTests() => Directory.CreateDirectory(_tempDir);

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

    private string MakeShortlistJson()
    {
        var json = """
{
  "format": "pzmapforge.deadmtl.system2.static-road-tile-candidate-shortlist.v1",
  "status": "SHORTLIST_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_local_tile_survey": "test.json",
  "top_per_family": 25,
  "families": [
    {
      "candidate_family": "asphalt_road_surface_candidate",
      "role": "road_surface",
      "source_intents": ["local_street_asphalt"],
      "input_candidate_count": 50,
      "shortlisted_candidate_count": 2,
      "resolution_status": "SHORTLISTED_CANDIDATES_PRESENT",
      "confidence": "LOCAL_TEXT_MATCH_RANKED_ONLY",
      "candidates": [
        { "rank": 1, "tile_name": "floors_exterior_street_asphalt_01_0",
          "source_file": "media/tiles/exterior.tiles", "score": 88,
          "score_reasons": ["tile_name contains asphalt", "tile_name contains street"],
          "source_confidence": "LOCAL_TEXT_MATCH_ONLY",
          "confidence": "LOCAL_TEXT_MATCH_RANKED_ONLY" },
        { "rank": 2, "tile_name": "floors_exterior_street_asphalt_02_0",
          "source_file": "media/tiles/exterior.tiles", "score": 88,
          "score_reasons": ["tile_name contains asphalt", "tile_name contains street"],
          "source_confidence": "LOCAL_TEXT_MATCH_ONLY",
          "confidence": "LOCAL_TEXT_MATCH_RANKED_ONLY" }
      ],
      "notes": ""
    }
  ],
  "totals": { "family_count": 1, "input_candidate_count": 50, "shortlisted_candidate_count": 2,
              "families_with_shortlist": 1, "families_without_shortlist": 0 },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false,
                      "runtime_proven": false, "public_playable_claim": false }
}
""";
        var path = Path.Combine(_tempDir, "shortlist.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private (string Json, string Md, string Csv, string Summary) MakeOutputPaths() =>
    (
        Path.Combine(_tempDir, ".local", "review.json"),
        Path.Combine(_tempDir, ".local", "review.md"),
        Path.Combine(_tempDir, ".local", "review.csv"),
        Path.Combine(_tempDir, ".local", "review.summary.txt")
    );

    // -----------------------------------------------------------------------
    // CLI command: exit zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_OnFakeShortlistInput()
    {
        var input                   = MakeShortlistJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        var (code, stdout, stderr) = RunCli(
            "system2-build-static-road-tile-candidate-review",
            "--input",       input,
            "--output-json", outJson,
            "--output-md",   outMd,
            "--output-csv",  outCsv,
            "--summary",     outSumm);

        Assert.True(code == 0,
            $"Exit {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // CLI command: output files created
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesOutputJson()
    {
        var input                   = MakeShortlistJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        RunCli("system2-build-static-road-tile-candidate-review",
            "--input", input, "--output-json", outJson,
            "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);

        Assert.True(File.Exists(outJson), "review.json not created");
    }

    [Fact]
    public void Cli_CreatesOutputMd()
    {
        var input                   = MakeShortlistJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        RunCli("system2-build-static-road-tile-candidate-review",
            "--input", input, "--output-json", outJson,
            "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);

        Assert.True(File.Exists(outMd), "review.md not created");
    }

    [Fact]
    public void Cli_CreatesOutputCsv()
    {
        var input                   = MakeShortlistJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        RunCli("system2-build-static-road-tile-candidate-review",
            "--input", input, "--output-json", outJson,
            "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);

        Assert.True(File.Exists(outCsv), "review.csv not created");
    }

    [Fact]
    public void Cli_CreatesSummaryTxt()
    {
        var input                   = MakeShortlistJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        RunCli("system2-build-static-road-tile-candidate-review",
            "--input", input, "--output-json", outJson,
            "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);

        Assert.True(File.Exists(outSumm), "review.summary.txt not created");
    }

    // -----------------------------------------------------------------------
    // CLI command: JSON status fields
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("REVIEW_PACKET_ONLY")]
    [InlineData("NOT_RUNTIME_PROVEN")]
    [InlineData("NOT_IMPLEMENTED")]
    [InlineData("NEEDS_MANUAL_REVIEW")]
    public void Cli_OutputJson_ContainsStatusField(string expected)
    {
        var input                   = MakeShortlistJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        RunCli("system2-build-static-road-tile-candidate-review",
            "--input", input, "--output-json", outJson,
            "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);

        Assert.Contains(expected, File.ReadAllText(outJson), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Markdown content checks
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputMd_ContainsTitle()
    {
        var input                   = MakeShortlistJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        RunCli("system2-build-static-road-tile-candidate-review",
            "--input", input, "--output-json", outJson,
            "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);

        Assert.Contains("MAP-22L", File.ReadAllText(outMd), StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputMd_ContainsVerdict()
    {
        var input                   = MakeShortlistJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        RunCli("system2-build-static-road-tile-candidate-review",
            "--input", input, "--output-json", outJson,
            "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);

        Assert.Contains("MAP22L_SYSTEM2_STATIC_ROAD_TILE_CANDIDATE_REVIEW_PACKET_COMPLETE",
            File.ReadAllText(outMd), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV content check
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputCsv_ContainsHeader()
    {
        var input                   = MakeShortlistJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        RunCli("system2-build-static-road-tile-candidate-review",
            "--input", input, "--output-json", outJson,
            "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);

        Assert.Contains("candidate_family,role,rank,tile_name,source_file,score,score_reasons,review_status,recommended_next_action,confidence",
            File.ReadAllText(outCsv), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CLI command: missing args -> exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenRequiredArgsMissing()
    {
        var (code, _, stderr) = RunCli("system2-build-static-road-tile-candidate-review");
        Assert.Equal(1, code);
        Assert.Contains("--input", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // CLI command: output not under .local -> exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var input   = MakeShortlistJson();
        var badJson = Path.Combine(_tempDir, "review.json");
        var (_, outMd, outCsv, outSumm) = MakeOutputPaths();

        var (code, _, stderr) = RunCli(
            "system2-build-static-road-tile-candidate-review",
            "--input", input, "--output-json", badJson,
            "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);

        Assert.Equal(1, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Unknown command list
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommandError_MentionsReviewCommand()
    {
        var (code, _, stderr) = RunCli("not-a-real-command");
        Assert.Equal(1, code);
        Assert.Contains("system2-build-static-road-tile-candidate-review", stderr, StringComparison.Ordinal);
    }
}

// -----------------------------------------------------------------------
// Helper script: static content + process tests
// -----------------------------------------------------------------------

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadTileCandidateReviewHelperScriptTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-system2-static-road-tile-candidate-review.ps1");

    private static string ReviewDir =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "system2-static-road-tile-candidate-review");

    private static string ReviewJson =>
        Path.Combine(ReviewDir, "system2_static_road_tile_candidate_review.json");

    private static string ReviewMd =>
        Path.Combine(ReviewDir, "system2_static_road_tile_candidate_review.md");

    private static string ReviewCsv =>
        Path.Combine(ReviewDir, "system2_static_road_tile_candidate_review.csv");

    private static string ReviewSummary =>
        Path.Combine(ReviewDir, "system2_static_road_tile_candidate_review.summary.txt");

    private static string DocPath =>
        Path.Combine(RepoRoot, "docs", "authoring", "DEADMTL_SYSTEM2_STATIC_ROAD_TILE_CANDIDATE_REVIEW.md");

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
    public void Readme_MentionsRunReviewScript() =>
        Assert.Contains("run-system2-static-road-tile-candidate-review.ps1",
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
    public void HelperScript_CreatesReviewJson()
    {
        RunScript();
        Assert.True(File.Exists(ReviewJson), $"Expected: {ReviewJson}");
    }

    [Fact]
    public void HelperScript_CreatesReviewMd()
    {
        RunScript();
        Assert.True(File.Exists(ReviewMd), $"Expected: {ReviewMd}");
    }

    [Fact]
    public void HelperScript_CreatesReviewCsv()
    {
        RunScript();
        Assert.True(File.Exists(ReviewCsv), $"Expected: {ReviewCsv}");
    }

    [Fact]
    public void HelperScript_CreatesReviewSummaryTxt()
    {
        RunScript();
        Assert.True(File.Exists(ReviewSummary), $"Expected: {ReviewSummary}");
    }

    [Fact]
    public void HelperScript_DoesNotCreateWorldGenOverrideLua()
    {
        RunScript();
        Assert.False(File.Exists(Path.Combine(ReviewDir, "WorldGenOverride.lua")),
            "WorldGenOverride.lua must not be written");
    }

    [Fact]
    public void HelperScript_DoesNotCreateLotpackFile()
    {
        RunScript();
        var lotpacks = Directory.Exists(ReviewDir)
            ? Directory.GetFiles(ReviewDir, "*.lotpack")
            : Array.Empty<string>();
        Assert.Empty(lotpacks);
    }
}
