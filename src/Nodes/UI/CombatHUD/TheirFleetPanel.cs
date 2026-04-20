using System.Collections.Generic;
using Godot;
using DerlictEmpires.Core.Combat;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Nodes.UI.CombatHUD;

/// <summary>
/// Right column during combat. Mirrors OurFleetPanel structure but no player
/// controls; information density depends on scan state. MVP: always "no scan"
/// so counts only. Blue Pre-Combat Scan upgrades this view (deferred).
/// </summary>
public partial class TheirFleetPanel : PanelContainer
{
    private BattleManager? _manager;
    private int _battleId;

    private Label _headerLabel = null!;
    private Label _scanStateLabel = null!;
    private VBoxContainer _rolesColumn = null!;
    private SalvageProjectionChip _salvageChip = null!;
    private readonly Dictionary<FleetRole, VBoxContainer> _roleRows = new();

    public void Configure(BattleManager manager, int battleId)
    {
        _manager = manager;
        _battleId = battleId;
    }

    public override void _Ready()
    {
        var bg = new StyleBoxFlat { BgColor = UIColors.GlassDark, BorderColor = UIColors.BorderMid };
        bg.SetBorderWidthAll(0);
        bg.BorderWidthLeft = 1;
        bg.SetCornerRadiusAll(0);
        bg.ContentMarginLeft = 14;
        bg.ContentMarginRight = 14;
        bg.ContentMarginTop = 14;
        bg.ContentMarginBottom = 14;
        AddThemeStyleboxOverride("panel", bg);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 10);
        AddChild(col);

        _headerLabel = new Label { Text = "HOSTILE FLEET" };
        UIFonts.StyleRole(_headerLabel, UIFonts.Role.Title, UIColors.AccentRed);
        col.AddChild(_headerLabel);

        _scanStateLabel = new Label { Text = "\u25A1 NO SCAN \u2014 COMPOSITION UNKNOWN" };
        UIFonts.StyleRole(_scanStateLabel, UIFonts.Role.Small);
        col.AddChild(_scanStateLabel);

        var sep = new ColorRect { Color = UIColors.BorderDim };
        sep.CustomMinimumSize = new Vector2(0, 1);
        col.AddChild(sep);

        _rolesColumn = new VBoxContainer();
        _rolesColumn.AddThemeConstantOverride("separation", 10);
        _rolesColumn.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        col.AddChild(_rolesColumn);

        _salvageChip = new SalvageProjectionChip { Name = "SalvageProjectionChip" };
        col.AddChild(_salvageChip);
    }

    public void Refresh()
    {
        if (_manager == null) return;
        var battle = _manager.GetBattle(_battleId);
        var agg = _manager.GetDefenderAggregate(_battleId);

        // Salvage projection — rough proxy: destroyed units = wrecks, very-low-HP units = debris.
        if (battle != null)
        {
            int wrecks = 0, debris = 0;
            foreach (var u in battle.Defenders)
            {
                if (u.IsDestroyed) wrecks++;
                else if (u.StructureMax > 0 && u.StructureHp / u.StructureMax < 0.1f) debris++;
            }
            _salvageChip.UpdateFrom(wrecks, debris, wrecks * 50);
        }

        var seen = new HashSet<FleetRole>();
        foreach (var slice in agg.Roles)
        {
            seen.Add(slice.Role);
            if (!_roleRows.TryGetValue(slice.Role, out var row))
            {
                row = BuildRoleRow();
                _roleRows[slice.Role] = row;
                _rolesColumn.AddChild(row);
            }
            PopulateRow(row, slice);
        }
        foreach (var key in new List<FleetRole>(_roleRows.Keys))
        {
            if (seen.Contains(key)) continue;
            _roleRows[key].QueueFree();
            _roleRows.Remove(key);
        }
    }

    private VBoxContainer BuildRoleRow()
    {
        var row = new VBoxContainer();
        row.AddThemeConstantOverride("separation", 4);

        var title = new Label { Name = "Title" };
        UIFonts.StyleRole(title, UIFonts.Role.Small, UIColors.TextLabel);
        row.AddChild(title);

        var hpBar = new HpPipsBar { Name = "HpBar" };
        hpBar.CustomMinimumSize = new Vector2(0, 8);
        row.AddChild(hpBar);

        return row;
    }

    private static void PopulateRow(VBoxContainer row, RoleSlice slice)
    {
        // Heuristic: with "no scan", we hide exact counts; fudge to a dash-count.
        var title = row.GetNode<Label>("Title");
        title.Text = $"\u25CF UNKNOWN \u00B7 {slice.AliveCount} ship{(slice.AliveCount == 1 ? "" : "s")}";

        var hpBar = row.GetNode<HpPipsBar>("HpBar");
        hpBar.Pips = slice.HpPips.ToArray();
        hpBar.QueueRedraw();
    }
}
