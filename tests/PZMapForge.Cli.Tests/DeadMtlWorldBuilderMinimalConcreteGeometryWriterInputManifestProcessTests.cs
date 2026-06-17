using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-writer-input-manifest-cli", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestProcessTests() =>
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

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-input-manifest.ps1");

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string WriteMvpJson()
    {
        var path = Path.Combine(_tempDir, "mvp.json");
        File.WriteAllText(path,
            "{\"format\":\"pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-mvp.v1\"," +
            "\"tile_id\":\"map_00\"," +
            "\"verdict\":\"MAP26A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_MVP_COMPLETE\"}");
        return path;
    }

    private string WriteOverlayJson()
    {
        var path = Path.Combine(_tempDir, "overlay.json");
        File.WriteAllText(path,
            "{\"format\":\"pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-qa-overlay.v1\"," +
            "\"map_id\":\"map_00\"," +
            "\"verdict\":\"MAP26B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_OVERLAY_COMPLETE\"}");
        return path;
    }

    private string WriteOverlayCsv()
    {
        var path = Path.Combine(_tempDir, "overlay.csv");
        File.WriteAllText(path, "feature_order,feature_kind,feature_id\n1,COMPONENT_BBOX,map_00_component_0001\n");
        return path;
    }

    private string WriteOverlayPng()
    {
        var path = Path.Combine(_tempDir, "overlay.png");
        File.WriteAllBytes(path, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        return path;
    }

    private string WriteReviewJson()
    {
        var path = Path.Combine(_tempDir, "review.json");
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"format\": \"pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-qa-review-packet.v1\",");
        sb.AppendLine("  \"map_id\": \"map_00\",");
        sb.AppendLine("  \"target_component_id\": \"map_00_component_0001\",");
        sb.AppendLine("  \"target_component_order\": 1,");
        sb.AppendLine("  \"intent\": \"RESIDENTIAL_LOT_BLOCK\",");
        sb.AppendLine("  \"access_readiness_class\": \"DUAL_ACCESS_CANDIDATE\",");
        sb.AppendLine("  \"component_bbox\": {\"min_x\": 124, \"min_y\": 10, \"max_x\": 212, \"max_y\": 69, \"width_px\": 89, \"height_px\": 60},");
        sb.AppendLine("  \"source_dimensions\": \"256x256\",");
        sb.AppendLine("  \"lot_count\": 7,");
        sb.AppendLine("  \"accepted_building_slot_count\": 7,");
        sb.AppendLine("  \"overlay_feature_count\": 15,");
        sb.AppendLine("  \"review_check_count\": 18,");
        sb.AppendLine("  \"passed_review_check_count\": 18,");
        sb.AppendLine("  \"failed_review_check_count\": 0,");
        sb.AppendLine("  \"writer_ready\": false,");
        sb.AppendLine("  \"runtime_valid\": false,");
        sb.AppendLine("  \"materialized\": false,");
        sb.AppendLine("  \"verdict\": \"MAP26C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET_COMPLETE\"");
        sb.Append("}");
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private string WriteReviewCsv()
    {
        var path = Path.Combine(_tempDir, "review.csv");
        File.WriteAllText(path, "check_order,check_id,check_status\n1,GEOMETRY_MVP_EXISTS,PASS\n");
        return path;
    }

    private string WriteReviewMd()
    {
        var path = Path.Combine(_tempDir, "review.md");
        File.WriteAllText(path, "# MAP-26C QA Review Packet\n\n## Claim Boundary\n\nQA-only.\n");
        return path;
    }

    private string WriteReviewSummary()
    {
        var path = Path.Combine(_tempDir, "review.summary.txt");
        File.WriteAllText(path, "verdict: MAP26C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_REVIEW_PACKET_COMPLETE\n");
        return path;
    }

    private (string Mvp, string Oj, string Oc, string Op, string Rj, string Rc, string Rm, string Rs)
        MakeValidFixtures() =>
        (WriteMvpJson(), WriteOverlayJson(), WriteOverlayCsv(), WriteOverlayPng(),
         WriteReviewJson(), WriteReviewCsv(), WriteReviewMd(), WriteReviewSummary());

    private (string Json, string Md, string Csv, string Summary) MakeOutputPaths(string subdir = "default")
    {
        var dir = Path.Combine(_tempDir, ".local", subdir);
        Directory.CreateDirectory(dir);
        return (
            Path.Combine(dir, "manifest.json"),
            Path.Combine(dir, "manifest.md"),
            Path.Combine(dir, "manifest.csv"),
            Path.Combine(dir, "manifest.summary.txt")
        );
    }

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
        psi.ArgumentList.Add("--");
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc     = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        Task.WaitAll(stdoutTask, stderrTask);
        proc.WaitForExit();
        return (proc.ExitCode, stdoutTask.Result, stderrTask.Result);
    }

    private const string Cmd = "deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-input-manifest";

    private (int ExitCode, string Stdout, string Stderr) RunBuild(string subdir = "default")
    {
        var (mvp, oj, oc, op, rj, rc, rm, rs) = MakeValidFixtures();
        var (j, m, c, s) = MakeOutputPaths(subdir);
        return RunCli(Cmd,
            "--geometry-mvp",      mvp,
            "--qa-overlay-json",   oj,
            "--qa-overlay-csv",    oc,
            "--qa-overlay-png",    op,
            "--qa-review-json",    rj,
            "--qa-review-csv",     rc,
            "--qa-review-md",      rm,
            "--qa-review-summary", rs,
            "--output-json",       j,
            "--output-md",         m,
            "--output-csv",        c,
            "--summary",           s);
    }

    // -----------------------------------------------------------------------
    // Exit codes
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_WithValidInputs()
    {
        var (code, stdout, stderr) = RunBuild("exit0");
        Assert.True(code == 0, $"Exit {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void Cli_ExitsOne_WhenMissingArgs()
    {
        var (code, _, _) = RunCli(Cmd);
        Assert.Equal(1, code);
    }

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var (mvp, oj, oc, op, rj, rc, rm, rs) = MakeValidFixtures();
        var outside = Path.Combine(_tempDir, "not-local");
        Directory.CreateDirectory(outside);
        var (code, _, _) = RunCli(Cmd,
            "--geometry-mvp",      mvp,
            "--qa-overlay-json",   oj,
            "--qa-overlay-csv",    oc,
            "--qa-overlay-png",    op,
            "--qa-review-json",    rj,
            "--qa-review-csv",     rc,
            "--qa-review-md",      rm,
            "--qa-review-summary", rs,
            "--output-json",       Path.Combine(outside, "out.json"),
            "--output-md",         Path.Combine(outside, "out.md"),
            "--output-csv",        Path.Combine(outside, "out.csv"),
            "--summary",           Path.Combine(outside, "out.txt"));
        Assert.Equal(1, code);
    }

    [Fact]
    public void Cli_ExitsOne_WhenMissingMvpJson()
    {
        var (_, oj, oc, op, rj, rc, rm, rs) = MakeValidFixtures();
        var (j, m, c, s) = MakeOutputPaths("no-mvp");
        var (code, _, _) = RunCli(Cmd,
            "--geometry-mvp",      "__nonexistent__.json",
            "--qa-overlay-json",   oj,
            "--qa-overlay-csv",    oc,
            "--qa-overlay-png",    op,
            "--qa-review-json",    rj,
            "--qa-review-csv",     rc,
            "--qa-review-md",      rm,
            "--qa-review-summary", rs,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // Output files created
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesOutputJson()
    {
        var (mvp, oj, oc, op, rj, rc, rm, rs) = MakeValidFixtures();
        var (j, m, c, s) = MakeOutputPaths("json");
        RunCli(Cmd,
            "--geometry-mvp", mvp, "--qa-overlay-json", oj, "--qa-overlay-csv", oc,
            "--qa-overlay-png", op, "--qa-review-json", rj, "--qa-review-csv", rc,
            "--qa-review-md", rm, "--qa-review-summary", rs,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(j), "output JSON not created");
    }

    [Fact]
    public void Cli_CreatesOutputMd()
    {
        var (mvp, oj, oc, op, rj, rc, rm, rs) = MakeValidFixtures();
        var (j, m, c, s) = MakeOutputPaths("md");
        RunCli(Cmd,
            "--geometry-mvp", mvp, "--qa-overlay-json", oj, "--qa-overlay-csv", oc,
            "--qa-overlay-png", op, "--qa-review-json", rj, "--qa-review-csv", rc,
            "--qa-review-md", rm, "--qa-review-summary", rs,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(m), "output MD not created");
    }

    [Fact]
    public void Cli_CreatesOutputCsv()
    {
        var (mvp, oj, oc, op, rj, rc, rm, rs) = MakeValidFixtures();
        var (j, m, c, s) = MakeOutputPaths("csv");
        RunCli(Cmd,
            "--geometry-mvp", mvp, "--qa-overlay-json", oj, "--qa-overlay-csv", oc,
            "--qa-overlay-png", op, "--qa-review-json", rj, "--qa-review-csv", rc,
            "--qa-review-md", rm, "--qa-review-summary", rs,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(c), "output CSV not created");
    }

    [Fact]
    public void Cli_CreatesOutputSummary()
    {
        var (mvp, oj, oc, op, rj, rc, rm, rs) = MakeValidFixtures();
        var (j, m, c, s) = MakeOutputPaths("summary");
        RunCli(Cmd,
            "--geometry-mvp", mvp, "--qa-overlay-json", oj, "--qa-overlay-csv", oc,
            "--qa-overlay-png", op, "--qa-review-json", rj, "--qa-review-csv", rc,
            "--qa-review-md", rm, "--qa-review-summary", rs,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(s), "output summary not created");
    }

    // -----------------------------------------------------------------------
    // Stdout / output content
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_Stdout_ContainsTitle()
    {
        var (_, stdout, _) = RunBuild("title");
        Assert.Contains("MAP-26D WorldBuilder minimal concrete geometry writer input manifest", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_Stdout_ContainsVerdict()
    {
        var (_, stdout, _) = RunBuild("verdict");
        Assert.Contains("MAP26D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST_COMPLETE", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_Stdout_ContainsGateStatus()
    {
        var (_, stdout, _) = RunBuild("gate");
        Assert.Contains("LOCKED_PENDING_OPERATOR_APPROVAL", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputJson_ContainsVerdict()
    {
        var (mvp, oj, oc, op, rj, rc, rm, rs) = MakeValidFixtures();
        var (j, m, c, s) = MakeOutputPaths("jsonverdict");
        RunCli(Cmd,
            "--geometry-mvp", mvp, "--qa-overlay-json", oj, "--qa-overlay-csv", oc,
            "--qa-overlay-png", op, "--qa-review-json", rj, "--qa-review-csv", rc,
            "--qa-review-md", rm, "--qa-review-summary", rs,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        var json = File.ReadAllText(j);
        Assert.Contains("MAP26D_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST_COMPLETE", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputJson_ContainsSha256()
    {
        var (mvp, oj, oc, op, rj, rc, rm, rs) = MakeValidFixtures();
        var (j, m, c, s) = MakeOutputPaths("jsonsha");
        RunCli(Cmd,
            "--geometry-mvp", mvp, "--qa-overlay-json", oj, "--qa-overlay-csv", oc,
            "--qa-overlay-png", op, "--qa-review-json", rj, "--qa-review-csv", rc,
            "--qa-review-md", rm, "--qa-review-summary", rs,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        var json = File.ReadAllText(j);
        Assert.Contains("\"sha256\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputJson_ApprovedForWriterExperiment_IsFalse()
    {
        var (mvp, oj, oc, op, rj, rc, rm, rs) = MakeValidFixtures();
        var (j, m, c, s) = MakeOutputPaths("jsonapproved");
        RunCli(Cmd,
            "--geometry-mvp", mvp, "--qa-overlay-json", oj, "--qa-overlay-csv", oc,
            "--qa-overlay-png", op, "--qa-review-json", rj, "--qa-review-csv", rc,
            "--qa-review-md", rm, "--qa-review-summary", rs,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        var json = File.ReadAllText(j);
        Assert.Contains("\"approved_for_writer_experiment\": false", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputSummary_ContainsGateStatus()
    {
        var (mvp, oj, oc, op, rj, rc, rm, rs) = MakeValidFixtures();
        var (j, m, c, s) = MakeOutputPaths("summarygate");
        RunCli(Cmd,
            "--geometry-mvp", mvp, "--qa-overlay-json", oj, "--qa-overlay-csv", oc,
            "--qa-overlay-png", op, "--qa-review-json", rj, "--qa-review-csv", rc,
            "--qa-review-md", rm, "--qa-review-summary", rs,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        var txt = File.ReadAllText(s);
        Assert.Contains("LOCKED_PENDING_OPERATOR_APPROVAL", txt, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Forbidden files
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_DoesNotEmit_LotpackOrWorldGenOverrideFiles()
    {
        RunBuild("forbidden");
        var produced = Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories);
        Assert.DoesNotContain(produced, p => p.EndsWith(".lotpack", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(produced, p => p.EndsWith("WorldGenOverride.lua", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Unknown command
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommand_MentionsWriterInputManifestCommand()
    {
        var (_, stdout, stderr) = RunCli("unknown-xyz-command");
        var combined = stdout + stderr;
        Assert.Contains("deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-input-manifest",
            combined, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Helper script
    // -----------------------------------------------------------------------

    [Fact]
    public void HelperScript_Exists()
    {
        Assert.True(File.Exists(HelperScript), $"Helper script not found: {HelperScript}");
    }

    [Fact]
    public void HelperScript_DoesNotContain_CompileWorldgen()
    {
        var text = File.ReadAllText(HelperScript);
        Assert.DoesNotContain("compile-worldgen", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContain_WorldGenOverrideLua()
    {
        var text = File.ReadAllText(HelperScript);
        Assert.DoesNotContain("WorldGenOverride.lua", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContain_DotLotpack()
    {
        var text = File.ReadAllText(HelperScript);
        Assert.DoesNotContain(".lotpack", text, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Doc
    // -----------------------------------------------------------------------

    [Fact]
    public void Doc_Exists()
    {
        var docPath = Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST.md");
        Assert.True(File.Exists(docPath), $"Doc not found: {docPath}");
    }

    [Fact]
    public void Doc_MentionsWriterGate()
    {
        var docPath = Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST.md");
        var text = File.ReadAllText(docPath);
        Assert.True(
            text.Contains("LOCKED_PENDING_OPERATOR_APPROVAL", StringComparison.Ordinal) ||
            text.Contains("writer_experiment_gate_status", StringComparison.OrdinalIgnoreCase),
            "doc should mention the writer gate status");
    }

    [Fact]
    public void Doc_MentionsClaimBoundary()
    {
        var docPath = Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_INPUT_MANIFEST.md");
        var text = File.ReadAllText(docPath);
        Assert.True(
            text.Contains("writer_ready", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Claim Boundary", StringComparison.OrdinalIgnoreCase),
            "doc should mention writer_ready or claim boundary");
    }
}
