namespace PZMapForge.Core.LocalPz;

public sealed class LocalPzInstallConfigLoadResult
{
    public bool IsValid => Errors.Count == 0;
    public LocalPzInstallConfig? Document { get; set; }
    public List<string> Errors { get; set; } = [];
}
