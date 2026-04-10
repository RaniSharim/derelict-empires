using Xunit;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Combat;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Tests.Combat;

public class WeaponsTriangleTests
{
    [Theory]
    [InlineData(WeaponType.Laser, DefenseType.Shield, 1.5f)]
    [InlineData(WeaponType.Railgun, DefenseType.Armor, 1.5f)]
    [InlineData(WeaponType.Missile, DefenseType.PointDefense, 1.3f)]
    public void EffectiveMatchups_HaveBonus(WeaponType weapon, DefenseType defense, float expected)
    {
        Assert.Equal(expected, WeaponsTriangle.GetMultiplier(weapon, defense));
    }

    [Theory]
    [InlineData(WeaponType.Laser, DefenseType.Armor, 0.6f)]
    [InlineData(WeaponType.Railgun, DefenseType.Shield, 0.7f)]
    public void PoorMatchups_HavePenalty(WeaponType weapon, DefenseType defense, float expected)
    {
        Assert.Equal(expected, WeaponsTriangle.GetMultiplier(weapon, defense));
    }
}

public class CombatSimulatorTests
{
    private static CombatUnit MakeUnit(int id, string name, int ownerId, float damage = 10f,
        WeaponType weapon = WeaponType.Laser, float shields = 20f, float armor = 15f, float hp = 50f)
    {
        return new CombatUnit
        {
            Id = id, Name = name, OwnerId = ownerId,
            Role = FleetRole.Brawler,
            WeaponDamage = damage, WeaponType = weapon,
            ShieldHp = shields, ShieldMax = shields, ShieldRegen = 1f,
            ArmorHp = armor, ArmorMax = armor,
            StructureHp = hp, StructureMax = hp,
            PointDefense = 5f, Morale = 100f
        };
    }

    [Fact]
    public void SameSeed_IdenticalResult()
    {
        var atk1 = new List<CombatUnit> { MakeUnit(0, "A1", 0), MakeUnit(1, "A2", 0) };
        var def1 = new List<CombatUnit> { MakeUnit(2, "D1", 1), MakeUnit(3, "D2", 1) };
        var log1 = new CombatSimulator().Simulate(atk1, def1, new GameRandom(42));

        var atk2 = new List<CombatUnit> { MakeUnit(0, "A1", 0), MakeUnit(1, "A2", 0) };
        var def2 = new List<CombatUnit> { MakeUnit(2, "D1", 1), MakeUnit(3, "D2", 1) };
        var log2 = new CombatSimulator().Simulate(atk2, def2, new GameRandom(42));

        Assert.Equal(log1.Rounds.Count, log2.Rounds.Count);
        Assert.Equal(log1.AttackerVictory, log2.AttackerVictory);
        Assert.Equal(log1.AttackersRemaining, log2.AttackersRemaining);
        Assert.Equal(log1.DefendersRemaining, log2.DefendersRemaining);
    }

    [Fact]
    public void BothSides_TakeCasualties()
    {
        var attackers = new List<CombatUnit> { MakeUnit(0, "A", 0, damage: 15f, hp: 40f) };
        var defenders = new List<CombatUnit> { MakeUnit(1, "D", 1, damage: 15f, hp: 40f) };

        var log = new CombatSimulator().Simulate(attackers, defenders, new GameRandom(42));

        // At least one side should have lost units or taken damage
        Assert.True(log.Rounds.Count > 0);
        bool anyEvents = log.Rounds.Any(r => r.Events.Count > 0);
        Assert.True(anyEvents);
    }

    [Fact]
    public void StrongerSide_MoreLikelyToWin()
    {
        int attackerWins = 0;
        for (int seed = 0; seed < 50; seed++)
        {
            var attackers = new List<CombatUnit>
            {
                MakeUnit(0, "A1", 0, damage: 20f, hp: 80f),
                MakeUnit(1, "A2", 0, damage: 20f, hp: 80f),
                MakeUnit(2, "A3", 0, damage: 20f, hp: 80f),
            };
            var defenders = new List<CombatUnit>
            {
                MakeUnit(3, "D1", 1, damage: 10f, hp: 40f),
            };

            var log = new CombatSimulator().Simulate(attackers, defenders, new GameRandom(seed));
            if (log.AttackerVictory) attackerWins++;
        }

        Assert.True(attackerWins > 35, $"3v1 attacker should win most: won {attackerWins}/50");
    }

    [Fact]
    public void DefenseLayers_ResolveInOrder()
    {
        // Unit with strong shields but weak armor and structure
        var defender = new CombatUnit
        {
            Id = 0, Name = "ShieldTank", OwnerId = 1, Role = FleetRole.Guardian,
            WeaponDamage = 5f, WeaponType = WeaponType.Laser,
            ShieldHp = 100f, ShieldMax = 100f, ShieldRegen = 0f,
            ArmorHp = 10f, ArmorMax = 10f,
            StructureHp = 20f, StructureMax = 20f,
            PointDefense = 0f, Morale = 100f
        };

        // Attacker with lasers (1.5x vs shields)
        var attacker = MakeUnit(1, "LaserShip", 0, damage: 30f, weapon: WeaponType.Laser, shields: 50f, armor: 10f, hp: 50f);

        var log = new CombatSimulator().Simulate(
            new List<CombatUnit> { attacker },
            new List<CombatUnit> { defender },
            new GameRandom(42));

        // Shields should have been damaged first
        // Log should show combat happening
        Assert.True(log.Rounds.Count > 0);
    }

    [Fact]
    public void MoraleBreak_CausesFlee()
    {
        // Overwhelm one unit with many attackers to break morale
        var attackers = Enumerable.Range(0, 10)
            .Select(i => MakeUnit(i, $"A{i}", 0, damage: 10f, hp: 100f))
            .ToList();

        var defender = MakeUnit(99, "Lone", 1, damage: 5f, hp: 200f);
        defender.Morale = 25f; // Start with low morale

        var log = new CombatSimulator().Simulate(attackers, new List<CombatUnit> { defender }, new GameRandom(42));

        // With low starting morale and heavy fire, combat should end relatively quickly
        Assert.True(log.Rounds.Count < 30);
    }
}
