using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderSourceMaskRegionRecord
{
    [JsonPropertyName("region_order")]
    public int RegionOrder { get; set; }

    [JsonPropertyName("source_color")]
    public string SourceColor { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("zone_type")]
    public string ZoneType { get; set; } = string.Empty;

    [JsonPropertyName("street_class")]
    public string StreetClass { get; set; } = string.Empty;

    [JsonPropertyName("pixel_count")]
    public int PixelCount { get; set; }

    [JsonPropertyName("bounds_min_x")]
    public int BoundsMinX { get; set; }

    [JsonPropertyName("bounds_min_y")]
    public int BoundsMinY { get; set; }

    [JsonPropertyName("bounds_max_x")]
    public int BoundsMaxX { get; set; }

    [JsonPropertyName("bounds_max_y")]
    public int BoundsMaxY { get; set; }

    [JsonPropertyName("bounds_width")]
    public int BoundsWidth { get; set; }

    [JsonPropertyName("bounds_height")]
    public int BoundsHeight { get; set; }

    [JsonPropertyName("bounds_status")]
    public string BoundsStatus { get; set; } = "PIXEL_BOUNDS_ONLY";

    [JsonPropertyName("primitive_type")]
    public string PrimitiveType { get; set; } = "MASK_REGION";

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = "SOURCE_MASK_ONLY_NO_GEOMETRY_CREATED";

    [JsonPropertyName("source_confidence")]
    public string SourceConfidence { get; set; } = "PNG_AND_ZONE_METADATA_MATCH";

    [JsonPropertyName("future_geometry_requirement_ids")]
    public List<string> FutureGeometryRequirementIds { get; set; } = new();

    [JsonPropertyName("future_consumers")]
    public List<string> FutureConsumers { get; set; } = new();

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderSourceMaskRegionSourceContract
{
    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("source_png_width_px")]
    public int SourcePngWidthPx { get; set; }

    [JsonPropertyName("source_png_height_px")]
    public int SourcePngHeightPx { get; set; }

    [JsonPropertyName("source_pixel_count")]
    public int SourcePixelCount { get; set; }

    [JsonPropertyName("authoring_scale")]
    public string AuthoringScale { get; set; } = "1_PIXEL_EQUALS_1_WORLD_TILE";

    [JsonPropertyName("pixel_origin")]
    public string PixelOrigin { get; set; } = "TOP_LEFT";

    [JsonPropertyName("x_axis")]
    public string XAxis { get; set; } = "EAST_POSITIVE";

    [JsonPropertyName("y_axis")]
    public string YAxis { get; set; } = "SOUTH_POSITIVE";

    [JsonPropertyName("coordinate_units")]
    public string CoordinateUnits { get; set; } = "SOURCE_PIXELS";

    [JsonPropertyName("bounds_are_pixel_bounds_not_geometry")]
    public bool BoundsArePixelBoundsNotGeometry { get; set; } = true;
}

public sealed class DeadMtlWorldBuilderSourceMaskRegionValidationRules
{
    [JsonPropertyName("png_must_exist")]
    public bool PngMustExist { get; set; } = true;

    [JsonPropertyName("png_must_be_256_by_256")]
    public bool PngMustBe256By256 { get; set; } = true;

    [JsonPropertyName("all_pixels_must_match_known_metadata_colors")]
    public bool AllPixelsMustMatchKnownMetadataColors { get; set; } = true;

    [JsonPropertyName("metadata_must_include_all_png_colors")]
    public bool MetadataMustIncludeAllPngColors { get; set; } = true;

    [JsonPropertyName("pixel_total_must_equal_width_times_height")]
    public bool PixelTotalMustEqualWidthTimesHeight { get; set; } = true;

    [JsonPropertyName("mask_bounds_must_be_within_source_png")]
    public bool MaskBoundsMustBeWithinSourcePng { get; set; } = true;

    [JsonPropertyName("mask_regions_are_not_geometry")]
    public bool MaskRegionsAreNotGeometry { get; set; } = true;

    [JsonPropertyName("no_runtime_claim_from_extraction")]
    public bool NoRuntimeClaimFromExtraction { get; set; } = true;

    [JsonPropertyName("no_writer_claim_from_extraction")]
    public bool NoWriterClaimFromExtraction { get; set; } = true;

    [JsonPropertyName("no_materialization_from_extraction")]
    public bool NoMaterializationFromExtraction { get; set; } = true;
}

public sealed class DeadMtlWorldBuilderSourceMaskRegionExtractionTotals
{
    [JsonPropertyName("source_png_width_px")]
    public int SourcePngWidthPx { get; set; }

    [JsonPropertyName("source_png_height_px")]
    public int SourcePngHeightPx { get; set; }

    [JsonPropertyName("source_pixel_count")]
    public int SourcePixelCount { get; set; }

    [JsonPropertyName("mask_region_count")]
    public int MaskRegionCount { get; set; }

    [JsonPropertyName("known_color_region_count")]
    public int KnownColorRegionCount { get; set; }

    [JsonPropertyName("unknown_color_region_count")]
    public int UnknownColorRegionCount { get; set; }

    [JsonPropertyName("metadata_matched_region_count")]
    public int MetadataMatchedRegionCount { get; set; }

    [JsonPropertyName("metadata_missing_region_count")]
    public int MetadataMissingRegionCount { get; set; }

    [JsonPropertyName("mask_region_pixel_total")]
    public int MaskRegionPixelTotal { get; set; }

    [JsonPropertyName("residential_pixel_count")]
    public int ResidentialPixelCount { get; set; }

    [JsonPropertyName("main_road_pixel_count")]
    public int MainRoadPixelCount { get; set; }

    [JsonPropertyName("greenspace_pixel_count")]
    public int GreenspacePixelCount { get; set; }

    [JsonPropertyName("back_alley_pixel_count")]
    public int BackAlleyPixelCount { get; set; }

    [JsonPropertyName("civic_placeholder_pixel_count")]
    public int CivicPlaceholderPixelCount { get; set; }

    [JsonPropertyName("commercial_pixel_count")]
    public int CommercialPixelCount { get; set; }

    [JsonPropertyName("ignore_pixel_count")]
    public int IgnorePixelCount { get; set; }

    [JsonPropertyName("created_geometry_count")]
    public int CreatedGeometryCount { get; set; }

    [JsonPropertyName("writer_ready_region_count")]
    public int WriterReadyRegionCount { get; set; }

    [JsonPropertyName("runtime_validated_region_count")]
    public int RuntimeValidatedRegionCount { get; set; }
}

public sealed class DeadMtlWorldBuilderSourceMaskRegionExtractionClaimBoundary
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

public sealed class DeadMtlWorldBuilderSourceMaskRegionExtraction
{
    [JsonPropertyName("format")]
    public string Format { get; set; } =
        "pzmapforge.deadmtl.worldbuilder.source-mask-region-extraction.v1";

    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("source_png")]
    public string SourcePng { get; set; } = string.Empty;

    [JsonPropertyName("source_zone_metadata_json")]
    public string SourceZoneMetadataJson { get; set; } = string.Empty;

    [JsonPropertyName("source_geometry_primitive_schema_json")]
    public string SourceGeometryPrimitiveSchemaJson { get; set; } = string.Empty;

    [JsonPropertyName("source_concrete_geometry_preflight_json")]
    public string SourceConcreteGeometryPreflightJson { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "SOURCE_MASK_REGION_EXTRACTION_CONTRACT_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; set; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; set; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("generation_status")]
    public string GenerationStatus { get; set; } = "NOT_EXECUTED";

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = "MASK_REGIONS_ONLY_NO_GEOMETRY_CREATED";

    [JsonPropertyName("extraction_status")]
    public string ExtractionStatus { get; set; } = "SOURCE_MASK_REGIONS_EXTRACTED";

    [JsonPropertyName("source_contract")]
    public DeadMtlWorldBuilderSourceMaskRegionSourceContract SourceContract { get; set; } = new();

    [JsonPropertyName("validation_rules")]
    public DeadMtlWorldBuilderSourceMaskRegionValidationRules ValidationRules { get; set; } = new();

    [JsonPropertyName("mask_regions")]
    public List<DeadMtlWorldBuilderSourceMaskRegionRecord> MaskRegions { get; set; } = new();

    [JsonPropertyName("totals")]
    public DeadMtlWorldBuilderSourceMaskRegionExtractionTotals Totals { get; set; } = new();

    [JsonPropertyName("claim_boundary")]
    public DeadMtlWorldBuilderSourceMaskRegionExtractionClaimBoundary ClaimBoundary { get; set; } = new();
}

public sealed class DeadMtlWorldBuilderSourceMaskRegionExtractionResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public DeadMtlWorldBuilderSourceMaskRegionExtraction Extraction { get; set; } = new();
}
