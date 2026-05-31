using System.Text.Json.Serialization;

namespace PZMapForge.Core.ParsedCell;

public sealed class ParsedCellDrift
{
    [JsonPropertyName("source_rgb")]
    public string SourceRgb { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("nearest_kind")]
    public string NearestKind { get; set; } = string.Empty;

    [JsonPropertyName("nearest_rgb")]
    public string NearestRgb { get; set; } = string.Empty;

    [JsonPropertyName("distance")]
    public double Distance { get; set; }
}
