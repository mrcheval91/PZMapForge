using System.Text.Json;
using PZMapForge.Core.WorldGen;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationLockedReplayAuditBuilderTests
    : IDisposable
{
    private readonly string _tempDir;
    private readonly string _map27fDir;
    private readonly string _map27cDir;
    private readonly string _map27gDir;
    private readonly string _outputDir;

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationLockedReplayAuditBuilderTests()
    {
        _tempDir   = Path.Combine(Path.GetTempPath(), "pzmapforge-map27h-core-test.local", Guid.NewGuid().ToString());
        _map27fDir = Path.Combine(_tempDir, "map27f.local", "map_00");
        _map27cDir = Path.Combine(_tempDir, "map27c.local", "map_00");
        _map27gDir = Path.Combine(_tempDir, "map27g.local", "map_00");
        _outputDir = Path.Combine(_tempDir, "output.local", "map_00");
        Directory.CreateDirectory(_map27fDir);
        Directory.CreateDirectory(_map27cDir);
        Directory.CreateDirectory(_map27gDir);
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
        File.WriteAllText(
            Path.Combine(_map27cDir, "map_00.sandbox_writer_tile_materialization_replay_log.json"),
            "{\"replay_log_entry_count\":5340}");
        File.WriteAllText(
            Path.Combine(_map27cDir, "map_00.sandbox_writer_tile_materialization_ownership_summary.json"),
            "{\"ownership_summary\":true}");
        File.WriteAllText(
            Path.Combine(_map27cDir, "map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json"),
            "{\"forbidden_output_guard\":true,\"all_clean\":true}");
    }

    private void WriteMap27gOutput()
    {
        WriteMap27fFiles();
        WriteMap27cFiles();
        var lockBuilder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilder();
        var lockResult  = lockBuilder.Build(_map27fDir, _map27cDir, _map27gDir);
        File.WriteAllText(
            Path.Combine(_map27gDir, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_replay_lock.json"),
            lockBuilder.RenderJson(lockResult));
    }

    private DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationLockedReplayAuditResult BuildResult()
    {
        WriteMap27gOutput();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationLockedReplayAuditBuilder();
        return builder.Build(_map27gDir, _outputDir);
    }

    [Fact]
    public void Build_WithValidInputs_VerdictIsComplete()
    {
        var r = BuildResult();
        Assert.Equal(
            "MAP27H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_LOCKED_REPLAY_AUDIT_COMPLETE",
            r.Verdict);
    }

    [Fact]
    public void Build_WithValidInputs_IsValidTrue()
    {
        var r = BuildResult();
        Assert.True(r.IsValid);
    }

    [Fact]
    public void Build_WithValidInputs_AuditStatusVerified()
    {
        var r = BuildResult();
        Assert.Equal("VERIFIED_LOCKED_REPLAY_SOURCE_SET", r.AuditStatus);
    }

    [Fact]
    public void Build_WithValidInputs_ReplayLockIdMatches()
    {
        var r = BuildResult();
        Assert.True(r.ReplayLockIdMatches);
    }

    [Fact]
    public void Build_WithValidInputs_SourceAndRecomputedIdAreEqual()
    {
        var r = BuildResult();
        Assert.Equal(r.SourceReplayLockId, r.RecomputedReplayLockId);
    }

    [Fact]
    public void Build_WithValidInputs_ReplayLockIdHasPrefix()
    {
        var r = BuildResult();
        Assert.StartsWith("map_00_replay_lock_", r.RecomputedReplayLockId);
    }

    [Fact]
    public void Build_WithValidInputs_LockedFileCount8()
    {
        var r = BuildResult();
        Assert.Equal(8, r.LockedFileCount);
        Assert.Equal(8, r.LockedFiles.Count);
    }

    [Fact]
    public void Build_WithValidInputs_HashMatchCount8()
    {
        var r = BuildResult();
        Assert.Equal(8, r.LockedFileHashMatchCount);
    }

    [Fact]
    public void Build_WithValidInputs_HashMismatchCount0()
    {
        var r = BuildResult();
        Assert.Equal(0, r.LockedFileHashMismatchCount);
    }

    [Fact]
    public void Build_WithValidInputs_HashMissingCount0()
    {
        var r = BuildResult();
        Assert.Equal(0, r.LockedFileHashMissingCount);
    }

    [Fact]
    public void Build_WithValidInputs_CheckCount45()
    {
        var r = BuildResult();
        Assert.Equal(45, r.CheckCount);
    }

    [Fact]
    public void Build_WithValidInputs_AllChecksPass()
    {
        var r = BuildResult();
        Assert.Equal(45, r.PassedCheckCount);
        Assert.Equal(0,  r.FailedCheckCount);
    }

    [Fact]
    public void Build_WithValidInputs_AllLockedFilesHaveVerifiedStatus()
    {
        var r = BuildResult();
        Assert.All(r.LockedFiles, f => Assert.Equal("LOCKED_FILE_VERIFIED", f.AuditStatus));
    }

    [Fact]
    public void Build_WithValidInputs_AllLockedFilesExist()
    {
        var r = BuildResult();
        Assert.All(r.LockedFiles, f => Assert.True(f.Exists));
    }

    [Fact]
    public void Build_WithValidInputs_AllLockedFilesHashMatch()
    {
        var r = BuildResult();
        Assert.All(r.LockedFiles, f => Assert.True(f.HashMatches));
    }

    [Fact]
    public void Build_WithValidInputs_AllLockedFilesHaveStoredAndRecomputedSha256()
    {
        var r = BuildResult();
        Assert.All(r.LockedFiles, f =>
        {
            Assert.NotEmpty(f.StoredSha256);
            Assert.NotEmpty(f.RecomputedSha256);
        });
    }

    [Fact]
    public void Build_WithValidInputs_File1Role_IsAcceptanceGateResultJson()
    {
        var r = BuildResult();
        Assert.Equal("ACCEPTANCE_GATE_RESULT_JSON", r.LockedFiles[0].FileRole);
    }

    [Fact]
    public void Build_WithValidInputs_File1Stage_IsMap27f()
    {
        var r = BuildResult();
        Assert.Equal("MAP-27F", r.LockedFiles[0].SourceStage);
    }

    [Fact]
    public void Build_WithValidInputs_File2Role_IsTileMaterializerResultJson()
    {
        var r = BuildResult();
        Assert.Equal("TILE_MATERIALIZER_RESULT_JSON", r.LockedFiles[1].FileRole);
    }

    [Fact]
    public void Build_WithValidInputs_Only1Map27fFileInLockedSet()
    {
        var r = BuildResult();
        Assert.Equal(1, r.LockedFiles.Count(f => f.SourceStage == "MAP-27F"));
    }

    [Fact]
    public void Build_WithValidInputs_7Map27cFilesInLockedSet()
    {
        var r = BuildResult();
        Assert.Equal(7, r.LockedFiles.Count(f => f.SourceStage == "MAP-27C"));
    }

    [Fact]
    public void Build_WithValidInputs_SandboxOnlyTrue()
    {
        var r = BuildResult();
        Assert.True(r.SandboxOnly);
    }

    [Fact]
    public void Build_WithValidInputs_PzRuntimeMaterializedFalse()
    {
        var r = BuildResult();
        Assert.False(r.PzRuntimeMaterialized);
    }

    [Fact]
    public void Build_WithValidInputs_WriterReadyFalse()
    {
        var r = BuildResult();
        Assert.False(r.WriterReady);
    }

    [Fact]
    public void Build_WithValidInputs_RuntimeValidFalse()
    {
        var r = BuildResult();
        Assert.False(r.RuntimeValid);
    }

    [Fact]
    public void Build_WithValidInputs_MaterializedFalse()
    {
        var r = BuildResult();
        Assert.False(r.Materialized);
    }

    [Fact]
    public void Build_WithValidInputs_RuntimeProofClaimedFalse()
    {
        var r = BuildResult();
        Assert.False(r.RuntimeProofClaimed);
    }

    [Fact]
    public void Build_WithValidInputs_PublicPlayablePackagingClaimedFalse()
    {
        var r = BuildResult();
        Assert.False(r.PublicPlayablePackagingClaimed);
    }

    [Fact]
    public void Build_WithValidInputs_AuditStageIsCorrect()
    {
        var r = BuildResult();
        Assert.Equal("SANDBOX_WRITER_TILE_MATERIALIZATION_LOCKED_REPLAY_AUDIT", r.AuditStage);
    }

    [Fact]
    public void Build_WithValidInputs_AuditModeIsCorrect()
    {
        var r = BuildResult();
        Assert.Equal("VERIFY_MAP27G1_REPLAY_LOCK_HASHES_AND_LOCK_ID_ONLY", r.AuditMode);
    }

    [Fact]
    public void Build_WithValidInputs_NextAllowedExperimentName()
    {
        var r = BuildResult();
        Assert.Equal("MAP-27I_SANDBOX_WRITER_LOCKED_MATERIALIZATION_REPLAY_DRY_RUN", r.NextAllowedExperimentName);
    }

    [Fact]
    public void Build_WithValidInputs_NextAllowedExperimentStatus()
    {
        var r = BuildResult();
        Assert.Equal("SANDBOX_ONLY_NOT_RUNTIME", r.NextAllowedExperimentStatus);
    }

    [Fact]
    public void Build_WithValidInputs_SourceReplayLockFileCountIs8()
    {
        var r = BuildResult();
        Assert.Equal(8, r.SourceReplayLockFileCount);
    }

    [Fact]
    public void Build_WithValidInputs_SourceReplayLockStatusLocked()
    {
        var r = BuildResult();
        Assert.Equal("LOCKED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY", r.SourceReplayLockStatus);
    }

    [Fact]
    public void Build_WithValidInputs_SourceReplayLockIsValidTrue()
    {
        var r = BuildResult();
        Assert.True(r.SourceReplayLockIsValid);
    }

    [Fact]
    public void Build_WithValidInputs_SourceReplayLockSha256NotEmpty()
    {
        var r = BuildResult();
        Assert.NotEmpty(r.SourceReplayLockSha256);
    }

    [Fact]
    public void Build_WithValidInputs_LockedFilesAllLockedForReplay()
    {
        var r = BuildResult();
        Assert.All(r.LockedFiles, f => Assert.True(f.LockedForReplay));
    }

    [Fact]
    public void Build_WithValidInputs_LockedFilesAllNotRuntimeConsumable()
    {
        var r = BuildResult();
        Assert.All(r.LockedFiles, f => Assert.False(f.RuntimeConsumable));
    }

    [Fact]
    public void Build_WithValidInputs_LockedFilesAllNotWriterConsumable()
    {
        var r = BuildResult();
        Assert.All(r.LockedFiles, f => Assert.False(f.WriterConsumable));
    }

    [Fact]
    public void Build_MissingReplayLockRoot_ReturnsInvalidVerdict()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationLockedReplayAuditBuilder();
        var r = builder.Build(
            Path.Combine(_tempDir, "nonexistent.local", "map_00"),
            _outputDir);
        Assert.False(r.IsValid);
        Assert.Equal(
            "MAP27H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_LOCKED_REPLAY_AUDIT_INVALID",
            r.Verdict);
        Assert.NotEmpty(r.Errors);
    }

    [Fact]
    public void Build_WithTamperedFile_HashMismatchFails()
    {
        WriteMap27gOutput();
        // Tamper one of the locked MAP-27C files after MAP-27G lock was built
        string tamperedFile = Path.Combine(_map27cDir, "map_00.sandbox_writer_tile_material_palette.json");
        File.WriteAllText(tamperedFile, "{\"tampered\":true}");

        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationLockedReplayAuditBuilder();
        var r = builder.Build(_map27gDir, _outputDir);

        Assert.False(r.IsValid);
        Assert.True(r.LockedFileHashMismatchCount > 0);
        Assert.False(r.ReplayLockIdMatches);
        Assert.Equal("AUDIT_FAILED", r.AuditStatus);
    }

    [Fact]
    public void Build_WithValidInputs_RoleOrderIsCorrect()
    {
        var r = BuildResult();
        Assert.Equal("ACCEPTANCE_GATE_RESULT_JSON",             r.LockedFiles[0].FileRole);
        Assert.Equal("TILE_MATERIALIZER_RESULT_JSON",           r.LockedFiles[1].FileRole);
        Assert.Equal("MATERIALIZED_CELLS_CSV",                  r.LockedFiles[2].FileRole);
        Assert.Equal("MATERIAL_PALETTE_JSON",                   r.LockedFiles[3].FileRole);
        Assert.Equal("LAYER_STACK_JSON",                        r.LockedFiles[4].FileRole);
        Assert.Equal("MATERIALIZATION_REPLAY_LOG_JSON",         r.LockedFiles[5].FileRole);
        Assert.Equal("MATERIALIZATION_OWNERSHIP_SUMMARY_JSON",  r.LockedFiles[6].FileRole);
        Assert.Equal("MATERIALIZER_FORBIDDEN_OUTPUT_GUARD_JSON", r.LockedFiles[7].FileRole);
    }

    [Fact]
    public void Build_WithValidInputs_NoErrors()
    {
        var r = BuildResult();
        Assert.Empty(r.Errors);
    }

    [Fact]
    public void Build_WithValidInputs_FormatIsCorrect()
    {
        var r = BuildResult();
        Assert.Equal(
            "MAP-27H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_LOCKED_REPLAY_AUDIT",
            r.Format);
    }

    [Fact]
    public void Build_WithValidInputs_MapIdIsMap00()
    {
        var r = BuildResult();
        Assert.Equal("map_00", r.MapId);
    }
}
