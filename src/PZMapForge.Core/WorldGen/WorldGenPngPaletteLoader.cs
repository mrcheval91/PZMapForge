using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class WorldGenPngPaletteLoader
{
    private const string RequiredFormat = "pzmapforge.worldgen.png-palette.v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas     = true,
        ReadCommentHandling     = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = false,
    };

    public static WorldGenPngPaletteLoadResult Load(string path)
    {
        var result = new WorldGenPngPaletteLoadResult();

        if (!File.Exists(path))
        {
            result.Errors.Add($"Palette file not found: {path}");
            return result;
        }

        WorldGenPngPalette palette;
        try
        {
            var json = File.ReadAllText(path);
            palette = JsonSerializer.Deserialize<WorldGenPngPalette>(json, JsonOptions)
                      ?? throw new JsonException("Deserialized to null.");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"JSON parse error: {ex.Message}");
            return result;
        }

        result.Palette = palette;
        Validate(palette, result.Errors);
        return result;
    }

    private static void Validate(WorldGenPngPalette palette, List<string> errors)
    {
        if (palette.Format != RequiredFormat)
            errors.Add($"format must be '{RequiredFormat}', got '{palette.Format}'.");

        if (palette.Entries == null || palette.Entries.Count == 0)
        {
            errors.Add("entries list is missing or empty.");
            return;
        }

        var seenColors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenKeys   = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < palette.Entries.Count; i++)
        {
            var e      = palette.Entries[i];
            var prefix = $"entries[{i}]";

            if (string.IsNullOrWhiteSpace(e.Color))
                errors.Add($"{prefix}: color is missing or empty.");
            else if (!TryParseHexColor(e.Color, out _, out _, out _))
                errors.Add($"{prefix}: color '{e.Color}' is not a valid #RRGGBB hex color.");
            else if (!seenColors.Add(e.Color))
                errors.Add($"{prefix}: duplicate color '{e.Color}'.");

            if (e.Type is not ("biome" or "prefab"))
                errors.Add($"{prefix}: type must be 'biome' or 'prefab', got '{e.Type}'.");

            if (string.IsNullOrWhiteSpace(e.Key))
            {
                errors.Add($"{prefix}: key is missing or empty.");
            }
            else
            {
                if (e.Type == "biome" && !WorldGenRegistry.Biomes.Contains(e.Key))
                    errors.Add($"{prefix}: unknown biome key '{e.Key}'.");
                else if (e.Type == "prefab" && !WorldGenRegistry.Prefabs.Contains(e.Key))
                    errors.Add($"{prefix}: unknown prefab key '{e.Key}'.");

                if (!seenKeys.Add(e.Key))
                    errors.Add($"{prefix}: duplicate key '{e.Key}' (each worldgen key may appear at most once per palette).");
            }
        }
    }

    internal static bool TryParseHexColor(string hex, out byte r, out byte g, out byte b)
    {
        r = 0; g = 0; b = 0;
        if (hex == null || hex.Length != 7 || hex[0] != '#') return false;
        try
        {
            r = Convert.ToByte(hex[1..3], 16);
            g = Convert.ToByte(hex[3..5], 16);
            b = Convert.ToByte(hex[5..7], 16);
            return true;
        }
        catch { return false; }
    }
}
