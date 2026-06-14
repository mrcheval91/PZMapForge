using System.Text;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class System2StaticRoadPlacementPlanBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-placement-plan", Path.GetRandomFileName());

    public System2StaticRoadPlacementPlanBuilderTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // -----------------------------------------------------------------------
    // JSON helpers
    // -----------------------------------------------------------------------

    private string WriteExtract(string json)
    {
        var path = Path.Combine(_tempDir, $"extract_{Path.GetRandomFileName()}.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private string SingleRunExtract(
        int originX, int originY,
        string layerId, string layerClass,
        int y, int xStart, int xEnd,
        string intent, string color) => $$"""
{
  "format": "pzmapforge.deadmtl.system2.static-road-extract.v1",
  "origin_x": {{originX}}, "origin_y": {{originY}}, "width": 50, "height": 50,
  "layers": [{
    "id": "{{layerId}}", "file": "test.png", "class": "{{layerClass}}",
    "non_empty_pixels": {{xEnd - xStart + 1}},
    "intents": ["{{intent}}"],
    "runs": [{
      "y": {{y}}, "x_start": {{xStart}}, "x_end": {{xEnd}},
      "world_y": {{originY + y}}, "world_x_start": {{originX + xStart}}, "world_x_end": {{originX + xEnd}},
      "intent": "{{intent}}", "color": "{{color}}"
    }],
    "nodes": []
  }],
  "totals": { "layer_count": 1, "non_empty_pixels": {{xEnd - xStart + 1}}, "unknown_opaque_pixels": 0 },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false, "runtime_proven": false, "public_playable_claim": false }
}
""";

    private string SingleNodeExtract(
        int originX, int originY,
        string layerId, string layerClass,
        int pixelX, int pixelY,
        string intent, string color) => $$"""
{
  "format": "pzmapforge.deadmtl.system2.static-road-extract.v1",
  "origin_x": {{originX}}, "origin_y": {{originY}}, "width": 50, "height": 50,
  "layers": [{
    "id": "{{layerId}}", "file": "test.png", "class": "{{layerClass}}",
    "non_empty_pixels": 1,
    "intents": ["{{intent}}"],
    "runs": [],
    "nodes": [{
      "pixel_x": {{pixelX}}, "pixel_y": {{pixelY}},
      "world_x": {{originX + pixelX}}, "world_y": {{originY + pixelY}},
      "intent": "{{intent}}", "color": "{{color}}"
    }]
  }],
  "totals": { "layer_count": 1, "non_empty_pixels": 1, "unknown_opaque_pixels": 0 },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false, "runtime_proven": false, "public_playable_claim": false }
}
""";

    // -----------------------------------------------------------------------
    // Missing file
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ReturnsError_WhenExtractFileMissing()
    {
        var result = System2StaticRoadPlacementPlanBuilder.Build(
            Path.Combine(_tempDir, "no-such-file.json"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Extract file not found"));
    }

    // -----------------------------------------------------------------------
    // Run expansion
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ExpandsOneRun_IntoExpectedPlacementCount()
    {
        var path = WriteExtract(SingleRunExtract(
            100, 200, "static_roads_local", "local_street",
            0, 0, 4, "local_street_asphalt", "#404040"));

        var result = System2StaticRoadPlacementPlanBuilder.Build(path);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(5, result.Plan!.Totals.PlacementCount);
    }

    [Fact]
    public void Build_RunExpansion_WorldCoordsMatchOriginPlusPixel()
    {
        var path = WriteExtract(SingleRunExtract(
            1000, 2000, "static_roads_local", "local_street",
            3, 5, 7, "local_street_asphalt", "#404040"));

        var result = System2StaticRoadPlacementPlanBuilder.Build(path);
        Assert.True(result.IsValid);

        var first = result.Plan!.Placements[0];
        Assert.Equal(1005, first.WorldX);
        Assert.Equal(2003, first.WorldY);
        Assert.Equal(5,    first.PixelX);
        Assert.Equal(3,    first.PixelY);
    }

    // -----------------------------------------------------------------------
    // Node expansion
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ConvertsNode_ToOnePlacement()
    {
        var path = WriteExtract(SingleNodeExtract(
            100, 200, "static_road_nodes", "intersection_turn_deadend_nodes",
            7, 4, "intersection_node", "#FF00FF"));

        var result = System2StaticRoadPlacementPlanBuilder.Build(path);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(1, result.Plan!.Totals.PlacementCount);
        Assert.Equal(1, result.Plan.Totals.NodeSourceCount);
        Assert.Equal(0, result.Plan.Totals.RunSourceCount);
    }

    // -----------------------------------------------------------------------
    // Intent to role mapping
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_LocalStreetAsphalt_MapsTo_RoadSurface()
    {
        var path = WriteExtract(SingleRunExtract(
            0, 0, "static_roads_local", "local_street",
            0, 0, 0, "local_street_asphalt", "#404040"));

        var result = System2StaticRoadPlacementPlanBuilder.Build(path);
        Assert.True(result.IsValid);
        Assert.Equal("road_surface", result.Plan!.Placements[0].Role);
    }

    [Fact]
    public void Build_AlleyRuelleAsphalt_MapsTo_RoadSurface()
    {
        var path = WriteExtract(SingleRunExtract(
            0, 0, "static_roads_alleys", "alley_ruelle",
            0, 0, 0, "alley_ruelle_asphalt", "#303030"));

        var result = System2StaticRoadPlacementPlanBuilder.Build(path);
        Assert.True(result.IsValid);
        Assert.Equal("road_surface", result.Plan!.Placements[0].Role);
    }

    [Fact]
    public void Build_SidewalkOrPedestrianCut_MapsTo_PedestrianCut()
    {
        var path = WriteExtract(SingleRunExtract(
            0, 0, "static_pedestrian_cuts", "pedestrian_cut",
            0, 0, 0, "sidewalk_or_pedestrian_cut", "#B0B0B0"));

        var result = System2StaticRoadPlacementPlanBuilder.Build(path);
        Assert.True(result.IsValid);
        Assert.Equal("pedestrian_cut", result.Plan!.Placements[0].Role);
    }

    [Fact]
    public void Build_IntersectionNode_MapsTo_RoadNode()
    {
        var path = WriteExtract(SingleNodeExtract(
            0, 0, "static_road_nodes", "intersection_turn_deadend_nodes",
            5, 5, "intersection_node", "#FF00FF"));

        var result = System2StaticRoadPlacementPlanBuilder.Build(path);
        Assert.True(result.IsValid);
        Assert.Equal("road_node", result.Plan!.Placements[0].Role);
    }

    // -----------------------------------------------------------------------
    // Unknown intent fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_UnknownIntent_Fails()
    {
        var path = WriteExtract(SingleRunExtract(
            0, 0, "static_roads_local", "local_street",
            0, 0, 0, "totally_unknown_intent_xyz", "#010203"));

        var result = System2StaticRoadPlacementPlanBuilder.Build(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Unknown intent"));
    }

    // -----------------------------------------------------------------------
    // Format and claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_OutputFormat_IsCorrect()
    {
        var path = WriteExtract(SingleRunExtract(
            0, 0, "static_roads_local", "local_street",
            0, 0, 2, "local_street_asphalt", "#404040"));

        var result = System2StaticRoadPlacementPlanBuilder.Build(path);
        Assert.True(result.IsValid);
        Assert.Equal("pzmapforge.deadmtl.system2.static-road-placement-plan.v1",
            result.Plan!.Format);
        Assert.Equal("PLAN_ONLY",           result.Plan.Status);
        Assert.Equal("NOT_RUNTIME_PROVEN",  result.Plan.RuntimeStatus);
        Assert.Equal("NOT_IMPLEMENTED",     result.Plan.WriterStatus);
    }

    [Fact]
    public void Build_ClaimBoundary_AllFalse()
    {
        var path = WriteExtract(SingleRunExtract(
            0, 0, "static_roads_local", "local_street",
            0, 0, 0, "local_street_asphalt", "#404040"));

        var result = System2StaticRoadPlacementPlanBuilder.Build(path);
        Assert.True(result.IsValid);
        Assert.False(result.Plan!.ClaimBoundary.WritesLotpack);
        Assert.False(result.Plan.ClaimBoundary.WritesWorldgenLua);
        Assert.False(result.Plan.ClaimBoundary.RuntimeProven);
        Assert.False(result.Plan.ClaimBoundary.PublicPlayableClaim);
    }

    // -----------------------------------------------------------------------
    // Duplicate positions counted
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_DuplicatePositions_Counted()
    {
        // Layer 1: run at (x=3, y=5) → world (13, 25)
        // Layer 2: node at pixel (3,5) → world (13, 25) — same position
        var json = """
{
  "format": "pzmapforge.deadmtl.system2.static-road-extract.v1",
  "origin_x": 10, "origin_y": 20, "width": 20, "height": 20,
  "layers": [
    {
      "id": "static_roads_local", "file": "a.png", "class": "local_street",
      "non_empty_pixels": 1, "intents": ["local_street_asphalt"],
      "runs": [{
        "y": 5, "x_start": 3, "x_end": 3,
        "world_y": 25, "world_x_start": 13, "world_x_end": 13,
        "intent": "local_street_asphalt", "color": "#404040"
      }],
      "nodes": []
    },
    {
      "id": "static_road_nodes", "file": "b.png", "class": "intersection_turn_deadend_nodes",
      "non_empty_pixels": 1, "intents": ["intersection_node"],
      "runs": [],
      "nodes": [{
        "pixel_x": 3, "pixel_y": 5, "world_x": 13, "world_y": 25,
        "intent": "intersection_node", "color": "#FF00FF"
      }]
    }
  ],
  "totals": { "layer_count": 2, "non_empty_pixels": 2, "unknown_opaque_pixels": 0 },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false, "runtime_proven": false, "public_playable_claim": false }
}
""";
        var path   = WriteExtract(json);
        var result = System2StaticRoadPlacementPlanBuilder.Build(path);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(2, result.Plan!.Totals.PlacementCount);
        Assert.Equal(1, result.Plan.Totals.DuplicatePositionCount);
    }
}
