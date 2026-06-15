using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class DeadMtlWorldBuilderSidewalkGenerationPlanBuilder
{
    public static DeadMtlWorldBuilderSidewalkGenerationPlanResult Build(
        string profilePath, string metadataPath, string lotPlanPath)
    {
        var errors = new List<string>();
        var plan   = new DeadMtlWorldBuilderSidewalkGenerationPlan
        {
            SourceProfileJson             = profilePath,
            SourceZoneMetadataJson        = metadataPath,
            SourceLotSubdivisionPlanJson  = lotPlanPath,
        };

        // --- Load profile ---
        if (!File.Exists(profilePath))
        {
            errors.Add($"Neighborhood profile not found: {profilePath}");
            return Fail(errors, plan);
        }

        DeadMtlWorldBuilderNeighborhoodProfile profile;
        try
        {
            var json = File.ReadAllText(profilePath, Encoding.UTF8);
            var opts = new JsonSerializerOptions
                { AllowTrailingCommas = true, PropertyNameCaseInsensitive = false };
            profile = JsonSerializer.Deserialize<DeadMtlWorldBuilderNeighborhoodProfile>(json, opts)
                      ?? new DeadMtlWorldBuilderNeighborhoodProfile();
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse profile JSON: {ex.Message}");
            return Fail(errors, plan);
        }

        // --- Load metadata ---
        if (!File.Exists(metadataPath))
        {
            errors.Add($"Zone metadata not found: {metadataPath}");
            return Fail(errors, plan);
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
            return Fail(errors, plan);
        }

        plan.TileId = metadata.TileId;

        // --- Load lot plan (required) ---
        if (!File.Exists(lotPlanPath))
        {
            errors.Add($"Lot subdivision plan not found: {lotPlanPath}");
            return Fail(errors, plan);
        }

        DeadMtlWorldBuilderLotSubdivisionPlan lotPlan;
        try
        {
            var json = File.ReadAllText(lotPlanPath, Encoding.UTF8);
            var opts = new JsonSerializerOptions
                { AllowTrailingCommas = true, PropertyNameCaseInsensitive = false };
            lotPlan = JsonSerializer.Deserialize<DeadMtlWorldBuilderLotSubdivisionPlan>(json, opts)
                      ?? new DeadMtlWorldBuilderLotSubdivisionPlan();
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse lot subdivision plan JSON: {ex.Message}");
            return Fail(errors, plan);
        }

        // --- Resolve sidewalk policy from profile ---
        var sp            = profile.SidewalkPolicy;
        int leftWidth     = sp?.DefaultLeftWidthTiles  ?? 0;
        int rightWidth    = sp?.DefaultRightWidthTiles ?? 0;
        string swSource   = sp?.SidewalkSource ?? "INSIDE_STREET_ZONE";

        // --- Build plan items from STREET_CORRIDOR color roles only ---
        foreach (var cr in metadata.ColorRoles.Where(r => r.Role == "STREET_CORRIDOR"))
        {
            var item = new DeadMtlWorldBuilderSidewalkGenerationPlanItem
            {
                Color = cr.Color,
                Role  = cr.Role,
                StreetClass = cr.StreetClass,
            };

            if (cr.StreetClass == "MAIN_ROAD" && cr.SidewalkEligible)
            {
                item.SidewalkAction   = "PLAN_GENERATE_SIDEWALKS_LATER";
                item.LeftWidthTiles   = leftWidth;
                item.RightWidthTiles  = rightWidth;
                item.SidewalkSource   = swSource;
                item.SourcePolicy     = "NEIGHBORHOOD_PROFILE";
                item.ClaimStatus      = "NOT_EXECUTED";
                item.Notes = $"Sidewalk dimensions from profile. " +
                             $"Left={leftWidth} tiles, Right={rightWidth} tiles. " +
                             $"Source={swSource}. Not generated in MAP-25D.";
            }
            else if (cr.StreetClass == "BACK_ALLEY")
            {
                item.SidewalkAction  = "PLAN_NO_SIDEWALKS";
                item.LeftWidthTiles  = 0;
                item.RightWidthTiles = 0;
                item.SidewalkSource  = "NONE";
                item.SourcePolicy    = "BACK_ALLEY_RULE";
                item.ClaimStatus     = "NOT_EXECUTED";
                item.Notes = "Back alley / service corridor. No sidewalks. Not primary frontage. " +
                             "Rear/service access only.";
            }
            else
            {
                // MAIN_ROAD not eligible, or unknown class
                item.SidewalkAction  = "PLAN_NO_SIDEWALKS";
                item.LeftWidthTiles  = 0;
                item.RightWidthTiles = 0;
                item.SidewalkSource  = "NONE";
                item.SourcePolicy    = "NOT_SIDEWALK_ELIGIBLE";
                item.ClaimStatus     = "NOT_EXECUTED";
                item.Notes = $"Street class {cr.StreetClass} is not sidewalk eligible.";
            }

            plan.PlanItems.Add(item);
        }

        // --- Totals ---
        plan.Totals = new DeadMtlWorldBuilderSidewalkGenerationPlanTotals
        {
            PlanItemCount              = plan.PlanItems.Count,
            MainRoadSidewalkLaterCount = plan.PlanItems.Count(i => i.SidewalkAction == "PLAN_GENERATE_SIDEWALKS_LATER"),
            BackAlleyNoSidewalkCount   = plan.PlanItems.Count(i => i.StreetClass == "BACK_ALLEY"),
            LeftWidthTilesTotal        = plan.PlanItems.Sum(i => i.LeftWidthTiles),
            RightWidthTilesTotal       = plan.PlanItems.Sum(i => i.RightWidthTiles),
            SidewalkSource             = swSource,
        };

        // --- Claim boundary: all false ---
        plan.ClaimBoundary = new DeadMtlWorldBuilderSidewalkGenerationPlanClaimBoundary
        {
            WritesLotpack           = false,
            WritesWorldgenLua       = false,
            RuntimeProven           = false,
            PublicPlayableClaim     = false,
            WriterReadyClaim        = false,
            GeneratesBuildingsNow   = false,
            GeneratesSidewalksNow   = false,
            SubdividesLotsNow       = false,
            CapturesChunkLayersNow  = false,
            PlacesFencesNow         = false,
            PlacesUniqueBuildingsNow = false,
        };

        return new DeadMtlWorldBuilderSidewalkGenerationPlanResult
            { IsValid = true, Errors = errors, Plan = plan };
    }

    public static string RenderMarkdown(DeadMtlWorldBuilderSidewalkGenerationPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-25D: DeadMTL WorldBuilder Sidewalk Generation Plan Contract");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** Contract only. No sidewalk generation. No terrain mutation.");
        sb.AppendLine("> No PNG changes. No lotpack writing. No worldgen override file.");
        sb.AppendLine("> No runtime proof. Not writer-ready.");
        sb.AppendLine();
        sb.AppendLine($"**tile_id:** {plan.TileId}");
        sb.AppendLine($"**status:** {plan.Status}");
        sb.AppendLine($"**generation_status:** {plan.GenerationStatus}");
        sb.AppendLine($"**runtime_status:** {plan.RuntimeStatus}");
        sb.AppendLine();
        sb.AppendLine("## Sidewalk Plan Items");
        sb.AppendLine();
        sb.AppendLine("| Color | Street Class | Sidewalk Action | Left Tiles | Right Tiles | Source | Policy |");
        sb.AppendLine("|-------|-------------|-----------------|-----------|------------|--------|--------|");
        foreach (var item in plan.PlanItems)
        {
            sb.AppendLine(
                $"| {item.Color} | {item.StreetClass} | {item.SidewalkAction} | " +
                $"{item.LeftWidthTiles} | {item.RightWidthTiles} | {item.SidewalkSource} | {item.SourcePolicy} |");
        }
        sb.AppendLine();
        sb.AppendLine("## Sidewalk Eligibility");
        sb.AppendLine();
        sb.AppendLine("Only `MAIN_ROAD` corridors (`#FF6600`) with `sidewalk_eligible = true` generate sidewalks.");
        sb.AppendLine("Sidewalk dimensions come from the active neighborhood profile (`deadmtl_baseline`):");
        sb.AppendLine($"- left_width_tiles:  {plan.Totals.LeftWidthTilesTotal}");
        sb.AppendLine($"- right_width_tiles: {plan.Totals.RightWidthTilesTotal}");
        sb.AppendLine($"- sidewalk_source:   {plan.Totals.SidewalkSource}");
        sb.AppendLine();
        sb.AppendLine("## Back Alley Note");
        sb.AppendLine();
        sb.AppendLine("`BACK_ALLEY` corridors (`#F000FF`) never receive sidewalks.");
        sb.AppendLine("`source_policy = BACK_ALLEY_RULE`. `sidewalk_source = NONE`. Width is always 0.");
        sb.AppendLine("Back alleys are rear/service access only. Not primary frontage.");
        sb.AppendLine();
        sb.AppendLine("## Sidewalk Source: INSIDE_STREET_ZONE");
        sb.AppendLine();
        sb.AppendLine("The active profile uses `sidewalk_source = INSIDE_STREET_ZONE`.");
        sb.AppendLine("This means sidewalks are carved from inside the street corridor width,");
        sb.AppendLine("not stolen from adjacent residential or commercial lots.");
        sb.AppendLine("If a future profile says `OUTSIDE_STREET_ZONE`, sidewalks may consume");
        sb.AppendLine("lot boundary space. MAP-25D plans the policy only; it does not execute.");
        sb.AppendLine();
        sb.AppendLine("## Totals");
        sb.AppendLine();
        sb.AppendLine($"plan_item_count:               {plan.Totals.PlanItemCount}");
        sb.AppendLine($"main_road_sidewalk_later:      {plan.Totals.MainRoadSidewalkLaterCount}");
        sb.AppendLine($"back_alley_no_sidewalk:        {plan.Totals.BackAlleyNoSidewalkCount}");
        sb.AppendLine($"left_width_tiles_total:        {plan.Totals.LeftWidthTilesTotal}");
        sb.AppendLine($"right_width_tiles_total:       {plan.Totals.RightWidthTilesTotal}");
        sb.AppendLine($"sidewalk_source:               {plan.Totals.SidewalkSource}");
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
        sb.AppendLine("**VERDICT: MAP25D_WORLDBUILDER_SIDEWALK_GENERATION_PLAN_CONTRACT_COMPLETE**");
        return sb.ToString();
    }

    public static string RenderCsv(DeadMtlWorldBuilderSidewalkGenerationPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine("color,role,street_class,sidewalk_action,left_width_tiles," +
                      "right_width_tiles,sidewalk_source,source_policy,claim_status,notes");
        foreach (var item in plan.PlanItems)
        {
            sb.AppendLine(
                $"{item.Color},{item.Role},{item.StreetClass},{item.SidewalkAction}," +
                $"{item.LeftWidthTiles},{item.RightWidthTiles},{item.SidewalkSource}," +
                $"{item.SourcePolicy},{item.ClaimStatus},{CsvEscape(item.Notes)}");
        }
        return sb.ToString();
    }

    public static string RenderSummary(DeadMtlWorldBuilderSidewalkGenerationPlanResult result)
    {
        var plan = result.Plan;
        var sb   = new StringBuilder();
        sb.AppendLine("MAP-25D: DeadMTL WorldBuilder Sidewalk Generation Plan Contract");
        sb.AppendLine();
        sb.AppendLine($"tile_id:                       {plan.TileId}");
        sb.AppendLine($"is_valid:                      {result.IsValid}");
        sb.AppendLine($"status:                        {plan.Status}");
        sb.AppendLine($"generation_status:             {plan.GenerationStatus}");
        sb.AppendLine();
        sb.AppendLine($"plan_item_count:               {plan.Totals.PlanItemCount}");
        sb.AppendLine($"main_road_sidewalk_later_count:{plan.Totals.MainRoadSidewalkLaterCount}");
        sb.AppendLine($"back_alley_no_sidewalk_count:  {plan.Totals.BackAlleyNoSidewalkCount}");
        sb.AppendLine($"left_width_tiles_total:        {plan.Totals.LeftWidthTilesTotal}");
        sb.AppendLine($"right_width_tiles_total:       {plan.Totals.RightWidthTilesTotal}");
        sb.AppendLine($"sidewalk_source:               {plan.Totals.SidewalkSource}");
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
        sb.AppendLine("VERDICT: MAP25D_WORLDBUILDER_SIDEWALK_GENERATION_PLAN_CONTRACT_COMPLETE");
        return sb.ToString();
    }

    private static DeadMtlWorldBuilderSidewalkGenerationPlanResult Fail(
        List<string> errors, DeadMtlWorldBuilderSidewalkGenerationPlan plan) =>
        new() { IsValid = false, Errors = errors, Plan = plan };

    private static string CsvEscape(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}
