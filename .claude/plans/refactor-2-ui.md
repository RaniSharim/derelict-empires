# Refactor 2 — UI Rebuild

**Status:** Not started. Hand-off plan for an agent picking this up cold.
**Created:** 2026-04-25
**Predecessor:** [refactor-1.md](refactor-1.md) — completed Phases 1–6 (extraction of DevHarness/OverlayRouter/CombatRouter/SelectionController and migration of game data to GameManager). MainScene is now ~761 lines (down from 1341). Game logic is properly decoupled. **Now the UI itself needs a real rebuild.**

---

## Why This Plan Exists

The active UI is built **imperatively in C# `_Ready()` methods** with no `.tscn` scenes. Result: large, painful-to-edit panels with hand-rolled containers, theme overrides scattered, and back-pointers to MainScene.

**Sizes today (the prosecution):**
- [LeftPanel.cs](../../src/Nodes/UI/LeftPanel.cs) — 652 lines
- [RightPanel.cs](../../src/Nodes/UI/RightPanel.cs) — 942 lines
- [TopBar.cs](../../src/Nodes/UI/TopBar.cs) — 316 lines
- [SystemViewScene.cs](../../src/Nodes/UI/SystemView/SystemViewScene.cs) — 457 lines
- [RightPanelController.cs](../../src/Nodes/UI/SystemView/RightPanelController.cs) — 281 lines
- [POICard.cs](../../src/Nodes/UI/SystemView/POICard.cs) — 328 lines
- All entity panels + sub-rows in `src/Nodes/UI/SystemView/` — total **~3000 lines** for SystemView alone

**Specific problems:**
1. **No `.tscn` files for any panel.** Anchors, sizing, containers — all in code. F6-testing impossible. Editor inspector useless. The `godot-4x-csharp` skill explicitly says: *"A scene must work when instanced alone (F6). If it crashes without a specific parent, the architecture is wrong."* None pass.
2. **300+ `AddThemeColorOverride` / `AddThemeStyleboxOverride` calls** scattered across panels. No project-wide `Theme` resource.
3. **Tab strip hand-rolled** at [LeftPanel.cs:127-149](../../src/Nodes/UI/LeftPanel.cs#L127-L149) instead of using `TabContainer` / `TabBar`.
4. **`SetMainScene(this)` back-pointers** ([LeftPanel.cs:2](../../src/Nodes/UI/LeftPanel.cs), [RightPanel.cs:18 occurrences](../../src/Nodes/UI/RightPanel.cs), [TechTreeOverlay.cs](../../src/Nodes/UI/TechTreeOverlay.cs)) — call-up coupling the skill flags as anti-pattern.
5. **Re-implementing what Godot gives free**: Containers, TabContainer, Theme, StyleBoxes, anchors-via-inspector.

---

## Goal State

```
scenes/ui/
  main_ui.tscn              CanvasLayer composite
  top_bar.tscn              top of screen
  left_panel.tscn           left edge with TabContainer
  fleets_tab.tscn           tab content
  research_tab.tscn         tab content
  build_tab.tscn            tab content
  right_panel.tscn          right edge, context-driven
  speed_time_widget.tscn    bottom-right
  event_log.tscn            bottom-right
  minimap.tscn              bottom-left

scenes/ui/system_view/
  system_view.tscn
  right_panel_controller.tscn
  colony_entity_panel.tscn
  station_entity_panel.tscn
  outpost_entity_panel.tscn
  salvage_entity_panel.tscn
  enemy_entity_panel.tscn
  empty_state_dashboard.tscn
  poi_card.tscn
  fleet_poi_card.tscn
  band_row.tscn
  building_row.tscn
  slot_chip.tscn
  sub_ticket_row.tscn
  entity_tab_strip.tscn

resources/ui/
  theme.tres                project-wide default

src/Core/Services/
  IGameQuery.cs             new — query interface for UI

src/Nodes/UI/             scripts attached to .tscn files (≤200 lines each)
src/Nodes/UI/SystemView/  same
```

---

## Required Reading Before Starting

1. **The skill:** invoke `godot-4x-csharp`. Especially:
   - `references/scene-hierarchy.md` — golden rules: call down, signal up, scenes self-contained, ready bottom-up.
   - `references/ui-system.md` — Control nodes, anchors, Containers, size flags, Theme, common bugs.
   - `references/fonts.md` — `LabelSettings` not theme overrides for text styling, font selection decision tree.
   - `templates/GodotThemeBuilder.cs` — adapt as the `Theme.tres` factory.
2. **Project conventions:** [CLAUDE.md](../../CLAUDE.md). Notably:
   - All scripts must be `public partial class`.
   - `McpLog.Info/Warn/Error` not `GD.Print`.
   - Two-font system in [UIFonts.cs](../../src/Nodes/UI/UIFonts.cs): Exo 2 SemiBold for titles, B612 Mono Bold for body. **12px is the hard floor.**
   - Precursor color palette in [UIColors.cs](../../src/Nodes/UI/UIColors.cs): 5 colors × 4 tones (Bright/Normal/Dim/Bg) plus accent/text/status colors.
3. **MCP loop:** every change → batch edits → `godot_reload` (windowed for screenshots) → `godot_stdout` for compile errors → `godot_screenshot` to verify.
4. **Memory entries:**
   - `project_typography_colors` — font and color systems.
   - `project_ui_implementation` — current UI state by phase.
   - `project_system_view` — System View P1–P6 details.

---

## Architectural Context (current state)

### Game-state ownership (post Refactor-1 Phase 4)
**[GameManager.cs](../../src/Autoloads/GameManager.cs) owns all game data:**
- `Empires`, `Fleets`, `Ships`, `Colonies`, `StationDatas` — `List<T>`
- `EmpiresById`, `ShipsById` — index dictionaries
- `LocalPlayerEmpire` — convenience for `Empires.Find(IsHuman)`
- `Galaxy` — `GalaxyData?`
- `MasterSeed`, `GameTime`, `CurrentSpeed`, `CurrentState`
- `LoadState(...)` — bulk load
- `RegisterEmpire(...)`, `AddFleetData(...)` — incremental adds

### Game systems (still on MainScene, exposed via public properties)
- `MainScene.SalvageSystem` — salvage activity manager
- `MainScene.ExplorationManager` — POI discovery / scanning
- `MainScene.MovementSystem` — fleet movement and pathing
- `MainScene.SettlementSystem` (internal) — colonies/outposts
- `MainScene.StationSystem` (internal) — stations
- `MainScene.TechRegistry` — tech tree data
- `MainScene.ResearchEngine` — research progress
- `MainScene.PlayerResearchState` — player's research state
- `MainScene.RegisterFleet/Colony/Station(...)` — orchestration helpers (data via gm + visuals here)

### UI helpers MainScene currently exposes (these MUST go in Phase 1)
These are the back-pointer surface UI panels currently use:

```csharp
public float GetSystemCapability(int poiId, SiteActivity type);
public int   GetSystemActiveCount(int poiId, SiteActivity type);
public bool  TryToggleScan(int poiId);
public bool  TryToggleExtract(int poiId);
public SalvageSiteData? GetSalvageSite(int siteId);
public IReadOnlyList<FleetData> Fleets { get; }
public IReadOnlyDictionary<int, ShipInstanceData> ShipsById { get; }
public EmpireData? PlayerEmpire { get; }
```

### Sibling controllers (already extracted, do not modify)
- [`DevHarness`](../../src/Nodes/Map/DevHarness.cs) — debug seeds + F-key shortcuts
- [`OverlayRouter`](../../src/Nodes/Map/OverlayRouter.cs) — tech tree, designer, system view open requests
- [`CombatRouter`](../../src/Nodes/Map/CombatRouter.cs) — BattleManager + popup
- [`SelectionController`](../../src/Nodes/Map/SelectionController.cs) — fleet selection state, path indicator

### EventBus (cross-system signals)
EventBus is an autoload at `src/Autoloads/EventBus.cs`. Existing events relevant to UI:
- `SystemSelected(StarSystemData)` / `SystemDoubleClicked(StarSystemData)` / `SystemRightClicked(StarSystemData)`
- `FleetSelected(int)` / `FleetSelectionToggled(int)` / `FleetDeselected()` / `FleetDoubleClicked(int)`
- `SystemViewOpened(int)` / `SystemViewClosed()`
- `SiteDiscovered`, `ScanProgressChanged`, `SiteScanComplete`, `YieldExtracted`, `SiteActivityChanged`, `SiteActivityRateChanged`
- `FastTick(float)` / `SlowTick(float)` / `BattleTick(int)` / `SpeedChanged(GameSpeed)` / `GamePaused` / `GameResumed`
- `TechTreeOpenRequested(TechTreeOpenRequest)` / `DesignerOpenRequested(DesignerOpenRequest)`
- `CombatStartRequested(int, int)` / `CombatStarted(int)` / `CombatEnded(int, CombatResult)`
- `SubsystemResearched(int, string)` / `TierUnlocked(int, PrecursorColor, ResearchCategory, int)`

### Current UI surface (what each panel does, for context)

**[TopBar.cs](../../src/Nodes/UI/TopBar.cs) (316 lines):**
- Top-of-screen bar with empire header (name, color/origin)
- Credits + parts display
- 5 precursor-color resource boxes ([FactionResourceBox.cs](../../src/Nodes/UI/FactionResourceBox.cs))
- Embedded [ResearchStrip](../../src/Nodes/UI/ResearchStrip.cs) showing TIER/MOD progress
- Subscribes to: `YieldExtracted`, `SiteActivityChanged`, `SubsystemResearched`, `TierUnlocked`, `ResourceChanged`

**[LeftPanel.cs](../../src/Nodes/UI/LeftPanel.cs) (652 lines):**
- Left edge of screen, ~300px wide
- Tabs: **FLEETS** / COLONIES (placeholder, disabled) / **RESEARCH** / **BUILD**
- FLEETS tab: list of fleet cards showing name, faction color, location, ship count, status
- RESEARCH tab: delegates to [ResearchTabContent](../../src/Nodes/UI/ResearchTabContent.cs) — shows current research, color affinity, ETA
- BUILD tab: ship designer entry point + saved designs
- COLONIES tab: empty (planned but never built)
- Subscribes to: `FleetSelected`, `FleetDeselected`, `FleetOrderChanged`, `FleetArrivedAtSystem`, `SubsystemResearched`, `TierUnlocked`
- `SetMainScene(this)` used to call MainScene methods for fleet data

**[RightPanel.cs](../../src/Nodes/UI/RightPanel.cs) (942 lines — biggest offender):**
- Right edge, ~280px wide
- Context-sensitive content driven by selection
- When a system is selected: lists POIs with SCAN/EXTRACT buttons, capability indicators, activity status
- When a fleet is selected: fleet details (ships, role, location)
- Calls `_main.TryToggleScan(poiId)` / `TryToggleExtract(poiId)` / `GetSystemCapability(...)` / `GetSystemActiveCount(...)` heavily

**[SpeedTimeWidget.cs](../../src/Nodes/UI/SpeedTimeWidget.cs) (166 lines):**
- Bottom-right
- ×1/×2/×4/×8 speed buttons + Pause toggle
- Cycle counter ("T-N · CYCLE")
- Subscribes to: `SpeedChanged`, `GamePaused`, `GameResumed`, `FastTick`

**[EventLog.cs](../../src/Nodes/UI/EventLog.cs) (318 lines):**
- Bottom-right "Recent Events" feed
- Subscribes to most events; converts each into a styled toast line
- Auto-trims to N most recent entries

**[Minimap.cs](../../src/Nodes/UI/Minimap.cs) (167 lines):**
- Bottom-left
- 2D top-down render of galaxy + fleet positions
- Click-to-pan camera
- Subscribes to: `FastTick`, `SystemSelected`

### System View surface
[SystemViewScene.cs](../../src/Nodes/UI/SystemView/SystemViewScene.cs) is the per-system drill-down. Opens on system double-click via OverlayRouter. Shows:
- Ring/band layout of POIs (Inner/Mid/Outer bands)
- Per-POI cards: planets, asteroid fields, salvage sites, stations
- Per-entity panels (right side): colony details, station details, etc.
- Tab strip when a POI is shared between multiple owners
- Detection coverage overlay (sensor range, signature emissions)

The right panel inside SystemView ([RightPanelController.cs](../../src/Nodes/UI/SystemView/RightPanelController.cs)) shows different entity panels based on what's selected:
- [ColonyEntityPanel](../../src/Nodes/UI/SystemView/ColonyEntityPanel.cs) — pop allocation, buildings, queue
- [StationEntityPanel](../../src/Nodes/UI/SystemView/StationEntityPanel.cs) — modules, HP
- [OutpostEntityPanel](../../src/Nodes/UI/SystemView/OutpostEntityPanel.cs)
- [SalvageEntityPanel](../../src/Nodes/UI/SystemView/SalvageEntityPanel.cs)
- [EnemyEntityPanel](../../src/Nodes/UI/SystemView/EnemyEntityPanel.cs)
- [EmptyStateDashboard](../../src/Nodes/UI/SystemView/EmptyStateDashboard.cs) — when nothing selected

---

## Phase 1 — Foundation *(medium risk, ~6 hours)*

Establish the decoupling layer + Theme. No visible UI changes yet.

### 1A — Define `IGameQuery` and decouple UI calls
- Create `src/Core/Services/IGameQuery.cs`. Plain C#, no Godot deps:
  ```csharp
  public interface IGameQuery
  {
      EmpireData? PlayerEmpire { get; }
      EmpireResearchState? PlayerResearchState { get; }
      IReadOnlyList<FleetData> Fleets { get; }
      IReadOnlyDictionary<int, ShipInstanceData> ShipsById { get; }
      float GetSystemCapability(int poiId, SiteActivity type);
      int   GetSystemActiveCount(int poiId, SiteActivity type);
      SalvageSiteData? GetSalvageSite(int siteId);
      POIData? FindPOI(int poiId, out int systemId);
  }
  ```
- `GameManager` implements `IGameQuery`. Move `GetSystemCapability` / `GetSystemActiveCount` / `FindPOI` / `GetSalvageSite` from MainScene → GameManager (these read `_salvageSystem` and `Galaxy` — pass via `Configure(SalvageSystem)` from MainScene's InitSalvage).
- **`PlayerResearchState`** lives where `_researchStates` lives (currently MainScene). Either:
  - Move `_researchStates` to GameManager too, OR
  - Have GameManager store a reference to a "research provider" set by MainScene during Init.
  - Recommend the first — research state is game data.

### 1B — Add EventBus action requests for SCAN/EXTRACT
- New events on EventBus: `ScanToggleRequested(int poiId)`, `ExtractToggleRequested(int poiId)`.
- New `SalvageActionHandler` Node (sibling of MainScene controllers): subscribes to those events, calls `MainScene.SalvageSystem.RequestActivity(...)` directly.
- Delete `MainScene.TryToggleScan` and `MainScene.TryToggleExtract`.

### 1C — Project-wide `theme.tres`
- Copy `templates/GodotThemeBuilder.cs` from the skill into `src/Nodes/UI/ThemeBuilder.cs`. Adapt to current palette/fonts.
- **Generate `resources/ui/theme.tres` once** — either in editor (open the project in Godot, build a Theme resource by saving from the inspector) or programmatically via a one-off `[Tool]` script. Then commit the .tres file.
- Default styles to define:
  - `Button` — normal/hover/pressed/focus StyleBoxes using `UIColors.GlassDark` + `BorderMid`/`BorderBright` borders, font = `UIFonts.Main` @ `SmallSize`, color = `TextBody`
  - `PanelContainer` / `Panel` — `GlassDark` fill with subtle border
  - `Label` — `UIFonts.Main` @ `NormalSize`, `TextBody` color
  - `LineEdit` — match Button styling
  - `TabContainer` — tab font, active/inactive color tokens
  - `ProgressBar` — bg = `GlassDarkFlat`, fill = `Accent`
- Set `gui/theme/custom = "res://resources/ui/theme.tres"` in `project.godot`.
- **Do NOT** strip overrides from existing imperative panels yet — they'll be deleted in Phases 2/3.

**Gate:** 335/335 tests pass. No visible regression. `IGameQuery` instantiable from tests. Existing UI still works (overrides take precedence over Theme defaults).

---

## Phase 2 — Galaxy-map UI as scenes *(high risk, ~12 hours)*

Rebuild the always-visible HUD as `.tscn` files. Each panel becomes ≤200 lines of script attached to an editor-authored scene.

### Scope
Six panels + one composite:
- `top_bar.tscn`
- `left_panel.tscn` (with tab sub-scenes)
- `right_panel.tscn`
- `speed_time_widget.tscn`
- `event_log.tscn`
- `minimap.tscn`
- `main_ui.tscn` — CanvasLayer that instances the above with anchors set in editor

### Implementation rules
- **Author scenes in the Godot editor.** Use Containers (`HBoxContainer`, `VBoxContainer`, `MarginContainer`, `GridContainer`) for layout. Set anchors on the root Control of each scene. Use `SizeFlags` and `CustomMinimumSize`, NOT explicit positions.
- **Use `TabContainer` for LeftPanel.** Each tab is its own scene (`fleets_tab.tscn`, `research_tab.tscn`, `build_tab.tscn`, optional `colonies_tab.tscn`).
- **Each panel script must:**
  - Be ≤200 lines
  - Have NO MainScene reference. Use `GameManager.Instance` for state queries (which implements `IGameQuery` after Phase 1).
  - Subscribe to relevant EventBus events in `_Ready`, unsubscribe in `_ExitTree`.
  - Use `[Export]` properties for editor-tunable values (e.g., max event-log entries).
  - Use `%UniqueName` for cross-scene references.
  - For SCAN/EXTRACT actions, fire `EventBus.Instance.FireScanToggleRequested(poiId)` etc. (added in Phase 1B).
- **F6-test every panel scene.** Each must instance and render without erroring (may show empty/placeholder data, that's fine).
- **Theme everything via `theme.tres` defaults.** Per-control overrides only for genuinely intentional cases (faction-tinted accents, state-based highlights). When in doubt, delete the override.
- **For text:** prefer `LabelSettings` resources over `AddThemeFontOverride`. See `references/fonts.md`.
- **For glassmorphism backdrops:** use `templates/tarnished_glass.gdshader` from the skill if a panel needs frosted blur.

### MainScene wiring change
Replace this in MainScene's `_Ready`:
```csharp
_topBar = new TopBar { Name = "TopBar" };
_uiLayer.AddChild(_topBar);
_leftPanel = new LeftPanel { Name = "LeftPanel" };
_leftPanel.SetMainScene(this);
// ... etc
```
With:
```csharp
[Export] private PackedScene _mainUiScene = null!;
// ...
var ui = _mainUiScene.Instantiate<CanvasLayer>();
AddChild(ui);
_topBar = ui.GetNode<TopBar>("%TopBar");
_leftPanel = ui.GetNode<LeftPanel>("%LeftPanel");
// etc.
```

### Per-panel notes

**`top_bar.tscn`:**
- Root: `Control` anchored top-full-width, ~64px tall.
- Children (HBox): empire header / credits / parts / 5× FactionResourceBox (instanced sub-scene `faction_resource_box.tscn`) / ResearchStrip
- ResearchStrip can stay as embedded scene or sub-scene.

**`left_panel.tscn`:**
- Root: `Control` anchored left-full-height, ~300px wide.
- Child: `TabContainer` with 4 tabs. Each tab content is a separate scene.
- `fleets_tab.tscn`: VBoxContainer of fleet cards (instance sub-scene `fleet_card.tscn` per fleet — populated by script). Script ≤150 lines.
- `research_tab.tscn`: instance of existing `ResearchTabContent`, but reduced to ≤200 lines.
- `build_tab.tscn`: shipyard entry point + saved designs list.

**`right_panel.tscn`:**
- Root: `Control` anchored right-full-height, ~280px wide.
- Internal: a `Control` parent that hosts whichever sub-panel is active (system info / fleet info / empty).
- Each sub-state is its own scene: `system_info_panel.tscn`, `fleet_info_panel.tscn`, `empty_right_panel.tscn`.
- Script swaps which sub-scene is visible based on `EventBus.SystemSelected` / `FleetSelected` / `Deselected`.

**`speed_time_widget.tscn`:**
- Bottom-right anchored. HBoxContainer of buttons. Pure layout in editor.

**`event_log.tscn`:**
- Bottom-right anchored. ScrollContainer + VBoxContainer. Script appends styled `Label`s.

**`minimap.tscn`:**
- Bottom-left anchored. SubViewportContainer + SubViewport with 2D rendering.

### Gate
- All 6 scenes F6-runnable in isolation.
- Visual screenshot matches pre-refactor baseline (expect minor pixel differences from anchor adjustments — within reason).
- Each panel script ≤200 lines; LeftPanel ≤200 (down from 652); RightPanel ≤200 (down from 942).
- 335/335 tests pass.
- Manual smoke test: click fleet → LeftPanel highlights + RightPanel updates. Right-click system → SelectionController issues move + LeftPanel updates. Game speed buttons work. Event log updates as events fire.

---

## Phase 3 — System View as scenes *(high risk, ~8 hours)*

Apply the same treatment to the per-system drill-down. ~3000 lines → ~1200 with proper scenes.

### Sub-scenes to create

| Current C# file | New scene path |
|---|---|
| `SystemViewScene.cs` | `scenes/ui/system_view/system_view.tscn` |
| `RightPanelController.cs` | `scenes/ui/system_view/right_panel_controller.tscn` |
| `ColonyEntityPanel.cs` | `colony_entity_panel.tscn` |
| `StationEntityPanel.cs` | `station_entity_panel.tscn` |
| `OutpostEntityPanel.cs` | `outpost_entity_panel.tscn` |
| `SalvageEntityPanel.cs` | `salvage_entity_panel.tscn` |
| `EnemyEntityPanel.cs` | `enemy_entity_panel.tscn` |
| `EmptyStateDashboard.cs` | `empty_state_dashboard.tscn` |
| `POICard.cs` | `poi_card.tscn` |
| `FleetPOICard.cs` | `fleet_poi_card.tscn` |
| `BandRow.cs` | `band_row.tscn` |
| `BuildingRow.cs` | `building_row.tscn` |
| `SlotChip.cs` | `slot_chip.tscn` |
| `SubTicketRow.cs` | `sub_ticket_row.tscn` |
| `EntityTabStrip.cs` | `entity_tab_strip.tscn` |

### OverlayRouter wiring change
[OverlayRouter.cs:73-87](../../src/Nodes/Map/OverlayRouter.cs#L73-L87) currently does:
```csharp
_systemView = new SystemViewScene { Name = "SystemViewScene" };
_uiLayer.AddChild(_systemView);
```
Change to:
```csharp
[Export] private PackedScene _systemViewScene = null!;
// in OnSystemSelectedForView:
_systemView = _systemViewScene.Instantiate<SystemViewScene>();
_uiLayer.AddChild(_systemView);
```

### Per-scene notes

**`system_view.tscn`:**
- Root: `Control` full-screen anchored. Background = solid bg covering galaxy map.
- Layout: title bar (system name) on top, central ring layout (Inner/Mid/Outer bands), right panel slot.
- The ring band drawing should stay in code (canvas-drawn rings). The POI cards are instanced sub-scenes positioned along the bands.

**`poi_card.tscn`:**
- Compact card with icon, name, type, sub-tickets indicator.
- Click selects → fires EventBus event for RightPanelController.
- 328 lines → target ≤150.

**`right_panel_controller.tscn`:**
- Root container that swaps which entity_panel sub-scene is visible.
- Subscribes to entity selection events.
- 281 lines → target ≤150.

**Entity panels** (`colony_entity_panel.tscn` etc.):
- Each is a self-contained .tscn.
- Use `LabelSettings` for typography.
- Bind data via a `Populate(entity)` method called by RightPanelController.
- 100-200 lines each.

### Sensor/detection overlay
The detection coverage rings + signature glyphs should remain code-drawn (they're per-frame canvas rendering). Their host node can be a sub-scene with the script attached.

### Gate
- All sub-scenes F6-runnable with a placeholder data binding.
- Visual parity with the pre-refactor System View screenshots.
- 335/335 tests pass.
- Manual smoke test: double-click system → SystemView opens. Click each POI type → correct entity panel shows. Click chip in colony → pop reallocation works. Sub-tickets render. Tab strip switches between owners on shared POIs. ESC or close button exits cleanly.

---

## Verification protocol per phase

Same as Refactor 1:

1. `dotnet build` — must succeed
2. `dotnet test tests/DerlictEmpires.Tests.csproj` — 335+ passing
3. `godot_reload` (windowed)
4. `godot_stdout` — no compile errors
5. `godot_screenshot` — visual parity with pre-phase baseline (capture before/after for diff)
6. Manual smoke: phase-relevant interactions
7. F6-test each new scene standalone
8. Commit per phase: `refactor(ui): phase N — <short title>`

## Per-phase commits

- Phase 1: `refactor(ui): foundation — IGameQuery, salvage action handler, theme.tres`
- Phase 2: `refactor(ui): galaxy-map panels as scenes`
- Phase 3: `refactor(ui): system view as scenes`

You may sub-split Phase 2/3 by panel if scope feels too big. Each panel is independently shippable once `IGameQuery` exists.

---

## Risk callouts

- **Phase 2 is the danger zone.** Rewriting 1500+ lines of imperative UI as `.tscn` files risks visual regression. Mitigations: per-panel sub-PRs, before/after screenshots in each, keep the old `.cs` panels in git history for diffing.
- **Theme.tres setup is finicky.** If you mis-configure StyleBoxes, every Control in the project looks wrong. Build incrementally — verify Button styling first, then expand.
- **The skill's `references/ui-system.md` warns about a specific bug**: setting `Position`/`Size` on a child of a Container — those get overridden by the Container. Use `SizeFlags` and `CustomMinimumSize` instead. **Read that section first.**
- **F6-runnability is the canary.** If a scene crashes when run alone, your scene architecture has hidden coupling.
- **Don't skip the IGameQuery work.** If you start Phase 2 with panels still calling `_main.X`, you'll just rebuild the back-pointer in `.tscn` form.

---

## What NOT to do

- Don't try to keep `SetMainScene(this)` working in the new scenes. The whole point is to remove it.
- Don't write theme overrides that match Theme defaults. If `theme.tres` says button color is X, don't `AddThemeColorOverride("font_color", X)` redundantly.
- Don't use `GD.Print` — use `McpLog.Info/Warn/Error`.
- Don't use Godot signals (`[Signal] delegate`) for cross-tree communication — use `EventBus`. Within a single scene, signals are fine.
- Don't replicate state in panel fields. Read from `GameManager.Instance` on demand or cache via EventBus events.
- Don't use `string.GetHashCode()` for anything determinism-sensitive — it's process-randomized in .NET 5+ (see [GameRandom.cs](../../src/Core/Random/GameRandom.cs) for the FNV-1a fix).

---

## When done

- MainScene shrinks further as `_topBar`, `_leftPanel`, etc. become single instantiation lines.
- All UI in `scenes/ui/`. All scripts ≤200 lines.
- `theme.tres` is the styling source of truth.
- No `_main.X` back-pointers from any UI panel.
- Update memory entries `project_ui_implementation` and `project_system_view` to reflect the rebuild.
