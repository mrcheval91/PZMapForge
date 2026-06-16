using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayBuilder
{
    private const int Scale            = 4;
    private const int ExpectedWidthPx  = 256;
    private const int ExpectedHeightPx = 256;
    private const int LegendPanelWidth = 320;

    public DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult Build(
        string pngPath,
        string geometryMvpPath,
        string outputPngPath)
    {
        var errors = new List<string>();

        if (!File.Exists(pngPath))
            errors.Add($"Source PNG not found: {pngPath}");
        if (!File.Exists(geometryMvpPath))
            errors.Add($"Geometry MVP JSON not found: {geometryMvpPath}");

        if (errors.Count > 0)
            return Invalid(pngPath, geometryMvpPath, outputPngPath, errors);

        int width, height;
        Bitmap sourceBmp;
        try
        {
            sourceBmp = new Bitmap(pngPath);
            width  = sourceBmp.Width;
            height = sourceBmp.Height;
        }
        catch (Exception ex)
        {
            errors.Add($"Source PNG cannot be loaded: {ex.Message}");
            return Invalid(pngPath, geometryMvpPath, outputPngPath, errors);
        }

        using (sourceBmp)
        {
            if (width != ExpectedWidthPx || height != ExpectedHeightPx)
            {
                errors.Add($"Source PNG dimensions are not {ExpectedWidthPx}x{ExpectedHeightPx}: actual {width}x{height}.");
                return Invalid(pngPath, geometryMvpPath, outputPngPath, errors);
            }

            DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult? mvp;
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                mvp = JsonSerializer.Deserialize<DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult>(
                    File.ReadAllText(geometryMvpPath), opts);
            }
            catch (Exception ex)
            {
                errors.Add($"Geometry MVP JSON cannot be parsed: {ex.Message}");
                return Invalid(pngPath, geometryMvpPath, outputPngPath, errors);
            }

            if (mvp == null || mvp.ComponentGeometry == null ||
                mvp.LotGeometry.Count == 0 || mvp.BuildingSlotGeometry.Count == 0)
            {
                errors.Add("Geometry MVP JSON does not contain the expected MAP-26A component/lots/slots records.");
                return Invalid(pngPath, geometryMvpPath, outputPngPath, errors);
            }

            var comp = mvp.ComponentGeometry;
            var lots = mvp.LotGeometry.OrderBy(l => l.LotOrder).ToList();
            var slots = mvp.BuildingSlotGeometry
                .Where(s => s.SlotStatus == "ACCEPTED")
                .OrderBy(s => s.SlotOrder)
                .ToList();

            var features = new List<DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayFeature>();
            int order = 1;

            AddFeature(features, ref order, "COMPONENT_BBOX", comp.ComponentId, comp.ComponentId,
                comp.ComponentId, comp.MinX, comp.MinY, comp.WidthPx, comp.HeightPx, drawOrder: 1, errors, width, height);

            foreach (var lot in lots)
            {
                AddFeature(features, ref order, "LOT_RECTANGLE", lot.LotId, lot.LotId,
                    lot.LotId, lot.MinX, lot.MinY, lot.WidthPx, lot.HeightPx, drawOrder: 2, errors, width, height);
            }

            foreach (var slot in slots)
            {
                AddFeature(features, ref order, "BUILDING_SLOT_RECTANGLE", slot.SlotId, slot.SlotId,
                    slot.SlotId, slot.MinX, slot.MinY, slot.WidthPx, slot.HeightPx, drawOrder: 3, errors, width, height);
            }

            if (errors.Count > 0)
                return Invalid(pngPath, geometryMvpPath, outputPngPath, errors);

            var result = new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult
            {
                Format                    = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-qa-overlay.v1",
                GeneratedUtc              = DateTime.UtcNow.ToString("o"),
                MapId                     = mvp.TileId,
                SourcePngPath             = pngPath,
                GeometryMvpPath           = geometryMvpPath,
                OutputPngPath             = outputPngPath,
                SourceWidthPx             = width,
                SourceHeightPx            = height,
                Scale                     = Scale,
                TargetComponentId         = comp.ComponentId,
                TargetComponentOrder      = comp.ComponentOrder,
                Intent                    = comp.Intent,
                AccessReadinessClass      = mvp.GeometryMvpContract.AccessReadinessClass,
                ComponentBbox             = new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayBbox
                {
                    MinX = comp.MinX, MinY = comp.MinY, MaxX = comp.MaxX, MaxY = comp.MaxY,
                    WidthPx = comp.WidthPx, HeightPx = comp.HeightPx,
                },
                LotCount                  = lots.Count,
                AcceptedBuildingSlotCount = slots.Count,
                OverlayFeatureCount       = features.Count,
                WriterReady               = false,
                RuntimeValid              = false,
                Materialized              = false,
                Features                  = features,
                Errors                    = errors,
                IsValid                   = true,
                Verdict                   = "MAP26B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_OVERLAY_COMPLETE",
            };

            try
            {
                RenderOverlayPng(sourceBmp, result, outputPngPath);
            }
            catch (Exception ex)
            {
                errors.Add($"Overlay PNG could not be written: {ex.Message}");
                return Invalid(pngPath, geometryMvpPath, outputPngPath, errors);
            }

            return result;
        }
    }

    // -----------------------------------------------------------------------

    private static void AddFeature(
        List<DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayFeature> features,
        ref int order,
        string kind,
        string featureId,
        string sourceGeometryId,
        string label,
        int x, int y, int w, int h,
        int drawOrder,
        List<string> errors,
        int sourceWidth, int sourceHeight)
    {
        int right  = x + w - 1;
        int bottom = y + h - 1;

        if (w <= 0 || h <= 0)
            errors.Add($"{kind} '{featureId}' has non-positive width/height: {w}x{h}.");
        if (x < 0 || y < 0 || right >= sourceWidth || bottom >= sourceHeight)
            errors.Add($"{kind} '{featureId}' rectangle ({x},{y})->({right},{bottom}) is outside source image bounds {sourceWidth}x{sourceHeight}.");

        features.Add(new DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayFeature
        {
            FeatureOrder     = order++,
            FeatureKind      = kind,
            FeatureId        = featureId,
            SourceGeometryId = sourceGeometryId,
            Label            = label,
            X                = x,
            Y                = y,
            Width            = w,
            Height           = h,
            Right            = right,
            Bottom           = bottom,
            Scale            = Scale,
            OverlayX         = x * Scale,
            OverlayY         = y * Scale,
            OverlayWidth     = w * Scale,
            OverlayHeight    = h * Scale,
            DrawOrder        = drawOrder,
        });
    }

    private static void RenderOverlayPng(
        Bitmap sourceBmp,
        DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult result,
        string outputPngPath)
    {
        int baseW = result.SourceWidthPx * Scale;
        int baseH = result.SourceHeightPx * Scale;

        using var overlay = new Bitmap(baseW + LegendPanelWidth, baseH);
        using var g = Graphics.FromImage(overlay);
        g.SmoothingMode     = SmoothingMode.None;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode   = PixelOffsetMode.Half;

        g.Clear(Color.White);
        g.DrawImage(sourceBmp, new Rectangle(0, 0, baseW, baseH));

        using var bboxPen   = new Pen(Color.Red, 3);
        using var lotPen    = new Pen(Color.Blue, 2);
        using var slotPen   = new Pen(Color.Lime, 1);
        using var bboxBrush = new SolidBrush(Color.Red);
        using var lotBrush  = new SolidBrush(Color.Blue);
        using var slotBrush = new SolidBrush(Color.Lime);
        using var font      = new Font(FontFamily.GenericMonospace, 9);

        foreach (var f in result.Features.OrderBy(f => f.DrawOrder).ThenBy(f => f.FeatureOrder))
        {
            var (pen, brush) = f.FeatureKind switch
            {
                "COMPONENT_BBOX"           => (bboxPen, bboxBrush),
                "LOT_RECTANGLE"             => (lotPen, lotBrush),
                "BUILDING_SLOT_RECTANGLE"   => (slotPen, slotBrush),
                _                           => (lotPen, lotBrush),
            };
            var rect = new Rectangle(f.OverlayX, f.OverlayY, f.OverlayWidth, f.OverlayHeight);
            g.DrawRectangle(pen, rect);
            g.DrawString(f.Label, font, brush, rect.X + 2, rect.Y + 2);
        }

        DrawLegend(g, result, baseW, baseH, font);

        overlay.Save(outputPngPath, System.Drawing.Imaging.ImageFormat.Png);
    }

    private static void DrawLegend(
        Graphics g,
        DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult result,
        int baseW, int baseH, Font font)
    {
        var legendRect = new Rectangle(baseW, 0, LegendPanelWidth, baseH);
        using var legendBrush = new SolidBrush(Color.WhiteSmoke);
        g.FillRectangle(legendBrush, legendRect);
        using var border = new Pen(Color.Black, 1);
        g.DrawRectangle(border, legendRect);

        var lines = new[]
        {
            "MAP-26B QA OVERLAY",
            $"map_id: {result.MapId}",
            $"component: {result.TargetComponentId}",
            $"intent: {result.Intent}",
            $"access: {result.AccessReadinessClass}",
            $"scale: {result.Scale}",
            $"lots: {result.LotCount}",
            $"slots: {result.AcceptedBuildingSlotCount}",
            $"features: {result.OverlayFeatureCount}",
            "writer_ready: false",
            "runtime_valid: false",
            "materialized: false",
            "",
            result.Verdict,
        };

        using var textBrush = new SolidBrush(Color.Black);
        int y = 8;
        foreach (var line in lines)
        {
            g.DrawString(line, font, textBrush, baseW + 8, y);
            y += 16;
        }
    }

    // -----------------------------------------------------------------------

    public string RenderJson(DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult result)
    {
        var opts = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(result, opts);
    }

    public string RenderMarkdown(DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-26B WorldBuilder Minimal Concrete Geometry QA Overlay");
        sb.AppendLine();
        sb.AppendLine($"- map id: {result.MapId}");
        sb.AppendLine($"- source PNG path: {result.SourcePngPath}");
        sb.AppendLine($"- geometry MVP JSON path: {result.GeometryMvpPath}");
        sb.AppendLine($"- overlay PNG path: {result.OutputPngPath}");
        sb.AppendLine($"- source dimensions: {result.SourceWidthPx}x{result.SourceHeightPx}");
        sb.AppendLine($"- overlay scale: {result.Scale}");
        sb.AppendLine($"- target component id/order: {result.TargetComponentId} (order {result.TargetComponentOrder})");
        sb.AppendLine($"- intent: {result.Intent}");
        sb.AppendLine($"- access class: {result.AccessReadinessClass}");
        if (result.ComponentBbox is { } bbox)
            sb.AppendLine($"- component bbox: ({bbox.MinX},{bbox.MinY})->({bbox.MaxX},{bbox.MaxY}) {bbox.WidthPx}x{bbox.HeightPx}px");
        sb.AppendLine($"- lot count: {result.LotCount}");
        sb.AppendLine($"- accepted building slot count: {result.AcceptedBuildingSlotCount}");
        sb.AppendLine($"- overlay feature count: {result.OverlayFeatureCount}");
        sb.AppendLine($"- writer_ready: {result.WriterReady}");
        sb.AppendLine($"- runtime_valid: {result.RuntimeValid}");
        sb.AppendLine($"- materialized: {result.Materialized}");
        sb.AppendLine($"- verdict: {result.Verdict}");
        sb.AppendLine();
        sb.AppendLine("This is a QA visualization contract only. It is not a writer, not runtime");
        sb.AppendLine("validation, not materialization, and not public playable packaging.");
        sb.AppendLine();
        sb.AppendLine("## Overlay Features");
        sb.AppendLine();
        sb.AppendLine("| feature_order | feature_kind | feature_id | label | x | y | width | height | right | bottom | overlay_x | overlay_y | overlay_width | overlay_height | draw_order |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var f in result.Features)
        {
            sb.AppendLine($"| {f.FeatureOrder} | {f.FeatureKind} | {f.FeatureId} | {f.Label} | {f.X} | {f.Y} | {f.Width} | {f.Height} | {f.Right} | {f.Bottom} | {f.OverlayX} | {f.OverlayY} | {f.OverlayWidth} | {f.OverlayHeight} | {f.DrawOrder} |");
        }
        return sb.ToString();
    }

    public string RenderCsv(DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("feature_order,feature_kind,feature_id,label,x,y,width,height,right,bottom,scale,overlay_x,overlay_y,overlay_width,overlay_height,draw_order");
        foreach (var f in result.Features)
        {
            sb.AppendLine($"{f.FeatureOrder},{f.FeatureKind},{f.FeatureId},{f.Label},{f.X},{f.Y},{f.Width},{f.Height},{f.Right},{f.Bottom},{f.Scale},{f.OverlayX},{f.OverlayY},{f.OverlayWidth},{f.OverlayHeight},{f.DrawOrder}");
        }
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult result)
    {
        var bbox = result.ComponentBbox;
        string bboxStr = bbox != null
            ? $"({bbox.MinX},{bbox.MinY})->({bbox.MaxX},{bbox.MaxY}) {bbox.WidthPx}x{bbox.HeightPx}px"
            : string.Empty;
        return $"""
map_id                  : {result.MapId}
source_png              : {result.SourcePngPath}
geometry_mvp            : {result.GeometryMvpPath}
overlay_png             : {result.OutputPngPath}
source_dimensions       : {result.SourceWidthPx}x{result.SourceHeightPx}
scale                   : {result.Scale}
target_component        : {result.TargetComponentId}
intent                  : {result.Intent}
access_class            : {result.AccessReadinessClass}
component_bbox          : {bboxStr}
lot_rectangles          : {result.LotCount}
building_slot_rectangles: {result.AcceptedBuildingSlotCount}
overlay_features        : {result.OverlayFeatureCount}
writer_ready            : {(result.WriterReady ? 1 : 0)}
runtime_valid           : {(result.RuntimeValid ? 1 : 0)}
materialized            : {(result.Materialized ? 1 : 0)}
verdict                 : {result.Verdict}
""";
    }

    // -----------------------------------------------------------------------

    private static DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult Invalid(
        string png, string geometryMvp, string outputPng, List<string> errors) =>
        new()
        {
            Format          = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-qa-overlay.v1",
            GeneratedUtc    = DateTime.UtcNow.ToString("o"),
            MapId           = "map_00",
            SourcePngPath   = png,
            GeometryMvpPath = geometryMvp,
            OutputPngPath   = outputPng,
            Errors          = errors,
            IsValid         = false,
            Verdict         = "MAP26B_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_QA_OVERLAY_INVALID",
        };
}
