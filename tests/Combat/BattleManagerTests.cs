using System.Collections.Generic;
using System.Linq;
using Xunit;
using DerlictEmpires.Core.Combat;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;

namespace DerlictEmpires.Tests.Combat;

public class BattleManagerTests
{
    private static (BattleManager mgr, FleetData atk, FleetData def,
        EmpireData atkEmp, EmpireData defEmp, Dictionary<int, ShipInstanceData> ships) BuildSetup(int seed = 123)
    {
        var rng = new GameRandom(seed).DeriveChild("battles");
        var mgr = new BattleManager(rng);

        var atkEmp = new EmpireData { Id = 1, Name = "Player", IsHuman = true, Affinity = PrecursorColor.Red };
        var defEmp = new EmpireData { Id = 2, Name = "Raiders", Affinity = PrecursorColor.Purple };

        var atk = new FleetData { Id = 10, Name = "Player Fleet", OwnerEmpireId = 1, CurrentSystemId = 0 };
        var def = new FleetData { Id = 20, Name = "Raider Fleet", OwnerEmpireId = 2, CurrentSystemId = 0 };

        var ships = new Dictionary<int, ShipInstanceData>();
        for (int i = 0; i < 2; i++)
        {
            var s = new ShipInstanceData
            {
                Id = 100 + i, Name = $"P{i}", OwnerEmpireId = 1,
                SizeClass = ShipSizeClass.Cruiser,
                Role = "Brawler",
                MaxHp = 200, CurrentHp = 200, FleetId = atk.Id,
                ShipDesignId = "design_player",
            };
            ships[s.Id] = s;
            atk.ShipIds.Add(s.Id);
        }
        for (int i = 0; i < 2; i++)
        {
            var s = new ShipInstanceData
            {
                Id = 200 + i, Name = $"R{i}", OwnerEmpireId = 2,
                SizeClass = ShipSizeClass.Fighter,
                Role = "Fighter",
                MaxHp = 40, CurrentHp = 40, FleetId = def.Id,
                ShipDesignId = "design_raider",
            };
            ships[s.Id] = s;
            def.ShipIds.Add(s.Id);
        }

        return (mgr, atk, def, atkEmp, defEmp, ships);
    }

    [Fact]
    public void StartBattle_PopulatesUnitsFromFleetShips()
    {
        var (mgr, atk, def, atkEmp, defEmp, ships) = BuildSetup();
        int id = mgr.StartBattle(atk, atkEmp, def, defEmp, ships, systemId: 0);
        var battle = mgr.GetBattle(id);

        Assert.NotNull(battle);
        Assert.Equal(2, battle!.Attackers.Count);
        Assert.Equal(2, battle.Defenders.Count);
        Assert.Equal(BattleState.Running, battle.State);
    }

    [Fact]
    public void ProcessTick_RunsCombat_AndEventuallyEnds()
    {
        var (mgr, atk, def, atkEmp, defEmp, ships) = BuildSetup();
        int id = mgr.StartBattle(atk, atkEmp, def, defEmp, ships, systemId: 0);

        CombatResult? outcome = null;
        mgr.BattleEnded += (bid, res) => { if (bid == id) outcome = res; };

        // Ticks advance; rounds fire every 8th tick. 200 ticks = ~25 rounds → plenty to resolve.
        for (int t = 0; t < 200 && !outcome.HasValue; t++)
            mgr.ProcessTick(0.1f);

        Assert.True(outcome.HasValue, "battle should have ended");
        Assert.NotEqual(CombatResult.Draw, outcome!.Value);
        Assert.Equal(BattleState.Ended, mgr.GetBattle(id)!.State);
    }

    [Fact]
    public void Deterministic_SameSeedProducesSameOutcome()
    {
        CombatResult? Run(int seed)
        {
            var (mgr, atk, def, atkEmp, defEmp, ships) = BuildSetup(seed);
            int id = mgr.StartBattle(atk, atkEmp, def, defEmp, ships, systemId: 0);
            CombatResult? result = null;
            mgr.BattleEnded += (bid, r) => { if (bid == id) result = r; };
            for (int t = 0; t < 500 && !result.HasValue; t++)
                mgr.ProcessTick(0.1f);
            return result;
        }

        var a = Run(42);
        var b = Run(42);
        Assert.Equal(a, b);
    }

    [Fact]
    public void PlayerRetreat_EndsBattleAsRetreat()
    {
        var (mgr, atk, def, atkEmp, defEmp, ships) = BuildSetup();
        int id = mgr.StartBattle(atk, atkEmp, def, defEmp, ships, systemId: 0);
        mgr.RequestPlayerRetreat(id);

        CombatResult? outcome = null;
        mgr.BattleEnded += (bid, r) => { if (bid == id) outcome = r; };
        for (int t = 0; t < 100 && !outcome.HasValue; t++)
            mgr.ProcessTick(0.1f);

        Assert.Equal(CombatResult.Retreat, outcome);
    }

    [Fact]
    public void Aggregate_GroupsByRole()
    {
        var (mgr, atk, def, atkEmp, defEmp, ships) = BuildSetup();
        int id = mgr.StartBattle(atk, atkEmp, def, defEmp, ships, systemId: 0);

        var agg = mgr.GetAttackerAggregate(id);
        Assert.Equal(2, agg.TotalShips);
        Assert.Single(agg.Roles);
        Assert.All(agg.Roles, r => Assert.Equal(2, r.ShipCount));
    }

    [Fact]
    public void DesignPerformance_SeedsEntryPerShipDesign()
    {
        var (mgr, atk, def, atkEmp, defEmp, ships) = BuildSetup();
        int id = mgr.StartBattle(atk, atkEmp, def, defEmp, ships, systemId: 0);

        var battle = mgr.GetBattle(id)!;
        Assert.True(battle.PerDesignPerformance.ContainsKey("design_player"));
        Assert.Equal(2, battle.PerDesignPerformance["design_player"].ShipsEngaged);
    }
}
