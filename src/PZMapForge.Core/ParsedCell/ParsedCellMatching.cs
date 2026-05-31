using System.Text.Json.Serialization;

namespace PZMapForge.Core.ParsedCell;

public sealed class ParsedCellMatching
{
    [JsonPropertyName("exact_pixels")]
    public int ExactPixels { get; set; }

    [JsonPropertyName("nearest_pixels")]
    public int NearestPixels { get; set; }

    [JsonPropertyName("unique_source_colours")]
    public int UniqueSourceColours { get; set; }

    [JsonPropertyName("unmapped_exact_colours")]
    public int UnmappedExactColours { get; set; }
}
