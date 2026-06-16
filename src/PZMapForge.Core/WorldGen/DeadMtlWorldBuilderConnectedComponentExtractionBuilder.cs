using System.Drawing;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

[SupportedOSPlatform("windows")]
public static class DeadMtlWorldBuilderConnectedComponentExtractionBuilder
{
    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };
    private static readonly int[] Dx = { 0, 0, -1, 1 };
    private static readonly int[] Dy = { -1, 1, 0, 0 };

    public static DeadMtlWorldBuilderConnectedComponentExtractionResult Build(
        string sourcePngPath,
        string metadataPath,
        string geometryPrimitiveSchemaPath,
        string sourceMaskRegionExtractionPath)
    {
        var errors = new List<string>();
        var extraction = new DeadMtlWorldBuilderConnectedComponentExtraction
        {
            TileId                           = "map_00",
            SourcePng                        = sourcePngPath,
            SourceZoneMetadataJson           = metadataPath,
            SourceGeometryPrimitiveSchemaJson = geometryPrimitiveSchemaPath,
            SourceMaskRegionExtractionJson   = sourceMaskRegionExtractionPath,
            ExtractionStatus                 = "CONNECTED_COMPONENTS_EXTRACTED",
            ConnectivityStatus               = "FOUR_WAY_PIXEL_CONNECTIVITY",
        };

        var inputs = new[]
        {
            ("source PNG",                    sourcePngPath),
            ("metadata",                      metadataPath),
            ("geometry primitive schema",     geometryPrimitiveSchemaPath),
            ("source mask region extraction", sourceMaskRegionExtractionPath),
        };

        foreach (var (label, path) in inputs)
        {
            if (!File.Exists(path))
                errors.Add($"Missing {label}: file not found: {path}");
        }

        if (errors.Count > 0)
            return Fail(errors, extraction);

        var colorRoles = LoadColorRoles(metadataPath, errors);
        if (errors.Count > 0)
            return Fail(errors, extraction);

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

        var pixels = new int[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var px = bmp.GetPixel(x, y);
                pixels[y * width + x] = (px.R << 16) | (px.G << 8) | px.B;
            }
        bmp.Dispose();

        var colorCounts = new Dictionary<int, int>();
        foreach (var key in pixels)
        {
            colorCounts.TryGetValue(key, out var c);
            colorCounts[key] = c + 1;
        }

        var unknownHex = new List<string>();
        foreach (var key in colorCounts.Keys)
        {
            var hex = ToHex(key);
            if (!colorRoles.ContainsKey(hex))
                unknownHex.Add(hex);
        }

        if (unknownHex.Count > 0)
        {
            errors.Add($"Unknown PNG colors not in zone metadata: {string.Join(", ", unknownHex)}");
            return Fail(errors, extraction);
        }

        extraction.SourceContract = new DeadMtlWorldBuilderConnectedComponentSourceContract
        {
            TileId                        = "map_00",
            SourcePngWidthPx              = width,
            SourcePngHeightPx             = height,
            SourcePixelCount              = width * height,
            AuthoringScale                = "1_PIXEL_EQUALS_1_WORLD_TILE",
            PixelOrigin                   = "TOP_LEFT",
            XAxis                         = "EAST_POSITIVE",
            YAxis                         = "SOUTH_POSITIVE",
            CoordinateUnits               = "SOURCE_PIXELS",
            ConnectivityRule              = "FOUR_WAY_NEIGHBOR_PIXELS",
            DiagonalConnectivityEnabled   = false,
            BoundsArePixelBoundsNotGeometry = true,
            ComponentsAreNotGeometry      = true,
        };

        extraction.ValidationRules = new DeadMtlWorldBuilderConnectedComponentValidationRules
        {
            PngMustExist                                         = true,
            PngMustBe256By256                                    = true,
            AllPixelsMustMatchKnownMetadataColors                = true,
            SourceMaskRegionExtractionMustExist                  = true,
            ComponentPixelTotalMustEqualSourcePixelCount         = true,
            ComponentColorPixelTotalsMustMatchParentMaskRegions  = true,
            ComponentBoundsMustBeWithinSourcePng                 = true,
            ComponentsAreNotGeometry                             = true,
            NoDiagonalConnectivity                               = true,
            NoRuntimeClaimFromComponentExtraction                = true,
            NoWriterClaimFromComponentExtraction                 = true,
            NoMaterializationFromComponentExtraction             = true,
        };

        // Parent color order: pixel_count desc, then key asc — matches MAP-25J sort
        var parentOrder = colorCounts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .ToList();

        var visited    = new bool[width * height];
        int globalOrder = 1;
        int parentRegionOrder = 1;

        foreach (var (colorKey, _) in parentOrder)
        {
            var hex = ToHex(colorKey);
            var cr  = colorRoles[hex];

            var components = new List<(int PixelCount, int MinX, int MinY, int MaxX, int MaxY)>();

            for (int i = 0; i < pixels.Length; i++)
            {
                if (visited[i] || pixels[i] != colorKey) continue;

                var queue = new Queue<int>();
                queue.Enqueue(i);
                visited[i] = true;

                int count = 0;
                int initX = i % width, initY = i / width;
                int mnX = initX, mnY = initY, mxX = initX, mxY = initY;

                while (queue.Count > 0)
                {
                    int idx = queue.Dequeue();
                    int cx = idx % width, cy = idx / width;
                    count++;
                    if (cx < mnX) mnX = cx;
                    if (cy < mnY) mnY = cy;
                    if (cx > mxX) mxX = cx;
                    if (cy > mxY) mxY = cy;

                    for (int d = 0; d < 4; d++)
                    {
                        int nx = cx + Dx[d], ny = cy + Dy[d];
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        int ni = ny * width + nx;
                        if (visited[ni] || pixels[ni] != colorKey) continue;
                        visited[ni] = true;
                        queue.Enqueue(ni);
                    }
                }

                components.Add((count, mnX, mnY, mxX, mxY));
            }

            var sorted = components
                .OrderByDescending(c => c.PixelCount)
                .ThenBy(c => c.MinY)
                .ThenBy(c => c.MinX)
                .ToList();

            int localIndex = 1;
            foreach (var (pxCount, mnx, mny, mxx, mxy) in sorted)
            {
                extraction.ConnectedComponents.Add(new DeadMtlWorldBuilderConnectedComponentRecord
                {
                    ComponentOrder               = globalOrder,
                    ComponentId                  = $"map_00_component_{globalOrder:D4}",
                    ParentRegionOrder            = parentRegionOrder,
                    ParentSourceColor            = hex,
                    Role                         = cr.Role,
                    ZoneType                     = cr.ZoneType,
                    StreetClass                  = cr.StreetClass,
                    LocalComponentIndex          = localIndex++,
                    PixelCount                   = pxCount,
                    BoundsMinX                   = mnx,
                    BoundsMinY                   = mny,
                    BoundsMaxX                   = mxx,
                    BoundsMaxY                   = mxy,
                    BoundsWidth                  = mxx - mnx + 1,
                    BoundsHeight                 = mxy - mny + 1,
                    BoundsStatus                 = "PIXEL_COMPONENT_BOUNDS_ONLY",
                    ConnectivityRule             = "FOUR_WAY_NEIGHBOR_PIXELS",
                    PrimitiveType                = "MASK_REGION",
                    ComponentStatus              = "CONNECTED_PIXEL_COMPONENT_ONLY",
                    GeometryStatus               = "CONNECTED_COMPONENT_ONLY_NO_GEOMETRY_CREATED",
                    SourceConfidence             = "PNG_METADATA_AND_MASK_REGION_MATCH",
                    FutureGeometryRequirementIds = FutureReqs(cr.Role, cr.ZoneType, cr.StreetClass),
                    FutureConsumers              = FutureConsumers(cr.Role),
                    Notes                        = NotesFor(cr.Role, cr.ZoneType, cr.StreetClass),
                });
                globalOrder++;
            }

            parentRegionOrder++;
        }

        extraction.Totals        = ComputeTotals(width, height, extraction.ConnectedComponents);
        extraction.ClaimBoundary = new DeadMtlWorldBuilderConnectedComponentExtractionClaimBoundary();

        return new DeadMtlWorldBuilderConnectedComponentExtractionResult
            { IsValid = true, Errors = errors, Extraction = extraction };
    }

    private static string ToHex(int key) =>
        $"#{(key >> 16) & 0xFF:X2}{(key >> 8) & 0xFF:X2}{key & 0xFF:X2}";

    private static List<string> FutureReqs(string role, string zoneType, string streetClass) =>
        (role, zoneType, streetClass) switch
        {
            ("ZONE", "RESIDENTIAL",       _) => new List<string>
                { "LOT_GEOMETRY", "BUILDING_SLOT_GEOMETRY", "FENCE_AND_LOT_BOUNDARY_GEOMETRY" },
            ("ZONE", "COMMERCIAL",        _) => new List<string>
                { "LOT_GEOMETRY", "BUILDING_SLOT_GEOMETRY", "FENCE_AND_LOT_BOUNDARY_GEOMETRY" },
            ("ZONE", "GREENSPACE",        _) => new List<string> { "GREENSPACE_LAYER_FUTURE" },
            ("STREET_CORRIDOR", _, "MAIN_ROAD")  => new List<string>
                { "MAIN_ROAD_CORRIDOR_GEOMETRY", "SIDEWALK_GEOMETRY" },
            ("STREET_CORRIDOR", _, "BACK_ALLEY") => new List<string>
                { "BACK_ALLEY_SERVICE_CORRIDOR_GEOMETRY" },
            ("UNIQUE_PLACEHOLDER", _, _) => new List<string>
                { "BUILDING_SLOT_GEOMETRY", "UNIQUE_BUILDING_BINDING" },
            _ => new List<string> { "NONE" },
        };

    private static List<string> FutureConsumers(string role) =>
        role == "IGNORE"
            ? new List<string> { "IGNORE" }
            : new List<string> { "REGION_EXTRACTOR", "GEOMETRY_GENERATOR", "FUTURE_MATERIALIZATION_PLAN" };

    private static string NotesFor(string role, string zoneType, string streetClass) =>
        (role, zoneType, streetClass) switch
        {
            ("ZONE", "RESIDENTIAL",       _) =>
                "Connected pixel component of residential zone. Source for future lot subdivision and building slot geometry.",
            ("ZONE", "COMMERCIAL",        _) =>
                "Connected pixel component of commercial zone. Source for future lot subdivision and building slot geometry.",
            ("ZONE", "GREENSPACE",        _) =>
                "Connected pixel component of greenspace zone. Source for future greenspace layer generation.",
            ("STREET_CORRIDOR", _, "MAIN_ROAD") =>
                "Connected pixel component of main road corridor. Source for future road corridor and sidewalk geometry.",
            ("STREET_CORRIDOR", _, "BACK_ALLEY") =>
                "Connected pixel component of back alley corridor. Source for future alley geometry, no sidewalks.",
            ("UNIQUE_PLACEHOLDER", _, _) =>
                "Connected pixel component of civic unique placeholder. Requires unique building binding before placement.",
            _ =>
                "Void or border pixels. No geometry will be derived from this component.",
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
                var color       = item.GetProperty("color").GetString() ?? "";
                var role        = item.GetProperty("role").GetString() ?? "";
                var zoneType    = item.TryGetProperty("zone_type",    out var zt) ? zt.GetString() ?? "" : "";
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

    private static DeadMtlWorldBuilderConnectedComponentExtractionTotals ComputeTotals(
        int width, int height,
        List<DeadMtlWorldBuilderConnectedComponentRecord> components)
    {
        var parentColors = components.Select(c => c.ParentSourceColor).Distinct().Count();
        return new DeadMtlWorldBuilderConnectedComponentExtractionTotals
        {
            SourcePngWidthPx              = width,
            SourcePngHeightPx             = height,
            SourcePixelCount              = width * height,
            ParentMaskRegionCount         = parentColors,
            ConnectedComponentCount       = components.Count,
            KnownColorComponentCount      = components.Count,
            UnknownColorComponentCount    = 0,
            ComponentPixelTotal           = components.Sum(c => c.PixelCount),
            ResidentialComponentCount     = components.Count(c => c.ZoneType == "RESIDENTIAL"),
            MainRoadComponentCount        = components.Count(c => c.StreetClass == "MAIN_ROAD"),
            GreenspaceComponentCount      = components.Count(c => c.ZoneType == "GREENSPACE"),
            BackAlleyComponentCount       = components.Count(c => c.StreetClass == "BACK_ALLEY"),
            CivicPlaceholderComponentCount = components.Count(c => c.Role == "UNIQUE_PLACEHOLDER"),
            CommercialComponentCount      = components.Count(c => c.ZoneType == "COMMERCIAL"),
            IgnoreComponentCount          = components.Count(c => c.Role == "IGNORE"),
            ResidentialPixelTotal         = components.Where(c => c.ZoneType == "RESIDENTIAL").Sum(c => c.PixelCount),
            MainRoadPixelTotal            = components.Where(c => c.StreetClass == "MAIN_ROAD").Sum(c => c.PixelCount),
            GreenspacePixelTotal          = components.Where(c => c.ZoneType == "GREENSPACE").Sum(c => c.PixelCount),
            BackAlleyPixelTotal           = components.Where(c => c.StreetClass == "BACK_ALLEY").Sum(c => c.PixelCount),
            CivicPlaceholderPixelTotal    = components.Where(c => c.Role == "UNIQUE_PLACEHOLDER").Sum(c => c.PixelCount),
            CommercialPixelTotal          = components.Where(c => c.ZoneType == "COMMERCIAL").Sum(c => c.PixelCount),
            IgnorePixelTotal              = components.Where(c => c.Role == "IGNORE").Sum(c => c.PixelCount),
            CreatedGeometryCount          = 0,
            WriterReadyComponentCount     = 0,
            RuntimeValidatedComponentCount = 0,
        };
    }

    public static string RenderMarkdown(DeadMtlWorldBuilderConnectedComponentExtraction extraction)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-25K: DeadMTL WorldBuilder Connected Component Extraction Contract");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** Connected component extraction only. No terrain generation. No lot subdivision.");
        sb.AppendLine("> No sidewalk geometry. No road geometry. No building placement. No fences.");
        sb.AppendLine("> No lotpack writing. No worldgen override file. No concrete geometry created.");
        sb.AppendLine("> No runtime proof. Not writer-ready. Layout not materialized.");
        sb.AppendLine();
        sb.AppendLine($"**tile_id:** {extraction.TileId}");
        sb.AppendLine($"**status:** {extraction.Status}");
        sb.AppendLine($"**extraction_status:** {extraction.ExtractionStatus}");
        sb.AppendLine($"**connectivity_status:** {extraction.ConnectivityStatus}");
        sb.AppendLine($"**geometry_status:** {extraction.GeometryStatus}");
        sb.AppendLine($"**generation_status:** {extraction.GenerationStatus}");
        sb.AppendLine($"**runtime_status:** {extraction.RuntimeStatus}");
        sb.AppendLine();
        sb.AppendLine("## Input Chain");
        sb.AppendLine();
        sb.AppendLine($"- source_png: `{extraction.SourcePng}`");
        sb.AppendLine($"- zone metadata: `{extraction.SourceZoneMetadataJson}`");
        sb.AppendLine($"- geometry primitive schema: `{extraction.SourceGeometryPrimitiveSchemaJson}`");
        sb.AppendLine($"- source mask region extraction: `{extraction.SourceMaskRegionExtractionJson}`");
        sb.AppendLine();
        sb.AppendLine("## Source PNG / Connectivity Contract");
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
        sb.AppendLine($"- connectivity_rule: {sc.ConnectivityRule}");
        sb.AppendLine($"- diagonal_connectivity_enabled: {sc.DiagonalConnectivityEnabled}");
        sb.AppendLine($"- bounds_are_pixel_bounds_not_geometry: {sc.BoundsArePixelBoundsNotGeometry}");
        sb.AppendLine($"- components_are_not_geometry: {sc.ComponentsAreNotGeometry}");
        sb.AppendLine();
        sb.AppendLine("## Connected Component Records");
        sb.AppendLine();
        sb.AppendLine("| Order | Component ID | Parent Color | Role | Local Idx | Pixel Count |");
        sb.AppendLine("|-------|--------------|--------------|------|-----------|-------------|");
        foreach (var c in extraction.ConnectedComponents)
        {
            sb.AppendLine(
                $"| {c.ComponentOrder} | {c.ComponentId} | {c.ParentSourceColor} | {c.Role} " +
                $"| {c.LocalComponentIndex} | {c.PixelCount} |");
        }
        sb.AppendLine();
        sb.AppendLine("## Component Count Totals");
        sb.AppendLine();
        var t = extraction.Totals;
        sb.AppendLine($"- parent_mask_region_count: {t.ParentMaskRegionCount}");
        sb.AppendLine($"- connected_component_count: {t.ConnectedComponentCount}");
        sb.AppendLine($"- known_color_component_count: {t.KnownColorComponentCount}");
        sb.AppendLine($"- unknown_color_component_count: {t.UnknownColorComponentCount}");
        sb.AppendLine($"- residential_component_count: {t.ResidentialComponentCount}");
        sb.AppendLine($"- main_road_component_count: {t.MainRoadComponentCount}");
        sb.AppendLine($"- greenspace_component_count: {t.GreenspaceComponentCount}");
        sb.AppendLine($"- back_alley_component_count: {t.BackAlleyComponentCount}");
        sb.AppendLine($"- civic_placeholder_component_count: {t.CivicPlaceholderComponentCount}");
        sb.AppendLine($"- commercial_component_count: {t.CommercialComponentCount}");
        sb.AppendLine($"- ignore_component_count: {t.IgnoreComponentCount}");
        sb.AppendLine();
        sb.AppendLine("## Pixel Total Validation");
        sb.AppendLine();
        sb.AppendLine($"- source_pixel_count: {t.SourcePixelCount}");
        sb.AppendLine($"- component_pixel_total: {t.ComponentPixelTotal}");
        sb.AppendLine($"- residential_pixel_total: {t.ResidentialPixelTotal}");
        sb.AppendLine($"- main_road_pixel_total: {t.MainRoadPixelTotal}");
        sb.AppendLine($"- greenspace_pixel_total: {t.GreenspacePixelTotal}");
        sb.AppendLine($"- back_alley_pixel_total: {t.BackAlleyPixelTotal}");
        sb.AppendLine($"- civic_placeholder_pixel_total: {t.CivicPlaceholderPixelTotal}");
        sb.AppendLine($"- commercial_pixel_total: {t.CommercialPixelTotal}");
        sb.AppendLine($"- ignore_pixel_total: {t.IgnorePixelTotal}");
        sb.AppendLine();
        sb.AppendLine("## Parent Mask Region Mapping");
        sb.AppendLine();
        var byParent = extraction.ConnectedComponents
            .GroupBy(c => c.ParentSourceColor)
            .OrderBy(g => g.First().ParentRegionOrder);
        foreach (var g in byParent)
        {
            sb.AppendLine(
                $"- **{g.Key}** ({g.First().Role}): {g.Count()} component(s), " +
                $"{g.Sum(c => c.PixelCount)} pixels");
        }
        sb.AppendLine();
        sb.AppendLine("## Future Geometry Requirement Mapping");
        sb.AppendLine();
        foreach (var g in byParent)
        {
            sb.AppendLine(
                $"- **{g.Key}:** {string.Join(", ", g.First().FutureGeometryRequirementIds)}");
        }
        sb.AppendLine();
        sb.AppendLine("## Why This Still Cannot Execute");
        sb.AppendLine();
        sb.AppendLine("Connected components are pixel-space islands extracted from the source PNG.");
        sb.AppendLine("They are NOT concrete lot polygons, road geometries, or building slots.");
        sb.AppendLine($"- created_geometry_count: {t.CreatedGeometryCount}");
        sb.AppendLine($"- writer_ready_component_count: {t.WriterReadyComponentCount}");
        sb.AppendLine($"- runtime_validated_component_count: {t.RuntimeValidatedComponentCount}");
        sb.AppendLine("No tile writer exists to convert these components into PZ tile data.");
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
        sb.AppendLine("**VERDICT: MAP25K_WORLDBUILDER_CONNECTED_COMPONENT_EXTRACTION_CONTRACT_COMPLETE**");
        return sb.ToString();
    }

    public static string RenderCsv(DeadMtlWorldBuilderConnectedComponentExtraction extraction)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "component_order,component_id,parent_region_order,parent_source_color," +
            "role,zone_type,street_class,local_component_index,pixel_count," +
            "bounds_min_x,bounds_min_y,bounds_max_x,bounds_max_y,bounds_width,bounds_height," +
            "bounds_status,connectivity_rule,primitive_type,component_status,geometry_status," +
            "source_confidence,future_geometry_requirement_ids,future_consumers,notes");
        foreach (var c in extraction.ConnectedComponents)
        {
            sb.AppendLine(
                $"{c.ComponentOrder},{c.ComponentId},{c.ParentRegionOrder},{c.ParentSourceColor}," +
                $"{c.Role},{c.ZoneType},{c.StreetClass},{c.LocalComponentIndex},{c.PixelCount}," +
                $"{c.BoundsMinX},{c.BoundsMinY},{c.BoundsMaxX},{c.BoundsMaxY},{c.BoundsWidth},{c.BoundsHeight}," +
                $"{c.BoundsStatus},{c.ConnectivityRule},{c.PrimitiveType},{c.ComponentStatus},{c.GeometryStatus}," +
                $"{c.SourceConfidence}," +
                $"\"{string.Join("|", c.FutureGeometryRequirementIds)}\"," +
                $"\"{string.Join("|", c.FutureConsumers)}\"," +
                $"\"{c.Notes}\"");
        }
        return sb.ToString();
    }

    public static string RenderSummary(DeadMtlWorldBuilderConnectedComponentExtractionResult result)
    {
        var e  = result.Extraction;
        var t  = e.Totals;
        var sb = new StringBuilder();
        sb.AppendLine("MAP-25K: DeadMTL WorldBuilder Connected Component Extraction Contract");
        sb.AppendLine();
        sb.AppendLine($"tile_id:                              {e.TileId}");
        sb.AppendLine($"is_valid:                             {result.IsValid}");
        sb.AppendLine($"status:                               {e.Status}");
        sb.AppendLine($"extraction_status:                    {e.ExtractionStatus}");
        sb.AppendLine($"connectivity_status:                  {e.ConnectivityStatus}");
        sb.AppendLine($"geometry_status:                      {e.GeometryStatus}");
        sb.AppendLine($"generation_status:                    {e.GenerationStatus}");
        sb.AppendLine();
        sb.AppendLine($"source_png_width_px:                  {t.SourcePngWidthPx}");
        sb.AppendLine($"source_png_height_px:                 {t.SourcePngHeightPx}");
        sb.AppendLine($"source_pixel_count:                   {t.SourcePixelCount}");
        sb.AppendLine($"parent_mask_region_count:             {t.ParentMaskRegionCount}");
        sb.AppendLine($"connected_component_count:            {t.ConnectedComponentCount}");
        sb.AppendLine($"known_color_component_count:          {t.KnownColorComponentCount}");
        sb.AppendLine($"unknown_color_component_count:        {t.UnknownColorComponentCount}");
        sb.AppendLine($"component_pixel_total:                {t.ComponentPixelTotal}");
        sb.AppendLine($"residential_component_count:          {t.ResidentialComponentCount}");
        sb.AppendLine($"main_road_component_count:            {t.MainRoadComponentCount}");
        sb.AppendLine($"greenspace_component_count:           {t.GreenspaceComponentCount}");
        sb.AppendLine($"back_alley_component_count:           {t.BackAlleyComponentCount}");
        sb.AppendLine($"civic_placeholder_component_count:    {t.CivicPlaceholderComponentCount}");
        sb.AppendLine($"commercial_component_count:           {t.CommercialComponentCount}");
        sb.AppendLine($"ignore_component_count:               {t.IgnoreComponentCount}");
        sb.AppendLine($"created_geometry_count:               {t.CreatedGeometryCount}");
        sb.AppendLine($"writer_ready_component_count:         {t.WriterReadyComponentCount}");
        sb.AppendLine($"runtime_validated_component_count:    {t.RuntimeValidatedComponentCount}");
        if (result.Errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("ERRORS");
            foreach (var err in result.Errors)
                sb.AppendLine($"  - {err}");
        }
        sb.AppendLine();
        sb.AppendLine("VERDICT: MAP25K_WORLDBUILDER_CONNECTED_COMPONENT_EXTRACTION_CONTRACT_COMPLETE");
        return sb.ToString();
    }

    private static DeadMtlWorldBuilderConnectedComponentExtractionResult Fail(
        List<string> errors, DeadMtlWorldBuilderConnectedComponentExtraction extraction) =>
        new() { IsValid = false, Errors = errors, Extraction = extraction };
}
