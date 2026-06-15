using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadFilteredTileCandidateShortlistFamily
{
    [JsonPropertyName("candidate_family")]
    public string CandidateFamily { get; init; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("input_candidate_count")]
    public int InputCandidateCount { get; init; }

    [JsonPropertyName("shortlisted_candidate_count")]
    public int ShortlistedCandidateCount { get; init; }

    [JsonPropertyName("resolution_status")]
    public string ResolutionStatus { get; init; } = string.Empty;

    [JsonPropertyName("confidence")]
    public string Confidence { get; init; } = "LOCAL_FILTERED_TEXT_MATCH_RANKED_ONLY";

    [JsonPropertyName("candidates")]
    public List<System2StaticRoadFilteredShortlistedTileCandidate> Candidates { get; init; } = new();

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}
