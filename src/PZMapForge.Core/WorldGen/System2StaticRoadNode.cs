using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed record System2StaticRoadNode(
    [property: JsonPropertyName("pixel_x")] int PixelX,
    [property: JsonPropertyName("pixel_y")] int PixelY,
    [property: JsonPropertyName("world_x")] int WorldX,
    [property: JsonPropertyName("world_y")] int WorldY,
    [property: JsonPropertyName("intent")]  string Intent,
    [property: JsonPropertyName("color")]   string Color);
