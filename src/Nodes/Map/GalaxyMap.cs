using Godot;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Galaxy map scene root. Renders stars, lanes, and per-system click areas.
/// Does NOT generate the galaxy — call LoadGalaxy() with pre-built data.
/// </summary>
public partial class GalaxyMap : Node3D
{
    private StarRenderer _starRenderer = null!;
    private LaneRenderer _laneRenderer = null!;
    private Node3D _systemNodes = null!;

    public override void _Ready()
    {
        // Create child containers
        _starRenderer = new StarRenderer { Name = "StarRenderer" };
        AddChild(_starRenderer);

        _laneRenderer = new LaneRenderer { Name = "LaneRenderer" };
        AddChild(_laneRenderer);

        _systemNodes = new Node3D { Name = "SystemNodes" };
        AddChild(_systemNodes);

        SetupEnvironment();
    }

    /// <summary>
    /// Render a galaxy from pre-built data. Can be called after _Ready.
    /// </summary>
    public void LoadGalaxy(GalaxyData galaxy)
    {
        // Clear existing system nodes (for reloads)
        foreach (var child in _systemNodes.GetChildren())
            child.QueueFree();

        _starRenderer.BuildFromGalaxy(galaxy);
        _laneRenderer.BuildFromGalaxy(galaxy);

        foreach (var system in galaxy.Systems)
        {
            var node = new StarSystemNode();
            _systemNodes.AddChild(node);
            node.Initialize(system);
        }

        McpLog.Info($"[GalaxyMap] Loaded {galaxy.Systems.Count} systems, {galaxy.Lanes.Count} lanes");
    }

    private void SetupEnvironment()
    {
        var env = new Godot.Environment();
        env.BackgroundMode = Godot.Environment.BGMode.Color;
        env.BackgroundColor = new Color(0.02f, 0.02f, 0.05f);
        env.AmbientLightSource = Godot.Environment.AmbientSource.Color;
        env.AmbientLightColor = new Color(0.15f, 0.15f, 0.2f);
        env.AmbientLightEnergy = 0.5f;

        // Environment bloom supplements the per-star shader glow.
        // HDR threshold ensures only the brightest star cores bloom,
        // creating a soft secondary halo around owned/core stars.
        env.GlowEnabled = true;
        env.GlowIntensity = 0.6f;
        env.GlowBloom = 0.1f;
        env.GlowHdrThreshold = 1.0f;
        env.GlowHdrScale = 2.0f;
        env.GlowBlendMode = Godot.Environment.GlowBlendModeEnum.Additive;

        var worldEnv = new WorldEnvironment();
        worldEnv.Environment = env;
        AddChild(worldEnv);

        var light = new DirectionalLight3D();
        light.RotationDegrees = new Vector3(-45, -30, 0);
        light.LightEnergy = 0.3f;
        light.LightColor = new Color(0.8f, 0.85f, 1.0f);
        light.ShadowEnabled = false;
        AddChild(light);
    }
}
