using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderVisibleCellRuntimeCandidateProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-map35a-cli-test.local", Path.GetRandomFileName());

    public DeadMtlWorldBuilderVisibleCellRuntimeCandidateProcessTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProject =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj");

    private string PrimarySourceRoot  => Path.Combine(_tempDir, "source-primary.local");
    private string FallbackSourceRoot => Path.Combine(_tempDir, "source-fallback.local");
    private string LocalModsRoot      => Path.Combine(_tempDir, "local-mods.local");
    private string OutputRoot         => Path.Combine(_tempDir, "output.local");
    private string OutputResult       => Path.Combine(OutputRoot, "result.json");
    private string ChecksCsv          => Path.Combine(OutputRoot, "checks.csv");
    private string Summary            => Path.Combine(OutputRoot, "summary.txt");
    private string FakeZomboidRoot    => Path.Combine(_tempDir, "zomboid.local");

    private void WriteSourceFiles(string root, int chunkdataSize = 18178)
    {
        Directory.CreateDirectory(root);
        File.WriteAllBytes(Path.Combine(root, "35_27.lotheader"),    new byte[63209]);
        File.WriteAllBytes(Path.Combine(root, "world_35_27.lotpack"), new byte[1056152]);
        File.WriteAllBytes(Path.Combine(root, "chunkdata_35_27.bin"), new byte[chunkdataSize]);
    }

    private void WriteFixtures()
    {
        WriteSourceFiles(PrimarySourceRoot);
        Directory.CreateDirectory(LocalModsRoot);
        Directory.CreateDirectory(OutputRoot);
    }

    private void WriteVisibleTerrainLog()
    {
        Directory.CreateDirectory(FakeZomboidRoot);
        File.WriteAllText(Path.Combine(FakeZomboidRoot, "console.txt"),
            "loading DeadMTL_MAP35A\n" +
            "mod \"DeadMTL_MAP35A\" overrides media/maps/deadmtl_map35a/35_27.lotheader\n" +
            "mod \"DeadMTL_MAP35A\" overrides media/maps/deadmtl_map35a/chunkdata_35_27.bin\n" +
            "mod \"DeadMTL_MAP35A\" overrides media/maps/deadmtl_map35a/world_35_27.lotpack\n" +
            "MapGroup something DeadMTL_MAP35A registered\n" +
            "CellLoader.LoadCellBinaryChunk start\n");
    }

    private string[] MakeBaseArgs() => new[]
    {
        "--primary-source-root",  PrimarySourceRoot,
        "--local-mods-root",      LocalModsRoot,
        "--output-root",          OutputRoot,
        "--output-result",        OutputResult,
        "--output-checks-csv",    ChecksCsv,
        "--summary",              Summary,
    };

    private (int ExitCode, string Output, string Error) RunCli(params string[] extraArgs)
    {
        var allArgs = new[] { "deadmtl-build-worldbuilder-visible-cell-runtime-candidate" }
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
    // MAP35A_CLI_1: Stage-only valid args exits zero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Cli1_StageOnly_ValidArgs_ExitsZero()
    {
        WriteFixtures();
        var (code, _, err) = RunCli(MakeBaseArgs());
        Assert.True(code == 0, $"Exit={code} stderr={err}");
    }

    // -----------------------------------------------------------------------
    // MAP35A_CLI_2: Missing primary source root exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Cli2_MissingPrimarySourceRoot_ExitsOne()
    {
        WriteFixtures();
        var (code, _, _) = RunCli(
            "--primary-source-root",  Path.Combine(_tempDir, "no_such_source.local"),
            "--local-mods-root",      LocalModsRoot,
            "--output-root",          OutputRoot);
        Assert.Equal(1, code);
    }

    // -----------------------------------------------------------------------
    // MAP35A_CLI_3: Bad local mods root (Steam path) exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Cli3_BadLocalModsRootSteam_ExitsOne()
    {
        WriteFixtures();
        string badMods = Path.Combine(_tempDir, "steamapps", "bad-mods.local");
        Directory.CreateDirectory(badMods);
        var (code, _, err) = RunCli(
            "--primary-source-root", PrimarySourceRoot,
            "--local-mods-root",     badMods,
            "--output-root",         OutputRoot);
        Assert.Equal(1, code);
        Assert.Contains("steamapps", err, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // MAP35A_CLI_4: Output-root must contain .local — exits nonzero otherwise
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Cli4_OutputRootMustContainDotLocal()
    {
        WriteFixtures();
        // Use GetTempPath() which yields e.g. C:\Users\...\AppData\Local\Temp\ — no ".local" substring
        string badOutput = Path.Combine(Path.GetTempPath(), "pzmapforge-map35a-bad-" + Path.GetRandomFileName());
        Directory.CreateDirectory(badOutput);
        try
        {
            var (code, _, err) = RunCli(
                "--primary-source-root", PrimarySourceRoot,
                "--local-mods-root",     LocalModsRoot,
                "--output-root",         badOutput);
            Assert.Equal(1, code);
            Assert.Contains(".local", err, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(badOutput, true); } catch { }
        }
    }

    // -----------------------------------------------------------------------
    // MAP35A_CLI_5: Install marker exists after --install-only
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Cli5_InstallMarkerExists_AfterInstallOnly()
    {
        WriteFixtures();
        RunCli(MakeBaseArgs().Concat(new[] { "--install-only" }).ToArray());
        string markerPath = Path.Combine(LocalModsRoot,
            "deadmtl_map35a_visible_cell_candidate",
            "PZMAPFORGE_MAP35A_TEST_INSTALL_MARKER.txt");
        Assert.True(File.Exists(markerPath), "Install marker must exist after --install-only");
    }

    // -----------------------------------------------------------------------
    // MAP35A_CLI_6: CollectLogs does not overwrite install marker
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Cli6_CollectLogs_DoesNotOverwriteInstallMarker()
    {
        WriteFixtures();
        // Install first
        RunCli(MakeBaseArgs().Concat(new[] { "--install-only" }).ToArray());
        string markerPath = Path.Combine(LocalModsRoot,
            "deadmtl_map35a_visible_cell_candidate",
            "PZMAPFORGE_MAP35A_TEST_INSTALL_MARKER.txt");
        string originalContent = File.ReadAllText(markerPath);

        WriteVisibleTerrainLog();

        // Collect logs
        RunCli(MakeBaseArgs()
            .Concat(new[]
            {
                "--collect-logs",
                "--zomboid-user-root",     FakeZomboidRoot,
                "--operator-observation",  "visible terrain",
            })
            .ToArray());

        Assert.Equal(originalContent, File.ReadAllText(markerPath));

        if (File.Exists(OutputResult))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(OutputResult));
            bool installPerformed = doc.RootElement.GetProperty("install_performed").GetBoolean();
            Assert.False(installPerformed);
        }
    }

    // -----------------------------------------------------------------------
    // MAP35A_CLI_7: Result JSON is written and parseable
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Cli7_ResultJsonWritten_AndParseable()
    {
        WriteFixtures();
        RunCli(MakeBaseArgs());
        Assert.True(File.Exists(OutputResult), "Result JSON must be written");
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputResult));
        Assert.Equal("MAP35B_VISIBLE_CELL_RUNTIME_CANDIDATE_V1", doc.RootElement.GetProperty("format").GetString());
        Assert.Equal("DeadMTL_MAP35A", doc.RootElement.GetProperty("map_id").GetString());
        Assert.True(doc.RootElement.GetProperty("b42_layout_written").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // MAP35A_CLI_8: Claim boundary fields are correct in result JSON
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35A_Cli8_ClaimBoundaryFieldsCorrect()
    {
        WriteFixtures();
        RunCli(MakeBaseArgs());
        Assert.True(File.Exists(OutputResult));
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputResult));
        var root = doc.RootElement;
        Assert.False(root.GetProperty("geometry_from_map31b_materialized").GetBoolean());
        Assert.False(root.GetProperty("runtime_proof_claimed").GetBoolean());
        Assert.False(root.GetProperty("playable_export_claimed").GetBoolean());
        Assert.False(root.GetProperty("public_package_claimed").GetBoolean());
        Assert.False(root.GetProperty("workshop_upload_performed").GetBoolean());
        Assert.False(root.GetProperty("steam_install_write").GetBoolean());
        Assert.True(root.GetProperty("local_user_mod_install_allowed").GetBoolean());
        Assert.True(root.GetProperty("duplicate_map_entries_possible").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // MAP35B_CLI_1: collect-log visible pass result JSON has runtime_visible_cell_proof_observed=true
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35B_Cli1_CollectLogs_VisiblePass_ProofObservedTrue()
    {
        WriteFixtures();
        RunCli(MakeBaseArgs().Concat(new[] { "--install-only" }).ToArray());
        WriteVisibleTerrainLog();

        RunCli(MakeBaseArgs()
            .Concat(new[]
            {
                "--collect-logs",
                "--zomboid-user-root",    FakeZomboidRoot,
                "--operator-observation", "visible terrain with textured ground, no roads or buildings observed",
            })
            .ToArray());

        Assert.True(File.Exists(OutputResult));
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputResult));
        Assert.True(doc.RootElement.GetProperty("runtime_visible_cell_proof_observed").GetBoolean());
        Assert.Equal("operator_observation_and_pz_logs",
            doc.RootElement.GetProperty("runtime_visible_cell_proof_source").GetString());
    }

    // -----------------------------------------------------------------------
    // MAP35B_CLI_2: collect-log visible pass result JSON has binary_cell_materialized=true
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35B_Cli2_CollectLogs_VisiblePass_BinaryCellMaterializedTrue()
    {
        WriteFixtures();
        RunCli(MakeBaseArgs().Concat(new[] { "--install-only" }).ToArray());
        WriteVisibleTerrainLog();

        RunCli(MakeBaseArgs()
            .Concat(new[]
            {
                "--collect-logs",
                "--zomboid-user-root",    FakeZomboidRoot,
                "--operator-observation", "visible terrain",
            })
            .ToArray());

        Assert.True(File.Exists(OutputResult));
        using var doc = JsonDocument.Parse(File.ReadAllText(OutputResult));
        Assert.True(doc.RootElement.GetProperty("binary_cell_materialized").GetBoolean());
    }

    // -----------------------------------------------------------------------
    // MAP35B_CLI_3: collect-log visible pass summary contains MAP35A_RUNTIME_VISIBLE_CELL_PASS
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35B_Cli3_CollectLogs_VisiblePass_SummaryContainsClassification()
    {
        WriteFixtures();
        RunCli(MakeBaseArgs().Concat(new[] { "--install-only" }).ToArray());
        WriteVisibleTerrainLog();

        var (_, stdout, _) = RunCli(MakeBaseArgs()
            .Concat(new[]
            {
                "--collect-logs",
                "--zomboid-user-root",    FakeZomboidRoot,
                "--operator-observation", "visible terrain",
                "--summary",              Summary,
            })
            .ToArray());

        Assert.True(File.Exists(Summary), "Summary file must be written");
        string summaryText = File.ReadAllText(Summary);
        Assert.Contains("MAP35A_RUNTIME_VISIBLE_CELL_PASS", summaryText, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // MAP35B_CLI_4: collect-log visible pass summary does not say terrain is not confirmed
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35B_Cli4_CollectLogs_VisiblePass_SummaryDoesNotSayNotConfirmed()
    {
        WriteFixtures();
        RunCli(MakeBaseArgs().Concat(new[] { "--install-only" }).ToArray());
        WriteVisibleTerrainLog();

        RunCli(MakeBaseArgs()
            .Concat(new[]
            {
                "--collect-logs",
                "--zomboid-user-root",    FakeZomboidRoot,
                "--operator-observation", "visible terrain",
                "--summary",              Summary,
            })
            .ToArray());

        Assert.True(File.Exists(Summary));
        string summaryText = File.ReadAllText(Summary);
        Assert.DoesNotContain("visible terrain not yet confirmed", summaryText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Visible-cell runtime proof observed : TRUE", summaryText, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // MAP35B_CLI_5: collect-log mode does not reinstall
    // -----------------------------------------------------------------------

    [Fact]
    public void Map35B_Cli5_CollectLogs_DoesNotReinstall()
    {
        WriteFixtures();
        RunCli(MakeBaseArgs().Concat(new[] { "--install-only" }).ToArray());

        string markerPath = Path.Combine(LocalModsRoot,
            "deadmtl_map35a_visible_cell_candidate",
            "PZMAPFORGE_MAP35A_TEST_INSTALL_MARKER.txt");
        string originalContent = File.ReadAllText(markerPath);

        WriteVisibleTerrainLog();
        RunCli(MakeBaseArgs()
            .Concat(new[]
            {
                "--collect-logs",
                "--zomboid-user-root",    FakeZomboidRoot,
                "--operator-observation", "visible terrain",
            })
            .ToArray());

        Assert.Equal(originalContent, File.ReadAllText(markerPath));
        if (File.Exists(OutputResult))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(OutputResult));
            Assert.False(doc.RootElement.GetProperty("install_performed").GetBoolean());
            Assert.True(doc.RootElement.GetProperty("collect_logs_mode_does_not_stage_or_install").GetBoolean());
        }
    }
}
