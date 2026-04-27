using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Bridges UI intent events to the layered <see cref="DerlictEmpires.Core.Exploration.SalvageSystem"/>.
/// Maps the legacy "scan toggle" intent to <c>RequestScan</c>/<c>RequestStop</c> on the
/// site's active layer, and "extract toggle" to <c>RequestScavenge</c>/<c>RequestStop</c>.
/// The site/layer state machine validates the call and silently no-ops when invalid
/// (e.g. trying to scavenge before the layer is scanned).
/// </summary>
public partial class SalvageActionHandler : Node
{
    private GameSystems _systems = null!;

    public void Configure(GameSystems systems) => _systems = systems;

    public override void _Ready()
    {
        EventBus.Instance.ScanToggleRequested += OnScanToggle;
        EventBus.Instance.ExtractToggleRequested += OnExtractToggle;
        EventBus.Instance.SkipLayerRequested += OnSkipLayer;
        EventBus.Instance.SpecialOutcomeRequested += OnSpecialOutcome;
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance == null) return;
        EventBus.Instance.ScanToggleRequested -= OnScanToggle;
        EventBus.Instance.ExtractToggleRequested -= OnExtractToggle;
        EventBus.Instance.SkipLayerRequested -= OnSkipLayer;
        EventBus.Instance.SpecialOutcomeRequested -= OnSpecialOutcome;
    }

    private void OnScanToggle(int poiId)
    {
        var salvage = _systems?.Salvage;
        if (salvage == null) { McpLog.Warn("[Scan] rejected: salvage system not ready"); return; }
        var player = GameManager.Instance?.LocalPlayerEmpire;
        if (player == null) return;

        var current = salvage.GetActivity(player.Id, poiId);
        bool changed = current == SiteActivity.Scanning
            ? salvage.RequestStop(player.Id, poiId)
            : salvage.RequestScan(player.Id, poiId);
        if (changed)
            McpLog.Info($"[Scan] POI {poiId} → {salvage.GetActivity(player.Id, poiId)}");
    }

    private void OnExtractToggle(int poiId)
    {
        var salvage = _systems?.Salvage;
        if (salvage == null) { McpLog.Warn("[Extract] rejected: salvage system not ready"); return; }
        var player = GameManager.Instance?.LocalPlayerEmpire;
        if (player == null) return;

        var current = salvage.GetActivity(player.Id, poiId);
        bool changed = current == SiteActivity.Extracting
            ? salvage.RequestStop(player.Id, poiId)
            : salvage.RequestScavenge(player.Id, poiId);
        if (changed)
            McpLog.Info($"[Extract] POI {poiId} → {salvage.GetActivity(player.Id, poiId)}");
    }

    private void OnSkipLayer(int poiId)
    {
        var salvage = _systems?.Salvage;
        if (salvage == null) { McpLog.Warn("[Skip] rejected: salvage system not ready"); return; }
        var player = GameManager.Instance?.LocalPlayerEmpire;
        if (player == null) return;

        if (salvage.RequestSkip(player.Id, poiId))
            McpLog.Info($"[Skip] POI {poiId} layer skipped");
    }

    private void OnSpecialOutcome(int poiId)
    {
        var salvage = _systems?.Salvage;
        if (salvage == null) { McpLog.Warn("[Outcome] rejected: salvage system not ready"); return; }
        var player = GameManager.Instance?.LocalPlayerEmpire;
        if (player == null) return;

        var resolution = salvage.RequestSpecialOutcome(player, poiId);
        if (resolution.Success)
            McpLog.Info($"[Outcome] POI {poiId} resolved: {resolution.Kind}");
        else
            McpLog.Warn($"[Outcome] POI {poiId} failed: {resolution.FailureReason}");
    }
}
