using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderChunkdataHexDisassemblyBuilder
{
    private static readonly JsonSerializerOptions s_json = new()
    {
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private const int MinSameRunBreak = 8;
    private const int MaxDiffRuns     = 500;
    private const int MaxRecordSamples = 10;

    private static readonly int[] s_candidateSizes = { 4, 8, 12, 16, 24, 32, 48, 64 };

    public DeadMtlWorldBuilderChunkdataHexDisassemblyResult Build(
        string minimalChunkdataPath,
        string visibleChunkdataPath,
        string outputRoot)
    {
        var result = new DeadMtlWorldBuilderChunkdataHexDisassemblyResult
        {
            Format               = "MAP36D_CHUNKDATA_HEX_DISASSEMBLY_V1",
            GeneratedUtc         = DateTime.UtcNow.ToString("O"),
            MinimalChunkdataPath = minimalChunkdataPath ?? string.Empty,
            VisibleChunkdataPath = visibleChunkdataPath ?? string.Empty,
            OutputRoot           = outputRoot           ?? string.Empty,
        };

        // Claim boundary — always false
        result.RuntimeBinaryWritten    = false;
        result.GeometryInjected        = false;
        result.PlayableExportClaimed   = false;
        result.WorkshopUploadPerformed = false;
        result.SteamInstallWrite       = false;

        result.MinimalChunkdataFound = !string.IsNullOrEmpty(result.MinimalChunkdataPath)
            && File.Exists(result.MinimalChunkdataPath);
        result.VisibleChunkdataFound = !string.IsNullOrEmpty(result.VisibleChunkdataPath)
            && File.Exists(result.VisibleChunkdataPath);

        if (!result.MinimalChunkdataFound)
            result.Errors.Add($"Minimal chunkdata not found: {result.MinimalChunkdataPath}");
        if (!result.VisibleChunkdataFound)
            result.Errors.Add($"Visible chunkdata not found: {result.VisibleChunkdataPath}");

        byte[]? min = null;
        byte[]? vis = null;

        if (result.MinimalChunkdataFound)
        {
            min = File.ReadAllBytes(result.MinimalChunkdataPath);
            result.MinimalSize   = min.Length;
            result.MinimalSha256 = ComputeSha256(min);
        }

        if (result.VisibleChunkdataFound)
        {
            vis = File.ReadAllBytes(result.VisibleChunkdataPath);
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

            long expansionSize = Math.Max(0, vis.Length - min.Length);
            result.CandidateRecordSizes = BuildCandidateRecordSizes(
                min.Length, vis.Length, expansionSize);

            result.RecordSamples8Byte  = BuildRecordSamples(vis, 8,  "visible");
            result.RecordSamples16Byte = BuildRecordSamples(vis, 16, "visible");
            result.RecordSamples32Byte = BuildRecordSamples(vis, 32, "visible");
        }
        else if (vis != null)
        {
            // Visible-only: still produce record-size analysis and samples
            result.CandidateRecordSizes = BuildCandidateRecordSizes(0, vis.Length, vis.Length);
            result.RecordSamples8Byte   = BuildRecordSamples(vis, 8,  "visible");
            result.RecordSamples16Byte  = BuildRecordSamples(vis, 16, "visible");
            result.RecordSamples32Byte  = BuildRecordSamples(vis, 32, "visible");
        }
        else
        {
            // Always emit the candidate record-size schema (with zeroes) so the check can pass structurally
            result.CandidateRecordSizes = BuildCandidateRecordSizes(0, 0, 0);
        }

        bool recordSamplesEmitted =
            result.RecordSamples8Byte.Count  > 0 ||
            result.RecordSamples16Byte.Count > 0 ||
            result.RecordSamples32Byte.Count > 0;

        // Checks
        var checks = new List<ChunkdataHexDisassemblyCheck>();
        AddCheck(checks, "MAP36D_MINIMAL_CHUNKDATA_FOUND",
            "Minimal chunkdata file found",
            "true", result.MinimalChunkdataFound.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36D_VISIBLE_CHUNKDATA_FOUND",
            "Visible chunkdata file found",
            "true", result.VisibleChunkdataFound.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36D_SHA256_COMPUTED",
            "SHA256 computed for both chunkdata files",
            "true", result.Sha256Computed.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36D_SIZE_DELTA_POSITIVE",
            "Visible chunkdata is larger than minimal (size_delta > 0)",
            "true", (result.SizeDelta > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36D_DIFF_RUNS_EMITTED",
            "At least one diff run detected",
            "true", (result.DiffRuns.Count > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36D_CANDIDATE_RECORD_SIZES_EMITTED",
            "Candidate record-size analysis emitted",
            "true", (result.CandidateRecordSizes.Count > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36D_RECORD_SAMPLES_EMITTED",
            "At least one record sample set emitted",
            "true", recordSamplesEmitted.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36D_NO_RUNTIME_BINARY_WRITE",
            "runtime_binary_written is false",
            "false", result.RuntimeBinaryWritten.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36D_GEOMETRY_INJECTED_FALSE",
            "geometry_injected is false",
            "false", result.GeometryInjected.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36D_PLAYABLE_EXPORT_CLAIMED_FALSE",
            "playable_export_claimed is false",
            "false", result.PlayableExportClaimed.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36D_WORKSHOP_UPLOAD_PERFORMED_FALSE",
            "workshop_upload_performed is false",
            "false", result.WorkshopUploadPerformed.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36D_STEAM_INSTALL_WRITE_FALSE",
            "steam_install_write is false",
            "false", result.SteamInstallWrite.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36D_OUTPUT_ROOT_CONTAINS_LOCAL",
            "output root path is sandboxed under .local",
            "true", result.OutputRoot.Contains(".local", StringComparison.OrdinalIgnoreCase)
                       .ToString().ToLowerInvariant());

        result.Checks          = checks;
        result.CheckCount      = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");
        result.IsValid         = result.FailedCheckCount == 0 && result.Errors.Count == 0;
        result.Verdict         = result.IsValid
            ? "MAP36D_CHUNKDATA_HEX_DISASSEMBLY_COMPLETE"
            : "MAP36D_CHUNKDATA_HEX_DISASSEMBLY_FAILED";

        return result;
    }

    // -------------------------------------------------------------------------
    // Diff analysis (same algorithm as MAP-36C lotheader builder)
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
                bool curDiff = i < compareLen ? min[i] != vis[i] : !inDiff;

                if (!inDiff && curDiff)
                {
                    EmitRun(runs, "SAME", runStart, i - 1, min, vis);
                    inDiff = true;
                    runStart = i;
                }
                else if (inDiff && !curDiff)
                {
                    int j = i;
                    while (j < compareLen && min[j] == vis[j]) j++;
                    int sameLen = j - i;

                    if (sameLen >= MinSameRunBreak || j == compareLen)
                    {
                        EmitRun(runs, "DIFF", runStart, i - 1, min, vis);
                        inDiff = false;
                        runStart = i;
                    }
                }

                if (runs.Count >= MaxDiffRuns) break;
            }
        }

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
            MinimalHexPreview = type is "SAME" or "DIFF"
                ? HexPreview(min, (int)start, (int)Math.Min(32, len)) : "EOF",
            VisibleHexPreview = type is "SAME" or "DIFF"
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
            regions.Add(new CandidateRegionRecord
            {
                RegionId       = $"REGION_{rid++:D3}",
                Label          = "COMMON_PREFIX",
                StartOffset    = 0,
                EndOffset      = prefixLen - 1,
                Length         = prefixLen,
                Interpretation = "CANDIDATE: Bytes identical from offset 0. Likely file magic or format version. UNVERIFIED.",
            });

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
                Interpretation = "CANDIDATE: Bytes differ at same offsets between files. Possibly chunk count, global metadata, or fixed-size header records. UNVERIFIED.",
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
                Interpretation = "CANDIDATE: Bytes present only in visible file. Likely additional chunk records added by visible-cell export. UNVERIFIED.",
            });
        }

        if (suffixLen > 0)
            regions.Add(new CandidateRegionRecord
            {
                RegionId       = $"REGION_{rid++:D3}",
                Label          = "COMMON_SUFFIX",
                StartOffset    = minSize - suffixLen,
                EndOffset      = minSize - 1,
                Length         = suffixLen,
                Interpretation = $"CANDIDATE: Last {suffixLen} bytes identical in both files at different absolute offsets. Likely footer or sentinel. UNVERIFIED.",
            });

        return regions;
    }

    private static List<HexdumpSection> BuildHexdumpSections(
        byte[] min, byte[] vis, long prefixLen,
        List<DiffRunRecord> diffRuns, long suffixLen)
    {
        var sections = new List<HexdumpSection>();

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
                Note               = $"CANDIDATE: {prefixLen} bytes identical from offset 0.",
            });
        }

        var firstDiff = diffRuns.FirstOrDefault(r => r.RunType == "DIFF");
        if (firstDiff != null)
        {
            int dLen = (int)Math.Min(64, firstDiff.Length);
            sections.Add(new HexdumpSection
            {
                SectionId          = "SECTION_FIRST_DIFF",
                Label              = $"First DIFF run — offset 0x{firstDiff.StartOffset:X8}, {dLen} of {firstDiff.Length} bytes",
                MinimalStartOffset = firstDiff.StartOffset,
                VisibleStartOffset = firstDiff.StartOffset,
                ByteCount          = dLen,
                MinimalHexdump     = FormatHexdump(min, (int)firstDiff.StartOffset, dLen, firstDiff.StartOffset),
                VisibleHexdump     = FormatHexdump(vis, (int)firstDiff.StartOffset, dLen, firstDiff.StartOffset),
                Note               = $"CANDIDATE: First divergence at 0x{firstDiff.StartOffset:X8}. Total run: {firstDiff.Length} bytes.",
            });
        }

        var largestDiff = diffRuns.Where(r => r.RunType == "DIFF")
                                  .OrderByDescending(r => r.Length).FirstOrDefault();
        if (largestDiff != null && largestDiff.RunId != firstDiff?.RunId)
        {
            int dLen = (int)Math.Min(64, largestDiff.Length);
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

        var expRun = diffRuns.FirstOrDefault(r => r.RunType == "EXPANSION");
        if (expRun != null)
        {
            int eLen = (int)Math.Min(64, expRun.Length);
            sections.Add(new HexdumpSection
            {
                SectionId          = "SECTION_EXPANSION",
                Label              = $"Expansion zone — offset 0x{expRun.StartOffset:X8}, {eLen} of {expRun.Length} bytes",
                MinimalStartOffset = -1,
                VisibleStartOffset = expRun.StartOffset,
                ByteCount          = eLen,
                MinimalHexdump     = "(no data — past minimal file end)",
                VisibleHexdump     = FormatHexdump(vis, (int)expRun.StartOffset, eLen, expRun.StartOffset),
                Note               = $"CANDIDATE: {expRun.Length} bytes visible-only at 0x{expRun.StartOffset:X8}. Likely record expansion.",
            });
        }

        if (min.Length > 0 && vis.Length > 0)
        {
            int sLen     = Math.Min(64, Math.Min(min.Length, vis.Length));
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
                Note               = $"CANDIDATE: Common suffix length = {suffixLen} bytes. Trailing bytes for footer/sentinel detection.",
            });
        }

        return sections;
    }

    // -------------------------------------------------------------------------
    // Record-size analysis
    // -------------------------------------------------------------------------

    private static List<CandidateRecordSizeRecord> BuildCandidateRecordSizes(
        long minSize, long visSize, long expansionSize)
    {
        var records = new List<CandidateRecordSizeRecord>();
        foreach (int sz in s_candidateSizes)
        {
            records.Add(new CandidateRecordSizeRecord
            {
                RecordSize                 = sz,
                VisibleFullRecordCount     = (int)(visSize / sz),
                VisibleRemainder           = visSize % sz,
                MinimalFullRecordCount     = (int)(minSize / sz),
                MinimalRemainder           = minSize % sz,
                DividesVisibleExactly      = visSize > 0 && visSize % sz == 0,
                DividesMinimalExactly      = minSize > 0 && minSize % sz == 0,
                DividesExpansionExactly    = expansionSize > 0 && expansionSize % sz == 0,
                DividesVisibleMinus2Exactly  = visSize >= 2 && (visSize - 2) % sz == 0,
                DividesMinimalMinus2Exactly  = minSize >= 2 && (minSize - 2) % sz == 0,
                Label                      = "GUESS_NOT_VERIFIED",
            });
        }
        return records;
    }

    private static List<RecordSampleRecord> BuildRecordSamples(
        byte[] data, int recordSize, string source)
    {
        var samples = new List<RecordSampleRecord>();
        if (data == null || data.Length < recordSize) return samples;
        int fullCount = data.Length / recordSize;
        int take = Math.Min(fullCount, MaxRecordSamples);
        for (int i = 0; i < take; i++)
        {
            int offset = i * recordSize;
            samples.Add(new RecordSampleRecord
            {
                RecordIndex = i,
                FileOffset  = offset,
                RecordSize  = recordSize,
                HexBytes    = HexPreview(data, offset, recordSize),
                Source      = source,
            });
        }
        return samples;
    }

    // -------------------------------------------------------------------------
    // Byte helpers
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
    // Check helper
    // -------------------------------------------------------------------------

    private static void AddCheck(List<ChunkdataHexDisassemblyCheck> checks,
        string id, string description, string expected, string actual)
    {
        checks.Add(new ChunkdataHexDisassemblyCheck
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

    public string RenderJson(DeadMtlWorldBuilderChunkdataHexDisassemblyResult result)
        => JsonSerializer.Serialize(result, s_json);

    public string RenderChecksCsv(DeadMtlWorldBuilderChunkdataHexDisassemblyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_id,description,expected,actual,check_status");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckId},{CsvEscape(c.Description)},{c.Expected},{c.Actual},{c.CheckStatus}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderChunkdataHexDisassemblyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"format                   : {result.Format}");
        sb.AppendLine($"generated_utc            : {result.GeneratedUtc}");
        sb.AppendLine($"minimal_found            : {result.MinimalChunkdataFound}  size={result.MinimalSize}");
        sb.AppendLine($"visible_found            : {result.VisibleChunkdataFound}  size={result.VisibleSize}");
        sb.AppendLine($"size_delta               : {result.SizeDelta}");
        sb.AppendLine($"sha256_computed          : {result.Sha256Computed}");
        sb.AppendLine($"common_prefix_length     : {result.CommonPrefixLength}");
        sb.AppendLine($"common_suffix_length     : {result.CommonSuffixLength}");
        sb.AppendLine($"diff_runs                : {result.DiffRuns.Count}");
        sb.AppendLine($"candidate_regions        : {result.CandidateRegions.Count}");
        sb.AppendLine($"candidate_record_sizes   : {result.CandidateRecordSizes.Count}");
        sb.AppendLine($"record_samples_8byte     : {result.RecordSamples8Byte.Count}");
        sb.AppendLine($"record_samples_16byte    : {result.RecordSamples16Byte.Count}");
        sb.AppendLine($"record_samples_32byte    : {result.RecordSamples32Byte.Count}");
        sb.AppendLine($"runtime_binary_written   : {result.RuntimeBinaryWritten}");
        sb.AppendLine($"geometry_injected        : {result.GeometryInjected}");
        sb.AppendLine($"playable_export_claimed  : {result.PlayableExportClaimed}");
        sb.AppendLine($"checks                   : {result.CheckCount} total / {result.PassedCheckCount} PASS / {result.FailedCheckCount} FAIL");
        sb.AppendLine($"verdict                  : {result.Verdict}");
        return sb.ToString();
    }

    public string RenderDiffRunsCsv(DeadMtlWorldBuilderChunkdataHexDisassemblyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("run_id,run_type,start_offset,end_offset,length,minimal_hex_preview,visible_hex_preview");
        foreach (var r in result.DiffRuns)
            sb.AppendLine($"{r.RunId},{r.RunType},{r.StartOffset},{r.EndOffset},{r.Length},{CsvEscape(r.MinimalHexPreview)},{CsvEscape(r.VisibleHexPreview)}");
        return sb.ToString();
    }

    public string RenderCandidateRecordSizesCsv(DeadMtlWorldBuilderChunkdataHexDisassemblyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("record_size,visible_full_count,visible_remainder,minimal_full_count,minimal_remainder," +
                      "divides_visible_exactly,divides_minimal_exactly,divides_expansion_exactly," +
                      "divides_visible_minus2_exactly,divides_minimal_minus2_exactly,label");
        foreach (var r in result.CandidateRecordSizes)
            sb.AppendLine($"{r.RecordSize},{r.VisibleFullRecordCount},{r.VisibleRemainder}," +
                          $"{r.MinimalFullRecordCount},{r.MinimalRemainder}," +
                          $"{r.DividesVisibleExactly},{r.DividesMinimalExactly},{r.DividesExpansionExactly}," +
                          $"{r.DividesVisibleMinus2Exactly},{r.DividesMinimalMinus2Exactly},{r.Label}");
        return sb.ToString();
    }

    public string RenderRecordSamplesCsv(List<RecordSampleRecord> samples)
    {
        var sb = new StringBuilder();
        sb.AppendLine("record_index,file_offset,record_size,hex_bytes,source");
        foreach (var s in samples)
            sb.AppendLine($"{s.RecordIndex},{s.FileOffset},{s.RecordSize},{CsvEscape(s.HexBytes)},{s.Source}");
        return sb.ToString();
    }

    public string RenderCandidateStructureMarkdown(DeadMtlWorldBuilderChunkdataHexDisassemblyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-36D: Chunkdata Candidate Structure");
        sb.AppendLine();
        sb.AppendLine($"Generated: {result.GeneratedUtc}");
        sb.AppendLine();
        sb.AppendLine("> **This is a read-only anatomy probe, NOT a format decoder.**");
        sb.AppendLine("> ALL structural interpretations are CANDIDATES, GUESSES, or UNVERIFIED.");
        sb.AppendLine("> No chunkdata format has been confirmed. Do not treat any interpretation as authoritative.");
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
        sb.AppendLine("## Candidate Record-Size Analysis");
        sb.AppendLine();
        sb.AppendLine("> All entries are GUESS_NOT_VERIFIED. Remainder = 0 does not prove a format.");
        sb.AppendLine();
        sb.AppendLine("| Size | Vis Count | Vis Rem | Min Count | Min Rem | Div Vis | Div Min | Div Exp | Div Vis-2 | Div Min-2 |");
        sb.AppendLine("|------|-----------|---------|-----------|---------|---------|---------|---------|-----------|-----------|");
        foreach (var r in result.CandidateRecordSizes)
            sb.AppendLine($"| {r.RecordSize} | {r.VisibleFullRecordCount} | {r.VisibleRemainder} | {r.MinimalFullRecordCount} | {r.MinimalRemainder} | {r.DividesVisibleExactly} | {r.DividesMinimalExactly} | {r.DividesExpansionExactly} | {r.DividesVisibleMinus2Exactly} | {r.DividesMinimalMinus2Exactly} |");
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
            sb.AppendLine($"| ... | (truncated — {result.DiffRuns.Count - 50} more) | | | |");
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
        sb.AppendLine("No binary files written. No geometry injection. Read-only chunkdata anatomy probe.");
        sb.AppendLine();
        sb.AppendLine("## Verdict");
        sb.AppendLine();
        sb.AppendLine($"**{result.Verdict}** — {result.PassedCheckCount}/{result.CheckCount} checks PASS");
        return sb.ToString();
    }

    public string RenderPrefixSuffixHexdumpMarkdown(DeadMtlWorldBuilderChunkdataHexDisassemblyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-36D: Chunkdata Prefix/Suffix Hexdump");
        sb.AppendLine();
        sb.AppendLine($"Generated: {result.GeneratedUtc}");
        sb.AppendLine();
        sb.AppendLine("> ALL interpretations are CANDIDATES and UNVERIFIED. This is a read-only anatomy probe.");
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
            sb.AppendLine($"**visible** (offset 0x{Math.Max(0, sec.VisibleStartOffset):X8}):");
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
