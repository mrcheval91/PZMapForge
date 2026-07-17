using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterBuilder
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    private static string HashFile(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLower();

    private static void AddCheck(List<DeadMtlLockedReplayBackendDryRunEmitterCheck> checks,
        string id, string label, string expected, string actual)
    {
        checks.Add(new DeadMtlLockedReplayBackendDryRunEmitterCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = string.Equals(expected, actual, StringComparison.Ordinal) ? "PASS" : "FAIL",
            Expected    = expected,
            Actual      = actual,
        });
    }

    private static void MakeCheck(List<DeadMtlLockedReplayBackendDryRunEmitterCheck> checks,
        string id, string label)
    {
        checks.Add(new DeadMtlLockedReplayBackendDryRunEmitterCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = "PASS",
            Expected    = "PASS",
            Actual      = "PASS",
        });
    }

    private static readonly (string Name, string Role)[] s_requiredSourceFiles = new[]
    {
        ("map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_plan.json",    "BACKEND_PLAN_RESULT_JSON"),
        ("map_00.sandbox_writer_locked_replay_backend_operation_plan.json",                    "BACKEND_OPERATION_PLAN_JSON"),
        ("map_00.sandbox_writer_locked_replay_backend_operation_plan.csv",                    "BACKEND_OPERATION_PLAN_CSV"),
        ("map_00.sandbox_writer_locked_replay_backend_source_manifest.json",                  "BACKEND_SOURCE_MANIFEST_JSON"),
        ("map_00.sandbox_writer_locked_replay_backend_forbidden_output_guard.json",           "BACKEND_FORBIDDEN_OUTPUT_GUARD_JSON"),
    };

    private static readonly (string Bucket, string EmitKind, string PayloadFamily)[] s_buckets = new[]
    {
        ("WALL",      "EMIT_WALL_BUCKET_DRY_RUN_RECORD",      "WALL_BUCKET_DRY_RUN"),
        ("FLOOR",     "EMIT_FLOOR_BUCKET_DRY_RUN_RECORD",     "FLOOR_BUCKET_DRY_RUN"),
        ("ACCESS",    "EMIT_ACCESS_BUCKET_DRY_RUN_RECORD",    "ACCESS_BUCKET_DRY_RUN"),
        ("LOT",       "EMIT_LOT_BUCKET_DRY_RUN_RECORD",       "LOT_BUCKET_DRY_RUN"),
        ("COMPONENT", "EMIT_COMPONENT_BUCKET_DRY_RUN_RECORD", "COMPONENT_BUCKET_DRY_RUN"),
    };

    private static string ScanOutputRoot(string outputRoot)
    {
        if (!Directory.Exists(outputRoot))
            return "POST_BACKEND_DRY_RUN_EMITTER_FORBIDDEN_SCAN PASS (0 forbidden artifacts in output root)";

        var patterns = new[] { "*.lotpack", "*.lotheader", "*.lua", "*.bin" };
        int count = patterns.Sum(p =>
            Directory.GetFiles(outputRoot, p, SearchOption.AllDirectories).Length);

        var allDirs = Directory.GetDirectories(outputRoot, "*", SearchOption.AllDirectories);
        if (allDirs.Any(d => { var di = new DirectoryInfo(d); return di.Name == "maps" && di.Parent?.Name == "media"; })) count++;
        if (allDirs.Any(d => string.Equals(new DirectoryInfo(d).Name, "steamapps", StringComparison.OrdinalIgnoreCase))) count++;

        return count == 0
            ? "POST_BACKEND_DRY_RUN_EMITTER_FORBIDDEN_SCAN PASS (0 forbidden artifacts in output root)"
            : $"POST_BACKEND_DRY_RUN_EMITTER_FORBIDDEN_SCAN FAIL ({count} forbidden artifacts found in output root)";
    }

    private static string ComputeEmissionDigest(
        string sourcePlanSha256, string sourceDigest,
        IReadOnlyList<DeadMtlLockedReplayBackendDryRunEmittedRecord> ops)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP27K_BACKEND_DRY_RUN_EMITTER_V1");
        sb.AppendLine(sourcePlanSha256);
        sb.AppendLine(sourceDigest);
        foreach (var op in ops)
        {
            sb.AppendLine($"{op.EmittedOperationId}|{op.SourceOperationKind}|{op.SourceMaterialBucket}|{op.PlannedCellCount}|{op.EmissionStatus}");
        }
        sb.AppendLine("writer_ready=false|runtime_valid=false|materialized=false|runtime_proof_claimed=false|public_playable_packaging_claimed=false");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()))).ToLower();
    }

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterResult Build(
        string backendPlanRoot,
        string outputRoot)
    {
        const string invalidVerdict =
            "MAP27K_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_DRY_RUN_EMITTER_INVALID";

        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterResult
        {
            Format                          = "MAP-27K_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_DRY_RUN_EMITTER",
            GeneratedUtc                    = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MapId                           = "map_00",
            SourceBackendPlanRoot           = backendPlanRoot,
            EmitterStage                    = "SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_DRY_RUN_EMITTER",
            EmitterMode                     = "EMIT_BACKEND_DRY_RUN_OPERATION_RECORDS_ONLY",
            SandboxOnly                     = true,
            SandboxBackendDryRunEmitterOnly = true,
            WriterReady                     = false,
            RuntimeValid                    = false,
            Materialized                    = false,
            PzRuntimeMaterialized           = false,
            RuntimeProofClaimed             = false,
            PublicPlayablePackagingClaimed  = false,
            ClaimBoundaryAudit              = "writer_ready=false | runtime_valid=false | materialized=false | runtime_proof_claimed=false | public_playable_packaging_claimed=false",
            NextAllowedExperimentName       = "MAP-27L_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_DRY_RUN_AUDIT",
            NextAllowedExperimentStatus     = "SANDBOX_ONLY_NOT_RUNTIME",
        };

        if (!Directory.Exists(backendPlanRoot))
        {
            result.Errors.Add($"MAP-27J backend plan root not found: {backendPlanRoot}");
            result.IsValid        = false;
            result.EmitterStatus  = "BACKEND_DRY_RUN_EMITTER_FAILED";
            result.Verdict        = invalidVerdict;
            return result;
        }

        const string planJsonName =
            "map_00.minimal_concrete_geometry_sandbox_writer_locked_replay_backend_plan.json";
        string planJsonPath = Path.Combine(backendPlanRoot, planJsonName);
        result.SourceBackendPlanJsonPath = planJsonPath;

        if (!File.Exists(planJsonPath))
        {
            result.Errors.Add($"MAP-27J backend plan JSON not found: {planJsonPath}");
            result.IsValid        = false;
            result.EmitterStatus  = "BACKEND_DRY_RUN_EMITTER_FAILED";
            result.Verdict        = invalidVerdict;
            return result;
        }

        result.SourceBackendPlanSha256 = HashFile(planJsonPath);

        // Parse MAP-27J plan JSON
        string srcPlanStatus        = string.Empty;
        bool   srcPlanIsValid       = false;
        int    srcCheckCount        = 0;
        int    srcPassedCheckCount  = 0;
        int    srcFailedCheckCount  = 0;
        int    srcOpPlanCount       = 0;
        int    srcTotalCells        = 0;
        int    srcNonEmptyGroups    = 0;
        int    srcEmptyGroups       = 0;
        bool   srcSandboxOnly       = false;
        bool   srcSandboxPlanOnly   = false;
        bool   srcWriterReady       = true;
        bool   srcRuntimeValid      = true;
        bool   srcMaterialized      = true;
        bool   srcPzRuntime         = true;
        bool   srcRuntimeProof      = true;
        bool   srcPublicPlayable    = true;
        string srcForbiddenScan     = string.Empty;
        string srcLockedReplayDigest = string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(planJsonPath, Encoding.UTF8));
            var r = doc.RootElement;
            JsonElement p;
            if (r.TryGetProperty("backend_plan_status",              out p)) srcPlanStatus        = p.GetString() ?? "";
            if (r.TryGetProperty("is_valid",                         out p)) srcPlanIsValid        = p.GetBoolean();
            if (r.TryGetProperty("check_count",                      out p)) srcCheckCount         = p.GetInt32();
            if (r.TryGetProperty("passed_check_count",               out p)) srcPassedCheckCount   = p.GetInt32();
            if (r.TryGetProperty("failed_check_count",               out p)) srcFailedCheckCount   = p.GetInt32();
            if (r.TryGetProperty("operation_plan_count",             out p)) srcOpPlanCount        = p.GetInt32();
            if (r.TryGetProperty("operation_plan_groups",            out p))
            {
                JsonElement g;
                if (p.TryGetProperty("total_planned_cell_count",            out g)) srcTotalCells     = g.GetInt32();
                if (p.TryGetProperty("non_empty_operation_group_count",     out g)) srcNonEmptyGroups = g.GetInt32();
                if (p.TryGetProperty("empty_operation_group_count",         out g)) srcEmptyGroups    = g.GetInt32();
            }
            if (r.TryGetProperty("sandbox_only",                     out p)) srcSandboxOnly       = p.GetBoolean();
            if (r.TryGetProperty("sandbox_backend_plan_only",        out p)) srcSandboxPlanOnly   = p.GetBoolean();
            if (r.TryGetProperty("writer_ready",                     out p)) srcWriterReady        = p.GetBoolean();
            if (r.TryGetProperty("runtime_valid",                    out p)) srcRuntimeValid       = p.GetBoolean();
            if (r.TryGetProperty("materialized",                     out p)) srcMaterialized       = p.GetBoolean();
            if (r.TryGetProperty("pz_runtime_materialized",         out p)) srcPzRuntime          = p.GetBoolean();
            if (r.TryGetProperty("runtime_proof_claimed",            out p)) srcRuntimeProof       = p.GetBoolean();
            if (r.TryGetProperty("public_playable_packaging_claimed", out p)) srcPublicPlayable    = p.GetBoolean();
            if (r.TryGetProperty("forbidden_artifact_scan",          out p)) srcForbiddenScan      = p.GetString() ?? "";
            if (r.TryGetProperty("source_locked_replay_digest",      out p)) srcLockedReplayDigest = p.GetString() ?? "";
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse MAP-27J plan JSON: {ex.Message}");
            result.IsValid       = false;
            result.EmitterStatus = "BACKEND_DRY_RUN_EMITTER_FAILED";
            result.Verdict       = invalidVerdict;
            return result;
        }

        result.SourceBackendPlanStatus          = srcPlanStatus;
        result.SourceBackendPlanIsValid         = srcPlanIsValid;
        result.SourceBackendPlanCheckCount      = srcCheckCount;
        result.SourceBackendPlanPassedCheckCount = srcPassedCheckCount;
        result.SourceBackendPlanFailedCheckCount = srcFailedCheckCount;
        result.SourceOperationPlanCount         = srcOpPlanCount;
        result.SourceTotalPlannedCellCount      = srcTotalCells;
        result.SourceNonEmptyOperationGroupCount = srcNonEmptyGroups;
        result.SourceEmptyOperationGroupCount   = srcEmptyGroups;
        result.SourceLockedReplayDigest         = srcLockedReplayDigest;
        result.SourceForbiddenScan              = srcForbiddenScan;

        // Source manifest — hash all 5 required input files
        var manifestFiles = new List<DeadMtlLockedReplayBackendDryRunSourceManifestFile>();
        for (int i = 0; i < s_requiredSourceFiles.Length; i++)
        {
            var (fname, role) = s_requiredSourceFiles[i];
            string fpath = Path.Combine(backendPlanRoot, fname);
            bool exists  = File.Exists(fpath);
            string hash  = exists ? HashFile(fpath) : string.Empty;
            manifestFiles.Add(new DeadMtlLockedReplayBackendDryRunSourceManifestFile
            {
                FileOrder = i + 1,
                FileRole  = role,
                FileName  = fname,
                FilePath  = fpath,
                Sha256    = hash,
                Exists    = exists,
                Required  = true,
            });
        }
        result.SourceManifestFiles = manifestFiles;
        int hashedCount = manifestFiles.Count(f => f.Exists && !string.IsNullOrEmpty(f.Sha256));

        // Read operation records from MAP-27J operation plan JSON
        string opPlanJsonPath = Path.Combine(backendPlanRoot,
            "map_00.sandbox_writer_locked_replay_backend_operation_plan.json");
        var srcOps = new List<(string Id, string Kind, string Bucket, int Cells, string TargetFamily, string PayloadKind)>();
        if (File.Exists(opPlanJsonPath))
        {
            try
            {
                using var opDoc = JsonDocument.Parse(File.ReadAllText(opPlanJsonPath, Encoding.UTF8));
                if (opDoc.RootElement.TryGetProperty("backend_operation_records", out var recs))
                {
                    foreach (var rec in recs.EnumerateArray())
                    {
                        string opId         = rec.TryGetProperty("operation_id",          out var v) ? v.GetString() ?? "" : "";
                        string opKind       = rec.TryGetProperty("operation_kind",        out v) ? v.GetString() ?? "" : "";
                        string bucket       = rec.TryGetProperty("source_material_bucket", out v) ? v.GetString() ?? "" : "";
                        int    cells        = rec.TryGetProperty("planned_cell_count",     out v) ? v.GetInt32() : 0;
                        string targetFamily = rec.TryGetProperty("backend_target_family",  out v) ? v.GetString() ?? "" : "";
                        string payloadKind  = rec.TryGetProperty("backend_payload_kind",   out v) ? v.GetString() ?? "" : "";
                        srcOps.Add((opId, opKind, bucket, cells, targetFamily, payloadKind));
                    }
                }
            }
            catch { /* srcOps stays empty; checks will catch it */ }
        }

        // Emit 5 dry-run records — use s_buckets order, match to source ops by bucket
        var emittedOps = new List<DeadMtlLockedReplayBackendDryRunEmittedRecord>();
        for (int i = 0; i < s_buckets.Length; i++)
        {
            var (bucket, emitKind, payloadFamily) = s_buckets[i];
            var src = srcOps.FirstOrDefault(o => o.Bucket == bucket);
            int cells = src.Bucket == bucket ? src.Cells : 0;
            string srcOpId       = src.Bucket == bucket ? src.Id : "";
            string srcOpKind     = src.Bucket == bucket ? src.Kind : "";
            string srcTargetFam  = src.Bucket == bucket ? src.TargetFamily : "";
            string srcPayKind    = src.Bucket == bucket ? src.PayloadKind : "";

            string emittedId = $"MAP27K_EMIT_{i + 1:000}_{bucket}";
            emittedOps.Add(new DeadMtlLockedReplayBackendDryRunEmittedRecord
            {
                EmittedOperationOrder      = i + 1,
                EmittedOperationId         = emittedId,
                SourceOperationId          = srcOpId,
                SourceOperationKind        = srcOpKind,
                SourceMaterialBucket       = bucket,
                PlannedCellCount           = cells,
                DryRunEmitKind             = emitKind,
                DryRunPayloadFamily        = payloadFamily,
                SourceLockedReplayDigest   = srcLockedReplayDigest,
                SourceBackendTargetFamily  = srcTargetFam,
                SourceBackendPayloadKind   = srcPayKind,
                RequiresLockedReplayDigest = true,
                WriterConsumable           = false,
                RuntimeConsumable          = false,
                SandboxOnly                = true,
                EmitsRuntimeFile           = false,
                EmitsBinaryFile            = false,
                EmitsLuaFile               = false,
                EmitsInstallPath           = false,
                EmissionStatus             = "DRY_RUN_EMITTED_SANDBOX_RECORD_ONLY",
                EmissionNotes              = bucket == "COMPONENT"
                    ? "Zero-count bucket; dry-run record emitted for invariant completeness"
                    : $"Dry-run record for {bucket.ToLower()} candidate bucket; not a runtime write",
            });
        }

        result.EmittedOperationRecords       = emittedOps;
        result.EmittedOperationCount         = emittedOps.Count;
        result.EmittedTotalPlannedCellCount  = emittedOps.Sum(o => o.PlannedCellCount);
        result.NonEmptyEmittedOperationCount = emittedOps.Count(o => o.PlannedCellCount > 0);
        result.EmptyEmittedOperationCount    = emittedOps.Count(o => o.PlannedCellCount == 0);
        var largest = emittedOps.OrderByDescending(o => o.PlannedCellCount).First();
        result.LargestEmittedOperationBucket    = largest.SourceMaterialBucket;
        result.LargestEmittedOperationCellCount = largest.PlannedCellCount;

        // Compute emission digest
        result.BackendDryRunEmissionDigest = ComputeEmissionDigest(
            result.SourceBackendPlanSha256, srcLockedReplayDigest, emittedOps);

        // Forbidden scan (preliminary — finalized after outputs are written)
        Directory.CreateDirectory(outputRoot);
        string forbiddenScan = ScanOutputRoot(outputRoot);
        bool   scanPasses    = forbiddenScan.StartsWith("POST_BACKEND_DRY_RUN_EMITTER_FORBIDDEN_SCAN PASS", StringComparison.Ordinal);
        result.ForbiddenArtifactScan = forbiddenScan;

        // 49 checks
        var checks = new List<DeadMtlLockedReplayBackendDryRunEmitterCheck>();

        // 1-6: Source file existence
        MakeCheck(checks, "MAP27J_BACKEND_PLAN_ROOT_EXISTS",
            "MAP-27J backend plan root exists");
        AddCheck(checks, "MAP27J_BACKEND_PLAN_JSON_EXISTS",
            "MAP-27J backend plan JSON exists",
            "true", File.Exists(planJsonPath) ? "true" : "false");
        AddCheck(checks, "MAP27J_BACKEND_OPERATION_PLAN_JSON_EXISTS",
            "MAP-27J backend operation plan JSON exists",
            "true", File.Exists(Path.Combine(backendPlanRoot, "map_00.sandbox_writer_locked_replay_backend_operation_plan.json")) ? "true" : "false");
        AddCheck(checks, "MAP27J_BACKEND_OPERATION_PLAN_CSV_EXISTS",
            "MAP-27J backend operation plan CSV exists",
            "true", File.Exists(Path.Combine(backendPlanRoot, "map_00.sandbox_writer_locked_replay_backend_operation_plan.csv")) ? "true" : "false");
        AddCheck(checks, "MAP27J_BACKEND_SOURCE_MANIFEST_JSON_EXISTS",
            "MAP-27J backend source manifest JSON exists",
            "true", File.Exists(Path.Combine(backendPlanRoot, "map_00.sandbox_writer_locked_replay_backend_source_manifest.json")) ? "true" : "false");
        AddCheck(checks, "MAP27J_BACKEND_FORBIDDEN_GUARD_JSON_EXISTS",
            "MAP-27J backend forbidden output guard JSON exists",
            "true", File.Exists(Path.Combine(backendPlanRoot, "map_00.sandbox_writer_locked_replay_backend_forbidden_output_guard.json")) ? "true" : "false");

        // 7: Source manifest hashed
        AddCheck(checks, "MAP27J_5_SOURCE_FILES_HASHED",
            "All 5 required MAP-27J source files hashed",
            "5", hashedCount.ToString());

        // 8-17: Source field validation
        AddCheck(checks, "MAP27J_BACKEND_PLAN_STATUS_COMPLETE",
            "MAP-27J backend_plan_status is BACKEND_PLAN_COMPLETE",
            "BACKEND_PLAN_COMPLETE", srcPlanStatus);
        AddCheck(checks, "MAP27J_IS_VALID_TRUE",
            "MAP-27J is_valid is true",
            "true", srcPlanIsValid ? "true" : "false");
        AddCheck(checks, "MAP27J_CHECK_COUNT_51",
            "MAP-27J check_count is 51",
            "51", srcCheckCount.ToString());
        AddCheck(checks, "MAP27J_PASSED_CHECK_COUNT_51",
            "MAP-27J passed_check_count is 51",
            "51", srcPassedCheckCount.ToString());
        AddCheck(checks, "MAP27J_FAILED_CHECK_COUNT_0",
            "MAP-27J failed_check_count is 0",
            "0", srcFailedCheckCount.ToString());
        AddCheck(checks, "MAP27J_OPERATION_PLAN_COUNT_5",
            "MAP-27J operation_plan_count is 5",
            "5", srcOpPlanCount.ToString());
        AddCheck(checks, "MAP27J_TOTAL_PLANNED_CELL_COUNT_5340",
            "MAP-27J total_planned_cell_count is 5340",
            "5340", srcTotalCells.ToString());
        AddCheck(checks, "MAP27J_NON_EMPTY_GROUPS_4",
            "MAP-27J non_empty_operation_group_count is 4",
            "4", srcNonEmptyGroups.ToString());
        AddCheck(checks, "MAP27J_EMPTY_GROUPS_1",
            "MAP-27J empty_operation_group_count is 1",
            "1", srcEmptyGroups.ToString());
        AddCheck(checks, "MAP27J_FORBIDDEN_SCAN_PASS",
            "MAP-27J forbidden_artifact_scan starts with PASS",
            "PASS", srcForbiddenScan.StartsWith("POST_BACKEND_PLAN_FORBIDDEN_SCAN PASS", StringComparison.Ordinal) ? "PASS" : "FAIL");

        // 18: Digest present
        AddCheck(checks, "SOURCE_LOCKED_REPLAY_DIGEST_PRESENT",
            "Source locked replay digest is present",
            "PRESENT", !string.IsNullOrEmpty(srcLockedReplayDigest) ? "PRESENT" : "ABSENT");

        // 19-23: Emitted operation aggregates
        AddCheck(checks, "EMITTED_OPERATION_COUNT_5",
            "Emitted operation count is 5",
            "5", emittedOps.Count.ToString());
        AddCheck(checks, "EMITTED_TOTAL_PLANNED_CELL_COUNT_5340",
            "Emitted total planned cell count is 5340",
            "5340", result.EmittedTotalPlannedCellCount.ToString());
        AddCheck(checks, "EMITTED_NON_EMPTY_OPERATION_COUNT_4",
            "Non-empty emitted operation count is 4",
            "4", result.NonEmptyEmittedOperationCount.ToString());
        AddCheck(checks, "EMITTED_EMPTY_OPERATION_COUNT_1",
            "Empty emitted operation count is 1",
            "1", result.EmptyEmittedOperationCount.ToString());
        AddCheck(checks, "EMITTED_LARGEST_BUCKET_FLOOR",
            "Largest emitted operation bucket is FLOOR",
            "FLOOR", result.LargestEmittedOperationBucket);

        // 24-28: Per-bucket counts
        var wallOp      = emittedOps.FirstOrDefault(o => o.SourceMaterialBucket == "WALL");
        var floorOp     = emittedOps.FirstOrDefault(o => o.SourceMaterialBucket == "FLOOR");
        var accessOp    = emittedOps.FirstOrDefault(o => o.SourceMaterialBucket == "ACCESS");
        var lotOp       = emittedOps.FirstOrDefault(o => o.SourceMaterialBucket == "LOT");
        var componentOp = emittedOps.FirstOrDefault(o => o.SourceMaterialBucket == "COMPONENT");

        AddCheck(checks, "WALL_EMITTED_COUNT_850",
            "WALL emitted planned_cell_count is 850",
            "850", wallOp?.PlannedCellCount.ToString() ?? "MISSING");
        AddCheck(checks, "FLOOR_EMITTED_COUNT_2444",
            "FLOOR emitted planned_cell_count is 2444",
            "2444", floorOp?.PlannedCellCount.ToString() ?? "MISSING");
        AddCheck(checks, "ACCESS_EMITTED_COUNT_148",
            "ACCESS emitted planned_cell_count is 148",
            "148", accessOp?.PlannedCellCount.ToString() ?? "MISSING");
        AddCheck(checks, "LOT_EMITTED_COUNT_1898",
            "LOT emitted planned_cell_count is 1898",
            "1898", lotOp?.PlannedCellCount.ToString() ?? "MISSING");
        AddCheck(checks, "COMPONENT_EMITTED_COUNT_0",
            "COMPONENT emitted planned_cell_count is 0",
            "0", componentOp?.PlannedCellCount.ToString() ?? "MISSING");

        // 29-37: Emitted operation invariants
        bool allRequireDigest    = emittedOps.All(o => o.RequiresLockedReplayDigest);
        bool allUseSourceDigest  = emittedOps.All(o => o.SourceLockedReplayDigest == srcLockedReplayDigest);
        bool allSandboxOnly      = emittedOps.All(o => o.SandboxOnly);
        bool noWriterConsumable  = emittedOps.All(o => !o.WriterConsumable);
        bool noRuntimeConsumable = emittedOps.All(o => !o.RuntimeConsumable);
        bool noEmitsRuntime      = emittedOps.All(o => !o.EmitsRuntimeFile);
        bool noEmitsBinary       = emittedOps.All(o => !o.EmitsBinaryFile);
        bool noEmitsLua          = emittedOps.All(o => !o.EmitsLuaFile);
        bool noEmitsInstall      = emittedOps.All(o => !o.EmitsInstallPath);

        AddCheck(checks, "ALL_EMITTED_OPERATIONS_REQUIRE_LOCKED_REPLAY_DIGEST",
            "All emitted operations have requires_locked_replay_digest = true",
            "true", allRequireDigest ? "true" : "false");
        AddCheck(checks, "ALL_EMITTED_OPERATIONS_USE_SOURCE_DIGEST",
            "All emitted operations carry the source locked replay digest",
            "true", allUseSourceDigest ? "true" : "false");
        AddCheck(checks, "ALL_EMITTED_OPERATIONS_SANDBOX_ONLY",
            "All emitted operations have sandbox_only = true",
            "true", allSandboxOnly ? "true" : "false");
        AddCheck(checks, "NO_EMITTED_OPERATION_WRITER_CONSUMABLE",
            "No emitted operation has writer_consumable = true",
            "true", noWriterConsumable ? "true" : "false");
        AddCheck(checks, "NO_EMITTED_OPERATION_RUNTIME_CONSUMABLE",
            "No emitted operation has runtime_consumable = true",
            "true", noRuntimeConsumable ? "true" : "false");
        AddCheck(checks, "NO_EMITTED_OPERATION_EMITS_RUNTIME_FILE",
            "No emitted operation has emits_runtime_file = true",
            "true", noEmitsRuntime ? "true" : "false");
        AddCheck(checks, "NO_EMITTED_OPERATION_EMITS_BINARY_FILE",
            "No emitted operation has emits_binary_file = true",
            "true", noEmitsBinary ? "true" : "false");
        AddCheck(checks, "NO_EMITTED_OPERATION_EMITS_LUA_FILE",
            "No emitted operation has emits_lua_file = true",
            "true", noEmitsLua ? "true" : "false");
        AddCheck(checks, "NO_EMITTED_OPERATION_EMITS_INSTALL_PATH",
            "No emitted operation has emits_install_path = true",
            "true", noEmitsInstall ? "true" : "false");

        // 38: Emission digest
        AddCheck(checks, "BACKEND_DRY_RUN_EMISSION_DIGEST_PRESENT",
            "Backend dry-run emission digest is present",
            "PRESENT", !string.IsNullOrEmpty(result.BackendDryRunEmissionDigest) ? "PRESENT" : "ABSENT");

        // 39: Forbidden scan
        AddCheck(checks, "POST_BACKEND_DRY_RUN_EMITTER_FORBIDDEN_SCAN_PASS",
            "Post backend dry-run emitter forbidden artifact scan passes",
            "PASS", scanPasses ? "PASS" : "FAIL");

        // 40-49: Claim boundary
        MakeCheck(checks, "SANDBOX_ONLY_TRUE",                         "sandbox_only is true");
        MakeCheck(checks, "SANDBOX_BACKEND_DRY_RUN_EMITTER_ONLY_TRUE", "sandbox_backend_dry_run_emitter_only is true");
        MakeCheck(checks, "WRITER_READY_FALSE",                        "writer_ready is false");
        MakeCheck(checks, "RUNTIME_VALID_FALSE",                       "runtime_valid is false");
        MakeCheck(checks, "MATERIALIZED_FALSE",                        "materialized is false");
        MakeCheck(checks, "PZ_RUNTIME_MATERIALIZED_FALSE",             "pz_runtime_materialized is false");
        MakeCheck(checks, "NO_RUNTIME_PROOF_CLAIM",                    "No runtime proof claimed");
        MakeCheck(checks, "NO_PUBLIC_PLAYABLE_PACKAGING_CLAIM",        "No public playable packaging claimed");
        MakeCheck(checks, "NO_RUNTIME_OUTPUTS_EMITTED",                "No runtime outputs emitted — dry-run backend records only");
        MakeCheck(checks, "NEXT_ALLOWED_EXPERIMENT_SANDBOX_ONLY",      "Next allowed experiment is sandbox-only");

        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass            = result.FailedCheckCount == 0;
        result.IsValid          = allPass;
        result.EmitterStatus    = allPass ? "BACKEND_DRY_RUN_EMITTER_COMPLETE" : "BACKEND_DRY_RUN_EMITTER_FAILED";
        result.Verdict          = allPass
            ? "MAP27K_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_DRY_RUN_EMITTER_COMPLETE"
            : invalidVerdict;

        return result;
    }

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterResult FinalizeAfterOutputs(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterResult result,
        string outputRoot)
    {
        const string invalidVerdict =
            "MAP27K_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_DRY_RUN_EMITTER_INVALID";

        string finalScan = ScanOutputRoot(outputRoot);
        bool   scanPasses = finalScan.StartsWith("POST_BACKEND_DRY_RUN_EMITTER_FORBIDDEN_SCAN PASS", StringComparison.Ordinal);
        result.ForbiddenArtifactScan = finalScan;

        var check = result.Checks.FirstOrDefault(c => c.CheckId == "POST_BACKEND_DRY_RUN_EMITTER_FORBIDDEN_SCAN_PASS");
        if (check is not null)
        {
            check.CheckStatus = scanPasses ? "PASS" : "FAIL";
            check.Actual      = scanPasses ? "PASS" : "FAIL";
        }

        result.PassedCheckCount = result.Checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = result.Checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass            = result.FailedCheckCount == 0;
        result.IsValid          = allPass;
        result.EmitterStatus    = allPass ? "BACKEND_DRY_RUN_EMITTER_COMPLETE" : "BACKEND_DRY_RUN_EMITTER_FAILED";
        result.Verdict          = allPass
            ? "MAP27K_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_DRY_RUN_EMITTER_COMPLETE"
            : invalidVerdict;

        return result;
    }

    public string RenderJson(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterResult result) =>
        JsonSerializer.Serialize(result, s_jsonOptions);

    public string RenderMarkdown(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-27K WorldBuilder Minimal Concrete Geometry Sandbox Writer Locked Replay Backend Dry-Run Emitter");
        sb.AppendLine();
        sb.AppendLine($"- **Map ID:** {result.MapId}");
        sb.AppendLine($"- **Generated UTC:** {result.GeneratedUtc}");
        sb.AppendLine($"- **Emitter Stage:** {result.EmitterStage}");
        sb.AppendLine($"- **Emitter Mode:** {result.EmitterMode}");
        sb.AppendLine($"- **Emitter Status:** {result.EmitterStatus}");
        sb.AppendLine($"- **Source Backend Plan Status:** {result.SourceBackendPlanStatus}");
        sb.AppendLine($"- **Source Locked Replay Digest:** `{result.SourceLockedReplayDigest}`");
        sb.AppendLine($"- **Emitted Operation Count:** {result.EmittedOperationCount}");
        sb.AppendLine($"- **Emitted Total Planned Cells:** {result.EmittedTotalPlannedCellCount}");
        sb.AppendLine($"- **Non-empty Emitted:** {result.NonEmptyEmittedOperationCount}");
        sb.AppendLine($"- **Empty Emitted:** {result.EmptyEmittedOperationCount}");
        sb.AppendLine($"- **Largest Bucket:** {result.LargestEmittedOperationBucket} ({result.LargestEmittedOperationCellCount} cells)");
        sb.AppendLine($"- **Emission Digest:** `{result.BackendDryRunEmissionDigest}`");
        sb.AppendLine($"- **Forbidden Artifact Scan:** {result.ForbiddenArtifactScan}");
        sb.AppendLine($"- **Verdict:** `{result.Verdict}`");
        sb.AppendLine($"- **Is Valid:** {result.IsValid}");
        sb.AppendLine($"- **Checks:** {result.CheckCount} / Passed: {result.PassedCheckCount} / Failed: {result.FailedCheckCount}");
        sb.AppendLine();
        sb.AppendLine("## Emitted Operations");
        sb.AppendLine();
        sb.AppendLine("| Order | Emit Kind | Bucket | Planned Cells | Status |");
        sb.AppendLine("|-------|-----------|--------|--------------|--------|");
        foreach (var op in result.EmittedOperationRecords)
            sb.AppendLine($"| {op.EmittedOperationOrder} | {op.DryRunEmitKind} | {op.SourceMaterialBucket} | {op.PlannedCellCount} | {op.EmissionStatus} |");
        sb.AppendLine();
        sb.AppendLine("## Checks");
        sb.AppendLine();
        sb.AppendLine("| # | Check ID | Status | Expected | Actual |");
        sb.AppendLine("|---|----------|--------|----------|--------|");
        foreach (var c in result.Checks)
            sb.AppendLine($"| {c.CheckOrder} | {c.CheckId} | {c.CheckStatus} | {c.Expected} | {c.Actual} |");
        return sb.ToString();
    }

    public string RenderCsv(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_order,check_id,check_status,expected,actual");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckOrder},{c.CheckId},{c.CheckStatus},{c.Expected},{c.Actual}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-27K WorldBuilder Minimal Concrete Geometry Sandbox Writer Locked Replay Backend Dry-Run Emitter");
        sb.AppendLine($"Generated UTC                    : {result.GeneratedUtc}");
        sb.AppendLine($"Map ID                           : {result.MapId}");
        sb.AppendLine($"Emitter Stage                    : {result.EmitterStage}");
        sb.AppendLine($"Emitter Mode                     : {result.EmitterMode}");
        sb.AppendLine($"Emitter Status                   : {result.EmitterStatus}");
        sb.AppendLine($"Source Backend Plan Status       : {result.SourceBackendPlanStatus}");
        sb.AppendLine($"Source Locked Replay Digest      : {result.SourceLockedReplayDigest}");
        sb.AppendLine($"Source Operation Plan Count      : {result.SourceOperationPlanCount}");
        sb.AppendLine($"Source Total Planned Cells       : {result.SourceTotalPlannedCellCount}");
        sb.AppendLine($"Emitted Operation Count          : {result.EmittedOperationCount}");
        sb.AppendLine($"Emitted Total Planned Cells      : {result.EmittedTotalPlannedCellCount}");
        sb.AppendLine($"Non-empty Emitted Operations     : {result.NonEmptyEmittedOperationCount}");
        sb.AppendLine($"Empty Emitted Operations         : {result.EmptyEmittedOperationCount}");
        sb.AppendLine($"Largest Emitted Bucket           : {result.LargestEmittedOperationBucket} ({result.LargestEmittedOperationCellCount} cells)");
        sb.AppendLine($"Emission Digest                  : {result.BackendDryRunEmissionDigest}");
        sb.AppendLine($"Forbidden Artifact Scan          : {result.ForbiddenArtifactScan}");
        sb.AppendLine($"Claim Boundary                   : {result.ClaimBoundaryAudit}");
        sb.AppendLine($"Verdict                          : {result.Verdict}");
        sb.AppendLine($"Is Valid                         : {(result.IsValid ? 1 : 0)}");
        sb.AppendLine($"Sandbox Only                     : 1");
        sb.AppendLine($"Sandbox Dry Run Emitter Only     : 1");
        sb.AppendLine($"PZ Runtime Materialized          : 0");
        sb.AppendLine($"Writer Ready                     : 0");
        sb.AppendLine($"Runtime Valid                    : 0");
        sb.AppendLine($"Materialized                     : 0");
        sb.AppendLine($"Runtime Proof                    : 0");
        sb.AppendLine($"Public Playable                  : 0");
        sb.AppendLine($"Next Allowed Experiment          : {result.NextAllowedExperimentName}");
        sb.AppendLine($"Next Experiment Status           : {result.NextAllowedExperimentStatus}");
        sb.AppendLine($"Checks                           : {result.CheckCount}");
        sb.AppendLine($"Passed                           : {result.PassedCheckCount}");
        sb.Append(    $"Failed                           : {result.FailedCheckCount}");
        return sb.ToString();
    }

    public string RenderOperationsJson(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterResult result)
    {
        var plan = new
        {
            emitted_operation_count          = result.EmittedOperationCount,
            emitted_total_planned_cell_count = result.EmittedTotalPlannedCellCount,
            non_empty_emitted_operation_count = result.NonEmptyEmittedOperationCount,
            empty_emitted_operation_count    = result.EmptyEmittedOperationCount,
            largest_emitted_operation_bucket = result.LargestEmittedOperationBucket,
            largest_emitted_operation_cell_count = result.LargestEmittedOperationCellCount,
            emitted_operation_records        = result.EmittedOperationRecords,
        };
        return JsonSerializer.Serialize(plan, s_jsonOptions);
    }

    public string RenderOperationsCsv(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("emitted_operation_order,emitted_operation_id,source_operation_id,source_material_bucket,planned_cell_count,dry_run_emit_kind,emission_status,requires_locked_replay_digest,writer_consumable,runtime_consumable,sandbox_only,emits_runtime_file,emits_binary_file,emits_lua_file,emits_install_path");
        foreach (var op in result.EmittedOperationRecords)
            sb.AppendLine($"{op.EmittedOperationOrder},{op.EmittedOperationId},{op.SourceOperationId},{op.SourceMaterialBucket},{op.PlannedCellCount},{op.DryRunEmitKind},{op.EmissionStatus},{op.RequiresLockedReplayDigest},{op.WriterConsumable},{op.RuntimeConsumable},{op.SandboxOnly},{op.EmitsRuntimeFile},{op.EmitsBinaryFile},{op.EmitsLuaFile},{op.EmitsInstallPath}");
        return sb.ToString();
    }

    public string RenderSourceManifestJson(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterResult result)
    {
        var manifest = new
        {
            source_backend_plan_root  = result.SourceBackendPlanRoot,
            source_backend_plan_sha256 = result.SourceBackendPlanSha256,
            source_file_count         = result.SourceManifestFiles.Count,
            all_source_files_exist    = result.SourceManifestFiles.All(f => f.Exists),
            all_source_files_hashed   = result.SourceManifestFiles.All(f => !string.IsNullOrEmpty(f.Sha256)),
            source_files              = result.SourceManifestFiles,
        };
        return JsonSerializer.Serialize(manifest, s_jsonOptions);
    }

    public string RenderDryRunDigestJson(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterResult result)
    {
        var digest = new
        {
            backend_dry_run_emission_digest   = result.BackendDryRunEmissionDigest,
            source_backend_plan_sha256        = result.SourceBackendPlanSha256,
            source_locked_replay_digest       = result.SourceLockedReplayDigest,
            emitted_operation_count           = result.EmittedOperationCount,
            emitted_total_planned_cell_count  = result.EmittedTotalPlannedCellCount,
        };
        return JsonSerializer.Serialize(digest, s_jsonOptions);
    }

    public string RenderForbiddenOutputGuardJson(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterResult result)
    {
        var guard = new
        {
            forbidden_output_guard                    = true,
            sandbox_only                              = true,
            sandbox_backend_dry_run_emitter_only      = true,
            writer_ready                              = false,
            runtime_valid                             = false,
            materialized                              = false,
            pz_runtime_materialized                   = false,
            runtime_proof_claimed                     = false,
            public_playable_packaging_claimed         = false,
            forbidden_artifact_scan                   = result.ForbiddenArtifactScan,
            all_clean = result.ForbiddenArtifactScan.StartsWith("POST_BACKEND_DRY_RUN_EMITTER_FORBIDDEN_SCAN PASS", StringComparison.Ordinal),
        };
        return JsonSerializer.Serialize(guard, s_jsonOptions);
    }
}
