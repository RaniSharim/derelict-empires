using System.Collections.Generic;
using Xunit;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Visibility;

namespace DerlictEmpires.Tests.Visibility;

public class SignatureCalculatorTests
{
    [Fact]
    public void Colony_SignatureScalesWithPopulation()
    {
        var small = new ColonyData { Population = 2 };
        var large = new ColonyData { Population = 15 };
        Assert.Equal(12, SignatureCalculator.ForColony(small));
        Assert.Equal(90, SignatureCalculator.ForColony(large));
    }

    [Fact]
    public void Outpost_SignatureIsPopTimes3()
    {
        Assert.Equal(9,  SignatureCalculator.ForOutpost(3));
        Assert.Equal(12, SignatureCalculator.ForOutpost(4));
    }

    [Fact]
    public void Station_SignatureCombinesTierAndModules()
    {
        var tier3NoModules = new StationData { SizeTier = 3, InstalledModules = new List<string>() };
        var tier3TwoMods   = new StationData { SizeTier = 3, InstalledModules = new List<string> { "Sensors", "Shipyard" } };
        Assert.Equal(45, SignatureCalculator.ForStation(tier3NoModules));
        Assert.Equal(49, SignatureCalculator.ForStation(tier3TwoMods));
    }

    [Fact]
    public void SalvageSite_SignatureReflectsHazard()
    {
        var low  = new SalvageSiteData { HazardLevel = 0.25f };
        var high = new SalvageSiteData { HazardLevel = 2.5f };
        Assert.Equal(5,  SignatureCalculator.ForSalvageSite(low));
        Assert.Equal(50, SignatureCalculator.ForSalvageSite(high));
    }

    [Fact]
    public void Fleet_SignatureIsShipCountTimes4()
    {
        var f = new FleetData { ShipIds = new List<int> { 1, 2, 3 } };
        Assert.Equal(12, SignatureCalculator.ForFleet(f));
    }

    [Fact]
    public void AllMethods_NullSafe()
    {
        Assert.Equal(0, SignatureCalculator.ForColony(null!));
        Assert.Equal(0, SignatureCalculator.ForStation(null!));
        Assert.Equal(0, SignatureCalculator.ForSalvageSite(null!));
        Assert.Equal(0, SignatureCalculator.ForFleet(null!));
    }
}
