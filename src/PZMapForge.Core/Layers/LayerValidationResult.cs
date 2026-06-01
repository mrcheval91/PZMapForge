namespace PZMapForge.Core.Layers;

public sealed class LayerValidationResult
{
    public bool IsValid =>
        Errors.Count == 0 && LayerResults.All(l => l.IsValid);

    public List<string>                    Errors       { get; set; } = [];
    public List<LayerValidationLayerResult> LayerResults { get; set; } = [];
    public List<string>                    Precedence   { get; set; } = [];

    public string ClaimBoundary { get; } = "planning_artifact_only_not_pz_load_tested";
}
