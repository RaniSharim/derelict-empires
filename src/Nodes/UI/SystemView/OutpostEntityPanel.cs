using Godot;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Settlements;
using DerlictEmpires.Core.Visibility;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// Right-panel variant for an Outpost. Simpler than Colony: no buildings, 2-pool allocation,
/// yield rate, detection, actions. See design/in_system_design.md §9.1.
/// </summary>
public partial class OutpostEntityPanel : VBoxContainer
{
    public void Populate(Outpost outpost)
    {
        foreach (var c in GetChildren()) c.QueueFree();
        AddThemeConstantOverride("separation", 8);

        if (outpost == null) return;

        // Header row: gold accent + name + sig.
        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", 8);
        AddChild(headerRow);

        var accent = new ColorRect
        {
            Color = new Color("#ddaa22"),
            CustomMinimumSize = new Vector2(3, 20),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        headerRow.AddChild(accent);

        var name = new Label { Text = string.IsNullOrEmpty(outpost.Name) ? $"Outpost {outpost.Id}" : outpost.Name };
        UIFonts.Style(name, UIFonts.Title, 13, UIColors.TextBright);
        headerRow.AddChild(name);

        headerRow.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        headerRow.AddChild(DetectionGlyph.CreateLabel(DetectionGlyph.Kind.Signature, 11, SignatureCalculator.ForOutpost(outpost.TotalPopulation).ToString()));

        // Status: pop + exploit focus.
        var status = new Label { Text = $"POPS {outpost.TotalPopulation}/{outpost.PopCap} · {outpost.ExploitationType.ToLower()}" };
        UIFonts.Style(status, UIFonts.Main, UIFonts.SmallSize, UIColors.TextLabel);
        AddChild(status);

        // Allocation (2-pool stacked bar — placeholder text for now, full bar widget lands with pop UX).
        AddSection("ALLOCATION");
        var pools = new Label { Text = $"mining {outpost.GetWorkersIn(WorkPool.Mining)} · production {outpost.GetWorkersIn(WorkPool.Production)}" };
        UIFonts.Style(pools, UIFonts.Main, UIFonts.SmallSize, UIColors.TextBody);
        AddChild(pools);

        // Yield (placeholder until ExtractionSystem surfaces per-outpost rates here).
        AddSection("YIELD");
        AddBody("—  rates pending extraction wiring");

        // Detection block.
        AddSection("DETECTION");
        AddBody("sig sources · pops  |  range 0.5b  |  observers none");

        // Actions.
        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        AddChild(actions);
        foreach (var label in new[] { "RELOCATE", "UPGRADE", "SCRAP", "GARRISON ▸" })
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
}
