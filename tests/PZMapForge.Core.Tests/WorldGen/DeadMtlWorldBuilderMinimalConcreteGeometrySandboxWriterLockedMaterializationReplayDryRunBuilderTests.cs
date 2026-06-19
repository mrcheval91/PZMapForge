using System.Text.Json;
using PZMapForge.Core.WorldGen;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunBuilderTests
    : IDisposable
{
    private readonly string _tempDir;
    private readonly string _map27fDir;
    private readonly string _map27cDir;
    private readonly string _map27gDir;
    private readonly string _map27hDir;
    private readonly string _outputDir;

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunBuilderTests()
    {
        _tempDir   = Path.Combine(Path.GetTempPath(), "pzmapforge-map27i-core-test.local", Guid.NewGuid().ToString());
        _map27fDir = Path.Combine(_tempDir, "map27f.local", "map_00");
        _map27cDir = Path.Combine(_tempDir, "map27c.local", "map_00");
        _map27gDir = Path.Combine(_tempDir, "map27g.local", "map_00");
        _map27hDir = Path.Combine(_tempDir, "map27h.local", "map_00");
        _outputDir = Path.Combine(_tempDir, "output.local", "map_00");
        Directory.CreateDirectory(_map27fDir);
        Directory.CreateDirectory(_map27cDir);
        Directory.CreateDirectory(_map27gDir);
        Directory.CreateDirectory(_map27hDir);
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
            format  = "MAP-27F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_ACCEPTANCE_GATE",
            verdict = "MAP27F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_ACCEPTANCE_GATE_COMPLETE",
            acceptance_gate_status                   = "ACCEPTED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY",
            accepted_for_next_sandbox_experiment     = true,
            accepted_for_runtime_writer              = false,
            accepted_for_playable_export             = false,
            sandbox_only                             = true,
            sandbox_materialized_source              = true,
            visual_qa_overlay_written                = true,
            pz_runtime_materialized                  = false,
            materialized_cell_count                  = 5340,
            rendered_cell_count                      = 5340,
            count_match_summary                      = "MATERIALIZED_CELL_COUNT(5340) == RENDERED_CELL_COUNT(5340): MATCH",
            building_wall_candidate_cell_count       = 850,
            building_floor_candidate_cell_count      = 2444,
            access_edge_cell_count                   = 148,
            lot_space_cell_count                     = 1898,
            component_residual_cell_count            = 0,
            material_kind_count                      = 5,
            layer_kind_count                         = 5,
            overlay_png_width                        = 1024,
            overlay_png_height                       = 1024,
            is_valid                                 = true,
            writer_ready                             = false,
            runtime_valid                            = false,
            materialized                             = false,
            runtime_proof_claimed                    = false,
            public_playable_packaging_claimed        = false,
            check_count                              = 38,
            passed_check_count                       = 38,
            failed_check_count                       = 0,
            next_allowed_experiment_name             = "MAP-27G_SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOCK",
            next_allowed_experiment_status           = "SANDBOX_ONLY_NOT_RUNTIME",
        };
        var opts = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(
            Path.Combine(_map27fDir, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.json"),
            JsonSerializer.Serialize(json, opts));
        File.WriteAllText(
            Path.Combine(_map27fDir, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.md"),
            "# MAP-27F\n");
        File.WriteAllText(
            Path.Combine(_map27fDir, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.csv"),
            "check_order,check_id,check_status\n");
        File.WriteAllText(
            Path.Combine(_map27fDir, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.summary.txt"),
            "MAP-27F summary\n");
    }

    private void WriteMap27cFiles()
    {
        var json = new
        {
            format    = "MAP-27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0",
            verdict   = "MAP27C_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZER_V0_COMPLETE",
            is_valid  = true,
            sandbox_only = true,
            materialized_cell_count = 5340,
        };
        var opts = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(
            Path.Combine(_map27cDir, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json"),
            JsonSerializer.Serialize(json, opts));
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

    private void WriteMap27hOutput()
    {
        WriteMap27fFiles();
        WriteMap27cFiles();
        var lockBuilder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilder();
        var lockResult  = lockBuilder.Build(_map27fDir, _map27cDir, _map27gDir);
        File.WriteAllText(
            Path.Combine(_map27gDir, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_replay_lock.json"),
            lockBuilder.RenderJson(lockResult));

        var auditBuilder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationLockedReplayAuditBuilder();
        var auditResult  = auditBuilder.Build(_map27gDir, _map27hDir);
        File.WriteAllText(
            Path.Combine(_map27hDir, "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_locked_replay_audit.json"),
            auditBuilder.RenderJson(auditResult));
    }

    private DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunResult BuildResult()
    {
        WriteMap27hOutput();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunBuilder();
        return builder.Build(_map27hDir, _outputDir);
    }

    // -----------------------------------------------------------------------
    // Valid fixture tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_WithValidInputs_VerdictIsComplete()
    {
        var r = BuildResult();
        Assert.Equal(
            "MAP27I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_MATERIALIZATION_REPLAY_DRY_RUN_COMPLETE",
            r.Verdict);
    }

    [Fact]
    public void Build_WithValidInputs_IsValidTrue()
    {
        var r = BuildResult();
        Assert.True(r.IsValid);
    }

    [Fact]
    public void Build_WithValidInputs_DryRunStatusComplete()
    {
        var r = BuildResult();
        Assert.Equal("LOCKED_REPLAY_DRY_RUN_COMPLETE", r.DryRunStatus);
    }

    [Fact]
    public void Build_WithValidInputs_CheckCount44()
    {
        var r = BuildResult();
        Assert.Equal(44, r.CheckCount);
    }

    [Fact]
    public void Build_WithValidInputs_AllChecksPass()
    {
        var r = BuildResult();
        Assert.Equal(44, r.PassedCheckCount);
        Assert.Equal(0,  r.FailedCheckCount);
    }

    [Fact]
    public void Build_WithValidInputs_MaterializedCellCount5340()
    {
        var r = BuildResult();
        Assert.Equal(5340, r.MaterializedCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_WallCount850()
    {
        var r = BuildResult();
        Assert.Equal(850, r.BuildingWallCandidateCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_FloorCount2444()
    {
        var r = BuildResult();
        Assert.Equal(2444, r.BuildingFloorCandidateCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_AccessCount148()
    {
        var r = BuildResult();
        Assert.Equal(148, r.AccessEdgeCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_LotCount1898()
    {
        var r = BuildResult();
        Assert.Equal(1898, r.LotSpaceCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_ComponentResidualCount0()
    {
        var r = BuildResult();
        Assert.Equal(0, r.ComponentResidualCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_MaterialKindCount5()
    {
        var r = BuildResult();
        Assert.Equal(5, r.MaterialKindCount);
    }

    [Fact]
    public void Build_WithValidInputs_LayerKindCount5()
    {
        var r = BuildResult();
        Assert.Equal(5, r.LayerKindCount);
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
    public void Build_WithValidInputs_MissingCount0()
    {
        var r = BuildResult();
        Assert.Equal(0, r.LockedFileMissingCount);
    }

    [Fact]
    public void Build_WithValidInputs_AllLockedFilesExist()
    {
        var r = BuildResult();
        Assert.All(r.LockedFiles, f => Assert.True(f.Exists));
    }

    [Fact]
    public void Build_WithValidInputs_AllLockedFilesHashStillMatch()
    {
        var r = BuildResult();
        Assert.All(r.LockedFiles, f => Assert.True(f.HashStillMatches));
    }

    [Fact]
    public void Build_WithValidInputs_LockedReplayDigestNotEmpty()
    {
        var r = BuildResult();
        Assert.NotEmpty(r.LockedReplayDigest);
    }

    [Fact]
    public void Build_WithValidInputs_DigestIsDeterministic()
    {
        WriteMap27hOutput();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunBuilder();
        var r1 = builder.Build(_map27hDir, _outputDir);
        var r2 = builder.Build(_map27hDir, _outputDir);
        Assert.Equal(r1.LockedReplayDigest, r2.LockedReplayDigest);
    }

    [Fact]
    public void Build_WithValidInputs_CellsCsvSha256NotEmpty()
    {
        var r = BuildResult();
        Assert.NotEmpty(r.MaterializedCellsCsvSha256);
    }

    [Fact]
    public void Build_WithValidInputs_RoleOrderIsCorrect()
    {
        var r = BuildResult();
        Assert.Equal("ACCEPTANCE_GATE_RESULT_JSON",              r.LockedFiles[0].FileRole);
        Assert.Equal("TILE_MATERIALIZER_RESULT_JSON",            r.LockedFiles[1].FileRole);
        Assert.Equal("MATERIALIZED_CELLS_CSV",                   r.LockedFiles[2].FileRole);
        Assert.Equal("MATERIAL_PALETTE_JSON",                    r.LockedFiles[3].FileRole);
        Assert.Equal("LAYER_STACK_JSON",                         r.LockedFiles[4].FileRole);
        Assert.Equal("MATERIALIZATION_REPLAY_LOG_JSON",          r.LockedFiles[5].FileRole);
        Assert.Equal("MATERIALIZATION_OWNERSHIP_SUMMARY_JSON",   r.LockedFiles[6].FileRole);
        Assert.Equal("MATERIALIZER_FORBIDDEN_OUTPUT_GUARD_JSON", r.LockedFiles[7].FileRole);
    }

    [Fact]
    public void Build_WithValidInputs_SandboxOnlyTrue()
    {
        var r = BuildResult();
        Assert.True(r.SandboxOnly);
    }

    [Fact]
    public void Build_WithValidInputs_SandboxLockedReplayDryRunTrue()
    {
        var r = BuildResult();
        Assert.True(r.SandboxLockedReplayDryRun);
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
    public void Build_WithValidInputs_PzRuntimeMaterializedFalse()
    {
        var r = BuildResult();
        Assert.False(r.PzRuntimeMaterialized);
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
    public void Build_WithValidInputs_ForbiddenArtifactScanContainsPass()
    {
        var r = BuildResult();
        Assert.Contains("PASS", r.ForbiddenArtifactScan, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WithValidInputs_ClaimBoundaryAuditIsCorrect()
    {
        var r = BuildResult();
        Assert.Equal(
            "writer_ready=false | runtime_valid=false | materialized=false | runtime_proof_claimed=false | public_playable_packaging_claimed=false",
            r.ClaimBoundaryAudit);
    }

    [Fact]
    public void Build_WithValidInputs_NextForbiddenStepsCount11()
    {
        var r = BuildResult();
        Assert.Equal(11, r.NextForbiddenSteps.Count);
    }

    [Fact]
    public void Build_WithValidInputs_NextForbiddenStepsContainsExpectedEntries()
    {
        var r = BuildResult();
        Assert.Contains("LOT_PACK_RUNTIME_BINARY",          r.NextForbiddenSteps);
        Assert.Contains("WORLDGEN_OVERRIDE_LUA",            r.NextForbiddenSteps);
        Assert.Contains("RUNTIME_PROOF_CLAIM",              r.NextForbiddenSteps);
        Assert.Contains("PUBLIC_PLAYABLE_PACKAGING_CLAIM",  r.NextForbiddenSteps);
    }

    [Fact]
    public void Build_WithValidInputs_SourceAuditStatusVerified()
    {
        var r = BuildResult();
        Assert.Equal("VERIFIED_LOCKED_REPLAY_SOURCE_SET", r.SourceAuditStatus);
    }

    [Fact]
    public void Build_WithValidInputs_SourceReplayLockIdMatches()
    {
        var r = BuildResult();
        Assert.True(r.SourceReplayLockIdMatches);
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
            "MAP-27I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_MATERIALIZATION_REPLAY_DRY_RUN",
            r.Format);
    }

    [Fact]
    public void Build_WithValidInputs_MapIdIsMap00()
    {
        var r = BuildResult();
        Assert.Equal("map_00", r.MapId);
    }

    [Fact]
    public void Build_WithValidInputs_RenderersNonEmpty()
    {
        WriteMap27hOutput();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunBuilder();
        var r = builder.Build(_map27hDir, _outputDir);
        Assert.NotEmpty(builder.RenderJson(r));
        Assert.NotEmpty(builder.RenderMarkdown(r));
        Assert.NotEmpty(builder.RenderCsv(r));
        Assert.NotEmpty(builder.RenderSummary(r));
        Assert.NotEmpty(builder.RenderMaterialCountsCsv(r));
        Assert.NotEmpty(builder.RenderSourceManifestJson(r));
        Assert.NotEmpty(builder.RenderReplayDigestJson(r));
        Assert.NotEmpty(builder.RenderForbiddenOutputGuardJson(r));
    }

    // -----------------------------------------------------------------------
    // Failure path tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_MissingAuditRoot_ReturnsInvalidVerdict()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunBuilder();
        var r = builder.Build(
            Path.Combine(_tempDir, "nonexistent.local", "map_00"),
            _outputDir);
        Assert.False(r.IsValid);
        Assert.Contains("INVALID", r.Verdict);
        Assert.NotEmpty(r.Errors);
    }

    [Fact]
    public void Build_MissingAuditJson_ReturnsInvalidVerdict()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunBuilder();
        var r = builder.Build(_map27hDir, _outputDir);
        Assert.False(r.IsValid);
        Assert.Contains("INVALID", r.Verdict);
        Assert.NotEmpty(r.Errors);
    }

    [Fact]
    public void Build_WithTamperedLockedFile_HashMismatchFails()
    {
        WriteMap27hOutput();
        string tamperedFile = Path.Combine(_map27cDir, "map_00.sandbox_writer_tile_material_palette.json");
        File.WriteAllText(tamperedFile, "{\"tampered\":true}");

        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunBuilder();
        var r = builder.Build(_map27hDir, _outputDir);

        Assert.True(r.LockedFileHashMismatchCount > 0);
        Assert.False(r.IsValid);
        Assert.Contains("INVALID", r.Verdict);
    }

    [Fact]
    public void Build_WithSteamappsDirectoryInOutputRoot_ForbiddenScanFails()
    {
        WriteMap27hOutput();
        Directory.CreateDirectory(Path.Combine(_outputDir, "steamapps"));

        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunBuilder();
        var r = builder.Build(_map27hDir, _outputDir);

        var check = r.Checks.Single(c => c.CheckId == "POST_DRY_RUN_FORBIDDEN_SCAN_PASS");
        Assert.Equal("FAIL", check.CheckStatus);
        Assert.Contains("FAIL", r.ForbiddenArtifactScan);
        Assert.False(r.IsValid);
        Assert.Contains("INVALID", r.Verdict);
    }
}
