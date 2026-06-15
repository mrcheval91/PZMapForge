using System.Text;
using System.Text.Json;

namespace PZMapForge.Core.WorldGen;

public static class DeadMtlWorldBuilderNeighborhoodProfileValidator
{
    private static readonly JsonDocumentOptions DocOpts = new() { AllowTrailingCommas = true };

    private static readonly HashSet<string> ValidSidewalkSources = new(StringComparer.Ordinal)
    {
        "INSIDE_STREET_ZONE", "OUTSIDE_STREET_ZONE", "MIXED", "NONE"
    };

    private static readonly HashSet<string> ValidIntensities = new(StringComparer.Ordinal)
    {
        "EMPTY", "LIGHT", "MEDIUM", "DENSE", "CONDENSED", "CROWDED"
    };

    private const string ExpectedFormat = "pzmapforge.deadmtl.worldbuilder.neighborhood-profile.v1";

    public static DeadMtlWorldBuilderNeighborhoodProfileValidationResult Validate(string profilePath)
    {
        var result = new DeadMtlWorldBuilderNeighborhoodProfileValidationResult();
        var checks = new List<DeadMtlWorldBuilderNeighborhoodProfileValidationCheck>();

        // FILE_EXISTS — must pass before anything else can be read
        if (!File.Exists(profilePath))
        {
            checks.Add(Fail("FILE_EXISTS", "ERROR", $"Profile file not found: {profilePath}"));
            result.Validation = Finalize(profilePath, string.Empty, checks, isValid: false);
            result.IsValid    = false;
            return result;
        }
        checks.Add(Pass("FILE_EXISTS", "ERROR", "Profile file exists."));

        DeadMtlWorldBuilderNeighborhoodProfile profile;
        try
        {
            var json    = File.ReadAllText(profilePath, Encoding.UTF8);
            var opts    = new JsonSerializerOptions { AllowTrailingCommas = true, PropertyNameCaseInsensitive = false };
            profile     = JsonSerializer.Deserialize<DeadMtlWorldBuilderNeighborhoodProfile>(json, opts)
                          ?? new DeadMtlWorldBuilderNeighborhoodProfile();
        }
        catch (Exception ex)
        {
            checks.Add(Fail("JSON_PARSE", "ERROR", $"Failed to parse profile JSON: {ex.Message}"));
            result.Validation = Finalize(profilePath, string.Empty, checks, isValid: false);
            result.IsValid    = false;
            return result;
        }

        // FORMAT_VALID
        checks.Add(profile.Format == ExpectedFormat
            ? Pass("FORMAT_VALID",    "ERROR", $"format == {ExpectedFormat}")
            : Fail("FORMAT_VALID",    "ERROR", $"format must be '{ExpectedFormat}', got '{profile.Format}'"));

        // PROFILE_ID_NON_EMPTY
        checks.Add(!string.IsNullOrWhiteSpace(profile.ProfileId)
            ? Pass("PROFILE_ID_NON_EMPTY", "ERROR", "profile_id is non-empty.")
            : Fail("PROFILE_ID_NON_EMPTY", "ERROR", "profile_id must be non-empty."));

        // --- Sidewalk policy ---
        var sw = profile.SidewalkPolicy;

        checks.Add(sw != null && ValidSidewalkSources.Contains(sw.SidewalkSource)
            ? Pass("SIDEWALK_SOURCE_VALID", "ERROR",
                   $"sidewalk_source '{sw?.SidewalkSource}' is valid.")
            : Fail("SIDEWALK_SOURCE_VALID", "ERROR",
                   $"sidewalk_source must be one of INSIDE_STREET_ZONE, OUTSIDE_STREET_ZONE, MIXED, NONE. Got: '{sw?.SidewalkSource}'."));

        checks.Add(sw == null || sw.DefaultLeftWidthTiles >= 0
            ? Pass("SIDEWALK_LEFT_WIDTH_NON_NEGATIVE", "ERROR",
                   "default_left_width_tiles >= 0.")
            : Fail("SIDEWALK_LEFT_WIDTH_NON_NEGATIVE", "ERROR",
                   $"default_left_width_tiles must be >= 0, got {sw?.DefaultLeftWidthTiles}."));

        checks.Add(sw == null || sw.DefaultRightWidthTiles >= 0
            ? Pass("SIDEWALK_RIGHT_WIDTH_NON_NEGATIVE", "ERROR",
                   "default_right_width_tiles >= 0.")
            : Fail("SIDEWALK_RIGHT_WIDTH_NON_NEGATIVE", "ERROR",
                   $"default_right_width_tiles must be >= 0, got {sw?.DefaultRightWidthTiles}."));

        // SIDEWALK_NONE_REQUIRES_NO_SIDEWALKS
        bool swNoneConflict = sw != null
            && sw.SidewalkSource == "NONE"
            && sw.DefaultHasSidewalks;
        checks.Add(!swNoneConflict
            ? Pass("SIDEWALK_NONE_REQUIRES_NO_SIDEWALKS", "ERROR",
                   "sidewalk_source NONE is consistent with default_has_sidewalks.")
            : Fail("SIDEWALK_NONE_REQUIRES_NO_SIDEWALKS", "ERROR",
                   "sidewalk_source is NONE but default_has_sidewalks is true — contradiction."));

        // SIDEWALK_HAS_REQUIRES_WIDTH
        bool swHasNoWidth = sw != null
            && sw.DefaultHasSidewalks
            && sw.DefaultLeftWidthTiles <= 0
            && sw.DefaultRightWidthTiles <= 0;
        checks.Add(!swHasNoWidth
            ? Pass("SIDEWALK_HAS_REQUIRES_WIDTH", "ERROR",
                   "If has_sidewalks, at least one width > 0.")
            : Fail("SIDEWALK_HAS_REQUIRES_WIDTH", "ERROR",
                   "default_has_sidewalks is true but both left and right widths are 0 or less."));

        // --- Street policy ---
        var st = profile.StreetPolicy;

        checks.Add(st != null && st.DefaultCorridorWidthTiles > 0
            ? Pass("STREET_CORRIDOR_WIDTH_POSITIVE", "ERROR",
                   $"default_corridor_width_tiles = {st?.DefaultCorridorWidthTiles} > 0.")
            : Fail("STREET_CORRIDOR_WIDTH_POSITIVE", "ERROR",
                   $"default_corridor_width_tiles must be > 0, got {st?.DefaultCorridorWidthTiles}."));

        checks.Add(st != null && st.DefaultLaneWidthTiles > 0
            ? Pass("STREET_LANE_WIDTH_POSITIVE", "ERROR",
                   $"default_lane_width_tiles = {st?.DefaultLaneWidthTiles} > 0.")
            : Fail("STREET_LANE_WIDTH_POSITIVE", "ERROR",
                   $"default_lane_width_tiles must be > 0, got {st?.DefaultLaneWidthTiles}."));

        checks.Add(st == null || st.DefaultLaneCount >= 0
            ? Pass("STREET_LANE_COUNT_NON_NEGATIVE", "ERROR",
                   $"default_lane_count = {st?.DefaultLaneCount} >= 0.")
            : Fail("STREET_LANE_COUNT_NON_NEGATIVE", "ERROR",
                   $"default_lane_count must be >= 0, got {st?.DefaultLaneCount}."));

        // --- Zoning policy ---
        var zp = profile.ZoningPolicy;

        checks.Add(zp?.Residential != null
            ? Pass("RESIDENTIAL_POLICY_EXISTS", "ERROR", "residential zoning policy exists.")
            : Fail("RESIDENTIAL_POLICY_EXISTS", "ERROR", "residential zoning policy is missing."));

        checks.Add(zp?.Commercial != null
            ? Pass("COMMERCIAL_POLICY_EXISTS", "ERROR", "commercial zoning policy exists.")
            : Fail("COMMERCIAL_POLICY_EXISTS", "ERROR", "commercial zoning policy is missing."));

        checks.Add(zp?.Industrial != null
            ? Pass("INDUSTRIAL_POLICY_EXISTS", "ERROR", "industrial zoning policy exists.")
            : Fail("INDUSTRIAL_POLICY_EXISTS", "ERROR", "industrial zoning policy is missing."));

        checks.Add(zp?.Residential == null || ValidIntensities.Contains(zp.Residential.DefaultIntensity)
            ? Pass("RESIDENTIAL_INTENSITY_VALID", "ERROR",
                   $"residential default_intensity '{zp?.Residential?.DefaultIntensity}' is valid.")
            : Fail("RESIDENTIAL_INTENSITY_VALID", "ERROR",
                   $"residential default_intensity '{zp?.Residential?.DefaultIntensity}' is not a valid intensity."));

        checks.Add(zp?.Commercial == null || ValidIntensities.Contains(zp.Commercial.DefaultIntensity)
            ? Pass("COMMERCIAL_INTENSITY_VALID", "ERROR",
                   $"commercial default_intensity '{zp?.Commercial?.DefaultIntensity}' is valid.")
            : Fail("COMMERCIAL_INTENSITY_VALID", "ERROR",
                   $"commercial default_intensity '{zp?.Commercial?.DefaultIntensity}' is not a valid intensity."));

        checks.Add(zp?.Industrial == null || ValidIntensities.Contains(zp.Industrial.DefaultIntensity)
            ? Pass("INDUSTRIAL_INTENSITY_VALID", "ERROR",
                   $"industrial default_intensity '{zp?.Industrial?.DefaultIntensity}' is valid.")
            : Fail("INDUSTRIAL_INTENSITY_VALID", "ERROR",
                   $"industrial default_intensity '{zp?.Industrial?.DefaultIntensity}' is not a valid intensity."));

        // --- Procedural fill policy ---
        var pf = profile.ProceduralFillPolicy;

        checks.Add(pf != null
            ? Pass("PROCEDURAL_FILL_POLICY_EXISTS", "ERROR", "procedural_fill_policy exists.")
            : Fail("PROCEDURAL_FILL_POLICY_EXISTS", "ERROR", "procedural_fill_policy is missing."));

        checks.Add(pf == null || pf.FillMissingUntilUniqueOverride
            ? Pass("PROCEDURAL_FILL_FILLS_UNTIL_OVERRIDE", "WARNING",
                   "fill_missing_until_unique_override is true.")
            : Fail("PROCEDURAL_FILL_FILLS_UNTIL_OVERRIDE", "WARNING",
                   "fill_missing_until_unique_override should be true for the baseline profile."));

        // --- PNG metadata policy ---
        var png = profile.PngMetadataPolicy;

        checks.Add(png == null || !string.Equals(png.ZoneColorMetadataStatus, "IMPLEMENTED", StringComparison.OrdinalIgnoreCase)
            ? Pass("PNG_ZONE_METADATA_IS_FUTURE", "ERROR",
                   $"zone_color_metadata_status '{png?.ZoneColorMetadataStatus}' is not IMPLEMENTED (correct for contract-only).")
            : Fail("PNG_ZONE_METADATA_IS_FUTURE", "ERROR",
                   "zone_color_metadata_status is IMPLEMENTED but this is a contract-only profile."));

        checks.Add(png == null || !string.Equals(png.BuildingColorMetadataStatus, "IMPLEMENTED", StringComparison.OrdinalIgnoreCase)
            ? Pass("PNG_BUILDING_METADATA_IS_FUTURE", "ERROR",
                   $"building_color_metadata_status '{png?.BuildingColorMetadataStatus}' is not IMPLEMENTED (correct).")
            : Fail("PNG_BUILDING_METADATA_IS_FUTURE", "ERROR",
                   "building_color_metadata_status is IMPLEMENTED but this is a contract-only profile."));

        checks.Add(png == null || !string.Equals(png.ChunkLayerCaptureStatus, "IMPLEMENTED", StringComparison.OrdinalIgnoreCase)
            ? Pass("PNG_CHUNK_CAPTURE_IS_FUTURE", "ERROR",
                   $"chunk_layer_capture_status '{png?.ChunkLayerCaptureStatus}' is not IMPLEMENTED (correct).")
            : Fail("PNG_CHUNK_CAPTURE_IS_FUTURE", "ERROR",
                   "chunk_layer_capture_status is IMPLEMENTED but this is a contract-only profile."));

        // --- Claim boundary ---
        var cb = profile.ClaimBoundary;

        checks.Add(cb != null
            ? Pass("CLAIM_BOUNDARY_EXISTS", "ERROR", "claim_boundary exists.")
            : Fail("CLAIM_BOUNDARY_EXISTS", "ERROR", "claim_boundary is missing."));

        checks.Add(cb == null || !cb.WritesLotpack
            ? Pass("CLAIM_WRITES_LOTPACK_FALSE",      "ERROR", "writes_lotpack is false.")
            : Fail("CLAIM_WRITES_LOTPACK_FALSE",      "ERROR", "writes_lotpack must be false for a contract-only profile."));

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

        checks.Add(cb == null || !cb.CapturesChunkLayersNow
            ? Pass("CLAIM_CAPTURES_CHUNK_LAYERS_FALSE", "ERROR", "captures_chunk_layers_now is false.")
            : Fail("CLAIM_CAPTURES_CHUNK_LAYERS_FALSE", "ERROR", "captures_chunk_layers_now must be false."));

        bool isValid = checks.All(c => c.Passed || c.Severity != "ERROR");
        result.Validation = Finalize(profilePath, profile.ProfileId, checks, isValid);
        result.IsValid    = isValid;
        return result;
    }

    public static string RenderMarkdown(DeadMtlWorldBuilderNeighborhoodProfileValidation v)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MAP-25A: DeadMTL WorldBuilder Neighborhood Profile Contract");
        sb.AppendLine();
        sb.AppendLine("> **WARNING:** Contract only. No terrain generation. No building placement.");
        sb.AppendLine("> No lotpack writing. No WorldGenOverride.lua. No runtime proof. Not writer-ready.");
        sb.AppendLine();
        sb.AppendLine($"**profile_id:** {v.ProfileId}");
        sb.AppendLine($"**profile_path:** {v.ProfilePath}");
        sb.AppendLine($"**is_valid:** {v.IsValid}");
        sb.AppendLine();
        sb.AppendLine("## Sidewalk Model");
        sb.AppendLine();
        sb.AppendLine("The street zone owns the full corridor width including sidewalks.");
        sb.AppendLine("sidewalk_source options:");
        sb.AppendLine("- `INSIDE_STREET_ZONE` (default): sidewalks carved from street corridor, adjacent lots untouched");
        sb.AppendLine("- `OUTSIDE_STREET_ZONE`: sidewalks taken from lot boundary (wider corridors)");
        sb.AppendLine("- `MIXED`: context-dependent");
        sb.AppendLine("- `NONE`: no sidewalks generated");
        sb.AppendLine();
        sb.AppendLine("## Street Corridor Model");
        sb.AppendLine();
        sb.AppendLine("Corridor = left_sidewalk + left_parking + lanes + right_parking + right_sidewalk + snowbank.");
        sb.AppendLine("Changing corridor width regenerates the full interior without eating adjacent lots.");
        sb.AppendLine();
        sb.AppendLine("## Zoning Intensity Scale");
        sb.AppendLine();
        sb.AppendLine("| Intensity | Meaning |");
        sb.AppendLine("|-----------|---------|");
        sb.AppendLine("| EMPTY | No generated buildings unless unique override |");
        sb.AppendLine("| LIGHT | Sparse, detached/low use, lots of gaps |");
        sb.AppendLine("| MEDIUM | Normal urban blocks |");
        sb.AppendLine("| DENSE | Strong urban fabric, repeated buildings |");
        sb.AppendLine("| CONDENSED | Tight city fabric, little wasted space |");
        sb.AppendLine("| CROWDED | Maximal packed urban fabric |");
        sb.AppendLine();
        sb.AppendLine("## Procedural Fill Policy");
        sb.AppendLine();
        sb.AppendLine("fill_missing_until_unique_override: true — WorldBuilder fills until a unique building override is assigned.");
        sb.AppendLine("unique_override_priority: UNIQUE_OVERRIDES_WIN — explicit placements always take priority over procedural fill.");
        sb.AppendLine("deterministic_seed_policy: PROFILE_PLUS_TILE_COORDINATE — fill is reproducible.");
        sb.AppendLine();
        sb.AppendLine("## PNG Metadata and Chunk Layer Capture (Future)");
        sb.AppendLine();
        sb.AppendLine("- `zone_color_metadata_status`: FUTURE_MAP25B — zone intent encoded in PNG metadata, not yet implemented.");
        sb.AppendLine("- `building_color_metadata_status`: FUTURE_MAP25C — building catalog color references, not yet implemented.");
        sb.AppendLine("- `chunk_layer_capture_status`: FUTURE — layered PNG export of ground/roads/vegetation/objects etc, not yet implemented.");
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
        sb.AppendLine($"- captures_chunk_layers_now: {v.ClaimBoundary.CapturesChunkLayersNow}");
        sb.AppendLine();
        sb.AppendLine("**VERDICT: MAP25A_WORLDBUILDER_NEIGHBORHOOD_PROFILE_CONTRACT_COMPLETE**");
        return sb.ToString();
    }

    public static string RenderCsv(DeadMtlWorldBuilderNeighborhoodProfileValidation v)
    {
        var sb = new StringBuilder();
        sb.AppendLine("rule_id,severity,passed,message");
        foreach (var c in v.Checks)
            sb.AppendLine($"{c.RuleId},{c.Severity},{c.Passed},{CsvEscape(c.Message)}");
        return sb.ToString();
    }

    public static string RenderSummary(DeadMtlWorldBuilderNeighborhoodProfileValidation v)
    {
        var sb = new StringBuilder();
        sb.AppendLine("MAP-25A: DeadMTL WorldBuilder Neighborhood Profile Contract");
        sb.AppendLine();
        sb.AppendLine($"profile_id:    {v.ProfileId}");
        sb.AppendLine($"profile_path:  {v.ProfilePath}");
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
        sb.AppendLine($"  captures_chunk_layers_now:   {v.ClaimBoundary.CapturesChunkLayersNow}");
        sb.AppendLine();
        if (v.Totals.Failed > 0)
        {
            sb.AppendLine("FAILED CHECKS:");
            foreach (var c in v.Checks.Where(c => !c.Passed))
                sb.AppendLine($"  [{c.Severity}] {c.RuleId}: {c.Message}");
            sb.AppendLine();
        }
        sb.AppendLine("VERDICT: MAP25A_WORLDBUILDER_NEIGHBORHOOD_PROFILE_CONTRACT_COMPLETE");
        return sb.ToString();
    }

    // -----------------------------------------------------------------------
    // Internals
    // -----------------------------------------------------------------------

    private static DeadMtlWorldBuilderNeighborhoodProfileValidationCheck Pass(
        string ruleId, string severity, string message) =>
        new() { RuleId = ruleId, Severity = severity, Passed = true,  Message = message };

    private static DeadMtlWorldBuilderNeighborhoodProfileValidationCheck Fail(
        string ruleId, string severity, string message) =>
        new() { RuleId = ruleId, Severity = severity, Passed = false, Message = message };

    private static DeadMtlWorldBuilderNeighborhoodProfileValidation Finalize(
        string profilePath,
        string profileId,
        List<DeadMtlWorldBuilderNeighborhoodProfileValidationCheck> checks,
        bool isValid)
    {
        int errors   = checks.Count(c => !c.Passed && c.Severity == "ERROR");
        int warnings = checks.Count(c => !c.Passed && c.Severity == "WARNING");
        return new DeadMtlWorldBuilderNeighborhoodProfileValidation
        {
            ProfilePath = profilePath,
            ProfileId   = profileId,
            IsValid     = isValid,
            Checks      = checks,
            Totals      = new DeadMtlWorldBuilderNeighborhoodProfileValidationTotals
            {
                ChecksRun = checks.Count,
                Passed    = checks.Count(c => c.Passed),
                Failed    = checks.Count(c => !c.Passed),
                Errors    = errors,
                Warnings  = warnings,
            },
            ClaimBoundary = new DeadMtlWorldBuilderNeighborhoodProfileClaimBoundary(),
        };
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
