namespace PZMapForge.Core.Primitives;

public sealed class PrimitiveClassificationResult
{
    public IReadOnlyList<PlanningPrimitive>     Primitives              { get; }
    public IReadOnlyList<PrimitiveKindSummary>  SummaryByPrimitiveType  { get; }
    public int                                  PrimitiveCount          => Primitives.Count;
    public int                                  TotalPixels             => SummaryByPrimitiveType.Sum(s => s.TotalPixels);

    internal PrimitiveClassificationResult(
        IReadOnlyList<PlanningPrimitive>    primitives,
        IReadOnlyList<PrimitiveKindSummary> summary)
    {
        Primitives             = primitives;
        SummaryByPrimitiveType = summary;
    }
}
