using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmissionAuditReceiptResult
{
    [JsonPropertyName("format")] public string Format { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")] public string GeneratedUtc { get; set; } = string.Empty;
    [JsonPropertyName("map_id")] public string MapId { get; set; } = string.Empty;
    [JsonPropertyName("target_component_id")] public string TargetComponentId { get; set; } = string.Empty;
    [JsonPropertyName("source_emitter_result_path")] public string SourceEmitterResultPath { get; set; } = string.Empty;
    [JsonPropertyName("source_emitter_result_sha256")] public string SourceEmitterResultSha256 { get; set; } = string.Empty;
    [JsonPropertyName("source_emitter_result_verdict")] public string SourceEmitterResultVerdict { get; set; } = string.Empty;
    [JsonPropertyName("source_emitter_result_is_valid")] public bool SourceEmitterResultIsValid { get; set; }
    [JsonPropertyName("source_emitter_output_root")] public string SourceEmitterOutputRoot { get; set; } = string.Empty;
    [JsonPropertyName("audited_file_count")] public int AuditedFileCount { get; set; }
    [JsonPropertyName("audited_files")] public List<DeadMtlAuditReceiptAuditedFile> AuditedFiles { get; set; } = new();
    [JsonPropertyName("expected_emitted_record_count")] public int ExpectedEmittedRecordCount { get; set; }
    [JsonPropertyName("actual_emitted_record_count")] public int ActualEmittedRecordCount { get; set; }
    [JsonPropertyName("hash_match_count")] public int HashMatchCount { get; set; }
    [JsonPropertyName("hash_mismatch_count")] public int HashMismatchCount { get; set; }
    [JsonPropertyName("forbidden_artifact_scan")] public DeadMtlAuditReceiptForbiddenScan ForbiddenArtifactScan { get; set; } = new();
    [JsonPropertyName("claim_boundary_audit")] public DeadMtlAuditReceiptClaimBoundaryAudit ClaimBoundaryAudit { get; set; } = new();
    [JsonPropertyName("geometry_record_audit")] public DeadMtlAuditReceiptGeometryRecordAudit GeometryRecordAudit { get; set; } = new();
    [JsonPropertyName("audit_check_count")] public int AuditCheckCount { get; set; }
    [JsonPropertyName("passed_audit_check_count")] public int PassedAuditCheckCount { get; set; }
    [JsonPropertyName("failed_audit_check_count")] public int FailedAuditCheckCount { get; set; }
    [JsonPropertyName("writer_ready")] public bool WriterReady { get; set; }
    [JsonPropertyName("runtime_valid")] public bool RuntimeValid { get; set; }
    [JsonPropertyName("materialized")] public bool Materialized { get; set; }
    [JsonPropertyName("approved_for_writer_experiment")] public bool ApprovedForWriterExperiment { get; set; }
    [JsonPropertyName("writer_experiment_gate_status")] public string WriterExperimentGateStatus { get; set; } = string.Empty;
    [JsonPropertyName("runtime_proof_claimed")] public bool RuntimeProofClaimed { get; set; }
    [JsonPropertyName("writer_ready_claimed")] public bool WriterReadyClaimed { get; set; }
    [JsonPropertyName("public_playable_packaging_claimed")] public bool PublicPlayablePackagingClaimed { get; set; }
    [JsonPropertyName("checks")] public List<DeadMtlAuditReceiptCheck> Checks { get; set; } = new();
    [JsonPropertyName("is_valid")] public bool IsValid { get; set; }
    [JsonPropertyName("verdict")] public string Verdict { get; set; } = string.Empty;
    [JsonPropertyName("errors")] public List<string> Errors { get; set; } = new();
}

public sealed class DeadMtlAuditReceiptAuditedFile
{
    [JsonPropertyName("file_order")] public int FileOrder { get; set; }
    [JsonPropertyName("file_id")] public string FileId { get; set; } = string.Empty;
    [JsonPropertyName("file_kind")] public string FileKind { get; set; } = string.Empty;
    [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;
    [JsonPropertyName("exists")] public bool Exists { get; set; }
    [JsonPropertyName("sha256")] public string Sha256 { get; set; } = string.Empty;
    [JsonPropertyName("size_bytes")] public long SizeBytes { get; set; }
    [JsonPropertyName("expected_sha256_from_emitter_descriptor")] public string ExpectedSha256FromEmitterDescriptor { get; set; } = string.Empty;
    [JsonPropertyName("hash_matches_emitter_descriptor")] public bool HashMatchesEmitterDescriptor { get; set; }
    [JsonPropertyName("required")] public bool Required { get; set; }
}

public sealed class DeadMtlAuditReceiptForbiddenScan
{
    [JsonPropertyName("lotpack_found")] public bool LotpackFound { get; set; }
    [JsonPropertyName("lotheader_found")] public bool LotheaderFound { get; set; }
    [JsonPropertyName("lua_file_found")] public bool LuaFileFound { get; set; }
    [JsonPropertyName("worldgen_override_lua_found")] public bool WorldgenOverrideLuaFound { get; set; }
    [JsonPropertyName("pz_install_path_found")] public bool PzInstallPathFound { get; set; }
    [JsonPropertyName("scan_passed")] public bool ScanPassed { get; set; }
    [JsonPropertyName("details")] public string Details { get; set; } = string.Empty;
}

public sealed class DeadMtlAuditReceiptClaimBoundaryAudit
{
    [JsonPropertyName("writer_ready")] public bool WriterReady { get; set; }
    [JsonPropertyName("runtime_valid")] public bool RuntimeValid { get; set; }
    [JsonPropertyName("materialized")] public bool Materialized { get; set; }
    [JsonPropertyName("approved_for_writer_experiment")] public bool ApprovedForWriterExperiment { get; set; }
    [JsonPropertyName("writer_experiment_gate_status")] public string WriterExperimentGateStatus { get; set; } = string.Empty;
    [JsonPropertyName("runtime_proof_claimed")] public bool RuntimeProofClaimed { get; set; }
    [JsonPropertyName("writer_ready_claimed")] public bool WriterReadyClaimed { get; set; }
    [JsonPropertyName("public_playable_packaging_claimed")] public bool PublicPlayablePackagingClaimed { get; set; }
    [JsonPropertyName("claim_boundary_audit_passed")] public bool ClaimBoundaryAuditPassed { get; set; }
}

public sealed class DeadMtlAuditReceiptGeometryRecordAudit
{
    [JsonPropertyName("target_component_id")] public string TargetComponentId { get; set; } = string.Empty;
    [JsonPropertyName("intent")] public string Intent { get; set; } = string.Empty;
    [JsonPropertyName("access_readiness_class")] public string AccessReadinessClass { get; set; } = string.Empty;
    [JsonPropertyName("bbox_min_x")] public int BboxMinX { get; set; }
    [JsonPropertyName("bbox_min_y")] public int BboxMinY { get; set; }
    [JsonPropertyName("bbox_max_x")] public int BboxMaxX { get; set; }
    [JsonPropertyName("bbox_max_y")] public int BboxMaxY { get; set; }
    [JsonPropertyName("bbox_width_px")] public int BboxWidthPx { get; set; }
    [JsonPropertyName("bbox_height_px")] public int BboxHeightPx { get; set; }
    [JsonPropertyName("lot_count")] public int LotCount { get; set; }
    [JsonPropertyName("building_slot_count")] public int BuildingSlotCount { get; set; }
    [JsonPropertyName("frontage_side")] public string FrontageSide { get; set; } = string.Empty;
    [JsonPropertyName("frontage_component_id")] public string FrontageComponentId { get; set; } = string.Empty;
    [JsonPropertyName("frontage_contact_px")] public int FrontageContactPx { get; set; }
    [JsonPropertyName("rear_service_side")] public string RearServiceSide { get; set; } = string.Empty;
    [JsonPropertyName("rear_service_component_id")] public string RearServiceComponentId { get; set; } = string.Empty;
    [JsonPropertyName("rear_service_contact_px")] public int RearServiceContactPx { get; set; }
    [JsonPropertyName("geometry_audit_passed")] public bool GeometryAuditPassed { get; set; }
}

public sealed class DeadMtlAuditReceiptCheck
{
    [JsonPropertyName("check_order")] public int CheckOrder { get; set; }
    [JsonPropertyName("check_id")] public string CheckId { get; set; } = string.Empty;
    [JsonPropertyName("check_label")] public string CheckLabel { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")] public string Expected { get; set; } = string.Empty;
    [JsonPropertyName("actual")] public string Actual { get; set; } = string.Empty;
    [JsonPropertyName("details")] public string Details { get; set; } = string.Empty;
}
