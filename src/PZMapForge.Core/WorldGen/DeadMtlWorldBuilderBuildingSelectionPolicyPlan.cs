using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderBuildingSelectionPolicyPlanItem
{
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("zone_type")]
    public string ZoneType { get; set; } = string.Empty;

    [JsonPropertyName("street_class")]
    public string StreetClass { get; set; } = string.Empty;

    [JsonPropertyName("selection_action")]
    public string SelectionAction { get; set; } = string.Empty;

    [JsonPropertyName("placement_mode")]
    public string PlacementMode { get; set; } = string.Empty;

    [JsonPropertyName("frontage_requirement")]
    public string FrontageRequirement { get; set; } = string.Empty;

    [JsonPropertyName("alley_rule")]
    public string AlleyRule { get; set; } = string.Empty;

    [JsonPropertyName("facade_rule")]
    public string FacadeRule { get; set; } = string.Empty;

    [JsonPropertyName("sidewalk_relation")]
    public string SidewalkRelation { get; set; } = string.Empty;

    [JsonPropertyName("allowed_building_families")]
    public List<string> AllowedBuildingFamilies { get; set; } = new();

    [JsonPropertyName("fit_policy")]
    public string FitPolicy { get; set; } = string.Empty;

    [JsonPropertyName("source_confidence")]
    public string SourceConfidence { get; set; } = "SEMANTIC_METADATA_ONLY";

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderBuildingSelectionPolicyPlanTotals
{
    [JsonPropertyName("plan_item_count")]
    public int PlanItemCount { get; set; }

    [JsonPropertyName("residential_selection_later_count")]
    public int ResidentialSelectionLaterCount { get; set; }

    [JsonPropertyName("commercial_selection_later_count")]
    public int CommercialSelectionLaterCount { get; set; }

    [JsonPropertyName("unique_building_required_later_count")]
    public int UniqueBuildingRequiredLaterCount { get; set; }

    [JsonPropertyName("greenspace_no_selection_count")]
    public int GreenspaceNoSelectionCount { get; set; }

    [JsonPropertyName("street_no_selection_count")]
    public int StreetNoSelectionCount { get; set; }

    [JsonPropertyName("ignore_count")]
    public int IgnoreCount { get; set; }

    [JsonPropertyName("concrete_building_ids_selected_now_count")]
    public int ConcreteBuildingIdsSelectedNowCount { get; set; }
}

public sealed class DeadMtlWorldBuilderBuildingSelectionPolicyPlanClaimBoundary
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
}

public sealed class DeadMtlWorldBuilderBuildingSelectionPolicyPlan
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = "pzmapforge.deadmtl.worldbuilder.building-selection-policy-plan.v1";

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

    [JsonPropertyName("status")]
    public string Status { get; set; } = "BUILDING_SELECTION_POLICY_PLAN_CONTRACT_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; set; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; set; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("generation_status")]
    public string GenerationStatus { get; set; } = "NOT_EXECUTED";

    [JsonPropertyName("placement_status")]
    public string PlacementStatus { get; set; } = "NOT_PLACED";

    [JsonPropertyName("plan_items")]
    public List<DeadMtlWorldBuilderBuildingSelectionPolicyPlanItem> PlanItems { get; set; } = new();

    [JsonPropertyName("totals")]
    public DeadMtlWorldBuilderBuildingSelectionPolicyPlanTotals Totals { get; set; } = new();

    [JsonPropertyName("claim_boundary")]
    public DeadMtlWorldBuilderBuildingSelectionPolicyPlanClaimBoundary ClaimBoundary { get; set; } = new();
}

public sealed class DeadMtlWorldBuilderBuildingSelectionPolicyPlanResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public DeadMtlWorldBuilderBuildingSelectionPolicyPlan Plan { get; set; } = new();
}
