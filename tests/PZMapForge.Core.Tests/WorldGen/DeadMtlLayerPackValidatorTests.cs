using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlLayerPackValidatorTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-deadmtl-validator-tests", Path.GetRandomFileName());

    public DeadMtlLayerPackValidatorTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string RealPackRoot =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack");

    // -----------------------------------------------------------------------
    // Minimal pack builder
    // -----------------------------------------------------------------------

    // Colors matching palettes/worldgen-png-palette.json
    private static readonly Color KnownWater   = Color.FromArgb(255,   0,   0, 255);
    private static readonly Color KnownShore   = Color.FromArgb(255, 216, 192, 128);
    private static readonly Color KnownForest  = Color.FromArgb(255,  32, 112,  32);
    private static readonly Color KnownRoad    = Color.FromArgb(255, 255, 102,   0);
    private static readonly Color Transparent  = Color.FromArgb(0,    0,   0,   0);

    private static readonly string[] System1Ids     = ["water", "shore", "parks_forest", "roads_major"];
    private static readonly string[] PlaceholderIds =
    [
        "roads_local", "zones_residential", "zones_commercial", "zones_industrial",
        "placed_buildings", "props", "npc_zones", "ownership",
    ];

    private string CreateMinimalValidPack(int width = 4, int height = 3, int originX = 10580, int originY = 8200)
    {
        var pack = Path.Combine(_tempDir, Path.GetRandomFileName());
        Directory.CreateDirectory(pack);
        Directory.CreateDirectory(Path.Combine(pack, "palettes"));
        Directory.CreateDirectory(Path.Combine(pack, "layers"));
        Directory.CreateDirectory(Path.Combine(pack, "scripts"));

        // Project manifest
        File.WriteAllText(Path.Combine(pack, "deadmtl_worldgen_project.json"), $$"""
            {
              "format": "pzmapforge.worldgen.project.v1",
              "map_id": "test_pack",
              "origin_x": {{originX}},
              "origin_y": {{originY}},
              "width": {{width}},
              "height": {{height}},
              "layers": [
                {"id":"water",       "path":"layers/water.png",       "palette":"palettes/worldgen-png-palette.json","priority":10},
                {"id":"shore",       "path":"layers/shore.png",       "palette":"palettes/worldgen-png-palette.json","priority":20},
                {"id":"parks_forest","path":"layers/parks_forest.png","palette":"palettes/worldgen-png-palette.json","priority":30},
                {"id":"roads_major", "path":"layers/roads_major.png", "palette":"palettes/worldgen-png-palette.json","priority":50}
              ]
            }
            """, Encoding.UTF8);

        // README
        File.WriteAllText(Path.Combine(pack, "README.md"), "# Test Pack\n");

        // Palettes
        File.WriteAllText(Path.Combine(pack, "palettes", "worldgen-png-palette.json"), """
            {
              "format": "pzmapforge.worldgen.png-palette.v1",
              "entries": [
                {"color":"#0000FF","type":"biome", "key":"water"},
                {"color":"#D8C080","type":"biome", "key":"sand_bank"},
                {"color":"#207020","type":"biome", "key":"birch_forest"},
                {"color":"#FF6600","type":"prefab","key":"normal_road_WE_00"},
                {"color":"#CC3300","type":"prefab","key":"highway_NS_00"}
              ]
            }
            """, Encoding.UTF8);

        File.WriteAllText(Path.Combine(pack, "palettes", "zoning-palette.json"),
            "{\"_note\":\"FUTURE\"}\n");
        File.WriteAllText(Path.Combine(pack, "palettes", "metadata-palette.json"),
            "{\"_note\":\"FUTURE\"}\n");

        // Scripts
        File.WriteAllText(Path.Combine(pack, "scripts", "generate-empty-layer-pack.ps1"), "# stub\n");
        File.WriteAllText(Path.Combine(pack, "scripts", "validate-layer-pack.ps1"), "# stub\n");

        // System 1 PNGs — each painted with its canonical color, first pixel only
        var system1Colors = new[] { KnownWater, KnownShore, KnownForest, KnownRoad };
        for (var i = 0; i < System1Ids.Length; i++)
        {
            using var bmp = new Bitmap(width, height);
            using var g   = Graphics.FromImage(bmp);
            g.Clear(Transparent);
            bmp.SetPixel(0, 0, system1Colors[i]);
            bmp.Save(Path.Combine(pack, "layers", $"{System1Ids[i]}.png"), ImageFormat.Png);
        }

        // Placeholder PNGs — fully transparent
        foreach (var id in PlaceholderIds)
        {
            using var bmp = new Bitmap(width, height);
            using var g   = Graphics.FromImage(bmp);
            g.Clear(Transparent);
            bmp.Save(Path.Combine(pack, "layers", $"{id}.png"), ImageFormat.Png);
        }

        return pack;
    }

    // -----------------------------------------------------------------------
    // Valid real pack tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_RealPack_IsValid()
    {
        var result = PZMapForge.Core.WorldGen.DeadMtlLayerPackValidator.Validate(RealPackRoot);
        Assert.True(result.IsValid,
            "Real pack validation failed:\n" + string.Join("\n", result.Errors));
    }

    [Fact]
    public void Validate_RealPack_HasZeroErrors()
    {
        var result = PZMapForge.Core.WorldGen.DeadMtlLayerPackValidator.Validate(RealPackRoot);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_RealPack_HasZeroWarnings()
    {
        var result = PZMapForge.Core.WorldGen.DeadMtlLayerPackValidator.Validate(RealPackRoot);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Validate_RealPack_ChecksRunIsPositive()
    {
        var result = PZMapForge.Core.WorldGen.DeadMtlLayerPackValidator.Validate(RealPackRoot);
        Assert.True(result.ChecksRun > 0, "ChecksRun should be positive for a full validation pass");
    }

    // -----------------------------------------------------------------------
    // Directory not found
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_DirectoryNotFound_ReturnsError()
    {
        var result = PZMapForge.Core.WorldGen.DeadMtlLayerPackValidator.Validate(
            Path.Combine(_tempDir, "nonexistent"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Missing required file
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_MissingProjectJson_ReturnsError()
    {
        var pack = CreateMinimalValidPack();
        File.Delete(Path.Combine(pack, "deadmtl_worldgen_project.json"));

        var result = PZMapForge.Core.WorldGen.DeadMtlLayerPackValidator.Validate(pack);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.Contains("deadmtl_worldgen_project.json", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_MissingLayerPng_ReturnsError()
    {
        var pack = CreateMinimalValidPack();
        File.Delete(Path.Combine(pack, "layers", "water.png"));

        var result = PZMapForge.Core.WorldGen.DeadMtlLayerPackValidator.Validate(pack);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.Contains("water.png", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_MissingScript_ReturnsError()
    {
        var pack = CreateMinimalValidPack();
        File.Delete(Path.Combine(pack, "scripts", "generate-empty-layer-pack.ps1"));

        var result = PZMapForge.Core.WorldGen.DeadMtlLayerPackValidator.Validate(pack);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.Contains("generate-empty-layer-pack.ps1", StringComparison.Ordinal));
    }

    // -----------------------------------------------------------------------
    // PNG dimensions
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_WrongPngDimensions_ReturnsError()
    {
        var pack = CreateMinimalValidPack(width: 4, height: 3);

        // Replace water.png with wrong-size PNG (2x2 instead of 4x3)
        using (var bmp = new Bitmap(2, 2))
        using (var g   = Graphics.FromImage(bmp))
        {
            g.Clear(KnownWater);
            bmp.Save(Path.Combine(pack, "layers", "water.png"), ImageFormat.Png);
        }

        var result = PZMapForge.Core.WorldGen.DeadMtlLayerPackValidator.Validate(pack);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.Contains("dimensions mismatch", StringComparison.OrdinalIgnoreCase) &&
                 e.Contains("water", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_PlaceholderWrongDimensions_ReturnsError()
    {
        var pack = CreateMinimalValidPack(width: 4, height: 3);

        // Replace roads_local.png with wrong-size transparent PNG
        using (var bmp = new Bitmap(1, 1))
        using (var g   = Graphics.FromImage(bmp))
        {
            g.Clear(Transparent);
            bmp.Save(Path.Combine(pack, "layers", "roads_local.png"), ImageFormat.Png);
        }

        var result = PZMapForge.Core.WorldGen.DeadMtlLayerPackValidator.Validate(pack);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.Contains("roads_local", StringComparison.Ordinal));
    }

    // -----------------------------------------------------------------------
    // Unknown color in System 1 layer
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_UnknownColorInSystem1Layer_ReturnsError()
    {
        var pack = CreateMinimalValidPack(width: 4, height: 3);

        // Repaint water.png with a color not in the palette
        using (var bmp = new Bitmap(4, 3))
        using (var g   = Graphics.FromImage(bmp))
        {
            g.Clear(Transparent);
            bmp.SetPixel(0, 0, Color.FromArgb(255, 255, 0, 255)); // magenta, not in palette
            bmp.Save(Path.Combine(pack, "layers", "water.png"), ImageFormat.Png);
        }

        var result = PZMapForge.Core.WorldGen.DeadMtlLayerPackValidator.Validate(pack);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.Contains("Unknown color", StringComparison.OrdinalIgnoreCase) &&
                 e.Contains("water", StringComparison.Ordinal));
    }

    // -----------------------------------------------------------------------
    // Unsupported layer in project
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_UnsupportedLayerInProject_ReturnsError()
    {
        var pack = CreateMinimalValidPack();

        // Rewrite manifest to include a placeholder layer id
        File.WriteAllText(Path.Combine(pack, "deadmtl_worldgen_project.json"), """
            {
              "format": "pzmapforge.worldgen.project.v1",
              "map_id": "test_pack",
              "origin_x": 10580,
              "origin_y": 8200,
              "width": 4,
              "height": 3,
              "layers": [
                {"id":"water",       "path":"layers/water.png",       "palette":"palettes/worldgen-png-palette.json","priority":10},
                {"id":"roads_local", "path":"layers/roads_local.png", "palette":"palettes/worldgen-png-palette.json","priority":99}
              ]
            }
            """, Encoding.UTF8);

        var result = PZMapForge.Core.WorldGen.DeadMtlLayerPackValidator.Validate(pack);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.Contains("roads_local", StringComparison.Ordinal) &&
                 e.Contains("unsupported", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Future placeholder painted → warning (not error)
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_PlaceholderLayerPainted_ReturnsWarning()
    {
        var pack = CreateMinimalValidPack(width: 4, height: 3);

        // Paint a pixel in roads_local.png (placeholder)
        using (var bmp = new Bitmap(4, 3))
        using (var g   = Graphics.FromImage(bmp))
        {
            g.Clear(Transparent);
            bmp.SetPixel(1, 1, Color.FromArgb(255, 100, 200, 50));
            bmp.Save(Path.Combine(pack, "layers", "roads_local.png"), ImageFormat.Png);
        }

        var result = PZMapForge.Core.WorldGen.DeadMtlLayerPackValidator.Validate(pack);

        Assert.True(result.IsValid, "A painted placeholder should be a warning, not an error");
        Assert.Contains(result.Warnings,
            w => w.Contains("roads_local", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_PlaceholderLayerPainted_DoesNotAddError()
    {
        var pack = CreateMinimalValidPack(width: 4, height: 3);

        using (var bmp = new Bitmap(4, 3))
        using (var g   = Graphics.FromImage(bmp))
        {
            g.Clear(Transparent);
            bmp.SetPixel(0, 0, Color.FromArgb(255, 10, 20, 30));
            bmp.Save(Path.Combine(pack, "layers", "ownership.png"), ImageFormat.Png);
        }

        var result = PZMapForge.Core.WorldGen.DeadMtlLayerPackValidator.Validate(pack);

        Assert.DoesNotContain(result.Errors,
            e => e.Contains("ownership", StringComparison.Ordinal));
    }

    // -----------------------------------------------------------------------
    // Manifest field validation
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_WrongOrigin_ReturnsError()
    {
        var pack = CreateMinimalValidPack(originX: 9999, originY: 9999);

        var result = PZMapForge.Core.WorldGen.DeadMtlLayerPackValidator.Validate(pack);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            e => e.Contains("origin_x", StringComparison.Ordinal));
    }
}
