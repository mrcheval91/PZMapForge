using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class WorldGenModule
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("x1")]
    public int X1 { get; init; }

    [JsonPropertyName("y1")]
    public int Y1 { get; init; }

    [JsonPropertyName("x2")]
    public int X2 { get; init; }

    [JsonPropertyName("y2")]
    public int Y2 { get; init; }
}
