using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadHumanApprovedTileCandidatesProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-manifest-cli", Path.GetRandomFileName());

    public System2StaticRoadHumanApprovedTileCandidatesProcessTests() => Directory.CreateDirectory(_tempDir);

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

    private string MakeAppliedReviewJson()
    {
        var json = """
{
  "format": "pzmapforge.deadmtl.system2.static-road-tile-candidate-review-applied.v1",
  "status": "HUMAN_REVIEW_APPLIED_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_review_json": "test.json",
  "source_decisions_csv": "test.csv",
  "families": [
    {
      "candidate_family": "asphalt_road_surface_candidate",
      "role": "road_surface",
      "review_items": [
        {
          "rank": 1,
          "tile_name": "floors_exterior_street_asphalt_01_0",
          "source_file": "media/tiles/exterior.tiles",
          "score": 88,
          "score_reasons": ["tile_name contains asphalt"],
          "review_status": "NEEDS_MANUAL_REVIEW",
          "human_note": "",
          "recommended_next_action": "Inspect in TileZed or tile sheet preview before writer use.",
          "confidence": "LOCAL_TEXT_MATCH_RANKED_ONLY"
        }
      ]
    }
  ],
  "totals": { "family_count": 1, "review_item_count": 1, "needs_manual_review_count": 1,
              "approved_count": 0, "rejected_count": 0, "applied_decision_count": 0, "unknown_decision_count": 0 },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false,
                      "runtime_proven": false, "public_playable_claim": false, "writer_ready_claim": false }
}
""";
        var path = Path.Combine(_tempDir, "applied.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private (string Json, string Md, string Csv, string Summary) MakeOutputPaths() =>
    (
        Path.Combine(_tempDir, ".local", "manifest.json"),
        Path.Combine(_tempDir, ".local", "manifest.md"),
        Path.Combine(_tempDir, ".local", "manifest.csv"),
        Path.Combine(_tempDir, ".local", "manifest.summary.txt")
    );

    private void RunCommand(string input, string outJson, string outMd, string outCsv, string outSumm) =>
        RunCli("system2-build-static-road-human-approved-tile-candidates",
            "--input",       input,
            "--output-json", outJson,
            "--output-md",   outMd,
            "--output-csv",  outCsv,
            "--summary",     outSumm);

    // -----------------------------------------------------------------------
    // Exit zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_OnFakeAppliedReview()
    {
        var input                           = MakeAppliedReviewJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        var (code, stdout, stderr) = RunCli(
            "system2-build-static-road-human-approved-tile-candidates",
            "--input", input, "--output-json", outJson,
            "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);

        Assert.True(code == 0, $"Exit {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // Output files created
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesOutputJson()
    {
        var input                           = MakeAppliedReviewJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outJson), "manifest.json not created");
    }

    [Fact]
    public void Cli_CreatesOutputMd()
    {
        var input                           = MakeAppliedReviewJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outMd), "manifest.md not created");
    }

    [Fact]
    public void Cli_CreatesOutputCsv()
    {
        var input                           = MakeAppliedReviewJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outCsv), "manifest.csv not created");
    }

    [Fact]
    public void Cli_CreatesSummaryTxt()
    {
        var input                           = MakeAppliedReviewJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outSumm), "manifest.summary.txt not created");
    }

    // -----------------------------------------------------------------------
    // JSON status fields
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("HUMAN_APPROVED_CANDIDATE_MANIFEST_ONLY")]
    [InlineData("NOT_RUNTIME_PROVEN")]
    [InlineData("NOT_IMPLEMENTED")]
    public void Cli_OutputJson_ContainsStatusField(string expected)
    {
        var input                           = MakeAppliedReviewJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.Contains(expected, File.ReadAllText(outJson), StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputJson_ContainsWriterReadyClaimFalse()
    {
        var input                           = MakeAppliedReviewJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.Contains("writer_ready_claim", File.ReadAllText(outJson), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Markdown content
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputMd_ContainsMap22NTitle()
    {
        var input                           = MakeAppliedReviewJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.Contains("MAP-22N", File.ReadAllText(outMd), StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputMd_ContainsVerdict()
    {
        var input                           = MakeAppliedReviewJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.Contains("MAP22N_SYSTEM2_STATIC_ROAD_HUMAN_APPROVED_TILE_CANDIDATES_COMPLETE",
            File.ReadAllText(outMd), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV header
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputCsv_ContainsRequiredHeader()
    {
        var input                           = MakeAppliedReviewJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.Contains(
            "bucket,candidate_family,role,rank,tile_name,source_file,score,score_reasons," +
            "human_note,review_status,confidence,recommended_next_action",
            File.ReadAllText(outCsv), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Missing args → exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenRequiredArgsMissing()
    {
        var (code, _, stderr) = RunCli("system2-build-static-road-human-approved-tile-candidates");
        Assert.Equal(1, code);
        Assert.Contains("--input", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Output not under .local → exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var input   = MakeAppliedReviewJson();
        var badJson = Path.Combine(_tempDir, "manifest.json");
        var (_, outMd, outCsv, outSumm) = MakeOutputPaths();

        var (code, _, stderr) = RunCli(
            "system2-build-static-road-human-approved-tile-candidates",
            "--input", input, "--output-json", badJson,
            "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);

        Assert.Equal(1, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Unknown command mentions the new command
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommandError_MentionsManifestCommand()
    {
        var (code, _, stderr) = RunCli("not-a-real-command");
        Assert.Equal(1, code);
        Assert.Contains("system2-build-static-road-human-approved-tile-candidates", stderr, StringComparison.Ordinal);
    }
}

// -----------------------------------------------------------------------
// Helper script: static + process tests
// -----------------------------------------------------------------------

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadHumanApprovedTileCandidatesHelperScriptTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-system2-static-road-human-approved-tile-candidates.ps1");

    private static string ManifestDir =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring",
            "system2-static-road-human-approved-tile-candidates");

    private static string ManifestJson =>
        Path.Combine(ManifestDir, "system2_static_road_human_approved_tile_candidates.json");

    private static string ManifestMd =>
        Path.Combine(ManifestDir, "system2_static_road_human_approved_tile_candidates.md");

    private static string ManifestCsv =>
        Path.Combine(ManifestDir, "system2_static_road_human_approved_tile_candidates.csv");

    private static string ManifestSummary =>
        Path.Combine(ManifestDir, "system2_static_road_human_approved_tile_candidates.summary.txt");

    private static string DocPath =>
        Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_SYSTEM2_STATIC_ROAD_HUMAN_APPROVED_TILE_CANDIDATES.md");

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
    public void Doc_StatesHumanApprovalIsNotWriterReady()
    {
        var text = File.ReadAllText(DocPath);
        Assert.True(
            text.Contains("writer-ready", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("writer ready", StringComparison.OrdinalIgnoreCase),
            "doc should state human approval is not writer-ready");
    }

    [Fact]
    public void Readme_MentionsManifestScript() =>
        Assert.Contains("run-system2-static-road-human-approved-tile-candidates.ps1",
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
    public void HelperScript_CreatesManifestJson()
    {
        RunScript();
        Assert.True(File.Exists(ManifestJson), $"Expected: {ManifestJson}");
    }

    [Fact]
    public void HelperScript_CreatesManifestMd()
    {
        RunScript();
        Assert.True(File.Exists(ManifestMd), $"Expected: {ManifestMd}");
    }

    [Fact]
    public void HelperScript_CreatesManifestCsv()
    {
        RunScript();
        Assert.True(File.Exists(ManifestCsv), $"Expected: {ManifestCsv}");
    }

    [Fact]
    public void HelperScript_CreatesManifestSummaryTxt()
    {
        RunScript();
        Assert.True(File.Exists(ManifestSummary), $"Expected: {ManifestSummary}");
    }

    [Fact]
    public void HelperScript_DoesNotCreateWorldGenOverrideLua()
    {
        RunScript();
        Assert.False(File.Exists(Path.Combine(ManifestDir, "WorldGenOverride.lua")),
            "WorldGenOverride.lua must not be written");
    }

    [Fact]
    public void HelperScript_DoesNotCreateLotpackFile()
    {
        RunScript();
        var lotpacks = Directory.Exists(ManifestDir)
            ? Directory.GetFiles(ManifestDir, "*.lotpack")
            : Array.Empty<string>();
        Assert.Empty(lotpacks);
    }
}
