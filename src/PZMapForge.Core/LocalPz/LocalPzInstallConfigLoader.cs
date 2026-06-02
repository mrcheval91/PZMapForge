using System.Text.Json;

namespace PZMapForge.Core.LocalPz;

/// <summary>
/// Loads and validates a local-only PZ install config document.
/// Validates the config document structure and safety flags only.
/// Does NOT require the real pz_install_root to exist on disk.
/// Does not access, copy, or inspect any PZ game assets.
/// Does not write to media/maps.
/// Claim boundary: planning_artifact_only_not_pz_load_tested
/// </summary>
public static class LocalPzInstallConfigLoader
{
    private const string RequiredSchema           = "pzmapforge.local-pz-install-config.v0.1";
    private const string RequiredClaimBoundary    = "planning_artifact_only_not_pz_load_tested";
    private const string RequiredTileReferenceMode = "local_reference_only";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas      = true,
        ReadCommentHandling      = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = false,
    };

    public static LocalPzInstallConfigLoadResult Load(string path)
    {
        var result = new LocalPzInstallConfigLoadResult();

        if (!File.Exists(path))
        {
            result.Errors.Add($"Local PZ install config not found: {path}");
            return result;
        }

        LocalPzInstallConfig doc;
        try
        {
            var json = File.ReadAllText(path);
            doc = JsonSerializer.Deserialize<LocalPzInstallConfig>(json, JsonOptions)
                  ?? throw new JsonException("Deserialized to null.");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"JSON parse error: {ex.Message}");
            return result;
        }

        result.Document = doc;
        Validate(doc, result.Errors);
        return result;
    }

    private static void Validate(LocalPzInstallConfig doc, List<string> errors)
    {
        if (doc.Schema != RequiredSchema)
            errors.Add($"schema must be '{RequiredSchema}', got '{doc.Schema}'.");

        if (doc.ClaimBoundary != RequiredClaimBoundary)
            errors.Add($"claim_boundary must be '{RequiredClaimBoundary}'.");

        if (string.IsNullOrWhiteSpace(doc.PzInstallRoot))
            errors.Add("pz_install_root must be non-empty.");

        if (string.IsNullOrWhiteSpace(doc.TilesRoot))
            errors.Add("tiles_root must be non-empty.");

        if (doc.AllowAssetCopy)
            errors.Add("allow_asset_copy must be false. Copying PZ assets is forbidden.");

        if (doc.AllowMediaMapsWrite)
            errors.Add("allow_media_maps_write must be false. Writing to game directories is forbidden.");

        if (doc.TileReferenceMode != RequiredTileReferenceMode)
            errors.Add($"tile_reference_mode must be '{RequiredTileReferenceMode}', got '{doc.TileReferenceMode}'.");
    }
}
