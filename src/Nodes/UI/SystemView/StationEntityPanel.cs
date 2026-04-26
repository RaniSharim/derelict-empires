using Godot;
using DerlictEmpires.Core.Stations;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// Right-panel variant for a player Station. See design/in_system_design.md §9.2.
/// Surfaces installed modules with typed glyphs, construction, logistics, and actions.
/// Takes the runtime <see cref="Station"/> to get the typed module list — StationData's
/// placeholder string modules remain unchanged.
/// </summary>
public partial class StationEntityPanel : EntityPanelBase
{
    public void Populate(Station station)
    {
        foreach (var c in GetChildren()) c.QueueFree();
        AddThemeConstantOverride("separation", 8);

        if (station == null) return;

        int sig = station.SizeTier * 15 + station.Modules.Count * 2;
        AddEntityHeader(
            UIColors.SensorIcon,
            string.IsNullOrEmpty(station.Name) ? $"Station {station.Id}" : station.Name,
            sig);

        var status = new Label { Text = $"TIER {station.SizeTier} · HP {station.BaseHp}" };
        UIFonts.Style(status, UIFonts.Main, UIFonts.SmallSize, UIColors.TextLabel);
        AddChild(status);

        AddSection("MODULES");
        int max = station.SizeTier + 1;
        foreach (var m in station.Modules)
            AddBody($"●  {ModuleLabel(m)}");
        for (int i = station.Modules.Count; i < max; i++)
            AddBody("·  + MODULE");

        AddSection("CONSTRUCTION");
        if (station.ShipQueue.Count == 0)
            AddBody("+ QUEUE SHIP");
        else
            AddBody($"◌  building  ({station.ShipQueue.Count} queued)");

        AddSection("LOGISTICS");
        AddBody("supply range —  |  upkeep —  |  stockpile —");

        AddActionsRow(new[] { "UPGRADE", "RENAME", "SCRAP" }, UIColors.TextDim);
    }

    private static string ModuleLabel(StationModule m) => m switch
    {
        ShipyardModule  => "shipyard",
        DefenseModule   => "defense",
        LogisticsModule => "logistics",
        TradeModule     => "trade",
        GarrisonModule  => "garrison",
        SensorModule    => "sensors",
        _               => "module",
    };
}
