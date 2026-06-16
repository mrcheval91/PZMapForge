using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderComponentAccessProfileResult
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("generated_utc")]
    public string GeneratedUtc { get; set; } = string.Empty;

    [JsonPropertyName("source_planning_candidates_path")]
    public string SourcePlanningCandidatesPath { get; set; } = string.Empty;

    [JsonPropertyName("source_component_intents_path")]
    public string SourceComponentIntentsPath { get; set; } = string.Empty;

    [JsonPropertyName("profile_contract")]
    public DeadMtlWorldBuilderComponentAccessProfileContract ProfileContract { get; set; } = new();

    [JsonPropertyName("profiles")]
    public List<DeadMtlWorldBuilderComponentAccessProfileRecord> Profiles { get; set; } = new();

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();

    [JsonPropertyName("is_valid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("verdict")]
    public string Verdict { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderComponentAccessProfileContract
{
    [JsonPropertyName("total_components_input")]
    public int TotalComponentsInput { get; set; }

    [JsonPropertyName("profile_records_extracted")]
    public int ProfileRecordsExtracted { get; set; }

    [JsonPropertyName("dual_access_candidate_count")]
    public int DualAccessCandidateCount { get; set; }

    [JsonPropertyName("frontage_only_candidate_count")]
    public int FrontageOnlyCandidateCount { get; set; }

    [JsonPropertyName("rear_service_only_candidate_count")]
    public int RearServiceOnlyCandidateCount { get; set; }

    [JsonPropertyName("landlocked_candidate_count")]
    public int LandlockedCandidateCount { get; set; }

    [JsonPropertyName("main_road_corridor_node_count")]
    public int MainRoadCorridorNodeCount { get; set; }

    [JsonPropertyName("back_alley_corridor_node_count")]
    public int BackAlleyCorridorNodeCount { get; set; }

    [JsonPropertyName("greenspace_mass_node_count")]
    public int GreenspaceMassNodeCount { get; set; }

    [JsonPropertyName("civic_placeholder_node_count")]
    public int CivicPlaceholderNodeCount { get; set; }

    [JsonPropertyName("ignored_boundary_component_count")]
    public int IgnoredBoundaryComponentCount { get; set; }

    [JsonPropertyName("created_geometry_count")]
    public int CreatedGeometryCount { get; set; }

    [JsonPropertyName("writer_ready_profile_count")]
    public int WriterReadyProfileCount { get; set; }

    [JsonPropertyName("runtime_validated_profile_count")]
    public int RuntimeValidatedProfileCount { get; set; }

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

public sealed class DeadMtlWorldBuilderComponentAccessProfileRecord
{
    [JsonPropertyName("profile_order")]
    public int ProfileOrder { get; set; }

    [JsonPropertyName("profile_id")]
    public string ProfileId { get; set; } = string.Empty;

    [JsonPropertyName("component_order")]
    public int ComponentOrder { get; set; }

    [JsonPropertyName("component_id")]
    public string ComponentId { get; set; } = string.Empty;

    [JsonPropertyName("source_color")]
    public string SourceColor { get; set; } = string.Empty;

    [JsonPropertyName("intent")]
    public string Intent { get; set; } = string.Empty;

    [JsonPropertyName("intent_family")]
    public string IntentFamily { get; set; } = string.Empty;

    [JsonPropertyName("frontage_candidate_count")]
    public int FrontageCandidateCount { get; set; }

    [JsonPropertyName("rear_service_access_candidate_count")]
    public int RearServiceAccessCandidateCount { get; set; }

    [JsonPropertyName("mixed_lot_block_candidate_count")]
    public int MixedLotBlockCandidateCount { get; set; }

    [JsonPropertyName("street_network_touchpoint_count")]
    public int StreetNetworkTouchpointCount { get; set; }

    [JsonPropertyName("greenspace_access_candidate_count")]
    public int GreenspaceAccessCandidateCount { get; set; }

    [JsonPropertyName("greenspace_civic_candidate_count")]
    public int GreenspaceCivicCandidateCount { get; set; }

    [JsonPropertyName("ignore_boundary_adjacency_count")]
    public int IgnoreBoundaryAdjacencyCount { get; set; }

    [JsonPropertyName("total_actionable_candidate_count")]
    public int TotalActionableCandidateCount { get; set; }

    [JsonPropertyName("total_candidate_count")]
    public int TotalCandidateCount { get; set; }

    [JsonPropertyName("primary_frontage_component_id")]
    public string PrimaryFrontageComponentId { get; set; } = string.Empty;

    [JsonPropertyName("primary_frontage_contact_px")]
    public int PrimaryFrontageContactPx { get; set; }

    [JsonPropertyName("primary_rear_service_component_id")]
    public string PrimaryRearServiceComponentId { get; set; } = string.Empty;

    [JsonPropertyName("primary_rear_service_contact_px")]
    public int PrimaryRearServiceContactPx { get; set; }

    [JsonPropertyName("access_readiness_class")]
    public string AccessReadinessClass { get; set; } = string.Empty;

    [JsonPropertyName("access_readiness_blocked_by")]
    public List<string> AccessReadinessBlockedBy { get; set; } = new();

    [JsonPropertyName("profile_status")]
    public string ProfileStatus { get; set; } = "COMPONENT_ACCESS_PROFILE_EXTRACTED";

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = "NO_GEOMETRY_CREATED";
}
