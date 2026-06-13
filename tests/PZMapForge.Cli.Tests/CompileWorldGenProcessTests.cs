using System.Diagnostics;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class CompileWorldGenProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-compile-worldgen-tests", Path.GetRandomFileName());

    public CompileWorldGenProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private static string SampleManifest =>
        Path.Combine(RepoRoot, "examples", "worldgen", "worldgen_layers_sample.json");

    private string LocalOutputDir  => Path.Combine(_tempDir, ".local", "worldgen");
    private string LocalOutputFile => Path.Combine(LocalOutputDir, "WorldGenOverride.lua");

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
    // Test 1: sample manifest compiles to exit 0 and writes a file
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGen_SampleManifest_ExitsZeroAndWritesFile()
    {
        var (code, stdout, stderr) = RunCli(
            "compile-worldgen",
            "--input",  SampleManifest,
            "--output", LocalOutputFile);

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
        Assert.True(File.Exists(LocalOutputFile), $"Output file not found: {LocalOutputFile}");
    }

    // -----------------------------------------------------------------------
    // Test 2: stdout contains Status OK
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGen_SampleManifest_StdoutContainsStatusOk()
    {
        var (_, stdout, _) = RunCli(
            "compile-worldgen",
            "--input",  SampleManifest,
            "--output", LocalOutputFile);

        Assert.Contains("Status:        OK", stdout, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 3: generated Lua contains required markers
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGen_SampleManifest_LuaContainsRequiredMarkers()
    {
        RunCli("compile-worldgen", "--input", SampleManifest, "--output", LocalOutputFile);

        var lua = File.ReadAllText(LocalOutputFile);
        Assert.Contains("PZMAPFORGE_WORLDGENOVERRIDE_LOADED",   lua, StringComparison.Ordinal);
        Assert.Contains("worldgen[\"static_modules\"] = {",    lua, StringComparison.Ordinal);
        Assert.Contains("biome = worldgen.biomes.water",        lua, StringComparison.Ordinal);
        Assert.Contains("prefab = worldgen.prefabs.normal_road_WE_00", lua, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 4: generated Lua does NOT use the invalid local-var form
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGen_SampleManifest_LuaDoesNotUseLocalVarForm()
    {
        RunCli("compile-worldgen", "--input", SampleManifest, "--output", LocalOutputFile);

        var lua = File.ReadAllText(LocalOutputFile);
        Assert.DoesNotContain("local worldgenOverride",     lua, StringComparison.Ordinal);
        Assert.DoesNotContain("return worldgenOverride",    lua, StringComparison.Ordinal);
        Assert.DoesNotContain(".static_modules =",          lua, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 5: position form is correct
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGen_SampleManifest_PositionFormIsCorrect()
    {
        RunCli("compile-worldgen", "--input", SampleManifest, "--output", LocalOutputFile);

        var lua = File.ReadAllText(LocalOutputFile);
        Assert.Contains("position = { xmin =", lua, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 6: missing --input exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGen_MissingInput_ExitsNonZero()
    {
        var (code, _, stderr) = RunCli("compile-worldgen", "--output", LocalOutputFile);

        Assert.NotEqual(0, code);
        Assert.Contains("--input", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 7: missing --output exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGen_MissingOutput_ExitsNonZero()
    {
        var (code, _, stderr) = RunCli("compile-worldgen", "--input", SampleManifest);

        Assert.NotEqual(0, code);
        Assert.Contains("--output", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 8: input file not found exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGen_InputNotFound_ExitsNonZero()
    {
        var (code, _, stderr) = RunCli(
            "compile-worldgen",
            "--input",  Path.Combine(_tempDir, "nonexistent.json"),
            "--output", LocalOutputFile);

        Assert.NotEqual(0, code);
        Assert.Contains("not found", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 9: output outside .local exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGen_OutputOutsideLocal_ExitsNonZero()
    {
        var badOutput = Path.Combine(_tempDir, "not-local", "WorldGenOverride.lua");
        var (code, _, stderr) = RunCli(
            "compile-worldgen",
            "--input",  SampleManifest,
            "--output", badOutput);

        Assert.NotEqual(0, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 10: invalid JSON exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGen_InvalidJson_ExitsNonZero()
    {
        var badJson = Path.Combine(_tempDir, "bad.json");
        File.WriteAllText(badJson, "not valid json {{{");
        var (code, _, stderr) = RunCli(
            "compile-worldgen",
            "--input",  badJson,
            "--output", LocalOutputFile);

        Assert.NotEqual(0, code);
        Assert.Contains("JSON", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 11: unknown biome key exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGen_UnknownBiomeKey_ExitsNonZero()
    {
        var badJson = Path.Combine(_tempDir, "bad-biome.json");
        File.WriteAllText(badJson,
            """{"map_id":"x","format":"pzmapforge.worldgen.layers.v1","static_modules":[{"id":"a","type":"biome","key":"INVALID_BIOME","x1":0,"y1":0,"x2":10,"y2":10}]}""");

        var (code, _, stderr) = RunCli(
            "compile-worldgen",
            "--input",  badJson,
            "--output", LocalOutputFile);

        Assert.NotEqual(0, code);
        Assert.Contains("INVALID_BIOME", stderr, StringComparison.Ordinal);
    }
    // -----------------------------------------------------------------------
    // Test 12: generated Lua has no UTF-8 BOM and stays ASCII-safe for PZ Lua
    // -----------------------------------------------------------------------

    [Fact]
    public void CompileWorldGen_SampleManifest_OutputHasNoUtf8BomAndIsAscii()
    {
        RunCli("compile-worldgen", "--input", SampleManifest, "--output", LocalOutputFile);

        var bytes = File.ReadAllBytes(LocalOutputFile);

        Assert.False(
            bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
            "Output starts with UTF-8 BOM.");

        Assert.All(bytes, b =>
            Assert.True(
                b == 9 || b == 10 || b == 13 || (b >= 32 && b <= 126),
                $"Non-ASCII/control byte found: 0x{b:X2}"));
    }

}
