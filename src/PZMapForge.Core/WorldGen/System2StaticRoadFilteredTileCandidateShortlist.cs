using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadFilteredTileCandidateShortlist
{
    [JsonPropertyName("format")]
    public string Format { get; init; } =
        "pzmapforge.deadmtl.system2.static-road-filtered-tile-candidate-shortlist.v1";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "FILTERED_SHORTLIST_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; init; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; init; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("source_filtered_survey")]
    public string SourceFilteredSurvey { get; init; } = string.Empty;

    [JsonPropertyName("top_per_family")]
    public int TopPerFamily { get; init; }

    [JsonPropertyName("families")]
    public List<System2StaticRoadFilteredTileCandidateShortlistFamily> Families { get; init; } = new();

    [JsonPropertyName("totals")]
    public System2StaticRoadFilteredTileCandidateShortlistTotals Totals { get; init; } = new();

    [JsonPropertyName("claim_boundary")]
    public System2FilteredShortlistClaimBoundary ClaimBoundary { get; init; } = new();
}

public sealed class System2StaticRoadFilteredTileCandidateShortlistTotals
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

public sealed class System2FilteredShortlistClaimBoundary
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

public sealed class System2StaticRoadFilteredTileCandidateShortlistResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; } = new();
    public System2StaticRoadFilteredTileCandidateShortlist? Shortlist { get; set; }
}
