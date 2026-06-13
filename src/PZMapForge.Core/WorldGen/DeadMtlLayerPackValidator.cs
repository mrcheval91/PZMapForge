using System.Drawing;
using System.Runtime.Versioning;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

/// <summary>
/// Validates the structure, dimensions, palette consistency, and pixel contents
/// of a DeadMTL worldgen layer pack directory.
///
/// Claim boundary: validator only. No playable PZ export produced.
/// Windows-only: uses System.Drawing.Common for PNG inspection.
/// </summary>
[SupportedOSPlatform("windows")]
public static class DeadMtlLayerPackValidator
{
    private const string RequiredProjectFormat  = "pzmapforge.worldgen.project.v1";
    private const string RequiredPaletteFormat  = "pzmapforge.worldgen.png-palette.v1";
    private const int    RequiredOriginX        = 10580;
    private const int    RequiredOriginY        = 8200;

    private static readonly string[] RequiredTopFiles =
    [
        "deadmtl_worldgen_project.json",
        "README.md",
    ];

    private static readonly string[] RequiredPaletteFiles =
    [
        "palettes/worldgen-png-palette.json",
        "palettes/zoning-palette.json",
        "palettes/metadata-palette.json",
    ];

    private static readonly string[] RequiredScripts =
    [
        "scripts/generate-empty-layer-pack.ps1",
        "scripts/validate-layer-pack.ps1",
    ];

    private static readonly string[] System1LayerIds =
        ["water", "shore", "parks_forest", "roads_major"];

    private static readonly string[] PlaceholderLayerIds =
    [
        "roads_local", "zones_residential", "zones_commercial", "zones_industrial",
        "placed_buildings", "props", "npc_zones", "ownership",
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas         = true,
        ReadCommentHandling         = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = false,
    };

    public static DeadMtlLayerPackValidationResult Validate(string packRoot)
    {
        var result = new DeadMtlLayerPackValidationResult();

        Check(result, Directory.Exists(packRoot),
            $"Layer pack directory not found: {packRoot}");
        if (!result.IsValid) return result;

        var root = Path.GetFullPath(packRoot);

        // --- required files exist ---
        foreach (var rel in RequiredTopFiles)
            CheckFile(root, rel, result);
        foreach (var rel in RequiredPaletteFiles)
            CheckFile(root, rel, result);
        foreach (var rel in RequiredScripts)
            CheckFile(root, rel, result);

        var allLayerIds = System1LayerIds.Concat(PlaceholderLayerIds);
        foreach (var id in allLayerIds)
            CheckFile(root, $"layers/{id}.png", result);

        if (!result.IsValid) return result;

        // --- load and validate project manifest ---
        var projectPath = Path.Combine(root, "deadmtl_worldgen_project.json");
        var project = LoadProject(projectPath, result);
        if (project == null) return result;

        ValidateManifest(project, result);

        // --- future layers not in project ---
        var projectLayerIds = project.Layers
            .Select(l => l.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var placeholder in PlaceholderLayerIds)
        {
            Check(result, !projectLayerIds.Contains(placeholder),
                $"Future placeholder layer '{placeholder}' must not be included in the worldgen project manifest.");
        }

        // --- worldgen palette valid ---
        var palettePath = Path.Combine(root, "palettes", "worldgen-png-palette.json");
        var colorMap    = LoadAndValidatePalette(palettePath, result);

        // --- PNG dimensions ---
        foreach (var id in System1LayerIds)
            ValidatePngDimensions(root, id, project.Width, project.Height, result);
        foreach (var id in PlaceholderLayerIds)
            ValidatePngDimensions(root, id, project.Width, project.Height, result);

        // --- System 1 PNG colors ---
        if (colorMap != null)
        {
            foreach (var id in System1LayerIds)
                ValidateSystem1PngColors(root, id, colorMap, result);
        }

        // --- placeholder transparency ---
        foreach (var id in PlaceholderLayerIds)
            ValidatePlaceholderTransparency(root, id, result);

        return result;
    }

    // -----------------------------------------------------------------------
    // Manifest
    // -----------------------------------------------------------------------

    private static WorldGenProjectManifest? LoadProject(string path, DeadMtlLayerPackValidationResult result)
    {
        try
        {
            var json    = File.ReadAllText(path);
            var project = JsonSerializer.Deserialize<WorldGenProjectManifest>(json, JsonOptions);
            if (project == null)
            {
                result.Errors.Add("Project manifest deserialized to null.");
                return null;
            }
            return project;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Project manifest JSON parse error: {ex.Message}");
            return null;
        }
    }

    private static void ValidateManifest(WorldGenProjectManifest project, DeadMtlLayerPackValidationResult result)
    {
        Check(result, project.Format == RequiredProjectFormat,
            $"Project format must be '{RequiredProjectFormat}', got '{project.Format}'.");

        Check(result, !string.IsNullOrWhiteSpace(project.MapId),
            "Project map_id is missing or empty.");

        Check(result, project.OriginX == RequiredOriginX,
            $"Project origin_x must be {RequiredOriginX}, got {project.OriginX}.");

        Check(result, project.OriginY == RequiredOriginY,
            $"Project origin_y must be {RequiredOriginY}, got {project.OriginY}.");

        Check(result, project.Width > 0,
            $"Project width must be positive, got {project.Width}.");

        Check(result, project.Height > 0,
            $"Project height must be positive, got {project.Height}.");

        var supported   = new HashSet<string>(System1LayerIds, StringComparer.Ordinal);
        var unsupported = project.Layers
            .Select(l => l.Id)
            .Where(id => !supported.Contains(id))
            .ToList();

        Check(result, unsupported.Count == 0,
            $"Project manifest contains unsupported layer ids: {string.Join(", ", unsupported)}. " +
            "Only System 1 layers (water, shore, parks_forest, roads_major) are permitted.");
    }

    // -----------------------------------------------------------------------
    // Palette
    // -----------------------------------------------------------------------

    private static Dictionary<(byte R, byte G, byte B), WorldGenPngPaletteEntry>? LoadAndValidatePalette(
        string palettePath, DeadMtlLayerPackValidationResult result)
    {
        var loadResult = WorldGenPngPaletteLoader.Load(palettePath);
        result.ChecksRun++;

        Check(result, loadResult.IsValid,
            string.Join("; ", loadResult.Errors));

        if (!loadResult.IsValid || loadResult.Palette == null)
            return null;

        return WorldGenRectExtractor.BuildColorMap(loadResult.Palette);
    }

    // -----------------------------------------------------------------------
    // PNG dimensions
    // -----------------------------------------------------------------------

    private static void ValidatePngDimensions(
        string root, string layerId, int expectedW, int expectedH,
        DeadMtlLayerPackValidationResult result)
    {
        var path = Path.Combine(root, "layers", $"{layerId}.png");
        result.ChecksRun++;
        try
        {
            using var bmp = new Bitmap(path);
            if (bmp.Width != expectedW || bmp.Height != expectedH)
                result.Errors.Add(
                    $"PNG dimensions mismatch for layer '{layerId}': " +
                    $"is {bmp.Width}x{bmp.Height}, expected {expectedW}x{expectedH} (from project manifest).");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Could not load PNG for layer '{layerId}': {ex.Message}");
        }
    }

    // -----------------------------------------------------------------------
    // System 1 PNG color validation
    // -----------------------------------------------------------------------

    private static void ValidateSystem1PngColors(
        string root, string layerId,
        Dictionary<(byte R, byte G, byte B), WorldGenPngPaletteEntry> colorMap,
        DeadMtlLayerPackValidationResult result)
    {
        var path = Path.Combine(root, "layers", $"{layerId}.png");
        result.ChecksRun++;
        try
        {
            using var bmp    = new Bitmap(path);
            var unknownSeen  = new HashSet<(byte, byte, byte)>();

            for (var y = 0; y < bmp.Height; y++)
            {
                for (var x = 0; x < bmp.Width; x++)
                {
                    var px = bmp.GetPixel(x, y);
                    if (px.A < 128) continue;

                    var key = (px.R, px.G, px.B);
                    if (!colorMap.ContainsKey(key) && unknownSeen.Add(key))
                        result.Errors.Add(
                            $"Unknown color in System 1 layer '{layerId}': " +
                            $"RGB({px.R},{px.G},{px.B}) at pixel ({x},{y}). " +
                            "Add to worldgen palette or repaint.");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Could not inspect colors in layer '{layerId}': {ex.Message}");
        }
    }

    // -----------------------------------------------------------------------
    // Placeholder transparency
    // -----------------------------------------------------------------------

    private static void ValidatePlaceholderTransparency(
        string root, string layerId,
        DeadMtlLayerPackValidationResult result)
    {
        var path = Path.Combine(root, "layers", $"{layerId}.png");
        result.ChecksRun++;
        try
        {
            using var bmp = new Bitmap(path);
            for (var y = 0; y < bmp.Height; y++)
            {
                for (var x = 0; x < bmp.Width; x++)
                {
                    if (bmp.GetPixel(x, y).A >= 128)
                    {
                        result.Warnings.Add(
                            $"Placeholder layer '{layerId}' contains non-transparent pixels. " +
                            "This layer is reserved for a future system and should remain fully transparent.");
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Could not inspect transparency in layer '{layerId}': {ex.Message}");
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static void CheckFile(string root, string rel, DeadMtlLayerPackValidationResult result)
    {
        var path = Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));
        Check(result, File.Exists(path), $"Required file not found: {rel}");
    }

    private static void Check(DeadMtlLayerPackValidationResult result, bool condition, string errorMessage)
    {
        result.ChecksRun++;
        if (!condition) result.Errors.Add(errorMessage);
    }
}
