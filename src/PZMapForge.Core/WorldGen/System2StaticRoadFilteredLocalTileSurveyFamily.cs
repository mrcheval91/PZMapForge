using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadFilteredLocalTileSurveyFamily
{
    [JsonPropertyName("candidate_family")]
    public string CandidateFamily { get; init; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("source_intents")]
    public List<string> SourceIntents { get; init; } = new();

    [JsonPropertyName("resolution_status")]
    public string ResolutionStatus { get; init; } = string.Empty;

    [JsonPropertyName("confidence")]
    public string Confidence { get; init; } = string.Empty;

    [JsonPropertyName("search_terms")]
    public List<string> SearchTerms { get; init; } = new();

    [JsonPropertyName("candidate_tiles")]
    public List<System2StaticRoadFilteredLocalTileCandidate> CandidateTiles { get; init; } = new();

    [JsonPropertyName("excluded_candidate_count")]
    public int ExcludedCandidateCount { get; init; }

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}
