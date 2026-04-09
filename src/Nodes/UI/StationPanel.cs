using Godot;
using System.Linq;
using DerlictEmpires.Core.Stations;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Panel showing station details: modules, slots, defense stats, construction queue.
/// </summary>
public partial class StationPanel : PanelContainer
{
    private Label _nameLabel = null!;
    private Label _slotsLabel = null!;
    private Label _modulesLabel = null!;
    private Label _statsLabel = null!;
    private Label _queueLabel = null!;

    private Station? _station;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(280, 0);
        AnchorsPreset = (int)LayoutPreset.BottomRight;
        GrowHorizontal = GrowDirection.Begin;
        GrowVertical = GrowDirection.Begin;
        OffsetBottom = -10;
        OffsetRight = -10;

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        AddChild(vbox);

        _nameLabel = new Label { Text = "STATION" };
        _nameLabel.AddThemeFontSizeOverride("font_size", 16);
        vbox.AddChild(_nameLabel);
        vbox.AddChild(new HSeparator());

        _slotsLabel = new Label();
        _slotsLabel.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(_slotsLabel);

        _modulesLabel = new Label();
        _modulesLabel.AddThemeFontSizeOverride("font_size", 11);
        _modulesLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vbox.AddChild(_modulesLabel);

        vbox.AddChild(new HSeparator());

        _statsLabel = new Label();
        _statsLabel.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(_statsLabel);

        _queueLabel = new Label();
        _queueLabel.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(_queueLabel);

        Visible = false;
    }

    public void Show(Station station)
    {
        _station = station;
        Visible = true;
    }

    public void Hide()
    {
        _station = null;
        Visible = false;
    }

    public override void _Process(double delta)
    {
        if (!Visible || _station == null) return;

        _nameLabel.Text = _station.Name;
        _slotsLabel.Text = $"Tier {_station.SizeTier} | Slots: {_station.UsedModuleSlots}/{_station.MaxModuleSlots} | HP: {_station.BaseHp}";

        if (_station.Modules.Count > 0)
        {
            var names = _station.Modules.Select(m => m.DisplayName);
            _modulesLabel.Text = "Modules: " + string.Join(", ", names);
        }
        else
        {
            _modulesLabel.Text = "Modules: none";
        }

        _statsLabel.Text = $"Weapons: {_station.TotalWeaponDamage:F0} | Shield: {_station.TotalShieldHp:F0} | Armor: {_station.TotalArmorHp:F0}\n" +
            $"Supply: {_station.TotalSupplyCapacity:F0} | Garrison: {_station.TotalGarrisonCapacity} | Sensors: {_station.SensorRange}";

        if (!_station.IsConstructed)
            _queueLabel.Text = $"Under construction: {_station.ConstructionProgress * 100:F0}%";
        else if (!_station.ModuleQueue.IsEmpty)
        {
            var current = _station.ModuleQueue.Current!;
            _queueLabel.Text = $"Installing: {current.Item.DisplayName} ({current.Progress * 100:F0}%)";
        }
        else
            _queueLabel.Text = _station.HasShipyard ? "Shipyard ready" : "No shipyard";
    }
}
