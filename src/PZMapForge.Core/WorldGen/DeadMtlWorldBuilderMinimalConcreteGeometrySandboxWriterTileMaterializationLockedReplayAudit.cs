using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializationLockedReplayAuditResult
{
    [JsonPropertyName("format")] public string Format { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")] public string GeneratedUtc { get; set; } = string.Empty;
    [JsonPropertyName("map_id")] public string MapId { get; set; } = string.Empty;

    [JsonPropertyName("audit_stage")] public string AuditStage { get; set; } = "SANDBOX_WRITER_TILE_MATERIALIZATION_LOCKED_REPLAY_AUDIT";
    [JsonPropertyName("audit_mode")] public string AuditMode { get; set; } = "VERIFY_MAP27G1_REPLAY_LOCK_HASHES_AND_LOCK_ID_ONLY";
    [JsonPropertyName("audit_status")] public string AuditStatus { get; set; } = string.Empty;

    [JsonPropertyName("source_replay_lock_root")] public string SourceReplayLockRoot { get; set; } = string.Empty;
    [JsonPropertyName("source_replay_lock_path")] public string SourceReplayLockPath { get; set; } = string.Empty;
    [JsonPropertyName("source_replay_lock_sha256")] public string SourceReplayLockSha256 { get; set; } = string.Empty;
    [JsonPropertyName("source_replay_lock_verdict")] public string SourceReplayLockVerdict { get; set; } = string.Empty;
    [JsonPropertyName("source_replay_lock_is_valid")] public bool SourceReplayLockIsValid { get; set; }
    [JsonPropertyName("source_replay_lock_status")] public string SourceReplayLockStatus { get; set; } = string.Empty;
    [JsonPropertyName("source_replay_lock_id")] public string SourceReplayLockId { get; set; } = string.Empty;
    [JsonPropertyName("source_replay_lock_file_count")] public int SourceReplayLockFileCount { get; set; }

    // Inherited from MAP-27G1 replay lock JSON
    [JsonPropertyName("target_component_id")] public string TargetComponentId { get; set; } = string.Empty;
    [JsonPropertyName("sandbox_materialized_source")] public bool SandboxMaterializedSource { get; set; }
    [JsonPropertyName("visual_qa_overlay_written")] public bool VisualQaOverlayWritten { get; set; }

    [JsonPropertyName("sandbox_only")] public bool SandboxOnly { get; set; } = true;
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

    [JsonPropertyName("recomputed_replay_lock_id")] public string RecomputedReplayLockId { get; set; } = string.Empty;
    [JsonPropertyName("replay_lock_id_matches")] public bool ReplayLockIdMatches { get; set; }

    [JsonPropertyName("locked_file_count")] public int LockedFileCount { get; set; }
    [JsonPropertyName("locked_file_hash_match_count")] public int LockedFileHashMatchCount { get; set; }
    [JsonPropertyName("locked_file_hash_mismatch_count")] public int LockedFileHashMismatchCount { get; set; }
    [JsonPropertyName("locked_file_missing_count")] public int LockedFileMissingCount { get; set; }
    [JsonPropertyName("locked_files")] public List<DeadMtlTileMaterializationLockedReplayAuditFile> LockedFiles { get; set; } = new();

    [JsonPropertyName("writer_ready")] public bool WriterReady { get; set; }
    [JsonPropertyName("runtime_valid")] public bool RuntimeValid { get; set; }
    [JsonPropertyName("materialized")] public bool Materialized { get; set; }
    [JsonPropertyName("runtime_proof_claimed")] public bool RuntimeProofClaimed { get; set; }
    [JsonPropertyName("public_playable_packaging_claimed")] public bool PublicPlayablePackagingClaimed { get; set; }

    [JsonPropertyName("next_allowed_experiment_name")] public string NextAllowedExperimentName { get; set; } = string.Empty;
    [JsonPropertyName("next_allowed_experiment_status")] public string NextAllowedExperimentStatus { get; set; } = string.Empty;

    [JsonPropertyName("next_forbidden_steps")] public List<string> NextForbiddenSteps { get; set; } = new();
    [JsonPropertyName("forbidden_artifact_scan")] public string ForbiddenArtifactScan { get; set; } = string.Empty;
    [JsonPropertyName("claim_boundary_audit")] public string ClaimBoundaryAudit { get; set; } = string.Empty;

    [JsonPropertyName("check_count")] public int CheckCount { get; set; }
    [JsonPropertyName("passed_check_count")] public int PassedCheckCount { get; set; }
    [JsonPropertyName("failed_check_count")] public int FailedCheckCount { get; set; }

    [JsonPropertyName("is_valid")] public bool IsValid { get; set; }
    [JsonPropertyName("verdict")] public string Verdict { get; set; } = string.Empty;
    [JsonPropertyName("checks")] public List<DeadMtlTileMaterializationLockedReplayAuditCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")] public List<string> Errors { get; set; } = new();
}

public sealed class DeadMtlTileMaterializationLockedReplayAuditFile
{
    [JsonPropertyName("file_order")] public int FileOrder { get; set; }
    [JsonPropertyName("source_stage")] public string SourceStage { get; set; } = string.Empty;
    [JsonPropertyName("file_role")] public string FileRole { get; set; } = string.Empty;
    [JsonPropertyName("file_name")] public string FileName { get; set; } = string.Empty;
    [JsonPropertyName("file_path")] public string FilePath { get; set; } = string.Empty;
    [JsonPropertyName("exists")] public bool Exists { get; set; }
    [JsonPropertyName("stored_sha256")] public string StoredSha256 { get; set; } = string.Empty;
    [JsonPropertyName("recomputed_sha256")] public string RecomputedSha256 { get; set; } = string.Empty;
    [JsonPropertyName("hash_matches")] public bool HashMatches { get; set; }
    [JsonPropertyName("locked_for_replay")] public bool LockedForReplay { get; set; }
    [JsonPropertyName("runtime_consumable")] public bool RuntimeConsumable { get; set; }
    [JsonPropertyName("writer_consumable")] public bool WriterConsumable { get; set; }
    [JsonPropertyName("audit_status")] public string AuditStatus { get; set; } = string.Empty;
}

public sealed class DeadMtlTileMaterializationLockedReplayAuditCheck
{
    [JsonPropertyName("check_order")] public int CheckOrder { get; set; }
    [JsonPropertyName("check_id")] public string CheckId { get; set; } = string.Empty;
    [JsonPropertyName("check_label")] public string CheckLabel { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")] public string Expected { get; set; } = string.Empty;
    [JsonPropertyName("actual")] public string Actual { get; set; } = string.Empty;
}
