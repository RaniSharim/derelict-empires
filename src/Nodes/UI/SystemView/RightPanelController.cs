using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Settlements;
using DerlictEmpires.Core.Stations;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// Owns the right-panel body and swaps between empty-state and per-entity variants based on
/// Selected Entity. Subscribes to EntitySelected / EntityDeselected / POIDeselected.
/// See design/in_system_design.md §7.
/// </summary>
public partial class RightPanelController : MarginContainer
{
    private VBoxContainer _content = null!;
    private EntityTabStrip?        _tabStrip;
    private EmptyStateDashboard?   _emptyState;
    private ColonyEntityPanel?     _colonyPanel;
    private OutpostEntityPanel?    _outpostPanel;
    private StationEntityPanel?    _stationPanel;
    private SalvageEntityPanel?    _salvagePanel;
    private EnemyEntityPanel?      _enemyPanel;

    private int _selectedPoiId = -1;
    private int _selectedEntityId = -1;

    // Context bundle — updated every time the hosting scene re-applies it.
    private StarSystemData? _system;
    private IReadOnlyList<Colony>?      _colonies;
    private IReadOnlyList<Outpost>?     _outposts;
    private IReadOnlyList<StationData>? _stations;
    private IReadOnlyList<Station>?     _stationsRuntime;
    private GalaxyData?                 _galaxy;
    private int _viewerEmpireId = -1;

    public override void _Ready()
    {
        AddThemeConstantOverride("margin_left", 14);
        AddThemeConstantOverride("margin_right", 14);
        AddThemeConstantOverride("margin_top", 12);
        AddThemeConstantOverride("margin_bottom", 12);

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 8);
        AddChild(_content);

        if (EventBus.Instance != null)
        {
            EventBus.Instance.EntitySelected   += OnEntitySelected;
            EventBus.Instance.EntityDeselected += OnEntityDeselected;
            EventBus.Instance.POISelected      += OnPOISelectedTracked;
            EventBus.Instance.POIDeselected    += OnPOIDeselectedTracked;
        }

        ShowEmptyState();
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.EntitySelected   -= OnEntitySelected;
            EventBus.Instance.EntityDeselected -= OnEntityDeselected;
            EventBus.Instance.POISelected      -= OnPOISelectedTracked;
            EventBus.Instance.POIDeselected    -= OnPOIDeselectedTracked;
        }
    }

    public void SetContext(
        StarSystemData? system,
        IReadOnlyList<Colony>? colonies,
        IReadOnlyList<Outpost>? outposts,
        IReadOnlyList<StationData>? stations,
        IReadOnlyList<Station>? stationsRuntime,
        GalaxyData? galaxy,
        int viewerEmpireId)
    {
        _system = system;
        _colonies = colonies;
        _outposts = outposts;
        _stations = stations;
        _stationsRuntime = stationsRuntime;
        _galaxy = galaxy;
        _viewerEmpireId = viewerEmpireId;
        if (_emptyState != null) RefreshEmpty();
    }

    private void Clear()
    {
        foreach (var c in _content.GetChildren()) c.QueueFree();
        _tabStrip = null;
        _emptyState = null;
        _colonyPanel = null;
        _outpostPanel = null;
        _stationPanel = null;
        _salvagePanel = null;
        _enemyPanel = null;
    }

    private void OnPOISelectedTracked(int poiId)
    {
        _selectedPoiId = poiId;
        // Tab strip refresh piggybacks on entity-panel rebuild via EnsureTabStrip.
    }

    private void OnPOIDeselectedTracked()
    {
        _selectedPoiId = -1;
    }

    private IReadOnlyList<POIEntity> ResolveCurrentPoiEntities()
    {
        if (_selectedPoiId < 0 || _system == null) return System.Array.Empty<POIEntity>();
        return POIContentResolver.GetEntitiesAt(
            _system.Id, _selectedPoiId, _colonies, _outposts, _stations,
            fleets: null, galaxy: _galaxy);
    }

    /// <summary>Add the tab strip at the top of the panel if the current POI is shared.</summary>
    private void EnsureTabStrip()
    {
        var entities = ResolveCurrentPoiEntities();
        if (entities.Count <= 1) return;

        _tabStrip = new EntityTabStrip();
        _content.AddChild(_tabStrip);
        _content.MoveChild(_tabStrip, 0);
        _tabStrip.Populate(entities, _viewerEmpireId, _selectedPoiId, _selectedEntityId);
    }

    private void ShowEmptyState()
    {
        Clear();
        _emptyState = new EmptyStateDashboard();
        _content.AddChild(_emptyState);
        RefreshEmpty();
    }

    private void RefreshEmpty()
    {
        _emptyState?.Populate(_system, _colonies, _outposts, _stations, _viewerEmpireId);
    }

    private void OnEntitySelected(string kind, int entityId, int poiId)
    {
        _selectedEntityId = entityId;
        _selectedPoiId    = poiId;
        // Owner check for Enemy intel variant: any foreign-owned entity renders as Enemy
        // regardless of kind (colonies, stations, outposts all surface as intel when foreign).
        int owner = ResolveOwner(kind, entityId);
        if (owner >= 0 && owner != _viewerEmpireId)
        {
            var foreignEntity = BuildForeignEntity(kind, entityId);
            if (foreignEntity != null) { ShowEnemy(foreignEntity); return; }
        }

        if (kind == POIEntityKind.Colony.ToString())
        {
            var colony = _colonies?.FirstOrDefault(c => c.Id == entityId);
            if (colony != null) { ShowColony(colony); return; }
        }
        else if (kind == POIEntityKind.Outpost.ToString())
        {
            var outpost = _outposts?.FirstOrDefault(o => o.Id == entityId);
            if (outpost != null) { ShowOutpost(outpost); return; }
        }
        else if (kind == POIEntityKind.Station.ToString())
        {
            var station = _stationsRuntime?.FirstOrDefault(s => s.Id == entityId);
            if (station != null) { ShowStation(station); return; }
        }
        else if (kind == POIEntityKind.SalvageSite.ToString())
        {
            var site = _galaxy?.GetSalvageSite(entityId);
            if (site != null) { ShowSalvage(site); return; }
        }
        ShowEmptyState();
    }

    private int ResolveOwner(string kind, int entityId)
    {
        if (kind == POIEntityKind.Colony.ToString())
            return _colonies?.FirstOrDefault(c => c.Id == entityId)?.OwnerEmpireId ?? -1;
        if (kind == POIEntityKind.Outpost.ToString())
            return _outposts?.FirstOrDefault(o => o.Id == entityId)?.OwnerEmpireId ?? -1;
        if (kind == POIEntityKind.Station.ToString())
            return _stationsRuntime?.FirstOrDefault(s => s.Id == entityId)?.OwnerEmpireId ?? -1;
        return -1;
    }

    private POIEntity? BuildForeignEntity(string kind, int entityId)
    {
        if (kind == POIEntityKind.Colony.ToString())
        {
            var c = _colonies?.FirstOrDefault(x => x.Id == entityId);
            if (c == null) return null;
            return new POIEntity
            {
                Kind = POIEntityKind.Colony, Id = c.Id, Name = c.Name,
                OwnerEmpireId = c.OwnerEmpireId, Signature = c.TotalPopulation * 6,
            };
        }
        if (kind == POIEntityKind.Outpost.ToString())
        {
            var o = _outposts?.FirstOrDefault(x => x.Id == entityId);
            if (o == null) return null;
            return new POIEntity
            {
                Kind = POIEntityKind.Outpost, Id = o.Id, Name = o.Name,
                OwnerEmpireId = o.OwnerEmpireId, Signature = o.TotalPopulation * 3,
            };
        }
        if (kind == POIEntityKind.Station.ToString())
        {
            var s = _stationsRuntime?.FirstOrDefault(x => x.Id == entityId);
            if (s == null) return null;
            return new POIEntity
            {
                Kind = POIEntityKind.Station, Id = s.Id, Name = s.Name,
                OwnerEmpireId = s.OwnerEmpireId, Signature = s.SizeTier * 15 + s.Modules.Count * 2,
            };
        }
        return null;
    }

    private void OnEntityDeselected()
    {
        _selectedEntityId = -1;
        ShowEmptyState();
    }

    private void ShowColony(Colony colony)
    {
        Clear();
        _colonyPanel = new ColonyEntityPanel();
        _content.AddChild(_colonyPanel);
        _colonyPanel.Populate(colony);
        EnsureTabStrip();
    }

    private void ShowOutpost(Outpost outpost)
    {
        Clear();
        _outpostPanel = new OutpostEntityPanel();
        _content.AddChild(_outpostPanel);
        _outpostPanel.Populate(outpost);
        EnsureTabStrip();
    }

    private void ShowStation(Station station)
    {
        Clear();
        _stationPanel = new StationEntityPanel();
        _content.AddChild(_stationPanel);
        _stationPanel.Populate(station);
        EnsureTabStrip();
    }

    private void ShowSalvage(SalvageSiteData site)
    {
        Clear();
        _salvagePanel = new SalvageEntityPanel();
        _content.AddChild(_salvagePanel);
        _salvagePanel.Populate(site);
        EnsureTabStrip();
    }

    private void ShowEnemy(POIEntity entity)
    {
        Clear();
        _enemyPanel = new EnemyEntityPanel();
        _content.AddChild(_enemyPanel);
        _enemyPanel.Populate(entity);
        EnsureTabStrip();
    }
}
