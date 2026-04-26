using System;
using System.Collections.Generic;
using Godot;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Autoloads;

/// <summary>
/// Loads and provides access to static game data definitions.
/// Registered as an autoload — access via DataLoader.Instance.
/// </summary>
public partial class DataLoader : Node
{
    public static DataLoader Instance { get; private set; } = null!;

    private const string SalvageTypesPath   = "res://resources/data/salvage_types.json";
    private const string SalvageDangersPath = "res://resources/data/salvage_dangers.json";
    private const string SalvageOutcomesPath = "res://resources/data/salvage_outcomes.json";

    private Dictionary<string, ResourceDefinition> _resources = new();

    /// <summary>
    /// Authored salvage catalog (site types, dangers, outcomes). Null only if the JSON
    /// files failed to load — callers must handle that explicitly during boot.
    /// </summary>
    public SalvageRegistry? Salvage { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        LoadResourceDefinitions();
        LoadSalvageRegistry();
        McpLog.Info($"[DataLoader] Ready — {_resources.Count} resources, " +
                    $"{Salvage?.Types.Count ?? 0} salvage types, " +
                    $"{Salvage?.Dangers.Count ?? 0} dangers, " +
                    $"{Salvage?.Outcomes.Count ?? 0} outcomes");
    }

    private void LoadResourceDefinitions()
    {
        foreach (var def in ResourceDefinition.All)
            _resources[def.Id] = def;
    }

    private void LoadSalvageRegistry()
    {
        try
        {
            string types = ReadProjectFile(SalvageTypesPath);
            string dangers = ReadProjectFile(SalvageDangersPath);
            string outcomes = ReadProjectFile(SalvageOutcomesPath);
            Salvage = SalvageRegistry.Load(types, dangers, outcomes);
        }
        catch (Exception ex)
        {
            McpLog.Error($"[DataLoader] Salvage registry load failed: {ex.Message}");
            Salvage = null;
        }
    }

    private static string ReadProjectFile(string resPath)
    {
        using var f = FileAccess.Open(resPath, FileAccess.ModeFlags.Read);
        if (f == null)
            throw new InvalidOperationException(
                $"could not open {resPath}: {FileAccess.GetOpenError()}");
        return f.GetAsText();
    }

    public ResourceDefinition? GetResource(string id) =>
        _resources.GetValueOrDefault(id);

    public ResourceDefinition? GetResource(PrecursorColor color, ResourceType type) =>
        ResourceDefinition.Find(color, type);

    public IReadOnlyCollection<ResourceDefinition> AllResources => _resources.Values;
}
