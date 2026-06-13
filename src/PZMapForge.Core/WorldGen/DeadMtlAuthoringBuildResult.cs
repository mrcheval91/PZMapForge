namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlAuthoringBuildResult
{
    public bool         IsValid           => Errors.Count == 0;
    public List<string> Errors            { get; } = [];
    public List<string> Warnings          { get; } = [];
    public int          ValidationChecks  { get; set; }
    public string?      MapId             { get; set; }
    public int          ModuleCount       { get; set; }
    public bool         LuaHasBom         { get; set; }
    public bool         LuaHasNonAscii    { get; set; }
    public string?      ManifestJsonPath  { get; set; }
    public string?      LuaPath           { get; set; }
    public string?      ProofSummaryPath  { get; set; }
}
