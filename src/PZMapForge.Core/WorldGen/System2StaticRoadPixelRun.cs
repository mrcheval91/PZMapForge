using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed record System2StaticRoadPixelRun(
    [property: JsonPropertyName("y")]             int Y,
    [property: JsonPropertyName("x_start")]       int XStart,
    [property: JsonPropertyName("x_end")]         int XEnd,
    [property: JsonPropertyName("world_y")]       int WorldY,
    [property: JsonPropertyName("world_x_start")] int WorldXStart,
    [property: JsonPropertyName("world_x_end")]   int WorldXEnd,
    [property: JsonPropertyName("intent")]        string Intent,
    [property: JsonPropertyName("color")]         string Color);
