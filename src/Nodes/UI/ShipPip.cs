using Godot;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.UI;

/// <summary>Tiny ship-class pip drawn next to a fleet card. Color + shape encode role/health.</summary>
public partial class ShipPip : Control
{
    private readonly ShipInstanceData? _ship;

    public ShipPip() { }
    public ShipPip(ShipInstanceData? ship) { _ship = ship; }

    public override void _Draw()
    {
        float w = Size.X;
        float h = Size.Y;
        var center = new Vector2(w / 2, h / 2);

        Color c = new Color(80 / 255f, 120 / 255f, 160 / 255f, 0.5f);
        if (_ship != null)
        {
            if (_ship.CurrentHp < _ship.MaxHp)
                c = UIColors.Alert;
            else if (_ship.Role == "Salvager")
                c = UIColors.GreenGlow;
            else if (_ship.Role == "Builder")
                c = UIColors.GoldGlow;
            else if (_ship.SizeClass >= ShipSizeClass.Destroyer)
                c = new Color(120 / 255f, 170 / 255f, 210 / 255f, 0.75f);
        }

        if (_ship?.Role == "Builder")
            DrawRect(new Rect2(0, 0, w, h), c);
        else if (_ship?.Role == "Salvager")
            DrawCircle(center, w / 2, c);
        else if (_ship != null && _ship.SizeClass >= ShipSizeClass.Destroyer)
            DrawRect(new Rect2(0, 0, w, h), c);
        else
        {
            DrawPolygon(new[] {
                new Vector2(w / 2, 0),
                new Vector2(w, h),
                new Vector2(0, h)
            }, new[] { c });
        }
    }
}
