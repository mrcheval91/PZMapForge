using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadTileCandidateShortlist
{
    [JsonPropertyName("format")]
    public string Format { get; init; } = "pzmapforge.deadmtl.system2.static-road-tile-candidate-shortlist.v1";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "SHORTLIST_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; init; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; init; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("source_local_tile_survey")]
    public string SourceLocalTileSurvey { get; init; } = string.Empty;

    [JsonPropertyName("top_per_family")]
    public int TopPerFamily { get; init; }

    [JsonPropertyName("families")]
    public List<System2StaticRoadTileCandidateShortlistFamily> Families { get; init; } = new();

    [JsonPropertyName("totals")]
    public System2StaticRoadTileCandidateShortlistTotals Totals { get; init; } = new();

    [JsonPropertyName("claim_boundary")]
    public System2ShortlistClaimBoundary ClaimBoundary { get; init; } = new();
}

public sealed class System2StaticRoadTileCandidateShortlistTotals
{
    [JsonPropertyName("family_count")]
    public int FamilyCount { get; init; }

    [JsonPropertyName("input_candidate_count")]
    public int InputCandidateCount { get; init; }

    [JsonPropertyName("shortlisted_candidate_count")]
    public int ShortlistedCandidateCount { get; init; }

    [JsonPropertyName("families_with_shortlist")]
    public int FamiliesWithShortlist { get; init; }

    [JsonPropertyName("families_without_shortlist")]
    public int FamiliesWithoutShortlist { get; init; }
}

public sealed class System2ShortlistClaimBoundary
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

public sealed class System2StaticRoadTileCandidateShortlistResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; } = new();
    public System2StaticRoadTileCandidateShortlist? Shortlist { get; set; }
}
