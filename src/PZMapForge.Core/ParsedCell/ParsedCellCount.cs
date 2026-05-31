using System.Text.Json.Serialization;

namespace PZMapForge.Core.ParsedCell;

public sealed class ParsedCellCount
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("gid")]
    public int Gid { get; set; }

    [JsonPropertyName("pixels")]
    public int Pixels { get; set; }
}
