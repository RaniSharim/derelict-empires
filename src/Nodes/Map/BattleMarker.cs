using Godot;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Pulsing red ring attached to a system while a battle is active there.
/// Fades opacity with a 0.8s sine so the eye catches it without distraction.
/// </summary>
public partial class BattleMarker : Node3D
{
    private MeshInstance3D _ring = null!;
    private StandardMaterial3D _material = null!;
    private float _phase;

    public override void _Ready()
    {
        _ring = new MeshInstance3D();
        var torus = new TorusMesh
        {
            InnerRadius = 3.8f,
            OuterRadius = 4.6f,
            Rings = 18,
            RingSegments = 24,
        };
        _ring.Mesh = torus;

        _material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.94f, 0.21f, 0.19f, 0.75f),
            EmissionEnabled = true,
            Emission = new Color(0.94f, 0.21f, 0.19f),
            EmissionEnergyMultiplier = 2.4f,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };
        _ring.MaterialOverride = _material;
        AddChild(_ring);
    }

    public override void _Process(double delta)
    {
        _phase += (float)delta * 1.8f;
        float pulse = 0.55f + 0.35f * Mathf.Sin(_phase);
        _material.AlbedoColor = new Color(0.94f, 0.21f, 0.19f, pulse);
        _material.EmissionEnergyMultiplier = 1.8f + 0.8f * pulse;
    }
}
