using Godot;
using System;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Random;
using DerlictEmpires.Core.Systems;
using DerlictEmpires.Nodes.Camera;
using DerlictEmpires.Nodes.UI;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Root scene that assembles the galaxy map, camera, and UI layers.
/// Shows a setup dialog first, then starts the game.
/// </summary>
public partial class MainScene : Node3D
{
    private CanvasLayer _uiLayer = null!;
    private GameSetupDialog _setupDialog = null!;
    private GameSetupManager.SetupResult _setupResult = null!;

    public override void _Ready()
    {
        GD.Print("[MainScene] Starting Derelict Empires...");

        // Pause until setup is complete
        if (GameManager.Instance != null)
        {
            GameManager.Instance.MasterSeed = 42;
            GameManager.Instance.CurrentState = GameState.Setup;
            GameManager.Instance.CurrentSpeed = GameSpeed.Paused;
        }

        // Generate galaxy immediately (visible behind dialog)
        var galaxyMap = new GalaxyMap { Name = "GalaxyMap" };
        AddChild(galaxyMap);

        // Camera rig
        var cameraRig = new StrategyCameraRig { Name = "CameraRig" };
        AddChild(cameraRig);

        var camera = new Camera3D { Name = "Camera3D" };
        camera.Position = new Vector3(0, 80, 24);
        camera.RotationDegrees = new Vector3(-70, 0, 0);
        camera.Far = 1000f;
        cameraRig.AddChild(camera);

        // UI layer
        _uiLayer = new CanvasLayer { Name = "UILayer" };
        AddChild(_uiLayer);

        var topBar = new TopBar { Name = "TopBar" };
        _uiLayer.AddChild(topBar);

        var tooltip = new SystemTooltip { Name = "SystemTooltip" };
        _uiLayer.AddChild(tooltip);

        // Show setup dialog
        _setupDialog = new GameSetupDialog { Name = "SetupDialog" };
        _setupDialog.SetupConfirmed += OnSetupConfirmed;
        _uiLayer.AddChild(_setupDialog);

        GD.Print("[MainScene] Showing setup dialog...");
    }

    private void OnSetupConfirmed(int colorIndex, int originIndex)
    {
        var affinity = (PrecursorColor)colorIndex;
        var origin = (Origin)originIndex;

        GD.Print($"[MainScene] Player chose {affinity} {origin}");

        var gm = GameManager.Instance;
        if (gm?.Galaxy == null) return;

        var rng = new GameRandom(gm.MasterSeed);
        var setupManager = new GameSetupManager();
        _setupResult = new GameSetupManager.SetupResult();

        // Create player empire
        var playerEmpire = setupManager.CreatePlayerEmpire(
            "Player Empire", affinity, origin, gm.Galaxy, _setupResult, rng.DeriveChild("player"));

        // Create some AI empires
        int aiCount = 4;
        for (int i = 0; i < aiCount; i++)
        {
            setupManager.CreateAIEmpire(gm.Galaxy, _setupResult, rng.DeriveChild(i + 1000));
        }

        // Store empires in GameManager
        gm.Empires = _setupResult.Empires;

        // Log results
        foreach (var empire in _setupResult.Empires)
        {
            var homeSystem = gm.Galaxy.GetSystem(empire.HomeSystemId);
            GD.Print($"  Empire: {empire.Name} | {empire.Affinity} {empire.Origin} | Home: {homeSystem?.Name}");
        }
        GD.Print($"  Colonies: {_setupResult.Colonies.Count}");
        GD.Print($"  Stations: {_setupResult.Stations.Count}");
        GD.Print($"  Fleets: {_setupResult.Fleets.Count}");
        GD.Print($"  Ships: {_setupResult.Ships.Count}");

        // Remove dialog and start game
        _setupDialog.QueueFree();
        gm.CurrentState = GameState.Playing;
        gm.CurrentSpeed = GameSpeed.Normal;

        GD.Print("[MainScene] Game started!");
    }
}
