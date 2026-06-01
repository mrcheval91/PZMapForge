using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using PZMapForge.Core.ImageParsing;
using PZMapForge.Core.ParsedCell;
using Xunit;

namespace PZMapForge.Core.Tests.ImageParsing;

/// <summary>
/// Cross-verifies ImageMapForgeParser output against the committed
/// tests/fixtures/parsed-cell/valid.json reference fixture.
///
/// The fixture was generated from a 300x300 image with:
///   Row 0: 292 grass pixels + one each of road/sidewalk/row_house/
///           depanneur/garage/industrial_yard/landmark/spawn
///   Rows 1-299: all grass
///
/// This test reconstructs that input image deterministically and asserts that
/// the .NET parser produces the same rows, counts, and matching stats.
///
/// Note: the fixture's palette_sha256 is a placeholder (64 zeros). The test
/// instead asserts the parser's palette_sha256 matches the actual
/// source/image-palette.json file hash.
///
/// Claim boundary: planning_artifact_only_not_pz_load_tested
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ImageMapForgeParserCrossVerificationTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public ImageMapForgeParserCrossVerificationTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // -----------------------------------------------------------------------
    // Paths
    // -----------------------------------------------------------------------

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string PalettePath =>
        Path.Combine(RepoRoot, "source", "image-palette.json");

    private static string ParsedCellFixture =>
        Path.Combine(RepoRoot, "tests", "fixtures", "parsed-cell", "valid.json");

    // -----------------------------------------------------------------------
    // Palette colours matching source/image-palette.json (exact RGB values)
    // -----------------------------------------------------------------------

    // Row 0 layout: 292 grass pixels + one pixel per non-grass kind in order
    // "rshdailp" (same order as in the committed fixture row 0 suffix)
    private static readonly (string Kind, char Code, int R, int G, int B)[] PaletteKinds =
    [
        ("grass",           'g', 100, 140,  70),  // exact palette match
        ("road",            'r',  70,  70,  70),
        ("sidewalk",        's', 190, 180, 160),
        ("row_house",       'h', 160, 110,  80),
        ("depanneur",       'd', 200, 130,  60),
        ("garage",          'a',  80,  80, 100),
        ("industrial_yard", 'i', 160, 130,  90),
        ("landmark",        'l', 255, 220,   0),
        ("spawn",           'p',   0, 220,  80),
    ];

    // -----------------------------------------------------------------------
    // Image construction helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Builds the 300x300 image corresponding to tests/fixtures/parsed-cell/valid.json.
    /// Row 0: 292 grass + "rshdailp" (one pixel per non-grass kind)
    /// Rows 1-299: all grass
    /// </summary>
    private string CreateFixtureEquivalentImage()
    {
        var path  = Path.Combine(_tempDir, "fixture-equivalent.png");
        var grass = Color.FromArgb(255, 100, 140, 70);

        using var bmp = new Bitmap(300, 300);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(grass);

        // Place one pixel per non-grass kind at columns 292-299 in row 0
        for (int i = 0; i < PaletteKinds.Length - 1; i++)   // skip grass (index 0)
        {
            var (_, _, r, gv, b) = PaletteKinds[i + 1];
            bmp.SetPixel(292 + i, 0, Color.FromArgb(255, r, gv, b));
        }

        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    // -----------------------------------------------------------------------
    // Shared parse result (computed once per test instance)
    // -----------------------------------------------------------------------

    private ImageMapForgeResult? _result;
    private ParsedCellDocument?  _fixture;

    private (ImageMapForgeResult Result, ParsedCellDocument Fixture) GetPair()
    {
        if (_result is null)
        {
            var imgPath = CreateFixtureEquivalentImage();
            _result  = ImageMapForgeParser.Parse(imgPath, PalettePath);

            var loaded = ParsedCellLoader.Load(ParsedCellFixture);
            Assert.True(loaded.IsValid, string.Join("; ", loaded.Errors));
            _fixture = loaded.Document!;
        }
        return (_result!, _fixture!);
    }

    // -----------------------------------------------------------------------
    // Test 1: header fields
    // -----------------------------------------------------------------------

    [Fact]
    public void CrossVerify_HeaderFields_Match()
    {
        var (result, fixture) = GetPair();

        Assert.Equal(fixture.Width,  result.Width);
        Assert.Equal(fixture.Height, result.Height);
        Assert.Equal(fixture.Resized, result.Resized);
        Assert.Equal("planning_artifact_only_not_pz_load_tested",
                     result.ClaimBoundary);
    }

    // -----------------------------------------------------------------------
    // Test 2: palette_sha256 matches actual file (fixture has placeholder zeros)
    // -----------------------------------------------------------------------

    [Fact]
    public void CrossVerify_PaletteSha256_MatchesActualFile()
    {
        var (result, _) = GetPair();

        using var sha    = SHA256.Create();
        using var stream = File.OpenRead(PalettePath);
        var expected = BitConverter.ToString(sha.ComputeHash(stream))
            .Replace("-", string.Empty).ToLowerInvariant();

        Assert.Equal(expected, result.PaletteSha256);
        // Confirm fixture has placeholder (documents the known state)
        Assert.Equal(new string('0', 64), _fixture!.PaletteSha256);
    }

    // -----------------------------------------------------------------------
    // Test 3: all rows match the fixture
    // -----------------------------------------------------------------------

    [Fact]
    public void CrossVerify_AllRows_Match_Fixture()
    {
        var (result, fixture) = GetPair();

        Assert.Equal(fixture.Rows.Count, result.Rows.Count);
        for (int i = 0; i < fixture.Rows.Count; i++)
            Assert.Equal(fixture.Rows[i], result.Rows[i]);
    }

    // -----------------------------------------------------------------------
    // Test 4: counts per kind match the fixture
    // -----------------------------------------------------------------------

    [Fact]
    public void CrossVerify_Counts_Match_Fixture()
    {
        var (result, fixture) = GetPair();

        var fixtureByKind = fixture.Counts.ToDictionary(c => c.Kind, StringComparer.Ordinal);
        var resultByKind  = result.Counts.ToDictionary(c => c.Kind, StringComparer.Ordinal);

        Assert.Equal(fixtureByKind.Count, resultByKind.Count);
        foreach (var (kind, fc) in fixtureByKind)
        {
            Assert.True(resultByKind.TryGetValue(kind, out var rc),
                $"Kind '{kind}' missing from parser result.");
            Assert.Equal(fc.Code,   rc!.Code);
            Assert.Equal(fc.Gid,    rc.Gid);
            Assert.Equal(fc.Pixels, rc.Pixels);
        }
    }

    // -----------------------------------------------------------------------
    // Test 5: matching stats — all exact matches, no nearest
    // -----------------------------------------------------------------------

    [Fact]
    public void CrossVerify_MatchingStats_AllExact()
    {
        var (result, fixture) = GetPair();

        Assert.Equal(fixture.Matching.ExactPixels,          result.Matching.ExactPixels);
        Assert.Equal(fixture.Matching.NearestPixels,        result.Matching.NearestPixels);
        Assert.Equal(fixture.Matching.UniqueSourceColours,  result.Matching.UniqueSourceColours);
        Assert.Equal(fixture.Matching.UnmappedExactColours, result.Matching.UnmappedExactColours);

        // Sanity: all pixels are exact matches in this fixture
        Assert.Equal(90000, result.Matching.ExactPixels);
        Assert.Equal(0,     result.Matching.NearestPixels);
    }

    // -----------------------------------------------------------------------
    // Test 6: resize cross-check — 150x150 all-grass → 300x300
    // -----------------------------------------------------------------------

    [Fact]
    public void CrossVerify_Resize_150x150_Produces_300x300_AllGrass()
    {
        var path  = Path.Combine(_tempDir, "grass-150.png");
        var grass = Color.FromArgb(255, 100, 140, 70);

        using (var bmp = new Bitmap(150, 150))
        {
            using var g = Graphics.FromImage(bmp);
            g.Clear(grass);
            bmp.Save(path, ImageFormat.Png);
        }

        var result = ImageMapForgeParser.Parse(path, PalettePath,
            new ImageMapForgeOptions { Resize = true });

        Assert.Equal(300, result.Width);
        Assert.Equal(300, result.Height);
        Assert.True(result.Resized);
        Assert.Equal(90000, result.Counts.Sum(c => c.Pixels));

        // All pixels should be grass after resize of a solid-grass image
        var grassCount = result.Counts.FirstOrDefault(c => c.Kind == "grass");
        Assert.NotNull(grassCount);
        Assert.Equal(90000, grassCount!.Pixels);
    }
}
