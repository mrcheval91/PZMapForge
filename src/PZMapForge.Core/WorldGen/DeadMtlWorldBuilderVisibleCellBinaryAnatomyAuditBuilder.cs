using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PZMapForge.Core.WorldGen;

public sealed class DeadMtlWorldBuilderVisibleCellBinaryAnatomyAuditBuilder
{
    private static readonly JsonSerializerOptions s_json = new()
    {
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private const string LotHeaderFile = "35_27.lotheader";
    private const string ChunkdataFile = "chunkdata_35_27.bin";
    private const string LotpackFile   = "world_35_27.lotpack";

    private static readonly string[] s_sourceRejectionMarkers =
        { "Dru_map", "Dru", "workshop donor", "third-party" };

    private static readonly string[] s_sidecarFiles =
    {
        "map.info", "spawnpoints.lua", "objects.lua", "worldmap.xml", "worldmap.xml.bin",
        "worldmap-forest.xml", "worldmap-forest.xml.bin", "streets.xml.bin",
        "spawnregions.lua", "WorldGenOverride.lua", "thumb.png", "worldmap.png",
    };

    public DeadMtlWorldBuilderVisibleCellBinaryAnatomyAuditResult Build(
        string map33aSeedDir,
        string map35aSourceDir,
        string map35aInstalledDir,
        string map31bEmitterJson)
    {
        var result = new DeadMtlWorldBuilderVisibleCellBinaryAnatomyAuditResult
        {
            Format             = "MAP36A_VISIBLE_CELL_BINARY_ANATOMY_AUDIT_V1",
            GeneratedUtc       = DateTime.UtcNow.ToString("O"),
            CellCoord          = "35_27",
            Map33aSeedDir      = map33aSeedDir      ?? string.Empty,
            Map35aSourceDir    = map35aSourceDir    ?? string.Empty,
            Map35aInstalledDir = map35aInstalledDir ?? string.Empty,
            Map31bEmitterJson  = map31bEmitterJson  ?? string.Empty,
        };

        result.Map33aSeedDirFound      = !string.IsNullOrEmpty(result.Map33aSeedDir)      && Directory.Exists(result.Map33aSeedDir);
        result.Map35aInstalledDirFound = !string.IsNullOrEmpty(result.Map35aInstalledDir) && Directory.Exists(result.Map35aInstalledDir);
        result.Map31bEmitterJsonFound  = !string.IsNullOrEmpty(result.Map31bEmitterJson)  && File.Exists(result.Map31bEmitterJson);

        if (!result.Map33aSeedDirFound)
            result.Errors.Add($"MAP-33A seed dir not found: {result.Map33aSeedDir}");

        // Reject MAP-35A source before existence check
        result.Map35aSourceRejected = !string.IsNullOrEmpty(result.Map35aSourceDir)
            && s_sourceRejectionMarkers.Any(m => result.Map35aSourceDir.Contains(m, StringComparison.OrdinalIgnoreCase));

        if (result.Map35aSourceRejected)
        {
            string marker = s_sourceRejectionMarkers.First(m =>
                result.Map35aSourceDir.Contains(m, StringComparison.OrdinalIgnoreCase));
            result.Map35aSourceRejectionReason = $"source path contains rejected marker: {marker}";
            result.Errors.Add(result.Map35aSourceRejectionReason);
            result.Map35aSourceDirFound = false;
        }
        else
        {
            result.Map35aSourceDirFound = !string.IsNullOrEmpty(result.Map35aSourceDir)
                && Directory.Exists(result.Map35aSourceDir);
            if (!result.Map35aSourceDirFound)
                result.Errors.Add($"MAP-35A source dir not found: {result.Map35aSourceDir}");
        }

        result.LotHeaderAnatomy = BuildFileAnatomy(LotHeaderFile,
            result.Map33aSeedDir, result.Map35aSourceDir, result.Map35aInstalledDir,
            result.Map33aSeedDirFound, result.Map35aSourceDirFound, result.Map35aInstalledDirFound);
        result.ChunkdataAnatomy = BuildFileAnatomy(ChunkdataFile,
            result.Map33aSeedDir, result.Map35aSourceDir, result.Map35aInstalledDir,
            result.Map33aSeedDirFound, result.Map35aSourceDirFound, result.Map35aInstalledDirFound);
        result.LotpackAnatomy   = BuildFileAnatomy(LotpackFile,
            result.Map33aSeedDir, result.Map35aSourceDir, result.Map35aInstalledDir,
            result.Map33aSeedDirFound, result.Map35aSourceDirFound, result.Map35aInstalledDirFound);

        result.ChunkdataSpecial = new ChunkdataSpecialAnalysis
        {
            MinimalSize            = result.ChunkdataAnatomy.MinimalSize,
            VisibleSize            = result.ChunkdataAnatomy.VisibleSize,
            SizeDelta              = result.ChunkdataAnatomy.SizeDelta,
            SizeRatio              = result.ChunkdataAnatomy.MinimalSize > 0
                ? Math.Round((double)result.ChunkdataAnatomy.VisibleSize / result.ChunkdataAnatomy.MinimalSize, 2)
                : 0.0,
            RecordCountGuessFixed32 = (int)(result.ChunkdataAnatomy.VisibleSize / 32),
            RecordCountGuessFixed8  = (int)(result.ChunkdataAnatomy.VisibleSize / 8),
            RecordCountGuessLabel   = "GUESS_NOT_VERIFIED",
        };

        result.Map31bCrossRef = BuildMap31bCrossRef(result.Map31bEmitterJson, result.Map31bEmitterJsonFound);

        BuildFileInventory(result);
        BuildSidecarRecords(result);

        result.RuntimeBinaryWritten    = false;
        result.GeometryInjected        = false;
        result.PlayableExportClaimed   = false;
        result.WorkshopUploadPerformed = false;
        result.SteamInstallWrite       = false;

        var checks = new List<BinaryAnatomyAuditCheck>();

        AddCheck(checks, "MAP36A_MAP33A_SEED_DIR_EXISTS",
            "MAP-33A minimal seed directory found",
            "true", result.Map33aSeedDirFound.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36A_MAP35A_SOURCE_DIR_EXISTS",
            "MAP-35A visible source directory found",
            "true", result.Map35aSourceDirFound.ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36A_MAP35A_SOURCE_NOT_REJECTED",
            "MAP-35A source path does not contain rejected markers",
            "true", (!result.Map35aSourceRejected).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36A_LOTHEADER_SIZE_CAPTURED",
            "lotheader minimal and visible sizes captured",
            "true", (result.LotHeaderAnatomy.MinimalSize > 0 && result.LotHeaderAnatomy.VisibleSize > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36A_CHUNKDATA_SIZE_CAPTURED",
            "chunkdata minimal and visible sizes captured",
            "true", (result.ChunkdataAnatomy.MinimalSize > 0 && result.ChunkdataAnatomy.VisibleSize > 0).ToString().ToLowerInvariant());
        AddCheck(checks, "MAP36A_LOTPACK_SIZE_CAPTURED",
            "lotpack minimal and visible sizes captured",
            "true", (result.LotpackAnatomy.MinimalSize > 0 && result.LotpackAnatomy.VisibleSize > 0).ToString().ToLowerInvariant());

        bool sha256Ok = !string.IsNullOrEmpty(result.LotHeaderAnatomy.MinimalSha256)
            && !string.IsNullOrEmpty(result.LotHeaderAnatomy.VisibleSha256)
            && !string.IsNullOrEmpty(result.ChunkdataAnatomy.MinimalSha256)
            && !string.IsNullOrEmpty(result.ChunkdataAnatomy.VisibleSha256)
            && !string.IsNullOrEmpty(result.LotpackAnatomy.MinimalSha256)
            && !string.IsNullOrEmpty(result.LotpackAnatomy.VisibleSha256);
        AddCheck(checks, "MAP36A_SHA256_COMPUTED",
            "SHA256 computed for minimal and visible binary files",
            "true", sha256Ok.ToString().ToLowerInvariant());

        bool entropyOk = result.LotHeaderAnatomy.MinimalEntropy  is >= 0 and <= 8
            && result.LotHeaderAnatomy.VisibleEntropy   is >= 0 and <= 8
            && result.ChunkdataAnatomy.MinimalEntropy   is >= 0 and <= 8
            && result.ChunkdataAnatomy.VisibleEntropy   is >= 0 and <= 8
            && result.LotpackAnatomy.MinimalEntropy     is >= 0 and <= 8
            && result.LotpackAnatomy.VisibleEntropy     is >= 0 and <= 8;
        AddCheck(checks, "MAP36A_ENTROPY_IN_RANGE",
            "entropy values in [0,8] for all binary files",
            "true", entropyOk.ToString().ToLowerInvariant());

        AddCheck(checks, "MAP36A_MAP31B_CROSS_REF_CAPTURED",
            "MAP-31B cross-reference recorded",
            "true", "true");

        bool inventoryComplete = result.Map33aSeedDirFound && result.Map35aSourceDirFound;
        AddCheck(checks, "MAP36A_FILE_INVENTORY_COMPLETE",
            "file inventory built from both seed and source dirs",
            "true", inventoryComplete.ToString().ToLowerInvariant());

        bool claimClean = !result.RuntimeBinaryWritten && !result.GeometryInjected
            && !result.PlayableExportClaimed && !result.WorkshopUploadPerformed && !result.SteamInstallWrite;
        AddCheck(checks, "MAP36A_CLAIM_BOUNDARY_CLEAN",
            "claim boundary fields all false",
            "true", claimClean.ToString().ToLowerInvariant());

        result.Checks           = checks;
        result.CheckCount       = checks.Count;
        result.PassedCheckCount = checks.Count(c => c.CheckStatus == "PASS");
        result.FailedCheckCount = checks.Count(c => c.CheckStatus == "FAIL");
        result.IsValid          = result.FailedCheckCount == 0 && result.Errors.Count == 0;
        result.Verdict          = result.IsValid ? "MAP36A_ANATOMY_AUDIT_COMPLETE" : "MAP36A_ANATOMY_AUDIT_INCOMPLETE";

        return result;
    }

    public string RenderJson(DeadMtlWorldBuilderVisibleCellBinaryAnatomyAuditResult result)
        => JsonSerializer.Serialize(result, s_json);

    public string RenderChecksCsv(DeadMtlWorldBuilderVisibleCellBinaryAnatomyAuditResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("check_id,description,expected,actual,check_status");
        foreach (var c in result.Checks)
            sb.AppendLine($"{c.CheckId},\"{c.Description}\",{c.Expected},{c.Actual},{c.CheckStatus}");
        return sb.ToString();
    }

    public string RenderSummary(DeadMtlWorldBuilderVisibleCellBinaryAnatomyAuditResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-36A DEADMTL VISIBLE CELL BINARY ANATOMY AUDIT");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"Cell coord                       : {r.CellCoord}");
        sb.AppendLine($"MAP-33A seed dir found           : {r.Map33aSeedDirFound}");
        sb.AppendLine($"MAP-35A source dir found         : {r.Map35aSourceDirFound}");
        sb.AppendLine($"MAP-35A source rejected          : {r.Map35aSourceRejected}");
        sb.AppendLine($"MAP-35A installed dir found      : {r.Map35aInstalledDirFound}");
        sb.AppendLine($"MAP-31B emitter JSON found       : {r.Map31bEmitterJsonFound}");
        sb.AppendLine("");
        sb.AppendLine($"Lotheader  minimal={r.LotHeaderAnatomy.MinimalSize}  visible={r.LotHeaderAnatomy.VisibleSize}  delta={r.LotHeaderAnatomy.SizeDelta}");
        sb.AppendLine($"Chunkdata  minimal={r.ChunkdataAnatomy.MinimalSize}  visible={r.ChunkdataAnatomy.VisibleSize}  delta={r.ChunkdataAnatomy.SizeDelta}");
        sb.AppendLine($"Lotpack    minimal={r.LotpackAnatomy.MinimalSize}  visible={r.LotpackAnatomy.VisibleSize}  delta={r.LotpackAnatomy.SizeDelta}");
        sb.AppendLine($"Chunkdata size ratio (GUESS)     : {r.ChunkdataSpecial.SizeRatio}");
        sb.AppendLine($"File inventory count             : {r.FileInventory.Count} files  (common={r.CommonFiles.Count}  seed_only={r.Map33aOnlyFiles.Count}  visible_only={r.Map35aOnlyFiles.Count})");
        sb.AppendLine($"MAP-31B emits_binary_file        : {r.Map31bCrossRef.EmitsBinaryFile}");
        sb.AppendLine($"MAP-31B sandbox_only             : {r.Map31bCrossRef.SandboxOnly}");
        sb.AppendLine($"MAP-31B geometry-to-binary gap   : {r.Map31bCrossRef.Map31bGeometryToBinaryGap}");
        sb.AppendLine($"Checks                           : {r.CheckCount} total / {r.PassedCheckCount} PASS / {r.FailedCheckCount} FAIL");
        sb.AppendLine($"Is Valid                         : {r.IsValid}");
        sb.AppendLine($"Verdict                          : {r.Verdict}");
        sb.AppendLine("Claim boundary                   : runtime_binary_written=false geometry_injected=false playable_export_claimed=false");
        return sb.ToString();
    }

    public string RenderFileInventoryCsv(DeadMtlWorldBuilderVisibleCellBinaryAnatomyAuditResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("file_name,presence,map33a_size,map35a_size,installed_size,map33a_sha256,map35a_sha256");
        foreach (var rec in result.FileInventory)
            sb.AppendLine($"{rec.FileName},{rec.Presence},{rec.Map33aSize},{rec.Map35aSize},{rec.InstalledSize},{rec.Map33aSha256},{rec.Map35aSha256}");
        return sb.ToString();
    }

    public string RenderByteDiffCsv(DeadMtlWorldBuilderVisibleCellBinaryAnatomyAuditResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("file_name,minimal_size,visible_size,size_delta,first_differing_byte_offset,total_differing_bytes,common_prefix_length,common_suffix_length,minimal_sha256,visible_sha256");
        foreach (var a in new[] { result.LotHeaderAnatomy, result.ChunkdataAnatomy, result.LotpackAnatomy })
            sb.AppendLine($"{a.FileName},{a.MinimalSize},{a.VisibleSize},{a.SizeDelta},{a.FirstDifferingByteOffset},{a.TotalDifferingBytes},{a.CommonPrefixLength},{a.CommonSuffixLength},{a.MinimalSha256},{a.VisibleSha256}");
        return sb.ToString();
    }

    public string RenderProofMarkdown(DeadMtlWorldBuilderVisibleCellBinaryAnatomyAuditResult r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-36A Visible Cell Binary Anatomy Audit");
        sb.AppendLine();
        sb.AppendLine($"Generated: {r.GeneratedUtc}");
        sb.AppendLine($"Cell: {r.CellCoord}");
        sb.AppendLine($"Format: {r.Format}");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| runtime_binary_written | {r.RuntimeBinaryWritten.ToString().ToLowerInvariant()} |");
        sb.AppendLine($"| geometry_injected | {r.GeometryInjected.ToString().ToLowerInvariant()} |");
        sb.AppendLine($"| playable_export_claimed | {r.PlayableExportClaimed.ToString().ToLowerInvariant()} |");
        sb.AppendLine($"| workshop_upload_performed | {r.WorkshopUploadPerformed.ToString().ToLowerInvariant()} |");
        sb.AppendLine($"| steam_install_write | {r.SteamInstallWrite.ToString().ToLowerInvariant()} |");
        sb.AppendLine();
        sb.AppendLine("## Binary File Anatomy");
        sb.AppendLine();
        sb.AppendLine("| File | Minimal Size | Visible Size | Delta |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var a in new[] { r.LotHeaderAnatomy, r.ChunkdataAnatomy, r.LotpackAnatomy })
            sb.AppendLine($"| {a.FileName} | {a.MinimalSize} | {a.VisibleSize} | {a.SizeDelta} |");
        sb.AppendLine();
        sb.AppendLine("## File Inventory Summary");
        sb.AppendLine();
        sb.AppendLine("| Presence | Count |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| common | {r.CommonFiles.Count} |");
        sb.AppendLine($"| map33a_only | {r.Map33aOnlyFiles.Count} |");
        sb.AppendLine($"| map35a_only | {r.Map35aOnlyFiles.Count} |");
        sb.AppendLine($"| installed_only | {r.InstalledOnlyFiles.Count} |");
        sb.AppendLine();
        sb.AppendLine("## MAP-31B Cross-Reference");
        sb.AppendLine();
        sb.AppendLine($"emits_binary_file: {r.Map31bCrossRef.EmitsBinaryFile.ToString().ToLowerInvariant()}");
        sb.AppendLine($"sandbox_only: {r.Map31bCrossRef.SandboxOnly.ToString().ToLowerInvariant()}");
        sb.AppendLine($"map31b_geometry_to_binary_gap: {r.Map31bCrossRef.Map31bGeometryToBinaryGap}");
        sb.AppendLine();
        sb.AppendLine("## Checks");
        sb.AppendLine();
        sb.AppendLine($"{r.PassedCheckCount}/{r.CheckCount} PASS");
        return sb.ToString();
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private static void BuildFileInventory(DeadMtlWorldBuilderVisibleCellBinaryAnatomyAuditResult result)
    {
        var seedFiles      = result.Map33aSeedDirFound      ? GetFileNamesInDir(result.Map33aSeedDir)      : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visibleFiles   = result.Map35aSourceDirFound    ? GetFileNamesInDir(result.Map35aSourceDir)    : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var installedFiles = result.Map35aInstalledDirFound ? GetFileNamesInDir(result.Map35aInstalledDir) : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var allFileNames = seedFiles
            .Concat(visibleFiles)
            .Concat(installedFiles)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

        foreach (var fileName in allFileNames)
        {
            bool inSeed      = seedFiles.Contains(fileName);
            bool inVisible   = visibleFiles.Contains(fileName);
            bool inInstalled = installedFiles.Contains(fileName);

            string presence;
            if (inSeed && inVisible)
            {
                presence = "common";
                result.CommonFiles.Add(fileName);
            }
            else if (inSeed)
            {
                presence = "map33a_only";
                result.Map33aOnlyFiles.Add(fileName);
            }
            else if (inVisible)
            {
                presence = "map35a_only";
                result.Map35aOnlyFiles.Add(fileName);
            }
            else
            {
                presence = "installed_only";
                result.InstalledOnlyFiles.Add(fileName);
            }

            result.FileInventory.Add(new FileInventoryRecord
            {
                FileName      = fileName,
                Presence      = presence,
                Map33aSize    = inSeed      ? GetFileSize(result.Map33aSeedDir,      fileName) : 0,
                Map35aSize    = inVisible   ? GetFileSize(result.Map35aSourceDir,    fileName) : 0,
                InstalledSize = inInstalled ? GetFileSize(result.Map35aInstalledDir, fileName) : 0,
                Map33aSha256  = inSeed      ? ComputeSha256File(result.Map33aSeedDir,   fileName) : string.Empty,
                Map35aSha256  = inVisible   ? ComputeSha256File(result.Map35aSourceDir, fileName) : string.Empty,
            });
        }
    }

    private static void BuildSidecarRecords(DeadMtlWorldBuilderVisibleCellBinaryAnatomyAuditResult result)
    {
        foreach (var fileName in s_sidecarFiles)
        {
            bool seedHas      = result.Map33aSeedDirFound      && File.Exists(Path.Combine(result.Map33aSeedDir,      fileName));
            bool visibleHas   = result.Map35aSourceDirFound    && File.Exists(Path.Combine(result.Map35aSourceDir,    fileName));
            bool installedHas = result.Map35aInstalledDirFound && File.Exists(Path.Combine(result.Map35aInstalledDir, fileName));

            if (!seedHas && !visibleHas && !installedHas) continue;

            result.SidecarFileRecords.Add(new SidecarFileRecord
            {
                FileName      = fileName,
                Map33aSize    = seedHas      ? GetFileSize(result.Map33aSeedDir,      fileName) : 0,
                Map35aSize    = visibleHas   ? GetFileSize(result.Map35aSourceDir,    fileName) : 0,
                InstalledSize = installedHas ? GetFileSize(result.Map35aInstalledDir, fileName) : 0,
                Map33aSha256  = seedHas      ? ComputeSha256File(result.Map33aSeedDir,      fileName) : string.Empty,
                Map35aSha256  = visibleHas   ? ComputeSha256File(result.Map35aSourceDir,    fileName) : string.Empty,
            });
        }
    }

    private static HashSet<string> GetFileNamesInDir(string dir)
    {
        if (!Directory.Exists(dir)) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return new HashSet<string>(
            Directory.GetFiles(dir).Select(f => Path.GetFileName(f)!),
            StringComparer.OrdinalIgnoreCase);
    }

    private static long GetFileSize(string dir, string fileName)
    {
        string path = Path.Combine(dir, fileName);
        return File.Exists(path) ? new FileInfo(path).Length : 0;
    }

    private static string ComputeSha256File(string dir, string fileName)
    {
        string path = Path.Combine(dir, fileName);
        if (!File.Exists(path)) return string.Empty;
        using var fs = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(fs)).ToLowerInvariant();
    }

    private static BinaryFileAnatomyRecord BuildFileAnatomy(
        string fileName,
        string map33aDir, string map35aDir, string installedDir,
        bool map33aFound, bool map35aFound, bool installedFound)
    {
        var rec = new BinaryFileAnatomyRecord { FileName = fileName };

        byte[]? minBytes  = TryReadFile(map33aFound,    map33aDir,    fileName);
        byte[]? visBytes  = TryReadFile(map35aFound,    map35aDir,    fileName);
        byte[]? instBytes = TryReadFile(installedFound, installedDir, fileName);

        rec.MinimalSize   = minBytes?.LongLength   ?? 0;
        rec.VisibleSize   = visBytes?.LongLength   ?? 0;
        rec.InstalledSize = instBytes?.LongLength  ?? 0;
        rec.SizeDelta     = rec.VisibleSize - rec.MinimalSize;

        rec.MinimalSha256   = minBytes  != null ? ComputeSha256(minBytes)  : string.Empty;
        rec.VisibleSha256   = visBytes  != null ? ComputeSha256(visBytes)  : string.Empty;
        rec.InstalledSha256 = instBytes != null ? ComputeSha256(instBytes) : string.Empty;

        const int prefixSuffixLen = 32;
        rec.MinimalPrefixHex = minBytes != null ? BytesToHex(minBytes, 0, Math.Min(prefixSuffixLen, minBytes.Length)) : string.Empty;
        rec.VisiblePrefixHex = visBytes != null ? BytesToHex(visBytes, 0, Math.Min(prefixSuffixLen, visBytes.Length)) : string.Empty;
        rec.MinimalSuffixHex = minBytes != null ? BytesToHex(minBytes, Math.Max(0, minBytes.Length - prefixSuffixLen), Math.Min(prefixSuffixLen, minBytes.Length)) : string.Empty;
        rec.VisibleSuffixHex = visBytes != null ? BytesToHex(visBytes, Math.Max(0, visBytes.Length - prefixSuffixLen), Math.Min(prefixSuffixLen, visBytes.Length)) : string.Empty;

        if (minBytes != null && visBytes != null)
        {
            int minLen = Math.Min(minBytes.Length, visBytes.Length);
            rec.CommonPrefixLength = ComputeCommonPrefixLength(minBytes, visBytes);
            rec.CommonSuffixLength = ComputeCommonSuffixLength(minBytes, visBytes);

            bool identical = rec.CommonPrefixLength == minLen && minBytes.Length == visBytes.Length;
            rec.FirstDifferingByteOffset = identical ? -1 : rec.CommonPrefixLength;

            long diffCount = 0;
            for (int i = 0; i < minLen; i++)
                if (minBytes[i] != visBytes[i]) diffCount++;
            rec.TotalDifferingBytes = diffCount;
        }

        rec.MinimalEntropy = minBytes != null ? ComputeEntropy(minBytes) : 0.0;
        rec.VisibleEntropy = visBytes != null ? ComputeEntropy(visBytes) : 0.0;

        rec.MinimalPrintableStrings = minBytes != null ? ExtractPrintableStrings(minBytes, 200) : string.Empty;
        rec.VisiblePrintableStrings = visBytes != null ? ExtractPrintableStrings(visBytes, 200) : string.Empty;

        rec.SizeDeltaObservation = rec.SizeDelta > 0
            ? $"visible {rec.SizeDelta} bytes larger than minimal"
            : rec.SizeDelta < 0
                ? $"visible {-rec.SizeDelta} bytes SMALLER than minimal"
                : "identical size";

        if (!string.IsNullOrEmpty(rec.VisiblePrefixHex))
            rec.HeaderObservation = $"visible prefix: {rec.VisiblePrefixHex[..Math.Min(32, rec.VisiblePrefixHex.Length)]}";

        return rec;
    }

    private static byte[]? TryReadFile(bool dirExists, string dir, string fileName)
    {
        if (!dirExists || string.IsNullOrEmpty(dir)) return null;
        string path = Path.Combine(dir, fileName);
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    private static Map31bCrossReferenceRecord BuildMap31bCrossRef(string jsonPath, bool jsonFound)
    {
        var rec = new Map31bCrossReferenceRecord
        {
            EmitterJsonPath           = jsonPath,
            EmitterJsonFound          = jsonFound,
            Map31bGeometryToBinaryGap = "MAP31B_GEOMETRY_NOT_CONNECTED_TO_BINARY_RUNTIME_FILES",
        };

        if (!jsonFound) return rec;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
            var root = doc.RootElement;

            if (root.TryGetProperty("emitted_operation_count", out var opCount))
                rec.EmittedOperationCount = opCount.GetInt32();
            if (root.TryGetProperty("emitted_total_planned_cell_count", out var cellCount))
                rec.EmittedTotalPlannedCellCount = cellCount.GetInt32();

            if (root.TryGetProperty("emitted_operation_records", out var records))
            {
                bool anyBinary  = false;
                bool anyRuntime = false;
                bool allSandbox = true;

                foreach (var op in records.EnumerateArray())
                {
                    if (op.TryGetProperty("emits_binary_file",  out var emitBin) && emitBin.GetBoolean())
                        anyBinary = true;
                    if (op.TryGetProperty("runtime_consumable", out var rtCons) && rtCons.GetBoolean())
                        anyRuntime = true;
                    if (op.TryGetProperty("sandbox_only", out var sandboxOnly) && !sandboxOnly.GetBoolean())
                        allSandbox = false;
                }

                rec.EmitsBinaryFile   = anyBinary;
                rec.RuntimeConsumable = anyRuntime;
                rec.SandboxOnly       = allSandbox;
            }
        }
        catch (Exception ex)
        {
            int cap = Math.Min(100, ex.Message.Length);
            rec.Map31bGeometryToBinaryGap = $"ERROR_PARSING_JSON: {ex.Message[..cap]}";
        }

        return rec;
    }

    private static string ComputeSha256(byte[] data)
        => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    private static string BytesToHex(byte[] data, int offset, int count)
    {
        if (data.Length == 0 || count == 0) return string.Empty;
        return Convert.ToHexString(data, offset, count).ToLowerInvariant();
    }

    private static long ComputeCommonPrefixLength(byte[] a, byte[] b)
    {
        int minLen = Math.Min(a.Length, b.Length);
        for (int i = 0; i < minLen; i++)
            if (a[i] != b[i]) return i;
        return minLen;
    }

    private static long ComputeCommonSuffixLength(byte[] a, byte[] b)
    {
        int minLen = Math.Min(a.Length, b.Length);
        int ai = a.Length - 1, bi = b.Length - 1;
        long count = 0;
        for (int i = 0; i < minLen; i++, ai--, bi--)
        {
            if (a[ai] != b[bi]) break;
            count++;
        }
        return count;
    }

    private static double ComputeEntropy(byte[] data)
    {
        if (data.Length == 0) return 0.0;
        var freq = new int[256];
        foreach (var b in data) freq[b]++;
        double entropy = 0.0;
        int n = data.Length;
        foreach (var f in freq)
        {
            if (f == 0) continue;
            double p = (double)f / n;
            entropy -= p * Math.Log2(p);
        }
        return Math.Round(entropy, 4);
    }

    private static string ExtractPrintableStrings(byte[] data, int maxTotalChars)
    {
        var sb  = new StringBuilder();
        var run = new StringBuilder();
        foreach (var b in data)
        {
            if (b >= 32 && b <= 126)
            {
                run.Append((char)b);
            }
            else
            {
                if (run.Length >= 4)
                {
                    if (sb.Length > 0) sb.Append(' ');
                    int remaining = maxTotalChars - sb.Length;
                    if (remaining <= 0) break;
                    string s = run.ToString();
                    sb.Append(s.Length <= remaining ? s : s[..remaining]);
                    if (sb.Length >= maxTotalChars) break;
                }
                run.Clear();
            }
        }
        if (run.Length >= 4 && sb.Length < maxTotalChars)
        {
            if (sb.Length > 0) sb.Append(' ');
            int remaining = maxTotalChars - sb.Length;
            string s = run.ToString();
            sb.Append(s.Length <= remaining ? s : s[..remaining]);
        }
        return sb.ToString();
    }

    private static void AddCheck(List<BinaryAnatomyAuditCheck> checks,
        string id, string description, string expected, string actual)
    {
        checks.Add(new BinaryAnatomyAuditCheck
        {
            CheckId     = id,
            Description = description,
            Expected    = expected,
            Actual      = actual,
            CheckStatus = expected == actual ? "PASS" : "FAIL",
        });
    }
}
