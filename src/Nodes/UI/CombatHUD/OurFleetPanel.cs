using System.Collections.Generic;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Combat;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Ships;

namespace DerlictEmpires.Nodes.UI.CombatHUD;

/// <summary>
/// Left column replacement during combat. Role-grouped aggregate rows; HP split into
/// per-ship pips; morale bar; per-role disposition dropdown; supply drain strip at bottom.
/// No per-ship interaction — the aggregation is load-bearing per §4.10 of the spec.
/// </summary>
public partial class OurFleetPanel : PanelContainer
{
    private BattleManager? _manager;
    private int _battleId;

    private VBoxContainer _rolesColumn = null!;
    private Label _headerLabel = null!;
    private Label _subLabel = null!;
    private readonly Dictionary<FleetRole, RoleWidget> _roleWidgets = new();

    public void Configure(BattleManager manager, int battleId)
    {
        _manager = manager;
        _battleId = battleId;
    }

    public override void _Ready()
    {
        var bg = new StyleBoxFlat { BgColor = UIColors.GlassDark, BorderColor = UIColors.BorderMid };
        bg.SetBorderWidthAll(0);
        bg.BorderWidthRight = 1;
        bg.SetCornerRadiusAll(0);
        bg.ContentMarginLeft = 14;
        bg.ContentMarginRight = 14;
        bg.ContentMarginTop = 14;
        bg.ContentMarginBottom = 14;
        AddThemeStyleboxOverride("panel", bg);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 10);
        AddChild(col);

        _headerLabel = new Label { Text = "OUR FLEET" };
        UIFonts.StyleRole(_headerLabel, UIFonts.Role.Title);
        col.AddChild(_headerLabel);

        _subLabel = new Label { Text = "" };
        UIFonts.StyleRole(_subLabel, UIFonts.Role.Small);
        col.AddChild(_subLabel);

        col.AddChild(BuildSeparator());

        _rolesColumn = new VBoxContainer();
        _rolesColumn.AddThemeConstantOverride("separation", 10);
        _rolesColumn.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        col.AddChild(_rolesColumn);

        col.AddChild(BuildSeparator());

        var retreat = new Button { Text = "RETREAT FLEET" };
        retreat.CustomMinimumSize = new Vector2(0, 36);
        UIFonts.StyleButtonRole(retreat, UIFonts.Role.Small, UIColors.AccentRed);
        GlassPanel.StyleButton(retreat);
        retreat.Pressed += () => _manager?.RequestPlayerRetreat(_battleId);
        col.AddChild(retreat);
    }

    public void Refresh()
    {
        if (_manager == null) return;
        var battle = _manager.GetBattle(_battleId);
        if (battle == null) return;

        var agg = _manager.GetAttackerAggregate(_battleId);

        _subLabel.Text = $"{agg.AliveShips}/{agg.TotalShips} SHIPS · HP {agg.AverageHpPercent:0}%";

        // Ensure row widgets exist for each role present, remove extras.
        var seen = new HashSet<FleetRole>();
        foreach (var slice in agg.Roles)
        {
            seen.Add(slice.Role);
            if (!_roleWidgets.TryGetValue(slice.Role, out var w))
            {
                w = new RoleWidget(slice.Role);
                _roleWidgets[slice.Role] = w;
                w.DispositionChanged += d => _manager?.SetDisposition(_battleId, slice.Role, d);
                _rolesColumn.AddChild(w);
            }
            w.UpdateFrom(slice);
        }
        foreach (var key in new List<FleetRole>(_roleWidgets.Keys))
        {
            if (seen.Contains(key)) continue;
            _roleWidgets[key].QueueFree();
            _roleWidgets.Remove(key);
        }
    }

    private static Control BuildSeparator()
    {
        var sep = new ColorRect { Color = UIColors.BorderDim };
        sep.CustomMinimumSize = new Vector2(0, 1);
        return sep;
    }
}

/// <summary>One role's aggregate row (HP pips + morale bar + disposition dropdown).</summary>
public partial class RoleWidget : VBoxContainer
{
    public event System.Action<Disposition>? DispositionChanged;

    private readonly FleetRole _role;
    private Label _title = null!;
    private HpPipsBar _hpBar = null!;
    private ColorRect _moraleFill = null!;
    private PanelContainer _moraleTrack = null!;
    private OptionButton _dispositionDropdown = null!;

    public RoleWidget(FleetRole role)
    {
        _role = role;
    }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);

        _title = new Label { Text = _role.ToString().ToUpperInvariant() };
        UIFonts.StyleRole(_title, UIFonts.Role.Small, UIColors.TextBright);
        AddChild(_title);

        _hpBar = new HpPipsBar();
        _hpBar.CustomMinimumSize = new Vector2(0, 8);
        AddChild(_hpBar);

        _moraleTrack = new PanelContainer();
        _moraleTrack.CustomMinimumSize = new Vector2(0, 4);
        var trackStyle = new StyleBoxFlat { BgColor = new Color(1, 1, 1, 0.08f) };
        trackStyle.SetBorderWidthAll(0);
        _moraleTrack.AddThemeStyleboxOverride("panel", trackStyle);
        _moraleFill = new ColorRect { Color = UIColors.AccentGreen };
        _moraleFill.AnchorLeft = 0; _moraleFill.AnchorTop = 0;
        _moraleFill.AnchorRight = 1; _moraleFill.AnchorBottom = 1;
        _moraleTrack.AddChild(_moraleFill);
        AddChild(_moraleTrack);

        _dispositionDropdown = new OptionButton();
        _dispositionDropdown.CustomMinimumSize = new Vector2(0, 28);
        UIFonts.StyleButtonRole(_dispositionDropdown, UIFonts.Role.Small);
        GlassPanel.StyleButton(_dispositionDropdown);
        foreach (var d in new[] { Disposition.Charge, Disposition.Hold, Disposition.StandBack, Disposition.Retreat })
            _dispositionDropdown.AddItem(d.ToString().ToUpperInvariant(), (int)d);
        _dispositionDropdown.Select((int)Disposition.Hold);
        _dispositionDropdown.ItemSelected += idx => DispositionChanged?.Invoke((Disposition)(int)idx);
        AddChild(_dispositionDropdown);
    }

    public void UpdateFrom(RoleSlice slice)
    {
        _title.Text = $"{_role.ToString().ToUpperInvariant()} · {slice.AliveCount}/{slice.ShipCount}";
        _hpBar.Pips = slice.HpPips.ToArray();
        _hpBar.QueueRedraw();

        // Morale bar scaling; < 30% flips to red.
        float pct = slice.AverageMoralePercent / 100f;
        _moraleFill.AnchorRight = Mathf.Clamp(pct, 0f, 1f);
        _moraleFill.Color = pct < 0.30f ? UIColors.AccentRed : UIColors.AccentGreen;
    }
}

public partial class HpPipsBar : Control
{
    public float[] Pips { get; set; } = System.Array.Empty<float>();

    public HpPipsBar()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        if (Pips.Length == 0) return;
        float w = Size.X;
        float h = Size.Y;
        float gap = 2f;
        float pipW = (w - gap * (Pips.Length - 1)) / Pips.Length;
        if (pipW < 1f) pipW = 1f;

        for (int i = 0; i < Pips.Length; i++)
        {
            float x = i * (pipW + gap);
            var trackRect = new Rect2(x, 0, pipW, h);
            DrawRect(trackRect, new Color(1, 1, 1, 0.08f), filled: true);

            var fillColor = Pips[i] switch
            {
                var p when p <= 0f   => new Color(UIColors.AccentRed.R, UIColors.AccentRed.G, UIColors.AccentRed.B, 0.35f),
                var p when p < 0.33f => UIColors.AccentRed,
                var p when p < 0.66f => UIColors.Moving,
                _                    => UIColors.AccentGreen,
            };
            var fillRect = new Rect2(x, 0, pipW * Mathf.Max(0.02f, Pips[i]), h);
            DrawRect(fillRect, fillColor, filled: true);
        }
    }
}
