using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterLockedReplayBackendDryRunEmitterResult
{
    [JsonPropertyName("format")]          public string Format { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")]   public string GeneratedUtc { get; set; } = string.Empty;
    [JsonPropertyName("map_id")]          public string MapId { get; set; } = string.Empty;

    [JsonPropertyName("source_backend_plan_root")]     public string SourceBackendPlanRoot { get; set; } = string.Empty;
    [JsonPropertyName("source_backend_plan_json_path")] public string SourceBackendPlanJsonPath { get; set; } = string.Empty;
    [JsonPropertyName("source_backend_plan_sha256")]   public string SourceBackendPlanSha256 { get; set; } = string.Empty;
    [JsonPropertyName("source_backend_plan_status")]   public string SourceBackendPlanStatus { get; set; } = string.Empty;
    [JsonPropertyName("source_backend_plan_is_valid")] public bool   SourceBackendPlanIsValid { get; set; }
    [JsonPropertyName("source_backend_plan_check_count")]        public int SourceBackendPlanCheckCount { get; set; }
    [JsonPropertyName("source_backend_plan_passed_check_count")] public int SourceBackendPlanPassedCheckCount { get; set; }
    [JsonPropertyName("source_backend_plan_failed_check_count")] public int SourceBackendPlanFailedCheckCount { get; set; }
    [JsonPropertyName("source_operation_plan_count")]            public int SourceOperationPlanCount { get; set; }
    [JsonPropertyName("source_total_planned_cell_count")]        public int SourceTotalPlannedCellCount { get; set; }
    [JsonPropertyName("source_non_empty_operation_group_count")] public int SourceNonEmptyOperationGroupCount { get; set; }
    [JsonPropertyName("source_empty_operation_group_count")]     public int SourceEmptyOperationGroupCount { get; set; }
    [JsonPropertyName("source_locked_replay_digest")]            public string SourceLockedReplayDigest { get; set; } = string.Empty;
    [JsonPropertyName("source_forbidden_scan")]                  public string SourceForbiddenScan { get; set; } = string.Empty;

    [JsonPropertyName("emitter_stage")]  public string EmitterStage { get; set; } = string.Empty;
    [JsonPropertyName("emitter_mode")]   public string EmitterMode { get; set; } = string.Empty;
    [JsonPropertyName("emitter_status")] public string EmitterStatus { get; set; } = string.Empty;

    [JsonPropertyName("sandbox_only")]                        public bool SandboxOnly { get; set; } = true;
    [JsonPropertyName("sandbox_backend_dry_run_emitter_only")] public bool SandboxBackendDryRunEmitterOnly { get; set; } = true;
    [JsonPropertyName("writer_ready")]                        public bool WriterReady { get; set; }
    [JsonPropertyName("runtime_valid")]                       public bool RuntimeValid { get; set; }
    [JsonPropertyName("materialized")]                        public bool Materialized { get; set; }
    [JsonPropertyName("pz_runtime_materialized")]             public bool PzRuntimeMaterialized { get; set; }
    [JsonPropertyName("runtime_proof_claimed")]               public bool RuntimeProofClaimed { get; set; }
    [JsonPropertyName("public_playable_packaging_claimed")]   public bool PublicPlayablePackagingClaimed { get; set; }

    [JsonPropertyName("emitted_operation_count")]             public int EmittedOperationCount { get; set; }
    [JsonPropertyName("emitted_total_planned_cell_count")]    public int EmittedTotalPlannedCellCount { get; set; }
    [JsonPropertyName("non_empty_emitted_operation_count")]   public int NonEmptyEmittedOperationCount { get; set; }
    [JsonPropertyName("empty_emitted_operation_count")]       public int EmptyEmittedOperationCount { get; set; }
    [JsonPropertyName("largest_emitted_operation_bucket")]    public string LargestEmittedOperationBucket { get; set; } = string.Empty;
    [JsonPropertyName("largest_emitted_operation_cell_count")] public int LargestEmittedOperationCellCount { get; set; }

    [JsonPropertyName("backend_dry_run_emission_digest")]     public string BackendDryRunEmissionDigest { get; set; } = string.Empty;

    [JsonPropertyName("emitted_operation_records")] public List<DeadMtlLockedReplayBackendDryRunEmittedRecord> EmittedOperationRecords { get; set; } = new();
    [JsonPropertyName("source_manifest_files")]     public List<DeadMtlLockedReplayBackendDryRunSourceManifestFile> SourceManifestFiles { get; set; } = new();

    [JsonPropertyName("forbidden_artifact_scan")]   public string ForbiddenArtifactScan { get; set; } = string.Empty;
    [JsonPropertyName("claim_boundary_audit")]      public string ClaimBoundaryAudit { get; set; } = string.Empty;
    [JsonPropertyName("next_allowed_experiment_name")]   public string NextAllowedExperimentName { get; set; } = string.Empty;
    [JsonPropertyName("next_allowed_experiment_status")] public string NextAllowedExperimentStatus { get; set; } = string.Empty;

    [JsonPropertyName("check_count")]        public int  CheckCount { get; set; }
    [JsonPropertyName("passed_check_count")] public int  PassedCheckCount { get; set; }
    [JsonPropertyName("failed_check_count")] public int  FailedCheckCount { get; set; }
    [JsonPropertyName("is_valid")]           public bool IsValid { get; set; }
    [JsonPropertyName("verdict")]            public string Verdict { get; set; } = string.Empty;
    [JsonPropertyName("checks")]             public List<DeadMtlLockedReplayBackendDryRunEmitterCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")]             public List<string> Errors { get; set; } = new();
}

public sealed class DeadMtlLockedReplayBackendDryRunEmittedRecord
{
    [JsonPropertyName("emitted_operation_order")]    public int    EmittedOperationOrder { get; set; }
    [JsonPropertyName("emitted_operation_id")]       public string EmittedOperationId { get; set; } = string.Empty;
    [JsonPropertyName("source_operation_id")]        public string SourceOperationId { get; set; } = string.Empty;
    [JsonPropertyName("source_operation_kind")]      public string SourceOperationKind { get; set; } = string.Empty;
    [JsonPropertyName("source_material_bucket")]     public string SourceMaterialBucket { get; set; } = string.Empty;
    [JsonPropertyName("planned_cell_count")]         public int    PlannedCellCount { get; set; }
    [JsonPropertyName("dry_run_emit_kind")]          public string DryRunEmitKind { get; set; } = string.Empty;
    [JsonPropertyName("dry_run_payload_family")]     public string DryRunPayloadFamily { get; set; } = string.Empty;
    [JsonPropertyName("source_locked_replay_digest")] public string SourceLockedReplayDigest { get; set; } = string.Empty;
    [JsonPropertyName("source_backend_target_family")] public string SourceBackendTargetFamily { get; set; } = string.Empty;
    [JsonPropertyName("source_backend_payload_kind")] public string SourceBackendPayloadKind { get; set; } = string.Empty;
    [JsonPropertyName("requires_locked_replay_digest")] public bool RequiresLockedReplayDigest { get; set; }
    [JsonPropertyName("writer_consumable")]          public bool WriterConsumable { get; set; }
    [JsonPropertyName("runtime_consumable")]         public bool RuntimeConsumable { get; set; }
    [JsonPropertyName("sandbox_only")]               public bool SandboxOnly { get; set; }
    [JsonPropertyName("emits_runtime_file")]         public bool EmitsRuntimeFile { get; set; }
    [JsonPropertyName("emits_binary_file")]          public bool EmitsBinaryFile { get; set; }
    [JsonPropertyName("emits_lua_file")]             public bool EmitsLuaFile { get; set; }
    [JsonPropertyName("emits_install_path")]         public bool EmitsInstallPath { get; set; }
    [JsonPropertyName("emission_status")]            public string EmissionStatus { get; set; } = string.Empty;
    [JsonPropertyName("emission_notes")]             public string EmissionNotes { get; set; } = string.Empty;
}

public sealed class DeadMtlLockedReplayBackendDryRunSourceManifestFile
{
    [JsonPropertyName("file_order")]  public int    FileOrder { get; set; }
    [JsonPropertyName("file_role")]   public string FileRole { get; set; } = string.Empty;
    [JsonPropertyName("file_name")]   public string FileName { get; set; } = string.Empty;
    [JsonPropertyName("file_path")]   public string FilePath { get; set; } = string.Empty;
    [JsonPropertyName("sha256")]      public string Sha256 { get; set; } = string.Empty;
    [JsonPropertyName("exists")]      public bool   Exists { get; set; }
    [JsonPropertyName("required")]    public bool   Required { get; set; }
}

public sealed class DeadMtlLockedReplayBackendDryRunEmitterCheck
{
    [JsonPropertyName("check_order")]  public int    CheckOrder { get; set; }
    [JsonPropertyName("check_id")]     public string CheckId { get; set; } = string.Empty;
    [JsonPropertyName("check_label")]  public string CheckLabel { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")]     public string Expected { get; set; } = string.Empty;
    [JsonPropertyName("actual")]       public string Actual { get; set; } = string.Empty;
}
