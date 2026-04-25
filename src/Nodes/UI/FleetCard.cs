using Godot;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// One row in the FLEETS tab of the LeftPanel. Reused N times (one per fleet).
/// Layout in <c>scenes/ui/fleet_card.tscn</c>; populated via <see cref="Populate"/>.
/// Click → fires EventBus selection events; Ctrl+click → toggle; double-click → pan camera.
/// </summary>
public partial class FleetCard : MarginContainer
{
    [Export] private Button _button = null!;
    [Export] private ColorRect _accentStrip = null!;
    [Export] private Label _name = null!;
    [Export] private Label _status = null!;
    [Export] private Label _id = null!;
    [Export] private Label _location = null!;
    [Export] private HBoxContainer _pipsRow = null!;

    private int _fleetId = -1;
    private bool _ctrlModifier;

    public override void _Ready()
    {
        UIFonts.Style(_name,     UIFonts.Title, UIFonts.TitleSize, UIColors.TextBright);
        UIFonts.Style(_status,   UIFonts.Main,  UIFonts.SmallSize, UIColors.TextDim);
        UIFonts.Style(_id,       UIFonts.Main,  UIFonts.SmallSize, UIColors.TextDim);
        UIFonts.Style(_location, UIFonts.Main,  UIFonts.NormalSize, UIColors.TextBody);

        _button.GuiInput += OnButtonGuiInput;
        _button.Pressed += OnButtonPressed;
    }

    public void Populate(
        FleetData fleet,
        IReadOnlyList<ShipInstanceData> shipsInFleet,
        string statusText,
        Color statusColor,
        Color accentColor,
        bool isSelected,
        string locationText,
        string tooltipText)
    {
        _fleetId = fleet.Id;
        _name.Text = fleet.Name.ToUpper();
        _status.Text = statusText;
        _status.AddThemeColorOverride("font_color", statusColor);
        _id.Text = $"#fcc{fleet.Id:X2}";
        _location.Text = locationText;
        _accentStrip.Color = new Color(accentColor, isSelected ? 1.0f : 0.6f);
        _button.TooltipText = tooltipText;

        foreach (var child in _pipsRow.GetChildren())
            child.QueueFree();
        for (int i = 0; i < Mathf.Min(shipsInFleet.Count, 12); i++)
        {
            var pip = new ShipPip(shipsInFleet[i]);
            pip.CustomMinimumSize = new Vector2(6, 6);
            pip.MouseFilter = MouseFilterEnum.Ignore;
            _pipsRow.AddChild(pip);
        }
    }

    private void OnButtonGuiInput(InputEvent ev)
    {
        if (ev is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed)
        {
            _ctrlModifier = mb.CtrlPressed;
            if (mb.DoubleClick)
                EventBus.Instance?.FireFleetDoubleClicked(_fleetId);
        }
    }

    private void OnButtonPressed()
    {
        bool ctrl = _ctrlModifier || Input.IsKeyPressed(Key.Ctrl);
        _ctrlModifier = false;
        if (ctrl) EventBus.Instance?.FireFleetSelectionToggled(_fleetId);
        else EventBus.Instance?.FireFleetSelected(_fleetId);
    }
}
