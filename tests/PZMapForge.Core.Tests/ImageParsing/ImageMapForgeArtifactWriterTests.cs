using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text.Json;
using PZMapForge.Core.ImageParsing;
using PZMapForge.Core.Palette;
using PZMapForge.Core.ParsedCell;
using Xunit;

namespace PZMapForge.Core.Tests.ImageParsing;

/// <summary>
/// Tests for ImageMapForgeArtifactWriter using programmatically created temp images.
/// Verifies that Write() produces a valid parsed-cell.json loadable by ParsedCellLoader.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ImageMapForgeArtifactWriterTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public ImageMapForgeArtifactWriterTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string PalettePath =>
        Path.Combine(RepoRoot, "source", "image-palette.json");

    private static readonly Color GrassColor = Color.FromArgb(255, 100, 140, 70);

    private string MakeGrassImage300()
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid()}.png");
        using var bmp = new Bitmap(300, 300);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(GrassColor);
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    private string MakeGrassImage150()
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid()}.png");
        using var bmp = new Bitmap(150, 150);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(GrassColor);
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    // -----------------------------------------------------------------------
    // Shared: parse 300x300 grass image and write artifact
    // -----------------------------------------------------------------------

    private (string JsonPath, ImageMapForgeResult Result) WriteDefault()
    {
        var img    = MakeGrassImage300();
        var result = ImageMapForgeParser.Parse(img, PalettePath);

        var paletteResult = PaletteLoader.Load(PalettePath);
        var jsonPath = ImageMapForgeArtifactWriter.Write(
            _tempDir, img, PalettePath, paletteResult.Document!, result);

        return (jsonPath, result);
    }

    // -----------------------------------------------------------------------
    // Test 1: JSON file is created
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_CreatesJsonFile()
    {
        var (json, _) = WriteDefault();
        Assert.True(File.Exists(json));
    }

    // -----------------------------------------------------------------------
    // Test 2: schema sentinel
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_Schema_IsCorrect()
    {
        var (json, _) = WriteDefault();
        var doc = JsonDocument.Parse(File.ReadAllText(json));
        Assert.Equal("pzmapforge.parsed-cell.v0.1",
            doc.RootElement.GetProperty("schema").GetString());
    }

    // -----------------------------------------------------------------------
    // Test 3: claim_boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_ClaimBoundary_IsCorrect()
    {
        var (json, _) = WriteDefault();
        var doc = JsonDocument.Parse(File.ReadAllText(json));
        Assert.Equal("planning_artifact_only_not_pz_load_tested",
            doc.RootElement.GetProperty("claim_boundary").GetString());
    }

    // -----------------------------------------------------------------------
    // Test 4: width and height == 300
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_Dimensions_Are300x300()
    {
        var (json, _) = WriteDefault();
        var doc = JsonDocument.Parse(File.ReadAllText(json));
        Assert.Equal(300, doc.RootElement.GetProperty("width").GetInt32());
        Assert.Equal(300, doc.RootElement.GetProperty("height").GetInt32());
    }

    // -----------------------------------------------------------------------
    // Test 5: rows count == 300
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_RowCount_Is300()
    {
        var (json, _) = WriteDefault();
        var doc  = JsonDocument.Parse(File.ReadAllText(json));
        var rows = doc.RootElement.GetProperty("rows").EnumerateArray().Count();
        Assert.Equal(300, rows);
    }

    // -----------------------------------------------------------------------
    // Test 6: counts sum to 90000
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_CountsSum_Is90000()
    {
        var (json, _) = WriteDefault();
        var doc    = JsonDocument.Parse(File.ReadAllText(json));
        var counts = doc.RootElement.GetProperty("counts").EnumerateArray();
        var total  = counts.Sum(c => c.GetProperty("pixels").GetInt32());
        Assert.Equal(90000, total);
    }

    // -----------------------------------------------------------------------
    // Test 7: resized == true when Resize=true
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_ResizedFlag_TrueWhenResized()
    {
        var img           = MakeGrassImage150();
        var result        = ImageMapForgeParser.Parse(img, PalettePath,
            new ImageMapForgeOptions { Resize = true });
        var paletteResult = PaletteLoader.Load(PalettePath);
        var jsonPath      = ImageMapForgeArtifactWriter.Write(
            _tempDir, img, PalettePath, paletteResult.Document!, result);

        var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
        Assert.True(doc.RootElement.GetProperty("resized").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 8: output is deterministic for same input
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_IsDeterministic()
    {
        var img           = MakeGrassImage300();
        var result        = ImageMapForgeParser.Parse(img, PalettePath);
        var paletteResult = PaletteLoader.Load(PalettePath);

        var dir1 = Path.Combine(_tempDir, "run1");
        var dir2 = Path.Combine(_tempDir, "run2");
        Directory.CreateDirectory(dir1);
        Directory.CreateDirectory(dir2);

        var j1 = ImageMapForgeArtifactWriter.Write(
            dir1, img, PalettePath, paletteResult.Document!, result);
        var j2 = ImageMapForgeArtifactWriter.Write(
            dir2, img, PalettePath, paletteResult.Document!, result);

        Assert.Equal(File.ReadAllText(j1), File.ReadAllText(j2));
    }

    // -----------------------------------------------------------------------
    // Test 9: output is loadable by ParsedCellLoader (structural validity)
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_Output_IsLoadableByParsedCellLoader()
    {
        var img           = MakeGrassImage300();
        var result        = ImageMapForgeParser.Parse(img, PalettePath);
        var paletteResult = PaletteLoader.Load(PalettePath);
        var jsonPath      = ImageMapForgeArtifactWriter.Write(
            _tempDir, img, PalettePath, paletteResult.Document!, result);

        var loaded = ParsedCellLoader.Load(jsonPath);
        Assert.True(loaded.IsValid, string.Join("; ", loaded.Errors));
    }
}
