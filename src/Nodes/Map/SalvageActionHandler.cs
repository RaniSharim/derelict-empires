using Godot;
using DerlictEmpires.Autoloads;
using DerlictEmpires.Core;
using DerlictEmpires.Core.Enums;

namespace DerlictEmpires.Nodes.Map;

/// <summary>
/// Receives salvage intent events from the UI (<c>EventBus.ScanToggleRequested</c>,
/// <c>EventBus.ExtractToggleRequested</c>), validates them against game state, and
/// forwards the resulting toggle to <see cref="GameSystems.Salvage"/>.
///
/// This is the single seam between UI button presses and the salvage system. UI panels
/// fire intent events without holding a system reference; tests can stub the handler.
/// </summary>
public partial class SalvageActionHandler : Node
{
    private GameSystems _systems = null!;

    public void Configure(GameSystems systems) => _systems = systems;

    public override void _Ready()
    {
        EventBus.Instance.ScanToggleRequested += OnScanToggle;
        EventBus.Instance.ExtractToggleRequested += OnExtractToggle;
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance == null) return;
        EventBus.Instance.ScanToggleRequested -= OnScanToggle;
        EventBus.Instance.ExtractToggleRequested -= OnExtractToggle;
    }

    private void OnScanToggle(int poiId) => Toggle(poiId, SiteActivity.Scanning, "Scan");
    private void OnExtractToggle(int poiId) => Toggle(poiId, SiteActivity.Extracting, "Extract");

    private void Toggle(int poiId, SiteActivity intended, string label)
    {
        var salvage = _systems?.Salvage;
        if (salvage == null) { McpLog.Warn($"[{label}] rejected: salvage system not ready"); return; }
        var player = GameManager.Instance?.LocalPlayerEmpire;
        if (player == null) return;

        var current = salvage.GetActivity(player.Id, poiId);
        var next = current == intended ? SiteActivity.None : intended;
        if (salvage.RequestActivity(player.Id, poiId, next))
            McpLog.Info($"[{label}] POI {poiId} → {next}");
    }
}
