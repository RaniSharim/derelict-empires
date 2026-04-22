using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Combat;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Floating battle panel anchored ABOVE the system where a battle is happening. Visible only
/// when that system is the currently-selected one. Two columns — enemy on the left, our fleet
/// on the right — each listing one row per role with ship boxes that show shield/armor/HP.
/// When the battle ends the panel swaps its body to a compact debrief instead of popping up a
/// separate modal. Closed by the user dismissing the system selection or via the Continue
/// button that appears after debrief.
/// </summary>
public partial class CombatPopup : Control
{
    public const int PanelWidth = 640;
    public const int ColumnWidth = 300;
    private const int ScreenOffsetY = -40;   // place the BOTTOM of the panel this far above the star
    private const int MinTopPx = 96;         // keep clear of TopBar

    private BattleManager _manager = null!;
    private int _battleId;
    private int _systemId;
    private Camera3D? _camera;
    private Vector3 _worldAnchor;
    private CombatResult? _finalResult;

    private PanelContainer _panel = null!;
    private VBoxContainer _rootColumn = null!;
    private Label _title = null!;
    private Label _statusLine = null!;
    private Control _liveBody = null!;
    private Control? _debriefBody;
    private VBoxContainer _ourRoles = null!;
    private VBoxContainer _enemyRoles = null!;
    private readonly Dictionary<FleetRole, RoleRow> _ourRows = new();
    private readonly Dictionary<FleetRole, RoleRow> _enemyRows = new();

    public void Configure(BattleManager manager, int battleId, int systemId, Camera3D? camera)
    {
        _manager = manager;
        _battleId = battleId;
        _systemId = systemId;
        _camera = camera;

        var system = GameManager.Instance?.Galaxy?.GetSystem(systemId);
        if (system != null)
            _worldAnchor = new Vector3(system.PositionX, 1.2f, system.PositionZ);
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 60;

        _panel = new PanelContainer { Name = "Panel" };
        _panel.CustomMinimumSize = new Vector2(PanelWidth, 0);
        _panel.Size = new Vector2(PanelWidth, 0);
        _panel.MouseFilter = MouseFilterEnum.Stop;

        var bg = new StyleBoxFlat
        {
            BgColor = UIColors.GlassDark,
            BorderColor = UIColors.BorderMid,
        };
        bg.SetBorderWidthAll(1);
        bg.SetCornerRadiusAll(4);
        bg.ContentMarginLeft = 12;
        bg.ContentMarginRight = 12;
        bg.ContentMarginTop = 10;
        bg.ContentMarginBottom = 10;
        _panel.AddThemeStyleboxOverride("panel", bg);
        AddChild(_panel);

        _rootColumn = new VBoxContainer();
        _rootColumn.AddThemeConstantOverride("separation", 8);
        _panel.AddChild(_rootColumn);

        _title = new Label();
        UIFonts.StyleRole(_title, UIFonts.Role.Title);
        _rootColumn.AddChild(_title);

        _statusLine = new Label { Text = "ENGAGED" };
        UIFonts.StyleRole(_statusLine, UIFonts.Role.Small, UIColors.AccentRed);
        _rootColumn.AddChild(_statusLine);

        _rootColumn.AddChild(MakeDivider());

        _liveBody = BuildLiveBody();
        _rootColumn.AddChild(_liveBody);

        UpdateTitle();
        Refresh();

        if (EventBus.Instance != null)
        {
            EventBus.Instance.BattleTick += OnBattleTick;
            EventBus.Instance.CombatEnded += OnCombatEnded;
            EventBus.Instance.SystemSelected += OnSystemSelected;
            EventBus.Instance.SystemDeselected += OnSystemDeselected;
        }

        Visible = false;
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.BattleTick -= OnBattleTick;
            EventBus.Instance.CombatEnded -= OnCombatEnded;
            EventBus.Instance.SystemSelected -= OnSystemSelected;
            EventBus.Instance.SystemDeselected -= OnSystemDeselected;
        }
    }

    public override void _Process(double delta)
    {
        if (!Visible || _camera == null) return;
        UpdateScreenPosition();
    }

    private void OnBattleTick(int battleId)
    {
        if (battleId != _battleId || _finalResult.HasValue) return;
        if (!Visible) return;
        Refresh();
    }

    private void OnCombatEnded(int battleId, CombatResult result)
    {
        if (battleId != _battleId) return;
        _finalResult = result;
        ShowDebrief(result);
    }

    private void OnSystemSelected(StarSystemData system)
    {
        bool match = system != null && system.Id == _systemId;
        Visible = match;
        if (match)
        {
            UpdateScreenPosition();
            if (!_finalResult.HasValue) Refresh();
        }
    }

    private void OnSystemDeselected()
    {
        Visible = false;
    }

    // ── Position ───────────────────────────────────────────────

    private void UpdateScreenPosition()
    {
        if (_camera == null) return;
        var vpSize = GetViewport().GetVisibleRect().Size;
        var screenPos = _camera.UnprojectPosition(_worldAnchor);

        var panelSize = _panel.Size;
        if (panelSize.Y < 1) panelSize = new Vector2(PanelWidth, 260);

        // Anchor ABOVE the star — bottom edge of the panel sits ScreenOffsetY pixels above the star.
        float x = screenPos.X - panelSize.X / 2f;
        float y = screenPos.Y + ScreenOffsetY - panelSize.Y;

        // Reserve space for TopBar and event log / bottom edge.
        x = Mathf.Clamp(x, 16f, vpSize.X - panelSize.X - 16f);
        y = Mathf.Clamp(y, MinTopPx, vpSize.Y - panelSize.Y - 32f);

        Position = new Vector2(x, y);
        Size = new Vector2(panelSize.X, panelSize.Y);
    }

    private void UpdateTitle()
    {
        var sys = GameManager.Instance?.Galaxy?.GetSystem(_systemId);
        _title.Text = $"BATTLE · {sys?.Name?.ToUpperInvariant() ?? "?"}";
    }

    // ── Live body (two columns) ─────────────────────────────────

    private Control BuildLiveBody()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);

        // LEFT column — ENEMY
        var enemyCol = new VBoxContainer();
        enemyCol.AddThemeConstantOverride("separation", 6);
        enemyCol.CustomMinimumSize = new Vector2(ColumnWidth, 0);
        enemyCol.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var enemyHeader = new Label { Text = "ENEMY" };
        UIFonts.StyleRole(enemyHeader, UIFonts.Role.Small, UIColors.AccentRed);
        enemyCol.AddChild(enemyHeader);

        _enemyRoles = new VBoxContainer();
        _enemyRoles.AddThemeConstantOverride("separation", 6);
        enemyCol.AddChild(_enemyRoles);

        row.AddChild(enemyCol);

        // Vertical separator
        var sep = new ColorRect { Color = UIColors.BorderDim };
        sep.CustomMinimumSize = new Vector2(1, 0);
        sep.SizeFlagsVertical = SizeFlags.ExpandFill;
        row.AddChild(sep);

        // RIGHT column — OUR FLEET
        var ourCol = new VBoxContainer();
        ourCol.AddThemeConstantOverride("separation", 6);
        ourCol.CustomMinimumSize = new Vector2(ColumnWidth, 0);
        ourCol.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var ourHeader = new Label { Text = "OUR FLEET" };
        UIFonts.StyleRole(ourHeader, UIFonts.Role.Small, UIColors.AccentGreen);
        ourCol.AddChild(ourHeader);

        _ourRoles = new VBoxContainer();
        _ourRoles.AddThemeConstantOverride("separation", 6);
        ourCol.AddChild(_ourRoles);

        row.AddChild(ourCol);

        return row;
    }

    private void Refresh()
    {
        var battle = _manager.GetBattle(_battleId);
        if (battle == null) return;
        RefreshSide(battle.Defenders, _enemyRoles, _enemyRows);
        RefreshSide(battle.Attackers, _ourRoles, _ourRows);
    }

    private static void RefreshSide(
        List<CombatUnit> units, VBoxContainer parent, Dictionary<FleetRole, RoleRow> rows)
    {
        var byRole = new Dictionary<FleetRole, List<CombatUnit>>();
        foreach (var u in units)
        {
            if (!byRole.TryGetValue(u.Role, out var list))
            {
                list = new List<CombatUnit>();
                byRole[u.Role] = list;
            }
            list.Add(u);
        }

        foreach (var (role, list) in byRole)
        {
            if (!rows.TryGetValue(role, out var row))
            {
                row = new RoleRow(role);
                rows[role] = row;
                parent.AddChild(row);
            }
            row.UpdateFrom(list);
        }

        var toRemove = new List<FleetRole>();
        foreach (var key in rows.Keys)
            if (!byRole.ContainsKey(key))
                toRemove.Add(key);
        foreach (var key in toRemove)
        {
            rows[key].QueueFree();
            rows.Remove(key);
        }
    }

    // ── Debrief body (swap-in) ─────────────────────────────────

    private void ShowDebrief(CombatResult result)
    {
        if (_debriefBody != null) return;

        _liveBody.Visible = false;

        Color statusColor = result switch
        {
            CombatResult.Victory => UIColors.AccentGreen,
            CombatResult.Defeat  => UIColors.AccentRed,
            CombatResult.Retreat => UIColors.Moving,
            _                    => UIColors.TextBody,
        };
        _statusLine.Text = $"RESULT · {result.ToString().ToUpperInvariant()}";
        _statusLine.AddThemeColorOverride("font_color", statusColor);

        _debriefBody = BuildDebriefBody(result);
        _rootColumn.AddChild(_debriefBody);
    }

    private Control BuildDebriefBody(CombatResult result)
    {
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 6);

        var battle = _manager.GetBattle(_battleId);

        // Time + summary line.
        var summary = new Label
        {
            Text = battle != null
                ? $"T+{(int)(battle.ElapsedSeconds / 60):00}:{(int)(battle.ElapsedSeconds % 60):00}   ·   " +
                  $"attackers {battle.AttackersAlive}/{battle.Attackers.Count}   defenders {battle.DefendersAlive}/{battle.Defenders.Count}"
                : "",
        };
        UIFonts.StyleRole(summary, UIFonts.Role.Small, UIColors.TextBody);
        col.AddChild(summary);

        col.AddChild(MakeDivider());

        // Per-design performance (compact).
        var perfHeader = new Label { Text = "PER-DESIGN PERFORMANCE" };
        UIFonts.StyleRole(perfHeader, UIFonts.Role.Small, UIColors.TextDim);
        col.AddChild(perfHeader);

        if (battle == null || battle.PerDesignPerformance.Count == 0)
        {
            var empty = new Label { Text = "No design data." };
            UIFonts.StyleRole(empty, UIFonts.Role.Small, UIColors.TextDim);
            col.AddChild(empty);
        }
        else
        {
            foreach (var perf in battle.PerDesignPerformance.Values.OrderByDescending(p => p.DamageDealt))
            {
                var line = new Label
                {
                    Text = $"{perf.DesignName.ToUpperInvariant()} · {perf.ShipsSurvived}/{perf.ShipsEngaged}   " +
                           $"dmg {(int)perf.DamageDealt} / taken {(int)perf.DamageTaken}",
                };
                UIFonts.StyleRole(line, UIFonts.Role.Small, UIColors.TextBright);
                col.AddChild(line);
            }
        }

        col.AddChild(MakeDivider());

        int xp = battle != null ? (int)(battle.ElapsedSeconds * 10f) : 0;
        var xpRow = new Label { Text = $"Expertise XP: {xp}" };
        UIFonts.StyleRole(xpRow, UIFonts.Role.Small, UIColors.DeltaPos);
        col.AddChild(xpRow);

        // Continue button — closes popup.
        var btnRow = new HBoxContainer();
        btnRow.AddThemeConstantOverride("separation", 6);
        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        btnRow.AddChild(spacer);

        var continueBtn = new Button { Text = "CONTINUE" };
        continueBtn.CustomMinimumSize = new Vector2(120, 30);
        UIFonts.StyleButtonRole(continueBtn, UIFonts.Role.Small, UIColors.TextBright);
        GlassPanel.StyleButton(continueBtn);
        continueBtn.Pressed += () => QueueFree();
        btnRow.AddChild(continueBtn);

        col.AddChild(btnRow);

        return col;
    }

    private static Control MakeDivider()
    {
        var r = new ColorRect { Color = UIColors.BorderDim };
        r.CustomMinimumSize = new Vector2(0, 1);
        return r;
    }
}

/// <summary>One role's row: label on top, ship boxes below.</summary>
public partial class RoleRow : VBoxContainer
{
    private readonly FleetRole _role;
    private Label _label = null!;
    private HBoxContainer _boxes = null!;
    private readonly Dictionary<int, ShipBox> _boxesById = new();

    public RoleRow(FleetRole role) { _role = role; }

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 3);

        _label = new Label();
        UIFonts.StyleRole(_label, UIFonts.Role.Small, UIColors.TextBright);
        AddChild(_label);

        _boxes = new HBoxContainer();
        _boxes.AddThemeConstantOverride("separation", 4);
        AddChild(_boxes);
    }

    public void UpdateFrom(List<CombatUnit> units)
    {
        int alive = 0;
        foreach (var u in units) if (!u.IsDestroyed) alive++;
        _label.Text = $"{_role.ToString().ToUpperInvariant()} · {alive}/{units.Count}";

        var seen = new HashSet<int>();
        foreach (var u in units)
        {
            seen.Add(u.Id);
            if (!_boxesById.TryGetValue(u.Id, out var box))
            {
                box = new ShipBox();
                _boxesById[u.Id] = box;
                _boxes.AddChild(box);
            }
            box.UpdateFrom(u);
        }

        var toRemove = new List<int>();
        foreach (var id in _boxesById.Keys)
            if (!seen.Contains(id))
                toRemove.Add(id);
        foreach (var id in toRemove)
        {
            _boxesById[id].QueueFree();
            _boxesById.Remove(id);
        }
    }
}

/// <summary>Small ship card: name + shield/armor/HP bars stacked vertically.</summary>
public partial class ShipBox : PanelContainer
{
    private const int BoxWidth = 68;
    private const int BarHeight = 3;

    private static readonly Color ShieldColor = new(0.35f, 0.75f, 1.0f);
    private static readonly Color ArmorColor = new(0.85f, 0.65f, 0.25f);

    private Label _name = null!;
    private BarFill _shield = null!;
    private BarFill _armor = null!;
    private BarFill _hp = null!;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(BoxWidth, 0);

        var bg = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0.35f),
            BorderColor = UIColors.BorderDim,
        };
        bg.SetBorderWidthAll(1);
        bg.SetCornerRadiusAll(2);
        bg.ContentMarginLeft = 4;
        bg.ContentMarginRight = 4;
        bg.ContentMarginTop = 3;
        bg.ContentMarginBottom = 4;
        AddThemeStyleboxOverride("panel", bg);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 2);
        AddChild(col);

        _name = new Label();
        UIFonts.StyleRole(_name, UIFonts.Role.Small, UIColors.TextBright);
        _name.ClipText = true;
        _name.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        col.AddChild(_name);

        _shield = new BarFill(ShieldColor);
        _shield.CustomMinimumSize = new Vector2(0, BarHeight);
        col.AddChild(_shield);

        _armor = new BarFill(ArmorColor);
        _armor.CustomMinimumSize = new Vector2(0, BarHeight);
        col.AddChild(_armor);

        _hp = new BarFill(UIColors.AccentGreen);
        _hp.CustomMinimumSize = new Vector2(0, BarHeight);
        col.AddChild(_hp);
    }

    public void UpdateFrom(CombatUnit u)
    {
        _name.Text = string.IsNullOrEmpty(u.Name) ? "?" : u.Name;

        _shield.SetFill(u.ShieldMax > 0 ? u.ShieldHp / u.ShieldMax : 0f, ShieldColor);
        _armor.SetFill(u.ArmorMax > 0 ? u.ArmorHp / u.ArmorMax : 0f, ArmorColor);

        float hpFrac = u.StructureMax > 0 ? u.StructureHp / u.StructureMax : 0f;
        Color hpCol = hpFrac switch
        {
            <= 0f   => new Color(UIColors.AccentRed.R, UIColors.AccentRed.G, UIColors.AccentRed.B, 0.35f),
            < 0.33f => UIColors.AccentRed,
            < 0.66f => UIColors.Moving,
            _       => UIColors.AccentGreen,
        };
        _hp.SetFill(hpFrac, hpCol);

        Modulate = u.IsDestroyed ? new Color(1, 1, 1, 0.4f) : new Color(1, 1, 1, 1);
    }
}

/// <summary>One-pixel height progress fill drawn as two rectangles (track + fill).</summary>
public partial class BarFill : Control
{
    private float _fill;
    private Color _color;

    public BarFill(Color color)
    {
        _color = color;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void SetFill(float frac, Color color)
    {
        _fill = Mathf.Clamp(frac, 0f, 1f);
        _color = color;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var w = Size.X;
        var h = Size.Y;
        DrawRect(new Rect2(0, 0, w, h), new Color(1, 1, 1, 0.08f), filled: true);
        if (_fill > 0f)
            DrawRect(new Rect2(0, 0, w * _fill, h), _color, filled: true);
    }
}
