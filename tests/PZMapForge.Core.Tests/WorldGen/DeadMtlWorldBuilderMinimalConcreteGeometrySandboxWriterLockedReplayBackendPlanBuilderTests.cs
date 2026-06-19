using System.Text.Json;
using PZMapForge.Core.WorldGen;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanBuilderTests
    : IDisposable
{
    private readonly string _tempDir;
    private readonly string _map27iDir;
    private readonly string _outputDir;

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanBuilderTests()
    {
        _tempDir   = Path.Combine(Path.GetTempPath(), "pzmapforge-map27j-core-test.local", Guid.NewGuid().ToString());
        _map27iDir = Path.Combine(_tempDir, "map27i.local", "map_00");
        _outputDir = Path.Combine(_tempDir, "output.local", "map_00");
        Directory.CreateDirectory(_map27iDir);
        Directory.CreateDirectory(_outputDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private const string ValidDigest = "15ca210cf7f96dc91e23cfb2f2556173a9b38c638adc7b6aea08104d3681334a";

    private void WriteMap27iFiles(
        string dryRunStatus     = "LOCKED_REPLAY_DRY_RUN_COMPLETE",
        string verdict          = "MAP27I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_MATERIALIZATION_REPLAY_DRY_RUN_COMPLETE",
        bool   isValid          = true,
        int    checkCount       = 53,
        int    passedCheckCount = 53,
        int    failedCheckCount = 0,
        string? digest          = null,
        int    cellCount        = 5340,
        int    wallCount        = 850,
        int    floorCount       = 2444,
        int    accessCount      = 148,
        int    lotCount         = 1898,
        int    residualCount    = 0,
        int    materialKinds    = 5)
    {
        string lockedDigest = digest ?? ValidDigest;
        var opts = new JsonSerializerOptions { WriteIndented = true };
        var json = new
        {
            format                                  = "MAP-27I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_MATERIALIZATION_REPLAY_DRY_RUN",
            dry_run_status                          = dryRunStatus,
            verdict,
            is_valid                                = isValid,
            check_count                             = checkCount,
            passed_check_count                      = passedCheckCount,
            failed_check_count                      = failedCheckCount,
            locked_replay_digest                    = lockedDigest,
            materialized_cell_count                 = cellCount,
            building_wall_candidate_cell_count      = wallCount,
            building_floor_candidate_cell_count     = floorCount,
            access_edge_cell_count                  = accessCount,
            lot_space_cell_count                    = lotCount,
            component_residual_cell_count           = residualCount,
            material_kind_count                     = materialKinds,
            sandbox_only                            = true,
            writer_ready                            = false,
            runtime_valid                           = false,
            materialized                            = false,
            runtime_proof_claimed                   = false,
            public_playable_packaging_claimed       = false,
        };
        File.WriteAllText(
            Path.Combine(_map27iDir, "map_00.minimal_concrete_geometry_sandbox_writer_locked_materialization_replay_dry_run.json"),
            JsonSerializer.Serialize(json, opts));
        File.WriteAllText(
            Path.Combine(_map27iDir, "map_00.minimal_concrete_geometry_sandbox_writer_locked_materialization_replay_dry_run.summary.txt"),
            "MAP-27I summary\n");
        File.WriteAllText(
            Path.Combine(_map27iDir, "map_00.sandbox_writer_locked_replay_material_counts.csv"),
            "material_kind,cell_count,source\nWALL,850,CSV\nFLOOR,2444,CSV\nACCESS,148,CSV\nLOT,1898,CSV\nCOMPONENT,0,CSV\n");
        File.WriteAllText(
            Path.Combine(_map27iDir, "map_00.sandbox_writer_locked_replay_source_manifest.json"),
            "{\"source_files\":[]}");
        File.WriteAllText(
            Path.Combine(_map27iDir, "map_00.sandbox_writer_locked_replay_digest.json"),
            JsonSerializer.Serialize(new { locked_replay_digest = lockedDigest }, opts));
        File.WriteAllText(
            Path.Combine(_map27iDir, "map_00.sandbox_writer_locked_replay_forbidden_output_guard.json"),
            "{\"forbidden_output_guard\":true,\"all_clean\":true}");
    }

    private DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanResult BuildResult(
        string? dryRunStatus = null, string? verdict = null, bool isValid = true,
        int checkCount = 53, int passedCheckCount = 53, int failedCheckCount = 0,
        string? digest = null)
    {
        WriteMap27iFiles(
            dryRunStatus     ?? "LOCKED_REPLAY_DRY_RUN_COMPLETE",
            verdict          ?? "MAP27I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_MATERIALIZATION_REPLAY_DRY_RUN_COMPLETE",
            isValid, checkCount, passedCheckCount, failedCheckCount, digest);
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanBuilder();
        return builder.Build(_map27iDir, _outputDir);
    }

    // -----------------------------------------------------------------------
    // Valid fixture tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_WithValidInputs_VerdictIsComplete()
    {
        var r = BuildResult();
        Assert.Equal(
            "MAP27J_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_PLAN_COMPLETE",
            r.Verdict);
    }

    [Fact]
    public void Build_WithValidInputs_IsValidTrue()
    {
        var r = BuildResult();
        Assert.True(r.IsValid);
    }

    [Fact]
    public void Build_WithValidInputs_BackendPlanStatusComplete()
    {
        var r = BuildResult();
        Assert.Equal("BACKEND_PLAN_COMPLETE", r.BackendPlanStatus);
    }

    [Fact]
    public void Build_WithValidInputs_CheckCount51()
    {
        var r = BuildResult();
        Assert.Equal(51, r.CheckCount);
    }

    [Fact]
    public void Build_WithValidInputs_AllChecksPass()
    {
        var r = BuildResult();
        Assert.Equal(51, r.PassedCheckCount);
        Assert.Equal(0,  r.FailedCheckCount);
    }

    [Fact]
    public void Build_WithValidInputs_SourceDryRunStatusComplete()
    {
        var r = BuildResult();
        Assert.Equal("LOCKED_REPLAY_DRY_RUN_COMPLETE", r.SourceDryRunStatus);
    }

    [Fact]
    public void Build_WithValidInputs_SourceCheckCount53()
    {
        var r = BuildResult();
        Assert.Equal(53, r.SourceCheckCount);
    }

    [Fact]
    public void Build_WithValidInputs_OperationPlanCount5()
    {
        var r = BuildResult();
        Assert.Equal(5, r.OperationPlanCount);
        Assert.Equal(5, r.BackendOperationRecords.Count);
    }

    [Fact]
    public void Build_WithValidInputs_TotalPlannedCells5340()
    {
        var r = BuildResult();
        Assert.Equal(5340, r.OperationPlanGroups.TotalPlannedCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_WallOpCellCount850()
    {
        var r = BuildResult();
        var op = r.BackendOperationRecords.Single(o => o.SourceMaterialBucket == "WALL");
        Assert.Equal(850, op.PlannedCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_FloorOpCellCount2444()
    {
        var r = BuildResult();
        var op = r.BackendOperationRecords.Single(o => o.SourceMaterialBucket == "FLOOR");
        Assert.Equal(2444, op.PlannedCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_AccessOpCellCount148()
    {
        var r = BuildResult();
        var op = r.BackendOperationRecords.Single(o => o.SourceMaterialBucket == "ACCESS");
        Assert.Equal(148, op.PlannedCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_LotOpCellCount1898()
    {
        var r = BuildResult();
        var op = r.BackendOperationRecords.Single(o => o.SourceMaterialBucket == "LOT");
        Assert.Equal(1898, op.PlannedCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_ComponentOpCellCount0()
    {
        var r = BuildResult();
        var op = r.BackendOperationRecords.Single(o => o.SourceMaterialBucket == "COMPONENT");
        Assert.Equal(0, op.PlannedCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_NonEmptyGroups4()
    {
        var r = BuildResult();
        Assert.Equal(4, r.OperationPlanGroups.NonEmptyOperationGroupCount);
    }

    [Fact]
    public void Build_WithValidInputs_EmptyGroups1()
    {
        var r = BuildResult();
        Assert.Equal(1, r.OperationPlanGroups.EmptyOperationGroupCount);
    }

    [Fact]
    public void Build_WithValidInputs_LargestGroupIsFloor()
    {
        var r = BuildResult();
        Assert.Equal("FLOOR", r.OperationPlanGroups.LargestOperationGroup);
        Assert.Equal(2444,    r.OperationPlanGroups.LargestOperationGroupCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_AllOperationsSandboxOnly()
    {
        var r = BuildResult();
        Assert.All(r.BackendOperationRecords, op => Assert.True(op.SandboxOnly));
    }

    [Fact]
    public void Build_WithValidInputs_NoOperationsWriterConsumable()
    {
        var r = BuildResult();
        Assert.All(r.BackendOperationRecords, op => Assert.False(op.WriterConsumable));
    }

    [Fact]
    public void Build_WithValidInputs_NoOperationsRuntimeConsumable()
    {
        var r = BuildResult();
        Assert.All(r.BackendOperationRecords, op => Assert.False(op.RuntimeConsumable));
    }

    [Fact]
    public void Build_WithValidInputs_NoOperationsEmitRuntimeFile()
    {
        var r = BuildResult();
        Assert.All(r.BackendOperationRecords, op => Assert.False(op.EmitsRuntimeFile));
    }

    [Fact]
    public void Build_WithValidInputs_NoOperationsEmitBinaryFile()
    {
        var r = BuildResult();
        Assert.All(r.BackendOperationRecords, op => Assert.False(op.EmitsBinaryFile));
    }

    [Fact]
    public void Build_WithValidInputs_NoOperationsEmitLuaFile()
    {
        var r = BuildResult();
        Assert.All(r.BackendOperationRecords, op => Assert.False(op.EmitsLuaFile));
    }

    [Fact]
    public void Build_WithValidInputs_NoOperationsEmitInstallPath()
    {
        var r = BuildResult();
        Assert.All(r.BackendOperationRecords, op => Assert.False(op.EmitsInstallPath));
    }

    [Fact]
    public void Build_WithValidInputs_AllOperationsRequireLockedReplayDigest()
    {
        var r = BuildResult();
        Assert.All(r.BackendOperationRecords, op => Assert.True(op.RequiresLockedReplayDigest));
    }

    [Fact]
    public void Build_WithValidInputs_AllOperationsCarrySourceDigest()
    {
        var r = BuildResult();
        Assert.All(r.BackendOperationRecords, op =>
            Assert.Equal(r.SourceLockedReplayDigest, op.SourceLockedReplayDigest));
    }

    [Fact]
    public void Build_WithValidInputs_SourceManifest6FilesHashed()
    {
        var r = BuildResult();
        Assert.Equal(6, r.SourceManifestFiles.Count);
        Assert.All(r.SourceManifestFiles, f => Assert.True(f.Exists));
        Assert.All(r.SourceManifestFiles, f => Assert.NotEmpty(f.Sha256));
    }

    [Fact]
    public void Build_WithValidInputs_SourceManifestRolesCorrect()
    {
        var r = BuildResult();
        Assert.Equal("DRY_RUN_RESULT_JSON",                      r.SourceManifestFiles[0].FileRole);
        Assert.Equal("DRY_RUN_SUMMARY_TXT",                     r.SourceManifestFiles[1].FileRole);
        Assert.Equal("LOCKED_REPLAY_MATERIAL_COUNTS_CSV",       r.SourceManifestFiles[2].FileRole);
        Assert.Equal("LOCKED_REPLAY_SOURCE_MANIFEST_JSON",      r.SourceManifestFiles[3].FileRole);
        Assert.Equal("LOCKED_REPLAY_DIGEST_JSON",               r.SourceManifestFiles[4].FileRole);
        Assert.Equal("LOCKED_REPLAY_FORBIDDEN_OUTPUT_GUARD_JSON", r.SourceManifestFiles[5].FileRole);
    }

    [Fact]
    public void Build_WithValidInputs_ForbiddenArtifactScanPass()
    {
        var r = BuildResult();
        Assert.Contains("PASS", r.ForbiddenArtifactScan, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WithValidInputs_SandboxOnlyTrue()
    {
        var r = BuildResult();
        Assert.True(r.SandboxOnly);
    }

    [Fact]
    public void Build_WithValidInputs_SandboxBackendPlanOnlyTrue()
    {
        var r = BuildResult();
        Assert.True(r.SandboxBackendPlanOnly);
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
    public void Build_WithValidInputs_NextForbiddenStepsCount11()
    {
        var r = BuildResult();
        Assert.Equal(11, r.NextForbiddenSteps.Count);
    }

    [Fact]
    public void Build_WithValidInputs_FormatIsCorrect()
    {
        var r = BuildResult();
        Assert.Equal(
            "MAP-27J_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_PLAN",
            r.Format);
    }

    [Fact]
    public void Build_WithValidInputs_MapIdIsMap00()
    {
        var r = BuildResult();
        Assert.Equal("map_00", r.MapId);
    }

    [Fact]
    public void Build_WithValidInputs_NoErrors()
    {
        var r = BuildResult();
        Assert.Empty(r.Errors);
    }

    [Fact]
    public void Build_WithValidInputs_OperationOrdersAre1To5()
    {
        var r = BuildResult();
        for (int i = 0; i < 5; i++)
            Assert.Equal(i + 1, r.BackendOperationRecords[i].OperationOrder);
    }

    [Fact]
    public void Build_WithValidInputs_OperationIdsContainOrderPrefix()
    {
        var r = BuildResult();
        Assert.StartsWith("MAP27J_OP_001_", r.BackendOperationRecords[0].OperationId);
        Assert.StartsWith("MAP27J_OP_002_", r.BackendOperationRecords[1].OperationId);
        Assert.StartsWith("MAP27J_OP_005_", r.BackendOperationRecords[4].OperationId);
    }

    [Fact]
    public void Build_WithValidInputs_BackendPayloadKindIsFutureTileWriteBucket()
    {
        var r = BuildResult();
        Assert.All(r.BackendOperationRecords, op =>
            Assert.Equal("FUTURE_TILE_WRITE_BUCKET", op.BackendPayloadKind));
    }

    [Fact]
    public void Build_WithValidInputs_RenderersNonEmpty()
    {
        WriteMap27iFiles();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanBuilder();
        var r = builder.Build(_map27iDir, _outputDir);
        Assert.NotEmpty(builder.RenderJson(r));
        Assert.NotEmpty(builder.RenderMarkdown(r));
        Assert.NotEmpty(builder.RenderCsv(r));
        Assert.NotEmpty(builder.RenderSummary(r));
        Assert.NotEmpty(builder.RenderOperationPlanJson(r));
        Assert.NotEmpty(builder.RenderOperationPlanCsv(r));
        Assert.NotEmpty(builder.RenderSourceManifestJson(r));
        Assert.NotEmpty(builder.RenderForbiddenOutputGuardJson(r));
    }

    // -----------------------------------------------------------------------
    // Failure path tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_MissingSourceRoot_ReturnsInvalidVerdict()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanBuilder();
        var r = builder.Build(Path.Combine(_tempDir, "nonexistent.local", "map_00"), _outputDir);
        Assert.False(r.IsValid);
        Assert.Contains("INVALID", r.Verdict);
        Assert.NotEmpty(r.Errors);
    }

    [Fact]
    public void Build_MissingSourceJson_ReturnsInvalidVerdict()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanBuilder();
        var r = builder.Build(_map27iDir, _outputDir);
        Assert.False(r.IsValid);
        Assert.Contains("INVALID", r.Verdict);
        Assert.NotEmpty(r.Errors);
    }

    [Fact]
    public void Build_WithSourceFailedCheckCount_IsInvalid()
    {
        var r = BuildResult(failedCheckCount: 1, passedCheckCount: 52, isValid: false,
            dryRunStatus: "LOCKED_REPLAY_DRY_RUN_FAILED",
            verdict: "MAP27I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_MATERIALIZATION_REPLAY_DRY_RUN_INVALID");
        Assert.False(r.IsValid);
        Assert.Contains("INVALID", r.Verdict);
    }

    [Fact]
    public void Build_WithSourceIsValidFalse_IsInvalid()
    {
        var r = BuildResult(isValid: false,
            dryRunStatus: "LOCKED_REPLAY_DRY_RUN_FAILED",
            verdict: "MAP27I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_MATERIALIZATION_REPLAY_DRY_RUN_INVALID",
            failedCheckCount: 2, passedCheckCount: 51);
        Assert.False(r.IsValid);
        Assert.Contains("INVALID", r.Verdict);
    }

    [Fact]
    public void Build_WithEmptyDigest_DigestPresentCheckFails()
    {
        var r = BuildResult(digest: "");
        var check = r.Checks.Single(c => c.CheckId == "MAP27I_LOCKED_REPLAY_DIGEST_PRESENT");
        Assert.Equal("FAIL", check.CheckStatus);
        Assert.False(r.IsValid);
    }

    [Fact]
    public void Build_WithSteamappsInOutputRoot_ForbiddenScanFails()
    {
        WriteMap27iFiles();
        Directory.CreateDirectory(Path.Combine(_outputDir, "steamapps"));
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanBuilder();
        var r = builder.Build(_map27iDir, _outputDir);
        var check = r.Checks.Single(c => c.CheckId == "POST_BACKEND_PLAN_FORBIDDEN_SCAN_PASS");
        Assert.Equal("FAIL", check.CheckStatus);
        Assert.Contains("FAIL", r.ForbiddenArtifactScan);
        Assert.False(r.IsValid);
    }

    [Fact]
    public void Build_WithBinFileInOutputRoot_ForbiddenScanFails()
    {
        WriteMap27iFiles();
        File.WriteAllText(Path.Combine(_outputDir, "test.bin"), "data");
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanBuilder();
        var r = builder.Build(_map27iDir, _outputDir);
        var check = r.Checks.Single(c => c.CheckId == "POST_BACKEND_PLAN_FORBIDDEN_SCAN_PASS");
        Assert.Equal("FAIL", check.CheckStatus);
        Assert.False(r.IsValid);
    }
}
