using PZMapForge.Core.Layers;
using PZMapForge.Core.Palette;
using PZMapForge.Core.ParsedCell;
using Xunit;

namespace PZMapForge.Core.Tests.Layers;

public sealed class LayerMergeArtifactWriterTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public LayerMergeArtifactWriterTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string PalettePath =>
        Path.Combine(RepoRoot, "source", "image-palette.json");

    private static string ValidFixture =>
        Path.Combine(RepoRoot, "tests", "fixtures", "layers", "valid-layer-manifest.json");

    private PaletteDocument Palette =>
        PaletteLoader.Load(PalettePath).Document!;

    // Build a minimal all-grass LayerMergeResult without running GDI+ image parsing.
    private static LayerMergeResult MakeGrassResult(int w = 300, int h = 300)
    {
        var rows = Enumerable.Repeat(new string('g', w), h).ToList();
        return new LayerMergeResult
        {
            Width              = w,
            Height             = h,
            Rows               = rows,
            Grid               = SemanticGrid.CreateForTesting(w, h, rows),
            Contributions      =
            [
                new LayerMergeContribution
                {
                    LayerName            = "terrain",
                    FilePath             = "terrain.png",
                    ContributedPixels    = 0,
                    IgnoredDefaultPixels = w * h,
                    ChosenPixels         = 0,
                    OverriddenPixels     = 0,
                },
            ],
            TotalConflictCount = 0,
            ConflictSample     = [],
        };
    }

    private (string ParsedCellPath, string MdPath) WriteDefault()
    {
        var result = MakeGrassResult();
        return LayerMergeArtifactWriter.Write(
            _tempDir, ValidFixture, PalettePath, Palette, result);
    }

    // -----------------------------------------------------------------------
    // Tests 1-2: file creation
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_CreatesLayerMergeReportMd()
    {
        var (_, md) = WriteDefault();
        Assert.True(File.Exists(md));
    }

    [Fact]
    public void Write_CreatesParsedCellJson()
    {
        var (json, _) = WriteDefault();
        Assert.True(File.Exists(json));
    }

    // -----------------------------------------------------------------------
    // Tests 3-5: report content
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_Report_ContainsClaimBoundary()
    {
        var (_, md) = WriteDefault();
        Assert.Contains(
            "planning_artifact_only_not_pz_load_tested",
            File.ReadAllText(md), StringComparison.Ordinal);
    }

    [Fact]
    public void Write_Report_ContainsContributionTable()
    {
        var (_, md) = WriteDefault();
        var content = File.ReadAllText(md);
        Assert.Contains("Layer contributions", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("terrain", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_Report_ContainsConflictCount()
    {
        var (_, md) = WriteDefault();
        Assert.Contains("Total conflicts:", File.ReadAllText(md), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 6: parsed-cell.json is loadable by ParsedCellLoader
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_ParsedCellJson_IsLoadable()
    {
        var (json, _) = WriteDefault();
        var loaded = ParsedCellLoader.Load(json);
        Assert.True(loaded.IsValid, string.Join("; ", loaded.Errors));
        Assert.Equal(300, loaded.Document!.Width);
        Assert.Equal(300, loaded.Document.Height);
        Assert.Equal(90000, loaded.Document.Counts.Sum(c => c.Pixels));
    }

    // -----------------------------------------------------------------------
    // Test 7: output is deterministic
    // -----------------------------------------------------------------------

    [Fact]
    public void Write_IsDeterministic()
    {
        var dir2 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir2);
        try
        {
            var result = MakeGrassResult();

            LayerMergeArtifactWriter.Write(_tempDir, ValidFixture, PalettePath, Palette, result);
            LayerMergeArtifactWriter.Write(dir2,     ValidFixture, PalettePath, Palette, result);

            var json1 = File.ReadAllText(Path.Combine(_tempDir, "parsed-cell.json"));
            var json2 = File.ReadAllText(Path.Combine(dir2,     "parsed-cell.json"));
            // Rows and counts must match; timestamps may differ so compare selectively
            // (ParsedCellDocument has no generated_at field so full equality holds)
            Assert.Equal(json1, json2);
        }
        finally
        {
            if (Directory.Exists(dir2)) Directory.Delete(dir2, recursive: true);
        }
    }
}
