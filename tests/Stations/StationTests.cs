using Xunit;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Random;
using DerlictEmpires.Core.Stations;

namespace DerlictEmpires.Tests.Stations;

public class StationTests
{
    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 4)]
    [InlineData(5, 6)]
    public void MaxModuleSlots_EqualsSize_Plus1(int tier, int expectedSlots)
    {
        var station = new Station { SizeTier = tier };
        Assert.Equal(expectedSlots, station.MaxModuleSlots);
    }

    [Fact]
    public void InstallModule_Success()
    {
        var station = new Station { SizeTier = 2, IsConstructed = true };
        var module = new ShipyardModule();
        Assert.True(station.InstallModule(module));
        Assert.True(station.HasShipyard);
        Assert.Equal(1, station.UsedModuleSlots);
    }

    [Fact]
    public void InstallModule_FailsWhenFull()
    {
        var station = new Station { SizeTier = 1, IsConstructed = true }; // 2 slots
        station.InstallModule(new ShipyardModule());
        station.InstallModule(new DefenseModule());
        // No room left
        Assert.False(station.CanInstallModule(new SensorModule()));
    }

    [Fact]
    public void InstallModule_FailsWhenNotConstructed()
    {
        var station = new Station { SizeTier = 3, IsConstructed = false };
        Assert.False(station.CanInstallModule(new ShipyardModule()));
    }

    [Fact]
    public void RemoveModule_Works()
    {
        var station = new Station { SizeTier = 2, IsConstructed = true };
        station.InstallModule(new ShipyardModule());
        Assert.True(station.HasShipyard);

        Assert.True(station.RemoveModule(StationModuleType.Shipyard));
        Assert.False(station.HasShipyard);
    }

    [Fact]
    public void DefenseStats_AggregateCorrectly()
    {
        var station = new Station { SizeTier = 3, IsConstructed = true };
        station.InstallModule(new DefenseModule { WeaponDamage = 10, ShieldHp = 20, ArmorHp = 15 });
        station.InstallModule(new DefenseModule { WeaponDamage = 15, ShieldHp = 30, ArmorHp = 10 });

        Assert.Equal(25f, station.TotalWeaponDamage);
        Assert.Equal(50f, station.TotalShieldHp);
        Assert.Equal(25f, station.TotalArmorHp);
    }

    [Fact]
    public void UpgradeSize_IncreasesSlots()
    {
        var system = new StationSystem();
        var station = new Station { SizeTier = 1 };
        system.AddStation(station);

        int slotsBefore = station.MaxModuleSlots;
        Assert.True(system.UpgradeSize(station));
        Assert.Equal(slotsBefore + 1, station.MaxModuleSlots);
    }

    [Fact]
    public void UpgradeSize_FailsAtMax()
    {
        var system = new StationSystem();
        var station = new Station { SizeTier = 5 };
        Assert.False(system.UpgradeSize(station));
    }

    [Fact]
    public void Construction_ProgressesToCompletion()
    {
        var system = new StationSystem();
        var job = system.StartConstruction(0, 0, 0, 99, constructionCost: 100);

        bool constructed = false;
        system.StationConstructed += _ => constructed = true;

        // Invest 50 points twice
        system.ProcessConstructionTick(99, 50);
        Assert.False(constructed);

        system.ProcessConstructionTick(99, 50);
        Assert.True(constructed);

        var station = system.Stations.First(s => s.Id == job.StationId);
        Assert.True(station.IsConstructed);
    }

    [Fact]
    public void QueueModuleInstall_Works()
    {
        var system = new StationSystem();
        var station = new Station { SizeTier = 2, IsConstructed = true };
        system.AddStation(station);

        var module = new SensorModule();
        Assert.True(system.QueueModuleInstall(station, module));
        Assert.Equal(1, station.ModuleQueue.Count);

        bool installed = false;
        system.ModuleInstalled += (_, _) => installed = true;

        // Process enough production to complete (SensorModule.InstallCost = 90)
        system.ProcessModuleTick(90);
        Assert.True(installed);
        Assert.True(station.Modules.Any(m => m.Type == StationModuleType.Sensors));
    }
}

public class PrecursorStationTests
{
    [Fact]
    public void Claim_SetsOwner()
    {
        var station = new PrecursorStation { Color = PrecursorColor.Red, TechTier = 2 };
        Assert.True(station.Claim(5));
        Assert.Equal(5, station.OwnerEmpireId);
        Assert.Equal(PrecursorStation.ClaimState.Claimed, station.State);
        Assert.False(station.IsConstructed);
    }

    [Fact]
    public void Claim_FailsIfAlreadyClaimed()
    {
        var station = new PrecursorStation { Color = PrecursorColor.Blue };
        station.Claim(1);
        Assert.False(station.Claim(2));
    }

    [Fact]
    public void Repair_MakesStationFunctional()
    {
        var station = new PrecursorStation { Color = PrecursorColor.Green };
        station.Modules.Add(new ShipyardModule());
        station.Claim(1);
        Assert.True(station.CompleteRepair());
        Assert.True(station.IsConstructed);
        Assert.True(station.HasShipyard);
    }

    [Fact]
    public void Scavenge_YieldsComponents_DestroysStation()
    {
        var station = new PrecursorStation
        {
            Color = PrecursorColor.Gold,
            ScavengeYieldBasic = 15,
            ScavengeYieldAdvanced = 4
        };
        station.Modules.Add(new DefenseModule());

        var (basic, advanced) = station.Scavenge();
        Assert.Equal(15, basic);
        Assert.Equal(4, advanced);
        Assert.Empty(station.Modules);
        Assert.Equal(PrecursorStation.ClaimState.Scavenged, station.State);
    }

    [Fact]
    public void Scavenge_FailsIfAlreadyScavenged()
    {
        var station = new PrecursorStation { Color = PrecursorColor.Purple };
        station.Scavenge();
        var (basic, advanced) = station.Scavenge();
        Assert.Equal(0, basic);
        Assert.Equal(0, advanced);
    }

    [Fact]
    public void GeneratePrecursorStation_HasModules()
    {
        var rng = new GameRandom(42);
        var station = StationSystem.GeneratePrecursorStation(0, PrecursorColor.Red, 3, 0, 0, rng);

        Assert.True(station.Modules.Count > 0, "Should have pre-filled modules");
        Assert.Equal(PrecursorColor.Red, station.Color);
        Assert.Equal(3, station.TechTier);
        Assert.True(station.HazardLevel > 0);
    }

    [Fact]
    public void GeneratePrecursorStation_Deterministic()
    {
        var s1 = StationSystem.GeneratePrecursorStation(0, PrecursorColor.Blue, 2, 0, 0, new GameRandom(99));
        var s2 = StationSystem.GeneratePrecursorStation(0, PrecursorColor.Blue, 2, 0, 0, new GameRandom(99));

        Assert.Equal(s1.Modules.Count, s2.Modules.Count);
        for (int i = 0; i < s1.Modules.Count; i++)
            Assert.Equal(s1.Modules[i].Type, s2.Modules[i].Type);
    }
}
