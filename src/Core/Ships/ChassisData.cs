using System;
using System.Collections.Generic;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Ships;

/// <summary>
/// Defines a ship chassis: size class, variant, slot count, free capacity, base stats.
/// 14 total (7 sizes × 2 variants).
/// </summary>
public class ChassisData
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public ShipSizeClass SizeClass { get; set; }
    public string Variant { get; set; } = ""; // e.g., "fast", "armed"
    public int BigSystemSlots { get; set; }
    public int FreeCapacity { get; set; }
    public int BaseHp { get; set; }
    public float BaseSpeed { get; set; }
    public float BaseVisibility { get; set; }
    public int MaintenanceCost { get; set; }

    public static readonly ChassisData[] All = GenerateAll();

    private static ChassisData[] GenerateAll() => new[]
    {
        // Fighter (carrier-launched only)
        new ChassisData { Id = "fighter_fast", DisplayName = "Light Fighter", SizeClass = ShipSizeClass.Fighter, Variant = "fast", BigSystemSlots = 1, FreeCapacity = 2, BaseHp = 30, BaseSpeed = 20f, BaseVisibility = 5f, MaintenanceCost = 2 },
        new ChassisData { Id = "fighter_armed", DisplayName = "Heavy Fighter", SizeClass = ShipSizeClass.Fighter, Variant = "armed", BigSystemSlots = 1, FreeCapacity = 3, BaseHp = 40, BaseSpeed = 16f, BaseVisibility = 8f, MaintenanceCost = 3 },
        // Corvette
        new ChassisData { Id = "corvette_fast", DisplayName = "Fast Corvette", SizeClass = ShipSizeClass.Corvette, Variant = "fast", BigSystemSlots = 2, FreeCapacity = 5, BaseHp = 60, BaseSpeed = 18f, BaseVisibility = 10f, MaintenanceCost = 5 },
        new ChassisData { Id = "corvette_armed", DisplayName = "Armed Corvette", SizeClass = ShipSizeClass.Corvette, Variant = "armed", BigSystemSlots = 3, FreeCapacity = 4, BaseHp = 70, BaseSpeed = 14f, BaseVisibility = 15f, MaintenanceCost = 7 },
        // Frigate
        new ChassisData { Id = "frigate_fast", DisplayName = "Patrol Frigate", SizeClass = ShipSizeClass.Frigate, Variant = "fast", BigSystemSlots = 3, FreeCapacity = 6, BaseHp = 90, BaseSpeed = 14f, BaseVisibility = 15f, MaintenanceCost = 8 },
        new ChassisData { Id = "frigate_heavy", DisplayName = "Escort Frigate", SizeClass = ShipSizeClass.Frigate, Variant = "heavy", BigSystemSlots = 3, FreeCapacity = 8, BaseHp = 110, BaseSpeed = 11f, BaseVisibility = 20f, MaintenanceCost = 10 },
        // Destroyer
        new ChassisData { Id = "destroyer_balanced", DisplayName = "Fleet Destroyer", SizeClass = ShipSizeClass.Destroyer, Variant = "balanced", BigSystemSlots = 3, FreeCapacity = 10, BaseHp = 140, BaseSpeed = 12f, BaseVisibility = 25f, MaintenanceCost = 12 },
        new ChassisData { Id = "destroyer_heavy", DisplayName = "Heavy Destroyer", SizeClass = ShipSizeClass.Destroyer, Variant = "heavy", BigSystemSlots = 3, FreeCapacity = 12, BaseHp = 170, BaseSpeed = 10f, BaseVisibility = 30f, MaintenanceCost = 15 },
        // Cruiser
        new ChassisData { Id = "cruiser_weapons", DisplayName = "Strike Cruiser", SizeClass = ShipSizeClass.Cruiser, Variant = "weapons", BigSystemSlots = 3, FreeCapacity = 14, BaseHp = 220, BaseSpeed = 9f, BaseVisibility = 35f, MaintenanceCost = 20 },
        new ChassisData { Id = "cruiser_defense", DisplayName = "Armored Cruiser", SizeClass = ShipSizeClass.Cruiser, Variant = "defense", BigSystemSlots = 3, FreeCapacity = 16, BaseHp = 280, BaseSpeed = 8f, BaseVisibility = 35f, MaintenanceCost = 22 },
        // Battleship
        new ChassisData { Id = "battleship_broadside", DisplayName = "Broadside Battleship", SizeClass = ShipSizeClass.Battleship, Variant = "broadside", BigSystemSlots = 3, FreeCapacity = 18, BaseHp = 350, BaseSpeed = 7f, BaseVisibility = 45f, MaintenanceCost = 30 },
        new ChassisData { Id = "battleship_carrier", DisplayName = "Fleet Carrier", SizeClass = ShipSizeClass.Battleship, Variant = "carrier", BigSystemSlots = 3, FreeCapacity = 20, BaseHp = 300, BaseSpeed = 7f, BaseVisibility = 50f, MaintenanceCost = 35 },
        // Titan
        new ChassisData { Id = "titan_firepower", DisplayName = "Dreadnought", SizeClass = ShipSizeClass.Titan, Variant = "firepower", BigSystemSlots = 3, FreeCapacity = 24, BaseHp = 500, BaseSpeed = 5f, BaseVisibility = 60f, MaintenanceCost = 50 },
        new ChassisData { Id = "titan_command", DisplayName = "Command Titan", SizeClass = ShipSizeClass.Titan, Variant = "command", BigSystemSlots = 3, FreeCapacity = 22, BaseHp = 450, BaseSpeed = 5f, BaseVisibility = 55f, MaintenanceCost = 45 },
    };

    public static ChassisData? FindById(string id)
    {
        foreach (var c in All) if (c.Id == id) return c;
        return null;
    }

    public static List<ChassisData> GetBySize(ShipSizeClass sizeClass)
    {
        var result = new List<ChassisData>();
        foreach (var c in All) if (c.SizeClass == sizeClass) result.Add(c);
        return result;
    }
}
