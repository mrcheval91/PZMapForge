using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayResult
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    [JsonPropertyName("generated_utc")]
    public string GeneratedUtc { get; set; } = string.Empty;

    [JsonPropertyName("map_id")]
    public string MapId { get; set; } = string.Empty;

    [JsonPropertyName("source_png_path")]
    public string SourcePngPath { get; set; } = string.Empty;

    [JsonPropertyName("geometry_mvp_path")]
    public string GeometryMvpPath { get; set; } = string.Empty;

    [JsonPropertyName("output_png_path")]
    public string OutputPngPath { get; set; } = string.Empty;

    [JsonPropertyName("source_width_px")]
    public int SourceWidthPx { get; set; }

    [JsonPropertyName("source_height_px")]
    public int SourceHeightPx { get; set; }

    [JsonPropertyName("scale")]
    public int Scale { get; set; }

    [JsonPropertyName("target_component_id")]
    public string TargetComponentId { get; set; } = string.Empty;

    [JsonPropertyName("target_component_order")]
    public int TargetComponentOrder { get; set; }

    [JsonPropertyName("intent")]
    public string Intent { get; set; } = string.Empty;

    [JsonPropertyName("access_readiness_class")]
    public string AccessReadinessClass { get; set; } = string.Empty;

    [JsonPropertyName("component_bbox")]
    public DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayBbox? ComponentBbox { get; set; }

    [JsonPropertyName("lot_count")]
    public int LotCount { get; set; }

    [JsonPropertyName("accepted_building_slot_count")]
    public int AcceptedBuildingSlotCount { get; set; }

    [JsonPropertyName("overlay_feature_count")]
    public int OverlayFeatureCount { get; set; }

    [JsonPropertyName("writer_ready")]
    public bool WriterReady { get; set; }

    [JsonPropertyName("runtime_valid")]
    public bool RuntimeValid { get; set; }

    [JsonPropertyName("materialized")]
    public bool Materialized { get; set; }

    [JsonPropertyName("features")]
    public List<DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayFeature> Features { get; set; } = new();

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();

    [JsonPropertyName("is_valid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("verdict")]
    public string Verdict { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayBbox
{
    [JsonPropertyName("min_x")]
    public int MinX { get; set; }

    [JsonPropertyName("min_y")]
    public int MinY { get; set; }

    [JsonPropertyName("max_x")]
    public int MaxX { get; set; }

    [JsonPropertyName("max_y")]
    public int MaxY { get; set; }

    [JsonPropertyName("width_px")]
    public int WidthPx { get; set; }

    [JsonPropertyName("height_px")]
    public int HeightPx { get; set; }
}

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryQaOverlayFeature
{
    [JsonPropertyName("feature_order")]
    public int FeatureOrder { get; set; }

    [JsonPropertyName("feature_kind")]
    public string FeatureKind { get; set; } = string.Empty;

    [JsonPropertyName("feature_id")]
    public string FeatureId { get; set; } = string.Empty;

    [JsonPropertyName("source_geometry_id")]
    public string SourceGeometryId { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("right")]
    public int Right { get; set; }

    [JsonPropertyName("bottom")]
    public int Bottom { get; set; }

    [JsonPropertyName("scale")]
    public int Scale { get; set; }

    [JsonPropertyName("overlay_x")]
    public int OverlayX { get; set; }

    [JsonPropertyName("overlay_y")]
    public int OverlayY { get; set; }

    [JsonPropertyName("overlay_width")]
    public int OverlayWidth { get; set; }

    [JsonPropertyName("overlay_height")]
    public int OverlayHeight { get; set; }

    [JsonPropertyName("draw_order")]
    public int DrawOrder { get; set; }
}
