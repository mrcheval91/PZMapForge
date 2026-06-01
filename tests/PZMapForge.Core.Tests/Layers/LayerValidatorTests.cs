using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text;
using PZMapForge.Core.Layers;
using Xunit;

namespace PZMapForge.Core.Tests.Layers;

[SupportedOSPlatform("windows")]
public sealed class LayerValidatorTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-validator-tests", Path.GetRandomFileName());

    public LayerValidatorTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string PalettePath =>
        Path.Combine(RepoRoot, "source", "image-palette.json");

    private static readonly Color GrassColor    = Color.FromArgb(100, 140,  70);
    private static readonly Color RoadColor     = Color.FromArgb( 70,  70,  70);
    private static readonly Color RowHouseColor = Color.FromArgb(160, 110,  80);

    private string MakeSolid(string name, int w, int h, Color color)
    {
        var path = Path.Combine(_tempDir, name);
        using var bmp = new Bitmap(w, h);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(color);
        bmp.Save(path, ImageFormat.Png);
        return path;
    }

    private string WriteManifest(string json)
    {
        var path = Path.Combine(_tempDir, "manifest.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    // -----------------------------------------------------------------------
    // Test 1: valid single terrain layer passes
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_ValidLayer_IsValid()
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

        var result = LayerValidator.Validate(manifest, PalettePath);

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Concat(
            result.LayerResults.SelectMany(l => l.Errors))));
        Assert.Single(result.LayerResults);
        Assert.True(result.LayerResults[0].IsValid);
        Assert.Equal(300, result.LayerResults[0].Width);
        Assert.Equal(300, result.LayerResults[0].Height);
    }

    // -----------------------------------------------------------------------
    // Test 2: missing image fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_MissingImage_IsInvalid()
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

        var result = LayerValidator.Validate(manifest, PalettePath);

        Assert.False(result.IsValid);
        Assert.Contains(result.LayerResults, l => l.Errors.Any(e => e.Contains("not found")));
    }

    // -----------------------------------------------------------------------
    // Test 3: disallowed kind fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_DisallowedKind_IsInvalid()
    {
        MakeSolid("roads.png", 300, 300, RoadColor); // all "road" pixels
        var manifest = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [{ "name": "roads", "path": "roads.png", "allowed_kinds": ["row_house"] }],
              "precedence": ["roads"]
            }
            """);

        var result = LayerValidator.Validate(manifest, PalettePath);

        Assert.False(result.IsValid);
        var lr = result.LayerResults[0];
        Assert.False(lr.IsValid);
        Assert.Equal(90000, lr.NonDefaultPixels);
        Assert.Equal(90000, lr.InvalidPixels);
        Assert.Contains(lr.Errors, e => e.Contains("kind 'road'", StringComparison.OrdinalIgnoreCase));
    }

    // -----------------------------------------------------------------------
    // Test 4: non-300x300 without --resize fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_NonSquare_WithoutResize_IsInvalid()
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

        var result = LayerValidator.Validate(manifest, PalettePath,
            new LayerMergeOptions { Resize = false });

        Assert.False(result.IsValid);
        Assert.Contains(result.LayerResults, l => !l.IsValid);
    }

    // -----------------------------------------------------------------------
    // Test 5: non-300x300 with --resize passes
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_NonSquare_WithResize_IsValid()
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

        var result = LayerValidator.Validate(manifest, PalettePath,
            new LayerMergeOptions { Resize = true });

        Assert.True(result.IsValid, string.Join("; ", result.LayerResults
            .SelectMany(l => l.Errors)));
        Assert.Equal(300, result.LayerResults[0].Width);
        Assert.Equal(300, result.LayerResults[0].Height);
    }

    // -----------------------------------------------------------------------
    // Test 6: invalid manifest (wrong schema) fails at manifest level
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_InvalidManifest_IsInvalid()
    {
        var manifest = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.WRONG",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [{ "name": "terrain", "path": "terrain.png", "allowed_kinds": ["grass"] }],
              "precedence": ["terrain"]
            }
            """);

        var result = LayerValidator.Validate(manifest, PalettePath);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors); // manifest-level error
        Assert.Empty(result.LayerResults); // never got to layers
    }

    // -----------------------------------------------------------------------
    // Test 7: deterministic — two runs produce equal results
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_Deterministic_TwoRunsEqual()
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

        var r1 = LayerValidator.Validate(manifest, PalettePath);
        var r2 = LayerValidator.Validate(manifest, PalettePath);

        Assert.Equal(r1.IsValid, r2.IsValid);
        Assert.Equal(r1.LayerResults[0].NonDefaultPixels,
                     r2.LayerResults[0].NonDefaultPixels);
        Assert.Equal(r1.LayerResults[0].InvalidPixels,
                     r2.LayerResults[0].InvalidPixels);
    }

    // -----------------------------------------------------------------------
    // Test 8: claim boundary is present on result
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_ClaimBoundary_IsSet()
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

        var result = LayerValidator.Validate(manifest, PalettePath);

        Assert.Equal("planning_artifact_only_not_pz_load_tested", result.ClaimBoundary);
    }
}
