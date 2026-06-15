using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderConcreteGeometryPreflightRequirement
{
    [JsonPropertyName("requirement_order")]
    public int RequirementOrder { get; set; }

    [JsonPropertyName("requirement_id")]
    public string RequirementId { get; set; } = string.Empty;

    [JsonPropertyName("requirement_type")]
    public string RequirementType { get; set; } = string.Empty;

    [JsonPropertyName("source_steps")]
    public List<string> SourceSteps { get; set; } = new();

    [JsonPropertyName("source_component_colors")]
    public List<string> SourceComponentColors { get; set; } = new();

    [JsonPropertyName("required_before_materialization")]
    public bool RequiredBeforeMaterialization { get; set; } = true;

    [JsonPropertyName("can_execute_now")]
    public bool CanExecuteNow { get; set; }

    [JsonPropertyName("blocked_reason")]
    public string BlockedReason { get; set; } = string.Empty;

    [JsonPropertyName("expected_future_output")]
    public string ExpectedFutureOutput { get; set; } = string.Empty;

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = "NOT_CREATED";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; set; } = "NOT_APPLICABLE";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; set; } = "NOT_APPLICABLE";

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderConcreteGeometryPreflightTotals
{
    [JsonPropertyName("requirement_count")]
    public int RequirementCount { get; set; }

    [JsonPropertyName("blocked_requirement_count")]
    public int BlockedRequirementCount { get; set; }

    [JsonPropertyName("can_execute_now_count")]
    public int CanExecuteNowCount { get; set; }

    [JsonPropertyName("geometry_requirement_count")]
    public int GeometryRequirementCount { get; set; }

    [JsonPropertyName("writer_requirement_count")]
    public int WriterRequirementCount { get; set; }

    [JsonPropertyName("runtime_requirement_count")]
    public int RuntimeRequirementCount { get; set; }

    [JsonPropertyName("concrete_geometry_created_now_count")]
    public int ConcreteGeometryCreatedNowCount { get; set; }

    [JsonPropertyName("layout_materialized_now_count")]
    public int LayoutMaterializedNowCount { get; set; }

    [JsonPropertyName("writer_ready_requirement_count")]
    public int WriterReadyRequirementCount { get; set; }

    [JsonPropertyName("runtime_validated_requirement_count")]
    public int RuntimeValidatedRequirementCount { get; set; }
}

public sealed class DeadMtlWorldBuilderConcreteGeometryPreflightClaimBoundary
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

public sealed class DeadMtlWorldBuilderConcreteGeometryPreflight
{
    [JsonPropertyName("format")]
    public string Format { get; set; } =
        "pzmapforge.deadmtl.worldbuilder.concrete-geometry-preflight.v1";

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

    [JsonPropertyName("source_future_world_layout_plan_json")]
    public string SourceFutureWorldLayoutPlanJson { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "CONCRETE_GEOMETRY_PREFLIGHT_CONTRACT_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; set; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; set; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("generation_status")]
    public string GenerationStatus { get; set; } = "NOT_EXECUTED";

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = "NO_GEOMETRY_CREATED";

    [JsonPropertyName("preflight_status")]
    public string PreflightStatus { get; set; } = "BLOCKED_PENDING_GEOMETRY_AND_WRITER";

    [JsonPropertyName("preflight_requirements")]
    public List<DeadMtlWorldBuilderConcreteGeometryPreflightRequirement> PreflightRequirements { get; set; } = new();

    [JsonPropertyName("totals")]
    public DeadMtlWorldBuilderConcreteGeometryPreflightTotals Totals { get; set; } = new();

    [JsonPropertyName("claim_boundary")]
    public DeadMtlWorldBuilderConcreteGeometryPreflightClaimBoundary ClaimBoundary { get; set; } = new();
}

public sealed class DeadMtlWorldBuilderConcreteGeometryPreflightResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public DeadMtlWorldBuilderConcreteGeometryPreflight Preflight { get; set; } = new();
}
