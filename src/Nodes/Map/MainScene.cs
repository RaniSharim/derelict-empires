using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Nodes.Camera;
using DerlictEmpires.Nodes.UI;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Root scene that assembles the galaxy map, camera, and UI layers.
/// This is the main scene registered in project.godot.
/// </summary>
public partial class MainScene : Node3D
{
    public override void _Ready()
    {
        GD.Print("[MainScene] Starting Derelict Empires...");

        // Set master seed
        if (GameManager.Instance != null)
        {
            GameManager.Instance.MasterSeed = 42;
            GameManager.Instance.CurrentState = Core.Enums.GameState.Playing;
            GameManager.Instance.CurrentSpeed = Core.Enums.GameSpeed.Normal;
        }

        // Galaxy map (3D world)
        var galaxyMap = new GalaxyMap { Name = "GalaxyMap" };
        AddChild(galaxyMap);

        // Camera rig
        var cameraRig = new StrategyCameraRig { Name = "CameraRig" };
        AddChild(cameraRig);

        // Camera3D child of the rig
        var camera = new Camera3D { Name = "Camera3D" };
        camera.Position = new Vector3(0, 80, 24);
        camera.RotationDegrees = new Vector3(-70, 0, 0);
        camera.Far = 1000f;
        cameraRig.AddChild(camera);

        // UI layer (renders on top of 3D)
        var uiLayer = new CanvasLayer { Name = "UILayer" };
        AddChild(uiLayer);

        var topBar = new TopBar { Name = "TopBar" };
        uiLayer.AddChild(topBar);

        var tooltip = new SystemTooltip { Name = "SystemTooltip" };
        uiLayer.AddChild(tooltip);

        GD.Print("[MainScene] Ready");
    }
}
