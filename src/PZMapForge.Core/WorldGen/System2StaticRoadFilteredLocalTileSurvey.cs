using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadFilteredLocalTileSurvey
{
    [JsonPropertyName("format")]
    public string Format { get; init; } =
        "pzmapforge.deadmtl.system2.static-road-local-tile-survey-filtered.v1";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "LOCAL_TILE_SURVEY_FILTERED_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; init; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; init; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("source_survey")]
    public string SourceSurvey { get; init; } = string.Empty;

    [JsonPropertyName("pz_root")]
    public string PzRoot { get; init; } = string.Empty;

    [JsonPropertyName("source_filter")]
    public System2FilteredSurveySourceFilter SourceFilter { get; init; } = new();

    [JsonPropertyName("families")]
    public List<System2StaticRoadFilteredLocalTileSurveyFamily> Families { get; init; } = new();

    [JsonPropertyName("totals")]
    public System2StaticRoadFilteredLocalTileSurveyTotals Totals { get; init; } = new();

    [JsonPropertyName("claim_boundary")]
    public System2FilteredSurveyClaimBoundary ClaimBoundary { get; init; } = new();
}

public sealed class System2FilteredSurveySourceFilter
{
    [JsonPropertyName("allowed_extensions")]
    public List<string> AllowedExtensions { get; init; } = new();

    [JsonPropertyName("excluded_path_fragments")]
    public List<string> ExcludedPathFragments { get; init; } = new();

    [JsonPropertyName("preferred_path_fragments")]
    public List<string> PreferredPathFragments { get; init; } = new();
}

public sealed class System2StaticRoadFilteredLocalTileSurveyTotals
{
    [JsonPropertyName("family_count")]
    public int FamilyCount { get; init; }

    [JsonPropertyName("candidate_tile_count")]
    public int CandidateTileCount { get; init; }

    [JsonPropertyName("excluded_source_file_count")]
    public int ExcludedSourceFileCount { get; init; }

    [JsonPropertyName("scanned_source_file_count")]
    public int ScannedSourceFileCount { get; init; }

    [JsonPropertyName("families_with_candidates")]
    public int FamiliesWithCandidates { get; init; }

    [JsonPropertyName("families_without_candidates")]
    public int FamiliesWithoutCandidates { get; init; }
}

public sealed class System2FilteredSurveyClaimBoundary
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

public sealed class System2StaticRoadFilteredLocalTileSurveyResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; } = new();
    public System2StaticRoadFilteredLocalTileSurvey? Survey { get; set; }
}
