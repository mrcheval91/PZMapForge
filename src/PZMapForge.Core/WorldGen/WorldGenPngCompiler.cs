using System.Drawing;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

/// <summary>
/// Converts a PNG color layer into a WorldGenManifest using horizontal run-length
/// detection followed by vertical rectangle merging.
///
/// Algorithm:
///   1. Scan each row for contiguous same-color segments (run-length).
///   2. For each row, extend any active rectangle whose (x1, x2, type, key) matches
///      a segment in the current row, or flush it if no match.
///   3. Emit world coordinates: world_x = origin_x + pixel_x, world_y = origin_y + pixel_y.
///
/// Windows-only: uses System.Drawing.Common for PNG access.
/// Claim boundary: planning_artifact_only_not_pz_load_tested
/// </summary>
[SupportedOSPlatform("windows")]
public static class WorldGenPngCompiler
{
    private const string ManifestFormat = "pzmapforge.worldgen.layers.v1";
    private const string OriginNote     = "Generated from PNG layer. Coordinates are world tile coordinates.";

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static WorldGenPngCompileResult Compile(
        string pngPath,
        string palettePath,
        string mapId,
        int    originX,
        int    originY,
        WorldGenPngCompileOptions? options = null)
    {
        var result = new WorldGenPngCompileResult();
        var opts   = options ?? WorldGenPngCompileOptions.Default;

        if (string.IsNullOrWhiteSpace(mapId))
        {
            result.Errors.Add("map_id is missing or empty.");
            return result;
        }

        var paletteResult = WorldGenPngPaletteLoader.Load(palettePath);
        if (!paletteResult.IsValid)
        {
            foreach (var e in paletteResult.Errors) result.Errors.Add($"palette: {e}");
            return result;
        }

        if (!File.Exists(pngPath))
        {
            result.Errors.Add($"PNG file not found: {pngPath}");
            return result;
        }

        var colorMap = BuildColorMap(paletteResult.Palette!);

        Bitmap bmp;
        try { bmp = new Bitmap(pngPath); }
        catch (Exception ex)
        {
            result.Errors.Add($"PNG load error: {ex.Message}");
            return result;
        }

        List<WorldGenModule> modules;
        using (bmp)
            modules = ExtractModules(bmp, colorMap, originX, originY, opts, result.Errors);

        if (!result.IsValid) return result;

        result.Manifest    = new WorldGenManifest
        {
            MapId         = mapId,
            Format        = ManifestFormat,
            OriginNote    = OriginNote,
            StaticModules = modules,
        };
        result.ModuleCount = modules.Count;
        return result;
    }

    public static string SerializeManifest(WorldGenManifest manifest) =>
        JsonSerializer.Serialize(manifest, WriteOptions);

    // -----------------------------------------------------------------------
    // Internal: color map
    // -----------------------------------------------------------------------

    private static Dictionary<(byte R, byte G, byte B), WorldGenPngPaletteEntry> BuildColorMap(
        WorldGenPngPalette palette)
    {
        var map = new Dictionary<(byte, byte, byte), WorldGenPngPaletteEntry>();
        foreach (var entry in palette.Entries)
        {
            WorldGenPngPaletteLoader.TryParseHexColor(entry.Color, out var r, out var g, out var b);
            map[(r, g, b)] = entry;
        }
        return map;
    }

    // -----------------------------------------------------------------------
    // Internal: rectangle extraction
    // -----------------------------------------------------------------------

    private static List<WorldGenModule> ExtractModules(
        Bitmap bmp,
        Dictionary<(byte R, byte G, byte B), WorldGenPngPaletteEntry> colorMap,
        int originX, int originY,
        WorldGenPngCompileOptions opts,
        List<string> errors)
    {
        var width   = bmp.Width;
        var height  = bmp.Height;
        var counter = 0;
        var output  = new List<WorldGenModule>();

        // active: segment key (x1, x2, type, worldgenKey) → (y1, y2)
        var active = new List<ActiveRect>();

        for (var y = 0; y < height; y++)
        {
            var rowSegs   = GetRowSegments(bmp, y, width, colorMap, opts, errors);
            var curKeys   = new HashSet<SegKey>(rowSegs.Select(s => s.Key));
            var nextActive = new List<ActiveRect>(active.Count);

            foreach (var rect in active)
            {
                if (curKeys.Contains(rect.Key))
                    nextActive.Add(rect with { Y2 = y });   // extend
                else
                    output.Add(MakeModule(rect, originX, originY, ++counter));  // flush
            }

            var extendedKeys = new HashSet<SegKey>(nextActive.Select(a => a.Key));
            foreach (var seg in rowSegs)
            {
                if (!extendedKeys.Contains(seg.Key))
                    nextActive.Add(new ActiveRect(seg.Key, y, y));
            }

            active = nextActive;
        }

        foreach (var rect in active)
            output.Add(MakeModule(rect, originX, originY, ++counter));

        return output;
    }

    private static List<RowSegment> GetRowSegments(
        Bitmap bmp, int y, int width,
        Dictionary<(byte R, byte G, byte B), WorldGenPngPaletteEntry> colorMap,
        WorldGenPngCompileOptions opts,
        List<string> errors)
    {
        var segments = new List<RowSegment>();
        var segStart = -1;
        WorldGenPngPaletteEntry? segEntry = null;

        for (var x = 0; x <= width; x++)
        {
            WorldGenPngPaletteEntry? entry = null;

            if (x < width)
            {
                var px = bmp.GetPixel(x, y);
                if (px.A >= 128)
                {
                    if (colorMap.TryGetValue((px.R, px.G, px.B), out var found))
                        entry = found;
                    else if (!opts.IgnoreUnknown)
                        errors.Add($"Unknown color at pixel ({x},{y}): RGB({px.R},{px.G},{px.B}). Add to palette or use --ignore-unknown.");
                }
            }

            var entryMatches = entry  != null &&
                               segEntry != null &&
                               entry.Type == segEntry.Type &&
                               entry.Key  == segEntry.Key;

            if (segEntry != null && !entryMatches)
            {
                segments.Add(new RowSegment(new SegKey(segStart, x - 1, segEntry.Type, segEntry.Key)));
                segEntry = null;
                segStart = -1;
            }

            if (entry != null && (segEntry == null || !entryMatches))
            {
                segStart = x;
                segEntry = entry;
            }
        }

        return segments;
    }

    private static WorldGenModule MakeModule(ActiveRect rect, int originX, int originY, int counter) =>
        new()
        {
            Id   = $"{rect.Key.WorldGenKey}_{counter:D6}",
            Type = rect.Key.Type,
            Key  = rect.Key.WorldGenKey,
            X1   = originX + rect.Key.X1,
            Y1   = originY + rect.Y1,
            X2   = originX + rect.Key.X2,
            Y2   = originY + rect.Y2,
        };

    // -----------------------------------------------------------------------
    // Private types
    // -----------------------------------------------------------------------

    private readonly record struct SegKey(int X1, int X2, string Type, string WorldGenKey);

    private record struct ActiveRect(SegKey Key, int Y1, int Y2);

    private readonly record struct RowSegment(SegKey Key);
}
