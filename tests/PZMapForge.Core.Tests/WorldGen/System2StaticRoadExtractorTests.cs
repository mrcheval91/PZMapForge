using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadExtractorTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-system2-extract", Path.GetRandomFileName());

    public System2StaticRoadExtractorTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string ContractJson =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack",
            "system2-static-road-overlay-contract.json");

    private static string IntentPalette =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack",
            "palettes", "system2-static-road-intent-palette.json");

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private string MakeContractJson(string packRoot, string layerFile) =>
        MakeContractJsonMulti(packRoot, new[] { ("static_roads_local", layerFile, "local_street") });

    private string MakeContractJsonMulti(string packRoot,
        (string id, string file, string cls)[] layers)
    {
        var layersJson = string.Join(",\n", layers.Select(l =>
            $$"""{ "id": "{{l.id}}", "file": "{{l.file}}", "class": "{{l.cls}}", "status": "SYSTEM_2_REQUIRED" }"""));
        var json = $$"""
{
  "format": "pzmapforge.deadmtl.system2.static-road-overlay-contract.v1",
  "source_reason": "test",
  "layers": [ {{layersJson}} ]
}
""";
        var path = Path.Combine(_tempDir, "contract.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private string MakePaletteJson(params (string intent, string hex)[] entries)
    {
        var entryJson = string.Join(",\n", entries.Select(e =>
            $$"""{ "intent": "{{e.intent}}", "hex": "{{e.hex}}" }"""));
        var json = $$"""
{
  "format": "pzmapforge.deadmtl.system2.static-road-intent-palette.v1",
  "entries": [ {{entryJson}} ]
}
""";
        var path = Path.Combine(_tempDir, "palette.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private string MakeSolidPng(int w, int h, Color color, string relativePath)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        using var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(color);
        bmp.Save(fullPath, ImageFormat.Png);
        return fullPath;
    }

    private string MakeTransparentPng(int w, int h, string relativePath)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        using var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        bmp.Save(fullPath, ImageFormat.Png);
        return fullPath;
    }

    // -----------------------------------------------------------------------
    // Missing file errors
    // -----------------------------------------------------------------------

    [Fact]
    public void Extract_ReturnsError_WhenContractMissing()
    {
        var result = System2StaticRoadExtractor.Extract(
            contractPath: Path.Combine(_tempDir, "no-contract.json"),
            palettePath:  Path.Combine(_tempDir, "no-palette.json"),
            packRoot:     _tempDir);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Contract file not found"));
    }

    [Fact]
    public void Extract_ReturnsError_WhenPaletteMissing()
    {
        var contract = MakeContractJson(_tempDir, "layers/test.png");
        var result = System2StaticRoadExtractor.Extract(
            contractPath: contract,
            palettePath:  Path.Combine(_tempDir, "no-palette.json"),
            packRoot:     _tempDir);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Palette file not found"));
    }

    [Fact]
    public void Extract_ReturnsError_WhenLayerPngMissing()
    {
        MakeTransparentPng(10, 10, "layers/dummy.png");
        var contract = MakeContractJson(_tempDir, "layers/missing.png");
        var palette  = MakePaletteJson(("local_street_asphalt", "#404040"));

        var result = System2StaticRoadExtractor.Extract(contract, palette, _tempDir);

        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Layer PNG not found"));
    }

    // -----------------------------------------------------------------------
    // Transparent PNG (all-empty layer)
    // -----------------------------------------------------------------------

    [Fact]
    public void Extract_SucceedsWithZeroNonEmptyPixels_WhenLayerIsTransparent()
    {
        MakeTransparentPng(10, 5, "layers/local.png");
        var contract = MakeContractJson(_tempDir, "layers/local.png");
        var palette  = MakePaletteJson(("local_street_asphalt", "#404040"));

        var result = System2StaticRoadExtractor.Extract(contract, palette, _tempDir);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.NotNull(result.Extract);
        Assert.Equal(0, result.Extract!.Totals.NonEmptyPixels);
        Assert.Equal(0, result.Extract.Totals.UnknownOpaquePixels);
        Assert.Single(result.Extract.Layers);
        Assert.Equal("static_roads_local", result.Extract.Layers[0].Id);
        Assert.Equal(0, result.Extract.Layers[0].Runs.Count);
    }

    // -----------------------------------------------------------------------
    // Solid-color PNG -> runs
    // -----------------------------------------------------------------------

    [Fact]
    public void Extract_EmitsHorizontalRun_ForSolidColorRow()
    {
        var color = Color.FromArgb(255, 0x40, 0x40, 0x40);
        MakeSolidPng(5, 3, color, "layers/local.png");
        var contract = MakeContractJson(_tempDir, "layers/local.png");
        var palette  = MakePaletteJson(("local_street_asphalt", "#404040"));

        var result = System2StaticRoadExtractor.Extract(contract, palette, _tempDir);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        var layer = result.Extract!.Layers[0];
        Assert.Equal(15, layer.NonEmptyPixels);
        Assert.Equal(3, layer.Runs.Count);
        var firstRun = layer.Runs[0];
        Assert.Equal(0, firstRun.Y);
        Assert.Equal(0, firstRun.XStart);
        Assert.Equal(4, firstRun.XEnd);
        Assert.Equal("local_street_asphalt", firstRun.Intent);
        Assert.Equal("#404040", firstRun.Color);
    }

    [Fact]
    public void Extract_WorldCoordsOffset_MatchesOrigin()
    {
        var color = Color.FromArgb(255, 0x40, 0x40, 0x40);
        MakeSolidPng(3, 2, color, "layers/local.png");
        var contract = MakeContractJson(_tempDir, "layers/local.png");
        var palette  = MakePaletteJson(("local_street_asphalt", "#404040"));

        var result = System2StaticRoadExtractor.Extract(
            contract, palette, _tempDir, originX: 100, originY: 200);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        var run = result.Extract!.Layers[0].Runs[0];
        Assert.Equal(200, run.WorldY);
        Assert.Equal(100, run.WorldXStart);
        Assert.Equal(102, run.WorldXEnd);
    }

    // -----------------------------------------------------------------------
    // Node layer -> nodes, not runs
    // -----------------------------------------------------------------------

    [Fact]
    public void Extract_EmitsNodes_ForStaticRoadNodesLayer()
    {
        var color = Color.FromArgb(255, 0xFF, 0x00, 0xFF);
        MakeSolidPng(2, 2, color, "layers/nodes.png");
        var contract = MakeContractJsonMulti(_tempDir,
            new[] { ("static_road_nodes", "layers/nodes.png", "intersection_turn_deadend_nodes") });
        var palette = MakePaletteJson(("intersection_node", "#FF00FF"));

        var result = System2StaticRoadExtractor.Extract(contract, palette, _tempDir);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        var layer = result.Extract!.Layers[0];
        Assert.Equal(4, layer.Nodes.Count);
        Assert.Equal(0, layer.Runs.Count);
        var node = layer.Nodes[0];
        Assert.Equal("intersection_node", node.Intent);
        Assert.Equal("#FF00FF", node.Color);
    }

    // -----------------------------------------------------------------------
    // Unknown opaque color
    // -----------------------------------------------------------------------

    [Fact]
    public void Extract_ReturnsInvalid_WhenUnknownOpaqueColorAndFailOnUnknown()
    {
        var color = Color.FromArgb(255, 0x01, 0x02, 0x03);
        MakeSolidPng(2, 2, color, "layers/local.png");
        var contract = MakeContractJson(_tempDir, "layers/local.png");
        var palette  = MakePaletteJson(("local_street_asphalt", "#404040"));

        var result = System2StaticRoadExtractor.Extract(
            contract, palette, _tempDir, failOnUnknownOpaque: true);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Unknown opaque color"));
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Extract_ClaimBoundary_WritesLotpackIsFalse()
    {
        MakeTransparentPng(4, 4, "layers/local.png");
        var contract = MakeContractJson(_tempDir, "layers/local.png");
        var palette  = MakePaletteJson(("local_street_asphalt", "#404040"));

        var result = System2StaticRoadExtractor.Extract(contract, palette, _tempDir);

        Assert.True(result.IsValid);
        Assert.False(result.Extract!.ClaimBoundary.WritesLotpack);
        Assert.False(result.Extract.ClaimBoundary.WritesWorldgenLua);
        Assert.False(result.Extract.ClaimBoundary.RuntimeProven);
        Assert.False(result.Extract.ClaimBoundary.PublicPlayableClaim);
    }

    // -----------------------------------------------------------------------
    // Format and status fields
    // -----------------------------------------------------------------------

    [Fact]
    public void Extract_OutputFormat_IsCorrect()
    {
        MakeTransparentPng(4, 4, "layers/local.png");
        var contract = MakeContractJson(_tempDir, "layers/local.png");
        var palette  = MakePaletteJson(("local_street_asphalt", "#404040"));

        var result = System2StaticRoadExtractor.Extract(contract, palette, _tempDir);

        Assert.True(result.IsValid);
        Assert.Equal("pzmapforge.deadmtl.system2.static-road-extract.v1", result.Extract!.Format);
        Assert.Equal("EXTRACT_ONLY",        result.Extract.Status);
        Assert.Equal("NOT_RUNTIME_PROVEN",  result.Extract.RuntimeStatus);
        Assert.Equal("NOT_IMPLEMENTED",     result.Extract.WriterStatus);
    }

    // -----------------------------------------------------------------------
    // Scale block
    // -----------------------------------------------------------------------

    [Fact]
    public void Extract_Scale_IsOnePixelPerMeter()
    {
        MakeTransparentPng(4, 4, "layers/local.png");
        var contract = MakeContractJson(_tempDir, "layers/local.png");
        var palette  = MakePaletteJson(("local_street_asphalt", "#404040"));

        var result = System2StaticRoadExtractor.Extract(contract, palette, _tempDir);

        Assert.True(result.IsValid);
        Assert.Equal(1, result.Extract!.Scale.PixelsPerMeter);
        Assert.Equal(1, result.Extract.Scale.MetersPerPixel);
        Assert.Equal(1, result.Extract.Scale.PzTilesPerPixel);
    }

    // -----------------------------------------------------------------------
    // JSON serialization round-trip
    // -----------------------------------------------------------------------

    [Fact]
    public void Extract_JsonOutput_ContainsSnakeCaseKeys()
    {
        MakeTransparentPng(4, 4, "layers/local.png");
        var contract = MakeContractJson(_tempDir, "layers/local.png");
        var palette  = MakePaletteJson(("local_street_asphalt", "#404040"));

        var result = System2StaticRoadExtractor.Extract(contract, palette, _tempDir);
        Assert.True(result.IsValid);

        var json = JsonSerializer.Serialize(result.Extract,
            new JsonSerializerOptions { WriteIndented = true });

        Assert.Contains("\"non_empty_pixels\"", json);
        Assert.Contains("\"claim_boundary\"",   json);
        Assert.Contains("\"runtime_status\"",   json);
        Assert.Contains("\"writer_status\"",    json);
        Assert.Contains("\"origin_x\"",         json);
        Assert.Contains("\"origin_y\"",         json);
    }

    // -----------------------------------------------------------------------
    // Totals aggregation
    // -----------------------------------------------------------------------

    [Fact]
    public void Extract_Totals_LayerCountMatchesContractLayers()
    {
        MakeTransparentPng(4, 4, "layers/a.png");
        MakeTransparentPng(4, 4, "layers/b.png");
        var contract = MakeContractJsonMulti(_tempDir, new[]
        {
            ("static_roads_local",  "layers/a.png", "local_street"),
            ("static_roads_alleys", "layers/b.png", "alley_ruelle"),
        });
        var palette = MakePaletteJson(("local_street_asphalt", "#404040"));

        var result = System2StaticRoadExtractor.Extract(contract, palette, _tempDir);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.Extract!.Totals.LayerCount);
    }
}
