using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PZMapForge.Core.WorldGen;

public static class System2StaticRoadFilteredLocalTileSurveyBuilder
{
    private static readonly Dictionary<string, string[]> FamilySearchTerms =
        new(StringComparer.Ordinal)
        {
            ["asphalt_road_surface_candidate"]   = ["asphalt", "street", "road", "pavement"],
            ["asphalt_alley_surface_candidate"]  = ["asphalt", "alley", "road", "pavement"],
            ["asphalt_service_lane_candidate"]   = ["asphalt", "service", "road", "lane", "pavement"],
            ["asphalt_parking_access_candidate"] = ["asphalt", "parking", "driveway", "pavement"],
            ["concrete_or_sidewalk_candidate"]   = ["concrete", "sidewalk", "pavement", "curb"],
            ["road_node_metadata_candidate"]     = [],
        };

    // Normalized to forward slashes for matching against normalized relative paths
    private static readonly string[] ExcludedFragments =
    [
        "media/profanity",
        "media/lua",
        "media/radio",
        "media/sound",
        "media/music",
        "media/maps",
        "media/scripts/items",
        "media/scripts/vehicles",
        "media/scripts/recipes",
        "media/scripts/clothing",
    ];

    // For JSON output: canonical display forms
    private static readonly string[] ExcludedFragmentsDisplay =
    [
        @"media\profanity",
        @"media\lua",
        @"media\radio",
        @"media\sound",
        @"media\music",
        @"media\maps",
        @"media\scripts\items",
        @"media\scripts\vehicles",
        @"media\scripts\recipes",
        @"media\scripts\clothing",
    ];

    private static readonly string[] PreferredFragments =
    [
        @"media\tiles",
        @"media\tiledefinitions",
        @"media\newtiledefinitions",
    ];

    private static readonly string[] AllowedExtensions =
        [".tiles", ".tiles2", ".txt", ".xml", ".lua"];

    private const long MaxFileSizeBytes = 2L * 1024 * 1024;

    private static readonly Regex IdentifierRegex =
        new(@"[a-zA-Z][a-zA-Z0-9_]{3,}", RegexOptions.Compiled);

    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };

    public static System2StaticRoadFilteredLocalTileSurveyResult Build(string surveyPath, string pzRoot)
    {
        var result = new System2StaticRoadFilteredLocalTileSurveyResult();

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
                    foreach (var i in siP.EnumerateArray())
                        ints.Add(i.GetString() ?? "");
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

        var pzRootExists    = Directory.Exists(pzRoot);
        var scannedCount    = 0;
        var excludedCount   = 0;
        string[] scanFiles  = [];

        if (pzRootExists)
            (scanFiles, scannedCount, excludedCount) = PartitionFiles(pzRoot);

        var outputFamilies = new List<System2StaticRoadFilteredLocalTileSurveyFamily>();

        foreach (var (family, intents, role) in inputFamilies)
        {
            var searchTerms = FamilySearchTerms.TryGetValue(family, out var t) ? t : [];

            if (searchTerms.Length == 0)
            {
                outputFamilies.Add(new System2StaticRoadFilteredLocalTileSurveyFamily
                {
                    CandidateFamily  = family,
                    Role             = role,
                    SourceIntents    = intents,
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
                outputFamilies.Add(new System2StaticRoadFilteredLocalTileSurveyFamily
                {
                    CandidateFamily  = family,
                    Role             = role,
                    SourceIntents    = intents,
                    ResolutionStatus = "UNRESOLVED_NEEDS_TILE_SURVEY",
                    Confidence       = "NONE_YET",
                    SearchTerms      = [.. searchTerms],
                    CandidateTiles   = [],
                    Notes            = "PZ install root not found; no tile search performed.",
                });
                continue;
            }

            var candidates = SearchFilesForFamily(scanFiles, pzRoot, searchTerms);
            outputFamilies.Add(new System2StaticRoadFilteredLocalTileSurveyFamily
            {
                CandidateFamily  = family,
                Role             = role,
                SourceIntents    = intents,
                ResolutionStatus = candidates.Count > 0
                    ? "FILTERED_SURVEY_CANDIDATES_FOUND"
                    : "FILTERED_SURVEY_NO_CANDIDATES_FOUND",
                Confidence       = candidates.Count > 0 ? "LOCAL_FILTERED_TEXT_MATCH_ONLY" : "NONE_YET",
                SearchTerms      = [.. searchTerms],
                CandidateTiles   = candidates,
                ExcludedCandidateCount = 0,
                Notes            = candidates.Count > 0
                    ? "Filtered candidates require visual/runtime validation before writer use."
                    : "No filtered candidates found. Inspect PZ tile definitions manually.",
            });
        }

        var withCandidates    = outputFamilies.Count(f => f.CandidateTiles.Count > 0);
        var withoutCandidates = outputFamilies.Count - withCandidates;
        var totalTiles        = outputFamilies.Sum(f => f.CandidateTiles.Count);

        result.IsValid = true;
        result.Survey  = new System2StaticRoadFilteredLocalTileSurvey
        {
            SourceSurvey = surveyPath,
            PzRoot       = pzRoot,
            SourceFilter = new System2FilteredSurveySourceFilter
            {
                AllowedExtensions      = [.. AllowedExtensions],
                ExcludedPathFragments  = [.. ExcludedFragmentsDisplay],
                PreferredPathFragments = [.. PreferredFragments],
            },
            Families = outputFamilies,
            Totals   = new System2StaticRoadFilteredLocalTileSurveyTotals
            {
                FamilyCount               = outputFamilies.Count,
                CandidateTileCount        = totalTiles,
                ExcludedSourceFileCount   = excludedCount,
                ScannedSourceFileCount    = scannedCount,
                FamiliesWithCandidates    = withCandidates,
                FamiliesWithoutCandidates = withoutCandidates,
            },
        };
        return result;
    }

    private static (string[] ScanFiles, int ScannedCount, int ExcludedCount) PartitionFiles(string root)
    {
        string[] allFiles;
        try
        {
            allFiles = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Where(f => AllowedExtensions.Contains(
                    Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                .ToArray();
        }
        catch
        {
            return ([], 0, 0);
        }

        var scan     = new List<string>();
        var excluded = 0;

        foreach (var f in allFiles)
        {
            var rel        = Path.GetRelativePath(root, f);
            var normalized = rel.Replace('\\', '/');

            if (ExcludedFragments.Any(frag =>
                normalized.Contains(frag, StringComparison.OrdinalIgnoreCase)))
            {
                excluded++;
            }
            else
            {
                scan.Add(f);
            }
        }

        return ([.. scan], scan.Count, excluded);
    }

    private static List<System2StaticRoadFilteredLocalTileCandidate> SearchFilesForFamily(
        string[] files, string pzRoot, string[] searchTerms)
    {
        var seen   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<System2StaticRoadFilteredLocalTileCandidate>();

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

                    result.Add(new System2StaticRoadFilteredLocalTileCandidate
                    {
                        TileName    = token,
                        SourceFile  = relPath,
                        MatchReason = $"matched term: {firstTerm}",
                        Confidence  = "LOCAL_FILTERED_TEXT_MATCH_ONLY",
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

    public static string RenderMarkdown(System2StaticRoadFilteredLocalTileSurvey survey)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-22O: System 2 Static Road Local Tile Survey (Filtered)");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** This is a source-filtered local text scan only. " +
                      "Confidence: LOCAL_FILTERED_TEXT_MATCH_ONLY. " +
                      "No runtime validation is claimed. " +
                      "Human and visual inspection required before writer use.");
        sb.AppendLine();
        sb.AppendLine("## Totals");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| family_count | {survey.Totals.FamilyCount} |");
        sb.AppendLine($"| candidate_tile_count | {survey.Totals.CandidateTileCount} |");
        sb.AppendLine($"| scanned_source_file_count | {survey.Totals.ScannedSourceFileCount} |");
        sb.AppendLine($"| excluded_source_file_count | {survey.Totals.ExcludedSourceFileCount} |");
        sb.AppendLine($"| families_with_candidates | {survey.Totals.FamiliesWithCandidates} |");
        sb.AppendLine($"| families_without_candidates | {survey.Totals.FamiliesWithoutCandidates} |");
        sb.AppendLine();
        sb.AppendLine("## Source filter");
        sb.AppendLine();
        sb.AppendLine("Excluded path fragments:");
        foreach (var frag in survey.SourceFilter.ExcludedPathFragments)
            sb.AppendLine($"- `{frag}`");
        sb.AppendLine();
        foreach (var family in survey.Families)
        {
            sb.AppendLine($"## {family.CandidateFamily}");
            sb.AppendLine();
            sb.AppendLine($"- role: {family.Role}");
            sb.AppendLine($"- resolution_status: {family.ResolutionStatus}");
            sb.AppendLine($"- confidence: {family.Confidence}");
            sb.AppendLine($"- candidates: {family.CandidateTiles.Count}");
            sb.AppendLine($"- notes: {family.Notes}");
            sb.AppendLine();
            if (family.CandidateTiles.Count > 0)
            {
                sb.AppendLine("| tile_name | source_file | match_reason |");
                sb.AppendLine("|---|---|---|");
                foreach (var tile in family.CandidateTiles)
                    sb.AppendLine($"| {tile.TileName} | {tile.SourceFile} | {tile.MatchReason} |");
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
        sb.AppendLine("MAP22O_SYSTEM2_STATIC_ROAD_LOCAL_TILE_SURVEY_FILTERED_COMPLETE");
        return sb.ToString();
    }

    public static string RenderCsv(System2StaticRoadFilteredLocalTileSurvey survey)
    {
        var sb = new StringBuilder();
        sb.AppendLine("candidate_family,role,tile_name,source_file,match_reason,confidence,resolution_status");
        foreach (var family in survey.Families)
        {
            foreach (var tile in family.CandidateTiles)
            {
                sb.AppendLine(
                    $"{CsvEscape(family.CandidateFamily)}," +
                    $"{CsvEscape(family.Role)}," +
                    $"{CsvEscape(tile.TileName)}," +
                    $"{CsvEscape(tile.SourceFile)}," +
                    $"{CsvEscape(tile.MatchReason)}," +
                    $"{CsvEscape(tile.Confidence)}," +
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
}
