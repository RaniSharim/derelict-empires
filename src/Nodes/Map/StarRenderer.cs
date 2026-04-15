using Godot;
using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Renders all star systems using MultiMeshInstance3D with billboard quads
/// and a custom glow shader. Each star is a camera-facing quad with radial
/// falloff glow, faction-colored tint, and desynchronized GPU-driven twinkle.
/// </summary>
public partial class StarRenderer : MultiMeshInstance3D
{
    private static readonly Dictionary<PrecursorColor, Color> ColorMap = new()
    {
        [PrecursorColor.Red] = new Color(1.0f, 0.3f, 0.2f),
        [PrecursorColor.Blue] = new Color(0.3f, 0.5f, 1.0f),
        [PrecursorColor.Green] = new Color(0.2f, 0.9f, 0.3f),
        [PrecursorColor.Gold] = new Color(1.0f, 0.85f, 0.2f),
        [PrecursorColor.Purple] = new Color(0.7f, 0.2f, 0.9f),
    };

    // Golden ratio fractional part — produces well-distributed quasi-random sequence
    private const float GoldenRatio = 0.618034f;

    public void BuildFromGalaxy(GalaxyData galaxy)
    {
        var mm = new MultiMesh();
        mm.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        mm.UseColors = true;
        mm.UseCustomData = true;

        // Billboard quad — the glow shader handles radial falloff and core rendering
        var quad = new QuadMesh();
        quad.Size = new Vector2(10f, 10f);

        mm.Mesh = quad;
        mm.InstanceCount = galaxy.Systems.Count;

        for (int i = 0; i < galaxy.Systems.Count; i++)
        {
            var sys = galaxy.Systems[i];
            var basis = Basis.Identity;

            // Core stars get a larger glow radius
            if (sys.IsCore)
                basis = basis.Scaled(new Vector3(1.3f, 1.3f, 1.3f));

            // Slight Y offset so stars render above lane lines
            var xform = new Transform3D(basis, new Vector3(sys.PositionX, 0.1f, sys.PositionZ));
            mm.SetInstanceTransform(i, xform);

            var color = sys.DominantColor.HasValue
                ? ColorMap.GetValueOrDefault(sys.DominantColor.Value, Colors.White)
                : Colors.White;

            // Brighten for HDR glow — values above 1.0 trigger environment bloom
            color = color.Lightened(0.2f);
            mm.SetInstanceColor(i, color);

            // Deterministic phase offset for desynchronized twinkle (no RNG needed)
            float phase = (sys.Id * GoldenRatio) % 1.0f;
            mm.SetInstanceCustomData(i, new Color(phase, 0, 0, 0));
        }

        Multimesh = mm;

        // Glow shader material — shared across all instances for draw-call batching
        if (ResourceLoader.Exists("res://shaders/star_glow.gdshader"))
        {
            var shader = GD.Load<Shader>("res://shaders/star_glow.gdshader");
            var mat = new ShaderMaterial();
            mat.Shader = shader;
            MaterialOverride = mat;
        }
        else
        {
            // Fallback: emission material if shader not found
            var mat = new StandardMaterial3D();
            mat.VertexColorUseAsAlbedo = true;
            mat.EmissionEnabled = true;
            mat.EmissionEnergyMultiplier = 2.0f;
            mat.Emission = Colors.White;
            mat.EmissionOperator = BaseMaterial3D.EmissionOperatorEnum.Multiply;
            MaterialOverride = mat;
        }
    }
}
