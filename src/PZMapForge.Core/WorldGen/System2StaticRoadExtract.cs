using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class System2StaticRoadExtract
{
    [JsonPropertyName("format")]
    public string Format { get; init; } = "pzmapforge.deadmtl.system2.static-road-extract.v1";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "EXTRACT_ONLY";

    [JsonPropertyName("runtime_status")]
    public string RuntimeStatus { get; init; } = "NOT_RUNTIME_PROVEN";

    [JsonPropertyName("writer_status")]
    public string WriterStatus { get; init; } = "NOT_IMPLEMENTED";

    [JsonPropertyName("source_contract")]
    public string SourceContract { get; init; } = string.Empty;

    [JsonPropertyName("origin_x")]
    public int OriginX { get; init; }

    [JsonPropertyName("origin_y")]
    public int OriginY { get; init; }

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }

    [JsonPropertyName("scale")]
    public System2ExtractScale Scale { get; init; } = new();

    [JsonPropertyName("layers")]
    public List<System2StaticRoadLayerSummary> Layers { get; init; } = new();

    [JsonPropertyName("totals")]
    public System2ExtractTotals Totals { get; init; } = new();

    [JsonPropertyName("claim_boundary")]
    public System2ExtractClaimBoundary ClaimBoundary { get; init; } = new();
}

public sealed class System2ExtractScale
{
    [JsonPropertyName("pixels_per_meter")]
    public int PixelsPerMeter { get; init; } = 1;

    [JsonPropertyName("meters_per_pixel")]
    public int MetersPerPixel { get; init; } = 1;

    [JsonPropertyName("pz_tiles_per_pixel")]
    public int PzTilesPerPixel { get; init; } = 1;
}

public sealed class System2ExtractTotals
{
    [JsonPropertyName("layer_count")]
    public int LayerCount { get; init; }

    [JsonPropertyName("non_empty_pixels")]
    public int NonEmptyPixels { get; init; }

    [JsonPropertyName("unknown_opaque_pixels")]
    public int UnknownOpaquePixels { get; init; }
}

public sealed class System2ExtractClaimBoundary
{
    [JsonPropertyName("writes_lotpack")]
    public bool WritesLotpack { get; init; } = false;

    [JsonPropertyName("writes_worldgen_lua")]
    public bool WritesWorldgenLua { get; init; } = false;

    [JsonPropertyName("runtime_proven")]
    public bool RuntimeProven { get; init; } = false;

    [JsonPropertyName("public_playable_claim")]
    public bool PublicPlayableClaim { get; init; } = false;
}

public sealed class System2StaticRoadExtractResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; } = new();
    public System2StaticRoadExtract? Extract { get; set; }
}
