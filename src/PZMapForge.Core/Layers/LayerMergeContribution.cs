namespace PZMapForge.Core.Layers;

public sealed class LayerMergeContribution
{
    public string LayerName            { get; set; } = string.Empty;
    public string FilePath             { get; set; } = string.Empty;
    public int    ContributedPixels    { get; set; }  // non-default pixels contributed by this layer
    public int    IgnoredDefaultPixels { get; set; }  // cells where this layer had only the default kind
    public int    InvalidPixels        { get; set; }  // always 0 in a successful merge (validated before merge)
    public int    ChosenPixels         { get; set; }  // cells where this layer's kind was selected as winner
    public int    OverriddenPixels     { get; set; }  // cells where this layer contributed but lost to higher precedence
}
