using System.Text.Json.Serialization;

namespace PZMapForge.Core.LocalPz;

public sealed class LocalTileReferenceSurvey
{
    public const string SchemaId = "pzmapforge.local-tile-reference-survey.v0.1";

    public const string ExpectedClaimBoundary =
        LocalPzInstallValidationResult.ExpectedClaimBoundary;

    [JsonPropertyName("schema")]
    public string Schema { get; set; } = SchemaId;

    [JsonPropertyName("claim_boundary")]
    public string ClaimBoundary { get; set; } = ExpectedClaimBoundary;

    [JsonPropertyName("generated_at_utc")]
    public string GeneratedAtUtc { get; set; } = string.Empty;

    [JsonPropertyName("source_config_path")]
    public string SourceConfigPath { get; set; } = string.Empty;

    [JsonPropertyName("install_root_exists")]
    public bool InstallRootExists { get; set; }

    [JsonPropertyName("tiles_root_exists")]
    public bool TilesRootExists { get; set; }

    [JsonPropertyName("extension_counts")]
    public SortedDictionary<string, int> ExtensionCounts { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("likely_tile_data_present")]
    public bool LikelyTileDataPresent { get; set; }

    [JsonPropertyName("png_present")]
    public bool PngPresent { get; set; }

    [JsonPropertyName("pack_present")]
    public bool PackPresent { get; set; }

    [JsonPropertyName("tiles_present")]
    public bool TilesPresent { get; set; }

    [JsonPropertyName("lotpack_present")]
    public bool LotPackPresent { get; set; }

    [JsonPropertyName("lotheader_present")]
    public bool LotHeaderPresent { get; set; }

    [JsonPropertyName("bin_present")]
    public bool BinPresent { get; set; }

    [JsonPropertyName("pz_assets_copied")]
    public bool PzAssetsCopied { get; set; }

    [JsonPropertyName("media_maps_touched")]
    public bool MediaMapsTouched { get; set; }

    [JsonPropertyName("playable_export_claimed")]
    public bool PlayableExportClaimed { get; set; }
}