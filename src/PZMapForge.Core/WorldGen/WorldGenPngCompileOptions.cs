namespace PZMapForge.Core.WorldGen;

public sealed class WorldGenPngCompileOptions
{
    public bool IgnoreUnknown { get; init; }
    public static WorldGenPngCompileOptions Default { get; } = new();
}
