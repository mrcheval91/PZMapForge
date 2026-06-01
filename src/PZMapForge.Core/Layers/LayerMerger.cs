using System.Runtime.Versioning;
using PZMapForge.Core.ImageParsing;
using PZMapForge.Core.Palette;
using PZMapForge.Core.ParsedCell;

namespace PZMapForge.Core.Layers;

/// <summary>
/// Merges multiple layer images into one semantic grid using manifest precedence.
/// Claim boundary: planning_artifact_only_not_pz_load_tested
/// Windows-only: delegates image parsing to ImageMapForgeParser (GDI+).
/// </summary>
[SupportedOSPlatform("windows")]
public static class LayerMerger
{
    private const int MaxConflictSample = 100;

    public static LayerMergeResult Merge(
        string           manifestPath,
        string           palettePath,
        LayerMergeOptions? options = null)
    {
        options ??= LayerMergeOptions.Default;
        var result = new LayerMergeResult();

        // 1. Load and validate manifest
        var manifestResult = LayerManifestLoader.Load(manifestPath);
        if (!manifestResult.IsValid)
        {
            result.Errors.AddRange(manifestResult.Errors);
            return result;
        }
        var manifest = manifestResult.Document!;

        // 2. Load palette and build code↔kind lookups
        var paletteResult = PaletteLoader.Load(palettePath);
        if (!paletteResult.IsValid)
        {
            foreach (var e in paletteResult.Errors)
                result.Errors.Add($"Palette: {e}");
            return result;
        }
        var palette = paletteResult.Document!;

        var codeToKind = palette.Kinds.ToDictionary(k => k.Code[0], k => k.Kind);
        var kindToCode = palette.Kinds.ToDictionary(k => k.Kind, k => k.Code[0]);

        if (!kindToCode.TryGetValue(options.DefaultKind, out var defaultCode))
        {
            result.Errors.Add($"DefaultKind '{options.DefaultKind}' not found in palette.");
            return result;
        }

        // 3. Resolve manifest directory for relative layer paths
        var manifestDir = Path.GetDirectoryName(Path.GetFullPath(manifestPath)) ?? "";

        // 4. Parse all layer images; collect errors but continue to report all missing files
        var parseOpts = new ImageMapForgeOptions { Resize = options.Resize };
        var parsedLayers = new Dictionary<string, ImageMapForgeResult>(StringComparer.Ordinal);

        foreach (var layer in manifest.Layers)
        {
            var resolved = Path.Combine(manifestDir, layer.FilePath);
            if (!File.Exists(resolved))
            {
                result.Errors.Add($"Layer '{layer.Name}' image not found: {resolved}");
                continue;
            }

            try
            {
                parsedLayers[layer.Name] = ImageMapForgeParser.Parse(resolved, palettePath, parseOpts);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Layer '{layer.Name}' parse error: {ex.Message}");
            }
        }

        if (result.Errors.Count > 0) return result;

        // 5. Validate allowed_kinds — check that every non-default kind present in
        //    each layer's image appears in that layer's allowed_kinds list.
        var allowedPerLayer = manifest.Layers
            .ToDictionary(l => l.Name, l => new HashSet<string>(l.AllowedKinds, StringComparer.Ordinal));

        foreach (var layer in manifest.Layers)
        {
            var parsed  = parsedLayers[layer.Name];
            var allowed = allowedPerLayer[layer.Name];

            foreach (var count in parsed.Counts)
            {
                if (count.Pixels == 0) continue;
                if (count.Kind == options.DefaultKind) continue;
                if (!allowed.Contains(count.Kind))
                    result.Errors.Add(
                        $"Layer '{layer.Name}': kind '{count.Kind}' (code '{count.Code}') is not in allowed_kinds.");
            }
        }

        if (result.Errors.Count > 0) return result;

        // 6. Build per-layer SemanticGrids and contribution trackers
        var layerGrids = manifest.Layers
            .ToDictionary(l => l.Name, l => parsedLayers[l.Name].BuildGrid());

        var contributions = manifest.Layers
            .ToDictionary(l => l.Name,
                l => new LayerMergeContribution { LayerName = l.Name, FilePath = l.FilePath });

        // 7. Merge cell by cell in precedence order (index 0 = highest priority)
        var orderedNames = manifest.Precedence.ToList();

        int w = manifest.Width;
        int h = manifest.Height;
        var mergedChars    = new char[h][];
        for (int y = 0; y < h; y++)
        {
            mergedChars[y] = new char[w];
            for (int x = 0; x < w; x++)
                mergedChars[y][x] = defaultCode;
        }

        var conflictSample = new List<LayerMergeConflict>();
        int totalConflicts = 0;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Collect non-default contributions in precedence order
                var cellContribs = new List<(string Name, char Code, string Kind)>();

                foreach (var name in orderedNames)
                {
                    var code = layerGrids[name].GetCode(x, y);
                    var kind = codeToKind[code];

                    if (kind == options.DefaultKind)
                        contributions[name].IgnoredDefaultPixels++;
                    else
                    {
                        contributions[name].ContributedPixels++;
                        cellContribs.Add((name, code, kind));
                    }
                }

                if (cellContribs.Count == 0)
                    continue; // all default; mergedChars already set to defaultCode

                // Winner = first in precedence order
                var (winName, winCode, winKind) = cellContribs[0];
                mergedChars[y][x] = winCode;
                contributions[winName].ChosenPixels++;

                if (cellContribs.Count >= 2)
                {
                    totalConflicts++;
                    var losers = cellContribs.Skip(1).ToList();

                    foreach (var (loserName, _, _) in losers)
                        contributions[loserName].OverriddenPixels++;

                    if (conflictSample.Count < MaxConflictSample)
                    {
                        conflictSample.Add(new LayerMergeConflict
                        {
                            X            = x,
                            Y            = y,
                            ChosenLayer  = winName,
                            ChosenKind   = winKind,
                            LosingLayers = losers.Select(l => l.Name).ToList(),
                            LosingKinds  = losers.Select(l => l.Kind).ToList(),
                        });
                    }
                }
            }
        }

        // 8. Build result
        var rows = mergedChars.Select(row => new string(row)).ToList();

        result.Width              = w;
        result.Height             = h;
        result.Rows               = rows;
        result.Grid               = SemanticGrid.CreateForTesting(w, h, rows);
        result.Contributions      = manifest.Layers.Select(l => contributions[l.Name]).ToList();
        result.TotalConflictCount = totalConflicts;
        result.ConflictSample     = conflictSample;
        return result;
    }
}
