using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Production;

namespace DerlictEmpires.Core.Stations;

/// <summary>
/// A space station at a POI. Has module slots determined by size tier.
/// Aggregates stats from installed modules.
/// </summary>
public class Station
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int OwnerEmpireId { get; set; }
    public int SystemId { get; set; }
    public int POIId { get; set; }

    /// <summary>Size tier 1-5. Determines module slot count.</summary>
    public int SizeTier { get; set; } = 1;

    /// <summary>Base HP of the station hull.</summary>
    public int BaseHp { get; set; } = 200;

    /// <summary>Installed modules.</summary>
    public List<StationModule> Modules { get; set; } = new();

    /// <summary>Production queue for ship construction (if shipyard installed).</summary>
    public ProductionQueue ShipQueue { get; } = new();

    /// <summary>Production queue for module installation.</summary>
    public ProductionQueue ModuleQueue { get; } = new();

    /// <summary>Whether station construction is complete.</summary>
    public bool IsConstructed { get; set; } = true;

    /// <summary>Construction progress if not yet built (0-1).</summary>
    public float ConstructionProgress { get; set; } = 1f;

    // === Computed Properties ===

    public int MaxModuleSlots => SizeTier + 1;
    public int UsedModuleSlots => Modules.Sum(m => m.SlotCost);
    public int FreeModuleSlots => MaxModuleSlots - UsedModuleSlots;

    public bool HasShipyard => Modules.Any(m => m.Type == StationModuleType.Shipyard);
    public bool HasLogistics => Modules.Any(m => m.Type == StationModuleType.Logistics);
    public bool HasTrade => Modules.Any(m => m.Type == StationModuleType.Trade);
    public bool HasGarrison => Modules.Any(m => m.Type == StationModuleType.Garrison);

    public ShipyardModule? Shipyard => Modules.OfType<ShipyardModule>().FirstOrDefault();

    /// <summary>Aggregate defense stats from all defense modules.</summary>
    public float TotalWeaponDamage => Modules.OfType<DefenseModule>().Sum(m => m.WeaponDamage);
    public float TotalShieldHp => Modules.OfType<DefenseModule>().Sum(m => m.ShieldHp);
    public float TotalArmorHp => Modules.OfType<DefenseModule>().Sum(m => m.ArmorHp);
    public float TotalPointDefense => Modules.OfType<DefenseModule>().Sum(m => m.PointDefenseRating);

    /// <summary>Aggregate logistics capacity.</summary>
    public float TotalSupplyCapacity => Modules.OfType<LogisticsModule>().Sum(m => m.SupplyCapacity);

    /// <summary>Aggregate garrison capacity.</summary>
    public int TotalGarrisonCapacity => Modules.OfType<GarrisonModule>().Sum(m => m.FleetCapacity);

    /// <summary>Aggregate sensor range (max, not sum).</summary>
    public int SensorRange => Modules.OfType<SensorModule>().Any()
        ? Modules.OfType<SensorModule>().Max(m => m.DetectionRange)
        : 0;

    /// <summary>Per-tick upkeep cost (credits).</summary>
    public int TotalUpkeep => Modules.Sum(m => m.Upkeep);

    /// <summary>Can a module be installed? Checks slot availability.</summary>
    public bool CanInstallModule(StationModule module) =>
        IsConstructed && module.SlotCost <= FreeModuleSlots;

    /// <summary>Install a module directly (bypasses queue).</summary>
    public bool InstallModule(StationModule module)
    {
        if (!CanInstallModule(module)) return false;
        Modules.Add(module);
        return true;
    }

    /// <summary>Remove a module by type (first match).</summary>
    public bool RemoveModule(StationModuleType type)
    {
        var module = Modules.FirstOrDefault(m => m.Type == type);
        if (module == null) return false;
        Modules.Remove(module);
        return true;
    }
}
