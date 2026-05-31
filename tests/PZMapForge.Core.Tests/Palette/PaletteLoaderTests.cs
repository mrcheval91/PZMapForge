using System.Text.Json;
using PZMapForge.Core.Palette;
using Xunit;

namespace PZMapForge.Core.Tests.Palette;

public sealed class PaletteLoaderTests
{
    // Path to the actual canonical palette shipped with PZMapForge.
    // Resolved relative to the test assembly's output directory.
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", ".."));

    private static string CanonicalPalettePath =>
        Path.Combine(RepoRoot, "source", "image-palette.json");

    // -----------------------------------------------------------------------
    // Canonical palette
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_CanonicalPalette_IsValid()
    {
        var result = PaletteLoader.Load(CanonicalPalettePath);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.NotNull(result.Document);
        Assert.Equal("pzmapforge.image-palette.v0.1", result.Document.Schema);
        Assert.Equal(300, result.Document.CellWidth);
        Assert.Equal(300, result.Document.CellHeight);
        Assert.Equal(9, result.Document.Kinds.Count);
    }

    // -----------------------------------------------------------------------
    // File not found
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_MissingFile_Fails()
    {
        var result = PaletteLoader.Load("/nonexistent/palette.json");

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("not found", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Helpers — build minimal valid palette JSON, then mutate
    // -----------------------------------------------------------------------

    private static string BuildValidPaletteJson(
        string? schema          = "pzmapforge.image-palette.v0.1",
        int cellWidth           = 300,
        int cellHeight          = 300,
        int previewScale        = 3,
        int tileSize            = 32,
        List<object>? kinds     = null)
    {
        kinds ??= DefaultKinds();
        var doc = new
        {
            schema,
            cell_width    = cellWidth,
            cell_height   = cellHeight,
            preview_scale = previewScale,
            tile_size     = tileSize,
            kinds
        };
        return JsonSerializer.Serialize(doc);
    }

    private static List<object> DefaultKinds() =>
    [
        new { kind = "grass",           code = "g", gid = 1, rgb = new[]{100,140, 70}, description = "" },
        new { kind = "road",            code = "r", gid = 2, rgb = new[]{ 70, 70, 70}, description = "" },
        new { kind = "sidewalk",        code = "s", gid = 3, rgb = new[]{190,180,160}, description = "" },
        new { kind = "row_house",       code = "h", gid = 4, rgb = new[]{160,110, 80}, description = "" },
        new { kind = "depanneur",       code = "d", gid = 5, rgb = new[]{200,130, 60}, description = "" },
        new { kind = "garage",          code = "a", gid = 6, rgb = new[]{ 80, 80,100}, description = "" },
        new { kind = "industrial_yard", code = "i", gid = 7, rgb = new[]{160,130, 90}, description = "" },
        new { kind = "landmark",        code = "l", gid = 8, rgb = new[]{255,220,  0}, description = "" },
        new { kind = "spawn",           code = "p", gid = 9, rgb = new[]{  0,220, 80}, description = "" },
    ];

    private static PaletteValidationResult LoadFromJson(string json)
    {
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, json);
        try { return PaletteLoader.Load(tmp); }
        finally { File.Delete(tmp); }
    }

    // -----------------------------------------------------------------------
    // Duplicate GID
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_DuplicateGid_Fails()
    {
        var kinds = DefaultKinds();
        // Replace gid 9 with 1 (duplicate)
        kinds[8] = new { kind = "spawn", code = "p", gid = 1, rgb = new[]{0,220,80}, description = "" };

        var result = LoadFromJson(BuildValidPaletteJson(kinds: kinds));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate gid", StringComparison.OrdinalIgnoreCase)
                                          || e.Contains("Duplicate GID", StringComparison.OrdinalIgnoreCase)
                                          || e.Contains("contiguous",    StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Missing required kind
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_MissingRequiredKind_Fails()
    {
        var kinds = DefaultKinds();
        kinds.RemoveAt(8); // remove spawn

        var result = LoadFromJson(BuildValidPaletteJson(kinds: kinds));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("spawn", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Invalid RGB (out of 0..255)
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_InvalidRgb_Fails()
    {
        var kinds = DefaultKinds();
        kinds[0] = new { kind = "grass", code = "g", gid = 1, rgb = new[]{300, 140, 70}, description = "" };

        var result = LoadFromJson(BuildValidPaletteJson(kinds: kinds));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("300", StringComparison.Ordinal)
                                          && e.Contains("0..255", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Duplicate code
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_DuplicateCode_Fails()
    {
        var kinds = DefaultKinds();
        kinds[1] = new { kind = "road", code = "g", gid = 2, rgb = new[]{70,70,70}, description = "" };

        var result = LoadFromJson(BuildValidPaletteJson(kinds: kinds));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate code", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Wrong schema
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_WrongSchema_Fails()
    {
        var result = LoadFromJson(BuildValidPaletteJson(schema: "wrong.schema"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("schema", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Wrong dimensions
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_WrongCellWidth_Fails()
    {
        var result = LoadFromJson(BuildValidPaletteJson(cellWidth: 512));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("cell_width", StringComparison.OrdinalIgnoreCase));
    }
}
