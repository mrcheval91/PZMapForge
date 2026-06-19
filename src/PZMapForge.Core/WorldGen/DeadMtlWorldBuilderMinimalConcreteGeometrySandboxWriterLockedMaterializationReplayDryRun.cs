using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedMaterializationReplayDryRunResult
{
    [JsonPropertyName("format")] public string Format { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")] public string GeneratedUtc { get; set; } = string.Empty;
    [JsonPropertyName("map_id")] public string MapId { get; set; } = string.Empty;

    [JsonPropertyName("source_audit_root")] public string SourceAuditRoot { get; set; } = string.Empty;
    [JsonPropertyName("source_audit_path")] public string SourceAuditPath { get; set; } = string.Empty;
    [JsonPropertyName("source_audit_sha256")] public string SourceAuditSha256 { get; set; } = string.Empty;
    [JsonPropertyName("source_audit_status")] public string SourceAuditStatus { get; set; } = string.Empty;
    [JsonPropertyName("source_replay_lock_id")] public string SourceReplayLockId { get; set; } = string.Empty;
    [JsonPropertyName("source_recomputed_replay_lock_id")] public string SourceRecomputedReplayLockId { get; set; } = string.Empty;
    [JsonPropertyName("source_replay_lock_id_matches")] public bool SourceReplayLockIdMatches { get; set; }

    [JsonPropertyName("dry_run_stage")] public string DryRunStage { get; set; } = "SANDBOX_WRITER_LOCKED_MATERIALIZATION_REPLAY_DRY_RUN";
    [JsonPropertyName("dry_run_mode")] public string DryRunMode { get; set; } = "REPLAY_LOCKED_MAP27C_MATERIALIZATION_SOURCES_ONLY";
    [JsonPropertyName("dry_run_status")] public string DryRunStatus { get; set; } = string.Empty;

    [JsonPropertyName("sandbox_only")] public bool SandboxOnly { get; set; } = true;
    [JsonPropertyName("sandbox_locked_replay_dry_run")] public bool SandboxLockedReplayDryRun { get; set; } = true;
    [JsonPropertyName("writer_ready")] public bool WriterReady { get; set; }
    [JsonPropertyName("runtime_valid")] public bool RuntimeValid { get; set; }
    [JsonPropertyName("materialized")] public bool Materialized { get; set; }
    [JsonPropertyName("pz_runtime_materialized")] public bool PzRuntimeMaterialized { get; set; }
    [JsonPropertyName("runtime_proof_claimed")] public bool RuntimeProofClaimed { get; set; }
    [JsonPropertyName("public_playable_packaging_claimed")] public bool PublicPlayablePackagingClaimed { get; set; }

    [JsonPropertyName("locked_file_count")] public int LockedFileCount { get; set; }
    [JsonPropertyName("locked_file_hash_match_count")] public int LockedFileHashMatchCount { get; set; }
    [JsonPropertyName("locked_file_hash_mismatch_count")] public int LockedFileHashMismatchCount { get; set; }
    [JsonPropertyName("locked_file_missing_count")] public int LockedFileMissingCount { get; set; }

    // Canonical counts — CSV-derived
    [JsonPropertyName("materialized_cell_count")] public int MaterializedCellCount { get; set; }
    [JsonPropertyName("building_wall_candidate_cell_count")] public int BuildingWallCandidateCellCount { get; set; }
    [JsonPropertyName("building_floor_candidate_cell_count")] public int BuildingFloorCandidateCellCount { get; set; }
    [JsonPropertyName("access_edge_cell_count")] public int AccessEdgeCellCount { get; set; }
    [JsonPropertyName("lot_space_cell_count")] public int LotSpaceCellCount { get; set; }
    [JsonPropertyName("component_residual_cell_count")] public int ComponentResidualCellCount { get; set; }
    [JsonPropertyName("material_kind_count")] public int MaterialKindCount { get; set; }
    [JsonPropertyName("layer_kind_count")] public int LayerKindCount { get; set; }

    // CSV-prefixed counts
    [JsonPropertyName("csv_materialized_cell_count")] public int CsvMaterializedCellCount { get; set; }
    [JsonPropertyName("csv_building_wall_candidate_cell_count")] public int CsvBuildingWallCandidateCellCount { get; set; }
    [JsonPropertyName("csv_building_floor_candidate_cell_count")] public int CsvBuildingFloorCandidateCellCount { get; set; }
    [JsonPropertyName("csv_access_edge_cell_count")] public int CsvAccessEdgeCellCount { get; set; }
    [JsonPropertyName("csv_lot_space_cell_count")] public int CsvLotSpaceCellCount { get; set; }
    [JsonPropertyName("csv_component_residual_cell_count")] public int CsvComponentResidualCellCount { get; set; }
    [JsonPropertyName("csv_material_kind_count")] public int CsvMaterialKindCount { get; set; }

    // Audit-prefixed counts (from MAP-27H stored values)
    [JsonPropertyName("audit_materialized_cell_count")] public int AuditMaterializedCellCount { get; set; }
    [JsonPropertyName("audit_building_wall_candidate_cell_count")] public int AuditBuildingWallCandidateCellCount { get; set; }
    [JsonPropertyName("audit_building_floor_candidate_cell_count")] public int AuditBuildingFloorCandidateCellCount { get; set; }
    [JsonPropertyName("audit_access_edge_cell_count")] public int AuditAccessEdgeCellCount { get; set; }
    [JsonPropertyName("audit_lot_space_cell_count")] public int AuditLotSpaceCellCount { get; set; }
    [JsonPropertyName("audit_component_residual_cell_count")] public int AuditComponentResidualCellCount { get; set; }
    [JsonPropertyName("audit_material_kind_count")] public int AuditMaterialKindCount { get; set; }

    [JsonPropertyName("materialized_cells_csv_sha256")] public string MaterializedCellsCsvSha256 { get; set; } = string.Empty;
    [JsonPropertyName("locked_replay_digest")] public string LockedReplayDigest { get; set; } = string.Empty;

    [JsonPropertyName("forbidden_artifact_scan")] public string ForbiddenArtifactScan { get; set; } = string.Empty;
    [JsonPropertyName("claim_boundary_audit")] public string ClaimBoundaryAudit { get; set; } = string.Empty;
    [JsonPropertyName("next_allowed_experiment_name")] public string NextAllowedExperimentName { get; set; } = string.Empty;
    [JsonPropertyName("next_allowed_experiment_status")] public string NextAllowedExperimentStatus { get; set; } = string.Empty;
    [JsonPropertyName("next_forbidden_steps")] public List<string> NextForbiddenSteps { get; set; } = new();

    [JsonPropertyName("check_count")] public int CheckCount { get; set; }
    [JsonPropertyName("passed_check_count")] public int PassedCheckCount { get; set; }
    [JsonPropertyName("failed_check_count")] public int FailedCheckCount { get; set; }
    [JsonPropertyName("is_valid")] public bool IsValid { get; set; }
    [JsonPropertyName("verdict")] public string Verdict { get; set; } = string.Empty;
    [JsonPropertyName("checks")] public List<DeadMtlLockedReplayDryRunCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")] public List<string> Errors { get; set; } = new();
    [JsonPropertyName("locked_files")] public List<DeadMtlLockedReplayDryRunLockedFile> LockedFiles { get; set; } = new();
}

public sealed class DeadMtlLockedReplayDryRunLockedFile
{
    [JsonPropertyName("file_order")] public int FileOrder { get; set; }
    [JsonPropertyName("file_role")] public string FileRole { get; set; } = string.Empty;
    [JsonPropertyName("file_path")] public string FilePath { get; set; } = string.Empty;
    [JsonPropertyName("stored_sha256")] public string StoredSha256 { get; set; } = string.Empty;
    [JsonPropertyName("rehashed_sha256")] public string RehashedSha256 { get; set; } = string.Empty;
    [JsonPropertyName("hash_still_matches")] public bool HashStillMatches { get; set; }
    [JsonPropertyName("exists")] public bool Exists { get; set; }
}

public sealed class DeadMtlLockedReplayDryRunCheck
{
    [JsonPropertyName("check_order")] public int CheckOrder { get; set; }
    [JsonPropertyName("check_id")] public string CheckId { get; set; } = string.Empty;
    [JsonPropertyName("check_label")] public string CheckLabel { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")] public string Expected { get; set; } = string.Empty;
    [JsonPropertyName("actual")] public string Actual { get; set; } = string.Empty;
}
