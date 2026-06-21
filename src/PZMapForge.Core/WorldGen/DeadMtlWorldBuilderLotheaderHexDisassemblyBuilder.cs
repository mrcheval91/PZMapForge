using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderLotheaderHexDisassemblyBuilder
{
    private static readonly JsonSerializerOptions s_json = new()
    {
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    // Minimum SAME run length to break out of a DIFF region (suppress noise).
    private const int MinSameRunBreak = 8;
    // Maximum diff runs stored (prevents unbounded output on pathological inputs).
    private const int MaxDiffRuns = 500;

    public DeadMtlWorldBuilderLotheaderHexDisassemblyResult Build(
        string minimalLotheaderPath,
        string visibleLotheaderPath,
        string outputRoot)
    {
        var result = new DeadMtlWorldBuilderLotheaderHexDisassemblyResult
        {
            Format               = "MAP36C_LOTHEADER_HEX_DISASSEMBLY_V1",
            GeneratedUtc         = DateTime.UtcNow.ToString("O"),
            MinimalLotheaderPath = minimalLotheaderPath ?? string.Empty,
            VisibleLotheaderPath = visibleLotheaderPath ?? string.Empty,
            OutputRoot           = outputRoot           ?? string.Empty,
        };

        // Claim boundary — always false
        result.RuntimeBinaryWritten    = false;
        result.GeometryInjected        = false;
        result.PlayableExportClaimed   = false;
        result.WorkshopUploadPerformed = false;
        result.SteamInstallWrite       = false;

        result.MinimalLotheaderFound = !string.IsNullOrEmpty(result.MinimalLotheaderPath)
            && File.Exists(result.MinimalLotheaderPath);
        result.VisibleLotheaderFound = !string.IsNullOrEmpty(result.VisibleLotheaderPath)
            && File.Exists(result.VisibleLotheaderPath);

        if (!result.MinimalLotheaderFound)
            result.Errors.Add($"Minimal lotheader not found: {result.MinimalLotheaderPath}");
        if (!result.VisibleLotheaderFound)
            result.Errors.Add($"Visible lotheader not found: {result.VisibleLotheaderPath}");

        byte[]? min = null;
        byte[]? vis = null;

        if (result.MinimalLotheaderFound)
        {
            min = File.ReadAllBytes(result.MinimalLotheaderPath);
            result.MinimalSize   = min.Length;
            result.MinimalSha256 = ComputeSha256(min);
        }

        if (result.VisibleLotheaderFound)
        {
            vis = File.ReadAllBytes(result.VisibleLotheaderPath);
            result.VisibleSize   = vis.Length;
            result.VisibleSha256 = ComputeSha256(vis);
        }

        result.Sha256Computed = !string.IsNullOrEmpty(result.MinimalSha256)
                             && !string.IsNullOrEmpty(result.VisibleSha256);
        result.SizeDelta = result.VisibleSize - result.MinimalSize;

        if (min != null && vis != null)
        {
            result.CommonPrefixLength = ComputeCommonPrefixLength(min, vis);
            result.CommonSuffixLength = ComputeCommonSuffixLength(min, vis, result.CommonPrefixLength);
            result.DiffRuns           = ComputeDiffRuns(min, vis);
            result.CandidateRegions   = BuildCandidateRegions(
                min.Length, vis.Length, result.DiffRuns,
                result.CommonPrefixLength, result.CommonSuffixLength);
            result.HexdumpSections = BuildHexdumpSections(
                min, vis, result.CommonPrefixLength, result.DiffRuns, result.CommonSuffixLength);
        }

        // Checks
        var checks = new List<LotheaderHexDisassemblyCheck>();
        AddCheck(checks, "MAP36C_MINIMAL_LOTHEADER_FOUND",
            "Minimal lotheader file found",
            "true", result.MinimalLotheaderFound.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36C_VISIBLE_LOTHEADER_FOUND",
            "Visible lotheader file found",
            "true", result.VisibleLotheaderFound.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36C_SHA256_COMPUTED",
            "SHA256 computed for both lotheader files",
            "true", result.Sha256Computed.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36C_SIZE_DELTA_POSITIVE",
            "Visible lotheader is larger than minimal (size_delta > 0)",
            "true", (result.SizeDelta > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36C_DIFF_RUNS_EMITTED",
            "At least one diff run detected",
            "true", (result.DiffRuns.Count > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36C_CANDIDATE_REGIONS_EMITTED",
            "At least one candidate structural region identified",
            "true", (result.CandidateRegions.Count > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36C_NO_RUNTIME_BINARY_WRITE",
            "runtime_binary_written is false",
            "false", result.RuntimeBinaryWritten.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36C_GEOMETRY_INJECTED_FALSE",
            "geometry_injected is false",
            "false", result.GeometryInjected.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36C_PLAYABLE_EXPORT_CLAIMED_FALSE",
            "playable_export_claimed is false",
            "false", result.PlayableExportClaimed.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36C_WORKSHOP_UPLOAD_PERFORMED_FALSE",
            "workshop_upload_performed is false",
            "false", result.WorkshopUploadPerformed.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36C_STEAM_INSTALL_WRITE_FALSE",
            "steam_install_write is false",
            "false", result.SteamInstallWrite.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36C_OUTPUT_ROOT_CONTAINS_LOCAL",
            "output root path is sandboxed under .local",
            "true", result.OutputRoot.Contains(".local", StringComparison.OrdinalIgnoreCase)
                       .ToString().ToLowerInvariant());

        result.Checks          = checks;
        result.CheckCount      = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");
        result.IsValid         = result.FailedCheckCount == 0 && result.Errors.Count == 0;
        result.Verdict         = result.IsValid
            ? "MAP36C_LOTHEADER_HEX_DISASSEMBLY_COMPLETE"
            : "MAP36C_LOTHEADER_HEX_DISASSEMBLY_FAILED";

        return result;
    }

    // -------------------------------------------------------------------------
    // Analysis helpers
    // -------------------------------------------------------------------------

    private static long ComputeCommonPrefixLength(byte[] min, byte[] vis)
    {
        int compareLen = Math.Min(min.Length, vis.Length);
        for (int i = 0; i < compareLen; i++)
            if (min[i] != vis[i]) return i;
        return compareLen;
    }

    private static long ComputeCommonSuffixLength(byte[] min, byte[] vis, long prefixLen)
    {
        int minLen = min.Length, visLen = vis.Length;
        int maxSuffix = (int)(Math.Min(minLen, visLen) - prefixLen);
        if (maxSuffix <= 0) return 0;
        int suffix = 0;
        for (int i = 0; i < maxSuffix; i++)
        {
            if (min[minLen - 1 - i] != vis[visLen - 1 - i]) break;
            suffix++;
        }
        return suffix;
    }

    private static List<DiffRunRecord> ComputeDiffRuns(byte[] min, byte[] vis)
    {
        var runs = new List<DiffRunRecord>();
        int compareLen = Math.Min(min.Length, vis.Length);

        if (compareLen > 0)
        {
            bool inDiff = min[0] != vis[0];
            long runStart = 0;

            for (int i = 1; i <= compareLen; i++)
            {
                // At i == compareLen: force-close by flipping state
                bool curDiff = i < compareLen ? min[i] != vis[i] : !inDiff;

                if (!inDiff && curDiff)
                {
                    // Transitioning SAME → DIFF: close SAME run
                    EmitRun(runs, inDiff ? "DIFF" : "SAME", runStart, i - 1, min, vis);
                    inDiff = true;
                    runStart = i;
                }
                else if (inDiff && !curDiff)
                {
                    // Transitioning DIFF → SAME: peek ahead to see if SAME run is long enough
                    int j = i;
                    while (j < compareLen && min[j] == vis[j]) j++;
                    int sameLen = j - i;

                    if (sameLen >= MinSameRunBreak || j == compareLen)
                    {
                        // Close DIFF run, start SAME run
                        EmitRun(runs, "DIFF", runStart, i - 1, min, vis);
                        inDiff = false;
                        runStart = i;
                    }
                    // else: short SAME region absorbed into current DIFF run — continue
                }

                if (runs.Count >= MaxDiffRuns) break;
            }
        }

        // Expansion zone (visible bytes past minimal end)
        if (vis.Length > min.Length && runs.Count < MaxDiffRuns)
        {
            long expStart = min.Length;
            long expLen   = vis.Length - min.Length;
            runs.Add(new DiffRunRecord
            {
                RunId             = $"RUN_{runs.Count + 1:D4}",
                RunType           = "EXPANSION",
                StartOffset       = expStart,
                EndOffset         = vis.Length - 1,
                Length            = expLen,
                MinimalHexPreview = "EOF",
                VisibleHexPreview = HexPreview(vis, (int)expStart, (int)Math.Min(32, expLen)),
            });
        }
        else if (min.Length > vis.Length && runs.Count < MaxDiffRuns)
        {
            long tStart = vis.Length;
            long tLen   = min.Length - vis.Length;
            runs.Add(new DiffRunRecord
            {
                RunId             = $"RUN_{runs.Count + 1:D4}",
                RunType           = "TRUNCATION",
                StartOffset       = tStart,
                EndOffset         = min.Length - 1,
                Length            = tLen,
                MinimalHexPreview = HexPreview(min, (int)tStart, (int)Math.Min(32, tLen)),
                VisibleHexPreview = "EOF",
            });
        }

        return runs;
    }

    private static void EmitRun(List<DiffRunRecord> runs, string type,
        long start, long end, byte[] min, byte[] vis)
    {
        if (runs.Count >= MaxDiffRuns) return;
        long len = end - start + 1;
        if (len <= 0) return;
        runs.Add(new DiffRunRecord
        {
            RunId             = $"RUN_{runs.Count + 1:D4}",
            RunType           = type,
            StartOffset       = start,
            EndOffset         = end,
            Length            = len,
            MinimalHexPreview = type == "SAME" || type == "DIFF"
                ? HexPreview(min, (int)start, (int)Math.Min(32, len)) : "EOF",
            VisibleHexPreview = type == "SAME" || type == "DIFF"
                ? HexPreview(vis, (int)start, (int)Math.Min(32, len)) : "EOF",
        });
    }

    private static List<CandidateRegionRecord> BuildCandidateRegions(
        long minSize, long visSize, List<DiffRunRecord> diffRuns,
        long prefixLen, long suffixLen)
    {
        var regions = new List<CandidateRegionRecord>();
        int rid = 1;

        if (prefixLen > 0)
        {
            regions.Add(new CandidateRegionRecord
            {
                RegionId       = $"REGION_{rid++:D3}",
                Label          = "COMMON_PREFIX",
                StartOffset    = 0,
                EndOffset      = prefixLen - 1,
                Length         = prefixLen,
                Interpretation = "CANDIDATE: Bytes identical in both files from offset 0. Likely file format header, magic bytes, or global map metadata. UNVERIFIED.",
            });
        }

        var diffZoneRuns = diffRuns.Where(r => r.RunType == "DIFF").ToList();
        if (diffZoneRuns.Count > 0)
        {
            long zStart = diffZoneRuns.Min(r => r.StartOffset);
            long zEnd   = diffZoneRuns.Max(r => r.EndOffset);
            regions.Add(new CandidateRegionRecord
            {
                RegionId       = $"REGION_{rid++:D3}",
                Label          = "OVERLAP_DIFF_ZONE",
                StartOffset    = zStart,
                EndOffset      = zEnd,
                Length         = zEnd - zStart + 1,
                Interpretation = "CANDIDATE: Bytes differ at same offsets between minimal and visible files. Likely tile table or tile entry region modified by visible-cell export. UNVERIFIED.",
            });
        }

        var expRuns = diffRuns.Where(r => r.RunType == "EXPANSION").ToList();
        if (expRuns.Count > 0)
        {
            long eStart = expRuns.Min(r => r.StartOffset);
            long eEnd   = expRuns.Max(r => r.EndOffset);
            regions.Add(new CandidateRegionRecord
            {
                RegionId       = $"REGION_{rid++:D3}",
                Label          = "EXPANSION_ZONE",
                StartOffset    = eStart,
                EndOffset      = eEnd,
                Length         = eEnd - eStart + 1,
                Interpretation = "CANDIDATE: Bytes present only in visible file past minimal file end. Likely additional tile data, tile table entries, or geometry records added by visible-cell export. UNVERIFIED.",
            });
        }
        else if (diffRuns.Any(r => r.RunType == "TRUNCATION"))
        {
            var tRun = diffRuns.First(r => r.RunType == "TRUNCATION");
            regions.Add(new CandidateRegionRecord
            {
                RegionId       = $"REGION_{rid++:D3}",
                Label          = "TRUNCATION_ZONE",
                StartOffset    = tRun.StartOffset,
                EndOffset      = tRun.EndOffset,
                Length         = tRun.Length,
                Interpretation = "CANDIDATE: Bytes present only in minimal file past visible file end. Unexpected — visible file should be larger. UNVERIFIED.",
            });
        }

        if (suffixLen > 0)
        {
            regions.Add(new CandidateRegionRecord
            {
                RegionId       = $"REGION_{rid++:D3}",
                Label          = "COMMON_SUFFIX",
                StartOffset    = minSize - suffixLen,
                EndOffset      = minSize - 1,
                Length         = suffixLen,
                Interpretation = $"CANDIDATE: Last {suffixLen} bytes identical in both files (at different absolute offsets). Likely file footer, sentinel bytes, or checksum region. UNVERIFIED.",
            });
        }

        return regions;
    }

    private static List<HexdumpSection> BuildHexdumpSections(
        byte[] min, byte[] vis, long prefixLen,
        List<DiffRunRecord> diffRuns, long suffixLen)
    {
        var sections = new List<HexdumpSection>();

        // Prefix section
        if (prefixLen > 0)
        {
            int pLen = (int)Math.Min(64, prefixLen);
            sections.Add(new HexdumpSection
            {
                SectionId          = "SECTION_PREFIX",
                Label              = $"Common prefix — first {pLen} of {prefixLen} bytes",
                MinimalStartOffset = 0,
                VisibleStartOffset = 0,
                ByteCount          = pLen,
                MinimalHexdump     = FormatHexdump(min, 0, pLen, 0),
                VisibleHexdump     = FormatHexdump(vis, 0, pLen, 0),
                Note               = $"CANDIDATE: {prefixLen} bytes identical from offset 0. May be file magic, header version, or global metadata.",
            });
        }

        // First DIFF run
        var firstDiff = diffRuns.FirstOrDefault(r => r.RunType == "DIFF");
        if (firstDiff != null)
        {
            int dLen = (int)Math.Min(32, firstDiff.Length);
            sections.Add(new HexdumpSection
            {
                SectionId          = "SECTION_FIRST_DIFF",
                Label              = $"First DIFF run — offset 0x{firstDiff.StartOffset:X8}, {dLen} of {firstDiff.Length} bytes",
                MinimalStartOffset = firstDiff.StartOffset,
                VisibleStartOffset = firstDiff.StartOffset,
                ByteCount          = dLen,
                MinimalHexdump     = FormatHexdump(min, (int)firstDiff.StartOffset, dLen, firstDiff.StartOffset),
                VisibleHexdump     = FormatHexdump(vis, (int)firstDiff.StartOffset, dLen, firstDiff.StartOffset),
                Note               = $"CANDIDATE: First divergence at offset 0x{firstDiff.StartOffset:X8}. Total DIFF run: {firstDiff.Length} bytes.",
            });
        }

        // Largest DIFF run (if distinct from first)
        var largestDiff = diffRuns.Where(r => r.RunType == "DIFF")
                                  .OrderByDescending(r => r.Length)
                                  .FirstOrDefault();
        if (largestDiff != null && largestDiff.RunId != firstDiff?.RunId)
        {
            int dLen = (int)Math.Min(32, largestDiff.Length);
            sections.Add(new HexdumpSection
            {
                SectionId          = "SECTION_LARGEST_DIFF",
                Label              = $"Largest DIFF run — offset 0x{largestDiff.StartOffset:X8}, {dLen} of {largestDiff.Length} bytes",
                MinimalStartOffset = largestDiff.StartOffset,
                VisibleStartOffset = largestDiff.StartOffset,
                ByteCount          = dLen,
                MinimalHexdump     = FormatHexdump(min, (int)largestDiff.StartOffset, dLen, largestDiff.StartOffset),
                VisibleHexdump     = FormatHexdump(vis, (int)largestDiff.StartOffset, dLen, largestDiff.StartOffset),
                Note               = $"CANDIDATE: Largest diff run {largestDiff.Length} bytes at 0x{largestDiff.StartOffset:X8}.",
            });
        }

        // Expansion zone preview
        var expRun = diffRuns.FirstOrDefault(r => r.RunType == "EXPANSION");
        if (expRun != null)
        {
            int eLen = (int)Math.Min(32, expRun.Length);
            sections.Add(new HexdumpSection
            {
                SectionId          = "SECTION_EXPANSION",
                Label              = $"Expansion zone — offset 0x{expRun.StartOffset:X8}, {eLen} of {expRun.Length} bytes",
                MinimalStartOffset = -1,
                VisibleStartOffset = expRun.StartOffset,
                ByteCount          = eLen,
                MinimalHexdump     = "(no data — past minimal file end)",
                VisibleHexdump     = FormatHexdump(vis, (int)expRun.StartOffset, eLen, expRun.StartOffset),
                Note               = $"CANDIDATE: {expRun.Length} bytes visible-only starting at 0x{expRun.StartOffset:X8}. Likely geometry or tile table expansion.",
            });
        }

        // File suffix comparison (last 32 bytes of each file)
        if (min.Length > 0 && vis.Length > 0)
        {
            int sLen     = Math.Min(32, Math.Min(min.Length, vis.Length));
            int minSufOff = min.Length - sLen;
            int visSufOff = vis.Length - sLen;
            sections.Add(new HexdumpSection
            {
                SectionId          = "SECTION_SUFFIX",
                Label              = $"File suffix — last {sLen} bytes of each file",
                MinimalStartOffset = minSufOff,
                VisibleStartOffset = visSufOff,
                ByteCount          = sLen,
                MinimalHexdump     = FormatHexdump(min, minSufOff, sLen, minSufOff),
                VisibleHexdump     = FormatHexdump(vis, visSufOff, sLen, visSufOff),
                Note               = $"CANDIDATE: Common suffix length = {suffixLen} bytes. Compare trailing bytes of each file for footer/sentinel detection.",
            });
        }

        return sections;
    }

    // -------------------------------------------------------------------------
    // Low-level byte helpers
    // -------------------------------------------------------------------------

    private static string ComputeSha256(byte[] data)
    {
        byte[] hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string HexPreview(byte[] data, int offset, int len)
    {
        if (data == null || offset >= data.Length) return "EOF";
        len = Math.Min(len, data.Length - offset);
        if (len <= 0) return string.Empty;
        return BitConverter.ToString(data, offset, len).Replace("-", " ");
    }

    private static string FormatHexdump(byte[] data, int offset, int count, long fileOffset)
    {
        if (data == null || offset < 0 || offset >= data.Length) return "(no data)";
        count = Math.Min(count, data.Length - offset);
        if (count <= 0) return string.Empty;

        var sb = new StringBuilder();
        for (int i = 0; i < count; i += 16)
        {
            int rowLen = Math.Min(16, count - i);
            sb.Append($"{fileOffset + i:X8}  ");
            for (int j = 0; j < rowLen; j++)
            {
                sb.Append($"{data[offset + i + j]:X2} ");
                if (j == 7) sb.Append(' ');
            }
            for (int j = rowLen; j < 16; j++)
            {
                sb.Append("   ");
                if (j == 7) sb.Append(' ');
            }
            sb.Append(" |");
            for (int j = 0; j < rowLen; j++)
            {
                byte b = data[offset + i + j];
                sb.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
            }
            sb.AppendLine("|");
        }
        return sb.ToString();
    }

    // -------------------------------------------------------------------------
    // Render helpers
    // -------------------------------------------------------------------------

    private static void AddCheck(List<LotheaderHexDisassemblyCheck> checks,
        string id, string description, string expected, string actual)
    {
        checks.Add(new LotheaderHexDisassemblyCheck
        {
            CheckId     = id,
            Description = description,
            Expected    = expected,
            Actual      = actual,
            CheckStatus = expected == actual ? "PASS" : "FAIL",
        });
    }

    public string RenderJson(DeadMtlWorldBuilderLotheaderHexDisassemblyResult result)
        => JsonSerializer.Serialize(result, s_json);

    public string RenderChecksCsv(DeadMtlWorldBuilderLotheaderHexDisassemblyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_id,description,expected,actual,check_status");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckId},{CsvEscape(c.Description)},{c.Expected},{c.Actual},{c.CheckStatus}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderLotheaderHexDisassemblyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"format                   : {result.Format}");
        sb.AppendLine($"generated_utc            : {result.GeneratedUtc}");
        sb.AppendLine($"minimal_found            : {result.MinimalLotheaderFound}  size={result.MinimalSize}");
        sb.AppendLine($"visible_found            : {result.VisibleLotheaderFound}  size={result.VisibleSize}");
        sb.AppendLine($"size_delta               : {result.SizeDelta}");
        sb.AppendLine($"sha256_computed          : {result.Sha256Computed}");
        sb.AppendLine($"common_prefix_length     : {result.CommonPrefixLength}");
        sb.AppendLine($"common_suffix_length     : {result.CommonSuffixLength}");
        sb.AppendLine($"diff_runs                : {result.DiffRuns.Count}");
        sb.AppendLine($"candidate_regions        : {result.CandidateRegions.Count}");
        sb.AppendLine($"runtime_binary_written   : {result.RuntimeBinaryWritten}");
        sb.AppendLine($"geometry_injected        : {result.GeometryInjected}");
        sb.AppendLine($"playable_export_claimed  : {result.PlayableExportClaimed}");
        sb.AppendLine($"checks                   : {result.CheckCount} total / {result.PassedCheckCount} PASS / {result.FailedCheckCount} FAIL");
        sb.AppendLine($"verdict                  : {result.Verdict}");
        return sb.ToString();
    }

    public string RenderByteRegionsCsv(DeadMtlWorldBuilderLotheaderHexDisassemblyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("region_id,label,start_offset,end_offset,length,interpretation");
        foreach (var r in result.CandidateRegions)
            sb.AppendLine($"{r.RegionId},{r.Label},{r.StartOffset},{r.EndOffset},{r.Length},{CsvEscape(r.Interpretation)}");
        return sb.ToString();
    }

    public string RenderDiffRunsCsv(DeadMtlWorldBuilderLotheaderHexDisassemblyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("run_id,run_type,start_offset,end_offset,length,minimal_hex_preview,visible_hex_preview");
        foreach (var r in result.DiffRuns)
            sb.AppendLine($"{r.RunId},{r.RunType},{r.StartOffset},{r.EndOffset},{r.Length},{CsvEscape(r.MinimalHexPreview)},{CsvEscape(r.VisibleHexPreview)}");
        return sb.ToString();
    }

    public string RenderCandidateStructureMarkdown(DeadMtlWorldBuilderLotheaderHexDisassemblyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-36C: Lotheader Candidate Structure");
        sb.AppendLine();
        sb.AppendLine($"Generated: {result.GeneratedUtc}");
        sb.AppendLine();
        sb.AppendLine("> **ALL structural interpretations below are CANDIDATES, GUESSES, or UNVERIFIED.**");
        sb.AppendLine("> No format has been confirmed. Do not treat any field as authoritative.");
        sb.AppendLine();
        sb.AppendLine("## File Summary");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|-------|-------|");
        sb.AppendLine($"| minimal_size | {result.MinimalSize} bytes |");
        sb.AppendLine($"| visible_size | {result.VisibleSize} bytes |");
        sb.AppendLine($"| size_delta | {result.SizeDelta} bytes |");
        sb.AppendLine($"| common_prefix_length | {result.CommonPrefixLength} bytes |");
        sb.AppendLine($"| common_suffix_length | {result.CommonSuffixLength} bytes |");
        sb.AppendLine($"| diff_runs | {result.DiffRuns.Count} |");
        sb.AppendLine();
        sb.AppendLine("## Candidate Structural Regions");
        sb.AppendLine();
        sb.AppendLine("| Region | Label | Start | End | Length | Interpretation |");
        sb.AppendLine("|--------|-------|-------|-----|--------|----------------|");
        foreach (var r in result.CandidateRegions)
            sb.AppendLine($"| {r.RegionId} | {r.Label} | 0x{r.StartOffset:X8} | 0x{r.EndOffset:X8} | {r.Length} | {r.Interpretation} |");
        sb.AppendLine();
        sb.AppendLine("## Diff Run Summary");
        sb.AppendLine();
        sb.AppendLine("| Run | Type | Start | End | Length |");
        sb.AppendLine("|-----|------|-------|-----|--------|");
        foreach (var r in result.DiffRuns.Take(50))
            sb.AppendLine($"| {r.RunId} | {r.RunType} | 0x{r.StartOffset:X8} | 0x{r.EndOffset:X8} | {r.Length} |");
        if (result.DiffRuns.Count > 50)
            sb.AppendLine($"| ... | (truncated — {result.DiffRuns.Count - 50} more runs) | | | |");
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
        sb.AppendLine();
        sb.AppendLine("No binary files written. No geometry injection. Read-only hex-disassembly probe.");
        sb.AppendLine();
        sb.AppendLine("## Verdict");
        sb.AppendLine();
        sb.AppendLine($"**{result.Verdict}** — {result.PassedCheckCount}/{result.CheckCount} checks PASS");
        return sb.ToString();
    }

    public string RenderPrefixSuffixHexdumpMarkdown(DeadMtlWorldBuilderLotheaderHexDisassemblyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-36C: Lotheader Prefix/Suffix Hexdump");
        sb.AppendLine();
        sb.AppendLine($"Generated: {result.GeneratedUtc}");
        sb.AppendLine();
        sb.AppendLine("> ALL structural interpretations are CANDIDATES and UNVERIFIED.");
        sb.AppendLine();

        foreach (var sec in result.HexdumpSections)
        {
            sb.AppendLine($"## {sec.SectionId}: {sec.Label}");
            sb.AppendLine();
            sb.AppendLine($"*{sec.Note}*");
            sb.AppendLine();
            if (sec.MinimalStartOffset >= 0)
            {
                sb.AppendLine($"**minimal** (offset 0x{sec.MinimalStartOffset:X8}):");
                sb.AppendLine("```");
                sb.Append(sec.MinimalHexdump);
                sb.AppendLine("```");
            }
            sb.AppendLine($"**visible** (offset 0x{(sec.VisibleStartOffset >= 0 ? sec.VisibleStartOffset : 0):X8}):");
            sb.AppendLine("```");
            sb.Append(sec.VisibleHexdump);
            sb.AppendLine("```");
            sb.AppendLine();
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
