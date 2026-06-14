using System.Text;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class System2StaticRoadTileFamilyPlanBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-tile-family", Path.GetRandomFileName());

    public System2StaticRoadTileFamilyPlanBuilderTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // -----------------------------------------------------------------------
    // JSON helpers
    // -----------------------------------------------------------------------

    private string WritePlan(string json)
    {
        var path = Path.Combine(_tempDir, $"plan_{Path.GetRandomFileName()}.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private string SinglePlacementPlan(
        string intent, string role, string color = "#404040",
        int worldX = 10600, int worldY = 8250,
        int pixelX = 20,    int pixelY = 50,
        string layerId = "static_roads_local", string layerClass = "local_street") => $$"""
{
  "format": "pzmapforge.deadmtl.system2.static-road-placement-plan.v1",
  "status": "PLAN_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_extract": "test.json",
  "origin_x": 10580, "origin_y": 8200, "width": 220, "height": 170,
  "placements": [{
    "world_x": {{worldX}}, "world_y": {{worldY}},
    "pixel_x": {{pixelX}}, "pixel_y": {{pixelY}},
    "layer_id": "{{layerId}}", "class": "{{layerClass}}",
    "intent": "{{intent}}", "role": "{{role}}", "color": "{{color}}"
  }],
  "totals": { "placement_count": 1, "run_source_count": 1, "node_source_count": 0,
              "duplicate_position_count": 0, "by_role": {}, "by_intent": {} },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false,
                      "runtime_proven": false, "public_playable_claim": false }
}
""";

    // -----------------------------------------------------------------------
    // Missing file
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ReturnsError_WhenPlacementPlanFileMissing()
    {
        var result = System2StaticRoadTileFamilyPlanBuilder.Build(
            Path.Combine(_tempDir, "no-such-file.json"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Placement plan file not found"));
    }

    // -----------------------------------------------------------------------
    // Intent to candidate family mapping
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_LocalStreetAsphalt_MapsTo_AsphaltRoadSurfaceCandidate()
    {
        var path   = WritePlan(SinglePlacementPlan("local_street_asphalt", "road_surface", "#404040"));
        var result = System2StaticRoadTileFamilyPlanBuilder.Build(path);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal("asphalt_road_surface_candidate", result.Plan!.Records[0].CandidateFamily);
    }

    [Fact]
    public void Build_AlleyRuelleAsphalt_MapsTo_AsphaltAlleySurfaceCandidate()
    {
        var path   = WritePlan(SinglePlacementPlan("alley_ruelle_asphalt", "road_surface", "#303030",
            layerId: "static_roads_alleys", layerClass: "alley_ruelle"));
        var result = System2StaticRoadTileFamilyPlanBuilder.Build(path);
        Assert.True(result.IsValid);
        Assert.Equal("asphalt_alley_surface_candidate", result.Plan!.Records[0].CandidateFamily);
    }

    [Fact]
    public void Build_ServiceLane_MapsTo_AsphaltServiceLaneCandidate()
    {
        var path   = WritePlan(SinglePlacementPlan("service_lane", "road_surface", "#505050",
            layerId: "static_roads_service", layerClass: "service_lane"));
        var result = System2StaticRoadTileFamilyPlanBuilder.Build(path);
        Assert.True(result.IsValid);
        Assert.Equal("asphalt_service_lane_candidate", result.Plan!.Records[0].CandidateFamily);
    }

    [Fact]
    public void Build_ParkingAccess_MapsTo_AsphaltParkingAccessCandidate()
    {
        var path   = WritePlan(SinglePlacementPlan("parking_access", "road_surface", "#606060",
            layerId: "static_roads_parking_access", layerClass: "parking_access"));
        var result = System2StaticRoadTileFamilyPlanBuilder.Build(path);
        Assert.True(result.IsValid);
        Assert.Equal("asphalt_parking_access_candidate", result.Plan!.Records[0].CandidateFamily);
    }

    [Fact]
    public void Build_SidewalkOrPedestrianCut_MapsTo_ConcreteOrSidewalkCandidate()
    {
        var path   = WritePlan(SinglePlacementPlan("sidewalk_or_pedestrian_cut", "pedestrian_cut", "#B0B0B0",
            layerId: "static_pedestrian_cuts", layerClass: "pedestrian_cut"));
        var result = System2StaticRoadTileFamilyPlanBuilder.Build(path);
        Assert.True(result.IsValid);
        Assert.Equal("concrete_or_sidewalk_candidate", result.Plan!.Records[0].CandidateFamily);
    }

    [Theory]
    [InlineData("intersection_node", "#FF00FF")]
    [InlineData("road_turn_node",    "#00FFFF")]
    [InlineData("dead_end_node",     "#FF9900")]
    public void Build_NodeIntents_MapTo_RoadNodeMetadataCandidate(string intent, string color)
    {
        var path   = WritePlan(SinglePlacementPlan(intent, "road_node", color,
            layerId: "static_road_nodes", layerClass: "intersection_turn_deadend_nodes"));
        var result = System2StaticRoadTileFamilyPlanBuilder.Build(path);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal("road_node_metadata_candidate", result.Plan!.Records[0].CandidateFamily);
    }

    // -----------------------------------------------------------------------
    // Unknown intent fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_UnknownIntent_Fails()
    {
        var path   = WritePlan(SinglePlacementPlan("totally_unknown_xyz", "road_surface", "#010203"));
        var result = System2StaticRoadTileFamilyPlanBuilder.Build(path);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Unknown intent"));
    }

    // -----------------------------------------------------------------------
    // record_count equals placement count
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_RecordCount_EqualsPlacementCount()
    {
        var json = """
{
  "format": "pzmapforge.deadmtl.system2.static-road-placement-plan.v1",
  "status": "PLAN_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_extract": "test.json",
  "origin_x": 10580, "origin_y": 8200, "width": 220, "height": 170,
  "placements": [
    { "world_x": 10600, "world_y": 8250, "pixel_x": 20, "pixel_y": 50,
      "layer_id": "static_roads_local", "class": "local_street",
      "intent": "local_street_asphalt", "role": "road_surface", "color": "#404040" },
    { "world_x": 10601, "world_y": 8250, "pixel_x": 21, "pixel_y": 50,
      "layer_id": "static_roads_local", "class": "local_street",
      "intent": "local_street_asphalt", "role": "road_surface", "color": "#404040" },
    { "world_x": 10650, "world_y": 8252, "pixel_x": 70, "pixel_y": 52,
      "layer_id": "static_road_nodes", "class": "intersection_turn_deadend_nodes",
      "intent": "intersection_node", "role": "road_node", "color": "#FF00FF" }
  ],
  "totals": { "placement_count": 3, "run_source_count": 1, "node_source_count": 1,
              "duplicate_position_count": 0, "by_role": {}, "by_intent": {} },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false,
                      "runtime_proven": false, "public_playable_claim": false }
}
""";
        var path   = WritePlan(json);
        var result = System2StaticRoadTileFamilyPlanBuilder.Build(path);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(3, result.Plan!.Totals.RecordCount);
    }

    // -----------------------------------------------------------------------
    // Format and status fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_OutputFormat_IsCorrect()
    {
        var path   = WritePlan(SinglePlacementPlan("local_street_asphalt", "road_surface"));
        var result = System2StaticRoadTileFamilyPlanBuilder.Build(path);
        Assert.True(result.IsValid);
        Assert.Equal("pzmapforge.deadmtl.system2.static-road-tile-family-plan.v1", result.Plan!.Format);
        Assert.Equal("PLAN_ONLY",          result.Plan.Status);
        Assert.Equal("NOT_RUNTIME_PROVEN", result.Plan.RuntimeStatus);
        Assert.Equal("NOT_IMPLEMENTED",    result.Plan.WriterStatus);
    }

    // -----------------------------------------------------------------------
    // Claim boundary all false
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ClaimBoundary_AllFalse()
    {
        var path   = WritePlan(SinglePlacementPlan("local_street_asphalt", "road_surface"));
        var result = System2StaticRoadTileFamilyPlanBuilder.Build(path);
        Assert.True(result.IsValid);
        Assert.False(result.Plan!.ClaimBoundary.WritesLotpack);
        Assert.False(result.Plan.ClaimBoundary.WritesWorldgenLua);
        Assert.False(result.Plan.ClaimBoundary.RuntimeProven);
        Assert.False(result.Plan.ClaimBoundary.PublicPlayableClaim);
    }

    // -----------------------------------------------------------------------
    // Confidence field
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Confidence_IsLowMetadataOnly()
    {
        var path   = WritePlan(SinglePlacementPlan("local_street_asphalt", "road_surface"));
        var result = System2StaticRoadTileFamilyPlanBuilder.Build(path);
        Assert.True(result.IsValid);
        Assert.Equal("LOW_METADATA_ONLY", result.Plan!.Records[0].Confidence);
    }
}
