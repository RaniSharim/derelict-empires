using Godot;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Bottom-left galaxy overview panel. Layout in <c>scenes/ui/minimap.tscn</c>;
/// drawing in <see cref="MinimapCanvas"/>. This shell binds the glass background
/// onto the panel container.
/// </summary>
public partial class Minimap : Control
{
    [Export] private PanelContainer _background = null!;

    public override void _Ready()
    {
        GlassPanel.Apply(_background, enableBlur: false);
    }
}
