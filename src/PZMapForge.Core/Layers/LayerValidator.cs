using System.Runtime.Versioning;
using PZMapForge.Core.ImageParsing;
using PZMapForge.Core.Palette;

namespace PZMapForge.Core.Layers;

/// <summary>
/// Validates a layer manifest and all referenced layer images without writing artifacts.
/// Reports manifest errors, missing images, parse errors, and disallowed kind violations.
/// Does not touch media/maps. Does not claim playable PZ export.
/// Claim boundary: planning_artifact_only_not_pz_load_tested
/// Windows-only: delegates image parsing to ImageMapForgeParser (GDI+).
/// </summary>
[SupportedOSPlatform("windows")]
public static class LayerValidator
{
    public static LayerValidationResult Validate(
        string            manifestPath,
        string            palettePath,
        LayerMergeOptions? options = null)
    {
        options ??= LayerMergeOptions.Default;
        var result = new LayerValidationResult();

        // 1. Load and validate manifest
        var manifestResult = LayerManifestLoader.Load(manifestPath);
        if (!manifestResult.IsValid)
        {
            result.Errors.AddRange(manifestResult.Errors);
            return result;
        }
        var manifest = manifestResult.Document!;
        result.Precedence = manifest.Precedence.ToList();

        // 2. Load palette
        var paletteResult = PaletteLoader.Load(palettePath);
        if (!paletteResult.IsValid)
        {
            foreach (var e in paletteResult.Errors)
                result.Errors.Add($"Palette: {e}");
            return result;
        }

        // 3. Resolve manifest directory for relative layer paths
        var manifestDir = Path.GetDirectoryName(Path.GetFullPath(manifestPath)) ?? "";
        var parseOpts   = new ImageMapForgeOptions { Resize = options.Resize };

        // 4. Validate each layer
        foreach (var layer in manifest.Layers)
        {
            var lr = new LayerValidationLayerResult
            {
                LayerName = layer.Name,
                FilePath  = layer.FilePath,
            };

            var resolved = Path.Combine(manifestDir, layer.FilePath);

            if (!File.Exists(resolved))
            {
                lr.Errors.Add($"Image not found: {resolved}");
                result.LayerResults.Add(lr);
                continue;
            }

            try
            {
                var parsed = ImageMapForgeParser.Parse(resolved, palettePath, parseOpts);
                lr.Width  = parsed.Width;
                lr.Height = parsed.Height;

                var allowed = new HashSet<string>(layer.AllowedKinds, StringComparer.Ordinal);

                foreach (var count in parsed.Counts)
                {
                    if (count.Pixels == 0) continue;
                    if (count.Kind == options.DefaultKind) continue;

                    lr.NonDefaultPixels += count.Pixels;

                    if (!allowed.Contains(count.Kind))
                    {
                        lr.InvalidPixels += count.Pixels;
                        lr.Errors.Add($"Kind '{count.Kind}' is not in allowed_kinds.");
                    }
                }
            }
            catch (Exception ex)
            {
                lr.Errors.Add($"Parse error: {ex.Message}");
            }

            result.LayerResults.Add(lr);
        }

        return result;
    }
}
