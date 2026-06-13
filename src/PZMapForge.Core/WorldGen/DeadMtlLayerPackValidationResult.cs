namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlLayerPackValidationResult
{
    public List<string> Errors   { get; } = [];
    public List<string> Warnings { get; } = [];
    public int          ChecksRun { get; set; }
    public bool         IsValid  => Errors.Count == 0;
}
