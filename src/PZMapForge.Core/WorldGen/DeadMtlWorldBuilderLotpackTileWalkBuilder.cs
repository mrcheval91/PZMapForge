using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderLotpackTileWalkBuilder
{
    private static readonly JsonSerializerOptions s_json = new()
    {
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private const int MinSameRunBreak    = 8;
    private const int MaxDiffRuns        = 500;
    private const int MinStringLength    = 4;
    private const int MaxStringsPerFile  = 500;
    private const int MaxOffsetSamples4K = 100;

    private static readonly int[] s_candidateSizes = { 4, 8, 12, 16, 24, 32, 48, 64, 128, 256 };

    public DeadMtlWorldBuilderLotpackTileWalkResult Build(
        string minimalLotpackPath,
        string visibleLotpackPath,
        string outputRoot)
    {
        var result = new DeadMtlWorldBuilderLotpackTileWalkResult
        {
            Format               = "MAP36E_LOTPACK_TILE_WALK_V1",
            GeneratedUtc         = DateTime.UtcNow.ToString("O"),
            MinimalLotpackPath   = minimalLotpackPath ?? string.Empty,
            VisibleLotpackPath   = visibleLotpackPath ?? string.Empty,
            OutputRoot           = outputRoot         ?? string.Empty,
        };

        result.RuntimeBinaryWritten    = false;
        result.GeometryInjected        = false;
        result.PlayableExportClaimed   = false;
        result.WorkshopUploadPerformed = false;
        result.SteamInstallWrite       = false;

        result.MinimalLotpackFound = !string.IsNullOrEmpty(result.MinimalLotpackPath)
            && File.Exists(result.MinimalLotpackPath);
        result.VisibleLotpackFound = !string.IsNullOrEmpty(result.VisibleLotpackPath)
            && File.Exists(result.VisibleLotpackPath);

        if (!result.MinimalLotpackFound)
            result.Errors.Add($"Minimal lotpack not found: {result.MinimalLotpackPath}");
        if (!result.VisibleLotpackFound)
            result.Errors.Add($"Visible lotpack not found: {result.VisibleLotpackPath}");

        byte[]? min = null;
        byte[]? vis = null;

        if (result.MinimalLotpackFound)
        {
            min = File.ReadAllBytes(result.MinimalLotpackPath);
            result.MinimalSize   = min.Length;
            result.MinimalSha256 = ComputeSha256(min);
        }

        if (result.VisibleLotpackFound)
        {
            vis = File.ReadAllBytes(result.VisibleLotpackPath);
            result.VisibleSize   = vis.Length;
            result.VisibleSha256 = ComputeSha256(vis);
        }

        result.Sha256Computed = !string.IsNullOrEmpty(result.MinimalSha256)
                             && !string.IsNullOrEmpty(result.VisibleSha256);
        result.SizeDelta = result.VisibleSize - result.MinimalSize;
        result.SizeDeltaClassification = result.SizeDelta < 0
            ? "TRUNCATION_OR_REPACK_DELTA_UNVERIFIED"
            : result.SizeDelta > 0
                ? "EXPANSION_DELTA_UNVERIFIED"
                : "NO_DELTA_UNVERIFIED";

        bool bothAvailable = min != null && vis != null;

        if (bothAvailable)
        {
            result.CommonPrefixLength = ComputeCommonPrefixLength(min!, vis!);
            result.CommonSuffixLength = ComputeCommonSuffixLength(min!, vis!, result.CommonPrefixLength);
            result.DiffRuns           = ComputeDiffRuns(min!, vis!);
            result.CandidateRegions   = BuildCandidateRegions(
                min!.Length, vis!.Length, result.DiffRuns,
                result.CommonPrefixLength, result.CommonSuffixLength);
            result.HexdumpSections = BuildHexdumpSections(
                min!, vis!, result.CommonPrefixLength, result.DiffRuns,
                result.CommonSuffixLength, result.SizeDelta);
        }
        else if (vis != null)
        {
            result.DiffRuns = new List<DiffRunRecord>();
        }
        else
        {
            result.DiffRuns = new List<DiffRunRecord>();
        }

        long absDelta = Math.Abs(result.SizeDelta);
        result.CandidateRecordSizes = BuildCandidateRecordSizes(
            result.MinimalSize, result.VisibleSize, absDelta);

        if (min != null)
            result.StringTable.AddRange(ExtractStrings(min, "minimal"));
        if (vis != null)
            result.StringTable.AddRange(ExtractStrings(vis, "visible"));

        result.ByteFrequency = BuildByteFrequency(min, vis);

        result.OffsetSamples = BuildOffsetSamples(min, vis);

        // Checks
        var checks = new List<LotpackTileWalkCheck>();
        AddCheck(checks, "MAP36E_MINIMAL_LOTPACK_FOUND",
            "Minimal lotpack file found",
            "true", result.MinimalLotpackFound.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36E_VISIBLE_LOTPACK_FOUND",
            "Visible lotpack file found",
            "true", result.VisibleLotpackFound.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36E_SHA256_COMPUTED",
            "SHA256 computed for both lotpack files",
            "true", result.Sha256Computed.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36E_SIZE_DELTA_CAPTURED",
            "Size delta computed (both files found)",
            "true", (result.MinimalLotpackFound && result.VisibleLotpackFound)
                       .ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36E_VISIBLE_SMALLER_THAN_MINIMAL_CAPTURED",
            "Visible lotpack is smaller than minimal (size_delta < 0)",
            "true", (result.SizeDelta < 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36E_DIFF_RUNS_EMITTED",
            "At least one diff run detected",
            "true", (result.DiffRuns.Count > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36E_CANDIDATE_RECORD_SIZES_EMITTED",
            "Candidate record-size analysis emitted",
            "true", (result.CandidateRecordSizes.Count > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36E_STRING_TABLE_EMITTED",
            "At least one printable ASCII string extracted",
            "true", (result.StringTable.Count > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36E_BYTE_FREQUENCY_EMITTED",
            "Byte frequency table emitted",
            "true", (result.ByteFrequency.Count > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36E_OFFSET_SAMPLES_EMITTED",
            "At least one offset sample emitted",
            "true", (result.OffsetSamples.Count > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36E_NO_RUNTIME_BINARY_WRITE",
            "runtime_binary_written is false",
            "false", result.RuntimeBinaryWritten.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36E_GEOMETRY_INJECTED_FALSE",
            "geometry_injected is false",
            "false", result.GeometryInjected.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36E_PLAYABLE_EXPORT_CLAIMED_FALSE",
            "playable_export_claimed is false",
            "false", result.PlayableExportClaimed.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36E_WORKSHOP_UPLOAD_PERFORMED_FALSE",
            "workshop_upload_performed is false",
            "false", result.WorkshopUploadPerformed.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36E_STEAM_INSTALL_WRITE_FALSE",
            "steam_install_write is false",
            "false", result.SteamInstallWrite.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36E_OUTPUT_ROOT_CONTAINS_LOCAL",
            "output root path is sandboxed under .local",
            "true", result.OutputRoot.Contains(".local", StringComparison.OrdinalIgnoreCase)
                       .ToString().ToLowerInvariant());

        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");
        result.IsValid          = result.FailedCheckCount == 0 && result.Errors.Count == 0;
        result.Verdict          = result.IsValid
            ? "MAP36E_LOTPACK_TILE_WALK_COMPLETE"
            : "MAP36E_LOTPACK_TILE_WALK_FAILED";

        return result;
    }

    // -------------------------------------------------------------------------
    // Diff analysis
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
        int maxSuffix = (int)(Math.Min(min.Length, vis.Length) - prefixLen);
        if (maxSuffix <= 0) return 0;
        int suffix = 0;
        for (int i = 0; i < maxSuffix; i++)
        {
            if (min[min.Length - 1 - i] != vis[vis.Length - 1 - i]) break;
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
            bool inDiff  = min[0] != vis[0];
            long runStart = 0;

            for (int i = 1; i <= compareLen; i++)
            {
                bool curDiff = i < compareLen ? min[i] != vis[i] : !inDiff;

                if (!inDiff && curDiff)
                {
                    EmitRun(runs, "SAME", runStart, i - 1, min, vis);
                    inDiff   = true;
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
                        inDiff   = false;
                        runStart = i;
                    }
                }

                if (runs.Count >= MaxDiffRuns) break;
            }
        }

        if (vis.Length > min.Length && runs.Count < MaxDiffRuns)
        {
            long eStart = min.Length;
            long eLen   = vis.Length - min.Length;
            runs.Add(new DiffRunRecord
            {
                RunId             = $"RUN_{runs.Count + 1:D4}",
                RunType           = "EXPANSION",
                StartOffset       = eStart,
                EndOffset         = vis.Length - 1,
                Length            = eLen,
                MinimalHexPreview = "EOF",
                VisibleHexPreview = HexPreview(vis, (int)eStart, (int)Math.Min(32, eLen)),
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
                Interpretation = "CANDIDATE: Bytes identical from offset 0. Likely file magic, format version, or fixed header. UNVERIFIED.",
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
                Interpretation = "CANDIDATE: Bytes differ at same offsets. Possibly tile index, count fields, or record values changed. UNVERIFIED.",
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
                Interpretation = "CANDIDATE: Bytes present only in visible file. Visible lotpack is larger. UNVERIFIED.",
            });
        }

        var truncRuns = diffRuns.Where(r => r.RunType == "TRUNCATION").ToList();
        if (truncRuns.Count > 0)
        {
            long tStart = truncRuns.Min(r => r.StartOffset);
            long tEnd   = truncRuns.Max(r => r.EndOffset);
            regions.Add(new CandidateRegionRecord
            {
                RegionId       = $"REGION_{rid++:D3}",
                Label          = "TRUNCATION_ZONE",
                StartOffset    = tStart,
                EndOffset      = tEnd,
                Length         = tEnd - tStart + 1,
                Interpretation = "CANDIDATE: Bytes present only in minimal file (visible is smaller). May indicate repack or removed padding. TRUNCATION_OR_REPACK_DELTA_UNVERIFIED.",
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
        List<DiffRunRecord> diffRuns, long suffixLen, long sizeDelta)
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

        // Truncation or expansion boundary
        var truncRun = diffRuns.FirstOrDefault(r => r.RunType == "TRUNCATION");
        if (truncRun != null)
        {
            int tLen = (int)Math.Min(64, truncRun.Length);
            sections.Add(new HexdumpSection
            {
                SectionId          = "SECTION_TRUNCATION",
                Label              = $"Truncation boundary — offset 0x{truncRun.StartOffset:X8}, {tLen} of {truncRun.Length} bytes",
                MinimalStartOffset = truncRun.StartOffset,
                VisibleStartOffset = -1,
                ByteCount          = tLen,
                MinimalHexdump     = FormatHexdump(min, (int)truncRun.StartOffset, tLen, truncRun.StartOffset),
                VisibleHexdump     = "(no data — past visible file end)",
                Note               = $"CANDIDATE: {truncRun.Length} bytes present only in minimal file at 0x{truncRun.StartOffset:X8}. TRUNCATION_OR_REPACK_DELTA_UNVERIFIED.",
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
                Note               = $"CANDIDATE: {expRun.Length} bytes visible-only at 0x{expRun.StartOffset:X8}.",
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

    private static List<LotpackCandidateRecordSizeRecord> BuildCandidateRecordSizes(
        long minSize, long visSize, long absDelta)
    {
        var records = new List<LotpackCandidateRecordSizeRecord>();
        foreach (int sz in s_candidateSizes)
        {
            records.Add(new LotpackCandidateRecordSizeRecord
            {
                RecordSize              = sz,
                VisibleFullRecordCount  = visSize > 0 ? (int)(visSize / sz) : 0,
                VisibleRemainder        = visSize > 0 ? visSize % sz : 0,
                MinimalFullRecordCount  = minSize > 0 ? (int)(minSize / sz) : 0,
                MinimalRemainder        = minSize > 0 ? minSize % sz : 0,
                DividesVisibleExactly   = visSize > 0 && visSize % sz == 0,
                DividesMinimalExactly   = minSize > 0 && minSize % sz == 0,
                DividesDeltaExactly     = absDelta > 0 && absDelta % sz == 0,
                Label                   = "GUESS_NOT_VERIFIED",
            });
        }
        return records;
    }

    // -------------------------------------------------------------------------
    // String extraction
    // -------------------------------------------------------------------------

    private static List<LotpackStringTableRecord> ExtractStrings(byte[] data, string source)
    {
        var results = new List<LotpackStringTableRecord>();
        int i = 0;
        while (i < data.Length && results.Count < MaxStringsPerFile)
        {
            if (data[i] >= 0x20 && data[i] <= 0x7E)
            {
                int start = i;
                while (i < data.Length && data[i] >= 0x20 && data[i] <= 0x7E) i++;
                int len = i - start;
                if (len >= MinStringLength)
                {
                    results.Add(new LotpackStringTableRecord
                    {
                        Offset  = start,
                        Length  = len,
                        Source  = source,
                        Preview = Encoding.ASCII.GetString(data, start, Math.Min(80, len)),
                    });
                }
            }
            else i++;
        }
        return results;
    }

    // -------------------------------------------------------------------------
    // Byte frequency
    // -------------------------------------------------------------------------

    private static List<LotpackByteFrequencyRecord> BuildByteFrequency(byte[]? min, byte[]? vis)
    {
        long[] minCounts = new long[256];
        long[] visCounts = new long[256];
        if (min != null) foreach (byte b in min) minCounts[b]++;
        if (vis != null) foreach (byte b in vis) visCounts[b]++;
        long minLen = min?.Length ?? 0;
        long visLen = vis?.Length ?? 0;
        var results = new List<LotpackByteFrequencyRecord>(256);
        for (int i = 0; i < 256; i++)
            results.Add(new LotpackByteFrequencyRecord
            {
                ByteValue        = i,
                HexValue         = i.ToString("X2"),
                MinimalCount     = minCounts[i],
                MinimalFrequency = minLen > 0 ? (double)minCounts[i] / minLen : 0,
                VisibleCount     = visCounts[i],
                VisibleFrequency = visLen > 0 ? (double)visCounts[i] / visLen : 0,
            });
        return results;
    }

    // -------------------------------------------------------------------------
    // Offset samples
    // -------------------------------------------------------------------------

    private static List<LotpackOffsetSampleRecord> BuildOffsetSamples(byte[]? min, byte[]? vis)
    {
        var samples = new List<LotpackOffsetSampleRecord>();

        // Always include offset-0 anchor for both files
        if (min != null)
            samples.Add(MakeSample(min, 0, 0, "minimal"));
        if (vis != null)
            samples.Add(MakeSample(vis, 0, 0, "visible"));

        // 4096-interval samples from visible (cap at 100 to avoid flooding)
        if (vis != null)
        {
            int count = 0;
            for (int offset = 4096; offset < vis.Length && count < MaxOffsetSamples4K; offset += 4096, count++)
                samples.Add(MakeSample(vis, offset, 4096, "visible"));
        }

        // 65536-interval samples from visible
        if (vis != null)
            for (int offset = 65536; offset < vis.Length; offset += 65536)
                samples.Add(MakeSample(vis, offset, 65536, "visible"));

        // 65536-interval samples from minimal
        if (min != null)
            for (int offset = 65536; offset < min.Length; offset += 65536)
                samples.Add(MakeSample(min, offset, 65536, "minimal"));

        return samples;
    }

    private static LotpackOffsetSampleRecord MakeSample(byte[] data, int offset, int interval, string source)
        => new()
        {
            SampleOffset  = offset,
            IntervalBytes = interval,
            Source        = source,
            HexPreview    = HexPreview(data, offset, Math.Min(32, data.Length - offset)),
        };

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

    private static void AddCheck(List<LotpackTileWalkCheck> checks,
        string id, string description, string expected, string actual)
    {
        checks.Add(new LotpackTileWalkCheck
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

    public string RenderJson(DeadMtlWorldBuilderLotpackTileWalkResult result)
        => JsonSerializer.Serialize(result, s_json);

    public string RenderChecksCsv(DeadMtlWorldBuilderLotpackTileWalkResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_id,description,expected,actual,check_status");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckId},{CsvEscape(c.Description)},{c.Expected},{c.Actual},{c.CheckStatus}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderLotpackTileWalkResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"format                       : {result.Format}");
        sb.AppendLine($"generated_utc                : {result.GeneratedUtc}");
        sb.AppendLine($"minimal_found                : {result.MinimalLotpackFound}  size={result.MinimalSize}");
        sb.AppendLine($"visible_found                : {result.VisibleLotpackFound}  size={result.VisibleSize}");
        sb.AppendLine($"size_delta                   : {result.SizeDelta}");
        sb.AppendLine($"size_delta_classification    : {result.SizeDeltaClassification}");
        sb.AppendLine($"sha256_computed              : {result.Sha256Computed}");
        sb.AppendLine($"common_prefix_length         : {result.CommonPrefixLength}");
        sb.AppendLine($"common_suffix_length         : {result.CommonSuffixLength}");
        sb.AppendLine($"diff_runs                    : {result.DiffRuns.Count}");
        sb.AppendLine($"candidate_regions            : {result.CandidateRegions.Count}");
        sb.AppendLine($"candidate_record_sizes       : {result.CandidateRecordSizes.Count}");
        sb.AppendLine($"string_table_entries         : {result.StringTable.Count}");
        sb.AppendLine($"byte_frequency_entries       : {result.ByteFrequency.Count}");
        sb.AppendLine($"offset_samples               : {result.OffsetSamples.Count}");
        sb.AppendLine($"runtime_binary_written       : {result.RuntimeBinaryWritten}");
        sb.AppendLine($"geometry_injected            : {result.GeometryInjected}");
        sb.AppendLine($"playable_export_claimed      : {result.PlayableExportClaimed}");
        sb.AppendLine($"checks                       : {result.CheckCount} total / {result.PassedCheckCount} PASS / {result.FailedCheckCount} FAIL");
        sb.AppendLine($"verdict                      : {result.Verdict}");
        return sb.ToString();
    }

    public string RenderDiffRunsCsv(DeadMtlWorldBuilderLotpackTileWalkResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("run_id,run_type,start_offset,end_offset,length,minimal_hex_preview,visible_hex_preview");
        foreach (var r in result.DiffRuns)
            sb.AppendLine($"{r.RunId},{r.RunType},{r.StartOffset},{r.EndOffset},{r.Length},{CsvEscape(r.MinimalHexPreview)},{CsvEscape(r.VisibleHexPreview)}");
        return sb.ToString();
    }

    public string RenderCandidateRecordSizesCsv(DeadMtlWorldBuilderLotpackTileWalkResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("record_size,visible_full_count,visible_remainder,minimal_full_count,minimal_remainder," +
                      "divides_visible_exactly,divides_minimal_exactly,divides_delta_exactly,label");
        foreach (var r in result.CandidateRecordSizes)
            sb.AppendLine($"{r.RecordSize},{r.VisibleFullRecordCount},{r.VisibleRemainder}," +
                          $"{r.MinimalFullRecordCount},{r.MinimalRemainder}," +
                          $"{r.DividesVisibleExactly},{r.DividesMinimalExactly},{r.DividesDeltaExactly},{r.Label}");
        return sb.ToString();
    }

    public string RenderStringTableCsv(DeadMtlWorldBuilderLotpackTileWalkResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("offset,length,source,preview");
        foreach (var s in result.StringTable)
            sb.AppendLine($"{s.Offset},{s.Length},{s.Source},{CsvEscape(s.Preview)}");
        return sb.ToString();
    }

    public string RenderByteFrequencyCsv(DeadMtlWorldBuilderLotpackTileWalkResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("byte_value,hex_value,minimal_count,minimal_frequency,visible_count,visible_frequency");
        foreach (var f in result.ByteFrequency)
            sb.AppendLine($"{f.ByteValue},{f.HexValue},{f.MinimalCount},{f.MinimalFrequency:F6},{f.VisibleCount},{f.VisibleFrequency:F6}");
        return sb.ToString();
    }

    public string RenderOffsetSampleCsv(DeadMtlWorldBuilderLotpackTileWalkResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("sample_offset,interval_bytes,source,hex_preview");
        foreach (var s in result.OffsetSamples)
            sb.AppendLine($"{s.SampleOffset},{s.IntervalBytes},{s.Source},{CsvEscape(s.HexPreview)}");
        return sb.ToString();
    }

    public string RenderCandidateStructureMarkdown(DeadMtlWorldBuilderLotpackTileWalkResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-36E: Lotpack Candidate Structure");
        sb.AppendLine();
        sb.AppendLine($"Generated: {result.GeneratedUtc}");
        sb.AppendLine();
        sb.AppendLine("> **This is a read-only anatomy probe, NOT a format decoder.**");
        sb.AppendLine("> ALL structural interpretations are CANDIDATES, GUESSES, or UNVERIFIED.");
        sb.AppendLine("> No lotpack format has been confirmed. Do not treat any interpretation as authoritative.");
        sb.AppendLine();
        sb.AppendLine("## File Summary");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|-------|-------|");
        sb.AppendLine($"| minimal_size | {result.MinimalSize} bytes |");
        sb.AppendLine($"| visible_size | {result.VisibleSize} bytes |");
        sb.AppendLine($"| size_delta | {result.SizeDelta} bytes |");
        sb.AppendLine($"| size_delta_classification | {result.SizeDeltaClassification} |");
        sb.AppendLine($"| common_prefix_length | {result.CommonPrefixLength} bytes |");
        sb.AppendLine($"| common_suffix_length | {result.CommonSuffixLength} bytes |");
        sb.AppendLine($"| diff_runs | {result.DiffRuns.Count} |");
        sb.AppendLine($"| string_table_entries | {result.StringTable.Count} |");
        sb.AppendLine($"| byte_frequency_entries | {result.ByteFrequency.Count} |");
        sb.AppendLine($"| offset_samples | {result.OffsetSamples.Count} |");
        sb.AppendLine();
        sb.AppendLine("## Notable: Visible Lotpack is Smaller Than Minimal");
        sb.AppendLine();
        sb.AppendLine($"> size_delta = {result.SizeDelta} bytes. This is classified as {result.SizeDeltaClassification}.");
        sb.AppendLine("> Unlike lotheader/chunkdata, the visible lotpack did NOT expand relative to the minimal seed.");
        sb.AppendLine("> This may indicate: padding removal, record compaction, or different generation path. UNVERIFIED.");
        sb.AppendLine();
        sb.AppendLine("## Candidate Record-Size Analysis");
        sb.AppendLine();
        sb.AppendLine("> All entries are GUESS_NOT_VERIFIED. Remainder = 0 does not prove a format.");
        sb.AppendLine("> divides_delta_exactly uses abs(size_delta) = " + Math.Abs(result.SizeDelta) + " bytes.");
        sb.AppendLine();
        sb.AppendLine("| Size | Vis Count | Vis Rem | Min Count | Min Rem | Div Vis | Div Min | Div Delta |");
        sb.AppendLine("|------|-----------|---------|-----------|---------|---------|---------|-----------|");
        foreach (var r in result.CandidateRecordSizes)
            sb.AppendLine($"| {r.RecordSize} | {r.VisibleFullRecordCount} | {r.VisibleRemainder} | {r.MinimalFullRecordCount} | {r.MinimalRemainder} | {r.DividesVisibleExactly} | {r.DividesMinimalExactly} | {r.DividesDeltaExactly} |");
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
        sb.AppendLine("## String Table Preview (first 20 entries)");
        sb.AppendLine();
        sb.AppendLine("| Offset | Len | Source | Preview |");
        sb.AppendLine("|--------|-----|--------|---------|");
        foreach (var s in result.StringTable.Take(20))
            sb.AppendLine($"| 0x{s.Offset:X8} | {s.Length} | {s.Source} | `{s.Preview.Replace("`", "'")}` |");
        if (result.StringTable.Count > 20)
            sb.AppendLine($"| ... | | | ({result.StringTable.Count - 20} more — see string-table.csv) |");
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
        sb.AppendLine("No binary files written. No geometry injection. Read-only lotpack anatomy probe.");
        sb.AppendLine();
        sb.AppendLine("## Verdict");
        sb.AppendLine();
        sb.AppendLine($"**{result.Verdict}** — {result.PassedCheckCount}/{result.CheckCount} checks PASS");
        return sb.ToString();
    }

    public string RenderPrefixSuffixHexdumpMarkdown(DeadMtlWorldBuilderLotpackTileWalkResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-36E: Lotpack Prefix/Suffix Hexdump");
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
            else
            {
                sb.AppendLine($"**minimal**: {sec.MinimalHexdump}");
                sb.AppendLine();
            }
            sb.AppendLine($"**visible** (offset 0x{Math.Max(0, sec.VisibleStartOffset):X8}):");
            if (sec.VisibleStartOffset < 0)
                sb.AppendLine(sec.VisibleHexdump);
            else
            {
                sb.AppendLine("```");
                sb.Append(sec.VisibleHexdump);
                sb.AppendLine("```");
            }
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
