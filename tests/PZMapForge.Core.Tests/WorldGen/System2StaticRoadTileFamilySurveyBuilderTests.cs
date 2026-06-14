using System.Text;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class System2StaticRoadTileFamilySurveyBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-tile-survey", Path.GetRandomFileName());

    public System2StaticRoadTileFamilySurveyBuilderTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // -----------------------------------------------------------------------
    // JSON fixture helpers
    // -----------------------------------------------------------------------

    private string WritePlan(string json)
    {
        var path = Path.Combine(_tempDir, $"plan_{Path.GetRandomFileName()}.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private static string AllSixFamiliesPlan() => """
{
  "format": "pzmapforge.deadmtl.system2.static-road-tile-family-plan.v1",
  "status": "PLAN_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_placement_plan": "test.json",
  "records": [
    { "world_x": 10600, "world_y": 8250, "pixel_x": 20, "pixel_y": 50,
      "layer_id": "static_roads_local", "class": "local_street",
      "intent": "local_street_asphalt", "role": "road_surface",
      "candidate_family": "asphalt_road_surface_candidate",
      "confidence": "LOW_METADATA_ONLY", "color": "#404040" },
    { "world_x": 10625, "world_y": 8270, "pixel_x": 45, "pixel_y": 70,
      "layer_id": "static_roads_alleys", "class": "alley_ruelle",
      "intent": "alley_ruelle_asphalt", "role": "road_surface",
      "candidate_family": "asphalt_alley_surface_candidate",
      "confidence": "LOW_METADATA_ONLY", "color": "#303030" },
    { "world_x": 10710, "world_y": 8240, "pixel_x": 130, "pixel_y": 40,
      "layer_id": "static_roads_service", "class": "service_lane",
      "intent": "service_lane", "role": "road_surface",
      "candidate_family": "asphalt_service_lane_candidate",
      "confidence": "LOW_METADATA_ONLY", "color": "#505050" },
    { "world_x": 10720, "world_y": 8280, "pixel_x": 140, "pixel_y": 80,
      "layer_id": "static_roads_parking_access", "class": "parking_access",
      "intent": "parking_access", "role": "road_surface",
      "candidate_family": "asphalt_parking_access_candidate",
      "confidence": "LOW_METADATA_ONLY", "color": "#606060" },
    { "world_x": 10640, "world_y": 8290, "pixel_x": 60, "pixel_y": 90,
      "layer_id": "static_pedestrian_cuts", "class": "pedestrian_cut",
      "intent": "sidewalk_or_pedestrian_cut", "role": "pedestrian_cut",
      "candidate_family": "concrete_or_sidewalk_candidate",
      "confidence": "LOW_METADATA_ONLY", "color": "#B0B0B0" },
    { "world_x": 10650, "world_y": 8252, "pixel_x": 70, "pixel_y": 52,
      "layer_id": "static_road_nodes", "class": "intersection_turn_deadend_nodes",
      "intent": "intersection_node", "role": "road_node",
      "candidate_family": "road_node_metadata_candidate",
      "confidence": "LOW_METADATA_ONLY", "color": "#FF00FF" }
  ],
  "totals": { "record_count": 6, "by_candidate_family": {}, "by_role": {}, "by_intent": {}, "unmapped_count": 0 },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false,
                      "runtime_proven": false, "public_playable_claim": false }
}
""";

    // -----------------------------------------------------------------------
    // Missing file
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ReturnsError_WhenTileFamilyPlanFileMissing()
    {
        var result = System2StaticRoadTileFamilySurveyBuilder.Build(
            Path.Combine(_tempDir, "no-such-file.json"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Tile family plan file not found"));
    }

    // -----------------------------------------------------------------------
    // Family count
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_CreatesSixFamilies_FromAllSixFamilyNames()
    {
        var path   = WritePlan(AllSixFamiliesPlan());
        var result = System2StaticRoadTileFamilySurveyBuilder.Build(path);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(6, result.Survey!.Families.Count);
    }

    [Fact]
    public void Build_FamilyCount_Totals_IsSix()
    {
        var path   = WritePlan(AllSixFamiliesPlan());
        var result = System2StaticRoadTileFamilySurveyBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.Equal(6, result.Survey!.Totals.FamilyCount);
    }

    // -----------------------------------------------------------------------
    // All families unresolved
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_AllFamilies_AreUnresolved()
    {
        var path   = WritePlan(AllSixFamiliesPlan());
        var result = System2StaticRoadTileFamilySurveyBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.All(result.Survey!.Families,
            f => Assert.Equal("UNRESOLVED_NEEDS_TILE_SURVEY", f.ResolutionStatus));
    }

    [Fact]
    public void Build_ResolvedFamilyCount_IsZero()
    {
        var path   = WritePlan(AllSixFamiliesPlan());
        var result = System2StaticRoadTileFamilySurveyBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.Equal(0, result.Survey!.Totals.ResolvedFamilyCount);
    }

    [Fact]
    public void Build_UnresolvedFamilyCount_IsSix()
    {
        var path   = WritePlan(AllSixFamiliesPlan());
        var result = System2StaticRoadTileFamilySurveyBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.Equal(6, result.Survey!.Totals.UnresolvedFamilyCount);
    }

    [Fact]
    public void Build_CandidateTileCount_IsZero()
    {
        var path   = WritePlan(AllSixFamiliesPlan());
        var result = System2StaticRoadTileFamilySurveyBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.Equal(0, result.Survey!.Totals.CandidateTileCount);
    }

    // -----------------------------------------------------------------------
    // All six family names present
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("asphalt_road_surface_candidate")]
    [InlineData("asphalt_alley_surface_candidate")]
    [InlineData("asphalt_service_lane_candidate")]
    [InlineData("asphalt_parking_access_candidate")]
    [InlineData("concrete_or_sidewalk_candidate")]
    [InlineData("road_node_metadata_candidate")]
    public void Build_ContainsFamilyName(string expectedFamily)
    {
        var path   = WritePlan(AllSixFamiliesPlan());
        var result = System2StaticRoadTileFamilySurveyBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.Contains(result.Survey!.Families,
            f => f.CandidateFamily == expectedFamily);
    }

    // -----------------------------------------------------------------------
    // Format and status fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_OutputFormat_IsCorrect()
    {
        var path   = WritePlan(AllSixFamiliesPlan());
        var result = System2StaticRoadTileFamilySurveyBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.Equal("pzmapforge.deadmtl.system2.static-road-tile-family-survey.v1",
            result.Survey!.Format);
        Assert.Equal("SURVEY_CONTRACT_ONLY",  result.Survey.Status);
        Assert.Equal("NOT_RUNTIME_PROVEN",    result.Survey.RuntimeStatus);
        Assert.Equal("NOT_IMPLEMENTED",       result.Survey.WriterStatus);
    }

    // -----------------------------------------------------------------------
    // Claim boundary all false
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ClaimBoundary_AllFalse()
    {
        var path   = WritePlan(AllSixFamiliesPlan());
        var result = System2StaticRoadTileFamilySurveyBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.False(result.Survey!.ClaimBoundary.WritesLotpack);
        Assert.False(result.Survey.ClaimBoundary.WritesWorldgenLua);
        Assert.False(result.Survey.ClaimBoundary.RuntimeProven);
        Assert.False(result.Survey.ClaimBoundary.PublicPlayableClaim);
    }

    // -----------------------------------------------------------------------
    // Source intents collected per family
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_RoadNodeFamily_CollectsMultipleIntents()
    {
        var json = """
{
  "format": "pzmapforge.deadmtl.system2.static-road-tile-family-plan.v1",
  "status": "PLAN_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_placement_plan": "test.json",
  "records": [
    { "world_x": 10650, "world_y": 8252, "pixel_x": 70, "pixel_y": 52,
      "layer_id": "static_road_nodes", "class": "intersection_turn_deadend_nodes",
      "intent": "intersection_node", "role": "road_node",
      "candidate_family": "road_node_metadata_candidate",
      "confidence": "LOW_METADATA_ONLY", "color": "#FF00FF" },
    { "world_x": 10700, "world_y": 8255, "pixel_x": 120, "pixel_y": 55,
      "layer_id": "static_road_nodes", "class": "intersection_turn_deadend_nodes",
      "intent": "road_turn_node", "role": "road_node",
      "candidate_family": "road_node_metadata_candidate",
      "confidence": "LOW_METADATA_ONLY", "color": "#00FFFF" },
    { "world_x": 10695, "world_y": 8272, "pixel_x": 115, "pixel_y": 72,
      "layer_id": "static_road_nodes", "class": "intersection_turn_deadend_nodes",
      "intent": "dead_end_node", "role": "road_node",
      "candidate_family": "road_node_metadata_candidate",
      "confidence": "LOW_METADATA_ONLY", "color": "#FF9900" }
  ],
  "totals": { "record_count": 3, "by_candidate_family": {}, "by_role": {}, "by_intent": {}, "unmapped_count": 0 },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false,
                      "runtime_proven": false, "public_playable_claim": false }
}
""";
        var path   = WritePlan(json);
        var result = System2StaticRoadTileFamilySurveyBuilder.Build(path);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        var nodeFamily = result.Survey!.Families.Single(f => f.CandidateFamily == "road_node_metadata_candidate");
        Assert.Equal(3, nodeFamily.SourceIntents.Count);
        Assert.Contains("intersection_node", nodeFamily.SourceIntents);
        Assert.Contains("road_turn_node",    nodeFamily.SourceIntents);
        Assert.Contains("dead_end_node",     nodeFamily.SourceIntents);
    }
}
