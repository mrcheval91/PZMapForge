namespace PZMapForge.Core.Regions;

public sealed class RegionBounds
{
    public int X      { get; }
    public int Y      { get; }
    public int Width  { get; }
    public int Height { get; }

    public RegionBounds(int x, int y, int width, int height)
    {
        X      = x;
        Y      = y;
        Width  = width;
        Height = height;
    }

    public bool Contains(double cx, double cy) =>
        cx >= X && cx <= X + Width  - 1 &&
        cy >= Y && cy <= Y + Height - 1;
}
