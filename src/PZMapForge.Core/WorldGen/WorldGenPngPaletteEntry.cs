using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class WorldGenPngPaletteEntry
{
    [JsonPropertyName("color")]
    public string Color { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;
}
