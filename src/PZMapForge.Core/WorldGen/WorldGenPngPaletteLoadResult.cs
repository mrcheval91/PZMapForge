namespace PZMapForge.Core.WorldGen;

public sealed class WorldGenPngPaletteLoadResult
{
    public WorldGenPngPalette? Palette { get; set; }
    public List<string> Errors { get; } = [];
    public bool IsValid => Errors.Count == 0;
}
