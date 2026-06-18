using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractBuilder
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true
    };

    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractResult Build(
        string auditReceiptPath,
        string componentRecordPath,
        string lotRecordsPath,
        string buildingSlotRecordsPath,
        string frontageAccessPath,
        string rearServiceAccessPath,
        string claimBoundaryPath)
    {
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractResult
        {
            Format      = "MAP-26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT",
            GeneratedUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MapId        = "map_00",
            TargetComponentId = "map_00_component_0001",
            AdapterContractStatus = "NORMALIZED_DRY_RUN_RECORDS_ONLY",
            WriterReady               = false,
            RuntimeValid              = false,
            Materialized              = false,
            ApprovedForWriterExperiment = false,
            WriterExperimentGateStatus  = "LOCKED_PENDING_OPERATOR_APPROVAL"
        };

        var checks = new List<DeadMtlAdapterContractCheck>();

        // --- guard: audit receipt ---
        if (!File.Exists(auditReceiptPath))
        {
            result.Errors.Add($"MAP-26H audit receipt not found: {auditReceiptPath}");
            result.IsValid = false;
            result.Verdict = "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_INVALID";
            return result;
        }

        // --- guard: component record ---
        if (!File.Exists(componentRecordPath))
        {
            result.Errors.Add($"Component record not found: {componentRecordPath}");
            result.IsValid = false;
            result.Verdict = "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_INVALID";
            return result;
        }

        // --- guard: lot records ---
        if (!File.Exists(lotRecordsPath))
        {
            result.Errors.Add($"Lot records not found: {lotRecordsPath}");
            result.IsValid = false;
            result.Verdict = "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_INVALID";
            return result;
        }

        // --- guard: building slot records ---
        if (!File.Exists(buildingSlotRecordsPath))
        {
            result.Errors.Add($"Building slot records not found: {buildingSlotRecordsPath}");
            result.IsValid = false;
            result.Verdict = "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_INVALID";
            return result;
        }

        // --- guard: frontage access ---
        if (!File.Exists(frontageAccessPath))
        {
            result.Errors.Add($"Frontage access record not found: {frontageAccessPath}");
            result.IsValid = false;
            result.Verdict = "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_INVALID";
            return result;
        }

        // --- guard: rear service access ---
        if (!File.Exists(rearServiceAccessPath))
        {
            result.Errors.Add($"Rear service access record not found: {rearServiceAccessPath}");
            result.IsValid = false;
            result.Verdict = "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_INVALID";
            return result;
        }

        // --- guard: claim boundary ---
        if (!File.Exists(claimBoundaryPath))
        {
            result.Errors.Add($"Claim boundary record not found: {claimBoundaryPath}");
            result.IsValid = false;
            result.Verdict = "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_INVALID";
            return result;
        }

        // --- parse audit receipt ---
        string auditReceiptSha256;
        string auditVerdict = string.Empty;
        bool auditIsValid = false;
        int auditedFileCount = 0;
        int hashMismatchCount = 0;
        int passedAuditCheckCount = 0;
        try
        {
            var auditBytes = File.ReadAllBytes(auditReceiptPath);
            auditReceiptSha256 = Convert.ToHexString(SHA256.HashData(auditBytes)).ToLower();

            using var auditDoc = JsonDocument.Parse(auditBytes);
            var root = auditDoc.RootElement;
            if (root.TryGetProperty("verdict", out var vProp)) auditVerdict = vProp.GetString() ?? string.Empty;
            if (root.TryGetProperty("is_valid", out var ivProp)) auditIsValid = ivProp.GetBoolean();
            if (root.TryGetProperty("audited_file_count", out var afProp)) auditedFileCount = afProp.GetInt32();
            if (root.TryGetProperty("hash_mismatch_count", out var hmProp)) hashMismatchCount = hmProp.GetInt32();
            if (root.TryGetProperty("passed_audit_check_count", out var pcProp)) passedAuditCheckCount = pcProp.GetInt32();
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse audit receipt: {ex.Message}");
            result.IsValid = false;
            result.Verdict = "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_INVALID";
            return result;
        }

        result.SourceAuditReceiptPath    = auditReceiptPath;
        result.SourceAuditReceiptSha256  = auditReceiptSha256;
        result.SourceAuditReceiptVerdict = auditVerdict;
        result.SourceAuditReceiptIsValid = auditIsValid;

        // --- parse component record ---
        string compTargetComponentId = string.Empty;
        string compIntent = string.Empty;
        string compAccessReadinessClass = string.Empty;
        string compSourceRecordId = string.Empty;
        int compMinX = 0, compMinY = 0, compMaxX = 0, compMaxY = 0, compWidthPx = 0, compHeightPx = 0;
        try
        {
            using var compDoc = JsonDocument.Parse(File.ReadAllBytes(componentRecordPath));
            var r = compDoc.RootElement;
            if (r.TryGetProperty("target_component_id", out var p)) compTargetComponentId = p.GetString() ?? string.Empty;
            if (r.TryGetProperty("intent", out p)) compIntent = p.GetString() ?? string.Empty;
            if (r.TryGetProperty("access_readiness_class", out p)) compAccessReadinessClass = p.GetString() ?? string.Empty;
            if (r.TryGetProperty("component_bbox", out var bbox))
            {
                if (bbox.TryGetProperty("min_x", out p)) compMinX = p.GetInt32();
                if (bbox.TryGetProperty("min_y", out p)) compMinY = p.GetInt32();
                if (bbox.TryGetProperty("max_x", out p)) compMaxX = p.GetInt32();
                if (bbox.TryGetProperty("max_y", out p)) compMaxY = p.GetInt32();
                if (bbox.TryGetProperty("width_px", out p)) compWidthPx = p.GetInt32();
                if (bbox.TryGetProperty("height_px", out p)) compHeightPx = p.GetInt32();
            }
            compSourceRecordId = "COMPONENT_WRITER_RECORD";
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse component record: {ex.Message}");
            result.IsValid = false;
            result.Verdict = "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_INVALID";
            return result;
        }

        result.NormalizedComponent = new DeadMtlAdapterContractNormalizedComponent
        {
            RecordKind            = "NORMALIZED_COMPONENT",
            MapId                 = "map_00",
            TargetComponentId     = compTargetComponentId,
            Intent                = compIntent,
            AccessReadinessClass  = compAccessReadinessClass,
            BboxMinX              = compMinX,
            BboxMinY              = compMinY,
            BboxMaxX              = compMaxX,
            BboxMaxY              = compMaxY,
            BboxWidthPx           = compWidthPx,
            BboxHeightPx          = compHeightPx,
            SourceRecordId        = compSourceRecordId,
            WriterConsumable      = false,
            RuntimeConsumable     = false
        };

        // --- parse lot records ---
        var normalizedLots = new List<DeadMtlAdapterContractNormalizedLot>();
        try
        {
            using var lotDoc = JsonDocument.Parse(File.ReadAllBytes(lotRecordsPath));
            var r = lotDoc.RootElement;
            if (r.TryGetProperty("lots", out var lotsArr))
            {
                foreach (var lot in lotsArr.EnumerateArray())
                {
                    int lotOrder = lot.TryGetProperty("lot_order", out var lop) ? lop.GetInt32() : 0;
                    string lotId = lot.TryGetProperty("lot_id", out var lip) ? lip.GetString() ?? "" : "";
                    string compId = lot.TryGetProperty("component_id", out var cip) ? cip.GetString() ?? "" : "";
                    int minX = lot.TryGetProperty("min_x", out var p) ? p.GetInt32() : 0;
                    int minY = lot.TryGetProperty("min_y", out p) ? p.GetInt32() : 0;
                    int maxX = lot.TryGetProperty("max_x", out p) ? p.GetInt32() : 0;
                    int maxY = lot.TryGetProperty("max_y", out p) ? p.GetInt32() : 0;
                    int widthPx = lot.TryGetProperty("width_px", out p) ? p.GetInt32() : 0;
                    int heightPx = lot.TryGetProperty("height_px", out p) ? p.GetInt32() : 0;
                    string frontageSide = lot.TryGetProperty("frontage_side", out p) ? p.GetString() ?? "" : "";
                    string lotRearSide = lot.TryGetProperty("rear_service_side", out p) ? p.GetString() ?? "" : "";
                    string geoType = lot.TryGetProperty("geometry_type", out p) ? p.GetString() ?? "" : "";
                    string geoStatus = lot.TryGetProperty("geometry_status", out p) ? p.GetString() ?? "" : "";
                    normalizedLots.Add(new DeadMtlAdapterContractNormalizedLot
                    {
                        LotOrder           = lotOrder,
                        LotId              = lotId,
                        ComponentId        = compId,
                        MinX               = minX,
                        MinY               = minY,
                        MaxX               = maxX,
                        MaxY               = maxY,
                        WidthPx            = widthPx,
                        HeightPx           = heightPx,
                        FrontageSide       = frontageSide,
                        RearServiceSide    = lotRearSide,
                        SourceGeometryType   = geoType,
                        SourceGeometryStatus = geoStatus,
                        WriterConsumable   = false,
                        RuntimeConsumable  = false
                    });
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse lot records: {ex.Message}");
            result.IsValid = false;
            result.Verdict = "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_INVALID";
            return result;
        }
        result.NormalizedLots = normalizedLots;

        // --- parse building slot records ---
        var normalizedSlots = new List<DeadMtlAdapterContractNormalizedBuildingSlot>();
        try
        {
            using var slotDoc = JsonDocument.Parse(File.ReadAllBytes(buildingSlotRecordsPath));
            var r = slotDoc.RootElement;
            if (r.TryGetProperty("building_slots", out var slotsArr))
            {
                foreach (var slot in slotsArr.EnumerateArray())
                {
                    int slotOrder = slot.TryGetProperty("slot_order", out var sop) ? sop.GetInt32() : 0;
                    string slotId = slot.TryGetProperty("slot_id", out var sip) ? sip.GetString() ?? "" : "";
                    string lotId = slot.TryGetProperty("lot_id", out var lip) ? lip.GetString() ?? "" : "";
                    int lotOrder = slot.TryGetProperty("lot_order", out var lop) ? lop.GetInt32() : 0;
                    string compId = slot.TryGetProperty("component_id", out var cip) ? cip.GetString() ?? "" : "";
                    int minX = slot.TryGetProperty("min_x", out var p) ? p.GetInt32() : 0;
                    int minY = slot.TryGetProperty("min_y", out p) ? p.GetInt32() : 0;
                    int maxX = slot.TryGetProperty("max_x", out p) ? p.GetInt32() : 0;
                    int maxY = slot.TryGetProperty("max_y", out p) ? p.GetInt32() : 0;
                    int widthPx = slot.TryGetProperty("width_px", out p) ? p.GetInt32() : 0;
                    int heightPx = slot.TryGetProperty("height_px", out p) ? p.GetInt32() : 0;
                    int fsb = slot.TryGetProperty("frontage_setback_px", out p) ? p.GetInt32() : 0;
                    int rsb = slot.TryGetProperty("rear_setback_px", out p) ? p.GetInt32() : 0;
                    int si = slot.TryGetProperty("side_inset_px", out p) ? p.GetInt32() : 0;
                    string status = slot.TryGetProperty("slot_status", out p) ? p.GetString() ?? "" : "";
                    string geoType = slot.TryGetProperty("geometry_type", out p) ? p.GetString() ?? "" : "";
                    string geoStatus = slot.TryGetProperty("geometry_status", out p) ? p.GetString() ?? "" : "";
                    normalizedSlots.Add(new DeadMtlAdapterContractNormalizedBuildingSlot
                    {
                        SlotOrder            = slotOrder,
                        SlotId               = slotId,
                        LotId                = lotId,
                        LotOrder             = lotOrder,
                        ComponentId          = compId,
                        MinX                 = minX,
                        MinY                 = minY,
                        MaxX                 = maxX,
                        MaxY                 = maxY,
                        WidthPx              = widthPx,
                        HeightPx             = heightPx,
                        FrontageSetbackPx    = fsb,
                        RearSetbackPx        = rsb,
                        SideInsetPx          = si,
                        SlotStatus           = status,
                        SourceGeometryType   = geoType,
                        SourceGeometryStatus = geoStatus,
                        WriterConsumable     = false,
                        RuntimeConsumable    = false
                    });
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse building slot records: {ex.Message}");
            result.IsValid = false;
            result.Verdict = "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_INVALID";
            return result;
        }
        result.NormalizedBuildingSlots = normalizedSlots;

        // --- parse frontage access record ---
        string frontSide = string.Empty;
        string frontCompId = string.Empty;
        int frontContactPx = 0;
        try
        {
            using var fDoc = JsonDocument.Parse(File.ReadAllBytes(frontageAccessPath));
            var r = fDoc.RootElement;
            if (r.TryGetProperty("frontage_side", out var p)) frontSide = p.GetString() ?? string.Empty;
            if (r.TryGetProperty("frontage_component_id", out p)) frontCompId = p.GetString() ?? string.Empty;
            if (r.TryGetProperty("frontage_contact_px", out p)) frontContactPx = p.GetInt32();
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse frontage access record: {ex.Message}");
            result.IsValid = false;
            result.Verdict = "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_INVALID";
            return result;
        }

        // --- parse rear service access record ---
        string rearSide = string.Empty;
        string rearCompId = string.Empty;
        int rearContactPx = 0;
        try
        {
            using var rDoc = JsonDocument.Parse(File.ReadAllBytes(rearServiceAccessPath));
            var r = rDoc.RootElement;
            if (r.TryGetProperty("rear_service_side", out var p)) rearSide = p.GetString() ?? string.Empty;
            if (r.TryGetProperty("rear_service_component_id", out p)) rearCompId = p.GetString() ?? string.Empty;
            if (r.TryGetProperty("rear_service_contact_px", out p)) rearContactPx = p.GetInt32();
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse rear service access record: {ex.Message}");
            result.IsValid = false;
            result.Verdict = "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_INVALID";
            return result;
        }

        result.NormalizedAccessRecords = new List<DeadMtlAdapterContractNormalizedAccessRecord>
        {
            new()
            {
                AccessOrder      = 1,
                AccessId         = "map_00_comp0001_frontage_access",
                AccessKind       = "FRONTAGE_ACCESS",
                Side             = frontSide,
                ComponentId      = frontCompId,
                ContactPx        = frontContactPx,
                WriterConsumable  = false,
                RuntimeConsumable = false
            },
            new()
            {
                AccessOrder      = 2,
                AccessId         = "map_00_comp0001_rear_service_access",
                AccessKind       = "REAR_SERVICE_ACCESS",
                Side             = rearSide,
                ComponentId      = rearCompId,
                ContactPx        = rearContactPx,
                WriterConsumable  = false,
                RuntimeConsumable = false
            }
        };

        // --- parse claim boundary ---
        bool cbWriterReady = false;
        bool cbRuntimeValid = false;
        bool cbMaterialized = false;
        bool cbApproved = false;
        string cbGateStatus = string.Empty;
        try
        {
            using var cbDoc = JsonDocument.Parse(File.ReadAllBytes(claimBoundaryPath));
            var r = cbDoc.RootElement;
            if (r.TryGetProperty("writer_ready", out var p)) cbWriterReady = p.GetBoolean();
            if (r.TryGetProperty("runtime_valid", out p)) cbRuntimeValid = p.GetBoolean();
            if (r.TryGetProperty("materialized", out p)) cbMaterialized = p.GetBoolean();
            if (r.TryGetProperty("approved_for_writer_experiment", out p)) cbApproved = p.GetBoolean();
            if (r.TryGetProperty("writer_experiment_gate_status", out p)) cbGateStatus = p.GetString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse claim boundary record: {ex.Message}");
            result.IsValid = false;
            result.Verdict = "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_INVALID";
            return result;
        }

        result.WriterReady               = cbWriterReady;
        result.RuntimeValid              = cbRuntimeValid;
        result.Materialized              = cbMaterialized;
        result.ApprovedForWriterExperiment = cbApproved;
        result.WriterExperimentGateStatus  = cbGateStatus;

        // --- forbidden output families ---
        result.ForbiddenOutputFamilies = new List<DeadMtlAdapterContractForbiddenOutputFamily>
        {
            new() { FamilyOrder=1, FamilyId="LOT_PACK_RUNTIME_BINARY",       BlockedPattern="*.lotpack",                 Status="FORBIDDEN", Details="Lot-pack runtime binary files are forbidden. No PZ lot pack is produced." },
            new() { FamilyOrder=2, FamilyId="LOT_HEADER_RUNTIME_BINARY",     BlockedPattern="*.lotheader",               Status="FORBIDDEN", Details="Lot-header runtime binary files are forbidden. No PZ lot header is produced." },
            new() { FamilyOrder=3, FamilyId="WORLDGEN_OVERRIDE_LUA",         BlockedPattern="WorldGenOverride.lua",      Status="FORBIDDEN", Details="WorldGen override Lua is forbidden. No runtime script is produced." },
            new() { FamilyOrder=4, FamilyId="RUNTIME_LUA",                   BlockedPattern="*.lua",                     Status="FORBIDDEN", Details="Runtime Lua scripts are forbidden. No Lua output is produced." },
            new() { FamilyOrder=5, FamilyId="PROJECT_ZOMBOID_INSTALL_PATH",  BlockedPattern="*/Project Zomboid/*",       Status="FORBIDDEN", Details="Project Zomboid installation path writes are forbidden. Nothing is installed into PZ." },
            new() { FamilyOrder=6, FamilyId="STEAM_WORKSHOP_OUTPUT",         BlockedPattern="*/steamapps/workshop/*",    Status="FORBIDDEN", Details="Steam Workshop output paths are forbidden. No workshop upload is performed." },
            new() { FamilyOrder=7, FamilyId="COMPILE_WORLDGEN_INVOCATION",   BlockedPattern="compile-worldgen",          Status="FORBIDDEN", Details="compile-worldgen invocation is forbidden. No compilation step is run." },
            new() { FamilyOrder=8, FamilyId="MAP_00_PNG_MUTATION",           BlockedPattern="map_00.png",                Status="FORBIDDEN", Details="map_00.png source asset mutation is forbidden. The source image is read-only." }
        };

        // --- build 28 checks ---
        int order = 0;

        // 1: MAP26H audit receipt exists (always PASS, past guard)
        checks.Add(MakeCheck(++order, "MAP26H_AUDIT_RECEIPT_EXISTS",
            "MAP-26H audit receipt file exists",
            "PRESENT", "PRESENT",
            "Audit receipt file confirmed present before parsing."));

        // 2: MAP26H audit receipt hashed (always PASS, past guard)
        checks.Add(MakeCheck(++order, "MAP26H_AUDIT_RECEIPT_HASHED",
            "MAP-26H audit receipt SHA-256 computed",
            "SHA256_PRESENT", auditReceiptSha256.Length == 64 ? "SHA256_PRESENT" : "SHA256_MISSING",
            $"SHA-256: {auditReceiptSha256}"));

        // 3: verdict COMPLETE
        string expectedVerdict26H = "MAP26H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMISSION_AUDIT_RECEIPT_COMPLETE";
        checks.Add(AddCheck(++order, "MAP26H_VERDICT_COMPLETE",
            "MAP-26H verdict is COMPLETE",
            expectedVerdict26H, auditVerdict,
            "Verifies the MAP-26H audit produced a COMPLETE verdict."));

        // 4: is_valid true
        checks.Add(AddCheck(++order, "MAP26H_IS_VALID_TRUE",
            "MAP-26H is_valid is true",
            "true", auditIsValid.ToString().ToLower(),
            "Verifies MAP-26H marked the audit as valid."));

        // 5: audited_file_count 12
        checks.Add(AddCheck(++order, "MAP26H_AUDITED_FILE_COUNT_12",
            "MAP-26H audited 12 files",
            "12", auditedFileCount.ToString(),
            "Verifies MAP-26H audited all 12 expected output files."));

        // 6: hash_mismatch_count 0
        checks.Add(AddCheck(++order, "MAP26H_HASH_MISMATCH_COUNT_0",
            "MAP-26H hash mismatch count is 0",
            "0", hashMismatchCount.ToString(),
            "Verifies no emitted record hashes failed verification in MAP-26H."));

        // 7: passed_audit_check_count 29
        checks.Add(AddCheck(++order, "MAP26H_PASSED_AUDIT_CHECKS_29",
            "MAP-26H passed all 29 audit checks",
            "29", passedAuditCheckCount.ToString(),
            "Verifies MAP-26H's 29 audit checks all passed."));

        // 8-13: input file existence (always PASS, past guard)
        checks.Add(MakeCheck(++order, "COMPONENT_RECORD_EXISTS",
            "Component writer record file exists",
            "PRESENT", "PRESENT", "Component record confirmed present before parsing."));

        checks.Add(MakeCheck(++order, "LOT_RECORDS_EXISTS",
            "Lot writer records file exists",
            "PRESENT", "PRESENT", "Lot records confirmed present before parsing."));

        checks.Add(MakeCheck(++order, "BUILDING_SLOT_RECORDS_EXISTS",
            "Building slot writer records file exists",
            "PRESENT", "PRESENT", "Building slot records confirmed present before parsing."));

        checks.Add(MakeCheck(++order, "FRONTAGE_ACCESS_RECORD_EXISTS",
            "Frontage access record file exists",
            "PRESENT", "PRESENT", "Frontage access record confirmed present before parsing."));

        checks.Add(MakeCheck(++order, "REAR_SERVICE_ACCESS_RECORD_EXISTS",
            "Rear service access record file exists",
            "PRESENT", "PRESENT", "Rear service access record confirmed present before parsing."));

        checks.Add(MakeCheck(++order, "CLAIM_BOUNDARY_RECORD_EXISTS",
            "Claim boundary record file exists",
            "PRESENT", "PRESENT", "Claim boundary record confirmed present before parsing."));

        // 14: normalized component stable
        bool compStable = string.Equals(result.NormalizedComponent.TargetComponentId, "map_00_component_0001", StringComparison.Ordinal)
            && string.Equals(result.NormalizedComponent.SourceRecordId, "COMPONENT_WRITER_RECORD", StringComparison.Ordinal);
        checks.Add(new DeadMtlAdapterContractCheck
        {
            CheckOrder  = ++order,
            CheckId     = "NORMALIZED_COMPONENT_STABLE",
            CheckLabel  = "Normalized component target_component_id and source_record_id are canonical",
            CheckStatus = compStable ? "PASS" : "FAIL",
            Expected    = "target_component_id:map_00_component_0001|source_record_id:COMPONENT_WRITER_RECORD",
            Actual      = $"target_component_id:{result.NormalizedComponent.TargetComponentId}|source_record_id:{result.NormalizedComponent.SourceRecordId}",
            Details     = "Verifies the normalized component ID and source_record_id match canonical values."
        });

        // 15: normalized lot count 7
        checks.Add(AddCheck(++order, "NORMALIZED_LOT_COUNT_7",
            "Normalized lot count is 7",
            "7", normalizedLots.Count.ToString(),
            "Verifies 7 lots were normalized from the lot writer records."));

        // 16: normalized building slot count 7
        checks.Add(AddCheck(++order, "NORMALIZED_BUILDING_SLOT_COUNT_7",
            "Normalized building slot count is 7",
            "7", normalizedSlots.Count.ToString(),
            "Verifies 7 building slots were normalized from the slot writer records."));

        // 17: normalized access record count 2
        checks.Add(AddCheck(++order, "NORMALIZED_ACCESS_RECORD_COUNT_2",
            "Normalized access record count is 2",
            "2", result.NormalizedAccessRecords.Count.ToString(),
            "Verifies 2 access records (frontage + rear service) were normalized."));

        // 18: frontage access stable
        var frontageRec = result.NormalizedAccessRecords[0];
        bool frontageStable = string.Equals(frontageRec.Side, "NORTH", StringComparison.Ordinal)
            && string.Equals(frontageRec.AccessKind, "FRONTAGE_ACCESS", StringComparison.Ordinal);
        checks.Add(new DeadMtlAdapterContractCheck
        {
            CheckOrder  = ++order,
            CheckId     = "FRONTAGE_ACCESS_STABLE",
            CheckLabel  = "Frontage access side is NORTH and access_kind is FRONTAGE_ACCESS",
            CheckStatus = frontageStable ? "PASS" : "FAIL",
            Expected    = "side:NORTH|access_kind:FRONTAGE_ACCESS",
            Actual      = $"side:{frontageRec.Side}|access_kind:{frontageRec.AccessKind}",
            Details     = $"Frontage component_id: {frontCompId}, contact_px: {frontContactPx}."
        });

        // 19: rear service access stable
        var rearRec = result.NormalizedAccessRecords[1];
        bool rearStable = string.Equals(rearRec.Side, "EAST", StringComparison.Ordinal)
            && string.Equals(rearRec.AccessKind, "REAR_SERVICE_ACCESS", StringComparison.Ordinal);
        checks.Add(new DeadMtlAdapterContractCheck
        {
            CheckOrder  = ++order,
            CheckId     = "REAR_SERVICE_ACCESS_STABLE",
            CheckLabel  = "Rear service access side is EAST and access_kind is REAR_SERVICE_ACCESS",
            CheckStatus = rearStable ? "PASS" : "FAIL",
            Expected    = "side:EAST|access_kind:REAR_SERVICE_ACCESS",
            Actual      = $"side:{rearRec.Side}|access_kind:{rearRec.AccessKind}",
            Details     = $"Rear service component_id: {rearCompId}, contact_px: {rearContactPx}."
        });

        // 20: forbidden output families listed with canonical IDs and FORBIDDEN status
        string[] canonicalFamilyIds = {
            "LOT_PACK_RUNTIME_BINARY", "LOT_HEADER_RUNTIME_BINARY", "WORLDGEN_OVERRIDE_LUA",
            "RUNTIME_LUA", "PROJECT_ZOMBOID_INSTALL_PATH", "STEAM_WORKSHOP_OUTPUT",
            "COMPILE_WORLDGEN_INVOCATION", "MAP_00_PNG_MUTATION"
        };
        bool familiesCanonical = result.ForbiddenOutputFamilies.Count == 8
            && result.ForbiddenOutputFamilies.Select(f => f.FamilyId).SequenceEqual(canonicalFamilyIds, StringComparer.Ordinal)
            && result.ForbiddenOutputFamilies.All(f => string.Equals(f.Status, "FORBIDDEN", StringComparison.Ordinal));
        checks.Add(new DeadMtlAdapterContractCheck
        {
            CheckOrder  = ++order,
            CheckId     = "FORBIDDEN_OUTPUT_FAMILIES_LISTED",
            CheckLabel  = "8 canonical forbidden output families declared with FORBIDDEN status",
            CheckStatus = familiesCanonical ? "PASS" : "FAIL",
            Expected    = "count:8|ids:canonical|status:FORBIDDEN",
            Actual      = $"count:{result.ForbiddenOutputFamilies.Count}|status_unique:{string.Join(",", result.ForbiddenOutputFamilies.Select(f => f.Status).Distinct())}",
            Details     = "Verifies all 8 canonical forbidden output family IDs are declared with status FORBIDDEN."
        });

        // 21: writer_ready false
        checks.Add(AddCheck(++order, "WRITER_READY_FALSE",
            "writer_ready is false",
            "false", cbWriterReady.ToString().ToLower(),
            "Verifies the claim boundary record declares writer_ready=false."));

        // 22: runtime_valid false
        checks.Add(AddCheck(++order, "RUNTIME_VALID_FALSE",
            "runtime_valid is false",
            "false", cbRuntimeValid.ToString().ToLower(),
            "Verifies the claim boundary record declares runtime_valid=false."));

        // 23: materialized false
        checks.Add(AddCheck(++order, "MATERIALIZED_FALSE",
            "materialized is false",
            "false", cbMaterialized.ToString().ToLower(),
            "Verifies the claim boundary record declares materialized=false."));

        // 24: approved_for_writer_experiment false
        checks.Add(AddCheck(++order, "APPROVED_FOR_WRITER_EXPERIMENT_FALSE",
            "approved_for_writer_experiment is false",
            "false", cbApproved.ToString().ToLower(),
            "Verifies no operator approval has been granted for a writer experiment."));

        // 25: gate status locked
        bool gateContainsLocked = cbGateStatus.StartsWith("LOCKED", StringComparison.OrdinalIgnoreCase);
        checks.Add(new DeadMtlAdapterContractCheck
        {
            CheckOrder  = ++order,
            CheckId     = "GATE_STATUS_LOCKED",
            CheckLabel  = "Writer experiment gate status contains LOCKED",
            CheckStatus = gateContainsLocked ? "PASS" : "FAIL",
            Expected    = "contains:LOCKED",
            Actual      = cbGateStatus,
            Details     = "Verifies the gate status is locked pending operator approval."
        });

        // 26: no writer consumable records
        bool anyWriterConsumable =
            result.NormalizedComponent.WriterConsumable ||
            normalizedLots.Any(l => l.WriterConsumable) ||
            normalizedSlots.Any(s => s.WriterConsumable) ||
            result.NormalizedAccessRecords.Any(a => a.WriterConsumable);
        checks.Add(AddCheck(++order, "NO_WRITER_CONSUMABLE_RECORDS",
            "No normalized records are writer-consumable",
            "false", anyWriterConsumable.ToString().ToLower(),
            "Verifies all normalized records have writer_consumable=false."));

        // 27: no runtime consumable records
        bool anyRuntimeConsumable =
            result.NormalizedComponent.RuntimeConsumable ||
            normalizedLots.Any(l => l.RuntimeConsumable) ||
            normalizedSlots.Any(s => s.RuntimeConsumable) ||
            result.NormalizedAccessRecords.Any(a => a.RuntimeConsumable);
        checks.Add(AddCheck(++order, "NO_RUNTIME_CONSUMABLE_RECORDS",
            "No normalized records are runtime-consumable",
            "false", anyRuntimeConsumable.ToString().ToLower(),
            "Verifies all normalized records have runtime_consumable=false."));

        // 28: no forbidden outputs created (always PASS)
        checks.Add(MakeCheck(++order, "NO_FORBIDDEN_OUTPUTS_CREATED",
            "No forbidden output files were created",
            "NONE_CREATED", "NONE_CREATED",
            "Adapter contract does not produce .lotpack, .lotheader, .lua, or PZ runtime artifacts."));

        result.Checks = checks;
        result.AdapterCheckCount       = checks.Count;
        result.PassedAdapterCheckCount = checks.Count(c => string.Equals(c.CheckStatus, "PASS", StringComparison.Ordinal));
        result.FailedAdapterCheckCount = checks.Count(c => string.Equals(c.CheckStatus, "FAIL", StringComparison.Ordinal));

        result.IsValid = result.FailedAdapterCheckCount == 0 && result.Errors.Count == 0;
        result.Verdict = result.IsValid
            ? "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_COMPLETE"
            : "MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_INVALID";

        return result;
    }

    private static DeadMtlAdapterContractCheck MakeCheck(int order, string id, string label, string expected, string actual, string details)
    {
        return new DeadMtlAdapterContractCheck
        {
            CheckOrder  = order,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = "PASS",
            Expected    = expected,
            Actual      = actual,
            Details     = details
        };
    }

    private static DeadMtlAdapterContractCheck AddCheck(int order, string id, string label, string expected, string actual, string details)
    {
        return new DeadMtlAdapterContractCheck
        {
            CheckOrder  = order,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = string.Equals(expected, actual, StringComparison.Ordinal) ? "PASS" : "FAIL",
            Expected    = expected,
            Actual      = actual,
            Details     = details
        };
    }

    public string RenderJson(DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractResult result)
        => JsonSerializer.Serialize(result, s_jsonOptions);

    public string RenderMarkdown(DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# MAP-26I WorldBuilder Minimal Concrete Geometry Writer Adapter Contract");
        sb.AppendLine();
        sb.AppendLine($"- **Map ID:** {result.MapId}");
        sb.AppendLine($"- **Target Component:** {result.TargetComponentId}");
        sb.AppendLine($"- **Generated UTC:** {result.GeneratedUtc}");
        sb.AppendLine($"- **Adapter Contract Status:** {result.AdapterContractStatus}");
        sb.AppendLine($"- **Verdict:** {result.Verdict}");
        sb.AppendLine($"- **Is Valid:** {result.IsValid}");
        sb.AppendLine($"- **Writer Ready:** {result.WriterReady}");
        sb.AppendLine($"- **Runtime Valid:** {result.RuntimeValid}");
        sb.AppendLine($"- **Materialized:** {result.Materialized}");
        sb.AppendLine();
        sb.AppendLine("## Source Audit Receipt");
        sb.AppendLine();
        sb.AppendLine($"- **Path:** {result.SourceAuditReceiptPath}");
        sb.AppendLine($"- **SHA-256:** {result.SourceAuditReceiptSha256}");
        sb.AppendLine($"- **Verdict:** {result.SourceAuditReceiptVerdict}");
        sb.AppendLine($"- **Is Valid:** {result.SourceAuditReceiptIsValid}");
        sb.AppendLine();
        sb.AppendLine("## Normalized Component");
        sb.AppendLine();
        sb.AppendLine($"- **Target Component ID:** {result.NormalizedComponent.TargetComponentId}");
        sb.AppendLine($"- **Intent:** {result.NormalizedComponent.Intent}");
        sb.AppendLine($"- **Access Readiness:** {result.NormalizedComponent.AccessReadinessClass}");
        sb.AppendLine($"- **BBox:** ({result.NormalizedComponent.BboxMinX},{result.NormalizedComponent.BboxMinY}) to ({result.NormalizedComponent.BboxMaxX},{result.NormalizedComponent.BboxMaxY}) {result.NormalizedComponent.BboxWidthPx}x{result.NormalizedComponent.BboxHeightPx}px");
        sb.AppendLine($"- **Writer Consumable:** {result.NormalizedComponent.WriterConsumable}");
        sb.AppendLine($"- **Runtime Consumable:** {result.NormalizedComponent.RuntimeConsumable}");
        sb.AppendLine();
        sb.AppendLine("## Normalized Lots");
        sb.AppendLine();
        sb.AppendLine("| Order | Lot ID | MinX | MinY | MaxX | MaxY | W | H | Writer | Runtime |");
        sb.AppendLine("|-------|--------|------|------|------|------|---|---|--------|---------|");
        foreach (var lot in result.NormalizedLots)
            sb.AppendLine($"| {lot.LotOrder} | {lot.LotId} | {lot.MinX} | {lot.MinY} | {lot.MaxX} | {lot.MaxY} | {lot.WidthPx} | {lot.HeightPx} | {lot.WriterConsumable} | {lot.RuntimeConsumable} |");
        sb.AppendLine();
        sb.AppendLine("## Normalized Building Slots");
        sb.AppendLine();
        sb.AppendLine("| Order | Slot ID | Lot ID | MinX | MinY | MaxX | MaxY | W | H | Writer | Runtime |");
        sb.AppendLine("|-------|---------|--------|------|------|------|------|---|---|--------|---------|");
        foreach (var slot in result.NormalizedBuildingSlots)
            sb.AppendLine($"| {slot.SlotOrder} | {slot.SlotId} | {slot.LotId} | {slot.MinX} | {slot.MinY} | {slot.MaxX} | {slot.MaxY} | {slot.WidthPx} | {slot.HeightPx} | {slot.WriterConsumable} | {slot.RuntimeConsumable} |");
        sb.AppendLine();
        sb.AppendLine("## Normalized Access Records");
        sb.AppendLine();
        sb.AppendLine("| Order | Kind | Side | Component | Contact px | Writer | Runtime |");
        sb.AppendLine("|-------|------|------|-----------|------------|--------|---------|");
        foreach (var ar in result.NormalizedAccessRecords)
            sb.AppendLine($"| {ar.AccessOrder} | {ar.AccessKind} | {ar.Side} | {ar.ComponentId} | {ar.ContactPx} | {ar.WriterConsumable} | {ar.RuntimeConsumable} |");
        sb.AppendLine();
        sb.AppendLine("## Forbidden Output Families");
        sb.AppendLine();
        sb.AppendLine("| Order | Family ID | Blocked Pattern | Status |");
        sb.AppendLine("|-------|-----------|-----------------|--------|");
        foreach (var f in result.ForbiddenOutputFamilies)
            sb.AppendLine($"| {f.FamilyOrder} | {f.FamilyId} | `{f.BlockedPattern}` | {f.Status} |");
        sb.AppendLine();
        sb.AppendLine("## Adapter Checks");
        sb.AppendLine();
        sb.AppendLine($"Total: {result.AdapterCheckCount} | Passed: {result.PassedAdapterCheckCount} | Failed: {result.FailedAdapterCheckCount}");
        sb.AppendLine();
        sb.AppendLine("| # | Check ID | Status | Expected | Actual |");
        sb.AppendLine("|---|----------|--------|----------|--------|");
        foreach (var c in result.Checks)
            sb.AppendLine($"| {c.CheckOrder} | {c.CheckId} | {c.CheckStatus} | {c.Expected} | {c.Actual} |");
        if (result.Errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Errors");
            sb.AppendLine();
            foreach (var e in result.Errors)
                sb.AppendLine($"- {e}");
        }
        return sb.ToString().TrimEnd();
    }

    public string RenderCsv(DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_order,check_id,check_label,check_status,expected,actual,details");
        foreach (var c in result.Checks)
        {
            string CsvEsc(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
            sb.AppendLine($"{c.CheckOrder},{CsvEsc(c.CheckId)},{CsvEsc(c.CheckLabel)},{c.CheckStatus},{CsvEsc(c.Expected)},{CsvEsc(c.Actual)},{CsvEsc(c.Details)}");
        }
        return sb.ToString().TrimEnd();
    }

    public string RenderSummary(DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"MAP-26I WorldBuilder Minimal Concrete Geometry Writer Adapter Contract");
        sb.AppendLine($"Generated UTC    : {result.GeneratedUtc}");
        sb.AppendLine($"Map ID           : {result.MapId}");
        sb.AppendLine($"Target Component : {result.TargetComponentId}");
        sb.AppendLine($"Contract Status  : {result.AdapterContractStatus}");
        sb.AppendLine($"Verdict          : {result.Verdict}");
        sb.AppendLine($"Is Valid         : {(result.IsValid ? 1 : 0)}");
        sb.AppendLine($"Writer Ready     : {(result.WriterReady ? 1 : 0)}");
        sb.AppendLine($"Runtime Valid    : {(result.RuntimeValid ? 1 : 0)}");
        sb.AppendLine($"Materialized     : {(result.Materialized ? 1 : 0)}");
        sb.AppendLine($"Approved Exp     : {(result.ApprovedForWriterExperiment ? 1 : 0)}");
        sb.AppendLine($"Gate Status      : {result.WriterExperimentGateStatus}");
        sb.AppendLine($"Normalized Lots  : {result.NormalizedLots.Count}");
        sb.AppendLine($"Normalized Slots : {result.NormalizedBuildingSlots.Count}");
        sb.AppendLine($"Access Records   : {result.NormalizedAccessRecords.Count}");
        sb.AppendLine($"Forbidden Fams   : {result.ForbiddenOutputFamilies.Count}");
        sb.AppendLine($"Checks           : {result.AdapterCheckCount}");
        sb.AppendLine($"Passed           : {result.PassedAdapterCheckCount}");
        sb.AppendLine($"Failed           : {result.FailedAdapterCheckCount}");
        if (result.Errors.Count > 0)
        {
            sb.AppendLine($"Errors           : {result.Errors.Count}");
            foreach (var e in result.Errors)
                sb.AppendLine($"  ERROR: {e}");
        }
        sb.Append($"Audit Receipt SHA: {result.SourceAuditReceiptSha256}");
        return sb.ToString();
    }
}
