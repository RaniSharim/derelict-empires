using System.Collections.Generic;
using Godot;
using DerlictEmpires.Core.Ships;
using DerlictEmpires.Core.Tech;

namespace DerlictEmpires.Nodes.UI.ShipDesigner;

/// <summary>
/// Middle pane of the ShipDesigner. One row per big-system slot, plus an orphan tray
/// when the chassis swap left slot fills without a home. Hover expands a row to show
/// [CHANGE] / [CLEAR] chips. Row click opens the SlotPickerDropdown inline.
/// </summary>
public partial class SlotMatrix : PanelContainer
{
    private readonly ShipDesignerOverlay _overlay;
    private VBoxContainer _slotsColumn = null!;
    private int _expandedSlot = -1;
    private SlotPickerDropdown? _activeDropdown;

    public SlotMatrix(ShipDesignerOverlay overlay)
    {
        _overlay = overlay;
    }

    public override void _Ready()
    {
        var bg = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0.15f), BorderColor = UIColors.BorderDim };
        bg.SetBorderWidthAll(1);
        bg.SetCornerRadiusAll(0);
        AddThemeStyleboxOverride("panel", bg);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        AddChild(margin);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 10);
        margin.AddChild(col);

        var header = new Label { Text = "BIG SYSTEM SLOTS" };
        UIFonts.StyleRole(header, UIFonts.Role.UILabel);
        col.AddChild(header);

        var scroll = new ScrollContainer();
        scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        col.AddChild(scroll);

        _slotsColumn = new VBoxContainer();
        _slotsColumn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _slotsColumn.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(_slotsColumn);

        Refresh();
    }

    public void Refresh()
    {
        // Rebuild from scratch — cheap enough for 3-slot chassis, and avoids stale cached dropdowns.
        foreach (var child in _slotsColumn.GetChildren()) child.QueueFree();
        _activeDropdown = null;

        var chassis = _overlay.Draft.GetChassis();
        if (chassis == null) return;

        for (int i = 0; i < chassis.BigSystemSlots; i++)
            _slotsColumn.AddChild(BuildSlotRow(i));
    }

    private Control BuildSlotRow(int slotIndex)
    {
        var wrapper = new VBoxContainer();
        wrapper.AddThemeConstantOverride("separation", 0);

        var container = new PanelContainer();
        bool expanded = _expandedSlot == slotIndex;
        container.CustomMinimumSize = new Vector2(0, expanded ? 56 : 36);

        string fillId = slotIndex < _overlay.Draft.SlotFills.Count ? _overlay.Draft.SlotFills[slotIndex] : "";
        bool filled = !string.IsNullOrEmpty(fillId);

        var bg = new StyleBoxFlat
        {
            BgColor = filled
                ? new Color(14 / 255f, 20 / 255f, 40 / 255f, 0.92f)
                : new Color(0, 0, 0, 0.35f),
            BorderColor = expanded ? UIColors.BorderBright : UIColors.BorderMid,
        };
        bg.SetBorderWidthAll(1);
        bg.SetCornerRadiusAll(0);
        bg.ContentMarginLeft = 12;
        bg.ContentMarginRight = 12;
        bg.ContentMarginTop = 6;
        bg.ContentMarginBottom = 6;
        container.AddThemeStyleboxOverride("panel", bg);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        container.AddChild(row);

        var slotTag = new Label { Text = $"S{slotIndex + 1}" };
        UIFonts.StyleRole(slotTag, UIFonts.Role.DataSmall);
        slotTag.CustomMinimumSize = new Vector2(28, 0);
        slotTag.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(slotTag);

        var label = new Label { Text = filled ? DescribeSubsystem(fillId) : "— EMPTY SLOT —" };
        UIFonts.StyleRole(label, UIFonts.Role.TitleMedium,
            filled ? UIColors.TextBright : UIColors.TextFaint);
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.ClipText = true;
        row.AddChild(label);

        if (expanded)
        {
            var actionBtn = MakeChip(filled ? "CHANGE" : "ADD", () =>
            {
                ToggleDropdown(slotIndex, wrapper);
            });
            row.AddChild(actionBtn);

            if (filled)
            {
                var clearBtn = MakeChip("CLEAR", () => _overlay.SetSlot(slotIndex, null));
                row.AddChild(clearBtn);
            }
        }
        else
        {
            var expandBtn = new Button { Text = filled ? "\u2630" : "+" };
            UIFonts.StyleButtonRole(expandBtn, UIFonts.Role.UILabel);
            GlassPanel.StyleButton(expandBtn);
            expandBtn.CustomMinimumSize = new Vector2(28, 24);
            expandBtn.Pressed += () =>
            {
                _expandedSlot = slotIndex;
                Refresh();
            };
            row.AddChild(expandBtn);
        }

        // Clicking anywhere on the row toggles expand — uses GuiInput so child buttons still work.
        container.GuiInput += ev =>
        {
            if (ev is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed)
            {
                _expandedSlot = _expandedSlot == slotIndex ? -1 : slotIndex;
                Refresh();
            }
        };

        wrapper.AddChild(container);

        if (expanded && _activeDropdown != null && _activeDropdown.SlotIndex == slotIndex)
            wrapper.AddChild(_activeDropdown);

        return wrapper;
    }

    private void ToggleDropdown(int slotIndex, VBoxContainer parent)
    {
        if (_activeDropdown != null && _activeDropdown.SlotIndex == slotIndex)
        {
            _activeDropdown.QueueFree();
            _activeDropdown = null;
            return;
        }

        var dd = new SlotPickerDropdown(_overlay, slotIndex);
        _activeDropdown = dd;
        parent.AddChild(dd);
    }

    private string DescribeSubsystem(string id)
    {
        var sub = _overlay.Registry?.GetSubsystem(id);
        if (sub == null) return id.ToUpperInvariant();
        string status = _overlay.ResearchState != null
            ? (_overlay.ResearchState.ResearchedSubsystems.Contains(id) ? "" : " · UNRESEARCHED")
            : "";
        return $"{sub.DisplayName.ToUpperInvariant()}  ·  T{sub.Tier}{status}";
    }

    private static Button MakeChip(string text, System.Action onPress)
    {
        var btn = new Button { Text = text };
        btn.CustomMinimumSize = new Vector2(76, 24);
        UIFonts.StyleButtonRole(btn, UIFonts.Role.UILabel);
        GlassPanel.StyleButton(btn);
        btn.Pressed += () => onPress();
        return btn;
    }
}
