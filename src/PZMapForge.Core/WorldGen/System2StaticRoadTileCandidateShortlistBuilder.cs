using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class System2StaticRoadTileCandidateShortlistBuilder
{
    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };

    // Positive scoring terms per family
    private static readonly Dictionary<string, (string Term, int Points)[]> PositiveTerms =
        new(StringComparer.Ordinal)
        {
            ["asphalt_road_surface_candidate"] =
            [
                ("asphalt",  50), ("street", 25), ("road", 20),
                ("exterior", 10),
            ],
            ["asphalt_alley_surface_candidate"] =
            [
                ("asphalt", 50), ("alley", 30), ("street", 20),
                ("road",    20), ("exterior", 10),
            ],
            ["asphalt_service_lane_candidate"] =
            [
                ("asphalt",  50), ("service", 25), ("lane", 20),
                ("road",     15), ("exterior", 10),
            ],
            ["asphalt_parking_access_candidate"] =
            [
                ("asphalt",   40), ("parking", 35), ("driveway", 25),
                ("pavement",  15), ("exterior", 10),
            ],
            ["concrete_or_sidewalk_candidate"] =
            [
                ("sidewalk", 50), ("concrete", 40), ("curb", 25),
                ("pavement", 15), ("exterior", 10),
            ],
        };

    // Source_file bonus terms (apply to all surface families)
    private static readonly (string Term, int Points)[] FileBonus =
    [
        ("tiles", 8), ("media", 5),
    ];

    // Negative terms applied to tile_name for all surface families
    private static readonly (string Term, int Points)[] NegativeTerms =
    [
        ("wall",        -100), ("roof",      -100), ("window",    -100), ("door",       -100),
        ("furniture",    -80), ("vehicle",    -80),
        ("sign",         -60), ("vegetation", -60), ("tree",       -60), ("water",       -60),
        ("blood",        -40),
        ("shadow",       -30), ("overlay",    -30),
    ];

    public static System2StaticRoadTileCandidateShortlistResult Build(
        string localSurveyPath, int topN = 25)
    {
        var result = new System2StaticRoadTileCandidateShortlistResult();

        if (!File.Exists(localSurveyPath))
        {
            result.Errors.Add($"Local tile survey file not found: {localSurveyPath}");
            return result;
        }

        List<FamilyInput> inputFamilies;
        try
        {
            var json = File.ReadAllText(localSurveyPath, Encoding.UTF8);
            using var doc = JsonDocument.Parse(json, DocOpts);
            var root = doc.RootElement;

            if (!root.TryGetProperty("families", out var familiesElem)
                || familiesElem.ValueKind != JsonValueKind.Array)
            {
                result.Errors.Add("Local survey JSON missing 'families' array");
                return result;
            }

            inputFamilies = new List<FamilyInput>();
            foreach (var f in familiesElem.EnumerateArray())
            {
                var family = f.TryGetProperty("candidate_family", out var cfP) ? cfP.GetString() ?? "" : "";
                var role   = f.TryGetProperty("role",             out var rP)  ? rP.GetString()  ?? "" : "";
                var intents = new List<string>();
                if (f.TryGetProperty("source_intents", out var siP) && siP.ValueKind == JsonValueKind.Array)
                    foreach (var i in siP.EnumerateArray())
                        intents.Add(i.GetString() ?? "");

                var candidates = new List<(string TileName, string SourceFile)>();
                if (f.TryGetProperty("candidate_tiles", out var ctP) && ctP.ValueKind == JsonValueKind.Array)
                {
                    foreach (var c in ctP.EnumerateArray())
                    {
                        var tileName   = c.TryGetProperty("tile_name",   out var tnP) ? tnP.GetString() ?? "" : "";
                        var sourceFile = c.TryGetProperty("source_file", out var sfP) ? sfP.GetString() ?? "" : "";
                        if (!string.IsNullOrEmpty(tileName))
                            candidates.Add((tileName, sourceFile));
                    }
                }

                if (!string.IsNullOrEmpty(family))
                    inputFamilies.Add(new FamilyInput(family, role, intents, candidates));
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse local survey JSON: {ex.Message}");
            return result;
        }

        if (inputFamilies.Count == 0)
        {
            result.Errors.Add("Local survey JSON contains no families");
            return result;
        }

        var outputFamilies = new List<System2StaticRoadTileCandidateShortlistFamily>();

        foreach (var input in inputFamilies)
        {
            // road_node_metadata_candidate: metadata only unless input has candidates
            var hasPositiveTerms = PositiveTerms.ContainsKey(input.Family);
            if (!hasPositiveTerms && input.Candidates.Count == 0)
            {
                outputFamilies.Add(new System2StaticRoadTileCandidateShortlistFamily
                {
                    CandidateFamily          = input.Family,
                    Role                     = input.Role,
                    SourceIntents            = input.Intents,
                    InputCandidateCount      = 0,
                    ShortlistedCandidateCount = 0,
                    ResolutionStatus         = "UNRESOLVED_METADATA_ONLY",
                    Confidence               = "LOCAL_TEXT_MATCH_RANKED_ONLY",
                    Candidates               = [],
                    Notes                    = "road_node_metadata_candidate: metadata only; no tile surface shortlist produced.",
                });
                continue;
            }

            if (input.Candidates.Count == 0)
            {
                outputFamilies.Add(new System2StaticRoadTileCandidateShortlistFamily
                {
                    CandidateFamily          = input.Family,
                    Role                     = input.Role,
                    SourceIntents            = input.Intents,
                    InputCandidateCount      = 0,
                    ShortlistedCandidateCount = 0,
                    ResolutionStatus         = "SHORTLISTED_NO_CANDIDATES",
                    Confidence               = "LOCAL_TEXT_MATCH_RANKED_ONLY",
                    Candidates               = [],
                    Notes                    = "No candidates from local tile survey.",
                });
                continue;
            }

            var positiveRules = PositiveTerms.TryGetValue(input.Family, out var pr) ? pr : [];

            var scored = new List<(int Score, List<string> Reasons, string TileName, string SourceFile)>();
            foreach (var (tileName, sourceFile) in input.Candidates)
            {
                var score   = 0;
                var reasons = new List<string>();
                var nameLow = tileName.ToLowerInvariant();
                var fileLow = sourceFile.ToLowerInvariant();

                foreach (var (term, pts) in positiveRules)
                {
                    if (nameLow.Contains(term))
                    { score += pts; reasons.Add($"tile_name contains {term}"); }
                }

                foreach (var (term, pts) in FileBonus)
                {
                    if (fileLow.Contains(term))
                    { score += pts; reasons.Add($"source_file contains {term}"); }
                }

                foreach (var (term, pts) in NegativeTerms)
                {
                    if (nameLow.Contains(term))
                    { score += pts; reasons.Add($"tile_name contains {term}"); }
                }

                scored.Add((score, reasons, tileName, sourceFile));
            }

            var shortlisted = scored
                .OrderByDescending(s => s.Score)
                .ThenBy(s => s.TileName, StringComparer.Ordinal)
                .Take(topN)
                .Select((s, idx) => new System2StaticRoadShortlistedTileCandidate
                {
                    Rank            = idx + 1,
                    TileName        = s.TileName,
                    SourceFile      = s.SourceFile,
                    Score           = s.Score,
                    ScoreReasons    = s.Reasons,
                    SourceConfidence = "LOCAL_TEXT_MATCH_ONLY",
                    Confidence      = "LOCAL_TEXT_MATCH_RANKED_ONLY",
                })
                .ToList();

            outputFamilies.Add(new System2StaticRoadTileCandidateShortlistFamily
            {
                CandidateFamily          = input.Family,
                Role                     = input.Role,
                SourceIntents            = input.Intents,
                InputCandidateCount      = input.Candidates.Count,
                ShortlistedCandidateCount = shortlisted.Count,
                ResolutionStatus         = shortlisted.Count > 0
                    ? "SHORTLISTED_CANDIDATES_PRESENT"
                    : "SHORTLISTED_NO_CANDIDATES",
                Confidence               = "LOCAL_TEXT_MATCH_RANKED_ONLY",
                Candidates               = shortlisted,
                Notes                    = "Shortlist requires visual/runtime validation before writer use.",
            });
        }

        var totalInput       = outputFamilies.Sum(f => f.InputCandidateCount);
        var totalShortlisted = outputFamilies.Sum(f => f.ShortlistedCandidateCount);
        var withShortlist    = outputFamilies.Count(f => f.Candidates.Count > 0);
        var withoutShortlist = outputFamilies.Count - withShortlist;

        result.IsValid   = true;
        result.Shortlist = new System2StaticRoadTileCandidateShortlist
        {
            SourceLocalTileSurvey = localSurveyPath,
            TopPerFamily          = topN,
            Families              = outputFamilies,
            Totals                = new System2StaticRoadTileCandidateShortlistTotals
            {
                FamilyCount              = outputFamilies.Count,
                InputCandidateCount      = totalInput,
                ShortlistedCandidateCount = totalShortlisted,
                FamiliesWithShortlist    = withShortlist,
                FamiliesWithoutShortlist = withoutShortlist,
            },
        };
        return result;
    }

    private sealed record FamilyInput(
        string Family,
        string Role,
        List<string> Intents,
        List<(string TileName, string SourceFile)> Candidates);
}
