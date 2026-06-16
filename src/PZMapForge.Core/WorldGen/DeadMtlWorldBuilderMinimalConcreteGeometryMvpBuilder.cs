using System.Drawing;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryMvpBuilder
{
    private static readonly int[] Dx = { 0, 0, -1, 1 };
    private static readonly int[] Dy = { -1, 1, 0, 0 };

    private const int TargetLotWidthPx = 12;
    private const int MinLotWidthPx    = 8;
    private const int MaxLotCount      = 8;
    private const int SideInset        = 2;
    private const int FrontageSetback  = 3;
    private const int RearSetback      = 3;

    public DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult Build(
        string pngPath,
        string connectedComponentsPath,
        string accessProfilePath,
        int    targetComponentOrder)
    {
        var errors = new List<string>();

        if (!File.Exists(pngPath))
            errors.Add($"PNG not found: {pngPath}");
        if (!File.Exists(connectedComponentsPath))
            errors.Add($"Connected components JSON not found: {connectedComponentsPath}");
        if (!File.Exists(accessProfilePath))
            errors.Add($"Access profile JSON not found: {accessProfilePath}");

        if (errors.Count > 0)
            return Invalid(pngPath, connectedComponentsPath, accessProfilePath, targetComponentOrder, errors);

        var opts = new JsonDocumentOptions { AllowTrailingCommas = true };

        // --- load tile_id from connected components ---
        string tileId = "map_00";
        using (var ccDoc = JsonDocument.Parse(File.ReadAllText(connectedComponentsPath), opts))
        {
            var root = ccDoc.RootElement;
            if (root.TryGetProperty("tile_id", out var tid))
                tileId = tid.GetString() ?? tileId;
        }
        string intent = string.Empty;

        // --- load access profile for target component ---
        string frontageId    = string.Empty;
        string rearId        = string.Empty;
        string accessClass   = string.Empty;
        using (var apDoc = JsonDocument.Parse(File.ReadAllText(accessProfilePath), opts))
        {
            var root = apDoc.RootElement;
            JsonElement profilesEl = default;
            bool found = false;
            if (root.TryGetProperty("profiles", out profilesEl))
            {
                foreach (var p in profilesEl.EnumerateArray())
                {
                    if (!p.TryGetProperty("component_order", out var co)) continue;
                    if (co.GetInt32() != targetComponentOrder) continue;
                    if (p.TryGetProperty("primary_frontage_component_id", out var fid))
                        frontageId = fid.GetString() ?? string.Empty;
                    if (p.TryGetProperty("primary_rear_service_component_id", out var rid))
                        rearId = rid.GetString() ?? string.Empty;
                    if (p.TryGetProperty("access_readiness_class", out var arc))
                        accessClass = arc.GetString() ?? string.Empty;
                    // load intent from profile if not found in CC
                    if (string.IsNullOrEmpty(intent) && p.TryGetProperty("intent", out var pi))
                        intent = pi.GetString() ?? string.Empty;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                errors.Add($"Target component order {targetComponentOrder} not found in access profile.");
                return Invalid(pngPath, connectedComponentsPath, accessProfilePath, targetComponentOrder, errors);
            }
        }

        int frontageOrder   = ParseComponentOrder(frontageId);
        int rearOrder       = ParseComponentOrder(rearId);
        string componentId  = $"{tileId}_component_{targetComponentOrder:D4}";

        // --- read PNG and build pixel labels ---
        int[] pixels;
        int width, height;
        using (var bmp = new Bitmap(pngPath))
        {
            width  = bmp.Width;
            height = bmp.Height;
            pixels = new int[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var px = bmp.GetPixel(x, y);
                pixels[y * width + x] = (px.R << 16) | (px.G << 8) | px.B;
            }
        }

        var pixelLabel = BuildPixelLabels(pixels, width, height);

        // --- compute bbox and pixel count for target ---
        int mnX = int.MaxValue, mnY = int.MaxValue;
        int mxX = int.MinValue, mxY = int.MinValue;
        int pixelCount = 0;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            if (pixelLabel[y * width + x] != targetComponentOrder) continue;
            pixelCount++;
            if (x < mnX) mnX = x; if (x > mxX) mxX = x;
            if (y < mnY) mnY = y; if (y > mxY) mxY = y;
        }

        if (pixelCount == 0)
        {
            errors.Add($"No pixels found for component order {targetComponentOrder}.");
            return Invalid(pngPath, connectedComponentsPath, accessProfilePath, targetComponentOrder, errors);
        }

        int bboxW = mxX - mnX + 1;
        int bboxH = mxY - mnY + 1;

        // --- side detection (4-directional contact count) ---
        int northFrontage = 0, southFrontage = 0, eastFrontage = 0, westFrontage = 0;
        int northRear = 0, southRear = 0, eastRear = 0, westRear = 0;

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            if (pixelLabel[y * width + x] != targetComponentOrder) continue;

            if (y > 0)
            {
                int neighbor = pixelLabel[(y - 1) * width + x];
                if (neighbor == frontageOrder) northFrontage++;
                if (neighbor == rearOrder)     northRear++;
            }
            if (y < height - 1)
            {
                int neighbor = pixelLabel[(y + 1) * width + x];
                if (neighbor == frontageOrder) southFrontage++;
                if (neighbor == rearOrder)     southRear++;
            }
            if (x < width - 1)
            {
                int neighbor = pixelLabel[y * width + x + 1];
                if (neighbor == frontageOrder) eastFrontage++;
                if (neighbor == rearOrder)     eastRear++;
            }
            if (x > 0)
            {
                int neighbor = pixelLabel[y * width + x - 1];
                if (neighbor == frontageOrder) westFrontage++;
                if (neighbor == rearOrder)     westRear++;
            }
        }

        string frontageSide     = ArgMaxSide(northFrontage, southFrontage, eastFrontage, westFrontage);
        int frontageContactPx   = Math.Max(Math.Max(northFrontage, southFrontage), Math.Max(eastFrontage, westFrontage));
        string rearSide         = ArgMaxSide(northRear, southRear, eastRear, westRear);
        int rearContactPx       = Math.Max(Math.Max(northRear, southRear), Math.Max(eastRear, westRear));

        // --- lot slicing ---
        bool splitAlongX = frontageSide == "NORTH" || frontageSide == "SOUTH";
        int span = splitAlongX ? bboxW : bboxH;

        int lotCount = Math.Clamp(span / TargetLotWidthPx, 1, MaxLotCount);
        // ensure each lot >= min width
        while (lotCount > 1 && span / lotCount < MinLotWidthPx) lotCount--;

        int baseSlice = span / lotCount;
        int extra     = span % lotCount;

        var lots  = new List<DeadMtlWorldBuilderMinimalConcreteLotGeometryRecord>(lotCount);
        var slots = new List<DeadMtlWorldBuilderMinimalConcreteBuildingSlotGeometryRecord>(lotCount);

        int cursor = splitAlongX ? mnX : mnY;

        for (int i = 0; i < lotCount; i++)
        {
            int sliceSize = baseSlice + (i < extra ? 1 : 0);
            int sliceEnd  = cursor + sliceSize - 1;

            int lotMinX, lotMinY, lotMaxX, lotMaxY;
            if (splitAlongX)
            {
                lotMinX = cursor; lotMaxX = sliceEnd;
                lotMinY = mnY;    lotMaxY = mxY;
            }
            else
            {
                lotMinX = mnX;    lotMaxX = mxX;
                lotMinY = cursor; lotMaxY = sliceEnd;
            }

            int lotW = lotMaxX - lotMinX + 1;
            int lotH = lotMaxY - lotMinY + 1;

            string lotId = $"{tileId}_comp{targetComponentOrder:D4}_lot_{i + 1:D4}";

            lots.Add(new DeadMtlWorldBuilderMinimalConcreteLotGeometryRecord
            {
                LotOrder        = i + 1,
                LotId           = lotId,
                ComponentOrder  = targetComponentOrder,
                ComponentId     = componentId,
                MinX            = lotMinX,
                MinY            = lotMinY,
                MaxX            = lotMaxX,
                MaxY            = lotMaxY,
                WidthPx         = lotW,
                HeightPx        = lotH,
                FrontageSide    = frontageSide,
                RearServiceSide = rearSide,
            });

            // --- building slot ---
            int slotMinX, slotMinY, slotMaxX, slotMaxY;
            if (splitAlongX)
            {
                // frontage is NORTH or SOUTH — setback applies along Y, inset along X
                slotMinX = lotMinX + SideInset;
                slotMaxX = lotMaxX - SideInset;
                slotMinY = frontageSide == "NORTH" ? lotMinY + FrontageSetback : lotMinY + RearSetback;
                slotMaxY = frontageSide == "NORTH" ? lotMaxY - RearSetback     : lotMaxY - FrontageSetback;
            }
            else
            {
                // frontage is EAST or WEST — setback applies along X, inset along Y
                slotMinY = lotMinY + SideInset;
                slotMaxY = lotMaxY - SideInset;
                slotMinX = frontageSide == "WEST" ? lotMinX + FrontageSetback : lotMinX + RearSetback;
                slotMaxX = frontageSide == "WEST" ? lotMaxX - RearSetback     : lotMaxX - FrontageSetback;
            }

            int slotW = slotMaxX - slotMinX + 1;
            int slotH = slotMaxY - slotMinY + 1;
            bool accepted = slotW >= 4 && slotH >= 4;

            string slotId     = $"{tileId}_comp{targetComponentOrder:D4}_slot_{i + 1:D4}";
            string slotStatus = accepted ? "ACCEPTED" : "REJECTED_TOO_SMALL";
            string geoStatus  = accepted ? "CONCRETE_PIXEL_GEOMETRY_CREATED" : "REJECTED_NO_GEOMETRY_CREATED";

            slots.Add(new DeadMtlWorldBuilderMinimalConcreteBuildingSlotGeometryRecord
            {
                SlotOrder         = i + 1,
                SlotId            = slotId,
                LotId             = lotId,
                LotOrder          = i + 1,
                ComponentOrder    = targetComponentOrder,
                ComponentId       = componentId,
                MinX              = slotMinX,
                MinY              = slotMinY,
                MaxX              = slotMaxX,
                MaxY              = slotMaxY,
                WidthPx           = slotW,
                HeightPx          = slotH,
                FrontageSetbackPx = FrontageSetback,
                RearSetbackPx     = RearSetback,
                SideInsetPx       = SideInset,
                SlotStatus        = slotStatus,
                GeometryStatus    = geoStatus,
            });

            cursor += sliceSize;
        }

        var compGeom = new DeadMtlWorldBuilderMinimalConcreteComponentGeometryRecord
        {
            ComponentOrder  = targetComponentOrder,
            ComponentId     = componentId,
            Intent          = intent,
            MinX            = mnX,
            MinY            = mnY,
            MaxX            = mxX,
            MaxY            = mxY,
            WidthPx         = bboxW,
            HeightPx        = bboxH,
            PixelCount      = pixelCount,
            FrontageSide    = frontageSide,
            RearServiceSide = rearSide,
        };

        int acceptedSlots  = slots.Count(s => s.SlotStatus == "ACCEPTED");
        int rejectedSlots  = slots.Count(s => s.SlotStatus != "ACCEPTED");
        int createdCount   = 1 + lots.Count + acceptedSlots; // 1 = component bbox

        var contract = new DeadMtlWorldBuilderMinimalConcreteGeometryMvpContract
        {
            TileId                        = tileId,
            TargetComponentOrder          = targetComponentOrder,
            TargetComponentId             = componentId,
            TargetIntent                  = intent,
            AccessReadinessClass          = accessClass,
            PrimaryFrontageComponentId    = frontageId,
            PrimaryFrontageComponentOrder = frontageOrder,
            PrimaryRearServiceComponentId = rearId,
            PrimaryRearServiceComponentOrder = rearOrder,
            ComponentBboxMinX             = mnX,
            ComponentBboxMinY             = mnY,
            ComponentBboxMaxX             = mxX,
            ComponentBboxMaxY             = mxY,
            ComponentBboxWidthPx          = bboxW,
            ComponentBboxHeightPx         = bboxH,
            ComponentPixelCount           = pixelCount,
            FrontageSide                  = frontageSide,
            FrontageContactPx             = frontageContactPx,
            RearServiceSide               = rearSide,
            RearServiceContactPx          = rearContactPx,
            LotGeometryCount              = lots.Count,
            BuildingSlotGeometryCount     = slots.Count,
            AcceptedBuildingSlotCount     = acceptedSlots,
            RejectedBuildingSlotCount     = rejectedSlots,
            CreatedGeometryCount          = createdCount,
            WriterReadyGeometryCount      = 0,
            RuntimeValidatedGeometryCount = 0,
            MaterializedGeometryCount     = 0,
            GeometryStatus                = "CONCRETE_PIXEL_GEOMETRY_CREATED",
        };

        var claimBoundary = new DeadMtlWorldBuilderMinimalConcreteClaimBoundary
        {
            WritesLotpack                 = false,
            WritesWorldgenLua             = false,
            RuntimeProven                 = false,
            PublicPlayableClaim           = false,
            WriterReadyClaim              = false,
            WriterReadyGeometryCount      = 0,
            RuntimeValidatedGeometryCount = 0,
            MaterializedGeometryCount     = 0,
        };

        return new DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult
        {
            Format                        = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-mvp.v1",
            TileId                        = tileId,
            GeneratedUtc                  = DateTime.UtcNow.ToString("o"),
            SourcePngPath                 = pngPath,
            SourceConnectedComponentsPath = connectedComponentsPath,
            SourceAccessProfilePath       = accessProfilePath,
            TargetComponentOrder          = targetComponentOrder,
            GeometryMvpContract           = contract,
            ComponentGeometry             = compGeom,
            LotGeometry                   = lots,
            BuildingSlotGeometry          = slots,
            ClaimBoundary                 = claimBoundary,
            Errors                        = errors,
            IsValid                       = true,
            Verdict                       = "MAP26A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_MVP_COMPLETE",
        };
    }

    // -----------------------------------------------------------------------

    public string RenderJson(DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult result)
    {
        var opts = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(result, opts);
    }

    public string RenderMarkdown(DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult result)
    {
        var c = result.GeometryMvpContract;
        var sb = new StringBuilder();
        sb.AppendLine($"# Minimal Concrete Geometry MVP — {result.TileId}");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {result.GeneratedUtc}");
        sb.AppendLine($"**Target:** {c.TargetComponentId} (order {c.TargetComponentOrder})");
        sb.AppendLine($"**Intent:** {c.TargetIntent}");
        sb.AppendLine($"**Access class:** {c.AccessReadinessClass}");
        sb.AppendLine();
        sb.AppendLine("## Component Bbox");
        sb.AppendLine($"- min_x={c.ComponentBboxMinX} min_y={c.ComponentBboxMinY} max_x={c.ComponentBboxMaxX} max_y={c.ComponentBboxMaxY}");
        sb.AppendLine($"- width={c.ComponentBboxWidthPx}px height={c.ComponentBboxHeightPx}px pixel_count={c.ComponentPixelCount}");
        sb.AppendLine();
        sb.AppendLine("## Sides");
        sb.AppendLine($"- Frontage ({c.PrimaryFrontageComponentId}): {c.FrontageSide} ({c.FrontageContactPx}px contact)");
        sb.AppendLine($"- Rear service ({c.PrimaryRearServiceComponentId}): {c.RearServiceSide} ({c.RearServiceContactPx}px contact)");
        sb.AppendLine();
        sb.AppendLine("## Geometry Counts");
        sb.AppendLine($"- Lots: {c.LotGeometryCount}");
        sb.AppendLine($"- Building slots: {c.BuildingSlotGeometryCount} ({c.AcceptedBuildingSlotCount} accepted, {c.RejectedBuildingSlotCount} rejected)");
        sb.AppendLine($"- Created geometry records: {c.CreatedGeometryCount}");
        sb.AppendLine($"- Writer-ready: {c.WriterReadyGeometryCount}");
        sb.AppendLine($"- Runtime-validated: {c.RuntimeValidatedGeometryCount}");
        sb.AppendLine($"- Materialized: {c.MaterializedGeometryCount}");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        var cb = result.ClaimBoundary;
        sb.AppendLine($"- writes_lotpack: {cb.WritesLotpack}");
        sb.AppendLine($"- writes_worldgen_lua: {cb.WritesWorldgenLua}");
        sb.AppendLine($"- runtime_proven: {cb.RuntimeProven}");
        sb.AppendLine($"- public_playable_claim: {cb.PublicPlayableClaim}");
        sb.AppendLine($"- writer_ready_claim: {cb.WriterReadyClaim}");
        sb.AppendLine();
        sb.AppendLine($"**Verdict:** {result.Verdict}");
        return sb.ToString();
    }

    public string RenderCsv(DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("record_order,record_type,record_id,component_order,component_id,min_x,min_y,max_x,max_y,width_px,height_px,frontage_side,rear_service_side,geometry_type,slot_status,geometry_status");

        int row = 1;
        var cg = result.ComponentGeometry;
        if (cg != null)
        {
            sb.AppendLine($"{row++},COMPONENT,{cg.ComponentId},{cg.ComponentOrder},{cg.ComponentId},{cg.MinX},{cg.MinY},{cg.MaxX},{cg.MaxY},{cg.WidthPx},{cg.HeightPx},{cg.FrontageSide},{cg.RearServiceSide},{cg.GeometryType},,{cg.GeometryStatus}");
        }
        foreach (var lot in result.LotGeometry)
        {
            sb.AppendLine($"{row++},LOT,{lot.LotId},{lot.ComponentOrder},{lot.ComponentId},{lot.MinX},{lot.MinY},{lot.MaxX},{lot.MaxY},{lot.WidthPx},{lot.HeightPx},{lot.FrontageSide},{lot.RearServiceSide},{lot.GeometryType},,{lot.GeometryStatus}");
        }
        foreach (var slot in result.BuildingSlotGeometry)
        {
            sb.AppendLine($"{row++},BUILDING_SLOT,{slot.SlotId},{slot.ComponentOrder},{slot.ComponentId},{slot.MinX},{slot.MinY},{slot.MaxX},{slot.MaxY},{slot.WidthPx},{slot.HeightPx},,,{slot.GeometryType},{slot.SlotStatus},{slot.GeometryStatus}");
        }
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult result)
    {
        var c = result.GeometryMvpContract;
        return $"""
MAP-26A Minimal Concrete Geometry MVP
tile_id          : {result.TileId}
target_component : {c.TargetComponentId} (order {c.TargetComponentOrder})
intent           : {c.TargetIntent}
access_class     : {c.AccessReadinessClass}
bbox             : ({c.ComponentBboxMinX},{c.ComponentBboxMinY})→({c.ComponentBboxMaxX},{c.ComponentBboxMaxY}) {c.ComponentBboxWidthPx}×{c.ComponentBboxHeightPx}px
pixel_count      : {c.ComponentPixelCount}
frontage_side    : {c.FrontageSide} ({c.FrontageContactPx}px)
rear_side        : {c.RearServiceSide} ({c.RearServiceContactPx}px)
lots             : {c.LotGeometryCount}
building_slots   : {c.BuildingSlotGeometryCount} ({c.AcceptedBuildingSlotCount} accepted)
created_geometry : {c.CreatedGeometryCount}
writer_ready     : {c.WriterReadyGeometryCount}
runtime_valid    : {c.RuntimeValidatedGeometryCount}
materialized     : {c.MaterializedGeometryCount}
verdict          : {result.Verdict}
""";
    }

    // -----------------------------------------------------------------------

    private static int ParseComponentOrder(string componentId)
    {
        if (string.IsNullOrEmpty(componentId)) return 0;
        var parts = componentId.Split('_');
        if (parts.Length == 0) return 0;
        return int.TryParse(parts[^1], out var n) ? n : 0;
    }

    private static string ArgMaxSide(int north, int south, int east, int west)
    {
        int max = Math.Max(Math.Max(north, south), Math.Max(east, west));
        if (max == 0) return "NORTH";
        if (north == max) return "NORTH";
        if (south == max) return "SOUTH";
        if (east  == max) return "EAST";
        return "WEST";
    }

    private static int[] BuildPixelLabels(int[] pixels, int width, int height)
    {
        var pixelLabel  = new int[pixels.Length];
        var colorCounts = new Dictionary<int, int>();
        foreach (var c in pixels)
        {
            colorCounts.TryGetValue(c, out var cnt);
            colorCounts[c] = cnt + 1;
        }
        var parentOrder = colorCounts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .Select(kv => kv.Key).ToList();

        var visited     = new bool[pixels.Length];
        int globalOrder = 1;

        foreach (var colorKey in parentOrder)
        {
            var components = new List<(int Count, int MinY, int MinX, List<int> Members)>();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (visited[i] || pixels[i] != colorKey) continue;
                var queue = new Queue<int>(); queue.Enqueue(i); visited[i] = true;
                int initX = i % width, initY = i / width;
                int mnX = initX, mnY = initY;
                var members = new List<int>();
                while (queue.Count > 0)
                {
                    int idx = queue.Dequeue(); int cx = idx % width, cy = idx / width;
                    members.Add(idx);
                    if (cx < mnX) mnX = cx;
                    if (cy < mnY) mnY = cy;
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = cx + Dx[d], ny = cy + Dy[d];
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        int ni = ny * width + nx;
                        if (visited[ni] || pixels[ni] != colorKey) continue;
                        visited[ni] = true; queue.Enqueue(ni);
                    }
                }
                components.Add((members.Count, mnY, mnX, members));
            }
            var sorted = components
                .OrderByDescending(c => c.Count)
                .ThenBy(c => c.MinY)
                .ThenBy(c => c.MinX)
                .ToList();
            foreach (var (_, _, _, members) in sorted)
            {
                int order = globalOrder++;
                foreach (var idx in members) pixelLabel[idx] = order;
            }
        }
        return pixelLabel;
    }

    private static DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult Invalid(
        string png, string cc, string ap, int targetOrder, List<string> errors) =>
        new()
        {
            Format                        = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-mvp.v1",
            TileId                        = "map_00",
            GeneratedUtc                  = DateTime.UtcNow.ToString("o"),
            SourcePngPath                 = png,
            SourceConnectedComponentsPath = cc,
            SourceAccessProfilePath       = ap,
            TargetComponentOrder          = targetOrder,
            Errors                        = errors,
            IsValid                       = false,
            Verdict                       = "MAP26A_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_MVP_INVALID",
        };
}
