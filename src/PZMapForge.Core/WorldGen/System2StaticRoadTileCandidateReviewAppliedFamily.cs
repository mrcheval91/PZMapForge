using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadTileCandidateReviewAppliedFamily
{
    [JsonPropertyName("candidate_family")]
    public string CandidateFamily { get; init; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("review_items")]
    public List<System2StaticRoadTileCandidateReviewAppliedItem> ReviewItems { get; init; } = new();
}
