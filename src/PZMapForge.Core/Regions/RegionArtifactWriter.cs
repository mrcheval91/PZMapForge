using System.Text.Json;
using System.Text.Json.Serialization;

namespace PZMapForge.Core.Regions;

/// <summary>
/// Serializes a RegionExtractionResult to regions.json.
/// Matches the schema produced by scripts/extract-regions.ps1.
/// Claim boundary: planning_artifact_only_not_pz_load_tested
/// </summary>
public static class RegionArtifactWriter
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy       = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented              = true,
        DefaultIgnoreCondition     = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Writes regions.json to <paramref name="outputDir"/> and returns the full path.
    /// </summary>
    public static string Write(
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
            Kind                 = s.Kind,
            Code                 = s.Code.ToString(),
            RegionCount          = s.RegionCount,
            TotalPixels          = s.TotalPixels,
            LargestRegionPixels  = s.LargestRegionPixels,
        }).ToList();

        var doc = new RegionDoc
        {
            Schema         = "pzmapforge.regions.v0.1",
            ClaimBoundary  = "planning_artifact_only_not_pz_load_tested",
            Source         = sourcePath,
            Width          = width,
            Height         = height,
            TotalRegions   = result.TotalRegions,
            Regions        = regions,
            SummaryByKind  = summary,
        };

        var jsonPath = Path.Combine(outputDir, "regions.json");
        using var fs = File.Create(jsonPath);
        JsonSerializer.Serialize(fs, doc, JsonOpts);
        return jsonPath;
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
        public int         RegionId   { get; init; }
        public string      Kind       { get; init; } = "";
        public string      Code       { get; init; } = "";
        public int         PixelCount { get; init; }
        public BoundsEntry   Bounds   { get; init; } = new();
        public CentroidEntry Centroid { get; init; } = new();
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
        public int X      { get; init; }
        public int Y      { get; init; }
        public int Width  { get; init; }
        public int Height { get; init; }
    }

    private sealed class CentroidEntry
    {
        public double X { get; init; }
        public double Y { get; init; }
    }
}
