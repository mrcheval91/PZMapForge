using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class System2StaticRoadTileCandidateReviewBuilder
{
    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };

    public static System2StaticRoadTileCandidateReviewResult Build(string shortlistPath)
    {
        var result = new System2StaticRoadTileCandidateReviewResult();

        if (!File.Exists(shortlistPath))
        {
            result.Errors.Add($"Shortlist file not found: {shortlistPath}");
            return result;
        }

        var outputFamilies = new List<System2StaticRoadTileCandidateReviewFamily>();

        try
        {
            var json = File.ReadAllText(shortlistPath, Encoding.UTF8);
            using var doc = JsonDocument.Parse(json, DocOpts);
            var root = doc.RootElement;

            if (!root.TryGetProperty("families", out var familiesElem)
                || familiesElem.ValueKind != JsonValueKind.Array)
            {
                result.Errors.Add("Shortlist JSON missing 'families' array");
                return result;
            }

            foreach (var f in familiesElem.EnumerateArray())
            {
                var family      = f.TryGetProperty("candidate_family",          out var cfP) ? cfP.GetString() ?? "" : "";
                var role        = f.TryGetProperty("role",                       out var rP)  ? rP.GetString()  ?? "" : "";
                var inputCount  = f.TryGetProperty("input_candidate_count",     out var icP) ? icP.GetInt32()  : 0;
                var shortCount  = f.TryGetProperty("shortlisted_candidate_count", out var scP) ? scP.GetInt32() : 0;

                var items = new List<System2StaticRoadTileCandidateReviewItem>();

                if (f.TryGetProperty("candidates", out var candidatesElem)
                    && candidatesElem.ValueKind == JsonValueKind.Array)
                {
                    foreach (var c in candidatesElem.EnumerateArray())
                    {
                        var rank       = c.TryGetProperty("rank",        out var rkP) ? rkP.GetInt32()    : 0;
                        var tileName   = c.TryGetProperty("tile_name",   out var tnP) ? tnP.GetString()   ?? "" : "";
                        var sourceFile = c.TryGetProperty("source_file", out var sfP) ? sfP.GetString()   ?? "" : "";
                        var score      = c.TryGetProperty("score",       out var spP) ? spP.GetInt32()    : 0;

                        var reasons = new List<string>();
                        if (c.TryGetProperty("score_reasons", out var srP)
                            && srP.ValueKind == JsonValueKind.Array)
                            foreach (var r in srP.EnumerateArray())
                                reasons.Add(r.GetString() ?? "");

                        if (!string.IsNullOrEmpty(tileName))
                            items.Add(new System2StaticRoadTileCandidateReviewItem
                            {
                                Rank                  = rank,
                                TileName              = tileName,
                                SourceFile            = sourceFile,
                                Score                 = score,
                                ScoreReasons          = reasons,
                                ReviewStatus          = "NEEDS_MANUAL_REVIEW",
                                RecommendedNextAction = "Inspect in TileZed or tile sheet preview before writer use.",
                                Confidence            = "LOCAL_TEXT_MATCH_RANKED_ONLY",
                            });
                    }
                }

                if (!string.IsNullOrEmpty(family))
                    outputFamilies.Add(new System2StaticRoadTileCandidateReviewFamily
                    {
                        CandidateFamily          = family,
                        Role                     = role,
                        InputCandidateCount      = inputCount,
                        ShortlistedCandidateCount = shortCount,
                        ReviewItems              = items,
                    });
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse shortlist JSON: {ex.Message}");
            return result;
        }

        if (outputFamilies.Count == 0)
        {
            result.Errors.Add("Shortlist JSON contains no families");
            return result;
        }

        var totalItems   = outputFamilies.Sum(f => f.ReviewItems.Count);
        var needsReview  = outputFamilies.Sum(f => f.ReviewItems.Count(i => i.ReviewStatus == "NEEDS_MANUAL_REVIEW"));
        var approved     = outputFamilies.Sum(f => f.ReviewItems.Count(i => i.ReviewStatus == "APPROVED_BY_HUMAN_REVIEW"));
        var rejected     = outputFamilies.Sum(f => f.ReviewItems.Count(i => i.ReviewStatus == "REJECTED_BY_HUMAN_REVIEW"));

        result.IsValid = true;
        result.Review  = new System2StaticRoadTileCandidateReview
        {
            SourceShortlist = shortlistPath,
            Families        = outputFamilies,
            Totals          = new System2StaticRoadTileCandidateReviewTotals
            {
                FamilyCount            = outputFamilies.Count,
                ReviewItemCount        = totalItems,
                NeedsManualReviewCount = needsReview,
                ApprovedCount          = approved,
                RejectedCount          = rejected,
            },
        };
        return result;
    }

    public static string RenderMarkdown(System2StaticRoadTileCandidateReview review)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# DeadMTL System 2 Tile Candidate Review Packet (MAP-22L)");
        sb.AppendLine();
        sb.AppendLine("> **Warning:** These are ranked text-match candidates only.");
        sb.AppendLine("> They are not runtime-proven and are not writer-ready.");
        sb.AppendLine("> All items require manual inspection in TileZed or a tile sheet preview");
        sb.AppendLine("> before any candidate can be used in a writer.");
        sb.AppendLine();
        sb.AppendLine("## Totals");
        sb.AppendLine();
        sb.AppendLine($"| Field | Value |");
        sb.AppendLine($"|---|---|");
        sb.AppendLine($"| family_count | {review.Totals.FamilyCount} |");
        sb.AppendLine($"| review_item_count | {review.Totals.ReviewItemCount} |");
        sb.AppendLine($"| needs_manual_review_count | {review.Totals.NeedsManualReviewCount} |");
        sb.AppendLine($"| approved_count | {review.Totals.ApprovedCount} |");
        sb.AppendLine($"| rejected_count | {review.Totals.RejectedCount} |");
        sb.AppendLine();

        foreach (var family in review.Families)
        {
            sb.AppendLine($"## {family.CandidateFamily}");
            sb.AppendLine();
            sb.AppendLine($"Role: {family.Role}");
            sb.AppendLine($"Input candidates: {family.InputCandidateCount} | Shortlisted: {family.ShortlistedCandidateCount}");
            sb.AppendLine();

            if (family.ReviewItems.Count == 0)
            {
                sb.AppendLine("_No candidates for this family._");
                sb.AppendLine();
                continue;
            }

            sb.AppendLine("| Rank | Tile Name | Score | Source File | Reasons | Review Status |");
            sb.AppendLine("|------|-----------|-------|-------------|---------|---------------|");
            foreach (var item in family.ReviewItems)
            {
                var reasons = string.Join("; ", item.ScoreReasons);
                sb.AppendLine($"| {item.Rank} | `{item.TileName}` | {item.Score} | {item.SourceFile} | {reasons} | {item.ReviewStatus} |");
            }
            sb.AppendLine();
        }

        sb.AppendLine("## Claim boundary");
        sb.AppendLine();
        sb.AppendLine("- Does NOT write lotpack files.");
        sb.AppendLine("- Does NOT write WorldGenOverride.lua.");
        sb.AppendLine("- Runtime proof is NOT claimed.");
        sb.AppendLine("- Public playable claim is NOT made.");
        sb.AppendLine("- All candidates carry confidence LOCAL_TEXT_MATCH_RANKED_ONLY.");
        sb.AppendLine("- No candidates are final writer choices.");
        sb.AppendLine();
        sb.AppendLine("VERDICT: MAP22L_SYSTEM2_STATIC_ROAD_TILE_CANDIDATE_REVIEW_PACKET_COMPLETE");
        return sb.ToString();
    }

    public static string RenderCsv(System2StaticRoadTileCandidateReview review)
    {
        var sb = new StringBuilder();
        sb.AppendLine("candidate_family,role,rank,tile_name,source_file,score,score_reasons,review_status,recommended_next_action,confidence");

        foreach (var family in review.Families)
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
}
