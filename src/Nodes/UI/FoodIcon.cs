using Godot;

namespace DerlictEmpires.Nodes.UI;

/// <summary>Brown circle icon for the food display.</summary>
public partial class FoodIcon : Control
{
    public override void _Draw()
    {
        float r = Mathf.Min(Size.X, Size.Y) / 2f;
        var center = Size / 2f;
        DrawCircle(center, r, UIColors.FoodText);
        DrawCircle(center, r * 0.6f, new Color(0.5f, 0.35f, 0.15f, 0.5f));
    }
}
