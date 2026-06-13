using System.Text.Json;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlLayerPackTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string PackRoot =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack");

    private static string File(params string[] parts) =>
        Path.Combine(new[] { PackRoot }.Concat(parts).ToArray());

    // -----------------------------------------------------------------------
    // Project and palette files exist
    // -----------------------------------------------------------------------

    [Fact]
    public void DeadMtl_ProjectManifest_Exists() =>
        Assert.True(System.IO.File.Exists(File("deadmtl_worldgen_project.json")));

    [Fact]
    public void DeadMtl_Readme_Exists() =>
        Assert.True(System.IO.File.Exists(File("README.md")));

    [Fact]
    public void DeadMtl_WorldgenPalette_Exists() =>
        Assert.True(System.IO.File.Exists(File("palettes", "worldgen-png-palette.json")));

    [Fact]
    public void DeadMtl_ZoningPalette_Exists() =>
        Assert.True(System.IO.File.Exists(File("palettes", "zoning-palette.json")));

    [Fact]
    public void DeadMtl_MetadataPalette_Exists() =>
        Assert.True(System.IO.File.Exists(File("palettes", "metadata-palette.json")));

    // -----------------------------------------------------------------------
    // System 1 (supported) layer PNGs exist
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("water.png")]
    [InlineData("shore.png")]
    [InlineData("parks_forest.png")]
    [InlineData("roads_major.png")]
    public void DeadMtl_SupportedLayer_Exists(string name) =>
        Assert.True(System.IO.File.Exists(File("layers", name)));

    // -----------------------------------------------------------------------
    // System 2-4 (future) placeholder layer PNGs exist
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("roads_local.png")]
    [InlineData("zones_residential.png")]
    [InlineData("zones_commercial.png")]
    [InlineData("zones_industrial.png")]
    [InlineData("placed_buildings.png")]
    [InlineData("props.png")]
    [InlineData("npc_zones.png")]
    [InlineData("ownership.png")]
    public void DeadMtl_PlaceholderLayer_Exists(string name) =>
        Assert.True(System.IO.File.Exists(File("layers", name)));

    // -----------------------------------------------------------------------
    // Scripts exist
    // -----------------------------------------------------------------------

    [Fact]
    public void DeadMtl_GenerateScript_Exists() =>
        Assert.True(System.IO.File.Exists(File("scripts", "generate-empty-layer-pack.ps1")));

    [Fact]
    public void DeadMtl_ValidateScript_Exists() =>
        Assert.True(System.IO.File.Exists(File("scripts", "validate-layer-pack.ps1")));

    // -----------------------------------------------------------------------
    // Project manifest JSON structure
    // -----------------------------------------------------------------------

    private static JsonDocument LoadManifest() =>
        JsonDocument.Parse(System.IO.File.ReadAllText(
            Path.Combine(PackRoot, "deadmtl_worldgen_project.json")));

    [Fact]
    public void DeadMtl_ProjectManifest_HasCorrectFormat()
    {
        using var doc = LoadManifest();
        Assert.Equal("pzmapforge.worldgen.project.v1",
            doc.RootElement.GetProperty("format").GetString());
    }

    [Fact]
    public void DeadMtl_ProjectManifest_HasMapId()
    {
        using var doc = LoadManifest();
        var mapId = doc.RootElement.GetProperty("map_id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(mapId));
    }

    [Fact]
    public void DeadMtl_ProjectManifest_HasCorrectOrigin()
    {
        using var doc = LoadManifest();
        var root = doc.RootElement;
        Assert.Equal(10580, root.GetProperty("origin_x").GetInt32());
        Assert.Equal(8200,  root.GetProperty("origin_y").GetInt32());
    }

    [Fact]
    public void DeadMtl_ProjectManifest_OnlyContainsSupportedLayers()
    {
        var supported = new HashSet<string>(StringComparer.Ordinal)
            { "water", "shore", "parks_forest", "roads_major" };

        using var doc    = LoadManifest();
        var layers       = doc.RootElement.GetProperty("layers").EnumerateArray();
        var unsupported  = layers
            .Select(l => l.GetProperty("id").GetString() ?? string.Empty)
            .Where(id => !supported.Contains(id))
            .ToList();

        Assert.Empty(unsupported);
    }

    [Fact]
    public void DeadMtl_ProjectManifest_HasFourSupportedLayers()
    {
        using var doc = LoadManifest();
        var count     = doc.RootElement.GetProperty("layers").GetArrayLength();
        Assert.Equal(4, count);
    }

    [Fact]
    public void DeadMtl_ProjectManifest_PlaceholderLayersNotInProject()
    {
        var placeholders = new[]
        {
            "roads_local", "zones_residential", "zones_commercial", "zones_industrial",
            "placed_buildings", "props", "npc_zones", "ownership"
        };

        using var doc   = LoadManifest();
        var layerIds    = doc.RootElement.GetProperty("layers").EnumerateArray()
            .Select(l => l.GetProperty("id").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var placeholder in placeholders)
            Assert.DoesNotContain(placeholder, layerIds);
    }

    // -----------------------------------------------------------------------
    // Worldgen palette has correct format and known keys
    // -----------------------------------------------------------------------

    [Fact]
    public void DeadMtl_WorldgenPalette_HasCorrectFormat()
    {
        using var doc = JsonDocument.Parse(System.IO.File.ReadAllText(
            File("palettes", "worldgen-png-palette.json")));
        Assert.Equal("pzmapforge.worldgen.png-palette.v1",
            doc.RootElement.GetProperty("format").GetString());
    }

    [Fact]
    public void DeadMtl_WorldgenPalette_ContainsWaterEntry()
    {
        using var doc   = JsonDocument.Parse(System.IO.File.ReadAllText(
            File("palettes", "worldgen-png-palette.json")));
        var entries     = doc.RootElement.GetProperty("entries").EnumerateArray();
        var waterExists = entries.Any(e =>
            e.GetProperty("type").GetString() == "biome" &&
            e.GetProperty("key").GetString()  == "water");
        Assert.True(waterExists, "Palette must contain biome:water");
    }
}
