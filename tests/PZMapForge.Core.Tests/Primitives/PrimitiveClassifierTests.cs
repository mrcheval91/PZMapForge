using System.Text.Json;
using System.Text.Json.Serialization;
using PZMapForge.Core.ParsedCell;
using PZMapForge.Core.Primitives;
using PZMapForge.Core.Regions;
using Xunit;

namespace PZMapForge.Core.Tests.Primitives;

public sealed class PrimitiveClassifierTests
{
    // -----------------------------------------------------------------------
    // Fixture paths
    // -----------------------------------------------------------------------

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string ParsedCellFixture =>
        Path.Combine(RepoRoot, "tests", "fixtures", "parsed-cell", "valid.json");

    private static string PrimitivesFixture =>
        Path.Combine(RepoRoot, "tests", "fixtures", "primitives", "valid.json");

    // -----------------------------------------------------------------------
    // Shared helper: run full pipeline from parsed-cell fixture
    // -----------------------------------------------------------------------

    private static PrimitiveClassificationResult ClassifyFromFixture()
    {
        var loaded = ParsedCellLoader.Load(ParsedCellFixture);
        Assert.True(loaded.IsValid, string.Join("; ", loaded.Errors));

        var codeToKind = loaded.Document!.Counts
            .ToDictionary(c => c.Code[0], c => c.Kind)
            .AsReadOnly();

        var regions = RegionExtractor.Extract(loaded.Grid!, codeToKind);
        return PrimitiveClassifier.Classify(regions);
    }

    // -----------------------------------------------------------------------
    // Test 1: primitive count from valid fixture
    // -----------------------------------------------------------------------

    [Fact]
    public void Classify_ValidFixture_CorrectPrimitiveCount()
    {
        var result = ClassifyFromFixture();

        // valid.json has 9 regions → 9 primitives (1:1 mapping)
        Assert.Equal(9, result.PrimitiveCount);
    }

    // -----------------------------------------------------------------------
    // Test 2: all 9 kinds classify to expected primitive type
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("grass",           "ground_region")]
    [InlineData("road",            "road_region")]
    [InlineData("sidewalk",        "sidewalk_region")]
    [InlineData("row_house",       "building_footprint")]
    [InlineData("depanneur",       "building_footprint")]
    [InlineData("garage",          "building_footprint")]
    [InlineData("industrial_yard", "yard_region")]
    [InlineData("landmark",        "landmark_marker")]
    [InlineData("spawn",           "spawn_marker")]
    public void Classify_KindMapsToExpectedPrimitiveType(string kind, string expectedTypeStr)
    {
        var result = ClassifyFromFixture();

        var prim = result.Primitives.FirstOrDefault(p => p.Kind == kind);
        Assert.NotNull(prim);
        Assert.Equal(expectedTypeStr, prim.PrimitiveTypeStr);
    }

    // -----------------------------------------------------------------------
    // Test 3: total pixel coverage preserved
    // -----------------------------------------------------------------------

    [Fact]
    public void Classify_ValidFixture_TotalPixelsCoverAll()
    {
        var result = ClassifyFromFixture();

        Assert.Equal(90000, result.TotalPixels);
    }

    // -----------------------------------------------------------------------
    // Test 4: deterministic ordering across two runs
    // -----------------------------------------------------------------------

    [Fact]
    public void Classify_IsDeterministic()
    {
        var r1 = ClassifyFromFixture();
        var r2 = ClassifyFromFixture();

        Assert.Equal(r1.PrimitiveCount, r2.PrimitiveCount);
        for (int i = 0; i < r1.Primitives.Count; i++)
        {
            Assert.Equal(r1.Primitives[i].PrimitiveId,      r2.Primitives[i].PrimitiveId);
            Assert.Equal(r1.Primitives[i].PrimitiveTypeStr, r2.Primitives[i].PrimitiveTypeStr);
            Assert.Equal(r1.Primitives[i].Kind,             r2.Primitives[i].Kind);
            Assert.Equal(r1.Primitives[i].PixelCount,       r2.Primitives[i].PixelCount);
        }
    }

    // -----------------------------------------------------------------------
    // Test 5: no unknown/unclassified regions
    // -----------------------------------------------------------------------

    [Fact]
    public void Classify_NoUnclassifiedRegions()
    {
        var result = ClassifyFromFixture();

        // Every primitive must have a non-empty type string
        Assert.All(result.Primitives, p =>
            Assert.False(string.IsNullOrWhiteSpace(p.PrimitiveTypeStr)));
    }

    // -----------------------------------------------------------------------
    // Test 6: unknown kind throws ArgumentException
    // -----------------------------------------------------------------------

    [Fact]
    public void Classify_UnknownKind_Throws()
    {
        // Build a minimal RegionExtractionResult with one region of kind "unknown_kind"
        var unknownRegion = SemanticRegion.CreateForTesting(
            "unknown_kind", 'z', 1,
            new RegionBounds(0, 0, 1, 1),
            new RegionCentroid(0, 0), regionId: 1);

        var fakeResult = RegionExtractionResult.CreateForTesting(
            [unknownRegion],
            [RegionKindSummary.CreateForTesting("unknown_kind", 'z', 1, 1, 1)]);

        Assert.Throws<ArgumentException>(() => PrimitiveClassifier.Classify(fakeResult));
    }

    // -----------------------------------------------------------------------
    // Test 7: building_footprint summary aggregates row_house + depanneur + garage
    // -----------------------------------------------------------------------

    [Fact]
    public void Classify_BuildingFootprintSummaryAggregates3Kinds()
    {
        var result = ClassifyFromFixture();

        var bfSummary = result.SummaryByPrimitiveType
            .FirstOrDefault(s => s.PrimitiveTypeString == "building_footprint");

        Assert.NotNull(bfSummary);
        Assert.Equal(3, bfSummary.RegionCount);    // row_house, depanneur, garage
        Assert.Equal(3, bfSummary.TotalPixels);
    }

    // -----------------------------------------------------------------------
    // Test 8: PS cross-verification — all primitives match reference fixture
    // -----------------------------------------------------------------------

    [Fact]
    public void Classify_MatchesPsReferenceFixture()
    {
        var dotnet = ClassifyFromFixture();

        var psDoc  = JsonSerializer.Deserialize<PsPrimitivesDoc>(
            File.ReadAllText(PrimitivesFixture), JsonOpts)!;

        // Count
        Assert.Equal(psDoc.PrimitiveCount, dotnet.PrimitiveCount);

        // Summary_by_primitive_type
        var psSum  = psDoc.SummaryByPrimitiveType.ToDictionary(s => s.PrimitiveType, StringComparer.Ordinal);
        var dotSum = dotnet.SummaryByPrimitiveType.ToDictionary(s => s.PrimitiveTypeString, StringComparer.Ordinal);
        Assert.Equal(psSum.Count, dotSum.Count);
        foreach (var (typeStr, psSummary) in psSum)
        {
            Assert.True(dotSum.TryGetValue(typeStr, out var ds),
                $"Summary type '{typeStr}' missing from .NET output.");
            Assert.Equal(psSummary.RegionCount,         ds!.RegionCount);
            Assert.Equal(psSummary.TotalPixels,         ds.TotalPixels);
            Assert.Equal(psSummary.LargestRegionPixels, ds.LargestRegionPixels);
        }

        // Per-primitive details (all 9 for the valid fixture, cap at 20)
        int n = Math.Min(psDoc.Primitives.Count, Math.Min(dotnet.PrimitiveCount, 20));
        for (int i = 0; i < n; i++)
        {
            var ps  = psDoc.Primitives[i];
            var dot = dotnet.Primitives[i];
            var lbl = $"primitive[{i}]";

            Assert.Equal(ps.PrimitiveType,    dot.PrimitiveTypeStr);
            Assert.Equal(ps.Kind,             dot.Kind);
            Assert.Equal(ps.Code,             dot.Code.ToString());
            Assert.Equal(ps.PixelCount,       dot.PixelCount);
            Assert.Equal(ps.SourceRegionId,   dot.SourceRegionId);
            Assert.Equal(ps.Bounds.X,         dot.Bounds.X);
            Assert.Equal(ps.Bounds.Y,         dot.Bounds.Y);
            Assert.Equal(ps.Bounds.Width,     dot.Bounds.Width);
            Assert.Equal(ps.Bounds.Height,    dot.Bounds.Height);
            Assert.Equal(ps.Centroid.X,       dot.Centroid.X, 2);
            Assert.Equal(ps.Centroid.Y,       dot.Centroid.Y, 2);
            Assert.Equal(ps.PlanningRole,     dot.PlanningRole);
        }
    }

    // -----------------------------------------------------------------------
    // Minimal deserialization models for the PS primitives.json fixture
    // -----------------------------------------------------------------------

    private static readonly JsonSerializerOptions JsonOpts = new()
    { AllowTrailingCommas = true };

    private sealed class PsPrimitivesDoc
    {
        [JsonPropertyName("primitive_count")]
        public int PrimitiveCount { get; set; }

        [JsonPropertyName("primitives")]
        public List<PsPrimitive> Primitives { get; set; } = [];

        [JsonPropertyName("summary_by_primitive_type")]
        public List<PsSummary> SummaryByPrimitiveType { get; set; } = [];
    }

    private sealed class PsPrimitive
    {
        [JsonPropertyName("primitive_id")]     public int    PrimitiveId     { get; set; }
        [JsonPropertyName("primitive_type")]   public string PrimitiveType   { get; set; } = "";
        [JsonPropertyName("source_region_id")] public int   SourceRegionId  { get; set; }
        [JsonPropertyName("kind")]             public string Kind            { get; set; } = "";
        [JsonPropertyName("code")]             public string Code            { get; set; } = "";
        [JsonPropertyName("pixel_count")]      public int    PixelCount      { get; set; }
        [JsonPropertyName("bounds")]           public PsBounds Bounds        { get; set; } = new();
        [JsonPropertyName("centroid")]         public PsCentroid Centroid    { get; set; } = new();
        [JsonPropertyName("planning_role")]    public string PlanningRole    { get; set; } = "";
    }

    private sealed class PsBounds
    {
        [JsonPropertyName("x")]      public int X      { get; set; }
        [JsonPropertyName("y")]      public int Y      { get; set; }
        [JsonPropertyName("width")]  public int Width  { get; set; }
        [JsonPropertyName("height")] public int Height { get; set; }
    }

    private sealed class PsCentroid
    {
        [JsonPropertyName("x")] public double X { get; set; }
        [JsonPropertyName("y")] public double Y { get; set; }
    }

    private sealed class PsSummary
    {
        [JsonPropertyName("primitive_type")]        public string PrimitiveType       { get; set; } = "";
        [JsonPropertyName("region_count")]          public int    RegionCount         { get; set; }
        [JsonPropertyName("total_pixels")]          public int    TotalPixels         { get; set; }
        [JsonPropertyName("largest_region_pixels")] public int    LargestRegionPixels { get; set; }
    }
}
