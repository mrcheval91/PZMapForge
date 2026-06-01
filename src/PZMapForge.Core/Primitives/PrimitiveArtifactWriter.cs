using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PZMapForge.Core.Regions;

namespace PZMapForge.Core.Primitives;

/// <summary>
/// Writes primitives.json and primitives-report.md from a PrimitiveClassificationResult.
/// Matches the artifact family produced by scripts/classify-primitives.ps1.
/// Claim boundary: planning_artifact_only_not_pz_load_tested
/// </summary>
public static class PrimitiveArtifactWriter
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy   = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Writes primitives.json and primitives-report.md to <paramref name="outputDir"/>.
    /// Returns (jsonPath, mdPath).
    /// </summary>
    public static (string JsonPath, string MdPath) Write(
        string                        outputDir,
        int                           width,
        int                           height,
        string                        sourcePath,
        PrimitiveClassificationResult result)
    {
        Directory.CreateDirectory(outputDir);

        var primitives = result.Primitives.Select(p => new PrimitiveEntry
        {
            PrimitiveId    = p.PrimitiveId,
            PrimitiveType  = p.PrimitiveTypeStr,
            SourceRegionId = p.SourceRegionId,
            Kind           = p.Kind,
            Code           = p.Code.ToString(),
            PixelCount     = p.PixelCount,
            Bounds         = new BoundsEntry { X = p.Bounds.X, Y = p.Bounds.Y, Width = p.Bounds.Width, Height = p.Bounds.Height },
            Centroid       = new CentroidEntry { X = p.Centroid.X, Y = p.Centroid.Y },
            PlanningRole   = p.PlanningRole,
        }).ToList();

        var summary = result.SummaryByPrimitiveType.Select(s => new TypeSummaryEntry
        {
            PrimitiveType       = s.PrimitiveTypeString,
            RegionCount         = s.RegionCount,
            TotalPixels         = s.TotalPixels,
            LargestRegionPixels = s.LargestRegionPixels,
        }).ToList();

        var doc = new PrimitiveDoc
        {
            Schema                 = "pzmapforge.primitives.v0.1",
            ClaimBoundary          = "planning_artifact_only_not_pz_load_tested",
            Source                 = sourcePath,
            Width                  = width,
            Height                 = height,
            PrimitiveCount         = result.PrimitiveCount,
            Primitives             = primitives,
            SummaryByPrimitiveType = summary,
        };

        var jsonPath = Path.Combine(outputDir, "primitives.json");
        using (var fs = File.Create(jsonPath))
            JsonSerializer.Serialize(fs, doc, JsonOpts);

        var mdPath = Path.Combine(outputDir, "primitives-report.md");
        File.WriteAllText(mdPath, BuildMarkdown(sourcePath, width, height, result),
            Encoding.UTF8);

        return (jsonPath, mdPath);
    }

    // -----------------------------------------------------------------------
    // Markdown
    // -----------------------------------------------------------------------

    private static string BuildMarkdown(
        string sourcePath, int width, int height,
        PrimitiveClassificationResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Primitives Report");
        sb.AppendLine();
        sb.AppendLine($"Source: {sourcePath}");
        sb.AppendLine($"Dimensions: {width}x{height}");
        sb.AppendLine($"Total primitives: {result.PrimitiveCount}");
        sb.AppendLine();
        sb.AppendLine("## Claim boundary");
        sb.AppendLine();
        sb.AppendLine("planning_artifact_only_not_pz_load_tested");
        sb.AppendLine();
        sb.AppendLine("## Summary by primitive type");
        sb.AppendLine();
        sb.AppendLine("| Primitive type | Regions | Total pixels | Largest region |");
        sb.AppendLine("|---|---:|---:|---:|");
        foreach (var s in result.SummaryByPrimitiveType)
            sb.AppendLine($"| {s.PrimitiveTypeString} | {s.RegionCount} | {s.TotalPixels} | {s.LargestRegionPixels} |");

        var top20 = result.Primitives.Take(20).ToList();
        if (top20.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Top primitives by sort order");
            sb.AppendLine();
            sb.AppendLine("| ID | Type | Kind | Pixels | Bounds (x,y WxH) | Role |");
            sb.AppendLine("|---|---|---|---:|---|---|");
            foreach (var p in top20)
            {
                var b = p.Bounds;
                sb.AppendLine(
                    $"| {p.PrimitiveId} | {p.PrimitiveTypeStr} | {p.Kind} | {p.PixelCount} | " +
                    $"({b.X},{b.Y}) {b.Width}x{b.Height} | {p.PlanningRole} |");
            }
        }

        return sb.ToString();
    }

    // -----------------------------------------------------------------------
    // Private POCOs
    // -----------------------------------------------------------------------

    private sealed class PrimitiveDoc
    {
        public string                 Schema                 { get; init; } = "";
        public string                 ClaimBoundary          { get; init; } = "";
        public string                 Source                 { get; init; } = "";
        public int                    Width                  { get; init; }
        public int                    Height                 { get; init; }
        public int                    PrimitiveCount         { get; init; }
        public List<PrimitiveEntry>   Primitives             { get; init; } = [];
        public List<TypeSummaryEntry> SummaryByPrimitiveType { get; init; } = [];
    }

    private sealed class PrimitiveEntry
    {
        public int           PrimitiveId    { get; init; }
        public string        PrimitiveType  { get; init; } = "";
        public int           SourceRegionId { get; init; }
        public string        Kind           { get; init; } = "";
        public string        Code           { get; init; } = "";
        public int           PixelCount     { get; init; }
        public BoundsEntry   Bounds         { get; init; } = new();
        public CentroidEntry Centroid       { get; init; } = new();
        public string        PlanningRole   { get; init; } = "";
    }

    private sealed class TypeSummaryEntry
    {
        public string PrimitiveType       { get; init; } = "";
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
