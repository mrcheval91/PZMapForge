using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

[SupportedOSPlatform("windows")]
public static class System2StaticRoadExtractor
{
    private sealed record PaletteEntry(string Intent, string Color);
    private sealed record ContractLayer(string Id, string File, string LayerClass);

    private static readonly JsonDocumentOptions DocOpts = new()
    {
        AllowTrailingCommas = true,
    };

    public static System2StaticRoadExtractResult Extract(
        string contractPath,
        string palettePath,
        string packRoot,
        int originX = 10580,
        int originY = 8200,
        bool failOnUnknownOpaque = true)
    {
        var result = new System2StaticRoadExtractResult();

        if (!File.Exists(contractPath))
        {
            result.Errors.Add($"Contract file not found: {contractPath}");
            return result;
        }
        if (!File.Exists(palettePath))
        {
            result.Errors.Add($"Palette file not found: {palettePath}");
            return result;
        }

        Dictionary<(byte R, byte G, byte B), PaletteEntry> palette;
        try
        {
            var paletteJson = File.ReadAllText(palettePath, Encoding.UTF8);
            using var paletteDoc = JsonDocument.Parse(paletteJson, DocOpts);
            palette = ParsePalette(paletteDoc);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse palette: {ex.Message}");
            return result;
        }

        List<ContractLayer> contractLayers;
        try
        {
            var contractJson = File.ReadAllText(contractPath, Encoding.UTF8);
            using var contractDoc = JsonDocument.Parse(contractJson, DocOpts);
            contractLayers = ParseContractLayers(contractDoc);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse contract: {ex.Message}");
            return result;
        }

        var layerSummaries  = new List<System2StaticRoadLayerSummary>(contractLayers.Count);
        var totalNonEmpty   = 0;
        var totalUnknown    = 0;
        var firstWidth      = 0;
        var firstHeight     = 0;

        foreach (var layer in contractLayers)
        {
            var pngPath = Path.Combine(packRoot,
                layer.File.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(pngPath))
            {
                result.Errors.Add($"Layer PNG not found: {pngPath}");
                continue;
            }

            var (summary, w, h, unknownCount, layerErrors) =
                ExtractLayer(layer, pngPath, palette, originX, originY, failOnUnknownOpaque);

            layerSummaries.Add(summary);
            result.Errors.AddRange(layerErrors);
            totalNonEmpty += summary.NonEmptyPixels;
            totalUnknown  += unknownCount;

            if (firstWidth  == 0 && w > 0) firstWidth  = w;
            if (firstHeight == 0 && h > 0) firstHeight = h;
        }

        if (failOnUnknownOpaque && totalUnknown > 0)
        {
            result.IsValid = false;
            return result;
        }

        result.IsValid = result.Errors.Count == 0;
        result.Extract = new System2StaticRoadExtract
        {
            Format         = "pzmapforge.deadmtl.system2.static-road-extract.v1",
            Status         = "EXTRACT_ONLY",
            RuntimeStatus  = "NOT_RUNTIME_PROVEN",
            WriterStatus   = "NOT_IMPLEMENTED",
            SourceContract = contractPath,
            OriginX        = originX,
            OriginY        = originY,
            Width          = firstWidth,
            Height         = firstHeight,
            Scale          = new System2ExtractScale(),
            Layers         = layerSummaries,
            Totals         = new System2ExtractTotals
            {
                LayerCount          = layerSummaries.Count,
                NonEmptyPixels      = totalNonEmpty,
                UnknownOpaquePixels = totalUnknown,
            },
            ClaimBoundary = new System2ExtractClaimBoundary(),
        };

        return result;
    }

    private static Dictionary<(byte R, byte G, byte B), PaletteEntry> ParsePalette(
        JsonDocument doc)
    {
        var result = new Dictionary<(byte R, byte G, byte B), PaletteEntry>();

        if (!doc.RootElement.TryGetProperty("entries", out var entries)
            || entries.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var entry in entries.EnumerateArray())
        {
            var hex    = entry.TryGetProperty("hex",    out var hP) ? hP.GetString() ?? "" : "";
            var intent = entry.TryGetProperty("intent", out var iP) ? iP.GetString() ?? "" : "";

            if (hex.StartsWith('#') && hex.Length == 7)
            {
                try
                {
                    var r = Convert.ToByte(hex[1..3], 16);
                    var g = Convert.ToByte(hex[3..5], 16);
                    var b = Convert.ToByte(hex[5..7], 16);
                    result[(r, g, b)] = new PaletteEntry(intent, hex.ToUpperInvariant());
                }
                catch { /* skip malformed entry */ }
            }
        }

        return result;
    }

    private static List<ContractLayer> ParseContractLayers(JsonDocument doc)
    {
        var result = new List<ContractLayer>();

        if (!doc.RootElement.TryGetProperty("layers", out var layers)
            || layers.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var layer in layers.EnumerateArray())
        {
            var id  = layer.TryGetProperty("id",    out var idP)    ? idP.GetString()    ?? "" : "";
            var file = layer.TryGetProperty("file", out var fileP)  ? fileP.GetString()  ?? "" : "";
            var cls = layer.TryGetProperty("class", out var classP) ? classP.GetString() ?? "" : "";
            result.Add(new ContractLayer(id, file, cls));
        }

        return result;
    }

    private static (System2StaticRoadLayerSummary Summary, int Width, int Height,
                    int UnknownOpaquePixels, List<string> Errors)
        ExtractLayer(
            ContractLayer layer,
            string pngPath,
            Dictionary<(byte R, byte G, byte B), PaletteEntry> palette,
            int originX,
            int originY,
            bool failOnUnknownOpaque)
    {
        var errors   = new List<string>();
        var runs     = new List<System2StaticRoadPixelRun>();
        var nodes    = new List<System2StaticRoadNode>();
        var intents  = new HashSet<string>(StringComparer.Ordinal);
        var nonEmpty = 0;
        var unknown  = 0;

        var isNodeLayer = layer.Id == "static_road_nodes";

        int width, height;

        using (var bmp = new Bitmap(pngPath))
        {
            width  = bmp.Width;
            height = bmp.Height;

            for (var py = 0; py < height; py++)
            {
                var runXStart  = -1;
                string? runInt = null;
                string? runCol = null;

                for (var px = 0; px <= width; px++)
                {
                    string? curIntent = null;
                    string? curColor  = null;

                    if (px < width)
                    {
                        var pixel = bmp.GetPixel(px, py);

                        if (pixel.A >= 128)
                        {
                            var key = (pixel.R, pixel.G, pixel.B);
                            if (palette.TryGetValue(key, out var entry))
                            {
                                curIntent = entry.Intent;
                                curColor  = $"#{pixel.R:X2}{pixel.G:X2}{pixel.B:X2}";
                                nonEmpty++;
                                intents.Add(curIntent);
                            }
                            else
                            {
                                unknown++;
                                if (failOnUnknownOpaque)
                                    errors.Add(
                                        $"Unknown opaque color in layer {layer.Id} at ({px},{py}): " +
                                        $"#{pixel.R:X2}{pixel.G:X2}{pixel.B:X2}");
                            }
                        }
                    }

                    var runChanged = curIntent != runInt;

                    if (runChanged && runXStart >= 0 && runInt != null)
                    {
                        var xe = px - 1;

                        if (isNodeLayer)
                        {
                            for (var nx = runXStart; nx <= xe; nx++)
                                nodes.Add(new System2StaticRoadNode(
                                    PixelX: nx,          PixelY: py,
                                    WorldX: originX + nx, WorldY: originY + py,
                                    Intent: runInt,      Color: runCol!));
                        }
                        else
                        {
                            runs.Add(new System2StaticRoadPixelRun(
                                Y: py, XStart: runXStart, XEnd: xe,
                                WorldY:      originY + py,
                                WorldXStart: originX + runXStart,
                                WorldXEnd:   originX + xe,
                                Intent: runInt, Color: runCol!));
                        }
                    }

                    if (runChanged)
                    {
                        runXStart = curIntent != null ? px : -1;
                        runInt    = curIntent;
                        runCol    = curColor;
                    }
                }
            }
        }

        var summary = new System2StaticRoadLayerSummary
        {
            Id             = layer.Id,
            File           = layer.File,
            LayerClass     = layer.LayerClass,
            NonEmptyPixels = nonEmpty,
            Intents        = intents.OrderBy(i => i).ToList(),
            Runs           = runs,
            Nodes          = nodes,
        };

        return (summary, width, height, unknown, errors);
    }
}
