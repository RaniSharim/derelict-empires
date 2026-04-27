# UI rules

UI scripts in this directory follow strict rules so panels stay decoupled from game state.

## Architecture

- **Scene-first.** Top-level panels live in `scenes/ui/*.tscn`. Reusable rows/cards (`fleet_card`, `event_log_entry`, `faction_resource_box`, `sub_ticket_row`, `entity_panel_header`) are sub-scenes instanced from the parent. Layout in editor; scripts only handle data binding + event wiring.
- **Read** through `IGameQuery` (`GameManager.Instance` implements it). Panels never reference `GameSystems` or `MainScene` directly. `IGameQuery` exposes the player empire, fleets, ships, galaxy, exploration/scan/contributing-fleet helpers, salvage capability/active-count, fleet orders, settlements/stations runtime, and tech/research state.
- **React** to change events on `EventBus` (subscribe in `_Ready`, unsubscribe in `_ExitTree`). Always unsubscribe — `EventBus` is an autoload and would otherwise hold the panel forever.
- **Write** by firing intent events on `EventBus` (e.g. `FireScanToggleRequested(poiId)`, `FireExtractToggleRequested(poiId)`, `FireFleetMoveOrderRequested(fleetId, targetSystemId)`, `FireCameraPanToWorldRequested(pos)`). A handler Node (`SalvageActionHandler`, `MovementActionHandler`) validates and forwards to the system. UI never mutates game state directly.
- **Compose, don't poll.** `_Process` is for animations/canvas redraws only — for state, subscribe to the event that signals the change. `ResearchStrip` refreshes on `SlowTick` + project-change events; `FactionResourceBox` guards its label assigns with a "value changed" check.
- **Control over PanelContainer for fixed-size panels** — PanelContainer fights with manual sizing.

## Fonts & Colors

Typography and the precursor color palette are centralized — no ad-hoc fonts, sizes, or colors in panels.

- **Fonts:** Two-font system in [`UIFonts.cs`](UIFonts.cs).
  - `UIFonts.Title` = Exo 2 SemiBold @ `TitleSize` (16) — fleet/POI/system/colony names.
  - `UIFonts.Main` = B612 Mono Bold @ `NormalSize` (14) or `SmallSize` (12) — everything else.
  - **12px is the hard floor.** Call `UIFonts.Style(label, UIFonts.Main, UIFonts.SmallSize, …)` or `UIFonts.StyleRole(label, UIFonts.Role.Small)`.
  - Fonts loaded via `FileAccess` (not Godot's import pipeline). Rendering settings (Hinting=Normal, SubpixelPositioning=Disabled, ForceAutohinter=false, Antialiasing=Gray) applied programmatically.
  - Legacy role names (`UILabel`, `TitleMedium`, `DataSmall`, etc.) alias to the 3-tier system for backward compat.
- **Colors:** Precursor tokens live in [`UIColors.cs`](UIColors.cs). Each of the 5 precursor colors has four presets:
  - `{Color}Bright`, `{Color}Normal`, `{Color}Dim`, `{Color}Bg` (e.g. `RedBright`, `BlueDim`).
  - Enum-driven lookup: `UIColors.GetPrecursor(PrecursorColor.Red, UIColors.Tone.Bright)`.
  - `GetFactionGlow` / `GetFactionBg` map to the `Normal` / `Bg` tones.
- **Theme:** Project-wide defaults live in `resources/ui/theme.tres`, built by [`ThemeBuilder.cs`](ThemeBuilder.cs). Per-control overrides are reserved for intentional accents.
- **Icons:** Faction emblems and resource SVGs from game-icons.net are centralized in [`IconMapping.cs`](IconMapping.cs).
- **Debug overlays:** `F11` toggles the font showcase. `F10` saves a PNG to `screenshots/`. `F12` toggles exclusive fullscreen.

## Subdirectories

- `SystemView/` — In-system entity panels (Colony, Outpost, Station, Fleet POI, Salvage, Enemy) and shared chrome (header, tab strip, building rows, slot chips, sub-tickets, POI cards).
- `CombatHUD/` — Pre/during/post combat overlays: pre-combat dialog, battle bar, our/their fleet panels, debrief, live event toasts, salvage projection chip.
- `ShipDesigner/` — Ship and fleet-template editor: chassis picker, slot matrix, module picker, profile pane, unlock picker.
