using System;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Systems;
using DerlictEmpires.Nodes.Map;

/// <summary>
/// Project-specific partial extension of McpBridge.
/// Registers commands that depend on Derelict Empires types (MainScene,
/// GameSaveData, EventBus, etc.) so the upstream McpBridge.cs template
/// can be copied in without being edited.
/// </summary>
public partial class McpBridge
{
    partial void InitializeProject()
    {
        _projectCommandHandler = DispatchProjectCommand;
    }

    private Task<string> DispatchProjectCommand(string cmd, JsonElement root)
    {
        string response = cmd switch
        {
            "load_state" => HandleLoadState(root),
            "save_state" => HandleSaveState(root),
            "tick"       => HandleTick(root),
            _            => null,
        };
        return Task.FromResult(response);
    }

    private MainScene FindMainScene()
    {
        return FindNodeOfType<MainScene>(GetTree().Root);
    }

    private static T FindNodeOfType<T>(Node node) where T : Node
    {
        if (node is T t) return t;
        foreach (var child in node.GetChildren())
        {
            var found = FindNodeOfType<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    // === load_state ====================================================

    private string HandleLoadState(JsonElement root)
    {
        try
        {
            string path = null;
            string json = null;

            if (root.TryGetProperty("path", out var pathEl))
                path = pathEl.GetString();
            if (root.TryGetProperty("json", out var jsonEl))
                json = jsonEl.GetRawText();

            GameSaveData saveData;
            if (!string.IsNullOrEmpty(json))
            {
                saveData = SaveLoadManager.FromJson(json);
            }
            else if (!string.IsNullOrEmpty(path))
            {
                saveData = SaveLoadManager.LoadFromFile(path);
            }
            else
            {
                return JsonErr("load_state requires 'path' (file path) or 'json' (inline JSON)");
            }

            var mainScene = FindMainScene();
            if (mainScene == null)
                return JsonErr("MainScene not found in scene tree");

            mainScene.LoadGame(saveData);
            return JsonOk(new
            {
                loaded = true,
                empires = saveData.Empires.Count,
                fleets = saveData.Fleets.Count,
                systems = saveData.Galaxy.Systems.Count
            });
        }
        catch (Exception ex)
        {
            return JsonErr($"load_state error: {ex.Message}");
        }
    }

    // === save_state ====================================================

    private string HandleSaveState(JsonElement root)
    {
        try
        {
            string path = null;
            if (root.TryGetProperty("path", out var pathEl))
                path = pathEl.GetString();

            var mainScene = FindMainScene();
            if (mainScene == null)
                return JsonErr("MainScene not found in scene tree");

            var saveData = mainScene.BuildGameSaveData();

            if (!string.IsNullOrEmpty(path))
            {
                SaveLoadManager.SaveToFile(saveData, path);
                return JsonOk(new { saved = true, path });
            }
            else
            {
                var json = SaveLoadManager.ToJson(saveData, compact: true);
                return JsonOk(new { saved = true, json });
            }
        }
        catch (Exception ex)
        {
            return JsonErr($"save_state error: {ex.Message}");
        }
    }

    // === tick ==========================================================

    private string HandleTick(JsonElement root)
    {
        try
        {
            int fast = 0;
            int slow = 0;
            if (root.TryGetProperty("fast", out var fastEl))
                fast = fastEl.GetInt32();
            if (root.TryGetProperty("slow", out var slowEl))
                slow = slowEl.GetInt32();

            if (fast <= 0 && slow <= 0)
                return JsonErr("tick requires 'fast' and/or 'slow' > 0");

            var eb = EventBus.Instance;
            var gm = GameManager.Instance;
            if (eb == null || gm == null)
                return JsonErr("EventBus or GameManager not initialized");

            for (int i = 0; i < fast; i++)
            {
                gm.GameTime += TurnManager.FastTickInterval;
                eb.FireFastTick(TurnManager.FastTickInterval);
            }

            for (int i = 0; i < slow; i++)
            {
                gm.GameTime += TurnManager.SlowTickInterval;
                eb.FireSlowTick(TurnManager.SlowTickInterval);
            }

            return JsonOk(new
            {
                fastFired = fast,
                slowFired = slow,
                gameTime = gm.GameTime
            });
        }
        catch (Exception ex)
        {
            return JsonErr($"tick error: {ex.Message}");
        }
    }
}
