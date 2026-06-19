using System.Text.Json;
using PZMapForge.Core.WorldGen;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterBuilderTests
    : IDisposable
{
    private readonly string _tempDir;
    private readonly string _map27jDir;
    private readonly string _outputDir;

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterBuilderTests()
    {
        _tempDir   = Path.Combine(Path.GetTempPath(), "pzmapforge-map27k-core-test.local", Guid.NewGuid().ToString());
        _map27jDir = Path.Combine(_tempDir, "map27j.local", "map_00");
        _outputDir = Path.Combine(_tempDir, "output.local", "map_00");
        Directory.CreateDirectory(_map27jDir);
        Directory.CreateDirectory(_outputDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private const string ValidDigest = "7dc5cbc0ac100a93e8c5e1ba2106242825228942101083ca6eeae324582e3f30";

    private void WriteMap27jFiles(
        string planStatus       = "BACKEND_PLAN_COMPLETE",
        bool   isValid          = true,
        int    checkCount       = 51,
        int    passedCheckCount = 51,
        int    failedCheckCount = 0,
        int    opPlanCount      = 5,
        int    totalCells       = 5340,
        int    nonEmptyGroups   = 4,
        int    emptyGroups      = 1,
        string? digest          = null,
        string forbiddenScan    = "POST_BACKEND_PLAN_FORBIDDEN_SCAN PASS (0 forbidden artifacts in output root)")
    {
        string lockedDigest = digest ?? ValidDigest;
        var opts = new JsonSerializerOptions { WriteIndented = true };

        var ops = new object[]
        {
            new { operation_id = "MAP27J_OP_001_WRITE_WALL_CANDIDATE_BUCKET",     operation_kind = "WRITE_WALL_CANDIDATE_BUCKET",     source_material_bucket = "WALL",      planned_cell_count = 850,  backend_target_family = "BUILDING_WALL_LAYER_CANDIDATE",      backend_payload_kind = "FUTURE_TILE_WRITE_BUCKET" },
            new { operation_id = "MAP27J_OP_002_WRITE_FLOOR_CANDIDATE_BUCKET",    operation_kind = "WRITE_FLOOR_CANDIDATE_BUCKET",    source_material_bucket = "FLOOR",     planned_cell_count = 2444, backend_target_family = "BUILDING_FLOOR_LAYER_CANDIDATE",     backend_payload_kind = "FUTURE_TILE_WRITE_BUCKET" },
            new { operation_id = "MAP27J_OP_003_WRITE_ACCESS_EDGE_BUCKET",        operation_kind = "WRITE_ACCESS_EDGE_BUCKET",        source_material_bucket = "ACCESS",    planned_cell_count = 148,  backend_target_family = "ACCESS_EDGE_LAYER_CANDIDATE",        backend_payload_kind = "FUTURE_TILE_WRITE_BUCKET" },
            new { operation_id = "MAP27J_OP_004_WRITE_LOT_SPACE_BUCKET",          operation_kind = "WRITE_LOT_SPACE_BUCKET",          source_material_bucket = "LOT",       planned_cell_count = 1898, backend_target_family = "LOT_SPACE_LAYER_CANDIDATE",          backend_payload_kind = "FUTURE_TILE_WRITE_BUCKET" },
            new { operation_id = "MAP27J_OP_005_WRITE_COMPONENT_RESIDUAL_BUCKET", operation_kind = "WRITE_COMPONENT_RESIDUAL_BUCKET", source_material_bucket = "COMPONENT", planned_cell_count = 0,    backend_target_family = "COMPONENT_RESIDUAL_LAYER_CANDIDATE", backend_payload_kind = "FUTURE_TILE_WRITE_BUCKET" },
        };

        var planJson = new
        {
            format                           = "MAP-27J_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_PLAN",
            backend_plan_status              = planStatus,
            is_valid                         = isValid,
            check_count                      = checkCount,
            passed_check_count               = passedCheckCount,
            failed_check_count               = failedCheckCount,
            operation_plan_count             = opPlanCount,
            operation_plan_groups            = new
            {
                total_planned_cell_count         = totalCells,
                non_empty_operation_group_count  = nonEmptyGroups,
                empty_operation_group_count      = emptyGroups,
            },
            sandbox_only                             = true,
            sandbox_backend_plan_only                = true,
            writer_ready                             = false,
            runtime_valid                            = false,
            materialized                             = false,
            pz_runtime_materialized                  = false,
            runtime_proof_claimed                    = false,
            public_playable_packaging_claimed        = false,
            forbidden_artifact_scan                  = forbiddenScan,
            source_locked_replay_digest              = lockedDigest,
            backend_operation_records                = ops,
        };

        File.WriteAllText(
            Path.Combine(_map27jDir,
                "map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_plan.json"),
            JsonSerializer.Serialize(planJson, opts));

        var opPlanJson = new { backend_operation_records = ops };
        File.WriteAllText(
            Path.Combine(_map27jDir, "map_00.sandbox_writer_locked_replay_backend_operation_plan.json"),
            JsonSerializer.Serialize(opPlanJson, opts));
        File.WriteAllText(
            Path.Combine(_map27jDir, "map_00.sandbox_writer_locked_replay_backend_operation_plan.csv"),
            "operation_order,operation_kind,source_material_bucket,planned_cell_count\n");
        File.WriteAllText(
            Path.Combine(_map27jDir, "map_00.sandbox_writer_locked_replay_backend_source_manifest.json"),
            "{\"source_file_count\":6}");
        File.WriteAllText(
            Path.Combine(_map27jDir, "map_00.sandbox_writer_locked_replay_backend_forbidden_output_guard.json"),
            "{\"forbidden_output_guard\":true,\"all_clean\":true}");
    }

    private DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterResult BuildResult(
        string planStatus = "BACKEND_PLAN_COMPLETE", bool isValid = true,
        int checkCount = 51, int passedCheckCount = 51, int failedCheckCount = 0,
        string? digest = null)
    {
        WriteMap27jFiles(planStatus, isValid, checkCount, passedCheckCount, failedCheckCount, digest: digest);
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterBuilder();
        return builder.Build(_map27jDir, _outputDir);
    }

    // -----------------------------------------------------------------------
    // Valid fixture tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_WithValidInputs_VerdictIsComplete()
    {
        var r = BuildResult();
        Assert.Equal(
            "MAP27K_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_DRY_RUN_EMITTER_COMPLETE",
            r.Verdict);
    }

    [Fact]
    public void Build_WithValidInputs_IsValidTrue()
    {
        var r = BuildResult();
        Assert.True(r.IsValid);
    }

    [Fact]
    public void Build_WithValidInputs_EmitterStatusComplete()
    {
        var r = BuildResult();
        Assert.Equal("BACKEND_DRY_RUN_EMITTER_COMPLETE", r.EmitterStatus);
    }

    [Fact]
    public void Build_WithValidInputs_CheckCount49()
    {
        var r = BuildResult();
        Assert.Equal(49, r.CheckCount);
    }

    [Fact]
    public void Build_WithValidInputs_AllChecksPass()
    {
        var r = BuildResult();
        Assert.Equal(49, r.PassedCheckCount);
        Assert.Equal(0,  r.FailedCheckCount);
    }

    [Fact]
    public void Build_WithValidInputs_Reads5SourceFiles()
    {
        var r = BuildResult();
        Assert.Equal(5, r.SourceManifestFiles.Count);
        Assert.All(r.SourceManifestFiles, f => Assert.True(f.Exists));
        Assert.All(r.SourceManifestFiles, f => Assert.NotEmpty(f.Sha256));
    }

    [Fact]
    public void Build_WithValidInputs_Emits5OperationRecords()
    {
        var r = BuildResult();
        Assert.Equal(5, r.EmittedOperationCount);
        Assert.Equal(5, r.EmittedOperationRecords.Count);
    }

    [Fact]
    public void Build_WithValidInputs_WallCount850()
    {
        var r = BuildResult();
        Assert.Equal(850, r.EmittedOperationRecords.Single(o => o.SourceMaterialBucket == "WALL").PlannedCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_FloorCount2444()
    {
        var r = BuildResult();
        Assert.Equal(2444, r.EmittedOperationRecords.Single(o => o.SourceMaterialBucket == "FLOOR").PlannedCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_AccessCount148()
    {
        var r = BuildResult();
        Assert.Equal(148, r.EmittedOperationRecords.Single(o => o.SourceMaterialBucket == "ACCESS").PlannedCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_LotCount1898()
    {
        var r = BuildResult();
        Assert.Equal(1898, r.EmittedOperationRecords.Single(o => o.SourceMaterialBucket == "LOT").PlannedCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_ComponentCount0()
    {
        var r = BuildResult();
        Assert.Equal(0, r.EmittedOperationRecords.Single(o => o.SourceMaterialBucket == "COMPONENT").PlannedCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_TotalPlannedCells5340()
    {
        var r = BuildResult();
        Assert.Equal(5340, r.EmittedTotalPlannedCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_FloorLargestBucket()
    {
        var r = BuildResult();
        Assert.Equal("FLOOR", r.LargestEmittedOperationBucket);
        Assert.Equal(2444,    r.LargestEmittedOperationCellCount);
    }

    [Fact]
    public void Build_WithValidInputs_NonEmptyCount4()
    {
        var r = BuildResult();
        Assert.Equal(4, r.NonEmptyEmittedOperationCount);
    }

    [Fact]
    public void Build_WithValidInputs_EmptyCount1()
    {
        var r = BuildResult();
        Assert.Equal(1, r.EmptyEmittedOperationCount);
    }

    [Fact]
    public void Build_WithValidInputs_AllEmittedSandboxOnly()
    {
        var r = BuildResult();
        Assert.All(r.EmittedOperationRecords, op => Assert.True(op.SandboxOnly));
    }

    [Fact]
    public void Build_WithValidInputs_NoEmittedWriterConsumable()
    {
        var r = BuildResult();
        Assert.All(r.EmittedOperationRecords, op => Assert.False(op.WriterConsumable));
    }

    [Fact]
    public void Build_WithValidInputs_NoEmittedRuntimeConsumable()
    {
        var r = BuildResult();
        Assert.All(r.EmittedOperationRecords, op => Assert.False(op.RuntimeConsumable));
    }

    [Fact]
    public void Build_WithValidInputs_NoEmittedEmitsRuntimeFile()
    {
        var r = BuildResult();
        Assert.All(r.EmittedOperationRecords, op => Assert.False(op.EmitsRuntimeFile));
    }

    [Fact]
    public void Build_WithValidInputs_NoEmittedEmitsBinaryFile()
    {
        var r = BuildResult();
        Assert.All(r.EmittedOperationRecords, op => Assert.False(op.EmitsBinaryFile));
    }

    [Fact]
    public void Build_WithValidInputs_NoEmittedEmitsLuaFile()
    {
        var r = BuildResult();
        Assert.All(r.EmittedOperationRecords, op => Assert.False(op.EmitsLuaFile));
    }

    [Fact]
    public void Build_WithValidInputs_NoEmittedEmitsInstallPath()
    {
        var r = BuildResult();
        Assert.All(r.EmittedOperationRecords, op => Assert.False(op.EmitsInstallPath));
    }

    [Fact]
    public void Build_WithValidInputs_AllEmittedRequireLockedReplayDigest()
    {
        var r = BuildResult();
        Assert.All(r.EmittedOperationRecords, op => Assert.True(op.RequiresLockedReplayDigest));
    }

    [Fact]
    public void Build_WithValidInputs_EmissionDigestPresent()
    {
        var r = BuildResult();
        Assert.NotEmpty(r.BackendDryRunEmissionDigest);
        var check = r.Checks.Single(c => c.CheckId == "BACKEND_DRY_RUN_EMISSION_DIGEST_PRESENT");
        Assert.Equal("PASS", check.CheckStatus);
    }

    [Fact]
    public void Build_WithValidInputs_ForbiddenScanPass()
    {
        var r = BuildResult();
        Assert.Contains("PASS", r.ForbiddenArtifactScan, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WithValidInputs_ClaimBoundaryFalse()
    {
        var r = BuildResult();
        Assert.False(r.WriterReady);
        Assert.False(r.RuntimeValid);
        Assert.False(r.Materialized);
        Assert.False(r.PzRuntimeMaterialized);
        Assert.False(r.RuntimeProofClaimed);
        Assert.False(r.PublicPlayablePackagingClaimed);
    }

    [Fact]
    public void Build_WithValidInputs_SandboxFlagsTrue()
    {
        var r = BuildResult();
        Assert.True(r.SandboxOnly);
        Assert.True(r.SandboxBackendDryRunEmitterOnly);
    }

    [Fact]
    public void Build_WithValidInputs_FormatAndMapId()
    {
        var r = BuildResult();
        Assert.Equal("MAP-27K_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_DRY_RUN_EMITTER", r.Format);
        Assert.Equal("map_00", r.MapId);
    }

    [Fact]
    public void Build_WithValidInputs_EmittedStatusDryRunOnly()
    {
        var r = BuildResult();
        Assert.All(r.EmittedOperationRecords, op =>
            Assert.Equal("DRY_RUN_EMITTED_SANDBOX_RECORD_ONLY", op.EmissionStatus));
    }

    [Fact]
    public void Build_WithValidInputs_RenderersNonEmpty()
    {
        WriteMap27jFiles();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterBuilder();
        var r = builder.Build(_map27jDir, _outputDir);
        Assert.NotEmpty(builder.RenderJson(r));
        Assert.NotEmpty(builder.RenderMarkdown(r));
        Assert.NotEmpty(builder.RenderCsv(r));
        Assert.NotEmpty(builder.RenderSummary(r));
        Assert.NotEmpty(builder.RenderOperationsJson(r));
        Assert.NotEmpty(builder.RenderOperationsCsv(r));
        Assert.NotEmpty(builder.RenderSourceManifestJson(r));
        Assert.NotEmpty(builder.RenderDryRunDigestJson(r));
        Assert.NotEmpty(builder.RenderForbiddenOutputGuardJson(r));
    }

    // -----------------------------------------------------------------------
    // Failure path tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_MissingSourceRoot_ReturnsInvalidVerdict()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterBuilder();
        var r = builder.Build(Path.Combine(_tempDir, "nonexistent.local", "map_00"), _outputDir);
        Assert.False(r.IsValid);
        Assert.Contains("INVALID", r.Verdict);
        Assert.NotEmpty(r.Errors);
    }

    [Fact]
    public void Build_MissingSourceJson_ReturnsInvalidVerdict()
    {
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterBuilder();
        var r = builder.Build(_map27jDir, _outputDir);
        Assert.False(r.IsValid);
        Assert.Contains("INVALID", r.Verdict);
        Assert.NotEmpty(r.Errors);
    }

    [Fact]
    public void Build_WithSourceIsInvalid_IsInvalid()
    {
        var r = BuildResult(planStatus: "BACKEND_PLAN_FAILED", isValid: false,
            failedCheckCount: 2, passedCheckCount: 49);
        Assert.False(r.IsValid);
        Assert.Contains("INVALID", r.Verdict);
    }

    [Fact]
    public void Build_WithBinFileInOutputRoot_ForbiddenScanFails()
    {
        WriteMap27jFiles();
        File.WriteAllText(Path.Combine(_outputDir, "evil.bin"), "data");
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterBuilder();
        var r = builder.Build(_map27jDir, _outputDir);
        var check = r.Checks.Single(c => c.CheckId == "POST_BACKEND_DRY_RUN_EMITTER_FORBIDDEN_SCAN_PASS");
        Assert.Equal("FAIL", check.CheckStatus);
        Assert.Contains("FAIL", r.ForbiddenArtifactScan);
        Assert.False(r.IsValid);
    }

    // -----------------------------------------------------------------------
    // FinalizeAfterOutputs tests
    // -----------------------------------------------------------------------

    [Fact]
    public void FinalizeAfterOutputs_WithSafeOutputs_KeepsForbiddenScanPass()
    {
        WriteMap27jFiles();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterBuilder();
        var r = builder.Build(_map27jDir, _outputDir);
        File.WriteAllText(Path.Combine(_outputDir, "map_00.plan.json"), "{\"ok\":true}");
        var finalized = builder.FinalizeAfterOutputs(r, _outputDir);
        Assert.Contains("PASS", finalized.ForbiddenArtifactScan, StringComparison.Ordinal);
        Assert.True(finalized.IsValid);
        Assert.Equal("BACKEND_DRY_RUN_EMITTER_COMPLETE", finalized.EmitterStatus);
    }

    [Fact]
    public void FinalizeAfterOutputs_WithBinFileAfterInitialBuild_MarksResultInvalid()
    {
        WriteMap27jFiles();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterBuilder();
        var r = builder.Build(_map27jDir, _outputDir);
        Assert.True(r.IsValid);
        File.WriteAllText(Path.Combine(_outputDir, "evil.bin"), "data");
        var finalized = builder.FinalizeAfterOutputs(r, _outputDir);
        Assert.Contains("FAIL", finalized.ForbiddenArtifactScan, StringComparison.Ordinal);
        Assert.False(finalized.IsValid);
        Assert.Equal("BACKEND_DRY_RUN_EMITTER_FAILED", finalized.EmitterStatus);
        Assert.Contains("INVALID", finalized.Verdict);
    }
}
