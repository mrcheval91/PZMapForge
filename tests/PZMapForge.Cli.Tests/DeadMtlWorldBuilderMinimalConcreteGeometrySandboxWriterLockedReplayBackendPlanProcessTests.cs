using System.Diagnostics;
using System.Text;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanProcessTests
    : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map27j-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanProcessTests() =>
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
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-replay-backend-plan.ps1");

    // -----------------------------------------------------------------------
    // Fixture helpers — build MAP-27I output using real Core builders
    // -----------------------------------------------------------------------

    private string GetMap27fRoot() => Path.Combine(_tempDir, "map27f.local", "map_00");
    private string GetMap27cRoot() => Path.Combine(_tempDir, "map27c.local", "map_00");
    private string GetMap27gRoot() => Path.Combine(_tempDir, "map27g.local", "map_00");
    private string GetMap27hRoot() => Path.Combine(_tempDir, "map27h.local", "map_00");
    private string GetMap27iRoot() => Path.Combine(_tempDir, "map27i.local", "map_00");
    private string GetOutputRoot() => Path.Combine(_tempDir, "output.local", "map_00");

    private string GetOutputJson() =>
        Path.Combine(GetOutputRoot(),
            "map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_plan.json");

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

    private void WriteMap27iOutput()
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
        var baseName = "map_00.minimal_concrete_geometry_sandbox_writer_locked_materialization_replay_dry_run";
        File.WriteAllText(Path.Combine(iRoot, baseName + ".json"),        dryRunBuilder.RenderJson(dryRunResult));
        File.WriteAllText(Path.Combine(iRoot, baseName + ".md"),          dryRunBuilder.RenderMarkdown(dryRunResult));
        File.WriteAllText(Path.Combine(iRoot, baseName + ".csv"),         dryRunBuilder.RenderCsv(dryRunResult));
        File.WriteAllText(Path.Combine(iRoot, baseName + ".summary.txt"), dryRunBuilder.RenderSummary(dryRunResult));
        File.WriteAllText(Path.Combine(iRoot, "map_00.sandbox_writer_locked_replay_material_counts.csv"),        dryRunBuilder.RenderMaterialCountsCsv(dryRunResult));
        File.WriteAllText(Path.Combine(iRoot, "map_00.sandbox_writer_locked_replay_source_manifest.json"),       dryRunBuilder.RenderSourceManifestJson(dryRunResult));
        File.WriteAllText(Path.Combine(iRoot, "map_00.sandbox_writer_locked_replay_digest.json"),                dryRunBuilder.RenderReplayDigestJson(dryRunResult));
        File.WriteAllText(Path.Combine(iRoot, "map_00.sandbox_writer_locked_replay_forbidden_output_guard.json"), dryRunBuilder.RenderForbiddenOutputGuardJson(dryRunResult));
    }

    private string[] BuildArgs(string? dryRunRootOverride = null, string? outputRootOverride = null)
    {
        string map27iRoot = dryRunRootOverride  ?? GetMap27iRoot();
        string outputRoot = outputRootOverride  ?? GetOutputRoot();
        Directory.CreateDirectory(outputRoot);
        string baseName = "map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_plan";
        return new[]
        {
            "run", "--project", CliProject, "--",
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-replay-backend-plan",
            "--dry-run-root",                map27iRoot,
            "--output-root",                 outputRoot,
            "--output-json",                 Path.Combine(outputRoot, baseName + ".json"),
            "--output-md",                   Path.Combine(outputRoot, baseName + ".md"),
            "--output-csv",                  Path.Combine(outputRoot, baseName + ".csv"),
            "--summary",                     Path.Combine(outputRoot, baseName + ".summary.txt"),
            "--output-operation-plan-json",  Path.Combine(outputRoot, "map_00.sandbox_writer_locked_replay_backend_operation_plan.json"),
            "--output-operation-plan-csv",   Path.Combine(outputRoot, "map_00.sandbox_writer_locked_replay_backend_operation_plan.csv"),
            "--output-source-manifest-json", Path.Combine(outputRoot, "map_00.sandbox_writer_locked_replay_backend_source_manifest.json"),
            "--output-forbidden-guard-json", Path.Combine(outputRoot, "map_00.sandbox_writer_locked_replay_backend_forbidden_output_guard.json"),
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
        WriteMap27iOutput();
        var (code, _, _) = Run(BuildArgs());
        Assert.Equal(0, code);
    }

    [Fact]
    public void MissingArgs_ExitCode1()
    {
        var (code, _, stderr) = Run(new[]
        {
            "run", "--project", CliProject, "--",
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-sandbox-writer-locked-replay-backend-plan",
        });
        Assert.Equal(1, code);
        Assert.Contains("--dry-run-root", stderr);
    }

    [Fact]
    public void MissingDryRunRoot_ExitCode1()
    {
        var badRoot = Path.Combine(_tempDir, "nonexistent.local", "map_00");
        var (code, _, _) = Run(BuildArgs(dryRunRootOverride: badRoot));
        Assert.Equal(1, code);
    }

    [Fact]
    public void NonLocalOutput_ExitCode1()
    {
        WriteMap27iOutput();
        var badRoot = Path.GetTempPath();
        var (code, _, stderr) = Run(BuildArgs(outputRootOverride: badRoot));
        Assert.Equal(1, code);
        Assert.Contains(".local", stderr);
    }

    [Fact]
    public void Writes8Outputs()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        string root    = GetOutputRoot();
        string baseName = "map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_plan";
        Assert.True(File.Exists(Path.Combine(root, baseName + ".json")));
        Assert.True(File.Exists(Path.Combine(root, baseName + ".md")));
        Assert.True(File.Exists(Path.Combine(root, baseName + ".csv")));
        Assert.True(File.Exists(Path.Combine(root, baseName + ".summary.txt")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_locked_replay_backend_operation_plan.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_locked_replay_backend_operation_plan.csv")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_locked_replay_backend_source_manifest.json")));
        Assert.True(File.Exists(Path.Combine(root, "map_00.sandbox_writer_locked_replay_backend_forbidden_output_guard.json")));
    }

    [Fact]
    public void VerdictComplete()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(
            "MAP27J_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_PLAN_COMPLETE",
            doc.RootElement.GetProperty("verdict").GetString());
    }

    [Fact]
    public void BackendPlanStatusComplete()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("BACKEND_PLAN_COMPLETE",
            doc.RootElement.GetProperty("backend_plan_status").GetString());
    }

    [Fact]
    public void SandboxBackendPlanOnlyTrue()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("sandbox_backend_plan_only").GetBoolean());
    }

    [Fact]
    public void CheckCount51AllPass()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(51, doc.RootElement.GetProperty("check_count").GetInt32());
        Assert.Equal(51, doc.RootElement.GetProperty("passed_check_count").GetInt32());
        Assert.Equal(0,  doc.RootElement.GetProperty("failed_check_count").GetInt32());
    }

    [Fact]
    public void OperationPlanCount5()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(5, doc.RootElement.GetProperty("operation_plan_count").GetInt32());
    }

    [Fact]
    public void TotalPlannedCells5340()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(5340,
            doc.RootElement.GetProperty("operation_plan_groups").GetProperty("total_planned_cell_count").GetInt32());
    }

    [Fact]
    public void NonEmptyGroups4()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(4,
            doc.RootElement.GetProperty("operation_plan_groups").GetProperty("non_empty_operation_group_count").GetInt32());
    }

    [Fact]
    public void EmptyGroups1()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(1,
            doc.RootElement.GetProperty("operation_plan_groups").GetProperty("empty_operation_group_count").GetInt32());
    }

    [Fact]
    public void OperationPlanJsonExists()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        Assert.True(File.Exists(Path.Combine(GetOutputRoot(),
            "map_00.sandbox_writer_locked_replay_backend_operation_plan.json")));
    }

    [Fact]
    public void OperationPlanCsvExists()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        Assert.True(File.Exists(Path.Combine(GetOutputRoot(),
            "map_00.sandbox_writer_locked_replay_backend_operation_plan.csv")));
    }

    [Fact]
    public void SourceManifestJsonExists()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        Assert.True(File.Exists(Path.Combine(GetOutputRoot(),
            "map_00.sandbox_writer_locked_replay_backend_source_manifest.json")));
    }

    [Fact]
    public void ForbiddenGuardJsonExists()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        Assert.True(File.Exists(Path.Combine(GetOutputRoot(),
            "map_00.sandbox_writer_locked_replay_backend_forbidden_output_guard.json")));
    }

    [Fact]
    public void ForbiddenArtifactScanContainsPass()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.Contains("PASS", doc.RootElement.GetProperty("forbidden_artifact_scan").GetString() ?? "");
    }

    [Fact]
    public void WriterReadyFalse()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("writer_ready").GetBoolean());
    }

    [Fact]
    public void RuntimeValidFalse()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("runtime_valid").GetBoolean());
    }

    [Fact]
    public void MaterializedFalse()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("materialized").GetBoolean());
    }

    [Fact]
    public void RuntimeProofClaimedFalse()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("runtime_proof_claimed").GetBoolean());
    }

    [Fact]
    public void PublicPlayablePackagingClaimedFalse()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("public_playable_packaging_claimed").GetBoolean());
    }

    [Fact]
    public void HelperScript_Exists()
        => Assert.True(File.Exists(HelperScript));

    [Fact]
    public void HelperScript_ContainsCanonicalMap27iRoot()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.Contains("locked-materialization-replay-dry-run", content);
    }

    [Fact]
    public void HelperScript_ContainsCanonicalMap27jOutputRoot()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.Contains("locked-replay-backend-plan", content);
    }

    [Fact]
    public void HelperScript_ContainsDryRunRootArg()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.Contains("--dry-run-root", content);
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

    [Fact]
    public void CheckIds_ContainMap27jPrefixedIds()
    {
        WriteMap27iOutput();
        Run(BuildArgs());
        var json = File.ReadAllText(GetOutputJson());
        using var doc = JsonDocument.Parse(json);
        var ids = doc.RootElement.GetProperty("checks")
            .EnumerateArray()
            .Select(c => c.GetProperty("check_id").GetString() ?? "")
            .ToList();
        Assert.Contains("MAP27I_DRY_RUN_ROOT_EXISTS",               ids);
        Assert.Contains("MAP27I_DRY_RUN_STATUS_COMPLETE",           ids);
        Assert.Contains("MAP27I_LOCKED_REPLAY_DIGEST_PRESENT",      ids);
        Assert.Contains("SOURCE_MANIFEST_6_FILES_HASHED",           ids);
        Assert.Contains("BACKEND_OPERATION_PLAN_5_RECORDS",         ids);
        Assert.Contains("BACKEND_OPERATION_PLAN_TOTAL_CELLS_5340",  ids);
        Assert.Contains("ALL_OPERATIONS_SANDBOX_ONLY",              ids);
        Assert.Contains("NO_OPERATION_EMITS_RUNTIME_FILE",          ids);
        Assert.Contains("POST_BACKEND_PLAN_FORBIDDEN_SCAN_PASS",    ids);
        Assert.Contains("SANDBOX_BACKEND_PLAN_ONLY_TRUE",           ids);
        Assert.Contains("NO_RUNTIME_OUTPUTS_EMITTED",               ids);
    }
}
