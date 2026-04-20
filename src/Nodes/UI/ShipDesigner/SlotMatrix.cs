using System.Collections.Generic;
using Godot;
using DerlictEmpires.Core.Ships;
using DerlictEmpires.Core.Tech;

namespace DerlictEmpires.Nodes.UI.ShipDesigner;

/// <summary>
/// Middle pane of the ShipDesigner. One row per big-system slot. Clicking a row
/// opens the ModulePickerDialog popup. A CLEAR chip is shown inline when the slot
/// is filled so the player can empty it without going through the popup.
/// </summary>
public partial class SlotMatrix : PanelContainer
{
    private readonly ShipDesignerOverlay _overlay;
    private VBoxContainer _slotsColumn = null!;

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
        foreach (var child in _slotsColumn.GetChildren()) child.QueueFree();

        var chassis = _overlay.Draft.GetChassis();
        if (chassis == null) return;

        for (int i = 0; i < chassis.BigSystemSlots; i++)
            _slotsColumn.AddChild(BuildSlotRow(i));
    }

    private Control BuildSlotRow(int slotIndex)
    {
        var container = new PanelContainer();
        container.CustomMinimumSize = new Vector2(0, 36);

        string fillId = slotIndex < _overlay.Draft.SlotFills.Count ? _overlay.Draft.SlotFills[slotIndex] : "";
        bool filled = !string.IsNullOrEmpty(fillId);

        var bg = new StyleBoxFlat
        {
            BgColor = filled
                ? new Color(14 / 255f, 20 / 255f, 40 / 255f, 0.92f)
                : new Color(0, 0, 0, 0.35f),
            BorderColor = UIColors.BorderMid,
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

        // CLEAR is always accessible inline when filled — saves a popup round-trip.
        if (filled)
            row.AddChild(MakeChip("CLEAR", () => _overlay.SetSlot(slotIndex, null)));

        // Row click (except on child chips) opens the picker popup. GuiInput only fires
        // when children don't consume the event, so the CLEAR chip keeps working.
        container.GuiInput += ev =>
        {
            if (ev is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed)
                OpenPicker(slotIndex);
        };

        return container;
    }

    private void OpenPicker(int slotIndex)
    {
        var dialog = new ModulePickerDialog { Name = "ModulePicker" };
        dialog.Configure(_overlay, slotIndex);
        // Parent to the same canvas as the ShipDesigner so it stacks above the designer.
        var host = _overlay.GetParent() ?? (Node)GetTree().Root;
        host.AddChild(dialog);
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
