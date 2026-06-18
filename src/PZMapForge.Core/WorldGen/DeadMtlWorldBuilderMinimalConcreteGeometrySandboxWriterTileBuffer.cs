using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileBufferResult
{
    [JsonPropertyName("format")] public string Format { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")] public string GeneratedUtc { get; set; } = string.Empty;
    [JsonPropertyName("map_id")] public string MapId { get; set; } = string.Empty;
    [JsonPropertyName("target_component_id")] public string TargetComponentId { get; set; } = string.Empty;
    [JsonPropertyName("source_sandbox_writer_result_path")] public string SourceSandboxWriterResultPath { get; set; } = string.Empty;
    [JsonPropertyName("source_sandbox_writer_result_sha256")] public string SourceSandboxWriterResultSha256 { get; set; } = string.Empty;
    [JsonPropertyName("source_sandbox_writer_verdict")] public string SourceSandboxWriterVerdict { get; set; } = string.Empty;
    [JsonPropertyName("source_sandbox_writer_is_valid")] public bool SourceSandboxWriterIsValid { get; set; }
    [JsonPropertyName("writer_stage")] public string WriterStage { get; set; } = "SANDBOX_WRITER_TILE_BUFFER_V0";
    [JsonPropertyName("writer_mode")] public string WriterMode { get; set; } = "APPLY_SANDBOX_OPERATIONS_TO_INTERNAL_TILE_BUFFER_ONLY";
    [JsonPropertyName("sandbox_only")] public bool SandboxOnly { get; set; } = true;
    [JsonPropertyName("buffer_width")] public int BufferWidth { get; set; } = 256;
    [JsonPropertyName("buffer_height")] public int BufferHeight { get; set; } = 256;
    [JsonPropertyName("coordinate_system")] public string CoordinateSystem { get; set; } = "PNG_PIXEL_TILE_SPACE";
    [JsonPropertyName("origin")] public string Origin { get; set; } = "TOP_LEFT";
    [JsonPropertyName("input_operation_file_count")] public int InputOperationFileCount { get; set; }
    [JsonPropertyName("input_operation_count")] public int InputOperationCount { get; set; }
    [JsonPropertyName("touched_cell_count")] public int TouchedCellCount { get; set; }
    [JsonPropertyName("primary_owned_cell_count")] public int PrimaryOwnedCellCount { get; set; }
    [JsonPropertyName("component_operation_count")] public int ComponentOperationCount { get; set; }
    [JsonPropertyName("lot_operation_count")] public int LotOperationCount { get; set; }
    [JsonPropertyName("building_slot_operation_count")] public int BuildingSlotOperationCount { get; set; }
    [JsonPropertyName("access_operation_count")] public int AccessOperationCount { get; set; }
    [JsonPropertyName("replay_entry_count")] public int ReplayEntryCount { get; set; }
    [JsonPropertyName("collision_count")] public int CollisionCount { get; set; }
    [JsonPropertyName("ownership_kind_count")] public int OwnershipKindCount { get; set; }
    [JsonPropertyName("output_files")] public List<DeadMtlSandboxWriterTileBufferOutputFile> OutputFiles { get; set; } = new();
    [JsonPropertyName("checks")] public List<DeadMtlSandboxWriterTileBufferCheck> Checks { get; set; } = new();
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

public sealed class DeadMtlSandboxWriterTileCell
{
    [JsonPropertyName("x")] public int X { get; set; }
    [JsonPropertyName("y")] public int Y { get; set; }
    [JsonPropertyName("primary_owner_kind")] public string PrimaryOwnerKind { get; set; } = string.Empty;
    [JsonPropertyName("primary_owner_id")] public string PrimaryOwnerId { get; set; } = string.Empty;
    [JsonPropertyName("tags")] public string Tags { get; set; } = string.Empty;
    [JsonPropertyName("operation_ids")] public string OperationIds { get; set; } = string.Empty;
    [JsonPropertyName("collision_count")] public int CollisionCount { get; set; }
}

public sealed class DeadMtlSandboxWriterTileBufferOwnershipRecord
{
    [JsonPropertyName("kind")] public string Kind { get; set; } = string.Empty;
    [JsonPropertyName("claimed_cell_count")] public int ClaimedCellCount { get; set; }
    [JsonPropertyName("primary_owned_cell_count")] public int PrimaryOwnedCellCount { get; set; }
    [JsonPropertyName("operation_count")] public int OperationCount { get; set; }
    [JsonPropertyName("source_operation_kinds")] public List<string> SourceOperationKinds { get; set; } = new();
}

public sealed class DeadMtlSandboxWriterTileBufferReplayEntry
{
    [JsonPropertyName("replay_order")] public int ReplayOrder { get; set; }
    [JsonPropertyName("operation_kind")] public string OperationKind { get; set; } = string.Empty;
    [JsonPropertyName("operation_group")] public string OperationGroup { get; set; } = string.Empty;
    [JsonPropertyName("source_file")] public string SourceFile { get; set; } = string.Empty;
    [JsonPropertyName("source_operation_order")] public int SourceOperationOrder { get; set; }
    [JsonPropertyName("applied_cell_count")] public int AppliedCellCount { get; set; }
    [JsonPropertyName("overwritten_cell_count")] public int OverwrittenCellCount { get; set; }
    [JsonPropertyName("collision_count")] public int CollisionCount { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "APPLIED_TO_SANDBOX_TILE_BUFFER";
    [JsonPropertyName("runtime_effect")] public string RuntimeEffect { get; set; } = "NONE";
}

public sealed class DeadMtlSandboxWriterTileBufferCollisionRecord
{
    [JsonPropertyName("collision_order")] public int CollisionOrder { get; set; }
    [JsonPropertyName("x")] public int X { get; set; }
    [JsonPropertyName("y")] public int Y { get; set; }
    [JsonPropertyName("previous_owner_kind")] public string PreviousOwnerKind { get; set; } = string.Empty;
    [JsonPropertyName("previous_owner_id")] public string PreviousOwnerId { get; set; } = string.Empty;
    [JsonPropertyName("new_owner_kind")] public string NewOwnerKind { get; set; } = string.Empty;
    [JsonPropertyName("new_owner_id")] public string NewOwnerId { get; set; } = string.Empty;
    [JsonPropertyName("operation_kind")] public string OperationKind { get; set; } = string.Empty;
    [JsonPropertyName("resolution")] public string Resolution { get; set; } = string.Empty;
}

public sealed class DeadMtlSandboxWriterTileBufferOutputFile
{
    [JsonPropertyName("file_order")] public int FileOrder { get; set; }
    [JsonPropertyName("file_name")] public string FileName { get; set; } = string.Empty;
    [JsonPropertyName("file_path")] public string FilePath { get; set; } = string.Empty;
    [JsonPropertyName("file_kind")] public string FileKind { get; set; } = string.Empty;
    [JsonPropertyName("sha256")] public string Sha256 { get; set; } = string.Empty;
    [JsonPropertyName("written")] public bool Written { get; set; }
}

public sealed class DeadMtlSandboxWriterTileBufferCheck
{
    [JsonPropertyName("check_order")] public int CheckOrder { get; set; }
    [JsonPropertyName("check_id")] public string CheckId { get; set; } = string.Empty;
    [JsonPropertyName("check_label")] public string CheckLabel { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")] public string Expected { get; set; } = string.Empty;
    [JsonPropertyName("actual")] public string Actual { get; set; } = string.Empty;
    [JsonPropertyName("details")] public string Details { get; set; } = string.Empty;
}
