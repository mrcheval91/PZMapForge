using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderChunkdataHexDisassemblyResult
{
    [JsonPropertyName("format")]         public string Format       { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")]  public string GeneratedUtc { get; set; } = string.Empty;
    // Input paths
    [JsonPropertyName("minimal_chunkdata_path")]  public string MinimalChunkdataPath { get; set; } = string.Empty;
    [JsonPropertyName("visible_chunkdata_path")]  public string VisibleChunkdataPath { get; set; } = string.Empty;
    [JsonPropertyName("output_root")]             public string OutputRoot           { get; set; } = string.Empty;
    // Input availability
    [JsonPropertyName("minimal_chunkdata_found")]  public bool MinimalChunkdataFound { get; set; }
    [JsonPropertyName("visible_chunkdata_found")]  public bool VisibleChunkdataFound { get; set; }
    // File sizes
    [JsonPropertyName("minimal_size")]  public long MinimalSize { get; set; }
    [JsonPropertyName("visible_size")]  public long VisibleSize { get; set; }
    [JsonPropertyName("size_delta")]    public long SizeDelta   { get; set; }
    // SHA256
    [JsonPropertyName("minimal_sha256")]   public string MinimalSha256  { get; set; } = string.Empty;
    [JsonPropertyName("visible_sha256")]   public string VisibleSha256  { get; set; } = string.Empty;
    [JsonPropertyName("sha256_computed")]  public bool   Sha256Computed { get; set; }
    // Structural analysis
    [JsonPropertyName("common_prefix_length")]  public long CommonPrefixLength { get; set; }
    [JsonPropertyName("common_suffix_length")]  public long CommonSuffixLength { get; set; }
    [JsonPropertyName("diff_runs")]             public List<DiffRunRecord>             DiffRuns             { get; set; } = new();
    [JsonPropertyName("candidate_regions")]     public List<CandidateRegionRecord>     CandidateRegions     { get; set; } = new();
    [JsonPropertyName("hexdump_sections")]      public List<HexdumpSection>            HexdumpSections      { get; set; } = new();
    // Record-size analysis
    [JsonPropertyName("candidate_record_sizes")]   public List<CandidateRecordSizeRecord> CandidateRecordSizes  { get; set; } = new();
    [JsonPropertyName("record_samples_8byte")]     public List<RecordSampleRecord>         RecordSamples8Byte    { get; set; } = new();
    [JsonPropertyName("record_samples_16byte")]    public List<RecordSampleRecord>         RecordSamples16Byte   { get; set; } = new();
    [JsonPropertyName("record_samples_32byte")]    public List<RecordSampleRecord>         RecordSamples32Byte   { get; set; } = new();
    // Claim boundary
    [JsonPropertyName("runtime_binary_written")]     public bool RuntimeBinaryWritten    { get; set; }
    [JsonPropertyName("geometry_injected")]          public bool GeometryInjected        { get; set; }
    [JsonPropertyName("playable_export_claimed")]    public bool PlayableExportClaimed   { get; set; }
    [JsonPropertyName("workshop_upload_performed")]  public bool WorkshopUploadPerformed { get; set; }
    [JsonPropertyName("steam_install_write")]        public bool SteamInstallWrite       { get; set; }
    // Output artifacts
    [JsonPropertyName("output_artifacts")]           public List<string> OutputArtifacts { get; set; } = new();
    // Result
    [JsonPropertyName("check_count")]        public int    CheckCount       { get; set; }
    [JsonPropertyName("passed_check_count")] public int    PassedCheckCount { get; set; }
    [JsonPropertyName("failed_check_count")] public int    FailedCheckCount { get; set; }
    [JsonPropertyName("is_valid")]           public bool   IsValid          { get; set; }
    [JsonPropertyName("verdict")]            public string Verdict          { get; set; } = string.Empty;
    [JsonPropertyName("checks")]             public List<ChunkdataHexDisassemblyCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")]             public List<string>                       Errors { get; set; } = new();
}

public sealed class CandidateRecordSizeRecord
{
    [JsonPropertyName("record_size")]                    public int  RecordSize                  { get; set; }
    [JsonPropertyName("visible_full_record_count")]      public int  VisibleFullRecordCount       { get; set; }
    [JsonPropertyName("visible_remainder")]              public long VisibleRemainder             { get; set; }
    [JsonPropertyName("minimal_full_record_count")]      public int  MinimalFullRecordCount       { get; set; }
    [JsonPropertyName("minimal_remainder")]              public long MinimalRemainder             { get; set; }
    [JsonPropertyName("divides_visible_exactly")]        public bool DividesVisibleExactly        { get; set; }
    [JsonPropertyName("divides_minimal_exactly")]        public bool DividesMinimalExactly        { get; set; }
    [JsonPropertyName("divides_expansion_exactly")]      public bool DividesExpansionExactly      { get; set; }
    [JsonPropertyName("divides_visible_minus2_exactly")] public bool DividesVisibleMinus2Exactly  { get; set; }
    [JsonPropertyName("divides_minimal_minus2_exactly")] public bool DividesMinimalMinus2Exactly  { get; set; }
    [JsonPropertyName("label")]                          public string Label                      { get; set; } = string.Empty;
}

public sealed class RecordSampleRecord
{
    [JsonPropertyName("record_index")]  public int    RecordIndex { get; set; }
    [JsonPropertyName("file_offset")]   public long   FileOffset  { get; set; }
    [JsonPropertyName("record_size")]   public int    RecordSize  { get; set; }
    [JsonPropertyName("hex_bytes")]     public string HexBytes    { get; set; } = string.Empty;
    [JsonPropertyName("source")]        public string Source      { get; set; } = string.Empty;
}

public sealed class ChunkdataHexDisassemblyCheck
{
    [JsonPropertyName("check_id")]     public string CheckId     { get; set; } = string.Empty;
    [JsonPropertyName("description")]  public string Description { get; set; } = string.Empty;
    [JsonPropertyName("expected")]     public string Expected    { get; set; } = string.Empty;
    [JsonPropertyName("actual")]       public string Actual      { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
}
