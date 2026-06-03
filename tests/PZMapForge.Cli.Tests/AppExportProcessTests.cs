using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using Xunit;

namespace PZMapForge.Cli.Tests;

/// <summary>
/// Process-level tests for the app-export CLI command.
/// All tests use programmatically generated images only.
/// No real PZ install is required.
/// Claim boundary: planning_artifact_only_not_pz_load_tested
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class AppExportProcessTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-app-export-tests", Path.GetRandomFileName());

    public AppExportProcessTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private static string PalettePath =>
        Path.Combine(RepoRoot, "source", "image-palette.json");

    private string CreateTestImage()
    {
        var imgPath = Path.Combine(_tempDir, "test.png");
        using var bmp = new Bitmap(300, 300);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(255, 100, 140, 70));
        bmp.Save(imgPath, ImageFormat.Png);
        return imgPath;
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
    // Test 1: valid image writes index.html and exits 0
    // -----------------------------------------------------------------------

    [Fact]
    public void AppExport_ValidImage_WritesIndexHtml()
    {
        var imgPath   = CreateTestImage();
        var outputDir = Path.Combine(_tempDir, ".local", "app");

        var (code, stdout, stderr) = RunCli(
            "app-export",
            "--path",    imgPath,
            "--palette", PalettePath,
            "--output",  outputDir);

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
        Assert.True(File.Exists(Path.Combine(outputDir, "index.html")),
            "index.html was not written");
        Assert.True(File.Exists(Path.Combine(outputDir, "images", "input-image.png")),
            "input image was not copied to images/");
        Assert.True(File.Exists(Path.Combine(outputDir, "images", "parsed-preview.png")),
            "parsed-preview.png was not written to images/");
    }

    // -----------------------------------------------------------------------
    // Test 2: index.html contains claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void AppExport_IndexHtml_ContainsClaimBoundary()
    {
        var imgPath   = CreateTestImage();
        var outputDir = Path.Combine(_tempDir, ".local", "app");

        var (code, _, _) = RunCli(
            "app-export",
            "--path",    imgPath,
            "--palette", PalettePath,
            "--output",  outputDir);

        Assert.Equal(0, code);

        var html = File.ReadAllText(Path.Combine(outputDir, "index.html"));
        Assert.Contains("planning_artifact_only_not_pz_load_tested", html, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 3: index.html links artifact file names
    // -----------------------------------------------------------------------

    [Fact]
    public void AppExport_IndexHtml_ContainsArtifactLinks()
    {
        var imgPath   = CreateTestImage();
        var outputDir = Path.Combine(_tempDir, ".local", "app");

        var (code, _, _) = RunCli(
            "app-export",
            "--path",    imgPath,
            "--palette", PalettePath,
            "--output",  outputDir);

        Assert.Equal(0, code);

        var html = File.ReadAllText(Path.Combine(outputDir, "index.html"));
        Assert.Contains("parsed-cell.json",          html, StringComparison.Ordinal);
        Assert.Contains("regions.json",              html, StringComparison.Ordinal);
        Assert.Contains("primitives.json",           html, StringComparison.Ordinal);
        Assert.Contains("plan-recommendations.json", html, StringComparison.Ordinal);
        Assert.Contains("<img ",                     html, StringComparison.Ordinal);
        Assert.Contains("images/input-image",        html, StringComparison.Ordinal);
        Assert.Contains("class=\"card\"",            html, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Test 4: refuses output outside .local
    // -----------------------------------------------------------------------

    [Fact]
    public void AppExport_NonLocalOutput_ExitsOne()
    {
        var imgPath   = CreateTestImage();
        var badOutput = _tempDir;  // no .local segment

        var (code, _, stderr) = RunCli(
            "app-export",
            "--path",    imgPath,
            "--palette", PalettePath,
            "--output",  badOutput);

        Assert.Equal(1, code);
        Assert.Contains(".local", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 5: exits nonzero when --path is missing
    // -----------------------------------------------------------------------

    [Fact]
    public void AppExport_MissingPath_ExitsOne()
    {
        var (code, _, stderr) = RunCli("app-export", "--palette", PalettePath);

        Assert.Equal(1, code);
        Assert.Contains("--path", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test 6: --run-name writes index.html in named subdirectory
    // -----------------------------------------------------------------------

    [Fact]
    public void AppExport_RunName_WritesIndexHtmlInSubdir()
    {
        var imgPath   = CreateTestImage();
        var outputDir = Path.Combine(_tempDir, ".local", "app");

        var (code, stdout, stderr) = RunCli(
            "app-export",
            "--path",     imgPath,
            "--palette",  PalettePath,
            "--output",   outputDir,
            "--run-name", "smoke");

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
        Assert.True(File.Exists(Path.Combine(outputDir, "smoke", "index.html")),
            "index.html was not written under smoke/ subdirectory");
    }

    // -----------------------------------------------------------------------
    // Test 7: unsafe run-name characters are sanitized deterministically
    // -----------------------------------------------------------------------

    [Fact]
    public void AppExport_UnsafeRunName_IsSanitized()
    {
        var imgPath   = CreateTestImage();
        var outputDir = Path.Combine(_tempDir, ".local", "app");

        // "my test run!" -> sanitized -> "my-test-run"
        var (code, stdout, stderr) = RunCli(
            "app-export",
            "--path",     imgPath,
            "--palette",  PalettePath,
            "--output",   outputDir,
            "--run-name", "my test run!");

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
        Assert.True(File.Exists(Path.Combine(outputDir, "my-test-run", "index.html")),
            "sanitized run-name subdirectory not found");
    }
}

// ---------------------------------------------------------------------------
// Shared fixture for content contract tests (one pipeline run, many assertions)
// ---------------------------------------------------------------------------

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class AppExportContentFixture : IDisposable
{
    private readonly string _root;

    public string OutputDir { get; }
    public int    ExitCode  { get; }
    public string IndexHtml { get; }

    public AppExportContentFixture()
    {
        _root     = Path.Combine(Path.GetTempPath(), "pzmapforge-app-content", Path.GetRandomFileName());
        OutputDir = Path.Combine(_root, ".local", "app");
        Directory.CreateDirectory(OutputDir);

        var imgPath = Path.Combine(_root, "grass.png");
        using (var bmp = new System.Drawing.Bitmap(300, 300))
        using (var g   = System.Drawing.Graphics.FromImage(bmp))
        {
            g.Clear(System.Drawing.Color.FromArgb(255, 100, 140, 70));
            bmp.Save(imgPath, System.Drawing.Imaging.ImageFormat.Png);
        }

        var repoRoot   = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var cliProject = Path.Combine(repoRoot, "src", "PZMapForge.Cli");
        var palette    = Path.Combine(repoRoot, "source", "image-palette.json");

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "dotnet",
            WorkingDirectory       = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        foreach (var a in new[]
            { "run", "--project", cliProject, "--configuration", "Release", "--no-build", "--",
              "app-export",
              "--path",    imgPath,
              "--palette", palette,
              "--output",  OutputDir })
            psi.ArgumentList.Add(a);

        using var proc = System.Diagnostics.Process.Start(psi)!;
        proc.StandardOutput.ReadToEnd();
        proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        ExitCode         = proc.ExitCode;
        IndexHtml        = ExitCode == 0
            ? File.ReadAllText(Path.Combine(OutputDir, "index.html"))
            : string.Empty;
        ParsedPreviewExists = ExitCode == 0 &&
            File.Exists(Path.Combine(OutputDir, "images", "parsed-preview.png"));
    }

    public bool ParsedPreviewExists { get; }

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); }
        catch { /* best effort */ }
    }
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class AppExportContentTests : IClassFixture<AppExportContentFixture>
{
    private readonly AppExportContentFixture _fix;
    public AppExportContentTests(AppExportContentFixture fix) => _fix = fix;

    [Fact]
    public void AppExport_Content_ExitsZero() =>
        Assert.Equal(0, _fix.ExitCode);

    [Fact]
    public void AppExport_Content_ContainsVisualLegend() =>
        Assert.Contains("Visual Legend", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Content_ContainsSwatchClass() =>
        Assert.Contains("class=\"swatch\"", _fix.IndexHtml, StringComparison.Ordinal);

    [Fact]
    public void AppExport_Content_ContainsPaletteName() =>
        Assert.Contains("Palette", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Content_ContainsArtifactFilesSection() =>
        Assert.Contains("Artifact Files", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Content_ContainsMapPreviewSection() =>
        Assert.Contains("Map Preview", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Content_ContainsSummarySection() =>
        Assert.Contains("Summary", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Content_ContainsWorkbenchClass() =>
        Assert.Contains("class=\"workbench\"", _fix.IndexHtml, StringComparison.Ordinal);

    [Fact]
    public void AppExport_Content_WritesParsedPreview() =>
        Assert.True(_fix.ParsedPreviewExists, "parsed-preview.png was not written");

    [Fact]
    public void AppExport_Content_ContainsAnalysisInputLabel() =>
        Assert.Contains("Analysis Input", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Content_ContainsParsedPreviewLabel() =>
        Assert.Contains("Parsed Preview", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Content_ContainsPaletteHealthSection() =>
        Assert.Contains("Palette Health", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Content_ContainsNotPaletteCleanText() =>
        Assert.Contains("not palette-clean", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Content_ContainsTextLabelsGuidance() =>
        Assert.Contains("Text labels and antialiasing should not be part of the analysis image", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Content_ContainsCleanPaletteOnlyGuidance() =>
        Assert.Contains("clean palette-only analysis image", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);
}

// ---------------------------------------------------------------------------
// Annotation workflow process tests
// ---------------------------------------------------------------------------

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class AppExportAnnotationTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-annotation-tests", Path.GetRandomFileName());

    public AppExportAnnotationTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string CliProjectPath =>
        Path.Combine(RepoRoot, "src", "PZMapForge.Cli");

    private static string PalettePath =>
        Path.Combine(RepoRoot, "source", "image-palette.json");

    private string CreateAnalysisImage()
    {
        var imgPath = Path.Combine(_tempDir, "analysis.png");
        using var bmp = new System.Drawing.Bitmap(300, 300);
        using var g   = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.FromArgb(255, 100, 140, 70));
        bmp.Save(imgPath, System.Drawing.Imaging.ImageFormat.Png);
        return imgPath;
    }

    private string CreateAnnotationImage()
    {
        var imgPath = Path.Combine(_tempDir, "annotation.png");
        using var bmp = new System.Drawing.Bitmap(300, 300);
        using var g   = System.Drawing.Graphics.FromImage(bmp);
        g.Clear(System.Drawing.Color.FromArgb(255, 100, 140, 70));
        g.DrawString("District A", new System.Drawing.Font("Arial", 12),
            System.Drawing.Brushes.Red, 10, 10);
        bmp.Save(imgPath, System.Drawing.Imaging.ImageFormat.Png);
        return imgPath;
    }

    private static (int ExitCode, string Stdout, string Stderr) RunCli(params string[] args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
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

        using var proc = System.Diagnostics.Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stdout, stderr);
    }

    // -----------------------------------------------------------------------
    // Test: --annotation writes annotation-image.png and updates index.html
    // -----------------------------------------------------------------------

    [Fact]
    public void AppExport_WithAnnotation_WritesFileAndUpdatesHtml()
    {
        var analysisPath   = CreateAnalysisImage();
        var annotationPath = CreateAnnotationImage();
        var outputDir      = Path.Combine(_tempDir, ".local", "app");

        var (code, stdout, stderr) = RunCli(
            "app-export",
            "--path",       analysisPath,
            "--palette",    PalettePath,
            "--output",     outputDir,
            "--annotation", annotationPath);

        Assert.True(code == 0, $"Exited {code}. Stdout: {stdout}. Stderr: {stderr}");
        Assert.True(File.Exists(Path.Combine(outputDir, "images", "annotation-image.png")),
            "annotation-image.png was not written");

        var html = File.ReadAllText(Path.Combine(outputDir, "index.html"));
        Assert.Contains("Annotation Reference", html, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test: missing --annotation file exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void AppExport_MissingAnnotationFile_ExitsOne()
    {
        var analysisPath = CreateAnalysisImage();
        var outputDir    = Path.Combine(_tempDir, ".local", "app");
        var missingPath  = Path.Combine(_tempDir, "nonexistent-annotation.png");

        var (code, _, stderr) = RunCli(
            "app-export",
            "--path",       analysisPath,
            "--palette",    PalettePath,
            "--output",     outputDir,
            "--annotation", missingPath);

        Assert.Equal(1, code);
        Assert.Contains("annotation", stderr, StringComparison.OrdinalIgnoreCase);
    }

    // -----------------------------------------------------------------------
    // Test: missing --svg-selection file exits nonzero
    // -----------------------------------------------------------------------

    [Fact]
    public void AppExport_MissingSvgSelectionFile_ExitsOne()
    {
        var analysisPath = CreateAnalysisImage();
        var outputDir    = Path.Combine(_tempDir, ".local", "app");
        var missingPath  = Path.Combine(_tempDir, "nonexistent-selection.json");

        var (code, _, stderr) = RunCli(
            "app-export",
            "--path",          analysisPath,
            "--palette",       PalettePath,
            "--output",        outputDir,
            "--svg-selection", missingPath);

        Assert.Equal(1, code);
        Assert.Contains("svg-selection", stderr, StringComparison.OrdinalIgnoreCase);
    }
}

// ---------------------------------------------------------------------------
// Shared fixture: one SVG annotation pipeline run, multiple assertions
// ---------------------------------------------------------------------------

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class AppExportSvgFixture : IDisposable
{
    private readonly string _root;

    public string OutputDir  { get; }
    public int    ExitCode   { get; }
    public string IndexHtml  { get; }

    public bool SvgImageExists    { get; }
    public bool SvgSummaryExists  { get; }
    public string SvgSummaryJson  { get; }

    public AppExportSvgFixture()
    {
        _root     = Path.Combine(Path.GetTempPath(), "pzmapforge-svg-tests", Path.GetRandomFileName());
        OutputDir = Path.Combine(_root, ".local", "app");
        Directory.CreateDirectory(OutputDir);

        // analysis image: solid grass colour, 300x300
        var imgPath = Path.Combine(_root, "analysis.png");
        using (var bmp = new System.Drawing.Bitmap(300, 300))
        using (var g   = System.Drawing.Graphics.FromImage(bmp))
        {
            g.Clear(System.Drawing.Color.FromArgb(255, 100, 140, 70));
            bmp.Save(imgPath, System.Drawing.Imaging.ImageFormat.Png);
        }

        // richer SVG: technical layer, transit label, district, park, path, polygon, text
        var svgPath = Path.Combine(_root, "reference.svg");
        File.WriteAllText(svgPath,
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"300\" height=\"300\">" +
            "<g id=\"fond_arrond\" class=\"background\">" +
            "<path id=\"path-a\" d=\"M10 10 L290 10 L290 290 Z\"/>" +
            "<polygon id=\"polygon-b\" points=\"50,50 250,50 250,250\"/>" +
            "</g>" +
            "<g id=\"districts\">" +
            "<text x=\"20\" y=\"30\">District A</text>" +
            "<text x=\"20\" y=\"50\">ANGRIGNON</text>" +
            "<text x=\"20\" y=\"70\">Pte-Angus</text>" +
            "</g></svg>",
            System.Text.Encoding.UTF8);

        var repoRoot   = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var cliProject = Path.Combine(repoRoot, "src", "PZMapForge.Cli");
        var palette    = Path.Combine(repoRoot, "source", "image-palette.json");

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "dotnet",
            WorkingDirectory       = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        foreach (var a in new[]
            { "run", "--project", cliProject, "--configuration", "Release", "--no-build", "--",
              "app-export",
              "--path",       imgPath,
              "--palette",    palette,
              "--output",     OutputDir,
              "--annotation", svgPath })
            psi.ArgumentList.Add(a);

        using var proc = System.Diagnostics.Process.Start(psi)!;
        proc.StandardOutput.ReadToEnd();
        proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        ExitCode = proc.ExitCode;

        var svgImagePath   = Path.Combine(OutputDir, "images", "annotation-image.svg");
        var svgSummaryPath = Path.Combine(OutputDir, "artifacts", "svg-reference-summary.json");
        var svgStructurePath = Path.Combine(OutputDir, "artifacts", "svg-reference-structure.json");
        IndexHtml         = ExitCode == 0 && File.Exists(Path.Combine(OutputDir, "index.html"))
            ? File.ReadAllText(Path.Combine(OutputDir, "index.html"))
            : string.Empty;
        SvgImageExists    = File.Exists(svgImagePath);
        SvgSummaryExists  = File.Exists(svgSummaryPath);
        SvgSummaryJson    = SvgSummaryExists  ? File.ReadAllText(svgSummaryPath)    : string.Empty;
        SvgStructureExists = File.Exists(svgStructurePath);
        SvgStructureJson   = SvgStructureExists ? File.ReadAllText(svgStructurePath) : string.Empty;
        var svgCandidatesPath = Path.Combine(OutputDir, "artifacts", "svg-layer-candidates.json");
        SvgLayerCandidatesExists = File.Exists(svgCandidatesPath);
        SvgLayerCandidatesJson   = SvgLayerCandidatesExists ? File.ReadAllText(svgCandidatesPath) : string.Empty;
        var svgSelectionPath  = Path.Combine(OutputDir, "artifacts", "svg-layer-selection.template.json");
        SvgLayerSelectionExists = File.Exists(svgSelectionPath);
        SvgLayerSelectionJson   = SvgLayerSelectionExists ? File.ReadAllText(svgSelectionPath) : string.Empty;
    }

    public bool   SvgStructureExists       { get; }
    public bool   SvgLayerCandidatesExists  { get; }
    public string SvgLayerCandidatesJson    { get; }
    public bool   SvgLayerSelectionExists   { get; }
    public string SvgLayerSelectionJson     { get; }
    public string SvgStructureJson   { get; }

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); }
        catch { /* best effort */ }
    }
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class AppExportSvgAnnotationTests : IClassFixture<AppExportSvgFixture>
{
    private readonly AppExportSvgFixture _fix;
    public AppExportSvgAnnotationTests(AppExportSvgFixture fix) => _fix = fix;

    [Fact]
    public void AppExport_Svg_ExitsZero() =>
        Assert.Equal(0, _fix.ExitCode);

    [Fact]
    public void AppExport_Svg_WritesAnnotationSvg() =>
        Assert.True(_fix.SvgImageExists, "annotation-image.svg was not written to images/");

    [Fact]
    public void AppExport_Svg_WritesSvgReferenceSummary() =>
        Assert.True(_fix.SvgSummaryExists, "svg-reference-summary.json was not written to artifacts/");

    [Fact]
    public void AppExport_Svg_IndexHtmlContainsSvgVectorReference() =>
        Assert.Contains("SVG Vector Reference", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_IndexHtmlContainsSvgNotParsedGeometry() =>
        Assert.Contains("SVG is not parsed into map geometry", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_SummaryContainsParsedAsGeometryFalse() =>
        Assert.Contains("\"parsed_as_geometry\": false", _fix.SvgSummaryJson, StringComparison.OrdinalIgnoreCase);

    // SVG-2: structure inspector tests

    [Fact]
    public void AppExport_Svg_WritesSvgReferenceStructure() =>
        Assert.True(_fix.SvgStructureExists, "svg-reference-structure.json was not written");

    [Fact]
    public void AppExport_Svg_StructureContainsParsedAsGeometryFalse() =>
        Assert.Contains("\"parsed_as_geometry\": false", _fix.SvgStructureJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_StructureContainsConvertedToMapGeometryFalse() =>
        Assert.Contains("\"converted_to_map_geometry\": false", _fix.SvgStructureJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_StructureElementCountsIncludePath() =>
        Assert.Contains("\"path\":", _fix.SvgStructureJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_StructureSampleIdsIncludeDistrictsId() =>
        Assert.Contains("districts", _fix.SvgStructureJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_StructureSampleTextLabelsIncludeDistrictA() =>
        Assert.Contains("District A", _fix.SvgStructureJson, StringComparison.Ordinal);

    [Fact]
    public void AppExport_Svg_IndexHtmlContainsSvgStructure() =>
        Assert.Contains("SVG Structure", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    // SVG-2A: parse status fields

    [Fact]
    public void AppExport_Svg_StructureContainsParseStatus() =>
        Assert.Contains("parse_status", _fix.SvgStructureJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_StructureParseStatusIsParsed() =>
        Assert.Contains("\"parsed\"", _fix.SvgStructureJson, StringComparison.Ordinal);

    [Fact]
    public void AppExport_Svg_StructureParseErrorIsEmpty() =>
        Assert.Contains("\"parse_error\": \"\"", _fix.SvgStructureJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_StructureContainsMaxCharactersField() =>
        Assert.Contains("max_characters_in_document", _fix.SvgStructureJson, StringComparison.OrdinalIgnoreCase);

    // SVG-3: structure viewer panel

    [Fact]
    public void AppExport_Svg_IndexHtmlContainsSvgStructureSummary() =>
        Assert.Contains("SVG Structure Summary", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_IndexHtmlContainsParseStatus() =>
        Assert.Contains("parse_status", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_IndexHtmlContainsElementCounts() =>
        Assert.Contains("Element Counts", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_IndexHtmlContainsSampleIds() =>
        Assert.Contains("Sample IDs", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_IndexHtmlContainsSampleTextLabels() =>
        Assert.Contains("Sample Text Labels", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_IndexHtmlContainsNotConvertedLanguage() =>
        Assert.Contains("not converted to map geometry", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    // SVG-4: layer candidate inventory

    [Fact]
    public void AppExport_Svg_WritesSvgLayerCandidates() =>
        Assert.True(_fix.SvgLayerCandidatesExists, "svg-layer-candidates.json was not written");

    [Fact]
    public void AppExport_Svg_LayerCandidatesContainsMethod() =>
        Assert.Contains("metadata_name_pattern_only", _fix.SvgLayerCandidatesJson, StringComparison.Ordinal);

    [Fact]
    public void AppExport_Svg_LayerCandidatesContainsConvertedFalse() =>
        Assert.Contains("\"converted_to_map_geometry\": false", _fix.SvgLayerCandidatesJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_LayerCandidatesContainsBoroughKey() =>
        Assert.Contains("borough_or_district_candidates", _fix.SvgLayerCandidatesJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_LayerCandidatesContainsLabelKey() =>
        Assert.Contains("label_candidates", _fix.SvgLayerCandidatesJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_IndexHtmlContainsSvgLayerCandidates() =>
        Assert.Contains("SVG Layer Candidates", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_IndexHtmlContainsMetadataCandidatesLanguage() =>
        Assert.Contains("metadata candidates only", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_IndexHtmlContainsNoGeometryConversionNote() =>
        Assert.Contains("No SVG geometry is converted", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    // SVG-5: tuned classification

    [Fact]
    public void AppExport_Svg_LayerCandidatesContainsTransitKey() =>
        Assert.Contains("transit_or_station_candidates", _fix.SvgLayerCandidatesJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_LayerCandidatesContainsTechnicalKey() =>
        Assert.Contains("technical_layer_candidates", _fix.SvgLayerCandidatesJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_LayerCandidatesContainsGenerationNotes() =>
        Assert.Contains("candidate_generation_notes", _fix.SvgLayerCandidatesJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_LayerCandidatesContainsInspectedSources() =>
        Assert.Contains("inspected_metadata_sources", _fix.SvgLayerCandidatesJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_IndexHtmlContainsTransitStation() =>
        Assert.Contains("Transit / Station", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_IndexHtmlContainsTechnicalLayers() =>
        Assert.Contains("Technical Layers", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_IndexHtmlStillContainsMetadataCandidatesOnly() =>
        Assert.Contains("metadata candidates only", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_IndexHtmlStillContainsNoGeometryConverted() =>
        Assert.Contains("No SVG geometry is converted", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    // SVG-6: full metadata inventory

    [Fact]
    public void AppExport_Svg_LayerCandidatesContainsTotalIdField() =>
        Assert.Contains("total_id_values_inspected", _fix.SvgLayerCandidatesJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_LayerCandidatesContainsTotalClassField() =>
        Assert.Contains("total_class_values_inspected", _fix.SvgLayerCandidatesJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_LayerCandidatesContainsTotalTextLabelsField() =>
        Assert.Contains("total_text_labels_inspected", _fix.SvgLayerCandidatesJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_LayerCandidatesContainsTotalMetadataField() =>
        Assert.Contains("total_metadata_values_inspected", _fix.SvgLayerCandidatesJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_IndexHtmlContainsMetadataValuesInspected() =>
        Assert.Contains("Metadata Values Inspected", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    // SVG-7: layer selection template

    [Fact]
    public void AppExport_Svg_WritesLayerSelectionTemplate() =>
        Assert.True(_fix.SvgLayerSelectionExists, "svg-layer-selection.template.json was not written");

    [Fact]
    public void AppExport_Svg_SelectionTemplateContainsOperatorReviewRequired() =>
        Assert.Contains("operator_review_required", _fix.SvgLayerSelectionJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_SelectionTemplateContainsSelectedFalse() =>
        Assert.Contains("\"selected\": false", _fix.SvgLayerSelectionJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_SelectionTemplateContainsIntendedUse() =>
        Assert.Contains("\"intended_use\"", _fix.SvgLayerSelectionJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_SelectionTemplateContainsConvertedFalse() =>
        Assert.Contains("\"converted_to_map_geometry\": false", _fix.SvgLayerSelectionJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_IndexHtmlContainsSvgLayerSelectionTemplate() =>
        Assert.Contains("SVG Layer Selection Template", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_IndexHtmlContainsOperatorReviewRequired() =>
        Assert.Contains("Operator review required", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Svg_IndexHtmlContainsDoesNotConvertGeometry() =>
        Assert.Contains("does not convert SVG geometry", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);
}

// ---------------------------------------------------------------------------
// Full inventory fixture: 35 borough IDs — proves count can exceed sample limit
// ---------------------------------------------------------------------------

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class AppExportSvgFullInventoryFixture : IDisposable
{
    private readonly string _root;
    public string OutputDir              { get; }
    public int    ExitCode               { get; }
    public string SvgLayerCandidatesJson { get; }

    public AppExportSvgFullInventoryFixture()
    {
        _root     = Path.Combine(Path.GetTempPath(), "pzmapforge-svg-full-inv", Path.GetRandomFileName());
        OutputDir = Path.Combine(_root, ".local", "app");
        Directory.CreateDirectory(OutputDir);

        var imgPath = Path.Combine(_root, "analysis.png");
        using (var bmp = new System.Drawing.Bitmap(300, 300))
        using (var g   = System.Drawing.Graphics.FromImage(bmp))
        {
            g.Clear(System.Drawing.Color.FromArgb(255, 100, 140, 70));
            bmp.Save(imgPath, System.Drawing.Imaging.ImageFormat.Png);
        }

        // SVG with 35 unique non-matching IDs (all go to borough_or_district).
        var sb = new System.Text.StringBuilder();
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"300\" height=\"300\">");
        for (var i = 1; i <= 35; i++)
            sb.Append($"<g id=\"zone-{i:D2}\"/>");
        sb.Append("</svg>");

        var svgPath = Path.Combine(_root, "large-reference.svg");
        File.WriteAllText(svgPath, sb.ToString(), System.Text.Encoding.UTF8);

        var repoRoot   = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var cliProject = Path.Combine(repoRoot, "src", "PZMapForge.Cli");
        var palette    = Path.Combine(repoRoot, "source", "image-palette.json");

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "dotnet",
            WorkingDirectory       = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        foreach (var a in new[]
            { "run", "--project", cliProject, "--configuration", "Release", "--no-build", "--",
              "app-export", "--path", imgPath, "--palette", palette,
              "--output", OutputDir, "--annotation", svgPath })
            psi.ArgumentList.Add(a);

        using var proc = System.Diagnostics.Process.Start(psi)!;
        proc.StandardOutput.ReadToEnd();
        proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        ExitCode = proc.ExitCode;

        var candidatesPath       = Path.Combine(OutputDir, "artifacts", "svg-layer-candidates.json");
        SvgLayerCandidatesJson   = File.Exists(candidatesPath) ? File.ReadAllText(candidatesPath) : string.Empty;
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); }
        catch { /* best effort */ }
    }
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class AppExportSvgFullInventoryTests : IClassFixture<AppExportSvgFullInventoryFixture>
{
    private readonly AppExportSvgFullInventoryFixture _fix;
    public AppExportSvgFullInventoryTests(AppExportSvgFullInventoryFixture fix) => _fix = fix;

    [Fact]
    public void AppExport_SvgFullInventory_ExitsZero() =>
        Assert.Equal(0, _fix.ExitCode);

    [Fact]
    public void AppExport_SvgFullInventory_BoroughCountExceedsSampleLimit()
    {
        // 35 IDs + svg root (no id) — borough bucket should have 35 items but samples capped at 30.
        Assert.Contains("\"count\": 35", _fix.SvgLayerCandidatesJson, StringComparison.Ordinal);
    }
}

// ---------------------------------------------------------------------------
// SVG-8: selection review fixture
// ---------------------------------------------------------------------------

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class AppExportSelectionFixture : IDisposable
{
    private readonly string _root;

    public string OutputDir         { get; }
    public int    ExitCode          { get; }
    public string SelectionReviewJson { get; }
    public string IndexHtml         { get; }

    public AppExportSelectionFixture()
    {
        _root     = Path.Combine(Path.GetTempPath(), "pzmapforge-selection", Path.GetRandomFileName());
        OutputDir = Path.Combine(_root, ".local", "app");
        Directory.CreateDirectory(OutputDir);

        var imgPath = Path.Combine(_root, "analysis.png");
        using (var bmp = new System.Drawing.Bitmap(300, 300))
        using (var g   = System.Drawing.Graphics.FromImage(bmp))
        {
            g.Clear(System.Drawing.Color.FromArgb(255, 100, 140, 70));
            bmp.Save(imgPath, System.Drawing.Imaging.ImageFormat.Png);
        }

        // Minimal selection JSON with one selected item.
        var selectionJson = """
{
  "schema": "pzmapforge.svg-layer-selection-template.v0.1",
  "source_file_name": "test.svg",
  "selection_status": "operator_review_required",
  "water_candidates": [
    { "value": "Eaux", "selected": true, "intended_use": "water body", "operator_note": "main river" }
  ],
  "borough_or_district_candidates": [
    { "value": "TestBorough", "selected": false, "intended_use": "", "operator_note": "" }
  ]
}
""";
        var selPath = Path.Combine(_root, "test-selection.json");
        File.WriteAllText(selPath, selectionJson, System.Text.Encoding.UTF8);

        var repoRoot   = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var cliProject = Path.Combine(repoRoot, "src", "PZMapForge.Cli");
        var palette    = Path.Combine(repoRoot, "source", "image-palette.json");

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "dotnet",
            WorkingDirectory       = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        foreach (var a in new[]
            { "run", "--project", cliProject, "--configuration", "Release", "--no-build", "--",
              "app-export", "--path", imgPath, "--palette", palette,
              "--output", OutputDir, "--svg-selection", selPath })
            psi.ArgumentList.Add(a);

        using var proc = System.Diagnostics.Process.Start(psi)!;
        proc.StandardOutput.ReadToEnd();
        proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        ExitCode = proc.ExitCode;

        var reviewPath = Path.Combine(OutputDir, "artifacts", "svg-layer-selection-review.json");
        SelectionReviewJson = File.Exists(reviewPath) ? File.ReadAllText(reviewPath) : string.Empty;

        var htmlPath = Path.Combine(OutputDir, "index.html");
        IndexHtml = File.Exists(htmlPath) ? File.ReadAllText(htmlPath) : string.Empty;
    }

    public bool InputCopied =>
        File.Exists(Path.Combine(OutputDir, "artifacts", "svg-layer-selection.input.json"));

    public bool ReviewExists =>
        File.Exists(Path.Combine(OutputDir, "artifacts", "svg-layer-selection-review.json"));

    public bool   ManifestJsonExists =>
        File.Exists(Path.Combine(OutputDir, "artifacts", "svg-planning-manifest.json"));
    public bool   ManifestMdExists =>
        File.Exists(Path.Combine(OutputDir, "artifacts", "svg-planning-manifest.md"));
    public string ManifestJson =>
        ManifestJsonExists ? File.ReadAllText(Path.Combine(OutputDir, "artifacts", "svg-planning-manifest.json")) : string.Empty;
    public string ManifestMd =>
        ManifestMdExists  ? File.ReadAllText(Path.Combine(OutputDir, "artifacts", "svg-planning-manifest.md"))   : string.Empty;

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); }
        catch { /* best effort */ }
    }
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class AppExportSelectionTests : IClassFixture<AppExportSelectionFixture>
{
    private readonly AppExportSelectionFixture _fix;
    public AppExportSelectionTests(AppExportSelectionFixture fix) => _fix = fix;

    [Fact]
    public void AppExport_Selection_ExitsZero() =>
        Assert.Equal(0, _fix.ExitCode);

    [Fact]
    public void AppExport_Selection_WritesInputCopy() =>
        Assert.True(_fix.InputCopied, "svg-layer-selection.input.json was not copied");

    [Fact]
    public void AppExport_Selection_WritesReviewJson() =>
        Assert.True(_fix.ReviewExists, "svg-layer-selection-review.json was not written");

    [Fact]
    public void AppExport_Selection_ReviewContainsSelectedCount() =>
        Assert.Contains("selected_count", _fix.SelectionReviewJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Selection_ReviewContainsSelectedValue() =>
        Assert.Contains("\"Eaux\"", _fix.SelectionReviewJson, StringComparison.Ordinal);

    [Fact]
    public void AppExport_Selection_ReviewContainsConvertedFalse() =>
        Assert.Contains("\"converted_to_map_geometry\": false", _fix.SelectionReviewJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Selection_IndexHtmlContainsSvgSelectionReview() =>
        Assert.Contains("SVG Selection Review", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Selection_IndexHtmlContainsMetadataOnlyNote() =>
        Assert.Contains("This review is metadata only", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Selection_IndexHtmlContainsNotExportedNote() =>
        Assert.Contains("not exported to Project Zomboid", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    // SVG-9: planning manifest

    [Fact]
    public void AppExport_Selection_WritesManifestJson() =>
        Assert.True(_fix.ManifestJsonExists, "svg-planning-manifest.json was not written");

    [Fact]
    public void AppExport_Selection_WritesManifestMd() =>
        Assert.True(_fix.ManifestMdExists, "svg-planning-manifest.md was not written");

    [Fact]
    public void AppExport_Selection_ManifestJsonContainsPlanningStatus() =>
        Assert.Contains("operator_selected_metadata_only", _fix.ManifestJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Selection_ManifestJsonContainsExportedFalse() =>
        Assert.Contains("\"exported_to_project_zomboid\": false", _fix.ManifestJson, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Selection_ManifestMdContainsTitle() =>
        Assert.Contains("SVG Planning Manifest", _fix.ManifestMd, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Selection_ManifestMdContainsNonClaim() =>
        Assert.Contains("No SVG geometry converted", _fix.ManifestMd, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Selection_IndexHtmlContainsSvgPlanningManifest() =>
        Assert.Contains("SVG Planning Manifest", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Selection_IndexHtmlContainsInertManifestNote() =>
        Assert.Contains("inert planning manifest", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    // SVG-10: visible manifest summary

    [Fact]
    public void AppExport_Selection_IndexHtmlManifestContainsSelectedCount() =>
        Assert.Contains("selected_count", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Selection_IndexHtmlManifestContainsPlanningStatus() =>
        Assert.Contains("operator_selected_metadata_only", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Selection_IndexHtmlManifestContainsSelectedValueEaux() =>
        Assert.Contains("Eaux", _fix.IndexHtml, StringComparison.Ordinal);

    [Fact]
    public void AppExport_Selection_IndexHtmlManifestContainsIntendedUseWaterBody() =>
        Assert.Contains("water body", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Selection_IndexHtmlManifestContainsNoSvgGeometryConverted() =>
        Assert.Contains("No SVG geometry converted", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Selection_IndexHtmlManifestContainsNoSvgCoordinatesExtracted() =>
        Assert.Contains("No SVG coordinates extracted", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void AppExport_Selection_IndexHtmlManifestContainsNoProjectZomboidExportGenerated() =>
        Assert.Contains("No Project Zomboid export generated", _fix.IndexHtml, StringComparison.OrdinalIgnoreCase);
}

// ---------------------------------------------------------------------------
// Zero-selected: does not fail, does not write manifest
// ---------------------------------------------------------------------------

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class AppExportZeroSelectionTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-zero-sel", Path.GetRandomFileName());

    public AppExportZeroSelectionTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string PalettePath =>
        Path.Combine(RepoRoot, "source", "image-palette.json");

    [Fact]
    public void AppExport_ZeroSelected_ExitsZeroAndNoManifest()
    {
        var imgPath = Path.Combine(_tempDir, "analysis.png");
        using (var bmp = new System.Drawing.Bitmap(300, 300))
        using (var g   = System.Drawing.Graphics.FromImage(bmp))
        {
            g.Clear(System.Drawing.Color.FromArgb(255, 100, 140, 70));
            bmp.Save(imgPath, System.Drawing.Imaging.ImageFormat.Png);
        }

        var selJson = """
{
  "schema": "pzmapforge.svg-layer-selection-template.v0.1",
  "water_candidates": [
    { "value": "Eaux", "selected": false, "intended_use": "", "operator_note": "" }
  ]
}
""";
        var selPath   = Path.Combine(_tempDir, "zero-selection.json");
        File.WriteAllText(selPath, selJson, System.Text.Encoding.UTF8);

        var outputDir = Path.Combine(_tempDir, ".local", "app");
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "dotnet",
            WorkingDirectory       = RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        foreach (var a in new[]
            { "run", "--project", Path.Combine(RepoRoot, "src", "PZMapForge.Cli"),
              "--configuration", "Release", "--no-build", "--",
              "app-export", "--path", imgPath, "--palette", PalettePath,
              "--output", outputDir, "--svg-selection", selPath })
            psi.ArgumentList.Add(a);

        using var proc = System.Diagnostics.Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        Assert.True(proc.ExitCode == 0,
            $"Exited {proc.ExitCode}. Stdout: {stdout}. Stderr: {stderr}");
        Assert.False(
            File.Exists(Path.Combine(outputDir, "artifacts", "svg-planning-manifest.json")),
            "svg-planning-manifest.json should not exist when selected_count == 0");
    }

    [Fact]
    public void AppExport_ZeroSelected_IndexHtmlContainsNoMetadataAvailable()
    {
        var imgPath = Path.Combine(_tempDir, "analysis2.png");
        using (var bmp = new System.Drawing.Bitmap(300, 300))
        using (var g   = System.Drawing.Graphics.FromImage(bmp))
        {
            g.Clear(System.Drawing.Color.FromArgb(255, 100, 140, 70));
            bmp.Save(imgPath, System.Drawing.Imaging.ImageFormat.Png);
        }

        var selJson = """
{
  "schema": "pzmapforge.svg-layer-selection-template.v0.1",
  "water_candidates": [
    { "value": "Eaux", "selected": false, "intended_use": "", "operator_note": "" }
  ]
}
""";
        var selPath   = Path.Combine(_tempDir, "zero-selection2.json");
        File.WriteAllText(selPath, selJson, System.Text.Encoding.UTF8);

        var outputDir = Path.Combine(_tempDir, ".local", "app2");
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "dotnet",
            WorkingDirectory       = RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        foreach (var a in new[]
            { "run", "--project", Path.Combine(RepoRoot, "src", "PZMapForge.Cli"),
              "--configuration", "Release", "--no-build", "--",
              "app-export", "--path", imgPath, "--palette", PalettePath,
              "--output", outputDir, "--svg-selection", selPath })
            psi.ArgumentList.Add(a);

        using var proc = System.Diagnostics.Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        Assert.True(proc.ExitCode == 0,
            $"Exited {proc.ExitCode}. Stdout: {stdout}. Stderr: {stderr}");

        var htmlPath = Path.Combine(outputDir, "index.html");
        Assert.True(File.Exists(htmlPath), "index.html was not written");
        var html = File.ReadAllText(htmlPath);
        Assert.Contains("no selected SVG metadata was available", html, StringComparison.OrdinalIgnoreCase);
    }
}
