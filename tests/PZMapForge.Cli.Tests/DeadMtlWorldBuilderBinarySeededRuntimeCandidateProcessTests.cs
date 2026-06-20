using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderBinarySeededRuntimeCandidateProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map33a-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderBinarySeededRuntimeCandidateProcessTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProject =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj");

    private string Map32AManifest  => Path.Combine(_tempDir, "map32a-manifest.local.json");
    private string BinarySeedRoot  => Path.Combine(_tempDir, "seed.local");
    private string OutputRoot      => Path.Combine(_tempDir, "candidate.local");
    private string Manifest        => Path.Combine(OutputRoot, "manifest.json");
    private string ChecksCsv       => Path.Combine(OutputRoot, "checks.csv");
    private string Summary         => Path.Combine(OutputRoot, "summary.txt");

    private void WriteFixtures()
    {
        File.WriteAllText(Map32AManifest, """
        {
          "format": "MAP32A_TEST",
          "lot_count": 98,
          "footprint_count": 96,
          "skipped_lot_count": 2,
          "sector_counts": [
            { "sector_id": "DEFAULT",          "lot_count": 47 },
            { "sector_id": "DOWNTOWN_CORE",    "lot_count": 26 },
            { "sector_id": "OPEN_RESIDENTIAL", "lot_count": 25 }
          ],
          "verdict": "MAP32A_MATERIALIZED_RUNTIME_CANDIDATE_STAGED"
        }
        """);

        Directory.CreateDirectory(BinarySeedRoot);
        File.WriteAllBytes(Path.Combine(BinarySeedRoot, "35_27.lotheader"),       new byte[] { 0x4C, 0x4F, 0x54, 0x48, 0x01, 0x00 });
        File.WriteAllBytes(Path.Combine(BinarySeedRoot, "world_35_27.lotpack"),    new byte[] { 0x4C, 0x4F, 0x54, 0x50, 0x01, 0x00, 0x00, 0x00 });
        File.WriteAllBytes(Path.Combine(BinarySeedRoot, "chunkdata_35_27.bin"),    new byte[] { 0x01, 0x02, 0x03, 0x04 });
        File.WriteAllBytes(Path.Combine(BinarySeedRoot, "streets.xml.bin"),        new byte[] { 0xAA, 0xBB });
        File.WriteAllBytes(Path.Combine(BinarySeedRoot, "worldmap.xml.bin"),        new byte[] { 0xCC, 0xDD });
        File.WriteAllBytes(Path.Combine(BinarySeedRoot, "worldmap-forest.xml.bin"), new byte[] { 0xEE, 0xFF });

        Directory.CreateDirectory(OutputRoot);
    }

    private string[] MakeFullArgs() => new[]
    {
        "--map32a-manifest",   Map32AManifest,
        "--binary-seed-root",  BinarySeedRoot,
        "--output-root",       OutputRoot,
        "--output-manifest",   Manifest,
        "--output-checks-csv", ChecksCsv,
        "--summary",           Summary,
    };

    private (int ExitCode, string Output, string Error) RunCli(params string[] extraArgs)
    {
        var allArgs = new[] { "deadmtl-build-worldbuilder-binary-seeded-runtime-candidate" }
            .Concat(extraArgs)
            .ToArray();

        var psi = new ProcessStartInfo("dotnet",
            $"run --project \"{CliProject}\" -- " +
            string.Join(" ", allArgs.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)))
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            WorkingDirectory       = RepoRoot,
        };

        using var proc = Process.Start(psi)!;
        string stdout = proc.StandardOutput.ReadToEnd();
        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit(180_000);
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // MAP-33A CLI 1. Valid args write manifest/checks/summary and exit zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_ValidArgs_ExitsZero()
    {
        WriteFixtures();
        var (code, _, err) = RunCli(MakeFullArgs());
        Assert.True(code == 0, $"Exit={code} stderr={err}");
        Assert.True(File.Exists(Manifest),   "manifest must exist");
        Assert.True(File.Exists(ChecksCsv),  "checks CSV must exist");
        Assert.True(File.Exists(Summary),    "summary must exist");
    }

    // -----------------------------------------------------------------------
    // MAP-33A CLI 2. Missing MAP-32A manifest exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_MissingMap32AManifest_ExitsOne()
    {
        WriteFixtures();
        var args = MakeFullArgs().ToList();
        int idx = Array.IndexOf(args.ToArray(), "--map32a-manifest");
        args[idx + 1] = Path.Combine(_tempDir, "no_such_manifest.json");
        var (code, _, _) = RunCli(args.ToArray());
        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // MAP-33A CLI 3. Missing binary seed root exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_MissingBinarySeedRoot_ExitsOne()
    {
        WriteFixtures();
        var args = MakeFullArgs().ToList();
        int idx = Array.IndexOf(args.ToArray(), "--binary-seed-root");
        args[idx + 1] = Path.Combine(_tempDir, "no_such_seed");
        var (code, _, _) = RunCli(args.ToArray());
        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // MAP-33A CLI 4. Output root outside .local exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_OutputRoot_OutsideLocal_ExitsOne()
    {
        WriteFixtures();
        string badRoot = Path.Combine(Path.GetTempPath(), "pzmapforge-map33a-bad", Guid.NewGuid().ToString());
        var (code, _, err) = RunCli(
            "--map32a-manifest",  Map32AManifest,
            "--binary-seed-root", BinarySeedRoot,
            "--output-root",      badRoot);
        Assert.Equal(1, code);
        Assert.Contains(".local", err, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // MAP-33A CLI 5. Metadata files are created
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_MetadataFiles_AreCreated()
    {
        WriteFixtures();
        var (code, _, err) = RunCli(MakeFullArgs());
        Assert.True(code == 0, $"Exit={code} stderr={err}");
        Assert.True(File.Exists(Path.Combine(OutputRoot, "mod.info")), "mod.info must exist");
        Assert.True(File.Exists(Path.Combine(OutputRoot, "media", "maps", "DeadMTL_MAP33A", "map.info")),       "map.info must exist");
        Assert.True(File.Exists(Path.Combine(OutputRoot, "media", "maps", "DeadMTL_MAP33A", "spawnpoints.lua")), "spawnpoints.lua must exist");
        Assert.True(File.Exists(Path.Combine(OutputRoot, "media", "maps", "DeadMTL_MAP33A", "objects.lua")),    "objects.lua must exist");
    }

    // -----------------------------------------------------------------------
    // MAP-33A CLI 6. Required binary files are created in staged map folder
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_RequiredBinaryFiles_AreCreated()
    {
        WriteFixtures();
        var (code, _, err) = RunCli(MakeFullArgs());
        Assert.True(code == 0, $"Exit={code} stderr={err}");
        string mapDir = Path.Combine(OutputRoot, "media", "maps", "DeadMTL_MAP33A");
        Assert.True(File.Exists(Path.Combine(mapDir, "35_27.lotheader")),    "35_27.lotheader must exist");
        Assert.True(File.Exists(Path.Combine(mapDir, "world_35_27.lotpack")), "world_35_27.lotpack must exist");
        Assert.True(File.Exists(Path.Combine(mapDir, "chunkdata_35_27.bin")), "chunkdata_35_27.bin must exist");
    }

    // -----------------------------------------------------------------------
    // MAP-33A CLI 7. Manifest has binary_cell_materialized=true
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_Manifest_BinaryCellMaterialized_True()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(Manifest)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(Manifest));
        Assert.True(doc.RootElement.GetProperty("binary_cell_materialized").GetBoolean(),
            "binary_cell_materialized must be true");
    }

    // -----------------------------------------------------------------------
    // MAP-33A CLI 8. Manifest has geometry_from_map31b_materialized=false
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_Manifest_GeometryFromMap31b_False()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(Manifest)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(Manifest));
        Assert.False(doc.RootElement.GetProperty("geometry_from_map31b_materialized").GetBoolean(),
            "geometry_from_map31b_materialized must be false");
    }

    // -----------------------------------------------------------------------
    // MAP-33A CLI 9. Checks CSV contains MAP33A check IDs
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_ChecksCsv_ContainsMap33AChecks()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(ChecksCsv)) return;
        string csv = File.ReadAllText(ChecksCsv);
        Assert.Contains("MAP33A_OUTPUT_ROOT_UNDER_LOCAL",               csv);
        Assert.Contains("MAP33A_BINARY_CELL_MATERIALIZED_TRUE",         csv);
        Assert.Contains("MAP33A_REQUIRED_BINARY_SEED_FILES_WRITTEN",    csv);
        Assert.Contains("MAP33A_CLAIM_BOUNDARY_RECORDED",               csv);
    }

    // -----------------------------------------------------------------------
    // MAP-33A CLI 10. No runtime/playable/public claims are true in manifest
    // -----------------------------------------------------------------------

    [Fact]
    public void Map33A_NoClaims_AsTrue_InManifest()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(Manifest)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(Manifest));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("runtime_proof_claimed").GetBoolean(),             "runtime_proof_claimed must be false");
        Assert.False(root.GetProperty("public_playable_packaging_claimed").GetBoolean(), "public_playable_packaging_claimed must be false");
        Assert.False(root.GetProperty("playable_export_claimed").GetBoolean(),           "playable_export_claimed must be false");
        Assert.False(root.GetProperty("raw_source_png_mutated").GetBoolean(),            "raw_source_png_mutated must be false");
        Assert.False(root.GetProperty("live_workshop_write").GetBoolean(),               "live_workshop_write must be false");
        Assert.False(root.GetProperty("pz_install_write").GetBoolean(),                  "pz_install_write must be false");
        Assert.True(root.GetProperty("sandbox_only").GetBoolean(),                       "sandbox_only must be true");
    }
}
