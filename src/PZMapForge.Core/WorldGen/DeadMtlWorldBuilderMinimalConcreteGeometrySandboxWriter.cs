using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterResult
{
    [JsonPropertyName("format")] public string Format { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")] public string GeneratedUtc { get; set; } = string.Empty;
    [JsonPropertyName("map_id")] public string MapId { get; set; } = string.Empty;
    [JsonPropertyName("target_component_id")] public string TargetComponentId { get; set; } = string.Empty;
    [JsonPropertyName("source_adapter_contract_path")] public string SourceAdapterContractPath { get; set; } = string.Empty;
    [JsonPropertyName("source_adapter_contract_sha256")] public string SourceAdapterContractSha256 { get; set; } = string.Empty;
    [JsonPropertyName("source_adapter_contract_verdict")] public string SourceAdapterContractVerdict { get; set; } = string.Empty;
    [JsonPropertyName("source_adapter_contract_is_valid")] public bool SourceAdapterContractIsValid { get; set; }
    [JsonPropertyName("source_adapter_contract_status")] public string SourceAdapterContractStatus { get; set; } = string.Empty;
    [JsonPropertyName("writer_stage")] public string WriterStage { get; set; } = "SANDBOX_WRITER_V0";
    [JsonPropertyName("writer_mode")] public string WriterMode { get; set; } = "WRITE_SANDBOX_OPERATION_ARTIFACTS_ONLY";
    [JsonPropertyName("sandbox_only")] public bool SandboxOnly { get; set; } = true;
    [JsonPropertyName("operation_file_count")] public int OperationFileCount { get; set; }
    [JsonPropertyName("operation_count")] public int OperationCount { get; set; }
    [JsonPropertyName("component_operation_count")] public int ComponentOperationCount { get; set; }
    [JsonPropertyName("lot_operation_count")] public int LotOperationCount { get; set; }
    [JsonPropertyName("building_slot_operation_count")] public int BuildingSlotOperationCount { get; set; }
    [JsonPropertyName("access_operation_count")] public int AccessOperationCount { get; set; }
    [JsonPropertyName("forbidden_output_guard_count")] public int ForbiddenOutputGuardCount { get; set; }
    [JsonPropertyName("operation_files")] public List<DeadMtlSandboxWriterOperationFile> OperationFiles { get; set; } = new();
    [JsonPropertyName("forbidden_output_guard")] public DeadMtlSandboxWriterForbiddenOutputGuardResult ForbiddenOutputGuard { get; set; } = new();
    [JsonPropertyName("checks")] public List<DeadMtlSandboxWriterCheck> Checks { get; set; } = new();
    [JsonPropertyName("check_count")] public int CheckCount { get; set; }
    [JsonPropertyName("passed_check_count")] public int PassedCheckCount { get; set; }
    [JsonPropertyName("failed_check_count")] public int FailedCheckCount { get; set; }
    [JsonPropertyName("writer_ready")] public bool WriterReady { get; set; }
    [JsonPropertyName("runtime_valid")] public bool RuntimeValid { get; set; }
    [JsonPropertyName("materialized")] public bool Materialized { get; set; }
    [JsonPropertyName("approved_for_writer_experiment")] public bool ApprovedForWriterExperiment { get; set; }
    [JsonPropertyName("writer_experiment_gate_status")] public string WriterExperimentGateStatus { get; set; } = string.Empty;
    [JsonPropertyName("runtime_proof_claimed")] public bool RuntimeProofClaimed { get; set; }
    [JsonPropertyName("public_playable_packaging_claimed")] public bool PublicPlayablePackagingClaimed { get; set; }
    [JsonPropertyName("is_valid")] public bool IsValid { get; set; }
    [JsonPropertyName("verdict")] public string Verdict { get; set; } = string.Empty;
    [JsonPropertyName("errors")] public List<string> Errors { get; set; } = new();
}

public sealed class DeadMtlSandboxWriterOperation
{
    [JsonPropertyName("operation_order")] public int OperationOrder { get; set; }
    [JsonPropertyName("operation_kind")] public string OperationKind { get; set; } = string.Empty;
    [JsonPropertyName("operation_group")] public string OperationGroup { get; set; } = string.Empty;
    [JsonPropertyName("source_record_id")] public string SourceRecordId { get; set; } = string.Empty;
    [JsonPropertyName("target_component_id")] public string TargetComponentId { get; set; } = string.Empty;
    [JsonPropertyName("lot_id")] public string LotId { get; set; } = string.Empty;
    [JsonPropertyName("lot_order")] public int LotOrder { get; set; }
    [JsonPropertyName("slot_id")] public string SlotId { get; set; } = string.Empty;
    [JsonPropertyName("slot_order")] public int SlotOrder { get; set; }
    [JsonPropertyName("access_id")] public string AccessId { get; set; } = string.Empty;
    [JsonPropertyName("access_kind")] public string AccessKind { get; set; } = string.Empty;
    [JsonPropertyName("side")] public string Side { get; set; } = string.Empty;
    [JsonPropertyName("min_x")] public int MinX { get; set; }
    [JsonPropertyName("min_y")] public int MinY { get; set; }
    [JsonPropertyName("max_x")] public int MaxX { get; set; }
    [JsonPropertyName("max_y")] public int MaxY { get; set; }
    [JsonPropertyName("width_px")] public int WidthPx { get; set; }
    [JsonPropertyName("height_px")] public int HeightPx { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "SANDBOX_OPERATION_WRITTEN";
    [JsonPropertyName("runtime_effect")] public string RuntimeEffect { get; set; } = "NONE";
}

public sealed class DeadMtlSandboxWriterOperationFile
{
    [JsonPropertyName("file_order")] public int FileOrder { get; set; }
    [JsonPropertyName("file_name")] public string FileName { get; set; } = string.Empty;
    [JsonPropertyName("file_path")] public string FilePath { get; set; } = string.Empty;
    [JsonPropertyName("file_kind")] public string FileKind { get; set; } = string.Empty;
    [JsonPropertyName("sha256")] public string Sha256 { get; set; } = string.Empty;
    [JsonPropertyName("operation_count")] public int OperationCount { get; set; }
    [JsonPropertyName("written")] public bool Written { get; set; }
}

public sealed class DeadMtlSandboxWriterForbiddenOutputGuardResult
{
    [JsonPropertyName("no_forbidden_artifacts_emitted")] public bool NoForbiddenArtifactsEmitted { get; set; } = true;
    [JsonPropertyName("guard_statement")] public string GuardStatement { get; set; } = "MAP-27A did not emit any forbidden output artifacts.";
    [JsonPropertyName("guards")] public List<DeadMtlSandboxWriterForbiddenOutputGuard> Guards { get; set; } = new();
}

public sealed class DeadMtlSandboxWriterForbiddenOutputGuard
{
    [JsonPropertyName("guard_order")] public int GuardOrder { get; set; }
    [JsonPropertyName("family_id")] public string FamilyId { get; set; } = string.Empty;
    [JsonPropertyName("blocked_pattern")] public string BlockedPattern { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = "FORBIDDEN";
    [JsonPropertyName("verified")] public string Verified { get; set; } = "NOT_EMITTED";
}

public sealed class DeadMtlSandboxWriterCheck
{
    [JsonPropertyName("check_order")] public int CheckOrder { get; set; }
    [JsonPropertyName("check_id")] public string CheckId { get; set; } = string.Empty;
    [JsonPropertyName("check_label")] public string CheckLabel { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")] public string Expected { get; set; } = string.Empty;
    [JsonPropertyName("actual")] public string Actual { get; set; } = string.Empty;
    [JsonPropertyName("details")] public string Details { get; set; } = string.Empty;
}
