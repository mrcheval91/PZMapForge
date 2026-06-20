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

    public DeadMtlWorldBuilderMap33AInGameLoadTestResult Build(
        string map33aManifestPath,
        string sourceCandidateRoot,
        string localModsRoot,
        string outputRoot,
        bool collectLogs = false,
        string? zomboidUserRoot = null)
    {
        var result = new DeadMtlWorldBuilderMap33AInGameLoadTestResult
        {
            Format                        = "MAP34A_INGAME_LOAD_TEST_V1",
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

        // Read binary_cell_materialized and geometry_from_map31b_materialized from MAP-33A manifest
        bool binaryCellMaterialized = false;
        bool geometryFromMap31b     = false;
        using (var doc = JsonDocument.Parse(File.ReadAllText(map33aManifestPath)))
        {
            var root = doc.RootElement;
            if (root.TryGetProperty("binary_cell_materialized",          out var bcm)) binaryCellMaterialized = bcm.GetBoolean();
            if (root.TryGetProperty("geometry_from_map31b_materialized", out var g31)) geometryFromMap31b     = g31.GetBoolean();
        }
        result.BinaryCellMaterialized          = binaryCellMaterialized;
        result.GeometryFromMap31bMaterialized  = geometryFromMap31b;

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

        // C3 — geometry_from_map31b_materialized must be false (we record but do not require it to block)
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

        // C5 — required binary files present in source candidate (under media/maps/MapName/)
        string srcMapDir = Path.Combine(sourceCandidateRoot, "media", "maps", MapName);
        var presentFiles = new List<string>();
        foreach (var name in s_requiredBinaryFiles)
        {
            string path = Path.Combine(srcMapDir, name);
            if (File.Exists(path)) presentFiles.Add(name);
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

        // C7 — local user mods path resolved (must not be a Steam/Workshop path)
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

        // Install candidate into local mods folder
        string installedRoot = Path.Combine(localModsRoot, InstalledFolderName);
        result.InstalledCandidateRoot = installedRoot;
        Directory.CreateDirectory(installedRoot);
        CopyTree(sourceCandidateRoot, installedRoot);
        result.InstallPerformed = true;

        AddCheck(checks, "MAP34A_CANDIDATE_COPIED_TO_LOCAL_MODS",
            $"MAP-33A candidate copied to local mods: {InstalledFolderName}",
            "PASS", Directory.Exists(installedRoot) ? "PASS" : "FAIL");

        // Write install marker
        string markerPath = Path.Combine(installedRoot, InstallMarkerFileName);
        File.WriteAllText(markerPath, RenderInstallMarker(sourceCandidateRoot));
        result.InstallMarkerWritten = File.Exists(markerPath);
        AddCheck(checks, "MAP34A_INSTALL_MARKER_WRITTEN",
            $"Install marker written: {InstallMarkerFileName}",
            "PASS", result.InstallMarkerWritten ? "PASS" : "FAIL");

        // Log collection
        result.RuntimeLogCollectionAttempted = collectLogs;
        if (collectLogs)
        {
            string zuRoot = zomboidUserRoot ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Zomboid");

            string logsDir   = Path.Combine(zuRoot, "Logs");
            string consoleTxt = Path.Combine(zuRoot, "console.txt");

            var logPaths = new List<string>();
            if (File.Exists(consoleTxt)) logPaths.Add(consoleTxt);
            if (Directory.Exists(logsDir))
                logPaths.AddRange(Directory.GetFiles(logsDir, "*.txt", SearchOption.TopDirectoryOnly));

            if (logPaths.Count > 0)
            {
                string outLogsDir = Path.Combine(outputRoot, "logs");
                Directory.CreateDirectory(outLogsDir);
                foreach (var src in logPaths)
                {
                    string dest = Path.Combine(outLogsDir, Path.GetFileName(src));
                    File.Copy(src, dest, overwrite: true);
                    result.RuntimeLogPaths.Add(dest);
                }
                result.RuntimeLogsFound = true;

                // Classify based on log content
                bool anyMentionMap = result.RuntimeLogPaths.Any(p =>
                {
                    try { return File.ReadAllText(p).Contains(MapName, StringComparison.OrdinalIgnoreCase); }
                    catch { return false; }
                });
                bool anyError = result.RuntimeLogPaths.Any(p =>
                {
                    try
                    {
                        string txt = File.ReadAllText(p);
                        return txt.Contains("ERROR", StringComparison.OrdinalIgnoreCase)
                            && txt.Contains(MapName, StringComparison.OrdinalIgnoreCase);
                    }
                    catch { return false; }
                });

                if (anyMentionMap && !anyError)
                    result.RuntimeClassification = "MAP34A_RUNTIME_LOAD_PASS";
                else if (anyError)
                    result.RuntimeClassification = "MAP34A_RUNTIME_LOAD_FAIL";
                else
                    result.RuntimeClassification = "MAP34A_RUNTIME_EVIDENCE_INSUFFICIENT";
            }
            else
            {
                result.RuntimeLogsFound      = false;
                result.RuntimeClassification = "MAP34A_RUNTIME_EVIDENCE_INSUFFICIENT";
            }
        }
        else
        {
            result.RuntimeClassification = "MAP34A_INSTALL_ONLY_READY";
        }

        AddCheck(checks, "MAP34A_RUNTIME_LOG_COLLECTION_ATTEMPTED",
            "Runtime log collection mode recorded",
            collectLogs ? "True" : "False",
            result.RuntimeLogCollectionAttempted.ToString());

        AddCheck(checks, "MAP34A_RUNTIME_PROOF_CLASSIFIED",
            "Runtime classification recorded",
            "CLASSIFIED", !string.IsNullOrEmpty(result.RuntimeClassification) ? "CLASSIFIED" : "UNCLASSIFIED");

        // Claim boundary checks
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

        Finalize(result, checks,
            valid: !checks.Any(c => c.CheckStatus == "FAIL") && result.Errors.Count == 0,
            result.RuntimeClassification);

        return result;
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
        sb.AppendLine("MAP-34A DEADMTL IN-GAME LOAD TEST");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"Map ID                       : {r.MapId}");
        sb.AppendLine($"Source MAP-33A candidate     : {r.SourceMap33ACandidateRoot}");
        sb.AppendLine($"Installed candidate root     : {r.InstalledCandidateRoot}");
        sb.AppendLine($"Install performed            : {r.InstallPerformed}");
        sb.AppendLine($"Install marker written       : {r.InstallMarkerWritten}");
        sb.AppendLine($"Binary cell materialized     : {r.BinaryCellMaterialized}");
        sb.AppendLine($"Geometry from MAP-31B        : {r.GeometryFromMap31bMaterialized}");
        sb.AppendLine($"Log collection attempted     : {r.RuntimeLogCollectionAttempted}");
        sb.AppendLine($"Logs found                   : {r.RuntimeLogsFound}");
        if (r.RuntimeLogPaths.Count > 0)
        {
            sb.AppendLine($"Log paths collected          : {r.RuntimeLogPaths.Count}");
            foreach (var p in r.RuntimeLogPaths)
                sb.AppendLine($"  {p}");
        }
        sb.AppendLine($"Runtime classification       : {r.RuntimeClassification}");
        sb.AppendLine($"Runtime proof claimed        : {r.RuntimeProofClaimed}");
        sb.AppendLine($"Playable export claimed      : {r.PlayableExportClaimed}");
        sb.AppendLine($"Checks                       : {r.CheckCount} total / {r.PassedCheckCount} PASS / {r.FailedCheckCount} FAIL");
        sb.AppendLine($"Is Valid                     : {r.IsValid}");
        sb.AppendLine($"Verdict                      : {r.Verdict}");
        sb.AppendLine($"Claim boundary               : runtime_proof_claimed=false | playable_export_claimed=false | geometry_from_map31b=false");
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
        {
            string subDst = Path.Combine(dst, Path.GetFileName(dir));
            CopyTree(dir, subDst);
        }
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
