namespace PZMapForge.Core.ParsedCell;

public sealed class ParsedCellLoadResult
{
    public bool IsValid => Errors.Count == 0;
    public ParsedCellDocument? Document { get; set; }
    public SemanticGrid? Grid { get; set; }
    public List<string> Errors { get; set; } = [];
}
