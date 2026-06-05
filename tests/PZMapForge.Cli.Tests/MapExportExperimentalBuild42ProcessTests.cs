using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

/// <summary>
/// Process-level tests for the map-export-experimental --build42-package flag (MAP-5D).
/// Verifies that the command generates a correct Build 42 Workshop-style nested package
/// layout under .local only.
/// Claim boundary: experimental_local_only_not_playable_not_load_tested
/// No playable export. No PZ assets. No load test.
/// MAP-5B remains LOAD_TEST_INCONCLUSIVE. Binary hypotheses remain UNTESTED.
/// </summary>
public sealed class MapExportExperimentalBuild42ProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map-export-b42-tests", Path.GetRandomFileName());

    public MapExportExperimentalBuild42ProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private string OutputBase => Path.Combine(_tempDir, ".local", "map-export-experimental");

    private const string TestMapId = "pzmapforge_b42_test";

    private string PkgRoot => Path.Combine(OutputBase, TestMapId + "_build42_workshop");

    private string ModRoot => Path.Combine(PkgRoot, "Contents", "mods", TestMapId);

    private string MapDataDir => Path.Combine(ModRoot, "media", "maps", TestMapId);

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

    // -----------------------------------------------------------------------
    // Test 1: --build42-package flag exits 0 and creates package root
    // -----------------------------------------------------------------------

    [Fact]
    public void Build42Package_ValidArgs_ExitsZeroAndCreatesPkgRoot()
    {
        var (code, stdout, stderr) = RunCli(
            "map-export-experimental",
            "--map-id", TestMapId,
            "--output", OutputBase,
            "--build42-package");

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
        Assert.True(Directory.Exists(PkgRoot));
        Assert.Contains("build42_workshop", stdout, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 2: workshop.txt exists at package root
    // -----------------------------------------------------------------------

    [Fact]
    public void Build42Package_CreatesWorkshopTxtAtRoot()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId,
               "--output", OutputBase, "--build42-package");

        Assert.True(File.Exists(Path.Combine(PkgRoot, "workshop.txt")));
    }

    // -----------------------------------------------------------------------
    // Test 3: preview.png exists at package root
    // -----------------------------------------------------------------------

    [Fact]
    public void Build42Package_CreatesPreviewPngAtRoot()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId,
               "--output", OutputBase, "--build42-package");

        Assert.True(File.Exists(Path.Combine(PkgRoot, "preview.png")));
    }

    // -----------------------------------------------------------------------
    // Test 4: nested mod.info exists under Contents/mods/<id>/
    // -----------------------------------------------------------------------

    [Fact]
    public void Build42Package_CreatesNestedModInfo()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId,
               "--output", OutputBase, "--build42-package");

        Assert.True(File.Exists(Path.Combine(ModRoot, "mod.info")));
    }

    // -----------------------------------------------------------------------
    // Test 5: nested mod.info contains category=map
    // -----------------------------------------------------------------------

    [Fact]
    public void Build42Package_ModInfoContainsCategoryMap()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId,
               "--output", OutputBase, "--build42-package");

        var content = File.ReadAllText(Path.Combine(ModRoot, "mod.info"));
        Assert.Contains("category=map", content, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 6: lotheader is exactly 8 bytes at nested path
    // -----------------------------------------------------------------------

    [Fact]
    public void Build42Package_Lotheader_IsExactly8Bytes()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId,
               "--output", OutputBase, "--build42-package");

        var path = Path.Combine(MapDataDir, "0_0.lotheader");
        Assert.True(File.Exists(path));
        Assert.Equal(8L, new FileInfo(path).Length);
    }

    // -----------------------------------------------------------------------
    // Test 7: lotpack is exactly 7208 bytes at nested path
    // -----------------------------------------------------------------------

    [Fact]
    public void Build42Package_Lotpack_IsExactly7208Bytes()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId,
               "--output", OutputBase, "--build42-package");

        var path = Path.Combine(MapDataDir, "world_0_0.lotpack");
        Assert.True(File.Exists(path));
        Assert.Equal(7208L, new FileInfo(path).Length);
    }

    // -----------------------------------------------------------------------
    // Test 8: lotpack first 8 bytes match known header
    // -----------------------------------------------------------------------

    [Fact]
    public void Build42Package_Lotpack_First8BytesMatchKnownHeader()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId,
               "--output", OutputBase, "--build42-package");

        var bytes = File.ReadAllBytes(Path.Combine(MapDataDir, "world_0_0.lotpack"));
        Assert.Equal(0x84, bytes[0]);
        Assert.Equal(0x03, bytes[1]);
        Assert.Equal(0x00, bytes[2]);
        Assert.Equal(0x00, bytes[3]);
        Assert.Equal(0x24, bytes[4]);
        Assert.Equal(0x1C, bytes[5]);
        Assert.Equal(0x00, bytes[6]);
        Assert.Equal(0x00, bytes[7]);
    }

    // -----------------------------------------------------------------------
    // Test 9: chunkdata is exactly 902 bytes at nested path
    // -----------------------------------------------------------------------

    [Fact]
    public void Build42Package_Chunkdata_IsExactly902Bytes()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId,
               "--output", OutputBase, "--build42-package");

        var path = Path.Combine(MapDataDir, "chunkdata_0_0.bin");
        Assert.True(File.Exists(path));
        Assert.Equal(902L, new FileInfo(path).Length);
    }

    // -----------------------------------------------------------------------
    // Test 10: chunkdata first 2 bytes match 0001
    // -----------------------------------------------------------------------

    [Fact]
    public void Build42Package_Chunkdata_First2BytesMatch0001()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId,
               "--output", OutputBase, "--build42-package");

        var bytes = File.ReadAllBytes(Path.Combine(MapDataDir, "chunkdata_0_0.bin"));
        Assert.Equal(0x00, bytes[0]);
        Assert.Equal(0x01, bytes[1]);
    }

    // -----------------------------------------------------------------------
    // Test 11: exactly 14 files under the package root
    // -----------------------------------------------------------------------

    [Fact]
    public void Build42Package_ExactlyFourteenFilesUnderPkgRoot()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId,
               "--output", OutputBase, "--build42-package");

        var files = Directory.GetFiles(PkgRoot, "*", SearchOption.AllDirectories);
        Assert.Equal(14, files.Length);
    }

    // -----------------------------------------------------------------------
    // Test 12: report JSON has package_layout = build42_workshop
    // -----------------------------------------------------------------------

    [Fact]
    public void Build42Package_ReportJson_HasBuild42Layout()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId,
               "--output", OutputBase, "--build42-package");

        var doc  = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(ModRoot, "experimental-map-export-report.json")));
        var root = doc.RootElement;

        Assert.Equal("build42_workshop", root.GetProperty("package_layout").GetString());
        Assert.False(root.GetProperty("playable_export_generated").GetBoolean());
        Assert.False(root.GetProperty("load_tested").GetBoolean());
        Assert.True(root.GetProperty("experimental_writer").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 13: --build42-package with newline_tileset_table_minimal
    //          produces a 34-byte lotheader in the nested map directory.
    // MAP-6D: build42 path supports the non-empty candidate.
    // -----------------------------------------------------------------------

    [Fact]
    public void Build42Package_MinimalLotheaderCandidate_LotheaderIs34Bytes()
    {
        var (code, stdout, stderr) = RunCli(
            "map-export-experimental", "--map-id", TestMapId,
            "--output", OutputBase, "--build42-package",
            "--lotheader-candidate", "newline_tileset_table_minimal");

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
        var path = Path.Combine(MapDataDir, "0_0.lotheader");
        Assert.True(File.Exists(path));
        Assert.Equal(34L, new FileInfo(path).Length);
    }
}
