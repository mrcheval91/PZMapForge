using System.Text.Json;

namespace PZMapForge.Core.Palette;

public static class PaletteLoader
{
    private const string RequiredSchema = "pzmapforge.image-palette.v0.1";
    private const int RequiredCellWidth  = 300;
    private const int RequiredCellHeight = 300;
    private const int RequiredTileSize   = 32;

    private static readonly string[] RequiredKinds =
    [
        "grass", "road", "sidewalk", "row_house", "depanneur",
        "garage", "industrial_yard", "landmark", "spawn"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas      = true,
        ReadCommentHandling      = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = false,
    };

    public static PaletteValidationResult Load(string path)
    {
        var result = new PaletteValidationResult();

        if (!File.Exists(path))
        {
            result.Errors.Add($"Palette file not found: {path}");
            return result;
        }

        PaletteDocument doc;
        try
        {
            var json = File.ReadAllText(path);
            doc = JsonSerializer.Deserialize<PaletteDocument>(json, JsonOptions)
                  ?? throw new JsonException("Deserialized to null.");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"JSON parse error: {ex.Message}");
            return result;
        }

        result.Document = doc;
        Validate(doc, result.Errors);
        return result;
    }

    private static void Validate(PaletteDocument doc, List<string> errors)
    {
        if (doc.Schema != RequiredSchema)
            errors.Add($"schema must be '{RequiredSchema}', got '{doc.Schema}'.");

        if (doc.CellWidth != RequiredCellWidth)
            errors.Add($"cell_width must be {RequiredCellWidth}, got {doc.CellWidth}.");

        if (doc.CellHeight != RequiredCellHeight)
            errors.Add($"cell_height must be {RequiredCellHeight}, got {doc.CellHeight}.");

        if (doc.PreviewScale <= 0)
            errors.Add($"preview_scale must be > 0, got {doc.PreviewScale}.");

        if (doc.TileSize != RequiredTileSize)
            errors.Add($"tile_size must be {RequiredTileSize}, got {doc.TileSize}.");

        if (doc.Kinds == null || doc.Kinds.Count == 0)
        {
            errors.Add("kinds list is empty or missing.");
            return;
        }

        var seenKinds  = new HashSet<string>(StringComparer.Ordinal);
        var seenGids   = new HashSet<int>();
        var seenCodes  = new HashSet<string>(StringComparer.Ordinal);

        foreach (var k in doc.Kinds)
        {
            if (string.IsNullOrWhiteSpace(k.Kind))
            {
                errors.Add("A palette entry has a blank kind.");
                continue;
            }

            if (!seenKinds.Add(k.Kind))
                errors.Add($"Duplicate kind: '{k.Kind}'.");

            if (k.Code.Length != 1)
                errors.Add($"kind '{k.Kind}': code must be exactly one character, got '{k.Code}'.");
            else if (!seenCodes.Add(k.Code))
                errors.Add($"Duplicate code: '{k.Code}' (kind '{k.Kind}').");

            if (k.Gid <= 0)
                errors.Add($"kind '{k.Kind}': gid must be positive, got {k.Gid}.");
            else if (!seenGids.Add(k.Gid))
                errors.Add($"Duplicate gid: {k.Gid} (kind '{k.Kind}').");

            if (k.Rgb == null || k.Rgb.Length != 3)
            {
                errors.Add($"kind '{k.Kind}': rgb must be a 3-element array.");
            }
            else
            {
                foreach (var v in k.Rgb)
                    if (v < 0 || v > 255)
                        errors.Add($"kind '{k.Kind}': rgb value {v} is out of range 0..255.");
            }
        }

        foreach (var required in RequiredKinds)
            if (!seenKinds.Contains(required))
                errors.Add($"Missing required kind: '{required}'.");

        // GIDs must be contiguous from 1 to Count
        var sortedGids = seenGids.OrderBy(g => g).ToList();
        for (var i = 0; i < sortedGids.Count; i++)
        {
            var expected = i + 1;
            if (sortedGids[i] != expected)
                errors.Add($"GIDs must be contiguous from 1. Expected {expected}, found {sortedGids[i]}.");
        }
    }
}
