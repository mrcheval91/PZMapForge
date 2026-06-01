using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using PZMapForge.Core.Layers;
using PZMapForge.Core.Regions;
using Xunit;

namespace PZMapForge.Core.Tests.Layers;

[SupportedOSPlatform("windows")]
public sealed class LayerMergerTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-merger-tests", Path.GetRandomFileName());

    public LayerMergerTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    // -----------------------------------------------------------------------
    // Paths
    // -----------------------------------------------------------------------

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string PalettePath =>
        Path.Combine(RepoRoot, "source", "image-palette.json");

    // -----------------------------------------------------------------------
    // Palette colours from source/image-palette.json (exact RGB values)
    // -----------------------------------------------------------------------

    private static readonly Color GrassColor    = Color.FromArgb(100, 140, 70);   // 'g'
    private static readonly Color RoadColor     = Color.FromArgb(70,  70,  70);   // 'r'
    private static readonly Color RowHouseColor = Color.FromArgb(160, 110, 80);   // 'h'
    private static readonly Color SpawnColor    = Color.FromArgb(0,   220, 80);   // 'p'

    // -----------------------------------------------------------------------
    // Image helpers
    // -----------------------------------------------------------------------

    private string MakeSolid(string name, int w, int h, Color color)
    {
        var path = Path.Combine(_tempDir, name);
        using var bmp = new Bitmap(w, h);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(color);
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    // Fill top `splitY` rows with `top`, remaining rows with `bottom`.
    private string MakeSplit(string name, int w, int h, Color top, Color bottom, int splitY)
    {
        var path = Path.Combine(_tempDir, name);
        using var bmp   = new Bitmap(w, h);
        using var g     = Graphics.FromImage(bmp);
        using var brush = new SolidBrush(Color.White);
        g.Clear(bottom);
        brush.Color = top;
        g.FillRectangle(brush, 0, 0, w, splitY);
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    // Fill background with `bg` then fill `zone` rect with `fill`.
    private string MakeZone(string name, int w, int h, Color bg, Rectangle zone, Color fill)
    {
        var path = Path.Combine(_tempDir, name);
        using var bmp   = new Bitmap(w, h);
        using var g     = Graphics.FromImage(bmp);
        using var brush = new SolidBrush(Color.White);
        g.Clear(bg);
        brush.Color = fill;
        g.FillRectangle(brush, zone);
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    // -----------------------------------------------------------------------
    // Manifest helper
    // -----------------------------------------------------------------------

    private string WriteManifest(string json)
    {
        var path = Path.Combine(_tempDir, "manifest.json");
        File.WriteAllText(path, json, System.Text.Encoding.UTF8);
        return path;
    }

    // -----------------------------------------------------------------------
    // Reusable code→kind lookup (from the standard palette)
    // -----------------------------------------------------------------------

    private static IReadOnlyDictionary<char, string> AllKinds =>
        new Dictionary<char, string>
        {
            ['g'] = "grass",    ['r'] = "road",          ['s'] = "sidewalk",
            ['h'] = "row_house",['d'] = "depanneur",     ['a'] = "garage",
            ['i'] = "industrial_yard", ['l'] = "landmark", ['p'] = "spawn",
        }.AsReadOnly();

    // -----------------------------------------------------------------------
    // Test 1: single terrain layer (all grass) merges to all-grass grid
    // -----------------------------------------------------------------------

    [Fact]
    public void Merge_SingleTerrainLayer_AllGrass()
    {
        MakeSolid("terrain.png", 300, 300, GrassColor);
        var manifest = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [{ "name": "terrain", "path": "terrain.png", "allowed_kinds": ["grass"] }],
              "precedence": ["terrain"]
            }
            """);

        var result = LayerMerger.Merge(manifest, PalettePath);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(300, result.Width);
        Assert.Equal(300, result.Height);
        Assert.Equal(300, result.Rows.Count);
        Assert.All(result.Rows, row => Assert.Equal(new string('g', 300), row));
    }

    // -----------------------------------------------------------------------
    // Test 2: two layers, roads wins over terrain in top half
    // -----------------------------------------------------------------------

    [Fact]
    public void Merge_TwoLayers_PrecedenceApplied()
    {
        MakeSolid("terrain.png", 300, 300, GrassColor);
        MakeSplit("roads.png",   300, 300, RoadColor, GrassColor, 150); // top 150 = road

        var manifest = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [
                { "name": "terrain", "path": "terrain.png", "allowed_kinds": ["grass"] },
                { "name": "roads",   "path": "roads.png",   "allowed_kinds": ["road"] }
              ],
              "precedence": ["roads", "terrain"]
            }
            """);

        var result = LayerMerger.Merge(manifest, PalettePath);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        for (int y = 0; y < 150; y++)
            Assert.Equal(new string('r', 300), result.Rows[y]);
        for (int y = 150; y < 300; y++)
            Assert.Equal(new string('g', 300), result.Rows[y]);
    }

    // -----------------------------------------------------------------------
    // Test 3: four non-overlapping zones, correct kind at each spot
    // -----------------------------------------------------------------------

    [Fact]
    public void Merge_FourLayers_NonOverlapping_CorrectKinds()
    {
        MakeSolid("terrain.png",   300, 300, GrassColor);
        MakeZone("roads.png",      300, 300, GrassColor, new Rectangle(0,   0,   150, 150), RoadColor);
        MakeZone("buildings.png",  300, 300, GrassColor, new Rectangle(150, 0,   150, 150), RowHouseColor);
        MakeZone("markers.png",    300, 300, GrassColor, new Rectangle(0,   150, 150, 150), SpawnColor);

        var manifest = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [
                { "name": "terrain",   "path": "terrain.png",   "allowed_kinds": ["grass"] },
                { "name": "roads",     "path": "roads.png",     "allowed_kinds": ["road"] },
                { "name": "buildings", "path": "buildings.png", "allowed_kinds": ["row_house"] },
                { "name": "markers",   "path": "markers.png",   "allowed_kinds": ["spawn"] }
              ],
              "precedence": ["markers", "buildings", "roads", "terrain"]
            }
            """);

        var result = LayerMerger.Merge(manifest, PalettePath);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(0, result.TotalConflictCount);
        Assert.NotNull(result.Grid);
        Assert.Equal('r', result.Grid!.GetCode(0,   0));    // top-left:  road
        Assert.Equal('h', result.Grid.GetCode(150,  0));    // top-right: row_house
        Assert.Equal('p', result.Grid.GetCode(0,   150));   // btm-left:  spawn
        Assert.Equal('g', result.Grid.GetCode(150, 150));   // btm-right: grass
    }

    // -----------------------------------------------------------------------
    // Test 4: missing layer image → invalid
    // -----------------------------------------------------------------------

    [Fact]
    public void Merge_MissingLayerImage_IsInvalid()
    {
        // terrain.png intentionally NOT created
        var manifest = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [{ "name": "terrain", "path": "terrain.png", "allowed_kinds": ["grass"] }],
              "precedence": ["terrain"]
            }
            """);

        var result = LayerMerger.Merge(manifest, PalettePath);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("terrain") && e.Contains("not found"));
    }

    // -----------------------------------------------------------------------
    // Test 5: layer image contains kind not in allowed_kinds → invalid
    // -----------------------------------------------------------------------

    [Fact]
    public void Merge_LayerKindNotAllowed_IsInvalid()
    {
        // roads.png has road pixels, but allowed_kinds only permits row_house
        MakeSolid("roads.png", 300, 300, RoadColor);
        var manifest = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [{ "name": "roads", "path": "roads.png", "allowed_kinds": ["row_house"] }],
              "precedence": ["roads"]
            }
            """);

        var result = LayerMerger.Merge(manifest, PalettePath);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("kind 'road'") && e.Contains("allowed_kinds"));
    }

    // -----------------------------------------------------------------------
    // Test 6: conflict count correct for minimal overlap
    // -----------------------------------------------------------------------

    [Fact]
    public void Merge_Conflict_CountIsCorrect()
    {
        // Only cell (0,0) has overlapping non-default contributions
        MakeZone("roads.png",     300, 300, GrassColor, new Rectangle(0, 0, 1, 1), RoadColor);
        MakeZone("buildings.png", 300, 300, GrassColor, new Rectangle(0, 0, 1, 1), RowHouseColor);

        var manifest = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [
                { "name": "roads",     "path": "roads.png",     "allowed_kinds": ["road"] },
                { "name": "buildings", "path": "buildings.png", "allowed_kinds": ["row_house"] }
              ],
              "precedence": ["buildings", "roads"]
            }
            """);

        var result = LayerMerger.Merge(manifest, PalettePath);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(1, result.TotalConflictCount);
        Assert.Single(result.ConflictSample);
        Assert.Equal("buildings", result.ConflictSample[0].ChosenLayer);
        Assert.Equal("row_house", result.ConflictSample[0].ChosenKind);
        Assert.Equal('h', result.Grid!.GetCode(0, 0)); // buildings won
    }

    // -----------------------------------------------------------------------
    // Test 7: conflict sample capped at 100 even when total is much larger
    // -----------------------------------------------------------------------

    [Fact]
    public void Merge_ConflictSample_CappedAt100()
    {
        // 150 rows × 300 cols = 45 000 overlapping cells → 45 000 conflicts
        MakeSplit("roads.png",     300, 300, RoadColor,     GrassColor, 150);
        MakeSplit("buildings.png", 300, 300, RowHouseColor, GrassColor, 150);

        var manifest = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [
                { "name": "roads",     "path": "roads.png",     "allowed_kinds": ["road"] },
                { "name": "buildings", "path": "buildings.png", "allowed_kinds": ["row_house"] }
              ],
              "precedence": ["buildings", "roads"]
            }
            """);

        var result = LayerMerger.Merge(manifest, PalettePath);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(45_000, result.TotalConflictCount);
        Assert.Equal(100, result.ConflictSample.Count);
    }

    // -----------------------------------------------------------------------
    // Test 8: Resize=true allows 150x150 layer image → output is 300x300
    // -----------------------------------------------------------------------

    [Fact]
    public void Merge_ResizeTrue_Allows150x150Layer()
    {
        MakeSolid("terrain.png", 150, 150, GrassColor); // intentionally undersized
        var manifest = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [{ "name": "terrain", "path": "terrain.png", "allowed_kinds": ["grass"] }],
              "precedence": ["terrain"]
            }
            """);

        var result = LayerMerger.Merge(manifest, PalettePath, new LayerMergeOptions { Resize = true });

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(300, result.Width);
        Assert.Equal(300, result.Height);
        Assert.Equal(300, result.Rows.Count);
    }

    // -----------------------------------------------------------------------
    // Test 9: Resize=false rejects 150x150 layer image
    // -----------------------------------------------------------------------

    [Fact]
    public void Merge_ResizeFalse_Rejects150x150Layer()
    {
        MakeSolid("terrain.png", 150, 150, GrassColor);
        var manifest = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [{ "name": "terrain", "path": "terrain.png", "allowed_kinds": ["grass"] }],
              "precedence": ["terrain"]
            }
            """);

        var result = LayerMerger.Merge(manifest, PalettePath, new LayerMergeOptions { Resize = false });

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    // -----------------------------------------------------------------------
    // Test 10: merge output is deterministic across two runs
    // -----------------------------------------------------------------------

    [Fact]
    public void Merge_Deterministic_TwoRunsIdentical()
    {
        MakeSolid("terrain.png", 300, 300, GrassColor);
        MakeSplit("roads.png",   300, 300, RoadColor, GrassColor, 100);

        var manifest = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [
                { "name": "terrain", "path": "terrain.png", "allowed_kinds": ["grass"] },
                { "name": "roads",   "path": "roads.png",   "allowed_kinds": ["road"] }
              ],
              "precedence": ["roads", "terrain"]
            }
            """);

        var r1 = LayerMerger.Merge(manifest, PalettePath);
        var r2 = LayerMerger.Merge(manifest, PalettePath);

        Assert.True(r1.IsValid);
        Assert.True(r2.IsValid);
        Assert.Equal(r1.Rows, r2.Rows);
        Assert.Equal(r1.TotalConflictCount, r2.TotalConflictCount);
    }

    // -----------------------------------------------------------------------
    // Test 11: merged SemanticGrid passes to RegionExtractor without error
    // -----------------------------------------------------------------------

    [Fact]
    public void Merge_Grid_PassableToRegionExtractor()
    {
        MakeSolid("terrain.png", 300, 300, GrassColor);
        var manifest = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [{ "name": "terrain", "path": "terrain.png", "allowed_kinds": ["grass"] }],
              "precedence": ["terrain"]
            }
            """);

        var result = LayerMerger.Merge(manifest, PalettePath);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.NotNull(result.Grid);
        var regions = RegionExtractor.Extract(result.Grid!, AllKinds);
        Assert.True(regions.TotalRegions > 0);
    }

    // -----------------------------------------------------------------------
    // Test 12: claim boundary is always set
    // -----------------------------------------------------------------------

    [Fact]
    public void Merge_ClaimBoundary_IsSet()
    {
        MakeSolid("terrain.png", 300, 300, GrassColor);
        var manifest = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [{ "name": "terrain", "path": "terrain.png", "allowed_kinds": ["grass"] }],
              "precedence": ["terrain"]
            }
            """);

        var result = LayerMerger.Merge(manifest, PalettePath);

        Assert.Equal("planning_artifact_only_not_pz_load_tested", result.ClaimBoundary);
    }
}
