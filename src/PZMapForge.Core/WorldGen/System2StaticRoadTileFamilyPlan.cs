using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadTileFamilyPlan
{
    [JsonPropertyName("format")]
    public string Format { get; init; } = "pzmapforge.deadmtl.system2.static-road-tile-family-plan.v1";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "PLAN_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; init; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; init; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("source_placement_plan")]
    public string SourcePlacementPlan { get; init; } = string.Empty;

    [JsonPropertyName("records")]
    public List<System2StaticRoadTileFamilyRecord> Records { get; init; } = new();

    [JsonPropertyName("totals")]
    public System2StaticRoadTileFamilyTotals Totals { get; init; } = new();

    [JsonPropertyName("claim_boundary")]
    public System2TileFamilyClaimBoundary ClaimBoundary { get; init; } = new();
}

public sealed class System2StaticRoadTileFamilyTotals
{
    [JsonPropertyName("record_count")]
    public int RecordCount { get; init; }

    [JsonPropertyName("by_candidate_family")]
    public Dictionary<string, int> ByCandidateFamily { get; init; } = new();

    [JsonPropertyName("by_role")]
    public Dictionary<string, int> ByRole { get; init; } = new();

    [JsonPropertyName("by_intent")]
    public Dictionary<string, int> ByIntent { get; init; } = new();

    [JsonPropertyName("unmapped_count")]
    public int UnmappedCount { get; init; }
}

public sealed class System2TileFamilyClaimBoundary
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

public sealed class System2StaticRoadTileFamilyPlanResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; } = new();
    public System2StaticRoadTileFamilyPlan? Plan { get; set; }
}
