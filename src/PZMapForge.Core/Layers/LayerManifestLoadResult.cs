namespace PZMapForge.Core.Layers;

public sealed class LayerManifestLoadResult
{
    public bool IsValid => Errors.Count == 0;
    public LayerManifest? Document { get; set; }
    public List<string> Errors { get; set; } = [];
}
