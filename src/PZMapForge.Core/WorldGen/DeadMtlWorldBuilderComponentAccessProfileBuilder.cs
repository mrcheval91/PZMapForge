using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class DeadMtlWorldBuilderComponentAccessProfileBuilder
{
    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };

    private static readonly string[] LotBlockBlockedBy =
    [
        "CONCRETE_GEOMETRY_GENERATOR_NOT_IMPLEMENTED",
        "STATIC_TILE_WRITER_NOT_IMPLEMENTED",
        "RUNTIME_VALIDATION_NOT_RUN",
    ];

    public static DeadMtlWorldBuilderComponentAccessProfileResult Build(
        string planningCandidatesPath,
        string componentIntentsPath)
    {
        var result = new DeadMtlWorldBuilderComponentAccessProfileResult
        {
            Format                      = "pzmapforge.deadmtl.worldbuilder.component-access-profile.v1",
            TileId                      = "map_00",
            GeneratedUtc                = DateTime.UtcNow.ToString("o"),
            SourcePlanningCandidatesPath = planningCandidatesPath,
            SourceComponentIntentsPath  = componentIntentsPath,
        };

        var errors = new List<string>();

        if (!File.Exists(planningCandidatesPath))
            errors.Add($"Missing planning candidates: file not found: {planningCandidatesPath}");
        if (!File.Exists(componentIntentsPath))
            errors.Add($"Missing component intents: file not found: {componentIntentsPath}");

        if (errors.Count > 0)
        {
            result.Errors  = errors;
            result.IsValid = false;
            result.Verdict = "MAP25O_BUILD_FAILED_MISSING_INPUTS";
            return result;
        }

        var intents    = LoadIntents(componentIntentsPath, errors);
        var candidates = LoadCandidateRefs(planningCandidatesPath, errors);

        if (errors.Count > 0)
        {
            result.Errors  = errors;
            result.IsValid = false;
            result.Verdict = "MAP25O_BUILD_FAILED_PARSE_ERROR";
            return result;
        }

        var profiles = new List<DeadMtlWorldBuilderComponentAccessProfileRecord>();

        for (var order = 1; order <= 45; order++)
        {
            if (!intents.TryGetValue(order, out var intent))
            {
                errors.Add($"Missing intent data for component_order {order}");
                continue;
            }

            var refs = candidates.TryGetValue(order, out var list) ? list : new List<CandidateRef>();

            int frontageCount    = 0, rearCount    = 0, mixedCount = 0;
            int streetCount      = 0, greenAccess  = 0, greenCivic = 0, ignoreCount = 0;
            int totalActionable  = 0, totalAll     = 0;

            string primaryFrontageId = string.Empty; int primaryFrontagePx = 0;
            string primaryRearId     = string.Empty; int primaryRearPx     = 0;

            foreach (var r in refs)
            {
                totalAll++;
                if (r.IsActionable) totalActionable++;

                switch (r.CandidateType)
                {
                    case "FRONTAGE_PLANNING_CANDIDATE":
                        frontageCount++;
                        if (r.ContactLengthPx > primaryFrontagePx ||
                            (r.ContactLengthPx == primaryFrontagePx && string.Compare(r.OtherComponentId, primaryFrontageId, StringComparison.Ordinal) < 0))
                        {
                            primaryFrontageId = r.OtherComponentId;
                            primaryFrontagePx = r.ContactLengthPx;
                        }
                        break;
                    case "REAR_SERVICE_ACCESS_PLANNING_CANDIDATE":
                        rearCount++;
                        if (r.ContactLengthPx > primaryRearPx ||
                            (r.ContactLengthPx == primaryRearPx && string.Compare(r.OtherComponentId, primaryRearId, StringComparison.Ordinal) < 0))
                        {
                            primaryRearId = r.OtherComponentId;
                            primaryRearPx = r.ContactLengthPx;
                        }
                        break;
                    case "MIXED_LOT_BLOCK_BOUNDARY_CANDIDATE":
                        mixedCount++;
                        break;
                    case "STREET_NETWORK_TOUCHPOINT_CANDIDATE":
                        streetCount++;
                        break;
                    case "GREENSPACE_ACCESS_CANDIDATE":
                        greenAccess++;
                        break;
                    case "CIVIC_GREENSPACE_CONTEXT_CANDIDATE":
                        greenCivic++;
                        break;
                    case "IGNORED_BOUNDARY_ADJACENCY":
                        ignoreCount++;
                        break;
                }
            }

            var accessClass = ClassifyAccess(intent.Intent, frontageCount, rearCount);
            var blockedBy   = IsLotBlock(intent.Intent)
                ? new List<string>(LotBlockBlockedBy)
                : new List<string> { "NONE" };

            if (!IsLotBlock(intent.Intent))
            {
                primaryFrontageId = string.Empty;
                primaryFrontagePx = 0;
                primaryRearId     = string.Empty;
                primaryRearPx     = 0;
            }

            profiles.Add(new DeadMtlWorldBuilderComponentAccessProfileRecord
            {
                ProfileOrder                 = order,
                ProfileId                    = $"map_00_profile_{order:D4}",
                ComponentOrder               = order,
                ComponentId                  = intent.ComponentId,
                SourceColor                  = intent.SourceColor,
                Intent                       = intent.Intent,
                IntentFamily                 = intent.IntentFamily,
                FrontageCandidateCount       = frontageCount,
                RearServiceAccessCandidateCount = rearCount,
                MixedLotBlockCandidateCount  = mixedCount,
                StreetNetworkTouchpointCount = streetCount,
                GreenspaceAccessCandidateCount = greenAccess,
                GreenspaceCivicCandidateCount  = greenCivic,
                IgnoreBoundaryAdjacencyCount = ignoreCount,
                TotalActionableCandidateCount = totalActionable,
                TotalCandidateCount          = totalAll,
                PrimaryFrontageComponentId   = primaryFrontageId,
                PrimaryFrontageContactPx     = primaryFrontagePx,
                PrimaryRearServiceComponentId = primaryRearId,
                PrimaryRearServiceContactPx  = primaryRearPx,
                AccessReadinessClass         = accessClass,
                AccessReadinessBlockedBy     = blockedBy,
                ProfileStatus                = "COMPONENT_ACCESS_PROFILE_EXTRACTED",
                GeometryStatus               = "NO_GEOMETRY_CREATED",
            });
        }

        if (errors.Count > 0)
        {
            result.Errors  = errors;
            result.IsValid = false;
            result.Verdict = "MAP25O_BUILD_FAILED_COMPONENT_ERROR";
            return result;
        }

        var contract = new DeadMtlWorldBuilderComponentAccessProfileContract
        {
            TotalComponentsInput           = profiles.Count,
            ProfileRecordsExtracted        = profiles.Count,
            DualAccessCandidateCount       = profiles.Count(p => p.AccessReadinessClass == "DUAL_ACCESS_CANDIDATE"),
            FrontageOnlyCandidateCount     = profiles.Count(p => p.AccessReadinessClass == "FRONTAGE_ONLY_CANDIDATE"),
            RearServiceOnlyCandidateCount  = profiles.Count(p => p.AccessReadinessClass == "REAR_SERVICE_ONLY_CANDIDATE"),
            LandlockedCandidateCount       = profiles.Count(p => p.AccessReadinessClass == "LANDLOCKED_CANDIDATE"),
            MainRoadCorridorNodeCount      = profiles.Count(p => p.AccessReadinessClass == "MAIN_ROAD_CORRIDOR_NODE"),
            BackAlleyCorridorNodeCount     = profiles.Count(p => p.AccessReadinessClass == "BACK_ALLEY_CORRIDOR_NODE"),
            GreenspaceMassNodeCount        = profiles.Count(p => p.AccessReadinessClass == "GREENSPACE_MASS_NODE"),
            CivicPlaceholderNodeCount      = profiles.Count(p => p.AccessReadinessClass == "CIVIC_PLACEHOLDER_NODE"),
            IgnoredBoundaryComponentCount  = profiles.Count(p => p.AccessReadinessClass == "IGNORED_BOUNDARY_COMPONENT"),
            CreatedGeometryCount           = 0,
            WriterReadyProfileCount        = 0,
            RuntimeValidatedProfileCount   = 0,
        };

        result.ProfileContract = contract;
        result.Profiles        = profiles;
        result.IsValid         = true;
        result.Verdict         = "MAP25O_WORLDBUILDER_COMPONENT_ACCESS_PROFILE_CONTRACT_COMPLETE";
        return result;
    }

    private static string ClassifyAccess(string intent, int frontage, int rear) => intent switch
    {
        "IGNORE_BORDER"          => "IGNORED_BOUNDARY_COMPONENT",
        "MAIN_ROAD_CORRIDOR"     => "MAIN_ROAD_CORRIDOR_NODE",
        "BACK_ALLEY_CORRIDOR"    => "BACK_ALLEY_CORRIDOR_NODE",
        "GREENSPACE_MASS"        => "GREENSPACE_MASS_NODE",
        "CIVIC_PLACEHOLDER"      => "CIVIC_PLACEHOLDER_NODE",
        "RESIDENTIAL_LOT_BLOCK" or "COMMERCIAL_LOT_BLOCK" =>
            (frontage >= 1, rear >= 1) switch
            {
                (true,  true)  => "DUAL_ACCESS_CANDIDATE",
                (true,  false) => "FRONTAGE_ONLY_CANDIDATE",
                (false, true)  => "REAR_SERVICE_ONLY_CANDIDATE",
                (false, false) => "LANDLOCKED_CANDIDATE",
            },
        _ => "UNKNOWN_CLASS",
    };

    private static bool IsLotBlock(string intent) =>
        intent is "RESIDENTIAL_LOT_BLOCK" or "COMMERCIAL_LOT_BLOCK";

    // -----------------------------------------------------------------------
    // Loaders
    // -----------------------------------------------------------------------

    private sealed record IntentData(string ComponentId, string SourceColor, string Intent, string IntentFamily);
    private sealed record CandidateRef(string CandidateType, int ContactLengthPx, string OtherComponentId, bool IsActionable);

    private static Dictionary<int, IntentData> LoadIntents(string path, List<string> errors)
    {
        var dict = new Dictionary<int, IntentData>();
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path), DocOpts);
            var root = doc.RootElement;

            JsonElement records;
            if (root.TryGetProperty("intent_records", out records) ||
                root.TryGetProperty("records", out records))
            {
                foreach (var el in records.EnumerateArray())
                {
                    var order       = el.GetProperty("component_order").GetInt32();
                    var componentId = el.TryGetProperty("component_id", out var cid) ? cid.GetString() ?? string.Empty : $"map_00_component_{order:D4}";
                    // MAP-25L uses parent_source_color and component_intent
                    var color  = el.TryGetProperty("parent_source_color", out var psc) ? psc.GetString() ?? string.Empty
                                 : el.TryGetProperty("source_color", out var sc) ? sc.GetString() ?? string.Empty : string.Empty;
                    var intent = el.TryGetProperty("component_intent", out var ci) ? ci.GetString() ?? string.Empty
                                 : el.TryGetProperty("intent", out var inv) ? inv.GetString() ?? string.Empty : string.Empty;
                    var family      = el.TryGetProperty("intent_family", out var fam) ? fam.GetString() ?? string.Empty : string.Empty;
                    dict[order]     = new IntentData(componentId, color, intent, family);
                }
            }
            else
            {
                errors.Add("Component intents JSON missing 'intent_records' or 'records' array.");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse component intents: {ex.Message}");
        }
        return dict;
    }

    private static Dictionary<int, List<CandidateRef>> LoadCandidateRefs(string path, List<string> errors)
    {
        var dict = new Dictionary<int, List<CandidateRef>>();

        void Add(int order, CandidateRef r)
        {
            if (!dict.TryGetValue(order, out var list))
            {
                list = new List<CandidateRef>();
                dict[order] = list;
            }
            list.Add(r);
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path), DocOpts);
            var root = doc.RootElement;

            JsonElement candidates;
            if (!root.TryGetProperty("candidates", out candidates))
            {
                errors.Add("Planning candidates JSON missing 'candidates' array.");
                return dict;
            }

            foreach (var el in candidates.EnumerateArray())
            {
                var candidateType = el.TryGetProperty("candidate_type", out var ct) ? ct.GetString() ?? string.Empty : string.Empty;
                var contact       = el.TryGetProperty("contact_length_px", out var clp) ? clp.GetInt32() : 0;
                var isActionable  = el.TryGetProperty("is_actionable", out var ia) && ia.GetBoolean();
                var orderA        = el.GetProperty("component_a_order").GetInt32();
                var orderB        = el.GetProperty("component_b_order").GetInt32();
                var idA           = el.TryGetProperty("component_a_id", out var aId) ? aId.GetString() ?? string.Empty : string.Empty;
                var idB           = el.TryGetProperty("component_b_id", out var bId) ? bId.GetString() ?? string.Empty : string.Empty;

                Add(orderA, new CandidateRef(candidateType, contact, idB, isActionable));
                Add(orderB, new CandidateRef(candidateType, contact, idA, isActionable));
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse planning candidates: {ex.Message}");
        }

        return dict;
    }

    // -----------------------------------------------------------------------
    // Renderers
    // -----------------------------------------------------------------------

    public static string RenderMarkdown(DeadMtlWorldBuilderComponentAccessProfileResult result)
    {
        var sb = new StringBuilder();
        var c  = result.ProfileContract;

        sb.AppendLine("# MAP-25O: DeadMTL WorldBuilder Component Access Profile Contract");
        sb.AppendLine();
        sb.AppendLine("**CONTRACT ONLY. Access profile only. No concrete geometry created. Not writer-ready. Not runtime-proven.**");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## What This Is");
        sb.AppendLine();
        sb.AppendLine("Per-component access profile extracted from MAP-25N planning candidates. " +
                      "For each of the 45 classified components, summarizes candidate counts by type, " +
                      "identifies primary frontage and rear-service partners, and assigns an access readiness class. " +
                      "Contract only. No geometry generated.");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Input Chain (2 inputs)");
        sb.AppendLine();
        sb.AppendLine("| Input | Source | File |");
        sb.AppendLine("|-------|--------|------|");
        sb.AppendLine("| Adjacency planning candidates | MAP-25N | map_00.adjacency_planning_candidate_extraction.json |");
        sb.AppendLine("| Component intent classification | MAP-25L | map_00.component_intent_classification.json |");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Access Profile Contract");
        sb.AppendLine();
        sb.AppendLine($"| Field | Value |");
        sb.AppendLine($"|-------|-------|");
        sb.AppendLine($"| tile_id | {result.TileId} |");
        sb.AppendLine($"| total_components_input | {c.TotalComponentsInput} |");
        sb.AppendLine($"| profile_records_extracted | {c.ProfileRecordsExtracted} |");
        sb.AppendLine($"| source_candidate_contract | MAP25N_ADJACENCY_PLANNING_CANDIDATE_EXTRACTION |");
        sb.AppendLine($"| source_intent_contract | MAP25L_COMPONENT_INTENT_CLASSIFICATION |");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Access Readiness Classification Rules");
        sb.AppendLine();
        sb.AppendLine("| Intent | Classification |");
        sb.AppendLine("|--------|----------------|");
        sb.AppendLine("| RESIDENTIAL_LOT_BLOCK or COMMERCIAL_LOT_BLOCK (frontage≥1 AND rear≥1) | DUAL_ACCESS_CANDIDATE |");
        sb.AppendLine("| RESIDENTIAL_LOT_BLOCK or COMMERCIAL_LOT_BLOCK (frontage≥1, rear=0) | FRONTAGE_ONLY_CANDIDATE |");
        sb.AppendLine("| RESIDENTIAL_LOT_BLOCK or COMMERCIAL_LOT_BLOCK (frontage=0, rear≥1) | REAR_SERVICE_ONLY_CANDIDATE |");
        sb.AppendLine("| RESIDENTIAL_LOT_BLOCK or COMMERCIAL_LOT_BLOCK (frontage=0, rear=0) | LANDLOCKED_CANDIDATE |");
        sb.AppendLine("| MAIN_ROAD_CORRIDOR | MAIN_ROAD_CORRIDOR_NODE |");
        sb.AppendLine("| BACK_ALLEY_CORRIDOR | BACK_ALLEY_CORRIDOR_NODE |");
        sb.AppendLine("| GREENSPACE_MASS | GREENSPACE_MASS_NODE |");
        sb.AppendLine("| CIVIC_PLACEHOLDER | CIVIC_PLACEHOLDER_NODE |");
        sb.AppendLine("| IGNORE_BORDER | IGNORED_BOUNDARY_COMPONENT |");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Primary Candidate Selection Rule");
        sb.AppendLine();
        sb.AppendLine("For each lot-block component, the primary frontage partner is the FRONTAGE_PLANNING_CANDIDATE neighbor " +
                      "with the largest contact_length_px. Tiebreak: smallest component_id (lexicographic). " +
                      "Same rule for primary rear-service partner from REAR_SERVICE_ACCESS_PLANNING_CANDIDATE neighbors. " +
                      "Non-lot-block components: primary_frontage_component_id = empty, primary_frontage_contact_px = 0.");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## No Generation Now");
        sb.AppendLine();
        sb.AppendLine("No terrain generated. No lot subdivision. No sidewalk geometry. No road geometry. " +
                      "No building placement. No fences. No concrete geometry created. Layout not materialized. " +
                      "No lotpack writing. No WorldGen override written. No runtime proof.");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Expected Totals (map_00)");
        sb.AppendLine();
        sb.AppendLine("| Field | Expected |");
        sb.AppendLine("|-------|---------|");
        sb.AppendLine($"| profile_records_extracted | {c.ProfileRecordsExtracted} |");
        sb.AppendLine($"| dual_access_candidate_count | {c.DualAccessCandidateCount} |");
        sb.AppendLine($"| frontage_only_candidate_count | {c.FrontageOnlyCandidateCount} |");
        sb.AppendLine($"| rear_service_only_candidate_count | {c.RearServiceOnlyCandidateCount} |");
        sb.AppendLine($"| landlocked_candidate_count | {c.LandlockedCandidateCount} |");
        sb.AppendLine($"| main_road_corridor_node_count | {c.MainRoadCorridorNodeCount} |");
        sb.AppendLine($"| back_alley_corridor_node_count | {c.BackAlleyCorridorNodeCount} |");
        sb.AppendLine($"| greenspace_mass_node_count | {c.GreenspaceMassNodeCount} |");
        sb.AppendLine($"| civic_placeholder_node_count | {c.CivicPlaceholderNodeCount} |");
        sb.AppendLine($"| ignored_boundary_component_count | {c.IgnoredBoundaryComponentCount} |");
        sb.AppendLine($"| created_geometry_count | {c.CreatedGeometryCount} |");
        sb.AppendLine($"| writer_ready_profile_count | {c.WriterReadyProfileCount} |");
        sb.AppendLine($"| runtime_validated_profile_count | {c.RuntimeValidatedProfileCount} |");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Per-Component Spot Values");
        sb.AppendLine();
        sb.AppendLine("| comp | intent | frontage | rear | class |");
        sb.AppendLine("|------|--------|----------|------|-------|");
        foreach (var p in result.Profiles.Where(p => new[] { 1, 23, 40, 41 }.Contains(p.ComponentOrder)))
            sb.AppendLine($"| {p.ComponentOrder} | {p.Intent} | {p.FrontageCandidateCount} | {p.RearServiceAccessCandidateCount} | {p.AccessReadinessClass} |");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary (all false, 15 fields)");
        sb.AppendLine();
        sb.AppendLine($"- writes_lotpack: {c.WritesLotpack.ToString().ToLowerInvariant()}");
        sb.AppendLine($"- writes_worldgen_lua: {c.WritesWorldgenLua.ToString().ToLowerInvariant()}");
        sb.AppendLine($"- runtime_proven: {c.RuntimeProven.ToString().ToLowerInvariant()}");
        sb.AppendLine($"- public_playable_claim: {c.PublicPlayableClaim.ToString().ToLowerInvariant()}");
        sb.AppendLine($"- writer_ready_claim: {c.WriterReadyClaim.ToString().ToLowerInvariant()}");
        sb.AppendLine($"- generates_terrain_now: {c.GeneratesTerrainNow.ToString().ToLowerInvariant()}");
        sb.AppendLine($"- generates_buildings_now: {c.GeneratesBuildingsNow.ToString().ToLowerInvariant()}");
        sb.AppendLine($"- generates_sidewalks_now: {c.GeneratesSidewalksNow.ToString().ToLowerInvariant()}");
        sb.AppendLine($"- subdivides_lots_now: {c.SubdividesLotsNow.ToString().ToLowerInvariant()}");
        sb.AppendLine($"- captures_chunk_layers_now: {c.CapturesChunkLayersNow.ToString().ToLowerInvariant()}");
        sb.AppendLine($"- places_fences_now: {c.PlacesFencesNow.ToString().ToLowerInvariant()}");
        sb.AppendLine($"- places_unique_buildings_now: {c.PlacesUniqueBuildingsNow.ToString().ToLowerInvariant()}");
        sb.AppendLine($"- selects_concrete_building_ids_now: {c.SelectsConcreteBuildingIdsNow.ToString().ToLowerInvariant()}");
        sb.AppendLine($"- creates_concrete_geometry_now: {c.CreatesConcreteGeometryNow.ToString().ToLowerInvariant()}");
        sb.AppendLine($"- materializes_layout_now: {c.MaterializesLayoutNow.ToString().ToLowerInvariant()}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## CLI Command");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine("deadmtl-build-worldbuilder-component-access-profile");
        sb.AppendLine("  --planning-candidates  <map_00.adjacency_planning_candidate_extraction.json>");
        sb.AppendLine("  --component-intents    <map_00.component_intent_classification.json>");
        sb.AppendLine("  --output-json          <out.json>");
        sb.AppendLine("  --output-md            <out.md>");
        sb.AppendLine("  --output-csv           <out.csv>");
        sb.AppendLine("  --summary              <out.txt>");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("All output paths must contain `.local`.");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("## Verdict");
        sb.AppendLine();
        sb.AppendLine($"`{result.Verdict}`");

        return sb.ToString();
    }

    public static string RenderCsv(DeadMtlWorldBuilderComponentAccessProfileResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("profile_order,profile_id,component_order,component_id,source_color,intent,intent_family," +
                      "frontage_candidate_count,rear_service_access_candidate_count,mixed_lot_block_candidate_count," +
                      "street_network_touchpoint_count,greenspace_access_candidate_count,greenspace_civic_candidate_count," +
                      "ignore_boundary_adjacency_count,total_actionable_candidate_count,total_candidate_count," +
                      "primary_frontage_component_id,primary_frontage_contact_px,primary_rear_service_component_id," +
                      "primary_rear_service_contact_px,access_readiness_class,geometry_status");

        foreach (var p in result.Profiles)
        {
            sb.AppendLine($"{p.ProfileOrder},{p.ProfileId},{p.ComponentOrder},{p.ComponentId},{p.SourceColor}," +
                          $"{p.Intent},{p.IntentFamily}," +
                          $"{p.FrontageCandidateCount},{p.RearServiceAccessCandidateCount},{p.MixedLotBlockCandidateCount}," +
                          $"{p.StreetNetworkTouchpointCount},{p.GreenspaceAccessCandidateCount},{p.GreenspaceCivicCandidateCount}," +
                          $"{p.IgnoreBoundaryAdjacencyCount},{p.TotalActionableCandidateCount},{p.TotalCandidateCount}," +
                          $"{p.PrimaryFrontageComponentId},{p.PrimaryFrontageContactPx},{p.PrimaryRearServiceComponentId}," +
                          $"{p.PrimaryRearServiceContactPx},{p.AccessReadinessClass},{p.GeometryStatus}");
        }

        return sb.ToString();
    }

    public static string RenderSummary(DeadMtlWorldBuilderComponentAccessProfileResult result)
    {
        var sb = new StringBuilder();
        var c  = result.ProfileContract;
        sb.AppendLine($"tile_id:                          {result.TileId}");
        sb.AppendLine($"is_valid:                         {result.IsValid}");
        sb.AppendLine($"profile_records_extracted:        {c.ProfileRecordsExtracted}");
        sb.AppendLine($"dual_access_candidate_count:      {c.DualAccessCandidateCount}");
        sb.AppendLine($"frontage_only_candidate_count:    {c.FrontageOnlyCandidateCount}");
        sb.AppendLine($"rear_service_only_candidate_count:{c.RearServiceOnlyCandidateCount}");
        sb.AppendLine($"landlocked_candidate_count:       {c.LandlockedCandidateCount}");
        sb.AppendLine($"main_road_corridor_node_count:    {c.MainRoadCorridorNodeCount}");
        sb.AppendLine($"back_alley_corridor_node_count:   {c.BackAlleyCorridorNodeCount}");
        sb.AppendLine($"greenspace_mass_node_count:       {c.GreenspaceMassNodeCount}");
        sb.AppendLine($"civic_placeholder_node_count:     {c.CivicPlaceholderNodeCount}");
        sb.AppendLine($"ignored_boundary_component_count: {c.IgnoredBoundaryComponentCount}");
        sb.AppendLine($"created_geometry_count:           {c.CreatedGeometryCount}");
        sb.AppendLine($"writer_ready_profile_count:       {c.WriterReadyProfileCount}");
        sb.AppendLine($"runtime_validated_profile_count:  {c.RuntimeValidatedProfileCount}");
        sb.AppendLine($"VERDICT: {result.Verdict}");
        return sb.ToString();
    }
}
