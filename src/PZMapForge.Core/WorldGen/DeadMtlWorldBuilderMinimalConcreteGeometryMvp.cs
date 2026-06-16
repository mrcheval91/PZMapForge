using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryMvpResult
{
    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("generated_utc")]
    public string GeneratedUtc { get; set; } = string.Empty;

    [JsonPropertyName("source_png_path")]
    public string SourcePngPath { get; set; } = string.Empty;

    [JsonPropertyName("source_connected_components_path")]
    public string SourceConnectedComponentsPath { get; set; } = string.Empty;

    [JsonPropertyName("source_access_profile_path")]
    public string SourceAccessProfilePath { get; set; } = string.Empty;

    [JsonPropertyName("target_component_order")]
    public int TargetComponentOrder { get; set; }

    [JsonPropertyName("geometry_mvp_contract")]
    public DeadMtlWorldBuilderMinimalConcreteGeometryMvpContract GeometryMvpContract { get; set; } = new();

    [JsonPropertyName("component_geometry")]
    public DeadMtlWorldBuilderMinimalConcreteComponentGeometryRecord? ComponentGeometry { get; set; }

    [JsonPropertyName("lot_geometry")]
    public List<DeadMtlWorldBuilderMinimalConcreteLotGeometryRecord> LotGeometry { get; set; } = new();

    [JsonPropertyName("building_slot_geometry")]
    public List<DeadMtlWorldBuilderMinimalConcreteBuildingSlotGeometryRecord> BuildingSlotGeometry { get; set; } = new();

    [JsonPropertyName("claim_boundary")]
    public DeadMtlWorldBuilderMinimalConcreteClaimBoundary ClaimBoundary { get; set; } = new();

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();

    [JsonPropertyName("is_valid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("verdict")]
    public string Verdict { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryMvpContract
{
    [JsonPropertyName("tile_id")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("target_component_order")]
    public int TargetComponentOrder { get; set; }

    [JsonPropertyName("target_component_id")]
    public string TargetComponentId { get; set; } = string.Empty;

    [JsonPropertyName("target_intent")]
    public string TargetIntent { get; set; } = string.Empty;

    [JsonPropertyName("access_readiness_class")]
    public string AccessReadinessClass { get; set; } = string.Empty;

    [JsonPropertyName("primary_frontage_component_id")]
    public string PrimaryFrontageComponentId { get; set; } = string.Empty;

    [JsonPropertyName("primary_frontage_component_order")]
    public int PrimaryFrontageComponentOrder { get; set; }

    [JsonPropertyName("primary_rear_service_component_id")]
    public string PrimaryRearServiceComponentId { get; set; } = string.Empty;

    [JsonPropertyName("primary_rear_service_component_order")]
    public int PrimaryRearServiceComponentOrder { get; set; }

    [JsonPropertyName("component_bbox_min_x")]
    public int ComponentBboxMinX { get; set; }

    [JsonPropertyName("component_bbox_min_y")]
    public int ComponentBboxMinY { get; set; }

    [JsonPropertyName("component_bbox_max_x")]
    public int ComponentBboxMaxX { get; set; }

    [JsonPropertyName("component_bbox_max_y")]
    public int ComponentBboxMaxY { get; set; }

    [JsonPropertyName("component_bbox_width_px")]
    public int ComponentBboxWidthPx { get; set; }

    [JsonPropertyName("component_bbox_height_px")]
    public int ComponentBboxHeightPx { get; set; }

    [JsonPropertyName("component_pixel_count")]
    public int ComponentPixelCount { get; set; }

    [JsonPropertyName("frontage_side")]
    public string FrontageSide { get; set; } = string.Empty;

    [JsonPropertyName("frontage_contact_px")]
    public int FrontageContactPx { get; set; }

    [JsonPropertyName("rear_service_side")]
    public string RearServiceSide { get; set; } = string.Empty;

    [JsonPropertyName("rear_service_contact_px")]
    public int RearServiceContactPx { get; set; }

    [JsonPropertyName("lot_geometry_count")]
    public int LotGeometryCount { get; set; }

    [JsonPropertyName("building_slot_geometry_count")]
    public int BuildingSlotGeometryCount { get; set; }

    [JsonPropertyName("accepted_building_slot_count")]
    public int AcceptedBuildingSlotCount { get; set; }

    [JsonPropertyName("rejected_building_slot_count")]
    public int RejectedBuildingSlotCount { get; set; }

    [JsonPropertyName("created_geometry_count")]
    public int CreatedGeometryCount { get; set; }

    [JsonPropertyName("writer_ready_geometry_count")]
    public int WriterReadyGeometryCount { get; set; }

    [JsonPropertyName("runtime_validated_geometry_count")]
    public int RuntimeValidatedGeometryCount { get; set; }

    [JsonPropertyName("materialized_geometry_count")]
    public int MaterializedGeometryCount { get; set; }

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = string.Empty;

    [JsonPropertyName("coordinate_system")]
    public string CoordinateSystem { get; set; } = "X_EAST_Y_SOUTH_Z_ZERO";

    [JsonPropertyName("geometry_units")]
    public string GeometryUnits { get; set; } = "SOURCE_PIXELS_EQUALS_PZ_WORLD_TILES";
}

public sealed class DeadMtlWorldBuilderMinimalConcreteComponentGeometryRecord
{
    [JsonPropertyName("component_order")]
    public int ComponentOrder { get; set; }

    [JsonPropertyName("component_id")]
    public string ComponentId { get; set; } = string.Empty;

    [JsonPropertyName("intent")]
    public string Intent { get; set; } = string.Empty;

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

    [JsonPropertyName("pixel_count")]
    public int PixelCount { get; set; }

    [JsonPropertyName("frontage_side")]
    public string FrontageSide { get; set; } = string.Empty;

    [JsonPropertyName("rear_service_side")]
    public string RearServiceSide { get; set; } = string.Empty;

    [JsonPropertyName("geometry_type")]
    public string GeometryType { get; set; } = "COMPONENT_BBOX_MVP";

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = "CONCRETE_PIXEL_GEOMETRY_CREATED";
}

public sealed class DeadMtlWorldBuilderMinimalConcreteLotGeometryRecord
{
    [JsonPropertyName("lot_order")]
    public int LotOrder { get; set; }

    [JsonPropertyName("lot_id")]
    public string LotId { get; set; } = string.Empty;

    [JsonPropertyName("component_order")]
    public int ComponentOrder { get; set; }

    [JsonPropertyName("component_id")]
    public string ComponentId { get; set; } = string.Empty;

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

    [JsonPropertyName("frontage_side")]
    public string FrontageSide { get; set; } = string.Empty;

    [JsonPropertyName("rear_service_side")]
    public string RearServiceSide { get; set; } = string.Empty;

    [JsonPropertyName("geometry_type")]
    public string GeometryType { get; set; } = "LOT_RECTANGLE_MVP";

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = "CONCRETE_PIXEL_GEOMETRY_CREATED";
}

public sealed class DeadMtlWorldBuilderMinimalConcreteBuildingSlotGeometryRecord
{
    [JsonPropertyName("slot_order")]
    public int SlotOrder { get; set; }

    [JsonPropertyName("slot_id")]
    public string SlotId { get; set; } = string.Empty;

    [JsonPropertyName("lot_id")]
    public string LotId { get; set; } = string.Empty;

    [JsonPropertyName("lot_order")]
    public int LotOrder { get; set; }

    [JsonPropertyName("component_order")]
    public int ComponentOrder { get; set; }

    [JsonPropertyName("component_id")]
    public string ComponentId { get; set; } = string.Empty;

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

    [JsonPropertyName("frontage_setback_px")]
    public int FrontageSetbackPx { get; set; } = 3;

    [JsonPropertyName("rear_setback_px")]
    public int RearSetbackPx { get; set; } = 3;

    [JsonPropertyName("side_inset_px")]
    public int SideInsetPx { get; set; } = 2;

    [JsonPropertyName("slot_status")]
    public string SlotStatus { get; set; } = string.Empty;

    [JsonPropertyName("geometry_type")]
    public string GeometryType { get; set; } = "BUILDING_SLOT_RECTANGLE_MVP";

    [JsonPropertyName("geometry_status")]
    public string GeometryStatus { get; set; } = string.Empty;
}

public sealed class DeadMtlWorldBuilderMinimalConcreteClaimBoundary
{
    [JsonPropertyName("writes_lotpack")]
    public bool WritesLotpack { get; set; }

    [JsonPropertyName("writes_worldgen_lua")]
    public bool WritesWorldgenLua { get; set; }

    [JsonPropertyName("runtime_proven")]
    public bool RuntimeProven { get; set; }

    [JsonPropertyName("public_playable_claim")]
    public bool PublicPlayableClaim { get; set; }

    [JsonPropertyName("writer_ready_claim")]
    public bool WriterReadyClaim { get; set; }

    [JsonPropertyName("writer_ready_geometry_count")]
    public int WriterReadyGeometryCount { get; set; }

    [JsonPropertyName("runtime_validated_geometry_count")]
    public int RuntimeValidatedGeometryCount { get; set; }

    [JsonPropertyName("materialized_geometry_count")]
    public int MaterializedGeometryCount { get; set; }
}
