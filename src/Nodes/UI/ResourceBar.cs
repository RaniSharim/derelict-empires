using Godot;
using System.Collections.Generic;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Compact HUD bar showing the player's key resource stockpiles.
/// Shows top resources with per-tick income indicators.
/// </summary>
public partial class ResourceBar : HBoxContainer
{
    private readonly Dictionary<string, Label> _labels = new();
    private readonly Dictionary<string, float> _incomeCache = new();

    private static readonly Color RedColor = new(1f, 0.4f, 0.3f);
    private static readonly Color BlueColor = new(0.4f, 0.6f, 1f);
    private static readonly Color GreenColor = new(0.3f, 0.9f, 0.4f);
    private static readonly Color GoldColor = new(1f, 0.9f, 0.3f);
    private static readonly Color PurpleColor = new(0.7f, 0.4f, 0.9f);

    private static readonly (PrecursorColor color, ResourceType type, string abbrev)[] DisplayedResources =
    {
        (PrecursorColor.Red, ResourceType.SimpleEnergy, "R:E"),
        (PrecursorColor.Red, ResourceType.BasicComponent, "R:C"),
        (PrecursorColor.Blue, ResourceType.SimpleEnergy, "B:E"),
        (PrecursorColor.Blue, ResourceType.BasicComponent, "B:C"),
        (PrecursorColor.Green, ResourceType.SimpleEnergy, "G:E"),
        (PrecursorColor.Green, ResourceType.BasicComponent, "G:C"),
        (PrecursorColor.Gold, ResourceType.SimpleEnergy, "Au:E"),
        (PrecursorColor.Gold, ResourceType.BasicComponent, "Au:C"),
        (PrecursorColor.Purple, ResourceType.SimpleEnergy, "Pu:E"),
        (PrecursorColor.Purple, ResourceType.BasicComponent, "Pu:C"),
    };

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);

        foreach (var (color, type, abbrev) in DisplayedResources)
        {
            var key = EmpireData.ResourceKey(color, type);
            var label = new Label();
            label.AddThemeFontSizeOverride("font_size", 11);
            label.Text = $"{abbrev}: 0";
            label.Modulate = GetColorForPrecursor(color);
            AddChild(label);
            _labels[key] = label;
        }
    }

    public override void _Process(double delta)
    {
        var empire = GameManager.Instance?.LocalPlayerEmpire;
        if (empire == null) return;

        foreach (var (color, type, abbrev) in DisplayedResources)
        {
            var key = EmpireData.ResourceKey(color, type);
            float amount = empire.GetResource(color, type);
            float income = _incomeCache.GetValueOrDefault(key);

            string incStr = income > 0.01f ? $" +{income:F1}" : "";
            if (_labels.TryGetValue(key, out var label))
                label.Text = $"{abbrev}: {amount:F0}{incStr}";
        }
    }

    /// <summary>Update the income cache from the extraction system.</summary>
    public void UpdateIncome(Dictionary<string, float> income)
    {
        _incomeCache.Clear();
        foreach (var kv in income)
            _incomeCache[kv.Key] = kv.Value;
    }

    private static Color GetColorForPrecursor(PrecursorColor color) => color switch
    {
        PrecursorColor.Red => RedColor,
        PrecursorColor.Blue => BlueColor,
        PrecursorColor.Green => GreenColor,
        PrecursorColor.Gold => GoldColor,
        PrecursorColor.Purple => PurpleColor,
        _ => Colors.White
    };
}
