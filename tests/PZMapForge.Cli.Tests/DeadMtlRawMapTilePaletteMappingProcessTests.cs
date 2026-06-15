using System.Diagnostics;
using System.Text;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlRawMapTilePaletteMappingProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-palette-mapping-cli", Path.GetRandomFileName());

    public DeadMtlRawMapTilePaletteMappingProcessTests() =>
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
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stdout, stderr);
    }

    private string MakeInspectionJson()
    {
        var json = @"{
  ""format"": ""pzmapforge.deadmtl.raw-map-tile-inspection.v1"",
  ""source_image"": ""test.png"",
  ""sha256"": ""abc123"",
  ""width"": 256,
  ""height"": 256,
  ""top_colors"": [
    { ""color"": ""#FF6600"", ""count"": 32768, ""percentage"": 50.00 },
    { ""color"": ""#00AA10"", ""count"": 32768, ""percentage"": 50.00 }
  ]
}";
        var path = Path.Combine(_tempDir, "inspection.json");
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private string MakeWorldgenPalette()
    {
        var path = Path.Combine(_tempDir, "worldgen_palette.json");
        File.WriteAllText(path,
            @"{ ""entries"": [{ ""color"": ""#FF6600"", ""type"": ""prefab"", ""key"": ""normal_road_WE_00"" }] }",
            Encoding.UTF8);
        return path;
    }

    private string MakeSystem2Palette()
    {
        var path = Path.Combine(_tempDir, "system2_palette.json");
        File.WriteAllText(path,
            @"{ ""entries"": [{ ""hex"": ""#404040"", ""intent"": ""local_street_asphalt"" }] }",
            Encoding.UTF8);
        return path;
    }

    private (string Json, string Md, string Csv, string Summary) MakeOutputPaths() =>
    (
        Path.Combine(_tempDir, ".local", "mapping.json"),
        Path.Combine(_tempDir, ".local", "mapping.md"),
        Path.Combine(_tempDir, ".local", "mapping.csv"),
        Path.Combine(_tempDir, ".local", "mapping.summary.txt")
    );

    private void RunCommand(string inspection, string wg, string s2,
        string outJson, string outMd, string outCsv, string outSumm) =>
        RunCli("deadmtl-build-raw-map-tile-palette-mapping",
            "--inspection",      inspection,
            "--worldgen-palette", wg,
            "--system2-palette",  s2,
            "--output-json",      outJson,
            "--output-md",        outMd,
            "--output-csv",       outCsv,
            "--summary",          outSumm);

    // -----------------------------------------------------------------------
    // Exit zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_OnValidInspectionAndPalettes()
    {
        var insp = MakeInspectionJson();
        var wg   = MakeWorldgenPalette();
        var s2   = MakeSystem2Palette();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        var (code, stdout, stderr) = RunCli(
            "deadmtl-build-raw-map-tile-palette-mapping",
            "--inspection",      insp,
            "--worldgen-palette", wg,
            "--system2-palette",  s2,
            "--output-json",      outJson,
            "--output-md",        outMd,
            "--output-csv",       outCsv,
            "--summary",          outSumm);

        Assert.True(code == 0, $"Exit {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    // -----------------------------------------------------------------------
    // Output files created
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_CreatesOutputJson()
    {
        var insp = MakeInspectionJson();
        var wg   = MakeWorldgenPalette();
        var s2   = MakeSystem2Palette();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(insp, wg, s2, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outJson), "mapping.json not created");
    }

    [Fact]
    public void Cli_CreatesOutputMd()
    {
        var insp = MakeInspectionJson();
        var wg   = MakeWorldgenPalette();
        var s2   = MakeSystem2Palette();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(insp, wg, s2, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outMd), "mapping.md not created");
    }

    [Fact]
    public void Cli_CreatesOutputCsv()
    {
        var insp = MakeInspectionJson();
        var wg   = MakeWorldgenPalette();
        var s2   = MakeSystem2Palette();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(insp, wg, s2, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outCsv), "mapping.csv not created");
    }

    [Fact]
    public void Cli_CreatesSummaryTxt()
    {
        var insp = MakeInspectionJson();
        var wg   = MakeWorldgenPalette();
        var s2   = MakeSystem2Palette();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(insp, wg, s2, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outSumm), "mapping.summary.txt not created");
    }

    // -----------------------------------------------------------------------
    // JSON status fields
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("RAW_TILE_PALETTE_MAPPING_CONTRACT_ONLY")]
    [InlineData("NOT_RUNTIME_PROVEN")]
    [InlineData("NOT_IMPLEMENTED")]
    public void Cli_OutputJson_ContainsStatusField(string expected)
    {
        var insp = MakeInspectionJson();
        var wg   = MakeWorldgenPalette();
        var s2   = MakeSystem2Palette();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(insp, wg, s2, outJson, outMd, outCsv, outSumm);
        Assert.Contains(expected, File.ReadAllText(outJson), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV header
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputCsv_ContainsRequiredHeader()
    {
        var insp = MakeInspectionJson();
        var wg   = MakeWorldgenPalette();
        var s2   = MakeSystem2Palette();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(insp, wg, s2, outJson, outMd, outCsv, outSumm);
        Assert.Contains(
            "source_color,pixel_count,percentage,mapping_status,target_system,target_type,target_key,confidence,nearest_suggestions,notes",
            File.ReadAllText(outCsv), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Missing args → exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenRequiredArgsMissing()
    {
        var (code, _, stderr) = RunCli("deadmtl-build-raw-map-tile-palette-mapping");
        Assert.Equal(1, code);
        Assert.Contains("--inspection", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Output not under .local → exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var insp    = MakeInspectionJson();
        var wg      = MakeWorldgenPalette();
        var s2      = MakeSystem2Palette();
        var badJson = Path.Combine(_tempDir, "mapping.json");
        var (_, outMd, outCsv, outSumm) = MakeOutputPaths();

        var (code, _, stderr) = RunCli(
            "deadmtl-build-raw-map-tile-palette-mapping",
            "--inspection",      insp,
            "--worldgen-palette", wg,
            "--system2-palette",  s2,
            "--output-json",      badJson,
            "--output-md",        outMd,
            "--output-csv",       outCsv,
            "--summary",          outSumm);

        Assert.Equal(1, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Unknown command mentions new command
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommandError_MentionsPaletteMappingCommand()
    {
        var (code, _, stderr) = RunCli("not-a-real-command");
        Assert.Equal(1, code);
        Assert.Contains("deadmtl-build-raw-map-tile-palette-mapping",
            stderr, StringComparison.Ordinal);
    }
}

// -----------------------------------------------------------------------
// Helper script: static + process tests
// -----------------------------------------------------------------------

public sealed class DeadMtlRawMapTilePaletteMappingHelperScriptTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-raw-map-tile-palette-mapping.ps1");

    private static string OutDir =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "raw-map-tile-palette-mapping", "map_00");

    private static string OutJson    => Path.Combine(OutDir, "map_00.raw_tile_palette_mapping.json");
    private static string OutMd      => Path.Combine(OutDir, "map_00.raw_tile_palette_mapping.md");
    private static string OutCsv     => Path.Combine(OutDir, "map_00.raw_tile_palette_mapping.csv");
    private static string OutSummary => Path.Combine(OutDir, "map_00.raw_tile_palette_mapping.summary.txt");

    private static string DocPath =>
        Path.Combine(RepoRoot, "docs", "authoring", "DEADMTL_RAW_TILE_PALETTE_MAPPING_CONTRACT.md");

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
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
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
    public void Doc_MentionsNoWriterReadyClaim()
    {
        var text = File.ReadAllText(DocPath);
        Assert.True(
            text.Contains("writer_ready_claim", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("writer readiness is NOT claimed", StringComparison.OrdinalIgnoreCase),
            "doc should state no writer-ready claim");
    }

    [Fact]
    public void Readme_MentionsPaletteMappingScript() =>
        Assert.Contains("run-deadmtl-raw-map-tile-palette-mapping.ps1",
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
