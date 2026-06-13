using System.Drawing;
using System.Runtime.Versioning;

namespace PZMapForge.Core.WorldGen;

/// <summary>
/// Shared rectangle extraction engine used by both WorldGenPngCompiler and
/// WorldGenProjectCompiler.
///
/// Two responsibilities:
///   1. ApplyBitmapToGrid — paint a loaded bitmap onto a semantic grid,
///      respecting transparency and palette lookups.
///   2. ExtractModules — run horizontal run-length + vertical merge on the
///      resolved semantic grid to produce WorldGenModule rectangles.
///
/// Windows-only: ApplyBitmapToGrid uses System.Drawing.Common.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class WorldGenRectExtractor
{
    // -----------------------------------------------------------------------
    // Color map builder
    // -----------------------------------------------------------------------

    internal static Dictionary<(byte R, byte G, byte B), WorldGenPngPaletteEntry> BuildColorMap(
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
    // Bitmap → grid
    // -----------------------------------------------------------------------

    // Paints a bitmap onto the semantic grid.
    // Transparent pixels (alpha < 128) leave existing grid entries unchanged.
    // Unknown non-transparent pixels are reported as errors unless IgnoreUnknown.
    // Grid is indexed [x, y] and pixels outside grid bounds are ignored.
    internal static void ApplyBitmapToGrid(
        Bitmap bmp,
        Dictionary<(byte R, byte G, byte B), WorldGenPngPaletteEntry> colorMap,
        (string Type, string Key)?[,] grid,
        WorldGenPngCompileOptions opts,
        List<string> errors)
    {
        var width  = Math.Min(bmp.Width,  grid.GetLength(0));
        var height = Math.Min(bmp.Height, grid.GetLength(1));

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var px = bmp.GetPixel(x, y);
                if (px.A < 128) continue;

                if (colorMap.TryGetValue((px.R, px.G, px.B), out var entry))
                    grid[x, y] = (entry.Type, entry.Key);
                else if (!opts.IgnoreUnknown)
                    errors.Add($"Unknown color at pixel ({x},{y}): RGB({px.R},{px.G},{px.B}). Add to palette or use --ignore-unknown.");
            }
        }
    }

    // -----------------------------------------------------------------------
    // Grid → modules
    // -----------------------------------------------------------------------

    // Extracts WorldGenModules from the resolved semantic grid using horizontal
    // run-length detection followed by vertical rectangle merging.
    internal static List<WorldGenModule> ExtractModules(
        (string Type, string Key)?[,] grid,
        int width, int height,
        int originX, int originY)
    {
        var counter    = 0;
        var output     = new List<WorldGenModule>();
        var active     = new List<ActiveRect>();

        for (var y = 0; y < height; y++)
        {
            var rowSegs    = GetRowSegments(grid, y, width);
            var curKeys    = new HashSet<SegKey>(rowSegs.Select(s => s.Key));
            var nextActive = new List<ActiveRect>(active.Count);

            foreach (var rect in active)
            {
                if (curKeys.Contains(rect.Key))
                    nextActive.Add(rect with { Y2 = y });
                else
                    output.Add(MakeModule(rect, originX, originY, ++counter));
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
        (string Type, string Key)?[,] grid, int y, int width)
    {
        var segments = new List<RowSegment>();
        var segStart = -1;
        (string Type, string Key)? segEntry = null;

        for (var x = 0; x <= width; x++)
        {
            (string Type, string Key)? entry = x < width ? grid[x, y] : null;

            var entryMatches = entry.HasValue    &&
                               segEntry.HasValue &&
                               entry.Value.Type == segEntry.Value.Type &&
                               entry.Value.Key  == segEntry.Value.Key;

            if (segEntry.HasValue && !entryMatches)
            {
                segments.Add(new RowSegment(
                    new SegKey(segStart, x - 1, segEntry.Value.Type, segEntry.Value.Key)));
                segEntry = null;
                segStart = -1;
            }

            if (entry.HasValue && (segEntry == null || !entryMatches))
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
    // Internal types
    // -----------------------------------------------------------------------

    internal readonly record struct SegKey(int X1, int X2, string Type, string WorldGenKey);
    internal record struct ActiveRect(SegKey Key, int Y1, int Y2);
    private  readonly record struct RowSegment(SegKey Key);
}
