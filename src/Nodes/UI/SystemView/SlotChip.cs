using System;
using Godot;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// A single pop-slot chip inside a BuildingRow. Renders ● filled / · empty / ◆ expert-filled
/// / ◇ expert-empty. Click fires a caller-supplied action. See design/in_system_design.md §8.2.
/// </summary>
public partial class SlotChip : Button
{
    public enum Kind { Worker, Expert }

    public Kind ChipKind { get; }
    public bool Filled { get; private set; }
    public int  SlotIndex { get; }

    public Action? OnClicked;

    public SlotChip(Kind kind, bool filled, int slotIndex)
    {
        ChipKind = kind;
        Filled = filled;
        SlotIndex = slotIndex;
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(16, 16);
        Flat = true;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        Refresh();
        Pressed += () => OnClicked?.Invoke();
    }

    public void SetFilled(bool filled)
    {
        Filled = filled;
        Refresh();
    }

    private void Refresh()
    {
        Text = (ChipKind, Filled) switch
        {
            (Kind.Worker, true)  => "●",
            (Kind.Worker, false) => "·",
            (Kind.Expert, true)  => "◆",
            (Kind.Expert, false) => "◇",
            _                    => "·",
        };
        var tint = (ChipKind, Filled) switch
        {
            (Kind.Worker, true)  => UIColors.SigIcon,    // yellow per spec §13.2 SlotFilled
            (Kind.Worker, false) => UIColors.TextFaint,
            (Kind.Expert, true)  => UIColors.SensorIcon, // azure per spec §13.2 SlotExpert
            (Kind.Expert, false) => UIColors.TextFaint,
            _                    => UIColors.TextFaint,
        };
        UIFonts.StyleButtonRole(this, UIFonts.Role.Small, tint);
    }
}
