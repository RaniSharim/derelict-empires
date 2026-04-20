using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Combat;
using DerlictEmpires.Nodes.Map;

namespace DerlictEmpires.Nodes.UI.CombatHUD;

/// <summary>
/// Top-left chip listing all active battles when &gt;1 is running. Sorted by urgency
/// (losing → even → winning). Click jumps the HUD to that battle. Hidden when only
/// one battle is active.
/// </summary>
public partial class BattleOutliner : PanelContainer
{
    private BattleManager? _manager;
    private VBoxContainer _list = null!;
    private int _activeBattleId = -1;

    public void Configure(BattleManager manager)
    {
        _manager = manager;
    }

    public override void _Ready()
    {
        AnchorLeft = 0; AnchorTop = 0; AnchorRight = 0; AnchorBottom = 0;
        OffsetLeft = 16;
        OffsetTop = TopBar.BarHeight + 12;
        CustomMinimumSize = new Vector2(240, 0);
        ZIndex = 150;

        var bg = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0.75f),
            BorderColor = UIColors.BorderMid,
        };
        bg.SetBorderWidthAll(1);
        bg.SetCornerRadiusAll(0);
        bg.ContentMarginLeft = 10;
        bg.ContentMarginRight = 10;
        bg.ContentMarginTop = 8;
        bg.ContentMarginBottom = 8;
        AddThemeStyleboxOverride("panel", bg);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 4);
        AddChild(col);

        var header = new Label { Text = "\u2694 BATTLES ACTIVE" };
        UIFonts.StyleRole(header, UIFonts.Role.Small, UIColors.AccentRed);
        col.AddChild(header);

        _list = new VBoxContainer();
        _list.AddThemeConstantOverride("separation", 2);
        col.AddChild(_list);

        if (EventBus.Instance != null)
        {
            EventBus.Instance.BattleTick += OnBattleTick;
            EventBus.Instance.CombatStarted += OnCombatChanged;
            EventBus.Instance.CombatEnded += OnCombatChangedEnded;
        }

        Visible = false;
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.BattleTick -= OnBattleTick;
            EventBus.Instance.CombatStarted -= OnCombatChanged;
            EventBus.Instance.CombatEnded -= OnCombatChangedEnded;
        }
    }

    public void SetActive(int battleId)
    {
        _activeBattleId = battleId;
        Rebuild();
    }

    private void OnCombatChanged(int _) => Rebuild();
    private void OnCombatChangedEnded(int _, CombatResult __) => Rebuild();
    private void OnBattleTick(int _) => Rebuild();

    private void Rebuild()
    {
        if (_manager == null) return;
        var battles = _manager.ActiveBattles.Where(b => b.State == BattleState.Running).ToList();

        if (battles.Count <= 1)
        {
            Visible = false;
            return;
        }
        Visible = true;

        foreach (var child in _list.GetChildren()) child.QueueFree();

        var sorted = battles
            .OrderBy(b => ClassifyUrgency(b))
            .ThenBy(b => b.Id);

        foreach (var b in sorted)
        {
            _list.AddChild(BuildRow(b));
        }
    }

    private static int ClassifyUrgency(Battle b)
    {
        int myAlive = b.AttackersAlive;
        int theirAlive = b.DefendersAlive;
        if (myAlive < theirAlive) return 0; // losing
        if (myAlive == theirAlive) return 1; // even
        return 2; // winning
    }

    private Control BuildRow(Battle b)
    {
        var btn = new Button
        {
            Text = $"\u25CF Battle #{b.Id}  \u00B7  {StatusLabel(b)}",
            Flat = true,
        };
        btn.Alignment = HorizontalAlignment.Left;
        btn.CustomMinimumSize = new Vector2(0, 22);
        UIFonts.StyleButtonRole(btn, UIFonts.Role.Small,
            b.Id == _activeBattleId ? UIColors.TextBright : UIColors.TextLabel);
        btn.Pressed += () => SetActive(b.Id);
        return btn;
    }

    private static string StatusLabel(Battle b) =>
        ClassifyUrgency(b) switch
        {
            0 => "LOSING",
            1 => "EVEN",
            _ => "WINNING",
        };
}
