using System.Diagnostics;
using System.Text;
using Xunit;

namespace PZMapForge.Cli.Tests;

/// <summary>
/// Process-level tests for the map-scaffold CLI command (MAP-3B).
/// map-scaffold is a text-only local scaffold writer: reads a map source JSON file
/// and writes exactly four text files under a .local output directory.
/// Claim boundary: map_scaffold_text_only_not_compiled_not_pz_load_tested
/// No compiled outputs. No PZ assets. No playable export.
/// </summary>
public sealed class MapScaffoldProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map-scaffold-tests", Path.GetRandomFileName());

    public MapScaffoldProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private static string MinimalCellExample =>
        Path.Combine(RepoRoot, "examples", "map-source", "minimal-cell.json");

    private string OutputDir => Path.Combine(_tempDir, ".local", "map-scaffold");

    private const string MinimalCellMapId = "deadmtl_minimal_test";

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

    private string WriteSource(string content)
    {
        var path = Path.Combine(_tempDir, $"source-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    // -----------------------------------------------------------------------
    // Test 1: valid minimal-cell.json exits 0 and writes exactly four files
    // -----------------------------------------------------------------------

    [Fact]
    public void MapScaffold_ValidMinimalCell_ExitsZeroAndWritesFourFiles()
    {
        var (code, stdout, stderr) = RunCli(
            "map-scaffold",
            "--source", MinimalCellExample,
            "--output", OutputDir);

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
        Assert.Contains("Status:                     OK", stdout, StringComparison.Ordinal);

        var allFiles = Directory.GetFiles(OutputDir, "*", SearchOption.AllDirectories);
        Assert.Equal(4, allFiles.Length);
    }

    // -----------------------------------------------------------------------
    // Test 2: writes mod.info
    // -----------------------------------------------------------------------

    [Fact]
    public void MapScaffold_WritesModInfo()
    {
        RunCli("map-scaffold", "--source", MinimalCellExample, "--output", OutputDir);

        Assert.True(File.Exists(Path.Combine(OutputDir, "mod.info")));
    }

    // -----------------------------------------------------------------------
    // Test 3: writes media/maps/<map_id>/map.info
    // -----------------------------------------------------------------------

    [Fact]
    public void MapScaffold_WritesMapInfo()
    {
        RunCli("map-scaffold", "--source", MinimalCellExample, "--output", OutputDir);

        var expected = Path.Combine(OutputDir, "media", "maps", MinimalCellMapId, "map.info");
        Assert.True(File.Exists(expected));
    }

    // -----------------------------------------------------------------------
    // Test 4: writes media/maps/<map_id>/spawnpoints.lua
    // -----------------------------------------------------------------------

    [Fact]
    public void MapScaffold_WritesSpawnpointsLua()
    {
        RunCli("map-scaffold", "--source", MinimalCellExample, "--output", OutputDir);

        var expected = Path.Combine(OutputDir, "media", "maps", MinimalCellMapId, "spawnpoints.lua");
        Assert.True(File.Exists(expected));
    }

    // -----------------------------------------------------------------------
    // Test 5: writes media/maps/<map_id>/README_PZMAPFORGE_BOUNDARY.txt
    // -----------------------------------------------------------------------

    [Fact]
    public void MapScaffold_WritesReadmeBoundaryTxt()
    {
        RunCli("map-scaffold", "--source", MinimalCellExample, "--output", OutputDir);

        var expected = Path.Combine(OutputDir, "media", "maps", MinimalCellMapId, "README_PZMAPFORGE_BOUNDARY.txt");
        Assert.True(File.Exists(expected));
    }

    // -----------------------------------------------------------------------
    // Test 6: generated files contain required boundary language
    // -----------------------------------------------------------------------

    [Fact]
    public void MapScaffold_GeneratedFilesContainBoundaryLanguage()
    {
        RunCli("map-scaffold", "--source", MinimalCellExample, "--output", OutputDir);

        var mapDir = Path.Combine(OutputDir, "media", "maps", MinimalCellMapId);

        var modInfo     = File.ReadAllText(Path.Combine(OutputDir, "mod.info"));
        var mapInfo     = File.ReadAllText(Path.Combine(mapDir, "map.info"));
        var spawnpoints = File.ReadAllText(Path.Combine(mapDir, "spawnpoints.lua"));
        var readme      = File.ReadAllText(Path.Combine(mapDir, "README_PZMAPFORGE_BOUNDARY.txt"));

        // Text-only scaffold phrase
        Assert.True(
            modInfo.Contains("Text-only scaffold",      StringComparison.OrdinalIgnoreCase) ||
            mapInfo.Contains("text-only scaffold",      StringComparison.OrdinalIgnoreCase) ||
            readme.Contains("Text-only scaffold",       StringComparison.OrdinalIgnoreCase) ||
            spawnpoints.Contains("text-only scaffold",  StringComparison.OrdinalIgnoreCase),
            "At least one file must contain 'Text-only scaffold'");

        // Not playable phrase
        Assert.True(
            modInfo.Contains("Not playable",      StringComparison.OrdinalIgnoreCase) ||
            readme.Contains("Not a playable",     StringComparison.OrdinalIgnoreCase),
            "At least one file must contain 'Not playable' or 'Not a playable'");

        // No compiled map files / no lotpack phrase
        Assert.True(
            modInfo.Contains("No compiled map files", StringComparison.OrdinalIgnoreCase) ||
            readme.Contains("No lotpack",             StringComparison.OrdinalIgnoreCase) ||
            readme.Contains("No compiled outputs",    StringComparison.OrdinalIgnoreCase),
            "At least one file must contain no-compiled-output language");

        // No PZ assets phrase
        Assert.True(
            modInfo.Contains("No PZ assets",     StringComparison.OrdinalIgnoreCase) ||
            mapInfo.Contains("pz_assets_included=false", StringComparison.OrdinalIgnoreCase) ||
            readme.Contains("No PZ assets",      StringComparison.OrdinalIgnoreCase),
            "At least one file must contain 'No PZ assets'");

        // Not load-tested phrase
        Assert.True(
            modInfo.Contains("Not load-tested",     StringComparison.OrdinalIgnoreCase) ||
            mapInfo.Contains("Not load-tested",     StringComparison.OrdinalIgnoreCase) ||
            spawnpoints.Contains("Not load-tested", StringComparison.OrdinalIgnoreCase) ||
            readme.Contains("Not load-tested",      StringComparison.OrdinalIgnoreCase),
            "At least one file must contain 'Not load-tested'");
    }

    // -----------------------------------------------------------------------
    // Test 7: stdout contains required safety lines
    // -----------------------------------------------------------------------

    [Fact]
    public void MapScaffold_Stdout_ContainsSafetyLines()
    {
        var (_, stdout, _) = RunCli(
            "map-scaffold",
            "--source", MinimalCellExample,
            "--output", OutputDir);

        Assert.Contains("text_only_scaffold_written: true",  stdout, StringComparison.Ordinal);
        Assert.Contains("compiled_outputs_written:   false", stdout, StringComparison.Ordinal);
        Assert.Contains("playable_export_generated:  false", stdout, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 8: no compiled output files exist under output
    // -----------------------------------------------------------------------

    [Fact]
    public void MapScaffold_NoCompiledOutputFilesExistUnderOutput()
    {
        RunCli("map-scaffold", "--source", MinimalCellExample, "--output", OutputDir);

        var allFiles = Directory.GetFiles(OutputDir, "*", SearchOption.AllDirectories);
        var forbidden = new[] { ".lotpack", ".lotheader", ".bin", ".tmx", ".pzw" };
        var bad = allFiles
            .Where(f => forbidden.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToList();

        Assert.Empty(bad);
    }

    // -----------------------------------------------------------------------
    // Test 9: missing --source exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void MapScaffold_MissingSource_ExitsNonZero()
    {
        var (code, _, stderr) = RunCli("map-scaffold", "--output", OutputDir);

        Assert.NotEqual(0, code);
        Assert.Contains("--source", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 10: source file not found exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void MapScaffold_SourceFileNotFound_ExitsNonZero()
    {
        var (code, _, stderr) = RunCli(
            "map-scaffold",
            "--source", Path.Combine(_tempDir, "nonexistent.json"),
            "--output", OutputDir);

        Assert.NotEqual(0, code);
        Assert.Contains("not found", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 11: invalid JSON exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void MapScaffold_InvalidJson_ExitsNonZero()
    {
        var path = WriteSource("not valid json {{{");
        var (code, _, stderr) = RunCli("map-scaffold", "--source", path, "--output", OutputDir);

        Assert.NotEqual(0, code);
        Assert.Contains("JSON", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 12: wrong schema exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void MapScaffold_WrongSchema_ExitsNonZero()
    {
        var path = WriteSource(
            """{"schema":"wrong.schema","claim_boundary":"map_source_only_not_exported_not_pz_load_tested","map_id":"x","format_version":"0.1","cell_size":300,"cells":[{"cell_id":"c","x":0,"y":0,"terrain":"grass","spawn_points":[],"zones":[]}]}""");

        var (code, _, stderr) = RunCli("map-scaffold", "--source", path, "--output", OutputDir);

        Assert.NotEqual(0, code);
        Assert.Contains("schema", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 13: wrong claim_boundary exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void MapScaffold_WrongClaimBoundary_ExitsNonZero()
    {
        var path = WriteSource(
            """{"schema":"pzmapforge.map-source.v0.1","claim_boundary":"wrong","map_id":"x","format_version":"0.1","cell_size":300,"cells":[{"cell_id":"c","x":0,"y":0,"terrain":"grass","spawn_points":[],"zones":[]}]}""");

        var (code, _, stderr) = RunCli("map-scaffold", "--source", path, "--output", OutputDir);

        Assert.NotEqual(0, code);
        Assert.Contains("claim_boundary", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 14: output outside .local exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void MapScaffold_OutputOutsideLocal_ExitsNonZero()
    {
        var badOutput = Path.Combine(_tempDir, "not-local", "mymap");

        var (code, _, stderr) = RunCli(
            "map-scaffold",
            "--source", MinimalCellExample,
            "--output", badOutput);

        Assert.NotEqual(0, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 15: direct media/maps output outside .local exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void MapScaffold_MediaMapsOutput_ExitsNonZero()
    {
        var badOutput = Path.Combine(_tempDir, "media", "maps", "mymap");

        var (code, _, stderr) = RunCli(
            "map-scaffold",
            "--source", MinimalCellExample,
            "--output", badOutput);

        Assert.NotEqual(0, code);
        Assert.Contains("media", stderr, StringComparison.OrdinalIgnoreCase);
    }
}
