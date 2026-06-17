using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketResult
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    [JsonPropertyName("generated_utc")]
    public string GeneratedUtc { get; set; } = string.Empty;

    [JsonPropertyName("map_id")]
    public string MapId { get; set; } = string.Empty;

    [JsonPropertyName("geometry_mvp_path")]
    public string GeometryMvpPath { get; set; } = string.Empty;

    [JsonPropertyName("qa_overlay_json_path")]
    public string QaOverlayJsonPath { get; set; } = string.Empty;

    [JsonPropertyName("qa_overlay_csv_path")]
    public string QaOverlayCsvPath { get; set; } = string.Empty;

    [JsonPropertyName("qa_overlay_png_path")]
    public string QaOverlayPngPath { get; set; } = string.Empty;

    [JsonPropertyName("target_component_id")]
    public string TargetComponentId { get; set; } = string.Empty;

    [JsonPropertyName("target_component_order")]
    public int TargetComponentOrder { get; set; }

    [JsonPropertyName("intent")]
    public string Intent { get; set; } = string.Empty;

    [JsonPropertyName("access_readiness_class")]
    public string AccessReadinessClass { get; set; } = string.Empty;

    [JsonPropertyName("component_bbox")]
    public DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBbox? ComponentBbox { get; set; }

    [JsonPropertyName("source_dimensions")]
    public string SourceDimensions { get; set; } = string.Empty;

    [JsonPropertyName("lot_count")]
    public int LotCount { get; set; }

    [JsonPropertyName("accepted_building_slot_count")]
    public int AcceptedBuildingSlotCount { get; set; }

    [JsonPropertyName("overlay_feature_count")]
    public int OverlayFeatureCount { get; set; }

    [JsonPropertyName("csv_feature_count")]
    public int CsvFeatureCount { get; set; }

    [JsonPropertyName("review_check_count")]
    public int ReviewCheckCount { get; set; }

    [JsonPropertyName("passed_review_check_count")]
    public int PassedReviewCheckCount { get; set; }

    [JsonPropertyName("failed_review_check_count")]
    public int FailedReviewCheckCount { get; set; }

    [JsonPropertyName("writer_ready")]
    public bool WriterReady { get; set; }

    [JsonPropertyName("runtime_valid")]
    public bool RuntimeValid { get; set; }

    [JsonPropertyName("materialized")]
    public bool Materialized { get; set; }

    [JsonPropertyName("is_valid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("verdict")]
    public string Verdict { get; set; } = string.Empty;

    [JsonPropertyName("checks")]
    public List<DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketCheck> Checks { get; set; } = new();

    [JsonPropertyName("feature_breakdown")]
    public List<DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketFeatureBreakdown> FeatureBreakdown { get; set; } = new();

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();
}

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketBbox
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

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketCheck
{
    [JsonPropertyName("check_order")]
    public int CheckOrder { get; set; }

    [JsonPropertyName("check_id")]
    public string CheckId { get; set; } = string.Empty;

    [JsonPropertyName("check_label")]
    public string CheckLabel { get; set; } = string.Empty;

    [JsonPropertyName("check_status")]
    public string CheckStatus { get; set; } = string.Empty;

    [JsonPropertyName("expected")]
    public string Expected { get; set; } = string.Empty;

    [JsonPropertyName("actual")]
    public string Actual { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string Details { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryQaReviewPacketFeatureBreakdown
{
    [JsonPropertyName("feature_kind")]
    public string FeatureKind { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }
}
