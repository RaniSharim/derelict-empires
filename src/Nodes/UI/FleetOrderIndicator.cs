using Godot;
using System.Collections.Generic;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Renders a path line on the galaxy map showing a fleet's planned route.
/// </summary>
public partial class FleetOrderIndicator : MeshInstance3D
{
    private static readonly Color PathColor = new(0.3f, 1.0f, 0.5f, 0.6f);

    public void ShowPath(GalaxyData galaxy, int fromSystemId, List<int> path)
    {
        if (path.Count == 0)
        {
            Mesh = null;
            return;
        }

        var vertices = new List<Vector3>();
        var colors = new List<Color>();

        // Start from current position
        var fromSys = galaxy.GetSystem(fromSystemId);
        if (fromSys == null) return;

        var prevPos = new Vector3(fromSys.PositionX, 0.5f, fromSys.PositionZ);

        foreach (int sysId in path)
        {
            var sys = galaxy.GetSystem(sysId);
            if (sys == null) continue;

            var pos = new Vector3(sys.PositionX, 0.5f, sys.PositionZ);
            vertices.Add(prevPos);
            vertices.Add(pos);
            colors.Add(PathColor);
            colors.Add(PathColor);
            prevPos = pos;
        }

        if (vertices.Count == 0)
        {
            Mesh = null;
            return;
        }

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Godot.Mesh.ArrayType.Max);
        arrays[(int)Godot.Mesh.ArrayType.Vertex] = vertices.ToArray();
        arrays[(int)Godot.Mesh.ArrayType.Color] = colors.ToArray();

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Lines, arrays);

        var mat = new StandardMaterial3D();
        mat.VertexColorUseAsAlbedo = true;
        mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        mesh.SurfaceSetMaterial(0, mat);

        Mesh = mesh;
    }

    public void Clear()
    {
        Mesh = null;
    }
}
