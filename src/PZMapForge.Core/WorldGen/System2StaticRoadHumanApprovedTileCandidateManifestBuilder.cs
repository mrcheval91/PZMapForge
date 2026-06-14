using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class System2StaticRoadHumanApprovedTileCandidateManifestBuilder
{
    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };

    private const string ActionApproved = "Use only as input for future visual/runtime validation; not writer-ready.";
    private const string ActionRejected = "Excluded by human review; do not use in writer.";
    private const string ActionPending  = "Inspect in TileZed or tile sheet preview before writer use.";

    public static System2StaticRoadHumanApprovedTileCandidateManifestResult Build(
        string appliedReviewJsonPath)
    {
        var result = new System2StaticRoadHumanApprovedTileCandidateManifestResult();

        if (!File.Exists(appliedReviewJsonPath))
        {
            result.Errors.Add($"Applied review JSON not found: {appliedReviewJsonPath}");
            return result;
        }

        var approved = new List<System2StaticRoadHumanApprovedTileCandidate>();
        var rejected = new List<System2StaticRoadHumanApprovedTileCandidate>();
        var pending  = new List<System2StaticRoadHumanApprovedTileCandidate>();
        var familyCount = 0;

        try
        {
            var json = File.ReadAllText(appliedReviewJsonPath, Encoding.UTF8);
            using var doc  = JsonDocument.Parse(json, DocOpts);
            var root = doc.RootElement;

            if (!root.TryGetProperty("families", out var familiesElem)
                || familiesElem.ValueKind != JsonValueKind.Array)
            {
                result.Errors.Add("Applied review JSON missing 'families' array");
                return result;
            }

            foreach (var f in familiesElem.EnumerateArray())
            {
                var family = f.TryGetProperty("candidate_family", out var cfP) ? cfP.GetString() ?? "" : "";
                var role   = f.TryGetProperty("role",             out var rP)  ? rP.GetString()  ?? "" : "";

                if (string.IsNullOrEmpty(family)) continue;
                familyCount++;

                if (!f.TryGetProperty("review_items", out var riElem)
                    || riElem.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var item in riElem.EnumerateArray())
                {
                    var rank       = item.TryGetProperty("rank",         out var rkP) ? rkP.GetInt32()  : 0;
                    var tileName   = item.TryGetProperty("tile_name",    out var tnP) ? tnP.GetString() ?? "" : "";
                    var sourceFile = item.TryGetProperty("source_file",  out var sfP) ? sfP.GetString() ?? "" : "";
                    var score      = item.TryGetProperty("score",        out var spP) ? spP.GetInt32()  : 0;
                    var humanNote  = item.TryGetProperty("human_note",   out var hnP) ? hnP.GetString() ?? "" : "";
                    var status     = item.TryGetProperty("review_status", out var rsP) ? rsP.GetString() ?? "" : "";
                    var confidence = item.TryGetProperty("confidence",   out var coP) ? coP.GetString() ?? "" : "";

                    var reasons = new List<string>();
                    if (item.TryGetProperty("score_reasons", out var srP)
                        && srP.ValueKind == JsonValueKind.Array)
                        foreach (var r in srP.EnumerateArray())
                            reasons.Add(r.GetString() ?? "");

                    if (string.IsNullOrEmpty(tileName)) continue;

                    var action = status switch
                    {
                        "APPROVED_BY_HUMAN_REVIEW" => ActionApproved,
                        "REJECTED_BY_HUMAN_REVIEW" => ActionRejected,
                        _                          => ActionPending,
                    };

                    var candidate = new System2StaticRoadHumanApprovedTileCandidate
                    {
                        CandidateFamily       = family,
                        Role                  = role,
                        Rank                  = rank,
                        TileName              = tileName,
                        SourceFile            = sourceFile,
                        Score                 = score,
                        ScoreReasons          = reasons,
                        HumanNote             = humanNote,
                        ReviewStatus          = status,
                        Confidence            = confidence,
                        RecommendedNextAction = action,
                    };

                    switch (status)
                    {
                        case "APPROVED_BY_HUMAN_REVIEW": approved.Add(candidate); break;
                        case "REJECTED_BY_HUMAN_REVIEW": rejected.Add(candidate); break;
                        default:                         pending.Add(candidate);  break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse applied review JSON: {ex.Message}");
            return result;
        }

        if (familyCount == 0)
        {
            result.Errors.Add("Applied review JSON contains no families");
            return result;
        }

        result.IsValid  = true;
        result.Manifest = new System2StaticRoadHumanApprovedTileCandidateManifest
        {
            SourceAppliedReviewJson = appliedReviewJsonPath,
            ApprovedCandidates      = approved,
            RejectedCandidates      = rejected,
            PendingCandidates       = pending,
            Totals                  = new System2StaticRoadHumanApprovedTileCandidateTotals
            {
                FamilyCount     = familyCount,
                ReviewItemCount = approved.Count + rejected.Count + pending.Count,
                ApprovedCount   = approved.Count,
                RejectedCount   = rejected.Count,
                PendingCount    = pending.Count,
            },
        };
        return result;
    }

    public static string RenderMarkdown(System2StaticRoadHumanApprovedTileCandidateManifest manifest)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# DeadMTL System 2 Human-Approved Tile Candidates (MAP-22N)");
        sb.AppendLine();
        sb.AppendLine("> **Warning:** Human approval is not runtime proof and not writer readiness.");
        sb.AppendLine("> Approved candidates are tiles a human considers worth keeping for a future");
        sb.AppendLine("> validation pass. They are not validated at runtime and are not writer-ready.");
        sb.AppendLine();
        sb.AppendLine("## Totals");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| family_count | {manifest.Totals.FamilyCount} |");
        sb.AppendLine($"| review_item_count | {manifest.Totals.ReviewItemCount} |");
        sb.AppendLine($"| approved_count | {manifest.Totals.ApprovedCount} |");
        sb.AppendLine($"| rejected_count | {manifest.Totals.RejectedCount} |");
        sb.AppendLine($"| pending_count | {manifest.Totals.PendingCount} |");
        sb.AppendLine();

        // Approved
        sb.AppendLine("## Approved candidates");
        sb.AppendLine();
        if (manifest.ApprovedCandidates.Count == 0)
        {
            sb.AppendLine("_No approved candidates._");
        }
        else
        {
            sb.AppendLine("| Family | Rank | Tile Name | Score | Source File | Human Note |");
            sb.AppendLine("|--------|------|-----------|-------|-------------|------------|");
            foreach (var c in manifest.ApprovedCandidates)
                sb.AppendLine($"| {c.CandidateFamily} | {c.Rank} | `{c.TileName}` | {c.Score} | {c.SourceFile} | {c.HumanNote} |");
        }
        sb.AppendLine();

        // Rejected
        sb.AppendLine("## Rejected candidates");
        sb.AppendLine();
        if (manifest.RejectedCandidates.Count == 0)
        {
            sb.AppendLine("_No rejected candidates._");
        }
        else
        {
            sb.AppendLine("| Family | Rank | Tile Name | Score | Source File | Human Note |");
            sb.AppendLine("|--------|------|-----------|-------|-------------|------------|");
            foreach (var c in manifest.RejectedCandidates)
                sb.AppendLine($"| {c.CandidateFamily} | {c.Rank} | `{c.TileName}` | {c.Score} | {c.SourceFile} | {c.HumanNote} |");
        }
        sb.AppendLine();

        // Pending
        sb.AppendLine("## Still pending manual review");
        sb.AppendLine();
        if (manifest.PendingCandidates.Count == 0)
        {
            sb.AppendLine("_All items have been reviewed._");
        }
        else
        {
            sb.AppendLine("| Family | Rank | Tile Name | Score | Source File |");
            sb.AppendLine("|--------|------|-----------|-------|-------------|");
            foreach (var c in manifest.PendingCandidates)
                sb.AppendLine($"| {c.CandidateFamily} | {c.Rank} | `{c.TileName}` | {c.Score} | {c.SourceFile} |");
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
        sb.AppendLine("- All approved candidates carry confidence HUMAN_REVIEW_ONLY_NOT_RUNTIME_PROVEN.");
        sb.AppendLine();
        sb.AppendLine("VERDICT: MAP22N_SYSTEM2_STATIC_ROAD_HUMAN_APPROVED_TILE_CANDIDATES_COMPLETE");
        return sb.ToString();
    }

    public static string RenderCsv(System2StaticRoadHumanApprovedTileCandidateManifest manifest)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "bucket,candidate_family,role,rank,tile_name,source_file,score,score_reasons," +
            "human_note,review_status,confidence,recommended_next_action");

        foreach (var (bucket, list) in new[]
        {
            ("approved", manifest.ApprovedCandidates),
            ("rejected", manifest.RejectedCandidates),
            ("pending",  manifest.PendingCandidates),
        })
        {
            foreach (var c in list)
            {
                var reasons = string.Join("|", c.ScoreReasons).Replace(",", ";");
                sb.AppendLine(
                    $"{bucket}," +
                    $"{EscCsv(c.CandidateFamily)}," +
                    $"{EscCsv(c.Role)}," +
                    $"{c.Rank}," +
                    $"{EscCsv(c.TileName)}," +
                    $"{EscCsv(c.SourceFile)}," +
                    $"{c.Score}," +
                    $"{EscCsv(reasons)}," +
                    $"{EscCsv(c.HumanNote)}," +
                    $"{EscCsv(c.ReviewStatus)}," +
                    $"{EscCsv(c.Confidence)}," +
                    $"{EscCsv(c.RecommendedNextAction)}");
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
