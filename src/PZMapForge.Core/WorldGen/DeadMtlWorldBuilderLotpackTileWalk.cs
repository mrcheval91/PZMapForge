using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderLotpackTileWalkResult
{
    [JsonPropertyName("format")]         public string Format       { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")]  public string GeneratedUtc { get; set; } = string.Empty;
    // Input paths
    [JsonPropertyName("minimal_lotpack_path")]  public string MinimalLotpackPath { get; set; } = string.Empty;
    [JsonPropertyName("visible_lotpack_path")]  public string VisibleLotpackPath { get; set; } = string.Empty;
    [JsonPropertyName("output_root")]           public string OutputRoot         { get; set; } = string.Empty;
    // Input availability
    [JsonPropertyName("minimal_lotpack_found")]  public bool MinimalLotpackFound { get; set; }
    [JsonPropertyName("visible_lotpack_found")]  public bool VisibleLotpackFound { get; set; }
    // File sizes
    [JsonPropertyName("minimal_size")]  public long MinimalSize { get; set; }
    [JsonPropertyName("visible_size")]  public long VisibleSize { get; set; }
    [JsonPropertyName("size_delta")]    public long SizeDelta   { get; set; }
    [JsonPropertyName("size_delta_classification")] public string SizeDeltaClassification { get; set; } = string.Empty;
    // SHA256
    [JsonPropertyName("minimal_sha256")]   public string MinimalSha256  { get; set; } = string.Empty;
    [JsonPropertyName("visible_sha256")]   public string VisibleSha256  { get; set; } = string.Empty;
    [JsonPropertyName("sha256_computed")]  public bool   Sha256Computed { get; set; }
    // Structural analysis
    [JsonPropertyName("common_prefix_length")]  public long CommonPrefixLength { get; set; }
    [JsonPropertyName("common_suffix_length")]  public long CommonSuffixLength { get; set; }
    [JsonPropertyName("diff_runs")]             public List<DiffRunRecord>         DiffRuns         { get; set; } = new();
    [JsonPropertyName("candidate_regions")]     public List<CandidateRegionRecord> CandidateRegions { get; set; } = new();
    [JsonPropertyName("hexdump_sections")]      public List<HexdumpSection>        HexdumpSections  { get; set; } = new();
    // Record-size analysis
    [JsonPropertyName("candidate_record_sizes")] public List<LotpackCandidateRecordSizeRecord> CandidateRecordSizes { get; set; } = new();
    // String table
    [JsonPropertyName("string_table")]    public List<LotpackStringTableRecord>    StringTable    { get; set; } = new();
    // Byte frequency
    [JsonPropertyName("byte_frequency")]  public List<LotpackByteFrequencyRecord>  ByteFrequency  { get; set; } = new();
    // Offset samples
    [JsonPropertyName("offset_samples")]  public List<LotpackOffsetSampleRecord>   OffsetSamples  { get; set; } = new();
    // Claim boundary
    [JsonPropertyName("runtime_binary_written")]     public bool RuntimeBinaryWritten    { get; set; }
    [JsonPropertyName("geometry_injected")]          public bool GeometryInjected        { get; set; }
    [JsonPropertyName("playable_export_claimed")]    public bool PlayableExportClaimed   { get; set; }
    [JsonPropertyName("workshop_upload_performed")]  public bool WorkshopUploadPerformed { get; set; }
    [JsonPropertyName("steam_install_write")]        public bool SteamInstallWrite       { get; set; }
    // Output artifacts
    [JsonPropertyName("output_artifacts")] public List<string> OutputArtifacts { get; set; } = new();
    // Result
    [JsonPropertyName("check_count")]        public int    CheckCount       { get; set; }
    [JsonPropertyName("passed_check_count")] public int    PassedCheckCount { get; set; }
    [JsonPropertyName("failed_check_count")] public int    FailedCheckCount { get; set; }
    [JsonPropertyName("is_valid")]           public bool   IsValid          { get; set; }
    [JsonPropertyName("verdict")]            public string Verdict          { get; set; } = string.Empty;
    [JsonPropertyName("checks")]             public List<LotpackTileWalkCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")]             public List<string>               Errors { get; set; } = new();
}

public sealed class LotpackCandidateRecordSizeRecord
{
    [JsonPropertyName("record_size")]                 public int    RecordSize              { get; set; }
    [JsonPropertyName("visible_full_record_count")]   public int    VisibleFullRecordCount  { get; set; }
    [JsonPropertyName("visible_remainder")]           public long   VisibleRemainder        { get; set; }
    [JsonPropertyName("minimal_full_record_count")]   public int    MinimalFullRecordCount  { get; set; }
    [JsonPropertyName("minimal_remainder")]           public long   MinimalRemainder        { get; set; }
    [JsonPropertyName("divides_visible_exactly")]     public bool   DividesVisibleExactly   { get; set; }
    [JsonPropertyName("divides_minimal_exactly")]     public bool   DividesMinimalExactly   { get; set; }
    [JsonPropertyName("divides_delta_exactly")]       public bool   DividesDeltaExactly     { get; set; }
    [JsonPropertyName("label")]                       public string Label                   { get; set; } = string.Empty;
}

public sealed class LotpackStringTableRecord
{
    [JsonPropertyName("offset")]   public long   Offset  { get; set; }
    [JsonPropertyName("length")]   public int    Length  { get; set; }
    [JsonPropertyName("source")]   public string Source  { get; set; } = string.Empty;
    [JsonPropertyName("preview")]  public string Preview { get; set; } = string.Empty;
}

public sealed class LotpackByteFrequencyRecord
{
    [JsonPropertyName("byte_value")]          public int    ByteValue          { get; set; }
    [JsonPropertyName("hex_value")]           public string HexValue           { get; set; } = string.Empty;
    [JsonPropertyName("minimal_count")]       public long   MinimalCount       { get; set; }
    [JsonPropertyName("minimal_frequency")]   public double MinimalFrequency   { get; set; }
    [JsonPropertyName("visible_count")]       public long   VisibleCount       { get; set; }
    [JsonPropertyName("visible_frequency")]   public double VisibleFrequency   { get; set; }
}

public sealed class LotpackOffsetSampleRecord
{
    [JsonPropertyName("sample_offset")]   public long   SampleOffset  { get; set; }
    [JsonPropertyName("interval_bytes")]  public int    IntervalBytes { get; set; }
    [JsonPropertyName("source")]          public string Source        { get; set; } = string.Empty;
    [JsonPropertyName("hex_preview")]     public string HexPreview    { get; set; } = string.Empty;
}

public sealed class LotpackTileWalkCheck
{
    [JsonPropertyName("check_id")]     public string CheckId     { get; set; } = string.Empty;
    [JsonPropertyName("description")]  public string Description { get; set; } = string.Empty;
    [JsonPropertyName("expected")]     public string Expected    { get; set; } = string.Empty;
    [JsonPropertyName("actual")]       public string Actual      { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
}
