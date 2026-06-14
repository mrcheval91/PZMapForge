using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadHumanApprovedTileCandidate
{
    [JsonPropertyName("candidate_family")]
    public string CandidateFamily { get; init; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

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

    [JsonPropertyName("human_note")]
    public string HumanNote { get; init; } = string.Empty;

    [JsonPropertyName("review_status")]
    public string ReviewStatus { get; init; } = string.Empty;

    [JsonPropertyName("confidence")]
    public string Confidence { get; init; } = string.Empty;

    [JsonPropertyName("recommended_next_action")]
    public string RecommendedNextAction { get; init; } = string.Empty;
}
