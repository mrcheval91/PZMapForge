using System.Diagnostics;
using System.Text;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterProcessTests
    : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map27k-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterProcessTests() =>
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
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-replay-backend-dry-run-emitter.ps1");

    // -----------------------------------------------------------------------
    // Fixture helpers — build MAP-27J output using real Core builders
    // -----------------------------------------------------------------------

    private string GetMap27fRoot() => Path.Combine(_tempDir, "map27f.local", "map_00");
    private string GetMap27cRoot() => Path.Combine(_tempDir, "map27c.local", "map_00");
    private string GetMap27gRoot() => Path.Combine(_tempDir, "map27g.local", "map_00");
    private string GetMap27hRoot() => Path.Combine(_tempDir, "map27h.local", "map_00");
    private string GetMap27iRoot() => Path.Combine(_tempDir, "map27i.local", "map_00");
    private string GetMap27jRoot() => Path.Combine(_tempDir, "map27j.local", "map_00");
    private string GetOutputRoot() => Path.Combine(_tempDir, "output.local", "map_00");

    private string GetOutputJson() =>
        Path.Combine(GetOutputRoot(),
            "map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_dry_run_emitter.json");

    private static string MakeFixtureCsv()
    {
        var sb = new StringBuilder("cell_x,cell_y,material_kind,layer_kind\n");
        for (int i = 0; i < 850;  i++) sb.Append($"{i},0,WALL,STRUCTURE\n");
        for (int i = 0; i < 2444; i++) sb.Append($"{i},1,FLOOR,FLOOR\n");
        for (int i = 0; i < 148;  i++) sb.Append($"{i},2,ACCESS,EDGE\n");
        for (int i = 0; i < 1898; i++) sb.Append($"{i},3,LOT,SPACE\n");
        return sb.ToString();
    }

    private void WriteMap27fFiles()
    {
        var root = GetMap27fRoot();
        Directory.CreateDirectory(root);
        var f27 = new
        {
            format  = "MAP-27F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_ACCEPTANCE_GATE",
            verdict = "MAP27F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_ACCEPTANCE_GATE_COMPLETE",
            acceptance_gate_status               = "ACCEPTED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY",
            accepted_for_next_sandbox_experiment = true,
            accepted_for_runtime_writer          = false,
            accepted_for_playable_export         = false,
            sandbox_only                         = true,
            sandbox_materialized_source          = true,
            visual_qa_overlay_written            = true,
            pz_runtime_materialized              = false,
            materialized_cell_count              = 5340,
            rendered_cell_count                  = 5340,
            count_match_summary                  = "MATERIALIZED_CELL_COUNT(5340) == RENDERED_CELL_COUNT(5340): MATCH",
            building_wall_candidate_cell_count   = 850,
            building_floor_candidate_cell_count  = 2444,
            access_edge_cell_count               = 148,
            lot_space_cell_count                 = 1898,
            component_residual_cell_count        = 0,
            material_kind_count                  = 5,
            layer_kind_count                     = 5,
            overlay_png_width                    = 1024,
            overlay_png_height                   = 1024,
            is_valid                             = true,
            writer_ready                         = false,
            runtime_valid                        = false,
            materialized                         = false,
            runtime_proof_claimed                = false,
            public_playable_packaging_claimed    = false,
            check_count                          = 38,
            passed_check_count                   = 38,
            failed_check_count                   = 0,
            next_allowed_experiment_name         = "MAP-27G_SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOCK",
            next_allowed_experiment_status       = "SANDBOX_ONLY_NOT_RUNTIME",
        };
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.json"),
            JsonSerializer.Serialize(f27));
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.md"), "# MAP-27F");
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.csv"),
            "check_order,check_id,check_status");
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.summary.txt"),
            "MAP-27F summary");
    }

    private void WriteMap27cFiles()
    {
        var root = GetMap27cRoot();
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json"),
            "{\"format\":\"MAP-27C\",\"is_valid\":true,\"sandbox_only\":true,\"materialized_cell_count\":5340}");
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_materialized_cells.csv"),
            MakeFixtureCsv());
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_material_palette.json"),
            "{\"materials\":[\"WALL\",\"FLOOR\",\"ACCESS\",\"LOT\",\"RESIDUAL\"]}");
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_layer_stack.json"),
            "{\"layers\":[\"STRUCTURE\",\"FLOOR\",\"EDGE\",\"SPACE\",\"RESIDUAL\"]}");
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_materialization_replay_log.json"),
            "{\"replay_log_entry_count\":5340}");
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_materialization_ownership_summary.json"),
            "{\"ownership_summary\":true}");
        File.WriteAllText(Path.Combine(root,
            "map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json"),
            "{\"forbidden_output_guard\":true,\"all_clean\":true}");
    }

    private void WriteMap27jOutput()
    {
        WriteMap27fFiles();
        WriteMap27cFiles();

        var gRoot = GetMap27gRoot();
        Directory.CreateDirectory(gRoot);
        var lockBuilder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilder();
        var lockResult  = lockBuilder.Build(GetMap27fRoot(), GetMap27cRoot(), gRoot);
        File.WriteAllText(
            Path.Combine(gRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_replay_lock.json"),
            lockBuilder.RenderJson(lockResult));

        var hRoot = GetMap27hRoot();
        Directory.CreateDirectory(hRoot);
        var auditBuilder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationLockedReplayAuditBuilder();
        var auditResult  = auditBuilder.Build(gRoot, hRoot);
        File.WriteAllText(
            Path.Combine(hRoot, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_locked_replay_audit.json"),
            auditBuilder.RenderJson(auditResult));

        var iRoot = GetMap27iRoot();
        Directory.CreateDirectory(iRoot);
        var dryRunBuilder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunBuilder();
        var dryRunResult  = dryRunBuilder.Build(hRoot, iRoot);
        var iBase = "map_00.minimal_concrete_geometry_sandbox_writer_locked_materialization_replay_dry_run";
        File.WriteAllText(Path.Combine(iRoot, iBase + ".json"),        dryRunBuilder.RenderJson(dryRunResult));
        File.WriteAllText(Path.Combine(iRoot, iBase + ".md"),          dryRunBuilder.RenderMarkdown(dryRunResult));
        File.WriteAllText(Path.Combine(iRoot, iBase + ".csv"),         dryRunBuilder.RenderCsv(dryRunResult));
        File.WriteAllText(Path.Combine(iRoot, iBase + ".summary.txt"), dryRunBuilder.RenderSummary(dryRunResult));
        File.WriteAllText(Path.Combine(iRoot, "map_00.sandbox_writer_locked_replay_material_counts.csv"),         dryRunBuilder.RenderMaterialCountsCsv(dryRunResult));
        File.WriteAllText(Path.Combine(iRoot, "map_00.sandbox_writer_locked_replay_source_manifest.json"),        dryRunBuilder.RenderSourceManifestJson(dryRunResult));
        File.WriteAllText(Path.Combine(iRoot, "map_00.sandbox_writer_locked_replay_digest.json"),                 dryRunBuilder.RenderReplayDigestJson(dryRunResult));
        File.WriteAllText(Path.Combine(iRoot, "map_00.sandbox_writer_locked_replay_forbidden_output_guard.json"), dryRunBuilder.RenderForbiddenOutputGuardJson(dryRunResult));

        var jRoot = GetMap27jRoot();
        Directory.CreateDirectory(jRoot);
        var planBuilder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanBuilder();
        var planResult  = planBuilder.Build(iRoot, jRoot);
        planResult = planBuilder.FinalizeAfterOutputs(planResult, jRoot);
        var jBase = "map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_plan";
        File.WriteAllText(Path.Combine(jRoot, jBase + ".json"),        planBuilder.RenderJson(planResult));
        File.WriteAllText(Path.Combine(jRoot, jBase + ".md"),          planBuilder.RenderMarkdown(planResult));
        File.WriteAllText(Path.Combine(jRoot, jBase + ".csv"),         planBuilder.RenderCsv(planResult));
        File.WriteAllText(Path.Combine(jRoot, jBase + ".summary.txt"), planBuilder.RenderSummary(planResult));
        File.WriteAllText(Path.Combine(jRoot, "map_00.sandbox_writer_locked_replay_backend_operation_plan.json"),         planBuilder.RenderOperationPlanJson(planResult));
        File.WriteAllText(Path.Combine(jRoot, "map_00.sandbox_writer_locked_replay_backend_operation_plan.csv"),          planBuilder.RenderOperationPlanCsv(planResult));
        File.WriteAllText(Path.Combine(jRoot, "map_00.sandbox_writer_locked_replay_backend_source_manifest.json"),        planBuilder.RenderSourceManifestJson(planResult));
        File.WriteAllText(Path.Combine(jRoot, "map_00.sandbox_writer_locked_replay_backend_forbidden_output_guard.json"), planBuilder.RenderForbiddenOutputGuardJson(planResult));
    }

    private string[] BuildArgs(string? backendPlanRootOverride = null, string? outputRootOverride = null)
    {
        string map27jRoot = backendPlanRootOverride ?? GetMap27jRoot();
        string outputRoot = outputRootOverride      ?? GetOutputRoot();
        Directory.CreateDirectory(outputRoot);
        string baseName = "map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_dry_run_emitter";
        return new[]
        {
            "run", "--project", CliProject, "--",
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-replay-backend-dry-run-emitter",
            "--backend-plan-root",             map27jRoot,
            "--output-root",                   outputRoot,
            "--output-json",                   Path.Combine(outputRoot, baseName + ".json"),
            "--output-md",                     Path.Combine(outputRoot, baseName + ".md"),
            "--output-csv",                    Path.Combine(outputRoot, baseName + ".csv"),
            "--summary",                       Path.Combine(outputRoot, baseName + ".summary.txt"),
            "--output-operations-json",        Path.Combine(outputRoot, "map_00.sandbox_writer_locked_replay_backend_dry_run_operations.json"),
            "--output-operations-csv",         Path.Combine(outputRoot, "map_00.sandbox_writer_locked_replay_backend_dry_run_operations.csv"),
            "--output-source-manifest-json",   Path.Combine(outputRoot, "map_00.sandbox_writer_locked_replay_backend_dry_run_source_manifest.json"),
            "--output-dry-run-digest-json",    Path.Combine(outputRoot, "map_00.sandbox_writer_locked_replay_backend_dry_run_digest.json"),
            "--output-forbidden-guard-json",   Path.Combine(outputRoot, "map_00.sandbox_writer_locked_replay_backend_dry_run_forbidden_output_guard.json"),
        };
    }

    private static (int ExitCode, string Stdout, string Stderr) Run(string[] args)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var proc = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        return (proc.ExitCode, stdoutTask.Result, stderrTask.Result);
    }

    // -----------------------------------------------------------------------
    // Tests
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidFixture_ExitCode0()
    {
        WriteMap27jOutput();
        var (code, _, _) = Run(BuildArgs());
        Assert.Equal(0, code);
    }

    [Fact]
    public void MissingArgs_ExitCode1()
    {
        var (code, _, stderr) = Run(new[]
        {
            "run", "--project", CliProject, "--",
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-replay-backend-dry-run-emitter",
        });
        Assert.Equal(1, code);
        Assert.Contains("--backend-plan-root", stderr);
    }

    [Fact]
    public void MissingSourceRoot_ExitCode1()
    {
        var badRoot = Path.Combine(_tempDir, "nonexistent.local", "map_00");
        var (code, _, _) = Run(BuildArgs(backendPlanRootOverride: badRoot));
        Assert.Equal(1, code);
    }

    [Fact]
    public void NonLocalOutput_ExitCode1()
    {
        WriteMap27jOutput();
        var badRoot = Path.GetTempPath();
        var (code, _, stderr) = Run(BuildArgs(outputRootOverride: badRoot));
        Assert.Equal(1, code);
        Assert.Contains(".local", stderr);
    }

    [Fact]
    public void Writes9Outputs()
    {
        WriteMap27jOutput();
        Run(BuildArgs());
        string root    = GetOutputRoot();
        string baseName = "map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_dry_run_emitter";
        Assert.True(File.Exists(Path.Combine(root, baseName + ".json")));
        Assert.True(File.Exists(Path.Combine(root, baseName + ".md")));
        Assert.True(File.Exists(Path.Combine(root, baseName + ".csv")));
        Assert.True(File.Exists(Path.Combine(root, baseName + ".summary.txt")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_locked_replay_backend_dry_run_operations.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_locked_replay_backend_dry_run_operations.csv")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_locked_replay_backend_dry_run_source_manifest.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_locked_replay_backend_dry_run_digest.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_locked_replay_backend_dry_run_forbidden_output_guard.json")));
    }

    [Fact]
    public void VerdictComplete()
    {
        WriteMap27jOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(
            "MAP27K_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_DRY_RUN_EMITTER_COMPLETE",
            doc.RootElement.GetProperty("verdict").GetString());
    }

    [Fact]
    public void CheckCount49AllPass()
    {
        WriteMap27jOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(49, doc.RootElement.GetProperty("check_count").GetInt32());
        Assert.Equal(49, doc.RootElement.GetProperty("passed_check_count").GetInt32());
        Assert.Equal(0,  doc.RootElement.GetProperty("failed_check_count").GetInt32());
    }

    [Fact]
    public void EmittedOperationCount5()
    {
        WriteMap27jOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(5, doc.RootElement.GetProperty("emitted_operation_count").GetInt32());
    }

    [Fact]
    public void EmittedTotalPlannedCells5340()
    {
        WriteMap27jOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(5340, doc.RootElement.GetProperty("emitted_total_planned_cell_count").GetInt32());
    }

    [Fact]
    public void OperationsJson_Contains5Records()
    {
        WriteMap27jOutput();
        Run(BuildArgs());
        var opsPath = Path.Combine(GetOutputRoot(), "map_00.sandbox_writer_locked_replay_backend_dry_run_operations.json");
        var json = File.ReadAllText(opsPath);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(5, doc.RootElement.GetProperty("emitted_operation_records").GetArrayLength());
    }

    [Fact]
    public void ForbiddenArtifactScan_ContainsExactPassString()
    {
        WriteMap27jOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        var scan = doc.RootElement.GetProperty("forbidden_artifact_scan").GetString() ?? "";
        Assert.Equal("POST_BACKEND_DRY_RUN_EMITTER_FORBIDDEN_SCAN PASS (0 forbidden artifacts in output root)", scan);
    }

    [Fact]
    public void ForbiddenGuardJson_ContainsFinalForbiddenScan()
    {
        WriteMap27jOutput();
        Run(BuildArgs());
        var guardPath = Path.Combine(GetOutputRoot(),
            "map_00.sandbox_writer_locked_replay_backend_dry_run_forbidden_output_guard.json");
        var json = File.ReadAllText(guardPath);
        using var doc = JsonDocument.Parse(json);
        Assert.Contains("PASS", doc.RootElement.GetProperty("forbidden_artifact_scan").GetString() ?? "");
        Assert.True(doc.RootElement.GetProperty("all_clean").GetBoolean());
    }

    [Fact]
    public void HelperScript_Exists()
        => Assert.True(File.Exists(HelperScript));

    [Fact]
    public void HelperScript_ContainsCanonicalMap27jRoot()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.Contains("locked-replay-backend-plan", content);
    }

    [Fact]
    public void HelperScript_ContainsCanonicalMap27kOutputRoot()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.Contains("locked-replay-backend-dry-run-emitter", content);
    }

    [Fact]
    public void HelperScript_ContainsBackendPlanRootArg()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.Contains("--backend-plan-root", content);
    }

    [Fact]
    public void DoesNotContainCompileWorldgen()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.DoesNotContain("compile-worldgen", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DoesNotContainWorldGenOverrideLua()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.DoesNotContain("WorldGenOverride.lua", content);
    }

    [Fact]
    public void DoesNotContainDotLotpack()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.DoesNotContain(".lotpack", content);
    }

    [Fact]
    public void DoesNotContainDotLotheader()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.DoesNotContain(".lotheader", content);
    }
}
