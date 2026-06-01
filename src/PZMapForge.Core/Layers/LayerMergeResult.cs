using PZMapForge.Core.ParsedCell;

namespace PZMapForge.Core.Layers;

public sealed class LayerMergeResult
{
    public bool IsValid => Errors.Count == 0;

    public List<string> Errors { get; set; } = [];

    public int         Width              { get; set; }
    public int         Height             { get; set; }
    public List<string> Rows             { get; set; } = [];
    public SemanticGrid? Grid            { get; set; }

    public List<LayerMergeContribution> Contributions { get; set; } = [];
    public int                          TotalConflictCount { get; set; }
    public List<LayerMergeConflict>     ConflictSample     { get; set; } = [];

    public string ClaimBoundary { get; } = "planning_artifact_only_not_pz_load_tested";
}
