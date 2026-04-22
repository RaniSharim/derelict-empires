using Godot;
using DerlictEmpires.Nodes.UI;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Billboarded explosion icon attached to a system while a battle is active there.
/// Pulses opacity and emission with a 0.8s sine so the eye catches it without distraction.
/// </summary>
public partial class BattleMarker : Node3D
{
    private const string IconPath = "res://assets/icons/combat/spiky_explosion.svg";
    private static readonly Color MarkerColor = new(0.94f, 0.21f, 0.19f);

    private Sprite3D _sprite = null!;
    private float _phase;

    public override void _Ready()
    {
        _sprite = new Sprite3D
        {
            Texture = SvgIcons.Load(IconPath, 128),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            PixelSize = 0.05f,
            Modulate = MarkerColor,
            NoDepthTest = true,
            RenderPriority = 2,
        };
        _sprite.Position = new Vector3(0, 2.2f, 0);
        AddChild(_sprite);
    }

    public override void _Process(double delta)
    {
        _phase += (float)delta * 1.8f;
        float pulse = 0.6f + 0.4f * Mathf.Sin(_phase);
        _sprite.Modulate = new Color(MarkerColor.R, MarkerColor.G, MarkerColor.B, pulse);
    }
}
