using System.Text.Json;

namespace PZMapForge.Core.ParsedCell;

public static class ParsedCellLoader
{
    private const string RequiredSchema        = "pzmapforge.parsed-cell.v0.1";
    private const string RequiredClaimBoundary = "planning_artifact_only_not_pz_load_tested";
    private const int    RequiredWidth         = 300;
    private const int    RequiredHeight        = 300;

    private static readonly string[] RequiredKinds =
    [
        "grass", "road", "sidewalk", "row_house", "depanneur",
        "garage", "industrial_yard", "landmark", "spawn"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas         = true,
        ReadCommentHandling         = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = false,
    };

    public static ParsedCellLoadResult Load(string path)
    {
        var result = new ParsedCellLoadResult();

        if (!File.Exists(path))
        {
            result.Errors.Add($"Parsed-cell file not found: {path}");
            return result;
        }

        ParsedCellDocument doc;
        try
        {
            var json = File.ReadAllText(path);
            doc = JsonSerializer.Deserialize<ParsedCellDocument>(json, JsonOptions)
                  ?? throw new JsonException("Deserialized to null.");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"JSON parse error: {ex.Message}");
            return result;
        }

        result.Document = doc;
        Validate(doc, result.Errors);

        if (result.IsValid)
            result.Grid = new SemanticGrid(doc.Width, doc.Height, doc.Rows);

        return result;
    }

    private static void Validate(ParsedCellDocument doc, List<string> errors)
    {
        if (doc.Schema != RequiredSchema)
            errors.Add($"schema must be '{RequiredSchema}', got '{doc.Schema}'.");

        if (doc.ClaimBoundary != RequiredClaimBoundary)
            errors.Add($"claim_boundary must be '{RequiredClaimBoundary}', got '{doc.ClaimBoundary}'.");

        if (doc.Width != RequiredWidth)
            errors.Add($"width must be {RequiredWidth}, got {doc.Width}.");

        if (doc.Height != RequiredHeight)
            errors.Add($"height must be {RequiredHeight}, got {doc.Height}.");

        if (doc.Rows == null || doc.Rows.Count == 0)
        {
            errors.Add("rows is empty or missing.");
            return;
        }

        if (doc.Rows.Count != doc.Height)
            errors.Add($"rows.Count ({doc.Rows.Count}) must equal height ({doc.Height}).");

        var badRowCount = 0;
        for (var i = 0; i < doc.Rows.Count && badRowCount <= 3; i++)
        {
            if (doc.Rows[i].Length != doc.Width)
            {
                errors.Add($"Row {i} length is {doc.Rows[i].Length}, expected {doc.Width}.");
                badRowCount++;
            }
        }

        if (badRowCount == 0)
        {
            var countsSum = doc.Counts?.Sum(c => c.Pixels) ?? 0;
            var expected  = doc.Width * doc.Height;
            if (countsSum != expected)
                errors.Add($"counts pixel sum is {countsSum}, expected {expected}.");

            var presentKinds = new HashSet<string>(
                doc.Counts?.Select(c => c.Kind) ?? [],
                StringComparer.Ordinal);

            foreach (var required in RequiredKinds)
                if (!presentKinds.Contains(required))
                    errors.Add($"Missing required kind in counts: '{required}'.");
        }
    }
}
