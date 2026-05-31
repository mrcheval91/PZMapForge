using System.Text.Json.Serialization;

namespace PZMapForge.Core.ParsedCell;

public sealed class ParsedCellDocument
{
    [JsonPropertyName("schema")]
    public string Schema { get; set; } = string.Empty;

    [JsonPropertyName("tool")]
    public string Tool { get; set; } = string.Empty;

    [JsonPropertyName("claim_boundary")]
    public string ClaimBoundary { get; set; } = string.Empty;

    [JsonPropertyName("source_image")]
    public string SourceImage { get; set; } = string.Empty;

    [JsonPropertyName("source_image_sha256")]
    public string SourceImageSha256 { get; set; } = string.Empty;

    [JsonPropertyName("palette")]
    public string Palette { get; set; } = string.Empty;

    [JsonPropertyName("palette_sha256")]
    public string PaletteSha256 { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("resized")]
    public bool Resized { get; set; }

    [JsonPropertyName("matching")]
    public ParsedCellMatching? Matching { get; set; }

    [JsonPropertyName("legend")]
    public List<ParsedCellLegendEntry> Legend { get; set; } = [];

    [JsonPropertyName("counts")]
    public List<ParsedCellCount> Counts { get; set; } = [];

    [JsonPropertyName("nearest_drift")]
    public List<ParsedCellDrift> NearestDrift { get; set; } = [];

    [JsonPropertyName("rows")]
    public List<string> Rows { get; set; } = [];

    [JsonPropertyName("outputs")]
    public ParsedCellOutputs? Outputs { get; set; }
}
