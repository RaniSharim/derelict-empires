using Godot;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// Intel-only right-panel variant for a foreign entity. Warning-red accent, Observed + Diplomatic
/// sections only, no management affordances. See design/in_system_design.md §9.4.
/// </summary>
public partial class EnemyEntityPanel : EntityPanelBase
{
    public void Populate(POIEntity entity)
    {
        foreach (var c in GetChildren()) c.QueueFree();
        AddThemeConstantOverride("separation", 8);

        if (entity == null) return;

        AddEntityHeader(
            UIColors.AccentRed,
            $"{entity.Name} · {entity.Kind}",
            entity.Signature,
            sigIsApprox: true);

        AddSection("OBSERVED");
        AddBody($"kind · {entity.Kind.ToString().ToLower()}");
        AddBody($"owner empire · {entity.OwnerEmpireId}");
        AddBody($"resolution · basic");

        AddSection("DIPLOMATIC");
        AddBody("claims · none filed");
        AddBody("relation · neutral");

        AddActionsRow(new[] { "MESSAGE", "DEMAND", "THREATEN" }, UIColors.AccentRed);
    }
}
