using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationAcceptanceGateResult
{
    [JsonPropertyName("format")] public string Format { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")] public string GeneratedUtc { get; set; } = string.Empty;
    [JsonPropertyName("map_id")] public string MapId { get; set; } = string.Empty;
    [JsonPropertyName("target_component_id")] public string TargetComponentId { get; set; } = string.Empty;

    [JsonPropertyName("source_qa_review_packet_root")] public string SourceQaReviewPacketRoot { get; set; } = string.Empty;
    [JsonPropertyName("source_qa_review_packet_path")] public string SourceQaReviewPacketPath { get; set; } = string.Empty;
    [JsonPropertyName("source_qa_review_packet_sha256")] public string SourceQaReviewPacketSha256 { get; set; } = string.Empty;
    [JsonPropertyName("source_qa_review_packet_verdict")] public string SourceQaReviewPacketVerdict { get; set; } = string.Empty;
    [JsonPropertyName("source_qa_review_packet_is_valid")] public bool SourceQaReviewPacketIsValid { get; set; }

    [JsonPropertyName("acceptance_stage")] public string AcceptanceStage { get; set; } = "SANDBOX_WRITER_TILE_MATERIALIZATION_ACCEPTANCE_GATE";
    [JsonPropertyName("acceptance_mode")] public string AcceptanceMode { get; set; } = "AUDIT_QA_REVIEW_PACKET_FOR_NEXT_SANDBOX_EXPERIMENT_ONLY";
    [JsonPropertyName("acceptance_gate_status")] public string AcceptanceGateStatus { get; set; } = string.Empty;
    [JsonPropertyName("accepted_for_next_sandbox_experiment")] public bool AcceptedForNextSandboxExperiment { get; set; }
    [JsonPropertyName("accepted_for_runtime_writer")] public bool AcceptedForRuntimeWriter { get; set; }
    [JsonPropertyName("accepted_for_playable_export")] public bool AcceptedForPlayableExport { get; set; }

    [JsonPropertyName("sandbox_only")] public bool SandboxOnly { get; set; } = true;
    [JsonPropertyName("sandbox_materialized_source")] public bool SandboxMaterializedSource { get; set; } = true;
    [JsonPropertyName("visual_qa_overlay_written")] public bool VisualQaOverlayWritten { get; set; }
    [JsonPropertyName("pz_runtime_materialized")] public bool PzRuntimeMaterialized { get; set; }

    [JsonPropertyName("reviewed_file_count")] public int ReviewedFileCount { get; set; }
    [JsonPropertyName("review_check_count")] public int ReviewCheckCount { get; set; }
    [JsonPropertyName("review_passed_check_count")] public int ReviewPassedCheckCount { get; set; }
    [JsonPropertyName("review_failed_check_count")] public int ReviewFailedCheckCount { get; set; }

    [JsonPropertyName("materialized_cell_count")] public int MaterializedCellCount { get; set; }
    [JsonPropertyName("rendered_cell_count")] public int RenderedCellCount { get; set; }
    [JsonPropertyName("count_match_summary")] public string CountMatchSummary { get; set; } = string.Empty;

    [JsonPropertyName("building_wall_candidate_cell_count")] public int BuildingWallCandidateCellCount { get; set; }
    [JsonPropertyName("building_floor_candidate_cell_count")] public int BuildingFloorCandidateCellCount { get; set; }
    [JsonPropertyName("access_edge_cell_count")] public int AccessEdgeCellCount { get; set; }
    [JsonPropertyName("lot_space_cell_count")] public int LotSpaceCellCount { get; set; }
    [JsonPropertyName("component_residual_cell_count")] public int ComponentResidualCellCount { get; set; }
    [JsonPropertyName("material_kind_count")] public int MaterialKindCount { get; set; }
    [JsonPropertyName("layer_kind_count")] public int LayerKindCount { get; set; }

    [JsonPropertyName("overlay_png_width")] public int OverlayPngWidth { get; set; }
    [JsonPropertyName("overlay_png_height")] public int OverlayPngHeight { get; set; }

    [JsonPropertyName("next_allowed_experiment_name")] public string NextAllowedExperimentName { get; set; } = string.Empty;
    [JsonPropertyName("next_allowed_experiment_status")] public string NextAllowedExperimentStatus { get; set; } = string.Empty;
    [JsonPropertyName("next_forbidden_steps")] public List<string> NextForbiddenSteps { get; set; } = new();

    [JsonPropertyName("acceptance_reasons")] public List<string> AcceptanceReasons { get; set; } = new();
    [JsonPropertyName("blocking_reasons")] public List<string> BlockingReasons { get; set; } = new();
    [JsonPropertyName("acceptance_criteria")] public List<DeadMtlTileMaterializationAcceptanceGateCriterion> AcceptanceCriteria { get; set; } = new();

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
    [JsonPropertyName("checks")] public List<DeadMtlTileMaterializationAcceptanceGateCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")] public List<string> Errors { get; set; } = new();
}

public sealed class DeadMtlTileMaterializationAcceptanceGateCriterion
{
    [JsonPropertyName("criterion_order")] public int CriterionOrder { get; set; }
    [JsonPropertyName("criterion_id")] public string CriterionId { get; set; } = string.Empty;
    [JsonPropertyName("criterion_label")] public string CriterionLabel { get; set; } = string.Empty;
    [JsonPropertyName("required_value")] public string RequiredValue { get; set; } = string.Empty;
    [JsonPropertyName("actual_value")] public string ActualValue { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
}

public sealed class DeadMtlTileMaterializationAcceptanceGateCheck
{
    [JsonPropertyName("check_order")] public int CheckOrder { get; set; }
    [JsonPropertyName("check_id")] public string CheckId { get; set; } = string.Empty;
    [JsonPropertyName("check_label")] public string CheckLabel { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")] public string Expected { get; set; } = string.Empty;
    [JsonPropertyName("actual")] public string Actual { get; set; } = string.Empty;
}
