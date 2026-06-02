namespace PZMapForge.Core.LocalPz;

/// <summary>
/// Validates a local PZ install config against the local filesystem in a
/// read-only way.
/// Claim boundary: planning_artifact_only_not_pz_load_tested.
/// </summary>
public static class LocalPzInstallValidator
{
    private static readonly string[] TileDataExtensions =
    [
        ".png",
        ".pack",
        ".tiles",
        ".lotpack",
        ".lotheader",
        ".bin"
    ];

    public static LocalPzInstallValidationResult Validate(string configPath)
    {
        var result = new LocalPzInstallValidationResult();

        var loaded = LocalPzInstallConfigLoader.Load(configPath);
        if (!loaded.IsValid)
        {
            foreach (var error in loaded.Errors)
                result.Errors.Add(error);

            return result;
        }

        if (loaded.Document is null)
        {
            result.Errors.Add("Config loader returned no document.");
            return result;
        }

        var config = loaded.Document;
        result.Config = config;

        if (config.AllowAssetCopy)
            result.Errors.Add("allow_asset_copy must be false.");

        if (config.AllowMediaMapsWrite)
            result.Errors.Add("allow_media_maps_write must be false.");

        if (!string.Equals(config.TileReferenceMode, "local_reference_only", StringComparison.Ordinal))
            result.Errors.Add("tile_reference_mode must be local_reference_only.");

        var installRoot = config.PzInstallRoot;
        result.Summary.InstallRootExists = Directory.Exists(installRoot);

        if (!result.Summary.InstallRootExists)
        {
            result.Errors.Add("pz_install_root does not exist.");
            return result;
        }

        var tilesRoot = Path.IsPathRooted(config.TilesRoot)
            ? config.TilesRoot
            : Path.Combine(installRoot, config.TilesRoot);

        result.Summary.TilesRootExists = Directory.Exists(tilesRoot);

        if (!result.Summary.TilesRootExists)
        {
            result.Errors.Add("tiles_root does not exist.");
            return result;
        }

        CountExtensions(tilesRoot, result.Summary);

        result.Summary.PngPresent = HasExtension(result.Summary, ".png");
        result.Summary.PackPresent = HasExtension(result.Summary, ".pack");
        result.Summary.TilesPresent = HasExtension(result.Summary, ".tiles");
        result.Summary.LotPackPresent = HasExtension(result.Summary, ".lotpack");
        result.Summary.LotHeaderPresent = HasExtension(result.Summary, ".lotheader");
        result.Summary.BinPresent = HasExtension(result.Summary, ".bin");

        result.Summary.LikelyTileDataPresent = TileDataExtensions.Any(
            extension => HasExtension(result.Summary, extension));

        // These are explicit evidence fields. The validator is read-only.
        result.Summary.PzAssetsCopied = false;
        result.Summary.MediaMapsTouched = false;

        return result;
    }

    private static void CountExtensions(string root, LocalPzInstallValidationSummary summary)
    {
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var extension = Path.GetExtension(file);
            if (string.IsNullOrWhiteSpace(extension))
                extension = "[none]";

            extension = extension.ToLowerInvariant();

            if (!summary.ExtensionCounts.TryAdd(extension, 1))
                summary.ExtensionCounts[extension]++;
        }
    }

    private static bool HasExtension(LocalPzInstallValidationSummary summary, string extension)
    {
        return summary.ExtensionCounts.TryGetValue(extension, out var count) && count > 0;
    }
}
