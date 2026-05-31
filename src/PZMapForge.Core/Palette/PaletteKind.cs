using System.Text.Json.Serialization;

namespace PZMapForge.Core.Palette;

public sealed class PaletteKind
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("gid")]
    public int Gid { get; set; }

    [JsonPropertyName("rgb")]
    public int[] Rgb { get; set; } = [];

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
