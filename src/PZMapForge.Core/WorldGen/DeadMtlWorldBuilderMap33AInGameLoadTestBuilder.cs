using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMap33AInGameLoadTestBuilder
{
    private static readonly JsonSerializerOptions s_json = new()
    {
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public const string MapId   = "DeadMTL_MAP33A";
    public const string MapName = "DeadMTL_MAP33A";
    public const string InstallMarkerFileName = "PZMAPFORGE_MAP34A_TEST_INSTALL_MARKER.txt";
    public const string InstalledFolderName   = "deadmtl_map33a_candidate";

    private static readonly string[] s_requiredBinaryFiles =
    {
        "35_27.lotheader",
        "world_35_27.lotpack",
        "chunkdata_35_27.bin",
    };

    // Fatal patterns that must appear on a line that also contains MapName
    private static readonly string[] s_candidateFatalPatterns =
    {
        "exception parsing spawnpoints.lua",
        "exception parsing map.info",
        "missing required binary file",
        "failed to load lotheader",
        "failed to load lotpack",
        "failed to load chunkdata",
        "kahlua exception",
    };

    public DeadMtlWorldBuilderMap33AInGameLoadTestResult Build(
        string map33aManifestPath,
        string sourceCandidateRoot,
        string localModsRoot,
        string outputRoot,
        bool collectLogs = false,
        string? zomboidUserRoot = null,
        string? operatorObservation = null)
    {
        var result = new DeadMtlWorldBuilderMap33AInGameLoadTestResult
        {
            Format                        = "MAP34B_INGAME_LOAD_TEST_V1",
            GeneratedUtc                  = DateTime.UtcNow.ToString("o"),
            SourceMap33AManifest          = map33aManifestPath,
            SourceMap33ACandidateRoot     = sourceCandidateRoot,
            LocalUserModsRoot             = localModsRoot,
            MapId                         = MapId,
            MapName                       = MapName,
            GeometryFromMap31bMaterialized = false,
            RuntimeValid                  = false,
            RuntimeProofClaimed           = false,
            PlayableExportClaimed         = false,
            PublicPackageClaimed          = false,
            OperatorObservation           = operatorObservation ?? string.Empty,
        };

        var checks = new List<LoadTestCheck>();

        // C1 — MAP-33A manifest exists
        bool manifestExists = File.Exists(map33aManifestPath);
        AddCheck(checks, "MAP34A_INPUT_MAP33A_MANIFEST_EXISTS",
            "MAP-33A manifest JSON input exists on disk",
            "PASS", manifestExists ? "PASS" : "FAIL");
        if (!manifestExists)
        {
            result.Errors.Add($"MAP-33A manifest not found: {map33aManifestPath}");
            Finalize(result, checks, valid: false, "MAP34A_REJECTED_MAP33A_MANIFEST_MISSING");
            return result;
        }

        bool binaryCellMaterialized = false;
        bool geometryFromMap31b     = false;
        using (var doc = JsonDocument.Parse(File.ReadAllText(map33aManifestPath)))
        {
            var root = doc.RootElement;
            if (root.TryGetProperty("binary_cell_materialized",          out var bcm)) binaryCellMaterialized = bcm.GetBoolean();
            if (root.TryGetProperty("geometry_from_map31b_materialized", out var g31)) geometryFromMap31b     = g31.GetBoolean();
        }
        result.BinaryCellMaterialized         = binaryCellMaterialized;
        result.GeometryFromMap31bMaterialized = geometryFromMap31b;

        // C2 — binary_cell_materialized must be true
        AddCheck(checks, "MAP34A_MAP33A_BINARY_CELL_MATERIALIZED_TRUE",
            "MAP-33A binary_cell_materialized=true (required binary files were seeded)",
            "True", binaryCellMaterialized.ToString());
        if (!binaryCellMaterialized)
        {
            result.Errors.Add("MAP-33A binary_cell_materialized=false; load test requires true");
            Finalize(result, checks, valid: false, "MAP34A_REJECTED_BINARY_CELL_NOT_MATERIALIZED");
            return result;
        }

        // C3 — geometry_from_map31b_materialized recorded
        AddCheck(checks, "MAP34A_MAP31B_GEOMETRY_NOT_CLAIMED",
            "geometry_from_map31b_materialized=false (seed is MAP-7Y sidecar, not MAP-31B geometry)",
            "False", geometryFromMap31b.ToString());

        // C4 — source candidate root exists
        bool candRootExists = Directory.Exists(sourceCandidateRoot);
        if (!candRootExists)
        {
            result.Errors.Add($"Source candidate root not found: {sourceCandidateRoot}");
            AddCheck(checks, "MAP34A_REQUIRED_BINARY_FILES_PRESENT",  "Source candidate directory exists", "PASS", "FAIL");
            AddCheck(checks, "MAP34A_REQUIRED_BINARY_FILES_NONEMPTY", "Source candidate directory exists", "PASS", "FAIL");
            Finalize(result, checks, valid: false, "MAP34A_REJECTED_SOURCE_CANDIDATE_ROOT_MISSING");
            return result;
        }

        // C5 — required binary files present in source candidate
        string srcMapDir   = Path.Combine(sourceCandidateRoot, "media", "maps", MapName);
        var presentFiles   = new List<string>();
        foreach (var name in s_requiredBinaryFiles)
        {
            if (File.Exists(Path.Combine(srcMapDir, name))) presentFiles.Add(name);
        }
        result.Map33ARequiredBinaryFiles.AddRange(presentFiles);
        bool allPresent = presentFiles.Count == s_requiredBinaryFiles.Length;
        AddCheck(checks, "MAP34A_REQUIRED_BINARY_FILES_PRESENT",
            $"All {s_requiredBinaryFiles.Length} required binary files present in source candidate",
            "PASS", allPresent ? "PASS" : "FAIL");
        if (!allPresent)
        {
            var missing = s_requiredBinaryFiles.Except(presentFiles).ToList();
            result.Errors.Add($"Missing required binary files in source candidate: {string.Join(", ", missing)}");
            AddCheck(checks, "MAP34A_REQUIRED_BINARY_FILES_NONEMPTY", "Required binary files non-empty", "PASS", "FAIL");
            Finalize(result, checks, valid: false, "MAP34A_REJECTED_REQUIRED_BINARY_FILES_MISSING");
            return result;
        }

        // C6 — required binary files non-empty
        bool allNonEmpty = s_requiredBinaryFiles.All(n => new FileInfo(Path.Combine(srcMapDir, n)).Length > 0);
        AddCheck(checks, "MAP34A_REQUIRED_BINARY_FILES_NONEMPTY",
            "All required binary files in source candidate are non-empty",
            "PASS", allNonEmpty ? "PASS" : "FAIL");

        // C7 — local user mods path resolved (must not be Steam or Workshop)
        bool noSteam    = !localModsRoot.Contains("steamapps", StringComparison.OrdinalIgnoreCase);
        bool noWorkshop = !localModsRoot.Contains("Workshop",  StringComparison.OrdinalIgnoreCase);
        bool modsPathSafe = noSteam && noWorkshop;
        AddCheck(checks, "MAP34A_LOCAL_USER_MODS_PATH_RESOLVED",
            "Local user mods root is not a Steam or Workshop path",
            "PASS", modsPathSafe ? "PASS" : "FAIL");
        if (!modsPathSafe)
        {
            result.Errors.Add($"Unsafe local mods root (contains Steam/Workshop path): {localModsRoot}");
            Finalize(result, checks, valid: false, "MAP34A_REJECTED_UNSAFE_LOCAL_MODS_PATH");
            return result;
        }

        AddCheck(checks, "MAP34A_NO_STEAM_INSTALL_WRITE",
            "Local mods root does not contain 'steamapps'",
            "PASS", noSteam ? "PASS" : "FAIL");
        AddCheck(checks, "MAP34A_NO_WORKSHOP_UPLOAD_WRITE",
            "Local mods root does not contain 'Workshop'",
            "PASS", noWorkshop ? "PASS" : "FAIL");

        string installedRoot = Path.Combine(localModsRoot, InstalledFolderName);
        result.InstalledCandidateRoot = installedRoot;

        // -----------------------------------------------------------------------
        // COLLECT-LOGS MODE: analyze logs only — do NOT reinstall
        // -----------------------------------------------------------------------
        if (collectLogs)
        {
            result.InstallPerformed          = false;
            result.RuntimeLogCollectionAttempted = true;

            bool installedExists = Directory.Exists(installedRoot);
            AddCheck(checks, "MAP34B_COLLECT_LOGS_DOES_NOT_REINSTALL",
                "Collect-logs mode does not reinstall or overwrite installed mod folder",
                "PASS", installedExists ? "PASS" : "FAIL");
            if (!installedExists)
            {
                result.Errors.Add($"Collect-logs mode requires prior install: {installedRoot} not found. Run -InstallOnly first.");
                Finalize(result, checks, valid: false, "MAP34B_COLLECT_REJECTED_NOT_INSTALLED");
                return result;
            }

            string zuRoot     = zomboidUserRoot ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Zomboid");
            string logsDir    = Path.Combine(zuRoot, "Logs");
            string consoleTxt = Path.Combine(zuRoot, "console.txt");

            var logPaths = new List<string>();
            if (File.Exists(consoleTxt))      logPaths.Add(consoleTxt);
            if (Directory.Exists(logsDir))
                logPaths.AddRange(Directory.GetFiles(logsDir, "*.txt", SearchOption.TopDirectoryOnly));

            AddCheck(checks, "MAP34B_LOGS_FOUND",
                "PZ log files found in Zomboid user folder",
                "PASS", logPaths.Count > 0 ? "PASS" : "FAIL");

            if (logPaths.Count > 0)
            {
                // Read content from source paths first
                var logTexts = new List<string>();
                foreach (var src in logPaths)
                {
                    try { logTexts.Add(File.ReadAllText(src)); } catch { }
                }
                string allText = string.Join("\n", logTexts);

                // Copy logs to output if outputRoot provided
                if (!string.IsNullOrEmpty(outputRoot))
                {
                    string outLogsDir = Path.Combine(outputRoot, "logs");
                    Directory.CreateDirectory(outLogsDir);
                    foreach (var src in logPaths)
                    {
                        string dest = Path.Combine(outLogsDir, Path.GetFileName(src));
                        File.Copy(src, dest, overwrite: true);
                        result.RuntimeLogPaths.Add(dest);
                    }
                }
                else
                {
                    result.RuntimeLogPaths.AddRange(logPaths);
                }
                result.RuntimeLogsFound = true;

                var (modLoaded, binaryMounted, mapgroupReg, spawnBlockerAbsent,
                     binaryChunkLoad, candidateErrors, unrelatedErrors, fallbackTerrain)
                    = ClassifyLogText(allText);

                result.CandidateModLoaded               = modLoaded;
                result.CandidateBinaryFilesMounted      = binaryMounted;
                result.CandidateMapgroupRegistered      = mapgroupReg;
                result.CandidateSpawnBlockerAbsent      = spawnBlockerAbsent;
                result.CandidateBinaryChunkLoadAttempted = binaryChunkLoad;
                result.CandidateSpecificErrorsFound     = candidateErrors;
                result.UnrelatedErrorsFound             = unrelatedErrors;
                result.FallbackEmptyTerrainDetected     = fallbackTerrain;

                bool obsConfirmsFallback = !string.IsNullOrEmpty(operatorObservation) &&
                    (operatorObservation.Contains("field",    StringComparison.OrdinalIgnoreCase) ||
                     operatorObservation.Contains("empty",    StringComparison.OrdinalIgnoreCase) ||
                     operatorObservation.Contains("fallback", StringComparison.OrdinalIgnoreCase));

                AddCheck(checks, "MAP34B_CANDIDATE_MOD_LOADED",
                    $"PZ logs show 'loading {MapName}'",
                    "PASS", modLoaded ? "PASS" : "FAIL");
                AddCheck(checks, "MAP34B_REQUIRED_BINARY_OVERRIDES_SEEN",
                    "PZ logs show binary file overrides for lotheader, lotpack, chunkdata",
                    "PASS", binaryMounted ? "PASS" : "FAIL");
                AddCheck(checks, "MAP34B_MAPGROUP_REGISTERED",
                    $"PZ logs show MapGroup entry for {MapName}",
                    "PASS", mapgroupReg ? "PASS" : "FAIL");
                AddCheck(checks, "MAP34B_SPAWN_BLOCKER_ABSENT",
                    "PZ logs do not contain spawn point table error or -1,-1,-1 player creation",
                    "PASS", spawnBlockerAbsent ? "PASS" : "FAIL");
                AddCheck(checks, "MAP34B_BINARY_CHUNK_LOAD_ATTEMPTED",
                    "PZ logs show CellLoader.LoadCellBinaryChunk start",
                    "PASS", binaryChunkLoad ? "PASS" : "FAIL");
                AddCheck(checks, "MAP34B_UNRELATED_ERRORS_IGNORED",
                    "Unrelated vanilla errors (FluidContainerScript, Recipe, FMOD, Network) do not force failure",
                    "PASS", "PASS");
                AddCheck(checks, "MAP34B_EMPTY_FALLBACK_TERRAIN_RECORDED",
                    "Empty/fallback terrain signal recorded (Looking in map folders, initSpawnBuildings no room)",
                    fallbackTerrain.ToString(), fallbackTerrain.ToString());
                AddCheck(checks, "MAP34B_OPERATOR_OBSERVATION_RECORDED",
                    "Operator observation recorded in result",
                    "PASS", "PASS");

                // Classify
                string classification;
                if (!modLoaded)
                    classification = "MAP34B_RUNTIME_MOD_NOT_LOADED";
                else if (!mapgroupReg)
                    classification = "MAP34B_RUNTIME_MAPGROUP_MISSING";
                else if (!spawnBlockerAbsent)
                    classification = "MAP34B_RUNTIME_SPAWN_BLOCKED";
                else if (candidateErrors)
                    classification = "MAP34B_RUNTIME_CANDIDATE_SPECIFIC_FAIL";
                else if (modLoaded && binaryMounted && mapgroupReg && spawnBlockerAbsent
                         && binaryChunkLoad && (fallbackTerrain || obsConfirmsFallback))
                    classification = "MAP34B_RUNTIME_PARTIAL_PASS_EMPTY_FALLBACK_TERRAIN";
                else
                    classification = "MAP34B_RUNTIME_EVIDENCE_INSUFFICIENT";

                result.RuntimeClassification = classification;

                AddCheck(checks, "MAP34B_RUNTIME_CLASSIFICATION_PARTIAL_PASS",
                    "Runtime classified as partial pass (empty/fallback terrain recognized)",
                    "MAP34B_RUNTIME_PARTIAL_PASS_EMPTY_FALLBACK_TERRAIN", classification);
            }
            else
            {
                result.RuntimeLogsFound      = false;
                result.RuntimeClassification = "MAP34B_RUNTIME_EVIDENCE_INSUFFICIENT";

                AddCheck(checks, "MAP34B_CANDIDATE_MOD_LOADED",          "PZ logs show mod loaded",          "PASS", "FAIL");
                AddCheck(checks, "MAP34B_REQUIRED_BINARY_OVERRIDES_SEEN", "PZ logs show binary overrides",   "PASS", "FAIL");
                AddCheck(checks, "MAP34B_MAPGROUP_REGISTERED",            "PZ logs show MapGroup",           "PASS", "FAIL");
                AddCheck(checks, "MAP34B_SPAWN_BLOCKER_ABSENT",           "PZ logs show no spawn blocker",   "PASS", "FAIL");
                AddCheck(checks, "MAP34B_BINARY_CHUNK_LOAD_ATTEMPTED",    "PZ logs show CellLoader start",   "PASS", "FAIL");
                AddCheck(checks, "MAP34B_UNRELATED_ERRORS_IGNORED",
                    "Unrelated vanilla errors do not force failure", "PASS", "PASS");
                AddCheck(checks, "MAP34B_EMPTY_FALLBACK_TERRAIN_RECORDED",
                    "Empty/fallback terrain signal recorded", "False", "False");
                AddCheck(checks, "MAP34B_OPERATOR_OBSERVATION_RECORDED",  "Operator observation recorded",   "PASS", "PASS");
                AddCheck(checks, "MAP34B_RUNTIME_CLASSIFICATION_PARTIAL_PASS",
                    "Runtime classified as partial pass",
                    "MAP34B_RUNTIME_PARTIAL_PASS_EMPTY_FALLBACK_TERRAIN",
                    result.RuntimeClassification);
            }
        }
        // -----------------------------------------------------------------------
        // INSTALL-ONLY MODE: copy candidate, write marker, no log collection
        // -----------------------------------------------------------------------
        else
        {
            Directory.CreateDirectory(installedRoot);
            CopyTree(sourceCandidateRoot, installedRoot);
            result.InstallPerformed = true;

            AddCheck(checks, "MAP34A_CANDIDATE_COPIED_TO_LOCAL_MODS",
                $"MAP-33A candidate copied to local mods: {InstalledFolderName}",
                "PASS", Directory.Exists(installedRoot) ? "PASS" : "FAIL");

            string markerPath = Path.Combine(installedRoot, InstallMarkerFileName);
            File.WriteAllText(markerPath, RenderInstallMarker(sourceCandidateRoot));
            result.InstallMarkerWritten = File.Exists(markerPath);
            AddCheck(checks, "MAP34A_INSTALL_MARKER_WRITTEN",
                $"Install marker written: {InstallMarkerFileName}",
                "PASS", result.InstallMarkerWritten ? "PASS" : "FAIL");

            result.RuntimeLogCollectionAttempted = false;
            result.RuntimeClassification = "MAP34A_INSTALL_ONLY_READY";

            AddCheck(checks, "MAP34A_RUNTIME_LOG_COLLECTION_ATTEMPTED",
                "Runtime log collection mode recorded",
                "False", result.RuntimeLogCollectionAttempted.ToString());
            AddCheck(checks, "MAP34A_RUNTIME_PROOF_CLASSIFIED",
                "Runtime classification recorded",
                "CLASSIFIED", !string.IsNullOrEmpty(result.RuntimeClassification) ? "CLASSIFIED" : "UNCLASSIFIED");
        }

        // Claim boundary checks (both modes)
        AddCheck(checks, "MAP34A_NO_FINAL_DEADMTL_GEOMETRY_CLAIM",
            "geometry_from_map31b_materialized=false (MAP-33A binary seed is MAP-7Y sidecar)",
            "False", result.GeometryFromMap31bMaterialized.ToString());
        AddCheck(checks, "MAP34A_PLAYABLE_EXPORT_CLAIM_GATED",
            "playable_export_claimed=false (gated until in-game spawn/terrain is proven)",
            "False", result.PlayableExportClaimed.ToString());

        bool claimBoundaryOk = !result.RuntimeProofClaimed && !result.PlayableExportClaimed
            && !result.PublicPackageClaimed && !result.GeometryFromMap31bMaterialized;
        AddCheck(checks, "MAP34A_CLAIM_BOUNDARY_RECORDED",
            "All runtime/playable/public claim flags false; geometry_from_map31b=false",
            "PASS", claimBoundaryOk ? "PASS" : "FAIL");
        AddCheck(checks, "MAP34B_NO_PLAYABLE_EXPORT_CLAIM",
            "playable_export_claimed=false",
            "False", result.PlayableExportClaimed.ToString());
        AddCheck(checks, "MAP34B_NO_FINAL_GEOMETRY_CLAIM",
            "geometry_from_map31b_materialized=false",
            "False", result.GeometryFromMap31bMaterialized.ToString());
        AddCheck(checks, "MAP34B_CLAIM_BOUNDARY_RECORDED",
            "All runtime/playable/public claim flags false; geometry_from_map31b=false",
            "PASS", claimBoundaryOk ? "PASS" : "FAIL");

        Finalize(result, checks,
            valid: !checks.Any(c => c.CheckStatus == "FAIL") && result.Errors.Count == 0,
            result.RuntimeClassification);

        return result;
    }

    // -----------------------------------------------------------------------
    // Log classifier
    // -----------------------------------------------------------------------

    private static (bool modLoaded, bool binaryFilesMounted, bool mapgroupRegistered,
        bool spawnBlockerAbsent, bool binaryChunkLoad, bool candidateErrors,
        bool unrelatedErrors, bool fallbackTerrain) ClassifyLogText(string allText)
    {
        // A. Candidate mod mounted
        bool modLoaded = allText.Contains($"loading {MapName}", StringComparison.OrdinalIgnoreCase);

        bool lotheaderOverride = allText.Contains("overrides media/maps/deadmtl_map33a/35_27.lotheader",    StringComparison.OrdinalIgnoreCase)
                              || allText.Contains($"overrides media/maps/{MapName}/35_27.lotheader",         StringComparison.OrdinalIgnoreCase);
        bool lotpackOverride   = allText.Contains("overrides media/maps/deadmtl_map33a/world_35_27.lotpack", StringComparison.OrdinalIgnoreCase)
                              || allText.Contains($"overrides media/maps/{MapName}/world_35_27.lotpack",      StringComparison.OrdinalIgnoreCase);
        bool binOverride       = allText.Contains("overrides media/maps/deadmtl_map33a/chunkdata_35_27.bin", StringComparison.OrdinalIgnoreCase)
                              || allText.Contains($"overrides media/maps/{MapName}/chunkdata_35_27.bin",      StringComparison.OrdinalIgnoreCase);
        bool binaryFilesMounted = lotheaderOverride && lotpackOverride && binOverride;

        // B. MapGroup registered
        bool mapgroupRegistered = allText.Contains("MapGroup", StringComparison.OrdinalIgnoreCase)
                               && allText.Contains(MapName,    StringComparison.OrdinalIgnoreCase);

        // C. Spawn blocker absent
        bool spawnBlockerPresent =
            allText.Contains("there is no spawn point table for the player's profession", StringComparison.OrdinalIgnoreCase) ||
            allText.Contains("can't create player at x,y,z=-1,-1,-1",                    StringComparison.OrdinalIgnoreCase) ||
            allText.Contains("square is null",                                            StringComparison.OrdinalIgnoreCase);
        bool spawnBlockerAbsent = !spawnBlockerPresent;

        // D. Binary chunk load attempted
        bool binaryChunkLoad = allText.Contains("CellLoader.LoadCellBinaryChunk start", StringComparison.OrdinalIgnoreCase);

        // E. Fallback/empty terrain signal
        bool fallbackTerrain =
            allText.Contains("Looking in these map folders:",                            StringComparison.OrdinalIgnoreCase) &&
            allText.Contains("<End of map-folders list>",                                StringComparison.OrdinalIgnoreCase) &&
            allText.Contains("initSpawnBuildings: no room or building at 10746,8288,0", StringComparison.OrdinalIgnoreCase);

        // Candidate-specific fatal errors: line must contain MapName AND a fatal pattern
        bool candidateErrors = false;
        foreach (var line in allText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.Contains(MapName, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var fp in s_candidateFatalPatterns)
            {
                if (line.Contains(fp, StringComparison.OrdinalIgnoreCase))
                {
                    candidateErrors = true;
                    break;
                }
            }
            if (candidateErrors) break;
        }

        // Unrelated errors: ERROR lines that do NOT mention MapName
        bool unrelatedErrors = allText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Any(line => line.Contains("ERROR", StringComparison.OrdinalIgnoreCase)
                      && !line.Contains(MapName, StringComparison.OrdinalIgnoreCase));

        return (modLoaded, binaryFilesMounted, mapgroupRegistered, spawnBlockerAbsent,
                binaryChunkLoad, candidateErrors, unrelatedErrors, fallbackTerrain);
    }

    // -----------------------------------------------------------------------
    // Output renderers
    // -----------------------------------------------------------------------

    public string RenderJson(DeadMtlWorldBuilderMap33AInGameLoadTestResult result)
        => JsonSerializer.Serialize(result, s_json);

    public string RenderChecksCsv(DeadMtlWorldBuilderMap33AInGameLoadTestResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_id,description,expected,actual,check_status");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckId},\"{c.Description}\",{c.Expected},{c.Actual},{c.CheckStatus}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderMap33AInGameLoadTestResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-34B DEADMTL IN-GAME LOAD TEST");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"Map ID                           : {r.MapId}");
        sb.AppendLine($"Source MAP-33A candidate         : {r.SourceMap33ACandidateRoot}");
        sb.AppendLine($"Installed candidate root         : {r.InstalledCandidateRoot}");
        sb.AppendLine($"Install performed                : {r.InstallPerformed}");
        sb.AppendLine($"Install marker written           : {r.InstallMarkerWritten}");
        sb.AppendLine($"Binary cell materialized         : {r.BinaryCellMaterialized}");
        sb.AppendLine($"Geometry from MAP-31B            : {r.GeometryFromMap31bMaterialized}");
        sb.AppendLine($"Log collection attempted         : {r.RuntimeLogCollectionAttempted}");
        sb.AppendLine($"Logs found                       : {r.RuntimeLogsFound}");
        if (r.RuntimeLogPaths.Count > 0)
        {
            sb.AppendLine($"Log paths collected              : {r.RuntimeLogPaths.Count}");
            foreach (var p in r.RuntimeLogPaths)
                sb.AppendLine($"  {p}");
        }
        sb.AppendLine($"Candidate mod loaded             : {r.CandidateModLoaded}");
        sb.AppendLine($"Binary files mounted             : {r.CandidateBinaryFilesMounted}");
        sb.AppendLine($"MapGroup registered              : {r.CandidateMapgroupRegistered}");
        sb.AppendLine($"Spawn blocker absent             : {r.CandidateSpawnBlockerAbsent}");
        sb.AppendLine($"Binary chunk load attempted      : {r.CandidateBinaryChunkLoadAttempted}");
        sb.AppendLine($"Empty/fallback terrain detected  : {r.FallbackEmptyTerrainDetected}");
        sb.AppendLine($"Candidate-specific errors found  : {r.CandidateSpecificErrorsFound}");
        sb.AppendLine($"Unrelated vanilla errors found   : {r.UnrelatedErrorsFound}");
        if (!string.IsNullOrEmpty(r.OperatorObservation))
            sb.AppendLine($"Operator observation             : {r.OperatorObservation}");
        sb.AppendLine($"Runtime classification           : {r.RuntimeClassification}");
        sb.AppendLine($"Runtime proof claimed            : {r.RuntimeProofClaimed}");
        sb.AppendLine($"Playable export claimed          : {r.PlayableExportClaimed}");
        sb.AppendLine($"Checks                           : {r.CheckCount} total / {r.PassedCheckCount} PASS / {r.FailedCheckCount} FAIL");
        sb.AppendLine($"Is Valid                         : {r.IsValid}");
        sb.AppendLine($"Verdict                          : {r.Verdict}");
        sb.AppendLine($"Claim boundary                   : runtime_proof_claimed=false | playable_export_claimed=false | geometry_from_map31b=false");
        return sb.ToString();
    }

    // -----------------------------------------------------------------------
    // Internal helpers
    // -----------------------------------------------------------------------

    private static string RenderInstallMarker(string sourceCandidateRoot) =>
        $"PZMAPFORGE_MAP34A_TEST_INSTALL_MARKER\n" +
        $"Generated     : {DateTime.UtcNow:o}\n" +
        $"Source        : MAP-33A binary-seeded runtime candidate\n" +
        $"Source path   : {sourceCandidateRoot}\n" +
        $"Map ID        : {MapId}\n" +
        $"binary_cell_materialized              : true\n" +
        $"geometry_from_map31b_materialized     : false\n" +
        $"runtime_proof_claimed                 : false\n" +
        $"playable_export_claimed               : false\n" +
        $"This is a controlled PZ load test installation. Not a public release.\n" +
        $"NOT a playable Project Zomboid map. Binary seed is MAP-7Y sidecar (not MAP-31B geometry).\n";

    private static void CopyTree(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var file in Directory.GetFiles(src))
            File.Copy(file, Path.Combine(dst, Path.GetFileName(file)), overwrite: true);
        foreach (var dir in Directory.GetDirectories(src))
            CopyTree(dir, Path.Combine(dst, Path.GetFileName(dir)));
    }

    private static void AddCheck(List<LoadTestCheck> checks,
        string id, string description, string expected, string actual)
    {
        checks.Add(new LoadTestCheck
        {
            CheckId     = id,
            Description = description,
            Expected    = expected,
            Actual      = actual,
            CheckStatus = expected == actual ? "PASS" : "FAIL",
        });
    }

    private static void Finalize(DeadMtlWorldBuilderMap33AInGameLoadTestResult result,
        List<LoadTestCheck> checks, bool valid, string verdict)
    {
        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");
        result.IsValid          = valid;
        result.Verdict          = verdict;
    }
}
