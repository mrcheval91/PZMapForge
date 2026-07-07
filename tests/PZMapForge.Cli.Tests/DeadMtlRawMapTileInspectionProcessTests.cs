using System.Diagnostics;
using System.Drawing;
using System.Runtime.Versioning;
using System.Text;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlRawMapTileInspectionProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-raw-tile-cli", Path.GetRandomFileName());

    public DeadMtlRawMapTileInspectionProcessTests() =>
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

    private string MakePng256()
    {
        var path = Path.Combine(_tempDir, "test_256.png");
        using var bmp = new Bitmap(256, 256);
        for (var y = 0; y < 256; y++)
            for (var x = 0; x < 256; x++)
                bmp.SetPixel(x, y, Color.FromArgb(255, 0x40, 0x40, 0x40));
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        return path;
    }

    private (string Json, string Md, string Csv, string Summary) MakeOutputPaths() =>
    (
        Path.Combine(_tempDir, ".local", "inspection.json"),
        Path.Combine(_tempDir, ".local", "inspection.md"),
        Path.Combine(_tempDir, ".local", "inspection.csv"),
        Path.Combine(_tempDir, ".local", "inspection.summary.txt")
    );

    private void RunCommand(string input, string outJson, string outMd, string outCsv, string outSumm) =>
        RunCli("deadmtl-inspect-raw-map-tile",
            "--input",       input,
            "--output-json", outJson,
            "--output-md",   outMd,
            "--output-csv",  outCsv,
            "--summary",     outSumm);

    // -----------------------------------------------------------------------
    // Exit zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_OnValid256x256Png()
    {
        var input                              = MakePng256();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();

        var (code, stdout, stderr) = RunCli(
            "deadmtl-inspect-raw-map-tile",
            "--input",       input,
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
        var input                              = MakePng256();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outJson), "inspection.json not created");
    }

    [Fact]
    public void Cli_CreatesOutputMd()
    {
        var input                              = MakePng256();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outMd), "inspection.md not created");
    }

    [Fact]
    public void Cli_CreatesOutputCsv()
    {
        var input                              = MakePng256();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outCsv), "inspection.csv not created");
    }

    [Fact]
    public void Cli_CreatesSummaryTxt()
    {
        var input                              = MakePng256();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.True(File.Exists(outSumm), "inspection.summary.txt not created");
    }

    // -----------------------------------------------------------------------
    // JSON status fields
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("RAW_TILE_INSPECTION_ONLY")]
    [InlineData("NOT_RUNTIME_PROVEN")]
    [InlineData("NOT_IMPLEMENTED")]
    public void Cli_OutputJson_ContainsStatusField(string expected)
    {
        var input                              = MakePng256();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.Contains(expected, File.ReadAllText(outJson), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Markdown content
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputMd_ContainsMap23ATitle()
    {
        var input                              = MakePng256();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.Contains("MAP-23A", File.ReadAllText(outMd), StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_OutputMd_ContainsVerdict()
    {
        var input                              = MakePng256();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.Contains("MAP23A_RAW_256_MAP_TILE_INSPECTION_COMPLETE",
            File.ReadAllText(outMd), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV header
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputCsv_ContainsRequiredHeader()
    {
        var input                              = MakePng256();
        var (outJson, outMd, outCsv, outSumm) = MakeOutputPaths();
        RunCommand(input, outJson, outMd, outCsv, outSumm);
        Assert.Contains(
            "hex_color,count,percentage,in_worldgen_palette,in_system2_palette,is_unknown",
            File.ReadAllText(outCsv), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Missing args → exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenRequiredArgsMissing()
    {
        var (code, _, stderr) = RunCli("deadmtl-inspect-raw-map-tile");
        Assert.Equal(1, code);
        Assert.Contains("--input", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Output not under .local → exit 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsOne_WhenOutputNotUnderLocal()
    {
        var input   = MakePng256();
        var badJson = Path.Combine(_tempDir, "inspection.json");
        var (_, outMd, outCsv, outSumm) = MakeOutputPaths();

        var (code, _, stderr) = RunCli(
            "deadmtl-inspect-raw-map-tile",
            "--input",       input,
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
    public void Cli_UnknownCommandError_MentionsInspectCommand()
    {
        var (code, _, stderr) = RunCli("not-a-real-command");
        Assert.Equal(1, code);
        Assert.Contains("deadmtl-inspect-raw-map-tile",
            stderr, StringComparison.Ordinal);
    }
}

// -----------------------------------------------------------------------
// Helper script: static + process tests
// -----------------------------------------------------------------------

[SupportedOSPlatform("windows")]
public sealed class DeadMtlRawMapTileInspectionHelperScriptTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-raw-map-tile-inspection.ps1");

    private static string OutDir =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "raw-map-tile-inspection", "map_00");

    private static string OutJson =>
        Path.Combine(OutDir, "map_00.raw_tile_inspection.json");

    private static string OutMd =>
        Path.Combine(OutDir, "map_00.raw_tile_inspection.md");

    private static string OutCsv =>
        Path.Combine(OutDir, "map_00.raw_tile_colors.csv");

    private static string OutSummary =>
        Path.Combine(OutDir, "map_00.raw_tile_inspection.summary.txt");

    private static string DocPath =>
        Path.Combine(RepoRoot, "docs", "authoring", "DEADMTL_RAW_256_MAP_TILE_INSPECTION.md");

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
    public void Readme_MentionsInspectionScript() =>
        Assert.Contains("run-deadmtl-raw-map-tile-inspection.ps1",
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
