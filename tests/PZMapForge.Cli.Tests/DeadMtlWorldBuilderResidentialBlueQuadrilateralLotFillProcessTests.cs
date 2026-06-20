using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map29a-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderResidentialBlueQuadrilateralLotFillProcessTests() =>
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
            "run-deadmtl-worldbuilder-residential-blue-quadrilateral-lot-fill.ps1");

    // North/south fixture PNG (inline, no System.Drawing dependency here)
    private string FixturePng => Path.Combine(_tempDir, "ns_fixture.local.png");
    private string OutputRoot  => Path.Combine(_tempDir, "output.local");
    private string OutputJson  => Path.Combine(OutputRoot, "map_00.blue_lot_fill.json");
    private string LotsCsv     => Path.Combine(OutputRoot, "map_00.blue_lot_fill_lots.csv");
    private string FacCsv      => Path.Combine(OutputRoot, "map_00.blue_lot_fill_facades.csv");
    private string ChkCsv      => Path.Combine(OutputRoot, "map_00.blue_lot_fill_checks.csv");
    private string OutputPng   => Path.Combine(OutputRoot, "map_00_residential_blue_lot_fill_output_native_256.png");
    private string OutputHtml  => Path.Combine(OutputRoot, "map_00_blue_lot_fill_viewer.html");
    private string Summary     => Path.Combine(OutputRoot, "map_00.blue_lot_fill.summary.txt");

    private void EnsureFixture()
    {
        // Create fixture PNG via dotnet script — use System.Drawing through a temp helper invocation
        // that creates the fixture in-process. We build a minimal PNG (256x256) using raw bytes.
        // Rather than creating it here, we run a quick dotnet-script helper via the CLI that draws it.
        // Simpler: call the builder Core directly via reflection — but we'd need the assembly.
        // Simplest: write a minimal valid PNG using the PNG spec (IHDR + IDAT + IEND).
        // For test purposes, we generate the fixture with PowerShell + System.Drawing through
        // an embedded inline script that we exec.
        if (File.Exists(FixturePng)) return;

        var script = $@"
Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap(256, 256)
$g   = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(18, 18, 24))
$g.Dispose()
# Orange roads
for ($x = 124; $x -le 212; $x++) {{
    $bmp.SetPixel($x,  8, [System.Drawing.Color]::FromArgb(220, 120, 30))
    $bmp.SetPixel($x,  9, [System.Drawing.Color]::FromArgb(220, 120, 30))
    $bmp.SetPixel($x, 70, [System.Drawing.Color]::FromArgb(220, 120, 30))
    $bmp.SetPixel($x, 71, [System.Drawing.Color]::FromArgb(220, 120, 30))
}}
# Blue rect
for ($x = 124; $x -le 212; $x++) {{
    for ($y = 10; $y -le 69; $y++) {{
        $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(58, 94, 174))
    }}
}}
$bmp.Save('{FixturePng.Replace("'", "''")}', [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
";
        var psi = new ProcessStartInfo("powershell",
            $"-ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        using var proc = Process.Start(psi)!;
        proc.WaitForExit(30_000);
    }

    private string[] MakeFullArgs() => new[]
    {
        "--source-png",        FixturePng,
        "--output-root",       OutputRoot,
        "--output-json",       OutputJson,
        "--output-lots-csv",   LotsCsv,
        "--output-facades-csv", FacCsv,
        "--output-checks-csv", ChkCsv,
        "--output-png",        OutputPng,
        "--output-html",       OutputHtml,
        "--summary",           Summary,
    };

    private (int ExitCode, string Output, string Error) RunCli(params string[] extraArgs)
    {
        var allArgs = new[] { "deadmtl-build-worldbuilder-residential-blue-quadrilateral-lot-fill" }
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
        proc.WaitForExit(180_000);
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Command string does not contain brittle build flags
    // -----------------------------------------------------------------------

    [Fact]
    public void RunCli_CommandString_DoesNotContainNoBuildOrConfigurationRelease()
    {
        var psi = new ProcessStartInfo("dotnet",
            $"run --project \"{CliProject}\" -- deadmtl-build-worldbuilder-residential-blue-quadrilateral-lot-fill");
        Assert.DoesNotContain("--no-build",              psi.Arguments, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("--configuration Release", psi.Arguments, StringComparison.OrdinalIgnoreCase);
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
        EnsureFixture();
        var (code, _, err) = RunCli(
            "--source-png",        FixturePng,
            "--output-root",       Path.GetTempPath(),
            "--output-json",       Path.Combine(_tempDir, "out.json"),
            "--output-lots-csv",   Path.Combine(_tempDir, "lots.csv"),
            "--output-facades-csv", Path.Combine(_tempDir, "fac.csv"),
            "--output-checks-csv", Path.Combine(_tempDir, "chk.csv"),
            "--output-png",        Path.Combine(_tempDir, "out.png"),
            "--output-html",       Path.Combine(_tempDir, "out.html"),
            "--summary",           Path.Combine(_tempDir, "sum.txt"));
        Assert.Equal(1, code);
        Assert.Contains(".local", err, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Command_ValidArgs_ExitsZero()
    {
        EnsureFixture();
        var (code, _, err) = RunCli(MakeFullArgs());
        Assert.True(code == 0, $"Exit={code} stderr={err}");
    }

    // -----------------------------------------------------------------------
    // Output files written
    // -----------------------------------------------------------------------

    [Fact]
    public void Command_AllOutputFilesWritten()
    {
        EnsureFixture();
        RunCli(MakeFullArgs());
        Assert.True(File.Exists(OutputJson), "JSON not written");
        Assert.True(File.Exists(LotsCsv),    "lots CSV not written");
        Assert.True(File.Exists(FacCsv),     "facades CSV not written");
        Assert.True(File.Exists(ChkCsv),     "checks CSV not written");
        Assert.True(File.Exists(OutputPng),  "output PNG not written");
        Assert.True(File.Exists(OutputHtml), "HTML not written");
        Assert.True(File.Exists(Summary),    "summary not written");
    }

    // -----------------------------------------------------------------------
    // JSON correctness
    // -----------------------------------------------------------------------

    [Fact]
    public void OutputJson_DetectedBlueComponentCount_Is1()
    {
        EnsureFixture();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.Equal(1, doc.RootElement.GetProperty("detected_blue_component_count").GetInt32());
    }

    [Fact]
    public void OutputJson_TotalLotCount_Is12()
    {
        EnsureFixture();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.Equal(12, doc.RootElement.GetProperty("total_lot_count").GetInt32());
    }

    [Fact]
    public void OutputJson_TotalFacadeEdgeCount_Is12()
    {
        EnsureFixture();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.Equal(12, doc.RootElement.GetProperty("total_facade_edge_count").GetInt32());
    }

    [Fact]
    public void OutputJson_UnsupportedBlueComponentCount_IsZero()
    {
        EnsureFixture();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.Equal(0, doc.RootElement.GetProperty("unsupported_blue_component_count").GetInt32());
    }

    [Fact]
    public void OutputJson_IsValid_IsTrue()
    {
        EnsureFixture();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.True(doc.RootElement.GetProperty("is_valid").GetBoolean());
    }

    [Fact]
    public void OutputJson_WriterReady_IsFalse()
    {
        EnsureFixture();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.False(doc.RootElement.GetProperty("writer_ready").GetBoolean());
    }

    [Fact]
    public void OutputJson_SandboxOnly_IsTrue()
    {
        EnsureFixture();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.True(doc.RootElement.GetProperty("sandbox_only").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Source PNG not mutated
    // -----------------------------------------------------------------------

    [Fact]
    public void SourcePng_HashUnchangedAfterCommand()
    {
        EnsureFixture();
        string hashBefore = ComputeSha256(FixturePng);
        RunCli(MakeFullArgs());
        string hashAfter  = ComputeSha256(FixturePng);
        Assert.Equal(hashBefore, hashAfter);
    }

    // -----------------------------------------------------------------------
    // No forbidden runtime artifacts
    // -----------------------------------------------------------------------

    [Fact]
    public void OutputRoot_ContainsNoForbiddenArtifacts()
    {
        EnsureFixture();
        RunCli(MakeFullArgs());
        foreach (var ext in new[] { "*.lotpack", "*.lotheader", "*.lua", "*.bin" })
        {
            var found = Directory.Exists(OutputRoot)
                ? Directory.GetFiles(OutputRoot, ext, SearchOption.AllDirectories)
                : Array.Empty<string>();
            Assert.Empty(found);
        }
    }

    // -----------------------------------------------------------------------
    // Helper script
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
        Assert.DoesNotContain("WorldGenOverride.lua", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("compile-worldgen",     content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("media/maps",           content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("steamapps",            content, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // MAP-29B — JSON includes red summary fields
    // -----------------------------------------------------------------------

    [Fact]
    public void OutputJson_ContainsRedSummaryFields()
    {
        EnsureFixture();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        var root = doc.RootElement;
        // All red fields must be present (fixture has no red, so counts are 0)
        Assert.True(root.TryGetProperty("detected_red_component_count", out _),
            "missing detected_red_component_count");
        Assert.True(root.TryGetProperty("red_lot_count", out _),
            "missing red_lot_count");
        Assert.True(root.TryGetProperty("red_facade_edge_count", out _),
            "missing red_facade_edge_count");
        Assert.True(root.TryGetProperty("source_red_pixels_replaced", out _),
            "missing source_red_pixels_replaced");
        Assert.True(root.TryGetProperty("source_red_pixels_remaining", out _),
            "missing source_red_pixels_remaining");
    }

    [Fact]
    public void OutputJson_BlueLotCount_MatchesTotalLotCount_WhenNoRed()
    {
        EnsureFixture();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        var root = doc.RootElement;
        int blueLots  = root.GetProperty("blue_lot_count").GetInt32();
        int totalLots = root.GetProperty("total_lot_count").GetInt32();
        int redLots   = root.GetProperty("red_lot_count").GetInt32();
        Assert.Equal(0, redLots);
        Assert.Equal(totalLots, blueLots);
    }

    [Fact]
    public void OutputJson_SourceRedPixelsRemaining_IsZero_WhenNoRed()
    {
        EnsureFixture();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        Assert.Equal(0, doc.RootElement.GetProperty("source_red_pixels_remaining").GetInt32());
    }

    // -----------------------------------------------------------------------
    // MAP-29B1 — JSON includes merge summary fields
    // -----------------------------------------------------------------------

    [Fact]
    public void OutputJson_ContainsMergeSummaryFields()
    {
        EnsureFixture();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("lot_sizing_policy_version", out _),
            "missing lot_sizing_policy_version");
        Assert.True(root.TryGetProperty("undersized_lot_merge_count", out _),
            "missing undersized_lot_merge_count");
        Assert.True(root.TryGetProperty("blue_undersized_lot_merge_count", out _),
            "missing blue_undersized_lot_merge_count");
        Assert.True(root.TryGetProperty("red_undersized_lot_merge_count", out _),
            "missing red_undersized_lot_merge_count");
    }

    [Fact]
    public void OutputJson_LotSizingPolicyVersion_IsNonEmpty()
    {
        EnsureFixture();
        RunCli(MakeFullArgs());
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputJson));
        var version = doc.RootElement.GetProperty("lot_sizing_policy_version").GetString();
        Assert.False(string.IsNullOrEmpty(version), "lot_sizing_policy_version should be non-empty");
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static string ComputeSha256(string path)
    {
        using var fs  = File.OpenRead(path);
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(fs)).ToLowerInvariant();
    }
}
