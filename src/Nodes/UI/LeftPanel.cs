using Godot;
using System.Collections.Generic;
using System.Linq;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Services;
using DerlictEmpires.Core.Ships;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Left edge HUD with FLEETS / COLONIES / RESEARCH / BUILD tabs. Layout in
/// <c>scenes/ui/left_panel.tscn</c>; per-fleet rows from <c>scenes/ui/fleet_card.tscn</c>.
/// Reads game state through <see cref="IGameQuery"/>; writes via EventBus intent events.
/// </summary>
public partial class LeftPanel : Control
{
    public const int PanelWidth = 310;

    [Export] private PanelContainer _background = null!;
    [Export] private Button _tabFleets = null!;
    [Export] private Button _tabColonies = null!;
    [Export] private Button _tabResearch = null!;
    [Export] private Button _tabBuild = null!;
    [Export] private VBoxContainer _listContainer = null!;
    [Export] private PackedScene _fleetCardScene = null!;

    private readonly Button[] _tabs = new Button[4];
    private static readonly string[] TabNames = { "FLEETS", "COLONIES", "RESEARCH", "BUILD" };
    private int _activeTab = 0;
    private readonly HashSet<int> _selectedFleetIds = new();

    private List<FleetData> _fleets = new();
    private List<ShipInstanceData> _ships = new();
    private ResearchTabContent? _researchContent;

    private static IGameQuery Query => GameManager.Instance!;

    public override void _Ready()
    {
        GlassPanel.Apply(_background, enableBlur: true);

        _tabs[0] = _tabFleets;
        _tabs[1] = _tabColonies;
        _tabs[2] = _tabResearch;
        _tabs[3] = _tabBuild;

        for (int i = 0; i < _tabs.Length; i++)
        {
            int idx = i;
            UIFonts.StyleButton(_tabs[i], UIFonts.Main, UIFonts.SmallSize, UIColors.TextBody);
            _tabs[i].ClipText = true;
            _tabs[i].Pressed += () => SetActiveTab(idx);
            StyleTab(_tabs[i], i == _activeTab);
        }
        _tabColonies.Disabled = true;

        var bus = EventBus.Instance;
        if (bus != null)
        {
            bus.FleetSelected += OnFleetSelected;
            bus.FleetSelectionToggled += OnFleetSelectionToggled;
            bus.FleetDeselected += OnFleetDeselected;
            bus.FleetOrderChanged += OnFleetOrderChanged;
            bus.FleetArrivedAtSystem += OnFleetArrivedAtSystem;
            bus.SiteActivityChanged += OnSiteActivityChanged;
            bus.SiteActivityRateChanged += OnSiteActivityRateChanged;
            bus.DesignSaved += OnDesignSaved;
        }
    }

    public override void _ExitTree()
    {
        var bus = EventBus.Instance;
        if (bus == null) return;
        bus.FleetSelected -= OnFleetSelected;
        bus.FleetSelectionToggled -= OnFleetSelectionToggled;
        bus.FleetDeselected -= OnFleetDeselected;
        bus.FleetOrderChanged -= OnFleetOrderChanged;
        bus.FleetArrivedAtSystem -= OnFleetArrivedAtSystem;
        bus.SiteActivityChanged -= OnSiteActivityChanged;
        bus.SiteActivityRateChanged -= OnSiteActivityRateChanged;
        bus.DesignSaved -= OnDesignSaved;
    }

    private void OnFleetOrderChanged(int fleetId) => RebuildList();
    private void OnFleetArrivedAtSystem(int fleetId, int systemId) => RebuildList();
    private void OnSiteActivityChanged(int empireId, int poiId, SiteActivity activity) => RebuildList();
    private void OnSiteActivityRateChanged(int empireId, int poiId) => RebuildList();
    private void OnDesignSaved(string designId) { if (_activeTab == 3) RebuildList(); }

    public void SetData(List<FleetData> fleets, List<ShipInstanceData> ships)
    {
        _fleets = fleets;
        _ships = ships;
        RebuildList();
    }

    private void SetActiveTab(int index)
    {
        _activeTab = index;
        for (int i = 0; i < _tabs.Length; i++)
            StyleTab(_tabs[i], i == index);
        RebuildList();
    }

    private void RebuildList()
    {
        foreach (var child in _listContainer.GetChildren())
            child.QueueFree();
        _researchContent = null;

        switch (_activeTab)
        {
            case 0: BuildFleetList(); break;
            case 2: BuildResearchTab(); break;
            case 3: BuildDesignsList(); break;
            default: BuildPlaceholder(TabNames[_activeTab]); break;
        }
    }

    private void BuildFleetList()
    {
        int playerId = GameManager.Instance?.LocalPlayerEmpire?.Id ?? -1;
        var playerFleets = _fleets.Where(f => f.OwnerEmpireId == playerId).ToList();

        if (playerFleets.Count == 0)
        {
            BuildEmptyMessage("No fleets available");
            return;
        }

        var galaxy = GameManager.Instance?.Galaxy;
        foreach (var fleet in playerFleets)
        {
            var card = _fleetCardScene.Instantiate<FleetCard>();
            _listContainer.AddChild(card);
            string sysName = galaxy?.GetSystem(fleet.CurrentSystemId)?.Name ?? $"System {fleet.CurrentSystemId}";
            var ships = _ships.Where(s => s.FleetId == fleet.Id).ToList();
            int shipCount = ships.Count;
            var (statusText, statusColor) = GetFleetStatus(fleet);
            card.Populate(
                fleet,
                ships,
                statusText,
                statusColor,
                accentColor: UIColors.Accent,
                isSelected: _selectedFleetIds.Contains(fleet.Id),
                locationText: $"Location: {sysName} \u00B7 {shipCount} SHIPS",
                tooltipText: BuildFleetTooltip(fleet));
        }
    }

    private void BuildResearchTab()
    {
        _researchContent = new ResearchTabContent { Name = "ResearchContent" };
        _researchContent.Configure(Query);
        _listContainer.AddChild(_researchContent);
    }

    private void BuildDesignsList()
    {
        var player = GameManager.Instance?.LocalPlayerEmpire;
        if (player == null) { BuildPlaceholder("BUILD"); return; }

        _listContainer.AddChild(BuildNewDesignRow());
        if (player.DesignState.Designs.Count == 0)
        {
            BuildEmptyMessage("No saved designs yet.");
            return;
        }
        foreach (var design in player.DesignState.Designs)
            _listContainer.AddChild(BuildDesignCard(design));
    }

    private static Control BuildNewDesignRow()
    {
        var outer = new MarginContainer();
        outer.AddThemeConstantOverride("margin_left", 8);
        outer.AddThemeConstantOverride("margin_right", 8);
        outer.AddThemeConstantOverride("margin_top", 8);
        outer.AddThemeConstantOverride("margin_bottom", 4);

        var btn = new Button { Text = "+  NEW DESIGN" };
        btn.CustomMinimumSize = new Vector2(0, 36);
        UIFonts.StyleButtonRole(btn, UIFonts.Role.UILabel, UIColors.Accent);
        GlassPanel.StyleButton(btn);
        btn.Pressed += () => EventBus.Instance?.FireDesignerOpenRequested(new DesignerOpenRequest());
        outer.AddChild(btn);
        return outer;
    }

    private static Control BuildDesignCard(ShipDesign design)
    {
        var outer = new MarginContainer();
        outer.AddThemeConstantOverride("margin_left", 8);
        outer.AddThemeConstantOverride("margin_right", 8);
        outer.AddThemeConstantOverride("margin_top", 3);
        outer.AddThemeConstantOverride("margin_bottom", 3);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(0, 72);
        var bg = new StyleBoxFlat
        {
            BgColor = new Color(14 / 255f, 20 / 255f, 40 / 255f, 0.92f),
            BorderColor = UIColors.BorderMid,
        };
        bg.SetBorderWidthAll(1);
        bg.SetCornerRadiusAll(4);
        bg.ContentMarginLeft = 10; bg.ContentMarginRight = 10;
        bg.ContentMarginTop = 8;   bg.ContentMarginBottom = 8;
        panel.AddThemeStyleboxOverride("panel", bg);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 4);
        panel.AddChild(col);

        var chassis = design.GetChassis();
        var title = new Label { Text = design.Name.ToUpperInvariant() };
        UIFonts.StyleRole(title, UIFonts.Role.TitleMedium);
        title.ClipText = true;
        col.AddChild(title);

        var info = new Label
        {
            Text = chassis != null
                ? $"{chassis.DisplayName.ToUpperInvariant()} \u00B7 {design.SlotFills.Count(s => !string.IsNullOrEmpty(s))}/{chassis.BigSystemSlots}"
                : "Unknown chassis"
        };
        UIFonts.StyleRole(info, UIFonts.Role.DataSmall);
        col.AddChild(info);

        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", 6);
        col.AddChild(actionRow);

        var editBtn = new Button { Text = "EDIT" };
        editBtn.CustomMinimumSize = new Vector2(72, 26);
        UIFonts.StyleButtonRole(editBtn, UIFonts.Role.UILabel);
        GlassPanel.StyleButton(editBtn);
        var capturedId = design.Id;
        editBtn.Pressed += () => EventBus.Instance?.FireDesignerOpenRequested(new DesignerOpenRequest { DesignId = capturedId });
        actionRow.AddChild(editBtn);

        var buildBtn = new Button { Text = "BUILD", Disabled = true, TooltipText = "BUILD queue — Phase F" };
        buildBtn.CustomMinimumSize = new Vector2(72, 26);
        UIFonts.StyleButtonRole(buildBtn, UIFonts.Role.UILabel);
        GlassPanel.StyleButton(buildBtn);
        actionRow.AddChild(buildBtn);

        actionRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        outer.AddChild(panel);
        return outer;
    }

    private void BuildEmptyMessage(string text)
    {
        var label = new Label { Text = text };
        UIFonts.Style(label, UIFonts.Main, UIFonts.SmallSize, UIColors.TextFaint);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddChild(label);
        _listContainer.AddChild(margin);
    }

    private void BuildPlaceholder(string tabName) => BuildEmptyMessage($"{tabName}\n(Coming soon)");

    private void OnFleetSelected(int fleetId)
    {
        _selectedFleetIds.Clear();
        _selectedFleetIds.Add(fleetId);
        RebuildList();
    }

    private void OnFleetSelectionToggled(int fleetId)
    {
        if (!_selectedFleetIds.Add(fleetId)) _selectedFleetIds.Remove(fleetId);
        RebuildList();
    }

    private void OnFleetDeselected()
    {
        _selectedFleetIds.Clear();
        RebuildList();
    }

    private (string text, Color color) GetFleetStatus(FleetData fleet)
    {
        var moveOrder = Query.GetFleetOrder(fleet.Id);
        if (moveOrder != null && !moveOrder.IsComplete)
            return ("EN ROUTE", UIColors.Moving);

        var (scans, extracts) = Query.GetFleetContributions(fleet.Id);
        if (extracts.Count > 0) return ("ENGAGED", UIColors.GreenGlow);
        if (scans.Count > 0)    return ("ENGAGED", UIColors.Accent);
        return ("IDLE", UIColors.TextDim);
    }

    private string BuildFleetTooltip(FleetData fleet)
    {
        var (scans, extracts) = Query.GetFleetContributions(fleet.Id);
        if (scans.Count == 0 && extracts.Count == 0) return "";

        var galaxy = Query.Galaxy;
        string NameFor(int poiId)
        {
            if (galaxy == null) return $"POI {poiId}";
            foreach (var s in galaxy.Systems)
                foreach (var p in s.POIs)
                    if (p.Id == poiId) return p.Name;
            return $"POI {poiId}";
        }
        var lines = new List<string>();
        if (scans.Count > 0)    lines.Add("Scanning: "   + string.Join(", ", scans.Select(NameFor)));
        if (extracts.Count > 0) lines.Add("Extracting: " + string.Join(", ", extracts.Select(NameFor)));
        return string.Join("\n", lines);
    }

    private static void StyleTab(Button tab, bool active)
    {
        var style = new StyleBoxFlat();
        style.SetCornerRadiusAll(0);
        style.SetBorderWidthAll(0);
        if (active)
        {
            style.BgColor = new Color(34 / 255f, 136 / 255f, 238 / 255f, 0.10f);
            style.BorderWidthBottom = 2;
            style.BorderColor = UIColors.Accent;
            tab.AddThemeColorOverride("font_color", UIColors.Accent);
        }
        else
        {
            style.BgColor = Colors.Transparent;
            tab.AddThemeColorOverride("font_color", UIColors.TextBody);
        }
        tab.AddThemeStyleboxOverride("normal", style);
        var hover = new StyleBoxFlat();
        hover.BgColor = new Color(34 / 255f, 136 / 255f, 238 / 255f, 0.08f);
        hover.SetBorderWidthAll(0); hover.SetCornerRadiusAll(0);
        tab.AddThemeStyleboxOverride("hover", hover);
        tab.AddThemeStyleboxOverride("pressed", style);
        tab.AddThemeStyleboxOverride("focus", style);
    }
}
