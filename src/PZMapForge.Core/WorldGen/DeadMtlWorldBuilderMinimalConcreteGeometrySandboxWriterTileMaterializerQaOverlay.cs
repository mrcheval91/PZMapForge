using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerQaOverlayResult
{
    [JsonPropertyName("format")] public string Format { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")] public string GeneratedUtc { get; set; } = string.Empty;
    [JsonPropertyName("map_id")] public string MapId { get; set; } = string.Empty;
    [JsonPropertyName("target_component_id")] public string TargetComponentId { get; set; } = string.Empty;
    [JsonPropertyName("source_tile_materializer_result_path")] public string SourceTileMaterializerResultPath { get; set; } = string.Empty;
    [JsonPropertyName("source_tile_materializer_result_sha256")] public string SourceTileMaterializerResultSha256 { get; set; } = string.Empty;
    [JsonPropertyName("source_tile_materializer_verdict")] public string SourceTileMaterializerVerdict { get; set; } = string.Empty;
    [JsonPropertyName("source_tile_materializer_is_valid")] public bool SourceTileMaterializerIsValid { get; set; }
    [JsonPropertyName("writer_stage")] public string WriterStage { get; set; } = "SANDBOX_WRITER_TILE_MATERIALIZER_QA_OVERLAY_V0";
    [JsonPropertyName("writer_mode")] public string WriterMode { get; set; } = "RENDER_SANDBOX_TILE_MATERIALIZATION_QA_OVERLAY_ONLY";
    [JsonPropertyName("sandbox_only")] public bool SandboxOnly { get; set; } = true;
    [JsonPropertyName("sandbox_materialized_source")] public bool SandboxMaterializedSource { get; set; } = true;
    [JsonPropertyName("visual_qa_overlay_written")] public bool VisualQaOverlayWritten { get; set; }
    [JsonPropertyName("pz_runtime_materialized")] public bool PzRuntimeMaterialized { get; set; }
    [JsonPropertyName("source_width")] public int SourceWidth { get; set; } = 256;
    [JsonPropertyName("source_height")] public int SourceHeight { get; set; } = 256;
    [JsonPropertyName("scale")] public int Scale { get; set; } = 4;
    [JsonPropertyName("overlay_width")] public int OverlayWidth { get; set; } = 1024;
    [JsonPropertyName("overlay_height")] public int OverlayHeight { get; set; } = 1024;
    [JsonPropertyName("input_materialized_cell_count")] public int InputMaterializedCellCount { get; set; }
    [JsonPropertyName("rendered_cell_count")] public int RenderedCellCount { get; set; }
    [JsonPropertyName("building_wall_candidate_cell_count")] public int BuildingWallCandidateCellCount { get; set; }
    [JsonPropertyName("building_floor_candidate_cell_count")] public int BuildingFloorCandidateCellCount { get; set; }
    [JsonPropertyName("access_edge_cell_count")] public int AccessEdgeCellCount { get; set; }
    [JsonPropertyName("lot_space_cell_count")] public int LotSpaceCellCount { get; set; }
    [JsonPropertyName("component_residual_cell_count")] public int ComponentResidualCellCount { get; set; }
    [JsonPropertyName("material_kind_count")] public int MaterialKindCount { get; set; }
    [JsonPropertyName("layer_kind_count")] public int LayerKindCount { get; set; }
    [JsonPropertyName("output_file_count")] public int OutputFileCount { get; set; }
    [JsonPropertyName("output_files")] public List<DeadMtlTileMaterializerQaOverlayOutputFile> OutputFiles { get; set; } = new();
    [JsonPropertyName("checks")] public List<DeadMtlTileMaterializerQaOverlayCheck> Checks { get; set; } = new();
    [JsonPropertyName("check_count")] public int CheckCount { get; set; }
    [JsonPropertyName("passed_check_count")] public int PassedCheckCount { get; set; }
    [JsonPropertyName("failed_check_count")] public int FailedCheckCount { get; set; }
    [JsonPropertyName("writer_ready")] public bool WriterReady { get; set; }
    [JsonPropertyName("runtime_valid")] public bool RuntimeValid { get; set; }
    [JsonPropertyName("materialized")] public bool Materialized { get; set; }
    [JsonPropertyName("runtime_proof_claimed")] public bool RuntimeProofClaimed { get; set; }
    [JsonPropertyName("public_playable_packaging_claimed")] public bool PublicPlayablePackagingClaimed { get; set; }
    [JsonPropertyName("is_valid")] public bool IsValid { get; set; }
    [JsonPropertyName("verdict")] public string Verdict { get; set; } = string.Empty;
    [JsonPropertyName("errors")] public List<string> Errors { get; set; } = new();
}

public sealed class DeadMtlTileMaterializerQaOverlayOutputFile
{
    [JsonPropertyName("file_order")] public int FileOrder { get; set; }
    [JsonPropertyName("file_name")] public string FileName { get; set; } = string.Empty;
    [JsonPropertyName("file_path")] public string FilePath { get; set; } = string.Empty;
    [JsonPropertyName("file_kind")] public string FileKind { get; set; } = string.Empty;
    [JsonPropertyName("sha256")] public string Sha256 { get; set; } = string.Empty;
    [JsonPropertyName("written")] public bool Written { get; set; }
}

public sealed class DeadMtlTileMaterializerQaOverlayCheck
{
    [JsonPropertyName("check_order")] public int CheckOrder { get; set; }
    [JsonPropertyName("check_id")] public string CheckId { get; set; } = string.Empty;
    [JsonPropertyName("check_label")] public string CheckLabel { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")] public string Expected { get; set; } = string.Empty;
    [JsonPropertyName("actual")] public string Actual { get; set; } = string.Empty;
}
