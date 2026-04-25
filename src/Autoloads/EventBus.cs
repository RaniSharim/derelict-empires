using System;
using System.Collections.Generic;
using Godot;
using DerlictEmpires.Core.Combat;
using DerlictEmpires.Core.Enums;
using DerlictEmpires.Core.Models;
using DerlictEmpires.Core.Tech;

namespace DerlictEmpires.Autoloads;

/// <summary>
/// Global event bus for cross-tree communication.
/// Uses C# events (Action delegates) for compile-time safety.
/// Registered as an autoload — access via EventBus.Instance.
/// </summary>
public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; } = null!;

    // Events suppressed by default in the debug subscriber — per-tick chatter drowns signal.
    private const string DefaultDebugBlocklist = "FastTick,SlowTick,BattleTick,ScanProgressChanged";

    public override void _Ready()
    {
        Instance = this;
        GD.Print("[EventBus] Ready");
        AttachDebugSubscriberIfEnabled();
    }

    // === Galaxy & Map ===
    public event Action<StarSystemData>? SystemSelected;
    public event Action? SystemDeselected;
    public event Action<StarSystemData>? SystemHovered;
    public event Action? SystemUnhovered;
    public event Action<StarSystemData>? SystemDoubleClicked;   // opens System View

    // === Game Loop ===
    public event Action<float>? FastTick;   // Arg: tick delta in game-seconds
    public event Action<float>? SlowTick;   // Arg: tick delta in game-seconds
    public event Action<GameSpeed>? SpeedChanged;
    public event Action? GamePaused;
    public event Action? GameResumed;

    // === Fleet ===
    public event Action<int>? FleetSelected;               // fleet ID — replaces selection
    public event Action<int>? FleetSelectionToggled;       // fleet ID — ctrl-click, adds or removes
    public event Action<int>? FleetDoubleClicked;          // fleet ID — pan camera to fleet
    public event Action? FleetDeselected;
    public event Action<int, int>? FleetArrivedAtSystem;   // fleet ID, system ID

    // === Empire ===
    public event Action<int, PrecursorColor, ResourceType, float>? ResourceChanged; // empire, color, type, newAmt

    // === Research ===
    public event Action<int, string>? SubsystemResearched;   // empireId, subsystemId
    public event Action<int, string>? ResearchStarted;       // empireId, projectId
    public event Action<int, PrecursorColor, TechCategory, int>? TierUnlocked; // empireId, color, category, tier

    // === Stations ===
    public event Action<int, int>? StationModuleInstalled;   // stationId, empireId
    public event Action<int, string>? ShipProduced;          // empireId, shipName

    // === Salvage / Exploration ===
    public event Action<int, int>? SiteDiscovered;                        // empireId, poiId
    public event Action<int, int, float, float>? ScanProgressChanged;     // empireId, poiId, progress, difficulty
    public event Action<int, int>? SiteScanComplete;                      // empireId, poiId
    public event Action<int, int, string, float>? YieldExtracted;         // empireId, poiId, resourceKey, amount
    public event Action<int>? FleetOrderChanged;                          // fleetId
    public event Action<StarSystemData>? SystemRightClicked;              // for command issuance
    public event Action<int, int, SiteActivity>? SiteActivityChanged;     // empireId, poiId, newActivity
    public event Action<int, int>? SiteActivityRateChanged;               // empireId, poiId (UI recomputes)

    // === Salvage intent events (UI → SalvageActionHandler) ===
    // UI fires these; handler validates and forwards to GameSystems.Salvage.
    public event Action<int>? ScanToggleRequested;                        // poiId
    public event Action<int>? ExtractToggleRequested;                     // poiId

    // === Ship Designer ===
    public event Action<DesignerOpenRequest>? DesignerOpenRequested;
    public event Action<string>? DesignSaved;                             // designId
    public event Action<string>? FleetTemplateSaved;                      // templateId

    // === Tech Tree Overlay ===
    public event Action<TechTreeOpenRequest>? TechTreeOpenRequested;

    // === Combat ===
    public event Action<int, int>? CombatStartRequested;                  // attackerFleetId, defenderFleetId
    public event Action<int>? CombatStarted;                              // battleId
    public event Action<int, CombatResult>? CombatEnded;                  // battleId, result
    public event Action<int>? BattleTick;                                 // battleId (fires at 4 Hz during combat)

    // === System View ===
    public event Action<int>? SystemViewOpened;                           // systemId
    public event Action? SystemViewClosed;
    public event Action<int>? POISelected;                                // poiId (within current Selected System)
    public event Action? POIDeselected;
    public event Action<string, int, int>? EntitySelected;                // entityKind, entityId, poiId
    public event Action? EntityDeselected;
    public event Action<int, string, int, bool>? BuildingSlotToggled;     // colonyId, buildingId, slotIndex, newState (true=filled)
    public event Action<int>? ColonyPopsChanged;                          // colonyId

    // === Deferred screens (Market / Diplomacy) — no-op receivers for now ===
    public event Action? MarketOpenRequested;
    public event Action<int>? DiplomacyOpenRequested;                     // empireId

    // Fire methods — centralizes null-check pattern
    public void FireSystemSelected(StarSystemData system) => SystemSelected?.Invoke(system);
    public void FireSystemDeselected() => SystemDeselected?.Invoke();
    public void FireSystemHovered(StarSystemData system) => SystemHovered?.Invoke(system);
    public void FireSystemUnhovered() => SystemUnhovered?.Invoke();
    public void FireSystemDoubleClicked(StarSystemData system) => SystemDoubleClicked?.Invoke(system);

    public void FireFastTick(float delta) => FastTick?.Invoke(delta);
    public void FireSlowTick(float delta) => SlowTick?.Invoke(delta);
    public void FireSpeedChanged(GameSpeed speed) => SpeedChanged?.Invoke(speed);
    public void FireGamePaused() => GamePaused?.Invoke();
    public void FireGameResumed() => GameResumed?.Invoke();

    public void FireFleetSelected(int fleetId) => FleetSelected?.Invoke(fleetId);
    public void FireFleetSelectionToggled(int fleetId) => FleetSelectionToggled?.Invoke(fleetId);
    public void FireFleetDoubleClicked(int fleetId) => FleetDoubleClicked?.Invoke(fleetId);
    public void FireFleetDeselected() => FleetDeselected?.Invoke();
    public void FireFleetArrivedAtSystem(int fleetId, int systemId) => FleetArrivedAtSystem?.Invoke(fleetId, systemId);

    public void FireResourceChanged(int empireId, PrecursorColor color, ResourceType type, float newAmount) =>
        ResourceChanged?.Invoke(empireId, color, type, newAmount);

    public void FireSubsystemResearched(int empireId, string subsystemId) =>
        SubsystemResearched?.Invoke(empireId, subsystemId);
    public void FireResearchStarted(int empireId, string projectId) =>
        ResearchStarted?.Invoke(empireId, projectId);
    public void FireTierUnlocked(int empireId, PrecursorColor color, TechCategory category, int tier) =>
        TierUnlocked?.Invoke(empireId, color, category, tier);
    public void FireStationModuleInstalled(int stationId, int empireId) =>
        StationModuleInstalled?.Invoke(stationId, empireId);
    public void FireShipProduced(int empireId, string shipName) =>
        ShipProduced?.Invoke(empireId, shipName);

    public void FireSiteDiscovered(int empireId, int poiId) =>
        SiteDiscovered?.Invoke(empireId, poiId);
    public void FireScanProgressChanged(int empireId, int poiId, float progress, float difficulty) =>
        ScanProgressChanged?.Invoke(empireId, poiId, progress, difficulty);
    public void FireSiteScanComplete(int empireId, int poiId) =>
        SiteScanComplete?.Invoke(empireId, poiId);
    public void FireYieldExtracted(int empireId, int poiId, string resourceKey, float amount) =>
        YieldExtracted?.Invoke(empireId, poiId, resourceKey, amount);
    public void FireFleetOrderChanged(int fleetId) =>
        FleetOrderChanged?.Invoke(fleetId);
    public void FireSystemRightClicked(StarSystemData system) =>
        SystemRightClicked?.Invoke(system);
    public void FireSiteActivityChanged(int empireId, int poiId, SiteActivity activity) =>
        SiteActivityChanged?.Invoke(empireId, poiId, activity);
    public void FireSiteActivityRateChanged(int empireId, int poiId) =>
        SiteActivityRateChanged?.Invoke(empireId, poiId);
    public void FireScanToggleRequested(int poiId) =>
        ScanToggleRequested?.Invoke(poiId);
    public void FireExtractToggleRequested(int poiId) =>
        ExtractToggleRequested?.Invoke(poiId);

    public void FireDesignerOpenRequested(DesignerOpenRequest request) =>
        DesignerOpenRequested?.Invoke(request);
    public void FireDesignSaved(string designId) =>
        DesignSaved?.Invoke(designId);
    public void FireFleetTemplateSaved(string templateId) =>
        FleetTemplateSaved?.Invoke(templateId);

    public void FireTechTreeOpenRequested(TechTreeOpenRequest request) =>
        TechTreeOpenRequested?.Invoke(request);

    public void FireCombatStartRequested(int attackerFleetId, int defenderFleetId) =>
        CombatStartRequested?.Invoke(attackerFleetId, defenderFleetId);
    public void FireCombatStarted(int battleId) =>
        CombatStarted?.Invoke(battleId);
    public void FireCombatEnded(int battleId, CombatResult result) =>
        CombatEnded?.Invoke(battleId, result);
    public void FireBattleTick(int battleId) =>
        BattleTick?.Invoke(battleId);

    public void FireSystemViewOpened(int systemId) =>
        SystemViewOpened?.Invoke(systemId);
    public void FireSystemViewClosed() =>
        SystemViewClosed?.Invoke();
    public void FirePOISelected(int poiId) =>
        POISelected?.Invoke(poiId);
    public void FirePOIDeselected() =>
        POIDeselected?.Invoke();
    public void FireEntitySelected(string entityKind, int entityId, int poiId) =>
        EntitySelected?.Invoke(entityKind, entityId, poiId);
    public void FireEntityDeselected() =>
        EntityDeselected?.Invoke();
    public void FireBuildingSlotToggled(int colonyId, string buildingId, int slotIndex, bool newState) =>
        BuildingSlotToggled?.Invoke(colonyId, buildingId, slotIndex, newState);
    public void FireColonyPopsChanged(int colonyId) =>
        ColonyPopsChanged?.Invoke(colonyId);

    public void FireMarketOpenRequested() =>
        MarketOpenRequested?.Invoke();
    public void FireDiplomacyOpenRequested(int empireId) =>
        DiplomacyOpenRequested?.Invoke(empireId);

    // === Debug subscriber ===
    // Opt-in via DEBUG_EVENTBUS=1. Logs `[evt tick=N] EventName { payload }` for every
    // fired event through McpLog so godot_logs/godot_stdout surface the cascade. Default
    // blocklist suppresses per-frame chatter (FastTick/SlowTick/BattleTick/ScanProgressChanged).
    // Override via DEBUG_EVENTBUS_FILTER="Foo,Bar" (leading `-` is accepted but ignored for
    // syntax compatibility). Set DEBUG_EVENTBUS_FILTER="" to log everything.
    private void AttachDebugSubscriberIfEnabled()
    {
        if (System.Environment.GetEnvironmentVariable("DEBUG_EVENTBUS") != "1") return;

        var rawFilter = System.Environment.GetEnvironmentVariable("DEBUG_EVENTBUS_FILTER")
                        ?? DefaultDebugBlocklist;
        var blocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in rawFilter.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var name = raw.Trim().TrimStart('-', '+');
            if (name.Length > 0) blocklist.Add(name);
        }

        McpLog.Info($"[evt] debug subscriber attached (blocklist=[{string.Join(",", blocklist)}])");

        string Tick()
        {
            var tm = TurnManager.Instance;
            return tm != null ? tm.FastTickCount.ToString() : "?";
        }

        void LogSafe(string name, Func<string> payload)
        {
            string p;
            try { p = payload(); }
            catch (Exception ex) { p = $"<payload error: {ex.Message}>"; }
            McpLog.Info($"[evt tick={Tick()}] {name} {{ {p} }}");
        }

        void Hook(string name, Action subscribe)
        {
            if (!blocklist.Contains(name)) subscribe();
        }

        // Galaxy & Map
        Hook("SystemSelected",   () => SystemSelected   += s => LogSafe("SystemSelected",   () => $"id={s?.Id} name={s?.Name}"));
        Hook("SystemDeselected", () => SystemDeselected += () => LogSafe("SystemDeselected", () => ""));
        Hook("SystemHovered",    () => SystemHovered    += s => LogSafe("SystemHovered",    () => $"id={s?.Id}"));
        Hook("SystemUnhovered",  () => SystemUnhovered  += () => LogSafe("SystemUnhovered",  () => ""));
        Hook("SystemDoubleClicked", () => SystemDoubleClicked += s => LogSafe("SystemDoubleClicked", () => $"id={s?.Id} name={s?.Name}"));

        // Game Loop
        Hook("FastTick",     () => FastTick     += d => LogSafe("FastTick",     () => $"dt={d}"));
        Hook("SlowTick",     () => SlowTick     += d => LogSafe("SlowTick",     () => $"dt={d}"));
        Hook("SpeedChanged", () => SpeedChanged += s => LogSafe("SpeedChanged", () => $"speed={s}"));
        Hook("GamePaused",   () => GamePaused   += () => LogSafe("GamePaused",   () => ""));
        Hook("GameResumed",  () => GameResumed  += () => LogSafe("GameResumed",  () => ""));

        // Fleet
        Hook("FleetSelected",          () => FleetSelected          += id => LogSafe("FleetSelected",          () => $"id={id}"));
        Hook("FleetSelectionToggled",  () => FleetSelectionToggled  += id => LogSafe("FleetSelectionToggled",  () => $"id={id}"));
        Hook("FleetDoubleClicked",     () => FleetDoubleClicked     += id => LogSafe("FleetDoubleClicked",     () => $"id={id}"));
        Hook("FleetDeselected",        () => FleetDeselected        += () => LogSafe("FleetDeselected",        () => ""));
        Hook("FleetArrivedAtSystem",   () => FleetArrivedAtSystem   += (f, s) => LogSafe("FleetArrivedAtSystem", () => $"fleet={f} system={s}"));

        // Empire
        Hook("ResourceChanged", () => ResourceChanged += (e, c, t, n) => LogSafe("ResourceChanged", () => $"empire={e} color={c} type={t} new={n}"));

        // Research
        Hook("SubsystemResearched", () => SubsystemResearched += (e, s)       => LogSafe("SubsystemResearched", () => $"empire={e} sub={s}"));
        Hook("ResearchStarted",     () => ResearchStarted     += (e, p)       => LogSafe("ResearchStarted",     () => $"empire={e} project={p}"));
        Hook("TierUnlocked",        () => TierUnlocked        += (e, c, cat, t) => LogSafe("TierUnlocked",      () => $"empire={e} color={c} cat={cat} tier={t}"));

        // Stations
        Hook("StationModuleInstalled", () => StationModuleInstalled += (s, e) => LogSafe("StationModuleInstalled", () => $"station={s} empire={e}"));
        Hook("ShipProduced",           () => ShipProduced           += (e, n) => LogSafe("ShipProduced",           () => $"empire={e} ship={n}"));

        // Salvage / Exploration
        Hook("SiteDiscovered",         () => SiteDiscovered         += (e, p)       => LogSafe("SiteDiscovered",         () => $"empire={e} poi={p}"));
        Hook("ScanProgressChanged",    () => ScanProgressChanged    += (e, p, pr, d) => LogSafe("ScanProgressChanged",    () => $"empire={e} poi={p} progress={pr:F2} diff={d:F2}"));
        Hook("SiteScanComplete",       () => SiteScanComplete       += (e, p)       => LogSafe("SiteScanComplete",       () => $"empire={e} poi={p}"));
        Hook("YieldExtracted",         () => YieldExtracted         += (e, p, r, a) => LogSafe("YieldExtracted",         () => $"empire={e} poi={p} res={r} amt={a}"));
        Hook("FleetOrderChanged",      () => FleetOrderChanged      += id          => LogSafe("FleetOrderChanged",      () => $"fleet={id}"));
        Hook("SystemRightClicked",     () => SystemRightClicked     += s           => LogSafe("SystemRightClicked",     () => $"id={s?.Id}"));
        Hook("SiteActivityChanged",    () => SiteActivityChanged    += (e, p, a)   => LogSafe("SiteActivityChanged",    () => $"empire={e} poi={p} activity={a}"));
        Hook("SiteActivityRateChanged",() => SiteActivityRateChanged+= (e, p)      => LogSafe("SiteActivityRateChanged",() => $"empire={e} poi={p}"));
        Hook("ScanToggleRequested",    () => ScanToggleRequested    += p           => LogSafe("ScanToggleRequested",    () => $"poi={p}"));
        Hook("ExtractToggleRequested", () => ExtractToggleRequested += p           => LogSafe("ExtractToggleRequested", () => $"poi={p}"));

        // Ship Designer
        Hook("DesignerOpenRequested",  () => DesignerOpenRequested  += r  => LogSafe("DesignerOpenRequested",  () => r?.ToString() ?? "null"));
        Hook("DesignSaved",            () => DesignSaved            += id => LogSafe("DesignSaved",            () => $"design={id}"));
        Hook("FleetTemplateSaved",     () => FleetTemplateSaved     += id => LogSafe("FleetTemplateSaved",     () => $"template={id}"));

        // Tech Tree
        Hook("TechTreeOpenRequested",  () => TechTreeOpenRequested  += r => LogSafe("TechTreeOpenRequested",  () => r?.ToString() ?? "null"));

        // Combat
        Hook("CombatStartRequested", () => CombatStartRequested += (a, d) => LogSafe("CombatStartRequested", () => $"attacker={a} defender={d}"));
        Hook("CombatStarted",        () => CombatStarted        += id     => LogSafe("CombatStarted",        () => $"battle={id}"));
        Hook("CombatEnded",          () => CombatEnded          += (id, r) => LogSafe("CombatEnded",          () => $"battle={id} result={r}"));
        Hook("BattleTick",           () => BattleTick           += id     => LogSafe("BattleTick",           () => $"battle={id}"));

        // System View
        Hook("SystemViewOpened", () => SystemViewOpened += id => LogSafe("SystemViewOpened", () => $"system={id}"));
        Hook("SystemViewClosed", () => SystemViewClosed += () => LogSafe("SystemViewClosed", () => ""));
        Hook("POISelected",      () => POISelected      += id => LogSafe("POISelected",      () => $"poi={id}"));
        Hook("POIDeselected",    () => POIDeselected    += () => LogSafe("POIDeselected",    () => ""));
        Hook("EntitySelected",   () => EntitySelected   += (k, id, p) => LogSafe("EntitySelected", () => $"kind={k} id={id} poi={p}"));
        Hook("EntityDeselected", () => EntityDeselected += () => LogSafe("EntityDeselected", () => ""));
        Hook("BuildingSlotToggled", () => BuildingSlotToggled += (c, b, i, s) => LogSafe("BuildingSlotToggled", () => $"colony={c} building={b} slot={i} filled={s}"));
        Hook("ColonyPopsChanged",   () => ColonyPopsChanged   += c => LogSafe("ColonyPopsChanged", () => $"colony={c}"));

        // Deferred screens
        Hook("MarketOpenRequested",    () => MarketOpenRequested    += ()  => LogSafe("MarketOpenRequested",    () => ""));
        Hook("DiplomacyOpenRequested", () => DiplomacyOpenRequested += id  => LogSafe("DiplomacyOpenRequested", () => $"empire={id}"));
    }
}
