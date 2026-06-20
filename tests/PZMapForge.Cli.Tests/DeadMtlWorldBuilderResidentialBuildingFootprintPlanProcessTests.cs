using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderResidentialBuildingFootprintPlanProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map28b-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderResidentialBuildingFootprintPlanProcessTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProject =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj");

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-residential-building-footprint-plan.ps1");

    private string OutputRoot       => Path.Combine(_tempDir, "output.local");
    private string OutputJson       => Path.Combine(OutputRoot, "map_00.residential_building_footprint_plan.json");
    private string FootprintsCsv    => Path.Combine(OutputRoot, "map_00.residential_building_footprint_plan_footprints.csv");
    private string ChecksCsv        => Path.Combine(OutputRoot, "map_00.residential_building_footprint_plan_checks.csv");
    private string Summary          => Path.Combine(OutputRoot, "map_00.residential_building_footprint_plan.summary.txt");
    private string Readme           => Path.Combine(OutputRoot, "README_MAP28B_RESIDENTIAL_BUILDING_FOOTPRINT_PLAN.md");
    private string CleanPng         => Path.Combine(OutputRoot, "map_00_residential_building_footprints_clean_native_256.png");
    private string DebugPng         => Path.Combine(OutputRoot, "map_00_residential_building_footprints_debug_native_256.png");
    private string OverlayPng       => Path.Combine(OutputRoot, "map_00_residential_building_footprints_overlay_native_256.png");
    private string OutputHtml       => Path.Combine(OutputRoot, "map_00_residential_building_footprints_viewer.html");
    private string ParentManifest   => Path.Combine(OutputRoot, "map_00.residential_building_footprint_plan_parent_manifest.json");

    private string[] MakeFullArgs() => new[]
    {
        "--output-root",            OutputRoot,
        "--output-json",            OutputJson,
        "--output-footprints-csv",  FootprintsCsv,
        "--output-checks-csv",      ChecksCsv,
        "--summary",                Summary,
        "--output-readme",          Readme,
        "--output-clean-png",       CleanPng,
        "--output-debug-png",       DebugPng,
        "--output-overlay-png",     OverlayPng,
        "--output-html",            OutputHtml,
        "--output-parent-manifest", ParentManifest,
    };

    private (int ExitCode, string Output, string Error) RunCli(params string[] extraArgs)
    {
        var allArgs = new[] { "deadmtl-build-worldbuilder-residential-building-footprint-plan" }
            .Concat(extraArgs)
            .ToArray();

        var psi = new ProcessStartInfo("dotnet",
            $"run --project \"{CliProject}\" -- " +
            string.Join(" ", allArgs.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)))
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            WorkingDirectory       = RepoRoot,
        };

        using var proc = Process.Start(psi)!;
        string stdout = proc.StandardOutput.ReadToEnd();
        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit(120_000);
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Command string guard
    // -----------------------------------------------------------------------

    [Fact]
    public void RunCli_CommandString_DoesNotContainNoBuildOrConfigurationRelease()
    {
        var psi = new ProcessStartInfo("dotnet",
            $"run --project \"{CliProject}\" -- deadmtl-build-worldbuilder-residential-building-footprint-plan");
        var cmdLine = psi.Arguments;
        Assert.DoesNotContain("--no-build",             cmdLine, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("--configuration Release", cmdLine, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Exit codes
    // -----------------------------------------------------------------------

    [Fact]
    public void Command_MissingArgs_ExitsOne()
    {
        var (code, _, _) = RunCli();
        Assert.Equal(1, code);
    }

    [Fact]
    public void Command_NonLocalOutputRoot_ExitsOne()
    {
        var (code, _, err) = RunCli(
            "--output-root",            Path.GetTempPath(),
            "--output-json",            Path.Combine(_tempDir, "out.json"),
            "--output-footprints-csv",  Path.Combine(_tempDir, "f.csv"),
            "--output-checks-csv",      Path.Combine(_tempDir, "c.csv"),
            "--summary",                Path.Combine(_tempDir, "s.txt"),
            "--output-readme",          Path.Combine(_tempDir, "readme.md"),
            "--output-clean-png",       Path.Combine(_tempDir, "clean.png"),
            "--output-debug-png",       Path.Combine(_tempDir, "debug.png"),
            "--output-overlay-png",     Path.Combine(_tempDir, "overlay.png"),
            "--output-html",            Path.Combine(_tempDir, "viewer.html"),
            "--output-parent-manifest", Path.Combine(_tempDir, "manifest.json"));
        Assert.Equal(1, code);
        Assert.Contains(".local", err, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Command_ValidArgs_ExitsZero()
    {
        var (code, _, _) = RunCli(MakeFullArgs());
        Assert.Equal(0, code);
    }

    // -----------------------------------------------------------------------
    // Output files written (10)
    // -----------------------------------------------------------------------

    [Fact]
    public void Command_AllOutputFilesWritten()
    {
        RunCli(MakeFullArgs());
        Assert.True(File.Exists(OutputJson),     "JSON not written");
        Assert.True(File.Exists(FootprintsCsv),  "footprints CSV not written");
        Assert.True(File.Exists(ChecksCsv),      "checks CSV not written");
        Assert.True(File.Exists(Summary),        "summary not written");
        Assert.True(File.Exists(Readme),         "README not written");
        Assert.True(File.Exists(CleanPng),       "clean PNG not written");
        Assert.True(File.Exists(DebugPng),       "debug PNG not written");
        Assert.True(File.Exists(OverlayPng),     "overlay PNG not written");
        Assert.True(File.Exists(OutputHtml),     "HTML not written");
        Assert.True(File.Exists(ParentManifest), "parent manifest not written");
    }

    // -----------------------------------------------------------------------
    // JSON correctness
    // -----------------------------------------------------------------------

    [Fact]
    public void OutputJson_TotalFootprintCount_Is12()
    {
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.Equal(12, doc.RootElement.GetProperty("total_footprint_count").GetInt32());
    }

    [Fact]
    public void OutputJson_NorthFootprintCount_Is6()
    {
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.Equal(6, doc.RootElement.GetProperty("north_footprint_count").GetInt32());
    }

    [Fact]
    public void OutputJson_SouthFootprintCount_Is6()
    {
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.Equal(6, doc.RootElement.GetProperty("south_footprint_count").GetInt32());
    }

    [Fact]
    public void OutputJson_EastFootprintCount_IsZero()
    {
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.Equal(0, doc.RootElement.GetProperty("east_footprint_count").GetInt32());
    }

    [Fact]
    public void OutputJson_CheckCount_AtLeast30()
    {
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.True(doc.RootElement.GetProperty("check_count").GetInt32() >= 30);
    }

    [Fact]
    public void OutputJson_FailedCheckCount_IsZero()
    {
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.Equal(0, doc.RootElement.GetProperty("failed_check_count").GetInt32());
    }

    [Fact]
    public void OutputJson_IsValid_IsTrue()
    {
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.True(doc.RootElement.GetProperty("is_valid").GetBoolean());
    }

    [Fact]
    public void OutputJson_WriterReady_IsFalse()
    {
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.False(doc.RootElement.GetProperty("writer_ready").GetBoolean());
    }

    [Fact]
    public void OutputJson_RuntimeValid_IsFalse()
    {
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.False(doc.RootElement.GetProperty("runtime_valid").GetBoolean());
    }

    [Fact]
    public void OutputJson_Materialized_IsFalse()
    {
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.False(doc.RootElement.GetProperty("materialized").GetBoolean());
    }

    [Fact]
    public void OutputJson_RuntimeProofClaimed_IsFalse()
    {
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.False(doc.RootElement.GetProperty("runtime_proof_claimed").GetBoolean());
    }

    [Fact]
    public void OutputJson_PublicPlayablePackagingClaimed_IsFalse()
    {
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.False(doc.RootElement.GetProperty("public_playable_packaging_claimed").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // PNG dimensions
    // -----------------------------------------------------------------------

    [Fact]
    public void CleanPng_Is256x256()
    {
        RunCli(MakeFullArgs());
        var (w, h) = ReadPngDimensions(CleanPng);
        Assert.Equal(256, w); Assert.Equal(256, h);
    }

    [Fact]
    public void DebugPng_Is256x256()
    {
        RunCli(MakeFullArgs());
        var (w, h) = ReadPngDimensions(DebugPng);
        Assert.Equal(256, w); Assert.Equal(256, h);
    }

    [Fact]
    public void OverlayPng_Is256x256()
    {
        RunCli(MakeFullArgs());
        var (w, h) = ReadPngDimensions(OverlayPng);
        Assert.Equal(256, w); Assert.Equal(256, h);
    }

    // -----------------------------------------------------------------------
    // ASCII-only outputs
    // -----------------------------------------------------------------------

    [Fact]
    public void Html_IsAsciiOnly()
    {
        RunCli(MakeFullArgs());
        Assert.All(File.ReadAllBytes(OutputHtml), b => Assert.True(b < 128, $"Non-ASCII 0x{b:X2}"));
    }

    [Fact]
    public void Readme_IsAsciiOnly()
    {
        RunCli(MakeFullArgs());
        Assert.All(File.ReadAllBytes(Readme), b => Assert.True(b < 128, $"Non-ASCII 0x{b:X2}"));
    }

    // -----------------------------------------------------------------------
    // No runtime artifacts
    // -----------------------------------------------------------------------

    [Fact]
    public void OutputRoot_ContainsNoForbiddenArtifacts()
    {
        RunCli(MakeFullArgs());
        foreach (var ext in new[] { "*.lotpack", "*.lotheader", "*.lua", "*.bin" })
            Assert.Empty(Directory.GetFiles(OutputRoot, ext, SearchOption.AllDirectories));
    }

    // -----------------------------------------------------------------------
    // Helper script
    // -----------------------------------------------------------------------

    [Fact]
    public void HelperScript_Exists() =>
        Assert.True(File.Exists(HelperScript), $"Helper script not found: {HelperScript}");

    [Fact]
    public void HelperScript_DoesNotContainForbiddenRuntimeWriterCalls()
    {
        var content = File.ReadAllText(HelperScript);
        Assert.DoesNotContain("WorldGenOverride.lua",     content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("compile-worldgen",         content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("media/maps",               content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("steamapps",                content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("--configuration Release",  content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("--no-build",               content, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static (int w, int h) ReadPngDimensions(string path)
    {
        var bytes = File.ReadAllBytes(path);
        int w = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        int h = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
        return (w, h);
    }
}
