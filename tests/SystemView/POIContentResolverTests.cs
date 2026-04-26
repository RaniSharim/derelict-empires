using System.Collections.Generic;
using Xunit;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Settlements;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Tests.SystemView;

public class POIContentResolverTests
{
    private static GalaxyData MakeGalaxy() => new();

    [Fact]
    public void EmptyPOI_ReturnsEmpty()
    {
        var list = POIContentResolver.GetEntitiesAt(1, 10,
            colonies: null, outposts: null, stations: null, fleets: null, galaxy: MakeGalaxy());
        Assert.Empty(list);
    }

    [Fact]
    public void ColonyAtPOI_IsReturnedWithCorrectOwnerAndSignature()
    {
        var colony = new Colony { Id = 1, Name = "Gaia", OwnerEmpireId = 7, SystemId = 3, POIId = 42 };
        colony.PopGroups.Add(new PopGroup { Count = 4, Allocation = WorkPool.Production });
        var list = POIContentResolver.GetEntitiesAt(3, 42,
            colonies: new[] { colony }, outposts: null, stations: null, fleets: null, galaxy: MakeGalaxy());
        var e = Assert.Single(list);
        Assert.Equal(POIEntityKind.Colony, e.Kind);
        Assert.Equal(7, e.OwnerEmpireId);
        Assert.Equal(24, e.Signature); // 4 pops × 6
    }

    [Fact]
    public void SharedPOI_ReturnsBothEntitiesInOrder()
    {
        var ours = new Outpost { Id = 1, OwnerEmpireId = 1, SystemId = 3, POIId = 10, Name = "Mine Alpha" };
        ours.PopGroups.Add(new PopGroup { Count = 3, Allocation = WorkPool.Mining });
        var foreign = new StationData { Id = 20, OwnerEmpireId = 2, SystemId = 3, POIId = 10, SizeTier = 2 };
        var list = POIContentResolver.GetEntitiesAt(3, 10,
            colonies: null, outposts: new[] { ours }, stations: new[] { foreign },
            fleets: null, galaxy: MakeGalaxy());
        Assert.Equal(2, list.Count);
        Assert.Equal(POIEntityKind.Outpost, list[0].Kind);
        Assert.Equal(POIEntityKind.Station, list[1].Kind);
    }

    [Fact]
    public void WrongSystem_FiltersEntitiesOut()
    {
        var colony = new Colony { Id = 1, OwnerEmpireId = 1, SystemId = 99, POIId = 42 };
        var list = POIContentResolver.GetEntitiesAt(3, 42,
            colonies: new[] { colony }, outposts: null, stations: null, fleets: null, galaxy: MakeGalaxy());
        Assert.Empty(list);
    }

    [Fact]
    public void SalvageSite_SurfacedFromGalaxy()
    {
        var galaxy = new GalaxyData();
        galaxy.SalvageSites.Add(new SalvageSiteData
        {
            Id = 5, POIId = 42, Tier = 3,
        });
        var list = POIContentResolver.GetEntitiesAt(3, 42,
            colonies: null, outposts: null, stations: null, fleets: null, galaxy: galaxy);
        var e = Assert.Single(list);
        Assert.Equal(POIEntityKind.SalvageSite, e.Kind);
        Assert.Equal(30, e.Signature); // tier 3 × 10
    }

    [Fact]
    public void Primary_ReturnsOwnedEntityFirst_EvenWhenOrderedLast()
    {
        var foreign = new StationData { Id = 20, OwnerEmpireId = 2, SystemId = 3, POIId = 10 };
        var ours    = new Outpost { Id = 1, OwnerEmpireId = 1, SystemId = 3, POIId = 10 };
        // Deliberately pass station first to simulate foreign-first ordering.
        var list = POIContentResolver.GetEntitiesAt(3, 10,
            colonies: null, outposts: new[] { ours }, stations: new[] { foreign },
            fleets: null, galaxy: MakeGalaxy());
        var primary = POIContentResolver.Primary(list, viewerEmpireId: 1);
        Assert.NotNull(primary);
        Assert.Equal(POIEntityKind.Outpost, primary!.Kind);
    }

    [Fact]
    public void Primary_NullWhenListEmpty()
    {
        Assert.Null(POIContentResolver.Primary(new List<POIEntity>(), viewerEmpireId: 1));
    }
}
