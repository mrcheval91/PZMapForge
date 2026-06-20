using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderResidentialParcelTopologyResult
{
    [JsonPropertyName("format")]        public string Format { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")] public string GeneratedUtc { get; set; } = string.Empty;
    [JsonPropertyName("map_id")]        public string MapId { get; set; } = string.Empty;
    [JsonPropertyName("component_id")]  public string ComponentId { get; set; } = string.Empty;

    [JsonPropertyName("topology_stage")]  public string TopologyStage { get; set; } = string.Empty;
    [JsonPropertyName("topology_status")] public string TopologyStatus { get; set; } = string.Empty;

    [JsonPropertyName("bbox_x1")]     public int BboxX1 { get; set; }
    [JsonPropertyName("bbox_y1")]     public int BboxY1 { get; set; }
    [JsonPropertyName("bbox_x2")]     public int BboxX2 { get; set; }
    [JsonPropertyName("bbox_y2")]     public int BboxY2 { get; set; }
    [JsonPropertyName("bbox_width")]  public int BboxWidth { get; set; }
    [JsonPropertyName("bbox_height")] public int BboxHeight { get; set; }

    [JsonPropertyName("sidewalk_width_north")]        public int  SidewalkWidthNorth { get; set; }
    [JsonPropertyName("sidewalk_width_south")]        public int  SidewalkWidthSouth { get; set; }
    [JsonPropertyName("sidewalk_width_east")]         public int  SidewalkWidthEast { get; set; }
    [JsonPropertyName("rear_fence_width")]            public int  RearFenceWidth { get; set; }
    [JsonPropertyName("min_residential_frontage_tiles")] public int MinResidentialFrontageTiles { get; set; }
    [JsonPropertyName("through_lots_enabled")]        public bool ThroughLotsEnabled { get; set; }
    [JsonPropertyName("invented_alleys_enabled")]     public bool InventedAlleysEnabled { get; set; }

    [JsonPropertyName("total_residential_lot_count")] public int TotalResidentialLotCount { get; set; }
    [JsonPropertyName("north_facing_lot_count")]      public int NorthFacingLotCount { get; set; }
    [JsonPropertyName("south_facing_lot_count")]      public int SouthFacingLotCount { get; set; }
    [JsonPropertyName("east_facing_lot_count")]       public int EastFacingLotCount { get; set; }
    [JsonPropertyName("through_lot_count")]           public int ThroughLotCount { get; set; }
    [JsonPropertyName("sidewalk_strip_count")]        public int SidewalkStripCount { get; set; }
    [JsonPropertyName("rear_boundary_strip_count")]   public int RearBoundaryStripCount { get; set; }
    [JsonPropertyName("invented_alley_count")]        public int InventedAlleyCount { get; set; }

    [JsonPropertyName("residential_parcels")] public List<DeadMtlResidentialParcel>        ResidentialParcels { get; set; } = new();
    [JsonPropertyName("frontage_edges")]      public List<DeadMtlResidentialFrontageEdge>  FrontageEdges { get; set; } = new();
    [JsonPropertyName("sidewalk_strips")]     public List<DeadMtlResidentialSidewalkStrip> SidewalkStrips { get; set; } = new();

    [JsonPropertyName("north_street_adjacency")] public bool NorthStreetAdjacency { get; set; }
    [JsonPropertyName("south_street_adjacency")] public bool SouthStreetAdjacency { get; set; }
    [JsonPropertyName("east_street_adjacency")]  public bool EastStreetAdjacency { get; set; }

    [JsonPropertyName("sandbox_only")]                       public bool SandboxOnly { get; set; } = true;
    [JsonPropertyName("parcel_topology_planning_only")]      public bool ParcelTopologyPlanningOnly { get; set; } = true;
    [JsonPropertyName("writer_ready")]                       public bool WriterReady { get; set; }
    [JsonPropertyName("runtime_valid")]                      public bool RuntimeValid { get; set; }
    [JsonPropertyName("materialized")]                       public bool Materialized { get; set; }
    [JsonPropertyName("pz_runtime_materialized")]            public bool PzRuntimeMaterialized { get; set; }
    [JsonPropertyName("runtime_proof_claimed")]              public bool RuntimeProofClaimed { get; set; }
    [JsonPropertyName("public_playable_packaging_claimed")]  public bool PublicPlayablePackagingClaimed { get; set; }

    [JsonPropertyName("forbidden_artifact_scan")] public string ForbiddenArtifactScan { get; set; } = string.Empty;

    [JsonPropertyName("check_count")]        public int  CheckCount { get; set; }
    [JsonPropertyName("passed_check_count")] public int  PassedCheckCount { get; set; }
    [JsonPropertyName("failed_check_count")] public int  FailedCheckCount { get; set; }
    [JsonPropertyName("is_valid")]           public bool IsValid { get; set; }
    [JsonPropertyName("verdict")]            public string Verdict { get; set; } = string.Empty;
    [JsonPropertyName("checks")]             public List<DeadMtlResidentialParcelTopologyCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")]             public List<string> Errors { get; set; } = new();
}

public sealed class DeadMtlResidentialParcel
{
    [JsonPropertyName("parcel_id")]             public string ParcelId { get; set; } = string.Empty;
    [JsonPropertyName("component_id")]          public string ComponentId { get; set; } = string.Empty;
    [JsonPropertyName("parcel_kind")]           public string ParcelKind { get; set; } = "RESIDENTIAL_LOT";
    [JsonPropertyName("frontage_direction")]    public string FrontageDirection { get; set; } = string.Empty;
    [JsonPropertyName("x1")]                   public int X1 { get; set; }
    [JsonPropertyName("y1")]                   public int Y1 { get; set; }
    [JsonPropertyName("x2")]                   public int X2 { get; set; }
    [JsonPropertyName("y2")]                   public int Y2 { get; set; }
    [JsonPropertyName("width")]                public int Width { get; set; }
    [JsonPropertyName("height")]               public int Height { get; set; }
    [JsonPropertyName("tile_count")]           public int TileCount { get; set; }
    [JsonPropertyName("is_corner_lot")]        public bool IsCornerLot { get; set; }
    [JsonPropertyName("is_through_lot")]       public bool IsThroughLot { get; set; }
    [JsonPropertyName("primary_frontage_edge_id")] public string PrimaryFrontageEdgeId { get; set; } = string.Empty;
    [JsonPropertyName("notes")]                public string Notes { get; set; } = string.Empty;
}

public sealed class DeadMtlResidentialFrontageEdge
{
    [JsonPropertyName("frontage_edge_id")]   public string FrontageEdgeId { get; set; } = string.Empty;
    [JsonPropertyName("parcel_id")]          public string ParcelId { get; set; } = string.Empty;
    [JsonPropertyName("frontage_direction")] public string FrontageDirection { get; set; } = string.Empty;
    [JsonPropertyName("edge_kind")]          public string EdgeKind { get; set; } = string.Empty;
    [JsonPropertyName("x1")]                public int X1 { get; set; }
    [JsonPropertyName("y1")]                public int Y1 { get; set; }
    [JsonPropertyName("x2")]                public int X2 { get; set; }
    [JsonPropertyName("y2")]                public int Y2 { get; set; }
    [JsonPropertyName("length_tiles")]       public int LengthTiles { get; set; }
}

public sealed class DeadMtlResidentialSidewalkStrip
{
    [JsonPropertyName("strip_id")]      public string StripId { get; set; } = string.Empty;
    [JsonPropertyName("strip_kind")]    public string StripKind { get; set; } = string.Empty;
    [JsonPropertyName("direction")]     public string Direction { get; set; } = string.Empty;
    [JsonPropertyName("x1")]           public int X1 { get; set; }
    [JsonPropertyName("y1")]           public int Y1 { get; set; }
    [JsonPropertyName("x2")]           public int X2 { get; set; }
    [JsonPropertyName("y2")]           public int Y2 { get; set; }
    [JsonPropertyName("width_tiles")]  public int WidthTiles { get; set; }
    [JsonPropertyName("notes")]        public string Notes { get; set; } = string.Empty;
}

public sealed class DeadMtlResidentialParcelTopologyCheck
{
    [JsonPropertyName("check_order")]  public int    CheckOrder { get; set; }
    [JsonPropertyName("check_id")]     public string CheckId { get; set; } = string.Empty;
    [JsonPropertyName("check_label")]  public string CheckLabel { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")]     public string Expected { get; set; } = string.Empty;
    [JsonPropertyName("actual")]       public string Actual { get; set; } = string.Empty;
}
