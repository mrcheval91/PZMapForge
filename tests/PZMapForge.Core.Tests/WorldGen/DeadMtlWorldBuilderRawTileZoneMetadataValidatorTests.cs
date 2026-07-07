using System.Text;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderRawTileZoneMetadataValidatorTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-zone-meta", Path.GetRandomFileName());

    public DeadMtlWorldBuilderRawTileZoneMetadataValidatorTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string Map00MetadataPath =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "worldbuilder", "tiles",
            "map_00.zone_metadata.json");

    private static string ProfilePath =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "worldbuilder", "neighborhoods",
            "deadmtl_baseline_neighborhood_profile.json");

    // All 7 standard colors from map_00.png
    private static readonly string[] Map00Colors =
    {
        "#7200FF", "#FF6600", "#00AA10", "#F000FF", "#B2BD87", "#CE0000", "#000000"
    };

    private string WriteInspection(IEnumerable<string> colors)
    {
        var path     = Path.Combine(_tempDir, Path.GetRandomFileName() + "_inspection.json");
        var entries  = colors.Select(c => new { color = c, count = 100 });
        var json     = JsonSerializer.Serialize(new
        {
            format     = "pzmapforge.deadmtl.raw-map-tile-inspection.v1",
            top_colors = entries,
        }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private string WriteMap00Inspection() => WriteInspection(Map00Colors);

    private string WriteMetadata(object obj)
    {
        var path = Path.Combine(_tempDir, Path.GetRandomFileName() + ".json");
        File.WriteAllText(path,
            JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }),
            Encoding.UTF8);
        return path;
    }

    private DeadMtlWorldBuilderRawTileZoneMetadataValidationResult RunReal() =>
        DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(
            Map00MetadataPath, WriteMap00Inspection(), ProfilePath);

    // -----------------------------------------------------------------------
    // File existence
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_ReturnsInvalid_WhenMetadataMissing()
    {
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(
            Path.Combine(_tempDir, "no_such.json"),
            WriteMap00Inspection(),
            ProfilePath);
        Assert.False(result.IsValid);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "METADATA_FILE_EXISTS").Passed);
    }

    [Fact]
    public void Validate_ReturnsInvalid_WhenInspectionMissing()
    {
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(
            Map00MetadataPath,
            Path.Combine(_tempDir, "no_inspection.json"),
            ProfilePath);
        Assert.False(result.IsValid);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "INSPECTION_JSON_EXISTS").Passed);
    }

    [Fact]
    public void Validate_ReturnsInvalid_WhenProfileMissing()
    {
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(
            Map00MetadataPath,
            WriteMap00Inspection(),
            Path.Combine(_tempDir, "no_profile.json"));
        Assert.False(result.IsValid);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "NEIGHBORHOOD_PROFILE_EXISTS").Passed);
    }

    // -----------------------------------------------------------------------
    // Valid baseline
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_ReturnsValid_ForMap00Metadata()
    {
        var result = RunReal();
        Assert.True(result.IsValid,
            string.Join("; ", result.Validation!.Checks.Where(c => !c.Passed)
                .Select(c => c.RuleId + ": " + c.Message)));
    }

    // -----------------------------------------------------------------------
    // Format
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_Fails_WhenFormatWrong()
    {
        var path = WriteMetadata(new { format = "wrong.format", tile_id = "map_00" });
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(
            path, WriteMap00Inspection(), ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "FORMAT_VALID").Passed);
    }

    // -----------------------------------------------------------------------
    // Color coverage
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_Fails_WhenInspectionColorMissingFromMetadata()
    {
        // Inspection has 7 colors; metadata only covers 6 (missing #F000FF)
        var inspection = WriteInspection(Map00Colors);
        var meta = WriteMetadata(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id    = "test_tile",
            neighborhood_profile_id = "deadmtl_baseline",
            color_roles = new object[]
            {
                new { color = "#7200FF", role = "ZONE",            zone_type = "RESIDENTIAL", development_intensity = "MEDIUM", street_class = "", sidewalk_eligible = false, sidewalk_policy_source = "", procedural_fill = true,  unique_override_allowed = true  },
                new { color = "#FF6600", role = "STREET_CORRIDOR", zone_type = "TRANSPORT",   development_intensity = "",       street_class = "MAIN_ROAD", sidewalk_eligible = true,  sidewalk_policy_source = "NEIGHBORHOOD_PROFILE", procedural_fill = false, unique_override_allowed = true  },
                new { color = "#00AA10", role = "ZONE",            zone_type = "GREENSPACE",  development_intensity = "LIGHT",  street_class = "", sidewalk_eligible = false, sidewalk_policy_source = "", procedural_fill = true,  unique_override_allowed = true  },
                new { color = "#B2BD87", role = "UNIQUE_PLACEHOLDER", zone_type = "CIVIC_SPECIAL_BUILDING", development_intensity = "", street_class = "", sidewalk_eligible = false, sidewalk_policy_source = "", procedural_fill = false, unique_override_allowed = true  },
                new { color = "#42CCFF", role = "ZONE",            zone_type = "COMMERCIAL",  development_intensity = "MEDIUM", street_class = "", sidewalk_eligible = false, sidewalk_policy_source = "", procedural_fill = true,  unique_override_allowed = true  },
                new { color = "#000000", role = "IGNORE",          zone_type = "VOID_OR_BORDER", development_intensity = "", street_class = "", sidewalk_eligible = false, sidewalk_policy_source = "", procedural_fill = false, unique_override_allowed = false },
                // #F000FF is intentionally absent
            },
            claim_boundary = new { writes_lotpack = false, writes_worldgen_lua = false, runtime_proven = false, public_playable_claim = false, writer_ready_claim = false, generates_buildings_now = false, generates_sidewalks_now = false, subdivides_lots_now = false, captures_chunk_layers_now = false },
        });
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(meta, inspection, ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "ALL_INSPECTION_COLORS_IN_METADATA").Passed);
    }

    [Fact]
    public void Validate_Fails_WhenMetadataColorAbsentFromInspection()
    {
        // Inspection has 6 colors (missing #F000FF); metadata covers 7
        var inspection = WriteInspection(Map00Colors.Where(c => c != "#F000FF"));
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(
            Map00MetadataPath, inspection, ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "ALL_METADATA_COLORS_IN_INSPECTION").Passed);
    }

    [Fact]
    public void Validate_Fails_WhenDuplicateColor()
    {
        var inspection = WriteInspection(new[] { "#7200FF", "#FF6600" });
        var meta = WriteMetadata(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id    = "test_tile",
            neighborhood_profile_id = "deadmtl_baseline",
            color_roles = new object[]
            {
                new { color = "#7200FF", role = "ZONE", zone_type = "RESIDENTIAL", development_intensity = "MEDIUM", street_class = "", sidewalk_eligible = false, sidewalk_policy_source = "", procedural_fill = true, unique_override_allowed = true },
                new { color = "#7200FF", role = "ZONE", zone_type = "RESIDENTIAL", development_intensity = "MEDIUM", street_class = "", sidewalk_eligible = false, sidewalk_policy_source = "", procedural_fill = true, unique_override_allowed = true },
                new { color = "#FF6600", role = "STREET_CORRIDOR", zone_type = "TRANSPORT", development_intensity = "", street_class = "MAIN_ROAD", sidewalk_eligible = true, sidewalk_policy_source = "NEIGHBORHOOD_PROFILE", procedural_fill = false, unique_override_allowed = true },
            },
            claim_boundary = new { writes_lotpack = false, writes_worldgen_lua = false, runtime_proven = false, public_playable_claim = false, writer_ready_claim = false, generates_buildings_now = false, generates_sidewalks_now = false, subdivides_lots_now = false, captures_chunk_layers_now = false },
        });
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(meta, inspection, ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "NO_DUPLICATE_COLORS").Passed);
    }

    // -----------------------------------------------------------------------
    // Role / street class
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_Fails_WhenRoleInvalid()
    {
        var inspection = WriteInspection(new[] { "#7200FF" });
        var meta = WriteMetadata(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id    = "test_tile",
            neighborhood_profile_id = "x",
            color_roles = new object[]
            {
                new { color = "#7200FF", role = "BOGUS_ROLE", zone_type = "RESIDENTIAL", development_intensity = "", street_class = "", sidewalk_eligible = false, sidewalk_policy_source = "", procedural_fill = false, unique_override_allowed = false },
            },
            claim_boundary = new { writes_lotpack = false, writes_worldgen_lua = false, runtime_proven = false, public_playable_claim = false, writer_ready_claim = false, generates_buildings_now = false, generates_sidewalks_now = false, subdivides_lots_now = false, captures_chunk_layers_now = false },
        });
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(meta, inspection, ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "ALL_ROLES_VALID").Passed);
    }

    [Fact]
    public void Validate_Fails_WhenStreetCorridorMissingStreetClass()
    {
        var inspection = WriteInspection(new[] { "#FF6600" });
        var meta = WriteMetadata(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id    = "test_tile",
            neighborhood_profile_id = "x",
            color_roles = new object[]
            {
                new { color = "#FF6600", role = "STREET_CORRIDOR", zone_type = "TRANSPORT", development_intensity = "", street_class = "", sidewalk_eligible = true, sidewalk_policy_source = "NEIGHBORHOOD_PROFILE", procedural_fill = false, unique_override_allowed = true },
            },
            claim_boundary = new { writes_lotpack = false, writes_worldgen_lua = false, runtime_proven = false, public_playable_claim = false, writer_ready_claim = false, generates_buildings_now = false, generates_sidewalks_now = false, subdivides_lots_now = false, captures_chunk_layers_now = false },
        });
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(meta, inspection, ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "STREET_CORRIDOR_HAS_STREET_CLASS").Passed);
    }

    // -----------------------------------------------------------------------
    // Sidewalk eligibility
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_Fails_WhenMainRoadSidewalkEligibleFalse()
    {
        var inspection = WriteInspection(new[] { "#FF6600" });
        var meta = WriteMetadata(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id    = "test_tile",
            neighborhood_profile_id = "x",
            color_roles = new object[]
            {
                new { color = "#FF6600", role = "STREET_CORRIDOR", zone_type = "TRANSPORT", development_intensity = "", street_class = "MAIN_ROAD", sidewalk_eligible = false, sidewalk_policy_source = "", procedural_fill = false, unique_override_allowed = true },
            },
            claim_boundary = new { writes_lotpack = false, writes_worldgen_lua = false, runtime_proven = false, public_playable_claim = false, writer_ready_claim = false, generates_buildings_now = false, generates_sidewalks_now = false, subdivides_lots_now = false, captures_chunk_layers_now = false },
        });
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(meta, inspection, ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "MAIN_ROAD_SIDEWALK_ELIGIBLE_TRUE").Passed);
    }

    [Fact]
    public void Validate_Fails_WhenBackAlleySidewalkEligibleTrue()
    {
        var inspection = WriteInspection(new[] { "#F000FF" });
        var meta = WriteMetadata(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id    = "test_tile",
            neighborhood_profile_id = "x",
            color_roles = new object[]
            {
                new { color = "#F000FF", role = "STREET_CORRIDOR", zone_type = "TRANSPORT", development_intensity = "", street_class = "BACK_ALLEY", sidewalk_eligible = true, sidewalk_policy_source = "NEIGHBORHOOD_PROFILE", procedural_fill = false, unique_override_allowed = true },
            },
            claim_boundary = new { writes_lotpack = false, writes_worldgen_lua = false, runtime_proven = false, public_playable_claim = false, writer_ready_claim = false, generates_buildings_now = false, generates_sidewalks_now = false, subdivides_lots_now = false, captures_chunk_layers_now = false },
        });
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(meta, inspection, ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "BACK_ALLEY_SIDEWALK_ELIGIBLE_FALSE").Passed);
    }

    [Fact]
    public void Validate_Fails_WhenSidewalkEligibleTrueWithoutNeighborhoodProfile()
    {
        var inspection = WriteInspection(new[] { "#FF6600" });
        var meta = WriteMetadata(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id    = "test_tile",
            neighborhood_profile_id = "x",
            color_roles = new object[]
            {
                new { color = "#FF6600", role = "STREET_CORRIDOR", zone_type = "TRANSPORT", development_intensity = "", street_class = "MAIN_ROAD", sidewalk_eligible = true, sidewalk_policy_source = "SOME_OTHER_SOURCE", procedural_fill = false, unique_override_allowed = true },
            },
            claim_boundary = new { writes_lotpack = false, writes_worldgen_lua = false, runtime_proven = false, public_playable_claim = false, writer_ready_claim = false, generates_buildings_now = false, generates_sidewalks_now = false, subdivides_lots_now = false, captures_chunk_layers_now = false },
        });
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(meta, inspection, ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "SIDEWALK_ELIGIBLE_REQUIRES_NEIGHBORHOOD_PROFILE").Passed);
    }

    [Fact]
    public void Validate_Fails_WhenNonStreetIsSidewalkEligible()
    {
        var inspection = WriteInspection(new[] { "#7200FF" });
        var meta = WriteMetadata(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id    = "test_tile",
            neighborhood_profile_id = "x",
            color_roles = new object[]
            {
                new { color = "#7200FF", role = "ZONE", zone_type = "RESIDENTIAL", development_intensity = "MEDIUM", street_class = "", sidewalk_eligible = true, sidewalk_policy_source = "NEIGHBORHOOD_PROFILE", procedural_fill = false, unique_override_allowed = true },
            },
            claim_boundary = new { writes_lotpack = false, writes_worldgen_lua = false, runtime_proven = false, public_playable_claim = false, writer_ready_claim = false, generates_buildings_now = false, generates_sidewalks_now = false, subdivides_lots_now = false, captures_chunk_layers_now = false },
        });
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(meta, inspection, ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "NON_STREET_NOT_SIDEWALK_ELIGIBLE").Passed);
    }

    // -----------------------------------------------------------------------
    // Intensity
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_Fails_WhenIntensityInvalid()
    {
        var inspection = WriteInspection(new[] { "#7200FF" });
        var meta = WriteMetadata(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id    = "test_tile",
            neighborhood_profile_id = "x",
            color_roles = new object[]
            {
                new { color = "#7200FF", role = "ZONE", zone_type = "RESIDENTIAL", development_intensity = "BOGUS_LEVEL", street_class = "", sidewalk_eligible = false, sidewalk_policy_source = "", procedural_fill = false, unique_override_allowed = false },
            },
            claim_boundary = new { writes_lotpack = false, writes_worldgen_lua = false, runtime_proven = false, public_playable_claim = false, writer_ready_claim = false, generates_buildings_now = false, generates_sidewalks_now = false, subdivides_lots_now = false, captures_chunk_layers_now = false },
        });
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(meta, inspection, ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "ALL_INTENSITIES_VALID").Passed);
    }

    // -----------------------------------------------------------------------
    // Color normalization guards
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_Passes_When00AA10PresentExactly()
    {
        var result = RunReal();
        Assert.True(result.Validation!.Checks.First(c => c.RuleId == "NO_COLOR_NORMALIZE_00AA10").Passed);
    }

    [Fact]
    public void Validate_Fails_When00AA00UsedInsteadOf00AA10()
    {
        // Replace #00AA10 with #00AA00 in both inspection and metadata
        var wrongColors = Map00Colors.Select(c => c == "#00AA10" ? "#00AA00" : c).ToArray();
        var inspection  = WriteInspection(wrongColors);
        // Build a metadata that uses #00AA00 instead of #00AA10
        var meta = WriteMetadata(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id    = "test_tile",
            neighborhood_profile_id = "x",
            color_roles = new object[]
            {
                new { color = "#7200FF", role = "ZONE",            zone_type = "RESIDENTIAL",    development_intensity = "MEDIUM", street_class = "",          sidewalk_eligible = false, sidewalk_policy_source = "",                   procedural_fill = true,  unique_override_allowed = true  },
                new { color = "#FF6600", role = "STREET_CORRIDOR", zone_type = "TRANSPORT",      development_intensity = "",       street_class = "MAIN_ROAD", sidewalk_eligible = true,  sidewalk_policy_source = "NEIGHBORHOOD_PROFILE", procedural_fill = false, unique_override_allowed = true  },
                new { color = "#00AA00", role = "ZONE",            zone_type = "GREENSPACE",     development_intensity = "LIGHT",  street_class = "",          sidewalk_eligible = false, sidewalk_policy_source = "",                   procedural_fill = true,  unique_override_allowed = true  },
                new { color = "#F000FF", role = "STREET_CORRIDOR", zone_type = "TRANSPORT",      development_intensity = "",       street_class = "BACK_ALLEY",sidewalk_eligible = false, sidewalk_policy_source = "",                   procedural_fill = false, unique_override_allowed = true  },
                new { color = "#B2BD87", role = "UNIQUE_PLACEHOLDER", zone_type = "CIVIC_SPECIAL_BUILDING", development_intensity = "", street_class = "", sidewalk_eligible = false, sidewalk_policy_source = "", procedural_fill = false, unique_override_allowed = true },
                new { color = "#42CCFF", role = "ZONE",            zone_type = "COMMERCIAL",     development_intensity = "MEDIUM", street_class = "",          sidewalk_eligible = false, sidewalk_policy_source = "",                   procedural_fill = true,  unique_override_allowed = true  },
                new { color = "#000000", role = "IGNORE",          zone_type = "VOID_OR_BORDER", development_intensity = "",       street_class = "",          sidewalk_eligible = false, sidewalk_policy_source = "",                   procedural_fill = false, unique_override_allowed = false },
            },
            claim_boundary = new { writes_lotpack = false, writes_worldgen_lua = false, runtime_proven = false, public_playable_claim = false, writer_ready_claim = false, generates_buildings_now = false, generates_sidewalks_now = false, subdivides_lots_now = false, captures_chunk_layers_now = false },
        });
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(meta, inspection, ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "NO_COLOR_NORMALIZE_00AA10").Passed);
    }

    [Fact]
    public void Validate_Passes_WhenF000FFPresentExactly()
    {
        var result = RunReal();
        Assert.True(result.Validation!.Checks.First(c => c.RuleId == "NO_COLOR_NORMALIZE_F000FF").Passed);
    }

    [Fact]
    public void Validate_Fails_WhenFF00FFUsedInsteadOfF000FF()
    {
        var wrongColors = Map00Colors.Select(c => c == "#F000FF" ? "#FF00FF" : c).ToArray();
        var inspection  = WriteInspection(wrongColors);
        var meta = WriteMetadata(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id    = "test_tile",
            neighborhood_profile_id = "x",
            color_roles = new object[]
            {
                new { color = "#7200FF", role = "ZONE",            zone_type = "RESIDENTIAL",    development_intensity = "MEDIUM", street_class = "",           sidewalk_eligible = false, sidewalk_policy_source = "",                   procedural_fill = true,  unique_override_allowed = true  },
                new { color = "#FF6600", role = "STREET_CORRIDOR", zone_type = "TRANSPORT",      development_intensity = "",       street_class = "MAIN_ROAD",  sidewalk_eligible = true,  sidewalk_policy_source = "NEIGHBORHOOD_PROFILE", procedural_fill = false, unique_override_allowed = true  },
                new { color = "#00AA10", role = "ZONE",            zone_type = "GREENSPACE",     development_intensity = "LIGHT",  street_class = "",           sidewalk_eligible = false, sidewalk_policy_source = "",                   procedural_fill = true,  unique_override_allowed = true  },
                new { color = "#FF00FF", role = "STREET_CORRIDOR", zone_type = "TRANSPORT",      development_intensity = "",       street_class = "BACK_ALLEY", sidewalk_eligible = false, sidewalk_policy_source = "",                   procedural_fill = false, unique_override_allowed = true  },
                new { color = "#B2BD87", role = "UNIQUE_PLACEHOLDER", zone_type = "CIVIC_SPECIAL_BUILDING", development_intensity = "", street_class = "", sidewalk_eligible = false, sidewalk_policy_source = "", procedural_fill = false, unique_override_allowed = true },
                new { color = "#42CCFF", role = "ZONE",            zone_type = "COMMERCIAL",     development_intensity = "MEDIUM", street_class = "",           sidewalk_eligible = false, sidewalk_policy_source = "",                   procedural_fill = true,  unique_override_allowed = true  },
                new { color = "#000000", role = "IGNORE",          zone_type = "VOID_OR_BORDER", development_intensity = "",       street_class = "",           sidewalk_eligible = false, sidewalk_policy_source = "",                   procedural_fill = false, unique_override_allowed = false },
            },
            claim_boundary = new { writes_lotpack = false, writes_worldgen_lua = false, runtime_proven = false, public_playable_claim = false, writer_ready_claim = false, generates_buildings_now = false, generates_sidewalks_now = false, subdivides_lots_now = false, captures_chunk_layers_now = false },
        });
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(meta, inspection, ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "NO_COLOR_NORMALIZE_F000FF").Passed);
    }

    // -----------------------------------------------------------------------
    // Baseline color checks
    // -----------------------------------------------------------------------

    [Fact] public void Validate_Baseline_7200FF_IsResidential()   => AssertBaselinePasses("BASELINE_7200FF_RESIDENTIAL");
    [Fact] public void Validate_Baseline_FF6600_IsMainRoad()       => AssertBaselinePasses("BASELINE_FF6600_MAIN_ROAD");
    [Fact] public void Validate_Baseline_F000FF_IsBackAlley()      => AssertBaselinePasses("BASELINE_F000FF_BACK_ALLEY");
    [Fact] public void Validate_Baseline_CE0000_IsCommercial()     => AssertBaselinePasses("BASELINE_CE0000_COMMERCIAL");
    [Fact] public void Validate_Baseline_00AA10_IsGreenspace()     => AssertBaselinePasses("BASELINE_00AA10_GREENSPACE");
    [Fact] public void Validate_Baseline_B2BD87_IsCivicSpecial()   => AssertBaselinePasses("BASELINE_B2BD87_CIVIC_SPECIAL");
    [Fact] public void Validate_Baseline_000000_IsIgnore()         => AssertBaselinePasses("BASELINE_000000_IGNORE");

    private void AssertBaselinePasses(string ruleId)
    {
        var result = RunReal();
        var check  = result.Validation!.Checks.FirstOrDefault(c => c.RuleId == ruleId);
        Assert.NotNull(check);
        Assert.True(check!.Passed, $"{ruleId}: {check.Message}");
    }

    // -----------------------------------------------------------------------
    // Lot subdivision policy
    // -----------------------------------------------------------------------

    private object ResidentialRoleWithNoLotPolicy() => new
    {
        color = "#7200FF", role = "ZONE", zone_type = "RESIDENTIAL", development_intensity = "MEDIUM",
        street_class = "", sidewalk_eligible = false, sidewalk_policy_source = "",
        procedural_fill = false, unique_override_allowed = false,
        lot_subdivision_policy = (object?)null,
    };

    [Fact]
    public void Validate_Fails_WhenResidentialLotSubdivisionPolicyMissing()
    {
        var inspection = WriteInspection(new[] { "#7200FF" });
        var meta = WriteMetadata(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id    = "test_tile",
            neighborhood_profile_id = "x",
            color_roles = new object[] { ResidentialRoleWithNoLotPolicy() },
            claim_boundary = new { writes_lotpack = false, writes_worldgen_lua = false, runtime_proven = false, public_playable_claim = false, writer_ready_claim = false, generates_buildings_now = false, generates_sidewalks_now = false, subdivides_lots_now = false, captures_chunk_layers_now = false },
        });
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(meta, inspection, ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "RESIDENTIAL_LOT_SUBDIVISION_ENABLED_LATER").Passed);
    }

    [Fact]
    public void Validate_Fails_WhenResidentialPreferredFrontageNotMainRoad()
    {
        var inspection = WriteInspection(new[] { "#7200FF" });
        var meta = WriteMetadata(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id    = "test_tile",
            neighborhood_profile_id = "x",
            color_roles = new object[]
            {
                new { color = "#7200FF", role = "ZONE", zone_type = "RESIDENTIAL", development_intensity = "MEDIUM",
                      street_class = "", sidewalk_eligible = false, sidewalk_policy_source = "",
                      procedural_fill = false, unique_override_allowed = false,
                      lot_subdivision_policy = new { enabled_later = true, future_status = "X", preferred_frontage = "BACK_ALLEY",
                          avoid_frontage = new[] { "BACK_ALLEY" }, facade_orientation_policy = "ROW_UNIFORM_FRONTAGE",
                          if_single_main_road_frontage = "", lot_line_fence_policy = "FUTURE_LAYER" } },
            },
            claim_boundary = new { writes_lotpack = false, writes_worldgen_lua = false, runtime_proven = false, public_playable_claim = false, writer_ready_claim = false, generates_buildings_now = false, generates_sidewalks_now = false, subdivides_lots_now = false, captures_chunk_layers_now = false },
        });
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(meta, inspection, ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "RESIDENTIAL_PREFERRED_FRONTAGE_MAIN_ROAD").Passed);
    }

    [Fact]
    public void Validate_Fails_WhenResidentialAvoidFrontageDoesNotIncludeBackAlley()
    {
        var inspection = WriteInspection(new[] { "#7200FF" });
        var meta = WriteMetadata(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id    = "test_tile",
            neighborhood_profile_id = "x",
            color_roles = new object[]
            {
                new { color = "#7200FF", role = "ZONE", zone_type = "RESIDENTIAL", development_intensity = "MEDIUM",
                      street_class = "", sidewalk_eligible = false, sidewalk_policy_source = "",
                      procedural_fill = false, unique_override_allowed = false,
                      lot_subdivision_policy = new { enabled_later = true, future_status = "X", preferred_frontage = "MAIN_ROAD",
                          avoid_frontage = new string[0], facade_orientation_policy = "ROW_UNIFORM_FRONTAGE",
                          if_single_main_road_frontage = "", lot_line_fence_policy = "FUTURE_LAYER" } },
            },
            claim_boundary = new { writes_lotpack = false, writes_worldgen_lua = false, runtime_proven = false, public_playable_claim = false, writer_ready_claim = false, generates_buildings_now = false, generates_sidewalks_now = false, subdivides_lots_now = false, captures_chunk_layers_now = false },
        });
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(meta, inspection, ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "RESIDENTIAL_AVOID_FRONTAGE_INCLUDES_BACK_ALLEY").Passed);
    }

    [Fact]
    public void Validate_Fails_WhenResidentialFacadeOrientationNotRowUniform()
    {
        var inspection = WriteInspection(new[] { "#7200FF" });
        var meta = WriteMetadata(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id    = "test_tile",
            neighborhood_profile_id = "x",
            color_roles = new object[]
            {
                new { color = "#7200FF", role = "ZONE", zone_type = "RESIDENTIAL", development_intensity = "MEDIUM",
                      street_class = "", sidewalk_eligible = false, sidewalk_policy_source = "",
                      procedural_fill = false, unique_override_allowed = false,
                      lot_subdivision_policy = new { enabled_later = true, future_status = "X", preferred_frontage = "MAIN_ROAD",
                          avoid_frontage = new[] { "BACK_ALLEY" }, facade_orientation_policy = "RANDOM",
                          if_single_main_road_frontage = "", lot_line_fence_policy = "FUTURE_LAYER" } },
            },
            claim_boundary = new { writes_lotpack = false, writes_worldgen_lua = false, runtime_proven = false, public_playable_claim = false, writer_ready_claim = false, generates_buildings_now = false, generates_sidewalks_now = false, subdivides_lots_now = false, captures_chunk_layers_now = false },
        });
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(meta, inspection, ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "RESIDENTIAL_FACADE_ORIENTATION_ROW_UNIFORM").Passed);
    }

    [Fact]
    public void Validate_Fails_WhenCommercialLotSubdivisionPolicyMissing()
    {
        var inspection = WriteInspection(new[] { "#42CCFF" });
        var meta = WriteMetadata(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id    = "test_tile",
            neighborhood_profile_id = "x",
            color_roles = new object[]
            {
                new { color = "#42CCFF", role = "ZONE", zone_type = "COMMERCIAL", development_intensity = "MEDIUM",
                      street_class = "", sidewalk_eligible = false, sidewalk_policy_source = "",
                      procedural_fill = false, unique_override_allowed = false,
                      lot_subdivision_policy = (object?)null },
            },
            claim_boundary = new { writes_lotpack = false, writes_worldgen_lua = false, runtime_proven = false, public_playable_claim = false, writer_ready_claim = false, generates_buildings_now = false, generates_sidewalks_now = false, subdivides_lots_now = false, captures_chunk_layers_now = false },
        });
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(meta, inspection, ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "COMMERCIAL_LOT_SUBDIVISION_ENABLED_LATER").Passed);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_Fails_WhenClaimBoundaryTrueValue()
    {
        var inspection = WriteInspection(new[] { "#000000" });
        var meta = WriteMetadata(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1",
            tile_id    = "test_tile",
            neighborhood_profile_id = "x",
            color_roles = new object[]
            {
                new { color = "#000000", role = "IGNORE", zone_type = "VOID_OR_BORDER", development_intensity = "", street_class = "", sidewalk_eligible = false, sidewalk_policy_source = "", procedural_fill = false, unique_override_allowed = false },
            },
            claim_boundary = new { writes_lotpack = true, writes_worldgen_lua = false, runtime_proven = false, public_playable_claim = false, writer_ready_claim = false, generates_buildings_now = false, generates_sidewalks_now = false, subdivides_lots_now = false, captures_chunk_layers_now = false },
        });
        var result = DeadMtlWorldBuilderRawTileZoneMetadataValidator.Validate(meta, inspection, ProfilePath);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "CLAIM_WRITES_LOTPACK_FALSE").Passed);
    }

    // -----------------------------------------------------------------------
    // Markdown
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderMarkdown_ContainsAllRequiredSections()
    {
        var result = RunReal();
        var md     = DeadMtlWorldBuilderRawTileZoneMetadataValidator.RenderMarkdown(result.Validation!);

        Assert.Contains("MAP-25B", md, StringComparison.Ordinal);
        Assert.True(md.Contains("Sidewalk", StringComparison.OrdinalIgnoreCase), "missing sidewalk section");
        Assert.True(md.Contains("Back Alley", StringComparison.OrdinalIgnoreCase), "missing back alley section");
        Assert.True(md.Contains("Lot Subdivision", StringComparison.OrdinalIgnoreCase) ||
                    md.Contains("lot_subdivision", StringComparison.OrdinalIgnoreCase), "missing lot subdivision");
        Assert.True(md.Contains("Frontage", StringComparison.OrdinalIgnoreCase) ||
                    md.Contains("preferred_frontage", StringComparison.OrdinalIgnoreCase), "missing frontage");
        Assert.True(md.Contains("Civic", StringComparison.OrdinalIgnoreCase) ||
                    md.Contains("CIVIC_SPECIAL_BUILDING", StringComparison.Ordinal), "missing civic placeholder");
        Assert.True(md.Contains("Fence", StringComparison.OrdinalIgnoreCase) ||
                    md.Contains("Cloture", StringComparison.OrdinalIgnoreCase), "missing fences/clotures");
        Assert.True(md.Contains("Procedural Fill", StringComparison.OrdinalIgnoreCase) ||
                    md.Contains("procedural_fill", StringComparison.OrdinalIgnoreCase), "missing procedural fill");
        Assert.True(md.Contains("Unique Override", StringComparison.OrdinalIgnoreCase) ||
                    md.Contains("unique_override_allowed", StringComparison.OrdinalIgnoreCase), "missing unique override");
        Assert.Contains("MAP25B_WORLDBUILDER_RAW_TILE_ZONE_METADATA_CONTRACT_COMPLETE",
            md, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV header
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCsv_ContainsRequiredHeader()
    {
        var result = RunReal();
        var csv    = DeadMtlWorldBuilderRawTileZoneMetadataValidator.RenderCsv(result.Validation!);
        Assert.Contains("rule_id,severity,passed,message", csv, StringComparison.Ordinal);
    }
}
