using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Systems;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Main galaxy map scene root. Orchestrates galaxy generation and
/// spawns all visual elements (stars, lanes, system nodes).
/// </summary>
public partial class GalaxyMap : Node3D
{
    private StarRenderer _starRenderer = null!;
    private LaneRenderer _laneRenderer = null!;
    private Node3D _systemNodes = null!;

    public override void _Ready()
    {
        GD.Print("[GalaxyMap] Generating galaxy...");

        // Create child containers
        _starRenderer = new StarRenderer { Name = "StarRenderer" };
        AddChild(_starRenderer);

        _laneRenderer = new LaneRenderer { Name = "LaneRenderer" };
        AddChild(_laneRenderer);

        _systemNodes = new Node3D { Name = "SystemNodes" };
        AddChild(_systemNodes);

        // Generate galaxy
        var config = new GalaxyGenerationConfig
        {
            Seed = GameManager.Instance?.MasterSeed ?? 42,
            TotalSystems = 100,
            ArmCount = 4,
            GalaxyRadius = 200f,
            MaxLaneLength = 60f,
            MinNeighbors = 2,
            MaxNeighbors = 4,
            HiddenLaneRatio = 0.15f
        };

        var galaxy = GalaxyGenerator.Generate(config);

        if (GameManager.Instance != null)
            GameManager.Instance.Galaxy = galaxy;

        GD.Print($"[GalaxyMap] Generated {galaxy.Systems.Count} systems, {galaxy.Lanes.Count} lanes");

        // Render
        _starRenderer.BuildFromGalaxy(galaxy);
        _laneRenderer.BuildFromGalaxy(galaxy);

        // Create per-system click areas
        foreach (var system in galaxy.Systems)
        {
            var node = new StarSystemNode();
            _systemNodes.AddChild(node);
            node.Initialize(system);
        }

        // Add environment
        SetupEnvironment();

        GD.Print("[GalaxyMap] Ready");
    }

    private void SetupEnvironment()
    {
        // World environment for space backdrop
        var env = new Godot.Environment();
        env.BackgroundMode = Godot.Environment.BGMode.Color;
        env.BackgroundColor = new Color(0.02f, 0.02f, 0.05f);
        env.AmbientLightSource = Godot.Environment.AmbientSource.Color;
        env.AmbientLightColor = new Color(0.15f, 0.15f, 0.2f);
        env.AmbientLightEnergy = 0.5f;

        // Glow for star emission
        env.GlowEnabled = true;
        env.GlowIntensity = 0.8f;
        env.GlowBloom = 0.3f;

        var worldEnv = new WorldEnvironment();
        worldEnv.Environment = env;
        AddChild(worldEnv);

        // Directional light for depth
        var light = new DirectionalLight3D();
        light.RotationDegrees = new Vector3(-45, -30, 0);
        light.LightEnergy = 0.3f;
        light.LightColor = new Color(0.8f, 0.85f, 1.0f);
        light.ShadowEnabled = false;
        AddChild(light);
    }
}
