using System.Diagnostics;
using System.Runtime.Versioning;
using Xunit;

namespace PZMapForge.Cli.Tests;

[SupportedOSPlatform("windows")]
public sealed class DeadMtlRoadLayerPlaceholderProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-road-placeholder-process", Path.GetRandomFileName());

    public DeadMtlRoadLayerPlaceholderProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string PlaceholderScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "generate-road-layer-placeholders.ps1");

    // Temp output dir under .local/ — not required by this script but mirrors convention
    private string LocalOutputDir => Path.Combine(_tempDir, ".local", "road-layer-placeholders");

    private static (int ExitCode, string Stdout, string Stderr) RunPowerShell(string script, params string[] args)
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
        psi.ArgumentList.Add(script);
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        proc.WaitForExit();
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Process tests
    // -----------------------------------------------------------------------

    [Fact]
    public void PlaceholderScript_ExitsZero()
    {
        var (code, stdout, stderr) = RunPowerShell(PlaceholderScript, "-OutputDir", LocalOutputDir);

        Assert.True(code == 0,
            $"Placeholder script exited {code}.\nStdout: {stdout}\nStderr: {stderr}");
    }

    [Fact]
    public void PlaceholderScript_CreatesHighwayPngNonzero()
    {
        RunPowerShell(PlaceholderScript, "-OutputDir", LocalOutputDir);

        var path = Path.Combine(LocalOutputDir, "layers", "roads_highway.png");
        Assert.True(File.Exists(path), "roads_highway.png not found");
        Assert.True(new FileInfo(path).Length > 0, "roads_highway.png is empty");
    }

    [Fact]
    public void PlaceholderScript_CreatesMajorRoadPngNonzero()
    {
        RunPowerShell(PlaceholderScript, "-OutputDir", LocalOutputDir);

        var path = Path.Combine(LocalOutputDir, "layers", "roads_major.png");
        Assert.True(File.Exists(path), "roads_major.png not found");
        Assert.True(new FileInfo(path).Length > 0, "roads_major.png is empty");
    }

    [Fact]
    public void PlaceholderScript_CreatesLocalRoadPngNonzero()
    {
        RunPowerShell(PlaceholderScript, "-OutputDir", LocalOutputDir);

        var path = Path.Combine(LocalOutputDir, "layers", "roads_local.png");
        Assert.True(File.Exists(path), "roads_local.png not found");
        Assert.True(new FileInfo(path).Length > 0, "roads_local.png is empty");
    }

    [Fact]
    public void PlaceholderScript_CreatesAlleysPngNonzero()
    {
        RunPowerShell(PlaceholderScript, "-OutputDir", LocalOutputDir);

        var path = Path.Combine(LocalOutputDir, "layers", "roads_alleys.png");
        Assert.True(File.Exists(path), "roads_alleys.png not found");
        Assert.True(new FileInfo(path).Length > 0, "roads_alleys.png is empty");
    }

    [Fact]
    public void PlaceholderScript_CreatesServiceRoadPngNonzero()
    {
        RunPowerShell(PlaceholderScript, "-OutputDir", LocalOutputDir);

        var path = Path.Combine(LocalOutputDir, "layers", "roads_service.png");
        Assert.True(File.Exists(path), "roads_service.png not found");
        Assert.True(new FileInfo(path).Length > 0, "roads_service.png is empty");
    }

    [Fact]
    public void PlaceholderScript_CreatesReadme()
    {
        RunPowerShell(PlaceholderScript, "-OutputDir", LocalOutputDir);

        Assert.True(File.Exists(Path.Combine(LocalOutputDir, "README.md")),
            "README.md not found");
    }

    [Fact]
    public void PlaceholderScript_ReadmeMentionsPlaceholder()
    {
        RunPowerShell(PlaceholderScript, "-OutputDir", LocalOutputDir);

        var text = File.ReadAllText(Path.Combine(LocalOutputDir, "README.md"));
        Assert.True(
            text.Contains("placeholder", StringComparison.OrdinalIgnoreCase),
            "README should describe files as placeholders");
    }

    [Fact]
    public void PlaceholderScript_DoesNotGenerateLua()
    {
        RunPowerShell(PlaceholderScript, "-OutputDir", LocalOutputDir);

        var luaFiles = Directory.GetFiles(LocalOutputDir, "*.lua", SearchOption.AllDirectories);
        Assert.Empty(luaFiles);
    }
}
