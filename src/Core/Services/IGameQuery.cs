using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Tech;

namespace DerlictEmpires.Core.Services;

/// <summary>
/// Read-only query surface for UI panels. The interface exists so panels can be
/// constructed against a fake during F6/test mode without pulling in
/// <c>MainScene</c> or <c>GameSystems</c> references.
///
/// Implementations:
///   - <c>GameManager</c> (production) — owns the data lists; forwards salvage/tech
///     queries to the live <c>GameSystems</c> set by <c>GameSystemsHost</c>.
///   - test fakes — return canned data, ignore intent events.
///
/// UI rule: read through <see cref="IGameQuery"/>, write through <c>EventBus</c>
/// intent events. Panels never call into systems directly.
/// </summary>
public interface IGameQuery
{
    EmpireData? PlayerEmpire { get; }
    EmpireResearchState? PlayerResearchState { get; }
    IReadOnlyList<FleetData> Fleets { get; }
    IReadOnlyList<EmpireData> Empires { get; }
    IReadOnlyDictionary<int, ShipInstanceData> ShipsById { get; }
    GalaxyData? Galaxy { get; }

    float GetSystemCapability(int poiId, SiteActivity type);
    int GetSystemActiveCount(int poiId, SiteActivity type);
    SalvageSiteData? GetSalvageSite(int siteId);
    POIData? FindPOI(int poiId, out int systemId);
    SiteActivity GetSiteActivity(int empireId, int poiId);

    /// <summary>Current move order for a fleet, or null if none.</summary>
    FleetOrder? GetFleetOrder(int fleetId);

    TechTreeRegistry? TechRegistry { get; }
    EmpireResearchState? GetResearchState(int empireId);
}
