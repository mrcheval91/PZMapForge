using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilderTests : IDisposable
{
    private readonly string _tempDir;

    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "pzmapforge-map26i-test.local", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private (string auditReceiptPath, string componentPath, string lotPath, string slotPath,
             string frontagePath, string rearPath, string claimPath) WriteValidFixture(
        string overrideVerdict = "",
        bool auditIsValid = true,
        int auditedFileCount = 12,
        int hashMismatchCount = 0,
        int passedAuditCheckCount = 29,
        string frontSide = "NORTH",
        string rearSide = "EAST",
        bool writerReady = false,
        bool runtimeValid = false,
        bool materialized = false,
        bool approved = false,
        string gateStatus = "LOCKED_PENDING_OPERATOR_APPROVAL",
        int lotCount = 7,
        int slotCount = 7)
    {
        string verdict = string.IsNullOrEmpty(overrideVerdict)
            ? "MAP26H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMISSION_AUDIT_RECEIPT_COMPLETE"
            : overrideVerdict;

        // audit receipt
        var auditReceiptObj = new
        {
            verdict,
            is_valid = auditIsValid,
            audited_file_count = auditedFileCount,
            hash_mismatch_count = hashMismatchCount,
            passed_audit_check_count = passedAuditCheckCount
        };
        string auditReceiptPath = Path.Combine(_tempDir, "map_00.audit_receipt.json");
        File.WriteAllText(auditReceiptPath, JsonSerializer.Serialize(auditReceiptObj));

        // component record
        var componentObj = new
        {
            record_kind = "COMPONENT_WRITER_RECORD",
            map_id = "map_00",
            target_component_id = "map_00_component_0001",
            intent = "RESIDENTIAL_LOT_BLOCK",
            access_readiness_class = "DUAL_ACCESS_CANDIDATE",
            component_bbox = new { min_x = 124, min_y = 10, max_x = 212, max_y = 69, width_px = 89, height_px = 60 },
            dry_run_only = true
        };
        string componentPath = Path.Combine(_tempDir, "map_00.component_writer_record.json");
        File.WriteAllText(componentPath, JsonSerializer.Serialize(componentObj));

        // lot records
        var lots = Enumerable.Range(1, lotCount).Select(i => new
        {
            lot_order = i,
            lot_id = $"map_00_comp0001_lot_{i:D4}",
            component_id = "map_00_component_0001",
            min_x = 124 + (i - 1) * 13,
            min_y = 10,
            max_x = 124 + (i - 1) * 13 + 12,
            max_y = 69,
            width_px = 13,
            height_px = 60,
            frontage_side = "NORTH",
            rear_service_side = "EAST",
            geometry_type = "LOT_RECTANGLE_MVP",
            geometry_status = "CONCRETE_PIXEL_GEOMETRY_CREATED"
        }).ToArray();
        string lotPath = Path.Combine(_tempDir, "map_00.lot_writer_records.json");
        File.WriteAllText(lotPath, JsonSerializer.Serialize(new { lots, dry_run_only = true }));

        // building slot records
        var buildingSlots = Enumerable.Range(1, slotCount).Select(i => new
        {
            slot_order = i,
            slot_id = $"map_00_comp0001_slot_{i:D4}",
            lot_id = $"map_00_comp0001_lot_{i:D4}",
            lot_order = i,
            component_id = "map_00_component_0001",
            min_x = 126 + (i - 1) * 13,
            min_y = 13,
            max_x = 134 + (i - 1) * 13,
            max_y = 66,
            width_px = 9,
            height_px = 54,
            frontage_setback_px = 3,
            rear_setback_px = 3,
            side_inset_px = 2,
            slot_status = "ACCEPTED",
            geometry_type = "BUILDING_SLOT_RECTANGLE_MVP",
            geometry_status = "CONCRETE_PIXEL_GEOMETRY_CREATED"
        }).ToArray();
        string slotPath = Path.Combine(_tempDir, "map_00.building_slot_writer_records.json");
        File.WriteAllText(slotPath, JsonSerializer.Serialize(new { building_slots = buildingSlots, dry_run_only = true }));

        // frontage access record
        var frontageObj = new
        {
            record_kind = "FRONTAGE_ACCESS_RECORD",
            frontage_side = frontSide,
            frontage_component_id = "map_00_component_0023",
            frontage_contact_px = 89,
            dry_run_only = true
        };
        string frontagePath = Path.Combine(_tempDir, "map_00.frontage_access_record.json");
        File.WriteAllText(frontagePath, JsonSerializer.Serialize(frontageObj));

        // rear service access record
        var rearObj = new
        {
            record_kind = "REAR_SERVICE_ACCESS_RECORD",
            rear_service_side = rearSide,
            rear_service_component_id = "map_00_component_0030",
            rear_service_contact_px = 60,
            dry_run_only = true
        };
        string rearPath = Path.Combine(_tempDir, "map_00.rear_service_access_record.json");
        File.WriteAllText(rearPath, JsonSerializer.Serialize(rearObj));

        // claim boundary record
        var claimObj = new
        {
            record_kind = "CLAIM_BOUNDARY_RECORD",
            writer_ready = writerReady,
            runtime_valid = runtimeValid,
            materialized,
            approved_for_writer_experiment = approved,
            writer_experiment_gate_status = gateStatus
        };
        string claimPath = Path.Combine(_tempDir, "map_00.claim_boundary_record.json");
        File.WriteAllText(claimPath, JsonSerializer.Serialize(claimObj));

        return (auditReceiptPath, componentPath, lotPath, slotPath, frontagePath, rearPath, claimPath);
    }

    [Fact]
    public void ValidFixture_Verdict_IsComplete()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder();
        var result = builder.Build(ar, comp, lot, slot, front, rear, claim);
        Assert.Equal("MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_COMPLETE", result.Verdict);
    }

    [Fact]
    public void ValidFixture_IsValid_True()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidFixture_CheckCount_Is28()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.Equal(28, result.AdapterCheckCount);
    }

    [Fact]
    public void ValidFixture_PassedCheckCount_Is28()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.Equal(28, result.PassedAdapterCheckCount);
    }

    [Fact]
    public void ValidFixture_FailedCheckCount_Is0()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.Equal(0, result.FailedAdapterCheckCount);
    }

    [Fact]
    public void ValidFixture_NormalizedLotCount_Is7()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.Equal(7, result.NormalizedLots.Count);
    }

    [Fact]
    public void ValidFixture_NormalizedSlotCount_Is7()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.Equal(7, result.NormalizedBuildingSlots.Count);
    }

    [Fact]
    public void ValidFixture_AccessRecordCount_Is2()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.Equal(2, result.NormalizedAccessRecords.Count);
    }

    [Fact]
    public void ValidFixture_ForbiddenFamilyCount_Is8()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.Equal(8, result.ForbiddenOutputFamilies.Count);
    }

    [Fact]
    public void ValidFixture_WriterReady_IsFalse()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.False(result.WriterReady);
    }

    [Fact]
    public void ValidFixture_RuntimeValid_IsFalse()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.False(result.RuntimeValid);
    }

    [Fact]
    public void ValidFixture_Materialized_IsFalse()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.False(result.Materialized);
    }

    [Fact]
    public void ValidFixture_ApprovedForWriterExperiment_IsFalse()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.False(result.ApprovedForWriterExperiment);
    }

    [Fact]
    public void ValidFixture_AllNormalizedLots_WriterConsumableFalse()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.All(result.NormalizedLots, l => Assert.False(l.WriterConsumable));
    }

    [Fact]
    public void ValidFixture_AllNormalizedLots_RuntimeConsumableFalse()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.All(result.NormalizedLots, l => Assert.False(l.RuntimeConsumable));
    }

    [Fact]
    public void ValidFixture_AllNormalizedSlots_WriterConsumableFalse()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.All(result.NormalizedBuildingSlots, s => Assert.False(s.WriterConsumable));
    }

    [Fact]
    public void ValidFixture_AllNormalizedSlots_RuntimeConsumableFalse()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.All(result.NormalizedBuildingSlots, s => Assert.False(s.RuntimeConsumable));
    }

    [Fact]
    public void ValidFixture_NormalizedComponent_TargetId_Stable()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.Equal("map_00_component_0001", result.NormalizedComponent.TargetComponentId);
    }

    [Fact]
    public void ValidFixture_AdapterContractStatus_IsNormalizedDryRunRecordsOnly()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.Equal("NORMALIZED_DRY_RUN_RECORDS_ONLY", result.AdapterContractStatus);
    }

    [Fact]
    public void MissingAuditReceipt_ReturnsInvalidVerdict()
    {
        var (_, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(Path.Combine(_tempDir, "missing.json"), comp, lot, slot, front, rear, claim);
        Assert.False(result.IsValid);
        Assert.Equal("MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_INVALID", result.Verdict);
    }

    [Fact]
    public void MissingComponentRecord_ReturnsInvalidVerdict()
    {
        var (ar, _, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, Path.Combine(_tempDir, "missing.json"), lot, slot, front, rear, claim);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void WrongAuditVerdict_FailsCheck3()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture(overrideVerdict: "WRONG_VERDICT");
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.False(result.IsValid);
        var check = result.Checks.Single(c => c.CheckId == "MAP26H_VERDICT_COMPLETE");
        Assert.Equal("FAIL", check.CheckStatus);
    }

    [Fact]
    public void AuditIsValidFalse_FailsCheck4()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture(auditIsValid: false);
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        var check = result.Checks.Single(c => c.CheckId == "MAP26H_IS_VALID_TRUE");
        Assert.Equal("FAIL", check.CheckStatus);
    }

    [Fact]
    public void AuditedFileCount11_FailsCheck5()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture(auditedFileCount: 11);
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        var check = result.Checks.Single(c => c.CheckId == "MAP26H_AUDITED_FILE_COUNT_12");
        Assert.Equal("FAIL", check.CheckStatus);
    }

    [Fact]
    public void HashMismatch1_FailsCheck6()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture(hashMismatchCount: 1);
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        var check = result.Checks.Single(c => c.CheckId == "MAP26H_HASH_MISMATCH_COUNT_0");
        Assert.Equal("FAIL", check.CheckStatus);
    }

    [Fact]
    public void PassedChecks28_FailsCheck7()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture(passedAuditCheckCount: 28);
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        var check = result.Checks.Single(c => c.CheckId == "MAP26H_PASSED_AUDIT_CHECKS_29");
        Assert.Equal("FAIL", check.CheckStatus);
    }

    [Fact]
    public void WrongFrontageSide_FailsCheck18()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture(frontSide: "SOUTH");
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        var check = result.Checks.Single(c => c.CheckId == "FRONTAGE_ACCESS_STABLE");
        Assert.Equal("FAIL", check.CheckStatus);
    }

    [Fact]
    public void WrongRearSide_FailsCheck19()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture(rearSide: "WEST");
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        var check = result.Checks.Single(c => c.CheckId == "REAR_SERVICE_ACCESS_STABLE");
        Assert.Equal("FAIL", check.CheckStatus);
    }

    [Fact]
    public void WriterReadyTrue_FailsCheck21()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture(writerReady: true);
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        var check = result.Checks.Single(c => c.CheckId == "WRITER_READY_FALSE");
        Assert.Equal("FAIL", check.CheckStatus);
    }

    [Fact]
    public void RuntimeValidTrue_FailsCheck22()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture(runtimeValid: true);
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        var check = result.Checks.Single(c => c.CheckId == "RUNTIME_VALID_FALSE");
        Assert.Equal("FAIL", check.CheckStatus);
    }

    [Fact]
    public void MaterializedTrue_FailsCheck23()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture(materialized: true);
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        var check = result.Checks.Single(c => c.CheckId == "MATERIALIZED_FALSE");
        Assert.Equal("FAIL", check.CheckStatus);
    }

    [Fact]
    public void GateStatusNotLocked_FailsCheck25()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture(gateStatus: "UNLOCKED");
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        var check = result.Checks.Single(c => c.CheckId == "GATE_STATUS_LOCKED");
        Assert.Equal("FAIL", check.CheckStatus);
    }

    [Fact]
    public void RenderJson_ContainsVerdict()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder();
        var result = builder.Build(ar, comp, lot, slot, front, rear, claim);
        var json = builder.RenderJson(result);
        Assert.Contains("MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_COMPLETE", json);
    }

    [Fact]
    public void RenderMarkdown_ContainsNormalizedLotsSection()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder();
        var result = builder.Build(ar, comp, lot, slot, front, rear, claim);
        var md = builder.RenderMarkdown(result);
        Assert.Contains("Normalized Lots", md);
    }

    [Fact]
    public void RenderCsv_Has28DataRows()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder();
        var result = builder.Build(ar, comp, lot, slot, front, rear, claim);
        var csv = builder.RenderCsv(result);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(29, lines.Length); // 1 header + 28 data rows
    }

    [Fact]
    public void RenderSummary_Contains1ForIsValid()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var builder = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder();
        var result = builder.Build(ar, comp, lot, slot, front, rear, claim);
        var summary = builder.RenderSummary(result);
        Assert.Contains("Is Valid         : 1", summary);
    }

    // MAP-26J canonical vocabulary tests

    [Fact]
    public void ValidFixture_NormalizedComponent_SourceRecordId_IsCanonical()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.Equal("COMPONENT_WRITER_RECORD", result.NormalizedComponent.SourceRecordId);
    }

    [Fact]
    public void ValidFixture_FrontageAccessKind_IsCanonical()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.Equal("FRONTAGE_ACCESS", result.NormalizedAccessRecords[0].AccessKind);
    }

    [Fact]
    public void ValidFixture_RearServiceAccessKind_IsCanonical()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.Equal("REAR_SERVICE_ACCESS", result.NormalizedAccessRecords[1].AccessKind);
    }

    [Fact]
    public void ValidFixture_ForbiddenFamilyIds_AreCanonical()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        string[] expected = {
            "LOT_PACK_RUNTIME_BINARY", "LOT_HEADER_RUNTIME_BINARY", "WORLDGEN_OVERRIDE_LUA",
            "RUNTIME_LUA", "PROJECT_ZOMBOID_INSTALL_PATH", "STEAM_WORKSHOP_OUTPUT",
            "COMPILE_WORLDGEN_INVOCATION", "MAP_00_PNG_MUTATION"
        };
        Assert.Equal(expected, result.ForbiddenOutputFamilies.Select(f => f.FamilyId).ToArray());
    }

    [Fact]
    public void ValidFixture_ForbiddenFamilyStatuses_AreForbidden()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.All(result.ForbiddenOutputFamilies, f => Assert.Equal("FORBIDDEN", f.Status));
    }

    [Fact]
    public void ValidFixture_ForbiddenFamilies_DoNotUseBlockedStatus()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        Assert.DoesNotContain(result.ForbiddenOutputFamilies, f => f.Status == "BLOCKED");
    }

    [Fact]
    public void ValidFixture_ForbiddenFamilies_DoNotUseOldIds()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteValidFixture();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder()
            .Build(ar, comp, lot, slot, front, rear, claim);
        string[] oldIds = {
            "DOTLOTPACK_FILES", "DOTLOTHEADER_FILES", "WORLDGENOVERRIDE_LUA",
            "RUNTIME_LUA_SCRIPTS", "COMPILE_WORLDGEN", "PZ_INSTALLATION_PATHS",
            "MEDIA_MAPS_DIRECTORY", "LOT_BIN_EXPORT"
        };
        var actualIds = result.ForbiddenOutputFamilies.Select(f => f.FamilyId).ToHashSet();
        Assert.True(oldIds.All(id => !actualIds.Contains(id)),
            "Forbidden families must not contain any old (non-canonical) family IDs.");
    }
}
