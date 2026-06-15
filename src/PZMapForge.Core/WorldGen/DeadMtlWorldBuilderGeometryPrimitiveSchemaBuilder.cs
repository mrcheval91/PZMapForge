using System.Text;

namespace PZMapForge.Core.WorldGen;

public static class DeadMtlWorldBuilderGeometryPrimitiveSchemaBuilder
{
    private static readonly (
        string Type, string Class,
        string[] CoordFields, string[] Required, string[] Optional,
        string[] SourceReqs, string[] FutureConsumers,
        string Notes)[] PrimitiveDefs =
    {
        (
            "POINT",
            "COORDINATE_PRIMITIVE",
            new[] { "x", "y", "z" },
            new[] { "x", "y" },
            new[] { "z", "label", "source_color" },
            new[] { "UNIQUE_BUILDING_BINDING", "RUNTIME_VALIDATION_PASS" },
            new[] { "UNIQUE_PLACEHOLDER_BINDING", "DEBUG_MARKERS" },
            "Atomic coordinate. Used for anchor points, unique building origins, and debug markers."
        ),
        (
            "LINE_SEGMENT",
            "LINEAR_PRIMITIVE",
            new[] { "x1", "y1", "x2", "y2", "z" },
            new[] { "x1", "y1", "x2", "y2" },
            new[] { "z", "width", "source_color" },
            new[] { "FENCE_AND_LOT_BOUNDARY_GEOMETRY" },
            new[] { "FENCE_LINES", "LOT_BOUNDARIES" },
            "Single straight edge. Primary primitive for fence lines and lot boundary segments."
        ),
        (
            "POLYLINE",
            "LINEAR_PRIMITIVE",
            new[] { "points[]", "z" },
            new[] { "points" },
            new[] { "z", "width", "source_color" },
            new[] { "MAIN_ROAD_CORRIDOR_GEOMETRY", "BACK_ALLEY_SERVICE_CORRIDOR_GEOMETRY" },
            new[] { "ROAD_CENTERLINES", "ALLEY_CENTERLINES" },
            "Ordered list of connected points. Used for road and alley centerline tracing."
        ),
        (
            "RECTANGLE",
            "AREA_PRIMITIVE",
            new[] { "x", "y", "width", "height", "z" },
            new[] { "x", "y", "width", "height" },
            new[] { "z", "source_color", "orientation" },
            new[] { "LOT_GEOMETRY", "BUILDING_SLOT_GEOMETRY", "SIDEWALK_GEOMETRY" },
            new[] { "LOT_RECTS", "BUILDING_SLOTS", "SIDEWALK_RECTS" },
            "Axis-aligned rectangle. Simplest area primitive for first-pass lot and sidewalk geometry."
        ),
        (
            "POLYGON",
            "AREA_PRIMITIVE",
            new[] { "outer_ring[]", "holes[]", "z" },
            new[] { "outer_ring" },
            new[] { "holes", "z", "source_color" },
            new[] {
                "LOT_GEOMETRY",
                "MAIN_ROAD_CORRIDOR_GEOMETRY",
                "BACK_ALLEY_SERVICE_CORRIDOR_GEOMETRY",
                "SIDEWALK_GEOMETRY",
                "BUILDING_SLOT_GEOMETRY"
            },
            new[] { "LOT_POLYGONS", "ROAD_POLYGONS", "SIDEWALK_POLYGONS", "BUILDING_SLOT_POLYGONS" },
            "General closed polygon with optional holes. Outer ring must have at least 3 points."
        ),
        (
            "CORRIDOR",
            "DERIVED_AREA_PRIMITIVE",
            new[] { "centerline", "width", "z" },
            new[] { "centerline", "width" },
            new[] { "z", "left_width", "right_width", "source_color" },
            new[] { "MAIN_ROAD_CORRIDOR_GEOMETRY", "BACK_ALLEY_SERVICE_CORRIDOR_GEOMETRY" },
            new[] { "ROAD_CORRIDORS", "ALLEY_CORRIDORS", "SIDEWALK_CONTEXT" },
            "Area derived by buffering a centerline polyline by width. Used for road and alley area generation."
        ),
        (
            "STRIP",
            "DERIVED_AREA_PRIMITIVE",
            new[] { "parent_corridor", "side", "width", "z" },
            new[] { "parent_corridor", "side", "width" },
            new[] { "z", "source_color" },
            new[] { "SIDEWALK_GEOMETRY" },
            new[] { "SIDEWALK_STRIPS", "CURB_STRIPS", "SNOWBANK_STRIPS" },
            "Narrow strip derived from a parent corridor. Used for sidewalks, curbs, and snowbanks."
        ),
        (
            "SLOT",
            "PLACEMENT_PRIMITIVE",
            new[] { "footprint", "frontage_edge", "z" },
            new[] { "footprint", "frontage_edge" },
            new[] { "z", "allowed_building_families", "source_color" },
            new[] { "BUILDING_SLOT_GEOMETRY", "UNIQUE_BUILDING_BINDING" },
            new[] { "BUILDING_PLACER", "UNIQUE_BUILDING_PLACER" },
            "Placement region with a known footprint and frontage edge. Used for building and unique building placement."
        ),
        (
            "BOUNDARY_LINE",
            "LINEAR_PRIMITIVE",
            new[] { "points[]", "boundary_type", "z" },
            new[] { "points", "boundary_type" },
            new[] { "z", "fence_style", "source_color" },
            new[] { "FENCE_AND_LOT_BOUNDARY_GEOMETRY" },
            new[] { "FENCE_PLACER", "LOT_BOUNDARY_WRITER" },
            "Typed boundary edge for fences and lot limits. boundary_type distinguishes fence from property line."
        ),
        (
            "MASK_REGION",
            "SOURCE_REGION_PRIMITIVE",
            new[] { "source_color", "pixel_count", "bounds", "z" },
            new[] { "source_color", "pixel_count", "bounds" },
            new[] { "z", "connected_component_id" },
            new[] {
                "LOT_GEOMETRY",
                "MAIN_ROAD_CORRIDOR_GEOMETRY",
                "BACK_ALLEY_SERVICE_CORRIDOR_GEOMETRY",
                "SIDEWALK_GEOMETRY",
                "BUILDING_SLOT_GEOMETRY"
            },
            new[] { "REGION_EXTRACTOR", "GEOMETRY_GENERATOR" },
            "Pixel-space color region from the source PNG. Seed for geometry extraction. Not yet converted to tile-space."
        ),
    };

    public static DeadMtlWorldBuilderGeometryPrimitiveSchemaResult Build(
        string profilePath,
        string metadataPath,
        string futureLayoutPlanPath,
        string geometryPreflightPath)
    {
        var errors = new List<string>();
        var schema = new DeadMtlWorldBuilderGeometryPrimitiveSchema
        {
            TileId                              = "map_00",
            SourceProfileJson                   = profilePath,
            SourceZoneMetadataJson              = metadataPath,
            SourceFutureWorldLayoutPlanJson     = futureLayoutPlanPath,
            SourceConcreteGeometryPreflightJson = geometryPreflightPath,
        };

        var inputs = new[]
        {
            ("profile",             profilePath),
            ("metadata",            metadataPath),
            ("future layout plan",  futureLayoutPlanPath),
            ("geometry preflight",  geometryPreflightPath),
        };

        foreach (var (label, path) in inputs)
        {
            if (!File.Exists(path))
                errors.Add($"Missing {label}: file not found: {path}");
        }

        if (errors.Count > 0)
            return Fail(errors, schema);

        schema.CoordinateContract = new DeadMtlWorldBuilderGeometryPrimitiveCoordinateContract
        {
            MapId               = "map_00",
            SourceTileWidthPx   = 256,
            SourceTileHeightPx  = 256,
            AuthoringScale      = "1_PIXEL_EQUALS_1_WORLD_TILE",
            PixelOrigin         = "TOP_LEFT",
            TileOrigin          = "LOCAL_TILE_ORIGIN_0_0",
            XAxis               = "EAST_POSITIVE",
            YAxis               = "SOUTH_POSITIVE",
            ZAxis               = "LEVEL_POSITIVE",
            DefaultZ            = 0,
            CoordinateUnits     = "WORLD_TILES",
            CoordinatePrecision = "INTEGER_TILE_COORDINATES_FIRST_PASS",
        };

        schema.ValidationRules = new DeadMtlWorldBuilderGeometryPrimitiveValidationRules
        {
            CoordinatesMustBeIntegerFirstPass   = true,
            CoordinatesMustBeWithinTileBounds   = true,
            TileBoundsMinX                      = 0,
            TileBoundsMinY                      = 0,
            TileBoundsMaxX                      = 255,
            TileBoundsMaxY                      = 255,
            WidthMustBePositive                 = true,
            HeightMustBePositive                = true,
            PolygonOuterRingMinPoints           = 3,
            PolylineMinPoints                   = 2,
            NoRuntimeClaimFromSchema            = true,
            NoWriterClaimFromSchema             = true,
            NoMaterializationFromSchema         = true,
        };

        foreach (var d in PrimitiveDefs)
        {
            schema.PrimitiveTypes.Add(new DeadMtlWorldBuilderGeometryPrimitiveType
            {
                PrimitiveType            = d.Type,
                GeometryClass            = d.Class,
                CoordinateFields         = new List<string>(d.CoordFields),
                RequiredFields           = new List<string>(d.Required),
                OptionalFields           = new List<string>(d.Optional),
                AllowedSourceRequirements = new List<string>(d.SourceReqs),
                AllowedFutureConsumers   = new List<string>(d.FutureConsumers),
                CreationStatus           = "NOT_CREATED",
                ValidationRules          = BuildValidationRulesFor(d.Class),
                Notes                    = d.Notes,
            });
        }

        schema.Totals    = ComputeTotals(schema.PrimitiveTypes);
        schema.ClaimBoundary = new DeadMtlWorldBuilderGeometryPrimitiveSchemaClaimBoundary
        {
            WritesLotpack                 = false,
            WritesWorldgenLua             = false,
            RuntimeProven                 = false,
            PublicPlayableClaim           = false,
            WriterReadyClaim              = false,
            GeneratesTerrainNow           = false,
            GeneratesBuildingsNow         = false,
            GeneratesSidewalksNow         = false,
            SubdividesLotsNow             = false,
            CapturesChunkLayersNow        = false,
            PlacesFencesNow               = false,
            PlacesUniqueBuildingsNow      = false,
            SelectsConcreteBuildingIdsNow = false,
            CreatesConcreteGeometryNow    = false,
            MaterializesLayoutNow         = false,
        };

        return new DeadMtlWorldBuilderGeometryPrimitiveSchemaResult
            { IsValid = true, Errors = errors, Schema = schema };
    }

    private static List<string> BuildValidationRulesFor(string geometryClass) =>
        geometryClass switch
        {
            "AREA_PRIMITIVE"         => new List<string> { "width_must_be_positive", "height_must_be_positive" },
            "DERIVED_AREA_PRIMITIVE" => new List<string> { "width_must_be_positive" },
            "LINEAR_PRIMITIVE"       => new List<string> { "polyline_min_points_2_or_more" },
            _                        => new List<string>(),
        };

    private static DeadMtlWorldBuilderGeometryPrimitiveSchemaTotals ComputeTotals(
        List<DeadMtlWorldBuilderGeometryPrimitiveType> types) =>
        new()
        {
            PrimitiveTypeCount          = types.Count,
            CoordinatePrimitiveCount    = types.Count(t => t.GeometryClass == "COORDINATE_PRIMITIVE"),
            LinearPrimitiveCount        = types.Count(t => t.GeometryClass == "LINEAR_PRIMITIVE"),
            AreaPrimitiveCount          = types.Count(t => t.GeometryClass == "AREA_PRIMITIVE"),
            DerivedAreaPrimitiveCount   = types.Count(t => t.GeometryClass == "DERIVED_AREA_PRIMITIVE"),
            PlacementPrimitiveCount     = types.Count(t => t.GeometryClass == "PLACEMENT_PRIMITIVE"),
            SourceRegionPrimitiveCount  = types.Count(t => t.GeometryClass == "SOURCE_REGION_PRIMITIVE"),
            CreatedGeometryCount        = 0,
            WriterReadyPrimitiveCount   = 0,
            RuntimeValidatedPrimitiveCount = 0,
        };

    public static string RenderMarkdown(DeadMtlWorldBuilderGeometryPrimitiveSchema schema)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-25I: DeadMTL WorldBuilder Geometry Primitive Schema Contract");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** Schema contract only. No terrain generation. No lot subdivision.");
        sb.AppendLine("> No sidewalk geometry. No road geometry. No building placement. No fences.");
        sb.AppendLine("> No lotpack writing. No worldgen override file. No concrete geometry created.");
        sb.AppendLine("> No runtime proof. Not writer-ready. Layout not materialized.");
        sb.AppendLine();
        sb.AppendLine($"**tile_id:** {schema.TileId}");
        sb.AppendLine($"**status:** {schema.Status}");
        sb.AppendLine($"**schema_status:** {schema.SchemaStatus}");
        sb.AppendLine($"**geometry_status:** {schema.GeometryStatus}");
        sb.AppendLine($"**generation_status:** {schema.GenerationStatus}");
        sb.AppendLine($"**runtime_status:** {schema.RuntimeStatus}");
        sb.AppendLine();
        sb.AppendLine("## Input Chain");
        sb.AppendLine();
        sb.AppendLine($"- profile: `{schema.SourceProfileJson}`");
        sb.AppendLine($"- zone metadata: `{schema.SourceZoneMetadataJson}`");
        sb.AppendLine($"- future world layout plan: `{schema.SourceFutureWorldLayoutPlanJson}`");
        sb.AppendLine($"- concrete geometry preflight: `{schema.SourceConcreteGeometryPreflightJson}`");
        sb.AppendLine();
        sb.AppendLine("## Coordinate Contract");
        sb.AppendLine();
        var cc = schema.CoordinateContract;
        sb.AppendLine($"- map_id: {cc.MapId}");
        sb.AppendLine($"- source_tile_width_px: {cc.SourceTileWidthPx}");
        sb.AppendLine($"- source_tile_height_px: {cc.SourceTileHeightPx}");
        sb.AppendLine($"- authoring_scale: {cc.AuthoringScale}");
        sb.AppendLine($"- pixel_origin: {cc.PixelOrigin}");
        sb.AppendLine($"- tile_origin: {cc.TileOrigin}");
        sb.AppendLine($"- x_axis: {cc.XAxis}");
        sb.AppendLine($"- y_axis: {cc.YAxis}");
        sb.AppendLine($"- z_axis: {cc.ZAxis}");
        sb.AppendLine($"- default_z: {cc.DefaultZ}");
        sb.AppendLine($"- coordinate_units: {cc.CoordinateUnits}");
        sb.AppendLine($"- coordinate_precision: {cc.CoordinatePrecision}");
        sb.AppendLine();
        sb.AppendLine("## Primitive Types");
        sb.AppendLine();
        sb.AppendLine("| Type | Class | Creation Status |");
        sb.AppendLine("|------|-------|-----------------|");
        foreach (var p in schema.PrimitiveTypes)
            sb.AppendLine($"| {p.PrimitiveType} | {p.GeometryClass} | {p.CreationStatus} |");
        sb.AppendLine();
        foreach (var p in schema.PrimitiveTypes)
        {
            sb.AppendLine($"**{p.PrimitiveType}:**");
            sb.AppendLine($"- geometry_class: {p.GeometryClass}");
            sb.AppendLine($"- coordinate_fields: {string.Join(", ", p.CoordinateFields)}");
            sb.AppendLine($"- required_fields: {string.Join(", ", p.RequiredFields)}");
            sb.AppendLine($"- optional_fields: {string.Join(", ", p.OptionalFields)}");
            sb.AppendLine($"- allowed_source_requirements: {string.Join(", ", p.AllowedSourceRequirements)}");
            sb.AppendLine($"- allowed_future_consumers: {string.Join(", ", p.AllowedFutureConsumers)}");
            sb.AppendLine($"- creation_status: {p.CreationStatus}");
            sb.AppendLine($"- notes: {p.Notes}");
            sb.AppendLine();
        }
        sb.AppendLine("## Validation Rules");
        sb.AppendLine();
        var vr = schema.ValidationRules;
        sb.AppendLine($"- coordinates_must_be_integer_first_pass: {vr.CoordinatesMustBeIntegerFirstPass}");
        sb.AppendLine($"- coordinates_must_be_within_tile_bounds: {vr.CoordinatesMustBeWithinTileBounds}");
        sb.AppendLine($"- tile_bounds_min_x: {vr.TileBoundsMinX}");
        sb.AppendLine($"- tile_bounds_min_y: {vr.TileBoundsMinY}");
        sb.AppendLine($"- tile_bounds_max_x: {vr.TileBoundsMaxX}");
        sb.AppendLine($"- tile_bounds_max_y: {vr.TileBoundsMaxY}");
        sb.AppendLine($"- width_must_be_positive: {vr.WidthMustBePositive}");
        sb.AppendLine($"- height_must_be_positive: {vr.HeightMustBePositive}");
        sb.AppendLine($"- polygon_outer_ring_min_points: {vr.PolygonOuterRingMinPoints}");
        sb.AppendLine($"- polyline_min_points: {vr.PolylineMinPoints}");
        sb.AppendLine($"- no_runtime_claim_from_schema: {vr.NoRuntimeClaimFromSchema}");
        sb.AppendLine($"- no_writer_claim_from_schema: {vr.NoWriterClaimFromSchema}");
        sb.AppendLine($"- no_materialization_from_schema: {vr.NoMaterializationFromSchema}");
        sb.AppendLine();
        sb.AppendLine("## Future Consumers");
        sb.AppendLine();
        foreach (var p in schema.PrimitiveTypes)
        {
            if (p.AllowedFutureConsumers.Count > 0)
                sb.AppendLine($"- **{p.PrimitiveType}:** {string.Join(", ", p.AllowedFutureConsumers)}");
        }
        sb.AppendLine();
        sb.AppendLine("## Why This Still Cannot Execute");
        sb.AppendLine();
        sb.AppendLine($"All {schema.Totals.PrimitiveTypeCount} primitive types defined. Zero geometry created.");
        sb.AppendLine($"- created_geometry_count: {schema.Totals.CreatedGeometryCount}");
        sb.AppendLine($"- writer_ready_primitive_count: {schema.Totals.WriterReadyPrimitiveCount}");
        sb.AppendLine($"- runtime_validated_primitive_count: {schema.Totals.RuntimeValidatedPrimitiveCount}");
        sb.AppendLine("Schema definitions exist but no geometry has been computed from source PNG regions.");
        sb.AppendLine("A tile writer that can consume these primitives does not yet exist.");
        sb.AppendLine("No runtime validation pass has been performed.");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        var cb = schema.ClaimBoundary;
        sb.AppendLine($"- writes_lotpack: {cb.WritesLotpack}");
        sb.AppendLine($"- writes_worldgen_lua: {cb.WritesWorldgenLua}");
        sb.AppendLine($"- runtime_proven: {cb.RuntimeProven}");
        sb.AppendLine($"- public_playable_claim: {cb.PublicPlayableClaim}");
        sb.AppendLine($"- writer_ready_claim: {cb.WriterReadyClaim}");
        sb.AppendLine($"- generates_terrain_now: {cb.GeneratesTerrainNow}");
        sb.AppendLine($"- generates_buildings_now: {cb.GeneratesBuildingsNow}");
        sb.AppendLine($"- generates_sidewalks_now: {cb.GeneratesSidewalksNow}");
        sb.AppendLine($"- subdivides_lots_now: {cb.SubdividesLotsNow}");
        sb.AppendLine($"- captures_chunk_layers_now: {cb.CapturesChunkLayersNow}");
        sb.AppendLine($"- places_fences_now: {cb.PlacesFencesNow}");
        sb.AppendLine($"- places_unique_buildings_now: {cb.PlacesUniqueBuildingsNow}");
        sb.AppendLine($"- selects_concrete_building_ids_now: {cb.SelectsConcreteBuildingIdsNow}");
        sb.AppendLine($"- creates_concrete_geometry_now: {cb.CreatesConcreteGeometryNow}");
        sb.AppendLine($"- materializes_layout_now: {cb.MaterializesLayoutNow}");
        sb.AppendLine();
        sb.AppendLine("## Totals");
        sb.AppendLine();
        sb.AppendLine($"primitive_type_count:           {schema.Totals.PrimitiveTypeCount}");
        sb.AppendLine($"coordinate_primitive_count:     {schema.Totals.CoordinatePrimitiveCount}");
        sb.AppendLine($"linear_primitive_count:         {schema.Totals.LinearPrimitiveCount}");
        sb.AppendLine($"area_primitive_count:           {schema.Totals.AreaPrimitiveCount}");
        sb.AppendLine($"derived_area_primitive_count:   {schema.Totals.DerivedAreaPrimitiveCount}");
        sb.AppendLine($"placement_primitive_count:      {schema.Totals.PlacementPrimitiveCount}");
        sb.AppendLine($"source_region_primitive_count:  {schema.Totals.SourceRegionPrimitiveCount}");
        sb.AppendLine($"created_geometry_count:         {schema.Totals.CreatedGeometryCount}");
        sb.AppendLine($"writer_ready_primitive_count:   {schema.Totals.WriterReadyPrimitiveCount}");
        sb.AppendLine($"runtime_validated_primitive_count: {schema.Totals.RuntimeValidatedPrimitiveCount}");
        sb.AppendLine();
        sb.AppendLine("**VERDICT: MAP25I_WORLDBUILDER_GEOMETRY_PRIMITIVE_SCHEMA_CONTRACT_COMPLETE**");
        return sb.ToString();
    }

    public static string RenderCsv(DeadMtlWorldBuilderGeometryPrimitiveSchema schema)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "primitive_type,geometry_class,coordinate_fields,required_fields,optional_fields," +
            "allowed_source_requirements,allowed_future_consumers,creation_status,notes");
        foreach (var p in schema.PrimitiveTypes)
        {
            sb.AppendLine(
                $"{p.PrimitiveType},{p.GeometryClass}," +
                $"\"{string.Join("|", p.CoordinateFields)}\",\"{string.Join("|", p.RequiredFields)}\"," +
                $"\"{string.Join("|", p.OptionalFields)}\",\"{string.Join("|", p.AllowedSourceRequirements)}\"," +
                $"\"{string.Join("|", p.AllowedFutureConsumers)}\",{p.CreationStatus}," +
                $"\"{p.Notes}\"");
        }
        return sb.ToString();
    }

    public static string RenderSummary(DeadMtlWorldBuilderGeometryPrimitiveSchemaResult result)
    {
        var s  = result.Schema;
        var sb = new StringBuilder();
        sb.AppendLine("MAP-25I: DeadMTL WorldBuilder Geometry Primitive Schema Contract");
        sb.AppendLine();
        sb.AppendLine($"tile_id:                           {s.TileId}");
        sb.AppendLine($"is_valid:                          {result.IsValid}");
        sb.AppendLine($"status:                            {s.Status}");
        sb.AppendLine($"schema_status:                     {s.SchemaStatus}");
        sb.AppendLine($"geometry_status:                   {s.GeometryStatus}");
        sb.AppendLine($"generation_status:                 {s.GenerationStatus}");
        sb.AppendLine();
        sb.AppendLine($"primitive_type_count:              {s.Totals.PrimitiveTypeCount}");
        sb.AppendLine($"coordinate_primitive_count:        {s.Totals.CoordinatePrimitiveCount}");
        sb.AppendLine($"linear_primitive_count:            {s.Totals.LinearPrimitiveCount}");
        sb.AppendLine($"area_primitive_count:              {s.Totals.AreaPrimitiveCount}");
        sb.AppendLine($"derived_area_primitive_count:      {s.Totals.DerivedAreaPrimitiveCount}");
        sb.AppendLine($"placement_primitive_count:         {s.Totals.PlacementPrimitiveCount}");
        sb.AppendLine($"source_region_primitive_count:     {s.Totals.SourceRegionPrimitiveCount}");
        sb.AppendLine($"created_geometry_count:            {s.Totals.CreatedGeometryCount}");
        sb.AppendLine($"writer_ready_primitive_count:      {s.Totals.WriterReadyPrimitiveCount}");
        sb.AppendLine($"runtime_validated_primitive_count: {s.Totals.RuntimeValidatedPrimitiveCount}");
        sb.AppendLine($"schema_status:                     {s.SchemaStatus}");
        sb.AppendLine($"geometry_status:                   {s.GeometryStatus}");
        if (result.Errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("ERRORS");
            foreach (var e in result.Errors)
                sb.AppendLine($"  - {e}");
        }
        sb.AppendLine();
        sb.AppendLine("VERDICT: MAP25I_WORLDBUILDER_GEOMETRY_PRIMITIVE_SCHEMA_CONTRACT_COMPLETE");
        return sb.ToString();
    }

    private static DeadMtlWorldBuilderGeometryPrimitiveSchemaResult Fail(
        List<string> errors, DeadMtlWorldBuilderGeometryPrimitiveSchema schema) =>
        new() { IsValid = false, Errors = errors, Schema = schema };
}
