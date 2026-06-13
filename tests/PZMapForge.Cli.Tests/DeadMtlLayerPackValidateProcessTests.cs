using System.Diagnostics;
using System.Runtime.Versioning;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlLayerPackValidateProcessTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private static string RealPackRoot =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack");

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
    // Test 1: valid real pack exits 0
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidateDeadMtl_ValidPack_ExitsZero()
    {
        var (code, stdout, stderr) = RunCli(
            "validate-deadmtl-layer-pack", "--input", RealPackRoot);

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // Test 2: stdout contains Status OK
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidateDeadMtl_ValidPack_StdoutContainsStatusOk()
    {
        var (_, stdout, _) = RunCli(
            "validate-deadmtl-layer-pack", "--input", RealPackRoot);

        Assert.Contains("Status:        OK", stdout, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 3: stdout contains Checks line
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidateDeadMtl_ValidPack_StdoutContainsChecksLine()
    {
        var (_, stdout, _) = RunCli(
            "validate-deadmtl-layer-pack", "--input", RealPackRoot);

        Assert.Contains("Checks:", stdout, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 4: stdout contains zero errors
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidateDeadMtl_ValidPack_StdoutReportsZeroErrors()
    {
        var (_, stdout, _) = RunCli(
            "validate-deadmtl-layer-pack", "--input", RealPackRoot);

        Assert.Contains("Errors:        0", stdout, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 5: stdout contains zero warnings
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidateDeadMtl_ValidPack_StdoutReportsZeroWarnings()
    {
        var (_, stdout, _) = RunCli(
            "validate-deadmtl-layer-pack", "--input", RealPackRoot);

        Assert.Contains("Warnings:      0", stdout, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 6: missing --input exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidateDeadMtl_MissingInput_ExitsNonZero()
    {
        var (code, _, stderr) = RunCli("validate-deadmtl-layer-pack");

        Assert.NotEqual(0, code);
        Assert.Contains("--input", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 7: nonexistent directory exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidateDeadMtl_DirectoryNotFound_ExitsNonZero()
    {
        var (code, _, stderr) = RunCli(
            "validate-deadmtl-layer-pack", "--input",
            Path.Combine(Path.GetTempPath(), "pzmapforge-does-not-exist-" + Guid.NewGuid()));

        Assert.NotEqual(0, code);
        Assert.Contains("INVALID", stderr, StringComparison.OrdinalIgnoreCase);
    }
}
