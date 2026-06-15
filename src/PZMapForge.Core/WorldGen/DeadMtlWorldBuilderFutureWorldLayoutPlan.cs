using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderFutureWorldLayoutPlanComponent
{
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("zone_type")]
    public string ZoneType { get; set; } = string.Empty;

    [JsonPropertyName("street_class")]
    public string StreetClass { get; set; } = string.Empty;

    [JsonPropertyName("component_type")]
    public string ComponentType { get; set; } = string.Empty;

    [JsonPropertyName("future_action")]
    public string FutureAction { get; set; } = string.Empty;

    [JsonPropertyName("lot_policy_source")]
    public string LotPolicySource { get; set; } = string.Empty;

    [JsonPropertyName("sidewalk_policy_source")]
    public string SidewalkPolicySource { get; set; } = string.Empty;

    [JsonPropertyName("building_policy_source")]
    public string BuildingPolicySource { get; set; } = string.Empty;

    [JsonPropertyName("frontage_rule")]
    public string FrontageRule { get; set; } = string.Empty;

    [JsonPropertyName("service_access_rule")]
    public string ServiceAccessRule { get; set; } = string.Empty;

    [JsonPropertyName("future_geometry_status")]
    public string FutureGeometryStatus { get; set; } = "NOT_CREATED";

    [JsonPropertyName("source_confidence")]
    public string SourceConfidence { get; set; } = string.Empty;

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderFutureExecutionRequirements
{
    [JsonPropertyName("requires_actual_lot_geometry")]
    public bool RequiresActualLotGeometry { get; set; } = true;

    [JsonPropertyName("requires_actual_sidewalk_geometry")]
    public bool RequiresActualSidewalkGeometry { get; set; } = true;

    [JsonPropertyName("requires_building_catalogue")]
    public bool RequiresBuildingCatalogue { get; set; } = true;

    [JsonPropertyName("requires_unique_building_bindings")]
    public bool RequiresUniqueBuildingBindings { get; set; } = true;

    [JsonPropertyName("requires_tile_writer")]
    public bool RequiresTileWriter { get; set; } = true;

    [JsonPropertyName("requires_runtime_validation")]
    public bool RequiresRuntimeValidation { get; set; } = true;

    [JsonPropertyName("can_execute_now")]
    public bool CanExecuteNow { get; set; }

    [JsonPropertyName("blocked_reason")]
    public string BlockedReason { get; set; } =
        "CONTRACT_ONLY_NO_GEOMETRY_NO_WRITER_NO_RUNTIME_PROOF";
}

public sealed class DeadMtlWorldBuilderFutureWorldLayoutPlanTotals
{
    [JsonPropertyName("layout_component_count")]
    public int LayoutComponentCount { get; set; }

    [JsonPropertyName("zone_layout_component_count")]
    public int ZoneLayoutComponentCount { get; set; }

    [JsonPropertyName("street_layout_component_count")]
    public int StreetLayoutComponentCount { get; set; }

    [JsonPropertyName("unique_placeholder_layout_component_count")]
    public int UniquePlaceholderLayoutComponentCount { get; set; }

    [JsonPropertyName("ignore_layout_component_count")]
    public int IgnoreLayoutComponentCount { get; set; }

    [JsonPropertyName("future_lot_group_policy_count")]
    public int FutureLotGroupPolicyCount { get; set; }

    [JsonPropertyName("future_sidewalk_corridor_policy_count")]
    public int FutureSidewalkCorridorPolicyCount { get; set; }

    [JsonPropertyName("future_back_alley_service_corridor_policy_count")]
    public int FutureBackAlleyServiceCorridorPolicyCount { get; set; }

    [JsonPropertyName("future_building_slot_policy_count")]
    public int FutureBuildingSlotPolicyCount { get; set; }

    [JsonPropertyName("concrete_geometry_created_now_count")]
    public int ConcreteGeometryCreatedNowCount { get; set; }

    [JsonPropertyName("concrete_building_ids_selected_now_count")]
    public int ConcreteBuildingIdsSelectedNowCount { get; set; }

    [JsonPropertyName("layout_materialized_now_count")]
    public int LayoutMaterializedNowCount { get; set; }
}

public sealed class DeadMtlWorldBuilderFutureWorldLayoutPlanClaimBoundary
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

public sealed class DeadMtlWorldBuilderFutureWorldLayoutPlan
{
    [JsonPropertyName("format")]
    public string Format { get; set; } =
        "pzmapforge.deadmtl.worldbuilder.future-world-layout-plan.v1";

    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("source_profile_json")]
    public string SourceProfileJson { get; set; } = string.Empty;

    [JsonPropertyName("source_zone_metadata_json")]
    public string SourceZoneMetadataJson { get; set; } = string.Empty;

    [JsonPropertyName("source_lot_subdivision_plan_json")]
    public string SourceLotSubdivisionPlanJson { get; set; } = string.Empty;

    [JsonPropertyName("source_sidewalk_generation_plan_json")]
    public string SourceSidewalkGenerationPlanJson { get; set; } = string.Empty;

    [JsonPropertyName("source_building_selection_policy_plan_json")]
    public string SourceBuildingSelectionPolicyPlanJson { get; set; } = string.Empty;

    [JsonPropertyName("source_generation_dependency_manifest_json")]
    public string SourceGenerationDependencyManifestJson { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "FUTURE_WORLD_LAYOUT_PLAN_CONTRACT_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; set; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; set; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("generation_status")]
    public string GenerationStatus { get; set; } = "NOT_EXECUTED";

    [JsonPropertyName("layout_status")]
    public string LayoutStatus { get; set; } = "NOT_MATERIALIZED";

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = "NO_GEOMETRY_CREATED";

    [JsonPropertyName("layout_components")]
    public List<DeadMtlWorldBuilderFutureWorldLayoutPlanComponent> LayoutComponents { get; set; } = new();

    [JsonPropertyName("future_execution_requirements")]
    public DeadMtlWorldBuilderFutureExecutionRequirements FutureExecutionRequirements { get; set; } = new();

    [JsonPropertyName("totals")]
    public DeadMtlWorldBuilderFutureWorldLayoutPlanTotals Totals { get; set; } = new();

    [JsonPropertyName("claim_boundary")]
    public DeadMtlWorldBuilderFutureWorldLayoutPlanClaimBoundary ClaimBoundary { get; set; } = new();
}

public sealed class DeadMtlWorldBuilderFutureWorldLayoutPlanResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public DeadMtlWorldBuilderFutureWorldLayoutPlan Plan { get; set; } = new();
}
