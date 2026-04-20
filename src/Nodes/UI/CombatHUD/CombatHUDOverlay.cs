using System;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Combat;
using DerlictEmpires.Nodes.Map;

namespace DerlictEmpires.Nodes.UI.CombatHUD;

/// <summary>
/// Combat HUD overlay. Not a full-screen glass modal — it hides the normal
/// LeftPanel / RightPanel and mounts OUR FLEET / THEIR FLEET columns in their
/// place plus a 64px Battle Bar along the bottom. The galaxy map stays visible.
/// Built as a Control parented to the UI CanvasLayer; z-index keeps it above
/// the panels it replaces.
/// </summary>
public partial class CombatHUDOverlay : Control
{
    public const int BarHeight = 64;

    private MainScene? _mainScene;
    private BattleManager? _manager;
    private int _battleId;

    private OurFleetPanel _ourPanel = null!;
    private TheirFleetPanel _theirPanel = null!;
    private BattleBar _battleBar = null!;
    private LiveEventToasts _toasts = null!;

    /// <summary>Raised when the HUD has run its fade-out and is safe to free.</summary>
    public event Action? Closed;

    public int BattleId => _battleId;

    public void Configure(MainScene mainScene, BattleManager manager, int battleId)
    {
        _mainScene = mainScene;
        _manager = manager;
        _battleId = battleId;
    }

    public override void _Ready()
    {
        AnchorLeft = 0;
        AnchorTop = 0;
        AnchorRight = 1;
        AnchorBottom = 1;
        OffsetLeft = 0;
        OffsetTop = 0;
        OffsetRight = 0;
        OffsetBottom = 0;
        MouseFilter = MouseFilterEnum.Pass;
        ZIndex = 100;

        BuildLayout();

        if (EventBus.Instance != null)
        {
            EventBus.Instance.BattleTick += OnBattleTick;
            EventBus.Instance.CombatEnded += OnCombatEnded;
        }

        RefreshAll();
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.BattleTick -= OnBattleTick;
            EventBus.Instance.CombatEnded -= OnCombatEnded;
        }
    }

    private void BuildLayout()
    {
        _ourPanel = new OurFleetPanel { Name = "OurFleetPanel" };
        _ourPanel.AnchorLeft = 0;
        _ourPanel.AnchorTop = 0;
        _ourPanel.AnchorRight = 0;
        _ourPanel.AnchorBottom = 1;
        _ourPanel.OffsetLeft = 0;
        _ourPanel.OffsetRight = 320;
        _ourPanel.OffsetTop = TopBar.BarHeight;
        _ourPanel.OffsetBottom = -BarHeight;
        AddChild(_ourPanel);
        if (_manager != null) _ourPanel.Configure(_manager, _battleId);

        _theirPanel = new TheirFleetPanel { Name = "TheirFleetPanel" };
        _theirPanel.AnchorLeft = 1;
        _theirPanel.AnchorTop = 0;
        _theirPanel.AnchorRight = 1;
        _theirPanel.AnchorBottom = 1;
        _theirPanel.OffsetLeft = -320;
        _theirPanel.OffsetRight = 0;
        _theirPanel.OffsetTop = TopBar.BarHeight;
        _theirPanel.OffsetBottom = -BarHeight;
        AddChild(_theirPanel);
        if (_manager != null) _theirPanel.Configure(_manager, _battleId);

        _battleBar = new BattleBar { Name = "BattleBar" };
        _battleBar.AnchorLeft = 0;
        _battleBar.AnchorTop = 1;
        _battleBar.AnchorRight = 1;
        _battleBar.AnchorBottom = 1;
        _battleBar.OffsetTop = -BarHeight;
        _battleBar.OffsetBottom = 0;
        AddChild(_battleBar);
        if (_manager != null) _battleBar.Configure(_manager, _battleId);

        _toasts = new LiveEventToasts { Name = "LiveEventToasts" };
        AddChild(_toasts);
        if (_manager != null && _mainScene != null) _toasts.Configure(_mainScene, _manager, _battleId);

        var outliner = new BattleOutliner { Name = "BattleOutliner" };
        AddChild(outliner);
        if (_manager != null)
        {
            outliner.Configure(_manager);
            outliner.SetActive(_battleId);
        }
    }

    private void OnBattleTick(int battleId)
    {
        if (battleId != _battleId) return;
        RefreshAll();
    }

    private void OnCombatEnded(int battleId, CombatResult result)
    {
        if (battleId != _battleId) return;
        // Leave the panels on-screen for one frame; the owning scene is responsible
        // for swapping to the debrief via its own listener. We just stop updating.
        _battleBar.MarkEnded(result);
    }

    private void RefreshAll()
    {
        _ourPanel?.Refresh();
        _theirPanel?.Refresh();
        _battleBar?.Refresh();
    }

    public void RequestClose()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.15f);
        tween.TweenCallback(Callable.From(() =>
        {
            Closed?.Invoke();
            QueueFree();
        }));
    }
}
