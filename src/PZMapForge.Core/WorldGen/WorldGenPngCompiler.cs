using System.Drawing;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

/// <summary>
/// Converts a single PNG color layer into a WorldGenManifest.
/// Rectangle extraction is delegated to WorldGenRectExtractor.
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

        var colorMap = WorldGenRectExtractor.BuildColorMap(paletteResult.Palette!);

        Bitmap bmp;
        try { bmp = new Bitmap(pngPath); }
        catch (Exception ex)
        {
            result.Errors.Add($"PNG load error: {ex.Message}");
            return result;
        }

        List<WorldGenModule> modules;
        using (bmp)
        {
            var grid = new (string Type, string Key)?[bmp.Width, bmp.Height];
            WorldGenRectExtractor.ApplyBitmapToGrid(bmp, colorMap, grid, opts, result.Errors);
            if (!result.IsValid) return result;
            modules = WorldGenRectExtractor.ExtractModules(grid, bmp.Width, bmp.Height, originX, originY);
        }

        result.Manifest = new WorldGenManifest
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
}
