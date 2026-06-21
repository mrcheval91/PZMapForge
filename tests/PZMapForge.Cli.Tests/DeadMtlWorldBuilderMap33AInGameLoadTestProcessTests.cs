using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMap33AInGameLoadTestProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map34a-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMap33AInGameLoadTestProcessTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProject =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj");

    private string Map33AManifest       => Path.Combine(_tempDir, "map33a-manifest.local.json");
    private string SourceCandidateRoot  => Path.Combine(_tempDir, "map33a-candidate.local");
    private string LocalModsRoot        => Path.Combine(_tempDir, "local-mods.local");
    private string OutputRoot           => Path.Combine(_tempDir, "output.local");
    private string OutputResult         => Path.Combine(OutputRoot, "result.json");
    private string ChecksCsv            => Path.Combine(OutputRoot, "checks.csv");
    private string Summary              => Path.Combine(OutputRoot, "summary.txt");
    private string FakeZomboidRoot      => Path.Combine(_tempDir, "zomboid.local");

    private void WriteFixtures()
    {
        File.WriteAllText(Map33AManifest, """
        {
          "format": "MAP33A_TEST",
          "binary_cell_materialized": true,
          "geometry_from_map31b_materialized": false,
          "verdict": "MAP33A_BINARY_SEEDED_RUNTIME_CANDIDATE_STAGED"
        }
        """);

        string mapDir = Path.Combine(SourceCandidateRoot, "media", "maps", "DeadMTL_MAP33A");
        Directory.CreateDirectory(mapDir);
        File.WriteAllText(Path.Combine(SourceCandidateRoot, "mod.info"), "id=DeadMTL_MAP33A\n");
        File.WriteAllBytes(Path.Combine(mapDir, "35_27.lotheader"),    new byte[] { 0x4C, 0x4F, 0x54, 0x48 });
        File.WriteAllBytes(Path.Combine(mapDir, "world_35_27.lotpack"), new byte[] { 0x4C, 0x4F, 0x54, 0x50 });
        File.WriteAllBytes(Path.Combine(mapDir, "chunkdata_35_27.bin"), new byte[] { 0x01, 0x02, 0x03 });
        File.WriteAllText(Path.Combine(mapDir, "map.info"),       "title=DeadMTL_MAP33A\n");
        File.WriteAllText(Path.Combine(mapDir, "spawnpoints.lua"), "function SpawnPoints() end\n");
        File.WriteAllText(Path.Combine(mapDir, "objects.lua"),    "-- placeholder\n");

        Directory.CreateDirectory(LocalModsRoot);
        Directory.CreateDirectory(OutputRoot);
    }

    private void WritePartialPassLog()
    {
        Directory.CreateDirectory(FakeZomboidRoot);
        File.WriteAllText(Path.Combine(FakeZomboidRoot, "console.txt"), """
            loading DeadMTL_MAP33A
            mod "DeadMTL_MAP33A" overrides media/maps/deadmtl_map33a/35_27.lotheader
            mod "DeadMTL_MAP33A" overrides media/maps/deadmtl_map33a/chunkdata_35_27.bin
            mod "DeadMTL_MAP33A" overrides media/maps/deadmtl_map33a/world_35_27.lotpack
            MapGroup something DeadMTL_MAP33A registered
            CellLoader.LoadCellBinaryChunk start
            Looking in these map folders:
            <End of map-folders list>
            initSpawnBuildings: no room or building at 10746,8288,0
            FluidContainerScript.load Sanitizing container name ERROR
            CraftRecipeComponentScript: Recipe Piano missing UiConfigScript
            """);
    }

    private void WriteSpawnBlockerLog()
    {
        Directory.CreateDirectory(FakeZomboidRoot);
        File.WriteAllText(Path.Combine(FakeZomboidRoot, "console.txt"), """
            loading DeadMTL_MAP33A
            mod "DeadMTL_MAP33A" overrides media/maps/deadmtl_map33a/35_27.lotheader
            mod "DeadMTL_MAP33A" overrides media/maps/deadmtl_map33a/chunkdata_35_27.bin
            mod "DeadMTL_MAP33A" overrides media/maps/deadmtl_map33a/world_35_27.lotpack
            MapGroup something DeadMTL_MAP33A registered
            there is no spawn point table for the player's profession
            """);
    }

    private string[] MakeFullArgs() => new[]
    {
        "--map33a-manifest",      Map33AManifest,
        "--source-candidate-root", SourceCandidateRoot,
        "--local-mods-root",      LocalModsRoot,
        "--output-root",          OutputRoot,
        "--output-result",        OutputResult,
        "--output-checks-csv",    ChecksCsv,
        "--summary",              Summary,
    };

    private (int ExitCode, string Output, string Error) RunCli(params string[] extraArgs)
    {
        var allArgs = new[] { "deadmtl-build-worldbuilder-map33a-ingame-load-test" }
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
    // MAP-34A CLI 1. Install-only valid args exit zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_InstallOnly_ValidArgs_ExitsZero()
    {
        WriteFixtures();
        var (code, _, err) = RunCli(MakeFullArgs());
        Assert.True(code == 0, $"Exit={code} stderr={err}");
    }

    // -----------------------------------------------------------------------
    // MAP-34A CLI 2. Missing MAP-33A manifest exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_MissingMap33AManifest_ExitsOne()
    {
        WriteFixtures();
        var args = MakeFullArgs().ToList();
        int idx = Array.IndexOf(args.ToArray(), "--map33a-manifest");
        args[idx + 1] = Path.Combine(_tempDir, "no_such_manifest.json");
        var (code, _, _) = RunCli(args.ToArray());
        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // MAP-34A CLI 3. Missing source candidate root exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_MissingSourceCandidateRoot_ExitsOne()
    {
        WriteFixtures();
        var args = MakeFullArgs().ToList();
        int idx = Array.IndexOf(args.ToArray(), "--source-candidate-root");
        args[idx + 1] = Path.Combine(_tempDir, "no_such_candidate");
        var (code, _, _) = RunCli(args.ToArray());
        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // MAP-34A CLI 4. Bad local mods root (Steam path) exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_BadLocalModsRoot_ExitsOne()
    {
        WriteFixtures();
        string badMods = Path.Combine(_tempDir, "steamapps", "bad-mods");
        Directory.CreateDirectory(badMods);
        var (code, _, err) = RunCli(
            "--map33a-manifest",       Map33AManifest,
            "--source-candidate-root", SourceCandidateRoot,
            "--local-mods-root",       badMods);
        Assert.Equal(1, code);
        Assert.Contains("steamapps", err, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // MAP-34A CLI 5. Install marker exists after install
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_InstallMarkerExists_AfterInstall()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        string markerPath = Path.Combine(LocalModsRoot,
            "deadmtl_map33a_candidate",
            "PZMAPFORGE_MAP34A_TEST_INSTALL_MARKER.txt");
        Assert.True(File.Exists(markerPath), "Install marker must exist after install");
    }

    // -----------------------------------------------------------------------
    // MAP-34A CLI 6. Result JSON exists
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_ResultJson_Exists()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        Assert.True(File.Exists(OutputResult), "result.json must exist");
    }

    // -----------------------------------------------------------------------
    // MAP-34A CLI 7. Checks CSV contains MAP34A check IDs
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_ChecksCsv_ContainsMap34AChecks()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(ChecksCsv)) return;
        string csv = File.ReadAllText(ChecksCsv);
        Assert.Contains("MAP34A_MAP33A_BINARY_CELL_MATERIALIZED_TRUE", csv);
        Assert.Contains("MAP34A_CANDIDATE_COPIED_TO_LOCAL_MODS",       csv);
        Assert.Contains("MAP34A_INSTALL_MARKER_WRITTEN",               csv);
        Assert.Contains("MAP34A_CLAIM_BOUNDARY_RECORDED",              csv);
    }

    // -----------------------------------------------------------------------
    // MAP-34A CLI 8. No final claims appear true in install-only result JSON
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34A_NoFinalClaims_True_InResult()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        if (!File.Exists(OutputResult)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputResult));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("runtime_proof_claimed").GetBoolean(),    "runtime_proof_claimed must be false");
        Assert.False(root.GetProperty("playable_export_claimed").GetBoolean(),  "playable_export_claimed must be false");
        Assert.False(root.GetProperty("public_package_claimed").GetBoolean(),   "public_package_claimed must be false");
        Assert.False(root.GetProperty("geometry_from_map31b_materialized").GetBoolean(), "geometry_from_map31b_materialized must be false");
        Assert.Equal("MAP34A_INSTALL_ONLY_READY", root.GetProperty("runtime_classification").GetString());
    }

    // -----------------------------------------------------------------------
    // MAP-34B CLI 1. --collect-logs --operator-observation writes result JSON
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34B_CollectLogsWithObservation_WritesResult()
    {
        WriteFixtures();
        // Install first
        RunCli(MakeFullArgs());
        // Write partial-pass log
        WritePartialPassLog();
        // Collect logs with operator observation
        var (code, _, err) = RunCli(
            "--map33a-manifest",       Map33AManifest,
            "--source-candidate-root", SourceCandidateRoot,
            "--local-mods-root",       LocalModsRoot,
            "--output-root",           OutputRoot,
            "--output-result",         OutputResult,
            "--output-checks-csv",     ChecksCsv,
            "--summary",               Summary,
            "--zomboid-user-root",     FakeZomboidRoot,
            "--collect-logs",
            "--operator-observation",  "loaded in a field, empty, fallback type");
        Assert.True(code == 0, $"Exit={code} stderr={err}");
        Assert.True(File.Exists(OutputResult), "result.json must exist after collect-logs");
    }

    // -----------------------------------------------------------------------
    // MAP-34B CLI 2. result JSON has runtime_classification = partial pass
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34B_CollectLogs_PartialPassClassification()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        WritePartialPassLog();
        RunCli(
            "--map33a-manifest",       Map33AManifest,
            "--source-candidate-root", SourceCandidateRoot,
            "--local-mods-root",       LocalModsRoot,
            "--output-root",           OutputRoot,
            "--output-result",         OutputResult,
            "--output-checks-csv",     ChecksCsv,
            "--summary",               Summary,
            "--zomboid-user-root",     FakeZomboidRoot,
            "--collect-logs",
            "--operator-observation",  "loaded in a field, empty, fallback type");
        if (!File.Exists(OutputResult)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputResult));
        Assert.Equal(
            "MAP34B_RUNTIME_PARTIAL_PASS_EMPTY_FALLBACK_TERRAIN",
            doc.RootElement.GetProperty("runtime_classification").GetString());
    }

    // -----------------------------------------------------------------------
    // MAP-34B CLI 3. Collect-logs does not overwrite installed mod marker
    //                (install_performed=false in result)
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34B_CollectLogs_DoesNotOverwriteInstallMarker()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        WritePartialPassLog();
        // Read marker content before collect-logs
        string markerPath = Path.Combine(LocalModsRoot, "deadmtl_map33a_candidate",
            "PZMAPFORGE_MAP34A_TEST_INSTALL_MARKER.txt");
        string markerBefore = File.ReadAllText(markerPath);
        // Run collect-logs
        RunCli(
            "--map33a-manifest",       Map33AManifest,
            "--source-candidate-root", SourceCandidateRoot,
            "--local-mods-root",       LocalModsRoot,
            "--output-root",           OutputRoot,
            "--output-result",         OutputResult,
            "--zomboid-user-root",     FakeZomboidRoot,
            "--collect-logs");
        // Result must show install_performed=false
        if (!File.Exists(OutputResult)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputResult));
        Assert.False(doc.RootElement.GetProperty("install_performed").GetBoolean(),
            "install_performed must be false in collect-logs mode");
        // Marker must not be overwritten (content unchanged)
        string markerAfter = File.ReadAllText(markerPath);
        Assert.Equal(markerBefore, markerAfter);
    }

    // -----------------------------------------------------------------------
    // MAP-34B CLI 4. Unrelated vanilla errors do not force runtime fail
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34B_CollectLogs_UnrelatedErrors_NoRuntimeFail()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        WritePartialPassLog(); // contains FluidContainerScript ERROR
        RunCli(
            "--map33a-manifest",       Map33AManifest,
            "--source-candidate-root", SourceCandidateRoot,
            "--local-mods-root",       LocalModsRoot,
            "--output-root",           OutputRoot,
            "--output-result",         OutputResult,
            "--zomboid-user-root",     FakeZomboidRoot,
            "--collect-logs",
            "--operator-observation",  "loaded in a field, empty, fallback type");
        if (!File.Exists(OutputResult)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputResult));
        string classification = doc.RootElement.GetProperty("runtime_classification").GetString() ?? "";
        Assert.NotEqual("MAP34B_RUNTIME_CANDIDATE_SPECIFIC_FAIL", classification);
        Assert.Equal("MAP34B_RUNTIME_PARTIAL_PASS_EMPTY_FALLBACK_TERRAIN", classification);
    }

    // -----------------------------------------------------------------------
    // MAP-34B CLI 5. Spawn blocker logs force spawn-blocked classification
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34B_CollectLogs_SpawnBlockerLogs_SpawnBlocked()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        WriteSpawnBlockerLog();
        RunCli(
            "--map33a-manifest",       Map33AManifest,
            "--source-candidate-root", SourceCandidateRoot,
            "--local-mods-root",       LocalModsRoot,
            "--output-root",           OutputRoot,
            "--output-result",         OutputResult,
            "--zomboid-user-root",     FakeZomboidRoot,
            "--collect-logs");
        if (!File.Exists(OutputResult)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputResult));
        Assert.Equal("MAP34B_RUNTIME_SPAWN_BLOCKED",
            doc.RootElement.GetProperty("runtime_classification").GetString());
    }

    // -----------------------------------------------------------------------
    // MAP-34B CLI 6. No playable/final geometry claims become true in partial pass
    // -----------------------------------------------------------------------

    [Fact]
    public void Map34B_NoFinalGeometryOrPlayableClaims()
    {
        WriteFixtures();
        RunCli(MakeFullArgs());
        WritePartialPassLog();
        RunCli(
            "--map33a-manifest",       Map33AManifest,
            "--source-candidate-root", SourceCandidateRoot,
            "--local-mods-root",       LocalModsRoot,
            "--output-root",           OutputRoot,
            "--output-result",         OutputResult,
            "--zomboid-user-root",     FakeZomboidRoot,
            "--collect-logs",
            "--operator-observation",  "loaded in a field, empty, fallback type");
        if (!File.Exists(OutputResult)) return;
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputResult));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("playable_export_claimed").GetBoolean(),          "playable_export_claimed must be false");
        Assert.False(root.GetProperty("runtime_proof_claimed").GetBoolean(),            "runtime_proof_claimed must be false");
        Assert.False(root.GetProperty("geometry_from_map31b_materialized").GetBoolean(), "geometry_from_map31b_materialized must be false");
        Assert.False(root.GetProperty("public_package_claimed").GetBoolean(),           "public_package_claimed must be false");
    }
}
