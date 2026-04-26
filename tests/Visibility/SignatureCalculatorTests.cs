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
    public void SalvageSite_SignatureReflectsTier()
    {
        var low  = new SalvageSiteData { Tier = 1 };
        var high = new SalvageSiteData { Tier = 5 };
        Assert.Equal(10, SignatureCalculator.ForSalvageSite(low));
        Assert.Equal(50, SignatureCalculator.ForSalvageSite(high));
    }

    [Fact]
    public void SalvageSite_MultiColorBonus()
    {
        var single = new SalvageSiteData { Tier = 3, Colors = new() { DerlictEmpires.Core.Enums.PrecursorColor.Red } };
        var multi  = new SalvageSiteData { Tier = 3, Colors = new()
        {
            DerlictEmpires.Core.Enums.PrecursorColor.Red,
            DerlictEmpires.Core.Enums.PrecursorColor.Blue,
        } };
        Assert.Equal(30, SignatureCalculator.ForSalvageSite(single));
        Assert.Equal(35, SignatureCalculator.ForSalvageSite(multi));
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
