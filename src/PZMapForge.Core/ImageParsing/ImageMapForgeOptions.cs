namespace PZMapForge.Core.ImageParsing;

/// <summary>Options for ImageMapForgeParser.Parse().</summary>
public sealed class ImageMapForgeOptions
{
    public static readonly ImageMapForgeOptions Default = new();

    /// <summary>
    /// When true, resize non-300x300 images to 300x300 using nearest-neighbour sampling.
    /// When false (default), non-300x300 images throw ArgumentException.
    /// </summary>
    public bool Resize { get; init; } = false;
}
