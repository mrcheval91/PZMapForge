using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PZMapForge.Core.Regions;

/// <summary>
/// Writes regions.json and regions-report.md from a RegionExtractionResult.
/// Matches the artifact family produced by scripts/extract-regions.ps1.
/// Claim boundary: planning_artifact_only_not_pz_load_tested
/// </summary>
public static class RegionArtifactWriter
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy   = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Writes regions.json and regions-report.md to <paramref name="outputDir"/>.
    /// Returns (jsonPath, mdPath).
    /// </summary>
    public static (string JsonPath, string MdPath) Write(
        string                 outputDir,
        int                    width,
        int                    height,
        string                 sourcePath,
        RegionExtractionResult result)
    {
        Directory.CreateDirectory(outputDir);

        var regions = result.Regions.Select(r => new RegionEntry
        {
            RegionId   = r.RegionId,
            Kind       = r.Kind,
            Code       = r.Code.ToString(),
            PixelCount = r.PixelCount,
            Bounds     = new BoundsEntry { X = r.Bounds.X, Y = r.Bounds.Y, Width = r.Bounds.Width, Height = r.Bounds.Height },
            Centroid   = new CentroidEntry { X = r.Centroid.X, Y = r.Centroid.Y },
        }).ToList();

        var summary = result.SummaryByKind.Select(s => new KindSummaryEntry
        {
            Kind                = s.Kind,
            Code                = s.Code.ToString(),
            RegionCount         = s.RegionCount,
            TotalPixels         = s.TotalPixels,
            LargestRegionPixels = s.LargestRegionPixels,
        }).ToList();

        var doc = new RegionDoc
        {
            Schema        = "pzmapforge.regions.v0.1",
            ClaimBoundary = "planning_artifact_only_not_pz_load_tested",
            Source        = sourcePath,
            Width         = width,
            Height        = height,
            TotalRegions  = result.TotalRegions,
            Regions       = regions,
            SummaryByKind = summary,
        };

        var jsonPath = Path.Combine(outputDir, "regions.json");
        using (var fs = File.Create(jsonPath))
            JsonSerializer.Serialize(fs, doc, JsonOpts);

        var mdPath = Path.Combine(outputDir, "regions-report.md");
        File.WriteAllText(mdPath, BuildMarkdown(sourcePath, width, height, result),
            Encoding.UTF8);

        return (jsonPath, mdPath);
    }

    // -----------------------------------------------------------------------
    // Markdown
    // -----------------------------------------------------------------------

    private static string BuildMarkdown(
        string sourcePath, int width, int height,
        RegionExtractionResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Regions Report");
        sb.AppendLine();
        sb.AppendLine($"Source: {sourcePath}");
        sb.AppendLine($"Dimensions: {width}x{height}");
        sb.AppendLine($"Total regions: {result.TotalRegions}");
        sb.AppendLine();
        sb.AppendLine("## Claim boundary");
        sb.AppendLine();
        sb.AppendLine("planning_artifact_only_not_pz_load_tested");
        sb.AppendLine();
        sb.AppendLine("## Summary by kind");
        sb.AppendLine();
        sb.AppendLine("| Kind | Code | Regions | Total pixels | Largest region |");
        sb.AppendLine("|---|---:|---:|---:|---:|");
        foreach (var s in result.SummaryByKind)
            sb.AppendLine($"| {s.Kind} | {s.Code} | {s.RegionCount} | {s.TotalPixels} | {s.LargestRegionPixels} |");

        var top20 = result.Regions.Take(20).ToList();
        if (top20.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Top regions by sort order");
            sb.AppendLine();
            sb.AppendLine("| ID | Kind | Code | Pixels | Bounds (x,y WxH) | Centroid |");
            sb.AppendLine("|---|---|---:|---:|---|---|");
            foreach (var r in top20)
            {
                var b = r.Bounds;
                var c = r.Centroid;
                sb.AppendLine(
                    $"| {r.RegionId} | {r.Kind} | {r.Code} | {r.PixelCount} | " +
                    $"({b.X},{b.Y}) {b.Width}x{b.Height} | ({c.X},{c.Y}) |");
            }
        }

        return sb.ToString();
    }

    // -----------------------------------------------------------------------
    // Private POCOs
    // -----------------------------------------------------------------------

    private sealed class RegionDoc
    {
        public string             Schema        { get; init; } = "";
        public string             ClaimBoundary { get; init; } = "";
        public string             Source        { get; init; } = "";
        public int                Width         { get; init; }
        public int                Height        { get; init; }
        public int                TotalRegions  { get; init; }
        public List<RegionEntry>      Regions       { get; init; } = [];
        public List<KindSummaryEntry> SummaryByKind { get; init; } = [];
    }

    private sealed class RegionEntry
    {
        public int           RegionId   { get; init; }
        public string        Kind       { get; init; } = "";
        public string        Code       { get; init; } = "";
        public int           PixelCount { get; init; }
        public BoundsEntry   Bounds     { get; init; } = new();
        public CentroidEntry Centroid   { get; init; } = new();
    }

    private sealed class KindSummaryEntry
    {
        public string Kind                { get; init; } = "";
        public string Code                { get; init; } = "";
        public int    RegionCount         { get; init; }
        public int    TotalPixels         { get; init; }
        public int    LargestRegionPixels { get; init; }
    }

    private sealed class BoundsEntry
    {
        public int X { get; init; }
        public int Y { get; init; }
        public int Width  { get; init; }
        public int Height { get; init; }
    }

    private sealed class CentroidEntry
    {
        public double X { get; init; }
        public double Y { get; init; }
    }
}
