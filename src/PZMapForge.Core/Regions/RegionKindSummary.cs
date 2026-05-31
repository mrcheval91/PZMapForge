namespace PZMapForge.Core.Regions;

public sealed class RegionKindSummary
{
    public string Kind                 { get; }
    public char   Code                 { get; }
    public int    RegionCount          { get; internal set; }
    public int    TotalPixels          { get; internal set; }
    public int    LargestRegionPixels  { get; internal set; }

    internal RegionKindSummary(string kind, char code)
    {
        Kind = kind;
        Code = code;
    }

    public static RegionKindSummary CreateForTesting(
        string kind, char code,
        int regionCount = 0, int totalPixels = 0, int largestRegionPixels = 0) =>
        new(kind, code)
        {
            RegionCount          = regionCount,
            TotalPixels          = totalPixels,
            LargestRegionPixels  = largestRegionPixels,
        };
}
