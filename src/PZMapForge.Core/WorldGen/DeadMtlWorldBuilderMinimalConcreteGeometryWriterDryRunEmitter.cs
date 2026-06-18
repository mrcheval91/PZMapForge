using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmitterResult
{
    [JsonPropertyName("format")] public string Format { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")] public string GeneratedUtc { get; set; } = string.Empty;
    [JsonPropertyName("map_id")] public string MapId { get; set; } = string.Empty;
    [JsonPropertyName("target_component_id")] public string TargetComponentId { get; set; } = string.Empty;
    [JsonPropertyName("source_dry_run_design_path")] public string SourceDryRunDesignPath { get; set; } = string.Empty;
    [JsonPropertyName("source_dry_run_design_sha256")] public string SourceDryRunDesignSha256 { get; set; } = string.Empty;
    [JsonPropertyName("source_dry_run_design_verdict")] public string SourceDryRunDesignVerdict { get; set; } = string.Empty;
    [JsonPropertyName("source_geometry_mvp_path")] public string SourceGeometryMvpPath { get; set; } = string.Empty;
    [JsonPropertyName("source_geometry_mvp_sha256")] public string SourceGeometryMvpSha256 { get; set; } = string.Empty;
    [JsonPropertyName("future_writer_name")] public string FutureWriterName { get; set; } = string.Empty;
    [JsonPropertyName("emitter_status")] public string EmitterStatus { get; set; } = string.Empty;
    [JsonPropertyName("dry_run_only")] public bool DryRunOnly { get; set; }
    [JsonPropertyName("writer_ready")] public bool WriterReady { get; set; }
    [JsonPropertyName("runtime_valid")] public bool RuntimeValid { get; set; }
    [JsonPropertyName("materialized")] public bool Materialized { get; set; }
    [JsonPropertyName("approved_for_writer_experiment")] public bool ApprovedForWriterExperiment { get; set; }
    [JsonPropertyName("writer_experiment_gate_status")] public string WriterExperimentGateStatus { get; set; } = string.Empty;
    [JsonPropertyName("output_root")] public string OutputRoot { get; set; } = string.Empty;
    [JsonPropertyName("emitted_record_count")] public int EmittedRecordCount { get; set; }
    [JsonPropertyName("emitted_records")] public List<DeadMtlDryRunEmittedRecordDescriptor> EmittedRecords { get; set; } = new();
    [JsonPropertyName("forbidden_output_scan")] public DeadMtlDryRunForbiddenOutputScan ForbiddenOutputScan { get; set; } = new();
    [JsonPropertyName("rollback_record")] public DeadMtlDryRunRollbackRecord RollbackRecord { get; set; } = new();
    [JsonPropertyName("claim_boundary")] public DeadMtlDryRunClaimBoundary ClaimBoundary { get; set; } = new();
    [JsonPropertyName("check_count")] public int CheckCount { get; set; }
    [JsonPropertyName("passed_check_count")] public int PassedCheckCount { get; set; }
    [JsonPropertyName("failed_check_count")] public int FailedCheckCount { get; set; }
    [JsonPropertyName("checks")] public List<DeadMtlDryRunEmitterCheck> Checks { get; set; } = new();
    [JsonPropertyName("is_valid")] public bool IsValid { get; set; }
    [JsonPropertyName("verdict")] public string Verdict { get; set; } = string.Empty;
    [JsonPropertyName("errors")] public List<string> Errors { get; set; } = new();
}

public sealed class DeadMtlDryRunEmittedRecordDescriptor
{
    [JsonPropertyName("record_order")] public int RecordOrder { get; set; }
    [JsonPropertyName("record_id")] public string RecordId { get; set; } = string.Empty;
    [JsonPropertyName("record_kind")] public string RecordKind { get; set; } = string.Empty;
    [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;
    [JsonPropertyName("sha256")] public string Sha256 { get; set; } = string.Empty;
    [JsonPropertyName("size_bytes")] public long SizeBytes { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
}

public sealed class DeadMtlDryRunForbiddenOutputScan
{
    [JsonPropertyName("lotpack_found")] public bool LotpackFound { get; set; }
    [JsonPropertyName("lotheader_found")] public bool LotheaderFound { get; set; }
    [JsonPropertyName("worldgen_override_lua_found")] public bool WorldgenOverrideLuaFound { get; set; }
    [JsonPropertyName("compile_worldgen_invocation_found")] public bool CompileWorldgenInvocationFound { get; set; }
    [JsonPropertyName("pz_install_path_found")] public bool PzInstallPathFound { get; set; }
    [JsonPropertyName("scan_passed")] public bool ScanPassed { get; set; }
    [JsonPropertyName("details")] public string Details { get; set; } = string.Empty;
}

public sealed class DeadMtlDryRunRollbackRecord
{
    [JsonPropertyName("record_kind")] public string RecordKind { get; set; } = string.Empty;
    [JsonPropertyName("dot_local_outputs_only")] public bool DotLocalOutputsOnly { get; set; }
    [JsonPropertyName("source_hashes_unchanged")] public bool SourceHashesUnchanged { get; set; }
    [JsonPropertyName("runtime_files_created")] public bool RuntimeFilesCreated { get; set; }
    [JsonPropertyName("pz_install_mutated")] public bool PzInstallMutated { get; set; }
    [JsonPropertyName("dry_run_only")] public bool DryRunOnly { get; set; }
}

public sealed class DeadMtlDryRunClaimBoundary
{
    [JsonPropertyName("record_kind")] public string RecordKind { get; set; } = string.Empty;
    [JsonPropertyName("writer_ready")] public bool WriterReady { get; set; }
    [JsonPropertyName("runtime_valid")] public bool RuntimeValid { get; set; }
    [JsonPropertyName("materialized")] public bool Materialized { get; set; }
    [JsonPropertyName("approved_for_writer_experiment")] public bool ApprovedForWriterExperiment { get; set; }
    [JsonPropertyName("writer_experiment_gate_status")] public string WriterExperimentGateStatus { get; set; } = string.Empty;
    [JsonPropertyName("runtime_proof_claimed")] public bool RuntimeProofClaimed { get; set; }
    [JsonPropertyName("writer_ready_claimed")] public bool WriterReadyClaimed { get; set; }
    [JsonPropertyName("public_playable_packaging_claimed")] public bool PublicPlayablePackagingClaimed { get; set; }
}

public sealed class DeadMtlDryRunEmitterCheck
{
    [JsonPropertyName("check_order")] public int CheckOrder { get; set; }
    [JsonPropertyName("check_id")] public string CheckId { get; set; } = string.Empty;
    [JsonPropertyName("check_label")] public string CheckLabel { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")] public string Expected { get; set; } = string.Empty;
    [JsonPropertyName("actual")] public string Actual { get; set; } = string.Empty;
    [JsonPropertyName("details")] public string Details { get; set; } = string.Empty;
}
