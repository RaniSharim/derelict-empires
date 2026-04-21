# Derelict Empires — System View UI Spec

**Scope.** Layout, components, and interaction rules for the System View scene. Supersedes the "System View (zoom-in)" deferred-to-future note in `derelict_empires_ui_spec.md` §13. Follows the overlay/scene conventions established by `derelict_empires_research_ui_spec.md` and `derelict_empires_design_combat_research_ui_spec.md`.

**Supplements.** Color tokens, glass panels, and typography inherit from `derelict_empires_ui_spec.md`. Selection conventions and event-bus architecture extend from `derelict_empires_core_loop_v1.md`. New primitives introduced here (sig/sensor glyphs, bands, sub-tickets, unmoored fleet-POIs, silhouette cards) are defined in this document.

---

## 1. Purpose

The System View is the primary screen where players spend most of their mid-to-late-game time (per GDD §17.1.2: "Secondary, Most Time Spent"). It is where colonies are managed, outposts tended, stations outfitted, salvage sites worked, and tactical awareness of foreign presence at the system scale surfaced.

The view is a **scene replacement** of the galaxy map, not an overlay. When active, the galaxy map is not rendered. Entry is via left-click on a system node in the galaxy map; exit is via `Esc` or the breadcrumb. Topbar persists; all other galaxy-map surfaces (left panel, right panel, floating widgets) are replaced by System View content.

The scene introduces a fourth orthogonal selection context — **Selected Entity** — alongside the existing Selected Fleet / System / POI from the core loop spec. See §11.

---

## 2. Screen Architecture

### 2.1 Frame

| Region | Dimensions | Source |
|---|---|---|
| Topbar | Full width × 44px | Persists from galaxy map |
| Breadcrumb bar | Full width × 32px | System View |
| Main area | Remaining viewport | System View |

Main area is a 2-column grid: **3fr for bands**, **1fr for the right panel** (minimum 240px). There is no left panel in this scene; its functions are absorbed into band headers (sensor coverage) and the right panel's empty state (fleets, contacts, operations log).

### 2.2 Breadcrumb Bar

Height 32px, `GlassDark` fill, bottom border `rgba(60,110,160,0.3)`. Padding `0 16px`, gap 12px.

Left-to-right contents:
- `← GALAXY` — navigation, `Accent` color, clickable to exit the scene
- divider `/` in `TextFaint`
- System name — Syncopate 11px, ALL-CAPS, 3px letter-spacing, `TextBright`
- divider `/`
- Context summary — "N POI · M SHARED · K CONTACTS" in Share Tech Mono 10px `TextDim`
- *margin-left: auto* — YOU chip: label "YOU" + sig glyph + signature number + sensor glyph + sensor number

The YOU chip persists as a self-awareness indicator. Its values roll up all player-owned assets in the current system and update every tick.

### 2.3 Entry and Exit

| Transition | Trigger | Emitted signal |
|---|---|---|
| Enter | Left-click system node on galaxy map | `SystemViewOpened { systemId }` |
| Exit | `Esc` (no deeper modal) or click `← GALAXY` | `SystemViewClosed` |

Selection contexts persist across entry/exit. Re-entering the same system restores the most recent Selected POI and Selected Entity.

---

## 3. Bands

### 3.1 Structure

The bands area is divided into three horizontal bands stacked vertically. Each band consumes 1/3 of the bands-area height. Band membership is **strict** — every POI is assigned to exactly one band at system generation and does not migrate.

| Band | Contains | Coverage bias |
|---|---|---|
| **Inner** | Habitable planets, hostile planets, near-star bodies | Player-occupied colonies saturate sensor coverage locally |
| **Mid** | Asteroid belts, debris fields, minor anomalies | Partial coverage by default; nebulae may null sensors here |
| **Outer** | Ship graveyards, abandoned precursor stations, megastructures, drifting fleets | Dark by default; requires sensor-equipped stations or scouts |

Band membership has no intrinsic mechanical effect on POIs themselves. Its mechanical weight comes from the detection layer (§6) — bands are where sensor coverage and concealment math operate.

### 3.2 Band Anatomy

**Band header** — 28px tall, small divider beneath. Left-to-right contents:
- Band name — Share Tech Mono 10px ALL-CAPS 2.5px letter-spacing, tinted to band color
- Quality label — `CLEAR` (Inner default) / `PATCHY` (Mid default) / `DARK` (Outer default) in `TextDim`
- *ml-auto:* sensor glyph + coverage number + "via {source}" caveat if applicable

**Band body** — horizontal flex, `align-items: flex-start`, gap 14px, padding 12px 16px. Houses POI cards top-aligned.

### 3.3 Band Tinting

Each band body carries a subtle background tint encoding its fog-of-war state:

- **Inner** — `rgba(85,187,255,0.03)` fill, `rgba(85,187,255,0.15)` bottom border. Feels clear.
- **Mid** — `rgba(90,160,230,0.02)` fill, `rgba(90,160,230,0.12)` bottom border. Feels partial.
- **Outer** — `rgba(10,14,22,0.6)` fill + repeating linear gradient scanline pattern (3px transparent / 4px `rgba(40,60,80,0.10)`). Feels dark. This tint is the encoding of patchy sensor coverage.

Tint intensity is static per band in v1. Dynamic tinting based on actual sensor coverage is deferred.

### 3.4 Sensor Coverage per Band

A scalar value per band per tick, rolled up from sensor-equipped player assets reaching that band. Open-ended (no cap). Displayed as a raw integer in the band header.

The "via {source}" qualifier appears when coverage in a band would drop to zero without a specific single asset. It informs the player where their visibility comes from, so destruction of that asset has legible consequence.

---

## 4. POI Card

### 4.1 Uniform Width

All POI cards are **160px wide**, regardless of content. Cards grow vertically when content demands — shared POIs with sub-tickets are taller than single-entity POIs but never wider. This uses the vertical space a band has (~170px) rather than horizontal space (bounded by band width / number of POIs).

### 4.2 Anatomy

Standard card:
- Padding `9px 10px 9px 8px`
- Border `1px solid BorderDim` (or brighter if selected/hovered)
- Left accent bar `3px` wide, colored by POI type
- Contents:
  - **Header row** — type tag (Share Tech Mono 9px `TextDim` 1px letter-spacing) + *ml-auto:* sig glyph + number (and sensor glyph + number where applicable)
  - **Name** — Exo 2 12px weight-500 ALL-CAPS, `TextLabel` or `TextBright` if selected
  - **Status line** — Share Tech Mono 10px, contents depend on state

### 4.3 Left Accent Bar Colors

| POI type | Accent |
|---|---|
| Colony (habitable, player-owned) | `#22dd44` (green) |
| Outpost (barren, player or foreign) | `#ddaa22` (gold) |
| Asteroid belt | `#ddaa22` (gold) |
| Debris field / ship graveyard | Faction glow of aligned color (commonly `#ff5540` Crimson) |
| Abandoned precursor station | Faction glow of aligned color |
| Player-built station | `#55bbff` (Azure) |
| Fleet-POI (unmoored) | `#22dd44` (owner color) |
| Shared POI | Split gradient: top half owner color, bottom half foreign color |
| Unknown / silhouette | `#ff5540` (foreign warning) |

### 4.4 Card States

| State | Treatment |
|---|---|
| **Idle** | Standard fill `rgba(4,8,16,0.88)`, `BorderDim` |
| **Selected** | Fill `rgba(34,136,238,0.14)`, border `#2288ee`, inner shadow `rgba(85,187,255,0.15)` |
| **Hover** | Fill lifted to `rgba(34,136,238,0.07)`, border `BorderMid` |
| **Foreign** | Standard fill, accent bar is foreign empire color |
| **Unmoored** (fleet off-POI) | Border is `1px dashed` instead of solid |
| **Coarse** (POI known, not scanned) | Name displayed as-is, status line reads placeholder (`"unscanned"`); actions limited to SCAN |
| **Undiscovered** | Card not rendered |

### 4.5 Card Variants

**Colony card.** Single-entity by default. Status line shows pop ratio and priority (e.g. `8/10 pops · prod focus`).

**Outpost card.** Status line shows pop count and exploit focus (`3p · mining`).

**Asteroid / debris / graveyard card.** Status line shows exploitation state (`unexploited`, `●extract 67%`, `depleted`).

**Station card** (player-built or repaired precursor). Status line shows key installed modules (`sensor · shipyard`).

**Fleet-POI card.** Rendered when a fleet is off any POI, drifting in the band's void. Dashed border, `FLEET · UNMOORED` type tag, drift glyph `⇠` suffixed to the name. Status line shows activity (`drifting · quiet`).

**Silhouette card.** Unknown contact detected via signature but not resolved to type or identity. Type tag reads `UNKNOWN · SILH` (or `· TYPE` / `· ID` at higher resolution tiers). Name is placeholder `? contact ?`. Status line is rough intel (`large sig · drifting`).

---

## 5. Sub-Tickets (Shared POIs)

A POI is **shared** when it hosts more than one entity — typically your outpost plus a foreign fleet, or your station plus a foreign outpost. The POI card expands vertically to show a sub-ticket per entity.

### 5.1 Card Structure with Sub-Tickets

- Header block (POI type tag, name, signature) occupies top ~42px of the card
- 1px divider beneath header
- Sub-ticket list fills remaining height

### 5.2 Sub-Ticket Row

Height 24px. Left-to-right contents:
- Ownership accent — 2px left border (your color `#55ccee` or foreign empire's glow color)
- Status dot — 8px colored by ownership
- Entity label — Share Tech Mono 10px, `TextLabel`, truncating with ellipsis if too long
- Secondary chip — resolution tag `[id]` / `[type]` / `[silh]` for foreign entities only
- *ml-auto:* small right chevron `▸` (clickable affordance)

### 5.3 Rules

- Sub-tickets appear **only** when the POI hosts more than one entity. Single-entity POIs stay compact.
- Sub-ticket order: your entities first, then foreign entities grouped by empire.
- Clicking a sub-ticket selects that Entity (§11). Clicking the POI header selects the POI (location-level).
- Cap: ~5 sub-tickets visible inline; beyond that, scroll within the card body.

---

## 6. Detection Layer

### 6.1 Glyphs

Two SVG glyphs used throughout the scene. Both render at 12×10px (card-level) or 11×9px (sub-label), tinted at draw time:

- **Signature** — `radar-cross-section` (Delapouite, game-icons.net / CC BY 3.0). Path: `IconMapping.SignatureIcon` → `res://assets/icons/detection/signature.svg`. Conveys *emitting*. Default color `UIColors.SigIcon` (`#ffcc44`).
- **Sensor** — `radar-dish` (Lorc, game-icons.net / CC BY 3.0). Path: `IconMapping.SensorIcon` → `res://assets/icons/detection/sensor.svg`. Conveys *listening*. Default color `UIColors.SensorIcon` (`#55bbff`).

Render via `DetectionGlyph` (Control subclass) for standalone use, or `DetectionGlyph.CreateLabel(kind, pixelSize, text)` for the common glyph-plus-number HBox.

Both glyphs are followed by a scalar number. No unit for signature; sensor uses the same raw scalar in UI (internally `b` for bands if helpful in tooltips).

### 6.2 Values

Signature and sensor are **open-ended scalars**. No defined cap. Indicative ranges for tuning:

| Thing | Indicative sig |
|---|---|
| Idle scout fleet | 2–5 |
| Small outpost | 8–15 |
| Colony (mid-size) | 40–70 |
| Large active colony (forges running) | 70–120 |
| Combat fleet in engagement | 120–200+ |
| Capital-class asset at full activity | 200+ |

Sensor scales similarly. Concrete thresholds and falloff curves are a tuning concern; the UI treats them as display-only integers.

### 6.3 Qualitative Labels

Human-readable labels accompany raw numbers where context helps (`sig 67 loud`, `sig 3 quiet`). Labels are derived from tuning tables mapping number ranges to: `quiet` / `moderate` / `loud` / `very loud` for signature, `partial` / `substantial` / `saturated` for sensor. Labels are **never primary** — they follow the number, never replace it.

### 6.4 Per-Band Coverage

Band header displays a single aggregate sensor-power number rolled up from all player-owned sensor-equipped assets whose reach includes that band. Appears as `<sensor glyph> COVERAGE N` in Share Tech Mono 10px.

When the number would be zero without a specific single asset, append ` · via {source name}` in `TextFaint`.

### 6.5 Detection Math Surfacing

Detection is a passive simulation — the player never actively scans per band. The UI's job is to surface the math, not ask for input. Specifically:

- Every POI card shows its current signature in the header corner.
- Every fleet card shows its current signature (and sensor, if sensor-equipped).
- The breadcrumb YOU chip shows the player's aggregate system-level signature and sensor.
- Foreign entities show their observed signature with approximation prefix `~` when resolution is coarse.

---

## 7. Right Panel

### 7.1 Frame

Width: 1/4 of main area, minimum 240px. Extends from below the breadcrumb to the bottom of the viewport. Left edge border `1px solid rgba(60,110,160,0.3)`. Background `rgba(4,8,16,0.94)`.

The panel has two modes: **empty state** (nothing selected), and **selected-entity** (a specific entity is selected via POI card or sub-ticket).

### 7.2 Empty State — Situation Dashboard

Displayed when Selected Entity is null. Vertical sections top-to-bottom:

- **Header** — "SITUATION · no selection · overview" in Share Tech Mono 10px
- **Foreign presence** — list of empires detected in the system with entity counts ("Crimson Forge · outpost · fleet · at Helion II"). Unknown silhouettes listed separately.
- **Running ops** — list of player ongoing operations in this system: extracts, builds, scans, drifting fleets. Each item shows activity + progress bar where applicable.
- **Recent** — last 3–5 system-scoped events with elapsed time ("Bone Fields scanned · 0:42 ago").

### 7.3 Selected-Entity State

Header row: 3px left accent bar colored by entity type + entity name (Syncopate 13px ALL-CAPS 2.5px letter-spacing) + type badge + sig/sensor glyphs with values.

Body sections below vary by entity type (§8–§10). Footer row always contains actions: `CLAIM` / `GOV ▾` / `DISPATCH ▸` where applicable, or entity-specific verbs.

### 7.4 Panel Density Target

The panel is narrow by design. Sections stack vertically; per-section height is minimized aggressively. The panel is expected to scroll only when queue/list content exceeds its natural extent. Current v1 density is a **tunable baseline** — visual audit during implementation may push content tighter or loosen it. See §16 for the pillar-check read on this.

---

## 8. Colony Panel (Selected-Entity: Colony)

### 8.1 Sections

| Section | Height target | Content |
|---|---|---|
| Header | ~50px | Name, type badge, sig+sensor |
| Status row | ~24px | `POPS 8/10 · HAPPY 72 · PRIO: prod ▾` (single line) |
| Buildings · Pops | ~180px | Unified list (§8.2) |
| Detection | ~44px | 2-line inline block (§8.3) |
| Actions | ~34px | `CLAIM · GOV ▾ · DISPATCH` |

### 8.2 Buildings · Pops (Unified Section)

This section replaces the previously-planned separate "Allocation" and "Buildings" sections from the GDD. **Pops are assigned to buildings, not to abstract work pools.** The colony's Production / Research / Food / Mining output is the sum of what its buildings produce.

Each building is one row in the list. Row states:

**At rest** (24px tall, single line):
- Building name (Share Tech Mono, 42px-wide label, tinted to color family)
- Slot visualization — `●` filled + `·` empty + `◆` expert (filled) / `◇` expert (empty), letter-spaced for legibility
- *ml-auto:* output summary (`+3 red`)
- Trailing sig contribution (`·+12` in `TextDim`)

**Focused / in edit mode** (row expands in place, ~60px tall):
- Header line — name, tier badge, color tag, *ml-auto:* output + sig contribution
- Slot chip row — 24×24px clickable chips (filled / empty / expert-filled / expert-empty). *ml-auto:* `UPG` and `SCRAP` buttons inline.
- Click a filled chip to unassign (pop returns to the unassigned pool). Click empty chip to assign (pop pulled from unassigned, or from lowest-priority matching pool if unassigned is empty).

**Under construction** (24px tall, dimmed tint):
- Building name + ◌ icon + `building` label + inline progress bar + % number

**Empty slot terminator** (24px tall, `+ ADD` row):
- `+ ADD` label + `build on empty slot` hint + remaining-slots count

A single building is in focused-edit state at a time. Clicking outside the section or another building row collapses the focused one.

### 8.3 Detection Block

Compressed two-line inline section. Header row: `DETECTION · ● at-risk` + *ml-auto:* `MITIG ▾` button. Body line: signature sources (`pops · forge`) + range (`2b`) + observers (`watched Crimson [id]`).

The MITIG dropdown reveals the mitigations list — one row per available action, each a mini-button (`THROTTLE EXTRACT · −1 sig`, `BUILD DAMPER (AZURE T2)`, `DISPATCH PICKET FLEET ▸`). Mitigations list is dynamic based on colony state and available tech.

### 8.4 Automatic vs. Manual Pop Assignment

The model assumes automatic assignment by colony priority (`prod` / `res` / `food` / `mine` / `balanced`). The player sets priority via the dropdown in the status row; `ColonyManager` reshuffles pops to matching buildings on priority change. Manual overrides via slot chips persist until the player manually un-overrides (right-click a slot: "clear override" pop-item).

### 8.5 Expert Slots

Certain buildings grant expert slots — visually `◆` (filled) / `◇` (empty). Only pops flagged as experts can fill them, and they produce 2× the building's base output. Expert-pop flagging is a separate mechanic (deferred to the Pops & Species design pass); from the System View's perspective, expert slots behave like standard slots with a different glyph and a different pop-eligibility check.

---

## 9. Outpost, Station, and Salvage Panels

### 9.1 Outpost Panel

Header row + status row collapse to a single two-line block (name, type, sig, pop count). Body:
- **Allocation** — 2 pools only (Mining, Salvage) using a compact stacked bar
- **Yield** — one line per active resource with rate per minute
- **Detection** — same block as Colony §8.3
- **Actions** — `RELOCATE` / `UPGRADE` / `SCRAP` / `GARRISON ▸`

No Buildings section — outposts don't support buildings (per GDD §10.1.1). Exploit selection lives as a dropdown in the Allocation block.

### 9.2 Station Panel

Header + status. Body:
- **Modules** — installed module grid, max 4/6 slots (Shipyard / Defense / Logistics / Trade / Garrison / Sensors); empty slots shown as dashed `+ MODULE` placeholders
- **Construction** — current ship-in-progress row with progress bar; empty state shows `+ QUEUE SHIP` button
- **Logistics** — supply range, fleet upkeep modifier, stockpile capacity (three-line readout)
- **Actions** — `UPGRADE` / `RENAME` / `SCRAP`

Module upgrade and swap interactions follow the focused-row pattern from §8.2.

### 9.3 Salvage / Derelict Panel

Header + status. Body:
- **Remaining yield** — one bar per resource present in the site, with remaining amount; depletion-curve hint in `TextFaint` beneath
- **Extraction** — assigned fleet, current rate, affinity multiplier
- **Intel** — hulk count, age, signature type, known hazards
- **Actions** — `CANCEL EXTRACT` (if active) / `SEND FLEET ▸` (if idle) / `CLAIM` / `BUILD OUTPOST` (if feasible)

### 9.4 Enemy Entity Panel (Intel-Only)

When a foreign entity is the Selected Entity (via sub-ticket click on a shared POI, or direct selection of a silhouette card), the right panel shows an **intel-only** variant:

- Header has a warning-red accent bar
- Body consists of only two sections: **Observed** (what we know — position, signature, resolution tier, last seen activity) and **Diplomatic** (claims filed, relations status, message button)
- No management affordances; actions are diplomatic only (`MESSAGE`, `DEMAND`, `THREATEN`, etc. — subject to the Diplomacy spec)

---

## 10. Entity Tab Strip (Shared POI Secondary Nav)

When a shared POI is the Selected POI, and the Selected Entity is one of its sub-entities, the right panel header carries a horizontal tab strip beneath the POI name. Each tab represents one entity on the POI; the selected tab is the current Selected Entity.

- Tab width: 2-4 tabs fit without overflow. 5+ tabs: horizontal scroll with auto-center on selection (browser-tab overflow pattern).
- Tab anatomy: 2px left border (ownership color) + small label (ownership label like `YOU` or `CRIMSON` + resolution tag `· TYPE` / `· ID` if foreign) + entity-type line.
- Clicking a tab sets Selected Entity; the panel body swaps accordingly.

The tab strip is secondary navigation. Sub-tickets in the band strip remain the primary entity selection affordance; tabs are for switching among entities on the POI you're already inspecting.

---

## 11. Selection Model

### 11.1 Four Orthogonal Contexts

Extending `derelict_empires_core_loop_v1.md` §6.1:

- **Selected Fleet** — unchanged. At most one fleet at a time.
- **Selected System** — unchanged. At most one system at a time.
- **Selected POI** — unchanged. Must belong to the currently Selected System.
- **Selected Entity** — new in this spec. At most one entity at a time. Must belong to the currently Selected POI (if the POI is shared) or to a standalone POI (in which case the Entity *is* that POI's primary and only content).

All four are orthogonal; changing one never clears another.

### 11.2 Click Rules

Within the System View:

| Target | Effect |
|---|---|
| POI card header | Sets Selected POI; does not set Selected Entity (empty-state panel shows POI-level digest). |
| Sub-ticket row | Sets Selected Entity to that entity; Selected POI remains unchanged. |
| Card body on single-entity POI | Sets both Selected POI and Selected Entity (since they're equivalent here). |
| Entity tab in right panel | Sets Selected Entity to that tab's entity. |
| Empty band space | Clears Selected POI and Selected Entity; does not change Selected Fleet. |
| Action button (`SCAN`, `EXTRACT`, slot chip, etc.) | Issues the relevant order; does not change any selection. |

Right-click semantics from the core loop spec continue to apply (right-click a system on the galaxy map issues `MoveToSystem`). Within the System View, right-click is reserved for future context-menu use; for v1 it is a no-op except for slot chips (right-click to clear manual override).

### 11.3 Esc Semantics

- If a focused building row is expanded: `Esc` collapses it.
- Else if Selected Entity is set: `Esc` clears it (right panel reverts to empty state).
- Else if Selected POI is set: `Esc` clears it.
- Else: `Esc` exits the scene (same as clicking `← GALAXY`).

---

## 12. Keyboard Shortcuts

| Key | Effect |
|---|---|
| `Esc` | See §11.3 |
| `Tab` / `Shift+Tab` | Cycle Selected Entity through the current Selected POI's entities (for shared POIs) |
| `1`–`9` | Jump Selected POI to the Nth POI card in the bands (top-to-bottom, left-to-right reading order) |
| `F` | Focus the Foreign Presence section in the right panel empty state |
| `Space` | Pause / resume (galaxy-wide, per existing convention) |
| `1` / `2` / `4` / `8` | Game speed (conflict with POI jump: when a POI card is hovered or selected, number keys are speed; otherwise, they're POI jump — subject to refinement during implementation) |

The number-key collision between speed control and POI jump is noted as a tuning item. Likely resolution: POI jump moves to `Ctrl+N` or arrow keys.

---

## 13. Color Tokens and Typography

### 13.1 Tokens Inherited

From `derelict_empires_ui_spec.md` unchanged: `GlassDark`, `BorderBright` / `BorderMid` / `BorderDim`, `Accent`, `TextBright` / `TextLabel` / `TextBody` / `TextDim` / `TextFaint`, faction base and glow colors.

### 13.2 New Tokens

| Token | Value | Use |
|---|---|---|
| `OwnerYou` | `#55ccee` | Sub-ticket accent bar + ownership label for the player empire |
| `SigIcon` | `#ffcc44` | Signature glyph default color |
| `SensorIcon` | `#55bbff` | Sensor glyph default color |
| `SlotFilled` | `#ffcc44` | Worker slot chip fill (filled state) |
| `SlotExpert` | `#55bbff` | Expert slot chip fill (distinguishes from worker) |
| `BandInnerTint` | `rgba(85,187,255,0.03)` | Inner band body fill |
| `BandMidTint` | `rgba(90,160,230,0.02)` | Mid band body fill |
| `BandOuterTint` | `rgba(10,14,22,0.60)` | Outer band body fill |
| `BandOuterScanline` | `rgba(40,60,80,0.10)` | Outer band scanline stripe color |

### 13.3 Typography (no new faces)

Syncopate for system name in breadcrumb and entity names in panel headers. Exo 2 for POI card names. Share Tech Mono for all numeric data, type tags, and code-like labels. Barlow Condensed inherits from the theme for general UI text (none introduced in this spec specifically).

Minimum interactive text size 10px (11px for non-interactive captions per the design system). Slot chips use a 12px glyph at 24px box.

---

## 14. Event Bus Signals

### 14.1 New Emitted Signals

- `SystemViewOpened { systemId }` — emitted by `GalaxyMapScreen` on system node left-click.
- `SystemViewClosed { }` — emitted by `SystemViewScene` on Esc or breadcrumb exit.
- `EntitySelected { entityType, entityId, poiId }` — emitted when Selected Entity changes. Consumers: right panel state, band strip for selection highlight.
- `EntityDeselected { }` — emitted on Esc or empty-band-space click.
- `BuildingSlotToggled { colonyId, buildingId, slotIndex, newState }` — emitted when a slot chip is clicked. Consumer: `ColonyManager` updates pop assignment, emits `ColonyPopsChanged`.
- `BuildingFocused { colonyId, buildingId }` / `BuildingBlurred { }` — emitted by the buildings list as rows expand/collapse.

### 14.2 New Consumed Signals

- `ColonyPopsChanged` / `ColonyBuildingsChanged` — update the buildings list.
- `SensorCoverageChanged { systemId, band, newCoverage, sources }` — updates band header sensor chip.
- `SignatureChanged { entityId, newSignature }` — updates the sig corner on the relevant card and aggregates in the YOU chip.
- `ForeignEntityObserved { entityId, resolution }` — renders or updates a silhouette card, or upgrades its resolution tier in place.
- `ForeignEntityLost` — removes a silhouette/type-resolved card from the band strip; last-seen info remains in the right panel Foreign Presence section for a grace period.

### 14.3 Architectural Notes

No manager is called directly from UI. All UI-triggered actions emit signals; managers react and emit state-change signals back. This matches the existing core-loop and research specs. The `SystemViewState` autoload (§15) aggregates derived state for UI consumption but does not mutate simulation.

---

## 15. Godot Implementation Notes

### 15.1 Scene Structure

```
SystemViewScene (Control, z=0, fills viewport)
├── TopbarHost (shared scene from galaxy map, z=100)
├── Breadcrumb (HBoxContainer, z=50)
│   ├── BackButton
│   ├── SystemNameLabel
│   ├── ContextSummaryLabel
│   └── YouChip (sig + sensor glyphs + numbers)
├── MainGrid (GridContainer columns=2, 3fr : 1fr)
│   ├── BandsArea (VBoxContainer, separator-style)
│   │   ├── InnerBand (BandRow scene)
│   │   ├── MidBand
│   │   └── OuterBand
│   └── RightPanel (PanelContainer)
│       └── RightPanelContent (VBoxContainer — empty state or selected-entity)
└── SubOverlays (hidden by default — mitigations, governor assign, etc.)
```

`BandRow` is a reusable scene containing: header HBox + body HBoxContainer (for POI cards). `POICard` is a reusable scene; its internal layout varies by card variant but all share the 160px width and standard anatomy.

### 15.2 Autoloads / Services

- **`SystemViewState`** (new) — holds the current scene's derived UI state: which system is viewed, current Selected Entity, building-focus state, mitigation-dropdown open state. Emits `SystemViewStateChanged` when any field mutates. Lives only while `SystemViewScene` is instanced.
- **`SelectionBus`** (may already exist from core loop — extend if present) — carries the four orthogonal selections.
- Existing autoloads `GalaxyManager`, `FleetManager`, `ExplorationManager`, `ResourceManager`, `ColonyManager` (per implementation plan) are all consumed; none newly introduced here except `SystemViewState`.

### 15.3 Rendering Notes

- Band outer scanline — CSS-equivalent via `ColorRect` + shader, or a pre-baked texture tiled. Pre-baked is simpler and cheaper.
- Sig and sensor glyphs should be SVG resources imported once and referenced via `TextureRect` or custom drawing. Avoid drawing them anew per-frame per-card; cache the textures.
- Slot chips use a single `StyleBoxFlat` per state (filled / empty / expert-filled / expert-empty). Four StyleBoxes, shared across all slot instances.
- Band POI flex container uses a `HBoxContainer` with `alignment=ALIGNMENT_BEGIN` and a fixed gap via `theme_constants/separation`. Top-alignment is the default for VBoxContainer children of `HBoxContainer`, so this comes for free.

### 15.4 Performance

The System View re-renders its bands on any `SiteDiscovered`, `FleetEnteredSystem`, `ForeignEntityObserved`, or `EntitySelected` signal. Diff re-rendering is expected — only the affected POI card node updates, not the whole bands area. The right panel fully rebuilds on Selected Entity change; empty-state → selected-state is a full swap of `RightPanelContent` children.

---

## 16. Deferred Items

Explicitly not designed in this spec, tracked for future passes:

| Item | Intended home |
|---|---|
| Active scanning (player-initiated high-resolution pings, signature spike tradeoff) | Fleet disposition design pass |
| Passive-vs-active sensor mode on fleets | Same |
| Multi-building construction queue order control | Colony queue design pass |
| Pop type flags (expert, civilian, specialist) | Pops & Species design pass |
| Drag-to-reorder pop assignment across buildings | UX polish pass |
| Band tint dynamic intensity based on current coverage | UX polish pass |
| Nebula-terrain effects on specific bands | Terrain design pass |
| Multi-sub-ticket scroll behavior for 6+ entities | Only if playtesting shows >5 entities at a POI is common |

---

## 17. Pillar Check

Per the strategy-game-ux skill, end-of-spec pillar verification.

| Pillar | Status | Notes |
|---|---|---|
| 1. Minimal micromanagement | ✅ | Pops auto-assign by priority; manual slot override is rare-action-priced. Detection is pure simulation, never a player input task. Passive sensors, passive signatures, no per-tick scanning. |
| 2. The map is the game | ✅✅ | System View is the map zoomed in. Bands are spatial fiction *and* mechanical terrain. Scene replacement is accepted because the scene is the map-at-zoom, not an abstraction. |
| 3. Salvage everything | ✅ | Outer band is a first-class spatial home for ruins and derelicts; salvage sites get their own panel variant with intel and depletion visibility. |
| 4. Nothing is locked | ✅ | Cross-color mitigations (e.g. Azure damper module in a non-Azure colony) are buildable with efficiency penalties, not blocked. |
| 5. Cooperation and competition | ✅✅ | Shared POI pattern (sub-tickets) is the direct UI embodiment of "no automatic territory." Competing extraction visibility surfaces the cost of sharing a belt without claiming it. |
| 6. Information asymmetry | ✅✅ | Strongest upgrade. Signature + sensor scalars, resolution tiers, band coverage, silhouette cards, YOU chip. Spatial fog-of-war becomes legible without collapsing the asymmetry. |
| 7. The 5th X — eXchange | ⚪ | Trade is empire-scoped, not system-scoped. Absence from this spec is correct. The diplomatic intel block on enemy entities is a small bridge to the Diplomacy spec. |

### 17.1 Open Tensions to Revisit

- **Right panel density.** Captured at a compact baseline (§7.4) but not validated against 10+ hours of play. Expect to tune during implementation; section heights marked "target" are reviewable.
- **Number-key shortcut collision** (§12). Speed control vs. POI jump needs a resolution before shipping.
- **Auto-assignment priority pool** (§8.4). If priority dropdown's options (`prod` / `res` / `food` / `mine` / `balanced`) don't match the tuning reality that comes out of playtesting, the option set changes. Not a structural concern.
- **Foreign-entity scrolled-off behavior.** When a silhouette card's underlying contact moves out of sensor range, the card should linger briefly with a "last seen" mark, not disappear immediately. Exact grace-period value is tuning.

---

*End of spec. Next design pass: empty-state operations dashboard content depth (running ops layout, recent events formatting), and the Diplomacy interaction surface that the enemy-entity intel panel hands off to.*