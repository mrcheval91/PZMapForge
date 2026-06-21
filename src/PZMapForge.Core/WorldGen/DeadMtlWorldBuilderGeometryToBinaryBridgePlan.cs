using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderGeometryToBinaryBridgePlanResult
{
    [JsonPropertyName("format")]         public string Format       { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")]  public string GeneratedUtc { get; set; } = string.Empty;
    // Input paths
    [JsonPropertyName("map36a1_audit_json")]   public string Map36a1AuditJson   { get; set; } = string.Empty;
    [JsonPropertyName("map31b_emitter_json")]  public string Map31bEmitterJson  { get; set; } = string.Empty;
    [JsonPropertyName("output_root")]          public string OutputRoot         { get; set; } = string.Empty;
    // Input availability
    [JsonPropertyName("audit_output_found")]   public bool AuditOutputFound   { get; set; }
    [JsonPropertyName("emitter_output_found")] public bool EmitterOutputFound { get; set; }
    // Source rejection
    [JsonPropertyName("source_rejected")]          public bool   SourceRejected        { get; set; }
    [JsonPropertyName("source_rejection_reason")]  public string SourceRejectionReason { get; set; } = string.Empty;
    // Emitter facts
    [JsonPropertyName("emits_binary_file")]  public bool EmitsBinaryFile { get; set; }
    [JsonPropertyName("sandbox_only")]       public bool SandboxOnly     { get; set; } = true;
    // Bridge plan content
    [JsonPropertyName("required_unknown_fields")]     public List<BridgePlanUnknownField> RequiredUnknownFields    { get; set; } = new();
    [JsonPropertyName("candidate_experiments")]        public List<BridgePlanExperiment>   CandidateExperiments     { get; set; } = new();
    [JsonPropertyName("required_unknown_count")]       public int                          RequiredUnknownCount     { get; set; }
    [JsonPropertyName("candidate_experiment_count")]   public int                          CandidateExperimentCount { get; set; }
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
    [JsonPropertyName("checks")]             public List<BridgePlanCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")]             public List<string>          Errors { get; set; } = new();
}

public sealed class BridgePlanUnknownField
{
    [JsonPropertyName("id")]           public string Id          { get; set; } = string.Empty;
    [JsonPropertyName("description")]  public string Description { get; set; } = string.Empty;
    [JsonPropertyName("file")]         public string File        { get; set; } = string.Empty;
    [JsonPropertyName("severity")]     public string Severity    { get; set; } = string.Empty;
}

public sealed class BridgePlanExperiment
{
    [JsonPropertyName("id")]           public string Id          { get; set; } = string.Empty;
    [JsonPropertyName("description")]  public string Description { get; set; } = string.Empty;
    [JsonPropertyName("target_file")]  public string TargetFile  { get; set; } = string.Empty;
    [JsonPropertyName("read_only")]    public bool   ReadOnly    { get; set; } = true;
}

public sealed class BridgePlanCheck
{
    [JsonPropertyName("check_id")]     public string CheckId     { get; set; } = string.Empty;
    [JsonPropertyName("description")]  public string Description { get; set; } = string.Empty;
    [JsonPropertyName("expected")]     public string Expected    { get; set; } = string.Empty;
    [JsonPropertyName("actual")]       public string Actual      { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
}
