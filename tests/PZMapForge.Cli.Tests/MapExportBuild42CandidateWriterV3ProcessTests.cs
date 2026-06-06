using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

/// <summary>
/// Process-level tests for --build42-candidate-profile empty_grass_v3 (MAP-7C).
/// Verifies the Build 42 candidate writer with:
/// - Same LOTH v3 (1024 entries + 1048-byte canonical trailer)
/// - Comment-only objects.lua (avoids MAP-7A LexState.token2str failure)
/// - Unemployed-key spawnpoints.lua (explicit profession spawn format)
/// Claim boundary: build42_candidate_only_not_load_tested_not_playable
/// No playable export. No PZ assets. No load test.
/// </summary>
public sealed class MapExportBuild42CandidateWriterV3ProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-b42-cand-v3-tests", Path.GetRandomFileName());

    public MapExportBuild42CandidateWriterV3ProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private string OutputBase => Path.Combine(_tempDir, ".local", "candidate-v3");
    private const string TestMapId = "pzmapforge_b42_cand_v3_test";

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

    private (int Code, string Out, string Err) RunCandidateV3() => RunCli(
        "map-export-experimental",
        "--map-id", TestMapId,
        "--output", OutputBase,
        "--build42-candidate-writer",
        "--build42-candidate-profile", "empty_grass_v3");

    private string LotheaderPath  => Path.Combine(MapDataDir, "0_0.lotheader");
    private string LotpackPath    => Path.Combine(MapDataDir, "world_0_0.lotpack");
    private string ObjectsLuaPath => Path.Combine(MapDataDir, "objects.lua");
    private string SpawnPath      => Path.Combine(MapDataDir, "spawnpoints.lua");
    private string ReportPath     => Path.Combine(VersionedDir, "experimental-map-export-report.json");

    private const string Map6yCanonicalTrailerSha256 =
        "93a8f3ccf2cafdc2fb7cd4f3836c29d87076f244f5ba685f92659fbdaf778ec7";

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
    // Test 1: empty_grass_v3 exits 0
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_ExitsZero()
    {
        var (code, stdout, stderr) = RunCandidateV3();
        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // Test 2: output layout contains 42/ directory
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_VersionedDirExists()
    {
        RunCandidateV3();
        Assert.True(Directory.Exists(VersionedDir));
    }

    // -----------------------------------------------------------------------
    // Test 3: 0_0.lotheader exists
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_LotheaderExists()
    {
        RunCandidateV3();
        Assert.True(File.Exists(LotheaderPath));
    }

    // -----------------------------------------------------------------------
    // Test 4: LOTH magic = 4C 4F 54 48
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_Lotheader_HasLothMagic()
    {
        RunCandidateV3();
        var bytes = File.ReadAllBytes(LotheaderPath);
        Assert.Equal(0x4C, bytes[0]);
        Assert.Equal(0x4F, bytes[1]);
        Assert.Equal(0x54, bytes[2]);
        Assert.Equal(0x48, bytes[3]);
    }

    // -----------------------------------------------------------------------
    // Test 5: LOTH version = 1
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_Lotheader_VersionIsOne()
    {
        RunCandidateV3();
        var bytes   = File.ReadAllBytes(LotheaderPath);
        var version = BitConverter.ToUInt32(bytes, 4);
        Assert.Equal(1u, version);
    }

    // -----------------------------------------------------------------------
    // Test 6: LOTH entry_count = 1024 (same as v2)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_Lotheader_EntryCountIs1024()
    {
        RunCandidateV3();
        var bytes = File.ReadAllBytes(LotheaderPath);
        var count = BitConverter.ToUInt32(bytes, 8);
        Assert.Equal(1024u, count);
    }

    // -----------------------------------------------------------------------
    // Test 7: LOTH total size = 29646 (same as v2: 12 + 28586 + 1048)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_Lotheader_TotalSizeIs29646()
    {
        RunCandidateV3();
        var info = new FileInfo(LotheaderPath);
        Assert.Equal(29646L, info.Length);
    }

    // -----------------------------------------------------------------------
    // Test 8: LOTH trailer SHA-256 matches MAP-6Y canonical value
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_Lotheader_TrailerSha256MatchesCanonical()
    {
        RunCandidateV3();
        var bytes        = File.ReadAllBytes(LotheaderPath);
        var trailerStart = FindTrailerStart(bytes);
        var trailerBytes = bytes.AsSpan(trailerStart, 1048).ToArray();
        var actualSha256 = string.Join("", SHA256.HashData(trailerBytes).Select(b => b.ToString("x2")));
        Assert.Equal(Map6yCanonicalTrailerSha256, actualSha256);
    }

    // -----------------------------------------------------------------------
    // Test 9: objects.lua exists
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_ObjectsLuaExists()
    {
        RunCandidateV3();
        Assert.True(File.Exists(ObjectsLuaPath));
    }

    // -----------------------------------------------------------------------
    // Test 10: objects.lua does NOT contain bare "return {}"
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_ObjectsLua_IsNotReturnOnly()
    {
        RunCandidateV3();
        var content = File.ReadAllText(ObjectsLuaPath, System.Text.Encoding.UTF8);
        Assert.DoesNotContain("return {}", content, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 11: objects.lua first non-empty line is a Lua comment (--)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_ObjectsLua_StartsWithComment()
    {
        RunCandidateV3();
        var lines         = File.ReadAllLines(ObjectsLuaPath, System.Text.Encoding.UTF8);
        var firstNonEmpty = lines.FirstOrDefault(l => l.Trim().Length > 0) ?? "";
        Assert.StartsWith("--", firstNonEmpty, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 12: spawnpoints.lua has "unemployed" key
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_SpawnpointsLua_HasUnemployedKey()
    {
        RunCandidateV3();
        var content = File.ReadAllText(SpawnPath, System.Text.Encoding.UTF8);
        Assert.Contains("unemployed", content, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 13: spawnpoints.lua has worldX and worldY
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_SpawnpointsLua_HasWorldCoordinates()
    {
        RunCandidateV3();
        var content = File.ReadAllText(SpawnPath, System.Text.Encoding.UTF8);
        Assert.Contains("worldX", content, StringComparison.Ordinal);
        Assert.Contains("worldY", content, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 14: spawnpoints.lua has posX, posY, posZ
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_SpawnpointsLua_HasPosCoordinates()
    {
        RunCandidateV3();
        var content = File.ReadAllText(SpawnPath, System.Text.Encoding.UTF8);
        Assert.Contains("posX", content, StringComparison.Ordinal);
        Assert.Contains("posY", content, StringComparison.Ordinal);
        Assert.Contains("posZ", content, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 15: report.build42_candidate_profile == "empty_grass_v3"
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_Report_ProfileIsV3()
    {
        RunCandidateV3();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.Equal("empty_grass_v3", doc.RootElement.GetProperty("build42_candidate_profile").GetString());
    }

    // -----------------------------------------------------------------------
    // Test 16: report contains lua_metadata_strategy field
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_Report_LuaMetadataStrategyPresent()
    {
        RunCandidateV3();
        var json = File.ReadAllText(ReportPath);
        Assert.Contains("lua_metadata_strategy", json, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 17: report contains objects_lua_strategy field
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_Report_ObjectsLuaStrategyPresent()
    {
        RunCandidateV3();
        var json = File.ReadAllText(ReportPath);
        Assert.Contains("objects_lua_strategy", json, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 18: report.load_tested == false
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_Report_LoadTestedFalse()
    {
        RunCandidateV3();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.False(doc.RootElement.GetProperty("load_tested").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 19: report.playable_export_claimed == false
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_Report_PlayableExportClaimedFalse()
    {
        RunCandidateV3();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.False(doc.RootElement.GetProperty("playable_export_claimed").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 20: LOTP size unchanged at 1056780 bytes
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV3_Lotpack_SizeIs1056780()
    {
        RunCandidateV3();
        var info = new FileInfo(LotpackPath);
        Assert.Equal(1056780L, info.Length);
    }
}
