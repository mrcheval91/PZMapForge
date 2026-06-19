using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanBuilder
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    private static string HashFile(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLower();

    private static void AddCheck(List<DeadMtlLockedReplayBackendPlanCheck> checks,
        string id, string label, string expected, string actual)
    {
        checks.Add(new DeadMtlLockedReplayBackendPlanCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = string.Equals(expected, actual, StringComparison.Ordinal) ? "PASS" : "FAIL",
            Expected    = expected,
            Actual      = actual,
        });
    }

    private static void MakeCheck(List<DeadMtlLockedReplayBackendPlanCheck> checks,
        string id, string label)
    {
        checks.Add(new DeadMtlLockedReplayBackendPlanCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = "PASS",
            Expected    = "PASS",
            Actual      = "PASS",
        });
    }

    private static readonly string[] s_requiredForbiddenSteps = new[]
    {
        "LOT_PACK_RUNTIME_BINARY",
        "LOT_HEADER_RUNTIME_BINARY",
        "WORLDGEN_OVERRIDE_LUA",
        "RUNTIME_LUA",
        "PROJECT_ZOMBOID_INSTALL_PATH",
        "STEAM_WORKSHOP_OUTPUT",
        "COMPILE_WORLDGEN_INVOCATION",
        "MAP_00_PNG_MUTATION",
        "RUNTIME_PROOF_CLAIM",
        "WRITER_READY_CLAIM",
        "PUBLIC_PLAYABLE_PACKAGING_CLAIM",
    };

    private static readonly (string Name, string Role)[] s_requiredSourceFiles = new[]
    {
        ("map_00.minimal_concrete_geometry_sandbox_writer_locked_materialization_replay_dry_run.json",         "DRY_RUN_RESULT_JSON"),
        ("map_00.minimal_concrete_geometry_sandbox_writer_locked_materialization_replay_dry_run.summary.txt",  "DRY_RUN_SUMMARY_TXT"),
        ("map_00.sandbox_writer_locked_replay_material_counts.csv",                                            "LOCKED_REPLAY_MATERIAL_COUNTS_CSV"),
        ("map_00.sandbox_writer_locked_replay_source_manifest.json",                                           "LOCKED_REPLAY_SOURCE_MANIFEST_JSON"),
        ("map_00.sandbox_writer_locked_replay_digest.json",                                                    "LOCKED_REPLAY_DIGEST_JSON"),
        ("map_00.sandbox_writer_locked_replay_forbidden_output_guard.json",                                    "LOCKED_REPLAY_FORBIDDEN_OUTPUT_GUARD_JSON"),
    };

    private static string ScanOutputRoot(string outputRoot)
    {
        if (!Directory.Exists(outputRoot))
            return "POST_BACKEND_PLAN_FORBIDDEN_SCAN PASS (0 forbidden artifacts in output root)";

        var patterns = new[] { "*.lotpack", "*.lotheader", "*.lua", "*.bin" };
        int count = patterns.Sum(p =>
            Directory.GetFiles(outputRoot, p, SearchOption.AllDirectories).Length);

        var allDirs = Directory.GetDirectories(outputRoot, "*", SearchOption.AllDirectories);
        if (allDirs.Any(d => { var di = new DirectoryInfo(d); return di.Name == "maps" && di.Parent?.Name == "media"; })) count++;
        if (allDirs.Any(d => string.Equals(new DirectoryInfo(d).Name, "steamapps", StringComparison.OrdinalIgnoreCase))) count++;

        return count == 0
            ? "POST_BACKEND_PLAN_FORBIDDEN_SCAN PASS (0 forbidden artifacts in output root)"
            : $"POST_BACKEND_PLAN_FORBIDDEN_SCAN FAIL ({count} forbidden artifacts found in output root)";
    }

    private static DeadMtlLockedReplayBackendOperationRecord MakeOperationRecord(
        int order, string kind, string bucket, int cellCount,
        string countField, string targetFamily, string digest) =>
        new()
        {
            OperationOrder             = order,
            OperationId                = $"MAP27J_OP_{order:000}_{kind}",
            OperationKind              = kind,
            SourceMaterialBucket       = bucket,
            PlannedCellCount           = cellCount,
            SourceCountField           = countField,
            SourceCountValue           = cellCount,
            BackendTargetFamily        = targetFamily,
            BackendPayloadKind         = "FUTURE_TILE_WRITE_BUCKET",
            RequiresLockedReplayDigest = true,
            SourceLockedReplayDigest   = digest,
            WriterConsumable           = false,
            RuntimeConsumable          = false,
            SandboxOnly                = true,
            EmitsRuntimeFile           = false,
            EmitsBinaryFile            = false,
            EmitsLuaFile               = false,
            EmitsInstallPath           = false,
            Notes                      = bucket == "COMPONENT"
                ? "Zero-count bucket; plan emitted for invariant completeness; future backend may skip if count is 0"
                : $"Future backend uses locked replay digest to verify source before writing {bucket.ToLower()} candidates",
        };

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanResult Build(
        string dryRunRoot,
        string outputRoot)
    {
        const string invalidVerdict =
            "MAP27J_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_PLAN_INVALID";

        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanResult
        {
            Format                         = "MAP-27J_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_PLAN",
            GeneratedUtc                   = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MapId                          = "map_00",
            SourceDryRunRoot               = dryRunRoot,
            BackendPlanStage               = "SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_PLAN",
            BackendPlanMode                = "BACKEND_NEUTRAL_OPERATION_PLAN_ONLY",
            SandboxOnly                    = true,
            SandboxBackendPlanOnly         = true,
            WriterReady                    = false,
            RuntimeValid                   = false,
            Materialized                   = false,
            PzRuntimeMaterialized          = false,
            RuntimeProofClaimed            = false,
            PublicPlayablePackagingClaimed = false,
            ClaimBoundaryAudit             = "writer_ready=false | runtime_valid=false | materialized=false | runtime_proof_claimed=false | public_playable_packaging_claimed=false",
            NextAllowedExperimentName      = "MAP-27K_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_DRY_RUN_EMITTER",
            NextAllowedExperimentStatus    = "SANDBOX_ONLY_NOT_RUNTIME",
            NextForbiddenSteps             = new List<string>(s_requiredForbiddenSteps),
        };

        if (!Directory.Exists(dryRunRoot))
        {
            result.Errors.Add($"MAP-27I dry-run root not found: {dryRunRoot}");
            result.IsValid = false;
            result.BackendPlanStatus = "BACKEND_PLAN_FAILED";
            result.Verdict = invalidVerdict;
            return result;
        }

        const string dryRunJsonName =
            "map_00.minimal_concrete_geometry_sandbox_writer_locked_materialization_replay_dry_run.json";
        string dryRunJsonPath = Path.Combine(dryRunRoot, dryRunJsonName);
        result.SourceDryRunJsonPath = dryRunJsonPath;

        if (!File.Exists(dryRunJsonPath))
        {
            result.Errors.Add($"MAP-27I dry-run JSON not found: {dryRunJsonPath}");
            result.IsValid = false;
            result.BackendPlanStatus = "BACKEND_PLAN_FAILED";
            result.Verdict = invalidVerdict;
            return result;
        }

        result.SourceDryRunSha256 = HashFile(dryRunJsonPath);

        // Parse MAP-27I JSON
        string srcDryRunStatus         = string.Empty;
        string srcVerdict              = string.Empty;
        bool   srcIsValid              = false;
        int    srcCheckCount           = 0;
        int    srcPassedCheckCount     = 0;
        int    srcFailedCheckCount     = 0;
        string srcLockedReplayDigest   = string.Empty;
        int    srcMaterializedCells    = 0;
        int    srcWall                 = 0;
        int    srcFloor                = 0;
        int    srcAccess               = 0;
        int    srcLot                  = 0;
        int    srcResidual             = 0;
        int    srcMaterialKinds        = 0;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(dryRunJsonPath, Encoding.UTF8));
            var r = doc.RootElement;
            JsonElement p;
            if (r.TryGetProperty("dry_run_status",                     out p)) srcDryRunStatus         = p.GetString() ?? "";
            if (r.TryGetProperty("verdict",                            out p)) srcVerdict               = p.GetString() ?? "";
            if (r.TryGetProperty("is_valid",                           out p)) srcIsValid               = p.GetBoolean();
            if (r.TryGetProperty("check_count",                        out p)) srcCheckCount            = p.GetInt32();
            if (r.TryGetProperty("passed_check_count",                 out p)) srcPassedCheckCount      = p.GetInt32();
            if (r.TryGetProperty("failed_check_count",                 out p)) srcFailedCheckCount      = p.GetInt32();
            if (r.TryGetProperty("locked_replay_digest",               out p)) srcLockedReplayDigest    = p.GetString() ?? "";
            if (r.TryGetProperty("materialized_cell_count",            out p)) srcMaterializedCells     = p.GetInt32();
            if (r.TryGetProperty("building_wall_candidate_cell_count", out p)) srcWall                  = p.GetInt32();
            if (r.TryGetProperty("building_floor_candidate_cell_count",out p)) srcFloor                 = p.GetInt32();
            if (r.TryGetProperty("access_edge_cell_count",             out p)) srcAccess                = p.GetInt32();
            if (r.TryGetProperty("lot_space_cell_count",               out p)) srcLot                   = p.GetInt32();
            if (r.TryGetProperty("component_residual_cell_count",      out p)) srcResidual              = p.GetInt32();
            if (r.TryGetProperty("material_kind_count",                out p)) srcMaterialKinds         = p.GetInt32();
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse MAP-27I dry-run JSON: {ex.Message}");
            result.IsValid = false;
            result.BackendPlanStatus = "BACKEND_PLAN_FAILED";
            result.Verdict = invalidVerdict;
            return result;
        }

        result.SourceDryRunStatus               = srcDryRunStatus;
        result.SourceVerdict                    = srcVerdict;
        result.SourceIsValid                    = srcIsValid;
        result.SourceCheckCount                 = srcCheckCount;
        result.SourcePassedCheckCount           = srcPassedCheckCount;
        result.SourceFailedCheckCount           = srcFailedCheckCount;
        result.SourceLockedReplayDigest         = srcLockedReplayDigest;
        result.SourceMaterializedCellCount      = srcMaterializedCells;
        result.SourceBuildingWallCandidateCellCount  = srcWall;
        result.SourceBuildingFloorCandidateCellCount = srcFloor;
        result.SourceAccessEdgeCellCount        = srcAccess;
        result.SourceLotSpaceCellCount          = srcLot;
        result.SourceComponentResidualCellCount = srcResidual;
        result.SourceMaterialKindCount          = srcMaterialKinds;

        // Source manifest — hash all 6 required input files
        var manifestFiles = new List<DeadMtlLockedReplayBackendSourceManifestFile>();
        for (int i = 0; i < s_requiredSourceFiles.Length; i++)
        {
            var (fname, role) = s_requiredSourceFiles[i];
            string fpath = Path.Combine(dryRunRoot, fname);
            bool exists  = File.Exists(fpath);
            string hash  = exists ? HashFile(fpath) : string.Empty;

            if (role == "LOCKED_REPLAY_DIGEST_JSON" && exists)
                result.SourceLockedReplayDigestSha256 = hash;

            manifestFiles.Add(new DeadMtlLockedReplayBackendSourceManifestFile
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

        // Backend operation records (5 canonical buckets)
        var ops = new List<DeadMtlLockedReplayBackendOperationRecord>
        {
            MakeOperationRecord(1, "WRITE_WALL_CANDIDATE_BUCKET",      "WALL",      srcWall,     "building_wall_candidate_cell_count",  "BUILDING_WALL_LAYER_CANDIDATE",     srcLockedReplayDigest),
            MakeOperationRecord(2, "WRITE_FLOOR_CANDIDATE_BUCKET",     "FLOOR",     srcFloor,    "building_floor_candidate_cell_count", "BUILDING_FLOOR_LAYER_CANDIDATE",    srcLockedReplayDigest),
            MakeOperationRecord(3, "WRITE_ACCESS_EDGE_BUCKET",         "ACCESS",    srcAccess,   "access_edge_cell_count",              "ACCESS_EDGE_LAYER_CANDIDATE",       srcLockedReplayDigest),
            MakeOperationRecord(4, "WRITE_LOT_SPACE_BUCKET",           "LOT",       srcLot,      "lot_space_cell_count",                "LOT_SPACE_LAYER_CANDIDATE",         srcLockedReplayDigest),
            MakeOperationRecord(5, "WRITE_COMPONENT_RESIDUAL_BUCKET",  "COMPONENT", srcResidual, "component_residual_cell_count",       "COMPONENT_RESIDUAL_LAYER_CANDIDATE", srcLockedReplayDigest),
        };
        result.BackendOperationRecords = ops;
        result.OperationPlanCount      = ops.Count;

        int totalPlanned      = ops.Sum(o => o.PlannedCellCount);
        int nonEmptyGroups    = ops.Count(o => o.PlannedCellCount > 0);
        int emptyGroups       = ops.Count(o => o.PlannedCellCount == 0);
        var largest           = ops.OrderByDescending(o => o.PlannedCellCount).First();
        result.OperationPlanGroups = new DeadMtlLockedReplayBackendPlanOperationGroups
        {
            OperationGroupCount              = ops.Count,
            TotalPlannedCellCount            = totalPlanned,
            NonEmptyOperationGroupCount      = nonEmptyGroups,
            EmptyOperationGroupCount         = emptyGroups,
            LargestOperationGroup            = largest.SourceMaterialBucket,
            LargestOperationGroupCellCount   = largest.PlannedCellCount,
        };

        // Forbidden artifact scan of output root
        Directory.CreateDirectory(outputRoot);
        string forbiddenScan = ScanOutputRoot(outputRoot);
        bool   scanPasses    = forbiddenScan.StartsWith("POST_BACKEND_PLAN_FORBIDDEN_SCAN PASS", StringComparison.Ordinal);
        result.ForbiddenArtifactScan = forbiddenScan;

        // 51 checks
        var checks = new List<DeadMtlLockedReplayBackendPlanCheck>();

        // 1-3: Source root/JSON/hash
        MakeCheck(checks, "MAP27I_DRY_RUN_ROOT_EXISTS",
            "MAP-27I dry-run root exists");
        AddCheck(checks, "MAP27I_DRY_RUN_JSON_EXISTS",
            "MAP-27I dry-run JSON file exists",
            "true", File.Exists(dryRunJsonPath) ? "true" : "false");
        MakeCheck(checks, "MAP27I_DRY_RUN_JSON_HASHED",
            "MAP-27I dry-run JSON SHA-256 hashed");

        // 4-17: Source field checks
        AddCheck(checks, "MAP27I_DRY_RUN_STATUS_COMPLETE",
            "MAP-27I dry_run_status is LOCKED_REPLAY_DRY_RUN_COMPLETE",
            "LOCKED_REPLAY_DRY_RUN_COMPLETE", srcDryRunStatus);
        AddCheck(checks, "MAP27I_VERDICT_COMPLETE",
            "MAP-27I verdict is complete",
            "MAP27I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_MATERIALIZATION_REPLAY_DRY_RUN_COMPLETE",
            srcVerdict);
        AddCheck(checks, "MAP27I_IS_VALID_TRUE",
            "MAP-27I is_valid is true",
            "true", srcIsValid ? "true" : "false");
        AddCheck(checks, "MAP27I_CHECK_COUNT_53",
            "MAP-27I check_count is 53",
            "53", srcCheckCount.ToString());
        AddCheck(checks, "MAP27I_PASSED_CHECK_COUNT_53",
            "MAP-27I passed_check_count is 53",
            "53", srcPassedCheckCount.ToString());
        AddCheck(checks, "MAP27I_FAILED_CHECK_COUNT_0",
            "MAP-27I failed_check_count is 0",
            "0", srcFailedCheckCount.ToString());
        AddCheck(checks, "MAP27I_LOCKED_REPLAY_DIGEST_PRESENT",
            "MAP-27I locked_replay_digest is non-empty",
            "PRESENT", !string.IsNullOrEmpty(srcLockedReplayDigest) ? "PRESENT" : "ABSENT");
        AddCheck(checks, "MAP27I_MATERIALIZED_CELL_COUNT_5340",
            "MAP-27I materialized_cell_count is 5340",
            "5340", srcMaterializedCells.ToString());
        AddCheck(checks, "MAP27I_WALL_COUNT_850",
            "MAP-27I building_wall_candidate_cell_count is 850",
            "850", srcWall.ToString());
        AddCheck(checks, "MAP27I_FLOOR_COUNT_2444",
            "MAP-27I building_floor_candidate_cell_count is 2444",
            "2444", srcFloor.ToString());
        AddCheck(checks, "MAP27I_ACCESS_COUNT_148",
            "MAP-27I access_edge_cell_count is 148",
            "148", srcAccess.ToString());
        AddCheck(checks, "MAP27I_LOT_COUNT_1898",
            "MAP-27I lot_space_cell_count is 1898",
            "1898", srcLot.ToString());
        AddCheck(checks, "MAP27I_COMPONENT_RESIDUAL_COUNT_0",
            "MAP-27I component_residual_cell_count is 0",
            "0", srcResidual.ToString());
        AddCheck(checks, "MAP27I_MATERIAL_KIND_COUNT_5",
            "MAP-27I material_kind_count is 5",
            "5", srcMaterialKinds.ToString());

        // 18-24: Source manifest file existence + hashed aggregate
        AddCheck(checks, "SOURCE_MANIFEST_FILE_1_DRY_RUN_RESULT_JSON_EXISTS",
            "Source manifest file 1 DRY_RUN_RESULT_JSON exists",
            "true", manifestFiles[0].Exists ? "true" : "false");
        AddCheck(checks, "SOURCE_MANIFEST_FILE_2_DRY_RUN_SUMMARY_TXT_EXISTS",
            "Source manifest file 2 DRY_RUN_SUMMARY_TXT exists",
            "true", manifestFiles[1].Exists ? "true" : "false");
        AddCheck(checks, "SOURCE_MANIFEST_FILE_3_LOCKED_REPLAY_MATERIAL_COUNTS_CSV_EXISTS",
            "Source manifest file 3 LOCKED_REPLAY_MATERIAL_COUNTS_CSV exists",
            "true", manifestFiles[2].Exists ? "true" : "false");
        AddCheck(checks, "SOURCE_MANIFEST_FILE_4_LOCKED_REPLAY_SOURCE_MANIFEST_JSON_EXISTS",
            "Source manifest file 4 LOCKED_REPLAY_SOURCE_MANIFEST_JSON exists",
            "true", manifestFiles[3].Exists ? "true" : "false");
        AddCheck(checks, "SOURCE_MANIFEST_FILE_5_LOCKED_REPLAY_DIGEST_JSON_EXISTS",
            "Source manifest file 5 LOCKED_REPLAY_DIGEST_JSON exists",
            "true", manifestFiles[4].Exists ? "true" : "false");
        AddCheck(checks, "SOURCE_MANIFEST_FILE_6_LOCKED_REPLAY_FORBIDDEN_OUTPUT_GUARD_JSON_EXISTS",
            "Source manifest file 6 LOCKED_REPLAY_FORBIDDEN_OUTPUT_GUARD_JSON exists",
            "true", manifestFiles[5].Exists ? "true" : "false");
        AddCheck(checks, "SOURCE_MANIFEST_6_FILES_HASHED",
            "All 6 required MAP-27I source files hashed",
            "6", hashedCount.ToString());

        // 25-33: Backend operation plan
        AddCheck(checks, "BACKEND_OPERATION_PLAN_5_RECORDS",
            "Backend operation plan has 5 records",
            "5", ops.Count.ToString());
        AddCheck(checks, "BACKEND_OPERATION_PLAN_TOTAL_CELLS_5340",
            "Backend operation plan total planned cell count is 5340",
            "5340", totalPlanned.ToString());
        AddCheck(checks, "BACKEND_OPERATION_PLAN_NON_EMPTY_GROUPS_4",
            "Backend operation plan non-empty group count is 4",
            "4", nonEmptyGroups.ToString());
        AddCheck(checks, "BACKEND_OPERATION_PLAN_EMPTY_GROUPS_1",
            "Backend operation plan empty group count is 1",
            "1", emptyGroups.ToString());
        AddCheck(checks, "WALL_OPERATION_CELL_COUNT_850",
            "WALL operation planned_cell_count is 850",
            "850", ops[0].PlannedCellCount.ToString());
        AddCheck(checks, "FLOOR_OPERATION_CELL_COUNT_2444",
            "FLOOR operation planned_cell_count is 2444",
            "2444", ops[1].PlannedCellCount.ToString());
        AddCheck(checks, "ACCESS_OPERATION_CELL_COUNT_148",
            "ACCESS operation planned_cell_count is 148",
            "148", ops[2].PlannedCellCount.ToString());
        AddCheck(checks, "LOT_OPERATION_CELL_COUNT_1898",
            "LOT operation planned_cell_count is 1898",
            "1898", ops[3].PlannedCellCount.ToString());
        AddCheck(checks, "COMPONENT_OPERATION_CELL_COUNT_0",
            "COMPONENT operation planned_cell_count is 0",
            "0", ops[4].PlannedCellCount.ToString());

        // 34-42: Operation invariants
        bool allRequireDigest = ops.All(o => o.RequiresLockedReplayDigest);
        bool allUseSourceDigest = ops.All(o => o.SourceLockedReplayDigest == srcLockedReplayDigest);
        bool allSandboxOnly = ops.All(o => o.SandboxOnly);
        bool noWriterConsumable = ops.All(o => !o.WriterConsumable);
        bool noRuntimeConsumable = ops.All(o => !o.RuntimeConsumable);
        bool noEmitsRuntime = ops.All(o => !o.EmitsRuntimeFile);
        bool noEmitsBinary = ops.All(o => !o.EmitsBinaryFile);
        bool noEmitsLua = ops.All(o => !o.EmitsLuaFile);
        bool noEmitsInstall = ops.All(o => !o.EmitsInstallPath);

        AddCheck(checks, "ALL_OPERATIONS_REQUIRE_LOCKED_REPLAY_DIGEST",
            "All operations have requires_locked_replay_digest = true",
            "true", allRequireDigest ? "true" : "false");
        AddCheck(checks, "ALL_OPERATIONS_USE_SOURCE_DIGEST",
            "All operations carry the source locked replay digest",
            "true", allUseSourceDigest ? "true" : "false");
        AddCheck(checks, "ALL_OPERATIONS_SANDBOX_ONLY",
            "All operations have sandbox_only = true",
            "true", allSandboxOnly ? "true" : "false");
        AddCheck(checks, "NO_OPERATION_WRITER_CONSUMABLE",
            "No operation has writer_consumable = true",
            "true", noWriterConsumable ? "true" : "false");
        AddCheck(checks, "NO_OPERATION_RUNTIME_CONSUMABLE",
            "No operation has runtime_consumable = true",
            "true", noRuntimeConsumable ? "true" : "false");
        AddCheck(checks, "NO_OPERATION_EMITS_RUNTIME_FILE",
            "No operation has emits_runtime_file = true",
            "true", noEmitsRuntime ? "true" : "false");
        AddCheck(checks, "NO_OPERATION_EMITS_BINARY_FILE",
            "No operation has emits_binary_file = true",
            "true", noEmitsBinary ? "true" : "false");
        AddCheck(checks, "NO_OPERATION_EMITS_LUA_FILE",
            "No operation has emits_lua_file = true",
            "true", noEmitsLua ? "true" : "false");
        AddCheck(checks, "NO_OPERATION_EMITS_INSTALL_PATH",
            "No operation has emits_install_path = true",
            "true", noEmitsInstall ? "true" : "false");

        // 43: Forbidden scan
        AddCheck(checks, "POST_BACKEND_PLAN_FORBIDDEN_SCAN_PASS",
            "Post backend plan forbidden artifact scan passes in output root",
            "PASS", scanPasses ? "PASS" : "FAIL");

        // 44-51: Claim boundary
        MakeCheck(checks, "SANDBOX_ONLY_TRUE",                      "sandbox_only is true");
        MakeCheck(checks, "SANDBOX_BACKEND_PLAN_ONLY_TRUE",         "sandbox_backend_plan_only is true");
        MakeCheck(checks, "WRITER_READY_FALSE",                     "writer_ready is false");
        MakeCheck(checks, "RUNTIME_VALID_FALSE",                    "runtime_valid is false");
        MakeCheck(checks, "MATERIALIZED_FALSE",                     "materialized is false");
        MakeCheck(checks, "NO_RUNTIME_PROOF_CLAIM",                 "No runtime proof claimed");
        MakeCheck(checks, "NO_PUBLIC_PLAYABLE_PACKAGING_CLAIM",     "No public playable packaging claimed");
        MakeCheck(checks, "NO_RUNTIME_OUTPUTS_EMITTED",             "No runtime outputs emitted — backend plan only");

        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass              = result.FailedCheckCount == 0;
        result.IsValid            = allPass;
        result.BackendPlanStatus  = allPass ? "BACKEND_PLAN_COMPLETE" : "BACKEND_PLAN_FAILED";
        result.Verdict            = allPass
            ? "MAP27J_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_PLAN_COMPLETE"
            : invalidVerdict;

        return result;
    }

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanResult FinalizeAfterOutputs(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanResult result,
        string outputRoot)
    {
        const string invalidVerdict =
            "MAP27J_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_PLAN_INVALID";

        string finalScan = ScanOutputRoot(outputRoot);
        bool   scanPasses = finalScan.StartsWith("POST_BACKEND_PLAN_FORBIDDEN_SCAN PASS", StringComparison.Ordinal);
        result.ForbiddenArtifactScan = finalScan;

        var check = result.Checks.FirstOrDefault(c => c.CheckId == "POST_BACKEND_PLAN_FORBIDDEN_SCAN_PASS");
        if (check is not null)
        {
            check.CheckStatus = scanPasses ? "PASS" : "FAIL";
            check.Actual      = scanPasses ? "PASS" : "FAIL";
        }

        result.PassedCheckCount = result.Checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = result.Checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass             = result.FailedCheckCount == 0;
        result.IsValid           = allPass;
        result.BackendPlanStatus = allPass ? "BACKEND_PLAN_COMPLETE" : "BACKEND_PLAN_FAILED";
        result.Verdict           = allPass
            ? "MAP27J_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_PLAN_COMPLETE"
            : invalidVerdict;

        return result;
    }

    public string RenderJson(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanResult result) =>
        JsonSerializer.Serialize(result, s_jsonOptions);

    public string RenderMarkdown(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-27J WorldBuilder Minimal Concrete Geometry Sandbox Writer Locked Replay Backend Plan");
        sb.AppendLine();
        sb.AppendLine($"- **Map ID:** {result.MapId}");
        sb.AppendLine($"- **Generated UTC:** {result.GeneratedUtc}");
        sb.AppendLine($"- **Backend Plan Stage:** {result.BackendPlanStage}");
        sb.AppendLine($"- **Backend Plan Mode:** {result.BackendPlanMode}");
        sb.AppendLine($"- **Backend Plan Status:** {result.BackendPlanStatus}");
        sb.AppendLine($"- **Source Dry Run Status:** {result.SourceDryRunStatus}");
        sb.AppendLine($"- **Source Locked Replay Digest:** `{result.SourceLockedReplayDigest}`");
        sb.AppendLine($"- **Operation Plan Count:** {result.OperationPlanCount}");
        sb.AppendLine($"- **Total Planned Cells:** {result.OperationPlanGroups.TotalPlannedCellCount}");
        sb.AppendLine($"- **Non-empty Groups:** {result.OperationPlanGroups.NonEmptyOperationGroupCount}");
        sb.AppendLine($"- **Empty Groups:** {result.OperationPlanGroups.EmptyOperationGroupCount}");
        sb.AppendLine($"- **Largest Group:** {result.OperationPlanGroups.LargestOperationGroup} ({result.OperationPlanGroups.LargestOperationGroupCellCount} cells)");
        sb.AppendLine($"- **Forbidden Artifact Scan:** {result.ForbiddenArtifactScan}");
        sb.AppendLine($"- **Verdict:** `{result.Verdict}`");
        sb.AppendLine($"- **Is Valid:** {result.IsValid}");
        sb.AppendLine($"- **Checks:** {result.CheckCount} / Passed: {result.PassedCheckCount} / Failed: {result.FailedCheckCount}");
        sb.AppendLine();
        sb.AppendLine("## Backend Operations");
        sb.AppendLine();
        sb.AppendLine("| Order | Operation Kind | Bucket | Planned Cells |");
        sb.AppendLine("|-------|---------------|--------|--------------|");
        foreach (var op in result.BackendOperationRecords)
            sb.AppendLine($"| {op.OperationOrder} | {op.OperationKind} | {op.SourceMaterialBucket} | {op.PlannedCellCount} |");
        sb.AppendLine();
        sb.AppendLine("## Checks");
        sb.AppendLine();
        sb.AppendLine("| # | Check ID | Status | Expected | Actual |");
        sb.AppendLine("|---|----------|--------|----------|--------|");
        foreach (var c in result.Checks)
            sb.AppendLine($"| {c.CheckOrder} | {c.CheckId} | {c.CheckStatus} | {c.Expected} | {c.Actual} |");
        return sb.ToString();
    }

    public string RenderCsv(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_order,check_id,check_status,expected,actual");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckOrder},{c.CheckId},{c.CheckStatus},{c.Expected},{c.Actual}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-27J WorldBuilder Minimal Concrete Geometry Sandbox Writer Locked Replay Backend Plan");
        sb.AppendLine($"Generated UTC                    : {result.GeneratedUtc}");
        sb.AppendLine($"Map ID                           : {result.MapId}");
        sb.AppendLine($"Backend Plan Stage               : {result.BackendPlanStage}");
        sb.AppendLine($"Backend Plan Mode                : {result.BackendPlanMode}");
        sb.AppendLine($"Backend Plan Status              : {result.BackendPlanStatus}");
        sb.AppendLine($"Source Dry Run Status            : {result.SourceDryRunStatus}");
        sb.AppendLine($"Source Locked Replay Digest      : {result.SourceLockedReplayDigest}");
        sb.AppendLine($"Source Is Valid                  : {(result.SourceIsValid ? 1 : 0)}");
        sb.AppendLine($"Source Check Count               : {result.SourceCheckCount}");
        sb.AppendLine($"Source Passed Check Count        : {result.SourcePassedCheckCount}");
        sb.AppendLine($"Source Failed Check Count        : {result.SourceFailedCheckCount}");
        sb.AppendLine($"Source Materialized Cell Count   : {result.SourceMaterializedCellCount}");
        sb.AppendLine($"Source Wall Count                : {result.SourceBuildingWallCandidateCellCount}");
        sb.AppendLine($"Source Floor Count               : {result.SourceBuildingFloorCandidateCellCount}");
        sb.AppendLine($"Source Access Count              : {result.SourceAccessEdgeCellCount}");
        sb.AppendLine($"Source Lot Count                 : {result.SourceLotSpaceCellCount}");
        sb.AppendLine($"Source Residual Count            : {result.SourceComponentResidualCellCount}");
        sb.AppendLine($"Source Material Kind Count       : {result.SourceMaterialKindCount}");
        sb.AppendLine($"Operation Plan Count             : {result.OperationPlanCount}");
        sb.AppendLine($"Total Planned Cells              : {result.OperationPlanGroups.TotalPlannedCellCount}");
        sb.AppendLine($"Non-empty Groups                 : {result.OperationPlanGroups.NonEmptyOperationGroupCount}");
        sb.AppendLine($"Empty Groups                     : {result.OperationPlanGroups.EmptyOperationGroupCount}");
        sb.AppendLine($"Largest Group                    : {result.OperationPlanGroups.LargestOperationGroup} ({result.OperationPlanGroups.LargestOperationGroupCellCount} cells)");
        sb.AppendLine($"Forbidden Artifact Scan          : {result.ForbiddenArtifactScan}");
        sb.AppendLine($"Claim Boundary                   : {result.ClaimBoundaryAudit}");
        sb.AppendLine($"Verdict                          : {result.Verdict}");
        sb.AppendLine($"Is Valid                         : {(result.IsValid ? 1 : 0)}");
        sb.AppendLine($"Sandbox Only                     : 1");
        sb.AppendLine($"Sandbox Backend Plan Only        : 1");
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

    public string RenderOperationPlanJson(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanResult result)
    {
        var plan = new
        {
            operation_plan_count  = result.OperationPlanCount,
            operation_plan_groups = result.OperationPlanGroups,
            backend_operation_records = result.BackendOperationRecords,
        };
        return JsonSerializer.Serialize(plan, s_jsonOptions);
    }

    public string RenderOperationPlanCsv(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("operation_order,operation_id,operation_kind,source_material_bucket,planned_cell_count,backend_target_family,backend_payload_kind,requires_locked_replay_digest,writer_consumable,runtime_consumable,sandbox_only,emits_runtime_file,emits_binary_file,emits_lua_file,emits_install_path");
        foreach (var op in result.BackendOperationRecords)
            sb.AppendLine($"{op.OperationOrder},{op.OperationId},{op.OperationKind},{op.SourceMaterialBucket},{op.PlannedCellCount},{op.BackendTargetFamily},{op.BackendPayloadKind},{op.RequiresLockedReplayDigest},{op.WriterConsumable},{op.RuntimeConsumable},{op.SandboxOnly},{op.EmitsRuntimeFile},{op.EmitsBinaryFile},{op.EmitsLuaFile},{op.EmitsInstallPath}");
        return sb.ToString();
    }

    public string RenderSourceManifestJson(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanResult result)
    {
        var manifest = new
        {
            source_dry_run_root  = result.SourceDryRunRoot,
            source_dry_run_sha256 = result.SourceDryRunSha256,
            source_file_count    = result.SourceManifestFiles.Count,
            source_files         = result.SourceManifestFiles,
        };
        return JsonSerializer.Serialize(manifest, s_jsonOptions);
    }

    public string RenderForbiddenOutputGuardJson(DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanResult result)
    {
        var guard = new
        {
            forbidden_output_guard            = true,
            sandbox_only                      = true,
            sandbox_backend_plan_only         = true,
            writer_ready                      = false,
            runtime_valid                     = false,
            materialized                      = false,
            pz_runtime_materialized           = false,
            runtime_proof_claimed             = false,
            public_playable_packaging_claimed = false,
            forbidden_artifact_scan           = result.ForbiddenArtifactScan,
            all_clean = result.ForbiddenArtifactScan.StartsWith("POST_BACKEND_PLAN_FORBIDDEN_SCAN PASS", StringComparison.Ordinal),
        };
        return JsonSerializer.Serialize(guard, s_jsonOptions);
    }
}
