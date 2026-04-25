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

    public override void _Ready()
    {
        Instance = this;
        LoadResourceDefinitions();
        McpLog.Info($"[DataLoader] Ready — {_resources.Count} resources loaded");
    }

    private void LoadResourceDefinitions()
    {
        foreach (var def in ResourceDefinition.All)
            _resources[def.Id] = def;
    }

    public ResourceDefinition? GetResource(string id) =>
        _resources.GetValueOrDefault(id);

    public ResourceDefinition? GetResource(PrecursorColor color, ResourceType type) =>
        ResourceDefinition.Find(color, type);

    public IReadOnlyCollection<ResourceDefinition> AllResources => _resources.Values;
}
