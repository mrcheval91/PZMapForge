namespace PZMapForge.Core.LocalPz;

public sealed class LocalPzInstallValidationSummary
{
    public bool InstallRootExists { get; set; }

    public bool TilesRootExists { get; set; }

    public SortedDictionary<string, int> ExtensionCounts { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public bool PngPresent { get; set; }

    public bool PackPresent { get; set; }

    public bool TilesPresent { get; set; }

    public bool LotPackPresent { get; set; }

    public bool LotHeaderPresent { get; set; }

    public bool BinPresent { get; set; }

    public bool LikelyTileDataPresent { get; set; }

    public bool PzAssetsCopied { get; set; }

    public bool MediaMapsTouched { get; set; }
}
