using Godot;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Renders navigable lanes between star systems as lines.
/// Visible lanes are bright; hidden lanes are not rendered until discovered.
/// </summary>
public partial class LaneRenderer : MeshInstance3D
{
    private static readonly Color VisibleLaneColor = new(0.3f, 0.4f, 0.5f, 0.6f);
    private static readonly Color ChokepointColor = new(0.6f, 0.3f, 0.2f, 0.8f);

    public void BuildFromGalaxy(GalaxyData galaxy)
    {
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);

        var vertices = new System.Collections.Generic.List<Vector3>();
        var colors = new System.Collections.Generic.List<Color>();

        foreach (var lane in galaxy.Lanes)
        {
            // Only render visible lanes
            if (lane.Type == LaneType.Hidden) continue;

            var sysA = galaxy.Systems[lane.SystemA];
            var sysB = galaxy.Systems[lane.SystemB];

            var posA = new Vector3(sysA.PositionX, 0, sysA.PositionZ);
            var posB = new Vector3(sysB.PositionX, 0, sysB.PositionZ);

            vertices.Add(posA);
            vertices.Add(posB);

            var color = lane.IsChokepoint ? ChokepointColor : VisibleLaneColor;
            colors.Add(color);
            colors.Add(color);
        }

        if (vertices.Count == 0) return;

        arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
        arrays[(int)Mesh.ArrayType.Color] = colors.ToArray();

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);

        var mat = new StandardMaterial3D();
        mat.VertexColorUseAsAlbedo = true;
        mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        mesh.SurfaceSetMaterial(0, mat);

        Mesh = mesh;
    }
}
