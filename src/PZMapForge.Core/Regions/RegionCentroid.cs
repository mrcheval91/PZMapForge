namespace PZMapForge.Core.Regions;

public sealed class RegionCentroid
{
    public double X { get; }
    public double Y { get; }

    public RegionCentroid(double x, double y)
    {
        X = x;
        Y = y;
    }
}
