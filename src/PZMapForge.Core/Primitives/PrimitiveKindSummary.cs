namespace PZMapForge.Core.Primitives;

public sealed class PrimitiveKindSummary
{
    public PlanningPrimitiveType PrimitiveType        { get; }
    public string                PrimitiveTypeString  { get; }
    public int                   RegionCount          { get; internal set; }
    public int                   TotalPixels          { get; internal set; }
    public int                   LargestRegionPixels  { get; internal set; }

    internal PrimitiveKindSummary(PlanningPrimitiveType type, string typeString)
    {
        PrimitiveType       = type;
        PrimitiveTypeString = typeString;
    }
}
