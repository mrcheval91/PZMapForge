using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

/// <summary>
/// Process-level tests for inspect-build42-experimental-package (MAP-5E).
/// Verifies a MAP-5D generated Build 42 Workshop package is internally complete.
/// Claim boundary: packaging_inspection_only_not_load_tested
/// Not a load test. No PZ assets. No files copied. No playable export claim.
/// MAP-5B remains LOAD_TEST_INCONCLUSIVE. Binary hypotheses remain UNTESTED.
/// </summary>
public sealed class InspectBuild42ExperimentalPackageProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-inspect-b42-tests", Path.GetRandomFileName());

    public InspectBuild42ExperimentalPackageProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private string OutputBase   => Path.Combine(_tempDir, ".local", "map-export-experimental");
    private string InspectBase  => Path.Combine(_tempDir, ".local", "inspections");
    private const string TestMapId = "pzmapforge_inspect_cli_test";
    private string PkgRoot      => Path.Combine(OutputBase, TestMapId + "_build42_workshop");
    private string InspectOut   => Path.Combine(InspectBase, "test-01");
    private string InspectionJson => Path.Combine(InspectOut, "build42-experimental-package-inspection.json");

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

    private void GeneratePackage()
    {
        RunCli("map-export-experimental",
               "--map-id", TestMapId,
               "--output", OutputBase,
               "--build42-package");
    }

    // -----------------------------------------------------------------------
    // Test 1: valid package exits 0 and writes report
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_ValidPackage_ExitsZeroAndWritesReport()
    {
        GeneratePackage();
        var (code, stdout, stderr) = RunCli(
            "inspect-build42-experimental-package",
            "--package", PkgRoot,
            "--output", InspectOut);

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
        Assert.True(File.Exists(InspectionJson));
        Assert.True(File.Exists(Path.Combine(InspectOut, "build42-experimental-package-inspection.md")));
    }

    // -----------------------------------------------------------------------
    // Test 2: overall_result is PASS for a fresh valid package
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_ValidPackage_OverallResultIsPass()
    {
        GeneratePackage();
        RunCli("inspect-build42-experimental-package",
               "--package", PkgRoot,
               "--output", InspectOut);

        var doc  = JsonDocument.Parse(File.ReadAllText(InspectionJson));
        Assert.Equal("PASS", doc.RootElement.GetProperty("overall_result").GetString());
    }

    // -----------------------------------------------------------------------
    // Test 3: passed_count equals check_count for valid package
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_ValidPackage_AllChecksPassed()
    {
        GeneratePackage();
        RunCli("inspect-build42-experimental-package",
               "--package", PkgRoot,
               "--output", InspectOut);

        var doc  = JsonDocument.Parse(File.ReadAllText(InspectionJson));
        var root = doc.RootElement;
        Assert.Equal(0, root.GetProperty("failed_count").GetInt32());
        Assert.Equal(root.GetProperty("check_count").GetInt32(),
                     root.GetProperty("passed_count").GetInt32());
    }

    // -----------------------------------------------------------------------
    // Test 4: inspection JSON has safety flags
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_ValidPackage_ReportJson_HasSafetyFlags()
    {
        GeneratePackage();
        RunCli("inspect-build42-experimental-package",
               "--package", PkgRoot,
               "--output", InspectOut);

        var doc  = JsonDocument.Parse(File.ReadAllText(InspectionJson));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("playable_export_claimed").GetBoolean());
        Assert.False(root.GetProperty("load_tested").GetBoolean());
        Assert.True(root.GetProperty("no_files_copied").GetBoolean());
        Assert.True(root.GetProperty("no_pz_assets_read").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // Test 5: stdout contains PASS and check count
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_ValidPackage_StdoutContainsPassAndCounts()
    {
        GeneratePackage();
        var (_, stdout, _) = RunCli(
            "inspect-build42-experimental-package",
            "--package", PkgRoot,
            "--output", InspectOut);

        Assert.Contains("Overall result: PASS", stdout, StringComparison.Ordinal);
        Assert.Contains("failed=0", stdout, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 6: missing --package exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_MissingPackage_ExitsNonZero()
    {
        var (code, _, stderr) = RunCli(
            "inspect-build42-experimental-package",
            "--output", InspectOut);

        Assert.NotEqual(0, code);
        Assert.Contains("--package", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 7: missing --output exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_MissingOutput_ExitsNonZero()
    {
        GeneratePackage();
        var (code, _, stderr) = RunCli(
            "inspect-build42-experimental-package",
            "--package", PkgRoot);

        Assert.NotEqual(0, code);
        Assert.Contains("--output", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 8: output outside .local exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_OutputOutsideLocal_ExitsNonZero()
    {
        GeneratePackage();
        var badOutput = Path.Combine(_tempDir, "not-local", "output");
        var (code, _, stderr) = RunCli(
            "inspect-build42-experimental-package",
            "--package", PkgRoot,
            "--output", badOutput);

        Assert.NotEqual(0, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 9: non-existent package exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_NonExistentPackage_ExitsNonZero()
    {
        var missing = Path.Combine(_tempDir, "does_not_exist");
        var (code, _, stderr) = RunCli(
            "inspect-build42-experimental-package",
            "--package", missing,
            "--output", InspectOut);

        Assert.NotEqual(0, code);
        Assert.Contains("not found", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 10: incomplete package produces FAIL result and exits 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Inspect_IncompletePackage_ProducesFailAndExitsOne()
    {
        // Create a package directory with no files (missing everything)
        var emptyPkg = Path.Combine(_tempDir, ".local", "empty-package");
        Directory.CreateDirectory(emptyPkg);

        var (code, stdout, _) = RunCli(
            "inspect-build42-experimental-package",
            "--package", emptyPkg,
            "--output", InspectOut);

        Assert.NotEqual(0, code);
        Assert.Contains("FAIL", stdout, StringComparison.Ordinal);
    }
}
