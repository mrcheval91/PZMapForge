using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadTileCandidateReview
{
    [JsonPropertyName("format")]
    public string Format { get; init; } = "pzmapforge.deadmtl.system2.static-road-tile-candidate-review.v1";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "REVIEW_PACKET_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; init; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; init; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("source_shortlist")]
    public string SourceShortlist { get; init; } = string.Empty;

    [JsonPropertyName("families")]
    public List<System2StaticRoadTileCandidateReviewFamily> Families { get; init; } = new();

    [JsonPropertyName("totals")]
    public System2StaticRoadTileCandidateReviewTotals Totals { get; init; } = new();

    [JsonPropertyName("claim_boundary")]
    public System2ReviewClaimBoundary ClaimBoundary { get; init; } = new();
}

public sealed class System2StaticRoadTileCandidateReviewTotals
{
    [JsonPropertyName("family_count")]
    public int FamilyCount { get; init; }

    [JsonPropertyName("review_item_count")]
    public int ReviewItemCount { get; init; }

    [JsonPropertyName("needs_manual_review_count")]
    public int NeedsManualReviewCount { get; init; }

    [JsonPropertyName("approved_count")]
    public int ApprovedCount { get; init; }

    [JsonPropertyName("rejected_count")]
    public int RejectedCount { get; init; }
}

public sealed class System2ReviewClaimBoundary
{
    [JsonPropertyName("writes_lotpack")]
    public bool WritesLotpack { get; init; } = false;

    [JsonPropertyName("writes_worldgen_lua")]
    public bool WritesWorldgenLua { get; init; } = false;

    [JsonPropertyName("runtime_proven")]
    public bool RuntimeProven { get; init; } = false;

    [JsonPropertyName("public_playable_claim")]
    public bool PublicPlayableClaim { get; init; } = false;
}

public sealed class System2StaticRoadTileCandidateReviewResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; } = new();
    public System2StaticRoadTileCandidateReview? Review { get; set; }
}
