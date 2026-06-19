using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderResidentialBuildingFootprintPlanResult
{
    [JsonPropertyName("format")]        public string Format { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")] public string GeneratedUtc { get; set; } = string.Empty;
    [JsonPropertyName("map_id")]        public string MapId { get; set; } = string.Empty;
    [JsonPropertyName("component_id")]  public string ComponentId { get; set; } = string.Empty;

    [JsonPropertyName("plan_stage")]  public string PlanStage { get; set; } = string.Empty;
    [JsonPropertyName("plan_status")] public string PlanStatus { get; set; } = string.Empty;

    [JsonPropertyName("source_parcel_topology_component_id")] public string SourceParcelTopologyComponentId { get; set; } = string.Empty;
    [JsonPropertyName("source_parcel_topology_valid")]        public bool   SourceParcelTopologyValid { get; set; }
    [JsonPropertyName("source_parcel_count")]                 public int    SourceParcelCount { get; set; }

    [JsonPropertyName("total_footprint_count")] public int TotalFootprintCount { get; set; }
    [JsonPropertyName("north_footprint_count")] public int NorthFootprintCount { get; set; }
    [JsonPropertyName("south_footprint_count")] public int SouthFootprintCount { get; set; }
    [JsonPropertyName("east_footprint_count")]  public int EastFootprintCount { get; set; }

    [JsonPropertyName("building_footprints")] public List<DeadMtlResidentialBuildingFootprint> BuildingFootprints { get; set; } = new();

    [JsonPropertyName("sandbox_only")]                      public bool SandboxOnly { get; set; } = true;
    [JsonPropertyName("building_footprint_planning_only")]  public bool BuildingFootprintPlanningOnly { get; set; } = true;
    [JsonPropertyName("writer_ready")]                      public bool WriterReady { get; set; }
    [JsonPropertyName("runtime_valid")]                     public bool RuntimeValid { get; set; }
    [JsonPropertyName("materialized")]                      public bool Materialized { get; set; }
    [JsonPropertyName("pz_runtime_materialized")]           public bool PzRuntimeMaterialized { get; set; }
    [JsonPropertyName("runtime_proof_claimed")]             public bool RuntimeProofClaimed { get; set; }
    [JsonPropertyName("public_playable_packaging_claimed")] public bool PublicPlayablePackagingClaimed { get; set; }

    [JsonPropertyName("forbidden_artifact_scan")] public string ForbiddenArtifactScan { get; set; } = string.Empty;

    [JsonPropertyName("check_count")]        public int  CheckCount { get; set; }
    [JsonPropertyName("passed_check_count")] public int  PassedCheckCount { get; set; }
    [JsonPropertyName("failed_check_count")] public int  FailedCheckCount { get; set; }
    [JsonPropertyName("is_valid")]           public bool IsValid { get; set; }
    [JsonPropertyName("verdict")]            public string Verdict { get; set; } = string.Empty;
    [JsonPropertyName("checks")] public List<DeadMtlResidentialBuildingFootprintCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")] public List<string> Errors { get; set; } = new();
}

public sealed class DeadMtlResidentialBuildingFootprint
{
    [JsonPropertyName("footprint_id")]        public string FootprintId { get; set; } = string.Empty;
    [JsonPropertyName("parent_parcel_id")]    public string ParentParcelId { get; set; } = string.Empty;
    [JsonPropertyName("frontage_direction")]  public string FrontageDirection { get; set; } = string.Empty;
    [JsonPropertyName("building_kind")]       public string BuildingKind { get; set; } = string.Empty;
    [JsonPropertyName("x1")]                 public int X1 { get; set; }
    [JsonPropertyName("y1")]                 public int Y1 { get; set; }
    [JsonPropertyName("x2")]                 public int X2 { get; set; }
    [JsonPropertyName("y2")]                 public int Y2 { get; set; }
    [JsonPropertyName("width")]              public int Width { get; set; }
    [JsonPropertyName("height")]             public int Height { get; set; }
    [JsonPropertyName("tile_count")]         public int TileCount { get; set; }
    [JsonPropertyName("setback_front")]      public int SetbackFront { get; set; }
    [JsonPropertyName("setback_rear")]       public int SetbackRear { get; set; }
    [JsonPropertyName("setback_side_left")]  public int SetbackSideLeft { get; set; }
    [JsonPropertyName("setback_side_right")] public int SetbackSideRight { get; set; }
    [JsonPropertyName("writer_ready")]       public bool WriterReady { get; set; }
    [JsonPropertyName("runtime_valid")]      public bool RuntimeValid { get; set; }
    [JsonPropertyName("materialized")]       public bool Materialized { get; set; }
}

public sealed class DeadMtlResidentialBuildingFootprintCheck
{
    [JsonPropertyName("check_order")]  public int    CheckOrder { get; set; }
    [JsonPropertyName("check_id")]     public string CheckId { get; set; } = string.Empty;
    [JsonPropertyName("check_label")]  public string CheckLabel { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")]     public string Expected { get; set; } = string.Empty;
    [JsonPropertyName("actual")]       public string Actual { get; set; } = string.Empty;
}
