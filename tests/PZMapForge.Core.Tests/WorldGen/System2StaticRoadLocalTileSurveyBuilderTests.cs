using System.Text;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class System2StaticRoadLocalTileSurveyBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-local-tile-survey", Path.GetRandomFileName());

    public System2StaticRoadLocalTileSurveyBuilderTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private string WriteSurvey(string json)
    {
        var path = Path.Combine(_tempDir, $"survey_{Path.GetRandomFileName()}.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private static string AllSixFamiliesSurvey() => """
{
  "format": "pzmapforge.deadmtl.system2.static-road-tile-family-survey.v1",
  "status": "SURVEY_CONTRACT_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_tile_family_plan": "test.json",
  "families": [
    { "candidate_family": "asphalt_road_surface_candidate",
      "source_intents": ["local_street_asphalt"], "role": "road_surface",
      "resolution_status": "UNRESOLVED_NEEDS_TILE_SURVEY", "confidence": "NONE_YET",
      "candidate_tiles": [], "notes": "" },
    { "candidate_family": "asphalt_alley_surface_candidate",
      "source_intents": ["alley_ruelle_asphalt"], "role": "road_surface",
      "resolution_status": "UNRESOLVED_NEEDS_TILE_SURVEY", "confidence": "NONE_YET",
      "candidate_tiles": [], "notes": "" },
    { "candidate_family": "asphalt_service_lane_candidate",
      "source_intents": ["service_lane"], "role": "road_surface",
      "resolution_status": "UNRESOLVED_NEEDS_TILE_SURVEY", "confidence": "NONE_YET",
      "candidate_tiles": [], "notes": "" },
    { "candidate_family": "asphalt_parking_access_candidate",
      "source_intents": ["parking_access"], "role": "road_surface",
      "resolution_status": "UNRESOLVED_NEEDS_TILE_SURVEY", "confidence": "NONE_YET",
      "candidate_tiles": [], "notes": "" },
    { "candidate_family": "concrete_or_sidewalk_candidate",
      "source_intents": ["sidewalk_or_pedestrian_cut"], "role": "pedestrian_cut",
      "resolution_status": "UNRESOLVED_NEEDS_TILE_SURVEY", "confidence": "NONE_YET",
      "candidate_tiles": [], "notes": "" },
    { "candidate_family": "road_node_metadata_candidate",
      "source_intents": ["intersection_node", "road_turn_node", "dead_end_node"], "role": "road_node",
      "resolution_status": "UNRESOLVED_NEEDS_TILE_SURVEY", "confidence": "NONE_YET",
      "candidate_tiles": [], "notes": "" }
  ],
  "totals": { "family_count": 6, "resolved_family_count": 0, "unresolved_family_count": 6, "candidate_tile_count": 0 },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false,
                      "runtime_proven": false, "public_playable_claim": false }
}
""";

    private string MakeFakePzRoot(params (string fileName, string content)[] files)
    {
        var pzRoot = Path.Combine(_tempDir, "fake_pz_" + Path.GetRandomFileName());
        Directory.CreateDirectory(pzRoot);
        foreach (var (fileName, content) in files)
        {
            var filePath = Path.Combine(pzRoot, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, content, Encoding.UTF8);
        }
        return pzRoot;
    }

    // -----------------------------------------------------------------------
    // Missing input
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ReturnsError_WhenSurveyFileMissing()
    {
        var result = System2StaticRoadLocalTileSurveyBuilder.Build(
            Path.Combine(_tempDir, "no-such-survey.json"),
            Path.Combine(_tempDir, "no-pz-root"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Survey file not found"));
    }

    // -----------------------------------------------------------------------
    // Missing PZ root → valid zero-candidate survey
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ReturnsValid_WhenPzRootMissing()
    {
        var surveyPath = WriteSurvey(AllSixFamiliesSurvey());
        var result = System2StaticRoadLocalTileSurveyBuilder.Build(
            surveyPath,
            Path.Combine(_tempDir, "no-such-pz-root"));

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void Build_ZeroCandidates_WhenPzRootMissing()
    {
        var surveyPath = WriteSurvey(AllSixFamiliesSurvey());
        var result = System2StaticRoadLocalTileSurveyBuilder.Build(
            surveyPath,
            Path.Combine(_tempDir, "no-such-pz-root"));

        Assert.True(result.IsValid);
        Assert.Equal(0, result.Survey!.Totals.CandidateTileCount);
    }

    // -----------------------------------------------------------------------
    // Family count
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_CreatesSixFamilies_FromAllSixFamilySurvey()
    {
        var surveyPath = WriteSurvey(AllSixFamiliesSurvey());
        var pzRoot     = Path.Combine(_tempDir, "no-such-pz");
        var result     = System2StaticRoadLocalTileSurveyBuilder.Build(surveyPath, pzRoot);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(6, result.Survey!.Families.Count);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ClaimBoundary_AllFalse()
    {
        var surveyPath = WriteSurvey(AllSixFamiliesSurvey());
        var result     = System2StaticRoadLocalTileSurveyBuilder.Build(
            surveyPath, Path.Combine(_tempDir, "no-pz"));

        Assert.True(result.IsValid);
        Assert.False(result.Survey!.ClaimBoundary.WritesLotpack);
        Assert.False(result.Survey.ClaimBoundary.WritesWorldgenLua);
        Assert.False(result.Survey.ClaimBoundary.RuntimeProven);
        Assert.False(result.Survey.ClaimBoundary.PublicPlayableClaim);
    }

    // -----------------------------------------------------------------------
    // Format and status fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_OutputFormat_IsCorrect()
    {
        var surveyPath = WriteSurvey(AllSixFamiliesSurvey());
        var result     = System2StaticRoadLocalTileSurveyBuilder.Build(
            surveyPath, Path.Combine(_tempDir, "no-pz"));

        Assert.True(result.IsValid);
        Assert.Equal("pzmapforge.deadmtl.system2.static-road-local-tile-survey.v1",
            result.Survey!.Format);
        Assert.Equal("LOCAL_TILE_SURVEY_ONLY", result.Survey.Status);
        Assert.Equal("NOT_RUNTIME_PROVEN",     result.Survey.RuntimeStatus);
        Assert.Equal("NOT_IMPLEMENTED",        result.Survey.WriterStatus);
    }

    // -----------------------------------------------------------------------
    // Search terms present in family output
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("asphalt_road_surface_candidate",   "asphalt")]
    [InlineData("asphalt_alley_surface_candidate",  "alley")]
    [InlineData("asphalt_service_lane_candidate",   "service")]
    [InlineData("asphalt_parking_access_candidate", "parking")]
    [InlineData("concrete_or_sidewalk_candidate",   "sidewalk")]
    public void Build_FamilySearchTerms_Present(string familyName, string expectedTerm)
    {
        var surveyPath = WriteSurvey(AllSixFamiliesSurvey());
        var result     = System2StaticRoadLocalTileSurveyBuilder.Build(
            surveyPath, Path.Combine(_tempDir, "no-pz"));

        Assert.True(result.IsValid);
        var family = result.Survey!.Families.Single(f => f.CandidateFamily == familyName);
        Assert.Contains(expectedTerm, family.SearchTerms);
    }

    // -----------------------------------------------------------------------
    // road_node_metadata_candidate stays unresolved even with PZ files
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_RoadNodeFamily_RemainsUnresolved_EvenWithPzFiles()
    {
        var surveyPath = WriteSurvey(AllSixFamiliesSurvey());
        var pzRoot     = MakeFakePzRoot(
            ("media/tiles/road_tiles.tiles", "road_node_intersection_01_0\nroad_node_turn_01_0\n"));

        var result = System2StaticRoadLocalTileSurveyBuilder.Build(surveyPath, pzRoot);

        Assert.True(result.IsValid);
        var nodeFamily = result.Survey!.Families.Single(f => f.CandidateFamily == "road_node_metadata_candidate");
        Assert.Equal("UNRESOLVED_NEEDS_TILE_SURVEY", nodeFamily.ResolutionStatus);
        Assert.Empty(nodeFamily.CandidateTiles);
    }

    // -----------------------------------------------------------------------
    // Fake PZ root → candidates have tile_name, source_file, match_reason, confidence
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_WithFakePzRoot_FindsCandidates_ForAsphaltFamily()
    {
        var surveyPath = WriteSurvey(AllSixFamiliesSurvey());
        var pzRoot     = MakeFakePzRoot(
            ("media/tiles/exterior.tiles",
             "floors_exterior_street_asphalt_01_0\nfloors_exterior_street_asphalt_02_0\n"));

        var result = System2StaticRoadLocalTileSurveyBuilder.Build(surveyPath, pzRoot);

        Assert.True(result.IsValid);
        var family = result.Survey!.Families.Single(f => f.CandidateFamily == "asphalt_road_surface_candidate");
        Assert.NotEmpty(family.CandidateTiles);
        var tile = family.CandidateTiles[0];
        Assert.False(string.IsNullOrEmpty(tile.TileName));
        Assert.False(string.IsNullOrEmpty(tile.SourceFile));
        Assert.False(string.IsNullOrEmpty(tile.MatchReason));
        Assert.Equal("LOCAL_TEXT_MATCH_ONLY", tile.Confidence);
    }

    [Fact]
    public void Build_WithFakePzRoot_FindsCandidates_ForSidewalkFamily()
    {
        var surveyPath = WriteSurvey(AllSixFamiliesSurvey());
        var pzRoot     = MakeFakePzRoot(
            ("media/tiles/ground.tiles",
             "floors_concrete_sidewalk_curb_01_0\nfloors_concrete_pavement_01_0\n"));

        var result = System2StaticRoadLocalTileSurveyBuilder.Build(surveyPath, pzRoot);

        Assert.True(result.IsValid);
        var family = result.Survey!.Families.Single(f => f.CandidateFamily == "concrete_or_sidewalk_candidate");
        Assert.NotEmpty(family.CandidateTiles);
        Assert.Equal("SURVEYED_CANDIDATES_FOUND", family.ResolutionStatus);
    }

    // -----------------------------------------------------------------------
    // Deduplication
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_DeduplicatesCandidates_ByTileName()
    {
        var surveyPath = WriteSurvey(AllSixFamiliesSurvey());
        var pzRoot     = MakeFakePzRoot(
            ("media/tiles/file1.tiles",
             "floors_exterior_street_asphalt_01_0\nfloors_exterior_street_asphalt_01_0\n"),
            ("media/tiles/file2.tiles",
             "floors_exterior_street_asphalt_01_0\n"));

        var result = System2StaticRoadLocalTileSurveyBuilder.Build(surveyPath, pzRoot);

        Assert.True(result.IsValid);
        var family    = result.Survey!.Families.Single(f => f.CandidateFamily == "asphalt_road_surface_candidate");
        var tileNames = family.CandidateTiles.Select(t => t.TileName).ToList();
        Assert.Equal(tileNames.Count, tileNames.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
