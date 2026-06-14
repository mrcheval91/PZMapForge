using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PZMapForge.Core.WorldGen;

public static class System2StaticRoadLocalTileSurveyBuilder
{
    private static readonly Dictionary<string, string[]> FamilySearchTerms =
        new(StringComparer.Ordinal)
        {
            ["asphalt_road_surface_candidate"]   = ["asphalt", "street", "road", "pavement"],
            ["asphalt_alley_surface_candidate"]  = ["asphalt", "alley", "road", "pavement"],
            ["asphalt_service_lane_candidate"]   = ["asphalt", "service", "road", "lane", "pavement"],
            ["asphalt_parking_access_candidate"] = ["asphalt", "parking", "driveway", "pavement"],
            ["concrete_or_sidewalk_candidate"]   = ["concrete", "sidewalk", "pavement", "curb"],
            ["road_node_metadata_candidate"]     = [], // metadata, not a surface
        };

    private static readonly string[] SearchExtensions = [".tiles", ".lua", ".txt", ".xml"];
    private const long MaxFileSizeBytes = 2L * 1024 * 1024;

    private static readonly Regex IdentifierRegex =
        new(@"[a-zA-Z][a-zA-Z0-9_]{3,}", RegexOptions.Compiled);

    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };

    public static System2StaticRoadLocalTileSurveyResult Build(string surveyPath, string pzRoot)
    {
        var result = new System2StaticRoadLocalTileSurveyResult();

        if (!File.Exists(surveyPath))
        {
            result.Errors.Add($"Survey file not found: {surveyPath}");
            return result;
        }

        var inputFamilies = new List<(string Family, List<string> Intents, string Role)>();

        try
        {
            var json = File.ReadAllText(surveyPath, Encoding.UTF8);
            using var doc  = JsonDocument.Parse(json, DocOpts);
            var root = doc.RootElement;

            if (!root.TryGetProperty("families", out var familiesElem)
                || familiesElem.ValueKind != JsonValueKind.Array)
            {
                result.Errors.Add("Survey JSON missing 'families' array");
                return result;
            }

            foreach (var f in familiesElem.EnumerateArray())
            {
                var family = f.TryGetProperty("candidate_family", out var cfP) ? cfP.GetString() ?? "" : "";
                var role   = f.TryGetProperty("role",             out var rP)  ? rP.GetString()  ?? "" : "";
                var ints   = new List<string>();
                if (f.TryGetProperty("source_intents", out var siP)
                    && siP.ValueKind == JsonValueKind.Array)
                {
                    foreach (var i in siP.EnumerateArray())
                        ints.Add(i.GetString() ?? "");
                }
                if (!string.IsNullOrEmpty(family))
                    inputFamilies.Add((family, ints, role));
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse survey JSON: {ex.Message}");
            return result;
        }

        if (inputFamilies.Count == 0)
        {
            result.Errors.Add("Survey JSON contains no families");
            return result;
        }

        var pzRootExists = Directory.Exists(pzRoot);
        var textFiles    = pzRootExists ? FindTextFiles(pzRoot) : [];

        var outputFamilies = new List<System2StaticRoadLocalTileSurveyFamily>();

        foreach (var (family, intents, role) in inputFamilies)
        {
            var searchTerms = FamilySearchTerms.TryGetValue(family, out var t) ? t : [];

            if (searchTerms.Length == 0)
            {
                outputFamilies.Add(new System2StaticRoadLocalTileSurveyFamily
                {
                    CandidateFamily  = family,
                    SourceIntents    = intents,
                    Role             = role,
                    ResolutionStatus = "UNRESOLVED_NEEDS_TILE_SURVEY",
                    Confidence       = "NONE_YET",
                    SearchTerms      = [],
                    CandidateTiles   = [],
                    Notes            = "road_node_metadata_candidate: no tile surface search terms defined; metadata only.",
                });
                continue;
            }

            if (!pzRootExists)
            {
                outputFamilies.Add(new System2StaticRoadLocalTileSurveyFamily
                {
                    CandidateFamily  = family,
                    SourceIntents    = intents,
                    Role             = role,
                    ResolutionStatus = "UNRESOLVED_NEEDS_TILE_SURVEY",
                    Confidence       = "NONE_YET",
                    SearchTerms      = [.. searchTerms],
                    CandidateTiles   = [],
                    Notes            = "PZ install root not found; no tile search performed.",
                });
                continue;
            }

            var candidates = SearchFilesForFamily(textFiles, pzRoot, searchTerms);
            outputFamilies.Add(new System2StaticRoadLocalTileSurveyFamily
            {
                CandidateFamily  = family,
                SourceIntents    = intents,
                Role             = role,
                ResolutionStatus = candidates.Count > 0
                    ? "SURVEYED_CANDIDATES_FOUND"
                    : "SURVEYED_NO_CANDIDATES_FOUND",
                Confidence       = candidates.Count > 0 ? "LOCAL_TEXT_MATCH_ONLY" : "NONE_YET",
                SearchTerms      = [.. searchTerms],
                CandidateTiles   = candidates,
                Notes            = candidates.Count > 0
                    ? "Candidates require visual/runtime validation before writer use."
                    : "No candidates found. Future task must inspect PZ tile definitions manually.",
            });
        }

        var withCandidates    = outputFamilies.Count(f => f.CandidateTiles.Count > 0);
        var withoutCandidates = outputFamilies.Count - withCandidates;
        var totalTiles        = outputFamilies.Sum(f => f.CandidateTiles.Count);

        result.IsValid = true;
        result.Survey  = new System2StaticRoadLocalTileSurvey
        {
            SourceSurvey = surveyPath,
            PzRoot       = pzRoot,
            Families     = outputFamilies,
            Totals       = new System2StaticRoadLocalTileSurveyTotals
            {
                FamilyCount               = outputFamilies.Count,
                FamiliesWithCandidates    = withCandidates,
                FamiliesWithoutCandidates = withoutCandidates,
                CandidateTileCount        = totalTiles,
            },
        };
        return result;
    }

    private static string[] FindTextFiles(string root)
    {
        try
        {
            return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Where(f => SearchExtensions.Contains(
                    Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static List<System2StaticRoadLocalTileCandidate> SearchFilesForFamily(
        string[] files, string pzRoot, string[] searchTerms)
    {
        var seen   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<System2StaticRoadLocalTileCandidate>();

        foreach (var file in files)
        {
            string content;
            try
            {
                if (new FileInfo(file).Length > MaxFileSizeBytes) continue;
                content = File.ReadAllText(file, Encoding.UTF8);
            }
            catch { continue; }

            var relPath = Path.GetRelativePath(pzRoot, file);

            foreach (var line in content.Split('\n'))
            {
                string? firstTerm = null;
                foreach (var term in searchTerms)
                {
                    if (line.Contains(term, StringComparison.OrdinalIgnoreCase))
                    { firstTerm = term; break; }
                }
                if (firstTerm is null) continue;

                foreach (Match m in IdentifierRegex.Matches(line))
                {
                    var token = m.Value;
                    if (!ContainsAnyTerm(token, searchTerms)) continue;
                    if (!seen.Add(token)) continue;

                    result.Add(new System2StaticRoadLocalTileCandidate
                    {
                        TileName    = token,
                        SourceFile  = relPath,
                        MatchReason = $"matched term: {firstTerm}",
                        Confidence  = "LOCAL_TEXT_MATCH_ONLY",
                    });
                }
            }
        }

        return result;
    }

    private static bool ContainsAnyTerm(string s, string[] terms)
    {
        foreach (var t in terms)
            if (s.Contains(t, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
