using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadTileCandidateReviewFamily
{
    [JsonPropertyName("candidate_family")]
    public string CandidateFamily { get; init; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("input_candidate_count")]
    public int InputCandidateCount { get; init; }

    [JsonPropertyName("shortlisted_candidate_count")]
    public int ShortlistedCandidateCount { get; init; }

    [JsonPropertyName("review_items")]
    public List<System2StaticRoadTileCandidateReviewItem> ReviewItems { get; init; } = new();
}
