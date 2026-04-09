using Godot;
using System.Collections.Generic;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Renders all star systems using MultiMeshInstance3D for performance.
/// Each star is a small sphere with color tinting based on precursor color.
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

    public void BuildFromGalaxy(GalaxyData galaxy)
    {
        var mm = new MultiMesh();
        mm.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
        mm.UseColors = true;

        // Create a sphere mesh for stars
        var sphere = new SphereMesh();
        sphere.Radius = 1.5f;
        sphere.Height = 3.0f;
        sphere.RadialSegments = 12;
        sphere.Rings = 6;

        mm.Mesh = sphere;
        mm.InstanceCount = galaxy.Systems.Count;

        for (int i = 0; i < galaxy.Systems.Count; i++)
        {
            var sys = galaxy.Systems[i];
            var xform = new Transform3D(Basis.Identity, new Vector3(sys.PositionX, 0, sys.PositionZ));

            // Scale core stars slightly larger
            if (sys.IsCore)
                xform = xform.Scaled(new Vector3(1.3f, 1.3f, 1.3f));

            mm.SetInstanceTransform(i, xform);

            var color = sys.DominantColor.HasValue
                ? ColorMap.GetValueOrDefault(sys.DominantColor.Value, Colors.White)
                : Colors.White;

            // Brighten the color for emission-like effect
            color = color.Lightened(0.2f);
            mm.SetInstanceColor(i, color);
        }

        Multimesh = mm;

        // Material with vertex color support and emission
        var mat = new StandardMaterial3D();
        mat.VertexColorUseAsAlbedo = true;
        mat.EmissionEnabled = true;
        mat.EmissionEnergyMultiplier = 2.0f;
        mat.Emission = Colors.White;
        mat.EmissionOperator = BaseMaterial3D.EmissionOperatorEnum.Multiply;
        MaterialOverride = mat;
    }
}
