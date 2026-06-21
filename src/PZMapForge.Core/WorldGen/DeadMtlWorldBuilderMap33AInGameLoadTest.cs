using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMap33AInGameLoadTestResult
{
    [JsonPropertyName("format")]                   public string Format                 { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")]            public string GeneratedUtc           { get; set; } = string.Empty;
    [JsonPropertyName("source_map33a_manifest")]   public string SourceMap33AManifest   { get; set; } = string.Empty;
    [JsonPropertyName("source_map33a_candidate_root")] public string SourceMap33ACandidateRoot { get; set; } = string.Empty;
    [JsonPropertyName("local_user_mods_root")]     public string LocalUserModsRoot      { get; set; } = string.Empty;
    [JsonPropertyName("installed_candidate_root")] public string InstalledCandidateRoot { get; set; } = string.Empty;
    [JsonPropertyName("map_id")]                   public string MapId                  { get; set; } = string.Empty;
    [JsonPropertyName("map_name")]                 public string MapName                { get; set; } = string.Empty;
    [JsonPropertyName("binary_cell_materialized")]          public bool BinaryCellMaterialized         { get; set; }
    [JsonPropertyName("geometry_from_map31b_materialized")] public bool GeometryFromMap31bMaterialized { get; set; }
    [JsonPropertyName("install_performed")]         public bool InstallPerformed         { get; set; }
    [JsonPropertyName("install_marker_written")]    public bool InstallMarkerWritten     { get; set; }
    [JsonPropertyName("runtime_log_collection_attempted")] public bool RuntimeLogCollectionAttempted { get; set; }
    [JsonPropertyName("runtime_logs_found")]        public bool RuntimeLogsFound         { get; set; }
    [JsonPropertyName("runtime_log_paths")]         public List<string> RuntimeLogPaths  { get; set; } = new();
    // MAP-34B log analysis fields
    [JsonPropertyName("candidate_mod_loaded")]                  public bool CandidateModLoaded                { get; set; }
    [JsonPropertyName("candidate_binary_files_mounted")]        public bool CandidateBinaryFilesMounted       { get; set; }
    [JsonPropertyName("candidate_mapgroup_registered")]         public bool CandidateMapgroupRegistered       { get; set; }
    [JsonPropertyName("candidate_spawn_blocker_absent")]        public bool CandidateSpawnBlockerAbsent       { get; set; }
    [JsonPropertyName("candidate_binary_chunk_load_attempted")] public bool CandidateBinaryChunkLoadAttempted { get; set; }
    [JsonPropertyName("candidate_specific_errors_found")]       public bool CandidateSpecificErrorsFound      { get; set; }
    [JsonPropertyName("unrelated_errors_found")]                public bool UnrelatedErrorsFound              { get; set; }
    [JsonPropertyName("fallback_empty_terrain_detected")]       public bool FallbackEmptyTerrainDetected      { get; set; }
    [JsonPropertyName("operator_observation")]                  public string OperatorObservation             { get; set; } = string.Empty;
    [JsonPropertyName("runtime_classification")]    public string RuntimeClassification  { get; set; } = string.Empty;
    [JsonPropertyName("runtime_valid")]             public bool RuntimeValid             { get; set; }
    [JsonPropertyName("runtime_proof_claimed")]     public bool RuntimeProofClaimed      { get; set; }
    [JsonPropertyName("playable_export_claimed")]   public bool PlayableExportClaimed    { get; set; }
    [JsonPropertyName("public_package_claimed")]    public bool PublicPackageClaimed     { get; set; }
    [JsonPropertyName("map33a_required_binary_files")] public List<string> Map33ARequiredBinaryFiles { get; set; } = new();
    [JsonPropertyName("check_count")]          public int  CheckCount        { get; set; }
    [JsonPropertyName("passed_check_count")]   public int  PassedCheckCount  { get; set; }
    [JsonPropertyName("failed_check_count")]   public int  FailedCheckCount  { get; set; }
    [JsonPropertyName("is_valid")]             public bool IsValid           { get; set; }
    [JsonPropertyName("verdict")]              public string Verdict         { get; set; } = string.Empty;
    [JsonPropertyName("checks")]               public List<LoadTestCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")]               public List<string> Errors    { get; set; } = new();
}

public sealed class LoadTestCheck
{
    [JsonPropertyName("check_id")]     public string CheckId     { get; set; } = string.Empty;
    [JsonPropertyName("description")]  public string Description { get; set; } = string.Empty;
    [JsonPropertyName("expected")]     public string Expected    { get; set; } = string.Empty;
    [JsonPropertyName("actual")]       public string Actual      { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
}
