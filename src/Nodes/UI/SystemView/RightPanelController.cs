using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Settlements;
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
    private EmptyStateDashboard? _emptyState;
    private ColonyEntityPanel?   _colonyPanel;

    // Context bundle — updated every time the hosting scene re-applies it.
    private StarSystemData? _system;
    private IReadOnlyList<Colony>?     _colonies;
    private IReadOnlyList<Outpost>?    _outposts;
    private IReadOnlyList<StationData>? _stations;
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
            EventBus.Instance.POIDeselected    += OnEntityDeselected;
        }

        ShowEmptyState();
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.EntitySelected   -= OnEntitySelected;
            EventBus.Instance.EntityDeselected -= OnEntityDeselected;
            EventBus.Instance.POIDeselected    -= OnEntityDeselected;
        }
    }

    public void SetContext(
        StarSystemData? system,
        IReadOnlyList<Colony>? colonies,
        IReadOnlyList<Outpost>? outposts,
        IReadOnlyList<StationData>? stations,
        int viewerEmpireId)
    {
        _system = system;
        _colonies = colonies;
        _outposts = outposts;
        _stations = stations;
        _viewerEmpireId = viewerEmpireId;
        if (_emptyState != null) RefreshEmpty();
    }

    private void Clear()
    {
        foreach (var c in _content.GetChildren()) c.QueueFree();
        _emptyState = null;
        _colonyPanel = null;
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
        if (kind == POIEntityKind.Colony.ToString())
        {
            var colony = _colonies?.FirstOrDefault(c => c.Id == entityId);
            if (colony != null) { ShowColony(colony); return; }
        }
        // Other entity kinds (Outpost, Station, Salvage, Enemy) land in P4. Fall back to empty
        // so the panel remains sensible even for unhandled kinds.
        ShowEmptyState();
    }

    private void OnEntityDeselected() => ShowEmptyState();

    private void ShowColony(Colony colony)
    {
        Clear();
        _colonyPanel = new ColonyEntityPanel();
        _content.AddChild(_colonyPanel);
        _colonyPanel.Populate(colony);
    }
}
