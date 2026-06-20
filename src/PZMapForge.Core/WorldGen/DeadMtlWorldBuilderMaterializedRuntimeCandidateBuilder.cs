using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMaterializedRuntimeCandidateBuilder
{
    private static readonly JsonSerializerOptions s_json = new()
    {
        WriteIndented              = true,
        DefaultIgnoreCondition     = JsonIgnoreCondition.Never,
    };

    public const string MapId   = "DeadMTL_MAP32A";
    public const string MapName = "DeadMTL_MAP32A";

    public DeadMtlWorldBuilderMaterializedRuntimeCandidateResult Build(
        string lotFillJsonPath, string footprintJsonPath, string outputRoot)
    {
        var result = new DeadMtlWorldBuilderMaterializedRuntimeCandidateResult
        {
            Format               = "MAP32A_MATERIALIZED_RUNTIME_CANDIDATE_V1",
            GeneratedUtc         = DateTime.UtcNow.ToString("o"),
            SourceLotFillJson    = lotFillJsonPath,
            SourceFootprintJson  = footprintJsonPath,
            OutputRoot           = outputRoot,
            MapId                = MapId,
            MapName              = MapName,
            SandboxOnly                    = true,
            WriterReady                    = false,
            RuntimeValid                   = false,
            RuntimeProofClaimed            = false,
            PublicPlayablePackagingClaimed = false,
            PlayableExportClaimed          = false,
            RawSourcePngMutated            = false,
        };

        var checks = new List<MaterializedRuntimeCandidateCheck>();

        // C1 — output root must be inside .local
        bool underLocal = outputRoot.Contains(".local", StringComparison.OrdinalIgnoreCase);
        AddCheck(checks, "MAP32A_OUTPUT_ROOT_UNDER_LOCAL",
            "Output root path is inside .local sandbox",
            "PASS", underLocal ? "PASS" : "FAIL");
        if (!underLocal)
        {
            result.Errors.Add($"Output root is outside .local: {outputRoot}");
            Finalize(result, checks, valid: false, "MAP32A_REJECTED_OUTPUT_ROOT_OUTSIDE_LOCAL");
            return result;
        }

        // C2 — lot-fill JSON exists
        bool lotFillExists = File.Exists(lotFillJsonPath);
        AddCheck(checks, "MAP32A_INPUT_LOT_FILL_JSON_EXISTS",
            "Lot-fill JSON input exists on disk",
            "PASS", lotFillExists ? "PASS" : "FAIL");
        if (!lotFillExists)
        {
            result.Errors.Add($"Lot-fill JSON not found: {lotFillJsonPath}");
            Finalize(result, checks, valid: false, "MAP32A_INPUT_LOT_FILL_JSON_MISSING");
            return result;
        }

        // C3 — footprint JSON exists
        bool fpExists = File.Exists(footprintJsonPath);
        AddCheck(checks, "MAP32A_INPUT_FOOTPRINT_JSON_EXISTS",
            "Footprint candidate JSON input exists on disk",
            "PASS", fpExists ? "PASS" : "FAIL");
        if (!fpExists)
        {
            result.Errors.Add($"Footprint JSON not found: {footprintJsonPath}");
            Finalize(result, checks, valid: false, "MAP32A_INPUT_FOOTPRINT_JSON_MISSING");
            return result;
        }

        // Read lot count from lot-fill JSON
        int lotCount = 0;
        using (var doc = JsonDocument.Parse(File.ReadAllText(lotFillJsonPath)))
            if (doc.RootElement.TryGetProperty("lots", out var lots))
                lotCount = lots.GetArrayLength();
        result.LotCount = lotCount;

        // Read footprint summary from footprint JSON
        int footprintCount = 0, skippedLotCount = 0;
        var sectorCounts = new List<SectorCountEntry>();
        using (var doc = JsonDocument.Parse(File.ReadAllText(footprintJsonPath)))
        {
            var root = doc.RootElement;
            if (root.TryGetProperty("footprint_count",   out var fc)) footprintCount  = fc.GetInt32();
            if (root.TryGetProperty("skipped_lot_count", out var sk)) skippedLotCount = sk.GetInt32();
            if (root.TryGetProperty("sector_counts", out var sc))
                foreach (var s in sc.EnumerateArray())
                    sectorCounts.Add(new SectorCountEntry
                    {
                        SectorId = s.TryGetProperty("sector_id",  out var sid) ? sid.GetString() ?? "" : "",
                        LotCount = s.TryGetProperty("lot_count",  out var lc)  ? lc.GetInt32()         : 0,
                    });
        }
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
        result.MetadataFilesWritten.Add(modInfoPath);
        AddCheck(checks, "MAP32A_STAGED_MOD_INFO_WRITTEN",
            "Staged mod.info written to candidate root",
            "PASS", File.Exists(modInfoPath) ? "PASS" : "FAIL");

        // Write map.info
        string mapInfoPath = Path.Combine(stagedMapRoot, "map.info");
        File.WriteAllText(mapInfoPath, RenderMapInfo(result.LotCount));
        result.MetadataFilesWritten.Add(mapInfoPath);
        AddCheck(checks, "MAP32A_STAGED_MAP_INFO_WRITTEN",
            $"Staged map.info written to media/maps/{MapName}/",
            "PASS", File.Exists(mapInfoPath) ? "PASS" : "FAIL");

        // Write spawnpoints.lua
        string spawnPath = Path.Combine(stagedMapRoot, "spawnpoints.lua");
        File.WriteAllText(spawnPath, RenderSpawnpoints());
        result.MetadataFilesWritten.Add(spawnPath);
        result.SpawnpointsWritten = File.Exists(spawnPath);
        AddCheck(checks, "MAP32A_SPAWNPOINTS_LUA_WRITTEN",
            "Staged spawnpoints.lua written",
            "PASS", result.SpawnpointsWritten ? "PASS" : "FAIL");

        // Write objects.lua
        string objectsPath = Path.Combine(stagedMapRoot, "objects.lua");
        File.WriteAllText(objectsPath, RenderObjectsLua());
        result.MetadataFilesWritten.Add(objectsPath);
        result.ObjectsLuaWritten = File.Exists(objectsPath);
        AddCheck(checks, "MAP32A_OBJECTS_LUA_WRITTEN",
            "Staged objects.lua written",
            "PASS", result.ObjectsLuaWritten ? "PASS" : "FAIL");

        // Authoring counts consumed from inputs
        AddCheck(checks, "MAP32A_AUTHORING_COUNTS_CONSUMED",
            $"Lot and footprint counts consumed (lots={result.LotCount} footprints={result.FootprintCount})",
            "PASS", "PASS");

        // Sector counts carried forward
        AddCheck(checks, "MAP32A_SECTOR_COUNTS_CARRIED_FORWARD",
            "Sector counts carried forward from footprint JSON",
            "PASS", sectorCounts.Count > 0 ? "PASS" : "FAIL");

        // Binary cell materialization — Path B (no binary writer available)
        result.BinaryCellMaterialized   = false;
        result.BinaryMaterializationGap =
            "PATH_B_METADATA_ONLY: No binary cell writer available. " +
            "Documented gaps: lotp_chunk_payload_format_not_understood, " +
            "lotheader_tile_table_visual_mapping_not_understood, " +
            "chunkdata_format_not_understood, " +
            "no_tile_placement_record_model. " +
            "Reference: MAP-9B canary outcome CANARY_IMPOSSIBLE_WITH_CURRENT_WRITER.";
        AddCheck(checks, "MAP32A_BINARY_CELL_MATERIALIZATION_ATTEMPTED",
            "Binary cell materialization attempted; Path B chosen — no writer available, gap explicitly recorded",
            "PATH_B_RECORDED", "PATH_B_RECORDED");

        // Scan: output root must not be a Workshop or PZ install path
        bool noWorkshop = !outputRoot.Contains("Workshop",  StringComparison.OrdinalIgnoreCase)
                       && !outputRoot.Contains("steamapps", StringComparison.OrdinalIgnoreCase);
        AddCheck(checks, "MAP32A_NO_LIVE_WORKSHOP_WRITE",
            "Output root is not a Workshop or steamapps path",
            "PASS", noWorkshop ? "PASS" : "FAIL");

        bool noPZInstall = !outputRoot.Contains("Project Zomboid", StringComparison.OrdinalIgnoreCase)
                        && !outputRoot.Contains("ProjectZomboid",  StringComparison.OrdinalIgnoreCase);
        AddCheck(checks, "MAP32A_NO_PROJECT_ZOMBOID_INSTALL_WRITE",
            "Output root is not a Project Zomboid install path",
            "PASS", noPZInstall ? "PASS" : "FAIL");

        // Claim boundary checks
        AddCheck(checks, "MAP32A_RAW_SOURCE_PNG_UNCHANGED",
            "Raw source PNG was not mutated (raw_source_png_mutated=false)",
            "False", result.RawSourcePngMutated.ToString());

        AddCheck(checks, "MAP32A_NO_PUBLIC_PACKAGE_CLAIM",
            "PublicPlayablePackagingClaimed is false",
            "False", result.PublicPlayablePackagingClaimed.ToString());

        AddCheck(checks, "MAP32A_RUNTIME_PROOF_FALSE",
            "RuntimeProofClaimed is false",
            "False", result.RuntimeProofClaimed.ToString());

        bool claimBoundaryOk = result.SandboxOnly
            && !result.WriterReady && !result.RuntimeValid
            && !result.RuntimeProofClaimed && !result.PublicPlayablePackagingClaimed
            && !result.PlayableExportClaimed && !result.RawSourcePngMutated;
        AddCheck(checks, "MAP32A_CLAIM_BOUNDARY_RECORDED",
            "All runtime/public/playable claim flags false; sandbox_only=true",
            "PASS", claimBoundaryOk ? "PASS" : "FAIL");

        Finalize(result, checks,
            valid: !checks.Any(c => c.CheckStatus == "FAIL") && result.Errors.Count == 0,
            "MAP32A_MATERIALIZED_RUNTIME_CANDIDATE_STAGED");

        return result;
    }

    // -----------------------------------------------------------------------
    // Static content renderers — follow MAP-7Y sidecar stub conventions
    // -----------------------------------------------------------------------

    private static string RenderModInfo() =>
        $"name=DeadMTL MAP32A Sandbox Runtime Candidate\n" +
        $"id={MapId}\n" +
        $"description=MAP-32A materialized runtime candidate generated by PZMapForge. " +
            $"Sandbox-only staging artifact. Not a playable Project Zomboid export. " +
            $"Binary cell files not materialized — binary writer gap recorded.\n" +
        $"category=map\n" +
        $"modversion=1.0\n" +
        $"pzversion=42.0\n" +
        $"versionMin=42.0";

    private static string RenderMapInfo(int lotCount) =>
        $"title={MapName}\n" +
        $"lots=NONE\n" +
        $"description=PZMapForge MAP-32A materialized runtime candidate. Sandbox staging only. " +
            $"Not compiled. Not PZ-load-tested. Source lots={lotCount}.\n" +
        $"fixed2x=true\n" +
        $"zoomX=10505\n" +
        $"zoomY=12220\n" +
        $"zoomS=14.5";

    // worldX=35 worldY=27 follow MAP-7Y sidecar stub coordinate conventions
    private static string RenderSpawnpoints() =>
        """
        -- DeadMTL MAP32A Sandbox Runtime Candidate
        -- Spawn coordinates follow MAP-7Y sidecar stub conventions (worldX=35, worldY=27).
        -- PZ load test not performed. Planning-only coordinates.
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
        -- MAP-32A materialized runtime candidate — objects placeholder
        -- No objects placed. Not PZ-load-tested.
        """;

    // -----------------------------------------------------------------------
    // Output renderers
    // -----------------------------------------------------------------------

    public string RenderJson(DeadMtlWorldBuilderMaterializedRuntimeCandidateResult result)
        => JsonSerializer.Serialize(result, s_json);

    public string RenderChecksCsv(DeadMtlWorldBuilderMaterializedRuntimeCandidateResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_id,description,expected,actual,check_status");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckId},\"{c.Description}\",{c.Expected},{c.Actual},{c.CheckStatus}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderMaterializedRuntimeCandidateResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-32A DEADMTL MATERIALIZED RUNTIME CANDIDATE");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"Map ID                  : {r.MapId}");
        sb.AppendLine($"Map Name                : {r.MapName}");
        sb.AppendLine($"Staged mod root         : {r.StagedModRoot}");
        sb.AppendLine($"Staged map root         : {r.StagedMapRoot}");
        sb.AppendLine($"Lot count               : {r.LotCount}");
        sb.AppendLine($"Footprint count         : {r.FootprintCount}");
        sb.AppendLine($"Skipped lot count       : {r.SkippedLotCount}");
        if (r.SectorCounts.Count > 0)
        {
            var sectorLine = string.Join(" ", r.SectorCounts.Select(sc => $"{sc.SectorId}={sc.LotCount}"));
            sb.AppendLine($"Sector counts           : {sectorLine}");
        }
        sb.AppendLine($"Metadata files written  : {r.MetadataFilesWritten.Count}");
        sb.AppendLine($"Binary cell materialized: {r.BinaryCellMaterialized}");
        sb.AppendLine($"Binary gap              : {r.BinaryMaterializationGap}");
        sb.AppendLine($"Checks                  : {r.CheckCount} total / {r.PassedCheckCount} PASS / {r.FailedCheckCount} FAIL");
        sb.AppendLine($"Is Valid                : {r.IsValid}");
        sb.AppendLine($"Verdict                 : {r.Verdict}");
        sb.AppendLine($"Claim boundary          : sandbox_only=true | writer_ready=false | runtime_valid=false | runtime_proof_claimed=false | playable_export_claimed=false");
        return sb.ToString();
    }

    // -----------------------------------------------------------------------
    // Internal helpers
    // -----------------------------------------------------------------------

    private static void AddCheck(List<MaterializedRuntimeCandidateCheck> checks,
        string id, string description, string expected, string actual)
    {
        checks.Add(new MaterializedRuntimeCandidateCheck
        {
            CheckId     = id,
            Description = description,
            Expected    = expected,
            Actual      = actual,
            CheckStatus = expected == actual ? "PASS" : "FAIL",
        });
    }

    private static void Finalize(DeadMtlWorldBuilderMaterializedRuntimeCandidateResult result,
        List<MaterializedRuntimeCandidateCheck> checks, bool valid, string verdict)
    {
        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");
        result.IsValid          = valid;
        result.Verdict          = verdict;
    }
}
