using System.Text.Json.Serialization;

namespace PZMapForge.Core.Layers;

public sealed class LayerManifest
{
    [JsonPropertyName("schema")]
    public string Schema { get; set; } = string.Empty;

    [JsonPropertyName("claim_boundary")]
    public string ClaimBoundary { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("layers")]
    public List<LayerManifestLayer> Layers { get; set; } = [];

    [JsonPropertyName("precedence")]
    public List<string> Precedence { get; set; } = [];
}
