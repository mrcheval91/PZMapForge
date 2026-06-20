using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillResult
{
    [JsonPropertyName("format")]          public string Format         { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")]   public string GeneratedUtc   { get; set; } = string.Empty;
    [JsonPropertyName("source_png")]      public string SourcePng      { get; set; } = string.Empty;
    [JsonPropertyName("source_png_sha256")] public string SourcePngSha256 { get; set; } = string.Empty;
    [JsonPropertyName("map_id")]          public string MapId          { get; set; } = string.Empty;

    [JsonPropertyName("detected_blue_component_count")]           public int DetectedBlueComponentCount          { get; set; }
    [JsonPropertyName("processed_quadrilateral_component_count")] public int ProcessedQuadrilateralComponentCount { get; set; }
    [JsonPropertyName("unsupported_blue_component_count")]        public int UnsupportedBlueComponentCount        { get; set; }
    [JsonPropertyName("blue_lot_count")]                          public int BlueLotCount                         { get; set; }
    [JsonPropertyName("blue_facade_edge_count")]                  public int BlueFacadeEdgeCount                  { get; set; }
    [JsonPropertyName("total_lot_count")]                         public int TotalLotCount                        { get; set; }
    [JsonPropertyName("total_facade_edge_count")]                 public int TotalFacadeEdgeCount                 { get; set; }

    [JsonPropertyName("detected_red_component_count")]           public int DetectedRedComponentCount          { get; set; }
    [JsonPropertyName("processed_red_component_count")]          public int ProcessedRedComponentCount         { get; set; }
    [JsonPropertyName("unsupported_red_component_count")]        public int UnsupportedRedComponentCount       { get; set; }
    [JsonPropertyName("red_lot_count")]                          public int RedLotCount                        { get; set; }
    [JsonPropertyName("red_facade_edge_count")]                  public int RedFacadeEdgeCount                 { get; set; }
    [JsonPropertyName("source_red_pixels_replaced")]             public int SourceRedPixelsReplaced            { get; set; }
    [JsonPropertyName("source_red_pixels_remaining")]           public int SourceRedPixelsRemaining           { get; set; }

    [JsonPropertyName("lot_sizing_policy_source")]               public string LotSizingPolicySource           { get; set; } = string.Empty;
    [JsonPropertyName("lot_sizing_policy_path")]                 public string LotSizingPolicyPath             { get; set; } = string.Empty;
    [JsonPropertyName("lot_sizing_policy_loaded")]               public bool   LotSizingPolicyLoaded           { get; set; }
    [JsonPropertyName("lot_sizing_policy_version")]              public string LotSizingPolicyVersion          { get; set; } = string.Empty;
    [JsonPropertyName("lot_sizing_policy_entry_count")]          public int    LotSizingPolicyEntryCount       { get; set; }
    [JsonPropertyName("undersized_lot_merge_count")]             public int    UndersizedLotMergeCount         { get; set; }
    [JsonPropertyName("blue_undersized_lot_merge_count")]        public int    BlueUndersizedLotMergeCount     { get; set; }
    [JsonPropertyName("red_undersized_lot_merge_count")]         public int    RedUndersizedLotMergeCount      { get; set; }

    [JsonPropertyName("components")]    public List<DetectedBlueComponent>           Components   { get; set; } = new();
    [JsonPropertyName("lots")]          public List<QuadrilateralLot>                Lots         { get; set; } = new();
    [JsonPropertyName("facade_edges")]  public List<QuadrilateralFacadeEdge>         FacadeEdges  { get; set; } = new();

    [JsonPropertyName("sandbox_only")]                      public bool SandboxOnly                     { get; set; } = true;
    [JsonPropertyName("writer_ready")]                      public bool WriterReady                     { get; set; }
    [JsonPropertyName("runtime_valid")]                     public bool RuntimeValid                    { get; set; }
    [JsonPropertyName("materialized")]                      public bool Materialized                    { get; set; }
    [JsonPropertyName("runtime_proof_claimed")]             public bool RuntimeProofClaimed             { get; set; }
    [JsonPropertyName("public_playable_packaging_claimed")] public bool PublicPlayablePackagingClaimed  { get; set; }

    [JsonPropertyName("forbidden_artifact_scan")] public string ForbiddenArtifactScan { get; set; } = string.Empty;

    [JsonPropertyName("check_count")]        public int    CheckCount        { get; set; }
    [JsonPropertyName("passed_check_count")] public int    PassedCheckCount  { get; set; }
    [JsonPropertyName("failed_check_count")] public int    FailedCheckCount  { get; set; }
    [JsonPropertyName("is_valid")]           public bool   IsValid           { get; set; }
    [JsonPropertyName("verdict")]            public string Verdict           { get; set; } = string.Empty;
    [JsonPropertyName("checks")]             public List<QuadrilateralLotFillCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")]             public List<string> Errors      { get; set; } = new();
}

public sealed class DetectedBlueComponent
{
    [JsonPropertyName("component_id")]         public string ComponentId        { get; set; } = string.Empty;
    [JsonPropertyName("parcel_class")]         public string ParcelClass        { get; set; } = string.Empty;
    [JsonPropertyName("bbox_x1")]              public int    BboxX1             { get; set; }
    [JsonPropertyName("bbox_y1")]              public int    BboxY1             { get; set; }
    [JsonPropertyName("bbox_x2")]              public int    BboxX2             { get; set; }
    [JsonPropertyName("bbox_y2")]              public int    BboxY2             { get; set; }
    [JsonPropertyName("bbox_width")]           public int    BboxWidth          { get; set; }
    [JsonPropertyName("bbox_height")]          public int    BboxHeight         { get; set; }
    [JsonPropertyName("pixel_count")]          public int    PixelCount         { get; set; }
    [JsonPropertyName("fill_ratio")]           public double FillRatio          { get; set; }
    [JsonPropertyName("processable")]          public bool   Processable        { get; set; }
    [JsonPropertyName("unsupported_reason")]   public string UnsupportedReason  { get; set; } = string.Empty;
    [JsonPropertyName("north_street_adjacency")] public bool NorthStreetAdjacency { get; set; }
    [JsonPropertyName("south_street_adjacency")] public bool SouthStreetAdjacency { get; set; }
    [JsonPropertyName("east_street_adjacency")]  public bool EastStreetAdjacency  { get; set; }
    [JsonPropertyName("west_street_adjacency")]  public bool WestStreetAdjacency  { get; set; }
    [JsonPropertyName("north_street_contact_count")] public int NorthStreetContactCount { get; set; }
    [JsonPropertyName("south_street_contact_count")] public int SouthStreetContactCount { get; set; }
    [JsonPropertyName("east_street_contact_count")]  public int EastStreetContactCount  { get; set; }
    [JsonPropertyName("west_street_contact_count")]  public int WestStreetContactCount  { get; set; }
    [JsonPropertyName("north_street_contact_ratio")] public double NorthStreetContactRatio { get; set; }
    [JsonPropertyName("south_street_contact_ratio")] public double SouthStreetContactRatio { get; set; }
    [JsonPropertyName("east_street_contact_ratio")]  public double EastStreetContactRatio  { get; set; }
    [JsonPropertyName("west_street_contact_ratio")]  public double WestStreetContactRatio  { get; set; }
    [JsonPropertyName("selected_frontage_group")]    public string       SelectedFrontageGroup { get; set; } = string.Empty;
    [JsonPropertyName("selected_primary_sides")]     public List<string> SelectedPrimarySides  { get; set; } = new();
    [JsonPropertyName("lot_count")]            public int    LotCount           { get; set; }
    [JsonPropertyName("facade_edge_count")]    public int    FacadeEdgeCount    { get; set; }
    [JsonPropertyName("lot_ids")]              public List<string> LotIds       { get; set; } = new();
    [JsonPropertyName("facade_edge_ids")]      public List<string> FacadeEdgeIds { get; set; } = new();
    [JsonPropertyName("orientation_case")]     public string OrientationCase    { get; set; } = string.Empty;
}

public sealed class QuadrilateralLot
{
    [JsonPropertyName("component_id")]            public string ComponentId          { get; set; } = string.Empty;
    [JsonPropertyName("lot_id")]                  public string LotId                { get; set; } = string.Empty;
    [JsonPropertyName("frontage_direction")]      public string FrontageDirection    { get; set; } = string.Empty;
    [JsonPropertyName("x1")]                      public int    X1                   { get; set; }
    [JsonPropertyName("y1")]                      public int    Y1                   { get; set; }
    [JsonPropertyName("x2")]                      public int    X2                   { get; set; }
    [JsonPropertyName("y2")]                      public int    Y2                   { get; set; }
    [JsonPropertyName("width")]                   public int    Width                { get; set; }
    [JsonPropertyName("height")]                  public int    Height               { get; set; }
    [JsonPropertyName("tile_count")]              public int    TileCount            { get; set; }
    [JsonPropertyName("shade_r")]                 public int    ShadeR               { get; set; }
    [JsonPropertyName("shade_g")]                 public int    ShadeG               { get; set; }
    [JsonPropertyName("shade_b")]                 public int    ShadeB               { get; set; }
    [JsonPropertyName("shade_rgb")]               public string ShadeRgb             { get; set; } = string.Empty;
    [JsonPropertyName("is_corner_lot")]           public bool   IsCornerLot          { get; set; }
    [JsonPropertyName("primary_facade_edge_id")]  public string PrimaryFacadeEdgeId  { get; set; } = string.Empty;
}

public sealed class QuadrilateralFacadeEdge
{
    [JsonPropertyName("component_id")]          public string ComponentId        { get; set; } = string.Empty;
    [JsonPropertyName("lot_id")]                public string LotId              { get; set; } = string.Empty;
    [JsonPropertyName("facade_edge_id")]        public string FacadeEdgeId       { get; set; } = string.Empty;
    [JsonPropertyName("frontage_direction")]    public string FrontageDirection  { get; set; } = string.Empty;
    [JsonPropertyName("x1")]                   public int    X1                 { get; set; }
    [JsonPropertyName("y1")]                   public int    Y1                 { get; set; }
    [JsonPropertyName("x2")]                   public int    X2                 { get; set; }
    [JsonPropertyName("y2")]                   public int    Y2                 { get; set; }
    [JsonPropertyName("length_tiles")]          public int    LengthTiles        { get; set; }
    [JsonPropertyName("street_contact_count")]  public int    StreetContactCount { get; set; }
    [JsonPropertyName("street_contact_ratio")]  public double StreetContactRatio { get; set; }
}

public sealed class QuadrilateralLotFillCheck
{
    [JsonPropertyName("check_order")]  public int    CheckOrder  { get; set; }
    [JsonPropertyName("check_id")]     public string CheckId     { get; set; } = string.Empty;
    [JsonPropertyName("check_label")]  public string CheckLabel  { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")]     public string Expected    { get; set; } = string.Empty;
    [JsonPropertyName("actual")]       public string Actual      { get; set; } = string.Empty;
}
