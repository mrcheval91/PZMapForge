using System.Drawing;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlRawMapTileInspectorTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-raw-tile-inspector", Path.GetRandomFileName());

    public DeadMtlRawMapTileInspectorTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private string MakePng(int width, int height, Color fill)
    {
        var path = Path.Combine(_tempDir, $"test_{width}x{height}.png");
        using var bmp = new Bitmap(width, height);
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                bmp.SetPixel(x, y, fill);
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return path;
    }

    private string MakeWorldgenPaletteJson(params string[] hexColors)
    {
        var entries = string.Join(",\n", hexColors.Select(h => $"  {{ \"color\": \"{h}\" }}"));
        var json    = $"{{ \"entries\": [{entries}] }}";
        var path    = Path.Combine(_tempDir, "worldgen_palette.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private string MakeSystem2PaletteJson(params string[] hexColors)
    {
        var entries = string.Join(",\n", hexColors.Select(h => $"  {{ \"hex\": \"{h}\" }}"));
        var json    = $"{{ \"entries\": [{entries}] }}";
        var path    = Path.Combine(_tempDir, "system2_palette.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    // -----------------------------------------------------------------------
    // Input validation
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_ReturnsError_WhenImageMissing()
    {
        var result = DeadMtlRawMapTileInspector.Inspect(
            Path.Combine(_tempDir, "no_such_file.png"));
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    // -----------------------------------------------------------------------
    // Basic validity
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_IsValid_OnSolidColorPng()
    {
        var png    = MakePng(256, 256, Color.FromArgb(255, 0x40, 0x40, 0x40));
        var result = DeadMtlRawMapTileInspector.Inspect(png);
        Assert.True(result.IsValid);
        Assert.NotNull(result.Inspection);
    }

    // -----------------------------------------------------------------------
    // Size validation
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_SizeValid_True_WhenExact256x256()
    {
        var png    = MakePng(256, 256, Color.Red);
        var result = DeadMtlRawMapTileInspector.Inspect(png);
        Assert.True(result.Inspection!.SizeValid);
        Assert.Equal(256, result.Inspection.Width);
        Assert.Equal(256, result.Inspection.Height);
    }

    [Fact]
    public void Inspect_SizeValid_False_WhenNotExpectedSize()
    {
        var png    = MakePng(128, 128, Color.Red);
        var result = DeadMtlRawMapTileInspector.Inspect(png);
        Assert.False(result.Inspection!.SizeValid);
    }

    // -----------------------------------------------------------------------
    // SHA256
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_Sha256_IsNonEmpty()
    {
        var png    = MakePng(256, 256, Color.Blue);
        var result = DeadMtlRawMapTileInspector.Inspect(png);
        Assert.NotEmpty(result.Inspection!.Sha256);
        Assert.Equal(64, result.Inspection.Sha256.Length);
    }

    // -----------------------------------------------------------------------
    // has_alpha
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_HasAlpha_False_WhenFullyOpaquePng()
    {
        var png    = MakePng(256, 256, Color.FromArgb(255, 0x40, 0x40, 0x40));
        var result = DeadMtlRawMapTileInspector.Inspect(png);
        Assert.False(result.Inspection!.HasAlpha);
    }

    [Fact]
    public void Inspect_HasAlpha_True_WhenTransparentPixelsPresent()
    {
        var path = Path.Combine(_tempDir, "with_transparent.png");
        using (var bmp = new Bitmap(256, 256))
        {
            for (var y = 0; y < 256; y++)
                for (var x = 0; x < 256; x++)
                    bmp.SetPixel(x, y, x < 128
                        ? Color.FromArgb(0, 0, 0, 0)
                        : Color.FromArgb(255, 0x40, 0x40, 0x40));
            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
        var result = DeadMtlRawMapTileInspector.Inspect(path);
        Assert.True(result.Inspection!.HasAlpha);
    }

    // -----------------------------------------------------------------------
    // Pixel counting
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_OpaquePixelCount_CorrectForSolidPng()
    {
        var png    = MakePng(256, 256, Color.FromArgb(255, 0xFF, 0x00, 0x00));
        var result = DeadMtlRawMapTileInspector.Inspect(png);
        Assert.Equal(256 * 256, result.Inspection!.OpaquePixelCount);
        Assert.Equal(0, result.Inspection.TransparentPixelCount);
    }

    [Fact]
    public void Inspect_UniqueColorCount_IsOne_ForSolidPng()
    {
        var png    = MakePng(256, 256, Color.FromArgb(255, 0x40, 0x40, 0x40));
        var result = DeadMtlRawMapTileInspector.Inspect(png);
        Assert.Equal(1, result.Inspection!.UniqueColorCount);
    }

    // -----------------------------------------------------------------------
    // Top colors
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_TopColors_ContainsExpectedHex()
    {
        var png    = MakePng(256, 256, Color.FromArgb(255, 0x40, 0x40, 0x40));
        var result = DeadMtlRawMapTileInspector.Inspect(png);
        Assert.Single(result.Inspection!.TopColors);
        Assert.Equal("#404040", result.Inspection.TopColors[0].Color,
            StringComparer.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Palette matching
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_PaletteMatches_WorldgenCount_WhenColorPresent()
    {
        var png    = MakePng(256, 256, Color.FromArgb(255, 0x00, 0xAA, 0x00));
        var wg     = MakeWorldgenPaletteJson("#00AA00", "#FF6600");
        var result = DeadMtlRawMapTileInspector.Inspect(png, worldgenPalettePath: wg);
        Assert.Equal(1, result.Inspection!.PaletteMatches.WorldgenPaletteMatchCount);
    }

    [Fact]
    public void Inspect_PaletteMatches_System2Count_WhenColorPresent()
    {
        var png    = MakePng(256, 256, Color.FromArgb(255, 0x40, 0x40, 0x40));
        var s2     = MakeSystem2PaletteJson("#404040", "#B0B0B0");
        var result = DeadMtlRawMapTileInspector.Inspect(png, system2PalettePath: s2);
        Assert.Equal(1, result.Inspection!.PaletteMatches.System2StaticRoadPaletteMatchCount);
    }

    [Fact]
    public void Inspect_UnknownColorCount_IsZero_WhenNoPalettes()
    {
        var png    = MakePng(256, 256, Color.Red);
        var result = DeadMtlRawMapTileInspector.Inspect(png);
        Assert.Equal(0, result.Inspection!.PaletteMatches.UnknownColorCount);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_ClaimBoundary_AllFalse()
    {
        var png    = MakePng(256, 256, Color.Red);
        var result = DeadMtlRawMapTileInspector.Inspect(png);
        var cb     = result.Inspection!.ClaimBoundary;
        Assert.False(cb.WritesLotpack);
        Assert.False(cb.WritesWorldgenLua);
        Assert.False(cb.RuntimeProven);
        Assert.False(cb.PublicPlayableClaim);
        Assert.False(cb.WriterReadyClaim);
    }

    // -----------------------------------------------------------------------
    // Markdown
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderMarkdown_ContainsMap23ATitle()
    {
        var png    = MakePng(256, 256, Color.Red);
        var result = DeadMtlRawMapTileInspector.Inspect(png);
        var md     = DeadMtlRawMapTileInspector.RenderMarkdown(result.Inspection!);
        Assert.Contains("MAP-23A", md, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderMarkdown_ContainsVerdict()
    {
        var png    = MakePng(256, 256, Color.Red);
        var result = DeadMtlRawMapTileInspector.Inspect(png);
        var md     = DeadMtlRawMapTileInspector.RenderMarkdown(result.Inspection!);
        Assert.Contains("MAP23A_RAW_256_MAP_TILE_INSPECTION_COMPLETE", md, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCsv_ContainsRequiredHeader()
    {
        var png    = MakePng(256, 256, Color.Red);
        var result = DeadMtlRawMapTileInspector.Inspect(png);
        var csv    = DeadMtlRawMapTileInspector.RenderCsv(result.Inspection!);
        Assert.Contains(
            "hex_color,count,percentage,in_worldgen_palette,in_system2_palette,is_unknown",
            csv, StringComparison.Ordinal);
    }
}
