# Refactor 3 — GameSystems Composition Root

**Status:** Ready to start. Hand-off plan for an agent picking this up cold.
**Created:** 2026-04-25
**Predecessor:** [refactor-1.md](refactor-1.md) — extracted DevHarness/OverlayRouter/CombatRouter/SelectionController; migrated game *data* to GameManager. MainScene is now 761 lines.
**Successor:** [refactor-2-ui.md](refactor-2-ui.md) — UI rebuild. **Refactor-3 lands first** so the UI rebuild targets a stable system surface.
**Target file:** [src/Nodes/Map/MainScene.cs](../../src/Nodes/Map/MainScene.cs) (761 → ~300 lines)
**Goal state:** MainScene is scene-graph wiring only. All logic systems live inside one `GameSystems` composition root that is testable without Godot. UI panels read via `IGameQuery` and write via `EventBus` intent events — zero direct system references.

---

## Motivation

After refactor-1, MainScene still owns the entire **logic-system layer**:

```
// MainScene.cs fields (still there)
private FleetMovementSystem? _movementSystem;
private ResourceExtractionSystem? _extractionSystem;
private SettlementSystem? _settlementSystem;
private StationSystem? _stationSystem;
private List<Station> _stations = new();
private TechTreeRegistry? _techRegistry;
private ResearchEngine? _researchEngine;
private Dictionary<int, EmpireResearchState> _researchStates = new();
private ExplorationManager _exploration = null!;
private SalvageSystem? _salvageSystem;

// Plus init helpers, save/load wiring, tick pumping, and ~12 public accessors
// for UI panels (TechRegistry, ResearchEngine, SalvageSystem, MovementSystem,
// SettlementSystem, StationSystem, GetSystemCapability, GetSystemActiveCount,
// TryToggleScan, TryToggleExtract, FindPOI, GetSalvageSite, ...)
```

This is a god surface. Three concrete pains:

1. **Tests can't exercise systems together.** Each pure-C# system is unit-testable individually, but the wiring (movement → exploration discovery → salvage rate refresh, salvage activity → topbar income) only exists inside `MainScene._Ready` / `InitSalvage`. To test the integration you need Godot.
2. **Save/load is split awkwardly.** `BuildGameSaveData` reaches into 4 different system fields. Restoring orders, extractions, and research states is a bespoke sequence inside `LoadGame`. There's no single "snapshot/restore" boundary.
3. **Sibling controllers and UI panels back-reference MainScene** to reach systems. `_main.SalvageSystem`, `_main.MovementSystem`, `_main.SettlementSystem`. Every accessor is a coupling point.

The fix is a **composition root**: one object that owns every game-logic system, wires them together, and exposes a single typed surface. MainScene holds one field. UI/controllers read from `Systems.Movement`, `Systems.Salvage`, etc.

---

## Goal State

```
src/Core/
  GameSystems.cs            ← NEW. Plain C# class. Owns all logic systems.
  GameSystemsHost.cs        ← NEW. Thin Node wrapper. Pumps EventBus ticks
                              into GameSystems. Lives in MainScene tree.

src/Nodes/Map/
  MainScene.cs              ← shrinks to ~200 lines (scene graph + bootstrap)
  FleetVisualController.cs  ← NEW. Owns _fleetContainer, _fleetNodes,
                              SpawnFleetNodes. Reads Systems.Movement.

src/Autoloads/
  GameManager.cs            ← unchanged (already owns data after refactor-1)
```

**Field count on MainScene after this refactor:** 8 (down from 17).
- `_uiLayer`, `_galaxyMap`, `_cameraRig`, `_systemsHost`, `_fleetVisuals`,
  `_devHarness`, `_overlayRouter`, `_combatRouter`, `_selectionController`,
  plus UI panel refs (those die with refactor-2-ui).

**Public accessors on MainScene after this refactor:** 1.
- `public GameSystems Systems => _systemsHost.Systems;`

Everything else (`SalvageSystem`, `MovementSystem`, `TryToggleScan`, `GetSystemCapability`, `RegisterColony`, etc.) moves either to `GameSystems` or dies (refactor-2-ui replaces some with EventBus action requests + IGameQuery).

---

## What `GameSystems` Looks Like

```csharp
namespace DerlictEmpires.Core;

/// <summary>
/// Composition root for all game-logic systems. Plain C# — no Godot dependencies.
/// Constructable in tests: new GameSystems(); systems.LoadFromSetup(...); systems.Tick(0.1f);
/// </summary>
public class GameSystems
{
    // ── Logic systems (read-only after Load*) ────────────
    public FleetMovementSystem    Movement     { get; private set; } = null!;
    public SalvageSystem          Salvage      { get; private set; } = null!;
    public ExplorationManager     Exploration  { get; private set; } = null!;
    public ResourceExtractionSystem Extraction { get; private set; } = null!;
    public SettlementSystem       Settlements  { get; private set; } = null!;
    public StationSystem          Stations     { get; private set; } = null!;
    public TechTreeRegistry       TechRegistry { get; private set; } = null!;
    public ResearchEngine         Research     { get; private set; } = null!;

    // ── Per-empire mutable state ─────────────────────────
    private readonly Dictionary<int, EmpireResearchState> _researchStates = new();
    public IReadOnlyDictionary<int, EmpireResearchState> ResearchStates => _researchStates;

    // ── Lifecycle ─────────────────────────────────────────
    public void LoadFromSetup(GalaxyData galaxy,
                              IReadOnlyList<EmpireData> empires,
                              IReadOnlyList<ColonyData> colonies,
                              IReadOnlyList<StationData> stations,
                              GameRandom rng) { ... }

    public void LoadFromSave(GameSaveData save) { ... }

    public void Tick(float fastDelta) { ... }   // movement + salvage + ...
    public void SlowTick(float delta) { ... }   // economy + growth + ...

    public GameSaveData BuildSaveData(GameManagerSnapshot data) { ... }

    // ── UI helpers (formerly on MainScene) ───────────────
    public float GetSystemCapability(int poiId, SiteActivity type, ...) { ... }
    public int   GetSystemActiveCount(int poiId, SiteActivity type, ...) { ... }
    public bool  RequestActivity(int empireId, int poiId, SiteActivity type) { ... }
    public POIData? FindPOI(int poiId, out int systemId) { ... }
    public SalvageSiteData? GetSalvageSite(int siteId) { ... }

    // ── Events (subscribed by GameSystemsHost and re-emitted on EventBus) ──
    public event Action<FleetData, int>? FleetArrivedAtSystem;
    public event Action<FleetData, int>? FleetDeparted;
    public event Action<FleetData>? FleetOrderCompleted;
    public event Action<int, int>? SiteDiscovered;
    public event Action<int, int, float, float>? ScanProgressChanged;
    public event Action<int, int>? SiteScanComplete;
    public event Action<int, int, string, int>? YieldExtracted;
    public event Action<int, int, SiteActivity>? SiteActivityChanged;
    public event Action<int, int>? SiteActivityRateChanged;
    public event Action<int, string>? SubsystemResearched;
    public event Action<int, PrecursorColor, ResearchCategory, int>? TierUnlocked;
    public event Action<Colony, string>? BuildingCompleted;
    public event Action<Colony>? PopulationGrew;
    public event Action<Station, IModule>? ModuleInstalled;
}
```

`GameSystemsHost` is the Node wrapper:

```csharp
public partial class GameSystemsHost : Node
{
    public GameSystems Systems { get; } = new();

    public override void _Ready()
    {
        EventBus.Instance.FastTick += OnFastTick;
        EventBus.Instance.SlowTick += OnSlowTick;

        // Re-emit GameSystems events on EventBus
        Systems.SiteDiscovered += (eid, pid) => EventBus.Instance.FireSiteDiscovered(eid, pid);
        Systems.YieldExtracted += (eid, pid, key, amt) => EventBus.Instance.FireYieldExtracted(eid, pid, key, amt);
        // ... etc
    }

    public override void _ExitTree() { /* unsubscribe */ }

    private void OnFastTick(float delta) => Systems.Tick(delta);
    private void OnSlowTick(float delta) => Systems.SlowTick(delta);
}
```

The split: **`GameSystems` = pure logic, no Godot. `GameSystemsHost` = thin Node that pumps Godot ticks into `GameSystems` and bridges to EventBus.**

---

## Visual Controller Split

Not everything moves into `GameSystems`. Keep a separation:

| Concern | Lives in |
|---|---|
| `FleetMovementSystem` (computes fleet positions) | `GameSystems.Movement` |
| `_fleetContainer` (Node3D parent) | `FleetVisualController` |
| `_fleetNodes` (`Dictionary<int, FleetNode>`) | `FleetVisualController` |
| `SpawnFleetNodes(galaxy)` | `FleetVisualController` |
| Fast-tick `node.UpdatePosition(x, z)` | `FleetVisualController` |
| `RegisterFleet(...)` (data + visual + UI refresh) | Split: data goes to GameManager, visual to FleetVisualController |

`FleetVisualController` is a Node, lives under MainScene. It depends on `GameSystems.Movement` (read-only) and `GameManager.Fleets` (read-only). It does not back-reference MainScene.

---

## Required Reading Before Starting

1. **The skill:** invoke `godot-4x-csharp`. Especially `references/scene-hierarchy.md` (call down / signal up / scenes self-contained / ready bottom-up).
2. **Project conventions:** [CLAUDE.md](../../CLAUDE.md). All scripts `public partial class`. `McpLog.Info/Warn/Error` not `GD.Print`. Seeded `GameRandom` only — never crypto RNG, never `GD.Randf()`. Two-tier tick (fast 0.1s, slow 1.0s).
3. **Predecessor plan:** [refactor-1.md](refactor-1.md) — context for what's already been extracted (DevHarness, OverlayRouter, CombatRouter, SelectionController) and how `GameManager` came to own the data lists.
4. **Successor plan:** skim [refactor-2-ui.md](refactor-2-ui.md) Phase 1A/1B — `IGameQuery` and EventBus intent events are partially scoped here in Phase 4 of *this* plan, with the rest landing in refactor-2.
5. **Target file as it stands now:** [src/Nodes/Map/MainScene.cs](../../src/Nodes/Map/MainScene.cs) — read end-to-end before touching anything.
6. **Memory entries:** `project_status`, `feedback_rng_testing`, `reference_eventbus_debug`.
7. **MCP loop:** every change → batch edits → `godot_reload` (windowed for screenshots) → `godot_stdout` for compile errors → `godot_screenshot` to verify.

---

## Sequencing — refactor-3 lands before refactor-2-ui

Decided. Refactor-2-ui's UI rebuild is bigger (~26h) and riskier than this refactor (~12h with Phase 4 included). Landing the system surface first means the UI rebuild targets a stable, decoupled interface from day one. `IGameQuery` becomes a thin facade over `GameSystems` + `GameManager` — no method-shuffling mid-rebuild.

**Concrete dependency this refactor introduces for refactor-2-ui:**
- `IGameQuery` interface lives in `src/Core/Services/IGameQuery.cs` (created here in Phase 4).
- `EventBus` action-request events: `ScanToggleRequested`, `ExtractToggleRequested` (created here in Phase 4).
- `GameManager` implements `IGameQuery`. Most read methods forward to `GameSystems`.
- A `SalvageActionHandler` Node listens for the action requests (created here in Phase 4).

When refactor-2-ui starts, its Phase 1A/1B is mostly done — it only needs to add any UI-specific query methods that surfaced during panel rebuilds. The Theme work (Phase 1C in refactor-2) is unchanged.

---

## UI Contract (the rule the next refactor enforces)

This refactor establishes the seam. **Refactor-2-ui will enforce it across every panel.** Stating it once here so the seam is built right:

| Direction | Mechanism | UI sees |
|---|---|---|
| **UI reads current state** | `IGameQuery` (read-only interface, implemented by `GameManager`) | An interface. No `GameSystems`, no `MainScene`. |
| **UI receives change notifications** | `EventBus` events with minimal payload (IDs + delta) | An autoload it already knows about. Subscribe in `_Ready`, unsubscribe in `_ExitTree`. |
| **UI requests action** | `EventBus` intent events: `ScanToggleRequested(poiId)`, `ExtractToggleRequested(poiId)`, `BuildOrderRequested(...)` etc. | Fires the event; never validates, never calls into systems. |
| **UI mutates game state directly** | **Forbidden.** | — |

**Round-trip example.** User clicks SCAN button:
1. UI fires `EventBus.Instance.FireScanToggleRequested(poiId)`.
2. `SalvageActionHandler` Node (sibling controller in MainScene tree) receives the event.
3. Handler validates (player empire exists, salvage system loaded) and calls `GameSystems.Salvage.RequestActivity(...)`.
4. `GameSystems.Salvage` fires `SiteActivityChanged` (via `GameSystemsHost` → EventBus).
5. Every subscribed UI panel receives `SiteActivityChanged`, re-reads `IGameQuery.GetSystemCapability(...)` for the values it needs, updates Labels.

UI knew nothing about `GameSystems` at any point. The same panel works in F6-test mode by stubbing `IGameQuery` and firing fake `EventBus` events.

**Why hybrid (events + query interface) and not pure-event:** late-subscribe panels (System View opened mid-game; Research tab clicked first time on turn 200) need *current* state, not the next change. Encoding "current state" into every event is enormous payload bloat; replaying the full event log on subscribe is impractical. Industry standard for strategy game UIs (Civ, Stellaris, Paradox) is exactly this hybrid.

---

## Phases

Four phases, each shippable independently. Verification gate is the same every time: `dotnet test` (335 passing), MCP reload + screenshot to confirm no visible regression, manual smoke (start game, scan a POI, extract, save → load → continue).

---

### Phase 1 — Create the shell *(low risk, ~2 hours)*

Stand up `GameSystems` and `GameSystemsHost` empty. Wire MainScene to instantiate the host. **Migrate one system as proof-of-concept: `FleetMovementSystem`.**

**Steps:**
1. Create `src/Core/GameSystems.cs` with empty class + the 8 system properties (all `null!` initially) + `LoadFromSetup`/`LoadFromSave`/`Tick`/`SlowTick`/`BuildSaveData` stubs.
2. Create `src/Nodes/Map/GameSystemsHost.cs` Node wrapper.
3. In `MainScene._Ready`, instantiate `_systemsHost` and add as child. Don't remove anything yet.
4. Move `FleetMovementSystem` ownership: `MainScene._movementSystem` → `GameSystems.Movement`. Move construction + signal wiring into `GameSystems.LoadFromSetup`. Re-emit `FleetArrived`/`FleetDeparted`/`OrderCompleted` from `GameSystemsHost` to EventBus.
5. Move `OnFastTick`'s movement processing into `GameSystems.Tick`. MainScene's `OnFastTick` becomes empty (and goes away after `GameSystemsHost` subscribes to FastTick directly).
6. Update `MainScene.MovementSystem` accessor to forward: `public FleetMovementSystem? MovementSystem => _systemsHost.Systems.Movement;`. Don't break anything that reads it yet.
7. Update `LoadGame` save-restore to call `Systems.LoadFromSave` for movement orders.

**Gate:** all tests pass; MCP smoke (fleet moves to right-clicked system; arrives correctly; path indicator updates).

---

### Phase 2 — Migrate remaining systems *(medium risk, ~4 hours)*

Move the remaining 7 systems into `GameSystems`. One commit per system.

**Order (least → most coupled):**
1. **`ResourceExtractionSystem`** — only used in LoadGame/BuildGameSaveData. Move state + add/remove methods into `GameSystems.Extraction`. Migrate save/restore.
2. **`SettlementSystem`** — owns colonies + production. Move `InitSettlements` → `GameSystems.LoadColonies`. Move `RegisterColony` → `GameSystems.AddColony` (returns bool). Re-emit `BuildingCompleted`/`PopulationGrew`.
3. **`StationSystem` + `_stations`** — **delete `_stations` entirely.** It's duplicate state; `StationSystem` already owns its stations. Move `InitStations` → `GameSystems.LoadStations`. Move `RegisterStation` → `GameSystems.AddStation`. Update `BuildGameSaveData` to enumerate `Systems.Stations.AllStations` instead of `_stations`.
4. **`TechRegistry` + `ResearchEngine` + `_researchStates`** — move into `GameSystems`. `InitResearch` → `GameSystems.LoadResearch`. `PlayerResearchState` becomes a method: `Systems.GetResearchState(empireId)`. The current `MainScene.PlayerResearchState` accessor forwards to `Systems.GetResearchState(GameManager.Instance.LocalPlayerEmpire?.Id ?? -1)` for the duration; refactor-2-ui Phase 1A pushes this down into IGameQuery and the accessor dies.
5. **`ExplorationManager`** — move construction + event re-emission to `GameSystems.LoadExploration`.
6. **`SalvageSystem`** — depends on Exploration, Galaxy, Movement (for `NotifyFleetMovedSystem`). Move `InitSalvage` → `GameSystems.LoadSalvage`. **Move `GetSystemCapability`, `GetSystemActiveCount`, `FindPOI`, `GetSalvageSite`, `RequestActivity` onto `GameSystems`.** MainScene's `TryToggleScan`/`TryToggleExtract` become one-liner forwards (they die in refactor-2-ui Phase 1B anyway).
7. **`UpdateTopBarDelta`** — this one's awkward. It computes per-second income from active extractions and pushes to `_topBar`. Two options:
   - **Keep on MainScene** for now; reads `Systems.Salvage` instead of `_salvageSystem`. (Simpler. The function dies when TopBar moves to subscribing directly to `IGameQuery.GetCurrentIncome()` or similar in refactor-2-ui.)
   - **Move to `GameSystems.GetCurrentIncome(empireId)`** as a query method; TopBar calls it on a slow-tick subscription. (Cleaner but couples migration to TopBar refactor.)
   - Recommend the first.

**Gate after each system move:** all tests pass; MCP smoke covering the migrated system specifically (e.g., after Salvage migration: scan a POI, see capability indicator update; extract, see income tick).

**Field count on MainScene after Phase 2:** 9 (down from 17). Methods deleted: ~10. Accessors that simply forward to `_systemsHost.Systems.X` — kept temporarily for refactor-2-ui to migrate.

---

### Phase 3 — Extract FleetVisualController + slim MainScene *(low risk, ~3 hours)*

Move scene-graph fleet visuals out. After this, MainScene is bootstrap + UI panel construction (the latter dies in refactor-2-ui).

**Steps:**
1. Create `src/Nodes/Map/FleetVisualController.cs`. Owns `_fleetContainer`, `_fleetNodes`, `SpawnFleetNodes`. Has `Configure(GameSystems systems, StrategyCameraRig cameraRig)`. Subscribes to `EventBus.FastTick` for position updates. Subscribes to a new `GameSystems` event (or reads `Systems.Fleets` and reconciles diff each call to `RegisterFleet`).
2. Move `RegisterFleet`'s visual half (FleetNode creation + label update + dictionary insert) into `FleetVisualController.AddFleetVisual(FleetData, isPlayer)`. The data half stays with `GameManager.AddFleetData`. Wrap both behind a single helper on… **either** `MainScene.RegisterFleet` (kept as orchestrator) **or** a new `GameSystems.SpawnFleet` that emits an event the visual controller listens for. Recommend the first — RegisterFleet is the right place for "data + visuals + UI refresh" composition.
3. Update `SelectionController.Configure` signature: it currently takes `Dictionary<int, FleetNode>` directly. Change to take `FleetVisualController` and read `_fleetVisuals.GetNode(fleetId)` per call. Avoids exposing the dictionary.
4. Move `OnFastTick`'s fleet position update loop from MainScene into `FleetVisualController`. MainScene's `OnFastTick` becomes empty and is deleted (FastTick subscription too).
5. Delete `MainScene._fleetContainer`, `_fleetNodes`, `SpawnFleetNodes`, the FastTick subscription in `_Ready`/`_ExitTree`.
6. **Audit MainScene fields and accessors.** After this phase the file should be:
   - Fields: `_uiLayer`, `_galaxyMap`, `_cameraRig`, `_systemsHost`, `_fleetVisuals`, `_devHarness`, `_overlayRouter`, `_combatRouter`, `_selectionController`, plus UI panel refs.
   - Public surface: `Systems` (single getter to `_systemsHost.Systems`), the existing UI helpers slated for refactor-2-ui Phase 1, `LoadGame`, `BuildGameSaveData`, `RegisterFleet`/`RegisterColony`/`RegisterStation` (orchestrators that combine GameManager + GameSystems + visuals).
   - File length: ~300 lines (down from 761).

**Gate:** all tests pass; MCP smoke (game starts, fleet visible at home system, fleet moves on right-click, attack popup works, save→load resumes correctly).

---

### Phase 4 — UI seam: IGameQuery + intent events *(low risk, ~3 hours)*

Build the seam the existing UI panels will start using *now*, and that refactor-2-ui will enforce on every rebuilt panel. Existing panels keep working through this phase — only their input/output channel changes.

**4A — `IGameQuery` interface.**
1. Create `src/Core/Services/IGameQuery.cs`. Plain C#, no Godot deps:
   ```csharp
   public interface IGameQuery
   {
       EmpireData? PlayerEmpire { get; }
       EmpireResearchState? PlayerResearchState { get; }
       IReadOnlyList<FleetData> Fleets { get; }
       IReadOnlyList<EmpireData> Empires { get; }
       IReadOnlyDictionary<int, ShipInstanceData> ShipsById { get; }
       GalaxyData? Galaxy { get; }

       float GetSystemCapability(int poiId, SiteActivity type);
       int   GetSystemActiveCount(int poiId, SiteActivity type);
       SalvageSiteData? GetSalvageSite(int siteId);
       POIData? FindPOI(int poiId, out int systemId);
       SiteActivity GetSiteActivity(int empireId, int poiId);

       TechTreeRegistry? TechRegistry { get; }
       EmpireResearchState? GetResearchState(int empireId);
   }
   ```
2. Make `GameManager` implement `IGameQuery`. Most properties already exist; the salvage/tech methods forward to `GameSystems` (which `GameManager` has a reference to via a setter `SetGameSystems(GameSystems)` called from `GameSystemsHost._Ready`). One-line forwards.
3. Migrate readers in **existing** UI panels from `_main.X` to `GameManager.Instance.X`. Pure find-and-replace — no logic changes. Panels still get a working query surface; they don't yet use `IGameQuery` as a typed reference because they're getting deleted in refactor-2-ui anyway. The interface exists for refactor-2-ui to depend on cleanly.

**4B — Intent events on EventBus.**
1. Add to `src/Autoloads/EventBus.cs`:
   ```csharp
   public event Action<int>? ScanToggleRequested;
   public event Action<int>? ExtractToggleRequested;
   public void FireScanToggleRequested(int poiId) => ScanToggleRequested?.Invoke(poiId);
   public void FireExtractToggleRequested(int poiId) => ExtractToggleRequested?.Invoke(poiId);
   ```
   Add matching `Hook` lines in `AttachDebugSubscriberIfEnabled` per CLAUDE.md.
2. Create `src/Nodes/Map/SalvageActionHandler.cs` — Node, sibling of other controllers:
   ```csharp
   public partial class SalvageActionHandler : Node
   {
       private GameSystems _systems = null!;
       public void Configure(GameSystems systems) => _systems = systems;

       public override void _Ready()
       {
           EventBus.Instance.ScanToggleRequested += OnScanToggle;
           EventBus.Instance.ExtractToggleRequested += OnExtractToggle;
       }
       public override void _ExitTree() { /* unsubscribe */ }

       private void OnScanToggle(int poiId) { /* validate + call _systems.Salvage.RequestActivity */ }
       private void OnExtractToggle(int poiId) { /* same */ }
   }
   ```
3. Register `SalvageActionHandler` in `MainScene._Ready` after `_systemsHost` is configured.
4. Migrate existing UI button handlers in `RightPanel.cs` from `_main.TryToggleScan(poiId)` → `EventBus.Instance.FireScanToggleRequested(poiId)`. Same for extract.
5. **Delete `MainScene.TryToggleScan` and `MainScene.TryToggleExtract`.**

**Gate:** all tests pass; MCP smoke covering the full SCAN→capability→extract→income round-trip (verify income tick increments after extract starts; verify cancel returns capability to baseline).

**File-size check:** MainScene should now be ~300 lines. The remaining UI-helper accessors (`SalvageSystem`, `MovementSystem`, `SettlementSystem`, `StationSystem`, `TechRegistry`, `ResearchEngine`) become forward-only and slated for deletion in refactor-2-ui. They survive this refactor unchanged because the existing imperative panels still use them.

---

## What This Refactor Does NOT Do

- **Does not touch UI panels.** All UI work is refactor-2-ui's domain. This refactor only changes which object UI panels read from (`_main.X` → `_main.Systems.X` or `IGameQuery`).
- **Does not change save format.** `GameSaveData` is unchanged. Only the code that reads/writes it moves.
- **Does not touch GameManager's data ownership** — Empires/Fleets/Ships/Colonies/StationDatas stay where refactor-1 put them.
- **Does not delete `RegisterFleet`/`RegisterColony`/`RegisterStation` on MainScene** — they're useful orchestrators. They get smaller (data → GameManager, system → GameSystems, visual → FleetVisualController) but the entry point on MainScene stays.
- **Does not split `GameSystems` into more pieces.** One container holding 8 systems is fine. Don't pre-fragment into `LogisticsContext`, `EconomyContext`, etc. until there's a concrete reason.

---

## Risks & Callouts

1. **Tick ordering matters.** Movement must run before Salvage's `NotifyFleetMovedSystem`-triggered rate refresh. Keep `GameSystems.Tick` explicit about order:
   ```csharp
   public void Tick(float delta) {
       Movement.ProcessTick(delta, _gameManager.Fleets);
       Salvage.ProcessTick(delta, _gameManager.Fleets, _gameManager.ShipsById, _gameManager.EmpiresById);
   }
   ```
2. **GameManager dependency.** `GameSystems` reads from `GameManager` for fleet/ship/empire collections. Two options: pass `GameManager` (or an `IGameData` interface over it) into `LoadFromSetup`/`Tick`, or have `GameSystems` accept the lists directly each tick. Recommend the interface — keeps `GameSystems` testable with a fake.
3. **Save/load is the riskiest single point.** Restoring fleet orders, extractions, research states, station modules — the order matters and it's currently spread across `LoadGame`. Plan: `GameSystems.LoadFromSave` does it all, in this order: `LoadColonies` → `LoadStations` → `LoadResearch` → `LoadExploration` → `LoadSalvage` → `LoadMovement` (orders need fleets and galaxy, both already in GameManager) → `LoadExtraction`. Test by saving, loading, ticking 10 seconds, comparing state hash.
4. **`UpdateTopBarDelta` cross-cutting.** Currently runs on `SlowTick` and on every `SiteActivityChanged` / `SiteActivityRateChanged`. This is the only piece of MainScene that genuinely orchestrates between Salvage events and UI. Either keep it on MainScene (reading `Systems.Salvage`), or move it onto a small `IncomeProjector` service inside `GameSystems`. Recommend kept on MainScene for this refactor; refactor-2-ui's TopBar rebuild will replace it.
5. **Don't break determinism.** The RNG seeding sequence inside `OnSetupConfirmed` (master → player → hostile → research) must produce the same world after the refactor. Keep the exact `DeriveChild` chain. Add a regression test: generate galaxy + empires twice with same seed; assert identical state.

---

## Out of Scope / Future Refactors

- Splitting `GameSystems` into smaller context objects (`CombatContext`, `EconomyContext`, etc.) — only if it grows past ~600 lines.
- Making `GameSystems` an autoload — keep it scene-scoped for now. The lifetime is "while a game is loaded," not "the entire process."
- Replacing GameManager's data lists with `GameSystems`-owned state — they're separate concerns. GameManager is the **data store** (serialization-friendly DTOs); GameSystems is the **behavior** (computes over those DTOs). Keep the seam.
