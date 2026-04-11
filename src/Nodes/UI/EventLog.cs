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
        // Anchors: bottom-right, 306px wide, 160px tall
        AnchorLeft = 1;
        AnchorRight = 1;
        AnchorTop = 1;
        AnchorBottom = 1;
        OffsetLeft = -RightPanel.PanelWidth;
        OffsetRight = 0;
        OffsetTop = -160;
        OffsetBottom = 0;
        ClipContents = true;
        ZIndex = 50;

        // Background with tarnished glass
        var bg = new PanelContainer { Name = "Bg" };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        GlassPanel.Apply(bg, enableBlur: true);
        AddChild(bg);

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

        // Cyan accent line (40px wide, per spec)
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

        _entryList = new VBoxContainer { Name = "EntryList" };
        _entryList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _entryList.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(_entryList);

        // Add some placeholder events on ready
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

        // Colored indicator dot (8px)
        var dot = new ColorRect();
        dot.CustomMinimumSize = new Vector2(8, 8);
        dot.Color = GetCategoryColor(evt.Category);
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
        EventCategory.Research => new Color("#9944dd"),
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

public enum EventCategory
{
    Info,
    Combat,
    Movement,
    Research,
    Build
}
