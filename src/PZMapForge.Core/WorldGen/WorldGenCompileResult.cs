namespace PZMapForge.Core.WorldGen;

public sealed class WorldGenCompileResult
{
    public string MapId      { get; set; } = string.Empty;
    public int    ModuleCount { get; set; }
    public string? Lua       { get; set; }
    public List<string> Errors { get; } = [];
    public bool IsValid => Errors.Count == 0;
}
