using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadTileCandidateReviewApplyProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-apply-cli", Path.GetRandomFileName());

    public System2StaticRoadTileCandidateReviewApplyProcessTests() => Directory.CreateDirectory(_tempDir);

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

    private string MakeReviewJson()
    {
        var json = """
{
  "format": "pzmapforge.deadmtl.system2.static-road-tile-candidate-review.v1",
  "status": "REVIEW_PACKET_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_shortlist": "test.json",
  "families": [
    {
      "candidate_family": "asphalt_road_surface_candidate",
      "role": "road_surface",
      "input_candidate_count": 50,
      "shortlisted_candidate_count": 2,
      "review_items": [
        {
          "rank": 1,
          "tile_name": "floors_exterior_street_asphalt_01_0",
          "source_file": "media/tiles/exterior.tiles",
          "score": 88,
          "score_reasons": ["tile_name contains asphalt", "tile_name contains street"],
          "review_status": "NEEDS_MANUAL_REVIEW",
          "recommended_next_action": "Inspect in TileZed or tile sheet preview before writer use.",
          "confidence": "LOCAL_TEXT_MATCH_RANKED_ONLY"
        },
        {
          "rank": 2,
          "tile_name": "floors_exterior_street_asphalt_02_0",
          "source_file": "media/tiles/exterior.tiles",
          "score": 88,
          "score_reasons": ["tile_name contains asphalt"],
          "review_status": "NEEDS_MANUAL_REVIEW",
          "recommended_next_action": "Inspect in TileZed or tile sheet preview before writer use.",
          "confidence": "LOCAL_TEXT_MATCH_RANKED_ONLY"
        }
      ]
    }
  ],
  "totals": { "family_count": 1, "review_item_count": 2, "needs_manual_review_count": 2,
              "approved_count": 0, "rejected_count": 0 },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false,
                      "runtime_proven": false, "public_playable_claim": false }
}
""";
        var path = Path.Combine(_tempDir, "review.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private string MakeUneditedCsv()
    {
        var csv =
            "candidate_family,role,rank,tile_name,source_file,score,score_reasons,review_status,recommended_next_action,confidence\n" +
            "asphalt_road_surface_candidate,road_surface,1,floors_exterior_street_asphalt_01_0,media/tiles/exterior.tiles,88,reasons,NEEDS_MANUAL_REVIEW,action,LOCAL_TEXT_MATCH_RANKED_ONLY\n" +
            "asphalt_road_surface_candidate,road_surface,2,floors_exterior_street_asphalt_02_0,media/tiles/exterior.tiles,88,reasons,NEEDS_MANUAL_REVIEW,action,LOCAL_TEXT_MATCH_RANKED_ONLY\n";
        var path = Path.Combine(_tempDir, "decisions.csv");
        File.WriteAllText(path, csv, Encoding.UTF8);
        return path;
    }

    private string MakeApprovalCsv()
    {
        var csv =
            "candidate_family,role,rank,tile_name,source_file,score,score_reasons,review_status,recommended_next_action,confidence,human_note\n" +
            "asphalt_road_surface_candidate,road_surface,1,floors_exterior_street_asphalt_01_0,media/tiles/exterior.tiles,88,reasons,APPROVED_BY_HUMAN_REVIEW,action,LOCAL_TEXT_MATCH_RANKED_ONLY,Correct asphalt surface\n" +
            "asphalt_road_surface_candidate,road_surface,2,floors_exterior_street_asphalt_02_0,media/tiles/exterior.tiles,88,reasons,NEEDS_MANUAL_REVIEW,action,LOCAL_TEXT_MATCH_RANKED_ONLY,\n";
        var path = Path.Combine(_tempDir, "decisions_approval.csv");
        File.WriteAllText(path, csv, Encoding.UTF8);
        return path;
    }

    private (string Json, string Md, string Csv, string Summary) MakeOutputPaths() =>
    (
        Path.Combine(_tempDir, ".local", "applied.json"),
        Path.Combine(_tempDir, ".local", "applied.md"),
        Path.Combine(_tempDir, ".local", "applied.csv"),
        Path.Combine(_tempDir, ".local", "applied.summary.txt")
    );

    // -----------------------------------------------------------------------
    // CLI: exit zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_OnFakeReviewJsonAndDecisionsCsv()
    {
        var reviewJson               = MakeReviewJson();
        var decisionsCsv             = MakeUneditedCsv();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        var (code, stdout, stderr) = RunCli(
            "system2-apply-static-road-tile-candidate-review",
            "--review-json",   reviewJson,
            "--decisions-csv", decisionsCsv,
            "--output-json",   outJson,
            "--output-md",     outMd,
            "--output-csv",    outCsv,
            "--summary",       outSumm);

        Assert.True(code == 0, $"Exit {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // CLI: output files created
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesOutputJson()
    {
        var reviewJson               = MakeReviewJson();
        var decisionsCsv             = MakeUneditedCsv();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        RunCli("system2-apply-static-road-tile-candidate-review",
            "--review-json", reviewJson, "--decisions-csv", decisionsCsv,
            "--output-json", outJson, "--output-md", outMd,
            "--output-csv",  outCsv, "--summary", outSumm);

        Assert.True(File.Exists(outJson), "applied.json not created");
    }

    [Fact]
    public void Cli_CreatesOutputMd()
    {
        var reviewJson               = MakeReviewJson();
        var decisionsCsv             = MakeUneditedCsv();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        RunCli("system2-apply-static-road-tile-candidate-review",
            "--review-json", reviewJson, "--decisions-csv", decisionsCsv,
            "--output-json", outJson, "--output-md", outMd,
            "--output-csv",  outCsv, "--summary", outSumm);

        Assert.True(File.Exists(outMd), "applied.md not created");
    }

    [Fact]
    public void Cli_CreatesOutputCsv()
    {
        var reviewJson               = MakeReviewJson();
        var decisionsCsv             = MakeUneditedCsv();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        RunCli("system2-apply-static-road-tile-candidate-review",
            "--review-json", reviewJson, "--decisions-csv", decisionsCsv,
            "--output-json", outJson, "--output-md", outMd,
            "--output-csv",  outCsv, "--summary", outSumm);

        Assert.True(File.Exists(outCsv), "applied.csv not created");
    }

    [Fact]
    public void Cli_CreatesSummaryTxt()
    {
        var reviewJson               = MakeReviewJson();
        var decisionsCsv             = MakeUneditedCsv();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        RunCli("system2-apply-static-road-tile-candidate-review",
            "--review-json", reviewJson, "--decisions-csv", decisionsCsv,
            "--output-json", outJson, "--output-md", outMd,
            "--output-csv",  outCsv, "--summary", outSumm);

        Assert.True(File.Exists(outSumm), "applied.summary.txt not created");
    }

    // -----------------------------------------------------------------------
    // CLI: JSON status fields
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("HUMAN_REVIEW_APPLIED_ONLY")]
    [InlineData("NOT_RUNTIME_PROVEN")]
    [InlineData("NOT_IMPLEMENTED")]
    public void Cli_OutputJson_ContainsStatusField(string expected)
    {
        var reviewJson               = MakeReviewJson();
        var decisionsCsv             = MakeUneditedCsv();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        RunCli("system2-apply-static-road-tile-candidate-review",
            "--review-json", reviewJson, "--decisions-csv", decisionsCsv,
            "--output-json", outJson, "--output-md", outMd,
            "--output-csv",  outCsv, "--summary", outSumm);

        Assert.Contains(expected, File.ReadAllText(outJson), StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputJson_ContainsHumanReviewConfidence_WhenApprovalExists()
    {
        var reviewJson               = MakeReviewJson();
        var decisionsCsv             = MakeApprovalCsv();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        RunCli("system2-apply-static-road-tile-candidate-review",
            "--review-json", reviewJson, "--decisions-csv", decisionsCsv,
            "--output-json", outJson, "--output-md", outMd,
            "--output-csv",  outCsv, "--summary", outSumm);

        Assert.Contains("HUMAN_REVIEW_ONLY_NOT_RUNTIME_PROVEN",
            File.ReadAllText(outJson), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CLI: Markdown content
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputMd_ContainsMap22MTitle()
    {
        var reviewJson               = MakeReviewJson();
        var decisionsCsv             = MakeUneditedCsv();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        RunCli("system2-apply-static-road-tile-candidate-review",
            "--review-json", reviewJson, "--decisions-csv", decisionsCsv,
            "--output-json", outJson, "--output-md", outMd,
            "--output-csv",  outCsv, "--summary", outSumm);

        Assert.Contains("MAP-22M", File.ReadAllText(outMd), StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputMd_ContainsVerdict()
    {
        var reviewJson               = MakeReviewJson();
        var decisionsCsv             = MakeUneditedCsv();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        RunCli("system2-apply-static-road-tile-candidate-review",
            "--review-json", reviewJson, "--decisions-csv", decisionsCsv,
            "--output-json", outJson, "--output-md", outMd,
            "--output-csv",  outCsv, "--summary", outSumm);

        Assert.Contains("MAP22M_SYSTEM2_STATIC_ROAD_TILE_CANDIDATE_REVIEW_APPLY_COMPLETE",
            File.ReadAllText(outMd), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CLI: CSV content
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputCsv_ContainsRequiredHeader()
    {
        var reviewJson               = MakeReviewJson();
        var decisionsCsv             = MakeUneditedCsv();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        RunCli("system2-apply-static-road-tile-candidate-review",
            "--review-json", reviewJson, "--decisions-csv", decisionsCsv,
            "--output-json", outJson, "--output-md", outMd,
            "--output-csv",  outCsv, "--summary", outSumm);

        Assert.Contains(
            "candidate_family,role,rank,tile_name,source_file,score,score_reasons," +
            "review_status,human_note,recommended_next_action,confidence",
            File.ReadAllText(outCsv), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CLI: missing args → exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenRequiredArgsMissing()
    {
        var (code, _, stderr) = RunCli("system2-apply-static-road-tile-candidate-review");
        Assert.Equal(1, code);
        Assert.Contains("--review-json", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // CLI: output not under .local → exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var reviewJson   = MakeReviewJson();
        var decisionsCsv = MakeUneditedCsv();
        var badJson      = Path.Combine(_tempDir, "applied.json");
        var (_, outMd, outCsv, outSumm) = MakeOutputPaths();

        var (code, _, stderr) = RunCli(
            "system2-apply-static-road-tile-candidate-review",
            "--review-json", reviewJson, "--decisions-csv", decisionsCsv,
            "--output-json", badJson, "--output-md", outMd,
            "--output-csv",  outCsv,  "--summary",   outSumm);

        Assert.Equal(1, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Unknown command mentions the apply command
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommandError_MentionsApplyCommand()
    {
        var (code, _, stderr) = RunCli("not-a-real-command");
        Assert.Equal(1, code);
        Assert.Contains("system2-apply-static-road-tile-candidate-review", stderr, StringComparison.Ordinal);
    }
}

// -----------------------------------------------------------------------
// Helper script: static content + process tests
// -----------------------------------------------------------------------

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadTileCandidateReviewApplyHelperScriptTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-system2-static-road-tile-candidate-review-apply.ps1");

    private static string ApplyDir =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring",
            "system2-static-road-tile-candidate-review-applied");

    private static string AppliedJson =>
        Path.Combine(ApplyDir, "system2_static_road_tile_candidate_review_applied.json");

    private static string AppliedMd =>
        Path.Combine(ApplyDir, "system2_static_road_tile_candidate_review_applied.md");

    private static string AppliedCsv =>
        Path.Combine(ApplyDir, "system2_static_road_tile_candidate_review_applied.csv");

    private static string AppliedSummary =>
        Path.Combine(ApplyDir, "system2_static_road_tile_candidate_review_applied.summary.txt");

    private static string DocPath =>
        Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_SYSTEM2_STATIC_ROAD_TILE_CANDIDATE_REVIEW_APPLY.md");

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
    // Static: forbidden strings in script
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
    // Static: doc checks
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
    public void Doc_StatesHumanApprovalIsNotWriterReady()
    {
        var text = File.ReadAllText(DocPath);
        Assert.True(
            text.Contains("writer-ready", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("writer ready", StringComparison.OrdinalIgnoreCase),
            "doc should state human approval is not writer-ready");
    }

    [Fact]
    public void Readme_MentionsApplyScript() =>
        Assert.Contains("run-system2-static-road-tile-candidate-review-apply.ps1",
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
    public void HelperScript_CreatesAppliedJson()
    {
        RunScript();
        Assert.True(File.Exists(AppliedJson), $"Expected: {AppliedJson}");
    }

    [Fact]
    public void HelperScript_CreatesAppliedMd()
    {
        RunScript();
        Assert.True(File.Exists(AppliedMd), $"Expected: {AppliedMd}");
    }

    [Fact]
    public void HelperScript_CreatesAppliedCsv()
    {
        RunScript();
        Assert.True(File.Exists(AppliedCsv), $"Expected: {AppliedCsv}");
    }

    [Fact]
    public void HelperScript_CreatesAppliedSummaryTxt()
    {
        RunScript();
        Assert.True(File.Exists(AppliedSummary), $"Expected: {AppliedSummary}");
    }

    [Fact]
    public void HelperScript_DoesNotCreateWorldGenOverrideLua()
    {
        RunScript();
        Assert.False(File.Exists(Path.Combine(ApplyDir, "WorldGenOverride.lua")),
            "WorldGenOverride.lua must not be written");
    }

    [Fact]
    public void HelperScript_DoesNotCreateLotpackFile()
    {
        RunScript();
        var lotpacks = Directory.Exists(ApplyDir)
            ? Directory.GetFiles(ApplyDir, "*.lotpack")
            : Array.Empty<string>();
        Assert.Empty(lotpacks);
    }
}
