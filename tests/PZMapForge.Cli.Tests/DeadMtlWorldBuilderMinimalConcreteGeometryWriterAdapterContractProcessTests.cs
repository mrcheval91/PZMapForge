using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace PZMapForge.Cli.Tests;

public sealed class DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractProcessTests : IDisposable
{
    private readonly string _tempDir;
    private static readonly string s_cliProject =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "PZMapForge.Cli", "PZMapForge.Cli.csproj"));

    public DeadMtlWorldBuilderMinimalConcreteGeometryWriterAdapterContractProcessTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "pzmapforge-map26i-cli-test.local", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private (string auditReceiptPath, string componentPath, string lotPath, string slotPath,
             string frontagePath, string rearPath, string claimPath) WriteEmitterFixture(
        string verdict = "MAP26H_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_DRY_RUN_EMISSION_AUDIT_RECEIPT_COMPLETE",
        bool isValid = true,
        int auditedFileCount = 12,
        int hashMismatchCount = 0,
        int passedAuditCheckCount = 29)
    {
        string auditReceiptPath = Path.Combine(_tempDir, "map_00.audit_receipt.json");
        File.WriteAllText(auditReceiptPath, JsonSerializer.Serialize(new
        {
            verdict,
            is_valid = isValid,
            audited_file_count = auditedFileCount,
            hash_mismatch_count = hashMismatchCount,
            passed_audit_check_count = passedAuditCheckCount
        }));

        string componentPath = Path.Combine(_tempDir, "map_00.component_writer_record.json");
        File.WriteAllText(componentPath, JsonSerializer.Serialize(new
        {
            record_kind = "COMPONENT_WRITER_RECORD",
            map_id = "map_00",
            target_component_id = "map_00_component_0001",
            intent = "RESIDENTIAL_LOT_BLOCK",
            access_readiness_class = "DUAL_ACCESS_CANDIDATE",
            component_bbox = new { min_x = 124, min_y = 10, max_x = 212, max_y = 69, width_px = 89, height_px = 60 },
            dry_run_only = true
        }));

        var lots = Enumerable.Range(1, 7).Select(i => new
        {
            lot_order = i,
            lot_id = $"map_00_comp0001_lot_{i:D4}",
            component_id = "map_00_component_0001",
            min_x = 124 + (i - 1) * 13,
            min_y = 10,
            max_x = 124 + (i - 1) * 13 + 12,
            max_y = 69,
            width_px = 13,
            height_px = 60,
            frontage_side = "NORTH",
            rear_service_side = "EAST",
            geometry_type = "LOT_RECTANGLE_MVP",
            geometry_status = "CONCRETE_PIXEL_GEOMETRY_CREATED"
        }).ToArray();
        string lotPath = Path.Combine(_tempDir, "map_00.lot_writer_records.json");
        File.WriteAllText(lotPath, JsonSerializer.Serialize(new { lots, dry_run_only = true }));

        var buildingSlots = Enumerable.Range(1, 7).Select(i => new
        {
            slot_order = i,
            slot_id = $"map_00_comp0001_slot_{i:D4}",
            lot_id = $"map_00_comp0001_lot_{i:D4}",
            lot_order = i,
            component_id = "map_00_component_0001",
            min_x = 126 + (i - 1) * 13,
            min_y = 13,
            max_x = 134 + (i - 1) * 13,
            max_y = 66,
            width_px = 9,
            height_px = 54,
            frontage_setback_px = 3,
            rear_setback_px = 3,
            side_inset_px = 2,
            slot_status = "ACCEPTED",
            geometry_type = "BUILDING_SLOT_RECTANGLE_MVP",
            geometry_status = "CONCRETE_PIXEL_GEOMETRY_CREATED"
        }).ToArray();
        string slotPath = Path.Combine(_tempDir, "map_00.building_slot_writer_records.json");
        File.WriteAllText(slotPath, JsonSerializer.Serialize(new { building_slots = buildingSlots, dry_run_only = true }));

        string frontagePath = Path.Combine(_tempDir, "map_00.frontage_access_record.json");
        File.WriteAllText(frontagePath, JsonSerializer.Serialize(new
        {
            record_kind = "FRONTAGE_ACCESS_RECORD",
            frontage_side = "NORTH",
            frontage_component_id = "map_00_component_0023",
            frontage_contact_px = 89,
            dry_run_only = true
        }));

        string rearPath = Path.Combine(_tempDir, "map_00.rear_service_access_record.json");
        File.WriteAllText(rearPath, JsonSerializer.Serialize(new
        {
            record_kind = "REAR_SERVICE_ACCESS_RECORD",
            rear_service_side = "EAST",
            rear_service_component_id = "map_00_component_0030",
            rear_service_contact_px = 60,
            dry_run_only = true
        }));

        string claimPath = Path.Combine(_tempDir, "map_00.claim_boundary_record.json");
        File.WriteAllText(claimPath, JsonSerializer.Serialize(new
        {
            record_kind = "CLAIM_BOUNDARY_RECORD",
            writer_ready = false,
            runtime_valid = false,
            materialized = false,
            approved_for_writer_experiment = false,
            writer_experiment_gate_status = "LOCKED_PENDING_OPERATOR_APPROVAL"
        }));

        return (auditReceiptPath, componentPath, lotPath, slotPath, frontagePath, rearPath, claimPath);
    }

    private (int exitCode, string stdout, string stderr) RunCli(string[] args)
    {
        var psi = new ProcessStartInfo("dotnet", $"run --project \"{s_cliProject}\" -- " + string.Join(" ", args))
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false
        };
        using var proc = Process.Start(psi)!;
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        Task.WaitAll(stdoutTask, stderrTask);
        proc.WaitForExit();
        return (proc.ExitCode, stdoutTask.Result, stderrTask.Result);
    }

    private string[] BuildArgs(string auditReceipt, string component, string lot, string slot,
        string frontage, string rear, string claim,
        string? outputJson = null, string? outputMd = null, string? outputCsv = null, string? summary = null)
    {
        outputJson  ??= Path.Combine(_tempDir, "out.json");
        outputMd    ??= Path.Combine(_tempDir, "out.md");
        outputCsv   ??= Path.Combine(_tempDir, "out.csv");
        summary     ??= Path.Combine(_tempDir, "out.summary.txt");
        return new[]
        {
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-adapter-contract",
            "--audit-receipt",          auditReceipt,
            "--component-record",       component,
            "--lot-records",            lot,
            "--building-slot-records",  slot,
            "--frontage-access",        frontage,
            "--rear-service-access",    rear,
            "--claim-boundary",         claim,
            "--output-root",            _tempDir,
            "--output-json",            outputJson,
            "--output-md",              outputMd,
            "--output-csv",             outputCsv,
            "--summary",                summary
        };
    }

    [Fact]
    public void ValidFixture_ExitCode0()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteEmitterFixture();
        var (exitCode, _, _) = RunCli(BuildArgs(ar, comp, lot, slot, front, rear, claim));
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void ValidFixture_OutputJsonExists()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteEmitterFixture();
        var outJson = Path.Combine(_tempDir, "result.json");
        RunCli(BuildArgs(ar, comp, lot, slot, front, rear, claim, outputJson: outJson));
        Assert.True(File.Exists(outJson));
    }

    [Fact]
    public void ValidFixture_OutputMdExists()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteEmitterFixture();
        var outMd = Path.Combine(_tempDir, "result.md");
        RunCli(BuildArgs(ar, comp, lot, slot, front, rear, claim, outputMd: outMd));
        Assert.True(File.Exists(outMd));
    }

    [Fact]
    public void ValidFixture_OutputCsvExists()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteEmitterFixture();
        var outCsv = Path.Combine(_tempDir, "result.csv");
        RunCli(BuildArgs(ar, comp, lot, slot, front, rear, claim, outputCsv: outCsv));
        Assert.True(File.Exists(outCsv));
    }

    [Fact]
    public void ValidFixture_SummaryExists()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteEmitterFixture();
        var outSummary = Path.Combine(_tempDir, "result.summary.txt");
        RunCli(BuildArgs(ar, comp, lot, slot, front, rear, claim, summary: outSummary));
        Assert.True(File.Exists(outSummary));
    }

    [Fact]
    public void ValidFixture_OutputJsonContainsCompleteVerdict()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteEmitterFixture();
        var outJson = Path.Combine(_tempDir, "result.json");
        RunCli(BuildArgs(ar, comp, lot, slot, front, rear, claim, outputJson: outJson));
        var json = File.ReadAllText(outJson);
        Assert.Contains("MAP26I_WORLDBUILDER_MINIMAL_CONCRETE_GEOMETRY_WRITER_ADAPTER_CONTRACT_COMPLETE", json);
    }

    [Fact]
    public void ValidFixture_StdoutContainsSummary()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteEmitterFixture();
        var (_, stdout, _) = RunCli(BuildArgs(ar, comp, lot, slot, front, rear, claim));
        Assert.Contains("MAP-26I", stdout);
    }

    [Fact]
    public void MissingAuditReceipt_ExitCode1()
    {
        var (_, comp, lot, slot, front, rear, claim) = WriteEmitterFixture();
        var (exitCode, _, _) = RunCli(BuildArgs(
            Path.Combine(_tempDir, "missing.json"), comp, lot, slot, front, rear, claim));
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void MissingRequiredArg_ExitCode1()
    {
        var (exitCode, _, stderr) = RunCli(new[] { "deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-adapter-contract" });
        Assert.Equal(1, exitCode);
        Assert.Contains("required", stderr);
    }

    [Fact]
    public void OutputWithoutDotLocal_ExitCode1()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteEmitterFixture();
        var noLocal = Path.Combine(Path.GetTempPath(), "no-local-dir");
        var (exitCode, _, _) = RunCli(new[]
        {
            "deadmtl-build-worldbuilder-minimal-concrete-geometry-writer-adapter-contract",
            "--audit-receipt",          ar,
            "--component-record",       comp,
            "--lot-records",            lot,
            "--building-slot-records",  slot,
            "--frontage-access",        front,
            "--rear-service-access",    rear,
            "--claim-boundary",         claim,
            "--output-root",            noLocal,
            "--output-json",            Path.Combine(noLocal, "out.json"),
            "--output-md",              Path.Combine(noLocal, "out.md"),
            "--output-csv",             Path.Combine(noLocal, "out.csv"),
            "--summary",                Path.Combine(noLocal, "out.summary.txt")
        });
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void ValidFixture_OutputJson_IsValidTrue()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteEmitterFixture();
        var outJson = Path.Combine(_tempDir, "result.json");
        RunCli(BuildArgs(ar, comp, lot, slot, front, rear, claim, outputJson: outJson));
        using var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.True(doc.RootElement.GetProperty("is_valid").GetBoolean());
    }

    [Fact]
    public void ValidFixture_OutputJson_CheckCount28()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteEmitterFixture();
        var outJson = Path.Combine(_tempDir, "result.json");
        RunCli(BuildArgs(ar, comp, lot, slot, front, rear, claim, outputJson: outJson));
        using var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.Equal(28, doc.RootElement.GetProperty("adapter_check_count").GetInt32());
    }

    [Fact]
    public void ValidFixture_OutputJson_NormalizedLots7()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteEmitterFixture();
        var outJson = Path.Combine(_tempDir, "result.json");
        RunCli(BuildArgs(ar, comp, lot, slot, front, rear, claim, outputJson: outJson));
        using var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.Equal(7, doc.RootElement.GetProperty("normalized_lots").GetArrayLength());
    }

    [Fact]
    public void ValidFixture_OutputJson_ForbiddenFamilies8()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteEmitterFixture();
        var outJson = Path.Combine(_tempDir, "result.json");
        RunCli(BuildArgs(ar, comp, lot, slot, front, rear, claim, outputJson: outJson));
        using var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.Equal(8, doc.RootElement.GetProperty("forbidden_output_families").GetArrayLength());
    }

    [Fact]
    public void ValidFixture_OutputJson_WriterReadyFalse()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteEmitterFixture();
        var outJson = Path.Combine(_tempDir, "result.json");
        RunCli(BuildArgs(ar, comp, lot, slot, front, rear, claim, outputJson: outJson));
        using var doc = JsonDocument.Parse(File.ReadAllText(outJson));
        Assert.False(doc.RootElement.GetProperty("writer_ready").GetBoolean());
    }

    [Fact]
    public void WrongAuditVerdict_ExitCode1()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteEmitterFixture(verdict: "WRONG_VERDICT");
        var (exitCode, _, _) = RunCli(BuildArgs(ar, comp, lot, slot, front, rear, claim));
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void SummaryContains28ForChecks()
    {
        var (ar, comp, lot, slot, front, rear, claim) = WriteEmitterFixture();
        var outSummary = Path.Combine(_tempDir, "result.summary.txt");
        RunCli(BuildArgs(ar, comp, lot, slot, front, rear, claim, summary: outSummary));
        var summary = File.ReadAllText(outSummary);
        Assert.Contains("Checks           : 28", summary);
    }
}
