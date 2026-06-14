using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadTileCandidateReviewApplied
{
    [JsonPropertyName("format")]
    public string Format { get; init; } =
        "pzmapforge.deadmtl.system2.static-road-tile-candidate-review-applied.v1";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "HUMAN_REVIEW_APPLIED_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; init; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; init; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("source_review_json")]
    public string SourceReviewJson { get; init; } = string.Empty;

    [JsonPropertyName("source_decisions_csv")]
    public string SourceDecisionsCsv { get; init; } = string.Empty;

    [JsonPropertyName("families")]
    public List<System2StaticRoadTileCandidateReviewAppliedFamily> Families { get; init; } = new();

    [JsonPropertyName("totals")]
    public System2StaticRoadTileCandidateReviewAppliedTotals Totals { get; init; } = new();

    [JsonPropertyName("claim_boundary")]
    public System2ApplyClaimBoundary ClaimBoundary { get; init; } = new();
}

public sealed class System2StaticRoadTileCandidateReviewAppliedTotals
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

    [JsonPropertyName("applied_decision_count")]
    public int AppliedDecisionCount { get; init; }

    [JsonPropertyName("unknown_decision_count")]
    public int UnknownDecisionCount { get; init; }
}

public sealed class System2ApplyClaimBoundary
{
    [JsonPropertyName("writes_lotpack")]
    public bool WritesLotpack { get; init; } = false;

    [JsonPropertyName("writes_worldgen_lua")]
    public bool WritesWorldgenLua { get; init; } = false;

    [JsonPropertyName("runtime_proven")]
    public bool RuntimeProven { get; init; } = false;

    [JsonPropertyName("public_playable_claim")]
    public bool PublicPlayableClaim { get; init; } = false;

    [JsonPropertyName("writer_ready_claim")]
    public bool WriterReadyClaim { get; init; } = false;
}

public sealed class System2StaticRoadTileCandidateReviewAppliedResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public System2StaticRoadTileCandidateReviewApplied? Applied { get; set; }
}
