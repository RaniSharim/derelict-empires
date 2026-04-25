using Godot;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Exploration;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Nodes.Map;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Right-side selection panel. Layout shell in <c>scenes/ui/right_panel.tscn</c>;
/// POI card construction stays code-built (sub-scene extraction is a follow-up).
/// MainScene reference still injected for SalvageSystem/MovementSystem queries; will
/// route through IGameQuery once it grows extraction-rate helpers.
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
    private MainScene? _mainScene;
    private int _hostileSelectedFleetId = -1;

    // Per-POI widget refs — updated in place on progress events so the full panel
    // doesn't rebuild 10×/sec during active scans. Cleared on every full rebuild.
    private readonly Dictionary<int, Label> _scanHeaderLabels = new();
    private readonly Dictionary<int, ProgressBar> _scanBars = new();
    private readonly Dictionary<int, Dictionary<string, (Label amount, ProgressBar bar)>> _yieldWidgets = new();

    public void SetMainScene(MainScene mainScene) => _mainScene = mainScene;

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
            EventBus.Instance.SystemSelected         += OnSystemSelected;
            EventBus.Instance.SystemDeselected       += OnSystemDeselected;
            EventBus.Instance.FleetSelected          += OnFleetSelectedForPanel;
            EventBus.Instance.FleetDeselected        += OnFleetDeselectedForPanel;
            EventBus.Instance.SiteDiscovered         += OnSiteDiscovered;
            EventBus.Instance.ScanProgressChanged    += OnScanProgressChanged;
            EventBus.Instance.SiteScanComplete       += OnSiteScanComplete;
            EventBus.Instance.YieldExtracted         += OnYieldExtracted;
            EventBus.Instance.FleetArrivedAtSystem   += OnFleetArrivedAtSystem;
            EventBus.Instance.SiteActivityChanged    += OnSiteActivityChanged;
            EventBus.Instance.SiteActivityRateChanged += OnSiteActivityRateChanged;
        }
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.SystemSelected         -= OnSystemSelected;
            EventBus.Instance.SystemDeselected       -= OnSystemDeselected;
            EventBus.Instance.SiteDiscovered         -= OnSiteDiscovered;
            EventBus.Instance.ScanProgressChanged    -= OnScanProgressChanged;
            EventBus.Instance.SiteScanComplete       -= OnSiteScanComplete;
            EventBus.Instance.YieldExtracted         -= OnYieldExtracted;
            EventBus.Instance.FleetArrivedAtSystem   -= OnFleetArrivedAtSystem;
            EventBus.Instance.SiteActivityChanged    -= OnSiteActivityChanged;
            EventBus.Instance.SiteActivityRateChanged -= OnSiteActivityRateChanged;
            EventBus.Instance.FleetSelected          -= OnFleetSelectedForPanel;
            EventBus.Instance.FleetDeselected        -= OnFleetDeselectedForPanel;
        }
    }

    private void OnSiteDiscovered(int empireId, int poiId) => RefreshIfRelevant(poiId);

    /// <summary>In-place update — avoids the per-tick rebuild that was eating clicks on sibling cards.</summary>
    private void OnScanProgressChanged(int empireId, int poiId, float progress, float difficulty)
    {
        if (_scanBars.TryGetValue(poiId, out var bar))
        {
            float frac = difficulty > 0 ? Mathf.Clamp(progress / difficulty, 0f, 1f) : 0f;
            bar.Value = frac;
            if (_scanHeaderLabels.TryGetValue(poiId, out var label))
                label.Text = $"SCANNING \u00B7 {(int)(frac * 100f)}%";
        }
    }

    private void OnSiteScanComplete(int empireId, int poiId) => RefreshIfRelevant(poiId);

    private void OnYieldExtracted(int empireId, int poiId, string key, float amount)
    {
        if (!_yieldWidgets.TryGetValue(poiId, out var byKey)) return;
        if (!byKey.TryGetValue(key, out var w)) return;
        if (_mainScene?.GetSalvageSite(FindSiteIdForPoi(poiId) ?? -1) is not { } site) return;
        float total = site.TotalYield.GetValueOrDefault(key);
        float remaining = site.RemainingYield.GetValueOrDefault(key);
        w.amount.Text = $"{remaining:F0} / {total:F0}";
        w.bar.Value = total > 0 ? Mathf.Clamp(remaining / total, 0, 1) : 0;
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
        if (_mainScene == null) return;

        var fleet = _mainScene.Fleets.FirstOrDefault(f => f.Id == fleetId);
        var playerId = _mainScene.PlayerEmpire?.Id ?? -1;
        if (fleet == null || fleet.OwnerEmpireId == playerId)
        {
            _hostileSelectedFleetId = -1;
            _hostileFleetSection.Visible = false;
            return;
        }

        _hostileSelectedFleetId = fleet.Id;
        _hostileFleetTitle.Text = fleet.Name.ToUpperInvariant();
        int shipCount = fleet.ShipIds.Count;
        var sys = GameManager.Instance?.Galaxy?.GetSystem(fleet.CurrentSystemId);
        _hostileFleetInfo.Text =
            $"{shipCount} ship{(shipCount == 1 ? "" : "s")} \u00B7 {sys?.Name?.ToUpperInvariant() ?? "UNKNOWN"}";
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
        if (_hostileSelectedFleetId < 0 || _mainScene == null) return;
        var hostile = _mainScene.Fleets.FirstOrDefault(f => f.Id == _hostileSelectedFleetId);
        if (hostile == null) return;
        var player = _mainScene.PlayerEmpire;
        if (player == null) return;

        var friendly = _mainScene.Fleets.FirstOrDefault(f =>
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
        _systemInfo.Text = $"{region} \u00B7 {color}";

        RebuildPOIList(system);
    }

    private void OnSystemDeselected()
    {
        _selectedSystem = null;
        Visible = false;
    }

    private void RebuildPOIList(StarSystemData system)
    {
        _scanHeaderLabels.Clear();
        _scanBars.Clear();
        _yieldWidgets.Clear();
        foreach (var child in _poiList.GetChildren())
            child.QueueFree();

        int playerId = _mainScene?.PlayerEmpire?.Id ?? -1;
        int shown = 0;
        foreach (var poi in system.POIs)
        {
            if (poi.SalvageSiteId.HasValue && _mainScene?.ExplorationManager is { } exp && playerId >= 0)
            {
                var state = exp.GetState(playerId, poi.Id);
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
        if (poi.SalvageSiteId.HasValue && _mainScene != null)
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
        var site = _mainScene!.GetSalvageSite(siteId);
        var player = _mainScene.PlayerEmpire;
        var salvage = _mainScene.SalvageSystem;
        if (site == null || player == null || salvage == null)
            return BuildFallbackCard(poi);

        var state = _mainScene.ExplorationManager?.GetState(player.Id, poi.Id) ?? ExplorationState.Undiscovered;
        var activity = salvage.GetActivity(player.Id, poi.Id);
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

        var row1 = new HBoxContainer();
        row1.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(row1);

        var nameLabel = new Label { Text = poi.Name };
        UIFonts.Style(nameLabel, UIFonts.Title, UIFonts.TitleSize, primaryColor);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        nameLabel.ClipText = true;
        row1.AddChild(nameLabel);

        var tagLabel = new Label { Text = $"{SalvageTypeTag(site.Type)} \u00B7 {site.Color.ToString().ToUpper()}" };
        UIFonts.Style(tagLabel, UIFonts.Main, UIFonts.SmallSize, UIColors.TextDim);
        row1.AddChild(tagLabel);

        bool showExtractBars = state == ExplorationState.Surveyed;

        if (activity == SiteActivity.Scanning)
            BuildScanProgress(vbox, site, player.Id, poi.Id);
        else if (activity == SiteActivity.Extracting)
        {
            BuildExtractProgress(vbox, site, player.Id, poi.Id);
            showExtractBars = false;
        }
        else if (state == ExplorationState.Discovered)
        {
            var hint = new Label { Text = "YIELD UNKNOWN — SCAN TO REVEAL" };
            UIFonts.Style(hint, UIFonts.Main, UIFonts.SmallSize, UIColors.TextFaint);
            vbox.AddChild(hint);
        }

        if (showExtractBars)
            BuildYieldBars(vbox, site, showInflection: true);

        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", 6);
        vbox.AddChild(actionRow);

        int poiIdCapture = poi.Id;
        if (activity == SiteActivity.Scanning || activity == SiteActivity.Extracting)
        {
            var cancel = MakeActionButton("CANCEL", primary: true, accent: new Color("#ff8866"));
            if (activity == SiteActivity.Scanning)
                cancel.Pressed += () => EventBus.Instance?.FireScanToggleRequested(poiIdCapture);
            else
                cancel.Pressed += () => EventBus.Instance?.FireExtractToggleRequested(poiIdCapture);
            actionRow.AddChild(cancel);
        }
        else if (state == ExplorationState.Surveyed)
        {
            float cap = _mainScene.GetSystemCapability(poi.Id, SiteActivity.Extracting);
            var extract = MakeActionButton("EXTRACT", primary: true, accent: primaryColor);
            extract.Disabled = cap <= 0f;
            if (extract.Disabled) extract.TooltipText = "Requires a salvager-class ship in system.";
            extract.Pressed += () => EventBus.Instance?.FireExtractToggleRequested(poiIdCapture);
            actionRow.AddChild(extract);
        }
        else
        {
            float cap = _mainScene.GetSystemCapability(poi.Id, SiteActivity.Scanning);
            var scan = MakeActionButton("SCAN", primary: true, accent: primaryColor);
            scan.Disabled = cap <= 0f;
            if (scan.Disabled) scan.TooltipText = "Requires a scout-class ship in system.";
            scan.Pressed += () => EventBus.Instance?.FireScanToggleRequested(poiIdCapture);
            actionRow.AddChild(scan);
        }

        return card;
    }

    private void BuildScanProgress(VBoxContainer parent, SalvageSiteData site, int empireId, int poiId)
    {
        float progress = _mainScene?.ExplorationManager?.GetScanProgress(empireId, poiId) ?? 0f;
        float frac = site.ScanDifficulty > 0f ? Mathf.Clamp(progress / site.ScanDifficulty, 0f, 1f) : 0f;

        var rateInfo = ComputeRateInfo(poiId, SiteActivity.Scanning);
        bool stalled = rateInfo.rate <= 0f;

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 6);
        parent.AddChild(header);

        var label = new Label { Text = $"SCANNING \u00B7 {(int)(frac * 100f)}%" };
        UIFonts.Style(label, UIFonts.Main, UIFonts.SmallSize, stalled ? UIColors.TextDim : UIColors.TextBright);
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        label.TooltipText = BuildContributorsTooltip(poiId, SiteActivity.Scanning, stalled);
        header.AddChild(label);
        _scanHeaderLabels[poiId] = label;

        var rateLabel = new Label { Text = stalled ? "STALLED" : FormatRate(rateInfo) };
        UIFonts.Style(rateLabel, UIFonts.Main, UIFonts.SmallSize, stalled ? new Color("#ff8866") : UIColors.TextDim);
        rateLabel.TooltipText = BuildRateBreakdownTooltip(poiId, SiteActivity.Scanning, rateInfo);
        header.AddChild(rateLabel);

        var bar = new ProgressBar
        {
            MinValue = 0, MaxValue = 1, Value = frac,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 3),
            Modulate = stalled ? new Color(1, 1, 1, 0.5f) : Colors.White,
        };
        parent.AddChild(bar);
        _scanBars[poiId] = bar;
    }

    private void BuildExtractProgress(VBoxContainer parent, SalvageSiteData site, int empireId, int poiId)
    {
        var rateInfo = ComputeRateInfo(poiId, SiteActivity.Extracting);
        bool stalled = rateInfo.rate <= 0f;

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 6);
        parent.AddChild(header);

        var label = new Label { Text = "EXTRACTING" };
        UIFonts.Style(label, UIFonts.Main, UIFonts.SmallSize, stalled ? UIColors.TextDim : UIColors.TextBright);
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        label.TooltipText = BuildContributorsTooltip(poiId, SiteActivity.Extracting, stalled);
        header.AddChild(label);

        var rateLabel = new Label { Text = stalled ? "STALLED" : FormatRate(rateInfo) };
        UIFonts.Style(rateLabel, UIFonts.Main, UIFonts.SmallSize, stalled ? new Color("#ff8866") : UIColors.TextDim);
        rateLabel.TooltipText = BuildRateBreakdownTooltip(poiId, SiteActivity.Extracting, rateInfo);
        header.AddChild(rateLabel);

        BuildYieldBars(parent, site, showInflection: true, desaturate: stalled);
    }

    private (float rate, int siblingCount, float totalCap) ComputeRateInfo(int poiId, SiteActivity type)
    {
        if (_mainScene == null) return (0, 0, 0);
        float totalCap = _mainScene.GetSystemCapability(poiId, type);
        int n = _mainScene.GetSystemActiveCount(poiId, type);
        float rate = n > 0 ? totalCap / n : 0f;
        return (rate, n, totalCap);
    }

    private static string FormatRate((float rate, int siblingCount, float totalCap) info)
    {
        string r = $"+{info.rate:F1}/s";
        return info.siblingCount > 1 ? $"{r} \u00F7{info.siblingCount}" : r;
    }

    private string BuildContributorsTooltip(int poiId, SiteActivity type, bool stalled)
    {
        if (_mainScene?.SalvageSystem == null || _mainScene.PlayerEmpire == null) return "";
        var fleets = _mainScene.SalvageSystem.GetContributingFleets(
            _mainScene.PlayerEmpire.Id, poiId, _mainScene.Fleets, _mainScene.ShipsById);
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
        if (_mainScene?.SalvageSystem != null && _mainScene.PlayerEmpire != null)
        {
            var fleets = _mainScene.SalvageSystem.GetContributingFleets(
                _mainScene.PlayerEmpire.Id, poiId, _mainScene.Fleets, _mainScene.ShipsById);
            if (fleets.Count > 0)
                lines.Add("Contributing fleets: " + string.Join(", ", fleets.Select(f => f.Name)));
        }
        return string.Join("\n", lines);
    }

    private void BuildYieldBars(VBoxContainer parent, SalvageSiteData site, bool showInflection = false, bool desaturate = false)
    {
        if (!_yieldWidgets.ContainsKey(site.POIId))
            _yieldWidgets[site.POIId] = new Dictionary<string, (Label, ProgressBar)>();
        var widgetMap = _yieldWidgets[site.POIId];

        foreach (var kv in site.TotalYield)
        {
            float total = kv.Value;
            float remaining = site.RemainingYield.GetValueOrDefault(kv.Key);
            float frac = total > 0 ? Mathf.Clamp(remaining / total, 0, 1) : 0;

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);
            parent.AddChild(row);

            var nameLabel = new Label { Text = FormatResourceKey(kv.Key) };
            UIFonts.Style(nameLabel, UIFonts.Main, UIFonts.SmallSize, desaturate ? UIColors.TextDim : UIColors.TextBody);
            nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(nameLabel);

            var amountLabel = new Label { Text = $"{remaining:F0} / {total:F0}" };
            UIFonts.Style(amountLabel, UIFonts.Main, UIFonts.SmallSize, desaturate ? UIColors.TextDim : UIColors.TextBright);
            row.AddChild(amountLabel);

            var barWrap = new Control();
            barWrap.CustomMinimumSize = new Vector2(0, 3);
            barWrap.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            parent.AddChild(barWrap);

            var bar = new ProgressBar
            {
                MinValue = 0, MaxValue = 1, Value = frac,
                ShowPercentage = false,
                Modulate = desaturate ? new Color(1, 1, 1, 0.45f) : Colors.White,
            };
            bar.SetAnchorsPreset(LayoutPreset.FullRect);
            barWrap.AddChild(bar);
            widgetMap[kv.Key] = (amountLabel, bar);

            if (showInflection)
            {
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
    }

    private static string FormatResourceKey(string key) => key.Replace('_', ' ').ToUpper();

    private static string SalvageTypeTag(SalvageSiteType type) => type switch
    {
        SalvageSiteType.MinorDerelict        => "DERELICT",
        SalvageSiteType.DebrisField          => "DEBRIS",
        SalvageSiteType.ShipGraveyard        => "GRAVEYARD",
        SalvageSiteType.MajorPrecursorSite   => "PRECURSOR",
        SalvageSiteType.PrecursorIntersection => "INTERSECT",
        SalvageSiteType.FailedSalvagerWreck  => "WRECK",
        SalvageSiteType.DesperationProject   => "STATION",
        _                                    => type.ToString().ToUpper(),
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
