using Xunit;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Tests.SystemView;

public class BandClassificationTests
{
    [Theory]
    [InlineData(POIType.HabitablePlanet,  Band.Inner)]
    [InlineData(POIType.BarrenPlanet,     Band.Inner)]
    [InlineData(POIType.AsteroidField,    Band.Mid)]
    [InlineData(POIType.DebrisField,      Band.Mid)]
    [InlineData(POIType.AbandonedStation, Band.Outer)]
    [InlineData(POIType.ShipGraveyard,    Band.Outer)]
    [InlineData(POIType.Megastructure,    Band.Outer)]
    public void POIType_MapsToExpectedBand(POIType type, Band expected)
    {
        Assert.Equal(expected, POIData.BandOf(type));
    }

    [Fact]
    public void POIData_BandMatchesHelperForType()
    {
        var p = new POIData { Type = POIType.AsteroidField };
        Assert.Equal(Band.Mid, p.Band);
    }

    [Fact]
    public void POIData_OuterTypesAreSaneForScanline()
    {
        Assert.Equal(Band.Outer, new POIData { Type = POIType.Megastructure }.Band);
        Assert.Equal(Band.Outer, new POIData { Type = POIType.ShipGraveyard }.Band);
        Assert.Equal(Band.Outer, new POIData { Type = POIType.AbandonedStation }.Band);
    }
}
