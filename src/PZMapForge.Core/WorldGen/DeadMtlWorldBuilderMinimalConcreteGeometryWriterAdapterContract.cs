using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractResult
{
    [JsonPropertyName("format")] public string Format { get; set; } = string.Empty;
    [JsonPropertyName("generated_utc")] public string GeneratedUtc { get; set; } = string.Empty;
    [JsonPropertyName("map_id")] public string MapId { get; set; } = string.Empty;
    [JsonPropertyName("target_component_id")] public string TargetComponentId { get; set; } = string.Empty;
    [JsonPropertyName("source_audit_receipt_path")] public string SourceAuditReceiptPath { get; set; } = string.Empty;
    [JsonPropertyName("source_audit_receipt_sha256")] public string SourceAuditReceiptSha256 { get; set; } = string.Empty;
    [JsonPropertyName("source_audit_receipt_verdict")] public string SourceAuditReceiptVerdict { get; set; } = string.Empty;
    [JsonPropertyName("source_audit_receipt_is_valid")] public bool SourceAuditReceiptIsValid { get; set; }
    [JsonPropertyName("adapter_contract_status")] public string AdapterContractStatus { get; set; } = string.Empty;
    [JsonPropertyName("normalized_component")] public DeadMtlAdapterContractNormalizedComponent NormalizedComponent { get; set; } = new();
    [JsonPropertyName("normalized_lots")] public List<DeadMtlAdapterContractNormalizedLot> NormalizedLots { get; set; } = new();
    [JsonPropertyName("normalized_building_slots")] public List<DeadMtlAdapterContractNormalizedBuildingSlot> NormalizedBuildingSlots { get; set; } = new();
    [JsonPropertyName("normalized_access_records")] public List<DeadMtlAdapterContractNormalizedAccessRecord> NormalizedAccessRecords { get; set; } = new();
    [JsonPropertyName("forbidden_output_families")] public List<DeadMtlAdapterContractForbiddenOutputFamily> ForbiddenOutputFamilies { get; set; } = new();
    [JsonPropertyName("adapter_check_count")] public int AdapterCheckCount { get; set; }
    [JsonPropertyName("passed_adapter_check_count")] public int PassedAdapterCheckCount { get; set; }
    [JsonPropertyName("failed_adapter_check_count")] public int FailedAdapterCheckCount { get; set; }
    [JsonPropertyName("writer_ready")] public bool WriterReady { get; set; }
    [JsonPropertyName("runtime_valid")] public bool RuntimeValid { get; set; }
    [JsonPropertyName("materialized")] public bool Materialized { get; set; }
    [JsonPropertyName("approved_for_writer_experiment")] public bool ApprovedForWriterExperiment { get; set; }
    [JsonPropertyName("writer_experiment_gate_status")] public string WriterExperimentGateStatus { get; set; } = string.Empty;
    [JsonPropertyName("checks")] public List<DeadMtlAdapterContractCheck> Checks { get; set; } = new();
    [JsonPropertyName("is_valid")] public bool IsValid { get; set; }
    [JsonPropertyName("verdict")] public string Verdict { get; set; } = string.Empty;
    [JsonPropertyName("errors")] public List<string> Errors { get; set; } = new();
}

public sealed class DeadMtlAdapterContractNormalizedComponent
{
    [JsonPropertyName("record_kind")] public string RecordKind { get; set; } = string.Empty;
    [JsonPropertyName("map_id")] public string MapId { get; set; } = string.Empty;
    [JsonPropertyName("target_component_id")] public string TargetComponentId { get; set; } = string.Empty;
    [JsonPropertyName("intent")] public string Intent { get; set; } = string.Empty;
    [JsonPropertyName("access_readiness_class")] public string AccessReadinessClass { get; set; } = string.Empty;
    [JsonPropertyName("bbox_min_x")] public int BboxMinX { get; set; }
    [JsonPropertyName("bbox_min_y")] public int BboxMinY { get; set; }
    [JsonPropertyName("bbox_max_x")] public int BboxMaxX { get; set; }
    [JsonPropertyName("bbox_max_y")] public int BboxMaxY { get; set; }
    [JsonPropertyName("bbox_width_px")] public int BboxWidthPx { get; set; }
    [JsonPropertyName("bbox_height_px")] public int BboxHeightPx { get; set; }
    [JsonPropertyName("source_record_id")] public string SourceRecordId { get; set; } = string.Empty;
    [JsonPropertyName("writer_consumable")] public bool WriterConsumable { get; set; }
    [JsonPropertyName("runtime_consumable")] public bool RuntimeConsumable { get; set; }
}

public sealed class DeadMtlAdapterContractNormalizedLot
{
    [JsonPropertyName("lot_order")] public int LotOrder { get; set; }
    [JsonPropertyName("lot_id")] public string LotId { get; set; } = string.Empty;
    [JsonPropertyName("component_id")] public string ComponentId { get; set; } = string.Empty;
    [JsonPropertyName("min_x")] public int MinX { get; set; }
    [JsonPropertyName("min_y")] public int MinY { get; set; }
    [JsonPropertyName("max_x")] public int MaxX { get; set; }
    [JsonPropertyName("max_y")] public int MaxY { get; set; }
    [JsonPropertyName("width_px")] public int WidthPx { get; set; }
    [JsonPropertyName("height_px")] public int HeightPx { get; set; }
    [JsonPropertyName("frontage_side")] public string FrontageSide { get; set; } = string.Empty;
    [JsonPropertyName("rear_service_side")] public string RearServiceSide { get; set; } = string.Empty;
    [JsonPropertyName("source_geometry_type")] public string SourceGeometryType { get; set; } = string.Empty;
    [JsonPropertyName("source_geometry_status")] public string SourceGeometryStatus { get; set; } = string.Empty;
    [JsonPropertyName("writer_consumable")] public bool WriterConsumable { get; set; }
    [JsonPropertyName("runtime_consumable")] public bool RuntimeConsumable { get; set; }
}

public sealed class DeadMtlAdapterContractNormalizedBuildingSlot
{
    [JsonPropertyName("slot_order")] public int SlotOrder { get; set; }
    [JsonPropertyName("slot_id")] public string SlotId { get; set; } = string.Empty;
    [JsonPropertyName("lot_id")] public string LotId { get; set; } = string.Empty;
    [JsonPropertyName("lot_order")] public int LotOrder { get; set; }
    [JsonPropertyName("component_id")] public string ComponentId { get; set; } = string.Empty;
    [JsonPropertyName("min_x")] public int MinX { get; set; }
    [JsonPropertyName("min_y")] public int MinY { get; set; }
    [JsonPropertyName("max_x")] public int MaxX { get; set; }
    [JsonPropertyName("max_y")] public int MaxY { get; set; }
    [JsonPropertyName("width_px")] public int WidthPx { get; set; }
    [JsonPropertyName("height_px")] public int HeightPx { get; set; }
    [JsonPropertyName("frontage_setback_px")] public int FrontageSetbackPx { get; set; }
    [JsonPropertyName("rear_setback_px")] public int RearSetbackPx { get; set; }
    [JsonPropertyName("side_inset_px")] public int SideInsetPx { get; set; }
    [JsonPropertyName("slot_status")] public string SlotStatus { get; set; } = string.Empty;
    [JsonPropertyName("source_geometry_type")] public string SourceGeometryType { get; set; } = string.Empty;
    [JsonPropertyName("source_geometry_status")] public string SourceGeometryStatus { get; set; } = string.Empty;
    [JsonPropertyName("writer_consumable")] public bool WriterConsumable { get; set; }
    [JsonPropertyName("runtime_consumable")] public bool RuntimeConsumable { get; set; }
}

public sealed class DeadMtlAdapterContractNormalizedAccessRecord
{
    [JsonPropertyName("access_order")] public int AccessOrder { get; set; }
    [JsonPropertyName("access_id")] public string AccessId { get; set; } = string.Empty;
    [JsonPropertyName("access_kind")] public string AccessKind { get; set; } = string.Empty;
    [JsonPropertyName("side")] public string Side { get; set; } = string.Empty;
    [JsonPropertyName("component_id")] public string ComponentId { get; set; } = string.Empty;
    [JsonPropertyName("contact_px")] public int ContactPx { get; set; }
    [JsonPropertyName("writer_consumable")] public bool WriterConsumable { get; set; }
    [JsonPropertyName("runtime_consumable")] public bool RuntimeConsumable { get; set; }
}

public sealed class DeadMtlAdapterContractForbiddenOutputFamily
{
    [JsonPropertyName("family_order")] public int FamilyOrder { get; set; }
    [JsonPropertyName("family_id")] public string FamilyId { get; set; } = string.Empty;
    [JsonPropertyName("blocked_pattern")] public string BlockedPattern { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("details")] public string Details { get; set; } = string.Empty;
}

public sealed class DeadMtlAdapterContractCheck
{
    [JsonPropertyName("check_order")] public int CheckOrder { get; set; }
    [JsonPropertyName("check_id")] public string CheckId { get; set; } = string.Empty;
    [JsonPropertyName("check_label")] public string CheckLabel { get; set; } = string.Empty;
    [JsonPropertyName("check_status")] public string CheckStatus { get; set; } = string.Empty;
    [JsonPropertyName("expected")] public string Expected { get; set; } = string.Empty;
    [JsonPropertyName("actual")] public string Actual { get; set; } = string.Empty;
    [JsonPropertyName("details")] public string Details { get; set; } = string.Empty;
}
