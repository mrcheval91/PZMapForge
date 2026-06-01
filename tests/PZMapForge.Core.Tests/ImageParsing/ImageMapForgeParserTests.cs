using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using PZMapForge.Core.ImageParsing;
using Xunit;

namespace PZMapForge.Core.Tests.ImageParsing;

/// <summary>
/// Tests for ImageMapForgeParser using programmatically created temp images.
/// No .local/ state dependency. No committed image fixtures.
/// All temp files cleaned up on dispose.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ImageMapForgeParserTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public ImageMapForgeParserTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // -----------------------------------------------------------------------
    // Palette path
    // -----------------------------------------------------------------------

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string PalettePath =>
        Path.Combine(RepoRoot, "source", "image-palette.json");

    // -----------------------------------------------------------------------
    // Image creation helpers
    // -----------------------------------------------------------------------

    // Grass colour from source/image-palette.json — RGB(100,140,70), code='g'
    private static readonly Color GrassColor = Color.FromArgb(255, 100, 140, 70);
    // Near-grass (not in palette — forces nearest-colour match)
    private static readonly Color NearGrassColor = Color.FromArgb(255, 101, 141, 71);

    private string MakeImage(int width, int height, Color fill, Color? spotColor = null, int spotX = 0, int spotY = 0)
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid()}.png");
        using var bmp = new Bitmap(width, height);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(fill);
        if (spotColor.HasValue)
            bmp.SetPixel(spotX, spotY, spotColor.Value);
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    // -----------------------------------------------------------------------
    // Test 1: 300x300 fixture parses successfully
    // -----------------------------------------------------------------------

    [Fact]
    public void Parse_300x300_Succeeds()
    {
        var img    = MakeImage(300, 300, GrassColor);
        var result = ImageMapForgeParser.Parse(img, PalettePath);

        Assert.Equal(300, result.Width);
        Assert.Equal(300, result.Height);
        Assert.False(result.Resized);
        Assert.Equal(300, result.Rows.Count);
    }

    // -----------------------------------------------------------------------
    // Test 2: 150x150 without Resize fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Parse_150x150_WithoutResize_Throws()
    {
        var img = MakeImage(150, 150, GrassColor);
        Assert.Throws<ArgumentException>(() =>
            ImageMapForgeParser.Parse(img, PalettePath));
    }

    // -----------------------------------------------------------------------
    // Test 3: 150x150 with Resize succeeds
    // -----------------------------------------------------------------------

    [Fact]
    public void Parse_150x150_WithResize_Succeeds()
    {
        var img    = MakeImage(150, 150, GrassColor);
        var result = ImageMapForgeParser.Parse(img, PalettePath,
            new ImageMapForgeOptions { Resize = true });

        Assert.Equal(300, result.Width);
        Assert.Equal(300, result.Height);
        Assert.True(result.Resized);
    }

    // -----------------------------------------------------------------------
    // Test 4: resized result has correct dimensions
    // -----------------------------------------------------------------------

    [Fact]
    public void Parse_Resized_CorrectRowsAndLengths()
    {
        var img    = MakeImage(150, 150, GrassColor);
        var result = ImageMapForgeParser.Parse(img, PalettePath,
            new ImageMapForgeOptions { Resize = true });

        Assert.Equal(300, result.Rows.Count);
        Assert.All(result.Rows, row => Assert.Equal(300, row.Length));
    }

    // -----------------------------------------------------------------------
    // Test 5: counts sum to 90000
    // -----------------------------------------------------------------------

    [Fact]
    public void Parse_CountsSum_Is90000()
    {
        var img    = MakeImage(300, 300, GrassColor);
        var result = ImageMapForgeParser.Parse(img, PalettePath);

        Assert.Equal(90000, result.Counts.Sum(c => c.Pixels));
    }

    // -----------------------------------------------------------------------
    // Test 6: resized == true when Resize=true
    // -----------------------------------------------------------------------

    [Fact]
    public void Parse_ResizedFlag_TrueWhenResized()
    {
        var img150 = MakeImage(150, 150, GrassColor);
        var img300 = MakeImage(300, 300, GrassColor);

        Assert.True(ImageMapForgeParser.Parse(img150, PalettePath,
            new ImageMapForgeOptions { Resize = true }).Resized);
        Assert.False(ImageMapForgeParser.Parse(img300, PalettePath).Resized);
    }

    // -----------------------------------------------------------------------
    // Test 7: palette_sha256 matches source/image-palette.json
    // -----------------------------------------------------------------------

    [Fact]
    public void Parse_PaletteSha256_MatchesFile()
    {
        var img    = MakeImage(300, 300, GrassColor);
        var result = ImageMapForgeParser.Parse(img, PalettePath);

        using var sha    = SHA256.Create();
        using var stream = File.OpenRead(PalettePath);
        var expected = BitConverter.ToString(sha.ComputeHash(stream))
            .Replace("-", string.Empty).ToLowerInvariant();

        Assert.Equal(expected, result.PaletteSha256);
    }

    // -----------------------------------------------------------------------
    // Test 8: invalid file path fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Parse_MissingImage_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ImageMapForgeParser.Parse(
                Path.Combine(_tempDir, "does-not-exist.png"),
                PalettePath));
    }

    // -----------------------------------------------------------------------
    // Test 9: unsupported extension fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Parse_UnsupportedExtension_Throws()
    {
        var path = Path.Combine(_tempDir, "image.jpg");
        File.WriteAllBytes(path, [0xFF, 0xD8, 0xFF]); // fake JPEG header
        Assert.Throws<ArgumentException>(() =>
            ImageMapForgeParser.Parse(path, PalettePath));
    }

    // -----------------------------------------------------------------------
    // Test 10: deterministic output across two parses
    // -----------------------------------------------------------------------

    [Fact]
    public void Parse_IsDeterministic()
    {
        var img = MakeImage(300, 300, GrassColor, NearGrassColor, 150, 150);

        var r1 = ImageMapForgeParser.Parse(img, PalettePath);
        var r2 = ImageMapForgeParser.Parse(img, PalettePath);

        Assert.Equal(r1.Width,   r2.Width);
        Assert.Equal(r1.Height,  r2.Height);
        Assert.Equal(r1.Resized, r2.Resized);

        // Rows must be identical
        Assert.Equal(r1.Rows.Count, r2.Rows.Count);
        for (int i = 0; i < r1.Rows.Count; i++)
            Assert.Equal(r1.Rows[i], r2.Rows[i]);

        // Counts must be identical
        var c1 = r1.Counts.OrderBy(c => c.Gid).ToList();
        var c2 = r2.Counts.OrderBy(c => c.Gid).ToList();
        Assert.Equal(c1.Count, c2.Count);
        for (int i = 0; i < c1.Count; i++)
            Assert.Equal(c1[i].Pixels, c2[i].Pixels);

        // Matching stats must be identical
        Assert.Equal(r1.Matching.ExactPixels,   r2.Matching.ExactPixels);
        Assert.Equal(r1.Matching.NearestPixels, r2.Matching.NearestPixels);
    }
}
