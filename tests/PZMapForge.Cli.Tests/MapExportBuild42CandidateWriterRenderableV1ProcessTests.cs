using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

/// <summary>
/// Process-level tests for --build42-candidate-profile renderable_v1.
/// PROVISIONAL: this profile is based on one human runtime test (2026-07-08) that
/// confirmed a cell written in this exact format rendered as a visually distinct,
/// non-fallback pattern in Build 42 -- not a repeatable automated proof. These tests
/// verify the writer's output is byte-shape-consistent with what was confirmed to
/// render, not that any output "works" in-game (no automated PZ runtime harness exists).
/// Claim boundary: build42_candidate_only_not_load_tested_not_playable.
/// </summary>
public sealed class MapExportBuild42CandidateWriterRenderableV1ProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-b42-cand-renderable-v1-tests", Path.GetRandomFileName());

    public MapExportBuild42CandidateWriterRenderableV1ProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private string OutputBase => Path.Combine(_tempDir, ".local", "candidate-renderable-v1");
    private const string TestMapId = "pzmapforge_b42_cand_renderable_v1_test";

    private string CandidateDir => Path.Combine(OutputBase, TestMapId + "_build42_candidate");
    private string VersionedDir => Path.Combine(CandidateDir, "42");
    private string CommonDir    => Path.Combine(CandidateDir, "common");
    private string MapDataDir   => Path.Combine(CommonDir, "media", "maps", TestMapId);

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
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        return (proc.ExitCode, stdout, stderr);
    }

    private (int Code, string Out, string Err) RunCandidateRenderableV1(int cellX = 35, int cellY = 27) => RunCli(
        "map-export-experimental",
        "--map-id", TestMapId,
        "--output", OutputBase,
        "--build42-candidate-writer",
        "--build42-candidate-profile", "renderable_v1",
        "--cell-x", cellX.ToString(),
        "--cell-y", cellY.ToString());

    private string LotheaderPath    => Path.Combine(MapDataDir, "35_27.lotheader");
    private string LotpackPath      => Path.Combine(MapDataDir, "world_35_27.lotpack");
    private string ChunkdataPath    => Path.Combine(MapDataDir, "chunkdata_35_27.bin");
    private string ObjectsLuaPath   => Path.Combine(MapDataDir, "objects.lua");
    private string SpawnPath        => Path.Combine(MapDataDir, "spawnpoints.lua");
    private string MapInfoPath      => Path.Combine(MapDataDir, "map.info");
    private string VersionedModInfoPath => Path.Combine(VersionedDir, "mod.info");
    private string CommonModInfoPath    => Path.Combine(CommonDir, "mod.info");
    private string ReportPath       => Path.Combine(VersionedDir, "experimental-map-export-report.json");

    private static bool HasBom(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
    }

    [Fact]
    public void RenderableV1_ExitsZero()
    {
        var (code, stdout, stderr) = RunCandidateRenderableV1();
        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // Folder structure: cell data under common/media/maps/, not <version>/media/maps/
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderableV1_CellDataIsUnderCommonFolder_NotVersionedFolder()
    {
        RunCandidateRenderableV1();
        Assert.True(File.Exists(LotheaderPath), $"Expected lotheader under common/: {LotheaderPath}");
        var wrongPath = Path.Combine(VersionedDir, "media", "maps", TestMapId, "35_27.lotheader");
        Assert.False(File.Exists(wrongPath), "Cell data must not be written under the versioned folder");
    }

    [Fact]
    public void RenderableV1_ModInfo_PresentInBothVersionedAndCommonFolders()
    {
        RunCandidateRenderableV1();
        Assert.True(File.Exists(VersionedModInfoPath), "mod.info must exist under 42/");
        Assert.True(File.Exists(CommonModInfoPath), "mod.info must also exist under common/");
    }

    // -----------------------------------------------------------------------
    // map.info: lots= names the parent world (Muldraugh, KY), not self-referential
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderableV1_MapInfo_LotsIsMuldraugh()
    {
        RunCandidateRenderableV1();
        var content = File.ReadAllText(MapInfoPath);
        Assert.Contains("lots=Muldraugh, KY", content, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // lotheader: 5 entries (4 real vanilla tile names + 1 distinctive marker tile),
    // matches the byte size of the confirmed-working reference file exactly (1166 bytes)
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderableV1_Lotheader_TotalSizeIs1161()
    {
        RunCandidateRenderableV1();
        // MAP-38D: marker tile changed "unofficial_fork_map_0" (21 chars) -> "floors_rugs_01_0"
        // (16 chars), 5 bytes shorter. 1166 -> 1161.
        Assert.Equal(1161L, new FileInfo(LotheaderPath).Length);
    }

    [Fact]
    public void RenderableV1_Lotheader_HasFiveEntries()
    {
        RunCandidateRenderableV1();
        var bytes = File.ReadAllBytes(LotheaderPath);
        var entryCount = BitConverter.ToUInt32(bytes, 8);
        Assert.Equal(5u, entryCount);
    }

    [Fact]
    public void RenderableV1_Lotheader_ContainsDistinctiveMarkerTile()
    {
        RunCandidateRenderableV1();
        var content = File.ReadAllText(LotheaderPath, System.Text.Encoding.ASCII);
        Assert.Contains("floors_rugs_01_0", content, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderableV1_Lotheader_ContainsRealVanillaTileNames()
    {
        RunCandidateRenderableV1();
        var content = File.ReadAllText(LotheaderPath, System.Text.Encoding.ASCII);
        Assert.Contains("blends_natural_01_16", content, StringComparison.Ordinal);
        Assert.Contains("blends_natural_01_21", content, StringComparison.Ordinal);
        Assert.Contains("blends_natural_01_22", content, StringComparison.Ordinal);
        Assert.Contains("blends_natural_01_23", content, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // lotpack: MAP-38H mixed encoding -- most of the 1024 chunks are the real
    // 8-byte Type-A "whole chunk is default" shorthand, matching the byte size
    // profile of MyMapMod's own real, authored world_31_45.lotpack (100,148
    // bytes total; this writer's prior uniform-768-byte-per-chunk shape was
    // 794,636 bytes, a 7.9x mismatch never checked against a real reference
    // until 2026-07-10). A central 16x16 block of chunks (indices where
    // 8 <= chunkX < 24 and 8 <= chunkY < 24, chunk grid is 32x32) is written
    // as full 64-explicit-record Type-B chunks referencing the marker tile,
    // guaranteed to cover the cell's spawn point (posX=posY=150 of 0-299).
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderableV1_Lotpack_SizeIs210956()
    {
        RunCandidateRenderableV1();
        // header(12) + offset_table(1024*8=8192) + 256 marker chunks*768 + 768 default chunks*8
        // = 8204 + 196608 + 6144 = 210956
        Assert.Equal(210956L, new FileInfo(LotpackPath).Length);
    }

    [Fact]
    public void RenderableV1_Lotpack_FirstChunkIsTypeADefaultShorthand()
    {
        RunCandidateRenderableV1();
        var bytes = File.ReadAllBytes(LotpackPath);
        var firstOffset = BitConverter.ToInt64(bytes, 12);
        Assert.Equal(8204L, firstOffset);
        // Chunk 0 is (chunkX=0, chunkY=0) -- outside the central 8..24 marker block,
        // so it must be the 8-byte Type-A whole-chunk-default shorthand.
        var secondOffset = BitConverter.ToInt64(bytes, 20);
        Assert.Equal(8204L + 8L, secondOffset);
        var field1 = BitConverter.ToUInt32(bytes, (int)firstOffset);
        var field2 = BitConverter.ToUInt32(bytes, (int)firstOffset + 4);
        Assert.Equal(0xFFFFFFFFu, field1);
        Assert.Equal(64u, field2); // run_length = 64 (whole chunk default)
    }

    [Fact]
    public void RenderableV1_Lotpack_CentralChunkIsTypeBExplicitMarkerTile()
    {
        RunCandidateRenderableV1();
        var bytes = File.ReadAllBytes(LotpackPath);
        // Chunk index for chunkX=16, chunkY=16 (inside the central 8..24 marker block).
        const int chunkIndex = 16 * 32 + 16;
        var offset = BitConverter.ToInt64(bytes, 12 + chunkIndex * 8);
        var field1 = BitConverter.ToUInt32(bytes, (int)offset);
        var field2 = BitConverter.ToUInt32(bytes, (int)offset + 4);
        var field3 = BitConverter.ToUInt32(bytes, (int)offset + 8);
        Assert.Equal(2u, field1);
        Assert.Equal(0xFFFFFFFFu, field2);
        Assert.Equal(4u, field3); // renderableTileIndex -- floors_rugs_01_0
    }

    [Fact]
    public void RenderableV1_Lotpack_MagicAndChunkCount()
    {
        RunCandidateRenderableV1();
        var bytes = File.ReadAllBytes(LotpackPath);
        Assert.Equal((byte)'L', bytes[0]);
        Assert.Equal((byte)'O', bytes[1]);
        Assert.Equal((byte)'T', bytes[2]);
        Assert.Equal((byte)'P', bytes[3]);
        var chunkCount = BitConverter.ToUInt32(bytes, 8);
        Assert.Equal(1024u, chunkCount);
    }

    // -----------------------------------------------------------------------
    // objects.lua / spawnpoints.lua / mod.info / map.info: no-BOM encoding
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderableV1_ObjectsLua_NoBom()
    {
        RunCandidateRenderableV1();
        Assert.False(HasBom(ObjectsLuaPath), "objects.lua must not have UTF-8 BOM");
    }

    [Fact]
    public void RenderableV1_ObjectsLua_ContainsVegitationZoneOverMarkerBlock()
    {
        // MAP-38N: real vanilla Muldraugh's own objects.lua uses "Vegitation" zone
        // objects to seed procedural vegetation density. This zone covers the same
        // tile range (cellX*256+64 .. +191) as the lotpack's central marker-tile
        // block (MAP-38H).
        var (cellX, cellY) = (35, 27); // RunCandidateRenderableV1's default cell
        RunCandidateRenderableV1();
        var content = File.ReadAllText(ObjectsLuaPath, System.Text.Encoding.ASCII);
        Assert.Contains("type = \"Vegitation\"", content, StringComparison.Ordinal);
        Assert.Contains($"x = {cellX * 256 + 64}", content, StringComparison.Ordinal);
        Assert.Contains($"y = {cellY * 256 + 64}", content, StringComparison.Ordinal);
        Assert.Contains("width = 128, height = 128", content, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderableV1_ObjectsLua_ContainsSecondVegitationZoneOverUntouchedGround()
    {
        // MAP-38P: second Vegitation zone over tiles 0..63 (chunkX/Y 0..7), which
        // are Type-A default-shorthand in the lotpack (outside the central 8..24
        // marker block) -- tests whether zones affect otherwise-unauthored ground
        // differently than already-explicit lotpack records (MAP-38O hypothesis).
        var (cellX, cellY) = (35, 27);
        RunCandidateRenderableV1();
        var content = File.ReadAllText(ObjectsLuaPath, System.Text.Encoding.ASCII);
        Assert.Contains($"x = {cellX * 256 + 0}, y = {cellY * 256 + 0}", content, StringComparison.Ordinal);
        Assert.Contains("width = 64, height = 64", content, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderableV1_SpawnpointsLua_NoBom()
    {
        RunCandidateRenderableV1();
        Assert.False(HasBom(SpawnPath), "spawnpoints.lua must not have UTF-8 BOM");
    }

    [Fact]
    public void RenderableV1_MapInfo_NoBom()
    {
        RunCandidateRenderableV1();
        Assert.False(HasBom(MapInfoPath), "map.info must not have UTF-8 BOM");
    }

    [Fact]
    public void RenderableV1_SpawnpointsLua_HasWorldAndPosCoordinates()
    {
        RunCandidateRenderableV1();
        var content = File.ReadAllText(SpawnPath);
        Assert.Contains("worldX", content, StringComparison.Ordinal);
        Assert.Contains("worldY", content, StringComparison.Ordinal);
        Assert.Contains("posX", content, StringComparison.Ordinal);
        Assert.Contains("posY", content, StringComparison.Ordinal);
        Assert.Contains("posZ", content, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Report: honest claim boundary preserved (not load-tested, not playable-claimed)
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderableV1_Report_ProfileIsRenderableV1()
    {
        RunCandidateRenderableV1();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.Equal("renderable_v1", doc.RootElement.GetProperty("build42_candidate_profile").GetString());
    }

    [Fact]
    public void RenderableV1_Report_LoadTestedFalse()
    {
        RunCandidateRenderableV1();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.False(doc.RootElement.GetProperty("load_tested").GetBoolean());
    }

    [Fact]
    public void RenderableV1_Report_PlayableExportClaimedFalse()
    {
        RunCandidateRenderableV1();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.False(doc.RootElement.GetProperty("playable_export_claimed").GetBoolean());
    }

    [Fact]
    public void RenderableV1_Report_PzAssetsCopiedFalse()
    {
        RunCandidateRenderableV1();
        var doc = JsonDocument.Parse(File.ReadAllText(ReportPath));
        Assert.False(doc.RootElement.GetProperty("pz_assets_copied").GetBoolean());
    }

    [Fact]
    public void RenderableV1_Report_FilesWrittenListsCommonPaths()
    {
        RunCandidateRenderableV1();
        var json = File.ReadAllText(ReportPath);
        Assert.Contains("common/media/maps/", json, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Unknown profile still rejected (guards the profile allow-list edit)
    // -----------------------------------------------------------------------

    [Fact]
    public void UnknownProfile_StillExitsNonzero()
    {
        var (code, _, stderr) = RunCli(
            "map-export-experimental",
            "--map-id", TestMapId,
            "--output", OutputBase,
            "--build42-candidate-writer",
            "--build42-candidate-profile", "not_a_real_profile",
            "--cell-x", "35",
            "--cell-y", "27");
        Assert.NotEqual(0, code);
        Assert.Contains("renderable_v1", stderr, StringComparison.Ordinal);
    }
}
