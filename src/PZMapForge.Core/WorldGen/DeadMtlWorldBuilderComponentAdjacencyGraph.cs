using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderAdjacencyEdgeRecord
{
    [JsonPropertyName("edge_order")]
    public int EdgeOrder { get; set; }

    [JsonPropertyName("edge_id")]
    public string EdgeId { get; set; } = string.Empty;

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

    [JsonPropertyName("adjacency_relationship")]
    public string AdjacencyRelationship { get; set; } = string.Empty;

    [JsonPropertyName("intent_pair_key")]
    public string IntentPairKey { get; set; } = string.Empty;

    [JsonPropertyName("is_undirected")]
    public bool IsUndirected { get; set; } = true;

    [JsonPropertyName("connectivity_rule")]
    public string ConnectivityRule { get; set; } = "FOUR_WAY_NEIGHBOR_PIXELS";

    [JsonPropertyName("diagonal_contact")]
    public bool DiagonalContact { get; set; }

    [JsonPropertyName("edge_status")]
    public string EdgeStatus { get; set; } = "PIXEL_TOUCH_ADJACENCY_ONLY";

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = "ADJACENCY_ONLY_NO_GEOMETRY_CREATED";

    [JsonPropertyName("future_planning_use")]
    public string FuturePlanningUse { get; set; } = string.Empty;

    [JsonPropertyName("blocked_by_requirements")]
    public List<string> BlockedByRequirements { get; set; } = new();

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderComponentAdjacencyGraphContract
{
    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("source_png_width_px")]
    public int SourcePngWidthPx { get; set; }

    [JsonPropertyName("source_png_height_px")]
    public int SourcePngHeightPx { get; set; }

    [JsonPropertyName("source_pixel_count")]
    public int SourcePixelCount { get; set; }

    [JsonPropertyName("component_node_count")]
    public int ComponentNodeCount { get; set; }

    [JsonPropertyName("source_component_contract")]
    public string SourceComponentContract { get; set; } = "MAP25K_CONNECTED_COMPONENT_EXTRACTION";

    [JsonPropertyName("source_intent_contract")]
    public string SourceIntentContract { get; set; } = "MAP25L_COMPONENT_INTENT_CLASSIFICATION";

    [JsonPropertyName("connectivity_rule")]
    public string ConnectivityRule { get; set; } = "FOUR_WAY_NEIGHBOR_PIXELS";

    [JsonPropertyName("diagonal_adjacency_enabled")]
    public bool DiagonalAdjacencyEnabled { get; set; }

    [JsonPropertyName("edge_contact_units")]
    public string EdgeContactUnits { get; set; } = "SOURCE_PIXEL_EDGES";

    [JsonPropertyName("adjacency_edges_are_undirected")]
    public bool AdjacencyEdgesAreUndirected { get; set; } = true;

    [JsonPropertyName("adjacency_edges_are_not_geometry")]
    public bool AdjacencyEdgesAreNotGeometry { get; set; } = true;

    [JsonPropertyName("component_bounds_are_pixel_bounds_not_geometry")]
    public bool ComponentBoundsArePixelBoundsNotGeometry { get; set; } = true;

    [JsonPropertyName("geometry_must_be_created_by_future_step")]
    public bool GeometryMustBeCreatedByFutureStep { get; set; } = true;
}

public sealed class DeadMtlWorldBuilderComponentAdjacencyGraphValidationRules
{
    [JsonPropertyName("source_png_must_exist")]
    public bool SourcePngMustExist { get; set; } = true;

    [JsonPropertyName("connected_component_extraction_must_exist")]
    public bool ConnectedComponentExtractionMustExist { get; set; } = true;

    [JsonPropertyName("component_intent_classification_must_exist")]
    public bool ComponentIntentClassificationMustExist { get; set; } = true;

    [JsonPropertyName("component_node_count_must_equal_45")]
    public bool ComponentNodeCountMustEqual45 { get; set; } = true;

    [JsonPropertyName("all_component_ids_must_exist_in_intent_classification")]
    public bool AllComponentIdsMustExistInIntentClassification { get; set; } = true;

    [JsonPropertyName("adjacency_edge_count_must_equal_82")]
    public bool AdjacencyEdgeCountMustEqual82 { get; set; } = true;

    [JsonPropertyName("contact_length_total_must_equal_4535")]
    public bool ContactLengthTotalMustEqual4535 { get; set; } = true;

    [JsonPropertyName("no_self_edges")]
    public bool NoSelfEdges { get; set; } = true;

    [JsonPropertyName("no_duplicate_undirected_edges")]
    public bool NoDuplicateUndirectedEdges { get; set; } = true;

    [JsonPropertyName("no_diagonal_adjacency")]
    public bool NoDiagonalAdjacency { get; set; } = true;

    [JsonPropertyName("all_edges_must_have_positive_contact_length")]
    public bool AllEdgesMustHavePositiveContactLength { get; set; } = true;

    [JsonPropertyName("adjacency_edges_are_not_geometry")]
    public bool AdjacencyEdgesAreNotGeometry { get; set; } = true;

    [JsonPropertyName("no_runtime_claim_from_adjacency_graph")]
    public bool NoRuntimeClaimFromAdjacencyGraph { get; set; } = true;

    [JsonPropertyName("no_writer_claim_from_adjacency_graph")]
    public bool NoWriterClaimFromAdjacencyGraph { get; set; } = true;

    [JsonPropertyName("no_materialization_from_adjacency_graph")]
    public bool NoMaterializationFromAdjacencyGraph { get; set; } = true;
}

public sealed class DeadMtlWorldBuilderComponentAdjacencyGraphTotals
{
    [JsonPropertyName("component_node_count")]
    public int ComponentNodeCount { get; set; }

    [JsonPropertyName("adjacency_edge_count")]
    public int AdjacencyEdgeCount { get; set; }

    [JsonPropertyName("known_component_edge_count")]
    public int KnownComponentEdgeCount { get; set; }

    [JsonPropertyName("unknown_component_edge_count")]
    public int UnknownComponentEdgeCount { get; set; }

    [JsonPropertyName("self_edge_count")]
    public int SelfEdgeCount { get; set; }

    [JsonPropertyName("duplicate_edge_count")]
    public int DuplicateEdgeCount { get; set; }

    [JsonPropertyName("diagonal_edge_count")]
    public int DiagonalEdgeCount { get; set; }

    [JsonPropertyName("contact_length_total_px")]
    public int ContactLengthTotalPx { get; set; }

    [JsonPropertyName("frontage_candidate_edge_count")]
    public int FrontageCandidateEdgeCount { get; set; }

    [JsonPropertyName("rear_or_service_access_candidate_edge_count")]
    public int RearOrServiceAccessCandidateEdgeCount { get; set; }

    [JsonPropertyName("street_network_touchpoint_edge_count")]
    public int StreetNetworkTouchpointEdgeCount { get; set; }

    [JsonPropertyName("greenspace_access_edge_count")]
    public int GreenspaceAccessEdgeCount { get; set; }

    [JsonPropertyName("greenspace_civic_edge_count")]
    public int GreenspaceCivicEdgeCount { get; set; }

    [JsonPropertyName("mixed_lot_block_edge_count")]
    public int MixedLotBlockEdgeCount { get; set; }

    [JsonPropertyName("ignore_boundary_adjacency_edge_count")]
    public int IgnoreBoundaryAdjacencyEdgeCount { get; set; }

    [JsonPropertyName("other_intent_adjacency_edge_count")]
    public int OtherIntentAdjacencyEdgeCount { get; set; }

    [JsonPropertyName("frontage_candidate_contact_total_px")]
    public int FrontageCandidateContactTotalPx { get; set; }

    [JsonPropertyName("rear_or_service_access_contact_total_px")]
    public int RearOrServiceAccessContactTotalPx { get; set; }

    [JsonPropertyName("street_network_touchpoint_contact_total_px")]
    public int StreetNetworkTouchpointContactTotalPx { get; set; }

    [JsonPropertyName("greenspace_access_contact_total_px")]
    public int GreenspaceAccessContactTotalPx { get; set; }

    [JsonPropertyName("greenspace_civic_contact_total_px")]
    public int GreenspaceCivicContactTotalPx { get; set; }

    [JsonPropertyName("mixed_lot_block_contact_total_px")]
    public int MixedLotBlockContactTotalPx { get; set; }

    [JsonPropertyName("ignore_boundary_adjacency_contact_total_px")]
    public int IgnoreBoundaryAdjacencyContactTotalPx { get; set; }

    [JsonPropertyName("other_intent_adjacency_contact_total_px")]
    public int OtherIntentAdjacencyContactTotalPx { get; set; }

    [JsonPropertyName("created_geometry_count")]
    public int CreatedGeometryCount { get; set; }

    [JsonPropertyName("writer_ready_edge_count")]
    public int WriterReadyEdgeCount { get; set; }

    [JsonPropertyName("runtime_validated_edge_count")]
    public int RuntimeValidatedEdgeCount { get; set; }

    [JsonPropertyName("materialized_edge_count")]
    public int MaterializedEdgeCount { get; set; }
}

public sealed class DeadMtlWorldBuilderComponentAdjacencyGraphClaimBoundary
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

public sealed class DeadMtlWorldBuilderComponentAdjacencyGraph
{
    [JsonPropertyName("format")]
    public string Format { get; set; } =
        "pzmapforge.deadmtl.worldbuilder.component-adjacency-graph.v1";

    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("source_png")]
    public string SourcePng { get; set; } = string.Empty;

    [JsonPropertyName("source_connected_component_extraction_json")]
    public string SourceConnectedComponentExtractionJson { get; set; } = string.Empty;

    [JsonPropertyName("source_component_intent_classification_json")]
    public string SourceComponentIntentClassificationJson { get; set; } = string.Empty;

    [JsonPropertyName("source_geometry_primitive_schema_json")]
    public string SourceGeometryPrimitiveSchemaJson { get; set; } = string.Empty;

    [JsonPropertyName("source_concrete_geometry_preflight_json")]
    public string SourceConcreteGeometryPreflightJson { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "COMPONENT_ADJACENCY_GRAPH_CONTRACT_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; set; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; set; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("generation_status")]
    public string GenerationStatus { get; set; } = "NOT_EXECUTED";

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = "ADJACENCY_GRAPH_ONLY_NO_GEOMETRY_CREATED";

    [JsonPropertyName("adjacency_status")]
    public string AdjacencyStatus { get; set; } = "COMPONENT_ADJACENCY_GRAPH_BUILT";

    [JsonPropertyName("materialization_status")]
    public string MaterializationStatus { get; set; } = "NOT_MATERIALIZED";

    [JsonPropertyName("adjacency_graph_contract")]
    public DeadMtlWorldBuilderComponentAdjacencyGraphContract AdjacencyGraphContract { get; set; } = new();

    [JsonPropertyName("validation_rules")]
    public DeadMtlWorldBuilderComponentAdjacencyGraphValidationRules ValidationRules { get; set; } = new();

    [JsonPropertyName("adjacency_edges")]
    public List<DeadMtlWorldBuilderAdjacencyEdgeRecord> AdjacencyEdges { get; set; } = new();

    [JsonPropertyName("totals")]
    public DeadMtlWorldBuilderComponentAdjacencyGraphTotals Totals { get; set; } = new();

    [JsonPropertyName("claim_boundary")]
    public DeadMtlWorldBuilderComponentAdjacencyGraphClaimBoundary ClaimBoundary { get; set; } = new();
}

public sealed class DeadMtlWorldBuilderComponentAdjacencyGraphResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public DeadMtlWorldBuilderComponentAdjacencyGraph Graph { get; set; } = new();
}
