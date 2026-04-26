using Godot;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// Shared base for right-panel entity-detail variants. Provides the entity-header sub-scene
/// loader plus the AddSection / AddBody / AddActionsRow helpers each panel used to duplicate.
/// Panels keep their own dynamic body construction; only the header + section/body framing is shared.
/// </summary>
public abstract partial class EntityPanelBase : VBoxContainer
{
    private static readonly PackedScene HeaderScene =
        GD.Load<PackedScene>("res://scenes/ui/entity_panel_header.tscn");

    /// <summary>Instance the entity_panel_header sub-scene, append it, and configure its accent + name + glyphs.</summary>
    protected EntityPanelHeader AddEntityHeader(
        Color accentColor, string name, int signature, int? sensor = null, bool sigIsApprox = false)
    {
        var header = HeaderScene.Instantiate<EntityPanelHeader>();
        AddChild(header);
        header.Configure(accentColor, name, signature, sensor, sigIsApprox);
        return header;
    }

    /// <summary>Add a small dim section title (10px Main, TextFaint).</summary>
    protected void AddSection(string title)
    {
        var l = new Label { Text = title };
        UIFonts.Style(l, UIFonts.Main, 10, UIColors.TextFaint);
        AddChild(l);
    }

    /// <summary>Add a body line (12px Main, TextBody).</summary>
    protected void AddBody(string text)
    {
        var l = new Label { Text = text };
        UIFonts.Style(l, UIFonts.Main, UIFonts.SmallSize, UIColors.TextBody);
        AddChild(l);
    }

    /// <summary>Add a horizontal row of flat buttons with shared tint. Returns the row for further wiring.</summary>
    protected HBoxContainer AddActionsRow(string[] labels, Color tint)
    {
        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        AddChild(actions);
        foreach (var label in labels)
        {
            var b = new Button { Text = label, Flat = true };
            UIFonts.StyleButtonRole(b, UIFonts.Role.Small, tint);
            actions.AddChild(b);
        }
        return actions;
    }
}
