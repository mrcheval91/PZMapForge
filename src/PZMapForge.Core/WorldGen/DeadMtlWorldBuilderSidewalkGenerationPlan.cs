using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderSidewalkGenerationPlanItem
{
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("street_class")]
    public string StreetClass { get; set; } = string.Empty;

    [JsonPropertyName("sidewalk_action")]
    public string SidewalkAction { get; set; } = string.Empty;

    [JsonPropertyName("left_width_tiles")]
    public int LeftWidthTiles { get; set; }

    [JsonPropertyName("right_width_tiles")]
    public int RightWidthTiles { get; set; }

    [JsonPropertyName("sidewalk_source")]
    public string SidewalkSource { get; set; } = string.Empty;

    [JsonPropertyName("source_policy")]
    public string SourcePolicy { get; set; } = string.Empty;

    [JsonPropertyName("claim_status")]
    public string ClaimStatus { get; set; } = "NOT_EXECUTED";

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderSidewalkGenerationPlanTotals
{
    [JsonPropertyName("plan_item_count")]
    public int PlanItemCount { get; set; }

    [JsonPropertyName("main_road_sidewalk_later_count")]
    public int MainRoadSidewalkLaterCount { get; set; }

    [JsonPropertyName("back_alley_no_sidewalk_count")]
    public int BackAlleyNoSidewalkCount { get; set; }

    [JsonPropertyName("left_width_tiles_total")]
    public int LeftWidthTilesTotal { get; set; }

    [JsonPropertyName("right_width_tiles_total")]
    public int RightWidthTilesTotal { get; set; }

    [JsonPropertyName("sidewalk_source")]
    public string SidewalkSource { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderSidewalkGenerationPlanClaimBoundary
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

public sealed class DeadMtlWorldBuilderSidewalkGenerationPlan
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = "pzmapforge.deadmtl.worldbuilder.sidewalk-generation-plan.v1";

    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("source_profile_json")]
    public string SourceProfileJson { get; set; } = string.Empty;

    [JsonPropertyName("source_zone_metadata_json")]
    public string SourceZoneMetadataJson { get; set; } = string.Empty;

    [JsonPropertyName("source_lot_subdivision_plan_json")]
    public string SourceLotSubdivisionPlanJson { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "SIDEWALK_GENERATION_PLAN_CONTRACT_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; set; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; set; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("generation_status")]
    public string GenerationStatus { get; set; } = "NOT_EXECUTED";

    [JsonPropertyName("plan_items")]
    public List<DeadMtlWorldBuilderSidewalkGenerationPlanItem> PlanItems { get; set; } = new();

    [JsonPropertyName("totals")]
    public DeadMtlWorldBuilderSidewalkGenerationPlanTotals Totals { get; set; } = new();

    [JsonPropertyName("claim_boundary")]
    public DeadMtlWorldBuilderSidewalkGenerationPlanClaimBoundary ClaimBoundary { get; set; } = new();
}

public sealed class DeadMtlWorldBuilderSidewalkGenerationPlanResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public DeadMtlWorldBuilderSidewalkGenerationPlan Plan { get; set; } = new();
}
