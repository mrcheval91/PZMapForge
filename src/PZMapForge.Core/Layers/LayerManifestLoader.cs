using System.Text.Json;
using PZMapForge.Core.Primitives;

namespace PZMapForge.Core.Layers;

public static class LayerManifestLoader
{
    private const string RequiredSchema        = "pzmapforge.layer-manifest.v0.1";
    private const string RequiredClaimBoundary = "planning_artifact_only_not_pz_load_tested";
    private const int    RequiredWidth         = 300;
    private const int    RequiredHeight        = 300;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas     = true,
        ReadCommentHandling     = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = false,
    };

    public static LayerManifestLoadResult Load(string path)
    {
        var result = new LayerManifestLoadResult();

        if (!File.Exists(path))
        {
            result.Errors.Add($"Layer manifest file not found: {path}");
            return result;
        }

        LayerManifest doc;
        try
        {
            var json = File.ReadAllText(path);
            doc = JsonSerializer.Deserialize<LayerManifest>(json, JsonOptions)
                  ?? throw new JsonException("Deserialized to null.");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"JSON parse error: {ex.Message}");
            return result;
        }

        result.Document = doc;
        Validate(doc, result.Errors);
        return result;
    }

    private static void Validate(LayerManifest doc, List<string> errors)
    {
        if (doc.Schema != RequiredSchema)
            errors.Add($"schema must be '{RequiredSchema}', got '{doc.Schema}'.");

        if (doc.ClaimBoundary != RequiredClaimBoundary)
            errors.Add($"claim_boundary must be '{RequiredClaimBoundary}'.");

        if (doc.Width != RequiredWidth)
            errors.Add($"width must be {RequiredWidth}, got {doc.Width}.");

        if (doc.Height != RequiredHeight)
            errors.Add($"height must be {RequiredHeight}, got {doc.Height}.");

        if (doc.Layers.Count == 0)
        {
            errors.Add("layers must be non-empty.");
            return;
        }

        ValidateLayers(doc.Layers, errors);
        ValidatePrecedence(doc.Layers, doc.Precedence, errors);
    }

    private static void ValidateLayers(List<LayerManifestLayer> layers, List<string> errors)
    {
        var seenNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var layer in layers)
        {
            if (string.IsNullOrWhiteSpace(layer.Name))
            {
                errors.Add("A layer has an empty or missing name.");
                continue;
            }

            if (!seenNames.Add(layer.Name))
                errors.Add($"Duplicate layer name: '{layer.Name}'.");

            if (string.IsNullOrWhiteSpace(layer.FilePath))
                errors.Add($"Layer '{layer.Name}' has an empty or missing path.");

            if (layer.AllowedKinds.Count == 0)
            {
                errors.Add($"Layer '{layer.Name}' allowed_kinds must be non-empty.");
            }
            else
            {
                foreach (var kind in layer.AllowedKinds)
                {
                    if (!PrimitiveClassifier.IsKnownKind(kind))
                        errors.Add($"Layer '{layer.Name}' has unknown kind '{kind}'.");
                }
            }
        }
    }

    private static void ValidatePrecedence(
        List<LayerManifestLayer> layers, List<string> precedence, List<string> errors)
    {
        if (precedence.Count == 0)
        {
            errors.Add("precedence must be non-empty.");
            return;
        }

        var layerNames = layers
            .Where(l => !string.IsNullOrWhiteSpace(l.Name))
            .Select(l => l.Name)
            .ToHashSet(StringComparer.Ordinal);

        var seenInPrecedence = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in precedence)
        {
            if (!seenInPrecedence.Add(entry))
                errors.Add($"Duplicate entry in precedence: '{entry}'.");

            if (!layerNames.Contains(entry))
                errors.Add($"Precedence entry '{entry}' does not match any layer name.");
        }

        foreach (var name in layerNames)
        {
            if (!precedence.Contains(name))
                errors.Add($"Layer '{name}' is missing from precedence.");
        }
    }
}
