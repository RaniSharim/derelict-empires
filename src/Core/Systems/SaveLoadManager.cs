using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Core.Systems;

/// <summary>
/// Serializes and deserializes GameSaveData to/from JSON.
/// Pure C# — no Godot dependency.
/// </summary>
public static class SaveLoadManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions CompactJsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Serialize game state to JSON string.</summary>
    public static string ToJson(GameSaveData data, bool compact = false)
    {
        return JsonSerializer.Serialize(data, compact ? CompactJsonOptions : JsonOptions);
    }

    /// <summary>Deserialize game state from JSON string.</summary>
    public static GameSaveData FromJson(string json)
    {
        var data = JsonSerializer.Deserialize<GameSaveData>(json, JsonOptions);
        if (data == null)
            throw new InvalidOperationException("Failed to deserialize save data: result was null");
        return data;
    }

    /// <summary>Save game state to a file.</summary>
    public static void SaveToFile(GameSaveData data, string path, bool compact = false)
    {
        var json = ToJson(data, compact);
        File.WriteAllText(path, json);
    }

    /// <summary>Load game state from a file.</summary>
    public static GameSaveData LoadFromFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Save file not found: {path}");
        var json = File.ReadAllText(path);
        return FromJson(json);
    }
}
