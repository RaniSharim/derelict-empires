using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Settlements;
using DerlictEmpires.Core.Visibility;

namespace DerlictEmpires.Core.Systems;

/// <summary>Entity kind at a POI, for System View rendering.</summary>
public enum POIEntityKind
{
    Colony,
    Outpost,
    Station,
    SalvageSite,
    Fleet
}

/// <summary>
/// Flattened view of one entity sitting at a POI. System-View-specific — hides the underlying
/// model heterogeneity (Colony vs StationData vs SalvageSiteData) behind a uniform record.
/// </summary>
public sealed class POIEntity
{
    public POIEntityKind Kind { get; init; }
    public int  Id         { get; init; }
    public string Name     { get; init; } = "";
    public int  OwnerEmpireId { get; init; } = -1;
    public int  Signature  { get; init; }
    public object Source   { get; init; } = null!; // the underlying model ref (Colony, StationData, ...)
}

/// <summary>
/// Given a system + POI, returns all entities moored at that POI with owner + signature.
/// Pure function — no Godot types, no singletons. Testable.
/// </summary>
public static class POIContentResolver
{
    public static List<POIEntity> GetEntitiesAt(
        int systemId,
        int poiId,
        IReadOnlyList<Colony>? colonies,
        IReadOnlyList<Outpost>? outposts,
        IReadOnlyList<StationData>? stations,
        IReadOnlyList<FleetData>? fleets,
        GalaxyData? galaxy)
    {
        var list = new List<POIEntity>();

        if (colonies != null)
        {
            foreach (var c in colonies.Where(x => x.SystemId == systemId && x.POIId == poiId))
                list.Add(new POIEntity
                {
                    Kind = POIEntityKind.Colony,
                    Id = c.Id, Name = c.Name, OwnerEmpireId = c.OwnerEmpireId,
                    Signature = c.TotalPopulation * 6,
                    Source = c,
                });
        }

        if (outposts != null)
        {
            foreach (var o in outposts.Where(x => x.SystemId == systemId && x.POIId == poiId))
                list.Add(new POIEntity
                {
                    Kind = POIEntityKind.Outpost,
                    Id = o.Id, Name = o.Name, OwnerEmpireId = o.OwnerEmpireId,
                    Signature = SignatureCalculator.ForOutpost(o.TotalPopulation),
                    Source = o,
                });
        }

        if (stations != null)
        {
            foreach (var s in stations.Where(x => x.SystemId == systemId && x.POIId == poiId))
                list.Add(new POIEntity
                {
                    Kind = POIEntityKind.Station,
                    Id = s.Id, Name = s.Name, OwnerEmpireId = s.OwnerEmpireId,
                    Signature = SignatureCalculator.ForStation(s),
                    Source = s,
                });
        }

        // Salvage sites are system-scoped via their POIId; no system-id filter (they belong to
        // one POI globally, and we trust the caller's systemId matches). A galaxy-scan would be
        // wasteful per card, so we only accept the caller's filtered list.
        if (galaxy != null)
        {
            foreach (var site in galaxy.SalvageSites.Where(x => x.POIId == poiId))
            {
                list.Add(new POIEntity
                {
                    Kind = POIEntityKind.SalvageSite,
                    Id = site.Id, Name = $"Salvage #{site.Id}", OwnerEmpireId = -1,
                    Signature = SignatureCalculator.ForSalvageSite(site),
                    Source = site,
                });
            }
        }

        // Fleets: v2 has no POI mooring (FleetData.CurrentSystemId only). A fleet appears on this
        // POI only if an external caller explicitly attributes it — for P2 we surface none, and
        // P5 adds MooredPOIId.
        _ = fleets;

        return list;
    }

    /// <summary>Primary entity for a POI card — the owner-first entity. Used when not expanding sub-tickets.</summary>
    public static POIEntity? Primary(List<POIEntity> entities, int viewerEmpireId)
    {
        if (entities == null || entities.Count == 0) return null;
        var own = entities.FirstOrDefault(e => e.OwnerEmpireId == viewerEmpireId);
        if (own != null) return own;
        return entities[0];
    }
}
