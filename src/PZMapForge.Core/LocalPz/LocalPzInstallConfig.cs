using System.Text.Json.Serialization;

namespace PZMapForge.Core.LocalPz;

public sealed class LocalPzInstallConfig
{
    [JsonPropertyName("schema")]
    public string Schema { get; set; } = string.Empty;

    [JsonPropertyName("claim_boundary")]
    public string ClaimBoundary { get; set; } = string.Empty;

    [JsonPropertyName("pz_install_root")]
    public string PzInstallRoot { get; set; } = string.Empty;

    [JsonPropertyName("tiles_root")]
    public string TilesRoot { get; set; } = string.Empty;

    [JsonPropertyName("allow_asset_copy")]
    public bool AllowAssetCopy { get; set; }

    [JsonPropertyName("allow_media_maps_write")]
    public bool AllowMediaMapsWrite { get; set; }

    [JsonPropertyName("tile_reference_mode")]
    public string TileReferenceMode { get; set; } = string.Empty;

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}
