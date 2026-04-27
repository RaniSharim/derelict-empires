using Godot;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Services;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Right-side selection panel. Layout shell in <c>scenes/ui/right_panel.tscn</c>;
/// POI card construction stays code-built (sub-scene extraction is a follow-up).
/// Reads via <see cref="IGameQuery"/>; writes via EventBus intent events.
/// </summary>
public partial class RightPanel : Control
{
    public const int PanelWidth = 306;

    [Export] private PanelContainer _background = null!;
    [Export] private MarginContainer _hostileFleetSection = null!;
    [Export] private Label _hostileFleetTitle = null!;
    [Export] private Label _hostileFleetInfo = null!;
    [Export] private Button _attackButton = null!;
    [Export] private Label _systemInfo = null!;
    [Export] private Label _systemName = null!;
    [Export] private VBoxContainer _poiList = null!;

    private StarSystemData? _selectedSystem;
    private int _hostileSelectedFleetId = -1;

    private static IGameQuery Query => GameManager.Instance!;

    // Per-(POI, layer) widget refs — updated in place on progress events so the full
    // panel doesn't rebuild 10x/sec during active scans. Cleared on every full rebuild.
    private readonly Dictionary<(int poi, int layer), Label> _layerScanLabels = new();
    private readonly Dictionary<(int poi, int layer), ProgressBar> _layerScanBars = new();
    private readonly Dictionary<(int poi, int layer), Dictionary<string, (Label amount, ProgressBar bar)>> _layerYieldWidgets = new();

    public override void _Ready()
    {
        GlassPanel.Apply(_background, enableBlur: true);

        UIFonts.Style(_systemName,         UIFonts.Title, UIFonts.TitleSize, UIColors.TextBright);
        UIFonts.Style(_systemInfo,         UIFonts.Main,  UIFonts.SmallSize, UIColors.TextDim);
        UIFonts.Style(_hostileFleetTitle,  UIFonts.Title, UIFonts.TitleSize, UIColors.TextBright);
        UIFonts.Style(_hostileFleetInfo,   UIFonts.Main,  UIFonts.SmallSize, UIColors.TextDim);
        UIFonts.Style(GetNode<Label>("Layout/HostileFleet/VBox/Tag"),
                                            UIFonts.Main, UIFonts.SmallSize, UIColors.AccentRed);
        UIFonts.StyleButton(_attackButton, UIFonts.Main,  UIFonts.SmallSize, UIColors.TextBright);

        _attackButton.Pressed += OnAttackPressed;

        if (EventBus.Instance != null)
        {
            EventBus.Instance.SystemSelected             += OnSystemSelected;
            EventBus.Instance.SystemDeselected           += OnSystemDeselected;
            EventBus.Instance.FleetSelected              += OnFleetSelectedForPanel;
            EventBus.Instance.FleetDeselected            += OnFleetDeselectedForPanel;
            EventBus.Instance.SiteDiscovered             += OnSiteDiscovered;
            EventBus.Instance.SiteLayerScanProgressChanged += OnLayerScanProgressChanged;
            EventBus.Instance.SiteScanComplete           += OnSiteScanComplete;
            EventBus.Instance.YieldExtracted             += OnYieldExtracted;
            EventBus.Instance.FleetArrivedAtSystem       += OnFleetArrivedAtSystem;
            EventBus.Instance.SiteActivityChanged        += OnSiteActivityChanged;
            EventBus.Instance.SiteActivityRateChanged    += OnSiteActivityRateChanged;
            EventBus.Instance.SiteLayerScanned           += OnLayerScanned;
            EventBus.Instance.SiteLayerScavenged         += OnLayerScavenged;
            EventBus.Instance.SiteLayerSkipped           += OnLayerSkipped;
            EventBus.Instance.SiteSpecialOutcomeReady    += OnSpecialOutcomeReady;
            EventBus.Instance.SiteSpecialOutcomeResolved += OnSpecialOutcomeResolved;
        }
    }

    public override void _ExitTree()
    {
        if (_attackButton != null)
            _attackButton.Pressed -= OnAttackPressed;

        if (EventBus.Instance != null)
        {
            EventBus.Instance.SystemSelected             -= OnSystemSelected;
            EventBus.Instance.SystemDeselected           -= OnSystemDeselected;
            EventBus.Instance.SiteDiscovered             -= OnSiteDiscovered;
            EventBus.Instance.SiteLayerScanProgressChanged -= OnLayerScanProgressChanged;
            EventBus.Instance.SiteScanComplete           -= OnSiteScanComplete;
            EventBus.Instance.YieldExtracted             -= OnYieldExtracted;
            EventBus.Instance.FleetArrivedAtSystem       -= OnFleetArrivedAtSystem;
            EventBus.Instance.SiteActivityChanged        -= OnSiteActivityChanged;
            EventBus.Instance.SiteActivityRateChanged    -= OnSiteActivityRateChanged;
            EventBus.Instance.SiteLayerScanned           -= OnLayerScanned;
            EventBus.Instance.SiteLayerScavenged         -= OnLayerScavenged;
            EventBus.Instance.SiteLayerSkipped           -= OnLayerSkipped;
            EventBus.Instance.SiteSpecialOutcomeReady    -= OnSpecialOutcomeReady;
            EventBus.Instance.SiteSpecialOutcomeResolved -= OnSpecialOutcomeResolved;
            EventBus.Instance.FleetSelected              -= OnFleetSelectedForPanel;
            EventBus.Instance.FleetDeselected            -= OnFleetDeselectedForPanel;
        }
    }

    private void OnSiteDiscovered(int empireId, int poiId) => RefreshIfRelevant(poiId);

    /// <summary>Per-tick scan progress update for one layer. Updates the layer's bar in-place.</summary>
    private void OnLayerScanProgressChanged(int empireId, int poiId, int layerIndex, float progress, float difficulty)
    {
        var key = (poiId, layerIndex);
        if (!_layerScanBars.TryGetValue(key, out var bar)) return;
        float frac = difficulty > 0 ? Mathf.Clamp(progress / difficulty, 0f, 1f) : 0f;
        bar.Value = frac;
        if (_layerScanLabels.TryGetValue(key, out var label))
            label.Text = $"SCAN · {(int)(frac * 100f)}%";
    }

    private void OnSiteScanComplete(int empireId, int poiId) { /* legacy; layered card uses SiteLayerScanned */ }

    private void OnYieldExtracted(int empireId, int poiId, string key, float amount)
    {
        var progress = QueryProgress(poiId);
        if (progress == null) return;
        int idx = progress.ActiveLayerIndex;
        if (idx >= progress.LayerCount) return;
        var site = Query.GetSalvageSite(FindSiteIdForPoi(poiId) ?? -1);
        if (site == null || idx >= site.Layers.Count) return;

        if (!_layerYieldWidgets.TryGetValue((poiId, idx), out var byKey)) return;
        if (!byKey.TryGetValue(key, out var w)) return;

        var layer = site.Layers[idx];
        float total = layer.Yield.GetValueOrDefault(key);
        float remaining = layer.RemainingYield.GetValueOrDefault(key);
        w.amount.Text = $"{remaining:F0} / {total:F0}";
        w.bar.Value = total > 0 ? Mathf.Clamp(remaining / total, 0, 1) : 0;
    }

    private void OnLayerScanned(int empireId, int poiId, int layerIndex) => RefreshIfRelevant(poiId);
    private void OnLayerScavenged(int empireId, int poiId, int layerIndex) => RefreshIfRelevant(poiId);
    private void OnLayerSkipped(int empireId, int poiId, int layerIndex) => RefreshIfRelevant(poiId);
    private void OnSpecialOutcomeReady(int empireId, int poiId, string outcomeId) => RefreshIfRelevant(poiId);
    private void OnSpecialOutcomeResolved(int empireId, int poiId, SalvageOutcomeProcessor.Resolution res) => RefreshIfRelevant(poiId);

    private static SalvageSiteProgress? QueryProgress(int poiId)
    {
        var player = Query.PlayerEmpire;
        return player == null ? null : Query.GetSalvageProgress(player.Id, poiId);
    }

    private int? FindSiteIdForPoi(int poiId)
    {
        if (_selectedSystem == null) return null;
        foreach (var p in _selectedSystem.POIs)
            if (p.Id == poiId) return p.SalvageSiteId;
        return null;
    }

    private void OnSiteActivityChanged(int empireId, int poiId, SiteActivity activity) => RefreshIfRelevant(poiId);
    private void OnSiteActivityRateChanged(int empireId, int poiId) => RefreshIfRelevant(poiId);
    private void OnFleetArrivedAtSystem(int fleetId, int systemId)
    {
        if (_selectedSystem != null && _selectedSystem.Id == systemId)
            RebuildPOIList(_selectedSystem);
    }

    private void RefreshIfRelevant(int poiId)
    {
        if (_selectedSystem == null) return;
        if (_selectedSystem.POIs.Any(p => p.Id == poiId))
            RebuildPOIList(_selectedSystem);
    }

    private void OnFleetSelectedForPanel(int fleetId)
    {
        var fleet = Query.Fleets.FirstOrDefault(f => f.Id == fleetId);
        var playerId = Query.PlayerEmpire?.Id ?? -1;
        if (fleet == null || fleet.OwnerEmpireId == playerId)
        {
            _hostileSelectedFleetId = -1;
            _hostileFleetSection.Visible = false;
            return;
        }

        _hostileSelectedFleetId = fleet.Id;
        _hostileFleetTitle.Text = fleet.Name.ToUpperInvariant();
        int shipCount = fleet.ShipIds.Count;
        var sys = Query.Galaxy?.GetSystem(fleet.CurrentSystemId);
        _hostileFleetInfo.Text =
            $"{shipCount} ship{(shipCount == 1 ? "" : "s")} · {sys?.Name?.ToUpperInvariant() ?? "UNKNOWN"}";
        _hostileFleetSection.Visible = true;
        Visible = true;
    }

    private void OnFleetDeselectedForPanel()
    {
        _hostileSelectedFleetId = -1;
        _hostileFleetSection.Visible = false;
    }

    private void OnAttackPressed()
    {
        if (_hostileSelectedFleetId < 0) return;
        var hostile = Query.Fleets.FirstOrDefault(f => f.Id == _hostileSelectedFleetId);
        if (hostile == null) return;
        var player = Query.PlayerEmpire;
        if (player == null) return;

        var friendly = Query.Fleets.FirstOrDefault(f =>
            f.OwnerEmpireId == player.Id && f.CurrentSystemId == hostile.CurrentSystemId);
        if (friendly == null)
        {
            McpLog.Warn("[ATTACK] No friendly fleet at that system");
            return;
        }
        EventBus.Instance?.FireCombatStartRequested(friendly.Id, hostile.Id);
    }

    private void OnSystemSelected(StarSystemData system)
    {
        _selectedSystem = system;
        Visible = true;

        _systemName.Text = system.Name.ToUpper();
        string region = system.IsCore ? "Core" : $"Arm {system.ArmIndex}";
        string color = system.DominantColor?.ToString() ?? "Neutral";
        _systemInfo.Text = $"{region} · {color}";

        RebuildPOIList(system);
    }

    private void OnSystemDeselected()
    {
        _selectedSystem = null;
        Visible = false;
    }

    private void RebuildPOIList(StarSystemData system)
    {
        _layerScanLabels.Clear();
        _layerScanBars.Clear();
        _layerYieldWidgets.Clear();
        foreach (var child in _poiList.GetChildren())
            child.QueueFree();

        int playerId = Query.PlayerEmpire?.Id ?? -1;
        int shown = 0;
        foreach (var poi in system.POIs)
        {
            if (poi.SalvageSiteId.HasValue && playerId >= 0)
            {
                var state = Query.GetExplorationState(playerId, poi.Id);
                if (state == ExplorationState.Undiscovered) continue;
            }
            _poiList.AddChild(BuildPOICard(poi));
            shown++;
        }

        if (shown == 0)
        {
            var empty = new Label { Text = system.POIs.Count == 0 ? "No points of interest" : "UNEXPLORED" };
            UIFonts.Style(empty, UIFonts.Main, UIFonts.SmallSize, UIColors.TextFaint);
            empty.HorizontalAlignment = HorizontalAlignment.Center;
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_top", 12);
            margin.AddChild(empty);
            _poiList.AddChild(margin);
        }
    }

    private Control BuildPOICard(POIData poi)
    {
        if (poi.SalvageSiteId.HasValue)
            return BuildSalvageCard(poi, poi.SalvageSiteId.Value);

        var poiColor = GetPOIColor(poi.Type);
        var tintColor = GetPOITint(poi.Type);

        var card = new PanelContainer();
        var cardStyle = new StyleBoxFlat();
        var cardBase = new Color(14 / 255f, 20 / 255f, 40 / 255f, 0.95f);
        cardStyle.BgColor = new Color(
            cardBase.R + tintColor.R * tintColor.A,
            cardBase.G + tintColor.G * tintColor.A,
            cardBase.B + tintColor.B * tintColor.A,
            cardBase.A);
        cardStyle.SetBorderWidthAll(1);
        cardStyle.BorderColor = UIColors.BorderMid;
        cardStyle.BorderWidthLeft = 4;
        cardStyle.BorderColor = new Color(poiColor, 0.7f);
        cardStyle.ContentMarginLeft = 14;
        cardStyle.ContentMarginRight = 12;
        cardStyle.ContentMarginTop = 10;
        cardStyle.ContentMarginBottom = 10;
        cardStyle.SetCornerRadiusAll(4);
        card.AddThemeStyleboxOverride("panel", cardStyle);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        card.AddChild(vbox);

        var row1 = new HBoxContainer();
        row1.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(row1);

        var nameLabel = new Label { Text = poi.Name };
        UIFonts.Style(nameLabel, UIFonts.Title, UIFonts.TitleSize, poiColor);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        nameLabel.ClipText = true;
        row1.AddChild(nameLabel);

        var typeLabel = new Label { Text = GetPOITypeLabel(poi.Type) };
        UIFonts.Style(typeLabel, UIFonts.Main, UIFonts.SmallSize, UIColors.TextBody);
        row1.AddChild(typeLabel);

        if (poi.Type == POIType.HabitablePlanet)
        {
            var statsRow = new HBoxContainer();
            statsRow.AddThemeConstantOverride("separation", 16);
            vbox.AddChild(statsRow);
            int sizeVal = (int)poi.PlanetSize + 1;
            AddStatReadout(statsRow, "POP:",     $"{sizeVal * 0.7f:F1}B");
            AddStatReadout(statsRow, "INCOME:",  $"{sizeVal * 1.5f:F1}K/M");
            AddStatReadout(statsRow, "DEFENSE:", $"{sizeVal * 500}");
        }
        else if (poi.Type == POIType.AsteroidField)
        {
            var statsRow = new HBoxContainer();
            statsRow.AddThemeConstantOverride("separation", 16);
            vbox.AddChild(statsRow);
            int depositCount = poi.Deposits?.Count ?? 0;
            AddStatReadout(statsRow, "DEPOSITS:", $"{depositCount}");
        }
        else
        {
            var metaLabel = new Label { Text = GetPOIMeta(poi) };
            UIFonts.Style(metaLabel, UIFonts.Main, UIFonts.SmallSize, UIColors.TextBody);
            vbox.AddChild(metaLabel);
        }

        return card;
    }

    private Control BuildSalvageCard(POIData poi, int siteId)
    {
        var site = Query.GetSalvageSite(siteId);
        var player = Query.PlayerEmpire;
        if (site == null || player == null)
            return BuildFallbackCard(poi);

        var primaryColor = ColorForPrecursor(site.Color);

        var card = new PanelContainer();
        var cardStyle = new StyleBoxFlat
        {
            BgColor = new Color(14 / 255f, 20 / 255f, 40 / 255f, 0.95f),
        };
        cardStyle.SetBorderWidthAll(1);
        cardStyle.BorderColor = UIColors.BorderMid;
        cardStyle.BorderWidthLeft = 4;
        cardStyle.BorderColor = new Color(primaryColor, 0.8f);
        cardStyle.ContentMarginLeft = 14;
        cardStyle.ContentMarginRight = 12;
        cardStyle.ContentMarginTop = 10;
        cardStyle.ContentMarginBottom = 10;
        cardStyle.SetCornerRadiusAll(4);
        card.AddThemeStyleboxOverride("panel", cardStyle);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        card.AddChild(vbox);

        // Header row 1: name + type tag
        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(headerRow);

        var nameLabel = new Label { Text = poi.Name };
        UIFonts.Style(nameLabel, UIFonts.Title, UIFonts.TitleSize, primaryColor);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        nameLabel.ClipText = true;
        headerRow.AddChild(nameLabel);

        var tagLabel = new Label { Text = $"{SalvageTypeTag(site.TypeId)} · T{site.Tier}" };
        UIFonts.Style(tagLabel, UIFonts.Main, UIFonts.SmallSize, UIColors.TextDim);
        headerRow.AddChild(tagLabel);

        // Header row 2: color dots + visibility
        var subRow = new HBoxContainer();
        subRow.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(subRow);

        BuildColorDots(subRow, site.Colors);
        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        subRow.AddChild(spacer);

        if (site.Visibility > 0f)
        {
            var visLabel = new Label { Text = $"VIS {site.Visibility:F0}" };
            UIFonts.Style(visLabel, UIFonts.Main, UIFonts.SmallSize, UIColors.TextDim);
            subRow.AddChild(visLabel);
        }

        var progress = Query.GetSalvageProgress(player.Id, poi.Id);
        int layerCount = site.Layers.Count;
        if (layerCount == 0)
        {
            var fallback = new Label { Text = "NO LAYER DATA" };
            UIFonts.Style(fallback, UIFonts.Main, UIFonts.SmallSize, UIColors.TextFaint);
            vbox.AddChild(fallback);
            return card;
        }
        progress ??= SalvageSiteProgress.ForSite(player.Id, poi.Id, layerCount);

        var sep = new HSeparator();
        vbox.AddChild(sep);

        for (int i = 0; i < layerCount; i++)
            BuildLayerRow(vbox, site, progress, i, poi.Id);

        BuildSpecialOutcomeFooter(vbox, site, progress, poi.Id);

        return card;
    }

    private enum LayerVisualState { Locked, Active, Scanning, Scanned, Scavenging, Scavenged, Skipped }

    private static LayerVisualState ResolveLayerState(SalvageSiteProgress progress, int idx, SiteActivity siteActivity)
    {
        if (idx < progress.ActiveLayerIndex)
        {
            if (progress.LayerScavenged[idx]) return LayerVisualState.Scavenged;
            if (progress.LayerSkipped[idx]) return LayerVisualState.Skipped;
            return LayerVisualState.Scavenged;
        }
        if (idx > progress.ActiveLayerIndex) return LayerVisualState.Locked;
        if (siteActivity == SiteActivity.Scanning) return LayerVisualState.Scanning;
        if (siteActivity == SiteActivity.Extracting) return LayerVisualState.Scavenging;
        if (progress.LayerScanned[idx]) return LayerVisualState.Scanned;
        return LayerVisualState.Active;
    }

    private void BuildLayerRow(VBoxContainer parent, SalvageSiteData site, SalvageSiteProgress progress, int layerIndex, int poiId)
    {
        var layer = site.Layers[layerIndex];
        var layerColor = ColorForPrecursor(layer.LayerColor);
        var siteActivity = Query.GetSiteActivity(progress.EmpireId, poiId);
        var visual = ResolveLayerState(progress, layerIndex, siteActivity);
        bool isActive = layerIndex == progress.ActiveLayerIndex;
        bool isLocked = visual == LayerVisualState.Locked;

        var row = new VBoxContainer();
        row.AddThemeConstantOverride("separation", 3);
        parent.AddChild(row);

        var head = new HBoxContainer();
        head.AddThemeConstantOverride("separation", 6);
        row.AddChild(head);

        string chevron = isActive ? "▼" : "  ";
        Color titleColor = isLocked ? UIColors.TextFaint : (isActive ? UIColors.TextBright : UIColors.TextBody);

        var title = new Label
        {
            Text = $"{chevron} LAYER {layerIndex + 1}/{site.Layers.Count} · {layer.LayerColor.ToString().ToUpper()}",
        };
        UIFonts.Style(title, UIFonts.Main, UIFonts.SmallSize, titleColor);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        head.AddChild(title);

        BuildLayerStateBadge(head, visual, layerColor);

        switch (visual)
        {
            case LayerVisualState.Locked:
                BuildLayerPreviewPips(row, layer, dim: true);
                break;
            case LayerVisualState.Active:
                BuildLayerPreviewPips(row, layer, dim: false);
                BuildLayerActions_Active(row, poiId, layerColor);
                break;
            case LayerVisualState.Scanning:
                BuildScanProgressBar(row, site, progress, layerIndex, poiId);
                BuildLayerPreviewPips(row, layer, dim: false);
                BuildLayerActions_StopScanning(row, poiId);
                break;
            case LayerVisualState.Scanned:
                BuildLayerYieldPreview(row, layer);
                BuildLayerResultPips(row, progress, layerIndex);
                BuildLayerActions_Scanned(row, poiId, layerColor);
                break;
            case LayerVisualState.Scavenging:
                BuildLayerYieldBars(row, layer, layerIndex, poiId);
                BuildLayerResultPips(row, progress, layerIndex);
                BuildLayerActions_StopScavenging(row, poiId);
                break;
            case LayerVisualState.Scavenged:
                BuildLayerYieldSummary(row, layer);
                BuildLayerResultPips(row, progress, layerIndex);
                break;
            case LayerVisualState.Skipped:
                var skipLabel = new Label { Text = "skipped" };
                UIFonts.Style(skipLabel, UIFonts.Main, UIFonts.SmallSize, UIColors.TextFaint);
                row.AddChild(skipLabel);
                break;
        }
    }

    private static void BuildLayerStateBadge(HBoxContainer parent, LayerVisualState state, Color tone)
    {
        string text = state switch
        {
            LayerVisualState.Locked     => "LOCKED",
            LayerVisualState.Active     => "ACTIVE",
            LayerVisualState.Scanning   => "SCANNING",
            LayerVisualState.Scanned    => "SCANNED",
            LayerVisualState.Scavenging => "SCAVENGING",
            LayerVisualState.Scavenged  => "SCAVENGED",
            LayerVisualState.Skipped    => "SKIPPED",
            _ => "",
        };
        var badge = new Label { Text = text };
        Color textColor = state == LayerVisualState.Locked ? UIColors.TextFaint
                       : state == LayerVisualState.Skipped ? UIColors.TextDim
                       : tone;
        UIFonts.Style(badge, UIFonts.Main, UIFonts.SmallSize, textColor);
        parent.AddChild(badge);
    }

    private void BuildLayerPreviewPips(VBoxContainer parent, SalvageLayer layer, bool dim)
    {
        if (layer.ResearchUnlockChance <= 0f && layer.DangerChance <= 0f) return;
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        parent.AddChild(row);

        if (layer.ResearchUnlockChance > 0f)
        {
            var pip = new Label { Text = $"Research {(int)(layer.ResearchUnlockChance * 100)}% ?" };
            UIFonts.Style(pip, UIFonts.Main, UIFonts.SmallSize, dim ? UIColors.TextFaint : UIColors.TextDim);
            row.AddChild(pip);
        }
        if (layer.DangerChance > 0f)
        {
            var pip = new Label { Text = $"Danger {(int)(layer.DangerChance * 100)}% {layer.DangerTypeId}" };
            UIFonts.Style(pip, UIFonts.Main, UIFonts.SmallSize, dim ? UIColors.TextFaint : new Color("#e8883a"));
            row.AddChild(pip);
        }
    }

    private void BuildLayerResultPips(VBoxContainer parent, SalvageSiteProgress progress, int layerIndex)
    {
        if (!progress.LayerScanned[layerIndex] && !progress.DangerTriggered[layerIndex]) return;
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        parent.AddChild(row);

        if (progress.LayerScanned[layerIndex])
        {
            string text;
            Color color;
            if (progress.ResearchUnlocked[layerIndex])
            {
                string subId = progress.ResearchSubsystemId[layerIndex] ?? "subsystem";
                var reg = Query.TechRegistry;
                string display = (reg != null && reg.GetSubsystem(subId)?.DisplayName is { Length: > 0 } dn) ? dn : subId;
                text = $"✓ {display}";
                color = UIColors.TextBright;
            }
            else
            {
                text = "Research ✗";
                color = UIColors.TextFaint;
            }
            var resPip = new Label { Text = text };
            UIFonts.Style(resPip, UIFonts.Main, UIFonts.SmallSize, color);
            row.AddChild(resPip);
        }

        if (progress.DangerTriggered[layerIndex])
        {
            var dangerPip = new Label { Text = "Danger ⚠" };
            UIFonts.Style(dangerPip, UIFonts.Main, UIFonts.SmallSize, UIColors.AccentRed);
            row.AddChild(dangerPip);
            // One-shot pulse so the player notices when scavenging just triggered danger.
            // Rebuild fires from SiteDangerTriggered → RefreshIfRelevant; the tween runs once
            // per build, which is acceptable since the layered card is rebuilt only on terminal events.
            dangerPip.Modulate = new Color(1, 1, 1, 0f);
            var tween = dangerPip.CreateTween();
            tween.TweenProperty(dangerPip, "modulate", new Color(1, 1, 1, 1f), 0.4f)
                 .SetTrans(Tween.TransitionType.Sine)
                 .SetEase(Tween.EaseType.Out);
        }
    }

    private void BuildLayerYieldPreview(VBoxContainer parent, SalvageLayer layer)
    {
        if (layer.Yield.Count == 0) return;
        var label = new Label
        {
            Text = "Yield: " + string.Join(", ", layer.Yield.OrderBy(kv => kv.Key)
                .Select(kv => $"{kv.Value:F0} {FormatResourceKey(kv.Key)}"))
        };
        UIFonts.Style(label, UIFonts.Main, UIFonts.SmallSize, UIColors.TextBody);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        parent.AddChild(label);
    }

    private void BuildLayerYieldSummary(VBoxContainer parent, SalvageLayer layer)
    {
        var label = new Label
        {
            Text = "scavenged: " + string.Join(", ", layer.Yield.OrderBy(kv => kv.Key)
                .Select(kv => $"{kv.Value:F0} {FormatResourceKey(kv.Key)}"))
        };
        UIFonts.Style(label, UIFonts.Main, UIFonts.SmallSize, UIColors.TextBody);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        parent.AddChild(label);
    }

    private void BuildScanProgressBar(VBoxContainer parent, SalvageSiteData site, SalvageSiteProgress progress, int layerIndex, int poiId)
    {
        var layer = site.Layers[layerIndex];
        float prog = progress.LayerScanProgress[layerIndex];
        float diff = layer.ScanDifficulty;
        float frac = diff > 0f ? Mathf.Clamp(prog / diff, 0f, 1f) : 0f;

        var rateInfo = ComputeRateInfo(poiId, SiteActivity.Scanning);
        bool stalled = rateInfo.rate <= 0f;

        var head = new HBoxContainer();
        head.AddThemeConstantOverride("separation", 6);
        parent.AddChild(head);

        var label = new Label { Text = $"SCAN · {(int)(frac * 100f)}%" };
        UIFonts.Style(label, UIFonts.Main, UIFonts.SmallSize, stalled ? UIColors.TextDim : UIColors.TextBright);
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        label.TooltipText = BuildContributorsTooltip(poiId, SiteActivity.Scanning, stalled);
        head.AddChild(label);
        _layerScanLabels[(poiId, layerIndex)] = label;

        var rateLabel = new Label { Text = stalled ? "STALLED" : FormatRate(rateInfo) };
        UIFonts.Style(rateLabel, UIFonts.Main, UIFonts.SmallSize, stalled ? new Color("#ff8866") : UIColors.TextDim);
        rateLabel.TooltipText = BuildRateBreakdownTooltip(poiId, SiteActivity.Scanning, rateInfo);
        head.AddChild(rateLabel);

        var bar = new ProgressBar
        {
            MinValue = 0, MaxValue = 1, Value = frac,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 3),
            Modulate = stalled ? new Color(1, 1, 1, 0.5f) : Colors.White,
        };
        parent.AddChild(bar);
        _layerScanBars[(poiId, layerIndex)] = bar;
    }

    private void BuildLayerYieldBars(VBoxContainer parent, SalvageLayer layer, int layerIndex, int poiId)
    {
        var rateInfo = ComputeRateInfo(poiId, SiteActivity.Extracting);
        bool stalled = rateInfo.rate <= 0f;

        var head = new HBoxContainer();
        head.AddThemeConstantOverride("separation", 6);
        parent.AddChild(head);

        var label = new Label { Text = "SCAVENGING" };
        UIFonts.Style(label, UIFonts.Main, UIFonts.SmallSize, stalled ? UIColors.TextDim : UIColors.TextBright);
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        label.TooltipText = BuildContributorsTooltip(poiId, SiteActivity.Extracting, stalled);
        head.AddChild(label);

        var rateLabel = new Label { Text = stalled ? "STALLED" : FormatRate(rateInfo) };
        UIFonts.Style(rateLabel, UIFonts.Main, UIFonts.SmallSize, stalled ? new Color("#ff8866") : UIColors.TextDim);
        rateLabel.TooltipText = BuildRateBreakdownTooltip(poiId, SiteActivity.Extracting, rateInfo);
        head.AddChild(rateLabel);

        if (!_layerYieldWidgets.TryGetValue((poiId, layerIndex), out var widgetMap))
            _layerYieldWidgets[(poiId, layerIndex)] = widgetMap = new Dictionary<string, (Label, ProgressBar)>();

        foreach (var kv in layer.Yield)
        {
            float total = kv.Value;
            float remaining = layer.RemainingYield.GetValueOrDefault(kv.Key);
            float frac = total > 0 ? Mathf.Clamp(remaining / total, 0, 1) : 0;

            var rowH = new HBoxContainer();
            rowH.AddThemeConstantOverride("separation", 6);
            parent.AddChild(rowH);

            var nameLabel = new Label { Text = FormatResourceKey(kv.Key) };
            UIFonts.Style(nameLabel, UIFonts.Main, UIFonts.SmallSize, stalled ? UIColors.TextDim : UIColors.TextBody);
            nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            rowH.AddChild(nameLabel);

            var amountLabel = new Label { Text = $"{remaining:F0} / {total:F0}" };
            UIFonts.Style(amountLabel, UIFonts.Main, UIFonts.SmallSize, stalled ? UIColors.TextDim : UIColors.TextBright);
            rowH.AddChild(amountLabel);

            var barWrap = new Control();
            barWrap.CustomMinimumSize = new Vector2(0, 3);
            barWrap.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            parent.AddChild(barWrap);

            var bar = new ProgressBar
            {
                MinValue = 0, MaxValue = 1, Value = frac,
                ShowPercentage = false,
                Modulate = stalled ? new Color(1, 1, 1, 0.45f) : Colors.White,
            };
            bar.SetAnchorsPreset(LayoutPreset.FullRect);
            barWrap.AddChild(bar);
            widgetMap[kv.Key] = (amountLabel, bar);

            var marker = new ColorRect();
            marker.Color = new Color(UIColors.TextDim, 0.9f);
            marker.AnchorLeft = SalvageSystem.InflectionRemainingFraction;
            marker.AnchorRight = SalvageSystem.InflectionRemainingFraction;
            marker.AnchorTop = 0;
            marker.AnchorBottom = 1;
            marker.OffsetLeft = 0;
            marker.OffsetRight = 1;
            marker.MouseFilter = MouseFilterEnum.Ignore;
            barWrap.AddChild(marker);
        }
    }

    private void BuildLayerActions_Active(VBoxContainer parent, int poiId, Color accent)
    {
        float cap = Query.GetSystemCapability(poiId, SiteActivity.Scanning);
        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", 6);
        parent.AddChild(actionRow);

        var scan = MakeActionButton("SCAN", primary: true, accent: accent);
        scan.Disabled = cap <= 0f;
        if (scan.Disabled) scan.TooltipText = "Requires a scout-class ship in system.";
        scan.Pressed += () => EventBus.Instance?.FireScanToggleRequested(poiId);
        actionRow.AddChild(scan);
    }

    private void BuildLayerActions_StopScanning(VBoxContainer parent, int poiId)
    {
        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", 6);
        parent.AddChild(actionRow);
        var stop = MakeActionButton("STOP", primary: true, accent: new Color("#ff8866"));
        stop.Pressed += () => EventBus.Instance?.FireScanToggleRequested(poiId);
        actionRow.AddChild(stop);
    }

    private void BuildLayerActions_Scanned(VBoxContainer parent, int poiId, Color accent)
    {
        float cap = Query.GetSystemCapability(poiId, SiteActivity.Extracting);
        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", 6);
        parent.AddChild(actionRow);

        var scavenge = MakeActionButton("SCAVENGE", primary: true, accent: accent);
        scavenge.Disabled = cap <= 0f;
        if (scavenge.Disabled) scavenge.TooltipText = "Requires a salvager-class ship in system.";
        scavenge.Pressed += () => EventBus.Instance?.FireExtractToggleRequested(poiId);
        actionRow.AddChild(scavenge);

        var skipBtn = MakeActionButton("SKIP", primary: false, accent: UIColors.TextDim);
        skipBtn.Pressed += () => EventBus.Instance?.FireSkipLayerRequested(poiId);
        actionRow.AddChild(skipBtn);
    }

    private void BuildLayerActions_StopScavenging(VBoxContainer parent, int poiId)
    {
        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", 6);
        parent.AddChild(actionRow);
        var stop = MakeActionButton("STOP", primary: true, accent: new Color("#ff8866"));
        stop.Pressed += () => EventBus.Instance?.FireExtractToggleRequested(poiId);
        actionRow.AddChild(stop);
    }

    private void BuildSpecialOutcomeFooter(VBoxContainer parent, SalvageSiteData site, SalvageSiteProgress progress, int poiId)
    {
        if (string.IsNullOrEmpty(site.SpecialOutcomeId)) return;

        var sep = new HSeparator();
        parent.AddChild(sep);

        string outcomeName = HumanizeOutcomeId(site.SpecialOutcomeId);
        Color outcomeColor = ColorForPrecursor(site.Color);

        if (progress.SpecialOutcomeConsumed)
        {
            var done = new Label { Text = $"✓ {outcomeName} consumed" };
            UIFonts.Style(done, UIFonts.Main, UIFonts.SmallSize, UIColors.TextDim);
            parent.AddChild(done);
            return;
        }

        if (!progress.SpecialOutcomeAvailable)
        {
            var locked = new Label { Text = $"Outcome: {outcomeName} (locked)" };
            UIFonts.Style(locked, UIFonts.Main, UIFonts.SmallSize, UIColors.TextFaint);
            parent.AddChild(locked);
            return;
        }

        var (canAfford, costStr) = ResolveOutcomeCost(site);
        var costLabel = new Label { Text = $"⚒ READY: {outcomeName}\nCost: {costStr}" };
        UIFonts.Style(costLabel, UIFonts.Main, UIFonts.SmallSize, UIColors.TextBody);
        costLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        parent.AddChild(costLabel);

        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", 6);
        parent.AddChild(actionRow);

        var btn = MakeActionButton(OutcomeButtonLabel(site.SpecialOutcomeId), primary: true, accent: outcomeColor);
        btn.Disabled = !canAfford;
        if (!canAfford) btn.TooltipText = $"Insufficient resources. Need: {costStr}";
        btn.Pressed += () => EventBus.Instance?.FireSpecialOutcomeRequested(poiId);
        actionRow.AddChild(btn);
    }

    private (bool canAfford, string costStr) ResolveOutcomeCost(SalvageSiteData site)
    {
        var player = Query.PlayerEmpire;
        if (player == null || string.IsNullOrEmpty(site.SpecialOutcomeId)) return (false, "?");

        var def = DataLoader.Instance?.Salvage?.GetOutcome(site.SpecialOutcomeId);
        if (def == null) return (false, "?");

        var primary = site.Color;
        var resolved = new Dictionary<string, float>();
        foreach (var kv in def.Cost)
        {
            string key = kv.Key.Contains('_')
                ? kv.Key
                : (System.Enum.TryParse<ResourceType>(kv.Key, out var t) ? EmpireData.ResourceKey(primary, t) : kv.Key);
            resolved[key] = resolved.GetValueOrDefault(key) + kv.Value;
        }

        bool canAfford = true;
        foreach (var kv in resolved)
        {
            float have = player.ResourceStockpile.GetValueOrDefault(kv.Key);
            if (have + 1e-4f < kv.Value) { canAfford = false; break; }
        }

        string costStr = string.Join(", ", resolved.Select(kv => $"{kv.Value:F0} {FormatResourceKey(kv.Key)}"));
        return (canAfford, costStr);
    }

    private static string OutcomeButtonLabel(string? outcomeId) => outcomeId switch
    {
        "repair_station"   => "REPAIR STATION",
        "recover_derelict" => "RECOVER DERELICT",
        _                  => "ACTIVATE",
    };

    private static string HumanizeOutcomeId(string? id) => id switch
    {
        null               => "",
        "repair_station"   => "Repair Station",
        "recover_derelict" => "Recover Derelict",
        _                  => id.Replace('_', ' '),
    };

    private static void BuildColorDots(HBoxContainer parent, IReadOnlyList<PrecursorColor> colors)
    {
        const int dotSize = 10;
        for (int i = 0; i < colors.Count; i++)
        {
            var c = UIColors.GetPrecursor(colors[i], UIColors.Tone.Normal);
            var box = new StyleBoxFlat { BgColor = c };
            box.SetCornerRadiusAll(dotSize / 2);
            var dot = new PanelContainer
            {
                CustomMinimumSize = new Vector2(dotSize, dotSize),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            dot.AddThemeStyleboxOverride("panel", box);
            parent.AddChild(dot);
        }
    }

    private (float rate, int siblingCount, float totalCap) ComputeRateInfo(int poiId, SiteActivity type)
    {
        float totalCap = Query.GetSystemCapability(poiId, type);
        int n = Query.GetSystemActiveCount(poiId, type);
        float rate = n > 0 ? totalCap / n : 0f;
        return (rate, n, totalCap);
    }

    private static string FormatRate((float rate, int siblingCount, float totalCap) info)
    {
        string r = $"+{info.rate:F1}/s";
        return info.siblingCount > 1 ? $"{r} ÷{info.siblingCount}" : r;
    }

    private string BuildContributorsTooltip(int poiId, SiteActivity type, bool stalled)
    {
        var player = Query.PlayerEmpire;
        if (player == null) return "";
        var fleets = Query.GetContributingFleets(player.Id, poiId);
        if (fleets.Count == 0)
            return stalled ? "No capable fleets in system.\nActivity preserved; arrival resumes progress." : "";
        var verb = type == SiteActivity.Scanning ? "Scanning" : "Extracting";
        return $"{verb} fleets: " + string.Join(", ", fleets.Select(f => f.Name));
    }

    private string BuildRateBreakdownTooltip(int poiId, SiteActivity type, (float rate, int siblingCount, float totalCap) info)
    {
        if (info.siblingCount == 0) return "";
        string typeWord = type == SiteActivity.Scanning ? "scan" : "extraction";
        var lines = new List<string>
        {
            $"Total system {typeWord} capacity: {info.totalCap:F1}/s",
            $"Split across {info.siblingCount} active {typeWord}{(info.siblingCount == 1 ? "" : "s")}",
            $"This site: {info.rate:F1}/s",
        };
        var player = Query.PlayerEmpire;
        if (player != null)
        {
            var fleets = Query.GetContributingFleets(player.Id, poiId);
            if (fleets.Count > 0)
                lines.Add("Contributing fleets: " + string.Join(", ", fleets.Select(f => f.Name)));
        }
        return string.Join("\n", lines);
    }

    private static string FormatResourceKey(string key) => key.Replace('_', ' ').ToUpper();

    private static string SalvageTypeTag(string typeId) => typeId switch
    {
        "minor_derelict"        => "DERELICT",
        "debris_field"          => "DEBRIS",
        "ship_graveyard"        => "GRAVEYARD",
        "major_precursor_site"  => "PRECURSOR",
        "precursor_intersection" => "INTERSECT",
        "failed_salvager_wreck" => "WRECK",
        "desperation_project"   => "STATION",
        "old_battlefield"       => "BATTLEFIELD",
        _                       => typeId.Replace('_', ' ').ToUpper(),
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

    private static Button MakeActionButton(string text, bool primary, Color accent)
    {
        var btn = new Button { Text = text };
        btn.CustomMinimumSize = new Vector2(0, 28);
        btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var normal = new StyleBoxFlat
        {
            BgColor = primary ? new Color(accent, 0.16f) : new Color(14 / 255f, 20 / 255f, 40 / 255f, 0.9f),
        };
        normal.SetBorderWidthAll(1);
        normal.BorderColor = primary ? new Color(accent, 0.55f) : UIColors.BorderMid;
        normal.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("normal", normal);

        var hover = new StyleBoxFlat { BgColor = new Color(accent, 0.28f) };
        hover.SetBorderWidthAll(1);
        hover.BorderColor = new Color(accent, 0.8f);
        hover.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        btn.AddThemeStyleboxOverride("focus", normal);

        var disabled = new StyleBoxFlat { BgColor = new Color(14 / 255f, 20 / 255f, 40 / 255f, 0.4f) };
        disabled.SetBorderWidthAll(1);
        disabled.BorderColor = UIColors.BorderDim;
        disabled.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("disabled", disabled);

        UIFonts.StyleButton(btn, UIFonts.Main, UIFonts.SmallSize, primary ? accent : UIColors.TextBody);
        return btn;
    }

    private static Control BuildFallbackCard(POIData poi)
    {
        var lbl = new Label { Text = poi.Name };
        UIFonts.Style(lbl, UIFonts.Main, UIFonts.SmallSize, UIColors.TextFaint);
        return lbl;
    }

    private static void AddStatReadout(HBoxContainer parent, string label, string value)
    {
        var stat = new VBoxContainer();
        stat.AddThemeConstantOverride("separation", 1);
        parent.AddChild(stat);

        var lbl = new Label { Text = label };
        UIFonts.Style(lbl, UIFonts.Main, UIFonts.SmallSize, UIColors.TextBody);
        stat.AddChild(lbl);

        var val = new Label { Text = value };
        UIFonts.Style(val, UIFonts.Main, UIFonts.NormalSize, UIColors.TextBright);
        stat.AddChild(val);
    }

    private static Color GetPOIColor(POIType type) => type switch
    {
        POIType.HabitablePlanet => new Color("#4caf50"),
        POIType.BarrenPlanet    => UIColors.TextDim,
        POIType.AsteroidField   => new Color("#ddaa22"),
        POIType.DebrisField     => new Color("#b366e8"),
        POIType.AbandonedStation => new Color("#b366e8"),
        POIType.ShipGraveyard   => new Color("#e85545"),
        POIType.Megastructure   => new Color("#44aaff"),
        _                       => UIColors.TextDim,
    };

    private static Color GetPOITint(POIType type) => type switch
    {
        POIType.HabitablePlanet => new Color(76 / 255f, 175 / 255f, 80 / 255f, 0.04f),
        POIType.DebrisField or POIType.AbandonedStation => new Color(179 / 255f, 102 / 255f, 232 / 255f, 0.04f),
        POIType.AsteroidField   => new Color(138 / 255f, 138 / 255f, 60 / 255f, 0.06f),
        POIType.Megastructure   => new Color(68 / 255f, 170 / 255f, 255 / 255f, 0.03f),
        POIType.ShipGraveyard   => new Color(232 / 255f, 85 / 255f, 69 / 255f, 0.04f),
        _                       => Colors.Transparent,
    };

    private static string GetPOITypeLabel(POIType type) => type switch
    {
        POIType.HabitablePlanet => "Colony",
        POIType.BarrenPlanet    => "Barren",
        POIType.AsteroidField   => "Asteroid Field",
        POIType.DebrisField     => "Derelict",
        POIType.AbandonedStation => "Abandoned",
        POIType.ShipGraveyard   => "Graveyard",
        POIType.Megastructure   => "Megastructure",
        _                       => type.ToString(),
    };

    private static string GetPOIMeta(POIData poi) => poi.Type switch
    {
        POIType.HabitablePlanet => $"SIZE {poi.PlanetSize}  DEPOSITS {poi.Deposits?.Count ?? 0}",
        POIType.AsteroidField   => $"DEPOSITS {poi.Deposits?.Count ?? 0}",
        POIType.BarrenPlanet    => $"SIZE {poi.PlanetSize}",
        _                       => $"COLOR {poi.DominantColor}",
    };
}
