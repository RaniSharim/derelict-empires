using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Ships;

namespace DerlictEmpires.Nodes.UI.ShipDesigner;

/// <summary>
/// Modal sub-overlay listing all 14 chassis in a 7×2 grid. Clicking a cell
/// swaps the draft's chassis via ShipDesignerOverlay.SetChassis. Availability
/// badges reflect whether the player has a compatible shipyard at home.
/// </summary>
public partial class ChassisPickerSubOverlay : GlassOverlay
{
    private ShipDesignerOverlay? _parent;

    public ChassisPickerSubOverlay()
    {
        OverlayTitle = "SELECT CHASSIS";
    }

    public void Configure(ShipDesignerOverlay parent)
    {
        _parent = parent;
    }

    public override void _Ready()
    {
        base._Ready();
        BuildGrid();
    }

    private void BuildGrid()
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 24);
        margin.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 12);
        margin.AddChild(col);

        var hint = new Label { Text = "CHASSIS FAMILIES — 7 SIZES × 2 VARIANTS" };
        UIFonts.StyleRole(hint, UIFonts.Role.UILabel);
        col.AddChild(hint);

        // Group by size class, row per size, 2 variants per row.
        var byClass = ChassisData.All
            .GroupBy(c => c.SizeClass)
            .OrderBy(g => (int)g.Key)
            .ToList();

        foreach (var group in byClass)
        {
            var header = new Label { Text = group.Key.ToString().ToUpperInvariant() };
            UIFonts.StyleRole(header, UIFonts.Role.TitleMedium);
            col.AddChild(header);

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 12);
            foreach (var chassis in group.OrderBy(c => c.Variant))
                row.AddChild(BuildCell(chassis));
            col.AddChild(row);
        }

        Body.AddChild(margin);
    }

    private Control BuildCell(ChassisData chassis)
    {
        var btn = new Button();
        btn.CustomMinimumSize = new Vector2(260, 72);
        btn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        btn.FocusMode = Control.FocusModeEnum.None;

        bool isCurrent = _parent != null && _parent.Draft.ChassisId == chassis.Id;

        var normal = new StyleBoxFlat
        {
            BgColor = new Color(14 / 255f, 20 / 255f, 40 / 255f, 0.92f),
            BorderColor = isCurrent ? UIColors.Accent : UIColors.BorderMid,
        };
        normal.SetBorderWidthAll(1);
        normal.SetCornerRadiusAll(2);
        normal.ContentMarginLeft = 12;
        normal.ContentMarginRight = 12;
        normal.ContentMarginTop = 10;
        normal.ContentMarginBottom = 10;

        var hover = new StyleBoxFlat
        {
            BgColor = new Color(22 / 255f, 30 / 255f, 56 / 255f, 0.97f),
            BorderColor = UIColors.BorderBright,
        };
        hover.SetBorderWidthAll(1);
        hover.SetCornerRadiusAll(2);
        hover.ContentMarginLeft = 12;
        hover.ContentMarginRight = 12;
        hover.ContentMarginTop = 10;
        hover.ContentMarginBottom = 10;

        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        btn.AddThemeStyleboxOverride("focus", normal);

        btn.Pressed += () =>
        {
            _parent?.SetChassis(chassis.Id);
            RequestClose();
        };

        // Content overlay
        var content = new MarginContainer();
        content.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        content.AddThemeConstantOverride("margin_left", 12);
        content.AddThemeConstantOverride("margin_right", 12);
        content.AddThemeConstantOverride("margin_top", 8);
        content.AddThemeConstantOverride("margin_bottom", 8);
        content.MouseFilter = Control.MouseFilterEnum.Ignore;
        btn.AddChild(content);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 4);
        col.MouseFilter = Control.MouseFilterEnum.Ignore;
        content.AddChild(col);

        var nameRow = new HBoxContainer();
        nameRow.AddThemeConstantOverride("separation", 6);
        nameRow.MouseFilter = Control.MouseFilterEnum.Ignore;
        col.AddChild(nameRow);

        var name = new Label { Text = chassis.DisplayName.ToUpperInvariant() };
        UIFonts.StyleRole(name, UIFonts.Role.TitleMedium);
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        nameRow.AddChild(name);

        // Availability — "✓" if the chassis size <= Cruiser, otherwise SHIPYARD requirement.
        var avail = chassis.SizeClass <= ShipSizeClass.Cruiser
            ? ("\u2713", UIColors.DeltaPos)
            : ("\u26a0 SHIPYARD", UIColors.Moving);
        var availLabel = new Label { Text = avail.Item1 };
        UIFonts.Style(availLabel, UIFonts.Main, UIFonts.SmallSize, avail.Item2);
        availLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        nameRow.AddChild(availLabel);

        var stats = new Label
        {
            Text = $"SLOTS {chassis.BigSystemSlots}  ·  CAP {chassis.FreeCapacity}  ·  HP {chassis.BaseHp}  ·  SPD {chassis.BaseSpeed:0.#}"
        };
        UIFonts.StyleRole(stats, UIFonts.Role.DataSmall);
        col.AddChild(stats);

        return btn;
    }
}
