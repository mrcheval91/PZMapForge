using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationQaReviewPacketResult
{
    [JsonPropertyName("format")] public string Format { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")] public string GeneratedUtc { get; set; } = string.Empty;
    [JsonPropertyName("map_id")] public string MapId { get; set; } = string.Empty;
    [JsonPropertyName("target_component_id")] public string TargetComponentId { get; set; } = string.Empty;

    [JsonPropertyName("source_tile_materializer_root")] public string SourceTileMaterializerRoot { get; set; } = string.Empty;
    [JsonPropertyName("source_tile_materializer_result_path")] public string SourceTileMaterializerResultPath { get; set; } = string.Empty;
    [JsonPropertyName("source_tile_materializer_result_sha256")] public string SourceTileMaterializerResultSha256 { get; set; } = string.Empty;
    [JsonPropertyName("source_tile_materializer_verdict")] public string SourceTileMaterializerVerdict { get; set; } = string.Empty;
    [JsonPropertyName("source_tile_materializer_is_valid")] public bool SourceTileMaterializerIsValid { get; set; }

    [JsonPropertyName("source_qa_overlay_root")] public string SourceQaOverlayRoot { get; set; } = string.Empty;
    [JsonPropertyName("source_qa_overlay_result_path")] public string SourceQaOverlayResultPath { get; set; } = string.Empty;
    [JsonPropertyName("source_qa_overlay_result_sha256")] public string SourceQaOverlayResultSha256 { get; set; } = string.Empty;
    [JsonPropertyName("source_qa_overlay_verdict")] public string SourceQaOverlayVerdict { get; set; } = string.Empty;
    [JsonPropertyName("source_qa_overlay_is_valid")] public bool SourceQaOverlayIsValid { get; set; }

    [JsonPropertyName("review_stage")] public string ReviewStage { get; set; } = "SANDBOX_WRITER_TILE_MATERIALIZATION_QA_REVIEW_PACKET";
    [JsonPropertyName("review_mode")] public string ReviewMode { get; set; } = "AUDIT_SANDBOX_TILE_MATERIALIZATION_CHAIN_ONLY";
    [JsonPropertyName("sandbox_only")] public bool SandboxOnly { get; set; } = true;
    [JsonPropertyName("sandbox_materialized_source")] public bool SandboxMaterializedSource { get; set; } = true;
    [JsonPropertyName("visual_qa_overlay_written")] public bool VisualQaOverlayWritten { get; set; }
    [JsonPropertyName("pz_runtime_materialized")] public bool PzRuntimeMaterialized { get; set; }

    [JsonPropertyName("reviewed_file_count")] public int ReviewedFileCount { get; set; }
    [JsonPropertyName("reviewed_files")] public List<DeadMtlTileMaterializationQaReviewPacketReviewedFile> ReviewedFiles { get; set; } = new();

    [JsonPropertyName("materialization_counts")] public DeadMtlTileMaterializationCounts MaterializationCounts { get; set; } = new();
    [JsonPropertyName("overlay_counts")] public DeadMtlTileOverlayCounts OverlayCounts { get; set; } = new();
    [JsonPropertyName("count_match_summary")] public string CountMatchSummary { get; set; } = string.Empty;

    [JsonPropertyName("overlay_png_path")] public string OverlayPngPath { get; set; } = string.Empty;
    [JsonPropertyName("overlay_png_sha256")] public string OverlayPngSha256 { get; set; } = string.Empty;
    [JsonPropertyName("overlay_png_width")] public int OverlayPngWidth { get; set; }
    [JsonPropertyName("overlay_png_height")] public int OverlayPngHeight { get; set; }

    [JsonPropertyName("canonical_path_audit")] public string CanonicalPathAudit { get; set; } = string.Empty;
    [JsonPropertyName("forbidden_artifact_scan")] public string ForbiddenArtifactScan { get; set; } = string.Empty;
    [JsonPropertyName("claim_boundary_audit")] public string ClaimBoundaryAudit { get; set; } = string.Empty;

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
    [JsonPropertyName("checks")] public List<DeadMtlTileMaterializationQaReviewPacketCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")] public List<string> Errors { get; set; } = new();
}

public sealed class DeadMtlTileMaterializationQaReviewPacketReviewedFile
{
    [JsonPropertyName("file_order")] public int FileOrder { get; set; }
    [JsonPropertyName("source_stage")] public string SourceStage { get; set; } = string.Empty;
    [JsonPropertyName("file_name")] public string FileName { get; set; } = string.Empty;
    [JsonPropertyName("file_path")] public string FilePath { get; set; } = string.Empty;
    [JsonPropertyName("file_kind")] public string FileKind { get; set; } = string.Empty;
    [JsonPropertyName("exists")] public bool Exists { get; set; }
    [JsonPropertyName("sha256")] public string Sha256 { get; set; } = string.Empty;
    [JsonPropertyName("size_bytes")] public long SizeBytes { get; set; }
}

public sealed class DeadMtlTileMaterializationCounts
{
    [JsonPropertyName("materialized_cell_count")] public int MaterializedCellCount { get; set; }
    [JsonPropertyName("building_wall_candidate_cell_count")] public int BuildingWallCandidateCellCount { get; set; }
    [JsonPropertyName("building_floor_candidate_cell_count")] public int BuildingFloorCandidateCellCount { get; set; }
    [JsonPropertyName("access_edge_cell_count")] public int AccessEdgeCellCount { get; set; }
    [JsonPropertyName("lot_space_cell_count")] public int LotSpaceCellCount { get; set; }
    [JsonPropertyName("component_residual_cell_count")] public int ComponentResidualCellCount { get; set; }
    [JsonPropertyName("material_kind_count")] public int MaterialKindCount { get; set; }
    [JsonPropertyName("layer_kind_count")] public int LayerKindCount { get; set; }
}

public sealed class DeadMtlTileOverlayCounts
{
    [JsonPropertyName("rendered_cell_count")] public int RenderedCellCount { get; set; }
    [JsonPropertyName("overlay_width")] public int OverlayWidth { get; set; }
    [JsonPropertyName("overlay_height")] public int OverlayHeight { get; set; }
    [JsonPropertyName("scale")] public int Scale { get; set; }
}

public sealed class DeadMtlTileMaterializationQaReviewPacketCheck
{
    [JsonPropertyName("check_order")] public int CheckOrder { get; set; }
    [JsonPropertyName("check_id")] public string CheckId { get; set; } = string.Empty;
    [JsonPropertyName("check_label")] public string CheckLabel { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")] public string Expected { get; set; } = string.Empty;
    [JsonPropertyName("actual")] public string Actual { get; set; } = string.Empty;
}
