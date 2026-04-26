using Godot;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Settlements;
using DerlictEmpires.Core.Visibility;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// Right-panel variant for an Outpost. Simpler than Colony: no buildings, 2-pool allocation,
/// yield rate, detection, actions. See design/in_system_design.md §9.1.
/// </summary>
public partial class OutpostEntityPanel : EntityPanelBase
{
    public void Populate(Outpost outpost)
    {
        foreach (var c in GetChildren()) c.QueueFree();
        AddThemeConstantOverride("separation", 8);

        if (outpost == null) return;

        AddEntityHeader(
            new Color("#ddaa22"),
            string.IsNullOrEmpty(outpost.Name) ? $"Outpost {outpost.Id}" : outpost.Name,
            SignatureCalculator.ForOutpost(outpost.TotalPopulation));

        var status = new Label { Text = $"POPS {outpost.TotalPopulation}/{outpost.PopCap} · {outpost.ExploitationType.ToLower()}" };
        UIFonts.Style(status, UIFonts.Main, UIFonts.SmallSize, UIColors.TextLabel);
        AddChild(status);

        AddSection("ALLOCATION");
        var pools = new Label { Text = $"mining {outpost.GetWorkersIn(WorkPool.Mining)} · production {outpost.GetWorkersIn(WorkPool.Production)}" };
        UIFonts.Style(pools, UIFonts.Main, UIFonts.SmallSize, UIColors.TextBody);
        AddChild(pools);

        AddSection("YIELD");
        AddBody("—  rates pending extraction wiring");

        AddSection("DETECTION");
        AddBody("sig sources · pops  |  range 0.5b  |  observers none");

        AddActionsRow(new[] { "RELOCATE", "UPGRADE", "SCRAP", "GARRISON ▸" }, UIColors.TextDim);
    }
}
