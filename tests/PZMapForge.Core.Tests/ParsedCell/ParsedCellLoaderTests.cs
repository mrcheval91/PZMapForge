using System.Text.Json;
using PZMapForge.Core.ParsedCell;
using Xunit;

namespace PZMapForge.Core.Tests.ParsedCell;

public sealed class ParsedCellLoaderTests
{
    // Fixture path: tests/fixtures/parsed-cell/valid.json
    // Resolved from the test binary output directory (5 levels up = repo root)
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string FixtureDir =>
        Path.Combine(RepoRoot, "tests", "fixtures", "parsed-cell");

    private static string ValidFixture => Path.Combine(FixtureDir, "valid.json");

    // -----------------------------------------------------------------------
    // Valid fixture
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_ValidFixture_IsValid()
    {
        var result = ParsedCellLoader.Load(ValidFixture);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.NotNull(result.Document);
        Assert.NotNull(result.Grid);
        Assert.Equal("pzmapforge.parsed-cell.v0.1", result.Document.Schema);
        Assert.Equal(300, result.Document.Width);
        Assert.Equal(300, result.Document.Height);
        Assert.Equal(300, result.Document.Rows.Count);
        Assert.Equal(9, result.Document.Counts.Count);
        Assert.Equal(90000, result.Document.Counts.Sum(c => c.Pixels));
    }

    // -----------------------------------------------------------------------
    // Missing file
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_MissingFile_Fails()
    {
        var result = ParsedCellLoader.Load("/nonexistent/parsed-cell.json");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Inline JSON helpers
    // -----------------------------------------------------------------------

    private static string BuildValidJson(
        string? schema         = "pzmapforge.parsed-cell.v0.1",
        string? claimBoundary  = "planning_artifact_only_not_pz_load_tested",
        int     width          = 300,
        int     height         = 300,
        string[]? rows         = null,
        object[]? counts       = null)
    {
        rows ??= BuildRows(width, height);
        counts ??= BuildCounts();

        return JsonSerializer.Serialize(new
        {
            schema,
            tool                = "ImageMapForge",
            claim_boundary      = claimBoundary,
            source_image        = "fixture",
            source_image_sha256 = new string('0', 64),
            palette             = "fixture",
            palette_sha256      = new string('0', 64),
            width,
            height,
            resized             = false,
            matching            = new { exact_pixels = width * height, nearest_pixels = 0,
                                        unique_source_colours = 9, unmapped_exact_colours = 0 },
            legend              = BuildLegend(),
            counts,
            nearest_drift       = Array.Empty<object>(),
            rows,
            outputs             = new { json = "parsed-cell.json", report = "parsed-cell-report.md",
                                        preview = "parsed-cell-preview.png",
                                        generated_tileset = "parsed-cell-tiles.png",
                                        tmx = "parsed-cell-basic.tmx" }
        });
    }

    private static string[] BuildRows(int width = 300, int height = 300)
    {
        // Row 0: 292 grass + one of each non-grass kind
        var row0 = new string('g', width - 8) + "rshdailp";
        var restRow = new string('g', width);
        var rows = new string[height];
        rows[0] = row0;
        for (var i = 1; i < height; i++) rows[i] = restRow;
        return rows;
    }

    private static object[] BuildLegend() =>
    [
        new { code = "g", kind = "grass",           gid = 1, rgb = new[]{100,140, 70}, description = "" },
        new { code = "r", kind = "road",            gid = 2, rgb = new[]{ 70, 70, 70}, description = "" },
        new { code = "s", kind = "sidewalk",        gid = 3, rgb = new[]{190,180,160}, description = "" },
        new { code = "h", kind = "row_house",       gid = 4, rgb = new[]{160,110, 80}, description = "" },
        new { code = "d", kind = "depanneur",       gid = 5, rgb = new[]{200,130, 60}, description = "" },
        new { code = "a", kind = "garage",          gid = 6, rgb = new[]{ 80, 80,100}, description = "" },
        new { code = "i", kind = "industrial_yard", gid = 7, rgb = new[]{160,130, 90}, description = "" },
        new { code = "l", kind = "landmark",        gid = 8, rgb = new[]{255,220,  0}, description = "" },
        new { code = "p", kind = "spawn",           gid = 9, rgb = new[]{  0,220, 80}, description = "" },
    ];

    private static object[] BuildCounts() =>
    [
        new { kind = "grass",           code = "g", gid = 1, pixels = 89992 },
        new { kind = "road",            code = "r", gid = 2, pixels = 1 },
        new { kind = "sidewalk",        code = "s", gid = 3, pixels = 1 },
        new { kind = "row_house",       code = "h", gid = 4, pixels = 1 },
        new { kind = "depanneur",       code = "d", gid = 5, pixels = 1 },
        new { kind = "garage",          code = "a", gid = 6, pixels = 1 },
        new { kind = "industrial_yard", code = "i", gid = 7, pixels = 1 },
        new { kind = "landmark",        code = "l", gid = 8, pixels = 1 },
        new { kind = "spawn",           code = "p", gid = 9, pixels = 1 },
    ];

    private static ParsedCellLoadResult LoadFromJson(string json)
    {
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, json);
        try { return ParsedCellLoader.Load(tmp); }
        finally { File.Delete(tmp); }
    }

    // -----------------------------------------------------------------------
    // Wrong schema
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_WrongSchema_Fails()
    {
        var result = LoadFromJson(BuildValidJson(schema: "wrong.schema"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("schema", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Wrong claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_WrongClaimBoundary_Fails()
    {
        var result = LoadFromJson(BuildValidJson(claimBoundary: "not_the_right_value"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("claim_boundary", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Wrong dimensions
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_WrongWidth_Fails()
    {
        // Declare width=512 in metadata but keep rows 300 wide
        var result = LoadFromJson(BuildValidJson(width: 512));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("width", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Bad row count
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_BadRowCount_Fails()
    {
        var rows = BuildRows();
        var shortRows = rows[..10];  // only 10 rows instead of 300
        var result = LoadFromJson(BuildValidJson(rows: shortRows));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("rows", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Bad row length
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_BadRowLength_Fails()
    {
        var rows = BuildRows();
        rows[5] = "tooshort";  // row 5 is too short
        var result = LoadFromJson(BuildValidJson(rows: rows));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.Contains("Row 5", StringComparison.OrdinalIgnoreCase) ||
            e.Contains("row 5", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Counts sum mismatch
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_CountsSumMismatch_Fails()
    {
        var counts = BuildCounts().Cast<object>().ToArray();
        // Replace grass count with a wrong value so sum != 90000
        counts[0] = new { kind = "grass", code = "g", gid = 1, pixels = 1 };
        var result = LoadFromJson(BuildValidJson(counts: counts));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("counts", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Missing required kind
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_MissingRequiredKind_Fails()
    {
        // Remove spawn from counts
        var counts = BuildCounts().Where((_, i) => i != 8).ToArray();  // drop spawn
        // Fix pixel sum: add spawn's pixel to grass
        var fixedCounts = new object[]
        {
            new { kind = "grass",           code = "g", gid = 1, pixels = 89993 },  // +1 for missing spawn
            new { kind = "road",            code = "r", gid = 2, pixels = 1 },
            new { kind = "sidewalk",        code = "s", gid = 3, pixels = 1 },
            new { kind = "row_house",       code = "h", gid = 4, pixels = 1 },
            new { kind = "depanneur",       code = "d", gid = 5, pixels = 1 },
            new { kind = "garage",          code = "a", gid = 6, pixels = 1 },
            new { kind = "industrial_yard", code = "i", gid = 7, pixels = 1 },
            new { kind = "landmark",        code = "l", gid = 8, pixels = 1 },
            // spawn omitted
        };
        var result = LoadFromJson(BuildValidJson(counts: fixedCounts));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("spawn", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // SemanticGrid: GetCode
    // -----------------------------------------------------------------------

    [Fact]
    public void SemanticGrid_GetCode_ReturnsExpectedCode()
    {
        var result = ParsedCellLoader.Load(ValidFixture);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Grid);

        // Row 0, first 292 columns are grass ('g')
        Assert.Equal('g', result.Grid.GetCode(0, 0));
        Assert.Equal('g', result.Grid.GetCode(291, 0));

        // Row 0 positions 292-299 hold "rshdailp"
        Assert.Equal('r', result.Grid.GetCode(292, 0));
        Assert.Equal('p', result.Grid.GetCode(299, 0));

        // All of row 1 is grass
        Assert.Equal('g', result.Grid.GetCode(0, 1));
        Assert.Equal('g', result.Grid.GetCode(299, 1));
    }

    // -----------------------------------------------------------------------
    // SemanticGrid: out-of-bounds throws
    // -----------------------------------------------------------------------

    [Fact]
    public void SemanticGrid_GetCode_OutOfBounds_Throws()
    {
        var result = ParsedCellLoader.Load(ValidFixture);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Grid);

        Assert.Throws<ArgumentOutOfRangeException>(() => result.Grid.GetCode(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.Grid.GetCode(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.Grid.GetCode(300, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.Grid.GetCode(0, 300));
    }
}
