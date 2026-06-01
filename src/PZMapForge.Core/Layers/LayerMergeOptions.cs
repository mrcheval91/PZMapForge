namespace PZMapForge.Core.Layers;

public sealed class LayerMergeOptions
{
    public static readonly LayerMergeOptions Default = new();

    public bool   Resize      { get; init; } = false;
    public string DefaultKind { get; init; } = "grass";
}
