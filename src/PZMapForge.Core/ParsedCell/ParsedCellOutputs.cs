using System.Text.Json.Serialization;

namespace PZMapForge.Core.ParsedCell;

public sealed class ParsedCellOutputs
{
    [JsonPropertyName("json")]
    public string Json { get; set; } = string.Empty;

    [JsonPropertyName("report")]
    public string Report { get; set; } = string.Empty;

    [JsonPropertyName("preview")]
    public string Preview { get; set; } = string.Empty;

    [JsonPropertyName("generated_tileset")]
    public string GeneratedTileset { get; set; } = string.Empty;

    [JsonPropertyName("tmx")]
    public string Tmx { get; set; } = string.Empty;
}
