using System.Text;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlRawMapTilePaletteMappingBuilderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-palette-mapping", Path.GetRandomFileName());

    public DeadMtlRawMapTilePaletteMappingBuilderTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private string MakeInspectionJson(params (string Hex, int Count, double Pct)[] colors)
    {
        var rows = string.Join(",\n    ", colors.Select(c =>
            $"{{ \"color\": \"{c.Hex}\", \"count\": {c.Count}, \"percentage\": {c.Pct:F2} }}"));
        var json = $@"{{
  ""format"": ""pzmapforge.deadmtl.raw-map-tile-inspection.v1"",
  ""source_image"": ""test.png"",
  ""sha256"": ""abc123"",
  ""width"": 256,
  ""height"": 256,
  ""top_colors"": [
    {rows}
  ]
}}";
        var path = Path.Combine(_tempDir, "inspection.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private string MakeWorldgenPaletteJson(params (string Color, string Type, string Key)[] entries)
    {
        var items = string.Join(",\n  ", entries.Select(e =>
            $"{{ \"color\": \"{e.Color}\", \"type\": \"{e.Type}\", \"key\": \"{e.Key}\" }}"));
        var path = Path.Combine(_tempDir, "worldgen_palette.json");
        File.WriteAllText(path, $"{{ \"entries\": [{items}] }}", Encoding.UTF8);
        return path;
    }

    private string MakeSystem2PaletteJson(params (string Hex, string Intent)[] entries)
    {
        var items = string.Join(",\n  ", entries.Select(e =>
            $"{{ \"hex\": \"{e.Hex}\", \"intent\": \"{e.Intent}\" }}"));
        var path = Path.Combine(_tempDir, "system2_palette.json");
        File.WriteAllText(path, $"{{ \"entries\": [{items}] }}", Encoding.UTF8);
        return path;
    }

    // -----------------------------------------------------------------------
    // Input validation
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ReturnsError_WhenInspectionFileMissing()
    {
        var result = DeadMtlRawMapTilePaletteMappingBuilder.Build(
            Path.Combine(_tempDir, "no_such_file.json"));
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    // -----------------------------------------------------------------------
    // Exact worldgen match
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_AutoMatches_WhenExactWorldgenColorPresent()
    {
        var insp = MakeInspectionJson(("#FF6600", 11378, 17.36));
        var wg   = MakeWorldgenPaletteJson(("#FF6600", "prefab", "normal_road_WE_00"));

        var result = DeadMtlRawMapTilePaletteMappingBuilder.Build(insp, wg);

        Assert.True(result.IsValid);
        var m = Assert.Single(result.Mapping!.Mappings);
        Assert.Equal("AUTO_MATCHED_EXISTING_PALETTE", m.MappingStatus);
        Assert.Equal("WORLDGEN",                     m.TargetSystem);
        Assert.Equal("prefab",                        m.TargetType);
        Assert.Equal("normal_road_WE_00",             m.TargetKey);
        Assert.Equal("EXACT_PALETTE_MATCH_ONLY",      m.Confidence);
    }

    // -----------------------------------------------------------------------
    // Exact System 2 match
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_AutoMatches_WhenExactSystem2ColorPresent()
    {
        var insp = MakeInspectionJson(("#404040", 32768, 50.00));
        var s2   = MakeSystem2PaletteJson(("#404040", "local_street_asphalt"));

        var result = DeadMtlRawMapTilePaletteMappingBuilder.Build(insp, system2PalettePath: s2);

        Assert.True(result.IsValid);
        var m = Assert.Single(result.Mapping!.Mappings);
        Assert.Equal("AUTO_MATCHED_EXISTING_PALETTE", m.MappingStatus);
        Assert.Equal("SYSTEM2_STATIC_ROAD",           m.TargetSystem);
        Assert.Equal("intent",                         m.TargetType);
        Assert.Equal("local_street_asphalt",           m.TargetKey);
        Assert.Equal("EXACT_PALETTE_MATCH_ONLY",       m.Confidence);
    }

    // -----------------------------------------------------------------------
    // Near color stays unmapped
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_LeavesUnmapped_WhenNearColorOnly()
    {
        var insp = MakeInspectionJson(("#00AA10", 10898, 16.63));
        var wg   = MakeWorldgenPaletteJson(("#00AA00", "biome", "grass_plain"));

        var result = DeadMtlRawMapTilePaletteMappingBuilder.Build(insp, wg);

        Assert.True(result.IsValid);
        var m = Assert.Single(result.Mapping!.Mappings);
        Assert.Equal("UNMAPPED_NEEDS_HUMAN_DECISION", m.MappingStatus);
        Assert.Equal("UNMAPPED",                      m.Confidence);
        Assert.Equal("",                              m.TargetSystem);
        Assert.Equal("",                              m.TargetKey);
    }

    [Fact]
    public void Build_DoesNotAutoNormalize_NearColor()
    {
        var insp = MakeInspectionJson(("#00AA10", 10898, 16.63));
        var wg   = MakeWorldgenPaletteJson(("#00AA00", "biome", "grass_plain"));

        var result = DeadMtlRawMapTilePaletteMappingBuilder.Build(insp, wg);

        Assert.True(result.IsValid);
        var m = Assert.Single(result.Mapping!.Mappings);
        Assert.Equal("#00AA10", m.SourceColor);
        Assert.NotEqual("AUTO_MATCHED_EXISTING_PALETTE", m.MappingStatus);
    }

    // -----------------------------------------------------------------------
    // Near-match suggestion for #00AA10
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_AddsSuggestion_WhenNearColorWithinThreshold()
    {
        var insp = MakeInspectionJson(("#00AA10", 10898, 16.63));
        var wg   = MakeWorldgenPaletteJson(("#00AA00", "biome", "grass_plain"));

        var result = DeadMtlRawMapTilePaletteMappingBuilder.Build(insp, wg);

        Assert.True(result.IsValid);
        var m    = Assert.Single(result.Mapping!.Mappings);
        var sugg = Assert.Single(m.NearestPaletteSuggestions);
        Assert.Equal("worldgen",   sugg.Palette);
        Assert.Equal("#00AA00",    sugg.Color);
        Assert.Equal("grass_plain", sugg.Key);
        Assert.Equal(16,           sugg.RgbDistance);
    }

    // -----------------------------------------------------------------------
    // Black remains unmapped with no suggestions
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_LeavesBlackUnmapped_WhenNotInPalette()
    {
        var insp = MakeInspectionJson(("#000000", 888, 1.35));
        var wg   = MakeWorldgenPaletteJson(("#FF6600", "prefab", "normal_road_WE_00"));

        var result = DeadMtlRawMapTilePaletteMappingBuilder.Build(insp, wg);

        Assert.True(result.IsValid);
        var m = Assert.Single(result.Mapping!.Mappings);
        Assert.Equal("UNMAPPED_NEEDS_HUMAN_DECISION", m.MappingStatus);
        Assert.Empty(m.NearestPaletteSuggestions);
    }

    // -----------------------------------------------------------------------
    // Totals
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_TotalsCount_AutoMatchedAndUnmapped()
    {
        var insp = MakeInspectionJson(
            ("#FF6600", 32768, 50.00),
            ("#00AA10", 16384, 25.00),
            ("#000000",  8192, 12.50),
            ("#7200FF",  8192, 12.50));
        var wg = MakeWorldgenPaletteJson(
            ("#FF6600", "prefab", "normal_road_WE_00"),
            ("#00AA00", "biome",  "grass_plain"));

        var result = DeadMtlRawMapTilePaletteMappingBuilder.Build(insp, wg);

        Assert.True(result.IsValid);
        Assert.Equal(4, result.Mapping!.Totals.MappingCount);
        Assert.Equal(1, result.Mapping.Totals.AutoMatchedCount);
        Assert.Equal(3, result.Mapping.Totals.UnmappedCount);
    }

    [Fact]
    public void Build_PixelCountTotal_SumsAllColors()
    {
        var insp = MakeInspectionJson(("#FF6600", 32768, 50.00), ("#000000", 32768, 50.00));

        var result = DeadMtlRawMapTilePaletteMappingBuilder.Build(insp);

        Assert.True(result.IsValid);
        Assert.Equal(65536, result.Mapping!.Totals.PixelCountTotal);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Build_ClaimBoundary_AllFalse()
    {
        var insp = MakeInspectionJson(("#FF6600", 65536, 100.00));

        var result = DeadMtlRawMapTilePaletteMappingBuilder.Build(insp);

        Assert.True(result.IsValid);
        var cb = result.Mapping!.ClaimBoundary;
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
    public void RenderMarkdown_ContainsMap23BTitle()
    {
        var insp   = MakeInspectionJson(("#FF6600", 65536, 100.00));
        var result = DeadMtlRawMapTilePaletteMappingBuilder.Build(insp);
        var md     = DeadMtlRawMapTilePaletteMappingBuilder.RenderMarkdown(result.Mapping!);
        Assert.Contains("MAP-23B", md, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderMarkdown_ContainsWarning()
    {
        var insp   = MakeInspectionJson(("#FF6600", 65536, 100.00));
        var result = DeadMtlRawMapTilePaletteMappingBuilder.Build(insp);
        var md     = DeadMtlRawMapTilePaletteMappingBuilder.RenderMarkdown(result.Mapping!);
        Assert.True(
            md.Contains("WARNING", StringComparison.OrdinalIgnoreCase) ||
            md.Contains("mapping contract only", StringComparison.OrdinalIgnoreCase),
            "markdown should contain a warning about the mapping boundary");
    }

    [Fact]
    public void RenderMarkdown_ContainsVerdict()
    {
        var insp   = MakeInspectionJson(("#FF6600", 65536, 100.00));
        var result = DeadMtlRawMapTilePaletteMappingBuilder.Build(insp);
        var md     = DeadMtlRawMapTilePaletteMappingBuilder.RenderMarkdown(result.Mapping!);
        Assert.Contains("MAP23B_RAW_TILE_PALETTE_MAPPING_CONTRACT_COMPLETE",
            md, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV header
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCsv_ContainsRequiredHeader()
    {
        var insp   = MakeInspectionJson(("#FF6600", 65536, 100.00));
        var result = DeadMtlRawMapTilePaletteMappingBuilder.Build(insp);
        var csv    = DeadMtlRawMapTilePaletteMappingBuilder.RenderCsv(result.Mapping!);
        Assert.Contains(
            "source_color,pixel_count,percentage,mapping_status,target_system,target_type,target_key,confidence,nearest_suggestions,notes",
            csv, StringComparison.Ordinal);
    }
}
