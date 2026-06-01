using PZMapForge.Core.ParsedCell;

namespace PZMapForge.Core.ImageParsing;

/// <summary>
/// Result of parsing a PNG/BMP blockout image through the ImageMapForge palette.
/// Mirrors the parsed-cell artifact fields produced by scripts/image-mapforge.ps1.
/// Claim boundary: planning_artifact_only_not_pz_load_tested
/// </summary>
public sealed class ImageMapForgeResult
{
    public string   ClaimBoundary   { get; } = "planning_artifact_only_not_pz_load_tested";
    public int      Width           { get; }
    public int      Height          { get; }
    public bool     Resized         { get; }
    public string   PaletteSha256   { get; }

    public IReadOnlyList<string>          Rows          { get; }
    public IReadOnlyList<ParsedCellCount> Counts        { get; }
    public ParsedCellMatching             Matching      { get; }
    public IReadOnlyList<ParsedCellDrift> NearestDrift  { get; }

    internal ImageMapForgeResult(
        int width, int height, bool resized, string paletteSha256,
        IReadOnlyList<string>          rows,
        IReadOnlyList<ParsedCellCount> counts,
        ParsedCellMatching             matching,
        IReadOnlyList<ParsedCellDrift> nearestDrift)
    {
        Width         = width;
        Height        = height;
        Resized       = resized;
        PaletteSha256 = paletteSha256;
        Rows          = rows;
        Counts        = counts;
        Matching      = matching;
        NearestDrift  = nearestDrift;
    }

    /// <summary>Builds a SemanticGrid from the parsed rows for downstream pipeline use.</summary>
    public SemanticGrid BuildGrid() =>
        SemanticGrid.CreateForTesting(Width, Height, Rows);
}
