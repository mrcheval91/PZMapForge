using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadLocalTileCandidate
{
    [JsonPropertyName("tile_name")]
    public string TileName { get; init; } = string.Empty;

    [JsonPropertyName("source_file")]
    public string SourceFile { get; init; } = string.Empty;

    [JsonPropertyName("match_reason")]
    public string MatchReason { get; init; } = string.Empty;

    [JsonPropertyName("confidence")]
    public string Confidence { get; init; } = "LOCAL_TEXT_MATCH_ONLY";
}
