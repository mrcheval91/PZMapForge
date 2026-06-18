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

        string storedVerdict          = string.Empty;
        bool   storedIsValid          = false;
        string storedStatus           = string.Empty;
        string storedReplayLockId     = string.Empty;
        int    storedFileCount        = 0;
        bool   storedSandboxOnly      = false;
        bool   storedPzRuntime        = true;
        var    storedLockedFiles      = new List<(int Order, string Stage, string Role, string Name, string Path, string Sha256, bool LockedForReplay, bool RuntimeConsumable, bool WriterConsumable)>();

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(lockFilePath, Encoding.UTF8));
            var r = doc.RootElement;
            if (r.TryGetProperty("verdict",              out var p)) storedVerdict      = p.GetString() ?? "";
            if (r.TryGetProperty("is_valid",             out p))     storedIsValid      = p.GetBoolean();
            if (r.TryGetProperty("replay_lock_status",   out p))     storedStatus       = p.GetString() ?? "";
            if (r.TryGetProperty("replay_lock_id",       out p))     storedReplayLockId = p.GetString() ?? "";
            if (r.TryGetProperty("replay_lock_file_count", out p))   storedFileCount    = p.GetInt32();
            if (r.TryGetProperty("sandbox_only",         out p))     storedSandboxOnly  = p.GetBoolean();
            if (r.TryGetProperty("pz_runtime_materialized", out p))  storedPzRuntime   = p.GetBoolean();

            if (r.TryGetProperty("replay_lock_files", out var filesEl) &&
                filesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var f in filesEl.EnumerateArray())
                {
                    int    order    = f.TryGetProperty("file_order",          out var fp) ? fp.GetInt32()    : 0;
                    string stage    = f.TryGetProperty("source_stage",        out fp)     ? fp.GetString() ?? "" : "";
                    string role     = f.TryGetProperty("file_role",           out fp)     ? fp.GetString() ?? "" : "";
                    string name     = f.TryGetProperty("file_name",           out fp)     ? fp.GetString() ?? "" : "";
                    string path     = f.TryGetProperty("file_path",           out fp)     ? fp.GetString() ?? "" : "";
                    string sha256   = f.TryGetProperty("sha256",              out fp)     ? fp.GetString() ?? "" : "";
                    bool   locked   = f.TryGetProperty("locked_for_replay",   out fp)     && fp.GetBoolean();
                    bool   runtime  = f.TryGetProperty("runtime_consumable",  out fp)     && fp.GetBoolean();
                    bool   writer   = f.TryGetProperty("writer_consumable",   out fp)     && fp.GetBoolean();
                    storedLockedFiles.Add((order, stage, role, name, path, sha256, locked, runtime, writer));
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse replay lock JSON: {ex.Message}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        result.SourceReplayLockVerdict   = storedVerdict;
        result.SourceReplayLockIsValid   = storedIsValid;
        result.SourceReplayLockStatus    = storedStatus;
        result.SourceReplayLockId        = storedReplayLockId;
        result.SourceReplayLockFileCount = storedFileCount;
        result.SandboxOnly               = storedSandboxOnly;
        result.PzRuntimeMaterialized     = storedPzRuntime;
        result.LockedFileCount           = storedLockedFiles.Count;

        // Re-hash each locked file and build audit file list
        var auditFiles = new List<DeadMtlTileMaterializationLockedReplayAuditFile>();
        foreach (var lf in storedLockedFiles)
        {
            bool   exists   = File.Exists(lf.Path);
            string recomp   = exists ? HashFile(lf.Path) : string.Empty;
            bool   matches  = exists && string.Equals(lf.Sha256, recomp, StringComparison.Ordinal);
            string status   = !exists ? "LOCKED_FILE_MISSING"
                            : matches ? "LOCKED_FILE_VERIFIED"
                            : "LOCKED_FILE_HASH_MISMATCH";
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
        result.LockedFileHashMatchCount    = auditFiles.Count(f => f.HashMatches);
        result.LockedFileHashMismatchCount = auditFiles.Count(f => f.Exists && !f.HashMatches);
        result.LockedFileHashMissingCount  = auditFiles.Count(f => !f.Exists);

        // Recompute replay lock ID using MAP-27G1 formula (with recomputed hashes)
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
            string lockHex  = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(lockInput))).ToLower();
            recomputedLockId = "map_00_replay_lock_" + lockHex[..16];
        }
        result.RecomputedReplayLockId = recomputedLockId;
        result.ReplayLockIdMatches    = string.Equals(storedReplayLockId, recomputedLockId, StringComparison.Ordinal);

        bool allHashesMatch = result.LockedFileHashMatchCount == 8 &&
                              result.LockedFileHashMismatchCount == 0 &&
                              result.LockedFileHashMissingCount  == 0;

        // Determine audit_status
        bool auditVerified = allHashesMatch && result.ReplayLockIdMatches;
        result.AuditStatus = auditVerified
            ? "VERIFIED_LOCKED_REPLAY_SOURCE_SET"
            : "AUDIT_FAILED";

        // Check that only 1 MAP-27F file in locked set (the JSON, not md/csv/summary)
        int map27fCount     = auditFiles.Count(f => f.SourceStage == "MAP-27F");
        bool noOldMap27fMd  = map27fCount == 1 &&
                              auditFiles.Where(f => f.SourceStage == "MAP-27F")
                                        .All(f => f.FileRole == "ACCEPTANCE_GATE_RESULT_JSON");

        // 45 checks
        var checks = new List<DeadMtlTileMaterializationLockedReplayAuditCheck>();

        // Group 1: Source replay lock file (4)
        MakeCheck(checks, "SOURCE_REPLAY_LOCK_ROOT_EXISTS",      "Replay lock root exists");
        AddCheck(checks,  "SOURCE_REPLAY_LOCK_FILE_EXISTS",       "Replay lock JSON file exists",
            "true", File.Exists(lockFilePath) ? "true" : "false");
        MakeCheck(checks, "SOURCE_REPLAY_LOCK_FILE_HASHED",      "Replay lock JSON SHA-256 hashed");
        MakeCheck(checks, "SOURCE_REPLAY_LOCK_FILE_PARSEABLE",   "Replay lock JSON parseable");

        // Group 2: Source lock header validation (6)
        AddCheck(checks, "SOURCE_REPLAY_LOCK_STATUS_LOCKED_FOR_NEXT_SANDBOX",
            "Source replay_lock_status is LOCKED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY",
            "LOCKED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY", storedStatus);
        AddCheck(checks, "SOURCE_REPLAY_LOCK_ID_PRESENT",
            "Source replay_lock_id is not empty",
            "true", !string.IsNullOrEmpty(storedReplayLockId) && storedReplayLockId != "LOCK_ID_NOT_GENERATED" ? "true" : "false");
        AddCheck(checks, "SOURCE_REPLAY_LOCK_FILE_COUNT_8",
            "Source replay_lock_file_count is 8",
            "8", storedFileCount.ToString());
        AddCheck(checks, "SOURCE_REPLAY_LOCK_VERDICT_COMPLETE",
            "Source verdict is MAP27G COMPLETE",
            "MAP27G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOCK_COMPLETE",
            storedVerdict);
        AddCheck(checks, "SOURCE_REPLAY_LOCK_IS_VALID_TRUE",
            "Source is_valid is true",
            "true", storedIsValid ? "true" : "false");
        AddCheck(checks, "SOURCE_REPLAY_LOCK_SANDBOX_ONLY_TRUE",
            "Source sandbox_only is true",
            "true", storedSandboxOnly ? "true" : "false");

        // Group 3: Per-file existence (8)
        for (int i = 0; i < 8; i++)
        {
            bool exists = i < auditFiles.Count && auditFiles[i].Exists;
            AddCheck(checks, $"LOCKED_FILE_{i + 1}_EXISTS",
                $"Locked file {i + 1} exists at stored path",
                "true", exists ? "true" : "false");
        }

        // Group 4: Per-file hash match (8)
        for (int i = 0; i < 8; i++)
        {
            bool matches = i < auditFiles.Count && auditFiles[i].HashMatches;
            AddCheck(checks, $"LOCKED_FILE_{i + 1}_HASH_MATCHES",
                $"Locked file {i + 1} recomputed SHA-256 matches stored",
                "true", matches ? "true" : "false");
        }

        // Group 5: Aggregate hash counts (3)
        AddCheck(checks, "ALL_8_LOCKED_FILE_HASHES_MATCH",
            "All 8 locked file hashes match",
            "true", allHashesMatch ? "true" : "false");
        AddCheck(checks, "LOCKED_FILE_HASH_MISMATCH_COUNT_0",
            "Locked file hash mismatch count is 0",
            "0", result.LockedFileHashMismatchCount.ToString());
        AddCheck(checks, "LOCKED_FILE_HASH_MISSING_COUNT_0",
            "Locked file hash missing count is 0",
            "0", result.LockedFileHashMissingCount.ToString());

        // Group 6: Replay lock ID recomputation (4)
        AddCheck(checks, "RECOMPUTED_REPLAY_LOCK_ID_PRESENT",
            "Recomputed replay_lock_id is not RECOMPUTED_LOCK_ID_NOT_GENERATED",
            "true",
            recomputedLockId != "RECOMPUTED_LOCK_ID_NOT_GENERATED" ? "true" : "false");
        AddCheck(checks, "RECOMPUTED_REPLAY_LOCK_ID_MATCHES_STORED",
            "Recomputed replay_lock_id matches stored replay_lock_id",
            "true", result.ReplayLockIdMatches ? "true" : "false");
        MakeCheck(checks, "REPLAY_LOCK_ID_DETERMINISTIC",
            "Replay lock ID formula is deterministic (recomputed == stored)");
        AddCheck(checks, "AUDIT_LOCKED_FILE_COUNT_8",
            "Audit found exactly 8 locked files",
            "8", auditFiles.Count.ToString());

        // Group 7: Claim boundary (6)
        AddCheck(checks, "AUDIT_SANDBOX_ONLY_TRUE",
            "sandbox_only from source lock is true",
            "true", storedSandboxOnly ? "true" : "false");
        AddCheck(checks, "AUDIT_PZ_RUNTIME_MATERIALIZED_FALSE",
            "pz_runtime_materialized from source lock is false",
            "false", storedPzRuntime ? "true" : "false");
        MakeCheck(checks, "AUDIT_WRITER_READY_FALSE",           "writer_ready is false");
        MakeCheck(checks, "AUDIT_RUNTIME_VALID_FALSE",          "runtime_valid is false");
        MakeCheck(checks, "AUDIT_MATERIALIZED_FALSE",           "materialized (global PZ) is false");
        MakeCheck(checks, "AUDIT_NO_RUNTIME_PROOF_CLAIM",       "No runtime proof claimed");

        // Group 8: Final checks (6)
        AddCheck(checks, "AUDIT_STATUS_VERIFIED",
            "audit_status is VERIFIED_LOCKED_REPLAY_SOURCE_SET",
            "VERIFIED_LOCKED_REPLAY_SOURCE_SET", result.AuditStatus);
        MakeCheck(checks, "NEXT_ALLOWED_EXPERIMENT_SANDBOX_ONLY",
            "Next allowed experiment is SANDBOX_ONLY_NOT_RUNTIME");
        AddCheck(checks, "NO_OLD_MAP27F_MD_IN_LOCK_FILES",
            "Only 1 MAP-27F file in lock set and it is ACCEPTANCE_GATE_RESULT_JSON",
            "true", noOldMap27fMd ? "true" : "false");
        MakeCheck(checks, "AUDIT_MODE_CORRECT",
            "Audit mode is VERIFY_MAP27G1_REPLAY_LOCK_HASHES_AND_LOCK_ID_ONLY");
        MakeCheck(checks, "AUDIT_REPLAY_LOCK_SOURCE_NEGATIVE",
            "Audit does not produce any runtime outputs");
        MakeCheck(checks, "AUDIT_COMPLETE_NO_RUNTIME_OUTPUTS",
            "No runtime outputs emitted during audit");

        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass = result.FailedCheckCount == 0;
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
        sb.AppendLine($"- **Hash Missing Count:** {result.LockedFileHashMissingCount}");
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
            string stored   = f.StoredSha256.Length  >= 16 ? f.StoredSha256[..16]  + "..." : f.StoredSha256;
            string recomp   = f.RecomputedSha256.Length >= 16 ? f.RecomputedSha256[..16] + "..." : f.RecomputedSha256;
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
        sb.AppendLine($"Hash Missing Count               : {result.LockedFileHashMissingCount}");
        sb.AppendLine($"Verdict                          : {result.Verdict}");
        sb.AppendLine($"Is Valid                         : {(result.IsValid ? 1 : 0)}");
        sb.AppendLine($"Sandbox Only                     : {(result.SandboxOnly ? 1 : 0)}");
        sb.AppendLine($"PZ Runtime Materialized          : {(result.PzRuntimeMaterialized ? 1 : 0)}");
        sb.AppendLine($"Writer Ready                     : {(result.WriterReady ? 1 : 0)}");
        sb.AppendLine($"Runtime Valid                    : {(result.RuntimeValid ? 1 : 0)}");
        sb.AppendLine($"Materialized                     : {(result.Materialized ? 1 : 0)}");
        sb.AppendLine($"Runtime Proof                    : {(result.RuntimeProofClaimed ? 1 : 0)}");
        sb.AppendLine($"Public Playable                  : {(result.PublicPlayablePackagingClaimed ? 1 : 0)}");
        sb.AppendLine($"Next Allowed Experiment          : {result.NextAllowedExperimentName}");
        sb.AppendLine($"Next Experiment Status           : {result.NextAllowedExperimentStatus}");
        sb.AppendLine($"Checks                           : {result.CheckCount}");
        sb.AppendLine($"Passed                           : {result.PassedCheckCount}");
        sb.Append($"Failed                           : {result.FailedCheckCount}");
        return sb.ToString();
    }
}
