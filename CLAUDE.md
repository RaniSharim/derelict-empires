# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Derelict Empires — a real-time 4X/5X space strategy game built with **Godot 4.3** and **C# (.NET 8.0)**. The comprehensive game design lives in `DESIGN.md` (1275 lines). The implementation plan is at `.claude/plans/twinkly-sleeping-minsky.md`.

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
  Autoloads/     EventBus, GameManager, DataLoader (registered in project.godot)
  Core/          Pure C# — no Godot dependencies, unit-testable
    Enums/       PrecursorColor, ResourceType, ShipSizeClass, etc.
    Models/      StarSystemData, EmpireData, GalaxyData, etc.
    Random/      GameRandom (seeded deterministic RNG wrapper)
    Systems/     Galaxy generation, pathfinding, combat, etc.
  Nodes/         Godot node scripts (Map/, Units/, Camera/, UI/)
scenes/          .tscn scene files
resources/       .tres data files
tests/           xUnit test project (references src/Core/ source files directly)
```

### Key Patterns
- **Call Down, Signal Up:** Parents call children; children emit signals; cross-tree uses EventBus
- **Self-contained scenes:** Each `.tscn` works when instanced alone (F6)
- **EventBus:** Singleton autoload with C# `event Action<T>` delegates (not Godot signals)
- **GameManager:** State container (speed, empires, galaxy ref, master seed). Not logic.
- **Deterministic seeded RNG:** All randomization uses `GameRandom` (wraps `System.Random`). Never use crypto RNG or `GD.Randf()`. Same seed = same results. Subsystems derive child RNGs via `GameRandom.DeriveChild(differentiator)`.
- **Data-driven:** Static game data in `ResourceDefinition.All` / `ComponentDefinition.All`. Future phases add `[GlobalClass]` Godot Resources as `.tres` files.
- **Two-tier tick:** Fast tick (0.1s) for movement/combat; Slow tick (1.0s) for economy/growth

### Core Data
- **5 precursor colors:** Red (weapons), Blue (info), Green (bio), Gold (trade), Purple (exotic)
- **20 raw resources:** 5 colors × 4 types (SimpleEnergy, AdvancedEnergy, SimpleParts, AdvancedParts)
- **10 components:** 5 colors × 2 tiers (Basic, Advanced)

### Input Actions
Defined in `project.godot`: `left_click`, `right_click`, `pause` (Space), `speed_up` (.), `speed_down` (,), `camera_up/down/left/right` (WASD)

## Environment Requirements

- Godot 4.3 with .NET support
- .NET 8.0 SDK
- `GODOT4` environment variable for VS Code debugging
