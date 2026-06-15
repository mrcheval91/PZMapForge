using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlRawMapTilePaletteMappingEntry
{
    [JsonPropertyName("source_color")]
    public string SourceColor { get; init; } = string.Empty;

    [JsonPropertyName("pixel_count")]
    public int PixelCount { get; init; }

    [JsonPropertyName("percentage")]
    public double Percentage { get; init; }

    [JsonPropertyName("mapping_status")]
    public string MappingStatus { get; init; } = string.Empty;

    [JsonPropertyName("target_system")]
    public string TargetSystem { get; init; } = string.Empty;

    [JsonPropertyName("target_type")]
    public string TargetType { get; init; } = string.Empty;

    [JsonPropertyName("target_key")]
    public string TargetKey { get; init; } = string.Empty;

    [JsonPropertyName("confidence")]
    public string Confidence { get; init; } = string.Empty;

    [JsonPropertyName("nearest_palette_suggestions")]
    public List<DeadMtlRawMapTilePaletteMappingSuggestion> NearestPaletteSuggestions { get; init; } = new();

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = string.Empty;
}

public sealed class DeadMtlRawMapTilePaletteMappingSuggestion
{
    [JsonPropertyName("palette")]
    public string Palette { get; init; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("rgb_distance")]
    public int RgbDistance { get; init; }
}

public sealed class DeadMtlRawMapTilePaletteMappingTotals
{
    [JsonPropertyName("mapping_count")]
    public int MappingCount { get; init; }

    [JsonPropertyName("auto_matched_count")]
    public int AutoMatchedCount { get; init; }

    [JsonPropertyName("unmapped_count")]
    public int UnmappedCount { get; init; }

    [JsonPropertyName("near_match_suggestion_count")]
    public int NearMatchSuggestionCount { get; init; }

    [JsonPropertyName("pixel_count_total")]
    public int PixelCountTotal { get; init; }
}

public sealed class DeadMtlRawMapTilePaletteMappingClaimBoundary
{
    [JsonPropertyName("writes_lotpack")]
    public bool WritesLotpack { get; init; } = false;

    [JsonPropertyName("writes_worldgen_lua")]
    public bool WritesWorldgenLua { get; init; } = false;

    [JsonPropertyName("runtime_proven")]
    public bool RuntimeProven { get; init; } = false;

    [JsonPropertyName("public_playable_claim")]
    public bool PublicPlayableClaim { get; init; } = false;

    [JsonPropertyName("writer_ready_claim")]
    public bool WriterReadyClaim { get; init; } = false;
}

public sealed class DeadMtlRawMapTilePaletteMapping
{
    [JsonPropertyName("format")]
    public string Format { get; init; } = "pzmapforge.deadmtl.raw-map-tile-palette-mapping.v1";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "RAW_TILE_PALETTE_MAPPING_CONTRACT_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; init; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; init; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("source_inspection_json")]
    public string SourceInspectionJson { get; init; } = string.Empty;

    [JsonPropertyName("source_image")]
    public string SourceImage { get; init; } = string.Empty;

    [JsonPropertyName("source_sha256")]
    public string SourceSha256 { get; init; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }

    [JsonPropertyName("mappings")]
    public List<DeadMtlRawMapTilePaletteMappingEntry> Mappings { get; init; } = new();

    [JsonPropertyName("totals")]
    public DeadMtlRawMapTilePaletteMappingTotals Totals { get; init; } = new();

    [JsonPropertyName("claim_boundary")]
    public DeadMtlRawMapTilePaletteMappingClaimBoundary ClaimBoundary { get; init; } = new();
}

public sealed class DeadMtlRawMapTilePaletteMappingResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; } = new();
    public DeadMtlRawMapTilePaletteMapping? Mapping { get; set; }
}
