using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

/// <summary>
/// Process-level tests for the map-export-experimental CLI command (MAP-5A).
/// map-export-experimental is an experimental local-only compiled empty cell writer.
/// Authorized by MAP-4H: MAP-5A_ALLOWED_EXPERIMENTAL_LOCAL_ONLY.
/// Writes hypothesis-only binary files under .local only.
/// Claim boundary: experimental_local_only_not_playable_not_load_tested
/// No playable export. No PZ assets. No load test.
/// </summary>
public sealed class MapExportExperimentalProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map-export-experimental-tests", Path.GetRandomFileName());

    public MapExportExperimentalProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private string OutputDir => Path.Combine(_tempDir, ".local", "map-export-experimental");

    private const string TestMapId = "pzmapforge_test_cell";

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
    // Test 1: valid args exit 0 and write expected files
    // -----------------------------------------------------------------------

    [Fact]
    public void MapExportExperimental_ValidArgs_ExitsZeroAndWritesExpectedFiles()
    {
        var (code, stdout, stderr) = RunCli(
            "map-export-experimental",
            "--map-id", TestMapId,
            "--output", OutputDir);

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
        Assert.True(File.Exists(Path.Combine(OutputDir, "mod.info")));
        Assert.True(File.Exists(Path.Combine(OutputDir, "media", "maps", TestMapId, "map.info")));
        Assert.True(File.Exists(Path.Combine(OutputDir, "experimental-map-export-report.json")));
        Assert.True(File.Exists(Path.Combine(OutputDir, "experimental-map-export-report.md")));
    }

    // -----------------------------------------------------------------------
    // Test 2: lotheader is exactly 8 bytes
    // -----------------------------------------------------------------------

    [Fact]
    public void MapExportExperimental_Lotheader_IsExactly8Bytes()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId, "--output", OutputDir);

        var path = Path.Combine(OutputDir, "media", "maps", TestMapId, "0_0.lotheader");
        Assert.True(File.Exists(path));
        Assert.Equal(8L, new FileInfo(path).Length);
    }

    // -----------------------------------------------------------------------
    // Test 3: lotpack is exactly 7208 bytes
    // -----------------------------------------------------------------------

    [Fact]
    public void MapExportExperimental_Lotpack_IsExactly7208Bytes()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId, "--output", OutputDir);

        var path = Path.Combine(OutputDir, "media", "maps", TestMapId, "world_0_0.lotpack");
        Assert.True(File.Exists(path));
        Assert.Equal(7208L, new FileInfo(path).Length);
    }

    // -----------------------------------------------------------------------
    // Test 4: chunkdata is exactly 902 bytes
    // -----------------------------------------------------------------------

    [Fact]
    public void MapExportExperimental_Chunkdata_IsExactly902Bytes()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId, "--output", OutputDir);

        var path = Path.Combine(OutputDir, "media", "maps", TestMapId, "chunkdata_0_0.bin");
        Assert.True(File.Exists(path));
        Assert.Equal(902L, new FileInfo(path).Length);
    }

    // -----------------------------------------------------------------------
    // Test 5: lotpack first 8 bytes match 84030000241c0000
    // -----------------------------------------------------------------------

    [Fact]
    public void MapExportExperimental_Lotpack_First8BytesMatchKnownHeader()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId, "--output", OutputDir);

        var path  = Path.Combine(OutputDir, "media", "maps", TestMapId, "world_0_0.lotpack");
        var bytes = File.ReadAllBytes(path);

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
    // Test 6: chunkdata first 2 bytes match 0001
    // -----------------------------------------------------------------------

    [Fact]
    public void MapExportExperimental_Chunkdata_First2BytesMatch0001()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId, "--output", OutputDir);

        var path  = Path.Combine(OutputDir, "media", "maps", TestMapId, "chunkdata_0_0.bin");
        var bytes = File.ReadAllBytes(path);

        Assert.Equal(0x00, bytes[0]);
        Assert.Equal(0x01, bytes[1]);
    }

    // -----------------------------------------------------------------------
    // Test 7: boundary README contains EXPERIMENTAL OUTPUT phrase
    // -----------------------------------------------------------------------

    [Fact]
    public void MapExportExperimental_BoundaryReadme_ContainsExperimentalOutputPhrase()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId, "--output", OutputDir);

        var path    = Path.Combine(OutputDir, "media", "maps", TestMapId,
                                   "README_PZMAPFORGE_BOUNDARY_EXPERIMENTAL.txt");
        var content = File.ReadAllText(path);
        Assert.Contains("EXPERIMENTAL OUTPUT", content, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 8: boundary README contains Not a playable Project Zomboid map
    // -----------------------------------------------------------------------

    [Fact]
    public void MapExportExperimental_BoundaryReadme_ContainsNotPlayablePhrase()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId, "--output", OutputDir);

        var path    = Path.Combine(OutputDir, "media", "maps", TestMapId,
                                   "README_PZMAPFORGE_BOUNDARY_EXPERIMENTAL.txt");
        var content = File.ReadAllText(path);
        Assert.Contains("Not a playable Project Zomboid map", content, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 9: report JSON has playable_export_generated false
    // -----------------------------------------------------------------------

    [Fact]
    public void MapExportExperimental_ReportJson_PlayableExportGeneratedFalse()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId, "--output", OutputDir);

        var doc  = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(OutputDir, "experimental-map-export-report.json")));
        var root = doc.RootElement;

        Assert.False(root.GetProperty("playable_export_generated").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 10: report JSON has load_tested false
    // -----------------------------------------------------------------------

    [Fact]
    public void MapExportExperimental_ReportJson_LoadTestedFalse()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId, "--output", OutputDir);

        var doc  = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(OutputDir, "experimental-map-export-report.json")));
        Assert.False(doc.RootElement.GetProperty("load_tested").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 11: report JSON has experimental_writer true
    // -----------------------------------------------------------------------

    [Fact]
    public void MapExportExperimental_ReportJson_ExperimentalWriterTrue()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId, "--output", OutputDir);

        var doc  = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(OutputDir, "experimental-map-export-report.json")));
        Assert.True(doc.RootElement.GetProperty("experimental_writer").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 12: no extra files — exactly 10 files under output
    // -----------------------------------------------------------------------

    [Fact]
    public void MapExportExperimental_OutputDir_ContainsExactly10Files()
    {
        RunCli("map-export-experimental", "--map-id", TestMapId, "--output", OutputDir);

        var files = Directory.GetFiles(OutputDir, "*", SearchOption.AllDirectories);
        Assert.Equal(10, files.Length);
    }

    // -----------------------------------------------------------------------
    // Test 13: missing --map-id exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void MapExportExperimental_MissingMapId_ExitsNonZero()
    {
        var (code, _, stderr) = RunCli("map-export-experimental", "--output", OutputDir);

        Assert.NotEqual(0, code);
        Assert.Contains("--map-id", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 14: output outside .local exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void MapExportExperimental_OutputOutsideLocal_ExitsNonZero()
    {
        var badOutput = Path.Combine(_tempDir, "not-local", "output");

        var (code, _, stderr) = RunCli(
            "map-export-experimental",
            "--map-id", TestMapId,
            "--output", badOutput);

        Assert.NotEqual(0, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 15: output in repo-level media/maps exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void MapExportExperimental_OutputInMediaMaps_ExitsNonZero()
    {
        var badOutput = Path.Combine(_tempDir, "media", "maps", "mymap");

        var (code, _, stderr) = RunCli(
            "map-export-experimental",
            "--map-id", TestMapId,
            "--output", badOutput);

        Assert.NotEqual(0, code);
        Assert.Contains("media", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 16: ProjectZomboid install-like path exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void MapExportExperimental_ProjectZomboidPath_ExitsNonZero()
    {
        var badOutput = Path.Combine(_tempDir, "steamapps", "common",
                                     "ProjectZomboid", "mods");

        var (code, _, stderr) = RunCli(
            "map-export-experimental",
            "--map-id", TestMapId,
            "--output", badOutput);

        Assert.NotEqual(0, code);
        Assert.Contains("PZ install", stderr, StringComparison.OrdinalIgnoreCase);
    }
}
