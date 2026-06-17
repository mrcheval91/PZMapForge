using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterExperimentScopeRecordResult
{
    [JsonPropertyName("format")]                           public string Format                          { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")]                    public string GeneratedUtc                    { get; set; } = string.Empty;
    [JsonPropertyName("map_id")]                           public string MapId                           { get; set; } = string.Empty;
    [JsonPropertyName("target_component_id")]              public string TargetComponentId               { get; set; } = string.Empty;
    [JsonPropertyName("source_manifest_path")]             public string SourceManifestPath              { get; set; } = string.Empty;
    [JsonPropertyName("source_manifest_sha256")]           public string SourceManifestSha256            { get; set; } = string.Empty;
    [JsonPropertyName("source_manifest_verdict")]          public string SourceManifestVerdict           { get; set; } = string.Empty;
    [JsonPropertyName("source_manifest_is_valid")]         public bool   SourceManifestIsValid           { get; set; }
    [JsonPropertyName("source_manifest_input_artifacts")]  public int    SourceManifestInputArtifacts    { get; set; }
    [JsonPropertyName("source_manifest_hashed_input_artifacts")] public int SourceManifestHashedInputArtifacts { get; set; }
    [JsonPropertyName("source_manifest_checks")]           public int    SourceManifestChecks            { get; set; }
    [JsonPropertyName("source_manifest_passed_checks")]    public int    SourceManifestPassedChecks      { get; set; }
    [JsonPropertyName("writer_ready")]                     public bool   WriterReady                     { get; set; }
    [JsonPropertyName("runtime_valid")]                    public bool   RuntimeValid                    { get; set; }
    [JsonPropertyName("materialized")]                     public bool   Materialized                    { get; set; }
    [JsonPropertyName("approved_for_writer_experiment")]   public bool   ApprovedForWriterExperiment     { get; set; }
    [JsonPropertyName("writer_experiment_gate_status")]    public string WriterExperimentGateStatus      { get; set; } = string.Empty;
    [JsonPropertyName("future_experiment_name")]           public string FutureExperimentName            { get; set; } = string.Empty;
    [JsonPropertyName("future_experiment_status")]         public string FutureExperimentStatus          { get; set; } = string.Empty;
    [JsonPropertyName("allowed_future_actions")]           public List<DeadMtlWorldBuilderWriterExperimentScopeAllowedAction>       AllowedFutureActions  { get; set; } = new();
    [JsonPropertyName("forbidden_actions")]                public List<DeadMtlWorldBuilderWriterExperimentScopeForbiddenAction>      ForbiddenActions      { get; set; } = new();
    [JsonPropertyName("required_preconditions")]           public List<DeadMtlWorldBuilderWriterExperimentScopeRequiredPrecondition> RequiredPreconditions { get; set; } = new();
    [JsonPropertyName("rollback_requirements")]            public List<DeadMtlWorldBuilderWriterExperimentScopeRollbackRequirement>  RollbackRequirements  { get; set; } = new();
    [JsonPropertyName("risk_register")]                    public List<DeadMtlWorldBuilderWriterExperimentScopeRisk>                 RiskRegister          { get; set; } = new();
    [JsonPropertyName("scope_check_count")]                public int    ScopeCheckCount                 { get; set; }
    [JsonPropertyName("passed_scope_check_count")]         public int    PassedScopeCheckCount           { get; set; }
    [JsonPropertyName("failed_scope_check_count")]         public int    FailedScopeCheckCount           { get; set; }
    [JsonPropertyName("checks")]                           public List<DeadMtlWorldBuilderWriterExperimentScopeCheck> Checks { get; set; } = new();
    [JsonPropertyName("is_valid")]                         public bool   IsValid                         { get; set; }
    [JsonPropertyName("verdict")]                          public string Verdict                         { get; set; } = string.Empty;
    [JsonPropertyName("errors")]                           public List<string> Errors                    { get; set; } = new();
}

public sealed class DeadMtlWorldBuilderWriterExperimentScopeAllowedAction
{
    [JsonPropertyName("action_order")] public int    ActionOrder { get; set; }
    [JsonPropertyName("action_id")]    public string ActionId    { get; set; } = string.Empty;
    [JsonPropertyName("action_label")] public string ActionLabel { get; set; } = string.Empty;
    [JsonPropertyName("status")]       public string Status      { get; set; } = string.Empty;
    [JsonPropertyName("details")]      public string Details     { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderWriterExperimentScopeForbiddenAction
{
    [JsonPropertyName("action_order")] public int    ActionOrder { get; set; }
    [JsonPropertyName("action_id")]    public string ActionId    { get; set; } = string.Empty;
    [JsonPropertyName("action_label")] public string ActionLabel { get; set; } = string.Empty;
    [JsonPropertyName("reason")]       public string Reason      { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderWriterExperimentScopeRequiredPrecondition
{
    [JsonPropertyName("precondition_order")] public int    PreconditionOrder { get; set; }
    [JsonPropertyName("precondition_id")]    public string PreconditionId    { get; set; } = string.Empty;
    [JsonPropertyName("precondition_label")] public string PreconditionLabel { get; set; } = string.Empty;
    [JsonPropertyName("status")]             public string Status            { get; set; } = string.Empty;
    [JsonPropertyName("details")]            public string Details           { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderWriterExperimentScopeRollbackRequirement
{
    [JsonPropertyName("requirement_order")] public int    RequirementOrder { get; set; }
    [JsonPropertyName("requirement_id")]    public string RequirementId    { get; set; } = string.Empty;
    [JsonPropertyName("requirement_label")] public string RequirementLabel { get; set; } = string.Empty;
    [JsonPropertyName("details")]           public string Details          { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderWriterExperimentScopeRisk
{
    [JsonPropertyName("risk_order")]  public int    RiskOrder  { get; set; }
    [JsonPropertyName("risk_id")]     public string RiskId     { get; set; } = string.Empty;
    [JsonPropertyName("risk_label")]  public string RiskLabel  { get; set; } = string.Empty;
    [JsonPropertyName("risk_level")]  public string RiskLevel  { get; set; } = string.Empty;
    [JsonPropertyName("mitigation")]  public string Mitigation { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderWriterExperimentScopeCheck
{
    [JsonPropertyName("check_order")]  public int    CheckOrder  { get; set; }
    [JsonPropertyName("check_id")]     public string CheckId     { get; set; } = string.Empty;
    [JsonPropertyName("check_label")]  public string CheckLabel  { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")]     public string Expected    { get; set; } = string.Empty;
    [JsonPropertyName("actual")]       public string Actual      { get; set; } = string.Empty;
    [JsonPropertyName("details")]      public string Details     { get; set; } = string.Empty;
}
