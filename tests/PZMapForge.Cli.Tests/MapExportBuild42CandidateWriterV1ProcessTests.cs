using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

/// <summary>
/// Process-level tests for --build42-candidate-profile empty_grass_v1 (MAP-6S).
/// Verifies the Build 42 LOTH v2 candidate writer output with 1024 generated entries.
/// Claim boundary: build42_candidate_only_not_load_tested_not_playable
/// No playable export. No PZ assets. No load test.
/// Entries are generated -- not copied from any reference mod.
/// </summary>
public sealed class MapExportBuild42CandidateWriterV1ProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-b42-cand-v1-tests", Path.GetRandomFileName());

    public MapExportBuild42CandidateWriterV1ProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private string OutputBase => Path.Combine(_tempDir, ".local", "candidate-v1");
    private const string TestMapId = "pzmapforge_b42_cand_v1_test";

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

    private (int Code, string Out, string Err) RunCandidateV1() => RunCli(
        "map-export-experimental",
        "--map-id", TestMapId,
        "--output", OutputBase,
        "--build42-candidate-writer",
        "--build42-candidate-profile", "empty_grass_v1");

    private string LotheaderPath => Path.Combine(MapDataDir, "0_0.lotheader");
    private string LotpackPath   => Path.Combine(MapDataDir, "world_0_0.lotpack");
    private string ChunkdataPath => Path.Combine(MapDataDir, "chunkdata_0_0.bin");
    private string ReportPath    => Path.Combine(VersionedDir, "experimental-map-export-report.json");

    // -----------------------------------------------------------------------
    // Test 1: empty_grass_v1 exits 0
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_ExitsZero()
    {
        var (code, stdout, stderr) = RunCandidateV1();
        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // Test 2: output layout contains 42/ directory
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_VersionedDirExists()
    {
        RunCandidateV1();
        Assert.True(Directory.Exists(VersionedDir), $"42/ directory should exist at {VersionedDir}");
    }

    // -----------------------------------------------------------------------
    // Test 3: 0_0.lotheader exists
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_LotheaderExists()
    {
        RunCandidateV1();
        Assert.True(File.Exists(LotheaderPath));
    }

    // -----------------------------------------------------------------------
    // Test 4: LOTH bytes 0-3 are 4C 4F 54 48 (LOTH magic)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_Lotheader_HasLothMagic()
    {
        RunCandidateV1();
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
    public void Build42CandidateV1_Lotheader_VersionIsOne()
    {
        RunCandidateV1();
        var bytes   = File.ReadAllBytes(LotheaderPath);
        var version = BitConverter.ToUInt32(bytes, 4);
        Assert.Equal(1u, version);
    }

    // -----------------------------------------------------------------------
    // Test 6: LOTH entry_count (bytes 8-11) == 1024
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_Lotheader_EntryCountIs1024()
    {
        RunCandidateV1();
        var bytes   = File.ReadAllBytes(LotheaderPath);
        var count   = BitConverter.ToUInt32(bytes, 8);
        Assert.Equal(1024u, count);
    }

    // -----------------------------------------------------------------------
    // Test 7: LOTH string table contains exactly 1024 newline-delimited entries
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_Lotheader_Has1024Entries()
    {
        RunCandidateV1();
        var bytes   = File.ReadAllBytes(LotheaderPath);
        var text    = System.Text.Encoding.ASCII.GetString(bytes, 12, bytes.Length - 12);
        var entries = text.Split('\n').Where(s => s.Length > 0).ToArray();
        Assert.Equal(1024, entries.Length);
    }

    // -----------------------------------------------------------------------
    // Test 8: First entry is blends_grassoverlays_01_0
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_Lotheader_FirstEntry()
    {
        RunCandidateV1();
        var bytes   = File.ReadAllBytes(LotheaderPath);
        var text    = System.Text.Encoding.ASCII.GetString(bytes, 12, bytes.Length - 12);
        var entries = text.Split('\n').Where(s => s.Length > 0).ToArray();
        Assert.Equal("blends_grassoverlays_01_0", entries[0]);
    }

    // -----------------------------------------------------------------------
    // Test 9: Last entry is blends_grassoverlays_01_1023
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_Lotheader_LastEntry()
    {
        RunCandidateV1();
        var bytes   = File.ReadAllBytes(LotheaderPath);
        var text    = System.Text.Encoding.ASCII.GetString(bytes, 12, bytes.Length - 12);
        var entries = text.Split('\n').Where(s => s.Length > 0).ToArray();
        Assert.Equal("blends_grassoverlays_01_1023", entries[^1]);
    }

    // -----------------------------------------------------------------------
    // Test 10: LOTH size is much larger than MAP-6L v0 (38 bytes)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_Lotheader_LargerThanV0()
    {
        RunCandidateV1();
        var info = new FileInfo(LotheaderPath);
        Assert.True(info.Length > 38, $"v1 LOTH ({info.Length} bytes) should be >> 38 bytes (v0)");
    }

    // -----------------------------------------------------------------------
    // Test 11: LOTH size >= 25000 bytes (MAP-6R evidence-backed threshold)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_Lotheader_SizeAtLeast25000()
    {
        RunCandidateV1();
        var info = new FileInfo(LotheaderPath);
        Assert.True(info.Length >= 25000,
            $"v1 LOTH ({info.Length} bytes) should be >= 25000 (MAP-6R: refs are 34920-76721 bytes)");
    }

    // -----------------------------------------------------------------------
    // Test 12: report.build42_candidate_profile == "empty_grass_v1"
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_Report_ProfileIsV1()
    {
        RunCandidateV1();
        var doc     = JsonDocument.Parse(File.ReadAllText(ReportPath));
        var profile = doc.RootElement.GetProperty("build42_candidate_profile").GetString();
        Assert.Equal("empty_grass_v1", profile);
    }

    // -----------------------------------------------------------------------
    // Test 13: report.loth_entry_count == 1024
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_Report_LothEntryCountIs1024()
    {
        RunCandidateV1();
        var doc   = JsonDocument.Parse(File.ReadAllText(ReportPath));
        var count = doc.RootElement.GetProperty("loth_entry_count").GetInt32();
        Assert.Equal(1024, count);
    }

    // -----------------------------------------------------------------------
    // Test 14: report.loth_status == "generated_not_load_tested"
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_Report_LothStatusGeneratedNotLoadTested()
    {
        RunCandidateV1();
        var doc    = JsonDocument.Parse(File.ReadAllText(ReportPath));
        var status = doc.RootElement.GetProperty("loth_status").GetString();
        Assert.Equal("generated_not_load_tested", status);
    }

    // -----------------------------------------------------------------------
    // Test 15: report.load_tested == false
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_Report_LoadTestedFalse()
    {
        RunCandidateV1();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.False(doc.RootElement.GetProperty("load_tested").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 16: report.playable_export_generated == false
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_Report_PlayableExportGeneratedFalse()
    {
        RunCandidateV1();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.False(doc.RootElement.GetProperty("playable_export_generated").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 17: report.playable_export_claimed == false
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_Report_PlayableExportClaimedFalse()
    {
        RunCandidateV1();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.False(doc.RootElement.GetProperty("playable_export_claimed").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 18: report.pz_assets_copied == false
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_Report_PzAssetsCopiedFalse()
    {
        RunCandidateV1();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.False(doc.RootElement.GetProperty("pz_assets_copied").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 19: report.pz_assets_read == false
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_Report_PzAssetsReadFalse()
    {
        RunCandidateV1();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.False(doc.RootElement.GetProperty("pz_assets_read").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 20: JSON report contains generated_entries_may_not_match_loaded_tile_definitions
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_Report_ContainsKnownRiskSentinel()
    {
        RunCandidateV1();
        var json = File.ReadAllText(ReportPath);
        Assert.Contains("generated_entries_may_not_match_loaded_tile_definitions",
            json, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 21: world_0_0.lotpack exists (LOTP unchanged from v0)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_LotpackExists()
    {
        RunCandidateV1();
        Assert.True(File.Exists(LotpackPath));
    }

    // -----------------------------------------------------------------------
    // Test 22: LOTP size unchanged from MAP-6L v0 (1056780 bytes)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_Lotpack_SizeIs1056780()
    {
        RunCandidateV1();
        var info = new FileInfo(LotpackPath);
        Assert.Equal(1056780L, info.Length);
    }

    // -----------------------------------------------------------------------
    // Test 23: chunkdata_0_0.bin exists (unchanged from v0)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_ChunkdataExists()
    {
        RunCandidateV1();
        Assert.True(File.Exists(ChunkdataPath));
    }

    // -----------------------------------------------------------------------
    // Test 24: chunkdata size unchanged (1026 bytes)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_Chunkdata_SizeIs1026()
    {
        RunCandidateV1();
        var info = new FileInfo(ChunkdataPath);
        Assert.Equal(1026L, info.Length);
    }

    // -----------------------------------------------------------------------
    // Test 25: output outside .local is refused
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV1_OutputOutsideLocal_ExitsNonZero()
    {
        var badOutput = Path.Combine(_tempDir, "not-local-v1-candidate");
        var (code, _, stderr) = RunCli(
            "map-export-experimental",
            "--map-id", TestMapId,
            "--output", badOutput,
            "--build42-candidate-writer",
            "--build42-candidate-profile", "empty_grass_v1");
        Assert.NotEqual(0, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }
}
