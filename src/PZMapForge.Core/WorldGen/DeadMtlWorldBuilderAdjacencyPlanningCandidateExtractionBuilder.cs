using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionBuilder
{
    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };

    public static DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionResult Build(
        string adjacencyGraphPath,
        string connectedComponentsPath,
        string componentIntentsPath,
        string geometryPrimitiveSchemaPath,
        string concreteGeometryPreflightPath)
    {
        var errors = new List<string>();
        var extraction = new DeadMtlWorldBuilderAdjacencyPlanningCandidateExtraction
        {
            TileId                                  = "map_00",
            SourceAdjacencyGraphJson                = adjacencyGraphPath,
            SourceConnectedComponentExtractionJson  = connectedComponentsPath,
            SourceComponentIntentClassificationJson = componentIntentsPath,
            SourceGeometryPrimitiveSchemaJson       = geometryPrimitiveSchemaPath,
            SourceConcreteGeometryPreflightJson     = concreteGeometryPreflightPath,
        };

        var inputs = new[]
        {
            ("adjacency graph",                      adjacencyGraphPath),
            ("connected component extraction",       connectedComponentsPath),
            ("component intent classification",      componentIntentsPath),
            ("geometry primitive schema",            geometryPrimitiveSchemaPath),
            ("concrete geometry preflight",          concreteGeometryPreflightPath),
        };

        foreach (var (label, path) in inputs)
        {
            if (!File.Exists(path))
                errors.Add($"Missing {label}: file not found: {path}");
        }

        if (errors.Count > 0)
            return Fail(errors, extraction);

        var edges = LoadAdjacencyEdges(adjacencyGraphPath, errors);
        if (errors.Count > 0)
            return Fail(errors, extraction);

        extraction.ExtractionContract = new DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionContract
        {
            TileId                         = "map_00",
            SourceAdjacencyGraphContract   = "MAP25M_COMPONENT_ADJACENCY_GRAPH",
            TotalAdjacencyEdgesInput       = edges.Count,
            CandidateRecordsExtracted      = edges.Count,
            ActionableCandidates           = edges.Count(e => e.AdjacencyRelationship != "IGNORE_BOUNDARY_ADJACENCY"),
            IgnoredCandidates              = edges.Count(e => e.AdjacencyRelationship == "IGNORE_BOUNDARY_ADJACENCY"),
            ExtractionProducesGeometry     = false,
            ExtractionProducesMaterialization = false,
        };

        extraction.ValidationRules = new DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionValidationRules();

        int order = 1;
        foreach (var edge in edges)
        {
            var candidateType = MapCandidateType(edge.AdjacencyRelationship);
            extraction.Candidates.Add(new DeadMtlWorldBuilderAdjacencyPlanningCandidateRecord
            {
                CandidateOrder              = order,
                CandidateId                 = $"map_00_candidate_{order:D4}",
                SourceEdgeId                = edge.EdgeId,
                SourceEdgeOrder             = edge.EdgeOrder,
                SourceAdjacencyRelationship = edge.AdjacencyRelationship,
                CandidateType               = candidateType,
                CandidatePriority           = MapPriority(candidateType),
                CandidateFamily             = MapFamily(candidateType),
                ComponentAId                = edge.ComponentAId,
                ComponentAOrder             = edge.ComponentAOrder,
                ComponentASourceColor       = edge.ComponentASourceColor,
                ComponentAIntent            = edge.ComponentAIntent,
                ComponentAIntentFamily      = edge.ComponentAIntentFamily,
                ComponentBId                = edge.ComponentBId,
                ComponentBOrder             = edge.ComponentBOrder,
                ComponentBSourceColor       = edge.ComponentBSourceColor,
                ComponentBIntent            = edge.ComponentBIntent,
                ComponentBIntentFamily      = edge.ComponentBIntentFamily,
                ContactLengthPx             = edge.ContactLengthPx,
                ContactUnits                = "SOURCE_PIXEL_EDGES",
                IsActionable                = candidateType != "IGNORED_BOUNDARY_ADJACENCY",
                FutureGeometryRequirementId = MapFutureGeometryRequirementId(candidateType),
                BlockedByRequirements       = MapBlockedBy(candidateType),
                ExtractionStatus            = "ADJACENCY_PLANNING_CANDIDATE_EXTRACTED",
                GeometryStatus              = "NO_GEOMETRY_CREATED",
                Notes                       = MapNotes(candidateType),
            });
            order++;
        }

        extraction.Totals        = ComputeTotals(extraction.Candidates);
        extraction.ClaimBoundary = new DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionClaimBoundary();

        return new DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionResult
            { IsValid = true, Errors = errors, Extraction = extraction };
    }

    private static string MapCandidateType(string adjacencyRelationship) => adjacencyRelationship switch
    {
        "FRONTAGE_CANDIDATE"               => "FRONTAGE_PLANNING_CANDIDATE",
        "REAR_OR_SERVICE_ACCESS_CANDIDATE" => "REAR_SERVICE_ACCESS_PLANNING_CANDIDATE",
        "STREET_NETWORK_TOUCHPOINT"        => "STREET_NETWORK_TOUCHPOINT_CANDIDATE",
        "GREENSPACE_ACCESS_EDGE"           => "GREENSPACE_ACCESS_CANDIDATE",
        "GREENSPACE_CIVIC_EDGE"            => "CIVIC_GREENSPACE_CONTEXT_CANDIDATE",
        "MIXED_LOT_BLOCK_EDGE"             => "MIXED_LOT_BLOCK_BOUNDARY_CANDIDATE",
        "IGNORE_BOUNDARY_ADJACENCY"        => "IGNORED_BOUNDARY_ADJACENCY",
        _                                  => "MANUAL_REVIEW_CANDIDATE",
    };

    private static int MapPriority(string candidateType) => candidateType switch
    {
        "FRONTAGE_PLANNING_CANDIDATE"          => 10,
        "REAR_SERVICE_ACCESS_PLANNING_CANDIDATE" => 20,
        "STREET_NETWORK_TOUCHPOINT_CANDIDATE"  => 30,
        "GREENSPACE_ACCESS_CANDIDATE"          => 40,
        "CIVIC_GREENSPACE_CONTEXT_CANDIDATE"   => 50,
        "MIXED_LOT_BLOCK_BOUNDARY_CANDIDATE"   => 60,
        "MANUAL_REVIEW_CANDIDATE"              => 90,
        "IGNORED_BOUNDARY_ADJACENCY"           => 999,
        _                                      => 999,
    };

    private static string MapFamily(string candidateType) => candidateType switch
    {
        "FRONTAGE_PLANNING_CANDIDATE"          => "FRONTAGE",
        "REAR_SERVICE_ACCESS_PLANNING_CANDIDATE" => "REAR_SERVICE_ACCESS",
        "STREET_NETWORK_TOUCHPOINT_CANDIDATE"  => "STREET_NETWORK",
        "GREENSPACE_ACCESS_CANDIDATE"          => "GREENSPACE_ACCESS",
        "CIVIC_GREENSPACE_CONTEXT_CANDIDATE"   => "CIVIC_GREENSPACE",
        "MIXED_LOT_BLOCK_BOUNDARY_CANDIDATE"   => "MIXED_LOT_BLOCK",
        "IGNORED_BOUNDARY_ADJACENCY"           => "IGNORED",
        _                                      => "MANUAL_REVIEW",
    };

    private static string MapFutureGeometryRequirementId(string candidateType) => candidateType switch
    {
        "FRONTAGE_PLANNING_CANDIDATE"          => "MAP25N_REQ_FRONTAGE_GEOMETRY_GENERATOR",
        "REAR_SERVICE_ACCESS_PLANNING_CANDIDATE" => "MAP25N_REQ_REAR_SERVICE_GEOMETRY_GENERATOR",
        "STREET_NETWORK_TOUCHPOINT_CANDIDATE"  => "MAP25N_REQ_STREET_NETWORK_GEOMETRY_GENERATOR",
        "GREENSPACE_ACCESS_CANDIDATE"          => "MAP25N_REQ_GREENSPACE_ACCESS_GEOMETRY_GENERATOR",
        "CIVIC_GREENSPACE_CONTEXT_CANDIDATE"   => "MAP25N_REQ_CIVIC_GREENSPACE_GEOMETRY_GENERATOR",
        "MIXED_LOT_BLOCK_BOUNDARY_CANDIDATE"   => "MAP25N_REQ_LOT_BLOCK_BOUNDARY_GEOMETRY_GENERATOR",
        "IGNORED_BOUNDARY_ADJACENCY"           => "MAP25N_REQ_NONE",
        _                                      => "MAP25N_REQ_MANUAL_REVIEW",
    };

    private static List<string> MapBlockedBy(string candidateType) =>
        candidateType == "IGNORED_BOUNDARY_ADJACENCY"
            ? new List<string> { "NONE" }
            : new List<string>
            {
                "CONCRETE_GEOMETRY_GENERATOR_NOT_IMPLEMENTED",
                "STATIC_TILE_WRITER_NOT_IMPLEMENTED",
                "RUNTIME_VALIDATION_NOT_RUN",
            };

    private static string MapNotes(string candidateType) => candidateType switch
    {
        "FRONTAGE_PLANNING_CANDIDATE"          => "Frontage boundary between lot block and main road corridor. Awaiting frontage geometry generator.",
        "REAR_SERVICE_ACCESS_PLANNING_CANDIDATE" => "Rear or service access boundary between lot block and back alley corridor. Awaiting service geometry generator.",
        "STREET_NETWORK_TOUCHPOINT_CANDIDATE"  => "Touchpoint between main road corridor and back alley corridor. Awaiting street network geometry generator.",
        "GREENSPACE_ACCESS_CANDIDATE"          => "Access boundary between road corridor and greenspace mass. Awaiting greenspace access geometry generator.",
        "CIVIC_GREENSPACE_CONTEXT_CANDIDATE"   => "Context boundary between greenspace mass and civic placeholder. Awaiting civic greenspace geometry generator.",
        "MIXED_LOT_BLOCK_BOUNDARY_CANDIDATE"   => "Shared boundary between residential and commercial lot blocks. Awaiting lot block boundary geometry generator.",
        "IGNORED_BOUNDARY_ADJACENCY"           => "Adjacency involves border or void component. No geometry planned.",
        _                                      => "Unclassified adjacency candidate. Requires manual review.",
    };

    private sealed record EdgeData(
        int EdgeOrder,
        string EdgeId,
        string ComponentAId,
        int ComponentAOrder,
        string ComponentASourceColor,
        string ComponentAIntent,
        string ComponentAIntentFamily,
        string ComponentBId,
        int ComponentBOrder,
        string ComponentBSourceColor,
        string ComponentBIntent,
        string ComponentBIntentFamily,
        int ContactLengthPx,
        string AdjacencyRelationship);

    private static List<EdgeData> LoadAdjacencyEdges(string path, List<string> errors)
    {
        var result = new List<EdgeData>();
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path), DocOpts);
            if (!doc.RootElement.TryGetProperty("adjacency_edges", out var arr))
            {
                errors.Add("Adjacency graph JSON missing 'adjacency_edges' array.");
                return result;
            }
            foreach (var item in arr.EnumerateArray())
            {
                result.Add(new EdgeData(
                    EdgeOrder:            item.GetProperty("edge_order").GetInt32(),
                    EdgeId:               item.GetProperty("edge_id").GetString()                   ?? "",
                    ComponentAId:         item.GetProperty("component_a_id").GetString()            ?? "",
                    ComponentAOrder:      item.GetProperty("component_a_order").GetInt32(),
                    ComponentASourceColor: item.GetProperty("component_a_source_color").GetString() ?? "",
                    ComponentAIntent:     item.GetProperty("component_a_intent").GetString()        ?? "",
                    ComponentAIntentFamily: item.GetProperty("component_a_intent_family").GetString() ?? "",
                    ComponentBId:         item.GetProperty("component_b_id").GetString()            ?? "",
                    ComponentBOrder:      item.GetProperty("component_b_order").GetInt32(),
                    ComponentBSourceColor: item.GetProperty("component_b_source_color").GetString() ?? "",
                    ComponentBIntent:     item.GetProperty("component_b_intent").GetString()        ?? "",
                    ComponentBIntentFamily: item.GetProperty("component_b_intent_family").GetString() ?? "",
                    ContactLengthPx:      item.GetProperty("contact_length_px").GetInt32(),
                    AdjacencyRelationship: item.GetProperty("adjacency_relationship").GetString()   ?? ""));
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse adjacency graph: {ex.Message}");
        }
        return result;
    }

    private static DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionTotals ComputeTotals(
        List<DeadMtlWorldBuilderAdjacencyPlanningCandidateRecord> candidates)
    {
        return new DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionTotals
        {
            TotalAdjacencyEdgesInput          = candidates.Count,
            TotalCandidateRecords             = candidates.Count,
            ActionableCandidateCount          = candidates.Count(c => c.IsActionable),
            IgnoredCandidateCount             = candidates.Count(c => !c.IsActionable),
            FrontagePlanningCandidateCount    = candidates.Count(c => c.CandidateType == "FRONTAGE_PLANNING_CANDIDATE"),
            RearServiceAccessPlanningCandidateCount = candidates.Count(c => c.CandidateType == "REAR_SERVICE_ACCESS_PLANNING_CANDIDATE"),
            StreetNetworkTouchpointCandidateCount = candidates.Count(c => c.CandidateType == "STREET_NETWORK_TOUCHPOINT_CANDIDATE"),
            GreenspaceAccessCandidateCount    = candidates.Count(c => c.CandidateType == "GREENSPACE_ACCESS_CANDIDATE"),
            CivicGreenspaceContextCandidateCount = candidates.Count(c => c.CandidateType == "CIVIC_GREENSPACE_CONTEXT_CANDIDATE"),
            MixedLotBlockBoundaryCandidateCount = candidates.Count(c => c.CandidateType == "MIXED_LOT_BLOCK_BOUNDARY_CANDIDATE"),
            IgnoredBoundaryAdjacencyCount     = candidates.Count(c => c.CandidateType == "IGNORED_BOUNDARY_ADJACENCY"),
            FrontageContactTotalPx            = candidates.Where(c => c.CandidateType == "FRONTAGE_PLANNING_CANDIDATE").Sum(c => c.ContactLengthPx),
            RearServiceAccessContactTotalPx   = candidates.Where(c => c.CandidateType == "REAR_SERVICE_ACCESS_PLANNING_CANDIDATE").Sum(c => c.ContactLengthPx),
            StreetNetworkContactTotalPx       = candidates.Where(c => c.CandidateType == "STREET_NETWORK_TOUCHPOINT_CANDIDATE").Sum(c => c.ContactLengthPx),
            GreenspaceAccessContactTotalPx    = candidates.Where(c => c.CandidateType == "GREENSPACE_ACCESS_CANDIDATE").Sum(c => c.ContactLengthPx),
            CivicGreenspaceContactTotalPx     = candidates.Where(c => c.CandidateType == "CIVIC_GREENSPACE_CONTEXT_CANDIDATE").Sum(c => c.ContactLengthPx),
            MixedLotBlockContactTotalPx       = candidates.Where(c => c.CandidateType == "MIXED_LOT_BLOCK_BOUNDARY_CANDIDATE").Sum(c => c.ContactLengthPx),
            IgnoredContactTotalPx             = candidates.Where(c => c.CandidateType == "IGNORED_BOUNDARY_ADJACENCY").Sum(c => c.ContactLengthPx),
            CreatedGeometryCount              = 0,
            WriterReadyCount                  = 0,
            RuntimeValidatedCount             = 0,
            MaterializedCount                 = 0,
        };
    }

    public static string RenderMarkdown(DeadMtlWorldBuilderAdjacencyPlanningCandidateExtraction extraction)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-25N: DeadMTL WorldBuilder Adjacency Planning Candidate Extraction Contract");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** Planning candidate extraction only. No terrain generation. No lot subdivision.");
        sb.AppendLine("> No sidewalk geometry. No road geometry. No frontage geometry. No rear access geometry.");
        sb.AppendLine("> No building slots. No fences. No concrete building IDs. No lotpack. No WorldGenOverride.lua.");
        sb.AppendLine("> No concrete geometry created. No runtime proof. Not writer-ready. Layout not materialized.");
        sb.AppendLine();
        sb.AppendLine($"**tile_id:** {extraction.TileId}");
        sb.AppendLine($"**status:** {extraction.Status}");
        sb.AppendLine($"**extraction_status:** {extraction.ExtractionStatus}");
        sb.AppendLine($"**geometry_status:** {extraction.GeometryStatus}");
        sb.AppendLine($"**generation_status:** {extraction.GenerationStatus}");
        sb.AppendLine($"**materialization_status:** {extraction.MaterializationStatus}");
        sb.AppendLine($"**runtime_status:** {extraction.RuntimeStatus}");
        sb.AppendLine();
        sb.AppendLine("## Input Chain");
        sb.AppendLine();
        sb.AppendLine($"- adjacency graph: `{extraction.SourceAdjacencyGraphJson}`");
        sb.AppendLine($"- connected component extraction: `{extraction.SourceConnectedComponentExtractionJson}`");
        sb.AppendLine($"- component intent classification: `{extraction.SourceComponentIntentClassificationJson}`");
        sb.AppendLine($"- geometry primitive schema: `{extraction.SourceGeometryPrimitiveSchemaJson}`");
        sb.AppendLine($"- concrete geometry preflight: `{extraction.SourceConcreteGeometryPreflightJson}`");
        sb.AppendLine();
        sb.AppendLine("## Extraction Contract");
        sb.AppendLine();
        var ec = extraction.ExtractionContract;
        sb.AppendLine($"- tile_id: {ec.TileId}");
        sb.AppendLine($"- source_adjacency_graph_contract: {ec.SourceAdjacencyGraphContract}");
        sb.AppendLine($"- total_adjacency_edges_input: {ec.TotalAdjacencyEdgesInput}");
        sb.AppendLine($"- candidate_records_extracted: {ec.CandidateRecordsExtracted}");
        sb.AppendLine($"- actionable_candidates: {ec.ActionableCandidates}");
        sb.AppendLine($"- ignored_candidates: {ec.IgnoredCandidates}");
        sb.AppendLine($"- extraction_produces_geometry: {ec.ExtractionProducesGeometry}");
        sb.AppendLine($"- extraction_produces_materialization: {ec.ExtractionProducesMaterialization}");
        sb.AppendLine();
        sb.AppendLine("## Candidate Type Mapping");
        sb.AppendLine();
        sb.AppendLine("| Source Adjacency Relationship | Candidate Type | Priority | Family |");
        sb.AppendLine("|-------------------------------|----------------|----------|--------|");
        sb.AppendLine("| FRONTAGE_CANDIDATE | FRONTAGE_PLANNING_CANDIDATE | 10 | FRONTAGE |");
        sb.AppendLine("| REAR_OR_SERVICE_ACCESS_CANDIDATE | REAR_SERVICE_ACCESS_PLANNING_CANDIDATE | 20 | REAR_SERVICE_ACCESS |");
        sb.AppendLine("| STREET_NETWORK_TOUCHPOINT | STREET_NETWORK_TOUCHPOINT_CANDIDATE | 30 | STREET_NETWORK |");
        sb.AppendLine("| GREENSPACE_ACCESS_EDGE | GREENSPACE_ACCESS_CANDIDATE | 40 | GREENSPACE_ACCESS |");
        sb.AppendLine("| GREENSPACE_CIVIC_EDGE | CIVIC_GREENSPACE_CONTEXT_CANDIDATE | 50 | CIVIC_GREENSPACE |");
        sb.AppendLine("| MIXED_LOT_BLOCK_EDGE | MIXED_LOT_BLOCK_BOUNDARY_CANDIDATE | 60 | MIXED_LOT_BLOCK |");
        sb.AppendLine("| IGNORE_BOUNDARY_ADJACENCY | IGNORED_BOUNDARY_ADJACENCY | 999 | IGNORED |");
        sb.AppendLine();
        sb.AppendLine("## Planning Candidate Records");
        sb.AppendLine();
        sb.AppendLine("| Order | Candidate ID | Source Edge | Candidate Type | Priority | Contact px | Actionable |");
        sb.AppendLine("|-------|-------------|-------------|----------------|----------|------------|------------|");
        foreach (var c in extraction.Candidates)
        {
            sb.AppendLine(
                $"| {c.CandidateOrder} | {c.CandidateId} | {c.SourceEdgeId} | {c.CandidateType} " +
                $"| {c.CandidatePriority} | {c.ContactLengthPx} | {c.IsActionable} |");
        }
        sb.AppendLine();
        var t = extraction.Totals;
        sb.AppendLine("## Extraction Totals");
        sb.AppendLine();
        sb.AppendLine($"- total_adjacency_edges_input: {t.TotalAdjacencyEdgesInput}");
        sb.AppendLine($"- total_candidate_records: {t.TotalCandidateRecords}");
        sb.AppendLine($"- actionable_candidate_count: {t.ActionableCandidateCount}");
        sb.AppendLine($"- ignored_candidate_count: {t.IgnoredCandidateCount}");
        sb.AppendLine($"- frontage_planning_candidate_count: {t.FrontagePlanningCandidateCount}");
        sb.AppendLine($"- rear_service_access_planning_candidate_count: {t.RearServiceAccessPlanningCandidateCount}");
        sb.AppendLine($"- street_network_touchpoint_candidate_count: {t.StreetNetworkTouchpointCandidateCount}");
        sb.AppendLine($"- greenspace_access_candidate_count: {t.GreenspaceAccessCandidateCount}");
        sb.AppendLine($"- civic_greenspace_context_candidate_count: {t.CivicGreenspaceContextCandidateCount}");
        sb.AppendLine($"- mixed_lot_block_boundary_candidate_count: {t.MixedLotBlockBoundaryCandidateCount}");
        sb.AppendLine($"- ignored_boundary_adjacency_count: {t.IgnoredBoundaryAdjacencyCount}");
        sb.AppendLine();
        sb.AppendLine("## Contact Totals by Candidate Type");
        sb.AppendLine();
        sb.AppendLine($"- frontage_contact_total_px: {t.FrontageContactTotalPx}");
        sb.AppendLine($"- rear_service_access_contact_total_px: {t.RearServiceAccessContactTotalPx}");
        sb.AppendLine($"- street_network_contact_total_px: {t.StreetNetworkContactTotalPx}");
        sb.AppendLine($"- greenspace_access_contact_total_px: {t.GreenspaceAccessContactTotalPx}");
        sb.AppendLine($"- civic_greenspace_contact_total_px: {t.CivicGreenspaceContactTotalPx}");
        sb.AppendLine($"- mixed_lot_block_contact_total_px: {t.MixedLotBlockContactTotalPx}");
        sb.AppendLine($"- ignored_contact_total_px: {t.IgnoredContactTotalPx}");
        sb.AppendLine();
        sb.AppendLine("## Why This Still Cannot Execute");
        sb.AppendLine();
        sb.AppendLine("Planning candidates are extracted records derived from adjacency edges.");
        sb.AppendLine("They are NOT concrete lot polygons, road geometries, frontage geometry,");
        sb.AppendLine("rear access geometry, building slots, or any materialized artifact.");
        sb.AppendLine($"- created_geometry_count: {t.CreatedGeometryCount}");
        sb.AppendLine($"- writer_ready_count: {t.WriterReadyCount}");
        sb.AppendLine($"- runtime_validated_count: {t.RuntimeValidatedCount}");
        sb.AppendLine($"- materialized_count: {t.MaterializedCount}");
        sb.AppendLine("No concrete geometry generator exists. No tile writer exists.");
        sb.AppendLine("No runtime validation pass has been performed.");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        var cb = extraction.ClaimBoundary;
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
        sb.AppendLine("## Verdict");
        sb.AppendLine();
        sb.AppendLine("**VERDICT: MAP25N_WORLDBUILDER_ADJACENCY_PLANNING_CANDIDATE_EXTRACTION_CONTRACT_COMPLETE**");
        return sb.ToString();
    }

    public static string RenderCsv(DeadMtlWorldBuilderAdjacencyPlanningCandidateExtraction extraction)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "candidate_order,candidate_id,source_edge_id,source_edge_order,source_adjacency_relationship," +
            "candidate_type,candidate_priority,candidate_family," +
            "component_a_id,component_a_order,component_a_source_color," +
            "component_a_intent,component_a_intent_family," +
            "component_b_id,component_b_order,component_b_source_color," +
            "component_b_intent,component_b_intent_family," +
            "contact_length_px,contact_units,is_actionable," +
            "future_geometry_requirement_id,blocked_by_requirements," +
            "extraction_status,geometry_status,notes");
        foreach (var c in extraction.Candidates)
        {
            sb.AppendLine(
                $"{c.CandidateOrder},{c.CandidateId},{c.SourceEdgeId},{c.SourceEdgeOrder},{c.SourceAdjacencyRelationship}," +
                $"{c.CandidateType},{c.CandidatePriority},{c.CandidateFamily}," +
                $"{c.ComponentAId},{c.ComponentAOrder},{c.ComponentASourceColor}," +
                $"{c.ComponentAIntent},{c.ComponentAIntentFamily}," +
                $"{c.ComponentBId},{c.ComponentBOrder},{c.ComponentBSourceColor}," +
                $"{c.ComponentBIntent},{c.ComponentBIntentFamily}," +
                $"{c.ContactLengthPx},{c.ContactUnits},{c.IsActionable}," +
                $"{c.FutureGeometryRequirementId}," +
                $"\"{string.Join("|", c.BlockedByRequirements)}\"," +
                $"{c.ExtractionStatus},{c.GeometryStatus}," +
                $"\"{c.Notes}\"");
        }
        return sb.ToString();
    }

    public static string RenderSummary(DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionResult result)
    {
        var e  = result.Extraction;
        var t  = e.Totals;
        var sb = new StringBuilder();
        sb.AppendLine("MAP-25N: DeadMTL WorldBuilder Adjacency Planning Candidate Extraction Contract");
        sb.AppendLine();
        sb.AppendLine($"tile_id:                                         {e.TileId}");
        sb.AppendLine($"is_valid:                                        {result.IsValid}");
        sb.AppendLine($"total_adjacency_edges_input:                     {t.TotalAdjacencyEdgesInput}");
        sb.AppendLine($"total_candidate_records:                         {t.TotalCandidateRecords}");
        sb.AppendLine($"actionable_candidate_count:                      {t.ActionableCandidateCount}");
        sb.AppendLine($"ignored_candidate_count:                         {t.IgnoredCandidateCount}");
        sb.AppendLine($"frontage_planning_candidate_count:               {t.FrontagePlanningCandidateCount}");
        sb.AppendLine($"rear_service_access_planning_candidate_count:    {t.RearServiceAccessPlanningCandidateCount}");
        sb.AppendLine($"street_network_touchpoint_candidate_count:       {t.StreetNetworkTouchpointCandidateCount}");
        sb.AppendLine($"greenspace_access_candidate_count:               {t.GreenspaceAccessCandidateCount}");
        sb.AppendLine($"civic_greenspace_context_candidate_count:        {t.CivicGreenspaceContextCandidateCount}");
        sb.AppendLine($"mixed_lot_block_boundary_candidate_count:        {t.MixedLotBlockBoundaryCandidateCount}");
        sb.AppendLine($"ignored_boundary_adjacency_count:                {t.IgnoredBoundaryAdjacencyCount}");
        sb.AppendLine($"frontage_contact_total_px:                       {t.FrontageContactTotalPx}");
        sb.AppendLine($"rear_service_access_contact_total_px:            {t.RearServiceAccessContactTotalPx}");
        sb.AppendLine($"street_network_contact_total_px:                 {t.StreetNetworkContactTotalPx}");
        sb.AppendLine($"greenspace_access_contact_total_px:              {t.GreenspaceAccessContactTotalPx}");
        sb.AppendLine($"civic_greenspace_contact_total_px:               {t.CivicGreenspaceContactTotalPx}");
        sb.AppendLine($"mixed_lot_block_contact_total_px:                {t.MixedLotBlockContactTotalPx}");
        sb.AppendLine($"ignored_contact_total_px:                        {t.IgnoredContactTotalPx}");
        sb.AppendLine($"created_geometry_count:                          {t.CreatedGeometryCount}");
        sb.AppendLine($"writer_ready_count:                              {t.WriterReadyCount}");
        sb.AppendLine($"runtime_validated_count:                         {t.RuntimeValidatedCount}");
        sb.AppendLine($"materialized_count:                              {t.MaterializedCount}");
        sb.AppendLine($"extraction_status:                               {e.ExtractionStatus}");
        sb.AppendLine($"geometry_status:                                 {e.GeometryStatus}");
        if (result.Errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("ERRORS");
            foreach (var err in result.Errors)
                sb.AppendLine($"  - {err}");
        }
        sb.AppendLine();
        sb.AppendLine("VERDICT: MAP25N_WORLDBUILDER_ADJACENCY_PLANNING_CANDIDATE_EXTRACTION_CONTRACT_COMPLETE");
        return sb.ToString();
    }

    private static DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionResult Fail(
        List<string> errors, DeadMtlWorldBuilderAdjacencyPlanningCandidateExtraction extraction) =>
        new() { IsValid = false, Errors = errors, Extraction = extraction };
}
