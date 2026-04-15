using Godot;
using System.Collections.Generic;
using DerlictEmpires.Autoloads;

namespace DerlictEmpires.Nodes.UI;

/// <summary>
/// Bottom-right event log panel showing recent game events.
/// 306px wide × 160px tall, tarnished glass.
/// </summary>
public partial class EventLog : Control
{
    private VBoxContainer _entryList = null!;
    private readonly List<EventEntry> _events = new();
    private const int MaxEvents = 20;

    public override void _Ready()
    {
        // Anchors: bottom-right, 306px wide, 160px tall, above speed widget
        AnchorLeft = 1;
        AnchorRight = 1;
        AnchorTop = 1;
        AnchorBottom = 1;
        OffsetLeft = -RightPanel.PanelWidth;
        OffsetRight = 0;
        OffsetTop = -222;
        OffsetBottom = -62;
        ClipContents = true;
        ZIndex = 50;

        // Background with tarnished glass
        var bg = new PanelContainer { Name = "Bg" };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        GlassPanel.Apply(bg, enableBlur: true);
        AddChild(bg);

        // Top edge highlight
        var topEdge = new ColorRect { Name = "TopEdge" };
        topEdge.Color = new Color(80 / 255f, 140 / 255f, 220 / 255f, 0.25f);
        topEdge.AnchorLeft = 0;
        topEdge.AnchorRight = 1;
        topEdge.AnchorTop = 0;
        topEdge.AnchorBottom = 0;
        topEdge.OffsetTop = 0;
        topEdge.OffsetBottom = 1;
        topEdge.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(topEdge);

        // Left edge highlight (panel is on right side)
        var leftEdge = new ColorRect { Name = "LeftEdge" };
        leftEdge.Color = new Color(80 / 255f, 140 / 255f, 220 / 255f, 0.18f);
        leftEdge.AnchorLeft = 0;
        leftEdge.AnchorRight = 0;
        leftEdge.AnchorTop = 0;
        leftEdge.AnchorBottom = 1;
        leftEdge.OffsetLeft = 0;
        leftEdge.OffsetRight = 1;
        leftEdge.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(leftEdge);

        // Main layout
        var layout = new VBoxContainer { Name = "Layout" };
        layout.SetAnchorsPreset(LayoutPreset.FullRect);
        layout.AddThemeConstantOverride("separation", 0);
        AddChild(layout);

        // Header
        var headerMargin = new MarginContainer();
        headerMargin.AddThemeConstantOverride("margin_left", 12);
        headerMargin.AddThemeConstantOverride("margin_right", 12);
        headerMargin.AddThemeConstantOverride("margin_top", 8);
        headerMargin.AddThemeConstantOverride("margin_bottom", 4);
        layout.AddChild(headerMargin);

        var headerVBox = new VBoxContainer();
        headerVBox.AddThemeConstantOverride("separation", 4);
        headerMargin.AddChild(headerVBox);

        var title = new Label { Text = "RECENT EVENTS" };
        UIFonts.Style(title, UIFonts.BarlowSemiBold, 9, UIColors.TextLabel);
        headerVBox.AddChild(title);

        // Cyan accent line (40px wide, per spec §8.4)
        var accent = new ColorRect();
        accent.CustomMinimumSize = new Vector2(40, 1);
        accent.Color = new Color(UIColors.Accent, 0.6f);
        headerVBox.AddChild(accent);

        // Divider
        var div = new ColorRect();
        div.CustomMinimumSize = new Vector2(0, 1);
        div.Color = UIColors.BorderDim;
        layout.AddChild(div);

        // Scrollable event list
        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        layout.AddChild(scroll);

        var listMargin = new MarginContainer();
        listMargin.AddThemeConstantOverride("margin_top", 4);
        listMargin.AddThemeConstantOverride("margin_bottom", 4);
        listMargin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(listMargin);

        _entryList = new VBoxContainer { Name = "EntryList" };
        _entryList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _entryList.AddThemeConstantOverride("separation", 6);
        listMargin.AddChild(_entryList);

        // Add placeholder events
        AddEvent("Game started", EventCategory.Info);

        // Subscribe to game events
        if (EventBus.Instance != null)
        {
            EventBus.Instance.FleetArrivedAtSystem += OnFleetArrived;
            EventBus.Instance.SubsystemResearched += OnResearchComplete;
            EventBus.Instance.StationModuleInstalled += OnModuleInstalled;
        }
    }

    private void OnFleetArrived(int fleetId, int systemId)
    {
        var galaxy = GameManager.Instance?.Galaxy;
        var sysName = galaxy?.GetSystem(systemId)?.Name ?? $"System {systemId}";
        AddEvent($"Fleet arrived at {sysName}", EventCategory.Movement);
    }

    private void OnResearchComplete(int empireId, string subId)
    {
        var playerEmpire = GameManager.Instance?.LocalPlayerEmpire;
        if (playerEmpire != null && empireId == playerEmpire.Id)
            AddEvent($"Research completed: {subId}", EventCategory.Research);
    }

    private void OnModuleInstalled(int stationId, int empireId)
    {
        var playerEmpire = GameManager.Instance?.LocalPlayerEmpire;
        if (playerEmpire != null && empireId == playerEmpire.Id)
            AddEvent($"Station module installed", EventCategory.Build);
    }

    /// <summary>Add a new event to the log.</summary>
    public void AddEvent(string text, EventCategory category)
    {
        _events.Insert(0, new EventEntry(text, category));
        if (_events.Count > MaxEvents)
            _events.RemoveAt(_events.Count - 1);

        RebuildList();
    }

    private void RebuildList()
    {
        foreach (var child in _entryList.GetChildren())
            child.QueueFree();

        foreach (var evt in _events)
        {
            var row = BuildEventRow(evt);
            _entryList.AddChild(row);
        }
    }

    private static Control BuildEventRow(EventEntry evt)
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(hbox);

        // Colored indicator dot (8px) per spec §5.8
        var dot = new EventDot(GetCategoryColor(evt.Category));
        dot.CustomMinimumSize = new Vector2(8, 8);
        dot.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        hbox.AddChild(dot);

        // Event text
        var text = new Label { Text = evt.Text };
        UIFonts.Style(text, UIFonts.ShareTechMono, 10, UIColors.TextBody);
        text.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        text.ClipText = true;
        hbox.AddChild(text);

        return margin;
    }

    private static Color GetCategoryColor(EventCategory category) => category switch
    {
        EventCategory.Combat => UIColors.Alert,
        EventCategory.Movement => UIColors.Moving,
        EventCategory.Research => new Color("#b366e8"),
        EventCategory.Build => UIColors.Accent,
        EventCategory.Info => UIColors.TextDim,
        _ => UIColors.TextDim
    };

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.FleetArrivedAtSystem -= OnFleetArrived;
            EventBus.Instance.SubsystemResearched -= OnResearchComplete;
            EventBus.Instance.StationModuleInstalled -= OnModuleInstalled;
        }
    }

    private record EventEntry(string Text, EventCategory Category);
}

/// <summary>Draws a colored circle with glow per spec §5.8.</summary>
public partial class EventDot : Control
{
    private readonly Color _color;

    public EventDot(Color color) => _color = color;

    public override void _Draw()
    {
        var center = Size / 2f;
        float radius = Mathf.Min(Size.X, Size.Y) / 2f;
        // Glow halo
        DrawCircle(center, radius + 2f, new Color(_color, 0.2f));
        // Main dot
        DrawCircle(center, radius, _color);
    }
}

public enum EventCategory
{
    Info,
    Combat,
    Movement,
    Research,
    Build
}
