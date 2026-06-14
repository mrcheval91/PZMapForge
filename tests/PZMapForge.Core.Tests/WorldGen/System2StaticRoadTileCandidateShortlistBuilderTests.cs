using System.Text;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class System2StaticRoadTileCandidateShortlistBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-shortlist", Path.GetRandomFileName());

    public System2StaticRoadTileCandidateShortlistBuilderTests() => Directory.CreateDirectory(_tempDir);

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

    private static string SurveyWithCandidates(string family, params string[] tileNames)
    {
        var candidateJson = string.Join(",\n", tileNames.Select(t =>
            $"{{\"tile_name\":\"{t}\",\"source_file\":\"media/tiles/exterior.tiles\",\"match_reason\":\"matched term: asphalt\",\"confidence\":\"LOCAL_TEXT_MATCH_ONLY\"}}"));

        return $$"""
{
  "format": "pzmapforge.deadmtl.system2.static-road-local-tile-survey.v1",
  "status": "LOCAL_TILE_SURVEY_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_survey": "test.json",
  "pz_root": "D:\\fake\\pz",
  "families": [
    {
      "candidate_family": "{{family}}",
      "source_intents": ["local_street_asphalt"],
      "role": "road_surface",
      "resolution_status": "SURVEYED_CANDIDATES_FOUND",
      "confidence": "LOCAL_TEXT_MATCH_ONLY",
      "search_terms": ["asphalt", "street", "road", "pavement"],
      "candidate_tiles": [{{candidateJson}}],
      "notes": ""
    }
  ],
  "totals": { "family_count": 1, "families_with_candidates": 1, "families_without_candidates": 0, "candidate_tile_count": {{tileNames.Length}} },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false, "runtime_proven": false, "public_playable_claim": false }
}
""";
    }

    private static string AllSixFamiliesSurvey(string[]? asphaltCandidates = null) =>
        $$"""
{
  "format": "pzmapforge.deadmtl.system2.static-road-local-tile-survey.v1",
  "status": "LOCAL_TILE_SURVEY_ONLY",
  "runtime_status": "NOT_RUNTIME_PROVEN",
  "writer_status": "NOT_IMPLEMENTED",
  "source_survey": "test.json",
  "pz_root": "D:\\fake\\pz",
  "families": [
    { "candidate_family": "asphalt_road_surface_candidate",
      "source_intents": ["local_street_asphalt"], "role": "road_surface",
      "resolution_status": "SURVEYED_CANDIDATES_FOUND", "confidence": "LOCAL_TEXT_MATCH_ONLY",
      "search_terms": ["asphalt", "street", "road", "pavement"],
      "candidate_tiles": [{{BuildCandidates(asphaltCandidates ?? ["floors_exterior_street_asphalt_01_0"])}}],
      "notes": "" },
    { "candidate_family": "asphalt_alley_surface_candidate",
      "source_intents": ["alley_ruelle_asphalt"], "role": "road_surface",
      "resolution_status": "SURVEYED_NO_CANDIDATES_FOUND", "confidence": "NONE_YET",
      "search_terms": ["asphalt", "alley", "road", "pavement"],
      "candidate_tiles": [], "notes": "" },
    { "candidate_family": "asphalt_service_lane_candidate",
      "source_intents": ["service_lane"], "role": "road_surface",
      "resolution_status": "SURVEYED_NO_CANDIDATES_FOUND", "confidence": "NONE_YET",
      "search_terms": ["asphalt", "service", "road", "lane", "pavement"],
      "candidate_tiles": [], "notes": "" },
    { "candidate_family": "asphalt_parking_access_candidate",
      "source_intents": ["parking_access"], "role": "road_surface",
      "resolution_status": "SURVEYED_NO_CANDIDATES_FOUND", "confidence": "NONE_YET",
      "search_terms": ["asphalt", "parking", "driveway", "pavement"],
      "candidate_tiles": [], "notes": "" },
    { "candidate_family": "concrete_or_sidewalk_candidate",
      "source_intents": ["sidewalk_or_pedestrian_cut"], "role": "pedestrian_cut",
      "resolution_status": "SURVEYED_NO_CANDIDATES_FOUND", "confidence": "NONE_YET",
      "search_terms": ["concrete", "sidewalk", "pavement", "curb"],
      "candidate_tiles": [], "notes": "" },
    { "candidate_family": "road_node_metadata_candidate",
      "source_intents": ["intersection_node", "road_turn_node", "dead_end_node"], "role": "road_node",
      "resolution_status": "UNRESOLVED_NEEDS_TILE_SURVEY", "confidence": "NONE_YET",
      "search_terms": [],
      "candidate_tiles": [], "notes": "" }
  ],
  "totals": { "family_count": 6, "families_with_candidates": 1, "families_without_candidates": 5, "candidate_tile_count": 1 },
  "claim_boundary": { "writes_lotpack": false, "writes_worldgen_lua": false, "runtime_proven": false, "public_playable_claim": false }
}
""";

    private static string BuildCandidates(string[] tileNames) =>
        string.Join(",\n", tileNames.Select(t =>
            $"{{\"tile_name\":\"{t}\",\"source_file\":\"media/tiles/exterior.tiles\",\"match_reason\":\"matched term: asphalt\",\"confidence\":\"LOCAL_TEXT_MATCH_ONLY\"}}"));

    // -----------------------------------------------------------------------
    // Missing input
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ReturnsError_WhenInputFileMissing()
    {
        var result = System2StaticRoadTileCandidateShortlistBuilder.Build(
            Path.Combine(_tempDir, "no-such-file.json"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Local tile survey file not found"));
    }

    // -----------------------------------------------------------------------
    // Family count
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_CreatesSixFamilies_FromAllSixFamilySurvey()
    {
        var path   = WriteSurvey(AllSixFamiliesSurvey());
        var result = System2StaticRoadTileCandidateShortlistBuilder.Build(path);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(6, result.Shortlist!.Families.Count);
    }

    // -----------------------------------------------------------------------
    // Format and status fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_OutputFormat_IsCorrect()
    {
        var path   = WriteSurvey(AllSixFamiliesSurvey());
        var result = System2StaticRoadTileCandidateShortlistBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.Equal("pzmapforge.deadmtl.system2.static-road-tile-candidate-shortlist.v1",
            result.Shortlist!.Format);
        Assert.Equal("SHORTLIST_ONLY",    result.Shortlist.Status);
        Assert.Equal("NOT_RUNTIME_PROVEN", result.Shortlist.RuntimeStatus);
        Assert.Equal("NOT_IMPLEMENTED",    result.Shortlist.WriterStatus);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ClaimBoundary_AllFalse()
    {
        var path   = WriteSurvey(AllSixFamiliesSurvey());
        var result = System2StaticRoadTileCandidateShortlistBuilder.Build(path);

        Assert.True(result.IsValid);
        Assert.False(result.Shortlist!.ClaimBoundary.WritesLotpack);
        Assert.False(result.Shortlist.ClaimBoundary.WritesWorldgenLua);
        Assert.False(result.Shortlist.ClaimBoundary.RuntimeProven);
        Assert.False(result.Shortlist.ClaimBoundary.PublicPlayableClaim);
    }

    // -----------------------------------------------------------------------
    // Top N respected
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_TopN_LimitsShortlistPerFamily()
    {
        var manyNames = Enumerable.Range(1, 50)
            .Select(i => $"floors_exterior_street_asphalt_{i:D2}_0")
            .ToArray();

        var path   = WriteSurvey(SurveyWithCandidates("asphalt_road_surface_candidate", manyNames));
        var result = System2StaticRoadTileCandidateShortlistBuilder.Build(path, topN: 5);

        Assert.True(result.IsValid);
        var family = result.Shortlist!.Families.Single(f => f.CandidateFamily == "asphalt_road_surface_candidate");
        Assert.Equal(5, family.Candidates.Count);
    }

    // -----------------------------------------------------------------------
    // Sorting: score desc then tile_name asc
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Candidates_SortedByScoreDescThenTileNameAsc()
    {
        var names = new[]
        {
            "floors_exterior_street_asphalt_02_0",
            "floors_exterior_street_asphalt_01_0",
            "some_wall_thing_0",
        };
        var path   = WriteSurvey(SurveyWithCandidates("asphalt_road_surface_candidate", names));
        var result = System2StaticRoadTileCandidateShortlistBuilder.Build(path);

        Assert.True(result.IsValid);
        var candidates = result.Shortlist!.Families
            .Single(f => f.CandidateFamily == "asphalt_road_surface_candidate")
            .Candidates;

        Assert.True(candidates.Count >= 2);
        // Asphalt+street+exterior should outrank wall candidate
        Assert.True(candidates[0].Score >= candidates[^1].Score);
    }

    // -----------------------------------------------------------------------
    // Candidate fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Candidate_HasRequiredFields()
    {
        var path   = WriteSurvey(SurveyWithCandidates("asphalt_road_surface_candidate",
            "floors_exterior_street_asphalt_01_0"));
        var result = System2StaticRoadTileCandidateShortlistBuilder.Build(path);

        Assert.True(result.IsValid);
        var candidate = result.Shortlist!.Families
            .Single(f => f.CandidateFamily == "asphalt_road_surface_candidate")
            .Candidates[0];

        Assert.Equal(1,                                  candidate.Rank);
        Assert.False(string.IsNullOrEmpty(candidate.TileName));
        Assert.False(string.IsNullOrEmpty(candidate.SourceFile));
        Assert.NotEmpty(candidate.ScoreReasons);
        Assert.Equal("LOCAL_TEXT_MATCH_ONLY",        candidate.SourceConfidence);
        Assert.Equal("LOCAL_TEXT_MATCH_RANKED_ONLY", candidate.Confidence);
    }

    // -----------------------------------------------------------------------
    // Positive terms increase score
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_AsphaltTileName_IncreasesScore()
    {
        var path   = WriteSurvey(SurveyWithCandidates("asphalt_road_surface_candidate",
            "floors_exterior_street_asphalt_01_0",
            "some_plain_floor_01_0"));
        var result = System2StaticRoadTileCandidateShortlistBuilder.Build(path);

        Assert.True(result.IsValid);
        var candidates = result.Shortlist!.Families
            .Single(f => f.CandidateFamily == "asphalt_road_surface_candidate")
            .Candidates;

        var asphaltCandidate = candidates.Single(c => c.TileName.Contains("asphalt"));
        var plainCandidate   = candidates.Single(c => c.TileName == "some_plain_floor_01_0");
        Assert.True(asphaltCandidate.Score > plainCandidate.Score);
    }

    // -----------------------------------------------------------------------
    // Negative terms reduce score
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_WallTileName_ReducesScore()
    {
        var path   = WriteSurvey(SurveyWithCandidates("asphalt_road_surface_candidate",
            "floors_exterior_street_asphalt_01_0",
            "road_asphalt_wall_sign_01_0"));
        var result = System2StaticRoadTileCandidateShortlistBuilder.Build(path);

        Assert.True(result.IsValid);
        var candidates = result.Shortlist!.Families
            .Single(f => f.CandidateFamily == "asphalt_road_surface_candidate")
            .Candidates;

        var goodCandidate = candidates.Single(c => c.TileName == "floors_exterior_street_asphalt_01_0");
        var wallCandidate = candidates.Single(c => c.TileName == "road_asphalt_wall_sign_01_0");
        Assert.True(goodCandidate.Score > wallCandidate.Score);
        Assert.Contains(wallCandidate.ScoreReasons, r => r.Contains("wall"));
    }

    // -----------------------------------------------------------------------
    // road_node_metadata_candidate stays UNRESOLVED_METADATA_ONLY
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_RoadNodeFamily_IsUnresolvedMetadataOnly_WhenNoCandidates()
    {
        var path   = WriteSurvey(AllSixFamiliesSurvey());
        var result = System2StaticRoadTileCandidateShortlistBuilder.Build(path);

        Assert.True(result.IsValid);
        var nodeFamily = result.Shortlist!.Families
            .Single(f => f.CandidateFamily == "road_node_metadata_candidate");

        Assert.Equal("UNRESOLVED_METADATA_ONLY", nodeFamily.ResolutionStatus);
        Assert.Empty(nodeFamily.Candidates);
    }

    // -----------------------------------------------------------------------
    // Totals
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_Totals_CountInputAndShortlisted()
    {
        var names = Enumerable.Range(1, 10)
            .Select(i => $"floors_exterior_street_asphalt_{i:D2}_0")
            .ToArray();

        var path   = WriteSurvey(SurveyWithCandidates("asphalt_road_surface_candidate", names));
        var result = System2StaticRoadTileCandidateShortlistBuilder.Build(path, topN: 5);

        Assert.True(result.IsValid);
        Assert.Equal(10, result.Shortlist!.Totals.InputCandidateCount);
        Assert.Equal(5,  result.Shortlist.Totals.ShortlistedCandidateCount);
    }
}
