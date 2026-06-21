using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderChunkdataRecordClusterAuditBuilder
{
    private static readonly JsonSerializerOptions s_json = new()
    {
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private static readonly int[] s_headerSizes = { 0, 1, 2, 4, 8, 16, 18 };
    private static readonly int[] s_recordWidths = { 4, 8, 16, 32, 64 };

    private const int FocusedHeader = 2;
    private const int FocusedWidth  = 8;
    private const int MaxSampleFirst = 64;
    private const int MaxSampleLast  = 64;
    private const int MaxTopRepeated = 50;

    public DeadMtlWorldBuilderChunkdataRecordClusterAuditResult Build(
        string minimalChunkdataPath,
        string visibleChunkdataPath,
        string outputRoot)
    {
        var result = new DeadMtlWorldBuilderChunkdataRecordClusterAuditResult
        {
            Format               = "MAP37A_CHUNKDATA_RECORD_CLUSTER_AUDIT_V1",
            GeneratedUtc         = DateTime.UtcNow.ToString("O"),
            MinimalChunkdataPath = minimalChunkdataPath ?? string.Empty,
            VisibleChunkdataPath = visibleChunkdataPath ?? string.Empty,
            OutputRoot           = outputRoot           ?? string.Empty,
        };

        result.RuntimeBinaryWritten    = false;
        result.GeometryInjected        = false;
        result.PlayableExportClaimed   = false;
        result.WorkshopUploadPerformed = false;
        result.SteamInstallWrite       = false;
        result.VerifiedChunkdataFormat = false;
        result.ReadOnlyProbe           = true;

        result.MinimalFound = !string.IsNullOrEmpty(result.MinimalChunkdataPath)
            && File.Exists(result.MinimalChunkdataPath);
        result.VisibleFound = !string.IsNullOrEmpty(result.VisibleChunkdataPath)
            && File.Exists(result.VisibleChunkdataPath);

        if (!result.MinimalFound)
            result.Errors.Add($"Minimal chunkdata not found: {result.MinimalChunkdataPath}");
        if (!result.VisibleFound)
            result.Errors.Add($"Visible chunkdata not found: {result.VisibleChunkdataPath}");

        byte[]? min = null;
        byte[]? vis = null;

        if (result.MinimalFound)
        {
            min = File.ReadAllBytes(result.MinimalChunkdataPath);
            result.MinimalSize   = min.Length;
            result.MinimalSha256 = ComputeSha256(min);
        }
        if (result.VisibleFound)
        {
            vis = File.ReadAllBytes(result.VisibleChunkdataPath);
            result.VisibleSize   = vis.Length;
            result.VisibleSha256 = ComputeSha256(vis);
        }

        result.SizeDelta = result.VisibleSize - result.MinimalSize;

        // A) Header-size candidate matrix
        result.CandidateHeaderSizes = BuildHeaderSizeCandidates(result.MinimalSize, result.VisibleSize, result.SizeDelta);

        // B+D) Width analyses (all combos) — focused on header=2 width=8 for deep stats
        result.RecordWidthAnalyses = new List<RecordWidthAnalysisRecord>();

        if (vis != null)
        {
            foreach (int h in s_headerSizes)
            foreach (int w in s_recordWidths)
                result.RecordWidthAnalyses.Add(BuildWidthAnalysis(vis, h, w, "visible"));
        }
        if (min != null)
        {
            foreach (int h in s_headerSizes)
            foreach (int w in s_recordWidths)
                result.RecordWidthAnalyses.Add(BuildWidthAnalysis(min, h, w, "minimal"));
        }

        // B) Focused header=2 width=8 column statistics, repeated/zero clusters, samples
        if (vis != null)
        {
            result.RecordColumnStatistics.AddRange(BuildColumnStats(vis, FocusedHeader, FocusedWidth, "visible"));
            result.RepeatedRecordClusters.AddRange(BuildRepeatedClusters(vis, FocusedHeader, FocusedWidth, "visible"));
            result.ZeroRecordClusters.AddRange(BuildZeroClusters(vis, FocusedHeader, FocusedWidth, "visible"));
            result.BytePatternClusters.AddRange(BuildBytePatternClusters(vis, FocusedHeader, FocusedWidth, "visible"));
            result.VisibleRecordSamples.AddRange(BuildRecordSamples(vis, FocusedHeader, FocusedWidth, "visible"));
        }
        if (min != null)
        {
            result.RecordColumnStatistics.AddRange(BuildColumnStats(min, FocusedHeader, FocusedWidth, "minimal"));
            result.RepeatedRecordClusters.AddRange(BuildRepeatedClusters(min, FocusedHeader, FocusedWidth, "minimal"));
            result.ZeroRecordClusters.AddRange(BuildZeroClusters(min, FocusedHeader, FocusedWidth, "minimal"));
            result.BytePatternClusters.AddRange(BuildBytePatternClusters(min, FocusedHeader, FocusedWidth, "minimal"));
            result.MinimalRecordSamples.AddRange(BuildRecordSamples(min, FocusedHeader, FocusedWidth, "minimal"));
        }

        // C) Changed record windows (header=2 width=8)
        if (min != null && vis != null)
            result.ChangedRecordWindows = BuildChangedRecordWindows(min, vis, FocusedHeader, FocusedWidth);
        else if (vis != null)
            result.ChangedRecordWindows = BuildVisibleOnlyWindows(vis, FocusedHeader, FocusedWidth);

        // E) Hypothesis summary
        result.HypothesisSummary = new List<string>
        {
            "2-byte header + 8-byte records is a strong size-fit hypothesis, not a verified format.",
            "No runtime tile IDs are decoded.",
            "No geometry is injected.",
            "MAP-37A does not create playable binary output.",
            $"minimal_size={result.MinimalSize}: 2 + 128 * 8 = {2 + 128 * 8} (exact fit: {result.MinimalSize == 2 + 128 * 8})",
            $"visible_size={result.VisibleSize}: 2 + 2272 * 8 = {2 + 2272 * 8} (exact fit: {result.VisibleSize == 2 + 2272 * 8})",
            $"size_delta={result.SizeDelta}: 2144 * 8 = {2144 * 8} (exact fit: {result.SizeDelta == 2144 * 8})",
            "visible_extra_records = 2272 - 128 = 2144 (CANDIDATE_RECORD_WINDOW_UNVERIFIED)",
        };

        // Checks
        var checks = new List<RecordClusterAuditCheck>();

        long minRecordCount2x8  = min != null && min.Length >= 2 ? (min.Length - 2) / 8 : 0;
        long visRecordCount2x8  = vis != null && vis.Length >= 2 ? (vis.Length - 2) / 8 : 0;
        long minRemainder2x8    = min != null && min.Length >= 2 ? (min.Length - 2) % 8 : -1;
        long visRemainder2x8    = vis != null && vis.Length >= 2 ? (vis.Length - 2) % 8 : -1;
        long expectedVisExtra   = visRecordCount2x8 - minRecordCount2x8;
        bool hasVisibleExtraWindow = result.ChangedRecordWindows.Any(w => w.WindowType == "VISIBLE_EXTRA");

        AddCheck(checks, "MAP37A_MINIMAL_CHUNKDATA_FOUND",
            "Minimal chunkdata file found", "true", result.MinimalFound.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP37A_VISIBLE_CHUNKDATA_FOUND",
            "Visible chunkdata file found", "true", result.VisibleFound.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP37A_HEADER_SIZE_CANDIDATES_EMITTED",
            "Header-size candidate matrix emitted",
            "true", (result.CandidateHeaderSizes.Count > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP37A_RECORD_WIDTH_ANALYSES_EMITTED",
            "Record-width analyses emitted",
            "true", (result.RecordWidthAnalyses.Count > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP37A_COLUMN_STATISTICS_EMITTED",
            "Column statistics emitted for header=2 width=8",
            "true", (result.RecordColumnStatistics.Count > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP37A_CHANGED_RECORD_WINDOWS_EMITTED",
            "Changed record windows emitted",
            "true", (result.ChangedRecordWindows.Count > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP37A_VISIBLE_EXTRA_WINDOW_EMITTED",
            "VISIBLE_EXTRA window present in changed record windows",
            "true", hasVisibleExtraWindow.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP37A_NO_RUNTIME_BINARY_WRITE",
            "runtime_binary_written is false",
            "false", result.RuntimeBinaryWritten.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP37A_GEOMETRY_INJECTED_FALSE",
            "geometry_injected is false",
            "false", result.GeometryInjected.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP37A_PLAYABLE_EXPORT_CLAIMED_FALSE",
            "playable_export_claimed is false",
            "false", result.PlayableExportClaimed.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP37A_VERIFIED_CHUNKDATA_FORMAT_FALSE",
            "verified_chunkdata_format is false",
            "false", result.VerifiedChunkdataFormat.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP37A_READ_ONLY_PROBE_TRUE",
            "read_only_probe is true",
            "true", result.ReadOnlyProbe.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP37A_OUTPUT_ROOT_CONTAINS_LOCAL",
            "output root path is sandboxed under .local",
            "true", result.OutputRoot.Contains(".local", StringComparison.OrdinalIgnoreCase)
                       .ToString().ToLowerInvariant());

        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");
        result.IsValid          = result.FailedCheckCount == 0 && result.Errors.Count == 0;
        result.Verdict          = result.IsValid
            ? "MAP37A_CHUNKDATA_RECORD_CLUSTER_AUDIT_COMPLETE"
            : "MAP37A_CHUNKDATA_RECORD_CLUSTER_AUDIT_FAILED";

        return result;
    }

    // -------------------------------------------------------------------------
    // A) Header-size candidate matrix
    // -------------------------------------------------------------------------

    private static List<RecordClusterHeaderSizeRecord> BuildHeaderSizeCandidates(
        long minSize, long visSize, long delta)
    {
        var records = new List<RecordClusterHeaderSizeRecord>();
        foreach (int h in s_headerSizes)
        foreach (int w in s_recordWidths)
        {
            long minPay  = Math.Max(0, minSize - h);
            long visPay  = Math.Max(0, visSize - h);
            long minFull = minPay / w;
            long visFull = visPay / w;
            long minRem  = minPay % w;
            long visRem  = visPay % w;
            long deltaPay = visPay - minPay;
            long deltaFull = w > 0 && deltaPay % w == 0 ? deltaPay / w : -1;
            int score = 0;
            if (minRem == 0) score++;
            if (visRem == 0) score++;
            if (deltaPay > 0 && deltaPay % w == 0) score++;
            records.Add(new RecordClusterHeaderSizeRecord
            {
                HeaderSize             = h,
                RecordWidth            = w,
                MinimalPayloadSize     = minPay,
                VisiblePayloadSize     = visPay,
                MinimalFullRecordCount = minFull,
                VisibleFullRecordCount = visFull,
                MinimalRemainder       = minRem,
                VisibleRemainder       = visRem,
                SizeDeltaPayload       = deltaPay,
                DeltaRecordCountIfExact = deltaFull,
                ExactFitScore          = score,
                Label                  = "GUESS_NOT_VERIFIED",
            });
        }
        return records;
    }

    // -------------------------------------------------------------------------
    // B+D) Width analysis (entropy/pattern summary)
    // -------------------------------------------------------------------------

    private static RecordWidthAnalysisRecord BuildWidthAnalysis(byte[] data, int header, int width, string source)
    {
        long payload = Math.Max(0, data.Length - header);
        long count   = payload / width;
        long rem     = payload % width;

        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        int zeros = 0, allff = 0;

        for (long i = 0; i < count; i++)
        {
            int off = header + (int)(i * width);
            bool isZero = true, isFf = true;
            var sb = new StringBuilder(width * 3);
            for (int j = 0; j < width; j++)
            {
                byte b = data[off + j];
                if (b != 0x00) isZero = false;
                if (b != 0xFF) isFf   = false;
                if (j > 0) sb.Append(' ');
                sb.Append(b.ToString("X2"));
            }
            string key = sb.ToString();
            seen.TryGetValue(key, out int existing);
            seen[key] = existing + 1;
            if (isZero) zeros++;
            if (isFf)   allff++;
        }

        int distinct   = seen.Count;
        int repeated   = (int)count - distinct;
        int mostCommon = seen.Count > 0 ? seen.Values.Max() : 0;

        string score;
        if (rem == 0 && count > 0 && (double)distinct / count < 0.5)
            score = "HIGH_HEURISTIC";
        else if (rem == 0 && count > 0)
            score = "MEDIUM_HEURISTIC";
        else
            score = "LOW_HEURISTIC";

        return new RecordWidthAnalysisRecord
        {
            HeaderSize          = header,
            RecordWidth         = width,
            Source              = source,
            RecordCount         = count,
            PayloadRemainder    = rem,
            DistinctRecords     = distinct,
            RepeatedRecords     = repeated,
            ZeroRecordCount     = zeros,
            AllFfRecordCount    = allff,
            MostCommonRecordCount = mostCommon,
            LikelyRecordLikeScore = score,
        };
    }

    // -------------------------------------------------------------------------
    // B) Column statistics (header=2 width=8)
    // -------------------------------------------------------------------------

    private static List<RecordColumnStatRecord> BuildColumnStats(byte[] data, int header, int width, string source)
    {
        long payload = Math.Max(0, data.Length - header);
        long count   = payload / width;
        var cols = new List<RecordColumnStatRecord>();

        for (int col = 0; col < width; col++)
        {
            int minV = 256, maxV = -1;
            long zeros = 0, ffs = 0;
            var freq = new int[256];

            for (long i = 0; i < count; i++)
            {
                int off = header + (int)(i * width) + col;
                byte b = data[off];
                if (b < minV) minV = b;
                if (b > maxV) maxV = b;
                if (b == 0x00) zeros++;
                if (b == 0xFF) ffs++;
                freq[b]++;
            }

            int mostCommonByte = 0, mostCommonCount = 0;
            var distinct = new HashSet<byte>();
            for (int v = 0; v < 256; v++)
            {
                if (freq[v] > 0) distinct.Add((byte)v);
                if (freq[v] > mostCommonCount) { mostCommonCount = freq[v]; mostCommonByte = v; }
            }

            cols.Add(new RecordColumnStatRecord
            {
                HeaderSize    = header,
                RecordWidth   = width,
                Source        = source,
                ColumnIndex   = col,
                MinValue      = count > 0 ? minV : 0,
                MaxValue      = count > 0 ? maxV : 0,
                DistinctCount = distinct.Count,
                ZeroCount     = zeros,
                FfCount       = ffs,
                MostCommonByte  = mostCommonByte,
                MostCommonCount = mostCommonCount,
            });
        }
        return cols;
    }

    // -------------------------------------------------------------------------
    // B) Repeated record clusters
    // -------------------------------------------------------------------------

    private static List<RepeatedRecordClusterRecord> BuildRepeatedClusters(
        byte[] data, int header, int width, string source)
    {
        long payload = Math.Max(0, data.Length - header);
        long count   = payload / width;
        var freq = new Dictionary<string, (int Count, long FirstOffset)>(StringComparer.Ordinal);

        for (long i = 0; i < count; i++)
        {
            int off = header + (int)(i * width);
            string key = HexPreview(data, off, width);
            if (!freq.TryGetValue(key, out var existing))
                freq[key] = (1, off);
            else
                freq[key] = (existing.Count + 1, existing.FirstOffset);
        }

        return freq
            .Where(kv => kv.Value.Count > 1)
            .OrderByDescending(kv => kv.Value.Count)
            .Take(MaxTopRepeated)
            .Select(kv => new RepeatedRecordClusterRecord
            {
                Source      = source,
                HexBytes    = kv.Key,
                Count       = kv.Value.Count,
                FirstOffset = kv.Value.FirstOffset,
            })
            .ToList();
    }

    // -------------------------------------------------------------------------
    // B) Zero record clusters (contiguous runs of zero records)
    // -------------------------------------------------------------------------

    private static List<ZeroRecordClusterRecord> BuildZeroClusters(
        byte[] data, int header, int width, string source)
    {
        long payload = Math.Max(0, data.Length - header);
        long count   = payload / width;
        var clusters = new List<ZeroRecordClusterRecord>();

        long? runStart = null;
        for (long i = 0; i <= count; i++)
        {
            bool isZero = false;
            if (i < count)
            {
                int off = header + (int)(i * width);
                isZero = true;
                for (int j = 0; j < width; j++)
                    if (data[off + j] != 0) { isZero = false; break; }
            }

            if (isZero && runStart == null)
                runStart = i;
            else if (!isZero && runStart != null)
            {
                clusters.Add(new ZeroRecordClusterRecord
                {
                    Source        = source,
                    StartRecord   = runStart.Value,
                    EndRecord     = i - 1,
                    LengthRecords = i - runStart.Value,
                    StartOffset   = header + runStart.Value * width,
                });
                runStart = null;
            }
        }
        return clusters;
    }

    // -------------------------------------------------------------------------
    // D) Byte pattern clusters (top repeated byte patterns across all records)
    // -------------------------------------------------------------------------

    private static List<BytePatternClusterRecord> BuildBytePatternClusters(
        byte[] data, int header, int width, string source)
    {
        long payload = Math.Max(0, data.Length - header);
        long count   = payload / width;
        var freq = new Dictionary<string, int>(StringComparer.Ordinal);

        for (long i = 0; i < count; i++)
        {
            int off = header + (int)(i * width);
            string key = HexPreview(data, off, Math.Min(4, width));
            freq.TryGetValue(key, out int existing);
            freq[key] = existing + 1;
        }

        return freq
            .Where(kv => kv.Value > 1)
            .OrderByDescending(kv => kv.Value)
            .Take(20)
            .Select(kv => new BytePatternClusterRecord
            {
                HeaderSize  = header,
                RecordWidth = width,
                Source      = source,
                Pattern     = kv.Key,
                Count       = kv.Value,
            })
            .ToList();
    }

    // -------------------------------------------------------------------------
    // B) Record samples (first 64, last 64, every 256th)
    // -------------------------------------------------------------------------

    private static List<RecordClusterSampleRecord> BuildRecordSamples(
        byte[] data, int header, int width, string source)
    {
        long payload = Math.Max(0, data.Length - header);
        long count   = payload / width;
        var samples  = new List<RecordClusterSampleRecord>();

        // First up to 64
        long firstTake = Math.Min(count, MaxSampleFirst);
        for (long i = 0; i < firstTake; i++)
        {
            long off = header + i * width;
            samples.Add(MakeSample(data, i, off, width, source, "FIRST"));
        }

        // Last up to 64 (non-overlapping with first)
        long lastStart = Math.Max(firstTake, count - MaxSampleLast);
        for (long i = lastStart; i < count; i++)
        {
            long off = header + i * width;
            samples.Add(MakeSample(data, i, off, width, source, "LAST"));
        }

        // Every 256th (if > 256 records, starting at 256)
        for (long i = 256; i < count; i += 256)
        {
            long off = header + i * width;
            samples.Add(MakeSample(data, i, off, width, source, "STRIDE256"));
        }

        return samples;
    }

    private static RecordClusterSampleRecord MakeSample(
        byte[] data, long idx, long off, int width, string source, string group)
    {
        return new RecordClusterSampleRecord
        {
            RecordIndex = idx,
            FileOffset  = off,
            RecordSize  = width,
            HexBytes    = HexPreview(data, (int)off, width),
            Source      = source,
            SampleGroup = group,
        };
    }

    // -------------------------------------------------------------------------
    // C) Changed record windows (min vs vis, header=2 width=8)
    // -------------------------------------------------------------------------

    private static List<ChangedRecordWindowRecord> BuildChangedRecordWindows(
        byte[] min, byte[] vis, int header, int width)
    {
        long minCount = Math.Max(0, (min.Length - header)) / width;
        long visCount = Math.Max(0, (vis.Length - header)) / width;
        long overlap  = Math.Min(minCount, visCount);

        var windows = new List<ChangedRecordWindowRecord>();

        // Compare overlapping region record-by-record, group into SAME/DIFF windows
        bool? inDiff = null;
        long winStart = 0;

        for (long i = 0; i <= overlap; i++)
        {
            bool differs = false;
            if (i < overlap)
            {
                int minOff = header + (int)(i * width);
                int visOff = header + (int)(i * width);
                for (int j = 0; j < width; j++)
                    if (min[minOff + j] != vis[visOff + j]) { differs = true; break; }
            }

            if (i == 0)
            {
                inDiff = differs;
                winStart = 0;
            }
            else if (differs != inDiff || i == overlap)
            {
                // Emit window for [winStart, i-1]
                long wStart = winStart;
                long wEnd   = i - 1;
                long wLen   = wEnd - wStart + 1;
                if (wLen > 0)
                {
                    long minOff = header + wStart * width;
                    long visOff = header + wStart * width;
                    windows.Add(new ChangedRecordWindowRecord
                    {
                        StartRecordIndex   = wStart,
                        EndRecordIndex     = wEnd,
                        LengthRecords      = wLen,
                        MinimalStartOffset = minOff,
                        VisibleStartOffset = visOff,
                        MinimalHexPreview  = HexPreview(min, (int)minOff, Math.Min(width, (int)(wLen * width))),
                        VisibleHexPreview  = HexPreview(vis, (int)visOff, Math.Min(width, (int)(wLen * width))),
                        WindowType         = inDiff!.Value ? "DIFF" : "SAME",
                        Label              = "CANDIDATE_RECORD_WINDOW_UNVERIFIED",
                    });
                }
                inDiff = differs;
                winStart = i;
            }
        }

        // VISIBLE_EXTRA: records present only in visible
        if (visCount > minCount)
        {
            long extraStart = minCount;
            long extraCount = visCount - minCount;
            long extraOff   = header + extraStart * width;
            windows.Add(new ChangedRecordWindowRecord
            {
                StartRecordIndex   = extraStart,
                EndRecordIndex     = visCount - 1,
                LengthRecords      = extraCount,
                MinimalStartOffset = -1,
                VisibleStartOffset = extraOff,
                MinimalHexPreview  = "(no data - past minimal file end)",
                VisibleHexPreview  = HexPreview(vis, (int)extraOff, Math.Min(width * 4, (int)(extraCount * width))),
                WindowType         = "VISIBLE_EXTRA",
                Label              = "CANDIDATE_RECORD_WINDOW_UNVERIFIED",
            });
        }
        else if (minCount > visCount)
        {
            long extraStart = visCount;
            long extraOff   = header + extraStart * width;
            windows.Add(new ChangedRecordWindowRecord
            {
                StartRecordIndex   = extraStart,
                EndRecordIndex     = minCount - 1,
                LengthRecords      = minCount - visCount,
                MinimalStartOffset = extraOff,
                VisibleStartOffset = -1,
                MinimalHexPreview  = HexPreview(min, (int)extraOff, Math.Min(width * 4, (int)((minCount - visCount) * width))),
                VisibleHexPreview  = "(no data - past visible file end)",
                WindowType         = "MINIMAL_EXTRA",
                Label              = "CANDIDATE_RECORD_WINDOW_UNVERIFIED",
            });
        }

        return windows;
    }

    private static List<ChangedRecordWindowRecord> BuildVisibleOnlyWindows(byte[] vis, int header, int width)
    {
        long visCount = Math.Max(0, (vis.Length - header)) / width;
        if (visCount == 0) return new();
        long extraOff = header;
        return new List<ChangedRecordWindowRecord>
        {
            new ChangedRecordWindowRecord
            {
                StartRecordIndex   = 0,
                EndRecordIndex     = visCount - 1,
                LengthRecords      = visCount,
                MinimalStartOffset = -1,
                VisibleStartOffset = extraOff,
                MinimalHexPreview  = "(no minimal file)",
                VisibleHexPreview  = HexPreview(vis, (int)extraOff, Math.Min(width * 4, (int)(visCount * width))),
                WindowType         = "VISIBLE_EXTRA",
                Label              = "CANDIDATE_RECORD_WINDOW_UNVERIFIED",
            }
        };
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
        if (data == null || offset < 0 || offset >= data.Length) return "EOF";
        len = Math.Min(len, data.Length - offset);
        if (len <= 0) return string.Empty;
        return BitConverter.ToString(data, offset, len).Replace("-", " ");
    }

    private static void AddCheck(List<RecordClusterAuditCheck> checks,
        string id, string description, string expected, string actual)
    {
        checks.Add(new RecordClusterAuditCheck
        {
            CheckId     = id,
            Description = description,
            Expected    = expected,
            Actual      = actual,
            CheckStatus = expected == actual ? "PASS" : "FAIL",
        });
    }

    private static string CsvEscape(string s)
    {
        if (s != null && (s.Contains(',') || s.Contains('"') || s.Contains('\n')))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s ?? string.Empty;
    }

    // -------------------------------------------------------------------------
    // Render methods
    // -------------------------------------------------------------------------

    public string RenderJson(DeadMtlWorldBuilderChunkdataRecordClusterAuditResult result)
        => JsonSerializer.Serialize(result, s_json);

    public string RenderChecksCsv(DeadMtlWorldBuilderChunkdataRecordClusterAuditResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_id,description,expected,actual,check_status");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckId},{CsvEscape(c.Description)},{c.Expected},{c.Actual},{c.CheckStatus}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderChunkdataRecordClusterAuditResult result)
    {
        long minRec = result.MinimalSize >= 2 ? (result.MinimalSize - 2) / 8 : 0;
        long visRec = result.VisibleSize >= 2 ? (result.VisibleSize - 2) / 8 : 0;
        long extraRec = visRec - minRec;
        var sb = new StringBuilder();
        sb.AppendLine($"format                           : {result.Format}");
        sb.AppendLine($"generated_utc                    : {result.GeneratedUtc}");
        sb.AppendLine($"minimal_found                    : {result.MinimalFound}  size={result.MinimalSize}");
        sb.AppendLine($"visible_found                    : {result.VisibleFound}  size={result.VisibleSize}");
        sb.AppendLine($"size_delta                       : {result.SizeDelta}");
        sb.AppendLine($"header2_width8_minimal_records   : {minRec}");
        sb.AppendLine($"header2_width8_visible_records   : {visRec}");
        sb.AppendLine($"header2_width8_visible_extra     : {extraRec}");
        sb.AppendLine($"header_size_candidates           : {result.CandidateHeaderSizes.Count}");
        sb.AppendLine($"record_width_analyses            : {result.RecordWidthAnalyses.Count}");
        sb.AppendLine($"column_statistics                : {result.RecordColumnStatistics.Count}");
        sb.AppendLine($"repeated_record_clusters         : {result.RepeatedRecordClusters.Count}");
        sb.AppendLine($"zero_record_clusters             : {result.ZeroRecordClusters.Count}");
        sb.AppendLine($"changed_record_windows           : {result.ChangedRecordWindows.Count}");
        sb.AppendLine($"minimal_record_samples           : {result.MinimalRecordSamples.Count}");
        sb.AppendLine($"visible_record_samples           : {result.VisibleRecordSamples.Count}");
        sb.AppendLine($"runtime_binary_written           : {result.RuntimeBinaryWritten}");
        sb.AppendLine($"geometry_injected                : {result.GeometryInjected}");
        sb.AppendLine($"playable_export_claimed          : {result.PlayableExportClaimed}");
        sb.AppendLine($"verified_chunkdata_format        : {result.VerifiedChunkdataFormat}");
        sb.AppendLine($"read_only_probe                  : {result.ReadOnlyProbe}");
        sb.AppendLine($"checks                           : {result.CheckCount} total / {result.PassedCheckCount} PASS / {result.FailedCheckCount} FAIL");
        sb.AppendLine($"verdict                          : {result.Verdict}");
        return sb.ToString();
    }

    public string RenderHeaderCandidatesCsv(DeadMtlWorldBuilderChunkdataRecordClusterAuditResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("header_size,record_width,minimal_payload_size,visible_payload_size," +
                      "minimal_full_record_count,visible_full_record_count,minimal_remainder,visible_remainder," +
                      "size_delta_payload,delta_record_count_if_exact,exact_fit_score,label");
        foreach (var r in result.CandidateHeaderSizes)
            sb.AppendLine($"{r.HeaderSize},{r.RecordWidth},{r.MinimalPayloadSize},{r.VisiblePayloadSize}," +
                          $"{r.MinimalFullRecordCount},{r.VisibleFullRecordCount},{r.MinimalRemainder},{r.VisibleRemainder}," +
                          $"{r.SizeDeltaPayload},{r.DeltaRecordCountIfExact},{r.ExactFitScore},{r.Label}");
        return sb.ToString();
    }

    public string RenderWidthAnalysisCsv(DeadMtlWorldBuilderChunkdataRecordClusterAuditResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("header_size,record_width,source,record_count,payload_remainder," +
                      "distinct_records,repeated_records,zero_record_count,all_ff_record_count," +
                      "most_common_record_count,likely_record_like_score");
        foreach (var r in result.RecordWidthAnalyses)
            sb.AppendLine($"{r.HeaderSize},{r.RecordWidth},{r.Source},{r.RecordCount},{r.PayloadRemainder}," +
                          $"{r.DistinctRecords},{r.RepeatedRecords},{r.ZeroRecordCount},{r.AllFfRecordCount}," +
                          $"{r.MostCommonRecordCount},{r.LikelyRecordLikeScore}");
        return sb.ToString();
    }

    public string RenderColumnStatsCsv(DeadMtlWorldBuilderChunkdataRecordClusterAuditResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("header_size,record_width,source,column_index,min_value,max_value," +
                      "distinct_count,zero_count,ff_count,most_common_byte,most_common_count");
        foreach (var r in result.RecordColumnStatistics)
            sb.AppendLine($"{r.HeaderSize},{r.RecordWidth},{r.Source},{r.ColumnIndex},{r.MinValue},{r.MaxValue}," +
                          $"{r.DistinctCount},{r.ZeroCount},{r.FfCount},{r.MostCommonByte},{r.MostCommonCount}");
        return sb.ToString();
    }

    public string RenderRepeatedRecordsCsv(DeadMtlWorldBuilderChunkdataRecordClusterAuditResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("source,hex_bytes,count,first_offset");
        foreach (var r in result.RepeatedRecordClusters)
            sb.AppendLine($"{r.Source},{CsvEscape(r.HexBytes)},{r.Count},{r.FirstOffset}");
        return sb.ToString();
    }

    public string RenderChangedRecordWindowsCsv(DeadMtlWorldBuilderChunkdataRecordClusterAuditResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("start_record_index,end_record_index,length_records," +
                      "minimal_start_offset,visible_start_offset," +
                      "minimal_hex_preview,visible_hex_preview,window_type,label");
        foreach (var r in result.ChangedRecordWindows)
            sb.AppendLine($"{r.StartRecordIndex},{r.EndRecordIndex},{r.LengthRecords}," +
                          $"{r.MinimalStartOffset},{r.VisibleStartOffset}," +
                          $"{CsvEscape(r.MinimalHexPreview)},{CsvEscape(r.VisibleHexPreview)}," +
                          $"{r.WindowType},{r.Label}");
        return sb.ToString();
    }

    public string RenderRecordSamplesCsv(List<RecordClusterSampleRecord> samples)
    {
        var sb = new StringBuilder();
        sb.AppendLine("record_index,file_offset,record_size,hex_bytes,source,sample_group");
        foreach (var s in samples)
            sb.AppendLine($"{s.RecordIndex},{s.FileOffset},{s.RecordSize},{CsvEscape(s.HexBytes)},{s.Source},{s.SampleGroup}");
        return sb.ToString();
    }

    public string RenderAuditMarkdown(DeadMtlWorldBuilderChunkdataRecordClusterAuditResult result)
    {
        long minRec = result.MinimalSize >= 2 ? (result.MinimalSize - 2) / 8 : 0;
        long visRec = result.VisibleSize >= 2 ? (result.VisibleSize - 2) / 8 : 0;
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-37A: Chunkdata Record Cluster Audit");
        sb.AppendLine();
        sb.AppendLine($"Generated: {result.GeneratedUtc}");
        sb.AppendLine();
        sb.AppendLine("> **Read-only record-cluster anatomy probe. NOT a format decoder.**");
        sb.AppendLine("> ALL structural interpretations are CANDIDATES, GUESSES, or UNVERIFIED.");
        sb.AppendLine("> No chunkdata format has been confirmed. Do not treat any result as authoritative.");
        sb.AppendLine();
        sb.AppendLine("## Hypothesis Summary");
        sb.AppendLine();
        foreach (var h in result.HypothesisSummary)
            sb.AppendLine($"- {h}");
        sb.AppendLine();
        sb.AppendLine("## File Summary");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|-------|-------|");
        sb.AppendLine($"| minimal_size | {result.MinimalSize} bytes |");
        sb.AppendLine($"| visible_size | {result.VisibleSize} bytes |");
        sb.AppendLine($"| size_delta | {result.SizeDelta} bytes |");
        sb.AppendLine($"| header=2 width=8 minimal records | {minRec} |");
        sb.AppendLine($"| header=2 width=8 visible records | {visRec} |");
        sb.AppendLine($"| header=2 width=8 visible extra | {visRec - minRec} |");
        sb.AppendLine();
        sb.AppendLine("## Best Exact-Fit Header/Width Candidates");
        sb.AppendLine();
        sb.AppendLine("| Header | Width | Vis Count | Vis Rem | Min Count | Min Rem | Delta Exact | Score |");
        sb.AppendLine("|--------|-------|-----------|---------|-----------|---------|-------------|-------|");
        foreach (var r in result.CandidateHeaderSizes.OrderByDescending(x => x.ExactFitScore).Take(10))
            sb.AppendLine($"| {r.HeaderSize} | {r.RecordWidth} | {r.VisibleFullRecordCount} | {r.VisibleRemainder} | {r.MinimalFullRecordCount} | {r.MinimalRemainder} | {r.DeltaRecordCountIfExact} | {r.ExactFitScore} |");
        sb.AppendLine();
        sb.AppendLine("## Changed Record Windows (header=2 width=8)");
        sb.AppendLine();
        sb.AppendLine("| Type | Start Rec | End Rec | Length | Vis Offset | Min Hex Preview |");
        sb.AppendLine("|------|-----------|---------|--------|------------|-----------------|");
        foreach (var r in result.ChangedRecordWindows)
            sb.AppendLine($"| {r.WindowType} | {r.StartRecordIndex} | {r.EndRecordIndex} | {r.LengthRecords} | {r.VisibleStartOffset} | {r.MinimalHexPreview[..Math.Min(32, r.MinimalHexPreview.Length)]} |");
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
        sb.AppendLine($"| verified_chunkdata_format | {result.VerifiedChunkdataFormat.ToString().ToLowerInvariant()} |");
        sb.AppendLine($"| read_only_probe | {result.ReadOnlyProbe.ToString().ToLowerInvariant()} |");
        sb.AppendLine();
        sb.AppendLine("No binary files written. No geometry injection. Read-only chunkdata record-cluster audit.");
        sb.AppendLine();
        sb.AppendLine("## Verdict");
        sb.AppendLine();
        sb.AppendLine($"**{result.Verdict}** - {result.PassedCheckCount}/{result.CheckCount} checks PASS");
        return sb.ToString();
    }
}
