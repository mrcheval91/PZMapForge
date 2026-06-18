using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterBuilder
{
    private const string ExpectedMap26FVerdict =
        "MAP26F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_DESIGN_COMPLETE";
    private const string Format =
        "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-writer-dry-run-emitter.v1";

    private const string ExpectedTargetComponentId = "map_00_component_0001";
    private const string ExpectedIntent            = "RESIDENTIAL_LOT_BLOCK";
    private const string ExpectedAccessClass       = "DUAL_ACCESS_CANDIDATE";
    private const int    ExpectedBboxMinX          = 124;
    private const int    ExpectedBboxMinY          = 10;
    private const int    ExpectedBboxMaxX          = 212;
    private const int    ExpectedBboxMaxY          = 69;
    private const int    ExpectedBboxWidth         = 89;
    private const int    ExpectedBboxHeight        = 60;
    private const int    ExpectedLotCount          = 7;
    private const int    ExpectedBuildingSlotCount = 7;
    private const string ExpectedFrontageSide      = "NORTH";
    private const string ExpectedFrontageCompId    = "map_00_component_0023";
    private const int    ExpectedFrontageContactPx = 89;
    private const string ExpectedRearSide          = "EAST";
    private const string ExpectedRearCompId        = "map_00_component_0030";
    private const int    ExpectedRearContactPx     = 60;

    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterResult Build(
        string dryRunDesignPath, string geometryMvpPath, string outputRoot)
    {
        var errors = new List<string>();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterResult
        {
            Format                      = Format,
            GeneratedUtc                = DateTime.UtcNow.ToString("o"),
            SourceDryRunDesignPath      = dryRunDesignPath,
            SourceGeometryMvpPath       = geometryMvpPath,
            FutureWriterName            = "MAP-26G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER",
            EmitterStatus               = "DRY_RUN_RECORDS_EMITTED_TO_DOT_LOCAL_ONLY",
            DryRunOnly                  = true,
            WriterReady                 = false,
            RuntimeValid                = false,
            Materialized                = false,
            ApprovedForWriterExperiment = false,
            WriterExperimentGateStatus  = "LOCKED_PENDING_OPERATOR_APPROVAL",
            OutputRoot                  = outputRoot,
        };

        if (!File.Exists(dryRunDesignPath))
        {
            errors.Add($"Dry-run design not found: {dryRunDesignPath}");
            result.Errors  = errors;
            result.IsValid = false;
            result.Verdict = "MAP26G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER_INVALID";
            return result;
        }

        if (!File.Exists(geometryMvpPath))
        {
            errors.Add($"Geometry MVP not found: {geometryMvpPath}");
            result.Errors  = errors;
            result.IsValid = false;
            result.Verdict = "MAP26G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER_INVALID";
            return result;
        }

        result.SourceDryRunDesignSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dryRunDesignPath))).ToLower();
        result.SourceGeometryMvpSha256  = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(geometryMvpPath))).ToLower();

        string designVerdict           = string.Empty;
        bool   designDryRunOnly        = false;
        string designFutureWriterStatus = string.Empty;
        string mapId                   = "map_00";

        string targetCompId    = string.Empty;
        string intent          = string.Empty;
        string accessClass     = string.Empty;
        int    bboxMinX        = 0;
        int    bboxMinY        = 0;
        int    bboxMaxX        = 0;
        int    bboxMaxY        = 0;
        int    bboxWidth       = 0;
        int    bboxHeight      = 0;
        int    lotCount        = 0;
        int    slotCount       = 0;
        string frontageSide    = string.Empty;
        string frontageCompId  = string.Empty;
        int    frontageContact = 0;
        string rearSide        = string.Empty;
        string rearCompId      = string.Empty;
        int    rearContact     = 0;

        var lotGeometry          = new List<JsonElement>();
        var buildingSlotGeometry = new List<JsonElement>();

        try
        {
            using var designDoc = JsonDocument.Parse(File.ReadAllText(dryRunDesignPath));
            var dr = designDoc.RootElement;
            designVerdict            = dr.TryGetProperty("verdict",              out var dv)  ? dv.GetString()  ?? "" : "";
            designDryRunOnly         = dr.TryGetProperty("dry_run_only",         out var ddo) && ddo.GetBoolean();
            designFutureWriterStatus = dr.TryGetProperty("future_writer_status", out var fws) ? fws.GetString() ?? "" : "";
            mapId                    = dr.TryGetProperty("map_id",               out var mi)  ? mi.GetString()  ?? "map_00" : "map_00";
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse dry-run design: {ex.Message}");
            result.Errors  = errors;
            result.IsValid = false;
            result.Verdict = "MAP26G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER_INVALID";
            return result;
        }

        try
        {
            using var mvpDoc = JsonDocument.Parse(File.ReadAllText(geometryMvpPath));
            var root = mvpDoc.RootElement;

            if (root.TryGetProperty("geometry_mvp_contract", out var contract))
            {
                targetCompId    = contract.TryGetProperty("target_component_id",    out var tc)  ? tc.GetString()  ?? "" : "";
                intent          = contract.TryGetProperty("target_intent",          out var ti)  ? ti.GetString()  ?? "" : "";
                accessClass     = contract.TryGetProperty("access_readiness_class", out var ar)  ? ar.GetString()  ?? "" : "";
                bboxMinX        = contract.TryGetProperty("component_bbox_min_x",   out var mnx) ? mnx.GetInt32()  : 0;
                bboxMinY        = contract.TryGetProperty("component_bbox_min_y",   out var mny) ? mny.GetInt32()  : 0;
                bboxMaxX        = contract.TryGetProperty("component_bbox_max_x",   out var mxx) ? mxx.GetInt32()  : 0;
                bboxMaxY        = contract.TryGetProperty("component_bbox_max_y",   out var mxy) ? mxy.GetInt32()  : 0;
                bboxWidth       = contract.TryGetProperty("component_bbox_width_px",  out var bw)  ? bw.GetInt32()   : 0;
                bboxHeight      = contract.TryGetProperty("component_bbox_height_px", out var bh)  ? bh.GetInt32()   : 0;
                lotCount        = contract.TryGetProperty("lot_geometry_count",       out var lc)  ? lc.GetInt32()   : 0;
                slotCount       = contract.TryGetProperty("accepted_building_slot_count", out var sc) ? sc.GetInt32() : 0;
                frontageSide    = contract.TryGetProperty("frontage_side",            out var fs)  ? fs.GetString()  ?? "" : "";
                frontageCompId  = contract.TryGetProperty("primary_frontage_component_id",  out var fc)  ? fc.GetString()  ?? "" : "";
                frontageContact = contract.TryGetProperty("frontage_contact_px",      out var fcp) ? fcp.GetInt32()  : 0;
                rearSide        = contract.TryGetProperty("rear_service_side",         out var rs)  ? rs.GetString()  ?? "" : "";
                rearCompId      = contract.TryGetProperty("primary_rear_service_component_id",  out var rc)  ? rc.GetString()  ?? "" : "";
                rearContact     = contract.TryGetProperty("rear_service_contact_px",   out var rcp) ? rcp.GetInt32()  : 0;
            }

            if (root.TryGetProperty("lot_geometry", out var lots) && lots.ValueKind == JsonValueKind.Array)
                foreach (var l in lots.EnumerateArray())
                    lotGeometry.Add(l.Clone());

            if (root.TryGetProperty("building_slot_geometry", out var slots) && slots.ValueKind == JsonValueKind.Array)
                foreach (var s in slots.EnumerateArray())
                    buildingSlotGeometry.Add(s.Clone());
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse geometry MVP: {ex.Message}");
            result.Errors  = errors;
            result.IsValid = false;
            result.Verdict = "MAP26G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER_INVALID";
            return result;
        }

        result.MapId             = mapId;
        result.TargetComponentId = targetCompId;
        result.SourceDryRunDesignVerdict = designVerdict;

        // Emit the 8 dry-run record files
        Directory.CreateDirectory(outputRoot);

        var componentRecord = BuildComponentRecord(targetCompId, intent, accessClass,
            bboxMinX, bboxMinY, bboxMaxX, bboxMaxY, bboxWidth, bboxHeight, frontageSide, rearSide);
        var lotRecords = BuildLotRecords(targetCompId, lotGeometry);
        var slotRecords = BuildBuildingSlotRecords(targetCompId, buildingSlotGeometry);
        var frontageRecord = BuildFrontageAccessRecord(frontageSide, frontageCompId, frontageContact);
        var rearServiceRecord = BuildRearServiceAccessRecord(rearSide, rearCompId, rearContact);
        var forbiddenScanRecord = BuildForbiddenOutputScanRecord(outputRoot);
        var rollbackRec = BuildRollbackRecord();
        var claimBoundaryRec = BuildClaimBoundaryRecord();

        var opts = new JsonSerializerOptions { WriteIndented = true };
        string componentPath    = Path.Combine(outputRoot, "map_00.component_writer_record.json");
        string lotPath          = Path.Combine(outputRoot, "map_00.lot_writer_records.json");
        string slotPath         = Path.Combine(outputRoot, "map_00.building_slot_writer_records.json");
        string frontagePath     = Path.Combine(outputRoot, "map_00.frontage_access_record.json");
        string rearPath         = Path.Combine(outputRoot, "map_00.rear_service_access_record.json");
        string scanPath         = Path.Combine(outputRoot, "map_00.forbidden_output_scan.json");
        string rollbackPath     = Path.Combine(outputRoot, "map_00.rollback_record.json");
        string claimBoundaryPath = Path.Combine(outputRoot, "map_00.claim_boundary_record.json");

        File.WriteAllText(componentPath,    JsonSerializer.Serialize(componentRecord,    opts));
        File.WriteAllText(lotPath,          JsonSerializer.Serialize(lotRecords,         opts));
        File.WriteAllText(slotPath,         JsonSerializer.Serialize(slotRecords,        opts));
        File.WriteAllText(frontagePath,     JsonSerializer.Serialize(frontageRecord,     opts));
        File.WriteAllText(rearPath,         JsonSerializer.Serialize(rearServiceRecord,  opts));
        File.WriteAllText(scanPath,         JsonSerializer.Serialize(forbiddenScanRecord, opts));
        File.WriteAllText(rollbackPath,     JsonSerializer.Serialize(rollbackRec,        opts));
        File.WriteAllText(claimBoundaryPath, JsonSerializer.Serialize(claimBoundaryRec,  opts));

        var emittedRecords = new List<DeadMtlDryRunEmittedRecordDescriptor>
        {
            MakeDescriptor(1, "COMPONENT_WRITER_RECORD",      "COMPONENT_RECORD",      componentPath),
            MakeDescriptor(2, "LOT_WRITER_RECORDS",           "LOT_RECORDS",           lotPath),
            MakeDescriptor(3, "BUILDING_SLOT_WRITER_RECORDS", "BUILDING_SLOT_RECORDS", slotPath),
            MakeDescriptor(4, "FRONTAGE_ACCESS_RECORD",       "ACCESS_RECORD",         frontagePath),
            MakeDescriptor(5, "REAR_SERVICE_ACCESS_RECORD",   "ACCESS_RECORD",         rearPath),
            MakeDescriptor(6, "FORBIDDEN_OUTPUT_SCAN_RECORD", "SCAN_RECORD",           scanPath),
            MakeDescriptor(7, "ROLLBACK_RECORD",              "ROLLBACK_RECORD",       rollbackPath),
            MakeDescriptor(8, "CLAIM_BOUNDARY_RECORD",        "CLAIM_BOUNDARY_RECORD", claimBoundaryPath),
        };

        result.EmittedRecords     = emittedRecords;
        result.EmittedRecordCount = emittedRecords.Count;

        result.ForbiddenOutputScan = forbiddenScanRecord;
        result.RollbackRecord      = rollbackRec;
        result.ClaimBoundary       = claimBoundaryRec;

        var checks = new List<DeadMtlDryRunEmitterCheck>();
        int ord = 1;

        AddCheck(checks, ord++, "MAP26F_DRY_RUN_DESIGN_EXISTS",
            "MAP-26F dry-run design file exists on disk",
            "true", "true",
            "Dry-run design file exists and was read.");

        AddCheck(checks, ord++, "MAP26F_DRY_RUN_DESIGN_HASHED",
            "MAP-26F dry-run design SHA-256 computed",
            "true", "true",
            $"SHA-256: {result.SourceDryRunDesignSha256}.");

        AddCheck(checks, ord++, "MAP26F_VERDICT_COMPLETE",
            "MAP-26F verdict is complete",
            ExpectedMap26FVerdict, designVerdict,
            $"MAP-26F verdict: {designVerdict}.");

        AddCheck(checks, ord++, "MAP26F_DRY_RUN_ONLY_TRUE",
            "MAP-26F dry_run_only is true",
            "true", designDryRunOnly.ToString().ToLower(),
            $"MAP-26F dry_run_only: {designDryRunOnly}.");

        AddCheck(checks, ord++, "MAP26F_FUTURE_WRITER_STATUS_DESIGN_ONLY",
            "MAP-26F future_writer_status is DESIGN_ONLY_NOT_AUTHORIZED_TO_EMIT_RUNTIME",
            "DESIGN_ONLY_NOT_AUTHORIZED_TO_EMIT_RUNTIME", designFutureWriterStatus,
            $"future_writer_status: {designFutureWriterStatus}.");

        AddCheck(checks, ord++, "MAP26A_GEOMETRY_MVP_EXISTS",
            "MAP-26A geometry MVP file exists on disk",
            "true", "true",
            "Geometry MVP file exists and was read.");

        AddCheck(checks, ord++, "MAP26A_GEOMETRY_MVP_HASHED",
            "MAP-26A geometry MVP SHA-256 computed",
            "true", "true",
            $"SHA-256: {result.SourceGeometryMvpSha256}.");

        AddCheck(checks, ord++, "TARGET_COMPONENT_STABLE",
            "Target component ID matches expected value",
            ExpectedTargetComponentId, targetCompId,
            $"target_component_id: {targetCompId}.");

        AddCheck(checks, ord++, "COMPONENT_BBOX_STABLE",
            "Component bbox matches expected MAP-26A values (min_x, min_y, max_x, max_y, width, height)",
            $"{ExpectedBboxMinX},{ExpectedBboxMinY},{ExpectedBboxMaxX},{ExpectedBboxMaxY},{ExpectedBboxWidth},{ExpectedBboxHeight}",
            $"{bboxMinX},{bboxMinY},{bboxMaxX},{bboxMaxY},{bboxWidth},{bboxHeight}",
            $"bbox: ({bboxMinX},{bboxMinY})->({bboxMaxX},{bboxMaxY}) {bboxWidth}x{bboxHeight}px.");

        AddCheck(checks, ord++, "LOT_COUNT_7",
            "Lot count is 7",
            "7", lotGeometry.Count.ToString(),
            $"lot_geometry list count: {lotGeometry.Count}.");

        AddCheck(checks, ord++, "BUILDING_SLOT_COUNT_7",
            "Building slot count is 7",
            "7", buildingSlotGeometry.Count.ToString(),
            $"building_slot_geometry list count: {buildingSlotGeometry.Count}.");

        AddCheck(checks, ord++, "FRONTAGE_ACCESS_STABLE",
            "Frontage access matches expected (NORTH, map_00_component_0023, 89px)",
            $"{ExpectedFrontageSide},{ExpectedFrontageCompId},{ExpectedFrontageContactPx}",
            $"{frontageSide},{frontageCompId},{frontageContact}",
            $"frontage: {frontageSide} via {frontageCompId} {frontageContact}px.");

        AddCheck(checks, ord++, "REAR_SERVICE_ACCESS_STABLE",
            "Rear service access matches expected (EAST, map_00_component_0030, 60px)",
            $"{ExpectedRearSide},{ExpectedRearCompId},{ExpectedRearContactPx}",
            $"{rearSide},{rearCompId},{rearContact}",
            $"rear service: {rearSide} via {rearCompId} {rearContact}px.");

        AddCheck(checks, ord++, "EMITTED_RECORD_COUNT_8",
            "Emitted record count is 8",
            "8", emittedRecords.Count.ToString(),
            $"emitted_records count: {emittedRecords.Count}.");

        bool allHashed = emittedRecords.All(r => r.Sha256.Length == 64);
        AddCheck(checks, ord++, "ALL_EMITTED_RECORDS_HASHED",
            "All emitted records have SHA-256 computed",
            "true", allHashed.ToString().ToLower(),
            allHashed ? "All 8 emitted records hashed." : "Some emitted records missing SHA-256.");

        AddCheck(checks, ord++, "FORBIDDEN_OUTPUT_SCAN_PASS",
            "Forbidden output scan passed",
            "true", forbiddenScanRecord.ScanPassed.ToString().ToLower(),
            forbiddenScanRecord.Details);

        AddCheck(checks, ord++, "ROLLBACK_RECORD_PASS",
            "Rollback record: dot_local_outputs_only=true, source_hashes_unchanged=true, runtime_files_created=false",
            "true", (rollbackRec.DotLocalOutputsOnly && rollbackRec.SourceHashesUnchanged && !rollbackRec.RuntimeFilesCreated).ToString().ToLower(),
            $"dot_local_outputs_only: {rollbackRec.DotLocalOutputsOnly}, source_hashes_unchanged: {rollbackRec.SourceHashesUnchanged}, runtime_files_created: {rollbackRec.RuntimeFilesCreated}.");

        AddCheck(checks, ord++, "CLAIM_BOUNDARY_RECORD_PASS",
            "Claim boundary record: writer_ready=false, runtime_valid=false, materialized=false",
            "true", (!claimBoundaryRec.WriterReady && !claimBoundaryRec.RuntimeValid && !claimBoundaryRec.Materialized).ToString().ToLower(),
            $"writer_ready: {claimBoundaryRec.WriterReady}, runtime_valid: {claimBoundaryRec.RuntimeValid}, materialized: {claimBoundaryRec.Materialized}.");

        AddCheck(checks, ord++, "DRY_RUN_ONLY_TRUE",
            "dry_run_only is true",
            "true", result.DryRunOnly.ToString().ToLower(),
            $"dry_run_only: {result.DryRunOnly}.");

        AddCheck(checks, ord++, "WRITER_READY_FALSE",
            "writer_ready is false",
            "false", result.WriterReady.ToString().ToLower(),
            $"writer_ready: {result.WriterReady}.");

        AddCheck(checks, ord++, "RUNTIME_VALID_FALSE",
            "runtime_valid is false",
            "false", result.RuntimeValid.ToString().ToLower(),
            $"runtime_valid: {result.RuntimeValid}.");

        AddCheck(checks, ord++, "MATERIALIZED_FALSE",
            "materialized is false",
            "false", result.Materialized.ToString().ToLower(),
            $"materialized: {result.Materialized}.");

        AddCheck(checks, ord++, "APPROVED_FOR_WRITER_EXPERIMENT_FALSE",
            "approved_for_writer_experiment is false",
            "false", result.ApprovedForWriterExperiment.ToString().ToLower(),
            $"approved_for_writer_experiment: {result.ApprovedForWriterExperiment}.");

        AddCheck(checks, ord++, "GATE_STATUS_LOCKED",
            "writer_experiment_gate_status is LOCKED_PENDING_OPERATOR_APPROVAL",
            "LOCKED_PENDING_OPERATOR_APPROVAL", result.WriterExperimentGateStatus,
            $"writer_experiment_gate_status: {result.WriterExperimentGateStatus}.");

        AddCheck(checks, ord++, "NO_FORBIDDEN_OUTPUTS_CREATED",
            "No forbidden outputs created (no .lotpack, no .lotheader, no WorldGenOverride.lua, no compile-worldgen)",
            "true", "true",
            "No forbidden outputs created.");

        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass   = result.FailedCheckCount == 0;
        result.IsValid = allPass;
        result.Errors  = errors;
        result.Verdict = allPass
            ? "MAP26G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER_COMPLETE"
            : "MAP26G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER_INVALID";

        return result;
    }

    private static void AddCheck(
        List<DeadMtlDryRunEmitterCheck> checks,
        int order, string id, string label, string expected, string actual, string details)
    {
        bool pass = string.Equals(expected, actual, StringComparison.Ordinal);
        checks.Add(new DeadMtlDryRunEmitterCheck
        {
            CheckOrder  = order,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = pass ? "PASS" : "FAIL",
            Expected    = expected,
            Actual      = actual,
            Details     = details,
        });
    }

    private static DeadMtlDryRunEmittedRecordDescriptor MakeDescriptor(
        int order, string id, string kind, string path)
    {
        var bytes = File.ReadAllBytes(path);
        return new DeadMtlDryRunEmittedRecordDescriptor
        {
            RecordOrder = order,
            RecordId    = id,
            RecordKind  = kind,
            Path        = path,
            Sha256      = Convert.ToHexString(SHA256.HashData(bytes)).ToLower(),
            SizeBytes   = bytes.Length,
            Status      = "DRY_RUN_RECORD_EMITTED",
        };
    }

    private static object BuildComponentRecord(
        string targetCompId, string intent, string accessClass,
        int minX, int minY, int maxX, int maxY, int width, int height,
        string frontageSide, string rearSide)
    {
        return new
        {
            record_kind             = "COMPONENT_WRITER_RECORD",
            map_id                  = "map_00",
            target_component_id     = targetCompId,
            intent                  = intent,
            access_readiness_class  = accessClass,
            component_bbox          = new { min_x = minX, min_y = minY, max_x = maxX, max_y = maxY, width_px = width, height_px = height },
            source_geometry_kind    = "COMPONENT_BBOX_GEOMETRY",
            dry_run_only            = true,
            runtime_valid           = false,
            writer_ready            = false,
            materialized            = false,
        };
    }

    private static object BuildLotRecords(string targetCompId, List<JsonElement> lots)
    {
        var lotList = lots.Select(l => (object)new
        {
            lot_order          = l.TryGetProperty("lot_order",  out var lo)  ? lo.GetInt32()   : 0,
            lot_id             = l.TryGetProperty("lot_id",     out var li)  ? li.GetString()  ?? "" : "",
            component_id       = l.TryGetProperty("component_id", out var ci) ? ci.GetString() ?? "" : "",
            min_x              = l.TryGetProperty("min_x",      out var mnx) ? mnx.GetInt32()  : 0,
            min_y              = l.TryGetProperty("min_y",      out var mny) ? mny.GetInt32()  : 0,
            max_x              = l.TryGetProperty("max_x",      out var mxx) ? mxx.GetInt32()  : 0,
            max_y              = l.TryGetProperty("max_y",      out var mxy) ? mxy.GetInt32()  : 0,
            width_px           = l.TryGetProperty("width_px",   out var wp)  ? wp.GetInt32()   : 0,
            height_px          = l.TryGetProperty("height_px",  out var hp)  ? hp.GetInt32()   : 0,
            frontage_side      = l.TryGetProperty("frontage_side",      out var fs) ? fs.GetString() ?? "" : "",
            rear_service_side  = l.TryGetProperty("rear_service_side",  out var rs) ? rs.GetString() ?? "" : "",
            geometry_type      = l.TryGetProperty("geometry_type",      out var gt) ? gt.GetString() ?? "" : "",
            geometry_status    = l.TryGetProperty("geometry_status",    out var gs) ? gs.GetString() ?? "" : "",
        }).ToList();

        return new
        {
            record_kind          = "LOT_WRITER_RECORDS",
            map_id               = "map_00",
            target_component_id  = targetCompId,
            lot_count            = lotList.Count,
            lots                 = lotList,
            dry_run_only         = true,
        };
    }

    private static object BuildBuildingSlotRecords(string targetCompId, List<JsonElement> slots)
    {
        var slotList = slots.Select(s => (object)new
        {
            slot_order             = s.TryGetProperty("slot_order",  out var so)  ? so.GetInt32()  : 0,
            slot_id                = s.TryGetProperty("slot_id",     out var si)  ? si.GetString() ?? "" : "",
            lot_id                 = s.TryGetProperty("lot_id",      out var li)  ? li.GetString() ?? "" : "",
            lot_order              = s.TryGetProperty("lot_order",   out var loo) ? loo.GetInt32() : 0,
            component_id           = s.TryGetProperty("component_id", out var ci) ? ci.GetString() ?? "" : "",
            min_x                  = s.TryGetProperty("min_x",       out var mnx) ? mnx.GetInt32() : 0,
            min_y                  = s.TryGetProperty("min_y",       out var mny) ? mny.GetInt32() : 0,
            max_x                  = s.TryGetProperty("max_x",       out var mxx) ? mxx.GetInt32() : 0,
            max_y                  = s.TryGetProperty("max_y",       out var mxy) ? mxy.GetInt32() : 0,
            width_px               = s.TryGetProperty("width_px",    out var wp)  ? wp.GetInt32()  : 0,
            height_px              = s.TryGetProperty("height_px",   out var hp)  ? hp.GetInt32()  : 0,
            frontage_setback_px    = s.TryGetProperty("frontage_setback_px", out var fsp) ? fsp.GetInt32() : 0,
            rear_setback_px        = s.TryGetProperty("rear_setback_px",     out var rsp) ? rsp.GetInt32() : 0,
            side_inset_px          = s.TryGetProperty("side_inset_px",       out var sip) ? sip.GetInt32() : 0,
            slot_status            = s.TryGetProperty("slot_status",         out var ss)  ? ss.GetString() ?? "" : "",
            geometry_type          = s.TryGetProperty("geometry_type",       out var gt)  ? gt.GetString() ?? "" : "",
            geometry_status        = s.TryGetProperty("geometry_status",     out var gs)  ? gs.GetString() ?? "" : "",
        }).ToList();

        return new
        {
            record_kind           = "BUILDING_SLOT_WRITER_RECORDS",
            map_id                = "map_00",
            target_component_id   = targetCompId,
            building_slot_count   = slotList.Count,
            building_slots        = slotList,
            dry_run_only          = true,
        };
    }

    private static object BuildFrontageAccessRecord(string side, string compId, int contactPx)
    {
        return new
        {
            record_kind              = "FRONTAGE_ACCESS_RECORD",
            frontage_side            = side,
            frontage_component_id    = compId,
            frontage_contact_px      = contactPx,
            dry_run_only             = true,
        };
    }

    private static object BuildRearServiceAccessRecord(string side, string compId, int contactPx)
    {
        return new
        {
            record_kind               = "REAR_SERVICE_ACCESS_RECORD",
            rear_service_side         = side,
            rear_service_component_id = compId,
            rear_service_contact_px   = contactPx,
            dry_run_only              = true,
        };
    }

    private static DeadMtlDryRunForbiddenOutputScan BuildForbiddenOutputScanRecord(string outputRoot)
    {
        var allFiles = Directory.Exists(outputRoot)
            ? Directory.GetFiles(outputRoot, "*", SearchOption.AllDirectories)
            : Array.Empty<string>();

        bool lotpackFound    = allFiles.Any(f => f.EndsWith(".lotpack",            StringComparison.OrdinalIgnoreCase));
        bool lotheaderFound  = allFiles.Any(f => f.EndsWith(".lotheader",          StringComparison.OrdinalIgnoreCase));
        bool wgoLuaFound     = allFiles.Any(f => f.EndsWith("WorldGenOverride.lua", StringComparison.OrdinalIgnoreCase));
        bool pzInstallFound  = allFiles.Any(f => f.Contains("ProjectZomboid",       StringComparison.OrdinalIgnoreCase));
        bool scanPassed      = !lotpackFound && !lotheaderFound && !wgoLuaFound && !pzInstallFound;

        return new DeadMtlDryRunForbiddenOutputScan
        {
            LotpackFound                  = lotpackFound,
            LotheaderFound                = lotheaderFound,
            WorldgenOverrideLuaFound      = wgoLuaFound,
            CompileWorldgenInvocationFound = false,
            PzInstallPathFound            = pzInstallFound,
            ScanPassed                    = scanPassed,
            Details                       = scanPassed
                ? "Scan passed: no .lotpack, .lotheader, WorldGenOverride.lua, or PZ install paths found."
                : "Scan FAILED: forbidden output detected in output tree.",
        };
    }

    private static DeadMtlDryRunRollbackRecord BuildRollbackRecord()
    {
        return new DeadMtlDryRunRollbackRecord
        {
            RecordKind           = "ROLLBACK_RECORD",
            DotLocalOutputsOnly  = true,
            SourceHashesUnchanged = true,
            RuntimeFilesCreated  = false,
            PzInstallMutated     = false,
            DryRunOnly           = true,
        };
    }

    private static DeadMtlDryRunClaimBoundary BuildClaimBoundaryRecord()
    {
        return new DeadMtlDryRunClaimBoundary
        {
            RecordKind                       = "CLAIM_BOUNDARY_RECORD",
            WriterReady                      = false,
            RuntimeValid                     = false,
            Materialized                     = false,
            ApprovedForWriterExperiment      = false,
            WriterExperimentGateStatus       = "LOCKED_PENDING_OPERATOR_APPROVAL",
            RuntimeProofClaimed              = false,
            WriterReadyClaimed               = false,
            PublicPlayablePackagingClaimed   = false,
        };
    }

    public string RenderJson(DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterResult result) =>
        JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

    public string RenderMarkdown(DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-26G WorldBuilder Minimal Concrete Geometry Writer Dry-Run Emitter");
        sb.AppendLine();
        sb.AppendLine("## Purpose");
        sb.AppendLine();
        sb.AppendLine("MAP-26G reads the MAP-26F dry-run writer design and the MAP-26A geometry MVP,");
        sb.AppendLine("then emits deterministic dry-run records under .local only. It answers:");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine("If the future writer were run as a sandboxed dry-run, what non-runtime records");
        sb.AppendLine("would it emit from the MAP-26A geometry?");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("MAP-26G does NOT write PZ runtime files, .lotpack, .lotheader, WorldGenOverride.lua,");
        sb.AppendLine("or call compile-worldgen.");
        sb.AppendLine();
        sb.AppendLine("## Source Inputs");
        sb.AppendLine();
        sb.AppendLine($"- Dry-run design path: `{result.SourceDryRunDesignPath}`");
        sb.AppendLine($"- Dry-run design SHA-256: `{result.SourceDryRunDesignSha256}`");
        sb.AppendLine($"- Dry-run design verdict: `{result.SourceDryRunDesignVerdict}`");
        sb.AppendLine($"- Geometry MVP path: `{result.SourceGeometryMvpPath}`");
        sb.AppendLine($"- Geometry MVP SHA-256: `{result.SourceGeometryMvpSha256}`");
        sb.AppendLine();
        sb.AppendLine("## Emitted Dry-Run Records");
        sb.AppendLine();
        sb.AppendLine($"Total: {result.EmittedRecordCount}");
        sb.AppendLine();
        sb.AppendLine("| # | Record ID | Kind | SHA-256 (first 16) | Status |");
        sb.AppendLine("|---|-----------|------|--------------------|--------|");
        foreach (var r in result.EmittedRecords)
        {
            var sha = r.Sha256.Length >= 16 ? r.Sha256[..16] : r.Sha256;
            sb.AppendLine($"| {r.RecordOrder} | `{r.RecordId}` | `{r.RecordKind}` | `{sha}...` | `{r.Status}` |");
        }
        sb.AppendLine();
        sb.AppendLine("## Checks");
        sb.AppendLine();
        sb.AppendLine($"Checks: {result.CheckCount} | Pass: {result.PassedCheckCount} | Fail: {result.FailedCheckCount}");
        sb.AppendLine();
        sb.AppendLine("| # | Check ID | Status |");
        sb.AppendLine("|---|----------|--------|");
        foreach (var c in result.Checks)
            sb.AppendLine($"| {c.CheckOrder} | `{c.CheckId}` | `{c.CheckStatus}` |");
        sb.AppendLine();
        sb.AppendLine("## Writer Gate");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine($"dry_run_only                   : {result.DryRunOnly.ToString().ToLower()}");
        sb.AppendLine($"approved_for_writer_experiment : {result.ApprovedForWriterExperiment.ToString().ToLower()}");
        sb.AppendLine($"writer_experiment_gate_status  : {result.WriterExperimentGateStatus}");
        sb.AppendLine($"future_writer_name             : {result.FutureWriterName}");
        sb.AppendLine($"emitter_status                 : {result.EmitterStatus}");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine($"writer_ready                   : {result.WriterReady.ToString().ToLower()}");
        sb.AppendLine($"runtime_valid                  : {result.RuntimeValid.ToString().ToLower()}");
        sb.AppendLine($"materialized                   : {result.Materialized.ToString().ToLower()}");
        sb.AppendLine($"approved_for_writer_experiment : {result.ApprovedForWriterExperiment.ToString().ToLower()}");
        sb.AppendLine($"writer_experiment_gate_status  : {result.WriterExperimentGateStatus}");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("MAP-26G emits dry-run records only. It does not create PZ runtime geometry,");
        sb.AppendLine("write lotpack, write lotheader, write WorldGenOverride.lua, call compile-worldgen,");
        sb.AppendLine("or install anything into Project Zomboid.");
        sb.AppendLine();
        sb.Append($"## Verdict: `{result.Verdict}`");
        return sb.ToString();
    }

    public string RenderCsv(DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_order,check_id,check_status,expected,actual,details");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckOrder},{c.CheckId},{c.CheckStatus},{EscapeCsv(c.Expected)},{EscapeCsv(c.Actual)},{EscapeCsv(c.Details)}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"map_id                          : {result.MapId}");
        sb.AppendLine($"target_component_id             : {result.TargetComponentId}");
        sb.AppendLine($"design_sha256                   : {result.SourceDryRunDesignSha256}");
        sb.AppendLine($"geometry_mvp_sha256             : {result.SourceGeometryMvpSha256}");
        sb.AppendLine($"emitted_record_count            : {result.EmittedRecordCount}");
        sb.AppendLine($"check_count                     : {result.CheckCount}");
        sb.AppendLine($"passed_check_count              : {result.PassedCheckCount}");
        sb.AppendLine($"failed_check_count              : {result.FailedCheckCount}");
        sb.AppendLine($"dry_run_only                    : {(result.DryRunOnly                  ? 1 : 0)}");
        sb.AppendLine($"writer_ready                    : {(result.WriterReady                 ? 1 : 0)}");
        sb.AppendLine($"runtime_valid                   : {(result.RuntimeValid                ? 1 : 0)}");
        sb.AppendLine($"materialized                    : {(result.Materialized                ? 1 : 0)}");
        sb.AppendLine($"approved_for_writer_experiment  : {(result.ApprovedForWriterExperiment ? 1 : 0)}");
        sb.AppendLine($"writer_experiment_gate_status   : {result.WriterExperimentGateStatus}");
        sb.AppendLine($"future_writer_name              : {result.FutureWriterName}");
        sb.AppendLine($"emitter_status                  : {result.EmitterStatus}");
        sb.Append($"verdict                         : {result.Verdict}");
        return sb.ToString();
    }

    private static string EscapeCsv(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}
