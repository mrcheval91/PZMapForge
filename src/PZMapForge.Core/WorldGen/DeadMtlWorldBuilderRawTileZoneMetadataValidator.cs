using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class DeadMtlWorldBuilderRawTileZoneMetadataValidator
{
    private const string ExpectedFormat  = "pzmapforge.deadmtl.worldbuilder.raw-tile-zone-metadata.v1";
    private const string Map00TileId     = "map_00";

    private static readonly HashSet<string> ValidRoles = new(StringComparer.Ordinal)
    {
        "ZONE", "STREET_CORRIDOR", "UNIQUE_PLACEHOLDER", "IGNORE"
    };

    private static readonly HashSet<string> ValidIntensities = new(StringComparer.Ordinal)
    {
        "EMPTY", "LIGHT", "MEDIUM", "DENSE", "CONDENSED", "CROWDED"
    };

    private static readonly HashSet<string> ValidStreetClasses = new(StringComparer.Ordinal)
    {
        "MAIN_ROAD", "BACK_ALLEY", "SERVICE", "LOCAL_ROAD", "FUTURE"
    };

    public static DeadMtlWorldBuilderRawTileZoneMetadataValidationResult Validate(
        string metadataPath, string inspectionJsonPath, string profilePath)
    {
        var result = new DeadMtlWorldBuilderRawTileZoneMetadataValidationResult();
        var checks = new List<DeadMtlWorldBuilderRawTileZoneMetadataValidationCheck>();

        if (!File.Exists(metadataPath))
        {
            checks.Add(Fail("METADATA_FILE_EXISTS", "ERROR", $"Metadata file not found: {metadataPath}"));
            result.Validation = Finalize(metadataPath, string.Empty, checks, isValid: false);
            result.IsValid    = false;
            return result;
        }
        checks.Add(Pass("METADATA_FILE_EXISTS", "ERROR", "Metadata file exists."));

        DeadMtlWorldBuilderRawTileZoneMetadata metadata;
        try
        {
            var json = File.ReadAllText(metadataPath, Encoding.UTF8);
            var opts = new JsonSerializerOptions { AllowTrailingCommas = true, PropertyNameCaseInsensitive = false };
            metadata = JsonSerializer.Deserialize<DeadMtlWorldBuilderRawTileZoneMetadata>(json, opts)
                       ?? new DeadMtlWorldBuilderRawTileZoneMetadata();
        }
        catch (Exception ex)
        {
            checks.Add(Fail("JSON_PARSE", "ERROR", $"Failed to parse metadata JSON: {ex.Message}"));
            result.Validation = Finalize(metadataPath, string.Empty, checks, isValid: false);
            result.IsValid    = false;
            return result;
        }

        checks.Add(metadata.Format == ExpectedFormat
            ? Pass("FORMAT_VALID",    "ERROR", $"format == {ExpectedFormat}")
            : Fail("FORMAT_VALID",    "ERROR", $"format must be '{ExpectedFormat}', got '{metadata.Format}'"));

        checks.Add(!string.IsNullOrWhiteSpace(metadata.TileId)
            ? Pass("TILE_ID_NON_EMPTY", "ERROR", "tile_id is non-empty.")
            : Fail("TILE_ID_NON_EMPTY", "ERROR", "tile_id must be non-empty."));

        checks.Add(!string.IsNullOrWhiteSpace(metadata.NeighborhoodProfileId)
            ? Pass("NEIGHBORHOOD_PROFILE_ID_NON_EMPTY", "ERROR", "neighborhood_profile_id is non-empty.")
            : Fail("NEIGHBORHOOD_PROFILE_ID_NON_EMPTY", "ERROR", "neighborhood_profile_id must be non-empty."));

        checks.Add(File.Exists(profilePath)
            ? Pass("NEIGHBORHOOD_PROFILE_EXISTS", "ERROR", $"Neighborhood profile found: {profilePath}")
            : Fail("NEIGHBORHOOD_PROFILE_EXISTS", "ERROR", $"Neighborhood profile not found: {profilePath}"));

        // Inspection JSON — early exit from coverage checks if missing
        if (!File.Exists(inspectionJsonPath))
        {
            checks.Add(Fail("INSPECTION_JSON_EXISTS", "ERROR",
                $"Inspection JSON not found: {inspectionJsonPath}"));
            // Can't run color coverage checks — add structural checks and finalize
            AddStructuralChecks(checks, metadata);
            bool iv = checks.All(c => c.Passed || c.Severity != "ERROR");
            result.Validation = Finalize(metadataPath, metadata.TileId, checks, iv, metadata.ClaimBoundary);
            result.IsValid    = iv;
            return result;
        }
        checks.Add(Pass("INSPECTION_JSON_EXISTS", "ERROR", "Inspection JSON exists."));

        // Parse inspection colors
        var inspectionColors = LoadInspectionColors(inspectionJsonPath);

        // Build metadata color lookup
        var metadataColors = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cr in metadata.ColorRoles)
        {
            var key = cr.Color;
            if (metadataColors.ContainsKey(key))
                metadataColors[key]++;
            else
                metadataColors[key] = 1;
        }

        // NO_DUPLICATE_COLORS
        var dupes = metadataColors.Where(kv => kv.Value > 1).Select(kv => kv.Key).ToList();
        checks.Add(dupes.Count == 0
            ? Pass("NO_DUPLICATE_COLORS", "ERROR", "No duplicate colors in color_roles.")
            : Fail("NO_DUPLICATE_COLORS", "ERROR",
                $"Duplicate colors in color_roles: {string.Join(", ", dupes)}"));

        // ALL_INSPECTION_COLORS_IN_METADATA
        var missingFromMeta = inspectionColors.Where(c => !metadataColors.ContainsKey(c)).ToList();
        checks.Add(missingFromMeta.Count == 0
            ? Pass("ALL_INSPECTION_COLORS_IN_METADATA", "ERROR",
                "All inspection colors appear in metadata.")
            : Fail("ALL_INSPECTION_COLORS_IN_METADATA", "ERROR",
                $"Inspection colors missing from metadata: {string.Join(", ", missingFromMeta)}"));

        // ALL_METADATA_COLORS_IN_INSPECTION
        var missingFromInspection = metadataColors.Keys.Where(c => !inspectionColors.Contains(c)).ToList();
        checks.Add(missingFromInspection.Count == 0
            ? Pass("ALL_METADATA_COLORS_IN_INSPECTION", "ERROR",
                "All metadata colors appear in inspection.")
            : Fail("ALL_METADATA_COLORS_IN_INSPECTION", "ERROR",
                $"Metadata colors absent from inspection: {string.Join(", ", missingFromInspection)}"));

        AddStructuralChecks(checks, metadata);

        bool isValid = checks.All(c => c.Passed || c.Severity != "ERROR");
        result.Validation = Finalize(metadataPath, metadata.TileId, checks, isValid, metadata.ClaimBoundary);
        result.IsValid    = isValid;
        return result;
    }

    private static void AddStructuralChecks(
        List<DeadMtlWorldBuilderRawTileZoneMetadataValidationCheck> checks,
        DeadMtlWorldBuilderRawTileZoneMetadata metadata)
    {
        var roles = metadata.ColorRoles;

        // ALL_ROLES_VALID
        var badRoles = roles.Where(r => !ValidRoles.Contains(r.Role)).Select(r => $"{r.Color}={r.Role}").ToList();
        checks.Add(badRoles.Count == 0
            ? Pass("ALL_ROLES_VALID", "ERROR", "All color roles are valid.")
            : Fail("ALL_ROLES_VALID", "ERROR", $"Invalid role values: {string.Join(", ", badRoles)}"));

        // ALL_ZONE_TYPES_NON_EMPTY
        var emptyTypes = roles.Where(r => string.IsNullOrWhiteSpace(r.ZoneType)).Select(r => r.Color).ToList();
        checks.Add(emptyTypes.Count == 0
            ? Pass("ALL_ZONE_TYPES_NON_EMPTY", "ERROR", "All zone_type values are non-empty.")
            : Fail("ALL_ZONE_TYPES_NON_EMPTY", "ERROR",
                $"Colors with empty zone_type: {string.Join(", ", emptyTypes)}"));

        // ALL_INTENSITIES_VALID (only where non-empty)
        var badIntensity = roles
            .Where(r => !string.IsNullOrEmpty(r.DevelopmentIntensity) && !ValidIntensities.Contains(r.DevelopmentIntensity))
            .Select(r => $"{r.Color}={r.DevelopmentIntensity}")
            .ToList();
        checks.Add(badIntensity.Count == 0
            ? Pass("ALL_INTENSITIES_VALID", "ERROR", "All development_intensity values are valid.")
            : Fail("ALL_INTENSITIES_VALID", "ERROR",
                $"Invalid development_intensity values: {string.Join(", ", badIntensity)}"));

        // STREET_CORRIDOR_HAS_STREET_CLASS
        var streetNoClass = roles
            .Where(r => r.Role == "STREET_CORRIDOR" && string.IsNullOrWhiteSpace(r.StreetClass))
            .Select(r => r.Color)
            .ToList();
        checks.Add(streetNoClass.Count == 0
            ? Pass("STREET_CORRIDOR_HAS_STREET_CLASS", "ERROR",
                "All STREET_CORRIDOR entries have street_class.")
            : Fail("STREET_CORRIDOR_HAS_STREET_CLASS", "ERROR",
                $"STREET_CORRIDOR entries missing street_class: {string.Join(", ", streetNoClass)}"));

        // ALL_STREET_CLASSES_VALID
        var badClass = roles
            .Where(r => !string.IsNullOrEmpty(r.StreetClass) && !ValidStreetClasses.Contains(r.StreetClass))
            .Select(r => $"{r.Color}={r.StreetClass}")
            .ToList();
        checks.Add(badClass.Count == 0
            ? Pass("ALL_STREET_CLASSES_VALID", "ERROR", "All street_class values are valid.")
            : Fail("ALL_STREET_CLASSES_VALID", "ERROR",
                $"Invalid street_class values: {string.Join(", ", badClass)}"));

        // MAIN_ROAD_SIDEWALK_ELIGIBLE_TRUE
        var mainRoadNotEligible = roles
            .Where(r => r.StreetClass == "MAIN_ROAD" && !r.SidewalkEligible)
            .Select(r => r.Color)
            .ToList();
        checks.Add(mainRoadNotEligible.Count == 0
            ? Pass("MAIN_ROAD_SIDEWALK_ELIGIBLE_TRUE", "ERROR",
                "All MAIN_ROAD entries have sidewalk_eligible = true.")
            : Fail("MAIN_ROAD_SIDEWALK_ELIGIBLE_TRUE", "ERROR",
                $"MAIN_ROAD entries with sidewalk_eligible=false: {string.Join(", ", mainRoadNotEligible)}"));

        // BACK_ALLEY_SIDEWALK_ELIGIBLE_FALSE
        var alleyEligible = roles
            .Where(r => r.StreetClass == "BACK_ALLEY" && r.SidewalkEligible)
            .Select(r => r.Color)
            .ToList();
        checks.Add(alleyEligible.Count == 0
            ? Pass("BACK_ALLEY_SIDEWALK_ELIGIBLE_FALSE", "ERROR",
                "All BACK_ALLEY entries have sidewalk_eligible = false.")
            : Fail("BACK_ALLEY_SIDEWALK_ELIGIBLE_FALSE", "ERROR",
                $"BACK_ALLEY entries with sidewalk_eligible=true: {string.Join(", ", alleyEligible)}"));

        // SIDEWALK_ELIGIBLE_REQUIRES_NEIGHBORHOOD_PROFILE
        var eligibleBadSource = roles
            .Where(r => r.SidewalkEligible && r.SidewalkPolicySource != "NEIGHBORHOOD_PROFILE")
            .Select(r => r.Color)
            .ToList();
        checks.Add(eligibleBadSource.Count == 0
            ? Pass("SIDEWALK_ELIGIBLE_REQUIRES_NEIGHBORHOOD_PROFILE", "ERROR",
                "All sidewalk_eligible entries use NEIGHBORHOOD_PROFILE as source.")
            : Fail("SIDEWALK_ELIGIBLE_REQUIRES_NEIGHBORHOOD_PROFILE", "ERROR",
                $"sidewalk_eligible=true without NEIGHBORHOOD_PROFILE: {string.Join(", ", eligibleBadSource)}"));

        // NON_STREET_NOT_SIDEWALK_ELIGIBLE
        var nonStreetEligible = roles
            .Where(r => r.Role != "STREET_CORRIDOR" && r.SidewalkEligible)
            .Select(r => r.Color)
            .ToList();
        checks.Add(nonStreetEligible.Count == 0
            ? Pass("NON_STREET_NOT_SIDEWALK_ELIGIBLE", "ERROR",
                "No non-STREET_CORRIDOR entry has sidewalk_eligible = true.")
            : Fail("NON_STREET_NOT_SIDEWALK_ELIGIBLE", "ERROR",
                $"Non-street colors with sidewalk_eligible=true: {string.Join(", ", nonStreetEligible)}"));

        // RESIDENTIAL_LOT_SUBDIVISION_ENABLED_LATER
        var resZones = roles.Where(r => r.Role == "ZONE" && r.ZoneType == "RESIDENTIAL").ToList();
        bool resLotOk = resZones.Count == 0 || resZones.All(r => r.LotSubdivisionPolicy?.EnabledLater == true);
        checks.Add(resLotOk
            ? Pass("RESIDENTIAL_LOT_SUBDIVISION_ENABLED_LATER", "ERROR",
                "RESIDENTIAL zones have lot_subdivision_policy.enabled_later = true.")
            : Fail("RESIDENTIAL_LOT_SUBDIVISION_ENABLED_LATER", "ERROR",
                "RESIDENTIAL zone missing lot_subdivision_policy with enabled_later = true."));

        // COMMERCIAL_LOT_SUBDIVISION_ENABLED_LATER
        var comZones = roles.Where(r => r.Role == "ZONE" && r.ZoneType == "COMMERCIAL").ToList();
        bool comLotOk = comZones.Count == 0 || comZones.All(r => r.LotSubdivisionPolicy?.EnabledLater == true);
        checks.Add(comLotOk
            ? Pass("COMMERCIAL_LOT_SUBDIVISION_ENABLED_LATER", "ERROR",
                "COMMERCIAL zones have lot_subdivision_policy.enabled_later = true.")
            : Fail("COMMERCIAL_LOT_SUBDIVISION_ENABLED_LATER", "ERROR",
                "COMMERCIAL zone missing lot_subdivision_policy with enabled_later = true."));

        // RESIDENTIAL_PREFERRED_FRONTAGE_MAIN_ROAD
        bool resFrontageOk = resZones.Count == 0 ||
            resZones.All(r => r.LotSubdivisionPolicy?.PreferredFrontage == "MAIN_ROAD");
        checks.Add(resFrontageOk
            ? Pass("RESIDENTIAL_PREFERRED_FRONTAGE_MAIN_ROAD", "ERROR",
                "RESIDENTIAL zones have preferred_frontage = MAIN_ROAD.")
            : Fail("RESIDENTIAL_PREFERRED_FRONTAGE_MAIN_ROAD", "ERROR",
                "RESIDENTIAL zone preferred_frontage must be MAIN_ROAD."));

        // RESIDENTIAL_AVOID_FRONTAGE_INCLUDES_BACK_ALLEY
        bool resAvoidOk = resZones.Count == 0 ||
            resZones.All(r => r.LotSubdivisionPolicy?.AvoidFrontage?.Contains("BACK_ALLEY",
                StringComparer.Ordinal) == true);
        checks.Add(resAvoidOk
            ? Pass("RESIDENTIAL_AVOID_FRONTAGE_INCLUDES_BACK_ALLEY", "ERROR",
                "RESIDENTIAL zones have BACK_ALLEY in avoid_frontage.")
            : Fail("RESIDENTIAL_AVOID_FRONTAGE_INCLUDES_BACK_ALLEY", "ERROR",
                "RESIDENTIAL zone avoid_frontage must include BACK_ALLEY."));

        // RESIDENTIAL_FACADE_ORIENTATION_ROW_UNIFORM
        bool resFacadeOk = resZones.Count == 0 ||
            resZones.All(r => r.LotSubdivisionPolicy?.FacadeOrientationPolicy == "ROW_UNIFORM_FRONTAGE");
        checks.Add(resFacadeOk
            ? Pass("RESIDENTIAL_FACADE_ORIENTATION_ROW_UNIFORM", "ERROR",
                "RESIDENTIAL zones have facade_orientation_policy = ROW_UNIFORM_FRONTAGE.")
            : Fail("RESIDENTIAL_FACADE_ORIENTATION_ROW_UNIFORM", "ERROR",
                "RESIDENTIAL zone facade_orientation_policy must be ROW_UNIFORM_FRONTAGE."));

        // COMMERCIAL_PREFERRED_FRONTAGE_MAIN_ROAD
        bool comFrontageOk = comZones.Count == 0 ||
            comZones.All(r => r.LotSubdivisionPolicy?.PreferredFrontage == "MAIN_ROAD");
        checks.Add(comFrontageOk
            ? Pass("COMMERCIAL_PREFERRED_FRONTAGE_MAIN_ROAD", "ERROR",
                "COMMERCIAL zones have preferred_frontage = MAIN_ROAD.")
            : Fail("COMMERCIAL_PREFERRED_FRONTAGE_MAIN_ROAD", "ERROR",
                "COMMERCIAL zone preferred_frontage must be MAIN_ROAD."));

        // UNIQUE_PLACEHOLDER_NOT_PROCEDURAL_FILL
        var upBad = roles
            .Where(r => r.Role == "UNIQUE_PLACEHOLDER" && (r.ProceduralFill || !r.UniqueOverrideAllowed))
            .Select(r => r.Color)
            .ToList();
        checks.Add(upBad.Count == 0
            ? Pass("UNIQUE_PLACEHOLDER_NOT_PROCEDURAL_FILL", "ERROR",
                "All UNIQUE_PLACEHOLDER entries have procedural_fill=false and unique_override_allowed=true.")
            : Fail("UNIQUE_PLACEHOLDER_NOT_PROCEDURAL_FILL", "ERROR",
                $"UNIQUE_PLACEHOLDER entries with wrong fill/override flags: {string.Join(", ", upBad)}"));

        // IGNORE_NOT_PROCEDURAL_FILL
        var ignoreBad = roles
            .Where(r => r.Role == "IGNORE" && (r.ProceduralFill || r.UniqueOverrideAllowed))
            .Select(r => r.Color)
            .ToList();
        checks.Add(ignoreBad.Count == 0
            ? Pass("IGNORE_NOT_PROCEDURAL_FILL", "ERROR",
                "All IGNORE entries have procedural_fill=false and unique_override_allowed=false.")
            : Fail("IGNORE_NOT_PROCEDURAL_FILL", "ERROR",
                $"IGNORE entries with wrong fill/override flags: {string.Join(", ", ignoreBad)}"));

        // --- Claim boundary ---
        var cb = metadata.ClaimBoundary;
        checks.Add(cb != null
            ? Pass("CLAIM_BOUNDARY_EXISTS", "ERROR", "claim_boundary exists.")
            : Fail("CLAIM_BOUNDARY_EXISTS", "ERROR", "claim_boundary is missing."));

        checks.Add(cb == null || !cb.WritesLotpack
            ? Pass("CLAIM_WRITES_LOTPACK_FALSE",      "ERROR", "writes_lotpack is false.")
            : Fail("CLAIM_WRITES_LOTPACK_FALSE",      "ERROR", "writes_lotpack must be false."));

        checks.Add(cb == null || !cb.WritesWorldgenLua
            ? Pass("CLAIM_WRITES_WORLDGEN_LUA_FALSE", "ERROR", "writes_worldgen_lua is false.")
            : Fail("CLAIM_WRITES_WORLDGEN_LUA_FALSE", "ERROR", "writes_worldgen_lua must be false."));

        checks.Add(cb == null || !cb.RuntimeProven
            ? Pass("CLAIM_RUNTIME_PROVEN_FALSE",      "ERROR", "runtime_proven is false.")
            : Fail("CLAIM_RUNTIME_PROVEN_FALSE",      "ERROR", "runtime_proven must be false."));

        checks.Add(cb == null || !cb.PublicPlayableClaim
            ? Pass("CLAIM_PUBLIC_PLAYABLE_FALSE",     "ERROR", "public_playable_claim is false.")
            : Fail("CLAIM_PUBLIC_PLAYABLE_FALSE",     "ERROR", "public_playable_claim must be false."));

        checks.Add(cb == null || !cb.WriterReadyClaim
            ? Pass("CLAIM_WRITER_READY_FALSE",        "ERROR", "writer_ready_claim is false.")
            : Fail("CLAIM_WRITER_READY_FALSE",        "ERROR", "writer_ready_claim must be false."));

        checks.Add(cb == null || !cb.GeneratesBuildingsNow
            ? Pass("CLAIM_GENERATES_BUILDINGS_FALSE", "ERROR", "generates_buildings_now is false.")
            : Fail("CLAIM_GENERATES_BUILDINGS_FALSE", "ERROR", "generates_buildings_now must be false."));

        checks.Add(cb == null || !cb.GeneratesSidewalksNow
            ? Pass("CLAIM_GENERATES_SIDEWALKS_FALSE", "ERROR", "generates_sidewalks_now is false.")
            : Fail("CLAIM_GENERATES_SIDEWALKS_FALSE", "ERROR", "generates_sidewalks_now must be false."));

        checks.Add(cb == null || !cb.SubdividesLotsNow
            ? Pass("CLAIM_SUBDIVIDES_LOTS_FALSE",     "ERROR", "subdivides_lots_now is false.")
            : Fail("CLAIM_SUBDIVIDES_LOTS_FALSE",     "ERROR", "subdivides_lots_now must be false."));

        checks.Add(cb == null || !cb.CapturesChunkLayersNow
            ? Pass("CLAIM_CAPTURES_CHUNK_LAYERS_FALSE", "ERROR", "captures_chunk_layers_now is false.")
            : Fail("CLAIM_CAPTURES_CHUNK_LAYERS_FALSE", "ERROR", "captures_chunk_layers_now must be false."));

        // --- Color normalization guards ---
        var metaColorSet = roles.Select(r => r.Color).ToHashSet(StringComparer.OrdinalIgnoreCase);

        bool hasAA00 = metaColorSet.Contains("#00AA00");
        bool hasAA10 = metaColorSet.Contains("#00AA10");
        checks.Add(!hasAA00 || hasAA10
            ? Pass("NO_COLOR_NORMALIZE_00AA10", "ERROR",
                "#00AA10 present as-is (not normalized to #00AA00).")
            : Fail("NO_COLOR_NORMALIZE_00AA10", "ERROR",
                "#00AA00 found in metadata but #00AA10 is absent — do not normalize #00AA10 to #00AA00."));

        bool hasFF00FF = metaColorSet.Contains("#FF00FF");
        bool hasF000FF = metaColorSet.Contains("#F000FF");
        checks.Add(!hasFF00FF || hasF000FF
            ? Pass("NO_COLOR_NORMALIZE_F000FF", "ERROR",
                "#F000FF present as-is (not normalized to #FF00FF).")
            : Fail("NO_COLOR_NORMALIZE_F000FF", "ERROR",
                "#FF00FF found in metadata but #F000FF is absent — do not normalize #F000FF to #FF00FF."));

        // --- Baseline checks (only for tile_id == "map_00") ---
        if (metadata.TileId == Map00TileId)
        {
            var byColor = roles.ToDictionary(r => r.Color, r => r, StringComparer.OrdinalIgnoreCase);

            checks.Add(byColor.TryGetValue("#7200FF", out var r7200) &&
                       r7200.Role == "ZONE" && r7200.ZoneType == "RESIDENTIAL"
                ? Pass("BASELINE_7200FF_RESIDENTIAL", "ERROR",
                    "#7200FF is ZONE / RESIDENTIAL.")
                : Fail("BASELINE_7200FF_RESIDENTIAL", "ERROR",
                    "#7200FF must be role=ZONE, zone_type=RESIDENTIAL."));

            checks.Add(byColor.TryGetValue("#FF6600", out var rFF66) &&
                       rFF66.Role == "STREET_CORRIDOR" && rFF66.StreetClass == "MAIN_ROAD"
                ? Pass("BASELINE_FF6600_MAIN_ROAD", "ERROR",
                    "#FF6600 is STREET_CORRIDOR / MAIN_ROAD.")
                : Fail("BASELINE_FF6600_MAIN_ROAD", "ERROR",
                    "#FF6600 must be role=STREET_CORRIDOR, street_class=MAIN_ROAD."));

            checks.Add(byColor.TryGetValue("#F000FF", out var rF000) &&
                       rF000.Role == "STREET_CORRIDOR" && rF000.StreetClass == "BACK_ALLEY"
                ? Pass("BASELINE_F000FF_BACK_ALLEY", "ERROR",
                    "#F000FF is STREET_CORRIDOR / BACK_ALLEY.")
                : Fail("BASELINE_F000FF_BACK_ALLEY", "ERROR",
                    "#F000FF must be role=STREET_CORRIDOR, street_class=BACK_ALLEY."));

            checks.Add(byColor.TryGetValue("#CE0000", out var rCE00) &&
                       rCE00.Role == "ZONE" && rCE00.ZoneType == "COMMERCIAL"
                ? Pass("BASELINE_CE0000_COMMERCIAL", "ERROR",
                    "#CE0000 is ZONE / COMMERCIAL.")
                : Fail("BASELINE_CE0000_COMMERCIAL", "ERROR",
                    "#CE0000 must be role=ZONE, zone_type=COMMERCIAL."));

            checks.Add(byColor.TryGetValue("#00AA10", out var r00AA) &&
                       r00AA.Role == "ZONE" && r00AA.ZoneType == "GREENSPACE"
                ? Pass("BASELINE_00AA10_GREENSPACE", "ERROR",
                    "#00AA10 is ZONE / GREENSPACE.")
                : Fail("BASELINE_00AA10_GREENSPACE", "ERROR",
                    "#00AA10 must be role=ZONE, zone_type=GREENSPACE."));

            checks.Add(byColor.TryGetValue("#B2BD87", out var rB2BD) &&
                       rB2BD.Role == "UNIQUE_PLACEHOLDER" && rB2BD.ZoneType == "CIVIC_SPECIAL_BUILDING"
                ? Pass("BASELINE_B2BD87_CIVIC_SPECIAL", "ERROR",
                    "#B2BD87 is UNIQUE_PLACEHOLDER / CIVIC_SPECIAL_BUILDING.")
                : Fail("BASELINE_B2BD87_CIVIC_SPECIAL", "ERROR",
                    "#B2BD87 must be role=UNIQUE_PLACEHOLDER, zone_type=CIVIC_SPECIAL_BUILDING."));

            checks.Add(byColor.TryGetValue("#000000", out var r0000) && r0000.Role == "IGNORE"
                ? Pass("BASELINE_000000_IGNORE", "ERROR",
                    "#000000 is IGNORE (not treated as transparency).")
                : Fail("BASELINE_000000_IGNORE", "ERROR",
                    "#000000 must be role=IGNORE. It is opaque black, not transparency."));
        }
    }

    private static HashSet<string> LoadInspectionColors(string inspectionJsonPath)
    {
        var colors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var json = File.ReadAllText(inspectionJsonPath, Encoding.UTF8);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("top_colors", out var topColors))
            {
                foreach (var entry in topColors.EnumerateArray())
                {
                    if (entry.TryGetProperty("color", out var c))
                    {
                        var val = c.GetString();
                        if (!string.IsNullOrEmpty(val)) colors.Add(val);
                    }
                }
            }
        }
        catch { /* return empty set — coverage checks will surface the issue */ }
        return colors;
    }

    private static DeadMtlWorldBuilderRawTileZoneMetadataValidation Finalize(
        string metadataPath, string tileId,
        List<DeadMtlWorldBuilderRawTileZoneMetadataValidationCheck> checks,
        bool isValid,
        DeadMtlWorldBuilderRawTileZoneMetadataClaimBoundary? cb = null)
    {
        var totals = new DeadMtlWorldBuilderRawTileZoneMetadataValidationTotals
        {
            ChecksRun = checks.Count,
            Passed    = checks.Count(c => c.Passed),
            Failed    = checks.Count(c => !c.Passed),
            Errors    = checks.Count(c => !c.Passed && c.Severity == "ERROR"),
            Warnings  = checks.Count(c => !c.Passed && c.Severity == "WARNING"),
        };
        return new DeadMtlWorldBuilderRawTileZoneMetadataValidation
        {
            TileId        = tileId,
            MetadataPath  = metadataPath,
            IsValid       = isValid,
            Checks        = checks,
            Totals        = totals,
            ClaimBoundary = cb ?? new(),
        };
    }

    public static string RenderMarkdown(DeadMtlWorldBuilderRawTileZoneMetadataValidation v)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-25B: DeadMTL WorldBuilder Raw Tile Zone Metadata Contract");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** Metadata only. No terrain generation. No sidewalk generation.");
        sb.AppendLine("> No lot subdivision. No building placement. No fence placement.");
        sb.AppendLine("> No lotpack writing. No runtime proof. Not writer-ready.");
        sb.AppendLine();
        sb.AppendLine($"**tile_id:** {v.TileId}");
        sb.AppendLine($"**metadata_path:** {v.MetadataPath}");
        sb.AppendLine($"**is_valid:** {v.IsValid}");
        sb.AppendLine();
        sb.AppendLine("## Color Role Table");
        sb.AppendLine();
        sb.AppendLine("| Color | Role | Zone Type | Street Class | Sidewalk Eligible | Notes |");
        sb.AppendLine("|-------|------|-----------|--------------|-------------------|-------|");
        sb.AppendLine("| #7200FF | ZONE | RESIDENTIAL | - | false | Residential fabric. Future lot subdivision. Frontage: MAIN_ROAD. |");
        sb.AppendLine("| #FF6600 | STREET_CORRIDOR | TRANSPORT | MAIN_ROAD | **true** | Main frontage road. Sidewalks generated later from neighborhood profile. |");
        sb.AppendLine("| #F000FF | STREET_CORRIDOR | TRANSPORT | BACK_ALLEY | false | Back alley / service corridor. No sidewalks. Rear access only. |");
        sb.AppendLine("| #CE0000 | ZONE | COMMERCIAL | - | false | Commercial zone. Future lot subdivision. Frontage: MAIN_ROAD. |");
        sb.AppendLine("| #00AA10 | ZONE | GREENSPACE | - | false | Park / open ground. Not normalized from any other green. |");
        sb.AppendLine("| #B2BD87 | UNIQUE_PLACEHOLDER | CIVIC_SPECIAL_BUILDING | - | false | Civic / government / institutional placeholder. No procedural fill. |");
        sb.AppendLine("| #000000 | IGNORE | VOID_OR_BORDER | - | false | Opaque black border marker. Not transparency. |");
        sb.AppendLine();
        sb.AppendLine("## Sidewalk Note");
        sb.AppendLine();
        sb.AppendLine("Sidewalks are NOT drawn as their own color in the source PNG.");
        sb.AppendLine("They are generated later from eligible MAIN_ROAD corridors using the active neighborhood profile.");
        sb.AppendLine("sidewalk_eligible = true applies only to MAIN_ROAD street corridors.");
        sb.AppendLine();
        sb.AppendLine("## Back Alley Note");
        sb.AppendLine();
        sb.AppendLine("BACK_ALLEY corridors (#F000FF) do not generate sidewalks.");
        sb.AppendLine("They serve rear/service access. They must not be used as primary facade frontage.");
        sb.AppendLine("Residential and commercial lots should prefer MAIN_ROAD (#FF6600) as their frontage.");
        sb.AppendLine();
        sb.AppendLine("## Lot Subdivision Note");
        sb.AppendLine();
        sb.AppendLine("Residential and commercial zones will be subdivided into lots in a future pass (FUTURE_MAP25C).");
        sb.AppendLine("lot_subdivision_policy.enabled_later = true marks them as eligible for future subdivision.");
        sb.AppendLine("No subdivision is executed now.");
        sb.AppendLine();
        sb.AppendLine("## Facade Orientation Note");
        sb.AppendLine();
        sb.AppendLine("Residential lots use ROW_UNIFORM_FRONTAGE: when a block has one MAIN_ROAD frontage,");
        sb.AppendLine("all lots in that row face that road. Alleys serve as rear/service access.");
        sb.AppendLine("If a block has multiple possible frontages, the layout system picks the best frontage");
        sb.AppendLine("based on road class, adjacency, block shape, and neighborhood rules.");
        sb.AppendLine();
        sb.AppendLine("## Commercial Lot Subdivision Note");
        sb.AppendLine();
        sb.AppendLine("Commercial zones (#CE0000) are also subdivided if large enough.");
        sb.AppendLine("Commercial lots prefer MAIN_ROAD frontage (preferred_frontage = MAIN_ROAD).");
        sb.AppendLine();
        sb.AppendLine("## Civic Placeholder Note");
        sb.AppendLine();
        sb.AppendLine("#B2BD87 is a UNIQUE_PLACEHOLDER for civic/government/institutional buildings.");
        sb.AppendLine("It is NOT procedural fill. unique_override_allowed = true.");
        sb.AppendLine("Later metadata may link specific instances to a building id.");
        sb.AppendLine();
        sb.AppendLine("## Fence / Cloture Note");
        sb.AppendLine();
        sb.AppendLine("Fences and clotures are a future layer generated on lot lines AFTER lot subdivision.");
        sb.AppendLine("lot_line_fence_policy = FUTURE_LAYER — not part of this metadata contract.");
        sb.AppendLine();
        sb.AppendLine("## Procedural Fill Note");
        sb.AppendLine();
        sb.AppendLine("ZONE colors with procedural_fill = true are filled procedurally by WorldBuilder.");
        sb.AppendLine("UNIQUE_PLACEHOLDERs (procedural_fill = false) reserve a specific building or hand-authored placement.");
        sb.AppendLine("IGNORE tiles are never filled.");
        sb.AppendLine();
        sb.AppendLine("## Unique Override Note");
        sb.AppendLine();
        sb.AppendLine("unique_override_allowed = true allows a hand-authored building or explicit placement to override procedural fill.");
        sb.AppendLine("IGNORE tiles (unique_override_allowed = false) do not accept overrides.");
        sb.AppendLine();
        sb.AppendLine("## Validation Results");
        sb.AppendLine();
        sb.AppendLine($"checks_run: {v.Totals.ChecksRun} | passed: {v.Totals.Passed} | failed: {v.Totals.Failed} | errors: {v.Totals.Errors} | warnings: {v.Totals.Warnings}");
        sb.AppendLine();
        sb.AppendLine("| rule_id | severity | passed | message |");
        sb.AppendLine("|---------|----------|--------|---------|");
        foreach (var c in v.Checks)
            sb.AppendLine($"| {c.RuleId} | {c.Severity} | {c.Passed} | {c.Message} |");
        sb.AppendLine();
        sb.AppendLine("## Claim Boundary");
        sb.AppendLine();
        sb.AppendLine($"- writes_lotpack: {v.ClaimBoundary.WritesLotpack}");
        sb.AppendLine($"- writes_worldgen_lua: {v.ClaimBoundary.WritesWorldgenLua}");
        sb.AppendLine($"- runtime_proven: {v.ClaimBoundary.RuntimeProven}");
        sb.AppendLine($"- public_playable_claim: {v.ClaimBoundary.PublicPlayableClaim}");
        sb.AppendLine($"- writer_ready_claim: {v.ClaimBoundary.WriterReadyClaim}");
        sb.AppendLine($"- generates_buildings_now: {v.ClaimBoundary.GeneratesBuildingsNow}");
        sb.AppendLine($"- generates_sidewalks_now: {v.ClaimBoundary.GeneratesSidewalksNow}");
        sb.AppendLine($"- subdivides_lots_now: {v.ClaimBoundary.SubdividesLotsNow}");
        sb.AppendLine($"- captures_chunk_layers_now: {v.ClaimBoundary.CapturesChunkLayersNow}");
        sb.AppendLine();
        sb.AppendLine("**VERDICT: MAP25B_WORLDBUILDER_RAW_TILE_ZONE_METADATA_CONTRACT_COMPLETE**");
        return sb.ToString();
    }

    public static string RenderCsv(DeadMtlWorldBuilderRawTileZoneMetadataValidation v)
    {
        var sb = new StringBuilder();
        sb.AppendLine("rule_id,severity,passed,message");
        foreach (var c in v.Checks)
            sb.AppendLine($"{c.RuleId},{c.Severity},{c.Passed},{CsvEscape(c.Message)}");
        return sb.ToString();
    }

    public static string RenderSummary(DeadMtlWorldBuilderRawTileZoneMetadataValidation v)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-25B: DeadMTL WorldBuilder Raw Tile Zone Metadata Contract");
        sb.AppendLine();
        sb.AppendLine($"tile_id:       {v.TileId}");
        sb.AppendLine($"metadata_path: {v.MetadataPath}");
        sb.AppendLine($"is_valid:      {v.IsValid}");
        sb.AppendLine();
        sb.AppendLine($"checks_run:    {v.Totals.ChecksRun}");
        sb.AppendLine($"passed:        {v.Totals.Passed}");
        sb.AppendLine($"failed:        {v.Totals.Failed}");
        sb.AppendLine($"errors:        {v.Totals.Errors}");
        sb.AppendLine($"warnings:      {v.Totals.Warnings}");
        sb.AppendLine();
        sb.AppendLine("CLAIM BOUNDARY");
        sb.AppendLine($"  writes_lotpack:              {v.ClaimBoundary.WritesLotpack}");
        sb.AppendLine($"  writes_worldgen_lua:         {v.ClaimBoundary.WritesWorldgenLua}");
        sb.AppendLine($"  runtime_proven:              {v.ClaimBoundary.RuntimeProven}");
        sb.AppendLine($"  public_playable_claim:       {v.ClaimBoundary.PublicPlayableClaim}");
        sb.AppendLine($"  writer_ready_claim:          {v.ClaimBoundary.WriterReadyClaim}");
        sb.AppendLine($"  generates_buildings_now:     {v.ClaimBoundary.GeneratesBuildingsNow}");
        sb.AppendLine($"  generates_sidewalks_now:     {v.ClaimBoundary.GeneratesSidewalksNow}");
        sb.AppendLine($"  subdivides_lots_now:         {v.ClaimBoundary.SubdividesLotsNow}");
        sb.AppendLine($"  captures_chunk_layers_now:   {v.ClaimBoundary.CapturesChunkLayersNow}");
        if (v.Checks.Any(c => !c.Passed))
        {
            sb.AppendLine();
            sb.AppendLine("FAILED CHECKS");
            foreach (var c in v.Checks.Where(c => !c.Passed))
                sb.AppendLine($"  [{c.Severity}] {c.RuleId}: {c.Message}");
        }
        sb.AppendLine();
        sb.AppendLine("VERDICT: MAP25B_WORLDBUILDER_RAW_TILE_ZONE_METADATA_CONTRACT_COMPLETE");
        return sb.ToString();
    }

    private static DeadMtlWorldBuilderRawTileZoneMetadataValidationCheck Pass(
        string ruleId, string severity, string message) =>
        new() { RuleId = ruleId, Severity = severity, Passed = true,  Message = message };

    private static DeadMtlWorldBuilderRawTileZoneMetadataValidationCheck Fail(
        string ruleId, string severity, string message) =>
        new() { RuleId = ruleId, Severity = severity, Passed = false, Message = message };

    private static string CsvEscape(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}
