using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

/// <summary>
/// Process-level tests for the map-plan CLI command.
/// map-plan is a dry-run-only command: reads a map source JSON file and writes
/// inert planning artifacts. No playable export is produced.
/// Claim boundary: map_plan_only_not_exported_not_pz_load_tested
/// </summary>
public sealed class MapPlanProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map-plan-tests", Path.GetRandomFileName());

    public MapPlanProcessTests() => Directory.CreateDirectory(_tempDir);

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

    private string OutputDir => Path.Combine(_tempDir, ".local", "map-plan");

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
    // Test 1: valid minimal-cell.json exits 0 and writes both artifacts
    // -----------------------------------------------------------------------

    [Fact]
    public void MapPlan_ValidMinimalCell_ExitsZeroAndWritesArtifacts()
    {
        var (code, stdout, stderr) = RunCli(
            "map-plan",
            "--source", MinimalCellExample,
            "--output", OutputDir);

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
        Assert.True(File.Exists(Path.Combine(OutputDir, "map-export-plan.json")));
        Assert.True(File.Exists(Path.Combine(OutputDir, "map-export-plan.md")));
        Assert.Contains("Status:                   OK", stdout, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 2: JSON contains required safety fields
    // -----------------------------------------------------------------------

    [Fact]
    public void MapPlan_Json_ContainsRequiredSafetyFields()
    {
        RunCli("map-plan", "--source", MinimalCellExample, "--output", OutputDir);

        var doc  = JsonDocument.Parse(File.ReadAllText(Path.Combine(OutputDir, "map-export-plan.json")));
        var root = doc.RootElement;

        Assert.Equal("pzmapforge.map-export-plan.v0.1",   root.GetProperty("schema").GetString());
        Assert.Equal("deadmtl_minimal_test",              root.GetProperty("map_id").GetString());
        Assert.True (root.GetProperty("dry_run").GetBoolean());
        Assert.False(root.GetProperty("execute_supported").GetBoolean());
        Assert.False(root.GetProperty("playable_export_generated").GetBoolean());
        Assert.False(root.GetProperty("media_maps_touched").GetBoolean());
        Assert.False(root.GetProperty("pz_assets_read_or_copied").GetBoolean());
        Assert.False(root.GetProperty("compiled_outputs_written").GetBoolean());
        Assert.False(root.GetProperty("local_mod_scaffold_written").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 3: markdown contains dry-run and non-claim text
    // -----------------------------------------------------------------------

    [Fact]
    public void MapPlan_Markdown_ContainsDryRunAndNonClaims()
    {
        RunCli("map-plan", "--source", MinimalCellExample, "--output", OutputDir);

        var content = File.ReadAllText(Path.Combine(OutputDir, "map-export-plan.md"));

        Assert.Contains("Dry-run", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No playable Project Zomboid export generated", content, StringComparison.Ordinal);
        Assert.Contains("No media/maps writes", content, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 4: missing --source exits 1
    // -----------------------------------------------------------------------

    [Fact]
    public void MapPlan_MissingSource_ExitsOne()
    {
        var (code, _, stderr) = RunCli("map-plan", "--output", OutputDir);

        Assert.Equal(1, code);
        Assert.Contains("--source", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 5: source file not found exits 1
    // -----------------------------------------------------------------------

    [Fact]
    public void MapPlan_SourceFileNotFound_ExitsOne()
    {
        var (code, _, stderr) = RunCli(
            "map-plan",
            "--source", Path.Combine(_tempDir, "nonexistent.json"),
            "--output", OutputDir);

        Assert.Equal(1, code);
        Assert.Contains("not found", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 6: wrong schema exits 1
    // -----------------------------------------------------------------------

    [Fact]
    public void MapPlan_WrongSchema_ExitsOne()
    {
        var path = WriteSource(
            """{"schema":"wrong.schema","claim_boundary":"map_source_only_not_exported_not_pz_load_tested","map_id":"x","format_version":"0.1","cell_size":300,"cells":[{"cell_id":"c","x":0,"y":0,"terrain":"grass","spawn_points":[],"zones":[]}]}""");

        var (code, _, stderr) = RunCli("map-plan", "--source", path, "--output", OutputDir);

        Assert.Equal(1, code);
        Assert.Contains("schema", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 7: wrong claim_boundary exits 1
    // -----------------------------------------------------------------------

    [Fact]
    public void MapPlan_WrongClaimBoundary_ExitsOne()
    {
        var path = WriteSource(
            """{"schema":"pzmapforge.map-source.v0.1","claim_boundary":"wrong","map_id":"x","format_version":"0.1","cell_size":300,"cells":[{"cell_id":"c","x":0,"y":0,"terrain":"grass","spawn_points":[],"zones":[]}]}""");

        var (code, _, stderr) = RunCli("map-plan", "--source", path, "--output", OutputDir);

        Assert.Equal(1, code);
        Assert.Contains("claim_boundary", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 8: output path containing media/maps exits 1
    // -----------------------------------------------------------------------

    [Fact]
    public void MapPlan_MediaMapsOutput_ExitsOne()
    {
        var badOutput = Path.Combine(_tempDir, "media", "maps", "mymap");

        var (code, _, stderr) = RunCli(
            "map-plan",
            "--source", MinimalCellExample,
            "--output", badOutput);

        Assert.Equal(1, code);
        Assert.Contains("media", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 9: invalid JSON exits 1
    // -----------------------------------------------------------------------

    [Fact]
    public void MapPlan_InvalidJson_ExitsOne()
    {
        var path = WriteSource("not valid json {{{");
        var (code, _, stderr) = RunCli("map-plan", "--source", path, "--output", OutputDir);

        Assert.Equal(1, code);
        Assert.Contains("JSON", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 10: JSON contains MAP-3A scaffold contract fields
    // -----------------------------------------------------------------------

    [Fact]
    public void MapPlan_Json_ContainsScaffoldContractFields()
    {
        RunCli("map-plan", "--source", MinimalCellExample, "--output", OutputDir);

        var doc  = JsonDocument.Parse(File.ReadAllText(Path.Combine(OutputDir, "map-export-plan.json")));
        var root = doc.RootElement;

        Assert.Equal("0.1",  root.GetProperty("scaffold_contract_version").GetString());
        Assert.False(root.GetProperty("text_only_scaffold_supported_now").GetBoolean());
        Assert.False(root.GetProperty("text_only_scaffold_written").GetBoolean());
        Assert.False(root.GetProperty("scaffold_execute_supported").GetBoolean());

        var files = root.GetProperty("future_scaffold_files").EnumerateArray().ToList();
        Assert.NotEmpty(files);

        var paths = files.Select(f => f.GetProperty("path").GetString() ?? string.Empty).ToList();
        Assert.Contains(paths, p => p.Contains("mod.info",    StringComparison.OrdinalIgnoreCase));
        Assert.Contains(paths, p => p.Contains("media/maps",  StringComparison.OrdinalIgnoreCase));

        foreach (var f in files)
            Assert.False(f.GetProperty("written_now").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 11: Markdown contains scaffold contract section
    // -----------------------------------------------------------------------

    [Fact]
    public void MapPlan_Markdown_ContainsScaffoldContractSection()
    {
        RunCli("map-plan", "--source", MinimalCellExample, "--output", OutputDir);

        var content = File.ReadAllText(Path.Combine(OutputDir, "map-export-plan.md"));

        Assert.Contains("Future text-only scaffold contract",    content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MAP-3A defines the future text-only scaffold only", content, StringComparison.Ordinal);
        Assert.Contains("future mod.info",                       content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("future media/maps",                     content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No local mod scaffold written",         content, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 12: Output directory contains only the 2 expected files
    // -----------------------------------------------------------------------

    [Fact]
    public void MapPlan_OutputDir_ContainsOnlyTwoFiles()
    {
        RunCli("map-plan", "--source", MinimalCellExample, "--output", OutputDir);

        var files = Directory.GetFiles(OutputDir, "*", SearchOption.TopDirectoryOnly)
                             .Select(Path.GetFileName)
                             .OrderBy(n => n)
                             .ToList();

        Assert.Equal(new[] { "map-export-plan.json", "map-export-plan.md" },
            files.OrderBy(f => f).ToArray());
    }

    // -----------------------------------------------------------------------
    // Test 13: No media/maps subdirectory exists under output
    // -----------------------------------------------------------------------

    [Fact]
    public void MapPlan_OutputDir_NoMediaMapsSubdirectory()
    {
        RunCli("map-plan", "--source", MinimalCellExample, "--output", OutputDir);

        var mediaMaps = Path.Combine(OutputDir, "media", "maps");
        Assert.False(Directory.Exists(mediaMaps));
    }
}
