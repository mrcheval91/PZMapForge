using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunDesignResult
{
    [JsonPropertyName("format")] public string Format { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")] public string GeneratedUtc { get; set; } = string.Empty;
    [JsonPropertyName("map_id")] public string MapId { get; set; } = string.Empty;
    [JsonPropertyName("target_component_id")] public string TargetComponentId { get; set; } = string.Empty;
    [JsonPropertyName("source_scope_record_path")] public string SourceScopeRecordPath { get; set; } = string.Empty;
    [JsonPropertyName("source_scope_record_sha256")] public string SourceScopeRecordSha256 { get; set; } = string.Empty;
    [JsonPropertyName("source_scope_record_verdict")] public string SourceScopeRecordVerdict { get; set; } = string.Empty;
    [JsonPropertyName("source_scope_record_is_valid")] public bool SourceScopeRecordIsValid { get; set; }
    [JsonPropertyName("source_manifest_path")] public string SourceManifestPath { get; set; } = string.Empty;
    [JsonPropertyName("source_manifest_sha256")] public string SourceManifestSha256 { get; set; } = string.Empty;
    [JsonPropertyName("source_geometry_mvp_path")] public string SourceGeometryMvpPath { get; set; } = string.Empty;
    [JsonPropertyName("source_geometry_mvp_sha256")] public string SourceGeometryMvpSha256 { get; set; } = string.Empty;
    [JsonPropertyName("future_writer_name")] public string FutureWriterName { get; set; } = string.Empty;
    [JsonPropertyName("future_writer_status")] public string FutureWriterStatus { get; set; } = string.Empty;
    [JsonPropertyName("dry_run_only")] public bool DryRunOnly { get; set; }
    [JsonPropertyName("writer_ready")] public bool WriterReady { get; set; }
    [JsonPropertyName("runtime_valid")] public bool RuntimeValid { get; set; }
    [JsonPropertyName("materialized")] public bool Materialized { get; set; }
    [JsonPropertyName("approved_for_writer_experiment")] public bool ApprovedForWriterExperiment { get; set; }
    [JsonPropertyName("writer_experiment_gate_status")] public string WriterExperimentGateStatus { get; set; } = string.Empty;
    [JsonPropertyName("sandbox_output_root")] public string SandboxOutputRoot { get; set; } = string.Empty;
    [JsonPropertyName("planned_output_records")] public List<DeadMtlWorldBuilderWriterDryRunPlannedOutputRecord> PlannedOutputRecords { get; set; } = new();
    [JsonPropertyName("sandbox_constraints")] public List<DeadMtlWorldBuilderWriterDryRunSandboxConstraint> SandboxConstraints { get; set; } = new();
    [JsonPropertyName("forbidden_output_guards")] public List<DeadMtlWorldBuilderWriterDryRunForbiddenOutputGuard> ForbiddenOutputGuards { get; set; } = new();
    [JsonPropertyName("rollback_checks")] public List<DeadMtlWorldBuilderWriterDryRunRollbackCheck> RollbackChecks { get; set; } = new();
    [JsonPropertyName("design_check_count")] public int DesignCheckCount { get; set; }
    [JsonPropertyName("passed_design_check_count")] public int PassedDesignCheckCount { get; set; }
    [JsonPropertyName("failed_design_check_count")] public int FailedDesignCheckCount { get; set; }
    [JsonPropertyName("checks")] public List<DeadMtlWorldBuilderWriterDryRunDesignCheck> Checks { get; set; } = new();
    [JsonPropertyName("is_valid")] public bool IsValid { get; set; }
    [JsonPropertyName("verdict")] public string Verdict { get; set; } = string.Empty;
    [JsonPropertyName("errors")] public List<string> Errors { get; set; } = new();
}

public sealed class DeadMtlWorldBuilderWriterDryRunPlannedOutputRecord
{
    [JsonPropertyName("record_order")] public int RecordOrder { get; set; }
    [JsonPropertyName("record_id")] public string RecordId { get; set; } = string.Empty;
    [JsonPropertyName("record_kind")] public string RecordKind { get; set; } = string.Empty;
    [JsonPropertyName("source_geometry_kind")] public string SourceGeometryKind { get; set; } = string.Empty;
    [JsonPropertyName("planned_path")] public string PlannedPath { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("details")] public string Details { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderWriterDryRunSandboxConstraint
{
    [JsonPropertyName("constraint_order")] public int ConstraintOrder { get; set; }
    [JsonPropertyName("constraint_id")] public string ConstraintId { get; set; } = string.Empty;
    [JsonPropertyName("constraint_label")] public string ConstraintLabel { get; set; } = string.Empty;
    [JsonPropertyName("details")] public string Details { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderWriterDryRunForbiddenOutputGuard
{
    [JsonPropertyName("guard_order")] public int GuardOrder { get; set; }
    [JsonPropertyName("guard_id")] public string GuardId { get; set; } = string.Empty;
    [JsonPropertyName("guard_label")] public string GuardLabel { get; set; } = string.Empty;
    [JsonPropertyName("blocked_pattern")] public string BlockedPattern { get; set; } = string.Empty;
    [JsonPropertyName("details")] public string Details { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderWriterDryRunRollbackCheck
{
    [JsonPropertyName("check_order")] public int CheckOrder { get; set; }
    [JsonPropertyName("check_id")] public string CheckId { get; set; } = string.Empty;
    [JsonPropertyName("check_label")] public string CheckLabel { get; set; } = string.Empty;
    [JsonPropertyName("details")] public string Details { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderWriterDryRunDesignCheck
{
    [JsonPropertyName("check_order")] public int CheckOrder { get; set; }
    [JsonPropertyName("check_id")] public string CheckId { get; set; } = string.Empty;
    [JsonPropertyName("check_label")] public string CheckLabel { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")] public string Expected { get; set; } = string.Empty;
    [JsonPropertyName("actual")] public string Actual { get; set; } = string.Empty;
    [JsonPropertyName("details")] public string Details { get; set; } = string.Empty;
}
