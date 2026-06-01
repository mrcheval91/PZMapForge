namespace PZMapForge.Core.Layers;

public sealed class LayerValidationLayerResult
{
    public bool IsValid => Errors.Count == 0;

    public string      LayerName        { get; set; } = string.Empty;
    public string      FilePath         { get; set; } = string.Empty;
    public List<string> Errors          { get; set; } = [];
    public int         NonDefaultPixels { get; set; }
    public int         InvalidPixels    { get; set; }
    public int         Width            { get; set; }
    public int         Height           { get; set; }
}
