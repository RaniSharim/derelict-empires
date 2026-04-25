# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Derelict Empires — a real-time 4X/5X space strategy game built with **Godot 4.6** and **C# (.NET 8.0)**. The comprehensive game design lives in `DESIGN.md` (1275 lines). The implementation plan is at `.claude/plans/twinkly-sleeping-minsky.md`. The galaxy map UI spec is at `ui_instructions.md`.

All 21 implementation phases (0-20) have their core C# systems implemented with 335 passing unit tests.

## Build & Run

```bash
dotnet build                                        # Build game
dotnet test tests/DerlictEmpires.Tests.csproj       # Run all unit tests (335 tests)
dotnet test tests/E2E/DerlictEmpires.E2E.csproj     # Run E2E tests (needs GODOT_BIN)
dotnet test --filter "FullyQualifiedName~Galaxy"     # Run tests by keyword
```

## Architecture

**Engine:** Godot 4.6 + C# via `Godot.NET.Sdk/4.6.2`. Root namespace: `DerlictEmpires`. Nullable enabled.

**All C# scripts MUST be `public partial class`** (Godot source generator requirement).

### Project Structure
```
src/
  Autoloads/       EventBus, GameManager (implements IGameQuery), DataLoader, TurnManager
  Core/            Pure C# — no Godot dependencies, unit-testable
    GameSystems.cs Composition root — owns all 8 logic systems and re-emits their events.
                   Constructable in tests; no Godot deps.
    AI/            UtilityBrain, PersonalityPresets, DifficultySettings
    Combat/        CombatSimulator, WeaponsTriangle, CombatUnit
    Diplomacy/     DiplomacyManager, ReputationSystem
    Economy/       CurrencyManager, MarketService
    Enums/         PrecursorColor, ResourceType, ShipSizeClass, etc. (14 files)
    Espionage/     EspionageManager, IntelCategory
    Events/        RandomEventSystem, VictoryConditionChecker
    Exploration/   ExplorationManager, HazardChecker, DerelictShip, SalvageSystem
    Leaders/       LeaderManager, Admiral/Governor traits
    Logistics/     LogisticsSystem, LogisticsNetwork
    Models/        StarSystemData, EmpireData, GalaxyData, FleetData, etc.
    Multiplayer/   SpeedVoting
    Production/    ProductionQueue, IProducible
    Random/        GameRandom (seeded deterministic RNG)
    Services/      IGameQuery — read-only UI facade, implemented by GameManager
    Settlements/   Colony, Outpost, PopAllocationManager, HappinessCalculator, BuildingData, SettlementSystem
    Ships/         ChassisData (14 chassis), ShipDesign, ShipDesignValidator
    Stations/      Station, 6 module types, PrecursorStation, StationSystem
    Systems/       GalaxyGenerator, LanePathfinder, FleetMovementSystem, ResourceExtractionSystem, etc.
    Tech/          TechTreeRegistry (150 nodes), ResearchEngine, EfficiencyCalculator, ExpertiseTracker
    Visibility/    VisibilitySystem, DetectionCalculator
  Nodes/           Godot node scripts
    Camera/        StrategyCameraRig (pan/zoom/rotate). Subscribes to EventBus.CameraPanToWorldRequested.
    Map/           MainScene (scene-graph wiring), GameSystemsHost (FastTick pump + EventBus bridge),
                   FleetVisualController (FleetNode container + per-tick position updates +
                     selection visuals + double-click→camera-pan emit),
                   SalvageActionHandler (intent events → GameSystems.Salvage),
                   MovementActionHandler (intent events → GameSystems.Movement),
                   GalaxyMap, StarRenderer, LaneRenderer, StarSystemNode,
                   DevHarness, OverlayRouter, CombatRouter, SelectionController (IGameQuery only)
    UI/            TopBar, LeftPanel, RightPanel, SpeedTimeWidget, EventLog, Minimap, etc.
                   All read via IGameQuery, write via EventBus intent events. No MainScene refs.
    Units/         FleetNode
scenes/            .tscn scene files
  ui/              Top-level panel scenes: top_bar, left_panel, right_panel, speed_time_widget,
                   event_log, minimap. Plus reusable sub-scenes: fleet_card, event_log_entry.
resources/ui/      theme.tres — project-wide Theme (Button/Panel/Label/etc. defaults).
                   Built by ThemeBuilder.cs; per-control overrides reserved for intentional accents.
tests/             335 xUnit tests (references src/Core/ directly, no Godot dependency)
  E2E/             E2E tests via McpBridge (needs running Godot, skips if GODOT_BIN not set)
    Fixtures/      Pre-designed JSON save files for E2E tests
```

Design docs are in design folder

### Key Patterns
- **Call Down, Signal Up:** Parents call children; children emit signals; cross-tree uses EventBus
- **Self-contained scenes:** Each `.tscn` works when instanced alone (F6)
- **EventBus:** Singleton autoload with C# `event Action<T>` delegates (not Godot signals)
- **GameManager:** Data store (speed, empires, fleets, ships, colonies, station DTOs, galaxy ref, master seed). Implements `IGameQuery` for UI reads. Not logic.
- **GameSystems composition root:** All 8 logic systems (Movement, Salvage, Exploration, Extraction, Settlements, Stations, TechRegistry, Research) live on `GameSystems` (pure C#, in `src/Core/GameSystems.cs`). `GameSystemsHost` (Node, in `src/Nodes/Map/`) pumps `EventBus.FastTick` into `Systems.Tick(...)` and bridges system-level events back onto `EventBus`. MainScene holds one field (`_systemsHost`) and exposes `Systems` for sibling controllers.
- **UI contract:**
  - **Scene-first.** Top-level panels live in `scenes/ui/*.tscn`. Reusable rows/cards (fleet_card, event_log_entry, faction_resource_box) are sub-scenes instanced from the parent. Layout in editor; scripts only handle data binding + event wiring.
  - **Read** through `IGameQuery` (`GameManager.Instance` implements it). Panels never reference `GameSystems` or `MainScene` directly. `IGameQuery` exposes the player empire, fleets, ships, galaxy, exploration/scan/contributing-fleet helpers, salvage capability/active-count, fleet orders, settlements/stations runtime, and tech/research state.
  - **React** to change events on `EventBus` (subscribe in `_Ready`, unsubscribe in `_ExitTree`). Always unsubscribe — `EventBus` is an autoload and would otherwise hold the panel forever.
  - **Write** by firing intent events on `EventBus` (e.g. `FireScanToggleRequested(poiId)`, `FireExtractToggleRequested(poiId)`, `FireFleetMoveOrderRequested(fleetId, targetSystemId)`, `FireCameraPanToWorldRequested(pos)`). A handler Node (`SalvageActionHandler`, `MovementActionHandler`) validates and forwards to the system. UI never mutates game state directly.
  - **Compose**, don't poll. `_Process` is for animations/canvas redraws only — for state, subscribe to the event that signals the change. ResearchStrip refreshes on `SlowTick` + project-change events; FactionResourceBox guards its label assigns with a "value changed" check.
- **Deterministic seeded RNG:** All randomization uses `GameRandom` (wraps `System.Random`). Never use crypto RNG or `GD.Randf()`. Same seed = same results. Subsystems derive child RNGs via `GameRandom.DeriveChild(differentiator)`.
- **Data-driven:** Static data arrays (ResourceDefinition.All, ComponentDefinition.All, ChassisData.All, BuildingData.All)
- **Two-tier tick:** Fast tick (0.1s) for movement/combat; Slow tick (1.0s) for economy/growth/research. `GameSystemsHost` owns the FastTick subscription; visuals (e.g. `FleetVisualController`) subscribe separately and read positions after the host has processed.

### Core Game Systems
- **Galaxy:** Spiral arm generation, K-nearest lane graph, Tarjan bridge-finding for chokepoints, POI distribution
- **Movement:** Dijkstra pathfinding on lane graph, tick-based interpolation
- **Settlements:** Colony (pop growth, happiness, buildings, production queue), Outpost (limited mining)
- **Stations:** Modular slots (Shipyard, Defense, Logistics, Trade, Garrison, Sensors), PrecursorStation
- **Tech:** 150 nodes (5 colors × 5 categories × 6 tiers), efficiency tiers (1.0/0.7/0.4), expertise, 10 synergies
- **Ships:** 14 chassis (7 sizes × 2 variants), slot-based design, validation
- **Combat:** Weapons triangle (Laser/Railgun/Missile vs PD/Shield/Armor), defense layers in order, morale
- **Exploration:** Discovery → Survey → Exploitation, hazard checks, derelict ship actions (5 types)
- **Economy:** Credits, market (fixed price + auctions), trade flow
- **Diplomacy:** Contact matrix, 6 agreement types, reputation (reliability + bilateral relation)
- **Espionage:** 6 intel categories, investment vs counter-intel, passive intel from trade
- **Leaders:** Admirals (fleet bonuses) and Governors (colony bonuses) with randomized traits
- **AI:** Utility-based framework, personality presets (Red Warrior, Gold Hauler, etc.), 4 difficulty levels
- **Visibility:** Per-empire fog of war, detection levels (None/Minimal/Basic/Detailed/Full)
- **Logistics:** Supply consumption (energy/parts/food), hub network with distance waste

### Input Actions
Defined in `project.godot`: `left_click`, `right_click`, `pause` (Space), `speed_up` (.), `speed_down` (,), `camera_up/down/left/right` (WASD)

### Fonts & Colors
Typography and the precursor color palette are centralized — no ad-hoc fonts, sizes, or colors in panels.

- **Fonts:** Two-font system in [`src/Nodes/UI/UIFonts.cs`](src/Nodes/UI/UIFonts.cs).
  - `UIFonts.Title` = Exo 2 SemiBold @ `TitleSize` (16) — fleet/POI/system/colony names.
  - `UIFonts.Main` = B612 Mono Bold @ `NormalSize` (14) or `SmallSize` (12) — everything else.
  - **12px is the hard floor.** Call `UIFonts.Style(label, UIFonts.Main, UIFonts.SmallSize, …)` or `UIFonts.StyleRole(label, UIFonts.Role.Small)`.
  - Fonts loaded via `FileAccess` (not Godot's import pipeline). Rendering settings (Hinting=Normal, SubpixelPositioning=Disabled, ForceAutohinter=false, Antialiasing=Gray) applied programmatically.
  - Legacy role names (`UILabel`, `TitleMedium`, `DataSmall`, etc.) alias to the 3-tier system for backward compat.
- **Colors:** Precursor tokens live in [`src/Nodes/UI/UIColors.cs`](src/Nodes/UI/UIColors.cs). Each of the 5 precursor colors has four presets:
  - `{Color}Bright`, `{Color}Normal`, `{Color}Dim`, `{Color}Bg` (e.g. `RedBright`, `BlueDim`).
  - Enum-driven lookup: `UIColors.GetPrecursor(PrecursorColor.Red, UIColors.Tone.Bright)`.
  - `GetFactionGlow` / `GetFactionBg` map to the `Normal` / `Bg` tones.
- **Debug overlays:** `F11` toggles the font showcase (catalogue + config matrix + pixel-position demo). `F10` saves a PNG to `screenshots/`. `F12` toggles exclusive fullscreen.

## Environment Requirements

- Godot 4.6 with .NET support
- .NET 8.0 SDK
- `GODOT4` environment variable for VS Code debugging

# Godot MCP — Claude Code Instructions

This project has a running MCP server (`godot-mcp`) that gives you direct control over a Godot 4 instance. Use it to verify every change you make.

# Godot skill
You have /godot-4x-csharp. 
Please use it.

## The Iteration Loop

Follow this loop for **every** change:

1. **Batch all related file edits first** — never reload between individual file changes. C# recompilation takes 10–30 seconds.
2. **`godot_reload`** — triggers C# recompilation and restarts the scene.
3. **`godot_stdout`** immediately — if compilation failed, the error is here. The bridge will not be up. Fix the error and reload before calling any other tool.
4. **`godot_screenshot`** — verify the scene renders correctly. **Only works in windowed mode** (pass `headless: false` to `godot_start`/`godot_reload`).
5. **`godot_scene_tree`** — verify node structure matches expectations.
6. **`godot_logs`** — check for runtime errors or warnings from `_Ready()` and early frames.
7. Repeat from step 1.

## Headless vs Windowed Mode

Both `godot_start` and `godot_reload` accept an optional `headless` parameter:

- **`headless: true`** (default) — no window, no GPU needed, faster. Screenshots return an error. Use for logic-only changes.
- **`headless: false`** — opens a Godot window, screenshots work. Use when you need to verify visuals.

**Choose based on what you're doing:**
- Editing game logic, fixing bugs, changing data → `headless: true` (or omit, it's the default)
- Building/changing UI, rendering, visual effects → `headless: false`

## Compilation Failures

If `godot_reload` completes but `godot_screenshot` hangs or the bridge does not respond, **always check `godot_stdout` first**. C# compile errors only appear there. The bridge never starts if the build fails, so no other tools will work until the error is fixed and the scene is reloaded.

## Batching Edits

C# recompilation is slow. Always batch all related file changes before calling `godot_reload`. Never reload after each individual file edit.

## Scene Tree as Ground Truth

After every reload, call `godot_scene_tree` before making assumptions about what nodes exist. Scenes can fail to instantiate silently if a script throws in `_Ready()` — this won't appear as a compile error in stdout but will appear in `godot_logs`.

## Screenshot Interpretation

Windowed renders are accurate and contain no editor gizmos or selection highlights. What is in the screenshot is exactly what the player would see at runtime.

## Process Lifecycle

- Call `godot_start` once at the beginning of a session (with `headless: false` if you need screenshots).
- Use `godot_reload` for all subsequent restarts (this is the primary iteration tool).
- Call `godot_stop` when the session is done.
- **Never call `godot_start` when a process is already running** — it will error. Use `godot_reload` instead.
- You can switch between headless and windowed by passing a different `headless` value to `godot_reload`.

## Logging

The project uses `McpLog.Info()`, `McpLog.Warn()`, `McpLog.Error()` instead of bare `GD.Print`. Use these in any code you write so logs are captured by the MCP bridge.

## Debugging the EventBus cascade

`EventBus` can attach a debug subscriber that logs every fired event as `[evt tick=N] EventName { payload }` through `McpLog`. Opt in via env var at process spawn:

```
godot_start  { env: { "DEBUG_EVENTBUS": "1" } }           // default blocklist
godot_reload { env: { "DEBUG_EVENTBUS": "1",
                      "DEBUG_EVENTBUS_FILTER": "FleetSelected,FleetDeselected" } }   // custom blocklist
godot_reload {}                                           // flag cleared, subscriber off
```

- `DEBUG_EVENTBUS=1` attaches. Unset or any other value = off (zero overhead).
- `DEBUG_EVENTBUS_FILTER` is a comma-separated blocklist (case-insensitive). Leading `-` on names is tolerated. Set to `""` to log everything.
- Default blocklist: `FastTick,SlowTick,BattleTick,ScanProgressChanged` — the per-frame/per-tick events that would otherwise drown signal.
- `tick` in the log line is `TurnManager.FastTickCount` at fire time, not a timestamp.
- Use this to debug selection / right-panel cascades instead of sprinkling `McpLog.Info` at event sites. Retrieve logs with `godot_logs`.

When adding a new event to `EventBus`, also add a matching `Hook(...)` line in `AttachDebugSubscriberIfEnabled` so it's observable by default.

## Save/Load State

The bridge supports `load_state` and `save_state` commands:

- **`godot_load_state`** — Load a JSON save file into the running instance. Accepts `path` (file) or `json` (inline).
- **`godot_save_state`** — Capture current game state as JSON. Accepts optional `path` to save to file.
- **`godot_tick`** — Fire fast/slow ticks manually without unpausing. Use for deterministic testing.

The save format is `GameSaveData` (defined in `src/Core/Models/GameSaveData.cs`). MainScene implements `LoadGame()` and `BuildGameSaveData()`.

## E2E Testing

E2E tests live in `tests/E2E/` and connect directly to Godot's McpBridge TCP port (9876).

- Tests **skip with a warning** if `GODOT_BIN` env var is not set
- One Godot instance per test run (shared via xUnit collection fixture)
- Each test loads a pre-designed save file from `tests/E2E/Fixtures/`
- Tests split by trait: `[Trait("Category", "Headless")]` vs `[Trait("Category", "Visual")]`
- Run: `dotnet test tests/E2E/DerlictEmpires.E2E.csproj`

## godot_eval — Currently Disabled

The `godot_eval` tool (Roslyn C# scripting) is disabled on Godot 4.6 Windows due to a native crash. Use `godot_scene_tree` and `godot_logs` to inspect live state instead.

## Project Structure

- `Scripts/McpBridge.cs` — TCP autoload that handles MCP commands (do not modify)
- `Scripts/McpLog.cs` — static logger (do not modify)
- `godot-mcp/` — Node.js MCP server
- `project.godot` — McpBridge is registered as an autoload