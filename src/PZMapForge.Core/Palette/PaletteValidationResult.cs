namespace PZMapForge.Core.Palette;

public sealed class PaletteValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public PaletteDocument? Document { get; set; }
    public List<string> Errors { get; set; } = [];
}
