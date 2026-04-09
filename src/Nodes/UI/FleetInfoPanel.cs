using Godot;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Side panel showing selected fleet info: name, ship list, location, orders.
/// </summary>
public partial class FleetInfoPanel : PanelContainer
{
    private Label _nameLabel = null!;
    private Label _locationLabel = null!;
    private Label _shipsLabel = null!;
    private Label _ordersLabel = null!;

    private int _selectedFleetId = -1;
    private List<FleetData> _fleets = new();
    private List<ShipInstanceData> _ships = new();

    public override void _Ready()
    {
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        AddChild(vbox);

        var titleLabel = new Label { Text = "FLEET INFO" };
        titleLabel.AddThemeFontSizeOverride("font_size", 16);
        vbox.AddChild(titleLabel);

        vbox.AddChild(new HSeparator());

        _nameLabel = new Label { Text = "" };
        _nameLabel.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(_nameLabel);

        _locationLabel = new Label { Text = "" };
        vbox.AddChild(_locationLabel);

        _shipsLabel = new Label { Text = "" };
        _shipsLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vbox.AddChild(_shipsLabel);

        _ordersLabel = new Label { Text = "" };
        vbox.AddChild(_ordersLabel);

        CustomMinimumSize = new Vector2(250, 0);
        Visible = false;

        // Position at right side
        AnchorsPreset = (int)LayoutPreset.RightWide;
        OffsetLeft = -260;
        OffsetTop = 50;

        EventBus.Instance.FleetSelected += OnFleetSelected;
        EventBus.Instance.FleetDeselected += OnFleetDeselected;
    }

    public void SetData(List<FleetData> fleets, List<ShipInstanceData> ships)
    {
        _fleets = fleets;
        _ships = ships;
    }

    private void OnFleetSelected(int fleetId)
    {
        _selectedFleetId = fleetId;
        Visible = true;
        UpdateDisplay();
    }

    private void OnFleetDeselected()
    {
        _selectedFleetId = -1;
        Visible = false;
    }

    public override void _Process(double delta)
    {
        if (Visible && _selectedFleetId >= 0)
            UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var fleet = _fleets.FirstOrDefault(f => f.Id == _selectedFleetId);
        if (fleet == null) { Visible = false; return; }

        _nameLabel.Text = fleet.Name;

        if (fleet.CurrentSystemId >= 0)
        {
            var sys = GameManager.Instance?.Galaxy?.GetSystem(fleet.CurrentSystemId);
            _locationLabel.Text = sys != null ? $"At: {sys.Name}" : $"System #{fleet.CurrentSystemId}";
        }
        else
        {
            _locationLabel.Text = "In transit...";
        }

        // Ship list
        var fleetShips = _ships.Where(s => fleet.ShipIds.Contains(s.Id)).ToList();
        var shipSummary = fleetShips
            .GroupBy(s => s.Role)
            .Select(g => $"  {g.Key}: {g.Count()}")
            .ToList();
        _shipsLabel.Text = $"Ships ({fleetShips.Count}):\n" + string.Join("\n", shipSummary);

        _ordersLabel.Text = "Orders: Idle";
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.FleetSelected -= OnFleetSelected;
            EventBus.Instance.FleetDeselected -= OnFleetDeselected;
        }
    }
}
