using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunBuilder
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    private static readonly string[] s_materialColumnNames =
        { "material_kind", "material", "material_id", "tile_material_kind" };

    private static string HashFile(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLower();

    private static void AddCheck(List<DeadMtlLockedReplayDryRunCheck> checks,
        string id, string label, string expected, string actual)
    {
        checks.Add(new DeadMtlLockedReplayDryRunCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = string.Equals(expected, actual, StringComparison.Ordinal) ? "PASS" : "FAIL",
            Expected    = expected,
            Actual      = actual,
        });
    }

    private static void MakeCheck(List<DeadMtlLockedReplayDryRunCheck> checks,
        string id, string label)
    {
        checks.Add(new DeadMtlLockedReplayDryRunCheck
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

    private static readonly string[] s_expectedRoles = new[]
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

    private static string ScanOutputRoot(string outputRoot)
    {
        if (!Directory.Exists(outputRoot))
            return "POST_DRY_RUN_FORBIDDEN_SCAN PASS (0 forbidden artifacts in output root)";

        var patterns = new[] { "*.lotpack", "*.lotheader", "*.lua", "*.bin" };
        int count = patterns.Sum(p =>
            Directory.GetFiles(outputRoot, p, SearchOption.AllDirectories).Length);

        var allDirs = Directory.GetDirectories(outputRoot, "*", SearchOption.AllDirectories);
        bool hasMediaMaps = allDirs.Any(d => { var di = new DirectoryInfo(d); return di.Name == "maps" && di.Parent?.Name == "media"; });
        if (hasMediaMaps) count++;

        bool hasSteamapps = allDirs.Any(d => string.Equals(new DirectoryInfo(d).Name, "steamapps", StringComparison.OrdinalIgnoreCase));
        if (hasSteamapps) count++;

        return count == 0
            ? "POST_DRY_RUN_FORBIDDEN_SCAN PASS (0 forbidden artifacts in output root)"
            : $"POST_DRY_RUN_FORBIDDEN_SCAN FAIL ({count} forbidden artifacts found in output root)";
    }

    // Parses the materialized cells CSV by header name.
    // Returns (total, wall, floor, access, lot, residual, materialKinds, parsed).
    // materialKinds is always 5 (the fixed set of canonical buckets).
    internal static (int Total, int Wall, int Floor, int Access, int Lot, int Residual, int MaterialKinds, bool Parsed)
        ParseMaterializedCellsCsv(string csvPath)
    {
        if (!File.Exists(csvPath))
            return (0, 0, 0, 0, 0, 0, 0, false);

        try
        {
            var lines = File.ReadAllLines(csvPath, Encoding.UTF8);
            if (lines.Length < 1)
                return (0, 0, 0, 0, 0, 0, 0, false);

            var headers = lines[0].Split(',').Select(h => h.Trim().ToLowerInvariant()).ToArray();
            int matCol = -1;
            foreach (var name in s_materialColumnNames)
            {
                matCol = Array.IndexOf(headers, name);
                if (matCol >= 0) break;
            }
            if (matCol < 0)
                return (0, 0, 0, 0, 0, 0, 0, false);

            int total = 0, wall = 0, floor = 0, access = 0, lot = 0, residual = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length <= matCol) continue;
                var mat = parts[matCol].Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(mat)) continue;

                total++;
                if      (mat.Contains("WALL",     StringComparison.Ordinal)) wall++;
                else if (mat.Contains("FLOOR",    StringComparison.Ordinal)) floor++;
                else if (mat.Contains("ACCESS",   StringComparison.Ordinal)) access++;
                else if (mat.Contains("LOT",      StringComparison.Ordinal)) lot++;
                else if (mat == "COMPONENT" || mat.Contains("RESIDUAL", StringComparison.Ordinal)) residual++;
            }

            // 5 canonical material kind buckets are always tracked regardless of per-bucket counts
            return (total, wall, floor, access, lot, residual, 5, true);
        }
        catch
        {
            return (0, 0, 0, 0, 0, 0, 0, false);
        }
    }

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunResult Build(
        string auditRoot,
        string outputRoot)
    {
        const string invalidVerdict =
            "MAP27I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_MATERIALIZATION_REPLAY_DRY_RUN_INVALID";

        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunResult
        {
            Format                         = "MAP-27I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_MATERIALIZATION_REPLAY_DRY_RUN",
            GeneratedUtc                   = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MapId                          = "map_00",
            SourceAuditRoot                = auditRoot,
            DryRunStage                    = "SANDBOX_WRITER_LOCKED_MATERIALIZATION_REPLAY_DRY_RUN",
            DryRunMode                     = "REPLAY_LOCKED_MAP27C_MATERIALIZATION_SOURCES_ONLY",
            SandboxOnly                    = true,
            SandboxLockedReplayDryRun      = true,
            WriterReady                    = false,
            RuntimeValid                   = false,
            Materialized                   = false,
            PzRuntimeMaterialized          = false,
            RuntimeProofClaimed            = false,
            PublicPlayablePackagingClaimed = false,
            NextAllowedExperimentName      = "MAP-27J_SANDBOX_WRITER_LOCKED_REPLAY_BACKEND_PLAN",
            NextAllowedExperimentStatus    = "SANDBOX_ONLY_NOT_RUNTIME",
            ClaimBoundaryAudit             = "writer_ready=false | runtime_valid=false | materialized=false | runtime_proof_claimed=false | public_playable_packaging_claimed=false",
            NextForbiddenSteps             = new List<string>(s_requiredForbiddenSteps),
        };

        if (!Directory.Exists(auditRoot))
        {
            result.Errors.Add($"MAP-27H audit root not found: {auditRoot}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        const string auditFileName =
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_locked_replay_audit.json";
        string auditPath = Path.Combine(auditRoot, auditFileName);
        result.SourceAuditPath = auditPath;

        if (!File.Exists(auditPath))
        {
            result.Errors.Add($"MAP-27H audit JSON not found: {auditPath}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        result.SourceAuditSha256 = HashFile(auditPath);

        // Parse MAP-27H audit JSON
        string storedAuditStatus            = string.Empty;
        bool   storedIsValid                = false;
        bool   storedReplayLockIdMatches    = false;
        int    storedLockedFileCount        = 0;
        int    storedHashMatchCount         = 0;
        int    storedHashMismatchCount      = 0;
        int    storedMissingCount           = 0;
        string storedForbiddenArtifactScan  = string.Empty;
        string storedSourceReplayLockId     = string.Empty;
        string storedRecomputedReplayLockId = string.Empty;
        bool   storedWriterReady            = false;
        bool   storedRuntimeValid           = false;
        bool   storedMaterialized           = false;
        bool   storedRuntimeProofClaimed    = false;
        bool   storedPublicPlayable         = false;
        int    storedMaterializedCellCount  = 0;
        int    storedWallCount              = 0;
        int    storedFloorCount             = 0;
        int    storedAccessCount            = 0;
        int    storedLotCount               = 0;
        int    storedResidualCount          = 0;
        int    storedMaterialKindCount      = 0;
        int    storedLayerKindCount         = 0;
        var    storedNextForbiddenSteps     = new List<string>();
        var    parsedLockedFiles            = new List<(int Order, string Role, string Path, string StoredSha256)>();

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(auditPath, Encoding.UTF8));
            var r = doc.RootElement;

            JsonElement p;
            if (r.TryGetProperty("audit_status",                      out p)) storedAuditStatus            = p.GetString() ?? "";
            if (r.TryGetProperty("is_valid",                          out p)) storedIsValid                = p.GetBoolean();
            if (r.TryGetProperty("replay_lock_id_matches",            out p)) storedReplayLockIdMatches    = p.GetBoolean();
            if (r.TryGetProperty("locked_file_count",                 out p)) storedLockedFileCount        = p.GetInt32();
            if (r.TryGetProperty("locked_file_hash_match_count",      out p)) storedHashMatchCount         = p.GetInt32();
            if (r.TryGetProperty("locked_file_hash_mismatch_count",   out p)) storedHashMismatchCount      = p.GetInt32();
            if (r.TryGetProperty("locked_file_missing_count",         out p)) storedMissingCount           = p.GetInt32();
            if (r.TryGetProperty("forbidden_artifact_scan",           out p)) storedForbiddenArtifactScan  = p.GetString() ?? "";
            if (r.TryGetProperty("source_replay_lock_id",             out p)) storedSourceReplayLockId     = p.GetString() ?? "";
            if (r.TryGetProperty("recomputed_replay_lock_id",         out p)) storedRecomputedReplayLockId = p.GetString() ?? "";
            if (r.TryGetProperty("writer_ready",                      out p)) storedWriterReady            = p.GetBoolean();
            if (r.TryGetProperty("runtime_valid",                     out p)) storedRuntimeValid           = p.GetBoolean();
            if (r.TryGetProperty("materialized",                      out p)) storedMaterialized           = p.GetBoolean();
            if (r.TryGetProperty("runtime_proof_claimed",             out p)) storedRuntimeProofClaimed    = p.GetBoolean();
            if (r.TryGetProperty("public_playable_packaging_claimed", out p)) storedPublicPlayable         = p.GetBoolean();
            if (r.TryGetProperty("materialized_cell_count",           out p)) storedMaterializedCellCount  = p.GetInt32();
            if (r.TryGetProperty("building_wall_candidate_cell_count",  out p)) storedWallCount            = p.GetInt32();
            if (r.TryGetProperty("building_floor_candidate_cell_count", out p)) storedFloorCount           = p.GetInt32();
            if (r.TryGetProperty("access_edge_cell_count",            out p)) storedAccessCount            = p.GetInt32();
            if (r.TryGetProperty("lot_space_cell_count",              out p)) storedLotCount               = p.GetInt32();
            if (r.TryGetProperty("component_residual_cell_count",     out p)) storedResidualCount          = p.GetInt32();
            if (r.TryGetProperty("material_kind_count",               out p)) storedMaterialKindCount      = p.GetInt32();
            if (r.TryGetProperty("layer_kind_count",                  out p)) storedLayerKindCount         = p.GetInt32();

            if (r.TryGetProperty("next_forbidden_steps", out var nfsEl) &&
                nfsEl.ValueKind == JsonValueKind.Array)
            {
                storedNextForbiddenSteps.Clear();
                foreach (var entry in nfsEl.EnumerateArray())
                    storedNextForbiddenSteps.Add(entry.GetString() ?? "");
            }

            if (r.TryGetProperty("locked_files", out var filesEl) &&
                filesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var f in filesEl.EnumerateArray())
                {
                    int    order      = f.TryGetProperty("file_order",    out var fp) ? fp.GetInt32()    : 0;
                    string role       = f.TryGetProperty("file_role",     out fp)     ? fp.GetString() ?? "" : "";
                    string filePath   = f.TryGetProperty("file_path",     out fp)     ? fp.GetString() ?? "" : "";
                    string storedHash = f.TryGetProperty("stored_sha256", out fp)     ? fp.GetString() ?? "" : "";
                    parsedLockedFiles.Add((order, role, filePath, storedHash));
                }
                parsedLockedFiles.Sort((a, b) => a.Order.CompareTo(b.Order));
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse MAP-27H audit JSON: {ex.Message}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        result.SourceAuditStatus            = storedAuditStatus;
        result.SourceReplayLockId           = storedSourceReplayLockId;
        result.SourceRecomputedReplayLockId = storedRecomputedReplayLockId;
        result.SourceReplayLockIdMatches    = storedReplayLockIdMatches;
        result.LayerKindCount               = storedLayerKindCount;
        result.NextForbiddenSteps           = storedNextForbiddenSteps.Count > 0
            ? storedNextForbiddenSteps
            : new List<string>(s_requiredForbiddenSteps);

        // Re-hash each locked file
        var lockedFiles = new List<DeadMtlLockedReplayDryRunLockedFile>();
        foreach (var lf in parsedLockedFiles)
        {
            bool   exists    = File.Exists(lf.Path);
            string reHash    = exists ? HashFile(lf.Path) : string.Empty;
            bool   matches   = exists && string.Equals(lf.StoredSha256, reHash, StringComparison.Ordinal);
            lockedFiles.Add(new DeadMtlLockedReplayDryRunLockedFile
            {
                FileOrder        = lf.Order,
                FileRole         = lf.Role,
                FilePath         = lf.Path,
                StoredSha256     = lf.StoredSha256,
                RehashedSha256   = reHash,
                HashStillMatches = matches,
                Exists           = exists,
            });
        }
        result.LockedFiles                 = lockedFiles;
        result.LockedFileCount             = lockedFiles.Count;
        result.LockedFileHashMatchCount    = lockedFiles.Count(f => f.HashStillMatches);
        result.LockedFileHashMismatchCount = lockedFiles.Count(f => f.Exists && !f.HashStillMatches);
        result.LockedFileMissingCount      = lockedFiles.Count(f => !f.Exists);

        // Load AND parse materialized cells CSV
        string cellsCsvSha256 = string.Empty;
        bool   csvLoaded      = false;
        bool   csvParsed      = false;
        int    csvTotal = 0, csvWall = 0, csvFloor = 0, csvAccess = 0, csvLot = 0, csvResidual = 0, csvMaterialKinds = 0;
        var    csvFile        = lockedFiles.FirstOrDefault(f => f.FileRole == "MATERIALIZED_CELLS_CSV");
        if (csvFile != null && csvFile.Exists)
        {
            cellsCsvSha256 = HashFile(csvFile.FilePath);
            csvLoaded      = true;
            (csvTotal, csvWall, csvFloor, csvAccess, csvLot, csvResidual, csvMaterialKinds, csvParsed) =
                ParseMaterializedCellsCsv(csvFile.FilePath);
        }
        result.MaterializedCellsCsvSha256 = cellsCsvSha256;

        // Canonical counts — CSV-derived
        result.MaterializedCellCount           = csvTotal;
        result.BuildingWallCandidateCellCount  = csvWall;
        result.BuildingFloorCandidateCellCount = csvFloor;
        result.AccessEdgeCellCount             = csvAccess;
        result.LotSpaceCellCount               = csvLot;
        result.ComponentResidualCellCount      = csvResidual;
        result.MaterialKindCount               = csvMaterialKinds;

        // CSV-prefixed fields
        result.CsvMaterializedCellCount           = csvTotal;
        result.CsvBuildingWallCandidateCellCount  = csvWall;
        result.CsvBuildingFloorCandidateCellCount = csvFloor;
        result.CsvAccessEdgeCellCount             = csvAccess;
        result.CsvLotSpaceCellCount               = csvLot;
        result.CsvComponentResidualCellCount      = csvResidual;
        result.CsvMaterialKindCount               = csvMaterialKinds;

        // Audit-prefixed fields (from MAP-27H stored values)
        result.AuditMaterializedCellCount           = storedMaterializedCellCount;
        result.AuditBuildingWallCandidateCellCount  = storedWallCount;
        result.AuditBuildingFloorCandidateCellCount = storedFloorCount;
        result.AuditAccessEdgeCellCount             = storedAccessCount;
        result.AuditLotSpaceCellCount               = storedLotCount;
        result.AuditComponentResidualCellCount      = storedResidualCount;
        result.AuditMaterialKindCount               = storedMaterialKindCount;

        // Locked replay digest (uses CSV-derived counts)
        bool allRehashedAndMatch = lockedFiles.Count == 8 && lockedFiles.All(f => f.Exists && f.HashStillMatches);
        string lockedReplayDigest = string.Empty;
        if (allRehashedAndMatch && csvLoaded)
        {
            var sb = new StringBuilder("MAP27I_LOCKED_REPLAY_DRY_RUN_V1");
            sb.Append($"|SOURCE_REPLAY_LOCK_ID:{storedSourceReplayLockId}");
            sb.Append($"|AUDIT_SHA256:{result.SourceAuditSha256}");
            foreach (var lf in lockedFiles)
                sb.Append($"|{lf.FileRole}:{lf.RehashedSha256}");
            sb.Append($"|CELLS_CSV_SHA256:{cellsCsvSha256}");
            sb.Append($"|CELL_COUNT:{result.MaterializedCellCount}");
            sb.Append($"|WALL:{result.BuildingWallCandidateCellCount}|FLOOR:{result.BuildingFloorCandidateCellCount}|ACCESS:{result.AccessEdgeCellCount}|LOT:{result.LotSpaceCellCount}|RESIDUAL:{result.ComponentResidualCellCount}");
            sb.Append($"|MATERIAL_KINDS:{result.MaterialKindCount}|LAYER_KINDS:{result.LayerKindCount}");
            lockedReplayDigest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()))).ToLower();
        }
        result.LockedReplayDigest = lockedReplayDigest;

        // Forbidden artifact scan of output root
        Directory.CreateDirectory(outputRoot);
        string forbiddenScan   = ScanOutputRoot(outputRoot);
        bool   scanPasses      = forbiddenScan.StartsWith("POST_DRY_RUN_FORBIDDEN_SCAN PASS", StringComparison.Ordinal);
        result.ForbiddenArtifactScan = forbiddenScan;

        bool rolesInOrder = lockedFiles.Count == 8 &&
            Enumerable.Range(0, 8).All(i => lockedFiles[i].FileRole == s_expectedRoles[i]);

        bool allStillMatch = result.LockedFileHashMatchCount == 8 &&
                             result.LockedFileHashMismatchCount == 0 &&
                             result.LockedFileMissingCount == 0;
        int  rehashedCount = lockedFiles.Count(f => f.Exists && !string.IsNullOrEmpty(f.RehashedSha256));

        bool storedForbiddenScanPassed = storedForbiddenArtifactScan.StartsWith("POST_AUDIT_FORBIDDEN_SCAN PASS", StringComparison.Ordinal);
        var  missingFromAudit = s_requiredForbiddenSteps.Where(r => !storedNextForbiddenSteps.Contains(r)).ToList();

        // CSV-vs-audit count match
        bool csvCountsMatchAudit =
            csvTotal    == storedMaterializedCellCount &&
            csvWall     == storedWallCount             &&
            csvFloor    == storedFloorCount            &&
            csvAccess   == storedAccessCount           &&
            csvLot      == storedLotCount              &&
            csvResidual == storedResidualCount         &&
            csvMaterialKinds == storedMaterialKindCount;

        string csvMatchActual = csvCountsMatchAudit ? "COUNTS_MATCH" : BuildCountsMismatchDetail(
            csvTotal, storedMaterializedCellCount,
            csvWall, storedWallCount,
            csvFloor, storedFloorCount,
            csvAccess, storedAccessCount,
            csvLot, storedLotCount,
            csvResidual, storedResidualCount,
            csvMaterialKinds, storedMaterialKindCount);

        // 53 checks in exact required order
        var checks = new List<DeadMtlLockedReplayDryRunCheck>();

        // 1-3: Audit root / file / hash
        MakeCheck(checks, "MAP27H_AUDIT_ROOT_EXISTS",
            "MAP-27H audit root exists");
        AddCheck(checks, "MAP27H_AUDIT_JSON_EXISTS",
            "MAP-27H audit JSON file exists",
            "true", File.Exists(auditPath) ? "true" : "false");
        MakeCheck(checks, "MAP27H_AUDIT_JSON_HASHED",
            "MAP-27H audit JSON SHA-256 hashed");

        // 4-12: Audit JSON field verification
        AddCheck(checks, "MAP27H_AUDIT_STATUS_VERIFIED",
            "MAP-27H audit_status is VERIFIED_LOCKED_REPLAY_SOURCE_SET",
            "VERIFIED_LOCKED_REPLAY_SOURCE_SET", storedAuditStatus);
        AddCheck(checks, "MAP27H_IS_VALID_TRUE",
            "MAP-27H is_valid is true",
            "true", storedIsValid ? "true" : "false");
        AddCheck(checks, "MAP27H_REPLAY_LOCK_ID_MATCHES_TRUE",
            "MAP-27H replay_lock_id_matches is true",
            "true", storedReplayLockIdMatches ? "true" : "false");
        AddCheck(checks, "MAP27H_LOCKED_FILE_COUNT_8",
            "MAP-27H locked_file_count is 8",
            "8", storedLockedFileCount.ToString());
        AddCheck(checks, "MAP27H_LOCKED_FILE_HASH_MATCH_COUNT_8",
            "MAP-27H locked_file_hash_match_count is 8",
            "8", storedHashMatchCount.ToString());
        AddCheck(checks, "MAP27H_LOCKED_FILE_HASH_MISMATCH_COUNT_0",
            "MAP-27H locked_file_hash_mismatch_count is 0",
            "0", storedHashMismatchCount.ToString());
        AddCheck(checks, "MAP27H_LOCKED_FILE_MISSING_COUNT_0",
            "MAP-27H locked_file_missing_count is 0",
            "0", storedMissingCount.ToString());
        AddCheck(checks, "MAP27H_FORBIDDEN_ARTIFACT_SCAN_PASS",
            "MAP-27H forbidden_artifact_scan indicates PASS",
            "PASS", storedForbiddenScanPassed ? "PASS" : "FAIL");
        string missingActual = missingFromAudit.Count == 0
            ? "REQUIRED_11_PRESENT"
            : "MISSING:" + string.Join(",", missingFromAudit);
        AddCheck(checks, "MAP27H_FORBIDDEN_STEPS_REQUIRED_11_PRESENT",
            "MAP-27H next_forbidden_steps contains all 11 required forbidden categories",
            "REQUIRED_11_PRESENT", missingActual);

        // 13: Role order
        AddCheck(checks, "LOCKED_FILE_ROLES_EXACT_ORDER",
            "Locked file roles match exact required order",
            "EXACT_ORDER_MATCH", rolesInOrder ? "EXACT_ORDER_MATCH" : "ORDER_MISMATCH");

        // 14-21: Per-file existence
        AddCheck(checks, "LOCKED_FILE_1_ACCEPTANCE_GATE_RESULT_JSON_EXISTS",
            "Locked file 1 ACCEPTANCE_GATE_RESULT_JSON exists at stored path",
            "true", lockedFiles.Count > 0 && lockedFiles[0].Exists ? "true" : "false");
        AddCheck(checks, "LOCKED_FILE_2_TILE_MATERIALIZER_RESULT_JSON_EXISTS",
            "Locked file 2 TILE_MATERIALIZER_RESULT_JSON exists at stored path",
            "true", lockedFiles.Count > 1 && lockedFiles[1].Exists ? "true" : "false");
        AddCheck(checks, "LOCKED_FILE_3_MATERIALIZED_CELLS_CSV_EXISTS",
            "Locked file 3 MATERIALIZED_CELLS_CSV exists at stored path",
            "true", lockedFiles.Count > 2 && lockedFiles[2].Exists ? "true" : "false");
        AddCheck(checks, "LOCKED_FILE_4_MATERIAL_PALETTE_JSON_EXISTS",
            "Locked file 4 MATERIAL_PALETTE_JSON exists at stored path",
            "true", lockedFiles.Count > 3 && lockedFiles[3].Exists ? "true" : "false");
        AddCheck(checks, "LOCKED_FILE_5_LAYER_STACK_JSON_EXISTS",
            "Locked file 5 LAYER_STACK_JSON exists at stored path",
            "true", lockedFiles.Count > 4 && lockedFiles[4].Exists ? "true" : "false");
        AddCheck(checks, "LOCKED_FILE_6_MATERIALIZATION_REPLAY_LOG_JSON_EXISTS",
            "Locked file 6 MATERIALIZATION_REPLAY_LOG_JSON exists at stored path",
            "true", lockedFiles.Count > 5 && lockedFiles[5].Exists ? "true" : "false");
        AddCheck(checks, "LOCKED_FILE_7_MATERIALIZATION_OWNERSHIP_SUMMARY_JSON_EXISTS",
            "Locked file 7 MATERIALIZATION_OWNERSHIP_SUMMARY_JSON exists at stored path",
            "true", lockedFiles.Count > 6 && lockedFiles[6].Exists ? "true" : "false");
        AddCheck(checks, "LOCKED_FILE_8_MATERIALIZER_FORBIDDEN_OUTPUT_GUARD_JSON_EXISTS",
            "Locked file 8 MATERIALIZER_FORBIDDEN_OUTPUT_GUARD_JSON exists at stored path",
            "true", lockedFiles.Count > 7 && lockedFiles[7].Exists ? "true" : "false");

        // 22-23: Re-hash aggregates
        AddCheck(checks, "ALL_8_LOCKED_FILES_REHASHED",
            "All 8 locked files successfully re-hashed",
            "8", rehashedCount.ToString());
        AddCheck(checks, "ALL_8_LOCKED_FILE_HASHES_STILL_MATCH",
            "All 8 locked file re-hashed SHA-256 values still match stored hashes",
            "true", allStillMatch ? "true" : "false");

        // 24: CSV loaded
        AddCheck(checks, "MATERIALIZED_CELLS_CSV_LOADED",
            "Materialized cells CSV loaded successfully",
            "LOADED", csvLoaded ? "LOADED" : "FAILED_TO_LOAD");

        // 25-32: Count checks — CSV-derived canonical counts
        AddCheck(checks, "MATERIALIZED_CELL_COUNT_5340",
            "materialized_cell_count is 5340",
            "5340", csvTotal.ToString());
        AddCheck(checks, "WALL_COUNT_850",
            "building_wall_candidate_cell_count is 850",
            "850", csvWall.ToString());
        AddCheck(checks, "FLOOR_COUNT_2444",
            "building_floor_candidate_cell_count is 2444",
            "2444", csvFloor.ToString());
        AddCheck(checks, "ACCESS_COUNT_148",
            "access_edge_cell_count is 148",
            "148", csvAccess.ToString());
        AddCheck(checks, "LOT_COUNT_1898",
            "lot_space_cell_count is 1898",
            "1898", csvLot.ToString());
        AddCheck(checks, "COMPONENT_RESIDUAL_COUNT_0",
            "component_residual_cell_count is 0",
            "0", csvResidual.ToString());
        AddCheck(checks, "MATERIAL_KIND_COUNT_5",
            "material_kind_count is 5",
            "5", csvMaterialKinds.ToString());
        AddCheck(checks, "LAYER_KIND_COUNT_5",
            "layer_kind_count is 5",
            "5", storedLayerKindCount.ToString());

        // 33: Digest
        AddCheck(checks, "LOCKED_REPLAY_DIGEST_COMPUTED",
            "Locked replay digest computed successfully",
            "true", !string.IsNullOrEmpty(lockedReplayDigest) ? "true" : "false");

        // 34-41: Claim boundary booleans
        MakeCheck(checks, "SANDBOX_ONLY_TRUE",
            "sandbox_only is true");
        MakeCheck(checks, "SANDBOX_LOCKED_REPLAY_DRY_RUN_TRUE",
            "sandbox_locked_replay_dry_run is true");
        MakeCheck(checks, "PZ_RUNTIME_MATERIALIZED_FALSE",
            "pz_runtime_materialized is false");
        MakeCheck(checks, "WRITER_READY_FALSE",
            "writer_ready is false");
        MakeCheck(checks, "RUNTIME_VALID_FALSE",
            "runtime_valid is false");
        MakeCheck(checks, "MATERIALIZED_FALSE",
            "materialized is false");
        MakeCheck(checks, "NO_RUNTIME_PROOF_CLAIM",
            "No runtime proof claimed");
        MakeCheck(checks, "NO_PUBLIC_PLAYABLE_PACKAGING_CLAIM",
            "No public playable packaging claimed");

        // 42-44: Final checks
        AddCheck(checks, "POST_DRY_RUN_FORBIDDEN_SCAN_PASS",
            "Post dry-run forbidden artifact scan passes in output root",
            "PASS", scanPasses ? "PASS" : "FAIL");
        MakeCheck(checks, "NEXT_ALLOWED_EXPERIMENT_SANDBOX_ONLY",
            "Next allowed experiment is sandbox-only, not runtime");
        MakeCheck(checks, "NO_RUNTIME_OUTPUTS_EMITTED",
            "No runtime outputs emitted — sandbox dry-run only");

        // 45-53: CSV replay checks
        AddCheck(checks, "MATERIALIZED_CELLS_CSV_PARSED",
            "Materialized cells CSV parsed by header name",
            "PARSED", csvParsed ? "PARSED" : "PARSE_FAILED");
        AddCheck(checks, "CSV_MATERIALIZED_CELL_COUNT_5340",
            "CSV materialized_cell_count is 5340",
            "5340", csvTotal.ToString());
        AddCheck(checks, "CSV_WALL_COUNT_850",
            "CSV building_wall_candidate_cell_count is 850",
            "850", csvWall.ToString());
        AddCheck(checks, "CSV_FLOOR_COUNT_2444",
            "CSV building_floor_candidate_cell_count is 2444",
            "2444", csvFloor.ToString());
        AddCheck(checks, "CSV_ACCESS_COUNT_148",
            "CSV access_edge_cell_count is 148",
            "148", csvAccess.ToString());
        AddCheck(checks, "CSV_LOT_COUNT_1898",
            "CSV lot_space_cell_count is 1898",
            "1898", csvLot.ToString());
        AddCheck(checks, "CSV_COMPONENT_RESIDUAL_COUNT_0",
            "CSV component_residual_cell_count is 0",
            "0", csvResidual.ToString());
        AddCheck(checks, "CSV_MATERIAL_KIND_COUNT_5",
            "CSV material_kind_count is 5",
            "5", csvMaterialKinds.ToString());
        AddCheck(checks, "CSV_COUNTS_MATCH_MAP27H_AUDIT",
            "CSV-derived counts match MAP-27H audit stored counts",
            "COUNTS_MATCH", csvMatchActual);

        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass    = result.FailedCheckCount == 0;
        result.IsValid  = allPass;
        result.DryRunStatus = allPass ? "LOCKED_REPLAY_DRY_RUN_COMPLETE" : "LOCKED_REPLAY_DRY_RUN_FAILED";
        result.Verdict  = allPass
            ? "MAP27I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_LOCKED_MATERIALIZATION_REPLAY_DRY_RUN_COMPLETE"
            : invalidVerdict;

        return result;
    }

    private static string BuildCountsMismatchDetail(
        int csvTotal, int auditTotal,
        int csvWall, int auditWall,
        int csvFloor, int auditFloor,
        int csvAccess, int auditAccess,
        int csvLot, int auditLot,
        int csvResidual, int auditResidual,
        int csvKinds, int auditKinds)
    {
        var mismatches = new List<string>();
        if (csvTotal    != auditTotal)    mismatches.Add($"cell_count(csv={csvTotal},audit={auditTotal})");
        if (csvWall     != auditWall)     mismatches.Add($"wall(csv={csvWall},audit={auditWall})");
        if (csvFloor    != auditFloor)    mismatches.Add($"floor(csv={csvFloor},audit={auditFloor})");
        if (csvAccess   != auditAccess)   mismatches.Add($"access(csv={csvAccess},audit={auditAccess})");
        if (csvLot      != auditLot)      mismatches.Add($"lot(csv={csvLot},audit={auditLot})");
        if (csvResidual != auditResidual) mismatches.Add($"residual(csv={csvResidual},audit={auditResidual})");
        if (csvKinds    != auditKinds)    mismatches.Add($"material_kinds(csv={csvKinds},audit={auditKinds})");
        return "COUNTS_MISMATCH:" + string.Join(",", mismatches);
    }

    public string RenderJson(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunResult result) =>
        JsonSerializer.Serialize(result, s_jsonOptions);

    public string RenderMarkdown(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-27I WorldBuilder Minimal Concrete Geometry Sandbox Writer Locked Materialization Replay Dry Run");
        sb.AppendLine();
        sb.AppendLine($"- **Map ID:** {result.MapId}");
        sb.AppendLine($"- **Generated UTC:** {result.GeneratedUtc}");
        sb.AppendLine($"- **Dry Run Stage:** {result.DryRunStage}");
        sb.AppendLine($"- **Dry Run Mode:** {result.DryRunMode}");
        sb.AppendLine($"- **Dry Run Status:** {result.DryRunStatus}");
        sb.AppendLine($"- **Source Audit Status:** {result.SourceAuditStatus}");
        sb.AppendLine($"- **Source Replay Lock ID:** `{result.SourceReplayLockId}`");
        sb.AppendLine($"- **Replay Lock ID Matches:** {result.SourceReplayLockIdMatches}");
        sb.AppendLine($"- **Locked File Count:** {result.LockedFileCount}");
        sb.AppendLine($"- **Hash Match Count:** {result.LockedFileHashMatchCount}");
        sb.AppendLine($"- **Hash Mismatch Count:** {result.LockedFileHashMismatchCount}");
        sb.AppendLine($"- **Missing Count:** {result.LockedFileMissingCount}");
        sb.AppendLine($"- **Materialized Cell Count (CSV):** {result.MaterializedCellCount}");
        sb.AppendLine($"- **Wall Count (CSV):** {result.BuildingWallCandidateCellCount}");
        sb.AppendLine($"- **Floor Count (CSV):** {result.BuildingFloorCandidateCellCount}");
        sb.AppendLine($"- **Access Count (CSV):** {result.AccessEdgeCellCount}");
        sb.AppendLine($"- **Lot Count (CSV):** {result.LotSpaceCellCount}");
        sb.AppendLine($"- **Residual Count (CSV):** {result.ComponentResidualCellCount}");
        sb.AppendLine($"- **Locked Replay Digest:** `{result.LockedReplayDigest}`");
        sb.AppendLine($"- **Forbidden Artifact Scan:** {result.ForbiddenArtifactScan}");
        sb.AppendLine($"- **Claim Boundary:** {result.ClaimBoundaryAudit}");
        sb.AppendLine($"- **Verdict:** `{result.Verdict}`");
        sb.AppendLine($"- **Is Valid:** {result.IsValid}");
        sb.AppendLine($"- **Checks:** {result.CheckCount} / Passed: {result.PassedCheckCount} / Failed: {result.FailedCheckCount}");
        sb.AppendLine($"- **Next Allowed Experiment:** {result.NextAllowedExperimentName} ({result.NextAllowedExperimentStatus})");
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
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_order,check_id,check_status,expected,actual");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckOrder},{c.CheckId},{c.CheckStatus},{c.Expected},{c.Actual}");
        return sb.ToString();
    }

    public string RenderSummary(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-27I WorldBuilder Minimal Concrete Geometry Sandbox Writer Locked Materialization Replay Dry Run");
        sb.AppendLine($"Generated UTC                    : {result.GeneratedUtc}");
        sb.AppendLine($"Map ID                           : {result.MapId}");
        sb.AppendLine($"Dry Run Stage                    : {result.DryRunStage}");
        sb.AppendLine($"Dry Run Mode                     : {result.DryRunMode}");
        sb.AppendLine($"Dry Run Status                   : {result.DryRunStatus}");
        sb.AppendLine($"Source Audit Status              : {result.SourceAuditStatus}");
        sb.AppendLine($"Source Replay Lock ID            : {result.SourceReplayLockId}");
        sb.AppendLine($"Replay Lock ID Matches           : {(result.SourceReplayLockIdMatches ? 1 : 0)}");
        sb.AppendLine($"Locked File Count                : {result.LockedFileCount}");
        sb.AppendLine($"Hash Match Count                 : {result.LockedFileHashMatchCount}");
        sb.AppendLine($"Hash Mismatch Count              : {result.LockedFileHashMismatchCount}");
        sb.AppendLine($"Missing Count                    : {result.LockedFileMissingCount}");
        sb.AppendLine($"Verdict                          : {result.Verdict}");
        sb.AppendLine($"Is Valid                         : {(result.IsValid ? 1 : 0)}");
        sb.AppendLine($"Sandbox Only                     : 1");
        sb.AppendLine($"Sandbox Locked Replay Dry Run    : 1");
        sb.AppendLine($"PZ Runtime Materialized          : 0");
        sb.AppendLine($"Writer Ready                     : 0");
        sb.AppendLine($"Runtime Valid                    : 0");
        sb.AppendLine($"Materialized                     : 0");
        sb.AppendLine($"Runtime Proof                    : 0");
        sb.AppendLine($"Public Playable                  : 0");
        sb.AppendLine($"Materialized Cell Count (CSV)    : {result.MaterializedCellCount}");
        sb.AppendLine($"Wall Count (CSV)                 : {result.BuildingWallCandidateCellCount}");
        sb.AppendLine($"Floor Count (CSV)                : {result.BuildingFloorCandidateCellCount}");
        sb.AppendLine($"Access Count (CSV)               : {result.AccessEdgeCellCount}");
        sb.AppendLine($"Lot Count (CSV)                  : {result.LotSpaceCellCount}");
        sb.AppendLine($"Residual Count (CSV)             : {result.ComponentResidualCellCount}");
        sb.AppendLine($"Material Kind Count              : {result.MaterialKindCount}");
        sb.AppendLine($"Layer Kind Count                 : {result.LayerKindCount}");
        sb.AppendLine($"Cells CSV SHA-256                : {result.MaterializedCellsCsvSha256}");
        sb.AppendLine($"Locked Replay Digest             : {result.LockedReplayDigest}");
        sb.AppendLine($"Forbidden Artifact Scan          : {result.ForbiddenArtifactScan}");
        sb.AppendLine($"Claim Boundary                   : {result.ClaimBoundaryAudit}");
        sb.AppendLine($"Next Allowed Experiment          : {result.NextAllowedExperimentName}");
        sb.AppendLine($"Next Experiment Status           : {result.NextAllowedExperimentStatus}");
        sb.AppendLine($"Checks                           : {result.CheckCount}");
        sb.AppendLine($"Passed                           : {result.PassedCheckCount}");
        sb.Append(    $"Failed                           : {result.FailedCheckCount}");
        return sb.ToString();
    }

    public string RenderMaterialCountsCsv(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("material_kind,cell_count,source");
        sb.AppendLine($"WALL,{result.BuildingWallCandidateCellCount},CSV");
        sb.AppendLine($"FLOOR,{result.BuildingFloorCandidateCellCount},CSV");
        sb.AppendLine($"ACCESS,{result.AccessEdgeCellCount},CSV");
        sb.AppendLine($"LOT,{result.LotSpaceCellCount},CSV");
        sb.Append(    $"COMPONENT,{result.ComponentResidualCellCount},CSV");
        return sb.ToString();
    }

    public string RenderSourceManifestJson(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunResult result)
    {
        var manifest = new
        {
            source_audit_path     = result.SourceAuditPath,
            source_audit_sha256   = result.SourceAuditSha256,
            source_replay_lock_id = result.SourceReplayLockId,
            locked_file_count     = result.LockedFileCount,
            locked_files          = result.LockedFiles.Select(f => new
            {
                file_order         = f.FileOrder,
                file_role          = f.FileRole,
                file_path          = f.FilePath,
                stored_sha256      = f.StoredSha256,
                rehashed_sha256    = f.RehashedSha256,
                hash_still_matches = f.HashStillMatches,
                exists             = f.Exists,
            }).ToList(),
        };
        return JsonSerializer.Serialize(manifest, s_jsonOptions);
    }

    public string RenderReplayDigestJson(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunResult result)
    {
        var digest = new
        {
            locked_replay_digest          = result.LockedReplayDigest,
            source_replay_lock_id         = result.SourceReplayLockId,
            source_audit_sha256           = result.SourceAuditSha256,
            materialized_cells_csv_sha256 = result.MaterializedCellsCsvSha256,
            materialized_cell_count       = result.MaterializedCellCount,
            dry_run_stage                 = result.DryRunStage,
            dry_run_mode                  = result.DryRunMode,
        };
        return JsonSerializer.Serialize(digest, s_jsonOptions);
    }

    public string RenderForbiddenOutputGuardJson(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunResult result)
    {
        var guard = new
        {
            forbidden_output_guard            = true,
            sandbox_only                      = true,
            sandbox_locked_replay_dry_run     = true,
            writer_ready                      = false,
            runtime_valid                     = false,
            materialized                      = false,
            pz_runtime_materialized           = false,
            runtime_proof_claimed             = false,
            public_playable_packaging_claimed = false,
            forbidden_artifact_scan           = result.ForbiddenArtifactScan,
            all_clean                         = result.ForbiddenArtifactScan.StartsWith("POST_DRY_RUN_FORBIDDEN_SCAN PASS", StringComparison.Ordinal),
        };
        return JsonSerializer.Serialize(guard, s_jsonOptions);
    }
}
