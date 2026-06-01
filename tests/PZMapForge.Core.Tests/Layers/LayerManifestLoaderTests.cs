using PZMapForge.Core.Layers;
using Xunit;

namespace PZMapForge.Core.Tests.Layers;

public sealed class LayerManifestLoaderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public LayerManifestLoaderTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string ValidFixture =>
        Path.Combine(RepoRoot, "tests", "fixtures", "layers", "valid-layer-manifest.json");

    private string WriteManifest(string json)
    {
        var path = Path.Combine(_tempDir, Path.GetRandomFileName() + ".json");
        File.WriteAllText(path, json, System.Text.Encoding.UTF8);
        return path;
    }

    // Minimal single-layer manifest; valid in all ways.
    private const string MinimalValid = """
        {
          "schema": "pzmapforge.layer-manifest.v0.1",
          "claim_boundary": "planning_artifact_only_not_pz_load_tested",
          "width": 300,
          "height": 300,
          "layers": [
            { "name": "terrain", "path": "terrain.png", "allowed_kinds": ["grass"] }
          ],
          "precedence": ["terrain"]
        }
        """;

    // -----------------------------------------------------------------------
    // Test 1: valid fixture
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_ValidFixture_IsValid()
    {
        var result = LayerManifestLoader.Load(ValidFixture);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.NotNull(result.Document);
        Assert.Equal("pzmapforge.layer-manifest.v0.1", result.Document!.Schema);
        Assert.Equal(4, result.Document.Layers.Count);
        Assert.Equal(4, result.Document.Precedence.Count);
    }

    // -----------------------------------------------------------------------
    // Test 2: missing file
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_MissingFile_IsInvalid()
    {
        var result = LayerManifestLoader.Load(Path.Combine(_tempDir, "does-not-exist.json"));

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    // -----------------------------------------------------------------------
    // Test 3: wrong schema
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_WrongSchema_IsInvalid()
    {
        var path = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.WRONG",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [{ "name": "terrain", "path": "terrain.png", "allowed_kinds": ["grass"] }],
              "precedence": ["terrain"]
            }
            """);

        var result = LayerManifestLoader.Load(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("schema"));
    }

    // -----------------------------------------------------------------------
    // Test 4: wrong claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_WrongClaimBoundary_IsInvalid()
    {
        var path = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "wrong_boundary",
              "width": 300, "height": 300,
              "layers": [{ "name": "terrain", "path": "terrain.png", "allowed_kinds": ["grass"] }],
              "precedence": ["terrain"]
            }
            """);

        var result = LayerManifestLoader.Load(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("claim_boundary"));
    }

    // -----------------------------------------------------------------------
    // Test 5: wrong dimensions
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_WrongDimensions_IsInvalid()
    {
        var path = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 150, "height": 150,
              "layers": [{ "name": "terrain", "path": "terrain.png", "allowed_kinds": ["grass"] }],
              "precedence": ["terrain"]
            }
            """);

        var result = LayerManifestLoader.Load(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("width") || e.Contains("height"));
    }

    // -----------------------------------------------------------------------
    // Test 6: duplicate layer names
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_DuplicateLayerNames_IsInvalid()
    {
        var path = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [
                { "name": "terrain", "path": "a.png", "allowed_kinds": ["grass"] },
                { "name": "terrain", "path": "b.png", "allowed_kinds": ["road"] }
              ],
              "precedence": ["terrain"]
            }
            """);

        var result = LayerManifestLoader.Load(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate layer name"));
    }

    // -----------------------------------------------------------------------
    // Test 7: layer missing from precedence
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_MissingLayerInPrecedence_IsInvalid()
    {
        var path = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [
                { "name": "terrain", "path": "terrain.png", "allowed_kinds": ["grass"] },
                { "name": "roads",   "path": "roads.png",   "allowed_kinds": ["road"] }
              ],
              "precedence": ["terrain"]
            }
            """);

        var result = LayerManifestLoader.Load(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("missing from precedence"));
    }

    // -----------------------------------------------------------------------
    // Test 8: unknown layer name in precedence
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_UnknownLayerInPrecedence_IsInvalid()
    {
        var path = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [
                { "name": "terrain", "path": "terrain.png", "allowed_kinds": ["grass"] }
              ],
              "precedence": ["terrain", "unknown_layer"]
            }
            """);

        var result = LayerManifestLoader.Load(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("does not match any layer name"));
    }

    // -----------------------------------------------------------------------
    // Test 9: duplicate entry in precedence
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_DuplicatePrecedenceEntry_IsInvalid()
    {
        var path = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [
                { "name": "terrain", "path": "terrain.png", "allowed_kinds": ["grass"] }
              ],
              "precedence": ["terrain", "terrain"]
            }
            """);

        var result = LayerManifestLoader.Load(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate entry in precedence"));
    }

    // -----------------------------------------------------------------------
    // Test 10: unknown allowed kind
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_UnknownAllowedKind_IsInvalid()
    {
        var path = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [
                { "name": "terrain", "path": "terrain.png", "allowed_kinds": ["not_a_real_kind"] }
              ],
              "precedence": ["terrain"]
            }
            """);

        var result = LayerManifestLoader.Load(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("unknown kind"));
    }

    // -----------------------------------------------------------------------
    // Test 11: empty allowed_kinds array
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_EmptyAllowedKinds_IsInvalid()
    {
        var path = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [
                { "name": "terrain", "path": "terrain.png", "allowed_kinds": [] }
              ],
              "precedence": ["terrain"]
            }
            """);

        var result = LayerManifestLoader.Load(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("allowed_kinds must be non-empty"));
    }

    // -----------------------------------------------------------------------
    // Test 12: empty layer path
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_EmptyLayerPath_IsInvalid()
    {
        var path = WriteManifest("""
            {
              "schema": "pzmapforge.layer-manifest.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "width": 300, "height": 300,
              "layers": [
                { "name": "terrain", "path": "", "allowed_kinds": ["grass"] }
              ],
              "precedence": ["terrain"]
            }
            """);

        var result = LayerManifestLoader.Load(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("empty or missing path"));
    }
}
