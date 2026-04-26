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

---

# Addendum — 2026-04-25 second-pass review (Phase 1–4 reports)

A second, independent review (`phase_1_report.md` … `phase_4_report.md`) flagged additional issues. The list below is the **validated** subset — items the reports got right after I checked the actual code, plus rejections for items that are wrong or already covered.

## Validation summary

| Report item | Status | Notes |
|---|---|---|
| P1.1 `MarketService.ActiveListings` allocates list per access | **Defer** | Real allocation, but `MarketService` is not wired into the runtime — only `tests/Economy/MarketTests.cs` instantiates it. Fix when the trade UI lands. |
| P1.2 `EmpireData.ResourceKey` string concat | **Real, fix** | `FactionResourceBox._Process` calls it 6×/box/frame. Confirmed in [FactionResourceBox.cs:188](../../src/Nodes/UI/FactionResourceBox.cs#L188). |
| P1.3 `MarketService.BuyListing/PlaceBid` O(N) | **Defer** | Same reason as P1.1. |
| P1.4 `ShipDesignValidator` exception pattern | **Reject** | Stylistic; current `ValidationResult` is the right pattern for a UI builder. No change. |
| P2.1 `UtilityBrain.Evaluate` LINQ chain | **Worth fixing pre-emptively** | Currently only called in tests (no runtime caller — confirmed via grep). Cheap to fix while it's small. |
| P2.2 GameManager / GameSystems "tight coupling" | **Reject** | This is the documented `IGameQuery` facade pattern (see `CLAUDE.md` "UI contract"). The forwarding is intentional. |
| P2.3 `Enum.GetValues<T>()` allocates | **Real, fix** | Hot callers: [ResearchEngine.cs:203](../../src/Core/Tech/ResearchEngine.cs#L203) (slow tick × empires × synergies) and [RandomEventSystem.cs:40](../../src/Core/Events/RandomEventSystem.cs#L40) (slow tick). |
| P3.1 `FleetMovementSystem.AdvanceFleet` LINQ on lanes | **Real, fix — highest priority** | Hot path: 10 Hz × every moving fleet. Calls `Lanes.Where(...).FirstOrDefault(...)` per tick. |
| P3.2 `BattleManager.RecordRoundToDesignPerformance .Sum()` | **Real, fix** | Per combat round (not per fast tick), so cooler than P3.1, but trivial to cache. |
| P3.3 Move `GetFleetPosition` to visual side | **Reject** | The Core method returns `(float x, float z)` — no Godot dependency leaks. Moving it would just duplicate the math in two places. |
| P4.1 Edge-pan bypasses UI consumption | **Real, fix** | Confirmed [StrategyCameraRig.cs:165](../../src/Nodes/Camera/StrategyCameraRig.cs#L165). The report's suggested fix is incomplete — see Phase F below for the correct approach. |
| P4.2 `MainScene.UpdateTopBarDelta` God-class smell | **Already tracked** | Called out in [refactor-3.md:268,385](refactor-3.md#L268). Don't duplicate — let that plan absorb it. |

## Phase E — GC and hot-path allocations (low risk, ~2 hr)

All real, all mechanical. Each item has a unit test (existing or trivial-to-add) so regressions show up immediately.

### E1 — `GalaxyData`: pre-index lanes by system
File: [src/Core/Models/GalaxyData.cs](../../src/Core/Models/GalaxyData.cs)

Currently `GetLanesForSystem(int)` returns `Lanes.Where(l => l.Connects(systemId))` — a fresh enumerator per call. Called from `FleetMovementSystem.AdvanceFleet` at 10 Hz per moving fleet, plus inside the Dijkstra inner loop in `LanePathfinder`.

- Add `private Dictionary<int, List<LaneData>>? _lanesBySystem;` field.
- Add `public void RebuildLaneIndex()` that walks `Lanes` once and populates the map. Call it at the end of galaxy generation and after `LoadGame` rehydrates lanes.
- Change `GetLanesForSystem` to return the indexed list (or `Array.Empty<LaneData>()` for unknown systemIds). Drop the `Lanes.Where(...)`.
- Keep `GetNeighbors` — it already composes on top of `GetLanesForSystem`.

Tests: existing `tests/Galaxy/LaneGeneratorTests.cs` and `tests/Pathfinding/*` use the public API only — they'll catch breakage. Add one new test: build a galaxy, mutate `Lanes`, confirm `RebuildLaneIndex` picks it up (defensive — saves debugging if save/load adds lanes post-gen).

### E2 — Cache `Enum.GetValues<T>()` results
Two hot files:

- [src/Core/Tech/ResearchEngine.cs:203](../../src/Core/Tech/ResearchEngine.cs#L203) — `CheckSynergyUnlocks` runs on `SlowTick × empire × synergy`, inner loop allocates a `TechCategory[]` each time.
- [src/Core/Events/RandomEventSystem.cs:40](../../src/Core/Events/RandomEventSystem.cs#L40) — runs on every event roll.

Pattern (apply to both):
```csharp
private static readonly TechCategory[] AllCategories = Enum.GetValues<TechCategory>();
```

Also worth doing in [BattleManager.cs:172](../../src/Core/Combat/BattleManager.cs#L172) (cooler path but the pattern is the same). Skip the test-only callers and the one-time `GameSetupManager` callers.

### E3 — `EmpireData`: stop building string keys at read time
File: [src/Core/Models/EmpireData.cs:36](../../src/Core/Models/EmpireData.cs#L36)

The `Dictionary<string, float> ResourceStockpile` keying is on the save format, so we can't change it without breaking saves. Two-tiered approach:

1. **Read-side cache (the actual hot path):** add a `static readonly string[,] _keyCache = new string[5, 6]` populated once with `$"{color}_{type}"` for every (color, type) pair. Change `ResourceKey(...)` to return `_keyCache[(int)color, (int)type]`. Zero allocations per read; identity-equal strings so the dict probe is a hash + ref-compare.
2. Leave the `Dictionary<string, float>` shape and the save format untouched.

Verifies that `FactionResourceBox._Process` no longer allocates strings each frame; the existing `tests/SmokeTests.EmpireData_ResourceStockpile_Works` covers correctness.

(The report's suggestion of a `struct ResourceId` key is correct architecturally but breaks `GameSaveData` JSON serialization and every consumer of `ResourceStockpile.GetValueOrDefault("Red_SimpleOre", …)` in `SalvageSystem`/`ResourceExtractionSystem`/`ResourceDistributionHelper`/etc. Not worth the blast radius for a fix the cache already nails.)

### E4 — `BattleManager.RecordRoundToDesignPerformance`: cache attacker budget
File: [src/Core/Combat/BattleManager.cs:266](../../src/Core/Combat/BattleManager.cs#L266)

- Add `float AttackerWeaponBudget` field on `Battle`. Compute it once in `BeginBattle` after attackers are populated. Recompute (with a `for` loop, not LINQ) inside `RecordRoundToDesignPerformance` only when the alive count changed since last round — track via `int _lastAttackerAliveCount` on `Battle`.
- Replace `battle.Attackers.Sum(u => u.WeaponDamage)` with the cached field.

### E5 — `UtilityBrain.Evaluate`: zero-alloc eval (defensive)
File: [src/Core/AI/AIManager.cs:84](../../src/Core/AI/AIManager.cs#L84)

Not currently hot (no runtime caller), but the fix is small and removes the LINQ pattern future readers might copy.

- Replace the LINQ chain with: pre-allocate `private readonly (AIAction action, float score)[] _scratch;` sized to `_actions.Count` in the ctor. Foreach-fill, sort the prefix in place, take top-N.
- Existing tests in `tests/AI/AITests.cs` cover the contract.

**Gate:** 335/335 unit tests pass. Add a single allocation-pressure micro-bench in `tests/Performance/` is **not** required for this phase — the changes are mechanical and the existing tests cover behaviour.

**Commit:** `perf(core): pre-index lanes, cache enum/key arrays, drop hot-path LINQ`

---

## Phase F — Camera edge-pan respects UI (low risk, ~30 min)

File: [src/Nodes/Camera/StrategyCameraRig.cs:158-178](../../src/Nodes/Camera/StrategyCameraRig.cs#L158-L178)

The report's proposed fix (read mouse pos in `_UnhandledInput`) is half-right — it solves the "pan while dragging UI on the edge" case but **not** the "mouse stationary on edge over a Control" case, because `_UnhandledInput` doesn't fire when the mouse isn't moving.

Correct fix: gate the existing `HandleEdgePan` on whether a UI Control is currently under the cursor.

```csharp
private void HandleEdgePan(float delta)
{
    if (!EdgePanEnabled) return;
    if (_middleMouseDragging) return;

    // NEW: skip edge-pan when a UI Control is under the cursor.
    // GuiGetHoveredControl() returns null when the cursor is over the 3D viewport
    // or off-window; non-null means a Control with MouseFilter=Stop/Pass owns the area.
    if (GetViewport().GuiGetHoveredControl() != null) return;

    // ...rest unchanged
}
```

This is the idiomatic Godot way and doesn't require buffering motion events. Verify by hovering over `LeftPanel`/`RightPanel`/`TopBar` and confirming the camera no longer drifts.

**Gate:** Manual smoke (MCP `godot_screenshot` after camera nudges with cursor over each panel) + 335/335 tests.
**Commit:** `fix(camera): suppress edge-pan when UI control hovered`

---

## Sequencing of new phases

E and F are independent of A–D and refactor-2-ui. Recommended order: **E1 → E2 → E3 → E4 → E5 → F**, each as its own commit. E1 has the highest gameplay impact (movement is wired and ticks 10 Hz). The rest are defensive/cleanup.

If only one thing ships from this addendum: **E1**.
