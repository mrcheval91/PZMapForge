using PZMapForge.Core.ParsedCell;
using PZMapForge.Core.Regions;
using Xunit;

namespace PZMapForge.Core.Tests.Regions;

public sealed class RegionExtractorTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string ValidFixture =>
        Path.Combine(RepoRoot, "tests", "fixtures", "parsed-cell", "valid.json");

    private static readonly IReadOnlyDictionary<char, string> DefaultCodeToKind =
        new Dictionary<char, string>
        {
            ['g'] = "grass",
            ['r'] = "road",
            ['s'] = "sidewalk",
            ['h'] = "row_house",
            ['d'] = "depanneur",
            ['a'] = "garage",
            ['i'] = "industrial_yard",
            ['l'] = "landmark",
            ['p'] = "spawn",
        }.AsReadOnly();

    /// <summary>Builds a SemanticGrid from an array of same-length strings.</summary>
    private static SemanticGrid MakeGrid(params string[] rows)
    {
        // SemanticGrid ctor is internal; go through ParsedCellLoader via temp JSON
        int w = rows[0].Length;
        int h = rows.Length;

        // Build a valid parsed-cell JSON for the requested rows
        var counts = new object[]
        {
            new { kind = "grass", code = "g", gid = 1, pixels = rows.Sum(r => r.Count(c => c == 'g')) },
            new { kind = "road",  code = "r", gid = 2, pixels = rows.Sum(r => r.Count(c => c == 'r')) },
        };

        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            schema              = "pzmapforge.parsed-cell.v0.1",
            tool                = "test",
            claim_boundary      = "planning_artifact_only_not_pz_load_tested",
            source_image        = "test",
            source_image_sha256 = new string('0', 64),
            palette             = "test",
            palette_sha256      = new string('0', 64),
            width               = w,
            height              = h,
            resized             = false,
            matching = new { exact_pixels = w * h, nearest_pixels = 0,
                             unique_source_colours = 2, unmapped_exact_colours = 0 },
            legend = new object[]
            {
                new { code = "g", kind = "grass", gid = 1, rgb = new[]{100,140,70}, description = "" },
                new { code = "r", kind = "road",  gid = 2, rgb = new[]{70,70,70},   description = "" },
            },
            counts,
            nearest_drift = Array.Empty<object>(),
            rows,
            outputs = new { json = "parsed-cell.json", report = "r.md",
                            preview = "p.png", generated_tileset = "t.png", tmx = "t.tmx" },
        });

        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, json);
        try
        {
            // ParsedCellLoader validates width/height == 300; for tiny grids we use it directly
            // only when the fixture is already 300x300. For small grids, build the grid manually
            // by reflection (SemanticGrid ctor is internal to Core — accessible within test project
            // via InternalsVisibleTo if added, or we route through a helper).
            // Simplest workaround: use the existing loader and override loader's constants via
            // a factory method — but LoadResult validates 300x300, so for small grids we
            // construct SemanticGrid via its internal ctor using InternalsVisibleTo.
            // Since InternalsVisibleTo is not yet set, we expose a public factory instead.
            return SemanticGrid.CreateForTesting(w, h, rows);
        }
        finally { File.Delete(tmp); }
    }

    private static RegionExtractionResult ExtractFromFixture()
    {
        var loaded = ParsedCellLoader.Load(ValidFixture);
        Assert.True(loaded.IsValid, string.Join("; ", loaded.Errors));

        var codeToKind = loaded.Document!.Counts
            .ToDictionary(c => c.Code[0], c => c.Kind)
            .AsReadOnly();
        return RegionExtractor.Extract(loaded.Grid!, codeToKind);
    }

    // -----------------------------------------------------------------------
    // Test 1: all-grass produces exactly 1 region
    // -----------------------------------------------------------------------

    [Fact]
    public void Extract_AllGrass_OneRegion()
    {
        var grid = MakeGrid(
            "ggg",
            "ggg",
            "ggg");
        var kindMap = new Dictionary<char, string> { ['g'] = "grass" }.AsReadOnly();

        var result = RegionExtractor.Extract(grid, kindMap);

        Assert.Equal(1, result.TotalRegions);
        Assert.Equal(9, result.TotalPixels);
        Assert.Single(result.SummaryByKind);
        Assert.Equal("grass", result.SummaryByKind[0].Kind);
    }

    // -----------------------------------------------------------------------
    // Test 2: valid fixture produces all 9 kinds
    // -----------------------------------------------------------------------

    [Fact]
    public void Extract_ValidFixture_AllNineKinds()
    {
        var result = ExtractFromFixture();

        var kindNames = result.SummaryByKind.Select(s => s.Kind).ToHashSet(StringComparer.Ordinal);
        var required  = new[] { "grass","road","sidewalk","row_house","depanneur",
                                "garage","industrial_yard","landmark","spawn" };
        foreach (var k in required)
            Assert.Contains(k, kindNames);
    }

    // -----------------------------------------------------------------------
    // Test 3: pixel sum == 90000
    // -----------------------------------------------------------------------

    [Fact]
    public void Extract_ValidFixture_PixelSumIs90000()
    {
        var result = ExtractFromFixture();

        Assert.Equal(90000, result.TotalPixels);
    }

    // -----------------------------------------------------------------------
    // Test 4: all regions have positive pixel count
    // -----------------------------------------------------------------------

    [Fact]
    public void Extract_ValidFixture_AllRegionsPositive()
    {
        var result = ExtractFromFixture();

        Assert.All(result.Regions, r => Assert.True(r.PixelCount > 0));
    }

    // -----------------------------------------------------------------------
    // Test 5: all bounds inside 300x300
    // -----------------------------------------------------------------------

    [Fact]
    public void Extract_ValidFixture_BoundsInsideGrid()
    {
        var result = ExtractFromFixture();

        Assert.All(result.Regions, r =>
        {
            var b = r.Bounds;
            Assert.True(b.X >= 0,                $"Region {r.RegionId}: x={b.X} < 0");
            Assert.True(b.Y >= 0,                $"Region {r.RegionId}: y={b.Y} < 0");
            Assert.True(b.X + b.Width  <= 300,   $"Region {r.RegionId}: x+w={b.X+b.Width}");
            Assert.True(b.Y + b.Height <= 300,   $"Region {r.RegionId}: y+h={b.Y+b.Height}");
        });
    }

    // -----------------------------------------------------------------------
    // Test 6: centroid inside bounds (with 0.5 tolerance for rounding)
    // -----------------------------------------------------------------------

    [Fact]
    public void Extract_ValidFixture_CentroidInsideBounds()
    {
        var result = ExtractFromFixture();

        Assert.All(result.Regions, r =>
        {
            var b = r.Bounds;
            var c = r.Centroid;
            Assert.True(b.Contains(c.X, c.Y) || Math.Abs(c.X - b.X) < 1 || Math.Abs(c.Y - b.Y) < 1,
                $"Region {r.RegionId} centroid ({c.X},{c.Y}) outside bounds ({b.X},{b.Y},{b.Width},{b.Height})");
        });
    }

    // -----------------------------------------------------------------------
    // Test 7: deterministic across two runs
    // -----------------------------------------------------------------------

    [Fact]
    public void Extract_ValidFixture_IsDeterministic()
    {
        var result1 = ExtractFromFixture();
        var result2 = ExtractFromFixture();

        Assert.Equal(result1.TotalRegions, result2.TotalRegions);

        for (int i = 0; i < result1.Regions.Count; i++)
        {
            var a = result1.Regions[i];
            var b = result2.Regions[i];
            Assert.Equal(a.RegionId,   b.RegionId);
            Assert.Equal(a.Kind,       b.Kind);
            Assert.Equal(a.PixelCount, b.PixelCount);
            Assert.Equal(a.Bounds.X,   b.Bounds.X);
            Assert.Equal(a.Bounds.Y,   b.Bounds.Y);
            Assert.Equal(a.Centroid.X, b.Centroid.X);
            Assert.Equal(a.Centroid.Y, b.Centroid.Y);
        }
    }

    // -----------------------------------------------------------------------
    // Test 8: 4-neighbor only — diagonally adjacent same-code cells are separate
    // -----------------------------------------------------------------------

    [Fact]
    public void Extract_DiagonalCells_AreSeparateRegions()
    {
        // 'g' cells are only diagonally adjacent, not 4-connected:
        //   g .
        //   . g
        // With 4-neighbor connectivity both 'g' cells are separate regions.
        var grid    = MakeGrid("g.", ".g");
        var kindMap = new Dictionary<char, string>
        {
            ['g'] = "grass",
            ['.'] = "road",
        }.AsReadOnly();

        var result = RegionExtractor.Extract(grid, kindMap);

        var grassRegions = result.Regions.Where(r => r.Code == 'g').ToList();
        Assert.Equal(2, grassRegions.Count);
        Assert.All(grassRegions, r => Assert.Equal(1, r.PixelCount));
    }
}
