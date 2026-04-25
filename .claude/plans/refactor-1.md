# Refactor 1 — MainScene.cs Decomposition

**Status:** Planned, not started
**Created:** 2026-04-25
**Target file:** [src/Nodes/Map/MainScene.cs](../../src/Nodes/Map/MainScene.cs) (1341 lines)
**Goal state:** ~300 lines of scene assembly + lifecycle. Logic lives in sibling Node controllers and `Core/`.

## Motivation

MainScene has eight responsibilities and 1341 lines:
- Game data ownership (duplicating GameManager)
- Selection state + path indicator
- Combat routing + battle visuals
- Overlay routing (tech tree, designer, system view)
- System initialization (movement, salvage, settlements, stations, research, extractions)
- Save/load
- Dev seeds + debug input
- UI panel construction in code

The `godot-4x-csharp` skill explicitly endorses a `Main.cs` glue scene, but warns against god classes ("split: TurnManager, AudioManager, etc."). This file is past the breaking point. UI panels reach back via `SetMainScene(this)` — call-up coupling the skill flags as anti-pattern.

## Phases

Eight phases, ordered by **risk** (low → high) and **dependency**. Each phase is independently shippable, with `dotnet test` + MCP reload + screenshot as verification gates.

---

### Phase 1 — Cut dead code *(no risk, ~1 hour)*

Pure deletion. No behavior change.

- [MainScene.cs:160-184](../../src/Nodes/Map/MainScene.cs#L160-L184) Remove created-and-hidden panels: `_fleetInfoPanel`, `_resourcePanel`, `_systemResourceView`, `_colonyPanel`, `_stationPanel`, `_researchPanel`. Verify each by grep — only their declarations + the SetData/Show calls inside Init* should reference them.
- [MainScene.cs:33](../../src/Nodes/Map/MainScene.cs#L33) Remove `_setupDialog` field + the `if (_setupDialog != null)` branch in [LoadGame](../../src/Nodes/Map/MainScene.cs#L477) — the field is never assigned.
- [MainScene.cs:625-650](../../src/Nodes/Map/MainScene.cs#L625-L650) Decide: either call `InitExtractions` from the new-game path, or delete it. Currently only `LoadGame` instantiates `_extractionSystem`, so new-game salvage works but raw resource extraction does not. **Recommend delete** — `_salvageSystem` covers MVP needs.
- [MainScene.cs:147](../../src/Nodes/Map/MainScene.cs#L147) Remove the `tooltip` `SystemTooltip` if also dead-on-arrival (verify usage).

**Gate:** `dotnet test` (261 passing), `godot_reload`, screenshot — galaxy renders, fleets selectable.

---

### Phase 2 — Extract `DevHarness` *(low risk, ~2 hours)*

New `src/Nodes/Map/DevHarness.cs` (Node, child of MainScene). Self-contained debug surface.

- Move methods: `DevSeedHomeColony`, `DevGrantShipSubsystems`, `DevSpawnHostileAndAttack`, `DevSpawnHostileAndAttackAuto`.
- Move debug input: F7, Shift+B, Ctrl+Shift+B from `_UnhandledInput`.
- Move screenshot/showcase/fullscreen: F10, F11, F12 — these are debug overlays, not gameplay.
- Add `[Export] private DevHarness _dev` or `GetNode<DevHarness>("DevHarness")` in `_Ready`.
- DevHarness needs read access to `_empires`, `_fleets`, `_ships`, `_settlementSystem`, `_stationSystem` — pass via `Configure(MainScene)` for now (Phase 8 will replace this).

**Gate:** All F-key debug shortcuts still work. `_UnhandledInput` shrinks to ~15 lines.

---

### Phase 3 — Extract `OverlayRouter` *(low risk, ~2 hours)*

New `src/Nodes/Map/OverlayRouter.cs`. Owns transient full-screen overlays.

- Move state: `_activeTechTreeOverlay`, `_activeDesignerOverlay`, `_systemView`.
- Move handlers: `OnTechTreeOpenRequested`, `OnDesignerOpenRequested`, `OnSystemSelectedForView`, `OnSystemViewClosed`, `ApplySystemViewContext`.
- Subscribes/unsubscribes to four EventBus events itself.
- Needs `_uiLayer` reference + context provider for SystemView (colonies/outposts/stations/fleets/galaxy/playerEmpireId). Inject via `Configure(CanvasLayer uiLayer, ISystemViewContext ctx)` — `MainScene` implements the interface temporarily.

**Gate:** T opens tech tree; Shift+D opens designer; system double-click opens SystemView. Close handlers fire.

---

### Phase 4 — Extract `CombatRouter` *(low risk, ~2 hours)*

New `src/Nodes/Map/CombatRouter.cs`. Owns BattleManager + visuals.

- Move state: `_battleManager`, `_activeBattleId`, `_battleMarkers`, `_activeCombatPopup`.
- Move handlers: `OnCombatStartRequested`, `EngageCombat`, `OnBattleEndedInternal`.
- Subscribes to `EventBus.CombatStartRequested`.
- Inject: `_uiLayer`, `_cameraRig`, lookup-callbacks for fleets/empires/ships (or pass full registries via Configure).

**Gate:** Ctrl+Shift+B spawns hostile + auto-engages; popup renders; debrief works.

---

### Phase 5 — Extract `SelectionController` *(medium risk, ~3 hours)*

New `src/Nodes/Map/SelectionController.cs`. Largest extraction by surface area but bounded.

- Move state: `_selection`, `_selectedFleetIds`, `_primarySelectedFleetId`, `_pathIndicator`.
- Move handlers: `OnFleetSelected`, `OnFleetSelectionToggled`, `OnFleetDeselected`, `OnFleetDoubleClicked`, `OnSystemRightClickedForMove`, `UpdatePathIndicator`.
- Move Escape-key fleet-deselect from `_UnhandledInput`.
- Public API: `SelectedFleetId`, `SelectedFleetIds`, `PathIndicator`.
- Inject: `_fleetNodes` (Dictionary ref), `_movementSystem`, `_cameraRig`, lookups for `_fleets`/`_empires`.

**Gate:** Click-select, Ctrl-click multi-select, double-click pan, right-click move, Escape deselect — all working. `_UnhandledInput` is now ~3 lines (just a fallthrough or empty).

---

### Phase 6 — UI as scenes *(medium risk, ~4 hours)*

Convert in-code panel instantiation to `.tscn` files.

- Create `scenes/ui/main_ui.tscn` with CanvasLayer root and child Controls: TopBar, LeftPanel, RightPanel, SpeedTimeWidget, EventLog, Minimap.
- Each panel becomes its own scene with anchors set in editor.
- MainScene exposes `[Export] PackedScene _mainUiScene` and instantiates once in `_Ready`.
- Use `%UniqueName` for panels MainScene/controllers need to reach (e.g., `%TopBar`, `%LeftPanel`).
- Drop the `new TopBar { Name = "TopBar" }` style throughout.
- Verify F6 on each panel scene shows it standalone (skill rule: "scenes are self-contained").

**Gate:** Visual screenshot identical to pre-refactor. Each panel scene F6-runs without erroring.

---

### Phase 7 — Consolidate game data ownership *(high risk, ~4 hours)*

Single source of truth.

- Move `_empires`, `_fleets`, `_ships`, `_colonyDatas`, `_stationDatas`, `_empiresById`, `_shipsById` to `GameManager` (already an autoload).
- Add convenience properties: `GameManager.Instance.Fleets`, `EmpiresById`, etc.
- Update all reads in MainScene and the new controllers to go through GameManager.
- Save/load (`BuildGameSaveData` / `LoadGame`) reads/writes via GameManager too.
- Delete the "owned here, mirrored to GameManager" comment when its lie becomes truth.

**Gate:** Save → load → state matches. New-game path produces identical state. All 261 tests pass.

---

### Phase 8 — Replace `SetMainScene` with EventBus + query interface *(high risk, ~4 hours)*

Eliminate child→parent back-pointers.

- Define `src/Core/Services/IGameQuery.cs`: `GetSystemCapability`, `GetSystemActiveCount`, `FindPOI`, `GetSalvageSite`, `PlayerEmpire`, `PlayerResearchState`. Plain C#, no Godot dependency.
- `GameManager` implements `IGameQuery` (or inject a separate `GameQueryService` autoload).
- Add EventBus request events: `ScanToggleRequested(int poiId)`, `ExtractToggleRequested(int poiId)`. A new `SalvageActionHandler` Node subscribes and calls `_salvageSystem`.
- Panels stop calling `_main.TryToggleScan(poiId)` — they fire the request event.
- Panels stop calling `_main.GetSystemCapability(...)` — they query `GameManager.Instance.GetSystemCapability(...)`.
- Remove `LeftPanel.SetMainScene`, `RightPanel.SetMainScene`, `ResearchStrip.Configure(MainScene)`. Replace with `Configure(IGameQuery)` or pure EventBus.
- Remove the public accessors at [MainScene.cs:818-829](../../src/Nodes/Map/MainScene.cs#L818-L829).

**Gate:** SCAN/EXTRACT buttons still work. Tech tree strip still updates. No panel holds a `MainScene` reference.

---

## Final state

```
src/Nodes/Map/
  MainScene.cs              ~300 lines: scene assembly, Init*, signal wiring
  DevHarness.cs             ~200 lines
  OverlayRouter.cs          ~150 lines
  CombatRouter.cs           ~180 lines
  SelectionController.cs    ~250 lines
  GalaxyMap.cs              (unchanged)
src/Core/Services/
  IGameQuery.cs             new
scenes/ui/
  main_ui.tscn              new
  top_bar.tscn              new
  left_panel.tscn           new
  ...
```

## Sequencing notes

- **Phases 1–4 in any order** after 1; they're independent.
- **Phase 5 before 6** so SelectionController can be wired before UI scenes get re-anchored.
- **Phase 7 before 8** — query interface depends on GameManager owning the data.
- **One PR per phase.** Tests + MCP screenshot in each. Don't bundle.

## Verification protocol per phase

1. `dotnet build` — must succeed
2. `dotnet test tests/DerlictEmpires.Tests.csproj` — 261+ passing
3. `godot_reload` (windowed)
4. `godot_stdout` — no compile errors
5. `godot_screenshot` — visual parity with pre-phase baseline
6. Manual smoke: relevant feature for the phase still works
7. Commit with message: `refactor(mainscene): phase N — <short title>`
