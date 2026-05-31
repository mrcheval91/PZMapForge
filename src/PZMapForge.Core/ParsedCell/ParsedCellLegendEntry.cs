using System.Text.Json.Serialization;

namespace PZMapForge.Core.ParsedCell;

public sealed class ParsedCellLegendEntry
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("gid")]
    public int Gid { get; set; }

    [JsonPropertyName("rgb")]
    public int[] Rgb { get; set; } = [];

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
