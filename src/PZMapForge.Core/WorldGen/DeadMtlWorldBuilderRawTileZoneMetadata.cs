using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderRawTileLotSubdivisionPolicy
{
    [JsonPropertyName("enabled_later")]
    public bool EnabledLater { get; set; }

    [JsonPropertyName("future_status")]
    public string FutureStatus { get; set; } = string.Empty;

    [JsonPropertyName("preferred_frontage")]
    public string PreferredFrontage { get; set; } = string.Empty;

    [JsonPropertyName("avoid_frontage")]
    public List<string> AvoidFrontage { get; set; } = new();

    [JsonPropertyName("facade_orientation_policy")]
    public string FacadeOrientationPolicy { get; set; } = string.Empty;

    [JsonPropertyName("if_single_main_road_frontage")]
    public string IfSingleMainRoadFrontage { get; set; } = string.Empty;

    [JsonPropertyName("lot_line_fence_policy")]
    public string LotLineFencePolicy { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderRawTileColorRole
{
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("zone_type")]
    public string ZoneType { get; set; } = string.Empty;

    [JsonPropertyName("development_intensity")]
    public string DevelopmentIntensity { get; set; } = string.Empty;

    [JsonPropertyName("street_class")]
    public string StreetClass { get; set; } = string.Empty;

    [JsonPropertyName("sidewalk_eligible")]
    public bool SidewalkEligible { get; set; }

    [JsonPropertyName("sidewalk_policy_source")]
    public string SidewalkPolicySource { get; set; } = string.Empty;

    [JsonPropertyName("procedural_fill")]
    public bool ProceduralFill { get; set; }

    [JsonPropertyName("unique_override_allowed")]
    public bool UniqueOverrideAllowed { get; set; }

    [JsonPropertyName("lot_subdivision_policy")]
    public DeadMtlWorldBuilderRawTileLotSubdivisionPolicy? LotSubdivisionPolicy { get; set; }

    [JsonPropertyName("building_family_hints")]
    public List<string> BuildingFamilyHints { get; set; } = new();

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderRawTileZoneMetadataClaimBoundary
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
}

public sealed class DeadMtlWorldBuilderRawTileZoneMetadata
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("source_image")]
    public string SourceImage { get; set; } = string.Empty;

    [JsonPropertyName("source_inspection_json")]
    public string SourceInspectionJson { get; set; } = string.Empty;

    [JsonPropertyName("source_palette_mapping_json")]
    public string SourcePaletteMappingJson { get; set; } = string.Empty;

    [JsonPropertyName("neighborhood_profile_id")]
    public string NeighborhoodProfileId { get; set; } = string.Empty;

    [JsonPropertyName("neighborhood_profile_path")]
    public string NeighborhoodProfilePath { get; set; } = string.Empty;

    [JsonPropertyName("metadata_status")]
    public string MetadataStatus { get; set; } = string.Empty;

    [JsonPropertyName("color_roles")]
    public List<DeadMtlWorldBuilderRawTileColorRole> ColorRoles { get; set; } = new();

    [JsonPropertyName("claim_boundary")]
    public DeadMtlWorldBuilderRawTileZoneMetadataClaimBoundary? ClaimBoundary { get; set; }
}

public sealed class DeadMtlWorldBuilderRawTileZoneMetadataValidationCheck
{
    [JsonPropertyName("rule_id")]
    public string RuleId { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("passed")]
    public bool Passed { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderRawTileZoneMetadataValidationTotals
{
    [JsonPropertyName("checks_run")]
    public int ChecksRun { get; set; }

    [JsonPropertyName("passed")]
    public int Passed { get; set; }

    [JsonPropertyName("failed")]
    public int Failed { get; set; }

    [JsonPropertyName("errors")]
    public int Errors { get; set; }

    [JsonPropertyName("warnings")]
    public int Warnings { get; set; }
}

public sealed class DeadMtlWorldBuilderRawTileZoneMetadataValidation
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata-validation.v1";

    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("metadata_path")]
    public string MetadataPath { get; set; } = string.Empty;

    [JsonPropertyName("is_valid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("checks")]
    public List<DeadMtlWorldBuilderRawTileZoneMetadataValidationCheck> Checks { get; set; } = new();

    [JsonPropertyName("totals")]
    public DeadMtlWorldBuilderRawTileZoneMetadataValidationTotals Totals { get; set; } = new();

    [JsonPropertyName("claim_boundary")]
    public DeadMtlWorldBuilderRawTileZoneMetadataClaimBoundary ClaimBoundary { get; set; } = new();
}

public sealed class DeadMtlWorldBuilderRawTileZoneMetadataValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public DeadMtlWorldBuilderRawTileZoneMetadataValidation? Validation { get; set; }
}
