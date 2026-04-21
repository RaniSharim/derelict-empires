using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Models;

/// <summary>
/// Runtime data for a colony on a habitable planet.
/// Full settlement with population growth, buildings, production queue.
/// </summary>
public class ColonyData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int OwnerEmpireId { get; set; }
    public int SystemId { get; set; }
    public int POIId { get; set; }

    /// <summary>Auto-allocation priority. Mirrors <see cref="Settlements.Colony.Priority"/>; persists across save/load.</summary>
    public ColonyPriority Priority { get; set; } = ColonyPriority.Balanced;

    /// <summary>Planet size determines pop cap.</summary>
    public PlanetSize PlanetSize { get; set; }

    /// <summary>Current total population.</summary>
    public int Population { get; set; } = 1;

    /// <summary>Maximum population from planet size + buildings.</summary>
    public int PopCap => GetBasePopCap() + BonusPopCap;

    public int BonusPopCap { get; set; }

    /// <summary>Happiness/stability 0-100. Affects productivity.</summary>
    public float Happiness { get; set; } = 70f;

    public int GetBasePopCap() => PlanetSize switch
    {
        PlanetSize.Small => 6,
        PlanetSize.Medium => 8,
        PlanetSize.Large => 13,
        PlanetSize.Prime => 20,
        PlanetSize.Exceptional => 30,
        _ => 5
    };
}
