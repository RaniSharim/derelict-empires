using Godot;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Settlements;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// Right-panel variant shown when a colony is the Selected Entity.
/// v3 wiring: header + status row + buildings list (at-rest rows only) + detection block + actions.
/// Expanded slot-chip editing, governor menu, and mitigations are spec'd for later passes.
/// See design/in_system_design.md §8.
/// </summary>
public partial class ColonyEntityPanel : VBoxContainer
{
    public void Populate(Colony colony)
    {
        foreach (var c in GetChildren()) c.QueueFree();
        AddThemeConstantOverride("separation", 8);

        if (colony == null) return;

        // Header row: colony-green accent bar + name + sig/sensor.
        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", 8);
        AddChild(headerRow);

        var accent = new ColorRect
        {
            Color = new Color("#22dd44"),
            CustomMinimumSize = new Vector2(3, 20),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        headerRow.AddChild(accent);

        var name = new Label { Text = string.IsNullOrEmpty(colony.Name) ? $"Colony {colony.Id}" : colony.Name };
        UIFonts.Style(name, UIFonts.Title, 13, UIColors.TextBright);
        headerRow.AddChild(name);

        headerRow.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        headerRow.AddChild(DetectionGlyph.CreateLabel(DetectionGlyph.Kind.Signature, 11, (colony.TotalPopulation * 6).ToString()));
        headerRow.AddChild(DetectionGlyph.CreateLabel(DetectionGlyph.Kind.Sensor,    11, "0"));

        // Status row: pops, happiness, priority.
        var statusLine = new Label
        {
            Text = $"POPS {colony.TotalPopulation}/{colony.PopCap} · HAPPY {(int)colony.Happiness} · PRIO: {PriorityLabel(colony.Priority)}",
        };
        UIFonts.Style(statusLine, UIFonts.Main, UIFonts.SmallSize, UIColors.TextLabel);
        AddChild(statusLine);

        // Buildings section.
        AddSection("BUILDINGS · POPS");
        if (colony.Buildings.Count == 0)
        {
            AddBody("—  no buildings");
        }
        else
        {
            foreach (var bid in colony.Buildings)
                AddBody($"●  {bid}");
        }

        // Detection block.
        AddSection("DETECTION");
        AddBody($"sig sources · pops  |  range 1b  |  observers none");

        // Actions — placeholder buttons in v3.
        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        AddChild(actions);
        foreach (var label in new[] { "CLAIM", "GOV ▾", "DISPATCH" })
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

    private static string PriorityLabel(ColonyPriority p) => p switch
    {
        ColonyPriority.Balanced        => "balanced",
        ColonyPriority.ProductionFocus => "prod",
        ColonyPriority.ResearchFocus   => "res",
        ColonyPriority.GrowthFocus     => "food",
        ColonyPriority.MiningFocus     => "mine",
        _                              => "—",
    };
}
