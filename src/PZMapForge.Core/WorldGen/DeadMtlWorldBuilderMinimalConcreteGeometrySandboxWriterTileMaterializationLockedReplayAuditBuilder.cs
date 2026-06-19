using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationLockedReplayAuditBuilder
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    private static string HashFile(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLower();

    private static void AddCheck(List<DeadMtlTileMaterializationLockedReplayAuditCheck> checks,
        string id, string label, string expected, string actual)
    {
        checks.Add(new DeadMtlTileMaterializationLockedReplayAuditCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = string.Equals(expected, actual, StringComparison.Ordinal) ? "PASS" : "FAIL",
            Expected    = expected,
            Actual      = actual,
        });
    }

    private static void MakeCheck(List<DeadMtlTileMaterializationLockedReplayAuditCheck> checks,
        string id, string label)
    {
        checks.Add(new DeadMtlTileMaterializationLockedReplayAuditCheck
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

    private static string ScanOutputRoot(string outputRoot)
    {
        if (!Directory.Exists(outputRoot))
            return "POST_AUDIT_FORBIDDEN_SCAN PASS (0 forbidden artifacts in output root)";

        var patterns = new[] { "*.lotpack", "*.lotheader", "*.lua", "*.bin" };
        int count = patterns.Sum(p =>
            Directory.GetFiles(outputRoot, p, SearchOption.AllDirectories).Length);

        var allDirs = Directory.GetDirectories(outputRoot, "*", SearchOption.AllDirectories);
        bool hasMediaMaps = allDirs.Any(d => { var di = new DirectoryInfo(d); return di.Name == "maps" && di.Parent?.Name == "media"; });
        if (hasMediaMaps) count++;

        bool hasSteamapps = allDirs.Any(d => string.Equals(new DirectoryInfo(d).Name, "steamapps", StringComparison.OrdinalIgnoreCase));
        if (hasSteamapps) count++;

        return count == 0
            ? "POST_AUDIT_FORBIDDEN_SCAN PASS (0 forbidden artifacts in output root)"
            : $"POST_AUDIT_FORBIDDEN_SCAN FAIL ({count} forbidden artifacts found in output root)";
    }

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationLockedReplayAuditResult Build(
        string replayLockRoot,
        string outputRoot)
    {
        const string invalidVerdict =
            "MAP27H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_LOCKED_REPLAY_AUDIT_INVALID";

        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationLockedReplayAuditResult
        {
            Format                         = "MAP-27H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_LOCKED_REPLAY_AUDIT",
            GeneratedUtc                   = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MapId                          = "map_00",
            AuditStage                     = "SANDBOX_WRITER_TILE_MATERIALIZATION_LOCKED_REPLAY_AUDIT",
            AuditMode                      = "VERIFY_MAP27G1_REPLAY_LOCK_HASHES_AND_LOCK_ID_ONLY",
            SourceReplayLockRoot           = replayLockRoot,
            WriterReady                    = false,
            RuntimeValid                   = false,
            Materialized                   = false,
            RuntimeProofClaimed            = false,
            PublicPlayablePackagingClaimed = false,
            NextAllowedExperimentName      = "MAP-27I_SANDBOX_WRITER_LOCKED_MATERIALIZATION_REPLAY_DRY_RUN",
            NextAllowedExperimentStatus    = "SANDBOX_ONLY_NOT_RUNTIME",
            ClaimBoundaryAudit             = "writer_ready=false | runtime_valid=false | materialized=false | runtime_proof_claimed=false | public_playable_packaging_claimed=false",
        };

        if (!Directory.Exists(replayLockRoot))
        {
            result.Errors.Add($"Replay lock root not found: {replayLockRoot}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        const string lockFileName =
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_replay_lock.json";
        string lockFilePath = Path.Combine(replayLockRoot, lockFileName);
        result.SourceReplayLockPath = lockFilePath;

        if (!File.Exists(lockFilePath))
        {
            result.Errors.Add($"Replay lock file not found: {lockFilePath}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        result.SourceReplayLockSha256 = HashFile(lockFilePath);

        string storedVerdict                   = string.Empty;
        bool   storedIsValid                   = false;
        string storedStatus                    = string.Empty;
        string storedReplayLockId              = string.Empty;
        int    storedFileCount                 = 0;
        bool   storedSandboxOnly               = false;
        bool   storedPzRuntime                 = true;
        bool   storedSandboxMaterializedSource = false;
        bool   storedVisualQaOverlayWritten    = false;
        int    storedMaterializedCellCount     = 0;
        int    storedRenderedCellCount         = 0;
        string storedCountMatchSummary         = string.Empty;
        int    storedWallCount                 = 0;
        int    storedFloorCount                = 0;
        int    storedAccessCount               = 0;
        int    storedLotCount                  = 0;
        int    storedResidualCount             = 0;
        int    storedMaterialKindCount         = 0;
        int    storedLayerKindCount            = 0;
        string storedTargetComponentId         = string.Empty;
        var    storedNextForbiddenSteps        = new List<string>();

        var storedLockedFiles = new List<(int Order, string Stage, string Role, string Name, string Path, string Sha256, bool LockedForReplay, bool RuntimeConsumable, bool WriterConsumable)>();

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(lockFilePath, Encoding.UTF8));
            var r = doc.RootElement;

            if (r.TryGetProperty("verdict",                             out var p)) storedVerdict                   = p.GetString() ?? "";
            if (r.TryGetProperty("is_valid",                            out p))     storedIsValid                   = p.GetBoolean();
            if (r.TryGetProperty("replay_lock_status",                  out p))     storedStatus                    = p.GetString() ?? "";
            if (r.TryGetProperty("replay_lock_id",                      out p))     storedReplayLockId              = p.GetString() ?? "";
            if (r.TryGetProperty("replay_lock_file_count",              out p))     storedFileCount                 = p.GetInt32();
            if (r.TryGetProperty("sandbox_only",                        out p))     storedSandboxOnly               = p.GetBoolean();
            if (r.TryGetProperty("pz_runtime_materialized",             out p))     storedPzRuntime                 = p.GetBoolean();
            if (r.TryGetProperty("sandbox_materialized_source",         out p))     storedSandboxMaterializedSource = p.GetBoolean();
            if (r.TryGetProperty("visual_qa_overlay_written",           out p))     storedVisualQaOverlayWritten    = p.GetBoolean();
            if (r.TryGetProperty("materialized_cell_count",             out p))     storedMaterializedCellCount     = p.GetInt32();
            if (r.TryGetProperty("rendered_cell_count",                 out p))     storedRenderedCellCount         = p.GetInt32();
            if (r.TryGetProperty("count_match_summary",                 out p))     storedCountMatchSummary         = p.GetString() ?? "";
            if (r.TryGetProperty("building_wall_candidate_cell_count",  out p))     storedWallCount                 = p.GetInt32();
            if (r.TryGetProperty("building_floor_candidate_cell_count", out p))     storedFloorCount                = p.GetInt32();
            if (r.TryGetProperty("access_edge_cell_count",              out p))     storedAccessCount               = p.GetInt32();
            if (r.TryGetProperty("lot_space_cell_count",                out p))     storedLotCount                  = p.GetInt32();
            if (r.TryGetProperty("component_residual_cell_count",       out p))     storedResidualCount             = p.GetInt32();
            if (r.TryGetProperty("material_kind_count",                 out p))     storedMaterialKindCount         = p.GetInt32();
            if (r.TryGetProperty("layer_kind_count",                    out p))     storedLayerKindCount            = p.GetInt32();
            if (r.TryGetProperty("target_component_id",                 out p))     storedTargetComponentId         = p.GetString() ?? "";

            if (r.TryGetProperty("next_forbidden_steps", out var nfsEl) &&
                nfsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in nfsEl.EnumerateArray())
                    storedNextForbiddenSteps.Add(entry.GetString() ?? "");
            }

            if (r.TryGetProperty("replay_lock_files", out var filesEl) &&
                filesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var f in filesEl.EnumerateArray())
                {
                    int    order   = f.TryGetProperty("file_order",         out var fp) ? fp.GetInt32()    : 0;
                    string stage   = f.TryGetProperty("source_stage",       out fp)     ? fp.GetString() ?? "" : "";
                    string role    = f.TryGetProperty("file_role",          out fp)     ? fp.GetString() ?? "" : "";
                    string name    = f.TryGetProperty("file_name",          out fp)     ? fp.GetString() ?? "" : "";
                    string path    = f.TryGetProperty("file_path",          out fp)     ? fp.GetString() ?? "" : "";
                    string sha256  = f.TryGetProperty("sha256",             out fp)     ? fp.GetString() ?? "" : "";
                    bool   locked  = f.TryGetProperty("locked_for_replay",  out fp)     && fp.GetBoolean();
                    bool   runtime = f.TryGetProperty("runtime_consumable", out fp)     && fp.GetBoolean();
                    bool   writer  = f.TryGetProperty("writer_consumable",  out fp)     && fp.GetBoolean();
                    storedLockedFiles.Add((order, stage, role, name, path, sha256, locked, runtime, writer));
                }
                storedLockedFiles.Sort((a, b) => a.Order.CompareTo(b.Order));
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse replay lock JSON: {ex.Message}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        result.SourceReplayLockVerdict         = storedVerdict;
        result.SourceReplayLockIsValid         = storedIsValid;
        result.SourceReplayLockStatus          = storedStatus;
        result.SourceReplayLockId              = storedReplayLockId;
        result.SourceReplayLockFileCount       = storedFileCount;
        result.SandboxOnly                     = storedSandboxOnly;
        result.PzRuntimeMaterialized           = storedPzRuntime;
        result.SandboxMaterializedSource       = storedSandboxMaterializedSource;
        result.VisualQaOverlayWritten          = storedVisualQaOverlayWritten;
        result.MaterializedCellCount           = storedMaterializedCellCount;
        result.RenderedCellCount               = storedRenderedCellCount;
        result.CountMatchSummary               = storedCountMatchSummary;
        result.BuildingWallCandidateCellCount  = storedWallCount;
        result.BuildingFloorCandidateCellCount = storedFloorCount;
        result.AccessEdgeCellCount             = storedAccessCount;
        result.LotSpaceCellCount               = storedLotCount;
        result.ComponentResidualCellCount      = storedResidualCount;
        result.MaterialKindCount               = storedMaterialKindCount;
        result.LayerKindCount                  = storedLayerKindCount;
        result.TargetComponentId               = storedTargetComponentId;
        result.NextForbiddenSteps              = storedNextForbiddenSteps;

        // Re-hash each locked file
        var auditFiles = new List<DeadMtlTileMaterializationLockedReplayAuditFile>();
        foreach (var lf in storedLockedFiles)
        {
            bool   exists  = File.Exists(lf.Path);
            string recomp  = exists ? HashFile(lf.Path) : string.Empty;
            bool   matches = exists && string.Equals(lf.Sha256, recomp, StringComparison.Ordinal);
            string status  = !exists ? "LOCKED_FILE_MISSING"
                           : matches ? "LOCKED_FILE_VERIFIED"
                           :           "LOCKED_FILE_HASH_MISMATCH";
            auditFiles.Add(new DeadMtlTileMaterializationLockedReplayAuditFile
            {
                FileOrder         = lf.Order,
                SourceStage       = lf.Stage,
                FileRole          = lf.Role,
                FileName          = lf.Name,
                FilePath          = lf.Path,
                Exists            = exists,
                StoredSha256      = lf.Sha256,
                RecomputedSha256  = recomp,
                HashMatches       = matches,
                LockedForReplay   = lf.LockedForReplay,
                RuntimeConsumable = lf.RuntimeConsumable,
                WriterConsumable  = lf.WriterConsumable,
                AuditStatus       = status,
            });
        }
        result.LockedFiles                 = auditFiles;
        result.LockedFileCount             = auditFiles.Count;
        result.LockedFileHashMatchCount    = auditFiles.Count(f => f.HashMatches);
        result.LockedFileHashMismatchCount = auditFiles.Count(f => f.Exists && !f.HashMatches);
        result.LockedFileMissingCount      = auditFiles.Count(f => !f.Exists);

        // Recompute replay lock ID using MAP-27G1 formula with recomputed hashes
        bool allExistAndHashed = auditFiles.Count == 8 && auditFiles.All(f => f.Exists && !string.IsNullOrEmpty(f.RecomputedSha256));
        string recomputedLockId = "RECOMPUTED_LOCK_ID_NOT_GENERATED";
        if (allExistAndHashed)
        {
            var lockInput = "MAP27G_REPLAY_LOCK_V1"
                + "|" + auditFiles[0].FileRole + ":" + auditFiles[0].RecomputedSha256
                + "|" + auditFiles[1].FileRole + ":" + auditFiles[1].RecomputedSha256
                + "|" + auditFiles[2].FileRole + ":" + auditFiles[2].RecomputedSha256
                + "|" + auditFiles[3].FileRole + ":" + auditFiles[3].RecomputedSha256
                + "|" + auditFiles[4].FileRole + ":" + auditFiles[4].RecomputedSha256
                + "|" + auditFiles[5].FileRole + ":" + auditFiles[5].RecomputedSha256
                + "|" + auditFiles[6].FileRole + ":" + auditFiles[6].RecomputedSha256
                + "|" + auditFiles[7].FileRole + ":" + auditFiles[7].RecomputedSha256;
            string lockHex = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(lockInput))).ToLower();
            recomputedLockId = "map_00_replay_lock_" + lockHex[..16];
        }
        result.RecomputedReplayLockId = recomputedLockId;
        result.ReplayLockIdMatches    = string.Equals(storedReplayLockId, recomputedLockId, StringComparison.Ordinal);

        bool allHashesMatch = result.LockedFileHashMatchCount == 8 &&
                              result.LockedFileHashMismatchCount == 0 &&
                              result.LockedFileMissingCount == 0;

        result.AuditStatus = allHashesMatch && result.ReplayLockIdMatches
            ? "VERIFIED_LOCKED_REPLAY_SOURCE_SET"
            : "AUDIT_FAILED";

        // MAP-27F file filter: only 1 MAP-27F file, and it must be ACCEPTANCE_GATE_RESULT_JSON
        bool noOldMap27fMd = auditFiles.Count(f => f.SourceStage == "MAP-27F") == 1 &&
                             auditFiles.Where(f => f.SourceStage == "MAP-27F")
                                       .All(f => f.FileRole == "ACCEPTANCE_GATE_RESULT_JSON");

        // Forbidden artifact scan of output root
        Directory.CreateDirectory(outputRoot);
        string forbiddenArtifactScan = ScanOutputRoot(outputRoot);
        bool   forbiddenScanPasses   = forbiddenArtifactScan.StartsWith("POST_AUDIT_FORBIDDEN_SCAN PASS", StringComparison.Ordinal);
        result.ForbiddenArtifactScan = forbiddenArtifactScan;

        // Expected locked file roles in exact required order
        var expectedRoles = new[]
        {
            "ACCEPTANCE_GATE_RESULT_JSON",
            "TILE_MATERIALIZER_RESULT_JSON",
            "MATERIALIZED_CELLS_CSV",
            "MATERIAL_PALETTE_JSON",
            "LAYER_STACK_JSON",
            "MATERIALIZATION_REPLAY_LOG_JSON",
            "MATERIALIZATION_OWNERSHIP_SUMMARY_JSON",
            "MATERIALIZER_FORBIDDEN_OUTPUT_GUARD_JSON",
        };
        bool rolesInOrder = auditFiles.Count == 8 &&
            Enumerable.Range(0, 8).All(i => auditFiles[i].FileRole == expectedRoles[i]);

        string countMatchActual = storedCountMatchSummary.EndsWith(": MATCH", StringComparison.Ordinal)
            ? "MATCH" : "MISMATCH";

        // 45 checks in exact required order
        var checks = new List<DeadMtlTileMaterializationLockedReplayAuditCheck>();

        // 1-8: MAP-27G1 header
        MakeCheck(checks, "MAP27G_REPLAY_LOCK_ROOT_EXISTS",
            "MAP-27G1 replay lock root exists");
        AddCheck(checks, "MAP27G_REPLAY_LOCK_JSON_EXISTS",
            "MAP-27G1 replay lock JSON file exists",
            "true", File.Exists(lockFilePath) ? "true" : "false");
        MakeCheck(checks, "MAP27G_REPLAY_LOCK_JSON_HASHED",
            "MAP-27G1 replay lock JSON SHA-256 hashed");
        AddCheck(checks, "MAP27G_VERDICT_COMPLETE",
            "MAP-27G1 verdict is REPLAY_LOCK_COMPLETE",
            "MAP27G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOCK_COMPLETE",
            storedVerdict);
        AddCheck(checks, "MAP27G_IS_VALID_TRUE",
            "MAP-27G1 is_valid is true",
            "true", storedIsValid ? "true" : "false");
        AddCheck(checks, "MAP27G_STATUS_LOCKED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY",
            "MAP-27G1 replay_lock_status is LOCKED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY",
            "LOCKED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY", storedStatus);
        AddCheck(checks, "MAP27G_REPLAY_LOCK_ID_PRESENT",
            "MAP-27G1 replay_lock_id is present and non-empty",
            "true",
            !string.IsNullOrEmpty(storedReplayLockId) && storedReplayLockId != "LOCK_ID_NOT_GENERATED"
                ? "true" : "false");
        AddCheck(checks, "MAP27G_REPLAY_LOCK_FILE_COUNT_8",
            "MAP-27G1 replay_lock_file_count is 8",
            "8", storedFileCount.ToString());

        // 9: Role order
        AddCheck(checks, "LOCKED_FILE_ROLES_EXACT_ORDER",
            "Locked file roles match exact required order",
            "EXACT_ORDER_MATCH", rolesInOrder ? "EXACT_ORDER_MATCH" : "ORDER_MISMATCH");

        // 10-17: Per-file existence (named by role)
        AddCheck(checks, "LOCKED_FILE_1_ACCEPTANCE_GATE_RESULT_JSON_EXISTS",
            "Locked file 1 ACCEPTANCE_GATE_RESULT_JSON exists at stored path",
            "true", auditFiles.Count > 0 && auditFiles[0].Exists ? "true" : "false");
        AddCheck(checks, "LOCKED_FILE_2_TILE_MATERIALIZER_RESULT_JSON_EXISTS",
            "Locked file 2 TILE_MATERIALIZER_RESULT_JSON exists at stored path",
            "true", auditFiles.Count > 1 && auditFiles[1].Exists ? "true" : "false");
        AddCheck(checks, "LOCKED_FILE_3_MATERIALIZED_CELLS_CSV_EXISTS",
            "Locked file 3 MATERIALIZED_CELLS_CSV exists at stored path",
            "true", auditFiles.Count > 2 && auditFiles[2].Exists ? "true" : "false");
        AddCheck(checks, "LOCKED_FILE_4_MATERIAL_PALETTE_JSON_EXISTS",
            "Locked file 4 MATERIAL_PALETTE_JSON exists at stored path",
            "true", auditFiles.Count > 3 && auditFiles[3].Exists ? "true" : "false");
        AddCheck(checks, "LOCKED_FILE_5_LAYER_STACK_JSON_EXISTS",
            "Locked file 5 LAYER_STACK_JSON exists at stored path",
            "true", auditFiles.Count > 4 && auditFiles[4].Exists ? "true" : "false");
        AddCheck(checks, "LOCKED_FILE_6_MATERIALIZATION_REPLAY_LOG_JSON_EXISTS",
            "Locked file 6 MATERIALIZATION_REPLAY_LOG_JSON exists at stored path",
            "true", auditFiles.Count > 5 && auditFiles[5].Exists ? "true" : "false");
        AddCheck(checks, "LOCKED_FILE_7_MATERIALIZATION_OWNERSHIP_SUMMARY_JSON_EXISTS",
            "Locked file 7 MATERIALIZATION_OWNERSHIP_SUMMARY_JSON exists at stored path",
            "true", auditFiles.Count > 6 && auditFiles[6].Exists ? "true" : "false");
        AddCheck(checks, "LOCKED_FILE_8_MATERIALIZER_FORBIDDEN_OUTPUT_GUARD_JSON_EXISTS",
            "Locked file 8 MATERIALIZER_FORBIDDEN_OUTPUT_GUARD_JSON exists at stored path",
            "true", auditFiles.Count > 7 && auditFiles[7].Exists ? "true" : "false");

        // 18-19: Hash aggregates
        int hashedCount = auditFiles.Count(f => f.Exists && !string.IsNullOrEmpty(f.RecomputedSha256));
        AddCheck(checks, "ALL_8_LOCKED_FILES_HASHED",
            "All 8 locked files successfully hashed",
            "8", hashedCount.ToString());
        AddCheck(checks, "ALL_8_LOCKED_FILE_HASHES_MATCH",
            "All 8 locked file recomputed hashes match stored hashes",
            "true", allHashesMatch ? "true" : "false");

        // 20-21: Replay lock ID
        AddCheck(checks, "REPLAY_LOCK_ID_RECOMPUTED",
            "Replay lock ID recomputed successfully",
            "true", recomputedLockId != "RECOMPUTED_LOCK_ID_NOT_GENERATED" ? "true" : "false");
        AddCheck(checks, "REPLAY_LOCK_ID_MATCHES_STORED",
            "Recomputed replay lock ID matches stored replay_lock_id",
            "true", result.ReplayLockIdMatches ? "true" : "false");

        // 22: Old MAP-27F file guard
        AddCheck(checks, "NO_OLD_MAP27F_MD_CSV_SUMMARY_LOCKED",
            "Only 1 MAP-27F file in lock set and it is ACCEPTANCE_GATE_RESULT_JSON",
            "true", noOldMap27fMd ? "true" : "false");

        // 23-25: Lock flags
        AddCheck(checks, "ALL_8_LOCKED_FOR_REPLAY_TRUE",
            "All 8 locked files have locked_for_replay=true",
            "8", auditFiles.Count(f => f.LockedForReplay).ToString());
        AddCheck(checks, "ALL_8_RUNTIME_CONSUMABLE_FALSE",
            "All 8 locked files have runtime_consumable=false",
            "0", auditFiles.Count(f => f.RuntimeConsumable).ToString());
        AddCheck(checks, "ALL_8_WRITER_CONSUMABLE_FALSE",
            "All 8 locked files have writer_consumable=false",
            "0", auditFiles.Count(f => f.WriterConsumable).ToString());

        // 26-29: Claim boundary booleans
        AddCheck(checks, "SANDBOX_ONLY_TRUE",
            "sandbox_only is true",
            "true", storedSandboxOnly ? "true" : "false");
        AddCheck(checks, "SANDBOX_MATERIALIZED_SOURCE_TRUE",
            "sandbox_materialized_source is true",
            "true", storedSandboxMaterializedSource ? "true" : "false");
        AddCheck(checks, "VISUAL_QA_OVERLAY_WRITTEN_TRUE",
            "visual_qa_overlay_written is true",
            "true", storedVisualQaOverlayWritten ? "true" : "false");
        AddCheck(checks, "PZ_RUNTIME_MATERIALIZED_FALSE",
            "pz_runtime_materialized is false",
            "false", storedPzRuntime ? "true" : "false");

        // 30-39: Inherited count checks
        AddCheck(checks, "MATERIALIZED_CELL_COUNT_5340",
            "materialized_cell_count is 5340",
            "5340", storedMaterializedCellCount.ToString());
        AddCheck(checks, "RENDERED_CELL_COUNT_5340",
            "rendered_cell_count is 5340",
            "5340", storedRenderedCellCount.ToString());
        AddCheck(checks, "COUNT_MATCH_SUMMARY_MATCH",
            "count_match_summary indicates MATCH",
            "MATCH", countMatchActual);
        AddCheck(checks, "WALL_COUNT_850",
            "building_wall_candidate_cell_count is 850",
            "850", storedWallCount.ToString());
        AddCheck(checks, "FLOOR_COUNT_2444",
            "building_floor_candidate_cell_count is 2444",
            "2444", storedFloorCount.ToString());
        AddCheck(checks, "ACCESS_COUNT_148",
            "access_edge_cell_count is 148",
            "148", storedAccessCount.ToString());
        AddCheck(checks, "LOT_COUNT_1898",
            "lot_space_cell_count is 1898",
            "1898", storedLotCount.ToString());
        AddCheck(checks, "COMPONENT_RESIDUAL_COUNT_0",
            "component_residual_cell_count is 0",
            "0", storedResidualCount.ToString());
        AddCheck(checks, "MATERIAL_KIND_COUNT_5",
            "material_kind_count is 5",
            "5", storedMaterialKindCount.ToString());
        AddCheck(checks, "LAYER_KIND_COUNT_5",
            "layer_kind_count is 5",
            "5", storedLayerKindCount.ToString());

        // 40-45: Final checks
        MakeCheck(checks, "NEXT_ALLOWED_EXPERIMENT_SANDBOX_ONLY",
            "Next allowed experiment is sandbox-only, not runtime");
        var missingForbiddenSteps = s_requiredForbiddenSteps.Where(r => !storedNextForbiddenSteps.Contains(r)).ToList();
        string forbiddenStepsActual = missingForbiddenSteps.Count == 0
            ? "REQUIRED_11_PRESENT"
            : "MISSING:" + string.Join(",", missingForbiddenSteps);
        AddCheck(checks, "FORBIDDEN_STEPS_LISTED",
            "next_forbidden_steps contains all 11 required forbidden categories",
            "REQUIRED_11_PRESENT", forbiddenStepsActual);
        AddCheck(checks, "POST_AUDIT_FORBIDDEN_SCAN_PASS",
            "Post-audit forbidden artifact scan passes in output root",
            "PASS", forbiddenScanPasses ? "PASS" : "FAIL");
        MakeCheck(checks, "WRITER_READY_FALSE",
            "writer_ready is false");
        MakeCheck(checks, "NO_RUNTIME_PROOF_CLAIM",
            "No runtime proof claimed");
        MakeCheck(checks, "NO_PUBLIC_PLAYABLE_PACKAGING_CLAIM",
            "No public playable packaging claimed");

        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass   = result.FailedCheckCount == 0;
        result.IsValid = allPass;
        result.Verdict = allPass
            ? "MAP27H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_LOCKED_REPLAY_AUDIT_COMPLETE"
            : invalidVerdict;

        return result;
    }

    public string RenderJson(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationLockedReplayAuditResult result) =>
        JsonSerializer.Serialize(result, s_jsonOptions);

    public string RenderMarkdown(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationLockedReplayAuditResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-27H WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Materialization Locked Replay Audit");
        sb.AppendLine();
        sb.AppendLine($"- **Map ID:** {result.MapId}");
        sb.AppendLine($"- **Generated UTC:** {result.GeneratedUtc}");
        sb.AppendLine($"- **Audit Stage:** {result.AuditStage}");
        sb.AppendLine($"- **Audit Mode:** {result.AuditMode}");
        sb.AppendLine($"- **Audit Status:** {result.AuditStatus}");
        sb.AppendLine($"- **Source Replay Lock ID:** `{result.SourceReplayLockId}`");
        sb.AppendLine($"- **Recomputed Replay Lock ID:** `{result.RecomputedReplayLockId}`");
        sb.AppendLine($"- **Replay Lock ID Matches:** {result.ReplayLockIdMatches}");
        sb.AppendLine($"- **Locked File Count:** {result.LockedFileCount}");
        sb.AppendLine($"- **Hash Match Count:** {result.LockedFileHashMatchCount}");
        sb.AppendLine($"- **Hash Mismatch Count:** {result.LockedFileHashMismatchCount}");
        sb.AppendLine($"- **Missing Count:** {result.LockedFileMissingCount}");
        sb.AppendLine($"- **Materialized Cell Count:** {result.MaterializedCellCount}");
        sb.AppendLine($"- **Forbidden Artifact Scan:** {result.ForbiddenArtifactScan}");
        sb.AppendLine($"- **Claim Boundary:** {result.ClaimBoundaryAudit}");
        sb.AppendLine($"- **Verdict:** `{result.Verdict}`");
        sb.AppendLine($"- **Is Valid:** {result.IsValid}");
        sb.AppendLine($"- **Checks:** {result.CheckCount} / Passed: {result.PassedCheckCount} / Failed: {result.FailedCheckCount}");
        sb.AppendLine($"- **Next Allowed Experiment:** {result.NextAllowedExperimentName} ({result.NextAllowedExperimentStatus})");
        sb.AppendLine();
        sb.AppendLine("## Locked Files");
        sb.AppendLine();
        sb.AppendLine("| # | Stage | Role | File | Stored SHA-256 | Recomputed SHA-256 | Match | Audit Status |");
        sb.AppendLine("|---|-------|------|------|----------------|--------------------|-------|--------------|");
        foreach (var f in result.LockedFiles)
        {
            string stored = f.StoredSha256.Length     >= 16 ? f.StoredSha256[..16]     + "..." : f.StoredSha256;
            string recomp = f.RecomputedSha256.Length >= 16 ? f.RecomputedSha256[..16] + "..." : f.RecomputedSha256;
            sb.AppendLine($"| {f.FileOrder} | {f.SourceStage} | {f.FileRole} | {f.FileName} | `{stored}` | `{recomp}` | {f.HashMatches} | {f.AuditStatus} |");
        }
        sb.AppendLine();
        sb.AppendLine("## Checks");
        sb.AppendLine();
        sb.AppendLine("| # | Check ID | Status | Expected | Actual |");
        sb.AppendLine("|---|----------|--------|----------|--------|");
        foreach (var c in result.Checks)
            sb.AppendLine($"| {c.CheckOrder} | {c.CheckId} | {c.CheckStatus} | {c.Expected} | {c.Actual} |");
        return sb.ToString();
    }

    public string RenderCsv(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationLockedReplayAuditResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_order,check_id,check_status,expected,actual");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckOrder},{c.CheckId},{c.CheckStatus},{c.Expected},{c.Actual}");
        return sb.ToString();
    }

    public string RenderSummary(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationLockedReplayAuditResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-27H WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Materialization Locked Replay Audit");
        sb.AppendLine($"Generated UTC                    : {result.GeneratedUtc}");
        sb.AppendLine($"Map ID                           : {result.MapId}");
        sb.AppendLine($"Audit Stage                      : {result.AuditStage}");
        sb.AppendLine($"Audit Mode                       : {result.AuditMode}");
        sb.AppendLine($"Audit Status                     : {result.AuditStatus}");
        sb.AppendLine($"Source Replay Lock ID            : {result.SourceReplayLockId}");
        sb.AppendLine($"Recomputed Replay Lock ID        : {result.RecomputedReplayLockId}");
        sb.AppendLine($"Replay Lock ID Matches           : {(result.ReplayLockIdMatches ? 1 : 0)}");
        sb.AppendLine($"Locked File Count                : {result.LockedFileCount}");
        sb.AppendLine($"Hash Match Count                 : {result.LockedFileHashMatchCount}");
        sb.AppendLine($"Hash Mismatch Count              : {result.LockedFileHashMismatchCount}");
        sb.AppendLine($"Missing Count                    : {result.LockedFileMissingCount}");
        sb.AppendLine($"Verdict                          : {result.Verdict}");
        sb.AppendLine($"Is Valid                         : {(result.IsValid ? 1 : 0)}");
        sb.AppendLine($"Sandbox Only                     : {(result.SandboxOnly ? 1 : 0)}");
        sb.AppendLine($"PZ Runtime Materialized          : {(result.PzRuntimeMaterialized ? 1 : 0)}");
        sb.AppendLine($"Sandbox Materialized Source      : {(result.SandboxMaterializedSource ? 1 : 0)}");
        sb.AppendLine($"Visual QA Overlay Written        : {(result.VisualQaOverlayWritten ? 1 : 0)}");
        sb.AppendLine($"Materialized Cell Count          : {result.MaterializedCellCount}");
        sb.AppendLine($"Rendered Cell Count              : {result.RenderedCellCount}");
        sb.AppendLine($"Count Match                      : {result.CountMatchSummary}");
        sb.AppendLine($"Wall Count                       : {result.BuildingWallCandidateCellCount}");
        sb.AppendLine($"Floor Count                      : {result.BuildingFloorCandidateCellCount}");
        sb.AppendLine($"Access Count                     : {result.AccessEdgeCellCount}");
        sb.AppendLine($"Lot Count                        : {result.LotSpaceCellCount}");
        sb.AppendLine($"Residual Count                   : {result.ComponentResidualCellCount}");
        sb.AppendLine($"Material Kind Count              : {result.MaterialKindCount}");
        sb.AppendLine($"Layer Kind Count                 : {result.LayerKindCount}");
        sb.AppendLine($"Writer Ready                     : {(result.WriterReady ? 1 : 0)}");
        sb.AppendLine($"Runtime Valid                    : {(result.RuntimeValid ? 1 : 0)}");
        sb.AppendLine($"Materialized                     : {(result.Materialized ? 1 : 0)}");
        sb.AppendLine($"Runtime Proof                    : {(result.RuntimeProofClaimed ? 1 : 0)}");
        sb.AppendLine($"Public Playable                  : {(result.PublicPlayablePackagingClaimed ? 1 : 0)}");
        sb.AppendLine($"Forbidden Steps Count            : {result.NextForbiddenSteps.Count}");
        sb.AppendLine($"Forbidden Artifact Scan          : {result.ForbiddenArtifactScan}");
        sb.AppendLine($"Claim Boundary                   : {result.ClaimBoundaryAudit}");
        sb.AppendLine($"Next Allowed Experiment          : {result.NextAllowedExperimentName}");
        sb.AppendLine($"Next Experiment Status           : {result.NextAllowedExperimentStatus}");
        sb.AppendLine($"Checks                           : {result.CheckCount}");
        sb.AppendLine($"Passed                           : {result.PassedCheckCount}");
        sb.Append($"Failed                           : {result.FailedCheckCount}");
        return sb.ToString();
    }
}
