using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class WorldGenPngPalette
{
    [JsonPropertyName("format")]
    public string Format { get; init; } = string.Empty;

    [JsonPropertyName("entries")]
    public List<WorldGenPngPaletteEntry> Entries { get; init; } = [];
}
