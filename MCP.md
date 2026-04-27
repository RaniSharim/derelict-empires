# Godot MCP — Iteration Guide

This project has a running MCP server (`godot-mcp`) that gives you direct control over a Godot 4 instance. Use it to verify every change.

You also have the `/godot-4x-csharp` skill — use it.

## The Iteration Loop

Follow this loop for **every** change:

1. **Batch all related file edits first** — never reload between individual file changes. C# recompilation takes 10–30 seconds.
2. **`godot_reload`** — triggers C# recompilation and restarts the scene.
3. **`godot_stdout`** immediately — if compilation failed, the error is here. The bridge will not be up. Fix the error and reload before calling any other tool.
4. **`godot_screenshot`** — verify the scene renders correctly. **Only works in windowed mode** (pass `headless: false` to `godot_start`/`godot_reload`).
5. **`godot_scene_tree`** — verify node structure matches expectations.
6. **`godot_logs`** — check for runtime errors or warnings from `_Ready()` and early frames.
7. Repeat from step 1.

## Headless vs Windowed

Both `godot_start` and `godot_reload` accept an optional `headless` parameter:

- **`headless: true`** (default) — no window, no GPU needed, faster. Screenshots return an error. Use for logic-only changes.
- **`headless: false`** — opens a Godot window, screenshots work. Use when you need to verify visuals.

Editing logic / data → headless. Editing UI / rendering / shaders → windowed.

## Compilation Failures

If `godot_reload` completes but `godot_screenshot` hangs or the bridge does not respond, **always check `godot_stdout` first**. C# compile errors only appear there. The bridge never starts if the build fails, so no other tools will work until the error is fixed and the scene is reloaded.

## Scene Tree as Ground Truth

After every reload, call `godot_scene_tree` before making assumptions about what nodes exist. Scenes can fail to instantiate silently if a script throws in `_Ready()` — this won't appear as a compile error in stdout but will appear in `godot_logs`.

## Process Lifecycle

- Call `godot_start` once at the beginning of a session (with `headless: false` if you need screenshots).
- Use `godot_reload` for all subsequent restarts (this is the primary iteration tool).
- Call `godot_stop` when done.
- **Never call `godot_start` when a process is already running** — it will error. Use `godot_reload`.
- Switch between headless and windowed by passing a different `headless` value to `godot_reload`.

## Logging

Use `McpLog.Info()`, `McpLog.Warn()`, `McpLog.Error()` (not bare `GD.Print`) so logs are captured by the MCP bridge.

## Debugging the EventBus cascade

`EventBus` can attach a debug subscriber that logs every fired event as `[evt tick=N] EventName { payload }` through `McpLog`. Opt in via env var at process spawn:

```
godot_start  { env: { "DEBUG_EVENTBUS": "1" } }           // default blocklist
godot_reload { env: { "DEBUG_EVENTBUS": "1",
                      "DEBUG_EVENTBUS_FILTER": "FleetSelected,FleetDeselected" } }
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

- Tests **skip with a warning** if `GODOT_BIN` env var is not set.
- One Godot instance per test run (shared via xUnit collection fixture).
- Each test loads a pre-designed save file from `tests/E2E/Fixtures/`.
- Tests split by trait: `[Trait("Category", "Headless")]` vs `[Trait("Category", "Visual")]`.
- Run: `dotnet test tests/E2E/DerlictEmpires.E2E.csproj`.

## godot_eval — Currently Disabled

The `godot_eval` tool (Roslyn C# scripting) is disabled on Godot 4.6 Windows due to a native crash. Use `godot_scene_tree` and `godot_logs` to inspect live state instead.

## MCP Bridge Files

- `Scripts/McpBridge.cs` — TCP autoload that handles MCP commands (do not modify).
- `Scripts/McpLog.cs` — static logger (do not modify).
- `godot-mcp/` — Node.js MCP server.
- `project.godot` — McpBridge is registered as an autoload.
