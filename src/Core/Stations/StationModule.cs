using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Stations;

/// <summary>Base class for all station modules.</summary>
public abstract class StationModule
{
    public abstract StationModuleType Type { get; }
    public abstract string DisplayName { get; }

    /// <summary>How many module slots this module occupies.</summary>
    public virtual int SlotCost => 1;

    /// <summary>Production cost to install this module.</summary>
    public abstract int InstallCost { get; }

    /// <summary>Per-tick resource upkeep cost (credits).</summary>
    public virtual int Upkeep => 5;
}

public class ShipyardModule : StationModule
{
    public override StationModuleType Type => StationModuleType.Shipyard;
    public override string DisplayName => "Shipyard";
    public override int InstallCost => 150;

    /// <summary>Max ship size class this shipyard can build (0=Fighter..6=Titan).</summary>
    public int MaxShipClass { get; set; } = 3; // Destroyer by default

    /// <summary>Production rate multiplier for ship construction.</summary>
    public float ProductionRate { get; set; } = 1.0f;

    /// <summary>Whether this shipyard can refit existing ships.</summary>
    public bool CanRefit { get; set; } = true;
}

public class DefenseModule : StationModule
{
    public override StationModuleType Type => StationModuleType.Defense;
    public override string DisplayName => "Defense Battery";
    public override int InstallCost => 100;

    public float WeaponDamage { get; set; } = 15f;
    public float ShieldHp { get; set; } = 50f;
    public float ArmorHp { get; set; } = 30f;
    public float PointDefenseRating { get; set; } = 10f;
}

public class LogisticsModule : StationModule
{
    public override StationModuleType Type => StationModuleType.Logistics;
    public override string DisplayName => "Logistics Hub";
    public override int InstallCost => 120;

    /// <summary>Supply points this hub can handle per tick.</summary>
    public float SupplyCapacity { get; set; } = 50f;

    /// <summary>Number of hops this hub extends the supply network.</summary>
    public int RangeExtension { get; set; } = 1;
}

public class TradeModule : StationModule
{
    public override StationModuleType Type => StationModuleType.Trade;
    public override string DisplayName => "Trade Hub";
    public override int InstallCost => 130;

    /// <summary>Bonus multiplier on trade goods throughput.</summary>
    public float ThroughputBonus { get; set; } = 0.25f;

    /// <summary>Enables market access at this station.</summary>
    public bool MarketAccess { get; set; } = true;
}

public class GarrisonModule : StationModule
{
    public override StationModuleType Type => StationModuleType.Garrison;
    public override string DisplayName => "Garrison Bay";
    public override int InstallCost => 80;

    /// <summary>Number of fleet slots for stationed fleets.</summary>
    public int FleetCapacity { get; set; } = 2;

    /// <summary>Maintenance cost reduction for stationed fleets (0-1).</summary>
    public float MaintenanceReduction { get; set; } = 0.20f;
}

public class SensorModule : StationModule
{
    public override StationModuleType Type => StationModuleType.Sensors;
    public override string DisplayName => "Sensor Array";
    public override int InstallCost => 90;

    /// <summary>Detection range in systems (0 = own system only).</summary>
    public int DetectionRange { get; set; } = 1;

    /// <summary>Stations are always visible — this tracks sensor quality for adjacent systems.</summary>
    public float SensorPower { get; set; } = 30f;
}
