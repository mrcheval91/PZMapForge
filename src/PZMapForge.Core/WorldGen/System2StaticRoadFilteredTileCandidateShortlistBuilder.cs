using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class System2StaticRoadFilteredTileCandidateShortlistBuilder
{
    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };

    // Primary positive terms per family (each hit: +20)
    private static readonly Dictionary<string, string[]> FamilyTerms =
        new(StringComparer.Ordinal)
        {
            ["asphalt_road_surface_candidate"]   = ["asphalt", "street", "road", "pavement"],
            ["asphalt_alley_surface_candidate"]  = ["asphalt", "alley", "road", "pavement"],
            ["asphalt_service_lane_candidate"]   = ["asphalt", "service", "road", "lane", "pavement"],
            ["asphalt_parking_access_candidate"] = ["asphalt", "parking", "driveway", "pavement"],
            ["concrete_or_sidewalk_candidate"]   = ["concrete", "sidewalk", "pavement", "curb"],
        };

    // tile_name surface bonus terms (+10 each)
    private static readonly string[] SurfaceBonusTerms =
        ["floor", "exterior", "pavement", "asphalt", "concrete"];

    // source_file bonus terms
    private static readonly (string Term, int Points)[] FileBonusTerms =
    [
        ("media/tiles",         12),
        (@"media\tiles",        12),
        ("tiledefinitions",     10),
        ("newtiledefinitions",  10),
        ("exterior",             5),
    ];

    // Negative tile_name terms
    private static readonly (string Term, int Points)[] NegativeTerms =
    [
        ("wall",       -100), ("roof",       -100), ("window",     -100), ("door",        -100),
        ("furniture",   -80), ("vehicle",     -80), ("car",         -80), ("sign",         -80),
        ("vegetation",  -60), ("tree",        -60), ("bush",        -60), ("water",        -60),
        ("blood",       -30), ("shadow",      -30), ("overlay",     -30),
    ];

    public static System2StaticRoadFilteredTileCandidateShortlistResult Build(
        string filteredSurveyPath, int topN = 25)
    {
        var result = new System2StaticRoadFilteredTileCandidateShortlistResult();

        if (!File.Exists(filteredSurveyPath))
        {
            result.Errors.Add($"Filtered survey file not found: {filteredSurveyPath}");
            return result;
        }

        List<FamilyInput> inputFamilies;
        try
        {
            var json = File.ReadAllText(filteredSurveyPath, Encoding.UTF8);
            using var doc = JsonDocument.Parse(json, DocOpts);
            var root = doc.RootElement;

            if (!root.TryGetProperty("families", out var familiesElem)
                || familiesElem.ValueKind != JsonValueKind.Array)
            {
                result.Errors.Add("Filtered survey JSON missing 'families' array");
                return result;
            }

            inputFamilies = new List<FamilyInput>();
            foreach (var f in familiesElem.EnumerateArray())
            {
                var family = f.TryGetProperty("candidate_family", out var cfP) ? cfP.GetString() ?? "" : "";
                var role   = f.TryGetProperty("role",             out var rP)  ? rP.GetString()  ?? "" : "";

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
                    inputFamilies.Add(new FamilyInput(family, role, candidates));
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse filtered survey JSON: {ex.Message}");
            return result;
        }

        if (inputFamilies.Count == 0)
        {
            result.Errors.Add("Filtered survey JSON contains no families");
            return result;
        }

        var outputFamilies = new List<System2StaticRoadFilteredTileCandidateShortlistFamily>();

        foreach (var input in inputFamilies)
        {
            var hasTerms = FamilyTerms.ContainsKey(input.Family);

            if (!hasTerms)
            {
                outputFamilies.Add(new System2StaticRoadFilteredTileCandidateShortlistFamily
                {
                    CandidateFamily           = input.Family,
                    Role                      = input.Role,
                    InputCandidateCount       = 0,
                    ShortlistedCandidateCount = 0,
                    ResolutionStatus          = "UNRESOLVED_METADATA_ONLY",
                    Confidence                = "LOCAL_FILTERED_TEXT_MATCH_RANKED_ONLY",
                    Candidates                = [],
                    Notes                     = "road_node_metadata_candidate: metadata only; no tile surface shortlist produced.",
                });
                continue;
            }

            if (input.Candidates.Count == 0)
            {
                outputFamilies.Add(new System2StaticRoadFilteredTileCandidateShortlistFamily
                {
                    CandidateFamily           = input.Family,
                    Role                      = input.Role,
                    InputCandidateCount       = 0,
                    ShortlistedCandidateCount = 0,
                    ResolutionStatus          = "FILTERED_SHORTLISTED_NO_CANDIDATES",
                    Confidence                = "LOCAL_FILTERED_TEXT_MATCH_RANKED_ONLY",
                    Candidates                = [],
                    Notes                     = "No candidates from filtered local tile survey.",
                });
                continue;
            }

            var familyTerms = FamilyTerms[input.Family];

            var scored = new List<(int Score, List<string> Reasons, string TileName, string SourceFile)>();
            foreach (var (tileName, sourceFile) in input.Candidates)
            {
                var score   = 0;
                var reasons = new List<string>();
                var nameLow = tileName.ToLowerInvariant();
                var fileLow = sourceFile.Replace('\\', '/').ToLowerInvariant();

                // Primary family terms: +20 each
                foreach (var term in familyTerms)
                {
                    if (nameLow.Contains(term))
                    { score += 20; reasons.Add($"tile_name contains {term}"); }
                }

                // Surface bonus terms in tile_name: +10 each
                foreach (var term in SurfaceBonusTerms)
                {
                    if (nameLow.Contains(term))
                    { score += 10; reasons.Add($"tile_name surface bonus: {term}"); }
                }

                // Source file bonuses (only add the first match per bonus rule to avoid double-count)
                foreach (var (term, pts) in FileBonusTerms)
                {
                    var normalizedTerm = term.Replace('\\', '/');
                    if (fileLow.Contains(normalizedTerm))
                    { score += pts; reasons.Add($"source_file contains {term}"); break; }
                }

                // tiledefinitions bonus (separate from tiles bonus)
                if (fileLow.Contains("tiledefinitions") && !fileLow.Contains("media/tiles"))
                { score += 10; reasons.Add("source_file contains tiledefinitions"); }

                // exterior in source_file
                if (fileLow.Contains("exterior"))
                { score += 5; reasons.Add("source_file contains exterior"); }

                // Negative tile_name terms
                foreach (var (term, pts) in NegativeTerms)
                {
                    if (nameLow.Contains(term))
                    { score += pts; reasons.Add($"tile_name contains {term} (negative)"); }
                }

                scored.Add((score, reasons, tileName, sourceFile));
            }

            var shortlisted = scored
                .OrderByDescending(s => s.Score)
                .ThenBy(s => s.TileName, StringComparer.Ordinal)
                .ThenBy(s => s.SourceFile, StringComparer.Ordinal)
                .Take(topN)
                .Select((s, idx) => new System2StaticRoadFilteredShortlistedTileCandidate
                {
                    Rank             = idx + 1,
                    TileName         = s.TileName,
                    SourceFile       = s.SourceFile,
                    Score            = s.Score,
                    ScoreReasons     = s.Reasons,
                    SourceConfidence = "LOCAL_FILTERED_TEXT_MATCH_ONLY",
                    Confidence       = "LOCAL_FILTERED_TEXT_MATCH_RANKED_ONLY",
                })
                .ToList();

            outputFamilies.Add(new System2StaticRoadFilteredTileCandidateShortlistFamily
            {
                CandidateFamily           = input.Family,
                Role                      = input.Role,
                InputCandidateCount       = input.Candidates.Count,
                ShortlistedCandidateCount = shortlisted.Count,
                ResolutionStatus          = shortlisted.Count > 0
                    ? "FILTERED_SHORTLISTED_CANDIDATES_PRESENT"
                    : "FILTERED_SHORTLISTED_NO_CANDIDATES",
                Confidence                = "LOCAL_FILTERED_TEXT_MATCH_RANKED_ONLY",
                Candidates                = shortlisted,
                Notes                     = "Filtered shortlist requires visual/runtime validation before writer use.",
            });
        }

        var totalInput       = outputFamilies.Sum(f => f.InputCandidateCount);
        var totalShortlisted = outputFamilies.Sum(f => f.ShortlistedCandidateCount);
        var withShortlist    = outputFamilies.Count(f => f.Candidates.Count > 0);
        var withoutShortlist = outputFamilies.Count - withShortlist;

        result.IsValid   = true;
        result.Shortlist = new System2StaticRoadFilteredTileCandidateShortlist
        {
            SourceFilteredSurvey = filteredSurveyPath,
            TopPerFamily         = topN,
            Families             = outputFamilies,
            Totals               = new System2StaticRoadFilteredTileCandidateShortlistTotals
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

    public static string RenderMarkdown(System2StaticRoadFilteredTileCandidateShortlist shortlist)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-22P: System 2 Static Road Filtered Tile Candidate Shortlist");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** Filtered shortlist is not runtime proof and not writer readiness. " +
                      "Confidence: LOCAL_FILTERED_TEXT_MATCH_RANKED_ONLY. " +
                      "All candidates require visual/runtime validation before writer use.");
        sb.AppendLine();
        sb.AppendLine("## Totals");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| family_count | {shortlist.Totals.FamilyCount} |");
        sb.AppendLine($"| input_candidate_count | {shortlist.Totals.InputCandidateCount} |");
        sb.AppendLine($"| shortlisted_candidate_count | {shortlist.Totals.ShortlistedCandidateCount} |");
        sb.AppendLine($"| families_with_shortlist | {shortlist.Totals.FamiliesWithShortlist} |");
        sb.AppendLine($"| families_without_shortlist | {shortlist.Totals.FamiliesWithoutShortlist} |");
        sb.AppendLine($"| top_per_family | {shortlist.TopPerFamily} |");
        sb.AppendLine();
        foreach (var family in shortlist.Families)
        {
            sb.AppendLine($"## {family.CandidateFamily}");
            sb.AppendLine();
            sb.AppendLine($"- role: {family.Role}");
            sb.AppendLine($"- resolution_status: {family.ResolutionStatus}");
            sb.AppendLine($"- confidence: {family.Confidence}");
            sb.AppendLine($"- input_candidates: {family.InputCandidateCount}");
            sb.AppendLine($"- shortlisted: {family.ShortlistedCandidateCount}");
            sb.AppendLine($"- notes: {family.Notes}");
            sb.AppendLine();
            if (family.Candidates.Count > 0)
            {
                sb.AppendLine("| rank | tile_name | score | source_file | score_reasons |");
                sb.AppendLine("|---|---|---|---|---|");
                foreach (var c in family.Candidates)
                    sb.AppendLine(
                        $"| {c.Rank} | {c.TileName} | {c.Score} | {c.SourceFile} | " +
                        $"{string.Join("; ", c.ScoreReasons)} |");
                sb.AppendLine();
            }
        }
        sb.AppendLine("## Claim boundary");
        sb.AppendLine();
        sb.AppendLine("- writes_lotpack: false");
        sb.AppendLine("- writes_worldgen_lua: false");
        sb.AppendLine("- runtime_proven: false");
        sb.AppendLine("- public_playable_claim: false");
        sb.AppendLine("- writer_ready_claim: false");
        sb.AppendLine();
        sb.AppendLine("## VERDICT");
        sb.AppendLine();
        sb.AppendLine("MAP22P_SYSTEM2_STATIC_ROAD_FILTERED_TILE_CANDIDATE_SHORTLIST_COMPLETE");
        return sb.ToString();
    }

    public static string RenderCsv(System2StaticRoadFilteredTileCandidateShortlist shortlist)
    {
        var sb = new StringBuilder();
        sb.AppendLine("candidate_family,role,rank,tile_name,source_file,score,score_reasons,source_confidence,confidence,resolution_status");
        foreach (var family in shortlist.Families)
        {
            foreach (var c in family.Candidates)
            {
                sb.AppendLine(
                    $"{CsvEscape(family.CandidateFamily)}," +
                    $"{CsvEscape(family.Role)}," +
                    $"{c.Rank}," +
                    $"{CsvEscape(c.TileName)}," +
                    $"{CsvEscape(c.SourceFile)}," +
                    $"{c.Score}," +
                    $"{CsvEscape(string.Join("; ", c.ScoreReasons))}," +
                    $"{CsvEscape(c.SourceConfidence)}," +
                    $"{CsvEscape(c.Confidence)}," +
                    $"{CsvEscape(family.ResolutionStatus)}");
            }
        }
        return sb.ToString();
    }

    private static string CsvEscape(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }

    private sealed record FamilyInput(
        string Family,
        string Role,
        List<(string TileName, string SourceFile)> Candidates);
}
