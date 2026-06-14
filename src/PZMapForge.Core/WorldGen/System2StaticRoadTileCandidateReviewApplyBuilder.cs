using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class System2StaticRoadTileCandidateReviewApplyBuilder
{
    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.Ordinal)
    {
        "NEEDS_MANUAL_REVIEW",
        "APPROVED_BY_HUMAN_REVIEW",
        "REJECTED_BY_HUMAN_REVIEW",
    };

    private sealed record BaseItem(
        int Rank,
        string TileName,
        string SourceFile,
        int Score,
        List<string> ScoreReasons);

    private sealed record BaseFamily(string Family, string Role, List<BaseItem> Items);

    public static System2StaticRoadTileCandidateReviewAppliedResult Build(
        string reviewJsonPath, string decisionsCsvPath)
    {
        var result = new System2StaticRoadTileCandidateReviewAppliedResult();

        if (!File.Exists(reviewJsonPath))
        {
            result.Errors.Add($"Review JSON not found: {reviewJsonPath}");
            return result;
        }
        if (!File.Exists(decisionsCsvPath))
        {
            result.Errors.Add($"Decisions CSV not found: {decisionsCsvPath}");
            return result;
        }

        var baseFamilies = new List<BaseFamily>();
        var itemIndex    = new HashSet<(string family, int rank, string tileName)>(
            EqualityComparer<(string, int, string)>.Default);

        try
        {
            var json = File.ReadAllText(reviewJsonPath, Encoding.UTF8);
            using var doc  = JsonDocument.Parse(json, DocOpts);
            var root = doc.RootElement;

            if (!root.TryGetProperty("families", out var familiesElem)
                || familiesElem.ValueKind != JsonValueKind.Array)
            {
                result.Errors.Add("Review JSON missing 'families' array");
                return result;
            }

            foreach (var f in familiesElem.EnumerateArray())
            {
                var family = f.TryGetProperty("candidate_family", out var cfP) ? cfP.GetString() ?? "" : "";
                var role   = f.TryGetProperty("role",             out var rP)  ? rP.GetString()  ?? "" : "";

                var items = new List<BaseItem>();

                if (f.TryGetProperty("review_items", out var riElem)
                    && riElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var c in riElem.EnumerateArray())
                    {
                        var rank       = c.TryGetProperty("rank",        out var rkP) ? rkP.GetInt32()  : 0;
                        var tileName   = c.TryGetProperty("tile_name",   out var tnP) ? tnP.GetString() ?? "" : "";
                        var sourceFile = c.TryGetProperty("source_file", out var sfP) ? sfP.GetString() ?? "" : "";
                        var score      = c.TryGetProperty("score",       out var spP) ? spP.GetInt32()  : 0;

                        var reasons = new List<string>();
                        if (c.TryGetProperty("score_reasons", out var srP)
                            && srP.ValueKind == JsonValueKind.Array)
                            foreach (var r in srP.EnumerateArray())
                                reasons.Add(r.GetString() ?? "");

                        if (!string.IsNullOrEmpty(tileName))
                        {
                            items.Add(new BaseItem(rank, tileName, sourceFile, score, reasons));
                            itemIndex.Add((family, rank, tileName));
                        }
                    }
                }

                if (!string.IsNullOrEmpty(family))
                    baseFamilies.Add(new BaseFamily(family, role, items));
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse review JSON: {ex.Message}");
            return result;
        }

        if (baseFamilies.Count == 0)
        {
            result.Errors.Add("Review JSON contains no families");
            return result;
        }

        // Parse decisions CSV
        string[] csvLines;
        try
        {
            csvLines = File.ReadAllLines(decisionsCsvPath, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to read decisions CSV: {ex.Message}");
            return result;
        }

        if (csvLines.Length == 0)
        {
            result.Errors.Add("Decisions CSV is empty");
            return result;
        }

        // Parse header to find column indices
        int colFamily = -1, colRank = -1, colTileName = -1, colStatus = -1, colNote = -1;
        var headerFields = SplitCsvLine(csvLines[0]);
        for (var i = 0; i < headerFields.Count; i++)
        {
            switch (headerFields[i].Trim())
            {
                case "candidate_family": colFamily   = i; break;
                case "rank":             colRank     = i; break;
                case "tile_name":        colTileName = i; break;
                case "review_status":    colStatus   = i; break;
                case "human_note":       colNote     = i; break;
            }
        }

        if (colFamily < 0 || colRank < 0 || colTileName < 0 || colStatus < 0)
        {
            result.Errors.Add(
                "Decisions CSV missing required columns: candidate_family, rank, tile_name, review_status");
            return result;
        }

        // Process data rows: collect last valid decision per key
        var pendingDecisions = new Dictionary<(string, int, string), (string status, string note)>();
        var unknownDecisionCount = 0;

        for (var lineIdx = 1; lineIdx < csvLines.Length; lineIdx++)
        {
            var line = csvLines[lineIdx];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var fields   = SplitCsvLine(line);
            var family   = colFamily   < fields.Count ? fields[colFamily].Trim()   : "";
            var tileName = colTileName < fields.Count ? fields[colTileName].Trim() : "";
            var rankStr  = colRank     < fields.Count ? fields[colRank].Trim()     : "0";
            var status   = colStatus   < fields.Count ? fields[colStatus].Trim()   : "";
            var note     = colNote >= 0 && colNote < fields.Count ? fields[colNote] : "";

            if (!int.TryParse(rankStr, out var rank)) rank = 0;
            var key = (family, rank, tileName);

            if (!itemIndex.Contains(key))
            {
                unknownDecisionCount++;
                result.Warnings.Add(
                    $"No matching item for CSV row: family={family} rank={rank} tile_name={tileName}");
                continue;
            }

            if (!ValidStatuses.Contains(status))
            {
                unknownDecisionCount++;
                result.Warnings.Add(
                    $"Invalid review_status '{status}' for tile_name={tileName}; keeping original status");
                continue;
            }

            if (pendingDecisions.ContainsKey(key))
                result.Warnings.Add(
                    $"Duplicate decision for family={family} rank={rank} tile_name={tileName}; last row wins");

            pendingDecisions[key] = (status, note);
        }

        var appliedDecisionCount = pendingDecisions.Count;

        // Build output families
        var outputFamilies = new List<System2StaticRoadTileCandidateReviewAppliedFamily>();
        foreach (var bf in baseFamilies)
        {
            var items = new List<System2StaticRoadTileCandidateReviewAppliedItem>();
            foreach (var bi in bf.Items)
            {
                var key          = (bf.Family, bi.Rank, bi.TileName);
                var reviewStatus = "NEEDS_MANUAL_REVIEW";
                var humanNote    = "";
                var confidence   = "LOCAL_TEXT_MATCH_RANKED_ONLY";
                var nextAction   = "Inspect in TileZed or tile sheet preview before writer use.";

                if (pendingDecisions.TryGetValue(key, out var decision))
                {
                    reviewStatus = decision.status;
                    humanNote    = decision.note;
                    confidence   = reviewStatus == "NEEDS_MANUAL_REVIEW"
                        ? "LOCAL_TEXT_MATCH_RANKED_ONLY"
                        : "HUMAN_REVIEW_ONLY_NOT_RUNTIME_PROVEN";
                    nextAction   = reviewStatus switch
                    {
                        "APPROVED_BY_HUMAN_REVIEW" =>
                            "Keep for future visual/runtime validation; not writer-ready yet.",
                        "REJECTED_BY_HUMAN_REVIEW" =>
                            "Exclude from writer use; rejected by human review.",
                        _ => "Inspect in TileZed or tile sheet preview before writer use.",
                    };
                }

                items.Add(new System2StaticRoadTileCandidateReviewAppliedItem
                {
                    Rank                  = bi.Rank,
                    TileName              = bi.TileName,
                    SourceFile            = bi.SourceFile,
                    Score                 = bi.Score,
                    ScoreReasons          = bi.ScoreReasons,
                    ReviewStatus          = reviewStatus,
                    HumanNote             = humanNote,
                    RecommendedNextAction = nextAction,
                    Confidence            = confidence,
                });
            }

            outputFamilies.Add(new System2StaticRoadTileCandidateReviewAppliedFamily
            {
                CandidateFamily = bf.Family,
                Role            = bf.Role,
                ReviewItems     = items,
            });
        }

        var totalItems   = outputFamilies.Sum(f => f.ReviewItems.Count);
        var approved     = outputFamilies.Sum(f => f.ReviewItems.Count(i => i.ReviewStatus == "APPROVED_BY_HUMAN_REVIEW"));
        var rejected     = outputFamilies.Sum(f => f.ReviewItems.Count(i => i.ReviewStatus == "REJECTED_BY_HUMAN_REVIEW"));
        var needsReview  = outputFamilies.Sum(f => f.ReviewItems.Count(i => i.ReviewStatus == "NEEDS_MANUAL_REVIEW"));

        result.IsValid = true;
        result.Applied = new System2StaticRoadTileCandidateReviewApplied
        {
            SourceReviewJson   = reviewJsonPath,
            SourceDecisionsCsv = decisionsCsvPath,
            Families           = outputFamilies,
            Totals             = new System2StaticRoadTileCandidateReviewAppliedTotals
            {
                FamilyCount            = outputFamilies.Count,
                ReviewItemCount        = totalItems,
                NeedsManualReviewCount = needsReview,
                ApprovedCount          = approved,
                RejectedCount          = rejected,
                AppliedDecisionCount   = appliedDecisionCount,
                UnknownDecisionCount   = unknownDecisionCount,
            },
        };
        return result;
    }

    public static string RenderMarkdown(System2StaticRoadTileCandidateReviewApplied applied)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# DeadMTL System 2 Tile Candidate Applied Review (MAP-22M)");
        sb.AppendLine();
        sb.AppendLine("> **Warning:** Human approval is not runtime proof and not writer readiness.");
        sb.AppendLine("> Approved candidates are candidates a human considers worth keeping for the");
        sb.AppendLine("> next validation pass. They are not validated at runtime and are not");
        sb.AppendLine("> writer-ready tile IDs.");
        sb.AppendLine();
        sb.AppendLine("## Totals");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| family_count | {applied.Totals.FamilyCount} |");
        sb.AppendLine($"| review_item_count | {applied.Totals.ReviewItemCount} |");
        sb.AppendLine($"| approved_count | {applied.Totals.ApprovedCount} |");
        sb.AppendLine($"| rejected_count | {applied.Totals.RejectedCount} |");
        sb.AppendLine($"| needs_manual_review_count | {applied.Totals.NeedsManualReviewCount} |");
        sb.AppendLine($"| applied_decision_count | {applied.Totals.AppliedDecisionCount} |");
        sb.AppendLine($"| unknown_decision_count | {applied.Totals.UnknownDecisionCount} |");
        sb.AppendLine();

        // Approved candidates section
        var approvedItems = applied.Families
            .SelectMany(f => f.ReviewItems
                .Where(i => i.ReviewStatus == "APPROVED_BY_HUMAN_REVIEW")
                .Select(i => (f.CandidateFamily, i)))
            .ToList();

        sb.AppendLine("## Approved candidates");
        sb.AppendLine();
        if (approvedItems.Count == 0)
        {
            sb.AppendLine("_No approved candidates._");
        }
        else
        {
            sb.AppendLine("| Family | Rank | Tile Name | Score | Source File | Human Note |");
            sb.AppendLine("|--------|------|-----------|-------|-------------|------------|");
            foreach (var (family, item) in approvedItems)
                sb.AppendLine($"| {family} | {item.Rank} | `{item.TileName}` | {item.Score} | {item.SourceFile} | {item.HumanNote} |");
        }
        sb.AppendLine();

        // Rejected candidates section
        var rejectedItems = applied.Families
            .SelectMany(f => f.ReviewItems
                .Where(i => i.ReviewStatus == "REJECTED_BY_HUMAN_REVIEW")
                .Select(i => (f.CandidateFamily, i)))
            .ToList();

        sb.AppendLine("## Rejected candidates");
        sb.AppendLine();
        if (rejectedItems.Count == 0)
        {
            sb.AppendLine("_No rejected candidates._");
        }
        else
        {
            sb.AppendLine("| Family | Rank | Tile Name | Score | Source File | Human Note |");
            sb.AppendLine("|--------|------|-----------|-------|-------------|------------|");
            foreach (var (family, item) in rejectedItems)
                sb.AppendLine($"| {family} | {item.Rank} | `{item.TileName}` | {item.Score} | {item.SourceFile} | {item.HumanNote} |");
        }
        sb.AppendLine();

        // Still needs manual review section
        var pendingItems = applied.Families
            .SelectMany(f => f.ReviewItems
                .Where(i => i.ReviewStatus == "NEEDS_MANUAL_REVIEW")
                .Select(i => (f.CandidateFamily, i)))
            .ToList();

        sb.AppendLine("## Still needs manual review");
        sb.AppendLine();
        if (pendingItems.Count == 0)
        {
            sb.AppendLine("_All items have been reviewed._");
        }
        else
        {
            sb.AppendLine("| Family | Rank | Tile Name | Score | Source File |");
            sb.AppendLine("|--------|------|-----------|-------|-------------|");
            foreach (var (family, item) in pendingItems)
                sb.AppendLine($"| {family} | {item.Rank} | `{item.TileName}` | {item.Score} | {item.SourceFile} |");
        }
        sb.AppendLine();

        sb.AppendLine("## Claim boundary");
        sb.AppendLine();
        sb.AppendLine("- Does NOT write lotpack files.");
        sb.AppendLine("- Does NOT write WorldGenOverride.lua.");
        sb.AppendLine("- Runtime proof is NOT claimed.");
        sb.AppendLine("- Public playable claim is NOT made.");
        sb.AppendLine("- Writer-ready claim is NOT made.");
        sb.AppendLine("- Human approval is NOT runtime validation.");
        sb.AppendLine("- All confidence values carry either LOCAL_TEXT_MATCH_RANKED_ONLY or HUMAN_REVIEW_ONLY_NOT_RUNTIME_PROVEN.");
        sb.AppendLine();
        sb.AppendLine("VERDICT: MAP22M_SYSTEM2_STATIC_ROAD_TILE_CANDIDATE_REVIEW_APPLY_COMPLETE");
        return sb.ToString();
    }

    public static string RenderCsv(System2StaticRoadTileCandidateReviewApplied applied)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "candidate_family,role,rank,tile_name,source_file,score,score_reasons," +
            "review_status,human_note,recommended_next_action,confidence");

        foreach (var family in applied.Families)
        {
            foreach (var item in family.ReviewItems)
            {
                var reasons = string.Join("|", item.ScoreReasons).Replace(",", ";");
                var action  = item.RecommendedNextAction.Replace(",", ";");
                sb.AppendLine(
                    $"{EscCsv(family.CandidateFamily)}," +
                    $"{EscCsv(family.Role)}," +
                    $"{item.Rank}," +
                    $"{EscCsv(item.TileName)}," +
                    $"{EscCsv(item.SourceFile)}," +
                    $"{item.Score}," +
                    $"{EscCsv(reasons)}," +
                    $"{EscCsv(item.ReviewStatus)}," +
                    $"{EscCsv(item.HumanNote)}," +
                    $"{EscCsv(action)}," +
                    $"{EscCsv(item.Confidence)}");
            }
        }

        return sb.ToString();
    }

    private static string EscCsv(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var fields  = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                { current.Append('"'); i++; }
                else if (c == '"')
                { inQuotes = false; }
                else
                { current.Append(c); }
            }
            else
            {
                if (c == '"')        { inQuotes = true; }
                else if (c == ',')   { fields.Add(current.ToString()); current.Clear(); }
                else                 { current.Append(c); }
            }
        }
        fields.Add(current.ToString());
        return fields;
    }
}
