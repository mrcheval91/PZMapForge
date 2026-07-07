using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class System2StaticRoadExtractHelperScriptTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-system2-static-road-extract.ps1");

    private static string ExtractDir =>
        Path.Combine(RepoRoot, ".local", "deadmtl-authoring", "system2-static-road-extract");

    private static string ExtractJson =>
        Path.Combine(ExtractDir, "system2_static_road_extract.json");

    private static string SummaryTxt =>
        Path.Combine(ExtractDir, "system2_static_road_extract.summary.txt");

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
    // Static content: forbidden strings
    // -----------------------------------------------------------------------

    [Fact]
    public void HelperScript_DoesNotContainCompileWorldgen()
    {
        var text = File.ReadAllText(HelperScript);
        Assert.DoesNotContain("compile-worldgen", text, StringComparison.Ordinal);
    }

    [Fact]
    public void HelperScript_DoesNotContainWorldGenOverrideLua()
    {
        var text = File.ReadAllText(HelperScript);
        Assert.DoesNotContain("WorldGenOverride.lua", text, StringComparison.Ordinal);
    }

    [Fact]
    public void HelperScript_DoesNotContainLotpack()
    {
        var text = File.ReadAllText(HelperScript);
        Assert.DoesNotContain(".lotpack", text, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Path computation: repo root is three parents up from script dir
    // -----------------------------------------------------------------------

    [Fact]
    public void HelperScript_ComputesRepoRootAsThreeParentsUp()
    {
        var text = File.ReadAllText(HelperScript);
        Assert.Contains("Split-Path -Parent | Split-Path -Parent | Split-Path -Parent",
            text, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Process tests: run the script end-to-end
    // -----------------------------------------------------------------------

    [Fact]
    public void HelperScript_ExitsZero()
    {
        var (code, stdout, stderr) = RunScript();
        Assert.True(code == 0,
            $"Script exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void HelperScript_CreatesExtractJson()
    {
        RunScript();
        Assert.True(File.Exists(ExtractJson), $"Expected: {ExtractJson}");
    }

    [Fact]
    public void HelperScript_CreatesSummaryTxt()
    {
        RunScript();
        Assert.True(File.Exists(SummaryTxt), $"Expected: {SummaryTxt}");
    }

    [Fact]
    public void HelperScript_ExtractJson_ContainsExtractOnly()
    {
        RunScript();
        var text = File.ReadAllText(ExtractJson);
        Assert.Contains("EXTRACT_ONLY", text, StringComparison.Ordinal);
    }

    [Fact]
    public void HelperScript_ExtractJson_ContainsNotRuntimeProven()
    {
        RunScript();
        var text = File.ReadAllText(ExtractJson);
        Assert.Contains("NOT_RUNTIME_PROVEN", text, StringComparison.Ordinal);
    }

    [Fact]
    public void HelperScript_ExtractJson_ContainsNotImplemented()
    {
        RunScript();
        var text = File.ReadAllText(ExtractJson);
        Assert.Contains("NOT_IMPLEMENTED", text, StringComparison.Ordinal);
    }

    [Fact]
    public void HelperScript_ExtractJson_TotalsLayerCountIsSix()
    {
        RunScript();
        using var doc = JsonDocument.Parse(File.ReadAllText(ExtractJson));
        var layerCount = doc.RootElement
            .GetProperty("totals")
            .GetProperty("layer_count")
            .GetInt32();
        Assert.Equal(6, layerCount);
    }

    [Fact]
    public void HelperScript_ExtractJson_TotalsNonEmptyPixelsIsZero_ForPlaceholders()
    {
        RunScript();
        using var doc = JsonDocument.Parse(File.ReadAllText(ExtractJson));
        var nonEmpty = doc.RootElement
            .GetProperty("totals")
            .GetProperty("non_empty_pixels")
            .GetInt32();
        Assert.Equal(0, nonEmpty);
    }

    [Fact]
    public void HelperScript_DoesNotCreateWorldGenOverrideLua_InExtractDir()
    {
        RunScript();
        var luaPath = Path.Combine(ExtractDir, "WorldGenOverride.lua");
        Assert.False(File.Exists(luaPath), "WorldGenOverride.lua must not be written");
    }

    [Fact]
    public void HelperScript_DoesNotCreateLotpack_InExtractDir()
    {
        RunScript();
        var lotpacks = Directory.Exists(ExtractDir)
            ? Directory.GetFiles(ExtractDir, "*.lotpack")
            : Array.Empty<string>();
        Assert.Empty(lotpacks);
    }
}
