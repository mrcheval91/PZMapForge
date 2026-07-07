using System.Diagnostics;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlVanillaBuildingSourceDiscoveryProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-vanilla-disc-cli", Path.GetRandomFileName());

    public DeadMtlVanillaBuildingSourceDiscoveryProcessTests() =>
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

    private (string Json, string Md, string Csv, string Summary) MakeOutputPaths() =>
    (
        Path.Combine(_tempDir, ".local", "disc.json"),
        Path.Combine(_tempDir, ".local", "disc.md"),
        Path.Combine(_tempDir, ".local", "disc.csv"),
        Path.Combine(_tempDir, ".local", "disc.summary.txt")
    );

    private (int ExitCode, string Stdout, string Stderr) RunDiscover()
    {
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        return RunCli(
            "deadmtl-discover-vanilla-building-sources",
            "--output-json", outJson,
            "--output-md",   outMd,
            "--output-csv",  outCsv,
            "--summary",     outSumm);
    }

    // -----------------------------------------------------------------------
    // Exit zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_OnValidOutputPaths()
    {
        var (code, stdout, stderr) = RunDiscover();
        Assert.True(code == 0, $"Exit {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // Output files created
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesOutputJson()
    {
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCli("deadmtl-discover-vanilla-building-sources",
            "--output-json", outJson, "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);
        Assert.True(File.Exists(outJson), "disc.json not created");
    }

    [Fact]
    public void Cli_CreatesOutputMd()
    {
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCli("deadmtl-discover-vanilla-building-sources",
            "--output-json", outJson, "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);
        Assert.True(File.Exists(outMd), "disc.md not created");
    }

    [Fact]
    public void Cli_CreatesOutputCsv()
    {
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCli("deadmtl-discover-vanilla-building-sources",
            "--output-json", outJson, "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);
        Assert.True(File.Exists(outCsv), "disc.csv not created");
    }

    [Fact]
    public void Cli_CreatesSummaryTxt()
    {
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCli("deadmtl-discover-vanilla-building-sources",
            "--output-json", outJson, "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);
        Assert.True(File.Exists(outSumm), "disc.summary.txt not created");
    }

    // -----------------------------------------------------------------------
    // JSON format field
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputJson_ContainsFormatField()
    {
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCli("deadmtl-discover-vanilla-building-sources",
            "--output-json", outJson, "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);
        Assert.Contains(
            "pzmapforge.deadmtl.vanilla-building-source-discovery.v1",
            File.ReadAllText(outJson), StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputJson_ContainsNotRuntimeProven()
    {
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCli("deadmtl-discover-vanilla-building-sources",
            "--output-json", outJson, "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);
        Assert.Contains("NOT_RUNTIME_PROVEN", File.ReadAllText(outJson), StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputJson_ClaimBoundary_AllFalse()
    {
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCli("deadmtl-discover-vanilla-building-sources",
            "--output-json", outJson, "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);
        var text = File.ReadAllText(outJson);
        Assert.DoesNotContain("\"writes_lotpack\": true",          text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"building_extraction_claim\": true", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"editable_vanilla_catalogue_claim\": true", text, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // CSV header
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputCsv_ContainsRequiredHeader()
    {
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCli("deadmtl-discover-vanilla-building-sources",
            "--output-json", outJson, "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm);
        Assert.Contains(
            "kind,root,map_folder,relative_path,lotheader_count,lotpack_count,tmx_count,tbx_count,building_count,map_info_present,mod_info_present,objects_lua_present,spawnpoints_lua_present,spawnregions_lua_present,editable_template_source,compiled_map_source,vanilla_source,sample_files",
            File.ReadAllText(outCsv), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Root args
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_WithAllFourRootArgs()
    {
        var fakeRoot = Path.Combine(_tempDir, "fake_pz");
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        var (code, stdout, stderr) = RunCli(
            "deadmtl-discover-vanilla-building-sources",
            "--output-json", outJson, "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm,
            "--pz-root",           Path.Combine(_tempDir, "fake_pz"),
            "--tools-root",        Path.Combine(_tempDir, "fake_tools"),
            "--user-zomboid-root", Path.Combine(_tempDir, "fake_user"),
            "--workspace-root",    Path.Combine(_tempDir, "fake_ws"));
        Assert.True(code == 0, $"Exit {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void Cli_OutputJson_ContainsFakeRootPath_WhenPzRootProvided()
    {
        var fakePath = Path.Combine(_tempDir, "my_custom_pz_root");
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCli("deadmtl-discover-vanilla-building-sources",
            "--output-json", outJson, "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm,
            "--pz-root", fakePath);
        // JSON escapes backslashes so check the leaf directory name, not the full path
        Assert.Contains("my_custom_pz_root", File.ReadAllText(outJson), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cli_OutputJson_MissingRootMarkedExistsFalse_WhenFakeRootProvided()
    {
        var fakePath = Path.Combine(_tempDir, "nonexistent_pz_root_12345");
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        var (code, _, _) = RunCli(
            "deadmtl-discover-vanilla-building-sources",
            "--output-json", outJson, "--output-md", outMd, "--output-csv", outCsv, "--summary", outSumm,
            "--pz-root", fakePath);
        Assert.Equal(0, code);
        Assert.Contains("false", File.ReadAllText(outJson), StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Missing args → exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenRequiredArgsMissing()
    {
        var (code, _, stderr) = RunCli("deadmtl-discover-vanilla-building-sources");
        Assert.Equal(1, code);
        Assert.Contains("--output-json", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Output not under .local → exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var badJson = Path.Combine(_tempDir, "disc.json");
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        var (code, _, stderr) = RunCli(
            "deadmtl-discover-vanilla-building-sources",
            "--output-json", badJson,
            "--output-md",   outMd,
            "--output-csv",  outCsv,
            "--summary",     outSumm);

        Assert.Equal(1, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Unknown command mentions new command
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommandError_MentionsDiscoveryCommand()
    {
        var (code, _, stderr) = RunCli("not-a-real-command");
        Assert.Equal(1, code);
        Assert.Contains("deadmtl-discover-vanilla-building-sources",
            stderr, StringComparison.Ordinal);
    }
}

// -----------------------------------------------------------------------
// Helper script: static + process tests
// -----------------------------------------------------------------------

public sealed class DeadMtlVanillaBuildingSourceDiscoveryHelperScriptTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-vanilla-building-source-discovery.ps1");

    private static string OutDir =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "vanilla-building-source-discovery");

    private static string OutJson    => Path.Combine(OutDir, "vanilla_building_source_discovery.json");
    private static string OutMd      => Path.Combine(OutDir, "vanilla_building_source_discovery.md");
    private static string OutCsv     => Path.Combine(OutDir, "vanilla_building_source_discovery.csv");
    private static string OutSummary => Path.Combine(OutDir, "vanilla_building_source_discovery.summary.txt");

    private static string DocPath =>
        Path.Combine(RepoRoot, "docs", "authoring", "DEADMTL_VANILLA_BUILDING_SOURCE_DISCOVERY.md");

    private static string ReadmePath =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "README.md");

    private static (int ExitCode, string Stdout, string Stderr) RunScript()
    {
        var psi = new ProcessStartInfo
        {
            FileName               = "powershell",
            WorkingDirectory       = RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        psi.ArgumentList.Add("-ExecutionPolicy");
        psi.ArgumentList.Add("Bypass");
        psi.ArgumentList.Add("-File");
        psi.ArgumentList.Add(HelperScript);

        using var proc = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Static: forbidden strings
    // -----------------------------------------------------------------------

    [Fact]
    public void HelperScript_DoesNotContainCompileWorldgen() =>
        Assert.DoesNotContain("compile-worldgen", File.ReadAllText(HelperScript), StringComparison.Ordinal);

    [Fact]
    public void HelperScript_DoesNotContainWorldGenOverrideLua() =>
        Assert.DoesNotContain("WorldGenOverride.lua", File.ReadAllText(HelperScript), StringComparison.Ordinal);

    [Fact]
    public void HelperScript_DoesNotContainLotpack() =>
        Assert.DoesNotContain(".lotpack", File.ReadAllText(HelperScript), StringComparison.Ordinal);

    // -----------------------------------------------------------------------
    // Static: doc
    // -----------------------------------------------------------------------

    [Fact]
    public void Doc_MentionsNoRuntimeProof()
    {
        var text = File.ReadAllText(DocPath);
        Assert.True(
            text.Contains("NOT_RUNTIME_PROVEN", StringComparison.Ordinal) ||
            text.Contains("runtime proof is NOT claimed", StringComparison.OrdinalIgnoreCase),
            "doc should state no runtime proof");
    }

    [Fact]
    public void Doc_MentionsNoBuildingExtractionClaim()
    {
        var text = File.ReadAllText(DocPath);
        Assert.True(
            text.Contains("building_extraction_claim", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("no building extraction", StringComparison.OrdinalIgnoreCase),
            "doc should state no building extraction claim");
    }

    [Fact]
    public void Doc_MentionsNoEditableVanillaCatalogueClaim()
    {
        var text = File.ReadAllText(DocPath);
        Assert.True(
            text.Contains("editable_vanilla_catalogue_claim", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("no editable catalogue", StringComparison.OrdinalIgnoreCase),
            "doc should state no editable vanilla catalogue claim");
    }

    [Fact]
    public void Readme_MentionsDiscoveryScript() =>
        Assert.Contains("run-deadmtl-vanilla-building-source-discovery.ps1",
            File.ReadAllText(ReadmePath), StringComparison.Ordinal);

    // -----------------------------------------------------------------------
    // Process
    // -----------------------------------------------------------------------

    [Fact]
    public void HelperScript_ExitsZero()
    {
        var (code, stdout, stderr) = RunScript();
        Assert.True(code == 0,
            $"Script exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void HelperScript_CreatesOutJson()
    {
        RunScript();
        Assert.True(File.Exists(OutJson), $"Expected: {OutJson}");
    }

    [Fact]
    public void HelperScript_CreatesOutMd()
    {
        RunScript();
        Assert.True(File.Exists(OutMd), $"Expected: {OutMd}");
    }

    [Fact]
    public void HelperScript_CreatesOutCsv()
    {
        RunScript();
        Assert.True(File.Exists(OutCsv), $"Expected: {OutCsv}");
    }

    [Fact]
    public void HelperScript_CreatesOutSummary()
    {
        RunScript();
        Assert.True(File.Exists(OutSummary), $"Expected: {OutSummary}");
    }

    [Fact]
    public void HelperScript_DoesNotCreateWorldGenOverrideLua()
    {
        RunScript();
        Assert.False(File.Exists(Path.Combine(OutDir, "WorldGenOverride.lua")),
            "WorldGenOverride.lua must not be written");
    }

    [Fact]
    public void HelperScript_DoesNotCreateLotpackFile()
    {
        RunScript();
        var lotpacks = Directory.Exists(OutDir)
            ? Directory.GetFiles(OutDir, "*.lotpack")
            : Array.Empty<string>();
        Assert.Empty(lotpacks);
    }

    [Fact]
    public void HelperScript_OutputJson_ContainsFormatField()
    {
        RunScript();
        if (!File.Exists(OutJson)) return;
        Assert.Contains(
            "pzmapforge.deadmtl.vanilla-building-source-discovery.v1",
            File.ReadAllText(OutJson), StringComparison.Ordinal);
    }
}
