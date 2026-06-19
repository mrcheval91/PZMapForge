using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendPlanResult
{
    [JsonPropertyName("format")]              public string Format { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")]       public string GeneratedUtc { get; set; } = string.Empty;
    [JsonPropertyName("map_id")]              public string MapId { get; set; } = string.Empty;

    [JsonPropertyName("source_dry_run_root")]                  public string SourceDryRunRoot { get; set; } = string.Empty;
    [JsonPropertyName("source_dry_run_json_path")]             public string SourceDryRunJsonPath { get; set; } = string.Empty;
    [JsonPropertyName("source_dry_run_sha256")]                public string SourceDryRunSha256 { get; set; } = string.Empty;
    [JsonPropertyName("source_locked_replay_digest")]          public string SourceLockedReplayDigest { get; set; } = string.Empty;
    [JsonPropertyName("source_locked_replay_digest_sha256")]   public string SourceLockedReplayDigestSha256 { get; set; } = string.Empty;
    [JsonPropertyName("source_dry_run_status")]                public string SourceDryRunStatus { get; set; } = string.Empty;
    [JsonPropertyName("source_verdict")]                       public string SourceVerdict { get; set; } = string.Empty;
    [JsonPropertyName("source_is_valid")]                      public bool   SourceIsValid { get; set; }
    [JsonPropertyName("source_check_count")]                   public int    SourceCheckCount { get; set; }
    [JsonPropertyName("source_passed_check_count")]            public int    SourcePassedCheckCount { get; set; }
    [JsonPropertyName("source_failed_check_count")]            public int    SourceFailedCheckCount { get; set; }
    [JsonPropertyName("source_materialized_cell_count")]               public int SourceMaterializedCellCount { get; set; }
    [JsonPropertyName("source_building_wall_candidate_cell_count")]    public int SourceBuildingWallCandidateCellCount { get; set; }
    [JsonPropertyName("source_building_floor_candidate_cell_count")]   public int SourceBuildingFloorCandidateCellCount { get; set; }
    [JsonPropertyName("source_access_edge_cell_count")]                public int SourceAccessEdgeCellCount { get; set; }
    [JsonPropertyName("source_lot_space_cell_count")]                  public int SourceLotSpaceCellCount { get; set; }
    [JsonPropertyName("source_component_residual_cell_count")]         public int SourceComponentResidualCellCount { get; set; }
    [JsonPropertyName("source_material_kind_count")]                   public int SourceMaterialKindCount { get; set; }

    [JsonPropertyName("backend_plan_stage")]  public string BackendPlanStage { get; set; } = string.Empty;
    [JsonPropertyName("backend_plan_mode")]   public string BackendPlanMode { get; set; } = string.Empty;
    [JsonPropertyName("backend_plan_status")] public string BackendPlanStatus { get; set; } = string.Empty;

    [JsonPropertyName("sandbox_only")]                      public bool SandboxOnly { get; set; } = true;
    [JsonPropertyName("sandbox_backend_plan_only")]         public bool SandboxBackendPlanOnly { get; set; } = true;
    [JsonPropertyName("writer_ready")]                      public bool WriterReady { get; set; }
    [JsonPropertyName("runtime_valid")]                     public bool RuntimeValid { get; set; }
    [JsonPropertyName("materialized")]                      public bool Materialized { get; set; }
    [JsonPropertyName("pz_runtime_materialized")]           public bool PzRuntimeMaterialized { get; set; }
    [JsonPropertyName("runtime_proof_claimed")]             public bool RuntimeProofClaimed { get; set; }
    [JsonPropertyName("public_playable_packaging_claimed")] public bool PublicPlayablePackagingClaimed { get; set; }

    [JsonPropertyName("operation_plan_count")]     public int OperationPlanCount { get; set; }
    [JsonPropertyName("operation_plan_groups")]    public DeadMtlLockedReplayBackendPlanOperationGroups OperationPlanGroups { get; set; } = new();
    [JsonPropertyName("backend_operation_records")] public List<DeadMtlLockedReplayBackendOperationRecord> BackendOperationRecords { get; set; } = new();
    [JsonPropertyName("source_manifest_files")]    public List<DeadMtlLockedReplayBackendSourceManifestFile> SourceManifestFiles { get; set; } = new();

    [JsonPropertyName("forbidden_artifact_scan")] public string ForbiddenArtifactScan { get; set; } = string.Empty;
    [JsonPropertyName("claim_boundary_audit")]    public string ClaimBoundaryAudit { get; set; } = string.Empty;
    [JsonPropertyName("next_allowed_experiment_name")]   public string NextAllowedExperimentName { get; set; } = string.Empty;
    [JsonPropertyName("next_allowed_experiment_status")] public string NextAllowedExperimentStatus { get; set; } = string.Empty;
    [JsonPropertyName("next_forbidden_steps")]   public List<string> NextForbiddenSteps { get; set; } = new();

    [JsonPropertyName("check_count")]        public int  CheckCount { get; set; }
    [JsonPropertyName("passed_check_count")] public int  PassedCheckCount { get; set; }
    [JsonPropertyName("failed_check_count")] public int  FailedCheckCount { get; set; }
    [JsonPropertyName("is_valid")]           public bool IsValid { get; set; }
    [JsonPropertyName("verdict")]            public string Verdict { get; set; } = string.Empty;
    [JsonPropertyName("checks")]             public List<DeadMtlLockedReplayBackendPlanCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")]             public List<string> Errors { get; set; } = new();
}

public sealed class DeadMtlLockedReplayBackendPlanOperationGroups
{
    [JsonPropertyName("operation_group_count")]               public int OperationGroupCount { get; set; }
    [JsonPropertyName("total_planned_cell_count")]            public int TotalPlannedCellCount { get; set; }
    [JsonPropertyName("non_empty_operation_group_count")]     public int NonEmptyOperationGroupCount { get; set; }
    [JsonPropertyName("empty_operation_group_count")]         public int EmptyOperationGroupCount { get; set; }
    [JsonPropertyName("largest_operation_group")]             public string LargestOperationGroup { get; set; } = string.Empty;
    [JsonPropertyName("largest_operation_group_cell_count")]  public int LargestOperationGroupCellCount { get; set; }
}

public sealed class DeadMtlLockedReplayBackendOperationRecord
{
    [JsonPropertyName("operation_order")]             public int    OperationOrder { get; set; }
    [JsonPropertyName("operation_id")]                public string OperationId { get; set; } = string.Empty;
    [JsonPropertyName("operation_kind")]              public string OperationKind { get; set; } = string.Empty;
    [JsonPropertyName("source_material_bucket")]      public string SourceMaterialBucket { get; set; } = string.Empty;
    [JsonPropertyName("planned_cell_count")]          public int    PlannedCellCount { get; set; }
    [JsonPropertyName("source_count_field")]          public string SourceCountField { get; set; } = string.Empty;
    [JsonPropertyName("source_count_value")]          public int    SourceCountValue { get; set; }
    [JsonPropertyName("backend_target_family")]       public string BackendTargetFamily { get; set; } = string.Empty;
    [JsonPropertyName("backend_payload_kind")]        public string BackendPayloadKind { get; set; } = string.Empty;
    [JsonPropertyName("requires_locked_replay_digest")] public bool RequiresLockedReplayDigest { get; set; }
    [JsonPropertyName("source_locked_replay_digest")] public string SourceLockedReplayDigest { get; set; } = string.Empty;
    [JsonPropertyName("writer_consumable")]           public bool WriterConsumable { get; set; }
    [JsonPropertyName("runtime_consumable")]          public bool RuntimeConsumable { get; set; }
    [JsonPropertyName("sandbox_only")]                public bool SandboxOnly { get; set; }
    [JsonPropertyName("emits_runtime_file")]          public bool EmitsRuntimeFile { get; set; }
    [JsonPropertyName("emits_binary_file")]           public bool EmitsBinaryFile { get; set; }
    [JsonPropertyName("emits_lua_file")]              public bool EmitsLuaFile { get; set; }
    [JsonPropertyName("emits_install_path")]          public bool EmitsInstallPath { get; set; }
    [JsonPropertyName("notes")]                       public string Notes { get; set; } = string.Empty;
}

public sealed class DeadMtlLockedReplayBackendSourceManifestFile
{
    [JsonPropertyName("file_order")]  public int    FileOrder { get; set; }
    [JsonPropertyName("file_role")]   public string FileRole { get; set; } = string.Empty;
    [JsonPropertyName("file_name")]   public string FileName { get; set; } = string.Empty;
    [JsonPropertyName("file_path")]   public string FilePath { get; set; } = string.Empty;
    [JsonPropertyName("sha256")]      public string Sha256 { get; set; } = string.Empty;
    [JsonPropertyName("exists")]      public bool   Exists { get; set; }
    [JsonPropertyName("required")]    public bool   Required { get; set; }
}

public sealed class DeadMtlLockedReplayBackendPlanCheck
{
    [JsonPropertyName("check_order")]  public int    CheckOrder { get; set; }
    [JsonPropertyName("check_id")]     public string CheckId { get; set; } = string.Empty;
    [JsonPropertyName("check_label")]  public string CheckLabel { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")]     public string Expected { get; set; } = string.Empty;
    [JsonPropertyName("actual")]       public string Actual { get; set; } = string.Empty;
}
