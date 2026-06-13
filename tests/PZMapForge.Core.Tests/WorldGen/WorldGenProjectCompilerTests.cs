using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

/// <summary>
/// Tests for WorldGenProjectCompiler. Creates PNGs and palette/project JSON
/// files programmatically in a temp directory. No committed fixtures.
/// Windows-only: uses System.Drawing.Common.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WorldGenProjectCompilerTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-project-compiler-tests", Path.GetRandomFileName());

    public WorldGenProjectCompilerTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // -----------------------------------------------------------------------
    // Colors matching worldgen-png-palette.json
    // -----------------------------------------------------------------------

    private static readonly Color Water      = Color.FromArgb(255,   0,   0, 255);
    private static readonly Color SandBank   = Color.FromArgb(255, 216, 192, 128);
    private static readonly Color GrassPlain = Color.FromArgb(255,   0, 170,   0);
    private static readonly Color RoadWE     = Color.FromArgb(255, 255,   0,   0);
    private static readonly Color HighwayNS  = Color.FromArgb(255, 170,   0,   0);
    private static readonly Color Unknown    = Color.FromArgb(255, 123,  45,  67);
    private static readonly Color Transparent = Color.FromArgb(0,    0,   0,   0);

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private string MakePng(string name, int width, int height, Color fill)
    {
        var path = Path.Combine(_tempDir, name);
        using var bmp = new Bitmap(width, height);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(fill);
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    private string MakePng(string name, int width, int height, Func<int, int, Color> pixelFn)
    {
        var path = Path.Combine(_tempDir, name);
        using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                bmp.SetPixel(x, y, pixelFn(x, y));
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    private string WritePalette(string filename = "palette.json")
    {
        var path = Path.Combine(_tempDir, filename);
        File.WriteAllText(path, """
            {
              "format": "pzmapforge.worldgen.png-palette.v1",
              "entries": [
                { "color": "#0000FF", "type": "biome",  "key": "water" },
                { "color": "#D8C080", "type": "biome",  "key": "sand_bank" },
                { "color": "#00AA00", "type": "biome",  "key": "grass_plain" },
                { "color": "#FF0000", "type": "prefab", "key": "normal_road_WE_00" },
                { "color": "#AA0000", "type": "prefab", "key": "highway_NS_00" }
              ]
            }
            """, Encoding.UTF8);
        return path;
    }

    private string WriteProject(string mapId, int width, int height, int originX, int originY,
        IEnumerable<(string id, string pngName, string paletteName, int priority)> layers)
    {
        var layersJson = string.Join(",\n    ", layers.Select(l =>
            $"{{\"id\":\"{l.id}\",\"path\":\"{l.pngName}\",\"palette\":\"{l.paletteName}\",\"priority\":{l.priority}}}"));

        var json = $$"""
            {
              "format": "pzmapforge.worldgen.project.v1",
              "map_id": "{{mapId}}",
              "origin_x": {{originX}},
              "origin_y": {{originY}},
              "width": {{width}},
              "height": {{height}},
              "layers": [
                {{layersJson}}
              ]
            }
            """;

        var path = Path.Combine(_tempDir, $"project-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    // -----------------------------------------------------------------------
    // Test 1: single water layer produces one module with world coords
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_SingleWaterLayer_ProducesOneModule()
    {
        var pal     = WritePalette();
        MakePng("water.png", 4, 3, Water);

        var project = WriteProject("test_map", 4, 3, 10000, 8000, [("water", "water.png", "palette.json", 10)]);
        var result  = WorldGenProjectCompiler.Compile(project);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(1, result.ModuleCount);

        var mod = result.Manifest!.StaticModules[0];
        Assert.Equal("biome", mod.Type);
        Assert.Equal("water", mod.Key);
        Assert.Equal(10000,   mod.X1);
        Assert.Equal(8000,    mod.Y1);
        Assert.Equal(10003,   mod.X2);
        Assert.Equal(8002,    mod.Y2);
    }

    // -----------------------------------------------------------------------
    // Test 2: higher priority layer overrides lower priority per pixel
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_HigherPriorityOverridesLower()
    {
        var pal = WritePalette();
        MakePng("water.png",    4, 4, Water);    // priority 10: fill with water
        MakePng("sandbank.png", 4, 4, SandBank); // priority 50: fill with sand_bank

        var project = WriteProject("test_map", 4, 4, 0, 0, [
            ("water",    "water.png",    "palette.json", 10),
            ("sandbank", "sandbank.png", "palette.json", 50),
        ]);
        var result = WorldGenProjectCompiler.Compile(project);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(1, result.ModuleCount);
        Assert.Equal("sand_bank", result.Manifest!.StaticModules[0].Key);
    }

    // -----------------------------------------------------------------------
    // Test 3: transparent pixels do not overwrite existing grid content
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_TransparentPixelDoesNotOverwrite()
    {
        var pal = WritePalette();
        MakePng("water.png", 4, 4, Water);        // priority 10: water everywhere
        MakePng("over.png",  4, 4, Transparent);  // priority 50: all transparent → no override

        var project = WriteProject("test_map", 4, 4, 0, 0, [
            ("water", "water.png", "palette.json", 10),
            ("over",  "over.png",  "palette.json", 50),
        ]);
        var result = WorldGenProjectCompiler.Compile(project);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(1, result.ModuleCount);
        Assert.Equal("water", result.Manifest!.StaticModules[0].Key);
    }

    // -----------------------------------------------------------------------
    // Test 4: two layers combine — partial override
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_TwoLayers_PartialOverride_ProducesCorrectModules()
    {
        var pal = WritePalette();
        // 4x4: water everywhere
        MakePng("water.png", 4, 4, Water);
        // 4x4: top 2 rows = grass, bottom 2 transparent
        MakePng("grass.png", 4, 4, (x, y) => y < 2 ? GrassPlain : Transparent);

        var project = WriteProject("test_map", 4, 4, 0, 0, [
            ("water", "water.png", "palette.json", 10),
            ("grass", "grass.png", "palette.json", 50),
        ]);
        var result = WorldGenProjectCompiler.Compile(project);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(2, result.ModuleCount);

        var keys = result.Manifest!.StaticModules.Select(m => m.Key).ToHashSet();
        Assert.Contains("grass_plain", keys);
        Assert.Contains("water",       keys);
    }

    // -----------------------------------------------------------------------
    // Test 5: layer order is deterministic when priorities tie (alphabetical id)
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_TiedPriority_DeterministicByLayerId()
    {
        var pal = WritePalette();
        MakePng("a_water.png",    4, 4, Water);    // id="a_layer", priority=10
        MakePng("b_sandbank.png", 4, 4, SandBank); // id="b_layer", priority=10

        // Both priority 10: "a_layer" < "b_layer" alphabetically, so a is applied first
        // and b overwrites it → result should be sand_bank
        var project = WriteProject("test_map", 4, 4, 0, 0, [
            ("b_layer", "b_sandbank.png", "palette.json", 10),
            ("a_layer", "a_water.png",    "palette.json", 10),
        ]);
        var result = WorldGenProjectCompiler.Compile(project);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(1, result.ModuleCount);
        // a_layer (water) applied first → b_layer (sand_bank) overwrites → sand_bank wins
        Assert.Equal("sand_bank", result.Manifest!.StaticModules[0].Key);
    }

    // -----------------------------------------------------------------------
    // Test 6: missing layer PNG fails validation
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_MissingLayerPng_ReturnsError()
    {
        WritePalette();
        var project = WriteProject("test_map", 4, 4, 0, 0, [
            ("water", "nonexistent.png", "palette.json", 10),
        ]);
        var result = WorldGenProjectCompiler.Compile(project);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("PNG not found", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Test 7: missing palette fails validation
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_MissingPalette_ReturnsError()
    {
        MakePng("water.png", 4, 4, Water);
        var project = WriteProject("test_map", 4, 4, 0, 0, [
            ("water", "water.png", "nonexistent-palette.json", 10),
        ]);
        var result = WorldGenProjectCompiler.Compile(project);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("palette not found", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Test 8: wrong format string fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_WrongFormat_ReturnsError()
    {
        var path = Path.Combine(_tempDir, "bad.json");
        File.WriteAllText(path, """{"format":"wrong","map_id":"x","origin_x":0,"origin_y":0,"width":4,"height":4,"layers":[]}""");
        var result = WorldGenProjectCompiler.Compile(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("format", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Test 9: missing map_id fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_MissingMapId_ReturnsError()
    {
        WritePalette();
        MakePng("water.png", 4, 4, Water);
        var path = Path.Combine(_tempDir, "noid.json");
        File.WriteAllText(path, """{"format":"pzmapforge.worldgen.project.v1","map_id":"","origin_x":0,"origin_y":0,"width":4,"height":4,"layers":[{"id":"w","path":"water.png","palette":"palette.json","priority":10}]}""");
        var result = WorldGenProjectCompiler.Compile(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("map_id", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Test 10: zero width fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_ZeroWidth_ReturnsError()
    {
        WritePalette();
        MakePng("water.png", 4, 4, Water);
        var path = Path.Combine(_tempDir, "badw.json");
        File.WriteAllText(path, """{"format":"pzmapforge.worldgen.project.v1","map_id":"x","origin_x":0,"origin_y":0,"width":0,"height":4,"layers":[{"id":"w","path":"water.png","palette":"palette.json","priority":10}]}""");
        var result = WorldGenProjectCompiler.Compile(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("width", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Test 11: empty layers list fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_EmptyLayers_ReturnsError()
    {
        var path = Path.Combine(_tempDir, "nolayers.json");
        File.WriteAllText(path, """{"format":"pzmapforge.worldgen.project.v1","map_id":"x","origin_x":0,"origin_y":0,"width":4,"height":4,"layers":[]}""");
        var result = WorldGenProjectCompiler.Compile(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("layers", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Test 12: duplicate layer id fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_DuplicateLayerId_ReturnsError()
    {
        WritePalette();
        MakePng("water.png", 4, 4, Water);
        var project = WriteProject("test_map", 4, 4, 0, 0, [
            ("water", "water.png", "palette.json", 10),
            ("water", "water.png", "palette.json", 20),
        ]);
        var result = WorldGenProjectCompiler.Compile(project);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate layer id", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Test 13: unknown color fails by default
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_UnknownColor_FailsByDefault()
    {
        WritePalette();
        MakePng("unknown.png", 4, 4, Unknown);
        var project = WriteProject("test_map", 4, 4, 0, 0, [
            ("layer1", "unknown.png", "palette.json", 10),
        ]);
        var result = WorldGenProjectCompiler.Compile(project);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Unknown color", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Test 14: unknown color ignored when --ignore-unknown
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_UnknownColor_IgnoredWithOption()
    {
        WritePalette();
        MakePng("unknown.png", 4, 4, Unknown);
        var project = WriteProject("test_map", 4, 4, 0, 0, [
            ("layer1", "unknown.png", "palette.json", 10),
        ]);
        var opts   = new WorldGenPngCompileOptions { IgnoreUnknown = true };
        var result = WorldGenProjectCompiler.Compile(project, opts);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(0, result.ModuleCount);
    }

    // -----------------------------------------------------------------------
    // Test 15: generated manifest passes through WorldGenCompiler
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_GeneratedManifest_PassesThroughWorldGenCompiler()
    {
        WritePalette();
        MakePng("water.png", 4, 4, Water);
        var project = WriteProject("roundtrip_map", 4, 4, 10000, 8000, [
            ("water", "water.png", "palette.json", 10),
        ]);

        var projectResult = WorldGenProjectCompiler.Compile(project);
        Assert.True(projectResult.IsValid, string.Join("; ", projectResult.Errors));

        var jsonPath = Path.Combine(_tempDir, "manifest.json");
        File.WriteAllText(jsonPath, WorldGenPngCompiler.SerializeManifest(projectResult.Manifest!));

        var compileResult = WorldGenCompiler.Compile(jsonPath);
        Assert.True(compileResult.IsValid, string.Join("; ", compileResult.Errors));
        Assert.NotNull(compileResult.Lua);
        Assert.Contains("worldgen[\"static_modules\"] = {", compileResult.Lua!, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 16: output manifest has correct format and map_id
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_ManifestHasCorrectFormatAndMapId()
    {
        WritePalette();
        MakePng("water.png", 2, 2, Water);
        var project = WriteProject("my_project", 2, 2, 0, 0, [
            ("water", "water.png", "palette.json", 10),
        ]);
        var result = WorldGenProjectCompiler.Compile(project);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal("my_project",                        result.Manifest!.MapId);
        Assert.Equal("pzmapforge.worldgen.layers.v1",    result.Manifest!.Format);
    }

    // -----------------------------------------------------------------------
    // Test 17: project file not found
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_ProjectFileNotFound_ReturnsError()
    {
        var result = WorldGenProjectCompiler.Compile(
            Path.Combine(_tempDir, "nonexistent.json"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Test 18: module ids are formatted with key + counter
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_ModuleIds_AreFormattedWithKeyAndCounter()
    {
        WritePalette();
        MakePng("water.png", 4, 4, Water);
        var project = WriteProject("test_map", 4, 4, 0, 0, [
            ("water", "water.png", "palette.json", 10),
        ]);
        var result = WorldGenProjectCompiler.Compile(project);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal("water_000001", result.Manifest!.StaticModules[0].Id);
    }
}
