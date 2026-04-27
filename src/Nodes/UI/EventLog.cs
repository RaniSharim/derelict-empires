using Godot;
using System.Collections.Generic;
using DerlictEmpires.Autoloads;

namespace DerlictEmpires.Nodes.UI;

public enum EventCategory
{
    Info,
    Combat,
    Movement,
    Research,
    Build,
}

/// <summary>
/// Bottom-right "Recent Events" feed. Layout in <c>scenes/ui/event_log.tscn</c>;
/// per-row layout in <c>scenes/ui/event_log_entry.tscn</c>. This script subscribes
/// to game events and instances entry rows, auto-trimming to <see cref="MaxEvents"/>.
/// </summary>
public partial class EventLog : Control
{
    private const int MaxEvents = 20;

    [Export] private PanelContainer _background = null!;
    [Export] private Label _title = null!;
    [Export] private VBoxContainer _entryList = null!;
    [Export] private PackedScene _entryScene = null!;

    private readonly List<(string text, EventCategory category)> _events = new();
    private int _lastDiscoverySystemId = -1;
    private int _discoveryStreakCount;

    public override void _Ready()
    {
        GlassPanel.Apply(_background, enableBlur: true);
        UIFonts.Style(_title, UIFonts.Main, UIFonts.SmallSize, UIColors.TextLabel);

        AddEvent("Game started", EventCategory.Info);

        if (EventBus.Instance != null)
        {
            EventBus.Instance.FleetArrivedAtSystem    += OnFleetArrived;
            EventBus.Instance.SubsystemResearched     += OnResearchComplete;
            EventBus.Instance.StationModuleInstalled  += OnModuleInstalled;
            EventBus.Instance.SiteDiscovered          += OnSiteDiscovered;
            EventBus.Instance.SiteScanComplete        += OnSiteScanComplete;
            EventBus.Instance.DesignSaved             += OnDesignSaved;
            EventBus.Instance.CombatStarted           += OnCombatStarted;
            EventBus.Instance.CombatEnded             += OnCombatEnded;
            EventBus.Instance.SiteResearchUnlocked    += OnSiteResearchUnlocked;
            EventBus.Instance.SiteDangerTriggered     += OnSiteDangerTriggered;
            EventBus.Instance.SiteSpecialOutcomeReady += OnSiteSpecialOutcomeReady;
        }
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.FleetArrivedAtSystem    -= OnFleetArrived;
            EventBus.Instance.SubsystemResearched     -= OnResearchComplete;
            EventBus.Instance.DesignSaved             -= OnDesignSaved;
            EventBus.Instance.CombatStarted           -= OnCombatStarted;
            EventBus.Instance.CombatEnded             -= OnCombatEnded;
            EventBus.Instance.StationModuleInstalled  -= OnModuleInstalled;
            EventBus.Instance.SiteDiscovered          -= OnSiteDiscovered;
            EventBus.Instance.SiteScanComplete        -= OnSiteScanComplete;
            EventBus.Instance.SiteResearchUnlocked    -= OnSiteResearchUnlocked;
            EventBus.Instance.SiteDangerTriggered     -= OnSiteDangerTriggered;
            EventBus.Instance.SiteSpecialOutcomeReady -= OnSiteSpecialOutcomeReady;
        }
    }

    public void AddEvent(string text, EventCategory category)
    {
        _events.Insert(0, (text, category));
        if (_events.Count > MaxEvents)
            _events.RemoveAt(_events.Count - 1);
        RebuildList();
    }

    private void RebuildList()
    {
        foreach (var child in _entryList.GetChildren())
            child.QueueFree();
        foreach (var (text, category) in _events)
        {
            var row = _entryScene.Instantiate<EventLogEntry>();
            _entryList.AddChild(row);
            row.Populate(text, category);
        }
    }

    private void OnDesignSaved(string designId)
    {
        var design = GameManager.Instance?.LocalPlayerEmpire?.DesignState.GetDesign(designId);
        if (design != null)
            AddEvent($"Design saved: {design.Name}", EventCategory.Build);
    }

    private void OnCombatStarted(int battleId) =>
        AddEvent($"Combat started \u2014 battle #{battleId}", EventCategory.Combat);

    private void OnCombatEnded(int battleId, Core.Combat.CombatResult result) =>
        AddEvent($"Battle #{battleId} {result.ToString().ToUpperInvariant()}", EventCategory.Combat);

    private void OnFleetArrived(int fleetId, int systemId)
    {
        var sysName = GameManager.Instance?.Galaxy?.GetSystem(systemId)?.Name ?? $"System {systemId}";
        AddEvent($"Fleet arrived at {sysName}", EventCategory.Movement);
        _lastDiscoverySystemId = systemId;
        _discoveryStreakCount = 0;
    }

    private void OnSiteDiscovered(int empireId, int poiId)
    {
        var player = GameManager.Instance?.LocalPlayerEmpire;
        if (player == null || empireId != player.Id) return;
        var galaxy = GameManager.Instance?.Galaxy;
        if (galaxy == null) return;

        foreach (var sys in galaxy.Systems)
        foreach (var poi in sys.POIs)
        {
            if (poi.Id != poiId) continue;
            _discoveryStreakCount++;
            string sysName = sys.Name.ToUpper();
            if (sys.Id == _lastDiscoverySystemId && _discoveryStreakCount > 1 && _events.Count > 0)
            {
                _events[0] = ($"{_discoveryStreakCount} SALVAGE SITES DISCOVERED \u00B7 {sysName}", EventCategory.Info);
                RebuildList();
            }
            else
            {
                _lastDiscoverySystemId = sys.Id;
                AddEvent($"SALVAGE SITE DISCOVERED \u00B7 {sysName}", EventCategory.Info);
            }
            return;
        }
    }

    private void OnSiteScanComplete(int empireId, int poiId)
    {
        var player = GameManager.Instance?.LocalPlayerEmpire;
        if (player == null || empireId != player.Id) return;
        var galaxy = GameManager.Instance?.Galaxy;
        string poiName = "site";
        if (galaxy != null)
        {
            foreach (var sys in galaxy.Systems)
            foreach (var poi in sys.POIs)
                if (poi.Id == poiId) { poiName = poi.Name; break; }
        }
        AddEvent($"SCAN COMPLETE \u00B7 {poiName}", EventCategory.Research);
    }

    private void OnResearchComplete(int empireId, string subId)
    {
        var player = GameManager.Instance?.LocalPlayerEmpire;
        if (player != null && empireId == player.Id)
            AddEvent($"Research completed: {subId}", EventCategory.Research);
    }

    private void OnModuleInstalled(int stationId, int empireId)
    {
        var player = GameManager.Instance?.LocalPlayerEmpire;
        if (player != null && empireId == player.Id)
            AddEvent($"Station module installed", EventCategory.Build);
    }

    private void OnSiteResearchUnlocked(int empireId, int poiId, int layerIndex)
    {
        var player = GameManager.Instance?.LocalPlayerEmpire;
        if (player == null || empireId != player.Id) return;
        AddEvent($"Research unlocked from salvage layer {layerIndex + 1}", EventCategory.Research);
    }

    private void OnSiteDangerTriggered(int empireId, int poiId, int layerIndex, string dangerTypeId, float severity)
    {
        var player = GameManager.Instance?.LocalPlayerEmpire;
        if (player == null || empireId != player.Id) return;
        AddEvent($"Salvage danger: {dangerTypeId} ({severity:F0})", EventCategory.Combat);
    }

    private void OnSiteSpecialOutcomeReady(int empireId, int poiId, string outcomeId)
    {
        var player = GameManager.Instance?.LocalPlayerEmpire;
        if (player == null || empireId != player.Id) return;
        string name = outcomeId switch
        {
            "repair_station"   => "Repair Station",
            "recover_derelict" => "Recover Derelict",
            _                  => outcomeId.Replace('_', ' '),
        };
        AddEvent($"SITE OUTCOME READY · {name}", EventCategory.Info);
    }
}
