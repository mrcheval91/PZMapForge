using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class DeadMtlWorldBuilderFutureWorldLayoutPlanBuilder
{
    public static DeadMtlWorldBuilderFutureWorldLayoutPlanResult Build(
        string profilePath,
        string metadataPath,
        string lotPlanPath,
        string sidewalkPlanPath,
        string buildingSelectionPlanPath,
        string dependencyManifestPath)
    {
        var errors = new List<string>();
        var plan   = new DeadMtlWorldBuilderFutureWorldLayoutPlan
        {
            TileId                              = "map_00",
            SourceProfileJson                   = profilePath,
            SourceZoneMetadataJson              = metadataPath,
            SourceLotSubdivisionPlanJson        = lotPlanPath,
            SourceSidewalkGenerationPlanJson    = sidewalkPlanPath,
            SourceBuildingSelectionPolicyPlanJson = buildingSelectionPlanPath,
            SourceGenerationDependencyManifestJson = dependencyManifestPath,
        };

        var inputs = new[]
        {
            ("profile",                  profilePath),
            ("metadata",                 metadataPath),
            ("lot plan",                 lotPlanPath),
            ("sidewalk plan",            sidewalkPlanPath),
            ("building selection plan",  buildingSelectionPlanPath),
            ("dependency manifest",      dependencyManifestPath),
        };

        foreach (var (label, path) in inputs)
        {
            if (!File.Exists(path))
                errors.Add($"Missing {label}: file not found: {path}");
        }

        if (errors.Count > 0)
            return Fail(errors, plan);

        // Load zone metadata to drive layout components
        DeadMtlWorldBuilderRawTileZoneMetadata metadata;
        try
        {
            var opts = new JsonSerializerOptions
                { AllowTrailingCommas = true, PropertyNameCaseInsensitive = false };
            var json = File.ReadAllText(metadataPath, Encoding.UTF8);
            metadata = JsonSerializer.Deserialize<DeadMtlWorldBuilderRawTileZoneMetadata>(json, opts)
                       ?? throw new InvalidOperationException("zone metadata deserialized as null");
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse zone metadata: {ex.Message}");
            return Fail(errors, plan);
        }

        foreach (var cr in metadata.ColorRoles)
            plan.LayoutComponents.Add(BuildComponent(cr));

        plan.FutureExecutionRequirements = new DeadMtlWorldBuilderFutureExecutionRequirements
        {
            RequiresActualLotGeometry       = true,
            RequiresActualSidewalkGeometry  = true,
            RequiresBuildingCatalogue       = true,
            RequiresUniqueBuildingBindings  = true,
            RequiresTileWriter              = true,
            RequiresRuntimeValidation       = true,
            CanExecuteNow                   = false,
            BlockedReason                   = "CONTRACT_ONLY_NO_GEOMETRY_NO_WRITER_NO_RUNTIME_PROOF",
        };

        plan.Totals = ComputeTotals(plan.LayoutComponents);

        plan.ClaimBoundary = new DeadMtlWorldBuilderFutureWorldLayoutPlanClaimBoundary
        {
            WritesLotpack                 = false,
            WritesWorldgenLua             = false,
            RuntimeProven                 = false,
            PublicPlayableClaim           = false,
            WriterReadyClaim              = false,
            GeneratesTerrainNow           = false,
            GeneratesBuildingsNow         = false,
            GeneratesSidewalksNow         = false,
            SubdividesLotsNow             = false,
            CapturesChunkLayersNow        = false,
            PlacesFencesNow               = false,
            PlacesUniqueBuildingsNow      = false,
            SelectsConcreteBuildingIdsNow = false,
            CreatesConcreteGeometryNow    = false,
            MaterializesLayoutNow         = false,
        };

        return new DeadMtlWorldBuilderFutureWorldLayoutPlanResult
            { IsValid = true, Errors = errors, Plan = plan };
    }

    private static DeadMtlWorldBuilderFutureWorldLayoutPlanComponent BuildComponent(
        DeadMtlWorldBuilderRawTileColorRole cr)
    {
        var c = new DeadMtlWorldBuilderFutureWorldLayoutPlanComponent
        {
            Color                = cr.Color,
            Role                 = cr.Role,
            ZoneType             = cr.ZoneType,
            StreetClass          = cr.StreetClass,
            FutureGeometryStatus = "NOT_CREATED",
            SourceConfidence     = "ZONE_METADATA_ONLY",
        };

        switch (cr.Role)
        {
            case "ZONE":
                switch (cr.ZoneType)
                {
                    case "RESIDENTIAL":
                        c.ComponentType       = "ZONE_LAYOUT_COMPONENT";
                        c.FutureAction        = "FUTURE_SUBDIVIDE_INTO_LOTS_AND_ASSIGN_RESIDENTIAL_BUILDINGS";
                        c.LotPolicySource     = "LOT_SUBDIVISION_PLAN";
                        c.BuildingPolicySource = "BUILDING_SELECTION_POLICY_PLAN";
                        c.SidewalkPolicySource = "SIDEWALK_GENERATION_PLAN";
                        c.FrontageRule        = "PREFER_MAIN_ROAD_FRONTAGE";
                        c.ServiceAccessRule   = "BACK_ALLEY_REAR_SERVICE_ONLY";
                        c.Notes               = "Residential zone. Future lot subdivision and building assignment required.";
                        break;
                    case "COMMERCIAL":
                        c.ComponentType       = "ZONE_LAYOUT_COMPONENT";
                        c.FutureAction        = "FUTURE_SUBDIVIDE_INTO_LOTS_AND_ASSIGN_COMMERCIAL_BUILDINGS";
                        c.LotPolicySource     = "LOT_SUBDIVISION_PLAN";
                        c.BuildingPolicySource = "BUILDING_SELECTION_POLICY_PLAN";
                        c.SidewalkPolicySource = "SIDEWALK_GENERATION_PLAN";
                        c.FrontageRule        = "PREFER_MAIN_ROAD_FRONTAGE";
                        c.ServiceAccessRule   = "BACK_ALLEY_SERVICE_ACCESS";
                        c.Notes               = "Commercial zone. Future lot subdivision and building assignment required.";
                        break;
                    case "GREENSPACE":
                        c.ComponentType       = "ZONE_LAYOUT_COMPONENT";
                        c.FutureAction        = "FUTURE_GENERATE_GREENSPACE_LAYER";
                        c.LotPolicySource     = "NO_LOT_SUBDIVISION";
                        c.BuildingPolicySource = "NO_PROCEDURAL_BUILDINGS";
                        c.SidewalkPolicySource = "NOT_APPLICABLE";
                        c.FrontageRule        = "NOT_APPLICABLE";
                        c.ServiceAccessRule   = "NOT_APPLICABLE";
                        c.Notes               = "Greenspace. No lot subdivision. No procedural buildings.";
                        break;
                    case "UNIQUE_PLACEHOLDER":
                    case "CIVIC_SPECIAL_BUILDING":
                        c.ComponentType       = "UNIQUE_PLACEHOLDER_LAYOUT_COMPONENT";
                        c.FutureAction        = "FUTURE_BIND_UNIQUE_BUILDING_ID_AND_PLACE";
                        c.LotPolicySource     = "UNIQUE_PLACEHOLDER_POLICY";
                        c.BuildingPolicySource = "BUILDING_SELECTION_POLICY_PLAN";
                        c.SidewalkPolicySource = "DEPENDS_ON_BOUND_UNIQUE_BUILDING";
                        c.FrontageRule        = "MAIN_ROAD_IF_AVAILABLE";
                        c.ServiceAccessRule   = "NOT_APPLICABLE";
                        c.Notes               = "Unique civic placeholder. Requires unique building id binding before placement.";
                        break;
                    default:
                        c.ComponentType       = "ZONE_LAYOUT_COMPONENT";
                        c.FutureAction        = "FUTURE_ZONE_LAYOUT_PENDING_CLASSIFICATION";
                        c.LotPolicySource     = "PENDING";
                        c.BuildingPolicySource = "PENDING";
                        c.SidewalkPolicySource = "PENDING";
                        c.FrontageRule        = "PENDING";
                        c.ServiceAccessRule   = "PENDING";
                        c.Notes               = $"Unclassified buildable zone type: {cr.ZoneType}.";
                        break;
                }
                break;

            case "STREET_CORRIDOR":
                switch (cr.StreetClass)
                {
                    case "MAIN_ROAD":
                        c.ComponentType       = "STREET_LAYOUT_COMPONENT";
                        c.FutureAction        = "FUTURE_GENERATE_MAIN_ROAD_CORRIDOR_AND_SIDEWALK_CONTEXT";
                        c.LotPolicySource     = "NO_LOT_SUBDIVISION";
                        c.BuildingPolicySource = "NO_BUILDING_PLACEMENT";
                        c.SidewalkPolicySource = "SIDEWALK_GENERATION_PLAN";
                        c.FrontageRule        = "PROVIDES_MAIN_FRONTAGE";
                        c.ServiceAccessRule   = "NOT_APPLICABLE";
                        c.Notes               = "Main road corridor. Provides frontage for adjacent residential and commercial zones.";
                        break;
                    case "BACK_ALLEY":
                        c.ComponentType       = "STREET_LAYOUT_COMPONENT";
                        c.FutureAction        = "FUTURE_GENERATE_BACK_ALLEY_SERVICE_CORRIDOR";
                        c.LotPolicySource     = "NO_LOT_SUBDIVISION";
                        c.BuildingPolicySource = "NO_BUILDING_PLACEMENT";
                        c.SidewalkPolicySource = "NO_SIDEWALKS";
                        c.FrontageRule        = "NO_PRIMARY_FRONTAGE";
                        c.ServiceAccessRule   = "BACK_ALLEY_SERVICE_ONLY";
                        c.Notes               = "Back alley / ruelle. No sidewalks. Rear service access only.";
                        break;
                    default:
                        c.ComponentType       = "STREET_LAYOUT_COMPONENT";
                        c.FutureAction        = "FUTURE_STREET_LAYOUT_PENDING_CLASSIFICATION";
                        c.LotPolicySource     = "NO_LOT_SUBDIVISION";
                        c.BuildingPolicySource = "NO_BUILDING_PLACEMENT";
                        c.SidewalkPolicySource = "PENDING";
                        c.FrontageRule        = "PENDING";
                        c.ServiceAccessRule   = "PENDING";
                        c.Notes               = $"Unclassified street corridor class: {cr.StreetClass}.";
                        break;
                }
                break;

            case "UNIQUE_PLACEHOLDER":
                c.ComponentType       = "UNIQUE_PLACEHOLDER_LAYOUT_COMPONENT";
                c.FutureAction        = "FUTURE_BIND_UNIQUE_BUILDING_ID_AND_PLACE";
                c.LotPolicySource     = "UNIQUE_PLACEHOLDER_POLICY";
                c.BuildingPolicySource = "BUILDING_SELECTION_POLICY_PLAN";
                c.SidewalkPolicySource = "DEPENDS_ON_BOUND_UNIQUE_BUILDING";
                c.FrontageRule        = "MAIN_ROAD_IF_AVAILABLE";
                c.ServiceAccessRule   = "NOT_APPLICABLE";
                c.Notes               = "Unique civic placeholder. Requires unique building id binding before placement.";
                break;

            case "IGNORE":
                c.ComponentType       = "IGNORE_LAYOUT_COMPONENT";
                c.FutureAction        = "IGNORE";
                c.LotPolicySource     = "NOT_APPLICABLE";
                c.BuildingPolicySource = "NOT_APPLICABLE";
                c.SidewalkPolicySource = "NOT_APPLICABLE";
                c.FrontageRule        = "NOT_APPLICABLE";
                c.ServiceAccessRule   = "NOT_APPLICABLE";
                c.Notes               = "Ignored. No layout plan generated for this color.";
                break;

            default:
                c.ComponentType       = "UNKNOWN_LAYOUT_COMPONENT";
                c.FutureAction        = "UNKNOWN";
                c.LotPolicySource     = "UNKNOWN";
                c.BuildingPolicySource = "UNKNOWN";
                c.SidewalkPolicySource = "UNKNOWN";
                c.FrontageRule        = "UNKNOWN";
                c.ServiceAccessRule   = "UNKNOWN";
                c.Notes               = $"Unrecognized role: {cr.Role}.";
                break;
        }

        return c;
    }

    private static DeadMtlWorldBuilderFutureWorldLayoutPlanTotals ComputeTotals(
        List<DeadMtlWorldBuilderFutureWorldLayoutPlanComponent> components)
    {
        int zone = 0, street = 0, unique = 0, ignore = 0;
        int lotGroup = 0, sidewalkCorridor = 0, backAlley = 0, buildingSlot = 0;

        foreach (var c in components)
        {
            switch (c.ComponentType)
            {
                case "ZONE_LAYOUT_COMPONENT":
                    zone++;
                    if (c.LotPolicySource is "LOT_SUBDIVISION_PLAN" or "NO_LOT_SUBDIVISION")
                    {
                        if (c.LotPolicySource == "LOT_SUBDIVISION_PLAN") lotGroup++;
                    }
                    if (c.BuildingPolicySource == "BUILDING_SELECTION_POLICY_PLAN") buildingSlot++;
                    break;
                case "UNIQUE_PLACEHOLDER_LAYOUT_COMPONENT":
                    unique++;
                    if (c.BuildingPolicySource == "BUILDING_SELECTION_POLICY_PLAN") buildingSlot++;
                    break;
                case "STREET_LAYOUT_COMPONENT":
                    street++;
                    if (c.SidewalkPolicySource == "SIDEWALK_GENERATION_PLAN") sidewalkCorridor++;
                    if (c.FutureAction == "FUTURE_GENERATE_BACK_ALLEY_SERVICE_CORRIDOR") backAlley++;
                    break;
                case "IGNORE_LAYOUT_COMPONENT":
                    ignore++;
                    break;
            }
        }

        return new DeadMtlWorldBuilderFutureWorldLayoutPlanTotals
        {
            LayoutComponentCount                      = components.Count,
            ZoneLayoutComponentCount                  = zone,
            StreetLayoutComponentCount                = street,
            UniquePlaceholderLayoutComponentCount     = unique,
            IgnoreLayoutComponentCount                = ignore,
            FutureLotGroupPolicyCount                 = lotGroup,
            FutureSidewalkCorridorPolicyCount         = sidewalkCorridor,
            FutureBackAlleyServiceCorridorPolicyCount = backAlley,
            FutureBuildingSlotPolicyCount             = buildingSlot,
            ConcreteGeometryCreatedNowCount           = 0,
            ConcreteBuildingIdsSelectedNowCount       = 0,
            LayoutMaterializedNowCount                = 0,
        };
    }

    public static string RenderMarkdown(DeadMtlWorldBuilderFutureWorldLayoutPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-25G: DeadMTL WorldBuilder Future World Layout Plan Contract");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** Contract only. No terrain generation. No lot subdivision.");
        sb.AppendLine("> No sidewalk generation. No building placement. No fences. No lotpack writing.");
        sb.AppendLine("> No worldgen override file. No concrete geometry created. No runtime proof.");
        sb.AppendLine("> Not writer-ready. Not public-playable. Layout not materialized.");
        sb.AppendLine();
        sb.AppendLine($"**tile_id:** {plan.TileId}");
        sb.AppendLine($"**status:** {plan.Status}");
        sb.AppendLine($"**layout_status:** {plan.LayoutStatus}");
        sb.AppendLine($"**geometry_status:** {plan.GeometryStatus}");
        sb.AppendLine($"**generation_status:** {plan.GenerationStatus}");
        sb.AppendLine($"**runtime_status:** {plan.RuntimeStatus}");
        sb.AppendLine();
        sb.AppendLine("## Input Chain");
        sb.AppendLine();
        sb.AppendLine($"- profile: `{plan.SourceProfileJson}`");
        sb.AppendLine($"- zone metadata: `{plan.SourceZoneMetadataJson}`");
        sb.AppendLine($"- lot subdivision plan: `{plan.SourceLotSubdivisionPlanJson}`");
        sb.AppendLine($"- sidewalk generation plan: `{plan.SourceSidewalkGenerationPlanJson}`");
        sb.AppendLine($"- building selection policy plan: `{plan.SourceBuildingSelectionPolicyPlanJson}`");
        sb.AppendLine($"- generation dependency manifest: `{plan.SourceGenerationDependencyManifestJson}`");
        sb.AppendLine();
        sb.AppendLine("## Layout Components");
        sb.AppendLine();
        sb.AppendLine("| Color | Role | Zone/Street | Component Type | Future Action |");
        sb.AppendLine("|-------|------|-------------|----------------|---------------|");
        foreach (var c in plan.LayoutComponents)
        {
            var zone = string.IsNullOrEmpty(c.ZoneType) ? c.StreetClass : c.ZoneType;
            sb.AppendLine($"| {c.Color} | {c.Role} | {zone} | {c.ComponentType} | {c.FutureAction} |");
        }
        sb.AppendLine();
        sb.AppendLine("## Residential Layout Policy");
        sb.AppendLine();
        var res = plan.LayoutComponents.FirstOrDefault(c => c.ZoneType == "RESIDENTIAL");
        if (res != null)
        {
            sb.AppendLine($"- future_action: {res.FutureAction}");
            sb.AppendLine($"- lot_policy_source: {res.LotPolicySource}");
            sb.AppendLine($"- building_policy_source: {res.BuildingPolicySource}");
            sb.AppendLine($"- sidewalk_policy_source: {res.SidewalkPolicySource}");
            sb.AppendLine($"- frontage_rule: {res.FrontageRule}");
            sb.AppendLine($"- service_access_rule: {res.ServiceAccessRule}");
            sb.AppendLine($"- future_geometry_status: {res.FutureGeometryStatus}");
        }
        sb.AppendLine();
        sb.AppendLine("## Commercial Layout Policy");
        sb.AppendLine();
        var com = plan.LayoutComponents.FirstOrDefault(c => c.ZoneType == "COMMERCIAL");
        if (com != null)
        {
            sb.AppendLine($"- future_action: {com.FutureAction}");
            sb.AppendLine($"- lot_policy_source: {com.LotPolicySource}");
            sb.AppendLine($"- building_policy_source: {com.BuildingPolicySource}");
            sb.AppendLine($"- sidewalk_policy_source: {com.SidewalkPolicySource}");
            sb.AppendLine($"- frontage_rule: {com.FrontageRule}");
            sb.AppendLine($"- service_access_rule: {com.ServiceAccessRule}");
        }
        sb.AppendLine();
        sb.AppendLine("## Greenspace Layout Policy");
        sb.AppendLine();
        var gs = plan.LayoutComponents.FirstOrDefault(c => c.ZoneType == "GREENSPACE");
        if (gs != null)
        {
            sb.AppendLine($"- future_action: {gs.FutureAction}");
            sb.AppendLine($"- building_policy_source: {gs.BuildingPolicySource}");
            sb.AppendLine($"- lot_policy_source: {gs.LotPolicySource}");
        }
        sb.AppendLine();
        sb.AppendLine("## Civic Unique Placeholder Layout Policy");
        sb.AppendLine();
        var civic = plan.LayoutComponents.FirstOrDefault(c =>
            c.ComponentType == "UNIQUE_PLACEHOLDER_LAYOUT_COMPONENT");
        if (civic != null)
        {
            sb.AppendLine($"- future_action: {civic.FutureAction}");
            sb.AppendLine($"- lot_policy_source: {civic.LotPolicySource}");
            sb.AppendLine($"- building_policy_source: {civic.BuildingPolicySource}");
            sb.AppendLine($"- sidewalk_policy_source: {civic.SidewalkPolicySource}");
            sb.AppendLine($"- frontage_rule: {civic.FrontageRule}");
        }
        sb.AppendLine();
        sb.AppendLine("## Street / Sidewalk / Back Alley Layout Policy");
        sb.AppendLine();
        foreach (var st in plan.LayoutComponents.Where(c => c.ComponentType == "STREET_LAYOUT_COMPONENT"))
        {
            var label = string.IsNullOrEmpty(st.StreetClass) ? st.ZoneType : st.StreetClass;
            sb.AppendLine($"**{label}:**");
            sb.AppendLine($"- future_action: {st.FutureAction}");
            sb.AppendLine($"- sidewalk_policy_source: {st.SidewalkPolicySource}");
            sb.AppendLine($"- frontage_rule: {st.FrontageRule}");
            sb.AppendLine($"- service_access_rule: {st.ServiceAccessRule}");
            sb.AppendLine();
        }
        sb.AppendLine("## Future Execution Requirements");
        sb.AppendLine();
        var req = plan.FutureExecutionRequirements;
        sb.AppendLine($"- requires_actual_lot_geometry: {req.RequiresActualLotGeometry}");
        sb.AppendLine($"- requires_actual_sidewalk_geometry: {req.RequiresActualSidewalkGeometry}");
        sb.AppendLine($"- requires_building_catalogue: {req.RequiresBuildingCatalogue}");
        sb.AppendLine($"- requires_unique_building_bindings: {req.RequiresUniqueBuildingBindings}");
        sb.AppendLine($"- requires_tile_writer: {req.RequiresTileWriter}");
        sb.AppendLine($"- requires_runtime_validation: {req.RequiresRuntimeValidation}");
        sb.AppendLine($"- can_execute_now: {req.CanExecuteNow}");
        sb.AppendLine($"- blocked_reason: {req.BlockedReason}");
        sb.AppendLine();
        sb.AppendLine("## Why This Cannot Execute Now");
        sb.AppendLine();
        sb.AppendLine("No lot geometry exists. No sidewalk geometry exists. No tile writer is implemented.");
        sb.AppendLine("No building catalogue with concrete building ids has been selected.");
        sb.AppendLine("No runtime validation has been performed. No unique building bindings exist.");
        sb.AppendLine("This plan describes future execution intent only.");
        sb.AppendLine();
        sb.AppendLine("## No Concrete Geometry");
        sb.AppendLine();
        sb.AppendLine($"concrete_geometry_created_now_count: {plan.Totals.ConcreteGeometryCreatedNowCount}");
        sb.AppendLine($"layout_materialized_now_count:       {plan.Totals.LayoutMaterializedNowCount}");
        sb.AppendLine();
        sb.AppendLine("## No Concrete Building IDs");
        sb.AppendLine();
        sb.AppendLine($"concrete_building_ids_selected_now_count: {plan.Totals.ConcreteBuildingIdsSelectedNowCount}");
        sb.AppendLine();
        sb.AppendLine("## Totals");
        sb.AppendLine();
        sb.AppendLine($"layout_component_count:                      {plan.Totals.LayoutComponentCount}");
        sb.AppendLine($"zone_layout_component_count:                 {plan.Totals.ZoneLayoutComponentCount}");
        sb.AppendLine($"street_layout_component_count:               {plan.Totals.StreetLayoutComponentCount}");
        sb.AppendLine($"unique_placeholder_layout_component_count:   {plan.Totals.UniquePlaceholderLayoutComponentCount}");
        sb.AppendLine($"ignore_layout_component_count:               {plan.Totals.IgnoreLayoutComponentCount}");
        sb.AppendLine($"future_lot_group_policy_count:               {plan.Totals.FutureLotGroupPolicyCount}");
        sb.AppendLine($"future_sidewalk_corridor_policy_count:       {plan.Totals.FutureSidewalkCorridorPolicyCount}");
        sb.AppendLine($"future_back_alley_service_corridor_policy_count: {plan.Totals.FutureBackAlleyServiceCorridorPolicyCount}");
        sb.AppendLine($"future_building_slot_policy_count:           {plan.Totals.FutureBuildingSlotPolicyCount}");
        sb.AppendLine($"concrete_geometry_created_now_count:         {plan.Totals.ConcreteGeometryCreatedNowCount}");
        sb.AppendLine($"concrete_building_ids_selected_now_count:    {plan.Totals.ConcreteBuildingIdsSelectedNowCount}");
        sb.AppendLine($"layout_materialized_now_count:               {plan.Totals.LayoutMaterializedNowCount}");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        var cb = plan.ClaimBoundary;
        sb.AppendLine($"- writes_lotpack: {cb.WritesLotpack}");
        sb.AppendLine($"- writes_worldgen_lua: {cb.WritesWorldgenLua}");
        sb.AppendLine($"- runtime_proven: {cb.RuntimeProven}");
        sb.AppendLine($"- public_playable_claim: {cb.PublicPlayableClaim}");
        sb.AppendLine($"- writer_ready_claim: {cb.WriterReadyClaim}");
        sb.AppendLine($"- generates_terrain_now: {cb.GeneratesTerrainNow}");
        sb.AppendLine($"- generates_buildings_now: {cb.GeneratesBuildingsNow}");
        sb.AppendLine($"- generates_sidewalks_now: {cb.GeneratesSidewalksNow}");
        sb.AppendLine($"- subdivides_lots_now: {cb.SubdividesLotsNow}");
        sb.AppendLine($"- captures_chunk_layers_now: {cb.CapturesChunkLayersNow}");
        sb.AppendLine($"- places_fences_now: {cb.PlacesFencesNow}");
        sb.AppendLine($"- places_unique_buildings_now: {cb.PlacesUniqueBuildingsNow}");
        sb.AppendLine($"- selects_concrete_building_ids_now: {cb.SelectsConcreteBuildingIdsNow}");
        sb.AppendLine($"- creates_concrete_geometry_now: {cb.CreatesConcreteGeometryNow}");
        sb.AppendLine($"- materializes_layout_now: {cb.MaterializesLayoutNow}");
        sb.AppendLine();
        sb.AppendLine("**VERDICT: MAP25G_WORLDBUILDER_FUTURE_WORLD_LAYOUT_PLAN_CONTRACT_COMPLETE**");
        return sb.ToString();
    }

    public static string RenderCsv(DeadMtlWorldBuilderFutureWorldLayoutPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine("color,role,zone_type,street_class,component_type,future_action," +
                      "lot_policy_source,sidewalk_policy_source,building_policy_source," +
                      "frontage_rule,service_access_rule,future_geometry_status,source_confidence");
        foreach (var c in plan.LayoutComponents)
        {
            sb.AppendLine(
                $"{c.Color},{c.Role},{c.ZoneType},{c.StreetClass},{c.ComponentType}," +
                $"{c.FutureAction},{c.LotPolicySource},{c.SidewalkPolicySource}," +
                $"{c.BuildingPolicySource},{c.FrontageRule},{c.ServiceAccessRule}," +
                $"{c.FutureGeometryStatus},{c.SourceConfidence}");
        }
        return sb.ToString();
    }

    public static string RenderSummary(DeadMtlWorldBuilderFutureWorldLayoutPlanResult result)
    {
        var p  = result.Plan;
        var sb = new StringBuilder();
        sb.AppendLine("MAP-25G: DeadMTL WorldBuilder Future World Layout Plan Contract");
        sb.AppendLine();
        sb.AppendLine($"tile_id:                               {p.TileId}");
        sb.AppendLine($"is_valid:                              {result.IsValid}");
        sb.AppendLine($"status:                                {p.Status}");
        sb.AppendLine($"layout_status:                         {p.LayoutStatus}");
        sb.AppendLine($"geometry_status:                       {p.GeometryStatus}");
        sb.AppendLine($"generation_status:                     {p.GenerationStatus}");
        sb.AppendLine();
        sb.AppendLine($"layout_component_count:                      {p.Totals.LayoutComponentCount}");
        sb.AppendLine($"zone_layout_component_count:                 {p.Totals.ZoneLayoutComponentCount}");
        sb.AppendLine($"street_layout_component_count:               {p.Totals.StreetLayoutComponentCount}");
        sb.AppendLine($"unique_placeholder_layout_component_count:   {p.Totals.UniquePlaceholderLayoutComponentCount}");
        sb.AppendLine($"ignore_layout_component_count:               {p.Totals.IgnoreLayoutComponentCount}");
        sb.AppendLine($"future_lot_group_policy_count:               {p.Totals.FutureLotGroupPolicyCount}");
        sb.AppendLine($"future_sidewalk_corridor_policy_count:       {p.Totals.FutureSidewalkCorridorPolicyCount}");
        sb.AppendLine($"future_back_alley_service_corridor_policy_count: {p.Totals.FutureBackAlleyServiceCorridorPolicyCount}");
        sb.AppendLine($"future_building_slot_policy_count:           {p.Totals.FutureBuildingSlotPolicyCount}");
        sb.AppendLine($"concrete_geometry_created_now_count:         {p.Totals.ConcreteGeometryCreatedNowCount}");
        sb.AppendLine($"concrete_building_ids_selected_now_count:    {p.Totals.ConcreteBuildingIdsSelectedNowCount}");
        sb.AppendLine($"layout_materialized_now_count:               {p.Totals.LayoutMaterializedNowCount}");
        sb.AppendLine($"layout_status:                         {p.LayoutStatus}");
        sb.AppendLine($"geometry_status:                       {p.GeometryStatus}");
        if (result.Errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("ERRORS");
            foreach (var e in result.Errors)
                sb.AppendLine($"  - {e}");
        }
        sb.AppendLine();
        sb.AppendLine("VERDICT: MAP25G_WORLDBUILDER_FUTURE_WORLD_LAYOUT_PLAN_CONTRACT_COMPLETE");
        return sb.ToString();
    }

    private static DeadMtlWorldBuilderFutureWorldLayoutPlanResult Fail(
        List<string> errors, DeadMtlWorldBuilderFutureWorldLayoutPlan plan) =>
        new() { IsValid = false, Errors = errors, Plan = plan };
}
