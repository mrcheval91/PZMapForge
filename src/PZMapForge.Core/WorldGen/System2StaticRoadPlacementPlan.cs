using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadPlacementPlan
{
    [JsonPropertyName("format")]
    public string Format { get; init; } = "pzmapforge.deadmtl.system2.static-road-placement-plan.v1";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "PLAN_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; init; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; init; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("source_extract")]
    public string SourceExtract { get; init; } = string.Empty;

    [JsonPropertyName("origin_x")]
    public int OriginX { get; init; }

    [JsonPropertyName("origin_y")]
    public int OriginY { get; init; }

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }

    [JsonPropertyName("placements")]
    public List<System2StaticRoadPlacementRecord> Placements { get; init; } = new();

    [JsonPropertyName("totals")]
    public System2StaticRoadPlacementTotals Totals { get; init; } = new();

    [JsonPropertyName("claim_boundary")]
    public System2PlacementClaimBoundary ClaimBoundary { get; init; } = new();
}

public sealed class System2StaticRoadPlacementTotals
{
    [JsonPropertyName("placement_count")]
    public int PlacementCount { get; init; }

    [JsonPropertyName("run_source_count")]
    public int RunSourceCount { get; init; }

    [JsonPropertyName("node_source_count")]
    public int NodeSourceCount { get; init; }

    [JsonPropertyName("duplicate_position_count")]
    public int DuplicatePositionCount { get; init; }

    [JsonPropertyName("by_role")]
    public Dictionary<string, int> ByRole { get; init; } = new();

    [JsonPropertyName("by_intent")]
    public Dictionary<string, int> ByIntent { get; init; } = new();
}

public sealed class System2PlacementClaimBoundary
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

public sealed class System2StaticRoadPlacementPlanResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; } = new();
    public System2StaticRoadPlacementPlan? Plan { get; set; }
}
