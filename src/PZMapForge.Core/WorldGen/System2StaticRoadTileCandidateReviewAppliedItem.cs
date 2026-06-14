using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadTileCandidateReviewAppliedItem
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

    [JsonPropertyName("review_status")]
    public string ReviewStatus { get; init; } = "NEEDS_MANUAL_REVIEW";

    [JsonPropertyName("human_note")]
    public string HumanNote { get; init; } = string.Empty;

    [JsonPropertyName("recommended_next_action")]
    public string RecommendedNextAction { get; init; } =
        "Inspect in TileZed or tile sheet preview before writer use.";

    [JsonPropertyName("confidence")]
    public string Confidence { get; init; } = "LOCAL_TEXT_MATCH_RANKED_ONLY";
}
