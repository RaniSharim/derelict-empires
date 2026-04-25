using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Nodes.UI;
using DerlictEmpires.Nodes.UI.ShipDesigner;
using DerlictEmpires.Nodes.UI.SystemView;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Routes EventBus open-requests to transient full-screen overlays:
/// tech tree, ship designer, system view. Owns the active-overlay refs so
/// MainScene stays agnostic to what's open.
/// </summary>
public partial class OverlayRouter : Node
{
    private MainScene _main = null!;
    private CanvasLayer _uiLayer = null!;

    private TechTreeOverlay? _activeTechTreeOverlay;
    private ShipDesignerOverlay? _activeDesignerOverlay;
    private SystemViewScene? _systemView;

    public void Configure(MainScene main, CanvasLayer uiLayer)
    {
        _main = main;
        _uiLayer = uiLayer;
    }

    public override void _Ready()
    {
        if (EventBus.Instance == null) return;
        EventBus.Instance.TechTreeOpenRequested += OnTechTreeOpenRequested;
        EventBus.Instance.DesignerOpenRequested += OnDesignerOpenRequested;
        EventBus.Instance.SystemDoubleClicked += OnSystemSelectedForView;
        EventBus.Instance.SystemViewClosed += OnSystemViewClosed;
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance == null) return;
        EventBus.Instance.TechTreeOpenRequested -= OnTechTreeOpenRequested;
        EventBus.Instance.DesignerOpenRequested -= OnDesignerOpenRequested;
        EventBus.Instance.SystemDoubleClicked -= OnSystemSelectedForView;
        EventBus.Instance.SystemViewClosed -= OnSystemViewClosed;
    }

    private void OnTechTreeOpenRequested(TechTreeOpenRequest request)
    {
        if (_activeTechTreeOverlay != null && IsInstanceValid(_activeTechTreeOverlay))
            return;

        var overlay = new TechTreeOverlay { Name = "TechTreeOverlay" };
        overlay.Configure(GameManager.Instance!, request.Color);
        overlay.TreeExited += () => _activeTechTreeOverlay = null;
        _activeTechTreeOverlay = overlay;
        _uiLayer.AddChild(overlay);
    }

    private void OnDesignerOpenRequested(DesignerOpenRequest request)
    {
        if (_activeDesignerOverlay != null && IsInstanceValid(_activeDesignerOverlay))
            return;

        var overlay = new ShipDesignerOverlay { Name = "ShipDesignerOverlay" };
        overlay.Configure(_main, request);
        overlay.TreeExited += () => _activeDesignerOverlay = null;
        _activeDesignerOverlay = overlay;
        _uiLayer.AddChild(overlay);
    }

    private void OnSystemSelectedForView(StarSystemData system)
    {
        if (_systemView != null && IsInstanceValid(_systemView))
        {
            ApplySystemViewContext();
            _systemView.Open(system);
            return;
        }

        _systemView = new SystemViewScene { Name = "SystemViewScene" };
        _uiLayer.AddChild(_systemView);
        ApplySystemViewContext();
        _systemView.Open(system);
        EventBus.Instance?.FireSystemViewOpened(system.Id);
    }

    private void ApplySystemViewContext()
    {
        if (_systemView == null) return;
        var playerId = GameManager.Instance?.LocalPlayerEmpire?.Id ?? -1;
        _systemView.SetContext(
            colonies:        _main.SettlementSystem?.Colonies,
            outposts:        _main.SettlementSystem?.Outposts,
            stations:        GameManager.Instance.StationDatas,
            stationsRuntime: _main.StationSystem?.Stations,
            fleets:          _main.Fleets,
            galaxy:          GameManager.Instance?.Galaxy,
            viewerEmpireId:  playerId);
    }

    private void OnSystemViewClosed()
    {
        _systemView = null;
    }
}
