using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderLotSubdivisionPlanItem
{
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("zone_type")]
    public string ZoneType { get; set; } = string.Empty;

    [JsonPropertyName("street_class")]
    public string StreetClass { get; set; } = string.Empty;

    [JsonPropertyName("subdivision_action")]
    public string SubdivisionAction { get; set; } = string.Empty;

    [JsonPropertyName("frontage_policy")]
    public string FrontagePolicy { get; set; } = string.Empty;

    [JsonPropertyName("rear_access_policy")]
    public string RearAccessPolicy { get; set; } = string.Empty;

    [JsonPropertyName("sidewalk_generation_policy")]
    public string SidewalkGenerationPolicy { get; set; } = string.Empty;

    [JsonPropertyName("lot_line_policy")]
    public string LotLinePolicy { get; set; } = string.Empty;

    [JsonPropertyName("building_selection_policy")]
    public string BuildingSelectionPolicy { get; set; } = string.Empty;

    [JsonPropertyName("facade_orientation_policy")]
    public string FacadeOrientationPolicy { get; set; } = string.Empty;

    [JsonPropertyName("source_confidence")]
    public string SourceConfidence { get; set; } = "SEMANTIC_METADATA_ONLY";

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderLotSubdivisionPlanTotals
{
    [JsonPropertyName("plan_item_count")]
    public int PlanItemCount { get; set; }

    [JsonPropertyName("residential_subdivide_later_count")]
    public int ResidentialSubdivideLaterCount { get; set; }

    [JsonPropertyName("commercial_subdivide_later_count")]
    public int CommercialSubdivideLaterCount { get; set; }

    [JsonPropertyName("street_no_subdivision_count")]
    public int StreetNoSubdivisionCount { get; set; }

    [JsonPropertyName("greenspace_no_subdivision_count")]
    public int GreenspaceNoSubdivisionCount { get; set; }

    [JsonPropertyName("unique_placeholder_no_subdivision_count")]
    public int UniquePlaceholderNoSubdivisionCount { get; set; }

    [JsonPropertyName("ignore_count")]
    public int IgnoreCount { get; set; }
}

public sealed class DeadMtlWorldBuilderLotSubdivisionPlanClaimBoundary
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
}

public sealed class DeadMtlWorldBuilderLotSubdivisionPlan
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = "pzmapforge.deadmtl.worldbuilder.lot-subdivision-plan.v1";

    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("source_zone_metadata_json")]
    public string SourceZoneMetadataJson { get; set; } = string.Empty;

    [JsonPropertyName("source_neighborhood_profile_json")]
    public string SourceNeighborhoodProfileJson { get; set; } = string.Empty;

    [JsonPropertyName("source_inspection_json")]
    public string SourceInspectionJson { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "LOT_SUBDIVISION_PLAN_CONTRACT_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; set; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; set; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("subdivision_status")]
    public string SubdivisionStatus { get; set; } = "NOT_EXECUTED";

    [JsonPropertyName("plan_items")]
    public List<DeadMtlWorldBuilderLotSubdivisionPlanItem> PlanItems { get; set; } = new();

    [JsonPropertyName("totals")]
    public DeadMtlWorldBuilderLotSubdivisionPlanTotals Totals { get; set; } = new();

    [JsonPropertyName("claim_boundary")]
    public DeadMtlWorldBuilderLotSubdivisionPlanClaimBoundary ClaimBoundary { get; set; } = new();
}

public sealed class DeadMtlWorldBuilderLotSubdivisionPlanResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public DeadMtlWorldBuilderLotSubdivisionPlan Plan { get; set; } = new();
}
