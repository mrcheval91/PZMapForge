using System.Text.Json.Serialization;

namespace PZMapForge.Core.Layers;

public sealed class LayerManifestLayer
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string FilePath { get; set; } = string.Empty;

    [JsonPropertyName("allowed_kinds")]
    public List<string> AllowedKinds { get; set; } = [];
}
