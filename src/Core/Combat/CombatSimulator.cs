using System;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Core.Combat;

/// <summary>
/// Pure C# combat simulator. Takes two sides, runs ticks, produces a CombatLog.
/// Deterministic given the same inputs and seed.
/// </summary>
public class CombatSimulator
{
    public CombatLog Simulate(
        List<CombatUnit> attackers,
        List<CombatUnit> defenders,
        GameRandom rng,
        int maxRounds = 30)
    {
        var log = new CombatLog();

        for (int round = 0; round < maxRounds; round++)
        {
            var roundLog = new CombatRound { RoundNumber = round };

            // Shield regeneration
            foreach (var unit in attackers.Concat(defenders))
            {
                if (!unit.IsDestroyed && !unit.IsRetreating)
                    unit.ShieldHp = MathF.Min(unit.ShieldHp + unit.ShieldRegen, unit.ShieldMax);
            }

            // Attackers fire on defenders
            ProcessFiring(attackers, defenders, roundLog, rng);
            // Defenders fire on attackers
            ProcessFiring(defenders, attackers, roundLog, rng);

            // Remove destroyed units
            int atkDestroyed = attackers.RemoveAll(u => u.IsDestroyed);
            int defDestroyed = defenders.RemoveAll(u => u.IsDestroyed);
            roundLog.AttackersLost = atkDestroyed;
            roundLog.DefendersLost = defDestroyed;

            // Process retreat
            ProcessRetreat(attackers, roundLog, rng);
            ProcessRetreat(defenders, roundLog, rng);

            // Morale update
            UpdateMorale(attackers, rng);
            UpdateMorale(defenders, rng);

            log.Rounds.Add(roundLog);

            // Check end conditions
            var aliveAtk = attackers.Count(u => !u.IsRetreating);
            var aliveDef = defenders.Count(u => !u.IsRetreating);
            if (aliveAtk == 0 || aliveDef == 0) break;
        }

        log.AttackerVictory = defenders.Count(u => !u.IsRetreating) == 0;
        log.AttackersRemaining = attackers.Count;
        log.DefendersRemaining = defenders.Count;
        return log;
    }

    private void ProcessFiring(
        List<CombatUnit> shooters,
        List<CombatUnit> targets,
        CombatRound round,
        GameRandom rng)
    {
        var validTargets = targets.Where(t => !t.IsDestroyed && !t.IsRetreating).ToList();
        if (validTargets.Count == 0) return;

        foreach (var shooter in shooters)
        {
            if (shooter.IsDestroyed || shooter.IsRetreating) continue;
            if (shooter.WeaponDamage <= 0) continue;

            // Target selection: focus on lowest structure HP
            var target = validTargets.OrderBy(t => t.StructureHp).First();

            float baseDamage = shooter.WeaponDamage * (0.8f + rng.NextFloat() * 0.4f);

            // 1. PD intercept phase (for missiles/railguns)
            if (shooter.WeaponType == WeaponType.Missile || shooter.WeaponType == WeaponType.Railgun)
            {
                float pdMult = WeaponsTriangle.GetMultiplier(shooter.WeaponType, DefenseType.PointDefense);
                float interceptChance = target.PointDefense * 0.01f / pdMult;
                if (rng.Chance(interceptChance))
                {
                    round.Events.Add($"{shooter.Name} missile intercepted by {target.Name} PD");
                    continue; // Shot blocked
                }
            }

            // 2. Shield absorption phase
            float shieldMult = WeaponsTriangle.GetMultiplier(shooter.WeaponType, DefenseType.Shield);
            float shieldDamage = baseDamage * shieldMult;
            if (target.ShieldHp > 0)
            {
                float absorbed = MathF.Min(shieldDamage, target.ShieldHp);
                target.ShieldHp -= absorbed;
                baseDamage -= absorbed / shieldMult; // Remaining damage continues
                if (baseDamage <= 0) continue;
            }

            // 3. Armor reduction phase
            float armorMult = WeaponsTriangle.GetMultiplier(shooter.WeaponType, DefenseType.Armor);
            float armorDamage = baseDamage * armorMult;
            if (target.ArmorHp > 0)
            {
                float absorbed = MathF.Min(armorDamage, target.ArmorHp);
                target.ArmorHp -= absorbed;
                baseDamage -= absorbed / armorMult;
                if (baseDamage <= 0) continue;
            }

            // 4. Structure damage phase
            target.StructureHp -= baseDamage;
            if (target.StructureHp < 0) target.StructureHp = 0;

            round.Events.Add($"{shooter.Name} hits {target.Name} for {baseDamage:F0} structure damage");

            if (target.IsDestroyed)
            {
                round.Events.Add($"{target.Name} destroyed!");
                validTargets.Remove(target);
                if (validTargets.Count == 0) break;
            }
        }
    }

    private void UpdateMorale(List<CombatUnit> units, GameRandom rng)
    {
        if (units.Count == 0) return;

        int destroyed = units.Count(u => u.IsDestroyed);
        float lossRatio = units.Count > 0 ? (float)destroyed / (units.Count + destroyed) : 0;

        foreach (var unit in units)
        {
            if (unit.IsDestroyed || unit.IsRetreating) continue;

            // Morale drops from casualties
            unit.Morale -= lossRatio * 15f;

            // Shield/armor damage also hurts morale
            if (unit.ShieldMax > 0)
                unit.Morale -= (1f - unit.ShieldHp / unit.ShieldMax) * 3f;

            unit.Morale = MathF.Max(unit.Morale, 0f);

            // Morale break: flee or charge
            if (unit.Morale < 20f && rng.Chance(0.3f))
                unit.IsRetreating = true;
        }
    }

    private void ProcessRetreat(List<CombatUnit> units, CombatRound round, GameRandom rng)
    {
        foreach (var unit in units)
        {
            if (!unit.IsRetreating || unit.IsDestroyed) continue;
            unit.RetreatProgress += 0.2f; // Takes ~5 rounds to escape
            if (unit.RetreatProgress >= 1.0f)
            {
                round.Events.Add($"{unit.Name} has escaped!");
            }
        }
        units.RemoveAll(u => u.IsRetreating && u.RetreatProgress >= 1.0f);
    }
}

public class CombatLog
{
    public List<CombatRound> Rounds { get; set; } = new();
    public bool AttackerVictory { get; set; }
    public int AttackersRemaining { get; set; }
    public int DefendersRemaining { get; set; }
}

public class CombatRound
{
    public int RoundNumber { get; set; }
    public List<string> Events { get; set; } = new();
    public int AttackersLost { get; set; }
    public int DefendersLost { get; set; }
}
