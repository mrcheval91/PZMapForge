using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

/// <summary>
/// Tests for WorldGenPngCompiler. Creates PNGs programmatically in a temp directory.
/// No .local/ state dependency. No committed image fixtures.
/// Windows-only: uses System.Drawing.Common.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WorldGenPngCompilerTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-png-compiler-tests", Path.GetRandomFileName());

    public WorldGenPngCompilerTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // -----------------------------------------------------------------------
    // Helpers: palette colors matching worldgen-png-palette.json
    // -----------------------------------------------------------------------

    private static readonly Color Water      = Color.FromArgb(255,   0,   0, 255);  // #0000FF
    private static readonly Color SandBank   = Color.FromArgb(255, 216, 192, 128);  // #D8C080
    private static readonly Color GrassPlain = Color.FromArgb(255,   0, 170,   0);  // #00AA00
    private static readonly Color RoadWE     = Color.FromArgb(255, 255,   0,   0);  // #FF0000
    private static readonly Color HighwayNS  = Color.FromArgb(255, 170,   0,   0);  // #AA0000
    private static readonly Color Unknown    = Color.FromArgb(255, 123,  45,  67);
    private static readonly Color Transparent = Color.FromArgb(0, 0, 0, 0);

    private static string SamplePalettePath =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "examples", "worldgen", "worldgen-png-palette.json"));

    private string MakePng(int width, int height, Color fill)
    {
        var path = Path.Combine(_tempDir, $"img-{Guid.NewGuid():N}.png");
        using var bmp = new Bitmap(width, height);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(fill);
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    private string MakePng(int width, int height, Func<int, int, Color> pixelFn)
    {
        var path = Path.Combine(_tempDir, $"img-{Guid.NewGuid():N}.png");
        using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                bmp.SetPixel(x, y, pixelFn(x, y));
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    private string WritePalette(string content)
    {
        var path = Path.Combine(_tempDir, $"palette-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content);
        return path;
    }

    // -----------------------------------------------------------------------
    // Test 1: solid water rectangle → 1 module with correct world coords
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_SolidWaterRect_ProducesOneModule()
    {
        var png    = MakePng(4, 3, Water);
        var result = WorldGenPngCompiler.Compile(png, SamplePalettePath, "test_map", 10000, 8000);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(1, result.ModuleCount);

        var mod = result.Manifest!.StaticModules[0];
        Assert.Equal("biome",  mod.Type);
        Assert.Equal("water",  mod.Key);
        Assert.Equal(10000,    mod.X1);
        Assert.Equal(8000,     mod.Y1);
        Assert.Equal(10003,    mod.X2);  // origin_x + (width-1)
        Assert.Equal(8002,     mod.Y2);  // origin_y + (height-1)
    }

    // -----------------------------------------------------------------------
    // Test 2: module id uses key + zero-padded counter
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_SolidWaterRect_ModuleIdIsFormatted()
    {
        var png    = MakePng(2, 2, Water);
        var result = WorldGenPngCompiler.Compile(png, SamplePalettePath, "test_map", 0, 0);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal("water_000001", result.Manifest!.StaticModules[0].Id);
    }

    // -----------------------------------------------------------------------
    // Test 3: two colors → two modules
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_TwoColorsInRows_ProducesTwoModules()
    {
        // 4x2: top row = water, bottom row = sand_bank
        var png = MakePng(4, 2, (x, y) => y == 0 ? Water : SandBank);
        var result = WorldGenPngCompiler.Compile(png, SamplePalettePath, "test_map", 0, 0);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(2, result.ModuleCount);

        var keys = result.Manifest!.StaticModules.Select(m => m.Key).OrderBy(k => k).ToList();
        Assert.Contains("water",    keys);
        Assert.Contains("sand_bank", keys);
    }

    // -----------------------------------------------------------------------
    // Test 4: L-shape becomes two rectangles
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_LShape_ProducesTwoRectangles()
    {
        // 3x3 L-shape (W=water, .=transparent):
        // WWW
        // W..
        // W..
        var png = MakePng(3, 3, (x, y) =>
        {
            if (y == 0) return Water;           // top row: all water
            if (x == 0) return Water;           // left column
            return Transparent;
        });

        var result = WorldGenPngCompiler.Compile(png, SamplePalettePath, "test_map", 0, 0);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(2, result.ModuleCount);
        Assert.All(result.Manifest!.StaticModules, m => Assert.Equal("water", m.Key));
    }

    // -----------------------------------------------------------------------
    // Test 5: transparent pixels are ignored
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_TransparentPixelsIgnored_ProducesNoModulesForTransparent()
    {
        // 3x3: center pixel water, all others transparent
        var png = MakePng(3, 3, (x, y) => x == 1 && y == 1 ? Water : Transparent);
        var result = WorldGenPngCompiler.Compile(png, SamplePalettePath, "test_map", 0, 0);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(1, result.ModuleCount);
        var mod = result.Manifest!.StaticModules[0];
        Assert.Equal(1, mod.X1);
        Assert.Equal(1, mod.Y1);
        Assert.Equal(1, mod.X2);
        Assert.Equal(1, mod.Y2);
    }

    // -----------------------------------------------------------------------
    // Test 6: unknown color fails by default
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_UnknownColor_FailsByDefault()
    {
        var png    = MakePng(2, 2, Unknown);
        var result = WorldGenPngCompiler.Compile(png, SamplePalettePath, "test_map", 0, 0);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Unknown color", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Test 7: unknown color ignored when --ignore-unknown
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_UnknownColor_IgnoredWithOption()
    {
        var png    = MakePng(2, 2, Unknown);
        var opts   = new WorldGenPngCompileOptions { IgnoreUnknown = true };
        var result = WorldGenPngCompiler.Compile(png, SamplePalettePath, "test_map", 0, 0, opts);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(0, result.ModuleCount);
    }

    // -----------------------------------------------------------------------
    // Test 8: two color types — biome and prefab
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_BiomeAndPrefab_BothEmittedCorrectly()
    {
        // 2x2: top=water(biome), bottom=road(prefab)
        var png = MakePng(2, 2, (x, y) => y == 0 ? Water : RoadWE);
        var result = WorldGenPngCompiler.Compile(png, SamplePalettePath, "test_map", 0, 0);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(2, result.ModuleCount);

        var biome  = result.Manifest!.StaticModules.First(m => m.Type == "biome");
        var prefab = result.Manifest!.StaticModules.First(m => m.Type == "prefab");
        Assert.Equal("water",           biome.Key);
        Assert.Equal("normal_road_WE_00", prefab.Key);
    }

    // -----------------------------------------------------------------------
    // Test 9: origin offset is applied to world coordinates
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_OriginOffset_AppliedToWorldCoords()
    {
        var png    = MakePng(3, 2, Water);
        var result = WorldGenPngCompiler.Compile(png, SamplePalettePath, "test_map", 10682, 8240);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        var mod = result.Manifest!.StaticModules[0];
        Assert.Equal(10682, mod.X1);
        Assert.Equal(8240,  mod.Y1);
        Assert.Equal(10684, mod.X2);  // 10682 + 2
        Assert.Equal(8241,  mod.Y2);  // 8240 + 1
    }

    // -----------------------------------------------------------------------
    // Test 10: output manifest has correct format and map_id
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_Manifest_HasCorrectFormatAndMapId()
    {
        var png    = MakePng(2, 2, Water);
        var result = WorldGenPngCompiler.Compile(png, SamplePalettePath, "my_test_map", 0, 0);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal("my_test_map",                       result.Manifest!.MapId);
        Assert.Equal("pzmapforge.worldgen.layers.v1",    result.Manifest!.Format);
    }

    // -----------------------------------------------------------------------
    // Test 11: serialized manifest passes through WorldGenCompiler
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_SerializedManifest_PassesThroughWorldGenCompiler()
    {
        var png    = MakePng(2, 2, Water);
        var result = WorldGenPngCompiler.Compile(png, SamplePalettePath, "roundtrip_map", 0, 0);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));

        var jsonPath = Path.Combine(_tempDir, "manifest.json");
        File.WriteAllText(jsonPath, WorldGenPngCompiler.SerializeManifest(result.Manifest!));

        var compileResult = WorldGenCompiler.Compile(jsonPath);
        Assert.True(compileResult.IsValid, string.Join("; ", compileResult.Errors));
        Assert.NotNull(compileResult.Lua);
        Assert.Contains("worldgen[\"static_modules\"] = {", compileResult.Lua, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 12: missing PNG file
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_MissingPng_ReturnsError()
    {
        var result = WorldGenPngCompiler.Compile(
            Path.Combine(_tempDir, "nonexistent.png"), SamplePalettePath, "test", 0, 0);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Test 13: missing palette file
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_MissingPalette_ReturnsError()
    {
        var png    = MakePng(2, 2, Water);
        var result = WorldGenPngCompiler.Compile(
            png, Path.Combine(_tempDir, "nonexistent-palette.json"), "test", 0, 0);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Test 14: bad palette format string
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_BadPaletteFormat_ReturnsError()
    {
        var palPath = WritePalette("""{"format":"wrong","entries":[{"color":"#0000FF","type":"biome","key":"water"}]}""");
        var png     = MakePng(2, 2, Water);
        var result  = WorldGenPngCompiler.Compile(png, palPath, "test", 0, 0);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("format", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Test 15: multiple side-by-side same-color segments on same row merge vertically
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_TwoStackedIdenticalRows_ProducesOneModule()
    {
        // 4x2: all water — should produce 1 rectangle spanning both rows
        var png    = MakePng(4, 2, Water);
        var result = WorldGenPngCompiler.Compile(png, SamplePalettePath, "test_map", 0, 0);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(1, result.ModuleCount);
        Assert.Equal(0, result.Manifest!.StaticModules[0].Y1);
        Assert.Equal(1, result.Manifest!.StaticModules[0].Y2);
    }

    // -----------------------------------------------------------------------
    // Test 16: pallette loader — sample palette is valid
    // -----------------------------------------------------------------------

    [Fact]
    public void PaletteLoader_SamplePalette_IsValid()
    {
        var result = WorldGenPngPaletteLoader.Load(SamplePalettePath);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.NotNull(result.Palette);
        Assert.Equal(5, result.Palette!.Entries.Count);
    }

    // -----------------------------------------------------------------------
    // Test 17: palette loader — unknown biome key fails
    // -----------------------------------------------------------------------

    [Fact]
    public void PaletteLoader_UnknownBiomeKey_ReturnsError()
    {
        var palPath = WritePalette("""{"format":"pzmapforge.worldgen.png-palette.v1","entries":[{"color":"#0000FF","type":"biome","key":"INVALID"}]}""");
        var result  = WorldGenPngPaletteLoader.Load(palPath);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("INVALID", StringComparison.Ordinal));
    }

    // -----------------------------------------------------------------------
    // Test 18: palette loader — duplicate color fails
    // -----------------------------------------------------------------------

    [Fact]
    public void PaletteLoader_DuplicateColor_ReturnsError()
    {
        var palPath = WritePalette("""{"format":"pzmapforge.worldgen.png-palette.v1","entries":[{"color":"#0000FF","type":"biome","key":"water"},{"color":"#0000FF","type":"biome","key":"grass_plain"}]}""");
        var result  = WorldGenPngPaletteLoader.Load(palPath);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Test 19: empty PNG (0 pixels) produces no modules
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_AllTransparent_ProducesZeroModules()
    {
        var png    = MakePng(4, 4, Transparent);
        var opts   = new WorldGenPngCompileOptions { IgnoreUnknown = true };
        var result = WorldGenPngCompiler.Compile(png, SamplePalettePath, "test_map", 0, 0, opts);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(0, result.ModuleCount);
    }

    // -----------------------------------------------------------------------
    // Test 20: all five palette entries produce valid modules
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_AllPaletteColors_ProduceFiveModules()
    {
        // 5x1 image: one pixel of each palette color
        var colors = new[] { Water, SandBank, GrassPlain, RoadWE, HighwayNS };
        var png    = MakePng(5, 1, (x, _) => colors[x]);
        var result = WorldGenPngCompiler.Compile(png, SamplePalettePath, "test_map", 0, 0);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(5, result.ModuleCount);

        var keys = result.Manifest!.StaticModules.Select(m => m.Key).ToHashSet();
        Assert.Contains("water",             keys);
        Assert.Contains("sand_bank",         keys);
        Assert.Contains("grass_plain",       keys);
        Assert.Contains("normal_road_WE_00", keys);
        Assert.Contains("highway_NS_00",     keys);
    }
}
