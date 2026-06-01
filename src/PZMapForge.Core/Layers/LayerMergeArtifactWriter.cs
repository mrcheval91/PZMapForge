using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PZMapForge.Core.Palette;
using PZMapForge.Core.ParsedCell;

namespace PZMapForge.Core.Layers;

/// <summary>
/// Writes layer merge artifacts: parsed-cell.json and layer-merge-report.md.
/// parsed-cell.json is compatible with ParsedCellLoader and downstream pipeline.
/// Does not touch media/maps. Does not claim playable PZ export.
/// Claim boundary: planning_artifact_only_not_pz_load_tested
/// </summary>
public static class LayerMergeArtifactWriter
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>
    /// Writes parsed-cell.json and layer-merge-report.md to <paramref name="outputDir"/>.
    /// Returns (parsedCellPath, mdPath).
    /// </summary>
    public static (string ParsedCellPath, string MdPath) Write(
        string            outputDir,
        string            manifestPath,
        string            palettePath,
        PaletteDocument   palette,
        LayerMergeResult  mergeResult,
        LayerMergeOptions? options = null)
    {
        options ??= LayerMergeOptions.Default;
        Directory.CreateDirectory(outputDir);

        var parsedCellPath = WriteParsedCell(outputDir, manifestPath, palettePath, palette, mergeResult, options);
        var mdPath         = WriteReport(outputDir, manifestPath, mergeResult, options);

        return (parsedCellPath, mdPath);
    }

    // -----------------------------------------------------------------------
    // parsed-cell.json
    // -----------------------------------------------------------------------

    private static string WriteParsedCell(
        string outputDir, string manifestPath, string palettePath,
        PaletteDocument palette, LayerMergeResult result, LayerMergeOptions options)
    {
        // Count pixels per kind from the merged rows
        var codeCount = new Dictionary<char, int>();
        foreach (var row in result.Rows)
            foreach (var ch in row)
                codeCount[ch] = codeCount.GetValueOrDefault(ch) + 1;

        var totalPixels = result.Width * result.Height;

        var counts = palette.Kinds
            .Select(k => new ParsedCellCount
            {
                Kind   = k.Kind,
                Code   = k.Code,
                Gid    = k.Gid,
                Pixels = codeCount.GetValueOrDefault(k.Code[0], 0),
            })
            .ToList();

        var legend = palette.Kinds
            .Select(k => new ParsedCellLegendEntry
            {
                Code        = k.Code,
                Kind        = k.Kind,
                Gid         = k.Gid,
                Rgb         = k.Rgb,
                Description = k.Description,
            })
            .ToList();

        var doc = new ParsedCellDocument
        {
            Schema            = "pzmapforge.parsed-cell.v0.1",
            Tool              = "pzmapforge.layer-merger",
            ClaimBoundary     = "planning_artifact_only_not_pz_load_tested",
            SourceImage       = Path.GetFullPath(manifestPath),
            SourceImageSha256 = ComputeFileSha256(manifestPath),
            Palette           = Path.GetFullPath(palettePath),
            PaletteSha256     = ComputeFileSha256(palettePath),
            Width             = result.Width,
            Height            = result.Height,
            Resized           = options.Resize,
            Matching          = new ParsedCellMatching
            {
                ExactPixels          = totalPixels,
                NearestPixels        = 0,
                UniqueSourceColours  = 0,
                UnmappedExactColours = 0,
            },
            Legend       = legend,
            Counts       = counts,
            NearestDrift = [],
            Rows         = result.Rows,
            Outputs      = new ParsedCellOutputs
            {
                Json             = "parsed-cell.json",
                Report           = string.Empty,
                Preview          = string.Empty,
                GeneratedTileset = string.Empty,
                Tmx              = string.Empty,
            },
        };

        var jsonPath = Path.Combine(outputDir, "parsed-cell.json");
        using var fs = File.Create(jsonPath);
        JsonSerializer.Serialize(fs, doc, JsonOpts);
        return jsonPath;
    }

    // -----------------------------------------------------------------------
    // layer-merge-report.md
    // -----------------------------------------------------------------------

    private static string WriteReport(
        string outputDir, string manifestPath,
        LayerMergeResult result, LayerMergeOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Layer Merge Report");
        sb.AppendLine();
        sb.AppendLine("## Claim boundary");
        sb.AppendLine();
        sb.AppendLine("planning_artifact_only_not_pz_load_tested");
        sb.AppendLine();
        sb.AppendLine("> This is a planning artifact only. It is not a Project Zomboid load-tested map export.");
        sb.AppendLine();
        sb.AppendLine("## Source");
        sb.AppendLine();
        sb.AppendLine($"Manifest: {Path.GetFullPath(manifestPath)}");
        sb.AppendLine($"Dimensions: {result.Width}x{result.Height}");
        sb.AppendLine($"Default kind: {options.DefaultKind}");
        sb.AppendLine();
        sb.AppendLine("## Conflict summary");
        sb.AppendLine();
        sb.AppendLine($"Total conflicts: {result.TotalConflictCount}");
        sb.AppendLine($"Conflict sample: {result.ConflictSample.Count} (max 100)");
        sb.AppendLine();
        sb.AppendLine("## Layer contributions");
        sb.AppendLine();
        sb.AppendLine("| Layer | Path | Contributed | Default | Chosen | Overridden |");
        sb.AppendLine("|---|---|---:|---:|---:|---:|");

        foreach (var c in result.Contributions)
        {
            sb.AppendLine(
                $"| {c.LayerName} | {c.FilePath} | {c.ContributedPixels} | " +
                $"{c.IgnoredDefaultPixels} | {c.ChosenPixels} | {c.OverriddenPixels} |");
        }

        if (result.ConflictSample.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Conflict sample (first 100)");
            sb.AppendLine();
            sb.AppendLine("| X | Y | Chosen layer | Chosen kind | Losing layers |");
            sb.AppendLine("|---:|---:|---|---|---|");
            foreach (var conflict in result.ConflictSample)
            {
                var losers = string.Join(", ", conflict.LosingLayers);
                sb.AppendLine(
                    $"| {conflict.X} | {conflict.Y} | {conflict.ChosenLayer} | " +
                    $"{conflict.ChosenKind} | {losers} |");
            }
        }

        var mdPath = Path.Combine(outputDir, "layer-merge-report.md");
        File.WriteAllText(mdPath, sb.ToString(), Encoding.UTF8);
        return mdPath;
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static string ComputeFileSha256(string path)
    {
        using var sha    = SHA256.Create();
        using var stream = File.OpenRead(path);
        return BitConverter.ToString(sha.ComputeHash(stream))
            .Replace("-", string.Empty).ToLowerInvariant();
    }
}
