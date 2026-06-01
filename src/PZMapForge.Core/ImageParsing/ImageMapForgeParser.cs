using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using PZMapForge.Core.Palette;
using PZMapForge.Core.ParsedCell;

namespace PZMapForge.Core.ImageParsing;

/// <summary>
/// Parses a PNG/BMP blockout image through a palette definition, producing a typed
/// ImageMapForgeResult equivalent to the parsed-cell artifact from image-mapforge.ps1.
///
/// Pixel matching algorithm (mirrors PS reference implementation):
///   1. Exact RGB match against palette.
///   2. Nearest-colour by minimum squared RGB distance (cached per unique unmapped colour).
///
/// Windows-only: uses System.Drawing.Common (GDI+) for image loading and pixel access.
/// Claim boundary: planning_artifact_only_not_pz_load_tested
/// </summary>
[SupportedOSPlatform("windows")]
public static class ImageMapForgeParser
{
    private const int RequiredWidth  = 300;
    private const int RequiredHeight = 300;

    /// <summary>
    /// Parse the image using the palette at <paramref name="palettePath"/>.
    /// Computes the palette SHA-256 from the file bytes.
    /// Throws <see cref="ArgumentException"/> on invalid inputs.
    /// </summary>
    public static ImageMapForgeResult Parse(
        string imagePath,
        string palettePath,
        ImageMapForgeOptions? options = null)
    {
        if (!File.Exists(imagePath))
            throw new ArgumentException($"Image file not found: {imagePath}", nameof(imagePath));

        if (!File.Exists(palettePath))
            throw new ArgumentException($"Palette file not found: {palettePath}", nameof(palettePath));

        var ext = Path.GetExtension(imagePath).ToLowerInvariant();
        if (ext is not ".png" and not ".bmp")
            throw new ArgumentException(
                $"Unsupported image extension '{ext}'. Use PNG or BMP.", nameof(imagePath));

        var paletteResult = PaletteLoader.Load(palettePath);
        if (!paletteResult.IsValid)
            throw new ArgumentException(
                $"Palette is invalid: {string.Join("; ", paletteResult.Errors)}", nameof(palettePath));

        var paletteSha256 = ComputeFileSha256(palettePath);
        return Parse(imagePath, paletteResult.Document!, paletteSha256, options);
    }

    // -----------------------------------------------------------------------
    // Internal implementation
    // -----------------------------------------------------------------------

    private static ImageMapForgeResult Parse(
        string imagePath, PaletteDocument palette,
        string paletteSha256, ImageMapForgeOptions? options)
    {
        var opts = options ?? ImageMapForgeOptions.Default;

        // Build palette lookups
        var entries    = BuildEntries(palette);
        var exactMap   = entries.ToDictionary(e => e.RgbKey, StringComparer.Ordinal);

        // Load and optionally resize image
        var sourceBmp = new Bitmap(imagePath);
        Bitmap workBmp;
        bool   resized;

        try
        {
            if (sourceBmp.Width != RequiredWidth || sourceBmp.Height != RequiredHeight)
            {
                if (!opts.Resize)
                    throw new ArgumentException(
                        $"Input image is {sourceBmp.Width}x{sourceBmp.Height}. " +
                        $"Expected {RequiredWidth}x{RequiredHeight}. " +
                        "Re-run with Resize=true to scale deterministically.",
                        nameof(imagePath));

                workBmp = new Bitmap(RequiredWidth, RequiredHeight);
                using var g = Graphics.FromImage(workBmp);
                g.SmoothingMode     = SmoothingMode.None;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode   = PixelOffsetMode.Half;
                g.DrawImage(sourceBmp, 0, 0, RequiredWidth, RequiredHeight);
                resized = true;
            }
            else
            {
                workBmp = new Bitmap(sourceBmp);
                resized = false;
            }
        }
        finally
        {
            sourceBmp.Dispose();
        }

        // Pixel scan
        int w = workBmp.Width, h = workBmp.Height;

        var rows          = new string[h];
        var kindCounts    = entries.ToDictionary(e => e.Kind, _ => 0, StringComparer.Ordinal);
        var colorFreqs    = new Dictionary<string, int>(StringComparer.Ordinal);
        var unmappedFreqs = new Dictionary<string, int>(StringComparer.Ordinal);
        // drift cache: rgbKey → (entry, distance, count)
        var driftCache = new Dictionary<string, (PaletteEntry Entry, double Distance, int Count)>(
            StringComparer.Ordinal);

        int exactPixels = 0, nearestPixels = 0;

        try
        {
            for (int y = 0; y < h; y++)
            {
                var chars = new char[w];
                for (int x = 0; x < w; x++)
                {
                    var px  = workBmp.GetPixel(x, y);
                    var key = $"{px.R},{px.G},{px.B}";

                    // Frequency tracking
                    colorFreqs[key] = colorFreqs.GetValueOrDefault(key, 0) + 1;

                    PaletteEntry entry;
                    if (exactMap.TryGetValue(key, out var exact))
                    {
                        entry = exact;
                        exactPixels++;
                    }
                    else
                    {
                        unmappedFreqs[key] = unmappedFreqs.GetValueOrDefault(key, 0) + 1;

                        if (driftCache.TryGetValue(key, out var cached))
                        {
                            entry = cached.Entry;
                            driftCache[key] = (cached.Entry, cached.Distance, cached.Count + 1);
                        }
                        else
                        {
                            entry = FindNearest(px, entries);
                            var dist = Math.Round(
                                Math.Sqrt(Math.Pow(px.R - entry.R, 2) +
                                          Math.Pow(px.G - entry.G, 2) +
                                          Math.Pow(px.B - entry.B, 2)), 2);
                            driftCache[key] = (entry, dist, 1);
                        }
                        nearestPixels++;
                    }

                    chars[x] = entry.Code;
                    kindCounts[entry.Kind]++;
                }
                rows[y] = new string(chars);
            }
        }
        finally
        {
            workBmp.Dispose();
        }

        // Build counts (sorted by GID ascending, matching PS output)
        var counts = entries
            .Select(e => new ParsedCellCount { Kind = e.Kind, Code = e.Code.ToString(), Gid = e.Gid, Pixels = kindCounts[e.Kind] })
            .ToList();

        // Build drift list sorted by count desc, then rgb asc
        var drift = driftCache
            .OrderByDescending(kv => kv.Value.Count)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => new ParsedCellDrift
            {
                SourceRgb   = kv.Key,
                Count       = kv.Value.Count,
                NearestKind = kv.Value.Entry.Kind,
                NearestRgb  = $"{kv.Value.Entry.R},{kv.Value.Entry.G},{kv.Value.Entry.B}",
                Distance    = kv.Value.Distance,
            })
            .ToList();

        var matching = new ParsedCellMatching
        {
            ExactPixels           = exactPixels,
            NearestPixels         = nearestPixels,
            UniqueSourceColours   = colorFreqs.Count,
            UnmappedExactColours  = unmappedFreqs.Count,
        };

        return new ImageMapForgeResult(
            RequiredWidth, RequiredHeight, resized, paletteSha256,
            rows, counts, matching, drift);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static PaletteEntry[] BuildEntries(PaletteDocument palette)
    {
        return palette.Kinds
            .OrderBy(k => k.Gid)
            .Select(k => new PaletteEntry(
                k.Kind,
                string.IsNullOrEmpty(k.Code) ? ' ' : k.Code[0],
                k.Gid,
                k.Rgb[0], k.Rgb[1], k.Rgb[2]))
            .ToArray();
    }

    private static PaletteEntry FindNearest(Color color, PaletteEntry[] entries)
    {
        PaletteEntry? best = null;
        double bestScore   = double.PositiveInfinity;

        foreach (var e in entries)
        {
            double dr    = color.R - e.R;
            double dg    = color.G - e.G;
            double db    = color.B - e.B;
            double score = dr * dr + dg * dg + db * db;
            if (score < bestScore) { best = e; bestScore = score; }
        }

        return best!;
    }

    private static string ComputeFileSha256(string path)
    {
        using var sha    = System.Security.Cryptography.SHA256.Create();
        using var stream = File.OpenRead(path);
        return BitConverter.ToString(sha.ComputeHash(stream))
            .Replace("-", string.Empty)
            .ToLowerInvariant();
    }

    // -----------------------------------------------------------------------
    // Private palette entry (avoids external dependency on System.Drawing.Color)
    // -----------------------------------------------------------------------

    private sealed class PaletteEntry
    {
        public string Kind   { get; }
        public char   Code   { get; }
        public int    Gid    { get; }
        public int    R      { get; }
        public int    G      { get; }
        public int    B      { get; }
        public string RgbKey { get; }

        public PaletteEntry(string kind, char code, int gid, int r, int g, int b)
        {
            Kind   = kind;
            Code   = code;
            Gid    = gid;
            R      = r;
            G      = g;
            B      = b;
            RgbKey = $"{r},{g},{b}";
        }
    }
}
