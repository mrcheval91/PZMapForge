namespace PZMapForge.Core.LocalPz;

public sealed class LocalPzInstallValidationResult
{
    public const string ExpectedClaimBoundary = "planning_artifact_only_not_pz_load_tested";

    public string ClaimBoundary { get; set; } = ExpectedClaimBoundary;

    public bool IsValid => Errors.Count == 0;

    public LocalPzInstallConfig? Config { get; set; }

    public LocalPzInstallValidationSummary Summary { get; set; } = new();

    public List<string> Errors { get; set; } = [];
}
