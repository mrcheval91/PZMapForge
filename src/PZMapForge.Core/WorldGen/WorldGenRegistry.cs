namespace PZMapForge.Core.WorldGen;

public static class WorldGenRegistry
{
    // Proven Build 42 worldgen biome keys.
    public static readonly IReadOnlySet<string> Biomes = new HashSet<string>(StringComparer.Ordinal)
    {
        "water",
        "sand_bank",
        "grass_plain",
        "flower_plain",
        "birch_forest",
        "oak_forest",
        "pine_forest",
        "light_birch_forest",
        "light_oak_forest",
        "light_pine_forest",
    };

    // Proven Build 42 worldgen prefab keys.
    public static readonly IReadOnlySet<string> Prefabs = new HashSet<string>(StringComparer.Ordinal)
    {
        "normal_road_WE_00",
        "highway_NS_00",
    };
}
