namespace PZMapForge.Core.WorldGen;

public sealed class WorldGenProjectCompileResult
{
    public WorldGenManifest? Manifest   { get; set; }
    public int               ModuleCount { get; set; }
    public List<string>      Errors { get; } = [];
    public bool              IsValid => Errors.Count == 0;
}
