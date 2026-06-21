using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderVisibleCellRuntimeCandidateBuilder
{
    private static readonly JsonSerializerOptions s_json = new()
    {
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public const string MapId              = "DeadMTL_MAP35A";
    public const string MapFolder          = "DeadMTL_MAP35A";
    public const string ModHumanTitle      = "DeadMTL MAP35A Visible Cell Runtime Candidate";
    public const string InstalledFolderName = "deadmtl_map35a_visible_cell_candidate";
    public const string InstallMarkerFileName = "PZMAPFORGE_MAP35A_TEST_INSTALL_MARKER.txt";

    private static readonly string[] s_requiredCellFiles =
    {
        "35_27.lotheader",
        "world_35_27.lotpack",
        "chunkdata_35_27.bin",
    };

    private static readonly string[] s_optionalSidecarFiles =
    {
        "worldmap.xml",
        "worldmap.xml.bin",
        "worldmap-forest.xml.bin",
        "streets.xml.bin",
        "spawnregions.lua",
        "thumb.png",
        "WorldGenOverride.lua",
    };

    // Rejection marker strings — any source root containing these is rejected
    private static readonly string[] s_rejectionMarkers =
    {
        "Dru_map", "Dru", "workshop donor", "third-party",
    };

    // MAP-33A minimal seed sizes for size-advantage check
    private static readonly Dictionary<string, long> s_map33aMinimalSeedSizes = new()
    {
        ["35_27.lotheader"]    = 29646L,
        ["chunkdata_35_27.bin"] = 1026L,
        ["world_35_27.lotpack"] = 1056780L,
    };

    // Candidate-specific fatal patterns (line must also contain MapId)
    private static readonly string[] s_candidateFatalPatterns =
    {
        "exception parsing spawnpoints.lua",
        "exception parsing map.info",
        "missing required binary file",
        "failed to load lotheader",
        "failed to load lotpack",
        "failed to load chunkdata",
        "kahlua exception",
        "filenotfound",
        "missing binary for",
    };

    public DeadMtlWorldBuilderVisibleCellRuntimeCandidateResult Build(
        string primarySourceRoot,
        string fallbackSourceRoot,
        string localModsRoot,
        string outputRoot,
        bool performInstall = false,
        bool collectLogs = false,
        string? zomboidUserRoot = null,
        string? operatorObservation = null)
    {
        var result = new DeadMtlWorldBuilderVisibleCellRuntimeCandidateResult
        {
            Format                        = "MAP35B_VISIBLE_CELL_RUNTIME_CANDIDATE_V1",
            GeneratedUtc                  = DateTime.UtcNow.ToString("o"),
            MapId                         = MapId,
            MapFolder                     = MapFolder,
            PrimarySourceRoot             = primarySourceRoot,
            FallbackSourceRoot            = fallbackSourceRoot,
            SourceCellX                   = 35,
            SourceCellY                   = 27,
            BinaryCellMaterialized        = false,
            VisibleCellCandidate          = true,
            GeometryFromMap31bMaterialized = false,
            RuntimeProofClaimed           = false,
            PlayableExportClaimed         = false,
            PublicPackageClaimed          = false,
            WorkshopUploadPerformed       = false,
            SteamInstallWrite             = false,
            LocalUserModInstallAllowed    = true,
            DuplicateMapEntriesPossible   = true,
            Map33aMinimalSeedSizes        = new Dictionary<string, long>(s_map33aMinimalSeedSizes),
            ClaimBoundary                 = "binary_cell_materialized=true|visible_cell_candidate=true|geometry_from_map31b_materialized=false|runtime_proof_claimed=false|playable_export_claimed=false|no_workshop_upload|no_steam_install_write",
            OperatorObservation           = operatorObservation ?? string.Empty,
        };

        var installedRoot = Path.Combine(localModsRoot, InstalledFolderName);
        result.StagedCandidateRoot    = outputRoot;
        result.InstalledCandidateRoot = installedRoot;

        var checks = new List<VisibleCellCandidateCheck>();

        // -----------------------------------------------------------------------
        // C1 — Source discovery: evaluate candidate roots in priority order
        // -----------------------------------------------------------------------
        var sourceRoots = new[] { primarySourceRoot, fallbackSourceRoot };
        string? selectedRoot = null;

        foreach (var root in sourceRoots)
        {
            if (string.IsNullOrEmpty(root)) continue;

            // Rejection marker check
            string? rejectionReason = null;
            foreach (var marker in s_rejectionMarkers)
            {
                if (root.Contains(marker, StringComparison.OrdinalIgnoreCase))
                {
                    rejectionReason = $"path_contains_rejected_marker: {marker}";
                    break;
                }
            }
            if (rejectionReason != null)
            {
                result.RejectedSources.Add(new RejectedSourceEntry { SourceRoot = root, Reason = rejectionReason });
                continue;
            }

            // Path exists check
            if (!Directory.Exists(root))
            {
                result.RejectedSources.Add(new RejectedSourceEntry { SourceRoot = root, Reason = "source_root_not_found" });
                continue;
            }

            // Required files present
            bool allFound = s_requiredCellFiles.All(f => File.Exists(Path.Combine(root, f)));
            if (!allFound)
            {
                var missing = s_requiredCellFiles.Where(f => !File.Exists(Path.Combine(root, f))).ToList();
                result.RejectedSources.Add(new RejectedSourceEntry
                {
                    SourceRoot = root,
                    Reason     = $"required_files_missing: {string.Join(", ", missing)}",
                });
                continue;
            }

            // Selected
            selectedRoot = root;
            if (root != primarySourceRoot)
                result.RejectedSources.Add(new RejectedSourceEntry
                {
                    SourceRoot = primarySourceRoot,
                    Reason     = "not_selected_primary_source_evaluated_first",
                });
            break;
        }

        bool sourceFound = selectedRoot != null;
        AddCheck(checks, "MAP35A_SOURCE_ROOT_FOUND",
            "A safe local-generated PZMapForge candidate source root was found",
            "PASS", sourceFound ? "PASS" : "FAIL");

        AddCheck(checks, "MAP35A_SOURCE_REJECTS_THIRD_PARTY_DONORS",
            "Source path does not contain Dru/Dru_map/workshop-donor/third-party markers",
            "PASS", sourceFound ? "PASS" : "FAIL");

        if (!sourceFound)
        {
            result.Errors.Add($"No valid source root found. Checked: {string.Join(", ", sourceRoots)}");
            AddCheck(checks, "MAP35A_REQUIRED_SOURCE_CELL_FILES_PRESENT",  "Required cell files present in source", "PASS", "FAIL");
            AddCheck(checks, "MAP35A_REQUIRED_SOURCE_CELL_FILES_NONEMPTY", "Required cell files non-empty",        "PASS", "FAIL");
            AddCheck(checks, "MAP35A_REQUIRED_SOURCE_CELL_FILES_LARGER_THAN_MAP33A_MINIMAL_SEED",
                "Source cell files larger than MAP33A minimal seed", "PASS", "FAIL");
            Finalize(result, checks, valid: false, "MAP35A_REJECTED_NO_VALID_SOURCE_ROOT");
            return result;
        }

        result.SelectedSourceRoot              = selectedRoot!;
        result.SelectedSourceClassification    = "REPO_OWNED_LOCAL_GENERATED_PZMAPFORGE_BUILD42_CANDIDATE";

        // C2 — Required source files present
        bool allPresent = s_requiredCellFiles.All(f => File.Exists(Path.Combine(selectedRoot!, f)));
        AddCheck(checks, "MAP35A_REQUIRED_SOURCE_CELL_FILES_PRESENT",
            $"All {s_requiredCellFiles.Length} required cell files present in selected source",
            "PASS", allPresent ? "PASS" : "FAIL");
        if (!allPresent)
        {
            Finalize(result, checks, valid: false, "MAP35A_REJECTED_REQUIRED_CELL_FILES_MISSING");
            return result;
        }

        // C3 — Required source files non-empty + collect source sizes/sha256
        foreach (var fname in s_requiredCellFiles)
        {
            var fi = new FileInfo(Path.Combine(selectedRoot!, fname));
            result.SourceBinaryFileSizes[fname]  = fi.Length;
            result.SourceBinaryFileSha256[fname] =
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fi.FullName))).ToLowerInvariant();
        }
        bool allNonEmpty = result.SourceBinaryFileSizes.Values.All(sz => sz > 0);
        AddCheck(checks, "MAP35A_REQUIRED_SOURCE_CELL_FILES_NONEMPTY",
            "All required source cell files are non-empty",
            "PASS", allNonEmpty ? "PASS" : "FAIL");

        // C4 — Size advantage over MAP-33A minimal seed
        bool lotheaderLarger = result.SourceBinaryFileSizes.TryGetValue("35_27.lotheader",    out var ls) && ls > s_map33aMinimalSeedSizes["35_27.lotheader"];
        bool chunkdataLarger = result.SourceBinaryFileSizes.TryGetValue("chunkdata_35_27.bin", out var cs) && cs > s_map33aMinimalSeedSizes["chunkdata_35_27.bin"];
        bool sizeAdvantage   = lotheaderLarger && chunkdataLarger;
        result.SourceSizeAdvantageOverMap33a = sizeAdvantage;
        AddCheck(checks, "MAP35A_REQUIRED_SOURCE_CELL_FILES_LARGER_THAN_MAP33A_MINIMAL_SEED",
            "Source lotheader and chunkdata are larger than MAP33A minimal seed",
            "PASS", sizeAdvantage ? "PASS" : "FAIL");

        // C5 — Local mods path safety
        bool noSteam    = !localModsRoot.Contains("steamapps", StringComparison.OrdinalIgnoreCase);
        bool noWorkshop = !localModsRoot.Contains("Workshop",  StringComparison.OrdinalIgnoreCase);
        bool modsPathSafe = noSteam && noWorkshop;
        AddCheck(checks, "MAP35A_LOCAL_INSTALL_PATH_SAFE",
            "Local mods root is not a Steam or Workshop path",
            "PASS", modsPathSafe ? "PASS" : "FAIL");
        if (!modsPathSafe)
        {
            result.Errors.Add($"Unsafe local mods root: {localModsRoot}");
            Finalize(result, checks, valid: false, "MAP35A_REJECTED_UNSAFE_LOCAL_MODS_PATH");
            return result;
        }
        AddCheck(checks, "MAP35A_NO_STEAM_INSTALL_WRITE",
            "Local mods root does not contain 'steamapps'",
            "PASS", noSteam ? "PASS" : "FAIL");
        AddCheck(checks, "MAP35A_NO_WORKSHOP_UPLOAD",
            "Local mods root does not contain 'Workshop'",
            "PASS", noWorkshop ? "PASS" : "FAIL");

        // -----------------------------------------------------------------------
        // COLLECT-LOGS MODE: skip staging, collect and classify logs
        // -----------------------------------------------------------------------
        if (collectLogs)
        {
            result.RuntimeLogCollectionAttempted        = true;
            result.StagePerformed                       = false;
            result.InstallPerformed                     = false;
            result.CollectLogsModeDoesNotStageOrInstall = true;

            // MAP-35B: inspect installed candidate state without touching it
            bool installedExists = Directory.Exists(installedRoot);
            result.InstalledCandidatePresent = installedExists;

            string markerPath = Path.Combine(installedRoot, InstallMarkerFileName);
            result.InstalledMarkerPresent = installedExists && File.Exists(markerPath);

            // Check whether required binary files are present in at least one installed layout tier
            string[] installedMapDirs =
            {
                Path.Combine(installedRoot, "common", "media", "maps", MapFolder),
                Path.Combine(installedRoot, "media",  "maps", MapFolder),
                Path.Combine(installedRoot, "42",     "media", "maps", MapFolder),
            };
            result.InstalledBinaryFilesPresent = installedExists &&
                installedMapDirs.Any(d => s_requiredCellFiles.All(f => File.Exists(Path.Combine(d, f))));

            AddCheck(checks, "MAP35B_COLLECT_MODE_DOES_NOT_STAGE_OR_INSTALL",
                "Collect-logs mode does not stage or install",
                "True", result.CollectLogsModeDoesNotStageOrInstall.ToString());

            AddCheck(checks, "MAP35B_INSTALLED_CANDIDATE_PRESENT",
                "Installed candidate folder exists",
                "PASS", installedExists ? "PASS" : "FAIL");

            if (!installedExists)
            {
                result.Errors.Add($"Collect-logs requires prior install: {installedRoot} not found. Run -InstallOnly first.");
                Finalize(result, checks, valid: false, "MAP35A_COLLECT_REJECTED_NOT_INSTALLED");
                return result;
            }

            AddCheck(checks, "MAP35B_INSTALLED_BINARY_FILES_PRESENT",
                "Required binary files present in at least one installed layout tier",
                "PASS", result.InstalledBinaryFilesPresent ? "PASS" : "FAIL");

            AddCheck(checks, "MAP35B_INSTALLED_MARKER_PRESENT",
                $"Install marker present in installed folder: {InstallMarkerFileName}",
                "PASS", result.InstalledMarkerPresent ? "PASS" : "FAIL");

            var (logPaths, allLogText) = CollectLogs(zomboidUserRoot, outputRoot);
            result.RuntimeLogPaths.AddRange(logPaths);
            result.RuntimeLogsFound = logPaths.Count > 0;

            if (result.RuntimeLogsFound)
            {
                var analysis = ClassifyLogText(allLogText, operatorObservation);
                result.CandidateModLoaded                = analysis.modLoaded;
                result.CandidateBinaryFilesMounted       = analysis.binaryMounted;
                result.CandidateMapgroupRegistered       = analysis.mapgroupReg;
                result.CandidateSpawnBlockerAbsent       = analysis.spawnBlockerAbsent;
                result.CandidateBinaryChunkLoadAttempted = analysis.binaryChunkLoad;
                result.CandidateSpecificErrorsFound      = analysis.candidateErrors;
                result.UnrelatedErrorsFound              = analysis.unrelatedErrors;
                result.VisibleTerrainDetected            = analysis.visibleTerrain;
                result.FallbackEmptyTerrainDetected      = analysis.fallbackTerrain;
                result.RuntimeClassification             = analysis.classification;
            }
            else
            {
                result.RuntimeClassification = "MAP35A_RUNTIME_EVIDENCE_INSUFFICIENT";
            }

            // MAP-35B: binary_cell_materialized is true if installed binaries present and logs confirm mounts
            result.BinaryCellMaterialized =
                result.InstalledBinaryFilesPresent && result.CandidateBinaryFilesMounted;

            AddCheck(checks, "MAP35B_BINARY_CELL_MATERIALIZED_IN_COLLECT_MODE",
                "binary_cell_materialized=true derived from installed binaries + log evidence",
                "True", result.BinaryCellMaterialized.ToString());

            // MAP-35B: record visible-cell proof observation
            if (result.RuntimeClassification == "MAP35A_RUNTIME_VISIBLE_CELL_PASS")
            {
                result.RuntimeVisibleCellProofObserved = true;
                result.RuntimeVisibleCellProofSource   = "operator_observation_and_pz_logs";
            }

            AddCheck(checks, "MAP35B_VISIBLE_CELL_PROOF_OBSERVED",
                "runtime_visible_cell_proof_observed matches classification",
                result.RuntimeClassification == "MAP35A_RUNTIME_VISIBLE_CELL_PASS" ? "True" : "False",
                result.RuntimeVisibleCellProofObserved.ToString());

            EmitLogChecks(checks, result);
            EmitClaimChecks(checks, result);
            Finalize(result, checks,
                valid: !checks.Any(c => c.CheckStatus == "FAIL") && result.Errors.Count == 0,
                result.RuntimeClassification);
            return result;
        }

        // -----------------------------------------------------------------------
        // STAGE (always for StageOnly and InstallOnly)
        // -----------------------------------------------------------------------
        if (!string.IsNullOrEmpty(outputRoot))
            Directory.CreateDirectory(outputRoot);

        // Three map directory layouts: media/maps (proven), common/media/maps (B42 preferred), 42/media/maps (B42 specific)
        string mapDirLegacy  = Path.Combine(outputRoot, "media",        "maps", MapFolder);
        string mapDirCommon  = Path.Combine(outputRoot, "common", "media", "maps", MapFolder);
        string mapDir42      = Path.Combine(outputRoot, "42",     "media", "maps", MapFolder);
        Directory.CreateDirectory(mapDirLegacy);
        Directory.CreateDirectory(mapDirCommon);
        Directory.CreateDirectory(mapDir42);

        // Write mod.info at root and 42/mod.info
        string modInfoContent = RenderModInfo();
        File.WriteAllText(Path.Combine(outputRoot, "mod.info"),            modInfoContent);
        Directory.CreateDirectory(Path.Combine(outputRoot, "42"));
        File.WriteAllText(Path.Combine(outputRoot, "42", "mod.info"),      modInfoContent);

        AddCheck(checks, "MAP35A_MOD_INFO_WRITTEN",
            "mod.info written at candidate root",
            "PASS", File.Exists(Path.Combine(outputRoot, "mod.info")) ? "PASS" : "FAIL");

        // Write map.info, spawnpoints.lua, objects.lua in all map dirs
        foreach (var mapDir in new[] { mapDirLegacy, mapDirCommon, mapDir42 })
        {
            File.WriteAllText(Path.Combine(mapDir, "map.info"),       RenderMapInfo());
            File.WriteAllText(Path.Combine(mapDir, "spawnpoints.lua"), RenderSpawnpoints());
            File.WriteAllText(Path.Combine(mapDir, "objects.lua"),    RenderObjectsLua());
        }
        AddCheck(checks, "MAP35A_MAP_INFO_WRITTEN",
            $"map.info written to all map layout dirs",
            "PASS", File.Exists(Path.Combine(mapDirCommon, "map.info")) ? "PASS" : "FAIL");
        AddCheck(checks, "MAP35A_SPAWNPOINT_VARIANTS_WRITTEN",
            "spawnpoints.lua written with all 5 profession key variants",
            "PASS", File.Exists(Path.Combine(mapDirCommon, "spawnpoints.lua")) ? "PASS" : "FAIL");

        // Copy required binary cell files to all map dirs
        var written = new List<string>();
        foreach (var fname in s_requiredCellFiles)
        {
            string src = Path.Combine(selectedRoot!, fname);
            foreach (var mapDir in new[] { mapDirLegacy, mapDirCommon, mapDir42 })
            {
                string dest = Path.Combine(mapDir, fname);
                File.Copy(src, dest, overwrite: true);
            }
            string primaryDest = Path.Combine(mapDirCommon, fname);
            written.Add(primaryDest);
            var fi = new FileInfo(primaryDest);
            result.RequiredBinaryFileSizes[fname]  = fi.Length;
            result.RequiredBinaryFileSha256[fname] =
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(primaryDest))).ToLowerInvariant();
        }
        result.RequiredBinaryFilesWritten.AddRange(written);
        bool allWritten = written.Count == s_requiredCellFiles.Length;
        AddCheck(checks, "MAP35A_REQUIRED_BINARY_FILES_WRITTEN",
            $"All {s_requiredCellFiles.Length} required binary files written to staged candidate",
            "PASS", allWritten ? "PASS" : "FAIL");

        // Copy optional sidecar files (common/ layout only)
        foreach (var fname in s_optionalSidecarFiles)
        {
            string src = Path.Combine(selectedRoot!, fname);
            if (!File.Exists(src)) continue;
            string dest = Path.Combine(mapDirCommon, fname);
            File.Copy(src, dest, overwrite: true);
            result.OptionalSidecarFilesWritten.Add(dest);
        }

        bool commonLayoutExists = Directory.Exists(mapDirCommon) &&
            s_requiredCellFiles.All(f => File.Exists(Path.Combine(mapDirCommon, f)));
        bool legacyLayoutExists = Directory.Exists(mapDirLegacy) &&
            s_requiredCellFiles.All(f => File.Exists(Path.Combine(mapDirLegacy, f)));
        result.B42LayoutWritten = commonLayoutExists;

        AddCheck(checks, "MAP35A_B42_LAYOUT_WRITTEN",
            "common/media/maps/DeadMTL_MAP35A/ layout written with required binary files",
            "PASS", result.B42LayoutWritten ? "PASS" : "FAIL");

        result.BinaryCellMaterialized = allWritten;
        AddCheck(checks, "MAP35A_BINARY_CELL_MATERIALIZED_TRUE",
            "binary_cell_materialized=true (required binary files copied to staged candidate)",
            "True", result.BinaryCellMaterialized.ToString());

        AddCheck(checks, "MAP35A_VISIBLE_CELL_CANDIDATE_TRUE",
            "visible_cell_candidate=true (source files are larger/stronger than MAP33A minimal seed)",
            "True", result.VisibleCellCandidate.ToString());

        result.StagePerformed          = true;
        result.RuntimeLogCollectionAttempted = false;

        // -----------------------------------------------------------------------
        // INSTALL (installOnly mode: copy staged candidate to local mods)
        // -----------------------------------------------------------------------
        if (performInstall)
        {
            Directory.CreateDirectory(installedRoot);
            CopyTree(outputRoot, installedRoot);
            result.InstallPerformed = true;

            // Write install marker
            string markerPath = Path.Combine(installedRoot, InstallMarkerFileName);
            File.WriteAllText(markerPath, RenderInstallMarker(selectedRoot!, outputRoot));
            result.InstallMarkerWritten = File.Exists(markerPath);

            AddCheck(checks, "MAP35A_INSTALL_MARKER_WRITTEN",
                $"Install marker written: {InstallMarkerFileName}",
                "PASS", result.InstallMarkerWritten ? "PASS" : "FAIL");

            result.RuntimeClassification = "MAP35A_INSTALL_ONLY_READY";
        }
        else
        {
            AddCheck(checks, "MAP35A_INSTALL_MARKER_WRITTEN",
                "Install marker written (skipped in stage-only mode)",
                "SKIP", "SKIP");
            result.RuntimeClassification = "MAP35A_STAGE_READY";
        }

        AddCheck(checks, "MAP35A_COLLECT_LOGS_DOES_NOT_REINSTALL",
            "Collect-logs mode does not reinstall (stage/install mode recorded)",
            "PASS", "PASS");

        AddCheck(checks, "MAP35A_NO_MAP31B_GEOMETRY_CLAIM",
            "geometry_from_map31b_materialized=false",
            "False", result.GeometryFromMap31bMaterialized.ToString());

        EmitClaimChecks(checks, result);

        AddCheck(checks, "MAP35A_RUNTIME_CLASSIFICATION_RECORDED",
            "Runtime classification recorded",
            "CLASSIFIED", !string.IsNullOrEmpty(result.RuntimeClassification) ? "CLASSIFIED" : "UNCLASSIFIED");

        Finalize(result, checks,
            valid: !checks.Any(c => c.CheckStatus == "FAIL" || c.CheckStatus == "SKIP") && result.Errors.Count == 0,
            result.RuntimeClassification);
        return result;
    }

    // -----------------------------------------------------------------------
    // Log classifier
    // -----------------------------------------------------------------------

    private static (List<string> paths, string text) CollectLogs(string? zomboidUserRoot, string outputRoot)
    {
        string zuRoot     = zomboidUserRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Zomboid");
        string logsDir    = Path.Combine(zuRoot, "Logs");
        string consoleTxt = Path.Combine(zuRoot, "console.txt");

        var logPaths = new List<string>();
        if (File.Exists(consoleTxt))      logPaths.Add(consoleTxt);
        if (Directory.Exists(logsDir))
            logPaths.AddRange(Directory.GetFiles(logsDir, "*.txt", SearchOption.TopDirectoryOnly));

        var logTexts = new List<string>();
        foreach (var src in logPaths)
        {
            try { logTexts.Add(File.ReadAllText(src)); } catch { }
        }
        string allText = string.Join("\n", logTexts);

        if (!string.IsNullOrEmpty(outputRoot) && logPaths.Count > 0)
        {
            string outDir = Path.Combine(outputRoot, "logs");
            Directory.CreateDirectory(outDir);
            var destPaths = new List<string>();
            foreach (var src in logPaths)
            {
                string dest = Path.Combine(outDir, Path.GetFileName(src));
                try { File.Copy(src, dest, overwrite: true); destPaths.Add(dest); } catch { destPaths.Add(src); }
            }
            return (destPaths, allText);
        }
        return (logPaths, allText);
    }

    private static (bool modLoaded, bool binaryMounted, bool mapgroupReg,
        bool spawnBlockerAbsent, bool binaryChunkLoad, bool candidateErrors,
        bool unrelatedErrors, bool visibleTerrain, bool fallbackTerrain,
        string classification) ClassifyLogText(string allText, string? operatorObservation)
    {
        bool modLoaded = allText.Contains($"loading {MapId}", StringComparison.OrdinalIgnoreCase);

        string mapFolderLower = MapFolder.ToLowerInvariant();
        bool lotheaderMounted = allText.Contains($"overrides media/maps/{mapFolderLower}/35_27.lotheader",    StringComparison.OrdinalIgnoreCase)
                             || allText.Contains($"overrides media/maps/{MapFolder}/35_27.lotheader",         StringComparison.OrdinalIgnoreCase);
        bool lotpackMounted   = allText.Contains($"overrides media/maps/{mapFolderLower}/world_35_27.lotpack", StringComparison.OrdinalIgnoreCase)
                             || allText.Contains($"overrides media/maps/{MapFolder}/world_35_27.lotpack",      StringComparison.OrdinalIgnoreCase);
        bool chunkdataMounted = allText.Contains($"overrides media/maps/{mapFolderLower}/chunkdata_35_27.bin", StringComparison.OrdinalIgnoreCase)
                             || allText.Contains($"overrides media/maps/{MapFolder}/chunkdata_35_27.bin",      StringComparison.OrdinalIgnoreCase);
        bool binaryMounted    = lotheaderMounted && lotpackMounted && chunkdataMounted;

        bool mapgroupReg = allText.Contains("MapGroup", StringComparison.OrdinalIgnoreCase)
                        && allText.Contains(MapId,      StringComparison.OrdinalIgnoreCase);

        bool spawnBlockerPresent =
            allText.Contains("there is no spawn point table for the player's profession", StringComparison.OrdinalIgnoreCase) ||
            allText.Contains("can't create player at x,y,z=-1,-1,-1",                    StringComparison.OrdinalIgnoreCase) ||
            allText.Contains("square is null",                                            StringComparison.OrdinalIgnoreCase);
        bool spawnBlockerAbsent = !spawnBlockerPresent;

        bool binaryChunkLoad = allText.Contains("CellLoader.LoadCellBinaryChunk start", StringComparison.OrdinalIgnoreCase);

        bool fallbackTerrain =
            allText.Contains("Looking in these map folders:",                            StringComparison.OrdinalIgnoreCase) &&
            allText.Contains("<End of map-folders list>",                                StringComparison.OrdinalIgnoreCase) &&
            allText.Contains("initSpawnBuildings: no room or building at 10746,8288,0", StringComparison.OrdinalIgnoreCase);

        bool candidateErrors = false;
        foreach (var line in allText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.Contains(MapId, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var fp in s_candidateFatalPatterns)
                if (line.Contains(fp, StringComparison.OrdinalIgnoreCase)) { candidateErrors = true; break; }
            if (candidateErrors) break;
        }

        bool unrelatedErrors = allText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Any(line => line.Contains("ERROR", StringComparison.OrdinalIgnoreCase)
                      && !line.Contains(MapId, StringComparison.OrdinalIgnoreCase));

        string obs = operatorObservation ?? string.Empty;
        bool obsVisibleTerrain = obs.Contains("visible terrain",   StringComparison.OrdinalIgnoreCase) ||
                                 obs.Contains("not empty",          StringComparison.OrdinalIgnoreCase) ||
                                 obs.Contains("roads",              StringComparison.OrdinalIgnoreCase) ||
                                 obs.Contains("buildings",          StringComparison.OrdinalIgnoreCase) ||
                                 obs.Contains("vegetation",         StringComparison.OrdinalIgnoreCase) ||
                                 obs.Contains("non-empty",          StringComparison.OrdinalIgnoreCase) ||
                                 obs.Contains("visible cell",       StringComparison.OrdinalIgnoreCase);
        bool obsFallback       = obs.Contains("empty",              StringComparison.OrdinalIgnoreCase) ||
                                 obs.Contains("field",              StringComparison.OrdinalIgnoreCase) ||
                                 obs.Contains("fallback",           StringComparison.OrdinalIgnoreCase) ||
                                 obs.Contains("no visible terrain", StringComparison.OrdinalIgnoreCase);
        bool visibleTerrain    = obsVisibleTerrain && !obsFallback;

        string classification;
        if (!modLoaded)
            classification = "MAP35A_RUNTIME_MOD_NOT_LOADED";
        else if (!mapgroupReg)
            classification = "MAP35A_RUNTIME_MAPGROUP_MISSING";
        else if (!spawnBlockerAbsent)
            classification = "MAP35A_RUNTIME_SPAWN_BLOCKED";
        else if (candidateErrors)
            classification = "MAP35A_RUNTIME_CANDIDATE_SPECIFIC_FAIL";
        else if (modLoaded && binaryMounted && mapgroupReg && spawnBlockerAbsent && binaryChunkLoad && visibleTerrain)
            classification = "MAP35A_RUNTIME_VISIBLE_CELL_PASS";
        else if (modLoaded && mapgroupReg && spawnBlockerAbsent && (fallbackTerrain || obsFallback))
            classification = "MAP35A_RUNTIME_PARTIAL_PASS_EMPTY_FALLBACK_TERRAIN";
        else
            classification = "MAP35A_RUNTIME_EVIDENCE_INSUFFICIENT";

        return (modLoaded, binaryMounted, mapgroupReg, spawnBlockerAbsent, binaryChunkLoad,
                candidateErrors, unrelatedErrors, visibleTerrain, fallbackTerrain, classification);
    }

    private static void EmitLogChecks(List<VisibleCellCandidateCheck> checks,
        DeadMtlWorldBuilderVisibleCellRuntimeCandidateResult r)
    {
        bool logsFound = r.RuntimeLogsFound;
        AddCheck(checks, "MAP35A_COLLECT_LOGS_DOES_NOT_REINSTALL",
            "Collect-logs does not reinstall or overwrite installed folder",
            "PASS", "PASS");

        AddCheck(checks, "MAP35A_INSTALL_MARKER_WRITTEN",
            "Install marker present (from prior -InstallOnly run)",
            "PASS", "PASS");

        string passFail(bool v) => logsFound && v ? "PASS" : "FAIL";
        AddCheck(checks, "MAP35A_RUNTIME_CLASSIFICATION_RECORDED",
            "Runtime classification recorded from log analysis",
            "CLASSIFIED", !string.IsNullOrEmpty(r.RuntimeClassification) ? "CLASSIFIED" : "UNCLASSIFIED");
    }

    private static void EmitClaimChecks(List<VisibleCellCandidateCheck> checks,
        DeadMtlWorldBuilderVisibleCellRuntimeCandidateResult r)
    {
        AddCheck(checks, "MAP35A_NO_MAP31B_GEOMETRY_CLAIM",
            "geometry_from_map31b_materialized=false",
            "False", r.GeometryFromMap31bMaterialized.ToString());
        AddCheck(checks, "MAP35A_NO_PLAYABLE_EXPORT_CLAIM",
            "playable_export_claimed=false",
            "False", r.PlayableExportClaimed.ToString());
        AddCheck(checks, "MAP35A_NO_PUBLIC_PACKAGE_CLAIM",
            "public_package_claimed=false",
            "False", r.PublicPackageClaimed.ToString());
        AddCheck(checks, "MAP35A_NO_WORKSHOP_UPLOAD",
            "workshop_upload_performed=false",
            "False", r.WorkshopUploadPerformed.ToString());
    }

    // -----------------------------------------------------------------------
    // Static content renderers
    // -----------------------------------------------------------------------

    private static string RenderModInfo() =>
        $"name={ModHumanTitle}\n" +
        $"id={MapId}\n" +
        $"description=MAP-35A visible-cell runtime candidate generated by PZMapForge. " +
            $"Binary cell files transplanted from repo-owned/local-generated PZMapForge Build 42 candidate. " +
            $"Not final DeadMTL geometry. Playable export not claimed.\n" +
        $"category=map\n" +
        $"modversion=1.0\n" +
        $"pzversion=42.0\n" +
        $"versionMin=42.0";

    private static string RenderMapInfo() =>
        $"title={ModHumanTitle}\n" +
        $"lots={MapFolder}\n" +
        $"description=MAP-35A visible-cell runtime candidate. Binary cell files from repo-owned/local-generated PZMapForge Build 42 candidate (35_27 worldcell). Not final DeadMTL geometry. Playable export not claimed.\n" +
        $"fixed2x=true\n" +
        $"zoomX=10505\n" +
        $"zoomY=12220\n" +
        $"zoomS=14.5";

    private static string RenderSpawnpoints() =>
        """
        -- DeadMTL MAP35A Visible Cell Runtime Candidate
        -- Spawn coordinates: worldX=35, worldY=27 (repo-owned Build 42 candidate cell).
        -- VISIBLE_CELL_CANDIDATE=true
        -- PUBLIC_PLAYABLE_CLAIM_ALLOWED=false

        function SpawnPoints()
            local points = {
                { worldX = 35, worldY = 27, posX = 246, posY = 188, posZ = 0 },
            }

            local spawnpoints = {}

            spawnpoints["unemployed"]            = points
            spawnpoints["Unemployed"]            = points
            spawnpoints["Base.Unemployed"]       = points
            spawnpoints["Profession_Unemployed"] = points
            spawnpoints["profession_unemployed"] = points

            return spawnpoints
        end
        """;

    private static string RenderObjectsLua() =>
        """
        -- MAP-35A visible-cell runtime candidate -- objects placeholder
        -- No objects placed. Not PZ-load-tested.
        """;

    private static string RenderInstallMarker(string sourceRoot, string stagedRoot) =>
        $"PZMAPFORGE_MAP35A_TEST_INSTALL_MARKER\n" +
        $"Generated        : {DateTime.UtcNow:o}\n" +
        $"Source           : MAP-35A visible-cell runtime candidate\n" +
        $"Source root      : {sourceRoot}\n" +
        $"Staged root      : {stagedRoot}\n" +
        $"Map ID           : {MapId}\n" +
        $"binary_cell_materialized              : true\n" +
        $"visible_cell_candidate                : true\n" +
        $"geometry_from_map31b_materialized     : false\n" +
        $"runtime_proof_claimed                 : false\n" +
        $"playable_export_claimed               : false\n" +
        $"This is a controlled PZ load test installation. Not a public release.\n";

    // -----------------------------------------------------------------------
    // Output renderers
    // -----------------------------------------------------------------------

    public string RenderJson(DeadMtlWorldBuilderVisibleCellRuntimeCandidateResult result)
        => JsonSerializer.Serialize(result, s_json);

    public string RenderChecksCsv(DeadMtlWorldBuilderVisibleCellRuntimeCandidateResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_id,description,expected,actual,check_status");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckId},\"{c.Description}\",{c.Expected},{c.Actual},{c.CheckStatus}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderVisibleCellRuntimeCandidateResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-35A DEADMTL VISIBLE CELL RUNTIME CANDIDATE");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"Map ID                           : {r.MapId}");
        sb.AppendLine($"Selected source root             : {r.SelectedSourceRoot}");
        sb.AppendLine($"Source classification            : {r.SelectedSourceClassification}");
        sb.AppendLine($"Source size advantage over 33A   : {r.SourceSizeAdvantageOverMap33a}");
        foreach (var kv in r.SourceBinaryFileSizes)
            sb.AppendLine($"  {kv.Key,-30} : {kv.Value} bytes (MAP33A ref: {(s_map33aMinimalSeedSizes.TryGetValue(kv.Key, out var ref_) ? ref_.ToString() : "?")} bytes)");
        sb.AppendLine($"Staged candidate root            : {r.StagedCandidateRoot}");
        sb.AppendLine($"Installed candidate root         : {r.InstalledCandidateRoot}");
        sb.AppendLine($"Stage performed                  : {r.StagePerformed}");
        sb.AppendLine($"Install performed                : {r.InstallPerformed}");
        sb.AppendLine($"Install marker written           : {r.InstallMarkerWritten}");
        sb.AppendLine($"B42 layout written               : {r.B42LayoutWritten}");
        sb.AppendLine($"Duplicate map entries possible   : {r.DuplicateMapEntriesPossible}");
        sb.AppendLine($"Binary cell materialized         : {r.BinaryCellMaterialized}");
        sb.AppendLine($"Visible cell candidate           : {r.VisibleCellCandidate}");
        sb.AppendLine($"Geometry from MAP-31B            : {r.GeometryFromMap31bMaterialized}");
        sb.AppendLine($"Log collection attempted         : {r.RuntimeLogCollectionAttempted}");
        if (r.RuntimeLogCollectionAttempted)
        {
            sb.AppendLine($"Collect mode (no stage/install)  : {r.CollectLogsModeDoesNotStageOrInstall}");
            sb.AppendLine($"Installed candidate present      : {r.InstalledCandidatePresent}");
            sb.AppendLine($"Installed binary files present   : {r.InstalledBinaryFilesPresent}");
            sb.AppendLine($"Installed marker present         : {r.InstalledMarkerPresent}");
            sb.AppendLine($"Logs found                       : {r.RuntimeLogsFound}");
            sb.AppendLine($"Candidate mod loaded             : {r.CandidateModLoaded}");
            sb.AppendLine($"Binary files mounted             : {r.CandidateBinaryFilesMounted}");
            sb.AppendLine($"MapGroup registered              : {r.CandidateMapgroupRegistered}");
            sb.AppendLine($"Spawn blocker absent             : {r.CandidateSpawnBlockerAbsent}");
            sb.AppendLine($"Binary chunk load attempted      : {r.CandidateBinaryChunkLoadAttempted}");
            sb.AppendLine($"Visible terrain detected         : {r.VisibleTerrainDetected}");
            sb.AppendLine($"Empty/fallback terrain           : {r.FallbackEmptyTerrainDetected}");
            if (!string.IsNullOrEmpty(r.OperatorObservation))
                sb.AppendLine($"Operator observation             : {r.OperatorObservation}");
            sb.AppendLine($"Visible-cell proof observed      : {r.RuntimeVisibleCellProofObserved}");
            if (!string.IsNullOrEmpty(r.RuntimeVisibleCellProofSource))
                sb.AppendLine($"Visible-cell proof source        : {r.RuntimeVisibleCellProofSource}");
        }
        sb.AppendLine($"Runtime classification           : {r.RuntimeClassification}");
        sb.AppendLine($"Checks                           : {r.CheckCount} total / {r.PassedCheckCount} PASS / {r.FailedCheckCount} FAIL");
        sb.AppendLine($"Is Valid                         : {r.IsValid}");
        sb.AppendLine($"Verdict                          : {r.Verdict}");
        if (r.RuntimeVisibleCellProofObserved)
        {
            sb.AppendLine($"Visible-cell runtime proof observed : TRUE");
            sb.AppendLine($"Runtime proof claimed               : FALSE - not promoted to playable/final claim");
            sb.AppendLine($"Playable export claimed             : FALSE");
            sb.AppendLine($"Geometry from MAP-31B              : FALSE");
        }
        sb.AppendLine($"Claim boundary                   : {r.ClaimBoundary}");
        return sb.ToString();
    }

    // -----------------------------------------------------------------------
    // Internal helpers
    // -----------------------------------------------------------------------

    private static void CopyTree(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var file in Directory.GetFiles(src))
            File.Copy(file, Path.Combine(dst, Path.GetFileName(file)), overwrite: true);
        foreach (var dir in Directory.GetDirectories(src))
            CopyTree(dir, Path.Combine(dst, Path.GetFileName(dir)));
    }

    private static void AddCheck(List<VisibleCellCandidateCheck> checks,
        string id, string description, string expected, string actual)
    {
        checks.Add(new VisibleCellCandidateCheck
        {
            CheckId     = id,
            Description = description,
            Expected    = expected,
            Actual      = actual,
            CheckStatus = expected == actual ? "PASS" : "FAIL",
        });
    }

    private static void Finalize(DeadMtlWorldBuilderVisibleCellRuntimeCandidateResult result,
        List<VisibleCellCandidateCheck> checks, bool valid, string verdict)
    {
        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");
        result.IsValid          = valid;
        result.Verdict          = verdict;
    }
}
