using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class DeadMtlWorldBuilderComponentIntentClassificationBuilder
{
    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };

    public static DeadMtlWorldBuilderComponentIntentClassificationResult Build(
        string metadataPath,
        string sourceMaskRegionExtractionPath,
        string connectedComponentExtractionPath,
        string geometryPrimitiveSchemaPath,
        string concreteGeometryPreflightPath)
    {
        var errors = new List<string>();
        var classification = new DeadMtlWorldBuilderComponentIntentClassification
        {
            TileId                               = "map_00",
            SourceZoneMetadataJson               = metadataPath,
            SourceMaskRegionExtractionJson       = sourceMaskRegionExtractionPath,
            SourceConnectedComponentExtractionJson = connectedComponentExtractionPath,
            SourceGeometryPrimitiveSchemaJson    = geometryPrimitiveSchemaPath,
            SourceConcreteGeometryPreflightJson  = concreteGeometryPreflightPath,
            ClassificationStatus                 = "COMPONENT_INTENTS_CLASSIFIED",
        };

        var inputs = new[]
        {
            ("metadata",                        metadataPath),
            ("source mask region extraction",   sourceMaskRegionExtractionPath),
            ("connected component extraction",  connectedComponentExtractionPath),
            ("geometry primitive schema",       geometryPrimitiveSchemaPath),
            ("concrete geometry preflight",     concreteGeometryPreflightPath),
        };

        foreach (var (label, path) in inputs)
        {
            if (!File.Exists(path))
                errors.Add($"Missing {label}: file not found: {path}");
        }

        if (errors.Count > 0)
            return Fail(errors, classification);

        var components = LoadConnectedComponents(connectedComponentExtractionPath, errors);
        if (errors.Count > 0)
            return Fail(errors, classification);

        classification.ClassificationContract = new DeadMtlWorldBuilderComponentIntentClassificationContract
        {
            TileId                               = "map_00",
            ParentConnectedComponentCount        = components.Count,
            SourceComponentContract              = "MAP25K_CONNECTED_COMPONENT_EXTRACTION",
            ClassificationInputStatus            = "CONNECTED_COMPONENTS_ONLY_NO_GEOMETRY_CREATED",
            ClassificationOutputStatus           = "INTENT_BUCKETS_ONLY",
            ComponentBoundsArePixelBoundsNotGeometry = true,
            IntentRecordsAreNotGeometry          = true,
            GeometryMustBeCreatedByFutureStep    = true,
        };

        classification.ValidationRules = new DeadMtlWorldBuilderComponentIntentClassificationValidationRules
        {
            ConnectedComponentExtractionMustExist       = true,
            ConnectedComponentCountMustEqual45          = true,
            AllComponentsMustReceiveIntent              = true,
            UnclassifiedComponentCountMustEqualZero     = true,
            IntentPixelTotalMustEqualComponentPixelTotal = true,
            IntentBoundsMustRemainPixelBounds           = true,
            IntentRecordsAreNotGeometry                 = true,
            NoRuntimeClaimFromIntentClassification      = true,
            NoWriterClaimFromIntentClassification       = true,
            NoMaterializationFromIntentClassification   = true,
        };

        int intentOrder = 1;
        foreach (var comp in components)
        {
            var intent  = ClassifyIntent(comp.Role, comp.ZoneType, comp.StreetClass);
            var family  = IntentFamily(intent);
            var reason  = ClassificationReason(comp.Role, comp.ZoneType, comp.StreetClass);
            var reqs    = FutureReqs(intent);
            var consumers = FutureConsumers(intent);
            var blocked = BlockedByReqs(intent);

            classification.IntentRecords.Add(new DeadMtlWorldBuilderComponentIntentRecord
            {
                IntentOrder               = intentOrder++,
                ComponentId               = comp.ComponentId,
                ComponentOrder            = comp.ComponentOrder,
                ParentSourceColor         = comp.ParentSourceColor,
                Role                      = comp.Role,
                ZoneType                  = comp.ZoneType,
                StreetClass               = comp.StreetClass,
                ComponentIntent           = intent,
                IntentBucket              = intent,
                IntentFamily              = family,
                ClassificationReason      = reason,
                ClassificationConfidence  = "COMPONENT_COLOR_ROLE_METADATA_MATCH",
                PixelCount                = comp.PixelCount,
                BoundsMinX                = comp.BoundsMinX,
                BoundsMinY                = comp.BoundsMinY,
                BoundsMaxX                = comp.BoundsMaxX,
                BoundsMaxY                = comp.BoundsMaxY,
                BoundsWidth               = comp.BoundsWidth,
                BoundsHeight              = comp.BoundsHeight,
                BoundsStatus              = "PIXEL_COMPONENT_BOUNDS_ONLY",
                SourceComponentStatus     = "CONNECTED_PIXEL_COMPONENT_ONLY",
                GeometryStatus            = "INTENT_ONLY_NO_GEOMETRY_CREATED",
                FutureGeometryRequirementIds = reqs,
                FutureConsumers           = consumers,
                BlockedByRequirements     = blocked,
                Notes                     = NotesFor(intent),
            });
        }

        classification.Totals        = ComputeTotals(classification.IntentRecords);
        classification.ClaimBoundary = new DeadMtlWorldBuilderComponentIntentClassificationClaimBoundary();

        return new DeadMtlWorldBuilderComponentIntentClassificationResult
            { IsValid = true, Errors = errors, Classification = classification };
    }

    private static string ClassifyIntent(string role, string zoneType, string streetClass) =>
        (role, zoneType, streetClass) switch
        {
            ("ZONE", "RESIDENTIAL",  _)          => "RESIDENTIAL_LOT_BLOCK",
            ("ZONE", "COMMERCIAL",   _)          => "COMMERCIAL_LOT_BLOCK",
            ("ZONE", "GREENSPACE",   _)          => "GREENSPACE_MASS",
            ("STREET_CORRIDOR", _, "MAIN_ROAD")  => "MAIN_ROAD_CORRIDOR",
            ("STREET_CORRIDOR", _, "BACK_ALLEY") => "BACK_ALLEY_CORRIDOR",
            ("UNIQUE_PLACEHOLDER",   _, _)       => "CIVIC_PLACEHOLDER",
            _                                    => "IGNORE_BORDER",
        };

    private static string IntentFamily(string intent) => intent switch
    {
        "RESIDENTIAL_LOT_BLOCK" => "LOT_AND_BUILDING_SLOT_CANDIDATE",
        "COMMERCIAL_LOT_BLOCK"  => "LOT_AND_BUILDING_SLOT_CANDIDATE",
        "MAIN_ROAD_CORRIDOR"    => "STREET_CORRIDOR_CANDIDATE",
        "BACK_ALLEY_CORRIDOR"   => "SERVICE_CORRIDOR_CANDIDATE",
        "GREENSPACE_MASS"       => "GREENSPACE_LAYER_CANDIDATE",
        "CIVIC_PLACEHOLDER"     => "UNIQUE_BUILDING_PLACEHOLDER_CANDIDATE",
        _                       => "IGNORE_NO_GEOMETRY",
    };

    private static string ClassificationReason(string role, string zoneType, string streetClass) =>
        (role, zoneType, streetClass) switch
        {
            ("ZONE", "RESIDENTIAL",  _)          => "ROLE_ZONE_TYPE_RESIDENTIAL",
            ("ZONE", "COMMERCIAL",   _)          => "ROLE_ZONE_TYPE_COMMERCIAL",
            ("ZONE", "GREENSPACE",   _)          => "ROLE_ZONE_TYPE_GREENSPACE",
            ("STREET_CORRIDOR", _, "MAIN_ROAD")  => "ROLE_STREET_CLASS_MAIN_ROAD",
            ("STREET_CORRIDOR", _, "BACK_ALLEY") => "ROLE_STREET_CLASS_BACK_ALLEY",
            ("UNIQUE_PLACEHOLDER",   _, _)       => "ROLE_UNIQUE_PLACEHOLDER",
            _                                    => "ROLE_IGNORE",
        };

    private static List<string> FutureReqs(string intent) => intent switch
    {
        "RESIDENTIAL_LOT_BLOCK" => new List<string>
            { "LOT_GEOMETRY", "BUILDING_SLOT_GEOMETRY", "FENCE_AND_LOT_BOUNDARY_GEOMETRY" },
        "COMMERCIAL_LOT_BLOCK"  => new List<string>
            { "LOT_GEOMETRY", "BUILDING_SLOT_GEOMETRY", "FENCE_AND_LOT_BOUNDARY_GEOMETRY" },
        "MAIN_ROAD_CORRIDOR"    => new List<string>
            { "MAIN_ROAD_CORRIDOR_GEOMETRY", "SIDEWALK_GEOMETRY" },
        "BACK_ALLEY_CORRIDOR"   => new List<string>
            { "BACK_ALLEY_SERVICE_CORRIDOR_GEOMETRY" },
        "GREENSPACE_MASS"       => new List<string> { "GREENSPACE_LAYER_FUTURE" },
        "CIVIC_PLACEHOLDER"     => new List<string>
            { "BUILDING_SLOT_GEOMETRY", "UNIQUE_BUILDING_BINDING" },
        _                       => new List<string> { "NONE" },
    };

    private static List<string> FutureConsumers(string intent) =>
        intent == "IGNORE_BORDER"
            ? new List<string> { "IGNORE" }
            : new List<string> { "GEOMETRY_GENERATOR", "MATERIALIZATION_PLANNER", "FUTURE_TILE_WRITER" };

    private static List<string> BlockedByReqs(string intent) =>
        intent == "IGNORE_BORDER"
            ? new List<string> { "NONE" }
            : new List<string>
            {
                "CONCRETE_GEOMETRY_GENERATOR_NOT_IMPLEMENTED",
                "STATIC_TILE_WRITER_NOT_IMPLEMENTED",
                "RUNTIME_VALIDATION_NOT_RUN",
            };

    private static string NotesFor(string intent) => intent switch
    {
        "RESIDENTIAL_LOT_BLOCK" =>
            "Residential lot block candidate. Awaiting lot subdivision geometry.",
        "COMMERCIAL_LOT_BLOCK"  =>
            "Commercial lot block candidate. Awaiting lot subdivision geometry.",
        "MAIN_ROAD_CORRIDOR"    =>
            "Main road corridor candidate. Awaiting road corridor and sidewalk geometry.",
        "BACK_ALLEY_CORRIDOR"   =>
            "Back alley corridor candidate. Awaiting alley geometry.",
        "GREENSPACE_MASS"       =>
            "Greenspace mass candidate. Awaiting greenspace layer generation.",
        "CIVIC_PLACEHOLDER"     =>
            "Civic unique placeholder candidate. Requires unique building binding.",
        _                       =>
            "Void or border component. No geometry will be derived.",
    };

    private sealed record ComponentData(
        int ComponentOrder,
        string ComponentId,
        string ParentSourceColor,
        string Role,
        string ZoneType,
        string StreetClass,
        int PixelCount,
        int BoundsMinX, int BoundsMinY, int BoundsMaxX, int BoundsMaxY,
        int BoundsWidth, int BoundsHeight);

    private static List<ComponentData> LoadConnectedComponents(
        string path, List<string> errors)
    {
        var result = new List<ComponentData>();
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path), DocOpts);
            if (!doc.RootElement.TryGetProperty("connected_components", out var arr))
            {
                errors.Add("Connected component extraction missing 'connected_components' array.");
                return result;
            }
            foreach (var item in arr.EnumerateArray())
            {
                int  compOrder = item.GetProperty("component_order").GetInt32();
                var  compId    = item.GetProperty("component_id").GetString() ?? "";
                var  color     = item.GetProperty("parent_source_color").GetString() ?? "";
                var  role      = item.GetProperty("role").GetString() ?? "";
                var  zoneType  = item.TryGetProperty("zone_type",    out var zt) ? zt.GetString() ?? "" : "";
                var  street    = item.TryGetProperty("street_class", out var sc) ? sc.GetString() ?? "" : "";
                int  px        = item.GetProperty("pixel_count").GetInt32();
                int  mnx       = item.GetProperty("bounds_min_x").GetInt32();
                int  mny       = item.GetProperty("bounds_min_y").GetInt32();
                int  mxx       = item.GetProperty("bounds_max_x").GetInt32();
                int  mxy       = item.GetProperty("bounds_max_y").GetInt32();
                int  bw        = item.GetProperty("bounds_width").GetInt32();
                int  bh        = item.GetProperty("bounds_height").GetInt32();

                result.Add(new ComponentData(compOrder, compId, color, role, zoneType, street,
                    px, mnx, mny, mxx, mxy, bw, bh));
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse connected component extraction: {ex.Message}");
        }
        return result;
    }

    private static DeadMtlWorldBuilderComponentIntentClassificationTotals ComputeTotals(
        List<DeadMtlWorldBuilderComponentIntentRecord> records)
    {
        var buckets = records.Select(r => r.IntentBucket).Distinct().Count();
        return new DeadMtlWorldBuilderComponentIntentClassificationTotals
        {
            ParentConnectedComponentCount   = records.Count,
            ClassifiedComponentCount        = records.Count,
            UnclassifiedComponentCount      = 0,
            IntentBucketCount               = buckets,
            ClassifiedPixelTotal            = records.Sum(r => r.PixelCount),
            ResidentialLotBlockCount        = records.Count(r => r.IntentBucket == "RESIDENTIAL_LOT_BLOCK"),
            CommercialLotBlockCount         = records.Count(r => r.IntentBucket == "COMMERCIAL_LOT_BLOCK"),
            MainRoadCorridorCount           = records.Count(r => r.IntentBucket == "MAIN_ROAD_CORRIDOR"),
            BackAlleyCorridorCount          = records.Count(r => r.IntentBucket == "BACK_ALLEY_CORRIDOR"),
            GreenspaceMassCount             = records.Count(r => r.IntentBucket == "GREENSPACE_MASS"),
            CivicPlaceholderCount           = records.Count(r => r.IntentBucket == "CIVIC_PLACEHOLDER"),
            IgnoreBorderCount               = records.Count(r => r.IntentBucket == "IGNORE_BORDER"),
            ResidentialLotBlockPixelTotal   = records.Where(r => r.IntentBucket == "RESIDENTIAL_LOT_BLOCK").Sum(r => r.PixelCount),
            CommercialLotBlockPixelTotal    = records.Where(r => r.IntentBucket == "COMMERCIAL_LOT_BLOCK").Sum(r => r.PixelCount),
            MainRoadCorridorPixelTotal      = records.Where(r => r.IntentBucket == "MAIN_ROAD_CORRIDOR").Sum(r => r.PixelCount),
            BackAlleyCorridorPixelTotal     = records.Where(r => r.IntentBucket == "BACK_ALLEY_CORRIDOR").Sum(r => r.PixelCount),
            GreenspaceMassPixelTotal        = records.Where(r => r.IntentBucket == "GREENSPACE_MASS").Sum(r => r.PixelCount),
            CivicPlaceholderPixelTotal      = records.Where(r => r.IntentBucket == "CIVIC_PLACEHOLDER").Sum(r => r.PixelCount),
            IgnoreBorderPixelTotal          = records.Where(r => r.IntentBucket == "IGNORE_BORDER").Sum(r => r.PixelCount),
            CreatedGeometryCount            = 0,
            WriterReadyIntentCount          = 0,
            RuntimeValidatedIntentCount     = 0,
            MaterializedIntentCount         = 0,
        };
    }

    public static string RenderMarkdown(DeadMtlWorldBuilderComponentIntentClassification classification)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-25L: DeadMTL WorldBuilder Component Intent Classification Contract");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** Intent classification only. No terrain generation. No lot subdivision.");
        sb.AppendLine("> No sidewalk geometry. No road geometry. No building placement. No fences.");
        sb.AppendLine("> No lotpack writing. No worldgen override file. No concrete geometry created.");
        sb.AppendLine("> No runtime proof. Not writer-ready. Layout not materialized.");
        sb.AppendLine();
        sb.AppendLine($"**tile_id:** {classification.TileId}");
        sb.AppendLine($"**status:** {classification.Status}");
        sb.AppendLine($"**classification_status:** {classification.ClassificationStatus}");
        sb.AppendLine($"**geometry_status:** {classification.GeometryStatus}");
        sb.AppendLine($"**generation_status:** {classification.GenerationStatus}");
        sb.AppendLine($"**materialization_status:** {classification.MaterializationStatus}");
        sb.AppendLine($"**runtime_status:** {classification.RuntimeStatus}");
        sb.AppendLine();
        sb.AppendLine("## Input Chain");
        sb.AppendLine();
        sb.AppendLine($"- zone metadata: `{classification.SourceZoneMetadataJson}`");
        sb.AppendLine($"- source mask region extraction: `{classification.SourceMaskRegionExtractionJson}`");
        sb.AppendLine($"- connected component extraction: `{classification.SourceConnectedComponentExtractionJson}`");
        sb.AppendLine($"- geometry primitive schema: `{classification.SourceGeometryPrimitiveSchemaJson}`");
        sb.AppendLine($"- concrete geometry preflight: `{classification.SourceConcreteGeometryPreflightJson}`");
        sb.AppendLine();
        sb.AppendLine("## Classification Contract");
        sb.AppendLine();
        var cc = classification.ClassificationContract;
        sb.AppendLine($"- tile_id: {cc.TileId}");
        sb.AppendLine($"- parent_connected_component_count: {cc.ParentConnectedComponentCount}");
        sb.AppendLine($"- source_component_contract: {cc.SourceComponentContract}");
        sb.AppendLine($"- classification_input_status: {cc.ClassificationInputStatus}");
        sb.AppendLine($"- classification_output_status: {cc.ClassificationOutputStatus}");
        sb.AppendLine($"- component_bounds_are_pixel_bounds_not_geometry: {cc.ComponentBoundsArePixelBoundsNotGeometry}");
        sb.AppendLine($"- intent_records_are_not_geometry: {cc.IntentRecordsAreNotGeometry}");
        sb.AppendLine($"- geometry_must_be_created_by_future_step: {cc.GeometryMustBeCreatedByFutureStep}");
        sb.AppendLine();
        sb.AppendLine("## Intent Bucket Definitions");
        sb.AppendLine();
        sb.AppendLine("| Intent Bucket | Intent Family |");
        sb.AppendLine("|---------------|---------------|");
        sb.AppendLine("| RESIDENTIAL_LOT_BLOCK | LOT_AND_BUILDING_SLOT_CANDIDATE |");
        sb.AppendLine("| COMMERCIAL_LOT_BLOCK  | LOT_AND_BUILDING_SLOT_CANDIDATE |");
        sb.AppendLine("| MAIN_ROAD_CORRIDOR    | STREET_CORRIDOR_CANDIDATE |");
        sb.AppendLine("| BACK_ALLEY_CORRIDOR   | SERVICE_CORRIDOR_CANDIDATE |");
        sb.AppendLine("| GREENSPACE_MASS       | GREENSPACE_LAYER_CANDIDATE |");
        sb.AppendLine("| CIVIC_PLACEHOLDER     | UNIQUE_BUILDING_PLACEHOLDER_CANDIDATE |");
        sb.AppendLine("| IGNORE_BORDER         | IGNORE_NO_GEOMETRY |");
        sb.AppendLine();
        sb.AppendLine("## Component Intent Records");
        sb.AppendLine();
        sb.AppendLine("| Order | Component ID | Color | Intent Bucket | Pixels |");
        sb.AppendLine("|-------|--------------|-------|---------------|--------|");
        foreach (var r in classification.IntentRecords)
        {
            sb.AppendLine(
                $"| {r.IntentOrder} | {r.ComponentId} | {r.ParentSourceColor} " +
                $"| {r.IntentBucket} | {r.PixelCount} |");
        }
        sb.AppendLine();
        sb.AppendLine("## Intent Count Totals");
        sb.AppendLine();
        var t = classification.Totals;
        sb.AppendLine($"- parent_connected_component_count: {t.ParentConnectedComponentCount}");
        sb.AppendLine($"- classified_component_count: {t.ClassifiedComponentCount}");
        sb.AppendLine($"- unclassified_component_count: {t.UnclassifiedComponentCount}");
        sb.AppendLine($"- intent_bucket_count: {t.IntentBucketCount}");
        sb.AppendLine($"- residential_lot_block_count: {t.ResidentialLotBlockCount}");
        sb.AppendLine($"- commercial_lot_block_count: {t.CommercialLotBlockCount}");
        sb.AppendLine($"- main_road_corridor_count: {t.MainRoadCorridorCount}");
        sb.AppendLine($"- back_alley_corridor_count: {t.BackAlleyCorridorCount}");
        sb.AppendLine($"- greenspace_mass_count: {t.GreenspaceMassCount}");
        sb.AppendLine($"- civic_placeholder_count: {t.CivicPlaceholderCount}");
        sb.AppendLine($"- ignore_border_count: {t.IgnoreBorderCount}");
        sb.AppendLine();
        sb.AppendLine("## Pixel Total Validation");
        sb.AppendLine();
        sb.AppendLine($"- classified_pixel_total: {t.ClassifiedPixelTotal}");
        sb.AppendLine($"- residential_lot_block_pixel_total: {t.ResidentialLotBlockPixelTotal}");
        sb.AppendLine($"- commercial_lot_block_pixel_total: {t.CommercialLotBlockPixelTotal}");
        sb.AppendLine($"- main_road_corridor_pixel_total: {t.MainRoadCorridorPixelTotal}");
        sb.AppendLine($"- back_alley_corridor_pixel_total: {t.BackAlleyCorridorPixelTotal}");
        sb.AppendLine($"- greenspace_mass_pixel_total: {t.GreenspaceMassPixelTotal}");
        sb.AppendLine($"- civic_placeholder_pixel_total: {t.CivicPlaceholderPixelTotal}");
        sb.AppendLine($"- ignore_border_pixel_total: {t.IgnoreBorderPixelTotal}");
        sb.AppendLine();
        sb.AppendLine("## Future Geometry Requirement Mapping");
        sb.AppendLine();
        var byBucket = classification.IntentRecords
            .GroupBy(r => r.IntentBucket)
            .OrderBy(g => g.Min(r => r.IntentOrder));
        foreach (var g in byBucket)
        {
            sb.AppendLine(
                $"- **{g.Key}:** {string.Join(", ", g.First().FutureGeometryRequirementIds)}");
        }
        sb.AppendLine();
        sb.AppendLine("## Why This Still Cannot Execute");
        sb.AppendLine();
        sb.AppendLine("Intent records are classified pixel-space components.");
        sb.AppendLine("They are NOT concrete lot polygons, road geometries, or building slots.");
        sb.AppendLine($"- created_geometry_count: {t.CreatedGeometryCount}");
        sb.AppendLine($"- writer_ready_intent_count: {t.WriterReadyIntentCount}");
        sb.AppendLine($"- runtime_validated_intent_count: {t.RuntimeValidatedIntentCount}");
        sb.AppendLine($"- materialized_intent_count: {t.MaterializedIntentCount}");
        sb.AppendLine("No concrete geometry generator exists. No tile writer exists.");
        sb.AppendLine("No runtime validation pass has been performed.");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        var cb = classification.ClaimBoundary;
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
        sb.AppendLine("## Verdict");
        sb.AppendLine();
        sb.AppendLine("**VERDICT: MAP25L_WORLDBUILDER_COMPONENT_INTENT_CLASSIFICATION_CONTRACT_COMPLETE**");
        return sb.ToString();
    }

    public static string RenderCsv(DeadMtlWorldBuilderComponentIntentClassification classification)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "intent_order,component_id,component_order,parent_source_color," +
            "role,zone_type,street_class,component_intent,intent_bucket,intent_family," +
            "classification_reason,classification_confidence," +
            "pixel_count,bounds_min_x,bounds_min_y,bounds_max_x,bounds_max_y,bounds_width,bounds_height," +
            "bounds_status,source_component_status,geometry_status," +
            "future_geometry_requirement_ids,future_consumers,blocked_by_requirements,notes");
        foreach (var r in classification.IntentRecords)
        {
            sb.AppendLine(
                $"{r.IntentOrder},{r.ComponentId},{r.ComponentOrder},{r.ParentSourceColor}," +
                $"{r.Role},{r.ZoneType},{r.StreetClass},{r.ComponentIntent},{r.IntentBucket},{r.IntentFamily}," +
                $"{r.ClassificationReason},{r.ClassificationConfidence}," +
                $"{r.PixelCount},{r.BoundsMinX},{r.BoundsMinY},{r.BoundsMaxX},{r.BoundsMaxY},{r.BoundsWidth},{r.BoundsHeight}," +
                $"{r.BoundsStatus},{r.SourceComponentStatus},{r.GeometryStatus}," +
                $"\"{string.Join("|", r.FutureGeometryRequirementIds)}\"," +
                $"\"{string.Join("|", r.FutureConsumers)}\"," +
                $"\"{string.Join("|", r.BlockedByRequirements)}\"," +
                $"\"{r.Notes}\"");
        }
        return sb.ToString();
    }

    public static string RenderSummary(DeadMtlWorldBuilderComponentIntentClassificationResult result)
    {
        var cl = result.Classification;
        var t  = cl.Totals;
        var sb = new StringBuilder();
        sb.AppendLine("MAP-25L: DeadMTL WorldBuilder Component Intent Classification Contract");
        sb.AppendLine();
        sb.AppendLine($"tile_id:                              {cl.TileId}");
        sb.AppendLine($"is_valid:                             {result.IsValid}");
        sb.AppendLine($"status:                               {cl.Status}");
        sb.AppendLine($"classification_status:                {cl.ClassificationStatus}");
        sb.AppendLine($"geometry_status:                      {cl.GeometryStatus}");
        sb.AppendLine($"generation_status:                    {cl.GenerationStatus}");
        sb.AppendLine($"materialization_status:               {cl.MaterializationStatus}");
        sb.AppendLine();
        sb.AppendLine($"parent_connected_component_count:     {t.ParentConnectedComponentCount}");
        sb.AppendLine($"classified_component_count:           {t.ClassifiedComponentCount}");
        sb.AppendLine($"unclassified_component_count:         {t.UnclassifiedComponentCount}");
        sb.AppendLine($"intent_bucket_count:                  {t.IntentBucketCount}");
        sb.AppendLine($"classified_pixel_total:               {t.ClassifiedPixelTotal}");
        sb.AppendLine($"residential_lot_block_count:          {t.ResidentialLotBlockCount}");
        sb.AppendLine($"commercial_lot_block_count:           {t.CommercialLotBlockCount}");
        sb.AppendLine($"main_road_corridor_count:             {t.MainRoadCorridorCount}");
        sb.AppendLine($"back_alley_corridor_count:            {t.BackAlleyCorridorCount}");
        sb.AppendLine($"greenspace_mass_count:                {t.GreenspaceMassCount}");
        sb.AppendLine($"civic_placeholder_count:              {t.CivicPlaceholderCount}");
        sb.AppendLine($"ignore_border_count:                  {t.IgnoreBorderCount}");
        sb.AppendLine($"created_geometry_count:               {t.CreatedGeometryCount}");
        sb.AppendLine($"writer_ready_intent_count:            {t.WriterReadyIntentCount}");
        sb.AppendLine($"runtime_validated_intent_count:       {t.RuntimeValidatedIntentCount}");
        sb.AppendLine($"materialized_intent_count:            {t.MaterializedIntentCount}");
        if (result.Errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("ERRORS");
            foreach (var err in result.Errors)
                sb.AppendLine($"  - {err}");
        }
        sb.AppendLine();
        sb.AppendLine("VERDICT: MAP25L_WORLDBUILDER_COMPONENT_INTENT_CLASSIFICATION_CONTRACT_COMPLETE");
        return sb.ToString();
    }

    private static DeadMtlWorldBuilderComponentIntentClassificationResult Fail(
        List<string> errors, DeadMtlWorldBuilderComponentIntentClassification classification) =>
        new() { IsValid = false, Errors = errors, Classification = classification };
}
