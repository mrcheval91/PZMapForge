using System.Text;
using System.Text.Json;
using PZMapForge.Core.WorldGen;
using Xunit;

namespace PZMapForge.Core.Tests.WorldGen;

public sealed class DeadMtlWorldBuilderNeighborhoodProfileValidatorTests : IDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "pzmapforge-nb-profile", Path.GetRandomFileName());

    public DeadMtlWorldBuilderNeighborhoodProfileValidatorTests() =>
        Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true); }
        catch { /* best effort */ }
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static string BaselineProfile =>
        Path.Combine(RepoRoot, "examples", "deadmtl-layer-pack", "worldbuilder", "neighborhoods",
            "deadmtl_baseline_neighborhood_profile.json");

    private string WriteProfile(object profile)
    {
        var path = Path.Combine(_tempDir, Path.GetRandomFileName() + ".json");
        File.WriteAllText(path,
            JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true }),
            Encoding.UTF8);
        return path;
    }

    private static object BaselineObject() => new
    {
        format        = "pzmapforge.deadmtl.worldbuilder.neighborhood-profile.v1",
        profile_id    = "test_profile",
        display_name  = "Test Profile",
        era           = "1993",
        city          = "Montreal",
        profile_status = "AUTHORING_PROFILE_ONLY",
        sidewalk_policy = new
        {
            default_has_sidewalks      = true,
            sidewalk_source            = "INSIDE_STREET_ZONE",
            default_left_width_tiles   = 2,
            default_right_width_tiles  = 2,
            allow_asymmetric_sidewalks = true,
            allow_street_override      = true,
        },
        street_policy = new
        {
            default_corridor_width_tiles    = 12,
            default_lane_width_tiles        = 3,
            default_lane_count              = 2,
            parking_lane_left               = true,
            parking_lane_right              = true,
            tree_strip_width_tiles          = 0,
            snowbank_reserved_width_tiles   = 1,
            curb_style                      = "MONTREAL_CONCRETE",
            street_light_policy             = "MONTREAL_ORANGE_SODIUM",
        },
        zoning_policy = new
        {
            residential = new
            {
                default_intensity   = "MEDIUM",
                allowed_intensities = new[] { "LIGHT", "MEDIUM", "DENSE" },
                building_families   = new[] { "duplex", "triplex" },
                procedural_fill     = true,
            },
            commercial = new
            {
                default_intensity   = "MEDIUM",
                allowed_intensities = new[] { "LIGHT", "MEDIUM", "DENSE" },
                building_families   = new[] { "depanneur", "restaurant" },
                procedural_fill     = true,
            },
            industrial = new
            {
                default_intensity   = "LIGHT",
                allowed_intensities = new[] { "EMPTY", "LIGHT", "MEDIUM" },
                building_families   = new[] { "warehouse", "yard" },
                procedural_fill     = true,
            },
        },
        procedural_fill_policy = new
        {
            enabled                          = true,
            fill_missing_until_unique_override = true,
            unique_override_priority         = "UNIQUE_OVERRIDES_WIN",
            deterministic_seed_policy        = "PROFILE_PLUS_TILE_COORDINATE",
        },
        png_metadata_policy = new
        {
            supports_color_metadata_file     = true,
            zone_color_metadata_status       = "FUTURE_MAP25B",
            building_color_metadata_status   = "FUTURE_MAP25C",
            chunk_layer_capture_status       = "FUTURE",
        },
        claim_boundary = new
        {
            writes_lotpack             = false,
            writes_worldgen_lua        = false,
            runtime_proven             = false,
            public_playable_claim      = false,
            writer_ready_claim         = false,
            generates_buildings_now    = false,
            captures_chunk_layers_now  = false,
        },
    };

    // -----------------------------------------------------------------------
    // Input validation
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_ReturnsInvalid_WhenFileMissing()
    {
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(
            Path.Combine(_tempDir, "no_such_file.json"));
        Assert.False(result.IsValid);
        var check = result.Validation!.Checks.First(c => c.RuleId == "FILE_EXISTS");
        Assert.False(check.Passed);
    }

    // -----------------------------------------------------------------------
    // Baseline profile passes
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_ReturnsValid_ForBaselineProfile()
    {
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(BaselineProfile);
        Assert.True(result.IsValid,
            string.Join("; ", result.Validation!.Checks.Where(c => !c.Passed).Select(c => c.RuleId + ": " + c.Message)));
    }

    [Fact]
    public void Validate_Baseline_ProfileId_IsDeadmtlBaseline()
    {
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(BaselineProfile);
        Assert.Equal("deadmtl_baseline", result.Validation!.ProfileId);
    }

    // -----------------------------------------------------------------------
    // format check
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_Fails_WhenFormatWrong()
    {
        var obj  = new { format = "wrong.format", profile_id = "x" };
        var path = WriteProfile(obj);
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(path);
        Assert.False(result.IsValid);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "FORMAT_VALID").Passed);
    }

    // -----------------------------------------------------------------------
    // profile_id
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_Fails_WhenProfileIdEmpty()
    {
        var path = WriteProfile(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.neighborhood-profile.v1",
            profile_id = "",
        });
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(path);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "PROFILE_ID_NON_EMPTY").Passed);
    }

    // -----------------------------------------------------------------------
    // Sidewalk source
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_Fails_WhenSidewalkSourceInvalid()
    {
        var path = WriteProfile(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.neighborhood-profile.v1",
            profile_id = "x",
            sidewalk_policy = new { sidewalk_source = "BOGUS_SOURCE" },
        });
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(path);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "SIDEWALK_SOURCE_VALID").Passed);
    }

    [Fact]
    public void Validate_Fails_WhenSidewalkSourceNone_HasSidewalksTrue()
    {
        var path = WriteProfile(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.neighborhood-profile.v1",
            profile_id = "x",
            sidewalk_policy = new
            {
                default_has_sidewalks    = true,
                sidewalk_source          = "NONE",
                default_left_width_tiles = 2,
                default_right_width_tiles = 2,
            },
        });
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(path);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "SIDEWALK_NONE_REQUIRES_NO_SIDEWALKS").Passed);
    }

    [Fact]
    public void Validate_Fails_WhenHasSidewalksTrueButBothWidthsZero()
    {
        var path = WriteProfile(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.neighborhood-profile.v1",
            profile_id = "x",
            sidewalk_policy = new
            {
                default_has_sidewalks    = true,
                sidewalk_source          = "INSIDE_STREET_ZONE",
                default_left_width_tiles = 0,
                default_right_width_tiles = 0,
            },
        });
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(path);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "SIDEWALK_HAS_REQUIRES_WIDTH").Passed);
    }

    [Fact]
    public void Validate_Fails_WhenSidewalkWidthNegative()
    {
        var path = WriteProfile(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.neighborhood-profile.v1",
            profile_id = "x",
            sidewalk_policy = new
            {
                default_has_sidewalks    = false,
                sidewalk_source          = "INSIDE_STREET_ZONE",
                default_left_width_tiles = -1,
                default_right_width_tiles = 0,
            },
        });
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(path);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "SIDEWALK_LEFT_WIDTH_NON_NEGATIVE").Passed);
    }

    // -----------------------------------------------------------------------
    // Street policy
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_Fails_WhenCorridorWidthZero()
    {
        var path = WriteProfile(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.neighborhood-profile.v1",
            profile_id = "x",
            street_policy = new
            {
                default_corridor_width_tiles = 0,
                default_lane_width_tiles     = 3,
                default_lane_count           = 2,
            },
        });
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(path);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "STREET_CORRIDOR_WIDTH_POSITIVE").Passed);
    }

    // -----------------------------------------------------------------------
    // Intensity
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_Fails_WhenIntensityInvalid()
    {
        var path = WriteProfile(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.neighborhood-profile.v1",
            profile_id = "x",
            zoning_policy = new
            {
                residential = new { default_intensity = "BOGUS", allowed_intensities = new string[0], building_families = new string[0] },
            },
        });
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(path);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "RESIDENTIAL_INTENSITY_VALID").Passed);
    }

    // -----------------------------------------------------------------------
    // Missing policies
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_Fails_WhenResidentialPolicyMissing()
    {
        var path = WriteProfile(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.neighborhood-profile.v1",
            profile_id = "x",
            zoning_policy = new { commercial = new { default_intensity = "LIGHT", allowed_intensities = new string[0], building_families = new string[0] } },
        });
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(path);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "RESIDENTIAL_POLICY_EXISTS").Passed);
    }

    [Fact]
    public void Validate_Fails_WhenProceduralFillPolicyMissing()
    {
        var path = WriteProfile(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.neighborhood-profile.v1",
            profile_id = "x",
        });
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(path);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "PROCEDURAL_FILL_POLICY_EXISTS").Passed);
    }

    // -----------------------------------------------------------------------
    // Claim boundary
    // -----------------------------------------------------------------------

    [Fact]
    public void Validate_Fails_WhenClaimBoundaryTrueValue()
    {
        var path = WriteProfile(new
        {
            format     = "pzmapforge.deadmtl.worldbuilder.neighborhood-profile.v1",
            profile_id = "x",
            claim_boundary = new { writes_lotpack = true, generates_buildings_now = false, captures_chunk_layers_now = false },
        });
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(path);
        Assert.False(result.Validation!.Checks.First(c => c.RuleId == "CLAIM_WRITES_LOTPACK_FALSE").Passed);
    }

    // -----------------------------------------------------------------------
    // Markdown
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderMarkdown_ContainsMap25ATitle()
    {
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(BaselineProfile);
        var md     = DeadMtlWorldBuilderNeighborhoodProfileValidator.RenderMarkdown(result.Validation!);
        Assert.Contains("MAP-25A", md, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderMarkdown_ContainsSidewalkModelSection()
    {
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(BaselineProfile);
        var md     = DeadMtlWorldBuilderNeighborhoodProfileValidator.RenderMarkdown(result.Validation!);
        Assert.True(
            md.Contains("Sidewalk Model", StringComparison.OrdinalIgnoreCase) ||
            md.Contains("sidewalk_source", StringComparison.OrdinalIgnoreCase),
            "markdown should explain the sidewalk model");
    }

    [Fact]
    public void RenderMarkdown_ContainsZoningIntensityScale()
    {
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(BaselineProfile);
        var md     = DeadMtlWorldBuilderNeighborhoodProfileValidator.RenderMarkdown(result.Validation!);
        Assert.True(
            md.Contains("CONDENSED", StringComparison.Ordinal) &&
            md.Contains("CROWDED",   StringComparison.Ordinal),
            "markdown should include full intensity scale");
    }

    [Fact]
    public void RenderMarkdown_ContainsVerdict()
    {
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(BaselineProfile);
        var md     = DeadMtlWorldBuilderNeighborhoodProfileValidator.RenderMarkdown(result.Validation!);
        Assert.Contains("MAP25A_WORLDBUILDER_NEIGHBORHOOD_PROFILE_CONTRACT_COMPLETE",
            md, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // CSV header
    // -----------------------------------------------------------------------

    [Fact]
    public void RenderCsv_ContainsRequiredHeader()
    {
        var result = DeadMtlWorldBuilderNeighborhoodProfileValidator.Validate(BaselineProfile);
        var csv    = DeadMtlWorldBuilderNeighborhoodProfileValidator.RenderCsv(result.Validation!);
        Assert.Contains("rule_id,severity,passed,message", csv, StringComparison.Ordinal);
    }
}
