using System.Drawing;
using System.Runtime.Versioning;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

/// <summary>
/// Compiles a multi-layer worldgen project into a single WorldGenManifest.
///
/// Pipeline:
///   project.json
///     → sorted layers (priority ascending, then id for ties)
///     → each layer's PNG painted onto a shared semantic grid
///     → WorldGenRectExtractor rectangle extraction
///     → WorldGenManifest (pzmapforge.worldgen.layers.v1)
///
/// Priority semantics: lower priority value = painted first; higher value overrides.
/// Transparent pixels (alpha < 128) leave the existing grid cell unchanged.
///
/// Paths in the project JSON are resolved relative to the project file directory.
/// Windows-only: uses System.Drawing.Common for PNG access.
/// Claim boundary: planning_artifact_only_not_pz_load_tested
/// </summary>
[SupportedOSPlatform("windows")]
public static class WorldGenProjectCompiler
{
    private const string RequiredFormat = "pzmapforge.worldgen.project.v1";
    private const string ManifestFormat = "pzmapforge.worldgen.layers.v1";
    private const string OriginNote     = "Generated from worldgen project. Coordinates are world tile coordinates.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas     = true,
        ReadCommentHandling     = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = false,
    };

    public static WorldGenProjectCompileResult Compile(
        string projectPath,
        WorldGenPngCompileOptions? options = null)
    {
        var result = new WorldGenProjectCompileResult();
        var opts   = options ?? WorldGenPngCompileOptions.Default;

        if (!File.Exists(projectPath))
        {
            result.Errors.Add($"Project file not found: {projectPath}");
            return result;
        }

        WorldGenProjectManifest project;
        try
        {
            var json = File.ReadAllText(projectPath);
            project = JsonSerializer.Deserialize<WorldGenProjectManifest>(json, JsonOptions)
                      ?? throw new JsonException("Deserialized to null.");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"JSON parse error: {ex.Message}");
            return result;
        }

        var projectDir = Path.GetDirectoryName(Path.GetFullPath(projectPath)) ?? ".";

        Validate(project, projectDir, result.Errors);
        if (!result.IsValid) return result;

        // Shared semantic grid: null = no content
        var grid = new (string Type, string Key)?[project.Width, project.Height];

        // Lower priority painted first; higher priority overwrites per pixel
        var orderedLayers = project.Layers
            .OrderBy(l => l.Priority)
            .ThenBy(l => l.Id, StringComparer.Ordinal)
            .ToList();

        foreach (var layer in orderedLayers)
        {
            var pngPath     = Path.GetFullPath(Path.Combine(projectDir, layer.Path));
            var palettePath = Path.GetFullPath(Path.Combine(projectDir, layer.Palette));

            var paletteResult = WorldGenPngPaletteLoader.Load(palettePath);
            if (!paletteResult.IsValid)
            {
                foreach (var e in paletteResult.Errors)
                    result.Errors.Add($"layer '{layer.Id}' palette: {e}");
                return result;
            }

            var colorMap = WorldGenRectExtractor.BuildColorMap(paletteResult.Palette!);

            Bitmap bmp;
            try { bmp = new Bitmap(pngPath); }
            catch (Exception ex)
            {
                result.Errors.Add($"layer '{layer.Id}': PNG load error: {ex.Message}");
                return result;
            }

            using (bmp)
                WorldGenRectExtractor.ApplyBitmapToGrid(bmp, colorMap, grid, opts, result.Errors);

            if (!result.IsValid) return result;
        }

        var modules = WorldGenRectExtractor.ExtractModules(
            grid, project.Width, project.Height, project.OriginX, project.OriginY);

        result.Manifest = new WorldGenManifest
        {
            MapId         = project.MapId,
            Format        = ManifestFormat,
            OriginNote    = OriginNote,
            StaticModules = modules,
        };
        result.ModuleCount = modules.Count;
        return result;
    }

    private static void Validate(
        WorldGenProjectManifest project, string projectDir, List<string> errors)
    {
        if (project.Format != RequiredFormat)
            errors.Add($"format must be '{RequiredFormat}', got '{project.Format}'.");

        if (string.IsNullOrWhiteSpace(project.MapId))
            errors.Add("map_id is missing or empty.");

        if (project.Width <= 0)
            errors.Add($"width must be positive, got {project.Width}.");

        if (project.Height <= 0)
            errors.Add($"height must be positive, got {project.Height}.");

        if (project.Layers == null || project.Layers.Count == 0)
        {
            errors.Add("layers list is missing or empty.");
            return;
        }

        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var layer in project.Layers)
        {
            if (string.IsNullOrWhiteSpace(layer.Id))
            {
                errors.Add("A layer has a missing or empty id.");
                continue;
            }

            if (!seenIds.Add(layer.Id))
                errors.Add($"Duplicate layer id '{layer.Id}'.");

            if (string.IsNullOrWhiteSpace(layer.Path))
            {
                errors.Add($"layer '{layer.Id}': path is missing or empty.");
            }
            else
            {
                var pngPath = Path.GetFullPath(Path.Combine(projectDir, layer.Path));
                if (!File.Exists(pngPath))
                    errors.Add($"layer '{layer.Id}': PNG not found at '{pngPath}'.");
            }

            if (string.IsNullOrWhiteSpace(layer.Palette))
            {
                errors.Add($"layer '{layer.Id}': palette is missing or empty.");
            }
            else
            {
                var palettePath = Path.GetFullPath(Path.Combine(projectDir, layer.Palette));
                if (!File.Exists(palettePath))
                    errors.Add($"layer '{layer.Id}': palette not found at '{palettePath}'.");
            }
        }
    }
}
