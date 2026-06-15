using System.Text;

namespace PZMapForge.Core.WorldGen;

public static class DeadMtlWorldBuilderConcreteGeometryPreflightBuilder
{
    private static readonly (
        string Id, string Type,
        string[] SourceSteps, string[] Colors,
        string ExpectedOutput, string WriterStatus, string RuntimeStatus, string Notes)[] RequirementDefs =
    {
        (
            "LOT_GEOMETRY",
            "GEOMETRY_REQUIREMENT",
            new[] { "LOT_SUBDIVISION_PLAN", "FUTURE_WORLD_LAYOUT_PLAN" },
            new[] { "#7200FF", "#42CCFF" },
            "Concrete lot polygons/rectangles for residential and commercial zones",
            "NOT_APPLICABLE",
            "NOT_APPLICABLE",
            "Lot geometry must be computed from lot subdivision plan and zone pixel bounds before building slots can be assigned."
        ),
        (
            "MAIN_ROAD_CORRIDOR_GEOMETRY",
            "GEOMETRY_REQUIREMENT",
            new[] { "ZONE_METADATA", "SIDEWALK_GENERATION_PLAN", "FUTURE_WORLD_LAYOUT_PLAN" },
            new[] { "#FF6600" },
            "Concrete main road corridor geometry",
            "NOT_APPLICABLE",
            "NOT_APPLICABLE",
            "Main road corridor pixel bounds must be converted to concrete tile-space geometry."
        ),
        (
            "BACK_ALLEY_SERVICE_CORRIDOR_GEOMETRY",
            "GEOMETRY_REQUIREMENT",
            new[] { "ZONE_METADATA", "FUTURE_WORLD_LAYOUT_PLAN" },
            new[] { "#F000FF" },
            "Concrete back alley / ruelle service corridor geometry",
            "NOT_APPLICABLE",
            "NOT_APPLICABLE",
            "Back alley corridor pixel bounds must be converted to concrete tile-space geometry."
        ),
        (
            "SIDEWALK_GEOMETRY",
            "GEOMETRY_REQUIREMENT",
            new[] { "SIDEWALK_GENERATION_PLAN", "FUTURE_WORLD_LAYOUT_PLAN" },
            new[] { "#FF6600" },
            "Concrete sidewalk strips from INSIDE_STREET_ZONE policy",
            "NOT_APPLICABLE",
            "NOT_APPLICABLE",
            "Sidewalk strips must be computed from sidewalk generation plan widths and main road corridor bounds."
        ),
        (
            "BUILDING_SLOT_GEOMETRY",
            "GEOMETRY_REQUIREMENT",
            new[] { "BUILDING_SELECTION_POLICY_PLAN", "FUTURE_WORLD_LAYOUT_PLAN" },
            new[] { "#7200FF", "#42CCFF", "#B2BD87" },
            "Concrete building slot footprints fit to future lots or unique placeholders",
            "NOT_APPLICABLE",
            "NOT_APPLICABLE",
            "Building slot footprints can only be computed after lot geometry exists."
        ),
        (
            "UNIQUE_BUILDING_BINDING",
            "GEOMETRY_REQUIREMENT",
            new[] { "BUILDING_SELECTION_POLICY_PLAN", "FUTURE_WORLD_LAYOUT_PLAN" },
            new[] { "#B2BD87" },
            "Binding from civic placeholder to concrete unique building id",
            "NOT_APPLICABLE",
            "NOT_APPLICABLE",
            "The #B2BD87 civic placeholder must be bound to a specific unique building id before placement."
        ),
        (
            "FENCE_AND_LOT_BOUNDARY_GEOMETRY",
            "GEOMETRY_REQUIREMENT",
            new[] { "LOT_SUBDIVISION_PLAN", "FUTURE_WORLD_LAYOUT_PLAN" },
            new[] { "#7200FF", "#42CCFF" },
            "Concrete lot boundary/fence line geometry",
            "NOT_APPLICABLE",
            "NOT_APPLICABLE",
            "Lot boundary and fence geometry depends on concrete lot polygons from LOT_GEOMETRY."
        ),
        (
            "TILE_WRITER_IMPLEMENTATION",
            "WRITER_REQUIREMENT",
            new[] { "FUTURE_WORLD_LAYOUT_PLAN" },
            Array.Empty<string>(),
            "Implemented static tile/object writer capable of materializing geometry",
            "NOT_IMPLEMENTED",
            "NOT_APPLICABLE",
            "A tile writer that can translate concrete geometry into PZ tile/object data does not yet exist."
        ),
        (
            "RUNTIME_VALIDATION_PASS",
            "RUNTIME_REQUIREMENT",
            new[] { "FUTURE_WORLD_LAYOUT_PLAN" },
            Array.Empty<string>(),
            "Project Zomboid runtime validation pass with visual/log proof",
            "NOT_APPLICABLE",
            "NOT_RUNTIME_PROVEN",
            "Runtime validation in PZ requires all geometry requirements and tile writer to be satisfied first."
        ),
    };

    public static DeadMtlWorldBuilderConcreteGeometryPreflightResult Build(
        string profilePath,
        string metadataPath,
        string lotPlanPath,
        string sidewalkPlanPath,
        string buildingSelectionPlanPath,
        string dependencyManifestPath,
        string futureLayoutPlanPath)
    {
        var errors  = new List<string>();
        var preflight = new DeadMtlWorldBuilderConcreteGeometryPreflight
        {
            TileId                              = "map_00",
            SourceProfileJson                   = profilePath,
            SourceZoneMetadataJson              = metadataPath,
            SourceLotSubdivisionPlanJson        = lotPlanPath,
            SourceSidewalkGenerationPlanJson    = sidewalkPlanPath,
            SourceBuildingSelectionPolicyPlanJson = buildingSelectionPlanPath,
            SourceGenerationDependencyManifestJson = dependencyManifestPath,
            SourceFutureWorldLayoutPlanJson     = futureLayoutPlanPath,
        };

        var inputs = new[]
        {
            ("profile",                  profilePath),
            ("metadata",                 metadataPath),
            ("lot plan",                 lotPlanPath),
            ("sidewalk plan",            sidewalkPlanPath),
            ("building selection plan",  buildingSelectionPlanPath),
            ("dependency manifest",      dependencyManifestPath),
            ("future layout plan",       futureLayoutPlanPath),
        };

        foreach (var (label, path) in inputs)
        {
            if (!File.Exists(path))
                errors.Add($"Missing {label}: file not found: {path}");
        }

        if (errors.Count > 0)
            return Fail(errors, preflight);

        // Build the 9 preflight requirements
        for (var i = 0; i < RequirementDefs.Length; i++)
        {
            var d = RequirementDefs[i];
            preflight.PreflightRequirements.Add(new DeadMtlWorldBuilderConcreteGeometryPreflightRequirement
            {
                RequirementOrder           = i + 1,
                RequirementId              = d.Id,
                RequirementType            = d.Type,
                SourceSteps                = new List<string>(d.SourceSteps),
                SourceComponentColors      = new List<string>(d.Colors),
                RequiredBeforeMaterialization = true,
                CanExecuteNow              = false,
                BlockedReason              = "CONTRACT_ONLY_NO_CONCRETE_GEOMETRY_NO_WRITER_NO_RUNTIME_PROOF",
                ExpectedFutureOutput       = d.ExpectedOutput,
                GeometryStatus             = "NOT_CREATED",
                WriterStatus               = d.WriterStatus,
                RuntimeStatus              = d.RuntimeStatus,
                Notes                      = d.Notes,
            });
        }

        preflight.Totals = ComputeTotals(preflight.PreflightRequirements);

        preflight.ClaimBoundary = new DeadMtlWorldBuilderConcreteGeometryPreflightClaimBoundary
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

        return new DeadMtlWorldBuilderConcreteGeometryPreflightResult
            { IsValid = true, Errors = errors, Preflight = preflight };
    }

    private static DeadMtlWorldBuilderConcreteGeometryPreflightTotals ComputeTotals(
        List<DeadMtlWorldBuilderConcreteGeometryPreflightRequirement> reqs)
    {
        return new DeadMtlWorldBuilderConcreteGeometryPreflightTotals
        {
            RequirementCount             = reqs.Count,
            BlockedRequirementCount      = reqs.Count(r => !r.CanExecuteNow),
            CanExecuteNowCount           = reqs.Count(r => r.CanExecuteNow),
            GeometryRequirementCount     = reqs.Count(r => r.RequirementType == "GEOMETRY_REQUIREMENT"),
            WriterRequirementCount       = reqs.Count(r => r.RequirementType == "WRITER_REQUIREMENT"),
            RuntimeRequirementCount      = reqs.Count(r => r.RequirementType == "RUNTIME_REQUIREMENT"),
            ConcreteGeometryCreatedNowCount = 0,
            LayoutMaterializedNowCount   = 0,
            WriterReadyRequirementCount  = 0,
            RuntimeValidatedRequirementCount = 0,
        };
    }

    public static string RenderMarkdown(DeadMtlWorldBuilderConcreteGeometryPreflight preflight)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-25H: DeadMTL WorldBuilder Concrete Geometry Preflight Contract");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** Contract only. No terrain generation. No lot subdivision.");
        sb.AppendLine("> No sidewalk geometry. No road geometry. No building placement. No fences.");
        sb.AppendLine("> No lotpack writing. No worldgen override file. No concrete geometry created.");
        sb.AppendLine("> No runtime proof. Not writer-ready. Layout not materialized.");
        sb.AppendLine();
        sb.AppendLine($"**tile_id:** {preflight.TileId}");
        sb.AppendLine($"**status:** {preflight.Status}");
        sb.AppendLine($"**preflight_status:** {preflight.PreflightStatus}");
        sb.AppendLine($"**geometry_status:** {preflight.GeometryStatus}");
        sb.AppendLine($"**generation_status:** {preflight.GenerationStatus}");
        sb.AppendLine($"**runtime_status:** {preflight.RuntimeStatus}");
        sb.AppendLine();
        sb.AppendLine("## Input Chain");
        sb.AppendLine();
        sb.AppendLine($"- profile: `{preflight.SourceProfileJson}`");
        sb.AppendLine($"- zone metadata: `{preflight.SourceZoneMetadataJson}`");
        sb.AppendLine($"- lot subdivision plan: `{preflight.SourceLotSubdivisionPlanJson}`");
        sb.AppendLine($"- sidewalk generation plan: `{preflight.SourceSidewalkGenerationPlanJson}`");
        sb.AppendLine($"- building selection policy plan: `{preflight.SourceBuildingSelectionPolicyPlanJson}`");
        sb.AppendLine($"- generation dependency manifest: `{preflight.SourceGenerationDependencyManifestJson}`");
        sb.AppendLine($"- future world layout plan: `{preflight.SourceFutureWorldLayoutPlanJson}`");
        sb.AppendLine();
        sb.AppendLine("## Preflight Requirements");
        sb.AppendLine();
        sb.AppendLine("| Order | Requirement ID | Type | Can Execute Now | Geometry Status |");
        sb.AppendLine("|-------|---------------|------|-----------------|-----------------|");
        foreach (var r in preflight.PreflightRequirements)
            sb.AppendLine($"| {r.RequirementOrder} | {r.RequirementId} | {r.RequirementType} | {r.CanExecuteNow} | {r.GeometryStatus} |");
        sb.AppendLine();
        sb.AppendLine("## Geometry Blockers");
        sb.AppendLine();
        foreach (var r in preflight.PreflightRequirements.Where(r => r.RequirementType == "GEOMETRY_REQUIREMENT"))
        {
            sb.AppendLine($"**{r.RequirementId}:**");
            sb.AppendLine($"- source_steps: {string.Join(", ", r.SourceSteps)}");
            sb.AppendLine($"- source_component_colors: {string.Join(", ", r.SourceComponentColors)}");
            sb.AppendLine($"- expected_future_output: {r.ExpectedFutureOutput}");
            sb.AppendLine($"- geometry_status: {r.GeometryStatus}");
            sb.AppendLine($"- notes: {r.Notes}");
            sb.AppendLine();
        }
        sb.AppendLine("## Writer Blocker");
        sb.AppendLine();
        var wr = preflight.PreflightRequirements.FirstOrDefault(r => r.RequirementType == "WRITER_REQUIREMENT");
        if (wr != null)
        {
            sb.AppendLine($"**{wr.RequirementId}:**");
            sb.AppendLine($"- writer_status: {wr.WriterStatus}");
            sb.AppendLine($"- expected_future_output: {wr.ExpectedFutureOutput}");
            sb.AppendLine($"- notes: {wr.Notes}");
        }
        sb.AppendLine();
        sb.AppendLine("## Runtime Validation Blocker");
        sb.AppendLine();
        var rv = preflight.PreflightRequirements.FirstOrDefault(r => r.RequirementType == "RUNTIME_REQUIREMENT");
        if (rv != null)
        {
            sb.AppendLine($"**{rv.RequirementId}:**");
            sb.AppendLine($"- runtime_status: {rv.RuntimeStatus}");
            sb.AppendLine($"- expected_future_output: {rv.ExpectedFutureOutput}");
            sb.AppendLine($"- notes: {rv.Notes}");
        }
        sb.AppendLine();
        sb.AppendLine("## Why This Still Cannot Execute");
        sb.AppendLine();
        sb.AppendLine($"All {preflight.Totals.RequirementCount} preflight requirements are blocked.");
        sb.AppendLine($"- {preflight.Totals.GeometryRequirementCount} geometry requirements: no concrete geometry exists.");
        sb.AppendLine($"- {preflight.Totals.WriterRequirementCount} writer requirement: tile writer not implemented.");
        sb.AppendLine($"- {preflight.Totals.RuntimeRequirementCount} runtime requirement: no runtime validation pass.");
        sb.AppendLine($"can_execute_now_count: {preflight.Totals.CanExecuteNowCount}");
        sb.AppendLine($"concrete_geometry_created_now_count: {preflight.Totals.ConcreteGeometryCreatedNowCount}");
        sb.AppendLine($"layout_materialized_now_count: {preflight.Totals.LayoutMaterializedNowCount}");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        var cb = preflight.ClaimBoundary;
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
        sb.AppendLine("## Totals");
        sb.AppendLine();
        sb.AppendLine($"requirement_count:                    {preflight.Totals.RequirementCount}");
        sb.AppendLine($"blocked_requirement_count:            {preflight.Totals.BlockedRequirementCount}");
        sb.AppendLine($"can_execute_now_count:                {preflight.Totals.CanExecuteNowCount}");
        sb.AppendLine($"geometry_requirement_count:           {preflight.Totals.GeometryRequirementCount}");
        sb.AppendLine($"writer_requirement_count:             {preflight.Totals.WriterRequirementCount}");
        sb.AppendLine($"runtime_requirement_count:            {preflight.Totals.RuntimeRequirementCount}");
        sb.AppendLine($"concrete_geometry_created_now_count:  {preflight.Totals.ConcreteGeometryCreatedNowCount}");
        sb.AppendLine($"layout_materialized_now_count:        {preflight.Totals.LayoutMaterializedNowCount}");
        sb.AppendLine($"writer_ready_requirement_count:       {preflight.Totals.WriterReadyRequirementCount}");
        sb.AppendLine($"runtime_validated_requirement_count:  {preflight.Totals.RuntimeValidatedRequirementCount}");
        sb.AppendLine();
        sb.AppendLine("**VERDICT: MAP25H_WORLDBUILDER_CONCRETE_GEOMETRY_PREFLIGHT_CONTRACT_COMPLETE**");
        return sb.ToString();
    }

    public static string RenderCsv(DeadMtlWorldBuilderConcreteGeometryPreflight preflight)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "requirement_order,requirement_id,requirement_type,source_steps,source_component_colors," +
            "required_before_materialization,can_execute_now,blocked_reason,expected_future_output," +
            "geometry_status,writer_status,runtime_status,notes");
        foreach (var r in preflight.PreflightRequirements)
        {
            sb.AppendLine(
                $"{r.RequirementOrder},{r.RequirementId},{r.RequirementType}," +
                $"\"{string.Join("|", r.SourceSteps)}\",\"{string.Join("|", r.SourceComponentColors)}\"," +
                $"{r.RequiredBeforeMaterialization},{r.CanExecuteNow},{r.BlockedReason}," +
                $"\"{r.ExpectedFutureOutput}\",{r.GeometryStatus},{r.WriterStatus},{r.RuntimeStatus}," +
                $"\"{r.Notes}\"");
        }
        return sb.ToString();
    }

    public static string RenderSummary(DeadMtlWorldBuilderConcreteGeometryPreflightResult result)
    {
        var p  = result.Preflight;
        var sb = new StringBuilder();
        sb.AppendLine("MAP-25H: DeadMTL WorldBuilder Concrete Geometry Preflight Contract");
        sb.AppendLine();
        sb.AppendLine($"tile_id:                               {p.TileId}");
        sb.AppendLine($"is_valid:                              {result.IsValid}");
        sb.AppendLine($"status:                                {p.Status}");
        sb.AppendLine($"preflight_status:                      {p.PreflightStatus}");
        sb.AppendLine($"geometry_status:                       {p.GeometryStatus}");
        sb.AppendLine($"generation_status:                     {p.GenerationStatus}");
        sb.AppendLine();
        sb.AppendLine($"requirement_count:                    {p.Totals.RequirementCount}");
        sb.AppendLine($"blocked_requirement_count:            {p.Totals.BlockedRequirementCount}");
        sb.AppendLine($"can_execute_now_count:                {p.Totals.CanExecuteNowCount}");
        sb.AppendLine($"geometry_requirement_count:           {p.Totals.GeometryRequirementCount}");
        sb.AppendLine($"writer_requirement_count:             {p.Totals.WriterRequirementCount}");
        sb.AppendLine($"runtime_requirement_count:            {p.Totals.RuntimeRequirementCount}");
        sb.AppendLine($"concrete_geometry_created_now_count:  {p.Totals.ConcreteGeometryCreatedNowCount}");
        sb.AppendLine($"layout_materialized_now_count:        {p.Totals.LayoutMaterializedNowCount}");
        sb.AppendLine($"writer_ready_requirement_count:       {p.Totals.WriterReadyRequirementCount}");
        sb.AppendLine($"runtime_validated_requirement_count:  {p.Totals.RuntimeValidatedRequirementCount}");
        sb.AppendLine($"preflight_status:                     {p.PreflightStatus}");
        if (result.Errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("ERRORS");
            foreach (var e in result.Errors)
                sb.AppendLine($"  - {e}");
        }
        sb.AppendLine();
        sb.AppendLine("VERDICT: MAP25H_WORLDBUILDER_CONCRETE_GEOMETRY_PREFLIGHT_CONTRACT_COMPLETE");
        return sb.ToString();
    }

    private static DeadMtlWorldBuilderConcreteGeometryPreflightResult Fail(
        List<string> errors, DeadMtlWorldBuilderConcreteGeometryPreflight preflight) =>
        new() { IsValid = false, Errors = errors, Preflight = preflight };
}
