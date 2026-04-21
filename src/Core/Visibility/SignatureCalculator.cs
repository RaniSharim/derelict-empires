using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Core.Visibility;

/// <summary>
/// Computes the scalar signature (how loudly something radiates) for an entity, deterministically
/// from its current state. Numbers are indicative per design/in_system_design.md §6.2 — pure
/// functions, no RNG, no side effects.
/// </summary>
public static class SignatureCalculator
{
    /// <summary>Colony signature. Pop growth + building activity drive it upward.</summary>
    public static int ForColony(ColonyData c)
    {
        if (c == null) return 0;
        return c.Population * 6;
    }

    /// <summary>Station signature. Size tier dominates; active modules add minor emission.</summary>
    public static int ForStation(StationData s)
    {
        if (s == null) return 0;
        return s.SizeTier * 15 + (s.InstalledModules?.Count ?? 0) * 2;
    }

    /// <summary>Salvage site signature. Passive, proportional to hazard radiation.</summary>
    public static int ForSalvageSite(SalvageSiteData site)
    {
        if (site == null) return 0;
        return (int)(site.HazardLevel * 20);
    }

    /// <summary>Fleet signature. Hull count dominates; add a combat spike separately at call site.</summary>
    public static int ForFleet(FleetData fleet)
    {
        if (fleet == null) return 0;
        return (fleet.ShipIds?.Count ?? 0) * 4;
    }

    /// <summary>
    /// Outpost signature. Outposts don't have their own data class in v1 (pop sits on Settlement
    /// runtime object); callers pass the pop count directly.
    /// </summary>
    public static int ForOutpost(int population) => population * 3;
}
