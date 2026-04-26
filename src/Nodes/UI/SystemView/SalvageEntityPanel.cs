using Godot;
using DerlictEmpires.Core.Exploration;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// Right-panel variant for a salvage / derelict site. See design/in_system_design.md §9.3.
/// Remaining yield bars, extraction state, intel, actions. Yield bars use a raw ColorRect ratio
/// (same pattern as ResearchTrackRow).
/// </summary>
public partial class SalvageEntityPanel : EntityPanelBase
{
    public void Populate(SalvageSiteData site)
    {
        foreach (var c in GetChildren()) c.QueueFree();
        AddThemeConstantOverride("separation", 8);

        if (site == null) return;

        // Aggregate danger across remaining (non-terminal) layers, scaled to ~20× signature
        // for the entity-header bar. Empty layer list → 0.
        float dangerSum = 0f;
        foreach (var l in site.Layers) dangerSum += l.DangerChance * l.DangerSeverity;
        int sig = (int)(dangerSum / System.Math.Max(1, site.Layers.Count) * 0.5f);
        AddEntityHeader(new Color("#ff5540"), site.Name.Length > 0 ? site.Name : $"Salvage #{site.Id}", sig);

        string colors = string.Join("+", site.Colors);
        var status = new Label { Text = $"{colors} · T{site.Tier} · {site.Layers.Count} layer(s)" };
        UIFonts.Style(status, UIFonts.Main, UIFonts.SmallSize, UIColors.TextLabel);
        AddChild(status);

        AddSection("REMAINING YIELD");
        var totals = site.TotalYield;
        var remaining = site.RemainingYield;
        if (remaining.Count == 0)
        {
            AddBody("depleted");
        }
        else
        {
            foreach (var kv in remaining)
            {
                float total = totals.TryGetValue(kv.Key, out var t) ? t : kv.Value;
                float ratio = total > 0.01f ? Mathf.Clamp(kv.Value / total, 0f, 1f) : 0f;
                AddYieldRow(kv.Key, kv.Value, ratio);
            }
        }

        AddSection("INTEL");
        string outcome = site.SpecialOutcomeId ?? "none";
        AddBody($"type · {site.TypeId}  |  layers {site.Layers.Count}  |  outcome {outcome}");

        AddActionsRow(new[] { "SEND FLEET ▸", "CLAIM", "BUILD OUTPOST" }, UIColors.TextDim);
    }

    private void AddYieldRow(string resourceKey, float remaining, float ratio)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        AddChild(row);

        var label = new Label { Text = resourceKey };
        UIFonts.Style(label, UIFonts.Main, UIFonts.SmallSize, UIColors.TextBody);
        label.CustomMinimumSize = new Vector2(140, 0);
        row.AddChild(label);

        var track = new PanelContainer();
        track.CustomMinimumSize = new Vector2(0, 8);
        track.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var trackBg = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.15f, 0.6f) };
        track.AddThemeStyleboxOverride("panel", trackBg);
        row.AddChild(track);

        var fill = new ColorRect { Color = UIColors.SensorIcon };
        fill.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        fill.AnchorRight = ratio;
        fill.MouseFilter = Control.MouseFilterEnum.Ignore;
        track.AddChild(fill);

        var amount = new Label { Text = $"{remaining:F0}" };
        UIFonts.Style(amount, UIFonts.Main, UIFonts.SmallSize, UIColors.TextDim);
        amount.CustomMinimumSize = new Vector2(40, 0);
        amount.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(amount);
    }
}
