using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed record System2StaticRoadPlacementRecord(
    [property: JsonPropertyName("world_x")]   int WorldX,
    [property: JsonPropertyName("world_y")]   int WorldY,
    [property: JsonPropertyName("pixel_x")]   int PixelX,
    [property: JsonPropertyName("pixel_y")]   int PixelY,
    [property: JsonPropertyName("layer_id")]  string LayerId,
    [property: JsonPropertyName("class")]     string LayerClass,
    [property: JsonPropertyName("intent")]    string Intent,
    [property: JsonPropertyName("role")]      string Role,
    [property: JsonPropertyName("color")]     string Color);
