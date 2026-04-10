namespace DerlictEmpires.Core.Combat;

public enum WeaponType { Laser, Railgun, Missile }
public enum DefenseType { PointDefense, Shield, Armor }
public enum CombatPosition { ChargeForward, HoldPosition, StandBack }

/// <summary>
/// Weapons triangle damage multipliers.
/// Lasers → effective vs Shields, Railguns → effective vs Armor, Missiles → overwhelm PD.
/// </summary>
public static class WeaponsTriangle
{
    /// <summary>
    /// Get damage multiplier when a weapon type hits a specific defense.
    /// Returns > 1 for effective matchups, < 1 for poor matchups.
    /// </summary>
    public static float GetMultiplier(WeaponType weapon, DefenseType defense) =>
        (weapon, defense) switch
        {
            // Lasers: drain shields, weak vs armor
            (WeaponType.Laser, DefenseType.Shield) => 1.5f,
            (WeaponType.Laser, DefenseType.Armor) => 0.6f,
            (WeaponType.Laser, DefenseType.PointDefense) => 1.0f, // PD can't stop lasers

            // Railguns: penetrate armor, deflected by shields
            (WeaponType.Railgun, DefenseType.Armor) => 1.5f,
            (WeaponType.Railgun, DefenseType.Shield) => 0.7f,
            (WeaponType.Railgun, DefenseType.PointDefense) => 0.8f, // PD can intercept some

            // Missiles: overwhelm PD through volume, balanced vs others
            (WeaponType.Missile, DefenseType.PointDefense) => 1.3f,
            (WeaponType.Missile, DefenseType.Shield) => 0.9f,
            (WeaponType.Missile, DefenseType.Armor) => 1.0f,

            _ => 1.0f
        };
}
