using System.Drawing;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

[SupportedOSPlatform("windows")]
public static class DeadMtlWorldBuilderSourceMaskRegionExtractionBuilder
{
    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };

    public static DeadMtlWorldBuilderSourceMaskRegionExtractionResult Build(
        string sourcePngPath,
        string metadataPath,
        string geometryPrimitiveSchemPath,
        string geometryPreflightPath)
    {
        var errors = new List<string>();
        var extraction = new DeadMtlWorldBuilderSourceMaskRegionExtraction
        {
            TileId                           = "map_00",
            SourcePng                        = sourcePngPath,
            SourceZoneMetadataJson           = metadataPath,
            SourceGeometryPrimitiveSchemaJson = geometryPrimitiveSchemPath,
            SourceConcreteGeometryPreflightJson = geometryPreflightPath,
            ExtractionStatus                 = "SOURCE_MASK_REGIONS_EXTRACTED",
        };

        var inputs = new[]
        {
            ("source PNG",               sourcePngPath),
            ("metadata",                 metadataPath),
            ("geometry primitive schema", geometryPrimitiveSchemPath),
            ("geometry preflight",        geometryPreflightPath),
        };

        foreach (var (label, path) in inputs)
        {
            if (!File.Exists(path))
                errors.Add($"Missing {label}: file not found: {path}");
        }

        if (errors.Count > 0)
            return Fail(errors, extraction);

        // Load zone metadata color role lookup
        var colorRoles = LoadColorRoles(metadataPath, errors);
        if (errors.Count > 0)
            return Fail(errors, extraction);

        // Load PNG and scan
        Bitmap bmp;
        try { bmp = new Bitmap(sourcePngPath); }
        catch (Exception ex)
        {
            errors.Add($"Failed to load PNG: {ex.Message}");
            return Fail(errors, extraction);
        }

        int width  = bmp.Width;
        int height = bmp.Height;

        if (width != 256 || height != 256)
        {
            bmp.Dispose();
            errors.Add($"PNG must be 256x256, got {width}x{height}");
            return Fail(errors, extraction);
        }

        // Scan pixels: count + bounds per color
        var colorCounts = new Dictionary<int, int>();
        var boundsMinX  = new Dictionary<int, int>();
        var boundsMinY  = new Dictionary<int, int>();
        var boundsMaxX  = new Dictionary<int, int>();
        var boundsMaxY  = new Dictionary<int, int>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var px  = bmp.GetPixel(x, y);
                int key = (px.R << 16) | (px.G << 8) | px.B;
                colorCounts.TryGetValue(key, out var c);
                colorCounts[key] = c + 1;

                if (!boundsMinX.ContainsKey(key))
                {
                    boundsMinX[key] = x; boundsMinY[key] = y;
                    boundsMaxX[key] = x; boundsMaxY[key] = y;
                }
                else
                {
                    if (x < boundsMinX[key]) boundsMinX[key] = x;
                    if (y < boundsMinY[key]) boundsMinY[key] = y;
                    if (x > boundsMaxX[key]) boundsMaxX[key] = x;
                    if (y > boundsMaxY[key]) boundsMaxY[key] = y;
                }
            }
        }
        bmp.Dispose();

        extraction.SourceContract = new DeadMtlWorldBuilderSourceMaskRegionSourceContract
        {
            TileId             = "map_00",
            SourcePngWidthPx   = width,
            SourcePngHeightPx  = height,
            SourcePixelCount   = width * height,
            AuthoringScale     = "1_PIXEL_EQUALS_1_WORLD_TILE",
            PixelOrigin        = "TOP_LEFT",
            XAxis              = "EAST_POSITIVE",
            YAxis              = "SOUTH_POSITIVE",
            CoordinateUnits    = "SOURCE_PIXELS",
            BoundsArePixelBoundsNotGeometry = true,
        };

        extraction.ValidationRules = new DeadMtlWorldBuilderSourceMaskRegionValidationRules
        {
            PngMustExist                        = true,
            PngMustBe256By256                   = true,
            AllPixelsMustMatchKnownMetadataColors = true,
            MetadataMustIncludeAllPngColors     = true,
            PixelTotalMustEqualWidthTimesHeight  = true,
            MaskBoundsMustBeWithinSourcePng     = true,
            MaskRegionsAreNotGeometry           = true,
            NoRuntimeClaimFromExtraction        = true,
            NoWriterClaimFromExtraction         = true,
            NoMaterializationFromExtraction     = true,
        };

        // Check for unknown colors (in PNG but not in metadata)
        var unknownHex = new List<string>();
        foreach (var key in colorCounts.Keys)
        {
            var hex = $"#{(key >> 16) & 0xFF:X2}{(key >> 8) & 0xFF:X2}{key & 0xFF:X2}";
            if (!colorRoles.ContainsKey(hex))
                unknownHex.Add(hex);
        }

        if (unknownHex.Count > 0)
        {
            errors.Add($"Unknown PNG colors not in zone metadata: {string.Join(", ", unknownHex)}");
            return Fail(errors, extraction);
        }

        // Sort by pixel count descending for region_order assignment
        var sorted = colorCounts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .ToList();

        int order = 1;
        foreach (var (key, count) in sorted)
        {
            var hex = $"#{(key >> 16) & 0xFF:X2}{(key >> 8) & 0xFF:X2}{key & 0xFF:X2}";
            var cr  = colorRoles[hex];
            int mnx = boundsMinX[key], mny = boundsMinY[key];
            int mxx = boundsMaxX[key], mxy = boundsMaxY[key];

            extraction.MaskRegions.Add(new DeadMtlWorldBuilderSourceMaskRegionRecord
            {
                RegionOrder    = order++,
                SourceColor    = hex,
                Role           = cr.Role,
                ZoneType       = cr.ZoneType,
                StreetClass    = cr.StreetClass,
                PixelCount     = count,
                BoundsMinX     = mnx,
                BoundsMinY     = mny,
                BoundsMaxX     = mxx,
                BoundsMaxY     = mxy,
                BoundsWidth    = mxx - mnx + 1,
                BoundsHeight   = mxy - mny + 1,
                BoundsStatus   = "PIXEL_BOUNDS_ONLY",
                PrimitiveType  = "MASK_REGION",
                GeometryStatus = "SOURCE_MASK_ONLY_NO_GEOMETRY_CREATED",
                SourceConfidence = "PNG_AND_ZONE_METADATA_MATCH",
                FutureGeometryRequirementIds = FutureReqs(cr.Role, cr.ZoneType, cr.StreetClass),
                FutureConsumers = FutureConsumers(cr.Role),
                Notes          = NotesFor(cr.Role, cr.ZoneType, cr.StreetClass),
            });
        }

        // Check metadata colors not in PNG
        var metadataHexes = new HashSet<string>(colorRoles.Keys, StringComparer.OrdinalIgnoreCase);
        var pngHexes      = new HashSet<string>(
            colorCounts.Keys.Select(k =>
                $"#{(k >> 16) & 0xFF:X2}{(k >> 8) & 0xFF:X2}{k & 0xFF:X2}"),
            StringComparer.OrdinalIgnoreCase);
        int metadataMissing = metadataHexes.Count(h => !pngHexes.Contains(h));

        extraction.Totals = ComputeTotals(width, height, extraction.MaskRegions,
            unknownHex.Count, metadataMissing);

        extraction.ClaimBoundary = new DeadMtlWorldBuilderSourceMaskRegionExtractionClaimBoundary
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

        return new DeadMtlWorldBuilderSourceMaskRegionExtractionResult
            { IsValid = true, Errors = errors, Extraction = extraction };
    }

    private static List<string> FutureReqs(string role, string zoneType, string streetClass) =>
        (role, zoneType, streetClass) switch
        {
            ("ZONE", "RESIDENTIAL",        _) => new List<string>
                { "LOT_GEOMETRY", "BUILDING_SLOT_GEOMETRY", "FENCE_AND_LOT_BOUNDARY_GEOMETRY" },
            ("ZONE", "COMMERCIAL",         _) => new List<string>
                { "LOT_GEOMETRY", "BUILDING_SLOT_GEOMETRY", "FENCE_AND_LOT_BOUNDARY_GEOMETRY" },
            ("ZONE", "GREENSPACE",         _) => new List<string> { "GREENSPACE_LAYER_FUTURE" },
            ("STREET_CORRIDOR", _, "MAIN_ROAD")  => new List<string>
                { "MAIN_ROAD_CORRIDOR_GEOMETRY", "SIDEWALK_GEOMETRY" },
            ("STREET_CORRIDOR", _, "BACK_ALLEY") => new List<string>
                { "BACK_ALLEY_SERVICE_CORRIDOR_GEOMETRY" },
            ("UNIQUE_PLACEHOLDER",         _, _) => new List<string>
                { "BUILDING_SLOT_GEOMETRY", "UNIQUE_BUILDING_BINDING" },
            _                                    => new List<string> { "NONE" },
        };

    private static List<string> FutureConsumers(string role) =>
        role == "IGNORE"
            ? new List<string> { "IGNORE" }
            : new List<string> { "REGION_EXTRACTOR", "GEOMETRY_GENERATOR", "FUTURE_MATERIALIZATION_PLAN" };

    private static string NotesFor(string role, string zoneType, string streetClass) =>
        (role, zoneType, streetClass) switch
        {
            ("ZONE", "RESIDENTIAL",        _) =>
                "Pixel mask for residential zone. Source for future lot subdivision and building slot geometry.",
            ("ZONE", "COMMERCIAL",         _) =>
                "Pixel mask for commercial zone. Source for future lot subdivision and building slot geometry.",
            ("ZONE", "GREENSPACE",         _) =>
                "Pixel mask for greenspace zone. Source for future greenspace layer generation.",
            ("STREET_CORRIDOR", _, "MAIN_ROAD") =>
                "Pixel mask for main road corridor. Source for future road corridor and sidewalk geometry.",
            ("STREET_CORRIDOR", _, "BACK_ALLEY") =>
                "Pixel mask for back alley service corridor. Source for future alley geometry, no sidewalks.",
            ("UNIQUE_PLACEHOLDER",         _, _) =>
                "Pixel mask for civic unique placeholder. Requires unique building binding before placement.",
            _ =>
                "Void or border pixels. No geometry will be derived from this region.",
        };

    private record ColorRole(string Role, string ZoneType, string StreetClass);

    private static Dictionary<string, ColorRole> LoadColorRoles(string metadataPath, List<string> errors)
    {
        var result = new Dictionary<string, ColorRole>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(metadataPath), DocOpts);
            if (!doc.RootElement.TryGetProperty("color_roles", out var arr))
            {
                errors.Add("Zone metadata missing 'color_roles' array.");
                return result;
            }
            foreach (var item in arr.EnumerateArray())
            {
                var color      = item.GetProperty("color").GetString() ?? "";
                var role       = item.GetProperty("role").GetString() ?? "";
                var zoneType   = item.TryGetProperty("zone_type",   out var zt) ? zt.GetString() ?? "" : "";
                var streetClass = item.TryGetProperty("street_class", out var sc) ? sc.GetString() ?? "" : "";
                if (!string.IsNullOrEmpty(color))
                    result[color] = new ColorRole(role, zoneType, streetClass);
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to parse zone metadata: {ex.Message}");
        }
        return result;
    }

    private static DeadMtlWorldBuilderSourceMaskRegionExtractionTotals ComputeTotals(
        int width, int height,
        List<DeadMtlWorldBuilderSourceMaskRegionRecord> regions,
        int unknownCount, int metadataMissing)
    {
        int knownCount = regions.Count;
        return new DeadMtlWorldBuilderSourceMaskRegionExtractionTotals
        {
            SourcePngWidthPx           = width,
            SourcePngHeightPx          = height,
            SourcePixelCount           = width * height,
            MaskRegionCount            = knownCount + unknownCount,
            KnownColorRegionCount      = knownCount,
            UnknownColorRegionCount    = unknownCount,
            MetadataMatchedRegionCount = knownCount,
            MetadataMissingRegionCount = metadataMissing,
            MaskRegionPixelTotal       = regions.Sum(r => r.PixelCount),
            ResidentialPixelCount      = regions.Where(r => r.ZoneType == "RESIDENTIAL").Sum(r => r.PixelCount),
            MainRoadPixelCount         = regions.Where(r => r.StreetClass == "MAIN_ROAD").Sum(r => r.PixelCount),
            GreenspacePixelCount       = regions.Where(r => r.ZoneType == "GREENSPACE").Sum(r => r.PixelCount),
            BackAlleyPixelCount        = regions.Where(r => r.StreetClass == "BACK_ALLEY").Sum(r => r.PixelCount),
            CivicPlaceholderPixelCount = regions.Where(r => r.Role == "UNIQUE_PLACEHOLDER").Sum(r => r.PixelCount),
            CommercialPixelCount       = regions.Where(r => r.ZoneType == "COMMERCIAL").Sum(r => r.PixelCount),
            IgnorePixelCount           = regions.Where(r => r.Role == "IGNORE").Sum(r => r.PixelCount),
            CreatedGeometryCount       = 0,
            WriterReadyRegionCount     = 0,
            RuntimeValidatedRegionCount = 0,
        };
    }

    public static string RenderMarkdown(DeadMtlWorldBuilderSourceMaskRegionExtraction extraction)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-25J: DeadMTL WorldBuilder Source Mask Region Extraction Contract");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** Source mask extraction only. No terrain generation. No lot subdivision.");
        sb.AppendLine("> No sidewalk geometry. No road geometry. No building placement. No fences.");
        sb.AppendLine("> No lotpack writing. No worldgen override file. No concrete geometry created.");
        sb.AppendLine("> No runtime proof. Not writer-ready. Layout not materialized.");
        sb.AppendLine();
        sb.AppendLine($"**tile_id:** {extraction.TileId}");
        sb.AppendLine($"**status:** {extraction.Status}");
        sb.AppendLine($"**extraction_status:** {extraction.ExtractionStatus}");
        sb.AppendLine($"**geometry_status:** {extraction.GeometryStatus}");
        sb.AppendLine($"**generation_status:** {extraction.GenerationStatus}");
        sb.AppendLine($"**runtime_status:** {extraction.RuntimeStatus}");
        sb.AppendLine();
        sb.AppendLine("## Input Chain");
        sb.AppendLine();
        sb.AppendLine($"- source_png: `{extraction.SourcePng}`");
        sb.AppendLine($"- zone metadata: `{extraction.SourceZoneMetadataJson}`");
        sb.AppendLine($"- geometry primitive schema: `{extraction.SourceGeometryPrimitiveSchemaJson}`");
        sb.AppendLine($"- concrete geometry preflight: `{extraction.SourceConcreteGeometryPreflightJson}`");
        sb.AppendLine();
        sb.AppendLine("## Source PNG Contract");
        sb.AppendLine();
        var sc = extraction.SourceContract;
        sb.AppendLine($"- tile_id: {sc.TileId}");
        sb.AppendLine($"- source_png_width_px: {sc.SourcePngWidthPx}");
        sb.AppendLine($"- source_png_height_px: {sc.SourcePngHeightPx}");
        sb.AppendLine($"- source_pixel_count: {sc.SourcePixelCount}");
        sb.AppendLine($"- authoring_scale: {sc.AuthoringScale}");
        sb.AppendLine($"- pixel_origin: {sc.PixelOrigin}");
        sb.AppendLine($"- x_axis: {sc.XAxis}");
        sb.AppendLine($"- y_axis: {sc.YAxis}");
        sb.AppendLine($"- coordinate_units: {sc.CoordinateUnits}");
        sb.AppendLine($"- bounds_are_pixel_bounds_not_geometry: {sc.BoundsArePixelBoundsNotGeometry}");
        sb.AppendLine();
        sb.AppendLine("## Mask Region Records");
        sb.AppendLine();
        sb.AppendLine("| Order | Color | Role | Zone Type / Street Class | Pixel Count |");
        sb.AppendLine("|-------|-------|------|--------------------------|-------------|");
        foreach (var r in extraction.MaskRegions)
        {
            var typeOrClass = !string.IsNullOrEmpty(r.StreetClass) ? r.StreetClass : r.ZoneType;
            sb.AppendLine($"| {r.RegionOrder} | {r.SourceColor} | {r.Role} | {typeOrClass} | {r.PixelCount} |");
        }
        sb.AppendLine();
        foreach (var r in extraction.MaskRegions)
        {
            sb.AppendLine($"**{r.SourceColor} ({r.Role}):**");
            sb.AppendLine($"- pixel_count: {r.PixelCount}");
            sb.AppendLine($"- bounds: ({r.BoundsMinX},{r.BoundsMinY}) to ({r.BoundsMaxX},{r.BoundsMaxY}) — {r.BoundsWidth}x{r.BoundsHeight}");
            sb.AppendLine($"- primitive_type: {r.PrimitiveType}");
            sb.AppendLine($"- geometry_status: {r.GeometryStatus}");
            sb.AppendLine($"- source_confidence: {r.SourceConfidence}");
            sb.AppendLine($"- future_geometry_requirement_ids: {string.Join(", ", r.FutureGeometryRequirementIds)}");
            sb.AppendLine($"- notes: {r.Notes}");
            sb.AppendLine();
        }
        sb.AppendLine("## Pixel Count Totals");
        sb.AppendLine();
        var t = extraction.Totals;
        sb.AppendLine($"- residential_pixel_count: {t.ResidentialPixelCount}");
        sb.AppendLine($"- main_road_pixel_count: {t.MainRoadPixelCount}");
        sb.AppendLine($"- greenspace_pixel_count: {t.GreenspacePixelCount}");
        sb.AppendLine($"- back_alley_pixel_count: {t.BackAlleyPixelCount}");
        sb.AppendLine($"- civic_placeholder_pixel_count: {t.CivicPlaceholderPixelCount}");
        sb.AppendLine($"- commercial_pixel_count: {t.CommercialPixelCount}");
        sb.AppendLine($"- ignore_pixel_count: {t.IgnorePixelCount}");
        sb.AppendLine($"- mask_region_pixel_total: {t.MaskRegionPixelTotal}");
        sb.AppendLine($"- source_pixel_count: {t.SourcePixelCount}");
        sb.AppendLine();
        sb.AppendLine("## Metadata Match Validation");
        sb.AppendLine();
        sb.AppendLine($"- mask_region_count: {t.MaskRegionCount}");
        sb.AppendLine($"- known_color_region_count: {t.KnownColorRegionCount}");
        sb.AppendLine($"- unknown_color_region_count: {t.UnknownColorRegionCount}");
        sb.AppendLine($"- metadata_matched_region_count: {t.MetadataMatchedRegionCount}");
        sb.AppendLine($"- metadata_missing_region_count: {t.MetadataMissingRegionCount}");
        sb.AppendLine();
        sb.AppendLine("## Future Geometry Requirement Mapping");
        sb.AppendLine();
        foreach (var r in extraction.MaskRegions)
        {
            sb.AppendLine($"- **{r.SourceColor}:** {string.Join(", ", r.FutureGeometryRequirementIds)}");
        }
        sb.AppendLine();
        sb.AppendLine("## Why This Still Cannot Execute");
        sb.AppendLine();
        sb.AppendLine("Mask regions are pixel-space color regions extracted from the source PNG.");
        sb.AppendLine("They are NOT concrete lot polygons, road geometries, or building slots.");
        sb.AppendLine($"- created_geometry_count: {t.CreatedGeometryCount}");
        sb.AppendLine($"- writer_ready_region_count: {t.WriterReadyRegionCount}");
        sb.AppendLine($"- runtime_validated_region_count: {t.RuntimeValidatedRegionCount}");
        sb.AppendLine("No tile writer exists to convert these mask regions into PZ tile data.");
        sb.AppendLine("No runtime validation pass has been performed.");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        var cb = extraction.ClaimBoundary;
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
        sb.AppendLine("**VERDICT: MAP25J_WORLDBUILDER_SOURCE_MASK_REGION_EXTRACTION_CONTRACT_COMPLETE**");
        return sb.ToString();
    }

    public static string RenderCsv(DeadMtlWorldBuilderSourceMaskRegionExtraction extraction)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "region_order,source_color,role,zone_type,street_class,pixel_count," +
            "bounds_min_x,bounds_min_y,bounds_max_x,bounds_max_y,bounds_width,bounds_height," +
            "bounds_status,primitive_type,geometry_status,source_confidence," +
            "future_geometry_requirement_ids,future_consumers,notes");
        foreach (var r in extraction.MaskRegions)
        {
            sb.AppendLine(
                $"{r.RegionOrder},{r.SourceColor},{r.Role},{r.ZoneType},{r.StreetClass},{r.PixelCount}," +
                $"{r.BoundsMinX},{r.BoundsMinY},{r.BoundsMaxX},{r.BoundsMaxY},{r.BoundsWidth},{r.BoundsHeight}," +
                $"{r.BoundsStatus},{r.PrimitiveType},{r.GeometryStatus},{r.SourceConfidence}," +
                $"\"{string.Join("|", r.FutureGeometryRequirementIds)}\",\"{string.Join("|", r.FutureConsumers)}\"," +
                $"\"{r.Notes}\"");
        }
        return sb.ToString();
    }

    public static string RenderSummary(DeadMtlWorldBuilderSourceMaskRegionExtractionResult result)
    {
        var e  = result.Extraction;
        var t  = e.Totals;
        var sb = new StringBuilder();
        sb.AppendLine("MAP-25J: DeadMTL WorldBuilder Source Mask Region Extraction Contract");
        sb.AppendLine();
        sb.AppendLine($"tile_id:                           {e.TileId}");
        sb.AppendLine($"is_valid:                          {result.IsValid}");
        sb.AppendLine($"status:                            {e.Status}");
        sb.AppendLine($"extraction_status:                 {e.ExtractionStatus}");
        sb.AppendLine($"geometry_status:                   {e.GeometryStatus}");
        sb.AppendLine($"generation_status:                 {e.GenerationStatus}");
        sb.AppendLine();
        sb.AppendLine($"source_png_width_px:               {t.SourcePngWidthPx}");
        sb.AppendLine($"source_png_height_px:              {t.SourcePngHeightPx}");
        sb.AppendLine($"source_pixel_count:                {t.SourcePixelCount}");
        sb.AppendLine($"mask_region_count:                 {t.MaskRegionCount}");
        sb.AppendLine($"known_color_region_count:          {t.KnownColorRegionCount}");
        sb.AppendLine($"unknown_color_region_count:        {t.UnknownColorRegionCount}");
        sb.AppendLine($"metadata_matched_region_count:     {t.MetadataMatchedRegionCount}");
        sb.AppendLine($"metadata_missing_region_count:     {t.MetadataMissingRegionCount}");
        sb.AppendLine($"mask_region_pixel_total:           {t.MaskRegionPixelTotal}");
        sb.AppendLine($"created_geometry_count:            {t.CreatedGeometryCount}");
        sb.AppendLine($"writer_ready_region_count:         {t.WriterReadyRegionCount}");
        sb.AppendLine($"runtime_validated_region_count:    {t.RuntimeValidatedRegionCount}");
        sb.AppendLine($"extraction_status:                 {e.ExtractionStatus}");
        sb.AppendLine($"geometry_status:                   {e.GeometryStatus}");
        if (result.Errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("ERRORS");
            foreach (var err in result.Errors)
                sb.AppendLine($"  - {err}");
        }
        sb.AppendLine();
        sb.AppendLine("VERDICT: MAP25J_WORLDBUILDER_SOURCE_MASK_REGION_EXTRACTION_CONTRACT_COMPLETE");
        return sb.ToString();
    }

    private static DeadMtlWorldBuilderSourceMaskRegionExtractionResult Fail(
        List<string> errors, DeadMtlWorldBuilderSourceMaskRegionExtraction extraction) =>
        new() { IsValid = false, Errors = errors, Extraction = extraction };
}
