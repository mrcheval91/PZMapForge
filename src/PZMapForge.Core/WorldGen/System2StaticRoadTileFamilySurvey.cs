using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadTileFamilySurvey
{
    [JsonPropertyName("format")]
    public string Format { get; init; } = "pzmapforge.deadmtl.system2.static-road-tile-family-survey.v1";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "SURVEY_CONTRACT_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; init; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; init; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("source_tile_family_plan")]
    public string SourceTileFamilyPlan { get; init; } = string.Empty;

    [JsonPropertyName("families")]
    public List<System2StaticRoadTileFamilySurveyFamily> Families { get; init; } = new();

    [JsonPropertyName("totals")]
    public System2StaticRoadTileFamilySurveyTotals Totals { get; init; } = new();

    [JsonPropertyName("claim_boundary")]
    public System2SurveyClaimBoundary ClaimBoundary { get; init; } = new();
}

public sealed class System2StaticRoadTileFamilySurveyTotals
{
    [JsonPropertyName("family_count")]
    public int FamilyCount { get; init; }

    [JsonPropertyName("resolved_family_count")]
    public int ResolvedFamilyCount { get; init; }

    [JsonPropertyName("unresolved_family_count")]
    public int UnresolvedFamilyCount { get; init; }

    [JsonPropertyName("candidate_tile_count")]
    public int CandidateTileCount { get; init; }
}

public sealed class System2SurveyClaimBoundary
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

public sealed class System2StaticRoadTileFamilySurveyResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; } = new();
    public System2StaticRoadTileFamilySurvey? Survey { get; set; }
}
