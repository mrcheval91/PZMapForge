using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadFilteredLocalTileSurveyProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-filtered-survey-cli", Path.GetRandomFileName());

    public System2StaticRoadFilteredLocalTileSurveyProcessTests() => Directory.CreateDirectory(_tempDir);

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

    private string MakeSurveyJson()
    {
        var json = """
{
  "format": "pzmapforge.deadmtl.system2.static-road-tile-family-survey.v1",
  "families": [
    {
      "candidate_family": "asphalt_road_surface_candidate",
      "role": "road_surface",
      "source_intents": ["primary roads"]
    }
  ]
}
""";
        var path = Path.Combine(_tempDir, "survey.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private (string Json, string Md, string Csv, string Summary) MakeOutputPaths() =>
    (
        Path.Combine(_tempDir, ".local", "filtered.json"),
        Path.Combine(_tempDir, ".local", "filtered.md"),
        Path.Combine(_tempDir, ".local", "filtered.csv"),
        Path.Combine(_tempDir, ".local", "filtered.summary.txt")
    );

    private void RunCommand(string input, string outJson, string outMd, string outCsv, string outSumm) =>
        RunCli("system2-build-static-road-local-tile-survey-filtered",
            "--input",       input,
            "--pz-root",     Path.Combine(_tempDir, "no-pz-root"),
            "--output-json", outJson,
            "--output-md",   outMd,
            "--output-csv",  outCsv,
            "--summary",     outSumm);

    // -----------------------------------------------------------------------
    // Exit zero (missing PZ root → valid zero-candidate survey)
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_OnFakeSurveyMissingPzRoot()
    {
        var input                              = MakeSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        var (code, stdout, stderr) = RunCli(
            "system2-build-static-road-local-tile-survey-filtered",
            "--input",       input,
            "--pz-root",     Path.Combine(_tempDir, "no-pz-root"),
            "--output-json", outJson,
            "--output-md",   outMd,
            "--output-csv",  outCsv,
            "--summary",     outSumm);

        Assert.True(code == 0, $"Exit {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // Output files created
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesOutputJson()
    {
        var input                              = MakeSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outJson), "filtered.json not created");
    }

    [Fact]
    public void Cli_CreatesOutputMd()
    {
        var input                              = MakeSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outMd), "filtered.md not created");
    }

    [Fact]
    public void Cli_CreatesOutputCsv()
    {
        var input                              = MakeSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outCsv), "filtered.csv not created");
    }

    [Fact]
    public void Cli_CreatesSummaryTxt()
    {
        var input                              = MakeSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outSumm), "filtered.summary.txt not created");
    }

    // -----------------------------------------------------------------------
    // JSON status fields
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("LOCAL_TILE_SURVEY_FILTERED_ONLY")]
    [InlineData("NOT_RUNTIME_PROVEN")]
    [InlineData("NOT_IMPLEMENTED")]
    public void Cli_OutputJson_ContainsStatusField(string expected)
    {
        var input                              = MakeSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.Contains(expected, File.ReadAllText(outJson), StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputJson_ContainsWriterReadyClaimFalse()
    {
        var input                              = MakeSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.Contains("writer_ready_claim", File.ReadAllText(outJson), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Markdown content
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputMd_ContainsMap22OTitle()
    {
        var input                              = MakeSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.Contains("MAP-22O", File.ReadAllText(outMd), StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputMd_ContainsVerdict()
    {
        var input                              = MakeSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.Contains("MAP22O_SYSTEM2_STATIC_ROAD_LOCAL_TILE_SURVEY_FILTERED_COMPLETE",
            File.ReadAllText(outMd), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV header
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputCsv_ContainsRequiredHeader()
    {
        var input                              = MakeSurveyJson();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.Contains(
            "candidate_family,role,tile_name,source_file,match_reason,confidence,resolution_status",
            File.ReadAllText(outCsv), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Missing args → exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenRequiredArgsMissing()
    {
        var (code, _, stderr) = RunCli("system2-build-static-road-local-tile-survey-filtered");
        Assert.Equal(1, code);
        Assert.Contains("--input", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Output not under .local → exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var input    = MakeSurveyJson();
        var badJson  = Path.Combine(_tempDir, "filtered.json");
        var (_, outMd, outCsv, outSumm) = MakeOutputPaths();

        var (code, _, stderr) = RunCli(
            "system2-build-static-road-local-tile-survey-filtered",
            "--input",       input,
            "--pz-root",     Path.Combine(_tempDir, "no-pz-root"),
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
    public void Cli_UnknownCommandError_MentionsFilteredSurveyCommand()
    {
        var (code, _, stderr) = RunCli("not-a-real-command");
        Assert.Equal(1, code);
        Assert.Contains("system2-build-static-road-local-tile-survey-filtered",
            stderr, StringComparison.Ordinal);
    }
}

// -----------------------------------------------------------------------
// Helper script: static + process tests
// -----------------------------------------------------------------------

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadFilteredLocalTileSurveyHelperScriptTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-system2-static-road-local-tile-survey-filtered.ps1");

    private static string OutDir =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring",
            "system2-static-road-local-tile-survey-filtered");

    private static string OutJson =>
        Path.Combine(OutDir, "system2_static_road_local_tile_survey_filtered.json");

    private static string OutMd =>
        Path.Combine(OutDir, "system2_static_road_local_tile_survey_filtered.md");

    private static string OutCsv =>
        Path.Combine(OutDir, "system2_static_road_local_tile_survey_filtered.csv");

    private static string OutSummary =>
        Path.Combine(OutDir, "system2_static_road_local_tile_survey_filtered.summary.txt");

    private static string DocPath =>
        Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_SYSTEM2_STATIC_ROAD_LOCAL_TILE_SURVEY_FILTERED.md");

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
            text.Contains("Runtime proof is NOT claimed", StringComparison.OrdinalIgnoreCase),
            "doc should state no runtime proof");
    }

    [Fact]
    public void Doc_MentionsNoLotpack()
    {
        var text = File.ReadAllText(DocPath);
        Assert.True(
            text.Contains("lotpack", StringComparison.OrdinalIgnoreCase),
            "doc should mention lotpack claim boundary");
    }

    [Fact]
    public void Doc_StatesSourceFilteringOnly()
    {
        var text = File.ReadAllText(DocPath);
        Assert.True(
            text.Contains("source filtering", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("source-filtered", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("hygiene", StringComparison.OrdinalIgnoreCase),
            "doc should state this is source filtering only");
    }

    [Fact]
    public void Readme_MentionsFilteredSurveyScript() =>
        Assert.Contains("run-system2-static-road-local-tile-survey-filtered.ps1",
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
}
