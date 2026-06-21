using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderLotheaderHexDisassemblyResult
{
    [JsonPropertyName("format")]         public string Format       { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")]  public string GeneratedUtc { get; set; } = string.Empty;
    // Input paths
    [JsonPropertyName("minimal_lotheader_path")]  public string MinimalLotheaderPath { get; set; } = string.Empty;
    [JsonPropertyName("visible_lotheader_path")]  public string VisibleLotheaderPath { get; set; } = string.Empty;
    [JsonPropertyName("output_root")]             public string OutputRoot           { get; set; } = string.Empty;
    // Input availability
    [JsonPropertyName("minimal_lotheader_found")]  public bool MinimalLotheaderFound { get; set; }
    [JsonPropertyName("visible_lotheader_found")]  public bool VisibleLotheaderFound { get; set; }
    // File sizes
    [JsonPropertyName("minimal_size")]  public long MinimalSize { get; set; }
    [JsonPropertyName("visible_size")]  public long VisibleSize { get; set; }
    [JsonPropertyName("size_delta")]    public long SizeDelta   { get; set; }
    // SHA256
    [JsonPropertyName("minimal_sha256")]  public string MinimalSha256 { get; set; } = string.Empty;
    [JsonPropertyName("visible_sha256")]  public string VisibleSha256 { get; set; } = string.Empty;
    [JsonPropertyName("sha256_computed")] public bool   Sha256Computed { get; set; }
    // Structural analysis
    [JsonPropertyName("common_prefix_length")]  public long CommonPrefixLength { get; set; }
    [JsonPropertyName("common_suffix_length")]  public long CommonSuffixLength { get; set; }
    [JsonPropertyName("diff_runs")]              public List<DiffRunRecord>          DiffRuns          { get; set; } = new();
    [JsonPropertyName("candidate_regions")]      public List<CandidateRegionRecord>  CandidateRegions  { get; set; } = new();
    [JsonPropertyName("hexdump_sections")]        public List<HexdumpSection>         HexdumpSections   { get; set; } = new();
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
    [JsonPropertyName("checks")]             public List<LotheaderHexDisassemblyCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")]             public List<string>                       Errors { get; set; } = new();
}

public sealed class DiffRunRecord
{
    [JsonPropertyName("run_id")]             public string RunId             { get; set; } = string.Empty;
    [JsonPropertyName("run_type")]           public string RunType           { get; set; } = string.Empty;
    [JsonPropertyName("start_offset")]       public long   StartOffset       { get; set; }
    [JsonPropertyName("end_offset")]         public long   EndOffset         { get; set; }
    [JsonPropertyName("length")]             public long   Length            { get; set; }
    [JsonPropertyName("minimal_hex_preview")] public string MinimalHexPreview { get; set; } = string.Empty;
    [JsonPropertyName("visible_hex_preview")] public string VisibleHexPreview { get; set; } = string.Empty;
}

public sealed class CandidateRegionRecord
{
    [JsonPropertyName("region_id")]       public string RegionId       { get; set; } = string.Empty;
    [JsonPropertyName("label")]           public string Label          { get; set; } = string.Empty;
    [JsonPropertyName("start_offset")]    public long   StartOffset    { get; set; }
    [JsonPropertyName("end_offset")]      public long   EndOffset      { get; set; }
    [JsonPropertyName("length")]          public long   Length         { get; set; }
    [JsonPropertyName("interpretation")]  public string Interpretation { get; set; } = string.Empty;
}

public sealed class HexdumpSection
{
    [JsonPropertyName("section_id")]             public string SectionId            { get; set; } = string.Empty;
    [JsonPropertyName("label")]                  public string Label                { get; set; } = string.Empty;
    [JsonPropertyName("minimal_start_offset")]   public long   MinimalStartOffset   { get; set; } = -1;
    [JsonPropertyName("visible_start_offset")]   public long   VisibleStartOffset   { get; set; } = -1;
    [JsonPropertyName("byte_count")]             public int    ByteCount            { get; set; }
    [JsonPropertyName("minimal_hexdump")]        public string MinimalHexdump       { get; set; } = string.Empty;
    [JsonPropertyName("visible_hexdump")]        public string VisibleHexdump       { get; set; } = string.Empty;
    [JsonPropertyName("note")]                   public string Note                 { get; set; } = string.Empty;
}

public sealed class LotheaderHexDisassemblyCheck
{
    [JsonPropertyName("check_id")]     public string CheckId     { get; set; } = string.Empty;
    [JsonPropertyName("description")]  public string Description { get; set; } = string.Empty;
    [JsonPropertyName("expected")]     public string Expected    { get; set; } = string.Empty;
    [JsonPropertyName("actual")]       public string Actual      { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
}
