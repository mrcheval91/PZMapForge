using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderGenerationDependencyManifestStep
{
    [JsonPropertyName("step_order")]
    public int StepOrder { get; set; }

    [JsonPropertyName("step_id")]
    public string StepId { get; set; } = string.Empty;

    [JsonPropertyName("map_id")]
    public string MapId { get; set; } = string.Empty;

    [JsonPropertyName("source_stage")]
    public string SourceStage { get; set; } = string.Empty;

    [JsonPropertyName("input_path")]
    public string InputPath { get; set; } = string.Empty;

    [JsonPropertyName("expected_format")]
    public string ExpectedFormat { get; set; } = string.Empty;

    [JsonPropertyName("actual_format")]
    public string ActualFormat { get; set; } = string.Empty;

    [JsonPropertyName("exists")]
    public bool Exists { get; set; }

    [JsonPropertyName("is_required")]
    public bool IsRequired { get; set; } = true;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; set; } = string.Empty;

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; set; } = string.Empty;

    [JsonPropertyName("generation_status")]
    public string GenerationStatus { get; set; } = string.Empty;

    [JsonPropertyName("depends_on")]
    public List<string> DependsOn { get; set; } = new();

    [JsonPropertyName("consumed_by")]
    public List<string> ConsumedBy { get; set; } = new();

    [JsonPropertyName("claim_status")]
    public string ClaimStatus { get; set; } = "CONTRACT_ONLY_NOT_EXECUTED";

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderGenerationDependencyEdge
{
    [JsonPropertyName("from_step_id")]
    public string FromStepId { get; set; } = string.Empty;

    [JsonPropertyName("to_step_id")]
    public string ToStepId { get; set; } = string.Empty;

    [JsonPropertyName("dependency_type")]
    public string DependencyType { get; set; } = "REQUIRED_INPUT";

    [JsonPropertyName("required")]
    public bool Required { get; set; } = true;

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderGenerationDependencyManifestTotals
{
    [JsonPropertyName("step_count")]
    public int StepCount { get; set; }

    [JsonPropertyName("dependency_edge_count")]
    public int DependencyEdgeCount { get; set; }

    [JsonPropertyName("required_input_count")]
    public int RequiredInputCount { get; set; }

    [JsonPropertyName("existing_input_count")]
    public int ExistingInputCount { get; set; }

    [JsonPropertyName("missing_input_count")]
    public int MissingInputCount { get; set; }

    [JsonPropertyName("format_match_count")]
    public int FormatMatchCount { get; set; }

    [JsonPropertyName("format_mismatch_count")]
    public int FormatMismatchCount { get; set; }

    [JsonPropertyName("contract_only_step_count")]
    public int ContractOnlyStepCount { get; set; }

    [JsonPropertyName("runtime_proven_step_count")]
    public int RuntimeProvenStepCount { get; set; }

    [JsonPropertyName("writer_ready_step_count")]
    public int WriterReadyStepCount { get; set; }

    [JsonPropertyName("generation_executed_step_count")]
    public int GenerationExecutedStepCount { get; set; }
}

public sealed class DeadMtlWorldBuilderGenerationDependencyManifestClaimBoundary
{
    [JsonPropertyName("writes_lotpack")]
    public bool WritesLotpack { get; set; }

    [JsonPropertyName("writes_worldgen_lua")]
    public bool WritesWorldgenLua { get; set; }

    [JsonPropertyName("runtime_proven")]
    public bool RuntimeProven { get; set; }

    [JsonPropertyName("public_playable_claim")]
    public bool PublicPlayableClaim { get; set; }

    [JsonPropertyName("writer_ready_claim")]
    public bool WriterReadyClaim { get; set; }

    [JsonPropertyName("generates_terrain_now")]
    public bool GeneratesTerrainNow { get; set; }

    [JsonPropertyName("generates_buildings_now")]
    public bool GeneratesBuildingsNow { get; set; }

    [JsonPropertyName("generates_sidewalks_now")]
    public bool GeneratesSidewalksNow { get; set; }

    [JsonPropertyName("subdivides_lots_now")]
    public bool SubdividesLotsNow { get; set; }

    [JsonPropertyName("captures_chunk_layers_now")]
    public bool CapturesChunkLayersNow { get; set; }

    [JsonPropertyName("places_fences_now")]
    public bool PlacesFencesNow { get; set; }

    [JsonPropertyName("places_unique_buildings_now")]
    public bool PlacesUniqueBuildingsNow { get; set; }

    [JsonPropertyName("selects_concrete_building_ids_now")]
    public bool SelectsConcreteBuildingIdsNow { get; set; }
}

public sealed class DeadMtlWorldBuilderGenerationDependencyManifest
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = "pzmapforge.deadmtl.worldbuilder.generation-dependency-manifest.v1";

    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "GENERATION_DEPENDENCY_MANIFEST_CONTRACT_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; set; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; set; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("generation_status")]
    public string GenerationStatus { get; set; } = "NOT_EXECUTED";

    [JsonPropertyName("pipeline_status")]
    public string PipelineStatus { get; set; } = "ORDERED_MANIFEST_ONLY";

    [JsonPropertyName("steps")]
    public List<DeadMtlWorldBuilderGenerationDependencyManifestStep> Steps { get; set; } = new();

    [JsonPropertyName("dependency_edges")]
    public List<DeadMtlWorldBuilderGenerationDependencyEdge> DependencyEdges { get; set; } = new();

    [JsonPropertyName("totals")]
    public DeadMtlWorldBuilderGenerationDependencyManifestTotals Totals { get; set; } = new();

    [JsonPropertyName("claim_boundary")]
    public DeadMtlWorldBuilderGenerationDependencyManifestClaimBoundary ClaimBoundary { get; set; } = new();
}

public sealed class DeadMtlWorldBuilderGenerationDependencyManifestResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public DeadMtlWorldBuilderGenerationDependencyManifest Manifest { get; set; } = new();
}
