using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderBucketToTilesheetCrossReferenceBuilderTests
{
    private static string TempDir() =>
        Path.Combine(Path.GetTempPath(), "pzmapforge-map36f-core-test.local", Path.GetRandomFileName());

    private static string MakePzFixture(string baseDir)
    {
        string tilesDir = Path.Combine(baseDir, "pz-fixture", "media", "tiles");
        Directory.CreateDirectory(tilesDir);
        File.WriteAllText(Path.Combine(tilesDir, "location_walls_01.txt"),
            "wall\nlocation_walls_exterior_01\nlocation_walls_interior_01\nwalls_concrete_01");
        File.WriteAllText(Path.Combine(tilesDir, "floors_exterior_01.txt"),
            "floor\nfloors_exterior_concrete_01\nfloors_interior_01\npavement");
        File.WriteAllText(Path.Combine(tilesDir, "vegetation_01.txt"),
            "grass\ntree\nbush\nvegetation_plants_01");
        File.WriteAllText(Path.Combine(tilesDir, "location_roads.txt"),
            "road\nstreet\nasphalt\npavement\nconcrete");
        return Path.Combine(baseDir, "pz-fixture");
    }

    [Fact]
    public void ValidBuild_NoPzInstall_EmitsBucketIntentInventory()
    {
        var dir = TempDir();
        string outRoot = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderBucketToTilesheetCrossReferenceBuilder();
        var result  = builder.Build(null, null, null, null, outRoot);

        Assert.True(result.IsValid,
            string.Join("; ", result.Checks.Where(c => c.CheckStatus == "FAIL").Select(c => c.CheckId)));
        Assert.Equal(8, result.BucketIntents.Count);
        Assert.Contains(result.BucketIntents, b => b.Bucket == "WALL");
        Assert.Contains(result.BucketIntents, b => b.Bucket == "FLOOR");
        Assert.Contains(result.BucketIntents, b => b.Bucket == "ROAD");
        Assert.Contains(result.BucketIntents, b => b.Bucket == "VEGETATION");
    }

    [Fact]
    public void ValidBuild_WithPzFixture_TileSourceInventoryHasEntries()
    {
        var dir    = TempDir();
        string pz  = MakePzFixture(dir);
        string out_ = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderBucketToTilesheetCrossReferenceBuilder();
        var result  = builder.Build(null, null, null, pz, out_);

        Assert.True(result.PzInstallFound);
        Assert.NotEmpty(result.TileSourceInventory);
        Assert.All(result.TileSourceInventory, s => Assert.Equal("local_pz_install", s.SourceType));
    }

    [Fact]
    public void ValidBuild_CandidateMappings_EmittedForKeyBuckets()
    {
        var dir    = TempDir();
        string pz  = MakePzFixture(dir);
        string out_ = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderBucketToTilesheetCrossReferenceBuilder();
        var result  = builder.Build(null, null, null, pz, out_);

        Assert.NotEmpty(result.BucketToTilesheetCandidates);
        Assert.Contains(result.BucketToTilesheetCandidates, c => c.Bucket == "WALL");
        Assert.Contains(result.BucketToTilesheetCandidates, c => c.Bucket == "FLOOR");
        Assert.Contains(result.BucketToTilesheetCandidates, c => c.Bucket == "ROAD");
        Assert.Contains(result.BucketToTilesheetCandidates, c => c.Bucket == "VEGETATION");
    }

    [Fact]
    public void ValidBuild_AllCandidates_HaveVerifiedRuntimeTileIdFalse()
    {
        var dir    = TempDir();
        string pz  = MakePzFixture(dir);
        string out_ = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderBucketToTilesheetCrossReferenceBuilder();
        var result  = builder.Build(null, null, null, pz, out_);

        Assert.All(result.BucketToTilesheetCandidates,
            c => Assert.False(c.VerifiedRuntimeTileId));
        Assert.False(result.VerifiedRuntimeTileId);
        Assert.True(result.ReadOnlyProbe);
    }

    [Fact]
    public void ValidBuild_ClaimBoundary_AllFalse()
    {
        var dir    = TempDir();
        string out_ = Path.Combine(dir, "out.local");

        var builder = new DeadMtlWorldBuilderBucketToTilesheetCrossReferenceBuilder();
        var result  = builder.Build(null, null, null, null, out_);

        Assert.False(result.RuntimeBinaryWritten);
        Assert.False(result.GeometryInjected);
        Assert.False(result.PlayableExportClaimed);
        Assert.False(result.WorkshopUploadPerformed);
        Assert.False(result.SteamInstallWrite);
        Assert.False(result.VerifiedRuntimeTileId);
    }

    [Fact]
    public void Build_OutputRootMissingLocal_CheckFails()
    {
        var dir    = Path.Combine(Path.GetTempPath(), "pzmapforge-map36f-nosandbox-" + Path.GetRandomFileName());
        string out_ = Path.Combine(dir, "out-no-sandbox");

        var builder = new DeadMtlWorldBuilderBucketToTilesheetCrossReferenceBuilder();
        var result  = builder.Build(null, null, null, null, out_);

        var check = result.Checks.Single(c => c.CheckId == "MAP36F_OUTPUT_ROOT_CONTAINS_LOCAL");
        Assert.Equal("FAIL", check.CheckStatus);
    }
}
