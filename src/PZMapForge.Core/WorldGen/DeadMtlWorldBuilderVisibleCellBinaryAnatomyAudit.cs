using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderVisibleCellBinaryAnatomyAuditResult
{
    // Identity
    [JsonPropertyName("format")]          public string Format       { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")]   public string GeneratedUtc { get; set; } = string.Empty;
    [JsonPropertyName("cell_coord")]      public string CellCoord    { get; set; } = "35_27";
    // Input paths
    [JsonPropertyName("map33a_seed_dir")]       public string Map33aSeedDir      { get; set; } = string.Empty;
    [JsonPropertyName("map35a_source_dir")]     public string Map35aSourceDir    { get; set; } = string.Empty;
    [JsonPropertyName("map35a_installed_dir")]  public string Map35aInstalledDir { get; set; } = string.Empty;
    [JsonPropertyName("map31b_emitter_json")]   public string Map31bEmitterJson  { get; set; } = string.Empty;
    // Input availability
    [JsonPropertyName("map33a_seed_dir_found")]      public bool Map33aSeedDirFound      { get; set; }
    [JsonPropertyName("map35a_source_dir_found")]    public bool Map35aSourceDirFound    { get; set; }
    [JsonPropertyName("map35a_installed_dir_found")] public bool Map35aInstalledDirFound { get; set; }
    [JsonPropertyName("map31b_emitter_json_found")]  public bool Map31bEmitterJsonFound  { get; set; }
    // Per-file binary anatomy
    [JsonPropertyName("lotheader_anatomy")]  public BinaryFileAnatomyRecord LotHeaderAnatomy  { get; set; } = new();
    [JsonPropertyName("chunkdata_anatomy")]  public BinaryFileAnatomyRecord ChunkdataAnatomy  { get; set; } = new();
    [JsonPropertyName("lotpack_anatomy")]    public BinaryFileAnatomyRecord LotpackAnatomy    { get; set; } = new();
    // Chunkdata special analysis
    [JsonPropertyName("chunkdata_special")]  public ChunkdataSpecialAnalysis ChunkdataSpecial { get; set; } = new();
    // MAP-31B cross-reference
    [JsonPropertyName("map31b_cross_ref")]   public Map31bCrossReferenceRecord Map31bCrossRef { get; set; } = new();
    // Claim boundary
    [JsonPropertyName("runtime_binary_written")]     public bool RuntimeBinaryWritten    { get; set; }
    [JsonPropertyName("geometry_injected")]          public bool GeometryInjected        { get; set; }
    [JsonPropertyName("playable_export_claimed")]    public bool PlayableExportClaimed   { get; set; }
    [JsonPropertyName("workshop_upload_performed")]  public bool WorkshopUploadPerformed { get; set; }
    [JsonPropertyName("steam_install_write")]        public bool SteamInstallWrite       { get; set; }
    // Result
    [JsonPropertyName("check_count")]        public int    CheckCount       { get; set; }
    [JsonPropertyName("passed_check_count")] public int    PassedCheckCount { get; set; }
    [JsonPropertyName("failed_check_count")] public int    FailedCheckCount { get; set; }
    [JsonPropertyName("is_valid")]           public bool   IsValid          { get; set; }
    [JsonPropertyName("verdict")]            public string Verdict          { get; set; } = string.Empty;
    [JsonPropertyName("checks")]             public List<BinaryAnatomyAuditCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")]             public List<string> Errors     { get; set; } = new();
}

public sealed class BinaryFileAnatomyRecord
{
    [JsonPropertyName("file_name")]               public string FileName             { get; set; } = string.Empty;
    [JsonPropertyName("minimal_size")]            public long   MinimalSize          { get; set; }
    [JsonPropertyName("visible_size")]            public long   VisibleSize          { get; set; }
    [JsonPropertyName("installed_size")]          public long   InstalledSize        { get; set; }
    [JsonPropertyName("size_delta")]              public long   SizeDelta            { get; set; }
    [JsonPropertyName("minimal_sha256")]          public string MinimalSha256        { get; set; } = string.Empty;
    [JsonPropertyName("visible_sha256")]          public string VisibleSha256        { get; set; } = string.Empty;
    [JsonPropertyName("installed_sha256")]        public string InstalledSha256      { get; set; } = string.Empty;
    [JsonPropertyName("minimal_prefix_hex")]      public string MinimalPrefixHex     { get; set; } = string.Empty;
    [JsonPropertyName("visible_prefix_hex")]      public string VisiblePrefixHex     { get; set; } = string.Empty;
    [JsonPropertyName("minimal_suffix_hex")]      public string MinimalSuffixHex     { get; set; } = string.Empty;
    [JsonPropertyName("visible_suffix_hex")]      public string VisibleSuffixHex     { get; set; } = string.Empty;
    [JsonPropertyName("first_differing_byte_offset")]  public long FirstDifferingByteOffset { get; set; } = -1;
    [JsonPropertyName("total_differing_bytes")]        public long TotalDifferingBytes      { get; set; }
    [JsonPropertyName("common_prefix_length")]         public long CommonPrefixLength       { get; set; }
    [JsonPropertyName("common_suffix_length")]         public long CommonSuffixLength       { get; set; }
    [JsonPropertyName("minimal_entropy")]         public double MinimalEntropy       { get; set; }
    [JsonPropertyName("visible_entropy")]         public double VisibleEntropy       { get; set; }
    [JsonPropertyName("minimal_printable_strings")]  public string MinimalPrintableStrings { get; set; } = string.Empty;
    [JsonPropertyName("visible_printable_strings")]  public string VisiblePrintableStrings { get; set; } = string.Empty;
    [JsonPropertyName("header_observation")]      public string HeaderObservation    { get; set; } = string.Empty;
    [JsonPropertyName("size_delta_observation")]  public string SizeDeltaObservation { get; set; } = string.Empty;
}

public sealed class ChunkdataSpecialAnalysis
{
    [JsonPropertyName("minimal_size")]                                public long   MinimalSize            { get; set; }
    [JsonPropertyName("visible_size")]                                public long   VisibleSize            { get; set; }
    [JsonPropertyName("size_delta")]                                  public long   SizeDelta              { get; set; }
    [JsonPropertyName("size_ratio")]                                  public double SizeRatio              { get; set; }
    [JsonPropertyName("record_count_guess_if_fixed_32_byte_records")] public int   RecordCountGuessFixed32 { get; set; }
    [JsonPropertyName("record_count_guess_if_fixed_8_byte_records")]  public int   RecordCountGuessFixed8  { get; set; }
    [JsonPropertyName("record_count_guess_label")]                    public string RecordCountGuessLabel  { get; set; } = "GUESS_NOT_VERIFIED";
}

public sealed class Map31bCrossReferenceRecord
{
    [JsonPropertyName("emitter_json_path")]                 public string EmitterJsonPath              { get; set; } = string.Empty;
    [JsonPropertyName("emitter_json_found")]                public bool   EmitterJsonFound             { get; set; }
    [JsonPropertyName("backend_dry_run_emission_digest")]   public string BackendDryRunEmissionDigest  { get; set; } = string.Empty;
    [JsonPropertyName("emitted_operation_count")]           public int    EmittedOperationCount        { get; set; }
    [JsonPropertyName("emitted_total_planned_cell_count")]  public int    EmittedTotalPlannedCellCount { get; set; }
    [JsonPropertyName("emits_binary_file")]                 public bool   EmitsBinaryFile              { get; set; }
    [JsonPropertyName("runtime_consumable")]                public bool   RuntimeConsumable            { get; set; }
    [JsonPropertyName("sandbox_only")]                      public bool   SandboxOnly                  { get; set; }
    [JsonPropertyName("map31b_geometry_to_binary_gap")]     public string Map31bGeometryToBinaryGap    { get; set; } = string.Empty;
}

public sealed class BinaryAnatomyAuditCheck
{
    [JsonPropertyName("check_id")]     public string CheckId     { get; set; } = string.Empty;
    [JsonPropertyName("description")]  public string Description { get; set; } = string.Empty;
    [JsonPropertyName("expected")]     public string Expected    { get; set; } = string.Empty;
    [JsonPropertyName("actual")]       public string Actual      { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
}
