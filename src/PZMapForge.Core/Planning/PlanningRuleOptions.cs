namespace PZMapForge.Core.Planning;

/// <summary>
/// Configurable thresholds for PlanningRuleEngine.Evaluate.
/// Defaults match the hardcoded values used before this type was introduced.
/// </summary>
public sealed class PlanningRuleOptions
{
    /// <summary>Default options — identical to the no-options overload behavior.</summary>
    public static readonly PlanningRuleOptions Default = new();

    /// <summary>
    /// Buildings with pixel_count &lt;= this value receive a tiny_building_candidate warning.
    /// Default: 9 (a 3x3 footprint or smaller).
    /// </summary>
    public int TinyBuildingPixelThreshold { get; }

    /// <summary>
    /// Ground regions with pixel_count &gt; this value receive a large_open_ground_area note.
    /// Default: 50000.
    /// </summary>
    public int LargeGroundPixelThreshold { get; }

    /// <param name="tinyBuildingPixelThreshold">Must be &gt;= 0.</param>
    /// <param name="largeGroundPixelThreshold">Must be &gt;= 0.</param>
    public PlanningRuleOptions(
        int tinyBuildingPixelThreshold = 9,
        int largeGroundPixelThreshold  = 50_000)
    {
        if (tinyBuildingPixelThreshold < 0)
            throw new ArgumentOutOfRangeException(
                nameof(tinyBuildingPixelThreshold),
                tinyBuildingPixelThreshold,
                "TinyBuildingPixelThreshold must be >= 0.");

        if (largeGroundPixelThreshold < 0)
            throw new ArgumentOutOfRangeException(
                nameof(largeGroundPixelThreshold),
                largeGroundPixelThreshold,
                "LargeGroundPixelThreshold must be >= 0.");

        TinyBuildingPixelThreshold = tinyBuildingPixelThreshold;
        LargeGroundPixelThreshold  = largeGroundPixelThreshold;
    }
}
