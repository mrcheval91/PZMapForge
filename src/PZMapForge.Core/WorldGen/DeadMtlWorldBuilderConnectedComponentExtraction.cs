using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderConnectedComponentRecord
{
    [JsonPropertyName("component_order")]
    public int ComponentOrder { get; set; }

    [JsonPropertyName("component_id")]
    public string ComponentId { get; set; } = string.Empty;

    [JsonPropertyName("parent_region_order")]
    public int ParentRegionOrder { get; set; }

    [JsonPropertyName("parent_source_color")]
    public string ParentSourceColor { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("zone_type")]
    public string ZoneType { get; set; } = string.Empty;

    [JsonPropertyName("street_class")]
    public string StreetClass { get; set; } = string.Empty;

    [JsonPropertyName("local_component_index")]
    public int LocalComponentIndex { get; set; }

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
    public string BoundsStatus { get; set; } = "PIXEL_COMPONENT_BOUNDS_ONLY";

    [JsonPropertyName("connectivity_rule")]
    public string ConnectivityRule { get; set; } = "FOUR_WAY_NEIGHBOR_PIXELS";

    [JsonPropertyName("primitive_type")]
    public string PrimitiveType { get; set; } = "MASK_REGION";

    [JsonPropertyName("component_status")]
    public string ComponentStatus { get; set; } = "CONNECTED_PIXEL_COMPONENT_ONLY";

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = "CONNECTED_COMPONENT_ONLY_NO_GEOMETRY_CREATED";

    [JsonPropertyName("source_confidence")]
    public string SourceConfidence { get; set; } = "PNG_METADATA_AND_MASK_REGION_MATCH";

    [JsonPropertyName("future_geometry_requirement_ids")]
    public List<string> FutureGeometryRequirementIds { get; set; } = new();

    [JsonPropertyName("future_consumers")]
    public List<string> FutureConsumers { get; set; } = new();

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderConnectedComponentSourceContract
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

    [JsonPropertyName("connectivity_rule")]
    public string ConnectivityRule { get; set; } = "FOUR_WAY_NEIGHBOR_PIXELS";

    [JsonPropertyName("diagonal_connectivity_enabled")]
    public bool DiagonalConnectivityEnabled { get; set; }

    [JsonPropertyName("bounds_are_pixel_bounds_not_geometry")]
    public bool BoundsArePixelBoundsNotGeometry { get; set; } = true;

    [JsonPropertyName("components_are_not_geometry")]
    public bool ComponentsAreNotGeometry { get; set; } = true;
}

public sealed class DeadMtlWorldBuilderConnectedComponentValidationRules
{
    [JsonPropertyName("png_must_exist")]
    public bool PngMustExist { get; set; } = true;

    [JsonPropertyName("png_must_be_256_by_256")]
    public bool PngMustBe256By256 { get; set; } = true;

    [JsonPropertyName("all_pixels_must_match_known_metadata_colors")]
    public bool AllPixelsMustMatchKnownMetadataColors { get; set; } = true;

    [JsonPropertyName("source_mask_region_extraction_must_exist")]
    public bool SourceMaskRegionExtractionMustExist { get; set; } = true;

    [JsonPropertyName("component_pixel_total_must_equal_source_pixel_count")]
    public bool ComponentPixelTotalMustEqualSourcePixelCount { get; set; } = true;

    [JsonPropertyName("component_color_pixel_totals_must_match_parent_mask_regions")]
    public bool ComponentColorPixelTotalsMustMatchParentMaskRegions { get; set; } = true;

    [JsonPropertyName("component_bounds_must_be_within_source_png")]
    public bool ComponentBoundsMustBeWithinSourcePng { get; set; } = true;

    [JsonPropertyName("components_are_not_geometry")]
    public bool ComponentsAreNotGeometry { get; set; } = true;

    [JsonPropertyName("no_diagonal_connectivity")]
    public bool NoDiagonalConnectivity { get; set; } = true;

    [JsonPropertyName("no_runtime_claim_from_component_extraction")]
    public bool NoRuntimeClaimFromComponentExtraction { get; set; } = true;

    [JsonPropertyName("no_writer_claim_from_component_extraction")]
    public bool NoWriterClaimFromComponentExtraction { get; set; } = true;

    [JsonPropertyName("no_materialization_from_component_extraction")]
    public bool NoMaterializationFromComponentExtraction { get; set; } = true;
}

public sealed class DeadMtlWorldBuilderConnectedComponentExtractionTotals
{
    [JsonPropertyName("source_png_width_px")]
    public int SourcePngWidthPx { get; set; }

    [JsonPropertyName("source_png_height_px")]
    public int SourcePngHeightPx { get; set; }

    [JsonPropertyName("source_pixel_count")]
    public int SourcePixelCount { get; set; }

    [JsonPropertyName("parent_mask_region_count")]
    public int ParentMaskRegionCount { get; set; }

    [JsonPropertyName("connected_component_count")]
    public int ConnectedComponentCount { get; set; }

    [JsonPropertyName("known_color_component_count")]
    public int KnownColorComponentCount { get; set; }

    [JsonPropertyName("unknown_color_component_count")]
    public int UnknownColorComponentCount { get; set; }

    [JsonPropertyName("component_pixel_total")]
    public int ComponentPixelTotal { get; set; }

    [JsonPropertyName("residential_component_count")]
    public int ResidentialComponentCount { get; set; }

    [JsonPropertyName("main_road_component_count")]
    public int MainRoadComponentCount { get; set; }

    [JsonPropertyName("greenspace_component_count")]
    public int GreenspaceComponentCount { get; set; }

    [JsonPropertyName("back_alley_component_count")]
    public int BackAlleyComponentCount { get; set; }

    [JsonPropertyName("civic_placeholder_component_count")]
    public int CivicPlaceholderComponentCount { get; set; }

    [JsonPropertyName("commercial_component_count")]
    public int CommercialComponentCount { get; set; }

    [JsonPropertyName("ignore_component_count")]
    public int IgnoreComponentCount { get; set; }

    [JsonPropertyName("residential_pixel_total")]
    public int ResidentialPixelTotal { get; set; }

    [JsonPropertyName("main_road_pixel_total")]
    public int MainRoadPixelTotal { get; set; }

    [JsonPropertyName("greenspace_pixel_total")]
    public int GreenspacePixelTotal { get; set; }

    [JsonPropertyName("back_alley_pixel_total")]
    public int BackAlleyPixelTotal { get; set; }

    [JsonPropertyName("civic_placeholder_pixel_total")]
    public int CivicPlaceholderPixelTotal { get; set; }

    [JsonPropertyName("commercial_pixel_total")]
    public int CommercialPixelTotal { get; set; }

    [JsonPropertyName("ignore_pixel_total")]
    public int IgnorePixelTotal { get; set; }

    [JsonPropertyName("created_geometry_count")]
    public int CreatedGeometryCount { get; set; }

    [JsonPropertyName("writer_ready_component_count")]
    public int WriterReadyComponentCount { get; set; }

    [JsonPropertyName("runtime_validated_component_count")]
    public int RuntimeValidatedComponentCount { get; set; }
}

public sealed class DeadMtlWorldBuilderConnectedComponentExtractionClaimBoundary
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

public sealed class DeadMtlWorldBuilderConnectedComponentExtraction
{
    [JsonPropertyName("format")]
    public string Format { get; set; } =
        "pzmapforge.deadmtl.worldbuilder.connected-component-extraction.v1";

    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("source_png")]
    public string SourcePng { get; set; } = string.Empty;

    [JsonPropertyName("source_zone_metadata_json")]
    public string SourceZoneMetadataJson { get; set; } = string.Empty;

    [JsonPropertyName("source_geometry_primitive_schema_json")]
    public string SourceGeometryPrimitiveSchemaJson { get; set; } = string.Empty;

    [JsonPropertyName("source_mask_region_extraction_json")]
    public string SourceMaskRegionExtractionJson { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "CONNECTED_COMPONENT_EXTRACTION_CONTRACT_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; set; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; set; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("generation_status")]
    public string GenerationStatus { get; set; } = "NOT_EXECUTED";

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = "CONNECTED_COMPONENTS_ONLY_NO_GEOMETRY_CREATED";

    [JsonPropertyName("extraction_status")]
    public string ExtractionStatus { get; set; } = "CONNECTED_COMPONENTS_EXTRACTED";

    [JsonPropertyName("connectivity_status")]
    public string ConnectivityStatus { get; set; } = "FOUR_WAY_PIXEL_CONNECTIVITY";

    [JsonPropertyName("source_contract")]
    public DeadMtlWorldBuilderConnectedComponentSourceContract SourceContract { get; set; } = new();

    [JsonPropertyName("validation_rules")]
    public DeadMtlWorldBuilderConnectedComponentValidationRules ValidationRules { get; set; } = new();

    [JsonPropertyName("connected_components")]
    public List<DeadMtlWorldBuilderConnectedComponentRecord> ConnectedComponents { get; set; } = new();

    [JsonPropertyName("totals")]
    public DeadMtlWorldBuilderConnectedComponentExtractionTotals Totals { get; set; } = new();

    [JsonPropertyName("claim_boundary")]
    public DeadMtlWorldBuilderConnectedComponentExtractionClaimBoundary ClaimBoundary { get; set; } = new();
}

public sealed class DeadMtlWorldBuilderConnectedComponentExtractionResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public DeadMtlWorldBuilderConnectedComponentExtraction Extraction { get; set; } = new();
}
