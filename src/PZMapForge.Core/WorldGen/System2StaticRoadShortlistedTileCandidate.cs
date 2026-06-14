using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadShortlistedTileCandidate
{
    [JsonPropertyName("rank")]
    public int Rank { get; init; }

    [JsonPropertyName("tile_name")]
    public string TileName { get; init; } = string.Empty;

    [JsonPropertyName("source_file")]
    public string SourceFile { get; init; } = string.Empty;

    [JsonPropertyName("score")]
    public int Score { get; init; }

    [JsonPropertyName("score_reasons")]
    public List<string> ScoreReasons { get; init; } = new();

    [JsonPropertyName("source_confidence")]
    public string SourceConfidence { get; init; } = "LOCAL_TEXT_MATCH_ONLY";

    [JsonPropertyName("confidence")]
    public string Confidence { get; init; } = "LOCAL_TEXT_MATCH_RANKED_ONLY";
}
