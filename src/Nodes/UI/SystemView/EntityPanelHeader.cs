using Godot;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// Shared header for every entity-detail variant in the System View right panel.
/// 3px accent strip + name (Exo 2 13px bright) + spacer + sig/sensor glyphs.
/// Layout: scenes/ui/entity_panel_header.tscn.
/// </summary>
public partial class EntityPanelHeader : HBoxContainer
{
    [Export] private ColorRect _accent = null!;
    [Export] private Label _nameLabel = null!;
    [Export] private HBoxContainer _glyphSlot = null!;

    public override void _Ready()
    {
        UIFonts.Style(_nameLabel, UIFonts.Title, 13, UIColors.TextBright);
    }

    /// <summary>Set accent color, name, and detection glyphs. Pass sensor=null to skip the sensor glyph.</summary>
    public void Configure(Color accentColor, string name, int signature, int? sensor = null, bool sigIsApprox = false)
    {
        _accent.Color = accentColor;
        _nameLabel.Text = name;

        foreach (var c in _glyphSlot.GetChildren()) c.QueueFree();
        string sigText = (sigIsApprox ? "~" : "") + signature.ToString();
        _glyphSlot.AddChild(DetectionGlyph.CreateLabel(DetectionGlyph.Kind.Signature, 11, sigText));
        if (sensor.HasValue)
            _glyphSlot.AddChild(DetectionGlyph.CreateLabel(DetectionGlyph.Kind.Sensor, 11, sensor.Value.ToString()));
    }
}
