using Godot;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Exploration;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// Right-panel variant for a salvage / derelict site, layered model. Mirrors the
/// stacked-layer rendering used in <see cref="DerlictEmpires.Nodes.UI.RightPanel"/> with
/// a denser System View layout. Read-only here — actions live on the galaxy-map RightPanel.
/// </summary>
public partial class SalvageEntityPanel : EntityPanelBase
{
    public void Populate(SalvageSiteData site)
    {
        foreach (var c in GetChildren()) c.QueueFree();
        AddThemeConstantOverride("separation", 6);

        if (site == null) return;

        // Aggregate danger across remaining (non-terminal) layers, scaled to ~20× signature
        // for the entity-header bar. Empty layer list → 0.
        float dangerSum = 0f;
        foreach (var l in site.Layers) dangerSum += l.DangerChance * l.DangerSeverity;
        int sig = (int)(dangerSum / System.Math.Max(1, site.Layers.Count) * 0.5f);
        var primary = ColorForPrecursor(site.Color);
        AddEntityHeader(primary, site.Name.Length > 0 ? site.Name : $"Salvage #{site.Id}", sig);

        string colors = string.Join("+", site.Colors);
        var status = new Label { Text = $"{colors} · T{site.Tier} · {site.Layers.Count} layer(s)" };
        UIFonts.Style(status, UIFonts.Main, UIFonts.SmallSize, UIColors.TextLabel);
        AddChild(status);

        var player = GameManager.Instance?.LocalPlayerEmpire;
        var progress = player == null ? null : GameManager.Instance?.GetSalvageProgress(player.Id, site.POIId);
        progress ??= SalvageSiteProgress.ForSite(player?.Id ?? -1, site.POIId, site.Layers.Count);

        if (site.Layers.Count == 0)
        {
            AddBody("no layer data");
            return;
        }

        AddSection("LAYERS");
        var siteActivity = player == null
            ? SiteActivity.None
            : (GameManager.Instance?.GetSiteActivity(player.Id, site.POIId) ?? SiteActivity.None);
        for (int i = 0; i < site.Layers.Count; i++)
            AddLayerRow(site, progress, i, siteActivity);

        if (!string.IsNullOrEmpty(site.SpecialOutcomeId))
        {
            AddSection("OUTCOME");
            string label;
            Color tone;
            if (progress.SpecialOutcomeConsumed) { label = $"✓ {HumanizeOutcomeId(site.SpecialOutcomeId)} consumed"; tone = UIColors.TextDim; }
            else if (progress.SpecialOutcomeAvailable) { label = $"⚒ READY: {HumanizeOutcomeId(site.SpecialOutcomeId)}"; tone = primary; }
            else { label = $"{HumanizeOutcomeId(site.SpecialOutcomeId)} (locked)"; tone = UIColors.TextFaint; }
            var l = new Label { Text = label };
            UIFonts.Style(l, UIFonts.Main, UIFonts.SmallSize, tone);
            AddChild(l);
        }
    }

    private void AddLayerRow(SalvageSiteData site, SalvageSiteProgress progress, int idx, SiteActivity siteActivity)
    {
        var layer = site.Layers[idx];
        bool isActive = idx == progress.ActiveLayerIndex;
        bool past = idx < progress.ActiveLayerIndex;
        bool future = idx > progress.ActiveLayerIndex;

        string state;
        Color tone;
        if (past && progress.LayerScavenged[idx]) { state = "SCAVENGED"; tone = ColorForPrecursor(layer.LayerColor); }
        else if (past && progress.LayerSkipped[idx]) { state = "SKIPPED"; tone = UIColors.TextDim; }
        else if (future) { state = "LOCKED"; tone = UIColors.TextFaint; }
        else if (siteActivity == SiteActivity.Scanning) { state = "SCANNING"; tone = ColorForPrecursor(layer.LayerColor); }
        else if (siteActivity == SiteActivity.Extracting) { state = "SCAVENGING"; tone = ColorForPrecursor(layer.LayerColor); }
        else if (progress.LayerScanned[idx]) { state = "SCANNED"; tone = ColorForPrecursor(layer.LayerColor); }
        else { state = "ACTIVE"; tone = ColorForPrecursor(layer.LayerColor); }

        // Header row: chevron + "L1/3 · Color" + state badge
        var head = new HBoxContainer();
        head.AddThemeConstantOverride("separation", 6);
        AddChild(head);

        string chevron = isActive ? "▼" : "  ";
        var title = new Label { Text = $"{chevron} L{idx + 1}/{site.Layers.Count} · {layer.LayerColor.ToString().ToUpper()}" };
        UIFonts.Style(title, UIFonts.Main, UIFonts.SmallSize,
            future ? UIColors.TextFaint : (isActive ? UIColors.TextBright : UIColors.TextBody));
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        head.AddChild(title);

        var badge = new Label { Text = state };
        UIFonts.Style(badge, UIFonts.Main, UIFonts.SmallSize, tone);
        head.AddChild(badge);

        // Body: yield + per-resource bar (shrunk version of RightPanel.BuildLayerYieldBars)
        if (layer.Yield.Count > 0)
        {
            foreach (var kv in layer.Yield)
            {
                float total = kv.Value;
                float remaining = layer.RemainingYield.GetValueOrDefault(kv.Key, total);
                float ratio = total > 0.01f ? Mathf.Clamp(remaining / total, 0f, 1f) : 0f;
                AddYieldBar(kv.Key, remaining, total, ratio);
            }
        }

        // Pips: research/danger preview pre-scan; results post-scan
        if (progress.LayerScanned[idx])
        {
            var pipRow = new HBoxContainer();
            pipRow.AddThemeConstantOverride("separation", 10);
            AddChild(pipRow);
            string text = progress.ResearchUnlocked[idx]
                ? $"✓ {progress.ResearchSubsystemId[idx] ?? "subsystem"}"
                : "Research ✗";
            var resPip = new Label { Text = text };
            UIFonts.Style(resPip, UIFonts.Main, UIFonts.SmallSize,
                progress.ResearchUnlocked[idx] ? UIColors.TextBright : UIColors.TextFaint);
            pipRow.AddChild(resPip);

            if (progress.DangerTriggered[idx])
            {
                var dangerPip = new Label { Text = "Danger ⚠" };
                UIFonts.Style(dangerPip, UIFonts.Main, UIFonts.SmallSize, UIColors.AccentRed);
                pipRow.AddChild(dangerPip);
            }
        }
        else if (!past && (layer.ResearchUnlockChance > 0f || layer.DangerChance > 0f))
        {
            var pipRow = new HBoxContainer();
            pipRow.AddThemeConstantOverride("separation", 10);
            AddChild(pipRow);
            if (layer.ResearchUnlockChance > 0f)
            {
                var pip = new Label { Text = $"Research {(int)(layer.ResearchUnlockChance * 100)}% ?" };
                UIFonts.Style(pip, UIFonts.Main, UIFonts.SmallSize,
                    future ? UIColors.TextFaint : UIColors.TextDim);
                pipRow.AddChild(pip);
            }
            if (layer.DangerChance > 0f)
            {
                var pip = new Label { Text = $"Danger {(int)(layer.DangerChance * 100)}% {layer.DangerTypeId}" };
                UIFonts.Style(pip, UIFonts.Main, UIFonts.SmallSize,
                    future ? UIColors.TextFaint : new Color("#e8883a"));
                pipRow.AddChild(pip);
            }
        }
    }

    private void AddYieldBar(string resourceKey, float remaining, float total, float ratio)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        AddChild(row);

        var label = new Label { Text = resourceKey };
        UIFonts.Style(label, UIFonts.Main, UIFonts.SmallSize, UIColors.TextBody);
        label.CustomMinimumSize = new Vector2(140, 0);
        row.AddChild(label);

        var track = new PanelContainer();
        track.CustomMinimumSize = new Vector2(0, 6);
        track.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var trackBg = new StyleBoxFlat { BgColor = new Color(0.1f, 0.1f, 0.15f, 0.6f) };
        track.AddThemeStyleboxOverride("panel", trackBg);
        row.AddChild(track);

        var fill = new ColorRect { Color = UIColors.SensorIcon };
        fill.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        fill.AnchorRight = ratio;
        fill.MouseFilter = Control.MouseFilterEnum.Ignore;
        track.AddChild(fill);

        var amount = new Label { Text = $"{remaining:F0}/{total:F0}" };
        UIFonts.Style(amount, UIFonts.Main, UIFonts.SmallSize, UIColors.TextDim);
        amount.CustomMinimumSize = new Vector2(56, 0);
        amount.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(amount);
    }

    private static string HumanizeOutcomeId(string? id) => id switch
    {
        null => "",
        "repair_station" => "Repair Station",
        "recover_derelict" => "Recover Derelict",
        _ => id.Replace('_', ' '),
    };

    private static Color ColorForPrecursor(PrecursorColor c) => c switch
    {
        PrecursorColor.Red    => new Color("#e85545"),
        PrecursorColor.Blue   => new Color("#44aaff"),
        PrecursorColor.Green  => new Color("#4caf50"),
        PrecursorColor.Gold   => new Color("#ddaa22"),
        PrecursorColor.Purple => new Color("#b366e8"),
        _                     => UIColors.TextBody,
    };
}
