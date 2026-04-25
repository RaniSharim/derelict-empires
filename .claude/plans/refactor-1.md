# Refactor 1 — MainScene.cs Decomposition + UI Rebuild

**Status:** Phase 1 complete (uncommitted). Phases 2–9 pending.
**Created:** 2026-04-25
**Revised:** 2026-04-25 — UI work split into 3 phases after realizing panels need rebuild, not just repackaging.
**Target file:** [src/Nodes/Map/MainScene.cs](../../src/Nodes/Map/MainScene.cs) (1341 → currently 1253 lines)
**Goal state:** MainScene ~300 lines of scene assembly. UI panels are `.tscn` files with thin scripts. Logic in sibling Node controllers and `Core/`.

## Motivation

Two problems, not one:

**A. MainScene is a god class** (1341 lines, 8 responsibilities):
- Game data ownership (duplicating GameManager)
- Selection state + path indicator
- Combat routing + battle visuals
- Overlay routing (tech tree, designer, system view)
- System initialization
- Save/load
- Dev seeds + debug input
- UI panel construction in code

**B. The UI panels themselves are wrong**, not just packaged wrong:
- [LeftPanel](../../src/Nodes/UI/LeftPanel.cs) is 652 lines of imperative C# UI construction
- [RightPanel](../../src/Nodes/UI/RightPanel.cs) is 942 lines
- Tab strip hand-rolled at [LeftPanel.cs:127-149](../../src/Nodes/UI/LeftPanel.cs#L127-L149) instead of using `TabContainer` / `TabBar`
- Theme styling per-control (`AddThemeColorOverride`, `AddThemeStyleboxOverride`) instead of a project-wide `Theme` resource
- No `.tscn` files for any panel — anchors and containers all in code
- F6-testing impossible
- Each panel reaches back via `SetMainScene(this)` — call-up coupling

The `godot-4x-csharp` skill is explicit: *"A scene must work when instanced alone (F6). If it crashes without a specific parent, the architecture is wrong."* None of these UIs pass that test.

## Phases

Nine phases, ordered by **risk** (low → high) and **dependency**. Each phase is independently shippable with `dotnet test` + MCP reload + screenshot as verification gates.

---

### Phase 1 — Cut dead code ✅ *(complete, uncommitted)*

Pure deletion. Behavior unchanged.

**Done:**
- Removed 7 unused fields from MainScene (`_setupDialog`, `_fleetInfoPanel`, `_resourcePanel`, `_systemResourceView`, `_colonyPanel`, `_stationPanel`, `_researchPanel`, `_incomeUpdateCounter`)
- Removed 30-line panel construction block in `_Ready`
- Removed `if (_setupDialog != null)` branch in `LoadGame`
- Removed `InitExtractions` method (unreachable from new-game path)
- Removed 4 commented-out blocks in Init* methods
- Deleted 8 orphaned panel class files (810 lines): `FleetInfoPanel.cs`, `ResourcePanel.cs`, `SystemResourceView.cs`, `ColonyPanel.cs`, `StationPanel.cs`, `ResearchPanel.cs`, `GameSetupDialog.cs`, `SystemTooltip.cs`
- Deleted dead E2E test `Station_PanelVisibleInSceneTree`

**Net:** −911 lines, +7 lines. MainScene 1341 → 1253.

**Gate passed:** 0 build errors, 335/335 tests pass, MCP windowed reload visual parity confirmed.

**Suggested commit:** `refactor(mainscene): phase 1 — cut dead code`

---

### Phase 2 — Extract `DevHarness` *(low risk, ~2 hours)*

New `src/Nodes/Map/DevHarness.cs` (Node, child of MainScene). Self-contained debug surface.

- Move methods: `DevSeedHomeColony`, `DevGrantShipSubsystems`, `DevSpawnHostileAndAttack`, `DevSpawnHostileAndAttackAuto`.
- Move debug input: F7, Shift+B, Ctrl+Shift+B from `_UnhandledInput`.
- Move screenshot/showcase/fullscreen: F10, F11, F12 — these are debug overlays, not gameplay.
- Add `[Export] private DevHarness _dev` or `GetNode<DevHarness>("DevHarness")` in `_Ready`.
- DevHarness needs read access to game data — pass via `Configure(MainScene)` for now (Phase 6 replaces this).

**Gate:** All F-key debug shortcuts still work. `_UnhandledInput` shrinks to ~15 lines.

---

### Phase 3 — Extract `OverlayRouter` *(low risk, ~2 hours)*

New `src/Nodes/Map/OverlayRouter.cs`. Owns transient full-screen overlays.

- Move state: `_activeTechTreeOverlay`, `_activeDesignerOverlay`, `_systemView`.
- Move handlers: `OnTechTreeOpenRequested`, `OnDesignerOpenRequested`, `OnSystemSelectedForView`, `OnSystemViewClosed`, `ApplySystemViewContext`.
- Subscribes/unsubscribes to four EventBus events itself.
- Needs `_uiLayer` reference + context provider for SystemView. Inject via `Configure(CanvasLayer uiLayer, ISystemViewContext ctx)`.

**Gate:** T opens tech tree; Shift+D opens designer; system double-click opens SystemView.

---

### Phase 4 — Extract `CombatRouter` *(low risk, ~2 hours)*

New `src/Nodes/Map/CombatRouter.cs`. Owns BattleManager + visuals.

- Move state: `_battleManager`, `_activeBattleId`, `_battleMarkers`, `_activeCombatPopup`.
- Move handlers: `OnCombatStartRequested`, `EngageCombat`, `OnBattleEndedInternal`.
- Subscribes to `EventBus.CombatStartRequested`.
- Inject: `_uiLayer`, `_cameraRig`, lookup-callbacks for fleets/empires/ships.

**Gate:** Ctrl+Shift+B spawns hostile + auto-engages; popup renders; debrief works.

---

### Phase 5 — Extract `SelectionController` *(medium risk, ~3 hours)*

New `src/Nodes/Map/SelectionController.cs`. Largest extraction by surface area but bounded.

- Move state: `_selection`, `_selectedFleetIds`, `_primarySelectedFleetId`, `_pathIndicator`.
- Move handlers: `OnFleetSelected`, `OnFleetSelectionToggled`, `OnFleetDeselected`, `OnFleetDoubleClicked`, `OnSystemRightClickedForMove`, `UpdatePathIndicator`.
- Move Escape-key fleet-deselect from `_UnhandledInput`.
- Public API: `SelectedFleetId`, `SelectedFleetIds`, `PathIndicator`.
- Inject: `_fleetNodes`, `_movementSystem`, `_cameraRig`, lookups for `_fleets`/`_empires`.

**Gate:** Click-select, Ctrl-click multi-select, double-click pan, right-click move, Escape deselect — all working.

---

### Phase 6 — Consolidate game state + `IGameQuery` *(high risk, ~4 hours)*

Single source of truth + queryable interface. Prerequisite for Phases 7–9.

- Move `_empires`, `_fleets`, `_ships`, `_colonyDatas`, `_stationDatas`, `_empiresById`, `_shipsById` from MainScene → `GameManager`.
- Define `src/Core/Services/IGameQuery.cs` (plain C#, no Godot deps): `GetSystemCapability`, `GetSystemActiveCount`, `FindPOI`, `GetSalvageSite`, `PlayerEmpire`, `PlayerResearchState`.
- `GameManager` implements `IGameQuery`.
- Add EventBus request events: `ScanToggleRequested(int poiId)`, `ExtractToggleRequested(int poiId)`. New `SalvageActionHandler` Node subscribes and calls `_salvageSystem`.
- Update controllers from Phases 2–5 to read through GameManager instead of MainScene.
- Save/load (`BuildGameSaveData` / `LoadGame`) reads/writes via GameManager.
- Delete the "owned here, mirrored to GameManager" comment when its lie becomes truth.
- Existing panels keep their `SetMainScene(this)` for now — Phase 8 rewrites them anyway.

**Gate:** Save → load → state matches. New-game path produces identical state. All 335 tests pass.

---

### Phase 7 — Project-wide `Theme.tres` *(medium risk, ~3 hours)*

Eliminate per-control theming. Establish the foundation for Phases 8–9.

- Create `resources/ui/theme.tres` in the editor with default styles for: `Button`, `Panel`, `PanelContainer`, `Label`, `LineEdit`, `TabContainer`, `ProgressBar`.
- Define color tokens in theme constants (Accent, TextBody, TextMuted, BgPanel, BgPanelDeep) using existing [UIColors](../../src/Nodes/UI/UIColors.cs) palette.
- Define font defaults using [UIFonts](../../src/Nodes/UI/UIFonts.cs) families and the 16/14/12 size tier.
- Set `ProjectSettings → gui/theme/custom` to `theme.tres` so it applies project-wide.
- Optional: separate themes per faction-color variant (`theme_red.tres`, `theme_blue.tres`) for tech tree overlays.
- Strip `AddThemeColorOverride` / `AddThemeStyleboxOverride` calls from existing panels where the new defaults match. Keep overrides only where genuinely intentional (e.g., faction-tinted accents).

**Gate:** Visual parity with Phase 6 baseline. Existing 600–900-line panels lose ~100 lines each of styling boilerplate.

---

### Phase 8 — Rebuild top-level UI panels as `.tscn` *(high risk, ~10 hours, biggest phase)*

The real fix for the "horrible" state. Each panel becomes a thin script attached to an editor-authored scene.

**Per-panel work:**
- **TopBar** → `scenes/ui/top_bar.tscn`. HBoxContainer for resource boxes. Existing 5×4 precursor token layout becomes editor-anchored. Script shrinks to data binding only.
- **LeftPanel** → `scenes/ui/left_panel.tscn` with **`TabContainer`** instead of hand-rolled tab strip. Each tab is its own scene: `fleets_tab.tscn`, `research_tab.tscn`, `build_tab.tscn`, (colonies tab placeholder for now). Target: <200 lines per tab script.
- **RightPanel** → `scenes/ui/right_panel.tscn`. Context-driven sub-scenes per selection type. Replace `SetMainScene(this)` with `[Export] IGameQuery` (or autoload access) + EventBus signals.
- **SpeedTimeWidget** → `scenes/ui/speed_time_widget.tscn`
- **EventLog** → `scenes/ui/event_log.tscn`
- **Minimap** → `scenes/ui/minimap.tscn`
- Composite: `scenes/ui/main_ui.tscn` — CanvasLayer with all of the above as instanced children, anchors set in editor.
- MainScene exposes `[Export] PackedScene _mainUiScene` and instantiates once. Drop all the `new TopBar { Name = "TopBar" }` calls.

**Wiring rules (enforced in this phase):**
- No panel may hold a `MainScene` back-pointer.
- Panel→system communication: EventBus events only.
- Panel→data reads: `GameManager.Instance` (which implements IGameQuery from Phase 6).
- Use `%UniqueName` for cross-panel refs MainScene/controllers need.

**Gate:** Visual screenshot matches Phase 7. Each panel scene F6-runs standalone without erroring. LeftPanel.cs ≤200 lines, RightPanel.cs ≤200 lines.

---

### Phase 9 — Rebuild SystemView as `.tscn` *(high risk, ~6 hours)*

Same treatment for the per-system drill-down.

- **SystemViewScene** → `scenes/ui/system_view/system_view.tscn` (currently 457 lines of code-built UI)
- **RightPanelController** → `scenes/ui/system_view/right_panel_controller.tscn` (281 lines)
- **Entity panels** as scenes:
  - `colony_entity_panel.tscn`
  - `station_entity_panel.tscn`
  - `outpost_entity_panel.tscn`
  - `salvage_entity_panel.tscn`
  - `enemy_entity_panel.tscn`
- Per-row components: `band_row.tscn`, `building_row.tscn`, `slot_chip.tscn`, `sub_ticket_row.tscn`, `poi_card.tscn`, `fleet_poi_card.tscn`.

**Gate:** All System View interactions still work — POI clicks, slot chips, building queue, sub-tickets, tab strip, fleet mooring.

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
src/Autoloads/
  GameManager.cs            owns game data, implements IGameQuery
src/Core/Services/
  IGameQuery.cs             new
src/Nodes/UI/
  *.cs                      thin scripts attached to .tscn files (≤200 lines each)
resources/ui/
  theme.tres                project-wide
scenes/ui/
  main_ui.tscn
  top_bar.tscn
  left_panel.tscn
  right_panel.tscn
  fleets_tab.tscn
  research_tab.tscn
  build_tab.tscn
  speed_time_widget.tscn
  event_log.tscn
  minimap.tscn
scenes/ui/system_view/
  system_view.tscn
  right_panel_controller.tscn
  colony_entity_panel.tscn
  station_entity_panel.tscn
  outpost_entity_panel.tscn
  salvage_entity_panel.tscn
  enemy_entity_panel.tscn
  ... + per-row sub-scenes
```

## Sequencing notes

- **Phases 2–5 in any order**; they're independent extractions from MainScene.
- **Phase 6 before 7–9** — query interface and data ownership must exist before panels can be rewritten to use them.
- **Phase 7 before 8–9** — Theme.tres makes the panel rewrites less verbose.
- **One PR per phase.** Don't bundle. UI phases (8, 9) may need internal sub-PRs per-panel given size.

## Verification protocol per phase

1. `dotnet build` — must succeed
2. `dotnet test tests/DerlictEmpires.Tests.csproj` — 335+ passing
3. `godot_reload` (windowed)
4. `godot_stdout` — no compile errors
5. `godot_screenshot` — visual parity with pre-phase baseline
6. Manual smoke: relevant feature for the phase still works
7. Commit with message: `refactor(mainscene): phase N — <short title>`

## Risk callouts

- **Phase 8 is the danger phase.** Rewriting 1500+ lines of imperative UI as `.tscn` files risks visual regression. Mitigations: per-panel sub-PRs, before/after screenshots in each, keep the old panel file in git history for diffing.
- **Phase 6 may surface latent bugs** in save/load if MainScene and GameManager were silently disagreeing on data. Run the full E2E save/load fixtures.
- **Phase 7 (Theme.tres) might require touching every existing panel** to remove now-redundant overrides. Scope it tight: only remove overrides whose value matches the new theme default.
