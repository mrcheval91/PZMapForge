using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderChunkdataRecordClusterAuditResult
{
    [JsonPropertyName("format")]               public string Format        { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")]        public string GeneratedUtc  { get; set; } = string.Empty;
    [JsonPropertyName("minimal_chunkdata_path")] public string MinimalChunkdataPath { get; set; } = string.Empty;
    [JsonPropertyName("visible_chunkdata_path")] public string VisibleChunkdataPath { get; set; } = string.Empty;
    [JsonPropertyName("output_root")]          public string OutputRoot    { get; set; } = string.Empty;
    // Availability
    [JsonPropertyName("minimal_found")]  public bool MinimalFound { get; set; }
    [JsonPropertyName("visible_found")]  public bool VisibleFound { get; set; }
    // Sizes
    [JsonPropertyName("minimal_size")]  public long MinimalSize { get; set; }
    [JsonPropertyName("visible_size")]  public long VisibleSize { get; set; }
    [JsonPropertyName("size_delta")]    public long SizeDelta   { get; set; }
    // SHA256
    [JsonPropertyName("minimal_sha256")]  public string MinimalSha256 { get; set; } = string.Empty;
    [JsonPropertyName("visible_sha256")]  public string VisibleSha256 { get; set; } = string.Empty;
    // Analysis outputs
    [JsonPropertyName("candidate_header_sizes")]        public List<RecordClusterHeaderSizeRecord>   CandidateHeaderSizes      { get; set; } = new();
    [JsonPropertyName("record_width_analyses")]         public List<RecordWidthAnalysisRecord>       RecordWidthAnalyses       { get; set; } = new();
    [JsonPropertyName("record_column_statistics")]      public List<RecordColumnStatRecord>          RecordColumnStatistics    { get; set; } = new();
    [JsonPropertyName("repeated_record_clusters")]      public List<RepeatedRecordClusterRecord>     RepeatedRecordClusters    { get; set; } = new();
    [JsonPropertyName("zero_record_clusters")]          public List<ZeroRecordClusterRecord>         ZeroRecordClusters        { get; set; } = new();
    [JsonPropertyName("byte_pattern_clusters")]         public List<BytePatternClusterRecord>        BytePatternClusters       { get; set; } = new();
    [JsonPropertyName("changed_record_windows")]        public List<ChangedRecordWindowRecord>       ChangedRecordWindows      { get; set; } = new();
    [JsonPropertyName("minimal_record_samples")]        public List<RecordClusterSampleRecord>       MinimalRecordSamples      { get; set; } = new();
    [JsonPropertyName("visible_record_samples")]        public List<RecordClusterSampleRecord>       VisibleRecordSamples      { get; set; } = new();
    [JsonPropertyName("hypothesis_summary")]            public List<string>                          HypothesisSummary         { get; set; } = new();
    // Claim boundary
    [JsonPropertyName("runtime_binary_written")]      public bool RuntimeBinaryWritten    { get; set; }
    [JsonPropertyName("geometry_injected")]           public bool GeometryInjected        { get; set; }
    [JsonPropertyName("playable_export_claimed")]     public bool PlayableExportClaimed   { get; set; }
    [JsonPropertyName("workshop_upload_performed")]   public bool WorkshopUploadPerformed { get; set; }
    [JsonPropertyName("steam_install_write")]         public bool SteamInstallWrite       { get; set; }
    [JsonPropertyName("verified_chunkdata_format")]   public bool VerifiedChunkdataFormat { get; set; }
    [JsonPropertyName("read_only_probe")]             public bool ReadOnlyProbe           { get; set; }
    // Output
    [JsonPropertyName("output_artifacts")]  public List<string>                       OutputArtifacts  { get; set; } = new();
    [JsonPropertyName("checks")]            public List<RecordClusterAuditCheck>      Checks           { get; set; } = new();
    [JsonPropertyName("errors")]            public List<string>                       Errors           { get; set; } = new();
    [JsonPropertyName("check_count")]       public int  CheckCount       { get; set; }
    [JsonPropertyName("passed_check_count")] public int PassedCheckCount { get; set; }
    [JsonPropertyName("failed_check_count")] public int FailedCheckCount { get; set; }
    [JsonPropertyName("is_valid")]          public bool IsValid          { get; set; }
    [JsonPropertyName("verdict")]           public string Verdict        { get; set; } = string.Empty;
}

public sealed class RecordClusterHeaderSizeRecord
{
    [JsonPropertyName("header_size")]          public int  HeaderSize        { get; set; }
    [JsonPropertyName("record_width")]         public int  RecordWidth       { get; set; }
    [JsonPropertyName("minimal_payload_size")] public long MinimalPayloadSize { get; set; }
    [JsonPropertyName("visible_payload_size")] public long VisiblePayloadSize { get; set; }
    [JsonPropertyName("minimal_full_record_count")] public long MinimalFullRecordCount { get; set; }
    [JsonPropertyName("visible_full_record_count")] public long VisibleFullRecordCount { get; set; }
    [JsonPropertyName("minimal_remainder")]    public long MinimalRemainder  { get; set; }
    [JsonPropertyName("visible_remainder")]    public long VisibleRemainder  { get; set; }
    [JsonPropertyName("size_delta_payload")]   public long SizeDeltaPayload  { get; set; }
    [JsonPropertyName("delta_record_count_if_exact")] public long DeltaRecordCountIfExact { get; set; }
    [JsonPropertyName("exact_fit_score")]      public int  ExactFitScore     { get; set; }
    [JsonPropertyName("label")]                public string Label           { get; set; } = string.Empty;
}

public sealed class RecordWidthAnalysisRecord
{
    [JsonPropertyName("header_size")]           public int  HeaderSize          { get; set; }
    [JsonPropertyName("record_width")]          public int  RecordWidth         { get; set; }
    [JsonPropertyName("source")]                public string Source            { get; set; } = string.Empty;
    [JsonPropertyName("record_count")]          public long RecordCount         { get; set; }
    [JsonPropertyName("payload_remainder")]     public long PayloadRemainder    { get; set; }
    [JsonPropertyName("distinct_records")]      public int  DistinctRecords     { get; set; }
    [JsonPropertyName("repeated_records")]      public int  RepeatedRecords     { get; set; }
    [JsonPropertyName("zero_record_count")]     public int  ZeroRecordCount     { get; set; }
    [JsonPropertyName("all_ff_record_count")]   public int  AllFfRecordCount    { get; set; }
    [JsonPropertyName("most_common_record_count")] public int MostCommonRecordCount { get; set; }
    [JsonPropertyName("likely_record_like_score")] public string LikelyRecordLikeScore { get; set; } = string.Empty;
}

public sealed class RecordColumnStatRecord
{
    [JsonPropertyName("header_size")]      public int  HeaderSize    { get; set; }
    [JsonPropertyName("record_width")]     public int  RecordWidth   { get; set; }
    [JsonPropertyName("source")]           public string Source      { get; set; } = string.Empty;
    [JsonPropertyName("column_index")]     public int  ColumnIndex   { get; set; }
    [JsonPropertyName("min_value")]        public int  MinValue      { get; set; }
    [JsonPropertyName("max_value")]        public int  MaxValue      { get; set; }
    [JsonPropertyName("distinct_count")]   public int  DistinctCount { get; set; }
    [JsonPropertyName("zero_count")]       public long ZeroCount     { get; set; }
    [JsonPropertyName("ff_count")]         public long FfCount       { get; set; }
    [JsonPropertyName("most_common_byte")] public int  MostCommonByte { get; set; }
    [JsonPropertyName("most_common_count")] public long MostCommonCount { get; set; }
}

public sealed class RepeatedRecordClusterRecord
{
    [JsonPropertyName("source")]       public string Source    { get; set; } = string.Empty;
    [JsonPropertyName("hex_bytes")]    public string HexBytes  { get; set; } = string.Empty;
    [JsonPropertyName("count")]        public int    Count     { get; set; }
    [JsonPropertyName("first_offset")] public long   FirstOffset { get; set; }
}

public sealed class ZeroRecordClusterRecord
{
    [JsonPropertyName("source")]         public string Source      { get; set; } = string.Empty;
    [JsonPropertyName("start_record")]   public long   StartRecord { get; set; }
    [JsonPropertyName("end_record")]     public long   EndRecord   { get; set; }
    [JsonPropertyName("length_records")] public long   LengthRecords { get; set; }
    [JsonPropertyName("start_offset")]   public long   StartOffset { get; set; }
}

public sealed class BytePatternClusterRecord
{
    [JsonPropertyName("header_size")]  public int    HeaderSize  { get; set; }
    [JsonPropertyName("record_width")] public int    RecordWidth { get; set; }
    [JsonPropertyName("source")]       public string Source      { get; set; } = string.Empty;
    [JsonPropertyName("pattern")]      public string Pattern     { get; set; } = string.Empty;
    [JsonPropertyName("count")]        public int    Count       { get; set; }
}

public sealed class ChangedRecordWindowRecord
{
    [JsonPropertyName("start_record_index")]    public long   StartRecordIndex   { get; set; }
    [JsonPropertyName("end_record_index")]      public long   EndRecordIndex     { get; set; }
    [JsonPropertyName("length_records")]        public long   LengthRecords      { get; set; }
    [JsonPropertyName("minimal_start_offset")]  public long   MinimalStartOffset { get; set; }
    [JsonPropertyName("visible_start_offset")]  public long   VisibleStartOffset { get; set; }
    [JsonPropertyName("minimal_hex_preview")]   public string MinimalHexPreview  { get; set; } = string.Empty;
    [JsonPropertyName("visible_hex_preview")]   public string VisibleHexPreview  { get; set; } = string.Empty;
    [JsonPropertyName("window_type")]           public string WindowType         { get; set; } = string.Empty;
    [JsonPropertyName("label")]                 public string Label              { get; set; } = string.Empty;
}

public sealed class RecordClusterSampleRecord
{
    [JsonPropertyName("record_index")]  public long   RecordIndex { get; set; }
    [JsonPropertyName("file_offset")]   public long   FileOffset  { get; set; }
    [JsonPropertyName("record_size")]   public int    RecordSize  { get; set; }
    [JsonPropertyName("hex_bytes")]     public string HexBytes    { get; set; } = string.Empty;
    [JsonPropertyName("source")]        public string Source      { get; set; } = string.Empty;
    [JsonPropertyName("sample_group")]  public string SampleGroup { get; set; } = string.Empty;
}

public sealed class RecordClusterAuditCheck
{
    [JsonPropertyName("check_id")]     public string CheckId     { get; set; } = string.Empty;
    [JsonPropertyName("description")]  public string Description { get; set; } = string.Empty;
    [JsonPropertyName("expected")]     public string Expected    { get; set; } = string.Empty;
    [JsonPropertyName("actual")]       public string Actual      { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
}
