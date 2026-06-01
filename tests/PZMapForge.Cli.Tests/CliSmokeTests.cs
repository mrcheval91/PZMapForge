using PZMapForge.Core.ImageParsing;
using PZMapForge.Core.Planning;
using PZMapForge.Core.Primitives;
using Xunit;

namespace PZMapForge.Cli.Tests;

// Smoke tests confirming Core types used by CLI commands are accessible and correct.
// Full process-level CLI integration tests are deferred to a later slice.
public sealed class CliSmokeTests
{
    [Fact]
    public void CliBuildAssembly_IsPresent()
    {
        var asm = typeof(PZMapForge.Core.Palette.PaletteLoader).Assembly;
        Assert.NotNull(asm);
        Assert.Contains("PZMapForge.Core", asm.FullName, StringComparison.Ordinal);
    }

    [Fact]
    public void PrimitiveClassifier_IsAccessible()
    {
        Assert.True(PrimitiveClassifier.IsKnownKind("grass"));
        Assert.True(PrimitiveClassifier.IsKnownKind("spawn"));
        Assert.False(PrimitiveClassifier.IsKnownKind("unknown_kind"));
        Assert.Equal(7, Enum.GetValues<PlanningPrimitiveType>().Length);
    }

    // -----------------------------------------------------------------------
    // PlanningRuleOptions — covers the threshold parsing logic used by
    // plan-check and plan-export CLI commands.
    // -----------------------------------------------------------------------

    [Fact]
    public void ThresholdParsing_DefaultOptions_HasExpectedValues()
    {
        Assert.Equal(9,      PlanningRuleOptions.Default.TinyBuildingPixelThreshold);
        Assert.Equal(50_000, PlanningRuleOptions.Default.LargeGroundPixelThreshold);
    }

    [Fact]
    public void ThresholdParsing_CustomValues_AreAccepted()
    {
        var opts = new PlanningRuleOptions(tinyBuildingPixelThreshold: 0, largeGroundPixelThreshold: 100_000);
        Assert.Equal(0,       opts.TinyBuildingPixelThreshold);
        Assert.Equal(100_000, opts.LargeGroundPixelThreshold);
    }

    [Fact]
    public void ThresholdParsing_ZeroTiny_IsValid()
    {
        var opts = new PlanningRuleOptions(tinyBuildingPixelThreshold: 0);
        Assert.Equal(0, opts.TinyBuildingPixelThreshold);
    }

    [Fact]
    public void ThresholdParsing_NegativeTiny_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PlanningRuleOptions(tinyBuildingPixelThreshold: -1));
    }

    [Fact]
    public void ThresholdParsing_NegativeLarge_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PlanningRuleOptions(largeGroundPixelThreshold: -1));
    }

    // -----------------------------------------------------------------------
    // ImageMapForgeOptions -- covers the type surface used by image-check
    // -----------------------------------------------------------------------

    [Fact]
    public void ImageMapForgeOptions_DefaultResize_IsFalse()
    {
        Assert.False(ImageMapForgeOptions.Default.Resize);
    }

    [Fact]
    public void ImageMapForgeOptions_ResizeTrue_CanBeConstructed()
    {
        Assert.True(new ImageMapForgeOptions { Resize = true }.Resize);
    }

    [Fact]
    public void ImageMapForgeParser_IsAccessibleFromCli()
    {
        var t = typeof(ImageMapForgeParser);
        Assert.Equal("ImageMapForgeParser", t.Name);
    }
}
