using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Settlements;
using DerlictEmpires.Core.Stations;
using DerlictEmpires.Core.Systems;
using DerlictEmpires.Core.Visibility;

namespace DerlictEmpires.Nodes.UI.SystemView;

/// <summary>
/// Full-screen scene replacement for a single star system. Covers the galaxy map and
/// the galaxy-scoped side panels; TopBar stays visible above. Parented to MainScene's
/// UILayer, positioned below the TopBar.
/// Spec: design/in_system_design.md (System View).
/// </summary>
public partial class SystemViewScene : Control
{
    private const float FadeSeconds = 0.12f;

    public StarSystemData? System { get; private set; }

    private Label? _systemNameLabel;
    private Label? _contextSummaryLabel;
    private BandRow? _innerBand;
    private BandRow? _midBand;
    private BandRow? _outerBand;
    private RightPanelController? _rightPanel;
    private Label? _youSigLabel;
    private Label? _youSensorLabel;

    // Data providers supplied by MainScene. Captured once per Open() call.
    private IReadOnlyList<Colony>?      _colonies;
    private IReadOnlyList<Outpost>?     _outposts;
    private IReadOnlyList<StationData>? _stations;
    private IReadOnlyList<Station>?     _stationsRuntime;
    private IReadOnlyList<FleetData>?   _fleets;
    private GalaxyData?                 _galaxy;
    private int                         _viewerEmpireId = -1;
    private int                        _selectedPoiId = -1;
    private string?                    _selectedEntityKind;

    public override void _Ready()
    {
        // Anchors: fill the viewport below the TopBar.
        AnchorLeft = 0; AnchorRight = 1; AnchorTop = 0; AnchorBottom = 1;
        OffsetLeft = 0; OffsetRight = 0;
        OffsetTop = TopBar.BarHeight;
        OffsetBottom = 0;
        ZIndex = 400;
        MouseFilter = MouseFilterEnum.Stop; // block clicks to layers below

        // Opaque background so galaxy + side panels don't bleed through.
        var bg = new ColorRect { Name = "Bg", Color = UIColors.BgDeep };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        bg.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(bg);

        var root = new VBoxContainer { Name = "Root" };
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 0);
        AddChild(root);

        BuildBreadcrumb(root);
        BuildMainGrid(root);

        if (EventBus.Instance != null)
        {
            EventBus.Instance.POISelected     += OnPOISelected;
            EventBus.Instance.EntitySelected  += OnEntitySelectedTracked;
            EventBus.Instance.EntityDeselected += OnEntityDeselectedTracked;
        }

        // Fade in.
        Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 1.0f, FadeSeconds);
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance == null) return;
        EventBus.Instance.POISelected     -= OnPOISelected;
        EventBus.Instance.EntitySelected  -= OnEntitySelectedTracked;
        EventBus.Instance.EntityDeselected -= OnEntityDeselectedTracked;
    }

    public void Open(StarSystemData system)
    {
        System = system;
        if (_systemNameLabel != null) _systemNameLabel.Text = system.Name;
        UpdateContextSummary();
        UpdateYouChip();
        RepopulateBands();
        UpdateBandCoverage();
        _rightPanel?.SetContext(system, _colonies, _outposts, _stations, _stationsRuntime, _galaxy, _viewerEmpireId);
    }

    private void UpdateBandCoverage()
    {
        if (System == null) return;
        var cov = SensorCoverageCalculator.Compute(_viewerEmpireId, System.Id, _stationsRuntime, _colonies);
        _innerBand?.SetCoverage(cov[Band.Inner].Coverage, cov[Band.Inner].SoleSource);
        _midBand  ?.SetCoverage(cov[Band.Mid]  .Coverage, cov[Band.Mid]  .SoleSource);
        _outerBand?.SetCoverage(cov[Band.Outer].Coverage, cov[Band.Outer].SoleSource);
    }

    /// <summary>Roll up player-owned sig/sensor across everything in this system.</summary>
    private void UpdateYouChip()
    {
        if (System == null) return;
        int sig = 0, sensor = 0;
        if (_colonies != null)
            foreach (var c in _colonies)
                if (c.SystemId == System.Id && c.OwnerEmpireId == _viewerEmpireId)
                    sig += c.TotalPopulation * 6;
        if (_outposts != null)
            foreach (var o in _outposts)
                if (o.SystemId == System.Id && o.OwnerEmpireId == _viewerEmpireId)
                    sig += o.TotalPopulation * 3;
        if (_stationsRuntime != null)
        {
            foreach (var s in _stationsRuntime)
            {
                if (s.SystemId != System.Id || s.OwnerEmpireId != _viewerEmpireId) continue;
                sig += s.SizeTier * 15 + s.Modules.Count * 2;
                foreach (var m in s.Modules)
                    if (m is Core.Stations.SensorModule sm)
                        sensor += (int)sm.SensorPower;
            }
        }
        if (_fleets != null)
            foreach (var f in _fleets)
                if (f.CurrentSystemId == System.Id && f.OwnerEmpireId == _viewerEmpireId)
                    sig += f.ShipIds.Count * 4;
        if (_youSigLabel   != null) _youSigLabel.Text    = sig.ToString();
        if (_youSensorLabel != null) _youSensorLabel.Text = sensor.ToString();
    }

    /// <summary>Supply the live game state needed to resolve POI contents and accents.</summary>
    public void SetContext(
        IReadOnlyList<Colony>? colonies,
        IReadOnlyList<Outpost>? outposts,
        IReadOnlyList<StationData>? stations,
        IReadOnlyList<Station>? stationsRuntime,
        IReadOnlyList<FleetData>? fleets,
        GalaxyData? galaxy,
        int viewerEmpireId)
    {
        _colonies = colonies;
        _outposts = outposts;
        _stations = stations;
        _stationsRuntime = stationsRuntime;
        _fleets   = fleets;
        _galaxy   = galaxy;
        _viewerEmpireId = viewerEmpireId;
    }

    private void RepopulateBands()
    {
        if (System == null || _innerBand == null || _midBand == null || _outerBand == null) return;

        var pois = System.POIs ?? new List<POIData>();
        var coverage = SensorCoverageCalculator.Compute(_viewerEmpireId, System.Id, _stationsRuntime, _colonies);

        IReadOnlyList<POIEntity> EntitiesFor(POIData p)
        {
            var entities = POIContentResolver.GetEntitiesAt(
                System.Id, p.Id, _colonies, _outposts, _stations, _fleets, _galaxy);
            // Decorate each foreign entity with a resolution tier based on band coverage.
            int bandCoverage = coverage.TryGetValue(p.Band, out var bc) ? bc.Coverage : 0;
            foreach (var e in entities)
            {
                if (e.OwnerEmpireId == _viewerEmpireId || e.OwnerEmpireId < 0)
                    e.Resolution = ResolutionTier.Id;
                else if (bandCoverage >= 50)
                    e.Resolution = ResolutionTier.Id;
                else if (bandCoverage >= 20)
                    e.Resolution = ResolutionTier.Type;
                else
                    e.Resolution = ResolutionTier.Silhouette;
            }
            return entities;
        }

        _innerBand.Populate(pois.Where(p => p.Band == Band.Inner).ToList(), EntitiesFor, _viewerEmpireId);
        _midBand  .Populate(pois.Where(p => p.Band == Band.Mid  ).ToList(), EntitiesFor, _viewerEmpireId);
        _outerBand.Populate(pois.Where(p => p.Band == Band.Outer).ToList(), EntitiesFor, _viewerEmpireId);

        // Unmoored fleets drift in the Mid band per spec §4.5. Only player-owned v1 — foreign
        // drifters appear once resolution tier wiring covers fleets.
        if (_fleets != null && System != null)
        {
            foreach (var f in _fleets)
            {
                if (f.CurrentSystemId != System.Id) continue;
                if (f.MooredPOIId.HasValue) continue;
                _midBand.AppendCard(new FleetPOICard(f, _viewerEmpireId));
            }
        }
    }

    private void OnPOISelected(int poiId)
    {
        _selectedPoiId = poiId;
        _innerBand?.SetSelection(poiId);
        _midBand  ?.SetSelection(poiId);
        _outerBand?.SetSelection(poiId);
    }

    private void OnEntitySelectedTracked(string kind, int entityId, int poiId)
    {
        _selectedEntityKind = kind;
    }

    private void OnEntityDeselectedTracked()
    {
        _selectedEntityKind = null;
    }

    public void Close()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.POISelected     -= OnPOISelected;
            EventBus.Instance.EntitySelected  -= OnEntitySelectedTracked;
            EventBus.Instance.EntityDeselected -= OnEntityDeselectedTracked;
        }
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, FadeSeconds);
        tween.Finished += QueueFree;
        EventBus.Instance?.FireSystemViewClosed();
    }

    private void BuildBreadcrumb(VBoxContainer parent)
    {
        var row = new PanelContainer { Name = "Breadcrumb", CustomMinimumSize = new Vector2(0, 32) };
        var bgStyle = new StyleBoxFlat
        {
            BgColor = UIColors.GlassDark,
            BorderColor = new Color(60 / 255f, 110 / 255f, 160 / 255f, 0.3f),
            BorderWidthBottom = 1,
        };
        row.AddThemeStyleboxOverride("panel", bgStyle);
        parent.AddChild(row);

        var h = new HBoxContainer();
        h.AddThemeConstantOverride("separation", 12);
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 0);
        margin.AddThemeConstantOverride("margin_bottom", 0);
        margin.AddChild(h);
        row.AddChild(margin);

        var back = new Button { Text = "← GALAXY", Flat = true };
        UIFonts.StyleButtonRole(back, UIFonts.Role.Small, UIColors.Accent);
        back.Pressed += OnBackPressed;
        h.AddChild(back);

        var div1 = MakeLabel("/", UIFonts.Role.Small, UIColors.TextFaint);
        h.AddChild(div1);

        _systemNameLabel = MakeLabel("—", UIFonts.Role.Title, UIColors.TextBright);
        _systemNameLabel.AddThemeFontSizeOverride("font_size", 13);
        h.AddChild(_systemNameLabel);

        var div2 = MakeLabel("/", UIFonts.Role.Small, UIColors.TextFaint);
        h.AddChild(div2);

        _contextSummaryLabel = MakeLabel("0 POI · 0 SHARED · 0 CONTACTS", UIFonts.Role.Small, UIColors.TextDim);
        h.AddChild(_contextSummaryLabel);

        // Spacer
        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        h.AddChild(spacer);

        // YOU chip — live aggregate of player sig + sensor in current system.
        var youChip = new HBoxContainer();
        youChip.AddThemeConstantOverride("separation", 6);
        var youLabel = MakeLabel("YOU", UIFonts.Role.Small, UIColors.TextDim);
        youChip.AddChild(youLabel);

        var sigHbox = new HBoxContainer();
        sigHbox.AddThemeConstantOverride("separation", 4);
        sigHbox.AddChild(new DetectionGlyph(DetectionGlyph.Kind.Signature, 11));
        _youSigLabel = new Label { Text = "0" };
        UIFonts.Style(_youSigLabel, UIFonts.Main, UIFonts.SmallSize, UIColors.SigIcon);
        sigHbox.AddChild(_youSigLabel);
        youChip.AddChild(sigHbox);

        var sensorHbox = new HBoxContainer();
        sensorHbox.AddThemeConstantOverride("separation", 4);
        sensorHbox.AddChild(new DetectionGlyph(DetectionGlyph.Kind.Sensor, 11));
        _youSensorLabel = new Label { Text = "0" };
        UIFonts.Style(_youSensorLabel, UIFonts.Main, UIFonts.SmallSize, UIColors.SensorIcon);
        sensorHbox.AddChild(_youSensorLabel);
        youChip.AddChild(sensorHbox);

        h.AddChild(youChip);

        // Explicit close button so users have a visible affordance beyond ← GALAXY.
        var closeButton = new Button { Text = "✕", Flat = true };
        UIFonts.StyleButtonRole(closeButton, UIFonts.Role.Small, UIColors.AccentRed);
        closeButton.TooltipText = "Close System View (Esc)";
        closeButton.Pressed += OnBackPressed;
        h.AddChild(closeButton);
    }

    private void BuildMainGrid(VBoxContainer parent)
    {
        var grid = new HBoxContainer { Name = "MainGrid", SizeFlagsVertical = SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("separation", 0);
        parent.AddChild(grid);

        var bandsCol = new VBoxContainer { Name = "BandsColumn" };
        bandsCol.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        bandsCol.SizeFlagsVertical   = SizeFlags.ExpandFill;
        bandsCol.SizeFlagsStretchRatio = 3;
        bandsCol.AddThemeConstantOverride("separation", 0);
        grid.AddChild(bandsCol);

        _innerBand = new BandRow(Band.Inner);
        _midBand   = new BandRow(Band.Mid);
        _outerBand = new BandRow(Band.Outer);
        bandsCol.AddChild(_innerBand);
        bandsCol.AddChild(_midBand);
        bandsCol.AddChild(_outerBand);

        // Right panel slot — hosts the RightPanelController which swaps between empty-state and
        // per-entity variants on EntitySelected / EntityDeselected.
        var rightSlot = new PanelContainer { Name = "RightPanelSlot" };
        rightSlot.SizeFlagsHorizontal   = SizeFlags.ExpandFill;
        rightSlot.SizeFlagsVertical     = SizeFlags.ExpandFill;
        rightSlot.SizeFlagsStretchRatio = 1;
        rightSlot.CustomMinimumSize     = new Vector2(240, 0);
        GlassPanel.Apply(rightSlot, enableBlur: false);
        grid.AddChild(rightSlot);

        _rightPanel = new RightPanelController();
        rightSlot.AddChild(_rightPanel);
    }

    private void UpdateContextSummary()
    {
        if (System == null || _contextSummaryLabel == null) return;
        int poi = System.POIs?.Count ?? 0;

        int shared = 0;
        var contactEmpires = new HashSet<int>();
        if (System.POIs != null)
        {
            foreach (var p in System.POIs)
            {
                var entities = POIContentResolver.GetEntitiesAt(
                    System.Id, p.Id, _colonies, _outposts, _stations, _fleets, _galaxy);
                if (entities.Count > 1) shared++;
                foreach (var e in entities)
                    if (e.OwnerEmpireId >= 0 && e.OwnerEmpireId != _viewerEmpireId)
                        contactEmpires.Add(e.OwnerEmpireId);
            }
        }

        _contextSummaryLabel.Text = $"{poi} POI · {shared} SHARED · {contactEmpires.Count} CONTACTS";
    }

    private static Label MakeLabel(string text, UIFonts.Role role, Color color)
    {
        var l = new Label { Text = text };
        UIFonts.StyleRole(l, role, color);
        return l;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey k || !k.Pressed || k.Echo) return;

        if (k.Keycode == Key.Escape)
        {
            HandleEscCascade();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (k.Keycode == Key.Tab)
        {
            CycleEntity(reverse: k.ShiftPressed);
            GetViewport().SetInputAsHandled();
            return;
        }

        // 1..9: jump Selected POI by reading order (Inner → Mid → Outer, left→right).
        int? digit = k.Keycode switch
        {
            Key.Key1 => 0, Key.Key2 => 1, Key.Key3 => 2, Key.Key4 => 3, Key.Key5 => 4,
            Key.Key6 => 5, Key.Key7 => 6, Key.Key8 => 7, Key.Key9 => 8,
            _ => (int?)null,
        };
        if (digit.HasValue)
        {
            JumpToPoiByIndex(digit.Value);
            GetViewport().SetInputAsHandled();
        }
    }

    private void CycleEntity(bool reverse)
    {
        if (System == null || _selectedPoiId < 0) return;
        var entities = POIContentResolver.GetEntitiesAt(
            System.Id, _selectedPoiId, _colonies, _outposts, _stations, _fleets, _galaxy);
        if (entities.Count == 0) return;

        int currentIdx = -1;
        for (int i = 0; i < entities.Count; i++)
        {
            if (_selectedEntityKind == entities[i].Kind.ToString()) { currentIdx = i; break; }
        }
        int nextIdx = currentIdx < 0
            ? 0
            : (reverse ? (currentIdx - 1 + entities.Count) % entities.Count
                       : (currentIdx + 1) % entities.Count);
        var pick = entities[nextIdx];
        EventBus.Instance?.FireEntitySelected(pick.Kind.ToString(), pick.Id, _selectedPoiId);
    }

    private void JumpToPoiByIndex(int index)
    {
        if (System?.POIs == null) return;
        var ordered = System.POIs
            .OrderBy(p => p.Band == Band.Inner ? 0 : p.Band == Band.Mid ? 1 : 2)
            .ThenBy(p => p.Id)
            .ToList();
        if (index < 0 || index >= ordered.Count) return;
        EventBus.Instance?.FirePOISelected(ordered[index].Id);
    }

    /// <summary>Esc cascade per design/in_system_design.md §11.3:
    /// entity cleared → POI cleared → scene exits. (Focused building row lands in P5.)</summary>
    private void HandleEscCascade()
    {
        if (_selectedEntityKind != null)
        {
            _selectedEntityKind = null;
            EventBus.Instance?.FireEntityDeselected();
            return;
        }
        if (_selectedPoiId >= 0)
        {
            _selectedPoiId = -1;
            _innerBand?.SetSelection(-1);
            _midBand  ?.SetSelection(-1);
            _outerBand?.SetSelection(-1);
            EventBus.Instance?.FirePOIDeselected();
            return;
        }
        Close();
    }

    private void OnBackPressed() => Close();
}
