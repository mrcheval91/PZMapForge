using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderGeometryPrimitiveType
{
    [JsonPropertyName("primitive_type")]
    public string PrimitiveType { get; set; } = string.Empty;

    [JsonPropertyName("geometry_class")]
    public string GeometryClass { get; set; } = string.Empty;

    [JsonPropertyName("coordinate_fields")]
    public List<string> CoordinateFields { get; set; } = new();

    [JsonPropertyName("required_fields")]
    public List<string> RequiredFields { get; set; } = new();

    [JsonPropertyName("optional_fields")]
    public List<string> OptionalFields { get; set; } = new();

    [JsonPropertyName("allowed_source_requirements")]
    public List<string> AllowedSourceRequirements { get; set; } = new();

    [JsonPropertyName("allowed_future_consumers")]
    public List<string> AllowedFutureConsumers { get; set; } = new();

    [JsonPropertyName("creation_status")]
    public string CreationStatus { get; set; } = "NOT_CREATED";

    [JsonPropertyName("validation_rules")]
    public List<string> ValidationRules { get; set; } = new();

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderGeometryPrimitiveCoordinateContract
{
    [JsonPropertyName("map_id")]
    public string MapId { get; set; } = string.Empty;

    [JsonPropertyName("source_tile_width_px")]
    public int SourceTileWidthPx { get; set; }

    [JsonPropertyName("source_tile_height_px")]
    public int SourceTileHeightPx { get; set; }

    [JsonPropertyName("authoring_scale")]
    public string AuthoringScale { get; set; } = string.Empty;

    [JsonPropertyName("pixel_origin")]
    public string PixelOrigin { get; set; } = string.Empty;

    [JsonPropertyName("tile_origin")]
    public string TileOrigin { get; set; } = string.Empty;

    [JsonPropertyName("x_axis")]
    public string XAxis { get; set; } = string.Empty;

    [JsonPropertyName("y_axis")]
    public string YAxis { get; set; } = string.Empty;

    [JsonPropertyName("z_axis")]
    public string ZAxis { get; set; } = string.Empty;

    [JsonPropertyName("default_z")]
    public int DefaultZ { get; set; }

    [JsonPropertyName("coordinate_units")]
    public string CoordinateUnits { get; set; } = string.Empty;

    [JsonPropertyName("coordinate_precision")]
    public string CoordinatePrecision { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderGeometryPrimitiveValidationRules
{
    [JsonPropertyName("coordinates_must_be_integer_first_pass")]
    public bool CoordinatesMustBeIntegerFirstPass { get; set; }

    [JsonPropertyName("coordinates_must_be_within_tile_bounds")]
    public bool CoordinatesMustBeWithinTileBounds { get; set; }

    [JsonPropertyName("tile_bounds_min_x")]
    public int TileBoundsMinX { get; set; }

    [JsonPropertyName("tile_bounds_min_y")]
    public int TileBoundsMinY { get; set; }

    [JsonPropertyName("tile_bounds_max_x")]
    public int TileBoundsMaxX { get; set; }

    [JsonPropertyName("tile_bounds_max_y")]
    public int TileBoundsMaxY { get; set; }

    [JsonPropertyName("width_must_be_positive")]
    public bool WidthMustBePositive { get; set; }

    [JsonPropertyName("height_must_be_positive")]
    public bool HeightMustBePositive { get; set; }

    [JsonPropertyName("polygon_outer_ring_min_points")]
    public int PolygonOuterRingMinPoints { get; set; }

    [JsonPropertyName("polyline_min_points")]
    public int PolylineMinPoints { get; set; }

    [JsonPropertyName("no_runtime_claim_from_schema")]
    public bool NoRuntimeClaimFromSchema { get; set; }

    [JsonPropertyName("no_writer_claim_from_schema")]
    public bool NoWriterClaimFromSchema { get; set; }

    [JsonPropertyName("no_materialization_from_schema")]
    public bool NoMaterializationFromSchema { get; set; }
}

public sealed class DeadMtlWorldBuilderGeometryPrimitiveSchemaTotals
{
    [JsonPropertyName("primitive_type_count")]
    public int PrimitiveTypeCount { get; set; }

    [JsonPropertyName("coordinate_primitive_count")]
    public int CoordinatePrimitiveCount { get; set; }

    [JsonPropertyName("linear_primitive_count")]
    public int LinearPrimitiveCount { get; set; }

    [JsonPropertyName("area_primitive_count")]
    public int AreaPrimitiveCount { get; set; }

    [JsonPropertyName("derived_area_primitive_count")]
    public int DerivedAreaPrimitiveCount { get; set; }

    [JsonPropertyName("placement_primitive_count")]
    public int PlacementPrimitiveCount { get; set; }

    [JsonPropertyName("source_region_primitive_count")]
    public int SourceRegionPrimitiveCount { get; set; }

    [JsonPropertyName("created_geometry_count")]
    public int CreatedGeometryCount { get; set; }

    [JsonPropertyName("writer_ready_primitive_count")]
    public int WriterReadyPrimitiveCount { get; set; }

    [JsonPropertyName("runtime_validated_primitive_count")]
    public int RuntimeValidatedPrimitiveCount { get; set; }
}

public sealed class DeadMtlWorldBuilderGeometryPrimitiveSchemaClaimBoundary
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

public sealed class DeadMtlWorldBuilderGeometryPrimitiveSchema
{
    [JsonPropertyName("format")]
    public string Format { get; set; } =
        "pzmapforge.deadmtl.worldbuilder.geometry-primitive-schema.v1";

    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("source_profile_json")]
    public string SourceProfileJson { get; set; } = string.Empty;

    [JsonPropertyName("source_zone_metadata_json")]
    public string SourceZoneMetadataJson { get; set; } = string.Empty;

    [JsonPropertyName("source_future_world_layout_plan_json")]
    public string SourceFutureWorldLayoutPlanJson { get; set; } = string.Empty;

    [JsonPropertyName("source_concrete_geometry_preflight_json")]
    public string SourceConcreteGeometryPreflightJson { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "GEOMETRY_PRIMITIVE_SCHEMA_CONTRACT_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; set; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; set; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("generation_status")]
    public string GenerationStatus { get; set; } = "NOT_EXECUTED";

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = "SCHEMA_ONLY_NO_GEOMETRY_CREATED";

    [JsonPropertyName("schema_status")]
    public string SchemaStatus { get; set; } = "PRIMITIVE_TYPES_DEFINED";

    [JsonPropertyName("coordinate_contract")]
    public DeadMtlWorldBuilderGeometryPrimitiveCoordinateContract CoordinateContract { get; set; } = new();

    [JsonPropertyName("validation_rules")]
    public DeadMtlWorldBuilderGeometryPrimitiveValidationRules ValidationRules { get; set; } = new();

    [JsonPropertyName("primitive_types")]
    public List<DeadMtlWorldBuilderGeometryPrimitiveType> PrimitiveTypes { get; set; } = new();

    [JsonPropertyName("totals")]
    public DeadMtlWorldBuilderGeometryPrimitiveSchemaTotals Totals { get; set; } = new();

    [JsonPropertyName("claim_boundary")]
    public DeadMtlWorldBuilderGeometryPrimitiveSchemaClaimBoundary ClaimBoundary { get; set; } = new();
}

public sealed class DeadMtlWorldBuilderGeometryPrimitiveSchemaResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public DeadMtlWorldBuilderGeometryPrimitiveSchema Schema { get; set; } = new();
}
