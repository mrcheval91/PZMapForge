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
    public void RenderableV1_Lotheader_TotalSizeIs1166()
    {
        RunCandidateRenderableV1();
        Assert.Equal(1166L, new FileInfo(LotheaderPath).Length);
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
        Assert.Contains("unofficial_fork_map_0", content, StringComparison.Ordinal);
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
    // lotpack: 1024 chunks x 768 bytes (64 explicit 12-byte tile records/chunk),
    // matching the shape used in the confirmed-working test
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderableV1_Lotpack_SizeIs794636()
    {
        RunCandidateRenderableV1();
        Assert.Equal(794636L, new FileInfo(LotpackPath).Length);
    }

    [Fact]
    public void RenderableV1_Lotpack_FirstChunkIs768BytesOfExplicitTileRecords()
    {
        RunCandidateRenderableV1();
        var bytes = File.ReadAllBytes(LotpackPath);
        var firstOffset = BitConverter.ToInt64(bytes, 12);
        Assert.Equal(8204L, firstOffset);
        var secondOffset = BitConverter.ToInt64(bytes, 20);
        Assert.Equal(8204L + 768L, secondOffset);

        // First record of the first chunk: [U32=2][U32=0xFFFFFFFF][U32=tile_index=4]
        var field1 = BitConverter.ToUInt32(bytes, (int)firstOffset);
        var field2 = BitConverter.ToUInt32(bytes, (int)firstOffset + 4);
        var field3 = BitConverter.ToUInt32(bytes, (int)firstOffset + 8);
        Assert.Equal(2u, field1);
        Assert.Equal(0xFFFFFFFFu, field2);
        Assert.Equal(4u, field3); // renderableTileIndex -- unofficial_fork_map_0
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
