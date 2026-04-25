using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Settlements;
using DerlictEmpires.Core.Stations;
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

    /// <summary>Exploration state of a POI for an empire (Undiscovered/Discovered/Surveyed).</summary>
    ExplorationState GetExplorationState(int empireId, int poiId);

    /// <summary>Scan progress for an empire on a POI (raw — caller divides by site difficulty).</summary>
    float GetScanProgress(int empireId, int poiId);

    /// <summary>Fleets contributing to the current activity at a POI (empire-owned, in-system, capable).</summary>
    IReadOnlyList<FleetData> GetContributingFleets(int empireId, int poiId);

    /// <summary>POI ids the fleet is currently contributing to, split by activity type.</summary>
    (IReadOnlyList<int> scanning, IReadOnlyList<int> extracting) GetFleetContributions(int fleetId);

    /// <summary>Live runtime colony list (settlement system objects, not DTOs). Empty if not loaded.</summary>
    IReadOnlyList<Colony> LiveColonies { get; }

    /// <summary>Live runtime outpost list. Empty if settlements not yet loaded.</summary>
    IReadOnlyList<Outpost> LiveOutposts { get; }

    /// <summary>Live runtime station list (mirrors StationDatas DTOs). Empty if stations not yet loaded.</summary>
    IReadOnlyList<Station> LiveStations { get; }

    TechTreeRegistry? TechRegistry { get; }
    EmpireResearchState? GetResearchState(int empireId);
}
