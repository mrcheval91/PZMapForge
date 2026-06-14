using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadHumanApprovedTileCandidateManifest
{
    [JsonPropertyName("format")]
    public string Format { get; init; } =
        "pzmapforge.deadmtl.system2.static-road-human-approved-tile-candidates.v1";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "HUMAN_APPROVED_CANDIDATE_MANIFEST_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; init; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; init; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("source_applied_review_json")]
    public string SourceAppliedReviewJson { get; init; } = string.Empty;

    [JsonPropertyName("approved_candidates")]
    public List<System2StaticRoadHumanApprovedTileCandidate> ApprovedCandidates { get; init; } = new();

    [JsonPropertyName("rejected_candidates")]
    public List<System2StaticRoadHumanApprovedTileCandidate> RejectedCandidates { get; init; } = new();

    [JsonPropertyName("pending_candidates")]
    public List<System2StaticRoadHumanApprovedTileCandidate> PendingCandidates { get; init; } = new();

    [JsonPropertyName("totals")]
    public System2StaticRoadHumanApprovedTileCandidateTotals Totals { get; init; } = new();

    [JsonPropertyName("claim_boundary")]
    public System2HumanApprovedClaimBoundary ClaimBoundary { get; init; } = new();
}

public sealed class System2StaticRoadHumanApprovedTileCandidateTotals
{
    [JsonPropertyName("family_count")]
    public int FamilyCount { get; init; }

    [JsonPropertyName("review_item_count")]
    public int ReviewItemCount { get; init; }

    [JsonPropertyName("approved_count")]
    public int ApprovedCount { get; init; }

    [JsonPropertyName("rejected_count")]
    public int RejectedCount { get; init; }

    [JsonPropertyName("pending_count")]
    public int PendingCount { get; init; }
}

public sealed class System2HumanApprovedClaimBoundary
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

public sealed class System2StaticRoadHumanApprovedTileCandidateManifestResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; } = new();
    public System2StaticRoadHumanApprovedTileCandidateManifest? Manifest { get; set; }
}
