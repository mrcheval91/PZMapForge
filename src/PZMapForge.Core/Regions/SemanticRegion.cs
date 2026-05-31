namespace PZMapForge.Core.Regions;

public sealed class SemanticRegion
{
    public int            RegionId   { get; internal set; }
    public string         Kind       { get; }
    public char           Code       { get; }
    public int            PixelCount { get; }
    public RegionBounds   Bounds     { get; }
    public RegionCentroid Centroid   { get; }

    internal SemanticRegion(
        string kind, char code, int pixelCount,
        RegionBounds bounds, RegionCentroid centroid)
    {
        Kind       = kind;
        Code       = code;
        PixelCount = pixelCount;
        Bounds     = bounds;
        Centroid   = centroid;
    }

    public static SemanticRegion CreateForTesting(
        string kind, char code, int pixelCount,
        RegionBounds bounds, RegionCentroid centroid,
        int regionId = 1) =>
        new(kind, code, pixelCount, bounds, centroid) { RegionId = regionId };
}
