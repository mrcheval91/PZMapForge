using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadLocalTileSurvey
{
    [JsonPropertyName("format")]
    public string Format { get; init; } = "pzmapforge.deadmtl.system2.static-road-local-tile-survey.v1";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "LOCAL_TILE_SURVEY_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; init; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; init; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("source_survey")]
    public string SourceSurvey { get; init; } = string.Empty;

    [JsonPropertyName("pz_root")]
    public string PzRoot { get; init; } = string.Empty;

    [JsonPropertyName("families")]
    public List<System2StaticRoadLocalTileSurveyFamily> Families { get; init; } = new();

    [JsonPropertyName("totals")]
    public System2StaticRoadLocalTileSurveyTotals Totals { get; init; } = new();

    [JsonPropertyName("claim_boundary")]
    public System2LocalSurveyClaimBoundary ClaimBoundary { get; init; } = new();
}

public sealed class System2StaticRoadLocalTileSurveyTotals
{
    [JsonPropertyName("family_count")]
    public int FamilyCount { get; init; }

    [JsonPropertyName("families_with_candidates")]
    public int FamiliesWithCandidates { get; init; }

    [JsonPropertyName("families_without_candidates")]
    public int FamiliesWithoutCandidates { get; init; }

    [JsonPropertyName("candidate_tile_count")]
    public int CandidateTileCount { get; init; }
}

public sealed class System2LocalSurveyClaimBoundary
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

public sealed class System2StaticRoadLocalTileSurveyResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; } = new();
    public System2StaticRoadLocalTileSurvey? Survey { get; set; }
}
