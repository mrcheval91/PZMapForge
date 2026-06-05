using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

/// <summary>
/// Process-level tests for the --build42-candidate-writer flag (MAP-6L).
/// Verifies the Build 42 candidate writer MVP output.
/// Claim boundary: build42_candidate_only_not_load_tested_not_playable
/// No playable export. No PZ assets. No load test.
/// All binary formats are candidate-only and not load-tested.
/// </summary>
public sealed class MapExportBuild42CandidateWriterProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-b42-cand-tests", Path.GetRandomFileName());

    public MapExportBuild42CandidateWriterProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private string OutputBase => Path.Combine(_tempDir, ".local", "candidate");
    private const string TestMapId = "pzmapforge_b42_cand_test";

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

    private (int Code, string Out, string Err) RunCandidate() => RunCli(
        "map-export-experimental",
        "--map-id", TestMapId,
        "--output", OutputBase,
        "--build42-candidate-writer",
        "--build42-candidate-profile", "empty_grass_v0");

    // -----------------------------------------------------------------------
    // Test 1: candidate writer exits 0
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_ExitsZero()
    {
        var (code, stdout, stderr) = RunCandidate();
        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // Test 2: output layout contains 42/ directory
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_VersionedDirExists()
    {
        RunCandidate();
        Assert.True(Directory.Exists(VersionedDir), $"42/ directory should exist at {VersionedDir}");
    }

    // -----------------------------------------------------------------------
    // Test 3: 0_0.lotheader exists
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_LotheaderExists()
    {
        RunCandidate();
        Assert.True(File.Exists(Path.Combine(MapDataDir, "0_0.lotheader")));
    }

    // -----------------------------------------------------------------------
    // Test 4: world_0_0.lotpack exists
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_LotpackExists()
    {
        RunCandidate();
        Assert.True(File.Exists(Path.Combine(MapDataDir, "world_0_0.lotpack")));
    }

    // -----------------------------------------------------------------------
    // Test 5: chunkdata_0_0.bin exists
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_ChunkdataExists()
    {
        RunCandidate();
        Assert.True(File.Exists(Path.Combine(MapDataDir, "chunkdata_0_0.bin")));
    }

    // -----------------------------------------------------------------------
    // Test 6: LOTH bytes 0-3 are 4C 4F 54 48
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Lotheader_HasLothMagic()
    {
        RunCandidate();
        var bytes = File.ReadAllBytes(Path.Combine(MapDataDir, "0_0.lotheader"));
        Assert.Equal(0x4C, bytes[0]);
        Assert.Equal(0x4F, bytes[1]);
        Assert.Equal(0x54, bytes[2]);
        Assert.Equal(0x48, bytes[3]);
    }

    // -----------------------------------------------------------------------
    // Test 7: LOTH version field (bytes 4-7) == 1 U32 LE
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Lotheader_VersionIsOne()
    {
        RunCandidate();
        var bytes = File.ReadAllBytes(Path.Combine(MapDataDir, "0_0.lotheader"));
        var version = BitConverter.ToUInt32(bytes, 4);
        Assert.Equal(1u, version);
    }

    // -----------------------------------------------------------------------
    // Test 8: LOTH entry_count (bytes 8-11) matches actual entries written
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Lotheader_EntryCountMatchesEntries()
    {
        RunCandidate();
        var bytes      = File.ReadAllBytes(Path.Combine(MapDataDir, "0_0.lotheader"));
        var declared   = (int)BitConverter.ToUInt32(bytes, 8);
        var text       = System.Text.Encoding.ASCII.GetString(bytes, 12, bytes.Length - 12);
        var entries    = text.Split('\n').Where(s => s.Length > 0).ToArray();
        Assert.Equal(declared, entries.Length);
    }

    // -----------------------------------------------------------------------
    // Test 9: LOTP bytes 0-3 are 4C 4F 54 50
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Lotpack_HasLotpMagic()
    {
        RunCandidate();
        var bytes = File.ReadAllBytes(Path.Combine(MapDataDir, "world_0_0.lotpack"));
        Assert.Equal(0x4C, bytes[0]);
        Assert.Equal(0x4F, bytes[1]);
        Assert.Equal(0x54, bytes[2]);
        Assert.Equal(0x50, bytes[3]);
    }

    // -----------------------------------------------------------------------
    // Test 10: LOTP version field (bytes 4-7) == 1 U32 LE
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Lotpack_VersionIsOne()
    {
        RunCandidate();
        var bytes   = File.ReadAllBytes(Path.Combine(MapDataDir, "world_0_0.lotpack"));
        var version = BitConverter.ToUInt32(bytes, 4);
        Assert.Equal(1u, version);
    }

    // -----------------------------------------------------------------------
    // Test 11: LOTP chunk_count (bytes 8-11) == 1024
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Lotpack_ChunkCountIs1024()
    {
        RunCandidate();
        var bytes      = File.ReadAllBytes(Path.Combine(MapDataDir, "world_0_0.lotpack"));
        var chunkCount = BitConverter.ToUInt32(bytes, 8);
        Assert.Equal(1024u, chunkCount);
    }

    // -----------------------------------------------------------------------
    // Test 12: First LOTP offset table entry == 8204
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Lotpack_FirstOffsetIs8204()
    {
        RunCandidate();
        var bytes  = File.ReadAllBytes(Path.Combine(MapDataDir, "world_0_0.lotpack"));
        var offset = BitConverter.ToUInt64(bytes, 12); // first entry at byte 12
        Assert.Equal(8204UL, offset);
    }

    // -----------------------------------------------------------------------
    // Test 13: Second LOTP offset table entry == 9228 (8204 + 1024)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Lotpack_SecondOffsetIs9228()
    {
        RunCandidate();
        var bytes  = File.ReadAllBytes(Path.Combine(MapDataDir, "world_0_0.lotpack"));
        var offset = BitConverter.ToUInt64(bytes, 12 + 8); // second entry at byte 20
        Assert.Equal(9228UL, offset);
    }

    // -----------------------------------------------------------------------
    // Test 14: Last LOTP offset table entry == 8204 + 1023 * 1024 = 1055756
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Lotpack_LastOffsetIs1055756()
    {
        RunCandidate();
        var bytes      = File.ReadAllBytes(Path.Combine(MapDataDir, "world_0_0.lotpack"));
        var lastPos    = 12 + 1023 * 8; // last entry at byte 8196
        var lastOffset = BitConverter.ToUInt64(bytes, lastPos);
        Assert.Equal(1055756UL, lastOffset); // 8204 + 1023 * 1024
    }

    // -----------------------------------------------------------------------
    // Test 15: LOTP file size == 1056780 bytes
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Lotpack_SizeIs1056780()
    {
        RunCandidate();
        var info = new FileInfo(Path.Combine(MapDataDir, "world_0_0.lotpack"));
        Assert.Equal(1056780L, info.Length);
    }

    // -----------------------------------------------------------------------
    // Test 16: chunkdata size == 1026 bytes
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Chunkdata_SizeIs1026()
    {
        RunCandidate();
        var info = new FileInfo(Path.Combine(MapDataDir, "chunkdata_0_0.bin"));
        Assert.Equal(1026L, info.Length);
    }

    // -----------------------------------------------------------------------
    // Test 17: chunkdata bytes 0-1 == 00 01
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Chunkdata_Header00_01()
    {
        RunCandidate();
        var bytes = File.ReadAllBytes(Path.Combine(MapDataDir, "chunkdata_0_0.bin"));
        Assert.Equal(0x00, bytes[0]);
        Assert.Equal(0x01, bytes[1]);
    }

    // -----------------------------------------------------------------------
    // Test 18: chunkdata body (bytes 2-1025) is all zero
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Chunkdata_BodyAllZero()
    {
        RunCandidate();
        var bytes = File.ReadAllBytes(Path.Combine(MapDataDir, "chunkdata_0_0.bin"));
        for (var i = 2; i < 1026; i++)
            Assert.Equal(0, bytes[i]);
    }

    // -----------------------------------------------------------------------
    // Test 19: report.writer_implemented == true
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Report_WriterImplementedTrue()
    {
        RunCandidate();
        var doc = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(VersionedDir, "experimental-map-export-report.json")));
        Assert.True(doc.RootElement.GetProperty("writer_implemented").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 20: report.load_tested == false
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Report_LoadTestedFalse()
    {
        RunCandidate();
        var doc = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(VersionedDir, "experimental-map-export-report.json")));
        Assert.False(doc.RootElement.GetProperty("load_tested").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 21: report.playable_export_generated == false
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Report_PlayableExportGeneratedFalse()
    {
        RunCandidate();
        var doc = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(VersionedDir, "experimental-map-export-report.json")));
        Assert.False(doc.RootElement.GetProperty("playable_export_generated").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 22: report.playable_export_claimed == false
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Report_PlayableExportClaimedFalse()
    {
        RunCandidate();
        var doc = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(VersionedDir, "experimental-map-export-report.json")));
        Assert.False(doc.RootElement.GetProperty("playable_export_claimed").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 23: report contains generated_not_load_tested in status fields
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Report_ContainsGeneratedNotLoadTested()
    {
        RunCandidate();
        var json = File.ReadAllText(Path.Combine(VersionedDir, "experimental-map-export-report.json"));
        Assert.Contains("generated_not_load_tested", json, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 24: output outside .local is refused
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_OutputOutsideLocal_ExitsNonZero()
    {
        var badOutput = Path.Combine(_tempDir, "not-local-candidate");
        var (code, _, stderr) = RunCli(
            "map-export-experimental",
            "--map-id", TestMapId,
            "--output", badOutput,
            "--build42-candidate-writer");
        Assert.NotEqual(0, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 25: report.lotp_file_size_expected == 1056780
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42Candidate_Report_LotpFileSizeExpectedIs1056780()
    {
        RunCandidate();
        var doc = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(VersionedDir, "experimental-map-export-report.json")));
        Assert.Equal(1056780, doc.RootElement.GetProperty("lotp_file_size_expected").GetInt32());
    }
}
