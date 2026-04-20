using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Combat;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Ships;

namespace DerlictEmpires.Nodes.UI.CombatHUD;

/// <summary>
/// Bottom 64px strip during combat. Four segments per spec §4.5:
/// [time &amp; state] [global commands] [target priority override] [emergency].
/// 4×/8× speeds are suppressed while engaged.
/// </summary>
public partial class BattleBar : PanelContainer
{
    private BattleManager? _manager;
    private int _battleId;

    private Label _timeLabel = null!;
    private Label _progressLabel = null!;
    private Button _speed1 = null!;
    private Button _speed2 = null!;
    private Label _endedBanner = null!;

    public void Configure(BattleManager manager, int battleId)
    {
        _manager = manager;
        _battleId = battleId;
    }

    public override void _Ready()
    {
        var bg = new StyleBoxFlat { BgColor = UIColors.GlassDarkFlat, BorderColor = UIColors.BorderMid };
        bg.SetBorderWidthAll(0);
        bg.BorderWidthTop = 1;
        bg.SetCornerRadiusAll(0);
        bg.ContentMarginLeft = 14;
        bg.ContentMarginRight = 14;
        bg.ContentMarginTop = 8;
        bg.ContentMarginBottom = 8;
        AddThemeStyleboxOverride("panel", bg);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 20);
        AddChild(row);

        row.AddChild(BuildTimeSegment());
        row.AddChild(BuildSeparator());
        row.AddChild(BuildGlobalCommands());
        row.AddChild(BuildSeparator());
        row.AddChild(BuildPrioritySegment());
        row.AddChild(BuildSeparator());
        row.AddChild(BuildEmergencySegment());

        _endedBanner = new Label { Text = "" };
        UIFonts.StyleRole(_endedBanner, UIFonts.Role.Title, UIColors.Moving);
        _endedBanner.Visible = false;
        _endedBanner.AnchorLeft = 0; _endedBanner.AnchorRight = 1;
        _endedBanner.HorizontalAlignment = HorizontalAlignment.Center;
        AddChild(_endedBanner);
    }

    public void Refresh()
    {
        if (_manager == null) return;
        var battle = _manager.GetBattle(_battleId);
        if (battle == null) return;

        int m = (int)(battle.ElapsedSeconds / 60f);
        int s = (int)(battle.ElapsedSeconds % 60f);
        _timeLabel.Text = $"T+{m:00}:{s:00}";

        // Progress proxy: cap at 60 sim-seconds.
        float progress = Mathf.Clamp(battle.ElapsedSeconds / 60f, 0f, 1f);
        _progressLabel.Text = $"{(int)(progress * 100)}%";

        var gm = GameManager.Instance;
        bool paused = gm?.CurrentSpeed == GameSpeed.Paused;
        bool speed2 = gm?.CurrentSpeed == GameSpeed.Fast;
        _speed1.Disabled = !paused && !speed2 ? false : false;
        _speed2.Disabled = false;
    }

    public void MarkEnded(CombatResult result)
    {
        _endedBanner.Visible = true;
        _endedBanner.Text = $"BATTLE ENDED \u2014 {result.ToString().ToUpperInvariant()}";
    }

    // === Segments ===========================================================

    private Control BuildTimeSegment()
    {
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 4);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        col.AddChild(row);

        _timeLabel = new Label { Text = "T+00:00" };
        UIFonts.StyleRole(_timeLabel, UIFonts.Role.Normal, UIColors.TextBright);
        row.AddChild(_timeLabel);

        _speed1 = MakeSpeedButton("1\u00D7", () =>
        {
            var gm = GameManager.Instance;
            if (gm != null) { gm.CurrentSpeed = GameSpeed.Normal; EventBus.Instance?.FireSpeedChanged(GameSpeed.Normal); }
        });
        _speed2 = MakeSpeedButton("2\u00D7", () =>
        {
            var gm = GameManager.Instance;
            if (gm != null) { gm.CurrentSpeed = GameSpeed.Fast; EventBus.Instance?.FireSpeedChanged(GameSpeed.Fast); }
        });
        row.AddChild(_speed1);
        row.AddChild(_speed2);

        _progressLabel = new Label { Text = "0%" };
        UIFonts.StyleRole(_progressLabel, UIFonts.Role.Small);
        col.AddChild(_progressLabel);

        return col;
    }

    private Control BuildGlobalCommands()
    {
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 4);

        col.AddChild(MakeActionButton("ALL CHARGE", UIColors.AccentRed, () => _manager?.SetAllDispositions(_battleId, Disposition.Charge)));
        col.AddChild(MakeActionButton("ALL HOLD", UIColors.TextLabel, () => _manager?.SetAllDispositions(_battleId, Disposition.Hold)));

        return col;
    }

    private Control BuildPrioritySegment()
    {
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 4);

        var label = new Label { Text = "TARGET PRIORITY" };
        UIFonts.StyleRole(label, UIFonts.Role.Small);
        col.AddChild(label);

        var dd = new OptionButton();
        dd.AddItem("BIGGEST THREAT");
        dd.AddItem("CARRIERS FIRST");
        dd.AddItem("NEAREST");
        dd.CustomMinimumSize = new Vector2(160, 28);
        UIFonts.StyleButtonRole(dd, UIFonts.Role.Small);
        GlassPanel.StyleButton(dd);
        col.AddChild(dd);

        return col;
    }

    private Control BuildEmergencySegment()
    {
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 4);

        col.AddChild(MakeActionButton("RETREAT ALL", UIColors.AccentRed, () =>
        {
            _manager?.RequestPlayerRetreat(_battleId);
        }));

        return col;
    }

    private Button MakeSpeedButton(string text, System.Action onPressed)
    {
        var btn = new Button { Text = text };
        btn.CustomMinimumSize = new Vector2(44, 28);
        UIFonts.StyleButtonRole(btn, UIFonts.Role.Small, UIColors.TextLabel);
        GlassPanel.StyleButton(btn);
        btn.Pressed += () => onPressed();
        return btn;
    }

    private Button MakeActionButton(string text, Color accent, System.Action onPressed)
    {
        var btn = new Button { Text = text };
        btn.CustomMinimumSize = new Vector2(140, 28);
        UIFonts.StyleButtonRole(btn, UIFonts.Role.Small, UIColors.TextBright);

        var normal = new StyleBoxFlat { BgColor = new Color(accent.R, accent.G, accent.B, 0.14f), BorderColor = accent };
        normal.SetBorderWidthAll(1);
        normal.SetCornerRadiusAll(2);
        var hover = new StyleBoxFlat { BgColor = new Color(accent.R, accent.G, accent.B, 0.26f), BorderColor = accent };
        hover.SetBorderWidthAll(1);
        hover.SetCornerRadiusAll(2);
        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        btn.AddThemeStyleboxOverride("focus", normal);

        btn.Pressed += () => onPressed();
        return btn;
    }

    private static Control BuildSeparator()
    {
        var sep = new ColorRect { Color = UIColors.BorderDim };
        sep.CustomMinimumSize = new Vector2(1, 0);
        sep.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        return sep;
    }
}
