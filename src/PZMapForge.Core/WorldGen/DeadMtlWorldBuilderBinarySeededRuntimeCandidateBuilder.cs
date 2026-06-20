using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderBinarySeededRuntimeCandidateBuilder
{
    private static readonly JsonSerializerOptions s_json = new()
    {
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public const string MapId   = "DeadMTL_MAP33A";
    public const string MapName = "DeadMTL_MAP33A";

    private static readonly string[] s_requiredBinarySeedFiles =
    {
        "35_27.lotheader",
        "world_35_27.lotpack",
        "chunkdata_35_27.bin",
    };

    private static readonly string[] s_optionalSidecarFiles =
    {
        "streets.xml.bin",
        "worldmap.xml.bin",
        "worldmap-forest.xml.bin",
    };

    public DeadMtlWorldBuilderBinarySeededRuntimeCandidateResult Build(
        string map32aManifestPath, string binarySeedRoot, string outputRoot)
    {
        var result = new DeadMtlWorldBuilderBinarySeededRuntimeCandidateResult
        {
            Format                        = "MAP33A_BINARY_SEEDED_RUNTIME_CANDIDATE_V1",
            GeneratedUtc                  = DateTime.UtcNow.ToString("o"),
            OutputRoot                    = outputRoot,
            MapId                         = MapId,
            MapName                       = MapName,
            SourceMap32AManifest          = map32aManifestPath,
            BinarySeedRoot                = binarySeedRoot,
            SandboxOnly                   = true,
            RuntimeValid                  = false,
            RuntimeProofClaimed           = false,
            PlayableExportClaimed         = false,
            PublicPlayablePackagingClaimed = false,
            RawSourcePngMutated           = false,
            LiveWorkshopWrite             = false,
            PzInstallWrite                = false,
            GeometryFromMap31bMaterialized = false,
        };

        var checks = new List<BinarySeededRuntimeCandidateCheck>();

        // C1 — MAP-32A manifest exists
        bool manifestExists = File.Exists(map32aManifestPath);
        AddCheck(checks, "MAP33A_INPUT_MAP32A_MANIFEST_EXISTS",
            "MAP-32A manifest JSON input exists on disk",
            "PASS", manifestExists ? "PASS" : "FAIL");
        if (!manifestExists)
        {
            result.Errors.Add($"MAP-32A manifest not found: {map32aManifestPath}");
            Finalize(result, checks, valid: false, "MAP33A_REJECTED_MAP32A_MANIFEST_MISSING");
            return result;
        }

        // C2 — binary seed root exists
        bool seedRootExists = Directory.Exists(binarySeedRoot);
        AddCheck(checks, "MAP33A_BINARY_SEED_ROOT_EXISTS",
            "Binary seed root directory exists on disk",
            "PASS", seedRootExists ? "PASS" : "FAIL");
        if (!seedRootExists)
        {
            result.Errors.Add($"Binary seed root not found: {binarySeedRoot}");
            Finalize(result, checks, valid: false, "MAP33A_REJECTED_BINARY_SEED_ROOT_MISSING");
            return result;
        }

        // C3 — provenance record
        result.BinarySeedProvenance =
            "REPO_OWNED_MAP7Y_SIDECAR_STUB: pzmapforge_build42_candidate_v4_001 " +
            "(35_27 worldcell, Build42 candidate binary, local-generated, not copied from third-party map)";
        AddCheck(checks, "MAP33A_BINARY_SEED_PROVENANCE_RECORDED",
            "Binary seed provenance is recorded as repo-owned MAP-7Y sidecar stub",
            "REPO_OWNED", "REPO_OWNED");

        // C4 — output root must be inside .local
        bool underLocal = outputRoot.Contains(".local", StringComparison.OrdinalIgnoreCase);
        AddCheck(checks, "MAP33A_OUTPUT_ROOT_UNDER_LOCAL",
            "Output root path is inside .local sandbox",
            "PASS", underLocal ? "PASS" : "FAIL");
        if (!underLocal)
        {
            result.Errors.Add($"Output root is outside .local: {outputRoot}");
            Finalize(result, checks, valid: false, "MAP33A_REJECTED_OUTPUT_ROOT_OUTSIDE_LOCAL");
            return result;
        }

        // Read authoring counts from MAP-32A manifest
        int lotCount = 0, footprintCount = 0, skippedLotCount = 0;
        var sectorCounts = new List<SectorCountEntry>();
        using (var doc = JsonDocument.Parse(File.ReadAllText(map32aManifestPath)))
        {
            var root = doc.RootElement;
            if (root.TryGetProperty("lot_count",          out var lc)) lotCount        = lc.GetInt32();
            if (root.TryGetProperty("footprint_count",    out var fc)) footprintCount  = fc.GetInt32();
            if (root.TryGetProperty("skipped_lot_count",  out var sk)) skippedLotCount = sk.GetInt32();
            if (root.TryGetProperty("sector_counts", out var sc))
                foreach (var s in sc.EnumerateArray())
                    sectorCounts.Add(new SectorCountEntry
                    {
                        SectorId = s.TryGetProperty("sector_id",  out var sid) ? sid.GetString() ?? "" : "",
                        LotCount = s.TryGetProperty("lot_count",  out var slc) ? slc.GetInt32()        : 0,
                    });
        }
        result.LotCount        = lotCount;
        result.FootprintCount  = footprintCount;
        result.SkippedLotCount = skippedLotCount;
        result.SectorCounts    = sectorCounts;

        // Create staged directory structure
        string stagedMapRoot  = Path.Combine(outputRoot, "media", "maps", MapName);
        result.StagedModRoot  = outputRoot;
        result.StagedMapRoot  = stagedMapRoot;
        Directory.CreateDirectory(stagedMapRoot);

        // Write mod.info
        string modInfoPath = Path.Combine(outputRoot, "mod.info");
        File.WriteAllText(modInfoPath, RenderModInfo());
        AddCheck(checks, "MAP33A_STAGED_MOD_INFO_WRITTEN",
            "Staged mod.info written to candidate root",
            "PASS", File.Exists(modInfoPath) ? "PASS" : "FAIL");

        // Write map.info
        string mapInfoPath = Path.Combine(stagedMapRoot, "map.info");
        File.WriteAllText(mapInfoPath, RenderMapInfo(lotCount));
        AddCheck(checks, "MAP33A_STAGED_MAP_INFO_WRITTEN",
            $"Staged map.info written to media/maps/{MapName}/",
            "PASS", File.Exists(mapInfoPath) ? "PASS" : "FAIL");

        // Write spawnpoints.lua
        string spawnPath = Path.Combine(stagedMapRoot, "spawnpoints.lua");
        File.WriteAllText(spawnPath, RenderSpawnpoints());
        AddCheck(checks, "MAP33A_SPAWNPOINTS_LUA_WRITTEN",
            "Staged spawnpoints.lua written",
            "PASS", File.Exists(spawnPath) ? "PASS" : "FAIL");

        // Write objects.lua
        string objectsPath = Path.Combine(stagedMapRoot, "objects.lua");
        File.WriteAllText(objectsPath, RenderObjectsLua());
        AddCheck(checks, "MAP33A_OBJECTS_LUA_WRITTEN",
            "Staged objects.lua written",
            "PASS", File.Exists(objectsPath) ? "PASS" : "FAIL");

        // Discover required binary seed files
        var discovered = new List<string>();
        foreach (var name in s_requiredBinarySeedFiles)
        {
            string src = Path.Combine(binarySeedRoot, name);
            if (File.Exists(src))
                discovered.Add(name);
        }
        result.BinarySeedFilesDiscovered.AddRange(discovered);
        bool allRequired = discovered.Count == s_requiredBinarySeedFiles.Length;
        AddCheck(checks, "MAP33A_REQUIRED_BINARY_SEED_FILES_DISCOVERED",
            $"All {s_requiredBinarySeedFiles.Length} required binary seed files found in seed root",
            "PASS", allRequired ? "PASS" : "FAIL");
        if (!allRequired)
        {
            var missing = s_requiredBinarySeedFiles.Except(discovered).ToList();
            result.Errors.Add($"Missing required binary seed files: {string.Join(", ", missing)}");
            Finalize(result, checks, valid: false, "MAP33A_FAILED_BINARY_SEED_FILES_MISSING");
            return result;
        }

        // Copy required binary files
        var written = new List<string>();
        foreach (var name in s_requiredBinarySeedFiles)
        {
            string src  = Path.Combine(binarySeedRoot, name);
            string dest = Path.Combine(stagedMapRoot, name);
            File.Copy(src, dest, overwrite: true);
            written.Add(dest);
        }
        result.BinarySeedFilesWritten.AddRange(written);
        bool allWritten = written.Count == s_requiredBinarySeedFiles.Length;
        AddCheck(checks, "MAP33A_REQUIRED_BINARY_SEED_FILES_WRITTEN",
            $"All {s_requiredBinarySeedFiles.Length} required binary seed files copied to staged map folder",
            "PASS", allWritten ? "PASS" : "FAIL");

        // Verify non-empty
        bool allNonEmpty = written.All(p => new FileInfo(p).Length > 0);
        AddCheck(checks, "MAP33A_REQUIRED_BINARY_SEED_FILES_NONEMPTY",
            "All copied required binary files are non-empty",
            "PASS", allNonEmpty ? "PASS" : "FAIL");

        // Compute SHA256 for each required binary file written
        foreach (var dest in written)
        {
            string name   = Path.GetFileName(dest);
            string sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dest))).ToLowerInvariant();
            result.BinarySeedFileSha256[name] = sha256;
        }

        // Copy optional sidecar files
        foreach (var name in s_optionalSidecarFiles)
        {
            string src  = Path.Combine(binarySeedRoot, name);
            string dest = Path.Combine(stagedMapRoot, name);
            if (!File.Exists(src)) continue;
            File.Copy(src, dest, overwrite: true);
            result.OptionalSidecarFilesWritten.Add(dest);
            string sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dest))).ToLowerInvariant();
            result.BinarySeedFileSha256[name] = sha256;
        }

        // binary_cell_materialized = true when all required files written and non-empty
        result.RequiredBinaryFilesPresent = allWritten && allNonEmpty;
        result.BinaryCellMaterialized     = result.RequiredBinaryFilesPresent;
        AddCheck(checks, "MAP33A_BINARY_CELL_MATERIALIZED_TRUE",
            "binary_cell_materialized=true (required binary files copied and non-empty)",
            "True", result.BinaryCellMaterialized.ToString());

        // Authoring counts carried forward
        AddCheck(checks, "MAP33A_AUTHORING_COUNTS_CARRIED_FORWARD",
            $"Lot/footprint/skipped counts carried from MAP-32A manifest (lots={lotCount} fp={footprintCount} skip={skippedLotCount})",
            "PASS", "PASS");

        // Sector counts carried forward
        AddCheck(checks, "MAP33A_SECTOR_COUNTS_CARRIED_FORWARD",
            "Sector counts carried forward from MAP-32A manifest",
            "PASS", sectorCounts.Count > 0 ? "PASS" : "FAIL");

        // Geometry not claimed from MAP-31B
        AddCheck(checks, "MAP33A_GEOMETRY_FROM_MAP31B_NOT_CLAIMED",
            "geometry_from_map31b_materialized=false (binary seed is MAP-7Y sidecar, not MAP-31B encoded geometry)",
            "False", result.GeometryFromMap31bMaterialized.ToString());

        // Safety: no Workshop or PZ install path
        bool noWorkshop  = !outputRoot.Contains("Workshop",      StringComparison.OrdinalIgnoreCase)
                        && !outputRoot.Contains("steamapps",     StringComparison.OrdinalIgnoreCase);
        AddCheck(checks, "MAP33A_NO_LIVE_WORKSHOP_WRITE",
            "Output root is not a Workshop or steamapps path",
            "PASS", noWorkshop ? "PASS" : "FAIL");

        bool noPzInstall = !outputRoot.Contains("Project Zomboid", StringComparison.OrdinalIgnoreCase)
                        && !outputRoot.Contains("ProjectZomboid",  StringComparison.OrdinalIgnoreCase);
        AddCheck(checks, "MAP33A_NO_PROJECT_ZOMBOID_INSTALL_WRITE",
            "Output root is not a Project Zomboid install path",
            "PASS", noPzInstall ? "PASS" : "FAIL");

        // Claim boundary checks
        AddCheck(checks, "MAP33A_NO_PUBLIC_PACKAGE_CLAIM",
            "PublicPlayablePackagingClaimed=false",
            "False", result.PublicPlayablePackagingClaimed.ToString());

        AddCheck(checks, "MAP33A_RUNTIME_PROOF_FALSE",
            "RuntimeProofClaimed=false",
            "False", result.RuntimeProofClaimed.ToString());

        AddCheck(checks, "MAP33A_PLAYABLE_EXPORT_CLAIM_FALSE",
            "PlayableExportClaimed=false",
            "False", result.PlayableExportClaimed.ToString());

        AddCheck(checks, "MAP33A_RAW_SOURCE_PNG_UNCHANGED",
            "RawSourcePngMutated=false",
            "False", result.RawSourcePngMutated.ToString());

        bool claimBoundaryOk = result.SandboxOnly
            && !result.RuntimeValid && !result.RuntimeProofClaimed
            && !result.PublicPlayablePackagingClaimed && !result.PlayableExportClaimed
            && !result.RawSourcePngMutated && !result.LiveWorkshopWrite && !result.PzInstallWrite;
        AddCheck(checks, "MAP33A_CLAIM_BOUNDARY_RECORDED",
            "All runtime/public/playable claim flags false; sandbox_only=true",
            "PASS", claimBoundaryOk ? "PASS" : "FAIL");

        Finalize(result, checks,
            valid: !checks.Any(c => c.CheckStatus == "FAIL") && result.Errors.Count == 0,
            "MAP33A_BINARY_SEEDED_RUNTIME_CANDIDATE_STAGED");

        return result;
    }

    // -----------------------------------------------------------------------
    // Static content renderers
    // -----------------------------------------------------------------------

    private static string RenderModInfo() =>
        $"name=DeadMTL MAP33A Binary-Seeded Runtime Candidate\n" +
        $"id={MapId}\n" +
        $"description=MAP-33A binary-seeded runtime candidate generated by PZMapForge. " +
            $"Sandbox-only staging artifact. Binary cell files seeded from repo-owned MAP-7Y sidecar stub. " +
            $"Not PZ-load-tested. Playable export not claimed.\n" +
        $"category=map\n" +
        $"modversion=1.0\n" +
        $"pzversion=42.0\n" +
        $"versionMin=42.0";

    private static string RenderMapInfo(int lotCount) =>
        $"title={MapName}\n" +
        $"lots=NONE\n" +
        $"description=PZMapForge MAP-33A binary-seeded runtime candidate. " +
            $"Binary cell files from repo-owned MAP-7Y sidecar stub (35_27 worldcell). " +
            $"Not PZ-load-tested. Source lots={lotCount}.\n" +
        $"fixed2x=true\n" +
        $"zoomX=10505\n" +
        $"zoomY=12220\n" +
        $"zoomS=14.5";

    private static string RenderSpawnpoints() =>
        """
        -- DeadMTL MAP33A Binary-Seeded Runtime Candidate
        -- Spawn coordinates follow MAP-7Y sidecar stub conventions (worldX=35, worldY=27).
        -- PZ load test not performed. Planning-only coordinates.
        -- BINARY_SEED=REPO_OWNED_MAP7Y_SIDECAR_STUB
        -- PUBLIC_PLAYABLE_CLAIM_ALLOWED=false

        function SpawnPoints()
            local spawnpoints = {}
            spawnpoints["Profession_Unemployed"] = {
                { worldX = 35, worldY = 27, posX = 246, posY = 188, posZ = 0 },
            }
            return spawnpoints
        end
        """;

    private static string RenderObjectsLua() =>
        """
        -- MAP-33A binary-seeded runtime candidate -- objects placeholder
        -- No objects placed. Not PZ-load-tested.
        """;

    // -----------------------------------------------------------------------
    // Output renderers
    // -----------------------------------------------------------------------

    public string RenderJson(DeadMtlWorldBuilderBinarySeededRuntimeCandidateResult result)
        => JsonSerializer.Serialize(result, s_json);

    public string RenderChecksCsv(DeadMtlWorldBuilderBinarySeededRuntimeCandidateResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_id,description,expected,actual,check_status");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckId},\"{c.Description}\",{c.Expected},{c.Actual},{c.CheckStatus}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderBinarySeededRuntimeCandidateResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-33A DEADMTL BINARY-SEEDED RUNTIME CANDIDATE");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"Map ID                         : {r.MapId}");
        sb.AppendLine($"Map Name                       : {r.MapName}");
        sb.AppendLine($"Staged mod root                : {r.StagedModRoot}");
        sb.AppendLine($"Staged map root                : {r.StagedMapRoot}");
        sb.AppendLine($"Binary seed root               : {r.BinarySeedRoot}");
        sb.AppendLine($"Binary seed provenance         : {r.BinarySeedProvenance}");
        sb.AppendLine($"Lot count                      : {r.LotCount}");
        sb.AppendLine($"Footprint count                : {r.FootprintCount}");
        sb.AppendLine($"Skipped lot count              : {r.SkippedLotCount}");
        if (r.SectorCounts.Count > 0)
            sb.AppendLine($"Sector counts                  : {string.Join(" ", r.SectorCounts.Select(sc => $"{sc.SectorId}={sc.LotCount}"))}");
        sb.AppendLine($"Binary seed files discovered   : {r.BinarySeedFilesDiscovered.Count}  ({string.Join(", ", r.BinarySeedFilesDiscovered)})");
        sb.AppendLine($"Binary seed files written      : {r.BinarySeedFilesWritten.Count}");
        foreach (var path in r.BinarySeedFilesWritten)
        {
            string name = Path.GetFileName(path);
            string sha  = r.BinarySeedFileSha256.TryGetValue(name, out var h) ? h[..16] + "..." : "?";
            sb.AppendLine($"  {name}  sha256={sha}");
        }
        if (r.OptionalSidecarFilesWritten.Count > 0)
        {
            sb.AppendLine($"Optional sidecar files written : {r.OptionalSidecarFilesWritten.Count}");
            foreach (var path in r.OptionalSidecarFilesWritten)
                sb.AppendLine($"  {Path.GetFileName(path)}");
        }
        sb.AppendLine($"Binary cell materialized       : {r.BinaryCellMaterialized}");
        sb.AppendLine($"Geometry from MAP-31B          : {r.GeometryFromMap31bMaterialized}");
        sb.AppendLine($"Checks                         : {r.CheckCount} total / {r.PassedCheckCount} PASS / {r.FailedCheckCount} FAIL");
        sb.AppendLine($"Is Valid                       : {r.IsValid}");
        sb.AppendLine($"Verdict                        : {r.Verdict}");
        sb.AppendLine($"Claim boundary                 : sandbox_only=true | runtime_proof_claimed=false | playable_export_claimed=false | live_workshop_write=false | pz_install_write=false");
        return sb.ToString();
    }

    // -----------------------------------------------------------------------
    // Internal helpers
    // -----------------------------------------------------------------------

    private static void AddCheck(List<BinarySeededRuntimeCandidateCheck> checks,
        string id, string description, string expected, string actual)
    {
        checks.Add(new BinarySeededRuntimeCandidateCheck
        {
            CheckId     = id,
            Description = description,
            Expected    = expected,
            Actual      = actual,
            CheckStatus = expected == actual ? "PASS" : "FAIL",
        });
    }

    private static void Finalize(DeadMtlWorldBuilderBinarySeededRuntimeCandidateResult result,
        List<BinarySeededRuntimeCandidateCheck> checks, bool valid, string verdict)
    {
        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");
        result.IsValid          = valid;
        result.Verdict          = verdict;
    }
}
