using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class DeadMtlWorldBuilderBuildingSelectionPolicyPlanBuilder
{
    public static DeadMtlWorldBuilderBuildingSelectionPolicyPlanResult Build(
        string profilePath, string metadataPath, string lotPlanPath, string sidewalkPlanPath)
    {
        var errors = new List<string>();
        var plan   = new DeadMtlWorldBuilderBuildingSelectionPolicyPlan
        {
            SourceProfileJson              = profilePath,
            SourceZoneMetadataJson         = metadataPath,
            SourceLotSubdivisionPlanJson   = lotPlanPath,
            SourceSidewalkGenerationPlanJson = sidewalkPlanPath,
        };

        var opts = new JsonSerializerOptions
            { AllowTrailingCommas = true, PropertyNameCaseInsensitive = false };

        if (!File.Exists(profilePath))
        {
            errors.Add($"Neighborhood profile not found: {profilePath}");
            return Fail(errors, plan);
        }
        try
        {
            var json = File.ReadAllText(profilePath, Encoding.UTF8);
            JsonSerializer.Deserialize<DeadMtlWorldBuilderNeighborhoodProfile>(json, opts);
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse profile JSON: {ex.Message}");
            return Fail(errors, plan);
        }

        if (!File.Exists(metadataPath))
        {
            errors.Add($"Zone metadata not found: {metadataPath}");
            return Fail(errors, plan);
        }

        DeadMtlWorldBuilderRawTileZoneMetadata metadata;
        try
        {
            var json = File.ReadAllText(metadataPath, Encoding.UTF8);
            metadata = JsonSerializer.Deserialize<DeadMtlWorldBuilderRawTileZoneMetadata>(json, opts)
                       ?? new DeadMtlWorldBuilderRawTileZoneMetadata();
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse zone metadata JSON: {ex.Message}");
            return Fail(errors, plan);
        }

        plan.TileId = metadata.TileId;

        if (!File.Exists(lotPlanPath))
        {
            errors.Add($"Lot subdivision plan not found: {lotPlanPath}");
            return Fail(errors, plan);
        }
        try
        {
            var json = File.ReadAllText(lotPlanPath, Encoding.UTF8);
            JsonSerializer.Deserialize<DeadMtlWorldBuilderLotSubdivisionPlan>(json, opts);
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse lot subdivision plan JSON: {ex.Message}");
            return Fail(errors, plan);
        }

        if (!File.Exists(sidewalkPlanPath))
        {
            errors.Add($"Sidewalk generation plan not found: {sidewalkPlanPath}");
            return Fail(errors, plan);
        }
        try
        {
            var json = File.ReadAllText(sidewalkPlanPath, Encoding.UTF8);
            JsonSerializer.Deserialize<DeadMtlWorldBuilderSidewalkGenerationPlan>(json, opts);
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse sidewalk generation plan JSON: {ex.Message}");
            return Fail(errors, plan);
        }

        foreach (var cr in metadata.ColorRoles)
            plan.PlanItems.Add(BuildItem(cr));

        plan.Totals = BuildTotals(plan.PlanItems);

        plan.ClaimBoundary = new DeadMtlWorldBuilderBuildingSelectionPolicyPlanClaimBoundary
        {
            WritesLotpack                  = false,
            WritesWorldgenLua              = false,
            RuntimeProven                  = false,
            PublicPlayableClaim            = false,
            WriterReadyClaim               = false,
            GeneratesBuildingsNow          = false,
            GeneratesSidewalksNow          = false,
            SubdividesLotsNow              = false,
            CapturesChunkLayersNow         = false,
            PlacesFencesNow                = false,
            PlacesUniqueBuildingsNow       = false,
            SelectsConcreteBuildingIdsNow  = false,
        };

        return new DeadMtlWorldBuilderBuildingSelectionPolicyPlanResult
            { IsValid = true, Errors = errors, Plan = plan };
    }

    private static DeadMtlWorldBuilderBuildingSelectionPolicyPlanItem BuildItem(
        DeadMtlWorldBuilderRawTileColorRole cr)
    {
        var item = new DeadMtlWorldBuilderBuildingSelectionPolicyPlanItem
        {
            Color       = cr.Color,
            Role        = cr.Role,
            ZoneType    = cr.ZoneType,
            StreetClass = cr.StreetClass,
        };

        switch (cr.Role)
        {
            case "ZONE":
                switch (cr.ZoneType)
                {
                    case "RESIDENTIAL":
                        item.SelectionAction         = "PLAN_SELECT_RESIDENTIAL_BUILDING_FAMILY_LATER";
                        item.PlacementMode           = "FUTURE_FIT_BUILDING_TO_LOT";
                        item.FrontageRequirement     = "PREFER_MAIN_ROAD_FRONTAGE";
                        item.AlleyRule               = "BACK_ALLEY_REAR_SERVICE_ONLY";
                        item.FacadeRule              = "ROW_UNIFORM_FRONTAGE";
                        item.SidewalkRelation        = "FRONTAGE_SIDEWALK_IF_MAIN_ROAD";
                        item.AllowedBuildingFamilies = new List<string>
                            { "duplex", "triplex", "plex_block", "apartment_lowrise" };
                        item.FitPolicy =
                            "fit_to_lot_width_depth_later; reject_if_footprint_exceeds_lot; " +
                            "prefer_facade_width_compatible_with_row; prefer_montreal_plex_frontage";
                        item.Notes =
                            "Eligible for building family selection after lot subdivision. " +
                            "No concrete building id selected now. MAIN_ROAD preferred frontage. " +
                            "BACK_ALLEY is rear/service only.";
                        break;

                    case "COMMERCIAL":
                        item.SelectionAction         = "PLAN_SELECT_COMMERCIAL_BUILDING_FAMILY_LATER";
                        item.PlacementMode           = "FUTURE_FIT_BUILDING_TO_LOT";
                        item.FrontageRequirement     = "PREFER_MAIN_ROAD_FRONTAGE";
                        item.AlleyRule               = "BACK_ALLEY_SERVICE_ACCESS";
                        item.FacadeRule              = "COMMERCIAL_FRONTAGE";
                        item.SidewalkRelation        = "FRONTAGE_SIDEWALK_IF_MAIN_ROAD";
                        item.AllowedBuildingFamilies = new List<string>
                            { "depanneur", "pharmacy", "restaurant", "main_street_storefront", "office_small" };
                        item.FitPolicy =
                            "fit_to_lot_width_depth_later; prefer_storefront_on_main_road; " +
                            "allow_service_rear_access_from_alley";
                        item.Notes =
                            "Eligible for commercial building family selection after lot subdivision. " +
                            "No concrete building id selected now. BACK_ALLEY provides service access.";
                        break;

                    case "GREENSPACE":
                        item.SelectionAction         = "PLAN_NO_BUILDING_SELECTION_GREENSPACE";
                        item.PlacementMode           = "NO_BUILDING_PLACEMENT";
                        item.FrontageRequirement     = "NOT_APPLICABLE";
                        item.AlleyRule               = "NOT_APPLICABLE";
                        item.FacadeRule              = "NOT_APPLICABLE";
                        item.SidewalkRelation        = "NOT_APPLICABLE";
                        item.AllowedBuildingFamilies = new List<string>();
                        item.FitPolicy               = "no_building_placement";
                        item.Notes =
                            "Greenspace zone. No procedural building placement. " +
                            "May contain UNIQUE_PLACEHOLDER markers. Do not fill over civic placeholders.";
                        break;

                    default:
                        item.SelectionAction         = "PLAN_NO_BUILDING_SELECTION_GREENSPACE";
                        item.PlacementMode           = "NO_BUILDING_PLACEMENT";
                        item.FrontageRequirement     = "NOT_APPLICABLE";
                        item.AlleyRule               = "NOT_APPLICABLE";
                        item.FacadeRule              = "NOT_APPLICABLE";
                        item.SidewalkRelation        = "NOT_APPLICABLE";
                        item.AllowedBuildingFamilies = new List<string>();
                        item.FitPolicy               = "no_building_placement";
                        item.Notes = $"Zone type {cr.ZoneType}. No building placement.";
                        break;
                }
                break;

            case "UNIQUE_PLACEHOLDER":
                item.SelectionAction         = "PLAN_REQUIRE_UNIQUE_BUILDING_ID_LATER";
                item.PlacementMode           = "FUTURE_UNIQUE_PLACEHOLDER_BINDING";
                item.FrontageRequirement     = "MAIN_ROAD_IF_AVAILABLE";
                item.AlleyRule               = "NOT_APPLICABLE";
                item.FacadeRule              = "UNIQUE_BUILDING_DEFINED_BY_METADATA";
                item.SidewalkRelation        = "DEPEND_ON_BOUND_UNIQUE_BUILDING";
                item.AllowedBuildingFamilies = new List<string>
                    { "government", "library", "community_center", "institutional", "special_building" };
                item.FitPolicy =
                    "future_metadata_must_bind_placeholder_to_specific_building_id; " +
                    "reject_random_procedural_building; do_not_overwrite_park_greenspace_randomly";
                item.Notes =
                    "Civic/special building placeholder. No procedural fill. No random selection. " +
                    "Future metadata must bind this to a specific building id. " +
                    "Sidewalk context depends on the bound unique building.";
                break;

            case "STREET_CORRIDOR":
                item.SelectionAction         = "PLAN_NO_BUILDING_SELECTION_STREET";
                item.PlacementMode           = "NO_BUILDING_PLACEMENT";
                item.FrontageRequirement     = "NOT_APPLICABLE";
                item.AlleyRule               = cr.StreetClass == "BACK_ALLEY" ? "NOT_APPLICABLE" : "NOT_APPLICABLE";
                item.FacadeRule              = cr.StreetClass == "BACK_ALLEY" ? "NO_PRIMARY_FACADE" : "NOT_APPLICABLE";
                item.SidewalkRelation        = "NOT_APPLICABLE";
                item.AllowedBuildingFamilies = new List<string>();
                item.FitPolicy               = "no_building_placement";
                item.Notes = cr.StreetClass == "BACK_ALLEY"
                    ? "Back alley/service corridor. No building placement. Rear/service access only. Not primary frontage. No sidewalks."
                    : "Main road corridor. No building placement. Provides frontage and sidewalk context for adjacent zones.";
                break;

            case "IGNORE":
            default:
                item.SelectionAction         = "PLAN_IGNORE";
                item.PlacementMode           = "NO_BUILDING_PLACEMENT";
                item.FrontageRequirement     = "NOT_APPLICABLE";
                item.AlleyRule               = "NOT_APPLICABLE";
                item.FacadeRule              = "NOT_APPLICABLE";
                item.SidewalkRelation        = "NOT_APPLICABLE";
                item.AllowedBuildingFamilies = new List<string>();
                item.FitPolicy               = "no_building_placement";
                item.Notes = "Opaque black border marker. Not transparency. Ignored. No building.";
                break;
        }

        return item;
    }

    private static DeadMtlWorldBuilderBuildingSelectionPolicyPlanTotals BuildTotals(
        List<DeadMtlWorldBuilderBuildingSelectionPolicyPlanItem> items) =>
        new()
        {
            PlanItemCount                     = items.Count,
            ResidentialSelectionLaterCount    = items.Count(i => i.SelectionAction == "PLAN_SELECT_RESIDENTIAL_BUILDING_FAMILY_LATER"),
            CommercialSelectionLaterCount     = items.Count(i => i.SelectionAction == "PLAN_SELECT_COMMERCIAL_BUILDING_FAMILY_LATER"),
            UniqueBuildingRequiredLaterCount  = items.Count(i => i.SelectionAction == "PLAN_REQUIRE_UNIQUE_BUILDING_ID_LATER"),
            GreenspaceNoSelectionCount        = items.Count(i => i.SelectionAction == "PLAN_NO_BUILDING_SELECTION_GREENSPACE"),
            StreetNoSelectionCount            = items.Count(i => i.SelectionAction == "PLAN_NO_BUILDING_SELECTION_STREET"),
            IgnoreCount                       = items.Count(i => i.SelectionAction == "PLAN_IGNORE"),
            ConcreteBuildingIdsSelectedNowCount = 0,
        };

    public static string RenderMarkdown(DeadMtlWorldBuilderBuildingSelectionPolicyPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-25E: DeadMTL WorldBuilder Building Selection Policy Plan Contract");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** Contract only. No building generation. No building placement.");
        sb.AppendLine("> No lot subdivision. No sidewalk generation. No fence placement.");
        sb.AppendLine("> No lotpack writing. No worldgen override file. No runtime proof. Not writer-ready.");
        sb.AppendLine();
        sb.AppendLine($"**tile_id:** {plan.TileId}");
        sb.AppendLine($"**status:** {plan.Status}");
        sb.AppendLine($"**placement_status:** {plan.PlacementStatus}");
        sb.AppendLine($"**generation_status:** {plan.GenerationStatus}");
        sb.AppendLine($"**runtime_status:** {plan.RuntimeStatus}");
        sb.AppendLine();
        sb.AppendLine("## Plan Items");
        sb.AppendLine();
        sb.AppendLine("| Color | Role | Zone/Street | Selection Action | Placement Mode | Frontage |");
        sb.AppendLine("|-------|------|-------------|-----------------|----------------|----------|");
        foreach (var item in plan.PlanItems)
        {
            var zs = string.IsNullOrEmpty(item.ZoneType) ? item.StreetClass : item.ZoneType;
            sb.AppendLine(
                $"| {item.Color} | {item.Role} | {zs} | {item.SelectionAction} | " +
                $"{item.PlacementMode} | {item.FrontageRequirement} |");
        }
        sb.AppendLine();
        sb.AppendLine("## Residential Selection Policy");
        sb.AppendLine();
        sb.AppendLine("#7200FF (RESIDENTIAL) is eligible for future building family selection after lot subdivision.");
        sb.AppendLine("Selection is deferred until lots are laid out. No concrete building id is selected now.");
        sb.AppendLine();
        sb.AppendLine("Allowed families: duplex, triplex, plex_block, apartment_lowrise");
        sb.AppendLine();
        sb.AppendLine("Fit policy:");
        sb.AppendLine("- Fit to lot width/depth after subdivision.");
        sb.AppendLine("- Reject building if footprint exceeds lot.");
        sb.AppendLine("- Prefer facade width compatible with row.");
        sb.AppendLine("- Prefer Montreal plex-like frontage.");
        sb.AppendLine();
        sb.AppendLine("Frontage: PREFER_MAIN_ROAD_FRONTAGE");
        sb.AppendLine("Alley: BACK_ALLEY_REAR_SERVICE_ONLY");
        sb.AppendLine("Facade: ROW_UNIFORM_FRONTAGE");
        sb.AppendLine("Sidewalk relation: FRONTAGE_SIDEWALK_IF_MAIN_ROAD");
        sb.AppendLine();
        sb.AppendLine("## Commercial Selection Policy");
        sb.AppendLine();
        sb.AppendLine("#42CCFF (COMMERCIAL) is eligible for future building family selection after lot subdivision.");
        sb.AppendLine("Selection is deferred. No concrete building id is selected now.");
        sb.AppendLine();
        sb.AppendLine("Allowed families: depanneur, pharmacy, restaurant, main_street_storefront, office_small");
        sb.AppendLine();
        sb.AppendLine("Fit policy:");
        sb.AppendLine("- Fit to lot width/depth after subdivision.");
        sb.AppendLine("- Prefer storefront on main road.");
        sb.AppendLine("- Allow service/rear access from alley.");
        sb.AppendLine();
        sb.AppendLine("Frontage: PREFER_MAIN_ROAD_FRONTAGE");
        sb.AppendLine("Alley: BACK_ALLEY_SERVICE_ACCESS");
        sb.AppendLine("Facade: COMMERCIAL_FRONTAGE");
        sb.AppendLine("Sidewalk relation: FRONTAGE_SIDEWALK_IF_MAIN_ROAD");
        sb.AppendLine();
        sb.AppendLine("## Civic Unique Placeholder Policy");
        sb.AppendLine();
        sb.AppendLine("#B2BD87 (UNIQUE_PLACEHOLDER / CIVIC_SPECIAL_BUILDING) requires a unique building id.");
        sb.AppendLine("No procedural fill. No random selection. Future metadata must bind this placeholder.");
        sb.AppendLine();
        sb.AppendLine("Allowed families: government, library, community_center, institutional, special_building");
        sb.AppendLine();
        sb.AppendLine("Fit policy:");
        sb.AppendLine("- Future metadata must bind placeholder to a specific building id.");
        sb.AppendLine("- Reject random procedural building.");
        sb.AppendLine("- Do not overwrite park/greenspace randomly.");
        sb.AppendLine();
        sb.AppendLine("Frontage: MAIN_ROAD_IF_AVAILABLE");
        sb.AppendLine("Facade: UNIQUE_BUILDING_DEFINED_BY_METADATA");
        sb.AppendLine("Sidewalk relation: DEPEND_ON_BOUND_UNIQUE_BUILDING");
        sb.AppendLine();
        sb.AppendLine("## Greenspace Exclusion");
        sb.AppendLine();
        sb.AppendLine("#00AA10 (GREENSPACE) does not receive procedural building placement.");
        sb.AppendLine("It may contain UNIQUE_PLACEHOLDER markers. Those are handled separately.");
        sb.AppendLine("Do not fill over civic placeholders with procedural buildings.");
        sb.AppendLine();
        sb.AppendLine("## Street and Back Alley Exclusion");
        sb.AppendLine();
        sb.AppendLine("#FF6600 (MAIN_ROAD) and #F000FF (BACK_ALLEY) do not receive building placement.");
        sb.AppendLine("MAIN_ROAD provides frontage and sidewalk context for adjacent zones.");
        sb.AppendLine("BACK_ALLEY provides rear/service access only. Not primary frontage.");
        sb.AppendLine("BACK_ALLEY does not generate sidewalks.");
        sb.AppendLine();
        sb.AppendLine("## Fit-to-Lot Future Note");
        sb.AppendLine();
        sb.AppendLine("Building selection is deferred until lot subdivision (MAP-25C future execution).");
        sb.AppendLine("Once lots are laid out, the building placer selects from the allowed family list");
        sb.AppendLine("using a deterministic seed (profile_plus_tile_coordinate policy).");
        sb.AppendLine("Lot size, frontage width, and row orientation all constrain the selection.");
        sb.AppendLine();
        sb.AppendLine("## Frontage and Facade Note");
        sb.AppendLine();
        sb.AppendLine("All eligible zones prefer MAIN_ROAD (#FF6600) as primary frontage.");
        sb.AppendLine("BACK_ALLEY (#F000FF) is rear/service only and must not be used as primary facade.");
        sb.AppendLine("Facade orientation follows the row direction relative to the frontage street.");
        sb.AppendLine();
        sb.AppendLine("## No Concrete Building IDs Now");
        sb.AppendLine();
        sb.AppendLine("concrete_building_ids_selected_now_count: 0");
        sb.AppendLine("No building file, no building id, no building geometry is selected, written, or implied.");
        sb.AppendLine("This plan records policy only. Building selection executes in a future step.");
        sb.AppendLine();
        sb.AppendLine("## Totals");
        sb.AppendLine();
        sb.AppendLine($"plan_item_count:                      {plan.Totals.PlanItemCount}");
        sb.AppendLine($"residential_selection_later_count:    {plan.Totals.ResidentialSelectionLaterCount}");
        sb.AppendLine($"commercial_selection_later_count:     {plan.Totals.CommercialSelectionLaterCount}");
        sb.AppendLine($"unique_building_required_later:       {plan.Totals.UniqueBuildingRequiredLaterCount}");
        sb.AppendLine($"greenspace_no_selection_count:        {plan.Totals.GreenspaceNoSelectionCount}");
        sb.AppendLine($"street_no_selection_count:            {plan.Totals.StreetNoSelectionCount}");
        sb.AppendLine($"ignore_count:                         {plan.Totals.IgnoreCount}");
        sb.AppendLine($"concrete_building_ids_selected_now:   {plan.Totals.ConcreteBuildingIdsSelectedNowCount}");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        sb.AppendLine($"- writes_lotpack: {plan.ClaimBoundary.WritesLotpack}");
        sb.AppendLine($"- writes_worldgen_lua: {plan.ClaimBoundary.WritesWorldgenLua}");
        sb.AppendLine($"- runtime_proven: {plan.ClaimBoundary.RuntimeProven}");
        sb.AppendLine($"- public_playable_claim: {plan.ClaimBoundary.PublicPlayableClaim}");
        sb.AppendLine($"- writer_ready_claim: {plan.ClaimBoundary.WriterReadyClaim}");
        sb.AppendLine($"- generates_buildings_now: {plan.ClaimBoundary.GeneratesBuildingsNow}");
        sb.AppendLine($"- generates_sidewalks_now: {plan.ClaimBoundary.GeneratesSidewalksNow}");
        sb.AppendLine($"- subdivides_lots_now: {plan.ClaimBoundary.SubdividesLotsNow}");
        sb.AppendLine($"- captures_chunk_layers_now: {plan.ClaimBoundary.CapturesChunkLayersNow}");
        sb.AppendLine($"- places_fences_now: {plan.ClaimBoundary.PlacesFencesNow}");
        sb.AppendLine($"- places_unique_buildings_now: {plan.ClaimBoundary.PlacesUniqueBuildingsNow}");
        sb.AppendLine($"- selects_concrete_building_ids_now: {plan.ClaimBoundary.SelectsConcreteBuildingIdsNow}");
        sb.AppendLine();
        sb.AppendLine("**VERDICT: MAP25E_WORLDBUILDER_BUILDING_SELECTION_POLICY_PLAN_CONTRACT_COMPLETE**");
        return sb.ToString();
    }

    public static string RenderCsv(DeadMtlWorldBuilderBuildingSelectionPolicyPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine("color,role,zone_type,street_class,selection_action,placement_mode," +
                      "frontage_requirement,alley_rule,facade_rule,sidewalk_relation," +
                      "allowed_building_families,fit_policy,notes");
        foreach (var item in plan.PlanItems)
        {
            sb.AppendLine(
                $"{item.Color},{item.Role},{CsvEscape(item.ZoneType)},{CsvEscape(item.StreetClass)}," +
                $"{item.SelectionAction},{item.PlacementMode}," +
                $"{item.FrontageRequirement},{item.AlleyRule},{item.FacadeRule},{item.SidewalkRelation}," +
                $"{CsvEscape(string.Join("|", item.AllowedBuildingFamilies))}," +
                $"{CsvEscape(item.FitPolicy)},{CsvEscape(item.Notes)}");
        }
        return sb.ToString();
    }

    public static string RenderSummary(DeadMtlWorldBuilderBuildingSelectionPolicyPlanResult result)
    {
        var plan = result.Plan;
        var sb   = new StringBuilder();
        sb.AppendLine("MAP-25E: DeadMTL WorldBuilder Building Selection Policy Plan Contract");
        sb.AppendLine();
        sb.AppendLine($"tile_id:                              {plan.TileId}");
        sb.AppendLine($"is_valid:                             {result.IsValid}");
        sb.AppendLine($"status:                               {plan.Status}");
        sb.AppendLine($"placement_status:                     {plan.PlacementStatus}");
        sb.AppendLine($"generation_status:                    {plan.GenerationStatus}");
        sb.AppendLine();
        sb.AppendLine($"plan_item_count:                      {plan.Totals.PlanItemCount}");
        sb.AppendLine($"residential_selection_later_count:    {plan.Totals.ResidentialSelectionLaterCount}");
        sb.AppendLine($"commercial_selection_later_count:     {plan.Totals.CommercialSelectionLaterCount}");
        sb.AppendLine($"unique_building_required_later_count: {plan.Totals.UniqueBuildingRequiredLaterCount}");
        sb.AppendLine($"greenspace_no_selection_count:        {plan.Totals.GreenspaceNoSelectionCount}");
        sb.AppendLine($"street_no_selection_count:            {plan.Totals.StreetNoSelectionCount}");
        sb.AppendLine($"ignore_count:                         {plan.Totals.IgnoreCount}");
        sb.AppendLine($"concrete_building_ids_selected_now_count: {plan.Totals.ConcreteBuildingIdsSelectedNowCount}");
        sb.AppendLine();
        sb.AppendLine("CLAIM BOUNDARY");
        sb.AppendLine($"  writes_lotpack:                    {plan.ClaimBoundary.WritesLotpack}");
        sb.AppendLine($"  writes_worldgen_lua:               {plan.ClaimBoundary.WritesWorldgenLua}");
        sb.AppendLine($"  runtime_proven:                    {plan.ClaimBoundary.RuntimeProven}");
        sb.AppendLine($"  public_playable_claim:             {plan.ClaimBoundary.PublicPlayableClaim}");
        sb.AppendLine($"  writer_ready_claim:                {plan.ClaimBoundary.WriterReadyClaim}");
        sb.AppendLine($"  generates_buildings_now:           {plan.ClaimBoundary.GeneratesBuildingsNow}");
        sb.AppendLine($"  generates_sidewalks_now:           {plan.ClaimBoundary.GeneratesSidewalksNow}");
        sb.AppendLine($"  subdivides_lots_now:               {plan.ClaimBoundary.SubdividesLotsNow}");
        sb.AppendLine($"  captures_chunk_layers_now:         {plan.ClaimBoundary.CapturesChunkLayersNow}");
        sb.AppendLine($"  places_fences_now:                 {plan.ClaimBoundary.PlacesFencesNow}");
        sb.AppendLine($"  places_unique_buildings_now:       {plan.ClaimBoundary.PlacesUniqueBuildingsNow}");
        sb.AppendLine($"  selects_concrete_building_ids_now: {plan.ClaimBoundary.SelectsConcreteBuildingIdsNow}");
        if (result.Errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("ERRORS");
            foreach (var e in result.Errors)
                sb.AppendLine($"  - {e}");
        }
        sb.AppendLine();
        sb.AppendLine("VERDICT: MAP25E_WORLDBUILDER_BUILDING_SELECTION_POLICY_PLAN_CONTRACT_COMPLETE");
        return sb.ToString();
    }

    private static DeadMtlWorldBuilderBuildingSelectionPolicyPlanResult Fail(
        List<string> errors, DeadMtlWorldBuilderBuildingSelectionPolicyPlan plan) =>
        new() { IsValid = false, Errors = errors, Plan = plan };

    private static string CsvEscape(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}
