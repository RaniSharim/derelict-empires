using System.Collections.Generic;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Combat;

namespace DerlictEmpires.Nodes.UI.CombatHUD;

/// <summary>
/// Upper-right stack of transient combat notifications (ship lost, morale break,
/// subsystem crit, auto-pause). Each toast fades in, holds ~3s, fades out.
/// Categories have default-on/off per spec §4.6; settings UI is deferred.
/// </summary>
public partial class LiveEventToasts : Control
{
    private const int MaxVisible = 5;
    private const float HoldSeconds = 3.0f;

    private VBoxContainer _stack = null!;
    private readonly Queue<Control> _toasts = new();
    private int _battleId;
    private BattleManager? _manager;
    private int _lastEventIndex;

    public void Configure(BattleManager manager, int battleId)
    {
        _manager = manager;
        _battleId = battleId;
    }

    public override void _Ready()
    {
        // Upper-right: anchor to top-right with negative offset so width ≈ 320px.
        AnchorLeft = 1; AnchorTop = 0;
        AnchorRight = 1; AnchorBottom = 0;
        OffsetLeft = -360;
        OffsetRight = -20;
        OffsetTop = TopBar.BarHeight + 12;
        OffsetBottom = 0;
        CustomMinimumSize = new Vector2(340, 0);
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 150;

        _stack = new VBoxContainer();
        _stack.AddThemeConstantOverride("separation", 6);
        _stack.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_stack);

        if (EventBus.Instance != null)
        {
            EventBus.Instance.BattleTick += OnBattleTick;
            EventBus.Instance.CombatEnded += OnCombatEnded;
        }
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.BattleTick -= OnBattleTick;
            EventBus.Instance.CombatEnded -= OnCombatEnded;
        }
    }

    private void OnBattleTick(int battleId)
    {
        if (battleId != _battleId || _manager == null) return;
        var battle = _manager.GetBattle(_battleId);
        if (battle == null) return;

        // Pull the tail of the event buffer since last fire.
        var events = battle.RecentEvents;
        for (int i = _lastEventIndex; i < events.Count; i++)
            Push(events[i]);
        _lastEventIndex = events.Count;
    }

    private void OnCombatEnded(int battleId, CombatResult result)
    {
        if (battleId != _battleId) return;
        Push($"\u25CF Combat ended — {result.ToString().ToUpperInvariant()}");
    }

    private void Push(string text)
    {
        var toast = BuildToast(text);
        _stack.AddChild(toast);
        _toasts.Enqueue(toast);

        while (_toasts.Count > MaxVisible)
            _toasts.Dequeue().QueueFree();

        // Fade out after HoldSeconds.
        var tween = CreateTween();
        tween.TweenInterval(HoldSeconds);
        tween.TweenProperty(toast, "modulate:a", 0f, 0.4f);
        tween.TweenCallback(Callable.From(() =>
        {
            toast.QueueFree();
        }));
    }

    private static Control BuildToast(string text)
    {
        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(0, 28);
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0.82f),
            BorderColor = UIColors.BorderMid,
        };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(0);
        style.ContentMarginLeft = 10;
        style.ContentMarginRight = 10;
        style.ContentMarginTop = 4;
        style.ContentMarginBottom = 4;
        panel.AddThemeStyleboxOverride("panel", style);

        var label = new Label { Text = text };
        UIFonts.StyleRole(label, UIFonts.Role.Small, UIColors.TextLabel);
        label.ClipText = true;
        panel.AddChild(label);

        panel.Modulate = new Color(1, 1, 1, 0);
        // Fade in on add.
        panel.Ready += () =>
        {
            var tween = panel.CreateTween();
            tween.TweenProperty(panel, "modulate:a", 1.0f, 0.15f);
        };
        return panel;
    }
}
