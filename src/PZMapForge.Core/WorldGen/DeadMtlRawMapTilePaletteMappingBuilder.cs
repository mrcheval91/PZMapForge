using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class DeadMtlRawMapTilePaletteMappingBuilder
{
    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };
    private const int NearMatchThreshold   = 32;
    private const int MaxSuggestionsPerColor = 3;

    public static DeadMtlRawMapTilePaletteMappingResult Build(
        string inspectionJsonPath,
        string worldgenPalettePath = "",
        string system2PalettePath  = "")
    {
        var result = new DeadMtlRawMapTilePaletteMappingResult();

        if (!File.Exists(inspectionJsonPath))
        {
            result.Errors.Add($"Inspection JSON not found: {inspectionJsonPath}");
            return result;
        }

        string sourceImage   = string.Empty;
        string sourceSha256  = string.Empty;
        int    width         = 0;
        int    height        = 0;
        var    topColors     = new List<(string Hex, int Count, double Pct)>();

        try
        {
            var json = File.ReadAllText(inspectionJsonPath, Encoding.UTF8);
            using var doc = JsonDocument.Parse(json, DocOpts);
            var root = doc.RootElement;

            if (root.TryGetProperty("source_image", out var si)) sourceImage  = si.GetString() ?? "";
            if (root.TryGetProperty("sha256",        out var sh)) sourceSha256 = sh.GetString() ?? "";
            if (root.TryGetProperty("width",          out var w))  width       = w.GetInt32();
            if (root.TryGetProperty("height",         out var h))  height      = h.GetInt32();

            if (root.TryGetProperty("top_colors", out var tc) &&
                tc.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in tc.EnumerateArray())
                {
                    var hex   = e.TryGetProperty("color",      out var cv) ? cv.GetString() ?? "" : "";
                    var count = e.TryGetProperty("count",      out var cn) ? cn.GetInt32()  : 0;
                    var pct   = e.TryGetProperty("percentage", out var pv) ? pv.GetDouble() : 0.0;
                    if (!string.IsNullOrEmpty(hex)) topColors.Add((hex, count, pct));
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to read inspection JSON: {ex.Message}");
            return result;
        }

        var worldgenEntries = LoadWorldgenPalette(worldgenPalettePath);
        var system2Entries  = LoadSystem2Palette(system2PalettePath);

        var mappings         = new List<DeadMtlRawMapTilePaletteMappingEntry>();
        int autoMatchedCount = 0;
        int unmappedCount    = 0;
        int nearSuggCount    = 0;
        int pixelTotal       = topColors.Sum(c => c.Count);

        foreach (var (hex, count, pct) in topColors)
        {
            var wgMatch = worldgenEntries.FirstOrDefault(e =>
                string.Equals(e.Color, hex, StringComparison.OrdinalIgnoreCase));

            if (wgMatch != null)
            {
                mappings.Add(new DeadMtlRawMapTilePaletteMappingEntry
                {
                    SourceColor   = hex,
                    PixelCount    = count,
                    Percentage    = pct,
                    MappingStatus = "AUTO_MATCHED_EXISTING_PALETTE",
                    TargetSystem  = "WORLDGEN",
                    TargetType    = wgMatch.Type,
                    TargetKey     = wgMatch.Key,
                    Confidence    = "EXACT_PALETTE_MATCH_ONLY",
                    Notes         = "Exact color match from worldgen-png-palette.json.",
                });
                autoMatchedCount++;
                continue;
            }

            var s2Match = system2Entries.FirstOrDefault(e =>
                string.Equals(e.Hex, hex, StringComparison.OrdinalIgnoreCase));

            if (s2Match != null)
            {
                mappings.Add(new DeadMtlRawMapTilePaletteMappingEntry
                {
                    SourceColor   = hex,
                    PixelCount    = count,
                    Percentage    = pct,
                    MappingStatus = "AUTO_MATCHED_EXISTING_PALETTE",
                    TargetSystem  = "SYSTEM2_STATIC_ROAD",
                    TargetType    = "intent",
                    TargetKey     = s2Match.Intent,
                    Confidence    = "EXACT_PALETTE_MATCH_ONLY",
                    Notes         = "Exact color match from system2-static-road-intent-palette.json.",
                });
                autoMatchedCount++;
                continue;
            }

            var suggestions = BuildSuggestions(hex, worldgenEntries, system2Entries);
            if (suggestions.Count > 0) nearSuggCount++;

            mappings.Add(new DeadMtlRawMapTilePaletteMappingEntry
            {
                SourceColor   = hex,
                PixelCount    = count,
                Percentage    = pct,
                MappingStatus = "UNMAPPED_NEEDS_HUMAN_DECISION",
                TargetSystem  = "",
                TargetType    = "",
                TargetKey     = "",
                Confidence    = "UNMAPPED",
                NearestPaletteSuggestions = suggestions,
                Notes = suggestions.Count > 0
                    ? "Near match only. Do not auto-normalize."
                    : "No matching palette entry found.",
            });
            unmappedCount++;
        }

        result.IsValid = true;
        result.Mapping = new DeadMtlRawMapTilePaletteMapping
        {
            SourceInspectionJson = inspectionJsonPath,
            SourceImage          = sourceImage,
            SourceSha256         = sourceSha256,
            Width                = width,
            Height               = height,
            Mappings             = mappings,
            Totals               = new DeadMtlRawMapTilePaletteMappingTotals
            {
                MappingCount             = mappings.Count,
                AutoMatchedCount         = autoMatchedCount,
                UnmappedCount            = unmappedCount,
                NearMatchSuggestionCount = nearSuggCount,
                PixelCountTotal          = pixelTotal,
            },
        };
        return result;
    }

    public static string RenderMarkdown(DeadMtlRawMapTilePaletteMapping mapping)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-23B: DeadMTL Raw Tile Palette Mapping Contract");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** This is a mapping contract only. " +
                      "No compilation, no runtime proof, no writer readiness.");
        sb.AppendLine();
        sb.AppendLine("## Source");
        sb.AppendLine();
        sb.AppendLine($"- source_image: `{mapping.SourceImage}`");
        sb.AppendLine($"- source_sha256: `{mapping.SourceSha256}`");
        sb.AppendLine($"- source_inspection_json: `{mapping.SourceInspectionJson}`");
        sb.AppendLine($"- status: {mapping.Status}");
        sb.AppendLine($"- runtime_status: {mapping.RuntimeStatus}");
        sb.AppendLine();
        sb.AppendLine("## Totals");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| mapping_count | {mapping.Totals.MappingCount} |");
        sb.AppendLine($"| auto_matched_count | {mapping.Totals.AutoMatchedCount} |");
        sb.AppendLine($"| unmapped_count | {mapping.Totals.UnmappedCount} |");
        sb.AppendLine($"| near_match_suggestion_count | {mapping.Totals.NearMatchSuggestionCount} |");
        sb.AppendLine($"| pixel_count_total | {mapping.Totals.PixelCountTotal} |");
        sb.AppendLine();
        sb.AppendLine("## Color mappings");
        sb.AppendLine();
        sb.AppendLine("| color | count | % | status | target_system | target_key | confidence |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var m in mapping.Mappings)
        {
            var targetKey = string.IsNullOrEmpty(m.TargetKey) ? "(unmapped)" : m.TargetKey;
            sb.AppendLine(
                $"| `{m.SourceColor}` | {m.PixelCount} | {m.Percentage:F2}% " +
                $"| {m.MappingStatus} | {m.TargetSystem} | {targetKey} | {m.Confidence} |");
        }
        sb.AppendLine();

        var unmapped = mapping.Mappings
            .Where(m => m.MappingStatus == "UNMAPPED_NEEDS_HUMAN_DECISION")
            .ToList();

        if (unmapped.Count > 0)
        {
            sb.AppendLine("## Unmapped colors");
            sb.AppendLine();
            foreach (var m in unmapped)
            {
                sb.AppendLine($"### `{m.SourceColor}` ({m.PixelCount} pixels, {m.Percentage:F2}%)");
                sb.AppendLine();
                if (m.NearestPaletteSuggestions.Count > 0)
                {
                    sb.AppendLine("Near-match suggestions (do NOT auto-normalize):");
                    sb.AppendLine();
                    foreach (var s in m.NearestPaletteSuggestions)
                        sb.AppendLine($"- `{s.Color}` ({s.Palette} / {s.Key}, rgb_distance={s.RgbDistance})");
                    sb.AppendLine();
                }
                else
                {
                    sb.AppendLine("No near-match suggestions within threshold.");
                    sb.AppendLine();
                }
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
        sb.AppendLine("MAP23B_RAW_TILE_PALETTE_MAPPING_CONTRACT_COMPLETE");
        return sb.ToString();
    }

    public static string RenderCsv(DeadMtlRawMapTilePaletteMapping mapping)
    {
        var sb = new StringBuilder();
        sb.AppendLine("source_color,pixel_count,percentage,mapping_status,target_system,target_type,target_key,confidence,nearest_suggestions,notes");
        foreach (var m in mapping.Mappings)
        {
            var suggestions = m.NearestPaletteSuggestions.Count > 0
                ? string.Join("|", m.NearestPaletteSuggestions.Select(s =>
                    $"{s.Color}:{s.Palette}/{s.Key}/d={s.RgbDistance}"))
                : "";
            sb.AppendLine(
                $"{m.SourceColor},{m.PixelCount},{m.Percentage:F2}," +
                $"{m.MappingStatus},{m.TargetSystem},{m.TargetType},{m.TargetKey}," +
                $"{m.Confidence},{suggestions},{m.Notes}");
        }
        return sb.ToString();
    }

    // -----------------------------------------------------------------------
    // Near-match suggestions
    // -----------------------------------------------------------------------

    private static List<DeadMtlRawMapTilePaletteMappingSuggestion> BuildSuggestions(
        string hex,
        IReadOnlyList<WorldgenEntry> worldgenEntries,
        IReadOnlyList<System2Entry>  system2Entries)
    {
        var (r1, g1, b1) = ParseHex(hex);
        var candidates = new List<(int Distance, DeadMtlRawMapTilePaletteMappingSuggestion Suggestion)>();

        foreach (var e in worldgenEntries)
        {
            var (r2, g2, b2) = ParseHex(e.Color);
            var d = RgbDistance(r1, g1, b1, r2, g2, b2);
            if (d <= NearMatchThreshold)
            {
                candidates.Add((d, new DeadMtlRawMapTilePaletteMappingSuggestion
                {
                    Palette     = "worldgen",
                    Color       = e.Color,
                    Type        = e.Type,
                    Key         = e.Key,
                    RgbDistance = d,
                }));
            }
        }

        foreach (var e in system2Entries)
        {
            var (r2, g2, b2) = ParseHex(e.Hex);
            var d = RgbDistance(r1, g1, b1, r2, g2, b2);
            if (d <= NearMatchThreshold)
            {
                candidates.Add((d, new DeadMtlRawMapTilePaletteMappingSuggestion
                {
                    Palette     = "system2_static_road",
                    Color       = e.Hex,
                    Type        = "intent",
                    Key         = e.Intent,
                    RgbDistance = d,
                }));
            }
        }

        return candidates
            .OrderBy(c => c.Distance)
            .Take(MaxSuggestionsPerColor)
            .Select(c => c.Suggestion)
            .ToList();
    }

    private static int RgbDistance(int r1, int g1, int b1, int r2, int g2, int b2)
    {
        var dr = r1 - r2;
        var dg = g1 - g2;
        var db = b1 - b2;
        return (int)Math.Round(Math.Sqrt(dr * dr + dg * dg + db * db));
    }

    private static (int R, int G, int B) ParseHex(string hex)
    {
        var h = hex.TrimStart('#');
        if (h.Length != 6) return (0, 0, 0);
        return (
            Convert.ToInt32(h[..2], 16),
            Convert.ToInt32(h[2..4], 16),
            Convert.ToInt32(h[4..6], 16));
    }

    // -----------------------------------------------------------------------
    // Palette loaders
    // -----------------------------------------------------------------------

    private sealed record WorldgenEntry(string Color, string Type, string Key);
    private sealed record System2Entry(string Hex, string Intent);

    private static IReadOnlyList<WorldgenEntry> LoadWorldgenPalette(string path)
    {
        var entries = new List<WorldgenEntry>();
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return entries;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8), DocOpts);
            if (doc.RootElement.TryGetProperty("entries", out var arr) &&
                arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in arr.EnumerateArray())
                {
                    var color = e.TryGetProperty("color", out var c) ? c.GetString() ?? "" : "";
                    var type  = e.TryGetProperty("type",  out var t) ? t.GetString() ?? "" : "";
                    var key   = e.TryGetProperty("key",   out var k) ? k.GetString() ?? "" : "";
                    if (!string.IsNullOrEmpty(color)) entries.Add(new WorldgenEntry(color, type, key));
                }
            }
        }
        catch { /* best effort */ }
        return entries;
    }

    private static IReadOnlyList<System2Entry> LoadSystem2Palette(string path)
    {
        var entries = new List<System2Entry>();
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return entries;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8), DocOpts);
            if (doc.RootElement.TryGetProperty("entries", out var arr) &&
                arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in arr.EnumerateArray())
                {
                    var hex    = e.TryGetProperty("hex",    out var h) ? h.GetString() ?? "" : "";
                    var intent = e.TryGetProperty("intent", out var i) ? i.GetString() ?? "" : "";
                    if (!string.IsNullOrEmpty(hex)) entries.Add(new System2Entry(hex, intent));
                }
            }
        }
        catch { /* best effort */ }
        return entries;
    }
}
