using Godot;

namespace DerlictEmpires.Nodes.UI;

/// <summary>Gold circle icon for the money display.</summary>
public partial class MoneyIcon : Control
{
    public override void _Draw()
    {
        float r = Mathf.Min(Size.X, Size.Y) / 2f;
        var center = Size / 2f;
        DrawCircle(center, r, UIColors.MoneyText);
        DrawCircle(center, r * 0.6f, new Color(0.6f, 0.5f, 0.0f, 0.5f));
    }
}
