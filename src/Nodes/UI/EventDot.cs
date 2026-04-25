using Godot;

namespace DerlictEmpires.Nodes.UI;

/// <summary>Colored dot with a soft halo, used in the event log per spec §5.8.</summary>
public partial class EventDot : Control
{
    private Color _color = Colors.White;

    [Export] public Color DotColor
    {
        get => _color;
        set { _color = value; QueueRedraw(); }
    }

    public override void _Draw()
    {
        var center = Size / 2f;
        float radius = Mathf.Min(Size.X, Size.Y) / 2f;
        DrawCircle(center, radius + 2f, new Color(_color, 0.2f));
        DrawCircle(center, radius, _color);
    }
}
