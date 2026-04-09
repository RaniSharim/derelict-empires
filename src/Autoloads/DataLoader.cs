using System.Collections.Generic;
using Godot;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Autoloads;

/// <summary>
/// Loads and provides access to static game data definitions.
/// Registered as an autoload — access via DataLoader.Instance.
/// </summary>
public partial class DataLoader : Node
{
    public static DataLoader Instance { get; private set; } = null!;

    private Dictionary<string, ResourceDefinition> _resources = new();
    private Dictionary<string, ComponentDefinition> _components = new();

    public override void _Ready()
    {
        Instance = this;
        LoadResourceDefinitions();
        LoadComponentDefinitions();
        GD.Print($"[DataLoader] Ready — {_resources.Count} resources, {_components.Count} components loaded");
    }

    private void LoadResourceDefinitions()
    {
        foreach (var def in ResourceDefinition.All)
            _resources[def.Id] = def;
    }

    private void LoadComponentDefinitions()
    {
        foreach (var def in ComponentDefinition.All)
            _components[def.Id] = def;
    }

    public ResourceDefinition? GetResource(string id) =>
        _resources.GetValueOrDefault(id);

    public ResourceDefinition? GetResource(PrecursorColor color, ResourceType type) =>
        ResourceDefinition.Find(color, type);

    public ComponentDefinition? GetComponent(string id) =>
        _components.GetValueOrDefault(id);

    public ComponentDefinition? GetComponent(PrecursorColor color, ComponentTier tier) =>
        ComponentDefinition.Find(color, tier);

    public IReadOnlyCollection<ResourceDefinition> AllResources => _resources.Values;
    public IReadOnlyCollection<ComponentDefinition> AllComponents => _components.Values;
}
