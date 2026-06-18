using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationReplayLockResult
{
    [JsonPropertyName("format")] public string Format { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")] public string GeneratedUtc { get; set; } = string.Empty;
    [JsonPropertyName("map_id")] public string MapId { get; set; } = string.Empty;
    [JsonPropertyName("target_component_id")] public string TargetComponentId { get; set; } = string.Empty;

    [JsonPropertyName("source_acceptance_gate_root")] public string SourceAcceptanceGateRoot { get; set; } = string.Empty;
    [JsonPropertyName("source_acceptance_gate_path")] public string SourceAcceptanceGatePath { get; set; } = string.Empty;
    [JsonPropertyName("source_acceptance_gate_sha256")] public string SourceAcceptanceGateSha256 { get; set; } = string.Empty;
    [JsonPropertyName("source_acceptance_gate_verdict")] public string SourceAcceptanceGateVerdict { get; set; } = string.Empty;
    [JsonPropertyName("source_acceptance_gate_is_valid")] public bool SourceAcceptanceGateIsValid { get; set; }
    [JsonPropertyName("source_acceptance_gate_status")] public string SourceAcceptanceGateStatus { get; set; } = string.Empty;
    [JsonPropertyName("source_accepted_for_next_sandbox_experiment")] public bool SourceAcceptedForNextSandboxExperiment { get; set; }
    [JsonPropertyName("source_accepted_for_runtime_writer")] public bool SourceAcceptedForRuntimeWriter { get; set; }
    [JsonPropertyName("source_accepted_for_playable_export")] public bool SourceAcceptedForPlayableExport { get; set; }

    [JsonPropertyName("source_tile_materializer_root")] public string SourceTileMaterializerRoot { get; set; } = string.Empty;
    [JsonPropertyName("source_tile_materializer_result_path")] public string SourceTileMaterializerResultPath { get; set; } = string.Empty;
    [JsonPropertyName("source_tile_materializer_result_sha256")] public string SourceTileMaterializerResultSha256 { get; set; } = string.Empty;

    [JsonPropertyName("replay_lock_stage")] public string ReplayLockStage { get; set; } = "SANDBOX_WRITER_TILE_MATERIALIZATION_REPLAY_LOCK";
    [JsonPropertyName("replay_lock_mode")] public string ReplayLockMode { get; set; } = "LOCK_ACCEPTED_SANDBOX_MATERIALIZATION_INPUTS_ONLY";
    [JsonPropertyName("replay_lock_status")] public string ReplayLockStatus { get; set; } = string.Empty;
    [JsonPropertyName("replay_lock_id")] public string ReplayLockId { get; set; } = string.Empty;
    [JsonPropertyName("replay_lock_file_count")] public int ReplayLockFileCount { get; set; }
    [JsonPropertyName("replay_lock_files")] public List<DeadMtlTileMaterializationReplayLockFile> ReplayLockFiles { get; set; } = new();

    [JsonPropertyName("sandbox_only")] public bool SandboxOnly { get; set; } = true;
    [JsonPropertyName("sandbox_materialized_source")] public bool SandboxMaterializedSource { get; set; } = true;
    [JsonPropertyName("visual_qa_overlay_written")] public bool VisualQaOverlayWritten { get; set; }
    [JsonPropertyName("pz_runtime_materialized")] public bool PzRuntimeMaterialized { get; set; }

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

    [JsonPropertyName("accepted_for_next_sandbox_experiment")] public bool AcceptedForNextSandboxExperiment { get; set; }
    [JsonPropertyName("accepted_for_runtime_writer")] public bool AcceptedForRuntimeWriter { get; set; }
    [JsonPropertyName("accepted_for_playable_export")] public bool AcceptedForPlayableExport { get; set; }

    [JsonPropertyName("next_allowed_experiment_name")] public string NextAllowedExperimentName { get; set; } = string.Empty;
    [JsonPropertyName("next_allowed_experiment_status")] public string NextAllowedExperimentStatus { get; set; } = string.Empty;
    [JsonPropertyName("next_forbidden_steps")] public List<string> NextForbiddenSteps { get; set; } = new();

    [JsonPropertyName("replay_requirements")] public List<string> ReplayRequirements { get; set; } = new();
    [JsonPropertyName("replay_lock_reasons")] public List<string> ReplayLockReasons { get; set; } = new();
    [JsonPropertyName("blocking_reasons")] public List<string> BlockingReasons { get; set; } = new();

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
    [JsonPropertyName("checks")] public List<DeadMtlTileMaterializationReplayLockCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")] public List<string> Errors { get; set; } = new();
}

public sealed class DeadMtlTileMaterializationReplayLockFile
{
    [JsonPropertyName("file_order")] public int FileOrder { get; set; }
    [JsonPropertyName("source_stage")] public string SourceStage { get; set; } = string.Empty;
    [JsonPropertyName("file_role")] public string FileRole { get; set; } = string.Empty;
    [JsonPropertyName("file_name")] public string FileName { get; set; } = string.Empty;
    [JsonPropertyName("file_path")] public string FilePath { get; set; } = string.Empty;
    [JsonPropertyName("exists")] public bool Exists { get; set; }
    [JsonPropertyName("sha256")] public string Sha256 { get; set; } = string.Empty;
    [JsonPropertyName("size_bytes")] public long SizeBytes { get; set; }
    [JsonPropertyName("locked_for_replay")] public bool LockedForReplay { get; set; }
    [JsonPropertyName("runtime_consumable")] public bool RuntimeConsumable { get; set; }
    [JsonPropertyName("writer_consumable")] public bool WriterConsumable { get; set; }
}

public sealed class DeadMtlTileMaterializationReplayLockCheck
{
    [JsonPropertyName("check_order")] public int CheckOrder { get; set; }
    [JsonPropertyName("check_id")] public string CheckId { get; set; } = string.Empty;
    [JsonPropertyName("check_label")] public string CheckLabel { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")] public string Expected { get; set; } = string.Empty;
    [JsonPropertyName("actual")] public string Actual { get; set; } = string.Empty;
}
