using System;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Core.Combat;

/// <summary>
/// Represents a ship (or station) in combat. Snapshot of combat-relevant stats.
/// </summary>
public class CombatUnit
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int OwnerId { get; set; }
    public FleetRole Role { get; set; }

    // Offense
    public float WeaponDamage { get; set; }
    public WeaponType WeaponType { get; set; }
    public float Tracking { get; set; } = 1.0f; // Accuracy vs small targets

    // Defense layers (resolved in order: PD → Shields → Armor → Structure)
    public float PointDefense { get; set; }
    public float ShieldHp { get; set; }
    public float ShieldMax { get; set; }
    public float ShieldRegen { get; set; } = 1f;
    public float ArmorHp { get; set; }
    public float ArmorMax { get; set; }
    public float StructureHp { get; set; }
    public float StructureMax { get; set; }

    // State
    public float Morale { get; set; } = 100f;
    public bool IsDestroyed => StructureHp <= 0;
    public bool IsRetreating { get; set; }
    public float RetreatProgress { get; set; } // 0-1, at 1.0 = escaped

    public CombatPosition Position { get; set; } = CombatPosition.HoldPosition;
}
