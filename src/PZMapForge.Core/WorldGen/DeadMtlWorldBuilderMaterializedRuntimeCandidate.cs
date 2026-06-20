using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMaterializedRuntimeCandidateResult
{
    [JsonPropertyName("format")]               public string Format             { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")]        public string GeneratedUtc       { get; set; } = string.Empty;
    [JsonPropertyName("source_lot_fill_json")] public string SourceLotFillJson  { get; set; } = string.Empty;
    [JsonPropertyName("source_footprint_json")]public string SourceFootprintJson { get; set; } = string.Empty;
    [JsonPropertyName("output_root")]          public string OutputRoot         { get; set; } = string.Empty;
    [JsonPropertyName("map_id")]               public string MapId              { get; set; } = string.Empty;
    [JsonPropertyName("map_name")]             public string MapName            { get; set; } = string.Empty;
    [JsonPropertyName("staged_mod_root")]      public string StagedModRoot      { get; set; } = string.Empty;
    [JsonPropertyName("staged_map_root")]      public string StagedMapRoot      { get; set; } = string.Empty;

    [JsonPropertyName("lot_count")]            public int    LotCount           { get; set; }
    [JsonPropertyName("footprint_count")]      public int    FootprintCount     { get; set; }
    [JsonPropertyName("skipped_lot_count")]    public int    SkippedLotCount    { get; set; }
    [JsonPropertyName("sector_counts")]        public List<SectorCountEntry> SectorCounts { get; set; } = new();

    [JsonPropertyName("metadata_files_written")]    public List<string> MetadataFilesWritten   { get; set; } = new();
    [JsonPropertyName("binary_cell_files_written")] public List<string> BinaryCellFilesWritten { get; set; } = new();
    [JsonPropertyName("binary_cell_materialized")]  public bool   BinaryCellMaterialized       { get; set; }
    [JsonPropertyName("binary_materialization_gap")]public string BinaryMaterializationGap     { get; set; } = string.Empty;

    [JsonPropertyName("spawnpoints_written")]                public bool SpawnpointsWritten              { get; set; }
    [JsonPropertyName("objects_lua_written")]                public bool ObjectsLuaWritten               { get; set; }
    [JsonPropertyName("raw_source_png_mutated")]             public bool RawSourcePngMutated             { get; set; }
    [JsonPropertyName("sandbox_only")]                       public bool SandboxOnly                     { get; set; } = true;
    [JsonPropertyName("writer_ready")]                       public bool WriterReady                     { get; set; }
    [JsonPropertyName("runtime_valid")]                      public bool RuntimeValid                    { get; set; }
    [JsonPropertyName("runtime_proof_claimed")]              public bool RuntimeProofClaimed             { get; set; }
    [JsonPropertyName("public_playable_packaging_claimed")]  public bool PublicPlayablePackagingClaimed  { get; set; }
    [JsonPropertyName("playable_export_claimed")]            public bool PlayableExportClaimed           { get; set; }

    [JsonPropertyName("check_count")]        public int    CheckCount        { get; set; }
    [JsonPropertyName("passed_check_count")] public int    PassedCheckCount  { get; set; }
    [JsonPropertyName("failed_check_count")] public int    FailedCheckCount  { get; set; }
    [JsonPropertyName("is_valid")]           public bool   IsValid           { get; set; }
    [JsonPropertyName("verdict")]            public string Verdict           { get; set; } = string.Empty;
    [JsonPropertyName("checks")]             public List<MaterializedRuntimeCandidateCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")]             public List<string> Errors      { get; set; } = new();
}

public sealed class MaterializedRuntimeCandidateCheck
{
    [JsonPropertyName("check_id")]     public string CheckId     { get; set; } = string.Empty;
    [JsonPropertyName("description")]  public string Description { get; set; } = string.Empty;
    [JsonPropertyName("expected")]     public string Expected    { get; set; } = string.Empty;
    [JsonPropertyName("actual")]       public string Actual      { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
}
