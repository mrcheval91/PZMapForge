using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

/// <summary>
/// Process-level tests for --build42-candidate-profile empty_grass_v4 (MAP-7D).
/// Verifies the Build 42 candidate writer with no-BOM encoding for all game-read text files.
/// MAP-7C retest confirmed UTF-8 BOM causes LexState.token2str ArrayIndexOutOfBoundsException.
/// V4 fix: use UTF8Encoding(false) for mod.info, map.info, spawnpoints.lua, objects.lua, README.
/// LOTH/LOTP/chunkdata unchanged from v3.
/// Claim boundary: build42_candidate_only_not_load_tested_not_playable
/// </summary>
public sealed class MapExportBuild42CandidateWriterV4ProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-b42-cand-v4-tests", Path.GetRandomFileName());

    public MapExportBuild42CandidateWriterV4ProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private string OutputBase => Path.Combine(_tempDir, ".local", "candidate-v4");
    private const string TestMapId = "pzmapforge_b42_cand_v4_test";

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

    private (int Code, string Out, string Err) RunCandidateV4() => RunCli(
        "map-export-experimental",
        "--map-id", TestMapId,
        "--output", OutputBase,
        "--build42-candidate-writer",
        "--build42-candidate-profile", "empty_grass_v4");

    private string LotheaderPath  => Path.Combine(MapDataDir, "0_0.lotheader");
    private string LotpackPath    => Path.Combine(MapDataDir, "world_0_0.lotpack");
    private string ObjectsLuaPath => Path.Combine(MapDataDir, "objects.lua");
    private string SpawnPath      => Path.Combine(MapDataDir, "spawnpoints.lua");
    private string ModInfoPath    => Path.Combine(VersionedDir, "mod.info");
    private string MapInfoPath    => Path.Combine(MapDataDir, "map.info");
    private string ReportPath     => Path.Combine(VersionedDir, "experimental-map-export-report.json");

    private const string Map6yCanonicalTrailerSha256 =
        "93a8f3ccf2cafdc2fb7cd4f3836c29d87076f244f5ba685f92659fbdaf778ec7";

    private static bool HasBom(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
    }

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
    // Test 1: empty_grass_v4 exits 0
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV4_ExitsZero()
    {
        var (code, stdout, stderr) = RunCandidateV4();
        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // Test 2: LOTH total size unchanged at 29646 (same as v3)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV4_Lotheader_TotalSizeIs29646()
    {
        RunCandidateV4();
        Assert.Equal(29646L, new FileInfo(LotheaderPath).Length);
    }

    // -----------------------------------------------------------------------
    // Test 3: LOTH trailer SHA-256 matches MAP-6Y canonical
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV4_Lotheader_TrailerSha256MatchesCanonical()
    {
        RunCandidateV4();
        var bytes        = File.ReadAllBytes(LotheaderPath);
        var trailerStart = FindTrailerStart(bytes);
        var trailerBytes = bytes.AsSpan(trailerStart, 1048).ToArray();
        var sha256       = string.Join("", SHA256.HashData(trailerBytes).Select(b => b.ToString("x2")));
        Assert.Equal(Map6yCanonicalTrailerSha256, sha256);
    }

    // -----------------------------------------------------------------------
    // Test 4: objects.lua has no UTF-8 BOM
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV4_ObjectsLua_NoBom()
    {
        RunCandidateV4();
        Assert.False(HasBom(ObjectsLuaPath),
            "objects.lua must not have UTF-8 BOM (MAP-7D fix: BOM caused LexState error in MAP-7C)");
    }

    // -----------------------------------------------------------------------
    // Test 5: spawnpoints.lua has no UTF-8 BOM
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV4_SpawnpointsLua_NoBom()
    {
        RunCandidateV4();
        Assert.False(HasBom(SpawnPath), "spawnpoints.lua must not have UTF-8 BOM");
    }

    // -----------------------------------------------------------------------
    // Test 6: mod.info has no UTF-8 BOM
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV4_ModInfo_NoBom()
    {
        RunCandidateV4();
        Assert.False(HasBom(ModInfoPath), "mod.info must not have UTF-8 BOM");
    }

    // -----------------------------------------------------------------------
    // Test 7: map.info has no UTF-8 BOM
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV4_MapInfo_NoBom()
    {
        RunCandidateV4();
        Assert.False(HasBom(MapInfoPath), "map.info must not have UTF-8 BOM");
    }

    // -----------------------------------------------------------------------
    // Test 8: objects.lua is not return_only (does not contain bare "return {}")
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV4_ObjectsLua_IsNotReturnOnly()
    {
        RunCandidateV4();
        var content = File.ReadAllText(ObjectsLuaPath, System.Text.Encoding.UTF8);
        Assert.DoesNotContain("return {}", content, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 9: objects.lua first non-empty line starts with "--" (comment-only)
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV4_ObjectsLua_StartsWithComment()
    {
        RunCandidateV4();
        var lines         = File.ReadAllLines(ObjectsLuaPath, System.Text.Encoding.UTF8);
        var firstNonEmpty = lines.FirstOrDefault(l => l.Trim().Length > 0) ?? "";
        Assert.StartsWith("--", firstNonEmpty, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 10: spawnpoints.lua has "unemployed" key
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV4_SpawnpointsLua_HasUnemployedKey()
    {
        RunCandidateV4();
        Assert.Contains("unemployed", File.ReadAllText(SpawnPath), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 11: spawnpoints.lua has worldX, worldY
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV4_SpawnpointsLua_HasWorldCoordinates()
    {
        RunCandidateV4();
        var content = File.ReadAllText(SpawnPath);
        Assert.Contains("worldX", content, StringComparison.Ordinal);
        Assert.Contains("worldY", content, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 12: spawnpoints.lua has posX, posY, posZ
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV4_SpawnpointsLua_HasPosCoordinates()
    {
        RunCandidateV4();
        var content = File.ReadAllText(SpawnPath);
        Assert.Contains("posX", content, StringComparison.Ordinal);
        Assert.Contains("posY", content, StringComparison.Ordinal);
        Assert.Contains("posZ", content, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 13: report.build42_candidate_profile == "empty_grass_v4"
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV4_Report_ProfileIsV4()
    {
        RunCandidateV4();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.Equal("empty_grass_v4", doc.RootElement.GetProperty("build42_candidate_profile").GetString());
    }

    // -----------------------------------------------------------------------
    // Test 14: report contains text_encoding_strategy
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV4_Report_TextEncodingStrategyPresent()
    {
        RunCandidateV4();
        var json = File.ReadAllText(ReportPath);
        Assert.Contains("text_encoding_strategy", json, StringComparison.Ordinal);
        Assert.Contains("ascii_no_bom", json, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 15: report.load_tested == false
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV4_Report_LoadTestedFalse()
    {
        RunCandidateV4();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.False(doc.RootElement.GetProperty("load_tested").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 16: report.playable_export_claimed == false
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV4_Report_PlayableExportClaimedFalse()
    {
        RunCandidateV4();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.False(doc.RootElement.GetProperty("playable_export_claimed").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 17: report.pz_assets_copied == false
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV4_Report_PzAssetsCopiedFalse()
    {
        RunCandidateV4();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.False(doc.RootElement.GetProperty("pz_assets_copied").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 18: LOTP size unchanged at 1056780 bytes
    // -----------------------------------------------------------------------
    [Fact]
    public void Build42CandidateV4_Lotpack_SizeIs1056780()
    {
        RunCandidateV4();
        Assert.Equal(1056780L, new FileInfo(LotpackPath).Length);
    }
}
