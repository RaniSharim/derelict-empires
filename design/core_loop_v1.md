# Derelict Empires — Core Loop v1 (MVP)

**Supplement to Systems Design v2 and Implementation Plan — April 2026**

This document specifies the first playable slice: the **explore → scan → extract** gameplay loop. It is a scoped subset of the Exploration & Salvage system (Systems Design v2, §6) with supporting fleet movement, galaxy generation, and UI. Where this document simplifies or defers a design decision from prior docs, it is marked explicitly. Nothing here is intended to contradict the long-term design — it is a staged implementation target.

---

# 1. Scope

## 1.1 In Scope

- A procedurally generated galaxy of connected star systems with POIs.
- Salvage sites placed at a subset of POIs, color-aligned by galaxy arm.
- A single player empire with a small starting fleet.
- Fleet movement along visible lanes with animated in-transit feedback.
- POI discovery on system entry (coarse scan).
- An explicit SCAN action that advances per-site knowledge to a full reveal.
- An EXTRACT action that streams color-matched resources into the empire stockpile with a diminishing-returns curve.
- Galaxy map UI with left panel (fleet list), right panel (POI list + actions), topbar resource HUD, and recent-events toast feed.

## 1.2 Out of Scope (Deferred)

| Deferred | Planned For |
|---|---|
| Survey tiers (minimal / basic / detailed / deep) | Post-MVP pass on Exploration |
| Per-subsystem `SalvageLayer` structure | Same pass |
| Hazards (guardian fleets, contamination, equipment damage) | Requires Combat |
| Outpost extraction mode | Colony system |
| System view (zoom-in) | Separate screen pass |
| AI empires, shared exploitation, claims | Phase 4+ |
| Combat, supply, morale | Phase 2+ |
| Survey tiers affecting detection distance | Covered by 1.1 simplification |
| Intra-system fleet positioning | Added as a second layer later, non-breaking |

## 1.3 Resolved Design Decisions

| # | Decision | Resolution |
|---|---|---|
| C1 | Fidelity of first slice | Mid-fidelity: multiple site types + color-matched yields. No layers, tiers, or hazards. |
| C2 | Zoom-in views | Galaxy map only. POIs live in the right panel. System view is a future separate screen. |
| C3 | Discovery vs scan | Two distinct actions. System entry yields a coarse reveal; explicit SCAN action reveals full yield breakdown. |
| C4 | Starting fleet | One Scout and one Salvager, both docked at the home system. Builder deferred. |
| C5 | Galaxy scale for iteration | 20-system test galaxy. Generator is config-driven so the same code produces 100 later. |
| C6 | Resource model | Full 30-entry `ColoredResource` stockpile from day one; only simple-tier yields populated by MVP sites. |
| C7 | Fleet movement | Animated along lanes in real time. Fleet icon slides. Intra-system travel is abstracted away. |
| C8 | Single-empire sandbox | One player, no AI, no combat, no diplomacy. |
| C9 | POI row during scan/extract | Inline progress bar + fleet name on the row itself. No popover. |
| C10 | Order queueing | None. Manual EXTRACT press after scan completes. |
| C11 | Input model | Left-click selects, right-click commands. Fleet and system selections are orthogonal — neither clears the other. Right-click a system with a fleet selected issues `MoveToSystem`. |

---

# 2. Loop Overview

A fleet is always in exactly one of two locations: `InSystem(system)` or `OnLane(from, to, progress)`. A salvage site lives at a POI, which lives in a system, and each site has a per-empire knowledge record that walks `Undiscovered → CoarseKnown → Scanned`. Under an active extract order, a site's `RemainingYield` depletes along a diminishing-returns curve but never reaches zero.

Three fleet orders drive the entire loop:

- **MoveToSystem** — traverses lanes; on arrival, triggers discovery of sites in the destination system.
- **ScanSite** — accumulates scan progress on a specific site; completion flips knowledge to Scanned.
- **ExtractSite** — streams resources from the site into the empire stockpile every tick.

That is the whole MVP loop.

---

# 3. Data Model

All types are pure C# under `src/Core/Models/`, with no Godot dependency. They are serializable for save/load.

## 3.1 Galaxy Topology

```csharp
public class StarSystem
{
    public Guid Id;
    public string Name;
    public Vector2 MapPosition;             // galaxy-map coordinates
    public PrecursorColor? ArmAlignment;    // nullable; core systems may be unaligned
    public List<Guid> PointOfInterestIds;
    public List<Guid> LaneIds;
}

public class Lane
{
    public Guid Id;
    public Guid SystemAId, SystemBId;       // undirected
    public float LengthUnits;               // drives transit time
    public bool IsHidden;                   // unused in MVP; placeholder for Hauler traits
}

public class PointOfInterest
{
    public Guid Id;
    public Guid StarSystemId;
    public PoiType Type;                    // Planet, Asteroid, DebrisField, ShipGraveyard, AbandonedStation, Derelict
    public string Name;
    public Guid? SalvageSiteId;             // null if this POI has no salvage
}
```

## 3.2 Salvage Site (MVP-Simplified)

The full Systems Design v2 model (`SalvageLayer`, hazards, survey state) is reduced to a flat, color-matched yield table. The per-subsystem layering can be added later by introducing a `Layers` dictionary alongside `RemainingYield` without breaking existing code.

```csharp
public enum SalvageSiteType
{
    MinorDerelict,         // small, fast, low yield
    DebrisField,           // medium, broad color spread
    ShipGraveyard,         // large, high yield, color-pure
    AbandonedStation       // large, color-pure, skews toward components
}

public class SalvageSite
{
    public Guid Id;
    public Guid PointOfInterestId;
    public SalvageSiteType Type;
    public PrecursorColor PrimaryColor;

    public float ScanDifficulty;            // total scan points required to fully reveal

    public Dictionary<ColoredResource, float> TotalYield;
    public Dictionary<ColoredResource, float> RemainingYield;
    public float DepletionCurveExponent;    // ~0.5 — first 50% extracts quickly, last 10% slowly
}
```

## 3.3 Per-Empire Site Knowledge

Knowledge is stored per (empire, site) pair rather than on the site itself. This keeps the site a pure world object and makes shared exploitation a zero-schema-change feature later.

```csharp
public enum SiteScanState { Undiscovered, CoarseKnown, Scanned }

public class SalvageSiteKnowledge
{
    public Guid EmpireId;
    public Guid SalvageSiteId;
    public SiteScanState ScanState;
    public float ScanProgress;              // accumulated toward SalvageSite.ScanDifficulty
    public GameTime LastUpdated;
}
```

`ExplorationManager` holds these in a `Dictionary<(Guid empireId, Guid siteId), SalvageSiteKnowledge>`. Absence of an entry means `Undiscovered`. The manager creates a `CoarseKnown` entry when a fleet owned by that empire enters the site's system for the first time.

## 3.4 Fleet Location & Orders

Locations and orders are discriminated records. This keeps switch statements exhaustive and makes serialization straightforward.

```csharp
public abstract record FleetLocation;
public record InSystem(Guid StarSystemId) : FleetLocation;
public record OnLane(Guid FromSystemId, Guid ToSystemId, float Progress) : FleetLocation;  // Progress in 0..1

public abstract record FleetOrder;
public record Idle() : FleetOrder;
public record MoveToSystem(Guid TargetSystemId, Queue<Guid> LaneRoute) : FleetOrder;
public record ScanSite(Guid SiteId) : FleetOrder;
public record ExtractSite(Guid SiteId) : FleetOrder;
```

A fleet holds one active order. If the player wants a scout scanning while a salvager extracts, they split the fleet first. Split and merge are instantaneous in MVP; the reorganization delay in the GDD is a tuning knob to enable later.

## 3.5 Ship Types (MVP)

Only two ship types ship with the first slice. Both are non-combatant for the purposes of this loop.

```csharp
public class ShipType
{
    public string Id;                       // "scout_mk1", "salvager_mk1"
    public string DisplayName;
    public float ScanStrength;              // scan points per tick when ScanSite active
    public float ExtractionStrength;        // extraction units per tick when ExtractSite active
    public float Speed;                     // lane progress per tick
    public float UpkeepEnergy;              // MVP: minimal, color unspecified
}
```

| ShipType | ScanStrength | ExtractionStrength | Speed | Notes |
|---|---|---|---|---|
| scout_mk1 | 10 | 1 | 0.07 | Fast, great scan, negligible hauling |
| salvager_mk1 | 2 | 15 | 0.04 | Slow, bulk extraction |

A fleet's aggregate `ScanStrength` and `ExtractionStrength` are the sums over its ships. Fleet speed is the minimum over its ships (the slowest sets the pace).

---

# 4. Managers

Four managers participate in the loop. Per the existing architecture principle, they do not call each other directly — they emit and subscribe to events on `EventBus`.

## 4.1 GalaxyManager (new)

Owns the static world. Generated once at new-game init and read-only thereafter.

**Generation pipeline:**

1. Lay out N star systems (N=20 for MVP) across a 2D spiral using jittered polar sampling — 4 arms radiating from a dense core blob of ~5 systems.
2. Assign each non-core system an `ArmAlignment` based on angular position. Core systems are unaligned.
3. Compute a Delaunay triangulation over system positions. Prune edges to a target average connectivity of ~2.5, preserving connectivity of the full graph and leaving 1–2 chokepoints intact.
4. Create 2–4 POIs per system. POI type distribution is biased by arm alignment (debris fields common, ship graveyards rarer).
5. For each POI, roll a ~40% chance to spawn a `SalvageSite`. `PrimaryColor` is biased toward the hosting system's arm (70% aligned, 30% mixed). `Type` is weighted by POI type — `DebrisField` POIs host `DebrisField` sites, `ShipGraveyard` POIs host `ShipGraveyard` sites, etc.
6. Generate yield tables per `SalvageSiteType` using the tuning table in §7.

**Responsibilities after generation:** none. The data is queried by other managers but never mutated.

## 4.2 FleetManager

Owns fleet composition, location, and orders. Ticks fleet state forward each game tick.

**Per-tick logic:**

- For each fleet with order `MoveToSystem(target, route)`:
  - If currently `InSystem(s)` and `s == target`: set order to `Idle`, emit `FleetEnteredSystem { empireId, systemId: s }`.
  - If currently `InSystem(s)` and route non-empty: pop next lane, transition to `OnLane(s, nextSystem, 0)`.
  - If currently `OnLane(a, b, p)`: advance `p += fleetSpeed / laneLength`. If `p >= 1`: transition to `InSystem(b)`, emit `FleetEnteredSystem`. If destination reached (route empty and `b == target`), set order to `Idle`.
- For each fleet with order `ScanSite(siteId)`:
  - Guard: fleet must be `InSystem(s)` where `s` matches the site's system. Otherwise cancel the order.
  - Compute `deltaScan = Σ(ship.ScanStrength)` for all ships in the fleet.
  - Emit `ScanProgressTick { empireId, siteId, delta: deltaScan }`.
- For each fleet with order `ExtractSite(siteId)`:
  - Guard: same location check; plus the per-empire knowledge must be `Scanned`. Otherwise cancel.
  - Compute per-resource extraction against `RemainingYield` using the depletion curve.
  - Emit `YieldExtracted { empireId, siteId, resource, amount }` per colored resource drawn.

FleetManager also listens for `SiteScanComplete` and sets the corresponding fleet's order to `Idle`.

## 4.3 ExplorationManager

Owns per-empire site knowledge. Never ticks directly — reacts to events.

**Subscribes to:**

- `FleetEnteredSystem { empireId, systemId }`: for every salvage site in that system, if no knowledge entry exists for this empire, create `SalvageSiteKnowledge` in `CoarseKnown` state and emit `SiteDiscovered { empireId, siteId }`.
- `ScanProgressTick { empireId, siteId, delta }`: look up the knowledge entry, advance `ScanProgress`. If `ScanProgress >= site.ScanDifficulty`: flip state to `Scanned`, clear progress, emit `SiteScanComplete { empireId, siteId }`.

## 4.4 ResourceManager

Already specified in the implementation plan (§"System 1: Resources"). For the core loop, it needs only one new subscription:

- `YieldExtracted { empireId, siteId, resource, amount }`: mutate the empire stockpile, emit `StockpileChanged { empireId, resource, newValue, delta }`.

---

# 5. Event Flow

The full chain from player click to stockpile increase:

```
Player: select fleet, right-click destination system on map
  → FleetManager: set fleet.Order = MoveToSystem(target, route)
  → emit FleetOrderChanged

[N ticks of lane traversal, FleetManager emits FleetLocationChanged each tick]

FleetManager: fleet arrives → emit FleetEnteredSystem
  → ExplorationManager: create CoarseKnown entries, emit SiteDiscovered (one per site)
    → RightPanel: re-render POI list with newly visible sites
    → RecentEvents: toast "N DERELICTS DISCOVERED · <SYSTEM>"

Player: select POI, press SCAN
  → FleetManager: set fleet.Order = ScanSite(siteId)
  → emit FleetOrderChanged

[N ticks of scanning, FleetManager emits ScanProgressTick each tick]

  → ExplorationManager: accumulate ScanProgress, re-emit SiteKnowledgeChanged
    → POIListItem: update progress bar
  → ExplorationManager: progress >= difficulty → flip to Scanned, emit SiteScanComplete
    → FleetManager: fleet.Order = Idle, emit FleetOrderChanged
    → POIListItem: flip to Scanned visual state

Player: press EXTRACT
  → FleetManager: set fleet.Order = ExtractSite(siteId)

[N ticks of extraction, each tick:]

  → FleetManager: emit YieldExtracted (per resource)
    → ResourceManager: mutate stockpile, emit StockpileChanged
      → TopBarHUD: update counter, pulse delta animation
      → POIListItem: shrink depletion bars
```

No manager ever calls another directly. Every arrow above is an event-bus signal.

---

# 6. UI/UX

The galaxy map screen is the whole UI. All state is reachable from it.

## 6.1 Selection Model

Three selection contexts exist and they are **orthogonal** — changing one never clears another. This is deliberate: selecting a system to inspect it should not cancel which fleet is about to act, and selecting a POI should not cancel which system is open.

- **Selected Fleet** — at most one at a time. Drives which fleet an action applies to. Persistent until another fleet is selected.
- **Selected System** — at most one at a time. Drives the right panel contents. Persistent until another system is selected.
- **Selected POI** — at most one at a time, and must belong to the currently selected system (switching systems clears the POI selection). Drives which POI the action buttons target.

**Two views of the same fleet selection.** Fleets appear in two places — as rows in the left panel's FLEETS tab, and as icons on the galaxy map. These are two views of one underlying selection state. Clicking a fleet in either view sets the same selected fleet, and both views render the selection highlight.

## 6.2 Input Rules

Input follows a standard strategy-game convention: left-click selects, right-click commands. The specific rules below resolve every click collision on the galaxy map.

### Left-click (selection only, never issues an order)

| Target | Effect |
|---|---|
| Fleet icon on the map | Sets Selected Fleet. Does not change Selected System or Selected POI. |
| Fleet row in the left panel | Same as above (same underlying selection). |
| System node on the map | Sets Selected System, fills right panel with its POIs. Clears Selected POI. Does not change Selected Fleet. |
| POI row in the right panel | Sets Selected POI. Does not change Selected Fleet or Selected System. |
| Empty map space | Clears Selected System (and therefore Selected POI). Does not change Selected Fleet. |
| Action button on a POI row (`SCAN`, `EXTRACT`, `CANCEL`) | Issues the corresponding order. This is the only left-click that commands rather than selects. |

### Right-click (command only, never changes selection)

| Target | Effect |
|---|---|
| System node on the map | If a fleet is selected and the system is reachable, issue `MoveToSystem` from the selected fleet to that system. Otherwise no-op. |
| Anything else | No-op. |

### Click-priority rule

Fleet icons and system nodes can overlap when a fleet is docked at a system. Fleets take click priority because they are the smaller, more specific target and selecting a docked fleet is the common case a player wants when clicking there. System selection in that case is achieved by clicking the system's name/halo area outside the fleet icon, or by clicking the system on the minimap.

### Keyboard shortcuts (future, called out here for design continuity)

- `Escape` — clears all three selections.
- `Tab` / `Shift+Tab` — cycle Selected Fleet through the fleet list.
- `Double-click a fleet` — pan the camera to center on that fleet (equivalent to the "Take me to fleet" button in the GDD).

## 6.3 Worked Example — The Two Selections Working Together

The player wants to compare two systems before committing a fleet to either.

1. Left-click Scout Alpha in the fleet list. Selected Fleet = Alpha. Selected System = none. Right panel is empty.
2. Left-click System A on the map. Selected Fleet = Alpha (unchanged). Selected System = A. Right panel shows A's POIs.
3. Player reads A's POI list, decides to look at B.
4. Left-click System B on the map. Selected Fleet = Alpha (still unchanged). Selected System = B. Right panel now shows B's POIs.
5. Player decides B is better. Right-click System B. `MoveToSystem` order issued from Alpha to B.

Alpha was selected once in step 1 and stayed selected through the browsing in 2–4. The inspection phase never interfered with the action phase.

## 6.4 POI Row Visual States

Every salvage-site POI renders in one of four states, driven by `SalvageSiteKnowledge.ScanState` and any active fleet order targeting it.

**Undiscovered** — not rendered at all; the POI row doesn't appear for this empire.

**Coarse** — color accent bar in the site's precursor color on the left edge. Type tag reads `DERELICT · CRIMSON` (or the corresponding color name). No yield information visible. Action buttons: `SCAN` (primary), `CLAIM` (disabled), `BUILD OUTPOST` (disabled), empty fourth slot.

**Scanned** — same accent. Yield breakdown rendered as one 2px bar per `ColoredResource` present in `RemainingYield`, each labeled with the resource icon and remaining amount in Share Tech Mono. Action buttons: `EXTRACT` (primary), `CLAIM` (disabled), `BUILD OUTPOST` (disabled), empty fourth.

**In progress** (scan or extract) — overlay on top of either Coarse or Scanned base state. A 2px progress strip runs under the POI name with the fleet name at 9px Share Tech Mono: `SCOUT ALPHA · 43%` for scan, or per-bar shrinkage animation for extract. The primary action button becomes `CANCEL`.

## 6.5 End-to-End Click Path

1. **Start.** Player has Scout Alpha and Salvager Bravo in their home system. Left panel shows both fleets. Right panel shows the home system's POIs (all Scanned by default at game start for the home system).
2. **Plot a course.** Player left-clicks an adjacent system on the galaxy map. Selected System switches to it; Selected Fleet is unchanged. Right panel shows the new system with no POIs — it reads "unexplored" because nothing has been scouted there.
3. **Send scout.** Player left-clicks Scout Alpha in the left panel to set Selected Fleet. Then right-clicks the destination system on the map to issue `MoveToSystem`. Scout Alpha's fleet-list row status tag changes to `EN ROUTE` with an ETA in seconds. The fleet icon begins animating along the lane, and a dashed route line appears from the fleet to the destination with dashes scrolling toward the target.
4. **Arrival.** Fleet reaches the target system. `FleetEnteredSystem` fires. `ExplorationManager` reveals salvage sites. Right panel re-renders with 2–3 new POI rows in Coarse state. Toast slides into Recent Events: `2 DERELICTS DISCOVERED · VEGA REACH`. Scout Alpha's status returns to `IDLE`.
5. **Scan.** Player left-clicks the first Coarse POI row to set Selected POI, then presses `SCAN`. Scout Alpha's order becomes `ScanSite`. A progress strip appears on the POI row under the name, reading `SCOUT ALPHA · 0%`. It fills as scan progresses. Fleet row status reads `SCANNING · 18%` (synced).
6. **Scan complete.** Progress hits 100%. `SiteScanComplete` fires. POI row flips to Scanned state — the yield bars appear with resource icons and quantities. Scout Alpha returns to Idle. Primary action on the POI row becomes `EXTRACT`.
7. **Extract.** Player left-clicks Salvager Bravo in the left panel (Selected Fleet switches to Bravo; the POI selection is unchanged because selections are orthogonal). With the scanned POI still selected, presses `EXTRACT`. Salvager Bravo's order becomes `ExtractSite`. Yield bars begin to shrink. Topbar resource counters for the relevant colored resources tick upward with delta pulses.
8. **Cancel or drift.** Player can press `CANCEL` at any time. Otherwise, the depletion curve slows extraction over time; once the remaining fraction drops below a chosen threshold, the player typically moves on to the next site.

## 6.6 Visual Feedback

These are stubbable but should be earmarked as first-class concerns, not afterthoughts — they carry most of the loop's feel:

- **Lane transit.** Faction-tinted trail behind the fleet icon. Speed proportional to real transit time.
- **Route preview line.** When a fleet is selected and has an active `MoveToSystem` order, a dashed line renders along the remaining lane route from the fleet's current position to the destination system. The dashes animate in the direction of travel — the dash pattern scrolls toward the destination, reinforcing the sense of movement. Line style: `1.2px dashed`, faction accent color at 50% opacity, dash pattern `6 8`. The animation speed should be proportional to fleet speed (faster fleets = faster dash scroll). The line disappears when the fleet arrives or the order is cancelled. When a fleet is selected but idle, no route line is shown.
- **Discovery flash.** When a system's POIs are first revealed, the system node on the map pulses briefly in the empire's accent color.
- **Scanning ring.** While a scan is active, the POI location on the galaxy map draws a subtle expanding ring in the scanning fleet's accent color.
- **Extraction sparkle.** While extract is active, the system node shows a small particle emission tinted by the primary resource color being drawn.
- **Topbar delta pulse.** When `StockpileChanged` fires, the affected resource cell briefly glows with the delta value rendered in Share Tech Mono next to the counter.
- **State transition flash.** POI row flashes once (white fade to base color) when `ScanState` flips Coarse → Scanned.

All visual feedback is driven by the event bus, not by the UI polling manager state.

---

# 7. File & Node Layout

```
src/Core/Models/
  StarSystem.cs
  Lane.cs
  PointOfInterest.cs
  FleetLocation.cs              (InSystem, OnLane records)
  FleetOrder.cs                 (Idle, MoveToSystem, ScanSite, ExtractSite records)
  SalvageSite.cs                (MVP-simplified)
  SalvageSiteKnowledge.cs
  ShipType.cs
  Fleet.cs
  Ship.cs

src/Core/Systems/
  GalaxyGenerator.cs            (one-shot procedural generation)
  LanePathfinder.cs             (Dijkstra over the lane graph)
  ScanCalculator.cs
  ExtractionCalculator.cs       (depletion-curve math)

src/Core/Enums/
  SalvageSiteType.cs
  PoiType.cs
  SiteScanState.cs

src/Managers/                   (all Godot autoloads)
  EventBus.cs                   (existing)
  SimulationManager.cs          (existing)
  GalaxyManager.cs              (NEW — owns map, read-only after gen)
  FleetManager.cs
  ExplorationManager.cs         (NEW manager for this slice)
  ResourceManager.cs            (existing; +1 subscription)

src/Nodes/Galaxy/
  GalaxyMapView.cs              (Control; renders starfield, lanes, systems, fleets)
  StarSystemNode.cs
  LaneNode.cs
  FleetIconNode.cs              (animates along a lane)

src/Nodes/UI/
  TopBarHUD.cs
  LeftPanel.cs
  FleetListItem.cs
  RightPanel.cs
  PoiListItem.cs                (owns the four-state render logic)
  RecentEventsFeed.cs

data/
  ship_types.json               (scout_mk1, salvager_mk1)
  salvage_site_types.json       (scan difficulty + yield ranges per type)
  galaxy_gen_config.json        (system count, arm count, POI density, etc.)
```

---

# 8. Tuning Knobs

All values below live in data files, not constants in code. Target numbers are for x1 speed (1 tick ≈ 1 second).

## 8.1 Salvage Site Types

| Type | ScanDifficulty | Total Yield (color-matched, simple tier) | Color Bias |
|---|---|---|---|
| MinorDerelict | 150 | 30–80 units across 1–2 resources | Pure aligned color |
| DebrisField | 250 | 60–140 units across 2–4 resources | Mixed (multiple colors) |
| ShipGraveyard | 500 | 200–400 units across 2–3 resources | Pure aligned color |
| AbandonedStation | 400 | 150–300 units, components-heavy | Pure aligned color |

## 8.2 Ship Types

See §3.5. Targets: a Scout crosses an average lane in ~15 ticks. Solo Scout scans a MinorDerelict in ~15 ticks, ShipGraveyard in ~50 ticks. Solo Salvager fully extracts a MinorDerelict in ~3 minutes of real time at x1 (accounting for the depletion curve).

## 8.3 Core Formulas

```
laneTransitTicks = lane.LengthUnits / fleet.Speed
scanTicks        = site.ScanDifficulty / Σ(ship.ScanStrength)

// Extraction per tick, per resource
remainingFrac  = RemainingYield[r] / TotalYield[r]
perTickYield   = Σ(ship.ExtractionStrength) * pow(remainingFrac, DepletionCurveExponent)
                * (TotalYield[r] / sum(TotalYield))  // distributes across resources proportionally
```

`DepletionCurveExponent = 0.5` by default — first 50% of a site extracts quickly, the last 10% drags. Never hits zero; the loop's exit condition is player decision, not exhaustion.

---

# 9. Implementation Order

This slice is a subset of Phase 1 + the first two bullets of Phase 2 from the Implementation Plan. Suggested build sequence:

1. **Galaxy generation and rendering.** `GalaxyGenerator` + `GalaxyMapView` + `StarSystemNode` + `LaneNode`. No fleets yet. Result: a navigable, zoomable static map.
2. **Fleet data model and list UI.** `Fleet`, `Ship`, `ShipType`, `FleetManager` stub, `LeftPanel`, `FleetListItem`. Fleets visible in the list but stationary.
3. **Lane movement.** `MoveToSystem` order, `LanePathfinder`, `FleetIconNode` animation, `FleetEnteredSystem` event. Right-click-to-move works.
4. **Discovery on entry.** `ExplorationManager`, `SalvageSiteKnowledge`, `SiteDiscovered` event, POI list rendering in `RightPanel` with Coarse state.
5. **Scanning.** `ScanSite` order, `ScanProgressTick`, `SiteScanComplete`, inline progress bar on POI row, Coarse → Scanned flip.
6. **Extraction.** `ExtractSite` order, depletion-curve math, `YieldExtracted` → `StockpileChanged`, topbar updates, depletion bar shrinkage.
7. **Recent events feed.** Toast slides for the four interesting events (discovery, scan complete, extraction started, site near-depleted).
8. **Tuning pass.** Play the loop start-to-finish five times. Adjust the numbers in §8 until the pacing feels right.

After step 8, the slice is complete and the next increments — survey tiers, salvage layers, hazards, outposts, a second empire — can each be layered in without restructuring the four managers above.