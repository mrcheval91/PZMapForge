using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderSidewalkPolicy
{
    [JsonPropertyName("default_has_sidewalks")]
    public bool DefaultHasSidewalks { get; init; }

    [JsonPropertyName("sidewalk_source")]
    public string SidewalkSource { get; init; } = string.Empty;

    [JsonPropertyName("default_left_width_tiles")]
    public int DefaultLeftWidthTiles { get; init; }

    [JsonPropertyName("default_right_width_tiles")]
    public int DefaultRightWidthTiles { get; init; }

    [JsonPropertyName("allow_asymmetric_sidewalks")]
    public bool AllowAsymmetricSidewalks { get; init; }

    [JsonPropertyName("allow_street_override")]
    public bool AllowStreetOverride { get; init; }
}

public sealed class DeadMtlWorldBuilderStreetPolicy
{
    [JsonPropertyName("default_corridor_width_tiles")]
    public int DefaultCorridorWidthTiles { get; init; }

    [JsonPropertyName("default_lane_width_tiles")]
    public int DefaultLaneWidthTiles { get; init; }

    [JsonPropertyName("default_lane_count")]
    public int DefaultLaneCount { get; init; }

    [JsonPropertyName("parking_lane_left")]
    public bool ParkingLaneLeft { get; init; }

    [JsonPropertyName("parking_lane_right")]
    public bool ParkingLaneRight { get; init; }

    [JsonPropertyName("tree_strip_width_tiles")]
    public int TreeStripWidthTiles { get; init; }

    [JsonPropertyName("snowbank_reserved_width_tiles")]
    public int SnowbankReservedWidthTiles { get; init; }

    [JsonPropertyName("curb_style")]
    public string CurbStyle { get; init; } = string.Empty;

    [JsonPropertyName("street_light_policy")]
    public string StreetLightPolicy { get; init; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderZoneFamilyPolicy
{
    [JsonPropertyName("default_intensity")]
    public string DefaultIntensity { get; init; } = string.Empty;

    [JsonPropertyName("allowed_intensities")]
    public List<string> AllowedIntensities { get; init; } = new();

    [JsonPropertyName("building_families")]
    public List<string> BuildingFamilies { get; init; } = new();

    [JsonPropertyName("procedural_fill")]
    public bool ProceduralFill { get; init; }
}

public sealed class DeadMtlWorldBuilderZoningPolicy
{
    [JsonPropertyName("residential")]
    public DeadMtlWorldBuilderZoneFamilyPolicy? Residential { get; init; }

    [JsonPropertyName("commercial")]
    public DeadMtlWorldBuilderZoneFamilyPolicy? Commercial { get; init; }

    [JsonPropertyName("industrial")]
    public DeadMtlWorldBuilderZoneFamilyPolicy? Industrial { get; init; }
}

public sealed class DeadMtlWorldBuilderProceduralFillPolicy
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    [JsonPropertyName("fill_missing_until_unique_override")]
    public bool FillMissingUntilUniqueOverride { get; init; }

    [JsonPropertyName("unique_override_priority")]
    public string UniqueOverridePriority { get; init; } = string.Empty;

    [JsonPropertyName("deterministic_seed_policy")]
    public string DeterministicSeedPolicy { get; init; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderPngMetadataPolicy
{
    [JsonPropertyName("supports_color_metadata_file")]
    public bool SupportsColorMetadataFile { get; init; }

    [JsonPropertyName("zone_color_metadata_status")]
    public string ZoneColorMetadataStatus { get; init; } = string.Empty;

    [JsonPropertyName("building_color_metadata_status")]
    public string BuildingColorMetadataStatus { get; init; } = string.Empty;

    [JsonPropertyName("chunk_layer_capture_status")]
    public string ChunkLayerCaptureStatus { get; init; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderNeighborhoodProfileClaimBoundary
{
    [JsonPropertyName("writes_lotpack")]
    public bool WritesLotpack { get; init; } = false;

    [JsonPropertyName("writes_worldgen_lua")]
    public bool WritesWorldgenLua { get; init; } = false;

    [JsonPropertyName("runtime_proven")]
    public bool RuntimeProven { get; init; } = false;

    [JsonPropertyName("public_playable_claim")]
    public bool PublicPlayableClaim { get; init; } = false;

    [JsonPropertyName("writer_ready_claim")]
    public bool WriterReadyClaim { get; init; } = false;

    [JsonPropertyName("generates_buildings_now")]
    public bool GeneratesBuildingsNow { get; init; } = false;

    [JsonPropertyName("captures_chunk_layers_now")]
    public bool CapturesChunkLayersNow { get; init; } = false;
}

public sealed class DeadMtlWorldBuilderNeighborhoodProfile
{
    [JsonPropertyName("format")]
    public string Format { get; init; } = string.Empty;

    [JsonPropertyName("profile_id")]
    public string ProfileId { get; init; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("era")]
    public string Era { get; init; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; init; } = string.Empty;

    [JsonPropertyName("profile_status")]
    public string ProfileStatus { get; init; } = string.Empty;

    [JsonPropertyName("sidewalk_policy")]
    public DeadMtlWorldBuilderSidewalkPolicy? SidewalkPolicy { get; init; }

    [JsonPropertyName("street_policy")]
    public DeadMtlWorldBuilderStreetPolicy? StreetPolicy { get; init; }

    [JsonPropertyName("zoning_policy")]
    public DeadMtlWorldBuilderZoningPolicy? ZoningPolicy { get; init; }

    [JsonPropertyName("procedural_fill_policy")]
    public DeadMtlWorldBuilderProceduralFillPolicy? ProceduralFillPolicy { get; init; }

    [JsonPropertyName("png_metadata_policy")]
    public DeadMtlWorldBuilderPngMetadataPolicy? PngMetadataPolicy { get; init; }

    [JsonPropertyName("claim_boundary")]
    public DeadMtlWorldBuilderNeighborhoodProfileClaimBoundary? ClaimBoundary { get; init; }
}

public sealed class DeadMtlWorldBuilderNeighborhoodProfileValidationCheck
{
    [JsonPropertyName("rule_id")]
    public string RuleId { get; init; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; init; } = string.Empty;

    [JsonPropertyName("passed")]
    public bool Passed { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderNeighborhoodProfileValidationTotals
{
    [JsonPropertyName("checks_run")]
    public int ChecksRun { get; init; }

    [JsonPropertyName("passed")]
    public int Passed { get; init; }

    [JsonPropertyName("failed")]
    public int Failed { get; init; }

    [JsonPropertyName("errors")]
    public int Errors { get; init; }

    [JsonPropertyName("warnings")]
    public int Warnings { get; init; }
}

public sealed class DeadMtlWorldBuilderNeighborhoodProfileValidation
{
    [JsonPropertyName("format")]
    public string Format { get; init; } = "pzmapforge.deadmtl.worldbuilder.neighborhood-profile-validation.v1";

    [JsonPropertyName("profile_path")]
    public string ProfilePath { get; init; } = string.Empty;

    [JsonPropertyName("profile_id")]
    public string ProfileId { get; init; } = string.Empty;

    [JsonPropertyName("is_valid")]
    public bool IsValid { get; init; }

    [JsonPropertyName("checks")]
    public List<DeadMtlWorldBuilderNeighborhoodProfileValidationCheck> Checks { get; init; } = new();

    [JsonPropertyName("totals")]
    public DeadMtlWorldBuilderNeighborhoodProfileValidationTotals Totals { get; init; } = new();

    [JsonPropertyName("claim_boundary")]
    public DeadMtlWorldBuilderNeighborhoodProfileClaimBoundary ClaimBoundary { get; init; } = new();
}

public sealed class DeadMtlWorldBuilderNeighborhoodProfileValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; } = new();
    public DeadMtlWorldBuilderNeighborhoodProfileValidation? Validation { get; set; }
}
