using System.Security.Cryptography;
using System.Text.Json;
using PZMapForge.Core.Palette;
using PZMapForge.Core.ParsedCell;

namespace PZMapForge.Core.ImageParsing;

/// <summary>
/// Serializes an ImageMapForgeResult to a parsed-cell.json artifact.
/// The caller is responsible for choosing a safe output directory.
/// Does not touch media/maps. Does not claim playable PZ export.
/// Claim boundary: planning_artifact_only_not_pz_load_tested
/// </summary>
public static class ImageMapForgeArtifactWriter
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>
    /// Writes parsed-cell.json to <paramref name="outputDir"/> and returns the full path.
    /// </summary>
    public static string Write(
        string          outputDir,
        string          imagePath,
        string          palettePath,
        PaletteDocument palette,
        ImageMapForgeResult result)
    {
        Directory.CreateDirectory(outputDir);

        var doc      = BuildDocument(imagePath, palettePath, palette, result);
        var jsonPath = Path.Combine(outputDir, "parsed-cell.json");

        using var fs = File.Create(jsonPath);
        JsonSerializer.Serialize(fs, doc, JsonOpts);

        return jsonPath;
    }

    // -----------------------------------------------------------------------
    // Document builder
    // -----------------------------------------------------------------------

    private static ParsedCellDocument BuildDocument(
        string imagePath, string palettePath,
        PaletteDocument palette, ImageMapForgeResult result)
    {
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

        return new ParsedCellDocument
        {
            Schema             = "pzmapforge.parsed-cell.v0.1",
            Tool               = "ImageMapForge (.NET)",
            ClaimBoundary      = "planning_artifact_only_not_pz_load_tested",
            SourceImage        = Path.GetFullPath(imagePath),
            SourceImageSha256  = ComputeFileSha256(imagePath),
            Palette            = Path.GetFullPath(palettePath),
            PaletteSha256      = result.PaletteSha256,
            Width              = result.Width,
            Height             = result.Height,
            Resized            = result.Resized,
            Matching           = result.Matching,
            Legend             = legend,
            Counts             = result.Counts.ToList(),
            NearestDrift       = result.NearestDrift.ToList(),
            Rows               = result.Rows.ToList(),
            Outputs            = new ParsedCellOutputs
            {
                Json             = "parsed-cell.json",
                Report           = string.Empty,
                Preview          = string.Empty,
                GeneratedTileset = string.Empty,
                Tmx              = string.Empty,
            },
        };
    }

    private static string ComputeFileSha256(string path)
    {
        using var sha    = SHA256.Create();
        using var stream = File.OpenRead(path);
        return BitConverter.ToString(sha.ComputeHash(stream))
            .Replace("-", string.Empty).ToLowerInvariant();
    }
}
