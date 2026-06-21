using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderVisibleCellRuntimeCandidateResult
{
    // Identity
    [JsonPropertyName("format")]                   public string Format                 { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")]            public string GeneratedUtc           { get; set; } = string.Empty;
    [JsonPropertyName("map_id")]                   public string MapId                  { get; set; } = string.Empty;
    [JsonPropertyName("map_folder")]               public string MapFolder              { get; set; } = string.Empty;
    [JsonPropertyName("staged_candidate_root")]    public string StagedCandidateRoot    { get; set; } = string.Empty;
    [JsonPropertyName("installed_candidate_root")] public string InstalledCandidateRoot { get; set; } = string.Empty;
    // Source discovery
    [JsonPropertyName("primary_source_root")]      public string PrimarySourceRoot      { get; set; } = string.Empty;
    [JsonPropertyName("fallback_source_root")]     public string FallbackSourceRoot     { get; set; } = string.Empty;
    [JsonPropertyName("selected_source_root")]     public string SelectedSourceRoot     { get; set; } = string.Empty;
    [JsonPropertyName("selected_source_classification")] public string SelectedSourceClassification { get; set; } = string.Empty;
    [JsonPropertyName("rejected_sources")]         public List<RejectedSourceEntry> RejectedSources { get; set; } = new();
    [JsonPropertyName("source_cell_x")]            public int SourceCellX              { get; set; } = 35;
    [JsonPropertyName("source_cell_y")]            public int SourceCellY              { get; set; } = 27;
    // Source file metadata
    [JsonPropertyName("source_binary_file_sizes")]  public Dictionary<string, long>   SourceBinaryFileSizes  { get; set; } = new();
    [JsonPropertyName("source_binary_file_sha256")] public Dictionary<string, string> SourceBinaryFileSha256 { get; set; } = new();
    // Staging output
    [JsonPropertyName("required_binary_files_written")]  public List<string> RequiredBinaryFilesWritten  { get; set; } = new();
    [JsonPropertyName("optional_sidecar_files_written")] public List<string> OptionalSidecarFilesWritten { get; set; } = new();
    [JsonPropertyName("required_binary_file_sizes")]     public Dictionary<string, long>   RequiredBinaryFileSizes  { get; set; } = new();
    [JsonPropertyName("required_binary_file_sha256")]    public Dictionary<string, string> RequiredBinaryFileSha256 { get; set; } = new();
    [JsonPropertyName("b42_layout_written")]       public bool B42LayoutWritten         { get; set; }
    [JsonPropertyName("duplicate_map_entries_possible")] public bool DuplicateMapEntriesPossible { get; set; }
    // Size comparison
    [JsonPropertyName("source_size_advantage_over_map33a")] public bool SourceSizeAdvantageOverMap33a { get; set; }
    [JsonPropertyName("map33a_minimal_seed_sizes")]          public Dictionary<string, long> Map33aMinimalSeedSizes { get; set; } = new();
    // Lifecycle state
    [JsonPropertyName("stage_performed")]           public bool StagePerformed          { get; set; }
    [JsonPropertyName("install_performed")]         public bool InstallPerformed        { get; set; }
    [JsonPropertyName("install_marker_written")]    public bool InstallMarkerWritten    { get; set; }
    [JsonPropertyName("runtime_log_collection_attempted")] public bool RuntimeLogCollectionAttempted { get; set; }
    [JsonPropertyName("runtime_logs_found")]        public bool RuntimeLogsFound        { get; set; }
    [JsonPropertyName("runtime_log_paths")]         public List<string> RuntimeLogPaths { get; set; } = new();
    // MAP-35A log analysis
    [JsonPropertyName("candidate_mod_loaded")]                  public bool CandidateModLoaded                { get; set; }
    [JsonPropertyName("candidate_binary_files_mounted")]        public bool CandidateBinaryFilesMounted       { get; set; }
    [JsonPropertyName("candidate_mapgroup_registered")]         public bool CandidateMapgroupRegistered       { get; set; }
    [JsonPropertyName("candidate_spawn_blocker_absent")]        public bool CandidateSpawnBlockerAbsent       { get; set; }
    [JsonPropertyName("candidate_binary_chunk_load_attempted")] public bool CandidateBinaryChunkLoadAttempted { get; set; }
    [JsonPropertyName("candidate_specific_errors_found")]       public bool CandidateSpecificErrorsFound      { get; set; }
    [JsonPropertyName("unrelated_errors_found")]                public bool UnrelatedErrorsFound              { get; set; }
    [JsonPropertyName("visible_terrain_detected")]              public bool VisibleTerrainDetected            { get; set; }
    [JsonPropertyName("fallback_empty_terrain_detected")]       public bool FallbackEmptyTerrainDetected      { get; set; }
    [JsonPropertyName("operator_observation")]                  public string OperatorObservation             { get; set; } = string.Empty;
    [JsonPropertyName("runtime_classification")]    public string RuntimeClassification { get; set; } = string.Empty;
    // Claim boundary
    [JsonPropertyName("binary_cell_materialized")]           public bool BinaryCellMaterialized         { get; set; }
    [JsonPropertyName("visible_cell_candidate")]             public bool VisibleCellCandidate           { get; set; }
    [JsonPropertyName("geometry_from_map31b_materialized")]  public bool GeometryFromMap31bMaterialized { get; set; }
    [JsonPropertyName("runtime_proof_claimed")]              public bool RuntimeProofClaimed            { get; set; }
    [JsonPropertyName("playable_export_claimed")]            public bool PlayableExportClaimed          { get; set; }
    [JsonPropertyName("public_package_claimed")]             public bool PublicPackageClaimed           { get; set; }
    [JsonPropertyName("workshop_upload_performed")]          public bool WorkshopUploadPerformed        { get; set; }
    [JsonPropertyName("steam_install_write")]                public bool SteamInstallWrite              { get; set; }
    [JsonPropertyName("local_user_mod_install_allowed")]     public bool LocalUserModInstallAllowed     { get; set; }
    [JsonPropertyName("claim_boundary")]             public string ClaimBoundary        { get; set; } = string.Empty;
    // Result
    [JsonPropertyName("check_count")]          public int  CheckCount        { get; set; }
    [JsonPropertyName("passed_check_count")]   public int  PassedCheckCount  { get; set; }
    [JsonPropertyName("failed_check_count")]   public int  FailedCheckCount  { get; set; }
    [JsonPropertyName("is_valid")]             public bool IsValid           { get; set; }
    [JsonPropertyName("verdict")]              public string Verdict         { get; set; } = string.Empty;
    [JsonPropertyName("checks")]               public List<VisibleCellCandidateCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")]               public List<string> Errors    { get; set; } = new();
}

public sealed class RejectedSourceEntry
{
    [JsonPropertyName("source_root")] public string SourceRoot { get; set; } = string.Empty;
    [JsonPropertyName("reason")]      public string Reason     { get; set; } = string.Empty;
}

public sealed class VisibleCellCandidateCheck
{
    [JsonPropertyName("check_id")]     public string CheckId     { get; set; } = string.Empty;
    [JsonPropertyName("description")]  public string Description { get; set; } = string.Empty;
    [JsonPropertyName("expected")]     public string Expected    { get; set; } = string.Empty;
    [JsonPropertyName("actual")]       public string Actual      { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
}
