using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Settlements;
using DerlictEmpires.Core.Systems;

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

    // Data providers supplied by MainScene. Captured once per Open() call.
    private IReadOnlyList<Colony>?     _colonies;
    private IReadOnlyList<Outpost>?    _outposts;
    private IReadOnlyList<StationData>? _stations;
    private IReadOnlyList<FleetData>?  _fleets;
    private GalaxyData?                _galaxy;
    private int                        _viewerEmpireId = -1;
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

    public void Open(StarSystemData system)
    {
        System = system;
        if (_systemNameLabel != null) _systemNameLabel.Text = system.Name;
        UpdateContextSummary();
        RepopulateBands();
        _rightPanel?.SetContext(system, _colonies, _outposts, _stations, _viewerEmpireId);
    }

    /// <summary>Supply the live game state needed to resolve POI contents and accents.</summary>
    public void SetContext(
        IReadOnlyList<Colony>? colonies,
        IReadOnlyList<Outpost>? outposts,
        IReadOnlyList<StationData>? stations,
        IReadOnlyList<FleetData>? fleets,
        GalaxyData? galaxy,
        int viewerEmpireId)
    {
        _colonies = colonies;
        _outposts = outposts;
        _stations = stations;
        _fleets   = fleets;
        _galaxy   = galaxy;
        _viewerEmpireId = viewerEmpireId;
    }

    private void RepopulateBands()
    {
        if (System == null || _innerBand == null || _midBand == null || _outerBand == null) return;

        var pois = System.POIs ?? new List<POIData>();

        POIEntity? PrimaryFor(POIData p)
        {
            var entities = POIContentResolver.GetEntitiesAt(
                System.Id, p.Id, _colonies, _outposts, _stations, _fleets, _galaxy);
            return POIContentResolver.Primary(entities, _viewerEmpireId);
        }

        _innerBand.Populate(pois.Where(p => p.Band == Band.Inner).ToList(), PrimaryFor);
        _midBand  .Populate(pois.Where(p => p.Band == Band.Mid  ).ToList(), PrimaryFor);
        _outerBand.Populate(pois.Where(p => p.Band == Band.Outer).ToList(), PrimaryFor);
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

        // YOU chip — placeholder values until P5.
        var youChip = new HBoxContainer();
        youChip.AddThemeConstantOverride("separation", 6);
        var youLabel = MakeLabel("YOU", UIFonts.Role.Small, UIColors.TextDim);
        youChip.AddChild(youLabel);
        youChip.AddChild(DetectionGlyph.CreateLabel(DetectionGlyph.Kind.Signature, 11, "0"));
        youChip.AddChild(DetectionGlyph.CreateLabel(DetectionGlyph.Kind.Sensor,    11, "0"));
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
        _contextSummaryLabel.Text = $"{poi} POI · 0 SHARED · 0 CONTACTS";
    }

    private static Label MakeLabel(string text, UIFonts.Role role, Color color)
    {
        var l = new Label { Text = text };
        UIFonts.StyleRole(l, role, color);
        return l;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey k && k.Pressed && !k.Echo && k.Keycode == Key.Escape)
        {
            HandleEscCascade();
            GetViewport().SetInputAsHandled();
        }
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
