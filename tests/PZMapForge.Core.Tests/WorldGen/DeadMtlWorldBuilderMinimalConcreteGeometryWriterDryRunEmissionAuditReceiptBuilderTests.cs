using System.Security.Cryptography;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmissionAuditReceiptBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-dry-run-emission-audit", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmissionAuditReceiptBuilderTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { }
    }

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private static string Sha256OfFile(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLower();

    private static string WriteJson(string path, object obj)
    {
        var opts = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(path, JsonSerializer.Serialize(obj, opts));
        return path;
    }

    private string WriteValidEmitterFixture(
        string? overrideVerdict = null,
        bool isValid = true,
        string? overrideEmitterStatus = null,
        bool dryRunOnly = true,
        int emittedRecordCount = 8,
        string? tamperRecordId = null,
        string tamperSha = "0000000000000000000000000000000000000000000000000000000000000000")
    {
        var outputRoot = Path.Combine(_tempDir, ".local", "emitter-output");
        Directory.CreateDirectory(outputRoot);

        var opts = new JsonSerializerOptions { WriteIndented = true };

        // 8 emitted record files
        string componentPath = Path.Combine(outputRoot, "map_00.component_writer_record.json");
        WriteJson(componentPath, new
        {
            record_kind            = "COMPONENT_WRITER_RECORD",
            map_id                 = "map_00",
            target_component_id    = "map_00_component_0001",
            intent                 = "RESIDENTIAL_LOT_BLOCK",
            access_readiness_class = "DUAL_ACCESS_CANDIDATE",
            component_bbox         = new { min_x = 124, min_y = 10, max_x = 212, max_y = 69, width_px = 89, height_px = 60 },
            source_geometry_kind   = "COMPONENT_BBOX_GEOMETRY",
            dry_run_only = true, runtime_valid = false, writer_ready = false, materialized = false,
        });

        string lotPath = Path.Combine(outputRoot, "map_00.lot_writer_records.json");
        WriteJson(lotPath, new
        {
            record_kind = "LOT_WRITER_RECORDS", map_id = "map_00",
            target_component_id = "map_00_component_0001", lot_count = 7,
            lots = new object[7], dry_run_only = true,
        });

        string slotPath = Path.Combine(outputRoot, "map_00.building_slot_writer_records.json");
        WriteJson(slotPath, new
        {
            record_kind = "BUILDING_SLOT_WRITER_RECORDS", map_id = "map_00",
            target_component_id = "map_00_component_0001", building_slot_count = 7,
            building_slots = new object[7], dry_run_only = true,
        });

        string frontagePath = Path.Combine(outputRoot, "map_00.frontage_access_record.json");
        WriteJson(frontagePath, new
        {
            record_kind = "FRONTAGE_ACCESS_RECORD",
            frontage_side = "NORTH", frontage_component_id = "map_00_component_0023",
            frontage_contact_px = 89, dry_run_only = true,
        });

        string rearPath = Path.Combine(outputRoot, "map_00.rear_service_access_record.json");
        WriteJson(rearPath, new
        {
            record_kind = "REAR_SERVICE_ACCESS_RECORD",
            rear_service_side = "EAST", rear_service_component_id = "map_00_component_0030",
            rear_service_contact_px = 60, dry_run_only = true,
        });

        string scanPath = Path.Combine(outputRoot, "map_00.forbidden_output_scan.json");
        WriteJson(scanPath, new
        {
            record_kind = "FORBIDDEN_OUTPUT_SCAN_RECORD",
            lotpack_found = false, lotheader_found = false, worldgen_override_lua_found = false,
            pz_install_path_found = false, scan_passed = true,
            details = "Scan passed: no .lotpack, .lotheader, WorldGenOverride.lua, or PZ install paths found.",
        });

        string rollbackPath = Path.Combine(outputRoot, "map_00.rollback_record.json");
        WriteJson(rollbackPath, new
        {
            record_kind = "ROLLBACK_RECORD",
            dot_local_outputs_only = true, source_hashes_unchanged = true,
            runtime_files_created = false, pz_install_mutated = false, dry_run_only = true,
        });

        string claimBoundaryPath = Path.Combine(outputRoot, "map_00.claim_boundary_record.json");
        WriteJson(claimBoundaryPath, new
        {
            record_kind = "CLAIM_BOUNDARY_RECORD",
            writer_ready = false, runtime_valid = false, materialized = false,
            approved_for_writer_experiment = false,
            writer_experiment_gate_status = "LOCKED_PENDING_OPERATOR_APPROVAL",
            runtime_proof_claimed = false, writer_ready_claimed = false,
            public_playable_packaging_claimed = false,
        });

        string Sha(string id, string path) =>
            tamperRecordId == id ? tamperSha : Sha256OfFile(path);

        var emittedRecords = new object[]
        {
            new { record_order = 1, record_id = "COMPONENT_WRITER_RECORD",      record_kind = "COMPONENT_RECORD",      path = componentPath,    sha256 = Sha("COMPONENT_WRITER_RECORD",      componentPath),    size_bytes = new FileInfo(componentPath).Length,    status = "DRY_RUN_RECORD_EMITTED" },
            new { record_order = 2, record_id = "LOT_WRITER_RECORDS",           record_kind = "LOT_RECORDS",           path = lotPath,          sha256 = Sha("LOT_WRITER_RECORDS",           lotPath),          size_bytes = new FileInfo(lotPath).Length,          status = "DRY_RUN_RECORD_EMITTED" },
            new { record_order = 3, record_id = "BUILDING_SLOT_WRITER_RECORDS", record_kind = "BUILDING_SLOT_RECORDS", path = slotPath,         sha256 = Sha("BUILDING_SLOT_WRITER_RECORDS", slotPath),         size_bytes = new FileInfo(slotPath).Length,         status = "DRY_RUN_RECORD_EMITTED" },
            new { record_order = 4, record_id = "FRONTAGE_ACCESS_RECORD",       record_kind = "ACCESS_RECORD",         path = frontagePath,     sha256 = Sha("FRONTAGE_ACCESS_RECORD",       frontagePath),     size_bytes = new FileInfo(frontagePath).Length,     status = "DRY_RUN_RECORD_EMITTED" },
            new { record_order = 5, record_id = "REAR_SERVICE_ACCESS_RECORD",   record_kind = "ACCESS_RECORD",         path = rearPath,         sha256 = Sha("REAR_SERVICE_ACCESS_RECORD",   rearPath),         size_bytes = new FileInfo(rearPath).Length,         status = "DRY_RUN_RECORD_EMITTED" },
            new { record_order = 6, record_id = "FORBIDDEN_OUTPUT_SCAN_RECORD", record_kind = "SCAN_RECORD",           path = scanPath,         sha256 = Sha("FORBIDDEN_OUTPUT_SCAN_RECORD", scanPath),         size_bytes = new FileInfo(scanPath).Length,         status = "DRY_RUN_RECORD_EMITTED" },
            new { record_order = 7, record_id = "ROLLBACK_RECORD",              record_kind = "ROLLBACK_RECORD",       path = rollbackPath,     sha256 = Sha("ROLLBACK_RECORD",              rollbackPath),     size_bytes = new FileInfo(rollbackPath).Length,     status = "DRY_RUN_RECORD_EMITTED" },
            new { record_order = 8, record_id = "CLAIM_BOUNDARY_RECORD",        record_kind = "CLAIM_BOUNDARY_RECORD", path = claimBoundaryPath, sha256 = Sha("CLAIM_BOUNDARY_RECORD",       claimBoundaryPath), size_bytes = new FileInfo(claimBoundaryPath).Length, status = "DRY_RUN_RECORD_EMITTED" },
        };

        string emitterBase = "map_00.minimal_concrete_geometry_writer_dry_run_emitter";
        string emitterJsonPath = Path.Combine(outputRoot, $"{emitterBase}.json");

        WriteJson(emitterJsonPath, new
        {
            format = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-writer-dry-run-emitter.v1",
            generated_utc = "2026-06-17T00:00:00.0000000Z",
            map_id = "map_00",
            target_component_id = "map_00_component_0001",
            emitter_status = overrideEmitterStatus ?? "DRY_RUN_RECORDS_EMITTED_TO_DOT_LOCAL_ONLY",
            dry_run_only = dryRunOnly,
            writer_ready = false, runtime_valid = false, materialized = false,
            approved_for_writer_experiment = false,
            writer_experiment_gate_status = "LOCKED_PENDING_OPERATOR_APPROVAL",
            output_root = outputRoot,
            emitted_record_count = emittedRecordCount,
            emitted_records = emittedRecords,
            forbidden_output_scan = new
            {
                scan_passed = true,
                details = "Scan passed: no .lotpack, .lotheader, WorldGenOverride.lua, or PZ install paths found.",
            },
            rollback_record = new
            {
                record_kind = "ROLLBACK_RECORD",
                dot_local_outputs_only = true, source_hashes_unchanged = true,
                runtime_files_created = false, pz_install_mutated = false, dry_run_only = true,
            },
            claim_boundary = new
            {
                record_kind = "CLAIM_BOUNDARY_RECORD",
                writer_ready = false, runtime_valid = false, materialized = false,
                approved_for_writer_experiment = false,
                writer_experiment_gate_status = "LOCKED_PENDING_OPERATOR_APPROVAL",
                runtime_proof_claimed = false, writer_ready_claimed = false,
                public_playable_packaging_claimed = false,
            },
            check_count = 25, passed_check_count = 25, failed_check_count = 0,
            checks = Array.Empty<object>(),
            is_valid = isValid,
            verdict = overrideVerdict ?? "MAP26G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER_COMPLETE",
            errors = Array.Empty<string>(),
        });

        // Write 3 companion main output files
        File.WriteAllText(Path.Combine(outputRoot, $"{emitterBase}.md"),          "# MAP-26G");
        File.WriteAllText(Path.Combine(outputRoot, $"{emitterBase}.csv"),         "check_order,check_id,check_status");
        File.WriteAllText(Path.Combine(outputRoot, $"{emitterBase}.summary.txt"), "verdict : MAP26G_COMPLETE");

        return emitterJsonPath;
    }

    private DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmissionAuditReceiptBuilder NewBuilder() =>
        new();

    // -----------------------------------------------------------------------
    // Guard tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_MissingEmitterResult_IsInvalid()
    {
        var result = NewBuilder().Build(
            Path.Combine(_tempDir, ".local", "missing.json"),
            Path.Combine(_tempDir, ".local", "emitter-output"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public void Build_MissingOutputRoot_IsInvalid()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(
            emitterPath,
            Path.Combine(_tempDir, ".local", "nonexistent-dir"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public void Build_MalformedEmitterResult_IsInvalid()
    {
        var outputRoot = Path.Combine(_tempDir, ".local", "malformed");
        Directory.CreateDirectory(outputRoot);
        var emitterPath = Path.Combine(outputRoot, "emitter.json");
        File.WriteAllText(emitterPath, "{ not valid json ::::");
        var result = NewBuilder().Build(emitterPath, outputRoot);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("parse"));
    }

    [Fact]
    public void Build_MissingOneEmittedRecord_IsInvalid()
    {
        var emitterPath = WriteValidEmitterFixture();
        var outputRoot  = Path.GetDirectoryName(emitterPath)!;
        File.Delete(Path.Combine(outputRoot, "map_00.component_writer_record.json"));
        var result = NewBuilder().Build(emitterPath, outputRoot);
        Assert.False(result.IsValid);
    }

    // -----------------------------------------------------------------------
    // Check-level failure tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_WrongMap26GVerdict_IsInvalid()
    {
        var emitterPath = WriteValidEmitterFixture(overrideVerdict: "MAP26G_SOMETHING_WRONG");
        var outputRoot  = Path.GetDirectoryName(emitterPath)!;
        var result = NewBuilder().Build(emitterPath, outputRoot);
        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, c => c.CheckId == "MAP26G_VERDICT_COMPLETE" && c.CheckStatus == "FAIL");
    }

    [Fact]
    public void Build_Map26GIsValidFalse_IsInvalid()
    {
        var emitterPath = WriteValidEmitterFixture(isValid: false);
        var outputRoot  = Path.GetDirectoryName(emitterPath)!;
        var result = NewBuilder().Build(emitterPath, outputRoot);
        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, c => c.CheckId == "MAP26G_IS_VALID_TRUE" && c.CheckStatus == "FAIL");
    }

    [Fact]
    public void Build_WrongEmitterStatus_IsInvalid()
    {
        var emitterPath = WriteValidEmitterFixture(overrideEmitterStatus: "WRONG_STATUS");
        var outputRoot  = Path.GetDirectoryName(emitterPath)!;
        var result = NewBuilder().Build(emitterPath, outputRoot);
        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, c => c.CheckId == "MAP26G_EMITTER_STATUS_DOT_LOCAL_ONLY" && c.CheckStatus == "FAIL");
    }

    [Fact]
    public void Build_DryRunOnlyFalse_IsInvalid()
    {
        var emitterPath = WriteValidEmitterFixture(dryRunOnly: false);
        var outputRoot  = Path.GetDirectoryName(emitterPath)!;
        var result = NewBuilder().Build(emitterPath, outputRoot);
        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, c => c.CheckId == "MAP26G_DRY_RUN_ONLY_TRUE" && c.CheckStatus == "FAIL");
    }

    [Fact]
    public void Build_HashMismatchForEmittedRecord_IsInvalid()
    {
        var emitterPath = WriteValidEmitterFixture(tamperRecordId: "COMPONENT_WRITER_RECORD");
        var outputRoot  = Path.GetDirectoryName(emitterPath)!;
        var result = NewBuilder().Build(emitterPath, outputRoot);
        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, c => c.CheckId == "NO_HASH_MISMATCHES" && c.CheckStatus == "FAIL");
    }

    // -----------------------------------------------------------------------
    // Valid build structural tests
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidFixture_IsValid()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidFixture_NoErrors()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidFixture_AllChecksPass()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        Assert.All(result.Checks, c => Assert.Equal("PASS", c.CheckStatus));
    }

    [Fact]
    public void ValidFixture_CheckCount_Is29()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        Assert.Equal(29, result.AuditCheckCount);
    }

    [Fact]
    public void ValidFixture_PassedCheckCount_Is29()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        Assert.Equal(29, result.PassedAuditCheckCount);
    }

    [Fact]
    public void ValidFixture_FailedCheckCount_Is0()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        Assert.Equal(0, result.FailedAuditCheckCount);
    }

    [Fact]
    public void ValidFixture_AuditedFileCount_Is12()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        Assert.Equal(12, result.AuditedFileCount);
    }

    // -----------------------------------------------------------------------
    // Hash / count tests
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidFixture_HashMatchCount_Is8()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        Assert.Equal(8, result.HashMatchCount);
    }

    [Fact]
    public void ValidFixture_HashMismatchCount_Is0()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        Assert.Equal(0, result.HashMismatchCount);
    }

    [Fact]
    public void ValidFixture_SourceEmitterResultSha256_Is64HexChars()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        Assert.Equal(64, result.SourceEmitterResultSha256.Length);
        Assert.Matches("^[0-9a-f]{64}$", result.SourceEmitterResultSha256);
    }

    // -----------------------------------------------------------------------
    // Geometry record audit tests
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidFixture_ComponentRecord_Valid()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        Assert.Equal("map_00_component_0001", result.GeometryRecordAudit.TargetComponentId);
        Assert.Equal("RESIDENTIAL_LOT_BLOCK", result.GeometryRecordAudit.Intent);
        Assert.Equal("DUAL_ACCESS_CANDIDATE", result.GeometryRecordAudit.AccessReadinessClass);
        Assert.Equal(124, result.GeometryRecordAudit.BboxMinX);
        Assert.Equal(10,  result.GeometryRecordAudit.BboxMinY);
        Assert.Equal(212, result.GeometryRecordAudit.BboxMaxX);
        Assert.Equal(69,  result.GeometryRecordAudit.BboxMaxY);
        Assert.Equal(89,  result.GeometryRecordAudit.BboxWidthPx);
        Assert.Equal(60,  result.GeometryRecordAudit.BboxHeightPx);
    }

    [Fact]
    public void ValidFixture_LotRecordsCount_Is7()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        Assert.Equal(7, result.GeometryRecordAudit.LotCount);
    }

    [Fact]
    public void ValidFixture_BuildingSlotRecordsCount_Is7()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        Assert.Equal(7, result.GeometryRecordAudit.BuildingSlotCount);
    }

    [Fact]
    public void ValidFixture_FrontageStable()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        Assert.Equal("NORTH",                  result.GeometryRecordAudit.FrontageSide);
        Assert.Equal("map_00_component_0023",  result.GeometryRecordAudit.FrontageComponentId);
        Assert.Equal(89,                       result.GeometryRecordAudit.FrontageContactPx);
    }

    [Fact]
    public void ValidFixture_RearServiceStable()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        Assert.Equal("EAST",                   result.GeometryRecordAudit.RearServiceSide);
        Assert.Equal("map_00_component_0030",  result.GeometryRecordAudit.RearServiceComponentId);
        Assert.Equal(60,                       result.GeometryRecordAudit.RearServiceContactPx);
    }

    // -----------------------------------------------------------------------
    // Scan / boundary tests
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidFixture_ForbiddenScan_Pass()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        Assert.True(result.ForbiddenArtifactScan.ScanPassed);
        Assert.False(result.ForbiddenArtifactScan.LotpackFound);
        Assert.False(result.ForbiddenArtifactScan.LotheaderFound);
        Assert.False(result.ForbiddenArtifactScan.LuaFileFound);
        Assert.False(result.ForbiddenArtifactScan.WorldgenOverrideLuaFound);
    }

    [Fact]
    public void ValidFixture_ClaimBoundary_Pass()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        Assert.False(result.ClaimBoundaryAudit.WriterReady);
        Assert.False(result.ClaimBoundaryAudit.RuntimeValid);
        Assert.False(result.ClaimBoundaryAudit.Materialized);
        Assert.False(result.ClaimBoundaryAudit.ApprovedForWriterExperiment);
        Assert.False(result.ClaimBoundaryAudit.RuntimeProofClaimed);
        Assert.True(result.ClaimBoundaryAudit.ClaimBoundaryAuditPassed);
    }

    // -----------------------------------------------------------------------
    // Verdict test
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidFixture_Verdict_IsComplete()
    {
        var emitterPath = WriteValidEmitterFixture();
        var result = NewBuilder().Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        Assert.Equal(
            "MAP26H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMISSION_AUDIT_RECEIPT_COMPLETE",
            result.Verdict);
    }

    // -----------------------------------------------------------------------
    // Render tests
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCsv_Has29DataRows()
    {
        var emitterPath = WriteValidEmitterFixture();
        var builder = NewBuilder();
        var result  = builder.Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        var csv     = builder.RenderCsv(result);
        var lines   = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(30, lines.Length); // 1 header + 29 data rows
    }

    [Fact]
    public void RenderMarkdown_ContainsClaimBoundary()
    {
        var emitterPath = WriteValidEmitterFixture();
        var builder = NewBuilder();
        var result  = builder.Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        var md      = builder.RenderMarkdown(result);
        Assert.Contains("Claim Boundary", md);
        Assert.Contains("writer_ready", md);
        Assert.Contains("runtime_valid", md);
    }

    [Fact]
    public void RenderSummary_ContainsVerdict()
    {
        var emitterPath = WriteValidEmitterFixture();
        var builder = NewBuilder();
        var result  = builder.Build(emitterPath, Path.GetDirectoryName(emitterPath)!);
        var summary = builder.RenderSummary(result);
        Assert.Contains("verdict", summary);
        Assert.Contains("MAP26H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMISSION_AUDIT_RECEIPT_COMPLETE", summary);
    }
}
