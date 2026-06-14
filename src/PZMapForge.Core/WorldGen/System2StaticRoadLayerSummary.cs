using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadLayerSummary
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("file")]
    public string File { get; init; } = string.Empty;

    [JsonPropertyName("class")]
    public string LayerClass { get; init; } = string.Empty;

    [JsonPropertyName("non_empty_pixels")]
    public int NonEmptyPixels { get; init; }

    [JsonPropertyName("intents")]
    public List<string> Intents { get; init; } = new();

    [JsonPropertyName("runs")]
    public List<System2StaticRoadPixelRun> Runs { get; init; } = new();

    [JsonPropertyName("nodes")]
    public List<System2StaticRoadNode> Nodes { get; init; } = new();
}
