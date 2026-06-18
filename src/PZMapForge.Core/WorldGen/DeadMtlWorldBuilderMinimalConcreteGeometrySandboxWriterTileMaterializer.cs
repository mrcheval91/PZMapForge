using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometrySandboxWriterTileMaterializerResult
{
    [JsonPropertyName("format")] public string Format { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")] public string GeneratedUtc { get; set; } = string.Empty;
    [JsonPropertyName("map_id")] public string MapId { get; set; } = string.Empty;
    [JsonPropertyName("target_component_id")] public string TargetComponentId { get; set; } = string.Empty;
    [JsonPropertyName("source_tile_buffer_result_path")] public string SourceTileBufferResultPath { get; set; } = string.Empty;
    [JsonPropertyName("source_tile_buffer_result_sha256")] public string SourceTileBufferResultSha256 { get; set; } = string.Empty;
    [JsonPropertyName("source_tile_buffer_verdict")] public string SourceTileBufferVerdict { get; set; } = string.Empty;
    [JsonPropertyName("source_tile_buffer_is_valid")] public bool SourceTileBufferIsValid { get; set; }
    [JsonPropertyName("writer_stage")] public string WriterStage { get; set; } = "SANDBOX_WRITER_TILE_MATERIALIZER_V0";
    [JsonPropertyName("writer_mode")] public string WriterMode { get; set; } = "MATERIALIZE_SANDBOX_TILE_BUFFER_TO_SANDBOX_MATERIAL_RECORDS_ONLY";
    [JsonPropertyName("sandbox_only")] public bool SandboxOnly { get; set; } = true;
    [JsonPropertyName("sandbox_materialized")] public bool SandboxMaterialized { get; set; } = true;
    [JsonPropertyName("pz_runtime_materialized")] public bool PzRuntimeMaterialized { get; set; }
    [JsonPropertyName("buffer_width")] public int BufferWidth { get; set; } = 256;
    [JsonPropertyName("buffer_height")] public int BufferHeight { get; set; } = 256;
    [JsonPropertyName("input_touched_cell_count")] public int InputTouchedCellCount { get; set; }
    [JsonPropertyName("materialized_cell_count")] public int MaterializedCellCount { get; set; }
    [JsonPropertyName("building_footprint_cell_count")] public int BuildingFootprintCellCount { get; set; }
    [JsonPropertyName("building_wall_candidate_cell_count")] public int BuildingWallCandidateCellCount { get; set; }
    [JsonPropertyName("building_floor_candidate_cell_count")] public int BuildingFloorCandidateCellCount { get; set; }
    [JsonPropertyName("access_edge_cell_count")] public int AccessEdgeCellCount { get; set; }
    [JsonPropertyName("lot_space_cell_count")] public int LotSpaceCellCount { get; set; }
    [JsonPropertyName("component_residual_cell_count")] public int ComponentResidualCellCount { get; set; }
    [JsonPropertyName("material_kind_count")] public int MaterialKindCount { get; set; }
    [JsonPropertyName("layer_kind_count")] public int LayerKindCount { get; set; }
    [JsonPropertyName("output_file_count")] public int OutputFileCount { get; set; }
    [JsonPropertyName("output_files")] public List<DeadMtlTileMaterializerOutputFile> OutputFiles { get; set; } = new();
    [JsonPropertyName("checks")] public List<DeadMtlTileMaterializerCheck> Checks { get; set; } = new();
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
    [JsonPropertyName("errors")] public List<string> Errors { get; set; } = new();
}

public sealed class DeadMtlSandboxWriterTileMaterializedCell
{
    [JsonPropertyName("x")] public int X { get; set; }
    [JsonPropertyName("y")] public int Y { get; set; }
    [JsonPropertyName("primary_owner_kind")] public string PrimaryOwnerKind { get; set; } = string.Empty;
    [JsonPropertyName("primary_owner_id")] public string PrimaryOwnerId { get; set; } = string.Empty;
    [JsonPropertyName("material_kind")] public string MaterialKind { get; set; } = string.Empty;
    [JsonPropertyName("layer_kind")] public string LayerKind { get; set; } = string.Empty;
    [JsonPropertyName("source_operation_ids")] public string SourceOperationIds { get; set; } = string.Empty;
    [JsonPropertyName("collision_count")] public int CollisionCount { get; set; }
}

public sealed class DeadMtlTileMaterialPaletteRecord
{
    [JsonPropertyName("palette_order")] public int PaletteOrder { get; set; }
    [JsonPropertyName("material_kind")] public string MaterialKind { get; set; } = string.Empty;
    [JsonPropertyName("layer_kind")] public string LayerKind { get; set; } = string.Empty;
    [JsonPropertyName("cell_count")] public int CellCount { get; set; }
}

public sealed class DeadMtlTileLayerStackRecord
{
    [JsonPropertyName("layer_order")] public int LayerOrder { get; set; }
    [JsonPropertyName("layer_kind")] public string LayerKind { get; set; } = string.Empty;
    [JsonPropertyName("material_kinds")] public List<string> MaterialKinds { get; set; } = new();
    [JsonPropertyName("cell_count")] public int CellCount { get; set; }
}

public sealed class DeadMtlTileMaterializationReplayEntry
{
    [JsonPropertyName("replay_order")] public int ReplayOrder { get; set; }
    [JsonPropertyName("owner_kind")] public string OwnerKind { get; set; } = string.Empty;
    [JsonPropertyName("material_kind")] public string MaterialKind { get; set; } = string.Empty;
    [JsonPropertyName("cell_count")] public int CellCount { get; set; }
}

public sealed class DeadMtlTileMaterializationOwnershipSummaryRecord
{
    [JsonPropertyName("owner_kind")] public string OwnerKind { get; set; } = string.Empty;
    [JsonPropertyName("materialized_cell_count")] public int MaterializedCellCount { get; set; }
    [JsonPropertyName("material_kinds")] public List<string> MaterialKinds { get; set; } = new();
}

public sealed class DeadMtlTileMaterializerOutputFile
{
    [JsonPropertyName("file_order")] public int FileOrder { get; set; }
    [JsonPropertyName("file_name")] public string FileName { get; set; } = string.Empty;
    [JsonPropertyName("file_path")] public string FilePath { get; set; } = string.Empty;
    [JsonPropertyName("file_kind")] public string FileKind { get; set; } = string.Empty;
    [JsonPropertyName("sha256")] public string Sha256 { get; set; } = string.Empty;
    [JsonPropertyName("written")] public bool Written { get; set; }
}

public sealed class DeadMtlTileMaterializerCheck
{
    [JsonPropertyName("check_order")] public int CheckOrder { get; set; }
    [JsonPropertyName("check_id")] public string CheckId { get; set; } = string.Empty;
    [JsonPropertyName("check_label")] public string CheckLabel { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")] public string Expected { get; set; } = string.Empty;
    [JsonPropertyName("actual")] public string Actual { get; set; } = string.Empty;
}
