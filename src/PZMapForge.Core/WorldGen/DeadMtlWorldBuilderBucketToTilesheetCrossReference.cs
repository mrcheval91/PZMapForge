using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderBucketToTilesheetCrossReferenceResult
{
    [JsonPropertyName("format")]         public string Format       { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")]  public string GeneratedUtc { get; set; } = string.Empty;
    // Input source paths
    [JsonPropertyName("emitter_json_path")]       public string EmitterJsonPath      { get; set; } = string.Empty;
    [JsonPropertyName("palette_guide_path")]      public string PaletteGuidePath     { get; set; } = string.Empty;
    [JsonPropertyName("palette_swatches_path")]   public string PaletteSwatchesPath  { get; set; } = string.Empty;
    [JsonPropertyName("pz_install_root")]         public string PzInstallRoot        { get; set; } = string.Empty;
    [JsonPropertyName("output_root")]             public string OutputRoot            { get; set; } = string.Empty;
    // Source availability
    [JsonPropertyName("emitter_json_found")]      public bool EmitterJsonFound     { get; set; }
    [JsonPropertyName("palette_guide_found")]     public bool PaletteGuideFound    { get; set; }
    [JsonPropertyName("palette_swatches_found")]  public bool PaletteSwatchesFound { get; set; }
    [JsonPropertyName("pz_install_found")]        public bool PzInstallFound       { get; set; }
    // Analysis
    [JsonPropertyName("bucket_intents")]              public List<BucketIntentRecord>                  BucketIntents              { get; set; } = new();
    [JsonPropertyName("tile_source_inventory")]       public List<TileSourceInventoryRecord>           TileSourceInventory        { get; set; } = new();
    [JsonPropertyName("tilesheet_candidates")]        public List<TilesheetCandidateRecord>            TilesheetCandidates        { get; set; } = new();
    [JsonPropertyName("bucket_to_tilesheet_candidates")] public List<BucketToTilesheetCandidateRecord> BucketToTilesheetCandidates { get; set; } = new();
    // Claim boundary
    [JsonPropertyName("runtime_binary_written")]      public bool RuntimeBinaryWritten    { get; set; }
    [JsonPropertyName("geometry_injected")]           public bool GeometryInjected        { get; set; }
    [JsonPropertyName("playable_export_claimed")]     public bool PlayableExportClaimed   { get; set; }
    [JsonPropertyName("workshop_upload_performed")]   public bool WorkshopUploadPerformed { get; set; }
    [JsonPropertyName("steam_install_write")]         public bool SteamInstallWrite       { get; set; }
    [JsonPropertyName("verified_runtime_tile_id")]    public bool VerifiedRuntimeTileId   { get; set; }
    [JsonPropertyName("read_only_probe")]             public bool ReadOnlyProbe           { get; set; }
    // Output artifacts
    [JsonPropertyName("output_artifacts")] public List<string> OutputArtifacts { get; set; } = new();
    // Result
    [JsonPropertyName("check_count")]        public int    CheckCount       { get; set; }
    [JsonPropertyName("passed_check_count")] public int    PassedCheckCount { get; set; }
    [JsonPropertyName("failed_check_count")] public int    FailedCheckCount { get; set; }
    [JsonPropertyName("is_valid")]           public bool   IsValid          { get; set; }
    [JsonPropertyName("verdict")]            public string Verdict          { get; set; } = string.Empty;
    [JsonPropertyName("checks")]             public List<BucketToTilesheetCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")]             public List<string>                 Errors { get; set; } = new();
}

public sealed class BucketIntentRecord
{
    [JsonPropertyName("bucket")]       public string Bucket      { get; set; } = string.Empty;
    [JsonPropertyName("intended_use")] public string IntendedUse { get; set; } = string.Empty;
    [JsonPropertyName("source")]       public string Source      { get; set; } = string.Empty;
}

public sealed class TileSourceInventoryRecord
{
    [JsonPropertyName("file_path")]        public string FilePath       { get; set; } = string.Empty;
    [JsonPropertyName("file_size_bytes")]  public long   FileSizeBytes  { get; set; }
    [JsonPropertyName("tile_terms_found")] public int    TileTermsFound { get; set; }
    [JsonPropertyName("source_type")]      public string SourceType     { get; set; } = string.Empty;
}

public sealed class TilesheetCandidateRecord
{
    [JsonPropertyName("candidate_name")]          public string CandidateName        { get; set; } = string.Empty;
    [JsonPropertyName("source_file")]             public string SourceFile           { get; set; } = string.Empty;
    [JsonPropertyName("evidence_text_preview")]   public string EvidenceTextPreview  { get; set; } = string.Empty;
    [JsonPropertyName("source_type")]             public string SourceType           { get; set; } = string.Empty;
}

public sealed class BucketToTilesheetCandidateRecord
{
    [JsonPropertyName("bucket")]                             public string Bucket                       { get; set; } = string.Empty;
    [JsonPropertyName("intended_use")]                       public string IntendedUse                  { get; set; } = string.Empty;
    [JsonPropertyName("candidate_tilesheet_or_tile_name")]   public string CandidateTilesheetOrTileName { get; set; } = string.Empty;
    [JsonPropertyName("source_file")]                        public string SourceFile                   { get; set; } = string.Empty;
    [JsonPropertyName("evidence_text_preview")]              public string EvidenceTextPreview          { get; set; } = string.Empty;
    [JsonPropertyName("confidence_label")]                   public string ConfidenceLabel              { get; set; } = string.Empty;
    [JsonPropertyName("verified_runtime_tile_id")]           public bool   VerifiedRuntimeTileId        { get; set; }
}

public sealed class BucketToTilesheetCheck
{
    [JsonPropertyName("check_id")]     public string CheckId     { get; set; } = string.Empty;
    [JsonPropertyName("description")]  public string Description { get; set; } = string.Empty;
    [JsonPropertyName("expected")]     public string Expected    { get; set; } = string.Empty;
    [JsonPropertyName("actual")]       public string Actual      { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
}
