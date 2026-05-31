namespace PZMapForge.Core.Regions;

public sealed class RegionExtractionResult
{
    public IReadOnlyList<SemanticRegion>    Regions          { get; }
    public IReadOnlyList<RegionKindSummary> SummaryByKind    { get; }
    public int                             TotalRegions      => Regions.Count;
    public int                             TotalPixels       => SummaryByKind.Sum(s => s.TotalPixels);

    internal RegionExtractionResult(
        IReadOnlyList<SemanticRegion>    regions,
        IReadOnlyList<RegionKindSummary> summary)
    {
        Regions       = regions;
        SummaryByKind = summary;
    }

    public static RegionExtractionResult CreateForTesting(
        IReadOnlyList<SemanticRegion>    regions,
        IReadOnlyList<RegionKindSummary> summary) =>
        new(regions, summary);
}
