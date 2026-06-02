using PZMapForge.Core.LocalPz;
using Xunit;

namespace PZMapForge.Core.Tests.LocalPz;

public sealed class LocalPzInstallValidatorTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-local-pz-validator-tests", Path.GetRandomFileName());

    public LocalPzInstallValidatorTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Best effort cleanup only.
        }
    }

    [Fact]
    public void Validate_ValidFakeInstallWithMediaRoot_Passes()
    {
        var installRoot = CreateFakeInstall();
        File.WriteAllText(Path.Combine(installRoot, "media", "tile.pack"), "");
        var config = WriteConfig(installRoot, "media");

        var result = LocalPzInstallValidator.Validate(config);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.True(result.Summary.InstallRootExists);
        Assert.True(result.Summary.TilesRootExists);
        Assert.True(result.Summary.PackPresent);
        Assert.True(result.Summary.LikelyTileDataPresent);
    }

    [Fact]
    public void Validate_MissingConfig_Fails()
    {
        var result = LocalPzInstallValidator.Validate(Path.Combine(_tempDir, "missing.json"));

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Validate_InvalidConfig_FailsThroughLoader()
    {
        var installRoot = CreateFakeInstall();
        var config = WriteConfig(installRoot, "media", allowAssetCopy: true);

        var result = LocalPzInstallValidator.Validate(config);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("allow_asset_copy", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_MissingInstallRoot_Fails()
    {
        var missingRoot = Path.Combine(_tempDir, "does-not-exist");
        var config = WriteConfig(missingRoot, "media");

        var result = LocalPzInstallValidator.Validate(config);

        Assert.False(result.IsValid);
        Assert.False(result.Summary.InstallRootExists);
        Assert.Contains(result.Errors, e => e.Contains("pz_install_root", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_MissingTilesRoot_Fails()
    {
        var installRoot = Directory.CreateDirectory(Path.Combine(_tempDir, "install")).FullName;
        var config = WriteConfig(installRoot, "media");

        var result = LocalPzInstallValidator.Validate(config);

        Assert.False(result.IsValid);
        Assert.True(result.Summary.InstallRootExists);
        Assert.False(result.Summary.TilesRootExists);
        Assert.Contains(result.Errors, e => e.Contains("tiles_root", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ExtensionCountsAreDeterministic()
    {
        var installRoot = CreateFakeInstall();
        File.WriteAllText(Path.Combine(installRoot, "media", "a.png"), "");
        File.WriteAllText(Path.Combine(installRoot, "media", "b.png"), "");
        File.WriteAllText(Path.Combine(installRoot, "media", "c.tiles"), "");
        var config = WriteConfig(installRoot, "media");

        var first = LocalPzInstallValidator.Validate(config);
        var second = LocalPzInstallValidator.Validate(config);

        Assert.True(first.IsValid);
        Assert.True(second.IsValid);
        Assert.Equal(first.Summary.ExtensionCounts, second.Summary.ExtensionCounts);
        Assert.Equal(2, first.Summary.ExtensionCounts[".png"]);
        Assert.Equal(1, first.Summary.ExtensionCounts[".tiles"]);
    }

    [Fact]
    public void Validate_LikelyTileDataPresent_WhenKnownExtensionsExist()
    {
        var installRoot = CreateFakeInstall();

        foreach (var extension in new[] { ".png", ".pack", ".tiles", ".lotpack", ".lotheader", ".bin" })
            File.WriteAllText(Path.Combine(installRoot, "media", "sample" + extension), "");

        var config = WriteConfig(installRoot, "media");

        var result = LocalPzInstallValidator.Validate(config);

        Assert.True(result.IsValid);
        Assert.True(result.Summary.PngPresent);
        Assert.True(result.Summary.PackPresent);
        Assert.True(result.Summary.TilesPresent);
        Assert.True(result.Summary.LotPackPresent);
        Assert.True(result.Summary.LotHeaderPresent);
        Assert.True(result.Summary.BinPresent);
        Assert.True(result.Summary.LikelyTileDataPresent);
    }

    [Fact]
    public void Validate_LikelyTileDataPresentFalse_WhenNoRelevantExtensionsExist()
    {
        var installRoot = CreateFakeInstall();
        File.WriteAllText(Path.Combine(installRoot, "media", "readme.txt"), "");
        var config = WriteConfig(installRoot, "media");

        var result = LocalPzInstallValidator.Validate(config);

        Assert.True(result.IsValid);
        Assert.False(result.Summary.LikelyTileDataPresent);
    }

    [Fact]
    public void Validate_DoesNotCreateFilesInFakeInstall()
    {
        var installRoot = CreateFakeInstall();
        File.WriteAllText(Path.Combine(installRoot, "media", "sample.pack"), "");
        var before = Directory.EnumerateFiles(installRoot, "*", SearchOption.AllDirectories)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        var config = WriteConfig(installRoot, "media");

        var result = LocalPzInstallValidator.Validate(config);

        var after = Directory.EnumerateFiles(installRoot, "*", SearchOption.AllDirectories)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.True(result.IsValid);
        Assert.Equal(before, after);
    }

    [Fact]
    public void Validate_ClaimBoundaryIsPlanningOnly()
    {
        var installRoot = CreateFakeInstall();
        var config = WriteConfig(installRoot, "media");

        var result = LocalPzInstallValidator.Validate(config);

        Assert.Equal("planning_artifact_only_not_pz_load_tested", result.ClaimBoundary);
    }

    [Fact]
    public void Validate_SafetyFlagsRemainFalse()
    {
        var installRoot = CreateFakeInstall();
        File.WriteAllText(Path.Combine(installRoot, "media", "sample.pack"), "");
        var config = WriteConfig(installRoot, "media");

        var result = LocalPzInstallValidator.Validate(config);

        Assert.True(result.IsValid);
        Assert.False(result.Summary.PzAssetsCopied);
        Assert.False(result.Summary.MediaMapsTouched);
    }

    private string CreateFakeInstall()
    {
        var installRoot = Directory.CreateDirectory(Path.Combine(_tempDir, "install-" + Guid.NewGuid())).FullName;
        Directory.CreateDirectory(Path.Combine(installRoot, "media"));
        return installRoot;
    }

    private string WriteConfig(
        string installRoot,
        string tilesRoot,
        bool allowAssetCopy = false,
        bool allowMediaMapsWrite = false,
        string tileReferenceMode = "local_reference_only")
    {
        var configPath = Path.Combine(_tempDir, "config-" + Guid.NewGuid() + ".json");

        var json = $$"""
        {
          "schema": "pzmapforge.local-pz-install-config.v0.1",
          "claim_boundary": "planning_artifact_only_not_pz_load_tested",
          "pz_install_root": "{{Escape(installRoot)}}",
          "tiles_root": "{{Escape(tilesRoot)}}",
          "allow_asset_copy": {{allowAssetCopy.ToString().ToLowerInvariant()}},
          "allow_media_maps_write": {{allowMediaMapsWrite.ToString().ToLowerInvariant()}},
          "tile_reference_mode": "{{Escape(tileReferenceMode)}}",
          "notes": "Test config. Do not commit real local paths."
        }
        """;

        File.WriteAllText(configPath, json);
        return configPath;
    }

    private static string Escape(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
