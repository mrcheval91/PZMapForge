namespace PZMapForge.Core.Layers;

public sealed class LayerMergeConflict
{
    public int         X             { get; set; }
    public int         Y             { get; set; }
    public string      ChosenLayer   { get; set; } = string.Empty;
    public string      ChosenKind    { get; set; } = string.Empty;
    public List<string> LosingLayers { get; set; } = [];
    public List<string> LosingKinds  { get; set; } = [];
}
