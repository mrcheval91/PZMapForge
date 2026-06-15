using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class DeadMtlWorldBuilderLotSubdivisionPlanBuilder
{
    public static DeadMtlWorldBuilderLotSubdivisionPlanResult Build(
        string metadataPath, string profilePath, string inspectionPath)
    {
        var errors = new List<string>();
        var plan   = new DeadMtlWorldBuilderLotSubdivisionPlan
        {
            SourceZoneMetadataJson       = metadataPath,
            SourceNeighborhoodProfileJson = profilePath,
            SourceInspectionJson         = inspectionPath,
        };

        if (!File.Exists(metadataPath))
        {
            errors.Add($"Zone metadata file not found: {metadataPath}");
            return new DeadMtlWorldBuilderLotSubdivisionPlanResult
                { IsValid = false, Errors = errors, Plan = plan };
        }

        DeadMtlWorldBuilderRawTileZoneMetadata metadata;
        try
        {
            var json = File.ReadAllText(metadataPath, Encoding.UTF8);
            var opts = new JsonSerializerOptions
                { AllowTrailingCommas = true, PropertyNameCaseInsensitive = false };
            metadata = JsonSerializer.Deserialize<DeadMtlWorldBuilderRawTileZoneMetadata>(json, opts)
                       ?? new DeadMtlWorldBuilderRawTileZoneMetadata();
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse zone metadata JSON: {ex.Message}");
            return new DeadMtlWorldBuilderLotSubdivisionPlanResult
                { IsValid = false, Errors = errors, Plan = plan };
        }

        plan.TileId = metadata.TileId;

        if (!File.Exists(profilePath))
            errors.Add($"Neighborhood profile not found: {profilePath}");

        // Build plan items
        foreach (var cr in metadata.ColorRoles)
            plan.PlanItems.Add(BuildItem(cr));

        // Build totals
        plan.Totals = BuildTotals(plan.PlanItems);

        // Validate and copy claim boundary
        if (metadata.ClaimBoundary != null)
        {
            var cb = metadata.ClaimBoundary;
            plan.ClaimBoundary = new DeadMtlWorldBuilderLotSubdivisionPlanClaimBoundary
            {
                WritesLotpack           = cb.WritesLotpack,
                WritesWorldgenLua       = cb.WritesWorldgenLua,
                RuntimeProven           = cb.RuntimeProven,
                PublicPlayableClaim     = cb.PublicPlayableClaim,
                WriterReadyClaim        = cb.WriterReadyClaim,
                GeneratesBuildingsNow   = cb.GeneratesBuildingsNow,
                GeneratesSidewalksNow   = cb.GeneratesSidewalksNow,
                SubdividesLotsNow       = cb.SubdividesLotsNow,
                CapturesChunkLayersNow  = cb.CapturesChunkLayersNow,
                PlacesFencesNow         = false,
                PlacesUniqueBuildingsNow = false,
            };
            if (cb.WritesLotpack)           errors.Add("writes_lotpack must be false.");
            if (cb.WritesWorldgenLua)       errors.Add("writes_worldgen_lua must be false.");
            if (cb.RuntimeProven)           errors.Add("runtime_proven must be false.");
            if (cb.PublicPlayableClaim)     errors.Add("public_playable_claim must be false.");
            if (cb.WriterReadyClaim)        errors.Add("writer_ready_claim must be false.");
            if (cb.GeneratesBuildingsNow)   errors.Add("generates_buildings_now must be false.");
            if (cb.GeneratesSidewalksNow)   errors.Add("generates_sidewalks_now must be false.");
            if (cb.SubdividesLotsNow)       errors.Add("subdivides_lots_now must be false.");
            if (cb.CapturesChunkLayersNow)  errors.Add("captures_chunk_layers_now must be false.");
        }
        else
        {
            errors.Add("claim_boundary is missing from zone metadata.");
        }

        // Color normalization guards
        var colorSet = metadata.ColorRoles
            .Select(r => r.Color)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (colorSet.Contains("#00AA00") && !colorSet.Contains("#00AA10"))
            errors.Add(
                "#00AA00 found in metadata but #00AA10 absent — do not normalize #00AA10 to #00AA00.");

        if (colorSet.Contains("#FF00FF") && !colorSet.Contains("#F000FF"))
            errors.Add(
                "#FF00FF found in metadata but #F000FF absent — do not normalize #F000FF to #FF00FF.");

        bool isValid = errors.Count == 0;
        return new DeadMtlWorldBuilderLotSubdivisionPlanResult
            { IsValid = isValid, Errors = errors, Plan = plan };
    }

    private static DeadMtlWorldBuilderLotSubdivisionPlanItem BuildItem(
        DeadMtlWorldBuilderRawTileColorRole cr)
    {
        var item = new DeadMtlWorldBuilderLotSubdivisionPlanItem
        {
            Color     = cr.Color,
            Role      = cr.Role,
            ZoneType  = cr.ZoneType,
            StreetClass = cr.StreetClass,
        };

        switch (cr.Role)
        {
            case "ZONE":
                switch (cr.ZoneType)
                {
                    case "RESIDENTIAL":
                        item.SubdivisionAction      = "PLAN_SUBDIVIDE_RESIDENTIAL_LATER";
                        item.FrontagePolicy         = "PREFER_MAIN_ROAD";
                        item.RearAccessPolicy       = "ALLOW_BACK_ALLEY_REAR_ACCESS";
                        item.SidewalkGenerationPolicy = "FUTURE_FROM_NEIGHBORHOOD_PROFILE_ON_MAIN_ROAD";
                        item.LotLinePolicy          = "FUTURE_LOT_LINES_AND_FENCES";
                        item.BuildingSelectionPolicy = "FUTURE_FIT_BUILDING_TO_LOT";
                        item.FacadeOrientationPolicy = "ROW_UNIFORM_FRONTAGE";
                        item.Notes = "Residential zone. Future lot subdivision. MAIN_ROAD preferred frontage. " +
                                     "BACK_ALLEY is rear/service access. Uniform facade orientation along each row.";
                        break;

                    case "COMMERCIAL":
                        item.SubdivisionAction      = "PLAN_SUBDIVIDE_COMMERCIAL_LATER";
                        item.FrontagePolicy         = "PREFER_MAIN_ROAD";
                        item.RearAccessPolicy       = "BACK_ALLEY_SERVICE_ONLY";
                        item.SidewalkGenerationPolicy = "FUTURE_FROM_NEIGHBORHOOD_PROFILE_ON_MAIN_ROAD";
                        item.LotLinePolicy          = "FUTURE_LOT_LINES_AND_FENCES";
                        item.BuildingSelectionPolicy = "FUTURE_FIT_BUILDING_TO_LOT";
                        item.FacadeOrientationPolicy = "COMMERCIAL_FRONTAGE";
                        item.Notes = "Commercial zone. Future lot subdivision if large enough. " +
                                     "MAIN_ROAD preferred frontage. BACK_ALLEY is service only.";
                        break;

                    default:
                        // GREENSPACE and others
                        item.SubdivisionAction      = "PLAN_NO_SUBDIVISION_GREENSPACE";
                        item.FrontagePolicy         = "NOT_APPLICABLE";
                        item.RearAccessPolicy       = "NOT_APPLICABLE";
                        item.SidewalkGenerationPolicy = "NOT_APPLICABLE";
                        item.LotLinePolicy          = "NOT_APPLICABLE";
                        item.BuildingSelectionPolicy = "NOT_APPLICABLE";
                        item.FacadeOrientationPolicy = "NOT_APPLICABLE";
                        item.Notes = $"Zone type {cr.ZoneType}. Not subdivided into lots. " +
                                     "May contain UNIQUE_PLACEHOLDER markers. Do not fill over civic placeholders.";
                        break;
                }
                break;

            case "STREET_CORRIDOR":
                item.SubdivisionAction = "PLAN_NO_SUBDIVISION_STREET_CORRIDOR";
                item.LotLinePolicy     = "NOT_APPLICABLE";
                switch (cr.StreetClass)
                {
                    case "MAIN_ROAD":
                        item.FrontagePolicy          = "NOT_APPLICABLE";
                        item.RearAccessPolicy        = "NOT_APPLICABLE";
                        item.SidewalkGenerationPolicy = "FUTURE_FROM_NEIGHBORHOOD_PROFILE_ON_MAIN_ROAD";
                        item.BuildingSelectionPolicy  = "NO_BUILDING_SELECTION";
                        item.FacadeOrientationPolicy  = "NOT_APPLICABLE";
                        item.Notes = "Main road corridor. No subdivision. Sidewalk-capable later via neighborhood profile.";
                        break;

                    case "BACK_ALLEY":
                        item.FrontagePolicy          = "NO_PRIMARY_FRONTAGE";
                        item.RearAccessPolicy        = "BACK_ALLEY_SERVICE_ONLY";
                        item.SidewalkGenerationPolicy = "NO_SIDEWALKS";
                        item.BuildingSelectionPolicy  = "NO_BUILDING_SELECTION";
                        item.FacadeOrientationPolicy  = "NO_PRIMARY_FACADE";
                        item.Notes = "Back alley / service corridor. No subdivision. No sidewalks. " +
                                     "Rear/service access only. Not primary frontage.";
                        break;

                    default:
                        item.FrontagePolicy          = "NOT_APPLICABLE";
                        item.RearAccessPolicy        = "NOT_APPLICABLE";
                        item.SidewalkGenerationPolicy = "NOT_APPLICABLE";
                        item.BuildingSelectionPolicy  = "NO_BUILDING_SELECTION";
                        item.FacadeOrientationPolicy  = "NOT_APPLICABLE";
                        item.Notes = $"Street corridor class {cr.StreetClass}. No subdivision.";
                        break;
                }
                break;

            case "UNIQUE_PLACEHOLDER":
                item.SubdivisionAction      = "PLAN_NO_SUBDIVISION_UNIQUE_PLACEHOLDER";
                item.FrontagePolicy         = "MAIN_ROAD_ONLY_IF_AVAILABLE";
                item.RearAccessPolicy       = "NOT_APPLICABLE";
                item.SidewalkGenerationPolicy = "NOT_APPLICABLE";
                item.LotLinePolicy          = "NOT_APPLICABLE";
                item.BuildingSelectionPolicy = "FUTURE_UNIQUE_BUILDING_ID_REQUIRED";
                item.FacadeOrientationPolicy = "NOT_APPLICABLE";
                item.Notes = "Civic/special building placeholder. No procedural subdivision or fill. " +
                             "Future: bind to specific building id (library, government, community center, etc.).";
                break;

            case "IGNORE":
            default:
                item.SubdivisionAction      = "PLAN_IGNORE";
                item.FrontagePolicy         = "NOT_APPLICABLE";
                item.RearAccessPolicy       = "NOT_APPLICABLE";
                item.SidewalkGenerationPolicy = "NOT_APPLICABLE";
                item.LotLinePolicy          = "NOT_APPLICABLE";
                item.BuildingSelectionPolicy = "NO_BUILDING_SELECTION";
                item.FacadeOrientationPolicy = "NOT_APPLICABLE";
                item.Notes = "Opaque black border marker. Not transparency. No subdivision. No building. Ignored.";
                break;
        }

        return item;
    }

    private static DeadMtlWorldBuilderLotSubdivisionPlanTotals BuildTotals(
        List<DeadMtlWorldBuilderLotSubdivisionPlanItem> items)
    {
        return new DeadMtlWorldBuilderLotSubdivisionPlanTotals
        {
            PlanItemCount                      = items.Count,
            ResidentialSubdivideLaterCount     = items.Count(i => i.SubdivisionAction == "PLAN_SUBDIVIDE_RESIDENTIAL_LATER"),
            CommercialSubdivideLaterCount      = items.Count(i => i.SubdivisionAction == "PLAN_SUBDIVIDE_COMMERCIAL_LATER"),
            StreetNoSubdivisionCount           = items.Count(i => i.SubdivisionAction == "PLAN_NO_SUBDIVISION_STREET_CORRIDOR"),
            GreenspaceNoSubdivisionCount       = items.Count(i => i.SubdivisionAction == "PLAN_NO_SUBDIVISION_GREENSPACE"),
            UniquePlaceholderNoSubdivisionCount = items.Count(i => i.SubdivisionAction == "PLAN_NO_SUBDIVISION_UNIQUE_PLACEHOLDER"),
            IgnoreCount                        = items.Count(i => i.SubdivisionAction == "PLAN_IGNORE"),
        };
    }

    public static string RenderMarkdown(DeadMtlWorldBuilderLotSubdivisionPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-25C: DeadMTL WorldBuilder Lot Subdivision Plan Contract");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** Contract only. No terrain generation. No actual lot subdivision.");
        sb.AppendLine("> No building placement. No sidewalk generation. No fence placement.");
        sb.AppendLine("> No lotpack writing. No runtime proof. Not writer-ready.");
        sb.AppendLine();
        sb.AppendLine($"**tile_id:** {plan.TileId}");
        sb.AppendLine($"**status:** {plan.Status}");
        sb.AppendLine($"**subdivision_status:** {plan.SubdivisionStatus}");
        sb.AppendLine($"**runtime_status:** {plan.RuntimeStatus}");
        sb.AppendLine();
        sb.AppendLine("## Plan Items");
        sb.AppendLine();
        sb.AppendLine("| Color | Role | Zone/Street | Subdivision Action | Frontage Policy | Sidewalk Policy |");
        sb.AppendLine("|-------|------|-------------|-------------------|----------------|-----------------|");
        foreach (var item in plan.PlanItems)
        {
            var zs = string.IsNullOrEmpty(item.ZoneType) ? item.StreetClass : item.ZoneType;
            sb.AppendLine($"| {item.Color} | {item.Role} | {zs} | {item.SubdivisionAction} | {item.FrontagePolicy} | {item.SidewalkGenerationPolicy} |");
        }
        sb.AppendLine();
        sb.AppendLine("## Frontage Policy");
        sb.AppendLine();
        sb.AppendLine("Residential and commercial zones prefer MAIN_ROAD (#FF6600) as their primary frontage.");
        sb.AppendLine("When a block has only one main road frontage, row lots should all face that main road.");
        sb.AppendLine("If multiple frontages exist, the future layout engine picks the best based on road class,");
        sb.AppendLine("block shape, adjacency, and neighborhood profile rules.");
        sb.AppendLine();
        sb.AppendLine("## Back Alley Note");
        sb.AppendLine();
        sb.AppendLine("BACK_ALLEY corridors (#F000FF) are rear/service access only.");
        sb.AppendLine("They must never be primary frontage unless a future explicit override says so.");
        sb.AppendLine("Residential lots avoid BACK_ALLEY frontage. Commercial lots use BACK_ALLEY for service only.");
        sb.AppendLine();
        sb.AppendLine("## Sidewalk Generation");
        sb.AppendLine();
        sb.AppendLine("Sidewalks are NOT generated here. They are a future layer generated from eligible MAIN_ROAD");
        sb.AppendLine("corridors using the active neighborhood profile (deadmtl_baseline).");
        sb.AppendLine("BACK_ALLEY corridors never generate sidewalks (sidewalk_generation_policy = NO_SIDEWALKS).");
        sb.AppendLine();
        sb.AppendLine("## Fence / Cloture Note");
        sb.AppendLine();
        sb.AppendLine("Fences and clotures are a future layer generated on lot lines AFTER lot subdivision.");
        sb.AppendLine("lot_line_policy = FUTURE_LOT_LINES_AND_FENCES for residential/commercial zones.");
        sb.AppendLine("Not generated in MAP-25C.");
        sb.AppendLine();
        sb.AppendLine("## Unique Placeholder (Civic / Special Building)");
        sb.AppendLine();
        sb.AppendLine("#B2BD87 is UNIQUE_PLACEHOLDER / CIVIC_SPECIAL_BUILDING.");
        sb.AppendLine("building_selection_policy = FUTURE_UNIQUE_BUILDING_ID_REQUIRED.");
        sb.AppendLine("No procedural fill. No subdivision. Future metadata binds this to a specific building id");
        sb.AppendLine("(library, government, community center, institutional, etc.).");
        sb.AppendLine();
        sb.AppendLine("## Procedural Fill");
        sb.AppendLine();
        sb.AppendLine("After lot subdivision (future), procedural fill selects a building family that fits each lot.");
        sb.AppendLine("Only ZONE colors with procedural_fill = true are eligible.");
        sb.AppendLine("UNIQUE_PLACEHOLDERs and IGNORE tiles are excluded from procedural fill.");
        sb.AppendLine();
        sb.AppendLine("## Totals");
        sb.AppendLine();
        sb.AppendLine($"plan_item_count:                      {plan.Totals.PlanItemCount}");
        sb.AppendLine($"residential_subdivide_later_count:    {plan.Totals.ResidentialSubdivideLaterCount}");
        sb.AppendLine($"commercial_subdivide_later_count:     {plan.Totals.CommercialSubdivideLaterCount}");
        sb.AppendLine($"street_no_subdivision_count:          {plan.Totals.StreetNoSubdivisionCount}");
        sb.AppendLine($"greenspace_no_subdivision_count:      {plan.Totals.GreenspaceNoSubdivisionCount}");
        sb.AppendLine($"unique_placeholder_no_subdivision:    {plan.Totals.UniquePlaceholderNoSubdivisionCount}");
        sb.AppendLine($"ignore_count:                         {plan.Totals.IgnoreCount}");
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
        sb.AppendLine();
        sb.AppendLine("**VERDICT: MAP25C_WORLDBUILDER_LOT_SUBDIVISION_PLAN_CONTRACT_COMPLETE**");
        return sb.ToString();
    }

    public static string RenderCsv(DeadMtlWorldBuilderLotSubdivisionPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine("color,role,zone_type,street_class,subdivision_action,frontage_policy," +
                      "rear_access_policy,sidewalk_generation_policy,lot_line_policy," +
                      "building_selection_policy,facade_orientation_policy,notes");
        foreach (var item in plan.PlanItems)
        {
            sb.AppendLine(
                $"{item.Color},{item.Role},{CsvEscape(item.ZoneType)},{CsvEscape(item.StreetClass)}," +
                $"{item.SubdivisionAction},{item.FrontagePolicy}," +
                $"{item.RearAccessPolicy},{item.SidewalkGenerationPolicy},{item.LotLinePolicy}," +
                $"{item.BuildingSelectionPolicy},{item.FacadeOrientationPolicy},{CsvEscape(item.Notes)}");
        }
        return sb.ToString();
    }

    public static string RenderSummary(DeadMtlWorldBuilderLotSubdivisionPlanResult result)
    {
        var plan = result.Plan;
        var sb   = new StringBuilder();
        sb.AppendLine("MAP-25C: DeadMTL WorldBuilder Lot Subdivision Plan Contract");
        sb.AppendLine();
        sb.AppendLine($"tile_id:                              {plan.TileId}");
        sb.AppendLine($"is_valid:                             {result.IsValid}");
        sb.AppendLine($"status:                               {plan.Status}");
        sb.AppendLine($"subdivision_status:                   {plan.SubdivisionStatus}");
        sb.AppendLine();
        sb.AppendLine($"plan_item_count:                      {plan.Totals.PlanItemCount}");
        sb.AppendLine($"residential_subdivide_later_count:    {plan.Totals.ResidentialSubdivideLaterCount}");
        sb.AppendLine($"commercial_subdivide_later_count:     {plan.Totals.CommercialSubdivideLaterCount}");
        sb.AppendLine($"street_no_subdivision_count:          {plan.Totals.StreetNoSubdivisionCount}");
        sb.AppendLine($"greenspace_no_subdivision_count:      {plan.Totals.GreenspaceNoSubdivisionCount}");
        sb.AppendLine($"unique_placeholder_count:             {plan.Totals.UniquePlaceholderNoSubdivisionCount}");
        sb.AppendLine($"ignore_count:                         {plan.Totals.IgnoreCount}");
        sb.AppendLine();
        sb.AppendLine("CLAIM BOUNDARY");
        sb.AppendLine($"  writes_lotpack:              {plan.ClaimBoundary.WritesLotpack}");
        sb.AppendLine($"  writes_worldgen_lua:         {plan.ClaimBoundary.WritesWorldgenLua}");
        sb.AppendLine($"  runtime_proven:              {plan.ClaimBoundary.RuntimeProven}");
        sb.AppendLine($"  public_playable_claim:       {plan.ClaimBoundary.PublicPlayableClaim}");
        sb.AppendLine($"  writer_ready_claim:          {plan.ClaimBoundary.WriterReadyClaim}");
        sb.AppendLine($"  generates_buildings_now:     {plan.ClaimBoundary.GeneratesBuildingsNow}");
        sb.AppendLine($"  generates_sidewalks_now:     {plan.ClaimBoundary.GeneratesSidewalksNow}");
        sb.AppendLine($"  subdivides_lots_now:         {plan.ClaimBoundary.SubdividesLotsNow}");
        sb.AppendLine($"  captures_chunk_layers_now:   {plan.ClaimBoundary.CapturesChunkLayersNow}");
        sb.AppendLine($"  places_fences_now:           {plan.ClaimBoundary.PlacesFencesNow}");
        sb.AppendLine($"  places_unique_buildings_now: {plan.ClaimBoundary.PlacesUniqueBuildingsNow}");
        if (result.Errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("ERRORS");
            foreach (var e in result.Errors)
                sb.AppendLine($"  - {e}");
        }
        sb.AppendLine();
        sb.AppendLine("VERDICT: MAP25C_WORLDBUILDER_LOT_SUBDIVISION_PLAN_CONTRACT_COMPLETE");
        return sb.ToString();
    }

    private static string CsvEscape(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}
