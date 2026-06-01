using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using Xunit;

namespace PZMapForge.Cli.Tests;

// ---------------------------------------------------------------------------
// Shared fixture — runs full-pipeline once; all contract tests share the output
// ---------------------------------------------------------------------------

[SupportedOSPlatform("windows")]
public sealed class FullPipelineContractFixture : IDisposable
{
    private readonly string _root;

    public string OutputDir { get; }
    public int    ExitCode  { get; }

    public FullPipelineContractFixture()
    {
        _root     = Path.Combine(Path.GetTempPath(), "pzmapforge-contract", Path.GetRandomFileName());
        OutputDir = Path.Combine(_root, ".local", "mapforge");
        Directory.CreateDirectory(OutputDir);

        var imgPath = Path.Combine(_root, "grass.png");
        using (var bmp = new Bitmap(300, 300))
        using (var g   = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(255, 100, 140, 70));
            bmp.Save(imgPath, ImageFormat.Png);
        }

        var repoRoot   = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var cliProject = Path.Combine(repoRoot, "src", "PZMapForge.Cli");
        var palette    = Path.Combine(repoRoot, "source", "image-palette.json");

        var psi = new ProcessStartInfo
        {
            FileName               = "dotnet",
            WorkingDirectory       = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        foreach (var a in new[]
            { "run", "--project", cliProject, "--",
              "full-pipeline",
              "--path",    imgPath,
              "--palette", palette,
              "--output",  OutputDir })
            psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi)!;
        proc.StandardOutput.ReadToEnd();
        proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        ExitCode = proc.ExitCode;
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); }
        catch { /* best effort */ }
    }
}

// ---------------------------------------------------------------------------
// Contract tests — one concern per [Fact], one pipeline run shared via fixture
// ---------------------------------------------------------------------------

[SupportedOSPlatform("windows")]
public sealed class FullPipelineContractTests : IClassFixture<FullPipelineContractFixture>
{
    private readonly FullPipelineContractFixture _fix;
    public FullPipelineContractTests(FullPipelineContractFixture fix) => _fix = fix;

    private string Art(string name) => Path.Combine(_fix.OutputDir, name);

    // -----------------------------------------------------------------------
    // Test 11: exit code
    // -----------------------------------------------------------------------

    [Fact]
    public void Pipeline_ExitsZero() =>
        Assert.Equal(0, _fix.ExitCode);

    // -----------------------------------------------------------------------
    // Tests 12-13: regions-report.md content
    // -----------------------------------------------------------------------

    [Fact]
    public void RegionsReport_ContainsClaimBoundary() =>
        Assert.Contains(
            "planning_artifact_only_not_pz_load_tested",
            File.ReadAllText(Art("regions-report.md")),
            StringComparison.Ordinal);

    [Fact]
    public void RegionsReport_ContainsSummaryByKind() =>
        Assert.Contains(
            "Summary by kind",
            File.ReadAllText(Art("regions-report.md")),
            StringComparison.OrdinalIgnoreCase);

    // -----------------------------------------------------------------------
    // Tests 14-15: primitives-report.md content
    // -----------------------------------------------------------------------

    [Fact]
    public void PrimitivesReport_ContainsClaimBoundary() =>
        Assert.Contains(
            "planning_artifact_only_not_pz_load_tested",
            File.ReadAllText(Art("primitives-report.md")),
            StringComparison.Ordinal);

    [Fact]
    public void PrimitivesReport_ContainsSummaryByPrimitiveType() =>
        Assert.Contains(
            "Summary by primitive type",
            File.ReadAllText(Art("primitives-report.md")),
            StringComparison.OrdinalIgnoreCase);

    // -----------------------------------------------------------------------
    // Test 16: plan-report.md content
    // -----------------------------------------------------------------------

    [Fact]
    public void PlanReport_ContainsClaimBoundary() =>
        Assert.Contains(
            "planning_artifact_only_not_pz_load_tested",
            File.ReadAllText(Art("plan-report.md")),
            StringComparison.Ordinal);
}
