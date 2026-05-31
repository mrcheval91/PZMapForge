using System.Text.Json;
using System.Text.Json.Serialization;

namespace PZMapForge.Core.Planning;

/// <summary>
/// Writes PlanningRuleResult to a deterministic JSON artifact and markdown report.
/// Caller is responsible for choosing (and validating) the output directory.
/// Does not touch media/maps. Does not claim playable PZ export.
/// Claim boundary: planning_artifact_only_not_pz_load_tested
/// </summary>
public static class PlanningArtifactWriter
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented        = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Writes plan-recommendations.json and plan-report.md to <paramref name="outputDir"/>.
    /// Returns (jsonPath, mdPath).
    /// Pass <paramref name="overrideGeneratedAt"/> in tests to get deterministic output.
    /// </summary>
    public static (string JsonPath, string MdPath) Write(
        string          outputDir,
        int             width,
        int             height,
        string          parsedCellPath,
        string          generatedBy,
        PlanningRuleResult result,
        DateTimeOffset? overrideGeneratedAt = null)
    {
        Directory.CreateDirectory(outputDir);

        var ts      = overrideGeneratedAt ?? DateTimeOffset.UtcNow;
        var jsonDoc = BuildDocument(ts, width, height, parsedCellPath, generatedBy, result);

        var jsonPath = Path.Combine(outputDir, "plan-recommendations.json");
        var mdPath   = Path.Combine(outputDir, "plan-report.md");

        using (var fs = File.Create(jsonPath))
            JsonSerializer.Serialize(fs, jsonDoc, JsonOpts);

        File.WriteAllText(mdPath, BuildMarkdown(ts, parsedCellPath, result), System.Text.Encoding.UTF8);

        return (jsonPath, mdPath);
    }

    // -----------------------------------------------------------------------
    // JSON document builder
    // -----------------------------------------------------------------------

    private static PlanDoc BuildDocument(
        DateTimeOffset ts, int width, int height,
        string parsedCellPath, string generatedBy,
        PlanningRuleResult result)
    {
        var recs = result.Recommendations
            .Select((r, i) => new RecEntry
            {
                RecommendationId  = i + 1,
                RecommendationType = r.RecommendationTypeStr,
                Severity          = r.Severity.ToString().ToLowerInvariant(),
                PlanningRole      = r.PlanningRole,
                SourcePrimitiveId = r.SourcePrimitiveId,
                PrimitiveType     = r.SourcePrimitiveTypeStr,
                PixelCount        = r.PixelCount,
                Bounds            = r.Bounds is null ? null : new BoundsEntry
                {
                    X = r.Bounds.X, Y = r.Bounds.Y,
                    Width = r.Bounds.Width, Height = r.Bounds.Height
                },
                Message = $"{r.RecommendationTypeStr}: {r.PlanningRole}",
            })
            .ToList();

        var s = result.Summary;
        var summary = new SummaryEntry
        {
            TotalPixels        = s.TotalPixels,
            PrimitiveCount     = s.PrimitiveCount,
            RecommendationCount = s.RecommendationCount,
            WarningCount       = s.WarningCount,
            CountsByRecommendationType = s.CountsByRecommendationType,
            CountsBySeverity   = s.CountsBySeverity,
        };

        return new PlanDoc
        {
            Schema             = "pzmapforge.plan-recommendations.v0.1",
            ClaimBoundary      = result.ClaimBoundary,
            GeneratedAtUtc     = ts.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Source             = new SourceEntry { ParsedCellPath = parsedCellPath, GeneratedBy = generatedBy },
            Width              = width,
            Height             = height,
            PrimitiveCount     = s.PrimitiveCount,
            RecommendationCount = result.RecommendationCount,
            WarningCount       = s.WarningCount,
            Recommendations    = recs,
            Summary            = summary,
        };
    }

    // -----------------------------------------------------------------------
    // Markdown builder
    // -----------------------------------------------------------------------

    private static string BuildMarkdown(
        DateTimeOffset ts, string parsedCellPath, PlanningRuleResult result)
    {
        var sb = new System.Text.StringBuilder();
        var s  = result.Summary;

        sb.AppendLine("# Plan Recommendations");
        sb.AppendLine();
        sb.AppendLine($"Generated: {ts:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Source: {parsedCellPath}");
        sb.AppendLine($"Schema: pzmapforge.plan-recommendations.v0.1");
        sb.AppendLine();
        sb.AppendLine("## Claim boundary");
        sb.AppendLine();
        sb.AppendLine(result.ClaimBoundary);
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"| Field | Value |");
        sb.AppendLine($"|---|---|");
        sb.AppendLine($"| Primitives | {s.PrimitiveCount} |");
        sb.AppendLine($"| Recommendations | {s.RecommendationCount} |");
        sb.AppendLine($"| Warnings | {s.WarningCount} |");
        sb.AppendLine($"| Total pixels | {s.TotalPixels} |");
        sb.AppendLine();

        // Warnings first
        var warnings = result.Recommendations
            .Where(r => r.Severity == PlanningSeverity.Warning)
            .ToList();

        if (warnings.Count > 0)
        {
            sb.AppendLine("## Warnings");
            sb.AppendLine();
            foreach (var w in warnings)
                sb.AppendLine($"- **{w.RecommendationTypeStr}** (primitive {w.SourcePrimitiveId}): {w.PlanningRole}");
            sb.AppendLine();
        }

        // Group by type
        sb.AppendLine("## Recommendations by type");
        sb.AppendLine();
        var byType = result.Recommendations
            .GroupBy(r => r.RecommendationTypeStr)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        foreach (var group in byType)
        {
            sb.AppendLine($"### {group.Key} ({group.Count()})");
            sb.AppendLine();
            sb.AppendLine("| Severity | Source ID | Pixels | Bounds | Role |");
            sb.AppendLine("|---|---|---:|---|---|");
            foreach (var r in group)
            {
                var bounds = r.Bounds is null ? "-" : $"({r.Bounds.X},{r.Bounds.Y}) {r.Bounds.Width}x{r.Bounds.Height}";
                sb.AppendLine($"| {r.Severity.ToString().ToLowerInvariant()} | {r.SourcePrimitiveId} | {r.PixelCount} | {bounds} | {r.PlanningRole} |");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    // -----------------------------------------------------------------------
    // Private POCO types for JSON serialization
    // -----------------------------------------------------------------------

    private sealed class PlanDoc
    {
        public string  Schema              { get; init; } = "";
        public string  ClaimBoundary       { get; init; } = "";
        public string  GeneratedAtUtc      { get; init; } = "";
        public SourceEntry Source          { get; init; } = new();
        public int     Width               { get; init; }
        public int     Height              { get; init; }
        public int     PrimitiveCount      { get; init; }
        public int     RecommendationCount { get; init; }
        public int     WarningCount        { get; init; }
        public List<RecEntry> Recommendations { get; init; } = [];
        public SummaryEntry   Summary         { get; init; } = new();
    }

    private sealed class SourceEntry
    {
        public string ParsedCellPath { get; init; } = "";
        public string GeneratedBy    { get; init; } = "";
    }

    private sealed class RecEntry
    {
        public int     RecommendationId   { get; init; }
        public string  RecommendationType { get; init; } = "";
        public string  Severity           { get; init; } = "";
        public string  PlanningRole       { get; init; } = "";
        public int     SourcePrimitiveId  { get; init; }
        public string  PrimitiveType      { get; init; } = "";
        public int     PixelCount         { get; init; }
        public BoundsEntry? Bounds        { get; init; }
        public string  Message            { get; init; } = "";
    }

    private sealed class BoundsEntry
    {
        public int X { get; init; }
        public int Y { get; init; }
        public int Width  { get; init; }
        public int Height { get; init; }
    }

    private sealed class SummaryEntry
    {
        public int TotalPixels         { get; init; }
        public int PrimitiveCount      { get; init; }
        public int RecommendationCount { get; init; }
        public int WarningCount        { get; init; }
        public IReadOnlyDictionary<string, int> CountsByRecommendationType { get; init; }
            = new Dictionary<string, int>();
        public IReadOnlyDictionary<string, int> CountsBySeverity { get; init; }
            = new Dictionary<string, int>();
    }
}
