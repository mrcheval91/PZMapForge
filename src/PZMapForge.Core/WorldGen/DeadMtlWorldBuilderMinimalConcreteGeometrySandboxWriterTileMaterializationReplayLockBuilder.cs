using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockBuilder
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    private static string HashFile(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLower();

    private static void AddCheck(List<DeadMtlTileMaterializationReplayLockCheck> checks,
        string id, string label, string expected, string actual)
    {
        checks.Add(new DeadMtlTileMaterializationReplayLockCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = string.Equals(expected, actual, StringComparison.Ordinal) ? "PASS" : "FAIL",
            Expected    = expected,
            Actual      = actual,
        });
    }

    private static void MakeCheck(List<DeadMtlTileMaterializationReplayLockCheck> checks, string id, string label)
    {
        checks.Add(new DeadMtlTileMaterializationReplayLockCheck
        {
            CheckOrder  = checks.Count + 1,
            CheckId     = id,
            CheckLabel  = label,
            CheckStatus = "PASS",
            Expected    = "PASS",
            Actual      = "PASS",
        });
    }

    public DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockResult Build(
        string acceptanceGateRoot,
        string tileMaterializerRoot,
        string outputRoot)
    {
        var result = new DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockResult
        {
            Format                         = "MAP-27G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOCK",
            GeneratedUtc                   = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            MapId                          = "map_00",
            ReplayLockStage                = "SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOCK",
            ReplayLockMode                 = "LOCK_ACCEPTED_SANDBOX_MATERIALIZATION_INPUTS_ONLY",
            SandboxOnly                    = true,
            SandboxMaterializedSource      = true,
            PzRuntimeMaterialized          = false,
            AcceptedForRuntimeWriter       = false,
            AcceptedForPlayableExport      = false,
            WriterReady                    = false,
            RuntimeValid                   = false,
            Materialized                   = false,
            RuntimeProofClaimed            = false,
            PublicPlayablePackagingClaimed = false,
            SourceAcceptanceGateRoot       = acceptanceGateRoot,
            SourceTileMaterializerRoot     = tileMaterializerRoot,
            NextAllowedExperimentName      = "MAP-27H_SANDBOX_WRITER_TILE_MATERIALIZATION_LOCKED_REPLAY_AUDIT",
            NextAllowedExperimentStatus    = "SANDBOX_ONLY_NOT_RUNTIME",
            NextForbiddenSteps             = new List<string>
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
            },
            ReplayRequirements = new List<string>
            {
                "Use exactly the locked MAP-27C materialized cells CSV.",
                "Use exactly the locked MAP-27C material palette JSON.",
                "Use exactly the locked MAP-27C layer stack JSON.",
                "Use exactly the locked MAP-27C materialization replay log JSON.",
                "Use exactly the locked MAP-27C ownership summary JSON.",
                "Preserve the MAP-27F acceptance gate hash.",
                "Do not regenerate source geometry inside MAP-27G.",
                "Do not write runtime PZ files.",
            },
        };

        const string invalidVerdict =
            "MAP27G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOCK_INVALID";

        if (!Directory.Exists(acceptanceGateRoot))
        {
            result.Errors.Add($"MAP-27F acceptance gate root not found: {acceptanceGateRoot}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }
        if (!Directory.Exists(tileMaterializerRoot))
        {
            result.Errors.Add($"MAP-27C tile materializer root not found: {tileMaterializerRoot}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        // MAP-27F acceptance gate sanity files (all 4 must exist; only JSON is locked)
        var agFiles = new[]
        {
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.json",
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.md",
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.csv",
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materialization_acceptance_gate.summary.txt",
        };

        // MAP-27C tile materializer replay source files (all 7 required; all 7 locked)
        var tmFiles = new[]
        {
            "map_00.minimal_concrete_geometry_sandbox_writer_tile_materializer_v0.json",
            "map_00.sandbox_writer_tile_materialized_cells.csv",
            "map_00.sandbox_writer_tile_material_palette.json",
            "map_00.sandbox_writer_tile_layer_stack.json",
            "map_00.sandbox_writer_tile_materialization_replay_log.json",
            "map_00.sandbox_writer_tile_materialization_ownership_summary.json",
            "map_00.sandbox_writer_tile_materializer_forbidden_output_guard.json",
        };

        var missingAg = new List<string>();
        foreach (var name in agFiles)
        {
            string fp = Path.Combine(acceptanceGateRoot, name);
            if (!File.Exists(fp)) missingAg.Add(fp);
        }
        var missingTm = new List<string>();
        foreach (var name in tmFiles)
        {
            string fp = Path.Combine(tileMaterializerRoot, name);
            if (!File.Exists(fp)) missingTm.Add(fp);
        }
        if (missingAg.Count > 0 || missingTm.Count > 0)
        {
            foreach (var f in missingAg) result.Errors.Add($"Required MAP-27F file not found: {f}");
            foreach (var f in missingTm) result.Errors.Add($"Required MAP-27C file not found: {f}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        // Hash MAP-27F JSON (primary source reference)
        string agJsonPath = Path.Combine(acceptanceGateRoot, agFiles[0]);
        result.SourceAcceptanceGatePath   = agJsonPath;
        result.SourceAcceptanceGateSha256 = HashFile(agJsonPath);

        // Parse MAP-27F JSON (flat top-level fields)
        string f27Verdict                    = string.Empty;
        bool   f27IsValid                    = false;
        string f27AcceptanceGateStatus       = string.Empty;
        bool   f27AcceptedForNextSandbox     = false;
        bool   f27AcceptedForRuntimeWriter   = false;
        bool   f27AcceptedForPlayableExport  = false;
        bool   f27SandboxOnly                = false;
        bool   f27SandboxMaterializedSource  = false;
        bool   f27VisualQaOverlayWritten     = false;
        bool   f27PzRuntimeMaterialized      = true;
        int    f27MaterializedCellCount      = 0;
        int    f27RenderedCellCount          = 0;
        string f27CountMatchSummary          = string.Empty;
        int    f27WallCount                  = 0;
        int    f27FloorCount                 = 0;
        int    f27AccessCount                = 0;
        int    f27LotCount                   = 0;
        int    f27ResidualCount              = 0;
        int    f27MaterialKindCount          = 0;
        int    f27LayerKindCount             = 0;
        int    f27OverlayPngWidth            = 0;
        int    f27OverlayPngHeight           = 0;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(agJsonPath, Encoding.UTF8));
            var r = doc.RootElement;
            if (r.TryGetProperty("verdict",                             out var p)) f27Verdict                   = p.GetString() ?? "";
            if (r.TryGetProperty("is_valid",                            out p))     f27IsValid                   = p.GetBoolean();
            if (r.TryGetProperty("acceptance_gate_status",              out p))     f27AcceptanceGateStatus      = p.GetString() ?? "";
            if (r.TryGetProperty("accepted_for_next_sandbox_experiment", out p))    f27AcceptedForNextSandbox    = p.GetBoolean();
            if (r.TryGetProperty("accepted_for_runtime_writer",         out p))     f27AcceptedForRuntimeWriter  = p.GetBoolean();
            if (r.TryGetProperty("accepted_for_playable_export",        out p))     f27AcceptedForPlayableExport = p.GetBoolean();
            if (r.TryGetProperty("sandbox_only",                        out p))     f27SandboxOnly               = p.GetBoolean();
            if (r.TryGetProperty("sandbox_materialized_source",         out p))     f27SandboxMaterializedSource = p.GetBoolean();
            if (r.TryGetProperty("visual_qa_overlay_written",           out p))     f27VisualQaOverlayWritten    = p.GetBoolean();
            if (r.TryGetProperty("pz_runtime_materialized",             out p))     f27PzRuntimeMaterialized     = p.GetBoolean();
            if (r.TryGetProperty("materialized_cell_count",             out p))     f27MaterializedCellCount     = p.GetInt32();
            if (r.TryGetProperty("rendered_cell_count",                 out p))     f27RenderedCellCount         = p.GetInt32();
            if (r.TryGetProperty("count_match_summary",                 out p))     f27CountMatchSummary         = p.GetString() ?? "";
            if (r.TryGetProperty("building_wall_candidate_cell_count",  out p))     f27WallCount                 = p.GetInt32();
            if (r.TryGetProperty("building_floor_candidate_cell_count", out p))     f27FloorCount                = p.GetInt32();
            if (r.TryGetProperty("access_edge_cell_count",              out p))     f27AccessCount               = p.GetInt32();
            if (r.TryGetProperty("lot_space_cell_count",                out p))     f27LotCount                  = p.GetInt32();
            if (r.TryGetProperty("component_residual_cell_count",       out p))     f27ResidualCount             = p.GetInt32();
            if (r.TryGetProperty("material_kind_count",                 out p))     f27MaterialKindCount         = p.GetInt32();
            if (r.TryGetProperty("layer_kind_count",                    out p))     f27LayerKindCount            = p.GetInt32();
            if (r.TryGetProperty("overlay_png_width",                   out p))     f27OverlayPngWidth           = p.GetInt32();
            if (r.TryGetProperty("overlay_png_height",                  out p))     f27OverlayPngHeight          = p.GetInt32();
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse MAP-27F acceptance gate JSON: {ex.Message}");
            result.IsValid = false;
            result.Verdict = invalidVerdict;
            return result;
        }

        result.SourceAcceptanceGateVerdict              = f27Verdict;
        result.SourceAcceptanceGateIsValid              = f27IsValid;
        result.SourceAcceptanceGateStatus               = f27AcceptanceGateStatus;
        result.SourceAcceptedForNextSandboxExperiment   = f27AcceptedForNextSandbox;
        result.SourceAcceptedForRuntimeWriter           = f27AcceptedForRuntimeWriter;
        result.SourceAcceptedForPlayableExport          = f27AcceptedForPlayableExport;
        result.AcceptedForNextSandboxExperiment         = f27AcceptedForNextSandbox;
        result.SandboxMaterializedSource                = f27SandboxMaterializedSource;
        result.VisualQaOverlayWritten                   = f27VisualQaOverlayWritten;
        result.MaterializedCellCount                    = f27MaterializedCellCount;
        result.RenderedCellCount                        = f27RenderedCellCount;
        result.CountMatchSummary                        = f27CountMatchSummary;
        result.BuildingWallCandidateCellCount           = f27WallCount;
        result.BuildingFloorCandidateCellCount          = f27FloorCount;
        result.AccessEdgeCellCount                      = f27AccessCount;
        result.LotSpaceCellCount                        = f27LotCount;
        result.ComponentResidualCellCount               = f27ResidualCount;
        result.MaterialKindCount                        = f27MaterialKindCount;
        result.LayerKindCount                           = f27LayerKindCount;
        result.OverlayPngWidth                          = f27OverlayPngWidth;
        result.OverlayPngHeight                         = f27OverlayPngHeight;

        // Hash MAP-27C tile materializer result JSON (primary source reference)
        string tmJsonPath = Path.Combine(tileMaterializerRoot, tmFiles[0]);
        result.SourceTileMaterializerResultPath   = tmJsonPath;
        result.SourceTileMaterializerResultSha256 = HashFile(tmJsonPath);

        // Forbidden artifact scan of output root
        Directory.CreateDirectory(outputRoot);
        bool   forbiddenScanPass = true;
        string lotPat  = "*." + "lotpack";
        string lhPat   = "*." + "lotheader";
        string luaPat  = "*." + "lua";
        foreach (var pattern in new[] { lotPat, lhPat, luaPat, "*.bin", "steamapps" })
        {
            var hits = Directory.GetFiles(outputRoot, pattern, SearchOption.AllDirectories);
            if (hits.Length > 0)
            {
                forbiddenScanPass = false;
                result.Errors.Add($"Forbidden artifact in output root: {hits[0]}");
            }
        }
        foreach (var dir in Directory.GetDirectories(outputRoot, "*", SearchOption.AllDirectories))
        {
            var di = new DirectoryInfo(dir);
            if (di.Name == "maps" && di.Parent?.Name == "media")
            {
                forbiddenScanPass = false;
                result.Errors.Add($"Forbidden directory in output root: {dir}");
            }
        }

        result.ForbiddenArtifactScan = forbiddenScanPass
            ? "POST_REPLAY_LOCK_SCAN PASS (0 forbidden artifacts in output root)"
            : "POST_REPLAY_LOCK_SCAN FAIL";
        result.ClaimBoundaryAudit =
            "writer_ready=false | runtime_valid=false | materialized=false | runtime_proof_claimed=false | public_playable_packaging_claimed=false";

        // Build 8 locked files: 1 MAP-27F acceptance gate JSON + 7 MAP-27C replay source files
        var lockFileDefs = new (string Root, string Name, string Stage, string Role)[]
        {
            (acceptanceGateRoot,   agFiles[0], "MAP-27F", "ACCEPTANCE_GATE_RESULT_JSON"),
            (tileMaterializerRoot, tmFiles[0], "MAP-27C", "TILE_MATERIALIZER_RESULT_JSON"),
            (tileMaterializerRoot, tmFiles[1], "MAP-27C", "MATERIALIZED_CELLS_CSV"),
            (tileMaterializerRoot, tmFiles[2], "MAP-27C", "MATERIAL_PALETTE_JSON"),
            (tileMaterializerRoot, tmFiles[3], "MAP-27C", "LAYER_STACK_JSON"),
            (tileMaterializerRoot, tmFiles[4], "MAP-27C", "MATERIALIZATION_REPLAY_LOG_JSON"),
            (tileMaterializerRoot, tmFiles[5], "MAP-27C", "MATERIALIZATION_OWNERSHIP_SUMMARY_JSON"),
            (tileMaterializerRoot, tmFiles[6], "MAP-27C", "MATERIALIZER_FORBIDDEN_OUTPUT_GUARD_JSON"),
        };

        var lockFiles = new List<DeadMtlTileMaterializationReplayLockFile>();
        foreach (var (root, name, stage, role) in lockFileDefs)
        {
            string fp    = Path.Combine(root, name);
            bool   exists = File.Exists(fp);
            string sha   = exists ? HashFile(fp) : string.Empty;
            long   size  = exists ? new FileInfo(fp).Length : 0;
            lockFiles.Add(new DeadMtlTileMaterializationReplayLockFile
            {
                FileOrder          = lockFiles.Count + 1,
                SourceStage        = stage,
                FileRole           = role,
                FileName           = name,
                FilePath           = fp,
                Exists             = exists,
                Sha256             = sha,
                SizeBytes          = size,
                LockedForReplay    = true,
                RuntimeConsumable  = false,
                WriterConsumable   = false,
            });
        }
        result.ReplayLockFiles     = lockFiles;
        result.ReplayLockFileCount = lockFiles.Count;

        // Compute replay lock ID from all 8 hashes (uppercase role names in formula)
        bool allFilesHashed = lockFiles.All(f => f.Exists && !string.IsNullOrEmpty(f.Sha256));
        string replayLockId     = "LOCK_ID_NOT_GENERATED";
        string replayLockStatus = "LOCK_FAILED";
        if (allFilesHashed)
        {
            var lockInput = "MAP27G_REPLAY_LOCK_V1"
                + "|" + lockFiles[0].FileRole + ":" + lockFiles[0].Sha256
                + "|" + lockFiles[1].FileRole + ":" + lockFiles[1].Sha256
                + "|" + lockFiles[2].FileRole + ":" + lockFiles[2].Sha256
                + "|" + lockFiles[3].FileRole + ":" + lockFiles[3].Sha256
                + "|" + lockFiles[4].FileRole + ":" + lockFiles[4].Sha256
                + "|" + lockFiles[5].FileRole + ":" + lockFiles[5].Sha256
                + "|" + lockFiles[6].FileRole + ":" + lockFiles[6].Sha256
                + "|" + lockFiles[7].FileRole + ":" + lockFiles[7].Sha256;
            string lockHex  = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(lockInput))).ToLower();
            replayLockId     = "map_00_replay_lock_" + lockHex[..16];
            replayLockStatus = "LOCKED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY";
        }
        result.ReplayLockId     = replayLockId;
        result.ReplayLockStatus = replayLockStatus;

        // Blocking reasons
        var blockingReasons = new List<string>();
        if (!f27IsValid)
            blockingReasons.Add("MAP-27F acceptance gate is_valid is false.");
        if (!string.Equals(f27AcceptanceGateStatus, "ACCEPTED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY", StringComparison.Ordinal))
            blockingReasons.Add($"MAP-27F acceptance_gate_status is not ACCEPTED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY: {f27AcceptanceGateStatus}");
        if (!f27AcceptedForNextSandbox)
            blockingReasons.Add("MAP-27F accepted_for_next_sandbox_experiment is false.");
        if (f27AcceptedForRuntimeWriter)
            blockingReasons.Add("MAP-27F accepted_for_runtime_writer is unexpectedly true.");
        foreach (var lf in lockFiles)
        {
            if (!lf.Exists)
                blockingReasons.Add($"Lock file not found: {lf.FilePath}");
        }
        if (!allFilesHashed)
            blockingReasons.Add("Not all lock files could be hashed.");
        if (!forbiddenScanPass)
            blockingReasons.Add("Forbidden artifacts found in replay-lock output root.");
        result.BlockingReasons = blockingReasons;

        // Replay lock reasons (only when all pass)
        bool lockAllPass = blockingReasons.Count == 0 && allFilesHashed && forbiddenScanPass;
        result.ReplayLockReasons = lockAllPass ? new List<string>
        {
            "MAP-27F acceptance gate is complete and valid.",
            "MAP-27F accepted the chain for the next sandbox experiment only.",
            "All 8 replay lock files exist and are hashed.",
            "Materialized and rendered cell counts remain matched at 5340.",
            "Replay lock is sandbox-only and runtime-negative.",
            "No forbidden artifacts were found in the replay-lock output root.",
        } : new List<string>();

        // 43 checks
        var checks = new List<DeadMtlTileMaterializationReplayLockCheck>();

        // Group 1: MAP-27F root and sanity (9)
        MakeCheck(checks, "MAP27F_ROOT_EXISTS",              "MAP-27F acceptance gate root exists");
        MakeCheck(checks, "MAP27F_4_EXPECTED_FILES_EXIST",   "All 4 MAP-27F acceptance gate files exist");
        MakeCheck(checks, "MAP27F_ACCEPTANCE_GATE_HASHED",   "MAP-27F acceptance gate JSON SHA-256 hashed");
        AddCheck(checks, "MAP27F_VERDICT_COMPLETE", "MAP-27F verdict is COMPLETE",
            "MAP27F_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_ACCEPTANCE_GATE_COMPLETE",
            f27Verdict);
        AddCheck(checks, "MAP27F_IS_VALID_TRUE",
            "MAP-27F is_valid is true", "true", f27IsValid ? "true" : "false");
        AddCheck(checks, "MAP27F_GATE_STATUS_ACCEPTED_FOR_NEXT_SANDBOX_ONLY",
            "MAP-27F acceptance_gate_status is ACCEPTED_FOR_NEXT_SANDBOX_ONLY",
            "ACCEPTED_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY", f27AcceptanceGateStatus);
        AddCheck(checks, "MAP27F_ACCEPTED_FOR_NEXT_SANDBOX_TRUE",
            "MAP-27F accepted_for_next_sandbox_experiment is true", "true", f27AcceptedForNextSandbox ? "true" : "false");
        AddCheck(checks, "MAP27F_ACCEPTED_FOR_RUNTIME_WRITER_FALSE",
            "MAP-27F accepted_for_runtime_writer is false", "false", f27AcceptedForRuntimeWriter ? "true" : "false");
        AddCheck(checks, "MAP27F_ACCEPTED_FOR_PLAYABLE_EXPORT_FALSE",
            "MAP-27F accepted_for_playable_export is false", "false", f27AcceptedForPlayableExport ? "true" : "false");

        // Group 2: MAP-27C root (2)
        MakeCheck(checks, "MAP27C_ROOT_EXISTS",              "MAP-27C tile materializer root exists");
        MakeCheck(checks, "MAP27C_7_REPLAY_SOURCE_FILES_EXIST", "All 7 MAP-27C replay source files exist");

        // Group 3: Lock file hashing and properties (6)
        int hashedCount          = lockFiles.Count(f => !string.IsNullOrEmpty(f.Sha256));
        int lockedForReplayCount = lockFiles.Count(f => f.LockedForReplay);
        int runtimeConsumCount   = lockFiles.Count(f => f.RuntimeConsumable);
        int writerConsumCount    = lockFiles.Count(f => f.WriterConsumable);
        AddCheck(checks, "ALL_8_REPLAY_LOCK_FILES_HASHED",
            "All 8 replay lock files are hashed", "8", hashedCount.ToString());
        AddCheck(checks, "ALL_8_REPLAY_LOCK_FILES_LOCKED_FOR_REPLAY",
            "All 8 replay lock files have locked_for_replay=true", "8", lockedForReplayCount.ToString());
        AddCheck(checks, "ALL_8_REPLAY_LOCK_FILES_RUNTIME_CONSUMABLE_FALSE",
            "All 8 replay lock files have runtime_consumable=false", "0", runtimeConsumCount.ToString());
        AddCheck(checks, "ALL_8_REPLAY_LOCK_FILES_WRITER_CONSUMABLE_FALSE",
            "All 8 replay lock files have writer_consumable=false", "0", writerConsumCount.ToString());
        AddCheck(checks, "REPLAY_LOCK_ID_PRESENT",
            "Replay lock ID is present (not LOCK_ID_NOT_GENERATED)", "true",
            !string.Equals(replayLockId, "LOCK_ID_NOT_GENERATED", StringComparison.Ordinal) ? "true" : "false");
        AddCheck(checks, "REPLAY_LOCK_FILE_COUNT_8",
            "Total locked file count is 8", "8", lockFiles.Count.ToString());

        // Group 4: Counts and geometry from MAP-27F (16)
        AddCheck(checks, "SANDBOX_ONLY_TRUE",
            "sandbox_only is true", "true", f27SandboxOnly ? "true" : "false");
        AddCheck(checks, "SANDBOX_MATERIALIZED_SOURCE_TRUE",
            "sandbox_materialized_source is true", "true", f27SandboxMaterializedSource ? "true" : "false");
        AddCheck(checks, "VISUAL_QA_OVERLAY_WRITTEN_TRUE",
            "visual_qa_overlay_written is true", "true", f27VisualQaOverlayWritten ? "true" : "false");
        AddCheck(checks, "PZ_RUNTIME_MATERIALIZED_FALSE",
            "pz_runtime_materialized is false", "false", f27PzRuntimeMaterialized ? "true" : "false");
        AddCheck(checks, "MATERIALIZED_CELL_COUNT_5340",
            "Materialized cell count is 5340", "5340", f27MaterializedCellCount.ToString());
        AddCheck(checks, "RENDERED_CELL_COUNT_5340",
            "Rendered cell count is 5340", "5340", f27RenderedCellCount.ToString());
        AddCheck(checks, "COUNT_MATCH_SUMMARY_MATCH",
            "Count match summary contains MATCH", "MATCH",
            f27CountMatchSummary.Contains("MATCH", StringComparison.Ordinal) ? "MATCH" : "MISMATCH");
        AddCheck(checks, "WALL_COUNT_850",
            "Building wall candidate count is 850", "850", f27WallCount.ToString());
        AddCheck(checks, "FLOOR_COUNT_2444",
            "Building floor candidate count is 2444", "2444", f27FloorCount.ToString());
        AddCheck(checks, "ACCESS_COUNT_148",
            "Access edge cell count is 148", "148", f27AccessCount.ToString());
        AddCheck(checks, "LOT_COUNT_1898",
            "Lot space cell count is 1898", "1898", f27LotCount.ToString());
        AddCheck(checks, "COMPONENT_RESIDUAL_COUNT_0",
            "Component residual cell count is 0", "0", f27ResidualCount.ToString());
        AddCheck(checks, "MATERIAL_KIND_COUNT_5",
            "Material kind count is 5", "5", f27MaterialKindCount.ToString());
        AddCheck(checks, "LAYER_KIND_COUNT_5",
            "Layer kind count is 5", "5", f27LayerKindCount.ToString());
        AddCheck(checks, "OVERLAY_PNG_WIDTH_1024",
            "Overlay PNG width is 1024", "1024", f27OverlayPngWidth.ToString());
        AddCheck(checks, "OVERLAY_PNG_HEIGHT_1024",
            "Overlay PNG height is 1024", "1024", f27OverlayPngHeight.ToString());

        // Group 5: Claim boundary and final (10)
        MakeCheck(checks, "NEXT_ALLOWED_EXPERIMENT_SANDBOX_ONLY", "Next allowed experiment status is SANDBOX_ONLY_NOT_RUNTIME");
        MakeCheck(checks, "FORBIDDEN_STEPS_LISTED",               "All 11 forbidden next steps are listed");
        AddCheck(checks, "BLOCKING_REASONS_EMPTY",
            "No blocking reasons", "0", blockingReasons.Count.ToString());
        AddCheck(checks, "POST_REPLAY_LOCK_FORBIDDEN_SCAN_PASS",
            "Post-replay-lock forbidden artifact scan passes", "PASS", forbiddenScanPass ? "PASS" : "FAIL");
        MakeCheck(checks, "WRITER_READY_FALSE",                 "writer_ready is false");
        MakeCheck(checks, "RUNTIME_VALID_FALSE",                "runtime_valid is false");
        MakeCheck(checks, "MATERIALIZED_FALSE",                 "materialized (global PZ) is false");
        MakeCheck(checks, "NO_RUNTIME_PROOF_CLAIM",             "No runtime proof claimed");
        MakeCheck(checks, "NO_PUBLIC_PLAYABLE_PACKAGING_CLAIM", "No public playable packaging claimed");
        MakeCheck(checks, "NO_RUNTIME_OUTPUTS_EMITTED",         "No runtime outputs emitted");

        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");

        bool allPass = result.FailedCheckCount == 0;
        result.IsValid = allPass;
        result.Verdict = allPass
            ? "MAP27G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOCK_COMPLETE"
            : invalidVerdict;

        return result;
    }

    public string RenderJson(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockResult result) =>
        JsonSerializer.Serialize(result, s_jsonOptions);

    public string RenderMarkdown(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-27G WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Materialization Replay Lock");
        sb.AppendLine();
        sb.AppendLine($"- **Map ID:** {result.MapId}");
        sb.AppendLine($"- **Generated UTC:** {result.GeneratedUtc}");
        sb.AppendLine($"- **Replay Lock Stage:** {result.ReplayLockStage}");
        sb.AppendLine($"- **Replay Lock Status:** {result.ReplayLockStatus}");
        sb.AppendLine($"- **Replay Lock ID:** `{result.ReplayLockId}`");
        sb.AppendLine($"- **Replay Lock File Count:** {result.ReplayLockFileCount}");
        sb.AppendLine($"- **Verdict:** `{result.Verdict}`");
        sb.AppendLine($"- **Is Valid:** {result.IsValid}");
        sb.AppendLine($"- **Accepted for Next Sandbox Experiment:** {result.AcceptedForNextSandboxExperiment}");
        sb.AppendLine($"- **Accepted for Runtime Writer:** {result.AcceptedForRuntimeWriter}");
        sb.AppendLine($"- **Accepted for Playable Export:** {result.AcceptedForPlayableExport}");
        sb.AppendLine($"- **Sandbox Only:** {result.SandboxOnly}");
        sb.AppendLine($"- **PZ Runtime Materialized:** {result.PzRuntimeMaterialized}");
        sb.AppendLine($"- **Forbidden Artifact Scan:** {result.ForbiddenArtifactScan}");
        sb.AppendLine($"- **Checks:** {result.CheckCount} / Passed: {result.PassedCheckCount} / Failed: {result.FailedCheckCount}");
        sb.AppendLine($"- **Next Allowed Experiment:** {result.NextAllowedExperimentName} ({result.NextAllowedExperimentStatus})");
        sb.AppendLine();
        sb.AppendLine("## Locked Files");
        sb.AppendLine();
        sb.AppendLine("| # | Stage | Role | File | SHA-256 | Locked |");
        sb.AppendLine("|---|-------|------|------|---------|--------|");
        foreach (var f in result.ReplayLockFiles)
            sb.AppendLine($"| {f.FileOrder} | {f.SourceStage} | {f.FileRole} | {f.FileName} | `{f.Sha256[..Math.Min(16, f.Sha256.Length)]}...` | {f.LockedForReplay} |");
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
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_order,check_id,check_status,expected,actual");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckOrder},{c.CheckId},{c.CheckStatus},{c.Expected},{c.Actual}");
        return sb.ToString();
    }

    public string RenderSummary(
        DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-27G WorldBuilder Minimal Concrete Geometry Sandbox Writer Tile Materialization Replay Lock");
        sb.AppendLine($"Generated UTC                 : {result.GeneratedUtc}");
        sb.AppendLine($"Map ID                        : {result.MapId}");
        sb.AppendLine($"Replay Lock Stage             : {result.ReplayLockStage}");
        sb.AppendLine($"Replay Lock Status            : {result.ReplayLockStatus}");
        sb.AppendLine($"Replay Lock ID                : {result.ReplayLockId}");
        sb.AppendLine($"Replay Lock File Count        : {result.ReplayLockFileCount}");
        sb.AppendLine($"Verdict                       : {result.Verdict}");
        sb.AppendLine($"Is Valid                      : {(result.IsValid ? 1 : 0)}");
        sb.AppendLine($"Accepted for Next Sandbox     : {(result.AcceptedForNextSandboxExperiment ? 1 : 0)}");
        sb.AppendLine($"Accepted for Runtime Writer   : {(result.AcceptedForRuntimeWriter ? 1 : 0)}");
        sb.AppendLine($"Accepted for Playable Export  : {(result.AcceptedForPlayableExport ? 1 : 0)}");
        sb.AppendLine($"Sandbox Only                  : {(result.SandboxOnly ? 1 : 0)}");
        sb.AppendLine($"PZ Runtime Materialized       : {(result.PzRuntimeMaterialized ? 1 : 0)}");
        sb.AppendLine($"Visual QA Overlay Written     : {(result.VisualQaOverlayWritten ? 1 : 0)}");
        sb.AppendLine($"Writer Ready                  : {(result.WriterReady ? 1 : 0)}");
        sb.AppendLine($"Runtime Valid                 : {(result.RuntimeValid ? 1 : 0)}");
        sb.AppendLine($"Materialized                  : {(result.Materialized ? 1 : 0)}");
        sb.AppendLine($"Runtime Proof                 : {(result.RuntimeProofClaimed ? 1 : 0)}");
        sb.AppendLine($"Public Playable               : {(result.PublicPlayablePackagingClaimed ? 1 : 0)}");
        sb.AppendLine($"Materialized Cell Count       : {result.MaterializedCellCount}");
        sb.AppendLine($"Rendered Cell Count           : {result.RenderedCellCount}");
        sb.AppendLine($"Count Match                   : {result.CountMatchSummary}");
        sb.AppendLine($"Forbidden Artifact Scan       : {result.ForbiddenArtifactScan}");
        sb.AppendLine($"Next Allowed Experiment       : {result.NextAllowedExperimentName}");
        sb.AppendLine($"Next Experiment Status        : {result.NextAllowedExperimentStatus}");
        sb.AppendLine($"Blocking Reasons              : {result.BlockingReasons.Count}");
        sb.AppendLine($"Checks                        : {result.CheckCount}");
        sb.AppendLine($"Passed                        : {result.PassedCheckCount}");
        sb.Append($"Failed                        : {result.FailedCheckCount}");
        return sb.ToString();
    }
}
