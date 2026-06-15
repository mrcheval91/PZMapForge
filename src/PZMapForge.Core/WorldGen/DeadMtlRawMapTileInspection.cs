using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlRawMapTileInspection
{
    [JsonPropertyName("format")]
    public string Format { get; init; } = "pzmapforge.deadmtl.raw-map-tile-inspection.v1";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "RAW_TILE_INSPECTION_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; init; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; init; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("source_image")]
    public string SourceImage { get; init; } = string.Empty;

    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }

    [JsonPropertyName("expected_width")]
    public int ExpectedWidth { get; init; }

    [JsonPropertyName("expected_height")]
    public int ExpectedHeight { get; init; }

    [JsonPropertyName("size_valid")]
    public bool SizeValid { get; init; }

    [JsonPropertyName("has_alpha")]
    public bool HasAlpha { get; init; }

    [JsonPropertyName("opaque_pixel_count")]
    public int OpaquePixelCount { get; init; }

    [JsonPropertyName("transparent_pixel_count")]
    public int TransparentPixelCount { get; init; }

    [JsonPropertyName("unique_color_count")]
    public int UniqueColorCount { get; init; }

    [JsonPropertyName("top_colors")]
    public List<DeadMtlRawMapTileTopColor> TopColors { get; init; } = new();

    [JsonPropertyName("palette_matches")]
    public DeadMtlRawMapTilePaletteMatches PaletteMatches { get; init; } = new();

    [JsonPropertyName("unknown_colors")]
    public List<string> UnknownColors { get; init; } = new();

    [JsonPropertyName("claim_boundary")]
    public DeadMtlRawMapTileInspectionClaimBoundary ClaimBoundary { get; init; } = new();
}

public sealed class DeadMtlRawMapTileTopColor
{
    [JsonPropertyName("color")]
    public string Color { get; init; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("percentage")]
    public double Percentage { get; init; }
}

public sealed class DeadMtlRawMapTilePaletteMatches
{
    [JsonPropertyName("worldgen_palette_match_count")]
    public int WorldgenPaletteMatchCount { get; init; }

    [JsonPropertyName("system2_static_road_palette_match_count")]
    public int System2StaticRoadPaletteMatchCount { get; init; }

    [JsonPropertyName("unknown_color_count")]
    public int UnknownColorCount { get; init; }
}

public sealed class DeadMtlRawMapTileInspectionClaimBoundary
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

public sealed class DeadMtlRawMapTileInspectionResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; } = new();
    public DeadMtlRawMapTileInspection? Inspection { get; set; }
    // Not serialized — loaded palette colors for CSV rendering
    public IReadOnlyList<string> WorldgenColors { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> System2Colors  { get; set; } = Array.Empty<string>();
}
