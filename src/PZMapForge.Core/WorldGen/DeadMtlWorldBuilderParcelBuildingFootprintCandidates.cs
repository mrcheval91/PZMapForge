using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderParcelBuildingFootprintCandidatesResult
{
    [JsonPropertyName("format")]                    public string Format               { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")]             public string GeneratedUtc         { get; set; } = string.Empty;
    [JsonPropertyName("source_lot_fill_json")]      public string SourceLotFillJson    { get; set; } = string.Empty;
    [JsonPropertyName("lot_fill_policy_version")]   public string LotFillPolicyVersion { get; set; } = string.Empty;

    [JsonPropertyName("policy_source")]             public string PolicySource         { get; set; } = string.Empty;
    [JsonPropertyName("policy_path")]               public string PolicyPath           { get; set; } = string.Empty;
    [JsonPropertyName("policy_loaded")]             public bool   PolicyLoaded         { get; set; }
    [JsonPropertyName("policy_version")]            public string PolicyVersion        { get; set; } = string.Empty;
    [JsonPropertyName("policy_entry_count")]        public int    PolicyEntryCount     { get; set; }

    [JsonPropertyName("total_lot_count")]           public int TotalLotCount           { get; set; }
    [JsonPropertyName("eligible_lot_count")]        public int EligibleLotCount        { get; set; }
    [JsonPropertyName("footprint_count")]           public int FootprintCount          { get; set; }
    [JsonPropertyName("skipped_lot_count")]         public int SkippedLotCount         { get; set; }
    [JsonPropertyName("blue_footprint_count")]      public int BlueFootprintCount      { get; set; }
    [JsonPropertyName("red_footprint_count")]       public int RedFootprintCount       { get; set; }

    [JsonPropertyName("footprints")]    public List<ParcelBuildingFootprintCandidate> Footprints   { get; set; } = new();
    [JsonPropertyName("skipped_lots")]  public List<SkippedFootprintLot>              SkippedLots  { get; set; } = new();

    [JsonPropertyName("sandbox_only")]                      public bool SandboxOnly                    { get; set; } = true;
    [JsonPropertyName("writer_ready")]                      public bool WriterReady                    { get; set; }
    [JsonPropertyName("runtime_valid")]                     public bool RuntimeValid                   { get; set; }
    [JsonPropertyName("materialized")]                      public bool Materialized                   { get; set; }
    [JsonPropertyName("runtime_proof_claimed")]             public bool RuntimeProofClaimed            { get; set; }
    [JsonPropertyName("public_playable_packaging_claimed")] public bool PublicPlayablePackagingClaimed { get; set; }

    [JsonPropertyName("forbidden_artifact_scan")] public string ForbiddenArtifactScan { get; set; } = string.Empty;

    [JsonPropertyName("check_count")]        public int    CheckCount        { get; set; }
    [JsonPropertyName("passed_check_count")] public int    PassedCheckCount  { get; set; }
    [JsonPropertyName("failed_check_count")] public int    FailedCheckCount  { get; set; }
    [JsonPropertyName("is_valid")]           public bool   IsValid           { get; set; }
    [JsonPropertyName("verdict")]            public string Verdict           { get; set; } = string.Empty;
    [JsonPropertyName("checks")]             public List<ParcelBuildingFootprintCheck> Checks { get; set; } = new();
    [JsonPropertyName("errors")]             public List<string> Errors      { get; set; } = new();
}

public sealed class ParcelBuildingFootprintCandidate
{
    [JsonPropertyName("footprint_id")]        public string FootprintId       { get; set; } = string.Empty;
    [JsonPropertyName("lot_id")]              public string LotId             { get; set; } = string.Empty;
    [JsonPropertyName("component_id")]        public string ComponentId       { get; set; } = string.Empty;
    [JsonPropertyName("parcel_class")]        public string ParcelClass       { get; set; } = string.Empty;
    [JsonPropertyName("frontage_direction")]  public string FrontageDirection { get; set; } = string.Empty;
    [JsonPropertyName("footprint_kind")]      public string FootprintKind     { get; set; } = string.Empty;
    [JsonPropertyName("lot_x1")]              public int    LotX1             { get; set; }
    [JsonPropertyName("lot_y1")]              public int    LotY1             { get; set; }
    [JsonPropertyName("lot_x2")]              public int    LotX2             { get; set; }
    [JsonPropertyName("lot_y2")]              public int    LotY2             { get; set; }
    [JsonPropertyName("lot_tile_count")]      public int    LotTileCount      { get; set; }
    [JsonPropertyName("fp_x1")]              public int    FpX1              { get; set; }
    [JsonPropertyName("fp_y1")]              public int    FpY1              { get; set; }
    [JsonPropertyName("fp_x2")]              public int    FpX2              { get; set; }
    [JsonPropertyName("fp_y2")]              public int    FpY2              { get; set; }
    [JsonPropertyName("fp_width")]           public int    FpWidth           { get; set; }
    [JsonPropertyName("fp_depth")]           public int    FpDepth           { get; set; }
    [JsonPropertyName("fp_tile_count")]      public int    FpTileCount       { get; set; }
    [JsonPropertyName("coverage_ratio")]     public double CoverageRatio     { get; set; }
    [JsonPropertyName("shade_r")]            public int    ShadeR            { get; set; }
    [JsonPropertyName("shade_g")]            public int    ShadeG            { get; set; }
    [JsonPropertyName("shade_b")]            public int    ShadeB            { get; set; }
}

public sealed class SkippedFootprintLot
{
    [JsonPropertyName("lot_id")]       public string LotId      { get; set; } = string.Empty;
    [JsonPropertyName("component_id")] public string ComponentId { get; set; } = string.Empty;
    [JsonPropertyName("parcel_class")] public string ParcelClass { get; set; } = string.Empty;
    [JsonPropertyName("reason")]       public string Reason     { get; set; } = string.Empty;
}

public sealed class ParcelBuildingFootprintCheck
{
    [JsonPropertyName("check_id")]     public string CheckId     { get; set; } = string.Empty;
    [JsonPropertyName("description")]  public string Description { get; set; } = string.Empty;
    [JsonPropertyName("expected")]     public string Expected    { get; set; } = string.Empty;
    [JsonPropertyName("actual")]       public string Actual      { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
}
