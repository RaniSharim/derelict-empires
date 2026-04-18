using System;
using System.Collections.Generic;
using System.Linq;

namespace DerlictEmpires.Core.Ships;

/// <summary>
/// An immutable ship design template. Chassis + slot fills + extras → computed stats.
/// Once saved, shipyards produce ships from this design.
/// </summary>
public class ShipDesign
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string ChassisId { get; set; } = "";

    /// <summary>Subsystem IDs filling each big system slot (empty string = unfilled).</summary>
    public List<string> SlotFills { get; set; } = new();

    /// <summary>Extra modification IDs (consume free capacity).</summary>
    public List<string> Extras { get; set; } = new();

    /// <summary>Total free capacity consumed by extras.</summary>
    public int UsedCapacity => Extras.Count * 2; // Simplified: 2 capacity per extra

    /// <summary>Scan points produced per fast tick while scanning.</summary>
    public float ScanStrength { get; set; }

    /// <summary>Extraction units produced per slow tick while extracting.</summary>
    public float ExtractionStrength { get; set; }

    /// <summary>Lane progress per fast tick (0..1 per tick fraction).</summary>
    public float Speed { get; set; }

    public ChassisData? GetChassis() => ChassisData.FindById(ChassisId);
}

/// <summary>Validates ship designs.</summary>
public static class ShipDesignValidator
{
    public struct ValidationResult
    {
        public bool IsValid;
        public string? Error;

        public static ValidationResult Ok() => new() { IsValid = true };
        public static ValidationResult Fail(string error) => new() { IsValid = false, Error = error };
    }

    public static ValidationResult Validate(ShipDesign design, HashSet<string>? researchedSubsystems = null)
    {
        var chassis = design.GetChassis();
        if (chassis == null)
            return ValidationResult.Fail($"Unknown chassis: {design.ChassisId}");

        if (design.SlotFills.Count > chassis.BigSystemSlots)
            return ValidationResult.Fail($"Too many slot fills: {design.SlotFills.Count} > {chassis.BigSystemSlots}");

        // Check all filled slots reference researched subsystems
        if (researchedSubsystems != null)
        {
            foreach (var subId in design.SlotFills)
            {
                if (!string.IsNullOrEmpty(subId) && !researchedSubsystems.Contains(subId))
                    return ValidationResult.Fail($"Unresearched subsystem: {subId}");
            }
        }

        // Check capacity
        if (design.UsedCapacity > chassis.FreeCapacity)
            return ValidationResult.Fail($"Over capacity: {design.UsedCapacity} > {chassis.FreeCapacity}");

        return ValidationResult.Ok();
    }
}

/// <summary>Calculates aggregate stats from a ship design.</summary>
public static class ShipStatCalculator
{
    public struct ShipStats
    {
        public int Hp;
        public float Speed;
        public float Visibility;
        public int MaintenanceCost;
        public int FilledSlots;
        public int TotalSlots;
        public int UsedCapacity;
        public int TotalCapacity;
    }

    public static ShipStats Calculate(ShipDesign design, float expertiseBonus = 1.0f)
    {
        var chassis = design.GetChassis();
        if (chassis == null) return default;

        int filledSlots = design.SlotFills.Count(s => !string.IsNullOrEmpty(s));

        return new ShipStats
        {
            Hp = (int)(chassis.BaseHp * expertiseBonus),
            Speed = chassis.BaseSpeed,
            Visibility = chassis.BaseVisibility + filledSlots * 2f,
            MaintenanceCost = chassis.MaintenanceCost + filledSlots * 2,
            FilledSlots = filledSlots,
            TotalSlots = chassis.BigSystemSlots,
            UsedCapacity = design.UsedCapacity,
            TotalCapacity = chassis.FreeCapacity,
        };
    }
}
