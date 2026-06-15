using System.Diagnostics;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderBuildingSelectionPolicyPlanProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-bspp-cli", Path.GetRandomFileName());

    public DeadMtlWorldBuilderBuildingSelectionPolicyPlanProcessTests() =>
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

    private static string ProfilePath =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "worldbuilder", "neighborhoods",
            "deadmtl_baseline_neighborhood_profile.json");

    private static string MetadataPath =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "worldbuilder", "tiles",
            "map_00.zone_metadata.json");

    private static string LotPlanPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-lot-subdivision-plan",
            "map_00", "map_00.lot_subdivision_plan.json");

    private static string SidewalkPlanPath =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "worldbuilder-sidewalk-generation-plan",
            "map_00", "map_00.sidewalk_generation_plan.json");

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-building-selection-policy-plan.ps1");

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
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stdout, stderr);
    }

    private (string Json, string Md, string Csv, string Summary) MakeOutputPaths(string subdir = "default")
    {
        var dir = Path.Combine(_tempDir, ".local", subdir);
        Directory.CreateDirectory(dir);
        return (
            Path.Combine(dir, "bspp.json"),
            Path.Combine(dir, "bspp.md"),
            Path.Combine(dir, "bspp.csv"),
            Path.Combine(dir, "bspp.summary.txt")
        );
    }

    private (int ExitCode, string Stdout, string Stderr) RunBuild()
    {
        var (j, m, c, s) = MakeOutputPaths();
        return RunCli(
            "deadmtl-build-worldbuilder-building-selection-policy-plan",
            "--profile",       ProfilePath,
            "--metadata",      MetadataPath,
            "--lot-plan",      LotPlanPath,
            "--sidewalk-plan", SidewalkPlanPath,
            "--output-json",   j,
            "--output-md",     m,
            "--output-csv",    c,
            "--summary",       s);
    }

    // -----------------------------------------------------------------------
    // Exit codes
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_WithValidInputs()
    {
        var (code, stdout, stderr) = RunBuild();
        Assert.True(code == 0, $"Exit {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void Cli_ExitsOne_WhenMissingArgs()
    {
        var (code, _, _) = RunCli("deadmtl-build-worldbuilder-building-selection-policy-plan");
        Assert.Equal(1, code);
    }

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var outsideDir = Path.Combine(_tempDir, "not-local");
        Directory.CreateDirectory(outsideDir);
        var (code, _, _) = RunCli(
            "deadmtl-build-worldbuilder-building-selection-policy-plan",
            "--profile",       ProfilePath,
            "--metadata",      MetadataPath,
            "--lot-plan",      LotPlanPath,
            "--sidewalk-plan", SidewalkPlanPath,
            "--output-json",   Path.Combine(outsideDir, "out.json"),
            "--output-md",     Path.Combine(outsideDir, "out.md"),
            "--output-csv",    Path.Combine(outsideDir, "out.csv"),
            "--summary",       Path.Combine(outsideDir, "out.txt"));
        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // Output files created
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesOutputJson()
    {
        var (j, m, c, s) = MakeOutputPaths("json");
        RunCli("deadmtl-build-worldbuilder-building-selection-policy-plan",
            "--profile", ProfilePath, "--metadata", MetadataPath,
            "--lot-plan", LotPlanPath, "--sidewalk-plan", SidewalkPlanPath,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(j), "output JSON not created");
    }

    [Fact]
    public void Cli_CreatesOutputMd()
    {
        var (j, m, c, s) = MakeOutputPaths("md");
        RunCli("deadmtl-build-worldbuilder-building-selection-policy-plan",
            "--profile", ProfilePath, "--metadata", MetadataPath,
            "--lot-plan", LotPlanPath, "--sidewalk-plan", SidewalkPlanPath,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(m), "output MD not created");
    }

    [Fact]
    public void Cli_CreatesOutputCsv()
    {
        var (j, m, c, s) = MakeOutputPaths("csv");
        RunCli("deadmtl-build-worldbuilder-building-selection-policy-plan",
            "--profile", ProfilePath, "--metadata", MetadataPath,
            "--lot-plan", LotPlanPath, "--sidewalk-plan", SidewalkPlanPath,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(c), "output CSV not created");
    }

    [Fact]
    public void Cli_CreatesOutputSummary()
    {
        var (j, m, c, s) = MakeOutputPaths("summary");
        RunCli("deadmtl-build-worldbuilder-building-selection-policy-plan",
            "--profile", ProfilePath, "--metadata", MetadataPath,
            "--lot-plan", LotPlanPath, "--sidewalk-plan", SidewalkPlanPath,
            "--output-json", j, "--output-md", m, "--output-csv", c, "--summary", s);
        Assert.True(File.Exists(s), "output summary not created");
    }

    // -----------------------------------------------------------------------
    // Stdout verdict
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_Stdout_ContainsVerdict()
    {
        var (_, stdout, _) = RunBuild();
        Assert.Contains("MAP25E_WORLDBUILDER_BUILDING_SELECTION_POLICY_PLAN_CONTRACT_COMPLETE",
            stdout, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Unknown command
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommand_MentionsBuildingSelectionPolicyPlanCommand()
    {
        var (_, stdout, stderr) = RunCli("unknown-xyz-command");
        var combined = stdout + stderr;
        Assert.Contains("deadmtl-build-worldbuilder-building-selection-policy-plan",
            combined, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Helper script
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
    // Doc and README
    // -----------------------------------------------------------------------

    [Fact]
    public void Doc_Exists()
    {
        var docPath = Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_WORLDBUILDER_BUILDING_SELECTION_POLICY_PLAN_CONTRACT.md");
        Assert.True(File.Exists(docPath), $"Doc not found: {docPath}");
    }

    [Fact]
    public void Doc_MentionsContractOnly_AndNoBuildingPlacement()
    {
        var docPath = Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_WORLDBUILDER_BUILDING_SELECTION_POLICY_PLAN_CONTRACT.md");
        var text = File.ReadAllText(docPath);
        Assert.True(
            text.Contains("CONTRACT ONLY", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("contract only", StringComparison.OrdinalIgnoreCase),
            "doc should state CONTRACT ONLY");
        Assert.True(
            text.Contains("No building generation", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("no building generation", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("No building placement", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("no building placement", StringComparison.OrdinalIgnoreCase),
            "doc should state no building generation / placement");
    }

    [Fact]
    public void README_MentionsHelperScript()
    {
        var readme = Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "README.md");
        Assert.True(File.Exists(readme), $"README not found: {readme}");
        var text = File.ReadAllText(readme);
        Assert.Contains("run-deadmtl-worldbuilder-building-selection-policy-plan",
            text, StringComparison.Ordinal);
    }
}
