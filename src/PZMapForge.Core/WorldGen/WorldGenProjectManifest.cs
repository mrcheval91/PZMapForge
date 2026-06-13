using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class WorldGenProjectManifest
{
    [JsonPropertyName("format")]
    public string Format { get; init; } = string.Empty;

    [JsonPropertyName("map_id")]
    public string MapId { get; init; } = string.Empty;

    [JsonPropertyName("origin_x")]
    public int OriginX { get; init; }

    [JsonPropertyName("origin_y")]
    public int OriginY { get; init; }

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }

    [JsonPropertyName("layers")]
    public List<WorldGenProjectLayer> Layers { get; init; } = [];
}
