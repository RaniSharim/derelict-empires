using Godot;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Draws a pre-loaded SVG/Texture2D filling the control's rect, tinted by <see cref="Tint"/>.
/// Parameterless so it can be attached to nodes inside a .tscn; populate via the setters.
/// </summary>
public partial class ResourceIcon : Control
{
    private Texture2D? _texture;
    private Color _tint = Colors.White;

    public Texture2D? Texture
    {
        get => _texture;
        set { _texture = value; QueueRedraw(); }
    }

    public Color Tint
    {
        get => _tint;
        set { _tint = value; QueueRedraw(); }
    }

    public override void _Draw()
    {
        if (_texture != null)
            DrawTextureRect(_texture, new Rect2(Vector2.Zero, Size), false, _tint);
    }
}
