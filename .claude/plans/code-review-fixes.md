# Code-Review Fix Plan

**Created:** 2026-04-25
**Companion to:** [refactor-2-ui.md](refactor-2-ui.md)

This plan addresses the 2026-04-25 code review findings. Items already owned by `refactor-2-ui` are noted and **not duplicated here** — this plan covers the orthogonal cleanups (controller decoupling, runtime safety, performance, lint).

Each phase is independently shippable. Verification protocol = same as refactor-1/2: `dotnet build` + `dotnet test` (335 passing) + `godot_reload` + `godot_screenshot` + manual smoke.

---

## Phase A — Runtime safety (low risk, ~1 hr)

Small, surgical correctness fixes. No architecture change.

### A1 — `CombatRouter`: harden init + drop sibling path
File: [src/Nodes/Map/CombatRouter.cs](../../src/Nodes/Map/CombatRouter.cs)

- **Stop walking the sibling tree for the camera.** Replace `_cameraRig.GetNode<Camera3D>("Camera3D")` ([CombatRouter.cs:96](../../src/Nodes/Map/CombatRouter.cs#L96)) with an explicit accessor on the rig: add `public Camera3D Camera => _camera;` to [StrategyCameraRig](../../src/Nodes/Camera/StrategyCameraRig.cs), cache the field in its `_Ready`, and have CombatRouter call `_cameraRig.Camera`.
- **Null-guard `GameManager.Instance`** in `EngageCombat` ([CombatRouter.cs:72-75](../../src/Nodes/Map/CombatRouter.cs#L72-L75)). Early-return with `McpLog.Warn` if `gm` or `gm.Galaxy` is null.
- **Defer RNG creation** until `MasterSeed` is known. Currently the `BattleManager` is created the first time combat starts, which is fine — but assert seed is non-zero (`gm.MasterSeed != 0`) and log if it is, since combat before `OnSetupConfirmed` indicates a bug.

### A2 — `FleetVisualController`: snapshot-iterate fleets
File: [src/Nodes/Map/FleetVisualController.cs:73](../../src/Nodes/Map/FleetVisualController.cs#L73)

- `OnFastTick` iterates `GameManager.Instance.Fleets` while movement may add/remove. Two options:
  1. Iterate the local `_fleetNodes` dictionary keys (visuals owns its own list of known fleet IDs), looking up `FleetData` via `GameManager.Instance` per ID.
  2. Snapshot via `foreach (var fleet in gm.Fleets.ToArray())`.
- Prefer (1) — visuals already track the set they care about. Drop the `gm` enumeration.
- Add `IsInstanceValid(GameManager.Instance)` guard at top.

### A3 — `GlassOverlay`: anchors, not Position
File: [src/Nodes/UI/GlassOverlay.cs:50](../../src/Nodes/UI/GlassOverlay.cs#L50)

- Replace `Position = Vector2.Zero` + manual `Size` with `SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect)`. The overlay is a screen-filling Control on a CanvasLayer — anchors give the same result and survive viewport resize.

### A4 — `RightPanel`: full signal cleanup
File: [src/Nodes/UI/RightPanel.cs:73-81](../../src/Nodes/UI/RightPanel.cs#L73-L81)

- `_ExitTree` already disconnects EventBus; add `_attackButton.Pressed -= OnAttackPressed` (and any other in-scene Button signals connected via `+=`).
- Audit all UI panels with one grep for `+= ` on Button/PanelContainer/etc. signals and verify each has a matching `-= ` in `_ExitTree`. **Note:** refactor-2-ui will rewrite these panels — Phase A4 only patches the leak in the meantime; if rebuild lands first, skip.

**Gate:** 335/335 tests, no visual diff, no log warnings on launch/load.
**Commit:** `fix(runtime): null-guards, sibling-path removal, anchor cleanup`

---

## Phase B — `SelectionController` decoupling (medium risk, ~3 hr)

The biggest sibling-coupling smell. Currently takes `MainScene`, `FleetVisualController`, `StrategyCameraRig` as ctor params ([SelectionController.cs:34-39](../../src/Nodes/Map/SelectionController.cs#L34-L39)).

### B1 — Replace `MainScene` ref with `IGameQuery`
- Already on `IGameQuery`: `Fleets`, `PlayerEmpire`, galaxy access via `FindPOI`.
- Need to add: `MovementSystem` accessor (or, better, expose `MovementSystem.GetOrder(int fleetId)` and `IssueMoveOrder(...)` indirectly via intent events).
- **Cleanest path:** add a single intent event to EventBus: `FleetMoveOrderRequested(int fleetId, int targetSystemId)`. SelectionController fires it; a new `MovementActionHandler` (sibling of `SalvageActionHandler`) validates and calls `Systems.Movement.IssueMoveOrder`. SelectionController no longer needs MovementSystem at all.
- For path-indicator rendering it still needs to *read* current orders. Add `FleetOrder? GetOrder(int fleetId)` to `IGameQuery` (forwards to `Systems.Movement`).

### B2 — Replace `FleetVisualController` ref with EventBus
SelectionController currently calls `_fleetVisuals.GetNode(fleetId)?.SetSelected(true/false)`. Two paths:
- **(a) Selection-state event:** fire `FleetSelectionVisualChanged(int fleetId, bool selected)`. FleetVisualController subscribes and calls its own `SetSelected` on the matching FleetNode. SelectionController owns *state*, FleetVisualController owns *visuals*.
- **(b) FleetVisualController subscribes directly to existing FleetSelected/FleetDeselected/FleetSelectionToggled events** and maintains its own selection-visual state.

Prefer **(b)** — fewer events, FleetVisualController is already a fast-tick subscriber. SelectionController stops touching fleet visuals entirely.

For `OnFleetDoubleClicked` (camera pan to fleet world position), replace `_fleetVisuals.GetNode(fleetId)?.GlobalPosition` with: fire `EventBus.FireCameraPanRequested(int fleetId)` and let `StrategyCameraRig` subscribe and read its own visual via the same query. **Or** add `Vector3? GetFleetWorldPosition(int fleetId)` to `IGameQuery` — FleetVisualController can register itself as the resolver. The event-based version is cleaner.

### B3 — Replace `StrategyCameraRig` ref with EventBus
- After B2's `CameraPanRequested(fleetId)` event lands, `StrategyCameraRig` subscribes in its own `_Ready`. SelectionController loses its third ref.

### B4 — Final SelectionController shape
```csharp
public partial class SelectionController : Node
{
    private IGameQuery _query = null!;
    private FleetOrderIndicator _pathIndicator = null!;
    public void Configure(IGameQuery query) => _query = query;
    // ...rest unchanged
}
```
And in `MainScene._Ready`: `_selectionController.Configure(GameManager.Instance);`

**Gate:** Same as Phase A. Manual smoke = full selection/move/double-click flow works.
**Commit:** `refactor(controllers): SelectionController via IGameQuery + EventBus`

---

## Phase C — Performance: kill per-frame state polling (low risk, ~2 hr)

### C1 — `ResearchStrip`: event-driven instead of `_Process`
File: [src/Nodes/UI/ResearchStrip.cs:75-89](../../src/Nodes/UI/ResearchStrip.cs#L75-L89)

- Drop `_Process` entirely.
- Subscribe in `_Ready` to: `ResearchStarted`, `SubsystemResearched`, `TierUnlocked`, `FastTick` (only for active-progress redraw — but progress updates come from `ResearchStateChanged`-style events; if no such event exists today, *add* `EventBus.ResearchProgressChanged(int empireId)` and fire it from `ResearchEngine` when progress increments).
- Cache last-rendered state; redraw only on event.
- Unsubscribe in `_ExitTree`.

### C2 — Audit other `_Process` polling
Grep `public override void _Process` under `src/Nodes/UI/`. For each hit, decide: legitimate per-frame visual (animations, pulses, canvas draw) → keep; state polling → convert to event-driven.

**Gate:** Visual parity, frame-time at idle should drop slightly.
**Commit:** `perf(ui): event-driven research strip + audit`

---

## Phase D — Lint pass (low risk, ~1 hr)

### D1 — `GD.Print` → `McpLog.Info`
- Grep `GD.Print` repo-wide. Replace with `McpLog.Info` per CLAUDE.md mandate. Skip `McpLog.cs` itself and any test-only utilities.
- Notable hits: [EventBus.cs:26](../../src/Autoloads/EventBus.cs#L26), [MainScene.cs:64](../../src/Nodes/Map/MainScene.cs#L64) (already McpLog — verify), GameManager autoload.

### D2 — `EventBus` debug blocklist as enum
File: [src/Autoloads/EventBus.cs:21](../../src/Autoloads/EventBus.cs#L21)

- Replace the comma-string `DefaultDebugBlocklist` with a `static readonly HashSet<string> DefaultDebugBlocklist = new(StringComparer.OrdinalIgnoreCase) { "FastTick", "SlowTick", ... };`. Eliminates typos and the case-insensitive parse.
- Update `AttachDebugSubscriberIfEnabled` consumers accordingly.

### D3 — Standardize child-node wiring
- Pick one pattern per panel: `[Export]` for editor-wired, `GetNode<T>` in `_Ready` for code-built. Document the choice in CLAUDE.md alongside Phase 4 of refactor-2-ui (or earlier).
- Don't churn existing files — apply during refactor-2-ui's `.tscn` rewrites.

**Gate:** No log-output regressions, debug subscriber still filters correctly with `DEBUG_EVENTBUS=1`.
**Commit:** `chore(lint): McpLog consistency + EventBus blocklist as set`

---

## What's deferred to refactor-2-ui (no duplication)

These review items belong to refactor-2-ui and are tracked there:

| Review item | Owner phase |
|---|---|
| `MainScene.SetMainScene(this)` injection circular dep | refactor-2-ui Phase 1A/1B/2 |
| Code-built UI vs `.tscn` | refactor-2-ui Phase 2/3 |
| `RightPanel`/`LeftPanel` size + complexity | refactor-2-ui Phase 2 |
| Hardcoded `GetNode("Child/Path")` in panels | refactor-2-ui Phase 2 (becomes `[Export]` slots) |
| `ChassisPane.GetParent() ?? GetTree().Root` fallback | refactor-2-ui Phase 3 |

---

## Sequencing

If both plans run together: **A → C → D → B** in parallel with refactor-2-ui Phases 1–4. Phase B (SelectionController decoupling) is independent of the UI rebuild and can land any time. Phase D3 (`[Export]` standardization) waits for refactor-2-ui Phase 2.

If refactor-2-ui is paused: do **A → B → C → D** sequentially. Each phase ships in its own commit.

## Risk

- **Phase B** is the only one that touches gameplay code paths (selection, movement, camera). Mitigations: keep SelectionController's external API (`SelectedFleetId`, `SelectedFleetIds`, `Reset`) unchanged; verify with the existing E2E selection tests in `tests/E2E/`.
- All other phases are local mechanical changes with screenshot parity as the gate.
