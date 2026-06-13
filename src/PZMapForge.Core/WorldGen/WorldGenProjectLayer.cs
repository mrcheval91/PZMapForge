using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class WorldGenProjectLayer
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("palette")]
    public string Palette { get; init; } = string.Empty;

    [JsonPropertyName("priority")]
    public int Priority { get; init; }
}
