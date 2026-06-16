using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderAdjacencyPlanningCandidateRecord
{
    [JsonPropertyName("candidate_order")]
    public int CandidateOrder { get; set; }

    [JsonPropertyName("candidate_id")]
    public string CandidateId { get; set; } = string.Empty;

    [JsonPropertyName("source_edge_id")]
    public string SourceEdgeId { get; set; } = string.Empty;

    [JsonPropertyName("source_edge_order")]
    public int SourceEdgeOrder { get; set; }

    [JsonPropertyName("source_adjacency_relationship")]
    public string SourceAdjacencyRelationship { get; set; } = string.Empty;

    [JsonPropertyName("candidate_type")]
    public string CandidateType { get; set; } = string.Empty;

    [JsonPropertyName("candidate_priority")]
    public int CandidatePriority { get; set; }

    [JsonPropertyName("candidate_family")]
    public string CandidateFamily { get; set; } = string.Empty;

    [JsonPropertyName("component_a_id")]
    public string ComponentAId { get; set; } = string.Empty;

    [JsonPropertyName("component_a_order")]
    public int ComponentAOrder { get; set; }

    [JsonPropertyName("component_a_source_color")]
    public string ComponentASourceColor { get; set; } = string.Empty;

    [JsonPropertyName("component_a_intent")]
    public string ComponentAIntent { get; set; } = string.Empty;

    [JsonPropertyName("component_a_intent_family")]
    public string ComponentAIntentFamily { get; set; } = string.Empty;

    [JsonPropertyName("component_b_id")]
    public string ComponentBId { get; set; } = string.Empty;

    [JsonPropertyName("component_b_order")]
    public int ComponentBOrder { get; set; }

    [JsonPropertyName("component_b_source_color")]
    public string ComponentBSourceColor { get; set; } = string.Empty;

    [JsonPropertyName("component_b_intent")]
    public string ComponentBIntent { get; set; } = string.Empty;

    [JsonPropertyName("component_b_intent_family")]
    public string ComponentBIntentFamily { get; set; } = string.Empty;

    [JsonPropertyName("contact_length_px")]
    public int ContactLengthPx { get; set; }

    [JsonPropertyName("contact_units")]
    public string ContactUnits { get; set; } = "SOURCE_PIXEL_EDGES";

    [JsonPropertyName("is_actionable")]
    public bool IsActionable { get; set; }

    [JsonPropertyName("future_geometry_requirement_id")]
    public string FutureGeometryRequirementId { get; set; } = string.Empty;

    [JsonPropertyName("blocked_by_requirements")]
    public List<string> BlockedByRequirements { get; set; } = new();

    [JsonPropertyName("extraction_status")]
    public string ExtractionStatus { get; set; } = "ADJACENCY_PLANNING_CANDIDATE_EXTRACTED";

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = "NO_GEOMETRY_CREATED";

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionContract
{
    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("source_adjacency_graph_contract")]
    public string SourceAdjacencyGraphContract { get; set; } = "MAP25M_COMPONENT_ADJACENCY_GRAPH";

    [JsonPropertyName("total_adjacency_edges_input")]
    public int TotalAdjacencyEdgesInput { get; set; }

    [JsonPropertyName("candidate_records_extracted")]
    public int CandidateRecordsExtracted { get; set; }

    [JsonPropertyName("actionable_candidates")]
    public int ActionableCandidates { get; set; }

    [JsonPropertyName("ignored_candidates")]
    public int IgnoredCandidates { get; set; }

    [JsonPropertyName("extraction_produces_geometry")]
    public bool ExtractionProducesGeometry { get; set; }

    [JsonPropertyName("extraction_produces_materialization")]
    public bool ExtractionProducesMaterialization { get; set; }
}

public sealed class DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionValidationRules
{
    [JsonPropertyName("source_adjacency_graph_must_exist")]
    public bool SourceAdjacencyGraphMustExist { get; set; } = true;

    [JsonPropertyName("adjacency_edge_count_must_equal_82")]
    public bool AdjacencyEdgeCountMustEqual82 { get; set; } = true;

    [JsonPropertyName("candidate_count_must_equal_82")]
    public bool CandidateCountMustEqual82 { get; set; } = true;

    [JsonPropertyName("actionable_candidate_count_must_equal_71")]
    public bool ActionableCandidateCountMustEqual71 { get; set; } = true;

    [JsonPropertyName("ignored_candidate_count_must_equal_11")]
    public bool IgnoredCandidateCountMustEqual11 { get; set; } = true;

    [JsonPropertyName("frontage_candidate_count_must_equal_26")]
    public bool FrontageCandidateCountMustEqual26 { get; set; } = true;

    [JsonPropertyName("rear_service_access_candidate_count_must_equal_26")]
    public bool RearServiceAccessCandidateCountMustEqual26 { get; set; } = true;

    [JsonPropertyName("street_network_touchpoint_candidate_count_must_equal_10")]
    public bool StreetNetworkTouchpointCandidateCountMustEqual10 { get; set; } = true;

    [JsonPropertyName("greenspace_access_candidate_count_must_equal_1")]
    public bool GreenspaceAccessCandidateCountMustEqual1 { get; set; } = true;

    [JsonPropertyName("civic_greenspace_context_candidate_count_must_equal_2")]
    public bool CivicGreenspaceContextCandidateCountMustEqual2 { get; set; } = true;

    [JsonPropertyName("mixed_lot_block_boundary_candidate_count_must_equal_6")]
    public bool MixedLotBlockBoundaryCandidateCountMustEqual6 { get; set; } = true;

    [JsonPropertyName("ignored_boundary_adjacency_count_must_equal_11")]
    public bool IgnoredBoundaryAdjacencyCountMustEqual11 { get; set; } = true;

    [JsonPropertyName("no_geometry_created")]
    public bool NoGeometryCreated { get; set; } = true;

    [JsonPropertyName("no_runtime_claim")]
    public bool NoRuntimeClaim { get; set; } = true;

    [JsonPropertyName("no_writer_claim")]
    public bool NoWriterClaim { get; set; } = true;

    [JsonPropertyName("no_materialization_claim")]
    public bool NoMaterializationClaim { get; set; } = true;
}

public sealed class DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionTotals
{
    [JsonPropertyName("total_adjacency_edges_input")]
    public int TotalAdjacencyEdgesInput { get; set; }

    [JsonPropertyName("total_candidate_records")]
    public int TotalCandidateRecords { get; set; }

    [JsonPropertyName("actionable_candidate_count")]
    public int ActionableCandidateCount { get; set; }

    [JsonPropertyName("ignored_candidate_count")]
    public int IgnoredCandidateCount { get; set; }

    [JsonPropertyName("frontage_planning_candidate_count")]
    public int FrontagePlanningCandidateCount { get; set; }

    [JsonPropertyName("rear_service_access_planning_candidate_count")]
    public int RearServiceAccessPlanningCandidateCount { get; set; }

    [JsonPropertyName("street_network_touchpoint_candidate_count")]
    public int StreetNetworkTouchpointCandidateCount { get; set; }

    [JsonPropertyName("greenspace_access_candidate_count")]
    public int GreenspaceAccessCandidateCount { get; set; }

    [JsonPropertyName("civic_greenspace_context_candidate_count")]
    public int CivicGreenspaceContextCandidateCount { get; set; }

    [JsonPropertyName("mixed_lot_block_boundary_candidate_count")]
    public int MixedLotBlockBoundaryCandidateCount { get; set; }

    [JsonPropertyName("ignored_boundary_adjacency_count")]
    public int IgnoredBoundaryAdjacencyCount { get; set; }

    [JsonPropertyName("frontage_contact_total_px")]
    public int FrontageContactTotalPx { get; set; }

    [JsonPropertyName("rear_service_access_contact_total_px")]
    public int RearServiceAccessContactTotalPx { get; set; }

    [JsonPropertyName("street_network_contact_total_px")]
    public int StreetNetworkContactTotalPx { get; set; }

    [JsonPropertyName("greenspace_access_contact_total_px")]
    public int GreenspaceAccessContactTotalPx { get; set; }

    [JsonPropertyName("civic_greenspace_contact_total_px")]
    public int CivicGreenspaceContactTotalPx { get; set; }

    [JsonPropertyName("mixed_lot_block_contact_total_px")]
    public int MixedLotBlockContactTotalPx { get; set; }

    [JsonPropertyName("ignored_contact_total_px")]
    public int IgnoredContactTotalPx { get; set; }

    [JsonPropertyName("created_geometry_count")]
    public int CreatedGeometryCount { get; set; }

    [JsonPropertyName("writer_ready_count")]
    public int WriterReadyCount { get; set; }

    [JsonPropertyName("runtime_validated_count")]
    public int RuntimeValidatedCount { get; set; }

    [JsonPropertyName("materialized_count")]
    public int MaterializedCount { get; set; }
}

public sealed class DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionClaimBoundary
{
    [JsonPropertyName("writes_lotpack")]
    public bool WritesLotpack { get; set; }

    [JsonPropertyName("writes_worldgen_lua")]
    public bool WritesWorldgenLua { get; set; }

    [JsonPropertyName("runtime_proven")]
    public bool RuntimeProven { get; set; }

    [JsonPropertyName("public_playable_claim")]
    public bool PublicPlayableClaim { get; set; }

    [JsonPropertyName("writer_ready_claim")]
    public bool WriterReadyClaim { get; set; }

    [JsonPropertyName("generates_terrain_now")]
    public bool GeneratesTerrainNow { get; set; }

    [JsonPropertyName("generates_buildings_now")]
    public bool GeneratesBuildingsNow { get; set; }

    [JsonPropertyName("generates_sidewalks_now")]
    public bool GeneratesSidewalksNow { get; set; }

    [JsonPropertyName("subdivides_lots_now")]
    public bool SubdividesLotsNow { get; set; }

    [JsonPropertyName("captures_chunk_layers_now")]
    public bool CapturesChunkLayersNow { get; set; }

    [JsonPropertyName("places_fences_now")]
    public bool PlacesFencesNow { get; set; }

    [JsonPropertyName("places_unique_buildings_now")]
    public bool PlacesUniqueBuildingsNow { get; set; }

    [JsonPropertyName("selects_concrete_building_ids_now")]
    public bool SelectsConcreteBuildingIdsNow { get; set; }

    [JsonPropertyName("creates_concrete_geometry_now")]
    public bool CreatesConcreteGeometryNow { get; set; }

    [JsonPropertyName("materializes_layout_now")]
    public bool MaterializesLayoutNow { get; set; }
}

public sealed class DeadMtlWorldBuilderAdjacencyPlanningCandidateExtraction
{
    [JsonPropertyName("format")]
    public string Format { get; set; } =
        "pzmapforge.deadmtl.worldbuilder.adjacency-planning-candidate-extraction.v1";

    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("source_adjacency_graph_json")]
    public string SourceAdjacencyGraphJson { get; set; } = string.Empty;

    [JsonPropertyName("source_connected_component_extraction_json")]
    public string SourceConnectedComponentExtractionJson { get; set; } = string.Empty;

    [JsonPropertyName("source_component_intent_classification_json")]
    public string SourceComponentIntentClassificationJson { get; set; } = string.Empty;

    [JsonPropertyName("source_geometry_primitive_schema_json")]
    public string SourceGeometryPrimitiveSchemaJson { get; set; } = string.Empty;

    [JsonPropertyName("source_concrete_geometry_preflight_json")]
    public string SourceConcreteGeometryPreflightJson { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "ADJACENCY_PLANNING_CANDIDATE_EXTRACTION_CONTRACT_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; set; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; set; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("generation_status")]
    public string GenerationStatus { get; set; } = "NOT_EXECUTED";

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = "CANDIDATE_EXTRACTION_ONLY_NO_GEOMETRY_CREATED";

    [JsonPropertyName("extraction_status")]
    public string ExtractionStatus { get; set; } = "ADJACENCY_PLANNING_CANDIDATES_EXTRACTED";

    [JsonPropertyName("materialization_status")]
    public string MaterializationStatus { get; set; } = "NOT_MATERIALIZED";

    [JsonPropertyName("extraction_contract")]
    public DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionContract ExtractionContract { get; set; } = new();

    [JsonPropertyName("validation_rules")]
    public DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionValidationRules ValidationRules { get; set; } = new();

    [JsonPropertyName("candidates")]
    public List<DeadMtlWorldBuilderAdjacencyPlanningCandidateRecord> Candidates { get; set; } = new();

    [JsonPropertyName("totals")]
    public DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionTotals Totals { get; set; } = new();

    [JsonPropertyName("claim_boundary")]
    public DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionClaimBoundary ClaimBoundary { get; set; } = new();
}

public sealed class DeadMtlWorldBuilderAdjacencyPlanningCandidateExtractionResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public DeadMtlWorldBuilderAdjacencyPlanningCandidateExtraction Extraction { get; set; } = new();
}
