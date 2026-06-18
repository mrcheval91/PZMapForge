using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmissionAuditReceiptBuilder
{
    private const string Format =
        "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-writer-dry-run-emission-audit-receipt.v1";
    private const string ExpectedMap26GVerdict =
        "MAP26G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER_COMPLETE";
    private const string ExpectedEmitterStatus     = "DRY_RUN_RECORDS_EMITTED_TO_DOT_LOCAL_ONLY";
    private const string ExpectedTargetComponentId = "map_00_component_0001";
    private const string ExpectedIntent            = "RESIDENTIAL_LOT_BLOCK";
    private const int    ExpectedBboxMinX          = 124;
    private const int    ExpectedBboxMinY          = 10;
    private const int    ExpectedBboxMaxX          = 212;
    private const int    ExpectedBboxMaxY           = 69;
    private const int    ExpectedBboxWidth          = 89;
    private const int    ExpectedBboxHeight         = 60;
    private const int    ExpectedLotCount           = 7;
    private const int    ExpectedBuildingSlotCount  = 7;
    private const string ExpectedFrontageSide       = "NORTH";
    private const string ExpectedFrontageCompId     = "map_00_component_0023";
    private const int    ExpectedFrontageContactPx  = 89;
    private const string ExpectedRearSide           = "EAST";
    private const string ExpectedRearCompId         = "map_00_component_0030";
    private const int    ExpectedRearContactPx      = 60;
    private const string ExpectedGateStatus         = "LOCKED_PENDING_OPERATOR_APPROVAL";

    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmissionAuditReceiptResult Build(
        string emitterResultPath, string emitterOutputRoot)
    {
        var errors = new List<string>();
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmissionAuditReceiptResult
        {
            Format                  = Format,
            GeneratedUtc            = DateTime.UtcNow.ToString("o"),
            SourceEmitterResultPath = emitterResultPath,
            SourceEmitterOutputRoot = emitterOutputRoot,
            WriterReady                    = false,
            RuntimeValid                   = false,
            Materialized                   = false,
            ApprovedForWriterExperiment    = false,
            WriterExperimentGateStatus     = ExpectedGateStatus,
            RuntimeProofClaimed            = false,
            WriterReadyClaimed             = false,
            PublicPlayablePackagingClaimed = false,
        };

        if (!File.Exists(emitterResultPath))
        {
            errors.Add($"MAP-26G emitter result not found: {emitterResultPath}");
            result.Errors  = errors;
            result.IsValid = false;
            result.Verdict = "MAP26H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMISSION_AUDIT_RECEIPT_INVALID";
            return result;
        }

        if (!Directory.Exists(emitterOutputRoot))
        {
            errors.Add($"MAP-26G emitter output root not found: {emitterOutputRoot}");
            result.Errors  = errors;
            result.IsValid = false;
            result.Verdict = "MAP26H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMISSION_AUDIT_RECEIPT_INVALID";
            return result;
        }

        result.SourceEmitterResultSha256 = HashFile(emitterResultPath);

        string emitterVerdict    = string.Empty;
        bool   emitterIsValid    = false;
        string emitterStatus     = string.Empty;
        bool   emitterDryRunOnly = false;
        int    emitterRecordCount = 0;
        string mapId              = "map_00";
        string targetComponentId  = string.Empty;

        var emittedDescriptors = new List<(string RecordId, string Path, string Sha256)>();

        try
        {
            using var doc  = JsonDocument.Parse(File.ReadAllText(emitterResultPath));
            var root = doc.RootElement;
            emitterVerdict     = root.TryGetProperty("verdict",              out var vd)  ? vd.GetString()  ?? "" : "";
            emitterIsValid     = root.TryGetProperty("is_valid",             out var iv)  && iv.GetBoolean();
            emitterStatus      = root.TryGetProperty("emitter_status",       out var es)  ? es.GetString()  ?? "" : "";
            emitterDryRunOnly  = root.TryGetProperty("dry_run_only",         out var dr)  && dr.GetBoolean();
            emitterRecordCount = root.TryGetProperty("emitted_record_count", out var erc) ? erc.GetInt32()  : 0;
            mapId              = root.TryGetProperty("map_id",               out var mi)  ? mi.GetString()  ?? "map_00" : "map_00";
            targetComponentId  = root.TryGetProperty("target_component_id",  out var tc)  ? tc.GetString()  ?? "" : "";

            if (root.TryGetProperty("emitted_records", out var records) && records.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in records.EnumerateArray())
                {
                    var rid  = r.TryGetProperty("record_id", out var ri) ? ri.GetString() ?? "" : "";
                    var path = r.TryGetProperty("path",      out var rp) ? rp.GetString() ?? "" : "";
                    var sha  = r.TryGetProperty("sha256",    out var rs) ? rs.GetString() ?? "" : "";
                    emittedDescriptors.Add((rid, path, sha));
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse emitter result: {ex.Message}");
            result.Errors  = errors;
            result.IsValid = false;
            result.Verdict = "MAP26H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMISSION_AUDIT_RECEIPT_INVALID";
            return result;
        }

        result.MapId             = mapId;
        result.TargetComponentId = targetComponentId;
        result.SourceEmitterResultVerdict  = emitterVerdict;
        result.SourceEmitterResultIsValid  = emitterIsValid;
        result.ExpectedEmittedRecordCount  = 8;
        result.ActualEmittedRecordCount    = emitterRecordCount;

        // Build 4 main MAP-26G output file descriptors
        string emitterBase = "map_00.minimal_concrete_geometry_writer_dry_run_emitter";
        var auditedFiles = new List<DeadMtlAuditReceiptAuditedFile>
        {
            MakeMainFileDescriptor(1, "EMITTER_RESULT_JSON",    "EMITTER_RESULT", emitterResultPath),
            MakeMainFileDescriptor(2, "EMITTER_RESULT_MD",      "EMITTER_RESULT", Path.Combine(emitterOutputRoot, $"{emitterBase}.md")),
            MakeMainFileDescriptor(3, "EMITTER_RESULT_CSV",     "EMITTER_RESULT", Path.Combine(emitterOutputRoot, $"{emitterBase}.csv")),
            MakeMainFileDescriptor(4, "EMITTER_RESULT_SUMMARY", "EMITTER_RESULT", Path.Combine(emitterOutputRoot, $"{emitterBase}.summary.txt")),
        };

        // Build 8 emitted record descriptors
        for (int i = 0; i < emittedDescriptors.Count; i++)
        {
            var (rid, path, expectedSha) = emittedDescriptors[i];
            auditedFiles.Add(MakeEmittedRecordDescriptor(5 + i, rid, path, expectedSha));
        }

        result.AuditedFiles     = auditedFiles;
        result.AuditedFileCount = auditedFiles.Count;
        result.HashMatchCount   = auditedFiles.Count(f => !string.IsNullOrEmpty(f.ExpectedSha256FromEmitterDescriptor) && f.HashMatchesEmitterDescriptor);
        result.HashMismatchCount = auditedFiles.Count(f => !string.IsNullOrEmpty(f.ExpectedSha256FromEmitterDescriptor) && !f.HashMatchesEmitterDescriptor);

        // Parse emitted record files for geometry verification
        string GetEmittedPath(string rid) =>
            emittedDescriptors.FirstOrDefault(d => d.RecordId == rid).Path ?? "";

        var geometryAudit     = BuildGeometryRecordAudit(
            GetEmittedPath("COMPONENT_WRITER_RECORD"),
            GetEmittedPath("LOT_WRITER_RECORDS"),
            GetEmittedPath("BUILDING_SLOT_WRITER_RECORDS"),
            GetEmittedPath("FRONTAGE_ACCESS_RECORD"),
            GetEmittedPath("REAR_SERVICE_ACCESS_RECORD"));

        var claimBoundaryAudit = BuildClaimBoundaryAudit(GetEmittedPath("CLAIM_BOUNDARY_RECORD"));
        var forbiddenScan      = BuildPostEmissionForbiddenScan(emitterOutputRoot);

        result.GeometryRecordAudit  = geometryAudit;
        result.ClaimBoundaryAudit   = claimBoundaryAudit;
        result.ForbiddenArtifactScan = forbiddenScan;

        // Copy claim boundary flags to result
        result.WriterReady                    = claimBoundaryAudit.WriterReady;
        result.RuntimeValid                   = claimBoundaryAudit.RuntimeValid;
        result.Materialized                   = claimBoundaryAudit.Materialized;
        result.ApprovedForWriterExperiment    = claimBoundaryAudit.ApprovedForWriterExperiment;
        result.WriterExperimentGateStatus     = claimBoundaryAudit.WriterExperimentGateStatus;
        result.RuntimeProofClaimed            = claimBoundaryAudit.RuntimeProofClaimed;
        result.WriterReadyClaimed             = claimBoundaryAudit.WriterReadyClaimed;
        result.PublicPlayablePackagingClaimed = claimBoundaryAudit.PublicPlayablePackagingClaimed;

        // ---- 29 checks ----
        var checks = new List<DeadMtlAuditReceiptCheck>();
        int ord = 1;

        // 1-2: always PASS (past file existence guard)
        AddCheck(checks, ord++, "MAP26G_EMITTER_RESULT_EXISTS",
            "MAP-26G emitter result file exists on disk",
            "true", "true",
            "Emitter result file exists and was read.");

        AddCheck(checks, ord++, "MAP26G_EMITTER_RESULT_HASHED",
            "MAP-26G emitter result SHA-256 computed",
            "true", "true",
            $"SHA-256: {result.SourceEmitterResultSha256}.");

        // 3-7: compare parsed values
        AddCheck(checks, ord++, "MAP26G_VERDICT_COMPLETE",
            "MAP-26G verdict is complete",
            ExpectedMap26GVerdict, emitterVerdict,
            $"MAP-26G verdict: {emitterVerdict}.");

        AddCheck(checks, ord++, "MAP26G_IS_VALID_TRUE",
            "MAP-26G is_valid is true",
            "true", emitterIsValid.ToString().ToLower(),
            $"MAP-26G is_valid: {emitterIsValid}.");

        AddCheck(checks, ord++, "MAP26G_EMITTER_STATUS_DOT_LOCAL_ONLY",
            "MAP-26G emitter_status is DRY_RUN_RECORDS_EMITTED_TO_DOT_LOCAL_ONLY",
            ExpectedEmitterStatus, emitterStatus,
            $"emitter_status: {emitterStatus}.");

        AddCheck(checks, ord++, "MAP26G_DRY_RUN_ONLY_TRUE",
            "MAP-26G dry_run_only is true",
            "true", emitterDryRunOnly.ToString().ToLower(),
            $"dry_run_only: {emitterDryRunOnly}.");

        AddCheck(checks, ord++, "MAP26G_EMITTED_RECORD_COUNT_8",
            "MAP-26G emitted_record_count is 8",
            "8", emitterRecordCount.ToString(),
            $"emitted_record_count: {emitterRecordCount}.");

        // 8-9: file existence and hashing
        int existCount   = auditedFiles.Count(f => f.Exists);
        int hashedCount  = auditedFiles.Count(f => f.Sha256.Length == 64);
        AddCheck(checks, ord++, "ALL_12_EXPECTED_OUTPUT_FILES_EXIST",
            "All 12 expected MAP-26G output files exist on disk",
            "12", existCount.ToString(),
            $"{existCount} of 12 expected files found.");

        AddCheck(checks, ord++, "ALL_12_EXPECTED_OUTPUT_FILES_HASHED",
            "All 12 expected output files have SHA-256 computed",
            "true", (hashedCount == 12).ToString().ToLower(),
            $"{hashedCount} of 12 files hashed.");

        // 10-11: hash verification
        int matchCount    = auditedFiles.Count(f => f.HashMatchesEmitterDescriptor);
        int mismatchCount = result.HashMismatchCount;
        AddCheck(checks, ord++, "ALL_8_EMITTED_RECORD_HASHES_MATCH",
            "All 8 emitted record file hashes match their emitter descriptors",
            "true", (mismatchCount == 0).ToString().ToLower(),
            mismatchCount == 0 ? "All 8 emitted record hashes verified." : $"{mismatchCount} hash mismatch(es) detected.");

        AddCheck(checks, ord++, "NO_HASH_MISMATCHES",
            "Hash mismatch count is 0",
            "0", mismatchCount.ToString(),
            mismatchCount == 0 ? "No hash mismatches." : $"{mismatchCount} mismatch(es).");

        // 12-18: geometry record checks
        AddCheck(checks, ord++, "COMPONENT_RECORD_TARGET_COMPONENT_STABLE",
            "Component record target_component_id matches expected",
            ExpectedTargetComponentId, geometryAudit.TargetComponentId,
            $"target_component_id: {geometryAudit.TargetComponentId}.");

        AddCheck(checks, ord++, "COMPONENT_RECORD_INTENT_STABLE",
            "Component record intent matches expected",
            ExpectedIntent, geometryAudit.Intent,
            $"intent: {geometryAudit.Intent}.");

        AddCheck(checks, ord++, "COMPONENT_RECORD_BBOX_STABLE",
            "Component record bbox matches expected (124,10,212,69,89,60)",
            $"{ExpectedBboxMinX},{ExpectedBboxMinY},{ExpectedBboxMaxX},{ExpectedBboxMaxY},{ExpectedBboxWidth},{ExpectedBboxHeight}",
            $"{geometryAudit.BboxMinX},{geometryAudit.BboxMinY},{geometryAudit.BboxMaxX},{geometryAudit.BboxMaxY},{geometryAudit.BboxWidthPx},{geometryAudit.BboxHeightPx}",
            $"bbox: ({geometryAudit.BboxMinX},{geometryAudit.BboxMinY})->({geometryAudit.BboxMaxX},{geometryAudit.BboxMaxY}) {geometryAudit.BboxWidthPx}x{geometryAudit.BboxHeightPx}px.");

        AddCheck(checks, ord++, "LOT_RECORDS_COUNT_7",
            "Lot records lot_count is 7",
            "7", geometryAudit.LotCount.ToString(),
            $"lot_count: {geometryAudit.LotCount}.");

        AddCheck(checks, ord++, "BUILDING_SLOT_RECORDS_COUNT_7",
            "Building slot records building_slot_count is 7",
            "7", geometryAudit.BuildingSlotCount.ToString(),
            $"building_slot_count: {geometryAudit.BuildingSlotCount}.");

        AddCheck(checks, ord++, "FRONTAGE_RECORD_STABLE",
            "Frontage access record matches expected (NORTH, map_00_component_0023, 89px)",
            $"{ExpectedFrontageSide},{ExpectedFrontageCompId},{ExpectedFrontageContactPx}",
            $"{geometryAudit.FrontageSide},{geometryAudit.FrontageComponentId},{geometryAudit.FrontageContactPx}",
            $"frontage: {geometryAudit.FrontageSide} via {geometryAudit.FrontageComponentId} {geometryAudit.FrontageContactPx}px.");

        AddCheck(checks, ord++, "REAR_SERVICE_RECORD_STABLE",
            "Rear service access record matches expected (EAST, map_00_component_0030, 60px)",
            $"{ExpectedRearSide},{ExpectedRearCompId},{ExpectedRearContactPx}",
            $"{geometryAudit.RearServiceSide},{geometryAudit.RearServiceComponentId},{geometryAudit.RearServiceContactPx}",
            $"rear service: {geometryAudit.RearServiceSide} via {geometryAudit.RearServiceComponentId} {geometryAudit.RearServiceContactPx}px.");

        // 19-22: scan / rollback / claim boundary from emitted records
        bool map26gScanPassed = false;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(emitterResultPath));
            var root = doc.RootElement;
            if (root.TryGetProperty("forbidden_output_scan", out var fosEl))
                map26gScanPassed = fosEl.TryGetProperty("scan_passed", out var sp) && sp.GetBoolean();
        }
        catch { }

        AddCheck(checks, ord++, "MAP26G_FORBIDDEN_SCAN_PASS",
            "MAP-26G internal forbidden output scan passed",
            "true", map26gScanPassed.ToString().ToLower(),
            map26gScanPassed ? "MAP-26G scan_passed: true." : "MAP-26G scan_passed: false.");

        AddCheck(checks, ord++, "POST_EMISSION_FORBIDDEN_SCAN_PASS",
            "Independent post-emission forbidden artifact scan of output root passed",
            "true", forbiddenScan.ScanPassed.ToString().ToLower(),
            forbiddenScan.Details);

        // Rollback from emitter result JSON embedded rollback_record
        bool dotLocalOnly = false;
        bool srcUnchanged  = false;
        bool runtimeCreated = false;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(emitterResultPath));
            var root = doc.RootElement;
            if (root.TryGetProperty("rollback_record", out var rr))
            {
                dotLocalOnly    = rr.TryGetProperty("dot_local_outputs_only",    out var dl)  && dl.GetBoolean();
                srcUnchanged    = rr.TryGetProperty("source_hashes_unchanged",   out var su)  && su.GetBoolean();
                runtimeCreated  = rr.TryGetProperty("runtime_files_created",     out var rfc) && rfc.GetBoolean();
            }
        }
        catch { }

        bool rollbackOk = dotLocalOnly && srcUnchanged && !runtimeCreated;
        AddCheck(checks, ord++, "ROLLBACK_RECORD_PASS",
            "Rollback record: dot_local_outputs_only=true, source_hashes_unchanged=true, runtime_files_created=false",
            "true", rollbackOk.ToString().ToLower(),
            $"dot_local_outputs_only: {dotLocalOnly}, source_hashes_unchanged: {srcUnchanged}, runtime_files_created: {runtimeCreated}.");

        bool claimBoundaryOk = !claimBoundaryAudit.WriterReady && !claimBoundaryAudit.RuntimeValid && !claimBoundaryAudit.Materialized;
        AddCheck(checks, ord++, "CLAIM_BOUNDARY_RECORD_PASS",
            "Claim boundary record: writer_ready=false, runtime_valid=false, materialized=false",
            "true", claimBoundaryOk.ToString().ToLower(),
            $"writer_ready: {claimBoundaryAudit.WriterReady}, runtime_valid: {claimBoundaryAudit.RuntimeValid}, materialized: {claimBoundaryAudit.Materialized}.");

        // 23-28: claim boundary flag verification
        AddCheck(checks, ord++, "WRITER_READY_FALSE",
            "writer_ready is false",
            "false", claimBoundaryAudit.WriterReady.ToString().ToLower(),
            $"writer_ready: {claimBoundaryAudit.WriterReady}.");

        AddCheck(checks, ord++, "RUNTIME_VALID_FALSE",
            "runtime_valid is false",
            "false", claimBoundaryAudit.RuntimeValid.ToString().ToLower(),
            $"runtime_valid: {claimBoundaryAudit.RuntimeValid}.");

        AddCheck(checks, ord++, "MATERIALIZED_FALSE",
            "materialized is false",
            "false", claimBoundaryAudit.Materialized.ToString().ToLower(),
            $"materialized: {claimBoundaryAudit.Materialized}.");

        AddCheck(checks, ord++, "APPROVED_FOR_WRITER_EXPERIMENT_FALSE",
            "approved_for_writer_experiment is false",
            "false", claimBoundaryAudit.ApprovedForWriterExperiment.ToString().ToLower(),
            $"approved_for_writer_experiment: {claimBoundaryAudit.ApprovedForWriterExperiment}.");

        AddCheck(checks, ord++, "GATE_STATUS_LOCKED",
            "writer_experiment_gate_status is LOCKED_PENDING_OPERATOR_APPROVAL",
            ExpectedGateStatus, claimBoundaryAudit.WriterExperimentGateStatus,
            $"writer_experiment_gate_status: {claimBoundaryAudit.WriterExperimentGateStatus}.");

        AddCheck(checks, ord++, "NO_RUNTIME_PROOF_CLAIMED",
            "runtime_proof_claimed is false",
            "false", claimBoundaryAudit.RuntimeProofClaimed.ToString().ToLower(),
            $"runtime_proof_claimed: {claimBoundaryAudit.RuntimeProofClaimed}.");

        // 29: always PASS by construction
        AddCheck(checks, ord++, "NO_FORBIDDEN_OUTPUTS_CREATED",
            "No forbidden outputs created by this audit receipt (no .lotpack, no .lotheader, no WorldGenOverride.lua)",
            "true", "true",
            "Audit receipt emits .local JSON/MD/CSV/summary only. No forbidden outputs created.");

        result.Checks             = checks;
        result.AuditCheckCount    = checks.Count;
        result.PassedAuditCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedAuditCheckCount = checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass   = result.FailedAuditCheckCount == 0;
        result.IsValid = allPass;
        result.Errors  = errors;
        result.Verdict = allPass
            ? "MAP26H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMISSION_AUDIT_RECEIPT_COMPLETE"
            : "MAP26H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMISSION_AUDIT_RECEIPT_INVALID";

        result.GeometryRecordAudit.GeometryAuditPassed  = geometryAudit.GeometryAuditPassed;
        result.ClaimBoundaryAudit.ClaimBoundaryAuditPassed = claimBoundaryAudit.ClaimBoundaryAuditPassed;

        return result;
    }

    private static void AddCheck(
        List<DeadMtlAuditReceiptCheck> checks,
        int order, string id, string label, string expected, string actual, string details)
    {
        bool pass = string.Equals(expected, actual, StringComparison.Ordinal);
        checks.Add(new DeadMtlAuditReceiptCheck
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

    private static string HashFile(string path)
    {
        if (!File.Exists(path)) return string.Empty;
        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLower();
    }

    private static DeadMtlAuditReceiptAuditedFile MakeMainFileDescriptor(
        int order, string fileId, string fileKind, string path)
    {
        bool exists   = File.Exists(path);
        string sha256 = exists ? HashFile(path) : string.Empty;
        long sizeBytes = exists ? new FileInfo(path).Length : 0;
        return new DeadMtlAuditReceiptAuditedFile
        {
            FileOrder                        = order,
            FileId                           = fileId,
            FileKind                         = fileKind,
            Path                             = path,
            Exists                           = exists,
            Sha256                           = sha256,
            SizeBytes                        = sizeBytes,
            ExpectedSha256FromEmitterDescriptor = string.Empty,
            HashMatchesEmitterDescriptor     = true,
            Required                         = true,
        };
    }

    private static DeadMtlAuditReceiptAuditedFile MakeEmittedRecordDescriptor(
        int order, string fileId, string path, string expectedSha256)
    {
        bool exists    = File.Exists(path);
        string actualSha = exists ? HashFile(path) : string.Empty;
        long sizeBytes  = exists ? new FileInfo(path).Length : 0;
        bool matches    = !string.IsNullOrEmpty(expectedSha256) &&
                          string.Equals(expectedSha256, actualSha, StringComparison.OrdinalIgnoreCase);
        return new DeadMtlAuditReceiptAuditedFile
        {
            FileOrder                        = order,
            FileId                           = fileId,
            FileKind                         = "EMITTED_RECORD",
            Path                             = path,
            Exists                           = exists,
            Sha256                           = actualSha,
            SizeBytes                        = sizeBytes,
            ExpectedSha256FromEmitterDescriptor = expectedSha256,
            HashMatchesEmitterDescriptor     = matches,
            Required                         = true,
        };
    }

    private static DeadMtlAuditReceiptGeometryRecordAudit BuildGeometryRecordAudit(
        string componentPath, string lotPath, string slotPath,
        string frontagePath, string rearPath)
    {
        var audit = new DeadMtlAuditReceiptGeometryRecordAudit();

        if (File.Exists(componentPath))
        {
            try
            {
                using var doc  = JsonDocument.Parse(File.ReadAllText(componentPath));
                var root = doc.RootElement;
                audit.TargetComponentId   = root.TryGetProperty("target_component_id",    out var tc)  ? tc.GetString()  ?? "" : "";
                audit.Intent              = root.TryGetProperty("intent",                 out var it)  ? it.GetString()  ?? "" : "";
                audit.AccessReadinessClass = root.TryGetProperty("access_readiness_class", out var ar) ? ar.GetString()  ?? "" : "";
                if (root.TryGetProperty("component_bbox", out var bbox))
                {
                    audit.BboxMinX   = bbox.TryGetProperty("min_x",     out var mnx) ? mnx.GetInt32() : 0;
                    audit.BboxMinY   = bbox.TryGetProperty("min_y",     out var mny) ? mny.GetInt32() : 0;
                    audit.BboxMaxX   = bbox.TryGetProperty("max_x",     out var mxx) ? mxx.GetInt32() : 0;
                    audit.BboxMaxY   = bbox.TryGetProperty("max_y",     out var mxy) ? mxy.GetInt32() : 0;
                    audit.BboxWidthPx  = bbox.TryGetProperty("width_px",  out var wp) ? wp.GetInt32()  : 0;
                    audit.BboxHeightPx = bbox.TryGetProperty("height_px", out var hp) ? hp.GetInt32()  : 0;
                }
            }
            catch { }
        }

        if (File.Exists(lotPath))
        {
            try
            {
                using var doc  = JsonDocument.Parse(File.ReadAllText(lotPath));
                audit.LotCount = doc.RootElement.TryGetProperty("lot_count", out var lc) ? lc.GetInt32() : 0;
            }
            catch { }
        }

        if (File.Exists(slotPath))
        {
            try
            {
                using var doc  = JsonDocument.Parse(File.ReadAllText(slotPath));
                audit.BuildingSlotCount = doc.RootElement.TryGetProperty("building_slot_count", out var sc) ? sc.GetInt32() : 0;
            }
            catch { }
        }

        if (File.Exists(frontagePath))
        {
            try
            {
                using var doc  = JsonDocument.Parse(File.ReadAllText(frontagePath));
                var root = doc.RootElement;
                audit.FrontageSide        = root.TryGetProperty("frontage_side",         out var fs)  ? fs.GetString()  ?? "" : "";
                audit.FrontageComponentId = root.TryGetProperty("frontage_component_id", out var fci) ? fci.GetString() ?? "" : "";
                audit.FrontageContactPx   = root.TryGetProperty("frontage_contact_px",   out var fcp) ? fcp.GetInt32()  : 0;
            }
            catch { }
        }

        if (File.Exists(rearPath))
        {
            try
            {
                using var doc  = JsonDocument.Parse(File.ReadAllText(rearPath));
                var root = doc.RootElement;
                audit.RearServiceSide        = root.TryGetProperty("rear_service_side",         out var rs)  ? rs.GetString()  ?? "" : "";
                audit.RearServiceComponentId = root.TryGetProperty("rear_service_component_id", out var rci) ? rci.GetString() ?? "" : "";
                audit.RearServiceContactPx   = root.TryGetProperty("rear_service_contact_px",   out var rcp) ? rcp.GetInt32()  : 0;
            }
            catch { }
        }

        audit.GeometryAuditPassed =
            audit.TargetComponentId   == "map_00_component_0001" &&
            audit.Intent              == "RESIDENTIAL_LOT_BLOCK" &&
            audit.BboxMinX == 124 && audit.BboxMinY == 10 && audit.BboxMaxX == 212 && audit.BboxMaxY == 69 &&
            audit.BboxWidthPx == 89 && audit.BboxHeightPx == 60 &&
            audit.LotCount == 7 && audit.BuildingSlotCount == 7 &&
            audit.FrontageSide == "NORTH" && audit.FrontageComponentId == "map_00_component_0023" && audit.FrontageContactPx == 89 &&
            audit.RearServiceSide == "EAST" && audit.RearServiceComponentId == "map_00_component_0030" && audit.RearServiceContactPx == 60;

        return audit;
    }

    private static DeadMtlAuditReceiptClaimBoundaryAudit BuildClaimBoundaryAudit(string claimBoundaryPath)
    {
        var audit = new DeadMtlAuditReceiptClaimBoundaryAudit
        {
            WriterExperimentGateStatus = ExpectedGateStatus,
        };

        if (File.Exists(claimBoundaryPath))
        {
            try
            {
                using var doc  = JsonDocument.Parse(File.ReadAllText(claimBoundaryPath));
                var root = doc.RootElement;
                audit.WriterReady                    = root.TryGetProperty("writer_ready",                    out var wr)  && wr.GetBoolean();
                audit.RuntimeValid                   = root.TryGetProperty("runtime_valid",                   out var rv)  && rv.GetBoolean();
                audit.Materialized                   = root.TryGetProperty("materialized",                    out var ma)  && ma.GetBoolean();
                audit.ApprovedForWriterExperiment    = root.TryGetProperty("approved_for_writer_experiment",  out var awe) && awe.GetBoolean();
                audit.WriterExperimentGateStatus     = root.TryGetProperty("writer_experiment_gate_status",   out var wgs) ? wgs.GetString() ?? ExpectedGateStatus : ExpectedGateStatus;
                audit.RuntimeProofClaimed            = root.TryGetProperty("runtime_proof_claimed",           out var rpc) && rpc.GetBoolean();
                audit.WriterReadyClaimed             = root.TryGetProperty("writer_ready_claimed",            out var wrc) && wrc.GetBoolean();
                audit.PublicPlayablePackagingClaimed = root.TryGetProperty("public_playable_packaging_claimed", out var ppc) && ppc.GetBoolean();
            }
            catch { }
        }

        audit.ClaimBoundaryAuditPassed =
            !audit.WriterReady && !audit.RuntimeValid && !audit.Materialized &&
            !audit.ApprovedForWriterExperiment &&
            audit.WriterExperimentGateStatus == ExpectedGateStatus &&
            !audit.RuntimeProofClaimed && !audit.WriterReadyClaimed && !audit.PublicPlayablePackagingClaimed;

        return audit;
    }

    private static DeadMtlAuditReceiptForbiddenScan BuildPostEmissionForbiddenScan(string outputRoot)
    {
        var allFiles = Directory.Exists(outputRoot)
            ? Directory.GetFiles(outputRoot, "*", SearchOption.AllDirectories)
            : Array.Empty<string>();

        bool lotpackFound  = allFiles.Any(f => f.EndsWith(".lotpack",             StringComparison.OrdinalIgnoreCase));
        bool lotheaderFound = allFiles.Any(f => f.EndsWith(".lotheader",          StringComparison.OrdinalIgnoreCase));
        bool luaFound       = allFiles.Any(f => f.EndsWith(".lua",                StringComparison.OrdinalIgnoreCase));
        bool wgoLuaFound    = allFiles.Any(f => f.EndsWith("WorldGenOverride.lua", StringComparison.OrdinalIgnoreCase));
        bool pzInstallFound = allFiles.Any(f =>
            f.Contains("ProjectZomboid", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("steamapps",      StringComparison.OrdinalIgnoreCase) ||
            f.Contains("workshop",       StringComparison.OrdinalIgnoreCase));

        bool scanPassed = !lotpackFound && !lotheaderFound && !luaFound && !wgoLuaFound && !pzInstallFound;

        return new DeadMtlAuditReceiptForbiddenScan
        {
            LotpackFound             = lotpackFound,
            LotheaderFound           = lotheaderFound,
            LuaFileFound             = luaFound,
            WorldgenOverrideLuaFound = wgoLuaFound,
            PzInstallPathFound       = pzInstallFound,
            ScanPassed               = scanPassed,
            Details                  = scanPassed
                ? "Scan passed: no .lotpack, .lotheader, .lua, WorldGenOverride.lua, or PZ install paths found."
                : "Scan FAILED: forbidden artifact detected in output tree.",
        };
    }

    public string RenderJson(DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmissionAuditReceiptResult result) =>
        JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

    public string RenderMarkdown(DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmissionAuditReceiptResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-26H WorldBuilder Minimal Concrete Geometry Writer Dry-Run Emission Audit Receipt");
        sb.AppendLine();
        sb.AppendLine("## Purpose");
        sb.AppendLine();
        sb.AppendLine("MAP-26H independently verifies MAP-26G's 8 emitted dry-run records after emission.");
        sb.AppendLine("It answers: Did MAP-26G emit exactly the expected records under .local, with stable");
        sb.AppendLine("hashes, correct claim boundaries, and no forbidden runtime artifacts?");
        sb.AppendLine();
        sb.AppendLine("This is NOT a real PZ writer and NOT runtime proof.");
        sb.AppendLine();
        sb.AppendLine("## Source Inputs");
        sb.AppendLine();
        sb.AppendLine($"- Emitter result: `{result.SourceEmitterResultPath}`");
        sb.AppendLine($"- Emitter result SHA-256: `{result.SourceEmitterResultSha256}`");
        sb.AppendLine($"- Emitter result verdict: `{result.SourceEmitterResultVerdict}`");
        sb.AppendLine($"- Emitter output root: `{result.SourceEmitterOutputRoot}`");
        sb.AppendLine();
        sb.AppendLine("## Audited Files");
        sb.AppendLine();
        sb.AppendLine($"Total: {result.AuditedFileCount} | Hash matches: {result.HashMatchCount} | Mismatches: {result.HashMismatchCount}");
        sb.AppendLine();
        sb.AppendLine("| # | File ID | Exists | SHA-256 (first 16) | Hash Match |");
        sb.AppendLine("|---|---------|--------|--------------------|------------|");
        foreach (var f in result.AuditedFiles)
        {
            var sha = f.Sha256.Length >= 16 ? f.Sha256[..16] : f.Sha256;
            sb.AppendLine($"| {f.FileOrder} | `{f.FileId}` | {(f.Exists ? "yes" : "no")} | `{sha}...` | {(f.HashMatchesEmitterDescriptor ? "yes" : "NO")} |");
        }
        sb.AppendLine();
        sb.AppendLine("## Geometry Record Audit");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine($"target_component_id    : {result.GeometryRecordAudit.TargetComponentId}");
        sb.AppendLine($"intent                 : {result.GeometryRecordAudit.Intent}");
        sb.AppendLine($"access_readiness_class : {result.GeometryRecordAudit.AccessReadinessClass}");
        sb.AppendLine($"bbox                   : ({result.GeometryRecordAudit.BboxMinX},{result.GeometryRecordAudit.BboxMinY})->({result.GeometryRecordAudit.BboxMaxX},{result.GeometryRecordAudit.BboxMaxY}) {result.GeometryRecordAudit.BboxWidthPx}x{result.GeometryRecordAudit.BboxHeightPx}px");
        sb.AppendLine($"lot_count              : {result.GeometryRecordAudit.LotCount}");
        sb.AppendLine($"building_slot_count    : {result.GeometryRecordAudit.BuildingSlotCount}");
        sb.AppendLine($"frontage               : {result.GeometryRecordAudit.FrontageSide} via {result.GeometryRecordAudit.FrontageComponentId} {result.GeometryRecordAudit.FrontageContactPx}px");
        sb.AppendLine($"rear_service           : {result.GeometryRecordAudit.RearServiceSide} via {result.GeometryRecordAudit.RearServiceComponentId} {result.GeometryRecordAudit.RearServiceContactPx}px");
        sb.AppendLine($"geometry_audit_passed  : {result.GeometryRecordAudit.GeometryAuditPassed.ToString().ToLower()}");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine($"writer_ready                     : {result.ClaimBoundaryAudit.WriterReady.ToString().ToLower()}");
        sb.AppendLine($"runtime_valid                    : {result.ClaimBoundaryAudit.RuntimeValid.ToString().ToLower()}");
        sb.AppendLine($"materialized                     : {result.ClaimBoundaryAudit.Materialized.ToString().ToLower()}");
        sb.AppendLine($"approved_for_writer_experiment   : {result.ClaimBoundaryAudit.ApprovedForWriterExperiment.ToString().ToLower()}");
        sb.AppendLine($"writer_experiment_gate_status    : {result.ClaimBoundaryAudit.WriterExperimentGateStatus}");
        sb.AppendLine($"runtime_proof_claimed            : {result.ClaimBoundaryAudit.RuntimeProofClaimed.ToString().ToLower()}");
        sb.AppendLine($"writer_ready_claimed             : {result.ClaimBoundaryAudit.WriterReadyClaimed.ToString().ToLower()}");
        sb.AppendLine($"public_playable_packaging_claimed: {result.ClaimBoundaryAudit.PublicPlayablePackagingClaimed.ToString().ToLower()}");
        sb.AppendLine($"claim_boundary_audit_passed      : {result.ClaimBoundaryAudit.ClaimBoundaryAuditPassed.ToString().ToLower()}");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Checks");
        sb.AppendLine();
        sb.AppendLine($"Checks: {result.AuditCheckCount} | Pass: {result.PassedAuditCheckCount} | Fail: {result.FailedAuditCheckCount}");
        sb.AppendLine();
        sb.AppendLine("| # | Check ID | Status |");
        sb.AppendLine("|---|----------|--------|");
        foreach (var c in result.Checks)
            sb.AppendLine($"| {c.CheckOrder} | `{c.CheckId}` | `{c.CheckStatus}` |");
        sb.AppendLine();
        sb.AppendLine("MAP-26H does not write .lotpack, .lotheader, WorldGenOverride.lua, or call compile-worldgen.");
        sb.AppendLine("It does not install anything into Project Zomboid.");
        sb.AppendLine();
        sb.Append($"## Verdict: `{result.Verdict}`");
        return sb.ToString();
    }

    public string RenderCsv(DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmissionAuditReceiptResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_order,check_id,check_status,expected,actual,details");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckOrder},{c.CheckId},{c.CheckStatus},{EscapeCsv(c.Expected)},{EscapeCsv(c.Actual)},{EscapeCsv(c.Details)}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmissionAuditReceiptResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"map_id                          : {result.MapId}");
        sb.AppendLine($"target_component_id             : {result.TargetComponentId}");
        sb.AppendLine($"emitter_result_sha256           : {result.SourceEmitterResultSha256}");
        sb.AppendLine($"audited_file_count              : {result.AuditedFileCount}");
        sb.AppendLine($"hash_match_count                : {result.HashMatchCount}");
        sb.AppendLine($"hash_mismatch_count             : {result.HashMismatchCount}");
        sb.AppendLine($"audit_check_count               : {result.AuditCheckCount}");
        sb.AppendLine($"passed_audit_check_count        : {result.PassedAuditCheckCount}");
        sb.AppendLine($"failed_audit_check_count        : {result.FailedAuditCheckCount}");
        sb.AppendLine($"writer_ready                    : {(result.WriterReady                 ? 1 : 0)}");
        sb.AppendLine($"runtime_valid                   : {(result.RuntimeValid                ? 1 : 0)}");
        sb.AppendLine($"materialized                    : {(result.Materialized                ? 1 : 0)}");
        sb.AppendLine($"approved_for_writer_experiment  : {(result.ApprovedForWriterExperiment ? 1 : 0)}");
        sb.AppendLine($"writer_experiment_gate_status   : {result.WriterExperimentGateStatus}");
        sb.AppendLine($"runtime_proof_claimed           : {(result.RuntimeProofClaimed            ? 1 : 0)}");
        sb.AppendLine($"writer_ready_claimed            : {(result.WriterReadyClaimed             ? 1 : 0)}");
        sb.AppendLine($"public_playable_packaging_claimed: {(result.PublicPlayablePackagingClaimed ? 1 : 0)}");
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
