using System.Text;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class WorldGenCompilerTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-worldgen-tests", Path.GetRandomFileName());

    public WorldGenCompilerTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string SamplePath =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "examples", "worldgen", "worldgen_layers_sample.json"));

    private string WriteManifest(string content)
    {
        var path = Path.Combine(_tempDir, $"manifest-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    // -----------------------------------------------------------------------
    // Sample manifest compiles successfully
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_SampleManifest_IsValid()
    {
        var result = WorldGenCompiler.Compile(SamplePath);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.NotNull(result.Lua);
        Assert.Equal("pzmapforge_build42_candidate_v4_001", result.MapId);
        Assert.Equal(3, result.ModuleCount);
    }

    // -----------------------------------------------------------------------
    // Output contains required Lua markers
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_SampleManifest_LuaContainsPrintMarkers()
    {
        var result = WorldGenCompiler.Compile(SamplePath);

        Assert.Contains("print(\"PZMAPFORGE_WORLDGENOVERRIDE_LOADED\")",         result.Lua!, StringComparison.Ordinal);
        Assert.Contains("print(\"PZMAPFORGE_WORLDGENOVERRIDE_MAP_ID=",           result.Lua!, StringComparison.Ordinal);
        Assert.Contains("print(\"PZMAPFORGE_WORLDGENOVERRIDE_MODULE_COUNT=",     result.Lua!, StringComparison.Ordinal);
    }

    [Fact]
    public void Compile_SampleManifest_LuaContainsStaticModulesAssignment()
    {
        var result = WorldGenCompiler.Compile(SamplePath);

        Assert.Contains("worldgen[\"static_modules\"] = {", result.Lua!, StringComparison.Ordinal);
    }

    [Fact]
    public void Compile_SampleManifest_LuaBiomeUsesCorrectForm()
    {
        var result = WorldGenCompiler.Compile(SamplePath);

        Assert.Contains("biome = worldgen.biomes.water",       result.Lua!, StringComparison.Ordinal);
        Assert.Contains("biome = worldgen.biomes.grass_plain", result.Lua!, StringComparison.Ordinal);
    }

    [Fact]
    public void Compile_SampleManifest_LuaPrefabUsesCorrectForm()
    {
        var result = WorldGenCompiler.Compile(SamplePath);

        Assert.Contains("prefab = worldgen.prefabs.normal_road_WE_00", result.Lua!, StringComparison.Ordinal);
    }

    [Fact]
    public void Compile_SampleManifest_LuaPositionUsesCorrectForm()
    {
        var result = WorldGenCompiler.Compile(SamplePath);

        Assert.Contains("position = { xmin = 10682, xmax = 10722, ymin = 8240, ymax = 8252 },", result.Lua!, StringComparison.Ordinal);
    }

    [Fact]
    public void Compile_SampleManifest_LuaContainsModuleIdComments()
    {
        var result = WorldGenCompiler.Compile(SamplePath);

        Assert.Contains("-- water_l_horizontal",  result.Lua!, StringComparison.Ordinal);
        Assert.Contains("-- shore_grass_north",   result.Lua!, StringComparison.Ordinal);
        Assert.Contains("-- road_we_south",       result.Lua!, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Validation: missing file
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_MissingFile_ReturnsError()
    {
        var result = WorldGenCompiler.Compile(Path.Combine(_tempDir, "nonexistent.json"));

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("not found", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Validation: invalid JSON
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_InvalidJson_ReturnsError()
    {
        var path = WriteManifest("not valid json {{{");
        var result = WorldGenCompiler.Compile(path);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("JSON parse error", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Validation: missing map_id
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_MissingMapId_ReturnsError()
    {
        var path = WriteManifest("""{"format":"pzmapforge.worldgen.layers.v1","static_modules":[{"id":"a","type":"biome","key":"water","x1":0,"y1":0,"x2":10,"y2":10}]}""");
        var result = WorldGenCompiler.Compile(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("map_id", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Validation: wrong format
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_WrongFormat_ReturnsError()
    {
        var path = WriteManifest("""{"map_id":"x","format":"wrong.format","static_modules":[{"id":"a","type":"biome","key":"water","x1":0,"y1":0,"x2":10,"y2":10}]}""");
        var result = WorldGenCompiler.Compile(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("format", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Validation: empty modules list
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_EmptyModules_ReturnsError()
    {
        var path = WriteManifest("""{"map_id":"x","format":"pzmapforge.worldgen.layers.v1","static_modules":[]}""");
        var result = WorldGenCompiler.Compile(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("static_modules", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Validation: unknown biome key
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_UnknownBiomeKey_ReturnsError()
    {
        var path = WriteManifest("""{"map_id":"x","format":"pzmapforge.worldgen.layers.v1","static_modules":[{"id":"a","type":"biome","key":"UNKNOWN_BIOME","x1":0,"y1":0,"x2":10,"y2":10}]}""");
        var result = WorldGenCompiler.Compile(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("UNKNOWN_BIOME", StringComparison.Ordinal));
    }

    // -----------------------------------------------------------------------
    // Validation: unknown prefab key
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_UnknownPrefabKey_ReturnsError()
    {
        var path = WriteManifest("""{"map_id":"x","format":"pzmapforge.worldgen.layers.v1","static_modules":[{"id":"a","type":"prefab","key":"UNKNOWN_PREFAB","x1":0,"y1":0,"x2":10,"y2":10}]}""");
        var result = WorldGenCompiler.Compile(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("UNKNOWN_PREFAB", StringComparison.Ordinal));
    }

    // -----------------------------------------------------------------------
    // Validation: invalid type value
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_InvalidType_ReturnsError()
    {
        var path = WriteManifest("""{"map_id":"x","format":"pzmapforge.worldgen.layers.v1","static_modules":[{"id":"a","type":"zone","key":"water","x1":0,"y1":0,"x2":10,"y2":10}]}""");
        var result = WorldGenCompiler.Compile(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("biome", StringComparison.OrdinalIgnoreCase) && e.Contains("prefab", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Validation: x2 < x1
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_X2LessThanX1_ReturnsError()
    {
        var path = WriteManifest("""{"map_id":"x","format":"pzmapforge.worldgen.layers.v1","static_modules":[{"id":"a","type":"biome","key":"water","x1":100,"y1":0,"x2":50,"y2":10}]}""");
        var result = WorldGenCompiler.Compile(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("x2", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Validation: y2 < y1
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_Y2LessThanY1_ReturnsError()
    {
        var path = WriteManifest("""{"map_id":"x","format":"pzmapforge.worldgen.layers.v1","static_modules":[{"id":"a","type":"biome","key":"water","x1":0,"y1":100,"x2":10,"y2":50}]}""");
        var result = WorldGenCompiler.Compile(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("y2", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Validation: duplicate module id
    // -----------------------------------------------------------------------

    [Fact]
    public void Compile_DuplicateId_ReturnsError()
    {
        var path = WriteManifest("""{"map_id":"x","format":"pzmapforge.worldgen.layers.v1","static_modules":[{"id":"a","type":"biome","key":"water","x1":0,"y1":0,"x2":10,"y2":10},{"id":"a","type":"biome","key":"grass_plain","x1":0,"y1":11,"x2":10,"y2":20}]}""");
        var result = WorldGenCompiler.Compile(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Registry: all known biomes are valid
    // -----------------------------------------------------------------------

    [Fact]
    public void Registry_AllBiomesCompile()
    {
        foreach (var biome in WorldGenRegistry.Biomes)
        {
            var path = WriteManifest($"{{\"map_id\":\"x\",\"format\":\"pzmapforge.worldgen.layers.v1\",\"static_modules\":[{{\"id\":\"m\",\"type\":\"biome\",\"key\":\"{biome}\",\"x1\":0,\"y1\":0,\"x2\":10,\"y2\":10}}]}}");
            var result = WorldGenCompiler.Compile(path);
            Assert.True(result.IsValid, $"Biome '{biome}' failed: {string.Join("; ", result.Errors)}");
        }
    }

    // -----------------------------------------------------------------------
    // Registry: all known prefabs are valid
    // -----------------------------------------------------------------------

    [Fact]
    public void Registry_AllPrefabsCompile()
    {
        foreach (var prefab in WorldGenRegistry.Prefabs)
        {
            var path = WriteManifest($"{{\"map_id\":\"x\",\"format\":\"pzmapforge.worldgen.layers.v1\",\"static_modules\":[{{\"id\":\"m\",\"type\":\"prefab\",\"key\":\"{prefab}\",\"x1\":0,\"y1\":0,\"x2\":10,\"y2\":10}}]}}");
            var result = WorldGenCompiler.Compile(path);
            Assert.True(result.IsValid, $"Prefab '{prefab}' failed: {string.Join("; ", result.Errors)}");
        }
    }
}
