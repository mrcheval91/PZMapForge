using System.Diagnostics;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderRawTileZoneMetadataProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-zone-meta-cli", Path.GetRandomFileName());

    public DeadMtlWorldBuilderRawTileZoneMetadataProcessTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProject =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj");

    private static string MetadataPath =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "worldbuilder", "tiles",
            "map_00.zone_metadata.json");

    private static string ProfilePath =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "worldbuilder", "neighborhoods",
            "deadmtl_baseline_neighborhood_profile.json");

    private static string InspectionPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "raw-map-tile-inspection",
            "map_00", "map_00.raw_tile_inspection.json");

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-zone-metadata-validation.ps1");

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
        psi.ArgumentList.Add(CliProject);
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

    private (string Json, string Md, string Csv, string Summary) MakeOutputPaths(string subdir = "default")
    {
        var dir = Path.Combine(_tempDir, ".local", subdir);
        Directory.CreateDirectory(dir);
        return (
            Path.Combine(dir, "zone_meta.validation.json"),
            Path.Combine(dir, "zone_meta.validation.md"),
            Path.Combine(dir, "zone_meta.validation.csv"),
            Path.Combine(dir, "zone_meta.validation.summary.txt")
        );
    }

    private (int ExitCode, string Stdout, string Stderr) RunValidate(string? inspectionOverride = null)
    {
        var (j, m, c, s) = MakeOutputPaths();
        return RunCli(
            "deadmtl-validate-worldbuilder-zone-metadata",
            "--metadata",    MetadataPath,
            "--inspection",  inspectionOverride ?? InspectionPath,
            "--profile",     ProfilePath,
            "--output-json", j,
            "--output-md",   m,
            "--output-csv",  c,
            "--summary",     s);
    }

    // -----------------------------------------------------------------------
    // Exit codes
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_WithValidMetadata()
    {
        var (code, stdout, stderr) = RunValidate();
        Assert.True(code == 0, $"Exit {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void Cli_ExitsOne_WhenMissingArgs()
    {
        var (code, _, _) = RunCli("deadmtl-validate-worldbuilder-zone-metadata");
        Assert.Equal(1, code);
    }

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var outsideDir = Path.Combine(_tempDir, "not-local");
        Directory.CreateDirectory(outsideDir);
        var (code, _, _) = RunCli(
            "deadmtl-validate-worldbuilder-zone-metadata",
            "--metadata",    MetadataPath,
            "--inspection",  InspectionPath,
            "--profile",     ProfilePath,
            "--output-json", Path.Combine(outsideDir, "out.json"),
            "--output-md",   Path.Combine(outsideDir, "out.md"),
            "--output-csv",  Path.Combine(outsideDir, "out.csv"),
            "--summary",     Path.Combine(outsideDir, "out.txt"));
        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // Output files created
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesOutputJson()
    {
        var (j, m, c, s) = MakeOutputPaths("json");
        RunCli("deadmtl-validate-worldbuilder-zone-metadata",
            "--metadata", MetadataPath, "--inspection", InspectionPath, "--profile", ProfilePath,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(j), "output JSON not created");
    }

    [Fact]
    public void Cli_CreatesOutputMd()
    {
        var (j, m, c, s) = MakeOutputPaths("md");
        RunCli("deadmtl-validate-worldbuilder-zone-metadata",
            "--metadata", MetadataPath, "--inspection", InspectionPath, "--profile", ProfilePath,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(m), "output MD not created");
    }

    [Fact]
    public void Cli_CreatesOutputCsv()
    {
        var (j, m, c, s) = MakeOutputPaths("csv");
        RunCli("deadmtl-validate-worldbuilder-zone-metadata",
            "--metadata", MetadataPath, "--inspection", InspectionPath, "--profile", ProfilePath,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(c), "output CSV not created");
    }

    [Fact]
    public void Cli_CreatesOutputSummary()
    {
        var (j, m, c, s) = MakeOutputPaths("summary");
        RunCli("deadmtl-validate-worldbuilder-zone-metadata",
            "--metadata", MetadataPath, "--inspection", InspectionPath, "--profile", ProfilePath,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(s), "output summary not created");
    }

    // -----------------------------------------------------------------------
    // Stdout verdict
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_Stdout_ContainsVerdict()
    {
        var (_, stdout, _) = RunValidate();
        Assert.Contains("MAP25B_WORLDBUILDER_RAW_TILE_ZONE_METADATA_CONTRACT_COMPLETE",
            stdout, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Unknown command
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommand_MentionsZoneMetadataCommand()
    {
        var (_, stdout, stderr) = RunCli("unknown-xyz-command");
        var combined = stdout + stderr;
        Assert.Contains("deadmtl-validate-worldbuilder-zone-metadata",
            combined, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Helper script — existence and forbidden strings
    // -----------------------------------------------------------------------

    [Fact]
    public void HelperScript_Exists()
    {
        Assert.True(File.Exists(HelperScript), $"Helper script not found: {HelperScript}");
    }

    [Fact]
    public void HelperScript_DoesNotContain_CompileWorldgen()
    {
        var text = File.ReadAllText(HelperScript);
        Assert.DoesNotContain("compile-worldgen", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContain_WorldGenOverrideLua()
    {
        var text = File.ReadAllText(HelperScript);
        Assert.DoesNotContain("WorldGenOverride.lua", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContain_DotLotpack()
    {
        var text = File.ReadAllText(HelperScript);
        Assert.DoesNotContain(".lotpack", text, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Doc checks
    // -----------------------------------------------------------------------

    [Fact]
    public void Doc_MentionsNoGeneration()
    {
        var docPath = Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_WORLDBUILDER_RAW_TILE_ZONE_METADATA_CONTRACT.md");
        Assert.True(File.Exists(docPath), $"Doc not found: {docPath}");
        var text = File.ReadAllText(docPath);
        Assert.True(
            text.Contains("No terrain generation", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Metadata only", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("metadata only", StringComparison.OrdinalIgnoreCase),
            "doc should explicitly state no generation / metadata only");
    }

    [Fact]
    public void README_MentionsHelperScript()
    {
        var readme = Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "README.md");
        Assert.True(File.Exists(readme), $"README not found: {readme}");
        var text = File.ReadAllText(readme);
        Assert.Contains("run-deadmtl-worldbuilder-zone-metadata-validation",
            text, StringComparison.Ordinal);
    }
}
