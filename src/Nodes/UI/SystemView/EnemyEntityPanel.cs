using Godot;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// Intel-only right-panel variant for a foreign entity. Warning-red accent, Observed + Diplomatic
/// sections only, no management affordances. See design/in_system_design.md §9.4.
/// </summary>
public partial class EnemyEntityPanel : VBoxContainer
{
    public void Populate(POIEntity entity)
    {
        foreach (var c in GetChildren()) c.QueueFree();
        AddThemeConstantOverride("separation", 8);

        if (entity == null) return;

        // Header: warning-red accent + name + sig.
        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", 8);
        AddChild(headerRow);

        var accent = new ColorRect
        {
            Color = UIColors.AccentRed,
            CustomMinimumSize = new Vector2(3, 20),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        headerRow.AddChild(accent);

        var name = new Label { Text = $"{entity.Name} · {entity.Kind}" };
        UIFonts.Style(name, UIFonts.Title, 13, UIColors.TextBright);
        headerRow.AddChild(name);

        headerRow.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        headerRow.AddChild(DetectionGlyph.CreateLabel(DetectionGlyph.Kind.Signature, 11, "~" + entity.Signature));

        // Observed section — what we know from sensors.
        AddSection("OBSERVED");
        AddBody($"kind · {entity.Kind.ToString().ToLower()}");
        AddBody($"owner empire · {entity.OwnerEmpireId}");
        AddBody($"resolution · basic");

        // Diplomatic — placeholder until Diplomacy spec lands.
        AddSection("DIPLOMATIC");
        AddBody("claims · none filed");
        AddBody("relation · neutral");

        // Diplomatic actions only — no management verbs per spec §9.4.
        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        AddChild(actions);
        foreach (var label in new[] { "MESSAGE", "DEMAND", "THREATEN" })
        {
            var b = new Button { Text = label, Flat = true };
            UIFonts.StyleButtonRole(b, UIFonts.Role.Small, UIColors.AccentRed);
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
