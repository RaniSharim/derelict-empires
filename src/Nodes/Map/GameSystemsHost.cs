using System.Collections.Generic;
using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Thin Godot Node wrapper around <see cref="GameSystems"/>. Pumps EventBus FastTick
/// into the systems and re-emits system-level events back onto EventBus for UI consumption.
///
/// All cross-tree fan-out from the logic layer goes through here — this is the single
/// place that knows about both <see cref="GameSystems"/> and <see cref="EventBus"/>.
/// </summary>
public partial class GameSystemsHost : Node
{
    public GameSystems Systems { get; } = new();

    public override void _Ready()
    {
        GameManager.Instance?.SetGameSystems(Systems);
        EventBus.Instance.FastTick += OnFastTick;

        // Bridge GameSystems events → EventBus.
        Systems.FleetArrived += (fleet, sysId) =>
        {
            McpLog.Info($"[Fleet] {fleet.Name} arrived at system {sysId}");
            EventBus.Instance?.FireFleetArrivedAtSystem(fleet.Id, sysId);
        };
        Systems.FleetOrderCompleted += fleet =>
            EventBus.Instance?.FireFleetOrderChanged(fleet.Id);

        Systems.SiteDiscovered += (eid, pid) =>
            EventBus.Instance?.FireSiteDiscovered(eid, pid);
        Systems.ScanProgressChanged += (eid, pid, prog, diff) =>
            EventBus.Instance?.FireScanProgressChanged(eid, pid, prog, diff);
        Systems.SiteScanComplete += (eid, pid) =>
            EventBus.Instance?.FireSiteScanComplete(eid, pid);
        Systems.YieldExtracted += (eid, pid, key, amt) =>
            EventBus.Instance?.FireYieldExtracted(eid, pid, key, amt);
        Systems.SiteActivityChanged += (eid, pid, act) =>
            EventBus.Instance?.FireSiteActivityChanged(eid, pid, act);
        Systems.SiteActivityRateChanged += (eid, pid) =>
            EventBus.Instance?.FireSiteActivityRateChanged(eid, pid);
        Systems.SiteLayerScanProgressChanged += (eid, pid, idx, prog, diff) =>
            EventBus.Instance?.FireSiteLayerScanProgressChanged(eid, pid, idx, prog, diff);
        Systems.SiteLayerScanned += (eid, pid, idx) =>
            EventBus.Instance?.FireSiteLayerScanned(eid, pid, idx);
        Systems.SiteLayerScavenged += (eid, pid, idx) =>
            EventBus.Instance?.FireSiteLayerScavenged(eid, pid, idx);
        Systems.SiteLayerSkipped += (eid, pid, idx) =>
            EventBus.Instance?.FireSiteLayerSkipped(eid, pid, idx);
        Systems.SiteResearchUnlocked += (eid, pid, idx) =>
            EventBus.Instance?.FireSiteResearchUnlocked(eid, pid, idx);
        Systems.SiteDangerTriggered += (eid, pid, idx, danger, sev) =>
            EventBus.Instance?.FireSiteDangerTriggered(eid, pid, idx, danger, sev);
        Systems.SiteSpecialOutcomeReady += (eid, pid, oid) =>
            EventBus.Instance?.FireSiteSpecialOutcomeReady(eid, pid, oid);
        Systems.SiteSpecialOutcomeResolved += (eid, pid, res) =>
            EventBus.Instance?.FireSiteSpecialOutcomeResolved(eid, pid, res);

        Systems.SubsystemResearched += (eid, subId) =>
        {
            var empire = GameManager.Instance?.EmpiresById.GetValueOrDefault(eid);
            McpLog.Info($"[Research] {empire?.Name} completed subsystem: {subId}");
            EventBus.Instance?.FireSubsystemResearched(eid, subId);
        };
        Systems.TierUnlocked += (eid, color, category, tier) =>
        {
            var empire = GameManager.Instance?.EmpiresById.GetValueOrDefault(eid);
            McpLog.Info($"[Research] {empire?.Name} unlocked {color} {category} tier {tier}");
            EventBus.Instance?.FireTierUnlocked(eid, color, category, tier);
        };

        Systems.BuildingCompleted += (colony, buildingId) =>
            McpLog.Info($"[Colony] {colony.Name} completed building: {buildingId}");
        Systems.PopulationGrew += colony =>
            McpLog.Info($"[Colony] {colony.Name} population grew to {colony.TotalPopulation}");

        Systems.ModuleInstalled += (station, module) =>
        {
            McpLog.Info($"[Station] {station.Name} installed module: {module.DisplayName}");
            EventBus.Instance?.FireStationModuleInstalled(station.Id, station.OwnerEmpireId);
        };

        Systems.DepositDepleted += (empireId, deposit) =>
            McpLog.Info($"[Resources] Deposit depleted for empire {empireId}: {deposit.Color} {deposit.Type}");
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
            EventBus.Instance.FastTick -= OnFastTick;
    }

    private void OnFastTick(float delta)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        Systems.Tick(delta, gm.Fleets, gm.ShipsById, gm.EmpiresById);
    }
}
