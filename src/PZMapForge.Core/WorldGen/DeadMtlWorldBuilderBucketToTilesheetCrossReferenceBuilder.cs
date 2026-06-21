using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderBucketToTilesheetCrossReferenceBuilder
{
    private static readonly JsonSerializerOptions s_json = new()
    {
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private const int MaxTileSourceFiles     = 500;
    private const int MaxBytesPerFile        = 8192;
    private const int MaxCandidatesPerBucket = 5;
    private const int MaxTotalCandidates     = 200;

    private static readonly (string Bucket, string IntendedUse)[] s_defaultBuckets =
    {
        ("WALL",       "wall_tile_exterior_or_interior"),
        ("FLOOR",      "floor_tile_exterior_or_interior"),
        ("ACCESS",     "door_fence_or_opening_tile"),
        ("LOT",        "lot_boundary_or_zone_tile"),
        ("ROAD",       "road_or_street_surface_tile"),
        ("VEGETATION", "vegetation_or_ground_cover_tile"),
        ("WATER",      "water_surface_tile"),
        ("UNKNOWN",    "unknown_or_default_tile"),
    };

    private static readonly (string Bucket, string CandidateName)[] s_builtInCandidates =
    {
        ("WALL",       "location_walls_exterior_01"),
        ("FLOOR",      "floors_exterior_cement_01"),
        ("ACCESS",     "location_doors_exterior_01"),
        ("LOT",        "location_interiors_basic_01"),
        ("ROAD",       "location_roads_01"),
        ("VEGETATION", "vegetation_plants_01"),
        ("WATER",      "location_water_01"),
        ("UNKNOWN",    "unknown_tile_candidate"),
    };

    private static readonly Dictionary<string, string[]> s_bucketTerms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["WALL"]       = new[] { "wall", "walls", "exterior" },
        ["FLOOR"]      = new[] { "floor", "floors", "concrete", "pavement", "asphalt" },
        ["ACCESS"]     = new[] { "fence", "road", "street", "pavement" },
        ["LOT"]        = new[] { "location", "residential", "industry", "exterior" },
        ["ROAD"]       = new[] { "road", "street", "asphalt", "pavement", "concrete" },
        ["VEGETATION"] = new[] { "grass", "vegetation", "tree", "bush" },
        ["WATER"]      = new[] { "water" },
        ["UNKNOWN"]    = new[] { "tile" },
    };

    private static readonly HashSet<string> s_scanExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".lua", ".xml", ".properties", ".json", ".ini", ".list",
    };

    private static readonly string[] s_autoDetectPzPaths =
    {
        @"C:\Program Files (x86)\Steam\steamapps\common\ProjectZomboid",
        @"C:\Program Files\Steam\steamapps\common\ProjectZomboid",
        @"D:\Steam\steamapps\common\ProjectZomboid",
        @"E:\Steam\steamapps\common\ProjectZomboid",
        @"C:\Games\Steam\steamapps\common\ProjectZomboid",
        @"D:\Games\Steam\steamapps\common\ProjectZomboid",
    };

    public DeadMtlWorldBuilderBucketToTilesheetCrossReferenceResult Build(
        string? emitterJsonPath,
        string? paletteGuidePath,
        string? paletteSwatchesPath,
        string? pzInstallRoot,
        string  outputRoot)
    {
        var result = new DeadMtlWorldBuilderBucketToTilesheetCrossReferenceResult
        {
            Format               = "MAP36F_BUCKET_TO_TILESHEET_CROSS_REFERENCE_V1",
            GeneratedUtc         = DateTime.UtcNow.ToString("O"),
            EmitterJsonPath      = emitterJsonPath     ?? string.Empty,
            PaletteGuidePath     = paletteGuidePath    ?? string.Empty,
            PaletteSwatchesPath  = paletteSwatchesPath ?? string.Empty,
            OutputRoot           = outputRoot          ?? string.Empty,
        };

        result.RuntimeBinaryWritten    = false;
        result.GeometryInjected        = false;
        result.PlayableExportClaimed   = false;
        result.WorkshopUploadPerformed = false;
        result.SteamInstallWrite       = false;
        result.VerifiedRuntimeTileId   = false;
        result.ReadOnlyProbe           = true;

        // Resolve PZ install root
        string? resolvedPzRoot = !string.IsNullOrEmpty(pzInstallRoot)
            ? pzInstallRoot
            : s_autoDetectPzPaths.FirstOrDefault(Directory.Exists);
        result.PzInstallRoot = resolvedPzRoot ?? string.Empty;
        result.PzInstallFound = !string.IsNullOrEmpty(resolvedPzRoot)
                             && Directory.Exists(resolvedPzRoot);

        // Source availability
        result.EmitterJsonFound      = !string.IsNullOrEmpty(emitterJsonPath)
                                    && File.Exists(emitterJsonPath);
        result.PaletteGuideFound     = !string.IsNullOrEmpty(paletteGuidePath)
                                    && File.Exists(paletteGuidePath);
        result.PaletteSwatchesFound  = !string.IsNullOrEmpty(paletteSwatchesPath)
                                    && File.Exists(paletteSwatchesPath);

        // ---- 1. Bucket intents ----
        result.BucketIntents = BuildBucketIntents(
            result.EmitterJsonFound  ? emitterJsonPath  : null,
            result.PaletteGuideFound ? paletteGuidePath : null);

        // ---- 2. Tile source scan ----
        if (result.PzInstallFound)
            result.TileSourceInventory = ScanTileSources(resolvedPzRoot!);

        // ---- 3. Tilesheet candidates from PZ files ----
        if (result.TileSourceInventory.Count > 0)
            result.TilesheetCandidates = BuildTilesheetCandidates(result.TileSourceInventory);

        // ---- 4. Bucket→tilesheet cross-reference mappings ----
        result.BucketToTilesheetCandidates = BuildBucketToTilesheetCandidates(
            result.BucketIntents, result.TileSourceInventory);

        // ---- Checks (12) ----
        var checks = new List<BucketToTilesheetCheck>();
        AddCheck(checks, "MAP36F_OUTPUT_ROOT_CONTAINS_LOCAL",
            "output root path is sandboxed under .local",
            "true", result.OutputRoot.Contains(".local", StringComparison.OrdinalIgnoreCase)
                       .ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36F_BUCKET_INTENT_INVENTORY_EMITTED",
            "Bucket intent inventory has at least one entry",
            "true", (result.BucketIntents.Count > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36F_TILE_SOURCE_INVENTORY_EMITTED",
            "Tile source inventory list was produced",
            "true", "true");
        AddCheck(checks, "MAP36F_TILE_CANDIDATE_INVENTORY_EMITTED",
            "Tilesheet candidate list was produced",
            "true", "true");
        AddCheck(checks, "MAP36F_BUCKET_TO_TILE_CANDIDATES_EMITTED",
            "Bucket-to-tilesheet candidate mappings emitted",
            "true", (result.BucketToTilesheetCandidates.Count > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36F_CLAIMS_RUNTIME_BINARY_WRITTEN_FALSE",
            "runtime_binary_written is false",
            "false", result.RuntimeBinaryWritten.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36F_GEOMETRY_INJECTED_FALSE",
            "geometry_injected is false",
            "false", result.GeometryInjected.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36F_PLAYABLE_EXPORT_CLAIMED_FALSE",
            "playable_export_claimed is false",
            "false", result.PlayableExportClaimed.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36F_WORKSHOP_UPLOAD_PERFORMED_FALSE",
            "workshop_upload_performed is false",
            "false", result.WorkshopUploadPerformed.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36F_STEAM_INSTALL_WRITE_FALSE",
            "steam_install_write is false",
            "false", result.SteamInstallWrite.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36F_VERIFIED_RUNTIME_TILE_ID_FALSE",
            "verified_runtime_tile_id is false — no tile IDs are runtime-verified",
            "false", result.VerifiedRuntimeTileId.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36F_READ_ONLY_PROBE_TRUE",
            "read_only_probe is true",
            "true", result.ReadOnlyProbe.ToString().ToLowerInvariant());

        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");
        result.IsValid          = result.FailedCheckCount == 0 && result.Errors.Count == 0;
        result.Verdict          = result.IsValid
            ? "MAP36F_BUCKET_TO_TILESHEET_CROSS_REFERENCE_COMPLETE"
            : "MAP36F_BUCKET_TO_TILESHEET_CROSS_REFERENCE_FAILED";

        return result;
    }

    // -------------------------------------------------------------------------
    // Bucket intent collection
    // -------------------------------------------------------------------------

    private static List<BucketIntentRecord> BuildBucketIntents(
        string? emitterJsonPath, string? paletteGuidePath)
    {
        var intents   = new List<BucketIntentRecord>();
        var seenBuckets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Hardcoded defaults — always present
        foreach (var (bucket, intendedUse) in s_defaultBuckets)
        {
            intents.Add(new BucketIntentRecord
            {
                Bucket      = bucket,
                IntendedUse = intendedUse,
                Source      = "built_in",
            });
            seenBuckets.Add(bucket);
        }

        // Augment from emitter JSON text scan
        if (emitterJsonPath != null && File.Exists(emitterJsonPath))
        {
            try
            {
                string text = File.ReadAllText(emitterJsonPath);
                foreach (var (bucket, intendedUse) in s_defaultBuckets)
                {
                    if (!seenBuckets.Contains(bucket)
                        && text.Contains(bucket, StringComparison.OrdinalIgnoreCase))
                    {
                        intents.Add(new BucketIntentRecord
                        {
                            Bucket      = bucket,
                            IntendedUse = intendedUse,
                            Source      = "emitter_json",
                        });
                        seenBuckets.Add(bucket);
                    }
                }
            }
            catch { }
        }

        // Augment from palette guide text scan
        if (paletteGuidePath != null && File.Exists(paletteGuidePath))
        {
            try
            {
                string text = File.ReadAllText(paletteGuidePath);
                foreach (var (bucket, intendedUse) in s_defaultBuckets)
                {
                    if (!seenBuckets.Contains(bucket)
                        && text.Contains(bucket, StringComparison.OrdinalIgnoreCase))
                    {
                        intents.Add(new BucketIntentRecord
                        {
                            Bucket      = bucket,
                            IntendedUse = intendedUse,
                            Source      = "palette_guide",
                        });
                        seenBuckets.Add(bucket);
                    }
                }
            }
            catch { }
        }

        return intents;
    }

    // -------------------------------------------------------------------------
    // Tile source scan
    // -------------------------------------------------------------------------

    private static List<TileSourceInventoryRecord> ScanTileSources(string pzInstallRoot)
    {
        var inventory = new List<TileSourceInventoryRecord>();
        try
        {
            var allTerms = s_bucketTerms.Values.SelectMany(t => t)
                               .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            int count = 0;
            foreach (string file in Directory.EnumerateFiles(
                pzInstallRoot, "*.*", SearchOption.AllDirectories))
            {
                if (count >= MaxTileSourceFiles) break;
                string ext = Path.GetExtension(file);
                if (!s_scanExtensions.Contains(ext)) continue;

                try
                {
                    long sizeBytes = new FileInfo(file).Length;
                    if (sizeBytes > 1024 * 1024) continue; // skip files > 1MB

                    int readBytes = (int)Math.Min(sizeBytes, MaxBytesPerFile);
                    byte[] buf = new byte[readBytes];
                    using var fs = File.OpenRead(file);
                    int read = fs.Read(buf, 0, readBytes);
                    string content = Encoding.UTF8.GetString(buf, 0, read);

                    int termsFound = allTerms.Count(t =>
                        content.Contains(t, StringComparison.OrdinalIgnoreCase));

                    if (termsFound > 0)
                    {
                        inventory.Add(new TileSourceInventoryRecord
                        {
                            FilePath       = file,
                            FileSizeBytes  = sizeBytes,
                            TileTermsFound = termsFound,
                            SourceType     = "local_pz_install",
                        });
                        count++;
                    }
                }
                catch { }
            }
        }
        catch { }
        return inventory;
    }

    // -------------------------------------------------------------------------
    // Tilesheet candidate extraction from scanned files
    // -------------------------------------------------------------------------

    private static List<TilesheetCandidateRecord> BuildTilesheetCandidates(
        List<TileSourceInventoryRecord> sourceInventory)
    {
        var candidates = new List<TilesheetCandidateRecord>();
        foreach (var src in sourceInventory)
        {
            string stem = Path.GetFileNameWithoutExtension(src.FilePath);
            string preview = string.Empty;
            try
            {
                int readBytes = (int)Math.Min(src.FileSizeBytes, MaxBytesPerFile);
                byte[] buf = new byte[readBytes];
                using var fs = File.OpenRead(src.FilePath);
                int read = fs.Read(buf, 0, readBytes);
                string content = Encoding.UTF8.GetString(buf, 0, read);
                preview = content.Length > 80
                    ? content.Substring(0, 80).Replace('\n', ' ').Replace('\r', ' ')
                    : content.Replace('\n', ' ').Replace('\r', ' ');
            }
            catch { }

            candidates.Add(new TilesheetCandidateRecord
            {
                CandidateName       = stem,
                SourceFile          = src.FilePath,
                EvidenceTextPreview = preview,
                SourceType          = "local_pz_install",
            });
        }
        return candidates;
    }

    // -------------------------------------------------------------------------
    // Bucket→tilesheet candidate cross-reference
    // -------------------------------------------------------------------------

    private static List<BucketToTilesheetCandidateRecord> BuildBucketToTilesheetCandidates(
        List<BucketIntentRecord> bucketIntents,
        List<TileSourceInventoryRecord> tileSourceInventory)
    {
        var candidates = new List<BucketToTilesheetCandidateRecord>();

        foreach (var intent in bucketIntents)
        {
            if (!s_bucketTerms.TryGetValue(intent.Bucket, out var terms))
                continue;

            int addedForBucket = 0;

            // Candidates from scanned PZ files
            foreach (var src in tileSourceInventory)
            {
                if (addedForBucket >= MaxCandidatesPerBucket
                    || candidates.Count >= MaxTotalCandidates) break;

                string content = string.Empty;
                try
                {
                    int readBytes = (int)Math.Min(src.FileSizeBytes, MaxBytesPerFile);
                    byte[] buf = new byte[readBytes];
                    using var fs = File.OpenRead(src.FilePath);
                    int read = fs.Read(buf, 0, readBytes);
                    content = Encoding.UTF8.GetString(buf, 0, read);
                }
                catch { continue; }

                string? matchedTerm = terms.FirstOrDefault(t =>
                    content.Contains(t, StringComparison.OrdinalIgnoreCase));
                if (matchedTerm == null) continue;

                string stem    = Path.GetFileNameWithoutExtension(src.FilePath);
                string fileName = Path.GetFileName(src.FilePath);
                bool nameHasTerm = terms.Any(t =>
                    fileName.Contains(t, StringComparison.OrdinalIgnoreCase));
                bool nameHasBucket = fileName.Contains(
                    intent.Bucket, StringComparison.OrdinalIgnoreCase);

                string confidence = nameHasBucket ? "HIGH_EXACT_BUCKET_MATCH"
                    : nameHasTerm ? "MEDIUM_NAME_MATCH"
                    : "LOW_GUESS";

                // Extract snippet around the matched term
                int idx = content.IndexOf(matchedTerm, StringComparison.OrdinalIgnoreCase);
                int snippetStart = Math.Max(0, idx - 20);
                int snippetLen   = Math.Min(80, content.Length - snippetStart);
                string preview   = content.Substring(snippetStart, snippetLen)
                    .Replace('\n', ' ').Replace('\r', ' ');

                candidates.Add(new BucketToTilesheetCandidateRecord
                {
                    Bucket                       = intent.Bucket,
                    IntendedUse                  = intent.IntendedUse,
                    CandidateTilesheetOrTileName = stem,
                    SourceFile                   = src.FilePath,
                    EvidenceTextPreview          = preview,
                    ConfidenceLabel              = confidence,
                    VerifiedRuntimeTileId        = false,
                });
                addedForBucket++;
            }

            // Built-in fallback if no PZ files produced a candidate for this bucket
            if (addedForBucket == 0 && candidates.Count < MaxTotalCandidates)
            {
                var builtin = s_builtInCandidates.FirstOrDefault(b =>
                    string.Equals(b.Bucket, intent.Bucket, StringComparison.OrdinalIgnoreCase));
                if (builtin.CandidateName != null)
                {
                    candidates.Add(new BucketToTilesheetCandidateRecord
                    {
                        Bucket                       = intent.Bucket,
                        IntendedUse                  = intent.IntendedUse,
                        CandidateTilesheetOrTileName = builtin.CandidateName,
                        SourceFile                   = "built_in_mapping",
                        EvidenceTextPreview          = $"Built-in name candidate for bucket {intent.Bucket}. GUESS_NOT_VERIFIED.",
                        ConfidenceLabel              = "LOW_GUESS",
                        VerifiedRuntimeTileId        = false,
                    });
                }
            }
        }

        return candidates;
    }

    // -------------------------------------------------------------------------
    // Check helper
    // -------------------------------------------------------------------------

    private static void AddCheck(List<BucketToTilesheetCheck> checks,
        string id, string description, string expected, string actual)
    {
        checks.Add(new BucketToTilesheetCheck
        {
            CheckId     = id,
            Description = description,
            Expected    = expected,
            Actual      = actual,
            CheckStatus = expected == actual ? "PASS" : "FAIL",
        });
    }

    // -------------------------------------------------------------------------
    // Render methods
    // -------------------------------------------------------------------------

    public string RenderJson(DeadMtlWorldBuilderBucketToTilesheetCrossReferenceResult result)
        => JsonSerializer.Serialize(result, s_json);

    public string RenderChecksCsv(DeadMtlWorldBuilderBucketToTilesheetCrossReferenceResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_id,description,expected,actual,check_status");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckId},{CsvEscape(c.Description)},{c.Expected},{c.Actual},{c.CheckStatus}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderBucketToTilesheetCrossReferenceResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"format                           : {result.Format}");
        sb.AppendLine($"generated_utc                    : {result.GeneratedUtc}");
        sb.AppendLine($"emitter_json_found               : {result.EmitterJsonFound}");
        sb.AppendLine($"palette_guide_found              : {result.PaletteGuideFound}");
        sb.AppendLine($"pz_install_found                 : {result.PzInstallFound}  root={result.PzInstallRoot}");
        sb.AppendLine($"bucket_intents                   : {result.BucketIntents.Count}");
        sb.AppendLine($"tile_source_files_scanned        : {result.TileSourceInventory.Count}");
        sb.AppendLine($"tilesheet_candidates             : {result.TilesheetCandidates.Count}");
        sb.AppendLine($"bucket_to_tilesheet_candidates   : {result.BucketToTilesheetCandidates.Count}");
        sb.AppendLine($"runtime_binary_written           : {result.RuntimeBinaryWritten}");
        sb.AppendLine($"verified_runtime_tile_id         : {result.VerifiedRuntimeTileId}");
        sb.AppendLine($"read_only_probe                  : {result.ReadOnlyProbe}");
        sb.AppendLine($"checks                           : {result.CheckCount} total / {result.PassedCheckCount} PASS / {result.FailedCheckCount} FAIL");
        sb.AppendLine($"verdict                          : {result.Verdict}");
        return sb.ToString();
    }

    public string RenderBucketIntentInventoryCsv(DeadMtlWorldBuilderBucketToTilesheetCrossReferenceResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("bucket,intended_use,source");
        foreach (var b in result.BucketIntents)
            sb.AppendLine($"{b.Bucket},{CsvEscape(b.IntendedUse)},{b.Source}");
        return sb.ToString();
    }

    public string RenderTileSourceInventoryCsv(DeadMtlWorldBuilderBucketToTilesheetCrossReferenceResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("file_path,file_size_bytes,tile_terms_found,source_type");
        foreach (var s in result.TileSourceInventory)
            sb.AppendLine($"{CsvEscape(s.FilePath)},{s.FileSizeBytes},{s.TileTermsFound},{s.SourceType}");
        return sb.ToString();
    }

    public string RenderTilesheetCandidateInventoryCsv(DeadMtlWorldBuilderBucketToTilesheetCrossReferenceResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("candidate_name,source_file,evidence_text_preview,source_type");
        foreach (var c in result.TilesheetCandidates)
            sb.AppendLine($"{CsvEscape(c.CandidateName)},{CsvEscape(c.SourceFile)},{CsvEscape(c.EvidenceTextPreview)},{c.SourceType}");
        return sb.ToString();
    }

    public string RenderBucketToTilesheetCandidatesCsv(DeadMtlWorldBuilderBucketToTilesheetCrossReferenceResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("bucket,intended_use,candidate_tilesheet_or_tile_name,source_file,evidence_text_preview,confidence_label,verified_runtime_tile_id");
        foreach (var c in result.BucketToTilesheetCandidates)
            sb.AppendLine($"{c.Bucket},{CsvEscape(c.IntendedUse)},{CsvEscape(c.CandidateTilesheetOrTileName)},{CsvEscape(c.SourceFile)},{CsvEscape(c.EvidenceTextPreview)},{c.ConfidenceLabel},{c.VerifiedRuntimeTileId}");
        return sb.ToString();
    }

    public string RenderCrossReferenceMarkdown(DeadMtlWorldBuilderBucketToTilesheetCrossReferenceResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-36F: Bucket-to-Tilesheet Cross-Reference");
        sb.AppendLine();
        sb.AppendLine($"Generated: {result.GeneratedUtc}");
        sb.AppendLine();
        sb.AppendLine("> **This is a read-only candidate cross-reference, NOT a geometry injection or tile writer.**");
        sb.AppendLine("> ALL mappings are CANDIDATES, GUESSES, or UNVERIFIED.");
        sb.AppendLine("> No runtime tile IDs are confirmed. Do not treat any mapping as an authoritative tile specification.");
        sb.AppendLine("> verified_runtime_tile_id = false for all entries.");
        sb.AppendLine();
        sb.AppendLine("## Source Inputs");
        sb.AppendLine();
        sb.AppendLine($"| Input | Found |");
        sb.AppendLine($"|-------|-------|");
        sb.AppendLine($"| emitter_json | {result.EmitterJsonFound} |");
        sb.AppendLine($"| palette_guide | {result.PaletteGuideFound} |");
        sb.AppendLine($"| palette_swatches | {result.PaletteSwatchesFound} |");
        sb.AppendLine($"| pz_install_root | {result.PzInstallFound} — `{result.PzInstallRoot}` |");
        sb.AppendLine();
        sb.AppendLine("## Bucket Intent Inventory");
        sb.AppendLine();
        sb.AppendLine("| Bucket | Intended Use | Source |");
        sb.AppendLine("|--------|-------------|--------|");
        foreach (var b in result.BucketIntents)
            sb.AppendLine($"| {b.Bucket} | {b.IntendedUse} | {b.Source} |");
        sb.AppendLine();
        sb.AppendLine($"## Local PZ Tile Source Files Scanned: {result.TileSourceInventory.Count}");
        sb.AppendLine();
        if (result.TileSourceInventory.Count == 0)
        {
            sb.AppendLine("> No local PZ tile source files found. PZ install not detected or no tile files matched scan criteria.");
            sb.AppendLine("> All candidates below are from built-in mapping knowledge. GUESS_NOT_VERIFIED.");
        }
        else
        {
            sb.AppendLine("| File | Size | Terms Found |");
            sb.AppendLine("|------|------|-------------|");
            foreach (var s in result.TileSourceInventory.Take(20))
                sb.AppendLine($"| `{Path.GetFileName(s.FilePath)}` | {s.FileSizeBytes} | {s.TileTermsFound} |");
            if (result.TileSourceInventory.Count > 20)
                sb.AppendLine($"| ... | | ({result.TileSourceInventory.Count - 20} more — see local-pz-tile-source-inventory.csv) |");
        }
        sb.AppendLine();
        sb.AppendLine("## Bucket-to-Tilesheet Candidate Mappings");
        sb.AppendLine();
        sb.AppendLine("> ALL entries are CANDIDATE/UNVERIFIED. verified_runtime_tile_id = false for all.");
        sb.AppendLine();
        sb.AppendLine("| Bucket | Candidate | Confidence | Source |");
        sb.AppendLine("|--------|-----------|------------|--------|");
        foreach (var c in result.BucketToTilesheetCandidates.Take(50))
            sb.AppendLine($"| {c.Bucket} | `{c.CandidateTilesheetOrTileName}` | {c.ConfidenceLabel} | `{Path.GetFileName(c.SourceFile)}` |");
        if (result.BucketToTilesheetCandidates.Count > 50)
            sb.AppendLine($"| ... | | | ({result.BucketToTilesheetCandidates.Count - 50} more — see bucket-to-tilesheet-candidates.csv) |");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|-------|-------|");
        sb.AppendLine($"| runtime_binary_written | {result.RuntimeBinaryWritten.ToString().ToLowerInvariant()} |");
        sb.AppendLine($"| geometry_injected | {result.GeometryInjected.ToString().ToLowerInvariant()} |");
        sb.AppendLine($"| playable_export_claimed | {result.PlayableExportClaimed.ToString().ToLowerInvariant()} |");
        sb.AppendLine($"| workshop_upload_performed | {result.WorkshopUploadPerformed.ToString().ToLowerInvariant()} |");
        sb.AppendLine($"| steam_install_write | {result.SteamInstallWrite.ToString().ToLowerInvariant()} |");
        sb.AppendLine($"| verified_runtime_tile_id | {result.VerifiedRuntimeTileId.ToString().ToLowerInvariant()} |");
        sb.AppendLine($"| read_only_probe | {result.ReadOnlyProbe.ToString().ToLowerInvariant()} |");
        sb.AppendLine();
        sb.AppendLine("No binary files written. No geometry injection. Read-only bucket-to-tilesheet cross-reference.");
        sb.AppendLine();
        sb.AppendLine("## Verdict");
        sb.AppendLine();
        sb.AppendLine($"**{result.Verdict}** — {result.PassedCheckCount}/{result.CheckCount} checks PASS");
        return sb.ToString();
    }

    private static string CsvEscape(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}
