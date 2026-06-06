using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

/// <summary>
/// Process-level tests for --build42-candidate-profile empty_grass_v2 (MAP-6Z).
/// Verifies the Build 42 LOTH v3 candidate writer: 1024 generated entries + 1048-byte stable trailer.
/// Trailer is the MAP-6Y canonical stable block (80 Dru_map simple cells, all identical).
/// Claim boundary: build42_candidate_only_not_load_tested_not_playable
/// No playable export. No PZ assets. No load test. Trailer not copied from PZ game assets.
/// </summary>
public sealed class MapExportBuild42CandidateWriterV2ProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-b42-cand-v2-tests", Path.GetRandomFileName());

    public MapExportBuild42CandidateWriterV2ProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private string OutputBase => Path.Combine(_tempDir, ".local", "candidate-v2");
    private const string TestMapId = "pzmapforge_b42_cand_v2_test";

    private string CandidateDir => Path.Combine(OutputBase, TestMapId + "_build42_candidate");
    private string VersionedDir => Path.Combine(CandidateDir, "42");
    private string MapDataDir   => Path.Combine(VersionedDir, "media", "maps", TestMapId);

    private static (int ExitCode, string Stdout, string Stderr) RunCli(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = "dotnet",
            WorkingDirectory       = RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add(CliProjectPath);
        psi.ArgumentList.Add("--configuration");
        psi.ArgumentList.Add("Release");
        psi.ArgumentList.Add("--no-build");
        psi.ArgumentList.Add("--");
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var proc = Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stdout, stderr);
    }

    private (int Code, string Out, string Err) RunCandidateV2() => RunCli(
        "map-export-experimental",
        "--map-id", TestMapId,
        "--output", OutputBase,
        "--build42-candidate-writer",
        "--build42-candidate-profile", "empty_grass_v2");

    private string LotheaderPath => Path.Combine(MapDataDir, "0_0.lotheader");
    private string LotpackPath   => Path.Combine(MapDataDir, "world_0_0.lotpack");
    private string ChunkdataPath => Path.Combine(MapDataDir, "chunkdata_0_0.bin");
    private string ReportPath    => Path.Combine(VersionedDir, "experimental-map-export-report.json");

    // MAP-6Z canonical trailer: first two U32LE = 8, rest zero.
    // SHA-256: 93a8f3ccf2cafdc2fb7cd4f3836c29d87076f244f5ba685f92659fbdaf778ec7
    private const string Map6yCanonicalTrailerSha256 =
        "93a8f3ccf2cafdc2fb7cd4f3836c29d87076f244f5ba685f92659fbdaf778ec7";

    // Finds the byte offset where the ASCII table ends and the trailer begins.
    // Scans from offset 12 (after the 12-byte header); stops at the first non-ASCII, non-newline byte.
    private static int FindTrailerStart(byte[] bytes)
    {
        var i = 12;
        while (i < bytes.Length)
        {
            var b = bytes[i];
            if (b == 0x0A || (b >= 0x20 && b <= 0x7E)) i++;
            else break;
        }
        return i;
    }

    // -----------------------------------------------------------------------
    // Test 1: empty_grass_v2 exits 0
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_ExitsZero()
    {
        var (code, stdout, stderr) = RunCandidateV2();
        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // Test 2: output layout contains 42/ directory
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_VersionedDirExists()
    {
        RunCandidateV2();
        Assert.True(Directory.Exists(VersionedDir), $"42/ directory should exist at {VersionedDir}");
    }

    // -----------------------------------------------------------------------
    // Test 3: 0_0.lotheader exists
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_LotheaderExists()
    {
        RunCandidateV2();
        Assert.True(File.Exists(LotheaderPath));
    }

    // -----------------------------------------------------------------------
    // Test 4: LOTH bytes 0-3 are 4C 4F 54 48 (LOTH magic)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Lotheader_HasLothMagic()
    {
        RunCandidateV2();
        var bytes = File.ReadAllBytes(LotheaderPath);
        Assert.Equal(0x4C, bytes[0]);
        Assert.Equal(0x4F, bytes[1]);
        Assert.Equal(0x54, bytes[2]);
        Assert.Equal(0x48, bytes[3]);
    }

    // -----------------------------------------------------------------------
    // Test 5: LOTH version field (bytes 4-7) == 1 U32 LE
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Lotheader_VersionIsOne()
    {
        RunCandidateV2();
        var bytes   = File.ReadAllBytes(LotheaderPath);
        var version = BitConverter.ToUInt32(bytes, 4);
        Assert.Equal(1u, version);
    }

    // -----------------------------------------------------------------------
    // Test 6: LOTH entry_count (bytes 8-11) == 1024
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Lotheader_EntryCountIs1024()
    {
        RunCandidateV2();
        var bytes = File.ReadAllBytes(LotheaderPath);
        var count = BitConverter.ToUInt32(bytes, 8);
        Assert.Equal(1024u, count);
    }

    // -----------------------------------------------------------------------
    // Test 7: LOTH ASCII region contains exactly 1024 newline-delimited entries before trailer
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Lotheader_Has1024EntriesBeforeTrailer()
    {
        RunCandidateV2();
        var bytes        = File.ReadAllBytes(LotheaderPath);
        var trailerStart = FindTrailerStart(bytes);
        var asciiText    = System.Text.Encoding.ASCII.GetString(bytes, 12, trailerStart - 12);
        var entries      = asciiText.Split('\n').Where(s => s.Length > 0).ToArray();
        Assert.Equal(1024, entries.Length);
    }

    // -----------------------------------------------------------------------
    // Test 8: First entry is blends_grassoverlays_01_0
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Lotheader_FirstEntry()
    {
        RunCandidateV2();
        var bytes        = File.ReadAllBytes(LotheaderPath);
        var trailerStart = FindTrailerStart(bytes);
        var asciiText    = System.Text.Encoding.ASCII.GetString(bytes, 12, trailerStart - 12);
        var entries      = asciiText.Split('\n').Where(s => s.Length > 0).ToArray();
        Assert.Equal("blends_grassoverlays_01_0", entries[0]);
    }

    // -----------------------------------------------------------------------
    // Test 9: Last entry is blends_grassoverlays_01_1023
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Lotheader_LastEntry()
    {
        RunCandidateV2();
        var bytes        = File.ReadAllBytes(LotheaderPath);
        var trailerStart = FindTrailerStart(bytes);
        var asciiText    = System.Text.Encoding.ASCII.GetString(bytes, 12, trailerStart - 12);
        var entries      = asciiText.Split('\n').Where(s => s.Length > 0).ToArray();
        Assert.Equal("blends_grassoverlays_01_1023", entries[^1]);
    }

    // -----------------------------------------------------------------------
    // Test 10: LOTH trailer size is exactly 1048 bytes
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Lotheader_TrailerSizeIs1048()
    {
        RunCandidateV2();
        var bytes        = File.ReadAllBytes(LotheaderPath);
        var trailerStart = FindTrailerStart(bytes);
        Assert.Equal(1048, bytes.Length - trailerStart);
    }

    // -----------------------------------------------------------------------
    // Test 11: LOTH trailer starts at expected offset (12 + ascii_table_bytes)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Lotheader_TrailerStartsAtExpectedOffset()
    {
        RunCandidateV2();
        var bytes        = File.ReadAllBytes(LotheaderPath);
        var trailerStart = FindTrailerStart(bytes);
        // Compute expected ASCII table size from the same entry generation logic
        var expectedAsciiSize = Enumerable.Range(0, 1024)
            .Sum(i => $"blends_grassoverlays_01_{i}\n".Length);
        Assert.Equal(12 + expectedAsciiSize, trailerStart);
    }

    // -----------------------------------------------------------------------
    // Test 12: LOTH trailer SHA-256 matches MAP-6Y canonical stable trailer
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Lotheader_TrailerSha256MatchesCanonical()
    {
        RunCandidateV2();
        var bytes        = File.ReadAllBytes(LotheaderPath);
        var trailerStart = FindTrailerStart(bytes);
        var trailerBytes = bytes.AsSpan(trailerStart, 1048).ToArray();
        var actualSha256 = string.Join("", SHA256.HashData(trailerBytes).Select(b => b.ToString("x2")));
        // Verify against the structure: first two U32LE = 8, rest zero
        var expectedTrailer = new byte[1048];
        expectedTrailer[0] = 0x08;
        expectedTrailer[4] = 0x08;
        var expectedSha256 = string.Join("", SHA256.HashData(expectedTrailer).Select(b => b.ToString("x2")));
        Assert.Equal(expectedSha256, actualSha256);
        // Also verify the canonical MAP-6Y constant
        Assert.Equal(Map6yCanonicalTrailerSha256, actualSha256);
    }

    // -----------------------------------------------------------------------
    // Test 13: LOTH total size == 12 + ascii_table_bytes + 1048
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Lotheader_TotalSizeIsHeaderPlusAsciiPlusTrailer()
    {
        RunCandidateV2();
        var bytes        = File.ReadAllBytes(LotheaderPath);
        var trailerStart = FindTrailerStart(bytes);
        // trailerStart = 12 + ascii_table_bytes, so total = trailerStart + 1048
        Assert.Equal(trailerStart + 1048, bytes.Length);
    }

    // -----------------------------------------------------------------------
    // Test 14: report.build42_candidate_profile == "empty_grass_v2"
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Report_ProfileIsV2()
    {
        RunCandidateV2();
        var doc     = JsonDocument.Parse(File.ReadAllText(ReportPath));
        var profile = doc.RootElement.GetProperty("build42_candidate_profile").GetString();
        Assert.Equal("empty_grass_v2", profile);
    }

    // -----------------------------------------------------------------------
    // Test 15: report.loth_trailer_strategy == "map6y_stable_literal_1048_block"
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Report_LothTrailerStrategyIsMap6yStableLiteral()
    {
        RunCandidateV2();
        var doc      = JsonDocument.Parse(File.ReadAllText(ReportPath));
        var strategy = doc.RootElement.GetProperty("loth_trailer_strategy").GetString();
        Assert.Equal("map6y_stable_literal_1048_block", strategy);
    }

    // -----------------------------------------------------------------------
    // Test 16: report.loth_trailer_size == 1048
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Report_LothTrailerSizeIs1048()
    {
        RunCandidateV2();
        var doc  = JsonDocument.Parse(File.ReadAllText(ReportPath));
        var size = doc.RootElement.GetProperty("loth_trailer_size").GetInt32();
        Assert.Equal(1048, size);
    }

    // -----------------------------------------------------------------------
    // Test 17: report.loth_trailer_status == "generated_not_load_tested"
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Report_LothTrailerStatusIsGeneratedNotLoadTested()
    {
        RunCandidateV2();
        var doc    = JsonDocument.Parse(File.ReadAllText(ReportPath));
        var status = doc.RootElement.GetProperty("loth_trailer_status").GetString();
        Assert.Equal("generated_not_load_tested", status);
    }

    // -----------------------------------------------------------------------
    // Test 18: report.load_tested == false
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Report_LoadTestedFalse()
    {
        RunCandidateV2();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.False(doc.RootElement.GetProperty("load_tested").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 19: report.playable_export_generated == false
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Report_PlayableExportGeneratedFalse()
    {
        RunCandidateV2();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.False(doc.RootElement.GetProperty("playable_export_generated").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 20: report.playable_export_claimed == false
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Report_PlayableExportClaimedFalse()
    {
        RunCandidateV2();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.False(doc.RootElement.GetProperty("playable_export_claimed").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 21: report.pz_assets_copied == false
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Report_PzAssetsCopiedFalse()
    {
        RunCandidateV2();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.False(doc.RootElement.GetProperty("pz_assets_copied").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 22: report.pz_assets_read == false
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Report_PzAssetsReadFalse()
    {
        RunCandidateV2();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.False(doc.RootElement.GetProperty("pz_assets_read").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 23: report contains stable_reference_block_may_not_match_generated_tile_table_or_cell_payload
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Report_ContainsKnownRiskSentinel()
    {
        RunCandidateV2();
        var json = File.ReadAllText(ReportPath);
        Assert.Contains(
            "stable_reference_block_may_not_match_generated_tile_table_or_cell_payload",
            json, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 24: world_0_0.lotpack exists (LOTP unchanged from MAP-6S)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_LotpackExists()
    {
        RunCandidateV2();
        Assert.True(File.Exists(LotpackPath));
    }

    // -----------------------------------------------------------------------
    // Test 25: LOTP size unchanged from MAP-6L (1056780 bytes)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Lotpack_SizeIs1056780()
    {
        RunCandidateV2();
        var info = new FileInfo(LotpackPath);
        Assert.Equal(1056780L, info.Length);
    }

    // -----------------------------------------------------------------------
    // Test 26: chunkdata_0_0.bin exists (unchanged)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_ChunkdataExists()
    {
        RunCandidateV2();
        Assert.True(File.Exists(ChunkdataPath));
    }

    // -----------------------------------------------------------------------
    // Test 27: chunkdata size unchanged (1026 bytes)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_Chunkdata_SizeIs1026()
    {
        RunCandidateV2();
        var info = new FileInfo(ChunkdataPath);
        Assert.Equal(1026L, info.Length);
    }

    // -----------------------------------------------------------------------
    // Test 28: output outside .local is refused
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV2_OutputOutsideLocal_ExitsNonZero()
    {
        var badOutput = Path.Combine(_tempDir, "not-local-v2-candidate");
        var (code, _, stderr) = RunCli(
            "map-export-experimental",
            "--map-id", TestMapId,
            "--output", badOutput,
            "--build42-candidate-writer",
            "--build42-candidate-profile", "empty_grass_v2");
        Assert.NotEqual(0, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }
}
