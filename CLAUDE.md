# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Derelict Empires — a real-time 4X/5X space strategy game built with **Godot 4.3** and **C# (.NET 8.0)**. The comprehensive game design lives in `DESIGN.md` (1275 lines). The implementation plan is at `.claude/plans/twinkly-sleeping-minsky.md`.

All 21 implementation phases (0-20) have their core C# systems implemented with 253 passing unit tests.

## Build & Run

```bash
dotnet build                                    # Build game
dotnet test tests/DerlictEmpires.Tests.csproj   # Run all unit tests
dotnet test --filter "FullyQualifiedName~Galaxy" # Run tests by keyword

# Run the game: set GODOT4 env var, use VS Code "Play" launch config
```

## Architecture

**Engine:** Godot 4.3 + C# via `Godot.NET.Sdk/4.3.0`. Root namespace: `DerlictEmpires`. Nullable enabled.

**All C# scripts MUST be `public partial class`** (Godot source generator requirement).

### Project Structure
```
src/
  Autoloads/       EventBus, GameManager, DataLoader, TurnManager
  Core/            Pure C# — no Godot dependencies, unit-testable
    AI/            UtilityBrain, PersonalityPresets, DifficultySettings
    Combat/        CombatSimulator, WeaponsTriangle, CombatUnit
    Diplomacy/     DiplomacyManager, ReputationSystem
    Economy/       CurrencyManager, MarketService
    Enums/         PrecursorColor, ResourceType, ShipSizeClass, etc. (14 files)
    Espionage/     EspionageManager, IntelCategory
    Events/        RandomEventSystem, VictoryConditionChecker
    Exploration/   ExplorationManager, HazardChecker, DerelictShip
    Leaders/       LeaderManager, Admiral/Governor traits
    Logistics/     LogisticsSystem, LogisticsNetwork
    Models/        StarSystemData, EmpireData, GalaxyData, FleetData, etc.
    Multiplayer/   SpeedVoting
    Production/    ProductionQueue, IProducible
    Random/        GameRandom (seeded deterministic RNG)
    Settlements/   Colony, Outpost, PopAllocationManager, HappinessCalculator, BuildingData
    Ships/         ChassisData (14 chassis), ShipDesign, ShipDesignValidator
    Stations/      Station, 6 module types, PrecursorStation, StationSystem
    Systems/       GalaxyGenerator, LanePathfinder, FleetMovementSystem, etc.
    Tech/          TechTreeRegistry (150 nodes), ResearchEngine, EfficiencyCalculator, ExpertiseTracker
    Visibility/    VisibilitySystem, DetectionCalculator
  Nodes/           Godot node scripts
    Camera/        StrategyCameraRig (pan/zoom/rotate)
    Map/           GalaxyMap, StarRenderer, LaneRenderer, StarSystemNode, MainScene
    UI/            TopBar, SpeedControl, FleetInfoPanel, ColonyPanel, ResourceBar, etc.
    Units/         FleetNode
scenes/            .tscn scene files
tests/             253 xUnit tests (references src/Core/ directly, no Godot dependency)
```

### Key Patterns
- **Call Down, Signal Up:** Parents call children; children emit signals; cross-tree uses EventBus
- **Self-contained scenes:** Each `.tscn` works when instanced alone (F6)
- **EventBus:** Singleton autoload with C# `event Action<T>` delegates (not Godot signals)
- **GameManager:** State container (speed, empires, galaxy ref, master seed). Not logic.
- **Deterministic seeded RNG:** All randomization uses `GameRandom` (wraps `System.Random`). Never use crypto RNG or `GD.Randf()`. Same seed = same results. Subsystems derive child RNGs via `GameRandom.DeriveChild(differentiator)`.
- **Data-driven:** Static data arrays (ResourceDefinition.All, ComponentDefinition.All, ChassisData.All, BuildingData.All)
- **Two-tier tick:** Fast tick (0.1s) for movement/combat; Slow tick (1.0s) for economy/growth/research

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

## Environment Requirements

- Godot 4.3 with .NET support
- .NET 8.0 SDK
- `GODOT4` environment variable for VS Code debugging

# Godot MCP — Claude Code Instructions

This project has a running MCP server (`godot-mcp`) that gives you direct control over a Godot 4 instance. Use it to verify every change you make.

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

## godot_eval — Currently Disabled

The `godot_eval` tool (Roslyn C# scripting) is disabled on Godot 4.6 Windows due to a native crash. Use `godot_scene_tree` and `godot_logs` to inspect live state instead.

## Project Structure

- `Scripts/McpBridge.cs` — TCP autoload that handles MCP commands (do not modify)
- `Scripts/McpLog.cs` — static logger (do not modify)
- `godot-mcp/` — Node.js MCP server
- `project.godot` — McpBridge is registered as an autoload