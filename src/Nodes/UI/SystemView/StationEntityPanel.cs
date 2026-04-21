using Godot;
using DerlictEmpires.Core.Stations;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// Right-panel variant for a player Station. See design/in_system_design.md §9.2.
/// Surfaces installed modules with typed glyphs, construction, logistics, and actions.
/// Takes the runtime <see cref="Station"/> to get the typed module list — StationData's
/// placeholder string modules remain unchanged.
/// </summary>
public partial class StationEntityPanel : VBoxContainer
{
    public void Populate(Station station)
    {
        foreach (var c in GetChildren()) c.QueueFree();
        AddThemeConstantOverride("separation", 8);

        if (station == null) return;

        // Header: azure accent + name.
        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", 8);
        AddChild(headerRow);

        var accent = new ColorRect
        {
            Color = UIColors.SensorIcon,
            CustomMinimumSize = new Vector2(3, 20),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        headerRow.AddChild(accent);

        var name = new Label { Text = string.IsNullOrEmpty(station.Name) ? $"Station {station.Id}" : station.Name };
        UIFonts.Style(name, UIFonts.Title, 13, UIColors.TextBright);
        headerRow.AddChild(name);

        headerRow.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        int sig = station.SizeTier * 15 + station.Modules.Count * 2;
        headerRow.AddChild(DetectionGlyph.CreateLabel(DetectionGlyph.Kind.Signature, 11, sig.ToString()));

        // Status: tier + HP.
        var status = new Label { Text = $"TIER {station.SizeTier} · HP {station.BaseHp}" };
        UIFonts.Style(status, UIFonts.Main, UIFonts.SmallSize, UIColors.TextLabel);
        AddChild(status);

        // Modules.
        AddSection("MODULES");
        int max = station.SizeTier + 1;
        foreach (var m in station.Modules)
            AddBody($"●  {ModuleLabel(m)}");
        for (int i = station.Modules.Count; i < max; i++)
            AddBody("·  + MODULE");

        // Construction queue head.
        AddSection("CONSTRUCTION");
        if (station.ShipQueue.Count == 0)
            AddBody("+ QUEUE SHIP");
        else
            AddBody($"◌  building  ({station.ShipQueue.Count} queued)");

        // Logistics readout (placeholders — real numbers come via Station aggregates later).
        AddSection("LOGISTICS");
        AddBody("supply range —  |  upkeep —  |  stockpile —");

        // Actions.
        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        AddChild(actions);
        foreach (var label in new[] { "UPGRADE", "RENAME", "SCRAP" })
        {
            var b = new Button { Text = label, Flat = true };
            UIFonts.StyleButtonRole(b, UIFonts.Role.Small, UIColors.TextDim);
            actions.AddChild(b);
        }
    }

    private void AddSection(string title)
    {
        var l = new Label { Text = title };
        UIFonts.Style(l, UIFonts.Main, 10, UIColors.TextFaint);
        AddChild(l);
    }

    private void AddBody(string text)
    {
        var l = new Label { Text = text };
        UIFonts.Style(l, UIFonts.Main, UIFonts.SmallSize, UIColors.TextBody);
        AddChild(l);
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
