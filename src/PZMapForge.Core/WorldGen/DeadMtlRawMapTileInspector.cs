using System.Drawing;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

[SupportedOSPlatform("windows")]
public static class DeadMtlRawMapTileInspector
{
    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };

    public static DeadMtlRawMapTileInspectionResult Inspect(
        string imagePath,
        string worldgenPalettePath = "",
        string system2PalettePath  = "",
        int expectedWidth          = 256,
        int expectedHeight         = 256,
        int topN                   = 32,
        int unknownColorCap        = 50)
    {
        var result = new DeadMtlRawMapTileInspectionResult();

        if (!File.Exists(imagePath))
        {
            result.Errors.Add($"Image file not found: {imagePath}");
            return result;
        }

        // SHA256 of raw file bytes
        string sha256;
        try
        {
            using var fs = File.OpenRead(imagePath);
            sha256 = string.Join("", SHA256.HashData(fs).Select(b => b.ToString("x2")));
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to hash image file: {ex.Message}");
            return result;
        }

        // Load PNG
        Bitmap bmp;
        try
        {
            bmp = new Bitmap(imagePath);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to load image: {ex.Message}");
            return result;
        }

        int width  = bmp.Width;
        int height = bmp.Height;
        bool sizeValid = width == expectedWidth && height == expectedHeight;

        // Count pixels — transparent bucket is separate to avoid collision with black (#000000, key=0)
        var opaqueColorCounts = new Dictionary<int, int>();
        int transparentCount  = 0;
        bool hasAlpha         = false;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var px = bmp.GetPixel(x, y);
                if (px.A != 255) hasAlpha = true;
                if (px.A == 0)
                {
                    transparentCount++;
                }
                else
                {
                    int key = (px.R << 16) | (px.G << 8) | px.B;
                    opaqueColorCounts.TryGetValue(key, out var c);
                    opaqueColorCounts[key] = c + 1;
                }
            }
        }

        bmp.Dispose();

        int opaqueCount = opaqueColorCounts.Values.Sum();
        int totalPixels = width * height;

        // Unique opaque colors
        var opaqueColors = opaqueColorCounts.ToList();
        int uniqueColorCount = opaqueColors.Count;

        // Top N colors by count
        var sorted = opaqueColors
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .Take(topN)
            .Select(kv => new DeadMtlRawMapTileTopColor
            {
                Color      = $"#{(kv.Key >> 16) & 0xFF:X2}{(kv.Key >> 8) & 0xFF:X2}{kv.Key & 0xFF:X2}",
                Count      = kv.Value,
                Percentage = totalPixels > 0
                    ? Math.Round(kv.Value * 100.0 / totalPixels, 2)
                    : 0.0,
            })
            .ToList();

        // Load palettes
        var worldgenColors  = LoadWorldgenPaletteColors(worldgenPalettePath);
        var system2Colors   = LoadSystem2PaletteColors(system2PalettePath);
        bool hasPalettes    = worldgenColors.Count > 0 || system2Colors.Count > 0;

        // Classify opaque colors
        int wgMatches  = 0;
        int s2Matches  = 0;
        var unknownColors = new List<string>();

        foreach (var (key, _) in opaqueColors)
        {
            var hex = $"#{(key >> 16) & 0xFF:X2}{(key >> 8) & 0xFF:X2}{key & 0xFF:X2}";
            bool inWg = worldgenColors.Contains(hex, StringComparer.OrdinalIgnoreCase);
            bool inS2 = system2Colors.Contains(hex, StringComparer.OrdinalIgnoreCase);

            if (inWg) wgMatches++;
            if (inS2) s2Matches++;
            if (!inWg && !inS2 && hasPalettes)
            {
                if (unknownColors.Count < unknownColorCap)
                    unknownColors.Add(hex);
            }
        }

        int unknownCount = hasPalettes
            ? opaqueColors.Count(kv =>
            {
                var hex = $"#{(kv.Key >> 16) & 0xFF:X2}{(kv.Key >> 8) & 0xFF:X2}{kv.Key & 0xFF:X2}";
                return !worldgenColors.Contains(hex, StringComparer.OrdinalIgnoreCase)
                    && !system2Colors.Contains(hex, StringComparer.OrdinalIgnoreCase);
            })
            : 0;

        result.WorldgenColors = worldgenColors.AsReadOnly();
        result.System2Colors  = system2Colors.AsReadOnly();
        result.IsValid   = true;
        result.Inspection = new DeadMtlRawMapTileInspection
        {
            SourceImage          = imagePath,
            Sha256               = sha256,
            Width                = width,
            Height               = height,
            ExpectedWidth        = expectedWidth,
            ExpectedHeight       = expectedHeight,
            SizeValid            = sizeValid,
            HasAlpha             = hasAlpha,
            OpaquePixelCount     = opaqueCount,
            TransparentPixelCount = transparentCount,
            UniqueColorCount     = uniqueColorCount,
            TopColors            = sorted,
            PaletteMatches       = new DeadMtlRawMapTilePaletteMatches
            {
                WorldgenPaletteMatchCount           = wgMatches,
                System2StaticRoadPaletteMatchCount  = s2Matches,
                UnknownColorCount                   = unknownCount,
            },
            UnknownColors = unknownColors,
        };
        return result;
    }

    public static string RenderMarkdown(DeadMtlRawMapTileInspection insp)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-23A: DeadMTL Raw 256x256 Map Tile Inspection");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** This is inspection only. " +
                      "No compilation, no runtime proof, no writer readiness.");
        sb.AppendLine();
        sb.AppendLine("## Source");
        sb.AppendLine();
        sb.AppendLine($"- source_image: `{insp.SourceImage}`");
        sb.AppendLine($"- sha256: `{insp.Sha256}`");
        sb.AppendLine($"- status: {insp.Status}");
        sb.AppendLine($"- runtime_status: {insp.RuntimeStatus}");
        sb.AppendLine();
        sb.AppendLine("## Size");
        sb.AppendLine();
        sb.AppendLine($"| Field | Value |");
        sb.AppendLine($"|---|---|");
        sb.AppendLine($"| width | {insp.Width} |");
        sb.AppendLine($"| height | {insp.Height} |");
        sb.AppendLine($"| expected_width | {insp.ExpectedWidth} |");
        sb.AppendLine($"| expected_height | {insp.ExpectedHeight} |");
        sb.AppendLine($"| size_valid | {insp.SizeValid} |");
        sb.AppendLine();
        sb.AppendLine("## Pixel statistics");
        sb.AppendLine();
        sb.AppendLine($"| Field | Value |");
        sb.AppendLine($"|---|---|");
        sb.AppendLine($"| has_alpha | {insp.HasAlpha} |");
        sb.AppendLine($"| opaque_pixel_count | {insp.OpaquePixelCount} |");
        sb.AppendLine($"| transparent_pixel_count | {insp.TransparentPixelCount} |");
        sb.AppendLine($"| unique_color_count | {insp.UniqueColorCount} |");
        sb.AppendLine();
        sb.AppendLine("## Palette matches");
        sb.AppendLine();
        sb.AppendLine($"| Palette | Matches |");
        sb.AppendLine($"|---|---|");
        sb.AppendLine($"| worldgen_palette | {insp.PaletteMatches.WorldgenPaletteMatchCount} |");
        sb.AppendLine($"| system2_static_road_palette | {insp.PaletteMatches.System2StaticRoadPaletteMatchCount} |");
        sb.AppendLine($"| unknown_colors | {insp.PaletteMatches.UnknownColorCount} |");
        sb.AppendLine();
        if (insp.UnknownColors.Count > 0)
        {
            sb.AppendLine("## Unknown colors (sample)");
            sb.AppendLine();
            foreach (var c in insp.UnknownColors)
                sb.AppendLine($"- `{c}`");
            sb.AppendLine();
        }
        sb.AppendLine("## Top colors");
        sb.AppendLine();
        sb.AppendLine("| color | count | percentage |");
        sb.AppendLine("|---|---|---|");
        foreach (var tc in insp.TopColors)
            sb.AppendLine($"| `{tc.Color}` | {tc.Count} | {tc.Percentage:F2}% |");
        sb.AppendLine();
        sb.AppendLine("## Claim boundary");
        sb.AppendLine();
        sb.AppendLine("- writes_lotpack: false");
        sb.AppendLine("- writes_worldgen_lua: false");
        sb.AppendLine("- runtime_proven: false");
        sb.AppendLine("- public_playable_claim: false");
        sb.AppendLine("- writer_ready_claim: false");
        sb.AppendLine();
        sb.AppendLine("## VERDICT");
        sb.AppendLine();
        sb.AppendLine("MAP23A_RAW_256_MAP_TILE_INSPECTION_COMPLETE");
        return sb.ToString();
    }

    public static string RenderCsv(
        DeadMtlRawMapTileInspection insp,
        IReadOnlyList<string>? worldgenColors = null,
        IReadOnlyList<string>? system2Colors  = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("hex_color,count,percentage,in_worldgen_palette,in_system2_palette,is_unknown");
        var wg = worldgenColors ?? Array.Empty<string>();
        var s2 = system2Colors  ?? Array.Empty<string>();
        bool hasPalettes = wg.Count > 0 || s2.Count > 0;
        foreach (var tc in insp.TopColors)
        {
            bool inWg      = wg.Contains(tc.Color, StringComparer.OrdinalIgnoreCase);
            bool inS2      = s2.Contains(tc.Color, StringComparer.OrdinalIgnoreCase);
            bool isUnknown = hasPalettes && !inWg && !inS2;
            sb.AppendLine(
                $"{tc.Color},{tc.Count},{tc.Percentage:F2}," +
                $"{inWg.ToString().ToLowerInvariant()}," +
                $"{inS2.ToString().ToLowerInvariant()}," +
                $"{isUnknown.ToString().ToLowerInvariant()}");
        }
        return sb.ToString();
    }

    private static List<string> LoadWorldgenPaletteColors(string path)
    {
        var colors = new List<string>();
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return colors;
        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            using var doc = JsonDocument.Parse(json, DocOpts);
            if (doc.RootElement.TryGetProperty("entries", out var entries)
                && entries.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in entries.EnumerateArray())
                {
                    if (e.TryGetProperty("color", out var c))
                    {
                        var hex = c.GetString() ?? "";
                        if (!string.IsNullOrEmpty(hex)) colors.Add(hex);
                    }
                }
            }
        }
        catch { /* best effort */ }
        return colors;
    }

    private static List<string> LoadSystem2PaletteColors(string path)
    {
        var colors = new List<string>();
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return colors;
        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            using var doc = JsonDocument.Parse(json, DocOpts);
            if (doc.RootElement.TryGetProperty("entries", out var entries)
                && entries.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in entries.EnumerateArray())
                {
                    if (e.TryGetProperty("hex", out var h))
                    {
                        var hex = h.GetString() ?? "";
                        if (!string.IsNullOrEmpty(hex)) colors.Add(hex);
                    }
                }
            }
        }
        catch { /* best effort */ }
        return colors;
    }
}
