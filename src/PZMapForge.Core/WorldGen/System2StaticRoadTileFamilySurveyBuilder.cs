using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class System2StaticRoadTileFamilySurveyBuilder
{
    private const string UnresolvedStatus = "UNRESOLVED_NEEDS_TILE_SURVEY";
    private const string SurveyNote =
        "Future task must inspect local PZ tile definitions / TileZed tilesets before selecting tile IDs.";

    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };

    public static System2StaticRoadTileFamilySurveyResult Build(string tileFamilyPlanPath)
    {
        var result = new System2StaticRoadTileFamilySurveyResult();

        if (!File.Exists(tileFamilyPlanPath))
        {
            result.Errors.Add($"Tile family plan file not found: {tileFamilyPlanPath}");
            return result;
        }

        // familyName -> (role, ordered unique intents)
        var familyData = new Dictionary<string, (string Role, List<string> Intents, HashSet<string> Seen)>(
            StringComparer.Ordinal);

        try
        {
            var json = File.ReadAllText(tileFamilyPlanPath, Encoding.UTF8);
            using var doc  = JsonDocument.Parse(json, DocOpts);
            var root = doc.RootElement;

            if (!root.TryGetProperty("records", out var recordsElem)
                || recordsElem.ValueKind != JsonValueKind.Array)
            {
                result.Errors.Add("Tile family plan JSON missing 'records' array");
                return result;
            }

            foreach (var r in recordsElem.EnumerateArray())
            {
                var family = r.TryGetProperty("candidate_family", out var cfP) ? cfP.GetString() ?? "" : "";
                var role   = r.TryGetProperty("role",             out var rP)  ? rP.GetString()  ?? "" : "";
                var intent = r.TryGetProperty("intent",           out var iP)  ? iP.GetString()  ?? "" : "";

                if (string.IsNullOrEmpty(family)) continue;

                if (!familyData.TryGetValue(family, out var entry))
                {
                    entry = (role, new List<string>(), new HashSet<string>(StringComparer.Ordinal));
                    familyData[family] = entry;
                }

                if (!string.IsNullOrEmpty(intent) && entry.Seen.Add(intent))
                    entry.Intents.Add(intent);
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse tile family plan JSON: {ex.Message}");
            return result;
        }

        if (familyData.Count == 0)
        {
            result.Errors.Add("Tile family plan contains no records with candidate_family");
            return result;
        }

        var families = familyData
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => new System2StaticRoadTileFamilySurveyFamily
            {
                CandidateFamily  = kv.Key,
                SourceIntents    = kv.Value.Intents,
                Role             = kv.Value.Role,
                ResolutionStatus = UnresolvedStatus,
                Confidence       = "NONE_YET",
                CandidateTiles   = new List<string>(),
                Notes            = SurveyNote,
            })
            .ToList();

        var unresolvedCount = families.Count(f => f.ResolutionStatus == UnresolvedStatus);
        var resolvedCount   = families.Count - unresolvedCount;
        var tileCount       = families.Sum(f => f.CandidateTiles.Count);

        result.IsValid = true;
        result.Survey  = new System2StaticRoadTileFamilySurvey
        {
            SourceTileFamilyPlan = tileFamilyPlanPath,
            Families             = families,
            Totals               = new System2StaticRoadTileFamilySurveyTotals
            {
                FamilyCount           = families.Count,
                ResolvedFamilyCount   = resolvedCount,
                UnresolvedFamilyCount = unresolvedCount,
                CandidateTileCount    = tileCount,
            },
        };
        return result;
    }
}
