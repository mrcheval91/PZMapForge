using System.Text.Json;
using PZMapForge.Core.WorldGen;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilderTests
    : IDisposable
{
    private readonly string _tempDir;
    private readonly string _map27fDir;
    private readonly string _map27cDir;
    private readonly string _outputDir;

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilderTests()
    {
        _tempDir   = Path.Combine(Path.GetTempPath(), "pzmapforge-map27g-core-test.local", Guid.NewGuid().ToString());
        _map27fDir = Path.Combine(_tempDir, "map27f.local", "map_00");
        _map27cDir = Path.Combine(_tempDir, "map27c.local", "map_00");
        _outputDir = Path.Combine(_tempDir, "output.local", "map_00");
        Directory.CreateDirectory(_map27fDir);
        Directory.CreateDirectory(_map27cDir);
        Directory.CreateDirectory(_outputDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private void WriteMap27fFiles()
    {
        var json = new
        {
            format = "MAP-27F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_ACCEPTANCE_GATE",
            verdict = "MAP27F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_ACCEPTANCE_GATE_COMPLETE",
            acceptance_stage = "SANDBOX_WRITER_TILE_MATERIALIZATION_ACCEPTANCE_GATE",
            acceptance_gate_status = "ACCEPTED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY",
            accepted_for_next_sandbox_experiment = true,
            accepted_for_runtime_writer = false,
            accepted_for_playable_export = false,
            sandbox_only = true,
            sandbox_materialized_source = true,
            visual_qa_overlay_written = true,
            pz_runtime_materialized = false,
            materialized_cell_count = 5340,
            rendered_cell_count = 5340,
            count_match_summary = "MATERIALIZED_CELL_COUNT(5340) == RENDERED_CELL_COUNT(5340): MATCH",
            building_wall_candidate_cell_count = 850,
            building_floor_candidate_cell_count = 2444,
            access_edge_cell_count = 148,
            lot_space_cell_count = 1898,
            component_residual_cell_count = 0,
            material_kind_count = 5,
            layer_kind_count = 5,
            overlay_png_width = 1024,
            overlay_png_height = 1024,
            is_valid = true,
            writer_ready = false,
            runtime_valid = false,
            materialized = false,
            runtime_proof_claimed = false,
            public_playable_packaging_claimed = false,
            check_count = 38,
            passed_check_count = 38,
            failed_check_count = 0,
            next_allowed_experiment_name = "MAP-27G_SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOCK",
            next_allowed_experiment_status = "SANDBOX_ONLY_NOT_RUNTIME",
        };
        File.WriteAllText(
            Path.Combine(_map27fDir, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.json"),
            JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(
            Path.Combine(_map27fDir, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.md"),
            "# MAP-27F acceptance gate\nstatus: ACCEPTED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY\n");
        File.WriteAllText(
            Path.Combine(_map27fDir, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.csv"),
            "check_order,check_id,check_status,expected,actual\n1,MAP27E_ROOT_EXISTS,PASS,PASS,PASS\n");
        File.WriteAllText(
            Path.Combine(_map27fDir, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.summary.txt"),
            "MAP-27F Acceptance Gate\nVerdict: COMPLETE\nIs Valid: 1\n");
    }

    private void WriteMap27cFiles()
    {
        var json = new
        {
            format = "MAP-27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0",
            verdict = "MAP27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0_COMPLETE",
            is_valid = true,
            sandbox_only = true,
            materialized_cell_count = 5340,
        };
        File.WriteAllText(
            Path.Combine(_map27cDir, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json"),
            JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(
            Path.Combine(_map27cDir, "map_00.sandbox_writer_tile_materialized_cells.csv"),
            "cell_x,cell_y,material_kind,layer_kind\n0,0,WALL,STRUCTURE\n");
        File.WriteAllText(
            Path.Combine(_map27cDir, "map_00.sandbox_writer_tile_material_palette.json"),
            "{\"materials\":[\"WALL\",\"FLOOR\",\"ACCESS\",\"LOT\",\"RESIDUAL\"]}");
        File.WriteAllText(
            Path.Combine(_map27cDir, "map_00.sandbox_writer_tile_layer_stack.json"),
            "{\"layers\":[\"STRUCTURE\",\"FLOOR\",\"EDGE\",\"SPACE\",\"RESIDUAL\"]}");
    }

    private DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockResult BuildResult()
    {
        WriteMap27fFiles();
        WriteMap27cFiles();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilder();
        return builder.Build(_map27fDir, _map27cDir, _outputDir);
    }

    [Fact]
    public void Build_WithValidInputs_Verdict_IsComplete()
    {
        var result = BuildResult();
        Assert.Equal(
            "MAP27G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOCK_COMPLETE",
            result.Verdict);
    }

    [Fact]
    public void Build_WithValidInputs_IsValid_True()
    {
        var result = BuildResult();
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Build_WithValidInputs_ReplayLockStatus_IsLocked()
    {
        var result = BuildResult();
        Assert.Equal("LOCKED", result.ReplayLockStatus);
    }

    [Fact]
    public void Build_WithValidInputs_ReplayLockId_HasPrefix()
    {
        var result = BuildResult();
        Assert.StartsWith("map_00_replay_lock_", result.ReplayLockId);
    }

    [Fact]
    public void Build_WithValidInputs_ReplayLockId_Is35Chars()
    {
        var result = BuildResult();
        Assert.Equal(35, result.ReplayLockId.Length); // "map_00_replay_lock_" (19) + 16 hex chars
    }

    [Fact]
    public void Build_WithValidInputs_ReplayLockFileCount_Is8()
    {
        var result = BuildResult();
        Assert.Equal(8, result.ReplayLockFileCount);
    }

    [Fact]
    public void Build_WithValidInputs_ReplayLockFiles_Count_Is8()
    {
        var result = BuildResult();
        Assert.Equal(8, result.ReplayLockFiles.Count);
    }

    [Fact]
    public void Build_WithValidInputs_AllLockFiles_LockedForReplay_True()
    {
        var result = BuildResult();
        Assert.All(result.ReplayLockFiles, f => Assert.True(f.LockedForReplay));
    }

    [Fact]
    public void Build_WithValidInputs_AllLockFiles_RuntimeConsumable_False()
    {
        var result = BuildResult();
        Assert.All(result.ReplayLockFiles, f => Assert.False(f.RuntimeConsumable));
    }

    [Fact]
    public void Build_WithValidInputs_AllLockFiles_WriterConsumable_False()
    {
        var result = BuildResult();
        Assert.All(result.ReplayLockFiles, f => Assert.False(f.WriterConsumable));
    }

    [Fact]
    public void Build_WithValidInputs_AllLockFiles_Exist_True()
    {
        var result = BuildResult();
        Assert.All(result.ReplayLockFiles, f => Assert.True(f.Exists));
    }

    [Fact]
    public void Build_WithValidInputs_AllLockFiles_Sha256_NonEmpty()
    {
        var result = BuildResult();
        Assert.All(result.ReplayLockFiles, f => Assert.NotEmpty(f.Sha256));
    }

    [Fact]
    public void Build_WithValidInputs_CheckCount_Is43()
    {
        var result = BuildResult();
        Assert.Equal(43, result.CheckCount);
    }

    [Fact]
    public void Build_WithValidInputs_PassedCheckCount_Is43()
    {
        var result = BuildResult();
        Assert.Equal(43, result.PassedCheckCount);
    }

    [Fact]
    public void Build_WithValidInputs_FailedCheckCount_Is0()
    {
        var result = BuildResult();
        Assert.Equal(0, result.FailedCheckCount);
    }

    [Fact]
    public void Build_WithValidInputs_BlockingReasons_IsEmpty()
    {
        var result = BuildResult();
        Assert.Empty(result.BlockingReasons);
    }

    [Fact]
    public void Build_WithValidInputs_SandboxOnly_True()
    {
        var result = BuildResult();
        Assert.True(result.SandboxOnly);
    }

    [Fact]
    public void Build_WithValidInputs_PzRuntimeMaterialized_False()
    {
        var result = BuildResult();
        Assert.False(result.PzRuntimeMaterialized);
    }

    [Fact]
    public void Build_WithValidInputs_AcceptedForRuntimeWriter_False()
    {
        var result = BuildResult();
        Assert.False(result.AcceptedForRuntimeWriter);
    }

    [Fact]
    public void Build_WithValidInputs_AcceptedForPlayableExport_False()
    {
        var result = BuildResult();
        Assert.False(result.AcceptedForPlayableExport);
    }

    [Fact]
    public void Build_WithValidInputs_WriterReady_False()
    {
        var result = BuildResult();
        Assert.False(result.WriterReady);
    }

    [Fact]
    public void Build_WithValidInputs_RuntimeValid_False()
    {
        var result = BuildResult();
        Assert.False(result.RuntimeValid);
    }

    [Fact]
    public void Build_WithValidInputs_Materialized_False()
    {
        var result = BuildResult();
        Assert.False(result.Materialized);
    }

    [Fact]
    public void Build_WithValidInputs_RuntimeProofClaimed_False()
    {
        var result = BuildResult();
        Assert.False(result.RuntimeProofClaimed);
    }

    [Fact]
    public void Build_WithValidInputs_PublicPlayablePackagingClaimed_False()
    {
        var result = BuildResult();
        Assert.False(result.PublicPlayablePackagingClaimed);
    }

    [Fact]
    public void Build_WithValidInputs_MaterializedCellCount_Is5340()
    {
        var result = BuildResult();
        Assert.Equal(5340, result.MaterializedCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_RenderedCellCount_Is5340()
    {
        var result = BuildResult();
        Assert.Equal(5340, result.RenderedCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_AcceptedForNextSandboxExperiment_True()
    {
        var result = BuildResult();
        Assert.True(result.AcceptedForNextSandboxExperiment);
    }

    [Fact]
    public void Build_WithValidInputs_NextAllowedExperimentName_Correct()
    {
        var result = BuildResult();
        Assert.Equal(
            "MAP-27H_SANDBOX_WRITER_TILE_MATERIALIZATION_LOCKED_REPLAY_AUDIT",
            result.NextAllowedExperimentName);
    }

    [Fact]
    public void Build_WithValidInputs_NextForbiddenSteps_Count_Is11()
    {
        var result = BuildResult();
        Assert.Equal(11, result.NextForbiddenSteps.Count);
    }

    [Fact]
    public void Build_WithValidInputs_ReplayLockReasons_Count_Is7()
    {
        var result = BuildResult();
        Assert.Equal(7, result.ReplayLockReasons.Count);
    }

    [Fact]
    public void Build_WithValidInputs_SourceAcceptanceGateStatus_Correct()
    {
        var result = BuildResult();
        Assert.Equal("ACCEPTED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY", result.SourceAcceptanceGateStatus);
    }

    [Fact]
    public void Build_WithValidInputs_SourceAcceptedForNextSandboxExperiment_True()
    {
        var result = BuildResult();
        Assert.True(result.SourceAcceptedForNextSandboxExperiment);
    }

    [Fact]
    public void Build_WithValidInputs_SourceAcceptedForRuntimeWriter_False()
    {
        var result = BuildResult();
        Assert.False(result.SourceAcceptedForRuntimeWriter);
    }

    [Fact]
    public void Build_WithValidInputs_4AgFilesInLockFiles()
    {
        var result = BuildResult();
        int agCount = result.ReplayLockFiles.Count(f => f.SourceStage == "MAP-27F");
        Assert.Equal(4, agCount);
    }

    [Fact]
    public void Build_WithValidInputs_4TmFilesInLockFiles()
    {
        var result = BuildResult();
        int tmCount = result.ReplayLockFiles.Count(f => f.SourceStage == "MAP-27C");
        Assert.Equal(4, tmCount);
    }

    [Fact]
    public void Build_WithValidInputs_ReplayLockId_IsDeterministic()
    {
        WriteMap27fFiles();
        WriteMap27cFiles();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilder();
        var r1 = builder.Build(_map27fDir, _map27cDir, _outputDir);
        var r2 = builder.Build(_map27fDir, _map27cDir, _outputDir);
        Assert.Equal(r1.ReplayLockId, r2.ReplayLockId);
    }

    [Fact]
    public void Build_WithValidInputs_ForbiddenArtifactScan_ContainsPass()
    {
        var result = BuildResult();
        Assert.Contains("PASS", result.ForbiddenArtifactScan);
    }

    [Fact]
    public void Build_WithMissingAcceptanceGateRoot_IsInvalid()
    {
        WriteMap27cFiles();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilder();
        var result = builder.Build(Path.Combine(_tempDir, "nonexistent.local"), _map27cDir, _outputDir);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_WithMissingTileMaterializerRoot_IsInvalid()
    {
        WriteMap27fFiles();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilder();
        var result = builder.Build(_map27fDir, Path.Combine(_tempDir, "nonexistent.local"), _outputDir);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_WithMissingAcceptanceGateJson_IsInvalid()
    {
        WriteMap27fFiles();
        WriteMap27cFiles();
        File.Delete(Path.Combine(_map27fDir,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.json"));
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilder();
        var result = builder.Build(_map27fDir, _map27cDir, _outputDir);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_WithMissingTileMaterializerJson_IsInvalid()
    {
        WriteMap27fFiles();
        WriteMap27cFiles();
        File.Delete(Path.Combine(_map27cDir,
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json"));
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilder();
        var result = builder.Build(_map27fDir, _map27cDir, _outputDir);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_WithInvalidAcceptanceGateJson_IsInvalid()
    {
        WriteMap27fFiles();
        WriteMap27cFiles();
        File.WriteAllText(
            Path.Combine(_map27fDir,
                "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.json"),
            "{ not valid json !!!");
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilder();
        var result = builder.Build(_map27fDir, _map27cDir, _outputDir);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Build_WithRejectedAcceptanceGateStatus_HasBlockingReasons()
    {
        var rejectedJson = new
        {
            format = "MAP-27F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_ACCEPTANCE_GATE",
            verdict = "MAP27F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_ACCEPTANCE_GATE_INVALID",
            acceptance_gate_status = "REJECTED_CRITERIA_NOT_MET",
            accepted_for_next_sandbox_experiment = false,
            accepted_for_runtime_writer = false,
            accepted_for_playable_export = false,
            sandbox_only = true,
            sandbox_materialized_source = true,
            visual_qa_overlay_written = true,
            pz_runtime_materialized = false,
            materialized_cell_count = 0,
            rendered_cell_count = 0,
            count_match_summary = "MISMATCH",
            building_wall_candidate_cell_count = 0,
            building_floor_candidate_cell_count = 0,
            access_edge_cell_count = 0,
            lot_space_cell_count = 0,
            component_residual_cell_count = 0,
            material_kind_count = 0,
            layer_kind_count = 0,
            overlay_png_width = 0,
            overlay_png_height = 0,
            is_valid = false,
            writer_ready = false,
            runtime_valid = false,
            materialized = false,
            runtime_proof_claimed = false,
            public_playable_packaging_claimed = false,
            check_count = 38,
            passed_check_count = 0,
            failed_check_count = 38,
        };
        Directory.CreateDirectory(_map27fDir);
        File.WriteAllText(
            Path.Combine(_map27fDir,
                "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.json"),
            JsonSerializer.Serialize(rejectedJson, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(
            Path.Combine(_map27fDir,
                "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.md"),
            "# MAP-27F rejected\n");
        File.WriteAllText(
            Path.Combine(_map27fDir,
                "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.csv"),
            "check_order,check_id,check_status\n");
        File.WriteAllText(
            Path.Combine(_map27fDir,
                "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.summary.txt"),
            "REJECTED\n");
        WriteMap27cFiles();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilder();
        var result = builder.Build(_map27fDir, _map27cDir, _outputDir);
        Assert.NotEmpty(result.BlockingReasons);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void RenderJson_WithValidResult_ProducesNonEmptyJson()
    {
        var result = BuildResult();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilder();
        string json = builder.RenderJson(result);
        Assert.NotEmpty(json);
        Assert.Contains("replay_lock_id", json);
        Assert.Contains("map_00_replay_lock_", json);
    }

    [Fact]
    public void RenderMarkdown_WithValidResult_ProducesNonEmptyMarkdown()
    {
        var result = BuildResult();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilder();
        string md = builder.RenderMarkdown(result);
        Assert.NotEmpty(md);
        Assert.Contains("MAP-27G", md);
    }

    [Fact]
    public void RenderCsv_WithValidResult_HasCheckCountRows()
    {
        var result = BuildResult();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilder();
        string csv = builder.RenderCsv(result);
        int rowCount = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length - 1; // minus header
        Assert.Equal(43, rowCount);
    }

    [Fact]
    public void RenderSummary_WithValidResult_ContainsLockId()
    {
        var result = BuildResult();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilder();
        string summary = builder.RenderSummary(result);
        Assert.Contains("map_00_replay_lock_", summary);
    }
}
