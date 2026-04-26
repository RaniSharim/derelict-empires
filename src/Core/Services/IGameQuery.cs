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
///
/// Why GameManager forwards to GameSystems instead of "putting derived state on the models":
///   The dependency graph is one-way — Models (POCOs) ← Systems ← GameManager (facade) ← UI.
///   No cycle. GameManager is *not* a model; it's the autoload-level facade that owns the
///   data lists and knows where derived/runtime state lives. Methods like
///   <see cref="GetFleetContributions"/> compute over <c>SalvageSystem._activities</c>,
///   which is a runtime map keyed by (empireId, poiId). Mirroring that onto
///   <c>FleetData</c> would (a) duplicate state into the JSON save format and guarantee
///   drift, and (b) couple model classes to runtime salvage logic. The forwarding
///   boilerplate here is the cost of the facade and is intentional — the alternative is
///   exposing <c>GameSystems</c> directly to UI, which is exactly what this interface
///   prevents.
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
