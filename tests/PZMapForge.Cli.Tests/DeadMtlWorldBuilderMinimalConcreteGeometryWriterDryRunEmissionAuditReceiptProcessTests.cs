using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmissionAuditReceiptProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-dry-run-emission-audit-cli", Path.GetRandomFileName());

    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterDryRunEmissionAuditReceiptProcessTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProject =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj");

    private static string HelperScript =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "scripts",
            "run-deadmtl-worldbuilder-minimal-concrete-geometry-writer-dry-run-emission-audit-receipt.ps1");

    private static string DocPath =>
        Path.Combine(RepoRoot, "docs", "authoring",
            "DEADMTL_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMISSION_AUDIT_RECEIPT.md");

    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private static string Sha256OfFile(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLower();

    private static string WriteJson(string path, object obj)
    {
        var opts = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(path, JsonSerializer.Serialize(obj, opts));
        return path;
    }

    private string WriteEmitterFixture()
    {
        var outputRoot = Path.Combine(_tempDir, ".local", "emitter-output");
        Directory.CreateDirectory(outputRoot);

        string componentPath = Path.Combine(outputRoot, "map_00.component_writer_record.json");
        WriteJson(componentPath, new { record_kind = "COMPONENT_WRITER_RECORD", map_id = "map_00", target_component_id = "map_00_component_0001", intent = "RESIDENTIAL_LOT_BLOCK", access_readiness_class = "DUAL_ACCESS_CANDIDATE", component_bbox = new { min_x = 124, min_y = 10, max_x = 212, max_y = 69, width_px = 89, height_px = 60 }, source_geometry_kind = "COMPONENT_BBOX_GEOMETRY", dry_run_only = true, runtime_valid = false, writer_ready = false, materialized = false });
        string lotPath = Path.Combine(outputRoot, "map_00.lot_writer_records.json");
        WriteJson(lotPath, new { record_kind = "LOT_WRITER_RECORDS", map_id = "map_00", target_component_id = "map_00_component_0001", lot_count = 7, lots = new object[7], dry_run_only = true });
        string slotPath = Path.Combine(outputRoot, "map_00.building_slot_writer_records.json");
        WriteJson(slotPath, new { record_kind = "BUILDING_SLOT_WRITER_RECORDS", map_id = "map_00", target_component_id = "map_00_component_0001", building_slot_count = 7, building_slots = new object[7], dry_run_only = true });
        string frontagePath = Path.Combine(outputRoot, "map_00.frontage_access_record.json");
        WriteJson(frontagePath, new { record_kind = "FRONTAGE_ACCESS_RECORD", frontage_side = "NORTH", frontage_component_id = "map_00_component_0023", frontage_contact_px = 89, dry_run_only = true });
        string rearPath = Path.Combine(outputRoot, "map_00.rear_service_access_record.json");
        WriteJson(rearPath, new { record_kind = "REAR_SERVICE_ACCESS_RECORD", rear_service_side = "EAST", rear_service_component_id = "map_00_component_0030", rear_service_contact_px = 60, dry_run_only = true });
        string scanPath = Path.Combine(outputRoot, "map_00.forbidden_output_scan.json");
        WriteJson(scanPath, new { record_kind = "FORBIDDEN_OUTPUT_SCAN_RECORD", lotpack_found = false, lotheader_found = false, worldgen_override_lua_found = false, pz_install_path_found = false, scan_passed = true, details = "Scan passed: no .lotpack, .lotheader, WorldGenOverride.lua, or PZ install paths found." });
        string rollbackPath = Path.Combine(outputRoot, "map_00.rollback_record.json");
        WriteJson(rollbackPath, new { record_kind = "ROLLBACK_RECORD", dot_local_outputs_only = true, source_hashes_unchanged = true, runtime_files_created = false, pz_install_mutated = false, dry_run_only = true });
        string claimBoundaryPath = Path.Combine(outputRoot, "map_00.claim_boundary_record.json");
        WriteJson(claimBoundaryPath, new { record_kind = "CLAIM_BOUNDARY_RECORD", writer_ready = false, runtime_valid = false, materialized = false, approved_for_writer_experiment = false, writer_experiment_gate_status = "LOCKED_PENDING_OPERATOR_APPROVAL", runtime_proof_claimed = false, writer_ready_claimed = false, public_playable_packaging_claimed = false });

        var emittedRecords = new object[]
        {
            new { record_order = 1, record_id = "COMPONENT_WRITER_RECORD",      record_kind = "COMPONENT_RECORD",      path = componentPath,     sha256 = Sha256OfFile(componentPath),     size_bytes = new FileInfo(componentPath).Length,     status = "DRY_RUN_RECORD_EMITTED" },
            new { record_order = 2, record_id = "LOT_WRITER_RECORDS",           record_kind = "LOT_RECORDS",           path = lotPath,           sha256 = Sha256OfFile(lotPath),           size_bytes = new FileInfo(lotPath).Length,           status = "DRY_RUN_RECORD_EMITTED" },
            new { record_order = 3, record_id = "BUILDING_SLOT_WRITER_RECORDS", record_kind = "BUILDING_SLOT_RECORDS", path = slotPath,          sha256 = Sha256OfFile(slotPath),          size_bytes = new FileInfo(slotPath).Length,          status = "DRY_RUN_RECORD_EMITTED" },
            new { record_order = 4, record_id = "FRONTAGE_ACCESS_RECORD",       record_kind = "ACCESS_RECORD",         path = frontagePath,      sha256 = Sha256OfFile(frontagePath),      size_bytes = new FileInfo(frontagePath).Length,      status = "DRY_RUN_RECORD_EMITTED" },
            new { record_order = 5, record_id = "REAR_SERVICE_ACCESS_RECORD",   record_kind = "ACCESS_RECORD",         path = rearPath,          sha256 = Sha256OfFile(rearPath),          size_bytes = new FileInfo(rearPath).Length,          status = "DRY_RUN_RECORD_EMITTED" },
            new { record_order = 6, record_id = "FORBIDDEN_OUTPUT_SCAN_RECORD", record_kind = "SCAN_RECORD",           path = scanPath,          sha256 = Sha256OfFile(scanPath),          size_bytes = new FileInfo(scanPath).Length,          status = "DRY_RUN_RECORD_EMITTED" },
            new { record_order = 7, record_id = "ROLLBACK_RECORD",              record_kind = "ROLLBACK_RECORD",       path = rollbackPath,      sha256 = Sha256OfFile(rollbackPath),      size_bytes = new FileInfo(rollbackPath).Length,      status = "DRY_RUN_RECORD_EMITTED" },
            new { record_order = 8, record_id = "CLAIM_BOUNDARY_RECORD",        record_kind = "CLAIM_BOUNDARY_RECORD", path = claimBoundaryPath, sha256 = Sha256OfFile(claimBoundaryPath), size_bytes = new FileInfo(claimBoundaryPath).Length, status = "DRY_RUN_RECORD_EMITTED" },
        };

        string emitterBase = "map_00.minimal_concrete_geometry_writer_dry_run_emitter";
        string emitterJsonPath = Path.Combine(outputRoot, $"{emitterBase}.json");
        WriteJson(emitterJsonPath, new
        {
            format = "pzmapforge.deadmtl.worldbuilder.minimal-concrete-geometry-writer-dry-run-emitter.v1",
            generated_utc = "2026-06-17T00:00:00.0000000Z",
            map_id = "map_00", target_component_id = "map_00_component_0001",
            emitter_status = "DRY_RUN_RECORDS_EMITTED_TO_DOT_LOCAL_ONLY",
            dry_run_only = true, writer_ready = false, runtime_valid = false, materialized = false,
            approved_for_writer_experiment = false,
            writer_experiment_gate_status = "LOCKED_PENDING_OPERATOR_APPROVAL",
            output_root = outputRoot, emitted_record_count = 8, emitted_records = emittedRecords,
            forbidden_output_scan = new { scan_passed = true, details = "Scan passed: no .lotpack, .lotheader, WorldGenOverride.lua, or PZ install paths found." },
            rollback_record = new { record_kind = "ROLLBACK_RECORD", dot_local_outputs_only = true, source_hashes_unchanged = true, runtime_files_created = false, pz_install_mutated = false, dry_run_only = true },
            claim_boundary = new { record_kind = "CLAIM_BOUNDARY_RECORD", writer_ready = false, runtime_valid = false, materialized = false, approved_for_writer_experiment = false, writer_experiment_gate_status = "LOCKED_PENDING_OPERATOR_APPROVAL", runtime_proof_claimed = false, writer_ready_claimed = false, public_playable_packaging_claimed = false },
            check_count = 25, passed_check_count = 25, failed_check_count = 0,
            checks = Array.Empty<object>(),
            is_valid = true,
            verdict = "MAP26G_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMITTER_COMPLETE",
            errors = Array.Empty<string>(),
        });
        File.WriteAllText(Path.Combine(outputRoot, $"{emitterBase}.md"),          "# MAP-26G");
        File.WriteAllText(Path.Combine(outputRoot, $"{emitterBase}.csv"),         "check_order,check_id,check_status");
        File.WriteAllText(Path.Combine(outputRoot, $"{emitterBase}.summary.txt"), "verdict : MAP26G_COMPLETE");

        return emitterJsonPath;
    }

    private (string OutputRoot, string Json, string Md, string Csv, string Summary) MakeOutputPaths(string subdir = "default")
    {
        var root = Path.Combine(_tempDir, ".local", subdir, "audit-receipt");
        Directory.CreateDirectory(root);
        return (
            root,
            Path.Combine(root, "receipt.json"),
            Path.Combine(root, "receipt.md"),
            Path.Combine(root, "receipt.csv"),
            Path.Combine(root, "receipt.summary.txt")
        );
    }

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
        psi.ArgumentList.Add("--");
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc     = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        Task.WaitAll(stdoutTask, stderrTask);
        proc.WaitForExit();
        return (proc.ExitCode, stdoutTask.Result, stderrTask.Result);
    }

    private const string Cmd = "deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-dry-run-emission-audit-receipt";

    private (int ExitCode, string Stdout, string Stderr) RunBuild(string subdir = "default")
    {
        var emitterPath = WriteEmitterFixture();
        var emitterOutputRoot = Path.GetDirectoryName(emitterPath)!;
        var (root, j, m, c, s) = MakeOutputPaths(subdir);
        return RunCli(Cmd,
            "--emitter-result",      emitterPath,
            "--emitter-output-root", emitterOutputRoot,
            "--output-root",         root,
            "--output-json",         j,
            "--output-md",           m,
            "--output-csv",          c,
            "--summary",             s);
    }

    // -----------------------------------------------------------------------
    // Exit code tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_ExitsZero_WithValidInputs()
    {
        var (exitCode, _, stderr) = RunBuild();
        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("ERROR", stderr);
    }

    [Fact]
    public void Cli_ExitsOne_WhenMissingArgs()
    {
        var (exitCode, _, _) = RunCli(Cmd);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void Cli_ExitsOne_WhenMissingEmitterResult()
    {
        var emitterOutputRoot = Path.Combine(_tempDir, ".local", "emitter-output");
        Directory.CreateDirectory(emitterOutputRoot);
        var (root, j, m, c, s) = MakeOutputPaths("missing-result");
        var (exitCode, _, _) = RunCli(Cmd,
            "--emitter-result",      Path.Combine(_tempDir, ".local", "nonexistent.json"),
            "--emitter-output-root", emitterOutputRoot,
            "--output-root",         root,
            "--output-json",         j,
            "--output-md",           m,
            "--output-csv",          c,
            "--summary",             s);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void Cli_ExitsOne_WhenMissingOutputRoot()
    {
        var emitterPath = WriteEmitterFixture();
        var emitterOutputRoot = Path.GetDirectoryName(emitterPath)!;
        var (root, j, m, c, s) = MakeOutputPaths("missing-output-root");
        var (exitCode, _, _) = RunCli(Cmd,
            "--emitter-result",      emitterPath,
            "--emitter-output-root", Path.Combine(_tempDir, ".local", "nonexistent-dir"),
            "--output-root",         root,
            "--output-json",         j,
            "--output-md",           m,
            "--output-csv",          c,
            "--summary",             s);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void Cli_ExitsOne_WhenEmitterOutputRootNotUnderLocal()
    {
        var emitterPath = WriteEmitterFixture();
        var emitterOutputRoot = Path.GetDirectoryName(emitterPath)!;
        var (root, j, m, c, s) = MakeOutputPaths("not-local-root");
        // Use a path that doesn't contain ".local"
        var badRoot = Path.Combine(_tempDir, "not-local-dir");
        Directory.CreateDirectory(badRoot);
        var (exitCode, _, stderr) = RunCli(Cmd,
            "--emitter-result",      emitterPath,
            "--emitter-output-root", badRoot,
            "--output-root",         root,
            "--output-json",         j,
            "--output-md",           m,
            "--output-csv",          c,
            "--summary",             s);
        Assert.Equal(1, exitCode);
        Assert.Contains(".local", stderr);
    }

    [Fact]
    public void Cli_ExitsOne_WhenOutputFilesNotUnderLocal()
    {
        var emitterPath = WriteEmitterFixture();
        var emitterOutputRoot = Path.GetDirectoryName(emitterPath)!;
        var badRoot = Path.Combine(_tempDir, "no-local-here");
        Directory.CreateDirectory(badRoot);
        var badJson = Path.Combine(badRoot, "receipt.json");
        var (exitCode, _, stderr) = RunCli(Cmd,
            "--emitter-result",      emitterPath,
            "--emitter-output-root", emitterOutputRoot,
            "--output-root",         badRoot,
            "--output-json",         badJson,
            "--output-md",           Path.Combine(_tempDir, ".local", "x.md"),
            "--output-csv",          Path.Combine(_tempDir, ".local", "x.csv"),
            "--summary",             Path.Combine(_tempDir, ".local", "x.txt"));
        Assert.Equal(1, exitCode);
        Assert.Contains(".local", stderr);
    }

    // -----------------------------------------------------------------------
    // Output tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_Creates4Outputs()
    {
        var emitterPath = WriteEmitterFixture();
        var emitterOutputRoot = Path.GetDirectoryName(emitterPath)!;
        var (root, j, m, c, s) = MakeOutputPaths("creates4");
        RunCli(Cmd,
            "--emitter-result",      emitterPath,
            "--emitter-output-root", emitterOutputRoot,
            "--output-root",         root,
            "--output-json",         j,
            "--output-md",           m,
            "--output-csv",          c,
            "--summary",             s);
        Assert.True(File.Exists(j));
        Assert.True(File.Exists(m));
        Assert.True(File.Exists(c));
        Assert.True(File.Exists(s));
    }

    // -----------------------------------------------------------------------
    // Stdout tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_Stdout_ContainsTitle()
    {
        var (_, stdout, _) = RunBuild("title");
        Assert.Contains("MAP-26H", stdout);
    }

    [Fact]
    public void Cli_Stdout_ContainsVerdict()
    {
        var (_, stdout, _) = RunBuild("verdict");
        Assert.Contains("MAP26H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMISSION_AUDIT_RECEIPT_COMPLETE", stdout);
    }

    [Fact]
    public void Cli_Stdout_ContainsAuditedFileCount()
    {
        var (_, stdout, _) = RunBuild("file-count");
        Assert.Contains("audited_file_count", stdout);
        Assert.Contains("12", stdout);
    }

    // -----------------------------------------------------------------------
    // JSON content tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_OutputJson_ContainsAuditedFileCount12()
    {
        var emitterPath = WriteEmitterFixture();
        var emitterOutputRoot = Path.GetDirectoryName(emitterPath)!;
        var (root, j, m, c, s) = MakeOutputPaths("json-content");
        RunCli(Cmd,
            "--emitter-result",      emitterPath,
            "--emitter-output-root", emitterOutputRoot,
            "--output-root",         root,
            "--output-json",         j,
            "--output-md",           m,
            "--output-csv",          c,
            "--summary",             s);
        var json = File.ReadAllText(j);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(12, doc.RootElement.GetProperty("audited_file_count").GetInt32());
    }

    [Fact]
    public void Cli_OutputJson_ContainsHashMismatchCount0()
    {
        var emitterPath = WriteEmitterFixture();
        var emitterOutputRoot = Path.GetDirectoryName(emitterPath)!;
        var (root, j, m, c, s) = MakeOutputPaths("json-hash");
        RunCli(Cmd,
            "--emitter-result",      emitterPath,
            "--emitter-output-root", emitterOutputRoot,
            "--output-root",         root,
            "--output-json",         j,
            "--output-md",           m,
            "--output-csv",          c,
            "--summary",             s);
        var json = File.ReadAllText(j);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(0, doc.RootElement.GetProperty("hash_mismatch_count").GetInt32());
    }

    // -----------------------------------------------------------------------
    // Helper script tests
    // -----------------------------------------------------------------------

    [Fact]
    public void HelperScript_Exists()
    {
        Assert.True(File.Exists(HelperScript), $"Helper script not found: {HelperScript}");
    }

    [Fact]
    public void HelperScript_DoesNotContain_CompileWorldgen()
    {
        var text = File.ReadAllText(HelperScript);
        Assert.DoesNotContain("compile-worldgen", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContain_WorldGenOverrideLua()
    {
        var text = File.ReadAllText(HelperScript);
        Assert.DoesNotContain("WorldGenOverride.lua", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContain_DotLotpack()
    {
        var text = File.ReadAllText(HelperScript);
        Assert.DoesNotContain(".lotpack", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelperScript_DoesNotContain_DotLotheader()
    {
        var text = File.ReadAllText(HelperScript);
        Assert.DoesNotContain(".lotheader", text, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Doc test
    // -----------------------------------------------------------------------

    [Fact]
    public void Doc_Exists()
    {
        Assert.True(File.Exists(DocPath), $"Doc not found: {DocPath}");
    }

    // -----------------------------------------------------------------------
    // Unknown command test
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_UnknownCommand_MentionsAuditReceiptCommand()
    {
        var (_, _, stderr) = RunCli("unknown-command-xyz");
        Assert.Contains("dry-run-emission-audit-receipt", stderr);
    }

    // -----------------------------------------------------------------------
    // Forbidden files test
    // -----------------------------------------------------------------------

    [Fact]
    public void Cli_DoesNotEmit_ForbiddenFiles()
    {
        var emitterPath = WriteEmitterFixture();
        var emitterOutputRoot = Path.GetDirectoryName(emitterPath)!;
        var (root, j, m, c, s) = MakeOutputPaths("forbidden");
        RunCli(Cmd,
            "--emitter-result",      emitterPath,
            "--emitter-output-root", emitterOutputRoot,
            "--output-root",         root,
            "--output-json",         j,
            "--output-md",           m,
            "--output-csv",          c,
            "--summary",             s);

        var allFiles = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
        Assert.DoesNotContain(allFiles, f => f.EndsWith(".lotpack",             StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(allFiles, f => f.EndsWith(".lotheader",           StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(allFiles, f => f.EndsWith(".lua",                 StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(allFiles, f => f.EndsWith("WorldGenOverride.lua", StringComparison.OrdinalIgnoreCase));
    }
}
