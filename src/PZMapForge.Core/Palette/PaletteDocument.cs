using System.Text.Json.Serialization;

namespace PZMapForge.Core.Palette;

public sealed class PaletteDocument
{
    [JsonPropertyName("schema")]
    public string Schema { get; set; } = string.Empty;

    [JsonPropertyName("cell_width")]
    public int CellWidth { get; set; }

    [JsonPropertyName("cell_height")]
    public int CellHeight { get; set; }

    [JsonPropertyName("preview_scale")]
    public int PreviewScale { get; set; }

    [JsonPropertyName("tile_size")]
    public int TileSize { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("kinds")]
    public List<PaletteKind> Kinds { get; set; } = [];
}
