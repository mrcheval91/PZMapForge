namespace PZMapForge.Core.ParsedCell;

/// <summary>
/// Typed 2D grid of semantic kind codes parsed from a parsed-cell artifact.
/// Coordinates are (x, y) where x is column and y is row, both zero-based.
/// </summary>
public sealed class SemanticGrid
{
    private readonly string[] _rows;

    public int Width  { get; }
    public int Height { get; }

    internal SemanticGrid(int width, int height, IEnumerable<string> rows)
    {
        Width  = width;
        Height = height;
        _rows  = rows.ToArray();
    }

    public bool InBounds(int x, int y) =>
        x >= 0 && x < Width && y >= 0 && y < Height;

    /// <summary>Returns the single-character semantic code at (x, y).</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when (x, y) is outside the grid.</exception>
    public char GetCode(int x, int y)
    {
        if (!InBounds(x, y))
            throw new ArgumentOutOfRangeException(
                $"{nameof(x)}/{nameof(y)}",
                $"({x}, {y}) is out of bounds for a {Width}x{Height} grid.");
        return _rows[y][x];
    }

    /// <summary>Returns the count of cells whose code equals <paramref name="code"/>.</summary>
    public int CountCode(char code)
    {
        var count = 0;
        foreach (var row in _rows)
            foreach (var c in row)
                if (c == code) count++;
        return count;
    }
}
