using PZMapForge.Core.LocalPz;
using Xunit;

namespace PZMapForge.Core.Tests.LocalPz;

public sealed class LocalPzInstallConfigLoaderTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public LocalPzInstallConfigLoaderTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string ValidFixture =>
        Path.Combine(RepoRoot, "tests", "fixtures", "local-pz", "valid-local-pz-install-config.json");

    private string WriteConfig(string json)
    {
        var path = Path.Combine(_tempDir, "config.json");
        File.WriteAllText(path, json, System.Text.Encoding.UTF8);
        return path;
    }

    // -----------------------------------------------------------------------
    // Test 1: valid fixture loads successfully
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_ValidFixture_IsValid()
    {
        var result = LocalPzInstallConfigLoader.Load(ValidFixture);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.NotNull(result.Document);
        Assert.Equal("pzmapforge.local-pz-install-config.v0.1", result.Document!.Schema);
        Assert.False(result.Document.AllowAssetCopy);
        Assert.False(result.Document.AllowMediaMapsWrite);
        Assert.Equal("local_reference_only", result.Document.TileReferenceMode);
    }

    // -----------------------------------------------------------------------
    // Test 2: missing file fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_MissingFile_IsInvalid()
    {
        var result = LocalPzInstallConfigLoader.Load(Path.Combine(_tempDir, "does-not-exist.json"));

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    // -----------------------------------------------------------------------
    // Test 3: wrong schema fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_WrongSchema_IsInvalid()
    {
        var path = WriteConfig("""
            {
              "schema": "pzmapforge.local-pz-install-config.WRONG",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "pz_install_root": "[ROOT]",
              "tiles_root": "media",
              "allow_asset_copy": false,
              "allow_media_maps_write": false,
              "tile_reference_mode": "local_reference_only"
            }
            """);

        var result = LocalPzInstallConfigLoader.Load(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("schema"));
    }

    // -----------------------------------------------------------------------
    // Test 4: wrong claim_boundary fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_WrongClaimBoundary_IsInvalid()
    {
        var path = WriteConfig("""
            {
              "schema": "pzmapforge.local-pz-install-config.v0.1",
              "claim_boundary": "wrong_boundary",
              "pz_install_root": "[ROOT]",
              "tiles_root": "media",
              "allow_asset_copy": false,
              "allow_media_maps_write": false,
              "tile_reference_mode": "local_reference_only"
            }
            """);

        var result = LocalPzInstallConfigLoader.Load(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("claim_boundary"));
    }

    // -----------------------------------------------------------------------
    // Test 5: empty pz_install_root fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_EmptyPzInstallRoot_IsInvalid()
    {
        var path = WriteConfig("""
            {
              "schema": "pzmapforge.local-pz-install-config.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "pz_install_root": "",
              "tiles_root": "media",
              "allow_asset_copy": false,
              "allow_media_maps_write": false,
              "tile_reference_mode": "local_reference_only"
            }
            """);

        var result = LocalPzInstallConfigLoader.Load(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("pz_install_root"));
    }

    // -----------------------------------------------------------------------
    // Test 6: empty tiles_root fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_EmptyTilesRoot_IsInvalid()
    {
        var path = WriteConfig("""
            {
              "schema": "pzmapforge.local-pz-install-config.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "pz_install_root": "[ROOT]",
              "tiles_root": "",
              "allow_asset_copy": false,
              "allow_media_maps_write": false,
              "tile_reference_mode": "local_reference_only"
            }
            """);

        var result = LocalPzInstallConfigLoader.Load(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("tiles_root"));
    }

    // -----------------------------------------------------------------------
    // Test 7: allow_asset_copy true fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_AllowAssetCopyTrue_IsInvalid()
    {
        var path = WriteConfig("""
            {
              "schema": "pzmapforge.local-pz-install-config.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "pz_install_root": "[ROOT]",
              "tiles_root": "media",
              "allow_asset_copy": true,
              "allow_media_maps_write": false,
              "tile_reference_mode": "local_reference_only"
            }
            """);

        var result = LocalPzInstallConfigLoader.Load(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("allow_asset_copy"));
    }

    // -----------------------------------------------------------------------
    // Test 8: allow_media_maps_write true fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_AllowMediaMapsWriteTrue_IsInvalid()
    {
        var path = WriteConfig("""
            {
              "schema": "pzmapforge.local-pz-install-config.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "pz_install_root": "[ROOT]",
              "tiles_root": "media",
              "allow_asset_copy": false,
              "allow_media_maps_write": true,
              "tile_reference_mode": "local_reference_only"
            }
            """);

        var result = LocalPzInstallConfigLoader.Load(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("allow_media_maps_write"));
    }

    // -----------------------------------------------------------------------
    // Test 9: wrong tile_reference_mode fails
    // -----------------------------------------------------------------------

    [Fact]
    public void Load_WrongTileReferenceMode_IsInvalid()
    {
        var path = WriteConfig("""
            {
              "schema": "pzmapforge.local-pz-install-config.v0.1",
              "claim_boundary": "planning_artifact_only_not_pz_load_tested",
              "pz_install_root": "[ROOT]",
              "tiles_root": "media",
              "allow_asset_copy": false,
              "allow_media_maps_write": false,
              "tile_reference_mode": "copy_assets"
            }
            """);

        var result = LocalPzInstallConfigLoader.Load(path);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("tile_reference_mode"));
    }
}
