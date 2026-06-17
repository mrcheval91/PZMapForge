using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestResult
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    [JsonPropertyName("generated_utc")]
    public string GeneratedUtc { get; set; } = string.Empty;

    [JsonPropertyName("map_id")]
    public string MapId { get; set; } = string.Empty;

    [JsonPropertyName("target_component_id")]
    public string TargetComponentId { get; set; } = string.Empty;

    [JsonPropertyName("target_component_order")]
    public int TargetComponentOrder { get; set; }

    [JsonPropertyName("intent")]
    public string Intent { get; set; } = string.Empty;

    [JsonPropertyName("access_readiness_class")]
    public string AccessReadinessClass { get; set; } = string.Empty;

    [JsonPropertyName("component_bbox")]
    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBbox? ComponentBbox { get; set; }

    [JsonPropertyName("source_dimensions")]
    public string SourceDimensions { get; set; } = string.Empty;

    [JsonPropertyName("lot_count")]
    public int LotCount { get; set; }

    [JsonPropertyName("accepted_building_slot_count")]
    public int AcceptedBuildingSlotCount { get; set; }

    [JsonPropertyName("overlay_feature_count")]
    public int OverlayFeatureCount { get; set; }

    [JsonPropertyName("review_check_count")]
    public int ReviewCheckCount { get; set; }

    [JsonPropertyName("passed_review_check_count")]
    public int PassedReviewCheckCount { get; set; }

    [JsonPropertyName("failed_review_check_count")]
    public int FailedReviewCheckCount { get; set; }

    [JsonPropertyName("input_artifact_count")]
    public int InputArtifactCount { get; set; }

    [JsonPropertyName("hashed_input_artifact_count")]
    public int HashedInputArtifactCount { get; set; }

    [JsonPropertyName("writer_ready")]
    public bool WriterReady { get; set; }

    [JsonPropertyName("runtime_valid")]
    public bool RuntimeValid { get; set; }

    [JsonPropertyName("materialized")]
    public bool Materialized { get; set; }

    [JsonPropertyName("approved_for_writer_experiment")]
    public bool ApprovedForWriterExperiment { get; set; }

    [JsonPropertyName("writer_experiment_gate_status")]
    public string WriterExperimentGateStatus { get; set; } = string.Empty;

    [JsonPropertyName("manifest_check_count")]
    public int ManifestCheckCount { get; set; }

    [JsonPropertyName("passed_manifest_check_count")]
    public int PassedManifestCheckCount { get; set; }

    [JsonPropertyName("failed_manifest_check_count")]
    public int FailedManifestCheckCount { get; set; }

    [JsonPropertyName("is_valid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("verdict")]
    public string Verdict { get; set; } = string.Empty;

    [JsonPropertyName("input_artifacts")]
    public List<DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestArtifact> InputArtifacts { get; set; } = new();

    [JsonPropertyName("checks")]
    public List<DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestCheck> Checks { get; set; } = new();

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();
}

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestBbox
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

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestArtifact
{
    [JsonPropertyName("artifact_order")]
    public int ArtifactOrder { get; set; }

    [JsonPropertyName("artifact_id")]
    public string ArtifactId { get; set; } = string.Empty;

    [JsonPropertyName("artifact_kind")]
    public string ArtifactKind { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("exists")]
    public bool Exists { get; set; }

    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = string.Empty;

    [JsonPropertyName("size_bytes")]
    public long SizeBytes { get; set; }

    [JsonPropertyName("source_task")]
    public string SourceTask { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }
}

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterInputManifestCheck
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
