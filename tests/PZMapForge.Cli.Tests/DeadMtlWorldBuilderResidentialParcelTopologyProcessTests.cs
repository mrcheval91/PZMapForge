using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderResidentialParcelTopologyProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map28a-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderResidentialParcelTopologyProcessTests() =>
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
            "run-deadmtl-worldbuilder-residential-parcel-topology.ps1");

    private string OutputRoot  => Path.Combine(_tempDir, "output.local");
    private string OutputJson  => Path.Combine(OutputRoot, "map_00.residential_parcel_topology.json");
    private string ParcelsCsv  => Path.Combine(OutputRoot, "map_00.residential_parcel_topology_parcels.csv");
    private string EdgesCsv    => Path.Combine(OutputRoot, "map_00.residential_parcel_topology_frontage_edges.csv");
    private string StripsCsv   => Path.Combine(OutputRoot, "map_00.residential_parcel_topology_sidewalk_strips.csv");
    private string ChecksCsv   => Path.Combine(OutputRoot, "map_00.residential_parcel_topology_checks.csv");
    private string Summary     => Path.Combine(OutputRoot, "map_00.residential_parcel_topology.summary.txt");
    private string CleanPng    => Path.Combine(OutputRoot, "map_00_residential_parcels_topology_clean_native_256.png");
    private string DebugPng    => Path.Combine(OutputRoot, "map_00_residential_parcels_topology_debug_native_256.png");
    private string OverlayPng  => Path.Combine(OutputRoot, "map_00_residential_parcels_topology_overlay_native_256.png");
    private string OutputHtml  => Path.Combine(OutputRoot, "map_00_residential_parcels_topology_viewer.html");

    private string[] MakeFullArgs() => new[]
    {
        "--output-root",              OutputRoot,
        "--output-json",              OutputJson,
        "--output-parcels-csv",       ParcelsCsv,
        "--output-frontage-edges-csv", EdgesCsv,
        "--output-sidewalk-strips-csv", StripsCsv,
        "--output-checks-csv",        ChecksCsv,
        "--summary",                  Summary,
        "--output-clean-png",         CleanPng,
        "--output-debug-png",         DebugPng,
        "--output-overlay-png",       OverlayPng,
        "--output-html",              OutputHtml,
    };

    private (int ExitCode, string Output, string Error) RunCli(params string[] extraArgs)
    {
        var allArgs = new[] { "deadmtl-build-worldbuilder-residential-parcel-topology" }
            .Concat(extraArgs)
            .ToArray();

        var psi = new ProcessStartInfo("dotnet",
            $"run --project \"{CliProject}\" --configuration Release --no-build -- " +
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
        proc.WaitForExit(60_000);
        return (proc.ExitCode, stdout, stderr);
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
            "--output-root",    Path.GetTempPath(),
            "--output-json",    Path.Combine(_tempDir, "out.json"),
            "--output-parcels-csv",       Path.Combine(_tempDir, "p.csv"),
            "--output-frontage-edges-csv", Path.Combine(_tempDir, "e.csv"),
            "--output-sidewalk-strips-csv", Path.Combine(_tempDir, "s.csv"),
            "--output-checks-csv",        Path.Combine(_tempDir, "c.csv"),
            "--summary",                  Path.Combine(_tempDir, "s.txt"),
            "--output-clean-png",         Path.Combine(_tempDir, "clean.png"),
            "--output-debug-png",         Path.Combine(_tempDir, "debug.png"),
            "--output-overlay-png",       Path.Combine(_tempDir, "overlay.png"),
            "--output-html",              Path.Combine(_tempDir, "viewer.html"));
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
    // Output files written
    // -----------------------------------------------------------------------

    [Fact]
    public void Command_AllOutputFilesWritten()
    {
        RunCli(MakeFullArgs());
        Assert.True(File.Exists(OutputJson),  "JSON not written");
        Assert.True(File.Exists(ParcelsCsv),  "parcels CSV not written");
        Assert.True(File.Exists(EdgesCsv),    "edges CSV not written");
        Assert.True(File.Exists(StripsCsv),   "strips CSV not written");
        Assert.True(File.Exists(ChecksCsv),   "checks CSV not written");
        Assert.True(File.Exists(Summary),     "summary not written");
        Assert.True(File.Exists(CleanPng),    "clean PNG not written");
        Assert.True(File.Exists(DebugPng),    "debug PNG not written");
        Assert.True(File.Exists(OverlayPng),  "overlay PNG not written");
        Assert.True(File.Exists(OutputHtml),  "HTML not written");
    }

    // -----------------------------------------------------------------------
    // JSON correctness
    // -----------------------------------------------------------------------

    [Fact]
    public void OutputJson_ComponentId_IsCorrect()
    {
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.Equal("map_00_component_0001",
            doc.RootElement.GetProperty("component_id").GetString());
    }

    [Fact]
    public void OutputJson_TotalLotCount_Is16()
    {
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.Equal(16,
            doc.RootElement.GetProperty("total_residential_lot_count").GetInt32());
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
    public void OutputJson_CheckCount_Is25()
    {
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.Equal(25, doc.RootElement.GetProperty("check_count").GetInt32());
    }

    [Fact]
    public void OutputJson_FailedCheckCount_IsZero()
    {
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.Equal(0, doc.RootElement.GetProperty("failed_check_count").GetInt32());
    }

    // -----------------------------------------------------------------------
    // CSV row counts
    // -----------------------------------------------------------------------

    [Fact]
    public void ParcelsCsv_Has16DataRows()
    {
        RunCli(MakeFullArgs());
        var rows = File.ReadAllLines(ParcelsCsv)
            .Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        Assert.Equal(17, rows.Length); // 1 header + 16 parcels
    }

    [Fact]
    public void ChecksCsv_Has25DataRows()
    {
        RunCli(MakeFullArgs());
        var rows = File.ReadAllLines(ChecksCsv)
            .Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        Assert.Equal(26, rows.Length); // 1 header + 25 checks
    }

    // -----------------------------------------------------------------------
    // PNG dimensions
    // -----------------------------------------------------------------------

    [Fact]
    public void CleanPng_Is256x256()
    {
        RunCli(MakeFullArgs());
        var (w, h) = ReadPngDimensions(CleanPng);
        Assert.Equal(256, w);
        Assert.Equal(256, h);
    }

    [Fact]
    public void DebugPng_Is256x256()
    {
        RunCli(MakeFullArgs());
        var (w, h) = ReadPngDimensions(DebugPng);
        Assert.Equal(256, w);
        Assert.Equal(256, h);
    }

    [Fact]
    public void OverlayPng_Is256x256()
    {
        RunCli(MakeFullArgs());
        var (w, h) = ReadPngDimensions(OverlayPng);
        Assert.Equal(256, w);
        Assert.Equal(256, h);
    }

    // -----------------------------------------------------------------------
    // ASCII-only outputs
    // -----------------------------------------------------------------------

    [Fact]
    public void Html_IsAsciiOnly()
    {
        RunCli(MakeFullArgs());
        var bytes = File.ReadAllBytes(OutputHtml);
        Assert.All(bytes, b => Assert.True(b < 128, $"Non-ASCII byte 0x{b:X2}"));
    }

    // -----------------------------------------------------------------------
    // No runtime artifacts
    // -----------------------------------------------------------------------

    [Fact]
    public void OutputRoot_ContainsNoForbiddenArtifacts()
    {
        RunCli(MakeFullArgs());
        foreach (var ext in new[] { "*.lotpack", "*.lotheader", "*.lua", "*.bin" })
        {
            var found = Directory.GetFiles(OutputRoot, ext, SearchOption.AllDirectories);
            Assert.Empty(found);
        }
    }

    // -----------------------------------------------------------------------
    // Helper script exists
    // -----------------------------------------------------------------------

    [Fact]
    public void HelperScript_Exists()
    {
        Assert.True(File.Exists(HelperScript),
            $"Helper script not found: {HelperScript}");
    }

    [Fact]
    public void HelperScript_DoesNotContainForbiddenRuntimeWriterCalls()
    {
        var content = File.ReadAllText(HelperScript);
        // Check for actual runtime writer invocations, not disclaimer mentions
        Assert.DoesNotContain("WorldGenOverride.lua",  content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("compile-worldgen",      content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("media/maps",            content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("steamapps",             content, StringComparison.OrdinalIgnoreCase);
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
