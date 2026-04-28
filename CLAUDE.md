# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Derelict Empires — a real-time 4X/5X space strategy game built with **Godot 4.6** and **C# (.NET 8.0)**. The comprehensive game design lives in `design/DESIGN.md` (1274 lines); newer `design/DESIGN_V2.md` (819 lines) and topic specs (`design/ui_instructions.md`, `design/in_system_design.md`, `design/research_ui_spec.md`, etc.) sit alongside.

In-flight implementation plans live in `.claude/plans/` (currently `code-review-fixes.md`, `refactor-1.md`, `refactor-2-ui.md`, `refactor-3.md`).

## Build & Run

```bash
dotnet build                                        # Build game
dotnet test tests/DerlictEmpires.Tests.csproj       # Run all unit tests (367 tests)
dotnet test tests/E2E/DerlictEmpires.E2E.csproj     # Run E2E tests (needs GODOT_BIN)
dotnet test --filter "FullyQualifiedName~Galaxy"     # Run tests by keyword
```

See [MCP.md](MCP.md) for how to drive the live Godot instance via the MCP bridge — this is the primary iteration tool for any change that needs visual or runtime verification.

**C# language server is configured for this workspace.**

For symbol-level questions — "where is X defined", "who calls Y", "what's the type of Z", 
"list functions in this file" — use the `LSP` tool (`goToDefinition`, `findReferences`, 
`incomingCalls`, `outgoingCalls`, `hover`, `documentSymbol`), not `Grep`.

For refactoring — renames, extractions, splits — always run findReferences on the target
symbol first to get the exact call site list, then make edits. Use incomingCalls /
outgoingCalls to understand dependency surfaces before moving or splitting code.

Reserve `Grep` for: config files, markdown, string-literal searches, and anything not symbol-shaped (`.tscn`/`.tres`, design docs).

If `workspaceSymbol` returns empty, the language server may still be indexing — 
fall back to per-file `documentSymbol`.

## Architecture

**Engine:** Godot 4.6 + C# via `Godot.NET.Sdk/4.6.2`. Root namespace: `DerlictEmpires`. Nullable enabled.

**All C# scripts MUST be `public partial class`** (Godot source generator requirement).

### Project Structure
```
src/
  Autoloads/       EventBus, GameManager (implements IGameQuery), DataLoader, TurnManager
  Core/            Pure C# — no Godot dependencies, unit-testable.
    GameSystems.cs Composition root — owns 8 logic systems (Movement, Extraction, Settlements,
                   Stations, Exploration, Salvage, TechRegistry, Research) and re-emits their
                   events. Constructable in tests; no Godot deps.
    Enums/         23 game enums (PrecursorColor, ResourceType, ShipSizeClass, POIType, etc.)
    Models/        DTOs — StarSystemData, EmpireData, GalaxyData, FleetData, ShipInstanceData,
                   ColonyData, StationData, POIData, GameSaveData, etc.
    Random/        GameRandom (seeded deterministic RNG — wraps System.Random)
    Services/      IGameQuery — read-only UI facade, implemented by GameManager
    Systems/       Galaxy generation (spiral arms, K-nearest lanes, POIs), lane pathfinding,
                   fleet movement, resource extraction, save/load, game setup, selection
    Tech/          150-node tech tree, research engine, efficiency/expertise tracking,
                   salvage research processor
    Ships/         14 chassis (7 sizes × 2 variants), ship designs, fleet templates,
                   empire design state, design profiler, MVP starting designs
    Settlements/   Colony, Outpost, pop groups + growth + happiness, building data + production
    Stations/      Station, modules (Shipyard/Defense/Logistics/Trade/Garrison/Sensors),
                   PrecursorStation, construction jobs
    Exploration/   ExplorationManager, hazard checks, derelict ships, full salvage subsystem
                   (registry, sites, layers, outcomes, color rolling, danger types,
                   excavation jobs, extraction calculator)
    Combat/        BattleManager + BattleAggregate (live battle state), CombatSimulator,
                   weapons triangle, combat units, combat results
    Visibility/    Per-empire fog of war, sensor coverage, signature calculation
    Production/    ProductionQueue, IProducible
    Economy/       Currency, market service
    Diplomacy/     DiplomacyManager
    Espionage/     EspionageManager
    Leaders/       LeaderSystem (Admiral / Governor traits)
    Logistics/     LogisticsSystem
    AI/            AIManager
    Events/        Random events, victory conditions
    Multiplayer/   Speed voting
  Nodes/           Godot node scripts.
    Camera/        StrategyCameraRig (pan/zoom/rotate; subscribes to CameraPanToWorldRequested)
    Map/           MainScene (scene-graph wiring), GameSystemsHost (FastTick pump + EventBus
                   bridge), FleetVisualController, SalvageActionHandler, MovementActionHandler,
                   GalaxyMap, StarRenderer, LaneRenderer, StarSystemNode, BattleMarker,
                   DevHarness, OverlayRouter, CombatRouter, SelectionController (IGameQuery only)
    UI/            Panels and chrome. See [src/Nodes/UI/CLAUDE.md](src/Nodes/UI/CLAUDE.md)
                   for the UI contract (IGameQuery reads, EventBus intent writes, fonts/colors,
                   subscribe/unsubscribe rules). Subdirs: SystemView/, CombatHUD/, ShipDesigner/.
    Units/         FleetNode
scenes/
  map/             main.tscn
  ui/              Top-level panels (top_bar, left_panel, right_panel, speed_time_widget,
                   event_log, minimap) and reusable sub-scenes (fleet_card, event_log_entry,
                   faction_resource_box, sub_ticket_row, entity_panel_header).
resources/ui/      theme.tres — project-wide Theme (Button/Panel/Label/etc. defaults).
                   Built by ThemeBuilder.cs; per-control overrides reserved for intentional accents.
Scripts/           McpBridge, McpLog (do not modify — see MCP.md)
tests/             367 xUnit tests (references src/Core/ directly, no Godot dependency)
  E2E/             E2E tests via McpBridge (needs running Godot, skips if GODOT_BIN not set)
    Fixtures/      Pre-designed JSON save files for E2E tests
design/            Design docs (DESIGN.md, DESIGN_V2.md, UI specs, system specs)
```

### Key Patterns
- **Call Down, Signal Up:** Parents call children; children emit signals; cross-tree uses EventBus.
- **Self-contained scenes:** Each `.tscn` works when instanced alone (F6).
- **EventBus:** Singleton autoload with C# `event Action<T>` delegates (not Godot signals).
- **GameManager:** Data store (speed, empires, fleets, ships, colonies, station DTOs, galaxy ref, master seed). Implements `IGameQuery` for UI reads. Not logic.
- **GameSystems composition root:** All 8 logic systems (Movement, Salvage, Exploration, Extraction, Settlements, Stations, TechRegistry, Research) live on `GameSystems` (pure C#, in `src/Core/GameSystems.cs`). `GameSystemsHost` (Node, in `src/Nodes/Map/`) pumps `EventBus.FastTick` into `Systems.Tick(...)` and bridges system-level events back onto `EventBus`. MainScene holds one field (`_systemsHost`) and exposes `Systems` for sibling controllers.
- **UI contract:** Panels read via `IGameQuery`, write by firing `EventBus` intent events handled by action-handler nodes; subscribe in `_Ready` and unsubscribe in `_ExitTree`. Full rules in [src/Nodes/UI/CLAUDE.md](src/Nodes/UI/CLAUDE.md).
- **Deterministic seeded RNG:** All randomization uses `GameRandom` (wraps `System.Random`). Never use crypto RNG or `GD.Randf()`. Same seed = same results. Subsystems derive child RNGs via `GameRandom.DeriveChild(differentiator)`.
- **Data-driven:** Static data arrays (`ResourceDefinition.All`, `ComponentDefinition.All`, `ChassisData.All`, `BuildingData.All`).
- **Two-tier tick:** Fast tick (0.1s) for movement/combat; Slow tick (1.0s) for economy/growth/research. `GameSystemsHost` owns the FastTick subscription; visuals (e.g. `FleetVisualController`) subscribe separately and read positions after the host has processed.
- **No Python:** Stack is C# end-to-end. Don't reach for python for JSON/data wrangling.

### Core Game Systems
- **Galaxy:** Spiral arm generation, K-nearest lane graph, Tarjan bridge-finding for chokepoints, POI distribution.
- **Movement:** Dijkstra pathfinding on lane graph, tick-based interpolation.
- **Settlements:** Colony (pop growth, happiness, buildings, production queue), Outpost (limited mining).
- **Stations:** Modular slots (Shipyard, Defense, Logistics, Trade, Garrison, Sensors), PrecursorStation.
- **Tech:** 150 nodes (5 colors × 5 categories × 6 tiers), efficiency tiers (1.0/0.7/0.4), expertise, 10 synergies.
- **Ships:** 14 chassis (7 sizes × 2 variants), slot-based design.
- **Combat:** Weapons triangle (Laser/Railgun/Missile vs PD/Shield/Armor), defense layers in order, morale.
- **Exploration:** Discovery → Survey → Exploitation, hazard checks, derelict ship actions, salvage sites with color/layer/outcome rolls.
- **Economy:** Credits, market (fixed price + auctions), trade flow.
- **Diplomacy:** Contact matrix, agreement types, reputation.
- **Espionage:** Intel categories, investment vs counter-intel.
- **Leaders:** Admirals (fleet bonuses) and Governors (colony bonuses) with randomized traits.
- **Visibility:** Per-empire fog of war, sensor coverage and signature calculation.
- **Logistics:** Supply consumption (energy/parts/food), hub network with distance waste.

### Input Actions
Defined in `project.godot`: `left_click`, `right_click`, `pause` (Space), `speed_up` (.), `speed_down` (,), `camera_up/down/left/right` (WASD).

## Environment Requirements

- Godot 4.6 with .NET support
- .NET 8.0 SDK
- `GODOT4` environment variable for VS Code debugging
