using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class WorldGenManifest
{
    [JsonPropertyName("map_id")]
    public string MapId { get; init; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; init; } = string.Empty;

    [JsonPropertyName("origin_note")]
    public string OriginNote { get; init; } = string.Empty;

    [JsonPropertyName("static_modules")]
    public List<WorldGenModule> StaticModules { get; init; } = [];
}
