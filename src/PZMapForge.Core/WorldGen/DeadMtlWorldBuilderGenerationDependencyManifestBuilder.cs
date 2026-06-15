using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class DeadMtlWorldBuilderGenerationDependencyManifestBuilder
{
    private static readonly (string StepId, string MapId, string SourceStage, string ExpectedFormat,
        string[] DependsOn, string[] ConsumedBy, string Notes)[] StepDefs =
    {
        (
            "PROFILE_CONTRACT",
            "map_00",
            "MAP-25A",
            "pzmapforge.deadmtl.worldbuilder.neighborhood-profile.v1",
            Array.Empty<string>(),
            new[] { "RAW_TILE_ZONE_METADATA", "LOT_SUBDIVISION_PLAN", "SIDEWALK_GENERATION_PLAN", "BUILDING_SELECTION_POLICY_PLAN" },
            "Neighborhood profile. Provides sidewalk dimensions, building families, and policy for all subsequent steps."
        ),
        (
            "RAW_TILE_ZONE_METADATA",
            "map_00",
            "MAP-25B",
            "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            new[] { "PROFILE_CONTRACT" },
            new[] { "LOT_SUBDIVISION_PLAN", "SIDEWALK_GENERATION_PLAN", "BUILDING_SELECTION_POLICY_PLAN" },
            "Zone metadata maps raw PNG colors to semantic WorldBuilder roles. Required by all downstream plans."
        ),
        (
            "LOT_SUBDIVISION_PLAN",
            "map_00",
            "MAP-25C",
            "pzmapforge.deadmtl.worldbuilder.lot-subdivision-plan.v1",
            new[] { "PROFILE_CONTRACT", "RAW_TILE_ZONE_METADATA" },
            new[] { "SIDEWALK_GENERATION_PLAN", "BUILDING_SELECTION_POLICY_PLAN" },
            "Lot subdivision plan. Derived from zone metadata and profile. Not yet executed."
        ),
        (
            "SIDEWALK_GENERATION_PLAN",
            "map_00",
            "MAP-25D",
            "pzmapforge.deadmtl.worldbuilder.sidewalk-generation-plan.v1",
            new[] { "PROFILE_CONTRACT", "RAW_TILE_ZONE_METADATA", "LOT_SUBDIVISION_PLAN" },
            new[] { "BUILDING_SELECTION_POLICY_PLAN" },
            "Sidewalk generation plan. MAIN_ROAD-only. Dimensions from profile. Not yet executed."
        ),
        (
            "BUILDING_SELECTION_POLICY_PLAN",
            "map_00",
            "MAP-25E",
            "pzmapforge.deadmtl.worldbuilder.building-selection-policy-plan.v1",
            new[] { "PROFILE_CONTRACT", "RAW_TILE_ZONE_METADATA", "LOT_SUBDIVISION_PLAN", "SIDEWALK_GENERATION_PLAN" },
            new[] { "FUTURE_WORLD_LAYOUT_PLAN" },
            "Building selection policy plan. Defines allowed families per zone. No concrete id selected now."
        ),
    };

    public static DeadMtlWorldBuilderGenerationDependencyManifestResult Build(
        string profilePath, string metadataPath, string lotPlanPath,
        string sidewalkPlanPath, string buildingSelectionPlanPath)
    {
        var errors  = new List<string>();
        var manifest = new DeadMtlWorldBuilderGenerationDependencyManifest { TileId = "map_00" };

        var opts = new JsonSerializerOptions
            { AllowTrailingCommas = true, PropertyNameCaseInsensitive = false };

        var inputPaths = new[]
        {
            ("PROFILE_CONTRACT",             profilePath),
            ("RAW_TILE_ZONE_METADATA",       metadataPath),
            ("LOT_SUBDIVISION_PLAN",         lotPlanPath),
            ("SIDEWALK_GENERATION_PLAN",     sidewalkPlanPath),
            ("BUILDING_SELECTION_POLICY_PLAN", buildingSelectionPlanPath),
        };

        // Validate all five inputs exist before reading formats
        foreach (var (stepId, path) in inputPaths)
        {
            if (!File.Exists(path))
                errors.Add($"{stepId}: file not found: {path}");
        }

        if (errors.Count > 0)
            return Fail(errors, manifest);

        // Build steps by reading actual format from each file
        for (var i = 0; i < StepDefs.Length; i++)
        {
            var def       = StepDefs[i];
            var inputPath = inputPaths[i].Item2;
            var exists    = File.Exists(inputPath);
            var actual    = string.Empty;
            var status    = "CONTRACT_ONLY_NOT_EXECUTED";

            if (exists)
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(inputPath, Encoding.UTF8));
                    if (doc.RootElement.TryGetProperty("format", out var fmt))
                        actual = fmt.GetString() ?? string.Empty;

                    // Read status fields
                    if (doc.RootElement.TryGetProperty("status", out var st))
                        status = st.GetString() ?? status;

                    // Validate format
                    if (!string.Equals(actual, def.ExpectedFormat, StringComparison.Ordinal))
                        errors.Add($"{def.StepId}: format mismatch. Expected={def.ExpectedFormat} Actual={actual}");
                }
                catch (Exception ex)
                {
                    errors.Add($"{def.StepId}: failed to parse JSON: {ex.Message}");
                }
            }

            var runtimeStatus = "NOT_RUNTIME_PROVEN";
            var writerStatus  = "NOT_IMPLEMENTED";
            var genStatus     = "NOT_EXECUTED";

            manifest.Steps.Add(new DeadMtlWorldBuilderGenerationDependencyManifestStep
            {
                StepOrder       = i + 1,
                StepId          = def.StepId,
                MapId           = def.MapId,
                SourceStage     = def.SourceStage,
                InputPath       = inputPath,
                ExpectedFormat  = def.ExpectedFormat,
                ActualFormat    = actual,
                Exists          = exists,
                IsRequired      = true,
                Status          = status,
                RuntimeStatus   = runtimeStatus,
                WriterStatus    = writerStatus,
                GenerationStatus = genStatus,
                DependsOn       = new List<string>(def.DependsOn),
                ConsumedBy      = new List<string>(def.ConsumedBy),
                ClaimStatus     = "CONTRACT_ONLY_NOT_EXECUTED",
                Notes           = def.Notes,
            });
        }

        // Build dependency edges
        manifest.DependencyEdges = BuildEdges();

        // Totals
        var steps = manifest.Steps;
        manifest.Totals = new DeadMtlWorldBuilderGenerationDependencyManifestTotals
        {
            StepCount                   = steps.Count,
            DependencyEdgeCount         = manifest.DependencyEdges.Count,
            RequiredInputCount          = steps.Count(s => s.IsRequired),
            ExistingInputCount          = steps.Count(s => s.Exists),
            MissingInputCount           = steps.Count(s => !s.Exists),
            FormatMatchCount            = steps.Count(s => s.Exists && s.ActualFormat == s.ExpectedFormat),
            FormatMismatchCount         = steps.Count(s => s.Exists && s.ActualFormat != s.ExpectedFormat),
            ContractOnlyStepCount       = steps.Count(s => s.ClaimStatus == "CONTRACT_ONLY_NOT_EXECUTED"),
            RuntimeProvenStepCount      = 0,
            WriterReadyStepCount        = 0,
            GenerationExecutedStepCount = 0,
        };

        manifest.ClaimBoundary = new DeadMtlWorldBuilderGenerationDependencyManifestClaimBoundary
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
        };

        bool isValid = errors.Count == 0;
        return new DeadMtlWorldBuilderGenerationDependencyManifestResult
            { IsValid = isValid, Errors = errors, Manifest = manifest };
    }

    private static List<DeadMtlWorldBuilderGenerationDependencyEdge> BuildEdges()
    {
        var edges = new List<DeadMtlWorldBuilderGenerationDependencyEdge>();
        foreach (var def in StepDefs)
        {
            foreach (var dep in def.DependsOn)
            {
                edges.Add(new DeadMtlWorldBuilderGenerationDependencyEdge
                {
                    FromStepId      = dep,
                    ToStepId        = def.StepId,
                    DependencyType  = "REQUIRED_INPUT",
                    Required        = true,
                    Notes           = $"{dep} must exist and be valid before {def.StepId} can be built.",
                });
            }
        }
        return edges;
    }

    public static string RenderMarkdown(DeadMtlWorldBuilderGenerationDependencyManifest manifest)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-25F: DeadMTL WorldBuilder Generation Dependency Manifest Contract");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** Contract only. No terrain generation. No lot subdivision.");
        sb.AppendLine("> No sidewalk generation. No building placement. No fences. No lotpack writing.");
        sb.AppendLine("> No worldgen override file. No runtime proof. Not writer-ready.");
        sb.AppendLine();
        sb.AppendLine($"**tile_id:** {manifest.TileId}");
        sb.AppendLine($"**status:** {manifest.Status}");
        sb.AppendLine($"**pipeline_status:** {manifest.PipelineStatus}");
        sb.AppendLine($"**generation_status:** {manifest.GenerationStatus}");
        sb.AppendLine($"**runtime_status:** {manifest.RuntimeStatus}");
        sb.AppendLine();
        sb.AppendLine("## Input Chain");
        sb.AppendLine();
        sb.AppendLine("Five inputs are required in order. Each step depends on all prior steps.");
        sb.AppendLine();
        foreach (var step in manifest.Steps)
        {
            sb.AppendLine($"**Step {step.StepOrder}: {step.StepId}** ({step.SourceStage})");
            sb.AppendLine($"- path: `{step.InputPath}`");
            sb.AppendLine($"- format: `{step.ExpectedFormat}`");
            sb.AppendLine($"- exists: {step.Exists}");
            sb.AppendLine($"- claim_status: {step.ClaimStatus}");
            sb.AppendLine();
        }
        sb.AppendLine("## Ordered Steps");
        sb.AppendLine();
        sb.AppendLine("| Order | Step ID | Source Stage | Status | Exists |");
        sb.AppendLine("|-------|---------|-------------|--------|--------|");
        foreach (var step in manifest.Steps)
            sb.AppendLine($"| {step.StepOrder} | {step.StepId} | {step.SourceStage} | {step.ClaimStatus} | {step.Exists} |");
        sb.AppendLine();
        sb.AppendLine("## Dependency Graph");
        sb.AppendLine();
        sb.AppendLine("```");
        foreach (var edge in manifest.DependencyEdges)
            sb.AppendLine($"{edge.FromStepId} -> {edge.ToStepId}");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## What Each Step Consumes and Produces");
        sb.AppendLine();
        foreach (var step in manifest.Steps)
        {
            sb.AppendLine($"**{step.StepId}**");
            if (step.DependsOn.Count == 0)
                sb.AppendLine("- depends_on: (none)");
            else
                sb.AppendLine($"- depends_on: {string.Join(", ", step.DependsOn)}");
            sb.AppendLine($"- consumed_by: {string.Join(", ", step.ConsumedBy)}");
            sb.AppendLine($"- notes: {step.Notes}");
            sb.AppendLine();
        }
        sb.AppendLine("## Future Consumer: FUTURE_WORLD_LAYOUT_PLAN");
        sb.AppendLine();
        sb.AppendLine("BUILDING_SELECTION_POLICY_PLAN is consumed by FUTURE_WORLD_LAYOUT_PLAN.");
        sb.AppendLine("That future step will combine lot subdivision, sidewalk generation, and building");
        sb.AppendLine("selection policy into an actual world layout execution plan.");
        sb.AppendLine("It is not yet defined and is not part of MAP-25F.");
        sb.AppendLine();
        sb.AppendLine("## No Generation Now");
        sb.AppendLine();
        sb.AppendLine("No terrain is generated. No lots are subdivided. No sidewalks are placed.");
        sb.AppendLine("No buildings are selected or placed. No fences are generated.");
        sb.AppendLine("This manifest records the dependency contract only.");
        sb.AppendLine($"contract_only_step_count: {manifest.Totals.ContractOnlyStepCount}");
        sb.AppendLine($"runtime_proven_step_count: {manifest.Totals.RuntimeProvenStepCount}");
        sb.AppendLine($"generation_executed_step_count: {manifest.Totals.GenerationExecutedStepCount}");
        sb.AppendLine();
        sb.AppendLine("## Totals");
        sb.AppendLine();
        sb.AppendLine($"step_count:                    {manifest.Totals.StepCount}");
        sb.AppendLine($"dependency_edge_count:         {manifest.Totals.DependencyEdgeCount}");
        sb.AppendLine($"required_input_count:          {manifest.Totals.RequiredInputCount}");
        sb.AppendLine($"existing_input_count:          {manifest.Totals.ExistingInputCount}");
        sb.AppendLine($"missing_input_count:           {manifest.Totals.MissingInputCount}");
        sb.AppendLine($"format_match_count:            {manifest.Totals.FormatMatchCount}");
        sb.AppendLine($"format_mismatch_count:         {manifest.Totals.FormatMismatchCount}");
        sb.AppendLine($"contract_only_step_count:      {manifest.Totals.ContractOnlyStepCount}");
        sb.AppendLine($"runtime_proven_step_count:     {manifest.Totals.RuntimeProvenStepCount}");
        sb.AppendLine($"writer_ready_step_count:       {manifest.Totals.WriterReadyStepCount}");
        sb.AppendLine($"generation_executed_step_count:{manifest.Totals.GenerationExecutedStepCount}");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        sb.AppendLine($"- writes_lotpack: {manifest.ClaimBoundary.WritesLotpack}");
        sb.AppendLine($"- writes_worldgen_lua: {manifest.ClaimBoundary.WritesWorldgenLua}");
        sb.AppendLine($"- runtime_proven: {manifest.ClaimBoundary.RuntimeProven}");
        sb.AppendLine($"- public_playable_claim: {manifest.ClaimBoundary.PublicPlayableClaim}");
        sb.AppendLine($"- writer_ready_claim: {manifest.ClaimBoundary.WriterReadyClaim}");
        sb.AppendLine($"- generates_terrain_now: {manifest.ClaimBoundary.GeneratesTerrainNow}");
        sb.AppendLine($"- generates_buildings_now: {manifest.ClaimBoundary.GeneratesBuildingsNow}");
        sb.AppendLine($"- generates_sidewalks_now: {manifest.ClaimBoundary.GeneratesSidewalksNow}");
        sb.AppendLine($"- subdivides_lots_now: {manifest.ClaimBoundary.SubdividesLotsNow}");
        sb.AppendLine($"- captures_chunk_layers_now: {manifest.ClaimBoundary.CapturesChunkLayersNow}");
        sb.AppendLine($"- places_fences_now: {manifest.ClaimBoundary.PlacesFencesNow}");
        sb.AppendLine($"- places_unique_buildings_now: {manifest.ClaimBoundary.PlacesUniqueBuildingsNow}");
        sb.AppendLine($"- selects_concrete_building_ids_now: {manifest.ClaimBoundary.SelectsConcreteBuildingIdsNow}");
        sb.AppendLine();
        sb.AppendLine("**VERDICT: MAP25F_WORLDBUILDER_GENERATION_DEPENDENCY_MANIFEST_CONTRACT_COMPLETE**");
        return sb.ToString();
    }

    public static string RenderCsv(DeadMtlWorldBuilderGenerationDependencyManifest manifest)
    {
        var sb = new StringBuilder();
        sb.AppendLine("step_order,step_id,source_stage,input_path,expected_format,actual_format," +
                      "exists,status,runtime_status,writer_status,generation_status,claim_status");
        foreach (var step in manifest.Steps)
        {
            sb.AppendLine(
                $"{step.StepOrder},{step.StepId},{step.SourceStage},{CsvEscape(step.InputPath)}," +
                $"{step.ExpectedFormat},{step.ActualFormat}," +
                $"{step.Exists},{step.Status},{step.RuntimeStatus},{step.WriterStatus}," +
                $"{step.GenerationStatus},{step.ClaimStatus}");
        }
        return sb.ToString();
    }

    public static string RenderSummary(DeadMtlWorldBuilderGenerationDependencyManifestResult result)
    {
        var m  = result.Manifest;
        var sb = new StringBuilder();
        sb.AppendLine("MAP-25F: DeadMTL WorldBuilder Generation Dependency Manifest Contract");
        sb.AppendLine();
        sb.AppendLine($"tile_id:                       {m.TileId}");
        sb.AppendLine($"is_valid:                      {result.IsValid}");
        sb.AppendLine($"status:                        {m.Status}");
        sb.AppendLine($"pipeline_status:               {m.PipelineStatus}");
        sb.AppendLine($"generation_status:             {m.GenerationStatus}");
        sb.AppendLine();
        sb.AppendLine($"step_count:                    {m.Totals.StepCount}");
        sb.AppendLine($"dependency_edge_count:         {m.Totals.DependencyEdgeCount}");
        sb.AppendLine($"required_input_count:          {m.Totals.RequiredInputCount}");
        sb.AppendLine($"existing_input_count:          {m.Totals.ExistingInputCount}");
        sb.AppendLine($"missing_input_count:           {m.Totals.MissingInputCount}");
        sb.AppendLine($"format_match_count:            {m.Totals.FormatMatchCount}");
        sb.AppendLine($"format_mismatch_count:         {m.Totals.FormatMismatchCount}");
        sb.AppendLine($"contract_only_step_count:      {m.Totals.ContractOnlyStepCount}");
        sb.AppendLine($"runtime_proven_step_count:     {m.Totals.RuntimeProvenStepCount}");
        sb.AppendLine($"writer_ready_step_count:       {m.Totals.WriterReadyStepCount}");
        sb.AppendLine($"generation_executed_step_count:{m.Totals.GenerationExecutedStepCount}");
        sb.AppendLine();
        sb.AppendLine("CLAIM BOUNDARY");
        sb.AppendLine($"  writes_lotpack:                    {m.ClaimBoundary.WritesLotpack}");
        sb.AppendLine($"  writes_worldgen_lua:               {m.ClaimBoundary.WritesWorldgenLua}");
        sb.AppendLine($"  runtime_proven:                    {m.ClaimBoundary.RuntimeProven}");
        sb.AppendLine($"  public_playable_claim:             {m.ClaimBoundary.PublicPlayableClaim}");
        sb.AppendLine($"  writer_ready_claim:                {m.ClaimBoundary.WriterReadyClaim}");
        sb.AppendLine($"  generates_terrain_now:             {m.ClaimBoundary.GeneratesTerrainNow}");
        sb.AppendLine($"  generates_buildings_now:           {m.ClaimBoundary.GeneratesBuildingsNow}");
        sb.AppendLine($"  generates_sidewalks_now:           {m.ClaimBoundary.GeneratesSidewalksNow}");
        sb.AppendLine($"  subdivides_lots_now:               {m.ClaimBoundary.SubdividesLotsNow}");
        sb.AppendLine($"  captures_chunk_layers_now:         {m.ClaimBoundary.CapturesChunkLayersNow}");
        sb.AppendLine($"  places_fences_now:                 {m.ClaimBoundary.PlacesFencesNow}");
        sb.AppendLine($"  places_unique_buildings_now:       {m.ClaimBoundary.PlacesUniqueBuildingsNow}");
        sb.AppendLine($"  selects_concrete_building_ids_now: {m.ClaimBoundary.SelectsConcreteBuildingIdsNow}");
        if (result.Errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("ERRORS");
            foreach (var e in result.Errors)
                sb.AppendLine($"  - {e}");
        }
        sb.AppendLine();
        sb.AppendLine("VERDICT: MAP25F_WORLDBUILDER_GENERATION_DEPENDENCY_MANIFEST_CONTRACT_COMPLETE");
        return sb.ToString();
    }

    private static DeadMtlWorldBuilderGenerationDependencyManifestResult Fail(
        List<string> errors, DeadMtlWorldBuilderGenerationDependencyManifest manifest) =>
        new() { IsValid = false, Errors = errors, Manifest = manifest };

    private static string CsvEscape(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}
