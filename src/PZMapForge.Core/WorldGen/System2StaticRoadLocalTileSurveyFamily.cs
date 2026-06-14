using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadLocalTileSurveyFamily
{
    [JsonPropertyName("candidate_family")]
    public string CandidateFamily { get; init; } = string.Empty;

    [JsonPropertyName("source_intents")]
    public List<string> SourceIntents { get; init; } = new();

    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("resolution_status")]
    public string ResolutionStatus { get; init; } = "UNRESOLVED_NEEDS_TILE_SURVEY";

    [JsonPropertyName("confidence")]
    public string Confidence { get; init; } = "NONE_YET";

    [JsonPropertyName("search_terms")]
    public List<string> SearchTerms { get; init; } = new();

    [JsonPropertyName("candidate_tiles")]
    public List<System2StaticRoadLocalTileCandidate> CandidateTiles { get; init; } = new();

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}
