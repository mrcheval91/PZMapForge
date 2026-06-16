using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderComponentIntentRecord
{
    [JsonPropertyName("intent_order")]
    public int IntentOrder { get; set; }

    [JsonPropertyName("component_id")]
    public string ComponentId { get; set; } = string.Empty;

    [JsonPropertyName("component_order")]
    public int ComponentOrder { get; set; }

    [JsonPropertyName("parent_source_color")]
    public string ParentSourceColor { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("zone_type")]
    public string ZoneType { get; set; } = string.Empty;

    [JsonPropertyName("street_class")]
    public string StreetClass { get; set; } = string.Empty;

    [JsonPropertyName("component_intent")]
    public string ComponentIntent { get; set; } = string.Empty;

    [JsonPropertyName("intent_bucket")]
    public string IntentBucket { get; set; } = string.Empty;

    [JsonPropertyName("intent_family")]
    public string IntentFamily { get; set; } = string.Empty;

    [JsonPropertyName("classification_reason")]
    public string ClassificationReason { get; set; } = string.Empty;

    [JsonPropertyName("classification_confidence")]
    public string ClassificationConfidence { get; set; } = "COMPONENT_COLOR_ROLE_METADATA_MATCH";

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

    [JsonPropertyName("source_component_status")]
    public string SourceComponentStatus { get; set; } = "CONNECTED_PIXEL_COMPONENT_ONLY";

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = "INTENT_ONLY_NO_GEOMETRY_CREATED";

    [JsonPropertyName("future_geometry_requirement_ids")]
    public List<string> FutureGeometryRequirementIds { get; set; } = new();

    [JsonPropertyName("future_consumers")]
    public List<string> FutureConsumers { get; set; } = new();

    [JsonPropertyName("blocked_by_requirements")]
    public List<string> BlockedByRequirements { get; set; } = new();

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderComponentIntentClassificationContract
{
    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("parent_connected_component_count")]
    public int ParentConnectedComponentCount { get; set; }

    [JsonPropertyName("source_component_contract")]
    public string SourceComponentContract { get; set; } = "MAP25K_CONNECTED_COMPONENT_EXTRACTION";

    [JsonPropertyName("classification_input_status")]
    public string ClassificationInputStatus { get; set; } = "CONNECTED_COMPONENTS_ONLY_NO_GEOMETRY_CREATED";

    [JsonPropertyName("classification_output_status")]
    public string ClassificationOutputStatus { get; set; } = "INTENT_BUCKETS_ONLY";

    [JsonPropertyName("component_bounds_are_pixel_bounds_not_geometry")]
    public bool ComponentBoundsArePixelBoundsNotGeometry { get; set; } = true;

    [JsonPropertyName("intent_records_are_not_geometry")]
    public bool IntentRecordsAreNotGeometry { get; set; } = true;

    [JsonPropertyName("geometry_must_be_created_by_future_step")]
    public bool GeometryMustBeCreatedByFutureStep { get; set; } = true;
}

public sealed class DeadMtlWorldBuilderComponentIntentClassificationValidationRules
{
    [JsonPropertyName("connected_component_extraction_must_exist")]
    public bool ConnectedComponentExtractionMustExist { get; set; } = true;

    [JsonPropertyName("connected_component_count_must_equal_45")]
    public bool ConnectedComponentCountMustEqual45 { get; set; } = true;

    [JsonPropertyName("all_components_must_receive_intent")]
    public bool AllComponentsMustReceiveIntent { get; set; } = true;

    [JsonPropertyName("unclassified_component_count_must_equal_zero")]
    public bool UnclassifiedComponentCountMustEqualZero { get; set; } = true;

    [JsonPropertyName("intent_pixel_total_must_equal_component_pixel_total")]
    public bool IntentPixelTotalMustEqualComponentPixelTotal { get; set; } = true;

    [JsonPropertyName("intent_bounds_must_remain_pixel_bounds")]
    public bool IntentBoundsMustRemainPixelBounds { get; set; } = true;

    [JsonPropertyName("intent_records_are_not_geometry")]
    public bool IntentRecordsAreNotGeometry { get; set; } = true;

    [JsonPropertyName("no_runtime_claim_from_intent_classification")]
    public bool NoRuntimeClaimFromIntentClassification { get; set; } = true;

    [JsonPropertyName("no_writer_claim_from_intent_classification")]
    public bool NoWriterClaimFromIntentClassification { get; set; } = true;

    [JsonPropertyName("no_materialization_from_intent_classification")]
    public bool NoMaterializationFromIntentClassification { get; set; } = true;
}

public sealed class DeadMtlWorldBuilderComponentIntentClassificationTotals
{
    [JsonPropertyName("parent_connected_component_count")]
    public int ParentConnectedComponentCount { get; set; }

    [JsonPropertyName("classified_component_count")]
    public int ClassifiedComponentCount { get; set; }

    [JsonPropertyName("unclassified_component_count")]
    public int UnclassifiedComponentCount { get; set; }

    [JsonPropertyName("intent_bucket_count")]
    public int IntentBucketCount { get; set; }

    [JsonPropertyName("classified_pixel_total")]
    public int ClassifiedPixelTotal { get; set; }

    [JsonPropertyName("residential_lot_block_count")]
    public int ResidentialLotBlockCount { get; set; }

    [JsonPropertyName("commercial_lot_block_count")]
    public int CommercialLotBlockCount { get; set; }

    [JsonPropertyName("main_road_corridor_count")]
    public int MainRoadCorridorCount { get; set; }

    [JsonPropertyName("back_alley_corridor_count")]
    public int BackAlleyCorridorCount { get; set; }

    [JsonPropertyName("greenspace_mass_count")]
    public int GreenspaceMassCount { get; set; }

    [JsonPropertyName("civic_placeholder_count")]
    public int CivicPlaceholderCount { get; set; }

    [JsonPropertyName("ignore_border_count")]
    public int IgnoreBorderCount { get; set; }

    [JsonPropertyName("residential_lot_block_pixel_total")]
    public int ResidentialLotBlockPixelTotal { get; set; }

    [JsonPropertyName("commercial_lot_block_pixel_total")]
    public int CommercialLotBlockPixelTotal { get; set; }

    [JsonPropertyName("main_road_corridor_pixel_total")]
    public int MainRoadCorridorPixelTotal { get; set; }

    [JsonPropertyName("back_alley_corridor_pixel_total")]
    public int BackAlleyCorridorPixelTotal { get; set; }

    [JsonPropertyName("greenspace_mass_pixel_total")]
    public int GreenspaceMassPixelTotal { get; set; }

    [JsonPropertyName("civic_placeholder_pixel_total")]
    public int CivicPlaceholderPixelTotal { get; set; }

    [JsonPropertyName("ignore_border_pixel_total")]
    public int IgnoreBorderPixelTotal { get; set; }

    [JsonPropertyName("created_geometry_count")]
    public int CreatedGeometryCount { get; set; }

    [JsonPropertyName("writer_ready_intent_count")]
    public int WriterReadyIntentCount { get; set; }

    [JsonPropertyName("runtime_validated_intent_count")]
    public int RuntimeValidatedIntentCount { get; set; }

    [JsonPropertyName("materialized_intent_count")]
    public int MaterializedIntentCount { get; set; }
}

public sealed class DeadMtlWorldBuilderComponentIntentClassificationClaimBoundary
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

public sealed class DeadMtlWorldBuilderComponentIntentClassification
{
    [JsonPropertyName("format")]
    public string Format { get; set; } =
        "pzmapforge.deadmtl.worldbuilder.component-intent-classification.v1";

    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("source_zone_metadata_json")]
    public string SourceZoneMetadataJson { get; set; } = string.Empty;

    [JsonPropertyName("source_mask_region_extraction_json")]
    public string SourceMaskRegionExtractionJson { get; set; } = string.Empty;

    [JsonPropertyName("source_connected_component_extraction_json")]
    public string SourceConnectedComponentExtractionJson { get; set; } = string.Empty;

    [JsonPropertyName("source_geometry_primitive_schema_json")]
    public string SourceGeometryPrimitiveSchemaJson { get; set; } = string.Empty;

    [JsonPropertyName("source_concrete_geometry_preflight_json")]
    public string SourceConcreteGeometryPreflightJson { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "COMPONENT_INTENT_CLASSIFICATION_CONTRACT_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; set; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; set; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("generation_status")]
    public string GenerationStatus { get; set; } = "NOT_EXECUTED";

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = "INTENT_CLASSIFICATION_ONLY_NO_GEOMETRY_CREATED";

    [JsonPropertyName("classification_status")]
    public string ClassificationStatus { get; set; } = "COMPONENT_INTENTS_CLASSIFIED";

    [JsonPropertyName("materialization_status")]
    public string MaterializationStatus { get; set; } = "NOT_MATERIALIZED";

    [JsonPropertyName("classification_contract")]
    public DeadMtlWorldBuilderComponentIntentClassificationContract ClassificationContract { get; set; } = new();

    [JsonPropertyName("validation_rules")]
    public DeadMtlWorldBuilderComponentIntentClassificationValidationRules ValidationRules { get; set; } = new();

    [JsonPropertyName("intent_records")]
    public List<DeadMtlWorldBuilderComponentIntentRecord> IntentRecords { get; set; } = new();

    [JsonPropertyName("totals")]
    public DeadMtlWorldBuilderComponentIntentClassificationTotals Totals { get; set; } = new();

    [JsonPropertyName("claim_boundary")]
    public DeadMtlWorldBuilderComponentIntentClassificationClaimBoundary ClaimBoundary { get; set; } = new();
}

public sealed class DeadMtlWorldBuilderComponentIntentClassificationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public DeadMtlWorldBuilderComponentIntentClassification Classification { get; set; } = new();
}
